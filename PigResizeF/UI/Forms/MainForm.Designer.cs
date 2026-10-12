using ReaLTaiizor.Controls;
using ReaLTaiizor.Enum.Material;

namespace PigResizeF.UI.Forms;

/// <summary>
/// 主窗体设计器部分
/// </summary>
partial class MainForm
{
    private MaterialTabControl tabControl;
    private TabPage tabConvert;
    private TabPage tabCrop;
    private TabPage tabJdDetail;

    // 顶部工具栏
    private Panel topPanel;
    private MaterialButton btnThemeToggle;
    private MaterialButton btnSettings;
    private MaterialButton btnAbout;
    private Label lblVersion;

    /// <summary>
    /// 必需的设计器变量
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// 清理所有正在使用的资源
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows 窗体设计器生成的代码

    /// <summary>
    /// 设计器支持所需的方法 - 不要使用代码编辑器修改此方法的内容
    /// </summary>
    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();

        // 顶部面板
        this.topPanel = new Panel();
        this.btnThemeToggle = new MaterialButton();
        this.btnSettings = new MaterialButton();
        this.btnAbout = new MaterialButton();
        this.lblVersion = new Label();

        // Tab 控件
        this.tabControl = new MaterialTabControl();
        this.tabConvert = new TabPage();
        this.tabCrop = new TabPage();
        this.tabJdDetail = new TabPage();

        this.SuspendLayout();

        //
        // topPanel
        //
        this.topPanel.Dock = DockStyle.Top;
        this.topPanel.Height = 64;
        this.topPanel.Padding = new Padding(16, 8, 16, 8);
        this.topPanel.Controls.Add(this.lblVersion);
        this.topPanel.Controls.Add(this.btnAbout);
        this.topPanel.Controls.Add(this.btnSettings);
        this.topPanel.Controls.Add(this.btnThemeToggle);

        //
        // lblVersion
        //
        this.lblVersion.Dock = DockStyle.Left;
        this.lblVersion.Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold);
        this.lblVersion.Text = "PigResizeF v2.0";
        this.lblVersion.TextAlign = ContentAlignment.MiddleLeft;
        this.lblVersion.AutoSize = true;
        this.lblVersion.Padding = new Padding(8, 0, 0, 0);

        //
        // btnThemeToggle
        //
        this.btnThemeToggle.Dock = DockStyle.Right;
        this.btnThemeToggle.AutoSize = false;
        this.btnThemeToggle.Size = new Size(48, 48);
        this.btnThemeToggle.Type = MaterialButtonType.Text;
        this.btnThemeToggle.Icon = Properties.Resources.IconTheme; // 需要添加图标资源
        this.btnThemeToggle.ToolTipText = "切换主题";
        this.btnThemeToggle.Click += (s, e) => ToggleTheme();

        //
        // btnSettings
        //
        this.btnSettings.Dock = DockStyle.Right;
        this.btnSettings.AutoSize = false;
        this.btnSettings.Size = new Size(48, 48);
        this.btnSettings.Type = MaterialButtonType.Text;
        this.btnSettings.Text = "⚙";
        this.btnSettings.ToolTipText = "设置";

        //
        // btnAbout
        //
        this.btnAbout.Dock = DockStyle.Right;
        this.btnAbout.AutoSize = false;
        this.btnAbout.Size = new Size(48, 48);
        this.btnAbout.Type = MaterialButtonType.Text;
        this.btnAbout.Text = "ℹ";
        this.btnAbout.ToolTipText = "关于";

        //
        // tabControl
        //
        this.tabControl.Dock = DockStyle.Fill;
        this.tabControl.Controls.Add(this.tabConvert);
        this.tabControl.Controls.Add(this.tabCrop);
        this.tabControl.Controls.Add(this.tabJdDetail);

        //
        // tabConvert
        //
        this.tabConvert.Text = "图片转换";
        this.tabConvert.UseVisualStyleBackColor = true;

        //
        // tabCrop
        //
        this.tabCrop.Text = "图片裁切";
        this.tabCrop.UseVisualStyleBackColor = true;

        //
        // tabJdDetail
        //
        this.tabJdDetail.Text = "京东详情页裁切";
        this.tabJdDetail.UseVisualStyleBackColor = true;

        //
        // MainForm
        //
        this.AutoScaleDimensions = new SizeF(96F, 96F);
        this.AutoScaleMode = AutoScaleMode.Dpi;
        this.ClientSize = new Size(1400, 960);
        this.Controls.Add(this.tabControl);
        this.Controls.Add(this.topPanel);
        this.Name = "MainForm";

        this.ResumeLayout(false);
    }

    #endregion
}
