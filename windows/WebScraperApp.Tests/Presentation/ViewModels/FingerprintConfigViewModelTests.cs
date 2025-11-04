using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;
using FishBrowser.WPF.Application.DTOs;
using FishBrowser.WPF.Application.Services;
using FishBrowser.WPF.Presentation.ViewModels;
using FishBrowser.WPF.Services;
using FishBrowser.WPF.Tests.TestFixtures;

namespace FishBrowser.WPF.Tests.Presentation.ViewModels;

/// <summary>
/// 指纹配置 ViewModel 单元测试
/// </summary>
public class FingerprintConfigViewModelTests
{
    private readonly Mock<FingerprintApplicationService> _mockFingerprintService;
    private readonly Mock<ILogService> _mockLogService;
    private readonly FingerprintConfigViewModel _viewModel;

    public FingerprintConfigViewModelTests()
    {
        var mockDatabaseService = new Mock<IDatabaseService>();
        var mockLogService1 = new Mock<ILogService>();
        var mockPresetService = new Mock<IFingerprintPresetService>();
        
        _mockFingerprintService = new Mock<FingerprintApplicationService>(
            mockDatabaseService.Object,
            mockLogService1.Object,
            mockPresetService.Object
        );
        _mockLogService = new Mock<ILogService>();
        _viewModel = new FingerprintConfigViewModel(_mockFingerprintService.Object, _mockLogService.Object);
    }

    [Fact]
    public void ViewModel_ShouldInitializeWithEmptyFingerprints()
    {
        // Arrange & Act
        var fingerprints = _viewModel.Fingerprints;

        // Assert
        Assert.NotNull(fingerprints);
        Assert.Empty(fingerprints);
    }

    [Fact]
    public void ViewModel_ShouldInitializeWithLoadCommand()
    {
        // Arrange & Act
        var loadCommand = _viewModel.LoadCommand;

        // Assert
        Assert.NotNull(loadCommand);
        Assert.True(loadCommand.CanExecute(null));
    }

    [Fact]
    public void ViewModel_ShouldInitializeWithCreateCommand()
    {
        // Arrange & Act
        var createCommand = _viewModel.CreateCommand;

        // Assert
        Assert.NotNull(createCommand);
        Assert.True(createCommand.CanExecute(null));
    }

    [Fact]
    public void ViewModel_SaveCommandShouldBeDisabledWhenNoEditingFingerprint()
    {
        // Arrange
        _viewModel.EditingFingerprint = null;

        // Act
        var canExecute = _viewModel.SaveCommand.CanExecute(null);

        // Assert
        Assert.False(canExecute);
    }

    [Fact]
    public void ViewModel_SaveCommandShouldBeEnabledWhenEditingFingerprint()
    {
        // Arrange
        _viewModel.EditingFingerprint = new FingerprintDTO { Name = "Test" };

        // Act
        var canExecute = _viewModel.SaveCommand.CanExecute(null);

        // Assert
        Assert.True(canExecute);
    }

    [Fact]
    public void ViewModel_DeleteCommandShouldBeDisabledWhenNoSelectedFingerprint()
    {
        // Arrange
        _viewModel.SelectedFingerprint = null;

        // Act
        var canExecute = _viewModel.DeleteCommand.CanExecute(null);

        // Assert
        Assert.False(canExecute);
    }

    [Fact]
    public void ViewModel_DeleteCommandShouldBeEnabledWhenSelectedFingerprint()
    {
        // Arrange
        _viewModel.SelectedFingerprint = new FingerprintDTO { Id = 1, Name = "Test" };

        // Act
        var canExecute = _viewModel.DeleteCommand.CanExecute(null);

        // Assert
        Assert.True(canExecute);
    }

    [Fact]
    public void ViewModel_CreateNewFingerprint_ShouldSetEditingFingerprint()
    {
        // Arrange
        _viewModel.EditingFingerprint = null;

        // Act
        _viewModel.CreateCommand.Execute(null);

        // Assert
        Assert.NotNull(_viewModel.EditingFingerprint);
        Assert.Equal("新指纹配置", _viewModel.EditingFingerprint.Name);
        Assert.Equal("Mozilla/5.0", _viewModel.EditingFingerprint.UserAgent);
    }

    [Fact]
    public void ViewModel_PropertyChanged_ShouldNotifyOnSelectedFingerprintChange()
    {
        // Arrange
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(FingerprintConfigViewModel.SelectedFingerprint))
                propertyChangedRaised = true;
        };

        // Act
        _viewModel.SelectedFingerprint = new FingerprintDTO { Id = 1, Name = "Test" };

        // Assert
        Assert.True(propertyChangedRaised);
    }

    [Fact]
    public void ViewModel_StatusMessage_ShouldUpdateOnSelectedFingerprintChange()
    {
        // Arrange
        var fingerprint = new FingerprintDTO { Id = 1, Name = "Test Fingerprint" };

        // Act
        _viewModel.SelectedFingerprint = fingerprint;

        // Assert
        Assert.Contains("Test Fingerprint", _viewModel.StatusMessage ?? "");
    }

    [Fact]
    public void ViewModel_CancelCommand_ShouldClearEditingFingerprint()
    {
        // Arrange
        _viewModel.EditingFingerprint = new FingerprintDTO { Name = "Test" };

        // Act
        _viewModel.CancelCommand.Execute(null);

        // Assert
        Assert.Null(_viewModel.EditingFingerprint);
    }

    [Fact]
    public void ViewModel_ImportPresetsCommand_ShouldBeAvailable()
    {
        // Arrange & Act
        var importCommand = _viewModel.ImportPresetsCommand;

        // Assert
        Assert.NotNull(importCommand);
        Assert.True(importCommand.CanExecute(null));
    }
}
