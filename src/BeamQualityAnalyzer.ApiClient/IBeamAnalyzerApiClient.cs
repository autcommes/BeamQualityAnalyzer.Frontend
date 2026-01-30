using BeamQualityAnalyzer.Contracts.Dtos;
using BeamQualityAnalyzer.Contracts.Messages;

namespace BeamQualityAnalyzer.ApiClient;

/// <summary>
/// 光束分析器 API 客户端接口
/// 封装与后端服务的 WebSocket 通信
/// </summary>
public interface IBeamAnalyzerApiClient : IDisposable
{
    // ==================== 连接管理 ====================
    
    /// <summary>
    /// 连接到服务器
    /// </summary>
    /// <param name="serverUrl">服务器地址（如 http://localhost:5000）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ConnectAsync(string serverUrl, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 断开连接
    /// </summary>
    Task DisconnectAsync();
    
    /// <summary>
    /// 连接状态
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// 连接状态变化事件
    /// </summary>
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    
    // ==================== 命令操作（调用服务器方法）====================
    
    #region 数据采集控制
    
    /// <summary>
    /// 启动数据采集
    /// </summary>
    Task<CommandResult> StartAcquisitionAsync();
    
    /// <summary>
    /// 停止数据采集
    /// </summary>
    Task<CommandResult> StopAcquisitionAsync();
    
    /// <summary>
    /// 急停
    /// </summary>
    Task<CommandResult> EmergencyStopAsync();
    
    /// <summary>
    /// 获取采集状态
    /// </summary>
    Task<AcquisitionStatusDto> GetAcquisitionStatusAsync();
    
    #endregion
    
    #region 设备控制
    
    /// <summary>
    /// 设备复位
    /// </summary>
    Task<CommandResult> ResetDeviceAsync();
    
    /// <summary>
    /// 获取设备状态
    /// </summary>
    Task<DeviceStatusDto> GetDeviceStatusAsync();
    
    #endregion
    
    #region 算法计算
    
    /// <summary>
    /// 重新计算分析
    /// </summary>
    Task<CommandResult> RecalculateAnalysisAsync(AnalysisParametersDto parameters);
    
    /// <summary>
    /// 获取最新分析结果
    /// </summary>
    Task<BeamAnalysisResultDto> GetLatestAnalysisResultAsync();
    
    #endregion
    
    #region 数据库操作
    
    /// <summary>
    /// 保存测量记录
    /// </summary>
    Task<CommandResult<int>> SaveMeasurementAsync(MeasurementRecordDto record);
    
    /// <summary>
    /// 查询测量记录
    /// </summary>
    Task<List<MeasurementRecordDto>> QueryMeasurementsAsync(QueryParametersDto parameters);
    
    /// <summary>
    /// 删除测量记录
    /// </summary>
    Task<CommandResult> DeleteMeasurementAsync(int id);
    
    #endregion
    
    #region 导出功能
    
    /// <summary>
    /// 生成截图
    /// </summary>
    Task<CommandResult<string>> GenerateScreenshotAsync();
    
    /// <summary>
    /// 生成报告
    /// </summary>
    Task<CommandResult<string>> GenerateReportAsync(ReportOptionsDto options);
    
    /// <summary>
    /// 下载文件（使用HTTP）
    /// </summary>
    /// <param name="filename">文件名</param>
    Task<byte[]> DownloadFileAsync(string filename);
    
    #endregion
    
    #region 配置管理
    
    /// <summary>
    /// 获取系统配置
    /// </summary>
    Task<AppSettingsDto> GetSettingsAsync();
    
    /// <summary>
    /// 更新系统配置
    /// </summary>
    Task<CommandResult> UpdateSettingsAsync(AppSettingsDto settings);
    
    /// <summary>
    /// 测试数据库连接
    /// </summary>
    Task<CommandResult> TestDatabaseConnectionAsync(DatabaseSettingsDto settings);
    
    #endregion
    
    #region 自动测试
    
    /// <summary>
    /// 启动自动测试
    /// </summary>
    Task<CommandResult> StartAutoTestAsync(AutoTestConfigurationDto config);
    
    /// <summary>
    /// 获取自动测试状态
    /// </summary>
    Task<AutoTestStatusDto> GetAutoTestStatusAsync();
    
    #endregion
    
    #region 数据流订阅
    
    /// <summary>
    /// 订阅数据流
    /// </summary>
    Task SubscribeToDataStreamAsync();
    
    /// <summary>
    /// 取消订阅数据流
    /// </summary>
    Task UnsubscribeFromDataStreamAsync();
    
    #endregion
    
    // ==================== 事件（接收服务器推送）====================
    
    /// <summary>
    /// 原始数据接收事件（10Hz频率）
    /// </summary>
    event EventHandler<RawDataReceivedMessage>? RawDataReceived;
    
    /// <summary>
    /// 计算完成事件
    /// </summary>
    event EventHandler<CalculationCompletedMessage>? CalculationCompleted;
    
    /// <summary>
    /// 可视化数据更新事件
    /// </summary>
    event EventHandler<VisualizationDataMessage>? VisualizationDataUpdated;
    
    /// <summary>
    /// 设备状态变化事件
    /// </summary>
    event EventHandler<DeviceStatusMessage>? DeviceStatusChanged;
    
    /// <summary>
    /// 采集状态变化事件
    /// </summary>
    event EventHandler<AcquisitionStatusMessage>? AcquisitionStatusChanged;
    
    /// <summary>
    /// 错误发生事件
    /// </summary>
    event EventHandler<ErrorMessage>? ErrorOccurred;
    
    /// <summary>
    /// 进度更新事件
    /// </summary>
    event EventHandler<ProgressMessage>? ProgressUpdated;
    
    /// <summary>
    /// 日志消息事件
    /// </summary>
    event EventHandler<LogMessage>? LogMessageReceived;
}

/// <summary>
/// 连接状态变化事件参数
/// </summary>
public class ConnectionStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public ConnectionStateChangedEventArgs(bool isConnected)
    {
        IsConnected = isConnected;
    }
}
