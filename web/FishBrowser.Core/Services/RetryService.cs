using System;
using System.Threading.Tasks;

namespace FishBrowser.WPF.Services;

/// <summary>
/// 重试服务 - 实现指数退避重试策略
/// </summary>
public class RetryService
{
    private readonly LogService _logService;
    private readonly int _maxRetries;
    private readonly int _initialDelayMs;
    private readonly double _backoffMultiplier;

    /// <summary>
    /// 初始化重试服务
    /// </summary>
    /// <param name="maxRetries">最大重试次数</param>
    /// <param name="initialDelayMs">初始延迟（毫秒）</param>
    /// <param name="backoffMultiplier">退避倍数</param>
    /// <param name="logService">日志服务</param>
    public RetryService(int maxRetries = 3, int initialDelayMs = 1000, double backoffMultiplier = 2.0, LogService logService = null)
    {
        if (maxRetries < 0)
            throw new ArgumentException("Max retries must be non-negative", nameof(maxRetries));
        if (initialDelayMs < 0)
            throw new ArgumentException("Initial delay must be non-negative", nameof(initialDelayMs));
        if (backoffMultiplier <= 0)
            throw new ArgumentException("Backoff multiplier must be positive", nameof(backoffMultiplier));

        _maxRetries = maxRetries;
        _initialDelayMs = initialDelayMs;
        _backoffMultiplier = backoffMultiplier;
        _logService = logService;

        _logService?.LogInfo("Retry", $"Retry service initialized: maxRetries={maxRetries}, initialDelay={initialDelayMs}ms, backoff={backoffMultiplier}");
    }

    /// <summary>
    /// 执行带重试的操作
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, string operationName = "Operation")
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        int retryCount = 0;

        while (true)
        {
            try
            {
                _logService?.LogInfo("Retry", $"Executing {operationName} (attempt {retryCount + 1}/{_maxRetries + 1})");
                return await action();
            }
            catch (Exception ex) when (retryCount < _maxRetries)
            {
                retryCount++;
                var delayMs = (int)(_initialDelayMs * Math.Pow(_backoffMultiplier, retryCount - 1));
                _logService?.LogInfo("Retry", $"{operationName} failed: {ex.Message}. Retrying in {delayMs}ms (attempt {retryCount}/{_maxRetries})");
                await Task.Delay(delayMs);
            }
            catch (Exception ex)
            {
                _logService?.LogError("Retry", $"{operationName} failed after {_maxRetries} retries: {ex.Message}", ex.StackTrace);
                throw;
            }
        }
    }

    /// <summary>
    /// 执行带重试的操作（无返回值）
    /// </summary>
    public async Task ExecuteAsync(Func<Task> action, string operationName = "Operation")
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        int retryCount = 0;

        while (true)
        {
            try
            {
                _logService?.LogInfo("Retry", $"Executing {operationName} (attempt {retryCount + 1}/{_maxRetries + 1})");
                await action();
                return;
            }
            catch (Exception ex) when (retryCount < _maxRetries)
            {
                retryCount++;
                var delayMs = (int)(_initialDelayMs * Math.Pow(_backoffMultiplier, retryCount - 1));
                _logService?.LogInfo("Retry", $"{operationName} failed: {ex.Message}. Retrying in {delayMs}ms (attempt {retryCount}/{_maxRetries})");
                await Task.Delay(delayMs);
            }
            catch (Exception ex)
            {
                _logService?.LogError("Retry", $"{operationName} failed after {_maxRetries} retries: {ex.Message}", ex.StackTrace);
                throw;
            }
        }
    }

    /// <summary>
    /// 计算延迟时间
    /// </summary>
    public int CalculateDelayMs(int retryAttempt)
    {
        if (retryAttempt < 0)
            throw new ArgumentException("Retry attempt must be non-negative", nameof(retryAttempt));

        return (int)(_initialDelayMs * Math.Pow(_backoffMultiplier, retryAttempt));
    }

    /// <summary>
    /// 获取重试配置
    /// </summary>
    public (int MaxRetries, int InitialDelayMs, double BackoffMultiplier) GetConfiguration()
    {
        return (_maxRetries, _initialDelayMs, _backoffMultiplier);
    }
}
