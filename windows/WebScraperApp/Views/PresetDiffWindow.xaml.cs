using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Views;

public partial class PresetDiffWindow : Window
{
    private readonly TraitGroupPreset _source;
    private readonly List<TraitGroupPreset> _others;

    public PresetDiffWindow(TraitGroupPreset source, List<TraitGroupPreset> others)
    {
        InitializeComponent();
        _source = source;
        _others = others;

        CompareCombo.ItemsSource = _others;
        CompareCombo.DisplayMemberPath = nameof(TraitGroupPreset.Name);
        if (_others.Count > 0)
        {
            CompareCombo.SelectedIndex = 0;
        }
        SourceBox.Text = PrettyJson(_source.ItemsJson);
    }

    private void CompareCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var compare = CompareCombo.SelectedItem as TraitGroupPreset;
        if (compare == null)
        {
            CompareBox.Text = string.Empty;
            StatusText.Text = string.Empty;
            return;
        }
        CompareBox.Text = PrettyJson(compare.ItemsJson);
        ComputeDiff(_source, compare);
    }

    private void ComputeDiff(TraitGroupPreset source, TraitGroupPreset compare)
    {
        try
        {
            var srcDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(source.ItemsJson ?? "{}") ?? new();
            var cmpDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(compare.ItemsJson ?? "{}") ?? new();

            var onlyInSource = srcDict.Keys.Except(cmpDict.Keys).ToList();
            var onlyInCompare = cmpDict.Keys.Except(srcDict.Keys).ToList();
            var different = srcDict.Keys.Intersect(cmpDict.Keys)
                .Where(k => JsonSerializer.Serialize(srcDict[k]) != JsonSerializer.Serialize(cmpDict[k]))
                .ToList();

            var msg = $"仅在源中: {onlyInSource.Count} | 仅在对比中: {onlyInCompare.Count} | 值不同: {different.Count}";
            if (onlyInSource.Count > 0) msg += $"\n仅在源中: {string.Join(", ", onlyInSource.Take(5))}";
            if (onlyInCompare.Count > 0) msg += $"\n仅在对比中: {string.Join(", ", onlyInCompare.Take(5))}";
            if (different.Count > 0) msg += $"\n值不同: {string.Join(", ", different.Take(5))}";
            StatusText.Text = msg;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"对比失败: {ex.Message}";
        }
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

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
