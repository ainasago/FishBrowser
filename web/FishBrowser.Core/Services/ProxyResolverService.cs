using System;
using System.Linq;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Services;

public class ProxyResolverService
{
    private readonly WebScraperDbContext _db;
    private readonly LogService _log;
    private readonly SecretService _secret;

    public ProxyResolverService(WebScraperDbContext db, LogService log, SecretService secret)
    {
        _db = db;
        _log = log;
        _secret = secret;
    }

    public ProxyServer? Resolve(BrowserEnvironment env, ProxyServer? manualOverride = null)
    {
        if (manualOverride != null)
        {
            return manualOverride;
        }

        if (env.ProxyRefId.HasValue)
        {
            var existing = _db.ProxyServers.FirstOrDefault(p => p.Id == env.ProxyRefId.Value && p.Enabled);
            if (existing != null) return existing;
        }

        if (env.ProxyMode != null && env.ProxyMode.Equals("pool", StringComparison.OrdinalIgnoreCase) && env.ProxyRefId.HasValue)
        {
            var pool = _db.ProxyPools.FirstOrDefault(x => x.Id == env.ProxyRefId.Value && x.Enabled);
            if (pool != null)
            {
                var candidate = _db.ProxyServers.Where(p => p.PoolId == pool.Id && p.Enabled)
                    .OrderByDescending(p => p.Score)
                    .ThenBy(p => p.ResponseTimeMs)
                    .FirstOrDefault();
                return candidate;
            }
        }

        return null;
    }
}
