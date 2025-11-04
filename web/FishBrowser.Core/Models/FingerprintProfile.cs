using System;
using System.Collections.Generic;

namespace FishBrowser.WPF.Models;

public class FingerprintProfile
{
    // 基础信息
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string AcceptLanguage { get; set; } = "zh-CN,zh;q=0.9,en;q=0.8";
    public int ViewportWidth { get; set; } = 1366;
    public int ViewportHeight { get; set; } = 768;
    public string Timezone { get; set; } = "Asia/Shanghai";
    public string Locale { get; set; } = "zh-CN";
    public string Platform { get; set; } = "Win32";
    public string Vendor { get; set; } = "Google Inc.";  // navigator.vendor

    // 高级指纹
    public string? CanvasFingerprint { get; set; }
    public string? WebGLRenderer { get; set; }
    public string? WebGLVendor { get; set; }
    public string? FontPreset { get; set; }
    public string? AudioSampleRate { get; set; }

    // 字体配置
    // real | custom
    public string FontsMode { get; set; } = "real";
    // JSON array of font family names when mode=custom
    public string? FontsJson { get; set; }

    // WebGL / WebGPU / Audio / Speech (iter1)
    // WebGL image: noise | real
    public string WebGLImageMode { get; set; } = "noise";
    // WebGL info: ua_based | custom | disable_hwaccel | real
    public string WebGLInfoMode { get; set; } = "ua_based";
    // WebGPU: match_webgl | real | disable
    public string WebGPUMode { get; set; } = "match_webgl";
    // AudioContext: noise | real
    public string AudioContextMode { get; set; } = "noise";
    // SpeechVoices
    public bool SpeechVoicesEnabled { get; set; } = true;

    // 安全设置
    public bool DisableWebRTC { get; set; } = false;
    public bool DisableDNSLeak { get; set; } = false;
    public bool DisableGeolocation { get; set; } = false;
    public bool RestrictPermissions { get; set; } = false;
    public bool EnableDNT { get; set; } = false;

    // 自定义属性
    public string? CustomJavaScript { get; set; }
    public string? CustomHeaders { get; set; }
    public string? CustomCookies { get; set; }
    public int DeviceMemory { get; set; } = 8;
    public int ProcessorCount { get; set; } = 4;

    // 防检测配置（Cloudflare 绕过）
    public string? PluginsJson { get; set; }  // JSON array of plugin objects: [{name, filename, description}]
    public string? LanguagesJson { get; set; }  // JSON array of languages: ["zh-CN", "zh", "en-US", "en"]
    public int HardwareConcurrency { get; set; } = 8;  // CPU 核心数
    public int MaxTouchPoints { get; set; } = 0;  // 触摸点数（桌面为 0）
    public string ConnectionType { get; set; } = "4g";  // 网络类型: 4g, 3g, wifi
    public int ConnectionRtt { get; set; } = 50;  // 网络延迟 (ms)
    public double ConnectionDownlink { get; set; } = 10.0;  // 下载速度 (Mbps)
    public string? SecChUa { get; set; }  // Client Hints: sec-ch-ua
    public string? SecChUaPlatform { get; set; }  // Client Hints: sec-ch-ua-platform
    public string? SecChUaMobile { get; set; }  // Client Hints: sec-ch-ua-mobile

    // 元配置关联（可选）
    public int? MetaProfileId { get; set; }
    public FingerprintMetaProfile? MetaProfile { get; set; }

    // 生成器编译产物（运行前一次性计算，运行时直接应用）
    public string? CompiledHeadersJson { get; set; }    // 按顺序与大小写的 headers 模板
    public string? CompiledScriptsJson { get; set; }    // Canvas/WebGL/Audio/Intl/Navigator 初始化脚本
    public string? CompiledContextOptionsJson { get; set; } // Playwright context options（locale/ua/timezone/viewport/proxy 等）
    public DateTime? LastGeneratedAt { get; set; }
    public string? GeneratorVersion { get; set; }

    // 分组和校验
    public int? GroupId { get; set; }  // 所属浏览器分组
    public BrowserGroup? Group { get; set; }
    
    public int RealisticScore { get; set; } = 0;  // 真实性评分 (0-100)
    public DateTime? LastValidatedAt { get; set; }  // 最后校验时间
    public int? LastValidationReportId { get; set; }  // 最后校验报告ID
    public FingerprintValidationReport? LastValidationReport { get; set; }

    // 元数据
    public bool IsPreset { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // 导航属性
    public ICollection<ScrapingTask> ScrapingTasks { get; set; } = new List<ScrapingTask>();
    public ICollection<Article> Articles { get; set; } = new List<Article>();
    public ICollection<FingerprintValidationReport> ValidationReports { get; set; } = new List<FingerprintValidationReport>();
}
