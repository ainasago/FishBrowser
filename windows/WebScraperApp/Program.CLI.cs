using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Services;
using FishBrowser.WPF.Engine;
using FishBrowser.WPF.Tests;
using FishBrowser.WPF.Infrastructure.Data;

namespace FishBrowser.WPF;

/// <summary>
/// CLI 命令行模式 - 用于自动化测试和批量操作
/// 使用方式: FishBrowser.WPF.exe --cli test
/// </summary>
public static class CLIMode
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length < 2 || args[0] != "--cli")
        {
            return 0; // 不是CLI模式，返回给GUI处理
        }

        var command = args[1].ToLower();
        
        // 构建主机
        var host = new HostBuilder()
            .ConfigureServices((context, services) =>
            {
                // 数据库
                services.AddDbContext<WebScraperDbContext>();

                // 服务
                services.AddScoped<TaskService>();
                services.AddScoped<ScraperService>();
                services.AddScoped<FingerprintService>();
                services.AddScoped<ProxyService>();
                services.AddScoped<AIService>();
                services.AddScoped<DatabaseService>();
                services.AddScoped<LogService>();

                // 引擎
                services.AddScoped<PlaywrightController>();
                services.AddScoped<HtmlParser>();
                services.AddScoped<FingerprintManager>();
                services.AddScoped<Engine.TaskScheduler>();
            })
            .Build();

        // 初始化数据库 (使用 FreeSql 迁移管理器，不删除数据)
        using (var scope = host.Services.CreateScope())
        {
            try
            {
                var migrationManager = scope.ServiceProvider.GetRequiredService<FreeSqlMigrationManager>();
                migrationManager.InitializeDatabase();

                var stats = migrationManager.GetStatistics();
                Console.WriteLine($"Database initialized: {stats.TableCount} tables, Size: {stats.GetFormattedSize()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database initialization error: {ex.Message}");
            }
        }

        try
        {
            return command switch
            {
                "test" => await RunTest(host),
                "scrape" => await RunScrape(host, args),
                "logs" => await ShowLogs(host),
                "help" => ShowHelp(),
                _ => HandleUnknownCommand(command)
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 错误: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            return 1;
        }
    }

    private static async Task<int> RunTest(IHost host)
    {
        Console.WriteLine("🚀 开始运行端到端测试...");
        Console.WriteLine("═══════════════════════════════════════════");
        
        try
        {
            await SampleTest.RunSampleAsync(host);
            Console.WriteLine("═══════════════════════════════════════════");
            Console.WriteLine("✅ 测试完成！");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("═══════════════════════════════════════════");
            Console.WriteLine($"❌ 测试失败: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> RunScrape(IHost host, string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("❌ 缺少参数");
            Console.WriteLine("用法: FishBrowser.WPF.exe --cli scrape <url> [fingerprintId] [proxyId]");
            return 1;
        }

        var url = args[2];
        var fingerprintId = args.Length > 3 ? int.Parse(args[3]) : 1;
        var proxyId = args.Length > 4 ? int.Parse(args[4]) : (int?)null;

        Console.WriteLine($"🌐 开始采集: {url}");
        Console.WriteLine("═══════════════════════════════════════════");

        using (var scope = host.Services.CreateScope())
        {
            var dbService = scope.ServiceProvider.GetRequiredService<DatabaseService>();
            var logService = scope.ServiceProvider.GetRequiredService<LogService>();
            var scraperService = scope.ServiceProvider.GetRequiredService<ScraperService>();

            try
            {
                // 创建任务
                var task = dbService.CreateTask(url, fingerprintId, proxyId);
                logService.LogInfo("CLI", $"✓ 任务创建成功: ID={task.Id}");

                // 执行采集
                await scraperService.ExecuteTaskAsync(task);
                
                // 查询结果
                var articles = dbService.GetRecentArticles(limit: 5);
                Console.WriteLine($"✓ 采集完成，找到 {articles.Count} 篇文章");
                
                foreach (var article in articles)
                {
                    Console.WriteLine($"  - {article.Title}");
                }

                Console.WriteLine("═══════════════════════════════════════════");
                Console.WriteLine("✅ 采集成功！");
                return 0;
            }
            catch (Exception ex)
            {
                logService.LogError("CLI", $"采集失败: {ex.Message}", ex.StackTrace);
                Console.WriteLine("═══════════════════════════════════════════");
                Console.WriteLine($"❌ 采集失败: {ex.Message}");
                return 1;
            }
        }
    }

    private static async Task<int> ShowLogs(IHost host)
    {
        Console.WriteLine("📋 最近 50 条日志");
        Console.WriteLine("═══════════════════════════════════════════");

        using (var scope = host.Services.CreateScope())
        {
            var logService = scope.ServiceProvider.GetRequiredService<LogService>();
            var logs = logService.GetLogs(50);

            foreach (var log in logs)
            {
                var icon = log.Level switch
                {
                    "INFO" => "ℹ️",
                    "WARN" => "⚠️",
                    "ERROR" => "❌",
                    _ => "•"
                };

                Console.WriteLine($"{icon} [{log.Timestamp:HH:mm:ss}] [{log.Level}] [{log.Source}] {log.Message}");
            }

            Console.WriteLine("═══════════════════════════════════════════");
            Console.WriteLine($"✅ 共 {logs.Count} 条日志");
            return 0;
        }
    }

    private static int ShowHelp()
    {
        Console.WriteLine("🔧 WebScraper CLI 命令行工具");
        Console.WriteLine("═══════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("用法:");
        Console.WriteLine("  FishBrowser.WPF.exe --cli <command> [options]");
        Console.WriteLine();
        Console.WriteLine("命令:");
        Console.WriteLine("  test                    运行端到端测试");
        Console.WriteLine("  scrape <url> [fpId] [pId]  采集指定URL");
        Console.WriteLine("  logs                    显示最近50条日志");
        Console.WriteLine("  help                    显示此帮助信息");
        Console.WriteLine();
        Console.WriteLine("示例:");
        Console.WriteLine("  FishBrowser.WPF.exe --cli test");
        Console.WriteLine("  FishBrowser.WPF.exe --cli scrape https://news.now.com/home/local");
        Console.WriteLine("  FishBrowser.WPF.exe --cli scrape https://example.com 1 1");
        Console.WriteLine("  FishBrowser.WPF.exe --cli logs");
        Console.WriteLine("═══════════════════════════════════════════");
        return 0;
    }

    private static int HandleUnknownCommand(string command)
    {
        Console.WriteLine($"❌ 未知命令: {command}");
        Console.WriteLine("使用 'FishBrowser.WPF.exe --cli help' 查看帮助");
        return 1;
    }
}
