using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;

namespace FishBrowser.WPF.Views;

public partial class SelectProxiesDialog : Window
{
    private readonly ProxyCatalogService _catalog;
    public ObservableCollection<SelectableProxy> Items { get; } = new();
    public List<int> SelectedProxyIds { get; private set; } = new();
    private int _pageIndex = 0;
    private const int PageSize = 100;
    private int _total = 0;

    public SelectProxiesDialog(ProxyCatalogService catalog)
    {
        InitializeComponent();
        _catalog = catalog;
        Grid.ItemsSource = Items;
        LoadPage();
    }

    private void LoadPage()
    {
        try
        {
            Items.Clear();
            var query = string.IsNullOrWhiteSpace(SearchBox.Text) ? null : SearchBox.Text.Trim();
            _total = _catalog.CountUngroupedProxies(query);
            var list = _catalog.GetUngroupedProxiesPaged(_pageIndex * PageSize, PageSize, query);
            foreach (var p in list)
            {
                Items.Add(new SelectableProxy
                {
                    Id = p.Id,
                    Address = p.Address,
                    Port = p.Port,
                    Protocol = p.Protocol,
                    Label = string.IsNullOrWhiteSpace(p.Label) ? $"{p.Address}:{p.Port}" : p.Label
                });
            }
            var totalPages = Math.Max(1, (int)Math.Ceiling(_total / (double)PageSize));
            PageInfo.Text = $"第 {_pageIndex + 1}/{totalPages} 页，共 {_total} 条";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Search_Click(object sender, RoutedEventArgs e)
    {
        _pageIndex = 0;
        LoadPage();
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var it in Items) it.IsSelected = true;
        Grid.Items.Refresh();
    }

    private void SelectNone_Click(object sender, RoutedEventArgs e)
    {
        foreach (var it in Items) it.IsSelected = false;
        Grid.Items.Refresh();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        SelectedProxyIds = Items.Where(i => i.IsSelected).Select(i => i.Id).ToList();
        DialogResult = true;
        Close();
    }

    private void PrevPage_Click(object sender, RoutedEventArgs e)
    {
        if (_pageIndex > 0)
        {
            _pageIndex--;
            LoadPage();
        }
    }

    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        var totalPages = Math.Max(1, (int)Math.Ceiling(_total / (double)PageSize));
        if (_pageIndex < totalPages - 1)
        {
            _pageIndex++;
            LoadPage();
        }
    }
}

public class SelectableProxy
{
    public int Id { get; set; }
    public bool IsSelected { get; set; }
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Protocol { get; set; } = "http";
    public string Label { get; set; } = string.Empty;
}
