using PigResizeF.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PigResizeF.Core.Services
{
    /// <summary>
    /// 图片处理服务实现：提供图片转换、裁切、压缩等核心功能。
    /// </summary>
    public class ImageProcessor : IImageProcessor
    {
        // 支持的图片格式
        private static readonly string[] SupportedFormats = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };

        /// <summary>
        /// 获取支持的图片格式
        /// </summary>
        public IEnumerable<string> GetSupportedFormats() => SupportedFormats;

        /// <summary>
        /// 验证文件是否为有效图片
        /// </summary>
        public bool IsValidImage(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return SupportedFormats.Contains(extension);
        }

        /// <summary>
        /// 转换单个图片：等比缩放并用白底补齐到目标尺寸
        /// </summary>
        public async Task<ProcessResult> ConvertImageAsync(
            string inputPath,
            ProcessOptions options,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new ProcessResult();

            try
            {
                if (!IsValidImage(inputPath))
                {
                    result.Success = false;
                    result.ErrorMessage = "不支持的图片格式";
                    return result;
                }

                var fileInfo = new FileInfo(inputPath);
                result.OriginalSize = fileInfo.Length;

                progress?.Report($"正在处理: {Path.GetFileName(inputPath)}");

                // 加载图片
                using var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(inputPath, cancellationToken);

                // 根据 EXIF 方向修正
                OrientImage(image);

                // 决定是否需要调整尺寸
                SixLabors.ImageSharp.Image<Rgba32> processedImage;
                if (options.ResizeImage && (options.TargetWidth > 0 || options.TargetHeight > 0))
                {
                    if (options.MaintainAspectRatio && options.PadWithBackground)
                    {
                        // 等比缩放并补白边
                        processedImage = ResizeImageWithPadding(
                            image,
                            options.TargetWidth,
                            options.TargetHeight,
                            options.BackgroundColor);
                    }
                    else if (options.MaintainAspectRatio)
                    {
                        // 等比缩放
                        processedImage = ResizeImageProportional(image, options.TargetWidth, options.TargetHeight);
                    }
                    else
                    {
                        // 强制拉伸到目标尺寸
                        processedImage = ResizeImageStretch(image, options.TargetWidth, options.TargetHeight);
                    }
                }
                else
                {
                    // 不调整尺寸，仅转换格式
                    processedImage = new SixLabors.ImageSharp.Image<Rgba32>(image.Width, image.Height);
                    processedImage.Mutate(x => x.DrawImage(image, 1.0f));
                }

                // 计算输出路径
                var outputPath = BuildOutputPath(inputPath, options);

                // 确保输出目录存在
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // 根据模式保存图片
                await SaveImageAsync(processedImage, outputPath, options, cancellationToken);

                processedImage.Dispose();

                // 获取输出文件信息
                var outputFileInfo = new FileInfo(outputPath);
                result.Success = true;
                result.OutputPath = outputPath;
                result.OutputSize = outputFileInfo.Length;
                result.Duration = stopwatch.Elapsed;

                progress?.Report($"已保存: {Path.GetFileName(outputPath)}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                progress?.Report($"处理失败: {ex.Message}");
            }

            stopwatch.Stop();
            return result;
        }

        /// <summary>
        /// 批量转换图片
        /// </summary>
        public async Task<List<ProcessResult>> ConvertImagesAsync(
            IEnumerable<string> inputPaths,
            ProcessOptions options,
            IProgress<(int current, int total, string message)>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var results = new List<ProcessResult>();
            var paths = inputPaths.ToList();
            var total = paths.Count;

            for (int i = 0; i < total; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var path = paths[i];
                progress?.Report((i + 1, total, $"正在处理 {i + 1}/{total}: {Path.GetFileName(path)}"));

                var result = await ConvertImageAsync(path, options, null, cancellationToken);
                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// 裁切单个图片：智能裁切白边
        /// </summary>
        public async Task<ProcessResult> CropImageAsync(
            string inputPath,
            CropOptions options,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new ProcessResult();

            try
            {
                if (!IsValidImage(inputPath))
                {
                    result.Success = false;
                    result.ErrorMessage = "不支持的图片格式";
                    return result;
                }

                var fileInfo = new FileInfo(inputPath);
                result.OriginalSize = fileInfo.Length;

                progress?.Report($"正在裁切: {Path.GetFileName(inputPath)}");

                // 加载图片
                using var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(inputPath, cancellationToken);

                // 根据 EXIF 方向修正
                OrientImage(image);

                // 根据模式裁切
                var croppedImage = CropImageByMode(image, options);

                // 计算输出路径
                var outputPath = BuildCropOutputPath(inputPath, options);

                // 确保输出目录存在
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // 保存裁切后的图片
                var encoder = new JpegEncoder { Quality = options.JpegQuality };
                await croppedImage.SaveAsync(outputPath, encoder, cancellationToken);

                // 获取输出文件信息
                var outputFileInfo = new FileInfo(outputPath);
                result.Success = true;
                result.OutputPath = outputPath;
                result.OutputSize = outputFileInfo.Length;
                result.Duration = stopwatch.Elapsed;

                progress?.Report($"已保存: {Path.GetFileName(outputPath)}");

                croppedImage.Dispose();
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                progress?.Report($"裁切失败: {ex.Message}");
            }

            stopwatch.Stop();
            return result;
        }

        /// <summary>
        /// 批量裁切图片
        /// </summary>
        public async Task<List<ProcessResult>> CropImagesAsync(
            IEnumerable<string> inputPaths,
            CropOptions options,
            IProgress<(int current, int total, string message)>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var results = new List<ProcessResult>();
            var paths = inputPaths.ToList();
            var total = paths.Count;

            for (int i = 0; i < total; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var path = paths[i];
                progress?.Report((i + 1, total, $"正在裁切 {i + 1}/{total}: {Path.GetFileName(path)}"));

                var result = await CropImageAsync(path, options, null, cancellationToken);
                results.Add(result);
            }

            return results;
        }

        #region 私有辅助方法

        /// <summary>
        /// 等比缩放图片并用指定颜色补齐到目标尺寸
        /// </summary>
        private static SixLabors.ImageSharp.Image<Rgba32> ResizeImageWithPadding(
            SixLabors.ImageSharp.Image<Rgba32> sourceImage,
            int targetWidth,
            int targetHeight,
            uint backgroundColor)
        {
            // 计算缩放比例（取较小值，确保图片完全在画布内）
            float widthRatio = (float)targetWidth / sourceImage.Width;
            float heightRatio = (float)targetHeight / sourceImage.Height;
            float ratio = Math.Min(widthRatio, heightRatio);

            // 计算缩放后的尺寸
            int newWidth = (int)(sourceImage.Width * ratio);
            int newHeight = (int)(sourceImage.Height * ratio);

            // 创建临时图片用于缩放
            var resized = sourceImage.Clone(x => x.Resize(newWidth, newHeight));

            // 创建背景画布
            var bgColor = SixLabors.ImageSharp.Color.FromRgba(
                (byte)((backgroundColor >> 16) & 0xFF),
                (byte)((backgroundColor >> 8) & 0xFF),
                (byte)(backgroundColor & 0xFF),
                (byte)((backgroundColor >> 24) & 0xFF)
            );
            var paddedImage = new SixLabors.ImageSharp.Image<Rgba32>(targetWidth, targetHeight, bgColor);

            // 居中绘制
            int xOffset = (targetWidth - newWidth) / 2;
            int yOffset = (targetHeight - newHeight) / 2;

            paddedImage.Mutate(x => x.DrawImage(resized, new SixLabors.ImageSharp.Point(xOffset, yOffset), 1.0f));
            resized.Dispose();

            return paddedImage;
        }

        /// <summary>
        /// 等比缩放图片（不补边）
        /// </summary>
        private static SixLabors.ImageSharp.Image<Rgba32> ResizeImageProportional(
            SixLabors.ImageSharp.Image<Rgba32> sourceImage,
            int targetWidth,
            int targetHeight)
        {
            // 计算缩放比例
            float widthRatio = targetWidth > 0 ? (float)targetWidth / sourceImage.Width : float.MaxValue;
            float heightRatio = targetHeight > 0 ? (float)targetHeight / sourceImage.Height : float.MaxValue;
            float ratio = Math.Min(widthRatio, heightRatio);

            // 计算新尺寸
            int newWidth = (int)(sourceImage.Width * ratio);
            int newHeight = (int)(sourceImage.Height * ratio);

            return sourceImage.Clone(x => x.Resize(newWidth, newHeight));
        }

        /// <summary>
        /// 强制拉伸图片到目标尺寸
        /// </summary>
        private static SixLabors.ImageSharp.Image<Rgba32> ResizeImageStretch(
            SixLabors.ImageSharp.Image<Rgba32> sourceImage,
            int targetWidth,
            int targetHeight)
        {
            return sourceImage.Clone(x => x.Resize(targetWidth, targetHeight));
        }

        /// <summary>
        /// 根据 EXIF 方向修正图片
        /// </summary>
        private static void OrientImage(SixLabors.ImageSharp.Image<Rgba32> image)
        {
            if (image.Metadata.ExifProfile != null &&
                image.Metadata.ExifProfile.TryGetValue(ExifTag.Orientation, out IExifValue<ushort>? orientationValue) &&
                orientationValue?.Value is ushort orientation)
            {
                switch (orientation)
                {
                    case 2: // 水平翻转
                        image.Mutate(x => x.Flip(FlipMode.Horizontal));
                        break;
                    case 3: // 旋转 180 度
                        image.Mutate(x => x.Rotate(RotateMode.Rotate180));
                        break;
                    case 4: // 垂直翻转
                        image.Mutate(x => x.Flip(FlipMode.Vertical));
                        break;
                    case 5: // 垂直翻转后顺时针旋转 90 度
                        image.Mutate(x => x.Flip(FlipMode.Vertical).Rotate(RotateMode.Rotate90));
                        break;
                    case 6: // 顺时针旋转 90 度
                        image.Mutate(x => x.Rotate(RotateMode.Rotate90));
                        break;
                    case 7: // 水平翻转后顺时针旋转 90 度
                        image.Mutate(x => x.Flip(FlipMode.Horizontal).Rotate(RotateMode.Rotate90));
                        break;
                    case 8: // 顺时针旋转 270 度
                        image.Mutate(x => x.Rotate(RotateMode.Rotate270));
                        break;
                }
            }
        }

        /// <summary>
        /// 根据模式裁切图片
        /// </summary>
        private static SixLabors.ImageSharp.Image<Rgba32> CropImageByMode(
            SixLabors.ImageSharp.Image<Rgba32> image,
            CropOptions options)
        {
            return options.Mode switch
            {
                CropMode.Auto => CropWhiteBordersAuto(image, options.WhiteThreshold),
                CropMode.Manual => CropManual(image, options.TargetWidth, options.TargetHeight),
                CropMode.Grid => CropGrid(image, options.GridRows, options.GridColumns),
                CropMode.Random => CropRandom(image),
                _ => throw new ArgumentException($"不支持的裁切模式: {options.Mode}")
            };
        }

        /// <summary>
        /// 自动模式：智能检测并裁切白边
        /// </summary>
        private static SixLabors.ImageSharp.Image<Rgba32> CropWhiteBordersAuto(
            SixLabors.ImageSharp.Image<Rgba32> image,
            int whiteThreshold)
        {
            // 简化版本：检测四边的白边并裁切
            int left = 0, top = 0, right = image.Width - 1, bottom = image.Height - 1;

            // 从上往下扫描
            bool foundTop = false;
            for (int y = 0; y < image.Height && !foundTop; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    if (!IsWhitePixel(pixel, whiteThreshold))
                    {
                        top = y;
                        foundTop = true;
                        break;
                    }
                }
            }

            // 从下往上扫描
            bool foundBottom = false;
            for (int y = image.Height - 1; y >= top && !foundBottom; y--)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    if (!IsWhitePixel(pixel, whiteThreshold))
                    {
                        bottom = y;
                        foundBottom = true;
                        break;
                    }
                }
            }

            // 从左往右扫描
            bool foundLeft = false;
            for (int x = 0; x < image.Width && !foundLeft; x++)
            {
                for (int y = top; y <= bottom; y++)
                {
                    var pixel = image[x, y];
                    if (!IsWhitePixel(pixel, whiteThreshold))
                    {
                        left = x;
                        foundLeft = true;
                        break;
                    }
                }
            }

            // 从右往左扫描
            bool foundRight = false;
            for (int x = image.Width - 1; x >= left && !foundRight; x--)
            {
                for (int y = top; y <= bottom; y++)
                {
                    var pixel = image[x, y];
                    if (!IsWhitePixel(pixel, whiteThreshold))
                    {
                        right = x;
                        foundRight = true;
                        break;
                    }
                }
            }

            // 裁切
            int cropWidth = right - left + 1;
            int cropHeight = bottom - top + 1;

            if (cropWidth <= 0 || cropHeight <= 0 || (left == 0 && top == 0 && right == image.Width - 1 && bottom == image.Height - 1))
            {
                // 没有需要裁切的白边，返回原图
                return image.Clone(x => { });
            }

            return image.Clone(x => x.Crop(new SixLabors.ImageSharp.Rectangle(left, top, cropWidth, cropHeight)));
        }

        /// <summary>
        /// 判断像素是否为白色
        /// </summary>
        private static bool IsWhitePixel(Rgba32 pixel, int threshold)
        {
            return pixel.R >= threshold && pixel.G >= threshold && pixel.B >= threshold;
        }

        /// <summary>
        /// 手动模式：裁切到指定尺寸
        /// </summary>
        private static SixLabors.ImageSharp.Image<Rgba32> CropManual(
            SixLabors.ImageSharp.Image<Rgba32> image,
            int targetWidth,
            int targetHeight)
        {
            // 居中裁切
            int x = Math.Max(0, (image.Width - targetWidth) / 2);
            int y = Math.Max(0, (image.Height - targetHeight) / 2);
            int width = Math.Min(targetWidth, image.Width);
            int height = Math.Min(targetHeight, image.Height);

            return image.Clone(ctx => ctx.Crop(new SixLabors.ImageSharp.Rectangle(x, y, width, height)));
        }

        /// <summary>
        /// 网格模式：按网格裁切
        /// </summary>
        private static SixLabors.ImageSharp.Image<Rgba32> CropGrid(
            SixLabors.ImageSharp.Image<Rgba32> image,
            int rows,
            int columns)
        {
            // 简化版：返回第一个网格
            int cellWidth = image.Width / columns;
            int cellHeight = image.Height / rows;

            return image.Clone(x => x.Crop(new SixLabors.ImageSharp.Rectangle(0, 0, cellWidth, cellHeight)));
        }

        /// <summary>
        /// 随机模式：从中心区域随机裁切
        /// </summary>
        private static SixLabors.ImageSharp.Image<Rgba32> CropRandom(SixLabors.ImageSharp.Image<Rgba32> image)
        {
            // 简化版：返回原图
            return image.Clone(x => { });
        }

        /// <summary>
        /// 构建输出路径
        /// </summary>
        private static string BuildOutputPath(string inputPath, ProcessOptions options)
        {
            var directory = string.IsNullOrEmpty(options.OutputDirectory)
                ? Path.GetDirectoryName(inputPath) ?? ""
                : options.OutputDirectory;

            if (options.UseSubdirectory)
            {
                directory = Path.Combine(directory, options.SubdirectoryName);
            }

            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputPath);
            var fileName = $"{options.FileNamePrefix}{fileNameWithoutExt}{options.FileNameSuffix}";

            var extension = options.Mode switch
            {
                ConversionMode.ToPng => ".png",
                ConversionMode.ToWebP => ".webp",
                ConversionMode.ToBmp => ".bmp",
                _ => ".jpg"
            };

            return Path.Combine(directory, fileName + extension);
        }

        /// <summary>
        /// 构建裁切输出路径
        /// </summary>
        private static string BuildCropOutputPath(string inputPath, CropOptions options)
        {
            var directory = string.IsNullOrEmpty(options.OutputDirectory)
                ? Path.GetDirectoryName(inputPath) ?? ""
                : options.OutputDirectory;

            if (options.UseSubdirectory)
            {
                directory = Path.Combine(directory, options.SubdirectoryName);
            }

            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputPath);
            var modeSuffix = options.Mode.ToString().ToLowerInvariant();
            var fileName = $"{fileNameWithoutExt}_{modeSuffix}";

            return Path.Combine(directory, fileName + "." + options.OutputFormat);
        }

        /// <summary>
        /// 保存图片
        /// </summary>
        private static async Task SaveImageAsync(
            SixLabors.ImageSharp.Image<Rgba32> image,
            string outputPath,
            ProcessOptions options,
            CancellationToken cancellationToken)
        {
            switch (options.Mode)
            {
                case ConversionMode.ToJpg:
                    var jpegEncoder = new JpegEncoder { Quality = options.JpegQuality };
                    await image.SaveAsync(outputPath, jpegEncoder, cancellationToken);
                    break;

                case ConversionMode.ToPng:
                    var pngEncoder = new PngEncoder { CompressionLevel = (PngCompressionLevel)options.PngCompressionLevel };
                    await image.SaveAsync(outputPath, pngEncoder, cancellationToken);
                    break;

                case ConversionMode.ToWebP:
                    var webpEncoder = new WebpEncoder { Quality = options.WebPQuality };
                    await image.SaveAsync(outputPath, webpEncoder, cancellationToken);
                    break;

                case ConversionMode.ToBmp:
                    await image.SaveAsBmpAsync(outputPath, cancellationToken);
                    break;

                default:
                    throw new NotSupportedException($"不支持的转换模式: {options.Mode}");
            }
        }

        #endregion
    }
}
