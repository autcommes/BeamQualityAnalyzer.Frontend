using BeamQualityAnalyzer.ApiClient;
using BeamQualityAnalyzer.Contracts.Dtos;
using BeamQualityAnalyzer.Contracts.Messages;
using BeamQualityAnalyzer.WpfClient.ViewModels;
using FsCheck;
using FsCheck.Xunit;
using Moq;

namespace BeamQualityAnalyzer.WpfClient.Tests;

/// <summary>
/// Property-based tests for StatusBarViewModel.
/// 
/// Property 17: Status bar operation feedback - Any operation should display feedback
/// Validates Requirement: 14.3
/// </summary>
public class StatusBarViewModelPropertyTests
{
    /// <summary>
    /// Helper to create a mock API client with event support
    /// </summary>
    private Mock<IBeamAnalyzerApiClient> CreateMockApiClient()
    {
        var mock = new Mock<IBeamAnalyzerApiClient>();
        mock.SetupGet(x => x.IsConnected).Returns(true);
        return mock;
    }

    /// <summary>
    /// Property 17: Connection state changes should update status bar
    /// </summary>
    [Property(MaxTest = 50)]
    public void ConnectionStateChange_ShouldUpdateStatusBar(bool isConnected)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var viewModel = new StatusBarViewModel(mockApiClient.Object);
        
        var initialStatusText = viewModel.StatusText;
        var initialLastOperationTime = viewModel.LastOperationTime;
        
        // Act
        mockApiClient.Raise(
            x => x.ConnectionStateChanged += null,
            mockApiClient.Object,
            new ConnectionStateChangedEventArgs(isConnected));
        
        // Assert
        var statusUpdated = viewModel.StatusText != initialStatusText;
        var timeUpdated = viewModel.LastOperationTime != initialLastOperationTime;
        var hasValidStatus = !string.IsNullOrWhiteSpace(viewModel.StatusText);
        
