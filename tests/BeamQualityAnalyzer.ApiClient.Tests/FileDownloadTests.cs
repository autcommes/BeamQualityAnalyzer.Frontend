using Microsoft.Extensions.Logging;
using Moq;

namespace BeamQualityAnalyzer.ApiClient.Tests;

/// <summary>
/// 文件下载功能测试
/// 验证客户端的 HTTP 文件下载逻辑
/// </summary>
public class FileDownloadTests
{
    private readonly Mock<ILogger<BeamAnalyzerApiClient>> _mockLogger;

    public FileDownloadTests()
    {
        _mockLogger = new Mock<ILogger<BeamAnalyzerApiClient>>();
    }

    [Fact]
    public async Task DownloadFileAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);
        var filename = "test.png";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await client.DownloadFileAsync(filename));
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    [Fact]
    public async Task DownloadFileAsync_WithEmptyFilename_ThrowsInvalidOperationException()
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);
        var filename = "";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await client.DownloadFileAsync(filename));
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    [Fact]
    public async Task DownloadFileAsync_WithNullFilename_ThrowsInvalidOperationException()
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);

        // Act & Assert
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await client.DownloadFileAsync(null));
#pragma warning restore CS8625
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    [Fact]
    public async Task DownloadFileAsync_WithValidFilename_AttemptsDownload()
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);
        var filename = "screenshot_20260129_120000.png";

        // Act & Assert
        // 由于未连接，应抛出 InvalidOperationException
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await client.DownloadFileAsync(filename));
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    [Theory]
    [InlineData("report.pdf")]
    [InlineData("screenshot.png")]
    [InlineData("data.csv")]
    public async Task DownloadFileAsync_WithDifferentFileTypes_HandlesCorrectly(string filename)
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await client.DownloadFileAsync(filename));
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    [Fact]
    public async Task DownloadFileAsync_WithSpecialCharactersInFilename_HandlesCorrectly()
    {
        // Arrange
        using var client = new BeamAnalyzerApiClient(_mockLogger.Object);
        var filename = "测试文件_2026-01-29.pdf";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await client.DownloadFileAsync(filename));
        
        Assert.Equal("未连接到服务器", exception.Message);
    }

    [Fact]
    public async Task Dispose_DisposesHttpClient()
    {
        // Arrange
        var client = new BeamAnalyzerApiClient(_mockLogger.Object);

        // Act
        client.Dispose();

        // Assert
        // 验证 Dispose 后无法下载文件
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await client.DownloadFileAsync("test.png"));
    }
}
