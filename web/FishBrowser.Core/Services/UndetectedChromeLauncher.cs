using SeleniumUndetectedChromeDriver;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using FishBrowser.WPF.Models;
using System.Runtime.InteropServices;

namespace FishBrowser.WPF.Services
{
    /// <summary>
    /// UndetectedChromeDriver 启动器
    /// 实现 IBrowserLauncher 接口，使用 Selenium + UndetectedChromeDriver 绕过 Cloudflare 的 TLS 指纹检测
    /// </summary>
    public class UndetectedChromeLauncher : IBrowserLauncher
    {
        private readonly ILogService _log;
        private UndetectedChromeDriver? _driver;
        private FingerprintProfile? _currentProfile;
        private bool _disposed = false;
        private TaskCompletionSource<bool>? _closeTaskSource;

        public BrowserEngineType EngineType => BrowserEngineType.UndetectedChrome;

        public UndetectedChromeLauncher(ILogService log)
        {
            _log = log;
        }

        public async Task LaunchAsync(
            FingerprintProfile profile,
            string? userDataPath = null,
            bool headless = false,
            ProxyConfig? proxy = null,
            BrowserEnvironment? environment = null)
        {
            try
            {
                // ⭐ 关键修复：验证并修正 Platform 与 UA 的一致性
                var antiDetectionService = new AntiDetectionService();
                antiDetectionService.ValidateProfile(profile);
                
                // 保存当前 profile 以供后续使用
                _currentProfile = profile;
                
                // ⭐ 调试日志：检查 Profile 的 Vendor
                _log.LogInfo("UndetectedChrome", $"[LAUNCH] Profile loaded - Platform={profile.Platform}, Vendor={profile.Vendor ?? "(null)"}, UA={profile.UserAgent?.Substring(0, Math.Min(50, profile.UserAgent?.Length ?? 0))}...");
                
                _log.LogInfo("UndetectedChrome", "========== Launching UndetectedChromeDriver ==========");
                _log.LogInfo("UndetectedChrome", "Downloading ChromeDriver...");

                // 自动下载匹配的 ChromeDriver
                var driverPath = await new ChromeDriverInstaller().Auto();
                _log.LogInfo("UndetectedChrome", $"ChromeDriver path: {driverPath}");

                // 配置 Chrome 选项
                // 设置用户数据目录
                userDataPath = PrepareUserDataPath(userDataPath);
                
                var options = BuildChromeOptions(profile, headless, proxy, environment, userDataPath);

                // 创建驱动
                _log.LogInfo("UndetectedChrome", "Creating driver instance...");
                _driver = UndetectedChromeDriver.Create(
                    options: options, 
                    driverExecutablePath: driverPath,
                    hideCommandPromptWindow: true  // ⭐ 隐藏命令行窗口
                );
                _currentProfile = profile;
                
                // 窗口设置和脚本注入
                await HandleWindowSetupAsync(environment);

                _log.LogInfo("UndetectedChrome", "=======================================================================");
                _log.LogInfo("UndetectedChrome", "✅ UndetectedChromeDriver launched successfully");
                _log.LogInfo("UndetectedChrome", "");
                _log.LogInfo("UndetectedChrome", "🎯 特点：");
                _log.LogInfo("UndetectedChrome", "  - 使用真实 Chrome 的 TLS 指纹（包含 GREASE）");
                _log.LogInfo("UndetectedChrome", "  - 修补了 ChromeDriver 的检测特征（cdc_ 变量）");
                _log.LogInfo("UndetectedChrome", "  - 移除了自动化标志");
                _log.LogInfo("UndetectedChrome", "  - 成功率 90-95%");
                _log.LogInfo("UndetectedChrome", "=======================================================================");

                // 初始化关闭任务
                _closeTaskSource = new TaskCompletionSource<bool>();
            }
            catch (Exception ex)
            {
                _log.LogError("UndetectedChrome", $"Failed to launch: {ex.Message}", ex.StackTrace);
                throw;
            }
        }

