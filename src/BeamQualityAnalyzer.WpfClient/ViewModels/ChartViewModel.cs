using System.Collections.ObjectModel;
using System.Globalization;
using BeamQualityAnalyzer.ApiClient;
using BeamQualityAnalyzer.Contracts.Dtos;
using BeamQualityAnalyzer.Contracts.Messages;
using BeamQualityAnalyzer.WpfClient.Helpers;
using CommunityToolkit.Mvvm.Input;

namespace BeamQualityAnalyzer.WpfClient.ViewModels;

/// <summary>
/// 图表工作区 ViewModel
/// 管理曲线图显示和参数表格
/// </summary>
/// <remarks>
/// Requirements:
/// - 3.3: 显示散点数据（原始采样点）
/// - 4.3: 计算 X 方向和 Y 方向的 M² 因子
/// - 4.4: 计算光束腰斑位置和腰斑直径
/// - 4.5: 计算峰值位置坐标
/// - 5.2: 显示 M² 因子（X、Y、全局）
/// - 5.3: 显示峰值位置坐标
/// - 5.4: 显示腰斑位置和腰斑直径
/// - 6.2: 使用新参数重新执行算法计算
/// - 6.4: 更新所有相关的曲线图和参数表格
/// - 6.5: 显示验证错误提示并阻止计算
/// - 15.8: 图表更新使用节流机制（Throttle）
/// - 17.2: 确保 UI 线程不被阻塞
/// </remarks>
public class ChartViewModel : ViewModelBase
{
    private readonly IBeamAnalyzerApiClient _apiClient;
    private readonly Action<Action>? _uiThreadInvoker;
    private readonly ThrottleHelper _chartUpdateThrottle;
    private readonly ThrottleHelper _parameterUpdateThrottle;
    
    // 数据集合
    private ObservableCollection<DataPoint> _rawDataX = new();
    private ObservableCollection<DataPoint> _rawDataY = new();
    private ObservableCollection<DataPoint> _fittedCurveX = new();
    private ObservableCollection<DataPoint> _fittedCurveY = new();
    
    // 参数表格数据
    private ObservableCollection<ParameterRow> _parameters = new();
    
    // 当前选中的标签页
    private int _selectedTabIndex;
    
    // 重新计算参数
    private double _magnification = 1.0;
    private double _line86Result;
    private double _secondOrderFitResult;
    
    // 验证错误
    private string? _validationError;
    
    /// <summary>
    /// X 方向原始数据点
    /// </summary>
    public ObservableCollection<DataPoint> RawDataX
    {
        get => _rawDataX;
        set => SetProperty(ref _rawDataX, value);
    }
    
    /// <summary>
    /// Y 方向原始数据点
    /// </summary>
    public ObservableCollection<DataPoint> RawDataY
    {
        get => _rawDataY;
        set => SetProperty(ref _rawDataY, value);
    }
    
    /// <summary>
    /// X 方向拟合曲线
    /// </summary>
    public ObservableCollection<DataPoint> FittedCurveX
    {
        get => _fittedCurveX;
        set => SetProperty(ref _fittedCurveX, value);
    }
    
    /// <summary>
    /// Y 方向拟合曲线
    /// </summary>
    public ObservableCollection<DataPoint> FittedCurveY
    {
        get => _fittedCurveY;
        set => SetProperty(ref _fittedCurveY, value);
    }
    
    /// <summary>
    /// 参数表格数据
    /// </summary>
    public ObservableCollection<ParameterRow> Parameters
    {
        get => _parameters;
        set => SetProperty(ref _parameters, value);
    }
    
