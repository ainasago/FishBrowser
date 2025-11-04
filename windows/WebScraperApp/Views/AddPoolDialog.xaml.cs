using System;
using System.Windows;
using System.Windows.Controls;

namespace FishBrowser.WPF.Views;

public partial class AddPoolDialog : Window
{
    public string PoolName { get; private set; } = string.Empty;
    public string Strategy { get; private set; } = "random";
    public string? OptionsJson { get; private set; }

    public AddPoolDialog()
    {
        InitializeComponent();
    }

    private void StrategyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var tag = (StrategyCombo?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "random";
        StickyPanel.Visibility = string.Equals(tag, "sticky", StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("请输入池名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        PoolName = name;
        if (StrategyCombo?.SelectedItem is ComboBoxItem item)
        {
            Strategy = (item.Tag?.ToString() ?? "random").ToLowerInvariant();
        }
        if (string.Equals(Strategy, "sticky", StringComparison.OrdinalIgnoreCase))
        {
            var scope = (StickyScopeCombo?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "session";
            int ttl = 10;
            int.TryParse(StickyTtlMinutesBox?.Text?.Trim(), out ttl);
            ttl = Math.Max(1, ttl);
            OptionsJson = System.Text.Json.JsonSerializer.Serialize(new { stickyScope = scope, stickyTtlMinutes = ttl });
        }
        DialogResult = true;
        Close();
    }
}
