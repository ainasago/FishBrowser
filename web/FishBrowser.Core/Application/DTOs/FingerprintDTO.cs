using System;

namespace FishBrowser.WPF.Application.DTOs;

/// <summary>
/// 指纹配置数据传输对象
/// </summary>
public class FingerprintDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string UserAgent { get; set; }
    public string AcceptLanguage { get; set; }
    public int ViewportWidth { get; set; }
    public int ViewportHeight { get; set; }
    public string Timezone { get; set; }
    public string Locale { get; set; }
    public string Platform { get; set; }

    // 高级指纹
    public string CanvasFingerprint { get; set; }
    public string WebGLRenderer { get; set; }
    public string WebGLVendor { get; set; }
    public string FontPreset { get; set; }
    public string AudioSampleRate { get; set; }

    // 安全设置
    public bool DisableWebRTC { get; set; }
    public bool DisableDNSLeak { get; set; }
    public bool DisableGeolocation { get; set; }
    public bool RestrictPermissions { get; set; }
    public bool EnableDNT { get; set; }

    // 自定义属性
    public string CustomJavaScript { get; set; }
    public string CustomHeaders { get; set; }
    public string CustomCookies { get; set; }
    public int DeviceMemory { get; set; }
    public int ProcessorCount { get; set; }

    public bool IsPreset { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
