using System.Text.Json;
using System.Windows;
using System.Windows.Media.Media3D;
using BeamQualityAnalyzer.ApiClient;
using BeamQualityAnalyzer.Contracts.Messages;
using BeamQualityAnalyzer.WpfClient.Helpers;

namespace BeamQualityAnalyzer.WpfClient.ViewModels;

/// <summary>
/// 可视化面板 ViewModel
/// 管理 2D 光斑图和 3D 能量分布可视化
/// </summary>
/// <remarks>
/// Requirements:
/// - 7.1: 显示 2D 光斑伪彩色热力图
/// - 7.2: 在 2D 光斑图中心显示十字标识线
/// - 8.1: 显示 3D Surface Plot 能量分布图
/// - 15.8: 图表更新使用节流机制（Throttle）
/// - 17.2: 确保 UI 线程不被阻塞
/// - 17.4: 3D 可视化在 300ms 内更新
/// </remarks>
public class VisualizationViewModel : ViewModelBase
{
    private readonly IBeamAnalyzerApiClient _apiClient;
    private readonly Action<Action>? _uiThreadInvoker;
    private readonly ThrottleHelper _visualization2DThrottle;
    private readonly ThrottleHelper _visualization3DThrottle;
    
    // 2D 光斑数据
    private double[,]? _spotIntensityData;
    private Point _spotCenter;
    
    // 3D 能量分布数据
    private Point3D[,]? _energyDistributionData;
    
    // 存储X和Y方向的3D数据
    private Point3D[,]? _energyDistributionDataX;
    private Point3D[,]? _energyDistributionDataY;
    
    // 当前选中的tab索引
    private int _selectedTabIndex;
    
    // 可视化设置
    private string _selectedColorMap = "Viridis";
    private double _zoomLevel = 1.0;
    
    /// <summary>
    /// 2D 光斑强度数据矩阵
    /// </summary>
    /// <remarks>
    /// 用于 ScottPlot Heatmap 显示伪彩色热力图
    /// Requirement 7.1: 显示 2D 光斑伪彩色热力图
    /// </remarks>
    public double[,]? SpotIntensityData
    {
        get => _spotIntensityData;
        set => SetProperty(ref _spotIntensityData, value);
    }
    
    /// <summary>
    /// 光斑中心坐标
    /// </summary>
    /// <remarks>
    /// 用于在 2D 光斑图上显示十字标识线
    /// Requirement 7.2: 在 2D 光斑图中心显示十字标识线
    /// </remarks>
    public Point SpotCenter
    {
        get => _spotCenter;
        set => SetProperty(ref _spotCenter, value);
    }
    
    /// <summary>
    /// 3D 能量分布数据
    /// </summary>
    /// <remarks>
    /// 用于 HelixToolkit.Wpf SurfaceVisual3D 显示 3D 能量分布
    /// Requirement 8.1: 显示 3D Surface Plot 能量分布图
    /// </remarks>
    public Point3D[,]? EnergyDistributionData
    {
        get => _energyDistributionData;
        set => SetProperty(ref _energyDistributionData, value);
    }
    
