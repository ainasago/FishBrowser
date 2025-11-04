using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;
using FishBrowser.WPF.Application.DTOs;
using FishBrowser.WPF.Application.Services;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;
using FishBrowser.WPF.Tests.TestFixtures;

namespace FishBrowser.WPF.Tests.Application.Services;

/// <summary>
/// 指纹配置应用服务单元测试
/// </summary>
public class FingerprintApplicationServiceTests
{
    private readonly Mock<IDatabaseService> _mockDatabaseService;
    private readonly Mock<ILogService> _mockLogService;
    private readonly Mock<IFingerprintPresetService> _mockPresetService;
    private readonly FingerprintApplicationService _service;

    public FingerprintApplicationServiceTests()
    {
        // 创建 Mock 对象
        _mockDatabaseService = new Mock<IDatabaseService>();
        _mockLogService = new Mock<ILogService>();
        _mockPresetService = new Mock<IFingerprintPresetService>();
        
        // 创建应用服务实例
        _service = new FingerprintApplicationService(
            _mockDatabaseService.Object,
            _mockLogService.Object,
            _mockPresetService.Object
        );
    }

    [Fact]
    public void GetAllFingerprints_ShouldReturnEmptyListWhenNoneExist()
    {
        // Arrange
        _mockDatabaseService
            .Setup(x => x.GetAllFingerprintProfiles())
            .Returns(new List<FingerprintProfile>());

        // Act
        var result = _service.GetAllFingerprints();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetAllFingerprints_ShouldReturnAllFingerprints()
    {
        // Arrange
        var fingerprints = new List<FingerprintProfile>
        {
            new FingerprintProfile { Id = 1, Name = "Test 1", UserAgent = "Mozilla/5.0" },
            new FingerprintProfile { Id = 2, Name = "Test 2", UserAgent = "Mozilla/5.1" }
        };
        _mockDatabaseService
            .Setup(x => x.GetAllFingerprintProfiles())
            .Returns(fingerprints);

        // Act
        var result = _service.GetAllFingerprints();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Test 1", result[0].Name);
        Assert.Equal("Test 2", result[1].Name);
    }

    [Fact]
    public void GetFingerprintById_ShouldReturnFingerprintWhenExists()
    {
        // Arrange
        var fingerprint = new FingerprintProfile { Id = 1, Name = "Test", UserAgent = "Mozilla/5.0" };
        _mockDatabaseService
            .Setup(x => x.GetFingerprintProfile(1))
            .Returns(fingerprint);

        // Act
        var result = _service.GetFingerprintById(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public void GetFingerprintById_ShouldReturnNullWhenNotExists()
    {
        // Arrange
        _mockDatabaseService
            .Setup(x => x.GetFingerprintProfile(It.IsAny<int>()))
            .Returns((FingerprintProfile?)null);

        // Act
        var result = _service.GetFingerprintById(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CreateFingerprint_ShouldThrowExceptionWhenNameIsEmpty()
    {
        // Arrange
        var dto = new FingerprintDTO { Name = "", UserAgent = "Mozilla/5.0" };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.CreateFingerprint(dto));
    }

    [Fact]
    public void CreateFingerprint_ShouldThrowExceptionWhenUserAgentIsEmpty()
    {
        // Arrange
        var dto = new FingerprintDTO { Name = "Test", UserAgent = "" };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.CreateFingerprint(dto));
    }

    [Fact]
    public void CreateFingerprint_ShouldThrowExceptionWhenViewportInvalid()
    {
        // Arrange
        var dto = new FingerprintDTO 
        { 
            Name = "Test", 
            UserAgent = "Mozilla/5.0",
            ViewportWidth = 0,
            ViewportHeight = 1080
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.CreateFingerprint(dto));
    }

    [Fact]
    public void CreateFingerprint_ShouldCreateSuccessfully()
    {
        // Arrange
        var dto = new FingerprintDTO 
        { 
            Name = "Test", 
            UserAgent = "Mozilla/5.0",
            ViewportWidth = 1920,
            ViewportHeight = 1080,
            Timezone = "Asia/Shanghai",
            Locale = "zh-CN"
        };
        var createdProfile = new FingerprintProfile 
        { 
            Id = 1,
            Name = dto.Name,
            UserAgent = dto.UserAgent
        };
        _mockDatabaseService
            .Setup(x => x.GetAllFingerprintProfiles())
            .Returns(new List<FingerprintProfile> { createdProfile });

        // Act
        var result = _service.CreateFingerprint(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public void DeleteFingerprint_ShouldThrowExceptionWhenNotExists()
    {
        // Arrange
        _mockDatabaseService
            .Setup(x => x.GetFingerprintProfile(It.IsAny<int>()))
            .Returns((FingerprintProfile?)null);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _service.DeleteFingerprint(999));
    }

    [Fact]
    public void DeleteFingerprint_ShouldDeleteSuccessfully()
    {
        // Arrange
        var fingerprint = new FingerprintProfile { Id = 1, Name = "Test", UserAgent = "Mozilla/5.0" };
        _mockDatabaseService
            .Setup(x => x.GetFingerprintProfile(1))
            .Returns(fingerprint);

        // Act
        _service.DeleteFingerprint(1);

        // Assert
        _mockDatabaseService.Verify(x => x.DeleteFingerprintProfile(1), Times.Once);
    }

    [Fact]
    public void GetAllPresets_ShouldReturnPresets()
    {
        // Arrange
        var presets = new List<FingerprintProfile>
        {
            new FingerprintProfile { Id = 1, Name = "Preset 1", UserAgent = "Mozilla/5.0", IsPreset = true }
        };
        _mockPresetService
            .Setup(x => x.GetAllPresets())
            .Returns(presets);

        // Act
        var result = _service.GetAllPresets();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Preset 1", result[0].Name);
    }
}
