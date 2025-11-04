using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FishBrowser.WPF.Services;

public class BrowserEnvironmentService
{
    private readonly WebScraperDbContext _db;
    private readonly ILogService _log;
    private readonly UserAgentCompositionService _ua;
    private readonly AntiDetectionService _antiDetectionSvc;

    public BrowserEnvironmentService(WebScraperDbContext db, ILogService log, UserAgentCompositionService ua, AntiDetectionService antiDetectionSvc)
    {
        _db = db;
        _log = log;
        _ua = ua;
        _antiDetectionSvc = antiDetectionSvc;
    }

    private class UaResult
    {
        public string UserAgent { get; set; } = string.Empty;
        public Dictionary<string, object?> RelatedTraits { get; set; } = new();
    }

    // 根据 Engine/OS 选择 UA 生成策略（当前以安全回退至 Chrome/Windows 生成器；后续可替换为细分方法）
    private UaResult GenerateUserAgentFor(string engine, string os, string locale)
    {
        try
        {
            // 预留扩展点：不同引擎/平台调用不同生成方法
            // 示例：if (engine=="firefox") return _ua.GenerateFirefoxWindows(locale);
            var res = _ua.GenerateChromeWindows(locale: locale);
            return new UaResult
            {
                UserAgent = res.UserAgent,
                RelatedTraits = res.RelatedTraits
            };
        }
        catch
        {
            // 回退：最小可用 UA
            return new UaResult
            {
                UserAgent = $"Mozilla/5.0 ({os}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
                RelatedTraits = new Dictionary<string, object?>
                {
                    ["browser.userAgent"] = $"Mozilla/5.0 ({os}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
                    ["browser.acceptLanguage"] = locale,
                    ["system.locale"] = locale
                }
            };
        }
    }

