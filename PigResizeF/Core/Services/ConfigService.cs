using System.Text.Json;
using PigResizeF.Core.Common;

namespace PigResizeF.Core.Services;

/// <summary>
/// 配置服务实现
/// </summary>
public class ConfigService : IConfigService
{
    private readonly string _configFilePath;
    private Dictionary<string, object> _configData;
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    public ConfigService()
    {
        _configFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            Constants.ConfigFileName);
        _configData = new Dictionary<string, object>();
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = await File.ReadAllTextAsync(_configFilePath);
                _configData = JsonSerializer.Deserialize<Dictionary<string, object>>(json)
                    ?? new Dictionary<string, object>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载配置失败: {ex.Message}");
            _configData = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    public async Task SaveAsync()
    {
        await _saveLock.WaitAsync();
        try
        {
            var directory = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var json = JsonSerializer.Serialize(_configData, options);
            await File.WriteAllTextAsync(_configFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存配置失败: {ex.Message}");
        }
        finally
        {
            _saveLock.Release();
        }
    }

    /// <summary>
    /// 获取配置值
    /// </summary>
    public T? GetValue<T>(string key, T? defaultValue = default)
    {
        try
        {
            if (_configData.TryGetValue(key, out var value))
            {
                if (value is JsonElement jsonElement)
                {
                    return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
                }
                return (T)Convert.ChangeType(value, typeof(T));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取配置值失败 [{key}]: {ex.Message}");
        }
        return defaultValue;
    }

    /// <summary>
    /// 设置配置值
    /// </summary>
    public void SetValue<T>(string key, T value)
    {
        _configData[key] = value!;
    }

    /// <summary>
    /// 获取最近使用的文件列表
    /// </summary>
    public List<string> GetRecentFiles()
    {
        return GetValue<List<string>>("App:RecentFiles") ?? new List<string>();
    }

    /// <summary>
    /// 添加最近使用的文件
    /// </summary>
    public void AddRecentFile(string filePath)
    {
        var recentFiles = GetRecentFiles();
        recentFiles.Remove(filePath); // 移除旧的
        recentFiles.Insert(0, filePath); // 添加到开头

        // 保持最大数量
        if (recentFiles.Count > Constants.MaxHistoryCount)
        {
            recentFiles = recentFiles.Take(Constants.MaxHistoryCount).ToList();
        }

        SetValue("App:RecentFiles", recentFiles);
    }

    /// <summary>
    /// 清除最近使用的文件
    /// </summary>
    public void ClearRecentFiles()
    {
        SetValue("App:RecentFiles", new List<string>());
    }
}
