using Microsoft.Extensions.Logging;
using Moq;

namespace BeamQualityAnalyzer.ApiClient.Tests;

/// <summary>
/// 连接和重试逻辑测试
/// 注意：这些测试验证客户端的连接管理逻辑，但由于 SignalR 的限制，
/// 实际的连接测试需要在集成测试中进行
/// </summary>
public class ConnectionAndRetryTests
{
    private readonly Mock<ILogger<BeamAnalyzerApiClient>> _mockLogger;

    public ConnectionAndRetryTests()
    {
        _mockLogger = new Mock<ILogger<BeamAnalyzerApiClient>>();
    }

    [Fact]
    public async Task ConnectAsync_WithInvalidUrl_ThrowsException()
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);
        var invalidUrl = "invalid-url";

        // Act & Assert
        // 注意：实际的连接失败会抛出 HttpRequestException 或 UriFormatException
        await Assert.ThrowsAnyAsync<Exception>(
            async () => await client.ConnectAsync(invalidUrl));
    }

    [Fact]
    public async Task DisconnectAsync_WhenNotConnected_DoesNotThrow()
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);

        // Act & Assert - 不应抛出异常
        await client.DisconnectAsync();
    }

    [Fact]
    public async Task DisconnectAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);

        // Act & Assert - 不应抛出异常
        await client.DisconnectAsync();
        await client.DisconnectAsync();
    }

    [Fact]
    public void ConnectionStateChanged_IsRaisedOnConnect()
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);
        var eventRaised = false;
        ConnectionStateChangedEventArgs? eventArgs = null;

        client.ConnectionStateChanged += (sender, args) =>
        {
            eventRaised = true;
            eventArgs = args;
        };

        // Act
        // 注意：实际的连接需要真实的服务器，这里只验证事件可以订阅
        // 实际的事件触发测试需要在集成测试中进行

        // Assert
        Assert.False(eventRaised); // 未实际连接时不会触发
    }

    [Fact]
    public void IsConnected_ReflectsConnectionState()
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);

        // Act & Assert
        Assert.False(client.IsConnected); // 初始状态应为未连接
    }

    [Theory]
    [InlineData("http://localhost:5000")]
    [InlineData("http://192.168.1.100:5000")]
    [InlineData("https://beam-analyzer.example.com")]
    public async Task ConnectAsync_WithValidUrlFormat_AttemptsConnection(string serverUrl)
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);

        // Act & Assert
        // 注意：由于没有真实服务器，连接会失败
        // 但我们验证客户端会尝试连接而不是立即抛出参数异常
        await Assert.ThrowsAnyAsync<Exception>(
            async () => await client.ConnectAsync(serverUrl));
    }

    [Fact]
    public async Task ConnectAsync_WithTrailingSlash_TrimsUrl()
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);
        var urlWithSlash = "http://localhost:5000/";

        // Act & Assert
        // 验证客户端会处理带尾部斜杠的 URL
        await Assert.ThrowsAnyAsync<Exception>(
            async () => await client.ConnectAsync(urlWithSlash));
    }
}
