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
    public class GpuCatalogService
    {
        private readonly WebScraperDbContext _db;
        private readonly LogService _log;
        private static readonly string SeedPath = Path.Combine(AppContext.BaseDirectory ?? Directory.GetCurrentDirectory(), "assets", "webgl", "webgl_gpu_seed.json");

        public GpuCatalogService(WebScraperDbContext db, LogService log)
        {
            _db = db;
            _log = log;
        }

        public async Task ImportSeedAsync()
        {
            try
            {
                _log.LogInfo("GpuSeed", $"Starting GPU seed import from: {SeedPath}");
                
                if (!File.Exists(SeedPath))
                {
                    _log.LogWarn("GpuSeed", $"Seed file not found: {SeedPath}");
                    return;
                }
                
                _log.LogInfo("GpuSeed", "Seed file found, reading...");
                var json = await File.ReadAllTextAsync(SeedPath);
                _log.LogInfo("GpuSeed", $"Seed file size: {json.Length} bytes");
                
                var items = JsonSerializer.Deserialize<List<SeedGpu>>(json) ?? new();
                _log.LogInfo("GpuSeed", $"Parsed {items.Count} GPU entries from seed file");
                
                if (items.Count == 0)
                {
                    _log.LogWarn("GpuSeed", "Seed file is empty");
                    return;
                }

                var existingCount = await _db.GpuInfos.CountAsync();
                _log.LogInfo("GpuSeed", $"Database currently has {existingCount} GPU entries");
                
                var existingKeys = new HashSet<string>(
                    await _db.GpuInfos.Select(g => $"{g.Vendor}|{g.Renderer}").ToListAsync(),
                    StringComparer.OrdinalIgnoreCase
                );
                _log.LogInfo("GpuSeed", $"Loaded {existingKeys.Count} existing GPU keys");
                
                var toAdd = new List<GpuInfo>();
                var skipped = 0;
                foreach (var it in items)
                {
                    if (string.IsNullOrWhiteSpace(it.vendor) || string.IsNullOrWhiteSpace(it.renderer))
                    {
                        skipped++;
                        continue;
                    }
                    var key = $"{it.vendor}|{it.renderer}";
                    if (existingKeys.Contains(key))
                    {
                        skipped++;
                        continue;
                    }
                    toAdd.Add(new GpuInfo
                    {
                        Vendor = it.vendor.Trim(),
                        Renderer = it.renderer.Trim(),
                        Adapter = it.adapter,
                        DriverVersion = it.driverVersion,
                        Api = it.api ?? "WebGL",
                        Backend = it.backend ?? "ANGLE-D3D11",
                        LimitsJson = it.limits != null ? JsonSerializer.Serialize(it.limits) : null,
                        ExtensionsJson = it.extensions != null ? JsonSerializer.Serialize(it.extensions) : null,
                        OsSupportJson = it.os != null ? JsonSerializer.Serialize(it.os) : null,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                
                _log.LogInfo("GpuSeed", $"Skipped {skipped} entries (invalid or duplicate), adding {toAdd.Count} new GPU entries");
                
                if (toAdd.Count > 0)
                {
                    await _db.GpuInfos.AddRangeAsync(toAdd);
                    await _db.SaveChangesAsync();
                    _log.LogInfo("GpuSeed", $"Successfully imported {toAdd.Count} GPU entries to database");
                    
                    var finalCount = await _db.GpuInfos.CountAsync();
                    _log.LogInfo("GpuSeed", $"Database now has {finalCount} total GPU entries");
                }
                else
                {
                    _log.LogInfo("GpuSeed", "No new GPU entries to import (all entries already exist or are invalid)");
                }
            }
            catch (Exception ex)
            {
                _log.LogError("GpuSeed", $"Import failed: {ex.Message}", ex.StackTrace);
            }
        }

        public async Task<List<GpuInfo>> SearchWebGLAsync(string? query = null, string? vendor = null, string? os = null)
        {
            try
            {
                query = (query ?? string.Empty).Trim();
                var q = _db.GpuInfos.AsNoTracking().AsQueryable();
                
                if (!string.IsNullOrEmpty(query))
                    q = q.Where(g => EF.Functions.Like(g.Vendor, $"%{query}%") || EF.Functions.Like(g.Renderer, $"%{query}%"));
                
                if (!string.IsNullOrWhiteSpace(vendor))
                    q = q.Where(g => g.Vendor.Contains(vendor));
                
                if (!string.IsNullOrWhiteSpace(os))
                    q = q.Where(g => (g.OsSupportJson ?? "").Contains(os));
                
                var result = await q.OrderBy(g => g.Vendor).ThenBy(g => g.Renderer).ToListAsync();
                _log.LogInfo("GpuCatalog", $"SearchWebGL(query='{query}', vendor='{vendor}', os='{os}') returned {result.Count} entries");
                return result;
            }
            catch (Exception ex)
            {
                _log.LogError("GpuCatalog", $"SearchWebGL failed: {ex.Message}", ex.StackTrace);
                return new();
            }
        }

        public async Task<List<GpuInfo>> RandomSubsetAsync(string os, int count)
        {
            try
            {
                var eligible = await SearchWebGLAsync(null, null, os);
                _log.LogInfo("GpuCatalog", $"RandomSubset: Found {eligible.Count} eligible GPU entries for OS '{os}'");
                var rng = new Random();
                var result = eligible.OrderBy(_ => rng.Next()).Take(Math.Max(0, count)).ToList();
                _log.LogInfo("GpuCatalog", $"RandomSubset: Selected {result.Count} random GPU entries from {eligible.Count}");
                return result;
            }
            catch (Exception ex)
            {
                _log.LogError("GpuCatalog", $"RandomSubset failed: {ex.Message}", ex.StackTrace);
                return new();
            }
        }

        private record SeedGpu(string vendor, string renderer, string? adapter, string? driverVersion, string? api, string? backend, Dictionary<string, object>? limits, string[]? extensions, string[]? os);
    }
}
