using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;
using WpfApplication = System.Windows.Application;
using System.Collections.ObjectModel;

namespace FishBrowser.WPF.Views;

public partial class TraitMetaEditorPage : Page
{
    private WebScraperDbContext _db;
    private ILogService _log;
    private IHost _host;

    private FingerprintMetaProfile? _current;
    private readonly List<TraitEntry> _entries = new();
    public ObservableCollection<string> AllKeys { get; } = new();
    public ObservableCollection<string> Regions { get; } = new();
    public ObservableCollection<string> Vendors { get; } = new();
    public ObservableCollection<string> DeviceClasses { get; } = new();
    private string _selectedSortMode = "weight"; // weight|label|value
    public string SelectedSortMode
    {
        get => _selectedSortMode;
        set { _selectedSortMode = value; RefreshAllOptions(); }
    }

    private string? _selectedRegion;
    public string? SelectedRegion
    {
        get => _selectedRegion;
        set { _selectedRegion = value; RefreshAllOptions(); }
    }

    private string? _selectedVendor;
    public string? SelectedVendor
    {
        get => _selectedVendor;
        set { _selectedVendor = value; RefreshAllOptions(); }
    }

    private string? _selectedDeviceClass;
    public string? SelectedDeviceClass
    {
        get => _selectedDeviceClass;
        set { _selectedDeviceClass = value; RefreshAllOptions(); }
    }

    public TraitMetaEditorPage()
    {
        InitializeComponent();
        _host = WpfApplication.Current.Resources["Host"] as IHost
                   ?? throw new InvalidOperationException("Host not found");
        _db = _host.Services.GetRequiredService<WebScraperDbContext>();
        _log = _host.Services.GetRequiredService<ILogService>();

        // 供 XAML 绑定使用
        DataContext = this;
        LoadAllKeys();
        LoadFilters();

        TraitGrid.ItemsSource = _entries;
        ReloadMetaList();
    }

    private void LoadAllKeys()
    {
        AllKeys.Clear();
        try
        {
            var keys = _db.TraitDefinitions
                .Select(d => d.Key)
                .Distinct()
                .OrderBy(k => k)
                .ToList();
            foreach (var k in keys) AllKeys.Add(k);
        }
        catch
        {
            // ignore
        }
    }

    private void LoadFilters()
    {
        try
        {
            Regions.Clear(); Vendors.Clear(); DeviceClasses.Clear();
            Regions.Add(""); Vendors.Add(""); DeviceClasses.Add("");
            foreach (var r in _db.TraitOptions.Select(o => o.Region).Where(r => r != null && r != "").Distinct().OrderBy(r => r!)) Regions.Add(r!);
            foreach (var v in _db.TraitOptions.Select(o => o.Vendor).Where(v => v != null && v != "").Distinct().OrderBy(v => v!)) Vendors.Add(v!);
            foreach (var d in _db.TraitOptions.Select(o => o.DeviceClass).Where(d => d != null && d != "").Distinct().OrderBy(d => d!)) DeviceClasses.Add(d!);
            SelectedRegion = ""; SelectedVendor = ""; SelectedDeviceClass = "";
        }
        catch { }
    }

    private void RefreshAllOptions()
    {
        foreach (var r in _entries) LoadOptionsForRow(r);
        TraitGrid.Items.Refresh();
    }

