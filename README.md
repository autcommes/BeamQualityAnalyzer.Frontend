# 光束质量分析系统 - 前端客户端

## 概述

光束质量分析系统前端客户端，基于 WPF + .NET 9.0 + MVVM 架构构建，提供工业上位机UI和实时数据可视化功能。

## 项目结构

```
BeamQualityAnalyzer.Frontend/
├─ src/
│   ├─ BeamQualityAnalyzer.WpfClient/       # WPF 客户端（当前实现）
│   ├─ BeamQualityAnalyzer.ApiClient/       # API 客户端封装库
│   ├─ BeamQualityAnalyzer.WinFormsClient/  # WinForms 客户端（未来）
│   └─ BeamQualityAnalyzer.WebClient/       # Vue3 Web 客户端（未来）
├─ tests/
│   ├─ BeamQualityAnalyzer.WpfClient.Tests/ # WPF 测试
│   └─ BeamQualityAnalyzer.ApiClient.Tests/ # API 客户端测试
├─ docs/                                    # 文档
├─ scripts/                                 # 打包脚本
└─ BeamQualityAnalyzer.Frontend.sln         # 解决方案文件
```

## 技术栈

- **.NET 9.0**: 运行时框架
- **WPF**: UI 框架
- **MVVM**: 架构模式
- **SignalR Client**: 实时通信客户端
- **CommunityToolkit.Mvvm**: MVVM 工具包
- **ScottPlot**: 2D 图表库（待添加）
- **HelixToolkit.Wpf**: 3D 可视化库（待添加）
- **Serilog**: 日志记录

## 快速开始

### 前置要求

- .NET 9.0 SDK
- Visual Studio 2022 或 VS Code
- 后端服务运行中

### 本地开发

1. 克隆仓库：
```bash
git clone https://github.com/yourorg/BeamQualityAnalyzer.Frontend.git
cd BeamQualityAnalyzer.Frontend
```

2. 还原依赖：
```bash
dotnet restore
```

3. 配置服务器地址（编辑 `src/BeamQualityAnalyzer.WpfClient/appsettings.json`）：
```json
{
  "ServerUrl": "http://localhost:5000"
}
```

4. 运行客户端：
```bash
dotnet run --project src/BeamQualityAnalyzer.WpfClient
```

## 配置说明

### appsettings.json

```json
{
  "ServerUrl": "http://localhost:5000",
  "AutoReconnect": true,
  "ReconnectInterval": 5000,
  "ConnectionTimeout": 30000
}
```

- `ServerUrl`: 后端服务器地址
- `AutoReconnect`: 是否自动重连
- `ReconnectInterval`: 重连间隔（毫秒）
- `ConnectionTimeout`: 连接超时（毫秒）

## 功能特性

### 已实现
- 项目结构搭建
- 基础配置

### 待实现
- 深色工业风格主题
- 实时数据可视化（2D/3D）
- SignalR 实时通信
- 数据采集控制
- 算法计算结果显示
- 截图和报告生成
- 数据库查询

## 开发指南

### MVVM 架构

```
View (XAML)
  ↓ DataBinding
ViewModel (ObservableObject)
  ↓ Commands/Properties
Model (Data/Services)
```

### 添加新视图

1. 在 `Views/` 目录创建 XAML 文件
2. 在 `ViewModels/` 目录创建对应的 ViewModel
3. 在 ViewModel 中实现 `ObservableObject`
4. 使用 `RelayCommand` 实现命令

## 测试

运行所有测试：
```bash
dotnet test
```

## 打包发布

```bash
dotnet publish src/BeamQualityAnalyzer.WpfClient -c Release -o publish/
```

## 日志

日志文件位置：`logs/wpf-client-{Date}.log`

## 许可证

MIT License

## 联系方式

- 项目主页: https://github.com/yourorg/BeamQualityAnalyzer.Frontend
- 问题反馈: https://github.com/yourorg/BeamQualityAnalyzer.Frontend/issues
