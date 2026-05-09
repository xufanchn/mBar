# Taskbar Display Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor taskbar from fixed 2-row columns to variable 0-4 row columns with group separators, SVG/PNG/ICO icon prefixes, and adaptive text/icon scaling.

**Architecture:** Column class gains `GroupKey` + `List<MetricItem> Slots` replacing fixed Top/Bottom. `UIController.BuildColumnsCore` groups items by `MonitorItemConfig.TaskbarColumnGroup`. `HorizontalLayout.Build` computes per-slot bounds. `TaskbarRenderer` draws separators between different-group columns, renders icons via new `IconResolver` (SkiaSharp SVG → cached Bitmap), and adapts font/icon size to row count.

**Tech Stack:** .NET 8 WinForms, SkiaSharp 2.88.x, Svg.Skia 2.x, GDI+

---

### Task 1: Add NuGet dependencies

**Files:**
- Modify: `LiteMonitor.csproj`

- [ ] **Step 1: Add SkiaSharp and Svg.Skia packages**

```bash
cd /home/xf/code/fork/mBar
dotnet add package SkiaSharp --version 2.88.9
dotnet add package Svg.Skia --version 2.0.0
```

- [ ] **Step 2: Verify build still works**

```bash
dotnet build -c Release --no-restore 2>&1 | tail -5
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add LiteMonitor.csproj
git commit -m "build: add SkiaSharp and Svg.Skia dependencies for SVG icon rendering"
```

---

### Task 2: Settings data model — MonitorItemConfig and Settings new fields

**Files:**
- Modify: `src/Core/Settings.cs`

- [ ] **Step 1: Add new properties to MonitorItemConfig**

After line 289 (`public int TaskbarSortIndex { get; set; } = 0;`), insert:

```csharp
        // ★★★ [新增] 任务栏列分组键 — 同值归入同一列 ★★★
        public string TaskbarColumnGroup { get; set; } = "";
        // ★★★ [新增] 图标资源 Key — 空 = 不显示图标 ★★★
        public string IconKey { get; set; } = "";
```

- [ ] **Step 2: Add ShowSeparators to Settings class**

After line 91 (`public bool TaskbarSingleLine { get; set; } = false;`), insert:

```csharp
        public bool ShowSeparators { get; set; } = true;  // 组间分隔符开关
```

- [ ] **Step 3: Build verify**

```bash
dotnet build -c Release --no-restore 2>&1 | tail -5
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add src/Core/Settings.cs
git commit -m "feat(任务栏): add TaskbarColumnGroup, IconKey to MonitorItemConfig and ShowSeparators to Settings"
```

---

### Task 3: Theme color config — add separator and iconPrimary

**Files:**
- Modify: `src/Core/ThemeManager.cs`
- Modify: `resources/themes/*.json` (all 10 theme files)

- [ ] **Step 1: Add new color fields to ColorConfig**

In `ColorConfig` class (around line 131, after `GroupBackground`), insert:

```csharp
        public string Separator { get; set; } = "#40FFFFFF";
        public string IconPrimary { get; set; } = "#CCCCCC";
```

- [ ] **Step 2: Add default color accessors to Theme**

Find where `Theme` class exposes parsed colors (look for `public Color GroupBackground` pattern), add:

```csharp
        public Color SeparatorColor { get; internal set; }
        public Color IconPrimaryColor { get; internal set; }
```

- [ ] **Step 3: Add parsing in Theme load method**

Find where other colors are parsed via `ParseColor` (grep for `GroupBackground` in ThemeManager.cs), add after that block:

```csharp
            t.SeparatorColor = ParseColor(c.Separator);
            t.IconPrimaryColor = ParseColor(c.IconPrimary);
```

- [ ] **Step 4: Add `layout.separatorHeightRatio` to LayoutConfig**

In `LayoutConfig` class, after `GroupTitleOffset`, add:

