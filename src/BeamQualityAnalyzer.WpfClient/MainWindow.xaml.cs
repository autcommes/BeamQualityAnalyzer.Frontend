using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BeamQualityAnalyzer.WpfClient.ViewModels;

namespace BeamQualityAnalyzer.WpfClient;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += OnWindowClosing;
    }
    
    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // 清理 ViewModel 资源
        if (DataContext is MainViewModel mainViewModel)
        {
            mainViewModel.UnsubscribeFromEvents();
        }
    }
}