    private static Dictionary<string, double>? _uaLangWeights;
    private static Dictionary<string, double> GetUaLangWeights()
    {
        if (_uaLangWeights != null) return _uaLangWeights;
        try
        {
            var baseDir = AppContext.BaseDirectory?.TrimEnd(System.IO.Path.DirectorySeparatorChar) ?? Environment.CurrentDirectory;
            var path = System.IO.Path.Combine(baseDir, "assets", "randomization", "randomization.json");
            if (System.IO.File.Exists(path))
            {
                using var doc = JsonDocument.Parse(System.IO.File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("uaLanguageWeights", out var node) && node.ValueKind == JsonValueKind.Object)
                {
                    var dict = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                    foreach (var prop in node.EnumerateObject())
                    {
                        if (prop.Value.TryGetDouble(out var w) && w > 0) dict[prop.Name] = w;
                    }
                    if (dict.Count > 0)
                    {
                        _uaLangWeights = dict;
                        return _uaLangWeights;
                    }
                }
            }
        }
        catch { }
        // fallback defaults
        _uaLangWeights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["en-US"] = 0.30,
            ["zh-CN"] = 0.25,
            ["es-ES"] = 0.15,
            ["pt-BR"] = 0.10,
            ["ja-JP"] = 0.08,
            ["de-DE"] = 0.06,
            ["en-GB"] = 0.06,
        };
        return _uaLangWeights;
    }

    private static System.Text.Json.JsonDocument? _randDoc;
    private static System.Text.Json.JsonDocument? LoadRandDoc()
    {
        if (_randDoc != null) return _randDoc;
        try
        {
            var baseDir = AppContext.BaseDirectory?.TrimEnd(System.IO.Path.DirectorySeparatorChar) ?? Environment.CurrentDirectory;
            var path = System.IO.Path.Combine(baseDir, "assets", "randomization", "randomization.json");
            if (System.IO.File.Exists(path))
            {
                _randDoc = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(path));
                return _randDoc;
            }
        }
        catch { }
        return null;
    }

    private static string PickIpBiasedLocale(Random rnd)
    {
        var weights = GetUaLangWeights();
        var sum = 0.0;
        foreach (var w in weights.Values) sum += w;
        if (sum <= 0) return "en-US";
        var roll = rnd.NextDouble() * sum;
        double acc = 0;
        foreach (var kv in weights)
        {
            acc += kv.Value;
            if (roll <= acc) return kv.Key;
        }
        return "en-US";
    }

    public class RandomizeOptions
    {
        public string? Engine { get; set; }
        public string? OS { get; set; }
        public string? Region { get; set; }
        public string? UaMode { get; set; }
        public string? ResolutionMode { get; set; }
        public int? HardwareConcurrency { get; set; }
        public int? DeviceMemory { get; set; }
        public string? WebRtcMode { get; set; }
        public string? CanvasMode { get; set; }
        public string? WebGLImageMode { get; set; }
        public string? WebGLInfoMode { get; set; }
        public string? WebGpuMode { get; set; }
        public string? AudioContextMode { get; set; }
        public bool? SpeechVoicesEnabled { get; set; }
        public string? FontsListMode { get; set; }
        public string? ProxyMode { get; set; }
    }

    /// <summary>
    /// 生成随机的 BrowserEnvironment 草稿（不落库），可传入可选参数做定制。
    /// </summary>
    public BrowserEnvironment BuildRandomDraft(RandomizeOptions? opts = null, string? seed = null)
    {
        var rnd = string.IsNullOrEmpty(seed) ? Random.Shared : new Random(seed.GetHashCode());
        string Pick(string[] arr, string? specified)
            => string.IsNullOrWhiteSpace(specified) ? arr[rnd.Next(arr.Length)] : specified!;
        string PickWeighted(Dictionary<string, double>? weights, string[] fallback)
        {
            if (weights == null || weights.Count == 0) return fallback[rnd.Next(fallback.Length)];
            var sum = 0.0; foreach (var v in weights.Values) sum += v; if (sum <= 0) return fallback[rnd.Next(fallback.Length)];
            var roll = rnd.NextDouble() * sum; double acc = 0; foreach (var kv in weights) { acc += kv.Value; if (roll <= acc) return kv.Key; }
            return fallback[0];
        }
        var cfg = LoadRandDoc();
        Dictionary<string, double>? ReadTopWeights(string key)
        {
            try
            {
                if (cfg != null && cfg.RootElement.TryGetProperty(key, out var node) && node.ValueKind == JsonValueKind.Object)
                {
                    var d = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                    foreach (var p in node.EnumerateObject()) if (p.Value.TryGetDouble(out var w)) d[p.Name] = w;
                    return d;
                }
            }
            catch { }
            return null;
        }
        Dictionary<string, double>? ReadSectionWeights(string section, string key)
        {
            try
            {
                if (cfg != null && cfg.RootElement.TryGetProperty(section, out var sec) && sec.ValueKind == JsonValueKind.Object)
                {
                    if (sec.TryGetProperty(key, out var node) && node.ValueKind == JsonValueKind.Object)
                    {
                        var d = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                        foreach (var p in node.EnumerateObject()) if (p.Value.TryGetDouble(out var w)) d[p.Name] = w;
                        return d;
                    }
                }
            }
            catch { }
            return null;
        }

        var engine = string.IsNullOrWhiteSpace(opts?.Engine)
            ? PickWeighted(ReadTopWeights("engineWeights"), new[] { "chrome", "firefox", "webkit", "edge", "brave" })
            : opts!.Engine!;
        var os = string.IsNullOrWhiteSpace(opts?.OS)
            ? PickWeighted(ReadTopWeights("osWeights"), new[] { "windows", "android", "ios", "macos", "linux" })
            : opts!.OS!;
        var region = Pick(new[] { "CN", "US", "JP", "DE", "UK", "IN", "BR" }, opts?.Region);
        var uaMode = Pick(new[] { "random", "match", "profile" }, opts?.UaMode);
        var resMode = Pick(new[] { "real", "custom" }, opts?.ResolutionMode);
        var webrtc = Pick(new[] { "hide", "disable", "real" }, opts?.WebRtcMode);
        var canvas = string.IsNullOrWhiteSpace(opts?.CanvasMode)
            ? PickWeighted(ReadSectionWeights(os is "android" or "ios" ? "mobile" : "desktop", "canvas"), new[] { "noise", "real" })
            : opts!.CanvasMode!;
        var webglImg = string.IsNullOrWhiteSpace(opts?.WebGLImageMode)
            ? PickWeighted(ReadSectionWeights(os is "android" or "ios" ? "mobile" : "desktop", "webglImage"), new[] { "noise", "real" })
            : opts!.WebGLImageMode!;
        var webglInfo = string.IsNullOrWhiteSpace(opts?.WebGLInfoMode)
            ? PickWeighted(ReadSectionWeights(os is "android" or "ios" ? "mobile" : "desktop", "webglInfo"), new[] { "ua_based", "custom", "real", "disable_hwaccel" })
            : opts!.WebGLInfoMode!;
        var webgpu = string.IsNullOrWhiteSpace(opts?.WebGpuMode)
            ? PickWeighted(ReadSectionWeights(os is "android" or "ios" ? "mobile" : "desktop", "webgpu"), new[] { "match_webgl", "real", "disable" })
            : opts!.WebGpuMode!;
        var audio = Pick(new[] { "noise", "real" }, opts?.AudioContextMode);
        var speech = opts?.SpeechVoicesEnabled ?? (rnd.NextDouble() < 0.7);
        var fontsMode = string.IsNullOrWhiteSpace(opts?.FontsListMode)
            ? PickWeighted(ReadSectionWeights(os is "android" or "ios" ? "mobile" : "desktop", "fonts"), new[] { "real", "custom" })
            : opts!.FontsListMode!;
        var proxyMode = Pick(new[] { "none", "reference", "api" }, opts?.ProxyMode);

        // 平台细化：移动端限制能力，禁用 WebGPU，WebGL Info 走 ua_based，不做自定义；字体采用 real
        if (os is "android" or "ios")
        {
            webglInfo = "ua_based";
            webgpu = "disable";
            fontsMode = "real";
            // Android/iOS 上 Canvas 噪音保持
        }

        // 如果是桌面端且 WebGL Info 选择 custom，则生成合理的 Vendor/Renderer
        string? glVendor = null;
        string? glRenderer = null;
        if (os is not ("android" or "ios") && string.Equals(webglInfo, "custom", StringComparison.OrdinalIgnoreCase))
        {
            (glVendor, glRenderer) = PickDesktopGpu(os, rnd);
        }
        _log.LogInfo("Randomize", $"Draft engine={engine}, os={os}, webglInfo={webglInfo}, vendor={(glVendor ?? "<null>")}, renderer={(glRenderer ?? "<null>")}");

        var hw = opts?.HardwareConcurrency ?? (os is "android" or "ios" ? rnd.Next(4, 9) : rnd.Next(4, 17)); // 移动端 4-8，桌面 4-16
        var mem = opts?.DeviceMemory ?? (os is "android" or "ios" ? new[] { 2, 4, 6, 8 }[rnd.Next(4)] : new[] { 4, 8, 16, 32 }[rnd.Next(4)]);

        // 如果是桌面且字体模式为 custom，则从字体库随机采样一组
        List<string>? fontsList = null;
        if (os is not ("android" or "ios") && string.Equals(fontsMode, "custom", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var osKey = os switch
                {
                    "windows" => "Windows",
                    "macos" => "macOS",
                    "linux" => "Linux",
                    _ => "Windows"
                };
                var all = _db.Fonts.ToList();
                var pool = all.Where(f => string.IsNullOrWhiteSpace(f.OsSupportJson) || f.OsSupportJson.Contains(osKey, StringComparison.OrdinalIgnoreCase))
                              .Select(f => f.Name)
                              .Distinct()
                              .ToList();
                if (pool.Count > 0)
                {
                    var count = Math.Min(30, pool.Count);
                    fontsList = new List<string>(count);
                    for (int i = 0; i < count; i++)
                    {
                        var idx = rnd.Next(pool.Count);
                        var name = pool[idx];
                        if (!fontsList.Contains(name)) fontsList.Add(name);
                    }
                    var head = string.Join(", ", fontsList.Take(6));
                    _log.LogInfo("Randomize", $"Fonts randomized: mode=custom, os={os}, pool={pool.Count}, pick={fontsList.Count}, head=[{head}{(fontsList.Count>6?" …":"")}] ");
                }
                else
                {
                    _log.LogWarn("Randomize", $"Fonts randomized: pool empty for os={osKey}");
                }
            }
            catch { }
        }

        return new BrowserEnvironment
        {
            Name = $"Env-{DateTime.Now:MMdd_HHmmss}",
            Engine = engine,
            OS = os,
            UaMode = uaMode,
            CustomUserAgent = null,
            ProxyMode = proxyMode,
            ProxyRefId = null,
            ProxyApiUrl = null,
            ProxyApiAuth = null,
            ProxyType = "server",
            ProxyAccount = null,
            Region = region,
            Vendor = null,
            DeviceClass = null,
            LanguageMode = "match",
            TimezoneMode = "match",
            GeolocationMode = "match",
            ResolutionMode = resMode,
            FontsListMode = fontsMode,
            FontsFingerprintMode = fontsMode,
            FontsMode = fontsMode,
            FontsJson = fontsList != null && fontsList.Count > 0 ? JsonSerializer.Serialize(fontsList) : null,
            WebRtcMode = webrtc,
            CanvasMode = canvas,
            WebGLImageMode = webglImg,
            WebGLInfoMode = webglInfo,
            WebGLVendor = glVendor,
            WebGLRenderer = glRenderer,
            WebGpuMode = webgpu,
            AudioContextMode = audio,
            SpeechVoicesEnabled = speech,
            MediaDevicesMode = "real",
            HardwareConcurrency = hw,
            DeviceMemory = mem,
            Dnt = "default",
            BatteryMode = "real",
            PortscanProtect = true,
            TlsPolicy = "default",
            LaunchArgs = null,
            CookiesMode = "by_env",
            MultiOpen = "follow_team",
            Notifications = "ask",
            BlockImages = "follow_team",
            BlockVideos = "follow_team",
            BlockSound = "follow_team"
        };
    }

    /// <summary>
    /// 从随机的 BrowserEnvironment 草稿构建一个非持久化的 FingerprintProfile（不落库）。
    /// 复用内部 UA/Traits 生成逻辑，适用于测试运行等临时场景。
    /// </summary>
    public FingerprintProfile BuildProfileFromDraft(BrowserEnvironment draft, string? seed = null)
    {
        if (draft == null) throw new ArgumentNullException(nameof(draft));
        var name = string.IsNullOrWhiteSpace(draft.Name) ? $"Env-{DateTime.Now:yyyyMMddHHmmss}" : draft.Name.Trim();

        // 1) 组装 traits（包含 UA / 语言 / 时区 / 分辨率 / WebGL 等）
        var traits = BuildTraits(draft);

        string? GetString(string key)
            => traits.TryGetValue(key, out var v) ? v?.ToString() : null;
        int GetInt(string key, int def)
            => traits.TryGetValue(key, out var v) && v is not null && int.TryParse(v.ToString(), out var n) ? n : def;

        // 2) 生成 FingerprintProfile（非持久化）
        var profile = new FingerprintProfile
        {
            Name = name + "-Profile",
            UserAgent = GetString("browser.userAgent") ?? draft.CustomUserAgent ??
                $"Mozilla/5.0 ({draft.OS ?? "Windows"}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
            AcceptLanguage = GetString("browser.acceptLanguage") ?? "zh-CN,zh;q=0.9,en;q=0.8",
            Locale = GetString("system.locale") ?? "zh-CN",
            Timezone = GetString("system.timezone") ?? "Asia/Shanghai",
            ViewportWidth = GetInt("device.viewport.width", 1366),
            ViewportHeight = GetInt("device.viewport.height", 768),
            Platform = (draft.OS ?? "windows").ToLowerInvariant() switch
            {
                "windows" => "Win32",
                "macos" => "MacIntel",
                "linux" => "Linux x86_64",
                "android" => "Android",
                "ios" => "iPhone",
                _ => "Win32"
            },
            CanvasFingerprint = draft.CanvasMode,
            WebGLImageMode = draft.WebGLImageMode ?? "noise",
            WebGLInfoMode = draft.WebGLInfoMode ?? "ua_based",
            WebGLVendor = GetString("graphics.webgl.vendor") ?? draft.WebGLVendor,
            WebGLRenderer = GetString("graphics.webgl.renderer") ?? draft.WebGLRenderer,
            WebGPUMode = draft.WebGpuMode ?? "match_webgl",
            AudioContextMode = draft.AudioContextMode ?? "noise",
            AudioSampleRate = "48000 Hz",
            SpeechVoicesEnabled = draft.SpeechVoicesEnabled,
            FontsMode = draft.FontsMode ?? draft.FontsListMode ?? "real",
            FontsJson = draft.FontsJson,
            DisableWebRTC = string.Equals(draft.WebRtcMode, "disable", StringComparison.OrdinalIgnoreCase),
            DisableDNSLeak = false,
            DisableGeolocation = false,
            RestrictPermissions = false,
            EnableDNT = false,
            DeviceMemory = draft.DeviceMemory.HasValue && draft.DeviceMemory.Value > 0 ? draft.DeviceMemory.Value : 8,
            ProcessorCount = draft.HardwareConcurrency.HasValue && draft.HardwareConcurrency.Value > 0 ? draft.HardwareConcurrency.Value : 4,
            IsPreset = false,
            CreatedAt = DateTime.UtcNow
        };

        // 3) 生成防检测数据（Cloudflare 绕过）
        _antiDetectionSvc.GenerateAntiDetectionData(profile);
        _log.LogInfo("BrowserEnvironmentService", $"Generated anti-detection data for profile: {profile.Name}");
        _log.LogInfo("BrowserEnvironmentService", $"  - Plugins: {profile.PluginsJson?.Length ?? 0} chars");
        _log.LogInfo("BrowserEnvironmentService", $"  - Languages: {profile.LanguagesJson}");
        _log.LogInfo("BrowserEnvironmentService", $"  - HardwareConcurrency: {profile.HardwareConcurrency}");
        _log.LogInfo("BrowserEnvironmentService", $"  - DeviceMemory: {profile.DeviceMemory}");
        _log.LogInfo("BrowserEnvironmentService", $"  - MaxTouchPoints: {profile.MaxTouchPoints}");
        _log.LogInfo("BrowserEnvironmentService", $"  - Connection: {profile.ConnectionType} (RTT: {profile.ConnectionRtt}ms, Downlink: {profile.ConnectionDownlink} Mbps)");
        _log.LogInfo("BrowserEnvironmentService", $"  - SecChUa: {profile.SecChUa}");

        return profile;
    }

    private static (string vendor, string renderer) PickDesktopGpu(string os, Random rnd)
    {
        // 常见 GPU 组合（简化版），按 OS 略作区分
        var listWin = new (string v, string r)[]
        {
            ("Google Inc. (Intel)", "ANGLE (Intel(R) UHD Graphics 630 Direct3D11 vs_5_0 ps_5_0)"),
            ("Google Inc. (NVIDIA)", "ANGLE (NVIDIA GeForce GTX 1650 Direct3D11 vs_5_0 ps_5_0)"),
            ("Google Inc. (AMD)", "ANGLE (AMD Radeon(TM) RX 5600 XT Direct3D11 vs_5_0 ps_5_0)"),
        };
        var listMac = new (string v, string r)[]
        {
            ("Apple Inc.", "Apple M1"),
            ("Apple Inc.", "AMD Radeon Pro 560X OpenGL Engine"),
            ("Apple Inc.", "Intel Iris Plus OpenGL Engine"),
        };
        var listLinux = new (string v, string r)[]
        {
            ("Google Inc. (Intel)", "Mesa Intel(R) UHD Graphics 620 (KBL GT2)"),
            ("Google Inc. (NVIDIA)", "NVIDIA GeForce GTX 1050/PCIe/SSE2"),
            ("Google Inc. (AMD)", "AMD Radeon RX 580 (POLARIS10, DRM 3.42.0, 5.13.0-30-generic, LLVM 12.0.0)"),
        };
        var pool = os switch
        {
            "windows" => listWin,
            "macos" => listMac,
            _ => listLinux
        };
        var pick = pool[rnd.Next(pool.Length)];
        return (pick.v, pick.r);
    }

    /// <summary>
    /// 直接创建随机环境（写库）。
    /// </summary>
    public BrowserEnvironment CreateRandomEnvironment(RandomizeOptions? opts = null, string? seed = null)
        => CreateFromDraft(BuildRandomDraft(opts, seed), seed);

    /// <summary>
    /// 从浏览器环境草稿创建环境、元配置与指纹配置。
    /// 返回持久化后的 BrowserEnvironment（包含 MetaProfileId 与 FingerprintProfileId）。
    /// </summary>
    public BrowserEnvironment CreateFromDraft(BrowserEnvironment draft, string? seed = null)
    {
        if (draft == null) throw new ArgumentNullException(nameof(draft));
        var name = string.IsNullOrWhiteSpace(draft.Name) ? $"Env-{DateTime.Now:yyyyMMddHHmmss}" : draft.Name.Trim();

        // 1) 组装 traits
        var traits = BuildTraits(draft);
        _log.LogInfo("CreateFromDraft", $"Traits built for {name}, hasWebGLVendor={traits.ContainsKey("graphics.webgl.vendor")}, hasWebGLRenderer={traits.ContainsKey("graphics.webgl.renderer")}");

        // 2) 创建 MetaProfile
        var meta = new FingerprintMetaProfile
        {
            Name = name + "-Meta",
            Version = "0.1.0",
            Source = "env",
            TraitsJson = JsonSerializer.Serialize(traits),
            LockFlagsJson = "[]",
            RandomizationPlanJson = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.FingerprintMetaProfiles.Add(meta);
        _db.SaveChanges();

        // 3) 生成 FingerprintProfile
        var depResolver = new DependencyResolutionService(_db);
        var generator = new FingerprintGeneratorService(_db, _log, depResolver);
        var profile = generator.GenerateFromMeta(meta, name + "-Profile", seed);
        _log.LogInfo("CreateFromDraft", $"Profile generated id={profile.Id}, WebGLVendor={(profile.WebGLVendor ?? "<null>")}, WebGLRenderer={(profile.WebGLRenderer ?? "<null>")}");
        _db.FingerprintProfiles.Add(profile);
        _db.SaveChanges();

        // 4) 保存 Environment
        var env = new BrowserEnvironment
        {
            Name = name,
            Engine = draft.Engine,
            OS = draft.OS,
            UaMode = draft.UaMode,
            CustomUserAgent = draft.CustomUserAgent,
            ProxyMode = draft.ProxyMode,
            ProxyRefId = draft.ProxyRefId,
            ProxyApiUrl = draft.ProxyApiUrl,
            ProxyApiAuth = draft.ProxyApiAuth,
            ProxyType = draft.ProxyType,
            ProxyAccount = draft.ProxyAccount,
            Region = draft.Region,
            Vendor = draft.Vendor,
            DeviceClass = draft.DeviceClass,
            LanguageMode = draft.LanguageMode,
            TimezoneMode = draft.TimezoneMode,
            GeolocationMode = draft.GeolocationMode,
            ResolutionMode = draft.ResolutionMode,
            FontsListMode = draft.FontsListMode,
            FontsFingerprintMode = draft.FontsFingerprintMode,
            FontsMode = draft.FontsMode,
            FontsJson = draft.FontsJson,
            WebRtcMode = draft.WebRtcMode,
            CanvasMode = draft.CanvasMode,
            WebGLImageMode = draft.WebGLImageMode,
            WebGLInfoMode = draft.WebGLInfoMode,
            WebGLVendor = draft.WebGLVendor,
            WebGLRenderer = draft.WebGLRenderer,
            WebGpuMode = draft.WebGpuMode,
            AudioContextMode = draft.AudioContextMode,
            SpeechVoicesEnabled = draft.SpeechVoicesEnabled,
            MediaDevicesMode = draft.MediaDevicesMode,
            HardwareConcurrency = draft.HardwareConcurrency,
            DeviceMemory = draft.DeviceMemory,
            Dnt = draft.Dnt,
            BatteryMode = draft.BatteryMode,
            PortscanProtect = draft.PortscanProtect,
            TlsPolicy = draft.TlsPolicy,
            LaunchArgs = draft.LaunchArgs,
            CookiesMode = draft.CookiesMode,
            MultiOpen = draft.MultiOpen,
            Notifications = draft.Notifications,
            BlockImages = draft.BlockImages,
            BlockVideos = draft.BlockVideos,
            BlockSound = draft.BlockSound,
            MetaProfileId = meta.Id,
            FingerprintProfileId = profile.Id,
            PreviewJson = BuildPreview(meta, profile, draft)
        };
        _db.BrowserEnvironments.Add(env);
        _db.SaveChanges();

        _log.LogInfo("Env", $"Created browser environment '{name}' with Profile {profile.Id}, WebGLVendor={(env.WebGLVendor ?? "<null>")}, WebGLRenderer={(env.WebGLRenderer ?? "<null>")}");
        return env;
    }

    private Dictionary<string, object?> BuildTraits(BrowserEnvironment draft)
    {
        var traits = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // UA 及相关 traits
        if (!string.Equals(draft.UaMode, "custom", StringComparison.OrdinalIgnoreCase))
        {
            // 当 LanguageMode=ip 时，不固定 Accept-Language，但 UA 语言可以倾向于 IP 常见语言
            var localePref = GuessLocale(draft);
            if (localePref == null && string.Equals(draft.LanguageMode, "ip", StringComparison.OrdinalIgnoreCase))
            {
                localePref = PickIpBiasedLocale(Random.Shared);
            }
            localePref ??= "zh-CN";
            var ua = GenerateUserAgentFor(draft.Engine, draft.OS, localePref);
            foreach (var kv in ua.RelatedTraits)
            {
                traits[kv.Key] = kv.Value;
            }
        }
        else if (!string.IsNullOrWhiteSpace(draft.CustomUserAgent))
        {
            traits["browser.userAgent"] = draft.CustomUserAgent;
        }

        // 语言/时区
        var locale = GuessLocale(draft);
        var tz = GuessTimezone(draft, locale);
        // 当选择 ip 模式时，不强行写死语言/时区，交由运行时根据 IP 决定
        if (!string.Equals(draft.LanguageMode, "ip", StringComparison.OrdinalIgnoreCase))
        {
            var loc = locale ?? "zh-CN";
            traits["browser.acceptLanguage"] = loc;
            traits["system.locale"] = loc;
        }
        if (!string.Equals(draft.TimezoneMode, "ip", StringComparison.OrdinalIgnoreCase))
        {
            var tzVal = tz ?? (locale == "en-US" ? "America/New_York" : "Asia/Shanghai");
            traits["system.timezone"] = tzVal;
        }

        // 分辨率
        if (string.Equals(draft.ResolutionMode, "real", StringComparison.OrdinalIgnoreCase))
        {
            traits["device.viewport.width"] = 1366;
            traits["device.viewport.height"] = 768;
        }

        // 设备资源
        traits["device.hardwareConcurrency"] = draft.HardwareConcurrency;
        traits["device.deviceMemory"] = draft.DeviceMemory;

        // 字体配置：当自定义时注入 fonts.mode 与 fonts.list
        var fm = draft.FontsMode ?? draft.FontsListMode;
        if (!string.IsNullOrWhiteSpace(fm))
        {
            traits["fonts.mode"] = fm;
            if (string.Equals(fm, "custom", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(draft.FontsJson))
            {
                try
                {
                    var list = JsonSerializer.Deserialize<List<string>>(draft.FontsJson!) ?? new List<string>();
                    traits["fonts.list"] = list;
                }
                catch
                {
                    traits["fonts.list"] = new List<string>();
                }
            }
        }

        // WebRTC 开关（简化：仅启用/禁用）
        bool webrtcEnabled = !string.Equals(draft.WebRtcMode, "disable", StringComparison.OrdinalIgnoreCase) &&
                              !string.Equals(draft.WebRtcMode, "hide", StringComparison.OrdinalIgnoreCase);
        traits["network.webrtc.enabled"] = webrtcEnabled;

        // Canvas/WebGL（示例）
        if (string.Equals(draft.CanvasMode, "noise", StringComparison.OrdinalIgnoreCase))
        {
            traits["graphics.canvas.noiseSeed"] = new Random().Next(1, 1000000);
        }

        // WebGL/WebGPU traits
        if (!string.IsNullOrWhiteSpace(draft.WebGLImageMode))
            traits["graphics.webgl.imageMode"] = draft.WebGLImageMode;
        if (!string.IsNullOrWhiteSpace(draft.WebGLInfoMode))
            traits["graphics.webgl.infoMode"] = draft.WebGLInfoMode;
        if (!string.IsNullOrWhiteSpace(draft.WebGpuMode))
            traits["graphics.webgpu.mode"] = draft.WebGpuMode;

        // 当 InfoMode=custom 时，确保写入 vendor/renderer，供 FingerprintGenerator 使用
        if (string.Equals(draft.WebGLInfoMode, "custom", StringComparison.OrdinalIgnoreCase))
        {
            var vendor = string.IsNullOrWhiteSpace(draft.WebGLVendor) ? null : draft.WebGLVendor;
            var renderer = string.IsNullOrWhiteSpace(draft.WebGLRenderer) ? null : draft.WebGLRenderer;
            if (vendor == null || renderer == null)
            {
                (vendor, renderer) = PickDesktopGpu(draft.OS ?? "windows", Random.Shared);
            }
            traits["graphics.webgl.vendor"] = vendor;
            traits["graphics.webgl.renderer"] = renderer;
            _log.LogInfo("Traits", $"WebGL infoMode=custom, vendor={(vendor ?? "<null>")}, renderer={(renderer ?? "<null>")}");
        }

        // Headers（保持示例数据集支持）
        if (!traits.ContainsKey("headers.order")) traits["headers.order"] = new[] { "user-agent", "accept-language" };

        return traits;
    }

    private static string? GuessLocale(BrowserEnvironment draft)
    {
        if (string.Equals(draft.LanguageMode, "custom", StringComparison.OrdinalIgnoreCase))
            return null; // 交由 UI 单独填充（后续扩展）
        if (string.Equals(draft.LanguageMode, "ip", StringComparison.OrdinalIgnoreCase))
            return null; // 交由运行时按 IP 判断
        // match：按 Region 映射
        return draft.Region?.ToUpperInvariant() switch
        {
            "CN" => "zh-CN",
            "US" => "en-US",
            "JP" => "ja-JP",
            "DE" => "de-DE",
            "UK" => "en-GB",
            "IN" => "en-IN",
            "BR" => "pt-BR",
            _ => "zh-CN"
        };
    }

    private static string? GuessTimezone(BrowserEnvironment draft, string locale)
    {
        if (string.Equals(draft.TimezoneMode, "custom", StringComparison.OrdinalIgnoreCase))
            return null;
        if (string.Equals(draft.TimezoneMode, "ip", StringComparison.OrdinalIgnoreCase))
            return null; // 交由运行时按 IP 判断
        // match：按 Region/Locale 映射
        if (!string.IsNullOrWhiteSpace(draft.Region))
        {
            switch (draft.Region.ToUpperInvariant())
            {
                case "CN": return "Asia/Shanghai";
                case "US": return "America/New_York";
                case "JP": return "Asia/Tokyo";
                case "DE": return "Europe/Berlin";
                case "UK": return "Europe/London";
                case "IN": return "Asia/Kolkata";
                case "BR": return "America/Sao_Paulo";
            }
        }
        // fallback by locale
        return locale == "en-US" ? "America/New_York" : "Asia/Shanghai";
    }

    private static string BuildPreview(FingerprintMetaProfile meta, FingerprintProfile profile, BrowserEnvironment env)
    {
        var webglVendor = profile.WebGLVendor ?? env.WebGLVendor;
        var webglRenderer = profile.WebGLRenderer ?? env.WebGLRenderer;
        var fid = ComputeFingerprintId(profile);
        System.Diagnostics.Debug.WriteLine($"[Preview] webglInfo={env.WebGLInfoMode}, vendor={(webglVendor ?? "<null>")}, renderer={(webglRenderer ?? "<null>")}");
        var preview = new
        {
            fingerprintId = fid,
            os = env.OS,
            userAgent = profile.UserAgent,
            language = profile.Locale,
            timezone = profile.Timezone,
            resolution = $"{profile.ViewportWidth}x{profile.ViewportHeight}",
            canvas = env.CanvasMode,
            webglImage = env.WebGLImageMode,
            webglInfo = env.WebGLInfoMode,
            webglVendor,
            webglRenderer,
            webrtc = env.WebRtcMode,
            audio = env.AudioContextMode,
            speech = env.SpeechVoicesEnabled ? "enabled" : "disabled",
            mediaDevices = env.MediaDevicesMode,
            hwConcurrency = env.HardwareConcurrency,
            deviceMemory = env.DeviceMemory,
            dnt = env.Dnt,
            battery = env.BatteryMode,
            portscanProtect = env.PortscanProtect
        };
        return JsonSerializer.Serialize(preview, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string ComputeFingerprintId(FingerprintProfile p)
    {
        var key = string.Join("|", new[]
        {
            p.UserAgent,
            p.Locale,
            p.Timezone,
            p.ViewportWidth.ToString(),
            p.ViewportHeight.ToString(),
            p.WebGLVendor ?? string.Empty,
            p.WebGLRenderer ?? string.Empty
        });
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
        var b64 = Convert.ToBase64String(hash)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
        return b64.Length > 22 ? b64.Substring(0, 22) : b64; // 22位短 ID
    }

    // ========== 分组管理 ==========
    public List<BrowserGroup> GetAllGroups() => _db.BrowserGroups.OrderBy(g => g.Order).ThenBy(g => g.Name).ToList();

    public BrowserGroup CreateGroup(string name, string? description = null)
    {
        var group = new BrowserGroup
        {
            Name = name,
            Description = description,
            Order = _db.BrowserGroups.Any() ? _db.BrowserGroups.Max(g => g.Order) + 1 : 0
        };
        _db.BrowserGroups.Add(group);
        _db.SaveChanges();
        _log.LogInfo("BrowserGroup", $"Created group: {name}");
        return group;
    }

    public void UpdateGroup(int groupId, string name, string? description = null)
    {
        var group = _db.BrowserGroups.FirstOrDefault(g => g.Id == groupId);
        if (group == null) throw new InvalidOperationException($"Group {groupId} not found");
        group.Name = name;
        group.Description = description;
        group.UpdatedAt = DateTime.UtcNow;
        _db.SaveChanges();
        _log.LogInfo("BrowserGroup", $"Updated group: {name}");
    }

    public void DeleteGroup(int groupId)
    {
        var group = _db.BrowserGroups.FirstOrDefault(g => g.Id == groupId);
        if (group == null) return;
        _db.BrowserGroups.Remove(group);
        _db.SaveChanges();
        _log.LogInfo("BrowserGroup", $"Deleted group: {group.Name}");
    }

    // ========== 浏览器管理 ==========
    public List<BrowserEnvironment> GetEnvironmentsByGroup(int? groupId = null)
    {
        var query = _db.BrowserEnvironments.AsQueryable();
        if (groupId.HasValue)
            query = query.Where(e => e.GroupId == groupId.Value);
        else
            query = query.Where(e => e.GroupId == null);
        return query.OrderBy(e => e.Name).ToList();
    }

    public List<BrowserEnvironment> GetAllEnvironments() => _db.BrowserEnvironments.OrderBy(e => e.Name).ToList();

    public BrowserEnvironment? GetEnvironmentById(int id) => _db.BrowserEnvironments.FirstOrDefault(e => e.Id == id);

    public void UpdateEnvironment(BrowserEnvironment env)
    {
        var existing = _db.BrowserEnvironments.FirstOrDefault(e => e.Id == env.Id);
        if (existing == null) throw new InvalidOperationException($"Environment {env.Id} not found");
        
        existing.Name = env.Name;
        existing.GroupId = env.GroupId;
        existing.Engine = env.Engine;
        existing.OS = env.OS;
        existing.UaMode = env.UaMode;
        existing.CustomUserAgent = env.CustomUserAgent;
        existing.ProxyMode = env.ProxyMode;
        existing.ProxyRefId = env.ProxyRefId;
        existing.ProxyApiUrl = env.ProxyApiUrl;
        existing.ProxyApiAuth = env.ProxyApiAuth;
        existing.ProxyType = env.ProxyType;
        existing.ProxyAccount = env.ProxyAccount;
        existing.Region = env.Region;
        existing.Vendor = env.Vendor;
        existing.DeviceClass = env.DeviceClass;
        existing.LanguageMode = env.LanguageMode;
        existing.TimezoneMode = env.TimezoneMode;
        existing.GeolocationMode = env.GeolocationMode;
        existing.ResolutionMode = env.ResolutionMode;
        existing.FontsListMode = env.FontsListMode;
        existing.FontsFingerprintMode = env.FontsFingerprintMode;
        existing.FontsMode = env.FontsMode;
        existing.FontsJson = env.FontsJson;
        existing.WebRtcMode = env.WebRtcMode;
        existing.CanvasMode = env.CanvasMode;
        existing.WebGLImageMode = env.WebGLImageMode;
        existing.WebGLInfoMode = env.WebGLInfoMode;
        existing.WebGLVendor = env.WebGLVendor;
        existing.WebGLRenderer = env.WebGLRenderer;
        existing.WebGpuMode = env.WebGpuMode;
        existing.AudioContextMode = env.AudioContextMode;
        existing.SpeechVoicesEnabled = env.SpeechVoicesEnabled;
        existing.MediaDevicesMode = env.MediaDevicesMode;
        existing.HardwareConcurrency = env.HardwareConcurrency;
        existing.DeviceMemory = env.DeviceMemory;
        existing.Dnt = env.Dnt;
        existing.BatteryMode = env.BatteryMode;
        existing.PortscanProtect = env.PortscanProtect;
        existing.TlsPolicy = env.TlsPolicy;
        existing.LaunchArgs = env.LaunchArgs;
        existing.CookiesMode = env.CookiesMode;
        existing.MultiOpen = env.MultiOpen;
        existing.Notifications = env.Notifications;
        existing.BlockImages = env.BlockImages;
        existing.BlockVideos = env.BlockVideos;
        existing.BlockSound = env.BlockSound;
        existing.UpdatedAt = DateTime.UtcNow;
        
        _db.SaveChanges();
        _log.LogInfo("BrowserEnv", $"Updated environment: {env.Name}");
    }

    public void DeleteEnvironment(int envId)
    {
        var env = _db.BrowserEnvironments.FirstOrDefault(e => e.Id == envId);
        if (env == null) return;
        
        // ⭐ 删除持久化文件（UserData 目录）
        if (!string.IsNullOrWhiteSpace(env.UserDataPath) && Directory.Exists(env.UserDataPath))
        {
            try
            {
                Directory.Delete(env.UserDataPath, recursive: true);
                _log.LogInfo("BrowserEnv", $"Deleted session data: {env.UserDataPath}");
            }
            catch (Exception ex)
            {
                _log.LogWarn("BrowserEnv", $"Failed to delete session data: {ex.Message}");
                // 继续删除数据库记录，即使文件删除失败
            }
        }
        
        _db.BrowserEnvironments.Remove(env);
        _db.SaveChanges();
        _log.LogInfo("BrowserEnv", $"Deleted environment: {env.Name}");
    }

    public void MoveEnvironmentToGroup(int envId, int? groupId)
    {
        var env = _db.BrowserEnvironments.FirstOrDefault(e => e.Id == envId);
        if (env == null) return;
        env.GroupId = groupId;
        env.UpdatedAt = DateTime.UtcNow;
        _db.SaveChanges();
        _log.LogInfo("BrowserEnv", $"Moved environment {env.Name} to group {groupId}");
    }

    // ========== 指纹配置绑定 ==========
    public void AttachProfile(int envId, int profileId)
    {
        var env = _db.BrowserEnvironments.FirstOrDefault(e => e.Id == envId) ?? throw new InvalidOperationException($"Environment {envId} not found");
        var profile = _db.FingerprintProfiles.FirstOrDefault(p => p.Id == profileId) ?? throw new InvalidOperationException($"FingerprintProfile {profileId} not found");
        env.FingerprintProfileId = profile.Id;
        env.UpdatedAt = DateTime.UtcNow;
        _db.SaveChanges();
        _log.LogInfo("BrowserEnv", $"Attached profile {profile.Name} (ID={profile.Id}) to environment {env.Name} (ID={env.Id})");
    }

    public void DetachProfile(int envId)
    {
        var env = _db.BrowserEnvironments.FirstOrDefault(e => e.Id == envId) ?? throw new InvalidOperationException($"Environment {envId} not found");
        env.FingerprintProfileId = null;
        env.UpdatedAt = DateTime.UtcNow;
        _db.SaveChanges();
        _log.LogInfo("BrowserEnv", $"Detached profile from environment {env.Name} (ID={env.Id})");
    }

    public void SwitchProfile(int envId, int profileId)
    {
        var env = _db.BrowserEnvironments.FirstOrDefault(e => e.Id == envId) ?? throw new InvalidOperationException($"Environment {envId} not found");
        var profile = _db.FingerprintProfiles.FirstOrDefault(p => p.Id == profileId) ?? throw new InvalidOperationException($"FingerprintProfile {profileId} not found");
        var oldProfileId = env.FingerprintProfileId;
        env.FingerprintProfileId = profile.Id;
        env.UpdatedAt = DateTime.UtcNow;
        _db.SaveChanges();
        _log.LogInfo("BrowserEnv", $"Switched profile for {env.Name} from {oldProfileId?.ToString() ?? "null"} to {profile.Id}");
    }
}
