using System;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using FishBrowser.WPF.Services;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Views;

public partial class AddProxyDialog : Window
{
    public ProxyServer Result { get; private set; }
    public string? PasswordPlain { get; private set; }

    public AddProxyDialog()
    {
        InitializeComponent();
        // 尝试加载可用的代理池
        try
        {
            var host = System.Windows.Application.Current.Resources["Host"] as Microsoft.Extensions.Hosting.IHost;
            var catalog = host?.Services.GetRequiredService<ProxyCatalogService>();
            var pools = catalog?.GetPools() ?? new System.Collections.Generic.List<FishBrowser.WPF.Models.ProxyPool>();
            PoolCombo.ItemsSource = pools;
            if (pools.Count > 0) PoolCombo.SelectedIndex = 0;
        }
        catch { /* 忽略：不阻塞弹窗 */ }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 验证必填项
            if (string.IsNullOrWhiteSpace(AddressTextBox.Text))
            {
                MessageBox.Show("请输入代理地址", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(PortTextBox.Text))
            {
                MessageBox.Show("请输入端口", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(PortTextBox.Text, out var port) || port < 1 || port > 65535)
            {
                MessageBox.Show("端口必须是 1-65535 之间的数字", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ProtocolCombo.SelectedIndex < 0)
            {
                MessageBox.Show("请选择协议", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 创建代理对象（仅返回基础字段；加密与持久化由服务处理）
            var protoItem = ProtocolCombo.SelectedItem as ComboBoxItem;
            var proto = protoItem?.Content?.ToString() ?? "HTTP";
            Result = new ProxyServer
            {
                Address = AddressTextBox.Text.Trim(),
                Port = port,
                Protocol = proto,
                Username = UsernameTextBox.Text.Trim(),
                Status = "Checking",
                CreatedAt = DateTime.UtcNow
            };
            PasswordPlain = PasswordBox.Password;
            // 将选中的池 Id 暂存到 Result.PoolId（由调用方在持久化时使用）
            if (PoolCombo.SelectedValue is int poolId)
            {
                Result.PoolId = poolId;
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void AddPoolFromDialog_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new AddPoolDialog { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                var host = System.Windows.Application.Current.Resources["Host"] as Microsoft.Extensions.Hosting.IHost;
                var catalog = host?.Services.GetRequiredService<ProxyCatalogService>()
                              ?? throw new InvalidOperationException("ProxyCatalogService not found");
                var pool = catalog.CreatePool(dlg.PoolName, dlg.Strategy);
                var pools = catalog.GetPools();
                PoolCombo.ItemsSource = pools;
                PoolCombo.SelectedValue = pool.Id;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"创建代理池失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
