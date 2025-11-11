using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Collections.Generic;

namespace WebScraperApp.Views
{
    /// <summary>
    /// Cloudflare 绕过测试窗口 - 完全独立的实现
    /// </summary>
    public partial class CloudflareTestWindow : Window
    {
        private ChromeDriver? _driver;
        private bool _isRunning = false;

        public CloudflareTestWindow()
        {
            InitializeComponent();
            Log("✅ Cloudflare 测试窗口已初始化");
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                LogTextBox.ScrollToEnd();
            });
        }

        private void UpdateStatus(string status)
        {
            Dispatcher.Invoke(() =>
            {
                StatusTextBlock.Text = status;
            });
        }

        private async void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
            {
                MessageBox.Show("浏览器已在运行中！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LaunchButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            UpdateStatus("正在启动浏览器...");

            try
            {
                await Task.Run(() => LaunchBrowser());
            }
            catch (Exception ex)
            {
                Log($"❌ 启动失败: {ex.Message}");
                MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                LaunchButton.IsEnabled = true;
                StopButton.IsEnabled = false;
                UpdateStatus("启动失败");
            }
        }

        private void LaunchBrowser()
        {
            try
            {
                Log("🚀 开始启动浏览器...");

                // 1. 获取配置
                string url = "";
                string platform = "";
                string userAgent = "";

                Dispatcher.Invoke(() =>
                {
                    url = UrlTextBox.Text.Trim();
                    platform = GetSelectedPlatform();
                    userAgent = UserAgentTextBox.Text.Trim();
                });

                Log($"📋 配置信息:");
                Log($"   URL: {url}");
                Log($"   Platform: {platform}");
                Log($"   User-Agent: {userAgent.Substring(0, Math.Min(80, userAgent.Length))}...");

                // 2. 计算 vendor
                string vendor = GetVendorForPlatform(platform);
                Log($"   Vendor: {vendor}");

                // 3. 设置 Chrome 选项
                var options = new ChromeOptions();
                
                // 基础选项
                options.AddArgument("--disable-blink-features=AutomationControlled");
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-gpu");
                options.AddArgument("--disable-software-rasterizer");
                options.AddArgument("--disable-extensions");
                options.AddArgument("--disable-popup-blocking");
                
                // 不使用 mobileEmulation，改用 CDP 设置
                // 这样可以避免冲突，并且有更精确的控制
                Log("📝 将通过 CDP 设置设备指标（不使用 mobileEmulation）");
                
                // 排除自动化标志
                options.AddExcludedArgument("enable-automation");
                options.AddAdditionalOption("useAutomationExtension", false);

                // 实验性选项
                options.AddUserProfilePreference("credentials_enable_service", false);
                options.AddUserProfilePreference("profile.password_manager_enabled", false);
                
                // 禁用 WebRTC（防止 IP 泄露）
                options.AddUserProfilePreference("webrtc.ip_handling_policy", "disable_non_proxied_udp");
                options.AddUserProfilePreference("webrtc.multiple_routes_enabled", false);
                options.AddUserProfilePreference("webrtc.nonproxied_udp_enabled", false);

                Log("✅ Chrome 选项配置完成");

                // 4. 启动 ChromeDriver
                Log("🔧 正在启动 ChromeDriver...");
                _driver = new ChromeDriver(options);
                _isRunning = true;
                Log("✅ ChromeDriver 启动成功");

                // 5. 设置设备指标（通过 CDP）
                SetDeviceMetrics(platform);

                // 6. 注入防检测脚本（通过 CDP）
                InjectAntiDetectionScripts(platform, vendor, userAgent);

                // 7. 导航到目标 URL
                Log($"🌐 正在访问: {url}");
                UpdateStatus($"正在访问 {url}");
                _driver.Navigate().GoToUrl(url);
                
                Log("✅ 页面加载完成");
                UpdateStatus("浏览器运行中");

                // 7. 等待浏览器关闭
                Log("⏳ 浏览器已启动，等待用户操作...");
                Log("💡 提示: 按 F12 打开开发者工具查看控制台日志");
                
                WaitForBrowserClose();
            }
            catch (Exception ex)
            {
                Log($"❌ 错误: {ex.Message}");
                Log($"   堆栈: {ex.StackTrace}");
                throw;
            }
        }

        private void SetDeviceMetrics(string platform)
        {
            if (_driver == null) return;

            try
            {
                if (platform == "iPhone" || platform == "iPad")
                {
                    Log("📱 设置移动设备指标...");

                    // iPhone 12 Pro 的屏幕尺寸
                    int width = platform == "iPhone" ? 390 : 820;
                    int height = platform == "iPhone" ? 844 : 1180;
                    double deviceScaleFactor = 3.0;

                    var metricsParams = new Dictionary<string, object>
                    {
                        { "width", width },
                        { "height", height },
                        { "deviceScaleFactor", deviceScaleFactor },
                        { "mobile", true },
                        { "screenWidth", width },
                        { "screenHeight", height },
                        { "positionX", 0 },
                        { "positionY", 0 }
                    };

                    _driver.ExecuteCdpCommand("Emulation.setDeviceMetricsOverride", metricsParams);
                    Log($"✅ 设备指标已设置: {width}x{height}, DPR={deviceScaleFactor}");

                    // 设置触摸事件模拟
                    var touchParams = new Dictionary<string, object>
                    {
                        { "enabled", true },
                        { "configuration", "mobile" }
                    };
                    _driver.ExecuteCdpCommand("Emulation.setTouchEmulationEnabled", touchParams);
                    Log("✅ 触摸事件模拟已启用");

                    // 设置 User-Agent 覆盖（确保一致性）
                    var uaParams = new Dictionary<string, object>
                    {
                        { "userAgent", "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1" },
                        { "platform", platform },
                        { "userAgentMetadata", new Dictionary<string, object>
                            {
                                { "brands", new object[]
                                    {
                                        new Dictionary<string, object> { { "brand", "Not A(Brand" }, { "version", "99" } },
                                        new Dictionary<string, object> { { "brand", "Safari" }, { "version", "17" } }
                                    }
                                },
                                { "fullVersion", "17.0" },
                                { "platform", "iOS" },
                                { "platformVersion", "17.0" },
                                { "architecture", "arm64" },
                                { "model", platform == "iPhone" ? "iPhone" : "iPad" },
                                { "mobile", true }
                            }
                        }
                    };
                    _driver.ExecuteCdpCommand("Emulation.setUserAgentOverride", uaParams);
                    Log("✅ User-Agent 覆盖已设置");
                }
            }
            catch (Exception ex)
            {
                Log($"⚠️ 设备指标设置失败: {ex.Message}");
            }
        }

        private void InjectAntiDetectionScripts(string platform, string vendor, string userAgent)
        {
            if (_driver == null) return;

            try
            {
                Log("💉 开始注入防检测脚本...");

                // 构建完整的防检测脚本
                string script = BuildAntiDetectionScript(platform, vendor, userAgent);

                // 通过 CDP 注入脚本
                var cdpCommand = new Dictionary<string, object>
                {
                    { "source", script }
                };

                _driver.ExecuteCdpCommand("Page.addScriptToEvaluateOnNewDocument", cdpCommand);
                Log("✅ 防检测脚本已通过 CDP 注入");

                // 执行一次脚本（用于当前页面）
                ((IJavaScriptExecutor)_driver).ExecuteScript(script);
                Log("✅ 防检测脚本已在当前页面执行");
            }
            catch (Exception ex)
            {
                Log($"⚠️ 脚本注入失败: {ex.Message}");
            }
        }

        private string BuildAntiDetectionScript(string platform, string vendor, string userAgent)
        {
            // 转义字符串
            string escapedUserAgent = userAgent.Replace("'", "\\'").Replace("\"", "\\\"");
            string escapedVendor = vendor.Replace("'", "\\'");
            string escapedPlatform = platform.Replace("'", "\\'");

            return $@"
(function() {{
    'use strict';
    console.log('[CF Test] 🚀 Initializing...');
    
    // 1. Remove webdriver
    delete Object.getPrototypeOf(navigator).webdriver;
    delete navigator.__proto__.webdriver;
    delete navigator.webdriver;
    Object.defineProperty(navigator, 'webdriver', {{ get: () => undefined, configurable: true }});
    console.log('[CF Test] ✅ webdriver removed');
    
    // 2. Set vendor
    Object.defineProperty(navigator, 'vendor', {{ get: () => '{escapedVendor}', configurable: true }});
    console.log('[CF Test] ✅ vendor: {escapedVendor}');
    
    // 3. Set platform
    Object.defineProperty(navigator, 'platform', {{ get: () => '{escapedPlatform}', configurable: true }});
    console.log('[CF Test] ✅ platform: {escapedPlatform}');
    
    // 4. Set userAgent
    Object.defineProperty(navigator, 'userAgent', {{ get: () => '{escapedUserAgent}', configurable: true }});
    console.log('[CF Test] ✅ userAgent set');
    
    // 5. Remove automation traces
    ['__webdriver_script_fn', '__driver_evaluate', '__playwright', '$cdc_asdjflasutopfhvcZLmcfl_'].forEach(prop => {{
        try {{ delete window[prop]; }} catch(e) {{}}
    }});
    console.log('[CF Test] ✅ Automation traces removed');
    
    // 6. Enhance Chrome object
    if (!window.chrome) window.chrome = {{}};
    window.chrome.app = {{ isInstalled: false }};
    window.chrome.csi = function() {{ return {{ startE: Date.now(), onloadT: Date.now(), pageT: Math.random() * 1000, tran: 15 }}; }};
    window.chrome.loadTimes = function() {{ return {{ requestTime: Date.now() / 1000, startLoadTime: Date.now() / 1000 }}; }};
    console.log('[CF Test] ✅ Chrome object enhanced');
    
    // 7. Fix Permissions API
    const originalQuery = navigator.permissions.query;
    navigator.permissions.query = function(params) {{
        if (params.name === 'notifications') return Promise.resolve({{ state: 'default', onchange: null }});
        return originalQuery.apply(this, arguments);
    }};
    console.log('[CF Test] ✅ Permissions API patched');
    
    // 8. Fix Performance API
    const originalGetEntriesByType = window.performance.getEntriesByType;
    window.performance.getEntriesByType = function(type) {{
        const entries = originalGetEntriesByType.call(this, type);
        if (type === 'navigation' && entries.length === 0) {{
            return [{{ name: document.location.href, entryType: 'navigation', startTime: 0, duration: Math.random() * 1000 }}];
        }}
        return entries;
    }};
    console.log('[CF Test] ✅ Performance API fixed');
    
    // 9. Intercept Turnstile requests
    const originalFetch = window.fetch;
    window.fetch = function(...args) {{
        const url = args[0];
        if (typeof url === 'string' && url.includes('challenges.cloudflare.com')) {{
            console.log('[CF Test] 🎯 Intercepting Turnstile request');
            if (args[1]) {{
                args[1].headers = args[1].headers || {{}};
                const isMobile = '{escapedPlatform}' === 'iPhone' || '{escapedPlatform}' === 'iPad';
                args[1].headers['sec-ch-ua-mobile'] = isMobile ? '?1' : '?0';
            }}
        }}
        return originalFetch.apply(this, args);
    }};
    console.log('[CF Test] ✅ Turnstile interception enabled');
    
    // 10. PAT support - 更强的处理
    if (!document.hasPrivateToken) {{
        document.hasPrivateToken = function(issuer) {{ 
            console.log('[CF Test] 🔐 PAT requested for:', issuer);
            return Promise.resolve(false); 
        }};
    }}
    
    // 拦截 PAT 请求
    const originalXHROpen = XMLHttpRequest.prototype.open;
    XMLHttpRequest.prototype.open = function(method, url, ...args) {{
        if (typeof url === 'string' && url.includes('/pat/')) {{
            console.log('[CF Test] 🚫 Blocking PAT XHR request:', url);
            // 不阻止，让它继续但记录
        }}
        return originalXHROpen.call(this, method, url, ...args);
    }};
    console.log('[CF Test] ✅ PAT support added');
    
    // 11. WebGPU 模拟
    if (!navigator.gpu) {{
        navigator.gpu = {{
            requestAdapter: function() {{
                console.log('[CF Test] 🎨 WebGPU adapter requested');
                return Promise.resolve(null);
            }}
        }};
        console.log('[CF Test] ✅ WebGPU mocked');
    }}
    
    // 12. 添加真实的触摸事件支持（iPhone 必须有）
    let touchSupported = false;
    try {{
        document.createEvent('TouchEvent');
        touchSupported = true;
    }} catch(e) {{}}
    
    if (!touchSupported && '{escapedPlatform}' === 'iPhone') {{
        console.log('[CF Test] ⚠️ Touch events not supported, but platform is iPhone');
    }} else {{
        console.log('[CF Test] ✅ Touch events: ' + touchSupported);
    }}
    
    // 13. 修复 screen 对象（iPhone 特定）
    if ('{escapedPlatform}' === 'iPhone' || '{escapedPlatform}' === 'iPad') {{
        Object.defineProperty(screen, 'width', {{ get: () => 390, configurable: true }});
        Object.defineProperty(screen, 'height', {{ get: () => 844, configurable: true }});
        Object.defineProperty(screen, 'availWidth', {{ get: () => 390, configurable: true }});
        Object.defineProperty(screen, 'availHeight', {{ get: () => 844, configurable: true }});
        Object.defineProperty(window, 'innerWidth', {{ get: () => 390, configurable: true }});
        Object.defineProperty(window, 'innerHeight', {{ get: () => 844, configurable: true }});
        Object.defineProperty(window, 'devicePixelRatio', {{ get: () => 3, configurable: true }});
        console.log('[CF Test] ✅ iPhone screen dimensions set (390x844, DPR=3)');
    }}
    
    console.log('[CF Test] ✅✅✅ All bypasses applied!');
    console.log('[CF Test] 📊 Summary:');
    console.log('  - webdriver: ' + navigator.webdriver);
    console.log('  - vendor: ' + navigator.vendor);
    console.log('  - platform: ' + navigator.platform);
    console.log('  - screen: ' + screen.width + 'x' + screen.height);
    console.log('  - devicePixelRatio: ' + window.devicePixelRatio);
}})();
";
        }

        private string GetSelectedPlatform()
        {
            int index = PlatformComboBox.SelectedIndex;
            return index switch
            {
                0 => "iPhone",
                1 => "iPad",
                2 => "Win32",
                3 => "MacIntel",
                4 => "Linux armv8l",
                _ => "Win32"
            };
        }

        private string GetVendorForPlatform(string platform)
        {
            return platform switch
            {
                "iPhone" or "iPad" or "iPod" or "MacIntel" => "Apple Computer, Inc.",
                "Linux armv8l" => "Google Inc.",
                _ => "Google Inc."
            };
        }

        private void WaitForBrowserClose()
        {
            try
            {
                while (_isRunning && _driver != null)
                {
                    try
                    {
                        _ = _driver.Title;
                        System.Threading.Thread.Sleep(1000);
                    }
                    catch
                    {
                        break;
                    }
                }
            }
            finally
            {
                Dispatcher.Invoke(() =>
                {
                    Log("🛑 浏览器已关闭");
                    UpdateStatus("浏览器已关闭");
                    LaunchButton.IsEnabled = true;
                    StopButton.IsEnabled = false;
                    _isRunning = false;
                });
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_driver != null)
                {
                    Log("🛑 正在关闭浏览器...");
                    _driver.Quit();
                    _driver.Dispose();
                    _driver = null;
                    _isRunning = false;
                    Log("✅ 浏览器已关闭");
                    UpdateStatus("浏览器已关闭");
                }

                LaunchButton.IsEnabled = true;
                StopButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                Log($"❌ 关闭失败: {ex.Message}");
            }
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Clear();
            Log("✅ 日志已清空");
        }

        private async void PythonServiceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PythonServiceButton.IsEnabled = false;
                UpdateStatus("正在通过 Python 服务解决 Cloudflare 挑战...");
                Log("🐍 使用 Python undetected-chromedriver 服务");
                Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                using var httpClient = new System.Net.Http.HttpClient 
                { 
                    Timeout = TimeSpan.FromMinutes(2) 
                };

                // 1. 检查服务是否运行
                Log("🔍 检查 Python 服务状态...");
                try
                {
                    var healthResponse = await httpClient.GetAsync("http://localhost:5000/health");
                    if (!healthResponse.IsSuccessStatusCode)
                    {
                        Log("❌ Python 服务未运行");
                        MessageBox.Show(
                            "Python 服务未运行\n\n" +
                            "请先启动服务:\n" +
                            "1. 打开 PowerShell\n" +
                            "2. cd d:\\1Dev\\webbrowser\\python\n" +
                            "3. python cloudflare_bypass_service.py\n\n" +
                            "或者双击运行: python\\start_service.bat",
                            "错误",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }

                    var healthBody = await healthResponse.Content.ReadAsStringAsync();
                    var healthJson = System.Text.Json.JsonDocument.Parse(healthBody);
                    var serviceName = healthJson.RootElement.GetProperty("service").GetString();
                    var version = healthJson.RootElement.GetProperty("version").GetString();

                    Log($"✅ Python 服务运行正常");
                    Log($"   服务: {serviceName}");
                    Log($"   版本: {version}");
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    Log("❌ 无法连接到 Python 服务");
                    MessageBox.Show(
                        "无法连接到 Python 服务\n\n" +
                        "请确保服务已启动:\n" +
                        "cd d:\\1Dev\\webbrowser\\python\n" +
                        "python cloudflare_bypass_service.py",
                        "错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // 2. 解决 Cloudflare 挑战
                Log("\n🚀 开始解决 Cloudflare 挑战...");
                Log($"   URL: {UrlTextBox.Text}");
                Log($"   模式: 显示浏览器窗口");
                Log($"   等待时间: 15 秒");
                Log("\n💡 浏览器窗口会自动打开，请稍候...");
                Log("💡 undetected-chromedriver 会自动处理验证");

                var requestData = new
                {
                    url = UrlTextBox.Text,
                    headless = false,
                    timeout = 60,
                    wait_time = 15
                };

                var json = System.Text.Json.JsonSerializer.Serialize(requestData);
                var content = new System.Net.Http.StringContent(
                    json,
                    System.Text.Encoding.UTF8,
                    "application/json");

                UpdateStatus("⏳ 正在解决挑战（15-30 秒）...");

                var response = await httpClient.PostAsync("http://localhost:5000/solve", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                var result = System.Text.Json.JsonDocument.Parse(responseBody);

                if (result.RootElement.GetProperty("success").GetBoolean())
                {
                    var cookies = result.RootElement.GetProperty("cookies");
                    var userAgent = result.RootElement.GetProperty("user_agent").GetString();
                    var sessionFile = result.RootElement.GetProperty("session_file").GetString();
                    var currentUrl = result.RootElement.GetProperty("current_url").GetString();

                    Log("\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    Log("✅ 挑战成功!");
                    Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    Log($"📊 Cookies: {cookies.EnumerateObject().Count()} 个");
                    Log($"🌐 当前 URL: {currentUrl}");
                    Log($"🔧 User-Agent: {userAgent}");
                    Log($"💾 会话文件: {sessionFile}");

                    // 显示 cookies
                    Log("\n📋 获取到的 Cookies:");
                    foreach (var cookie in cookies.EnumerateObject())
                    {
                        var value = cookie.Value.GetString() ?? "";
                        var displayValue = value.Length > 40 ? value.Substring(0, 40) + "..." : value;
                        Log($"   • {cookie.Name}: {displayValue}");
                    }

                    Log("\n💡 提示:");
                    Log("   1. Cookies 已保存到会话文件");
                    Log("   2. 可以在 C# 中使用这些 cookies 进行后续请求");
                    Log("   3. 会话有效期通常为 1-24 小时");
                    Log("   4. 可以调用 /get_session API 获取已保存的会话");

                    UpdateStatus("✅ 挑战成功！");

                    MessageBox.Show(
                        $"Cloudflare 挑战成功！\n\n" +
                        $"✅ Cookies: {cookies.EnumerateObject().Count()} 个\n" +
                        $"✅ 会话已保存\n" +
                        $"✅ 可以使用这些 cookies 进行后续请求\n\n" +
                        $"会话文件:\n{sessionFile}",
                        "成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    var error = result.RootElement.GetProperty("error").GetString();
                    Log($"\n❌ 挑战失败: {error}");

                    if (result.RootElement.TryGetProperty("traceback", out var traceback))
                    {
                        Log($"\n详细错误:\n{traceback.GetString()}");
                    }

                    UpdateStatus($"❌ 失败: {error}");

                    MessageBox.Show(
                        $"挑战失败: {error}\n\n" +
                        "可能的原因:\n" +
                        "1. 网络连接问题\n" +
                        "2. Cloudflare 检测到自动化\n" +
                        "3. IP 被封禁\n\n" +
                        "建议:\n" +
                        "1. 检查网络连接\n" +
                        "2. 尝试使用代理\n" +
                        "3. 增加 wait_time",
                        "错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Log($"\n❌ 错误: {ex.Message}");
                UpdateStatus($"❌ 错误: {ex.Message}");
                MessageBox.Show(
                    $"发生错误: {ex.Message}\n\n{ex.StackTrace}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                PythonServiceButton.IsEnabled = true;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_driver != null)
            {
                try
                {
                    _driver.Quit();
                    _driver.Dispose();
                }
                catch { }
            }

            base.OnClosing(e);
        }
    }
}
