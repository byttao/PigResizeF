namespace PigResizeF.Core.Enums;

/// <summary>
/// 图片裁切模式枚举
/// </summary>
public enum CropMode
{
    /// <summary>
    /// 自动识别 (根据分界线检测判断为单图、四宫格或九宫格)
    /// </summary>
    Auto,

    /// <summary>
    /// 四角乱切 (从四个角落各裁切一张 3:4 比例图片)
    /// </summary>
    RandomCorners,

    /// <summary>
    /// 四宫格拆分 (2x2 均匀分割)
    /// </summary>
    Grid2x2,

    /// <summary>
    /// 九宫格拆分 (3x3 均匀分割)
    /// </summary>
    Grid3x3
}
