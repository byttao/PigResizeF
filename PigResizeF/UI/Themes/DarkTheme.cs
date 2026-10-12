using System.Drawing;

namespace PigResizeF.UI.Themes;

/// <summary>
/// 深色主题
/// </summary>
public class DarkTheme : ThemeBase
{
    // 背景色 - 深色层级，避免纯黑
    public override Color BackgroundPrimary => Color.FromArgb(15, 19, 28); // #0F131C 最深
    public override Color BackgroundSecondary => Color.FromArgb(22, 29, 43); // #161D2B
    public override Color BackgroundTertiary => Color.FromArgb(30, 38, 54); // #1E2636
    public override Color Surface => Color.FromArgb(30, 38, 54); // 卡片表面
    public override Color SurfaceHover => Color.FromArgb(39, 51, 73); // 悬停表面

    // 文本色 - 浅色但不刺眼
    public override Color TextPrimary => Color.FromArgb(241, 245, 249); // #F1F5F9 主文字
    public override Color TextSecondary => Color.FromArgb(203, 213, 225); // #CBD5E1 次要文字
    public override Color TextTertiary => Color.FromArgb(148, 163, 184); // #94A3B8 辅助文字
    public override Color TextDisabled => Color.FromArgb(100, 116, 139); // 禁用文字

    // 品牌色 - 天蓝色系（深色模式下稍微调亮）
    public override Color Primary => Color.FromArgb(56, 189, 248); // #38BDF8
    public override Color PrimaryHover => Color.FromArgb(125, 211, 252); // 悬停更亮
    public override Color PrimaryPressed => Color.FromArgb(14, 165, 233); // 按下
    public override Color PrimaryLight => Color.FromArgb(7, 89, 133); // 深色模式下的浅蓝背景

    // 功能色
    public override Color Success => Color.FromArgb(110, 231, 183); // #6EE7B7 翡翠绿
    public override Color Warning => Color.FromArgb(252, 211, 77); // #FCD34D 琥珀色
    public override Color Error => Color.FromArgb(248, 113, 113); // #F87171 珊瑚红
    public override Color Info => Color.FromArgb(96, 165, 250); // 信息蓝

    // 边框色
    public override Color BorderPrimary => Color.FromArgb(51, 65, 85); // #334155
    public override Color BorderSecondary => Color.FromArgb(39, 51, 73);
    public override Color BorderFocus => Color.FromArgb(56, 189, 248); // 聚焦用主色

    // 阴影
    public override Color ShadowColor => Color.FromArgb(0, 0, 0); // 纯黑阴影
}
