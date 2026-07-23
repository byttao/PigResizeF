using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PigResizeF
{
    internal sealed class JdPreviewCanvas : ScrollableControl
    {
        private const int MaxCachedImages = 6;
        private const int MaxRenderedWidth = 2048;
        private readonly List<PreviewItem> items = new List<PreviewItem>();
        private readonly Dictionary<string, Bitmap> imageCache = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
        private readonly LinkedList<string> cacheOrder = new LinkedList<string>();
        private float zoom = 1F;
        private int compositionWidth;
        private long compositionHeight;

        public JdPreviewCanvas()
        {
            AutoScroll = true;
            DoubleBuffered = true;
            BackColor = System.Drawing.Color.White;
        }

        public void SetImages(IEnumerable<string> paths)
        {
            items.Clear();
            foreach (var path in paths)
            {
                var info = SixLabors.ImageSharp.Image.Identify(path)
                    ?? throw new InvalidOperationException($"无法读取图片：{Path.GetFileName(path)}");
                items.Add(new PreviewItem(path, info.Width, info.Height));
            }

            compositionWidth = items.Count == 0 ? 0 : items.Max(item => item.Width);
            compositionHeight = compositionWidth == 0
                ? 0
                : items.Sum(item => (long)Math.Max(1, Math.Round(item.Height * (double)compositionWidth / item.Width)));
            zoom = compositionWidth == 0 ? 1F : Math.Clamp((ClientSize.Width - 24F) / compositionWidth, 0.05F, 1F);
            ClearCache();
            UpdateScrollArea();
            Invalidate();
        }

        public void ClearImages()
        {
            items.Clear();
            compositionWidth = 0;
            compositionHeight = 0;
            zoom = 1F;
            ClearCache();
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
                var oldZoom = zoom;
                zoom = Math.Clamp(zoom * (e.Delta > 0 ? 1.15F : 1F / 1.15F), 0.05F, 2F);
                if (Math.Abs(oldZoom - zoom) > float.Epsilon)
                {
                    var contentY = -AutoScrollPosition.Y + e.Y;
                    UpdateScrollArea();
                    var newY = Math.Max(0, (int)(contentY * zoom / oldZoom - e.Y));
                    AutoScrollPosition = new Point(0, newY);
                    ClearCache();
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

            foreach (var item in items)
            {
                var itemHeight = Math.Max(1L, (long)Math.Round(item.Height * (double)compositionWidth / item.Width));
                var top = (long)Math.Round(y * zoom);
                var bottom = (long)Math.Round((y + itemHeight) * zoom);
                if (bottom >= viewportTop && top <= viewportBottom)
                {
                    var bitmap = GetBitmap(item, displayWidth);
                    var screenTop = Math.Clamp(top + AutoScrollPosition.Y, int.MinValue, int.MaxValue);
                    var screenHeight = Math.Clamp(bottom - top, 1, int.MaxValue);
                    e.Graphics.DrawImage(bitmap, AutoScrollPosition.X, (int)screenTop, displayWidth, (int)screenHeight);
                }
                y += itemHeight;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ClearCache();
            }
            base.Dispose(disposing);
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

        private Bitmap GetBitmap(PreviewItem item, int displayWidth)
        {
            if (imageCache.TryGetValue(item.Path, out var cached))
            {
                MoveToCacheTail(item.Path);
                return cached;
            }

            using var stream = File.OpenRead(item.Path);
            using var source = Image.FromStream(stream);
            var cacheWidth = Math.Min(MaxRenderedWidth, displayWidth);
            var cacheHeight = Math.Max(1, (int)Math.Round(source.Height * (double)cacheWidth / source.Width));
            var rendered = new Bitmap(cacheWidth, cacheHeight);
            using (var graphics = Graphics.FromImage(rendered))
            {
                graphics.Clear(System.Drawing.Color.White);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(source, 0, 0, cacheWidth, cacheHeight);
            }

            imageCache[item.Path] = rendered;
            cacheOrder.AddLast(item.Path);
            while (cacheOrder.Count > MaxCachedImages)
            {
                var oldest = cacheOrder.First!.Value;
                cacheOrder.RemoveFirst();
                imageCache[oldest].Dispose();
                imageCache.Remove(oldest);
            }
            return rendered;
        }

        private void MoveToCacheTail(string path)
        {
            var node = cacheOrder.Find(path);
            if (node != null)
            {
                cacheOrder.Remove(node);
                cacheOrder.AddLast(node);
            }
        }

        private void ClearCache()
        {
            foreach (var bitmap in imageCache.Values)
            {
                bitmap.Dispose();
            }
            imageCache.Clear();
            cacheOrder.Clear();
        }

        private sealed record PreviewItem(string Path, int Width, int Height);
    }
}
