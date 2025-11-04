using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FishBrowser.WPF.Services;
using WpfApplication = System.Windows.Application;

namespace FishBrowser.WPF.Views;

public partial class HelpView : Page
{
    private readonly LogService _logService;

    public HelpView()
    {
        InitializeComponent();

        // 从 DI 容器获取服务
        var host = WpfApplication.Current.Resources["Host"] as IHost;
        _logService = host?.Services.GetRequiredService<LogService>() ?? throw new InvalidOperationException("LogService not found");

        // 设置系统信息
        SetSystemInfo();
        
        // 检查 Playwright 状态
        CheckPlaywrightStatus();
    }

    private void SetSystemInfo()
    {
        try
        {
            // 设置版本信息
            VersionText.Text = "v1.0";
            
            // 设置日志路径
            var currentDir = Directory.GetCurrentDirectory();
            var logsDir = Path.Combine(currentDir, "logs");
            if (currentDir.Contains("bin"))
            {
                var projectRoot = Directory.GetParent(currentDir)?.Parent?.Parent?.FullName ?? currentDir;
                logsDir = Path.Combine(projectRoot, "logs");
            }
            LogPathText.Text = Path.Combine(logsDir, "webscraper.log");
        }
        catch (Exception ex)
        {
            _logService.LogError("HelpView", $"Failed to set system info: {ex.Message}", ex.StackTrace);
        }
    }

