using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using PigResizeF.Core.Services;
using PigResizeF.UI.Forms;

namespace PigResizeF;

internal static class Program
{
    /// <summary>
    /// 应用程序的主入口点
    /// </summary>
    [STAThread]
    static void Main()
    {
        // 配置日志
        ConfigureLogging();

        try
        {
            Log.Information("PigResizeF v2.0 启动");

            // 初始化应用程序
            ApplicationConfiguration.Initialize();

            // 配置依赖注入
            var services = ConfigureServices();
            var serviceProvider = services.BuildServiceProvider();

            // 获取主窗体并运行
            var mainForm = serviceProvider.GetRequiredService<MainForm>();
            Application.Run(mainForm);

            Log.Information("PigResizeF v2.0 正常退出");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "应用程序启动失败");
            MessageBox.Show(
                $"应用程序启动失败:\n\n{ex.Message}",
                "错误",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// 配置服务
    /// </summary>
    private static IServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();

        // 配置
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // 核心服务
        services.AddSingleton<IConfigService, ConfigService>();
        services.AddSingleton<IImageProcessor, ImageProcessor>();
        services.AddSingleton<IUpdateChecker, UpdateChecker>();

        // UI 窗体
        services.AddTransient<MainForm>();

        return services;
    }

    /// <summary>
    /// 配置日志
    /// </summary>
    private static void ConfigureLogging()
    {
        var logPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Logs",
            "PigResizeF-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}
