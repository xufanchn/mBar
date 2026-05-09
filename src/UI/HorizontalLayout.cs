using LiteMonitor.src.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace LiteMonitor
{
    public enum LayoutMode
    {
        Horizontal,
        Taskbar
    }

    public class HorizontalLayout
    {
        private readonly Theme _t;
        private readonly LayoutMode _mode;
        private readonly Settings _settings;

        private readonly int _padding;
        private int _rowH;

        // DPI
        private readonly float _dpiScale;

        public int PanelWidth { get; private set; }

        public HorizontalLayout(Theme t, int initialWidth, LayoutMode mode, Settings? settings = null)
        {
            _t = t;
            _mode = mode;
            _settings = settings ?? Settings.Load();

            using (var g = Graphics.FromHwnd(IntPtr.Zero))
            {
                _dpiScale = g.DpiX / 96f;
            }

            _padding = t.Layout.Padding;

            if (mode == LayoutMode.Horizontal)
                _rowH = Math.Max(t.FontItem.Height, t.FontValue.Height);
            else
                _rowH = 0; // 任务栏模式稍后根据 taskbarHeight 决定

            PanelWidth = initialWidth;
        }

        /// <summary>
        /// Build：横屏/任务栏共用布局
        /// </summary>
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
                    totalWidth += (int)Math.Round(1 * dpi) + gap;
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

                int colHeight = (_mode == LayoutMode.Taskbar) ? taskbarHeight : (singleLine ? _rowH : _rowH * 2);
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

        private int MeasureMetricItem(Graphics g, MetricItem item, Settings.TBStyle s)
        {
            if (item == null) return 0;

            float dpi = _dpiScale;

            // [通用逻辑] 如果隐藏标签 (ShortLabel 为空 或 " ")，则只计算文本宽
            if (string.IsNullOrEmpty(item.ShortLabel) || item.ShortLabel == " ")
            {
                // 对于 Dashboard/IP 类，直接使用当前文本作为测量依据
                string valText = item.TextValue ?? item.GetFormattedText(true);
                if (string.IsNullOrEmpty(valText)) return 0;

                Font valFont;
                bool disposeFont = false;

                if (_mode == LayoutMode.Taskbar)
                {
                    valFont = new Font(s.Font, s.Size, s.Bold ? FontStyle.Bold : FontStyle.Regular);
                    disposeFont = true;
                }
                else
                {
                    valFont = _t.FontItem;
                }

                try
                {
                    int w = TextRenderer.MeasureText(g, valText, valFont,
                        new Size(int.MaxValue, int.MaxValue),
                        TextFormatFlags.NoPadding).Width;
                    
                    // 纯文本项建议稍微加一点点左右 padding，防止紧贴
                    return w + 4;
                }
                finally
                {
                    if (disposeFont) valFont.Dispose();
                }
            }
            else
            {
                // [普通逻辑] 标签 + 数值 + 间距
                // 1. Label
                string label = item.ShortLabel;
                Font labelFont, valueFont;
                bool disposeFont = false;

                if (_mode == LayoutMode.Taskbar)
                {
                    var fs = s.Bold ? FontStyle.Bold : FontStyle.Regular;
                    var f = new Font(s.Font, s.Size, fs);
                    labelFont = f; valueFont = f;
                    disposeFont = true;
                }
                else
                {
                    labelFont = _t.FontItem;
                    valueFont = _t.FontValue;
                }

                try
                {
                    int wLabel = TextRenderer.MeasureText(g, label, labelFont,
                        new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;

                    // 2. Value (使用样本值估算 或 真实值)
                    string sample = GenerateSampleText(item);

                    int wValue = TextRenderer.MeasureText(g, sample, valueFont,
                        new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;

                    // 3. Padding
                    int paddingX;
                    if (_mode == LayoutMode.Taskbar || _settings.HorizontalFollowsTaskbar)
                        paddingX = (int)Math.Round(s.Inner * dpi);
                    else
                        paddingX = (int)Math.Round(_settings.HorizontalInnerSpacing * dpi);

                    return wLabel + wValue + paddingX;
                }
                finally
                {
                    if (disposeFont)
                    {
                        labelFont.Dispose();
                        // valueFont is same reference as labelFont in Taskbar mode
                    }
                }
            }
        }

        private string GenerateSampleText(MetricItem item)
        {
            // 1. 优先获取 TextValue
            string val = item.TextValue ?? "";
            
            // ★★★ 优化：直接调用 GetUnitStr 获取默认单位 ★★★
            // 无论是硬件(°C) 还是 插件(从InfoService查)，这里都能统一拿到了
            string rawUnit = MetricUtils.GetUnitStr(item.Key, 0, MetricUtils.UnitContext.Taskbar);

            // 2. 硬件监控项的特殊处理（如果没有 TextValue 且不是插件项，则使用硬件估算逻辑）
            if (string.IsNullOrEmpty(val) && !item.Key.StartsWith("DASH.", StringComparison.OrdinalIgnoreCase))
            {
                val = MetricUtils.GetSampleValueStr(item.Key);

                // [特殊保留] 网速/硬盘仍需强制 "MB"，防止 GetUnitStr 返回 "KB" 导致宽度不够出现抖动
                var type = MetricUtils.GetType(item.Key);
                if (type == MetricType.DataSpeed || type == MetricType.DataSize)
                {
                    rawUnit = "MB"; 
                }
            }

            // 2. 处理显示单位 (叠加用户配置)
            string userFmt = item.BoundConfig?.UnitTaskbar;
            string unit = MetricUtils.GetDisplayUnit(item.Key, rawUnit, userFmt);

            // 3. 拼接并生成样本 (将所有数字替换为 '0')
            // [Optimization] 使用 string.Create 避免中间数组分配 (Net 8.0+)
            bool appendUnit = !string.IsNullOrEmpty(unit) && !val.EndsWith(unit);
            int totalLen = val.Length + (appendUnit ? unit.Length : 0);
            
            return string.Create(totalLen, (val, unit, appendUnit), (span, state) =>
            {
                var (v, u, append) = state;
                int pos = 0;
                
                // 写入数值部分 (数字转0)
                foreach (char c in v)
                {
                    span[pos++] = char.IsDigit(c) ? '0' : c;
                }
                
                // 写入单位部分
                if (append)
                {
                    foreach (char c in u)
                    {
                        span[pos++] = char.IsDigit(c) ? '0' : c; 
                    }
                }
            });
        }

        // [通用方案] 获取当前布局的签名是否变化 (用于检测是否需要重绘)
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
    }

    public class Column
    {
        public string GroupKey = "";
        public List<MetricItem> Slots = new();
        public int ColumnWidth;
        public Rectangle Bounds = Rectangle.Empty;
        public Rectangle[] SlotBounds = Array.Empty<Rectangle>();
        public bool HasSeparatorBefore = false;

        public int SlotCount => Slots.Count;

        public MetricItem? this[int index] => (uint)index < (uint)Slots.Count ? Slots[index] : null;
    }
}