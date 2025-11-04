using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Services;
using FishBrowser.WPF.Engine;
using FishBrowser.WPF.Infrastructure.Data;
using FishBrowser.WPF.Infrastructure.Data.Repositories;
using FishBrowser.WPF.Application.Services; 
using FishBrowser.WPF.Domain.Services;
using FishBrowser.WPF.Domain.Repositories;
using FreeSql;
using Microsoft.EntityFrameworkCore;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;
using HtmlParser = FishBrowser.WPF.Engine.HtmlParser;

namespace FishBrowser.WPF.Infrastructure.Configuration;

/// <summary>
/// 依赖注入配置扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加数据库服务
    /// </summary>
    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 解析数据库路径（配置优先，其次项目根目录，最后 bin 目录）
        string ResolveDbPath()
        {
            var cfgPath = configuration["Database:Path"];
            if (!string.IsNullOrWhiteSpace(cfgPath))
            {
                return Path.IsPathRooted(cfgPath) ? cfgPath : Path.GetFullPath(cfgPath);
            }
            var baseDir = AppContext.BaseDirectory?.TrimEnd(Path.DirectorySeparatorChar) ?? Directory.GetCurrentDirectory();
            // 尝试回到项目根目录（bin/Debug/netX → 项目目录）
            var projDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
            var candidate = Path.Combine(projDir, "webscraper.db");
            return candidate;
        }

        var dbPathResolved = ResolveDbPath();

        // 注册 DbContext（使用解析后的路径）
        services.AddDbContext<WebScraperDbContext>(options =>
        {
            options.UseSqlite($"Data Source={dbPathResolved}");
        });

        // 注册 FreeSql (SQLite)
        services.AddSingleton<IFreeSql>(sp =>
        {
            var dbPath = dbPathResolved;
            var fsql = new FreeSqlBuilder()
                .UseConnectionString(DataType.Sqlite, $"Data Source={dbPath}")
                .UseAutoSyncStructure(false)
                .Build();
            return fsql;
        });

        // 注册迁移管理器
        services.AddSingleton<FreeSqlMigrationManager>();

        return services;
    }

    /// <summary>
    /// 添加应用服务
    /// </summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        // 基础服务
        services.AddSingleton<LogService>();
        services.AddSingleton<RetryService>();
        services.AddSingleton<DatabaseService>();
        services.AddSingleton<FingerprintService>();
        services.AddSingleton<FingerprintPresetService>();
        services.AddSingleton<ProxyService>();
        services.AddSingleton<SecretService>();
        services.AddScoped<ProxyCatalogService>();
        services.AddScoped<ProxyHealthService>();
        services.AddScoped<ProxyResolverService>();
        services.AddSingleton<AIService>();
        services.AddSingleton<TaskQueueService>();
        services.AddSingleton<TaskService>();
        services.AddSingleton<ScraperService>();
        services.AddSingleton<HtmlParser>();
        services.AddSingleton<MetaValidationService>();
        services.AddSingleton<DependencyResolutionService>();
        services.AddSingleton<UserAgentCompositionService>();
        services.AddScoped<BrowserEnvironmentService>();
        services.AddScoped<BrowserSessionService>();
        services.AddSingleton<FingerprintGeneratorService>();
        services.AddSingleton<MetaCatalogIoService>();
        services.AddScoped<FontService>();
        services.AddScoped<GpuCatalogService>();
        services.AddSingleton<AntiDetectionService>();  // 防检测数据生成与校验
        services.AddSingleton<FingerprintCollectorService>();  // 通用指纹收集服务
        
        // 浏览器分组和指纹校验服务 (M1)
        services.AddScoped<BrowserGroupService>();
        services.AddScoped<FingerprintValidationService>();
        
        // 真实数据库和随机生成器 (M2)
        services.AddSingleton<ChromeVersionDatabase>();
        services.AddScoped<RandomFingerprintGeneratorService>();
        services.AddScoped<BrowserRandomGenerator>();
        
        // AI 提供商服务
        services.AddScoped<IAIProviderService, AIProviderService>();
        services.AddScoped<IAIClientService, AIClientService>();
        
        // 任务测试运行器（依赖 Scoped 的 BrowserEnvironmentService）
        services.AddScoped<TaskTestRunnerService>();
        services.AddScoped<DslParser>();
        services.AddScoped<DslExecutor>();
        
        // 接口注册
        services.AddScoped<IDatabaseService, DatabaseService>();
        services.AddScoped<ILogService, LogService>();
        services.AddSingleton<IFingerprintPresetService, FingerprintPresetService>();
        
        // 同时注册具体类（为了兼容直接依赖具体类的代码）
        services.AddScoped<DatabaseService>();
        services.AddScoped<LogService>();
        services.AddSingleton<FingerprintPresetService>();

        // 应用层服务
        services.AddScoped<FingerprintApplicationService>();

        return services;
    }

    /// <summary>
    /// 添加 ViewModel 服务
    /// </summary>
    public static IServiceCollection AddViewModels(
        this IServiceCollection services)
    {
        //services.AddScoped<FingerprintConfigViewModel>();

        return services;
    }

    /// <summary>
    /// 添加领域服务和仓储
    /// </summary>
    public static IServiceCollection AddDomainServices(
        this IServiceCollection services)
    {
        // 仓储
        services.AddScoped<IFingerprintRepository, FingerprintRepository>();

        // 领域服务
        services.AddScoped<FingerprintDomainService>();

        return services;
    }

    /// <summary>
    /// 添加引擎服务
    /// </summary>
    public static IServiceCollection AddEngineServices(
        this IServiceCollection services)
    {
        services.AddScoped<PlaywrightController>();
        services.AddScoped<FingerprintManager>();
        services.AddScoped<Engine.TaskScheduler>();

        return services;
    }

    /// <summary>
    /// 添加所有服务 (一键配置)
    /// </summary>
    public static IServiceCollection AddAllServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDatabaseServices(configuration);
        services.AddDomainServices();
        services.AddApplicationServices();
        services.AddViewModels();
        services.AddEngineServices();

        return services;
    }
}
