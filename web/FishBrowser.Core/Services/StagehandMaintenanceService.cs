using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Services;

/// <summary>
/// Stagehand AI 浏览器自动化框架维护服务
/// 负责 Stagehand 的安装、更新、卸载和状态检查
/// </summary>
public class StagehandMaintenanceService
{
    private readonly LogService _logService;

    public StagehandMaintenanceService(LogService logService)
    {
        _logService = logService;
    }

    /// <summary>
    /// 获取 Stagehand 安装路径（全局 node_modules）
    /// </summary>
    private static string GetStagehandGlobalPath()
    {
        // npm global path: npm config get prefix
        // 通常在 Windows: %APPDATA%\npm\node_modules\@browserbasehq\stagehand
        // Linux/macOS: /usr/local/lib/node_modules/@browserbasehq/stagehand
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "npm", "node_modules", "@browserbasehq", "stagehand");
        }
        else
        {
            return "/usr/local/lib/node_modules/@browserbasehq/stagehand";
        }
    }

    /// <summary>
    /// 获取 Stagehand 状态
    /// </summary>
    public async Task<StagehandStatus> GetStatusAsync()
    {
        var status = new StagehandStatus();
        try
        {
            // 检查 Node.js 是否安装
            status.NodeVersion = await GetNodeVersionAsync();
            status.IsNodeInstalled = !string.IsNullOrEmpty(status.NodeVersion);

            if (!status.IsNodeInstalled)
            {
                _logService.LogWarn("StagehandMaintenance", "Node.js is not installed");
                return status;
            }

            // 检查 npm 版本
            status.NpmVersion = await GetNpmVersionAsync();

            // 检查 Stagehand 是否安装
            var stagehandPath = GetStagehandGlobalPath();
            status.IsInstalled = Directory.Exists(stagehandPath);
            status.InstallPath = status.IsInstalled ? stagehandPath : null;

            if (status.IsInstalled)
            {
                // 读取 package.json 获取版本
                var packageJsonPath = Path.Combine(stagehandPath, "package.json");
                if (File.Exists(packageJsonPath))
                {
                    var packageJson = await File.ReadAllTextAsync(packageJsonPath);
                    var versionMatch = Regex.Match(packageJson, "\"version\":\\s*\"([^\"]+)\"");
                    if (versionMatch.Success)
                    {
                        status.InstalledVersion = versionMatch.Groups[1].Value;
                    }
                }
            }

            // 检查 Playwright 依赖（Stagehand 依赖 Playwright）
            status.PlaywrightInstalled = await CheckPlaywrightInstalledAsync();

            // 暂时跳过获取最新版本（避免阻塞）
            // 可以在安装/更新时再获取
            status.LatestVersion = "检查中...";

            _logService.LogInfo("StagehandMaintenance", $"Status: Installed={status.IsInstalled}, Version={status.InstalledVersion}");
        }
        catch (Exception ex)
        {
            _logService.LogError("StagehandMaintenance", $"Failed to get status: {ex.Message}", ex.StackTrace);
        }
        return status;
    }

    /// <summary>
    /// 安装 Stagehand
    /// </summary>
    public async Task InstallAsync()
    {
        try
        {
            _logService.LogInfo("StagehandMaintenance", "Starting Stagehand installation...");

            // 检查 Node.js
            var nodeVersion = await GetNodeVersionAsync();
            if (string.IsNullOrEmpty(nodeVersion))
            {
                throw new Exception("Node.js is not installed. Please install Node.js first from https://nodejs.org/");
            }

            _logService.LogInfo("StagehandMaintenance", $"Node.js version: {nodeVersion}");

            // 安装 Stagehand 全局包
            _logService.LogInfo("StagehandMaintenance", "Installing @browserbasehq/stagehand globally...");
            await RunNpmCommandAsync("install -g @browserbasehq/stagehand");

            // 检查 Playwright 是否已安装
            var playwrightInstalled = await CheckPlaywrightInstalledAsync();
            if (playwrightInstalled)
            {
                _logService.LogInfo("StagehandMaintenance", "Playwright is already installed, skipping browser installation");
            }
            else
            {
                // 安装 Playwright 浏览器
                _logService.LogInfo("StagehandMaintenance", "Installing Playwright browsers...");
                await RunNpxCommandAsync("playwright install");
            }

            _logService.LogInfo("StagehandMaintenance", "Stagehand installation completed successfully");
        }
        catch (Exception ex)
        {
            _logService.LogError("StagehandMaintenance", $"Installation failed: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// 更新 Stagehand
    /// </summary>
    public async Task UpdateAsync()
    {
        try
        {
            _logService.LogInfo("StagehandMaintenance", "Starting Stagehand update...");

            // 更新 Stagehand 全局包
            _logService.LogInfo("StagehandMaintenance", "Updating @browserbasehq/stagehand...");
            await RunNpmCommandAsync("update -g @browserbasehq/stagehand");

            // 检查 Playwright 是否已安装
            var playwrightInstalled = await CheckPlaywrightInstalledAsync();
            if (playwrightInstalled)
            {
                // 更新 Playwright 浏览器
                _logService.LogInfo("StagehandMaintenance", "Updating Playwright browsers...");
                await RunNpxCommandAsync("playwright install");
            }
            else
            {
                _logService.LogInfo("StagehandMaintenance", "Playwright not installed, skipping browser update");
            }

            _logService.LogInfo("StagehandMaintenance", "Stagehand update completed successfully");
        }
        catch (Exception ex)
        {
            _logService.LogError("StagehandMaintenance", $"Update failed: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// 卸载 Stagehand
    /// </summary>
    public async Task UninstallAsync()
    {
        try
        {
            _logService.LogInfo("StagehandMaintenance", "Starting Stagehand uninstallation...");

            // 卸载全局包
            _logService.LogInfo("StagehandMaintenance", "Uninstalling @browserbasehq/stagehand...");
            await RunNpmCommandAsync("uninstall -g @browserbasehq/stagehand");

            // 可选：清理安装目录
            var stagehandPath = GetStagehandGlobalPath();
            if (Directory.Exists(stagehandPath))
            {
                try
                {
                    Directory.Delete(stagehandPath, true);
                    _logService.LogInfo("StagehandMaintenance", $"Deleted directory: {stagehandPath}");
                }
                catch (Exception ex)
                {
                    _logService.LogWarn("StagehandMaintenance", $"Failed to delete directory: {ex.Message}");
                }
            }

            _logService.LogInfo("StagehandMaintenance", "Stagehand uninstallation completed successfully");
        }
        catch (Exception ex)
        {
            _logService.LogError("StagehandMaintenance", $"Uninstallation failed: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// 测试 Stagehand 连接
    /// </summary>
    public async Task<bool> TestStagehandAsync()
    {
        try
        {
            _logService.LogInfo("StagehandMaintenance", "Testing Stagehand connection...");

            // 测试方法：检查 npm list 中是否有 stagehand
            try
            {
                var output = await RunCommandAsync("npm", "list -g @browserbasehq/stagehand --depth=0");
                _logService.LogInfo("StagehandMaintenance", $"npm list output: [{output}]");
                
                if (!string.IsNullOrEmpty(output) && output.Contains("@browserbasehq/stagehand"))
                {
                    _logService.LogInfo("StagehandMaintenance", $"Stagehand test passed - found in global packages");
                    return true;
                }
                else
                {
                    // 备用方法：检查安装目录
                    var stagehandPath = GetStagehandGlobalPath();
                    if (Directory.Exists(stagehandPath))
                    {
                        _logService.LogInfo("StagehandMaintenance", $"Stagehand test passed - found directory: {stagehandPath}");
                        return true;
                    }
                    
                    _logService.LogWarn("StagehandMaintenance", $"Stagehand test failed - not found in global packages or directory. Output: [{output}]");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logService.LogError("StagehandMaintenance", $"Stagehand test failed: {ex.Message}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logService.LogError("StagehandMaintenance", $"Test failed: {ex.Message}", ex.StackTrace);
            return false;
        }
    }

    #region Helper Methods

    private async Task<string?> GetNodeVersionAsync()
    {
        try
        {
            // 尝试多个可能的路径
            var commands = new[] { "node", "node.exe" };
            foreach (var cmd in commands)
            {
                var output = await RunCommandAsync(cmd, "--version");
                if (!string.IsNullOrEmpty(output))
                    return output.Trim();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> GetNpmVersionAsync()
    {
        try
        {
            // 尝试多个可能的路径
            var commands = new[] { "npm", "npm.cmd", "npm.exe" };
            foreach (var cmd in commands)
            {
                var output = await RunCommandAsync(cmd, "--version");
                if (!string.IsNullOrEmpty(output))
                    return output.Trim();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> GetLatestVersionAsync()
    {
        try
        {
            var output = await RunCommandAsync("npm", "view @browserbasehq/stagehand version");
            return output?.Trim();
        }
        catch (Exception ex)
        {
            _logService.LogWarn("StagehandMaintenance", $"Failed to get latest version: {ex.Message}");
            return null;
        }
    }

    private async Task<bool> CheckPlaywrightInstalledAsync()
    {
        try
        {
            // 优先使用快速的目录检查（不需要执行命令）
            var playwrightPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ms-playwright")
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "ms-playwright");

            if (Directory.Exists(playwrightPath))
            {
                _logService.LogInfo("StagehandMaintenance", $"Playwright directory found: {playwrightPath}");
                return true;
            }

            // 如果目录不存在，尝试命令检查（可能较慢，但更准确）
            // 注意：这里可能会卡住，所以放在最后
            try
            {
                var output = await RunCommandAsync("npx", "playwright --version");
                if (!string.IsNullOrEmpty(output))
                {
                    _logService.LogInfo("StagehandMaintenance", $"Playwright detected via command: {output.Trim()}");
                    return true;
                }
            }
            catch
            {
                // 命令执行失败，忽略
            }

            return false;
        }
        catch (Exception ex)
        {
            _logService.LogWarn("StagehandMaintenance", $"Failed to check Playwright: {ex.Message}");
            return false;
        }
    }

    private async Task RunNpmCommandAsync(string arguments)
    {
        // Windows 上 npm 是批处理文件，需要通过 cmd 执行
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            await RunProcessAsync("cmd.exe", $"/c npm {arguments}");
        }
        else
        {
            await RunProcessAsync("npm", arguments);
        }
    }

    private async Task RunNpxCommandAsync(string arguments)
    {
        // Windows 上 npx 是批处理文件，需要通过 cmd 执行
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            await RunProcessAsync("cmd.exe", $"/c npx {arguments}");
        }
        else
        {
            await RunProcessAsync("npx", arguments);
        }
    }

    private async Task RunNodeCommandAsync(string scriptPath)
    {
        await RunProcessAsync("node", $"\"{scriptPath}\"");
    }

    private async Task<string?> RunCommandAsync(string fileName, string arguments)
    {
        try
        {
            // Windows 上对于 npm/npx 等批处理文件，需要通过 cmd 执行
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && 
                (fileName == "npm" || fileName == "npx" || fileName == "npm.cmd"))
            {
                fileName = "cmd.exe";
                arguments = $"/c {(fileName == "npm" ? "npm" : "npx")} {arguments}";
            }

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return process.ExitCode == 0 ? output : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task RunProcessAsync(string fileName, string arguments)
    {
        _logService.LogInfo("StagehandMaintenance", $"Executing: {fileName} {arguments}");

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            var errorMsg = $"Failed to start process: {fileName} {arguments}";
            _logService.LogError("StagehandMaintenance", errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        // 记录所有输出
        if (!string.IsNullOrWhiteSpace(output))
        {
            _logService.LogInfo("StagehandMaintenance", $"Output: {output.Trim()}");
        }
        if (!string.IsNullOrWhiteSpace(error))
        {
            _logService.LogError("StagehandMaintenance", $"Error: {error.Trim()}");
        }

        // 检查退出码
        if (process.ExitCode != 0)
        {
            var errorMsg = $"Process '{fileName} {arguments}' failed with exit code {process.ExitCode}";
            if (!string.IsNullOrWhiteSpace(error))
            {
                errorMsg += $"\nError output: {error.Trim()}";
            }
            _logService.LogError("StagehandMaintenance", errorMsg);
            throw new Exception(errorMsg);
        }

        _logService.LogInfo("StagehandMaintenance", $"Process completed successfully: {fileName}");
    }

    #endregion
}
