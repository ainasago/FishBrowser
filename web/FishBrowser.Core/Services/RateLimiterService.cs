using System;
using System.Threading;
using System.Threading.Tasks;

namespace FishBrowser.WPF.Services;

/// <summary>
/// 速率限制服务 - 使用令牌桶算法实现请求限流
/// </summary>
public class RateLimiterService
{
    private double _tokens;
    private readonly double _capacity;
    private readonly double _refillRate;  // 每秒补充的令牌数
    private DateTime _lastRefillTime;
    private readonly object _lockObject = new object();
    private readonly LogService _logService;

    /// <summary>
    /// 初始化速率限制器
    /// </summary>
    /// <param name="requestsPerSecond">每秒允许的请求数</param>
    /// <param name="logService">日志服务</param>
    public RateLimiterService(double requestsPerSecond, LogService logService)
    {
        if (requestsPerSecond <= 0)
            throw new ArgumentException("Requests per second must be greater than 0", nameof(requestsPerSecond));

        _capacity = requestsPerSecond;
        _refillRate = requestsPerSecond;
        _tokens = _capacity;
        _lastRefillTime = DateTime.UtcNow;
        _logService = logService;

        _logService.LogInfo("RateLimiter", $"Rate limiter initialized: {requestsPerSecond} requests/second");
    }

    /// <summary>
    /// 尝试消费令牌（非阻塞）
    /// </summary>
    public bool TryConsume(int tokens = 1)
    {
        lock (_lockObject)
        {
            Refill();
            if (_tokens >= tokens)
            {
                _tokens -= tokens;
                _logService.LogInfo("RateLimiter", $"Consumed {tokens} token(s). Remaining: {_tokens:F2}");
                return true;
            }
            _logService.LogInfo("RateLimiter", $"Insufficient tokens. Required: {tokens}, Available: {_tokens:F2}");
            return false;
        }
    }

    /// <summary>
    /// 等待直到可以消费令牌（阻塞）
    /// </summary>
    public async Task ConsumeAsync(int tokens = 1)
    {
        while (!TryConsume(tokens))
        {
            // 计算需要等待的时间
            var waitTime = CalculateWaitTime(tokens);
            await Task.Delay((int)(waitTime * 1000));
        }
    }

    /// <summary>
    /// 补充令牌
    /// </summary>
    private void Refill()
    {
        var now = DateTime.UtcNow;
        var timePassed = (now - _lastRefillTime).TotalSeconds;
        
        if (timePassed > 0)
        {
            var tokensToAdd = timePassed * _refillRate;
            _tokens = Math.Min(_capacity, _tokens + tokensToAdd);
            _lastRefillTime = now;
        }
    }

    /// <summary>
    /// 计算需要等待的时间（秒）
    /// </summary>
    private double CalculateWaitTime(int tokens)
    {
        lock (_lockObject)
        {
            Refill();
            var deficit = tokens - _tokens;
            return deficit > 0 ? deficit / _refillRate : 0;
        }
    }

    /// <summary>
    /// 获取当前令牌数
    /// </summary>
    public double GetAvailableTokens()
    {
        lock (_lockObject)
        {
            Refill();
            return _tokens;
        }
    }

    /// <summary>
    /// 重置速率限制器
    /// </summary>
    public void Reset()
    {
        lock (_lockObject)
        {
            _tokens = _capacity;
            _lastRefillTime = DateTime.UtcNow;
            _logService.LogInfo("RateLimiter", "Rate limiter reset");
        }
    }

    /// <summary>
    /// 获取速率限制器状态
    /// </summary>
    public (double AvailableTokens, double Capacity, double RefillRate) GetStatus()
    {
        lock (_lockObject)
        {
            Refill();
            return (_tokens, _capacity, _refillRate);
        }
    }
}
