namespace PigResizeF.Core.Models
{
    /// <summary>
    /// 转换模式
    /// </summary>
    public enum ConversionMode
    {
        /// <summary>
        /// 转为 JPG 格式
        /// </summary>
        ToJpg,

        /// <summary>
        /// 转为 PNG 格式
        /// </summary>
        ToPng,

        /// <summary>
        /// 转为 WebP 格式
        /// </summary>
        ToWebP,

        /// <summary>
        /// 转为 BMP 格式
        /// </summary>
        ToBmp
    }

    /// <summary>
    /// 图片处理选项
    /// </summary>
    public class ProcessOptions
    {
        /// <summary>
        /// 转换模式
        /// </summary>
        public ConversionMode Mode { get; set; } = ConversionMode.ToJpg;

        /// <summary>
        /// 目标宽度（0 表示保持原始宽度）
        /// </summary>
        public int TargetWidth { get; set; } = 0;

        /// <summary>
        /// 目标高度（0 表示保持原始高度）
        /// </summary>
        public int TargetHeight { get; set; } = 0;

        /// <summary>
        /// JPEG 质量 (1-100)
        /// </summary>
        public int JpegQuality { get; set; } = 85;

        /// <summary>
        /// PNG 压缩级别 (0-9)
        /// </summary>
        public int PngCompressionLevel { get; set; } = 6;

        /// <summary>
        /// WebP 质量 (1-100)
        /// </summary>
        public int WebPQuality { get; set; } = 85;

        /// <summary>
        /// 输出目录（为空则使用原文件目录）
        /// </summary>
        public string OutputDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 是否使用子目录
        /// </summary>
        public bool UseSubdirectory { get; set; } = false;

        /// <summary>
        /// 子目录名称
        /// </summary>
        public string SubdirectoryName { get; set; } = "output";

        /// <summary>
        /// 是否保留原始文件名
        /// </summary>
        public bool KeepOriginalFileName { get; set; } = true;

        /// <summary>
        /// 文件名前缀
        /// </summary>
        public string FileNamePrefix { get; set; } = string.Empty;

        /// <summary>
        /// 文件名后缀
        /// </summary>
        public string FileNameSuffix { get; set; } = string.Empty;

        /// <summary>
        /// 是否调整尺寸
        /// </summary>
        public bool ResizeImage { get; set; } = false;

        /// <summary>
        /// 是否保持宽高比
        /// </summary>
        public bool MaintainAspectRatio { get; set; } = true;

        /// <summary>
        /// 是否填充背景（保持宽高比时）
        /// </summary>
        public bool PadWithBackground { get; set; } = true;

        /// <summary>
        /// 背景颜色（ARGB 格式）
        /// </summary>
        public uint BackgroundColor { get; set; } = 0xFFFFFFFF; // 白色

        /// <summary>
        /// 是否覆盖已存在的文件
        /// </summary>
        public bool OverwriteExisting { get; set; } = false;
    }
}
