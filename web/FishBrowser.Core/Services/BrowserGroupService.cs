using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Services;

/// <summary>
/// 浏览器分组服务 - 管理浏览器分组、默认配置和校验规则
/// </summary>
public class BrowserGroupService
{
    private readonly WebScraperDbContext _db;
    private readonly ILogService _logService;

    public BrowserGroupService(WebScraperDbContext db, ILogService logService)
    {
        _db = db;
        _logService = logService;
    }

    /// <summary>
    /// 创建浏览器分组
    /// </summary>
    public async Task<BrowserGroup> CreateGroupAsync(string name, string? description = null, string? icon = null)
    {
        try
        {
            var group = new BrowserGroup
            {
                Name = name,
                Description = description,
                Icon = icon ?? "🌐",
                CreatedAt = DateTime.UtcNow
            };

            _db.BrowserGroups.Add(group);
            await _db.SaveChangesAsync();

            _logService.LogInfo("BrowserGroupService", $"Created browser group: {name} (ID: {group.Id})");
            return group;
        }
        catch (Exception ex)
        {
            _logService.LogError("BrowserGroupService", $"Failed to create browser group: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 获取所有浏览器分组
    /// </summary>
    public async Task<List<BrowserGroup>> GetAllGroupsAsync()
    {
        try
        {
            return await _db.BrowserGroups
                .Include(g => g.Environments)
                .Include(g => g.ValidationRules)
                .OrderBy(g => g.Order)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logService.LogError("BrowserGroupService", $"Failed to get browser groups: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 获取指定分组
    /// </summary>
    public async Task<BrowserGroup?> GetGroupByIdAsync(int groupId)
    {
        try
        {
            return await _db.BrowserGroups
                .Include(g => g.Environments)
                .Include(g => g.ValidationRules)
                .FirstOrDefaultAsync(g => g.Id == groupId);
        }
        catch (Exception ex)
        {
            _logService.LogError("BrowserGroupService", $"Failed to get browser group {groupId}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 更新浏览器分组
    /// </summary>
    public async Task<BrowserGroup> UpdateGroupAsync(int groupId, string? name = null, string? description = null, 
        string? icon = null, int? minRealisticScore = null, int? maxCloudflareRiskScore = null)
    {
        try
        {
            var group = await _db.BrowserGroups.FindAsync(groupId);
            if (group == null)
                throw new InvalidOperationException($"Browser group {groupId} not found");

            if (!string.IsNullOrEmpty(name))
                group.Name = name;
            if (description != null)
                group.Description = description;
            if (!string.IsNullOrEmpty(icon))
                group.Icon = icon;
            if (minRealisticScore.HasValue)
                group.MinRealisticScore = minRealisticScore.Value;
            if (maxCloudflareRiskScore.HasValue)
                group.MaxCloudflareRiskScore = maxCloudflareRiskScore.Value;

            group.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logService.LogInfo("BrowserGroupService", $"Updated browser group: {groupId}");
            return group;
        }
        catch (Exception ex)
        {
            _logService.LogError("BrowserGroupService", $"Failed to update browser group {groupId}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 删除浏览器分组
    /// </summary>
    public async Task DeleteGroupAsync(int groupId)
    {
        try
        {
            var group = await _db.BrowserGroups.FindAsync(groupId);
            if (group == null)
                throw new InvalidOperationException($"Browser group {groupId} not found");

            _db.BrowserGroups.Remove(group);
            await _db.SaveChangesAsync();

            _logService.LogInfo("BrowserGroupService", $"Deleted browser group: {groupId}");
        }
        catch (Exception ex)
        {
            _logService.LogError("BrowserGroupService", $"Failed to delete browser group {groupId}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 获取分组内的所有浏览器环境
    /// </summary>
    public async Task<List<BrowserEnvironment>> GetGroupEnvironmentsAsync(int groupId)
    {
        try
        {
            return await _db.BrowserEnvironments
                .Where(e => e.GroupId == groupId)
                .Include(e => e.FingerprintProfile)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logService.LogError("BrowserGroupService", $"Failed to get group environments: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 获取分组的校验规则
    /// </summary>
    public async Task<List<Models.ValidationRule>> GetGroupValidationRulesAsync(int groupId)
    {
        try
        {
            return await _db.ValidationRules
                .Where(r => r.BrowserGroupId == groupId && r.IsEnabled)
                .OrderByDescending(r => r.Priority)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logService.LogError("BrowserGroupService", $"Failed to get group validation rules: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 添加校验规则到分组
    /// </summary>
    public async Task<Models.ValidationRule> AddValidationRuleAsync(int groupId, string name, string ruleType, 
        int weight = 100, string? configJson = null)
    {
        try
        {
            var rule = new Models.ValidationRule
            {
                BrowserGroupId = groupId,
                Name = name,
                RuleType = ruleType,
                Weight = weight,
                ConfigJson = configJson,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.ValidationRules.Add(rule);
            await _db.SaveChangesAsync();

            _logService.LogInfo("BrowserGroupService", $"Added validation rule: {name} to group {groupId}");
            return rule;
        }
        catch (Exception ex)
        {
            _logService.LogError("BrowserGroupService", $"Failed to add validation rule: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 检查指纹是否满足分组的校验规则
    /// </summary>
    public async Task<bool> ValidateProfileForGroupAsync(int groupId, FingerprintProfile profile)
    {
        try
        {
            var group = await GetGroupByIdAsync(groupId);
            if (group == null)
                return false;

            // 检查最小真实性评分
            if (profile.RealisticScore < group.MinRealisticScore)
            {
                _logService.LogWarn("BrowserGroupService", 
                    $"Profile {profile.Id} fails realism check: {profile.RealisticScore} < {group.MinRealisticScore}");
                return false;
            }

            // 检查最大Cloudflare风险评分
            if (profile.LastValidationReport != null && 
                profile.LastValidationReport.CloudflareRiskScore > group.MaxCloudflareRiskScore)
            {
                _logService.LogWarn("BrowserGroupService", 
                    $"Profile {profile.Id} fails Cloudflare risk check: {profile.LastValidationReport.CloudflareRiskScore} > {group.MaxCloudflareRiskScore}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logService.LogError("BrowserGroupService", $"Failed to validate profile for group: {ex.Message}");
            throw;
        }
    }
}
