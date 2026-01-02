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

        public string Title => Loc.T("Memory Writing");

        public bool IsVisible { get; set; } = true;

        public void Render()
        {
            if (!IsVisible) return;

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(300, 150), ImGuiCond.FirstUseEver);
            
            // NoClose flag is not directly available in ImGui.Begin(string name, flags). 
            // If we don't pass a ref bool p_open, there is no close button.
            // We want it to be collapsible, so we don't use NoCollapse.
            if (ImGui.Begin(Title)) 
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
        }
    }
}
