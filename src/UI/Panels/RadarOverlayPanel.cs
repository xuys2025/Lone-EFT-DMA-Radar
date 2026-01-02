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

using ImGuiNET;
using LoneEftDmaRadar.UI.Loot;
using LoneEftDmaRadar.UI.Localization;

namespace LoneEftDmaRadar.UI.Panels
{
    /// <summary>
    /// Radar Overlay Panel - Quick access controls shown over the radar.
    /// </summary>
    internal static class RadarOverlayPanel
    {
        // Panel-local state
        private static string _searchText = string.Empty;
        private static bool _lootOverlayVisible;

        private static EftDmaConfig Config { get; } = Program.Config;

        /// <summary>
        /// Hides the loot overlay if it's currently visible.
        /// </summary>
        public static void HideLootOverlay()
        {
            _lootOverlayVisible = false;
        }

        /// <summary>
        /// Apply loot search filter.
        /// </summary>
        private static void ApplyLootSearch()
        {
            LootFilter.SearchString = _searchText?.Trim();
            Memory.Loot?.RefreshFilter();
        }

        /// <summary>
        /// Draw the overlay controls at the top of the radar.
        /// </summary>
        public static void DrawTopBar()
        {
            // Dynamic position in top-left, below menu bar
            // Use GetFrameHeight() to account for UI Scale and Menu Bar height
            float menuBarHeight = ImGui.GetFrameHeight();
            ImGui.SetNextWindowPos(new Vector2(10, menuBarHeight + 5), ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0.7f);

            var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize |
                        ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings;

            if (ImGui.Begin("RadarTopBar", flags))
            {
                // Map Free Toggle Button
                bool isMapFree = RadarWindow.IsMapFreeEnabled;
                if (isMapFree)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.6f, 0.2f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.7f, 0.3f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.1f, 0.5f, 0.1f, 1.0f));
                }

