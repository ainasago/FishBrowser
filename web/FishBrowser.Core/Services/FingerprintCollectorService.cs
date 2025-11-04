using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FishBrowser.WPF.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FishBrowser.WPF.Services
{
    /// <summary>
    /// 通用指纹收集服务 - 从浏览器收集完整的指纹信息并生成 JSON
    /// </summary>
    public class FingerprintCollectorService
    {
        private readonly ILogService _log;
        private readonly string _collectorScriptPath;

        public FingerprintCollectorService(ILogService logService)
        {
            _log = logService;
            _collectorScriptPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "assets", "scripts", "fingerprint-collector.js"
            );
        }

        /// <summary>
        /// 从 FingerprintProfile 生成完整的 JSON 指纹信息
        /// </summary>
        /// <param name="profile">指纹配置</param>
        /// <param name="webdriverMode">Webdriver 模式（undefined/true/false/delete），默认为 undefined</param>
        public string GenerateFingerprintJson(FingerprintProfile profile, string? webdriverMode = null)
        {
            try
            {
                // 解析 Plugins JSON
                object? pluginsList = null;
                int pluginsCount = 0;
                if (!string.IsNullOrEmpty(profile.PluginsJson))
                {
                    try
                    {
                        pluginsList = JsonSerializer.Deserialize<object>(profile.PluginsJson);
                        if (pluginsList is JsonElement element && element.ValueKind == JsonValueKind.Array)
                        {
                            pluginsCount = element.GetArrayLength();
                        }
                    }
                    catch { }
                }

                // 解析 Languages JSON
                string[]? languagesList = null;
                if (!string.IsNullOrEmpty(profile.LanguagesJson))
                {
                    try
                    {
                        languagesList = JsonSerializer.Deserialize<string[]>(profile.LanguagesJson);
                    }
                    catch { }
                }

                // 解析 Fonts JSON
                string[]? fontsList = null;
                if (!string.IsNullOrEmpty(profile.FontsJson))
                {
                    try
                    {
                        fontsList = JsonSerializer.Deserialize<string[]>(profile.FontsJson);
                    }
                    catch { }
                }

                // ⭐ 根据 Platform 或直接使用 Profile.Vendor
                var vendor = !string.IsNullOrEmpty(profile.Vendor) 
                    ? profile.Vendor 
                    : (profile.Platform ?? "Win32") switch
                    {
                        "iPhone" or "iPad" or "MacIntel" => "Apple Computer, Inc.",
                        "Linux armv8l" => "Google Inc.",  // Android
                        _ => "Google Inc."  // Windows/Linux
                    };
                
                var fingerprintData = new
                {
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    basic = new
                    {
                        userAgent = profile.UserAgent ?? "",
                        platform = profile.Platform ?? "Win32",
                        language = profile.Locale ?? "zh-CN",
                        languages = languagesList ?? new[] { profile.Locale ?? "zh-CN" },
                        vendor = vendor,
                        appVersion = profile.UserAgent?.Replace("Mozilla/", "") ?? "",
                        appName = "Netscape",
                        appCodeName = "Mozilla",
                        product = "Gecko",
                        productSub = "20030107",
                        cookieEnabled = true,
                        onLine = true,
                        doNotTrack = (string?)null
                    },
                    automation = new
                    {
                        webdriver = GetWebdriverValue(webdriverMode),
                        _phantom = false,
                        _selenium = false,
                        callPhantom = false,
                        __nightmare = false,
                        __webdriver_script_fn = false,
                        domAutomation = false,
                        domAutomationController = false,
                        cdc_variables = Array.Empty<string>(),
                        chrome_runtime = false,
                        chrome_loadTimes = true,
                        chrome_csi = true
                    },
                    hardware = new
                    {
                        hardwareConcurrency = profile.HardwareConcurrency,
                        deviceMemory = profile.DeviceMemory,
                        maxTouchPoints = profile.MaxTouchPoints,
                        connection = new
                        {
                            effectiveType = profile.ConnectionType ?? "4g",
                            rtt = profile.ConnectionRtt,
                            downlink = profile.ConnectionDownlink,
                            saveData = false
                        }
                    },
                    plugins = new
                    {
                        count = pluginsCount,
                        list = pluginsList ?? new object()
                    },
                    mimeTypes = new
                    {
                        count = 2,
                        list = new[]
                        {
                            new { type = "application/pdf", suffixes = "pdf", description = "Portable Document Format" },
                            new { type = "text/pdf", suffixes = "pdf", description = "Portable Document Format" }
                        }
                    },
                    screen = new
                    {
                        width = profile.ViewportWidth,
                        height = profile.ViewportHeight,
                        availWidth = profile.ViewportWidth,
                        availHeight = profile.ViewportHeight,
                        colorDepth = 24,
                        pixelDepth = 24,
                        orientation = "landscape-primary",
                        devicePixelRatio = 1
                    },
                    canvas = new
                    {
                        hash = "NiaGQAMRYAHSQOCYWOtAgAVI65hnNsoGIsACpIHAMbHWgcD/AQAA///7F05aAAAABklEQVQDAEcX/r94KdwxAAAAAElFTkSuQmCC",
                        length = 5290
                    },
                    webgl = new
                    {
                        supported = true,
                        vendor = profile.WebGLVendor ?? "WebKit",
                        renderer = profile.WebGLRenderer ?? "WebKit WebGL",
                        version = "WebGL 1.0 (OpenGL ES 2.0 Chromium)",
                        shadingLanguageVersion = "WebGL GLSL ES 1.0 (OpenGL ES GLSL ES 1.0 Chromium)",
                        unmaskedVendor = profile.WebGLVendor ?? "Google Inc. (AMD)",
                        unmaskedRenderer = profile.WebGLRenderer ?? "ANGLE (AMD, AMD Radeon(TM) Graphics Direct3D11)",
                        extensions = new[]
                        {
                            "ANGLE_instanced_arrays", "EXT_blend_minmax", "EXT_clip_control",
                            "EXT_color_buffer_half_float", "EXT_depth_clamp", "EXT_disjoint_timer_query",
                            "EXT_float_blend", "EXT_frag_depth", "EXT_polygon_offset_clamp",
                            "EXT_shader_texture_lod", "EXT_texture_compression_bptc", "EXT_texture_compression_rgtc",
                            "EXT_texture_filter_anisotropic", "EXT_texture_mirror_clamp_to_edge", "EXT_sRGB",
                            "KHR_parallel_shader_compile", "OES_element_index_uint", "OES_fbo_render_mipmap",
                            "OES_standard_derivatives", "OES_texture_float", "OES_texture_float_linear",
                            "OES_texture_half_float", "OES_texture_half_float_linear", "OES_vertex_array_object",
                            "WEBGL_blend_func_extended", "WEBGL_color_buffer_float", "WEBGL_compressed_texture_s3tc",
                            "WEBGL_compressed_texture_s3tc_srgb", "WEBGL_debug_renderer_info", "WEBGL_debug_shaders",
                            "WEBGL_depth_texture", "WEBGL_draw_buffers", "WEBGL_lose_context",
                            "WEBGL_multi_draw", "WEBGL_polygon_mode"
                        },
                        maxTextureSize = 16384,
                        maxViewportDims = new { _0 = 32767, _1 = 32767 },
                        maxRenderbufferSize = 16384,
                        aliasedLineWidthRange = new { _0 = 1, _1 = 1 },
                        aliasedPointSizeRange = new { _0 = 1, _1 = 1024 }
                    },
                    webgpu = new
                    {
                        supported = true
                    },
                    audio = new { },
                    fonts = new
                    {
                        detected = fontsList ?? GetDefaultFonts(profile.Platform ?? "Win32"),
                        count = fontsList?.Length ?? GetDefaultFonts(profile.Platform ?? "Win32").Length
                    },
                    timezone = new
                    {
                        offset = GetTimezoneOffset(profile.Timezone ?? "Asia/Shanghai"),
                        timezone = profile.Timezone ?? "Asia/Shanghai",
                        locale = profile.Locale ?? "zh-CN"
                    },
                    permissions = new
                    {
                        notification = "default"
                    },
                    battery = "supported",
                    mediaDevices = new
                    {
                        supported = true,
                        enumerateDevices = true
                    },
                    serviceWorker = new
                    {
                        supported = true
                    },
                    bluetooth = new
                    {
                        supported = true
                    },
                    usb = new
                    {
                        supported = true
                    },
                    speechSynthesis = new
                    {
                        supported = true,
                        voicesCount = 0
                    },
                    performance = new
                    {
                        memory = new
                        {
                            jsHeapSizeLimit = 4294705152,
                            totalJSHeapSize = 2921071,
                            usedJSHeapSize = 2001959
                        },
                        navigation = new
                        {
                            type = 0,
                            redirectCount = 0
                        },
                        timing = new
                        {
                            navigationStart = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            loadEventEnd = 0,
                            domContentLoadedEventEnd = 0
                        }
                    },
                    window = new
                    {
                        innerWidth = profile.ViewportWidth,
                        innerHeight = profile.ViewportHeight,
                        outerWidth = profile.ViewportWidth + 15,
                        outerHeight = profile.ViewportHeight + 88,
                        screenX = 10,
                        screenY = 10,
                        pageXOffset = 0,
                        pageYOffset = 0
                    },
                    document = new
                    {
                        characterSet = "UTF-8",
                        compatMode = "CSS1Compat",
                        hidden = false,
                        visibilityState = "visible"
                    },
                    errorStack = new
                    {
                        stack = "Error: test\\n    at <anonymous>:325:23\\n    at <anonymous>:332:11\\n    at <anonymous>:351:3",
                        stackLength = 88
                    }
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                return JsonSerializer.Serialize(fingerprintData, options);
            }
            catch (Exception ex)
            {
                _log.LogError("FingerprintCollector", $"Failed to generate fingerprint JSON: {ex.Message}");
                return "{}";
            }
        }

        /// <summary>
        /// 根据平台获取默认字体列表
        /// </summary>
        private string[] GetDefaultFonts(string platform)
        {
            return platform switch
            {
                "iPhone" or "iPad" => new[]
                {
                    "Helvetica Neue", "San Francisco", "PingFang SC", "Hiragino Sans",
                    "Arial", "Verdana", "Times New Roman", "Courier New"
                },
                "MacIntel" => new[]
                {
                    "Helvetica Neue", "San Francisco", "PingFang SC", "Hiragino Sans",
                    "Arial", "Verdana", "Times New Roman", "Courier New", "Georgia"
                },
                "Linux armv8l" => new[]  // Android
                {
                    "Roboto", "Noto Sans", "Droid Sans", "Arial", "Verdana"
                },
                _ => new[]  // Windows/Linux
                {
                    "Arial", "Verdana", "Times New Roman", "Courier New",
                    "Georgia", "Comic Sans MS", "Trebuchet MS", "Impact"
                }
            };
        }

        /// <summary>
        /// 根据时区名称获取 UTC 偏移量（分钟）
        /// </summary>
        private int GetTimezoneOffset(string timezoneName)
        {
            try
            {
                var timezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneName);
                var offset = timezone.GetUtcOffset(DateTime.UtcNow);
                return -(int)offset.TotalMinutes;  // JavaScript 使用负值表示东时区
            }
            catch
            {
                // 如果找不到时区，返回常见时区的默认值
                return timezoneName switch
                {
                    "Asia/Shanghai" or "Asia/Hong_Kong" or "Asia/Singapore" => -480,  // UTC+8
                    "America/New_York" => 300,  // UTC-5 (EST)
                    "America/Los_Angeles" => 480,  // UTC-8 (PST)
                    "Europe/London" => 0,  // UTC+0 (GMT)
                    "Europe/Paris" => -60,  // UTC+1 (CET)
                    "Asia/Tokyo" => -540,  // UTC+9 (JST)
                    _ => -480  // 默认 UTC+8
                };
            }
        }

        /// <summary>
        /// 获取指纹收集脚本内容
        /// </summary>
        public async Task<string?> GetCollectorScriptAsync()
        {
            try
            {
                if (!File.Exists(_collectorScriptPath))
                {
                    _log.LogWarn("FingerprintCollector", $"Collector script not found: {_collectorScriptPath}");
                    return null;
                }

                return await File.ReadAllTextAsync(_collectorScriptPath);
            }
            catch (Exception ex)
            {
                _log.LogError("FingerprintCollector", $"Failed to read collector script: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 保存指纹 JSON 到文件
        /// </summary>
        public async Task<string?> SaveFingerprintJsonAsync(string json, string fileName)
        {
            try
            {
                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                await File.WriteAllTextAsync(filePath, json);
                _log.LogInfo("FingerprintCollector", $"Fingerprint JSON saved: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                _log.LogError("FingerprintCollector", $"Failed to save fingerprint JSON: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 根据 webdriverMode 返回正确的 webdriver 值
        /// </summary>
        private object? GetWebdriverValue(string? webdriverMode)
        {
            return (webdriverMode ?? "undefined") switch
            {
                "undefined" or "delete" => null,  // JSON 中显示为 null
                "true" => true,
                "false" => false,
                _ => null
            };
        }
    }
}
