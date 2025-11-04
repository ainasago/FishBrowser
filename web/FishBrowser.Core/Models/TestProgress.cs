namespace FishBrowser.WPF.Models;

/// <summary>
/// 测试进度信息
/// </summary>
public class TestProgress
{
    /// <summary>
    /// 当前阶段
    /// </summary>
    public TestStage Stage { get; set; }

    /// <summary>
    /// 当前步骤索引
    /// </summary>
    public int CurrentStep { get; set; }

    /// <summary>
    /// 总步骤数
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// 进度消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 日志级别
    /// </summary>
    public LogLevel Level { get; set; } = LogLevel.Info;

    /// <summary>
    /// 截图数据（可选）
    /// </summary>
    public byte[]? Screenshot { get; set; }

    /// <summary>
    /// 步骤名称（可选）
    /// </summary>
    public string? StepName { get; set; }
}

/// <summary>
/// 测试阶段
/// </summary>
public enum TestStage
{
    /// <summary>
    /// 初始化
    /// </summary>
    Initializing,

    /// <summary>
    /// 生成指纹
    /// </summary>
    GeneratingFingerprint,

    /// <summary>
    /// 启动浏览器
    /// </summary>
    StartingBrowser,

    /// <summary>
    /// 执行步骤
    /// </summary>
    ExecutingSteps,

    /// <summary>
    /// 完成
    /// </summary>
    Completed,

    /// <summary>
    /// 失败
    /// </summary>
    Failed,

    /// <summary>
    /// 清理资源
    /// </summary>
    CleaningUp
}

/// <summary>
/// 日志级别
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}
