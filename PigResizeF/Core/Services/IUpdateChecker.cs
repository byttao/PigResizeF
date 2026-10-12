namespace PigResizeF.Core.Services;

/// <summary>
/// 更新检查服务接口
/// </summary>
public interface IUpdateChecker
{
    /// <summary>
    /// 检查是否有新版本
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新信息，如果没有新版本则返回 null</returns>
    Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前版本号
    /// </summary>
    Version GetCurrentVersion();
}

/// <summary>
/// 更新信息模型
/// </summary>
public class UpdateInfo
{
    /// <summary>
    /// 最新版本号
    /// </summary>
    public Version Version { get; set; } = new Version(0, 0, 0);

    /// <summary>
    /// 版本标签 (如 v2.0.0)
    /// </summary>
    public string TagName { get; set; } = string.Empty;

    /// <summary>
    /// 发布页面 URL
    /// </summary>
    public string ReleaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// 发布说明
    /// </summary>
    public string? ReleaseNotes { get; set; }

    /// <summary>
    /// 发布时间
    /// </summary>
    public DateTime PublishedAt { get; set; }

    /// <summary>
    /// 是否为预发布版本
    /// </summary>
    public bool IsPrerelease { get; set; }
}
