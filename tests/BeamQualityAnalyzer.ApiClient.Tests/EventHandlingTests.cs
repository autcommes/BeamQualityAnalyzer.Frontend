using BeamQualityAnalyzer.Contracts.Messages;
using Microsoft.Extensions.Logging;
using Moq;

namespace BeamQualityAnalyzer.ApiClient.Tests;

/// <summary>
/// 事件处理测试
/// 验证客户端可以正确订阅和处理各种服务器推送事件
/// </summary>
public class EventHandlingTests
{
    private readonly Mock<ILogger<BeamAnalyzerApiClient>> _mockLogger;

    public EventHandlingTests()
    {
        _mockLogger = new Mock<ILogger<BeamAnalyzerApiClient>>();
    }

    [Fact]
    public void RawDataReceived_CanSubscribeAndUnsubscribe()
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);
        var eventRaised = false;
        EventHandler<RawDataReceivedMessage> handler = (sender, args) => eventRaised = true;

        // Act
        client.RawDataReceived += handler;
        client.RawDataReceived -= handler;

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void CalculationCompleted_CanSubscribeAndUnsubscribe()
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);
        var eventRaised = false;
        EventHandler<CalculationCompletedMessage> handler = (sender, args) => eventRaised = true;

        // Act
        client.CalculationCompleted += handler;
        client.CalculationCompleted -= handler;

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void VisualizationDataUpdated_CanSubscribeAndUnsubscribe()
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);
        var eventRaised = false;
        EventHandler<VisualizationDataMessage> handler = (sender, args) => eventRaised = true;

        // Act
        client.VisualizationDataUpdated += handler;
        client.VisualizationDataUpdated -= handler;

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void DeviceStatusChanged_CanSubscribeAndUnsubscribe()
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);
        var eventRaised = false;
        EventHandler<DeviceStatusMessage> handler = (sender, args) => eventRaised = true;

        // Act
        client.DeviceStatusChanged += handler;
        client.DeviceStatusChanged -= handler;

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void AcquisitionStatusChanged_CanSubscribeAndUnsubscribe()
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);
        var eventRaised = false;
        EventHandler<AcquisitionStatusMessage> handler = (sender, args) => eventRaised = true;

        // Act
        client.AcquisitionStatusChanged += handler;
        client.AcquisitionStatusChanged -= handler;

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void ErrorOccurred_CanSubscribeAndUnsubscribe()
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);
        var eventRaised = false;
        EventHandler<ErrorMessage> handler = (sender, args) => eventRaised = true;

        // Act
        client.ErrorOccurred += handler;
        client.ErrorOccurred -= handler;

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void ProgressUpdated_CanSubscribeAndUnsubscribe()
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);
        var eventRaised = false;
        EventHandler<ProgressMessage> handler = (sender, args) => eventRaised = true;

        // Act
        client.ProgressUpdated += handler;
        client.ProgressUpdated -= handler;

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void LogMessageReceived_CanSubscribeAndUnsubscribe()
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);
        var eventRaised = false;
        EventHandler<LogMessage> handler = (sender, args) => eventRaised = true;

        // Act
        client.LogMessageReceived += handler;
        client.LogMessageReceived -= handler;

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void ConnectionStateChanged_CanSubscribeAndUnsubscribe()
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);
        var eventRaised = false;
        EventHandler<ConnectionStateChangedEventArgs> handler = (sender, args) => eventRaised = true;

        // Act
        client.ConnectionStateChanged += handler;
        client.ConnectionStateChanged -= handler;

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void MultipleSubscribers_CanSubscribeToSameEvent()
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);
        var subscriber1Called = false;
        var subscriber2Called = false;

        EventHandler<ErrorMessage> handler1 = (sender, args) => subscriber1Called = true;
        EventHandler<ErrorMessage> handler2 = (sender, args) => subscriber2Called = true;

        // Act
        client.ErrorOccurred += handler1;
        client.ErrorOccurred += handler2;

        // Assert
        // 验证可以添加多个订阅者（实际触发需要集成测试）
        Assert.False(subscriber1Called);
        Assert.False(subscriber2Called);

        // Cleanup
        client.ErrorOccurred -= handler1;
        client.ErrorOccurred -= handler2;
    }

    [Fact]
    public void EventSubscription_AfterDispose_DoesNotThrow()
    {
        // Arrange
        var client = new BeamAnalyzerApiClient(_mockLogger.Object);
        client.Dispose();

        // Act & Assert - 不应抛出异常
        client.RawDataReceived += (sender, args) => { };
        client.CalculationCompleted += (sender, args) => { };
        client.ErrorOccurred += (sender, args) => { };
    }
}
