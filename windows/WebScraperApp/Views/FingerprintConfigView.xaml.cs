using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Text.Json;
using Microsoft.Win32;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;
using FishBrowser.WPF.Application.Services;
using WpfApplication = System.Windows.Application;

namespace FishBrowser.WPF.Views;

public partial class FingerprintConfigView : Page
{
    private readonly DatabaseService _dbService;
    private readonly LogService _logService;
    private readonly FingerprintPresetService _presetService;
    private readonly FingerprintApplicationService _appService;
    private ObservableCollection<FingerprintProfile> _templates;
    private FingerprintProfile _currentTemplate;
    private CheckBox? _headerSelectAllCheckBox;

    public FingerprintConfigView()
    {
        InitializeComponent();

        // 从 DI 容器获取服务
        var host = WpfApplication.Current.Resources["Host"] as IHost;
        _dbService = host?.Services.GetRequiredService<DatabaseService>() ?? throw new InvalidOperationException("DatabaseService not found");
        _logService = host?.Services.GetRequiredService<LogService>() ?? throw new InvalidOperationException("LogService not found");
        _presetService = host?.Services.GetRequiredService<FingerprintPresetService>() ?? throw new InvalidOperationException("FingerprintPresetService not found");
        _appService = host?.Services.GetRequiredService<FingerprintApplicationService>() ?? throw new InvalidOperationException("FingerprintApplicationService not found");

        _templates = new ObservableCollection<FingerprintProfile>();
        TemplateDataGrid.ItemsSource = _templates;

        LoadTemplates();
    }

