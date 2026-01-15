/*
 * Lone EFT DMA Radar
 * Brought to you by Lone (Lone DMA)
 * 
MIT License

Copyright (c) 2025 Lone DMA

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 *
*/

using LoneEftDmaRadar.Misc;

namespace LoneEftDmaRadar.UI.Skia
{
    internal static class CustomFonts
    {
        /// <summary>
        /// UI 默认字体（优先使用 HarmonyOS Sans 中文字体，备用 NeoSansStdRegular）
        /// </summary>
        public static SKTypeface NeoSansStdRegular { get; }

        public static SKTypeface GetUiTypefaceForLanguage(string language)
        {
            // 使用 HarmonyOS Sans 作为默认字体，支持中英文混合显示
            // 无需根据语言切换字体，HarmonyOS Sans 对中英文都有良好的支持
            return NeoSansStdRegular;
        }

        static CustomFonts()
        {
            try
            {
                // 优先加载 HarmonyOS Sans 中文字体（更好的中英文混合显示效果）
                using (var stream = Utilities.OpenResource("LoneEftDmaRadar.Resources.HarmonyOS_Sans_SC_Regular.ttf"))
                {
                    if (stream != null)
                    {
                        var fontData = new byte[stream.Length];
                        stream.ReadExactly(fontData);
                        NeoSansStdRegular = SKTypeface.FromStream(new MemoryStream(fontData, false));
                        return;
                    }
                }
            }
            catch
            {
                // HarmonyOS Sans 加载失败，尝试备用字体 NeoSansStdRegular
                try
                {
                    using (var stream = Utilities.OpenResource("LoneEftDmaRadar.Resources.NeoSansStdRegular.otf"))
                    {
                        if (stream != null)
                        {
                            var fontData = new byte[stream.Length];
                            stream.ReadExactly(fontData);
                            NeoSansStdRegular = SKTypeface.FromStream(new MemoryStream(fontData, false));
                            return;
                        }
                    }
                }
                catch
                {
                    // 嵌入字体都加载失败，回退到系统字体
                }
            }

            try
            {
                // Fallback to system fonts
                // Priority list:
                // 1. Microsoft YaHei UI - Best for Windows UI (Chinese + English)
                // 2. Microsoft YaHei - Standard Chinese
                // 3. Segoe UI - Standard English (Fallback)
                // 4. Arial - Universal Fallback
                string[] fontFamilies = { "Microsoft YaHei UI", "Microsoft YaHei", "Segoe UI", "Arial" };
                
                SKTypeface typeface = null;
                foreach (var family in fontFamilies)
                {
                    typeface = SKTypeface.FromFamilyName(family);
                    // Verify we actually got the requested family (or close to it)
                    if (typeface != null && typeface.FamilyName.Contains(family, StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                }

                NeoSansStdRegular = typeface ?? SKTypeface.Default;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("ERROR Loading Custom Fonts!", ex);
            }
        }

        private static bool IsChineseLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return false;
            language = language.Trim();
            return language.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryGetChineseSystemTypeface(out SKTypeface typeface)
        {
            // Prefer Windows system fonts with broad CJK coverage.
            // We avoid bundling a large CJK font file to keep the repo light.
            string[] candidates =
            [
                "Microsoft YaHei UI",
                "Microsoft YaHei",
                "SimHei",
                "SimSun",
                "NSimSun",
                "PingFang SC",
                "Noto Sans CJK SC",
                "Source Han Sans SC"
            ];

            foreach (var family in candidates)
            {
                try
                {
                    var tf = SKTypeface.FromFamilyName(family);
                    if (tf is null)
                        continue;

                    // Basic sanity check: can it render a common CJK glyph?
                    var glyphs = tf.GetGlyphs("中");
                    if (glyphs is { Length: > 0 } && glyphs[0] != 0)
                    {
                        typeface = tf;
                        return true;
                    }
                }
                catch
                {
                    // Try next
                }
            }

            typeface = null!;
            return false;
        }
    }
}
