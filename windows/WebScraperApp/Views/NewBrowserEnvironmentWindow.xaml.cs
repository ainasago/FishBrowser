using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;

namespace FishBrowser.WPF.Views;

public partial class NewBrowserEnvironmentWindow : Window
{
    // Selected profile
    private FingerprintProfile? _selectedProfile;
    
    // Fonts state (iter1)
    private string _fontsMode = "real"; // real | custom
    private System.Collections.Generic.List<string> _selectedFonts = new System.Collections.Generic.List<string>();
    // WebGL / WebGPU / Audio / Speech
    private string _webGLImageMode = "noise";
    private string _webGLInfoMode = "ua_based";
    private string _webGpuMode = "match_webgl";
    private string _audioContextMode = "noise";
    private bool _speechVoicesEnabled = true;

    // 标记用户是否在当前会话中修改过指纹相关 UI（WebGL/Fonts/WebGPU/Audio/Speech）
    private bool _userModifiedFingerprint = false;
    // 抑制在初始化/加载时的覆盖提示，仅对用户主动切换时提示
    private bool _isInitializingUI = false;
    // 抑制在窗口加载完成前或代码设置引发的选择变更时的覆盖提示
    private bool _suppressProfilePrompt = true;

    private IHost _host;
    private WebScraperDbContext _db;
    private ILogService _log;
    private BrowserEnvironmentService _envSvc;
    private ProxyCatalogService _proxyCatalog;
    private ProxyHealthService _proxyHealth;
    private BrowserEnvironment? _createdEnv;
    private BrowserEnvironment? _editingEnv;
    private bool _isEditMode;

    public NewBrowserEnvironmentWindow(BrowserEnvironment? existingEnv = null)
    {
        _isInitializingUI = true;
        InitializeComponent();
        
        _host = System.Windows.Application.Current.Resources["Host"] as IHost ?? throw new InvalidOperationException("Host not found");
        _db = _host.Services.GetRequiredService<WebScraperDbContext>();
        _log = _host.Services.GetRequiredService<ILogService>();
        _envSvc = _host.Services.GetRequiredService<BrowserEnvironmentService>();
        _proxyCatalog = _host.Services.GetRequiredService<ProxyCatalogService>();
        _proxyHealth = _host.Services.GetRequiredService<ProxyHealthService>();

        if (existingEnv != null)
        {
            _editingEnv = existingEnv;
            _isEditMode = true;
            Title = "编辑浏览器环境";
            LoadExistingData(existingEnv);
        }

        UpdateUaCustomVisibility();
        UpdateButtonText();
        LoadProfilesAndPresets();
        LoadProxies();
        LoadPools();
        InitializeToggleButtons(); // 初始化 ToggleButton 状态
        UpdatePreview(); // 初始化时显示默认预览
        // 在窗口 Loaded 后再允许提示，避免初始化过程中的选择变化触发覆盖提示
        this.Loaded += (_, __) => { _isInitializingUI = false; _suppressProfilePrompt = false; };
    }

