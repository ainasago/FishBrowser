using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;
using WpfApplication = System.Windows.Application;

namespace FishBrowser.WPF.Views;

public partial class OptionPickerWindow : Window
{
    private readonly string _traitKey;
    private readonly WebScraperDbContext _db;
    private List<OptionRow> _rows = new();

    public string? SelectedValueJson { get; private set; }

    public OptionPickerWindow(string traitKey)
    {
        InitializeComponent();
        _traitKey = traitKey;
        KeyText.Text = traitKey;

        var host = WpfApplication.Current.Resources["Host"] as IHost
                   ?? throw new InvalidOperationException("Host not found");
        _db = host.Services.GetRequiredService<WebScraperDbContext>();

        LoadOptions();
    }

    private void LoadOptions()
    {
        var def = _db.TraitDefinitions.FirstOrDefault(d => d.Key == _traitKey);
        if (def == null)
        {
            StatusText.Text = "未找到对应的 TraitDefinition";
            OptionGrid.ItemsSource = null;
            return;
        }
        var opts = _db.TraitOptions.Where(o => o.TraitDefinitionId == def.Id).OrderByDescending(o => o.Weight).ToList();
        _rows = opts.Select(o => new OptionRow
        {
            Label = string.IsNullOrWhiteSpace(o.Label) ? "(无)" : o.Label,
            Weight = o.Weight,
            Region = o.Region,
            Vendor = o.Vendor,
            DeviceClass = o.DeviceClass,
            ValueJson = o.ValueJson
        }).ToList();
        OptionGrid.ItemsSource = _rows;
        StatusText.Text = $"共 {_rows.Count} 个选项";
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // 控制占位符文本的显示/隐藏
        PlaceholderText.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        
        var s = SearchBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(s))
        {
            OptionGrid.ItemsSource = _rows;
        }
        else
        {
            OptionGrid.ItemsSource = _rows.Where(r =>
                (r.Label?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.ValueJson?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Region?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Vendor?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.DeviceClass?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();
        }
        OptionGrid.Items.Refresh();
    }

    private void Pick_Click(object sender, RoutedEventArgs e)
    {
        if (OptionGrid.SelectedItem is OptionRow row)
        {
            SelectedValueJson = row.ValueJson;
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("请先选择一个选项", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private class OptionRow
    {
        public string? Label { get; set; }
        public double Weight { get; set; }
        public string? Region { get; set; }
        public string? Vendor { get; set; }
        public string? DeviceClass { get; set; }
        public string ValueJson { get; set; } = string.Empty;
    }
}
