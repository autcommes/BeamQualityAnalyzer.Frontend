using BeamQualityAnalyzer.ApiClient;
using BeamQualityAnalyzer.Contracts.Dtos;
using BeamQualityAnalyzer.Contracts.Messages;
using BeamQualityAnalyzer.WpfClient.ViewModels;
using Moq;
using Xunit;

namespace BeamQualityAnalyzer.WpfClient.Tests;

/// <summary>
/// ViewModel 集成测试
/// 验证 ViewModel 层与 ApiClient 的集�?
/// </summary>
public class ViewModelIntegrationTests
{
    /// <summary>
    /// 测试 MainViewModel 正确创建所有子 ViewModel
    /// </summary>
    [Fact]
    public void MainViewModel_Should_Create_All_Child_ViewModels()
    {
        // Arrange
        var mockApiClient = new Mock<IBeamAnalyzerApiClient>();
        
        // Act
        var mockSettingsService = new Mock<BeamQualityAnalyzer.WpfClient.Services.ISettingsService>();
        mockSettingsService.Setup(x => x.LoadSettingsAsync()).ReturnsAsync(new BeamQualityAnalyzer.WpfClient.Models.AppSettings());
        var mainViewModel = new MainViewModel(mockApiClient.Object, mockSettingsService.Object);
        
        // Assert
        Assert.NotNull(mainViewModel.ChartViewModel);
        Assert.NotNull(mainViewModel.VisualizationViewModel);
        Assert.NotNull(mainViewModel.StatusBarViewModel);
    }
    
    /// <summary>
    /// 测试 MainViewModel 所有命令都已初始化
    /// </summary>
    [Fact]
    public void MainViewModel_Should_Initialize_All_Commands()
    {
        // Arrange
        var mockApiClient = new Mock<IBeamAnalyzerApiClient>();
        
        // Act
        var mockSettingsService = new Mock<BeamQualityAnalyzer.WpfClient.Services.ISettingsService>();
        mockSettingsService.Setup(x => x.LoadSettingsAsync()).ReturnsAsync(new BeamQualityAnalyzer.WpfClient.Models.AppSettings());
        var mainViewModel = new MainViewModel(mockApiClient.Object, mockSettingsService.Object);
        
        // Assert
        Assert.NotNull(mainViewModel.StartAcquisitionCommand);
        Assert.NotNull(mainViewModel.EmergencyStopCommand);
        Assert.NotNull(mainViewModel.ResetMotorCommand);
        Assert.NotNull(mainViewModel.TakeScreenshotCommand);
        Assert.NotNull(mainViewModel.ExportReportCommand);
        Assert.NotNull(mainViewModel.SaveToDatabaseCommand);
        Assert.NotNull(mainViewModel.StartAutoTestCommand);
        Assert.NotNull(mainViewModel.OpenSettingsCommand);
    }
    
    /// <summary>
    /// 测试命令在未连接时不可执�?
    /// </summary>
    [Fact]
    public void Commands_Should_Be_Disabled_When_Not_Connected()
    {
        // Arrange
        var mockApiClient = new Mock<IBeamAnalyzerApiClient>();
        mockApiClient.Setup(x => x.IsConnected).Returns(false);
        
        var mockSettingsService = new Mock<BeamQualityAnalyzer.WpfClient.Services.ISettingsService>();
        mockSettingsService.Setup(x => x.LoadSettingsAsync()).ReturnsAsync(new BeamQualityAnalyzer.WpfClient.Models.AppSettings());
        var mainViewModel = new MainViewModel(mockApiClient.Object, mockSettingsService.Object);
        
        // Act & Assert
        Assert.False(mainViewModel.StartAcquisitionCommand.CanExecute(null));
        Assert.False(mainViewModel.EmergencyStopCommand.CanExecute(null));
        Assert.False(mainViewModel.ResetMotorCommand.CanExecute(null));
        Assert.False(mainViewModel.TakeScreenshotCommand.CanExecute(null));
        Assert.False(mainViewModel.ExportReportCommand.CanExecute(null));
        Assert.False(mainViewModel.SaveToDatabaseCommand.CanExecute(null));
        Assert.False(mainViewModel.StartAutoTestCommand.CanExecute(null));
    }
    
