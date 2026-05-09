using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace mBar.src.Plugins.Native
{
    public static class CryptoNative
    {
        // 原始 Worker URL (作为备用)
        // 注意：此处假设用户原来的 Worker 地址。如果用户未提供，可以在代码中留空让用户填，
        // 但根据上下文，用户提供了 worker 代码，且说 "放到cloudflare worker里"，
        // 我这里使用一个占位符，用户需要在 Crypto.json 中配置，或者在这里硬编码。
        // 为了灵活性，我们尽量通过参数传递 Fallback URL。

        private static HttpClient CreateClient(TimeSpan timeout)
        {
            var handler = new SocketsHttpHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                UseProxy = true,
                Proxy = System.Net.WebRequest.GetSystemWebProxy(),
                SslOptions = new System.Net.Security.SslClientAuthenticationOptions 
                {
                    RemoteCertificateValidationCallback = delegate { return true; }
                }
            };
            var client = new HttpClient(handler);
            client.Timeout = timeout;
            client.DefaultRequestHeaders.Add("User-Agent", "mBar/1.0");
            return client;
        }

        public static async Task<string> FetchAsync(string symbol, string fallbackUrl)
        {
            // 1. 规范化 Symbol (参考 Worker 逻辑)
            symbol = (symbol ?? "BTC").ToUpper();
            if (symbol.Length <= 4 && !symbol.EndsWith("USDT")) symbol += "USDT";
            else if (!symbol.Contains("USDT") && !symbol.Contains("-") && !symbol.Contains("USD")) symbol += "USDT";

            // 2. 尝试直连 Bybit
            try
            {
                string bybitUrl = $"https://api.bybit.com/v5/market/tickers?category=spot&symbol={symbol}";
                using (var client = CreateClient(TimeSpan.FromSeconds(3))) // 直连超时要短
                {
                    var resp = await client.GetAsync(bybitUrl);
                    resp.EnsureSuccessStatusCode();
                    
                    var json = await resp.Content.ReadAsStringAsync();
                    
                    // 解析 Bybit 原生数据并转换为 mBar 标准格式
                    return ParseBybitResponse(json, symbol);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CryptoNative] Direct connect failed: {ex.Message}. Switching to fallback.");
            }

            // 3. 降级到 Worker (Fallback)
            if (!string.IsNullOrEmpty(fallbackUrl))
            {
                try
                {
                    // Fallback URL 可能是 "https://crypto./?symbol={{symbol}}"
                    // 我们需要替换 symbol
                    string targetUrl = fallbackUrl.Replace("{{symbol}}", symbol);
                    
                    // 如果 URL 里本身没有 symbol 参数 (用户只给了 base url)，我们补上
                    if (!targetUrl.Contains("symbol=") && !targetUrl.Contains(symbol))
                    {
                        targetUrl += (targetUrl.Contains("?") ? "&" : "?") + $"symbol={symbol}";
                    }

                    using (var client = CreateClient(TimeSpan.FromSeconds(10)))
                    {
                        var response = await client.GetAsync(targetUrl);
                        response.EnsureSuccessStatusCode();
                        return await response.Content.ReadAsStringAsync();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Crypto Native Failed (Direct & Fallback): {ex.Message}");
                }
            }

            throw new Exception("Crypto Native Failed: Direct connection failed and no fallback provided.");
        }

        private static string ParseBybitResponse(string jsonStr, string symbol)
        {
            using var doc = JsonDocument.Parse(jsonStr);
            var root = doc.RootElement;
            
            if (root.GetProperty("retCode").GetInt32() != 0)
            {
                throw new Exception($"Bybit Error: {root.GetProperty("retMsg").GetString()}");
            }

            var list = root.GetProperty("result").GetProperty("list");
            if (list.GetArrayLength() == 0)
            {
                throw new Exception("Symbol Not Found");
            }

            var data = list[0];

            // 转换逻辑 (Copy from JS Worker)
            // price24hPcnt: "0.025" -> 2.5
            double pcnt = 0;
            if (double.TryParse(data.GetProperty("price24hPcnt").GetString(), out double p))
            {
                pcnt = Math.Round(p * 100, 2);
            }

            var responseData = new
            {
                name = data.GetProperty("symbol").GetString(),
                price = data.GetProperty("lastPrice").GetString(), // 保持字符串或转double，Worker是parseFloat
                change_percent = pcnt,
                high = data.GetProperty("highPrice24h").GetString(),
                low = data.GetProperty("lowPrice24h").GetString(),
                vol = data.GetProperty("turnover24h").GetString(),
                source = "Direct"
            };

            return JsonSerializer.Serialize(responseData);
        }
    }
}
