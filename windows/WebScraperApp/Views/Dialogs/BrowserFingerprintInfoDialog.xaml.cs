using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;

namespace FishBrowser.WPF.Views.Dialogs
{
    public partial class BrowserFingerprintInfoDialog : Window
    {
        private FingerprintProfile? _profile;
        private BrowserEnvironment? _environment;

        public BrowserFingerprintInfoDialog(FingerprintProfile profile, BrowserEnvironment? environment = null)
        {
            InitializeComponent();
            _profile = profile;
            _environment = environment;
            
            // 设置窗口 icon
            try
            {
                var iconPath = System.IO.Path.Combine(
                    System.AppDomain.CurrentDomain.BaseDirectory,
                    "assets",
                    "icon.ico"
                );
                
                if (System.IO.File.Exists(iconPath))
                {
                    this.Icon = new BitmapImage(new System.Uri(iconPath));
                }
            }
            catch
            {
                // Icon 加载失败，继续运行
            }
            
            LoadFingerprintInfo();
            LoadFingerprintJson();
        }

        private void LoadFingerprintInfo()
        {
            if (_profile == null)
            {
                FingerprintTextBox.Text = "No fingerprint profile loaded";
                return;
            }

            var info = new System.Text.StringBuilder();
            info.AppendLine("=".PadRight(80, '='));
            info.AppendLine("🔍 浏览器指纹信息");
            info.AppendLine("=".PadRight(80, '='));
            info.AppendLine();

            // 基础信息
            info.AppendLine("📋 基础信息");
            info.AppendLine("-".PadRight(80, '-'));
            info.AppendLine($"Profile ID:              {_profile.Id}");
            info.AppendLine($"Profile Name:            {_profile.Name}");
            info.AppendLine($"Created At:              {_profile.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            info.AppendLine($"Updated At:              {_profile.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
            info.AppendLine();

            // User-Agent 信息
            info.AppendLine("🌐 User-Agent");
            info.AppendLine("-".PadRight(80, '-'));
            info.AppendLine($"User-Agent:              {_profile.UserAgent}");
            info.AppendLine();

            // 语言和地区
            info.AppendLine("🗣️ 语言和地区");
            info.AppendLine("-".PadRight(80, '-'));
            info.AppendLine($"Locale:                  {_profile.Locale}");
            info.AppendLine($"Languages:               {_profile.LanguagesJson}");
            info.AppendLine($"Timezone:                {_profile.Timezone}");
            info.AppendLine();

            // 屏幕和视口
            info.AppendLine("📱 屏幕和视口");
            info.AppendLine("-".PadRight(80, '-'));
            info.AppendLine($"Viewport Width:          {_profile.ViewportWidth}");
            info.AppendLine($"Viewport Height:         {_profile.ViewportHeight}");
            info.AppendLine();

            // 平台信息
            info.AppendLine("💻 平台信息");
            info.AppendLine("-".PadRight(80, '-'));
            info.AppendLine($"Platform:                {_profile.Platform}");
            info.AppendLine();

            // WebGL 信息
            info.AppendLine("🎮 WebGL 信息");
            info.AppendLine("-".PadRight(80, '-'));
            info.AppendLine($"WebGL Vendor:            {_profile.WebGLVendor}");
            info.AppendLine($"WebGL Renderer:          {_profile.WebGLRenderer}");
            info.AppendLine();

            // 字体信息
            info.AppendLine("🔤 字体信息");
            info.AppendLine("-".PadRight(80, '-'));
            info.AppendLine($"Fonts Mode:              {_profile.FontsMode}");
            info.AppendLine($"Fonts JSON:              {_profile.FontsJson}");
            info.AppendLine();

            // 硬件信息
            info.AppendLine("⚙️ 硬件信息");
            info.AppendLine("-".PadRight(80, '-'));
            info.AppendLine($"Hardware Concurrency:    {_profile.HardwareConcurrency}");
            info.AppendLine($"Device Memory:           {_profile.DeviceMemory}");
            info.AppendLine($"Max Touch Points:        {_profile.MaxTouchPoints}");
            info.AppendLine();

            // 网络信息
            info.AppendLine("🌍 网络信息");
            info.AppendLine("-".PadRight(80, '-'));
            info.AppendLine($"Connection Type:         {_profile.ConnectionType}");
            info.AppendLine($"Connection RTT:          {_profile.ConnectionRtt}");
            info.AppendLine($"Connection Downlink:     {_profile.ConnectionDownlink}");
            info.AppendLine();

            // Sec-CH-UA 信息
            info.AppendLine("🔐 Sec-CH-UA 信息");
            info.AppendLine("-".PadRight(80, '-'));
            info.AppendLine($"Sec-CH-UA:               {_profile.SecChUa}");
            info.AppendLine($"Sec-CH-UA-Platform:      {_profile.SecChUaPlatform}");
            info.AppendLine($"Sec-CH-UA-Mobile:        {_profile.SecChUaMobile}");
            info.AppendLine();

            // Plugins 信息
            info.AppendLine("🔌 Plugins 信息");
            info.AppendLine("-".PadRight(80, '-'));
            info.AppendLine($"Plugins JSON:            {_profile.PluginsJson}");
            info.AppendLine();

            // 其他信息
            info.AppendLine("📌 其他信息");
            info.AppendLine("-".PadRight(80, '-'));
            info.AppendLine($"Accept Language:         {_profile.AcceptLanguage}");
            
            // Webdriver 配置
            var webdriverMode = _environment?.WebdriverMode ?? "undefined";
            var webdriverDisplay = webdriverMode switch
            {
                "undefined" or "delete" => "undefined (已隐藏)",
                "true" => "true (显示)",
                "false" => "false (显示)",
                _ => webdriverMode
            };
            info.AppendLine($"Webdriver Mode:          {webdriverDisplay}");
            info.AppendLine();

            info.AppendLine("=".PadRight(80, '='));

            FingerprintTextBox.Text = info.ToString();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            FingerprintTextBox.SelectAll();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (FingerprintTextBox.SelectedText.Length > 0)
            {
                Clipboard.SetText(FingerprintTextBox.SelectedText);
            }
            else
            {
                Clipboard.SetText(FingerprintTextBox.Text);
            }
            MessageBox.Show("已复制到剪贴板", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadFingerprintJson()
        {
            if (_profile == null)
            {
                FingerprintJsonTextBox.Text = "{}";
                return;
            }

            try
            {
                // 使用通用的指纹收集服务
                var host = System.Windows.Application.Current.Resources["Host"] as IHost;
                if (host != null)
                {
                    var collectorService = host.Services.GetRequiredService<FingerprintCollectorService>();
                    var webdriverMode = _environment?.WebdriverMode ?? "undefined";
                    FingerprintJsonTextBox.Text = collectorService.GenerateFingerprintJson(_profile, webdriverMode);
                }
                else
                {
                    FingerprintJsonTextBox.Text = "{ \"error\": \"Service not available\" }";
                }
            }
            catch (Exception ex)
            {
                FingerprintJsonTextBox.Text = $"{{ \"error\": \"{ex.Message}\" }}";
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadFingerprintInfo();
            LoadFingerprintJson();
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                FileName = $"fingerprint-{_profile?.Name}-{System.DateTime.Now:yyyyMMdd-HHmmss}.txt"
            };

            if (saveDialog.ShowDialog() == true)
            {
                System.IO.File.WriteAllText(saveDialog.FileName, FingerprintTextBox.Text);
                MessageBox.Show($"已导出到: {saveDialog.FileName}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
