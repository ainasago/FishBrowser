using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Services;

public class ProxyService
{
    private readonly DatabaseService _dbService;
    private readonly Random _random = new();

    public ProxyService(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    /// <summary>
    /// 获取下一个可用代理
    /// </summary>
    public ProxyServer? GetNextProxy(string strategy = "Random")
    {
        var onlineProxies = _dbService.GetOnlineProxies();
        
        if (onlineProxies.Count == 0)
            return null;

        return strategy switch
        {
            "Random" => onlineProxies[_random.Next(onlineProxies.Count)],
            "RoundRobin" => onlineProxies[0], // 简化实现，实际需要维护状态
            _ => onlineProxies[_random.Next(onlineProxies.Count)]
        };
    }

    /// <summary>
    /// 检查代理健康状态
    /// </summary>
    public async Task<bool> CheckProxyHealthAsync(ProxyServer proxy)
    {
        try
        {
            using var handler = new HttpClientHandler();
            handler.Proxy = new System.Net.WebProxy($"{proxy.Protocol}://{proxy.Address}:{proxy.Port}");
            handler.UseProxy = true;

            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
            var startTime = DateTime.UtcNow;
            var response = await client.GetAsync("http://httpbin.org/ip");
            var responseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            if (response.IsSuccessStatusCode)
            {
                _dbService.UpdateProxyStatus(proxy.Id, "Online", responseTime);
                return true;
            }
        }
        catch
        {
            _dbService.UpdateProxyStatus(proxy.Id, "Offline");
        }

        return false;
    }

    /// <summary>
    /// 批量检查所有代理
    /// </summary>
    public async Task CheckAllProxiesAsync()
    {
        var allProxies = _dbService.GetOnlineProxies();
        var tasks = allProxies.Select(p => CheckProxyHealthAsync(p)).ToList();
        await Task.WhenAll(tasks);
    }
}
