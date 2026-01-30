using System.Windows;
using System.Windows.Controls;
using ScottPlot;
using ScottPlot.Plottables;

namespace BeamQualityAnalyzer.WpfClient.Views;

/// <summary>
/// 2D 光斑可视化视图
/// 使用 ScottPlot Heatmap 显示伪彩色热力图
/// </summary>
/// <remarks>
/// Requirements:
/// - 7.1: 显示 2D 光斑伪彩色热力图
/// - 7.2: 在 2D 光斑图中心显示十字标识线
/// - 7.3: 支持 2D 光斑图的缩放和平移操作
/// - 7.5: 使用紫色背景色显示 2D 光斑图区域
/// - 7.6: 使用彩色梯度映射表示能量强度
/// </remarks>
public partial class Spot2DView : UserControl
{
    private Heatmap? _heatmap;
    private Crosshair? _crosshair;
    
    /// <summary>
    /// 光斑强度数据依赖属性
    /// </summary>
    public static readonly DependencyProperty SpotIntensityDataProperty =
        DependencyProperty.Register(
            nameof(SpotIntensityData),
            typeof(double[,]),
            typeof(Spot2DView),
            new PropertyMetadata(null, OnSpotIntensityDataChanged));
    
    /// <summary>
    /// 光斑中心依赖属性
    /// </summary>
    public static readonly DependencyProperty SpotCenterProperty =
        DependencyProperty.Register(
            nameof(SpotCenter),
            typeof(Point),
            typeof(Spot2DView),
            new PropertyMetadata(new Point(0, 0), OnSpotCenterChanged));
    
    /// <summary>
    /// 光斑强度数据
    /// </summary>
    public double[,]? SpotIntensityData
    {
        get => (double[,]?)GetValue(SpotIntensityDataProperty);
        set => SetValue(SpotIntensityDataProperty, value);
    }
    
    /// <summary>
    /// 光斑中心坐标
    /// </summary>
    public Point SpotCenter
    {
        get => (Point)GetValue(SpotCenterProperty);
        set => SetValue(SpotCenterProperty, value);
    }
    
    public Spot2DView()
    {
        InitializeComponent();
        InitializePlot();
    }
    
    /// <summary>
    /// 初始化 ScottPlot 图表
    /// </summary>
    private void InitializePlot()
    {
        // 设置紫色背景（Requirement 7.5）
        SpotPlot.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#2D1B3D");
        SpotPlot.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#2D1B3D");
        
        // 设置坐标轴样式
        SpotPlot.Plot.Axes.Color(ScottPlot.Color.FromHex("#D4D4D4"));
        SpotPlot.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#3E3E42");
        
        // 设置坐标轴标签（使用英文避免字体问题）
        SpotPlot.Plot.XLabel("X (px)");
        SpotPlot.Plot.YLabel("Y (px)");
        SpotPlot.Plot.Title("Spot Intensity");
        
        // 启用缩放和平移（Requirement 7.3）
        // ScottPlot 5.x 默认启用交互功能
        
        SpotPlot.Refresh();
    }
    
    /// <summary>
    /// 光斑强度数据变化处理
    /// </summary>
    private static void OnSpotIntensityDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Spot2DView view && e.NewValue is double[,] data)
        {
            view.UpdateHeatmap(data);
        }
    }
    
    /// <summary>
    /// 光斑中心变化处理
    /// </summary>
    private static void OnSpotCenterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Spot2DView view && e.NewValue is Point center)
        {
            view.UpdateCrosshair(center);
        }
    }
    
    /// <summary>
    /// 更新热力图
    /// </summary>
    /// <param name="data">强度数据矩阵</param>
    private void UpdateHeatmap(double[,] data)
    {
        try
        {
            // 移除旧的热力图
            if (_heatmap != null)
            {
                SpotPlot.Plot.Remove(_heatmap);
            }
            
            // 创建新的热力图（Requirement 7.1, 7.6）
            _heatmap = SpotPlot.Plot.Add.Heatmap(data);
            
            // 设置彩色梯度映射（Requirement 7.6）
            // 使用 Viridis 颜色映射表（从低能量到高能量：紫色→蓝色→绿色→黄色）
            _heatmap.Colormap = new ScottPlot.Colormaps.Viridis();
            
            // 添加颜色条
            SpotPlot.Plot.Add.ColorBar(_heatmap);
            
            // 自动调整坐标轴范围
            SpotPlot.Plot.Axes.AutoScale();
            
            // 刷新图表
            SpotPlot.Refresh();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"更新热力图失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 更新十字标识线
    /// </summary>
    /// <param name="center">中心坐标</param>
    private void UpdateCrosshair(Point center)
    {
        try
        {
            // 移除旧的十字线
            if (_crosshair != null)
            {
                SpotPlot.Plot.Remove(_crosshair);
            }
            
            // 创建新的十字线（Requirement 7.2）
            _crosshair = SpotPlot.Plot.Add.Crosshair(center.X, center.Y);
            
            // 设置十字线样式
            _crosshair.LineColor = ScottPlot.Color.FromHex("#FFFFFF");
            _crosshair.LineWidth = 1.5f;
            _crosshair.LinePattern = LinePattern.Solid;
            
            // 刷新图表
            SpotPlot.Refresh();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"更新十字线失败: {ex.Message}");
        }
    }
}
