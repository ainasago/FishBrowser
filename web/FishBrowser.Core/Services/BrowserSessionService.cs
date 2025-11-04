using System;
using System.IO;
using System.Linq;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Services;

public class BrowserSessionService
{
    private readonly WebScraperDbContext _db;
    private readonly ILogService _log;
    private readonly string _baseSessionPath;

    public BrowserSessionService(WebScraperDbContext db, ILogService log)
    {
        _db = db;
        _log = log;
        
        // ⭐ 会话数据统一存储在项目根目录的 debug/BrowserSessions 文件夹
        // 获取项目根目录（向上查找包含 .csproj 或 .sln 的目录）
        var currentDir = Directory.GetCurrentDirectory();
        var projectRoot = FindProjectRoot(currentDir) ?? currentDir;
        
        _baseSessionPath = Path.Combine(projectRoot, "debug", "BrowserSessions");
        
        if (!Directory.Exists(_baseSessionPath))
        {
            Directory.CreateDirectory(_baseSessionPath);
            _log.LogInfo("BrowserSession", $"Created session storage: {_baseSessionPath}");
        }
        else
        {
            _log.LogInfo("BrowserSession", $"Using session storage: {_baseSessionPath}");
        }
    }
    
    /// <summary>
    /// 查找项目根目录（包含 .sln 或 web 文件夹的目录）
    /// </summary>
    private string? FindProjectRoot(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            // 检查是否包含 .sln 文件
            if (dir.GetFiles("*.sln").Length > 0)
            {
                return dir.FullName;
            }
            
            // 检查是否是 web 目录（包含 FishBrowser.Api 和 FishBrowser.Web）
            var hasApi = Directory.Exists(Path.Combine(dir.FullName, "FishBrowser.Api"));
            var hasWeb = Directory.Exists(Path.Combine(dir.FullName, "FishBrowser.Web"));
            if (hasApi && hasWeb)
            {
                return dir.FullName;
            }
            
            dir = dir.Parent;
        }
        return null;
    }

    /// <summary>
    /// 为浏览器环境初始化会话目录
    /// </summary>
    public string InitializeSessionPath(BrowserEnvironment env)
    {
        _log.LogInfo("BrowserSession", $"InitializeSessionPath called for env {env.Name} (ID: {env.Id})");
        
        if (!string.IsNullOrWhiteSpace(env.UserDataPath) && Directory.Exists(env.UserDataPath))
        {
            _log.LogInfo("BrowserSession", $"Using existing session path: {env.UserDataPath}");
            
            // 检查目录内容
            var dirInfo = new DirectoryInfo(env.UserDataPath);
            var fileCount = dirInfo.GetFiles("*", SearchOption.AllDirectories).Length;
            _log.LogInfo("BrowserSession", $"Session directory contains {fileCount} files");
            
            return env.UserDataPath;
        }

        // 创建独立的会话目录：BrowserSessions/env_{id}_{name}
        var safeName = string.Join("_", env.Name.Split(Path.GetInvalidFileNameChars()));
        var sessionPath = Path.Combine(_baseSessionPath, $"env_{env.Id}_{safeName}");
        _log.LogInfo("BrowserSession", $"Creating new session path: {sessionPath}");
        
        if (!Directory.Exists(sessionPath))
        {
            Directory.CreateDirectory(sessionPath);
            _log.LogInfo("BrowserSession", $"Session directory created successfully");
        }
        else
        {
            _log.LogInfo("BrowserSession", $"Session directory already exists");
        }

        // 更新数据库
        env.UserDataPath = sessionPath;
        env.UpdatedAt = DateTime.UtcNow;
        _db.SaveChanges();
        _log.LogInfo("BrowserSession", $"Session path saved to database");

        return sessionPath;
    }

    /// <summary>
    /// 记录启动信息
    /// </summary>
    public void RecordLaunch(int envId)
    {
        _log.LogInfo("BrowserSession", $"RecordLaunch called for env ID: {envId}");
        var env = _db.BrowserEnvironments.FirstOrDefault(e => e.Id == envId);
        if (env == null)
        {
            _log.LogError("BrowserSession", $"Environment not found: {envId}");
            return;
        }

        var oldCount = env.LaunchCount;
        env.LastLaunchedAt = DateTime.UtcNow;
        env.LaunchCount++;
        _db.SaveChanges();

        _log.LogInfo("BrowserSession", $"Launch recorded for env {env.Name}: {oldCount} -> {env.LaunchCount}, time: {env.LastLaunchedAt}");
    }

    /// <summary>
    /// 清除会话数据（删除 UserData 目录）
    /// </summary>
    public void ClearSession(int envId)
    {
        var env = _db.BrowserEnvironments.FirstOrDefault(e => e.Id == envId);
        if (env == null || string.IsNullOrWhiteSpace(env.UserDataPath)) return;

        try
        {
            if (Directory.Exists(env.UserDataPath))
            {
                Directory.Delete(env.UserDataPath, recursive: true);
                _log.LogInfo("BrowserSession", $"Cleared session data: {env.UserDataPath}");
            }

            env.UserDataPath = null;
            env.LaunchCount = 0;
            env.LastLaunchedAt = null;
            env.UpdatedAt = DateTime.UtcNow;
            _db.SaveChanges();
        }
        catch (Exception ex)
        {
            _log.LogError("BrowserSession", $"Failed to clear session: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// 获取会话大小（MB）
    /// </summary>
    public double GetSessionSize(string? userDataPath)
    {
        if (string.IsNullOrWhiteSpace(userDataPath) || !Directory.Exists(userDataPath))
            return 0;

        try
        {
            var dirInfo = new DirectoryInfo(userDataPath);
            var totalBytes = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
            return totalBytes / (1024.0 * 1024.0); // MB
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 检查会话是否存在
    /// </summary>
    public bool HasSession(BrowserEnvironment env)
    {
        return !string.IsNullOrWhiteSpace(env.UserDataPath) && Directory.Exists(env.UserDataPath);
    }
}
