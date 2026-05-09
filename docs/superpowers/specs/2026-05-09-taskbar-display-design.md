# 任务栏显示重构 — 设计文档

> 日期: 2026-05-09 | 分支: feat-taskbar-display

## 1. 概述

重构任务栏监控显示，支持可变行列、组间分隔符、SVG 图标系统。

### 范围

| 包含 | 不包含 |
|------|--------|
| Column 可变行数 (0-4) | 主界面简洁模式 (DESIGN.md §12) |
| 组间分隔符渲染 | 双主题系统 (DESIGN.md §11) |
| SVG/PNG/ICO 图标渲染 | Tab 标签页 (DESIGN.md §2-7) |
| 自适应字号/图标缩放 | 响应式布局 (DESIGN.md §5) |

## 2. 数据模型

### Column 类变更

```
// 旧
class Column {
    MetricItem? Top;
    MetricItem? Bottom;
    Rectangle Bounds, BoundsTop, BoundsBottom;
    int ColumnWidth;
}

// 新
class Column {
    string GroupKey;              // 列分组键，同组 items 归入同一列
    List<MetricItem> Slots;       // 可变行列表 (0-4 items)
    Rectangle Bounds;             // 整列 bounds
    Rectangle[] SlotBounds;       // 每行独立 bounds，长度 = Slots.Count
    int ColumnWidth;
    bool HasSeparatorBefore;      // 该列前是否画分隔符
}
```

GroupKey 为空时该 item 独立成列。

列内 Slots 数量上限 4，超出部分忽略。0 行占位列（用户需求的空白占位）通过显式配置空列实现，本文档暂不覆盖，留待后续。

### MonitorItemConfig 新增字段

| 字段 | 类型 | 默认 | 说明 |
|------|------|------|------|
| TaskbarColumnGroup | string | "" | 列分组键，同值归入同一列 |
| IconKey | string | "" | 图标资源 key，空 = 无图标 |

移除 DESIGN.md 中的 TaskbarRowCount — 行数由列内 Slots 数量自动决定。

## 3. 列构建逻辑

位置: `UIController.BuildColumnsCore(forTaskbar: true)`

```
1. 筛选 VisibleInTaskbar 的 items
2. 按 TaskbarColumnGroup 分组
3. 每组内按 TaskbarSortIndex 排序 → 得到 Slots 列表
4. 组间按每组最小 TaskbarSortIndex 排序 → 列顺序
5. 未设 Group 的 items 各自独立成列（单行）
```

## 4. 分隔符渲染

位置: `TaskbarRenderer.Render()`

| 属性 | 值 |
|------|-----|
| 触发条件 | col[i].GroupKey != col[i-1].GroupKey |
| 宽度 | 1px × DPI scale |
| 高度 | taskbarHeight × 0.6 |
| 垂直位置 | 列间距正中间，垂直居中 |
| 颜色 | 主题 color.separator |
| 首尾 | 首列前、尾列后不画 |

深色默认: `#40FFFFFF`，浅色默认: `#40000000`

## 5. 图标系统

### 加载优先级

1. `.svg` → SkiaSharp + Svg.Skia 矢量渲染（首选）
2. `.ico` → 提取最匹配目标尺寸的内嵌尺寸
3. `.png` → GDI+ Image.FromFile + 缩放
4. 无文件 → 不显示图标

### 渲染架构

```
IconResolver.Load(key, targetSizePx, themeColor):
  cacheKey = (key, targetSize, themeColor)
  if cache hit → return cached Bitmap
  
  查找 resources/icons/{key}.svg → SvgDocument
    → 替换 var(--icon-color) 为 themeColor
    → 栅格化到 targetSize × targetSize → SKBitmap
    → 转 System.Drawing.Bitmap → 缓存 → 返回
  
  查找 resources/icons/{key}.ico → new Icon(file)
    → 选最接近 targetSize 的内嵌尺寸 → ToBitmap()
    → 缓存 → 返回
  
  查找 resources/icons/{key}.png → Image.FromFile → 缩放到 targetSize
    → 缓存 → 返回
  
  return null
```

### NuGet 依赖

- `SkiaSharp` (2.88.x) — 跨平台 2D 图形
- `Svg.Skia` (2.x) — SVG 解析 + SkiaSharp 渲染

### 自适应尺寸

| 列行数 | 图标尺寸 | 字号系数 |
|--------|---------|---------|
| 1 | rowHeight × 0.65 | ×1.4 |
| 2 | rowHeight × 0.55 | ×1.0 |
| 3 | rowHeight × 0.45 | ×0.8 |
| 4 | rowHeight × 0.38 | ×0.65 |

## 6. 渲染流程

### TaskbarRenderer.Render() 更新

```
Render(g, cols, light):
  for i = 0..cols.Count-1:
    col = cols[i]
    
    // 画分隔符
    if col.HasSeparatorBefore:
      sepX = col.Bounds.Left - gap/2
      sepH = (int)(taskbarHeight * 0.6)
      sepY = (taskbarHeight - sepH) / 2
      g.DrawLine(sepPen, sepX, sepY, sepX, sepY + sepH)
    
    // 画每行
    for slotIdx = 0..col.Slots.Count-1:
      item = col.Slots[slotIdx]
      rc = col.SlotBounds[slotIdx]
      
      // 画图标（如果有）
      if item.IconKey != "":
        icon = IconResolver.Load(item.IconKey, iconSize, themeColor)
        g.DrawImage(icon, iconX, rc.Y + (rc.H - iconSize)/2)
      
      // 画文字（颜色跟随系统，主题可覆盖）
      DrawSlotText(g, item, rc, light)
```

### 颜色规则

- 图标颜色: 主题 color.iconPrimary（自定义），不跟随系统明暗
- 数值颜色: 默认跟随系统任务栏文字色（light? LABEL_LIGHT : LABEL_DARK），主题可覆盖
- 分隔符颜色: 主题 color.separator

## 7. 涉及文件

| 文件 | 改动 |
|------|------|
| `src/UI/HorizontalLayout.cs` | Column 类重写，Build 支持 Slots + 分隔符间距 |
| `src/UI/TaskbarRenderer.cs` | 新增分隔符绘制、图标绘制、多行渲染 |
| `src/UI/UIController.cs` | BuildColumnsCore 改用 GroupKey 分组 |
| `src/UI/TaskbarForm.cs` | 适配新 Column 结构 |
| `src/Core/Settings.cs` | MonitorItemConfig 新增字段 |
| `src/Core/SettingsHelper.cs` | 默认配置更新 |
| `src/Core/ThemeManager.cs` | 新增 color.separator, color.iconPrimary 解析 |
| `resources/themes/*.json` | 新增 color.separator, color.iconPrimary |
| `src/UI/New/IconResolver.cs` | 新增: 图标加载/缓存/渲染 |
| `LiteMonitor.csproj` | 新增 SkiaSharp, Svg.Skia 依赖 |

## 8. 验证方式

项目无自动化测试，手动构建运行验证：

1. `dotnet build -c Release` 通过
2. 运行 exe，任务栏正常显示
3. 同一 Group 的 items 在同一列，不同 Group 之间有分隔符
4. 列行数自适应：1 行大字，2 行标准，3+ 紧凑
5. SVG 图标正确渲染，颜色跟随主题
6. PNG/ICO 图标正常 fallback
7. 拖拽/右键菜单/自动隐藏 不受影响
