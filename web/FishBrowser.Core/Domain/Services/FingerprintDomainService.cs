using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FishBrowser.WPF.Domain.Repositories;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;

namespace FishBrowser.WPF.Domain.Services;

/// <summary>
/// 指纹配置领域服务
/// 处理指纹配置相关的业务规则
/// </summary>
public class FingerprintDomainService
{
    private readonly IFingerprintRepository _repository;
    private readonly ILogService _logService;

    public FingerprintDomainService(
        IFingerprintRepository repository,
        ILogService logService)
    {
        _repository = repository;
        _logService = logService;
    }

    /// <summary>
    /// 创建新指纹配置
    /// </summary>
    public async Task<FingerprintProfile> CreateFingerprintAsync(
        string name,
        string userAgent,
        string locale,
        string timezone)
    {
        // 验证输入
        ValidateFingerprint(name, userAgent);

        // 检查名称是否已存在
        if (await _repository.NameExistsAsync(name))
            throw new InvalidOperationException($"指纹配置名称 '{name}' 已存在");

        // 创建新指纹
        var fingerprint = new FingerprintProfile
        {
            Name = name,
            UserAgent = userAgent,
            Locale = locale,
            Timezone = timezone,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(fingerprint);
        await _repository.SaveChangesAsync();

        _logService.LogInfo("FingerprintDomainService", $"Created fingerprint: {name}");

        return fingerprint;
    }

    /// <summary>
    /// 更新指纹配置
    /// </summary>
    public async Task<FingerprintProfile> UpdateFingerprintAsync(FingerprintProfile fingerprint)
    {
        // 验证输入
        ValidateFingerprint(fingerprint.Name, fingerprint.UserAgent);

        // 检查是否存在
        var existing = await _repository.GetByIdAsync(fingerprint.Id);
        if (existing == null)
            throw new InvalidOperationException($"指纹配置 ID {fingerprint.Id} 不存在");

        // 如果名称改变，检查新名称是否已存在
        if (existing.Name != fingerprint.Name && await _repository.NameExistsAsync(fingerprint.Name))
            throw new InvalidOperationException($"指纹配置名称 '{fingerprint.Name}' 已存在");

        // 更新时间
        fingerprint.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(fingerprint);
        await _repository.SaveChangesAsync();

        _logService.LogInfo("FingerprintDomainService", $"Updated fingerprint: {fingerprint.Name}");

        return fingerprint;
    }

    /// <summary>
    /// 删除指纹配置
    /// </summary>
    public async Task DeleteFingerprintAsync(int id)
    {
        var fingerprint = await _repository.GetByIdAsync(id);
        if (fingerprint == null)
            throw new InvalidOperationException($"指纹配置 ID {id} 不存在");

        // 不允许删除预设指纹
        if (fingerprint.IsPreset)
            throw new InvalidOperationException($"不能删除预设指纹 '{fingerprint.Name}'");

        await _repository.DeleteAsync(id);
        await _repository.SaveChangesAsync();

        _logService.LogInfo("FingerprintDomainService", $"Deleted fingerprint: {fingerprint.Name}");
    }

    /// <summary>
    /// 复制指纹配置
    /// </summary>
    public async Task<FingerprintProfile> CloneFingerprintAsync(int sourceId, string newName)
    {
        // 获取源指纹
        var source = await _repository.GetByIdAsync(sourceId);
        if (source == null)
            throw new InvalidOperationException($"源指纹 ID {sourceId} 不存在");

        // 验证新名称
        ValidateFingerprint(newName, source.UserAgent);

        if (await _repository.NameExistsAsync(newName))
            throw new InvalidOperationException($"指纹配置名称 '{newName}' 已存在");

        // 创建副本
        var clone = new FingerprintProfile
        {
            Name = newName,
            UserAgent = source.UserAgent,
            AcceptLanguage = source.AcceptLanguage,
            ViewportWidth = source.ViewportWidth,
            ViewportHeight = source.ViewportHeight,
            Timezone = source.Timezone,
            Locale = source.Locale,
            Platform = source.Platform,
            CanvasFingerprint = source.CanvasFingerprint,
            WebGLRenderer = source.WebGLRenderer,
            WebGLVendor = source.WebGLVendor,
            FontPreset = source.FontPreset,
            AudioSampleRate = source.AudioSampleRate,
            DisableWebRTC = source.DisableWebRTC,
            DisableDNSLeak = source.DisableDNSLeak,
            DisableGeolocation = source.DisableGeolocation,
            RestrictPermissions = source.RestrictPermissions,
            EnableDNT = source.EnableDNT,
            CustomJavaScript = source.CustomJavaScript,
            CustomHeaders = source.CustomHeaders,
            CustomCookies = source.CustomCookies,
            DeviceMemory = source.DeviceMemory,
            ProcessorCount = source.ProcessorCount,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(clone);
        await _repository.SaveChangesAsync();

        _logService.LogInfo("FingerprintDomainService", $"Cloned fingerprint: {source.Name} -> {newName}");

        return clone;
    }

    /// <summary>
    /// 获取所有预设指纹
    /// </summary>
    public async Task<IEnumerable<FingerprintProfile>> GetPresetsAsync()
    {
        return await _repository.GetPresetsAsync();
    }

    /// <summary>
    /// 验证指纹配置
    /// </summary>
    private void ValidateFingerprint(string name, string userAgent)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("指纹配置名称不能为空", nameof(name));

        if (string.IsNullOrWhiteSpace(userAgent))
            throw new ArgumentException("User-Agent 不能为空", nameof(userAgent));

        if (name.Length > 100)
            throw new ArgumentException("指纹配置名称不能超过 100 个字符", nameof(name));

        if (userAgent.Length > 500)
            throw new ArgumentException("User-Agent 不能超过 500 个字符", nameof(userAgent));
    }
}