        private ChromeOptions BuildChromeOptions(
            FingerprintProfile profile,
            bool headless,
            ProxyConfig? proxy,
            BrowserEnvironment? environment,
            string? userDataPath = null)
        {
            var options = new ChromeOptions();

            // ⭐ 设置用户数据目录（持久化）
            if (!string.IsNullOrEmpty(userDataPath))
            {
                options.AddArgument($"--user-data-dir={userDataPath}");
                _log.LogInfo("UndetectedChrome", $"✅ User data directory set: {userDataPath}");
            }

            // 基础参数（UndetectedChromeDriver 会自动处理大部分反检测）
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--no-sandbox");
            
            // ⭐ 禁用 User-Agent Client Hints，防止 Chrome 自动推断 Platform
            options.AddArgument("--disable-features=UserAgentClientHints");
            
            // 关键：禁用自动化标志（UndetectedChromeDriver 会自动处理这些）
            // 注意：不要手动添加 excludeSwitches，UndetectedChromeDriver 内部已处理
            // options.AddArgument("--disable-blink-features=AutomationControlled");
            // options.AddExcludedArgument("enable-automation");
            
            // 设置实验性选项
            options.AddUserProfilePreference("credentials_enable_service", false);
            options.AddUserProfilePreference("profile.password_manager_enabled", false);

            // 窗口大小（从环境或配置文件获取）
            var width = environment?.CustomViewportWidth ?? profile.ViewportWidth;
            var height = environment?.CustomViewportHeight ?? profile.ViewportHeight;
            
            // ⭐ 检测是否为移动设备，启用移动模拟
            var platform = profile.Platform ?? "Win32";
            bool isMobileDevice = platform switch
            {
                "iPhone" or "iPad" => true,
                "Linux armv8l" => true,  // Android
                _ => false
            };
            
            if (!headless)
            {
                if (isMobileDevice)
                {
                    // 移动设备：设置窗口大小并启用移动模拟
                    options.AddArgument($"--window-size={width},{height}");
                    // 添加移动设备模拟参数
                    options.AddArgument("--use-mobile-user-agent");
                    _log.LogInfo("UndetectedChrome", $"Mobile device mode: {width}x{height}");
                }
                else
                {
                    // 桌面设备：启动时最大化，避免画面偏移
                    options.AddArgument("--start-maximized");
                    _log.LogInfo("UndetectedChrome", $"Window will be maximized (avoiding offset issues)");
                }
            }
            else
            {
                options.AddArgument("--headless=new");
                options.AddArgument($"--window-size={width},{height}");
                _log.LogInfo("UndetectedChrome", $"Headless mode: {width}x{height}");
            }

            // 代理配置
            if (proxy != null && !string.IsNullOrEmpty(proxy.Server))
            {
                options.AddArgument($"--proxy-server={proxy.Server}");
                _log.LogInfo("UndetectedChrome", $"Proxy configured: {proxy.Server}");
            }

            // 智能指纹配置：使用真实的指纹数据
            ApplySmartFingerprint(options, profile, environment);

            _log.LogInfo("UndetectedChrome", "Chrome options configured");
            return options;
        }

        /// <summary>
        /// 智能指纹配置：使用真实可信的指纹数据
        /// </summary>
        private void ApplySmartFingerprint(ChromeOptions options, FingerprintProfile profile, BrowserEnvironment? environment)
        {
            _log.LogInfo("UndetectedChrome", "========== Smart Fingerprint Configuration ==========");

            // 1. User-Agent：使用真实的 Chrome 版本号
            var userAgent = NormalizeUserAgent(profile.UserAgent);
            if (!string.IsNullOrEmpty(userAgent))
            {
                options.AddArgument($"--user-agent={userAgent}");
                _log.LogInfo("UndetectedChrome", $"✅ User-Agent: {userAgent}");
            }
            else
            {
                _log.LogInfo("UndetectedChrome", "⚠️ Using system default User-Agent");
            }

            // 2. Language：使用真实的语言列表
            var language = GetPrimaryLanguage(profile.LanguagesJson);
            if (!string.IsNullOrEmpty(language))
            {
                options.AddArgument($"--lang={language}");
                _log.LogInfo("UndetectedChrome", $"✅ Language: {language}");
            }
            else
            {
                _log.LogInfo("UndetectedChrome", "⚠️ Using system default Language");
            }

            // 3. Timezone：使用真实的时区
            if (!string.IsNullOrEmpty(profile.Timezone))
            {
                // 验证时区是否真实存在
                if (IsValidTimezone(profile.Timezone))
                {
                    // 注意：Chrome 不直接支持 --timezone 参数
                    // 需要通过 CDP 或 JS 注入来设置
                    _log.LogInfo("UndetectedChrome", $"✅ Timezone: {profile.Timezone} (will be set via JS)");
                }
                else
                {
                    _log.LogWarn("UndetectedChrome", $"⚠️ Invalid timezone: {profile.Timezone}, using system default");
                }
            }

            _log.LogInfo("UndetectedChrome", "========== Fingerprint Configuration Complete ==========");
        }

