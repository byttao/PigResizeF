using System.Drawing;

namespace PigResizeF.UI.Themes;

/// <summary>
/// 主题基类
/// </summary>
public abstract class ThemeBase
{
    // 背景色
    public abstract Color BackgroundPrimary { get; }
    public abstract Color BackgroundSecondary { get; }
    public abstract Color BackgroundTertiary { get; }
    public abstract Color Surface { get; }
    public abstract Color SurfaceHover { get; }

    // 文本色
    public abstract Color TextPrimary { get; }
    public abstract Color TextSecondary { get; }
    public abstract Color TextTertiary { get; }
    public abstract Color TextDisabled { get; }

    // 品牌色
    public abstract Color Primary { get; }
    public abstract Color PrimaryHover { get; }
    public abstract Color PrimaryPressed { get; }
    public abstract Color PrimaryLight { get; }

    // 功能色
    public abstract Color Success { get; }
    public abstract Color Warning { get; }
    public abstract Color Error { get; }
    public abstract Color Info { get; }

    // 边框色
    public abstract Color BorderPrimary { get; }
    public abstract Color BorderSecondary { get; }
    public abstract Color BorderFocus { get; }

    // 阴影
    public abstract Color ShadowColor { get; }

    // 圆角大小
    public virtual int BorderRadiusSmall => 8;
    public virtual int BorderRadiusMedium => 16;
    public virtual int BorderRadiusLarge => 24;
    public virtual int BorderRadiusPill => 999;

    // 间距
    public virtual int SpacingXs => 4;
    public virtual int SpacingS => 8;
    public virtual int SpacingM => 12;
    public virtual int SpacingL => 16;
    public virtual int SpacingXl => 24;
    public virtual int SpacingXxl => 32;
    public virtual int Spacing3xl => 48;

    // 字体大小
    public virtual float FontSizeXs => 11f;
    public virtual float FontSizeS => 12f;
    public virtual float FontSizeM => 13f;
    public virtual float FontSizeL => 15f;
    public virtual float FontSizeXl => 16f;
    public virtual float FontSizeXxl => 20f;
    public virtual float FontSize3xl => 24f;

    // 字体
    public virtual string FontFamily => "Microsoft YaHei UI";

    /// <summary>
    /// 创建阴影画刷
    /// </summary>
    public virtual SolidBrush CreateShadowBrush(byte alpha = 38)
    {
        return new SolidBrush(Color.FromArgb(alpha, ShadowColor));
    }

    /// <summary>
    /// 混合两个颜色
    /// </summary>
    public static Color BlendColors(Color color1, Color color2, float ratio)
    {
        ratio = Math.Clamp(ratio, 0f, 1f);
        return Color.FromArgb(
            (int)(color1.R * (1 - ratio) + color2.R * ratio),
            (int)(color1.G * (1 - ratio) + color2.G * ratio),
            (int)(color1.B * (1 - ratio) + color2.B * ratio)
        );
    }

    /// <summary>
    /// 调整颜色亮度
    /// </summary>
    public static Color AdjustBrightness(Color color, float factor)
    {
        return Color.FromArgb(
            color.A,
            (int)Math.Clamp(color.R * factor, 0, 255),
            (int)Math.Clamp(color.G * factor, 0, 255),
            (int)Math.Clamp(color.B * factor, 0, 255)
        );
    }
}
