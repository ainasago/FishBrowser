using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Services;

public class ProxyCatalogService
{
    private readonly Dictionary<string, (int ProxyId, DateTime ExpireAt)> _stickyCache = new();
    private TimeSpan _defaultStickyTtl = TimeSpan.FromMinutes(10);
    private readonly WebScraperDbContext _db;
    private readonly LogService _log;
    private readonly SecretService _secret;

    public ProxyCatalogService(WebScraperDbContext db, LogService log, SecretService secret)
    {
        _db = db;
        _log = log;
        _secret = secret;
    }

    public ProxyServer Create(string label, string protocol, string address, int port, string? username, string? passwordPlain)
    {
        var proxy = new ProxyServer
        {
            Label = label?.Trim() ?? string.Empty,
            Protocol = protocol?.Trim().ToLowerInvariant() ?? "http",
            Address = address?.Trim() ?? string.Empty,
            Port = port,
            Username = string.IsNullOrWhiteSpace(username) ? null : username.Trim(),
            PasswordEncrypted = _secret.Encrypt(passwordPlain),
            Enabled = true,
            Status = "Checking",
            CreatedAt = DateTime.UtcNow
        };
        _db.ProxyServers.Add(proxy);
        _db.SaveChanges();
        _log.LogInfo("Proxy", $"Created proxy {proxy.Label} {proxy.Protocol}://{proxy.Address}:{proxy.Port}");
        return proxy;
    }

    public ProxyServer? GetById(int id) => _db.ProxyServers.FirstOrDefault(p => p.Id == id);

    public List<ProxyServer> GetAll() => _db.ProxyServers.OrderByDescending(p => p.Enabled).ThenBy(p => p.Label).ToList();

    public void Update(ProxyServer proxy)
    {
        proxy.UpdatedAt = DateTime.UtcNow;
        _db.SaveChanges();
    }

    public void Delete(int id)
    {
        var p = _db.ProxyServers.FirstOrDefault(x => x.Id == id);
        if (p == null) return;
        _db.ProxyServers.Remove(p);
        _db.SaveChanges();
        _log.LogInfo("Proxy", $"Deleted proxy {id}");
    }

    public ProxyPool CreatePool(string name, string strategy = "random", string? optionsJson = null)
    {
        var pool = new ProxyPool { Name = name.Trim(), Strategy = strategy.Trim().ToLowerInvariant(), Enabled = true, BypassDomainsJson = optionsJson };
        _db.ProxyPools.Add(pool);
        _db.SaveChanges();
        return pool;
    }

    public List<ProxyPool> GetPools() => _db.ProxyPools.Include(p => p.Proxies).OrderBy(p => p.Name).ToList();

    public void Update(ProxyPool pool)
    {
        pool.UpdatedAt = DateTime.UtcNow;
        _db.SaveChanges();
    }

    public void DeletePool(int poolId)
    {
        var pool = _db.ProxyPools.Include(p => p.Proxies).FirstOrDefault(p => p.Id == poolId);
        if (pool == null) return;
        // detach members
        foreach (var m in pool.Proxies.ToList())
        {
            m.PoolId = null;
        }
        _db.ProxyPools.Remove(pool);
        _db.SaveChanges();
        _log.LogInfo("Proxy", $"Deleted pool {poolId}");
    }

    public List<ProxyServer> GetProxiesByPool(int poolId)
        => _db.ProxyServers.Where(p => p.PoolId == poolId).OrderBy(p => p.Label).ToList();

    public List<ProxyServer> GetUngroupedProxies()
        => _db.ProxyServers.Where(p => p.PoolId == null).OrderBy(p => p.Label).ToList();

    public void AssignProxiesToPool(IEnumerable<int> proxyIds, int poolId)
    {
        var list = _db.ProxyServers.Where(p => proxyIds.Contains(p.Id)).ToList();
        foreach (var p in list) p.PoolId = poolId;
        _db.SaveChanges();
        _log.LogInfo("Proxy", $"Assigned {list.Count} proxies to pool {poolId}");
    }

