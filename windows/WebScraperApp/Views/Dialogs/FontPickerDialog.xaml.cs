using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;

namespace FishBrowser.WPF.Views
{
    public partial class FontPickerDialog : Window
    {
        private IHost _host;
        private FontService _fontSvc;
        private ObservableCollection<FontItemViewModel> _allFonts;
        private ObservableCollection<FontItemViewModel> _filteredFonts;
        public List<string> SelectedFonts { get; private set; } = new();

        public FontPickerDialog(List<string>? preselected = null)
        {
            InitializeComponent();
            _host = System.Windows.Application.Current.Resources["Host"] as IHost ?? throw new InvalidOperationException("Host not found");
            try
            {
                _fontSvc = _host.Services.GetRequiredService<FontService>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法加载字体服务: {ex.Message}\n\n请确保应用已正确初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                _fontSvc = null!;
            }
            _allFonts = new();
            _filteredFonts = new();
            FontList.ItemsSource = _filteredFonts;
            OsFilterCombo.SelectedIndex = 0;
            CategoryFilterCombo.SelectedIndex = 0;
            if (_fontSvc != null)
            {
                _ = LoadFontsAsync(preselected);
            }
        }

        private async Task LoadFontsAsync(List<string>? preselected)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[FontPickerDialog] Starting LoadFontsAsync...");
                
                if (_fontSvc == null)
                {
                    System.Diagnostics.Debug.WriteLine("[FontPickerDialog] FontService is null!");
                    MessageBox.Show("字体服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine("[FontPickerDialog] Calling GetAllAsync...");
                var fonts = await _fontSvc.GetAllAsync();
                System.Diagnostics.Debug.WriteLine($"[FontPickerDialog] Got {fonts.Count} fonts from service");
                
                var preselectedSet = new HashSet<string>(preselected ?? new(), StringComparer.OrdinalIgnoreCase);
                System.Diagnostics.Debug.WriteLine($"[FontPickerDialog] Preselected count: {preselectedSet.Count}");
                
                foreach (var f in fonts)
                {
                    _allFonts.Add(new FontItemViewModel
                    {
                        Name = f.Name,
                        Category = f.Category,
                        Vendor = f.Vendor,
                        OsSupportJson = f.OsSupportJson,
                        IsSelected = preselectedSet.Contains(f.Name)
                    });
                }
                
                System.Diagnostics.Debug.WriteLine($"[FontPickerDialog] Added {_allFonts.Count} items to _allFonts");
                ApplyFilters();
                System.Diagnostics.Debug.WriteLine($"[FontPickerDialog] After ApplyFilters: {_filteredFonts.Count} items in _filteredFonts");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FontPickerDialog] Error: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"加载字体失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();

        private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private void ApplyFilters()
        {
            var query = SearchBox.Text?.Trim() ?? string.Empty;
            var osTag = GetComboTag(OsFilterCombo);
            var categoryTag = GetComboTag(CategoryFilterCombo);

            _filteredFonts.Clear();
            foreach (var item in _allFonts)
            {
                bool matchSearch = string.IsNullOrEmpty(query) || item.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
                bool matchOs = string.IsNullOrEmpty(osTag) || (item.OsSupportJson?.Contains(osTag) ?? false);
                bool matchCategory = string.IsNullOrEmpty(categoryTag) || item.Category.Equals(categoryTag, StringComparison.OrdinalIgnoreCase);

                if (matchSearch && matchOs && matchCategory)
                    _filteredFonts.Add(item);
            }
            UpdateCount();
        }

        private void UpdateCount()
        {
            var selected = _filteredFonts.Count(x => x.IsSelected);
            CountText.Text = $"显示 {_filteredFonts.Count} / 总计 {_allFonts.Count}  |  已选 {selected}";
        }

        private async void Shuffle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var osTag = GetComboTag(OsFilterCombo);
                var osName = osTag switch
                {
                    "Windows" => "Windows",
                    "macOS" => "macOS",
                    "Linux" => "Linux",
                    "Android" => "Android",
                    "iOS" => "iOS",
                    _ => "Windows"
                };
                var sample = await _fontSvc.RandomSubsetAsync(osName, 30);
                var sampleSet = new HashSet<string>(sample, StringComparer.OrdinalIgnoreCase);
                foreach (var item in _allFonts)
                    item.IsSelected = sampleSet.Contains(item.Name);
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"换一换失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            SelectedFonts = _allFonts.Where(x => x.IsSelected).Select(x => x.Name).ToList();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private static string? GetComboTag(ComboBox combo)
        {
            if (combo.SelectedItem is ComboBoxItem item)
                return item.Tag?.ToString();
            return null;
        }

        public class FontItemViewModel
        {
            public string Name { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string Vendor { get; set; } = string.Empty;
            public string? OsSupportJson { get; set; }
            public bool IsSelected { get; set; }
        }
    }
}
