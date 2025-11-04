using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Views.Dialogs;

public partial class TaskTestProgressDialog : Window
{
    private readonly ObservableCollection<string> _logItems = new();
    private readonly DispatcherTimer _timer;
    private DateTime _startTime;
    private CancellationTokenSource? _cancellationTokenSource;
    private int _successSteps = 0;
    private int _failedSteps = 0;
    
    public TaskTestProgressDialog(string taskName)
    {
        InitializeComponent();
        
        TaskNameText.Text = taskName;
        LogList.ItemsSource = _logItems;
        
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();
        
        _startTime = DateTime.Now;
    }
    
    public void SetCancellationTokenSource(CancellationTokenSource cts)
    {
        _cancellationTokenSource = cts;
    }
    
    public void UpdateProgress(TestProgress progress)
    {
        Dispatcher.Invoke(() =>
        {
            // 更新阶段
            StageText.Text = $"当前阶段：{GetStageText(progress.Stage)}";
            
            // 更新进度条
            if (progress.TotalSteps > 0)
            {
                var percentage = (double)progress.CurrentStep / progress.TotalSteps * 100;
                ProgressBar.Value = percentage;
                ProgressText.Text = $"{percentage:F0}%";
                
                StepsExecutedText.Text = $"{progress.CurrentStep} / {progress.TotalSteps}";
            }
            
            // 添加日志
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var icon = GetLogIcon(progress.Level);
            var logEntry = $"[{timestamp}] {icon} {progress.Message}";
            _logItems.Add(logEntry);
            
            // 自动滚动到底部
            LogScrollViewer.ScrollToEnd();
            
            // 更新截图
            if (progress.Screenshot != null && progress.Screenshot.Length > 0)
            {
                UpdateScreenshot(progress.Screenshot);
            }
            
            // 更新统计
            if (progress.Stage == TestStage.ExecutingSteps)
            {
                if (progress.Level == LogLevel.Error)
                {
                    _failedSteps++;
                    FailedStepsText.Text = _failedSteps.ToString();
                }
                else if (progress.Message.Contains("✅"))
                {
                    _successSteps++;
                    SuccessStepsText.Text = _successSteps.ToString();
                }
            }
            
            // 完成或失败时启用关闭按钮
            if (progress.Stage == TestStage.Completed || progress.Stage == TestStage.Failed)
            {
                _timer.Stop();
                CloseButton.IsEnabled = true;
                StopButton.IsEnabled = false;
                
                if (progress.Stage == TestStage.Completed)
                {
                    TitleText.Text = "✅ 测试完成";
                }
                else
                {
                    TitleText.Text = "❌ 测试失败";
                }
            }
        });
    }
    
    private void UpdateScreenshot(byte[] imageData)
    {
        try
        {
            using var ms = new MemoryStream(imageData);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = ms;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            
            ScreenshotImage.Source = bitmap;
        }
        catch (Exception ex)
        {
            _logItems.Add($"[{DateTime.Now:HH:mm:ss}] ⚠️ 截图加载失败: {ex.Message}");
        }
    }
    
    private void Timer_Tick(object? sender, EventArgs e)
    {
        var elapsed = DateTime.Now - _startTime;
        ElapsedTimeText.Text = $"{elapsed:mm\\:ss}";
    }
    
    private string GetStageText(TestStage stage)
    {
        return stage switch
        {
            TestStage.Initializing => "初始化",
            TestStage.GeneratingFingerprint => "生成指纹",
            TestStage.StartingBrowser => "启动浏览器",
            TestStage.ExecutingSteps => "执行步骤",
            TestStage.Completed => "完成",
            TestStage.Failed => "失败",
            TestStage.CleaningUp => "清理资源",
            _ => "未知"
        };
    }
    
    private string GetLogIcon(LogLevel level)
    {
        return level switch
        {
            LogLevel.Debug => "🔍",
            LogLevel.Info => "ℹ️",
            LogLevel.Warning => "⚠️",
            LogLevel.Error => "❌",
            _ => "•"
        };
    }
    
    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "确定要停止测试吗？",
            "确认停止",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            _cancellationTokenSource?.Cancel();
            StopButton.IsEnabled = false;
            _logItems.Add($"[{DateTime.Now:HH:mm:ss}] ⚠️ 用户取消测试");
        }
    }
    
    private void CopyLog_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var logText = string.Join(Environment.NewLine, _logItems);
            Clipboard.SetText(logText);
            MessageBox.Show("日志已复制到剪贴板", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"复制失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
