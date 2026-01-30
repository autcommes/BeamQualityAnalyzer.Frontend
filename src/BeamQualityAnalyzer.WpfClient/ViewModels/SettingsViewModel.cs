using BeamQualityAnalyzer.WpfClient.Models;
using BeamQualityAnalyzer.WpfClient.Services;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;
using System.Windows.Media;

namespace BeamQualityAnalyzer.WpfClient.ViewModels;

/// <summary>
/// 设置对话框 ViewModel
/// </summary>
/// <remarks>
/// Requirements:
/// - 13.1: 打开设置对话框
/// - 13.2: 设备连接参数配置
/// - 13.3: 输出目录配置
/// - 13.4: 算法参数配置
/// - 13.5: UI 主题配置
/// </remarks>
public class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly Window _dialogWindow;
    private readonly AppSettings _originalSettings;
    
    // 服务器配置
    private string _serverUrl = "http://localhost:5000";
    private bool _autoReconnect = true;
    private int _reconnectInterval = 5000;
    private int _connectionTimeout = 30000;
    
    // 设备配置
    private string _deviceConnectionType = "Virtual";
    private string _devicePortName = "COM3";
    private int _deviceBaudRate = 115200;
    private int _deviceAcquisitionFrequency = 10;
    
    // 算法配置
    private double _algorithmDefaultWavelength = 632.8;
    private int _algorithmMinDataPoints = 10;
    private double _algorithmFitTolerance = 0.001;
    
    // 导出配置
    private string _exportScreenshotDirectory = @"C:\BeamAnalyzer\Screenshots";
    private string _exportReportDirectory = @"C:\BeamAnalyzer\Reports";
    private string _exportImageFormat = "PNG";
    
    // 数据库配置
    private bool _remoteDatabaseEnabled = false;
    private string _remoteDatabaseType = "None";
    private string? _remoteDatabaseConnectionString;
    private string _databaseTestResult = "";
    private Brush _databaseTestResultColor = Brushes.White;
    
    // UI 配置
    private string _uiTheme = "Dark";
    private double _uiChartRefreshInterval = 200;
    private double _uiVisualization3DRefreshInterval = 300;
    
    // 日志配置
    private string _loggingMinimumLevel = "Information";
    private string _loggingDirectory = @"C:\BeamAnalyzer\Logs";
    
    #region Properties
    
    public string ServerUrl
    {
        get => _serverUrl;
        set => SetProperty(ref _serverUrl, value);
    }
    
    public bool AutoReconnect
    {
        get => _autoReconnect;
        set => SetProperty(ref _autoReconnect, value);
    }
    
    public int ReconnectInterval
    {
        get => _reconnectInterval;
        set => SetProperty(ref _reconnectInterval, value);
    }
    
    public int ConnectionTimeout
    {
        get => _connectionTimeout;
        set => SetProperty(ref _connectionTimeout, value);
    }
    
    public string DeviceConnectionType
    {
        get => _deviceConnectionType;
        set => SetProperty(ref _deviceConnectionType, value);
    }
    
    public string DevicePortName
    {
        get => _devicePortName;
        set => SetProperty(ref _devicePortName, value);
    }
    
    public int DeviceBaudRate
    {
        get => _deviceBaudRate;
        set => SetProperty(ref _deviceBaudRate, value);
    }
    
    public int DeviceAcquisitionFrequency
    {
        get => _deviceAcquisitionFrequency;
        set => SetProperty(ref _deviceAcquisitionFrequency, value);
    }
    
    public double AlgorithmDefaultWavelength
    {
        get => _algorithmDefaultWavelength;
        set => SetProperty(ref _algorithmDefaultWavelength, value);
    }
    
    public int AlgorithmMinDataPoints
    {
        get => _algorithmMinDataPoints;
        set => SetProperty(ref _algorithmMinDataPoints, value);
    }
    
    public double AlgorithmFitTolerance
    {
        get => _algorithmFitTolerance;
        set => SetProperty(ref _algorithmFitTolerance, value);
    }
    
    public string ExportScreenshotDirectory
    {
        get => _exportScreenshotDirectory;
        set => SetProperty(ref _exportScreenshotDirectory, value);
    }
    
    public string ExportReportDirectory
    {
        get => _exportReportDirectory;
        set => SetProperty(ref _exportReportDirectory, value);
    }
    
    public string ExportImageFormat
    {
        get => _exportImageFormat;
        set => SetProperty(ref _exportImageFormat, value);
    }
    
    public bool RemoteDatabaseEnabled
    {
        get => _remoteDatabaseEnabled;
        set => SetProperty(ref _remoteDatabaseEnabled, value);
    }
    
    public string RemoteDatabaseType
    {
        get => _remoteDatabaseType;
        set => SetProperty(ref _remoteDatabaseType, value);
    }
    
    public string? RemoteDatabaseConnectionString
    {
        get => _remoteDatabaseConnectionString;
        set => SetProperty(ref _remoteDatabaseConnectionString, value);
    }
    
    public string DatabaseTestResult
    {
        get => _databaseTestResult;
        set => SetProperty(ref _databaseTestResult, value);
    }
    
    public Brush DatabaseTestResultColor
    {
        get => _databaseTestResultColor;
        set => SetProperty(ref _databaseTestResultColor, value);
    }
    
    public string UITheme
    {
        get => _uiTheme;
        set => SetProperty(ref _uiTheme, value);
    }
    
    public double UIChartRefreshInterval
    {
        get => _uiChartRefreshInterval;
        set => SetProperty(ref _uiChartRefreshInterval, value);
    }
    
    public double UIVisualization3DRefreshInterval
    {
        get => _uiVisualization3DRefreshInterval;
        set => SetProperty(ref _uiVisualization3DRefreshInterval, value);
    }
    
    public string LoggingMinimumLevel
    {
        get => _loggingMinimumLevel;
        set => SetProperty(ref _loggingMinimumLevel, value);
    }
    
    public string LoggingDirectory
    {
        get => _loggingDirectory;
        set => SetProperty(ref _loggingDirectory, value);
    }
    
    #endregion
    
    #region Commands
    
    public IAsyncRelayCommand TestDatabaseConnectionCommand { get; }
    public IRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }
    
    #endregion
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public SettingsViewModel(ISettingsService settingsService, Window dialogWindow, AppSettings currentSettings)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _dialogWindow = dialogWindow ?? throw new ArgumentNullException(nameof(dialogWindow));
        _originalSettings = currentSettings ?? throw new ArgumentNullException(nameof(currentSettings));
        
        // 从当前配置加载值
        LoadFromSettings(currentSettings);
        
        // 创建命令
        TestDatabaseConnectionCommand = new AsyncRelayCommand(TestDatabaseConnectionAsync);
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
    }
    
    /// <summary>
    /// 从 AppSettings 加载配置到 ViewModel
    /// </summary>
    private void LoadFromSettings(AppSettings settings)
    {
        ServerUrl = settings.ServerUrl;
        AutoReconnect = settings.AutoReconnect;
        ReconnectInterval = settings.ReconnectInterval;
        ConnectionTimeout = settings.ConnectionTimeout;
        
        DeviceConnectionType = settings.DeviceConnectionType;
        DevicePortName = settings.DevicePortName;
        DeviceBaudRate = settings.DeviceBaudRate;
        DeviceAcquisitionFrequency = settings.DeviceAcquisitionFrequency;
        
        AlgorithmDefaultWavelength = settings.AlgorithmDefaultWavelength;
        AlgorithmMinDataPoints = settings.AlgorithmMinDataPoints;
        AlgorithmFitTolerance = settings.AlgorithmFitTolerance;
        
        ExportScreenshotDirectory = settings.ExportScreenshotDirectory;
        ExportReportDirectory = settings.ExportReportDirectory;
        ExportImageFormat = settings.ExportImageFormat;
        
        RemoteDatabaseEnabled = settings.RemoteDatabaseEnabled;
        RemoteDatabaseType = settings.RemoteDatabaseType;
        RemoteDatabaseConnectionString = settings.RemoteDatabaseConnectionString;
        
        UITheme = settings.UITheme;
        UIChartRefreshInterval = settings.UIChartRefreshInterval;
        UIVisualization3DRefreshInterval = settings.UIVisualization3DRefreshInterval;
        
        LoggingMinimumLevel = settings.LoggingMinimumLevel;
        LoggingDirectory = settings.LoggingDirectory;
    }
    
    /// <summary>
    /// 将 ViewModel 的值保存到 AppSettings
    /// </summary>
    private void SaveToSettings(AppSettings settings)
    {
        settings.ServerUrl = ServerUrl;
        settings.AutoReconnect = AutoReconnect;
        settings.ReconnectInterval = ReconnectInterval;
        settings.ConnectionTimeout = ConnectionTimeout;
        
        settings.DeviceConnectionType = DeviceConnectionType;
        settings.DevicePortName = DevicePortName;
        settings.DeviceBaudRate = DeviceBaudRate;
        settings.DeviceAcquisitionFrequency = DeviceAcquisitionFrequency;
        
        settings.AlgorithmDefaultWavelength = AlgorithmDefaultWavelength;
        settings.AlgorithmMinDataPoints = AlgorithmMinDataPoints;
        settings.AlgorithmFitTolerance = AlgorithmFitTolerance;
        
        settings.ExportScreenshotDirectory = ExportScreenshotDirectory;
        settings.ExportReportDirectory = ExportReportDirectory;
        settings.ExportImageFormat = ExportImageFormat;
        
        settings.RemoteDatabaseEnabled = RemoteDatabaseEnabled;
        settings.RemoteDatabaseType = RemoteDatabaseType;
        settings.RemoteDatabaseConnectionString = RemoteDatabaseConnectionString;
        
        settings.UITheme = UITheme;
        settings.UIChartRefreshInterval = UIChartRefreshInterval;
        settings.UIVisualization3DRefreshInterval = UIVisualization3DRefreshInterval;
        
        settings.LoggingMinimumLevel = LoggingMinimumLevel;
        settings.LoggingDirectory = LoggingDirectory;
    }
    
    /// <summary>
    /// 测试数据库连接
    /// </summary>
    private async Task TestDatabaseConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(RemoteDatabaseConnectionString))
        {
            DatabaseTestResult = "请输入连接字符串";
            DatabaseTestResultColor = new SolidColorBrush(Color.FromRgb(0xF4, 0x47, 0x47)); // #F44747
            return;
        }
        
        DatabaseTestResult = "正在测试连接...";
        DatabaseTestResultColor = new SolidColorBrush(Color.FromRgb(0xD7, 0xBA, 0x7D)); // #D7BA7D
        
        try
        {
            var isConnected = await _settingsService.TestRemoteDatabaseConnectionAsync(
                RemoteDatabaseConnectionString, 
                RemoteDatabaseType);
            
            if (isConnected)
            {
                DatabaseTestResult = "连接成功";
                DatabaseTestResultColor = new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0xB0)); // #4EC9B0
            }
            else
            {
                DatabaseTestResult = "连接失败";
                DatabaseTestResultColor = new SolidColorBrush(Color.FromRgb(0xF4, 0x47, 0x47)); // #F44747
            }
        }
        catch (Exception ex)
        {
            DatabaseTestResult = $"连接异常: {ex.Message}";
            DatabaseTestResultColor = new SolidColorBrush(Color.FromRgb(0xF4, 0x47, 0x47)); // #F44747
        }
    }
    
    /// <summary>
    /// 保存配置
    /// </summary>
    private void Save()
    {
        // 将 ViewModel 的值保存到原始配置对象
        SaveToSettings(_originalSettings);
        
        // 设置对话框结果
        if (_dialogWindow is Views.SettingsDialog dialog)
        {
            dialog.IsSaved = true;
        }
        
        // 关闭对话框
        _dialogWindow.DialogResult = true;
        _dialogWindow.Close();
    }
    
    /// <summary>
    /// 取消
    /// </summary>
    private void Cancel()
    {
        // 设置对话框结果
        if (_dialogWindow is Views.SettingsDialog dialog)
        {
            dialog.IsSaved = false;
        }
        
        // 关闭对话框
        _dialogWindow.DialogResult = false;
        _dialogWindow.Close();
    }
}
