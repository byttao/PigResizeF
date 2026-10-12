using PigResizeF.Core.Models;

namespace PigResizeF.Core.Services;

/// <summary>
/// 图片处理服务接口
/// </summary>
public interface IImageProcessor
{
    /// <summary>
    /// 转换单张图片
    /// </summary>
    /// <param name="task">转换任务</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理结果</returns>
    Task<ProcessResult> ConvertImageAsync(ConvertTask task, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量转换图片
    /// </summary>
    /// <param name="tasks">转换任务列表</param>
    /// <param name="progress">进度回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理结果列表</returns>
    Task<List<ProcessResult>> ConvertImagesAsync(
        List<ConvertTask> tasks,
        IProgress<(int completed, int total)>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 裁切单张图片
    /// </summary>
    /// <param name="task">裁切任务</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理结果</returns>
    Task<ProcessResult> CropImageAsync(CropTask task, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量裁切图片
    /// </summary>
    /// <param name="tasks">裁切任务列表</param>
    /// <param name="progress">进度回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理结果列表</returns>
    Task<List<ProcessResult>> CropImagesAsync(
        List<CropTask> tasks,
        IProgress<(int completed, int total)>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 自动检测裁切模式
    /// </summary>
    /// <param name="filePath">图片文件路径</param>
    /// <returns>检测到的裁切模式</returns>
    Task<Enums.CropMode> DetectCropModeAsync(string filePath);

    /// <summary>
    /// 处理京东详情页
    /// </summary>
    /// <param name="task">京东详情页任务</param>
    /// <param name="progress">进度回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理结果</returns>
    Task<ProcessResult> ProcessJdDetailAsync(
        JdDetailTask task,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
}
