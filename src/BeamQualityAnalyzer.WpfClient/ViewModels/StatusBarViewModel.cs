using BeamQualityAnalyzer.ApiClient;
using BeamQualityAnalyzer.Contracts.Messages;

namespace BeamQualityAnalyzer.WpfClient.ViewModels;

/// <summary>
/// 状态栏 ViewModel
/// 显示系统状态、操作反馈和进度信息
/// </summary>
/// <remarks>
/// Requirements:
/// - 14.1: 显示当前系统状态（就绪、采集中、计算中、错误等）
/// - 14.2: 使用状态色标识不同状态
/// - 14.3: 显示操作反馈信息
/// - 14.4: 显示最后一次操作的时间戳
/// </remarks>
public class StatusBarViewModel : ViewModelBase
{
    private readonly IBeamAnalyzerApiClient _apiClient;
    
    private string _statusText = "就绪";
    private StatusLevel _statusLevel = StatusLevel.Normal;
    private DateTime? _lastOperationTime;
    private double _progressValue;
    private bool _isProgressVisible;
    
    /// <summary>
    /// 状态文本
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }
    
    /// <summary>
    /// 状态级别
    /// </summary>
    public StatusLevel StatusLevel
    {
        get => _statusLevel;
        private set => SetProperty(ref _statusLevel, value);
    }
    
    /// <summary>
    /// 最后操作时间
    /// </summary>
    public DateTime? LastOperationTime
    {
        get => _lastOperationTime;
        private set => SetProperty(ref _lastOperationTime, value);
    }
    
    /// <summary>
    /// 进度值 (0-100)
    /// </summary>
    public double ProgressValue
    {
        get => _progressValue;
        private set => SetProperty(ref _progressValue, value);
    }
    
    /// <summary>
    /// 进度条是否可见
    /// </summary>
    public bool IsProgressVisible
    {
        get => _isProgressVisible;
        private set => SetProperty(ref _isProgressVisible, value);
    }
    
    /// <summary>
    /// 状态颜色（用于 UI 绑定）
    /// </summary>
    public string StatusColor => StatusLevel switch
    {
        StatusLevel.Normal => "#4EC9B0",   // 正常 - 青绿色
        StatusLevel.Warning => "#D7BA7D",  // 警告 - 黄色
        StatusLevel.Error => "#F44747",    // 错误 - 红色
        _ => "#D4D4D4"                     // 默认 - 灰色
    };
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="apiClient">API 客户端</param>
    public StatusBarViewModel(IBeamAnalyzerApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        
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
        _apiClient.DeviceStatusChanged += OnDeviceStatusChanged;
        _apiClient.ErrorOccurred += OnErrorOccurred;
        _apiClient.ProgressUpdated += OnProgressUpdated;
        _apiClient.CalculationCompleted += OnCalculationCompleted;
    }
    
    /// <summary>
    /// 连接状态变化处理
    /// </summary>
    private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        if (e.IsConnected)
        {
            UpdateStatus("已连接到服务器", StatusLevel.Normal);
        }
        else
        {
            UpdateStatus("与服务器断开连接", StatusLevel.Warning);
        }
    }
    
    /// <summary>
    /// 采集状态变化处理
    /// </summary>
    private void OnAcquisitionStatusChanged(object? sender, AcquisitionStatusMessage e)
    {
        if (e.IsAcquiring)
        {
            UpdateStatus($"采集中 - {e.DataPointCount} 个数据点 ({e.Frequency:F1} Hz)", StatusLevel.Normal, e.Timestamp);
        }
        else
        {
            UpdateStatus("采集已停止", StatusLevel.Normal, e.Timestamp);
        }
    }
    
    /// <summary>
    /// 设备状态变化处理
    /// </summary>
    private void OnDeviceStatusChanged(object? sender, DeviceStatusMessage e)
    {
        var message = string.IsNullOrEmpty(e.Message) ? e.Status : $"{e.Status} - {e.Message}";
        UpdateStatus(message, StatusLevel.Normal, e.Timestamp);
    }
    
    /// <summary>
    /// 错误发生处理
    /// </summary>
    private void OnErrorOccurred(object? sender, ErrorMessage e)
    {
        var level = e.Level.ToLowerInvariant() switch
        {
            "warning" => StatusLevel.Warning,
            "error" => StatusLevel.Error,
            _ => StatusLevel.Error
        };
        
        var message = string.IsNullOrEmpty(e.Title) ? e.Message : $"{e.Title}: {e.Message}";
        UpdateStatus(message, level, e.Timestamp);
    }
    
    /// <summary>
    /// 进度更新处理
    /// </summary>
    private void OnProgressUpdated(object? sender, ProgressMessage e)
    {
        ProgressValue = e.Percentage;
        IsProgressVisible = e.Percentage > 0 && e.Percentage < 100;
        
        var message = string.IsNullOrEmpty(e.Message) 
            ? $"{e.Operation} - {e.Percentage:F0}%" 
            : $"{e.Operation} - {e.Message} ({e.Percentage:F0}%)";
        
        UpdateStatus(message, StatusLevel.Normal, e.Timestamp);
        
        // 进度完成后隐藏进度条
        if (e.Percentage >= 100)
        {
            IsProgressVisible = false;
        }
    }
    
    /// <summary>
    /// 计算完成处理
    /// </summary>
    private void OnCalculationCompleted(object? sender, CalculationCompletedMessage e)
    {
        UpdateStatus("计算完成", StatusLevel.Normal, e.Timestamp);
        IsProgressVisible = false;
    }
    
    /// <summary>
    /// 更新状态
    /// </summary>
    /// <param name="text">状态文本</param>
    /// <param name="level">状态级别</param>
    /// <param name="timestamp">时间戳（可选）</param>
    private void UpdateStatus(string text, StatusLevel level, DateTime? timestamp = null)
    {
        StatusText = text;
        StatusLevel = level;
        LastOperationTime = timestamp ?? DateTime.Now;
        
        // 触发 StatusColor 属性变化通知
        OnPropertyChanged(nameof(StatusColor));
    }
    
    /// <summary>
    /// 取消订阅所有事件
    /// </summary>
    public void UnsubscribeFromEvents()
    {
        _apiClient.ConnectionStateChanged -= OnConnectionStateChanged;
        _apiClient.AcquisitionStatusChanged -= OnAcquisitionStatusChanged;
        _apiClient.DeviceStatusChanged -= OnDeviceStatusChanged;
        _apiClient.ErrorOccurred -= OnErrorOccurred;
        _apiClient.ProgressUpdated -= OnProgressUpdated;
        _apiClient.CalculationCompleted -= OnCalculationCompleted;
    }
}

/// <summary>
/// 状态级别枚举
/// </summary>
public enum StatusLevel
{
    /// <summary>
    /// 正常状态 - 青绿色 #4EC9B0
    /// </summary>
    Normal,
    
    /// <summary>
    /// 警告状态 - 黄色 #D7BA7D
    /// </summary>
    Warning,
    
    /// <summary>
    /// 错误状态 - 红色 #F44747
    /// </summary>
    Error
}