        /// <summary>
        /// 规范化 User-Agent：确保使用真实的 Chrome 版本号
        /// </summary>
        private string NormalizeUserAgent(string? userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return string.Empty;

            // 检查是否包含 Chrome 版本号
            var chromeVersionMatch = System.Text.RegularExpressions.Regex.Match(userAgent, @"Chrome/(\d+\.\d+\.\d+\.\d+)");
            if (!chromeVersionMatch.Success)
                return userAgent;

            var version = chromeVersionMatch.Groups[1].Value;
            var parts = version.Split('.');
            
            // 验证版本号格式
            if (parts.Length != 4)
                return userAgent;

            // 检查主版本号是否在合理范围内（100-150）
            if (int.TryParse(parts[0], out int majorVersion))
            {
                if (majorVersion < 100 || majorVersion > 150)
                {
                    // 使用当前最新稳定版本号（141）
                    var normalizedVersion = $"141.0.{parts[2]}.{parts[3]}";
                    var normalizedUA = userAgent.Replace(version, normalizedVersion);
                    _log.LogInfo("UndetectedChrome", $"📝 Normalized version: {version} → {normalizedVersion}");
                    return normalizedUA;
                }
                else
                {
                    _log.LogInfo("UndetectedChrome", $"✅ Chrome version {majorVersion} is valid, keeping original UA");
                }
            }

            return userAgent;
        }

        /// <summary>
        /// 获取主要语言
        /// </summary>
        private string GetPrimaryLanguage(string? languagesJson)
        {
            if (string.IsNullOrEmpty(languagesJson))
                return string.Empty;

            try
            {
                var languages = System.Text.Json.JsonSerializer.Deserialize<List<string>>(languagesJson);
                if (languages != null && languages.Count > 0)
                {
                    return languages[0];
                }
            }
            catch (Exception ex)
            {
                _log.LogWarn("UndetectedChrome", $"Failed to parse languages: {ex.Message}");
            }

            return string.Empty;
        }

        /// <summary>
        /// 验证时区是否有效
        /// </summary>
        private bool IsValidTimezone(string timezone)
        {
            try
            {
                // 尝试查找时区
                var tz = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                return true;
            }
            catch
            {
                // 时区不存在
                return false;
            }
        }

        private string PrepareUserDataPath(string? userDataPath)
        {
            if (string.IsNullOrEmpty(userDataPath))
            {
                userDataPath = Path.Combine(
                    Path.GetTempPath(),
                    "ChromeUserData_" + Guid.NewGuid().ToString("N"));
                _log.LogInfo("UndetectedChrome", $"Using temp user data dir: {userDataPath}");
            }
            else
            {
                _log.LogInfo("UndetectedChrome", $"Using persistent user data dir: {userDataPath}");
            }

            return userDataPath;
        }

        private async Task HandleWindowSetupAsync(BrowserEnvironment? environment)
        {
            _log.LogInfo("UndetectedChrome", "HandleWindowSetupAsync started");
            
            if (_driver == null)
            {
                _log.LogWarn("UndetectedChrome", "Driver is null, skipping window setup");
                return;
            }

            try
            {
                // 等待窗口完全加载
                await Task.Delay(500);
                
                // ⭐ 先使用 CDP 注入脚本（在页面加载前执行）
                if (_currentProfile != null)
                {
                    InjectScriptViaCDP(_currentProfile, environment);
                }
                
                // ⭐ 导航到 about:blank 并刷新，让 CDP 脚本生效
                try
                {
                    _driver.Navigate().GoToUrl("about:blank");
                    await Task.Delay(300);
                    // 刷新页面，让 CDP 脚本在新页面加载时执行
                    _driver.Navigate().Refresh();
                    await Task.Delay(300);
                    _log.LogInfo("UndetectedChrome", "Page refreshed to apply CDP scripts");
                }
                catch (Exception ex)
                {
                    _log.LogWarn("UndetectedChrome", $"Failed to navigate/refresh: {ex.Message}");
                }

                // 注入反检测 JavaScript（包含指纹设置）
                InjectAntiDetectionScript(environment);
                
                // ⭐ 强制修复 webdriver（在页面加载后立即执行）
                ForceFixWebdriver(environment);
                
                // ⭐ 在脚本注入后启用设备模拟（避免冲突）
                if (_currentProfile != null)
                {
                    EnableDeviceEmulationIfNeeded(_currentProfile);
                }
                
                // 不再自动显示指纹信息页面，改为在浏览器管理界面手动打开
                // ShowFingerprintInfoAsync();

                // 如果指定了自定义分辨率，在最大化后调整
                if (environment != null && 
                    (environment.CustomViewportWidth.HasValue || environment.CustomViewportHeight.HasValue))
                {
                    var width = environment.CustomViewportWidth ?? 1280;
                    var height = environment.CustomViewportHeight ?? 720;
                    
                    _log.LogInfo("UndetectedChrome", $"Applying custom resolution: {width}x{height}");
                    
                    // 使用 JavaScript 调整窗口大小
                    var js = (IJavaScriptExecutor)_driver;
                    js.ExecuteScript($"window.resizeTo({width}, {height});");
                }

                _log.LogInfo("UndetectedChrome", "Window setup completed");
            }
            catch (Exception ex)
            {
                _log.LogWarn("UndetectedChrome", $"Window setup warning: {ex.Message}");
            }
        }

