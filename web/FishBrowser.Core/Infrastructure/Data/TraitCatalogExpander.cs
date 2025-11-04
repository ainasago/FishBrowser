using System;
using System.Collections.Generic;
using System.Linq;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Infrastructure.Data;

/// <summary>
/// 扩展 Trait 目录到 30+ 维度（M3 目标）
/// </summary>
public class TraitCatalogExpander
{
    private readonly WebScraperDbContext _db;

    public TraitCatalogExpander(WebScraperDbContext db)
    {
        _db = db;
    }

    public void ExpandCatalog()
    {
        // 确保分类存在
        EnsureCategories();
        
        // 添加扩展 Trait
        AddWebRTCTraits();
        AddDNSTraits();
        AddHeadersTraits();
        AddStorageTraits();
        AddPermissionsTraits();
        AddDeviceTraits();
        AddNetworkTraits();
        
        _db.SaveChanges();
    }

    private void EnsureCategories()
    {
        var categories = new[]
        {
            ("Browser", "浏览器特征", 1),
            ("Device", "设备特征", 2),
            ("Graphics", "图形特征", 3),
            ("Audio", "音频特征", 4),
            ("Network", "网络特征", 5),
            ("Storage", "存储特征", 6),
            ("Permissions", "权限特征", 7),
            ("System", "系统特征", 8),
            ("Headers", "请求头特征", 9),
            ("Privacy", "隐私特征", 10)
        };

        foreach (var (name, desc, order) in categories)
        {
            if (!_db.TraitCategories.Any(c => c.Name == name))
            {
                _db.TraitCategories.Add(new TraitCategory { Name = name, Description = desc, Order = order });
            }
        }
        _db.SaveChanges();
    }

    private void AddWebRTCTraits()
    {
        var cat = _db.TraitCategories.First(c => c.Name == "Network");
        
        AddTraitIfNotExists(cat, "network.webrtc.enabled", "WebRTC 启用", TraitValueType.Bool, "false",
            dependencies: new[] { "network.webrtc.iceServers" });
        
        AddTraitIfNotExists(cat, "network.webrtc.iceServers", "ICE 服务器列表", TraitValueType.Array,
            "[\"stun:stun.l.google.com:19302\"]");
        
        AddTraitIfNotExists(cat, "network.webrtc.ipHandling", "IP 处理策略", TraitValueType.String, "\"default\"",
            constraints: "{\"enum\": [\"default\", \"default_public_and_private\", \"default_public_only\", \"disable_non_proxied_udp\"]}");
    }

    private void AddDNSTraits()
    {
        var cat = _db.TraitCategories.First(c => c.Name == "Network");
        
        AddTraitIfNotExists(cat, "network.dns.leakProtection", "DNS 泄露防护", TraitValueType.Bool, "true");
        
        AddTraitIfNotExists(cat, "network.dns.servers", "DNS 服务器", TraitValueType.Array,
            "[\"8.8.8.8\", \"8.8.4.4\"]");
    }

    private void AddHeadersTraits()
    {
        var cat = _db.TraitCategories.First(c => c.Name == "Headers");
        
        AddTraitIfNotExists(cat, "headers.order", "请求头顺序", TraitValueType.Array,
            "[\"user-agent\", \"accept-language\", \"accept-encoding\", \"accept\"]");
        
        AddTraitIfNotExists(cat, "headers.extra", "额外请求头", TraitValueType.Object, "{}");
        
        AddTraitIfNotExists(cat, "headers.refererPolicy", "Referer 策略", TraitValueType.String, "\"strict-origin-when-cross-origin\"");
    }

