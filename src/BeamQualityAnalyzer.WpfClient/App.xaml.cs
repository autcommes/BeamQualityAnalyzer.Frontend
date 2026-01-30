using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Threading;
using Serilog;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BeamQualityAnalyzer.ApiClient;
using BeamQualityAnalyzer.WpfClient.Services;
using BeamQualityAnalyzer.WpfClient.ViewModels;

namespace BeamQualityAnalyzer.WpfClient;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IBeamAnalyzerApiClient? _apiClient;
    private IConfiguration? _configuration;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 配置 Serilog
        ConfigureSerilog();

        // 注册全局异常处理器
        RegisterGlobalExceptionHandlers();

        Log.Information("光束质量分析系统 WPF 客户端启动");
        
        // 初始化应用程序
        InitializeApplication();
    }

    private void ConfigureSerilog()
    {
        // 加载配置文件
        _configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // 配置 Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(_configuration)
            .Enrich.WithProperty("Application", "WpfClient")
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .Enrich.WithProperty("UserName", Environment.UserName)
            .CreateLogger();

        Log.Information("Serilog 日志系统已初始化");
    }
    
    private async void InitializeApplication()
    {
        try
        {
            Log.Information("正在初始化应用程序...");
            
            // 创建 API 客户端
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(Log.Logger);
            });
            var apiClientLogger = loggerFactory.CreateLogger<BeamAnalyzerApiClient>();
            _apiClient = new BeamAnalyzerApiClient(apiClientLogger);
            
            // 创建设置服务
            var settingsServiceLogger = loggerFactory.CreateLogger<SettingsService>();
            var settingsService = new SettingsService(settingsServiceLogger);
            
            // 创建主窗口和 ViewModel（先创建窗口，即使连接失败也能显示）
            var mainWindow = new MainWindow();
            var mainViewModel = new MainViewModel(_apiClient, settingsService);
            mainWindow.DataContext = mainViewModel;
            
            // 显示主窗口
            mainWindow.Show();
            
            // 加载配置
            var settings = await settingsService.LoadSettingsAsync();
            var serverUrl = settings?.ServerUrl ?? _configuration?["ServerUrl"] ?? "http://localhost:5000";
            
            Log.Information("服务器地址: {ServerUrl}", serverUrl);
            
            // 尝试连接到服务器（在后台进行，不阻塞窗口显示）
            _ = Task.Run(async () =>
            {
                try
                {
                    Log.Information("正在连接到服务器...");
                    await _apiClient.ConnectAsync(serverUrl);
                    Log.Information("已连接到服务器");
                    
                    // 连接成功后订阅数据流
                    await _apiClient.SubscribeToDataStreamAsync();
                    Log.Information("已订阅数据流");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "连接到服务器失败");
                    
                    // 在 UI 线程显示错误消息
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            $"无法连接到服务器:\n{ex.Message}\n\n请检查:\n1. 后端服务是否正在运行\n2. 服务器地址是否正确: {serverUrl}\n\n您可以在设置中修改服务器地址后重新连接。",
                            "连接失败",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    });
                }
            });
            
            Log.Information("应用程序初始化完成");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "应用程序初始化失败");
            MessageBox.Show(
                $"应用程序初始化失败:\n{ex.Message}",
                "初始化错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void RegisterGlobalExceptionHandlers()
    {
        // 1. UI线程未处理异常
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // 2. 非UI线程未处理异常
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // 3. Task未观察到的异常
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        Log.Information("全局异常处理器已注册");
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            // 记录异常到日志
            Log.Error(e.Exception, "UI线程未处理异常: {Message}", e.Exception.Message);

            // 显示友好的错误对话框
            ShowErrorDialog(e.Exception, "应用程序错误");

            // 标记异常已处理，防止应用程序崩溃
            e.Handled = true;
        }
        catch (Exception ex)
        {
            // 如果异常处理器本身出错，记录到日志
            Log.Fatal(ex, "异常处理器失败");
        }
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            var exception = e.ExceptionObject as Exception;
            if (exception != null)
            {
                // 记录异常到日志
                Log.Fatal(exception, "非UI线程未处理异常: {Message}, IsTerminating: {IsTerminating}", 
                    exception.Message, e.IsTerminating);

                // 如果应用程序即将终止，显示错误对话框
                if (e.IsTerminating)
                {
                    Dispatcher.Invoke(() =>
                    {
                        ShowErrorDialog(exception, "严重错误 - 应用程序即将关闭");
                    });
                }
            }
            else
            {
                Log.Fatal("非UI线程未处理异常: {ExceptionObject}, IsTerminating: {IsTerminating}", 
                    e.ExceptionObject, e.IsTerminating);
            }
        }
        catch (Exception ex)
        {
            // 如果异常处理器本身出错，记录到日志
            Log.Fatal(ex, "异常处理器失败");
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
            // 记录异常到日志
            Log.Error(e.Exception, "Task未观察到的异常: {Message}", e.Exception.Message);

            // 标记异常已观察，防止应用程序崩溃
            e.SetObserved();

            // 显示友好的错误对话框（在UI线程上）
            Dispatcher.Invoke(() =>
            {
                ShowErrorDialog(e.Exception, "后台任务错误");
            });
        }
        catch (Exception ex)
        {
            // 如果异常处理器本身出错，记录到日志
            Log.Fatal(ex, "异常处理器失败");
        }
    }

    private void ShowErrorDialog(Exception exception, string title)
    {
        try
        {
            // 构建友好的错误消息
            var message = $"发生了一个错误，但应用程序将继续运行。\n\n" +
                          $"错误类型: {exception.GetType().Name}\n" +
                          $"错误消息: {exception.Message}\n\n" +
                          $"详细信息已记录到日志文件。\n" +
                          $"如果问题持续存在，请联系技术支持。";

            // 显示错误对话框
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            // 如果显示对话框失败，至少记录到日志
            Log.Fatal(ex, "显示错误对话框失败");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("应用程序正在退出...");
        
        // 断开 API 客户端连接
        if (_apiClient != null)
        {
            try
            {
                // 使用 Task.Run 避免死锁，并设置超时
                var disconnectTask = Task.Run(async () => await _apiClient.DisconnectAsync());
                if (!disconnectTask.Wait(TimeSpan.FromSeconds(3)))
                {
                    Log.Warning("断开连接超时");
                }
                _apiClient.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "断开 API 客户端连接失败");
            }
        }
        
        // 取消注册异常处理器
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

        Log.Information("应用程序正常退出");
        Log.CloseAndFlush();

        base.OnExit(e);
    }
}

