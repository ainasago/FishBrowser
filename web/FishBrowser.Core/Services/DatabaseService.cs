using System;
using System.Collections.Generic;
using System.Linq;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Services;

public class DatabaseService : IDatabaseService
{
    private readonly WebScraperDbContext _dbContext;

    public DatabaseService(WebScraperDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // ScrapingTask 操作
    public ScrapingTask CreateTask(string url, int fingerprintProfileId, int? proxyServerId = null)
    {
        var task = new ScrapingTask
        {
            Url = url,
            FingerprintProfileId = fingerprintProfileId,
            ProxyServerId = proxyServerId,
            Status = Models.TaskStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.ScrapingTasks.Add(task);
        _dbContext.SaveChanges();
        return task;
    }

    public ScrapingTask? GetTask(int id)
    {
        return _dbContext.ScrapingTasks.FirstOrDefault(t => t.Id == id);
    }

    public List<ScrapingTask> GetAllTasks()
    {
        return _dbContext.ScrapingTasks.ToList();
    }

    public List<ScrapingTask> GetTasksByStatus(Models.TaskStatus status)
    {
        return _dbContext.ScrapingTasks.Where(t => t.Status == status).ToList();
    }

    public void UpdateTaskStatus(int taskId, Models.TaskStatus status, string? errorMessage = null)
    {
        var task = GetTask(taskId);
        if (task != null)
        {
            task.Status = status;
            task.ErrorMessage = errorMessage;
            if (status == Models.TaskStatus.Completed || status == Models.TaskStatus.Failed)
            {
                task.CompletedAt = DateTime.UtcNow;
            }
            _dbContext.SaveChanges();
        }
    }

    public int GetTaskCount()
    {
        return _dbContext.ScrapingTasks.Count();
    }

    public int GetTaskCountByStatus(Models.TaskStatus status)
    {
        return _dbContext.ScrapingTasks.Count(t => t.Status == status);
    }

    public List<RecentTask> GetRecentTasks(int limit = 10)
    {
        var tasks = _dbContext.ScrapingTasks
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToList();
            
        return tasks.Select(t => new RecentTask
        {
            Name = t.Url,
            Status = t.Status.ToString(),
            CompletedTime = t.CompletedAt.HasValue ? t.CompletedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : ""
        }).ToList();
    }

    // Article 操作
    public Article CreateArticle(string url, string title, string content, int fingerprintProfileId, int? proxyServerId = null)
    {
        var article = new Article
        {
            Url = url,
            Title = title,
            Content = content,
            FingerprintProfileId = fingerprintProfileId,
            ProxyServerId = proxyServerId,
            ScrapedAt = DateTime.UtcNow
        };

        _dbContext.Articles.Add(article);
        _dbContext.SaveChanges();
        return article;
    }

    public Article? GetArticle(int id)
    {
        return _dbContext.Articles.FirstOrDefault(a => a.Id == id);
    }

    public List<Article> GetRecentArticles(int limit = 50)
    {
        return _dbContext.Articles
            .OrderByDescending(a => a.ScrapedAt)
            .Take(limit)
            .ToList();
    }

    // FingerprintProfile 操作
    public FingerprintProfile CreateFingerprintProfile(string name, string userAgent, string locale, string timezone)
    {
        var profile = new FingerprintProfile
        {
            Name = name,
            UserAgent = userAgent,
            Locale = locale,
            Timezone = timezone,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.FingerprintProfiles.Add(profile);
        _dbContext.SaveChanges();
        return profile;
    }

    public List<FingerprintProfile> GetAllFingerprintProfiles()
    {
        return _dbContext.FingerprintProfiles.ToList();
    }

    public FingerprintProfile? GetFingerprintProfile(int id)
    {
        return _dbContext.FingerprintProfiles.FirstOrDefault(f => f.Id == id);
    }

    public void UpdateFingerprintProfile(FingerprintProfile profile)
    {
        var existing = _dbContext.FingerprintProfiles.FirstOrDefault(f => f.Id == profile.Id);
        if (existing != null)
        {
            existing.Name = profile.Name;
            existing.UserAgent = profile.UserAgent;
            existing.AcceptLanguage = profile.AcceptLanguage;
            existing.ViewportWidth = profile.ViewportWidth;
            existing.ViewportHeight = profile.ViewportHeight;
            existing.Timezone = profile.Timezone;
            existing.Locale = profile.Locale;
            existing.Platform = profile.Platform;
            existing.CanvasFingerprint = profile.CanvasFingerprint;
            existing.WebGLRenderer = profile.WebGLRenderer;
            existing.WebGLVendor = profile.WebGLVendor;
            existing.FontPreset = profile.FontPreset;
            existing.AudioSampleRate = profile.AudioSampleRate;
            existing.DisableWebRTC = profile.DisableWebRTC;
            existing.DisableDNSLeak = profile.DisableDNSLeak;
            existing.DisableGeolocation = profile.DisableGeolocation;
            existing.RestrictPermissions = profile.RestrictPermissions;
            existing.EnableDNT = profile.EnableDNT;
            existing.CustomJavaScript = profile.CustomJavaScript;
            existing.CustomHeaders = profile.CustomHeaders;
            existing.CustomCookies = profile.CustomCookies;
            existing.DeviceMemory = profile.DeviceMemory;
            existing.ProcessorCount = profile.ProcessorCount;
            existing.UpdatedAt = DateTime.UtcNow;

            _dbContext.SaveChanges();
        }
    }

    public void DeleteFingerprintProfile(int id)
    {
        var profile = _dbContext.FingerprintProfiles.FirstOrDefault(f => f.Id == id);
        if (profile == null)
            return;

        // 检查引用关系，给用户提示但允许级联删除
        var taskCount = _dbContext.ScrapingTasks.Count(t => t.FingerprintProfileId == id);
        var articleCount = _dbContext.Articles.Count(a => a.FingerprintProfileId == id);
        
        if (taskCount > 0 || articleCount > 0)
        {
            // 仅记录警告，不阻止删除（级联删除会自动处理）
            throw new InvalidOperationException($"删除指纹配置 '{profile.Name}' 将级联删除 {taskCount} 个关联任务和 {articleCount} 篇关联文章。如确认删除，请重试。");
        }

        _dbContext.FingerprintProfiles.Remove(profile);
        _dbContext.SaveChanges();
    }

    // ProxyServer 操作
    public ProxyServer CreateProxyServer(string address, int port, string protocol = "http")
    {
        var proxy = new ProxyServer
        {
            Address = address,
            Port = port,
            Protocol = protocol,
            Status = "Online",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.ProxyServers.Add(proxy);
        _dbContext.SaveChanges();
        return proxy;
    }

    public List<ProxyServer> GetAllProxyServers()
    {
        return _dbContext.ProxyServers.ToList();
    }

    public List<ProxyServer> GetOnlineProxies()
    {
        return _dbContext.ProxyServers.Where(p => p.Status == "Online").ToList();
    }

    public void UpdateProxyStatus(int proxyId, string status, int responseTimeMs = 0)
    {
        var proxy = _dbContext.ProxyServers.FirstOrDefault(p => p.Id == proxyId);
        if (proxy != null)
        {
            proxy.Status = status;
            proxy.ResponseTimeMs = responseTimeMs;
            proxy.LastCheckedAt = DateTime.UtcNow;
            _dbContext.SaveChanges();
        }
    }
}
