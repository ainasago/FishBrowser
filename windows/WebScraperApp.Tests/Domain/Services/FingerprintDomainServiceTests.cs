using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;
using FishBrowser.WPF.Domain.Repositories;
using FishBrowser.WPF.Domain.Services;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;
using FishBrowser.WPF.Tests.TestFixtures;

namespace FishBrowser.WPF.Tests.Domain.Services;

/// <summary>
/// 指纹配置领域服务单元测试
/// </summary>
public class FingerprintDomainServiceTests
{
    private readonly Mock<IFingerprintRepository> _mockRepository;
    private readonly Mock<ILogService> _mockLogService;
    private readonly FingerprintDomainService _service;

    public FingerprintDomainServiceTests()
    {
        // 创建 Mock 对象
        _mockRepository = new Mock<IFingerprintRepository>();
        _mockLogService = new Mock<ILogService>();
        
        // 创建领域服务实例
        _service = new FingerprintDomainService(_mockRepository.Object, _mockLogService.Object);
    }

    [Fact]
    public async Task CreateFingerprintAsync_ShouldThrowExceptionWhenNameIsEmpty()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateFingerprintAsync("", "Mozilla/5.0", "zh-CN", "Asia/Shanghai")
        );
    }

    [Fact]
    public async Task CreateFingerprintAsync_ShouldThrowExceptionWhenUserAgentIsEmpty()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateFingerprintAsync("Test", "", "zh-CN", "Asia/Shanghai")
        );
    }

    [Fact]
    public async Task CreateFingerprintAsync_ShouldThrowExceptionWhenNameAlreadyExists()
    {
        // Arrange
        _mockRepository
            .Setup(x => x.NameExistsAsync("Test"))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateFingerprintAsync("Test", "Mozilla/5.0", "zh-CN", "Asia/Shanghai")
        );
    }

    [Fact]
    public async Task CreateFingerprintAsync_ShouldCreateSuccessfully()
    {
        // Arrange
        _mockRepository
            .Setup(x => x.NameExistsAsync("Test"))
            .ReturnsAsync(false);
        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<FingerprintProfile>()))
            .Returns(Task.CompletedTask);
        _mockRepository
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateFingerprintAsync("Test", "Mozilla/5.0", "zh-CN", "Asia/Shanghai");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal("Mozilla/5.0", result.UserAgent);
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<FingerprintProfile>()), Times.Once);
        _mockRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateFingerprintAsync_ShouldThrowExceptionWhenNotExists()
    {
        // Arrange
        var fingerprint = new FingerprintProfile { Id = 999, Name = "Test", UserAgent = "Mozilla/5.0" };
        _mockRepository
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((FingerprintProfile?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateFingerprintAsync(fingerprint)
        );
    }

    [Fact]
    public async Task UpdateFingerprintAsync_ShouldUpdateSuccessfully()
    {
        // Arrange
        var existing = new FingerprintProfile { Id = 1, Name = "Test", UserAgent = "Mozilla/5.0" };
        var updated = new FingerprintProfile { Id = 1, Name = "Test Updated", UserAgent = "Mozilla/5.1" };
        _mockRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(existing);
        _mockRepository
            .Setup(x => x.UpdateAsync(It.IsAny<FingerprintProfile>()))
            .Returns(Task.CompletedTask);
        _mockRepository
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateFingerprintAsync(updated);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Updated", result.Name);
        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<FingerprintProfile>()), Times.Once);
        _mockRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteFingerprintAsync_ShouldThrowExceptionWhenNotExists()
    {
        // Arrange
        _mockRepository
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((FingerprintProfile?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteFingerprintAsync(999)
        );
    }

    [Fact]
    public async Task DeleteFingerprintAsync_ShouldThrowExceptionWhenPreset()
    {
        // Arrange
        var preset = new FingerprintProfile { Id = 1, Name = "Preset", UserAgent = "Mozilla/5.0", IsPreset = true };
        _mockRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(preset);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteFingerprintAsync(1)
        );
    }

    [Fact]
    public async Task DeleteFingerprintAsync_ShouldDeleteSuccessfully()
    {
        // Arrange
        var fingerprint = new FingerprintProfile { Id = 1, Name = "Test", UserAgent = "Mozilla/5.0", IsPreset = false };
        _mockRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(fingerprint);
        _mockRepository
            .Setup(x => x.DeleteAsync(1))
            .Returns(Task.CompletedTask);
        _mockRepository
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteFingerprintAsync(1);

        // Assert
        _mockRepository.Verify(x => x.DeleteAsync(1), Times.Once);
        _mockRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CloneFingerprintAsync_ShouldThrowExceptionWhenSourceNotExists()
    {
        // Arrange
        _mockRepository
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((FingerprintProfile?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CloneFingerprintAsync(999, "Clone")
        );
    }

    [Fact]
    public async Task CloneFingerprintAsync_ShouldThrowExceptionWhenNameAlreadyExists()
    {
        // Arrange
        var source = new FingerprintProfile { Id = 1, Name = "Test", UserAgent = "Mozilla/5.0" };
        _mockRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(source);
        _mockRepository
            .Setup(x => x.NameExistsAsync("Clone"))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CloneFingerprintAsync(1, "Clone")
        );
    }

    [Fact]
    public async Task CloneFingerprintAsync_ShouldCloneSuccessfully()
    {
        // Arrange
        var source = new FingerprintProfile 
        { 
            Id = 1, 
            Name = "Test", 
            UserAgent = "Mozilla/5.0",
            ViewportWidth = 1920,
            ViewportHeight = 1080
        };
        _mockRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(source);
        _mockRepository
            .Setup(x => x.NameExistsAsync("Clone"))
            .ReturnsAsync(false);
        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<FingerprintProfile>()))
            .Returns(Task.CompletedTask);
        _mockRepository
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CloneFingerprintAsync(1, "Clone");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Clone", result.Name);
        Assert.Equal("Mozilla/5.0", result.UserAgent);
        Assert.Equal(1920, result.ViewportWidth);
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<FingerprintProfile>()), Times.Once);
        _mockRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetPresetsAsync_ShouldReturnPresets()
    {
        // Arrange
        var presets = new List<FingerprintProfile>
        {
            new FingerprintProfile { Id = 1, Name = "Preset 1", UserAgent = "Mozilla/5.0", IsPreset = true }
        };
        _mockRepository
            .Setup(x => x.GetPresetsAsync())
            .ReturnsAsync(presets);

        // Act
        var result = await _service.GetPresetsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }
}