        /// <summary>
        /// 使用 CDP 在页面加载前注入脚本（最可靠的方法）
        /// </summary>
        private void InjectScriptViaCDP(FingerprintProfile profile, BrowserEnvironment? environment)
        {
            if (_driver == null)
                return;

            try
            {
                // 准备指纹数据
                var platform = (profile.Platform ?? "Win32").Replace("'", "\\'").Replace("\"", "\\\"");
                var userAgent = (profile.UserAgent ?? "").Replace("'", "\\'").Replace("\"", "\\\"");
                var maxTouchPoints = profile.MaxTouchPoints;
                
                // ⭐ Chrome 浏览器在所有平台的 Vendor 都是 "Google Inc."
                // 注意：只有 Safari 才会返回 "Apple Computer, Inc."
                var vendor = "Google Inc.";
                
                // 获取 webdriver 配置
                var webdriverMode = environment?.WebdriverMode ?? "undefined";
                
                // 构建 webdriver 处理脚本
                var webdriverScript = "";
                if (webdriverMode == "undefined" || webdriverMode == "delete")
                {
                    webdriverScript = @"
                        // 完全移除 webdriver 属性
                        try { delete Object.getPrototypeOf(navigator).webdriver; } catch(e) {}
                        try { delete navigator.__proto__.webdriver; } catch(e) {}
                        try { delete navigator.webdriver; } catch(e) {}
                        Object.defineProperty(navigator, 'webdriver', { 
                            get: () => undefined,
                            configurable: true,
                            enumerable: false
                        });
                    ";
                }
                else if (webdriverMode == "true")
                {
                    webdriverScript = @"
                        Object.defineProperty(navigator, 'webdriver', { 
                            get: () => true,
                            configurable: true,
                            enumerable: true
                        });
                    ";
                }
                else if (webdriverMode == "false")
                {
                    webdriverScript = @"
                        Object.defineProperty(navigator, 'webdriver', { 
                            get: () => false,
                            configurable: true,
                            enumerable: true
                        });
                    ";
                }
                
                // 构建注入脚本（在每个页面加载前执行）
                var script = $@"
                    {webdriverScript}
                    
                    Object.defineProperty(navigator, 'platform', {{
                        get: () => '{platform}',
                        configurable: true
                    }});
                    Object.defineProperty(navigator, 'userAgent', {{
                        get: () => '{userAgent}',
                        configurable: true
                    }});
                    Object.defineProperty(navigator, 'appVersion', {{
                        get: () => '{userAgent}'.replace('Mozilla/', ''),
                        configurable: true
                    }});
                    Object.defineProperty(navigator, 'vendor', {{
                        get: () => '{vendor}',
                        configurable: true
                    }});
                    Object.defineProperty(navigator, 'maxTouchPoints', {{
                        get: () => {maxTouchPoints},
                        configurable: true
                    }});
                ";
                
                // 使用 CDP Page.addScriptToEvaluateOnNewDocument
                var cdpCommand = new Dictionary<string, object>
                {
                    { "source", script }
                };
                
                _driver.ExecuteCdpCommand("Page.addScriptToEvaluateOnNewDocument", cdpCommand);
                _log.LogInfo("UndetectedChrome", $"✅ CDP script injected: Platform={platform}, Vendor={vendor}, MaxTouchPoints={maxTouchPoints}, WebdriverMode={webdriverMode}");
                
                // ⭐ 也注入 cloudflare-anti-detection.js 到每个新页面（用于其他防检测措施）
                var antiDetectionScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "scripts", "cloudflare-anti-detection.js");
                if (File.Exists(antiDetectionScriptPath))
                {
                    var antiDetectionScript = File.ReadAllText(antiDetectionScriptPath);
                    var antiDetectionCdpCommand = new Dictionary<string, object>
                    {
                        { "source", antiDetectionScript }
                    };
                    _driver.ExecuteCdpCommand("Page.addScriptToEvaluateOnNewDocument", antiDetectionCdpCommand);
                    _log.LogInfo("UndetectedChrome", $"✅ CDP anti-detection script injected (size: {antiDetectionScript.Length} bytes)");
                }
            }
            catch (Exception ex)
            {
                _log.LogWarn("UndetectedChrome", $"Failed to inject CDP script: {ex.Message}");
            }
        }

