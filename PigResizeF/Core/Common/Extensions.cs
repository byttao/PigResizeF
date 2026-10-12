namespace PigResizeF.Core.Common;

/// <summary>
/// 扩展方法集合
/// </summary>
public static class Extensions
{
    /// <summary>
    /// 判断文件是否为支持的图片格式
    /// </summary>
    public static bool IsImageFile(this string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
        return Constants.SupportedImageExtensions.Contains(extension);
    }

    /// <summary>
    /// 获取友好的文件大小显示
    /// </summary>
    public static string ToFileSizeString(this long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// 获取友好的时间跨度显示
    /// </summary>
    public static string ToFriendlyString(this TimeSpan span)
    {
        if (span.TotalSeconds < 1)
            return $"{span.TotalMilliseconds:0}ms";
        if (span.TotalMinutes < 1)
            return $"{span.TotalSeconds:0.#}秒";
        if (span.TotalHours < 1)
            return $"{span.TotalMinutes:0}分{span.Seconds}秒";
        return $"{span.TotalHours:0}小时{span.Minutes}分";
    }

    /// <summary>
    /// 安全地创建目录
    /// </summary>
    public static bool EnsureDirectoryExists(this string directoryPath)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取转换模式的目标尺寸
    /// </summary>
    public static (int Width, int Height) GetTargetSize(this Enums.ConvertMode mode)
    {
        return mode switch
        {
            Enums.ConvertMode.Square1440 => (1440, 1440),
            Enums.ConvertMode.Tall1440 => (1440, 1920),
            Enums.ConvertMode.Square800 => (800, 800),
            Enums.ConvertMode.ProductLong => (1440, 2160),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }

    /// <summary>
    /// 获取转换模式的显示名称
    /// </summary>
    public static string GetDisplayName(this Enums.ConvertMode mode)
    {
        return mode switch
        {
            Enums.ConvertMode.Square1440 => "1440方图",
            Enums.ConvertMode.Tall1440 => "1440长图",
            Enums.ConvertMode.Square800 => "800方图",
            Enums.ConvertMode.ProductLong => "商品长图",
            _ => mode.ToString()
        };
    }

    /// <summary>
    /// 获取裁切模式的显示名称
    /// </summary>
    public static string GetDisplayName(this Enums.CropMode mode)
    {
        return mode switch
        {
            Enums.CropMode.Auto => "自动识别",
            Enums.CropMode.RandomCorners => "四角乱切",
            Enums.CropMode.Grid2x2 => "四宫格拆分",
            Enums.CropMode.Grid3x3 => "九宫格拆分",
            _ => mode.ToString()
        };
    }

    /// <summary>
    /// 限制数值在指定范围内
    /// </summary>
    public static T Clamp<T>(this T value, T min, T max) where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0) return min;
        if (value.CompareTo(max) > 0) return max;
        return value;
    }

    /// <summary>
    /// 安全地尝试删除文件
    /// </summary>
    public static bool TryDeleteFile(this string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}
