using System.Windows;
using BeamQualityAnalyzer.WpfClient.ViewModels;

namespace BeamQualityAnalyzer.WpfClient.Views;

/// <summary>
/// SettingsDialog.xaml 的交互逻辑
/// </summary>
public partial class SettingsDialog : Window
{
    public SettingsDialog()
    {
        InitializeComponent();
    }
    
    /// <summary>
    /// 获取或设置对话框结果（用户是否点击了保存）
    /// </summary>
    public bool IsSaved { get; set; }
}
