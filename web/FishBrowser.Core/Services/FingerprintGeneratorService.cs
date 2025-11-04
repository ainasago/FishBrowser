using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FishBrowser.WPF.Services;

public class FingerprintGeneratorService
{
    private readonly WebScraperDbContext _db;
    private readonly ILogService _log;
    private readonly DependencyResolutionService _depResolver;

    public FingerprintGeneratorService(WebScraperDbContext db, ILogService log, DependencyResolutionService depResolver)
    {
        _db = db;
        _log = log;
        _depResolver = depResolver;
    }

    public FingerprintProfile GenerateFromMeta(FingerprintMetaProfile meta, string name, string? seed = null)
    {
        var traits = ParseJson<Dictionary<string, object?>>(meta.TraitsJson) ?? new();

        // 上下文：Region/DeviceClass/Vendor 来源于 traits 或 meta 字段（可扩展）
        var context = new GenerationContext
        {
            Region = GetString(traits, "context.region", null),
            DeviceClass = GetString(traits, "context.deviceClass", null),
            Vendor = GetString(traits, "context.vendor", null),
            Seed = seed ?? $"meta:{meta.Id}:{name}"
        };

        // 锁定键与随机计划
        var locks = ParseLocks(meta.LockFlagsJson);
        var plan = ParseRandomPlan(meta.RandomizationPlanJson);
        ApplyRandomPlan(traits, plan, locks, context);

        // 缺省填充：根据 Option 权重池选择（可复现随机），跳过锁定键
        EnsureWithOptions(traits, "browser.uach.platform", context, locks);
        EnsureWithOptions(traits, "system.locale", context, locks);
        EnsureWithOptions(traits, "system.timezone", context, locks);
        EnsureWithOptions(traits, "graphics.webgl.vendor", context, locks);
        EnsureWithOptions(traits, "graphics.webgl.renderer", context, locks);
        EnsureWithOptions(traits, "headers.order", context, locks);

        // 应用依赖修复与冲突检测
        _depResolver.ApplyDependencyFixes(traits);
        var conflicts = _depResolver.DetectConflicts(traits);
        if (conflicts.Count > 0)
        {
            _log.LogInfo("Generator", $"检测到冲突 ({conflicts.Count}): {string.Join("; ", conflicts.Take(3))}");
        }
        FixDependencies(traits);
        var profile = new FingerprintProfile
        {
            Name = name,
            MetaProfileId = meta.Id,
            UserAgent = GetString(traits, "browser.userAgent", "Mozilla/5.0"),
            AcceptLanguage = GetString(traits, "browser.acceptLanguage", "zh-CN,zh;q=0.9"),
            Platform = GetString(traits, "browser.platform", "Win32"),
            Locale = GetString(traits, "system.locale", "zh-CN"),
            Timezone = GetString(traits, "system.timezone", "Asia/Shanghai"),
            ViewportWidth = GetInt(traits, "device.viewport.width", 1366),
            ViewportHeight = GetInt(traits, "device.viewport.height", 768),
            DeviceMemory = GetInt(traits, "device.deviceMemory", 8),
            ProcessorCount = GetInt(traits, "device.hardwareConcurrency", 4),
            DisableWebRTC = !GetBool(traits, "network.webrtc.enabled", false),
            DisableDNSLeak = GetBool(traits, "network.dns.leakProtection", true),
            EnableDNT = GetBool(traits, "privacy.doNotTrack", false),
            WebGLVendor = GetString(traits, "graphics.webgl.vendor", null),
            WebGLRenderer = GetString(traits, "graphics.webgl.renderer", null),
            FontPreset = SerializeIfPresent(traits, "device.fonts"),
            AudioSampleRate = GetString(traits, "audio.context.sampleRate", null),
            // 字体配置（从 Meta 或 Traits 中读取）
            FontsMode = meta.FontsMode ?? "real",
            FontsJson = meta.FontsJson,
            // WebGL / WebGPU / Audio / Speech 配置
            WebGLImageMode = meta.WebGLImageMode ?? "noise",
            WebGLInfoMode = meta.WebGLInfoMode ?? "ua_based",
            WebGPUMode = meta.WebGPUMode ?? "match_webgl",
            AudioContextMode = meta.AudioContextMode ?? "noise",
            SpeechVoicesEnabled = meta.SpeechVoicesEnabled,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        ApplyCompiledOutputs(profile, meta, traits);
        return profile;
    }

    public void ApplyCompiledOutputs(FingerprintProfile profile, FingerprintMetaProfile meta, Dictionary<string, object?> traits)
    {
        var headersOrder = ParseJson<List<string>>(GetString(traits, "headers.order", "[]")) ?? new List<string>();
        var extraHeaders = ParseJson<Dictionary<string, string>>(GetString(traits, "headers.extra", "{}")) ?? new Dictionary<string, string>();
        var headers = new List<KeyValuePair<string, string>>();
        foreach (var key in headersOrder)
        {
            switch (key)
            {
                case "user-agent":
                    headers.Add(new KeyValuePair<string, string>("user-agent", profile.UserAgent));
                    break;
                case "accept-language":
                    headers.Add(new KeyValuePair<string, string>("accept-language", profile.AcceptLanguage));
                    break;
                default:
                    if (extraHeaders.TryGetValue(key, out var val))
                        headers.Add(new KeyValuePair<string, string>(key, val));
                    break;
            }
        }
        foreach (var kv in extraHeaders)
        {
            if (!headers.Any(h => h.Key.Equals(kv.Key, StringComparison.OrdinalIgnoreCase)))
                headers.Add(new KeyValuePair<string, string>(kv.Key, kv.Value));
        }
        profile.CompiledHeadersJson = JsonSerializer.Serialize(headers);

        var scripts = new Dictionary<string, object?>
        {
            ["canvas"] = new { noiseSeed = GetInt(traits, "graphics.canvas.noiseSeed", 0) },
            ["webgl"] = new { vendor = profile.WebGLVendor, renderer = profile.WebGLRenderer },
            ["audio"] = new { sampleRate = profile.AudioSampleRate },
            ["intl"] = new { locale = profile.Locale, timezone = profile.Timezone }
        };
        profile.CompiledScriptsJson = JsonSerializer.Serialize(scripts);

        var contextOptions = new Dictionary<string, object?>
        {
            ["userAgent"] = profile.UserAgent,
            ["locale"] = profile.Locale,
            ["timezoneId"] = profile.Timezone,
            ["viewport"] = new { width = profile.ViewportWidth, height = profile.ViewportHeight }
        };
        profile.CompiledContextOptionsJson = JsonSerializer.Serialize(contextOptions);
        profile.LastGeneratedAt = DateTime.UtcNow;
        profile.GeneratorVersion = "0.1.0";
    }

    private static T? ParseJson<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        try { return JsonSerializer.Deserialize<T>(json!); } catch { return default; }
    }

