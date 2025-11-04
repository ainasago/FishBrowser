using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace FishBrowser.WPF.Services;

/// <summary>
/// Playwright 浏览器自动安装服务
/// </summary>
public class PlaywrightInstaller
{
    private static readonly Serilog.ILogger _logger = Log.ForContext(typeof(PlaywrightInstaller));

    /// <summary>
    /// 检查并安装 Playwright 浏览器
    /// </summary>
    public static async Task EnsurePlaywrightInstalledAsync()
    {
        try
        {
            _logger.Information("Checking Playwright installation...");

            // 检查 Chromium 是否已安装
            var chromiumPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ms-playwright"
            );

            if (Directory.Exists(chromiumPath))
            {
                var chromiumExe = Directory.GetFiles(chromiumPath, "chrome.exe", SearchOption.AllDirectories);
                if (chromiumExe.Length > 0)
                {
                    _logger.Information("Playwright is already installed at {Path}", chromiumExe[0]);
                    return;
                }
            }

            _logger.Information("Playwright not found. Starting installation...");
            await InstallPlaywrightAsync();
            _logger.Information("Playwright installation completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to ensure Playwright installation");
            throw;
        }
    }

    /// <summary>
    /// 安装 Playwright 浏览器
    /// </summary>
    private static async Task InstallPlaywrightAsync()
    {
        try
        {
            // 方法 1: 使用 dotnet tool
            _logger.Information("Attempting to install Playwright using dotnet tool...");
            
            var processInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "tool install --global Microsoft.Playwright.CLI",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode == 0 || process.ExitCode == 1) // 1 = already installed
                    {
                        _logger.Information("Dotnet tool installed successfully");
                    }
                }
            }

            // 方法 2: 使用 playwright install 命令
            _logger.Information("Running playwright install command...");
            
            var installInfo = new ProcessStartInfo
            {
                FileName = "playwright",
                Arguments = "install",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(installInfo))
            {
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    
                    await process.WaitForExitAsync();

                    if (!string.IsNullOrEmpty(output))
                    {
                        _logger.Information("Playwright install output: {Output}", output);
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        _logger.Warning("Playwright install error: {Error}", error);
                    }

                    if (process.ExitCode == 0)
                    {
                        _logger.Information("Playwright installed successfully");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during Playwright installation");
            throw;
        }
    }
}
