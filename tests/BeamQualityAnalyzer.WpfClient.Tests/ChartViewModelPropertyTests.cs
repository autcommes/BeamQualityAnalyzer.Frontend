using System.Diagnostics;
using System.Globalization;
using BeamQualityAnalyzer.ApiClient;
using BeamQualityAnalyzer.Contracts.Dtos;
using BeamQualityAnalyzer.Contracts.Messages;
using BeamQualityAnalyzer.WpfClient.ViewModels;
using FsCheck;
using FsCheck.Xunit;
using Moq;

namespace BeamQualityAnalyzer.WpfClient.Tests;

/// <summary>
/// Property-based tests for ChartViewModel.
/// 
/// Property 6: Numeric formatting precision - Numbers should be formatted to 4 significant figures
/// Property 7: Parameter validation rejects invalid input - Invalid parameters should be rejected
/// Property 23: Parameter table refresh performance - Table should refresh within 100ms
/// 
/// Validates Requirements: 5.6, 6.5, 5.5
/// </summary>
public class ChartViewModelPropertyTests
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

    #region Property 6: Numeric Formatting Precision

    /// <summary>
    /// Property 6: All numeric values should be formatted to 4 significant figures
    /// Validates Requirement 5.6
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(Generators) })]
    public void NumericValues_ShouldBeFormattedTo4SignificantFigures(ValidCalculationMessage validMessage)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var viewModel = new ChartViewModel(mockApiClient.Object);

        // Act
        mockApiClient.Raise(
            x => x.CalculationCompleted += null,
            mockApiClient.Object,
            validMessage.Message);

        // Assert - Check all parameter values are formatted correctly
        var allValuesValid = true;
        var errorMessages = new List<string>();

        foreach (var param in viewModel.Parameters)
        {
            var values = new[] { param.GlobalValue, param.XValue, param.YValue };
            
            foreach (var value in values)
            {
                if (value != "-" && !string.IsNullOrEmpty(value))
                {
                    // Try to parse the value
                    if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var numValue))
                    {
                        // Check if it's formatted to 4 significant figures (G4 format)
                        var expectedFormat = numValue.ToString("G4", CultureInfo.InvariantCulture);
                        
                        if (value != expectedFormat)
                        {
                            allValuesValid = false;
                            errorMessages.Add($"Value '{value}' should be formatted as '{expectedFormat}' (4 sig figs)");
                        }
                    }
                }
            }
        }

        Assert.True(allValuesValid, $"All numeric values should be formatted to 4 significant figures. Errors: {string.Join(", ", errorMessages)}");
    }

    /// <summary>
    /// Property 6: Special values (NaN, Infinity) should be displayed as "-"
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = new[] { typeof(Generators) })]
    public void SpecialValues_ShouldBeDisplayedAsDash(SpecialValueMessage specialMessage)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var viewModel = new ChartViewModel(mockApiClient.Object);

        // Act
        mockApiClient.Raise(
            x => x.CalculationCompleted += null,
            mockApiClient.Object,
            specialMessage.Message);

        // Assert - Special values should be displayed as "-"
        var allSpecialValuesHandled = true;
        
        foreach (var param in viewModel.Parameters)
        {
            var values = new[] { param.GlobalValue, param.XValue, param.YValue };
            
            foreach (var value in values)
            {
                // Should either be "-" or a valid number
                if (value != "-")
                {
                    if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var numValue))
                    {
                        allSpecialValuesHandled = false;
                    }
                    else if (double.IsNaN(numValue) || double.IsInfinity(numValue))
                    {
                        allSpecialValuesHandled = false;
                    }
                }
            }
        }

        Assert.True(allSpecialValuesHandled, "Special values (NaN, Infinity) should be displayed as '-'");
    }

    #endregion

    #region Property 7: Parameter Validation

    /// <summary>
    /// Property 7: Negative magnification should be rejected
    /// Validates Requirement 6.5
    /// </summary>
    [Property(MaxTest = 50)]
    public void NegativeMagnification_ShouldBeRejected(NegativeInt negativeMagnification)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var viewModel = new ChartViewModel(mockApiClient.Object);

        // Act
        viewModel.Magnification = negativeMagnification.Get;

        // Assert
        var hasError = viewModel.HasValidationError;
        var errorMessageValid = !string.IsNullOrEmpty(viewModel.ValidationError);
        var commandDisabled = !viewModel.RecalculateCommand.CanExecute(null);

        Assert.True(hasError && errorMessageValid && commandDisabled,
            $"Negative magnification ({negativeMagnification.Get}) should be rejected with validation error");
    }

    /// <summary>
    /// Property 7: Zero magnification should be rejected
    /// </summary>
    [Fact]
    public void ZeroMagnification_ShouldBeRejected()
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var viewModel = new ChartViewModel(mockApiClient.Object);

        // Act
        viewModel.Magnification = 0;

        // Assert
        Assert.True(viewModel.HasValidationError);
        Assert.NotEmpty(viewModel.ValidationError!);
        Assert.False(viewModel.RecalculateCommand.CanExecute(null));
    }

    /// <summary>
    /// Property 7: Positive magnification should be accepted
    /// </summary>
    [Property(MaxTest = 50)]
    public void PositiveMagnification_ShouldBeAccepted(PositiveInt positiveMagnification)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var viewModel = new ChartViewModel(mockApiClient.Object);

        // Act
        viewModel.Magnification = positiveMagnification.Get;

        // Assert
        var noError = !viewModel.HasValidationError;
        var noErrorMessage = string.IsNullOrEmpty(viewModel.ValidationError);
        var commandEnabled = viewModel.RecalculateCommand.CanExecute(null);

        Assert.True(noError && noErrorMessage && commandEnabled,
            $"Positive magnification ({positiveMagnification.Get}) should be accepted");
    }

    /// <summary>
    /// Property 7: Validation error should prevent command execution
    /// </summary>
    [Property(MaxTest = 50)]
    public void ValidationError_ShouldPreventCommandExecution(NegativeInt invalidMagnification)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        mockApiClient.Setup(x => x.RecalculateAnalysisAsync(It.IsAny<AnalysisParametersDto>()))
            .ReturnsAsync(new CommandResult { Success = true });
        
        var viewModel = new ChartViewModel(mockApiClient.Object);

        // Act
        viewModel.Magnification = invalidMagnification.Get;
        var canExecute = viewModel.RecalculateCommand.CanExecute(null);

        // Assert - Command should be disabled when there's a validation error
        var commandIsDisabled = !canExecute;
        var hasValidationError = viewModel.HasValidationError;
        var errorMessageExists = !string.IsNullOrEmpty(viewModel.ValidationError);

        Assert.True(commandIsDisabled && hasValidationError && errorMessageExists,
            $"Validation error should disable RecalculateCommand (CanExecute={canExecute}, HasError={hasValidationError}, ErrorMsg='{viewModel.ValidationError}')");
    }

    /// <summary>
    /// Property 7: Clearing validation error should re-enable command
    /// </summary>
    [Property(MaxTest = 50)]
    public void ClearingValidationError_ShouldReEnableCommand(
        NegativeInt invalidMagnification,
        PositiveInt validMagnification)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var viewModel = new ChartViewModel(mockApiClient.Object);

        // Act - Set invalid value
        viewModel.Magnification = invalidMagnification.Get;
        var disabledWhenInvalid = !viewModel.RecalculateCommand.CanExecute(null);

        // Act - Set valid value
        viewModel.Magnification = validMagnification.Get;
        var enabledWhenValid = viewModel.RecalculateCommand.CanExecute(null);

        // Assert
        Assert.True(disabledWhenInvalid && enabledWhenValid,
            "Command should be disabled with invalid input and re-enabled with valid input");
    }

    #endregion

    #region Property 23: Parameter Table Refresh Performance

    /// <summary>
    /// Property 23: Parameter table should refresh within 100ms
    /// Validates Requirement 5.5
    /// </summary>
    [Property(MaxTest = 20, Arbitrary = new[] { typeof(Generators) })]
    public void ParameterTableRefresh_ShouldCompleteWithin100ms(ValidCalculationMessage validMessage)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var viewModel = new ChartViewModel(mockApiClient.Object);

        // Capture initial state
        var initialParameterValues = viewModel.Parameters
            .Select(p => $"{p.Name}:{p.GlobalValue}:{p.XValue}:{p.YValue}")
            .ToList();

        // Act - Measure refresh time
        var stopwatch = Stopwatch.StartNew();
        
        mockApiClient.Raise(
            x => x.CalculationCompleted += null,
            mockApiClient.Object,
            validMessage.Message);
        
        stopwatch.Stop();

        // Assert
        var refreshTime = stopwatch.ElapsedMilliseconds;
        var parametersUpdated = !viewModel.Parameters
            .Select(p => $"{p.Name}:{p.GlobalValue}:{p.XValue}:{p.YValue}")
            .SequenceEqual(initialParameterValues);

        var performanceOk = refreshTime <= 100;

        Assert.True(performanceOk && parametersUpdated,
            $"Parameter table refresh took {refreshTime}ms (should be ≤ 100ms)");
    }

    /// <summary>
    /// Property 23: Multiple rapid updates should all complete within 100ms each
    /// </summary>
    [Property(MaxTest = 10)]
    public void MultipleRapidUpdates_ShouldEachCompleteWithin100ms(PositiveInt updateCount)
    {
        // Arrange
        var mockApiClient = CreateMockApiClient();
        var viewModel = new ChartViewModel(mockApiClient.Object);
        
        var count = Math.Min(updateCount.Get % 10 + 1, 10); // 1-10 updates
        var allUpdatesFast = true;
        var updateTimes = new List<long>();

        // Act - Send multiple updates
        for (int i = 0; i < count; i++)
        {
            var message = new CalculationCompletedMessage
            {
                MSquaredX = 1.0 + i * 0.1,
                MSquaredY = 1.0 + i * 0.1,
                MSquaredGlobal = 1.0 + i * 0.1,
                PeakPositionX = 10.0 + i,
                PeakPositionY = 10.0 + i,
                BeamWaistPositionX = 5.0 + i,
                BeamWaistPositionY = 5.0 + i,
                BeamWaistDiameterX = 100.0 + i * 10,
                BeamWaistDiameterY = 100.0 + i * 10,
                Timestamp = DateTime.Now
            };

            var stopwatch = Stopwatch.StartNew();
            
            mockApiClient.Raise(
                x => x.CalculationCompleted += null,
                mockApiClient.Object,
                message);
            
            stopwatch.Stop();
            updateTimes.Add(stopwatch.ElapsedMilliseconds);

            if (stopwatch.ElapsedMilliseconds > 100)
            {
                allUpdatesFast = false;
            }
        }

        var maxTime = updateTimes.Max();
        var avgTime = updateTimes.Average();

        Assert.True(allUpdatesFast,
            $"{count} rapid updates: max={maxTime}ms, avg={avgTime:F1}ms (all should be ≤ 100ms)");
    }

    #endregion

    #region Helper Generators

    /// <summary>
    /// Wrapper class for valid calculation messages
    /// </summary>
    public class ValidCalculationMessage
    {
        public CalculationCompletedMessage Message { get; set; } = null!;
    }

    /// <summary>
    /// Wrapper class for special value messages
    /// </summary>
    public class SpecialValueMessage
    {
        public CalculationCompletedMessage Message { get; set; } = null!;
    }

    #endregion

    #region Generators

    /// <summary>
    /// FsCheck generators for ChartViewModel tests
    /// </summary>
    public static class Generators
    {
        /// <summary>
        /// Generate a valid CalculationCompletedMessage with realistic values
        /// </summary>
        public static Arbitrary<ValidCalculationMessage> ValidCalculationMessageArbitrary()
        {
            var gen = from mSquaredX in FsCheck.Gen.Choose(100, 500).Select(x => x / 100.0) // 1.0 - 5.0
                      from mSquaredY in FsCheck.Gen.Choose(100, 500).Select(y => y / 100.0)
                      from peakX in FsCheck.Gen.Choose(-100, 100).Select(x => x / 10.0) // -10.0 - 10.0
                      from peakY in FsCheck.Gen.Choose(-100, 100).Select(y => y / 10.0)
                      from waistPosX in FsCheck.Gen.Choose(0, 100).Select(x => x / 10.0) // 0.0 - 10.0
                      from waistPosY in FsCheck.Gen.Choose(0, 100).Select(y => y / 10.0)
                      from waistDiamX in FsCheck.Gen.Choose(50, 500).Select(x => (double)x) // 50 - 500 μm
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

        /// <summary>
        /// Generate a CalculationCompletedMessage with special values (NaN, Infinity)
        /// </summary>
        public static Arbitrary<SpecialValueMessage> SpecialValueMessageArbitrary()
        {
            var specialValues = new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity };
            
            var gen = from specialIndex in FsCheck.Gen.Choose(0, specialValues.Length - 1)
                      select new SpecialValueMessage
                      {
                          Message = new CalculationCompletedMessage
                          {
                              MSquaredX = specialValues[specialIndex],
                              MSquaredY = specialValues[(specialIndex + 1) % specialValues.Length],
                              MSquaredGlobal = specialValues[(specialIndex + 2) % specialValues.Length],
                              PeakPositionX = specialValues[specialIndex],
                              PeakPositionY = specialValues[(specialIndex + 1) % specialValues.Length],
                              BeamWaistPositionX = specialValues[(specialIndex + 2) % specialValues.Length],
                              BeamWaistPositionY = specialValues[specialIndex],
                              BeamWaistDiameterX = specialValues[(specialIndex + 1) % specialValues.Length],
                              BeamWaistDiameterY = specialValues[(specialIndex + 2) % specialValues.Length],
                              Timestamp = DateTime.Now
                          }
                      };
            
            return FsCheck.Arb.From(gen);
        }
    }

    #endregion
}
