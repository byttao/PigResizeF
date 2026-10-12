namespace PigResizeF.Core.Models;

/// <summary>
/// 图片转换任务模型
/// </summary>
public class ConvertTask
{
    /// <summary>
    /// 任务唯一标识符
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 源文件路径
    /// </summary>
    public string SourceFilePath { get; set; } = string.Empty;

    /// <summary>
    /// 目标文件路径
    /// </summary>
    public string? TargetFilePath { get; set; }

    /// <summary>
    /// 转换模式
    /// </summary>
    public Enums.ConvertMode ConvertMode { get; set; }

    /// <summary>
    /// 任务状态
    /// </summary>
    public TaskStatus Status { get; set; } = TaskStatus.Pending;

    /// <summary>
    /// 进度百分比 (0-100)
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 开始处理时间
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 处理耗时 (毫秒)
    /// </summary>
    public long? DurationMs => CompletedAt.HasValue && StartedAt.HasValue
        ? (long)(CompletedAt.Value - StartedAt.Value).TotalMilliseconds
        : null;
}

/// <summary>
/// 任务状态枚举
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// 等待处理
    /// </summary>
    Pending,

    /// <summary>
    /// 处理中
    /// </summary>
    Processing,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed,

    /// <summary>
    /// 失败
    /// </summary>
    Failed,

    /// <summary>
    /// 已取消
    /// </summary>
    Cancelled
}
