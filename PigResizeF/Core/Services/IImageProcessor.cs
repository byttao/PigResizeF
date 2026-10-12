using PigResizeF.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PigResizeF.Core.Services
{
    /// <summary>
    /// 图片处理服务接口
    /// </summary>
    public interface IImageProcessor
    {
        /// <summary>
        /// 转换单个图片
        /// </summary>
        /// <param name="inputPath">输入文件路径</param>
        /// <param name="options">处理选项</param>
        /// <param name="progress">进度报告</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>处理结果</returns>
        Task<ProcessResult> ConvertImageAsync(
            string inputPath,
            ProcessOptions options,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量转换图片
        /// </summary>
        /// <param name="inputPaths">输入文件路径列表</param>
        /// <param name="options">处理选项</param>
        /// <param name="progress">进度报告</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>处理结果列表</returns>
        Task<List<ProcessResult>> ConvertImagesAsync(
            IEnumerable<string> inputPaths,
            ProcessOptions options,
            IProgress<(int current, int total, string message)>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 裁切单个图片
        /// </summary>
        /// <param name="inputPath">输入文件路径</param>
        /// <param name="options">裁切选项</param>
        /// <param name="progress">进度报告</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>处理结果</returns>
        Task<ProcessResult> CropImageAsync(
            string inputPath,
            CropOptions options,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量裁切图片
        /// </summary>
        /// <param name="inputPaths">输入文件路径列表</param>
        /// <param name="options">裁切选项</param>
        /// <param name="progress">进度报告</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>处理结果列表</returns>
        Task<List<ProcessResult>> CropImagesAsync(
            IEnumerable<string> inputPaths,
            CropOptions options,
            IProgress<(int current, int total, string message)>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取支持的图片格式
        /// </summary>
        /// <returns>支持的文件扩展名列表</returns>
        IEnumerable<string> GetSupportedFormats();

        /// <summary>
        /// 验证文件是否为有效图片
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否为有效图片</returns>
        bool IsValidImage(string filePath);
    }
}
