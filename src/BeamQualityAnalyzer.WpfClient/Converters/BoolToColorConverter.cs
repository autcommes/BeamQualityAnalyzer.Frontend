using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BeamQualityAnalyzer.WpfClient;

/// <summary>
/// 布尔值到颜色的转换器
/// 用于连接状态指示器
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            // 已连接: 青绿色 #4EC9B0
            // 未连接: 红色 #F44747
            return isConnected
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4EC9B0"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44747"));
        }
        
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44747"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
