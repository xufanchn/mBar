[中文文档](./README.md)

# <img src="./resources/screenshots/logo.png"  width="28" style="vertical-align: middle; margin-top: -4px;" /> mBar

An independent fork of [LiteMonitor](https://github.com/Diorser/LiteMonitor) — a lightweight, customizable Windows hardware monitor tracking CPU, GPU, memory, disk, and network in real time.

> This fork is now independently developed and no longer syncs with upstream.

## 🖥️ Monitoring Features

| Category | Metrics |
|-----------|----------|
| 💻 **CPU** | Usage, temperature, frequency, power, fan, pump |
| 🎮 **GPU** | Usage, temperature, VRAM, frequency, power, fan (NVIDIA / AMD / Intel) |
| 💾 **Host** | Memory usage, FPS, disk/motherboard temp, chassis fan |
| 🔋 **Battery** | Status, charge, power, current, voltage |
| 📀 **Disk** | Read/write speed (KB/s, MB/s) |
| 🌐 **Network** | Upload/download speed (KB/s, MB/s) |
| 📈 **Traffic** | Daily upload/download statistics |
| 🔌 **Plugins** | Weather, stocks, crypto, latency, exchange rates, custom plugins |
| 🔧 **Sensors** | Full hardware sensor tree view |

## Features

| Feature | Description |
|---|---|
| 🎨 Themes | JSON-defined colors, fonts, spacing, corner radius. 10+ built-in themes. |
| 🟥🟨🟩 Alerts | Three-color threshold-based alerts for values and bars. |
| 🌍 i18n | Chinese, English, Japanese, Korean, French, German, Spanish, Russian. |
| 🧮 Horizontal + Taskbar | Horizontal bar layout, taskbar embedding, group separators, SVG icons. |
| 🔠 DPI Scaling | Adaptive DPI + user-defined UI scale. |
| 🎞️ Smooth Animation | Exponential moving average with adjustable speed. |

## 📦 Build

### Requirements
- Windows 10 / 11
- .NET 8 SDK
- Visual Studio 2022 or Rider

```bash
git clone git@github.com:xufanchn/mBar.git
cd mBar
dotnet build -c Release
```

Output: `bin/Release/net8.0-windows/LiteMonitor.exe`

## 📄 License

Based on [LiteMonitor](https://github.com/Diorser/LiteMonitor) (MIT). This fork is also **MIT License**.