    private void CommandTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    private void CopyCommand1_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(Command1TextBox.Text);
            MessageBox.Show("命令已复制到剪贴板！", "复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
            _logService.LogInfo("HelpView", "Command 1 copied to clipboard");
        }
        catch (Exception ex)
        {
            _logService.LogError("HelpView", $"Failed to copy command 1: {ex.Message}", ex.StackTrace);
            MessageBox.Show("复制失败，请手动选择文字复制。", "复制失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CopyCommand2_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(Command2TextBox.Text);
            MessageBox.Show("命令已复制到剪贴板！", "复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
            _logService.LogInfo("HelpView", "Command 2 copied to clipboard");
        }
        catch (Exception ex)
        {
            _logService.LogError("HelpView", $"Failed to copy command 2: {ex.Message}", ex.StackTrace);
            MessageBox.Show("复制失败，请手动选择文字复制。", "复制失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void CheckPlaywrightStatus()
    {
        try
        {
            InstallStatusText.Text = "检查中...";
            VersionInfoText.Text = "检查中...";
            InstallPathText.Text = "检查中...";
            BrowserListText.Text = "检查中...";

            await Task.Run(() =>
            {
                // 检查安装状态
                var playwrightPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ms-playwright"
                );

                Dispatcher.Invoke(() =>
                {
                    if (Directory.Exists(playwrightPath))
                    {
                        InstallStatusText.Text = "✅ 已安装";
                        InstallStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80));
                        
                        InstallPathText.Text = playwrightPath;
                        
                        // 获取浏览器列表
                        var browsers = Directory.GetDirectories(playwrightPath);
                        var browserNames = browsers.Select(Path.GetFileName).ToArray();
                        BrowserListText.Text = string.Join(", ", browserNames);
                    }
                    else
                    {
                        InstallStatusText.Text = "❌ 未安装";
                        InstallStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54));
                        
                        InstallPathText.Text = "未找到";
                        BrowserListText.Text = "无";
                    }
                });

                // 检查版本信息
                try
                {
                    // 方法1: 尝试从项目文件获取 Playwright 版本
                    var rootPath = GetProjectRootPath();
                    var csprojPath = Path.Combine(rootPath, "FishBrowser.WPF.csproj");
                    
                    string packageVersion = null;
                    if (File.Exists(csprojPath))
                    {
                        var csprojContent = File.ReadAllText(csprojPath);
                        var versionMatch = System.Text.RegularExpressions.Regex.Match(csprojContent, @"<PackageReference Include=""Microsoft\.Playwright"" Version=""([^""]+)""");
                        if (versionMatch.Success)
                        {
                            packageVersion = versionMatch.Groups[1].Value;
                        }
                    }

                    // 方法2: 尝试获取 CLI 版本
                    string cliVersion = null;
                    try
                    {
                        var processInfo = new ProcessStartInfo
                        {
                            FileName = "playwright",
                            Arguments = "--version",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                            WorkingDirectory = rootPath
                        };

                        using (var process = Process.Start(processInfo))
                        {
                            if (process != null)
                            {
                                var output = process.StandardOutput.ReadToEnd();
                                var error = process.StandardError.ReadToEnd();
                                process.WaitForExit();
                                
                                _logService.LogInfo("HelpView", $"Playwright CLI output: '{output}', Error: '{error}', Exit code: {process.ExitCode}");
                                
                                if (!string.IsNullOrEmpty(output))
                                {
                                    cliVersion = output.Trim();
                                }
                                else if (!string.IsNullOrEmpty(error))
                                {
                                    _logService.LogInfo("HelpView", $"Playwright CLI error: {error}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError("HelpView", $"Failed to get CLI version: {ex.Message}");
                        // CLI 可能未安装，忽略错误
                    }

                    Dispatcher.Invoke(() =>
                    {
                        if (!string.IsNullOrEmpty(packageVersion) && !string.IsNullOrEmpty(cliVersion))
                        {
                            VersionInfoText.Text = $"{packageVersion} (CLI: {cliVersion})";
                        }
                        else if (!string.IsNullOrEmpty(packageVersion))
                        {
                            VersionInfoText.Text = $"{packageVersion} (CLI 未安装)";
                        }
                        else if (!string.IsNullOrEmpty(cliVersion))
                        {
                            VersionInfoText.Text = cliVersion;
                        }
                        else
                        {
                            VersionInfoText.Text = "无法获取版本信息";
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logService.LogError("HelpView", $"Error checking version: {ex.Message}");
                    Dispatcher.Invoke(() =>
                    {
                        VersionInfoText.Text = "版本检查失败";
                    });
                }
            });
        }
        catch (Exception ex)
        {
            _logService.LogError("HelpView", $"Failed to check Playwright status: {ex.Message}", ex.StackTrace);
            
            InstallStatusText.Text = "❌ 检查失败";
            VersionInfoText.Text = "检查失败";
            InstallPathText.Text = "检查失败";
            BrowserListText.Text = "检查失败";
        }
    }

    private void RefreshStatus_Click(object sender, RoutedEventArgs e)
    {
        CheckPlaywrightStatus();
    }

    private void UpdateProgress(int percentage, string status, string detail = "")
    {
        Dispatcher.Invoke(() =>
        {
            InstallProgressBar.Value = percentage;
            ProgressText.Text = status;
            ProgressDetailText.Text = detail;
        });
    }

    private void ShowProgress(bool show)
    {
        Dispatcher.Invoke(() =>
        {
            ProgressBorder.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        });
    }

    private async void InstallPlaywright_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 禁用所有操作按钮
            SetButtonsEnabled(false);
            ShowProgress(true);
            UpdateProgress(0, "开始安装...", "准备安装 Playwright 浏览器");

            _logService.LogInfo("HelpView", "Starting Playwright installation from help page");

            // 执行安装
            await InstallPlaywrightAsync();

            // 安装完成
            UpdateProgress(100, "安装完成！", "Playwright 浏览器安装成功");
            await Task.Delay(2000); // 显示完成状态2秒
            ShowProgress(false);

            MessageBox.Show("Playwright 安装完成！\n\n请重新运行应用并点击[运行测试]按钮验证安装结果。", 
                          "安装成功", MessageBoxButton.OK, MessageBoxImage.Information);

            _logService.LogInfo("HelpView", "Playwright installation completed successfully");
            
            // 刷新状态
            CheckPlaywrightStatus();
        }
        catch (Exception ex)
        {
            _logService.LogError("HelpView", $"Playwright installation failed: {ex.Message}", ex.StackTrace);
            
            UpdateProgress(0, "安装失败", ex.Message);
            await Task.Delay(3000); // 显示错误状态3秒
            ShowProgress(false);
            
            MessageBox.Show($"安装失败: {ex.Message}\n\n请尝试手动安装：\n1. 打开 PowerShell\n2. 运行: playwright install", 
                          "安装失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            // 恢复按钮状态
            SetButtonsEnabled(true);
        }
    }

    private async void UpdatePlaywright_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 禁用所有操作按钮
            SetButtonsEnabled(false);
            ShowProgress(true);
            UpdateProgress(10, "检查版本...", "正在检查最新版本");

            _logService.LogInfo("HelpView", "Starting Playwright update check from help page");

            // 获取当前版本和最新版本
            var currentVersion = GetCurrentVersion();
            var latestVersion = await GetLatestPlaywrightVersionAsync();

            if (string.IsNullOrEmpty(latestVersion))
            {
                UpdateProgress(0, "检查失败", "无法获取最新版本信息");
                await Task.Delay(3000);
                ShowProgress(false);
                MessageBox.Show("无法获取最新版本信息，请检查网络连接。", 
                              "检查失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 比较版本
            if (IsLatestVersion(currentVersion, latestVersion))
            {
                UpdateProgress(100, "已是最新版本", $"当前版本 {currentVersion} 已是最新版本");
                await Task.Delay(3000);
                ShowProgress(false);
                MessageBox.Show($"当前版本 {currentVersion} 已是最新版本，无需更新。", 
                              "已是最新版本", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 确认更新
            var result = MessageBox.Show(
                $"发现新版本：{latestVersion}\n当前版本：{currentVersion}\n\n是否立即更新？",
                "发现新版本", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes)
            {
                ShowProgress(false);
                return;
            }

            // 执行更新
            UpdateProgress(30, "开始更新...", "准备更新 Playwright 浏览器");
            await UpdatePlaywrightAsync();

            // 更新完成
            UpdateProgress(100, "更新完成！", $"Playwright 已更新到版本 {latestVersion}");
            await Task.Delay(2000);
            ShowProgress(false);

            MessageBox.Show($"Playwright 已成功更新到版本 {latestVersion}！", 
                          "更新成功", MessageBoxButton.OK, MessageBoxImage.Information);

            _logService.LogInfo("HelpView", "Playwright update completed successfully");
            
            // 刷新状态
            CheckPlaywrightStatus();
        }
        catch (Exception ex)
        {
            _logService.LogError("HelpView", $"Playwright update failed: {ex.Message}", ex.StackTrace);
            
            UpdateProgress(0, "更新失败", ex.Message);
            await Task.Delay(3000);
            ShowProgress(false);
            
            MessageBox.Show($"更新失败: {ex.Message}", 
                          "更新失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    private string GetCurrentVersion()
    {
        try
        {
            var projectPath = GetProjectRootPath();
            var csprojPath = Path.Combine(projectPath, "FishBrowser.WPF.csproj");
            
            if (File.Exists(csprojPath))
            {
                var csprojContent = File.ReadAllText(csprojPath);
                var versionMatch = System.Text.RegularExpressions.Regex.Match(csprojContent, @"<PackageReference Include=""Microsoft\.Playwright"" Version=""([^""]+)""");
                if (versionMatch.Success)
                {
                    return versionMatch.Groups[1].Value;
                }
            }
        }
        catch (Exception ex)
        {
            _logService.LogError("HelpView", $"Failed to get current version: {ex.Message}");
        }
        return "未知";
    }

    private bool IsLatestVersion(string current, string latest)
    {
        if (string.IsNullOrEmpty(current) || string.IsNullOrEmpty(latest))
            return false;

        try
        {
            var currentVersion = new Version(current);
            var latestVersion = new Version(latest);
            return currentVersion >= latestVersion;
        }
        catch
        {
            return current == latest;
        }
    }

    private async void UninstallPlaywright_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "确定要卸载 Playwright 浏览器吗？\n\n这将删除所有已下载的浏览器文件，WebScraper 将无法正常工作。",
            "确认卸载", 
            MessageBoxButton.YesNo, 
            MessageBoxImage.Warning
        );

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            // 禁用所有操作按钮
            SetButtonsEnabled(false);
            ShowProgress(true);
            UpdateProgress(0, "开始卸载...", "准备卸载 Playwright 浏览器");

            _logService.LogInfo("HelpView", "Starting Playwright uninstallation from help page");

            // 执行卸载
            await UninstallPlaywrightAsync();

            // 卸载完成
            UpdateProgress(100, "卸载完成！", "Playwright 浏览器已成功卸载");
            await Task.Delay(2000);
            ShowProgress(false);

            MessageBox.Show("Playwright 卸载完成！\n\n如需重新使用，请点击[安装]按钮重新安装。", 
                          "卸载成功", MessageBoxButton.OK, MessageBoxImage.Information);

            _logService.LogInfo("HelpView", "Playwright uninstallation completed successfully");
            
            // 刷新状态
            CheckPlaywrightStatus();
        }
        catch (Exception ex)
        {
            _logService.LogError("HelpView", $"Playwright uninstallation failed: {ex.Message}", ex.StackTrace);
            
            UpdateProgress(0, "卸载失败", ex.Message);
            await Task.Delay(3000);
            ShowProgress(false);
            
            MessageBox.Show($"卸载失败: {ex.Message}", 
                          "卸载失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    private string GetProjectRootPath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        if (currentDir.Contains("bin"))
        {
            return Directory.GetParent(currentDir)?.Parent?.Parent?.FullName ?? currentDir;
        }
        return currentDir;
    }

    private async Task<string> GetLatestPlaywrightVersionAsync()
    {
        try
        {
            // 使用 NuGet API 获取最新版本
            using (var httpClient = new System.Net.Http.HttpClient())
            {
                var response = await httpClient.GetStringAsync("https://api.nuget.org/v3-flatcontainer/microsoft.playwright/index.json");
                var json = System.Text.Json.JsonDocument.Parse(response);
                var latestVersion = json.RootElement.GetProperty("versions").EnumerateArray().LastOrDefault().GetString();
                return latestVersion;
            }
        }
        catch (Exception ex)
        {
            _logService.LogError("HelpView", $"Failed to get latest version: {ex.Message}");
            return null;
        }
    }

    private void SetButtonsEnabled(bool enabled)
    {
        Dispatcher.Invoke(() =>
        {
            InstallPlaywrightButton.IsEnabled = enabled;
            UpdatePlaywrightButton.IsEnabled = enabled;
            UninstallPlaywrightButton.IsEnabled = enabled;
            RefreshStatusButton.IsEnabled = enabled;
            
            if (enabled)
            {
                InstallPlaywrightButton.Content = "安装";
                UpdatePlaywrightButton.Content = "更新";
                UninstallPlaywrightButton.Content = "卸载";
            }
        });
    }

    private async Task InstallPlaywrightAsync()
    {
        try
        {
            // 方法 1: 使用 dotnet tool
            UpdateProgress(10, "安装 CLI 工具...", "正在安装 Playwright CLI 工具");
            _logService.LogInfo("HelpView", "Installing Playwright using dotnet tool...");
            
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
                    _logService.LogInfo("HelpView", $"Dotnet tool install exit code: {process.ExitCode}");
                }
            }

            // 方法 2: 使用 playwright install
            UpdateProgress(30, "下载浏览器...", "正在下载 Playwright 浏览器");
            _logService.LogInfo("HelpView", "Running playwright install...");
            
            // 获取项目根目录
            var currentDir = Directory.GetCurrentDirectory();
            var projectRoot = currentDir;
            if (currentDir.Contains("bin"))
            {
                projectRoot = Directory.GetParent(currentDir)?.Parent?.Parent?.FullName ?? currentDir;
            }
            
            var installInfo = new ProcessStartInfo
            {
                FileName = "playwright",
                Arguments = "install",
                WorkingDirectory = projectRoot,  // 设置工作目录为项目根目录
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(installInfo))
            {
                if (process != null)
                {
                    UpdateProgress(50, "安装浏览器...", "正在安装浏览器文件");
                    
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    
                    await process.WaitForExitAsync();

                    _logService.LogInfo("HelpView", $"Playwright install exit code: {process.ExitCode}");
                    
                    if (!string.IsNullOrEmpty(output))
                    {
                        _logService.LogInfo("HelpView", $"Playwright install output: {output}");
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        _logService.LogWarn("HelpView", $"Playwright install error: {error}");
                    }

                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"Playwright install failed with exit code {process.ExitCode}");
                    }
                }
            }

            // 验证安装
            UpdateProgress(90, "验证安装...", "正在验证安装结果");
            
            var chromiumPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ms-playwright",
                "chromium-1091",
                "chrome-win",
                "chrome.exe"
            );

            if (!File.Exists(chromiumPath))
            {
                throw new Exception("Playwright installation completed but chromium executable not found");
            }

            _logService.LogInfo("HelpView", $"Playwright verified at: {chromiumPath}");
            UpdateProgress(100, "安装完成！", "Playwright 浏览器安装成功");
        }
        catch (Exception ex)
        {
            _logService.LogError("HelpView", $"Error during Playwright installation: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    private async Task UpdatePlaywrightAsync()
    {
        try
        {
            UpdateProgress(10, "更新 CLI 工具...", "正在更新 Playwright CLI 工具");
            _logService.LogInfo("HelpView", "Updating Playwright CLI tool...");
            
            var processInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "tool update --global Microsoft.Playwright.CLI",
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
                    _logService.LogInfo("HelpView", $"Dotnet tool update exit code: {process.ExitCode}");
                }
            }

            UpdateProgress(30, "更新浏览器...", "正在更新 Playwright 浏览器");
            _logService.LogInfo("HelpView", "Updating playwright browsers...");
            
            // 获取项目根目录
            var currentDir = Directory.GetCurrentDirectory();
            var projectRoot = currentDir;
            if (currentDir.Contains("bin"))
            {
                projectRoot = Directory.GetParent(currentDir)?.Parent?.Parent?.FullName ?? currentDir;
            }
            
            var updateInfo = new ProcessStartInfo
            {
                FileName = "playwright",
                Arguments = "install",
                WorkingDirectory = projectRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(updateInfo))
            {
                if (process != null)
                {
                    UpdateProgress(60, "下载更新...", "正在下载浏览器更新");
                    
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    
                    await process.WaitForExitAsync();

                    _logService.LogInfo("HelpView", $"Playwright update exit code: {process.ExitCode}");
                    
                    if (!string.IsNullOrEmpty(output))
                    {
                        _logService.LogInfo("HelpView", $"Playwright update output: {output}");
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        _logService.LogWarn("HelpView", $"Playwright update error: {error}");
                    }

                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"Playwright update failed with exit code {process.ExitCode}");
                    }
                }
            }

            UpdateProgress(100, "更新完成！", "Playwright 浏览器更新成功");
        }
        catch (Exception ex)
        {
            _logService.LogError("HelpView", $"Error during Playwright update: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    private async Task UninstallPlaywrightAsync()
    {
        try
        {
            UpdateProgress(20, "删除浏览器文件...", "正在删除 Playwright 浏览器文件");
            _logService.LogInfo("HelpView", "Uninstalling Playwright browsers...");
            
            var playwrightPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ms-playwright"
            );

            if (Directory.Exists(playwrightPath))
            {
                // 删除整个 ms-playwright 目录
                Directory.Delete(playwrightPath, true);
                _logService.LogInfo("HelpView", $"Deleted Playwright directory: {playwrightPath}");
            }

            UpdateProgress(60, "卸载 CLI 工具...", "正在卸载 Playwright CLI 工具");
            _logService.LogInfo("HelpView", "Uninstalling Playwright CLI tool...");
            
            var processInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "tool uninstall --global Microsoft.Playwright.CLI",
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
                    _logService.LogInfo("HelpView", $"Dotnet tool uninstall exit code: {process.ExitCode}");
                }
            }

            UpdateProgress(100, "卸载完成！", "Playwright 浏览器已成功卸载");
        }
        catch (Exception ex)
        {
            _logService.LogError("HelpView", $"Error during Playwright uninstallation: {ex.Message}", ex.StackTrace);
            throw;
        }
    }
}
