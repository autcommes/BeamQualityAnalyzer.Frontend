using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Media3D;
using BeamQualityAnalyzer.ApiClient;
using BeamQualityAnalyzer.Contracts.Messages;
using BeamQualityAnalyzer.WpfClient.ViewModels;
using FsCheck;
using FsCheck.Xunit;
using Moq;

namespace BeamQualityAnalyzer.WpfClient.Tests;

/// <summary>
/// Property-based tests for VisualizationViewModel.
/// 
/// Property 20: 2D chart update performance - 2D chart should update within 200ms
/// Property 21: 3D visualization update performance - 3D visualization should update within 300ms
/// 
/// Validates Requirements: 7.4, 8.6
/// </summary>
public class VisualizationViewModelPropertyTests
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
    /// Helper to create a synchronous UI thread invoker for testing
    /// </summary>
    private Action<Action> CreateSyncUIThreadInvoker()
    {
        return action => action(); // Execute synchronously in test
    }

    #region Property 20: 2D Chart Update Performance

    /// <summary>
    /// Property 20: 2D chart should update within 200ms
    /// Validates Requirement 7.4
    /// </summary>
    [Property(MaxTest = 20, Arbitrary = new[] { typeof(Generators) })]
    public void TwoDChartUpdate_ShouldCompleteWithin200ms(Valid2DVisualizationMessage validMessage)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var uiThreadInvoker = CreateSyncUIThreadInvoker();
        var viewModel = new VisualizationViewModel(mockApiClient.Object, uiThreadInvoker);

        // Capture initial state
        var initialSpotData = viewModel.SpotIntensityData;
        var initialCenter = viewModel.SpotCenter;

        // Act - Measure update time
        var stopwatch = Stopwatch.StartNew();
        
        mockApiClient.Raise(
            x => x.VisualizationDataUpdated += null,
            mockApiClient.Object,
            validMessage.Message);
        
        stopwatch.Stop();

        // Assert
        var updateTime = stopwatch.ElapsedMilliseconds;
        var spotDataUpdated = viewModel.SpotIntensityData != initialSpotData;
        var centerUpdated = viewModel.SpotCenter != initialCenter;

        var performanceOk = updateTime <= 200;
        var dataUpdated = spotDataUpdated || centerUpdated;

        Assert.True(performanceOk && dataUpdated,
            $"2D chart update took {updateTime}ms (should be ≤ 200ms), data updated: {dataUpdated}");
    }

    /// <summary>
    /// Property 20: Multiple rapid 2D updates should all complete within 200ms each
    /// </summary>
    [Property(MaxTest = 10)]
    public void MultipleRapid2DUpdates_ShouldEachCompleteWithin200ms(PositiveInt updateCount)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var uiThreadInvoker = CreateSyncUIThreadInvoker();
        var viewModel = new VisualizationViewModel(mockApiClient.Object, uiThreadInvoker);
        
        var count = Math.Min(updateCount.Get % 10 + 1, 10); // 1-10 updates
        var allUpdatesFast = true;
        var updateTimes = new List<long>();

        // Act - Send multiple updates
        for (int i = 0; i < count; i++)
        {
            var message = CreateVisualizationMessage(32, 32, i); // 32x32 matrix

            var stopwatch = Stopwatch.StartNew();
            
            mockApiClient.Raise(
                x => x.VisualizationDataUpdated += null,
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
            $"{count} rapid 2D updates: max={maxTime}ms, avg={avgTime:F1}ms (all should be ≤ 200ms)");
    }

    /// <summary>
    /// Property 20: Large 2D matrix (128x128) should still update within 200ms
    /// </summary>
    [Property(MaxTest = 10)]
    public void Large2DMatrix_ShouldUpdateWithin200ms(PositiveInt seed)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var uiThreadInvoker = CreateSyncUIThreadInvoker();
        var viewModel = new VisualizationViewModel(mockApiClient.Object, uiThreadInvoker);

        var message = CreateVisualizationMessage(128, 128, seed.Get); // Large 128x128 matrix

        // Act - Measure update time
        var stopwatch = Stopwatch.StartNew();
        
        mockApiClient.Raise(
            x => x.VisualizationDataUpdated += null,
            mockApiClient.Object,
            message);
        
        stopwatch.Stop();

        // Assert
        var updateTime = stopwatch.ElapsedMilliseconds;
        var dataUpdated = viewModel.SpotIntensityData != null;

        var performanceOk = updateTime <= 200;

        Assert.True(performanceOk && dataUpdated,
            $"Large 2D matrix (128x128) update took {updateTime}ms (should be ≤ 200ms)");
    }

    #endregion

    #region Property 21: 3D Visualization Update Performance

    /// <summary>
    /// Property 21: 3D visualization should update within 300ms
    /// Validates Requirement 8.6
    /// </summary>
    [Property(MaxTest = 20, Arbitrary = new[] { typeof(Generators) })]
    public void ThreeDVisualizationUpdate_ShouldCompleteWithin300ms(Valid3DVisualizationMessage validMessage)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var uiThreadInvoker = CreateSyncUIThreadInvoker();
        var viewModel = new VisualizationViewModel(mockApiClient.Object, uiThreadInvoker);

        // Capture initial state
        var initialEnergyData = viewModel.EnergyDistributionData;

        // Act - Measure update time
        var stopwatch = Stopwatch.StartNew();
        
        mockApiClient.Raise(
            x => x.VisualizationDataUpdated += null,
            mockApiClient.Object,
            validMessage.Message);
        
        stopwatch.Stop();

        // Assert
        var updateTime = stopwatch.ElapsedMilliseconds;
        var energyDataUpdated = viewModel.EnergyDistributionData != initialEnergyData;

        var performanceOk = updateTime <= 300;

        Assert.True(performanceOk && energyDataUpdated,
            $"3D visualization update took {updateTime}ms (should be ≤ 300ms), data updated: {energyDataUpdated}");
    }

    /// <summary>
    /// Property 21: Multiple rapid 3D updates should all complete within 300ms each
    /// </summary>
    [Property(MaxTest = 10)]
    public void MultipleRapid3DUpdates_ShouldEachCompleteWithin300ms(PositiveInt updateCount)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var uiThreadInvoker = CreateSyncUIThreadInvoker();
        var viewModel = new VisualizationViewModel(mockApiClient.Object, uiThreadInvoker);
        
        var count = Math.Min(updateCount.Get % 10 + 1, 10); // 1-10 updates
        var allUpdatesFast = true;
        var updateTimes = new List<long>();

        // Act - Send multiple updates
        for (int i = 0; i < count; i++)
        {
            var message = CreateVisualizationMessage(32, 32, i); // 32x32 matrix

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
            $"{count} rapid 3D updates: max={maxTime}ms, avg={avgTime:F1}ms (all should be ≤ 300ms)");
    }

    /// <summary>
    /// Property 21: Large 3D matrix (128x128) should still update within 300ms
    /// </summary>
    [Property(MaxTest = 10)]
    public void Large3DMatrix_ShouldUpdateWithin300ms(PositiveInt seed)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var uiThreadInvoker = CreateSyncUIThreadInvoker();
        var viewModel = new VisualizationViewModel(mockApiClient.Object, uiThreadInvoker);

        var message = CreateVisualizationMessage(128, 128, seed.Get); // Large 128x128 matrix

        // Act - Measure update time
        var stopwatch = Stopwatch.StartNew();
        
        mockApiClient.Raise(
            x => x.VisualizationDataUpdated += null,
            mockApiClient.Object,
            message);
        
        stopwatch.Stop();

        // Assert
        var updateTime = stopwatch.ElapsedMilliseconds;
        var dataUpdated = viewModel.EnergyDistributionData != null;

        var performanceOk = updateTime <= 300;

        Assert.True(performanceOk && dataUpdated,
            $"Large 3D matrix (128x128) update took {updateTime}ms (should be ≤ 300ms)");
    }

    /// <summary>
    /// Property 21: 3D data conversion should produce valid Point3D matrix
    /// </summary>
    [Property(MaxTest = 20, Arbitrary = new[] { typeof(Generators) })]
    public void ThreeDDataConversion_ShouldProduceValidPoint3DMatrix(Valid3DVisualizationMessage validMessage)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var uiThreadInvoker = CreateSyncUIThreadInvoker();
        var viewModel = new VisualizationViewModel(mockApiClient.Object, uiThreadInvoker);

        // Act
        mockApiClient.Raise(
            x => x.VisualizationDataUpdated += null,
            mockApiClient.Object,
            validMessage.Message);

        // Assert
        var energyData = viewModel.EnergyDistributionData;
        
        if (energyData != null)
        {
            var rows = energyData.GetLength(0);
            var cols = energyData.GetLength(1);
            
            // Check that all Point3D values are valid
            var allPointsValid = true;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    var point = energyData[i, j];
                    
                    // X and Y should match indices
                    if (point.X != i || point.Y != j)
                    {
                        allPointsValid = false;
                        break;
                    }
                    
                    // Z should be a valid number (not NaN or Infinity)
                    if (double.IsNaN(point.Z) || double.IsInfinity(point.Z))
                    {
                        allPointsValid = false;
                        break;
                    }
                }
                
                if (!allPointsValid) break;
            }
            
            Assert.True(allPointsValid, "All Point3D values should be valid (X=i, Y=j, Z=intensity)");
        }
    }

    #endregion

    #region Combined Performance Tests

    /// <summary>
    /// Combined test: Both 2D and 3D updates should meet their performance targets
    /// </summary>
    [Property(MaxTest = 10)]
    public void CombinedUpdate_ShouldMeetBothPerformanceTargets(PositiveInt seed)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var uiThreadInvoker = CreateSyncUIThreadInvoker();
        var viewModel = new VisualizationViewModel(mockApiClient.Object, uiThreadInvoker);

        var message = CreateVisualizationMessage(64, 64, seed.Get); // Medium 64x64 matrix

        // Act - Measure update time
        var stopwatch = Stopwatch.StartNew();
        
        mockApiClient.Raise(
            x => x.VisualizationDataUpdated += null,
            mockApiClient.Object,
            message);
        
        stopwatch.Stop();

        // Assert
        var updateTime = stopwatch.ElapsedMilliseconds;
        var spotDataUpdated = viewModel.SpotIntensityData != null;
        var energyDataUpdated = viewModel.EnergyDistributionData != null;

        // Both 2D and 3D should update, and total time should be within 3D limit (300ms)
        var performanceOk = updateTime <= 300;
        var bothUpdated = spotDataUpdated && energyDataUpdated;

        Assert.True(performanceOk && bothUpdated,
            $"Combined 2D+3D update took {updateTime}ms (should be ≤ 300ms), both updated: {bothUpdated}");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Create a VisualizationDataMessage with specified matrix size
    /// </summary>
    private VisualizationDataMessage CreateVisualizationMessage(int rows, int cols, int seed)
    {
        var random = new System.Random(seed);
        
        // Create intensity matrix
        var intensityMatrix = new double[rows][];
        for (int i = 0; i < rows; i++)
        {
            intensityMatrix[i] = new double[cols];
            for (int j = 0; j < cols; j++)
            {
                // Generate Gaussian-like intensity distribution
                var dx = i - rows / 2.0;
                var dy = j - cols / 2.0;
                var distance = Math.Sqrt(dx * dx + dy * dy);
                intensityMatrix[i][j] = Math.Exp(-distance * distance / (rows * cols / 16.0)) * random.NextDouble();
            }
        }

        return new VisualizationDataMessage
        {
            SpotCenterX = rows / 2.0 + random.NextDouble() - 0.5,
            SpotCenterY = cols / 2.0 + random.NextDouble() - 0.5,
            SpotIntensityDataJson = JsonSerializer.Serialize(intensityMatrix),
            EnergyDistribution3DJson = JsonSerializer.Serialize(intensityMatrix),
            Timestamp = DateTime.Now
        };
    }

    #endregion

    #region Helper Generators

    /// <summary>
    /// Wrapper class for valid 2D visualization messages
    /// </summary>
    public class Valid2DVisualizationMessage
    {
        public VisualizationDataMessage Message { get; set; } = null!;
    }

    /// <summary>
    /// Wrapper class for valid 3D visualization messages
    /// </summary>
    public class Valid3DVisualizationMessage
    {
        public VisualizationDataMessage Message { get; set; } = null!;
    }

    #endregion

    #region Generators

    /// <summary>
    /// FsCheck generators for VisualizationViewModel tests
    /// </summary>
    public static class Generators
    {
        /// <summary>
        /// Generate a valid 2D VisualizationDataMessage with realistic matrix data
        /// </summary>
        public static Arbitrary<Valid2DVisualizationMessage> Valid2DVisualizationMessageArbitrary()
        {
            var gen = from rows in FsCheck.Gen.Choose(16, 64) // 16x16 to 64x64
                      from cols in FsCheck.Gen.Choose(16, 64)
                      from centerX in FsCheck.Gen.Choose(0, 100).Select(x => x / 10.0)
                      from centerY in FsCheck.Gen.Choose(0, 100).Select(y => y / 10.0)
                      from seed in FsCheck.Gen.Choose(0, 10000)
                      select new Valid2DVisualizationMessage
                      {
                          Message = CreateVisualizationMessageStatic(rows, cols, centerX, centerY, seed)
                      };
            
            return FsCheck.Arb.From(gen);
        }

        /// <summary>
        /// Generate a valid 3D VisualizationDataMessage with realistic matrix data
        /// </summary>
        public static Arbitrary<Valid3DVisualizationMessage> Valid3DVisualizationMessageArbitrary()
        {
            var gen = from rows in FsCheck.Gen.Choose(16, 64) // 16x16 to 64x64
                      from cols in FsCheck.Gen.Choose(16, 64)
                      from centerX in FsCheck.Gen.Choose(0, 100).Select(x => x / 10.0)
                      from centerY in FsCheck.Gen.Choose(0, 100).Select(y => y / 10.0)
                      from seed in FsCheck.Gen.Choose(0, 10000)
                      select new Valid3DVisualizationMessage
                      {
                          Message = CreateVisualizationMessageStatic(rows, cols, centerX, centerY, seed)
                      };
            
            return FsCheck.Arb.From(gen);
        }

        /// <summary>
        /// Static helper to create VisualizationDataMessage (for use in generators)
        /// </summary>
        private static VisualizationDataMessage CreateVisualizationMessageStatic(
            int rows, int cols, double centerX, double centerY, int seed)
        {
            var random = new System.Random(seed);
            
            // Create intensity matrix
            var intensityMatrix = new double[rows][];
            for (int i = 0; i < rows; i++)
            {
                intensityMatrix[i] = new double[cols];
                for (int j = 0; j < cols; j++)
                {
                    // Generate Gaussian-like intensity distribution
                    var dx = i - rows / 2.0;
                    var dy = j - cols / 2.0;
                    var distance = Math.Sqrt(dx * dx + dy * dy);
                    intensityMatrix[i][j] = Math.Exp(-distance * distance / (rows * cols / 16.0)) * random.NextDouble();
                }
            }

            return new VisualizationDataMessage
            {
                SpotCenterX = centerX,
                SpotCenterY = centerY,
                SpotIntensityDataJson = JsonSerializer.Serialize(intensityMatrix),
                EnergyDistribution3DJson = JsonSerializer.Serialize(intensityMatrix),
                Timestamp = DateTime.Now
            };
        }
    }

    #endregion
}