    private void OpenMetaEditor_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 导航到TraitMetaEditorPage而不是打开Window
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.MainFrame.Navigate(new TraitMetaEditorPage());
            }
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to open meta editor page: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"打开元配置编辑器失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenPresetMgmt_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 导航到PresetManagementPage而不是打开Window
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.MainFrame.Navigate(new PresetManagementPage());
            }
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to open preset management page: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"打开预设管理失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenBatch_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 导航到BatchGenerationPage而不是打开Window
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.MainFrame.Navigate(new BatchGenerationPage());
            }
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to open batch generation page: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"打开批量生成失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ExportCatalog_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var sfd = new SaveFileDialog
            {
                Title = "导出元目录",
                Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                FileName = "fingerprint.meta.catalog.json",
                AddExtension = true,
                DefaultExt = ".json",
                OverwritePrompt = true
            };
            if (sfd.ShowDialog() != true) return;

            var host = WpfApplication.Current.Resources["Host"] as IHost;
            var io = host?.Services.GetRequiredService<MetaCatalogIoService>() ?? throw new InvalidOperationException("MetaCatalogIoService not found");
            await io.ExportAsync(sfd.FileName);

            _logService.LogInfo("UI", $"Exported meta catalog to {sfd.FileName}");
            MessageBox.Show($"导出完成\n保存到: {sfd.FileName}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to export meta catalog: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"导出元目录失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ImportCatalog_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var ofd = new OpenFileDialog
            {
                Title = "导入元目录",
                Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                FileName = "fingerprint.meta.catalog.json",
                DefaultExt = ".json",
                CheckFileExists = true,
                Multiselect = false
            };
            if (ofd.ShowDialog() != true) return;

            var result = MessageBox.Show("以合并模式导入吗？\n是=合并，否=覆盖更新", "导入模式", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.Cancel) return;
            var merge = result == MessageBoxResult.Yes;

            var host = WpfApplication.Current.Resources["Host"] as IHost;
            var io = host?.Services.GetRequiredService<MetaCatalogIoService>() ?? throw new InvalidOperationException("MetaCatalogIoService not found");
            await io.ImportAsync(ofd.FileName, merge);

            _logService.LogInfo("UI", $"Imported meta catalog from {ofd.FileName} (merge={merge})");
            MessageBox.Show($"导入完成 (模式: {(merge ? "合并" : "覆盖")})\n来源: {ofd.FileName}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to import meta catalog: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"导入元目录失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenValidation_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var win = new ValidationWindow
            {
                Owner = Window.GetWindow(this)
            };
            win.ShowDialog();
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to open validation: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"打开校验窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenCatalog_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 导航到TraitCatalogPage而不是打开Window
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.MainFrame.Navigate(new TraitCatalogPage());
            }
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to open catalog page: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"打开元目录失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GenerateFromMeta_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new FingerprintMetaEditorDialog
            {
                Owner = Window.GetWindow(this)
            };
            var ok = dlg.ShowDialog();
            if (ok == true)
            {
                LoadTemplates();
            }
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to open meta editor: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"打开元配置编辑器失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadTemplates()
    {
        try
        {
            _templates.Clear();
            var templates = _dbService.GetAllFingerprintProfiles();
            foreach (var template in templates)
            {
                _templates.Add(template);
            }
            _logService.LogInfo("UI", $"Loaded {templates.Count} fingerprint templates");

            // 清空选中并同步表头复选框状态
            TemplateDataGrid.SelectedIndex = -1;
            UpdateHeaderSelectAllState();
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to load templates: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"加载模板失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void NewTemplate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _currentTemplate = new FingerprintProfile();
            ClearEditForm();
            ConfigTabControl.IsEnabled = true;
            _logService.LogInfo("UI", "Creating new fingerprint template");
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to create new template: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"创建模板失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TemplateDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (TemplateDataGrid.SelectedItem is FingerprintProfile template)
            {
                _currentTemplate = template;
                LoadTemplateToForm(template);
                ConfigTabControl.IsEnabled = true;
            }

            // 同步表头复选框三态
            UpdateHeaderSelectAllState();
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to select template: {ex.Message}", ex.StackTrace);
        }
    }

    private void HeaderSelectAllCheckBox_Loaded(object sender, RoutedEventArgs e)
    {
        _headerSelectAllCheckBox = sender as CheckBox;
        UpdateHeaderSelectAllState();
    }

    private void SelectAllCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (_headerSelectAllCheckBox == null) return;
        var state = _headerSelectAllCheckBox.IsChecked;

        if (state == true)
        {
            // 全选
            TemplateDataGrid.SelectAll();
        }
        else
        {
            // 全不选（包括不确定状态点击）
            TemplateDataGrid.UnselectAll();
        }

        UpdateHeaderSelectAllState();
    }

    private void UpdateHeaderSelectAllState()
    {
        if (_headerSelectAllCheckBox == null) return;
        var total = _templates?.Count ?? 0;
        var selected = TemplateDataGrid?.SelectedItems?.Count ?? 0;

        if (total == 0)
        {
            _headerSelectAllCheckBox.IsChecked = false;
            return;
        }

        if (selected == 0)
        {
            _headerSelectAllCheckBox.IsChecked = false;
        }
        else if (selected == total)
        {
            _headerSelectAllCheckBox.IsChecked = true;
        }
        else
        {
            _headerSelectAllCheckBox.IsChecked = null; // 不确定状态
        }
    }

    private void LoadTemplateToForm(FingerprintProfile template)
    {
        // Tab 1: 基础信息
        NameTextBox.Text = template.Name;
        UserAgentTextBox.Text = template.UserAgent;
        AcceptLanguageCombo.Text = template.AcceptLanguage ?? "zh-CN,zh;q=0.9,en;q=0.8";
        ViewportWidthBox.Text = template.ViewportWidth.ToString();
        ViewportHeightBox.Text = template.ViewportHeight.ToString();
        TimezoneCombo.Text = template.Timezone ?? "Asia/Shanghai";
        LocaleCombo.Text = template.Locale ?? "zh-CN";

        // Tab 2: 高级指纹
        CanvasFingerprintCombo.Text = template.CanvasFingerprint ?? "默认";
        WebGLRendererCombo.Text = template.WebGLRenderer ?? "Intel Iris Graphics 640";
        WebGLVendorCombo.Text = template.WebGLVendor ?? "Intel Inc.";
        FontPresetCombo.Text = template.FontPreset ?? "Windows 标准";
        AudioSampleRateCombo.Text = template.AudioSampleRate ?? "48000 Hz";

        // Tab 3: 安全设置
        DisableWebRTCCheckBox.IsChecked = template.DisableWebRTC;
        DisableDNSLeakCheckBox.IsChecked = template.DisableDNSLeak;
        DisableGeolocationCheckBox.IsChecked = template.DisableGeolocation;
        RestrictPermissionsCheckBox.IsChecked = template.RestrictPermissions;
        EnableDNTCheckBox.IsChecked = template.EnableDNT;

        // Tab 4: 自定义属性
        CustomJSTextBox.Text = template.CustomJavaScript ?? "";
        CustomHeadersTextBox.Text = template.CustomHeaders ?? "{\n  \"X-Custom-Header\": \"value\"\n}";
        CustomCookiesTextBox.Text = template.CustomCookies ?? "{\n  \"name\": \"value\"\n}";
        DeviceMemoryCombo.Text = template.DeviceMemory.ToString();
        ProcessorCountCombo.Text = template.ProcessorCount.ToString();
    }

    private void ClearEditForm()
    {
        // Tab 1: 基础信息
        NameTextBox.Text = "";
        UserAgentTextBox.Text = "";
        AcceptLanguageCombo.SelectedIndex = 0;
        ViewportWidthBox.Text = "1920";
        ViewportHeightBox.Text = "1080";
        TimezoneCombo.SelectedIndex = 0;
        LocaleCombo.SelectedIndex = 0;

        // Tab 2: 高级指纹
        CanvasFingerprintCombo.SelectedIndex = 0;
        WebGLRendererCombo.SelectedIndex = 0;
        WebGLVendorCombo.SelectedIndex = 0;
        FontPresetCombo.SelectedIndex = 0;
        AudioSampleRateCombo.SelectedIndex = 1;

        // Tab 3: 安全设置
        DisableWebRTCCheckBox.IsChecked = false;
        DisableDNSLeakCheckBox.IsChecked = false;
        DisableGeolocationCheckBox.IsChecked = false;
        RestrictPermissionsCheckBox.IsChecked = false;
        EnableDNTCheckBox.IsChecked = false;

        // Tab 4: 自定义属性
        CustomJSTextBox.Text = "";
        CustomHeadersTextBox.Text = "{\n  \"X-Custom-Header\": \"value\"\n}";
        CustomCookiesTextBox.Text = "{\n  \"name\": \"value\"\n}";
        DeviceMemoryCombo.SelectedIndex = 1;
        ProcessorCountCombo.SelectedIndex = 1;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("请输入模板名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_currentTemplate == null)
            {
                _currentTemplate = new FingerprintProfile();
            }

            // Tab 1: 基础信息
            _currentTemplate.Name = NameTextBox.Text;
            _currentTemplate.UserAgent = UserAgentTextBox.Text;
            _currentTemplate.AcceptLanguage = AcceptLanguageCombo.Text;
            _currentTemplate.ViewportWidth = int.TryParse(ViewportWidthBox.Text, out var width) ? width : 1920;
            _currentTemplate.ViewportHeight = int.TryParse(ViewportHeightBox.Text, out var height) ? height : 1080;
            _currentTemplate.Timezone = TimezoneCombo.Text;
            _currentTemplate.Locale = LocaleCombo.Text;

            // Tab 2: 高级指纹
            _currentTemplate.CanvasFingerprint = CanvasFingerprintCombo.Text;
            _currentTemplate.WebGLRenderer = WebGLRendererCombo.Text;
            _currentTemplate.WebGLVendor = WebGLVendorCombo.Text;
            _currentTemplate.FontPreset = FontPresetCombo.Text;
            _currentTemplate.AudioSampleRate = AudioSampleRateCombo.Text;

            // Tab 3: 安全设置
            _currentTemplate.DisableWebRTC = DisableWebRTCCheckBox.IsChecked ?? false;
            _currentTemplate.DisableDNSLeak = DisableDNSLeakCheckBox.IsChecked ?? false;
            _currentTemplate.DisableGeolocation = DisableGeolocationCheckBox.IsChecked ?? false;
            _currentTemplate.RestrictPermissions = RestrictPermissionsCheckBox.IsChecked ?? false;
            _currentTemplate.EnableDNT = EnableDNTCheckBox.IsChecked ?? false;

            // Tab 4: 自定义属性
            _currentTemplate.CustomJavaScript = CustomJSTextBox.Text;
            _currentTemplate.CustomHeaders = CustomHeadersTextBox.Text;
            _currentTemplate.CustomCookies = CustomCookiesTextBox.Text;
            _currentTemplate.DeviceMemory = int.TryParse(DeviceMemoryCombo.Text, out var memory) ? memory : 8;
            _currentTemplate.ProcessorCount = int.TryParse(ProcessorCountCombo.Text, out var processor) ? processor : 4;

            if (_currentTemplate.Id == 0)
            {
                _dbService.CreateFingerprintProfile(
                    _currentTemplate.Name,
                    _currentTemplate.UserAgent,
                    _currentTemplate.Locale,
                    _currentTemplate.Timezone
                );
                _logService.LogInfo("UI", $"Created fingerprint template: {_currentTemplate.Name}");
            }
            else
            {
                _logService.LogInfo("UI", $"Updated fingerprint template: {_currentTemplate.Name}");
            }

            LoadTemplates();
            MessageBox.Show("模板保存成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to save template: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"保存模板失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        ClearEditForm();
        _currentTemplate = null;
        TemplateDataGrid.SelectedIndex = -1;
        ConfigTabControl.IsEnabled = false;
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logService.LogInfo("UI", "Importing fingerprint templates via OpenFileDialog (skipExistingByName=true)");

            var ofd = new OpenFileDialog
            {
                Title = "导入指纹模板",
                Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                FileName = "fingerprints.import.json",
                DefaultExt = ".json",
                CheckFileExists = true,
                Multiselect = false
            };
            if (ofd.ShowDialog() != true) return;

            var created = _appService.ImportFromJson(ofd.FileName);

            LoadTemplates();
            MessageBox.Show($"导入完成：新增 {created} 个模板\n来源: {ofd.FileName}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to import templates: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ImportOverride_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logService.LogInfo("UI", "Importing fingerprint templates via OpenFileDialog with override (skipExistingByName=false)");

            var ofd = new OpenFileDialog
            {
                Title = "导入指纹模板(覆盖同名)",
                Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                FileName = "fingerprints.import.json",
                DefaultExt = ".json",
                CheckFileExists = true,
                Multiselect = false
            };
            if (ofd.ShowDialog() != true) return;

            var created = _appService.ImportFromJson(ofd.FileName, skipExistingByName: false);
            LoadTemplates();
            MessageBox.Show($"导入(覆盖同名)完成：新增/更新 {created} 个模板\n来源: {ofd.FileName}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to import (override) templates: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"导入(覆盖同名)失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 如果有选择则只导出选中，否则导出全部
            var selectedItems = TemplateDataGrid.SelectedItems;
            var selectedList = new List<FingerprintProfile>();
            if (selectedItems != null && selectedItems.Count > 0)
            {
                foreach (var item in selectedItems)
                {
                    if (item is FingerprintProfile fp) selectedList.Add(fp);
                }
            }

            var sfd = new SaveFileDialog
            {
                Title = selectedList.Count > 0 ? "导出选中指纹模板" : "导出全部指纹模板",
                Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                FileName = selectedList.Count > 0 ? "fingerprints.selected.export.json" : "fingerprints.export.json",
                AddExtension = true,
                DefaultExt = ".json",
                OverwritePrompt = true
            };
            if (sfd.ShowDialog() != true) return;

            if (selectedList.Count > 0)
            {
                var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                var json = System.Text.Json.JsonSerializer.Serialize(selectedList, options);
                File.WriteAllText(sfd.FileName, json);
                _logService.LogInfo("UI", $"Exported {selectedList.Count} selected fingerprints to {sfd.FileName}");
                MessageBox.Show($"导出选中完成：共 {selectedList.Count} 个模板\n保存到: {sfd.FileName}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var count = _appService.ExportToJson(sfd.FileName);
                _logService.LogInfo("UI", $"Exported all fingerprints: {count} to {sfd.FileName}");
                MessageBox.Show($"导出完成：共 {count} 个模板\n保存到: {sfd.FileName}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to export templates: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportSelected_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var selected = TemplateDataGrid.SelectedItems;
            if (selected == null || selected.Count == 0)
            {
                MessageBox.Show("请先选择要导出的模板（支持多选）", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var list = new List<FingerprintProfile>();
            foreach (var item in selected)
            {
                if (item is FingerprintProfile fp)
                {
                    list.Add(fp);
                }
            }

            if (list.Count == 0)
            {
                MessageBox.Show("所选项目无效，请重试", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var sfd = new SaveFileDialog
            {
                Title = "导出选中指纹模板",
                Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                FileName = "fingerprints.selected.export.json",
                AddExtension = true,
                DefaultExt = ".json",
                OverwritePrompt = true
            };
            if (sfd.ShowDialog() != true) return;

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(list, options);
            File.WriteAllText(sfd.FileName, json);

            _logService.LogInfo("UI", $"Exported {list.Count} selected fingerprints to {sfd.FileName}");
            MessageBox.Show($"导出选中完成：共 {list.Count} 个模板\n保存到: {sfd.FileName}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to export selected templates: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"导出选中失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 收集选中项（优先 DataGrid 选中，多选时批量）
            var selected = TemplateDataGrid.SelectedItems;
            var toDelete = new List<FingerprintProfile>();
            if (selected != null && selected.Count > 0)
            {
                foreach (var item in selected)
                {
                    if (item is FingerprintProfile fp)
                        toDelete.Add(fp);
                }
            }

            // 如果没有多选，则回退到当前单个选中
            if (toDelete.Count == 0)
            {
                if (_currentTemplate == null || _currentTemplate.Id == 0)
                {
                    MessageBox.Show("请先选择要删除的模板", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                toDelete.Add(_currentTemplate);
            }

            // 统计关联数据
            int totalTasks = 0, totalArticles = 0;
            foreach (var t in toDelete)
            {
                totalTasks += _dbService.GetAllTasks().Count(x => x.FingerprintProfileId == t.Id);
                totalArticles += _dbService.GetRecentArticles(int.MaxValue).Count(x => x.FingerprintProfileId == t.Id);
            }

            string confirmMsg = toDelete.Count == 1
                ? $"确定要删除模板 '{toDelete[0].Name}' 吗？"
                : $"确定要批量删除 {toDelete.Count} 个模板吗？\n\n选中的模板：\n{string.Join("\n", toDelete.Select(t => "• " + t.Name))}";

            if (totalTasks > 0 || totalArticles > 0)
            {
                confirmMsg += $"\n\n⚠️ 警告：此操作将级联删除：\n• {totalTasks} 个关联任务\n• {totalArticles} 篇关联文章\n\n请确认是否继续。";
            }

            var result = MessageBox.Show(confirmMsg, toDelete.Count == 1 ? "确认删除" : "确认批量删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                int deletedCount = 0;
                foreach (var t in toDelete)
                {
                    try
                    {
                        _dbService.DeleteFingerprintProfile(t.Id);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError("UI", $"Failed to delete template {t.Name}: {ex.Message}", ex.StackTrace);
                    }
                }

                _logService.LogInfo("UI", toDelete.Count == 1
                    ? $"Deleted fingerprint template: {toDelete[0].Name} (ID: {toDelete[0].Id})"
                    : $"Batch deleted {deletedCount} fingerprint templates");

                LoadTemplates();
                Cancel_Click(null, null);
                MessageBox.Show(toDelete.Count == 1 ? "模板删除成功" : $"成功删除 {deletedCount} 个模板", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to delete template: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"删除模板失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Presets_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var presets = _presetService.GetAllPresets();
            var presetNames = string.Join("\n", presets.Select(p => $"• {p.Name}"));
            
            MessageBox.Show(
                $"可用的预设模板：\n\n{presetNames}\n\n点击'一键还原'按钮导入所有预设模板到数据库。",
                "预设模板列表",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            
            _logService.LogInfo("UI", "Viewed preset templates list");
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to view presets: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"查看预设模板失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RestorePresets_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = MessageBox.Show(
                "确定要导入所有预设模板到数据库吗？\n\n这将添加 8 个预设指纹模板。",
                "一键还原预设模板",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                _presetService.ImportAllPresetsToDatabase(_dbService);
                LoadTemplates();
                MessageBox.Show("预设模板导入成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                _logService.LogInfo("UI", "Imported all preset templates");
            }
        }
        catch (Exception ex)
        {
            _logService.LogError("UI", $"Failed to restore presets: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"导入预设模板失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
