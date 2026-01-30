using System.Diagnostics;
using BeamQualityAnalyzer.ApiClient;
using BeamQualityAnalyzer.Contracts.Dtos;
using BeamQualityAnalyzer.Contracts.Messages;
using BeamQualityAnalyzer.WpfClient.Models;
using BeamQualityAnalyzer.WpfClient.Services;
using BeamQualityAnalyzer.WpfClient.ViewModels;
using FsCheck;
using FsCheck.Xunit;
using Moq;

namespace BeamQualityAnalyzer.WpfClient.Tests;

/// <summary>
/// Property-based tests for MainViewModel.
/// 
/// Property 1: Data acquisition trigger state update - Clicking start should initiate acquisition
/// Property 2: Emergency stop immediately stops operations - Emergency stop should immediately stop
/// Property 19: Button response time - 100ms response to clicks
/// Validates Requirements: 2.1, 2.2, 17.2
/// </summary>
public class MainViewModelPropertyTests
{
    /// <summary>
    /// Helper to create a mock API client with event support
    /// </summary>
    private Mock<IBeamAnalyzerApiClient> CreateMockApiClient(bool isConnected = true)
    {
        var mock = new Mock<IBeamAnalyzerApiClient>();
        mock.SetupGet(x => x.IsConnected).Returns(isConnected);
        return mock;
    }

    /// <summary>
    /// Helper to create a mock Settings Service
    /// </summary>
    private Mock<ISettingsService> CreateMockSettingsService()
    {
        var mock = new Mock<ISettingsService>();
        mock.Setup(x => x.LoadSettingsAsync())
            .ReturnsAsync(new AppSettings());
        return mock;
    }

    /// <summary>
    /// Property 1: Data acquisition trigger should update state
    /// Feature: beam-quality-analyzer, Property 1: 数据采集触发状态更�?
    /// </summary>
    [Fact]
    public async Task StartAcquisition_ShouldUpdateStateToAcquiring()
    {
        // Arrange
        var mockApiClient = CreateMockApiClient(isConnected: true);
        mockApiClient
            .Setup(x => x.StartAcquisitionAsync())
            .ReturnsAsync(CommandResult.SuccessResult("采集已启动"));
        
        var viewModel = new MainViewModel(mockApiClient.Object, CreateMockSettingsService().Object);
        
        // Simulate connection
        mockApiClient.Raise(
            x => x.ConnectionStateChanged += null,
            mockApiClient.Object,
            new ConnectionStateChangedEventArgs(true));
        
        var initialStatus = viewModel.CurrentStatus;
        var initialIsAcquiring = viewModel.IsAcquiring;
        
        // Act
        await viewModel.StartAcquisitionCommand.ExecuteAsync(null);
        
        // Simulate acquisition status change from server
        mockApiClient.Raise(
            x => x.AcquisitionStatusChanged += null,
            mockApiClient.Object,
            new AcquisitionStatusMessage
            {
                IsAcquiring = true,
                DataPointCount = 0,
                Frequency = 10.0,
                Timestamp = DateTime.Now
            });
        
        // Assert
        Assert.True(viewModel.IsAcquiring, "IsAcquiring should be true after starting acquisition");
        Assert.Equal("采集中", viewModel.CurrentStatus);
        Assert.False(initialIsAcquiring, "Initial IsAcquiring should be false");
        
        // Verify API was called
        mockApiClient.Verify(x => x.StartAcquisitionAsync(), Times.Once);
    }

    /// <summary>
    /// Property 1: Start acquisition command should be disabled when already acquiring
    /// </summary>
    [Fact]
    public void StartAcquisitionCommand_ShouldBeDisabled_WhenAlreadyAcquiring()
    {
        // Arrange
        var mockApiClient = CreateMockApiClient(isConnected: true);
        var viewModel = new MainViewModel(mockApiClient.Object, CreateMockSettingsService().Object);
        
        // Simulate connection
        mockApiClient.Raise(
            x => x.ConnectionStateChanged += null,
            mockApiClient.Object,
            new ConnectionStateChangedEventArgs(true));
        
        // Act - Simulate acquisition started
        mockApiClient.Raise(
            x => x.AcquisitionStatusChanged += null,
            mockApiClient.Object,
            new AcquisitionStatusMessage
            {
                IsAcquiring = true,
                DataPointCount = 0,
                Frequency = 10.0,
                Timestamp = DateTime.Now
            });
        
        // Assert
        Assert.False(viewModel.StartAcquisitionCommand.CanExecute(null),
            "StartAcquisitionCommand should be disabled when already acquiring");
        Assert.True(viewModel.EmergencyStopCommand.CanExecute(null),
            "EmergencyStopCommand should be enabled when acquiring");
    }

