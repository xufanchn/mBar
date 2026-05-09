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
            if (_cache.TryGetValue(cacheKey, out var cached))
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
                using var svg = new SKSvg();
                svg.Load(path);
                if (svg.Model == null) return null;

                var skColor = new SKColor((uint)color.ToArgb() & 0x00FFFFFF | 0xFF000000);
                float maxDim = Math.Max(svg.Model.CullRect.Width, svg.Model.CullRect.Height);
                if (maxDim <= 0) return null;
                float scale = size / maxDim;

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