    private void AddStorageTraits()
    {
        var cat = _db.TraitCategories.First(c => c.Name == "Storage");
        
        AddTraitIfNotExists(cat, "storage.localStorage.enabled", "LocalStorage 启用", TraitValueType.Bool, "true");
        
        AddTraitIfNotExists(cat, "storage.sessionStorage.enabled", "SessionStorage 启用", TraitValueType.Bool, "true");
        
        AddTraitIfNotExists(cat, "storage.indexedDB.enabled", "IndexedDB 启用", TraitValueType.Bool, "true");
        
        AddTraitIfNotExists(cat, "storage.cookies.enabled", "Cookie 启用", TraitValueType.Bool, "true");
    }

    private void AddPermissionsTraits()
    {
        var cat = _db.TraitCategories.First(c => c.Name == "Permissions");
        
        AddTraitIfNotExists(cat, "permissions.geolocation", "地理位置权限", TraitValueType.String, "\"deny\"",
            constraints: "{\"enum\": [\"allow\", \"deny\", \"prompt\"]}");
        
        AddTraitIfNotExists(cat, "permissions.camera", "摄像头权限", TraitValueType.String, "\"deny\"",
            constraints: "{\"enum\": [\"allow\", \"deny\", \"prompt\"]}");
        
        AddTraitIfNotExists(cat, "permissions.microphone", "麦克风权限", TraitValueType.String, "\"deny\"",
            constraints: "{\"enum\": [\"allow\", \"deny\", \"prompt\"]}");
        
        AddTraitIfNotExists(cat, "permissions.notifications", "通知权限", TraitValueType.String, "\"deny\"",
            constraints: "{\"enum\": [\"allow\", \"deny\", \"prompt\"]}");
    }

    private void AddDeviceTraits()
    {
        var cat = _db.TraitCategories.First(c => c.Name == "Device");
        
        AddTraitIfNotExists(cat, "device.deviceMemory", "设备内存 (GB)", TraitValueType.Number, "8");
        
        AddTraitIfNotExists(cat, "device.hardwareConcurrency", "CPU 核心数", TraitValueType.Number, "4");
        
        AddTraitIfNotExists(cat, "device.maxTouchPoints", "触摸点数", TraitValueType.Number, "0");
        
        AddTraitIfNotExists(cat, "device.screen.width", "屏幕宽度", TraitValueType.Number, "1920");
        
        AddTraitIfNotExists(cat, "device.screen.height", "屏幕高度", TraitValueType.Number, "1080");
        
        AddTraitIfNotExists(cat, "device.screen.colorDepth", "色深", TraitValueType.Number, "24");
        
        AddTraitIfNotExists(cat, "device.screen.pixelDepth", "像素深度", TraitValueType.Number, "24");
    }

    private void AddNetworkTraits()
    {
        var cat = _db.TraitCategories.First(c => c.Name == "Network");
        
        AddTraitIfNotExists(cat, "network.connection.type", "连接类型", TraitValueType.String, "\"4g\"",
            constraints: "{\"enum\": [\"4g\", \"3g\", \"2g\", \"slow-2g\"]}");
        
        AddTraitIfNotExists(cat, "network.connection.downlink", "下行速度 (Mbps)", TraitValueType.Number, "10");
        
        AddTraitIfNotExists(cat, "network.connection.rtt", "往返时间 (ms)", TraitValueType.Number, "50");
        
        AddTraitIfNotExists(cat, "network.connection.saveData", "节省数据模式", TraitValueType.Bool, "false");
    }

    private void AddTraitIfNotExists(TraitCategory category, string key, string displayName, TraitValueType valueType,
        string? defaultValue = null, string? constraints = null, string[]? dependencies = null)
    {
        if (_db.TraitDefinitions.Any(d => d.Key == key))
            return;

        var def = new TraitDefinition
        {
            Key = key,
            DisplayName = displayName,
            CategoryId = category.Id,
            ValueType = valueType,
            DefaultValueJson = defaultValue,
            ConstraintsJson = constraints,
            DependenciesJson = dependencies != null ? System.Text.Json.JsonSerializer.Serialize(dependencies) : null,
            VersionIntroduced = "0.2.0"
        };
        _db.TraitDefinitions.Add(def);
        _db.SaveChanges();
    }
}
