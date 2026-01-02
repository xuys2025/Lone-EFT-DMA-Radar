using ImGuiNET;
using System.Reflection;

namespace LoneEftDmaRadar.UI.Localization
{
    internal static class ImGuiFonts
    {
        private static volatile bool _pending;
        private static float _pendingScale = 1.0f;
        private static float _appliedScale = -1.0f;
        private static volatile bool _initialSetupDone;

        /// <summary>
        /// Configure fonts for ImGui atlas. Call this from ImGuiController's onConfigureIO callback
        /// which runs BEFORE the font atlas is built and locked.
        /// </summary>
        public static void ConfigureFontsForAtlas(float uiScale)
        {
            try
            {
                var io = ImGui.GetIO();
                float fontSize = Math.Clamp(16f * uiScale, 12f, 28f);

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

                    var ranges = io.Fonts.GetGlyphRangesChineseFull();
                    io.Fonts.AddFontFromFileTTF(candidate, fontSize, null, ranges);
                    
                    Logging.WriteLine($"ImGuiFonts: Configured Chinese font: {candidate} ({fontSize:0.0}px)");
                    _appliedScale = uiScale;
                    _initialSetupDone = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"ImGuiFonts: Error configuring font: {ex.Message}");
            }
        }

        /// <summary>
        /// Request loading a Chinese-capable ImGui font. The actual font atlas mutation must happen
        /// BEFORE ImGui.NewFrame() (i.e. before ImGuiController.Update), otherwise cimgui will assert.
        /// </summary>
        public static void RequestChineseFont(float uiScale = 1.0f)
        {
            _pendingScale = uiScale;
            _pending = true;
        }

        /// <summary>
        /// Request rebuilding the current font with a new scale. Safe to call from ImGui callbacks.
        /// </summary>
        public static void RequestRebuildWithScale(float uiScale)
        {
            // Queue a rebuild with the new scale, which will happen before the next NewFrame
            _pendingScale = uiScale;
            _pending = true;
        }

        /// <summary>
        /// Apply any pending font requests. Call this right before ImGuiController.Update().
        /// </summary>
        public static void ApplyPending(object imguiController)
        {
            // If we have no ImGui context yet, wait.
            if (ImGui.GetCurrentContext() == IntPtr.Zero)
                return;

            // Only apply if there's a pending request with a different scale
            if (!_pending)
            {
                // On first render frame, force initial setup if not already done
                if (!_initialSetupDone)
                {
                    _initialSetupDone = true;
                    _pending = true;
                    if (_pendingScale == 0f) _pendingScale = 1.0f; // default scale
                }
                else
                {
                    return;
                }
            }

            // Coalesce repeated requests.
            var scale = _pendingScale;
            if (Math.Abs(scale - _appliedScale) < 0.001f)
            {
                _pending = false;
                return;
            }

            try
            {
                if (TryBuildChineseFont(scale))
                {
                    _appliedScale = scale;
                    // Let ImGuiController rebuild font texture on next Update
                    RecreateFontDeviceTexture(imguiController);
                }

                // Apply FontGlobalScale here (safe because we're before NewFrame)
                try
                {
                    ImGui.GetIO().FontGlobalScale = scale;
                }
                catch
                {
                    // Ignore if context isn't ready
                }
            }
            finally
            {
                _pending = false;
            }
        }

        /// <summary>
        /// Back-compat helper. Prefer RequestChineseFont + ApplyPending.
        /// </summary>
        public static void TryUseChineseFont(float uiScale = 1.0f) => RequestChineseFont(uiScale);

        private static bool TryBuildChineseFont(float uiScale)
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
                    
                    // Do NOT call io.Fonts.Build() here - ImGuiController will build it automatically.
                    // Calling Build() here causes "Locked ImFontAtlas" assertions.
                    
                    Logging.WriteLine($"ImGuiFonts: Loaded Chinese font: {candidate} ({fontSize:0.0}px)");
                    return true;
                }
            }
            catch
            {
                // If font loading fails, fall back to default font (may not have CJK glyphs).
            }

            return false;
        }

        private static void RecreateFontDeviceTexture(object imguiController)
        {
            if (imguiController is null)
                return;

            try
            {
                // Silk.NET's ImGuiController provides a method to rebuild the GPU font texture.
#pragma warning disable IL2075
                var method = imguiController.GetType().GetMethod(
                    "RecreateFontDeviceTexture",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
#pragma warning restore IL2075

                method?.Invoke(imguiController, null);
            }
            catch
            {
                // Best effort; if we can't recreate the font texture, the controller may still do it later.
            }
        }
    }
}
