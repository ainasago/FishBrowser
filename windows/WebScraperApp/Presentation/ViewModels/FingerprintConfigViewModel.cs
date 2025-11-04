using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.IO;
using FishBrowser.WPF.Application.DTOs;
using FishBrowser.WPF.Application.Services;
using FishBrowser.WPF.Presentation.Commands;
using FishBrowser.WPF.Services;

namespace FishBrowser.WPF.Presentation.ViewModels;

/// <summary>
/// 指纹配置 ViewModel
/// </summary>
public class FingerprintConfigViewModel : ViewModelBase
{
    private readonly FingerprintApplicationService _fingerprintService;
    private readonly ILogService _logService;

    private ObservableCollection<FingerprintDTO> _fingerprints;
    private FingerprintDTO? _selectedFingerprint;
    private FingerprintDTO? _editingFingerprint;
    private bool _isLoading;
    private string? _statusMessage;

    public ObservableCollection<FingerprintDTO> Fingerprints
    {
        get => _fingerprints;
        set => SetProperty(ref _fingerprints, value);
    }

    public FingerprintDTO? SelectedFingerprint
    {
        get => _selectedFingerprint;
        set
        {
            if (SetProperty(ref _selectedFingerprint, value))
            {
                EditingFingerprint = value != null ? CloneFingerprint(value) : null;
                OnSelectedFingerprintChanged();
            }
        }
    }

