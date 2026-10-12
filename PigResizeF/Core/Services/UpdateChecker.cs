using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace PigResizeF.Core.Services
{
    /// <summary>
    /// 更新检查服务实现
    /// </summary>
    public class UpdateChecker : IUpdateChecker
    {
        private readonly HttpClient _httpClient;
        private readonly string _currentVersion;
        private const string GithubApiUrl = "https://api.github.com/repos/byttao/PigResizeF/releases/latest";

        public UpdateChecker()
        {
            // 配置代理
            var handler = new HttpClientHandler();
            try
            {
                handler.Proxy = new WebProxy("http://192.168.10.8:1188");
                handler.UseProxy = true;
            }
            catch
            {
                // 如果代理配置失败，使用默认设置
                handler.UseProxy = false;
            }

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PigResizeF/1.5");

            // 获取当前版本
            _currentVersion = GetCurrentVersion();
        }

        /// <summary>
        /// 检查更新
        /// </summary>
        public async Task<UpdateInfo?> CheckForUpdateAsync()
        {
            try
            {
                using var response = await _httpClient.GetAsync(GithubApiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                using var release = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
                var root = release.RootElement;

                var tagName = root.GetProperty("tag_name").GetString();
                var releaseUrl = root.GetProperty("html_url").GetString();
                var releaseBody = root.GetProperty("body").GetString() ?? string.Empty;
                var publishedAt = root.GetProperty("published_at").GetDateTime();

                if (string.IsNullOrWhiteSpace(tagName) || string.IsNullOrWhiteSpace(releaseUrl))
                {
                    return null;
                }

                // 移除版本号前的 'v' 前缀
                var latestVersion = tagName.TrimStart('v');
                var hasUpdate = CompareVersions(_currentVersion, latestVersion);

                return new UpdateInfo
                {
                    LatestVersion = latestVersion,
                    CurrentVersion = _currentVersion,
                    HasUpdate = hasUpdate,
                    ReleaseUrl = releaseUrl,
                    ReleaseNotes = releaseBody,
                    PublishedAt = publishedAt
                };
            }
            catch (Exception)
            {
                // 静默失败，不影响程序正常使用
                return null;
            }
        }

        /// <summary>
        /// 获取当前版本
        /// </summary>
        public string GetCurrentVersion()
        {
            if (!string.IsNullOrEmpty(_currentVersion))
            {
                return _currentVersion;
            }

            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.5.0";
            }
            catch
            {
                return "1.5.0";
            }
        }

        /// <summary>
        /// 比较版本号
        /// </summary>
        /// <param name="currentVersion">当前版本</param>
        /// <param name="latestVersion">最新版本</param>
        /// <returns>如果有新版本返回 true</returns>
        private static bool CompareVersions(string currentVersion, string latestVersion)
        {
            try
            {
                var current = Version.Parse(currentVersion);
                var latest = Version.Parse(latestVersion);
                return latest > current;
            }
            catch
            {
                // 如果版本号格式不正确，返回 false
                return false;
            }
        }
    }
}