    /// <summary>
    /// 当前选中的tab索引
    /// </summary>
    /// <remarks>
    /// 0: 光束直径 X
    /// 1: 光束直径 Y
    /// 2: 双曲拟合 X
    /// 3: 双曲拟合 Y
    /// </remarks>
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetProperty(ref _selectedTabIndex, value))
            {
                // tab切换时更新3D视图
                UpdateEnergyDistributionForSelectedTab();
            }
        }
    }
    
    /// <summary>
    /// 选中的颜色映射表
    /// </summary>
    /// <remarks>
    /// 可选值: Viridis, Plasma, Inferno, Magma, Jet, Hot, Cool
    /// </remarks>
    public string SelectedColorMap
    {
        get => _selectedColorMap;
        set => SetProperty(ref _selectedColorMap, value);
    }
    
    /// <summary>
    /// 缩放级别
    /// </summary>
    public double ZoomLevel
    {
        get => _zoomLevel;
        set => SetProperty(ref _zoomLevel, value);
    }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="apiClient">API 客户端</param>
    /// <param name="uiThreadInvoker">UI 线程调用器（用于测试注入）</param>
    public VisualizationViewModel(IBeamAnalyzerApiClient apiClient, Action<Action>? uiThreadInvoker = null)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _uiThreadInvoker = uiThreadInvoker;
        
        // 创建节流辅助器
        // Requirement 17.3: 2D 图表更新在 200ms 内完成
        _visualization2DThrottle = new ThrottleHelper(200);
        
        // Requirement 17.4: 3D 可视化更新在 300ms 内完成
        _visualization3DThrottle = new ThrottleHelper(300);
        
        // 订阅事件
        SubscribeToEvents();
    }
    
    /// <summary>
    /// 订阅 API 客户端事件
    /// </summary>
    private void SubscribeToEvents()
    {
        _apiClient.VisualizationDataUpdated += OnVisualizationDataUpdated;
        _apiClient.RawDataReceived += OnRawDataReceived;
    }
    
    /// <summary>
    /// 可视化数据更新处理
    /// </summary>
    /// <remarks>
    /// Requirement 15.8: 图表更新使用节流机制（Throttle）
    /// Requirement 17.2: 确保 UI 线程不被阻塞
    /// </remarks>
    private void OnVisualizationDataUpdated(object? sender, VisualizationDataMessage e)
    {
        Serilog.Log.Debug("VisualizationViewModel 收到可视化数据更新: SpotCenter=({X}, {Y})", e.SpotCenterX, e.SpotCenterY);
        
        // 使用节流机制更新 2D 光斑数据
        _visualization2DThrottle.Throttle(() =>
        {
            InvokeOnUIThread(() => UpdateSpotIntensityData(e));
        });
        
        // 使用节流机制更新 3D 能量分布数据
        _visualization3DThrottle.Throttle(() =>
        {
            InvokeOnUIThread(() => UpdateEnergyDistributionData(e));
        });
    }
    
    /// <summary>
    /// 原始数据接收处理（用于实时更新可视化）
    /// </summary>
    private void OnRawDataReceived(object? sender, RawDataReceivedMessage e)
    {
        // 如果原始数据包含强度矩阵，也可以在这里更新
        // 这里暂时不处理，等待专门的 VisualizationDataUpdated 事件
    }
    
    /// <summary>
    /// 更新 2D 光斑强度数据
    /// </summary>
    /// <remarks>
    /// Requirement 17.3: 2D 图表降采样优化
    /// </remarks>
    private void UpdateSpotIntensityData(VisualizationDataMessage message)
    {
        try
        {
            // 更新光斑中心
            SpotCenter = new Point(message.SpotCenterX, message.SpotCenterY);
            
            // 反序列化 2D 强度数据
            if (!string.IsNullOrEmpty(message.SpotIntensityDataJson))
            {
                var intensityData = DeserializeMatrix(message.SpotIntensityDataJson);
                if (intensityData != null)
                {
                    // 降采样（如果矩阵过大）
                    // 对于 2D 热力图，限制为 200x200 像素
                    const int maxSize = 200;
                    int rows = intensityData.GetLength(0);
                    int cols = intensityData.GetLength(1);
                    
                    if (rows > maxSize || cols > maxSize)
                    {
                        int targetRows = Math.Min(rows, maxSize);
                        int targetCols = Math.Min(cols, maxSize);
                        intensityData = DataDownsamplingHelper.DownsampleMatrixAverage(intensityData, targetRows, targetCols);
                    }
                    
                    SpotIntensityData = intensityData;
                }
            }
        }
        catch (Exception ex)
        {
            // 记录错误但不抛出，避免影响其他功能
            System.Diagnostics.Debug.WriteLine($"更新 2D 光斑数据失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 更新 3D 能量分布数据
    /// </summary>
    /// <remarks>
    /// Requirement 17.4: 3D 可视化 LOD 优化
    /// </remarks>
    private void UpdateEnergyDistributionData(VisualizationDataMessage message)
    {
        try
        {
            Serilog.Log.Debug("开始更新3D能量分布数据");
            
            // 反序列化 X 方向 3D 能量分布数据
            if (!string.IsNullOrEmpty(message.EnergyDistribution3DXJson))
            {
                var energyDataX = DeserializeMatrix(message.EnergyDistribution3DXJson);
                if (energyDataX != null)
                {
                    Serilog.Log.Debug("3D数据X矩阵大小: {Rows}x{Cols}", energyDataX.GetLength(0), energyDataX.GetLength(1));
                    
                    // LOD 优化
                    const int maxSize = 100;
                    int rows = energyDataX.GetLength(0);
                    int cols = energyDataX.GetLength(1);
                    
                    if (rows > maxSize || cols > maxSize)
                    {
                        int targetRows = Math.Min(rows, maxSize);
                        int targetCols = Math.Min(cols, maxSize);
                        energyDataX = DataDownsamplingHelper.DownsampleMatrixAverage(energyDataX, targetRows, targetCols);
                    }
                    
                    _energyDistributionDataX = ConvertToPoint3DMatrix(energyDataX);
                    Serilog.Log.Debug("3D能量分布数据X已更新");
                }
            }
            
            // 反序列化 Y 方向 3D 能量分布数据
            if (!string.IsNullOrEmpty(message.EnergyDistribution3DYJson))
            {
                var energyDataY = DeserializeMatrix(message.EnergyDistribution3DYJson);
                if (energyDataY != null)
                {
                    Serilog.Log.Debug("3D数据Y矩阵大小: {Rows}x{Cols}", energyDataY.GetLength(0), energyDataY.GetLength(1));
                    
                    // LOD 优化
                    const int maxSize = 100;
                    int rows = energyDataY.GetLength(0);
                    int cols = energyDataY.GetLength(1);
                    
                    if (rows > maxSize || cols > maxSize)
                    {
                        int targetRows = Math.Min(rows, maxSize);
                        int targetCols = Math.Min(cols, maxSize);
                        energyDataY = DataDownsamplingHelper.DownsampleMatrixAverage(energyDataY, targetRows, targetCols);
                    }
                    
                    _energyDistributionDataY = ConvertToPoint3DMatrix(energyDataY);
                    Serilog.Log.Debug("3D能量分布数据Y已更新");
                }
            }
            
            // 根据当前选中的tab更新显示
            UpdateEnergyDistributionForSelectedTab();
        }
        catch (Exception ex)
        {
            // 记录错误但不抛出，避免影响其他功能
            Serilog.Log.Error(ex, "更新 3D 能量分布数据失败");
        }
    }
    
    /// <summary>
    /// 根据选中的tab更新3D能量分布显示
    /// </summary>
    private void UpdateEnergyDistributionForSelectedTab()
    {
        // 根据tab索引选择显示X或Y方向的数据
        // Tab 0, 2: X方向
        // Tab 1, 3: Y方向
        if (_selectedTabIndex == 0 || _selectedTabIndex == 2)
        {
            if (_energyDistributionDataX != null)
            {
                EnergyDistributionData = _energyDistributionDataX;
                Serilog.Log.Debug("切换到X方向3D视图");
            }
        }
        else if (_selectedTabIndex == 1 || _selectedTabIndex == 3)
        {
            if (_energyDistributionDataY != null)
            {
                EnergyDistributionData = _energyDistributionDataY;
                Serilog.Log.Debug("切换到Y方向3D视图");
            }
        }
    }
    
    /// <summary>
    /// 反序列化矩阵数据
    /// </summary>
    /// <param name="json">JSON 字符串</param>
    /// <returns>2D 数组</returns>
    private double[,]? DeserializeMatrix(string json)
    {
        try
        {
            // 反序列化为锯齿数组
            var jaggedArray = JsonSerializer.Deserialize<double[][]>(json);
            if (jaggedArray == null || jaggedArray.Length == 0)
                return null;
            
            // 转换为二维数组
            int rows = jaggedArray.Length;
            int cols = jaggedArray[0].Length;
            var matrix = new double[rows, cols];
            
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    matrix[i, j] = jaggedArray[i][j];
                }
            }
            
            return matrix;
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// 将 2D 强度矩阵转换为 3D Point3D 矩阵
    /// </summary>
    /// <param name="intensityMatrix">强度矩阵</param>
    /// <returns>Point3D 矩阵</returns>
    private Point3D[,] ConvertToPoint3DMatrix(double[,] intensityMatrix)
    {
        int rows = intensityMatrix.GetLength(0);
        int cols = intensityMatrix.GetLength(1);
        var point3DMatrix = new Point3D[rows, cols];
        
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                // X, Y 坐标对应矩阵索引，Z 坐标对应强度值
                point3DMatrix[i, j] = new Point3D(i, j, intensityMatrix[i, j]);
            }
        }
        
        return point3DMatrix;
    }
    
    /// <summary>
    /// 在 UI 线程上执行操作
    /// </summary>
    private void InvokeOnUIThread(Action action)
    {
        if (_uiThreadInvoker != null)
        {
            // 测试模式：使用注入的调用器
            _uiThreadInvoker(action);
        }
        else
        {
            // 生产模式：使用 UIThreadHelper
            UIThreadHelper.RunOnUIThread(action);
        }
    }
    
    /// <summary>
    /// 取消订阅所有事件
    /// </summary>
    public void UnsubscribeFromEvents()
    {
        _apiClient.VisualizationDataUpdated -= OnVisualizationDataUpdated;
        _apiClient.RawDataReceived -= OnRawDataReceived;
        
        // 释放节流辅助器
        _visualization2DThrottle?.Dispose();
        _visualization3DThrottle?.Dispose();
    }
}
