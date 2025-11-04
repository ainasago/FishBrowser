using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FishBrowser.WPF.Services;

public class MetaCatalogIoService
{
    private readonly WebScraperDbContext _db;

    public MetaCatalogIoService(WebScraperDbContext db)
    {
        _db = db;
    }

    public async Task ExportAsync(string filePath)
    {
        var dto = new CatalogDto
        {
            SchemaVersion = "1.0",
            CatalogVersion = _db.CatalogVersions.OrderByDescending(v => v.Id).FirstOrDefault()?.Version ?? "0.1.0",
            ExportedAt = DateTime.UtcNow.ToString("O"),
            Categories = _db.TraitCategories
                .Select(c => new CategoryDto
                {
                    Name = c.Name,
                    Description = c.Description,
                    Order = c.Order,
                    Traits = c.Traits.OrderBy(t => t.Key).Select(t => new TraitDto
                    {
                        Key = t.Key,
                        DisplayName = t.DisplayName,
                        ValueType = t.ValueType.ToString(),
                        DefaultValueJson = t.DefaultValueJson,
                        Options = t.Options.Select(o => new OptionDto
                        {
                            ValueJson = o.ValueJson,
                            Label = o.Label,
                            Weight = o.Weight,
                            Region = o.Region,
                            Vendor = o.Vendor,
                            DeviceClass = o.DeviceClass
                        }).ToList()
                    }).ToList()
                }).OrderBy(c => c.Order).ToList(),
            Presets = _db.TraitGroupPresets.Select(p => new PresetDto
            {
                Name = p.Name,
                TraitsJson = p.ItemsJson, // map ItemsJson -> TraitsJson for IO schema
                Scope = p.Tags // map Tags -> Scope for IO schema
            }).ToList()
        };

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task ImportAsync(string filePath, bool merge = true)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var dto = JsonSerializer.Deserialize<CatalogDto>(json) ?? new CatalogDto();
        
        // 验证 Schema 版本
        if (string.IsNullOrWhiteSpace(dto.SchemaVersion))
            throw new InvalidOperationException("缺少 schemaVersion 字段");
        if (!dto.SchemaVersion.StartsWith("1."))
            throw new InvalidOperationException($"不支持的 Schema 版本: {dto.SchemaVersion}");
        
        // 记录导入时间和版本
        if (!string.IsNullOrWhiteSpace(dto.CatalogVersion))
        {
            var existing = _db.CatalogVersions.FirstOrDefault(v => v.Version == dto.CatalogVersion);
            if (existing == null)
            {
                _db.CatalogVersions.Add(new CatalogVersion 
                { 
                    Version = dto.CatalogVersion, 
                    PublishedAt = DateTime.UtcNow,
                    Changelog = $"Imported from {filePath} at {DateTime.UtcNow:O}"
                });
                _db.SaveChanges();
            }
        }

        foreach (var catDto in dto.Categories)
        {
            var cat = _db.TraitCategories.FirstOrDefault(c => c.Name == catDto.Name);
            if (cat == null)
            {
                cat = new TraitCategory { Name = catDto.Name, Description = catDto.Description, Order = catDto.Order };
                _db.TraitCategories.Add(cat);
                _db.SaveChanges();
            }
            else if (!merge)
            {
                cat.Description = catDto.Description;
                cat.Order = catDto.Order;
                _db.SaveChanges();
            }

            foreach (var traitDto in catDto.Traits)
            {
                var def = _db.TraitDefinitions.FirstOrDefault(d => d.Key == traitDto.Key);
                if (def == null)
                {
                    def = new TraitDefinition
                    {
                        Key = traitDto.Key,
                        DisplayName = traitDto.DisplayName,
                        CategoryId = cat.Id,
                        ValueType = Enum.TryParse<TraitValueType>(traitDto.ValueType, out var t) ? t : TraitValueType.String,
                        DefaultValueJson = traitDto.DefaultValueJson
                    };
                    _db.TraitDefinitions.Add(def);
                    _db.SaveChanges();
                }
                else if (!merge)
                {
                    def.DisplayName = traitDto.DisplayName;
                    def.CategoryId = cat.Id;
                    def.ValueType = Enum.TryParse<TraitValueType>(traitDto.ValueType, out var t) ? t : def.ValueType;
                    def.DefaultValueJson = traitDto.DefaultValueJson;
                    _db.SaveChanges();
                }

                foreach (var optDto in traitDto.Options)
                {
                    var exists = _db.TraitOptions.FirstOrDefault(o => o.TraitDefinitionId == def.Id && o.ValueJson == optDto.ValueJson);
                    if (exists == null)
                    {
                        _db.TraitOptions.Add(new TraitOption
                        {
                            TraitDefinitionId = def.Id,
                            ValueJson = optDto.ValueJson,
                            Label = optDto.Label,
                            Weight = optDto.Weight,
                            Region = optDto.Region,
                            Vendor = optDto.Vendor,
                            DeviceClass = optDto.DeviceClass
                        });
                        _db.SaveChanges();
                    }
                    else if (!merge)
                    {
                        exists.Label = optDto.Label;
                        exists.Weight = optDto.Weight;
                        exists.Region = optDto.Region;
                        exists.Vendor = optDto.Vendor;
                        exists.DeviceClass = optDto.DeviceClass;
                        _db.SaveChanges();
                    }
                }
            }
        }

        foreach (var p in dto.Presets)
        {
            var exist = _db.TraitGroupPresets.FirstOrDefault(x => x.Name == p.Name);
            if (exist == null)
            {
                _db.TraitGroupPresets.Add(new TraitGroupPreset
                {
                    Name = p.Name,
                    ItemsJson = p.TraitsJson, // map back to ItemsJson
                    Tags = p.Scope // map back to Tags
                });
                _db.SaveChanges();
            }
            else if (!merge)
            {
                exist.ItemsJson = p.TraitsJson;
                exist.Tags = p.Scope;
                _db.SaveChanges();
            }
        }

        if (!_db.CatalogVersions.Any())
        {
            _db.CatalogVersions.Add(new CatalogVersion { Version = dto.CatalogVersion ?? "0.1.0", PublishedAt = DateTime.Now });
            _db.SaveChanges();
        }
    }

    private class CatalogDto
    {
        public string? SchemaVersion { get; set; }
        public string? CatalogVersion { get; set; }
        public string? ExportedAt { get; set; }
        public List<CategoryDto> Categories { get; set; } = new();
        public List<PresetDto> Presets { get; set; } = new();
    }
    private class CategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Order { get; set; }
        public List<TraitDto> Traits { get; set; } = new();
    }
    private class TraitDto
    {
        public string Key { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string ValueType { get; set; } = "String";
        public string? DefaultValueJson { get; set; }
        public List<OptionDto> Options { get; set; } = new();
    }
    private class OptionDto
    {
        public string ValueJson { get; set; } = string.Empty;
        public string? Label { get; set; }
        public double Weight { get; set; }
        public string? Region { get; set; }
        public string? Vendor { get; set; }
        public string? DeviceClass { get; set; }
    }
    private class PresetDto
    {
        public string Name { get; set; } = string.Empty;
        public string TraitsJson { get; set; } = "{}";
        public string? Scope { get; set; }
    }
}
