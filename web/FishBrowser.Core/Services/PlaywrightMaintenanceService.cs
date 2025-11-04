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

public class PlaywrightMaintenanceService
{
    private readonly LogService _logService;

    public PlaywrightMaintenanceService(LogService logService)
    {
        _logService = logService;
    }

    private static string GetPlaywrightPath()
    {
        // Linux/macOS: ~/.cache/ms-playwright
        // Windows: %LOCALAPPDATA%\ms-playwright
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ms-playwright"
            );
        }
        else
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".cache", "ms-playwright");
        }
    }

    public async Task<PlaywrightStatus> GetStatusAsync()
    {
        var status = new PlaywrightStatus();
        try
        {
            var playwrightPath = GetPlaywrightPath();
            status.IsInstalled = Directory.Exists(playwrightPath);
            status.InstallPath = status.IsInstalled ? playwrightPath : null;

            if (status.IsInstalled)
            {
                var browsers = Directory.GetDirectories(playwrightPath);
                status.BrowserList = browsers.Select(Path.GetFileName).Where(s => !string.IsNullOrEmpty(s)).ToList()!;
            }

            // Package version from csproj (best-effort)
            try
            {
                var rootPath = GetProjectRootPath();
                var csprojPath = Path.Combine(rootPath, "FishBrowser.WPF.csproj");
                if (File.Exists(csprojPath))
                {
                    var csprojContent = await File.ReadAllTextAsync(csprojPath);
                    var versionMatch = Regex.Match(csprojContent, "<PackageReference Include=\"Microsoft\\.Playwright\" Version=\"([^\"]+)\"");
                    if (versionMatch.Success)
                    {
                        status.PackageVersion = versionMatch.Groups[1].Value;
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogWarn("PlaywrightMaintenanceService", $"Failed to read package version: {ex.Message}");
            }

            // CLI version
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
                    WorkingDirectory = GetProjectRootPath()
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    _logService.LogInfo("PlaywrightMaintenanceService", $"Playwright CLI output: '{output}', Error: '{error}', Exit code: {process.ExitCode}");

                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        status.CliVersion = output.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogWarn("PlaywrightMaintenanceService", $"Failed to get CLI version: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            _logService.LogError("PlaywrightMaintenanceService", $"Failed to get status: {ex.Message}", ex.StackTrace);
        }
        return status;
    }

    public async Task<string?> GetLatestPlaywrightVersionAsync()
    {
        try
        {
            using var httpClient = new System.Net.Http.HttpClient();
            var response = await httpClient.GetStringAsync("https://api.nuget.org/v3-flatcontainer/microsoft.playwright/index.json");
            var json = System.Text.Json.JsonDocument.Parse(response);
            var latestVersion = json.RootElement.GetProperty("versions").EnumerateArray().LastOrDefault().GetString();
            return latestVersion;
        }
        catch (Exception ex)
        {
            _logService.LogError("PlaywrightMaintenanceService", $"Failed to get latest version: {ex.Message}");
            return null;
        }
    }

    public string GetCurrentPackageVersion()
    {
        try
        {
            var projectPath = GetProjectRootPath();
            var csprojPath = Path.Combine(projectPath, "FishBrowser.WPF.csproj");
            if (File.Exists(csprojPath))
            {
                var csprojContent = File.ReadAllText(csprojPath);
                var versionMatch = Regex.Match(csprojContent, "<PackageReference Include=\"Microsoft\\.Playwright\" Version=\"([^\"]+)\"");
                if (versionMatch.Success)
                {
                    return versionMatch.Groups[1].Value;
                }
            }
        }
        catch (Exception ex)
        {
            _logService.LogError("PlaywrightMaintenanceService", $"Failed to get current version: {ex.Message}");
        }
        return "未知";
    }

    public async Task InstallAsync()
    {
        try
        {
            _logService.LogInfo("PlaywrightMaintenanceService", "Installing Playwright CLI tool globally via dotnet tool...");
            await RunProcessAsync("dotnet", "tool install --global Microsoft.Playwright.CLI");

            _logService.LogInfo("PlaywrightMaintenanceService", "Running 'playwright install' to download browsers globally...");
            await RunProcessAsync("playwright", "install");

            _logService.LogInfo("PlaywrightMaintenanceService", "Installing system dependencies (Linux only)...");
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    await RunProcessAsync("playwright", "install-deps");
                }
                catch (Exception ex)
                {
                    _logService.LogWarn("PlaywrightMaintenanceService", $"Failed to install system dependencies (may require sudo): {ex.Message}");
                }
            }

            _logService.LogInfo("PlaywrightMaintenanceService", "Playwright installation completed successfully");
        }
        catch (Exception ex)
        {
            _logService.LogError("PlaywrightMaintenanceService", $"Error during Playwright installation: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task UpdateAsync()
    {
        try
        {
            _logService.LogInfo("PlaywrightMaintenanceService", "Updating Playwright CLI tool globally via dotnet tool...");
            await RunProcessAsync("dotnet", "tool update --global Microsoft.Playwright.CLI");

            _logService.LogInfo("PlaywrightMaintenanceService", "Updating Playwright browsers globally via 'playwright install'...");
            await RunProcessAsync("playwright", "install");

            _logService.LogInfo("PlaywrightMaintenanceService", "Updating system dependencies (Linux only)...");
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    await RunProcessAsync("playwright", "install-deps");
                }
                catch (Exception ex)
                {
                    _logService.LogWarn("PlaywrightMaintenanceService", $"Failed to update system dependencies (may require sudo): {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _logService.LogError("PlaywrightMaintenanceService", $"Error during Playwright update: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task UninstallAsync()
    {
        try
        {
            _logService.LogInfo("PlaywrightMaintenanceService", "Deleting ms-playwright directory...");
            var playwrightPath = GetPlaywrightPath();
            if (Directory.Exists(playwrightPath))
            {
                Directory.Delete(playwrightPath, true);
                _logService.LogInfo("PlaywrightMaintenanceService", $"Deleted: {playwrightPath}");
            }

            _logService.LogInfo("PlaywrightMaintenanceService", "Uninstalling Playwright CLI tool globally via dotnet tool...");
            await RunProcessAsync("dotnet", "tool uninstall --global Microsoft.Playwright.CLI");
        }
        catch (Exception ex)
        {
            _logService.LogError("PlaywrightMaintenanceService", $"Error during Playwright uninstallation: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    private static string GetProjectRootPath()
    {
        var baseDir = Directory.GetCurrentDirectory();
        if (baseDir.Contains("bin"))
        {
            return Directory.GetParent(baseDir)?.Parent?.Parent?.FullName ?? baseDir;
        }
        return baseDir;
    }

    private async Task RunProcessAsync(string fileName, string arguments, string? workingDirectory = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            psi.WorkingDirectory = workingDirectory;
        }

        using var process = Process.Start(psi);
        if (process == null) throw new InvalidOperationException($"Failed to start process: {fileName}");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (!string.IsNullOrWhiteSpace(output))
        {
            _logService.LogInfo("PlaywrightMaintenanceService", output.Trim());
        }
        if (!string.IsNullOrWhiteSpace(error))
        {
            _logService.LogWarn("PlaywrightMaintenanceService", error.Trim());
        }
        if (process.ExitCode != 0)
        {
            throw new Exception($"Process '{fileName} {arguments}' failed with exit code {process.ExitCode}");
        }
    }
}