```csharp
        public double SeparatorHeightRatio { get; set; } = 0.6;
```

- [ ] **Step 5: Don't scale separatorHeightRatio in LayoutConfig.Scale()**

(SeparatorHeightRatio is a ratio 0-1, not a pixel value — skip it in the Scale method.)

- [ ] **Step 6: Update all 10 theme JSON files**

For each `resources/themes/*.json`, add to the `color` block:

```json
    "separator": "#40FFFFFF",
    "iconPrimary": "#CCCCCC"
```

And add to the `layout` block:

```json
    "separatorHeightRatio": 0.6
```

- [ ] **Step 7: Build verify**

```bash
dotnet build -c Release --no-restore 2>&1 | tail -5
```

Expected: Build succeeded.

- [ ] **Step 8: Commit**

```bash
git add src/Core/ThemeManager.cs resources/themes/
git commit -m "feat(主题): add Separator and IconPrimary colors to theme config"
```

---

### Task 4: Update default items with new fields

**Files:**
- Modify: `src/Core/SettingsHelper.cs`

- [ ] **Step 1: Add new fields in InitDefaultItems**

Open `src/Core/SettingsHelper.cs`, find `InitDefaultItems()`. For each item where `VisibleInTaskbar = true` is set, add the corresponding `TaskbarColumnGroup` and `IconKey`.

The exact group mapping:

| Key | TaskbarColumnGroup | IconKey | VisibleInTaskbar |
|-----|-------------------|---------|-----------------|
| NET.Up | `"network"` | `"network_up"` | true |
| NET.Down | `"network"` | `"network_down"` | true |
| CPU.Load | `"cpu"` | `"cpu"` | true |
| CPU.Temp | `"cpu"` | `"cpu_temp"` | true |
| GPU.Load | `"gpu"` | `"gpu"` | true |
| GPU.Temp | `"gpu"` | `"gpu_temp"` | true |
| MEM.Load | `"host"` | `"memory"` | true |
| BAT.Percent | `"battery"` | `"battery"` | true |
| DASH.Time | — | — | **false** (hide) |
| DASH.Date | — | — | **false** (hide) |

Implementation pattern for each item — add these lines right after the existing `item.TaskbarSortIndex = ...` or `item.VisibleInTaskbar = true` line:

```csharp
item.TaskbarColumnGroup = "cpu";
item.IconKey = "cpu";
```

For items hidden from taskbar, change `VisibleInTaskbar` from `true` to `false`. No need to set Group/Icon for hidden items.

- [ ] **Step 2: Build verify**

```bash
dotnet build -c Release --no-restore 2>&1 | tail -5
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/Core/SettingsHelper.cs
git commit -m "feat(配置): initialize TaskbarColumnGroup and IconKey in default items"
```

---

### Task 5: Rewrite Column class

**Files:**
- Modify: `src/UI/HorizontalLayout.cs` (Column class at bottom of file)

- [ ] **Step 1: Replace Column class**

Replace the existing `Column` class (lines 330-341) with:

```csharp
    public class Column
    {
        public string GroupKey = "";
        public List<MetricItem> Slots = new();       // 0-4 items
        public int ColumnWidth;
        public Rectangle Bounds = Rectangle.Empty;
        public Rectangle[] SlotBounds = Array.Empty<Rectangle>();
        public bool HasSeparatorBefore = false;       // set by layout builder
    }
```

- [ ] **Step 2: Add SlotCount helper property**

```csharp
        public int SlotCount => Slots?.Count ?? 0;
```

- [ ] **Step 3: Add convenience indexer for accessing slots**

```csharp
        public MetricItem? this[int index] => (Slots != null && index >= 0 && index < Slots.Count) ? Slots[index] : null;
```

- [ ] **Step 4: Build verify**

```bash
dotnet build -c Release --no-restore 2>&1 | tail -20
```

