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
    public partial class GpuPickerDialog : Window
    {
        private IHost _host;
        private GpuCatalogService _gpuSvc;
        private ObservableCollection<GpuItemViewModel> _allGpus;
        private ObservableCollection<GpuItemViewModel> _filteredGpus;
        public List<GpuInfo> SelectedGpus { get; private set; } = new();

        public GpuPickerDialog(List<GpuInfo>? preselected = null)
        {
            InitializeComponent();
            _host = System.Windows.Application.Current.Resources["Host"] as IHost ?? throw new InvalidOperationException("Host not found");
            try
            {
                _gpuSvc = _host.Services.GetRequiredService<GpuCatalogService>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法加载 GPU 服务: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                _gpuSvc = null!;
            }
            _allGpus = new();
            _filteredGpus = new();
            GpuList.ItemsSource = _filteredGpus;
            OsFilterCombo.SelectedIndex = 0;
            BackendFilterCombo.SelectedIndex = 0;
            if (_gpuSvc != null)
            {
                _ = LoadGpusAsync(preselected);
            }
        }

        private async Task LoadGpusAsync(List<GpuInfo>? preselected)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[GpuPickerDialog] Starting LoadGpusAsync...");
                
                if (_gpuSvc == null)
                {
                    System.Diagnostics.Debug.WriteLine("[GpuPickerDialog] GpuCatalogService is null!");
                    MessageBox.Show("GPU 服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine("[GpuPickerDialog] Calling SearchWebGLAsync...");
                var gpus = await _gpuSvc.SearchWebGLAsync();
                System.Diagnostics.Debug.WriteLine($"[GpuPickerDialog] Got {gpus.Count} GPUs from service");
                
                var preselectedSet = new HashSet<int>(preselected?.Select(g => g.Id) ?? new int[0]);
                System.Diagnostics.Debug.WriteLine($"[GpuPickerDialog] Preselected count: {preselectedSet.Count}");
                
                foreach (var g in gpus)
                {
                    _allGpus.Add(new GpuItemViewModel
                    {
                        Id = g.Id,
                        Vendor = g.Vendor,
                        Renderer = g.Renderer,
                        Adapter = g.Adapter,
                        Backend = g.Backend,
                        OsSupportJson = g.OsSupportJson,
                        IsSelected = preselectedSet.Contains(g.Id)
                    });
                }
                
                System.Diagnostics.Debug.WriteLine($"[GpuPickerDialog] Added {_allGpus.Count} items to _allGpus");
                ApplyFilters();
                UpdateCount();
                System.Diagnostics.Debug.WriteLine($"[GpuPickerDialog] After ApplyFilters: {_filteredGpus.Count} items in _filteredGpus");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GpuPickerDialog] Error: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"加载 GPU 失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private void ApplyFilters()
        {
            if (_filteredGpus == null || _allGpus == null)
                return;

            var query = SearchBox?.Text?.Trim().ToLower() ?? "";
            var osFilter = (OsFilterCombo?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            var backendFilter = (BackendFilterCombo?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";

            _filteredGpus.Clear();
            foreach (var gpu in _allGpus)
            {
                if (!string.IsNullOrEmpty(query) && 
                    !gpu.Vendor.ToLower().Contains(query) && 
                    !gpu.Renderer.ToLower().Contains(query))
                    continue;

                if (!string.IsNullOrEmpty(osFilter) && 
                    (string.IsNullOrEmpty(gpu.OsSupportJson) || !gpu.OsSupportJson.Contains(osFilter)))
                    continue;

                if (!string.IsNullOrEmpty(backendFilter) && gpu.Backend != backendFilter)
                    continue;

                _filteredGpus.Add(gpu);
            }
            UpdateCount();
        }

        private void GpuCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateCount();
        }

        private void UpdateCount()
        {
            var selected = _allGpus.Count(g => g.IsSelected);
            CountText.Text = $"已选: {selected} / 显示: {_filteredGpus.Count} / 总计: {_allGpus.Count}";
        }

        private void Shuffle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var osFilter = (OsFilterCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Windows";
                var osName = string.IsNullOrEmpty(osFilter) ? "Windows" : osFilter;
                
                Task.Run(async () =>
                {
                    var gpus = await _gpuSvc.RandomSubsetAsync(osName, 3);
                    Dispatcher.Invoke(() =>
                    {
                        // 清除现有选择
                        foreach (var gpu in _allGpus)
                            gpu.IsSelected = false;
                        
                        // 选择随机 GPU
                        foreach (var gpu in gpus)
                        {
                            var item = _allGpus.FirstOrDefault(g => g.Id == gpu.Id);
                            if (item != null)
                                item.IsSelected = true;
                        }
                        UpdateCount();
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"换一换失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            SelectedGpus = _allGpus.Where(g => g.IsSelected).Select(g => new GpuInfo 
            { 
                Id = g.Id, 
                Vendor = g.Vendor, 
                Renderer = g.Renderer,
                Adapter = g.Adapter,
                Backend = g.Backend,
                OsSupportJson = g.OsSupportJson
            }).ToList();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class GpuItemViewModel
    {
        public int Id { get; set; }
        public string Vendor { get; set; } = string.Empty;
        public string Renderer { get; set; } = string.Empty;
        public string? Adapter { get; set; }
        public string Backend { get; set; } = string.Empty;
        public string? OsSupportJson { get; set; }
        public bool IsSelected { get; set; }
    }
}
