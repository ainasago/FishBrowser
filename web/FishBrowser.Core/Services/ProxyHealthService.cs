using System;
using System.Net.Http;
using System.Threading.Tasks;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Data;

namespace FishBrowser.WPF.Services;

public class ProxyHealthService
{
    private readonly WebScraperDbContext _db;
    private readonly LogService _log;

    public ProxyHealthService(WebScraperDbContext db, LogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<bool> QuickProbeAsync(ProxyServer proxy)
    {
        try
        {
            using var handler = new HttpClientHandler
            {
                Proxy = new System.Net.WebProxy($"{proxy.Protocol}://{proxy.Address}:{proxy.Port}"),
                UseProxy = true
            };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(3) };
            var t0 = DateTime.UtcNow;
            var resp = await client.GetAsync("https://www.example.com");
            var ms = (int)(DateTime.UtcNow - t0).TotalMilliseconds;
            proxy.ResponseTimeMs = ms;
            proxy.Status = resp.IsSuccessStatusCode ? "Online" : "Offline";
            proxy.LastCheckedAt = DateTime.UtcNow;
            _db.SaveChanges();
            _db.ProxyHealthLogs.Add(new ProxyHealthLog
            {
                ProxyServerId = proxy.Id,
                TestType = "https",
                Success = resp.IsSuccessStatusCode,
                LatencyMs = ms,
                Timestamp = DateTime.UtcNow
            });
            _db.SaveChanges();
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            proxy.Status = "Offline";
            proxy.LastCheckedAt = DateTime.UtcNow;
            _db.SaveChanges();
            _db.ProxyHealthLogs.Add(new ProxyHealthLog
            {
                ProxyServerId = proxy.Id,
                TestType = "https",
                Success = false,
                ErrorCode = ex.GetType().Name,
                LatencyMs = 0,
                Timestamp = DateTime.UtcNow
            });
            _db.SaveChanges();
            _log.LogWarn("ProxyHealth", $"Quick probe failed: {proxy.Address}:{proxy.Port} - {ex.Message}");
            return false;
        }
    }
}
