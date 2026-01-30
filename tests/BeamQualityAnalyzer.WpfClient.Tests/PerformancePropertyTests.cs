using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
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
/// Property-based tests for performance requirements.
/// 
/// Property 18: Application startup time - Should start within 2 seconds
/// Property 19: Button response time - Should respond within 100ms (tested in MainViewModelPropertyTests)
/// Property 20: 2D chart update performance - Should update within 200ms
/// Property 21: 3D visualization update performance - Should update within 300ms
/// Property 22: Algorithm calculation performance - Should complete within 500ms
/// Property 25: UI thread non-blocking - UI should not freeze
/// 
/// Validates Requirements: 17.1, 17.2, 17.3, 17.4, 17.5, 15.8
/// </summary>
public class PerformancePropertyTests
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
    /// Helper to create a mock Settings Service
    /// </summary>
    private Mock<ISettingsService> CreateMockSettingsService()
    {
        var mock = new Mock<ISettingsService>();
        mock.Setup(x => x.LoadSettingsAsync())
            .ReturnsAsync(new AppSettings
            {
                ServerUrl = "http://localhost:5000",
                AutoReconnect = true,
                ReconnectInterval = 5000
            });
        return mock;
    }

    #region Property 18: Application Startup Time

    /// <summary>
    /// Property 18: Application startup time - MainViewModel should initialize within 2 seconds
    /// Feature: beam-quality-analyzer, Property 18: 应用启动时间
    /// Validates Requirement 17.1
    /// </summary>
    [Fact]
    public void MainViewModel_ShouldInitializeWithin2Seconds()
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var mockSettingsService = CreateMockSettingsService();

        // Act - Measure initialization time
        var stopwatch = Stopwatch.StartNew();
        
        var viewModel = new MainViewModel(mockApiClient.Object, mockSettingsService.Object);
        
        // Simulate connection (part of startup)
        mockApiClient.Raise(
            x => x.ConnectionStateChanged += null,
            mockApiClient.Object,
            new ConnectionStateChangedEventArgs(true));
        
        stopwatch.Stop();

        // Assert
        var startupTime = stopwatch.ElapsedMilliseconds;
        Assert.True(startupTime < 2000,
            $"MainViewModel initialization should complete within 2000ms, took {startupTime}ms");
    }

    /// <summary>
    /// Property 18: ChartViewModel should initialize quickly
    /// </summary>
    [Fact]
    public void ChartViewModel_ShouldInitializeQuickly()
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();

        // Act - Measure initialization time
        var stopwatch = Stopwatch.StartNew();
        
        var viewModel = new ChartViewModel(mockApiClient.Object);
        
        stopwatch.Stop();

        // Assert - Should be nearly instant (< 100ms)
        var initTime = stopwatch.ElapsedMilliseconds;
        Assert.True(initTime < 100,
            $"ChartViewModel initialization should be fast, took {initTime}ms");
    }

    /// <summary>
    /// Property 18: VisualizationViewModel should initialize quickly
    /// </summary>
    [Fact]
    public void VisualizationViewModel_ShouldInitializeQuickly()
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();

        // Act - Measure initialization time
        var stopwatch = Stopwatch.StartNew();
        
        var viewModel = new VisualizationViewModel(mockApiClient.Object);
        
        stopwatch.Stop();

        // Assert - Should be nearly instant (< 100ms)
        var initTime = stopwatch.ElapsedMilliseconds;
        Assert.True(initTime < 100,
            $"VisualizationViewModel initialization should be fast, took {initTime}ms");
    }

    /// <summary>
    /// Property 18: StatusBarViewModel should initialize quickly
    /// </summary>
    [Fact]
    public void StatusBarViewModel_ShouldInitializeQuickly()
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();

        // Act - Measure initialization time
        var stopwatch = Stopwatch.StartNew();
        
        var viewModel = new StatusBarViewModel(mockApiClient.Object);
        
        stopwatch.Stop();

        // Assert - Should be nearly instant (< 100ms)
        var initTime = stopwatch.ElapsedMilliseconds;
        Assert.True(initTime < 100,
            $"StatusBarViewModel initialization should be fast, took {initTime}ms");
    }

    #endregion

    #region Property 20: 2D Chart Update Performance

    /// <summary>
    /// Property 20: 2D chart data update should complete within 200ms
    /// Feature: beam-quality-analyzer, Property 20: 2D图表更新性能
    /// Validates Requirement 17.3
    /// </summary>
    [Property(MaxTest = 20, Arbitrary = new[] { typeof(Generators) })]
    public void ChartDataUpdate_ShouldCompleteWithin200ms(ValidRawDataMessage rawDataMessage)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var viewModel = new ChartViewModel(mockApiClient.Object);

        // Act - Measure update time
        var stopwatch = Stopwatch.StartNew();
        
        mockApiClient.Raise(
            x => x.RawDataReceived += null,
            mockApiClient.Object,
            rawDataMessage.Message);
        
        stopwatch.Stop();

        // Assert
        var updateTime = stopwatch.ElapsedMilliseconds;
        Assert.True(updateTime < 200,
            $"2D chart update should complete within 200ms, took {updateTime}ms");
    }

    /// <summary>
    /// Property 20: Chart should handle rapid updates efficiently
    /// </summary>
    [Property(MaxTest = 10)]
    public void ChartDataUpdate_ShouldHandleRapidUpdates(PositiveInt updateCount)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var viewModel = new ChartViewModel(mockApiClient.Object);
        
        var count = Math.Min(updateCount.Get % 20 + 1, 20); // 1-20 updates
        var allUpdatesFast = true;
        var updateTimes = new List<long>();

        // Act - Send multiple rapid updates
        for (int i = 0; i < count; i++)
        {
            var message = CreateRawDataMessage(i);

            var stopwatch = Stopwatch.StartNew();
            
            mockApiClient.Raise(
                x => x.RawDataReceived += null,
                mockApiClient.Object,
                message);
            
            stopwatch.Stop();
            updateTimes.Add(stopwatch.ElapsedMilliseconds);

            if (stopwatch.ElapsedMilliseconds > 200)
            {
                allUpdatesFast = false;
            }
        }

        var maxTime = updateTimes.Max();
        var avgTime = updateTimes.Average();

        Assert.True(allUpdatesFast,
            $"{count} rapid chart updates: max={maxTime}ms, avg={avgTime:F1}ms (all should be ≤ 200ms)");
    }

    #endregion

    #region Property 21: 3D Visualization Update Performance

    /// <summary>
    /// Property 21: 3D visualization update should complete within 300ms
    /// Feature: beam-quality-analyzer, Property 21: 3D可视化更新性能
    /// Validates Requirement 17.4
    /// </summary>
    [Property(MaxTest = 20, Arbitrary = new[] { typeof(Generators) })]
    public void VisualizationDataUpdate_ShouldCompleteWithin300ms(ValidVisualizationMessage vizMessage)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var viewModel = new VisualizationViewModel(mockApiClient.Object);

        // Act - Measure update time
        var stopwatch = Stopwatch.StartNew();
        
        mockApiClient.Raise(
            x => x.VisualizationDataUpdated += null,
            mockApiClient.Object,
            vizMessage.Message);
        
        stopwatch.Stop();

        // Assert
        var updateTime = stopwatch.ElapsedMilliseconds;
        Assert.True(updateTime < 300,
            $"3D visualization update should complete within 300ms, took {updateTime}ms");
    }

    /// <summary>
    /// Property 21: 3D visualization should handle rapid updates efficiently
    /// </summary>
    [Property(MaxTest = 10)]
    public void VisualizationDataUpdate_ShouldHandleRapidUpdates(PositiveInt updateCount)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var viewModel = new VisualizationViewModel(mockApiClient.Object);
        
        var count = Math.Min(updateCount.Get % 10 + 1, 10); // 1-10 updates
        var allUpdatesFast = true;
        var updateTimes = new List<long>();

        // Act - Send multiple rapid updates
        for (int i = 0; i < count; i++)
        {
            var message = CreateVisualizationMessage(i);

            var stopwatch = Stopwatch.StartNew();
            
            mockApiClient.Raise(
                x => x.VisualizationDataUpdated += null,
                mockApiClient.Object,
                message);
            
            stopwatch.Stop();
            updateTimes.Add(stopwatch.ElapsedMilliseconds);

            if (stopwatch.ElapsedMilliseconds > 300)
            {
                allUpdatesFast = false;
            }
        }

        var maxTime = updateTimes.Max();
        var avgTime = updateTimes.Average();

        Assert.True(allUpdatesFast,
            $"{count} rapid 3D visualization updates: max={maxTime}ms, avg={avgTime:F1}ms (all should be ≤ 300ms)");
    }

    #endregion

    #region Property 22: Algorithm Calculation Performance

    /// <summary>
    /// Property 22: Algorithm calculation should complete within 500ms
    /// Feature: beam-quality-analyzer, Property 22: 算法计算性能
    /// Validates Requirement 17.5
    /// </summary>
    [Property(MaxTest = 20, Arbitrary = new[] { typeof(Generators) })]
    public void AlgorithmCalculation_ShouldCompleteWithin500ms(ValidCalculationMessage calcMessage)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var viewModel = new ChartViewModel(mockApiClient.Object);

        // Act - Measure calculation processing time
        var stopwatch = Stopwatch.StartNew();
        
        mockApiClient.Raise(
            x => x.CalculationCompleted += null,
            mockApiClient.Object,
            calcMessage.Message);
        
        stopwatch.Stop();

        // Assert
        var processingTime = stopwatch.ElapsedMilliseconds;
        Assert.True(processingTime < 500,
            $"Algorithm calculation processing should complete within 500ms, took {processingTime}ms");
    }

    /// <summary>
    /// Property 22: Recalculate command should execute within 500ms
    /// </summary>
    [Fact]
    public async Task RecalculateCommand_ShouldExecuteWithin500ms()
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        mockApiClient.Setup(x => x.RecalculateAnalysisAsync(It.IsAny<AnalysisParametersDto>()))
            .ReturnsAsync(CommandResult.SuccessResult("重新计算已启动"));
        
        var viewModel = new ChartViewModel(mockApiClient.Object);
        viewModel.Magnification = 1.0; // Valid magnification

        // Act - Measure command execution time
        var stopwatch = Stopwatch.StartNew();
        
        await viewModel.RecalculateCommand.ExecuteAsync(null);
        
        stopwatch.Stop();

        // Assert
        var executionTime = stopwatch.ElapsedMilliseconds;
        Assert.True(executionTime < 500,
            $"Recalculate command should execute within 500ms, took {executionTime}ms");
    }

    #endregion

    #region Property 25: UI Thread Non-Blocking

    /// <summary>
    /// Property 25: Long-running operations should not block UI thread
    /// Feature: beam-quality-analyzer, Property 25: UI线程非阻塞
    /// Validates Requirement 15.8
    /// </summary>
    [Fact]
    public async Task LongRunningOperations_ShouldNotBlockUIThread()
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        
        // Simulate a slow API call (100ms delay)
        mockApiClient.Setup(x => x.StartAcquisitionAsync())
            .Returns(async () =>
            {
                await Task.Delay(100);
                return CommandResult.SuccessResult("采集已启动");
            });
        
        var viewModel = new MainViewModel(mockApiClient.Object, CreateMockSettingsService().Object);
        
        // Simulate connection
        mockApiClient.Raise(
            x => x.ConnectionStateChanged += null,
            mockApiClient.Object,
            new ConnectionStateChangedEventArgs(true));

        // Act - Execute command and check if UI thread is responsive
        var commandTask = viewModel.StartAcquisitionCommand.ExecuteAsync(null);
        
        // Simulate UI thread work during command execution
        var uiWorkStopwatch = Stopwatch.StartNew();
        
        // Try to do some "UI work" while command is executing
        for (int i = 0; i < 10; i++)
        {
            // Simulate UI property access
            var status = viewModel.CurrentStatus;
            var isAcquiring = viewModel.IsAcquiring;
            
            await Task.Delay(10);
        }
        
        uiWorkStopwatch.Stop();
        
        await commandTask;

        // Assert - UI work should complete quickly even while command is running
        Assert.True(uiWorkStopwatch.ElapsedMilliseconds < 200,
            $"UI thread should remain responsive during long operations, UI work took {uiWorkStopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Property 25: Multiple concurrent operations should not block UI
    /// </summary>
    [Fact]
    public async Task MultipleConcurrentOperations_ShouldNotBlockUI()
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        
        // Setup multiple slow operations
        mockApiClient.Setup(x => x.StartAcquisitionAsync())
            .Returns(async () => { await Task.Delay(50); return CommandResult.SuccessResult("Success"); });
        mockApiClient.Setup(x => x.ResetDeviceAsync())
            .Returns(async () => { await Task.Delay(50); return CommandResult.SuccessResult("Success"); });
        mockApiClient.Setup(x => x.GenerateScreenshotAsync())
            .Returns(async () => { await Task.Delay(50); return CommandResult<string>.SuccessResult("screenshot.png", "Success"); });
        
        var viewModel = new MainViewModel(mockApiClient.Object, CreateMockSettingsService().Object);
        
        // Simulate connection
        mockApiClient.Raise(
            x => x.ConnectionStateChanged += null,
            mockApiClient.Object,
            new ConnectionStateChangedEventArgs(true));

        // Act - Execute multiple commands concurrently
        var tasks = new List<Task>
        {
            viewModel.StartAcquisitionCommand.ExecuteAsync(null),
            viewModel.ResetMotorCommand.ExecuteAsync(null),
            viewModel.TakeScreenshotCommand.ExecuteAsync(null)
        };
        
        // Simulate UI thread work during concurrent operations
        var uiThreadResponsive = true;
        var uiWorkStopwatch = Stopwatch.StartNew();
        
        while (!Task.WhenAll(tasks).IsCompleted)
        {
            // Simulate UI property access
            var status = viewModel.CurrentStatus;
            var isConnected = viewModel.IsConnected;
            
            await Task.Delay(10);
            
            // If UI work takes too long, UI is blocked
            if (uiWorkStopwatch.ElapsedMilliseconds > 500)
            {
                uiThreadResponsive = false;
                break;
            }
        }
        
        uiWorkStopwatch.Stop();
        
        await Task.WhenAll(tasks);

        // Assert
        Assert.True(uiThreadResponsive,
            $"UI thread should remain responsive during concurrent operations, UI work took {uiWorkStopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Property 25: Data updates should not block UI thread
    /// </summary>
    [Property(MaxTest = 10)]
    public void RapidDataUpdates_ShouldNotBlockUIThread(PositiveInt updateCount)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var viewModel = new ChartViewModel(mockApiClient.Object);
        
        var count = Math.Min(updateCount.Get % 50 + 10, 50); // 10-50 updates
        var uiThreadResponsive = true;

        // Act - Send rapid data updates
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < count; i++)
        {
            var message = CreateRawDataMessage(i);
            
            mockApiClient.Raise(
                x => x.RawDataReceived += null,
                mockApiClient.Object,
                message);
            
            // Simulate UI property access between updates
            var rawDataCount = viewModel.RawDataX.Count;
            var parametersCount = viewModel.Parameters.Count;
            
            // If processing takes too long, UI might be blocked
            if (stopwatch.ElapsedMilliseconds > count * 20) // Allow 20ms per update
            {
                uiThreadResponsive = false;
                break;
            }
        }
        
        stopwatch.Stop();

        // Assert
        var avgTimePerUpdate = stopwatch.ElapsedMilliseconds / (double)count;
        Assert.True(uiThreadResponsive && avgTimePerUpdate < 20,
            $"{count} rapid updates: avg={avgTimePerUpdate:F1}ms per update, total={stopwatch.ElapsedMilliseconds}ms (UI should remain responsive)");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Create a raw data message for testing
    /// </summary>
    private RawDataReceivedMessage CreateRawDataMessage(int index)
    {
        var dataPoints = new RawDataPointDto[10];
        
        for (int i = 0; i < 10; i++)
        {
            dataPoints[i] = new RawDataPointDto
            {
                DetectorPosition = i * 10.0 + index,
                BeamDiameterX = 100.0 + i * 5.0 + index,
                BeamDiameterY = 100.0 + i * 5.0 + index,
                Timestamp = DateTime.Now
            };
        }

        return new RawDataReceivedMessage
        {
            DataPoints = dataPoints,
            Timestamp = DateTime.Now
        };
    }

    /// <summary>
    /// Create a visualization message for testing
    /// </summary>
    private VisualizationDataMessage CreateVisualizationMessage(int index)
    {
        // Create a simple JSON string for testing (no need to actually serialize matrix)
        var spotIntensityJson = "{\"data\":\"test_spot_intensity\"}";
        var energyDistributionJson = "{\"data\":\"test_energy_distribution\"}";

        return new VisualizationDataMessage
        {
            SpotIntensityDataJson = spotIntensityJson,
            SpotCenterX = 10.0 + index * 0.1,
            SpotCenterY = 10.0 + index * 0.1,
            EnergyDistribution3DJson = energyDistributionJson,
            Timestamp = DateTime.Now
        };
    }

    #endregion

    #region Generators

    /// <summary>
    /// Wrapper class for valid raw data messages
    /// </summary>
    public class ValidRawDataMessage
    {
        public RawDataReceivedMessage Message { get; set; } = null!;
    }

    /// <summary>
    /// Wrapper class for valid visualization messages
    /// </summary>
    public class ValidVisualizationMessage
    {
        public VisualizationDataMessage Message { get; set; } = null!;
    }

    /// <summary>
    /// Wrapper class for valid calculation messages
    /// </summary>
    public class ValidCalculationMessage
    {
        public CalculationCompletedMessage Message { get; set; } = null!;
    }

    /// <summary>
    /// FsCheck generators for performance tests
    /// </summary>
    public static class Generators
    {
        /// <summary>
        /// Generate a valid RawDataReceivedMessage
        /// </summary>
        public static Arbitrary<ValidRawDataMessage> ValidRawDataMessageArbitrary()
        {
            var gen = from dataPointCount in FsCheck.Gen.Choose(5, 20)
                      from startPosition in FsCheck.Gen.Choose(0, 100).Select(x => x / 10.0)
                      select new ValidRawDataMessage
                      {
                          Message = new RawDataReceivedMessage
                          {
                              DataPoints = Enumerable.Range(0, dataPointCount)
                                  .Select(i => new RawDataPointDto
                                  {
                                      DetectorPosition = startPosition + i * 10.0,
                                      BeamDiameterX = 100.0 + i * 5.0,
                                      BeamDiameterY = 100.0 + i * 5.0,
                                      Timestamp = DateTime.Now
                                  })
                                  .ToArray(),
                              Timestamp = DateTime.Now
                          }
                      };
            
            return FsCheck.Arb.From(gen);
        }

        /// <summary>
        /// Generate a valid VisualizationDataMessage
        /// </summary>
        public static Arbitrary<ValidVisualizationMessage> ValidVisualizationMessageArbitrary()
        {
            var gen = from centerX in FsCheck.Gen.Choose(0, 100).Select(x => x / 10.0)
                      from centerY in FsCheck.Gen.Choose(0, 100).Select(y => y / 10.0)
                      select new ValidVisualizationMessage
                      {
                          Message = new VisualizationDataMessage
                          {
                              SpotIntensityDataJson = "{\"data\":\"test_spot_intensity\"}",
                              SpotCenterX = centerX,
                              SpotCenterY = centerY,
                              EnergyDistribution3DJson = "{\"data\":\"test_energy_distribution\"}",
                              Timestamp = DateTime.Now
                          }
                      };
            
            return FsCheck.Arb.From(gen);
        }

        /// <summary>
        /// Generate a valid CalculationCompletedMessage
        /// </summary>
        public static Arbitrary<ValidCalculationMessage> ValidCalculationMessageArbitrary()
        {
            var gen = from mSquaredX in FsCheck.Gen.Choose(100, 500).Select(x => x / 100.0)
                      from mSquaredY in FsCheck.Gen.Choose(100, 500).Select(y => y / 100.0)
                      from peakX in FsCheck.Gen.Choose(-100, 100).Select(x => x / 10.0)
                      from peakY in FsCheck.Gen.Choose(-100, 100).Select(y => y / 10.0)
                      from waistPosX in FsCheck.Gen.Choose(0, 100).Select(x => x / 10.0)
                      from waistPosY in FsCheck.Gen.Choose(0, 100).Select(y => y / 10.0)
                      from waistDiamX in FsCheck.Gen.Choose(50, 500).Select(x => (double)x)
                      from waistDiamY in FsCheck.Gen.Choose(50, 500).Select(y => (double)y)
                      select new ValidCalculationMessage
                      {
                          Message = new CalculationCompletedMessage
                          {
                              MSquaredX = mSquaredX,
                              MSquaredY = mSquaredY,
                              MSquaredGlobal = Math.Sqrt(mSquaredX * mSquaredY),
                              PeakPositionX = peakX,
                              PeakPositionY = peakY,
                              BeamWaistPositionX = waistPosX,
                              BeamWaistPositionY = waistPosY,
                              BeamWaistDiameterX = waistDiamX,
                              BeamWaistDiameterY = waistDiamY,
                              Timestamp = DateTime.Now
                          }
                      };
            
            return FsCheck.Arb.From(gen);
        }
    }

    #endregion
}
