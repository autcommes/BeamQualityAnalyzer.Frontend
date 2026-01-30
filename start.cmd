@echo off
chcp 65001 >nul
echo 正在关闭旧的 WPF 客户端进程...
taskkill /F /IM BeamQualityAnalyzer.WpfClient.exe 2>nul
timeout /t 2 /nobreak >nul

echo 正在编译 WPF 客户端...
cd src\BeamQualityAnalyzer.WpfClient
dotnet build
if %errorlevel% neq 0 (
    echo 编译失败！
    pause
    exit /b %errorlevel%
)

echo 正在启动 WPF 客户端...
start "" "bin\Debug\net8.0-windows\BeamQualityAnalyzer.WpfClient.exe"
echo WPF 客户端已启动
cd ..\..
pause
