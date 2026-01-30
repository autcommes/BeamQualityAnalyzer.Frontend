using BeamQualityAnalyzer.Contracts.Dtos;
using BeamQualityAnalyzer.Contracts.Messages;
using Microsoft.Extensions.Logging;
using Moq;

namespace BeamQualityAnalyzer.ApiClient.Tests;

/// <summary>
/// API 客户端单元测试
/// 注意：由于 HubConnection 是密封类，这些测试主要验证客户端的错误处理和状态管理逻辑
/// </summary>
public class BeamAnalyzerApiClientTests : IDisposable
{
    private readonly Mock<ILogger<BeamAnalyzerApiClient>> _mockLogger;
    private readonly BeamAnalyzerApiClient _client;

    public BeamAnalyzerApiClientTests()
    {
        _mockLogger = new Mock<ILogger<BeamAnalyzerApiClient>>();
        _client = new BeamAnalyzerApiClient(_mockLogger.Object);
    }

    public void Dispose()
    {
        _client?.Dispose();
    }

    #region 构造函数测试

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
        Assert.Throws<ArgumentNullException>(() => new BeamAnalyzerApiClient(null));
#pragma warning restore CS8625
    }

    [Fact]
    public void Constructor_WithValidLogger_CreatesInstance()
    {
        // Arrange & Act
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);

        // Assert
        Assert.NotNull(client);
        Assert.False(client.IsConnected);
    }

    #endregion

    #region 连接状态测试

    [Fact]
    public void IsConnected_InitialState_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(_client.IsConnected);
    }

    [Fact]
    public async Task StartAcquisitionAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _client.StartAcquisitionAsync());
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    [Fact]
    public async Task StopAcquisitionAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _client.StopAcquisitionAsync());
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    [Fact]
    public async Task EmergencyStopAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _client.EmergencyStopAsync());
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    [Fact]
    public async Task GetAcquisitionStatusAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _client.GetAcquisitionStatusAsync());
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    #endregion

    #region 设备控制测试

    [Fact]
    public async Task ResetDeviceAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _client.ResetDeviceAsync());
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    [Fact]
    public async Task GetDeviceStatusAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _client.GetDeviceStatusAsync());
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    #endregion

    #region 算法计算测试

    [Fact]
    public async Task RecalculateAnalysisAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        var parameters = new AnalysisParametersDto();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _client.RecalculateAnalysisAsync(parameters));
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    [Fact]
    public async Task GetLatestAnalysisResultAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _client.GetLatestAnalysisResultAsync());
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    #endregion

    #region 数据库操作测试

    [Fact]
    public async Task SaveMeasurementAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        var record = new MeasurementRecordDto();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _client.SaveMeasurementAsync(record));
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    [Fact]
    public async Task QueryMeasurementsAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        var parameters = new QueryParametersDto();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _client.QueryMeasurementsAsync(parameters));
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    [Fact]
    public async Task DeleteMeasurementAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _client.DeleteMeasurementAsync(1));
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    #endregion

    #region 导出功能测试

    [Fact]
    public async Task GenerateScreenshotAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _client.GenerateScreenshotAsync());
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    [Fact]
    public async Task GenerateReportAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new ReportOptionsDto();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _client.GenerateReportAsync(options));
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    [Fact]
    public async Task DownloadFileAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _client.DownloadFileAsync("test.png"));
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    #endregion

    #region 配置管理测试

    [Fact]
    public async Task GetSettingsAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _client.GetSettingsAsync());
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    [Fact]
    public async Task UpdateSettingsAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new AppSettingsDto();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _client.UpdateSettingsAsync(settings));
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    [Fact]
    public async Task TestDatabaseConnectionAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new DatabaseSettingsDto();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _client.TestDatabaseConnectionAsync(settings));
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    #endregion

    #region 自动测试功能测试

    [Fact]
    public async Task StartAutoTestAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new AutoTestConfigurationDto
        {
            AnalysisParameters = new AnalysisParametersDto()
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _client.StartAutoTestAsync(config));
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    [Fact]
    public async Task GetAutoTestStatusAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _client.GetAutoTestStatusAsync());
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    #endregion

    #region 数据流订阅测试

    [Fact]
    public async Task SubscribeToDataStreamAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _client.SubscribeToDataStreamAsync());
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    [Fact]
    public async Task UnsubscribeFromDataStreamAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _client.UnsubscribeFromDataStreamAsync());
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    #endregion

    #region 事件处理测试

    [Fact]
    public void RawDataReceived_EventCanBeSubscribed()
    {
        // Arrange
        var eventRaised = false;
        _client.RawDataReceived += (sender, args) => eventRaised = true;

        // Act
        // 注意：由于无法触发实际的 SignalR 事件，这里只验证事件可以订阅
        // 实际的事件触发需要集成测试

        // Assert
        Assert.False(eventRaised); // 未连接时不会触发
    }

    [Fact]
    public void CalculationCompleted_EventCanBeSubscribed()
    {
        // Arrange
        var eventRaised = false;
        _client.CalculationCompleted += (sender, args) => eventRaised = true;

        // Act & Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void ConnectionStateChanged_EventCanBeSubscribed()
    {
        // Arrange
        var eventRaised = false;
        _client.ConnectionStateChanged += (sender, args) => eventRaised = true;

        // Act & Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void ErrorOccurred_EventCanBeSubscribed()
    {
        // Arrange
        var eventRaised = false;
        _client.ErrorOccurred += (sender, args) => eventRaised = true;

        // Act & Assert
        Assert.False(eventRaised);
    }

    #endregion

    #region Dispose 测试

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);

        // Act & Assert - 不应抛出异常
        client.Dispose();
        client.Dispose();
    }

    [Fact]
    public void Dispose_DisconnectsClient()
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);

        // Act
        client.Dispose();

        // Assert
        Assert.False(client.IsConnected);
    }

    #endregion
}