    private static string? GetString(Dictionary<string, object?> dict, string key, string? fallback)
    {
        return dict.TryGetValue(key, out var v) && v != null ? v.ToString() : fallback;
    }

    private static int GetInt(Dictionary<string, object?> dict, string key, int fallback)
    {
        if (!dict.TryGetValue(key, out var v) || v == null) return fallback;
        if (v is JsonElement je && je.ValueKind == JsonValueKind.Number && je.TryGetInt32(out var n)) return n;
        if (int.TryParse(v.ToString(), out var m)) return m;
        return fallback;
    }

    private static bool GetBool(Dictionary<string, object?> dict, string key, bool fallback)
    {
        if (!dict.TryGetValue(key, out var v) || v == null) return fallback;
        if (v is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.True) return true;
            if (je.ValueKind == JsonValueKind.False) return false;
        }
        if (bool.TryParse(v.ToString(), out var b)) return b;
        return fallback;
    }

    private static string? SerializeIfPresent(Dictionary<string, object?> dict, string key)
    {
        if (!dict.TryGetValue(key, out var v) || v == null) return null;
        try { return JsonSerializer.Serialize(v); } catch { return null; }
    }

    // ---------- Enhanced generation helpers ----------
    private sealed class GenerationContext
    {
        public string? Region { get; set; }
        public string? Vendor { get; set; }
        public string? DeviceClass { get; set; }
        public string Seed { get; set; } = Guid.NewGuid().ToString();
    }

    private void EnsureWithOptions(Dictionary<string, object?> traits, string traitKey, GenerationContext ctx, HashSet<string>? locks = null)
    {
        if (locks != null && locks.Contains(traitKey)) return;
        if (traits.ContainsKey(traitKey)) return;
        var def = _db.TraitDefinitions.FirstOrDefault(d => d.Key == traitKey);
        if (def == null) return;
        var query = _db.TraitOptions.Where(o => o.TraitDefinitionId == def.Id).AsQueryable();
        if (!string.IsNullOrWhiteSpace(ctx.Region)) query = query.Where(o => o.Region == ctx.Region || o.Region == null);
        if (!string.IsNullOrWhiteSpace(ctx.Vendor)) query = query.Where(o => o.Vendor == ctx.Vendor || o.Vendor == null);
        if (!string.IsNullOrWhiteSpace(ctx.DeviceClass)) query = query.Where(o => o.DeviceClass == ctx.DeviceClass || o.DeviceClass == null);
        var list = query.ToList();
        if (list.Count == 0) return;
        var picked = PickWeighted(list, ctx.Seed + ":" + traitKey);
        if (picked == null) return;
        // 解析 ValueJson 并放入 traits
        try
        {
            using var doc = JsonDocument.Parse(picked.ValueJson);
            traits[traitKey] = JsonElementToObject(doc.RootElement);
        }
        catch
        {
            // 如果不是严格 JSON，则按字符串处理
            traits[traitKey] = picked.ValueJson;
        }
    }

    // ---------- Locks & Randomization Plan ----------
    private static HashSet<string> ParseLocks(string? lockFlagsJson)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(lockFlagsJson)) return set;
        try
        {
            var arr = JsonSerializer.Deserialize<List<string>>(lockFlagsJson);
            if (arr != null) foreach (var k in arr) if (!string.IsNullOrWhiteSpace(k)) set.Add(k);
        }
        catch { }
        return set;
    }

    private sealed class PlanSpec
    {
        public string Type { get; set; } = string.Empty; // UniformInt | NormalInt
        public double? Min { get; set; }
        public double? Max { get; set; }
        public double? Mean { get; set; }
        public double? Stddev { get; set; }
    }

    private static Dictionary<string, PlanSpec> ParseRandomPlan(string? planJson)
    {
        if (string.IsNullOrWhiteSpace(planJson)) return new();
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, PlanSpec>>(planJson!) ?? new();
        }
        catch { return new(); }
    }

    private void ApplyRandomPlan(Dictionary<string, object?> traits, Dictionary<string, PlanSpec> plan, HashSet<string> locks, GenerationContext ctx)
    {
        if (plan.Count == 0) return;
        var rnd = CreateDeterministicRandom(ctx.Seed + ":plan");
        foreach (var kv in plan)
        {
            var key = kv.Key;
            if (locks.Contains(key)) continue; // 跳过锁定
            if (traits.ContainsKey(key)) continue; // 已有值不覆盖
            var spec = kv.Value;
            switch (spec.Type)
            {
                case "UniformInt":
                    {
                        int min = (int)Math.Round(spec.Min ?? 0);
                        int max = (int)Math.Round(spec.Max ?? min);
                        if (max < min) (min, max) = (max, min);
                        int val = min == max ? min : rnd.Next(min, max + 1);
                        traits[key] = val;
                        break;
                    }
                case "NormalInt":
                    {
                        double mean = spec.Mean ?? 0;
                        double std = spec.Stddev ?? 1;
                        // Box–Muller
                        double u1 = 1.0 - rnd.NextDouble();
                        double u2 = 1.0 - rnd.NextDouble();
                        double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                        int val = (int)Math.Round(mean + std * z);
                        if (spec.Min.HasValue) val = Math.Max(val, (int)Math.Round(spec.Min.Value));
                        if (spec.Max.HasValue) val = Math.Min(val, (int)Math.Round(spec.Max.Value));
                        traits[key] = val;
                        break;
                    }
            }
        }
    }

    private static object? JsonElementToObject(JsonElement el)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.String: return el.GetString();
            case JsonValueKind.Number:
                if (el.TryGetInt64(out var l)) return l;
                if (el.TryGetDouble(out var d)) return d;
                return null;
            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            case JsonValueKind.Object: return JsonSerializer.Deserialize<Dictionary<string, object?>>(el.GetRawText());
            case JsonValueKind.Array: return JsonSerializer.Deserialize<List<object?>>(el.GetRawText());
            default: return null;
        }
    }

    private TraitOption? PickWeighted(List<TraitOption> options, string seed)
    {
        if (options.Count == 1) return options[0];
        // 伪随机（可复现）
        var rnd = CreateDeterministicRandom(seed);
        var sum = options.Sum(o => o.Weight <= 0 ? 0.0 : o.Weight);
        if (sum <= 0) return options[rnd.Next(options.Count)];
        var roll = rnd.NextDouble() * sum;
        double acc = 0;
        foreach (var o in options)
        {
            var w = o.Weight <= 0 ? 0.0 : o.Weight;
            acc += w;
            if (roll <= acc) return o;
        }
        return options.LastOrDefault();
    }

    private static Random CreateDeterministicRandom(string seed)
    {
        unchecked
        {
            int h = 23;
            foreach (var ch in seed)
                h = h * 31 + ch;
            return new Random(h);
        }
    }

    private static void FixDependencies(Dictionary<string, object?> traits)
    {
        // 规则1：locale 与 timezone 简单配对
        var loc = traits.TryGetValue("system.locale", out var lv) ? lv?.ToString() : null;
        var tz = traits.TryGetValue("system.timezone", out var tv) ? tv?.ToString() : null;
        if (loc == "zh-CN" && string.IsNullOrWhiteSpace(tz)) traits["system.timezone"] = "Asia/Shanghai";
        if (loc == "en-US" && string.IsNullOrWhiteSpace(tz)) traits["system.timezone"] = "America/New_York";

        // 规则2：UA-CH platform 与 platform 对齐（简化处理）
        var uachPlatform = traits.TryGetValue("browser.uach.platform", out var up) ? up?.ToString() : null;
        var platform = traits.TryGetValue("browser.platform", out var pf) ? pf?.ToString() : null;
        if (string.IsNullOrWhiteSpace(platform) && !string.IsNullOrWhiteSpace(uachPlatform))
        {
            platform = uachPlatform == "Windows" ? "Win32" : (uachPlatform == "macOS" ? "MacIntel" : platform);
            if (!string.IsNullOrWhiteSpace(platform)) traits["browser.platform"] = platform!;
        }
    }
}
