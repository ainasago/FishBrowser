using System;
using System.Collections.Generic;

namespace FishBrowser.WPF.Models;

/// <summary>
/// 指纹校验报告 - 记录指纹的校验结果和评分
/// </summary>
public class FingerprintValidationReport
{
    public int Id { get; set; }
    
    // 关联的指纹配置
    public int FingerprintProfileId { get; set; }
    public FingerprintProfile? FingerprintProfile { get; set; }
    
    // 总体评分 (0-100)
    public int TotalScore { get; set; } = 0;
    
    // 三维度评分
    public int ConsistencyScore { get; set; } = 0;      // 一致性评分 (0-100)
    public int RealisticScore { get; set; } = 0;        // 真实性评分 (0-100)
    public int CloudflareRiskScore { get; set; } = 0;   // Cloudflare风险评分 (0-100，越低越好)
    
    // 评分公式: (一致性 + 真实性 + (100 - 风险)) / 3
    public string ScoringFormula { get; set; } = "(ConsistencyScore + RealisticScore + (100 - CloudflareRiskScore)) / 3";
    
    // 风险等级: safe | low | medium | high | critical
    public string RiskLevel { get; set; } = "medium";
    
    // 检查结果 (JSON数组)
    public string? CheckResultsJson { get; set; }
    
    // 建议 (JSON数组)
    public string? RecommendationsJson { get; set; }
    
    // 校验详情
    public string? Details { get; set; }
    
    // 校验时间
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
    
    // 校验版本 (用于追踪校验规则版本)
    public string? ValidationVersion { get; set; }
    
    // 元数据
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
