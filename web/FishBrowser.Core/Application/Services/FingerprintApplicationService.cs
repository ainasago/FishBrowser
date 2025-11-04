using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;
using FishBrowser.WPF.Application.DTOs;
using FishBrowser.WPF.Application.Mappers;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FishBrowser.WPF.Application.Services;

/// <summary>
/// 指纹配置应用服务
/// 处理指纹配置的业务逻辑
/// </summary>
public class FingerprintApplicationService
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogService _logService;
    private readonly IFingerprintPresetService _presetService;

    public FingerprintApplicationService(
        IDatabaseService databaseService,
        ILogService logService,
        IFingerprintPresetService presetService)
    {
        _databaseService = databaseService;
        _logService = logService;
        _presetService = presetService;
    }

    /// <summary>
    /// 获取所有指纹配置
    /// </summary>
    public List<FingerprintDTO> GetAllFingerprints()
    {
        try
        {
            var profiles = _databaseService.GetAllFingerprintProfiles();
            return profiles.Select(FingerprintMapper.ToDTO).ToList();
        }
        catch (Exception ex)
        {
            _logService.LogError("FingerprintApplicationService", $"Failed to get all fingerprints: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// 获取指定指纹配置
    /// </summary>
    public FingerprintDTO GetFingerprintById(int id)
    {
        try
        {
            var profile = _databaseService.GetFingerprintProfile(id);
            return FingerprintMapper.ToDTO(profile);
        }
        catch (Exception ex)
        {
            _logService.LogError("FingerprintApplicationService", $"Failed to get fingerprint: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// 创建指纹配置
    /// </summary>
    public FingerprintDTO CreateFingerprint(FingerprintDTO dto)
    {
        try
        {
            ValidateFingerprint(dto);

            var profile = FingerprintMapper.ToEntity(dto);
            profile.CreatedAt = DateTime.UtcNow;

            _databaseService.CreateFingerprintProfile(
                profile.Name,
                profile.UserAgent,
                profile.Locale,
                profile.Timezone
            );

            _logService.LogInfo("FingerprintApplicationService", $"Fingerprint created: {profile.Name}");

            // 重新获取以获得完整的数据
            var profiles = _databaseService.GetAllFingerprintProfiles();
            var created = profiles.FirstOrDefault(p => p.Name == profile.Name);
            return FingerprintMapper.ToDTO(created);
        }
        catch (Exception ex)
        {
            _logService.LogError("FingerprintApplicationService", $"Failed to create fingerprint: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// 更新指纹配置
    /// </summary>
    public FingerprintDTO UpdateFingerprint(FingerprintDTO dto)
    {
        try
        {
            ValidateFingerprint(dto);

            var profile = FingerprintMapper.ToEntity(dto);
            profile.UpdatedAt = DateTime.UtcNow;

            _databaseService.UpdateFingerprintProfile(profile);

            _logService.LogInfo("FingerprintApplicationService", $"Fingerprint updated: {profile.Name}");

            return FingerprintMapper.ToDTO(profile);
        }
        catch (Exception ex)
        {
            _logService.LogError("FingerprintApplicationService", $"Failed to update fingerprint: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// 删除指纹配置
    /// </summary>
    public void DeleteFingerprint(int id)
    {
        try
        {
            var profile = _databaseService.GetFingerprintProfile(id);
            if (profile == null)
                throw new InvalidOperationException($"Fingerprint with ID {id} not found");

            _databaseService.DeleteFingerprintProfile(id);

            _logService.LogInfo("FingerprintApplicationService", $"Fingerprint deleted: {profile.Name}");
        }
        catch (Exception ex)
        {
            _logService.LogError("FingerprintApplicationService", $"Failed to delete fingerprint: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// 导入所有预设指纹
    /// </summary>
    public int ImportAllPresets()
    {
        try
        {
            var presets = _presetService.GetAllPresets();
            var allProfiles = _databaseService.GetAllFingerprintProfiles();
            int count = 0;

            foreach (var preset in presets)
            {
                var existing = allProfiles.FirstOrDefault(p => p.Name == preset.Name);
                if (existing == null)
                {
                    _databaseService.CreateFingerprintProfile(
                        preset.Name,
                        preset.UserAgent,
                        preset.Locale,
                        preset.Timezone
                    );
                    count++;
                }
            }

            _logService.LogInfo("FingerprintApplicationService", $"Imported {count} preset fingerprints");
            return count;
        }
        catch (Exception ex)
        {
            _logService.LogError("FingerprintApplicationService", $"Failed to import presets: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// 获取所有预设指纹
    /// </summary>
    public List<FingerprintDTO> GetAllPresets()
    {
        try
        {
            var presets = _presetService.GetAllPresets();
            return presets.Select(FingerprintMapper.ToDTO).ToList();
        }
        catch (Exception ex)
        {
            _logService.LogError("FingerprintApplicationService", $"Failed to get presets: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// 将所有指纹配置导出为 JSON 文件
    /// </summary>
    public int ExportToJson(string filePath)
    {
        try
        {
            var list = GetAllFingerprints();
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(list, options);
            File.WriteAllText(filePath, json);
            _logService.LogInfo("FingerprintApplicationService", $"Exported {list.Count} fingerprints to {filePath}");
            return list.Count;
        }
        catch (Exception ex)
        {
            _logService.LogError("FingerprintApplicationService", $"Failed to export fingerprints: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// 从 JSON 文件导入指纹配置（存在同名则跳过或更新名称冲突逻辑）
    /// </summary>
    public int ImportFromJson(string filePath, bool skipExistingByName = true)
    {
        try
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("文件不存在", filePath);

            var json = File.ReadAllText(filePath);
            var imported = JsonSerializer.Deserialize<List<FingerprintDTO>>(json) ?? new List<FingerprintDTO>();

            var existing = _databaseService.GetAllFingerprintProfiles();
            int created = 0;
            foreach (var dto in imported)
            {
                if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.UserAgent))
                    continue;

                var dup = existing.FirstOrDefault(p => p.Name == dto.Name);
                if (dup != null)
                {
                    if (skipExistingByName)
                        continue;

                    // 更新现有记录
                    var entity = FingerprintMapper.ToEntity(dto);
                    entity.Id = dup.Id;
                    _databaseService.UpdateFingerprintProfile(entity);
                }
                else
                {
                    _databaseService.CreateFingerprintProfile(dto.Name, dto.UserAgent, dto.Locale, dto.Timezone);
                    created++;
                }
            }

            _logService.LogInfo("FingerprintApplicationService", $"Imported {created} fingerprints from {filePath}");
            return created;
        }
        catch (Exception ex)
        {
            _logService.LogError("FingerprintApplicationService", $"Failed to import fingerprints: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// 验证指纹配置
    /// </summary>
    private void ValidateFingerprint(FingerprintDTO dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Fingerprint name is required");

        if (string.IsNullOrWhiteSpace(dto.UserAgent))
            throw new ArgumentException("User-Agent is required");

        if (dto.ViewportWidth <= 0 || dto.ViewportHeight <= 0)
            throw new ArgumentException("Viewport dimensions must be greater than 0");
    }
}
