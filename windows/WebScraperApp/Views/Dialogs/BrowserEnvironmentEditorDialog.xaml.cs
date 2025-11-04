using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;

namespace FishBrowser.WPF.Views.Dialogs
{
    public partial class BrowserEnvironmentEditorDialog : Window
    {
        private readonly IHost _host;
        private readonly WebScraperDbContext _db;
        private readonly ILogService _log;
        private readonly BrowserEnvironmentService _envSvc;
        private readonly FingerprintGeneratorService _fpGenSvc;
        private readonly FingerprintValidationService _fpValSvc;
        private readonly AntiDetectionService _antiDetectSvc;
        private readonly ChromeVersionDatabase _chromeVersionDb;
        private readonly GpuCatalogService _gpuCatalog;
        private readonly FontService _fontSvc;
        
        private List<string> _selectedFonts = new List<string>();
        
        // 存储生成的连接信息（用于保存）
        private string? _generatedConnectionType;
        private int? _generatedConnectionRtt;
        private double? _generatedConnectionDownlink;
        
        private BrowserEnvironment? _existingEnvironment;
        private bool _isEditMode;
        private System.Windows.Threading.DispatcherTimer? _validationTimer;

        public BrowserEnvironment? Result { get; private set; }

        // 新建模式
        public BrowserEnvironmentEditorDialog()
        {
            InitializeComponent();
            
            _host = System.Windows.Application.Current.Resources["Host"] as IHost ?? throw new InvalidOperationException("Host not found");
            _db = _host.Services.GetRequiredService<WebScraperDbContext>();
            _log = _host.Services.GetRequiredService<ILogService>();
            _envSvc = _host.Services.GetRequiredService<BrowserEnvironmentService>();
            _fpGenSvc = _host.Services.GetRequiredService<FingerprintGeneratorService>();
            _fpValSvc = _host.Services.GetRequiredService<FingerprintValidationService>();
            _antiDetectSvc = _host.Services.GetRequiredService<AntiDetectionService>();
            _chromeVersionDb = _host.Services.GetRequiredService<ChromeVersionDatabase>();
            _gpuCatalog = _host.Services.GetRequiredService<GpuCatalogService>();
            _fontSvc = _host.Services.GetRequiredService<FontService>();

            _isEditMode = false;
            
            Loaded += OnLoaded;
        }

        // 编辑模式
        public BrowserEnvironmentEditorDialog(BrowserEnvironment environment) : this()
        {
            _existingEnvironment = environment;
            _isEditMode = true;
            Title = $"编辑浏览器 - {environment.Name}";
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadGroups();
            
            if (_isEditMode && _existingEnvironment != null)
            {
                LoadExistingEnvironment();
            }
            else
            {
                // 新建模式：生成默认随机指纹
                GenerateFullyRandomFingerprint();
            }

            // 启动实时校验定时器
            StartValidationTimer();
        }

        #region 数据加载

        private void LoadGroups()
        {
            var groups = _envSvc.GetAllGroups();
            GroupComboBox.Items.Clear();
            GroupComboBox.Items.Add(new BrowserGroup { Id = 0, Name = "（无分组）" });
            foreach (var group in groups)
            {
                GroupComboBox.Items.Add(group);
            }
            GroupComboBox.SelectedIndex = 0;
        }

        private void LoadExistingEnvironment()
        {
            if (_existingEnvironment == null) return;

            NameTextBox.Text = _existingEnvironment.Name;
            NotesTextBox.Text = _existingEnvironment.Notes ?? "";
            
            // 选择分组
            if (_existingEnvironment.GroupId.HasValue)
            {
                var group = GroupComboBox.Items.Cast<BrowserGroup>()
                    .FirstOrDefault(g => g.Id == _existingEnvironment.GroupId.Value);
                if (group != null)
                    GroupComboBox.SelectedItem = group;
            }

            // 引擎和操作系统
            SelectComboBoxItemByTag(EngineComboBox, _existingEnvironment.Engine ?? "UndetectedChrome");
            SelectComboBoxItemByContent(OSComboBox, _existingEnvironment.OS ?? "Windows");
            
            // 分辨率
            var resolution = $"{_existingEnvironment.ViewportWidth}x{_existingEnvironment.ViewportHeight}";
            SelectComboBoxItemByTag(ResolutionComboBox, resolution);
            
            // 语言
            SelectComboBoxItemByTag(LocaleComboBox, _existingEnvironment.Locale ?? "zh-CN");
            
            // 持久化
            PersistenceCheckBox.IsChecked = _existingEnvironment.EnablePersistence;

            // 指纹特征
            UserAgentTextBox.Text = _existingEnvironment.UserAgent ?? "";
            PlatformTextBox.Text = _existingEnvironment.Platform ?? "";
            TimezoneTextBox.Text = _existingEnvironment.Timezone ?? "";
            HardwareConcurrencyTextBox.Text = _existingEnvironment.HardwareConcurrency?.ToString() ?? "";
            DeviceMemoryTextBox.Text = _existingEnvironment.DeviceMemory?.ToString() ?? "";
            MaxTouchPointsTextBox.Text = _existingEnvironment.MaxTouchPoints?.ToString() ?? "";
            WebGLVendorTextBox.Text = _existingEnvironment.WebGLVendor ?? "";
            WebGLRendererTextBox.Text = _existingEnvironment.WebGLRenderer ?? "";
            LanguagesJsonTextBox.Text = _existingEnvironment.LanguagesJson ?? "";
            PluginsJsonTextBox.Text = _existingEnvironment.PluginsJson ?? "";
            SecChUaTextBox.Text = _existingEnvironment.SecChUa ?? "";
            SecChUaPlatformTextBox.Text = _existingEnvironment.SecChUaPlatform ?? "";
            SelectComboBoxItemByTag(WebdriverModeComboBox, _existingEnvironment.WebdriverMode ?? "undefined");
        }

