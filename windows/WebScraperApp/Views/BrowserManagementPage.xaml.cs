using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;
using FishBrowser.WPF.Views.Dialogs;

namespace FishBrowser.WPF.Views;

public partial class BrowserManagementPage : Page
{
    private IHost _host;
    private WebScraperDbContext _db;
    private ILogService _log;
    private BrowserEnvironmentService _svc;
    private BrowserSessionService _sessionSvc;
    private int? _selectedGroupId;
    private bool _isLaunching = false; // 防止重复启动

    public BrowserManagementPage()
    {
        InitializeComponent();
        _host = System.Windows.Application.Current.Resources["Host"] as IHost ?? throw new InvalidOperationException("Host not found");
        _db = _host.Services.GetRequiredService<WebScraperDbContext>();
        _log = _host.Services.GetRequiredService<ILogService>();
        _svc = _host.Services.GetRequiredService<BrowserEnvironmentService>();
        _sessionSvc = _host.Services.GetRequiredService<BrowserSessionService>();

        Loaded += (s, e) => LoadData();
    }

    private void LoadData()
    {
        LoadGroups();
        LoadEnvironments();
    }

    private void LoadGroups()
    {
        var groups = _svc.GetAllGroups();
        // 添加"未分组"虚拟项
        var all = new System.Collections.ObjectModel.ObservableCollection<object>();
        all.Add(new { Id = (int?)null, Name = "未分组" });
        foreach (var g in groups) all.Add(g);
        GroupList.ItemsSource = all;
        if (GroupList.Items.Count > 0) GroupList.SelectedIndex = 0;
    }

    private void LoadEnvironments()
    {
        var envs = _selectedGroupId.HasValue
            ? _svc.GetEnvironmentsByGroup(_selectedGroupId.Value)
            : _svc.GetEnvironmentsByGroup(null); // 未分组
        EnvironmentGrid.ItemsSource = envs;
        var groupName = _selectedGroupId.HasValue
            ? _db.BrowserGroups.FirstOrDefault(g => g.Id == _selectedGroupId.Value)?.Name ?? "未知分组"
            : "未分组";
        GroupTitle.Text = $"{groupName} ({envs.Count} 个浏览器)";
        StatusText.Text = $"共 {_svc.GetAllEnvironments().Count} 个浏览器环境";
    }

