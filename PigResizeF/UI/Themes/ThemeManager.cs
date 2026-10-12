namespace PigResizeF.UI.Themes;

/// <summary>
/// 主题管理器 (单例模式)
/// </summary>
public class ThemeManager
{
    private static ThemeManager? _instance;
    private static readonly object _lock = new();
    private ThemeBase _currentTheme;

    /// <summary>
    /// 获取主题管理器实例
    /// </summary>
    public static ThemeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new ThemeManager();
                }
            }
            return _instance;
        }
    }

    private ThemeManager()
    {
        _currentTheme = new LightTheme(); // 默认亮色主题
    }

    /// <summary>
    /// 当前主题
    /// </summary>
    public ThemeBase Current => _currentTheme;

    /// <summary>
    /// 当前是否为深色主题
    /// </summary>
    public bool IsDarkMode => _currentTheme is DarkTheme;

    /// <summary>
    /// 主题改变事件
    /// </summary>
    public event EventHandler? ThemeChanged;

    /// <summary>
    /// 切换到亮色主题
    /// </summary>
    public void ApplyLightTheme()
    {
        if (_currentTheme is LightTheme)
            return;

        _currentTheme = new LightTheme();
        OnThemeChanged();
    }

    /// <summary>
    /// 切换到深色主题
    /// </summary>
    public void ApplyDarkTheme()
    {
        if (_currentTheme is DarkTheme)
            return;

        _currentTheme = new DarkTheme();
        OnThemeChanged();
    }

    /// <summary>
    /// 切换主题
    /// </summary>
    public void ToggleTheme()
    {
        if (IsDarkMode)
            ApplyLightTheme();
        else
            ApplyDarkTheme();
    }

    /// <summary>
    /// 应用指定主题
    /// </summary>
    public void ApplyTheme(string themeName)
    {
        switch (themeName?.ToLowerInvariant())
        {
            case "light":
                ApplyLightTheme();
                break;
            case "dark":
                ApplyDarkTheme();
                break;
            default:
                throw new ArgumentException($"未知的主题名称: {themeName}");
        }
    }

    /// <summary>
    /// 获取当前主题名称
    /// </summary>
    public string GetCurrentThemeName()
    {
        return IsDarkMode ? "Dark" : "Light";
    }

    private void OnThemeChanged()
    {
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}