Expected: Build errors in UIController, TaskbarForm, HorizontalLayout, TaskbarRenderer — all referencing old Top/Bottom/BoundsTop/BoundsBottom. This is expected; we'll fix them in subsequent tasks.

- [ ] **Step 5: Commit**

```bash
git add src/UI/HorizontalLayout.cs
git commit -m "refactor(任务栏): rewrite Column class with Slots list replacing Top/Bottom"
```

---

### Task 6: Rewrite UIController.BuildColumnsCore with group-based logic

**Files:**
- Modify: `src/UI/UIController.cs`

- [ ] **Step 1: Replace BuildColumnsCore method**

Replace the existing `BuildColumnsCore` method (lines 347-395) with:

```csharp
        private List<Column> BuildColumnsCore(bool forTaskbar)
        {
            var items = _cfg.MonitorItems
                .Where(x => forTaskbar ? x.VisibleInTaskbar : x.VisibleInPanel)
                .ToList();

            if (!forTaskbar)
            {
                // Panel mode: keep existing behavior (pairing by SortIndex groups)
                items = items
                    .GroupBy(x => x.UIGroup)
                    .OrderBy(g => g.Min(item => item.SortIndex))
                    .SelectMany(g => g.OrderBy(item => item.SortIndex))
                    .ToList();

                bool singleLine = _cfg.HorizontalMode && _cfg.HorizontalSingleLine;
                int step = singleLine ? 1 : 2;
                var cols = new List<Column>();

                for (int i = 0; i < items.Count; i += step)
                {
                    var col = new Column { GroupKey = "" };
                    col.Slots.Add(CreateMetric(items[i]));
                    if (!singleLine && i + 1 < items.Count)
                        col.Slots.Add(CreateMetric(items[i + 1]));
                    cols.Add(col);
                }
                return cols;
            }

            // Taskbar mode: group by TaskbarColumnGroup
            var groups = items
                .Where(x => x.VisibleInTaskbar)
                .GroupBy(x => string.IsNullOrEmpty(x.TaskbarColumnGroup) ? x.Key : x.TaskbarColumnGroup)
                .OrderBy(g => g.Min(item => item.TaskbarSortIndex))
                .ToList();

            var taskbarCols = new List<Column>();
            foreach (var g in groups)
            {
                var col = new Column { GroupKey = g.Key };
                var sorted = g.OrderBy(item => item.TaskbarSortIndex).Take(4).ToList();
                foreach (var cfg in sorted)
                    col.Slots.Add(CreateMetric(cfg));
                taskbarCols.Add(col);
            }

            return taskbarCols;
        }
```

- [ ] **Step 2: Update UpdateCol to work with Slots**

Find `UpdateCol` method and update to iterate Slots instead of Top/Bottom:

```csharp
        private void UpdateCol(Column col)
        {
            foreach (var item in col.Slots)
            {
                if (item == null) continue;
                if (item.Key.StartsWith("DASH."))
                {
                    string dashKey = item.Key.Substring(5);
                    item.TextValue = InfoService.Instance.GetValue(dashKey);
                    item.Value = null;
                }
                else
                {
                    InitMetricValue(item);
                }
            }
        }
```

- [ ] **Step 3: Also update the horizontal columns loop**

Find the existing `foreach (var col in _hxColsHorizontal) UpdateCol(col);` loop (around line 231) and update to also cover _hxColsTaskbar the same way.

- [ ] **Step 4: Build verify**

```bash
dotnet build -c Release --no-restore 2>&1 | tail -20
```

Expected: Remaining build errors in HorizontalLayout.cs, TaskbarRenderer.cs, TaskbarForm.cs.

- [ ] **Step 5: Commit**

```bash
git add src/UI/UIController.cs
git commit -m "refactor(任务栏): rewrite BuildColumnsCore with TaskbarColumnGroup grouping"
```

---

### Task 7: Rewrite HorizontalLayout.Build for variable slots and separators

**Files:**
- Modify: `src/UI/HorizontalLayout.cs`

