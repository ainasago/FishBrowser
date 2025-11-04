using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FishBrowser.WPF.Services;

public class TraitsDatasetSeeder
{
    private readonly WebScraperDbContext _db;

    public TraitsDatasetSeeder(WebScraperDbContext db)
    {
        _db = db;
    }

    public int SeedFromFolder(string folder, bool merge = true)
    {
        if (!Directory.Exists(folder)) return 0;
        int inserted = 0;
        foreach (var file in Directory.GetFiles(folder, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("traits", out var traitsObj))
                {
                    foreach (var prop in traitsObj.EnumerateObject())
                    {
                        var key = prop.Name;
                        var arr = prop.Value;
                        inserted += ImportArray(key, arr, merge);
                    }
                }
                else if (root.TryGetProperty("trait", out var traitEl))
                {
                    var key = traitEl.GetString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(key)) continue;
                    if (root.TryGetProperty("options", out var arr))
                    {
                        inserted += ImportArray(key, arr, merge);
                    }
                }
            }
            catch { /* ignore invalid file */ }
        }
        return inserted;
    }

    private int ImportArray(string key, JsonElement arr, bool merge)
    {
        var def = _db.TraitDefinitions.FirstOrDefault(d => d.Key == key);
        if (def == null)
        {
            // auto-create definition if missing
            def = new TraitDefinition { Key = key, DisplayName = key, ValueType = GuessValueType(arr), CategoryId = _db.TraitCategories.First().Id };
            _db.TraitDefinitions.Add(def);
            _db.SaveChanges();
        }

        int count = 0;
        if (arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in arr.EnumerateArray())
            {
                string? valueJson = el.ValueKind is JsonValueKind.Object or JsonValueKind.Array ? el.GetRawText() : JsonSerializer.Serialize(ExtractValue(el));
                string? label = TryGetString(el, "label");
                double weight = TryGetDouble(el, "weight") ?? 1.0;
                string? region = TryGetString(el, "region");
                string? vendor = TryGetString(el, "vendor");
                string? deviceClass = TryGetString(el, "deviceClass");

                var exists = _db.TraitOptions.FirstOrDefault(o => o.TraitDefinitionId == def.Id && o.ValueJson == valueJson);
                if (exists == null)
                {
                    _db.TraitOptions.Add(new TraitOption
                    {
                        TraitDefinitionId = def.Id,
                        ValueJson = valueJson!,
                        Label = label,
                        Weight = weight,
                        Region = region,
                        Vendor = vendor,
                        DeviceClass = deviceClass
                    });
                    count++;
                }
                else if (!merge)
                {
                    exists.Label = label;
                    exists.Weight = weight;
                    exists.Region = region;
                    exists.Vendor = vendor;
                    exists.DeviceClass = deviceClass;
                }
            }
            _db.SaveChanges();
        }
        return count;
    }

    private static TraitValueType GuessValueType(JsonElement arr)
    {
        if (arr.ValueKind == JsonValueKind.Array && arr.GetArrayLength() > 0)
        {
            var first = arr[0];
            return first.ValueKind switch
            {
                JsonValueKind.String => TraitValueType.String,
                JsonValueKind.Number => TraitValueType.Number,
                JsonValueKind.True or JsonValueKind.False => TraitValueType.Bool,
                JsonValueKind.Array => TraitValueType.Array,
                JsonValueKind.Object => TraitValueType.Object,
                _ => TraitValueType.String
            };
        }
        return TraitValueType.String;
    }

    private static string? TryGetString(JsonElement el, string name) => el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    private static double? TryGetDouble(JsonElement el, string name) => el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;

    private static object? ExtractValue(JsonElement el)
    {
        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.TryGetDouble(out var d) ? d : null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => el.EnumerateArray().Select(ExtractValue).ToList(),
            JsonValueKind.Object => el.TryGetProperty("value", out var v) ? ExtractValue(v) : el.GetRawText(),
            _ => null
        };
    }
}
