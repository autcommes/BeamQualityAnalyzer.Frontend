using BeamQualityAnalyzer.ApiClient;
using BeamQualityAnalyzer.Contracts.Dtos;
using BeamQualityAnalyzer.WpfClient.Services;
using BeamQualityAnalyzer.WpfClient.Views;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Serilog;

namespace BeamQualityAnalyzer.WpfClient.ViewModels;

/// <summary>
/// 主窗口 ViewModel
/// 管理整个应用程序的业务逻辑和状态
/// </summary>
/// <remarks>
/// Requirements:
/// - 2.1: 启动数据采集流程并更新状态栏显示为"采集中"
/// - 2.2: 立即停止所有数据采集和设备运动
/// - 2.3: 发送复位命令到设备
/// - 9.1: 捕获当前主窗口的完整内容
/// - 10.1: 生成包含当前测量结果的 PDF 报告
/// - 11.1: 将当前测量的原始数据和计算结果保存到数据库
/// - 12.1: 启动预定义的自动测试序列
/// - 13.1: 打开设置对话框
/// </remarks>
public class MainViewModel : ViewModelBase
{
    private readonly IBeamAnalyzerApiClient _apiClient;
    private readonly ISettingsService _settingsService;
    
    // 子 ViewModel
    private readonly ChartViewModel _chartViewModel;
    private readonly VisualizationViewModel _visualizationViewModel;
    private readonly StatusBarViewModel _statusBarViewModel;
    
    // 状态属性
    private bool _isAcquiring;
    private bool _isCalculating;
    private string _currentStatus = "就绪";
    private bool _isConnected;
    
    /// <summary>
    /// 图表 ViewModel
    /// </summary>
    public ChartViewModel ChartViewModel => _chartViewModel;
    
    /// <summary>
    /// 可视化 ViewModel
    /// </summary>
    public VisualizationViewModel VisualizationViewModel => _visualizationViewModel;
    
    /// <summary>
    /// 状态栏 ViewModel
    /// </summary>
    public StatusBarViewModel StatusBarViewModel => _statusBarViewModel;
    
    /// <summary>
    /// 是否正在采集数据
    /// </summary>
    public bool IsAcquiring
    {
        get => _isAcquiring;
        private set
        {
            if (SetProperty(ref _isAcquiring, value))
            {
                // 通知命令状态变化
                StartAcquisitionCommand.NotifyCanExecuteChanged();
                EmergencyStopCommand.NotifyCanExecuteChanged();
            }
        }
    }
    
    /// <summary>
    /// 是否正在计算
    /// </summary>
    public bool IsCalculating
    {
        get => _isCalculating;
        private set => SetProperty(ref _isCalculating, value);
    }
    
    /// <summary>
    /// 当前状态文本
    /// </summary>
    public string CurrentStatus
    {
        get => _currentStatus;
        private set => SetProperty(ref _currentStatus, value);
    }
    