    private void GenerateUA_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null)
        {
            MessageBox.Show("请先选择一个元配置", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            var dlg = new FishBrowser.WPF.Views.UserAgentBuilderDialog() { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true) return;

            // 组装 UA
            string ua = BuildUserAgent(dlg);
            // 推断 platform
            string platform = InferPlatform(dlg);

            // 写入相关 traits（字符串需 JSON 转义）
            AddOrUpdateEntry("browser.userAgent", JsonSerializer.Serialize(ua));
            AddOrUpdateEntry("browser.platform", JsonSerializer.Serialize(platform));
            AddOrUpdateEntry("browser.name", JsonSerializer.Serialize(dlg.BrowserName));
            AddOrUpdateEntry("browser.version", JsonSerializer.Serialize(dlg.BrowserVersion));
            if (int.TryParse(dlg.BrowserMajor, out var major)) AddOrUpdateEntry("browser.version.major", major.ToString());
            AddOrUpdateEntry("browser.engine.name", JsonSerializer.Serialize(dlg.Engine));
            AddOrUpdateEntry("browser.engine.version", JsonSerializer.Serialize(dlg.EngineVersion));
            AddOrUpdateEntry("system.os.name", JsonSerializer.Serialize(dlg.OS));
            AddOrUpdateEntry("system.os.version", JsonSerializer.Serialize(dlg.OSVersion));
            AddOrUpdateEntry("system.platform.arch", JsonSerializer.Serialize(dlg.CpuArch));
            AddOrUpdateEntry("device.model", JsonSerializer.Serialize(dlg.DeviceModel));

            // Client Hints 头与平台信息
            if (dlg.GenerateCH)
            {
                var brands = BuildSecCHBrands(dlg);
                AddOrUpdateEntry("headers.accept-ch", JsonSerializer.Serialize("Sec-CH-UA, Sec-CH-UA-Mobile, Sec-CH-UA-Platform, Sec-CH-UA-Platform-Version, Sec-CH-UA-Model"));
                AddOrUpdateEntry("headers.sec-ch-ua", JsonSerializer.Serialize(brands));
                AddOrUpdateEntry("headers.sec-ch-ua-mobile", JsonSerializer.Serialize(dlg.IsMobile ? "?1" : "?0"));
                AddOrUpdateEntry("headers.sec-ch-ua-platform", JsonSerializer.Serialize(dlg.OS));
                AddOrUpdateEntry("headers.sec-ch-ua-platform-version", JsonSerializer.Serialize(dlg.PlatformVersion));
                // 仅移动端带型号
                if (dlg.IsMobile)
                    AddOrUpdateEntry("headers.sec-ch-ua-model", JsonSerializer.Serialize(dlg.DeviceModel));
                else
                    AddOrUpdateEntry("headers.sec-ch-ua-model", JsonSerializer.Serialize(string.Empty));
            }

            TraitGrid.Items.Refresh();
            StatusText.Text = "已生成并填充自定义 UA 相关键";
        }
        catch (Exception ex)
        {
            _log.LogError("MetaEditor", $"Generate UA failed: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"生成 UA 失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string BuildUserAgent(FishBrowser.WPF.Views.UserAgentBuilderDialog dlg)
    {
        var browser = dlg.BrowserName;
        var ver = dlg.BrowserVersion;
        var engine = dlg.Engine;
        var os = dlg.OS;
        var osVer = dlg.OSVersion;
        var webkit = dlg.AppleWebKitVersion;
        var archToken = dlg.CpuArch switch
        {
            "x86" => "WOW64",
            "x86_64" or "amd64" => "Win64; x64",
            "arm64" => os == "Windows" ? "ARM64" : "arm_64",
            _ => dlg.CpuArch
        };

        if (os == "Windows")
        {
            var nt = osVer == "11" ? "10.0" : "10.0"; // 简化处理
            // Blink 家族（Edge/Chrome）
            if (engine == "Blink")
            {
                var chromeToken = $"Chrome/{ver}";
                var baseUA = $"Mozilla/5.0 (Windows NT {nt}; {archToken}) AppleWebKit/{webkit} (KHTML, like Gecko) {chromeToken} Safari/{webkit}";
                if (browser.Equals("Edge", StringComparison.OrdinalIgnoreCase)) return baseUA + $" Edg/{ver}";
                if (browser.Equals("Chrome", StringComparison.OrdinalIgnoreCase)) return baseUA;
            }
            // Gecko (Firefox)
            if (browser.Equals("Firefox", StringComparison.OrdinalIgnoreCase))
            {
                return $"Mozilla/5.0 (Windows NT 10.0; {archToken}; rv:{dlg.BrowserMajor}) Gecko/20100101 Firefox/{ver}";
            }
        }
        else if (os == "macOS")
        {
            if (engine == "WebKit" || browser == "Safari")
            {
                // 简化：Safari 模式
                return $"Mozilla/5.0 (Macintosh; Intel Mac OS X 14_4) AppleWebKit/{webkit} (KHTML, like Gecko) Version/{dlg.SafariVersion} Safari/{webkit}";
            }
            // Chrome on macOS
            return $"Mozilla/5.0 (Macintosh; Intel Mac OS X 14_4) AppleWebKit/{webkit} (KHTML, like Gecko) Chrome/{ver} Safari/{webkit}";
        }
        else if (os == "Linux")
        {
            var arch = dlg.CpuArch == "x86_64" || dlg.CpuArch == "amd64" ? "x86_64" : dlg.CpuArch;
            return $"Mozilla/5.0 (X11; Linux {arch}) AppleWebKit/{webkit} (KHTML, like Gecko) Chrome/{ver} Safari/{webkit}";
        }
        else if (os == "Android")
        {
            var model = string.IsNullOrWhiteSpace(dlg.DeviceModel) ? "Pixel 7" : dlg.DeviceModel;
            return $"Mozilla/5.0 (Linux; Android {osVer}; {model}) AppleWebKit/{webkit} (KHTML, like Gecko) Chrome/{ver} Mobile Safari/{webkit}";
        }
        else if (os == "iOS")
        {
            var iosVer = osVer.Replace('.', '_');
            return $"Mozilla/5.0 (iPhone; CPU iPhone OS {iosVer} like Mac OS X) AppleWebKit/{webkit} (KHTML, like Gecko) Version/{dlg.SafariVersion} Mobile/15E148 Safari/{webkit}";
        }

        // Fallback: Chrome desktop
        return $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/{webkit} (KHTML, like Gecko) Chrome/{ver} Safari/{webkit}";
    }

    private static string BuildSecCHBrands(FishBrowser.WPF.Views.UserAgentBuilderDialog dlg)
    {
        // 简化的 brands 列表，符合 UA-CH 字符串格式
        var major = int.TryParse(dlg.BrowserMajor, out var m) ? m.ToString() : dlg.BrowserMajor;
        if (dlg.BrowserName.Equals("Edge", StringComparison.OrdinalIgnoreCase))
        {
            return $"\"Not.A/Brand\";v=\"8\", \"Chromium\";v=\"{major}\", \"Microsoft Edge\";v=\"{major}\"";
        }
        if (dlg.BrowserName.Equals("Chrome", StringComparison.OrdinalIgnoreCase))
        {
            return $"\"Not.A/Brand\";v=\"8\", \"Chromium\";v=\"{major}\", \"Google Chrome\";v=\"{major}\"";
        }
        if (dlg.BrowserName.Equals("Firefox", StringComparison.OrdinalIgnoreCase))
        {
            // Firefox UA-CH 采用不同方案，这里占位
            return $"\"Not.A/Brand\";v=\"8\", \"Firefox\";v=\"{major}\"";
        }
        if (dlg.BrowserName.Equals("Safari", StringComparison.OrdinalIgnoreCase))
        {
            return $"\"Not.A/Brand\";v=\"8\", \"Safari\";v=\"{major}\"";
        }
        return $"\"Not.A/Brand\";v=\"8\", \"Chromium\";v=\"{major}\"";
    }

    private static string InferPlatform(FishBrowser.WPF.Views.UserAgentBuilderDialog dlg)
    {
        return dlg.OS switch
        {
            "Windows" => "Win32",
            "macOS" => "MacIntel",
            "Linux" => "Linux x86_64",
            "Android" => "Android",
            "iOS" => "iPhone",
            _ => "Win32"
        };
    }

    private UserAgentCompositionService TryResolveUAService()
    {
        try
        {
            return _host.Services.GetRequiredService<UserAgentCompositionService>();
        }
        catch
        {
            return new UserAgentCompositionService();
        }
    }

    public TraitMetaEditorPage(string initialKey, string initialValueJson) : this()
    {
        // 选择或创建一个 Meta，用于承载传入的条目
        if (MetaCombo.Items.Count > 0)
        {
            MetaCombo.SelectedIndex = 0;
        }
        else
        {
            var meta = new FingerprintMetaProfile { Name = $"Meta-{DateTime.Now:HHmmss}", Version = "0.1.0", Source = "user", TraitsJson = "{}", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _db.FingerprintMetaProfiles.Add(meta);
            _db.SaveChanges();
            ReloadMetaList();
            MetaCombo.SelectedItem = meta;
        }

        // 添加条目到网格（不立即保存，用户可编辑后点保存）
        AddOrUpdateEntry(initialKey, initialValueJson);
        TraitGrid.Items.Refresh();
        StatusText.Text = $"已添加条目: {initialKey}";
    }

    private void ReloadMetaList()
    {
        var metas = _db.FingerprintMetaProfiles.OrderByDescending(m => m.Id).ToList();
        MetaCombo.ItemsSource = metas;
        MetaCombo.DisplayMemberPath = nameof(FingerprintMetaProfile.Name);
        if (metas.Count > 0)
        {
            MetaCombo.SelectedIndex = 0;
        }
        StatusText.Text = $"共 {metas.Count} 个元配置";
    }

    private void MetaCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _entries.Clear();
        _current = MetaCombo.SelectedItem as FingerprintMetaProfile;
        if (_current == null)
        {
            TraitGrid.Items.Refresh();
            return;
        }
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(_current.TraitsJson ?? "{}");
            var dictSafe = dict ?? new();
            foreach (var kv in dictSafe.OrderBy(k => k.Key))
            {
                var row = NewRow(kv.Key, SerializeValue(kv.Value));
                LoadOptionsForRow(row);
                _entries.Add(row);
            }
            LockFlagsBox.Text = _current.LockFlagsJson ?? "[]";
            RandomPlanBox.Text = _current.RandomizationPlanJson ?? "{}";
            // 加载字体信息到 TraitsJson 中（便于显示与编辑）
            if (!string.IsNullOrEmpty(_current.FontsMode))
            {
                dictSafe["fonts.mode"] = _current.FontsMode;
            }
            if (!string.IsNullOrEmpty(_current.FontsJson))
            {
                try
                {
                    dictSafe["fonts.list"] = JsonSerializer.Deserialize<List<string>>(_current.FontsJson) ?? new();
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            _log.LogError("MetaEditor", $"Parse traits failed: {ex.Message}", ex.StackTrace);
        }
        finally
        {
            TraitGrid.Items.Refresh();
        }
    }

    private string SerializeValue(object? value)
    {
        if (value == null) return string.Empty;
        return value is string s ? s : JsonSerializer.Serialize(value);
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // 控制占位符文本的显示/隐藏
        PlaceholderText.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        
        var s = SearchBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(s))
        {
            TraitGrid.ItemsSource = _entries;
        }
        else
        {
            TraitGrid.ItemsSource = _entries.Where(x => x.Key.Contains(s, StringComparison.OrdinalIgnoreCase));
        }
        TraitGrid.Items.Refresh();
    }

    private void NewMeta_Click(object sender, RoutedEventArgs e)
    {
        var meta = new FingerprintMetaProfile { Name = $"Meta-{DateTime.Now:HHmmss}", Version = "0.1.0", Source = "user", TraitsJson = "{}", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.FingerprintMetaProfiles.Add(meta);
        _db.SaveChanges();
        ReloadMetaList();
        MetaCombo.SelectedItem = meta;

        // 新建时：默认列出所有键并填充默认值
        _entries.Clear();
        var defs = _db.TraitDefinitions.OrderBy(d => d.Key).ToList();
        foreach (var def in defs)
        {
            var row = NewRow(def.Key, string.IsNullOrWhiteSpace(def.DefaultValueJson) ? string.Empty : def.DefaultValueJson);
            LoadOptionsForRow(row);
            _entries.Add(row);
        }
        TraitGrid.Items.Refresh();
        StatusText.Text = $"已载入 {defs.Count} 个键的默认值";
    }

    private void SaveMeta_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null)
        {
            MessageBox.Show("请先选择一个元配置", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            var dict = new Dictionary<string, object?>();
            foreach (var row in _entries)
            {
                if (string.IsNullOrWhiteSpace(row.Key)) continue;
                if (string.IsNullOrWhiteSpace(row.Value)) continue;
                dict[row.Key.Trim()] = TryParseValue(row.Value);
            }
            _current.TraitsJson = JsonSerializer.Serialize(dict);
            _current.LockFlagsJson = string.IsNullOrWhiteSpace(LockFlagsBox.Text) ? "[]" : LockFlagsBox.Text;
            _current.RandomizationPlanJson = string.IsNullOrWhiteSpace(RandomPlanBox.Text) ? "{}" : RandomPlanBox.Text;
            // 保存字体信息（从 TraitsJson 中提取或使用默认值）
            if (dict.TryGetValue("fonts.mode", out var fontsModeObj))
            {
                _current.FontsMode = fontsModeObj?.ToString() ?? "real";
            }
            if (dict.TryGetValue("fonts.list", out var fontsListObj))
            {
                _current.FontsJson = fontsListObj?.ToString();
            }
            _db.SaveChanges();
            StatusText.Text = "已保存";
        }
        catch (Exception ex)
        {
            _log.LogError("MetaEditor", $"Save meta failed: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddOrUpdateEntry(string key, string valueJson)
    {
        var existing = _entries.FirstOrDefault(e => string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            existing.Value = valueJson;
            LoadOptionsForRow(existing);
        }
        else
        {
            var row = NewRow(key, valueJson);
            LoadOptionsForRow(row);
            _entries.Add(row);
        }
    }

    private TraitEntry NewRow(string key, string value)
    {
        return new TraitEntry(onKeyChanged: r => LoadOptionsForRow(r)) { Key = key, Value = value };
    }

    private void LoadOptionsForRow(TraitEntry row)
    {
        try
        {
            row.Options.Clear();
            if (string.IsNullOrWhiteSpace(row.Key)) return;
            var def = _db.TraitDefinitions.FirstOrDefault(d => d.Key == row.Key);
            if (def == null) return;

            // 加载 TraitOptions
            var query = _db.TraitOptions.Where(o => o.TraitDefinitionId == def.Id);
            if (!string.IsNullOrWhiteSpace(SelectedRegion)) query = query.Where(o => o.Region == SelectedRegion);
            if (!string.IsNullOrWhiteSpace(SelectedVendor)) query = query.Where(o => o.Vendor == SelectedVendor);
            if (!string.IsNullOrWhiteSpace(SelectedDeviceClass)) query = query.Where(o => o.DeviceClass == SelectedDeviceClass);
            // 排序
            IOrderedEnumerable<Models.TraitOption> ordered;
            var list = query.ToList();
            if (SelectedSortMode == "label") ordered = list.OrderBy(o => o.Label).ThenBy(o => o.ValueJson);
            else if (SelectedSortMode == "value") ordered = list.OrderBy(o => o.ValueJson);
            else ordered = list.OrderByDescending(o => o.Weight).ThenBy(o => o.Label);
            var opts = ordered.ToList();
            foreach (var o in opts)
            {
                var display = string.IsNullOrWhiteSpace(o.Label) ? o.ValueJson : o.Label;
                row.Options.Add(new OptionItem { Display = display!, ValueJson = o.ValueJson });
            }
            row.RebuildFiltered();
            // 若当前值为空且定义有默认值，则填充默认
            if (string.IsNullOrWhiteSpace(row.Value) && !string.IsNullOrWhiteSpace(def.DefaultValueJson))
            {
                row.Value = def.DefaultValueJson!;
            }
        }
        catch
        {
            // ignore
        }
    }

    private object? TryParseValue(string text)
    {
        try
        {
            text = text.Trim();
            if (text.StartsWith("{") || text.StartsWith("[") || text.StartsWith("\""))
            {
                return JsonSerializer.Deserialize<object>(text);
            }
            if (int.TryParse(text, out var i)) return i;
            if (double.TryParse(text, out var d)) return d;
            if (bool.TryParse(text, out var b)) return b;
            if (string.Equals(text, "null", StringComparison.OrdinalIgnoreCase)) return null;
            return text; // treat as plain string
        }
        catch
        {
            return text;
        }
    }

    private void GenerateProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null)
        {
            MessageBox.Show("请先选择一个元配置", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            SaveMeta_Click(sender, e);
            var name = $"{_current.Name}-Profile-{DateTime.Now:HHmmss}";
            
            // 直接创建生成器实例，避免 DI 容器问题
            var depResolver = new DependencyResolutionService(_db);
            var generator = new FingerprintGeneratorService(_db, _log, depResolver);
            var profile = generator.GenerateFromMeta(_current, name);
            _db.FingerprintProfiles.Add(profile);
            _db.SaveChanges();
            MessageBox.Show($"已生成并保存 Profile: {name}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _log.LogError("MetaEditor", $"Generate profile failed: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"生成失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PickOption_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is not TraitEntry row) return;
        if (string.IsNullOrWhiteSpace(row.Key))
        {
            MessageBox.Show("请先填写 Key", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        // 特殊处理字体列表编辑
        if (row.Key.Equals("fonts.list", StringComparison.OrdinalIgnoreCase))
        {
            EditFontsList_Click(row);
            return;
        }
        // 特殊处理 GPU 编辑
        if (row.Key.Equals("graphics.webgl.vendor", StringComparison.OrdinalIgnoreCase) || 
            row.Key.Equals("graphics.webgl.renderer", StringComparison.OrdinalIgnoreCase))
        {
            EditGpuList_Click(row);
            return;
        }
        var win = new OptionPickerWindow(row.Key) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            row.Value = win.SelectedValueJson;
            TraitGrid.Items.Refresh();
        }
    }

    private void EditFontsList_Click(TraitEntry row)
    {
        try
        {
            var preselected = new List<string>();
            if (!string.IsNullOrWhiteSpace(row.Value))
            {
                try
                {
                    preselected = JsonSerializer.Deserialize<List<string>>(row.Value) ?? new();
                }
                catch { }
            }
            var dlg = new FontPickerDialog(preselected) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
            {
                row.Value = JsonSerializer.Serialize(dlg.SelectedFonts);
                TraitGrid.Items.Refresh();
            }
        }
        catch (Exception ex)
        {
            _log.LogError("MetaEditor", $"Edit fonts list failed: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"编辑字体失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void EditGpuList_Click(TraitEntry row)
    {
        try
        {
            var preselected = new List<GpuInfo>();
            if (!string.IsNullOrWhiteSpace(row.Value))
            {
                try
                {
                    var vendorRenderer = JsonSerializer.Deserialize<Dictionary<string, string>>(row.Value);
                    if (vendorRenderer != null && vendorRenderer.ContainsKey("vendor") && vendorRenderer.ContainsKey("renderer"))
                    {
                        var gpuSvc = _host.Services.GetRequiredService<GpuCatalogService>();
                        System.Threading.Tasks.Task.Run(async () =>
                        {
                            var gpus = await gpuSvc.SearchWebGLAsync(null, vendorRenderer["vendor"]);
                            var matching = gpus.FirstOrDefault(g => g.Renderer == vendorRenderer["renderer"]);
                            if (matching != null)
                                preselected.Add(matching);
                        }).Wait();
                    }
                }
                catch { }
            }
            var dlg = new GpuPickerDialog(preselected) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true && dlg.SelectedGpus.Count > 0)
            {
                var gpu = dlg.SelectedGpus[0];
                row.Value = JsonSerializer.Serialize(new { vendor = gpu.Vendor, renderer = gpu.Renderer });
                TraitGrid.Items.Refresh();
                _log.LogInfo("MetaEditor", $"Edited GPU: {gpu.Vendor} / {gpu.Renderer}");
            }
        }
        catch (Exception ex)
        {
            _log.LogError("MetaEditor", $"Edit GPU list failed: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"编辑 GPU 失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LockAll_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var keys = _entries.Select(x => x.Key).Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().ToList();
            LockFlagsBox.Text = JsonSerializer.Serialize(keys);
        }
        catch { }
    }

    private void UnlockAll_Click(object sender, RoutedEventArgs e)
    {
        LockFlagsBox.Text = "[]";
    }

    private void FillMissingWithDefaults_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var defs = _db.TraitDefinitions.ToDictionary(d => d.Key, d => d.DefaultValueJson);
            foreach (var row in _entries)
            {
                if (string.IsNullOrWhiteSpace(row.Value) && row.Key != null && defs.TryGetValue(row.Key, out var defVal) && !string.IsNullOrWhiteSpace(defVal))
                {
                    row.Value = defVal!;
                }
            }
            TraitGrid.Items.Refresh();
            StatusText.Text = "已补齐缺省值";
        }
        catch (Exception ex)
        {
            _log.LogWarn("MetaEditor", $"Fill defaults failed: {ex.Message}");
        }
    }

    private void FillRandomPlanSample_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var plan = new Dictionary<string, object?>();

            foreach (var row in _entries)
            {
                if (string.IsNullOrWhiteSpace(row.Key)) continue;
                var key = row.Key.Trim();
                var valText = row.Value?.Trim();

                object rule = BuildRuleForKey(key, valText);
                plan[key] = rule;
            }

            RandomPlanBox.Text = JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true });
            StatusText.Text = "已为全部键生成默认随机计划";
        }
        catch (Exception ex)
        {
            _log.LogWarn("MetaEditor", $"Build random plan failed: {ex.Message}");
            MessageBox.Show($"生成随机计划失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private object BuildRuleForKey(string key, string? valText)
    {
        // 常见键的特化
        if (key.Contains("device.viewport.width", StringComparison.OrdinalIgnoreCase))
        {
            var w = TryParseInt(valText, 1366);
            return new { type = "UniformInt", min = Math.Max(960, w - 400), max = Math.Max(w + 400, 1280) };
        }
        if (key.Contains("device.viewport.height", StringComparison.OrdinalIgnoreCase))
        {
            var h = TryParseInt(valText, 768);
            return new { type = "UniformInt", min = Math.Max(600, h - 200), max = Math.Max(h + 200, 720) };
        }
        if (key.Contains("device.hardwareConcurrency", StringComparison.OrdinalIgnoreCase))
        {
            return new { type = "UniformInt", min = 2, max = 32 };
        }
        if (key.Contains("device.deviceMemory", StringComparison.OrdinalIgnoreCase))
        {
            return new { type = "UniformInt", min = 2, max = 32 };
        }
        if (key.Contains("graphics.canvas.noiseSeed", StringComparison.OrdinalIgnoreCase))
        {
            return new { type = "UniformInt", min = 1, max = 1000000 };
        }
        if (key.Contains("network.webrtc.enabled", StringComparison.OrdinalIgnoreCase))
        {
            return new { type = "Bool", trueRatio = 0.3 };
        }
        if (key.Contains("system.locale", StringComparison.OrdinalIgnoreCase) || key.Contains("browser.acceptLanguage", StringComparison.OrdinalIgnoreCase))
        {
            return new { type = "OneOf", values = new[] { "zh-CN", "en-US", "zh-TW" } };
        }
        if (key.Contains("system.timezone", StringComparison.OrdinalIgnoreCase))
        {
            return new { type = "OneOf", values = new[] { "Asia/Shanghai", "America/New_York", "Europe/London" } };
        }
        if (key.Contains("browser.platform", StringComparison.OrdinalIgnoreCase))
        {
            return new { type = "OneOf", values = new[] { "Win32", "MacIntel", "Linux x86_64", "iPhone" } };
        }
        if (key.Contains("browser.userAgent", StringComparison.OrdinalIgnoreCase))
        {
            return new { type = "OneOf", values = new[] {
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_4) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Safari/605.1.15",
                "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36",
                "Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Mobile/15E148 Safari/604.1"
            } };
        }

        // 基于值类型的通用规则
        if (int.TryParse(valText, out var iv))
        {
            var min = Math.Max(0, iv - Math.Max(1, iv / 5));
            var max = iv + Math.Max(2, iv / 5);
            return new { type = "UniformInt", min, max };
        }
        if (double.TryParse(valText, out var dv))
        {
            var std = Math.Max(0.1, Math.Abs(dv * 0.1));
            return new { type = "NormalDouble", mean = dv, stddev = std };
        }
        if (bool.TryParse(valText, out _))
        {
            return new { type = "Bool", trueRatio = 0.5 };
        }
        if (!string.IsNullOrWhiteSpace(valText) && valText.StartsWith("["))
        {
            // 数组：提供洗牌
            return new { type = "Shuffle" };
        }

        // 其它字符串：提供一个通用集合
        return new { type = "OneOf", values = new[] { valText ?? string.Empty, "alpha", "beta", "gamma" } };
    }

    private static int TryParseInt(string? text, int fallback)
    {
        return int.TryParse(text, out var v) ? v : fallback;
    }

    public class OptionItem
    {
        public string Display { get; set; } = string.Empty;
        public string ValueJson { get; set; } = string.Empty;
    }

    private class TraitEntry : System.ComponentModel.INotifyPropertyChanged
    {
        private string _key = string.Empty;
        private string _value = string.Empty;
        private readonly Action<TraitEntry> _onKeyChanged;

        public TraitEntry(Action<TraitEntry>? onKeyChanged = null)
        {
            _onKeyChanged = onKeyChanged ?? (_ => { });
        }

        public ObservableCollection<OptionItem> Options { get; } = new();
        public ObservableCollection<DisplayItem> FilteredOptions { get; } = new();

        public string Key
        {
            get => _key;
            set
            {
                if (_key == value) return;
                _key = value;
                OnPropertyChanged(nameof(Key));
                _onKeyChanged(this);
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                OnPropertyChanged(nameof(Value));
                RebuildFiltered();
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

        public void RebuildFiltered()
        {
            // 过滤规则：包含匹配（子串），来源为当前 Value 文本（作为搜索关键字）
            var keyword = (_value ?? string.Empty).Trim().Trim('"');
            FilteredOptions.Clear();
            foreach (var o in Options)
            {
                var text = o.Display ?? string.Empty;
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    FilteredOptions.Add(new DisplayItem { Prefix = text, Match = string.Empty, Suffix = string.Empty, ValueJson = o.ValueJson });
                    continue;
                }
                var idx = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var prefix = text.Substring(0, idx);
                    var match = text.Substring(idx, keyword.Length);
                    var suffix = text.Substring(idx + keyword.Length);
                    FilteredOptions.Add(new DisplayItem { Prefix = prefix, Match = match, Suffix = suffix, ValueJson = o.ValueJson });
                }
            }
        }
    }

    public class DisplayItem
    {
        public string Prefix { get; set; } = string.Empty;
        public string Match { get; set; } = string.Empty;
        public string Suffix { get; set; } = string.Empty;
        public string ValueJson { get; set; } = string.Empty;
    }
}