        Assert.True(statusUpdated && timeUpdated && hasValidStatus,
            $"Connection state change (connected={isConnected}) should update status bar");
    }

    /// <summary>
    /// Property 17: Acquisition status changes should update status bar
    /// </summary>
    [Property(MaxTest = 50)]
    public void AcquisitionStatusChange_ShouldUpdateStatusBar(
        bool isAcquiring,
        PositiveInt dataPointCount)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var viewModel = new StatusBarViewModel(mockApiClient.Object);
        
        var initialStatusText = viewModel.StatusText;
        var frequency = 10.0 + (dataPointCount.Get % 10);
        
        var message = new AcquisitionStatusMessage
        {
            IsAcquiring = isAcquiring,
            DataPointCount = dataPointCount.Get,
            Frequency = frequency,
            Timestamp = DateTime.Now
        };
        
        // Act
        mockApiClient.Raise(
            x => x.AcquisitionStatusChanged += null,
            mockApiClient.Object,
            message);
        
        // Assert
        var statusUpdated = viewModel.StatusText != initialStatusText;
        var hasValidStatus = !string.IsNullOrWhiteSpace(viewModel.StatusText);
        var timeUpdated = viewModel.LastOperationTime.HasValue;
        
        Assert.True(statusUpdated && hasValidStatus && timeUpdated,
            $"Acquisition status change (acquiring={isAcquiring}) should update status bar");
    }

    /// <summary>
    /// Property 17: Device status changes should update status bar
    /// </summary>
    [Property(MaxTest = 50)]
    public void DeviceStatusChange_ShouldUpdateStatusBar(NonEmptyString status)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var viewModel = new StatusBarViewModel(mockApiClient.Object);
        
        var initialStatusText = viewModel.StatusText;
        
        var message = new DeviceStatusMessage
        {
            Status = status.Get,
            Message = "Device status update",
            Timestamp = DateTime.Now
        };
        
        // Act
        mockApiClient.Raise(
            x => x.DeviceStatusChanged += null,
            mockApiClient.Object,
            message);
        
        // Assert
        var statusUpdated = viewModel.StatusText != initialStatusText;
        var hasValidStatus = !string.IsNullOrWhiteSpace(viewModel.StatusText);
        var timeUpdated = viewModel.LastOperationTime.HasValue;
        
        Assert.True(statusUpdated && hasValidStatus && timeUpdated,
            "Device status change should update status bar");
    }

    /// <summary>
    /// Property 17: Error events should update status bar with error level
    /// </summary>
    [Property(MaxTest = 50)]
    public void ErrorOccurred_ShouldUpdateStatusBarWithErrorLevel(NonEmptyString errorMessage)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var viewModel = new StatusBarViewModel(mockApiClient.Object);
        
        var initialStatusText = viewModel.StatusText;
        var initialStatusLevel = viewModel.StatusLevel;
        
        // Test both warning and error levels
        var levels = new[] { "warning", "error" };
        var level = levels[Math.Abs(errorMessage.Get.GetHashCode()) % levels.Length];
        
        var message = new ErrorMessage
        {
            Level = level,
            Message = errorMessage.Get,
            Title = "Error",
            Timestamp = DateTime.Now
        };
        
        // Act
        mockApiClient.Raise(
            x => x.ErrorOccurred += null,
            mockApiClient.Object,
            message);
        
        // Assert
        var statusUpdated = viewModel.StatusText != initialStatusText;
        var levelUpdated = viewModel.StatusLevel != initialStatusLevel;
        var hasValidStatus = !string.IsNullOrWhiteSpace(viewModel.StatusText);
        var timeUpdated = viewModel.LastOperationTime.HasValue;
        
        // Error or warning should set appropriate status level
        var correctLevel = level == "warning" 
            ? viewModel.StatusLevel == StatusLevel.Warning 
            : viewModel.StatusLevel == StatusLevel.Error;
        
        Assert.True(statusUpdated && levelUpdated && hasValidStatus && timeUpdated && correctLevel,
            $"Error event (level={level}) should update status bar with correct level");
    }

    /// <summary>
    /// Property 17: Progress updates should update status bar
    /// </summary>
    [Property(MaxTest = 50)]
    public void ProgressUpdate_ShouldUpdateStatusBar(NonEmptyString operation, PositiveInt percentage)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var viewModel = new StatusBarViewModel(mockApiClient.Object);
        
        var initialStatusText = viewModel.StatusText;
        var initialProgressValue = viewModel.ProgressValue;
        
        var percentValue = percentage.Get % 101; // 0-100
        
        var message = new ProgressMessage
        {
            Operation = operation.Get,
            Percentage = percentValue,
            Message = "Progress update",
            Timestamp = DateTime.Now
        };
        
        // Act
        mockApiClient.Raise(
            x => x.ProgressUpdated += null,
            mockApiClient.Object,
            message);
        
        // Assert
        var statusUpdated = viewModel.StatusText != initialStatusText;
        var progressUpdated = viewModel.ProgressValue != initialProgressValue;
        var hasValidStatus = !string.IsNullOrWhiteSpace(viewModel.StatusText);
        var timeUpdated = viewModel.LastOperationTime.HasValue;
        var progressInRange = viewModel.ProgressValue >= 0 && viewModel.ProgressValue <= 100;
        
        // Progress bar should be visible when percentage is between 0 and 100
        var correctVisibility = percentValue > 0 && percentValue < 100
            ? viewModel.IsProgressVisible
            : !viewModel.IsProgressVisible;
        
        Assert.True(statusUpdated && progressUpdated && hasValidStatus && timeUpdated && 
                progressInRange && correctVisibility,
            $"Progress update ({percentValue}%) should update status bar");
    }

    /// <summary>
    /// Property 17: Calculation completed should update status bar
    /// </summary>
    [Fact]
    public void CalculationCompleted_ShouldUpdateStatusBar()
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var viewModel = new StatusBarViewModel(mockApiClient.Object);
        
        var initialStatusText = viewModel.StatusText;
        
        var message = new CalculationCompletedMessage
        {
            MSquaredX = 1.0,
            MSquaredY = 1.0,
            Timestamp = DateTime.Now
        };
        
        // Act
        mockApiClient.Raise(
            x => x.CalculationCompleted += null,
            mockApiClient.Object,
            message);
        
        // Assert
        var statusUpdated = viewModel.StatusText != initialStatusText;
        var hasValidStatus = !string.IsNullOrWhiteSpace(viewModel.StatusText);
        var timeUpdated = viewModel.LastOperationTime.HasValue;
        var progressHidden = !viewModel.IsProgressVisible;
        
        Assert.True(statusUpdated && hasValidStatus && timeUpdated && progressHidden,
            "Calculation completed should update status bar and hide progress");
    }

    /// <summary>
    /// Property 17: Status color should match status level
    /// </summary>
    [Theory]
    [InlineData("warning", StatusLevel.Warning, "#D7BA7D")]
    [InlineData("error", StatusLevel.Error, "#F44747")]
    [InlineData("unknown", StatusLevel.Error, "#F44747")] // Unknown levels default to Error
    public void StatusColor_ShouldMatchStatusLevel(string errorLevel, StatusLevel expectedLevel, string expectedColor)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var viewModel = new StatusBarViewModel(mockApiClient.Object);
        
        // Act
        mockApiClient.Raise(
            x => x.ErrorOccurred += null,
            mockApiClient.Object,
            new ErrorMessage
            {
                Level = errorLevel,
                Message = "Test message",
                Timestamp = DateTime.Now
            });
        
        // Assert
        Assert.Equal(expectedColor, viewModel.StatusColor);
        Assert.Equal(expectedLevel, viewModel.StatusLevel);
    }

    /// <summary>
    /// Property 17: Multiple operations should all update status bar
    /// </summary>
    [Fact]
    public void MultipleOperations_ShouldAllUpdateStatusBar()
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var viewModel = new StatusBarViewModel(mockApiClient.Object);
        
        var statusUpdates = new List<string>();
        var timeUpdates = new List<DateTime?>();
        
        // Act - Trigger multiple operations
        var operations = new Action[]
        {
            () => mockApiClient.Raise(x => x.ConnectionStateChanged += null, mockApiClient.Object, new ConnectionStateChangedEventArgs(true)),
            () => mockApiClient.Raise(x => x.AcquisitionStatusChanged += null, mockApiClient.Object, new AcquisitionStatusMessage { IsAcquiring = true, DataPointCount = 10, Frequency = 10.0, Timestamp = DateTime.Now }),
            () => mockApiClient.Raise(x => x.DeviceStatusChanged += null, mockApiClient.Object, new DeviceStatusMessage { Status = "Ready", Timestamp = DateTime.Now }),
            () => mockApiClient.Raise(x => x.ErrorOccurred += null, mockApiClient.Object, new ErrorMessage { Level = "error", Message = "Test error", Timestamp = DateTime.Now }),
            () => mockApiClient.Raise(x => x.ProgressUpdated += null, mockApiClient.Object, new ProgressMessage { Operation = "Test", Percentage = 50, Timestamp = DateTime.Now }),
            () => mockApiClient.Raise(x => x.CalculationCompleted += null, mockApiClient.Object, new CalculationCompletedMessage { MSquaredX = 1.0, MSquaredY = 1.0, Timestamp = DateTime.Now })
        };
        
        foreach (var operation in operations)
        {
            operation();
            statusUpdates.Add(viewModel.StatusText);
            timeUpdates.Add(viewModel.LastOperationTime);
        }
        
        // Assert - All operations should have updated the status
        var allStatusValid = statusUpdates.All(s => !string.IsNullOrWhiteSpace(s));
        var allTimesValid = timeUpdates.All(t => t.HasValue);
        var statusesChanged = statusUpdates.Distinct().Count() > 1;
        
        Assert.True(allStatusValid && allTimesValid && statusesChanged,
            $"All {operations.Length} operations should update status bar");
    }
}
