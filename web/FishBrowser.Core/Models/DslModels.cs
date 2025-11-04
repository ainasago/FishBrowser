using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace FishBrowser.WPF.Models;

/// <summary>
/// DSL 流程
/// </summary>
public class DslFlow
{
    public string DslVersion { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<DslStep>? Steps { get; set; }
}

/// <summary>
/// DSL 步骤（Phase 1 简化版）
/// </summary>
public class DslStep
{
    // 通用形态：- step: open|click|type|fill ...
    public string? Step { get; set; }

    // 兼容 AI 输出：允许使用 "type:" 表示动作种类
    [YamlMember(Alias = "type")]
    public string? Kind { get; set; }

    public string? Url { get; set; }
    public DslSelector? Selector { get; set; }
    public string? Value { get; set; }

    public DslOpenAction? Open { get; set; }
    public DslClickAction? Click { get; set; }
    public DslFillAction? Fill { get; set; }
    public DslTypeAction? TypeAction { get; set; }
    public DslWaitForAction? WaitFor { get; set; }
    public DslWaitNetworkIdleAction? WaitNetworkIdle { get; set; }
    public DslScreenshotAction? Screenshot { get; set; }
    public DslLogAction? Log { get; set; }
    public DslSleepAction? Sleep { get; set; }
}

// 步骤动作类型
public class DslOpenAction 
{ 
    public string Url { get; set; } = string.Empty; 
}

public class DslClickAction 
{ 
    public DslSelector? Selector { get; set; } 
}

public class DslFillAction 
{ 
    public DslSelector? Selector { get; set; }
    public string Value { get; set; } = string.Empty;
}

public class DslTypeAction 
{ 
    public DslSelector? Selector { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class DslWaitForAction 
{ 
    public DslSelector? Selector { get; set; } 
}

public class DslWaitNetworkIdleAction { }

public class DslScreenshotAction 
{ 
    public string? File { get; set; } 
}

public class DslLogAction 
{ 
    public string? Level { get; set; }
    public string? Message { get; set; } 
}

public class DslSleepAction 
{ 
    public int Ms { get; set; } 
}

public class DslSelector
{
    public string Type { get; set; } = "css"; // css|xpath|text|role
    public string Value { get; set; } = string.Empty;
}
