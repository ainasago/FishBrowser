using System;
using System.Collections.Generic;
using System.Linq;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Services;

/// <summary>
/// 指纹预设模板服务 - 提供预定义的指纹模板
/// </summary>
public class FingerprintPresetService : IFingerprintPresetService
{
    private readonly ILogService _logService;

    public FingerprintPresetService(ILogService logService)
    {
        _logService = logService;
    }

    /// <summary>
    /// 获取所有预设模板
    /// </summary>
    public List<FingerprintProfile> GetAllPresets()
    {
        return new List<FingerprintProfile>
        {
            GetWindowsChromePreset(),
            GetWindowsFirefoxPreset(),
            GetMacChromePreset(),
            GetMacSafariPreset(),
            GetLinuxChromePreset(),
            GetiPhonePreset(),
            GetAndroidPreset(),
            GetEdgeWindowsPreset(),
        };
    }

    /// <summary>
    /// Windows Chrome 预设
    /// </summary>
    public FingerprintProfile GetWindowsChromePreset()
    {
        return new FingerprintProfile
        {
            Name = "Windows Chrome (预设)",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36",
            AcceptLanguage = "zh-CN,zh;q=0.9,en;q=0.8",
            ViewportWidth = 1920,
            ViewportHeight = 1080,
            Timezone = "Asia/Shanghai",
            Locale = "zh-CN",
            Platform = "Win32",
            CanvasFingerprint = "默认",
            WebGLRenderer = "Intel Iris Graphics 640",
            WebGLVendor = "Intel Inc.",
            FontPreset = "Windows 标准",
            AudioSampleRate = "48000 Hz",
            DisableWebRTC = true,
            DisableDNSLeak = true,
            DisableGeolocation = true,
            RestrictPermissions = true,
            EnableDNT = true,
            DeviceMemory = 8,
            ProcessorCount = 4,
            IsPreset = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Windows Firefox 预设
    /// </summary>
    public FingerprintProfile GetWindowsFirefoxPreset()
    {
        return new FingerprintProfile
        {
            Name = "Windows Firefox (预设)",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0",
            AcceptLanguage = "zh-CN,zh;q=0.9,en;q=0.8",
            ViewportWidth = 1366,
            ViewportHeight = 768,
            Timezone = "Asia/Shanghai",
            Locale = "zh-CN",
            Platform = "Win32",
            CanvasFingerprint = "默认",
            WebGLRenderer = "ANGLE (Intel HD Graphics 630)",
            WebGLVendor = "Google Inc.",
            FontPreset = "Windows 标准",
            AudioSampleRate = "48000 Hz",
            DisableWebRTC = true,
            DisableDNSLeak = true,
            DisableGeolocation = true,
            RestrictPermissions = true,
            EnableDNT = true,
            DeviceMemory = 8,
            ProcessorCount = 4,
            IsPreset = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Mac Chrome 预设
    /// </summary>
    public FingerprintProfile GetMacChromePreset()
    {
        return new FingerprintProfile
        {
            Name = "Mac Chrome (预设)",
            UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36",
            AcceptLanguage = "en-US,en;q=0.9",
            ViewportWidth = 1440,
            ViewportHeight = 900,
            Timezone = "America/New_York",
            Locale = "en-US",
            Platform = "MacIntel",
            CanvasFingerprint = "默认",
            WebGLRenderer = "Apple M1",
            WebGLVendor = "Apple Inc.",
            FontPreset = "Mac 标准",
            AudioSampleRate = "48000 Hz",
            DisableWebRTC = true,
            DisableDNSLeak = true,
            DisableGeolocation = true,
            RestrictPermissions = true,
            EnableDNT = true,
            DeviceMemory = 16,
            ProcessorCount = 8,
            IsPreset = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Mac Safari 预设
    /// </summary>
    public FingerprintProfile GetMacSafariPreset()
    {
        return new FingerprintProfile
        {
            Name = "Mac Safari (预设)",
            UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15",
            AcceptLanguage = "en-US,en;q=0.9",
            ViewportWidth = 1440,
            ViewportHeight = 900,
            Timezone = "America/New_York",
            Locale = "en-US",
            Platform = "MacIntel",
            CanvasFingerprint = "默认",
            WebGLRenderer = "Apple M1",
            WebGLVendor = "Apple Inc.",
            FontPreset = "Mac 标准",
            AudioSampleRate = "48000 Hz",
            DisableWebRTC = true,
            DisableDNSLeak = true,
            DisableGeolocation = true,
            RestrictPermissions = true,
            EnableDNT = true,
            DeviceMemory = 16,
            ProcessorCount = 8,
            IsPreset = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Linux Chrome 预设
    /// </summary>
    public FingerprintProfile GetLinuxChromePreset()
    {
        return new FingerprintProfile
        {
            Name = "Linux Chrome (预设)",
            UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36",
            AcceptLanguage = "en-US,en;q=0.9",
            ViewportWidth = 1920,
            ViewportHeight = 1080,
            Timezone = "Europe/London",
            Locale = "en-US",
            Platform = "Linux x86_64",
            CanvasFingerprint = "默认",
            WebGLRenderer = "ANGLE (Intel HD Graphics)",
            WebGLVendor = "Google Inc.",
            FontPreset = "Linux 标准",
            AudioSampleRate = "48000 Hz",
            DisableWebRTC = true,
            DisableDNSLeak = true,
            DisableGeolocation = true,
            RestrictPermissions = true,
            EnableDNT = true,
            DeviceMemory = 8,
            ProcessorCount = 4,
            IsPreset = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// iPhone 预设
    /// </summary>
    public FingerprintProfile GetiPhonePreset()
    {
        return new FingerprintProfile
        {
            Name = "iPhone 14 (预设)",
            UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1",
            AcceptLanguage = "zh-CN,zh;q=0.9",
            ViewportWidth = 390,
            ViewportHeight = 844,
            Timezone = "Asia/Shanghai",
            Locale = "zh-CN",
            Platform = "iPhone",
            CanvasFingerprint = "默认",
            WebGLRenderer = "Apple M1",
            WebGLVendor = "Apple Inc.",
            FontPreset = "iOS 标准",
            AudioSampleRate = "48000 Hz",
            DisableWebRTC = true,
            DisableDNSLeak = true,
            DisableGeolocation = true,
            RestrictPermissions = true,
            EnableDNT = true,
            DeviceMemory = 4,
            ProcessorCount = 6,
            IsPreset = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Android 预设
    /// </summary>
    public FingerprintProfile GetAndroidPreset()
    {
        return new FingerprintProfile
        {
            Name = "Android Chrome (预设)",
            UserAgent = "Mozilla/5.0 (Linux; Android 13; SM-S901B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36",
            AcceptLanguage = "zh-CN,zh;q=0.9",
            ViewportWidth = 412,
            ViewportHeight = 915,
            Timezone = "Asia/Shanghai",
            Locale = "zh-CN",
            Platform = "Android",
            CanvasFingerprint = "默认",
            WebGLRenderer = "Adreno (TM) 8 Gen 1",
            WebGLVendor = "Qualcomm",
            FontPreset = "Android 标准",
            AudioSampleRate = "48000 Hz",
            DisableWebRTC = true,
            DisableDNSLeak = true,
            DisableGeolocation = true,
            RestrictPermissions = true,
            EnableDNT = true,
            DeviceMemory = 8,
            ProcessorCount = 8,
            IsPreset = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Windows Edge 预设
    /// </summary>
    public FingerprintProfile GetEdgeWindowsPreset()
    {
        return new FingerprintProfile
        {
            Name = "Windows Edge (预设)",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36 Edg/141.0.0.0",
            AcceptLanguage = "zh-CN,zh;q=0.9,en;q=0.8",
            ViewportWidth = 1920,
            ViewportHeight = 1080,
            Timezone = "Asia/Shanghai",
            Locale = "zh-CN",
            Platform = "Win32",
            CanvasFingerprint = "默认",
            WebGLRenderer = "Intel Iris Graphics 640",
            WebGLVendor = "Intel Inc.",
            FontPreset = "Windows 标准",
            AudioSampleRate = "48000 Hz",
            DisableWebRTC = true,
            DisableDNSLeak = true,
            DisableGeolocation = true,
            RestrictPermissions = true,
            EnableDNT = true,
            DeviceMemory = 8,
            ProcessorCount = 4,
            IsPreset = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 获取预设模板（按名称）
    /// </summary>
    public FingerprintProfile? GetPresetByName(string name)
    {
        return name switch
        {
            "Windows Chrome (预设)" => GetWindowsChromePreset(),
            "Windows Firefox (预设)" => GetWindowsFirefoxPreset(),
            "Mac Chrome (预设)" => GetMacChromePreset(),
            "Mac Safari (预设)" => GetMacSafariPreset(),
            "Linux Chrome (预设)" => GetLinuxChromePreset(),
            "iPhone 14 (预设)" => GetiPhonePreset(),
            "Android Chrome (预设)" => GetAndroidPreset(),
            "Windows Edge (预设)" => GetEdgeWindowsPreset(),
            _ => null
        };
    }

    /// <summary>
    /// 导入所有预设模板到数据库
    /// </summary>
    public void ImportAllPresetsToDatabase(IDatabaseService dbService)
    {
        try
        {
            var presets = GetAllPresets();
            int count = 0;

            foreach (var preset in presets)
            {
                // 检查是否已存在（通过名称查询所有模板）
                var allProfiles = dbService.GetAllFingerprintProfiles();
                var existing = allProfiles.FirstOrDefault(p => p.Name == preset.Name);
                if (existing == null)
                {
                    dbService.CreateFingerprintProfile(
                        preset.Name,
                        preset.UserAgent,
                        preset.Locale,
                        preset.Timezone
                    );
                    count++;
                }
            }

            _logService.LogInfo("PresetService", $"Imported {count} preset templates to database");
        }
        catch (Exception ex)
        {
            _logService.LogError("PresetService", $"Failed to import presets: {ex.Message}", ex.StackTrace);
        }
    }

    /// <summary>
    /// 恢复单个预设模板
    /// </summary>
    public FingerprintProfile? RestorePreset(string presetName)
    {
        var preset = GetPresetByName(presetName);
        if (preset != null)
        {
            _logService.LogInfo("PresetService", $"Restored preset: {presetName}");
        }
        return preset;
    }
}
