@echo off
chcp 65001 >nul
echo 正在清理前端构建产物...

echo 清理 bin 目录...
for /d /r . %%d in (bin) do @if exist "%%d" rd /s /q "%%d"

echo 清理 obj 目录...
for /d /r . %%d in (obj) do @if exist "%%d" rd /s /q "%%d"

echo 清理日志文件...
if exist "src\BeamQualityAnalyzer.WpfClient\logs" rd /s /q "src\BeamQualityAnalyzer.WpfClient\logs"

echo 清理本地数据库...
if exist "src\BeamQualityAnalyzer.WpfClient\config.db" del /f /q "src\BeamQualityAnalyzer.WpfClient\config.db"
if exist "src\BeamQualityAnalyzer.WpfClient\config.db-shm" del /f /q "src\BeamQualityAnalyzer.WpfClient\config.db-shm"
if exist "src\BeamQualityAnalyzer.WpfClient\config.db-wal" del /f /q "src\BeamQualityAnalyzer.WpfClient\config.db-wal"

echo 清理 WPF 临时文件...
del /s /q /f *_wpftmp.csproj 2>nul
for /d /r . %%d in (*_wpftmp.csproj.nuget.*) do @if exist "%%d" rd /s /q "%%d"

echo 清理 NuGet 包缓存...
for /d /r . %%d in (packages) do @if exist "%%d" rd /s /q "%%d"

echo 前端清理完成！
pause
