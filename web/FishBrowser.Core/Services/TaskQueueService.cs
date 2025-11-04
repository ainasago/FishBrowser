using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Services;

/// <summary>
/// 任务队列服务 - 管理采集任务的队列、优先级和并发执行
/// </summary>
public class TaskQueueService
{
    private readonly PriorityQueue<ScrapingTask, int> _taskQueue;
    private readonly SemaphoreSlim _concurrencySemaphore;
    private readonly LogService _logService;
    private readonly int _maxConcurrency;
    private int _currentRunningTasks;
    private bool _isRunning;

    public TaskQueueService(LogService logService, int maxConcurrency = 5)
    {
        _logService = logService;
        _maxConcurrency = maxConcurrency;
        _taskQueue = new PriorityQueue<ScrapingTask, int>();
        _concurrencySemaphore = new SemaphoreSlim(maxConcurrency);
        _currentRunningTasks = 0;
        _isRunning = false;
    }

    /// <summary>
    /// 添加任务到队列
    /// </summary>
    public void EnqueueTask(ScrapingTask task, int priority = 0)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));

        _taskQueue.Enqueue(task, priority);
        _logService.LogInfo("TaskQueue", $"Task {task.Id} enqueued with priority {priority}. Queue length: {_taskQueue.Count}");
    }

    /// <summary>
    /// 从队列中获取下一个任务
    /// </summary>
    public ScrapingTask DequeueTask()
    {
        if (_taskQueue.Count == 0)
            return null;

        var task = _taskQueue.Dequeue();
        _logService.LogInfo("TaskQueue", $"Task {task.Id} dequeued. Remaining: {_taskQueue.Count}");
        return task;
    }

    /// <summary>
    /// 获取队列长度
    /// </summary>
    public int GetQueueLength()
    {
        return _taskQueue.Count;
    }

    /// <summary>
    /// 获取当前运行的任务数
    /// </summary>
    public int GetRunningTaskCount()
    {
        return _currentRunningTasks;
    }

    /// <summary>
    /// 获取队列状态
    /// </summary>
    public (int QueuedTasks, int RunningTasks, int MaxConcurrency) GetQueueStatus()
    {
        return (_taskQueue.Count, _currentRunningTasks, _maxConcurrency);
    }

    /// <summary>
    /// 执行任务（带并发控制）
    /// </summary>
    public async Task<T> ExecuteTaskAsync<T>(Func<Task<T>> taskAction)
    {
        if (taskAction == null)
            throw new ArgumentNullException(nameof(taskAction));

        // 等待信号量（并发控制）
        await _concurrencySemaphore.WaitAsync();
        Interlocked.Increment(ref _currentRunningTasks);

        try
        {
            _logService.LogInfo("TaskQueue", $"Executing task. Running: {_currentRunningTasks}/{_maxConcurrency}");
            return await taskAction();
        }
        finally
        {
            Interlocked.Decrement(ref _currentRunningTasks);
            _concurrencySemaphore.Release();
            _logService.LogInfo("TaskQueue", $"Task completed. Running: {_currentRunningTasks}/{_maxConcurrency}");
        }
    }

    /// <summary>
    /// 启动队列处理
    /// </summary>
    public void Start()
    {
        _isRunning = true;
        _logService.LogInfo("TaskQueue", $"Task queue started. Max concurrency: {_maxConcurrency}");
    }

    /// <summary>
    /// 停止队列处理
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        _logService.LogInfo("TaskQueue", "Task queue stopped");
    }

    /// <summary>
    /// 清空队列
    /// </summary>
    public void Clear()
    {
        var count = _taskQueue.Count;
        while (_taskQueue.Count > 0)
        {
            _taskQueue.Dequeue();
        }
        _logService.LogInfo("TaskQueue", $"Queue cleared. Removed {count} tasks");
    }

    /// <summary>
    /// 暂停队列
    /// </summary>
    public void Pause()
    {
        _isRunning = false;
        _logService.LogInfo("TaskQueue", "Task queue paused");
    }

    /// <summary>
    /// 恢复队列
    /// </summary>
    public void Resume()
    {
        _isRunning = true;
        _logService.LogInfo("TaskQueue", "Task queue resumed");
    }

    /// <summary>
    /// 检查队列是否运行中
    /// </summary>
    public bool IsRunning => _isRunning;
}
