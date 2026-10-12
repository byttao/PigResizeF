using PigResizeF.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
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
    /// 图片处理服务实现
    /// </summary>
    public class ImageProcessor : IImageProcessor
    {
        private static readonly string[] SupportedFormats = { ".jpg", ".jpeg", ".png", ".bmp", ".webp", ".gif" };

        /// <summary>
        /// 转换单个图片
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
                if (!File.Exists(inputPath))
                {
                    result.ErrorMessage = "文件不存在";
                    return result;
                }

                var fileInfo = new FileInfo(inputPath);
                result.OriginalSize = fileInfo.Length;

                progress?.Report($"正在处理: {Path.GetFileName(inputPath)}");

                // 加载图片
                using var image = await Image.LoadAsync<Rgba32>(inputPath, cancellationToken);

                // 修正 EXIF 方向
                OrientImage(image);

                // 确定输出路径
                string outputPath = GetOutputPath(inputPath, options);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

                // 根据模式保存图片
                await SaveImageAsync(image, outputPath, options, cancellationToken);

                result.OutputPath = outputPath;
                result.OutputSize = new FileInfo(outputPath).Length;
                result.Success = true;

                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;

                progress?.Report($"完成: {Path.GetFileName(outputPath)} ({FormatFileSize(result.OutputSize)})");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                progress?.Report($"失败: {ex.Message}");
            }

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
            int current = 0;
            int total = paths.Count;

            foreach (var path in paths)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                current++;
                progress?.Report((current, total, $"处理中 ({current}/{total})"));

                var fileProgress = new Progress<string>(msg =>
                    progress?.Report((current, total, msg)));

                var result = await ConvertImageAsync(path, options, fileProgress, cancellationToken);
                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// 裁切单个图片
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
                if (!File.Exists(inputPath))
                {
                    result.ErrorMessage = "文件不存在";
                    return result;
                }

                var fileInfo = new FileInfo(inputPath);
                result.OriginalSize = fileInfo.Length;

                progress?.Report($"正在裁切: {Path.GetFileName(inputPath)}");

                using var image = await Image.LoadAsync<Rgba32>(inputPath, cancellationToken);
                OrientImage(image);

                // 根据模式执行裁切
                Image<Rgba32> croppedImage;
                switch (options.Mode)
                {
                    case CropMode.Auto:
                        croppedImage = AutoCropWhitespace(image, options.WhiteThreshold);
                        break;
                    case CropMode.Manual:
                        croppedImage = ManualCrop(image, options.TargetWidth, options.TargetHeight);
                        break;
                    default:
                        croppedImage = image.Clone(ctx => { });
                        break;
                }

                // 确定输出路径
                string outputPath = GetCropOutputPath(inputPath, options);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

                // 保存裁切后的图片
                await SaveCroppedImageAsync(croppedImage, outputPath, options, cancellationToken);

                result.OutputPath = outputPath;
                result.OutputSize = new FileInfo(outputPath).Length;
                result.Success = true;

                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;

                progress?.Report($"裁切完成: {Path.GetFileName(outputPath)}");

                if (croppedImage != image)
                {
                    croppedImage.Dispose();
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                progress?.Report($"裁切失败: {ex.Message}");
            }

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
            int current = 0;
            int total = paths.Count;

            foreach (var path in paths)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                current++;
                progress?.Report((current, total, $"裁切中 ({current}/{total})"));

                var fileProgress = new Progress<string>(msg =>
                    progress?.Report((current, total, msg)));

                var result = await CropImageAsync(path, options, fileProgress, cancellationToken);
                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// 获取支持的图片格式
        /// </summary>
        public IEnumerable<string> GetSupportedFormats()
        {
            return SupportedFormats;
        }

        /// <summary>
        /// 验证文件是否为有效图片
        /// </summary>
        public bool IsValidImage(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return SupportedFormats.Contains(extension);
        }

        #region 私有辅助方法

        /// <summary>
        /// 根据 EXIF Orientation 修正图片方向
        /// </summary>
        private static void OrientImage(Image<Rgba32> image)
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
        /// 自动裁切白边
        /// </summary>
        private static Image<Rgba32> AutoCropWhitespace(Image<Rgba32> image, int whiteThreshold)
        {
            int minX = image.Width;
            int minY = image.Height;
            int maxX = 0;
            int maxY = 0;

            // 扫描图片找出非白色区域的边界
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    if (pixel.R < whiteThreshold || pixel.G < whiteThreshold || pixel.B < whiteThreshold)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            // 如果整张图都是白色，不裁切
            if (minX >= maxX || minY >= maxY)
            {
                return image.Clone(ctx => { });
            }

            // 计算裁切区域
            int width = maxX - minX + 1;
            int height = maxY - minY + 1;

            var croppedImage = image.Clone(ctx => ctx.Crop(new Rectangle(minX, minY, width, height)));
            return croppedImage;
        }

        /// <summary>
        /// 手动裁切到指定尺寸（居中裁切）
        /// </summary>
        private static Image<Rgba32> ManualCrop(Image<Rgba32> image, int targetWidth, int targetHeight)
        {
            int width = Math.Min(targetWidth, image.Width);
            int height = Math.Min(targetHeight, image.Height);
            int x = (image.Width - width) / 2;
            int y = (image.Height - height) / 2;

            var croppedImage = image.Clone(ctx => ctx.Crop(new Rectangle(x, y, width, height)));
            return croppedImage;
        }

        /// <summary>
        /// 保存图片（根据转换选项）
        /// </summary>
        private static async Task SaveImageAsync(
            Image<Rgba32> image,
            string outputPath,
            ProcessOptions options,
            CancellationToken cancellationToken)
        {
            switch (options.Mode)
            {
                case ConversionMode.ToJpg:
                    await image.SaveAsync(outputPath, new JpegEncoder { Quality = options.JpegQuality }, cancellationToken);
                    break;
                case ConversionMode.ToPng:
                    await image.SaveAsync(outputPath, new PngEncoder { CompressionLevel = (PngCompressionLevel)options.PngCompressionLevel }, cancellationToken);
                    break;
                case ConversionMode.ToWebP:
                    await image.SaveAsync(outputPath, new WebpEncoder { Quality = options.WebPQuality }, cancellationToken);
                    break;
                case ConversionMode.ToBmp:
                    await image.SaveAsync(outputPath, new BmpEncoder(), cancellationToken);
                    break;
            }
        }

        /// <summary>
        /// 保存裁切后的图片
        /// </summary>
        private static async Task SaveCroppedImageAsync(
            Image<Rgba32> image,
            string outputPath,
            CropOptions options,
            CancellationToken cancellationToken)
        {
            var format = options.OutputFormat.ToLowerInvariant();
            switch (format)
            {
                case "jpg":
                case "jpeg":
                    await image.SaveAsync(outputPath, new JpegEncoder { Quality = options.JpegQuality }, cancellationToken);
                    break;
                case "png":
                    await image.SaveAsync(outputPath, new PngEncoder(), cancellationToken);
                    break;
                case "webp":
                    await image.SaveAsync(outputPath, new WebpEncoder { Quality = options.JpegQuality }, cancellationToken);
                    break;
                default:
                    await image.SaveAsync(outputPath, new JpegEncoder { Quality = options.JpegQuality }, cancellationToken);
                    break;
            }
        }

        /// <summary>
        /// 获取输出文件路径
        /// </summary>
        private static string GetOutputPath(string inputPath, ProcessOptions options)
        {
            string directory = options.OutputDirectory;
            if (string.IsNullOrWhiteSpace(directory))
            {
                directory = Path.GetDirectoryName(inputPath)!;
            }

            if (options.UseSubdirectory)
            {
                directory = Path.Combine(directory, options.SubdirectoryName);
            }

            string fileName = Path.GetFileNameWithoutExtension(inputPath);
            if (options.KeepOriginalFileName)
            {
                fileName = $"{options.FileNamePrefix}{fileName}{options.FileNameSuffix}";
            }
            else
            {
                fileName = $"{options.FileNamePrefix}{DateTime.Now:yyyyMMddHHmmss}{options.FileNameSuffix}";
            }

            string extension = options.Mode switch
            {
                ConversionMode.ToJpg => ".jpg",
                ConversionMode.ToPng => ".png",
                ConversionMode.ToWebP => ".webp",
                ConversionMode.ToBmp => ".bmp",
                _ => ".jpg"
            };

            return Path.Combine(directory, fileName + extension);
        }

        /// <summary>
        /// 获取裁切输出文件路径
        /// </summary>
        private static string GetCropOutputPath(string inputPath, CropOptions options)
        {
            string directory = options.OutputDirectory;
            if (string.IsNullOrWhiteSpace(directory))
            {
                directory = Path.GetDirectoryName(inputPath)!;
            }

            if (options.UseSubdirectory)
            {
                directory = Path.Combine(directory, options.SubdirectoryName);
            }

            string fileName = Path.GetFileNameWithoutExtension(inputPath);
            string extension = $".{options.OutputFormat}";

            return Path.Combine(directory, fileName + "_cropped" + extension);
        }

        /// <summary>
        /// 格式化文件大小显示
        /// </summary>
        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        #endregion
    }
}