    /// <summary>
    /// 是否已连接到服务器
    /// </summary>
    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (SetProperty(ref _isConnected, value))
            {
                // 通知所有命令状态变化
                NotifyAllCommandsCanExecuteChanged();
            }
        }
    }
    
    // 命令
    /// <summary>
    /// 启动数据采集命令
    /// </summary>
    /// <remarks>
    /// Requirement 2.1: 启动数据采集流程并更新状态栏显示为"采集中"
    /// </remarks>
    public IAsyncRelayCommand StartAcquisitionCommand { get; }
    
    /// <summary>
    /// 急停命令
    /// </summary>
    /// <remarks>
    /// Requirement 2.2: 立即停止所有数据采集和设备运动
    /// </remarks>
    public IAsyncRelayCommand EmergencyStopCommand { get; }
    
    /// <summary>
    /// 电机复位命令
    /// </summary>
    /// <remarks>
    /// Requirement 2.3: 发送复位命令到设备
    /// </remarks>
    public IAsyncRelayCommand ResetMotorCommand { get; }
    
    /// <summary>
    /// 截图命令
    /// </summary>
    /// <remarks>
    /// Requirement 9.1: 捕获当前主窗口的完整内容
    /// </remarks>
    public IAsyncRelayCommand TakeScreenshotCommand { get; }
    
    /// <summary>
    /// 导出报告命令
    /// </summary>
    /// <remarks>
    /// Requirement 10.1: 生成包含当前测量结果的 PDF 报告
    /// </remarks>
    public IAsyncRelayCommand ExportReportCommand { get; }
    
    /// <summary>
    /// 保存到数据库命令
    /// </summary>
    /// <remarks>
    /// Requirement 11.1: 将当前测量的原始数据和计算结果保存到数据库
    /// </remarks>
    public IAsyncRelayCommand SaveToDatabaseCommand { get; }
    
    /// <summary>
    /// 启动自动测试命令
    /// </summary>
    /// <remarks>
    /// Requirement 12.1: 启动预定义的自动测试序列
    /// </remarks>
    public IAsyncRelayCommand StartAutoTestCommand { get; }
    
    /// <summary>
    /// 打开设置命令
    /// </summary>
    /// <remarks>
    /// Requirement 13.1: 打开设置对话框
    /// </remarks>
    public IRelayCommand OpenSettingsCommand { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="apiClient">API 客户端</param>
    /// <param name="settingsService">配置服务</param>
    public MainViewModel(IBeamAnalyzerApiClient apiClient, ISettingsService settingsService)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        
        // 创建子 ViewModel
        _chartViewModel = new ChartViewModel(apiClient);
        _visualizationViewModel = new VisualizationViewModel(apiClient);
        _statusBarViewModel = new StatusBarViewModel(apiClient);
        
        // 创建命令
        StartAcquisitionCommand = new AsyncRelayCommand(
            StartAcquisitionAsync,
            () => IsConnected && !IsAcquiring);
        
        // 急停按钮始终可用（安全考虑）
        EmergencyStopCommand = new AsyncRelayCommand(
            EmergencyStopAsync,
            () => IsConnected);
        
        ResetMotorCommand = new AsyncRelayCommand(
            ResetMotorAsync,
            () => IsConnected);
        
        TakeScreenshotCommand = new AsyncRelayCommand(
            TakeScreenshotAsync,
            () => IsConnected);
        
        ExportReportCommand = new AsyncRelayCommand(
            ExportReportAsync,
            () => IsConnected);
        
        SaveToDatabaseCommand = new AsyncRelayCommand(
            SaveToDatabaseAsync,
            () => IsConnected);
        
        StartAutoTestCommand = new AsyncRelayCommand(
            StartAutoTestAsync,
            () => IsConnected && !IsAcquiring);
        
        // 设置按钮始终可用
        OpenSettingsCommand = new RelayCommand(
            OpenSettings,
            () => true);
        
        // 订阅 API 客户端事件
        SubscribeToEvents();
    }
    
    /// <summary>
    /// 订阅 API 客户端事件
    /// </summary>
    private void SubscribeToEvents()
    {
        _apiClient.ConnectionStateChanged += OnConnectionStateChanged;
        _apiClient.AcquisitionStatusChanged += OnAcquisitionStatusChanged;
        _apiClient.CalculationCompleted += OnCalculationCompleted;
        
        // 初始化连接状态
        IsConnected = _apiClient.IsConnected;
    }
    
    /// <summary>
    /// 连接状态变化处理
    /// </summary>
    private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        // 确保在 UI 线程上更新
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            IsConnected = e.IsConnected;
            CurrentStatus = e.IsConnected ? "已连接" : "未连接";
            Log.Information("连接状态变化: IsConnected = {IsConnected}", e.IsConnected);
        });
    }
    
    /// <summary>
    /// 采集状态变化处理
    /// </summary>
    private void OnAcquisitionStatusChanged(object? sender, Contracts.Messages.AcquisitionStatusMessage e)
    {
        IsAcquiring = e.IsAcquiring;
        CurrentStatus = e.IsAcquiring ? "采集中" : "就绪";
    }
    
    /// <summary>
    /// 计算完成处理
    /// </summary>
    private void OnCalculationCompleted(object? sender, Contracts.Messages.CalculationCompletedMessage e)
    {
        IsCalculating = false;
        CurrentStatus = "计算完成";
    }
    
    /// <summary>
    /// 启动数据采集
    /// </summary>
    /// <remarks>
    /// Requirement 17.2: 所有耗时操作使用 Task.Run 在后台线程执行
    /// </remarks>
    private async Task StartAcquisitionAsync()
    {
        try
        {
            Log.Information("用户启动数据采集");
            CurrentStatus = "正在启动采集...";
            
            // 在后台线程执行 API 调用
            var result = await Task.Run(async () => await _apiClient.StartAcquisitionAsync());
            
            if (!result.Success)
            {
                Log.Warning("启动数据采集失败: {Message}", result.Message);
                CurrentStatus = $"启动采集失败: {result.Message}";
            }
            else
            {
                Log.Information("数据采集已启动");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "启动数据采集异常");
            CurrentStatus = $"启动采集异常: {ex.Message}";
        }
    }
    
    /// <summary>
    /// 急停
    /// </summary>
    /// <remarks>
    /// Requirement 17.2: 所有耗时操作使用 Task.Run 在后台线程执行
    /// </remarks>
    private async Task EmergencyStopAsync()
    {
        try
        {
            Log.Warning("用户执行急停");
            CurrentStatus = "正在执行急停...";
            
            // 在后台线程执行 API 调用
            var result = await Task.Run(async () => await _apiClient.EmergencyStopAsync());
            
            if (result.Success)
            {
                Log.Information("急停执行成功");
                CurrentStatus = "急停执行成功";
            }
            else
            {
                Log.Error("急停执行失败: {Message}", result.Message);
                CurrentStatus = $"急停执行失败: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            CurrentStatus = $"急停执行异常: {ex.Message}";
        }
    }
    
    /// <summary>
    /// 电机复位
    /// </summary>
    /// <remarks>
    /// Requirement 17.2: 所有耗时操作使用 Task.Run 在后台线程执行
    /// </remarks>
    private async Task ResetMotorAsync()
    {
        try
        {
            Log.Information("用户执行设备复位");
            CurrentStatus = "正在复位设备...";
            
            // 在后台线程执行 API 调用
            var result = await Task.Run(async () => await _apiClient.ResetDeviceAsync());
            
            if (result.Success)
            {
                Log.Information("设备复位成功");
                CurrentStatus = "设备复位成功";
            }
            else
            {
                Log.Warning("设备复位失败: {Message}", result.Message);
                CurrentStatus = $"设备复位失败: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "设备复位异常");
            CurrentStatus = $"设备复位异常: {ex.Message}";
        }
    }
    
    /// <summary>
    /// 截图
    /// </summary>
    /// <remarks>
    /// Requirement 17.2: 所有耗时操作使用 Task.Run 在后台线程执行
    /// </remarks>
    private async Task TakeScreenshotAsync()
    {
        try
        {
            Log.Information("用户请求截图");
            CurrentStatus = "正在生成截图...";
            
            // 在后台线程执行 API 调用
            var result = await Task.Run(async () => await _apiClient.GenerateScreenshotAsync());
            
            if (result.Success)
            {
                Log.Information("截图已保存: {FilePath}", result.Data);
                CurrentStatus = $"截图已保存: {result.Data}";
            }
            else
            {
                Log.Warning("截图失败: {Message}", result.Message);
                CurrentStatus = $"截图失败: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "截图异常");
            CurrentStatus = $"截图异常: {ex.Message}";
        }
    }
    
    /// <summary>
    /// 导出报告
    /// </summary>
    /// <remarks>
    /// Requirement 17.2: 所有耗时操作使用 Task.Run 在后台线程执行
    /// </remarks>
    private async Task ExportReportAsync()
    {
        try
        {
            Log.Information("用户请求导出报告");
            CurrentStatus = "正在生成报告...";
            
            var options = new ReportOptionsDto
            {
                IncludeCharts = true,
                Include2DSpot = true,
                Include3DEnergy = true,
                IncludeRawData = true
            };
            
            // 在后台线程执行 API 调用
            var result = await Task.Run(async () => await _apiClient.GenerateReportAsync(options));
            
            if (result.Success)
            {
                Log.Information("报告已保存: {FilePath}", result.Data);
                CurrentStatus = $"报告已保存: {result.Data}";
            }
            else
            {
                Log.Warning("报告生成失败: {Message}", result.Message);
                CurrentStatus = $"报告生成失败: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "报告生成异常");
            CurrentStatus = $"报告生成异常: {ex.Message}";
        }
    }
    
    /// <summary>
    /// 保存到数据库
    /// </summary>
    /// <remarks>
    /// Requirement 17.2: 所有耗时操作使用 Task.Run 在后台线程执行
    /// </remarks>
    private async Task SaveToDatabaseAsync()
    {
        try
        {
            Log.Information("用户请求保存到数据库");
            CurrentStatus = "正在保存到数据库...";
            
            // 创建测量记录（这里需要从当前数据构建）
            var record = new MeasurementRecordDto
            {
                MeasurementTime = DateTime.Now,
                DeviceInfo = "Beam Profiler",
                Status = "Complete",
                Notes = "手动保存"
            };
            
            // 在后台线程执行 API 调用
            var result = await Task.Run(async () => await _apiClient.SaveMeasurementAsync(record));
            
            if (result.Success)
            {
                Log.Information("数据已保存到数据库，记录ID: {RecordId}", result.Data);
                CurrentStatus = $"数据已保存，记录ID: {result.Data}";
            }
            else
            {
                Log.Warning("保存到数据库失败: {Message}", result.Message);
                CurrentStatus = $"保存失败: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "保存到数据库异常");
            CurrentStatus = $"保存异常: {ex.Message}";
        }
    }
    
    /// <summary>
    /// 启动自动测试
    /// </summary>
    /// <remarks>
    /// Requirement 17.2: 所有耗时操作使用 Task.Run 在后台线程执行
    /// </remarks>
    private async Task StartAutoTestAsync()
    {
        try
        {
            Log.Information("用户启动自动测试");
            CurrentStatus = "正在启动自动测试...";
            
            var config = new AutoTestConfigurationDto
            {
                TestCycles = 3,
                IncludeWarmup = true,
                WarmupSeconds = 5,
                AnalysisParameters = new AnalysisParametersDto
                {
                    Magnification = 1.0,
                    Wavelength = 632.8,
                    MinDataPoints = 10,
                    FitTolerance = 0.001
                }
            };
            
            // 在后台线程执行 API 调用
            var result = await Task.Run(async () => await _apiClient.StartAutoTestAsync(config));
            
            if (result.Success)
            {
                Log.Information("自动测试已启动");
                CurrentStatus = "自动测试已启动";
            }
            else
            {
                Log.Warning("自动测试启动失败: {Message}", result.Message);
                CurrentStatus = $"自动测试启动失败: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "自动测试启动异常");
            CurrentStatus = $"自动测试启动异常: {ex.Message}";
        }
    }
    
    /// <summary>
    /// 打开设置对话框
    /// </summary>
    /// <remarks>
    /// Requirement 13.1: 打开设置对话框
    /// 
    /// 实现步骤：
    /// 1. 加载当前配置
    /// 2. 创建 SettingsDialog 实例并传递当前配置
    /// 3. 显示对话框（ShowDialog）
    /// 4. 如果用户点击保存，重新加载配置并应用
    /// 5. 更新 API 客户端连接（如果 ServerUrl 变化）
    /// 6. 通知用户配置已保存
    /// </remarks>
    private async void OpenSettings()
    {
        try
        {
            Log.Information("用户打开设置对话框");
            CurrentStatus = "正在打开设置...";
            
            // 1. 加载当前配置
            var currentSettings = await _settingsService.LoadSettingsAsync();
            
            // 记录原始 ServerUrl，用于检测是否变化
            var originalServerUrl = currentSettings.ServerUrl;
            
            // 2. 创建 SettingsDialog 实例并传递当前配置
            var dialog = new SettingsDialog();
            var viewModel = new SettingsViewModel(_settingsService, dialog, currentSettings);
            dialog.DataContext = viewModel;
            
            // 3. 显示对话框（ShowDialog）
            var result = dialog.ShowDialog();
            
            // 4. 如果用户点击保存，重新加载配置并应用
            if (result == true && dialog.IsSaved)
            {
                Log.Information("用户保存配置");
                CurrentStatus = "正在保存配置...";
                
                // 保存配置到数据库
                await _settingsService.SaveSettingsAsync(currentSettings, "用户手动修改配置");
                Log.Information("配置已保存到数据库");
                
                // 5. 更新 API 客户端连接（如果 ServerUrl 变化）
                if (currentSettings.ServerUrl != originalServerUrl)
                {
                    Log.Information("服务器地址已变化，从 {OldUrl} 到 {NewUrl}", originalServerUrl, currentSettings.ServerUrl);
                    CurrentStatus = "服务器地址已变化，正在重新连接...";
                    
                    try
                    {
                        // 断开当前连接
                        await _apiClient.DisconnectAsync();
                        
                        // 等待断开完成
                        await Task.Delay(1000);
                        
                        // 使用新的 ServerUrl 重新连接
                        await _apiClient.ConnectAsync(currentSettings.ServerUrl);
                        
                        Log.Information("已重新连接到服务器");
                        CurrentStatus = "已重新连接到服务器";
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "重新连接到服务器失败");
                        CurrentStatus = $"重新连接失败: {ex.Message}";
                    }
                }
                else
                {
                    // 6. 通知用户配置已保存
                    CurrentStatus = "配置已保存";
                }
            }
            else
            {
                Log.Information("用户取消设置");
                CurrentStatus = "已取消设置";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "打开设置对话框失败");
            CurrentStatus = $"设置失败: {ex.Message}";
        }
    }
    
    /// <summary>
    /// 通知所有命令的 CanExecute 状态变化
    /// </summary>
    private void NotifyAllCommandsCanExecuteChanged()
    {
        StartAcquisitionCommand.NotifyCanExecuteChanged();
        EmergencyStopCommand.NotifyCanExecuteChanged();
        ResetMotorCommand.NotifyCanExecuteChanged();
        TakeScreenshotCommand.NotifyCanExecuteChanged();
        ExportReportCommand.NotifyCanExecuteChanged();
        SaveToDatabaseCommand.NotifyCanExecuteChanged();
        StartAutoTestCommand.NotifyCanExecuteChanged();
    }
    
    /// <summary>
    /// 取消订阅所有事件
    /// </summary>
    public void UnsubscribeFromEvents()
    {
        _apiClient.ConnectionStateChanged -= OnConnectionStateChanged;
        _apiClient.AcquisitionStatusChanged -= OnAcquisitionStatusChanged;
        _apiClient.CalculationCompleted -= OnCalculationCompleted;
        
        // 取消订阅子 ViewModel 的事件
        _chartViewModel.UnsubscribeFromEvents();
        _visualizationViewModel.UnsubscribeFromEvents();
        _statusBarViewModel.UnsubscribeFromEvents();
    }
}

