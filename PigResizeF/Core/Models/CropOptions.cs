namespace PigResizeF.Core.Models
{
    /// <summary>
    /// 裁切模式
    /// </summary>
    public enum CropMode
    {
        /// <summary>
        /// 自动裁切白边
        /// </summary>
        Auto,

        /// <summary>
        /// 手动指定尺寸
        /// </summary>
        Manual,

        /// <summary>
        /// 九宫格裁切
        /// </summary>
        Grid,

        /// <summary>
        /// 随机裁切
        /// </summary>
        Random
    }

    /// <summary>
    /// 裁切选项
    /// </summary>
    public class CropOptions
    {
        /// <summary>
        /// 裁切模式
        /// </summary>
        public CropMode Mode { get; set; } = CropMode.Auto;

        /// <summary>
        /// 目标宽度（手动模式）
        /// </summary>
        public int TargetWidth { get; set; } = 800;

        /// <summary>
        /// 目标高度（手动模式）
        /// </summary>
        public int TargetHeight { get; set; } = 800;

        /// <summary>
        /// 白色阈值（自动模式，0-255）
        /// </summary>
        public int WhiteThreshold { get; set; } = 250;

        /// <summary>
        /// 网格行数（九宫格模式）
        /// </summary>
        public int GridRows { get; set; } = 3;

        /// <summary>
        /// 网格列数（九宫格模式）
        /// </summary>
        public int GridColumns { get; set; } = 3;

        /// <summary>
        /// 输出格式
        /// </summary>
        public string OutputFormat { get; set; } = "jpg";

        /// <summary>
        /// JPEG 质量
        /// </summary>
        public int JpegQuality { get; set; } = 90;

        /// <summary>
        /// 输出目录
        /// </summary>
        public string OutputDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 是否使用子目录
        /// </summary>
        public bool UseSubdirectory { get; set; } = true;

        /// <summary>
        /// 子目录名称
        /// </summary>
        public string SubdirectoryName { get; set; } = "cropped";

        /// <summary>
        /// 是否快速模式（跳过预览）
        /// </summary>
        public bool QuickMode { get; set; } = false;

        /// <summary>
        /// 是否覆盖已存在的文件
        /// </summary>
        public bool OverwriteExisting { get; set; } = false;
    }
}
