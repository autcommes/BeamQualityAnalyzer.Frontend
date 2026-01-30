using BeamQualityAnalyzer.Contracts.Dtos;
using BeamQualityAnalyzer.Contracts.Messages;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace BeamQualityAnalyzer.ApiClient;

/// <summary>
/// 光束分析器 API 客户端实现
/// </summary>
public class BeamAnalyzerApiClient : IBeamAnalyzerApiClient
{
    private readonly ILogger<BeamAnalyzerApiClient> _logger;
    private HubConnection? _hubConnection;
    private HttpClient? _httpClient;
    private string? _serverUrl;
    private bool _disposed;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public BeamAnalyzerApiClient(ILogger<BeamAnalyzerApiClient> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = new HttpClient();
    }
    
    // ==================== 连接管理 ====================
    
    /// <inheritdoc/>
    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
    
    /// <inheritdoc/>
    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    
    /// <inheritdoc/>
    public async Task ConnectAsync(string serverUrl, CancellationToken cancellationToken = default)
    {
        if (_hubConnection != null)
        {
            await DisconnectAsync();
        }
        
        _serverUrl = serverUrl.TrimEnd('/');
        
        _logger.LogInformation("正在创建 SignalR 连接到: {ServerUrl}", _serverUrl);
        
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{_serverUrl}/beamAnalyzerHub")
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
            .Build();
        
        // 注册服务器推送事件
        RegisterServerEvents();
        
        // 连接状态变化处理
        _hubConnection.Closed += OnConnectionClosed;
        _hubConnection.Reconnecting += OnReconnecting;
        _hubConnection.Reconnected += OnReconnected;
        
        _logger.LogInformation("正在启动 SignalR 连接...");
        await _hubConnection.StartAsync(cancellationToken);
        
        _logger.LogInformation("已连接到服务器: {ServerUrl}", serverUrl);
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(true));
    }
    
    /// <inheritdoc/>
    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
            
            _logger.LogInformation("已断开服务器连接");
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(false));
        }
    }
    
    // ==================== 命令操作实现 ====================
    
    #region 数据采集控制
    
    /// <inheritdoc/>
    public async Task<CommandResult> StartAcquisitionAsync()
    {
        EnsureConnected();
        return await _hubConnection!.InvokeAsync<CommandResult>("StartAcquisition");
    }
    
    /// <inheritdoc/>
    public async Task<CommandResult> StopAcquisitionAsync()
    {
        EnsureConnected();
        return await _hubConnection!.InvokeAsync<CommandResult>("StopAcquisition");
    }
    
    /// <inheritdoc/>
    public async Task<CommandResult> EmergencyStopAsync()
    {
        EnsureConnected();
        return await _hubConnection!.InvokeAsync<CommandResult>("EmergencyStop");
    }
    
    /// <inheritdoc/>
    public async Task<AcquisitionStatusDto> GetAcquisitionStatusAsync()
    {
        EnsureConnected();
        return await _hubConnection!.InvokeAsync<AcquisitionStatusDto>("GetAcquisitionStatus");
    }
    
    #endregion
    
    #region 设备控制
    
    /// <inheritdoc/>
    public async Task<CommandResult> ResetDeviceAsync()
    {
        EnsureConnected();
        return await _hubConnection!.InvokeAsync<CommandResult>("ResetDevice");
    }
    
    /// <inheritdoc/>
    public async Task<DeviceStatusDto> GetDeviceStatusAsync()
    {
        EnsureConnected();
        return await _hubConnection!.InvokeAsync<DeviceStatusDto>("GetDeviceStatus");
    }
    
    #endregion
    
    #region 算法计算
    
    /// <inheritdoc/>
    public async Task<CommandResult> RecalculateAnalysisAsync(AnalysisParametersDto parameters)
    {
        EnsureConnected();
        return await _hubConnection!.InvokeAsync<CommandResult>("RecalculateAnalysis", parameters);
    }
    
    /// <inheritdoc/>
    public async Task<BeamAnalysisResultDto> GetLatestAnalysisResultAsync()
    {
        EnsureConnected();
        return await _hubConnection!.InvokeAsync<BeamAnalysisResultDto>("GetLatestAnalysisResult");
    }
    
    #endregion
    
    #region 数据库操作
    
    /// <inheritdoc/>
    public async Task<CommandResult<int>> SaveMeasurementAsync(MeasurementRecordDto record)
    {
        EnsureConnected();
        return await _hubConnection!.InvokeAsync<CommandResult<int>>("SaveMeasurement", record);
    }
    
    /// <inheritdoc/>
    public async Task<List<MeasurementRecordDto>> QueryMeasurementsAsync(QueryParametersDto parameters)
    {
        EnsureConnected();
        return await _hubConnection!.InvokeAsync<List<MeasurementRecordDto>>("QueryMeasurements", parameters);
    }
    
    /// <inheritdoc/>
    public async Task<CommandResult> DeleteMeasurementAsync(int id)
    {
        EnsureConnected();
        return await _hubConnection!.InvokeAsync<CommandResult>("DeleteMeasurement", id);
    }
    
    #endregion
    
    #region 导出功能
    
    /// <inheritdoc/>
    public async Task<CommandResult<string>> GenerateScreenshotAsync()
    {
        EnsureConnected();
        return await _hubConnection!.InvokeAsync<CommandResult<string>>("GenerateScreenshot");
    }
    
    /// <inheritdoc/>
    public async Task<CommandResult<string>> GenerateReportAsync(ReportOptionsDto options)
    {
        EnsureConnected();
        return await _hubConnection!.InvokeAsync<CommandResult<string>>("GenerateReport", options);
    }
    
    /// <inheritdoc/>
    public async Task<byte[]> DownloadFileAsync(string filename)
    {
        if (string.IsNullOrEmpty(_serverUrl))
        {
            throw new InvalidOperationException("未连接到服务器");
        }
        
        // 使用 HTTP 下载文件
        var response = await _httpClient!.GetAsync($"{_serverUrl}/api/export/download/{filename}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }
    
    #endregion
    
    #region 配置管理
    
    /// <inheritdoc/>
    public async Task<AppSettingsDto> GetSettingsAsync()
    {
        EnsureConnected();
        return await _hubConnection!.InvokeAsync<AppSettingsDto>("GetSettings");
    }
    
    /// <inheritdoc/>
    public async Task<CommandResult> UpdateSettingsAsync(AppSettingsDto settings)
    {
        EnsureConnected();
        return await _hubConnection!.InvokeAsync<CommandResult>("UpdateSettings", settings);
    }
    
    /// <inheritdoc/>
    public async Task<CommandResult> TestDatabaseConnectionAsync(DatabaseSettingsDto settings)
    {
        EnsureConnected();
        return await _hubConnection!.InvokeAsync<CommandResult>("TestDatabaseConnection", settings);
    }
    
    #endregion
    
    #region 自动测试
    
    /// <inheritdoc/>
    public async Task<CommandResult> StartAutoTestAsync(AutoTestConfigurationDto config)
    {
        EnsureConnected();
        return await _hubConnection!.InvokeAsync<CommandResult>("StartAutoTest", config);
    }
    
    /// <inheritdoc/>
    public async Task<AutoTestStatusDto> GetAutoTestStatusAsync()
    {
        EnsureConnected();
        return await _hubConnection!.InvokeAsync<AutoTestStatusDto>("GetAutoTestStatus");
    }
    
    #endregion
    
    #region 数据流订阅
    
    /// <inheritdoc/>
    public async Task SubscribeToDataStreamAsync()
    {
        EnsureConnected();
        await _hubConnection!.InvokeAsync("SubscribeToDataStream");
    }
    
    /// <inheritdoc/>
    public async Task UnsubscribeFromDataStreamAsync()
    {
        EnsureConnected();
        await _hubConnection!.InvokeAsync("UnsubscribeFromDataStream");
    }
    
    #endregion
    
    // ==================== 事件定义 ====================
    
    /// <inheritdoc/>
    public event EventHandler<RawDataReceivedMessage>? RawDataReceived;
    
    /// <inheritdoc/>
    public event EventHandler<CalculationCompletedMessage>? CalculationCompleted;
    
    /// <inheritdoc/>
    public event EventHandler<VisualizationDataMessage>? VisualizationDataUpdated;
    
    /// <inheritdoc/>
    public event EventHandler<DeviceStatusMessage>? DeviceStatusChanged;
    
    /// <inheritdoc/>
    public event EventHandler<AcquisitionStatusMessage>? AcquisitionStatusChanged;
    
    /// <inheritdoc/>
    public event EventHandler<ErrorMessage>? ErrorOccurred;
    
    /// <inheritdoc/>
    public event EventHandler<ProgressMessage>? ProgressUpdated;
    
    /// <inheritdoc/>
    public event EventHandler<LogMessage>? LogMessageReceived;
    
    // ==================== 服务器推送事件注册 ====================
    
    private void RegisterServerEvents()
    {
        if (_hubConnection == null)
            return;
        
        _hubConnection.On<RawDataReceivedMessage>("OnRawDataReceived", message =>
        {
            RawDataReceived?.Invoke(this, message);
        });
        
        _hubConnection.On<CalculationCompletedMessage>("OnCalculationCompleted", message =>
        {
            CalculationCompleted?.Invoke(this, message);
        });
        
        _hubConnection.On<VisualizationDataMessage>("OnVisualizationDataUpdated", message =>
        {
            VisualizationDataUpdated?.Invoke(this, message);
        });
        
        _hubConnection.On<DeviceStatusMessage>("OnDeviceStatusChanged", message =>
        {
            DeviceStatusChanged?.Invoke(this, message);
        });
        
        _hubConnection.On<AcquisitionStatusMessage>("OnAcquisitionStatusChanged", message =>
        {
            AcquisitionStatusChanged?.Invoke(this, message);
        });
        
        _hubConnection.On<ErrorMessage>("OnErrorOccurred", message =>
        {
            ErrorOccurred?.Invoke(this, message);
        });
        
        _hubConnection.On<ProgressMessage>("OnProgressUpdated", message =>
        {
            ProgressUpdated?.Invoke(this, message);
        });
        
        _hubConnection.On<LogMessage>("OnLogMessage", message =>
        {
            LogMessageReceived?.Invoke(this, message);
        });
    }
    
    // ==================== 辅助方法 ====================
    
    private void EnsureConnected()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("未连接到服务器");
        }
    }
    
    private Task OnConnectionClosed(Exception? exception)
    {
        _logger.LogWarning(exception, "连接已关闭");
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(false));
        return Task.CompletedTask;
    }
    
    private Task OnReconnecting(Exception? exception)
    {
        _logger.LogInformation("正在重新连接...");
        return Task.CompletedTask;
    }
    
    private Task OnReconnected(string? connectionId)
    {
        _logger.LogInformation("已重新连接，连接ID: {ConnectionId}", connectionId);
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(true));
        return Task.CompletedTask;
    }
    
    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;
        
        DisconnectAsync().GetAwaiter().GetResult();
        _httpClient?.Dispose();
        _disposed = true;
        
        GC.SuppressFinalize(this);
    }
}
