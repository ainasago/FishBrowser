using System;

namespace FishBrowser.WPF.Models;

public class BrowserEnvironment
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Group
    public int? GroupId { get; set; }
    public BrowserGroup? Group { get; set; }

    // Engine/OS
    public string Engine { get; set; } = "chrome"; // chrome | firefox | edge (reserve)
    public string EngineMode { get; set; } = "playwright"; // playwright | undetected_chrome
    public string OS { get; set; } = "windows";    // windows | android | ios | macos

    // UA - 直接存储完整的 User-Agent
    public string UaMode { get; set; } = "random"; // random | match | custom
    public string? CustomUserAgent { get; set; }
    public string? UserAgent { get; set; } = ""; // 实际使用的 User-Agent
    public string? Platform { get; set; } = "Win32"; // Win32 | MacIntel | Linux x86_64
    public string? Locale { get; set; } = "zh-CN"; // zh-CN | en-US 等
    public string? Timezone { get; set; } = "Asia/Shanghai"; // Asia/Shanghai 等

    // Proxy
    public string ProxyMode { get; set; } = "none"; // none | reference | api
    public int? ProxyRefId { get; set; }
    public string? ProxyApiUrl { get; set; }
    public string? ProxyApiAuth { get; set; }
    public string? ProxyType { get; set; }
    public string? ProxyAccount { get; set; }

    // Matching preferences
    public string? Region { get; set; }
    public string? Vendor { get; set; }
    public string? DeviceClass { get; set; }

    // Advanced toggles (store as strings/ints for flexibility)
    public string LanguageMode { get; set; } = "ip";     // ip | match | custom
    public string TimezoneMode { get; set; } = "ip";     // ip | match | custom
    public string GeolocationMode { get; set; } = "ask"; // ask | deny | ip | custom
    public string ResolutionMode { get; set; } = "real"; // real | custom
    public string FontsListMode { get; set; } = "real";  // real | custom
    public string FontsFingerprintMode { get; set; } = "noise"; // noise | real

    public string WebRtcMode { get; set; } = "hide";     // forward | hide | replace | real | disable
    public string CanvasMode { get; set; } = "noise";    // noise | real
    public string WebGLImageMode { get; set; } = "noise";// noise | real
    public string WebGLInfoMode { get; set; } = "ua_based"; // ua_based | custom | disable_hwaccel | real
    public string? WebGLVendor { get; set; }
    public string? WebGLRenderer { get; set; }
    public string WebGpuMode { get; set; } = "match_webgl"; // match_webgl | real | disable
    public string AudioContextMode { get; set; } = "noise"; // noise | real
    public bool SpeechVoicesEnabled { get; set; } = true;
    public string MediaDevicesMode { get; set; } = "noise"; // noise | real

    // Fonts
    public string FontsMode { get; set; } = "real"; // real | custom
    public string? FontsJson { get; set; } // JSON array of font names

    // Resolution custom - 覆盖 Profile 的分辨率
    public string? ResolutionCustom { get; set; } // e.g., "1920x1080" (deprecated)
    public int? CustomViewportWidth { get; set; } // 自定义宽度，覆盖 Profile
    public int? CustomViewportHeight { get; set; } // 自定义高度，覆盖 Profile

    public int? HardwareConcurrency { get; set; } = 10;
    public int? DeviceMemory { get; set; } = 8;
    public int? MaxTouchPoints { get; set; } = 0; // 触摸点数
    
    // 网络配置
    public string? ConnectionType { get; set; } // 4g | wifi 等
    public int? ConnectionRtt { get; set; } // RTT 延迟
    public double? ConnectionDownlink { get; set; } // 下载速度
    
    // Sec-CH-UA 相关
    public string? SecChUa { get; set; }
    public string? SecChUaPlatform { get; set; }
    public string? SecChUaMobile { get; set; }
    
    // Webdriver 配置
    public string WebdriverMode { get; set; } = "undefined"; // undefined | true | false | delete
    
    // Languages 和 Plugins
    public string? LanguagesJson { get; set; } // JSON 数组
    public string? PluginsJson { get; set; } // JSON 数组
    
    // 视口大小（直接存储，不依赖 Profile）
    public int ViewportWidth { get; set; } = 1920;
    public int ViewportHeight { get; set; } = 1080;
    
    // 备注
    public string? Notes { get; set; }
    public string Dnt { get; set; } = "default"; // default | on | off
    public string BatteryMode { get; set; } = "noise"; // noise | prohibit | real
    public bool PortscanProtect { get; set; } = true;
    public string TlsPolicy { get; set; } = "default"; // default | custom

    public string? LaunchArgs { get; set; }
    public string CookiesMode { get; set; } = "by_env"; // by_env | by_user
    public string MultiOpen { get; set; } = "follow_team"; // follow_team | on | off
    public string Notifications { get; set; } = "ask"; // ask | deny | allow
    public string BlockImages { get; set; } = "follow_team"; // follow_team | on | off
    public string BlockVideos { get; set; } = "follow_team";
    public string BlockSound { get; set; } = "follow_team";

    // Associations
    public int? MetaProfileId { get; set; }
    public FingerprintMetaProfile? MetaProfile { get; set; }
    public int? FingerprintProfileId { get; set; }
    public FingerprintProfile? FingerprintProfile { get; set; }

    // Session persistence
    public string? UserDataPath { get; set; }
    public bool EnablePersistence { get; set; } = true;
    public bool Headless { get; set; } = false; // 无头模式，默认有头
    public DateTime? LastLaunchedAt { get; set; }
    public int LaunchCount { get; set; } = 0;

    // Preview & timestamps
    public string? PreviewJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
