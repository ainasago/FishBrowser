using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Configurations;
using FishBrowser.WPF.Services;
using FishBrowser.WPF.Models;
using Microsoft.Extensions.Hosting;
using WpfApplication = System.Windows.Application;


namespace FishBrowser.WPF.Views
{
    public partial class DashboardView : Page, INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly LogService _logService;
        private readonly DispatcherTimer _timer;
        private readonly DispatcherTimer _systemInfoTimer;
        private readonly DispatcherTimer _chartTimer;
        private readonly Random _random = new Random();

        // 图表数据
        public SeriesCollection SeriesCollection { get; set; }
        public string[] TrendLabels { get; set; }
        public Func<double, string> Formatter { get; set; }

        // 活动日志
        public ObservableCollection<ActivityLogEntry> ActivityLogs { get; set; }

        // 最近任务
        public ObservableCollection<RecentTask> RecentTasks { get; set; }

        public DashboardView()
        {
            InitializeComponent();
            
            // 获取服务
            var host = WpfApplication.Current.Resources["Host"] as IHost
                       ?? throw new InvalidOperationException("Host not found");
            _databaseService = host.Services.GetService(typeof(DatabaseService)) as DatabaseService;
            _logService = host.Services.GetService(typeof(LogService)) as LogService;
            
            // 初始化数据
            DataContext = this;
            InitializeChartData();
            ActivityLogs = new ObservableCollection<ActivityLogEntry>();
            RecentTasks = new ObservableCollection<RecentTask>();
            
            // 设置定时器
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(5);
            _timer.Tick += Timer_Tick;
            _timer.Start();
            
            _systemInfoTimer = new DispatcherTimer();
            _systemInfoTimer.Interval = TimeSpan.FromSeconds(10);
            _systemInfoTimer.Tick += SystemInfoTimer_Tick;
            _systemInfoTimer.Start();
            
            _chartTimer = new DispatcherTimer();
            _chartTimer.Interval = TimeSpan.FromMinutes(1);
            _chartTimer.Tick += ChartTimer_Tick;
            _chartTimer.Start();
            
            // 初始化页面
            Loaded += DashboardView_Loaded;
            
            // 添加初始活动日志
            AddActivityLog("仪表盘已加载");
        }

        private void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            // 更新欢迎信息
            UpdateWelcomeMessage();
            
            // 刷新统计数据
            RefreshStatistics();
            
            // 更新系统信息
            UpdateSystemInfo();
            
