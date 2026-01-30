using Xunit;
using BeamQualityAnalyzer.WpfClient;
using BeamQualityAnalyzer.WpfClient.ViewModels;
using BeamQualityAnalyzer.ApiClient;
using BeamQualityAnalyzer.WpfClient.Services;
using System.Windows.Media;
using System.Windows;
using Moq;

namespace BeamQualityAnalyzer.WpfClient.Tests;

/// <summary>
/// UI完整性验证测试
/// 验证任务 22：UI完整性验证检查点
/// </summary>
public class UIIntegrityTests
{
    [Fact]
    public void StatusLevelToColorConverter_ShouldReturnCorrectColors()
    {
        // Arrange
        var converter = new StatusLevelToColorConverter();
        
#pragma warning disable CS8625 // 测试中故意传递null参数
        // Act & Assert - Normal状态应返回青绿色 #4EC9B0
        var normalColor = converter.Convert(StatusLevel.Normal, typeof(Brush), null, null) as SolidColorBrush;
#pragma warning restore CS8625
        Assert.NotNull(normalColor);
        Assert.Equal("#FF4EC9B0", normalColor.Color.ToString());
        
#pragma warning disable CS8625 // 测试中故意传递null参数
        // Act & Assert - Warning状态应返回黄色 #D7BA7D
        var warningColor = converter.Convert(StatusLevel.Warning, typeof(Brush), null, null) as SolidColorBrush;
#pragma warning restore CS8625
        Assert.NotNull(warningColor);
        Assert.Equal("#FFD7BA7D", warningColor.Color.ToString());
        
#pragma warning disable CS8625 // 测试中故意传递null参数
        // Act & Assert - Error状态应返回红色 #F44747
        var errorColor = converter.Convert(StatusLevel.Error, typeof(Brush), null, null) as SolidColorBrush;
#pragma warning restore CS8625
        Assert.NotNull(errorColor);
        Assert.Equal("#FFF44747", errorColor.Color.ToString());
    }
    
    [Fact]
    public void BoolToColorConverter_ShouldReturnCorrectColors()
    {
        // Arrange
        var converter = new BoolToColorConverter();
        
#pragma warning disable CS8625 // 测试中故意传递null参数
        // Act & Assert - 已连接应返回青绿色 #4EC9B0
        var connectedColor = converter.Convert(true, typeof(Brush), null, null) as SolidColorBrush;
#pragma warning restore CS8625
        Assert.NotNull(connectedColor);
        Assert.Equal("#FF4EC9B0", connectedColor.Color.ToString());
        
#pragma warning disable CS8625 // 测试中故意传递null参数
        // Act & Assert - 未连接应返回红色 #F44747
        var disconnectedColor = converter.Convert(false, typeof(Brush), null, null) as SolidColorBrush;
#pragma warning restore CS8625
        Assert.NotNull(disconnectedColor);
        Assert.Equal("#FFF44747", disconnectedColor.Color.ToString());
    }
    
    [Fact]
    public void BoolToConnectionTextConverter_ShouldReturnCorrectText()
    {
        // Arrange
        var converter = new BoolToConnectionTextConverter();
        
#pragma warning disable CS8625 // 测试中故意传递null参数
        // Act & Assert - 已连接
        var connectedText = converter.Convert(true, typeof(string), null, null);
#pragma warning restore CS8625
        Assert.Equal("已连接", connectedText);
        
#pragma warning disable CS8625 // 测试中故意传递null参数
        // Act & Assert - 未连接
        var disconnectedText = converter.Convert(false, typeof(string), null, null);
#pragma warning restore CS8625
        Assert.Equal("未连接", disconnectedText);
    }
    
    [Fact]
    public void BoolToVisibilityConverter_ShouldReturnCorrectVisibility()
    {
        // Arrange
        var converter = new BoolToVisibilityConverter();
        
#pragma warning disable CS8625 // 测试中故意传递null参数
        // Act & Assert - true应返回Visible
        var visible = converter.Convert(true, typeof(Visibility), null, null);
#pragma warning restore CS8625
        Assert.Equal(Visibility.Visible, visible);
        
#pragma warning disable CS8625 // 测试中故意传递null参数
        // Act & Assert - false应返回Collapsed
        var collapsed = converter.Convert(false, typeof(Visibility), null, null);
#pragma warning restore CS8625
        Assert.Equal(Visibility.Collapsed, collapsed);
    }
    
