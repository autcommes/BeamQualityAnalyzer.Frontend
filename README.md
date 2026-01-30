# 光束质量分析系统 - 前端客户端

## 概述

光束质量分析系统前端客户端，基于 WPF + .NET 8.0 + MVVM 架构构建，提供深色工业风格 UI 和实时数据可视化功能。

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

- **.NET 8.0**: 运行时框架
- **WPF**: UI 框架
- **MVVM**: 架构模式（CommunityToolkit.Mvvm）
- **SignalR Client**: 实时通信客户端
- **ScottPlot 5.0**: 2D 图表库
- **HelixToolkit.Wpf**: 3D 可视化库
- **Entity Framework Core**: 本地配置数据库
- **Serilog**: 日志记录

## 快速开始

### 前置要求

- .NET 8.0 SDK
- Visual Studio 2022 或 VS Code
- Windows 10/11 操作系统
- 后端服务运行中（默认 http://192.168.0.196:5000）

### 本地开发

1. 克隆仓库：
```bash
git clone https://github.com/autcommes/BeamQualityAnalyzer.Frontend.git
cd BeamQualityAnalyzer.Frontend
```

2. 还原依赖：
```bash
dotnet restore
```

3. 配置服务器地址（可选，默认为 http://192.168.0.196:5000）：
   - 启动客户端后，点击右上角设置按钮
   - 修改服务器 URL
   - 点击保存

4. 运行客户端：
```bash
dotnet run --project src/BeamQualityAnalyzer.WpfClient
```

## 配置说明

### 设置对话框

客户端启动后，点击右上角齿轮图标打开设置对话框，可配置：

- **服务器 URL**: 后端服务地址（默认 http://192.168.0.196:5000）
- **采集参数**:
  - 采集间隔（毫秒）
  - 采样点数
  - 噪声水平
- **分析参数**:
  - 拟合算法选择
  - 阈值设置
  - 计算精度

所有配置自动保存到本地 SQLite 数据库。

## 功能特性

### 已实现
- ✅ 深色工业风格主题（紫色调）
- ✅ SignalR 实时通信和自动重连
- ✅ 数据采集控制（开始/停止）
- ✅ 实时数据可视化
  - 2D 光斑图（ScottPlot）
  - 3D 能量分布图（HelixToolkit）
  - 实时曲线图表
- ✅ 分析参数配置
- ✅ 设置对话框（服务器地址、采集参数等）
- ✅ 状态栏实时状态显示
- ✅ 本地配置持久化（SQLite）
- ✅ 结构化日志记录

### 待实现
- ⏳ 自动测试功能
- ⏳ 历史数据查询
- ⏳ 报告生成和导出
- ⏳ 截图功能
- ⏳ 多语言支持

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

- 项目主页: https://github.com/autcommes/BeamQualityAnalyzer.Frontend
- 问题反馈: https://github.com/autcommes/BeamQualityAnalyzer.Frontend/issues
- 后端仓库: https://github.com/autcommes/BeamQualityAnalyzer.Backend
