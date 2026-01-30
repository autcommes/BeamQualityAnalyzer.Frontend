using System.Windows;
using System.Windows.Threading;

namespace BeamQualityAnalyzer.WpfClient.Helpers;

/// <summary>
/// UI 线程调度辅助类
/// 确保所有 UI 更新操作在 UI 线程上执行
/// </summary>
/// <remarks>
/// Requirement 15.8: 确保 UI 线程不被阻塞
/// Requirement 17.2: 所有耗时操作使用 Task.Run 在后台线程执行
/// 
/// 使用场景：
/// - 从后台线程更新 UI 元素
/// - 从事件处理器更新 ViewModel 属性
/// - 确保线程安全的 UI 操作
/// </remarks>
public static class UIThreadHelper
{
    /// <summary>
    /// 在 UI 线程上执行操作（同步）
    /// </summary>
    /// <param name="action">要执行的操作</param>
    /// <remarks>
    /// 如果当前已在 UI 线程，直接执行
    /// 否则，使用 Dispatcher.Invoke 切换到 UI 线程
    /// </remarks>
    public static void RunOnUIThread(Action action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));
        
        var dispatcher = Application.Current?.Dispatcher;
        
        if (dispatcher == null)
        {
            // 没有 Dispatcher（测试环境或非 WPF 应用）
            action();
            return;
        }
        
        if (dispatcher.CheckAccess())
        {
            // 当前已在 UI 线程，直接执行
            action();
        }
        else
        {
            // 切换到 UI 线程执行
            dispatcher.Invoke(action, DispatcherPriority.Normal);
        }
    }
    
    /// <summary>
    /// 在 UI 线程上执行操作（异步）
    /// </summary>
    /// <param name="action">要执行的操作</param>
    /// <returns>异步任务</returns>
    /// <remarks>
    /// 如果当前已在 UI 线程，直接执行
    /// 否则，使用 Dispatcher.InvokeAsync 切换到 UI 线程
    /// </remarks>
    public static async Task RunOnUIThreadAsync(Action action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));
        
        var dispatcher = Application.Current?.Dispatcher;
        
        if (dispatcher == null)
        {
            // 没有 Dispatcher（测试环境或非 WPF 应用）
            action();
            return;
        }
        
        if (dispatcher.CheckAccess())
        {
            // 当前已在 UI 线程，直接执行
            action();
        }
        else
        {
            // 切换到 UI 线程执行
            await dispatcher.InvokeAsync(action, DispatcherPriority.Normal);
        }
    }
    
    /// <summary>
    /// 在 UI 线程上执行操作（异步，带返回值）
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="func">要执行的函数</param>
    /// <returns>函数返回值</returns>
    public static async Task<T> RunOnUIThreadAsync<T>(Func<T> func)
    {
        if (func == null)
            throw new ArgumentNullException(nameof(func));
        
        var dispatcher = Application.Current?.Dispatcher;
        
        if (dispatcher == null)
        {
            // 没有 Dispatcher（测试环境或非 WPF 应用）
            return func();
        }
        
        if (dispatcher.CheckAccess())
        {
            // 当前已在 UI 线程，直接执行
            return func();
        }
        else
        {
            // 切换到 UI 线程执行
            return await dispatcher.InvokeAsync(func, DispatcherPriority.Normal);
        }
    }
    
    /// <summary>
    /// 在后台线程上执行耗时操作
    /// </summary>
    /// <param name="action">要执行的操作</param>
    /// <returns>异步任务</returns>
    /// <remarks>
    /// 使用 Task.Run 在后台线程执行，避免阻塞 UI 线程
    /// Requirement 17.2: 所有耗时操作使用 Task.Run 在后台线程执行
    /// </remarks>
    public static Task RunOnBackgroundThreadAsync(Action action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));
        
        return Task.Run(action);
    }
    
    /// <summary>
    /// 在后台线程上执行耗时操作（带返回值）
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="func">要执行的函数</param>
    /// <returns>函数返回值</returns>
    public static Task<T> RunOnBackgroundThreadAsync<T>(Func<T> func)
    {
        if (func == null)
            throw new ArgumentNullException(nameof(func));
        
        return Task.Run(func);
    }
    
    /// <summary>
    /// 在后台线程上执行耗时操作，然后在 UI 线程上更新结果
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="backgroundWork">后台工作</param>
    /// <param name="uiUpdate">UI 更新操作</param>
    /// <returns>异步任务</returns>
    /// <remarks>
    /// 典型使用场景：
    /// 1. 在后台线程执行数据处理
    /// 2. 在 UI 线程更新 ViewModel 属性
    /// </remarks>
    public static async Task RunWithUIUpdateAsync<T>(Func<T> backgroundWork, Action<T> uiUpdate)
    {
        if (backgroundWork == null)
            throw new ArgumentNullException(nameof(backgroundWork));
        if (uiUpdate == null)
            throw new ArgumentNullException(nameof(uiUpdate));
        
        // 在后台线程执行工作
        var result = await RunOnBackgroundThreadAsync(backgroundWork);
        
        // 在 UI 线程更新结果
        await RunOnUIThreadAsync(() => uiUpdate(result));
    }
    
    /// <summary>
    /// 检查当前是否在 UI 线程
    /// </summary>
    /// <returns>如果在 UI 线程返回 true，否则返回 false</returns>
    public static bool IsOnUIThread()
    {
        var dispatcher = Application.Current?.Dispatcher;
        return dispatcher?.CheckAccess() ?? true;
    }
}

