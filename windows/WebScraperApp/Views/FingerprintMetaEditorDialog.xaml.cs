using System;
using System.Text.Json;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;
using WpfApplication = System.Windows.Application;

namespace FishBrowser.WPF.Views;

public partial class FingerprintMetaEditorDialog : Window
{
    private readonly WebScraperDbContext _db;
    private readonly ILogService _log;
    private readonly IHost _host;

    public FingerprintMetaEditorDialog()
    {
        InitializeComponent();
        _host = WpfApplication.Current.Resources["Host"] as IHost;
        _db = _host?.Services.GetRequiredService<WebScraperDbContext>() ?? throw new InvalidOperationException("DbContext not found");
        _log = _host?.Services.GetRequiredService<ILogService>() ?? throw new InvalidOperationException("LogService not found");

        // 默认示例
        if (string.IsNullOrWhiteSpace(JsonBox.Text))
        {
            JsonBox.Text = "{\n  \"browser.userAgent\": \"Mozilla/5.0\",\n  \"system.locale\": \"zh-CN\",\n  \"system.timezone\": \"Asia/Shanghai\",\n  \"device.viewport.width\": 1366,\n  \"device.viewport.height\": 768\n}";
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("请输入名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 验证 JSON 格式
            using var doc = JsonDocument.Parse(JsonBox.Text);
            var traitsJson = JsonBox.Text;

            // 创建 MetaProfile
            var meta = new FingerprintMetaProfile
            {
                Name = $"Meta-{NameBox.Text}",
                Version = "0.1.0",
                Source = "user",
                TraitsJson = traitsJson,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.FingerprintMetaProfiles.Add(meta);
            _db.SaveChanges();

            // 生成 Profile 并保存
            var depResolver = new DependencyResolutionService(_db);
            var generator = new FingerprintGeneratorService(_db, _log, depResolver);
            var profile = generator.GenerateFromMeta(meta, NameBox.Text);
            _db.FingerprintProfiles.Add(profile);
            _db.SaveChanges();

            _log.LogInfo("MetaEditor", $"Generated fingerprint from meta: {NameBox.Text} (MetaId={meta.Id}, ProfileId={profile.Id})");
            MessageBox.Show("生成并保存成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            _log.LogError("MetaEditor", $"Failed to generate from meta: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"生成失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