    private void Randomize_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var opts = new BrowserEnvironmentService.RandomizeOptions();
            var draft = _envSvc.BuildRandomDraft(opts);
            ApplyDraftToUI(draft);
            UpdatePreview();
            StatusText.Text = "已生成随机配置";
        }
        catch (Exception ex)
        {
            _log?.LogError("EnvUI", $"Randomize failed: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"随机生成失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AdvancedRandomize_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            //var dlg = new FishBrowser.WPF.Views.Dialogs.AdvancedRandomizeDialog { Owner = this };
            //if (dlg.ShowDialog() == true)
            //{
            //    var opts = dlg.SelectedOptions;
            //    var draft = _envSvc.BuildRandomDraft(opts);
            //    ApplyDraftToUI(draft);
            //    UpdatePreview();
            //    StatusText.Text = "已按高级参数生成随机配置";
            //}
        }
        catch (Exception ex)
        {
            _log?.LogError("EnvUI", $"AdvancedRandomize failed: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"高级随机失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyDraftToUI(BrowserEnvironment d)
    {
        // Engine
        SetComboTag(EngineCombo, d.Engine);
        EngineChromeBtn.IsChecked = d.Engine == "chrome";
        if (EngineFirefoxBtn != null) EngineFirefoxBtn.IsChecked = d.Engine == "firefox";
        if (EngineWebKitBtn != null) EngineWebKitBtn.IsChecked = d.Engine == "webkit";
        if (EngineEdgeBtn != null) EngineEdgeBtn.IsChecked = d.Engine == "edge";
        if (EngineBraveBtn != null) EngineBraveBtn.IsChecked = d.Engine == "brave";
        // OS
        SetComboTag(OsCombo, d.OS);
        OsWindowsBtn.IsChecked = d.OS == "windows";
        OsAndroidBtn.IsChecked = d.OS == "android";
        OsiOSBtn.IsChecked = d.OS == "ios";
        OsMacBtn.IsChecked = d.OS == "macos";
        OsLinuxBtn.IsChecked = d.OS == "linux";
        // UA
        SetComboTag(UaModeCombo, d.UaMode);
        UaRandomBtn.IsChecked = d.UaMode == "random";
        UaMatchBtn.IsChecked = d.UaMode == "match";
        UaCustomBtn.IsChecked = d.UaMode == "custom";
        UaProfileBtn.IsChecked = d.UaMode == "profile";
        CustomUaBox.Text = d.CustomUserAgent ?? string.Empty;
        UpdateUaCustomVisibility();
        // Proxy basic
        SetComboTag(ProxyModeCombo, d.ProxyMode);
        ProxyNoneBtn.IsChecked = d.ProxyMode == "none";
        ProxyRefBtn.IsChecked = d.ProxyMode == "reference";
        ProxyApiBtn.IsChecked = d.ProxyMode == "api";
        ProxyTargetServerBtn.IsChecked = d.ProxyType != null && d.ProxyType.Equals("server", StringComparison.OrdinalIgnoreCase);
        ProxyTargetPoolBtn.IsChecked = d.ProxyType != null && d.ProxyType.Equals("pool", StringComparison.OrdinalIgnoreCase);
        // Region & vendor/device
        RegionBox.Text = d.Region ?? "CN";
        RegionIpBtn.IsChecked = RegionBox.Text.Equals("IP", StringComparison.OrdinalIgnoreCase);
        RegionCnBtn.IsChecked = RegionBox.Text.Equals("CN", StringComparison.OrdinalIgnoreCase);
        RegionUsBtn.IsChecked = RegionBox.Text.Equals("US", StringComparison.OrdinalIgnoreCase);
        DeviceClassBox.Text = d.DeviceClass ?? string.Empty;
        VendorBox.Text = d.Vendor ?? string.Empty;
        // Resolution
        SetComboTag(ResolutionCombo, d.ResolutionMode);
        ResolutionRealBtn.IsChecked = d.ResolutionMode == "real";
        ResolutionCustomBtn.IsChecked = d.ResolutionMode == "custom";
        // Fonts
        SetComboTag(FontsModeCombo, d.FontsListMode);
        FontsRealBtn.IsChecked = d.FontsListMode == "real";
        FontsCustomBtn.IsChecked = d.FontsListMode == "custom";
        _fontsMode = d.FontsListMode ?? _fontsMode;
        if (EditFontsButton != null) EditFontsButton.IsEnabled = string.Equals(_fontsMode, "custom", StringComparison.OrdinalIgnoreCase);
        if (ShuffleFontsButton != null) ShuffleFontsButton.IsEnabled = string.Equals(_fontsMode, "custom", StringComparison.OrdinalIgnoreCase);
        // 若为自定义模式，优先使用草稿自带的 FontsJson；若空则异步填充默认子集
        if (string.Equals(_fontsMode, "custom", StringComparison.OrdinalIgnoreCase))
        {
            bool loadedFromDraft = false;
            try
            {
                if (!string.IsNullOrWhiteSpace(d.FontsJson))
                {
                    var list = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(d.FontsJson!);
                    if (list != null && list.Count > 0)
                    {
                        _selectedFonts = list;
                        loadedFromDraft = true;
                        _log?.LogInfo("EnvUI", $"[FONTS-UI] ApplyDraft: loaded from draft FontsJson, count={_selectedFonts.Count}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                _log?.LogWarn("EnvUI", $"[FONTS-UI] ApplyDraft: parse FontsJson failed: {ex.Message}");
            }
            if (!loadedFromDraft && (_selectedFonts == null || _selectedFonts.Count == 0))
            {
                _ = EnsureFontsReadyAsync();
            }
        }
        UpdateFontsSummary();
        try
        {
            var head = string.Join(", ", _selectedFonts.Take(6));
            _log?.LogInfo("EnvUI", $"[FONTS-UI] ApplyDraft: mode={_fontsMode}, count={_selectedFonts.Count}, head=[{head}{(_selectedFonts.Count>6?" …":"")}] ");
        }
        catch { }
        // WebRTC/Canvas/WebGL/WebGPU/Audio/Speech
        SetComboTag(WebRtcCombo, d.WebRtcMode);
        WebRtcHideBtn.IsChecked = d.WebRtcMode == "hide";
        WebRtcDisableBtn.IsChecked = d.WebRtcMode == "disable";
        WebRtcRealBtn.IsChecked = d.WebRtcMode == "real";
        CanvasNoiseBtn.IsChecked = d.CanvasMode == "noise";
        CanvasRealBtn.IsChecked = d.CanvasMode == "real";
        _webGLImageMode = d.WebGLImageMode ?? _webGLImageMode;
        _webGLInfoMode = d.WebGLInfoMode ?? _webGLInfoMode;
        _webGpuMode = d.WebGpuMode ?? _webGpuMode;
        _audioContextMode = d.AudioContextMode ?? _audioContextMode;
        _speechVoicesEnabled = d.SpeechVoicesEnabled;
        // 当 Info=custom 时，把服务层生成的 Vendor/Renderer 映射到 UI；否则清空
        try
        {
            if (string.Equals(_webGLInfoMode, "custom", StringComparison.OrdinalIgnoreCase))
            {
                if (WebGLVendorBox != null) WebGLVendorBox.Text = string.IsNullOrWhiteSpace(d.WebGLVendor) ? WebGLVendorBox.Text : d.WebGLVendor;
                if (WebGLRendererBox != null) WebGLRendererBox.Text = string.IsNullOrWhiteSpace(d.WebGLRenderer) ? WebGLRendererBox.Text : d.WebGLRenderer;
            }
            else
            {
                if (WebGLVendorBox != null) WebGLVendorBox.Text = string.Empty;
                if (WebGLRendererBox != null) WebGLRendererBox.Text = string.Empty;
            }
            _log?.LogInfo("EnvUI", $"ApplyDraft: webglInfo={_webGLInfoMode}, vendor={(d.WebGLVendor ?? "<null>")}, renderer={(d.WebGLRenderer ?? "<null>")}");
        }
        catch { }
        WebGLImageNoiseBtn.IsChecked = _webGLImageMode == "noise";
        WebGLImageRealBtn.IsChecked = _webGLImageMode == "real";
        WebGLInfoUaBtn.IsChecked = _webGLInfoMode == "ua_based";
        WebGLInfoCustomBtn.IsChecked = _webGLInfoMode == "custom";
        WebGLInfoDisableBtn.IsChecked = _webGLInfoMode == "disable_hwaccel";
        WebGLInfoRealBtn.IsChecked = _webGLInfoMode == "real";
        WebGpuMatchBtn.IsChecked = _webGpuMode == "match_webgl";
        WebGpuRealBtn.IsChecked = _webGpuMode == "real";
        WebGpuDisableBtn.IsChecked = _webGpuMode == "disable";
        AudioNoiseBtn.IsChecked = _audioContextMode == "noise";
        AudioRealBtn.IsChecked = _audioContextMode == "real";
        SpeechEnabledBtn.IsChecked = _speechVoicesEnabled;
        SpeechDisabledBtn.IsChecked = !_speechVoicesEnabled;
        // Resources
        HwBox.Text = (d.HardwareConcurrency > 0 ? d.HardwareConcurrency : 8).ToString();
        MemBox.Text = (d.DeviceMemory > 0 ? d.DeviceMemory : 8).ToString();
    }

    private BrowserEnvironment BuildDraftFromUI()
    {
        return new BrowserEnvironment
        {
            Name = string.IsNullOrWhiteSpace(NameBox.Text) ? $"Env-{DateTime.Now:yyyyMMddHHmmss}" : NameBox.Text.Trim(),
            Engine = GetSelectedTag(EngineCombo) ?? "chrome",
            OS = GetSelectedTag(OsCombo) ?? "windows",
            UaMode = GetSelectedTag(UaModeCombo) ?? "random",
            CustomUserAgent = string.IsNullOrWhiteSpace(CustomUaBox.Text) ? null : CustomUaBox.Text.Trim(),
            ProxyMode = GetSelectedTag(ProxyModeCombo) ?? "none",
            ProxyRefId = ExistingProxyCombo?.SelectedValue as int?,
            ProxyApiUrl = null,
            ProxyApiAuth = null,
            ProxyType = ProxyTargetPoolBtn?.IsChecked == true ? "pool" : "server",
            ProxyAccount = null,
            Region = RegionBox.Text,
            Vendor = VendorBox.Text,
            DeviceClass = DeviceClassBox.Text,
            LanguageMode = string.Equals(RegionBox.Text, "IP", StringComparison.OrdinalIgnoreCase) ? "ip" : "match",
            TimezoneMode = string.Equals(RegionBox.Text, "IP", StringComparison.OrdinalIgnoreCase) ? "ip" : "match",
            GeolocationMode = "match",
            ResolutionMode = GetSelectedTag(ResolutionCombo) ?? "real",
            FontsListMode = GetSelectedTag(FontsModeCombo) ?? "real",
            FontsFingerprintMode = GetSelectedTag(FontsModeCombo) ?? "real",
            FontsMode = GetSelectedTag(FontsModeCombo) ?? "real",
            FontsJson = string.Equals(GetSelectedTag(FontsModeCombo) ?? "real", "custom", StringComparison.OrdinalIgnoreCase)
                ? System.Text.Json.JsonSerializer.Serialize(_selectedFonts ?? new System.Collections.Generic.List<string>())
                : null,
            WebRtcMode = GetSelectedTag(WebRtcCombo) ?? "hide",
            CanvasMode = GetSelectedTag(CanvasCombo) ?? "noise",
            WebGLImageMode = _webGLImageMode,
            WebGLInfoMode = _webGLInfoMode,
            WebGpuMode = _webGpuMode,
            AudioContextMode = _audioContextMode,
            SpeechVoicesEnabled = _speechVoicesEnabled,
            MediaDevicesMode = "real",
            WebGLVendor = string.Equals(_webGLInfoMode, "custom", StringComparison.OrdinalIgnoreCase) && WebGLVendorBox != null && !string.IsNullOrWhiteSpace(WebGLVendorBox.Text) ? WebGLVendorBox.Text.Trim() : null,
            WebGLRenderer = string.Equals(_webGLInfoMode, "custom", StringComparison.OrdinalIgnoreCase) && WebGLRendererBox != null && !string.IsNullOrWhiteSpace(WebGLRendererBox.Text) ? WebGLRendererBox.Text.Trim() : null,
            HardwareConcurrency = SafeParseInt(HwBox.Text, 10),
            DeviceMemory = SafeParseInt(MemBox.Text, 8),
            Dnt = "default",
            BatteryMode = "real",
            PortscanProtect = true,
            TlsPolicy = "default",
            LaunchArgs = null,
            CookiesMode = "by_env",
            MultiOpen = "follow_team",
            Notifications = "ask",
            BlockImages = "follow_team",
            BlockVideos = "follow_team",
            BlockSound = "follow_team"
        };
    }

    private void LoadPools()
    {
        try
        {
            var pools = _proxyCatalog.GetPools();
            if (ExistingPoolCombo != null)
            {
                ExistingPoolCombo.ItemsSource = pools;
                if (_editingEnv?.ProxyType != null && _editingEnv.ProxyType.Equals("pool", StringComparison.OrdinalIgnoreCase) && _editingEnv.ProxyRefId.HasValue)
                {
                    ExistingPoolCombo.SelectedValue = _editingEnv.ProxyRefId.Value;
                }
                else if (pools.Count > 0)
                {
                    ExistingPoolCombo.SelectedIndex = 0;
                }
            }
            _log?.LogInfo("EnvUI", $"Loaded {pools.Count} proxy pools");
        }
        catch (Exception ex)
        {
            _log?.LogWarn("EnvUI", $"LoadPools failed: {ex.Message}");
        }
    }

    private void LoadProxies()
    {
        try
        {
            var list = _proxyCatalog.GetAll();
            if (ExistingProxyCombo != null)
            {
                ExistingProxyCombo.ItemsSource = list;
                if (_editingEnv?.ProxyRefId != null)
                {
                    ExistingProxyCombo.SelectedValue = _editingEnv.ProxyRefId.Value;
                }
                else if (list.Count > 0)
                {
                    ExistingProxyCombo.SelectedIndex = 0;
                }
            }
            _log?.LogInfo("EnvUI", $"Loaded {list.Count} proxies");
        }
        catch (Exception ex)
        {
            _log?.LogWarn("EnvUI", $"LoadProxies failed: {ex.Message}");
        }
    }

    private void AddProxyInline_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new AddProxyDialog { Owner = this };
            if (dlg.ShowDialog() == true && dlg.Result != null)
            {
                var p = dlg.Result;
                var label = string.IsNullOrWhiteSpace(p.Label) ? $"{p.Address}:{p.Port}" : p.Label;
                var created = _proxyCatalog.Create(label, p.Protocol, p.Address, p.Port, p.Username, dlg.PasswordPlain);
                // 若对话框选择了所属池，则写入 PoolId 并持久化
                if (dlg.Result.PoolId.HasValue)
                {
                    created.PoolId = dlg.Result.PoolId.Value;
                    _proxyCatalog.Update(created);
                    LoadPools();
                }
                LoadProxies();
                if (ExistingProxyCombo != null)
                {
                    ExistingProxyCombo.SelectedValue = created.Id;
                }
                MessageBox.Show("代理已创建", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _log?.LogError("EnvUI", $"AddProxyInline failed: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"创建代理失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExistingProxyCombo_DropDownOpened(object sender, EventArgs e)
    {
        // 打开下拉时刷新一次列表，确保看到刚添加的代理
        var current = ExistingProxyCombo?.SelectedValue as int?;
        LoadProxies();
        if (current.HasValue && ExistingProxyCombo != null)
        {
            ExistingProxyCombo.SelectedValue = current.Value;
        }
    }

    private void ExistingPoolCombo_DropDownOpened(object sender, EventArgs e)
    {
        var current = ExistingPoolCombo?.SelectedValue as int?;
        LoadPools();
        if (current.HasValue && ExistingPoolCombo != null)
        {
            ExistingPoolCombo.SelectedValue = current.Value;
        }
    }

    private void ProxyTarget_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Primitives.ToggleButton btn && btn.Tag is string tag)
        {
            var isPool = string.Equals(tag, "pool", StringComparison.OrdinalIgnoreCase);
            ProxyTargetServerBtn.IsChecked = !isPool;
            ProxyTargetPoolBtn.IsChecked = isPool;
            UpdateProxyUIVisibility();
        }
    }

    private void AddPoolInline_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new AddPoolDialog { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                var pool = _proxyCatalog.CreatePool(dlg.PoolName, dlg.Strategy, dlg.OptionsJson);
                LoadPools();
                if (ExistingPoolCombo != null)
                {
                    ExistingPoolCombo.SelectedValue = pool.Id;
                }
                MessageBox.Show("代理池已创建", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _log?.LogError("EnvUI", $"AddPoolInline failed: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"创建代理池失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void TestProxy_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ExistingProxyCombo?.SelectedItem is ProxyServer proxy)
            {
                StatusText.Text = "正在测试代理...";
                var ok = await _proxyHealth.QuickProbeAsync(proxy);
                StatusText.Text = ok ? $"代理可用，延迟 {proxy.ResponseTimeMs} ms" : "代理不可用";
                MessageBox.Show(ok ? $"可用，延迟 {proxy.ResponseTimeMs} ms" : "不可用", "测试结果", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show("请先选择一个代理", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _log?.LogWarn("EnvUI", $"TestProxy failed: {ex.Message}");
            MessageBox.Show($"测试失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void InitializeToggleButtons()
    {
        // Engine
        var engineTag = GetSelectedTag(EngineCombo) ?? "chrome";
        if (EngineChromeBtn != null) EngineChromeBtn.IsChecked = (engineTag == "chrome");
        if (EngineFirefoxBtn != null) EngineFirefoxBtn.IsChecked = (engineTag == "firefox");
        if (EngineWebKitBtn != null) EngineWebKitBtn.IsChecked = (engineTag == "webkit");
        if (EngineEdgeBtn != null) EngineEdgeBtn.IsChecked = (engineTag == "edge");
        if (EngineBraveBtn != null) EngineBraveBtn.IsChecked = (engineTag == "brave");

        // Engine Mode (默认 playwright)
        var engineMode = _editingEnv?.EngineMode ?? "playwright";
        if (EngineModePlaywrightBtn != null) EngineModePlaywrightBtn.IsChecked = (engineMode == "playwright");
        if (EngineModeUndetectedBtn != null) EngineModeUndetectedBtn.IsChecked = (engineMode == "undetected_chrome");

        // OS
        var osTagInit = GetSelectedTag(OsCombo) ?? "windows";
        if (OsWindowsBtn != null) OsWindowsBtn.IsChecked = (osTagInit == "windows");
        if (OsAndroidBtn != null) OsAndroidBtn.IsChecked = (osTagInit == "android");
        if (OsiOSBtn != null) OsiOSBtn.IsChecked = (osTagInit == "ios");
        if (OsMacBtn != null) OsMacBtn.IsChecked = (osTagInit == "macos");
        if (OsLinuxBtn != null) OsLinuxBtn.IsChecked = (osTagInit == "linux");

        // Proxy
        var proxyTag = GetSelectedTag(ProxyModeCombo) ?? "none";
        if (ProxyNoneBtn != null) ProxyNoneBtn.IsChecked = (proxyTag == "none");
        if (ProxyRefBtn != null) ProxyRefBtn.IsChecked = (proxyTag == "reference");
        if (ProxyApiBtn != null) ProxyApiBtn.IsChecked = (proxyTag == "api");
        // Proxy target (server or pool)
        var target = _editingEnv?.ProxyType ?? "server";
        if (ProxyTargetServerBtn != null) ProxyTargetServerBtn.IsChecked = !string.Equals(target, "pool", StringComparison.OrdinalIgnoreCase);
        if (ProxyTargetPoolBtn != null) ProxyTargetPoolBtn.IsChecked = string.Equals(target, "pool", StringComparison.OrdinalIgnoreCase);
        UpdateProxyUIVisibility();

        // Region (store chosen into RegionBox for compatibility)
        if (string.IsNullOrWhiteSpace(RegionBox.Text)) RegionBox.Text = "CN";
        if (RegionIpBtn != null) RegionIpBtn.IsChecked = RegionBox.Text.Equals("IP", StringComparison.OrdinalIgnoreCase);
        if (RegionCnBtn != null) RegionCnBtn.IsChecked = RegionBox.Text.Equals("CN", StringComparison.OrdinalIgnoreCase);
        if (RegionUsBtn != null) RegionUsBtn.IsChecked = RegionBox.Text.Equals("US", StringComparison.OrdinalIgnoreCase);

        // Resolution
        var resTag = GetSelectedTag(ResolutionCombo) ?? "real";
        ResolutionRealBtn.IsChecked = (resTag == "real");
        ResolutionCustomBtn.IsChecked = (resTag == "custom");

        // WebRTC
        var webrtcTag = GetSelectedTag(WebRtcCombo) ?? "hide";
        WebRtcHideBtn.IsChecked = (webrtcTag == "hide");
        WebRtcDisableBtn.IsChecked = (webrtcTag == "disable");
        WebRtcRealBtn.IsChecked = (webrtcTag == "real");

        // Canvas
        var canvasTag = GetSelectedTag(CanvasCombo) ?? "noise";
        CanvasNoiseBtn.IsChecked = (canvasTag == "noise");
        CanvasRealBtn.IsChecked = (canvasTag == "real");

        // Webdriver
        var webdriverTag = _editingEnv?.WebdriverMode ?? "undefined";
        WebdriverUndefinedBtn.IsChecked = (webdriverTag == "undefined");
        WebdriverTrueBtn.IsChecked = (webdriverTag == "true");
        WebdriverFalseBtn.IsChecked = (webdriverTag == "false");

        // WebGL Image
        WebGLImageNoiseBtn.IsChecked = (_webGLImageMode == "noise");
        WebGLImageRealBtn.IsChecked = (_webGLImageMode == "real");
        
        // WebGL Info
        WebGLInfoUaBtn.IsChecked = (_webGLInfoMode == "ua_based");
        WebGLInfoCustomBtn.IsChecked = (_webGLInfoMode == "custom");
        WebGLInfoDisableBtn.IsChecked = (_webGLInfoMode == "disable_hwaccel");
        WebGLInfoRealBtn.IsChecked = (_webGLInfoMode == "real");
        
        // WebGPU
        WebGpuMatchBtn.IsChecked = (_webGpuMode == "match_webgl");
        WebGpuRealBtn.IsChecked = (_webGpuMode == "real");
        WebGpuDisableBtn.IsChecked = (_webGpuMode == "disable");
        
        // AudioContext
        AudioNoiseBtn.IsChecked = (_audioContextMode == "noise");
        AudioRealBtn.IsChecked = (_audioContextMode == "real");
        
        // SpeechVoices
        SpeechEnabledBtn.IsChecked = _speechVoicesEnabled;
        SpeechDisabledBtn.IsChecked = !_speechVoicesEnabled;

        // UA Mode
        var uaTag = GetSelectedTag(UaModeCombo) ?? "random";
        UaRandomBtn.IsChecked = (uaTag == "random");
        UaMatchBtn.IsChecked = (uaTag == "match");
        UaCustomBtn.IsChecked = (uaTag == "custom");
        UaProfileBtn.IsChecked = (uaTag == "profile");
        UpdateUaCustomVisibility();
    }

    private void Engine_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Primitives.ToggleButton btn && btn.Tag is string tag)
        {
            SetComboTag(EngineCombo, tag);
            EngineChromeBtn.IsChecked = (tag == "chrome");
            if (EngineFirefoxBtn != null) EngineFirefoxBtn.IsChecked = (tag == "firefox");
            if (EngineWebKitBtn != null) EngineWebKitBtn.IsChecked = (tag == "webkit");
            if (EngineEdgeBtn != null) EngineEdgeBtn.IsChecked = (tag == "edge");
            if (EngineBraveBtn != null) EngineBraveBtn.IsChecked = (tag == "brave");
            _log.LogInfo("EnvUI", $"Engine changed to: {tag}");
            UpdatePreview();
        }
    }

    private void EngineMode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Primitives.ToggleButton btn && btn.Tag is string tag)
        {
            EngineModePlaywrightBtn.IsChecked = (tag == "playwright");
            EngineModeUndetectedBtn.IsChecked = (tag == "undetected_chrome");
            _log.LogInfo("EnvUI", $"Engine mode changed to: {tag}");
            
            // 如果选择 UndetectedChrome，强制引擎为 Chrome
            if (tag == "undetected_chrome")
            {
                SetComboTag(EngineCombo, "chrome");
                EngineChromeBtn.IsChecked = true;
                EngineFirefoxBtn.IsChecked = false;
                EngineWebKitBtn.IsChecked = false;
                EngineEdgeBtn.IsChecked = false;
                EngineBraveBtn.IsChecked = false;
                _log.LogInfo("EnvUI", "UndetectedChrome mode: forcing engine to Chrome");
            }
            
            UpdatePreview();
        }
    }

    private void Os_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Primitives.ToggleButton btn && btn.Tag is string tag)
        {
            SetComboTag(OsCombo, tag);
            OsWindowsBtn.IsChecked = (tag == "windows");
            OsAndroidBtn.IsChecked = (tag == "android");
            OsiOSBtn.IsChecked = (tag == "ios");
            OsMacBtn.IsChecked = (tag == "macos");
            OsLinuxBtn.IsChecked = (tag == "linux");
            _log.LogInfo("EnvUI", $"OS changed to: {tag}");
            ApplyOsDefaults();
            UpdatePreview();
        }
    }

    private void Proxy_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Primitives.ToggleButton btn && btn.Tag is string tag)
        {
            SetComboTag(ProxyModeCombo, tag);
            ProxyNoneBtn.IsChecked = (tag == "none");
            ProxyRefBtn.IsChecked = (tag == "reference");
            ProxyApiBtn.IsChecked = (tag == "api");
            _log.LogInfo("EnvUI", $"Proxy mode changed to: {tag}");
            UpdateProxyUIVisibility();
            UpdatePreview();
        }
    }

    private void UpdateProxyUIVisibility()
    {
        var mode = GetSelectedTag(ProxyModeCombo) ?? "none";
        var isRef = string.Equals(mode, "reference", StringComparison.OrdinalIgnoreCase);
        if (ProxyTargetServerBtn != null) ProxyTargetServerBtn.IsEnabled = isRef;
        if (ProxyTargetPoolBtn != null) ProxyTargetPoolBtn.IsEnabled = isRef;
        var usePool = ProxyTargetPoolBtn?.IsChecked == true;
        if (ExistingProxyCombo != null)
        {
            ExistingProxyCombo.IsEnabled = isRef && !usePool;
            ExistingProxyCombo.Visibility = (isRef && !usePool) ? Visibility.Visible : Visibility.Collapsed;
        }
        if (ExistingPoolCombo != null)
        {
            ExistingPoolCombo.IsEnabled = isRef && usePool;
            ExistingPoolCombo.Visibility = (isRef && usePool) ? Visibility.Visible : Visibility.Collapsed;
        }
        if (AddPoolInlineButton != null)
        {
            AddPoolInlineButton.IsEnabled = isRef && usePool;
            AddPoolInlineButton.Visibility = (isRef && usePool) ? Visibility.Visible : Visibility.Collapsed;
        }
        if (AddProxyInlineButton != null) AddProxyInlineButton.IsEnabled = isRef && !usePool;
        if (TestProxyButton != null) TestProxyButton.IsEnabled = isRef; // 测试按钮：后续可对池实现批测
    }

    private void Region_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Primitives.ToggleButton btn && btn.Tag is string tag)
        {
            RegionBox.Text = tag;
            var t = tag.ToUpperInvariant();
            if (RegionIpBtn != null) RegionIpBtn.IsChecked = (t == "IP");
            RegionCnBtn.IsChecked = (t == "CN");
            RegionUsBtn.IsChecked = (t == "US");
            if (RegionJpBtn != null) RegionJpBtn.IsChecked = (t == "JP");
            if (RegionDeBtn != null) RegionDeBtn.IsChecked = (t == "DE");
            if (RegionUkBtn != null) RegionUkBtn.IsChecked = (t == "UK");
            if (RegionInBtn != null) RegionInBtn.IsChecked = (t == "IN");
            if (RegionBrBtn != null) RegionBrBtn.IsChecked = (t == "BR");
            _log.LogInfo("EnvUI", $"Region changed to: {tag}");
            UpdatePreview();
        }
    }

    private void UaMode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Primitives.ToggleButton btn && btn.Tag is string tag)
        {
            SetComboTag(UaModeCombo, tag);
            UaRandomBtn.IsChecked = (tag == "random");
            UaMatchBtn.IsChecked = (tag == "match");
            UaCustomBtn.IsChecked = (tag == "custom");
            UaProfileBtn.IsChecked = (tag == "profile");
            UpdateUaCustomVisibility();
            _log.LogInfo("EnvUI", $"UA mode changed to: {tag}");
            UpdatePreview();
        }
    }

    private void Resolution_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Primitives.ToggleButton btn && btn.Tag is string tag)
        {
            SetComboTag(ResolutionCombo, tag);
            ResolutionRealBtn.IsChecked = (tag == "real");
            ResolutionCustomBtn.IsChecked = (tag == "custom");
            
            // 显示/隐藏分辨率输入框
            bool isCustom = (tag == "custom");
            ResolutionWidthLabel.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
            ResolutionWidthBox.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
            ResolutionHeightLabel.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
            ResolutionHeightBox.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
            
            // 如果选择自定义，从 Profile 加载默认值
            if (isCustom && _selectedProfile != null)
            {
                ResolutionWidthBox.Text = _selectedProfile.ViewportWidth.ToString();
                ResolutionHeightBox.Text = _selectedProfile.ViewportHeight.ToString();
            }
            
            _log.LogInfo("EnvUI", $"Resolution changed to: {tag}");
            UpdatePreview();
        }
    }

    private void WebRtc_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Primitives.ToggleButton btn && btn.Tag is string tag)
        {
            SetComboTag(WebRtcCombo, tag);
            WebRtcHideBtn.IsChecked = (tag == "hide");
            WebRtcDisableBtn.IsChecked = (tag == "disable");
            WebRtcRealBtn.IsChecked = (tag == "real");
            _log.LogInfo("EnvUI", $"WebRTC changed to: {tag}");
            UpdatePreview();
        }
    }

    private void Canvas_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Primitives.ToggleButton btn && btn.Tag is string tag)
        {
            SetComboTag(CanvasCombo, tag);
            CanvasNoiseBtn.IsChecked = (tag == "noise");
            CanvasRealBtn.IsChecked = (tag == "real");
            _log.LogInfo("EnvUI", $"Canvas mode changed to: {tag}");
            UpdatePreview();
        }
    }

    private void Webdriver_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Primitives.ToggleButton btn && btn.Tag is string tag)
        {
            SetComboTag(WebdriverCombo, tag);
            WebdriverUndefinedBtn.IsChecked = (tag == "undefined");
            WebdriverTrueBtn.IsChecked = (tag == "true");
            WebdriverFalseBtn.IsChecked = (tag == "false");
            _log.LogInfo("EnvUI", $"Webdriver mode changed to: {tag}");
            UpdatePreview();
        }
    }

    private void FontsMode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Primitives.ToggleButton btn && btn.Tag is string tag)
        {
            SetComboTag(FontsModeCombo, tag);
            FontsRealBtn.IsChecked = (tag == "real");
            FontsCustomBtn.IsChecked = (tag == "custom");
            _fontsMode = tag;
            var isCustom = string.Equals(_fontsMode, "custom", StringComparison.OrdinalIgnoreCase);
            if (EditFontsButton != null) EditFontsButton.IsEnabled = isCustom;
            if (ShuffleFontsButton != null) ShuffleFontsButton.IsEnabled = isCustom;
            UpdateFontsSummary();
            _log.LogInfo("EnvUI", $"Fonts mode changed to: {tag}");
            UpdatePreview();
        }
    }

    private void LoadProfilesAndPresets()
    {
        try
        {
            // 加载已有的 FingerprintProfile
            var profiles = _db.FingerprintProfiles.OrderByDescending(p => p.UpdatedAt).ToList();
            ExistingProfileCombo.ItemsSource = profiles;
            _log.LogInfo("EnvUI", $"Loaded {profiles.Count} profiles from database");
            
            if (profiles.Any())
            {
                ExistingProfileCombo.SelectedIndex = 0;
                _log.LogInfo("EnvUI", $"Selected first profile: {profiles[0].Name}");
            }
            else
            {
                _log.LogWarn("EnvUI", "No profiles found in database");
            }

            // 加载预设（TraitGroupPresets）
            var presets = _db.TraitGroupPresets.OrderBy(p => p.Name).ToList();
            PresetCombo.ItemsSource = presets;
            _log.LogInfo("EnvUI", $"Loaded {presets.Count} presets from database");
            
            if (presets.Any())
            {
                PresetCombo.SelectedIndex = 0;
                _log.LogInfo("EnvUI", $"Selected first preset: {presets[0].Name}");
            }
            else
            {
                _log.LogWarn("EnvUI", "No presets found in database");
            }
        }
        catch (Exception ex)
        {
            _log.LogError("EnvUI", $"Failed to load profiles/presets: {ex.Message}", ex.StackTrace);
        }
    }

    private void UaModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateUaCustomVisibility();
        if (_log != null) _log.LogInfo("EnvUI", "UA mode selection changed");
        UpdatePreview();
        // 确保 UI 稳定后再恢复提示
        Dispatcher.BeginInvoke(new Action(() => { _isInitializingUI = false; _suppressProfilePrompt = false; }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }

    private async void FontsModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var beforeCount = _selectedFonts.Count;
        _fontsMode = GetSelectedTag(FontsModeCombo) ?? "real";
        var isCustom = string.Equals(_fontsMode, "custom", StringComparison.OrdinalIgnoreCase);
        if (EditFontsButton != null) EditFontsButton.IsEnabled = isCustom;
        if (ShuffleFontsButton != null) ShuffleFontsButton.IsEnabled = isCustom;
        if (isCustom && _selectedFonts.Count == 0)
        {
            await ShuffleFontsInternalAsync(keepPortion: 0);
        }
        UpdateFontsSummary();
        _log?.LogInfo("Fonts", $"[MODE] changed to {_fontsMode}, beforeCount={beforeCount}, afterCount={_selectedFonts.Count}");
        UpdatePreview();
    }

    private async void ShuffleFonts_Click(object sender, RoutedEventArgs e)
    {
        await ShuffleFontsInternalAsync(keepPortion: 0.1);
        UpdateFontsSummary();
        UpdatePreview();
    }

    private void EditFonts_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new FontPickerDialog(_selectedFonts) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            _selectedFonts = dlg.SelectedFonts;
            UpdateFontsSummary();
            UpdatePreview();
        }
    }

    private async Task EnsureFontsReadyAsync()
    {
        try
        {
            FontService? svc = null;
            try
            {
                svc = _host.Services.GetRequiredService<FontService>();
            }
            catch (Exception ex)
            {
                _log.LogWarn("Fonts", $"FontService not available: {ex.Message}");
                return;
            }

            // 获取当前选择的操作系统（使用 OsCombo + GetSelectedTag）
            var osTag = GetSelectedTag(OsCombo) ?? "windows";
            var osName = osTag switch
            {
                "windows" => "Windows",
                "macos" => "macOS",
                "linux" => "Linux",
                "android" => "Android",
                "ios" => "iOS",
                _ => "Windows"
            };

            // 获取随机字体子集作为默认字体
            _log?.LogInfo("Fonts", $"[ENSURE] start os={osName}, beforeCount={_selectedFonts.Count}");
            var defaultFonts = await svc.RandomSubsetAsync(osName, 30);
            _selectedFonts = defaultFonts ?? new System.Collections.Generic.List<string>();
            var head = string.Join(", ", _selectedFonts.Take(6));
            _log?.LogInfo("Fonts", $"[ENSURE] done os={osName}, afterCount={_selectedFonts.Count}, head=[{head}{(_selectedFonts.Count>6?" …":"")}] ");
            UpdateFontsSummary();
            UpdatePreview();
        }
        catch (Exception ex)
        {
            _log.LogError("Fonts", $"EnsureFontsReadyAsync failed: {ex.Message}", ex.StackTrace);
        }
    }

    private void UpdateUaCustomVisibility()
    {
        if (CustomUaBox == null || UaModeCombo == null) return;
        
        var mode = GetSelectedTag(UaModeCombo) ?? "random";
        CustomUaBox.Visibility = string.Equals(mode, "custom", StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateButtonText()
    {
        if (CreateButton == null) return;
        CreateButton.Content = _isEditMode ? "保存" : "创建环境";
    }

    private void ProfileMode_Changed(object sender, RoutedEventArgs e)
    {
        // Guard against null controls during XAML initialization
        if (ExistingProfileLabel == null || ExistingProfileCombo == null || 
            PresetLabel == null || PresetCombo == null)
            return;

        bool isExisting = ProfileModeExisting.IsChecked == true;
        
        if (isExisting && ExistingProfileCombo.SelectedItem is FingerprintProfile profile)
        {
            var uaMode = GetSelectedTag(UaModeCombo) ?? "random";
            // 仅当选择 'profile' 模式时，顶部 Profile 面板展示 UA；否则不展示 UA，避免混淆
            var info = string.Empty;
            if (string.Equals(uaMode, "profile", StringComparison.OrdinalIgnoreCase))
            {
                info = $"UA: {profile.UserAgent}\n";
            }
            info += $"Locale: {profile.Locale}\n" +
                    $"Timezone: {profile.Timezone}\n" +
                    $"Updated: {profile.UpdatedAt:yyyy-MM-dd HH:mm}";
            ProfileInfoText.Text = info;
            var summary = new System.Text.StringBuilder();
            summary.AppendLine("=== 选择已有 Profile 预览 ===");
            summary.AppendLine($"名称: {profile.Name}");
            if (string.Equals(uaMode, "profile", StringComparison.OrdinalIgnoreCase))
                summary.AppendLine($"UA: {profile.UserAgent}");
            summary.AppendLine($"Locale: {profile.Locale}");
            summary.AppendLine($"Timezone: {profile.Timezone}");
            summary.AppendLine($"更新时间: {profile.UpdatedAt:yyyy-MM-dd HH:mm}");
            summary.AppendLine();
            summary.AppendLine("=== 启动参数预览 ===");
            summary.AppendLine($"浏览器: {GetSelectedTag(EngineCombo) ?? "chrome"}");
            summary.AppendLine($"操作系统: {GetSelectedTag(OsCombo) ?? "windows"}");
            // 已取 uaMode 上方
            summary.AppendLine($"UA模式: {uaMode}");
            // 仅展示实际生效的 UA
            if (string.Equals(uaMode, "profile", StringComparison.OrdinalIgnoreCase))
            {
                summary.AppendLine($"UA: {profile.UserAgent}");
            }
            else if (string.Equals(uaMode, "custom", StringComparison.OrdinalIgnoreCase))
            {
                summary.AppendLine($"UA: {CustomUaBox.Text}");
            }
            else
            {
                // random / match
                summary.AppendLine("UA: (将于运行时按所选模式生成)");
            }
            // 字体概要
            summary.AppendLine($"字体列表: {BuildFontsPreviewLine()}");
            summary.AppendLine($"浏览器: {GetSelectedTag(EngineCombo) ?? "chrome"}");
            summary.AppendLine($"操作系统: {GetSelectedTag(OsCombo) ?? "windows"}");
            summary.AppendLine($"代理: {GetSelectedTag(ProxyModeCombo) ?? "none"}");
            summary.AppendLine($"区域: {RegionBox.Text}");
            summary.AppendLine($"分辨率: {GetSelectedTag(ResolutionCombo) ?? "real"}");
            summary.AppendLine($"WebRTC: {GetSelectedTag(WebRtcCombo) ?? "hide"}");
            summary.AppendLine($"Canvas: {GetSelectedTag(CanvasCombo) ?? "noise"}");
            summary.AppendLine($"硬件并发: {SafeParseInt(HwBox.Text, 10)}");
            summary.AppendLine($"设备内存: {SafeParseInt(MemBox.Text, 8)}");
            // 新增：WebGL/WebGPU/Audio/Speech 预览
            summary.AppendLine($"WebGL 图像: {_webGLImageMode}");
            summary.AppendLine($"WebGL Info: {_webGLInfoMode}");
            var v = string.IsNullOrWhiteSpace(WebGLVendorBox.Text) ? "(未设置)" : WebGLVendorBox.Text;
            var r = string.IsNullOrWhiteSpace(WebGLRendererBox.Text) ? "(未设置)" : WebGLRendererBox.Text;
            summary.AppendLine($"WebGL Vendor/Renderer: {v} / {r}");
            summary.AppendLine($"WebGPU: {_webGpuMode}");
            summary.AppendLine($"AudioContext: {_audioContextMode}");
            summary.AppendLine($"SpeechVoices: {_speechVoicesEnabled}");
            PreviewBox.Text = summary.ToString();
            if (_log != null)
            {
                _log.LogInfo("EnvUI", "Preview updated (existing profile)");
                _log.LogInfo("Preview", "[PREVIEW]\n" + PreviewBox.Text);
            }
        }
        else if (!isExisting && PresetCombo.SelectedItem is TraitGroupPreset preset)
        {
            var info = $"预设: {preset.Name}\n" +
                      $"描述: {preset.Description ?? "无"}\n" +
                      $"创建时间: {preset.CreatedAt:yyyy-MM-dd HH:mm}";
            ProfileInfoText.Text = info;
            var summary = new System.Text.StringBuilder();
            summary.AppendLine("=== 从预设生成 预览 ===");
            summary.AppendLine($"预设: {preset.Name}");
            summary.AppendLine($"描述: {preset.Description ?? "无"}");
            summary.AppendLine();
            summary.AppendLine("=== 启动参数预览 ===");
            summary.AppendLine($"浏览器: {GetSelectedTag(EngineCombo) ?? "chrome"}");
            summary.AppendLine($"操作系统: {GetSelectedTag(OsCombo) ?? "windows"}");
            var uaMode = GetSelectedTag(UaModeCombo) ?? "random";
            summary.AppendLine($"UA模式: {uaMode}");
            if (string.Equals(uaMode, "custom", StringComparison.OrdinalIgnoreCase))
                summary.AppendLine($"自定义UA: {CustomUaBox.Text}");
            // 字体概要
            summary.AppendLine($"字体列表: {BuildFontsPreviewLine()}");
            summary.AppendLine($"浏览器: {GetSelectedTag(EngineCombo) ?? "chrome"}");
            summary.AppendLine($"操作系统: {GetSelectedTag(OsCombo) ?? "windows"}");
            summary.AppendLine($"代理: {GetSelectedTag(ProxyModeCombo) ?? "none"}");
            summary.AppendLine($"区域: {RegionBox.Text}");
            summary.AppendLine($"分辨率: {GetSelectedTag(ResolutionCombo) ?? "real"}");
            summary.AppendLine($"WebRTC: {GetSelectedTag(WebRtcCombo) ?? "hide"}");
            summary.AppendLine($"Canvas: {GetSelectedTag(CanvasCombo) ?? "noise"}");
            summary.AppendLine($"硬件并发: {SafeParseInt(HwBox.Text, 10)}");
            summary.AppendLine($"设备内存: {SafeParseInt(MemBox.Text, 8)}");
            // 新增：WebGL/WebGPU/Audio/Speech 预览
            summary.AppendLine($"WebGL 图像: {_webGLImageMode}");
            summary.AppendLine($"WebGL Info: {_webGLInfoMode}");
            var v2 = string.IsNullOrWhiteSpace(WebGLVendorBox.Text) ? "(未设置)" : WebGLVendorBox.Text;
            var r2 = string.IsNullOrWhiteSpace(WebGLRendererBox.Text) ? "(未设置)" : WebGLRendererBox.Text;
            summary.AppendLine($"WebGL Vendor/Renderer: {v2} / {r2}");
            summary.AppendLine($"WebGPU: {_webGpuMode}");
            summary.AppendLine($"AudioContext: {_audioContextMode}");
            summary.AppendLine($"SpeechVoices: {_speechVoicesEnabled}");
            PreviewBox.Text = summary.ToString();
            if (_log != null)
            {
                _log.LogInfo("EnvUI", "Preview updated (preset)");
                _log.LogInfo("Preview", "[PREVIEW]\n" + PreviewBox.Text);
            }
        }
        else
        {
            ProfileInfoText.Text = "请选择指纹配置";
            PreviewBox.Text = "=== 提示 ===\n请选择一个指纹配置或预设来查看预览。\n\n如果下拉框为空，请先在\"指纹配置\"页面创建 Profile 或预设。";
            if (_log != null)
            {
                _log.LogInfo("EnvUI", "Preview cleared (no selection)");
                _log.LogInfo("Preview", "[PREVIEW]\n" + PreviewBox.Text);
            }
        }
    }

    private void ExistingProfile_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_log != null) _log.LogInfo("EnvUI", "Existing profile selection changed");
        if (ExistingProfileCombo.SelectedItem is FingerprintProfile p)
        {
            // 保存选中的 Profile
            _selectedProfile = p;
            
            // 初始化阶段或抑制阶段、或与当前环境绑定的 Profile 相同 → 不提示、不覆盖
            if (_isInitializingUI || _suppressProfilePrompt || (_editingEnv?.FingerprintProfileId == p.Id))
            {
                _log?.LogInfo("EnvUI", "[PROFILE-APPLY] Skipped prompt (init/suppress/same profile)");
                UpdatePreview();
                return;
            }
            // 仅当用户打开下拉并选择时才提示；程序设置 SelectedItem 时不提示
            if (!ExistingProfileCombo.IsDropDownOpen)
            {
                _log?.LogInfo("EnvUI", "[PROFILE-APPLY] Skipped prompt (programmatic selection)");
                UpdatePreview();
                return;
            }
            // 询问是否用 Profile 覆盖当前界面值
            var confirm = MessageBox.Show(
                "将覆盖当前界面上的 WebGL 和字体设置，是否继续？",
                "覆盖确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );
            if (confirm != MessageBoxResult.Yes)
            {
                _log?.LogInfo("EnvUI", "[PROFILE-APPLY] User canceled overwrite; keep current UI values");
                UpdatePreview();
                return;
            }

            // 将 Profile 中的 WebGL Vendor/Renderer 显示到输入框
            WebGLVendorBox.Text = p.WebGLVendor ?? string.Empty;
            WebGLRendererBox.Text = p.WebGLRenderer ?? string.Empty;

            var fm = p.FontsMode ?? "real";
            _fontsMode = fm;
            SetComboTag(FontsModeCombo, fm);
            if (FontsRealBtn != null) FontsRealBtn.IsChecked = fm == "real";
            if (FontsCustomBtn != null) FontsCustomBtn.IsChecked = fm == "custom";
            if (EditFontsButton != null) EditFontsButton.IsEnabled = fm == "custom";
            if (ShuffleFontsButton != null) ShuffleFontsButton.IsEnabled = fm == "custom";
            try
            {
                _selectedFonts = string.IsNullOrWhiteSpace(p.FontsJson)
                    ? new System.Collections.Generic.List<string>()
                    : System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(p.FontsJson!) ?? new System.Collections.Generic.List<string>();
            }
            catch { _selectedFonts = new System.Collections.Generic.List<string>(); }
            UpdateFontsSummary();
        }
        else
        {
            // 无选择时重置为默认，避免沿用上一次的字体状态
            _fontsMode = "real";
            SetComboTag(FontsModeCombo, _fontsMode);
            if (FontsRealBtn != null) FontsRealBtn.IsChecked = true;
            if (FontsCustomBtn != null) FontsCustomBtn.IsChecked = false;
            if (EditFontsButton != null) EditFontsButton.IsEnabled = false;
            if (ShuffleFontsButton != null) ShuffleFontsButton.IsEnabled = false;
            _selectedFonts = new System.Collections.Generic.List<string>();
            UpdateFontsSummary();
        }
        UpdatePreview();
    }

    private void PresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_log != null) _log.LogInfo("EnvUI", "Preset selection changed");
        if (_isInitializingUI || _suppressProfilePrompt)
        {
            // 初始化阶段无需提示
            return;
        }
        if (PresetCombo.SelectedItem is TraitGroupPreset)
        {
            // 仅当用户打开下拉并选择时才提示；程序设置 SelectedItem 时不提示
            if (!PresetCombo.IsDropDownOpen)
            {
                UpdatePreview();
                return;
            }
            var confirm = MessageBox.Show(
                "选择预设可能会覆盖当前界面上的部分设置，是否继续？",
                "覆盖确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );
            if (confirm != MessageBoxResult.Yes)
            {
                _log?.LogInfo("EnvUI", "[PROFILE-APPLY] User canceled overwrite; keep current UI values");
                UpdatePreview();
                return;
            }
        }
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (ProfileInfoText == null || PreviewBox == null)
        {
            if (_log != null) _log.LogWarn("EnvUI", "Preview controls not ready");
            return;
        }

        bool isExisting = ProfileModeExisting.IsChecked == true;
        
        if (isExisting && ExistingProfileCombo.SelectedItem is FingerprintProfile profile)
        {
            var uaMode = GetSelectedTag(UaModeCombo) ?? "random";
            // 仅当选择 'profile' 模式时，顶部 Profile 面板展示 UA；否则不展示 UA，避免混淆
            var info = string.Empty;
            if (string.Equals(uaMode, "profile", StringComparison.OrdinalIgnoreCase))
            {
                info = $"UA: {profile.UserAgent}\n";
            }
            info += $"Locale: {profile.Locale}\n" +
                    $"Timezone: {profile.Timezone}\n" +
                    $"Updated: {profile.UpdatedAt:yyyy-MM-dd HH:mm}";
            ProfileInfoText.Text = info;
            var summary = new System.Text.StringBuilder();
            summary.AppendLine("=== 选择已有 Profile 预览 ===");
            summary.AppendLine($"名称: {profile.Name}");
            if (string.Equals(uaMode, "profile", StringComparison.OrdinalIgnoreCase))
                summary.AppendLine($"UA: {profile.UserAgent}");
            summary.AppendLine($"Locale: {profile.Locale}");
            summary.AppendLine($"Timezone: {profile.Timezone}");
            summary.AppendLine($"更新时间: {profile.UpdatedAt:yyyy-MM-dd HH:mm}");
            summary.AppendLine();
            summary.AppendLine("=== 启动参数预览 ===");
            summary.AppendLine($"浏览器: {GetSelectedTag(EngineCombo) ?? "chrome"}");
            summary.AppendLine($"操作系统: {GetSelectedTag(OsCombo) ?? "windows"}");
            // 已取 uaMode 上方
            summary.AppendLine($"UA模式: {uaMode}");
            // 仅展示实际生效的 UA
            if (string.Equals(uaMode, "profile", StringComparison.OrdinalIgnoreCase))
            {
                summary.AppendLine($"UA: {profile.UserAgent}");
            }
            else if (string.Equals(uaMode, "custom", StringComparison.OrdinalIgnoreCase))
            {
                summary.AppendLine($"UA: {CustomUaBox.Text}");
            }
            else
            {
                // random / match
                summary.AppendLine("UA: (将于运行时按所选模式生成)");
            }
            // 字体概要
            summary.AppendLine($"字体列表: {BuildFontsPreviewLine()}");
            summary.AppendLine($"浏览器: {GetSelectedTag(EngineCombo) ?? "chrome"}");
            summary.AppendLine($"操作系统: {GetSelectedTag(OsCombo) ?? "windows"}");
            summary.AppendLine($"代理: {GetSelectedTag(ProxyModeCombo) ?? "none"}");
            summary.AppendLine($"区域: {RegionBox.Text}");
            summary.AppendLine($"分辨率: {GetSelectedTag(ResolutionCombo) ?? "real"}");
            summary.AppendLine($"WebRTC: {GetSelectedTag(WebRtcCombo) ?? "hide"}");
            summary.AppendLine($"Canvas: {GetSelectedTag(CanvasCombo) ?? "noise"}");
            summary.AppendLine($"硬件并发: {SafeParseInt(HwBox.Text, 10)}");
            summary.AppendLine($"设备内存: {SafeParseInt(MemBox.Text, 8)}");
            // 新增：WebGL/WebGPU/Audio/Speech 预览
            summary.AppendLine($"WebGL 图像: {_webGLImageMode}");
            summary.AppendLine($"WebGL Info: {_webGLInfoMode}");
            var v = string.IsNullOrWhiteSpace(WebGLVendorBox.Text) ? "(未设置)" : WebGLVendorBox.Text;
            var r = string.IsNullOrWhiteSpace(WebGLRendererBox.Text) ? "(未设置)" : WebGLRendererBox.Text;
            summary.AppendLine($"WebGL Vendor/Renderer: {v} / {r}");
            summary.AppendLine($"WebGPU: {_webGpuMode}");
            summary.AppendLine($"AudioContext: {_audioContextMode}");
            summary.AppendLine($"SpeechVoices: {_speechVoicesEnabled}");
            PreviewBox.Text = summary.ToString();
            if (_log != null) _log.LogInfo("EnvUI", "Preview updated (existing profile)");
        }
        else if (!isExisting && PresetCombo.SelectedItem is TraitGroupPreset preset)
        {
            var info = $"预设: {preset.Name}\n" +
                      $"描述: {preset.Description ?? "无"}\n" +
                      $"创建时间: {preset.CreatedAt:yyyy-MM-dd HH:mm}";
            ProfileInfoText.Text = info;
            var summary = new System.Text.StringBuilder();
            summary.AppendLine("=== 从预设生成 预览 ===");
            summary.AppendLine($"预设: {preset.Name}");
            summary.AppendLine($"描述: {preset.Description ?? "无"}");
            summary.AppendLine();
            summary.AppendLine("=== 启动参数预览 ===");
            summary.AppendLine($"浏览器: {GetSelectedTag(EngineCombo) ?? "chrome"}");
            summary.AppendLine($"操作系统: {GetSelectedTag(OsCombo) ?? "windows"}");
            var uaMode = GetSelectedTag(UaModeCombo) ?? "random";
            summary.AppendLine($"UA模式: {uaMode}");
            if (string.Equals(uaMode, "custom", StringComparison.OrdinalIgnoreCase))
                summary.AppendLine($"自定义UA: {CustomUaBox.Text}");
            // 字体概要
            summary.AppendLine($"字体列表: {BuildFontsPreviewLine()}");
            summary.AppendLine($"浏览器: {GetSelectedTag(EngineCombo) ?? "chrome"}");
            summary.AppendLine($"操作系统: {GetSelectedTag(OsCombo) ?? "windows"}");
            summary.AppendLine($"代理: {GetSelectedTag(ProxyModeCombo) ?? "none"}");
            summary.AppendLine($"区域: {RegionBox.Text}");
            summary.AppendLine($"分辨率: {GetSelectedTag(ResolutionCombo) ?? "real"}");
            summary.AppendLine($"WebRTC: {GetSelectedTag(WebRtcCombo) ?? "hide"}");
            summary.AppendLine($"Canvas: {GetSelectedTag(CanvasCombo) ?? "noise"}");
            summary.AppendLine($"硬件并发: {SafeParseInt(HwBox.Text, 10)}");
            summary.AppendLine($"设备内存: {SafeParseInt(MemBox.Text, 8)}");
            // 新增：WebGL/WebGPU/Audio/Speech 预览
            summary.AppendLine($"WebGL 图像: {_webGLImageMode}");
            summary.AppendLine($"WebGL Info: {_webGLInfoMode}");
            var v2 = string.IsNullOrWhiteSpace(WebGLVendorBox.Text) ? "(未设置)" : WebGLVendorBox.Text;
            var r2 = string.IsNullOrWhiteSpace(WebGLRendererBox.Text) ? "(未设置)" : WebGLRendererBox.Text;
            summary.AppendLine($"WebGL Vendor/Renderer: {v2} / {r2}");
            summary.AppendLine($"WebGPU: {_webGpuMode}");
            summary.AppendLine($"AudioContext: {_audioContextMode}");
            summary.AppendLine($"SpeechVoices: {_speechVoicesEnabled}");
            PreviewBox.Text = summary.ToString();
            if (_log != null) _log.LogInfo("EnvUI", "Preview updated (preset)");
        }
        else
        {
            ProfileInfoText.Text = "请选择指纹配置";
            PreviewBox.Text = "=== 提示 ===\n请选择一个指纹配置或预设来查看预览。\n\n如果下拉框为空，请先在\"指纹配置\"页面创建 Profile 或预设。";
            if (_log != null) _log.LogInfo("EnvUI", "Preview cleared (no selection)");
        }
    }

    private void UpdateFontsSummary()
    {
        if (FontsSummaryText == null) return;
        FontsSummaryText.Text = BuildFontsPreviewLine();
        _log?.LogInfo("Fonts", $"[SUMMARY] {FontsSummaryText.Text}");
    }

    private string BuildFontsPreviewLine()
    {
        if (!string.Equals(_fontsMode, "custom", StringComparison.OrdinalIgnoreCase))
            return "真实（系统真实字体）";
        var n = _selectedFonts.Count;
        if (n == 0) return "自定义（未选择）";
        var head = string.Join(", ", _selectedFonts.Take(6));
        return $"自定义（{n}）：{head}{(n > 6 ? " …" : string.Empty)}";
    }

    private async System.Threading.Tasks.Task ShuffleFontsInternalAsync(double keepPortion)
    {
        try
        {
            var beforeCount = _selectedFonts.Count;
            var beforeHead = string.Join(", ", _selectedFonts.Take(6));
            var osTag = GetSelectedTag(OsCombo) ?? "windows";
            var osName = osTag switch
            {
                "windows" => "Windows",
                "macos" => "macOS",
                "linux" => "Linux",
                "android" => "Android",
                "ios" => "iOS",
                _ => "Windows"
            };
            FontService? svc = null;
            try
            {
                svc = _host.Services.GetRequiredService<FontService>();
            }
            catch (Exception ex)
            {
                _log.LogWarn("Fonts", $"FontService not available: {ex.Message}");
                return;
            }
            var sample = await svc.RandomSubsetAsync(osName, 30);
            if (keepPortion > 0 && _selectedFonts.Count > 0)
            {
                var keep = (int)Math.Round(_selectedFonts.Count * keepPortion);
                var kept = _selectedFonts.Take(Math.Max(0, keep)).ToList();
                // 合并去重
                var set = new System.Collections.Generic.HashSet<string>(kept);
                foreach (var f in sample)
                    if (set.Add(f)) kept.Add(f);
                _selectedFonts = kept;
            }
            else
            {
                _selectedFonts = sample;
            }
            var afterHead = string.Join(", ", _selectedFonts.Take(6));
            _log?.LogInfo("Fonts", $"[SHUFFLE] os={osName}, keep={keepPortion:P0}, before={beforeCount}:[{beforeHead}{(beforeCount>6?" …":"")}], sample={sample?.Count ?? 0}, after={_selectedFonts.Count}:[{afterHead}{(_selectedFonts.Count>6?" …":"")}]");
        }
        catch (Exception ex)
        {
            _log.LogWarn("Fonts", $"Shuffle fonts failed: {ex.Message}");
        }
    }

    private void Generic_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender == OsCombo)
        {
            ApplyOsDefaults();
        }

        if (_log != null) _log.LogInfo("EnvUI", "Generic selection changed");
        UpdatePreview();
    }

    private void Generic_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_log == null) return;
        _log.LogInfo("EnvUI", "Generic text changed");
        UpdatePreview();
    }

    private void ApplyOsDefaults()
    {
        if (UaModeCombo == null || CustomUaBox == null || OsCombo == null)
        {
            // Controls not ready yet; skip applying defaults
            if (_log != null) _log.LogWarn("EnvUI", "ApplyOsDefaults skipped (controls not ready)");
            return;
        }
        var os = GetSelectedTag(OsCombo) ?? "windows";
        // 预置常见 UA（保持可维护的少量样例）
        string uaWindows = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";
        string uaMac = "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_4) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Safari/605.1.15";
        string uaLinux = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";
        string uaAndroid = "Mozilla/5.0 (Linux; Android 13; Pixel 7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Mobile Safari/537.36";
        string uaiOS = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Mobile/15E148 Safari/604.1";

        // 对非 Windows 平台，默认使用自定义 UA，更直观；Windows 继续保持随机
        switch (os)
        {
            case "windows":
                SetComboTag(UaModeCombo, "random");
                CustomUaBox.Text = uaWindows;
                break;
            case "macos":
                SetComboTag(UaModeCombo, "custom");
                CustomUaBox.Text = uaMac;
                break;
            case "linux":
                SetComboTag(UaModeCombo, "custom");
                CustomUaBox.Text = uaLinux;
                break;
            case "android":
                SetComboTag(UaModeCombo, "custom");
                CustomUaBox.Text = uaAndroid;
                break;
            case "ios":
                SetComboTag(UaModeCombo, "custom");
                CustomUaBox.Text = uaiOS;
                break;
        }

        // 切换为自定义时确保文本框可见
        UpdateUaCustomVisibility();
    }

    private async void Create_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_isEditMode && _editingEnv != null)
            {
                // 编辑模式：更新现有环境
                _editingEnv.Name = string.IsNullOrWhiteSpace(NameBox.Text) ? _editingEnv.Name : NameBox.Text.Trim();
                _editingEnv.Engine = GetSelectedTag(EngineCombo) ?? "chrome";
                _editingEnv.EngineMode = (EngineModeUndetectedBtn?.IsChecked == true) ? "undetected_chrome" : "playwright";
                _editingEnv.OS = GetSelectedTag(OsCombo) ?? "windows";
                _editingEnv.UaMode = GetSelectedTag(UaModeCombo) ?? "random";
                _editingEnv.CustomUserAgent = string.IsNullOrWhiteSpace(CustomUaBox.Text) ? null : CustomUaBox.Text.Trim();
                _editingEnv.ProxyMode = GetSelectedTag(ProxyModeCombo) ?? "none";
                _editingEnv.ProxyType = ProxyTargetPoolBtn?.IsChecked == true ? "pool" : "server";
                if (string.Equals(_editingEnv.ProxyMode, "reference", StringComparison.OrdinalIgnoreCase))
                {
                    if (_editingEnv.ProxyType == "pool")
                    {
                        if (ExistingPoolCombo?.SelectedValue is int poolId)
                            _editingEnv.ProxyRefId = poolId;
                        else
                            _editingEnv.ProxyRefId = null;
                    }
                    else
                    {
                        if (ExistingProxyCombo?.SelectedValue is int pid)
                            _editingEnv.ProxyRefId = pid;
                        else
                            _editingEnv.ProxyRefId = null;
                    }
                }
                else
                {
                    _editingEnv.ProxyRefId = null;
                    _editingEnv.ProxyType = null;
                }
                _editingEnv.Region = string.IsNullOrWhiteSpace(RegionBox.Text) ? null : RegionBox.Text.Trim();
                _editingEnv.DeviceClass = string.IsNullOrWhiteSpace(DeviceClassBox.Text) ? null : DeviceClassBox.Text.Trim();
                _editingEnv.Vendor = string.IsNullOrWhiteSpace(VendorBox.Text) ? null : VendorBox.Text.Trim();
                _editingEnv.ResolutionMode = GetSelectedTag(ResolutionCombo) ?? "real";
                
                // 自定义分辨率覆盖 Profile 的分辨率
                if (_editingEnv.ResolutionMode == "custom")
                {
                    if (int.TryParse(ResolutionWidthBox.Text, out int customWidth) && customWidth > 0)
                        _editingEnv.CustomViewportWidth = customWidth;
                    if (int.TryParse(ResolutionHeightBox.Text, out int customHeight) && customHeight > 0)
                        _editingEnv.CustomViewportHeight = customHeight;
                }
                _editingEnv.WebRtcMode = GetSelectedTag(WebRtcCombo) ?? "hide";
                _editingEnv.CanvasMode = GetSelectedTag(CanvasCombo) ?? "noise";
                _editingEnv.WebdriverMode = GetSelectedTag(WebdriverCombo) ?? "undefined";
                _editingEnv.HardwareConcurrency = SafeParseInt(HwBox.Text, 10);
                _editingEnv.DeviceMemory = SafeParseInt(MemBox.Text, 8);
                _editingEnv.FontsListMode = _fontsMode;
                _editingEnv.FontsFingerprintMode = _fontsMode == "custom" ? "noise" : "real";
                _editingEnv.FontsMode = _fontsMode;
                _editingEnv.FontsJson = string.Equals(_fontsMode, "custom", StringComparison.OrdinalIgnoreCase) 
                    ? System.Text.Json.JsonSerializer.Serialize(_selectedFonts ?? new System.Collections.Generic.List<string>())
                    : null;
                // WebGL / WebGPU / Audio / Speech
                _editingEnv.WebGLImageMode = _webGLImageMode;
                _editingEnv.WebGLInfoMode = _webGLInfoMode;
                _editingEnv.WebGLVendor = string.IsNullOrWhiteSpace(WebGLVendorBox.Text) ? null : WebGLVendorBox.Text.Trim();
                _editingEnv.WebGLRenderer = string.IsNullOrWhiteSpace(WebGLRendererBox.Text) ? null : WebGLRendererBox.Text.Trim();
                _editingEnv.WebGpuMode = _webGpuMode;
                _editingEnv.AudioContextMode = _audioContextMode;
                _editingEnv.SpeechVoicesEnabled = _speechVoicesEnabled;
                
                // 保存前确保字体就绪（custom 但未填充时）
                if (string.Equals(_fontsMode, "custom", StringComparison.OrdinalIgnoreCase) && _selectedFonts.Count == 0)
                {
                    await EnsureFontsReadyAsync();
                }

                // 同步当前界面 WebGL / Fonts 到已绑定的 FingerprintProfile（如果有）
                try
                {
                    if (_editingEnv.FingerprintProfileId.HasValue)
                    {
                        var prof = _db.FingerprintProfiles.FirstOrDefault(x => x.Id == _editingEnv.FingerprintProfileId.Value);
                        if (prof != null)
                        {
                            var newVendor = string.IsNullOrWhiteSpace(WebGLVendorBox.Text) ? null : WebGLVendorBox.Text.Trim();
                            var newRenderer = string.IsNullOrWhiteSpace(WebGLRendererBox.Text) ? null : WebGLRendererBox.Text.Trim();
                            if (!string.Equals(prof.WebGLVendor, newVendor, StringComparison.Ordinal)) prof.WebGLVendor = newVendor;
                            if (!string.Equals(prof.WebGLRenderer, newRenderer, StringComparison.Ordinal)) prof.WebGLRenderer = newRenderer;
                            // Fonts
                            prof.FontsMode = _fontsMode;
                            if (string.Equals(_fontsMode, "custom", StringComparison.OrdinalIgnoreCase))
                            {
                                prof.FontsJson = System.Text.Json.JsonSerializer.Serialize(_selectedFonts ?? new System.Collections.Generic.List<string>());
                            }
                            else
                            {
                                prof.FontsJson = null;
                            }
                            _db.SaveChanges();
                            _log.LogInfo("EnvUI", $"Persisted WebGL/Fonts to profile {_editingEnv.FingerprintProfileId}: {prof.WebGLVendor} / {prof.WebGLRenderer}, FontsMode={prof.FontsMode}, FontsJsonCount={(prof.FontsJson==null?0:(_selectedFonts?.Count ?? 0))}");
                        }
                    }
                }
                catch (Exception pex)
                {
                    _log.LogWarn("EnvUI", $"Persist profile WebGL failed: {pex.Message}");
                }

                // 直接保存到数据库，确保所有字段持久化
                _log.LogInfo("EnvUI", $"[SAVE-BEFORE] FontsMode={_editingEnv.FontsMode}, FontsJson={(_editingEnv.FontsJson?.Length ?? 0)} chars, WebGLImageMode={_editingEnv.WebGLImageMode}, WebGLInfoMode={_editingEnv.WebGLInfoMode}, WebGLVendor={_editingEnv.WebGLVendor}, WebGLRenderer={_editingEnv.WebGLRenderer}, WebGpuMode={_editingEnv.WebGpuMode}, AudioContextMode={_editingEnv.AudioContextMode}, SpeechVoicesEnabled={_editingEnv.SpeechVoicesEnabled}");
                _db.SaveChanges();
                StatusText.Text = $"已更新环境：{_editingEnv.Name}";
                _log.LogInfo("EnvUI", $"[SAVE-AFTER] Environment saved: {_editingEnv.Name}, FontsMode={_editingEnv.FontsMode}, WebGL={_editingEnv.WebGLVendor}/{_editingEnv.WebGLRenderer}");
                MessageBox.Show("更新成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                // 创建模式：创建新环境
                bool useExistingProfile = ProfileModeExisting.IsChecked == true;
                
                if (useExistingProfile)
                {
                    // 使用已有 Profile
                    if (ExistingProfileCombo.SelectedItem is not FingerprintProfile selectedProfile)
                    {
                        MessageBox.Show("请选择一个指纹配置", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 保存前确保字体就绪（custom 但未填充时）
                    if (string.Equals(_fontsMode, "custom", StringComparison.OrdinalIgnoreCase) && _selectedFonts.Count == 0)
                    {
                        await EnsureFontsReadyAsync();
                    }

                    // 将当前 WebGL / Fonts 持久化到所选 Profile（如果有）
                    if (selectedProfile != null)
                    {
                        selectedProfile.WebGLVendor = string.IsNullOrWhiteSpace(WebGLVendorBox.Text) ? selectedProfile.WebGLVendor : WebGLVendorBox.Text.Trim();
                        selectedProfile.WebGLRenderer = string.IsNullOrWhiteSpace(WebGLRendererBox.Text) ? selectedProfile.WebGLRenderer : WebGLRendererBox.Text.Trim();
                        // Fonts
                        selectedProfile.FontsMode = _fontsMode;
                        if (string.Equals(_fontsMode, "custom", StringComparison.OrdinalIgnoreCase))
                        {
                            selectedProfile.FontsJson = System.Text.Json.JsonSerializer.Serialize(_selectedFonts ?? new System.Collections.Generic.List<string>());
                        }
                        else
                        {
                            selectedProfile.FontsJson = null;
                        }
                        _db.SaveChanges();
                        _log.LogInfo("EnvUI", $"Persisted WebGL/Fonts to profile {selectedProfile.Name}: {selectedProfile.WebGLVendor} / {selectedProfile.WebGLRenderer}, FontsMode={selectedProfile.FontsMode}, FontsJsonCount={(selectedProfile.FontsJson==null?0:(_selectedFonts?.Count ?? 0))}");
                    }

                    var env = new BrowserEnvironment
                    {
                        Name = string.IsNullOrWhiteSpace(NameBox.Text) ? $"Env-{DateTime.Now:HHmmss}" : NameBox.Text.Trim(),
                        Engine = GetSelectedTag(EngineCombo) ?? "chrome",
                        EngineMode = (EngineModeUndetectedBtn?.IsChecked == true) ? "undetected_chrome" : "playwright",
                        OS = GetSelectedTag(OsCombo) ?? "windows",
                        UaMode = GetSelectedTag(UaModeCombo) ?? "random",
                        CustomUserAgent = string.IsNullOrWhiteSpace(CustomUaBox.Text) ? null : CustomUaBox.Text.Trim(),
                        ProxyMode = GetSelectedTag(ProxyModeCombo) ?? "none",
                        ProxyType = (ProxyTargetPoolBtn?.IsChecked == true) ? "pool" : "server",
                        ProxyRefId = (string.Equals((GetSelectedTag(ProxyModeCombo) ?? "none"), "reference", StringComparison.OrdinalIgnoreCase))
                            ? ((ProxyTargetPoolBtn?.IsChecked == true)
                                ? (ExistingPoolCombo?.SelectedValue as int?)
                                : (ExistingProxyCombo?.SelectedValue as int?))
                            : null,
                        Region = string.IsNullOrWhiteSpace(RegionBox.Text) ? null : RegionBox.Text.Trim(),
                        DeviceClass = string.IsNullOrWhiteSpace(DeviceClassBox.Text) ? null : DeviceClassBox.Text.Trim(),
                        Vendor = string.IsNullOrWhiteSpace(VendorBox.Text) ? null : VendorBox.Text.Trim(),
                        ResolutionMode = GetSelectedTag(ResolutionCombo) ?? "real",
                        // 自定义分辨率覆盖 Profile 的分辨率
                        CustomViewportWidth = (GetSelectedTag(ResolutionCombo) == "custom" && int.TryParse(ResolutionWidthBox.Text, out int w) && w > 0) ? w : null,
                        CustomViewportHeight = (GetSelectedTag(ResolutionCombo) == "custom" && int.TryParse(ResolutionHeightBox.Text, out int h) && h > 0) ? h : null,
                        WebRtcMode = GetSelectedTag(WebRtcCombo) ?? "hide",
                        CanvasMode = GetSelectedTag(CanvasCombo) ?? "noise",
                        WebdriverMode = GetSelectedTag(WebdriverCombo) ?? "undefined",
                        HardwareConcurrency = SafeParseInt(HwBox.Text, 10),
                        DeviceMemory = SafeParseInt(MemBox.Text, 8),
                        FontsListMode = _fontsMode,
                        FontsFingerprintMode = _fontsMode == "custom" ? "noise" : "real",
                        FontsMode = _fontsMode,
                        FontsJson = string.Equals(_fontsMode, "custom", StringComparison.OrdinalIgnoreCase) 
                            ? System.Text.Json.JsonSerializer.Serialize(_selectedFonts ?? new System.Collections.Generic.List<string>())
                            : null,
                        // WebGL / WebGPU / Audio / Speech
                        WebGLImageMode = _webGLImageMode,
                        WebGLInfoMode = _webGLInfoMode,
                        WebGLVendor = string.IsNullOrWhiteSpace(WebGLVendorBox.Text) ? null : WebGLVendorBox.Text.Trim(),
                        WebGLRenderer = string.IsNullOrWhiteSpace(WebGLRendererBox.Text) ? null : WebGLRendererBox.Text.Trim(),
                        WebGpuMode = _webGpuMode,
                        AudioContextMode = _audioContextMode,
                        SpeechVoicesEnabled = _speechVoicesEnabled,
                        FingerprintProfileId = selectedProfile.Id
                    };
                    _log.LogInfo("EnvUI", $"[CREATE-BEFORE] FontsMode={env.FontsMode}, FontsJson={env.FontsJson?.Length ?? 0} chars, WebGLImageMode={env.WebGLImageMode}, WebGLInfoMode={env.WebGLInfoMode}, WebGLVendor={env.WebGLVendor}, WebGLRenderer={env.WebGLRenderer}, WebGpuMode={env.WebGpuMode}, AudioContextMode={env.AudioContextMode}, SpeechVoicesEnabled={env.SpeechVoicesEnabled}");
                    _db.BrowserEnvironments.Add(env);
                    _db.SaveChanges();
                    _createdEnv = env;
                    StatusText.Text = $"已创建环境：{env.Name}（绑定 Profile: {selectedProfile.Name}）";
                    _log.LogInfo("EnvUI", $"[CREATE-AFTER] Created environment {env.Name} with existing profile {selectedProfile.Name}, FontsMode={env.FontsMode}, WebGL={env.WebGLVendor}/{env.WebGLRenderer}");

                    LoadProfilesAndPresets();
                    UpdatePreview();

                    MessageBox.Show("创建成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    // 从预设生成 Profile（使用 BuildRandomDraft + BuildProfileFromDraft）
                    _log.LogInfo("EnvUI", "Creating new environment with generated profile from random draft");
                    
                    // 1. 构建随机草稿
                    var opts = new BrowserEnvironmentService.RandomizeOptions();
                    var draft = _envSvc.BuildRandomDraft(opts);
                    
                    // 2. 从草稿生成 Profile（会自动生成防检测数据）
                    var newProfile = _envSvc.BuildProfileFromDraft(draft);
                    newProfile.Name = $"Profile-{DateTime.Now:yyMMdd-HHmmss}";
                    
                    // 2.5. 校验并修复一致性
                    var antiDetectionSvc = _host.Services.GetRequiredService<AntiDetectionService>();
                    var validationErrors = antiDetectionSvc.ValidateProfile(newProfile);
                    if (validationErrors.Count > 0)
                    {
                        _log.LogWarn("EnvUI", $"Profile validation warnings: {string.Join("; ", validationErrors)}");
                    }
                    
                    // 3. 保存 Profile 到数据库
                    _db.FingerprintProfiles.Add(newProfile);
                    _db.SaveChanges();
                    _log.LogInfo("EnvUI", $"Created new profile: {newProfile.Name} with anti-detection data (validated and fixed)");
                    
                    // 4. 创建环境并绑定新 Profile
                    var env = new BrowserEnvironment
                    {
                        Name = string.IsNullOrWhiteSpace(NameBox.Text) ? $"Env-{DateTime.Now:HHmmss}" : NameBox.Text.Trim(),
                        Engine = draft.Engine,
                        OS = draft.OS,
                        UaMode = draft.UaMode,
                        CustomUserAgent = draft.CustomUserAgent,
                        ProxyMode = draft.ProxyMode,
                        ProxyType = draft.ProxyType,
                        ProxyRefId = draft.ProxyRefId,
                        Region = draft.Region,
                        DeviceClass = draft.DeviceClass,
                        Vendor = draft.Vendor,
                        ResolutionMode = draft.ResolutionMode,
                        CustomViewportWidth = draft.CustomViewportWidth,
                        CustomViewportHeight = draft.CustomViewportHeight,
                        WebRtcMode = draft.WebRtcMode,
                        CanvasMode = draft.CanvasMode,
                        HardwareConcurrency = draft.HardwareConcurrency,
                        DeviceMemory = draft.DeviceMemory,
                        FontsListMode = draft.FontsListMode,
                        FontsFingerprintMode = draft.FontsFingerprintMode,
                        FontsMode = draft.FontsMode,
                        FontsJson = draft.FontsJson,
                        WebGLImageMode = draft.WebGLImageMode,
                        WebGLInfoMode = draft.WebGLInfoMode,
                        WebGLVendor = draft.WebGLVendor,
                        WebGLRenderer = draft.WebGLRenderer,
                        WebGpuMode = draft.WebGpuMode,
                        AudioContextMode = draft.AudioContextMode,
                        SpeechVoicesEnabled = draft.SpeechVoicesEnabled,
                        FingerprintProfileId = newProfile.Id
                    };
                    
                    _db.BrowserEnvironments.Add(env);
                    _db.SaveChanges();
                    _createdEnv = env;
                    
                    StatusText.Text = $"已创建环境：{env.Name}（新 Profile: {newProfile.Name}）";
                    _log.LogInfo("EnvUI", $"Created environment {env.Name} with NEW profile {newProfile.Name} (anti-detection data included)");
                    
                    LoadProfilesAndPresets();
                    UpdatePreview();
                    
                    MessageBox.Show($"创建成功！\n环境：{env.Name}\n新 Profile：{newProfile.Name}\n✅ 已生成防检测数据", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
            }
        }
        catch (Exception ex)
        {
            _log.LogError("Env", $"Create/Update environment failed: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => this.Close();

    private void LoadExistingData(BrowserEnvironment env)
    {
        _isInitializingUI = true;
        _suppressProfilePrompt = true;
        _log.LogInfo("EnvUI", $"[LOAD-START] Loading environment: {env.Name}, ID={env.Id}");
        NameBox.Text = env.Name;
        SetComboTag(EngineCombo, env.Engine);
        SetComboTag(OsCombo, env.OS);
        SetComboTag(UaModeCombo, env.UaMode);
        CustomUaBox.Text = env.CustomUserAgent ?? string.Empty;
        SetComboTag(ProxyModeCombo, env.ProxyMode);
        RegionBox.Text = env.Region ?? string.Empty;
        DeviceClassBox.Text = env.DeviceClass ?? string.Empty;
        VendorBox.Text = env.Vendor ?? string.Empty;
        SetComboTag(ResolutionCombo, env.ResolutionMode);
        SetComboTag(WebRtcCombo, env.WebRtcMode);
        SetComboTag(CanvasCombo, env.CanvasMode);
        HwBox.Text = env.HardwareConcurrency.ToString();
        MemBox.Text = env.DeviceMemory.ToString();
        PreviewBox.Text = env.PreviewJson ?? "{}";
        
        // 从环境直接加载 WebGL 和字体（先赋值变量，后面 InitializeToggleButtons 会用到）
        _log.LogInfo("EnvUI", $"[LOAD-WEBGL] WebGLVendor={env.WebGLVendor}, WebGLRenderer={env.WebGLRenderer}, WebGLImageMode={env.WebGLImageMode}, WebGLInfoMode={env.WebGLInfoMode}");
        WebGLVendorBox.Text = env.WebGLVendor ?? string.Empty;
        WebGLRendererBox.Text = env.WebGLRenderer ?? string.Empty;
        // 先赋值这些变量，InitializeToggleButtons 会用到
        _webGLImageMode = env.WebGLImageMode ?? "noise";
        _webGLInfoMode = env.WebGLInfoMode ?? "ua_based";
        _webGpuMode = env.WebGpuMode ?? "match_webgl";
        _audioContextMode = env.AudioContextMode ?? "noise";
        _speechVoicesEnabled = env.SpeechVoicesEnabled;
        _log.LogInfo("EnvUI", $"[LOAD-MODES-ASSIGNED] _webGLImageMode={_webGLImageMode}, _webGLInfoMode={_webGLInfoMode}, _webGpuMode={_webGpuMode}, _audioContextMode={_audioContextMode}, _speechVoicesEnabled={_speechVoicesEnabled}");
        
        // 从环境加载字体
        _log.LogInfo("EnvUI", $"[LOAD-FONTS] FontsMode={env.FontsMode}, FontsJson length={env.FontsJson?.Length ?? 0}");
        _fontsMode = env.FontsMode ?? "real";
        SetComboTag(FontsModeCombo, _fontsMode);
        FontsRealBtn.IsChecked = _fontsMode == "real";
        FontsCustomBtn.IsChecked = _fontsMode == "custom";
        EditFontsButton.IsEnabled = _fontsMode == "custom";
        ShuffleFontsButton.IsEnabled = _fontsMode == "custom";
        try
        {
            _selectedFonts = string.IsNullOrWhiteSpace(env.FontsJson)
                ? new System.Collections.Generic.List<string>()
                : System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(env.FontsJson!) ?? new System.Collections.Generic.List<string>();
            _log.LogInfo("EnvUI", $"[LOAD-FONTS-RESULT] Deserialized {_selectedFonts.Count} fonts");
        }
        catch (Exception fx) 
        { 
            _log.LogWarn("EnvUI", $"[LOAD-FONTS-ERROR] Failed to deserialize fonts: {fx.Message}");
            _selectedFonts = new System.Collections.Generic.List<string>(); 
        }
        UpdateFontsSummary();
        
        // 同步 UI 按钮状态（WebGL/WebGPU/Audio/Speech modes）
        InitializeToggleButtons();
        _log.LogInfo("EnvUI", $"[LOAD-END] Environment loaded successfully, UI buttons initialized");

        // 强制刷新 UI
        this.UpdateLayout();
        //System.Windows.Forms.Application.DoEvents(); // Force UI refresh

        // 重要：加载环境后不再自动用 Profile 覆盖界面值，优先以环境与界面为准
        // 如需使用 Profile 值，应在切换 Profile/预设时经用户确认后再应用
        //    }
        //}
        //catch (Exception ex)
        //{
        //    _log.LogWarn("EnvUI", $"LoadExistingData profile sync failed: {ex.Message}");
        //}
        // 同步完成后刷新预览，避免显示上一个环境的缓存内容
        UpdatePreview();
    }

    private void SetComboTag(ComboBox combo, string tag)
    {
        if (combo == null) return;
        if (combo.Items == null || combo.Items.Count == 0) return;

        foreach (var obj in combo.Items)
        {
            if (obj is ComboBoxItem item)
            {
                if (string.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
        }
    }

    private async void LaunchBrowser_Click(object sender, RoutedEventArgs e)
    {
        if (_createdEnv == null || _createdEnv.FingerprintProfileId == null)
        {
            MessageBox.Show("未找到已创建的环境或指纹配置。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            LaunchButton.IsEnabled = false;
            StatusText.Text = "正在启动浏览器...";

            // 重新从 DB 加载以确保包含完整数据
            var profile = _db.FingerprintProfiles.FirstOrDefault(p => p.Id == _createdEnv.FingerprintProfileId.Value);
            if (profile == null)
            {
                MessageBox.Show("未找到指纹配置。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var fingerprintSvc = _host.Services.GetRequiredService<FingerprintService>();
            var logSvc = _host.Services.GetRequiredService<LogService>();
            var secretSvc = _host.Services.GetRequiredService<SecretService>();
            var controller = new FishBrowser.WPF.Engine.PlaywrightController(logSvc, fingerprintSvc, secretSvc);

            // 解析代理：支持 ProxyType=server/pool
            ProxyServer? launchProxy = null;
            if (string.Equals(_createdEnv.ProxyMode, "reference", StringComparison.OrdinalIgnoreCase) && _createdEnv.ProxyRefId.HasValue)
            {
                if (string.Equals(_createdEnv.ProxyType, "server", StringComparison.OrdinalIgnoreCase))
                {
                    launchProxy = _proxyCatalog.GetById(_createdEnv.ProxyRefId.Value);
                }
                else if (string.Equals(_createdEnv.ProxyType, "pool", StringComparison.OrdinalIgnoreCase))
                {
                    var pool = _proxyCatalog.GetPools().FirstOrDefault(p => p.Id == _createdEnv.ProxyRefId.Value);
                    var strategy = pool?.Strategy ?? "random";
                    launchProxy = _proxyCatalog.PickProxyFromPool(_createdEnv.ProxyRefId.Value, strategy);
                    _log?.LogInfo("EnvUI", $"Picked proxy from pool {_createdEnv.ProxyRefId} with strategy {strategy}: {(launchProxy != null ? launchProxy.Address+":"+launchProxy.Port : "null")}" );
                }
            }

            await controller.InitializeBrowserAsync(profile, proxy: launchProxy, headless: false);
            StatusText.Text = "浏览器已启动，正在打开测试页面...";

            // 打开测试页面验证指纹
            var html = await controller.NavigateAsync("https://httpbin.org/headers");

            // 执行 JS 获取 navigator 信息
            var jsResult = await controller.EvaluateAsync(@"({
                userAgent: navigator.userAgent,
                platform: navigator.platform,
                language: navigator.language,
                languages: navigator.languages,
                hardwareConcurrency: navigator.hardwareConcurrency,
                deviceMemory: navigator.deviceMemory,
                webdriver: navigator.webdriver
            })");

            var info = System.Text.Json.JsonSerializer.Serialize(jsResult, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            PreviewBox.Text += "\n\n=== 浏览器验证结果 ===\n" + info;
            StatusText.Text = "验证完成！查看预览窗口。";

            await controller.DisposeAsync();
            LaunchButton.IsEnabled = true;
            MessageBox.Show("浏览器测试成功！查看右侧预览窗口的验证结果。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _log.LogError("EnvTest", $"Launch browser failed: {ex.Message}", ex.StackTrace);
            StatusText.Text = $"启动失败: {ex.Message}";
            LaunchButton.IsEnabled = true;
            MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string? GetSelectedTag(ComboBox combo)
    {
        if (combo.SelectedItem is ComboBoxItem item)
            return item.Tag?.ToString();
        return null;
    }

    private static int SafeParseInt(string? text, int fallback)
    {
        if (int.TryParse(text?.Trim(), out var v)) return v;
        return fallback;
    }

    // WebGL / WebGPU / Audio / Speech 事件处理
    private void WebGLImageMode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Primitives.ToggleButton btn && btn.Tag is string tag)
        {
            _webGLImageMode = tag;
            // 更新所有按钮的选中状态
            WebGLImageNoiseBtn.IsChecked = (tag == "noise");
            WebGLImageRealBtn.IsChecked = (tag == "real");
            _log.LogInfo("EnvUI", $"WebGL Image Mode changed to: {tag}");
            UpdatePreview();
        }
    }

    private void WebGLInfoMode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Primitives.ToggleButton btn && btn.Tag is string tag)
        {
            _webGLInfoMode = tag;
            // 更新所有按钮的选中状态
            WebGLInfoUaBtn.IsChecked = (tag == "ua_based");
            WebGLInfoCustomBtn.IsChecked = (tag == "custom");
            WebGLInfoDisableBtn.IsChecked = (tag == "disable_hwaccel");
            WebGLInfoRealBtn.IsChecked = (tag == "real");
            _log.LogInfo("EnvUI", $"WebGL Info Mode changed to: {tag}");
            UpdatePreview();
        }
    }

    private void WebGPUMode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Primitives.ToggleButton btn && btn.Tag is string tag)
        {
            _webGpuMode = tag;
            // 更新所有按钮的选中状态
            WebGpuMatchBtn.IsChecked = (tag == "match_webgl");
            WebGpuRealBtn.IsChecked = (tag == "real");
            WebGpuDisableBtn.IsChecked = (tag == "disable");
            _log.LogInfo("EnvUI", $"WebGPU Mode changed to: {tag}");
            UpdatePreview();
        }
    }

    private void AudioContextMode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Primitives.ToggleButton btn && btn.Tag is string tag)
        {
            _audioContextMode = tag;
            // 更新所有按钮的选中状态
            AudioNoiseBtn.IsChecked = (tag == "noise");
            AudioRealBtn.IsChecked = (tag == "real");
            _log.LogInfo("EnvUI", $"AudioContext Mode changed to: {tag}");
            UpdatePreview();
        }
    }

    private void SpeechVoices_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Primitives.ToggleButton btn && btn.Tag is string tag)
        {
            _speechVoicesEnabled = tag == "true";
            // 更新所有按钮的选中状态
            SpeechEnabledBtn.IsChecked = _speechVoicesEnabled;
            SpeechDisabledBtn.IsChecked = !_speechVoicesEnabled;
            _log.LogInfo("EnvUI", $"SpeechVoices changed to: {_speechVoicesEnabled}");
            UpdatePreview();
        }
    }

    private async void EditWebGL_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var preselected = new System.Collections.Generic.List<GpuInfo>();
            if (!string.IsNullOrWhiteSpace(WebGLVendorBox.Text) && !string.IsNullOrWhiteSpace(WebGLRendererBox.Text))
            {
                // 尝试从数据库找到匹配的 GPU
                var gpuSvc = _host.Services.GetRequiredService<GpuCatalogService>();
                var gpus = await gpuSvc.SearchWebGLAsync(null, WebGLVendorBox.Text);
                var matching = gpus.FirstOrDefault(g => g.Renderer == WebGLRendererBox.Text);
                if (matching != null)
                    preselected.Add(matching);
            }
            
            var dlg = new GpuPickerDialog(preselected) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true && dlg.SelectedGpus.Count > 0)
            {
                var gpu = dlg.SelectedGpus[0];
                WebGLVendorBox.Text = gpu.Vendor;
                WebGLRendererBox.Text = gpu.Renderer;
                _log.LogInfo("EnvUI", $"Edited WebGL: {gpu.Vendor} / {gpu.Renderer}");
                UpdatePreview();
            }
        }
        catch (Exception ex)
        {
            _log.LogError("EnvUI", $"Edit WebGL failed: {ex.Message}");
            MessageBox.Show($"编辑 WebGL 失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShuffleWebGL_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var gpuSvc = _host.Services.GetRequiredService<GpuCatalogService>();
            var osTag = GetSelectedTag(OsCombo) ?? "windows";
            var osName = osTag switch
            {
                "windows" => "Windows",
                "macos" => "macOS",
                "linux" => "Linux",
                "android" => "Android",
                "ios" => "iOS",
                _ => "Windows"
            };
            System.Threading.Tasks.Task.Run(async () =>
            {
                var gpus = await gpuSvc.RandomSubsetAsync(osName, 1);
                if (gpus.Count > 0)
                {
                    var gpu = gpus[0];
                    Dispatcher.Invoke(() =>
                    {
                        WebGLVendorBox.Text = gpu.Vendor;
                        WebGLRendererBox.Text = gpu.Renderer;
                        _log.LogInfo("EnvUI", $"Shuffled WebGL: {gpu.Vendor} / {gpu.Renderer}");
                        UpdatePreview();
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        _log.LogWarn("EnvUI", $"Shuffle WebGL found 0 GPUs for OS {osName}");
                        MessageBox.Show($"未找到可用的 GPU（OS: {osName}）", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
            });
        }
        catch (Exception ex)
        {
            _log.LogError("EnvUI", $"Shuffle WebGL failed: {ex.Message}");
        }
    }
}