    public List<ProxyServer> GetUngroupedProxiesPaged(int skip, int take, string? query)
    {
        var q = _db.ProxyServers.Where(p => p.PoolId == null);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var lq = query.Trim().ToLower();
            q = q.Where(p => (p.Label ?? "").ToLower().Contains(lq) || (p.Address + ":" + p.Port).ToLower().Contains(lq));
        }
        return q.OrderBy(p => p.Label).Skip(skip).Take(take).ToList();
    }

    public int CountUngroupedProxies(string? query)
    {
        var q = _db.ProxyServers.Where(p => p.PoolId == null);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var lq = query.Trim().ToLower();
            q = q.Where(p => (p.Label ?? "").ToLower().Contains(lq) || (p.Address + ":" + p.Port).ToLower().Contains(lq));
        }
        return q.Count();
    }

    public ProxyServer? PickProxyFromPool(int poolId, string strategy = "random", string? stickyScope = null, string? stickyKey = null, int? stickyTtlMinutes = null)
    {
        var pool = _db.ProxyPools.FirstOrDefault(p => p.Id == poolId && p.Enabled);
        if (pool == null) return null;
        var candidates = _db.ProxyServers.Where(p => p.PoolId == poolId && p.Enabled).ToList();
        if (candidates.Count == 0) return null;
        strategy = (strategy ?? "random").Trim().ToLowerInvariant();
        // derive sticky options from pool optionsJson (stored in BypassDomainsJson)
        string poolScope = "session";
        int poolTtl = (int)_defaultStickyTtl.TotalMinutes;
        if (!string.IsNullOrWhiteSpace(pool.BypassDomainsJson))
        {
            try
            {
                var doc = System.Text.Json.JsonDocument.Parse(pool.BypassDomainsJson);
                if (doc.RootElement.TryGetProperty("stickyScope", out var s) && s.ValueKind == System.Text.Json.JsonValueKind.String)
                    poolScope = s.GetString() ?? poolScope;
                if (doc.RootElement.TryGetProperty("stickyTtlMinutes", out var t) && t.TryGetInt32(out var ttl))
                    poolTtl = Math.Max(1, ttl);
            }
            catch { }
        }
        var scope = stickyScope ?? poolScope;
        var ttlSpan = TimeSpan.FromMinutes(stickyTtlMinutes ?? poolTtl);
        var key = $"{poolId}|{scope}|{stickyKey ?? "global"}";
        switch (strategy)
        {
            case "sticky":
                // 若缓存命中且未过期且仍可用，则返回缓存
                if (_stickyCache.TryGetValue(key, out var entry) && entry.ExpireAt > DateTime.UtcNow)
                {
                    var cached = candidates.FirstOrDefault(p => p.Id == entry.ProxyId);
                    if (cached != null)
                        return cached;
                }
                // 否则按健康优先挑选作为新粘性代理
                var stickyPick = candidates
                    .OrderByDescending(p => p.Status == "Online")
                    .ThenBy(p => p.ResponseTimeMs == 0 ? int.MaxValue : p.ResponseTimeMs)
                    .FirstOrDefault();
                if (stickyPick != null)
                {
                    _stickyCache[key] = (stickyPick.Id, DateTime.UtcNow.Add(ttlSpan));
                }
                return stickyPick;
            case "least-used":
                return candidates
                    .OrderBy(p => p.CurrentConcurrency)
                    .ThenBy(p => p.SuccessCount + p.FailCount)
                    .ThenBy(p => p.ResponseTimeMs)
                    .FirstOrDefault();
            case "health":
            case "health-priority":
                return candidates
                    .OrderByDescending(p => p.Status == "Online")
                    .ThenBy(p => p.ResponseTimeMs == 0 ? int.MaxValue : p.ResponseTimeMs)
                    .FirstOrDefault();
            case "random":
            default:
                var idx = Random.Shared.Next(0, candidates.Count);
                return candidates[idx];
        }
    }
}
