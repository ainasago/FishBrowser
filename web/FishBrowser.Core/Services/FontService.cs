using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FishBrowser.WPF.Services
{
    public class FontService
    {
        private readonly WebScraperDbContext _db;
        private readonly LogService _log;
        private static readonly string SeedPath = Path.Combine(AppContext.BaseDirectory ?? Directory.GetCurrentDirectory(), "assets", "fonts", "fonts_seed.json");

        public FontService(WebScraperDbContext db, LogService log)
        {
            _db = db;
            _log = log;
        }

        public async Task ImportSeedAsync()
        {
            try
            {
                _log.LogInfo("FontSeed", $"Starting font seed import from: {SeedPath}");
                
                if (!File.Exists(SeedPath))
                {
                    _log.LogWarn("FontSeed", $"Seed file not found: {SeedPath}");
                    return;
                }
                
                _log.LogInfo("FontSeed", "Seed file found, reading...");
                var json = await File.ReadAllTextAsync(SeedPath);
                _log.LogInfo("FontSeed", $"Seed file size: {json.Length} bytes");
                
                var items = JsonSerializer.Deserialize<List<SeedFont>>(json) ?? new();
                _log.LogInfo("FontSeed", $"Parsed {items.Count} font entries from seed file");
                
                if (items.Count == 0)
                {
                    _log.LogWarn("FontSeed", "Seed file is empty");
                    return;
                }

                var existingCount = await _db.Fonts.CountAsync();
                _log.LogInfo("FontSeed", $"Database currently has {existingCount} fonts");
                
                var existingNames = new HashSet<string>(await _db.Fonts.Select(f => f.Name).ToListAsync(), StringComparer.OrdinalIgnoreCase);
                _log.LogInfo("FontSeed", $"Loaded {existingNames.Count} existing font names");
                
                var toAdd = new List<Font>();
                var skipped = 0;
                foreach (var it in items)
                {
                    if (string.IsNullOrWhiteSpace(it.name))
                    {
                        skipped++;
                        continue;
                    }
                    if (existingNames.Contains(it.name))
                    {
                        skipped++;
                        continue;
                    }
                    toAdd.Add(new Font
                    {
                        Name = it.name.Trim(),
                        AliasesJson = it.aliases != null ? JsonSerializer.Serialize(it.aliases) : null,
                        Category = string.IsNullOrWhiteSpace(it.category) ? "Sans" : it.category!,
                        Vendor = string.IsNullOrWhiteSpace(it.vendor) ? "Other" : it.vendor!,
                        OsSupportJson = it.os != null ? JsonSerializer.Serialize(it.os) : null,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                
                _log.LogInfo("FontSeed", $"Skipped {skipped} entries (empty or duplicate), adding {toAdd.Count} new fonts");
                
                if (toAdd.Count > 0)
                {
                    await _db.Fonts.AddRangeAsync(toAdd);
                    await _db.SaveChangesAsync();
                    _log.LogInfo("FontSeed", $"Successfully imported {toAdd.Count} fonts to database");
                    
                    var finalCount = await _db.Fonts.CountAsync();
                    _log.LogInfo("FontSeed", $"Database now has {finalCount} total fonts");
                }
                else
                {
                    _log.LogInfo("FontSeed", "No new fonts to import (all entries already exist or are invalid)");
                }
            }
            catch (Exception ex)
            {
                _log.LogError("FontSeed", $"Import failed: {ex.Message}", ex.StackTrace);
            }
        }

        public async Task<List<Font>> GetAllAsync()
        {
            try
            {
                var fonts = await _db.Fonts.AsNoTracking().OrderBy(f => f.Name).ToListAsync();
                _log.LogInfo("FontService", $"GetAllAsync returned {fonts.Count} fonts");
                return fonts;
            }
            catch (Exception ex)
            {
                _log.LogError("FontService", $"GetAllAsync failed: {ex.Message}", ex.StackTrace);
                return new();
            }
        }

        public async Task<List<Font>> SearchAsync(string? query, string? os = null)
        {
            try
            {
                query = (query ?? string.Empty).Trim();
                
                // 规范化 OS 名称（大小写不敏感）
                var normalizedOs = NormalizeOsName(os);
                
                var q = _db.Fonts.AsNoTracking().AsQueryable();
                if (!string.IsNullOrEmpty(query)) q = q.Where(f => EF.Functions.Like(f.Name, $"%{query}%") || (f.AliasesJson ?? "").Contains(query));
                if (!string.IsNullOrWhiteSpace(normalizedOs)) q = q.Where(f => (f.OsSupportJson ?? "").Contains(normalizedOs));
                var result = await q.OrderBy(f => f.Name).ToListAsync();
                _log.LogInfo("FontService", $"SearchAsync(query='{query}', os='{os}' -> '{normalizedOs}') returned {result.Count} fonts");
                return result;
            }
            catch (Exception ex)
            {
                _log.LogError("FontService", $"SearchAsync failed: {ex.Message}", ex.StackTrace);
                return new();
            }
        }
        
        /// <summary>
        /// 规范化操作系统名称（大小写不敏感）
        /// </summary>
        private string? NormalizeOsName(string? os)
        {
            if (string.IsNullOrWhiteSpace(os)) return null;
            
            return os.ToLower() switch
            {
                "windows" => "Windows",
                "macos" => "macOS",
                "linux" => "Linux",
                "android" => "Android",
                "ios" => "iOS",
                _ => os // 保持原样
            };
        }

        public async Task<List<string>> RandomSubsetAsync(string os, int count)
        {
            try
            {
                var eligible = await SearchAsync(null, os);
                _log.LogInfo("FontService", $"RandomSubsetAsync: Found {eligible.Count} eligible fonts for OS '{os}'");
                var rng = new Random();
                var result = eligible.OrderBy(_ => rng.Next()).Take(Math.Max(0, count)).Select(f => f.Name).ToList();
                _log.LogInfo("FontService", $"RandomSubsetAsync: Selected {result.Count} random fonts from {eligible.Count}");
                return result;
            }
            catch (Exception ex)
            {
                _log.LogError("FontService", $"RandomSubsetAsync failed: {ex.Message}", ex.StackTrace);
                return new();
            }
        }

        private record SeedFont(string name, string? category, string? vendor, string[]? os, string[]? aliases);
    }
}
