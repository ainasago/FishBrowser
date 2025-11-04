using FishBrowser.WPF.Models;
using FishBrowser.WPF.Application.DTOs;

namespace FishBrowser.WPF.Application.Mappers;

/// <summary>
/// 指纹配置映射器
/// </summary>
public static class FingerprintMapper
{
    /// <summary>
    /// 将实体转换为 DTO
    /// </summary>
    public static FingerprintDTO ToDTO(FingerprintProfile entity)
    {
        if (entity == null)
            return null;

        return new FingerprintDTO
        {
            Id = entity.Id,
            Name = entity.Name,
            UserAgent = entity.UserAgent,
            AcceptLanguage = entity.AcceptLanguage,
            ViewportWidth = entity.ViewportWidth,
            ViewportHeight = entity.ViewportHeight,
            Timezone = entity.Timezone,
            Locale = entity.Locale,
            Platform = entity.Platform,
            CanvasFingerprint = entity.CanvasFingerprint,
            WebGLRenderer = entity.WebGLRenderer,
            WebGLVendor = entity.WebGLVendor,
            FontPreset = entity.FontPreset,
            AudioSampleRate = entity.AudioSampleRate,
            DisableWebRTC = entity.DisableWebRTC,
            DisableDNSLeak = entity.DisableDNSLeak,
            DisableGeolocation = entity.DisableGeolocation,
            RestrictPermissions = entity.RestrictPermissions,
            EnableDNT = entity.EnableDNT,
            CustomJavaScript = entity.CustomJavaScript,
            CustomHeaders = entity.CustomHeaders,
            CustomCookies = entity.CustomCookies,
            DeviceMemory = entity.DeviceMemory,
            ProcessorCount = entity.ProcessorCount,
            IsPreset = entity.IsPreset,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <summary>
    /// 将 DTO 转换为实体
    /// </summary>
    public static FingerprintProfile ToEntity(FingerprintDTO dto)
    {
        if (dto == null)
            return null;

        return new FingerprintProfile
        {
            Id = dto.Id,
            Name = dto.Name,
            UserAgent = dto.UserAgent,
            AcceptLanguage = dto.AcceptLanguage,
            ViewportWidth = dto.ViewportWidth,
            ViewportHeight = dto.ViewportHeight,
            Timezone = dto.Timezone,
            Locale = dto.Locale,
            Platform = dto.Platform,
            CanvasFingerprint = dto.CanvasFingerprint,
            WebGLRenderer = dto.WebGLRenderer,
            WebGLVendor = dto.WebGLVendor,
            FontPreset = dto.FontPreset,
            AudioSampleRate = dto.AudioSampleRate,
            DisableWebRTC = dto.DisableWebRTC,
            DisableDNSLeak = dto.DisableDNSLeak,
            DisableGeolocation = dto.DisableGeolocation,
            RestrictPermissions = dto.RestrictPermissions,
            EnableDNT = dto.EnableDNT,
            CustomJavaScript = dto.CustomJavaScript,
            CustomHeaders = dto.CustomHeaders,
            CustomCookies = dto.CustomCookies,
            DeviceMemory = dto.DeviceMemory,
            ProcessorCount = dto.ProcessorCount,
            IsPreset = dto.IsPreset,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }
}
