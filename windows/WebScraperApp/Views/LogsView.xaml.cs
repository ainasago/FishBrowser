using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;
using WpfApplication = System.Windows.Application;

namespace FishBrowser.WPF.Views;

public partial class LogsView : Page
{
    private readonly LogService _logService;
    private ObservableCollection<LogEntry> _logs;
    private ObservableCollection<LogEntry> _filteredLogs;

    public LogsView()
    {
        InitializeComponent();

        // 从 DI 容器获取服务
        var host = WpfApplication.Current.Resources["Host"] as IHost;
        _logService = host?.Services.GetRequiredService<LogService>() ?? throw new InvalidOperationException("LogService not found");

        _logs = new ObservableCollection<LogEntry>();
        _filteredLogs = new ObservableCollection<LogEntry>();
        LogDataGrid.ItemsSource = _filteredLogs;

        // 设置过滤器事件
        LevelFilter.SelectionChanged += (s, e) => ApplyFilter();
        
        // 页面加载时自动刷新日志
        this.Loaded += (s, e) => RefreshLogs_Click(null, null);
    }

    private void RefreshLogs_Click(object? sender, RoutedEventArgs? e)
    {
        LoadLogs();
    }

    private void LoadLogs()
    {
        try
        {
            _logs.Clear();
            var logs = _logService.GetLogs(500);
            foreach (var log in logs)
            {
                _logs.Add(log);
            }
            if (logs.Count == 0)
            {
                // Fallback: try load from Serilog file
                var loadedFromFile = TryLoadFromFile(maxLines: 500);
                _logService.LogWarn("UI", $"DB returned 0 logs, loaded {loadedFromFile} from file fallback");
            }
            ApplyFilter();
            _logService.LogInfo("UI", $"Loaded {_logs.Count} logs into grid");
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to load logs: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"加载日志失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private int TryLoadFromFile(int maxLines)
    {
        try
        {
            // Serilog file path logic mirrors LogService
            var currentDir = System.IO.Directory.GetCurrentDirectory();
            var logsDir = System.IO.Path.Combine(currentDir, "logs");
            if (currentDir.Contains("bin"))
            {
                var projectRoot = System.IO.Directory.GetParent(currentDir)?.Parent?.Parent?.FullName
                    ?? currentDir;
                logsDir = System.IO.Path.Combine(projectRoot, "logs");
            }
            var logFile = System.IO.Path.Combine(logsDir, "webscraper.log");
            if (!System.IO.File.Exists(logFile)) return 0;

            var lines = System.IO.File.ReadAllLines(logFile);
            // take last maxLines
            var start = Math.Max(0, lines.Length - maxLines);
            for (int i = start; i < lines.Length; i++)
            {
                var entry = ParseSerilogLine(lines[i]);
                if (entry != null) _logs.Add(entry);
            }
            return _logs.Count;
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Fallback load from file failed: {ex.Message}", ex.StackTrace);
            return 0;
        }
    }

    private LogEntry? ParseSerilogLine(string line)
    {
        try
        {
            // Format: [yyyy-MM-dd HH:mm:ss] [LVL] [SourceContext] Message
            if (!line.StartsWith("[")) return null;
            int p1 = line.IndexOf("] ");
            int p2 = p1 > 0 ? line.IndexOf("] ", p1 + 2) : -1;
            int p3 = p2 > 0 ? line.IndexOf("] ", p2 + 2) : -1;
            if (p1 < 0 || p2 < 0 || p3 < 0) return null;

            var tsText = line.Substring(1, p1 - 1);
            var level = line.Substring(p1 + 3, (p2 - (p1 + 3)) - 1).Trim('[', ']');
            var source = line.Substring(p2 + 3, (p3 - (p2 + 3)) - 1).Trim('[', ']');
            var message = line[(p3 + 2)..];

            DateTime ts;
            if (!DateTime.TryParse(tsText, out ts)) ts = DateTime.Now;

            return new LogEntry
            {
                Timestamp = ts,
                Level = level,
                Source = source,
                Message = message
            };
        }
        catch
        {
            return null;
        }
    }

    private void ApplyFilter()
    {
        try
        {
            _filteredLogs.Clear();
            var selectedLevel = (LevelFilter.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "全部";

            foreach (var log in _logs)
            {
                if (selectedLevel == "全部" || log.Level == selectedLevel)
                {
                    _filteredLogs.Add(log);
                }
            }
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to apply filter: {ex.Message}", ex.StackTrace);
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        LoadLogs();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = MessageBox.Show("确定要清空所有日志吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _logs.Clear();
                _filteredLogs.Clear();
                _logService.LogInfo("UI", "Logs cleared");
                MessageBox.Show("日志已清空", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to clear logs: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"清空日志失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logService.LogInfo("UI", "Exporting logs");
            // TODO: Implement export functionality (CSV/TXT)
            MessageBox.Show("导出功能开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to export logs: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
