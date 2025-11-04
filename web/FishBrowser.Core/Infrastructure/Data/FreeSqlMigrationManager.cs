using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;
using FishBrowser.WPF.Data;
using FreeSql;

namespace FishBrowser.WPF.Infrastructure.Data;

/// <summary>
/// FreeSql 数据库迁移管理器
/// 负责数据库表结构的自动同步，不删除现有数据
/// </summary>
public class FreeSqlMigrationManager
{
    private readonly WebScraperDbContext _dbContext;
    private readonly LogService _logService;
    private readonly IFreeSql _fsql;

    public FreeSqlMigrationManager(WebScraperDbContext dbContext, LogService logService, IFreeSql fsql)
    {
        _dbContext = dbContext;
        _logService = logService;
        _fsql = fsql;
    }

    /// <summary>
    /// 基于 EF 元数据创建缺失的表（最小字段同步，不建外键约束）
    /// </summary>
    private void CreateTable(Microsoft.EntityFrameworkCore.Metadata.IEntityType entityType, string tableName)
    {
        try
        {
            var props = entityType.GetProperties();
            var pk = entityType.FindPrimaryKey();
            var pkNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (pk != null)
            {
                foreach (var p in pk.Properties)
                {
                    pkNames.Add(p.GetColumnName(StoreObjectIdentifier.Table(tableName)) ?? p.Name);
                }
            }

            var columns = new List<string>();
            foreach (var p in props)
            {
                var colName = p.GetColumnName(StoreObjectIdentifier.Table(tableName)) ?? p.Name;
                var colType = GetSqliteColumnType(p.ClrType);
                var notNull = p.IsNullable ? string.Empty : " NOT NULL";
                var def = GetDefaultValue(p);

                var isPk = pkNames.Contains(colName);
                var pkSuffix = string.Empty;
                if (isPk)
                {
                    // sqlite 主键列
                    if (string.Equals(colType, "INTEGER", StringComparison.OrdinalIgnoreCase))
                        pkSuffix = " PRIMARY KEY AUTOINCREMENT";
                    else
                        pkSuffix = " PRIMARY KEY";
                    // 主键不再追加 NOT NULL/DEFAULT
                    notNull = string.Empty;
                    def = string.Empty;
                }

                columns.Add($"{colName} {colType}{pkSuffix}{notNull}{def}");
            }

            var sql = $"CREATE TABLE IF NOT EXISTS {tableName} (\n  {string.Join(",\n  ", columns)}\n);";

            var connection = _dbContext.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }

            _logService.LogInfo("Database", $"Table created: {tableName}");
        }
        catch (Exception ex)
        {
            _logService.LogError("Database", $"Failed to create table {tableName}: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// 初始化数据库 (自动迁移，不删除数据)
    /// </summary>
    public void InitializeDatabase()
    {
        try
        {
            _dbContext.Database.EnsureCreated();
            
            // 扩展 Trait 目录到 30+ 维度
            var expander = new TraitCatalogExpander(_dbContext);
            expander.ExpandCatalog();

            _logService.LogInfo("Database", "Starting database initialization...");

            // 同步表结构 (添加新列，不删除现有数据)
            SyncSchema();

            // 重要：为历史数据填充 NULL 默认值，避免读取时抛出 NULL 异常
            ApplyNullDefaults();

            // 元数据库最小种子
            SeedMetaCatalog();

            // 导入 /data/traits 数据集至 TraitOptions（可多次运行，merge 模式）
            try
            {
                var folder = Path.Combine(AppContext.BaseDirectory?.TrimEnd(Path.DirectorySeparatorChar) ?? Directory.GetCurrentDirectory(), "data", "traits");
                var seeder = new TraitsDatasetSeeder(_dbContext);
                var inserted = seeder.SeedFromFolder(folder, merge: true);
                _logService.LogInfo("Database", $"Traits dataset seeding completed. Inserted/updated options: {inserted}");
            }
            catch (Exception sx)
            {
                _logService.LogWarn("Database", $"Traits dataset seeding skipped: {sx.Message}");
            }

            _logService.LogInfo("Database", "Database initialized successfully");
        }
        catch (Exception ex)
        {
            _logService.LogError("Database", $"Failed to initialize database: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// 为历史数据填充 NULL 默认值（不会覆盖已有非 NULL 值）
    /// 解决 EF 读取非空字符串列却遇到 NULL 导致的 InvalidCastException
    /// </summary>
    private void ApplyNullDefaults()
    {
        try
        {
            _logService.LogInfo("Database", "Applying null default patches for legacy rows...");

            // FingerprintProfiles: 将关键非空字符串字段的 NULL 值填充为默认值
            _dbContext.Database.ExecuteSqlRaw(@"
UPDATE FingerprintProfiles SET Name = '' WHERE Name IS NULL;
UPDATE FingerprintProfiles SET UserAgent = '' WHERE UserAgent IS NULL;
UPDATE FingerprintProfiles SET AcceptLanguage = 'zh-CN,zh;q=0.9,en;q=0.8' WHERE AcceptLanguage IS NULL;
UPDATE FingerprintProfiles SET Timezone = 'Asia/Shanghai' WHERE Timezone IS NULL;
UPDATE FingerprintProfiles SET Locale = 'zh-CN' WHERE Locale IS NULL;
UPDATE FingerprintProfiles SET Platform = 'Win32' WHERE Platform IS NULL;
UPDATE FingerprintProfiles SET FontsMode = 'real' WHERE FontsMode IS NULL;
UPDATE FingerprintProfiles SET WebGLImageMode = 'noise' WHERE WebGLImageMode IS NULL;
UPDATE FingerprintProfiles SET WebGLInfoMode = 'ua_based' WHERE WebGLInfoMode IS NULL;
UPDATE FingerprintProfiles SET WebGPUMode = 'match_webgl' WHERE WebGPUMode IS NULL;
UPDATE FingerprintProfiles SET AudioContextMode = 'noise' WHERE AudioContextMode IS NULL;
UPDATE FingerprintProfiles SET ConnectionType = '4g' WHERE ConnectionType IS NULL;

-- 数值字段若为 NULL，填充为合理默认
UPDATE FingerprintProfiles SET ViewportWidth = 1366 WHERE ViewportWidth IS NULL OR ViewportWidth = 0;
UPDATE FingerprintProfiles SET ViewportHeight = 768 WHERE ViewportHeight IS NULL OR ViewportHeight = 0;
UPDATE FingerprintProfiles SET HardwareConcurrency = 8 WHERE HardwareConcurrency IS NULL OR HardwareConcurrency = 0;
UPDATE FingerprintProfiles SET MaxTouchPoints = 0 WHERE MaxTouchPoints IS NULL;
UPDATE FingerprintProfiles SET ConnectionRtt = 50 WHERE ConnectionRtt IS NULL OR ConnectionRtt = 0;
UPDATE FingerprintProfiles SET ConnectionDownlink = 10.0 WHERE ConnectionDownlink IS NULL OR ConnectionDownlink = 0;

-- 时间字段
UPDATE FingerprintProfiles SET CreatedAt = COALESCE(CreatedAt, CURRENT_TIMESTAMP);

-- BrowserEnvironments: 关键字符串默认值
UPDATE BrowserEnvironments SET UserAgent = '' WHERE UserAgent IS NULL;
UPDATE BrowserEnvironments SET Platform = 'Win32' WHERE Platform IS NULL;
UPDATE BrowserEnvironments SET Locale = 'zh-CN' WHERE Locale IS NULL;
UPDATE BrowserEnvironments SET Timezone = 'Asia/Shanghai' WHERE Timezone IS NULL;

-- 数值默认
UPDATE BrowserEnvironments SET ViewportWidth = COALESCE(ViewportWidth, 1280);
UPDATE BrowserEnvironments SET ViewportHeight = COALESCE(ViewportHeight, 720);

");

            _logService.LogInfo("Database", "Null default patches applied");
        }
        catch (Exception ex)
        {
            _logService.LogWarn("Database", $"ApplyNullDefaults skipped: {ex.Message}");
        }
    }

    /// <summary>
    /// 元数据库最小数据种子（只在缺失时插入）
    /// </summary>
    private void SeedMetaCatalog()
    {
        try
        {
            _logService.LogInfo("Database", "Seeding fingerprint meta catalog (if missing)...");

            // Category: Browser
            var browser = _dbContext.TraitCategories.FirstOrDefault(c => c.Name == "Browser");
            if (browser == null)
            {
                browser = new TraitCategory { Name = "Browser", Description = "Browser level traits", Order = 1 };
                _dbContext.TraitCategories.Add(browser);
                _dbContext.SaveChanges();
            }

            // Category: Device
            var device = _dbContext.TraitCategories.FirstOrDefault(c => c.Name == "Device");
            if (device == null)
            {
                device = new TraitCategory { Name = "Device", Description = "Device & hardware traits", Order = 2 };
                _dbContext.TraitCategories.Add(device);
                _dbContext.SaveChanges();
            }

            // Category: Graphics
            var graphics = _dbContext.TraitCategories.FirstOrDefault(c => c.Name == "Graphics");
            if (graphics == null)
            {
                graphics = new TraitCategory { Name = "Graphics", Description = "Canvas/WebGL traits", Order = 3 };
                _dbContext.TraitCategories.Add(graphics);
                _dbContext.SaveChanges();
            }

            // Category: Privacy
            var privacy = _dbContext.TraitCategories.FirstOrDefault(c => c.Name == "Privacy");
            if (privacy == null)
            {
                privacy = new TraitCategory { Name = "Privacy", Description = "Privacy & detection traits", Order = 4 };
                _dbContext.TraitCategories.Add(privacy);
                _dbContext.SaveChanges();
            }

            // Category: Network
            var network = _dbContext.TraitCategories.FirstOrDefault(c => c.Name == "Network");
            if (network == null)
            {
                network = new TraitCategory { Name = "Network", Description = "Network/WebRTC/DNS traits", Order = 5 };
                _dbContext.TraitCategories.Add(network);
                _dbContext.SaveChanges();
            }

            // Category: Storage
            var storage = _dbContext.TraitCategories.FirstOrDefault(c => c.Name == "Storage");
            if (storage == null)
            {
                storage = new TraitCategory { Name = "Storage", Description = "Storage & cookie traits", Order = 6 };
                _dbContext.TraitCategories.Add(storage);
                _dbContext.SaveChanges();
            }

            // Category: Permissions
            var permissions = _dbContext.TraitCategories.FirstOrDefault(c => c.Name == "Permissions");
            if (permissions == null)
            {
                permissions = new TraitCategory { Name = "Permissions", Description = "Permissions simulation traits", Order = 7 };
                _dbContext.TraitCategories.Add(permissions);
                _dbContext.SaveChanges();
            }

            // Category: System
            var system = _dbContext.TraitCategories.FirstOrDefault(c => c.Name == "System");
            if (system == null)
            {
                system = new TraitCategory { Name = "System", Description = "OS/Intl/Timezone traits", Order = 8 };
                _dbContext.TraitCategories.Add(system);
                _dbContext.SaveChanges();
            }

            // Category: Sensors
            var sensors = _dbContext.TraitCategories.FirstOrDefault(c => c.Name == "Sensors");
            if (sensors == null)
            {
                sensors = new TraitCategory { Name = "Sensors", Description = "Battery/Orientation/Media devices traits", Order = 9 };
                _dbContext.TraitCategories.Add(sensors);
                _dbContext.SaveChanges();
            }

            // Category: Headers
            var headers = _dbContext.TraitCategories.FirstOrDefault(c => c.Name == "Headers");
            if (headers == null)
            {
                headers = new TraitCategory { Name = "Headers", Description = "HTTP headers template traits", Order = 10 };
                _dbContext.TraitCategories.Add(headers);
                _dbContext.SaveChanges();
            }

            // TraitDefinition helpers
            void EnsureTrait(TraitCategory cat, string key, string display, TraitValueType type, string? defaultJson = null)
            {
                if (_dbContext.TraitDefinitions.Any(d => d.Key == key)) return;
                _dbContext.TraitDefinitions.Add(new TraitDefinition
                {
                    Key = key,
                    DisplayName = display,
                    CategoryId = cat.Id,
                    ValueType = type,
                    DefaultValueJson = defaultJson
                });
                _dbContext.SaveChanges();
            }

            // TraitOption helper
            void EnsureOption(string traitKey, string valueJson, string? label = null, double weight = 1.0, string? region = null, string? vendor = null, string? deviceClass = null)
            {
                var def = _dbContext.TraitDefinitions.FirstOrDefault(x => x.Key == traitKey);
                if (def == null) return;
                if (_dbContext.TraitOptions.Any(o => o.TraitDefinitionId == def.Id && o.ValueJson == valueJson)) return;
                _dbContext.TraitOptions.Add(new TraitOption
                {
                    TraitDefinitionId = def.Id,
                    ValueJson = valueJson,
                    Label = label,
                    Weight = weight,
                    Region = region,
                    Vendor = vendor,
                    DeviceClass = deviceClass
                });
                _dbContext.SaveChanges();
            }

            // Browser traits
            EnsureTrait(browser, "browser.userAgent", "User-Agent", TraitValueType.String, "\"Mozilla/5.0\"");
            EnsureTrait(browser, "browser.acceptLanguage", "Accept-Language", TraitValueType.String, "\"zh-CN,zh;q=0.9\"");
            EnsureTrait(browser, "browser.platform", "Platform", TraitValueType.String, "\"Win32\"");
            // UA-CH (Client Hints)
            EnsureTrait(browser, "browser.uach.brands", "Sec-CH-UA Brands", TraitValueType.Array, "[]");
            EnsureTrait(browser, "browser.uach.fullVersionList", "Sec-CH-UA Full Version List", TraitValueType.Array, "[]");
            EnsureTrait(browser, "browser.uach.platform", "Sec-CH-UA-Platform", TraitValueType.String, "\"Windows\"");
            EnsureTrait(browser, "browser.uach.platformVersion", "Sec-CH-UA-Platform-Version", TraitValueType.String, "\"15.0.0\"");
            EnsureTrait(browser, "browser.uach.mobile", "Sec-CH-UA-Mobile", TraitValueType.Bool, "false");
            EnsureTrait(browser, "browser.uach.arch", "Sec-CH-UA-Arch", TraitValueType.String, "\"x86\"");
            EnsureTrait(browser, "browser.uach.model", "Sec-CH-UA-Model", TraitValueType.String, "\"\"");

            // Device traits
            EnsureTrait(device, "device.hardwareConcurrency", "Hardware Concurrency", TraitValueType.Number, "8");
            EnsureTrait(device, "device.deviceMemory", "Device Memory (GB)", TraitValueType.Number, "8");
            EnsureTrait(device, "device.touchSupport", "Touch Support", TraitValueType.Bool, "false");
            EnsureTrait(device, "device.pixelRatio", "Device Pixel Ratio", TraitValueType.Number, "1");
            EnsureTrait(device, "device.screen.width", "Screen Width", TraitValueType.Number, "1920");
            EnsureTrait(device, "device.screen.height", "Screen Height", TraitValueType.Number, "1080");
            EnsureTrait(device, "device.viewport.width", "Viewport Width", TraitValueType.Number, "1366");
            EnsureTrait(device, "device.viewport.height", "Viewport Height", TraitValueType.Number, "768");
            EnsureTrait(device, "device.fonts", "Fonts Set", TraitValueType.Array, "[]");

            // Graphics traits
            EnsureTrait(graphics, "graphics.webgl.vendor", "WebGL Vendor", TraitValueType.String, "\"Google Inc. (NVIDIA)\"");
            EnsureTrait(graphics, "graphics.webgl.renderer", "WebGL Renderer", TraitValueType.String, "\"ANGLE (NVIDIA, NVIDIA GeForce, D3D11)\"");
            EnsureTrait(graphics, "graphics.canvas.noiseSeed", "Canvas Noise Seed", TraitValueType.Number, "0");
            EnsureTrait(graphics, "graphics.webgl.extensions", "WebGL Extensions", TraitValueType.Array, "[]");
            EnsureTrait(graphics, "graphics.webgl2.enabled", "WebGL2 Enabled", TraitValueType.Bool, "true");

            // Privacy traits
            EnsureTrait(privacy, "privacy.webdriver", "Navigator.webdriver", TraitValueType.Bool, "false");
            EnsureTrait(privacy, "privacy.doNotTrack", "Do Not Track", TraitValueType.Bool, "false");
            EnsureTrait(privacy, "privacy.plugins.enabled", "Plugins Exposed", TraitValueType.Bool, "false");
            EnsureTrait(privacy, "privacy.battery.enabled", "Battery API Enabled", TraitValueType.Bool, "false");
            EnsureTrait(privacy, "privacy.sensor.enabled", "Generic Sensor Enabled", TraitValueType.Bool, "false");

            // System/Intl traits
            EnsureTrait(system, "system.timezone", "Timezone", TraitValueType.String, "\"Asia/Shanghai\"");
            EnsureTrait(system, "system.locale", "Locale", TraitValueType.String, "\"zh-CN\"");
            EnsureTrait(system, "system.intl.calendar", "Intl Calendar", TraitValueType.String, "\"gregory\"");
            EnsureTrait(system, "system.languages", "Navigator.languages", TraitValueType.Array, "[\"zh-CN\",\"zh\",\"en\"]");

            // Headers traits
            EnsureTrait(headers, "headers.order", "Headers Order", TraitValueType.Array, "[\"accept\",\"accept-language\",\"user-agent\"]");
            EnsureTrait(headers, "headers.caseStyle", "Header Case Style", TraitValueType.String, "\"kebab\"");
            EnsureTrait(headers, "headers.extra", "Extra Headers", TraitValueType.Object, "{}");

            // Network traits (WebRTC/DNS/Connection)
            EnsureTrait(network, "network.webrtc.enabled", "WebRTC Enabled", TraitValueType.Bool, "false");
            EnsureTrait(network, "network.webrtc.localIpsVisible", "WebRTC Local IPs Visible", TraitValueType.Bool, "false");
            EnsureTrait(network, "network.dns.leakProtection", "DNS Leak Protection", TraitValueType.Bool, "true");
            EnsureTrait(network, "network.connection.downlink", "Connection Downlink", TraitValueType.Number, "10");
            EnsureTrait(network, "network.connection.effectiveType", "Connection Effective Type", TraitValueType.String, "\"4g\"");

            // Storage traits
            EnsureTrait(storage, "storage.cookies.enabled", "Cookies Enabled", TraitValueType.Bool, "true");
            EnsureTrait(storage, "storage.localStorage.size", "LocalStorage Size", TraitValueType.Number, "5242880");
            EnsureTrait(storage, "storage.sessionStorage.size", "SessionStorage Size", TraitValueType.Number, "5242880");

            // Permissions traits
            EnsureTrait(permissions, "perm.geolocation", "Geolocation Permission", TraitValueType.String, "\"denied\"");
            EnsureTrait(permissions, "perm.notifications", "Notifications Permission", TraitValueType.String, "\"default\"");
            EnsureTrait(permissions, "perm.camera", "Camera Permission", TraitValueType.String, "\"denied\"");
            EnsureTrait(permissions, "perm.microphone", "Microphone Permission", TraitValueType.String, "\"denied\"");

            // Sensors traits
            EnsureTrait(sensors, "sensors.deviceOrientation.enabled", "DeviceOrientation Enabled", TraitValueType.Bool, "false");
            EnsureTrait(sensors, "sensors.mediaDevices.count", "Media Devices Count", TraitValueType.Number, "0");

            // Audio traits
            EnsureTrait(graphics, "audio.context.sampleRate", "AudioContext Sample Rate", TraitValueType.Number, "48000");
            EnsureTrait(graphics, "audio.context.noiseSeed", "Audio Noise Seed", TraitValueType.Number, "0");

            // ---- Seed TraitOption pools ----
            // UA-CH brands
            EnsureOption("browser.uach.brands", "[{\\\"brand\\\":\\\"Chromium\\\",\\\"version\\\":\\\"126\\\"},{\\\"brand\\\":\\\"Google Chrome\\\",\\\"version\\\":\\\"126\\\"}]", "Chrome 126", 3.0, vendor: "Google", deviceClass: "Desktop");
            EnsureOption("browser.uach.brands", "[{\\\"brand\\\":\\\"Chromium\\\",\\\"version\\\":\\\"125\\\"},{\\\"brand\\\":\\\"Microsoft Edge\\\",\\\"version\\\":\\\"125\\\"}]", "Edge 125", 2.0, vendor: "Microsoft", deviceClass: "Desktop");
            // UA-CH platform
            EnsureOption("browser.uach.platform", "\"Windows\"", "Windows", 3.0, deviceClass: "Desktop");
            EnsureOption("browser.uach.platform", "\"macOS\"", "macOS", 1.5, deviceClass: "Desktop");
            EnsureOption("browser.uach.platform", "\"Android\"", "Android", 1.5, deviceClass: "Mobile");

            // WebGL vendor/renderer
            EnsureOption("graphics.webgl.vendor", "\"Google Inc. (NVIDIA)\"", "NVIDIA (ANGLE)", 2.0, vendor: "NVIDIA");
            EnsureOption("graphics.webgl.vendor", "\"Google Inc. (Intel)\"", "Intel (ANGLE)", 2.0, vendor: "Intel");
            EnsureOption("graphics.webgl.vendor", "\"Google Inc. (AMD)\"", "AMD (ANGLE)", 1.5, vendor: "AMD");
            EnsureOption("graphics.webgl.renderer", "\"ANGLE (NVIDIA, NVIDIA GeForce, D3D11)\"", "NVIDIA D3D11", 2.0, vendor: "NVIDIA");
            EnsureOption("graphics.webgl.renderer", "\"ANGLE (Intel, Intel Iris, D3D11)\"", "Intel D3D11", 2.0, vendor: "Intel");
            EnsureOption("graphics.webgl.renderer", "\"ANGLE (AMD, Radeon, D3D11)\"", "AMD D3D11", 1.5, vendor: "AMD");

            // Locales / Regions
            EnsureOption("system.locale", "\"zh-CN\"", "Chinese (China)", 3.0, region: "CN");
            EnsureOption("system.locale", "\"en-US\"", "English (US)", 2.5, region: "US");
            EnsureOption("system.locale", "\"en-GB\"", "English (UK)", 1.5, region: "GB");
            EnsureOption("system.timezone", "\"Asia/Shanghai\"", "Asia/Shanghai", 3.0, region: "CN");
            EnsureOption("system.timezone", "\"America/New_York\"", "America/New_York", 2.0, region: "US");
            EnsureOption("system.timezone", "\"Europe/London\"", "Europe/London", 1.5, region: "GB");

            // Header orders presets
            EnsureOption("headers.order", "[\\\"accept\\\",\\\"accept-language\\\",\\\"user-agent\\\",\\\"sec-ch-ua\\\"]", "Chrome-like", 3.0, vendor: "Google");
            EnsureOption("headers.order", "[\\\"accept\\\",\\\"user-agent\\\",\\\"accept-language\\\"]", "Minimal", 1.0);

            // Presets
            if (!_dbContext.TraitGroupPresets.Any())
            {
                _dbContext.TraitGroupPresets.Add(new TraitGroupPreset
                {
                    Name = "Desktop-CN",
                    Description = "桌面中国区域",
                    Version = "0.1.0",
                    Tags = "desktop,cn",
                    ItemsJson = "{\n  \"browser.platform\": \"Win32\",\n  \"system.locale\": \"zh-CN\",\n  \"system.timezone\": \"Asia/Shanghai\",\n  \"device.viewport.width\": 1366,\n  \"device.viewport.height\": 768\n}"
                });
                _dbContext.TraitGroupPresets.Add(new TraitGroupPreset
                {
                    Name = "Desktop-US",
                    Description = "桌面美国区域",
                    Version = "0.1.0",
                    Tags = "desktop,us",
                    ItemsJson = "{\n  \"browser.platform\": \"Win32\",\n  \"system.locale\": \"en-US\",\n  \"system.timezone\": \"America/New_York\",\n  \"device.viewport.width\": 1920,\n  \"device.viewport.height\": 1080\n}"
                });
                _dbContext.SaveChanges();
            }

            // Catalog version
            if (!_dbContext.CatalogVersions.Any())
            {
                _dbContext.CatalogVersions.Add(new CatalogVersion
                {
                    Version = "0.1.0",
                    PublishedAt = DateTime.Now,
                    Changelog = "Initial minimal meta catalog"
                });
                _dbContext.SaveChanges();
            }

            _logService.LogInfo("Database", "Meta catalog seed complete");
        }
        catch (Exception ex)
        {
            _logService.LogError("Database", $"Failed to seed meta catalog: {ex.Message}", ex.StackTrace);
        }
    }

    /// <summary>
    /// 同步数据库表结构
    /// 自动添加新列，不删除现有数据
    /// </summary>
    public void SyncSchema()
    {
        try
        {
            _logService.LogInfo("Database", "Syncing database schema via FreeSql CodeFirst...");

            // 使用 EF 模型发现的所有实体类型，并对齐表名
            var efEntityTypes = _dbContext.Model.GetEntityTypes()
                .Where(t => t.ClrType.IsClass && !t.ClrType.IsAbstract)
                .ToList();

            foreach (var efEntity in efEntityTypes)
            {
                var tableName = efEntity.GetSchemaQualifiedTableName() ?? efEntity.Name;
                // 对齐 FreeSql 实体表名到 EF 的表名
                _fsql.CodeFirst.ConfigEntity(efEntity.ClrType, e => e.Name(tableName));
            }

            var entityClrTypes = efEntityTypes.Select(t => t.ClrType).Distinct().ToArray();

            // 使用 FreeSql CodeFirst 同步表结构（仅创建/改列，不删数据）
            _fsql.CodeFirst.SyncStructure(entityClrTypes);

            _logService.LogInfo("Database", "Database schema synced successfully (FreeSql CodeFirst)");
        }
        catch (Exception ex)
        {
            _logService.LogError("Database", $"Failed to sync schema (FreeSql): {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// 同步表的列
    /// </summary>
    private void SyncTableColumns(Microsoft.EntityFrameworkCore.Metadata.IEntityType entityType, string tableName)
    {
        try
        {
            var properties = entityType.GetProperties();
            var existingColumns = GetTableColumns(tableName);

            foreach (var property in properties)
            {
                var columnName = property.GetColumnName(StoreObjectIdentifier.Table(tableName)) ?? property.Name;

                if (!existingColumns.Contains(columnName))
                {
                    _logService.LogInfo("Database", $"Adding column: {tableName}.{columnName}");
                    AddColumn(tableName, columnName, property);
                }
            }
        }
        catch (Exception ex)
        {
            _logService.LogError("Database", $"Failed to sync columns for {tableName}: {ex.Message}", ex.StackTrace);
        }
    }

    /// <summary>
    /// 检查表是否存在
    /// </summary>
    private bool TableExists(string tableName)
    {
        try
        {
            var connection = _dbContext.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'";
                var result = command.ExecuteScalar();
                return result != null;
            }
        }
        catch (Exception ex)
        {
            _logService.LogError("Database", $"Failed to check table existence: {ex.Message}", ex.StackTrace);
            return false;
        }
    }

    /// <summary>
    /// 获取表的所有列
    /// </summary>
    private HashSet<string> GetTableColumns(string tableName)
    {
        var columns = new HashSet<string>();

        try
        {
            var connection = _dbContext.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"PRAGMA table_info({tableName})";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var columnName = reader.GetString(1);
                        columns.Add(columnName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logService.LogError("Database", $"Failed to get table columns: {ex.Message}", ex.StackTrace);
        }

        return columns;
    }

    /// <summary>
    /// 添加列到表
    /// </summary>
    private void AddColumn(string tableName, string columnName, Microsoft.EntityFrameworkCore.Metadata.IProperty property)
    {
        try
        {
            var columnType = GetSqliteColumnType(property.ClrType);
            var nullable = property.IsNullable ? "" : " NOT NULL";
            var defaultValue = GetDefaultValue(property);

            var sql = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType}{nullable}{defaultValue}";

            var connection = _dbContext.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }

            _logService.LogInfo("Database", $"Column added: {tableName}.{columnName}");
        }
        catch (Exception ex)
        {
            _logService.LogError("Database", $"Failed to add column {tableName}.{columnName}: {ex.Message}", ex.StackTrace);
        }
    }

    /// <summary>
    /// 获取 SQLite 列类型
    /// </summary>
    private string GetSqliteColumnType(Type clrType)
    {
        if (clrType == typeof(int) || clrType == typeof(int?))
            return "INTEGER";
        if (clrType == typeof(long) || clrType == typeof(long?))
            return "INTEGER";
        if (clrType == typeof(bool) || clrType == typeof(bool?))
            return "INTEGER";
        if (clrType == typeof(decimal) || clrType == typeof(decimal?))
            return "REAL";
        if (clrType == typeof(double) || clrType == typeof(double?))
            return "REAL";
        if (clrType == typeof(DateTime) || clrType == typeof(DateTime?))
            return "TEXT";
        if (clrType == typeof(Guid) || clrType == typeof(Guid?))
            return "TEXT";
        
        return "TEXT";
    }

    /// <summary>
    /// 获取默认值
    /// </summary>
    private string GetDefaultValue(Microsoft.EntityFrameworkCore.Metadata.IProperty property)
    {
        try
        {
            var defaultValue = property.GetDefaultValue();
            if (defaultValue == null)
                return "";

            if (defaultValue is bool boolValue)
                return $" DEFAULT {(boolValue ? 1 : 0)}";
            if (defaultValue is int intValue)
                return $" DEFAULT {intValue}";
            if (defaultValue is string stringValue)
                return $" DEFAULT '{stringValue}'";

            return "";
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// 获取数据库统计信息
    /// </summary>
    public DatabaseStatistics GetStatistics()
    {
        try
        {
            var stats = new DatabaseStatistics();
            var connection = _dbContext.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            // 获取表数量
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table'";
                stats.TableCount = (long)command.ExecuteScalar();
            }

            // 获取数据库大小
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT page_count * page_size FROM pragma_page_count(), pragma_page_size()";
                var result = command.ExecuteScalar();
                stats.DatabaseSizeBytes = result != null ? (long)result : 0;
            }

            // 获取每个表的行数
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var tableName = reader.GetString(0);
                        stats.TableRowCounts[tableName] = 0;
                    }
                }
            }

            return stats;
        }
        catch (Exception ex)
        {
            _logService.LogError("Database", $"Failed to get statistics: {ex.Message}", ex.StackTrace);
            return new DatabaseStatistics();
        }
    }

}

/// <summary>
/// 数据库统计信息
/// </summary>
public class DatabaseStatistics
{
    public long TableCount { get; set; }
    public long DatabaseSizeBytes { get; set; }
    public Dictionary<string, long> TableRowCounts { get; set; } = new();

    public string GetFormattedSize()
    {
        const long kb = 1024;
        const long mb = kb * 1024;
        const long gb = mb * 1024;

        if (DatabaseSizeBytes >= gb)
            return $"{DatabaseSizeBytes / (double)gb:F2} GB";
        if (DatabaseSizeBytes >= mb)
            return $"{DatabaseSizeBytes / (double)mb:F2} MB";
        if (DatabaseSizeBytes >= kb)
            return $"{DatabaseSizeBytes / (double)kb:F2} KB";

        return $"{DatabaseSizeBytes} B";
    }
}
