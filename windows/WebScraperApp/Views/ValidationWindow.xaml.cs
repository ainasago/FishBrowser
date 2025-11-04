using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Services;
using WpfApplication = System.Windows.Application;

namespace FishBrowser.WPF.Views;

public partial class ValidationWindow : Window
{
    private WebScraperDbContext _db;
    private MetaValidationService _validator;

    public ValidationWindow()
    {
        InitializeComponent();
        var host = WpfApplication.Current.Resources["Host"] as IHost
                   ?? throw new InvalidOperationException("Host not found");
        var mm = host.Services.GetRequiredService<FishBrowser.WPF.Infrastructure.Data.FreeSqlMigrationManager>();
        mm.InitializeDatabase();

        _db = host.Services.GetRequiredService<WebScraperDbContext>();
        _validator = host.Services.GetRequiredService<MetaValidationService>();

        LoadMetas();
    }

    private void LoadMetas()
    {
        MetaGrid.ItemsSource = _db.FingerprintMetaProfiles.OrderByDescending(m => m.Id).ToList();
        StatusText.Text = $"共 { _db.FingerprintMetaProfiles.Count() } 个元配置";
        MissingList.ItemsSource = null;
        ConflictsList.ItemsSource = null;
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        LoadMetas();
    }

    private void ValidateSelected_Click(object sender, RoutedEventArgs e)
    {
        var meta = MetaGrid.SelectedItem as FishBrowser.WPF.Models.FingerprintMetaProfile;
        if (meta == null)
        {
            MessageBox.Show("请先选择一个元配置", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var report = _validator.ValidateMetaProfile(meta);
        MissingList.ItemsSource = report.Missing.ToList();
        ConflictsList.ItemsSource = report.Conflicts.ToList();
        StatusText.Text = report.IsOk ? "校验通过" : $"缺失 {report.Missing.Count} 项，冲突 {report.Conflicts.Count} 项";
    }

    private void MetaGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        MissingList.ItemsSource = null;
        ConflictsList.ItemsSource = null;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
