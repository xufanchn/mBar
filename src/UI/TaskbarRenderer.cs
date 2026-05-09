using mBar.src.Core;
using System.Drawing.Text;

namespace mBar
{
    /// <summary>
    /// 任务栏渲染器（仅负责绘制，不再负责布局）
    /// </summary>
    public static class TaskbarRenderer
    {
        //private static readonly Settings _settings = Settings.Load();

        // 字体缓存 - 直接初始化，避免每次渲染都创建字体
        private static Font? _cachedFont = null;

        // 浅色主题 — Windows 状态栏风格
        private static readonly Color LABEL_LIGHT = Color.FromArgb(20, 20, 20);
        private static readonly Color SAFE_LIGHT = Color.FromArgb(0x00, 0xAA, 0xBB);  // cyan-teal
        private static readonly Color WARN_LIGHT = Color.FromArgb(0xCC, 0x77, 0x00);
        private static readonly Color CRIT_LIGHT = Color.FromArgb(0xCC, 0x22, 0x33);

        // 深色主题 — 科幻霓虹风
        private static readonly Color LABEL_DARK = Color.FromArgb(0xDD, 0xDD, 0xDD);
        private static readonly Color SAFE_DARK = Color.FromArgb(0x00, 0xE5, 0xFF);   // cyan neon
        private static readonly Color WARN_DARK = Color.FromArgb(0xFF, 0xCC, 0x33);   // amber neon
        private static readonly Color CRIT_DARK = Color.FromArgb(0xFF, 0x44, 0x55);   // red neon

        // ★★★ [新增] 自定义颜色缓存 ★★★
        private static bool _useCustom = false;
        private static Color _cLabel, _cSafe, _cWarn, _cCrit;

        // ★★★ [新增] 极简的核心：手动刷新缓存 ★★★
        // 在 UIController 初始化或配置变更时调用它
        public static void ReloadStyle(Settings cfg)
        {
            var s = cfg.GetStyle();

            // ★★★ 修复：先释放旧字体资源，防止 GDI 句柄泄漏 ★★★
            if (_cachedFont != null)
            {
                try { _cachedFont.Dispose(); } catch { }
                _cachedFont = null;
            }

            // 清除缩放字体缓存
            foreach (var f in _scaledFontCache.Values) { try { f.Dispose(); } catch { } }
            _scaledFontCache.Clear();

            // 无论开关怎么变，这里拿到的永远是正确参数
            _cachedFont = UIUtils.GetFont(s.Font, s.Size, s.Bold);

            // 颜色依然允许自定义
            _useCustom = cfg.TaskbarCustomStyle;
            if (_useCustom)
            {
                try {
                    _cLabel = ColorTranslator.FromHtml(cfg.TaskbarColorLabel);
                    _cSafe = ColorTranslator.FromHtml(cfg.TaskbarColorSafe);
                    _cWarn = ColorTranslator.FromHtml(cfg.TaskbarColorWarn);
                    _cCrit = ColorTranslator.FromHtml(cfg.TaskbarColorCrit);
                } catch {
                    // 容错：如果解析失败，回退到默认
                    _useCustom = false;
                }
            }
        }

        public static void Render(Graphics g, List<Column> cols, bool light, int taskbarHeight, Color separatorColor, Color iconColor)
        {
            if (_cachedFont == null)
                ReloadStyle(Settings.Load());

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            float dpi = g.DpiX / 96f;
            using var sepPen = new Pen(separatorColor, 1f * dpi);

            for (int i = 0; i < cols.Count; i++)
            {
                var col = cols[i];

                // Draw separator
                if (col.HasSeparatorBefore)
                {
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

        private static void DrawSlot(Graphics g, MetricItem item, Rectangle rc, bool light, Color iconColor, int slotCount)
        {
            Font font = GetScaledFont(slotCount);
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
                    x += iconSize + 3;
                }
            }

            var remainingRect = new Rectangle(x, rc.Y, rc.Right - x, rc.Height);

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

        // [新增] 辅助：根据状态快速获取颜色 (替代原来的 PickColor)
        private static Color GetStateColor(int state, bool light)
        {
            if (state == 2) return light ? CRIT_LIGHT : CRIT_DARK;
            if (state == 1) return light ? WARN_LIGHT : WARN_DARK;
            return light ? SAFE_LIGHT : SAFE_DARK;
        }

        // [新增] 辅助：自定义模式
        private static Color GetCustomStateColor(int state)
        {
            if (state == 2) return _cCrit;
            if (state == 1) return _cWarn;
            return _cSafe;
        }

        private static int GetIconSize(int rowHeight, int slotCount)
        {
            float ratio = slotCount switch { 1 => 0.65f, 2 => 0.55f, 3 => 0.45f, _ => 0.38f };
            return Math.Max(8, (int)(rowHeight * ratio));
        }

        private static readonly Dictionary<float, Font> _scaledFontCache = new();

        private static Font GetScaledFont(int slotCount)
        {
            float coef = slotCount switch { 1 => 1.0f, 2 => 0.9f, 3 => 0.8f, _ => 0.7f };
            if (Math.Abs(coef - 1.0f) < 0.01f) return _cachedFont!;

            if (!_scaledFontCache.TryGetValue(coef, out var font) || font == null)
            {
                float newSize = _cachedFont!.Size * coef;
                font = new Font(_cachedFont.FontFamily, newSize, _cachedFont.Style);
                _scaledFontCache[coef] = font;
            }
            return font;
        }
    }
}