- [ ] **Step 1: Rewrite Build method**

Replace the existing `Build` method (lines 53-155) with:

```csharp
        public int Build(List<Column> cols, int taskbarHeight = 32)
        {
            if (cols == null || cols.Count == 0) return 0;

            var s = _settings.GetStyle();
            int pad = _padding;
            int padV = (_mode == LayoutMode.Taskbar) ? 0 : _padding / 2;
            bool singleLine = (_mode == LayoutMode.Taskbar && _settings.TaskbarSingleLine) ||
                              (_mode == LayoutMode.Horizontal && _settings.HorizontalSingleLine);

            if (_mode == LayoutMode.Taskbar)
                _rowH = singleLine ? taskbarHeight : taskbarHeight / 2;

            float dpi = _dpiScale;
            int gapBase = (_mode == LayoutMode.Taskbar) ? s.Gap : _settings.HorizontalItemSpacing;
            int gap = (int)Math.Round(gapBase * dpi);

            // Determine which columns get a separator before them
            string prevGroup = "";
            foreach (var col in cols)
            {
                if (!string.IsNullOrEmpty(prevGroup) && col.GroupKey != prevGroup)
                    col.HasSeparatorBefore = true;
                prevGroup = col.GroupKey;
            }

            using (var g = Graphics.FromHwnd(IntPtr.Zero))
            {
                foreach (var col in cols)
                {
                    int maxSlotWidth = 0;
                    foreach (var item in col.Slots)
                    {
                        int w = MeasureMetricItem(g, item, s);
                        if (w > maxSlotWidth) maxSlotWidth = w;
                    }
                    col.ColumnWidth = maxSlotWidth;
                }
            }

            int totalWidth = pad * 2;
            foreach (var col in cols)
            {
                totalWidth += col.ColumnWidth;
                if (col.HasSeparatorBefore)
                    totalWidth += (int)Math.Round(1 * dpi) + gap; // separator width + gap
            }
            if (cols.Count > 1)
            {
                int normalGaps = cols.Count - 1 - cols.Count(c => c.HasSeparatorBefore);
                totalWidth += normalGaps * gap;
            }
            PanelWidth = totalWidth;

            // Compute per-slot bounds
            int x = pad;
            foreach (var col in cols)
            {
                if (col.HasSeparatorBefore)
                    x += (int)Math.Round(1 * dpi) + gap;

                int colHeight = taskbarHeight;
                col.Bounds = new Rectangle(x, padV, col.ColumnWidth, colHeight);

                int n = col.Slots.Count;
                if (n == 0)
                {
                    col.SlotBounds = Array.Empty<Rectangle>();
                }
                else
                {
                    int slotH = colHeight / n;
                    col.SlotBounds = new Rectangle[n];
                    for (int i = 0; i < n; i++)
                    {
                        int y = padV + i * slotH + s.VOff;
                        col.SlotBounds[i] = new Rectangle(x, y, col.ColumnWidth, slotH - s.VOff);
                    }
                }

                x += col.ColumnWidth + gap;
            }

            return padV * 2 + taskbarHeight;
        }
```

- [ ] **Step 2: Update GetLayoutSignature to use Slots**

Replace `GetLayoutSignature` (lines 303-327):

```csharp
        public string GetLayoutSignature(List<Column> cols)
        {
            if (cols == null || cols.Count == 0) return "";
            unchecked
            {
                int hash = 17;
                foreach (var col in cols)
                {
                    hash = hash * 31 + col.GroupKey.GetHashCode();
                    foreach (var item in col.Slots)
                    {
                        if (item == null) continue;
                        string text = item.TextValue ?? item.GetFormattedText(true);
                        hash = hash * 31 + text.Length;
                        foreach (char c in text) if (!char.IsDigit(c)) hash = (hash << 5) - hash + c;
                    }
                }
                return hash.ToString();
            }
        }
```

