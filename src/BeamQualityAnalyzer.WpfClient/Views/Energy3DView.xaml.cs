using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace BeamQualityAnalyzer.WpfClient.Views;

/// <summary>
/// 3D 能量分布可视化视图
/// </summary>
public partial class Energy3DView : UserControl
{
    private ModelVisual3D? _surfaceModel;
    
    public static readonly DependencyProperty EnergyDistributionDataProperty =
        DependencyProperty.Register(
            nameof(EnergyDistributionData),
            typeof(Point3D[,]),
            typeof(Energy3DView),
            new PropertyMetadata(null, OnEnergyDistributionDataChanged));
    
    public Point3D[,]? EnergyDistributionData
    {
        get => (Point3D[,]?)GetValue(EnergyDistributionDataProperty);
        set => SetValue(EnergyDistributionDataProperty, value);
    }
    
    public Energy3DView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }
    
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (EnergyDistributionData == null)
        {
            ShowDemoGaussianSurface();
        }
        AddAxisLabels();
    }
    
    /// <summary>
    /// 显示示例高斯曲面
    /// </summary>
    private void ShowDemoGaussianSurface()
    {
        int size = 80;
        var data = new Point3D[size, size];
        
        double amplitude = 250;
        double sigma = 60;
        
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                double x = (i - size / 2.0) * 6.25;  // -250 to 250
                double y = (j - size / 2.0) * 6.25;
                
                double z = amplitude * Math.Exp(-(x * x + y * y) / (2 * sigma * sigma));
                
                data[i, j] = new Point3D(x, y, z);
            }
        }
        
        UpdateSurface(data);
    }
    
    /// <summary>
    /// 添加坐标轴标签
    /// </summary>
    private void AddAxisLabels()
    {
        var labelColor = Colors.LightGray;
        
        // Z 轴标签（左侧）
        for (int z = 0; z <= 250; z += 50)
        {
            var label = new BillboardTextVisual3D
            {
                Text = z.ToString(),
                Position = new Point3D(-270, -250, z),
                Foreground = new SolidColorBrush(labelColor),
                FontSize = 12
            };
            Viewport3D.Children.Add(label);
        }
        
        // X 轴标签（底部前方）
        for (int x = -200; x <= 200; x += 100)
        {
            if (x == 0) continue;
            var label = new BillboardTextVisual3D
            {
                Text = x.ToString(),
                Position = new Point3D(x, 270, 0),
                Foreground = new SolidColorBrush(labelColor),
                FontSize = 12
            };
            Viewport3D.Children.Add(label);
        }
        
        // Y 轴标签（底部右侧）
        for (int y = -200; y <= 200; y += 100)
        {
            if (y == 0) continue;
            var label = new BillboardTextVisual3D
            {
                Text = y.ToString(),
                Position = new Point3D(270, y, 0),
                Foreground = new SolidColorBrush(labelColor),
                FontSize = 12
            };
            Viewport3D.Children.Add(label);
        }
    }
    
    private static void OnEnergyDistributionDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Energy3DView view && e.NewValue is Point3D[,] data)
        {
            view.UpdateSurface(data);
        }
    }
    
    /// <summary>
    /// 更新 3D Surface
    /// </summary>
    private void UpdateSurface(Point3D[,] data)
    {
        try
        {
            if (_surfaceModel != null)
                Viewport3D.Children.Remove(_surfaceModel);
            
            int rows = data.GetLength(0);
            int cols = data.GetLength(1);
            
            if (rows < 2 || cols < 2) return;
            
            // 找到 Z 值范围
            double minZ = double.MaxValue;
            double maxZ = double.MinValue;
            
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (data[i, j].Z < minZ) minZ = data[i, j].Z;
                    if (data[i, j].Z > maxZ) maxZ = data[i, j].Z;
                }
            }
            
            double zRange = maxZ - minZ;
            if (zRange < 0.001) zRange = 1;
            
            // 创建 Mesh
            var mesh = new MeshGeometry3D();
            var colors = new List<Color>();
            
            // 添加顶点
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    mesh.Positions.Add(data[i, j]);
                    
                    // 根据高度计算颜色
                    double normalizedZ = (data[i, j].Z - minZ) / zRange;
                    colors.Add(GetRainbowColor(normalizedZ));
                    
                    // 纹理坐标
                    mesh.TextureCoordinates.Add(new Point(normalizedZ, 0.5));
                }
            }
            
            // 添加三角形
            for (int i = 0; i < rows - 1; i++)
            {
                for (int j = 0; j < cols - 1; j++)
                {
                    int p00 = i * cols + j;
                    int p10 = (i + 1) * cols + j;
                    int p11 = (i + 1) * cols + (j + 1);
                    int p01 = i * cols + (j + 1);
                    
                    mesh.TriangleIndices.Add(p00);
                    mesh.TriangleIndices.Add(p10);
                    mesh.TriangleIndices.Add(p11);
                    
                    mesh.TriangleIndices.Add(p00);
                    mesh.TriangleIndices.Add(p11);
                    mesh.TriangleIndices.Add(p01);
                }
            }
            
            // 计算法线
            mesh.Normals = CalculateNormals(mesh);
            
            // 创建渐变材质
            var material = new DiffuseMaterial(CreateRainbowGradient());
            
            var geometryModel = new GeometryModel3D
            {
                Geometry = mesh,
                Material = material,
                BackMaterial = material
            };
            
            _surfaceModel = new ModelVisual3D { Content = geometryModel };
            Viewport3D.Children.Add(_surfaceModel);
            
            Viewport3D.ZoomExtents();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"更新 3D Surface 失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 计算法线
    /// </summary>
    private Vector3DCollection CalculateNormals(MeshGeometry3D mesh)
    {
        var normals = new Vector3DCollection();
        var normalAccumulator = new Vector3D[mesh.Positions.Count];
        
        for (int i = 0; i < mesh.TriangleIndices.Count; i += 3)
        {
            int i0 = mesh.TriangleIndices[i];
            int i1 = mesh.TriangleIndices[i + 1];
            int i2 = mesh.TriangleIndices[i + 2];
            
            var p0 = mesh.Positions[i0];
            var p1 = mesh.Positions[i1];
            var p2 = mesh.Positions[i2];
            
            var v1 = p1 - p0;
            var v2 = p2 - p0;
            var normal = Vector3D.CrossProduct(v1, v2);
            normal.Normalize();
            
            normalAccumulator[i0] += normal;
            normalAccumulator[i1] += normal;
            normalAccumulator[i2] += normal;
        }
        
        foreach (var n in normalAccumulator)
        {
            var normalized = n;
            normalized.Normalize();
            normals.Add(normalized);
        }
        
        return normals;
    }
    
    /// <summary>
    /// 获取彩虹颜色
    /// </summary>
    private Color GetRainbowColor(double value)
    {
        // 0 = 蓝, 0.25 = 青, 0.5 = 绿, 0.75 = 黄, 1 = 红
        value = Math.Max(0, Math.Min(1, value));
        
        double r, g, b;
        
        if (value < 0.25)
        {
            r = 0;
            g = value * 4;
            b = 1;
        }
        else if (value < 0.5)
        {
            r = 0;
            g = 1;
            b = 1 - (value - 0.25) * 4;
        }
        else if (value < 0.75)
        {
            r = (value - 0.5) * 4;
            g = 1;
            b = 0;
        }
        else
        {
            r = 1;
            g = 1 - (value - 0.75) * 4;
            b = 0;
        }
        
        return Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }
    
    /// <summary>
    /// 创建彩虹渐变
    /// </summary>
    private LinearGradientBrush CreateRainbowGradient()
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0.5),
            EndPoint = new Point(1, 0.5)
        };
        
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 255), 0.0));     // 蓝
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 255), 0.25));  // 青
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 0), 0.5));     // 绿
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 255, 0), 0.75));  // 黄
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 128, 0), 0.9));   // 橙
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 0, 0), 1.0));     // 红
        
        return brush;
    }
}