                if (ImGui.Button(Loc.WithId(isMapFree ? "Map Free: ON##MapFree" : "Map Free: OFF##MapFree")))
                {
                    RadarWindow.IsMapFreeEnabled = !isMapFree;
                    if (isMapFree) // Was on, now turning off
                    {
                        RadarWindow.MapPanPosition = Vector2.Zero;
                    }
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(Loc.T("Toggle free map panning (drag to move map)"));

                if (isMapFree)
                {
                    ImGui.PopStyleColor(3);
                }

                // Loot button - only show when loot is enabled
                if (Config.Loot.Enabled)
                {
                    ImGui.SameLine();
                    if (ImGui.Button(Loc.WithId("Loot##LootOverlay")))
                    {
                        _lootOverlayVisible = !_lootOverlayVisible;
                    }
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip(Loc.T("Open loot filter options"));
                }

                ImGui.End();
            }
        }

        /// <summary>
        /// Draw the loot overlay panel (shown when loot button is clicked).
        /// </summary>
        public static void DrawLootOverlay()
        {
            if (!Config.Loot.Enabled)
                return;

            // Loot Options Panel - only show if toggled on
            if (!_lootOverlayVisible)
                return;

            ImGui.SetNextWindowPos(new Vector2(10, 70), ImGuiCond.Appearing);
            ImGui.SetNextWindowBgAlpha(0.9f);

            var flags = ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize;
            if (ImGui.Begin(Loc.Title("Loot Options"), ref _lootOverlayVisible, flags))
            {
                DrawLootOptions();
            }
            ImGui.End();
        }

        private static void DrawLootOptions()
        {
            ImGui.SetNextItemWidth(250);

            // Search
            ImGui.Text(Loc.T("Item Search:"));
            ImGui.SetNextItemWidth(250);
            if (ImGui.InputText("##LootSearch", ref _searchText, 64))
            {
                ApplyLootSearch();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Loc.T("Search for specific items by name"));
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                ImGui.SameLine();
                if (ImGui.Button("X##ClearSearch"))
                {
                    _searchText = string.Empty;
                    ApplyLootSearch();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(Loc.T("Clear search"));
            }

            ImGui.Separator();

            // Value Thresholds - side by side
            ImGui.Text(Loc.T("Min Value:"));
            ImGui.SameLine(150);
            ImGui.Text(Loc.T("Valuable Min:"));

            ImGui.SetNextItemWidth(140);
            int minValue = Config.Loot.MinValue;
            if (ImGui.InputInt("##MinValue", ref minValue, 1000, 10000))
            {
                Config.Loot.MinValue = Math.Max(0, minValue);
                Memory.Loot?.RefreshFilter();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Loc.T("Minimum value to display regular loot"));
            ImGui.SameLine(150);
            ImGui.SetNextItemWidth(140);
            int valuableMin = Config.Loot.MinValueValuable;
            if (ImGui.InputInt("##ValuableMin", ref valuableMin, 1000, 10000))
            {
                Config.Loot.MinValueValuable = Math.Max(0, valuableMin);
                Memory.Loot?.RefreshFilter();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Loc.T("Minimum value to highlight as valuable"));

            ImGui.Separator();

            // Price options on one line
            bool pricePerSlot = Config.Loot.PricePerSlot;
            if (ImGui.Checkbox(Loc.WithId("Price per Slot##PricePerSlot"), ref pricePerSlot))
            {
                Config.Loot.PricePerSlot = pricePerSlot;
                Memory.Loot?.RefreshFilter();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Loc.T("Calculate value based on price per inventory slot"));
            ImGui.SameLine(150);
            ImGui.Text(Loc.T("Mode:"));
            ImGui.SameLine();
            int priceMode = (int)Config.Loot.PriceMode;
            if (ImGui.RadioButton(Loc.WithId("Flea##PriceModeFlea"), ref priceMode, 0))
            {
                Config.Loot.PriceMode = LootPriceMode.FleaMarket;
                Memory.Loot?.RefreshFilter();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Loc.T("Use flea market prices"));
            ImGui.SameLine();
            if (ImGui.RadioButton(Loc.WithId("Trader##PriceModeTrader"), ref priceMode, 1))
            {
                Config.Loot.PriceMode = LootPriceMode.Trader;
                Memory.Loot?.RefreshFilter();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Loc.T("Use trader sell prices"));

            ImGui.Separator();

            // Category toggles
            bool hideCorpses = Config.Loot.HideCorpses;
            if (ImGui.Checkbox(Loc.WithId("Hide Corpses##HideCorpses"), ref hideCorpses))
            {
                Config.Loot.HideCorpses = hideCorpses;
                Memory.Loot?.RefreshFilter();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Loc.T("Hide player corpses from the radar"));
            ImGui.SameLine(150);
            bool showMeds = LootFilter.ShowMeds;
            if (ImGui.Checkbox(Loc.WithId("Show Meds##ShowMeds"), ref showMeds))
            {
                LootFilter.ShowMeds = showMeds;
                Memory.Loot?.RefreshFilter();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Loc.T("Show medical items regardless of value"));

            bool showFood = LootFilter.ShowFood;
            if (ImGui.Checkbox(Loc.WithId("Show Food##ShowFood"), ref showFood))
            {
                LootFilter.ShowFood = showFood;
                Memory.Loot?.RefreshFilter();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Loc.T("Show food and drinks regardless of value"));
            ImGui.SameLine(150);
            bool showBackpacks = LootFilter.ShowBackpacks;
            if (ImGui.Checkbox(Loc.WithId("Show Backpacks##ShowBackpacks"), ref showBackpacks))
            {
                LootFilter.ShowBackpacks = showBackpacks;
                Memory.Loot?.RefreshFilter();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Loc.T("Show backpacks regardless of value"));

            bool showQuestItems = LootFilter.ShowQuestItems;
            if (ImGui.Checkbox(Loc.WithId("Show Quest Items##ShowQuestItems"), ref showQuestItems))
            {
                LootFilter.ShowQuestItems = showQuestItems;
                Memory.Loot?.RefreshFilter();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Loc.T("Show all static quest items on the map."));
        }

        /// <summary>
        /// Draw the map setup helper overlay.
        /// </summary>
        public static void DrawMapSetupHelper()
        {
            bool mapSetupVisible = MapSetupHelperPanel.ShowOverlay;
            if (!mapSetupVisible)
                return;

            var io = ImGui.GetIO();
            ImGui.SetNextWindowPos(new Vector2(io.DisplaySize.X - 310, 10), ImGuiCond.Appearing);
            ImGui.SetNextWindowSize(new Vector2(300, 80), ImGuiCond.Appearing);
            ImGui.SetNextWindowBgAlpha(0.8f);

            if (ImGui.Begin("Map Setup Helper", ref mapSetupVisible, ImGuiWindowFlags.NoSavedSettings))
            {
                ImGui.Text(Loc.T("Current Coordinates:"));
                ImGui.TextColored(new Vector4(0.2f, 0.8f, 0.2f, 1f), MapSetupHelperPanel.Coords);
            }
            ImGui.End();
            MapSetupHelperPanel.ShowOverlay = mapSetupVisible;
        }
    }
}
