using System.Timers;

namespace BeamQualityAnalyzer.WpfClient.Helpers;

/// <summary>
/// 节流辅助类
/// 用于限制高频操作的执行频率，避免过度刷新 UI
/// </summary>
/// <remarks>
/// Requirement 15.8: 图表更新使用节流机制（Throttle）
/// Requirement 17.2: 确保 UI 线程不被阻塞
/// 
/// 使用场景：
/// - 图表数据更新（限制刷新频率为 200ms）
/// - 3D 可视化更新（限制刷新频率为 300ms）
/// - 参数表格更新（限制刷新频率为 100ms）
/// </remarks>
public class ThrottleHelper : IDisposable
{
    private readonly System.Timers.Timer _timer;
    private Action? _pendingAction;
    private readonly object _lock = new object();
    private bool _disposed;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="intervalMs">节流间隔（毫秒）</param>
    public ThrottleHelper(double intervalMs)
    {
        _timer = new System.Timers.Timer(intervalMs);
        _timer.AutoReset = false;
        _timer.Elapsed += OnTimerElapsed;
    }
    
    /// <summary>
    /// 执行节流操作
    /// </summary>
    /// <param name="action">要执行的操作</param>
    /// <remarks>
    /// 如果在节流间隔内多次调用，只会执行最后一次操作
    /// </remarks>
    public void Throttle(Action action)
    {
        if (_disposed)
            return;
        
        lock (_lock)
        {
            // 保存待执行的操作
            _pendingAction = action;
            
            // 如果定时器未运行，立即执行并启动定时器
            if (!_timer.Enabled)
            {
                ExecutePendingAction();
                _timer.Start();
            }
            // 否则，操作将在定时器触发时执行
        }
    }
    
    /// <summary>
    /// 定时器触发处理
    /// </summary>
    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        lock (_lock)
        {
            // 如果有待执行的操作，执行它
            if (_pendingAction != null)
            {
                ExecutePendingAction();
                _timer.Start(); // 重新启动定时器
            }
        }
    }
    
    /// <summary>
    /// 执行待执行的操作
    /// </summary>
    private void ExecutePendingAction()
    {
        var action = _pendingAction;
        _pendingAction = null;
        
        try
        {
            action?.Invoke();
        }
        catch (Exception ex)
        {
            // 记录错误但不抛出，避免影响其他功能
            System.Diagnostics.Debug.WriteLine($"节流操作执行失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 立即执行待执行的操作（如果有）
    /// </summary>
    public void Flush()
    {
        if (_disposed)
            return;
        
        lock (_lock)
        {
            _timer.Stop();
            ExecutePendingAction();
        }
    }
    
    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        
        lock (_lock)
        {
            _timer.Stop();
            _timer.Dispose();
            _pendingAction = null;
            _disposed = true;
        }
    }
}

