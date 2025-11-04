namespace FishBrowser.WPF.Models;

/// <summary>
/// 测试运行选项
/// </summary>
public class TestRunOptions
{
    /// <summary>
    /// 是否使用随机指纹（默认true）
    /// </summary>
    public bool UseRandomFingerprint { get; set; } = true;

    /// <summary>
    /// 指定指纹配置ID（UseRandomFingerprint=false时使用）
    /// </summary>
    public int? FingerprintProfileId { get; set; }

    /// <summary>
    /// 是否无头模式（默认false，显示浏览器）
    /// </summary>
    public bool Headless { get; set; } = false;

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// 是否保存截图
    /// </summary>
    public bool SaveScreenshots { get; set; } = true;

    /// <summary>
    /// 测试后是否清理临时资源
    /// </summary>
    public bool CleanupAfterTest { get; set; } = true;
}