            // 加载最近任务
            LoadRecentTasks();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            RefreshStatistics();
        }

        private void SystemInfoTimer_Tick(object sender, EventArgs e)
        {
            UpdateSystemInfo();
        }

        private void ChartTimer_Tick(object sender, EventArgs e)
        {
            UpdateChartData();
        }

        private void RefreshStatistics()
        {
            try
            {
                if (_databaseService != null)
                {
                    // 获取任务统计
                    var totalTasks = _databaseService.GetTaskCount();
                    var successTasks = _databaseService.GetTaskCountByStatus(Models.TaskStatus.Completed);
                    var failedTasks = _databaseService.GetTaskCountByStatus(Models.TaskStatus.Failed);
                    var inProgressTasks = _databaseService.GetTaskCountByStatus(Models.TaskStatus.Running);
                    
                    // 更新UI
                    Dispatcher.Invoke(() =>
                    {
                        TotalTasksText.Text = totalTasks.ToString();
                        SuccessTasksText.Text = successTasks.ToString();
                        FailedTasksText.Text = failedTasks.ToString();
                        InProgressTasksText.Text = inProgressTasks.ToString();
                        
                        // 计算百分比
                        double successRate = totalTasks > 0 ? (successTasks * 100.0 / totalTasks) : 0;
                        double failureRate = totalTasks > 0 ? (failedTasks * 100.0 / totalTasks) : 0;
                        
                        SuccessRateText.Text = $"成功率: {successRate:F1}%";
                        FailureRateText.Text = $"失败率: {failureRate:F1}%";
                        
                        // 模拟变化数据
                        int totalChange = _random.Next(-5, 10);
                        TotalTasksChangeText.Text = totalChange >= 0 ? $"↑ {totalChange}" : $"↓ {Math.Abs(totalChange)}";
                        TotalTasksChangeText.Foreground = totalChange >= 0 ? 
                            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 124, 16)) : 
                            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 17, 35));
                        
                        TotalTasksChangePercentText.Text = $"({totalChange * 100.0 / Math.Max(totalTasks - totalChange, 1):F1}%)";
                        
                        // 活跃线程数（模拟）
                        ActiveThreadsText.Text = $"{_random.Next(1, 8)} 线程";
                    });
                }
            }
            catch (Exception ex)
            {
                _logService?.LogError("DashboardView", $"刷新统计数据时出错: {ex.Message}", ex.StackTrace);
            }
        }

        private void UpdateWelcomeMessage()
        {
            try
            {
                var now = DateTime.Now;
                CurrentDateTimeText.Text = now.ToString("yyyy年MM月dd日 HH:mm:ss");
                
                string greeting;
                if (now.Hour < 6)
                    greeting = "夜深了，注意休息";
                else if (now.Hour < 12)
                    greeting = "早上好";
                else if (now.Hour < 18)
                    greeting = "下午好";
                else
                    greeting = "晚上好";
                
                GreetingText.Text = $"{greeting}！";
            }
            catch (Exception ex)
            {
                _logService?.LogError("DashboardView", $"更新欢迎信息时出错: {ex.Message}", ex.StackTrace);
            }
        }

        private void UpdateSystemInfo()
        {
            try
            {
                // 获取系统信息（模拟）
                var cpuUsage = _random.Next(15, 85);
                var memoryUsage = _random.Next(30, 90);
                var activeBrowsers = _random.Next(0, 5);
                var availableProxies = _random.Next(50, 200);
                
                Dispatcher.Invoke(() =>
                {
                    CpuUsageText.Text = $"{cpuUsage}%";
                    MemoryUsageText.Text = $"{memoryUsage}%";
                    ActiveBrowsersText.Text = activeBrowsers.ToString();
                    AvailableProxiesText.Text = availableProxies.ToString();
                });
            }
            catch (Exception ex)
            {
                _logService?.LogError("DashboardView", $"更新系统信息时出错: {ex.Message}", ex.StackTrace);
            }
        }

        private void LoadRecentTasks()
        {
            try
            {
                // 从数据库获取最近任务
                var recentTasks = _databaseService.GetRecentTasks(5);
                
                Dispatcher.Invoke(() =>
                {
                    RecentTasks.Clear();
                    foreach (var task in recentTasks)
                    {
                        RecentTasks.Add(task);
                    }
                    
                    RecentTasksGrid.ItemsSource = RecentTasks;
                });
            }
            catch (Exception ex)
            {
                _logService?.LogError("DashboardView", $"加载最近任务时出错: {ex.Message}", ex.StackTrace);
            }
        }

        private void InitializeChartData()
        {
            // 配置图表数据
            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "成功任务",
                    Values = new ChartValues<double> { 3, 5, 7, 4, 8, 6, 9 },
                    PointGeometry = null,
                    LineSmoothness = 0.5
                },
                new LineSeries
                {
                    Title = "失败任务",
                    Values = new ChartValues<double> { 1, 2, 1, 3, 2, 1, 2 },
                    PointGeometry = null,
                    LineSmoothness = 0.5
                }
            };

            TrendLabels = new[] { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
            Formatter = value => value.ToString("N0");

            TaskTrendChart.Series = SeriesCollection;
        }

        private void UpdateChartData()
        {
            try
            {
                // 模拟图表数据更新
                Dispatcher.Invoke(() =>
                {
                    // 添加新数据点并移除最旧的
                    if (SeriesCollection[0].Values.Count > 6)
                    {
                        SeriesCollection[0].Values.RemoveAt(0);
                        SeriesCollection[1].Values.RemoveAt(0);
                    }

                    SeriesCollection[0].Values.Add((double)_random.Next(3, 12));
                    SeriesCollection[1].Values.Add((double)_random.Next(0, 4));
                    
                    // 更新时间标签
                    var now = DateTime.Now;
                    var newLabel = now.ToString("MM/dd");
                    var newLabels = new string[7];
                    Array.Copy(TrendLabels, 1, newLabels, 0, 6);
                    newLabels[6] = newLabel;
                    TrendLabels = newLabels;
                    
                    TaskTrendChart.AxisX[0].Labels = TrendLabels;
                });
            }
            catch (Exception ex)
            {
                _logService?.LogError("DashboardView", $"更新图表数据时出错: {ex.Message}", ex.StackTrace);
            }
        }

        private void AddActivityLog(string message)
        {
            try
            {
                var logEntry = new ActivityLogEntry
                {
                    Timestamp = DateTime.Now.ToString("HH:mm:ss"),
                    Message = message
                };
                
                Dispatcher.Invoke(() =>
                {
                    ActivityLogs.Insert(0, logEntry);
                    
                    // 保持日志数量在合理范围内
                    while (ActivityLogs.Count > 50)
                    {
                        ActivityLogs.RemoveAt(ActivityLogs.Count - 1);
                    }
                    
                    ActivityLogListBox.ItemsSource = ActivityLogs;
                });
            }
            catch (Exception ex)
            {
                _logService?.LogError("DashboardView", $"添加活动日志时出错: {ex.Message}", ex.StackTrace);
            }
        }

        // 快速操作按钮事件
        private void NewTask_Click(object sender, RoutedEventArgs e)
        {
            AddActivityLog("点击了新建任务按钮");
            // 导航到任务管理页面
            if (this.Parent is Frame frame && frame.Parent is MainWindow mainWindow)
            {
                mainWindow.MainFrame.Navigate(new Uri("Views/TaskManagementPage.xaml", UriKind.Relative));
            }
        }

        private void NewBrowser_Click(object sender, RoutedEventArgs e)
        {
            AddActivityLog("点击了新建浏览器按钮");
            // 导航到浏览器管理页面
            if (this.Parent is Frame frame && frame.Parent is MainWindow mainWindow)
            {
                mainWindow.MainFrame.Navigate(new Uri("Views/BrowserManagementPage.xaml", UriKind.Relative));
            }
        }

        private void ImportProxy_Click(object sender, RoutedEventArgs e)
        {
            AddActivityLog("点击了导入代理按钮");
            // 导航到代理管理页面
            if (this.Parent is Frame frame && frame.Parent is MainWindow mainWindow)
            {
                mainWindow.MainFrame.Navigate(new Uri("Views/ProxyManagementPage.xaml", UriKind.Relative));
            }
        }

        private void ExportData_Click(object sender, RoutedEventArgs e)
        {
            AddActivityLog("点击了导出数据按钮");
            // 导航到数据导出页面
            if (this.Parent is Frame frame && frame.Parent is MainWindow mainWindow)
            {
                mainWindow.MainFrame.Navigate(new Uri("Views/DataExportPage.xaml", UriKind.Relative));
            }
        }

        private void ViewLogs_Click(object sender, RoutedEventArgs e)
        {
            AddActivityLog("点击了查看日志按钮");
            // 导航到日志查看页面
            if (this.Parent is Frame frame && frame.Parent is MainWindow mainWindow)
            {
                mainWindow.MainFrame.Navigate(new Uri("Views/LogViewPage.xaml", UriKind.Relative));
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            AddActivityLog("点击了系统设置按钮");
            // 导航到设置页面
            if (this.Parent is Frame frame && frame.Parent is MainWindow mainWindow)
            {
                mainWindow.MainFrame.Navigate(new Uri("Views/SettingsPage.xaml", UriKind.Relative));
            }
        }

        private void ViewAllTasks_Click(object sender, RoutedEventArgs e)
        {
            AddActivityLog("点击了查看全部任务按钮");
            // 导航到任务管理页面
            if (this.Parent is Frame frame && frame.Parent is MainWindow mainWindow)
            {
                mainWindow.MainFrame.Navigate(new Uri("Views/TaskManagementPage.xaml", UriKind.Relative));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // 活动日志条目
    public class ActivityLogEntry
    {
        public string Timestamp { get; set; }
        public string Message { get; set; }
    }
}