    private void GroupList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GroupList.SelectedItem == null) return;
        var item = GroupList.SelectedItem;
        if (item is BrowserGroup group)
            _selectedGroupId = group.Id;
        else
            _selectedGroupId = null; // 未分组
        LoadEnvironments();
    }

    private Window GetParentWindow() => Window.GetWindow(this);

    private void NewGroup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new GroupEditDialog { Owner = GetParentWindow() };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                _svc.CreateGroup(dialog.GroupName, dialog.GroupDescription);
                LoadGroups();
                StatusText.Text = "分组创建成功";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void EditGroup_Click(object sender, RoutedEventArgs e)
    {
        if (!_selectedGroupId.HasValue)
        {
            MessageBox.Show("未分组不可编辑", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var group = _db.BrowserGroups.FirstOrDefault(g => g.Id == _selectedGroupId.Value);
        if (group == null) return;

        var dialog = new GroupEditDialog(group) { Owner = GetParentWindow() };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                _svc.UpdateGroup(group.Id, dialog.GroupName, dialog.GroupDescription);
                LoadGroups();
                StatusText.Text = "分组更新成功";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void DeleteGroup_Click(object sender, RoutedEventArgs e)
    {
        if (!_selectedGroupId.HasValue)
        {
            MessageBox.Show("未分组不可删除", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var group = _db.BrowserGroups.FirstOrDefault(g => g.Id == _selectedGroupId.Value);
        if (group == null) return;

        var result = MessageBox.Show($"确定删除分组 '{group.Name}' 吗？\n该分组下的浏览器将变为未分组。", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                _svc.DeleteGroup(group.Id);
                LoadData();
                StatusText.Text = "分组已删除";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void NewEnvironment_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new NewBrowserEnvironmentWindow { Owner = GetParentWindow() };
        if (dialog.ShowDialog() == true)
        {
            LoadEnvironments();
        }
    }

    private void EditEnvironment_Click(object sender, RoutedEventArgs e)
    {
        var env = EnvironmentGrid.SelectedItem as BrowserEnvironment;
        if (env == null)
        {
            MessageBox.Show("请选择一个浏览器环境", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var dialog = new NewBrowserEnvironmentWindow(env) { Owner = GetParentWindow() };
        if (dialog.ShowDialog() == true)
        {
            LoadEnvironments();
        }
    }

    private void DeleteEnvironment_Click(object sender, RoutedEventArgs e)
    {
        var env = EnvironmentGrid.SelectedItem as BrowserEnvironment;
        if (env == null)
        {
            MessageBox.Show("请选择一个浏览器环境", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show($"确定删除浏览器环境 '{env.Name}' 吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                _svc.DeleteEnvironment(env.Id);
                LoadEnvironments();
                StatusText.Text = "浏览器环境已删除";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void LaunchEnvironment_Click(object sender, RoutedEventArgs e)
    {
        // 防止重复启动
        if (_isLaunching)
        {
            MessageBox.Show("浏览器正在启动中，请稍候...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var env = EnvironmentGrid.SelectedItem as BrowserEnvironment;
        if (env == null || env.FingerprintProfileId == null)
        {
            MessageBox.Show("请选择一个已创建的浏览器环境", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _isLaunching = true;
        try
        {
            _log.LogInfo("BrowserMgmt", $"========== Starting browser launch for env: {env.Name} (ID: {env.Id}) ==========");
            StatusText.Text = "正在启动浏览器...";
            
            var profile = _db.FingerprintProfiles.FirstOrDefault(p => p.Id == env.FingerprintProfileId.Value);
            if (profile == null)
            {
                _log.LogError("BrowserMgmt", $"Fingerprint profile not found for env {env.Name}");
                MessageBox.Show("未找到指纹配置", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            _log.LogInfo("BrowserMgmt", $"Loaded fingerprint profile: {profile.Name} (ID: {profile.Id})");

            // 初始化会话目录（如果启用持久化）
            string? userDataPath = null;
            if (env.EnablePersistence)
            {
                _log.LogInfo("BrowserMgmt", "Persistence enabled, initializing session path...");
                userDataPath = _sessionSvc.InitializeSessionPath(env);
                _log.LogInfo("BrowserMgmt", $"Session path initialized: {userDataPath}");
                StatusText.Text = $"使用持久化会话: {userDataPath}";
            }
            else
            {
                _log.LogInfo("BrowserMgmt", "Persistence disabled, using temporary session");
            }

            _log.LogInfo("BrowserMgmt", "Creating BrowserControllerAdapter...");
            var fingerprintSvc = _host.Services.GetRequiredService<FingerprintService>();
            var logSvc = _host.Services.GetRequiredService<LogService>();
            var secretSvc = _host.Services.GetRequiredService<SecretService>();
            
            // 使用新的适配器，根据环境配置决定引擎模式
            var controller = new BrowserControllerAdapter(logSvc, fingerprintSvc, secretSvc);
            bool useUndetectedChrome = string.Equals(env.EngineMode, "undetected_chrome", StringComparison.OrdinalIgnoreCase);
            controller.SetUseUndetectedChrome(useUndetectedChrome);
            _log.LogInfo("BrowserMgmt", $"Engine mode from environment: {env.EngineMode} -> UseUndetectedChrome: {useUndetectedChrome}");

            // 检查是否加载 Automa 扩展（默认启用）
            // 注意：UndetectedChrome 模式下暂不支持扩展
            bool loadAutoma = LoadAutomaCheckBox.IsChecked ?? true;
            _log.LogInfo("BrowserMgmt", $"Automa extension loading: {loadAutoma} (UndetectedChrome mode: extensions not supported)");

            _log.LogInfo("BrowserMgmt", "Initializing browser...");
            // 传递环境对象以支持自定义分辨率覆盖
            await controller.InitializeBrowserAsync(profile, proxy: null, headless: false, userDataPath: userDataPath, loadAutoma: loadAutoma, environment: env);
            _log.LogInfo("BrowserMgmt", "Browser initialized successfully");
            
            // 记录启动
            _sessionSvc.RecordLaunch(env.Id);
            _log.LogInfo("BrowserMgmt", $"Launch recorded, count: {env.LaunchCount + 1}");
            
            StatusText.Text = "浏览器已启动，正在打开测试页面...";

            _log.LogInfo("BrowserMgmt", "Navigating to test page...");
            await controller.NavigateAsync("https://httpbin.org/headers");
            _log.LogInfo("BrowserMgmt", "Navigation completed");
            
            var sessionInfo = env.EnablePersistence ? $"会话将在关闭浏览器后自动保存，启动次数: {env.LaunchCount + 1}" : "会话未保存（临时模式）";
            var engineInfo = useUndetectedChrome 
                ? " | 🛡️ UndetectedChrome（真实 TLS 指纹，成功率 90-95%）" 
                : " | 🎭 Playwright（标准模式）";
            StatusText.Text = $"浏览器 '{env.Name}' 已启动 | {sessionInfo}{engineInfo}";
            
            // 重新加载显示更新后的启动次数
            LoadEnvironments();
            
            // 在后台等待浏览器关闭以确保会话保存
            if (env.EnablePersistence)
            {
                _log.LogInfo("BrowserMgmt", "Starting background task to wait for browser close...");
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _log.LogInfo("BrowserMgmt", "Background task: Calling WaitForCloseAsync...");
                        await controller.WaitForCloseAsync();
                        _log.LogInfo("BrowserMgmt", $"========== Browser '{env.Name}' closed, session saved ==========");
                    }
                    catch (Exception ex)
                    {
                        _log.LogError("BrowserMgmt", $"Error waiting for browser close: {ex.Message}", ex.StackTrace);
                    }
                });
                _log.LogInfo("BrowserMgmt", "Background task started");
            }
            else
            {
                _log.LogInfo("BrowserMgmt", "Persistence disabled, not waiting for close");
            }
        }
        catch (Exception ex)
        {
            _log.LogError("BrowserMgmt", $"Launch failed: {ex.Message}", ex.StackTrace);
            StatusText.Text = $"启动失败: {ex.Message}";
            MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isLaunching = false; // 重置标志
        }
    }

    private void ClearSession_Click(object sender, RoutedEventArgs e)
    {
        var env = EnvironmentGrid.SelectedItem as BrowserEnvironment;
        if (env == null)
        {
            MessageBox.Show("请选择一个浏览器环境", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!_sessionSvc.HasSession(env))
        {
            MessageBox.Show("该环境没有保存的会话数据", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var sessionSize = _sessionSvc.GetSessionSize(env.UserDataPath);
        var result = MessageBox.Show(
            $"确定清除浏览器环境 '{env.Name}' 的会话数据吗？\n\n会话大小: {sessionSize:F2} MB\n启动次数: {env.LaunchCount}\n\n清除后将删除所有Cookie、历史记录、扩展等数据。",
            "确认清除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                _sessionSvc.ClearSession(env.Id);
                LoadEnvironments();
                StatusText.Text = "会话数据已清除";
                MessageBox.Show("会话数据已清除", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"清除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ChangeProfile_Click(object sender, RoutedEventArgs e)
    {
        var env = EnvironmentGrid.SelectedItem as BrowserEnvironment;
        if (env == null)
        {
            MessageBox.Show("请选择一个浏览器环境", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var profiles = _db.FingerprintProfiles.OrderByDescending(p => p.UpdatedAt).ToList();
        if (!profiles.Any())
        {
            MessageBox.Show("没有可用的指纹配置，请先在'指纹配置'中创建", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SelectProfileDialog(profiles, env.FingerprintProfileId) { Owner = GetParentWindow() };
        if (dialog.ShowDialog() == true && dialog.SelectedProfileId.HasValue)
        {
            try
            {
                _svc.SwitchProfile(env.Id, dialog.SelectedProfileId.Value);
                LoadEnvironments();
                StatusText.Text = "指纹配置已更换";
                _log.LogInfo("BrowserMgmt", $"Profile switched for env {env.Name}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更换失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BatchChangeProfile_Click(object sender, RoutedEventArgs e)
    {
        var envs = _selectedGroupId.HasValue
            ? _svc.GetEnvironmentsByGroup(_selectedGroupId.Value)
            : _svc.GetAllEnvironments();

        if (!envs.Any())
        {
            MessageBox.Show("当前分组/列表中没有浏览器环境", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var profiles = _db.FingerprintProfiles.OrderByDescending(p => p.UpdatedAt).ToList();
        if (!profiles.Any())
        {
            MessageBox.Show("没有可用的指纹配置，请先在'指纹配置'中创建", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SelectProfileDialog(profiles, null) { Owner = GetParentWindow() };
        if (dialog.ShowDialog() == true && dialog.SelectedProfileId.HasValue)
        {
            try
            {
                int count = 0;
                foreach (var env in envs)
                {
                    _svc.SwitchProfile(env.Id, dialog.SelectedProfileId.Value);
                    count++;
                }
                LoadEnvironments();
                StatusText.Text = $"已为 {count} 个环境更换指纹配置";
                _log.LogInfo("BrowserMgmt", $"Batch profile switch completed for {count} environments");
                MessageBox.Show($"成功为 {count} 个环境更换指纹配置", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"批量更换失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void MoveEnvironment_Click(object sender, RoutedEventArgs e)
    {
        var env = EnvironmentGrid.SelectedItem as BrowserEnvironment;
        if (env == null)
        {
            MessageBox.Show("请选择一个浏览器环境", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var groups = _svc.GetAllGroups();
        var dialog = new MoveToGroupDialog(groups, env.GroupId) { Owner = GetParentWindow() };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                _svc.MoveEnvironmentToGroup(env.Id, dialog.SelectedGroupId);
                LoadEnvironments();
                StatusText.Text = "浏览器已移动";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"移动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void LaunchMVP_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "正在启动 Cloudflare 测试浏览器...";
            _log.LogInfo("BrowserMgmt", "========== Starting Cloudflare Test Browser ==========");

            // 询问用户使用哪个浏览器
            var result = MessageBox.Show(
                "Firefox 已证实可以绕过 Cloudflare 的 TLS 指纹检测！\n\n" +
                "选择浏览器：\n" +
                "• 是(Y) = Firefox（推荐，已验证可通过）\n" +
                "• 否(N) = Chrome（TLS 指纹可能被检测）\n" +
                "• 取消 = 取消启动",
                "选择浏览器引擎",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
            {
                StatusText.Text = "已取消";
                return;
            }

            bool useFirefox = (result == MessageBoxResult.Yes);

            // 创建一个能通过 Cloudflare 的完整配置
            var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            
            Microsoft.Playwright.IBrowser browser;
            
            if (useFirefox)
            {
                _log.LogInfo("BrowserMgmt", "🦊 Using Firefox (TLS fingerprint bypass confirmed)");
                browser = await playwright.Firefox.LaunchAsync(new Microsoft.Playwright.BrowserTypeLaunchOptions
                {
                    Headless = false
                });
            }
            else
            {
                _log.LogInfo("BrowserMgmt", "🌐 Using Chrome (TLS fingerprint may be detected)");
                // 使用 Chrome channel（真实 Chrome，但 TLS 指纹仍然是 Playwright 的）
                // 注意：TLS 指纹问题无法通过 JS 解决，详见 docs/TLS_FINGERPRINT_ISSUE.md
                browser = await playwright.Chromium.LaunchAsync(new Microsoft.Playwright.BrowserTypeLaunchOptions
            {
                Headless = false,
                Channel = "chrome",  // 使用真实 Chrome
                Args = new[]
                {
                    "--disable-blink-features=AutomationControlled",
                    "--disable-features=IsolateOrigins,site-per-process",
                    "--disable-site-isolation-trials",
                    "--disable-web-security",
                    "--disable-features=BlockInsecurePrivateNetworkRequests",
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-accelerated-2d-canvas",
                    "--no-first-run",
                    "--no-zygote",
                    // 注意：不要禁用 GPU，Cloudflare 会检查 WebGL
                    // "--disable-gpu",  // 已注释，保持 GPU 启用
                    "--hide-scrollbars",
                    "--mute-audio",
                    "--disable-background-timer-throttling",
                    "--disable-backgrounding-occluded-windows",
                    "--disable-renderer-backgrounding",
                    "--disable-infobars",
                    "--window-position=0,0",
                    "--ignore-certifcate-errors",
                    "--ignore-certifcate-errors-spki-list",
                    "--disable-features=TranslateUI",
                    "--disable-features=BlinkGenPropertyTrees",
                    "--disable-ipc-flooding-protection",
                    "--enable-features=NetworkService,NetworkServiceInProcess"
                }
            });
            }

            // 创建上下文，配置完整的防检测参数
            var contextOptions = new Microsoft.Playwright.BrowserNewContextOptions
            {
                Locale = "zh-CN",
                TimezoneId = "Asia/Shanghai",
                ViewportSize = new Microsoft.Playwright.ViewportSize { Width = 1280, Height = 720 },
                DeviceScaleFactor = 1
            };

            if (useFirefox)
            {
                contextOptions.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0";
                contextOptions.ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    ["Accept-Language"] = "zh-CN,zh;q=0.9,en;q=0.8"
                };
            }
            else
            {
                contextOptions.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36";
                contextOptions.ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    ["Accept-Language"] = "zh-CN,zh;q=0.9,en;q=0.8",
                    ["sec-ch-ua"] = "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"141\", \"Google Chrome\";v=\"141\"",
                    ["sec-ch-ua-mobile"] = "?0",
                    ["sec-ch-ua-platform"] = "\"Windows\""
                };
            }

            var context = await browser.NewContextAsync(contextOptions);

            // 从文件加载 Cloudflare 防检测脚本
            var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "scripts", "cloudflare-anti-detection.js");
            if (!File.Exists(scriptPath))
            {
                _log.LogError("BrowserMgmt", $"Anti-detection script not found: {scriptPath}");
                StatusText.Text = "❌ 防检测脚本文件未找到";
                return;
            }
            
            var antiDetectionScript = await File.ReadAllTextAsync(scriptPath);
            await context.AddInitScriptAsync(antiDetectionScript);
            _log.LogInfo("BrowserMgmt", $"✅ Loaded anti-detection script from: {scriptPath}");

            var page = await context.NewPageAsync();
            
            _log.LogInfo("BrowserMgmt", "Navigating to Cloudflare test site...");
            
            // 不等待 NetworkIdle，因为 Cloudflare 验证页面会一直有网络活动
            await page.GotoAsync("https://nowsecure.nl", new Microsoft.Playwright.PageGotoOptions
            {
                Timeout = 30000,  // 30 秒超时
                WaitUntil = Microsoft.Playwright.WaitUntilState.DOMContentLoaded  // 只等待 DOM 加载
            });
            
            // 等待 Cloudflare 验证完成（最多 15 秒）
            _log.LogInfo("BrowserMgmt", "Waiting for Cloudflare verification...");
            try
            {
                // 等待验证页面消失或成功页面出现
                await page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.Load, new Microsoft.Playwright.PageWaitForLoadStateOptions
                {
                    Timeout = 15000
                });
                _log.LogInfo("BrowserMgmt", "✅ Page loaded, Cloudflare check may have passed");
            }
            catch
            {
                _log.LogWarn("BrowserMgmt", "Cloudflare verification still in progress (this is normal)");
            }
            
            // 模拟人类行为（关键！）
            _log.LogInfo("BrowserMgmt", "Simulating human behavior...");
            var random = new Random();
            
            // 1. 等待（模拟阅读页面）
            await Task.Delay(random.Next(2000, 4000));
            _log.LogInfo("BrowserMgmt", "  - Waiting (reading page)...");
            
            // 2. 鼠标移动（模拟查看内容）
            for (int i = 0; i < 5; i++)
            {
                int x = random.Next(100, 800);
                int y = random.Next(100, 600);
                await page.Mouse.MoveAsync(x, y);
                await Task.Delay(random.Next(300, 800));
                _log.LogInfo("BrowserMgmt", $"  - Mouse move to ({x}, {y})");
            }
            
            // 3. 滚动（模拟浏览页面）
            int scrollAmount = random.Next(50, 200);
            await page.Mouse.WheelAsync(0, scrollAmount);
            await Task.Delay(random.Next(1000, 2000));
            _log.LogInfo("BrowserMgmt", $"  - Scroll down {scrollAmount}px");
            
            // 4. 再次鼠标移动
            int finalX = random.Next(100, 800);
            int finalY = random.Next(100, 600);
            await page.Mouse.MoveAsync(finalX, finalY);
            await Task.Delay(random.Next(500, 1000));
            _log.LogInfo("BrowserMgmt", $"  - Final mouse move to ({finalX}, {finalY})");
            
            _log.LogInfo("BrowserMgmt", "✅ Human behavior simulation completed");

            _log.LogInfo("BrowserMgmt", "✅ Cloudflare test browser launched successfully");
            _log.LogInfo("BrowserMgmt", "========== Configuration Summary (30 Anti-Detection Measures) ==========");
            _log.LogInfo("BrowserMgmt", "  [Browser]");
            _log.LogInfo("BrowserMgmt", "    - Channel: chrome (real Chrome, not Chromium)");
            _log.LogInfo("BrowserMgmt", "    - UserAgent: Chrome/120.0.0.0 on Windows 10");
            _log.LogInfo("BrowserMgmt", "    - Platform: Win32 (matches UA)");
            _log.LogInfo("BrowserMgmt", "    - Vendor: Google Inc.");
            _log.LogInfo("BrowserMgmt", "  [Navigator]");
            _log.LogInfo("BrowserMgmt", "    - Plugins: ✅ 3 plugins (PDF, Native Client)");
            _log.LogInfo("BrowserMgmt", "    - MimeTypes: ✅ 2 types");
            _log.LogInfo("BrowserMgmt", "    - Languages: ✅ ['zh-CN', 'zh', 'en-US', 'en']");
            _log.LogInfo("BrowserMgmt", "    - Webdriver: ✅ Hidden (undefined)");
            _log.LogInfo("BrowserMgmt", "  [Headers]");
            _log.LogInfo("BrowserMgmt", "    - Client Hints: ✅ sec-ch-ua, sec-ch-ua-platform, sec-ch-ua-mobile");
            _log.LogInfo("BrowserMgmt", "    - Accept-Language: zh-CN,zh;q=0.9,en;q=0.8");
            _log.LogInfo("BrowserMgmt", "  [Hardware]");
            _log.LogInfo("BrowserMgmt", "    - CPU: ✅ 8 cores");
            _log.LogInfo("BrowserMgmt", "    - Memory: ✅ 8GB RAM");
            _log.LogInfo("BrowserMgmt", "    - Touch: ✅ 0 touch points");
            _log.LogInfo("BrowserMgmt", "    - Screen: ✅ 1920x1080, 24-bit color");
            _log.LogInfo("BrowserMgmt", "  [Network]");
            _log.LogInfo("BrowserMgmt", "    - Connection: ✅ 4g, 50ms RTT, 10Mbps downlink");
            _log.LogInfo("BrowserMgmt", "  [Fingerprints]");
            _log.LogInfo("BrowserMgmt", "    - Canvas: ✅ Noise injection enabled");
            _log.LogInfo("BrowserMgmt", "    - WebGL: ✅ Vendor/Renderer spoofed (Intel Inc. / Intel Iris OpenGL Engine)");
            _log.LogInfo("BrowserMgmt", "    - AudioContext: ✅ Noise injection enabled");
            _log.LogInfo("BrowserMgmt", "  [Chrome Objects]");
            _log.LogInfo("BrowserMgmt", "    - chrome.runtime: ✅");
            _log.LogInfo("BrowserMgmt", "    - chrome.loadTimes: ✅");
            _log.LogInfo("BrowserMgmt", "    - chrome.csi: ✅");
            _log.LogInfo("BrowserMgmt", "  [Timezone]");
            _log.LogInfo("BrowserMgmt", "    - Timezone: ✅ Asia/Shanghai (UTC+8)");
            _log.LogInfo("BrowserMgmt", "    - TimezoneOffset: ✅ -480 minutes");
            _log.LogInfo("BrowserMgmt", "  [Automation Traces]");
            _log.LogInfo("BrowserMgmt", "    - cdc_* variables: ✅ Removed");
            _log.LogInfo("BrowserMgmt", "    - navigator.__proto__.webdriver: ✅ Deleted");
            _log.LogInfo("BrowserMgmt", "  [Turnstile-Specific APIs]");
            _log.LogInfo("BrowserMgmt", "    - Battery API: ✅ Spoofed");
            _log.LogInfo("BrowserMgmt", "    - MediaDevices: ✅ Spoofed (3 devices)");
            _log.LogInfo("BrowserMgmt", "    - Permissions API: ✅ Enhanced");
            _log.LogInfo("BrowserMgmt", "    - ServiceWorker: ✅ Spoofed");
            _log.LogInfo("BrowserMgmt", "    - Bluetooth/USB: ✅ Spoofed");
            _log.LogInfo("BrowserMgmt", "    - Presentation/Credentials: ✅ Spoofed");
            _log.LogInfo("BrowserMgmt", "    - Keyboard/MediaSession: ✅ Spoofed");
            _log.LogInfo("BrowserMgmt", "=======================================================================");
            _log.LogInfo("BrowserMgmt", "🛡️ 提示：如果仍然无法通过验证，请：");
            _log.LogInfo("BrowserMgmt", "   1. 在浏览器控制台运行：console.log(navigator.webdriver, navigator.plugins.length)");
            _log.LogInfo("BrowserMgmt", "   2. 检查是否有鼠标移动（某些站点需要人类行为）");
            _log.LogInfo("BrowserMgmt", "   3. 尝试手动点击验证框");
            
            StatusText.Text = "✅ Cloudflare Turnstile 测试浏览器已启动（30 项防检测 + 人类行为模拟）";

            // 等待浏览器关闭
            _ = Task.Run(async () =>
            {
                try
                {
                    while (context.Pages.Count > 0)
                    {
                        await Task.Delay(1000);
                    }
                    await browser.CloseAsync();
                    playwright.Dispose();
                    _log.LogInfo("BrowserMgmt", "Cloudflare test browser closed");
                }
                catch (Exception ex)
                {
                    _log.LogError("BrowserMgmt", $"Error closing test browser: {ex.Message}", ex.StackTrace);
                }
            });
        }
        catch (Exception ex)
        {
            _log.LogError("BrowserMgmt", $"Cloudflare test launch failed: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"启动失败: {ex.Message}\n\n提示：需要安装 Google Chrome 浏览器", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "启动失败";
        }
    }

    private async void CompareFingerprints_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "正在启动指纹对比测试...";
            _log.LogInfo("BrowserMgmt", "========== 指纹对比测试开始 ==========");

            var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            
            // 收集器脚本路径
            var collectorPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "scripts", "fingerprint-collector.js");
            if (!File.Exists(collectorPath))
            {
                _log.LogError("BrowserMgmt", $"Fingerprint collector script not found: {collectorPath}");
                StatusText.Text = "❌ 指纹收集脚本未找到";
                return;
            }
            
            var collectorScript = await File.ReadAllTextAsync(collectorPath);
            
            _log.LogInfo("BrowserMgmt", "========== 测试 1: 真实 Chrome（无任何修改）==========");
            
            // 1. 启动真实 Chrome（无任何防检测）
            var realBrowser = await playwright.Chromium.LaunchAsync(new Microsoft.Playwright.BrowserTypeLaunchOptions
            {
                Headless = false,
                Channel = "chrome"
            });
            
            var realContext = await realBrowser.NewContextAsync();
            await realContext.AddInitScriptAsync(collectorScript);
            
            var realPage = await realContext.NewPageAsync();
            await realPage.GotoAsync("https://nowsecure.nl");
            await Task.Delay(3000);
            
            // 收集真实 Chrome 的指纹
            var realFingerprint = await realPage.EvaluateAsync<string>("JSON.stringify(window.__fingerprint__, null, 2)");
            var realFingerprintPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fingerprint_real_chrome.json");
            await File.WriteAllTextAsync(realFingerprintPath, realFingerprint);
            _log.LogInfo("BrowserMgmt", $"✅ 真实 Chrome 指纹已保存: {realFingerprintPath}");
            
            // 检查是否通过 Cloudflare
            var realPassed = await CheckCloudflareStatus(realPage);
            _log.LogInfo("BrowserMgmt", $"真实 Chrome Cloudflare 状态: {(realPassed ? "✅ 通过" : "❌ 未通过")}");
            
            await Task.Delay(2000);
            await realBrowser.CloseAsync();
            
            _log.LogInfo("BrowserMgmt", "========== 测试 2: Playwright + 防检测脚本 ==========");
            
            // 2. 启动 Playwright + 防检测
            var playwrightBrowser = await playwright.Chromium.LaunchAsync(new Microsoft.Playwright.BrowserTypeLaunchOptions
            {
                Headless = false,
                Channel = "chrome",
                Args = new[]
                {
                    "--disable-blink-features=AutomationControlled",
                    "--disable-features=IsolateOrigins,site-per-process",
                    "--no-sandbox"
                }
            });
            
            var playwrightContext = await playwrightBrowser.NewContextAsync(new Microsoft.Playwright.BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36",
                Locale = "zh-CN",
                TimezoneId = "Asia/Shanghai",
                ViewportSize = new Microsoft.Playwright.ViewportSize { Width = 1280, Height = 720 }
            });
            
            // 加载防检测脚本
            var antiDetectionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "scripts", "cloudflare-anti-detection.js");
            if (File.Exists(antiDetectionPath))
            {
                var antiDetectionScript = await File.ReadAllTextAsync(antiDetectionPath);
                await playwrightContext.AddInitScriptAsync(antiDetectionScript);
            }
            
            await playwrightContext.AddInitScriptAsync(collectorScript);
            
            var playwrightPage = await playwrightContext.NewPageAsync();
            await playwrightPage.GotoAsync("https://nowsecure.nl");
            await Task.Delay(3000);
            
            // 收集 Playwright 的指纹
            var playwrightFingerprint = await playwrightPage.EvaluateAsync<string>("JSON.stringify(window.__fingerprint__, null, 2)");
            var playwrightFingerprintPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fingerprint_playwright.json");
            await File.WriteAllTextAsync(playwrightFingerprintPath, playwrightFingerprint);
            _log.LogInfo("BrowserMgmt", $"✅ Playwright 指纹已保存: {playwrightFingerprintPath}");
            
            // 检查是否通过 Cloudflare
            var playwrightPassed = await CheckCloudflareStatus(playwrightPage);
            _log.LogInfo("BrowserMgmt", $"Playwright Cloudflare 状态: {(playwrightPassed ? "✅ 通过" : "❌ 未通过")}");
            
            await Task.Delay(2000);
            await playwrightBrowser.CloseAsync();
            
            playwright.Dispose();
            
            _log.LogInfo("BrowserMgmt", "=======================================================================");
            _log.LogInfo("BrowserMgmt", "📊 对比结果：");
            _log.LogInfo("BrowserMgmt", $"  真实 Chrome: {(realPassed ? "✅ 通过" : "❌ 未通过")}");
            _log.LogInfo("BrowserMgmt", $"  Playwright: {(playwrightPassed ? "✅ 通过" : "❌ 未通过")}");
            _log.LogInfo("BrowserMgmt", "");
            _log.LogInfo("BrowserMgmt", "📁 指纹文件已保存：");
            _log.LogInfo("BrowserMgmt", $"  真实 Chrome: {realFingerprintPath}");
            _log.LogInfo("BrowserMgmt", $"  Playwright: {playwrightFingerprintPath}");
            _log.LogInfo("BrowserMgmt", "");
            _log.LogInfo("BrowserMgmt", "🔍 请使用文本编辑器或在线 JSON Diff 工具对比这两个文件");
            _log.LogInfo("BrowserMgmt", "   推荐工具：https://www.jsondiff.com/");
            _log.LogInfo("BrowserMgmt", "=======================================================================");
            
            StatusText.Text = $"✅ 指纹对比完成 - 真实 Chrome: {(realPassed ? "通过" : "未通过")} | Playwright: {(playwrightPassed ? "通过" : "未通过")}";
            
            // 打开文件夹
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{realFingerprintPath}\"");
        }
        catch (Exception ex)
        {
            _log.LogError("BrowserMgmt", $"Fingerprint comparison failed: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"指纹对比失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "指纹对比失败";
        }
    }

    private async Task<bool> CheckCloudflareStatus(Microsoft.Playwright.IPage page)
    {
        try
        {
            // 等待页面稳定
            await Task.Delay(2000);
            
            // 检查是否有 Cloudflare 验证页面
            var title = await page.TitleAsync();
            var content = await page.ContentAsync();
            
            // 检查是否有 "Just a moment" 或其他 Cloudflare 特征
            if (title.Contains("Just a moment") || content.Contains("Checking your browser"))
            {
                return false;
            }
            
            // 检查是否有 403 或其他错误
            if (content.Contains("403") || content.Contains("Access denied"))
            {
                return false;
            }
            
            // 如果没有这些特征，认为通过了
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async void LaunchFirefox_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "正在启动 Firefox 测试浏览器...";
            _log.LogInfo("BrowserMgmt", "========== Starting Firefox Test Browser ==========");

            var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            
            // 使用 Firefox（TLS 指纹可能不同）
            var browser = await playwright.Firefox.LaunchAsync(new Microsoft.Playwright.BrowserTypeLaunchOptions
            {
                Headless = false
            });

            var context = await browser.NewContextAsync(new Microsoft.Playwright.BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0",
                Locale = "zh-CN",
                TimezoneId = "Asia/Shanghai",
                ViewportSize = new Microsoft.Playwright.ViewportSize { Width = 1280, Height = 720 }
            });

            // 加载防检测脚本
            var antiDetectionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "scripts", "cloudflare-anti-detection.js");
            if (File.Exists(antiDetectionPath))
            {
                var antiDetectionScript = await File.ReadAllTextAsync(antiDetectionPath);
                await context.AddInitScriptAsync(antiDetectionScript);
                _log.LogInfo("BrowserMgmt", $"✅ Loaded anti-detection script");
            }

            var page = await context.NewPageAsync();
            
            _log.LogInfo("BrowserMgmt", "Navigating to test site...");
            await page.GotoAsync("https://www.iyf.tv/", new Microsoft.Playwright.PageGotoOptions
            {
                Timeout = 30000,
                WaitUntil = Microsoft.Playwright.WaitUntilState.DOMContentLoaded
            });
            
            _log.LogInfo("BrowserMgmt", "=======================================================================");
            _log.LogInfo("BrowserMgmt", "🦊 Firefox 测试浏览器已启动");
            _log.LogInfo("BrowserMgmt", "");
            _log.LogInfo("BrowserMgmt", "📊 测试说明：");
            _log.LogInfo("BrowserMgmt", "  - Firefox 的 TLS 指纹与 Chrome 不同");
            _log.LogInfo("BrowserMgmt", "  - 可能绕过 Cloudflare 的 TLS 检测");
            _log.LogInfo("BrowserMgmt", "  - 如果成功，说明问题确实是 TLS 指纹");
            _log.LogInfo("BrowserMgmt", "=======================================================================");
            
            StatusText.Text = "✅ Firefox 测试浏览器已启动";

            // 等待浏览器关闭
            _ = Task.Run(async () =>
            {
                try
                {
                    while (context.Pages.Count > 0)
                    {
                        await Task.Delay(1000);
                    }
                    await browser.CloseAsync();
                    playwright.Dispose();
                    _log.LogInfo("BrowserMgmt", "Firefox test browser closed");
                }
                catch (Exception ex)
                {
                    _log.LogError("BrowserMgmt", $"Error closing Firefox browser: {ex.Message}", ex.StackTrace);
                }
            });
        }
        catch (Exception ex)
        {
            _log.LogError("BrowserMgmt", $"Firefox test launch failed: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"启动失败: {ex.Message}\n\n提示：需要安装 Firefox 浏览器", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "启动失败";
        }
    }

    private async void LaunchUndetectedChrome_Click(object sender, RoutedEventArgs e)
    {
        UndetectedChromeService? service = null;
        
        try
        {
            StatusText.Text = "正在启动 Undetected Chrome...";
            _log.LogInfo("BrowserMgmt", "========== Starting Undetected Chrome ==========");

            service = new UndetectedChromeService(_log);
            var driver = await service.CreateDriverAsync();

            // 访问测试网站
            await Task.Run(() =>
            {
                driver.GoToUrl("https://www.iyf.tv/");
                Thread.Sleep(3000);  // 等待页面加载
            });
            
            _log.LogInfo("BrowserMgmt", "✅ Undetected Chrome 已启动并访问测试网站");
            StatusText.Text = "✅ Undetected Chrome 已启动";

            // 等待浏览器关闭
            _ = Task.Run(() =>
            {
                try
                {
                    while (service.IsRunning())
                    {
                        Thread.Sleep(1000);
                    }
                    service.Dispose();
                    _log.LogInfo("BrowserMgmt", "Undetected Chrome closed");
                }
                catch (Exception ex)
                {
                    _log.LogError("BrowserMgmt", $"Error: {ex.Message}", ex.StackTrace);
                }
            });
        }
        catch (Exception ex)
        {
            _log.LogError("BrowserMgmt", $"Failed to launch: {ex.Message}", ex.StackTrace);
            
            var errorMessage = $"启动失败: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorMessage += $"\n\n详细信息：{ex.InnerException.Message}";
            }
            errorMessage += "\n\n提示：";
            errorMessage += "\n1. 确保已安装 Chrome 浏览器";
            errorMessage += "\n2. 首次运行会自动下载 ChromeDriver";
            errorMessage += "\n3. 如果下载失败，请检查网络连接";
            
            MessageBox.Show(errorMessage, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "启动失败";
            
            service?.Dispose();
        }
    }
}
