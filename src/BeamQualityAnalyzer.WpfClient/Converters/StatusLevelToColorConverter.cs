using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using BeamQualityAnalyzer.WpfClient.ViewModels;

namespace BeamQualityAnalyzer.WpfClient;

/// <summary>
/// 状态级别到颜色的转换器
/// </summary>
/// <remarks>
/// Requirement 14.2: 使用状态色标识不同状态
/// - Normal: #4EC9B0 (青绿色)
/// - Warning: #D7BA7D (黄色)
/// - Error: #F44747 (红色)
/// </remarks>
public class StatusLevelToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is StatusLevel level)
        {
            return level switch
            {
                StatusLevel.Normal => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4EC9B0")),
                StatusLevel.Warning => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D7BA7D")),
                StatusLevel.Error => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44747")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D4D4D4"))
            };
        }
        
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D4D4D4"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