    [Fact]
    public void BoolToVisibilityConverter_ConvertBack_ShouldWork()
    {
        // Arrange
        var converter = new BoolToVisibilityConverter();
        
#pragma warning disable CS8625 // 测试中故意传递null参数
        // Act & Assert - Visible应返回true
        var trueValue = converter.ConvertBack(Visibility.Visible, typeof(bool), null, null);
#pragma warning restore CS8625
        Assert.Equal(true, trueValue);
        
#pragma warning disable CS8625 // 测试中故意传递null参数
        // Act & Assert - Collapsed应返回false
        var falseValue = converter.ConvertBack(Visibility.Collapsed, typeof(bool), null, null);
#pragma warning restore CS8625
        Assert.Equal(false, falseValue);
    }
    
    [Fact]
    public void MainViewModel_ShouldHaveAllRequiredProperties()
    {
        // Arrange
        var mockApiClient = new Mock<IBeamAnalyzerApiClient>();
        var mockSettingsService = new Mock<ISettingsService>();
        
        // Act
        var viewModel = new MainViewModel(mockApiClient.Object, mockSettingsService.Object);
        
        // Assert - 验证所有必需的属性存在
        Assert.NotNull(viewModel.ChartViewModel);
        Assert.NotNull(viewModel.VisualizationViewModel);
        Assert.NotNull(viewModel.StatusBarViewModel);
        
        // 验证命令存在
        Assert.NotNull(viewModel.StartAcquisitionCommand);
        Assert.NotNull(viewModel.EmergencyStopCommand);
        Assert.NotNull(viewModel.ResetMotorCommand);
        Assert.NotNull(viewModel.TakeScreenshotCommand);
        Assert.NotNull(viewModel.ExportReportCommand);
        Assert.NotNull(viewModel.SaveToDatabaseCommand);
        Assert.NotNull(viewModel.StartAutoTestCommand);
        Assert.NotNull(viewModel.OpenSettingsCommand);
    }
    
    [Fact]
    public void ChartViewModel_ShouldHaveAllRequiredProperties()
    {
        // Arrange
        var mockApiClient = new Mock<IBeamAnalyzerApiClient>();
        
        // Act
        var viewModel = new ChartViewModel(mockApiClient.Object, null);
        
        // Assert - 验证数据集合存在
        Assert.NotNull(viewModel.RawDataX);
        Assert.NotNull(viewModel.RawDataY);
        Assert.NotNull(viewModel.FittedCurveX);
        Assert.NotNull(viewModel.FittedCurveY);
        Assert.NotNull(viewModel.Parameters);
        
        // 验证命令存在
        Assert.NotNull(viewModel.RecalculateCommand);
        
        // 验证初始值
        Assert.Equal(0, viewModel.SelectedTabIndex);
        Assert.Equal(1.0, viewModel.Magnification);
    }
    
    [Fact]
    public void StatusBarViewModel_ShouldHaveCorrectInitialState()
    {
        // Arrange
        var mockApiClient = new Mock<IBeamAnalyzerApiClient>();
        
        // Act
        var viewModel = new StatusBarViewModel(mockApiClient.Object);
        
        // Assert - 验证初始状态
        Assert.Equal("就绪", viewModel.StatusText);
        Assert.Equal(StatusLevel.Normal, viewModel.StatusLevel);
        Assert.Equal(0, viewModel.ProgressValue);
        Assert.False(viewModel.IsProgressVisible);
    }
    
    [Fact]
    public void VisualizationViewModel_ShouldHaveAllRequiredProperties()
    {
        // Arrange
        var mockApiClient = new Mock<IBeamAnalyzerApiClient>();
        
        // Act
        var viewModel = new VisualizationViewModel(mockApiClient.Object, null);
        
        // Assert - 验证属性存在（初始值可以为null）
        // SpotIntensityData 和 EnergyDistributionData 在接收到数据前为 null
        Assert.NotNull(viewModel);
        Assert.Equal(0, viewModel.SpotCenter.X);
        Assert.Equal(0, viewModel.SpotCenter.Y);
        Assert.Equal("Viridis", viewModel.SelectedColorMap);
        Assert.Equal(1.0, viewModel.ZoomLevel);
    }
}
