using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Util;
using PigResizeF.Core.Services;
using PigResizeF.UI.Presenters;

namespace PigResizeF.UI.Forms;

/// <summary>
/// 主窗体 - 使用 ReaLTaiizor Material Theme
/// </summary>
public partial class MainForm : MaterialForm
{
    private readonly IImageProcessor _imageProcessor;
    private readonly IConfigService _configService;
    private readonly IUpdateChecker _updateChecker;
    private readonly MainPresenter _presenter;

    // Material 主题管理器
    private readonly MaterialSkinManager _materialSkinManager;

    public MainForm(
        IImageProcessor imageProcessor,
        IConfigService configService,
        IUpdateChecker updateChecker)
    {
        _imageProcessor = imageProcessor ?? throw new ArgumentNullException(nameof(imageProcessor));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _updateChecker = updateChecker ?? throw new ArgumentNullException(nameof(updateChecker));

        // 初始化 Material 主题
        _materialSkinManager = MaterialSkinManager.Instance;
        _materialSkinManager.AddFormToManage(this);
        _materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;

        // 设置主题色板 - 天蓝色系
        _materialSkinManager.ColorScheme = new ColorScheme(
            Primary.Blue500,      // 主色 #2196F3
            Primary.Blue700,      // 深主色 #1976D2
            Primary.Blue200,      // 浅主色 #90CAF9
            Accent.Cyan200,       // 强调色 #80DEEA
            TextShade.WHITE       // 文字色调
        );

        InitializeComponent();
        InitializeCustomComponents();

        _presenter = new MainPresenter(this, imageProcessor, configService, updateChecker);

        // 加载配置
        LoadConfiguration();
    }

    /// <summary>
    /// 初始化自定义组件
    /// </summary>
    private void InitializeCustomComponents()
    {
        // 设置窗体属性
        Text = $"PigResizeF v2.0 - 图片批量裁切工具";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1200, 900);
        Size = new Size(1400, 960);

        // 启用双缓冲减少闪烁
        SetStyle(ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint, true);
        UpdateStyles();
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    private void LoadConfiguration()
    {
        try
        {
            // 加载主题设置
            var theme = _configService.GetValue<string>("App:Theme", "Light");
            ApplyTheme(theme);

            // 恢复窗口状态
            var windowState = _configService.GetValue<Core.Services.WindowState?>("App:Window");
            if (windowState != null)
            {
                if (windowState.Maximized)
                {
                    WindowState = FormWindowState.Maximized;
                }
                else
                {
                    Location = new Point(windowState.X, windowState.Y);
                    Size = new Size(windowState.Width, windowState.Height);
                }
            }
        }
        catch (Exception ex)
        {
            // 配置加载失败不影响启动
            System.Diagnostics.Debug.WriteLine($"加载配置失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 应用主题
    /// </summary>
    public void ApplyTheme(string themeName)
    {
        _materialSkinManager.Theme = themeName?.ToLowerInvariant() switch
        {
            "dark" => MaterialSkinManager.Themes.DARK,
            "light" => MaterialSkinManager.Themes.LIGHT,
            _ => MaterialSkinManager.Themes.LIGHT
        };
    }

    /// <summary>
    /// 切换主题
    /// </summary>
    public void ToggleTheme()
    {
        var newTheme = _materialSkinManager.Theme == MaterialSkinManager.Themes.LIGHT
            ? MaterialSkinManager.Themes.DARK
            : MaterialSkinManager.Themes.LIGHT;

        _materialSkinManager.Theme = newTheme;

        // 保存主题设置
        _configService.SetValue("App:Theme", newTheme == MaterialSkinManager.Themes.DARK ? "Dark" : "Light");
        _ = _configService.SaveAsync();
    }

    /// <summary>
    /// 窗体关闭时保存状态
    /// </summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);

        try
        {
            // 保存窗口状态
            var windowState = new Core.Services.WindowState
            {
                X = Location.X,
                Y = Location.Y,
                Width = Size.Width,
                Height = Size.Height,
                Maximized = WindowState == FormWindowState.Maximized
            };

            _configService.SetValue("App:Window", windowState);
            _ = _configService.SaveAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存配置失败: {ex.Message}");
        }
    }
}
