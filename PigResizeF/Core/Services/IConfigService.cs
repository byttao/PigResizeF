namespace PigResizeF.Core.Services;

/// <summary>
/// 配置服务接口
/// </summary>
public interface IConfigService
{
    /// <summary>
    /// 获取配置值
    /// </summary>
    T? GetValue<T>(string key, T? defaultValue = default);

    /// <summary>
    /// 设置配置值
    /// </summary>
    void SetValue<T>(string key, T value);

    /// <summary>
    /// 保存配置到文件
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// 加载配置
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// 获取最近使用的文件列表
    /// </summary>
    List<string> GetRecentFiles();

    /// <summary>
    /// 添加最近使用的文件
    /// </summary>
    void AddRecentFile(string filePath);

    /// <summary>
    /// 清除最近使用的文件
    /// </summary>
    void ClearRecentFiles();
}

/// <summary>
/// 应用配置模型
/// </summary>
public class AppConfig
{
    /// <summary>
    /// 主题模式 (Light/Dark)
    /// </summary>
    public string Theme { get; set; } = "Light";

    /// <summary>
    /// 默认转换模式
    /// </summary>
    public string DefaultConvertMode { get; set; } = "Square1440";

    /// <summary>
    /// 默认裁切模式
    /// </summary>
    public string DefaultCropMode { get; set; } = "Auto";

    /// <summary>
    /// 是否启用快速裁切模式
    /// </summary>
    public bool QuickCropEnabled { get; set; } = false;

    /// <summary>
    /// 自定义输出目录
    /// </summary>
    public string? CustomOutputDirectory { get; set; }

    /// <summary>
    /// 是否使用自定义输出目录
    /// </summary>
    public bool UseCustomOutput { get; set; } = false;

    /// <summary>
    /// 最大并发处理数
    /// </summary>
    public int MaxConcurrency { get; set; } = 4;

    /// <summary>
    /// 最近使用的文件列表
    /// </summary>
    public List<string> RecentFiles { get; set; } = new();

    /// <summary>
    /// 启动时检查更新
    /// </summary>
    public bool CheckUpdateOnStartup { get; set; } = true;

    /// <summary>
    /// 窗口位置和大小
    /// </summary>
    public WindowState? Window { get; set; }
}

/// <summary>
/// 窗口状态
/// </summary>
public class WindowState
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 1200;
    public int Height { get; set; } = 900;
    public bool Maximized { get; set; }
}
