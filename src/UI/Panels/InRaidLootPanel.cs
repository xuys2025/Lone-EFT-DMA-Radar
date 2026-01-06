using ImGuiNET;
using LoneEftDmaRadar.Tarkov.World.Loot;
using LoneEftDmaRadar.UI.Localization;
using LoneEftDmaRadar.Web.TarkovDev;
using System.Numerics;

namespace LoneEftDmaRadar.UI.Panels
{
    public sealed class InRaidLootPanel
    {
        private List<LootItem> _cachedLoot = new();
        private List<LootItem> _filteredLoot = new();
        private DateTime _lastUpdate = DateTime.MinValue;
        private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(3);
        private string _filterText = string.Empty;
        private float _contentWidth = 350f;

        public string Title => Loc.Title("Raid Loot");

        public void Render()
        {
            bool shouldRefilter = false;

            // Only update the list occasionally to save performance
            if (DateTime.Now - _lastUpdate > _updateInterval)
            {
                if (Memory.Loot?.LootItems is { } items)
                {
                    _cachedLoot = items
                        .OrderByDescending(x => x.Price)
                        .ToList();
                }
                else
                {
                    _cachedLoot.Clear();
                }
                _lastUpdate = DateTime.Now;
                shouldRefilter = true;
            }
            
            // Allow showing the collapse button
            ImGuiStylePtr style = ImGui.GetStyle();
            ImGuiDir prevDir = style.WindowMenuButtonPosition;
            style.WindowMenuButtonPosition = ImGuiDir.Left;

            var io = ImGui.GetIO();

            // Position: Bottom-Left
            ImGui.SetNextWindowPos(new Vector2(10f, io.DisplaySize.Y - 10f), ImGuiCond.Always, new Vector2(0.0f, 1.0f));
            
            // Constrain width: Increase min width to 350 and max to 50% of screen to fit long item names
            ImGui.SetNextWindowSizeConstraints(new Vector2(350, 0), new Vector2(io.DisplaySize.X * 0.5f, io.DisplaySize.Y * 0.5f));

            // Added NoScrollbar to prevent double scrollbars
            var flags = ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar;

            if (ImGui.Begin(Title, flags)) 
            {
                // Search Box
                ImGui.SetNextItemWidth(-1); // Use full width
                if (ImGui.InputTextWithHint("###Search", Loc.T("Search..."), ref _filterText, 64))
                {
                    shouldRefilter = true;
                }

                if (shouldRefilter)
                {
                    if (string.IsNullOrWhiteSpace(_filterText))
                    {
                        _filteredLoot = _cachedLoot.ToList();
                    }
                    else
                    {
                        string search = _filterText.Trim();
                        _filteredLoot = _cachedLoot
                            .Where(x => (x.Name ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) || 
                                        (x.ShortName ?? "").Contains(search, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }

                    // Calculate required width based on content
                    float maxTextWidth = 0f;
                    foreach (var item in _filteredLoot)
                    {
                        string txt = $"{FormatPrice(item.Price)} - {item.Name ?? item.ID}";
                        var size = ImGui.CalcTextSize(txt);
                        if (size.X > maxTextWidth) maxTextWidth = size.X;
                    }
                    // Checkbox (~20) + Spacing (~8) + Text + Padding (~20) + Scrollbar allowance (~20)
                    _contentWidth = maxTextWidth + ImGui.GetFrameHeight() + style.ItemSpacing.X + 40f;
                }

                float itemHeight = ImGui.GetTextLineHeightWithSpacing();
                float totalHeight = _filteredLoot.Count * itemHeight;
                
                // Show approx 8 items
                float maxHeight = itemHeight * 8.5f; 
                float childHeight = Math.Clamp(totalHeight, itemHeight, maxHeight);
                
                // Use calculated content width, clamped to min/max constraints
                float childWidth = Math.Clamp(_contentWidth, 350f, io.DisplaySize.X * 0.5f);

                // Pass actual computed width
                if (ImGui.BeginChild("LootList", new Vector2(childWidth, childHeight), ImGuiChildFlags.Borders))
                {
                    // Use Clipper for performance with large lists
                    unsafe
                    {
                        ImGuiListClipper clipper;
                        ImGuiListClipperPtr clipperPtr = new ImGuiListClipperPtr(&clipper);
                        clipperPtr.Begin(_filteredLoot.Count);
                        
                        while (clipperPtr.Step())
                        {
                            for (int i = clipperPtr.DisplayStart; i < clipperPtr.DisplayEnd; i++)
                            {
                                if (i >= _filteredLoot.Count) break;

                                var item = _filteredLoot[i];
                                
                                bool forceShow = item.ForceShow;
                                ImGui.PushID(i);
                                if (ImGui.Checkbox("", ref forceShow))
                                {
                                    item.ForceShow = forceShow;
                                    Memory.Loot?.RefreshFilter();
                                }
                                if (ImGui.IsItemHovered())
                                    ImGui.SetTooltip(Loc.T("Force show on map (Overrides filter)"));
                                
                                ImGui.SameLine();
                                if (ImGui.Button("Find"))
                                {
                                    Misc.LootFinder.SetTarget(item.Position);
                                }
                                if (ImGui.IsItemHovered())
                                    ImGui.SetTooltip(Loc.T("Show indicator on map for 3 seconds"));

                                ImGui.PopID();
                                ImGui.SameLine();

                                string displayText = $"{FormatPrice(item.Price)} - {item.Name ?? item.ID}";
                                
                                // Color code based on price
                                Vector4 color = GetColorForPrice(item.Price);
                                ImGui.TextColored(color, displayText);
                            }
                        }
                        clipperPtr.End();
                    }
                }
                ImGui.EndChild();
            }
            ImGui.End();

            style.WindowMenuButtonPosition = prevDir;
        }

        private static string FormatPrice(int price)
        {
            if (price >= 1000000)
                return $"{(price / 1000000f):0.##}M";
            if (price >= 1000)
                return $"{(price / 1000f):0}k";
            return price.ToString();
        }

        private static Vector4 GetColorForPrice(int price)
        {
            if (price >= 500000) return new Vector4(1.0f, 0.2f, 0.2f, 1.0f); // Red
            if (price >= 100000) return new Vector4(0.8f, 0.4f, 1.0f, 1.0f); // Purple
            if (price >= 50000) return new Vector4(0.2f, 0.6f, 1.0f, 1.0f);  // Blue
            if (price >= 10000) return new Vector4(0.2f, 1.0f, 0.2f, 1.0f);  // Green
            return new Vector4(1.0f, 1.0f, 1.0f, 1.0f);                      // White
        }
    }
}
