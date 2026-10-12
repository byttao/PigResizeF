namespace PigResizeF.Core.Models;

/// <summary>
/// 处理结果模型
/// </summary>
/// <typeparam name="T">结果数据类型</typeparam>
public class ProcessResult<T>
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 结果数据
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static ProcessResult<T> Success(T data)
    {
        return new ProcessResult<T>
        {
            IsSuccess = true,
            Data = data
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static ProcessResult<T> Failure(string errorMessage)
    {
        return new ProcessResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// 无返回数据的处理结果
/// </summary>
public class ProcessResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static ProcessResult Success()
    {
        return new ProcessResult { IsSuccess = true };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static ProcessResult Failure(string errorMessage)
    {
        return new ProcessResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
