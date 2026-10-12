using System.Drawing;

namespace PigResizeF.UI.Themes;

/// <summary>
/// 亮色主题
/// </summary>
public class LightTheme : ThemeBase
{
    // 背景色 - 浅灰白色调
    public override Color BackgroundPrimary => Color.FromArgb(255, 255, 255); // 纯白
    public override Color BackgroundSecondary => Color.FromArgb(248, 250, 252); // 极浅灰
    public override Color BackgroundTertiary => Color.FromArgb(241, 245, 249); // 浅灰
    public override Color Surface => Color.FromArgb(255, 255, 255); // 卡片白色
    public override Color SurfaceHover => Color.FromArgb(248, 250, 252);

    // 文本色
    public override Color TextPrimary => Color.FromArgb(15, 23, 42); // 深灰黑
    public override Color TextSecondary => Color.FromArgb(71, 85, 105); // 中灰
    public override Color TextTertiary => Color.FromArgb(148, 163, 184); // 浅灰
    public override Color TextDisabled => Color.FromArgb(203, 213, 225); // 禁用灰

    // 品牌色 - 天蓝色系
    public override Color Primary => Color.FromArgb(56, 189, 248); // #38BDF8
    public override Color PrimaryHover => Color.FromArgb(14, 165, 233); // 悬停深蓝
    public override Color PrimaryPressed => Color.FromArgb(2, 132, 199); // 按下更深
    public override Color PrimaryLight => Color.FromArgb(224, 242, 254); // 浅蓝背景

    // 功能色
    public override Color Success => Color.FromArgb(110, 231, 183); // #6EE7B7 翡翠绿
    public override Color Warning => Color.FromArgb(252, 211, 77); // #FCD34D 琥珀色
    public override Color Error => Color.FromArgb(248, 113, 113); // #F87171 珊瑚红
    public override Color Info => Color.FromArgb(147, 197, 253); // 信息蓝

    // 边框色
    public override Color BorderPrimary => Color.FromArgb(226, 232, 240); // #E2E8F0
    public override Color BorderSecondary => Color.FromArgb(241, 245, 249);
    public override Color BorderFocus => Color.FromArgb(56, 189, 248); // 聚焦用主色

    // 阴影
    public override Color ShadowColor => Color.FromArgb(15, 23, 42); // 深色阴影
}