- [ ] **Step 3: Build verify**

```bash
dotnet build -c Release --no-restore 2>&1 | tail -20
```

Expected: Remaining errors in TaskbarRenderer.cs, TaskbarForm.cs.

- [ ] **Step 4: Commit**

```bash
git add src/UI/HorizontalLayout.cs
git commit -m "refactor(任务栏): rewrite HorizontalLayout.Build for variable slots and separator spacing"
```

---

### Task 8: Create IconResolver

**Files:**
- Create: `src/UI/IconResolver.cs`

- [ ] **Step 1: Create IconResolver.cs**

```csharp
using SkiaSharp;
using Svg.Skia;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;

namespace LiteMonitor
{
    public static class IconResolver
    {
        private static readonly string _iconDir = Path.Combine(AppContext.BaseDirectory, "resources", "icons");
        private static readonly ConcurrentDictionary<(string, int, int), Bitmap?> _cache = new();

        public static Bitmap? Load(string key, int targetSize, Color themeColor)
        {
            if (string.IsNullOrEmpty(key)) return null;
            int colorArgb = themeColor.ToArgb();
            var cacheKey = (key, targetSize, colorArgb);
            if (_cache.TryGetValue(cacheKey, out var cached) && cached != null)
                return cached;

            Bitmap? result = TryLoadSvg(key, targetSize, themeColor)
                ?? TryLoadIco(key, targetSize)
                ?? TryLoadPng(key, targetSize);

            _cache[cacheKey] = result;
            return result;
        }

        private static Bitmap? TryLoadSvg(string key, int size, Color color)
        {
            string path = Path.Combine(_iconDir, key + ".svg");
            if (!File.Exists(path)) return null;

            try
            {
                var svg = new SKSvg();
                svg.Load(path);
                if (svg.Model == null) return null;

                var skColor = new SKColor((uint)color.ToArgb() & 0x00FFFFFF | 0xFF000000);
                float scale = size / Math.Max(svg.Model.CullRect.Width, svg.Model.CullRect.Height);

                var info = new SKImageInfo(size, size);
                using var surface = SKSurface.Create(info);
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);
                canvas.Scale(scale);
                canvas.Translate(-svg.Model.CullRect.Left, -svg.Model.CullRect.Top);

                using var paint = new SKPaint
                {
                    ColorFilter = SKColorFilter.CreateBlendMode(skColor, SKBlendMode.SrcIn)
                };
                canvas.DrawPicture(svg.Model, paint);

                using var skImage = surface.Snapshot();
                using var data = skImage.Encode(SKEncodedImageFormat.Png, 100);
                using var ms = new MemoryStream(data.ToArray());
                return new Bitmap(ms);
            }
            catch
            {
                return null;
            }
        }

        private static Bitmap? TryLoadIco(string key, int size)
        {
            string path = Path.Combine(_iconDir, key + ".ico");
            if (!File.Exists(path)) return null;

            try
            {
                using var icon = new Icon(path, size, size);
                return icon.ToBitmap();
            }
            catch
            {
                return null;
            }
        }

        private static Bitmap? TryLoadPng(string key, int size)
        {
            string path = Path.Combine(_iconDir, key + ".png");
            if (!File.Exists(path)) return null;

            try
            {
                using var img = Image.FromFile(path);
                return new Bitmap(img, size, size);
            }
            catch
            {
                return null;
            }
        }

        public static void ClearCache()
        {
            foreach (var kv in _cache)
                kv.Value?.Dispose();
            _cache.Clear();
        }
    }
}
```

- [ ] **Step 2: Build verify**

```bash
dotnet build -c Release --no-restore 2>&1 | tail -10
```

Expected: Build succeeded (this file is new, no breaking changes).

- [ ] **Step 3: Commit**

```bash
git add src/UI/IconResolver.cs
git commit -m "feat(图标): add IconResolver with SVG/ICO/PNG loading and caching"
```

