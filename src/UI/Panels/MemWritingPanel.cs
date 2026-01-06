using ImGuiNET;
using LoneEftDmaRadar.UI.Localization;

namespace LoneEftDmaRadar.UI.Panels
{
    public sealed class MemWritingPanel
    {
        private readonly EftDmaConfig _config;

        public MemWritingPanel(EftDmaConfig config)
        {
            _config = config;
        }

        public string Title => Loc.Title("Memory Writing");

        public bool IsVisible { get; set; } = true;

        public void Render()
        {
            if (!IsVisible) return;
            
            // Temporarily enable collapse button for this window
            ImGuiStylePtr style = ImGui.GetStyle();
            ImGuiDir prevDir = style.WindowMenuButtonPosition;
            style.WindowMenuButtonPosition = ImGuiDir.Left;

            var flags = ImGuiWindowFlags.AlwaysAutoResize;
            if (ImGui.Begin(Title, flags)) 
            {
                bool noRecoil = _config.Misc.NoRecoil;
                if (ImGui.Checkbox(Loc.T("No Recoil"), ref noRecoil))
                {
                    _config.Misc.NoRecoil = noRecoil;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(Loc.T("Removes weapon recoil"));

                bool noSway = _config.Misc.NoSway;
                if (ImGui.Checkbox(Loc.T("No Sway"), ref noSway))
                {
                    _config.Misc.NoSway = noSway;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(Loc.T("Removes weapon sway/breath"));

                bool antiAfk = _config.Misc.AntiAfk;
                if (ImGui.Checkbox(Loc.T("Anti-AFK"), ref antiAfk))
                {
                    _config.Misc.AntiAfk = antiAfk;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(Loc.T("Prevents being kicked for inactivity in menu"));
            }
            ImGui.End();

            style.WindowMenuButtonPosition = prevDir;
        }
    }
}
