using System;
using System.Threading.Tasks;

namespace PigResizeF.Core.Services
{
    /// <summary>
    /// 更新检查服务接口
    /// </summary>
    public interface IUpdateChecker
    {
        /// <summary>
        /// 检查更新
        /// </summary>
        /// <returns>更新信息，如果没有更新则返回 null</returns>
        Task<UpdateInfo?> CheckForUpdateAsync();

        /// <summary>
        /// 获取当前版本
        /// </summary>
        /// <returns>当前版本号</returns>
        string GetCurrentVersion();
    }

    /// <summary>
    /// 更新信息
    /// </summary>
    public class UpdateInfo
    {
        /// <summary>
        /// 最新版本号
        /// </summary>
        public string LatestVersion { get; set; } = string.Empty;

        /// <summary>
        /// 当前版本号
        /// </summary>
        public string CurrentVersion { get; set; } = string.Empty;

        /// <summary>
        /// 是否有新版本
        /// </summary>
        public bool HasUpdate { get; set; }

        /// <summary>
        /// 发布页面 URL
        /// </summary>
        public string ReleaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// 更新说明
        /// </summary>
        public string ReleaseNotes { get; set; } = string.Empty;

        /// <summary>
        /// 发布时间
        /// </summary>
        public DateTime PublishedAt { get; set; }
    }
}
