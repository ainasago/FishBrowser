using Moq;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Services;

namespace FishBrowser.WPF.Tests.TestFixtures;

/// <summary>
/// Mock 工厂 - 用于创建测试用的 Mock 对象
/// </summary>
public static class TestMockFactory
{
    /// <summary>
    /// 创建 DatabaseService 的 Mock
    /// </summary>
    public static Mock<DatabaseService> CreateDatabaseServiceMock()
    {
        var mock = new Mock<DatabaseService>(MockBehavior.Loose);
        return mock;
    }

    /// <summary>
    /// 创建 LogService 的 Mock
    /// </summary>
    public static Mock<LogService> CreateLogServiceMock()
    {
        var mock = new Mock<LogService>(MockBehavior.Loose);
        return mock;
    }

    /// <summary>
    /// 创建 FingerprintPresetService 的 Mock
    /// </summary>
    public static Mock<FingerprintPresetService> CreateFingerprintPresetServiceMock()
    {
        var mock = new Mock<FingerprintPresetService>(MockBehavior.Loose);
        return mock;
    }

    /// <summary>
    /// 创建 DbContext 的 Mock
    /// </summary>
    public static Mock<WebScraperDbContext> CreateDbContextMock()
    {
        var mock = new Mock<WebScraperDbContext>(MockBehavior.Loose);
        return mock;
    }
}
