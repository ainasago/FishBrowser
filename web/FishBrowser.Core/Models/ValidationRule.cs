using System;
using System.Collections.Generic;

namespace FishBrowser.WPF.Models;

/// <summary>
/// 指纹校验规则 - 定义校验标准和权重
/// </summary>
public class ValidationRule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // 规则类型: consistency | realism | cloudflare_risk
    public string RuleType { get; set; } = "consistency";
    
    // 规则优先级 (1-10, 10最高)
    public int Priority { get; set; } = 5;
    
    // 权重 (0-100)
    public int Weight { get; set; } = 100;
    
    // 规则是否启用
    public bool IsEnabled { get; set; } = true;
    
    // 规则配置 (JSON格式)
    public string? ConfigJson { get; set; }
    
    // 关联的浏览器分组 (可选，null表示全局规则)
    public int? BrowserGroupId { get; set; }
    public BrowserGroup? BrowserGroup { get; set; }
    
    // 元数据
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
