using System.Collections.Generic;
using FishBrowser.WPF.Models;

public interface IDatabaseService
{
    // ScrapingTask
    ScrapingTask CreateTask(string url, int fingerprintProfileId, int? proxyServerId = null);
    ScrapingTask? GetTask(int id);
    List<ScrapingTask> GetAllTasks();
    List<ScrapingTask> GetTasksByStatus(FishBrowser.WPF.Models.TaskStatus status);
    void UpdateTaskStatus(int taskId, FishBrowser.WPF.Models.TaskStatus status, string? errorMessage = null);
    int GetTaskCount();
    int GetTaskCountByStatus(FishBrowser.WPF.Models.TaskStatus status);
    List<RecentTask> GetRecentTasks(int limit = 10);

    // Article
    Article CreateArticle(string url, string title, string content, int fingerprintProfileId, int? proxyServerId = null);
    Article? GetArticle(int id);
    List<Article> GetRecentArticles(int limit = 50);

    // FingerprintProfile
    FingerprintProfile CreateFingerprintProfile(string name, string userAgent, string locale, string timezone);
    List<FingerprintProfile> GetAllFingerprintProfiles();
    FingerprintProfile? GetFingerprintProfile(int id);
    void UpdateFingerprintProfile(FingerprintProfile profile);
    void DeleteFingerprintProfile(int id);

    // ProxyServer
    ProxyServer CreateProxyServer(string address, int port, string protocol = "http");
    List<ProxyServer> GetAllProxyServers();
    List<ProxyServer> GetOnlineProxies();
    void UpdateProxyStatus(int proxyId, string status, int responseTimeMs = 0);
}
