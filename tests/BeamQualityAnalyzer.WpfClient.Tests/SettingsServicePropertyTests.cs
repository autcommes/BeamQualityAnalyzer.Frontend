using BeamQualityAnalyzer.WpfClient.Models;
using BeamQualityAnalyzer.WpfClient.Services;
using BeamQualityAnalyzer.WpfClient.Data;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging;
using Moq;

namespace BeamQualityAnalyzer.WpfClient.Tests;

/// <summary>
/// Property-based tests for SettingsService to verify configuration persistence.
/// 
/// Property 10: Configuration persistence round-trip - saved configuration should be identical when loaded
/// Validates Requirements: 13.6, 13.7
/// </summary>
public class SettingsServicePropertyTests : IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly string _testDbPath;

    public SettingsServicePropertyTests()
    {
        // 使用测试专用的数据库路径
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_config_{Guid.NewGuid()}.db");
        
        // 设置测试数据库路径
        ConfigDbContext.TestDatabasePath = _testDbPath;
        
        // 创建 SettingsService 实例
        var logger = new Mock<ILogger<SettingsService>>().Object;
        _settingsService = new SettingsService(logger);
    }

    public void Dispose()
    {
        // 清理测试数据库
        ConfigDbContext.TestDatabasePath = null;
        
        if (File.Exists(_testDbPath))
        {
            try
            {
                File.Delete(_testDbPath);
            }
            catch
            {
                // 忽略删除失败
            }
        }
    }

    /// <summary>
    /// Property 10: Configuration persistence round-trip
    /// 
    /// For any valid AppSettings configuration:
    /// - When saved to database
    /// - Then loaded from database
    /// - The loaded configuration should be equivalent to the original
    /// 
    /// Validates Requirements: 13.6, 13.7
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = new[] { typeof(AppSettingsGenerators) })]
    public Property SavedConfiguration_WhenLoaded_ShouldBeEquivalent(AppSettings generatedSettings)
    {
        return Prop.ForAll(
            Arb.From(Gen.Constant(generatedSettings)),
            settings =>
            {
                // Arrange - 确保使用新的数据库实例
                var testDbPath = Path.Combine(Path.GetTempPath(), $"test_config_{Guid.NewGuid()}.db");
                ConfigDbContext.TestDatabasePath = testDbPath;
                
                try
                {
                    var logger = new Mock<ILogger<SettingsService>>().Object;
                    var service = new SettingsService(logger);
                    
                    // 先加载默认配置（这会创建数据库和初始记录）
                    var defaultSettings = service.LoadSettingsAsync().Result;
                    
                    // 将生成的属性值复制到已加载的配置对象
                    defaultSettings.ServerUrl = settings.ServerUrl;
                    defaultSettings.AutoReconnect = settings.AutoReconnect;
                    defaultSettings.ReconnectInterval = settings.ReconnectInterval;
                    defaultSettings.ConnectionTimeout = settings.ConnectionTimeout;
                    defaultSettings.DeviceConnectionType = settings.DeviceConnectionType;
                    defaultSettings.DevicePortName = settings.DevicePortName;
                    defaultSettings.DeviceBaudRate = settings.DeviceBaudRate;
                    defaultSettings.DeviceAcquisitionFrequency = settings.DeviceAcquisitionFrequency;
                    defaultSettings.AlgorithmDefaultWavelength = settings.AlgorithmDefaultWavelength;
                    defaultSettings.AlgorithmMinDataPoints = settings.AlgorithmMinDataPoints;
                    defaultSettings.AlgorithmFitTolerance = settings.AlgorithmFitTolerance;
                    defaultSettings.ExportScreenshotDirectory = settings.ExportScreenshotDirectory;
                    defaultSettings.ExportReportDirectory = settings.ExportReportDirectory;
                    defaultSettings.ExportImageFormat = settings.ExportImageFormat;
                    defaultSettings.RemoteDatabaseEnabled = settings.RemoteDatabaseEnabled;
                    defaultSettings.RemoteDatabaseType = settings.RemoteDatabaseType;
                    defaultSettings.RemoteDatabaseConnectionString = settings.RemoteDatabaseConnectionString;
                    defaultSettings.RemoteDatabaseCommandTimeout = settings.RemoteDatabaseCommandTimeout;
                    defaultSettings.RemoteDatabaseEnableRetry = settings.RemoteDatabaseEnableRetry;
                    defaultSettings.RemoteDatabaseMaxRetryCount = settings.RemoteDatabaseMaxRetryCount;
                    defaultSettings.UITheme = settings.UITheme;
                    defaultSettings.UIChartRefreshInterval = settings.UIChartRefreshInterval;
                    defaultSettings.UIVisualization3DRefreshInterval = settings.UIVisualization3DRefreshInterval;
                    defaultSettings.LoggingMinimumLevel = settings.LoggingMinimumLevel;
                    defaultSettings.LoggingDirectory = settings.LoggingDirectory;
                    defaultSettings.Version = settings.Version;
                    
                    // Act - 保存修改后的配置（同步执行）
                    service.SaveSettingsAsync(defaultSettings, "Property test").Wait();
                    
                    // 清除缓存，强制从数据库加载
                    var freshService = new SettingsService(logger);
                    var loadedSettings = freshService.LoadSettingsAsync().Result;
                    
                    // Assert - 验证所有关键属性相等
                    var isEquivalent =
                        loadedSettings.Id == defaultSettings.Id &&
                        loadedSettings.ServerUrl == defaultSettings.ServerUrl &&
                        loadedSettings.AutoReconnect == defaultSettings.AutoReconnect &&
                        loadedSettings.ReconnectInterval == defaultSettings.ReconnectInterval &&
                        loadedSettings.ConnectionTimeout == defaultSettings.ConnectionTimeout &&
                        loadedSettings.DeviceConnectionType == defaultSettings.DeviceConnectionType &&
                        loadedSettings.DevicePortName == defaultSettings.DevicePortName &&
                        loadedSettings.DeviceBaudRate == defaultSettings.DeviceBaudRate &&
                        loadedSettings.DeviceAcquisitionFrequency == defaultSettings.DeviceAcquisitionFrequency &&
                        Math.Abs(loadedSettings.AlgorithmDefaultWavelength - defaultSettings.AlgorithmDefaultWavelength) < 0.001 &&
                        loadedSettings.AlgorithmMinDataPoints == defaultSettings.AlgorithmMinDataPoints &&
                        Math.Abs(loadedSettings.AlgorithmFitTolerance - defaultSettings.AlgorithmFitTolerance) < 0.0001 &&
                        loadedSettings.ExportScreenshotDirectory == defaultSettings.ExportScreenshotDirectory &&
                        loadedSettings.ExportReportDirectory == defaultSettings.ExportReportDirectory &&
                        loadedSettings.ExportImageFormat == defaultSettings.ExportImageFormat &&
                        loadedSettings.RemoteDatabaseEnabled == defaultSettings.RemoteDatabaseEnabled &&
                        loadedSettings.RemoteDatabaseType == defaultSettings.RemoteDatabaseType &&
                        loadedSettings.RemoteDatabaseConnectionString == defaultSettings.RemoteDatabaseConnectionString &&
                        loadedSettings.RemoteDatabaseCommandTimeout == defaultSettings.RemoteDatabaseCommandTimeout &&
                        loadedSettings.RemoteDatabaseEnableRetry == defaultSettings.RemoteDatabaseEnableRetry &&
                        loadedSettings.RemoteDatabaseMaxRetryCount == defaultSettings.RemoteDatabaseMaxRetryCount &&
                        loadedSettings.UITheme == defaultSettings.UITheme &&
                        Math.Abs(loadedSettings.UIChartRefreshInterval - defaultSettings.UIChartRefreshInterval) < 0.001 &&
                        Math.Abs(loadedSettings.UIVisualization3DRefreshInterval - defaultSettings.UIVisualization3DRefreshInterval) < 0.001 &&
                        loadedSettings.LoggingMinimumLevel == defaultSettings.LoggingMinimumLevel &&
                        loadedSettings.LoggingDirectory == defaultSettings.LoggingDirectory &&
                        loadedSettings.Version == defaultSettings.Version;
                    
                    return isEquivalent.ToProperty()
                        .Label($"Configuration round-trip failed. Original ServerUrl: {defaultSettings.ServerUrl}, Loaded: {loadedSettings.ServerUrl}");
                }
                finally
                {
                    // 清理测试数据库
                    ConfigDbContext.TestDatabasePath = null;
                    if (File.Exists(testDbPath))
                    {
                        try
                        {
                            File.Delete(testDbPath);
                        }
                        catch
                        {
                            // 忽略删除失败
                        }
                    }
                }
            });
    }

    /// <summary>
    /// Unit test: Default configuration should be loadable
    /// </summary>
    [Fact]
    public async Task LoadSettings_FirstTime_ShouldReturnDefaultConfiguration()
    {
        // Arrange
        var testDbPath = Path.Combine(Path.GetTempPath(), $"test_config_{Guid.NewGuid()}.db");
        ConfigDbContext.TestDatabasePath = testDbPath;
        
        try
        {
            var logger = new Mock<ILogger<SettingsService>>().Object;
            var service = new SettingsService(logger);
            
            // Act
            var settings = await service.LoadSettingsAsync();
            
            // Assert
            Assert.NotNull(settings);
            Assert.Equal(1, settings.Id);
            Assert.Equal("http://localhost:5000", settings.ServerUrl);
            Assert.True(settings.AutoReconnect);
            Assert.Equal(5000, settings.ReconnectInterval);
        }
        finally
        {
            ConfigDbContext.TestDatabasePath = null;
            if (File.Exists(testDbPath))
            {
                try
                {
                    File.Delete(testDbPath);
                }
                catch
                {
                    // 忽略删除失败
                }
            }
        }
    }

    /// <summary>
    /// Unit test: Configuration history should be saved
    /// </summary>
    [Fact]
    public async Task SaveSettings_ShouldCreateHistoryRecord()
    {
        // Arrange
        var testDbPath = Path.Combine(Path.GetTempPath(), $"test_config_{Guid.NewGuid()}.db");
        ConfigDbContext.TestDatabasePath = testDbPath;
        
        try
        {
            var logger = new Mock<ILogger<SettingsService>>().Object;
            var service = new SettingsService(logger);
            
            var settings = await service.LoadSettingsAsync();
            settings.ServerUrl = "http://test-server:5000";
            
            // Act
            await service.SaveSettingsAsync(settings, "Test change");
            var history = await service.GetSettingsHistoryAsync(10);
            
            // Assert
            Assert.NotEmpty(history);
            Assert.Contains(history, h => h.ChangeDescription != null && h.ChangeDescription.Contains("Test change"));
        }
        finally
        {
            ConfigDbContext.TestDatabasePath = null;
            if (File.Exists(testDbPath))
            {
                try
                {
                    File.Delete(testDbPath);
                }
                catch
                {
                    // 忽略删除失败
                }
            }
        }
    }

    /// <summary>
    /// Unit test: Rollback should restore previous configuration
    /// </summary>
    [Fact]
    public async Task RollbackToHistory_ShouldRestorePreviousConfiguration()
    {
        // Arrange
        var testDbPath = Path.Combine(Path.GetTempPath(), $"test_config_{Guid.NewGuid()}.db");
        ConfigDbContext.TestDatabasePath = testDbPath;
        
        try
        {
            var logger = new Mock<ILogger<SettingsService>>().Object;
            var service = new SettingsService(logger);
            
            // 保存初始配置
            var settings = await service.LoadSettingsAsync();
            var originalUrl = settings.ServerUrl;
            await service.SaveSettingsAsync(settings, "Initial");
            
            // 修改配置
            settings.ServerUrl = "http://modified-server:5000";
            await service.SaveSettingsAsync(settings, "Modified");
            
            // 获取历史记录
            var history = await service.GetSettingsHistoryAsync(10);
            var initialHistory = history.FirstOrDefault(h => h.ChangeDescription == "Initial");
            
            Assert.NotNull(initialHistory);
            
            // Act - 回滚到初始配置
            var rollbackSuccess = await service.RollbackToHistoryAsync(initialHistory.Id);
            
            // Assert
            Assert.True(rollbackSuccess);
            
            var restoredSettings = await new SettingsService(logger).LoadSettingsAsync();
            Assert.Equal(originalUrl, restoredSettings.ServerUrl);
        }
        finally
        {
            ConfigDbContext.TestDatabasePath = null;
            if (File.Exists(testDbPath))
            {
                try
                {
                    File.Delete(testDbPath);
                }
                catch
                {
                    // 忽略删除失败
                }
            }
        }
    }
}

