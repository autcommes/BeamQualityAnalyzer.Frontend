using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using ScottPlot;
using DataPoint = BeamQualityAnalyzer.WpfClient.ViewModels.DataPoint;

namespace BeamQualityAnalyzer.WpfClient.Views;

/// <summary>
/// ChartControl.xaml 的交互逻辑
/// 封装 ScottPlot 图表控件
/// </summary>
public partial class ChartControl : UserControl
{
    public static readonly DependencyProperty RawDataProperty =
        DependencyProperty.Register(
            nameof(RawData),
            typeof(ObservableCollection<DataPoint>),
            typeof(ChartControl),
            new PropertyMetadata(null, OnDataChanged));

    public static readonly DependencyProperty FittedCurveProperty =
        DependencyProperty.Register(
            nameof(FittedCurve),
            typeof(ObservableCollection<DataPoint>),
            typeof(ChartControl),
            new PropertyMetadata(null, OnDataChanged));

    public static readonly DependencyProperty XAxisLabelProperty =
        DependencyProperty.Register(
            nameof(XAxisLabel),
            typeof(string),
            typeof(ChartControl),
            new PropertyMetadata("X", OnAxisLabelChanged));

    public static readonly DependencyProperty YAxisLabelProperty =
        DependencyProperty.Register(
            nameof(YAxisLabel),
            typeof(string),
            typeof(ChartControl),
            new PropertyMetadata("Y", OnAxisLabelChanged));

    public ObservableCollection<DataPoint>? RawData
    {
        get => (ObservableCollection<DataPoint>?)GetValue(RawDataProperty);
        set => SetValue(RawDataProperty, value);
    }

    public ObservableCollection<DataPoint>? FittedCurve
    {
        get => (ObservableCollection<DataPoint>?)GetValue(FittedCurveProperty);
        set => SetValue(FittedCurveProperty, value);
    }

    public string XAxisLabel
    {
        get => (string)GetValue(XAxisLabelProperty);
        set => SetValue(XAxisLabelProperty, value);
    }

    public string YAxisLabel
    {
        get => (string)GetValue(YAxisLabelProperty);
        set => SetValue(YAxisLabelProperty, value);
    }

    public ChartControl()
    {
        InitializeComponent();
        InitializeChart();
    }

    private void InitializeChart()
    {
        // 配置深色主题
        WpfPlot.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#1E1E1E");
        WpfPlot.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#252526");
        
        // 配置坐标轴
        WpfPlot.Plot.Axes.Color(ScottPlot.Color.FromHex("#D4D4D4"));
        WpfPlot.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#3E3E42");
        
        // 初始刷新
        WpfPlot.Refresh();
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ChartControl control)
        {
            control.UpdateChart();
            
            // 订阅集合变化事件
            if (e.OldValue is ObservableCollection<DataPoint> oldCollection)
            {
                oldCollection.CollectionChanged -= control.OnCollectionChanged;
            }
            
            if (e.NewValue is ObservableCollection<DataPoint> newCollection)
            {
                newCollection.CollectionChanged += control.OnCollectionChanged;
            }
        }
    }

    private static void OnAxisLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ChartControl control)
        {
            control.UpdateAxisLabels();
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateChart();
    }

    private void UpdateChart()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(UpdateChart);
            return;
        }

        WpfPlot.Plot.Clear();

        // 添加原始数据散点
        if (RawData != null && RawData.Count > 0)
        {
            var xData = RawData.Select(p => p.X).ToArray();
            var yData = RawData.Select(p => p.Y).ToArray();
            
            var scatter = WpfPlot.Plot.Add.Scatter(xData, yData);
            scatter.Color = ScottPlot.Color.FromHex("#4EC9B0");
            scatter.LineWidth = 0;
            scatter.MarkerSize = 8;
            scatter.MarkerShape = MarkerShape.FilledCircle;
            scatter.LegendText = "Raw Data";
        }

        // 添加拟合曲线
        if (FittedCurve != null && FittedCurve.Count > 0)
        {
            var xData = FittedCurve.Select(p => p.X).ToArray();
            var yData = FittedCurve.Select(p => p.Y).ToArray();
            
            var line = WpfPlot.Plot.Add.Scatter(xData, yData);
            line.Color = ScottPlot.Color.FromHex("#007ACC");
            line.LineWidth = 2;
            line.MarkerSize = 0;
            line.LegendText = "Fitted Curve";
        }

        // 显示图例
        WpfPlot.Plot.ShowLegend();
        WpfPlot.Plot.Legend.BackgroundColor = ScottPlot.Color.FromHex("#2D2D30");
        WpfPlot.Plot.Legend.FontColor = ScottPlot.Color.FromHex("#D4D4D4");
        WpfPlot.Plot.Legend.OutlineColor = ScottPlot.Color.FromHex("#3E3E42");

        // 自动缩放
        WpfPlot.Plot.Axes.AutoScale();
        
        // 刷新显示
        WpfPlot.Refresh();
    }

    private void UpdateAxisLabels()
    {
        WpfPlot.Plot.XLabel(XAxisLabel);
        WpfPlot.Plot.YLabel(YAxisLabel);
        WpfPlot.Plot.Axes.Color(ScottPlot.Color.FromHex("#D4D4D4"));
        WpfPlot.Refresh();
    }
}
