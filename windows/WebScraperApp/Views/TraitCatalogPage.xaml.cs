using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.ViewModels;
using WpfApplication = System.Windows.Application;
using FishBrowser.WPF.Views;

namespace FishBrowser.WPF.Views;

public partial class TraitCatalogPage : Page
{
    private TraitCatalogViewModel _vm;
    private WebScraperDbContext _db;

    public TraitCatalogPage()
    {
        InitializeComponent();
        var host = WpfApplication.Current.Resources["Host"] as IHost
                   ?? throw new InvalidOperationException("Host not found");
        // 确保数据库/表结构已初始化（防止首次打开窗口时尚未完成初始化）
        try
        {
            var mm = host.Services.GetRequiredService<FishBrowser.WPF.Infrastructure.Data.FreeSqlMigrationManager>();
            mm.InitializeDatabase();
        }
        catch { /* ignore, UI will attempt to load anyway */ }

        _db = host.Services.GetRequiredService<WebScraperDbContext>();
        _vm = new TraitCatalogViewModel(_db);
        DataContext = _vm;

        CategoryTree.ItemsSource = _vm.Categories;
        TraitGrid.ItemsSource = _vm.Traits;
        UpdateStatus();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _vm.Search = (sender as TextBox)?.Text ?? string.Empty;
        UpdateStatus();
    }

    private void CategoryTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        _vm.SelectedCategory = e.NewValue as FishBrowser.WPF.Models.TraitCategory;
        UpdateStatus();
    }

    private void TraitGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var def = TraitGrid.SelectedItem as FishBrowser.WPF.Models.TraitDefinition;
        if (def == null)
        {
            KeyText.Text = string.Empty;
            NameText.Text = string.Empty;
            TypeText.Text = string.Empty;
            DefaultText.Text = string.Empty;
            OptionGrid.ItemsSource = null;
            ConstraintsText.Text = string.Empty;
            DepsText.Text = string.Empty;
            ConflictsText.Text = string.Empty;
            DistText.Text = string.Empty;
            return;
        }
        KeyText.Text = def.Key;
        NameText.Text = def.DisplayName ?? string.Empty;
        TypeText.Text = def.ValueType.ToString();
        DefaultText.Text = def.DefaultValueJson ?? string.Empty;
        ConstraintsText.Text = Ellipsize(def.ConstraintsJson);
        DepsText.Text = Ellipsize(def.DependenciesJson);
        ConflictsText.Text = Ellipsize(def.ConflictsJson);
        DistText.Text = Ellipsize(def.DistributionsJson);

        // Load options with weights for the selected trait
        var options = _db.TraitOptions
            .Where(o => o.TraitDefinitionId == def.Id)
            .OrderByDescending(o => o.Weight)
            .Select(o => new
            {
                o.Label,
                o.Weight,
                o.Region,
                o.Vendor,
                o.DeviceClass,
                o.ValueJson
            })
            .ToList();
        OptionGrid.ItemsSource = options;
    }

    private void UpdateStatus()
    {
        var cat = _vm.SelectedCategory?.Name ?? "(全部)";
        StatusText.Text = $"分类: {cat} | 条目: {_vm.Traits.Count}";
    }

    private void FilterCheck_Changed(object sender, RoutedEventArgs e)
    {
        _vm.ShowRequiredOnly = RequiredCheck.IsChecked == true;
        _vm.ShowExperimentalOnly = ExperimentalCheck.IsChecked == true;
        _vm.ShowRandomizableOnly = RandomizableCheck.IsChecked == true;
        UpdateStatus();
    }

    private static string Ellipsize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var t = text.Replace('\n', ' ').Replace('\r', ' ');
        return t.Length > 200 ? t.Substring(0, 200) + "…" : t;
    }

    private void OptionCopyValue_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext != null)
        {
            var row = (sender as FrameworkElement).DataContext;
            var valueProperty = row.GetType().GetProperty("ValueJson");
            string val = valueProperty?.GetValue(row) as string ?? string.Empty;
            try { Clipboard.SetText(val); } catch { }
        }
    }

    private void OptionCopyAsEntry_Click(object sender, RoutedEventArgs e)
    {
        var def = TraitGrid.SelectedItem as FishBrowser.WPF.Models.TraitDefinition;
        if (def == null) return;
        if ((sender as FrameworkElement)?.DataContext != null)
        {
            var row = (sender as FrameworkElement).DataContext;
            var valueProperty = row.GetType().GetProperty("ValueJson");
            string key = def.Key;
            string val = valueProperty?.GetValue(row) as string ?? string.Empty;
            var json = $"\"{key}\": {val}";
            try { Clipboard.SetText(json); } catch { }
        }
    }

    private void OptionSendToEditor_Click(object sender, RoutedEventArgs e)
    {
        var def = TraitGrid.SelectedItem as FishBrowser.WPF.Models.TraitDefinition;
        if (def == null) return;
        if ((sender as FrameworkElement)?.DataContext != null)
        {
            var row = (sender as FrameworkElement).DataContext;
            var valueProperty = row.GetType().GetProperty("ValueJson");
            string key = def.Key;
            string val = valueProperty?.GetValue(row) as string ?? string.Empty;
            
            // 导航到元配置编辑器页面
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                var page = new TraitMetaEditorPage(key, val);
                mainWindow.MainFrame.Navigate(page);
            }
        }
    }

    private void CopyConstraints_Click(object sender, RoutedEventArgs e)
    {
        try { Clipboard.SetText(ConstraintsText.Text); } catch { }
    }

    private void CopyDeps_Click(object sender, RoutedEventArgs e)
    {
        try { Clipboard.SetText(DepsText.Text); } catch { }
    }

    private void CopyConflicts_Click(object sender, RoutedEventArgs e)
    {
        try { Clipboard.SetText(ConflictsText.Text); } catch { }
    }

    private void CopyDist_Click(object sender, RoutedEventArgs e)
    {
        try { Clipboard.SetText(DistText.Text); } catch { }
    }
}