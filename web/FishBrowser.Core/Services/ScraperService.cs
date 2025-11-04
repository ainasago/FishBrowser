using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FishBrowser.WPF.Engine;
using FishBrowser.WPF.Models;
using HtmlParser = FishBrowser.WPF.Engine.HtmlParser;
using TaskScheduler = FishBrowser.WPF.Engine.TaskScheduler;

namespace FishBrowser.WPF.Services;

public class ScraperService
{
    private readonly DatabaseService _dbService;
    private readonly LogService _logService;
    private readonly FingerprintService _fingerprintService;
    private readonly ProxyService _proxyService;
    private readonly SecretService _secretService;
    private readonly AIService _aiService;
    private readonly TaskScheduler _scheduler;

    public ScraperService(
        DatabaseService dbService,
        LogService logService,
        FingerprintService fingerprintService,
        ProxyService proxyService,
        SecretService secretService,
        AIService aiService,
        TaskScheduler scheduler)
    {
        _dbService = dbService;
        _logService = logService;
        _fingerprintService = fingerprintService;
        _proxyService = proxyService;
        _secretService = secretService;
        _aiService = aiService;
        _scheduler = scheduler;
    }

    /// <summary>
    /// 执行单个采集任务
    /// </summary>
    public async Task ExecuteTaskAsync(ScrapingTask task)
    {
        PlaywrightController? controller = null;
        try
        {
            _dbService.UpdateTaskStatus(task.Id, Models.TaskStatus.Running);

            // 获取指纹与代理
            var fingerprint = _dbService.GetFingerprintProfile(task.FingerprintProfileId);
            if (fingerprint == null)
                throw new InvalidOperationException($"Fingerprint profile {task.FingerprintProfileId} not found");

            var proxy = task.ProxyServerId.HasValue 
                ? _dbService.GetOnlineProxies().FirstOrDefault(p => p.Id == task.ProxyServerId) 
                : null;

            // 初始化浏览器（集成指纹编译产物）
            controller = new PlaywrightController(_logService, _fingerprintService, _secretService);
            await controller.InitializeBrowserAsync(fingerprint, proxy);

            // 导航与抓取
            var html = await controller.NavigateAsync(task.Url);

            // 解析内容
            var parser = new HtmlParser(_logService);
            var (title, content, author, publishedAt) = await parser.ExtractArticleAsync(html);

            // 保存文章
            var article = _dbService.CreateArticle(task.Url, title, content, task.FingerprintProfileId, task.ProxyServerId);
            article.Author = author;
            article.PublishedAt = publishedAt;
            article.RawHtml = html;

            // 触发 AI 处理
            try
            {
                var summary = await _aiService.GenerateSummaryAsync(content);
                var (category, confidence) = await _aiService.ClassifyArticleAsync(title, content);

                _logService.LogInfo("ScraperService", $"AI processing completed for article {article.Id}");
            }
            catch (Exception ex)
            {
                _logService.LogWarn("ScraperService", $"AI processing failed: {ex.Message}");
            }

            // 更新任务状态
            _dbService.UpdateTaskStatus(task.Id, Models.TaskStatus.Completed);
            _logService.LogInfo("ScraperService", $"Task {task.Id} completed successfully");
        }
        catch (Exception ex)
        {
            _dbService.UpdateTaskStatus(task.Id, Models.TaskStatus.Failed, ex.Message);
            _logService.LogError("ScraperService", $"Task {task.Id} failed: {ex.Message}", ex.StackTrace);
        }
        finally
        {
            if (controller != null)
                await controller.DisposeAsync();
        }
    }

    /// <summary>
    /// 批量执行任务
    /// </summary>
    public async Task ExecuteTasksAsync(List<ScrapingTask> tasks, int concurrency = 5)
    {
        var semaphore = new SemaphoreSlim(concurrency);
        var taskList = new List<Task>();

        foreach (var task in tasks)
        {
            await semaphore.WaitAsync();
            var t = Task.Run(async () =>
            {
                try
                {
                    await ExecuteTaskAsync(task);
                }
                finally
                {
                    semaphore.Release();
                }
            });
            taskList.Add(t);
        }

        await Task.WhenAll(taskList);
    }
}