---

### Task 9: Rewrite TaskbarRenderer for multi-row, separators, and icons

**Files:**
- Modify: `src/UI/TaskbarRenderer.cs`

- [ ] **Step 1: Rewrite Render method**

Replace the existing `Render` method and related private helpers:

```csharp
        public static void Render(Graphics g, List<Column> cols, bool light, int taskbarHeight, Color separatorColor, Color iconColor)
        {
            if (_cachedFont == null)
                ReloadStyle(Settings.Load());

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using var sepPen = new Pen(separatorColor, 1f * GetDpiScale(g));

            for (int i = 0; i < cols.Count; i++)
            {
                var col = cols[i];

                // Draw separator
                if (col.HasSeparatorBefore)
                {
                    float dpi = GetDpiScale(g);
                    int sepH = (int)(taskbarHeight * 0.6);
                    int sepY = (taskbarHeight - sepH) / 2;
                    int sepX = col.Bounds.Left - (int)Math.Round(2 * dpi);
                    g.DrawLine(sepPen, sepX, sepY, sepX, sepY + sepH);
                }

                // Draw each slot
                int n = col.Slots.Count;
                for (int s = 0; s < n; s++)
                {
                    var item = col.Slots[s];
                    if (item == null) continue;
                    var rc = col.SlotBounds[s];

                    DrawSlot(g, item, rc, light, iconColor, n);
                }
            }
        }

        private static float GetDpiScale(Graphics g) => g.DpiX / 96f;

        private static void DrawSlot(Graphics g, MetricItem item, Rectangle rc, bool light, Color iconColor, int slotCount)
        {
            Font font = GetScaledFont(slotCount);
            float fontSizeCoef = GetFontSizeCoef(slotCount);
            int iconSize = GetIconSize(rc.Height, slotCount);

            int x = rc.Left;

            // Draw icon
            if (!string.IsNullOrEmpty(item.BoundConfig?.IconKey))
            {
                var icon = IconResolver.Load(item.BoundConfig.IconKey, iconSize, iconColor);
                if (icon != null)
                {
                    int iconY = rc.Y + (rc.Height - iconSize) / 2;
                    g.DrawImage(icon, x, iconY, iconSize, iconSize);
                    x += iconSize + 3; // gap after icon
                }
            }

            var remainingRect = new Rectangle(x, rc.Y, rc.Right - x, rc.Height);

            // Label or full-text rendering
            string label = item.ShortLabel;
            bool hideLabel = string.IsNullOrEmpty(label) || label == " ";
            if (!hideLabel && string.IsNullOrEmpty(label)) label = item.Label;
            if (!hideLabel && string.IsNullOrEmpty(label)) label = item.Key;

            string value = item.GetFormattedText(true);

            Color labelColor = light ? LABEL_LIGHT : LABEL_DARK;
            Color valueColor = GetStateColor(item.CachedColorState, light);

            if (_useCustom)
            {
                labelColor = _cLabel;
                valueColor = GetCustomStateColor(item.CachedColorState);
            }

            if (hideLabel)
            {
                TextRenderer.DrawText(g, value, font, remainingRect, valueColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
            else
            {
                TextRenderer.DrawText(g, label, font, remainingRect, labelColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, value, font, remainingRect, valueColor,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }

        private static Font GetScaledFont(int slotCount)
        {
            // Returns appropriately scaled font based on slot count
            // Re-uses _cachedFont but with size adjusted
            float coef = slotCount switch
            {
                1 => 1.4f,
                2 => 1.0f,
                3 => 0.8f,
                _ => 0.65f
            };
            if (Math.Abs(coef - 1.0f) < 0.01f) return _cachedFont!;
            // Create and cache scaled fonts
            return GetOrCreateScaledFont(coef);
        }

        private static float GetFontSizeCoef(int slotCount) => slotCount switch
        {
            1 => 1.4f, 2 => 1.0f, 3 => 0.8f, _ => 0.65f
        };

        private static int GetIconSize(int rowHeight, int slotCount)
        {
            float ratio = slotCount switch
            {
                1 => 0.65f, 2 => 0.55f, 3 => 0.45f, _ => 0.38f
            };
            return Math.Max(8, (int)(rowHeight * ratio));
        }

        // Cache for scaled fonts
        private static readonly Dictionary<float, Font> _scaledFontCache = new();
        private static Font GetOrCreateScaledFont(float coef)
        {
            if (!_scaledFontCache.TryGetValue(coef, out var font) || font == null)
            {
                float newSize = _cachedFont!.Size * coef;
                font = new Font(_cachedFont.FontFamily, newSize, _cachedFont.Style);
                _scaledFontCache[coef] = font;
            }
            return font;
        }
```

