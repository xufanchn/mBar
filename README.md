# <img src="./resources/screenshots/logo.png"  width="28" style="vertical-align: middle; margin-top: -4px;" /> mBar

> **m**odern · **m**ulti · **m**onitor — on the bar
> 现代 · 多元 · 监控

基于 [LiteMonitor](https://github.com/Diorser/LiteMonitor) 的独立分支，一款轻量、可定制的 Windows 桌面硬件监控软件 — 实时监测 CPU、GPU、内存、磁盘、网络等系统性能。

> 本仓库已脱离上游独立开发，不再同步上游更新。

## 🖥️ 监控功能

| 分类 | 监控指标 |
|------|-----------|
| 💻 **处理器（CPU）**  | 实时监测 CPU 使用率、温度、频率、功耗、风扇、水冷等数据。 |
| 🎮 **显卡（GPU）**  | 展示 GPU 使用率、核心温度、显存、频率、功耗 风扇，兼容 NVIDIA / AMD / Intel 显卡。 |
| 💾 **主机（HOST）** | 显示系统内存占用、FPS刷新率、磁盘温度、主板温度、机箱风扇等。 |
| 🔋 **电池（Battery）** | 监测电池状态（充电状态、电量、功耗、电流、电压等）。 |
| 📀 **磁盘（Disk）**   | 监控磁盘读取与写入速度（KB/s、MB/s）。支持自动/手动选择磁盘。 |
| 🌐 **网络（Network）** | 实时显示上传与下载速度（KB/s、MB/s）。支持自动/手动选择网卡。 |
| 📈 **流量统计（Traffic）** | 统计每日上传与下载流量。 |
| 🔌 **插件系统（Plugin）** | 支持监控天气、股票、加密货币、代理延迟、汇率等，支持自定义插件。 |
| 🔧 **硬件传感器（Sensors）** | 硬件详情面板可以查看和监控所有系统硬件传感器数据。 |

## 产品功能

| 功能 | 说明 |
|---|---|
| 🎨 自定义主题 | 通过 JSON 定义颜色、字体、间距、圆角等，10+ 内置主题。 |
| 🟥🟨🟩 三色报警 | 监控项根据阈值自动切换进度条/数值颜色。 |
| 🌍 多语言界面 | 内置中/英/日/韩/法/德/西/俄多语言。 |
| 📊 监控项管理 | 按需显示或隐藏 CPU、GPU、VRAM、内存、磁盘、网络等模块。 |
| 🧮 横屏模式 + 任务栏 | 横条布局，任务栏嵌入显示，组间分隔符，SVG 图标支持。 |
| 📏 面板宽度调整 | 即时调整面板宽度，布局自动重排。 |
| 🔠 UI 缩放 | 自适应 DPI + 用户自定义缩放。 |
| 🎞️ 动画平滑 | 数值更新支持平滑动画，可调节速度。 |
| 🪟 窗口与界面 | 圆角显示、透明度调节、阴影、高质量字体渲染。 |
| 🧭 靠边自动隐藏 | 靠屏幕边缘自动收起，靠近边缘自动弹出。 |
| 👆 鼠标穿透模式 | 启用后，窗口不拦截鼠标事件。 |
| 🔄 更新检测 | 启动时静默检查新版本。 |
| 🚀 开机自启 | 通过计划任务方式实现。 |
| 📂 配置文件 | 所有设置实时写入 `settings.json`，支持迁移与备份。 |

## 📦 编译

### 环境要求
- Windows 10 / 11
- .NET 8 SDK
- Visual Studio 2022 或 Rider

```bash
git clone git@github.com:xufanchn/mBar.git
cd mBar
dotnet build -c Release
```

输出：`bin/Release/net8.0-windows/LiteMonitor.exe`

## 🎨 主题

主题文件位于 `resources/themes/`。支持自定义布局、颜色、字体。

## 📄 开源协议

基于上游 [LiteMonitor](https://github.com/Diorser/LiteMonitor)（MIT License），本分支同样以 **MIT License** 开源。