    /// <summary>
    /// 当前选中的标签页索引
    /// </summary>
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }
    
    /// <summary>
    /// 倍率
    /// </summary>
    public double Magnification
    {
        get => _magnification;
        set
        {
            if (SetProperty(ref _magnification, value))
            {
                ValidateParameters();
            }
        }
    }
    
    /// <summary>
    /// 86线结果
    /// </summary>
    public double Line86Result
    {
        get => _line86Result;
        set
        {
            if (SetProperty(ref _line86Result, value))
            {
                ValidateParameters();
            }
        }
    }
    
    /// <summary>
    /// 二阶拟合结果
    /// </summary>
    public double SecondOrderFitResult
    {
        get => _secondOrderFitResult;
        set
        {
            if (SetProperty(ref _secondOrderFitResult, value))
            {
                ValidateParameters();
            }
        }
    }
    
    /// <summary>
    /// 验证错误信息
    /// </summary>
    public string? ValidationError
    {
        get => _validationError;
        private set => SetProperty(ref _validationError, value);
    }
    
    /// <summary>
    /// 是否有验证错误
    /// </summary>
    public bool HasValidationError => !string.IsNullOrEmpty(ValidationError);
    
    /// <summary>
    /// 重新计算命令
    /// </summary>
    public IAsyncRelayCommand RecalculateCommand { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="apiClient">API 客户端</param>
    /// <param name="uiThreadInvoker">UI 线程调用器（用于测试注入）</param>
    public ChartViewModel(IBeamAnalyzerApiClient apiClient, Action<Action>? uiThreadInvoker = null)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _uiThreadInvoker = uiThreadInvoker;
        
        // 创建节流辅助器
        // Requirement 17.3: 2D 曲线图更新在 200ms 内完成
        _chartUpdateThrottle = new ThrottleHelper(200);
        
        // Requirement 5.5: 参数表格刷新在 100ms 内完成
        _parameterUpdateThrottle = new ThrottleHelper(100);
        
        // 创建命令
        RecalculateCommand = new AsyncRelayCommand(
            RecalculateAsync,
            () => !HasValidationError);
        
        // 订阅事件
        SubscribeToEvents();
        
        // 初始化参数表格
        InitializeParameters();
    }
    
    /// <summary>
    /// 订阅 API 客户端事件
    /// </summary>
    private void SubscribeToEvents()
    {
        _apiClient.RawDataReceived += OnRawDataReceived;
        _apiClient.CalculationCompleted += OnCalculationCompleted;
    }
    
    /// <summary>
    /// 初始化参数表格
    /// </summary>
    private void InitializeParameters()
    {
        Parameters = new ObservableCollection<ParameterRow>
        {
            new ParameterRow { Name = "M² 因子", GlobalValue = "-", XValue = "-", YValue = "-" },
            new ParameterRow { Name = "峰值位置", GlobalValue = "-", XValue = "-", YValue = "-" },
            new ParameterRow { Name = "腰斑位置 (mm)", GlobalValue = "-", XValue = "-", YValue = "-" },
            new ParameterRow { Name = "腰斑直径 (μm)", GlobalValue = "-", XValue = "-", YValue = "-" }
        };
    }
    
    /// <summary>
    /// 原始数据接收处理
    /// </summary>
    /// <remarks>
    /// Requirement 15.8: 图表更新使用节流机制（Throttle）
    /// Requirement 17.2: 确保 UI 线程不被阻塞
    /// Requirement 17.3: 2D 曲线图降采样（最多 1000 点）
    /// 
    /// 使用节流机制限制更新频率，避免高频数据导致 UI 卡顿
    /// 使用降采样减少数据点数量，提高渲染性能
    /// </remarks>
    private void OnRawDataReceived(object? sender, RawDataReceivedMessage e)
    {
        if (e.DataPoints == null || e.DataPoints.Length == 0)
            return;
        
        // 使用节流机制更新原始数据点
        _chartUpdateThrottle.Throttle(() =>
        {
            // 在后台线程处理数据转换和降采样
            var dataPointsX = e.DataPoints.Select(p => new DataPoint(p.DetectorPosition, p.BeamDiameterX)).ToList();
            var dataPointsY = e.DataPoints.Select(p => new DataPoint(p.DetectorPosition, p.BeamDiameterY)).ToList();
            
            // 降采样（如果数据点超过 1000 个）
            if (DataDownsamplingHelper.NeedsDownsampling(dataPointsX.Count))
            {
                dataPointsX = DataDownsamplingHelper.Downsample(dataPointsX);
                dataPointsY = DataDownsamplingHelper.Downsample(dataPointsY);
            }
            
            // 在 UI 线程上执行更新
            InvokeOnUIThread(() =>
            {
                // 清空现有数据
                RawDataX.Clear();
                RawDataY.Clear();
                
                // 添加降采样后的数据点
                foreach (var point in dataPointsX)
                {
                    RawDataX.Add(point);
                }
                
                foreach (var point in dataPointsY)
                {
                    RawDataY.Add(point);
                }
            });
        });
    }
    
    /// <summary>
    /// 计算完成处理
    /// </summary>
    /// <remarks>
    /// Requirement 15.8: 图表更新使用节流机制（Throttle）
    /// Requirement 17.2: 确保 UI 线程不被阻塞
    /// </remarks>
    private void OnCalculationCompleted(object? sender, CalculationCompletedMessage e)
    {
        // 使用节流机制更新拟合曲线
        _chartUpdateThrottle.Throttle(() =>
        {
            InvokeOnUIThread(() => UpdateFittedCurves(e));
        });
        
        // 使用节流机制更新参数表格
        _parameterUpdateThrottle.Throttle(() =>
        {
            InvokeOnUIThread(() => UpdateParameters(e));
        });
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
    /// 更新拟合曲线
    /// </summary>
    /// <remarks>
    /// Requirement 17.3: 2D 曲线图降采样（最多 1000 点）
    /// </remarks>
    private void UpdateFittedCurves(CalculationCompletedMessage result)
    {
        // 更新 X 方向拟合曲线
        FittedCurveX.Clear();
        if (result.HyperbolicFitX?.FittedCurve != null && RawDataX.Count > 0)
        {
            var positions = RawDataX.Select(p => p.X).ToArray();
            var fittedValues = result.HyperbolicFitX.FittedCurve;
            
            var fittedPoints = new List<DataPoint>();
            for (int i = 0; i < Math.Min(positions.Length, fittedValues.Length); i++)
            {
                fittedPoints.Add(new DataPoint(positions[i], fittedValues[i]));
            }
            
            // 降采样（如果数据点超过 1000 个）
            if (DataDownsamplingHelper.NeedsDownsampling(fittedPoints.Count))
            {
                fittedPoints = DataDownsamplingHelper.Downsample(fittedPoints);
            }
            
            foreach (var point in fittedPoints)
            {
                FittedCurveX.Add(point);
            }
        }
        
        // 更新 Y 方向拟合曲线
        FittedCurveY.Clear();
        if (result.HyperbolicFitY?.FittedCurve != null && RawDataY.Count > 0)
        {
            var positions = RawDataY.Select(p => p.X).ToArray();
            var fittedValues = result.HyperbolicFitY.FittedCurve;
            
            var fittedPoints = new List<DataPoint>();
            for (int i = 0; i < Math.Min(positions.Length, fittedValues.Length); i++)
            {
                fittedPoints.Add(new DataPoint(positions[i], fittedValues[i]));
            }
            
            // 降采样（如果数据点超过 1000 个）
            if (DataDownsamplingHelper.NeedsDownsampling(fittedPoints.Count))
            {
                fittedPoints = DataDownsamplingHelper.Downsample(fittedPoints);
            }
            
            foreach (var point in fittedPoints)
            {
                FittedCurveY.Add(point);
            }
        }
    }
    
    /// <summary>
    /// 更新参数表格
    /// </summary>
    private void UpdateParameters(CalculationCompletedMessage result)
    {
        // M² 因子
        Parameters[0].GlobalValue = FormatValue(result.MSquaredGlobal);
        Parameters[0].XValue = FormatValue(result.MSquaredX);
        Parameters[0].YValue = FormatValue(result.MSquaredY);
        
        // 峰值位置
        Parameters[1].GlobalValue = "-";
        Parameters[1].XValue = FormatValue(result.PeakPositionX);
        Parameters[1].YValue = FormatValue(result.PeakPositionY);
        
        // 腰斑位置
        Parameters[2].GlobalValue = "-";
        Parameters[2].XValue = FormatValue(result.BeamWaistPositionX);
        Parameters[2].YValue = FormatValue(result.BeamWaistPositionY);
        
        // 腰斑直径
        Parameters[3].GlobalValue = "-";
        Parameters[3].XValue = FormatValue(result.BeamWaistDiameterX);
        Parameters[3].YValue = FormatValue(result.BeamWaistDiameterY);
    }
    
    /// <summary>
    /// 格式化数值（保留4位有效数字）
    /// </summary>
    /// <remarks>
    /// Requirement 5.6: 对数值参数保留 4 位有效数字
    /// </remarks>
    private string FormatValue(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return "-";
        
        // 保留4位有效数字
        return value.ToString("G4", CultureInfo.InvariantCulture);
    }
    
    /// <summary>
    /// 验证参数
    /// </summary>
    /// <remarks>
    /// Requirement 6.5: 显示验证错误并阻止计算执行
    /// </remarks>
    private void ValidateParameters()
    {
        ValidationError = null;
        
        // 验证倍率（不能为负数或零）
        if (Magnification <= 0)
        {
            ValidationError = "倍率必须大于 0";
        }
        
        // 通知命令状态变化
        OnPropertyChanged(nameof(HasValidationError));
        RecalculateCommand.NotifyCanExecuteChanged();
    }
    
    /// <summary>
    /// 重新计算
    /// </summary>
    private async Task RecalculateAsync()
    {
        try
        {
            // 创建分析参数
            var parameters = new AnalysisParametersDto
            {
                Magnification = Magnification,
                Line86Result = Line86Result,
                SecondOrderFitResult = SecondOrderFitResult
            };
            
            // 调用 API 重新计算
            var result = await _apiClient.RecalculateAnalysisAsync(parameters);
            
            if (!result.Success)
            {
                ValidationError = result.Message ?? "重新计算失败";
            }
        }
        catch (Exception ex)
        {
            ValidationError = $"重新计算失败: {ex.Message}";
        }
    }
    
    /// <summary>
    /// 取消订阅所有事件
    /// </summary>
    public void UnsubscribeFromEvents()
    {
        _apiClient.RawDataReceived -= OnRawDataReceived;
        _apiClient.CalculationCompleted -= OnCalculationCompleted;
        
        // 释放节流辅助器
        _chartUpdateThrottle?.Dispose();
        _parameterUpdateThrottle?.Dispose();
    }
}

/// <summary>
/// 数据点（用于图表绑定）
/// </summary>
public class DataPoint
{
    /// <summary>
    /// X 坐标
    /// </summary>
    public double X { get; set; }
    
    /// <summary>
    /// Y 坐标
    /// </summary>
    public double Y { get; set; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public DataPoint(double x, double y)
    {
        X = x;
        Y = y;
    }
}

/// <summary>
/// 参数表格行（用于参数表格绑定）
/// </summary>
public class ParameterRow
{
    /// <summary>
    /// 参数名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 全局值
    /// </summary>
    public string GlobalValue { get; set; } = "-";
    
    /// <summary>
    /// X 方向值
    /// </summary>
    public string XValue { get; set; } = "-";
    
    /// <summary>
    /// Y 方向值
    /// </summary>
    public string YValue { get; set; } = "-";
}
