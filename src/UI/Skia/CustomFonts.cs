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

namespace LoneEftDmaRadar.UI.Skia
{
    internal static class CustomFonts
    {
        /// <summary>
        /// Neo Sans Std Regular
        /// </summary>
        public static SKTypeface NeoSansStdRegular { get; }

        public static SKTypeface GetUiTypefaceForLanguage(string language)
        {
            if (IsChineseLanguage(language) && TryGetChineseSystemTypeface(out var chinese))
                return chinese;

            return NeoSansStdRegular;
        }

        static CustomFonts()
        {
            try
            {
                byte[] neoSansStdRegular;
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LoneEftDmaRadar.NeoSansStdRegular.otf"))
                {
                    neoSansStdRegular = new byte[stream!.Length];
                    stream.ReadExactly(neoSansStdRegular);
                }
                NeoSansStdRegular = SKTypeface.FromStream(new MemoryStream(neoSansStdRegular, false));
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