- [ ] **Step 2: Update ReloadStyle to also clear scaled font cache**

Add to `ReloadStyle`:

```csharp
            foreach (var f in _scaledFontCache.Values) { try { f.Dispose(); } catch { } }
            _scaledFontCache.Clear();
```

- [ ] **Step 3: Build verify**

```bash
dotnet build -c Release --no-restore 2>&1 | tail -20
```

Expected: Errors only in TaskbarForm.cs (caller of Render).

- [ ] **Step 4: Commit**

```bash
git add src/UI/TaskbarRenderer.cs
git commit -m "refactor(任务栏): rewrite TaskbarRenderer for multi-row, separators, and icons"
```

---

### Task 10: Adapt TaskbarForm to new Column and Renderer signatures

**Files:**
- Modify: `src/UI/TaskbarForm.cs`

- [ ] **Step 1: Update Tick method — replace old BoundsTop/BoundsBottom logic**

In the `Tick` method, remove the old `isTaskbarSingle` logic and replace with simplified Slots-based logic. The column Bounds are now fully computed by `HorizontalLayout.Build`.

Replace the block from line 198 to 223:

```csharp
            if (_bizHelper.IsVertical())
            {
                _bizHelper.BuildVerticalLayout(_cols);
                _lastLayoutSignature = "vertical";
            }
            else
            {
                bool isUninitialized = (_cols.Count > 0 && _cols[0].Bounds.IsEmpty);
                string currentSig = _layout.GetLayoutSignature(_cols) + "_" + _bizHelper.Height;
                bool isContentChanged = (currentSig != _lastLayoutSignature);

                if (isUninitialized || isContentChanged)
                {
                    _layout.Build(_cols, _bizHelper.Height);
                    Width = _layout.PanelWidth;
                    Height = _bizHelper.Height;
                    _lastLayoutSignature = currentSig;
                }
            }
```

- [ ] **Step 2: Update OnPaint to pass new args to Render**

Replace the `TaskbarRenderer.Render(g, _cols, _bizHelper.LastIsLightTheme);` call with:

```csharp
            var theme = ThemeManager.Current;
            Color sepColor = _cfg.ShowSeparators ? theme.SeparatorColor : Color.Transparent;
            Color iconColor = theme.IconPrimaryColor;
            TaskbarRenderer.Render(g, _cols, _bizHelper.LastIsLightTheme, _bizHelper.Height, sepColor, iconColor);
```

- [ ] **Step 3: Build verify**

```bash
dotnet build -c Release --no-restore 2>&1 | tail -10
```

Expected: Build succeeded with zero errors.

- [ ] **Step 4: Commit**

```bash
git add src/UI/TaskbarForm.cs
git commit -m "refactor(任务栏): adapt TaskbarForm to new Column and Renderer signatures"
```

---

### Task 11: Update HorizontalRenderer for new Column Slots

**Files:**
- Modify: `src/UI/HorizontalRenderer.cs`

- [ ] **Step 1: Rewrite DrawColumn to use Slots**

Replace the existing `DrawColumn` method (lines 26-44):

