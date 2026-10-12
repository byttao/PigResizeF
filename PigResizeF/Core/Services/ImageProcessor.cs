using PigResizeF.Core.Models;
using PigResizeF.Core.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using ImageSharpImage = SixLabors.ImageSharp.Image;
using ImageSharpColor = SixLabors.ImageSharp.Color;
using ImageSharpPoint = SixLabors.ImageSharp.Point;
using ImageSharpRectangle = SixLabors.ImageSharp.Rectangle;

namespace PigResizeF.Core.Services;

/// <summary>
/// 图片处理服务实现
/// </summary>
public class ImageProcessor : IImageProcessor
{
    /// <summary>
    /// 转换单张图片
    /// </summary>
    public async Task<ProcessResult> ConvertImageAsync(ConvertTask task, CancellationToken cancellationToken = default)
    {
        try
        {
            task.Status = TaskStatus.Processing;
            task.StartedAt = DateTime.Now;

            await Task.Run(() =>
            {
                using var image = ImageSharpImage.Load<Rgba32>(task.SourceFilePath);

                // 修正 EXIF 方向
                OrientImage(image);

                // 获取目标尺寸
                var (width, height) = task.ConvertMode.GetTargetSize();

                // 等比缩放并白底补齐
                var resized = ResizeWithPadding(image, width, height);

                // 确定输出路径
                var outputPath = task.TargetFilePath ?? GenerateOutputPath(
                    task.SourceFilePath,
                    task.ConvertMode.GetDisplayName());

                // 保存
                resized.Save(outputPath, new JpegEncoder { Quality = Constants.DefaultJpegQuality });

                task.TargetFilePath = outputPath;
            }, cancellationToken);

            task.Status = TaskStatus.Completed;
            task.CompletedAt = DateTime.Now;
            task.Progress = 100;

            return ProcessResult.Success();
        }
        catch (Exception ex)
        {
            task.Status = TaskStatus.Failed;
            task.ErrorMessage = ex.Message;
            task.CompletedAt = DateTime.Now;
            return ProcessResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// 批量转换图片
    /// </summary>
    public async Task<List<ProcessResult>> ConvertImagesAsync(
        List<ConvertTask> tasks,
        IProgress<(int completed, int total)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ProcessResult>();
        var completed = 0;

        foreach (var task in tasks)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var result = await ConvertImageAsync(task, cancellationToken);
            results.Add(result);

            completed++;
            progress?.Report((completed, tasks.Count));
        }

        return results;
    }

    /// <summary>
    /// 裁切单张图片
    /// </summary>
    public async Task<ProcessResult> CropImageAsync(CropTask task, CancellationToken cancellationToken = default)
    {
        try
        {
            task.Status = TaskStatus.Processing;
            task.StartedAt = DateTime.Now;

            await Task.Run(() =>
            {
                using var image = ImageSharpImage.Load<Rgba32>(task.SourceFilePath);
                OrientImage(image);

                // 如果是自动模式，先检测
                var mode = task.CropMode;
                if (mode == Enums.CropMode.Auto)
                {
                    mode = DetectCropModeSync(image);
                }

                // 执行裁切
                var outputDir = task.OutputDirectory
                    ?? Path.GetDirectoryName(task.SourceFilePath)
                    ?? Environment.CurrentDirectory;

                outputDir.EnsureDirectoryExists();

                var outputCount = CropImage(image, task.SourceFilePath, outputDir, mode);
                task.OutputFileCount = outputCount;
            }, cancellationToken);

            task.Status = TaskStatus.Completed;
            task.CompletedAt = DateTime.Now;
            task.Progress = 100;

            return ProcessResult.Success();
        }
        catch (Exception ex)
        {
            task.Status = TaskStatus.Failed;
            task.ErrorMessage = ex.Message;
            task.CompletedAt = DateTime.Now;
            return ProcessResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// 批量裁切图片
    /// </summary>
    public async Task<List<ProcessResult>> CropImagesAsync(
        List<CropTask> tasks,
        IProgress<(int completed, int total)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ProcessResult>();
        var completed = 0;

        foreach (var task in tasks)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var result = await CropImageAsync(task, cancellationToken);
            results.Add(result);

            completed++;
            progress?.Report((completed, tasks.Count));
        }

        return results;
    }

    /// <summary>
    /// 自动检测裁切模式
    /// </summary>
    public async Task<Enums.CropMode> DetectCropModeAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            using var image = ImageSharpImage.Load<Rgba32>(filePath);
            OrientImage(image);
            return DetectCropModeSync(image);
        });
    }

    /// <summary>
    /// 处理京东详情页
    /// </summary>
    public async Task<ProcessResult> ProcessJdDetailAsync(
        JdDetailTask task,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // TODO: 实现京东详情页处理逻辑
        await Task.CompletedTask;
        return ProcessResult.Success();
    }

    #region 私有辅助方法

    /// <summary>
    /// 修正图片 EXIF 方向
    /// </summary>
    private static void OrientImage(ImageSharpImage<Rgba32> image)
    {
        if (image.Metadata.ExifProfile?.TryGetValue(ExifTag.Orientation, out IExifValue<ushort>? orientationValue) == true
            && orientationValue?.Value is ushort orientation)
        {
            image.Mutate(context =>
            {
                switch (orientation)
                {
                    case 2: context.Flip(FlipMode.Horizontal); break;
                    case 3: context.Rotate(RotateMode.Rotate180); break;
                    case 4: context.Flip(FlipMode.Vertical); break;
                    case 5: context.Flip(FlipMode.Vertical).Rotate(RotateMode.Rotate90); break;
                    case 6: context.Rotate(RotateMode.Rotate90); break;
                    case 7: context.Flip(FlipMode.Horizontal).Rotate(RotateMode.Rotate90); break;
                    case 8: context.Rotate(RotateMode.Rotate270); break;
                }
            });
        }
    }

    /// <summary>
    /// 等比缩放并白底补齐
    /// </summary>
    private static ImageSharpImage<Rgba32> ResizeWithPadding(ImageSharpImage<Rgba32> source, int targetWidth, int targetHeight)
    {
        var widthRatio = (float)targetWidth / source.Width;
        var heightRatio = (float)targetHeight / source.Height;
        var ratio = Math.Min(widthRatio, heightRatio);

        var newWidth = (int)(source.Width * ratio);
        var newHeight = (int)(source.Height * ratio);

        source.Mutate(x => x.Resize(newWidth, newHeight));

        var result = new ImageSharpImage<Rgba32>(targetWidth, targetHeight, ImageSharpColor.White);
        var x = (targetWidth - newWidth) / 2;
        var y = (targetHeight - newHeight) / 2;

        result.Mutate(ctx => ctx.DrawImage(source, new ImageSharpPoint(x, y), 1.0f));
        return result;
    }

    /// <summary>
    /// 生成输出文件路径
    /// </summary>
    private static string GenerateOutputPath(string sourcePath, string suffix)
    {
        var directory = Path.GetDirectoryName(sourcePath) ?? Environment.CurrentDirectory;
        var fileName = Path.GetFileNameWithoutExtension(sourcePath);
        return Path.Combine(directory, $"{fileName}_{suffix}.jpg");
    }

    /// <summary>
    /// 同步检测裁切模式
    /// </summary>
    private static Enums.CropMode DetectCropModeSync(ImageSharpImage<Rgba32> image)
    {
        // TODO: 实现分界线检测算法
        // 暂时返回四角乱切
        return Enums.CropMode.RandomCorners;
    }

    /// <summary>
    /// 执行裁切
    /// </summary>
    private static int CropImage(ImageSharpImage<Rgba32> image, string sourcePath, string outputDir, Enums.CropMode mode)
    {
        var originalName = Path.GetFileNameWithoutExtension(sourcePath);
        var encoder = new JpegEncoder { Quality = Constants.HighQualityJpeg };

        return mode switch
        {
            Enums.CropMode.RandomCorners => CropRandomCorners(image, originalName, outputDir, encoder),
            Enums.CropMode.Grid2x2 => CropGrid(image, originalName, outputDir, encoder, 2),
            Enums.CropMode.Grid3x3 => CropGrid(image, originalName, outputDir, encoder, 3),
            _ => 0
        };
    }

    /// <summary>
    /// 四角乱切
    /// </summary>
    private static int CropRandomCorners(ImageSharpImage<Rgba32> image, string name, string outputDir, JpegEncoder encoder)
    {
        var maxWidth = (int)(image.Width * 0.85);
        var maxHeight = (int)(image.Height * 0.85);
        var cropWidth = Math.Min(maxWidth, (int)(maxHeight * 0.75));
        var cropHeight = Math.Min(maxHeight, (int)(cropWidth * 1.33));

        var positions = new[]
        {
            new ImageSharpPoint(0, 0),
            new ImageSharpPoint(image.Width - cropWidth, 0),
            new ImageSharpPoint(0, image.Height - cropHeight),
            new ImageSharpPoint(image.Width - cropWidth, image.Height - cropHeight)
        };

        for (var i = 0; i < positions.Length; i++)
        {
            using var cropped = image.Clone(ctx => ctx
                .Crop(new ImageSharpRectangle(positions[i].X, positions[i].Y, cropWidth, cropHeight))
                .Resize(Constants.RandomCropWidth, Constants.RandomCropHeight));

            cropped.Save(Path.Combine(outputDir, $"{name}-乱切{i + 1}.jpg"), encoder);
        }

        return positions.Length;
    }

    /// <summary>
    /// 宫格拆分
    /// </summary>
    private static int CropGrid(ImageSharpImage<Rgba32> image, string name, string outputDir, JpegEncoder encoder, int gridSize)
    {
        var index = 1;
        for (var row = 0; row < gridSize; row++)
        {
            var top = row * image.Height / gridSize;
            var bottom = (row + 1) * image.Height / gridSize;

            for (var col = 0; col < gridSize; col++)
            {
                var left = col * image.Width / gridSize;
                var right = (col + 1) * image.Width / gridSize;

                using var cropped = image.Clone(ctx => ctx.Crop(
                    new ImageSharpRectangle(left, top, right - left, bottom - top)));

                cropped.Save(Path.Combine(outputDir, $"{name}-拆分{index++}.jpg"), encoder);
            }
        }

        return gridSize * gridSize;
    }

    #endregion
}
