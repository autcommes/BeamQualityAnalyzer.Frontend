using CommunityToolkit.Mvvm.ComponentModel;

namespace BeamQualityAnalyzer.WpfClient.ViewModels;

/// <summary>
/// Base class for all ViewModels in the application.
/// Inherits from ObservableObject to provide INotifyPropertyChanged implementation.
/// </summary>
/// <remarks>
/// This class provides the foundation for MVVM pattern implementation by:
/// - Implementing INotifyPropertyChanged through ObservableObject
/// - Providing property change notification infrastructure
/// - Supporting data binding between Views and ViewModels
/// 
/// Requirements: 15.7 - ViewModel property changes should trigger UI updates
/// </remarks>
public abstract class ViewModelBase : ObservableObject
{
    /// <summary>
    /// Gets a value indicating whether the ViewModel is currently in design mode.
    /// Useful for providing design-time data in XAML designer.
    /// </summary>
    protected static bool IsInDesignMode => 
        System.ComponentModel.DesignerProperties.GetIsInDesignMode(
            new System.Windows.DependencyObject());
}
