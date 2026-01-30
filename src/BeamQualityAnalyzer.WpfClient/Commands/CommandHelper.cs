using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace BeamQualityAnalyzer.WpfClient.Commands;

/// <summary>
/// Helper class for creating and managing commands in ViewModels.
/// Provides factory methods for creating RelayCommand and AsyncRelayCommand instances.
/// </summary>
/// <remarks>
/// This class simplifies command creation by providing:
/// - Factory methods for synchronous commands (RelayCommand)
/// - Factory methods for asynchronous commands (AsyncRelayCommand)
/// - Support for commands with and without parameters
/// - Support for CanExecute predicates
/// 
/// Requirements:
/// - 2.1: Data acquisition control commands
/// - 2.2: Emergency stop command
/// - 2.3: Device reset command
/// </remarks>
public static class CommandHelper
{
    /// <summary>
    /// Creates a synchronous command without parameters.
    /// </summary>
    /// <param name="execute">The action to execute when the command is invoked.</param>
    /// <param name="canExecute">Optional predicate to determine if the command can execute.</param>
    /// <returns>A new RelayCommand instance.</returns>
    public static IRelayCommand CreateCommand(Action execute, Func<bool>? canExecute = null)
    {
        return canExecute == null
            ? new RelayCommand(execute)
            : new RelayCommand(execute, canExecute);
    }

    /// <summary>
    /// Creates a synchronous command with a parameter.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    /// <param name="execute">The action to execute when the command is invoked.</param>
    /// <param name="canExecute">Optional predicate to determine if the command can execute.</param>
    /// <returns>A new RelayCommand instance.</returns>
    public static IRelayCommand<T> CreateCommand<T>(Action<T?> execute, Predicate<T?>? canExecute = null)
    {
        return canExecute == null
            ? new RelayCommand<T>(execute)
            : new RelayCommand<T>(execute, canExecute);
    }

    /// <summary>
    /// Creates an asynchronous command without parameters.
    /// </summary>
    /// <param name="execute">The async function to execute when the command is invoked.</param>
    /// <param name="canExecute">Optional predicate to determine if the command can execute.</param>
    /// <returns>A new AsyncRelayCommand instance.</returns>
    public static IAsyncRelayCommand CreateAsyncCommand(
        Func<Task> execute, 
        Func<bool>? canExecute = null)
    {
        return canExecute == null
            ? new AsyncRelayCommand(execute)
            : new AsyncRelayCommand(execute, canExecute);
    }

    /// <summary>
    /// Creates an asynchronous command with a parameter.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    /// <param name="execute">The async function to execute when the command is invoked.</param>
    /// <param name="canExecute">Optional predicate to determine if the command can execute.</param>
    /// <returns>A new AsyncRelayCommand instance.</returns>
    public static IAsyncRelayCommand<T> CreateAsyncCommand<T>(
        Func<T?, Task> execute, 
        Predicate<T?>? canExecute = null)
    {
        return canExecute == null
            ? new AsyncRelayCommand<T>(execute)
            : new AsyncRelayCommand<T>(execute, canExecute);
    }

    /// <summary>
    /// Creates an asynchronous command with cancellation support.
    /// </summary>
    /// <param name="execute">The async function to execute when the command is invoked.</param>
    /// <param name="canExecute">Optional predicate to determine if the command can execute.</param>
    /// <returns>A new AsyncRelayCommand instance with cancellation support.</returns>
    public static IAsyncRelayCommand CreateCancellableAsyncCommand(
        Func<CancellationToken, Task> execute,
        Func<bool>? canExecute = null)
    {
        return canExecute == null
            ? new AsyncRelayCommand(execute)
            : new AsyncRelayCommand(execute, canExecute);
    }

    /// <summary>
    /// Creates an asynchronous command with parameter and cancellation support.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    /// <param name="execute">The async function to execute when the command is invoked.</param>
    /// <param name="canExecute">Optional predicate to determine if the command can execute.</param>
    /// <returns>A new AsyncRelayCommand instance with cancellation support.</returns>
    public static IAsyncRelayCommand<T> CreateCancellableAsyncCommand<T>(
        Func<T?, CancellationToken, Task> execute,
        Predicate<T?>? canExecute = null)
    {
        return canExecute == null
            ? new AsyncRelayCommand<T>(execute)
            : new AsyncRelayCommand<T>(execute, canExecute);
    }
}
