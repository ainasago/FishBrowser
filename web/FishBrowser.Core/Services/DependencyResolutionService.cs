using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FishBrowser.WPF.Services;

/// <summary>
/// 依赖/冲突解析与拓扑排序服务
/// </summary>
public class DependencyResolutionService
{
    private readonly WebScraperDbContext _db;

    public DependencyResolutionService(WebScraperDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 解析 Trait 的依赖关系（拓扑排序）
    /// </summary>
    public List<string> ResolveOrder(Dictionary<string, object?> traits)
    {
        var order = new List<string>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        foreach (var key in traits.Keys)
        {
            if (!visited.Contains(key))
            {
                DepthFirstSearch(key, traits, order, visited, visiting);
            }
        }

        return order;
    }

    private void DepthFirstSearch(string key, Dictionary<string, object?> traits, List<string> order, HashSet<string> visited, HashSet<string> visiting)
    {
        if (visited.Contains(key)) return;
        if (visiting.Contains(key)) throw new InvalidOperationException($"循环依赖检测到: {key}");

        visiting.Add(key);

        var def = _db.TraitDefinitions.FirstOrDefault(d => d.Key == key);
        if (def != null && !string.IsNullOrWhiteSpace(def.DependenciesJson))
        {
            try
            {
                var deps = JsonSerializer.Deserialize<List<string>>(def.DependenciesJson) ?? new();
                foreach (var dep in deps)
                {
                    if (traits.ContainsKey(dep) && !visited.Contains(dep))
                    {
                        DepthFirstSearch(dep, traits, order, visited, visiting);
                    }
                }
            }
            catch { /* ignore parse errors */ }
        }

        visiting.Remove(key);
        visited.Add(key);
        order.Add(key);
    }

    /// <summary>
    /// 检测冲突并返回冲突说明
    /// </summary>
    public List<string> DetectConflicts(Dictionary<string, object?> traits)
    {
        var conflicts = new List<string>();

        // 检查每个 Trait 的冲突规则
        foreach (var key in traits.Keys)
        {
            var def = _db.TraitDefinitions.FirstOrDefault(d => d.Key == key);
            if (def == null || string.IsNullOrWhiteSpace(def.ConflictsJson)) continue;

            try
            {
                var conflictRules = JsonSerializer.Deserialize<Dictionary<string, object?>>(def.ConflictsJson) ?? new();
                foreach (var rule in conflictRules)
                {
                    var conflictKey = rule.Key;
                    if (!traits.ContainsKey(conflictKey)) continue;

                    var currentVal = JsonSerializer.Serialize(traits[key]);
                    var conflictVal = JsonSerializer.Serialize(traits[conflictKey]);
                    var ruleVal = JsonSerializer.Serialize(rule.Value);

                    // 简单规则：如果冲突键存在且值匹配规则，则冲突
                    if (conflictVal == ruleVal)
                    {
                        conflicts.Add($"'{key}' 与 '{conflictKey}' 冲突: {key}={currentVal} 不应与 {conflictKey}={conflictVal} 共存");
                    }
                }
            }
            catch { /* ignore parse errors */ }
        }

        return conflicts;
    }

    /// <summary>
    /// 应用依赖修复（自动补全依赖的缺失值）
    /// </summary>
    public void ApplyDependencyFixes(Dictionary<string, object?> traits)
    {
        var order = ResolveOrder(traits);
        var fixed_keys = new HashSet<string>(traits.Keys);

        foreach (var key in order)
        {
            var def = _db.TraitDefinitions.FirstOrDefault(d => d.Key == key);
            if (def == null || string.IsNullOrWhiteSpace(def.DependenciesJson)) continue;

            try
            {
                var deps = JsonSerializer.Deserialize<List<string>>(def.DependenciesJson) ?? new();
                foreach (var dep in deps)
                {
                    if (!traits.ContainsKey(dep))
                    {
                        // 尝试用默认值填充
                        var depDef = _db.TraitDefinitions.FirstOrDefault(d => d.Key == dep);
                        if (depDef != null && !string.IsNullOrWhiteSpace(depDef.DefaultValueJson))
                        {
                            try
                            {
                                using var doc = JsonDocument.Parse(depDef.DefaultValueJson);
                                traits[dep] = JsonElementToObject(doc.RootElement);
                            }
                            catch { /* ignore */ }
                        }
                    }
                }
            }
            catch { /* ignore */ }
        }
    }

    /// <summary>
    /// 验证 Trait 组合的合法性
    /// </summary>
    public ValidationResult Validate(Dictionary<string, object?> traits)
    {
        var result = new ValidationResult();

        // 检测循环依赖
        try
        {
            ResolveOrder(traits);
        }
        catch (InvalidOperationException ex)
        {
            result.Errors.Add(ex.Message);
        }

        // 检测冲突
        var conflicts = DetectConflicts(traits);
        result.Warnings.AddRange(conflicts);

        // 检查必需字段
        foreach (var key in traits.Keys)
        {
            var def = _db.TraitDefinitions.FirstOrDefault(d => d.Key == key);
            if (def == null)
            {
                result.Warnings.Add($"未知的 Trait: {key}");
            }
        }

        return result;
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
            case JsonValueKind.Null: return null;
            case JsonValueKind.Array:
                var arr = new List<object?>();
                foreach (var item in el.EnumerateArray())
                    arr.Add(JsonElementToObject(item));
                return arr;
            case JsonValueKind.Object:
                var obj = new Dictionary<string, object?>();
                foreach (var prop in el.EnumerateObject())
                    obj[prop.Name] = JsonElementToObject(prop.Value);
                return obj;
            default: return null;
        }
    }

    public class ValidationResult
    {
        public List<string> Errors { get; } = new();
        public List<string> Warnings { get; } = new();
        public bool IsValid => Errors.Count == 0;
    }
}