    /// <summary>
    /// 测试连接状态变化时命令状态更�?
    /// </summary>
    [Fact]
    public void Commands_Should_Update_When_Connection_State_Changes()
    {
        // Arrange
        var mockApiClient = new Mock<IBeamAnalyzerApiClient>();
        mockApiClient.Setup(x => x.IsConnected).Returns(false);
        
        var mockSettingsService = new Mock<BeamQualityAnalyzer.WpfClient.Services.ISettingsService>();
        mockSettingsService.Setup(x => x.LoadSettingsAsync()).ReturnsAsync(new BeamQualityAnalyzer.WpfClient.Models.AppSettings());
        var mainViewModel = new MainViewModel(mockApiClient.Object, mockSettingsService.Object);
        
        // Act - 模拟连接成功
        mockApiClient.Setup(x => x.IsConnected).Returns(true);
        mockApiClient.Raise(
            x => x.ConnectionStateChanged += null,
            mockApiClient.Object,
            new ConnectionStateChangedEventArgs(true));
        
        // Assert
        Assert.True(mainViewModel.IsConnected);
        Assert.True(mainViewModel.StartAcquisitionCommand.CanExecute(null));
        Assert.True(mainViewModel.ResetMotorCommand.CanExecute(null));
    }
    
    /// <summary>
    /// 测试 ChartViewModel 订阅了正确的事件
    /// </summary>
    [Fact]
    public void ChartViewModel_Should_Subscribe_To_ApiClient_Events()
    {
        // Arrange
        var mockApiClient = new Mock<IBeamAnalyzerApiClient>();
        
        // Act
        var chartViewModel = new ChartViewModel(mockApiClient.Object);
        
        // Assert - 验证事件订阅
        mockApiClient.VerifyAdd(x => x.RawDataReceived += It.IsAny<EventHandler<RawDataReceivedMessage>>(), Times.Once);
        mockApiClient.VerifyAdd(x => x.CalculationCompleted += It.IsAny<EventHandler<CalculationCompletedMessage>>(), Times.Once);
    }
    
    /// <summary>
    /// 测试 StatusBarViewModel 订阅了正确的事件
    /// </summary>
    [Fact]
    public void StatusBarViewModel_Should_Subscribe_To_ApiClient_Events()
    {
        // Arrange
        var mockApiClient = new Mock<IBeamAnalyzerApiClient>();
        
        // Act
        var statusBarViewModel = new StatusBarViewModel(mockApiClient.Object);
        
        // Assert - 验证事件订阅
        mockApiClient.VerifyAdd(x => x.ConnectionStateChanged += It.IsAny<EventHandler<ConnectionStateChangedEventArgs>>(), Times.Once);
        mockApiClient.VerifyAdd(x => x.AcquisitionStatusChanged += It.IsAny<EventHandler<AcquisitionStatusMessage>>(), Times.Once);
        mockApiClient.VerifyAdd(x => x.DeviceStatusChanged += It.IsAny<EventHandler<DeviceStatusMessage>>(), Times.Once);
        mockApiClient.VerifyAdd(x => x.ErrorOccurred += It.IsAny<EventHandler<ErrorMessage>>(), Times.Once);
        mockApiClient.VerifyAdd(x => x.ProgressUpdated += It.IsAny<EventHandler<ProgressMessage>>(), Times.Once);
        mockApiClient.VerifyAdd(x => x.CalculationCompleted += It.IsAny<EventHandler<CalculationCompletedMessage>>(), Times.Once);
    }
    
    /// <summary>
    /// 测试 VisualizationViewModel 订阅了正确的事件
    /// </summary>
    [Fact]
    public void VisualizationViewModel_Should_Subscribe_To_ApiClient_Events()
    {
        // Arrange
        var mockApiClient = new Mock<IBeamAnalyzerApiClient>();
        
        // Act
        var visualizationViewModel = new VisualizationViewModel(mockApiClient.Object);
        
        // Assert - 验证事件订阅
        mockApiClient.VerifyAdd(x => x.VisualizationDataUpdated += It.IsAny<EventHandler<VisualizationDataMessage>>(), Times.Once);
        mockApiClient.VerifyAdd(x => x.RawDataReceived += It.IsAny<EventHandler<RawDataReceivedMessage>>(), Times.Once);
    }
    
