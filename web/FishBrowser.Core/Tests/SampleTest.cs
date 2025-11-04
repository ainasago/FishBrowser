using FishBrowser.WPF.Data;
using FishBrowser.WPF.Services;
using FishBrowser.WPF.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace FishBrowser.WPF.Tests;

/// <summary>
/// 示例测试代码（可在 Program.cs 中调用）
/// </summary>
public class SampleTest
{
    public static async Task RunSampleAsync(IHost host)
    {
        using (var scope = host.Services.CreateScope())
        {
            var dbService = scope.ServiceProvider.GetRequiredService<DatabaseService>();
            var logService = scope.ServiceProvider.GetRequiredService<LogService>();
            var scraperService = scope.ServiceProvider.GetRequiredService<ScraperService>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            try
            {
                // 直接使用指定的测试 URL
                var testUrl = "https://news.now.com/home/life/player?newsId=623091";
                var testTimeout = 60;
                
                logService.LogInfo("SampleTest", "=== 开始采集测试 ===");
                logService.LogInfo("SampleTest", $"测试 URL: {testUrl}");
                logService.LogInfo("SampleTest", $"超时设置: {testTimeout} 秒");

                // 1. 创建指纹配置
                logService.LogInfo("SampleTest", "步骤 1: 创建指纹配置");
                var fingerprint = dbService.CreateFingerprintProfile(
                    name: "Test Windows Desktop",
                    userAgent: "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                    locale: "zh-CN",
                    timezone: "Asia/Shanghai"
                );
                logService.LogInfo("SampleTest", $"✓ 指纹配置创建成功: ID={fingerprint.Id}");

                // 2. 创建采集任务
                logService.LogInfo("SampleTest", "步骤 2: 创建采集任务");
                var task = dbService.CreateTask(
                    url: testUrl,
                    fingerprintProfileId: fingerprint.Id
                );
                logService.LogInfo("SampleTest", $"✓ 采集任务创建成功: ID={task.Id}, URL={task.Url}");

                // 3. 执行采集（注意：需要网络连接）
                logService.LogInfo("SampleTest", "步骤 3: 执行采集任务");
                try
                {
                    await scraperService.ExecuteTaskAsync(task);
                    logService.LogInfo("SampleTest", "✓ 采集任务执行完成");
                }
                catch (Exception ex)
                {
                    logService.LogError("SampleTest", $"采集任务执行失败: {ex.Message}", ex.StackTrace);
                }

                // 4. 查询结果
                logService.LogInfo("SampleTest", "步骤 4: 查询采集结果");
                var articles = dbService.GetRecentArticles(limit: 10);
                logService.LogInfo("SampleTest", $"✓ 查询到 {articles.Count} 篇文章");
                foreach (var article in articles)
                {
                    logService.LogInfo("SampleTest", $"  - {article.Title} (URL: {article.Url})");
                }

                // 5. 查询日志
                logService.LogInfo("SampleTest", "步骤 5: 查询系统日志");
                var logs = logService.GetLogs(limit: 20);
                logService.LogInfo("SampleTest", $"✓ 查询到 {logs.Count} 条日志");

                logService.LogInfo("SampleTest", "=== 采集测试完成 ===");
            }
            catch (Exception ex)
            {
                logService.LogError("SampleTest", $"测试失败: {ex.Message}", ex.StackTrace);
            }
        }
    }
}
