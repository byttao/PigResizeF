using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ImageSharpImage = SixLabors.ImageSharp.Image;
using ImageSharpSize = SixLabors.ImageSharp.Size;

namespace PigResizeF
{
    // 京东详情页长图预览控件：只绘制可见区域，并缓存少量缩略图以控制内存占用。
    internal sealed class JdPreviewCanvas : ScrollableControl
    {
        // 缩略图宽度和缓存数量共同限制预览时的解码与内存压力。
        private const int ThumbnailWidth = 1024;
        private const int MaxCachedImages = 4;
        private readonly List<PreviewItem> items = new List<PreviewItem>();
        private readonly Dictionary<string, Bitmap> imageCache = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
        private readonly LinkedList<string> cacheOrder = new LinkedList<string>();
        private readonly HashSet<string> loadingPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> failedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly object cacheLock = new object();
        private readonly SemaphoreSlim decodeSemaphore = new SemaphoreSlim(2, 2);
        private float zoom = 1F;
        private int compositionWidth;
        private long compositionHeight;
        private int cacheGeneration;

        public JdPreviewCanvas()
        {
            AutoScroll = true;
            DoubleBuffered = true;
            BackColor = System.Drawing.Color.White;
        }

        public void SetImages(IEnumerable<string> paths)
        {
            // 仅读取图片信息来计算长图布局，不在此处拼接或解码完整图片。
            var newItems = new List<PreviewItem>();
            foreach (var path in paths)
            {
                var info = ImageSharpImage.Identify(path)
                    ?? throw new InvalidOperationException($"无法读取图片：{Path.GetFileName(path)}");
                var orientation = GetOrientation(info);
                var (width, height) = GetDisplayDimensions(info.Width, info.Height, orientation);
                newItems.Add(new PreviewItem(path, width, height, orientation));
            }

            // 仅调整排序时复用缓存，避免用户移动列表后重新加载缩略图。
            var sameImageSet = items.Count == newItems.Count &&
                items.Select(item => item.Path).OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .SequenceEqual(newItems.Select(item => item.Path).OrderBy(path => path, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
            items.Clear();
            items.AddRange(newItems);

            compositionWidth = items.Count == 0 ? 0 : items.Max(item => item.Width);
            compositionHeight = compositionWidth == 0
                ? 0
                : items.Sum(item => (long)Math.Max(1, Math.Round(item.Height * (double)compositionWidth / item.Width)));
            zoom = compositionWidth == 0 ? 1F : Math.Clamp((ClientSize.Width - 24F) / compositionWidth, 0.05F, 1F);
            if (!sameImageSet)
            {
                ResetCache();
            }
            UpdateScrollArea();
            Invalidate();
        }

        public void ClearImages()
        {
            items.Clear();
            compositionWidth = 0;
            compositionHeight = 0;
            zoom = 1F;
            ResetCache();
            AutoScrollMinSize = Size.Empty;
            AutoScrollPosition = Point.Empty;
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateScrollArea();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if ((ModifierKeys & Keys.Control) == Keys.Control && compositionWidth > 0)
            {
                // Ctrl + 滚轮以鼠标所在内容位置为锚点缩放，普通滚轮仍交给滚动容器。
                var oldZoom = zoom;
                zoom = Math.Clamp(zoom * (e.Delta > 0 ? 1.15F : 1F / 1.15F), 0.05F, 2F);
                if (Math.Abs(oldZoom - zoom) > float.Epsilon)
                {
                    var contentY = -AutoScrollPosition.Y + e.Y;
                    UpdateScrollArea();
                    var newY = Math.Max(0, (int)(contentY * zoom / oldZoom - e.Y));
                    AutoScrollPosition = new Point(0, newY);
                    Invalidate();
                }
                return;
            }

            base.OnMouseWheel(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (items.Count == 0 || compositionWidth == 0)
            {
                return;
            }

            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            var viewportTop = -AutoScrollPosition.Y;
            var viewportBottom = viewportTop + ClientSize.Height;
            var y = 0L;
            var displayWidth = Math.Max(1, (int)Math.Round(compositionWidth * zoom));

            // 超长预览只请求和绘制当前视口相交的图片。
            foreach (var item in items)
            {
                var itemHeight = Math.Max(1L, (long)Math.Round(item.Height * (double)compositionWidth / item.Width));
                var top = (long)Math.Round(y * zoom);
                var bottom = (long)Math.Round((y + itemHeight) * zoom);
                if (bottom >= viewportTop && top <= viewportBottom)
                {
                    DrawPreviewItem(e.Graphics, item, displayWidth, top, bottom);
                }
                y += itemHeight;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ResetCache();
            }
            base.Dispose(disposing);
        }

        private void DrawPreviewItem(Graphics graphics, PreviewItem item, int displayWidth, long top, long bottom)
        {
            var screenTop = Math.Clamp(top + AutoScrollPosition.Y, int.MinValue, int.MaxValue);
            var screenHeight = Math.Clamp(bottom - top, 1, int.MaxValue);
            var bitmap = TryGetCachedBitmap(item.Path);
            if (bitmap != null)
            {
                graphics.DrawImage(bitmap, AutoScrollPosition.X, (int)screenTop, displayWidth, (int)screenHeight);
                return;
            }

            RequestThumbnail(item);
            using var placeholder = new SolidBrush(System.Drawing.Color.FromArgb(241, 245, 249));
            graphics.FillRectangle(placeholder, AutoScrollPosition.X, (int)screenTop, displayWidth, (int)screenHeight);
        }

        private void RequestThumbnail(PreviewItem item)
        {
            int generation;
            lock (cacheLock)
            {
                if (imageCache.ContainsKey(item.Path) || loadingPaths.Contains(item.Path) || failedPaths.Contains(item.Path))
                {
                    return;
                }

                loadingPaths.Add(item.Path);
                generation = cacheGeneration;
            }

            // 解码放到后台线程，绘制线程只负责显示已有缓存或占位块。
            _ = Task.Run(() => LoadThumbnailAsync(item, generation));
        }

        private async Task LoadThumbnailAsync(PreviewItem item, int generation)
        {
            Bitmap? bitmap = null;
            try
            {
                await decodeSemaphore.WaitAsync();
                try
                {
                    bitmap = CreateThumbnail(item);
                }
                finally
                {
                    decodeSemaphore.Release();
                }
            }
            catch (Exception)
            {
                // 单张源图损坏不影响其余图片的预览。
            }

            if (IsDisposed || !IsHandleCreated)
            {
                bitmap?.Dispose();
                return;
            }

            try
            {
                BeginInvoke(new Action(() => CompleteThumbnail(item.Path, generation, bitmap)));
            }
            catch (InvalidOperationException)
            {
                bitmap?.Dispose();
            }
        }

        private void CompleteThumbnail(string path, int generation, Bitmap? bitmap)
        {
            lock (cacheLock)
            {
                if (generation != cacheGeneration)
                {
                    bitmap?.Dispose();
                    return;
                }

                loadingPaths.Remove(path);
                if (bitmap == null)
                {
                    failedPaths.Add(path);
                    return;
                }

                imageCache[path] = bitmap;
                cacheOrder.AddLast(path);
                // 使用最近最少使用顺序淘汰旧缩略图，防止连续滚动占满内存。
                while (cacheOrder.Count > MaxCachedImages)
                {
                    var oldest = cacheOrder.First!.Value;
                    cacheOrder.RemoveFirst();
                    imageCache[oldest].Dispose();
                    imageCache.Remove(oldest);
                }
            }

            Invalidate();
        }

        private Bitmap? TryGetCachedBitmap(string path)
        {
            lock (cacheLock)
            {
                if (!imageCache.TryGetValue(path, out var bitmap))
                {
                    return null;
                }

                var node = cacheOrder.Find(path);
                if (node != null)
                {
                    cacheOrder.Remove(node);
                    cacheOrder.AddLast(node);
                }
                return bitmap;
            }
        }

        private static Bitmap CreateThumbnail(PreviewItem item)
        {
            // 解码阶段直接请求目标尺寸，避免先加载大图再缩放。
            var targetWidth = Math.Min(ThumbnailWidth, item.Width);
            var targetHeight = Math.Max(1, (int)Math.Round(item.Height * (double)targetWidth / item.Width));
            var decoderOptions = new DecoderOptions
            {
                TargetSize = new ImageSharpSize(targetWidth, targetHeight),
                SkipMetadata = true,
                MaxFrames = 1
            };

            using var image = ImageSharpImage.Load<Bgra32>(decoderOptions, item.Path);
            ApplyOrientation(image, item.Orientation);
            if (image.Width != targetWidth || image.Height != targetHeight)
            {
                image.Mutate(context => context.Resize(targetWidth, targetHeight));
            }
            return ConvertToBitmap(image);
        }

        private static Bitmap ConvertToBitmap(SixLabors.ImageSharp.Image<Bgra32> image)
        {
            var pixels = new Bgra32[image.Width * image.Height];
            image.CopyPixelDataTo(pixels);
            var sourceBytes = MemoryMarshal.AsBytes(pixels.AsSpan()).ToArray();
            var bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
            var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var data = bitmap.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                var rowLength = bitmap.Width * 4;
                if (data.Stride == rowLength)
                {
                    Marshal.Copy(sourceBytes, 0, data.Scan0, sourceBytes.Length);
                }
                else
                {
                    for (var row = 0; row < bitmap.Height; row++)
                    {
                        Marshal.Copy(sourceBytes, row * rowLength, IntPtr.Add(data.Scan0, row * data.Stride), rowLength);
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
            return bitmap;
        }

        private static ushort GetOrientation(SixLabors.ImageSharp.ImageInfo info)
        {
            if (info.Metadata.ExifProfile != null &&
                info.Metadata.ExifProfile.TryGetValue(ExifTag.Orientation, out IExifValue<ushort>? orientationValue) &&
                orientationValue?.Value is ushort orientation)
            {
                return orientation;
            }
            return 1;
        }

        private static (int Width, int Height) GetDisplayDimensions(int width, int height, ushort orientation)
        {
            return orientation is 5 or 6 or 7 or 8 ? (height, width) : (width, height);
        }

        private static void ApplyOrientation(SixLabors.ImageSharp.Image<Bgra32> image, ushort orientation)
        {
            image.Mutate(context =>
            {
                switch (orientation)
                {
                    case 2:
                        context.Flip(FlipMode.Horizontal);
                        break;
                    case 3:
                        context.Rotate(RotateMode.Rotate180);
                        break;
                    case 4:
                        context.Flip(FlipMode.Vertical);
                        break;
                    case 5:
                        context.Flip(FlipMode.Vertical).Rotate(RotateMode.Rotate90);
                        break;
                    case 6:
                        context.Rotate(RotateMode.Rotate90);
                        break;
                    case 7:
                        context.Flip(FlipMode.Horizontal).Rotate(RotateMode.Rotate90);
                        break;
                    case 8:
                        context.Rotate(RotateMode.Rotate270);
                        break;
                }
            });
        }

        private void ResetCache()
        {
            lock (cacheLock)
            {
                // 代次递增后，后台尚未完成的旧任务会在回到 UI 线程时被丢弃。
                cacheGeneration++;
                foreach (var bitmap in imageCache.Values)
                {
                    bitmap.Dispose();
                }
                imageCache.Clear();
                cacheOrder.Clear();
                loadingPaths.Clear();
                failedPaths.Clear();
            }
        }

        private void UpdateScrollArea()
        {
            if (compositionWidth == 0)
            {
                AutoScrollMinSize = Size.Empty;
                return;
            }

            AutoScrollMinSize = new Size(
                Math.Max(1, (int)Math.Min(int.MaxValue, Math.Ceiling(compositionWidth * zoom))),
                Math.Max(1, (int)Math.Min(int.MaxValue, Math.Ceiling(compositionHeight * zoom))));
        }

        private sealed record PreviewItem(string Path, int Width, int Height, ushort Orientation);
    }
}