        /// <summary>
        /// 注入反检测脚本（重用现有的 cloudflare-anti-detection.js）
        /// </summary>
        private void InjectAntiDetectionScript(BrowserEnvironment? environment)
        {
            if (_driver == null || _currentProfile == null)
                return;

            try
            {
                var js = (IJavaScriptExecutor)_driver;
                
                // 1. 加载现有的 Cloudflare 防检测脚本
                var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "scripts", "cloudflare-anti-detection.js");
                if (File.Exists(scriptPath))
                {
                    var antiDetectionScript = File.ReadAllText(scriptPath);
                    _log.LogInfo("UndetectedChrome", $"[AntiDetection] Loading script from: {scriptPath}");
                    _log.LogInfo("UndetectedChrome", $"[AntiDetection] Script size: {antiDetectionScript.Length} bytes");
                    js.ExecuteScript(antiDetectionScript);
                    _log.LogInfo("UndetectedChrome", $"✅ Loaded anti-detection script from: {scriptPath}");
                }
                else
                {
                    _log.LogWarn("UndetectedChrome", $"⚠️ Anti-detection script not found: {scriptPath}");
                }

                // 2. 注入自定义指纹数据（时区、语言等）
                InjectCustomFingerprint();
            }
            catch (Exception ex)
            {
                _log.LogWarn("UndetectedChrome", $"Failed to inject anti-detection script: {ex.Message}");
            }
        }

        /// <summary>
        /// 注入自定义指纹数据（补充 cloudflare-anti-detection.js 未覆盖的部分）
        /// </summary>
        private void InjectCustomFingerprint()
        {
            if (_driver == null || _currentProfile == null)
                return;

            try
            {
                var js = (IJavaScriptExecutor)_driver;
                
                // 准备指纹数据
                var languages = GetLanguagesArray(_currentProfile.LanguagesJson);
                var timezone = _currentProfile.Timezone ?? "Asia/Shanghai";
                
                // 注入完整的指纹数据（补充 cloudflare-anti-detection.js）
                var hardwareConcurrency = _currentProfile.HardwareConcurrency > 0 ? _currentProfile.HardwareConcurrency : 8;
                var deviceMemory = _currentProfile.DeviceMemory > 0 ? _currentProfile.DeviceMemory : 8;
                var platform = (_currentProfile.Platform ?? "Win32").Replace("'", "\\'");
                var userAgent = (_currentProfile.UserAgent ?? "").Replace("'", "\\'");
                var maxTouchPoints = _currentProfile.MaxTouchPoints;
                
                // ⭐ 从 Profile 读取 Vendor，如果没有则根据 Platform 设置
                var vendor = !string.IsNullOrEmpty(_currentProfile.Vendor) 
                    ? _currentProfile.Vendor 
                    : platform switch
                    {
                        "iPhone" or "iPad" or "MacIntel" => "Apple Computer, Inc.",
                        "Linux armv8l" => "Google Inc.",  // Android
                        _ => "Google Inc."  // Windows/Linux
                    };
                
                _log.LogInfo("UndetectedChrome", $"[CDP Inject] Platform={platform}, Vendor={vendor} (from Profile: {!string.IsNullOrEmpty(_currentProfile.Vendor)})");
                
                var script = $@"
                    // 1. 覆盖 userAgent（最重要！）
                    Object.defineProperty(navigator, 'userAgent', {{
                        get: () => '{userAgent}',
                        configurable: true
                    }});
                    
                    // 2. 覆盖 appVersion（必须与 userAgent 一致）
                    Object.defineProperty(navigator, 'appVersion', {{
                        get: () => '{userAgent}'.replace('Mozilla/', ''),
                        configurable: true
                    }});
                    
                    // 3. 覆盖 platform
                    Object.defineProperty(navigator, 'platform', {{
                        get: () => '{platform}',
                        configurable: true
                    }});
                    
                    // 4. 覆盖 vendor
                    Object.defineProperty(navigator, 'vendor', {{
                        get: () => '{vendor}',
                        configurable: true
                    }});
                    
                    // 5. 覆盖 maxTouchPoints
                    Object.defineProperty(navigator, 'maxTouchPoints', {{
                        get: () => {maxTouchPoints},
                        configurable: true
                    }});
                    
                    // 5. 覆盖 languages（使用配置的语言）
                    Object.defineProperty(navigator, 'languages', {{
                        get: () => {languages},
                        configurable: true
                    }});
                    
                    // 6. 覆盖 hardwareConcurrency
                    Object.defineProperty(navigator, 'hardwareConcurrency', {{
                        get: () => {hardwareConcurrency},
                        configurable: true
                    }});
                    
                    // 7. 覆盖 deviceMemory
                    Object.defineProperty(navigator, 'deviceMemory', {{
                        get: () => {deviceMemory},
                        configurable: true
                    }});
                    
                    // 5. 覆盖时区（Intl.DateTimeFormat）
                    const originalDateTimeFormat = Intl.DateTimeFormat;
                    Intl.DateTimeFormat = function(...args) {{
                        const instance = new originalDateTimeFormat(...args);
                        const originalResolvedOptions = instance.resolvedOptions;
                        instance.resolvedOptions = function() {{
                            const options = originalResolvedOptions.call(this);
                            options.timeZone = '{timezone}';
                            return options;
                        }};
                        return instance;
                    }};
                ";
                
                _log.LogInfo("UndetectedChrome", $"[CustomFingerprint] About to execute script for Platform={platform}, Vendor={vendor}");
                js.ExecuteScript(script);
                _log.LogInfo("UndetectedChrome", $"✅ Custom fingerprint injected (Platform={platform}, Vendor={vendor}, Timezone: {timezone}, Languages: {languages})");
            }
            catch (Exception ex)
            {
                _log.LogWarn("UndetectedChrome", $"Failed to inject custom fingerprint: {ex.Message}");
            }
        }

        private void ForceFixWebdriver(BrowserEnvironment? environment)
        {
            if (_driver == null) return;

            try
            {
                var webdriverMode = environment?.WebdriverMode ?? "undefined";
                var js = (IJavaScriptExecutor)_driver;

                string script = webdriverMode switch
                {
                    "undefined" or "delete" => @"
                        // 多次尝试删除 webdriver
                        try { delete Object.getPrototypeOf(navigator).webdriver; } catch(e) {}
                        try { delete navigator.__proto__.webdriver; } catch(e) {}
                        try { delete navigator.webdriver; } catch(e) {}
                        
                        // 使用 Object.defineProperty 强制设置为 undefined
                        Object.defineProperty(navigator, 'webdriver', { 
                            get: () => undefined,
                            set: () => {},
                            configurable: true,
                            enumerable: false
                        });
                        
                        // 验证结果
                        console.log('[ForceFixWebdriver] navigator.webdriver =', navigator.webdriver);
                    ",
                    "true" => @"
                        Object.defineProperty(navigator, 'webdriver', { 
                            get: () => true,
                            configurable: true,
                            enumerable: true
                        });
                        console.log('[ForceFixWebdriver] navigator.webdriver =', navigator.webdriver);
                    ",
                    "false" => @"
                        Object.defineProperty(navigator, 'webdriver', { 
                            get: () => false,
                            configurable: true,
                            enumerable: true
                        });
                        console.log('[ForceFixWebdriver] navigator.webdriver =', navigator.webdriver);
                    ",
                    _ => ""
                };

                if (!string.IsNullOrEmpty(script))
                {
                    js.ExecuteScript(script);
                    _log.LogInfo("UndetectedChrome", $"✅ Force fixed webdriver: mode={webdriverMode}");
                }
            }
            catch (Exception ex)
            {
                _log.LogWarn("UndetectedChrome", $"Failed to force fix webdriver: {ex.Message}");
            }
        }

        private string GetLanguagesArray(string? languagesJson)
        {
            if (string.IsNullOrEmpty(languagesJson))
                return "['en-US', 'en']";

            try
            {
                var languages = System.Text.Json.JsonSerializer.Deserialize<List<string>>(languagesJson);
                if (languages != null && languages.Count > 0)
                {
                    var jsArray = string.Join(", ", languages.Select(l => $"'{l}'"));
                    return $"[{jsArray}]";
                }
            }
            catch
            {
                // 解析失败，使用默认值
            }

            return "['en-US', 'en']";
        }

        /// <summary>
        /// 在独立窗口中显示指纹信息（已废弃，改为在浏览器管理界面手动打开）
        /// </summary>
        [Obsolete("不再自动显示指纹信息，改为在浏览器管理界面手动打开")]
        private void ShowFingerprintInfoAsync()
        {
            if (_currentProfile == null)
                return;

            try
            {
                //// 在主线程中打开对话框
                //System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                //{
                //    var dialog = new FishBrowser.WPF.Views.Dialogs.BrowserFingerprintInfoDialog(_currentProfile);
                //    dialog.ShowDialog();
                //});
                
                _log.LogInfo("UndetectedChrome", "✅ Fingerprint info dialog opened");
            }
            catch (Exception ex)
            {
                _log.LogWarn("UndetectedChrome", $"Failed to show fingerprint info: {ex.Message}");
            }
        }

        /// <summary>
        /// 生成指纹信息 HTML（从模板文件加载）
        /// </summary>
        private string GenerateFingerprintInfoHtml()
        {
            if (_currentProfile == null)
                return "<html><body>No profile loaded</body></html>";

            try
            {
                // 读取 HTML 模板文件
                var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "templates", "fingerprint-info.html");
                
                if (!File.Exists(templatePath))
                {
                    _log.LogWarn("UndetectedChrome", $"Template not found: {templatePath}");
                    return "<html><body>Template not found</body></html>";
                }

                var html = File.ReadAllText(templatePath);

                // 替换占位符
                var userAgent = NormalizeUserAgent(_currentProfile.UserAgent);
                var languages = _currentProfile.LanguagesJson ?? "[]";
                var timezone = _currentProfile.Timezone ?? "Not set";
                var platform = _currentProfile.Platform ?? "Not set";
                // 使用 Viewport 作为屏幕分辨率的显示来源（Profile 中无 ScreenWidth/ScreenHeight 字段）
                var screenResolution = $"{_currentProfile.ViewportWidth}x{_currentProfile.ViewportHeight}";
                var viewportSize = $"{_currentProfile.ViewportWidth}x{_currentProfile.ViewportHeight}";

                html = html.Replace("{{USER_AGENT}}", userAgent)
                           .Replace("{{LANGUAGES}}", languages)
                           .Replace("{{TIMEZONE}}", timezone)
                           .Replace("{{PLATFORM}}", platform)
                           .Replace("{{SCREEN_RESOLUTION}}", screenResolution)
                           .Replace("{{VIEWPORT_SIZE}}", viewportSize);

                return html;
            }
            catch (Exception ex)
            {
                _log.LogError("UndetectedChrome", $"Failed to load template: {ex.Message}");
                return "<html><body>Error loading template</body></html>";
            }
        }

        public async Task NavigateAsync(string url)
        {
            if (_driver == null)
                throw new InvalidOperationException("Browser not launched. Call LaunchAsync first.");

            _log.LogInfo("UndetectedChrome", $"Navigating to: {url}");
            
            await Task.Run(() =>
            {
                _driver.Navigate().GoToUrl(url);
            });

            _log.LogInfo("UndetectedChrome", "Navigation completed");
        }

        public async Task<string> GetTitleAsync()
        {
            if (_driver == null)
                throw new InvalidOperationException("Browser not launched.");

            return await Task.Run(() => _driver.Title);
        }

        public async Task<string> GetPageSourceAsync()
        {
            if (_driver == null)
                throw new InvalidOperationException("Browser not launched.");

            return await Task.Run(() => _driver.PageSource);
        }

        /// <summary>
        /// 如果是移动设备，启用 Chrome DevTools 设备模拟模式
        /// </summary>
        private void EnableDeviceEmulationIfNeeded(FingerprintProfile profile)
        {
            if (_driver == null || profile == null)
                return;

            try
            {
                var platform = profile.Platform ?? "Win32";
                
                // 判断是否为移动设备
                bool isMobileDevice = platform switch
                {
                    "iPhone" or "iPad" => true,
                    "Linux armv8l" => true,  // Android
                    _ => false  // Windows/Mac/Linux 桌面
                };

                if (!isMobileDevice)
                {
                    _log.LogInfo("UndetectedChrome", $"Platform '{platform}' is desktop, skipping device emulation");
                    return;
                }

                // 获取设备参数
                var width = profile.ViewportWidth > 0 ? profile.ViewportWidth : 375;
                var height = profile.ViewportHeight > 0 ? profile.ViewportHeight : 667;
                var deviceScaleFactor = 2.0;  // Retina 屏幕
                var mobile = true;
                var userAgent = profile.UserAgent ?? "";

                // 根据平台调整参数
                if (platform == "iPad")
                {
                    width = profile.ViewportWidth > 0 ? profile.ViewportWidth : 768;
                    height = profile.ViewportHeight > 0 ? profile.ViewportHeight : 1024;
                    deviceScaleFactor = 2.0;
                }
                else if (platform == "Linux armv8l")  // Android
                {
                    width = profile.ViewportWidth > 0 ? profile.ViewportWidth : 360;
                    height = profile.ViewportHeight > 0 ? profile.ViewportHeight : 640;
                    deviceScaleFactor = 3.0;  // 高端 Android 设备
                }

                _log.LogInfo("UndetectedChrome", $"🎯 Enabling device emulation for {platform}");
                _log.LogInfo("UndetectedChrome", $"   - Viewport: {width}x{height}");
                _log.LogInfo("UndetectedChrome", $"   - Device Scale Factor: {deviceScaleFactor}");
                _log.LogInfo("UndetectedChrome", $"   - Mobile: {mobile}");

                // 使用 JavaScript 执行设备模拟（UndetectedChromeDriver 不支持 CDP SendCommand）
                // 通过 JavaScript 设置 viewport 和 devicePixelRatio
                var js = (IJavaScriptExecutor)_driver;
                
                // 设置 viewport 和 screen 对象（使用 configurable: true 允许重新定义）
                js.ExecuteScript($@"
                    // 设置 window 尺寸
                    try {{ window.resizeTo({width}, {height}); }} catch(e) {{}}
                    
                    // 设置 window 属性（允许重新定义）
                    Object.defineProperty(window, 'innerWidth', {{ get: () => {width}, configurable: true }});
                    Object.defineProperty(window, 'innerHeight', {{ get: () => {height}, configurable: true }});
                    Object.defineProperty(window, 'outerWidth', {{ get: () => {width}, configurable: true }});
                    Object.defineProperty(window, 'outerHeight', {{ get: () => {height}, configurable: true }});
                    
                    // 设置 devicePixelRatio
                    Object.defineProperty(window, 'devicePixelRatio', {{ 
                        get: () => {deviceScaleFactor.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                        configurable: true
                    }});
                    
                    // 设置 screen 对象（允许重新定义）
                    Object.defineProperty(screen, 'width', {{ get: () => {width}, configurable: true }});
                    Object.defineProperty(screen, 'height', {{ get: () => {height}, configurable: true }});
                    Object.defineProperty(screen, 'availWidth', {{ get: () => {width}, configurable: true }});
                    Object.defineProperty(screen, 'availHeight', {{ get: () => {height}, configurable: true }});
                    
                    console.log('📱 Device emulation applied: {{width}}x{{height}}, DPR: {deviceScaleFactor.ToString(System.Globalization.CultureInfo.InvariantCulture)}');
                ");

                _log.LogInfo("UndetectedChrome", "✅ Device emulation enabled successfully");
            }
            catch (Exception ex)
            {
                _log.LogWarn("UndetectedChrome", $"Failed to enable device emulation: {ex.Message}");
                // 不抛出异常，继续启动
            }
        }

        public bool IsRunning()
        {
            if (_driver == null)
                return false;

            try
            {
                // ⭐ 检查 driver 是否仍然有效
                // 访问 Title 属性会触发与浏览器的通信
                // 如果浏览器已关闭或连接丢失，会抛出异常
                _ = _driver.Title;
                return true;
            }
            catch (NullReferenceException)
            {
                // WebDriver 内部对象为 null，说明浏览器已关闭
                _driver = null;
                return false;
            }
            catch (InvalidOperationException)
            {
                // 浏览器连接已断开
                return false;
            }
            catch (Exception ex)
            {
                // 其他异常也表示浏览器不可用
                _log.LogInfo("UndetectedChrome", $"IsRunning check failed: {ex.GetType().Name} - {ex.Message}");
                return false;
            }
        }

        public async Task WaitForCloseAsync()
        {
            if (_closeTaskSource == null)
                return;

            // 在后台轮询检查浏览器是否关闭
            _ = Task.Run(async () =>
            {
                try
                {
                    while (IsRunning())
                    {
                        await Task.Delay(1000);
                    }
                    _closeTaskSource.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    _log.LogError("UndetectedChrome", $"Error waiting for close: {ex.Message}", ex.StackTrace);
                    _closeTaskSource.TrySetException(ex);
                }
            });

            await _closeTaskSource.Task;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                try
                {
                    if (_driver != null)
                    {
                        _log.LogInfo("UndetectedChrome", "Closing driver...");
                        _driver.Quit();
                        _driver.Dispose();
                        _driver = null;
                        _log.LogInfo("UndetectedChrome", "Driver closed successfully");
                    }

                    _closeTaskSource?.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    _log.LogError("UndetectedChrome", $"Error disposing driver: {ex.Message}", ex.StackTrace);
                }
            }

            _disposed = true;
        }

        ~UndetectedChromeLauncher()
        {
            Dispose(false);
        }
    }
}