        private void SelectComboBoxItemByTag(ComboBox comboBox, string tag)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Tag?.ToString() == tag)
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
        }

        private void SelectComboBoxItemByContent(ComboBox comboBox, string content)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Content?.ToString() == content)
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
        }

        #endregion

        #region 指纹生成

        private void FullyRandomize_Click(object sender, RoutedEventArgs e)
        {
            GenerateFullyRandomFingerprint();
            StatusText.Text = "✅ 已生成完全随机指纹";
        }

        private async void Regenerate_Click(object sender, RoutedEventArgs e)
        {
            await GenerateFingerprintBasedOnConfig();
            StatusText.Text = "✅ 已基于当前配置重新生成指纹";
        }

        private async void GenerateFullyRandomFingerprint()
        {
            try
            {
                var random = new Random();
                
                // 检查是否固定浏览器配置
                if (LockBrowserConfigCheckBox?.IsChecked != true)
                {
                    // 随机选择操作系统（包括移动端）
                    var osList = new[] { "Windows", "MacOS", "Linux", "Android", "iOS" };
                    var selectedOS = osList[random.Next(osList.Length)];
                    SelectComboBoxItemByContent(OSComboBox, selectedOS);

                    // 根据操作系统随机选择合适的分辨率
                    string[] resolutions;
                    if (selectedOS == "Android")
                    {
                        resolutions = new[] { "360x800", "412x915", "1080x2400" };
                    }
                    else if (selectedOS == "iOS")
                    {
                        resolutions = new[] { "390x844", "393x852", "428x926", "820x1180", "1024x1366" };
                    }
                    else
                    {
                        resolutions = new[] { "1920x1080", "1366x768", "1280x720", "2560x1440", "3840x2160" };
                    }
                    var selectedRes = resolutions[random.Next(resolutions.Length)];
                    SelectComboBoxItemByTag(ResolutionComboBox, selectedRes);

                    // 随机选择语言
                    var locales = new[] { "zh-CN", "en-US", "ja-JP", "ko-KR" };
                    var selectedLocale = locales[random.Next(locales.Length)];
                    SelectComboBoxItemByTag(LocaleComboBox, selectedLocale);
                }
                
                // 获取当前操作系统（可能是固定的，也可能是刚随机的）
                var os = (OSComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Windows";

                // 生成基础指纹
                await GenerateFingerprintBasedOnConfig();
                
                // 调用独立的随机功能（使用更大的数据库）
                // 检查固定状态，只随机未固定的项
                if (LockUACheckBox?.IsChecked != true)
                {
                    RandomizeUA_Click(this, new RoutedEventArgs());
                }
                
                if (LockWebGLCheckBox?.IsChecked != true)
                {
                    RandomizeWebGL_Click(this, new RoutedEventArgs());
                }
                
                if (LockFontsCheckBox?.IsChecked != true)
                {
                    // 随机字体（异步）
                    var osKey = os switch
                    {
                        "Windows" => "windows",
                        "MacOS" => "macos",
                        "Linux" => "linux",
                        "Android" => "android",
                        "iOS" => "ios",
                        _ => "windows"
                    };
                    var fontCount = random.Next(30, 51);
                    _selectedFonts = await _fontSvc.RandomSubsetAsync(osKey, fontCount);
                    FontsListTextBox.Text = string.Join(", ", _selectedFonts);
                    FontsCountText.Text = $"{_selectedFonts.Count} 个字体";
                }
                
                var lockedItems = new List<string>();
                if (LockBrowserConfigCheckBox?.IsChecked == true) lockedItems.Add("浏览器配置");
                if (LockUACheckBox?.IsChecked == true) lockedItems.Add("UA");
                if (LockWebGLCheckBox?.IsChecked == true) lockedItems.Add("WebGL");
                if (LockFontsCheckBox?.IsChecked == true) lockedItems.Add("Fonts");
                
                var lockedInfo = lockedItems.Count > 0 ? $" (固定: {string.Join(", ", lockedItems)})" : "";
                var chromeVersion = ExtractChromeVersion(UserAgentTextBox.Text);
                _log.LogInfo("BrowserEnvironmentEditor", $"Fully randomized fingerprint: OS={os}, UA=Chrome/{chromeVersion}, WebGL={WebGLVendorTextBox.Text}, Fonts={_selectedFonts.Count}{lockedInfo}");
            }
            catch (Exception ex)
            {
                _log.LogError("BrowserEnvironmentEditor", $"Failed to generate random fingerprint: {ex.Message}");
                MessageBox.Show($"生成随机指纹失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task GenerateFingerprintBasedOnConfig()
        {
            try
            {
                var os = (OSComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Windows";
                var locale = (LocaleComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "zh-CN";
                var resolutionTag = (ResolutionComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "1920x1080";
                
                // 解析分辨率
                var parts = resolutionTag.Split('x');
                var width = int.Parse(parts[0]);
                var height = int.Parse(parts[1]);

                // 生成 User-Agent
                var chromeVersion = 141;
                var userAgent = GenerateUserAgent(os, chromeVersion);
                UserAgentTextBox.Text = userAgent;

                // 生成 Platform
                PlatformTextBox.Text = os switch
                {
                    "Windows" => "Win32",
                    "MacOS" => "MacIntel",
                    "Linux" => "Linux x86_64",
                    "Android" => "Linux armv8l",
                    "iOS" => "iPhone",
                    _ => "Win32"
                };

                // 生成 Timezone
                TimezoneTextBox.Text = locale switch
                {
                    "zh-CN" => "Asia/Shanghai",
                    "en-US" => "America/New_York",
                    "ja-JP" => "Asia/Tokyo",
                    "ko-KR" => "Asia/Seoul",
                    _ => "Asia/Shanghai"
                };

                // 生成硬件配置（根据 OS 使用真实配置）
                var random = new Random();
                
                if (os == "iOS")
                {
                    // ⭐ iOS 真实硬件配置
                    var iosCores = new[] { 4, 6 };  // iPhone: A15/A16 是 6 核，A14 是 4 核
                    var iosMemory = new[] { 4, 6, 8 };  // iPhone: 4GB (标准), 6GB (Pro), 8GB (Pro Max)
                    HardwareConcurrencyTextBox.Text = iosCores[random.Next(iosCores.Length)].ToString();
                    DeviceMemoryTextBox.Text = iosMemory[random.Next(iosMemory.Length)].ToString();
                    MaxTouchPointsTextBox.Text = "5";  // iPhone 固定 5 个触摸点
                }
                else if (os == "Android")
                {
                    // Android 配置
                    var androidCores = new[] { 4, 6, 8 };
                    var androidMemory = new[] { 4, 6, 8, 12 };
                    HardwareConcurrencyTextBox.Text = androidCores[random.Next(androidCores.Length)].ToString();
                    DeviceMemoryTextBox.Text = androidMemory[random.Next(androidMemory.Length)].ToString();
                    MaxTouchPointsTextBox.Text = random.Next(5, 11).ToString();
                }
                else
                {
                    // 桌面操作系统（Windows/Mac/Linux）
                    var commonCores = new[] { 4, 6, 8, 12, 16 };
                    var commonMemory = new[] { 8, 16, 32 };
                    HardwareConcurrencyTextBox.Text = commonCores[random.Next(commonCores.Length)].ToString();
                    DeviceMemoryTextBox.Text = commonMemory[random.Next(commonMemory.Length)].ToString();
                    MaxTouchPointsTextBox.Text = "0";  // 桌面设备无触摸点
                }

                // WebGL 配置由独立的随机方法处理，这里不再硬编码
                // 如果 WebGL 字段为空，则使用数据库随机生成
                if (string.IsNullOrWhiteSpace(WebGLVendorTextBox.Text) || string.IsNullOrWhiteSpace(WebGLRendererTextBox.Text))
                {
                    var osKey = os switch
                    {
                        "Windows" => "Windows",
                        "MacOS" => "macOS",
                        "Linux" => "Linux",
                        "Android" => "Android",
                        "iOS" => "iOS",
                        _ => "Windows"
                    };
                    var gpus = await _gpuCatalog.RandomSubsetAsync(osKey, 1);
                    if (gpus.Count > 0)
                    {
                        WebGLVendorTextBox.Text = gpus[0].Vendor;
                        WebGLRendererTextBox.Text = gpus[0].Renderer;
                    }
                }

                // 生成防检测数据
                var tempProfile = new FingerprintProfile
                {
                    UserAgent = userAgent,
                    Platform = PlatformTextBox.Text,
                    Locale = locale,
                    HardwareConcurrency = int.Parse(HardwareConcurrencyTextBox.Text),
                    DeviceMemory = int.Parse(DeviceMemoryTextBox.Text),
                    MaxTouchPoints = int.Parse(MaxTouchPointsTextBox.Text)
                };
                
                _antiDetectSvc.GenerateAntiDetectionData(tempProfile);

                LanguagesJsonTextBox.Text = tempProfile.LanguagesJson;
                PluginsJsonTextBox.Text = tempProfile.PluginsJson;
                SecChUaTextBox.Text = tempProfile.SecChUa;
                SecChUaPlatformTextBox.Text = tempProfile.SecChUaPlatform;
                
                // ⭐ 保存连接信息（用于后续保存）
                _generatedConnectionType = tempProfile.ConnectionType;
                _generatedConnectionRtt = tempProfile.ConnectionRtt;
                _generatedConnectionDownlink = tempProfile.ConnectionDownlink;

                // 触发校验
                ValidateFingerprint();
            }
            catch (Exception ex)
            {
                _log.LogError("BrowserEnvironmentEditor", $"Failed to generate fingerprint: {ex.Message}");
                MessageBox.Show($"生成指纹失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateUserAgent(string os, int chromeVersion)
        {
            return os switch
            {
                "Windows" => $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion}.0.0.0 Safari/537.36",
                "MacOS" => $"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion}.0.0.0 Safari/537.36",
                "Linux" => $"Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion}.0.0.0 Safari/537.36",
                "Android" => $"Mozilla/5.0 (Linux; Android 13; SM-S901B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion}.0.0.0 Mobile Safari/537.36",
                "iOS" => $"Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1",
                _ => $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion}.0.0.0 Safari/537.36"
            };
        }

        /// <summary>
        /// 随机 User-Agent（使用真实的 Chrome 版本号）
        /// </summary>
        private void RandomizeUA_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var os = (OSComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Windows";
                
                // ⭐ 使用 ChromeVersionDatabase 获取真实版本号
                var chromeVersion = _chromeVersionDb.GetRandomVersion(os);
                if (chromeVersion == null)
                {
                    _log.LogWarn("BrowserEnvironmentEditor", $"No Chrome version found for OS: {os}, using Windows");
                    chromeVersion = _chromeVersionDb.GetRandomVersion("Windows");
                }
                
                if (chromeVersion == null)
                {
                    throw new InvalidOperationException("No Chrome versions available");
                }
                
                // 生成 User-Agent
                var userAgent = os switch
                {
                    "Windows" => $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion.Version} Safari/537.36",
                    "Mac" or "MacOS" => $"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion.Version} Safari/537.36",
                    "Linux" => $"Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion.Version} Safari/537.36",
                    "Android" => $"Mozilla/5.0 (Linux; Android 13; SM-S901B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion.Version} Mobile Safari/537.36",
                    "iOS" => "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1",
                    _ => $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion.Version} Safari/537.36"
                };
                
                UserAgentTextBox.Text = userAgent;
                
                _log.LogInfo("BrowserEnvironmentEditor", $"Randomized UA for OS '{os}' with Chrome {chromeVersion.Version}");
                StatusText.Text = $"✅ 已随机生成 {os} User-Agent (Chrome {chromeVersion.Version})";
            }
            catch (Exception ex)
            {
                _log.LogError("BrowserEnvironmentEditor", $"Failed to randomize UA: {ex.Message}");
                MessageBox.Show($"随机 User-Agent 失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 随机 WebGL 配置（独立功能）
        /// </summary>
        private async void RandomizeWebGL_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var os = (OSComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Windows";
                var osKey = os switch
                {
                    "Windows" => "Windows",
                    "MacOS" => "macOS",
                    "Linux" => "Linux",
                    "Android" => "Android",
                    "iOS" => "iOS",
                    _ => "Windows"
                };
                
                var gpus = await _gpuCatalog.RandomSubsetAsync(osKey, 1);
                if (gpus.Count > 0)
                {
                    WebGLVendorTextBox.Text = gpus[0].Vendor;
                    WebGLRendererTextBox.Text = gpus[0].Renderer;
                    
                    _log.LogInfo("BrowserEnvironmentEditor", $"Randomized WebGL for OS '{os}': {gpus[0].Vendor} / {gpus[0].Renderer}");
                    StatusText.Text = $"✅ 已随机生成 {os} WebGL 配置";
                }
                else
                {
                    _log.LogWarn("BrowserEnvironmentEditor", $"No GPU found for OS '{os}'");
                    StatusText.Text = $"⚠️ 未找到 {os} 的 GPU 配置";
                }
            }
            catch (Exception ex)
            {
                _log.LogError("BrowserEnvironmentEditor", $"Failed to randomize WebGL: {ex.Message}");
                MessageBox.Show($"随机 WebGL 失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 随机字体列表（独立功能）
        /// </summary>
        private async void RandomizeFonts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var os = (OSComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Windows";
                var osKey = os switch
                {
                    "Windows" => "windows",
                    "MacOS" => "macos",
                    "Linux" => "linux",
                    _ => "windows"
                };

                // 随机选择 30-50 个字体
                var random = new Random();
                var count = random.Next(30, 51);
                _selectedFonts = await _fontSvc.RandomSubsetAsync(osKey, count);
                
                FontsListTextBox.Text = string.Join(", ", _selectedFonts);
                FontsCountText.Text = $"{_selectedFonts.Count} 个字体";
                
                _log.LogInfo("BrowserEnvironmentEditor", $"Randomized {_selectedFonts.Count} fonts for OS '{os}'");
                StatusText.Text = $"✅ 已随机生成 {_selectedFonts.Count} 个字体";
            }
            catch (Exception ex)
            {
                _log.LogError("BrowserEnvironmentEditor", $"Failed to randomize fonts: {ex.Message}");
                MessageBox.Show($"随机字体失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 实时校验

        private void StartValidationTimer()
        {
            _validationTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _validationTimer.Tick += (s, e) => ValidateFingerprint();
            _validationTimer.Start();
        }

        private void FingerprintField_Changed(object sender, TextChangedEventArgs e)
        {
            // 字段变化时触发校验（通过定时器延迟执行）
        }

        private void ValidateFingerprint()
        {
            try
            {
                // 创建临时 Profile 用于校验
                var tempProfile = CreateTempProfile();
                
                // 一致性检查
                var consistencyScore = CheckConsistency(tempProfile);
                ConsistencyScoreBar.Value = consistencyScore;
                ConsistencyScoreText.Text = consistencyScore.ToString();

                // 真实性检查
                var realismScore = CheckRealism(tempProfile);
                RealismScoreBar.Value = realismScore;
                RealismScoreText.Text = realismScore.ToString();

                // Cloudflare 风险检查
                var cloudflareRisk = CheckCloudflareRisk(tempProfile);
                CloudflareRiskBar.Value = cloudflareRisk;
                CloudflareRiskText.Text = cloudflareRisk.ToString();

                // 计算总分
                var totalScore = (consistencyScore + realismScore + (100 - cloudflareRisk)) / 3;
                TotalScoreBar.Value = totalScore;
                TotalScoreText.Text = ((int)totalScore).ToString();

                // 更新进度条颜色
                UpdateScoreBarColors(totalScore);

                // 风险等级
                var riskLevel = GetRiskLevel(totalScore);
                RiskLevelText.Text = $"风险等级: {GetRiskLevelText(riskLevel)}";

                // 生成建议
                var recommendations = GenerateRecommendations(tempProfile, consistencyScore, realismScore, cloudflareRisk);
                RecommendationsList.ItemsSource = recommendations;

                // 更新预览
                UpdateFingerprintPreview(tempProfile);
            }
            catch (Exception ex)
            {
                _log.LogError("BrowserEnvironmentEditor", $"Validation failed: {ex.Message}");
            }
        }

        private FingerprintProfile CreateTempProfile()
        {
            return new FingerprintProfile
            {
                Name = "Temp",
                UserAgent = UserAgentTextBox.Text,
                Platform = PlatformTextBox.Text,
                Timezone = TimezoneTextBox.Text,
                Locale = (LocaleComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "zh-CN",
                HardwareConcurrency = int.TryParse(HardwareConcurrencyTextBox.Text, out var hc) ? hc : 8,
                DeviceMemory = int.TryParse(DeviceMemoryTextBox.Text, out var dm) ? dm : 8,
                MaxTouchPoints = int.TryParse(MaxTouchPointsTextBox.Text, out var mtp) ? mtp : 0,
                WebGLVendor = WebGLVendorTextBox.Text,
                WebGLRenderer = WebGLRendererTextBox.Text,
                LanguagesJson = LanguagesJsonTextBox.Text,
                PluginsJson = PluginsJsonTextBox.Text,
                SecChUa = SecChUaTextBox.Text,
                SecChUaPlatform = SecChUaPlatformTextBox.Text,
                ViewportWidth = 1920,
                ViewportHeight = 1080
            };
        }

        private int CheckConsistency(FingerprintProfile profile)
        {
            var checks = new List<bool>();

            // UA与Platform一致性（智能检查）
            bool uaPlatformMatch = false;
            if (profile.Platform == "Win32" && profile.UserAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase))
                uaPlatformMatch = true;
            else if (profile.Platform == "MacIntel" && profile.UserAgent.Contains("Macintosh", StringComparison.OrdinalIgnoreCase))
                uaPlatformMatch = true;
            else if (profile.Platform.Contains("Linux", StringComparison.OrdinalIgnoreCase) && profile.UserAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
                uaPlatformMatch = true;
            else if (profile.UserAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
                uaPlatformMatch = true; // Android 设备
            else if (profile.UserAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) || profile.UserAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
                uaPlatformMatch = true; // iOS 设备
            checks.Add(uaPlatformMatch);

            // Platform与SecChUA一致性
            checks.Add(string.IsNullOrEmpty(profile.SecChUaPlatform) || 
                      profile.SecChUaPlatform.Contains(profile.Platform, StringComparison.OrdinalIgnoreCase));

            // Locale与Languages一致性
            checks.Add(string.IsNullOrEmpty(profile.LanguagesJson) || 
                      profile.LanguagesJson.Contains(profile.Locale, StringComparison.OrdinalIgnoreCase));

            return (checks.Count(c => c) * 100) / checks.Count;
        }

        private int CheckRealism(FingerprintProfile profile)
        {
            var scores = new List<int>();

            // Chrome版本检查（120-141 都是有效的现代版本）
            var chromeVersion = ExtractChromeVersion(profile.UserAgent);
            if (chromeVersion.HasValue)
            {
                if (chromeVersion >= 120 && chromeVersion <= 141)
                    scores.Add(100); // 最新版本
                else if (chromeVersion >= 100 && chromeVersion < 120)
                    scores.Add(85); // 较新版本
                else if (chromeVersion >= 80 && chromeVersion < 100)
                    scores.Add(70); // 旧版本
                else
                    scores.Add(50); // 过时版本
            }
            else
            {
                scores.Add(60); // 无法识别版本
            }

            // 硬件配置检查
            scores.Add((profile.HardwareConcurrency >= 8 && profile.HardwareConcurrency <= 16) ? 100 : 70);

            // GPU检查
            scores.Add(!string.IsNullOrEmpty(profile.WebGLVendor) ? 100 : 50);

            // 防检测数据检查
            scores.Add(!string.IsNullOrEmpty(profile.PluginsJson) && 
                      !string.IsNullOrEmpty(profile.LanguagesJson) &&
                      !string.IsNullOrEmpty(profile.SecChUa) ? 100 : 60);

            return (int)scores.Average();
        }

        private int CheckCloudflareRisk(FingerprintProfile profile)
        {
            var risk = 0;

            // HeadlessChrome标志
            if (profile.UserAgent.Contains("HeadlessChrome", StringComparison.OrdinalIgnoreCase))
                risk += 30;

            // 防检测数据缺失
            if (string.IsNullOrEmpty(profile.PluginsJson)) risk += 20;
            if (string.IsNullOrEmpty(profile.LanguagesJson)) risk += 20;
            if (string.IsNullOrEmpty(profile.SecChUa)) risk += 20;

            // 触摸点数异常
            if (profile.MaxTouchPoints > 0) risk += 10;

            return Math.Min(risk, 100);
        }

        private string GetRiskLevel(double totalScore)
        {
            return totalScore switch
            {
                >= 90 => "safe",
                >= 70 => "low",
                >= 50 => "medium",
                _ => "high"
            };
        }

        private string GetRiskLevelText(string level)
        {
            return level switch
            {
                "safe" => "✅ 安全",
                "low" => "⚠️ 低风险",
                "medium" => "⚠️ 中风险",
                "high" => "❌ 高风险",
                _ => "未知"
            };
        }

        private List<string> GenerateRecommendations(FingerprintProfile profile, int consistency, int realism, int risk)
        {
            var recommendations = new List<string>();

            if (consistency < 90)
                recommendations.Add("一致性较低：检查 UA、Platform、Locale 是否匹配");

            if (realism < 80)
                recommendations.Add("真实性不足：建议使用 Chrome 141+ 版本");

            if (string.IsNullOrEmpty(profile.WebGLVendor))
                recommendations.Add("缺少 WebGL 配置：建议添加 GPU 信息");

            if (string.IsNullOrEmpty(profile.PluginsJson))
                recommendations.Add("缺少 Plugins 数据：点击「完全随机」生成");

            if (risk > 30)
                recommendations.Add("Cloudflare 风险较高：检查防检测数据完整性");

            if (profile.MaxTouchPoints > 0)
                recommendations.Add("桌面设备触摸点应为 0");

            if (recommendations.Count == 0)
                recommendations.Add("✅ 指纹配置良好，无需优化");

            return recommendations;
        }

        private void UpdateScoreBarColors(double totalScore)
        {
            var color = totalScore switch
            {
                >= 90 => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Green
                >= 70 => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Orange
                >= 50 => new SolidColorBrush(Color.FromRgb(255, 193, 7)), // Amber
                _ => new SolidColorBrush(Color.FromRgb(244, 67, 54)) // Red
            };

            TotalScoreBar.Foreground = color;
            ConsistencyScoreBar.Foreground = color;
            RealismScoreBar.Foreground = color;
        }

        private void UpdateFingerprintPreview(FingerprintProfile profile)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"User-Agent: {profile.UserAgent}");
            sb.AppendLine($"Platform: {profile.Platform}");
            sb.AppendLine($"Timezone: {profile.Timezone}");
            sb.AppendLine($"Locale: {profile.Locale}");
            sb.AppendLine($"Hardware: {profile.HardwareConcurrency} cores, {profile.DeviceMemory} GB");
            sb.AppendLine($"WebGL: {profile.WebGLVendor}");
            sb.AppendLine($"  Renderer: {profile.WebGLRenderer}");
            sb.AppendLine($"Touch Points: {profile.MaxTouchPoints}");
            
            if (!string.IsNullOrEmpty(profile.SecChUa))
                sb.AppendLine($"Sec-CH-UA: {profile.SecChUa}");

            FingerprintPreviewText.Text = sb.ToString();
        }

        #endregion

        #region 事件处理

        private void EngineComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 引擎变化时可能需要调整某些配置
        }

        private async void OSComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 操作系统变化时重新生成指纹
            if (IsLoaded)
            {
                await GenerateFingerprintBasedOnConfig();
            }
        }

        private void ResolutionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 分辨率变化
        }

        private void WebdriverMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Webdriver 模式变化
        }

        private async void LocaleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 语言变化时重新生成指纹
            if (IsLoaded)
            {
                await GenerateFingerprintBasedOnConfig();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证必填字段
                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                {
                    MessageBox.Show("请输入浏览器名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 创建或更新环境
                BrowserEnvironment env;
                if (_isEditMode && _existingEnvironment != null)
                {
                    env = _existingEnvironment;
                }
                else
                {
                    env = new BrowserEnvironment();
                }

                // 基本信息
                env.Name = NameTextBox.Text;
                env.Notes = NotesTextBox.Text;
                env.Engine = (EngineComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "UndetectedChrome";
                env.OS = (OSComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Windows";
                env.Locale = (LocaleComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "zh-CN";
                env.EnablePersistence = PersistenceCheckBox.IsChecked == true;

                // 分组
                var selectedGroup = GroupComboBox.SelectedItem as BrowserGroup;
                env.GroupId = selectedGroup?.Id > 0 ? selectedGroup.Id : null;

                // 分辨率
                var resolutionTag = (ResolutionComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "1920x1080";
                var parts = resolutionTag.Split('x');
                env.ViewportWidth = int.Parse(parts[0]);
                env.ViewportHeight = int.Parse(parts[1]);

                // 指纹特征（直接存储在 BrowserEnvironment 中）
                env.UserAgent = UserAgentTextBox.Text;
                env.Platform = PlatformTextBox.Text;
                env.Timezone = TimezoneTextBox.Text;
                
                // ⭐ 调试日志：检查保存的值
                _log.LogInfo("BrowserEnvironmentEditor", $"[SAVE] Environment: Platform={env.Platform}, UserAgent={env.UserAgent?.Substring(0, Math.Min(50, env.UserAgent.Length))}...");
                env.HardwareConcurrency = int.TryParse(HardwareConcurrencyTextBox.Text, out var hc) ? hc : null;
                env.DeviceMemory = int.TryParse(DeviceMemoryTextBox.Text, out var dm) ? dm : null;
                env.MaxTouchPoints = int.TryParse(MaxTouchPointsTextBox.Text, out var mtp) ? mtp : null;
                env.WebGLVendor = WebGLVendorTextBox.Text;
                env.WebGLRenderer = WebGLRendererTextBox.Text;
                env.LanguagesJson = LanguagesJsonTextBox.Text;
                env.PluginsJson = PluginsJsonTextBox.Text;
                env.SecChUa = SecChUaTextBox.Text;
                env.SecChUaPlatform = SecChUaPlatformTextBox.Text;
                env.WebdriverMode = (WebdriverModeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "undefined";
                _log.LogInfo("BrowserEnvironmentEditor", $"[SAVE] WebdriverMode: {env.WebdriverMode}");
                
                // ⭐ 保存连接信息
                env.ConnectionType = _generatedConnectionType;
                env.ConnectionRtt = _generatedConnectionRtt;
                env.ConnectionDownlink = _generatedConnectionDownlink;

                // 自动创建或更新对应的 FingerprintProfile（用于启动浏览器）
                FingerprintProfile profile;
                if (env.FingerprintProfileId.HasValue)
                {
                    // 更新现有 Profile
                    profile = _db.FingerprintProfiles.FirstOrDefault(p => p.Id == env.FingerprintProfileId.Value);
                    if (profile != null)
                    {
                        UpdateProfileFromEnvironment(profile, env);
                        _db.FingerprintProfiles.Update(profile);
                    }
                    else
                    {
                        // Profile 不存在，创建新的
                        profile = CreateProfileFromEnvironment(env);
                        _db.FingerprintProfiles.Add(profile);
                        _db.SaveChanges(); // 先保存 Profile 获取 ID
                        env.FingerprintProfileId = profile.Id;
                    }
                }
                else
                {
                    // 创建新 Profile
                    profile = CreateProfileFromEnvironment(env);
                    _db.FingerprintProfiles.Add(profile);
                    _db.SaveChanges(); // 先保存 Profile 获取 ID
                    env.FingerprintProfileId = profile.Id;
                }
                
                // ⭐ 强制验证：确保 Profile 的 Platform 与 Environment 一致
                if (profile.Platform != env.Platform)
                {
                    _log.LogWarn("BrowserEnvironmentEditor", $"[PLATFORM MISMATCH] Profile.Platform={profile.Platform}, Env.Platform={env.Platform}, forcing update...");
                    profile.Platform = env.Platform;
                    profile.UserAgent = env.UserAgent;
                    profile.MaxTouchPoints = env.MaxTouchPoints ?? 0;
                    _db.FingerprintProfiles.Update(profile);
                    _db.SaveChanges();
                    _log.LogInfo("BrowserEnvironmentEditor", $"[PLATFORM FIXED] Profile.Platform updated to {profile.Platform}");
                }

                // 保存 Environment 到数据库
                if (_isEditMode)
                {
                    _db.BrowserEnvironments.Update(env);
                }
                else
                {
                    env.CreatedAt = DateTime.UtcNow;
                    _db.BrowserEnvironments.Add(env);
                }

                _db.SaveChanges();

                Result = env;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _log.LogError("BrowserEnvironmentEditor", $"Save failed: {ex.Message}");
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FingerprintProfile CreateProfileFromEnvironment(BrowserEnvironment env)
        {
            // ⭐ 调试日志：检查从 Environment 创建 Profile
            _log.LogInfo("BrowserEnvironmentEditor", $"[CREATE PROFILE] From Environment: Platform={env.Platform}, UserAgent={env.UserAgent?.Substring(0, Math.Min(50, env.UserAgent?.Length ?? 0))}...");
            
            // ⭐ 根据 Platform 设置正确的 Vendor
            var platform = env.Platform ?? "Win32";
            var vendor = platform switch
            {
                "iPhone" or "iPad" or "MacIntel" => "Apple Computer, Inc.",
                "Linux armv8l" => "Google Inc.",  // Android
                _ => "Google Inc."  // Windows/Linux
            };
            
            // ⭐ 根据 Platform 设置合理的硬件配置
            var defaultHardwareConcurrency = platform switch
            {
                "iPhone" or "iPad" => 4,  // iPhone 通常是 4 或 6 核
                "MacIntel" => 8,  // Mac 通常是 8 核或更多
                "Linux armv8l" => 8,  // Android 高端设备
                _ => 8  // Windows/Linux
            };
            
            var defaultDeviceMemory = platform switch
            {
                "iPhone" or "iPad" => 4,  // iPhone 通常是 4GB 或 6GB
                "MacIntel" => 8,  // Mac 通常是 8GB 或更多
                "Linux armv8l" => 6,  // Android 高端设备
                _ => 8  // Windows/Linux
            };
            
            var profile = new FingerprintProfile
            {
                Name = $"Auto_{env.Name}",
                UserAgent = env.UserAgent ?? "",
                Platform = platform,
                Vendor = vendor,
                Timezone = env.Timezone ?? "Asia/Shanghai",
                Locale = env.Locale ?? "zh-CN",
                ViewportWidth = env.ViewportWidth,
                ViewportHeight = env.ViewportHeight,
                HardwareConcurrency = env.HardwareConcurrency ?? defaultHardwareConcurrency,
                DeviceMemory = env.DeviceMemory ?? defaultDeviceMemory,
                MaxTouchPoints = env.MaxTouchPoints ?? 0,
                WebGLVendor = env.WebGLVendor,
                WebGLRenderer = env.WebGLRenderer,
                LanguagesJson = env.LanguagesJson,
                PluginsJson = env.PluginsJson,
                SecChUa = env.SecChUa,
                SecChUaPlatform = env.SecChUaPlatform,
                SecChUaMobile = env.SecChUaMobile,
                ConnectionType = env.ConnectionType,
                ConnectionRtt = env.ConnectionRtt ?? 0,
                ConnectionDownlink = env.ConnectionDownlink ?? 0,
                CreatedAt = DateTime.UtcNow
            };
            
            // 调试日志：检查创建的 Profile
            _log.LogInfo("BrowserEnvironmentEditor", $"[CREATED PROFILE] Platform={profile.Platform}, Vendor={profile.Vendor}, UserAgent={profile.UserAgent?.Substring(0, Math.Min(50, profile.UserAgent.Length))}...");
            return profile;
        }

        private void UpdateProfileFromEnvironment(FingerprintProfile profile, BrowserEnvironment env)
        {
            // 调试日志：检查更新前的 Profile
            _log.LogInfo("BrowserEnvironmentEditor", $"[UPDATE PROFILE BEFORE] Platform={profile.Platform}, UserAgent={profile.UserAgent?.Substring(0, Math.Min(50, profile.UserAgent.Length))}...");
            
            // ⭐ 根据 Platform 设置正确的 Vendor
            var platform = env.Platform ?? "Win32";
            var vendor = platform switch
            {
                "iPhone" or "iPad" or "MacIntel" => "Apple Computer, Inc.",
                "Linux armv8l" => "Google Inc.",  // Android
                _ => "Google Inc."  // Windows/Linux
            };
            
            profile.Name = $"Auto_{env.Name}";
            profile.UserAgent = env.UserAgent ?? "";
            profile.Platform = platform;
            profile.Vendor = vendor;
            profile.Timezone = env.Timezone ?? "Asia/Shanghai";
            
            // ⭐ 调试日志：检查更新后的 Profile
            _log.LogInfo("BrowserEnvironmentEditor", $"[UPDATE PROFILE AFTER] Platform={profile.Platform}, UserAgent={profile.UserAgent?.Substring(0, Math.Min(50, profile.UserAgent?.Length ?? 0))}...");
            profile.Locale = env.Locale ?? "zh-CN";
            profile.ViewportWidth = env.ViewportWidth;
            profile.ViewportHeight = env.ViewportHeight;
            profile.HardwareConcurrency = env.HardwareConcurrency ?? 8;
            profile.DeviceMemory = env.DeviceMemory ?? 8;
            profile.MaxTouchPoints = env.MaxTouchPoints ?? 0;
            profile.WebGLVendor = env.WebGLVendor;
            profile.WebGLRenderer = env.WebGLRenderer;
            profile.LanguagesJson = env.LanguagesJson;
            profile.PluginsJson = env.PluginsJson;
            profile.SecChUa = env.SecChUa;
            profile.SecChUaPlatform = env.SecChUaPlatform;
            profile.SecChUaMobile = env.SecChUaMobile;
            profile.ConnectionType = env.ConnectionType;
            profile.ConnectionRtt = env.ConnectionRtt ?? 0;
            profile.ConnectionDownlink = env.ConnectionDownlink ?? 0;
            profile.UpdatedAt = DateTime.UtcNow;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _validationTimer?.Stop();
            base.OnClosed(e);
        }

        /// <summary>
        /// 从 User-Agent 提取 Chrome 版本号
        /// </summary>
        private int? ExtractChromeVersion(string userAgent)
        {
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(userAgent ?? "", @"Chrome/(\d+)\.");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int version))
                {
                    return version;
                }
            }
            catch { }
            return null;
        }

        #endregion
    }
}
