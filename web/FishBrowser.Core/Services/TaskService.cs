using System.Collections.Generic;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Services;

public class TaskService
{
    private readonly DatabaseService _dbService;
    private readonly LogService _logService;

    public TaskService(DatabaseService dbService, LogService logService)
    {
        _dbService = dbService;
        _logService = logService;
    }

    /// <summary>
    /// 创建采集任务
    /// </summary>
    public ScrapingTask CreateTask(string url, int fingerprintProfileId, int? proxyServerId = null)
    {
        var task = _dbService.CreateTask(url, fingerprintProfileId, proxyServerId);
        _logService.LogInfo("TaskService", $"Created task {task.Id} for URL: {url}");
        return task;
    }

    /// <summary>
    /// 获取待处理任务
    /// </summary>
    public List<ScrapingTask> GetPendingTasks()
    {
        return _dbService.GetTasksByStatus(Models.TaskStatus.Pending);
    }

    /// <summary>
    /// 更新任务状态
    /// </summary>
    public void UpdateTaskStatus(int taskId, Models.TaskStatus status, string? errorMessage = null)
    {
        _dbService.UpdateTaskStatus(taskId, status, errorMessage);
        _logService.LogInfo("TaskService", $"Task {taskId} status updated to {status}");
    }

    /// <summary>
    /// 重试失败的任务
    /// </summary>
    public void RetryTask(int taskId, int maxRetries = 3)
    {
        var task = _dbService.GetTask(taskId);
        if (task != null && task.RetryCount < maxRetries)
        {
            task.RetryCount++;
            task.Status = Models.TaskStatus.Pending;
            _logService.LogInfo("TaskService", $"Retrying task {taskId} (attempt {task.RetryCount}/{maxRetries})");
        }
    }
}
