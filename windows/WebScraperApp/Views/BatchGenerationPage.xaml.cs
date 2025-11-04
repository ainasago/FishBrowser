using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;

namespace FishBrowser.WPF.Views
{
    /// <summary>
    /// BatchGenerationPage.xaml 的交互逻辑
    /// </summary>
    public partial class BatchGenerationPage : Page
    {
        private WebScraperDbContext _db;
        private ILogService _log;
        private IHost _host;

        public BatchGenerationPage()
        {
            InitializeComponent();
            _host = System.Windows.Application.Current.Resources["Host"] as IHost
                       ?? throw new InvalidOperationException("Host not found");
            var mm = _host.Services.GetRequiredService<FishBrowser.WPF.Infrastructure.Data.FreeSqlMigrationManager>();
            mm.InitializeDatabase();

            _db = _host.Services.GetRequiredService<WebScraperDbContext>();
            _log = _host.Services.GetRequiredService<ILogService>();

            // 载入预设
            LoadPresetsOrOfferSeed();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // 返回主页
            if (Parent is Frame frame)
            {
                frame.Navigate(null);
            }
        }

        private void SetStatus(string text) { StatusText.Text = text; }

        private void GenerateExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PresetCombo.SelectedItem is not TraitGroupPreset preset)
                {
                    MessageBox.Show("请先选择预设 (Preset)", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                if (!int.TryParse(CountBox.Text.Trim(), out var n) || n <= 0 || n > 1000)
                {
                    MessageBox.Show("请输入 1-1000 的生成数量", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var sfd = new SaveFileDialog
                {
                    Title = "导出批量生成的指纹",
                    Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                    FileName = $"fingerprints.batch.{DateTime.Now:yyyyMMddHHmmss}.json",
                    AddExtension = true,
                    DefaultExt = ".json",
                    OverwritePrompt = true
                };
                if (sfd.ShowDialog() != true) return;

                var baseSeed = string.IsNullOrWhiteSpace(SeedBox.Text) ? $"batch-{DateTime.Now:HHmmss}" : SeedBox.Text.Trim();
                var region = string.IsNullOrWhiteSpace(RegionBox.Text) ? null : RegionBox.Text.Trim();
                var deviceClass = string.IsNullOrWhiteSpace(DeviceClassBox.Text) ? null : DeviceClassBox.Text.Trim();
                var vendor = string.IsNullOrWhiteSpace(VendorBox.Text) ? null : VendorBox.Text.Trim();

                // 组合 meta traits：preset + context
                var results = new List<FingerprintProfile>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < n; i++)
                {
                    var meta = new FingerprintMetaProfile
                    {
                        Name = $"BatchMeta-{preset.Name}-{i+1}",
                        Version = "0.1.0",
                        Source = "batch",
                        TraitsJson = MergeContextIntoTraits(preset.ItemsJson, region, deviceClass, vendor),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    var seed = $"{baseSeed}:{i}";

                    // 可选：使用 UA 生成器覆盖 UA 相关键
                    if (UseUAGeneratorCheck.IsChecked == true)
                    {
                        try
                        {
                            var uaSvc = new FishBrowser.WPF.Services.UserAgentCompositionService();
                            var ua = uaSvc.GenerateChromeWindows(locale: region == "CN" ? "zh-CN" : null);
                            // 合并 UA 相关 traits 到 meta
                            Dictionary<string, object?> traits;
                            try
                            {
                                traits = string.IsNullOrWhiteSpace(meta.TraitsJson)
                                    ? new Dictionary<string, object?>()
                                    : (JsonSerializer.Deserialize<Dictionary<string, object?>>(meta.TraitsJson) ?? new Dictionary<string, object?>());
                            }
                            catch { traits = new Dictionary<string, object?>(); }
                            foreach (var kv in ua.RelatedTraits)
                            {
                                traits[kv.Key] = kv.Value;
                            }
                            meta.TraitsJson = JsonSerializer.Serialize(traits);
                        }
                        catch { /* ignore UA gen errors */ }
                    }

                    // 直接创建生成器实例，避免 DI 容器问题
                    var depResolver = new DependencyResolutionService(_db);
                    var generator = new FingerprintGeneratorService(_db, _log, depResolver);
                    var profile = generator.GenerateFromMeta(meta, $"{preset.Name}-{i+1}", seed);
                    // 去重（基于关键属性快速哈希）
                    var key = string.Join("|", new[]
                    {
                        profile.UserAgent,
                        profile.Locale,
                        profile.Timezone,
                        profile.ViewportWidth.ToString(),
                        profile.ViewportHeight.ToString(),
                        profile.WebGLVendor ?? string.Empty,
                        profile.WebGLRenderer ?? string.Empty
                    });
                    if (seen.Add(key))
                    {
                        results.Add(profile);
                    }
                }

                var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(sfd.FileName, json);
                _log.LogInfo("Batch", $"Generated {results.Count} profiles (requested {n}), exported to {sfd.FileName}");
                SetStatus($"已导出 {results.Count}/{n} 个（去重后）");
                MessageBox.Show($"导出完成：{results.Count}/{n} 个（去重后）\n保存到: {sfd.FileName}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _log.LogError("Batch", $"Failed batch generation: {ex.Message}", ex.StackTrace);
                MessageBox.Show($"批量生成失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string MergeContextIntoTraits(string? presetTraitsJson, string? region, string? deviceClass, string? vendor)
        {
            // 将 context 写入 traits: context.region/deviceClass/vendor
            Dictionary<string, object?> traits;
            try
            {
                traits = string.IsNullOrWhiteSpace(presetTraitsJson)
                    ? new Dictionary<string, object?>()
                    : (JsonSerializer.Deserialize<Dictionary<string, object?>>(presetTraitsJson) ?? new Dictionary<string, object?>());
            }
            catch
            {
                traits = new Dictionary<string, object?>();
            }

            var ctx = new Dictionary<string, object?>();
            if (!string.IsNullOrWhiteSpace(region)) ctx["region"] = region;
            if (!string.IsNullOrWhiteSpace(deviceClass)) ctx["deviceClass"] = deviceClass;
            if (!string.IsNullOrWhiteSpace(vendor)) ctx["vendor"] = vendor;
            if (ctx.Count > 0) traits["context"] = ctx;

            return JsonSerializer.Serialize(traits);
        }

        private void LoadPresetsOrOfferSeed()
        {
            var presets = _db.TraitGroupPresets.OrderBy(p => p.Name).ToList();
            if (presets.Count == 0)
            {
                var r = MessageBox.Show(
                    "未检测到任何预设。是否自动创建默认预设（Desktop-CN / Desktop-US）？",
                    "预设为空",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    try
                    {
                        _db.TraitGroupPresets.Add(new TraitGroupPreset
                        {
                            Name = "Desktop-CN",
                            Description = "桌面中国区域",
                            Version = "0.1.0",
                            Tags = "desktop,cn",
                            ItemsJson = "{\n  \"browser.platform\": \"Win32\",\n  \"system.locale\": \"zh-CN\",\n  \"system.timezone\": \"Asia/Shanghai\",\n  \"device.viewport.width\": 1366,\n  \"device.viewport.height\": 768\n}"
                        });
                        _db.TraitGroupPresets.Add(new TraitGroupPreset
                        {
                            Name = "Desktop-US",
                            Description = "桌面美国区域",
                            Version = "0.1.0",
                            Tags = "desktop,us",
                            ItemsJson = "{\n  \"browser.platform\": \"Win32\",\n  \"system.locale\": \"en-US\",\n  \"system.timezone\": \"America/New_York\",\n  \"device.viewport.width\": 1920,\n  \"device.viewport.height\": 1080\n}"
                        });
                        _db.SaveChanges();
                        presets = _db.TraitGroupPresets.OrderBy(p => p.Name).ToList();
                        _log.LogInfo("Batch", "Seeded default presets (Desktop-CN/US)");
                    }
                    catch (Exception ex)
                    {
                        _log.LogError("Batch", $"Failed to seed default presets: {ex.Message}", ex.StackTrace);
                        MessageBox.Show($"创建默认预设失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            PresetCombo.ItemsSource = presets;
            PresetCombo.DisplayMemberPath = nameof(TraitGroupPreset.Name);
            if (presets.Count > 0) PresetCombo.SelectedIndex = 0;
            SetStatus($"可用预设: {presets.Count}");
        }
    }
}