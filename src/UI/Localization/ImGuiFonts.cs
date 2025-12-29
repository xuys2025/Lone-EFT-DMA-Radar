using ImGuiNET;

namespace LoneEftDmaRadar.UI.Localization
{
    internal static class ImGuiFonts
    {
        public static void TryUseChineseFont(float uiScale = 1.0f)
        {
            try
            {
                var io = ImGui.GetIO();

                // Pick a reasonable default size; let existing UI scaling handle the rest.
                float fontSize = Math.Clamp(16f * uiScale, 12f, 28f);

                // Try common Windows CJK fonts (prefer Microsoft YaHei).
                string windowsFonts = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
                string[] candidates =
                [
                    Path.Combine(windowsFonts, "msyh.ttc"),
                    Path.Combine(windowsFonts, "msyh.ttf"),
                    Path.Combine(windowsFonts, "msyhbd.ttf"),
                    Path.Combine(windowsFonts, "simhei.ttf"),
                    Path.Combine(windowsFonts, "simsun.ttc"),
                ];

                foreach (var candidate in candidates)
                {
                    if (!File.Exists(candidate))
                        continue;

                    // Some CJK characters (e.g. "浏") are not part of the "SimplifiedCommon" subset and will
                    // render as '?' if we use that range. Use the full Chinese range to avoid missing glyphs.
                    var ranges = io.Fonts.GetGlyphRangesChineseFull();

                    // Clear default fonts so we don't bloat the atlas.
                    io.Fonts.Clear();

                    // The first font in the atlas becomes the default font.
                    io.Fonts.AddFontFromFileTTF(candidate, fontSize, null, ranges);
                    Logging.WriteLine($"ImGuiFonts: Loaded Chinese font: {candidate} ({fontSize:0.0}px)");
                    return;
                }
            }
            catch
            {
                // If font loading fails, fall back to default font (may not have CJK glyphs).
            }
        }
    }
}
