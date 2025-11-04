using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;
using WpfApplication = System.Windows.Application;

namespace FishBrowser.WPF.Views;

public partial class TaskManagementView : Page
{
    private readonly DatabaseService _dbService;
    private readonly LogService _logService;
    private readonly ScraperService _scraperService;
    private ObservableCollection<ScrapingTask> _tasks;
    private DispatcherTimer _refreshTimer;

    public TaskManagementView()
    {
        InitializeComponent();

        // 从 DI 容器获取服务
        var host = WpfApplication.Current.Resources["Host"] as IHost;
        _dbService = host?.Services.GetRequiredService<DatabaseService>() ?? throw new InvalidOperationException("DatabaseService not found");
        _logService = host?.Services.GetRequiredService<LogService>() ?? throw new InvalidOperationException("LogService not found");
        _scraperService = host?.Services.GetRequiredService<ScraperService>() ?? throw new InvalidOperationException("ScraperService not found");

        _tasks = new ObservableCollection<ScrapingTask>();
        TaskDataGrid.ItemsSource = _tasks;

        LoadTasks();
        
        // 启动自动刷新定时器 - 每 2 秒刷新一次任务状态
        _refreshTimer = new DispatcherTimer();
        _refreshTimer.Interval = TimeSpan.FromSeconds(2);
        _refreshTimer.Tick += (s, e) => RefreshTaskStatus();
        _refreshTimer.Start();
        
        // 页面卸载时停止定时器
        this.Unloaded += (s, e) => _refreshTimer?.Stop();
    }

    private void LoadTasks()
    {
        try
        {
            _tasks.Clear();
            var tasks = _dbService.GetAllTasks();
            foreach (var task in tasks)
            {
                _tasks.Add(task);
            }
            _logService.LogInfo("UI", $"Loaded {tasks.Count} tasks");
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to load tasks: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"加载任务失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void NewTask_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logService.LogInfo("UI", "Creating new task");
            
            // 创建示例任务 - 使用默认指纹配置 ID=1
            var task = _dbService.CreateTask("https://example.com", fingerprintProfileId: 1, proxyServerId: null);
            _tasks.Add(task);
            _logService.LogInfo("UI", $"Task created: {task.Id}");
            MessageBox.Show("任务创建成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to create task: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"创建任务失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshTaskStatus()
    {
        try
        {
            // 从数据库获取最新的任务状态
            var latestTasks = _dbService.GetAllTasks();
            
            // 更新现有任务的状态
            foreach (var latestTask in latestTasks)
            {
                var existingTask = _tasks.FirstOrDefault(t => t.Id == latestTask.Id);
                if (existingTask != null)
                {
                    // 更新任务属性
                    existingTask.Status = latestTask.Status;
                    existingTask.CompletedAt = latestTask.CompletedAt;
                    existingTask.ErrorMessage = latestTask.ErrorMessage;
                }
            }
            
            // 添加新任务
            foreach (var latestTask in latestTasks)
            {
                if (!_tasks.Any(t => t.Id == latestTask.Id))
                {
                    _tasks.Add(latestTask);
                }
            }
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to refresh task status: {ex.Message}", ex.StackTrace);
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        LoadTasks();
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logService.LogInfo("UI", "Exporting tasks");
            // TODO: Implement export functionality
            MessageBox.Show("导出功能开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to export tasks: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TaskDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // 检测 Ctrl+C 快捷键
        if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            try
            {
                // 获取选中的行数
                var selectedCount = TaskDataGrid.SelectedItems.Count;
                
                if (selectedCount == 0)
                {
                    _logService.LogInfo("UI", "没有选中任何行");
                    return;
                }

                // 延迟显示提示，确保复制已完成
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        // 显示复制成功提示
                        var message = $"✓ 已复制 {selectedCount} 行数据到剪贴板";
                        _logService.LogInfo("UI", message);
                        
                        // 显示提示框
                        MessageBox.Show(message, "复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError("UI", $"显示提示失败: {ex.Message}", ex.StackTrace);
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                _logService.LogError("UI", $"复制失败: {ex.Message}", ex.StackTrace);
            }
        }
    }
}