```csharp
        private static void DrawColumn(Graphics g, Column col, Theme t)
        {
            if (col.Bounds == Rectangle.Empty) return;

            for (int i = 0; i < col.Slots.Count; i++)
            {
                var item = col.Slots[i];
                if (item == null) continue;
                var rc = i < col.SlotBounds.Length ? col.SlotBounds[i] : Rectangle.Empty;
                if (rc != Rectangle.Empty)
                    DrawItem(g, item, rc, t);
            }
        }
```

- [ ] **Step 2: Build verify**

```bash
dotnet build -c Release --no-restore 2>&1 | tail -10
```

Expected: Remaining errors only in TaskbarBizHelper.cs.

- [ ] **Step 3: Commit**

```bash
git add src/UI/HorizontalRenderer.cs
git commit -m "refactor(横屏): update HorizontalRenderer to use Column.Slots"
```

---

### Task 12: Update TaskbarBizHelper.BuildVerticalLayout for new Column Slots

**Files:**
- Modify: `src/UI/Helpers/TaskbarBizHelper.cs`

- [ ] **Step 1: Rewrite BuildVerticalLayout method**

Replace the existing `BuildVerticalLayout` (lines 203-238):

```csharp
        public void BuildVerticalLayout(List<Column> cols)
        {
            var s = _cfg.GetStyle();
            int w = _taskbarRect.Width;
            if (w < 20) w = 60;

            int margin = Math.Max(0, s.Inner / 2);
            int contentWidth = w - (margin * 2);

            int y = 0;
            foreach (var col in cols)
            {
                int n = col.Slots.Count;
                if (n == 0)
                {
                    col.Bounds = Rectangle.Empty;
                    col.SlotBounds = Array.Empty<Rectangle>();
                    continue;
                }

                int itemHeight = (int)(s.Size * 1.5f + 6);
                if (itemHeight < 20) itemHeight = 20;

                col.SlotBounds = new Rectangle[n];
                int colHeight = n * itemHeight;
                col.Bounds = new Rectangle(margin, y, contentWidth, colHeight);

                for (int i = 0; i < n; i++)
                {
                    col.SlotBounds[i] = new Rectangle(margin, y, contentWidth, itemHeight);
                    y += itemHeight;
                }
            }

            int totalHeight = y;
            _winHelper.SetPosition(_hTaskbar, _taskbarRect.Left, _taskbarRect.Top, w, totalHeight);
        }
```

- [ ] **Step 2: Build verify**

```bash
dotnet build -c Release --no-restore 2>&1 | tail -10
```

Expected: Build succeeded. 0 Error(s).

- [ ] **Step 3: Commit**

```bash
git add src/UI/Helpers/TaskbarBizHelper.cs
git commit -m "refactor(任务栏): update BuildVerticalLayout to use Column.Slots"
```

---

### Task 13: Final integration — build, run, smoke test

**Files:**
- No file changes — verification only

- [ ] **Step 1: Clean Release build**

```bash
dotnet build -c Release 2>&1
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 2: Prepare icon directory**

```bash
mkdir -p bin/Release/net8.0-windows/resources/icons/
# Optional: drop test SVG icons here
```

- [ ] **Step 3: Run and verify taskbar**

Run `bin/Release/net8.0-windows/LiteMonitor.exe`:

1. Taskbar shows columns grouped by `TaskbarColumnGroup`
2. NET.Up + NET.Down in one column, CPU.Load + CPU.Temp in another
3. Different groups separated by vertical separator lines
4. No crash when icon files are missing
5. Right-click context menu still works
6. Double-click toggles main form
7. Resize taskbar — layout recalculates correctly

- [ ] **Step 4: Verify single-row column display**

Set GPU to have only one visible item → renders with larger text.

- [ ] **Step 5: Commit any integration fixes**

```bash
git status
# If fixes needed: git add ... && git commit -m "fix(任务栏): integration fixes"
```
