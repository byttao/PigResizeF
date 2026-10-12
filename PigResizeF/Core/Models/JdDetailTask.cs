namespace PigResizeF.Core.Models;

/// <summary>
/// 京东详情页处理任务模型
/// </summary>
public class JdDetailTask
{
    /// <summary>
    /// 任务唯一标识符
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 源文件路径列表 (按顺序拼接)
    /// </summary>
    public List<string> SourceFilePaths { get; set; } = new();

    /// <summary>
    /// 输出目录
    /// </summary>
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 任务状态
    /// </summary>
    public TaskStatus Status { get; set; } = TaskStatus.Pending;

    /// <summary>
    /// 进度百分比 (0-100)
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// 输出文件数量
    /// </summary>
    public int OutputFileCount { get; set; }

    /// <summary>
    /// 输出文件路径列表
    /// </summary>
    public List<string> OutputFiles { get; set; } = new();

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
