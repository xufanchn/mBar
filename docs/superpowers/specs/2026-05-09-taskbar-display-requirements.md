# 任务栏显示重构 — 需求文档

> 日期: 2026-05-09 | 分支: feat-taskbar-display

## 来源

基于 [DESIGN.md](../../../DESIGN.md) 第 8-10 节，经用户对话确认细化。

## 功能需求

### FR1: 可变行列

当前 Column 固定 Top/Bottom 双行结构。改为可变 Slots 列表。

| 规则 | 说明 |
|------|------|
| 行数范围 | 0-4 行，超出部分忽略 |
| 列形成 | 相同 TaskbarColumnGroup 的 items 归入同一列 |
| 列内排序 | 按 TaskbarSortIndex 升序 |
| 列间排序 | 按每列最小 TaskbarSortIndex 升序 |
| 独立列 | 未设 Group 的 item 各自独立成列（1行） |
| 0 行占位 | 暂不覆盖，留待后续 |

### FR2: 组间分隔符

不同 Group 的列之间绘制竖线分隔符。

| 属性 | 值 |
|------|-----|
| 触发条件 | col[i].GroupKey != col[i-1].GroupKey |
| 宽度 | 1px × DPI scale |
| 高度 | 任务栏高度 × 60%，垂直居中 |
| 首尾 | 首列前、尾列后不画 |
| 开关 | Settings.ShowSeparators (bool, 默认 true) |

### FR3: 图标系统

每行可选显示图标前缀。格式支持 SVG（首选）、ICO、PNG。

| 规则 | 说明 |
|------|------|
| 配置 | MonitorItemConfig.IconKey，空 = 无图标 |
| 格式优先级 | SVG > ICO > PNG > 无 |
| SVG 渲染 | SkiaSharp + Svg.Skia，支持 fill 颜色替换 |
| 颜色 | 图标按主题 color.iconPrimary 着色，不跟随系统明暗 |
| 缓存 | 按 (key, size, color) 缓存 Bitmap |
| 自适应缩放 | 图标尺寸随列行数自动调整（见 FR4） |

### FR4: 自适应缩放

图标和字号根据列内行数自动调整，无需手动配置。

| 列行数 | 图标尺寸 | 字号系数 | 效果 |
|--------|---------|---------|------|
| 1 | rowHeight × 0.65 | ×1.4 | 大字突出 |
| 2 | rowHeight × 0.55 | ×1.0 | 标准 |
| 3 | rowHeight × 0.45 | ×0.8 | 紧凑 |
| 4 | rowHeight × 0.38 | ×0.65 | 迷你 |

### FR5: 颜色规则

| 元素 | 颜色来源 | 说明 |
|------|---------|------|
| 图标 | 主题 color.iconPrimary | 自定义色，不跟随系统 |
| 数值文字 | 系统任务栏文字色 | light: #141414, dark: #FFFFFF，主题可覆盖 |
| 分隔符 | 主题 color.separator | 深色默认 #40FFFFFF，浅色默认 #40000000 |

## 非功能需求

### NFR1: 性能

- 图标缓存命中后零渲染开销
- SVG 首次加载 < 1ms（目标尺寸 ≤ 16px）
- 任务栏 Tick 周期不变（当前 RefreshMs，最小 60ms）

### NFR2: 兼容性

- 现有功能不受影响：拖拽、右键菜单、自动隐藏、多显示器、Win10/Win11
- 横屏模式 `HorizontalRender` 不在此次改动范围

## 与 DESIGN.md 的关系

| DESIGN.md 章节 | 本次实现 |
|---------------|---------|
| §8 任务栏视觉分隔符 | 完整实现 |
| §9 响应式图标资源系统 | 实现（无 GIF、无多分辨率 PNG） |
| §10 自适应列缩放 | 实现（行数由 Slots 自动决定，非手动配置） |
| §11 双主题 | 不做（仅新增 separator/iconPrimary 颜色字段） |
| §12 主界面简洁模式 | 不做 |
| §13 配置字段汇总 | 部分采用（TaskbarColumnGroup、IconKey；不用 TaskbarRowCount） |