    public FingerprintDTO? EditingFingerprint
    {
        get => _editingFingerprint;
        set => SetProperty(ref _editingFingerprint, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    // 命令
    public ICommand LoadCommand { get; }
    public ICommand CreateCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ImportPresetsCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand ImportCommand { get; }

    public FingerprintConfigViewModel(
        FingerprintApplicationService fingerprintService,
        ILogService logService)
    {
        _fingerprintService = fingerprintService;
        _logService = logService;
        _fingerprints = new ObservableCollection<FingerprintDTO>();

        // 初始化命令
        LoadCommand = new RelayCommand(_ => LoadFingerprints());
        CreateCommand = new RelayCommand(_ => CreateNewFingerprint());
        SaveCommand = new RelayCommand(_ => SaveFingerprint(), _ => EditingFingerprint != null);
        DeleteCommand = new RelayCommand(_ => DeleteFingerprint(), _ => SelectedFingerprint != null);
        CancelCommand = new RelayCommand(_ => CancelEdit());
        ImportPresetsCommand = new RelayCommand(_ => ImportPresets());
        ExportCommand = new RelayCommand(_ => ExportFingerprints());
        ImportCommand = new RelayCommand(_ => ImportFingerprints());

        // 初始加载
        LoadFingerprints();
    }

    /// <summary>
    /// 加载所有指纹配置
    /// </summary>
    private void LoadFingerprints()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "加载中...";

            var fingerprints = _fingerprintService.GetAllFingerprints();
            Fingerprints.Clear();
            foreach (var fp in fingerprints)
                Fingerprints.Add(fp);

            StatusMessage = $"已加载 {fingerprints.Count} 个指纹配置";
            _logService.LogInfo("FingerprintConfigViewModel", $"Loaded {fingerprints.Count} fingerprints");
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败: {ex.Message}";
            _logService.LogError("FingerprintConfigViewModel", $"Failed to load fingerprints: {ex.Message}", ex.StackTrace);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 导出指纹配置为 JSON 文件（默认保存到应用目录 fingerprints.export.json）
    /// </summary>
    private void ExportFingerprints(string? path = null)
    {
        try
        {
            IsLoading = true;
            StatusMessage = "导出中...";

            var file = path ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fingerprints.export.json");
            var count = _fingerprintService.ExportToJson(file);

            StatusMessage = $"已导出 {count} 个指纹到: {file}";
            _logService.LogInfo("FingerprintConfigViewModel", $"Exported {count} fingerprints to {file}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"导出失败: {ex.Message}";
            _logService.LogError("FingerprintConfigViewModel", $"Failed to export: {ex.Message}", ex.StackTrace);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 从 JSON 文件导入指纹（默认读取应用目录 fingerprints.import.json）
    /// </summary>
    private void ImportFingerprints(string? path = null)
    {
        try
        {
            IsLoading = true;
            StatusMessage = "导入中...";

            var file = path ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fingerprints.import.json");
            var created = _fingerprintService.ImportFromJson(file);

            LoadFingerprints();
            StatusMessage = $"已从 {file} 导入 {created} 个指纹";
            _logService.LogInfo("FingerprintConfigViewModel", $"Imported {created} fingerprints from {file}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"导入失败: {ex.Message}";
            _logService.LogError("FingerprintConfigViewModel", $"Failed to import: {ex.Message}", ex.StackTrace);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 创建新指纹配置
    /// </summary>
    private void CreateNewFingerprint()
    {
        EditingFingerprint = new FingerprintDTO
        {
            Name = "新指纹配置",
            UserAgent = "Mozilla/5.0",
            ViewportWidth = 1920,
            ViewportHeight = 1080,
            Timezone = "Asia/Shanghai",
            Locale = "zh-CN",
            CreatedAt = DateTime.UtcNow
        };
        SelectedFingerprint = null;
        StatusMessage = "创建新指纹配置";
    }

    /// <summary>
    /// 保存指纹配置
    /// </summary>
    private void SaveFingerprint()
    {
        try
        {
            if (EditingFingerprint == null)
                return;

            IsLoading = true;
            StatusMessage = "保存中...";

            FingerprintDTO result;
            if (EditingFingerprint.Id == 0)
            {
                // 创建新的
                result = _fingerprintService.CreateFingerprint(EditingFingerprint);
                Fingerprints.Add(result);
                StatusMessage = "指纹配置创建成功";
            }
            else
            {
                // 更新现有的
                result = _fingerprintService.UpdateFingerprint(EditingFingerprint);
                var index = Fingerprints.IndexOf(SelectedFingerprint!);
                if (index >= 0)
                    Fingerprints[index] = result;
                StatusMessage = "指纹配置更新成功";
            }

            SelectedFingerprint = result;
            _logService.LogInfo("FingerprintConfigViewModel", $"Fingerprint saved: {result.Name}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存失败: {ex.Message}";
            _logService.LogError("FingerprintConfigViewModel", $"Failed to save fingerprint: {ex.Message}", ex.StackTrace);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 删除指纹配置
    /// </summary>
    private void DeleteFingerprint()
    {
        try
        {
            if (SelectedFingerprint == null)
                return;

            IsLoading = true;
            StatusMessage = "删除中...";

            _fingerprintService.DeleteFingerprint(SelectedFingerprint.Id);
            Fingerprints.Remove(SelectedFingerprint);
            SelectedFingerprint = null;
            EditingFingerprint = null;

            StatusMessage = "指纹配置删除成功";
            _logService.LogInfo("FingerprintConfigViewModel", "Fingerprint deleted");
        }
        catch (Exception ex)
        {
            StatusMessage = $"删除失败: {ex.Message}";
            _logService.LogError("FingerprintConfigViewModel", $"Failed to delete fingerprint: {ex.Message}", ex.StackTrace);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 取消编辑
    /// </summary>
    private void CancelEdit()
    {
        EditingFingerprint = null;
        StatusMessage = "已取消编辑";
    }

    /// <summary>
    /// 导入所有预设
    /// </summary>
    private void ImportPresets()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "导入预设中...";

            var count = _fingerprintService.ImportAllPresets();
            LoadFingerprints();

            StatusMessage = $"成功导入 {count} 个预设指纹";
            _logService.LogInfo("FingerprintConfigViewModel", $"Imported {count} presets");
        }
        catch (Exception ex)
        {
            StatusMessage = $"导入失败: {ex.Message}";
            _logService.LogError("FingerprintConfigViewModel", $"Failed to import presets: {ex.Message}", ex.StackTrace);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 选中指纹变更时的处理
    /// </summary>
    private void OnSelectedFingerprintChanged()
    {
        StatusMessage = SelectedFingerprint != null 
            ? $"已选中: {SelectedFingerprint.Name}" 
            : "未选中指纹";
    }

    /// <summary>
    /// 克隆指纹对象
    /// </summary>
    private static FingerprintDTO CloneFingerprint(FingerprintDTO original)
    {
        return new FingerprintDTO
        {
            Id = original.Id,
            Name = original.Name,
            UserAgent = original.UserAgent,
            AcceptLanguage = original.AcceptLanguage,
            ViewportWidth = original.ViewportWidth,
            ViewportHeight = original.ViewportHeight,
            Timezone = original.Timezone,
            Locale = original.Locale,
            Platform = original.Platform,
            CanvasFingerprint = original.CanvasFingerprint,
            WebGLRenderer = original.WebGLRenderer,
            WebGLVendor = original.WebGLVendor,
            FontPreset = original.FontPreset,
            AudioSampleRate = original.AudioSampleRate,
            DisableWebRTC = original.DisableWebRTC,
            DisableDNSLeak = original.DisableDNSLeak,
            DisableGeolocation = original.DisableGeolocation,
            RestrictPermissions = original.RestrictPermissions,
            EnableDNT = original.EnableDNT,
            CustomJavaScript = original.CustomJavaScript,
            CustomHeaders = original.CustomHeaders,
            CustomCookies = original.CustomCookies,
            DeviceMemory = original.DeviceMemory,
            ProcessorCount = original.ProcessorCount,
            IsPreset = original.IsPreset,
            CreatedAt = original.CreatedAt,
            UpdatedAt = original.UpdatedAt
        };
    }
}
