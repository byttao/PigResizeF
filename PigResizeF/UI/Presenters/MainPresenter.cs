using PigResizeF.Core.Services;
using PigResizeF.UI.Forms;

namespace PigResizeF.UI.Presenters;

/// <summary>
/// 主窗体 Presenter (MVP 模式)
/// </summary>
public class MainPresenter
{
    private readonly MainForm _view;
    private readonly IImageProcessor _imageProcessor;
    private readonly IConfigService _configService;
    private readonly IUpdateChecker _updateChecker;

    public MainPresenter(
        MainForm view,
        IImageProcessor imageProcessor,
        IConfigService configService,
        IUpdateChecker updateChecker)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _imageProcessor = imageProcessor ?? throw new ArgumentNullException(nameof(imageProcessor));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _updateChecker = updateChecker ?? throw new ArgumentNullException(nameof(updateChecker));

        Initialize();
    }

    /// <summary>
    /// 初始化
    /// </summary>
    private void Initialize()
    {
        // 订阅窗体事件
        _view.Load += OnViewLoad;
        _view.Shown += OnViewShown;
    }

    /// <summary>
    /// 窗体加载时
    /// </summary>
    private void OnViewLoad(object? sender, EventArgs e)
    {
        // 可以在这里初始化数据
    }

    /// <summary>
    /// 窗体显示后
    /// </summary>
    private async void OnViewShown(object? sender, EventArgs e)
    {
        // 检查更新
        var checkUpdate = _configService.GetValue<bool>("App:CheckUpdateOnStartup", true);
        if (checkUpdate)
        {
            await CheckForUpdatesAsync();
        }
    }

    /// <summary>
    /// 检查更新
    /// </summary>
    private async Task CheckForUpdatesAsync()
    {
        try
        {
            var updateInfo = await _updateChecker.CheckForUpdateAsync();
            if (updateInfo != null)
            {
                var result = MessageBox.Show(
                    $"发现新版本 {updateInfo.TagName}\n\n" +
                    $"当前版本: v{_updateChecker.GetCurrentVersion()}\n" +
                    $"发布时间: {updateInfo.PublishedAt:yyyy-MM-dd}\n\n" +
                    $"是否打开下载页面？",
                    "发现新版本",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = updateInfo.ReleaseUrl,
                        UseShellExecute = true
                    });
                }
            }
        }
        catch (Exception ex)
        {
            // 更新检查失败不影响使用
            System.Diagnostics.Debug.WriteLine($"检查更新失败: {ex.Message}");
        }
    }
}
