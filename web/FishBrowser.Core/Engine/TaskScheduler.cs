using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FishBrowser.WPF.Services;

namespace FishBrowser.WPF.Engine;

public class TaskScheduler
{
    private readonly SemaphoreSlim _semaphore;
    private readonly LogService _logService;
    private readonly Dictionary<string, DateTime> _lastRequestTime = new();
    private readonly int _minIntervalMs;

    public TaskScheduler(LogService logService, int maxConcurrency = 5, int minIntervalMs = 1000)
    {
        _semaphore = new SemaphoreSlim(maxConcurrency);
        _logService = logService;
        _minIntervalMs = minIntervalMs;
    }

    /// <summary>
    /// 执行任务（带并发控制与速率限制）
    /// </summary>
    public async Task ExecuteAsync(Func<Task> action, string domain)
    {
        await _semaphore.WaitAsync();
        try
        {
            // 检查域名最后请求时间
            if (_lastRequestTime.TryGetValue(domain, out var lastTime))
            {
                var elapsed = (DateTime.UtcNow - lastTime).TotalMilliseconds;
                if (elapsed < _minIntervalMs)
                {
                    await Task.Delay((int)(_minIntervalMs - elapsed));
                }
            }

            await action();
            _lastRequestTime[domain] = DateTime.UtcNow;
            _logService.LogInfo("TaskScheduler", $"Task executed for domain: {domain}");
        }
        catch (Exception ex)
        {
            _logService.LogError("TaskScheduler", $"Task execution failed for domain {domain}: {ex.Message}", ex.StackTrace);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// 执行任务（带重试）
    /// </summary>
    public async Task ExecuteWithRetryAsync(Func<Task> action, int maxRetries = 3, int delayMs = 1000)
    {
        int attempt = 0;
        while (attempt < maxRetries)
        {
            try
            {
                await action();
                return;
            }
            catch (Exception ex)
            {
                attempt++;
                if (attempt >= maxRetries)
                {
                    _logService.LogError("TaskScheduler", $"Task failed after {maxRetries} retries: {ex.Message}", ex.StackTrace);
                    throw;
                }

                var delay = delayMs * (int)Math.Pow(2, attempt - 1); // 指数退避
                _logService.LogWarn("TaskScheduler", $"Retry attempt {attempt}/{maxRetries} after {delay}ms");
                await Task.Delay(delay);
            }
        }
    }
}
