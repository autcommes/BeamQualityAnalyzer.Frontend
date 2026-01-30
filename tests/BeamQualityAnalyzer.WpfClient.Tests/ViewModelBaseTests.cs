using System.ComponentModel;
using BeamQualityAnalyzer.WpfClient.ViewModels;

namespace BeamQualityAnalyzer.WpfClient.Tests;

/// <summary>
/// Tests for ViewModelBase to verify property change notification.
/// 
/// Property 15: Property changes should trigger UI updates
/// Validates Requirement: 15.7
/// </summary>
public class ViewModelBaseTests
{
    /// <summary>
    /// Test ViewModel that exposes a simple property for testing.
    /// </summary>
    private class TestViewModel : ViewModelBase
    {
        private string _testProperty = string.Empty;
        private int _numericProperty;
        private bool _booleanProperty;

        public string TestProperty
        {
            get => _testProperty;
            set => SetProperty(ref _testProperty, value);
        }

        public int NumericProperty
        {
            get => _numericProperty;
            set => SetProperty(ref _numericProperty, value);
        }

        public bool BooleanProperty
        {
            get => _booleanProperty;
            set => SetProperty(ref _booleanProperty, value);
        }
    }

    [Fact]
    public void PropertyChange_ShouldRaisePropertyChangedEvent()
    {
        // Arrange
        var viewModel = new TestViewModel();
        var propertyChangedRaised = false;
        string? changedPropertyName = null;

        viewModel.PropertyChanged += (sender, args) =>
        {
            propertyChangedRaised = true;
            changedPropertyName = args.PropertyName;
        };

        // Act
        viewModel.TestProperty = "New Value";

        // Assert
        Assert.True(propertyChangedRaised, "PropertyChanged event should be raised when property value changes");
        Assert.Equal(nameof(TestViewModel.TestProperty), changedPropertyName);
    }

    [Fact]
    public void PropertyChange_WithSameValue_ShouldNotRaisePropertyChangedEvent()
    {
        // Arrange
        var viewModel = new TestViewModel();
        viewModel.TestProperty = "Initial Value";
        
        var propertyChangedRaised = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            propertyChangedRaised = true;
        };

        // Act
        viewModel.TestProperty = "Initial Value"; // Same value

        // Assert
        Assert.False(propertyChangedRaised, "PropertyChanged event should not be raised when value doesn't change");
    }

    [Fact]
    public void NumericPropertyChange_ShouldRaisePropertyChangedEvent()
    {
        // Arrange
        var viewModel = new TestViewModel();
        var propertyChangedRaised = false;
        string? changedPropertyName = null;

        viewModel.PropertyChanged += (sender, args) =>
        {
            propertyChangedRaised = true;
            changedPropertyName = args.PropertyName;
        };

        // Act
        viewModel.NumericProperty = 42;

        // Assert
        Assert.True(propertyChangedRaised, "PropertyChanged event should be raised for numeric property changes");
        Assert.Equal(nameof(TestViewModel.NumericProperty), changedPropertyName);
        Assert.Equal(42, viewModel.NumericProperty);
    }

    [Fact]
    public void BooleanPropertyChange_ShouldRaisePropertyChangedEvent()
    {
        // Arrange
        var viewModel = new TestViewModel();
        var propertyChangedRaised = false;
        string? changedPropertyName = null;

        viewModel.PropertyChanged += (sender, args) =>
        {
            propertyChangedRaised = true;
            changedPropertyName = args.PropertyName;
        };

        // Act
        viewModel.BooleanProperty = true;

        // Assert
        Assert.True(propertyChangedRaised, "PropertyChanged event should be raised for boolean property changes");
        Assert.Equal(nameof(TestViewModel.BooleanProperty), changedPropertyName);
        Assert.True(viewModel.BooleanProperty);
    }

    [Fact]
    public void MultiplePropertyChanges_ShouldRaiseMultipleEvents()
    {
        // Arrange
        var viewModel = new TestViewModel();
        var eventCount = 0;
        var changedProperties = new List<string>();

        viewModel.PropertyChanged += (sender, args) =>
        {
            eventCount++;
            if (args.PropertyName != null)
            {
                changedProperties.Add(args.PropertyName);
            }
        };

        // Act
        viewModel.TestProperty = "Value 1";
        viewModel.NumericProperty = 10;
        viewModel.BooleanProperty = true;

        // Assert
        Assert.Equal(3, eventCount);
        Assert.Contains(nameof(TestViewModel.TestProperty), changedProperties);
        Assert.Contains(nameof(TestViewModel.NumericProperty), changedProperties);
        Assert.Contains(nameof(TestViewModel.BooleanProperty), changedProperties);
    }

    [Fact]
    public void ViewModelBase_ShouldImplementINotifyPropertyChanged()
    {
        // Arrange & Act
        var viewModel = new TestViewModel();

        // Assert
        Assert.IsAssignableFrom<INotifyPropertyChanged>(viewModel);
    }

    [Fact]
    public void PropertyChangedEvent_ShouldProvideCorrectSender()
    {
        // Arrange
        var viewModel = new TestViewModel();
        object? eventSender = null;

        viewModel.PropertyChanged += (sender, args) =>
        {
            eventSender = sender;
        };

        // Act
        viewModel.TestProperty = "Test";

        // Assert
        Assert.Same(viewModel, eventSender);
    }
}
