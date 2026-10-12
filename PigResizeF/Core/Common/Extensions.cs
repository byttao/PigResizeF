using System;
using System.IO;

namespace PigResizeF.Core.Common
{
    /// <summary>
    /// 扩展方法
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// 格式化文件大小
        /// </summary>
        /// <param name="bytes">字节数</param>
        /// <returns>格式化后的字符串</returns>
        public static string FormatFileSize(this long bytes)
        {
            if (bytes == 0) return "0 B";

            double size = bytes;
            int order = 0;

            while (size >= 1024 && order < Constants.FileSizeUnits.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {Constants.FileSizeUnits[order]}";
        }

        /// <summary>
        /// 检查文件是否为支持的图片格式
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否为支持的图片</returns>
        public static bool IsSupportedImageFormat(this string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return Array.Exists(Constants.SupportedImageFormats, ext => ext == extension);
        }

        /// <summary>
        /// 安全获取文件名（处理长路径）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件名</returns>
        public static string SafeGetFileName(this string filePath)
        {
            try
            {
                return Path.GetFileName(filePath);
            }
            catch
            {
                return filePath;
            }
        }

        /// <summary>
        /// 确保目录存在
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        public static void EnsureDirectoryExists(this string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        /// <summary>
        /// 格式化时间跨度
        /// </summary>
        /// <param name="timeSpan">时间跨度</param>
        /// <returns>格式化后的字符串</returns>
        public static string FormatDuration(this TimeSpan timeSpan)
        {
            if (timeSpan.TotalSeconds < 1)
                return $"{timeSpan.TotalMilliseconds:0}ms";
            if (timeSpan.TotalMinutes < 1)
                return $"{timeSpan.TotalSeconds:0.0}s";
            if (timeSpan.TotalHours < 1)
                return $"{timeSpan.TotalMinutes:0.0}m";
            return $"{timeSpan.TotalHours:0.0}h";
        }

        /// <summary>
        /// 生成唯一文件名（如果文件已存在）
        /// </summary>
        /// <param name="filePath">原始文件路径</param>
        /// <returns>唯一的文件路径</returns>
        public static string GetUniqueFilePath(this string filePath)
        {
            if (!File.Exists(filePath))
                return filePath;

            var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);
            int counter = 1;

            string newFilePath;
            do
            {
                newFilePath = Path.Combine(directory, $"{fileNameWithoutExtension}_{counter}{extension}");
                counter++;
            }
            while (File.Exists(newFilePath));

            return newFilePath;
        }

        /// <summary>
        /// 截断字符串到指定长度
        /// </summary>
        /// <param name="value">原始字符串</param>
        /// <param name="maxLength">最大长度</param>
        /// <param name="suffix">截断后的后缀</param>
        /// <returns>截断后的字符串</returns>
        public static string Truncate(this string value, int maxLength, string suffix = "...")
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;

            return value.Substring(0, maxLength - suffix.Length) + suffix;
        }
    }
}
