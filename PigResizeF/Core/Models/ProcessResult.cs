using System;

namespace PigResizeF.Core.Models
{
    /// <summary>
    /// 处理结果
    /// </summary>
    public class ProcessResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// 输出文件路径
        /// </summary>
        public string OutputPath { get; set; } = string.Empty;

        /// <summary>
        /// 处理耗时
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// 原始文件大小 (字节)
        /// </summary>
        public long OriginalSize { get; set; }

        /// <summary>
        /// 输出文件大小 (字节)
        /// </summary>
        public long OutputSize { get; set; }

        /// <summary>
        /// 压缩率 (百分比)
        /// </summary>
        public double CompressionRatio => OriginalSize > 0
            ? (1 - (double)OutputSize / OriginalSize) * 100
            : 0;
    }
}
