namespace PigResizeF.Core.Common
{
    /// <summary>
    /// 应用程序常量
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// 应用程序名称
        /// </summary>
        public const string AppName = "PigResizeF";

        /// <summary>
        /// 应用程序版本
        /// </summary>
        public const string AppVersion = "1.6.0";

        /// <summary>
        /// GitHub 仓库 URL
        /// </summary>
        public const string GithubRepoUrl = "https://github.com/byttao/PigResizeF";

        /// <summary>
        /// 代理地址
        /// </summary>
        public const string ProxyAddress = "http://192.168.10.8:1188";

        /// <summary>
        /// 支持的图片格式
        /// </summary>
        public static readonly string[] SupportedImageFormats = { ".jpg", ".jpeg", ".png", ".bmp", ".webp", ".gif" };

        /// <summary>
        /// 默认 JPEG 质量
        /// </summary>
        public const int DefaultJpegQuality = 85;

        /// <summary>
        /// 默认 PNG 压缩级别
        /// </summary>
        public const int DefaultPngCompressionLevel = 6;

        /// <summary>
        /// 默认 WebP 质量
        /// </summary>
        public const int DefaultWebPQuality = 85;

        /// <summary>
        /// 默认白色阈值（用于裁切）
        /// </summary>
        public const int DefaultWhiteThreshold = 250;

        /// <summary>
        /// 默认输出子目录名称
        /// </summary>
        public const string DefaultOutputSubdirectory = "output";

        /// <summary>
        /// 裁切输出子目录名称
        /// </summary>
        public const string CropOutputSubdirectory = "cropped";

        /// <summary>
        /// 文件大小单位
        /// </summary>
        public static readonly string[] FileSizeUnits = { "B", "KB", "MB", "GB", "TB" };
    }
}
