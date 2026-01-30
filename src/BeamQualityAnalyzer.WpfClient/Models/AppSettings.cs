using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeamQualityAnalyzer.WpfClient.Models;

/// <summary>
/// 应用程序配置实体
/// 存储在本地 SQLite 数据库中（%APPDATA%\BeamQualityAnalyzer\config.db）
/// </summary>
/// <remarks>
/// Requirements:
/// - 13.2: 设备连接参数配置
/// - 13.3: 输出目录配置
/// - 13.4: 算法参数配置
/// - 13.5: UI 主题配置
/// - 13.6: 配置持久化到本地数据库
/// </remarks>
[Table("AppSettings")]
public class AppSettings
{
    /// <summary>
    /// 主键（固定为 1，单行表）
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; } = 1;
    
    // ==================== 服务器配置 ====================
    
    /// <summary>
    /// 服务器 URL
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string ServerUrl { get; set; } = "http://localhost:5000";
    
    /// <summary>
    /// 自动重连
    /// </summary>
    public bool AutoReconnect { get; set; } = true;
    
    /// <summary>
    /// 重连间隔（毫秒）
    /// </summary>
    public int ReconnectInterval { get; set; } = 5000;
    
    /// <summary>
    /// 连接超时（毫秒）
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30000;
    
    // ==================== 设备配置 ====================
    
    /// <summary>
    /// 设备连接类型（Virtual, Serial, USB, Network）
    /// </summary>
    [MaxLength(50)]
    public string DeviceConnectionType { get; set; } = "Virtual";
    
    /// <summary>
    /// 设备端口名称（如 COM3）
    /// </summary>
    [MaxLength(50)]
    public string DevicePortName { get; set; } = "COM3";
    
    /// <summary>
    /// 设备波特率
    /// </summary>
    public int DeviceBaudRate { get; set; } = 115200;
    
    /// <summary>
    /// 数据采集频率（Hz）
    /// </summary>
    public int DeviceAcquisitionFrequency { get; set; } = 10;
    
    // ==================== 算法配置 ====================
    
    /// <summary>
    /// 默认波长（nm）
    /// </summary>
    public double AlgorithmDefaultWavelength { get; set; } = 632.8;
    
    /// <summary>
    /// 最小数据点数
    /// </summary>
    public int AlgorithmMinDataPoints { get; set; } = 10;
    
    /// <summary>
    /// 拟合容差
    /// </summary>
    public double AlgorithmFitTolerance { get; set; } = 0.001;
    
    // ==================== 导出配置 ====================
    
    /// <summary>
    /// 截图保存目录
    /// </summary>
    [MaxLength(500)]
    public string ExportScreenshotDirectory { get; set; } = @"C:\BeamAnalyzer\Screenshots";
    
    /// <summary>
    /// 报告保存目录
    /// </summary>
    [MaxLength(500)]
    public string ExportReportDirectory { get; set; } = @"C:\BeamAnalyzer\Reports";
    
    /// <summary>
    /// 图像格式（PNG, JPEG）
    /// </summary>
    [MaxLength(10)]
    public string ExportImageFormat { get; set; } = "PNG";
    
    // ==================== 远程数据库配置 ====================
    
    /// <summary>
    /// 启用远程数据库
    /// </summary>
    public bool RemoteDatabaseEnabled { get; set; } = false;
    
    /// <summary>
    /// 远程数据库类型（None, MySQL, SqlServer）
    /// </summary>
    [MaxLength(50)]
    public string RemoteDatabaseType { get; set; } = "None";
    
    /// <summary>
    /// 远程数据库连接字符串
    /// </summary>
    [MaxLength(1000)]
    public string? RemoteDatabaseConnectionString { get; set; }
    
    /// <summary>
    /// 远程数据库命令超时（秒）
    /// </summary>
    public int RemoteDatabaseCommandTimeout { get; set; } = 30;
    
    /// <summary>
    /// 启用远程数据库重试
    /// </summary>
    public bool RemoteDatabaseEnableRetry { get; set; } = true;
    
    /// <summary>
    /// 远程数据库最大重试次数
    /// </summary>
    public int RemoteDatabaseMaxRetryCount { get; set; } = 3;
    
    // ==================== UI 配置 ====================
    
    /// <summary>
    /// UI 主题（Dark, Light）
    /// </summary>
    [MaxLength(50)]
    public string UITheme { get; set; } = "Dark";
    
    /// <summary>
    /// 图表刷新间隔（毫秒）
    /// </summary>
    public double UIChartRefreshInterval { get; set; } = 200;
    
    /// <summary>
    /// 3D 可视化刷新间隔（毫秒）
    /// </summary>
    public double UIVisualization3DRefreshInterval { get; set; } = 300;
    
    // ==================== 日志配置 ====================
    
    /// <summary>
    /// 日志最小级别（Debug, Information, Warning, Error）
    /// </summary>
    [MaxLength(50)]
    public string LoggingMinimumLevel { get; set; } = "Information";
    
    /// <summary>
    /// 日志保存目录
    /// </summary>
    [MaxLength(500)]
    public string LoggingDirectory { get; set; } = @"C:\BeamAnalyzer\Logs";
    
    // ==================== 元数据 ====================
    
    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 配置版本
    /// </summary>
    [MaxLength(50)]
    public string Version { get; set; } = "1.0.0";
}
