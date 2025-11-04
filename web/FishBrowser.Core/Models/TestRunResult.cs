using System;
using System.Collections.Generic;

namespace FishBrowser.WPF.Models;

/// <summary>
/// 测试运行结果
/// </summary>
public class TestRunResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 运行时长
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// 总步骤数
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// 已执行步骤数
    /// </summary>
    public int StepsExecuted { get; set; }

    /// <summary>
    /// 步骤结果列表
    /// </summary>
    public List<StepResult> StepResults { get; set; } = new();

    /// <summary>
    /// 截图文件路径列表
    /// </summary>
    public List<string> Screenshots { get; set; } = new();

    /// <summary>
    /// 提取的数据
    /// </summary>
    public Dictionary<string, object> ExtractedData { get; set; } = new();
}

/// <summary>
/// 步骤执行结果
/// </summary>
public class StepResult
{
    /// <summary>
    /// 步骤索引
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// 步骤名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 执行时长
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// 截图路径
    /// </summary>
    public string? ScreenshotPath { get; set; }
}