    /// <summary>
    /// 测试 MainViewModel 正确取消订阅所有事�?
    /// </summary>
    [Fact]
    public void MainViewModel_Should_Unsubscribe_All_Events()
    {
        // Arrange
        var mockApiClient = new Mock<IBeamAnalyzerApiClient>();
        var mockSettingsService = new Mock<BeamQualityAnalyzer.WpfClient.Services.ISettingsService>();
        mockSettingsService.Setup(x => x.LoadSettingsAsync()).ReturnsAsync(new BeamQualityAnalyzer.WpfClient.Models.AppSettings());
        var mainViewModel = new MainViewModel(mockApiClient.Object, mockSettingsService.Object);
        
        // Act
        mainViewModel.UnsubscribeFromEvents();
        
        // Assert - 验证事件取消订阅（MainViewModel 自己订阅的事件）
        // 注意：子 ViewModel 也会取消订阅，所以某些事件会被取消订阅多�?
        mockApiClient.VerifyRemove(x => x.ConnectionStateChanged -= It.IsAny<EventHandler<ConnectionStateChangedEventArgs>>(), Times.AtLeastOnce);
        mockApiClient.VerifyRemove(x => x.AcquisitionStatusChanged -= It.IsAny<EventHandler<AcquisitionStatusMessage>>(), Times.AtLeastOnce);
        mockApiClient.VerifyRemove(x => x.CalculationCompleted -= It.IsAny<EventHandler<CalculationCompletedMessage>>(), Times.AtLeastOnce);
    }
    
    /// <summary>
    /// 测试所�?ViewModel 都继承自 ViewModelBase
    /// </summary>
    [Fact]
    public void All_ViewModels_Should_Inherit_From_ViewModelBase()
    {
        // Arrange
        var mockApiClient = new Mock<IBeamAnalyzerApiClient>();
        
        // Act
        var mockSettingsService = new Mock<BeamQualityAnalyzer.WpfClient.Services.ISettingsService>();
        mockSettingsService.Setup(x => x.LoadSettingsAsync()).ReturnsAsync(new BeamQualityAnalyzer.WpfClient.Models.AppSettings());
        var mainViewModel = new MainViewModel(mockApiClient.Object, mockSettingsService.Object);
        var chartViewModel = new ChartViewModel(mockApiClient.Object);
        var statusBarViewModel = new StatusBarViewModel(mockApiClient.Object);
        var visualizationViewModel = new VisualizationViewModel(mockApiClient.Object);
        
        // Assert
        Assert.IsAssignableFrom<ViewModelBase>(mainViewModel);
        Assert.IsAssignableFrom<ViewModelBase>(chartViewModel);
        Assert.IsAssignableFrom<ViewModelBase>(statusBarViewModel);
        Assert.IsAssignableFrom<ViewModelBase>(visualizationViewModel);
    }
    
    /// <summary>
    /// 测试 ChartViewModel �?RecalculateCommand 调用 ApiClient
    /// </summary>
    [Fact]
    public async Task ChartViewModel_RecalculateCommand_Should_Call_ApiClient()
    {
        // Arrange
        var mockApiClient = new Mock<IBeamAnalyzerApiClient>();
        mockApiClient
            .Setup(x => x.RecalculateAnalysisAsync(It.IsAny<AnalysisParametersDto>()))
            .ReturnsAsync(new CommandResult { Success = true });
        
        var chartViewModel = new ChartViewModel(mockApiClient.Object);
        chartViewModel.Magnification = 2.0;
        
        // Act
        await chartViewModel.RecalculateCommand.ExecuteAsync(null);
        
        // Assert
        mockApiClient.Verify(
            x => x.RecalculateAnalysisAsync(It.Is<AnalysisParametersDto>(p => p.Magnification == 2.0)),
            Times.Once);
    }
}
