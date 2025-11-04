using System.Diagnostics;
using System.Runtime.InteropServices;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;
using FishBrowser.WPF.Engine;
using Microsoft.EntityFrameworkCore;

namespace FishBrowser.Api.Services;

public class BrowserLaunchService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BrowserLaunchService> _logger;
    private readonly Dictionary<int, Process> _runningBrowsers = new();
    private readonly Dictionary<int, BrowserControllerAdapter> _runningBrowserControllers = new(); // 追踪通过 BrowserControllerAdapter 启动的浏览器实例

    public BrowserLaunchService(IServiceScopeFactory scopeFactory, ILogger<BrowserLaunchService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<(bool success, string message, int? processId)> LaunchBrowserAsync(int browserId)
    {
        try
        {
            BrowserEnvironment browser;
            FingerprintProfile? profile;
            
            // 使用 scope 访问 DbContext 和服务
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<WebScraperDbContext>();
                browser = await context.BrowserEnvironments.FindAsync(browserId);
                
                if (browser == null)
                {
                    return (false, "浏览器不存在", null);
                }

                // 获取指纹配置
                profile = await context.FingerprintProfiles.FirstOrDefaultAsync(p => p.Id == browser.FingerprintProfileId);
                if (profile == null)
                {
                    return (false, "未找到指纹配置，请重新编辑浏览器", null);
                }
            }

            // 检查是否已经在运行
            if (_runningBrowsers.ContainsKey(browserId))
            {
                var existingProcess = _runningBrowsers[browserId];
                if (!existingProcess.HasExited)
                {
                    return (false, "浏览器已在运行中", existingProcess.Id);
                }
                _runningBrowsers.Remove(browserId);
            }
            
            // 检查通过 BrowserControllerAdapter 启动的浏览器
            if (_runningBrowserControllers.ContainsKey(browserId))
            {
                return (false, "浏览器已在运行中", null);
            }

            // 使用与 WPF 相同的启动逻辑
            var controller = await LaunchUsingBrowserControllerAsync(browser, profile);
            
            // 保存控制器实例并启动监控任务
            _runningBrowserControllers[browserId] = controller;
            
            // 启动后台任务监控浏览器关闭
            _ = Task.Run(async () => await MonitorBrowserCloseAsync(browserId, controller));

            // 更新启动统计
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<WebScraperDbContext>();
                var browserToUpdate = await context.BrowserEnvironments.FindAsync(browserId);
                if (browserToUpdate != null)
                {
                    browserToUpdate.LaunchCount++;
                    browserToUpdate.LastLaunchedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                }
            }

            // 根据引擎显示不同的状态信息
            string engineInfo = browser.Engine switch
            {
                "UndetectedChrome" => "🤖 UndetectedChrome（成功率 90-95%）",
                "Firefox" => "🦊 Firefox",
                "Chromium" => "🌐 Chromium",
                _ => "🤖 UndetectedChrome（成功率 90-95%）"
            };

            _logger.LogInformation("Browser {Id} launched successfully using {Engine}", browserId, engineInfo);
            return (true, $"浏览器 '{browser.Name}' 已启动 | {engineInfo}", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error launching browser {Id}", browserId);
            return (false, $"启动失败: {ex.Message}", null);
        }
    }

    private async Task<BrowserControllerAdapter> LaunchUsingBrowserControllerAsync(BrowserEnvironment env, FingerprintProfile profile)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var logService = scope.ServiceProvider.GetRequiredService<ILogService>();
            var fingerprintService = scope.ServiceProvider.GetRequiredService<FingerprintService>();
            var secretService = scope.ServiceProvider.GetRequiredService<SecretService>();
            var sessionService = scope.ServiceProvider.GetRequiredService<BrowserSessionService>();

            // 调试日志：检查 Profile 中的 Platform 和 UserAgent
            var uaPreview = profile.UserAgent != null && profile.UserAgent.Length > 50 
                ? profile.UserAgent.Substring(0, 50) + "..." 
                : profile.UserAgent ?? "(null)";
            logService.LogInfo("BrowserLaunch", $"Profile loaded: Platform={profile.Platform}, UserAgent={uaPreview}");

            string? userDataPath = null;
            if (env.EnablePersistence)
            {
                userDataPath = sessionService.InitializeSessionPath(env);
            }

            var controller = new BrowserControllerAdapter(logService, fingerprintService, secretService);
            
            // 根据 Engine 设置选择浏览器引擎
            // Firefox 和 Chromium 使用 Playwright，UndetectedChrome 使用 UndetectedChrome
            bool useUndetectedChrome = env.Engine?.Equals("UndetectedChrome", StringComparison.OrdinalIgnoreCase) ?? true;
            controller.SetUseUndetectedChrome(useUndetectedChrome);
            
            // 设置浏览器类型（用于 Playwright）
            if (!useUndetectedChrome)
            {
                string browserType = env.Engine?.Equals("Firefox", StringComparison.OrdinalIgnoreCase) == true ? "firefox" : "chromium";
                controller.SetBrowserType(browserType);
            }

            await controller.InitializeBrowserAsync(profile, proxy: null, headless: false, userDataPath: userDataPath, loadAutoma: false, environment: env);
            
            logService.LogInfo("BrowserLaunch", $"Browser '{env.Name}' launched successfully using {env.Engine ?? "UndetectedChrome"}");
            
            return controller;
        }
    }

    /// <summary>
    /// 监控浏览器关闭事件
    /// </summary>
    private async Task MonitorBrowserCloseAsync(int browserId, BrowserControllerAdapter controller)
    {
        try
        {
            _logger.LogInformation("Started monitoring browser {Id} for closure", browserId);
            
            // 等待浏览器关闭
            await controller.WaitForCloseAsync();
            
            // 浏览器已关闭，从追踪器中移除
            _runningBrowserControllers.Remove(browserId);
            
            _logger.LogInformation("Browser {Id} has been closed by user", browserId);
            
            // 释放控制器资源
            await controller.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring browser {Id} closure", browserId);
            // 发生错误时也要清理
            _runningBrowserControllers.Remove(browserId);
        }
    }

    public Dictionary<int, int> GetRunningBrowsers()
    {
        // 清理已退出的进程
        var toRemove = _runningBrowsers.Where(kvp => kvp.Value.HasExited).Select(kvp => kvp.Key).ToList();
        foreach (var id in toRemove)
        {
            _runningBrowsers.Remove(id);
        }

        var result = new Dictionary<int, int>();
        
        // 添加通过 Process 追踪的浏览器
        foreach (var kvp in _runningBrowsers.Where(kvp => !kvp.Value.HasExited))
        {
            result[kvp.Key] = kvp.Value.Id;
        }
        
        // 添加通过 BrowserControllerAdapter 启动的浏览器（使用 browserId 作为伪进程ID）
        foreach (var kvp in _runningBrowserControllers)
        {
            if (!result.ContainsKey(kvp.Key))
            {
                result[kvp.Key] = kvp.Key; // 使用 browserId 作为进程ID
            }
        }
        
        return result;
    }
    
    public async Task<bool> StopBrowserAsync(int browserId)
    {
        try
        {
            // 检查是否有 BrowserControllerAdapter 实例
            if (_runningBrowserControllers.TryGetValue(browserId, out var controller))
            {
                _runningBrowserControllers.Remove(browserId);
                await controller.DisposeAsync();
                _logger.LogInformation("Browser {Id} stopped via controller", browserId);
                return true;
            }
            
            // 检查是否有 Process 实例
            if (_runningBrowsers.TryGetValue(browserId, out var process))
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                    process.WaitForExit(5000);
                }
                _runningBrowsers.Remove(browserId);
                _logger.LogInformation("Browser {Id} stopped via process", browserId);
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping browser {Id}", browserId);
            return false;
        }
    }
}
