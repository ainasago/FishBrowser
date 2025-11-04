using System;
using System.Collections.Generic;

namespace FishBrowser.WPF.Models;

/// <summary>
/// 浏览器分组 - 按场景分类管理浏览器环境和指纹配置
/// </summary>
public class BrowserGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; } = "🌐";  // 分组图标
    public int Order { get; set; } = 0;
    
    // 分组默认配置
    public string? DefaultProxyId { get; set; }
    public string? DefaultLocale { get; set; }
    public string? DefaultTimezone { get; set; }
    
    // 校验规则
    public int MinRealisticScore { get; set; } = 70;  // 最小真实性评分
    public int MaxCloudflareRiskScore { get; set; } = 50;  // 最大Cloudflare风险评分
    
    // 元数据
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // 导航属性
    public ICollection<BrowserEnvironment> Environments { get; set; } = new List<BrowserEnvironment>();
    public ICollection<ValidationRule> ValidationRules { get; set; } = new List<ValidationRule>();
}
