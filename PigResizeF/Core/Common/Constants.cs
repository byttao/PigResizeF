namespace PigResizeF.Core.Common;

/// <summary>
/// 全局常量定义
/// </summary>
public static class Constants
{
    /// <summary>
    /// 应用程序名称
    /// </summary>
    public const string AppName = "PigResizeF";

    /// <summary>
    /// 应用程序显示名称
    /// </summary>
    public const string AppDisplayName = "图片批量裁切工具";

    /// <summary>
    /// 当前版本号
    /// </summary>
    public const string Version = "2.0.0";

    /// <summary>
    /// GitHub 仓库地址
    /// </summary>
    public const string GitHubRepo = "https://github.com/byttao/PigResizeF";

    /// <summary>
    /// GitHub API 最新版本检查地址
    /// </summary>
    public const string GitHubApiLatestRelease = "https://api.github.com/repos/byttao/PigResizeF/releases/latest";

    /// <summary>
    /// 支持的图片格式扩展名
    /// </summary>
    public static readonly string[] SupportedImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };

    /// <summary>
    /// 京东详情页输出宽度 (像素)
    /// </summary>
    public const int JdOutputWidth = 790;

    /// <summary>
    /// 京东详情页每段高度 (像素)
    /// </summary>
    public const int JdSegmentHeight = 1500;

    /// <summary>
    /// JPEG 默认输出质量 (1-100)
    /// </summary>
    public const int DefaultJpegQuality = 90;

    /// <summary>
    /// JPEG 高质量输出 (用于京东详情页等场景)
    /// </summary>
    public const int HighQualityJpeg = 100;

    /// <summary>
    /// 四角乱切输出图片尺寸
    /// </summary>
    public const int RandomCropWidth = 1440;
    public const int RandomCropHeight = 1920;

    /// <summary>
    /// 配置文件名
    /// </summary>
    public const string ConfigFileName = "appsettings.json";

    /// <summary>
    /// 历史记录最大保存数量
    /// </summary>
    public const int MaxHistoryCount = 50;
}
