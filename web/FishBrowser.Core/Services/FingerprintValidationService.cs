using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FishBrowser.WPF.Services;

/// <summary>
/// 指纹校验服务 - 三维度评分系统（一致性、真实性、Cloudflare风险）
/// </summary>
public class FingerprintValidationService
{
    private readonly WebScraperDbContext _db;
    private readonly ILogService _logService;

    public FingerprintValidationService(WebScraperDbContext db, ILogService logService)
    {
        _db = db;
        _logService = logService;
    }

    /// <summary>
    /// 校验指纹并生成报告
    /// </summary>
    public async Task<FingerprintValidationReport> ValidateAsync(FingerprintProfile profile)
    {
        try
        {
            _logService.LogInfo("FingerprintValidationService", $"Starting validation for profile: {profile.Name}");

            var consistencyScore = CheckConsistency(profile);
            var realismScore = CheckRealism(profile);
            var cloudflareRiskScore = CheckCloudflareRisk(profile);

            // 计算总体评分: (一致性 + 真实性 + (100 - 风险)) / 3
            var totalScore = (consistencyScore + realismScore + (100 - cloudflareRiskScore)) / 3;

            // 确定风险等级
            var riskLevel = GetRiskLevel(totalScore);

            // 生成建议
            var recommendations = GenerateRecommendations(profile, consistencyScore, realismScore, cloudflareRiskScore);

            // 创建报告
            var report = new FingerprintValidationReport
            {
                FingerprintProfileId = profile.Id,
                TotalScore = (int)totalScore,
                ConsistencyScore = consistencyScore,
                RealisticScore = realismScore,
                CloudflareRiskScore = cloudflareRiskScore,
                RiskLevel = riskLevel,
                ValidatedAt = DateTime.UtcNow,
                ValidationVersion = "1.0"
            };

            // 序列化建议
            report.RecommendationsJson = JsonSerializer.Serialize(recommendations);

            _db.FingerprintValidationReports.Add(report);

            // 更新Profile的校验信息
            profile.RealisticScore = realismScore;
            profile.LastValidatedAt = DateTime.UtcNow;
            profile.LastValidationReportId = report.Id;
            profile.LastValidationReport = report;

            await _db.SaveChangesAsync();

            _logService.LogInfo("FingerprintValidationService", 
                $"Validation completed for profile {profile.Name}: Total={totalScore}, Risk={riskLevel}");

            return report;
        }
        catch (Exception ex)
        {
            _logService.LogError("FingerprintValidationService", $"Validation failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 检查一致性 (0-100)
    /// 检查: UA与Platform、Platform与Sec-CH-UA-Platform、Locale与Languages、Timezone与Locale
    /// </summary>
    private int CheckConsistency(FingerprintProfile profile)
    {
        var checks = new List<(string name, bool passed)>();

        // 1. UA与Platform一致性
        var uaPlatformMatch = profile.UserAgent.Contains(profile.Platform, StringComparison.OrdinalIgnoreCase);
        checks.Add(("UA-Platform Match", uaPlatformMatch));

        // 2. Platform与Sec-CH-UA-Platform一致性
        var platformSecChMatch = string.IsNullOrEmpty(profile.SecChUaPlatform) || 
            profile.SecChUaPlatform.Contains(profile.Platform, StringComparison.OrdinalIgnoreCase);
        checks.Add(("Platform-SecChUA Match", platformSecChMatch));

        // 3. Locale与Languages一致性
        var localeLanguagesMatch = string.IsNullOrEmpty(profile.LanguagesJson) || 
            profile.LanguagesJson.Contains(profile.Locale, StringComparison.OrdinalIgnoreCase);
        checks.Add(("Locale-Languages Match", localeLanguagesMatch));

        // 4. Timezone与Locale一致性
        var timezoneLocaleMatch = IsTimezoneLocaleConsistent(profile.Timezone, profile.Locale);
        checks.Add(("Timezone-Locale Match", timezoneLocaleMatch));

        // 计算得分
        var passedCount = checks.Count(c => c.passed);
        return (passedCount * 100) / checks.Count;
    }

    /// <summary>
    /// 检查真实性 (0-100)
    /// 检查: Chrome版本、硬件配置、GPU、字体
    /// </summary>
    private int CheckRealism(FingerprintProfile profile)
    {
        var checks = new List<(string name, int score)>();

        // 1. Chrome版本检查 (应该是141+)
        var versionScore = profile.UserAgent.Contains("141", StringComparison.OrdinalIgnoreCase) ? 100 : 
                          profile.UserAgent.Contains("140", StringComparison.OrdinalIgnoreCase) ? 80 : 60;
        checks.Add(("Chrome Version", versionScore));

        // 2. 硬件配置检查 (8-16核、8-32GB)
        var hardwareScore = (profile.HardwareConcurrency >= 8 && profile.HardwareConcurrency <= 16) ? 100 : 
                           (profile.HardwareConcurrency >= 4 && profile.HardwareConcurrency <= 32) ? 70 : 40;
        checks.Add(("Hardware Config", hardwareScore));

        // 3. GPU检查 (是否有WebGL配置)
        var gpuScore = !string.IsNullOrEmpty(profile.WebGLVendor) && !string.IsNullOrEmpty(profile.WebGLRenderer) ? 100 : 50;
        checks.Add(("GPU Config", gpuScore));

        // 4. 字体检查 (是否有字体配置)
        var fontScore = !string.IsNullOrEmpty(profile.FontsJson) ? 100 : 50;
        checks.Add(("Fonts Config", fontScore));

        // 5. 防检测数据检查
        var antiDetectionScore = (!string.IsNullOrEmpty(profile.PluginsJson) && 
                                 !string.IsNullOrEmpty(profile.LanguagesJson) &&
                                 !string.IsNullOrEmpty(profile.SecChUa)) ? 100 : 60;
        checks.Add(("Anti-Detection Data", antiDetectionScore));

        // 计算平均得分
        return (int)checks.Average(c => c.score);
    }

    /// <summary>
    /// 检查Cloudflare风险 (0-100，越低越好)
    /// 检查: HeadlessChrome标志、防检测数据完整性、屏幕分辨率、webdriver标志
    /// </summary>
    private int CheckCloudflareRisk(FingerprintProfile profile)
    {
        var riskScore = 0;

        // 1. HeadlessChrome标志 (包含则风险+30)
        if (profile.UserAgent.Contains("HeadlessChrome", StringComparison.OrdinalIgnoreCase))
            riskScore += 30;

        // 2. 防检测数据缺失 (缺少则风险+20)
        if (string.IsNullOrEmpty(profile.PluginsJson))
            riskScore += 20;
        if (string.IsNullOrEmpty(profile.LanguagesJson))
            riskScore += 20;
        if (string.IsNullOrEmpty(profile.SecChUa))
            riskScore += 20;

        // 3. 屏幕分辨率异常 (1920x1080以外则风险+15)
        if (profile.ViewportWidth != 1280 && profile.ViewportWidth != 1366 && profile.ViewportWidth != 1920)
            riskScore += 15;

        // 4. webdriver标志 (false则风险+10，因为真实Chrome是true)
        // 注: 这里假设webdriver标志已在防检测脚本中处理

        // 5. 触摸点数异常 (桌面应该是0)
        if (profile.MaxTouchPoints > 0)
            riskScore += 10;

        // 6. 网络配置异常 (RTT过低或速度过高)
        if (profile.ConnectionRtt < 20 || profile.ConnectionDownlink > 100)
            riskScore += 15;

        return Math.Min(riskScore, 100);
    }

    /// <summary>
    /// 获取风险等级
    /// </summary>
    private string GetRiskLevel(double totalScore)
    {
        return totalScore switch
        {
            >= 90 => "safe",
            >= 70 => "low",
            >= 50 => "medium",
            >= 30 => "high",
            _ => "critical"
        };
    }

    /// <summary>
    /// 生成建议
    /// </summary>
    private List<string> GenerateRecommendations(FingerprintProfile profile, int consistency, int realism, int cloudflareRisk)
    {
        var recommendations = new List<string>();

        if (consistency < 70)
            recommendations.Add("⚠️ 一致性评分较低，建议检查UA、Platform、Languages的匹配度");

        if (realism < 70)
        {
            if (string.IsNullOrEmpty(profile.WebGLVendor))
                recommendations.Add("⚠️ 缺少WebGL配置，建议添加GPU信息");
            if (string.IsNullOrEmpty(profile.FontsJson))
                recommendations.Add("⚠️ 缺少字体配置，建议添加字体列表");
            if (profile.HardwareConcurrency < 8 || profile.HardwareConcurrency > 16)
                recommendations.Add("⚠️ 硬件配置不合理，建议设置8-16核心");
        }

        if (cloudflareRisk > 50)
        {
            if (string.IsNullOrEmpty(profile.PluginsJson))
                recommendations.Add("🔴 缺少Plugins数据，Cloudflare可能检测到自动化");
            if (string.IsNullOrEmpty(profile.LanguagesJson))
                recommendations.Add("🔴 缺少Languages数据，Cloudflare可能检测到自动化");
            if (string.IsNullOrEmpty(profile.SecChUa))
                recommendations.Add("🔴 缺少Sec-CH-UA数据，Cloudflare可能检测到自动化");
            if (profile.ConnectionRtt < 20)
                recommendations.Add("🔴 网络延迟过低，可能被检测为自动化");
        }

        if (recommendations.Count == 0)
            recommendations.Add("✅ 指纹配置良好，可以使用");

        return recommendations;
    }

    /// <summary>
    /// 检查Timezone与Locale是否一致
    /// </summary>
    private bool IsTimezoneLocaleConsistent(string timezone, string locale)
    {
        // 简单的一致性检查
        var localeRegion = locale.Split('-').LastOrDefault()?.ToUpper() ?? "";
        
        return timezone switch
        {
            "Asia/Shanghai" => locale.StartsWith("zh"),
            "Asia/Tokyo" => locale.StartsWith("ja"),
            "Europe/London" => locale.StartsWith("en"),
            "America/New_York" => locale.StartsWith("en"),
            _ => true  // 其他情况认为一致
        };
    }

    /// <summary>
    /// 获取指纹的所有校验报告
    /// </summary>
    public async Task<List<FingerprintValidationReport>> GetProfileReportsAsync(int profileId)
    {
        try
        {
            return await _db.FingerprintValidationReports
                .Where(r => r.FingerprintProfileId == profileId)
                .OrderByDescending(r => r.ValidatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logService.LogError("FingerprintValidationService", $"Failed to get profile reports: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 删除校验报告
    /// </summary>
    public async Task DeleteReportAsync(int reportId)
    {
        try
        {
            var report = await _db.FingerprintValidationReports.FindAsync(reportId);
            if (report != null)
            {
                _db.FingerprintValidationReports.Remove(report);
                await _db.SaveChangesAsync();
                _logService.LogInfo("FingerprintValidationService", $"Deleted validation report: {reportId}");
            }
        }
        catch (Exception ex)
        {
            _logService.LogError("FingerprintValidationService", $"Failed to delete report: {ex.Message}");
            throw;
        }
    }
}