    /// <summary>
    /// Property 2: Emergency stop should immediately stop operations
    /// Feature: beam-quality-analyzer, Property 2: 急停立即停止操作
    /// </summary>
    [Fact]
    public async Task EmergencyStop_ShouldImmediatelyStopOperations()
    {
        // Arrange
        var mockApiClient = CreateMockApiClient(isConnected: true);
        mockApiClient
            .Setup(x => x.EmergencyStopAsync())
            .ReturnsAsync(CommandResult.SuccessResult("急停执行成功"));
        
        var viewModel = new MainViewModel(mockApiClient.Object, CreateMockSettingsService().Object);
        
        // Simulate connection and acquisition
        mockApiClient.Raise(
            x => x.ConnectionStateChanged += null,
            mockApiClient.Object,
            new ConnectionStateChangedEventArgs(true));
        
        mockApiClient.Raise(
            x => x.AcquisitionStatusChanged += null,
            mockApiClient.Object,
            new AcquisitionStatusMessage
            {
                IsAcquiring = true,
                DataPointCount = 10,
                Frequency = 10.0,
                Timestamp = DateTime.Now
            });
        
        Assert.True(viewModel.IsAcquiring, "Should be acquiring before emergency stop");
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        await viewModel.EmergencyStopCommand.ExecuteAsync(null);
        stopwatch.Stop();
        
        // At this point, status should contain "急停"
        var statusAfterCommand = viewModel.CurrentStatus;
        
        // Simulate acquisition stopped from server
        mockApiClient.Raise(
            x => x.AcquisitionStatusChanged += null,
            mockApiClient.Object,
            new AcquisitionStatusMessage
            {
                IsAcquiring = false,
                DataPointCount = 10,
                Frequency = 0.0,
                Timestamp = DateTime.Now
            });
        
        // Assert
        Assert.False(viewModel.IsAcquiring, "IsAcquiring should be false after emergency stop");
        Assert.Contains("急停", statusAfterCommand, StringComparison.OrdinalIgnoreCase);
        
        // After acquisition status change, status becomes "就绪"
        Assert.Equal("就绪", viewModel.CurrentStatus);
        
        // Verify API was called
        mockApiClient.Verify(x => x.EmergencyStopAsync(), Times.Once);
        
        // Emergency stop should be fast (< 100ms for command execution)
        Assert.True(stopwatch.ElapsedMilliseconds < 100,
            $"Emergency stop should execute quickly, took {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Property 2: Emergency stop command should only be enabled when acquiring
    /// </summary>
    [Fact]
    public void EmergencyStopCommand_ShouldOnlyBeEnabled_WhenAcquiring()
    {
        // Arrange
        var mockApiClient = CreateMockApiClient(isConnected: true);
        var viewModel = new MainViewModel(mockApiClient.Object, CreateMockSettingsService().Object);
        
        // Simulate connection
        mockApiClient.Raise(
            x => x.ConnectionStateChanged += null,
            mockApiClient.Object,
            new ConnectionStateChangedEventArgs(true));
        
        // Assert - Initially not acquiring
        Assert.False(viewModel.EmergencyStopCommand.CanExecute(null),
            "EmergencyStopCommand should be disabled when not acquiring");
        
        // Act - Start acquiring
        mockApiClient.Raise(
            x => x.AcquisitionStatusChanged += null,
            mockApiClient.Object,
            new AcquisitionStatusMessage
            {
                IsAcquiring = true,
                DataPointCount = 0,
                Frequency = 10.0,
                Timestamp = DateTime.Now
            });
        
        // Assert - Now acquiring
        Assert.True(viewModel.EmergencyStopCommand.CanExecute(null),
            "EmergencyStopCommand should be enabled when acquiring");
    }

    /// <summary>
    /// Property 19: Button response time - Commands should respond within 100ms
    /// Feature: beam-quality-analyzer, Property 19: 按钮响应时间
    /// </summary>
    [Theory]
    [InlineData("StartAcquisition")]
    [InlineData("EmergencyStop")]
    [InlineData("ResetMotor")]
    [InlineData("TakeScreenshot")]
    [InlineData("ExportReport")]
    [InlineData("SaveToDatabase")]
    [InlineData("StartAutoTest")]
    public async Task Command_ShouldRespondWithin100ms(string commandName)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient(isConnected: true);
        
        // Setup all API methods to return quickly
        mockApiClient.Setup(x => x.StartAcquisitionAsync())
            .ReturnsAsync(CommandResult.SuccessResult("Success"));
        mockApiClient.Setup(x => x.EmergencyStopAsync())
            .ReturnsAsync(CommandResult.SuccessResult("Success"));
        mockApiClient.Setup(x => x.ResetDeviceAsync())
            .ReturnsAsync(CommandResult.SuccessResult("Success"));
        mockApiClient.Setup(x => x.GenerateScreenshotAsync())
            .ReturnsAsync(CommandResult<string>.SuccessResult("screenshot.png", "Success"));
        mockApiClient.Setup(x => x.GenerateReportAsync(It.IsAny<ReportOptionsDto>()))
            .ReturnsAsync(CommandResult<string>.SuccessResult("report.pdf", "Success"));
        mockApiClient.Setup(x => x.SaveMeasurementAsync(It.IsAny<MeasurementRecordDto>()))
            .ReturnsAsync(CommandResult<int>.SuccessResult(1, "Success"));
        mockApiClient.Setup(x => x.StartAutoTestAsync(It.IsAny<AutoTestConfigurationDto>()))
            .ReturnsAsync(CommandResult.SuccessResult("Success"));
        
        var viewModel = new MainViewModel(mockApiClient.Object, CreateMockSettingsService().Object);
        
        // Simulate connection
        mockApiClient.Raise(
            x => x.ConnectionStateChanged += null,
            mockApiClient.Object,
            new ConnectionStateChangedEventArgs(true));
        
        // For emergency stop, need to be acquiring
        if (commandName == "EmergencyStop")
        {
            mockApiClient.Raise(
                x => x.AcquisitionStatusChanged += null,
                mockApiClient.Object,
                new AcquisitionStatusMessage
                {
                    IsAcquiring = true,
                    DataPointCount = 0,
                    Frequency = 10.0,
                    Timestamp = DateTime.Now
                });
        }
        
        // Act - Measure command execution time
        var stopwatch = Stopwatch.StartNew();
        
        switch (commandName)
        {
            case "StartAcquisition":
                await viewModel.StartAcquisitionCommand.ExecuteAsync(null);
                break;
            case "EmergencyStop":
                await viewModel.EmergencyStopCommand.ExecuteAsync(null);
                break;
            case "ResetMotor":
                await viewModel.ResetMotorCommand.ExecuteAsync(null);
                break;
            case "TakeScreenshot":
                await viewModel.TakeScreenshotCommand.ExecuteAsync(null);
                break;
            case "ExportReport":
                await viewModel.ExportReportCommand.ExecuteAsync(null);
                break;
            case "SaveToDatabase":
                await viewModel.SaveToDatabaseCommand.ExecuteAsync(null);
                break;
            case "StartAutoTest":
                await viewModel.StartAutoTestCommand.ExecuteAsync(null);
                break;
        }
        
        stopwatch.Stop();
        
        // Assert - Command should respond within 100ms
        Assert.True(stopwatch.ElapsedMilliseconds < 100,
            $"{commandName} command should respond within 100ms, took {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Property 19: OpenSettings command should respond immediately (synchronous)
    /// </summary>
    [Fact]
    public void OpenSettingsCommand_ShouldRespondImmediately()
    {
        // Arrange
        var mockApiClient = CreateMockApiClient(isConnected: true);
        var viewModel = new MainViewModel(mockApiClient.Object, CreateMockSettingsService().Object);
        
        // Act - Measure command execution time
        var stopwatch = Stopwatch.StartNew();
        viewModel.OpenSettingsCommand.Execute(null);
        stopwatch.Stop();
        
        // Assert - Synchronous command should be nearly instant
        Assert.True(stopwatch.ElapsedMilliseconds < 10,
            $"OpenSettings command should respond immediately, took {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Property 1 & 2: State transitions should be consistent
    /// </summary>
    [Fact]
    public async Task StateTransitions_ShouldBeConsistent()
    {
        // Arrange
        var mockApiClient = CreateMockApiClient(isConnected: true);
        mockApiClient.Setup(x => x.StartAcquisitionAsync())
            .ReturnsAsync(CommandResult.SuccessResult("Success"));
        mockApiClient.Setup(x => x.EmergencyStopAsync())
            .ReturnsAsync(CommandResult.SuccessResult("Success"));
        
        var viewModel = new MainViewModel(mockApiClient.Object, CreateMockSettingsService().Object);
        
        // Simulate connection
        mockApiClient.Raise(
            x => x.ConnectionStateChanged += null,
            mockApiClient.Object,
            new ConnectionStateChangedEventArgs(true));
        
        // Act & Assert - Test state transitions
        
        // 1. Initial state: not acquiring
        Assert.False(viewModel.IsAcquiring);
        Assert.True(viewModel.StartAcquisitionCommand.CanExecute(null));
        Assert.False(viewModel.EmergencyStopCommand.CanExecute(null));
        
        // 2. Start acquisition
        await viewModel.StartAcquisitionCommand.ExecuteAsync(null);
        mockApiClient.Raise(
            x => x.AcquisitionStatusChanged += null,
            mockApiClient.Object,
            new AcquisitionStatusMessage { IsAcquiring = true, DataPointCount = 0, Frequency = 10.0, Timestamp = DateTime.Now });
        
        Assert.True(viewModel.IsAcquiring);
        Assert.False(viewModel.StartAcquisitionCommand.CanExecute(null));
        Assert.True(viewModel.EmergencyStopCommand.CanExecute(null));
        
        // 3. Emergency stop
        await viewModel.EmergencyStopCommand.ExecuteAsync(null);
        mockApiClient.Raise(
            x => x.AcquisitionStatusChanged += null,
            mockApiClient.Object,
            new AcquisitionStatusMessage { IsAcquiring = false, DataPointCount = 10, Frequency = 0.0, Timestamp = DateTime.Now });
        
        Assert.False(viewModel.IsAcquiring);
        Assert.True(viewModel.StartAcquisitionCommand.CanExecute(null));
        Assert.False(viewModel.EmergencyStopCommand.CanExecute(null));
    }

    /// <summary>
    /// Commands should be disabled when not connected
    /// </summary>
    [Fact]
    public void Commands_ShouldBeDisabled_WhenNotConnected()
    {
        // Arrange
        var mockApiClient = CreateMockApiClient(isConnected: false);
        var viewModel = new MainViewModel(mockApiClient.Object, CreateMockSettingsService().Object);
        
        // Act - Simulate disconnected state
        mockApiClient.Raise(
            x => x.ConnectionStateChanged += null,
            mockApiClient.Object,
            new ConnectionStateChangedEventArgs(false));
        
        // Assert - All commands should be disabled
        Assert.False(viewModel.StartAcquisitionCommand.CanExecute(null),
            "StartAcquisitionCommand should be disabled when not connected");
        Assert.False(viewModel.EmergencyStopCommand.CanExecute(null),
            "EmergencyStopCommand should be disabled when not connected");
        Assert.False(viewModel.ResetMotorCommand.CanExecute(null),
            "ResetMotorCommand should be disabled when not connected");
        Assert.False(viewModel.TakeScreenshotCommand.CanExecute(null),
            "TakeScreenshotCommand should be disabled when not connected");
        Assert.False(viewModel.ExportReportCommand.CanExecute(null),
            "ExportReportCommand should be disabled when not connected");
        Assert.False(viewModel.SaveToDatabaseCommand.CanExecute(null),
            "SaveToDatabaseCommand should be disabled when not connected");
        Assert.False(viewModel.StartAutoTestCommand.CanExecute(null),
            "StartAutoTestCommand should be disabled when not connected");
        
        // OpenSettings should always be enabled
        Assert.True(viewModel.OpenSettingsCommand.CanExecute(null),
            "OpenSettingsCommand should always be enabled");
    }

    /// <summary>
    /// Connection state changes should update command availability
    /// </summary>
    [Fact]
    public void ConnectionStateChange_ShouldUpdateCommandAvailability()
    {
        // Arrange
        var mockApiClient = CreateMockApiClient(isConnected: false);
        var viewModel = new MainViewModel(mockApiClient.Object, CreateMockSettingsService().Object);
        
        // Initially disconnected
        Assert.False(viewModel.IsConnected);
        Assert.False(viewModel.StartAcquisitionCommand.CanExecute(null));
        
        // Act - Connect
        mockApiClient.Raise(
            x => x.ConnectionStateChanged += null,
            mockApiClient.Object,
            new ConnectionStateChangedEventArgs(true));
        
        // Assert - Commands should be enabled
        Assert.True(viewModel.IsConnected);
        Assert.True(viewModel.StartAcquisitionCommand.CanExecute(null));
        Assert.True(viewModel.ResetMotorCommand.CanExecute(null));
        
        // Act - Disconnect
        mockApiClient.Raise(
            x => x.ConnectionStateChanged += null,
            mockApiClient.Object,
            new ConnectionStateChangedEventArgs(false));
        
        // Assert - Commands should be disabled again
        Assert.False(viewModel.IsConnected);
        Assert.False(viewModel.StartAcquisitionCommand.CanExecute(null));
        Assert.False(viewModel.ResetMotorCommand.CanExecute(null));
    }

    /// <summary>
    /// CurrentStatus should update for all operations
    /// </summary>
    [Fact]
    public async Task CurrentStatus_ShouldUpdateForAllOperations()
    {
        // Arrange
        var mockApiClient = CreateMockApiClient(isConnected: true);
        mockApiClient.Setup(x => x.ResetDeviceAsync())
            .ReturnsAsync(CommandResult.SuccessResult("设备复位成功"));
        
        var viewModel = new MainViewModel(mockApiClient.Object, CreateMockSettingsService().Object);
        
        // Simulate connection
        mockApiClient.Raise(
            x => x.ConnectionStateChanged += null,
            mockApiClient.Object,
            new ConnectionStateChangedEventArgs(true));
        
        var initialStatus = viewModel.CurrentStatus;
        
        // Act
        await viewModel.ResetMotorCommand.ExecuteAsync(null);
        
        // Assert
        Assert.NotEqual(initialStatus, viewModel.CurrentStatus);
        Assert.Contains("复位", viewModel.CurrentStatus);
    }

    /// <summary>
    /// ViewModel should handle API errors gracefully
    /// </summary>
    [Fact]
    public async Task Commands_ShouldHandleApiErrors_Gracefully()
    {
        // Arrange
        var mockApiClient = CreateMockApiClient(isConnected: true);
        mockApiClient.Setup(x => x.StartAcquisitionAsync())
            .ReturnsAsync(CommandResult.FailureResult("设备连接失败"));
        
        var viewModel = new MainViewModel(mockApiClient.Object, CreateMockSettingsService().Object);
        
        // Simulate connection
        mockApiClient.Raise(
            x => x.ConnectionStateChanged += null,
            mockApiClient.Object,
            new ConnectionStateChangedEventArgs(true));
        
        // Act
        await viewModel.StartAcquisitionCommand.ExecuteAsync(null);
        
        // Assert - Should not throw, should update status with error
        Assert.Contains("失败", viewModel.CurrentStatus);
        Assert.False(viewModel.IsAcquiring);
    }

    /// <summary>
    /// ViewModel should handle API exceptions gracefully
    /// </summary>
    [Fact]
    public async Task Commands_ShouldHandleApiExceptions_Gracefully()
    {
        // Arrange
        var mockApiClient = CreateMockApiClient(isConnected: true);
        mockApiClient.Setup(x => x.StartAcquisitionAsync())
            .ThrowsAsync(new InvalidOperationException("网络错误"));
        
        var viewModel = new MainViewModel(mockApiClient.Object, CreateMockSettingsService().Object);
        
        // Simulate connection
        mockApiClient.Raise(
            x => x.ConnectionStateChanged += null,
            mockApiClient.Object,
            new ConnectionStateChangedEventArgs(true));
        
        // Act
        await viewModel.StartAcquisitionCommand.ExecuteAsync(null);
        
        // Assert - Should not throw, should update status with exception message
        Assert.Contains("异常", viewModel.CurrentStatus);
        Assert.False(viewModel.IsAcquiring);
    }
}
