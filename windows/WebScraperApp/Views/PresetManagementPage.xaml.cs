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
using WpfApplication = System.Windows.Application;

namespace FishBrowser.WPF.Views;

public partial class PresetManagementPage : Page
{
    private readonly WebScraperDbContext _db;
    private List<TraitGroupPreset> _allPresets = new();
    private TraitGroupPreset? _current;

    public PresetManagementPage()
    {
        InitializeComponent();
        var host = WpfApplication.Current.Resources["Host"] as IHost
                   ?? throw new InvalidOperationException("Host not found");
        _db = host.Services.GetRequiredService<WebScraperDbContext>();

        PresetGrid.ItemsSource = _allPresets;
        SearchBox.TextChanged += (s, e) => ReloadPresets();
        ReloadPresets();
    }

    private void ReloadPresets()
    {
        var search = SearchBox.Text?.Trim() ?? string.Empty;
        var query = _db.TraitGroupPresets.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
        _allPresets = query.OrderBy(p => p.Name).ToList();
        PresetGrid.ItemsSource = _allPresets;
        StatusText.Text = $"共 {_allPresets.Count} 个预设";
    }

    private void PresetGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _current = PresetGrid.SelectedItem as TraitGroupPreset;
        if (_current == null)
        {
            NameBox.Text = string.Empty;
            DescBox.Text = string.Empty;
            TagsBox.Text = string.Empty;
            TraitsBox.Text = string.Empty;
            return;
        }
        NameBox.Text = _current.Name;
        DescBox.Text = _current.Description ?? string.Empty;
        TagsBox.Text = _current.Tags ?? string.Empty;
        TraitsBox.Text = PrettyJson(_current.ItemsJson);
    }

    private void NewPreset_Click(object sender, RoutedEventArgs e)
    {
        var preset = new TraitGroupPreset
        {
            Name = $"Preset-{DateTime.Now:HHmmss}",
            Description = string.Empty,
            Version = "0.1.0",
            Tags = string.Empty,
            ItemsJson = "{}"
        };
        _db.TraitGroupPresets.Add(preset);
        _db.SaveChanges();
        ReloadPresets();
        PresetGrid.SelectedItem = preset;
    }

    private void EditPreset_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null)
        {
            MessageBox.Show("请先选择一个预设", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        // 编辑在右侧详情面板进行，点保存即可
        MessageBox.Show("在右侧详情面板编辑，然后点保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SavePreset_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null)
        {
            MessageBox.Show("请先选择一个预设", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            _current.Name = NameBox.Text.Trim();
            _current.Description = DescBox.Text.Trim();
            _current.Tags = TagsBox.Text.Trim();
            
            // 验证 JSON
            using var doc = JsonDocument.Parse(TraitsBox.Text);
            _current.ItemsJson = TraitsBox.Text;
            
            _db.SaveChanges();
            StatusText.Text = "已保存";
            ReloadPresets();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeletePreset_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null)
        {
            MessageBox.Show("请先选择一个预设", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var r = MessageBox.Show($"确定删除预设 '{_current.Name}' 吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r == MessageBoxResult.Yes)
        {
            _db.TraitGroupPresets.Remove(_current);
            _db.SaveChanges();
            _current = null;
            ReloadPresets();
        }
    }

    private void DuplicatePreset_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null)
        {
            MessageBox.Show("请先选择一个预设", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var copy = new TraitGroupPreset
        {
            Name = $"{_current.Name}-Copy-{DateTime.Now:HHmmss}",
            Description = _current.Description,
            Version = _current.Version,
            Tags = _current.Tags,
            ItemsJson = _current.ItemsJson
        };
        _db.TraitGroupPresets.Add(copy);
        _db.SaveChanges();
        ReloadPresets();
        PresetGrid.SelectedItem = copy;
    }

    private void DiffPreset_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null)
        {
            MessageBox.Show("请先选择一个预设", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var others = _allPresets.Where(p => p.Id != _current.Id).ToList();
        if (others.Count == 0)
        {
            MessageBox.Show("没有其他预设可对比", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var win = new PresetDiffWindow(_current, others) { Owner = Window.GetWindow(this) };
        win.ShowDialog();
    }

    private void ExportPreset_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null)
        {
            MessageBox.Show("请先选择一个预设", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var sfd = new SaveFileDialog { Filter = "JSON 文件|*.json", FileName = $"{_current.Name}.json" };
        if (sfd.ShowDialog() == true)
        {
            try
            {
                var dto = new { _current.Name, _current.Description, _current.Version, _current.Tags, _current.ItemsJson };
                var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(sfd.FileName, json);
                MessageBox.Show("导出成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ImportPreset_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog { Filter = "JSON 文件|*.json" };
        if (ofd.ShowDialog() == true)
        {
            try
            {
                var json = File.ReadAllText(ofd.FileName);
                var dto = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
                if (dto == null) throw new InvalidOperationException("Invalid JSON");
                
                var preset = new TraitGroupPreset
                {
                    Name = (dto.TryGetValue("Name", out var n) ? n?.ToString() : null) ?? $"Imported-{DateTime.Now:HHmmss}",
                    Description = dto.TryGetValue("Description", out var d) ? d?.ToString() : null,
                    Version = dto.TryGetValue("Version", out var v) ? v?.ToString() : "0.1.0",
                    Tags = dto.TryGetValue("Tags", out var t) ? t?.ToString() : null,
                    ItemsJson = dto.TryGetValue("ItemsJson", out var ij) ? ij?.ToString() : "{}"
                };
                _db.TraitGroupPresets.Add(preset);
                _db.SaveChanges();
                ReloadPresets();
                PresetGrid.SelectedItem = preset;
                MessageBox.Show("导入成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void TagsBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        TagsPlaceholderText.Visibility = string.IsNullOrEmpty(TagsBox.Text) ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string PrettyJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return "{}";
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch { return json; }
    }
}