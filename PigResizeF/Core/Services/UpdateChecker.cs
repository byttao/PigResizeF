using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using PigResizeF.Core.Common;

namespace PigResizeF.Core.Services;

/// <summary>
/// 更新检查服务实现
/// </summary>
public class UpdateChecker : IUpdateChecker
{
    private readonly HttpClient _httpClient;

    public UpdateChecker()
    {
        // 配置代理
        var handler = new HttpClientHandler();
        var proxyAddress = "http://192.168.10.8:1188";
        handler.Proxy = new System.Net.WebProxy(proxyAddress);
        handler.UseProxy = true;

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"PigResizeF/{Constants.Version}");
    }

    /// <summary>
    /// 检查是否有新版本
    /// </summary>
    public async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                Constants.GitHubApiLatestRelease,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // 解析版本信息
            var tagName = root.GetProperty("tag_name").GetString();
            var releaseUrl = root.GetProperty("html_url").GetString();
            var body = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() : null;
            var publishedAtStr = root.GetProperty("published_at").GetString();
            var isPrerelease = root.GetProperty("prerelease").GetBoolean();

            if (string.IsNullOrWhiteSpace(tagName) ||
                string.IsNullOrWhiteSpace(releaseUrl) ||
                !Version.TryParse(tagName.TrimStart('v', 'V'), out var latestVersion))
            {
                return null;
            }

            // 比较版本
            var currentVersion = GetCurrentVersion();
            if (latestVersion <= currentVersion)
            {
                return null;
            }

            // 有新版本
            return new UpdateInfo
            {
                Version = latestVersion,
                TagName = tagName,
                ReleaseUrl = releaseUrl,
                ReleaseNotes = body,
                PublishedAt = DateTime.TryParse(publishedAtStr, out var publishedAt)
                    ? publishedAt
                    : DateTime.Now,
                IsPrerelease = isPrerelease
            };
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"网络请求失败: {ex.Message}");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            System.Diagnostics.Debug.WriteLine($"请求超时: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"JSON 解析失败: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"检查更新失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 获取当前版本号
    /// </summary>
    public Version GetCurrentVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetName().Version ?? new Version(2, 0, 0);
    }
}