/// <summary>
/// FsCheck generators for AppSettings
/// </summary>
public static class AppSettingsGenerators
{
    /// <summary>
    /// Generator for valid AppSettings instances
    /// </summary>
    public static Arbitrary<AppSettings> AppSettings()
    {
        var gen = from serverUrl in Gen.Elements("http://localhost:5000", "http://192.168.1.100:5000", "https://server.example.com")
                  from autoReconnect in Arb.Generate<bool>()
                  from reconnectInterval in Gen.Choose(1000, 30000)
                  from connectionTimeout in Gen.Choose(10000, 60000)
                  from deviceType in Gen.Elements("Virtual", "Serial", "USB", "Network")
                  from portName in Gen.Elements("COM1", "COM3", "COM5", "/dev/ttyUSB0")
                  from baudRate in Gen.Elements(9600, 19200, 38400, 57600, 115200)
                  from acquisitionFreq in Gen.Choose(1, 100)
                  from wavelength in Gen.Choose(400, 1000).Select(x => (double)x)
                  from minDataPoints in Gen.Choose(5, 50)
                  from fitTolerance in Gen.Choose(1, 100).Select(x => x / 10000.0)
                  from screenshotDir in Gen.Constant(@"C:\BeamAnalyzer\Screenshots")
                  from reportDir in Gen.Constant(@"C:\BeamAnalyzer\Reports")
                  from imageFormat in Gen.Elements("PNG", "JPEG")
                  from dbEnabled in Arb.Generate<bool>()
                  from dbType in Gen.Elements("None", "MySQL", "SqlServer")
                  from dbTimeout in Gen.Choose(10, 120)
                  from dbRetry in Arb.Generate<bool>()
                  from dbMaxRetry in Gen.Choose(1, 5)
                  from theme in Gen.Elements("Dark", "Light")
                  from chartRefresh in Gen.Choose(100, 500).Select(x => (double)x)
                  from viz3dRefresh in Gen.Choose(200, 1000).Select(x => (double)x)
                  from logLevel in Gen.Elements("Debug", "Information", "Warning", "Error")
                  from logDir in Gen.Constant(@"C:\BeamAnalyzer\Logs")
                  select new BeamQualityAnalyzer.WpfClient.Models.AppSettings
                  {
                      Id = 1,
                      ServerUrl = serverUrl,
                      AutoReconnect = autoReconnect,
                      ReconnectInterval = reconnectInterval,
                      ConnectionTimeout = connectionTimeout,
                      DeviceConnectionType = deviceType,
                      DevicePortName = portName,
                      DeviceBaudRate = baudRate,
                      DeviceAcquisitionFrequency = acquisitionFreq,
                      AlgorithmDefaultWavelength = wavelength,
                      AlgorithmMinDataPoints = minDataPoints,
                      AlgorithmFitTolerance = fitTolerance,
                      ExportScreenshotDirectory = screenshotDir,
                      ExportReportDirectory = reportDir,
                      ExportImageFormat = imageFormat,
                      RemoteDatabaseEnabled = dbEnabled,
                      RemoteDatabaseType = dbType,
                      RemoteDatabaseConnectionString = dbEnabled ? "Server=localhost;Database=test;User=root;Password=pass;" : null,
                      RemoteDatabaseCommandTimeout = dbTimeout,
                      RemoteDatabaseEnableRetry = dbRetry,
                      RemoteDatabaseMaxRetryCount = dbMaxRetry,
                      UITheme = theme,
                      UIChartRefreshInterval = chartRefresh,
                      UIVisualization3DRefreshInterval = viz3dRefresh,
                      LoggingMinimumLevel = logLevel,
                      LoggingDirectory = logDir,
                      LastModified = DateTime.Now,
                      Version = "1.0.0"
                  };

        return Arb.From(gen);
    }
}
