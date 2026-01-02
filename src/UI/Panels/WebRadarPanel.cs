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
using LoneEftDmaRadar.UI.Localization;
using LoneEftDmaRadar.Web.WebRadar;

namespace LoneEftDmaRadar.UI.Panels
{
    /// <summary>
    /// Web Radar Panel for the ImGui-based Radar.
    /// </summary>
    internal static class WebRadarPanel
    {
        private enum StartButtonState
        {
            Start,
            Starting,
            Running
        }

        // Panel-local state (moved from RadarUIState)
        private static string _bindAddress;
        private static string _port;
        private static string _tickRate;
        private static bool _upnpEnabled;
        private static readonly string _password;
        private static bool _isRunning;
        private static StartButtonState _startButtonState = StartButtonState.Start;
        private static string _serverUrl = string.Empty;
        private static bool _uiEnabled = true;

        private static EftDmaConfig Config { get; } = Program.Config;

        static WebRadarPanel()
        {
            // Initialize from config
            _bindAddress = Config.WebRadar.IP ?? "0.0.0.0";
            _port = Config.WebRadar.Port ?? "55555";
            _tickRate = Config.WebRadar.TickRate ?? "60";
            _upnpEnabled = Config.WebRadar.UPnP;
            _password = WebRadarServer.Password;
        }

        /// <summary>
        /// Draw the web radar panel.
        /// </summary>
        public static void Draw()
        {
            ImGui.SeparatorText(Loc.T("Web Radar Server"));

            if (!_uiEnabled)
            {
                ImGui.BeginDisabled();
            }

            // Server Configuration
            ImGui.Text(Loc.T("Bind Address:"));
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputText("##BindAddress", ref _bindAddress, 64))
            {
                Config.WebRadar.IP = _bindAddress;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Loc.T("IP address to bind the server to (0.0.0.0 for all interfaces)"));
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), Loc.T("(e.g., 0.0.0.0 for all interfaces)"));

            ImGui.Text(Loc.T("Port:"));
            ImGui.SetNextItemWidth(100);
            if (ImGui.InputText("##Port", ref _port, 6))
            {
                if (int.TryParse(_port, out _))
                {
                    Config.WebRadar.Port = _port;
                }
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Loc.T("Port number for the web radar server"));

            ImGui.Text(Loc.T("Tick Rate (Hz):"));
            ImGui.SetNextItemWidth(100);
            if (ImGui.InputText("##TickRate", ref _tickRate, 4))
            {
                if (int.TryParse(_tickRate, out _))
                {
                    Config.WebRadar.TickRate = _tickRate;
                }
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Loc.T("Update frequency in Hz (higher = more responsive, more bandwidth)"));

            if (ImGui.Checkbox(Loc.WithId("Enable UPnP##EnableUPnP"), ref _upnpEnabled))
            {
                Config.WebRadar.UPnP = _upnpEnabled;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Loc.T("Automatically configure port forwarding on your router"));
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), Loc.T("(Automatic port forwarding)"));

            ImGui.Separator();

            // Password (read-only, auto-generated)
            ImGui.Text(Loc.T("Session Password:"));
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.2f, 0.8f, 0.2f, 1f), _password);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Loc.T("Auto-generated password for this session"));

            if (!_uiEnabled)
            {
                ImGui.EndDisabled();
            }

            ImGui.Separator();

            // Start/Stop Button
            string startButtonLabel = _startButtonState switch
            {
                StartButtonState.Start => Loc.T("Start"),
                StartButtonState.Starting => Loc.T("Starting..."),
                StartButtonState.Running => Loc.T("Running..."),
                _ => Loc.T("Start")
            };

            if (ImGui.Button(startButtonLabel, new Vector2(150, 30)))
            {
                if (!_isRunning)
                {
                    StartServer();
                }
            }
            if (ImGui.IsItemHovered() && !_isRunning)
                ImGui.SetTooltip(Loc.T("Start the web radar server"));

            // Server URL
            if (!string.IsNullOrEmpty(_serverUrl))
            {
                ImGui.Separator();
                ImGui.Text(Loc.T("Server URL:"));
                ImGui.TextWrapped(_serverUrl);

                if (ImGui.Button(Loc.WithId("Copy URL##CopyUrl")))
                {
                    CopyUrlToClipboard();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(Loc.T("Copy the URL to share with teammates"));
            }

            ImGui.Separator();

            // Instructions
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), Loc.T("Instructions:"));
            ImGui.BulletText(Loc.T("Configure the server settings above"));
            ImGui.BulletText(Loc.T("Click 'Start' to begin the web radar server"));
            ImGui.BulletText(Loc.T("Share the generated URL with teammates"));
            ImGui.BulletText(Loc.T("They can view the radar in their web browser"));
        }

        private static async void StartServer()
        {
            _uiEnabled = false;
            _startButtonState = StartButtonState.Starting;

            try
            {
                var tickRate = TimeSpan.FromSeconds(1) / int.Parse(_tickRate);
                string bindIP = _bindAddress.Trim();
                int port = int.Parse(_port);

                var externalIP = await WebRadarServer.GetExternalIPAsync();
                await WebRadarServer.StartAsync(bindIP, port, tickRate, _upnpEnabled);

                _isRunning = true;
                _startButtonState = StartButtonState.Running;
                _serverUrl = $"https://webradar.lone-dma.org/?host={externalIP}&port={port}&password={_password}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(RadarWindow.Handle,
                    string.Format(Loc.T("ERROR Starting Web Radar Server: {0}"), ex.Message),
                    Loc.T("Web Radar"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _startButtonState = StartButtonState.Start;
                _uiEnabled = true;
            }
        }

        private static void CopyUrlToClipboard()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_serverUrl))
                {
                    Clipboard.SetText(_serverUrl);
                    MessageBox.Show(RadarWindow.Handle, Loc.T("Web Radar URL copied to clipboard."), Loc.T("Web Radar"),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(RadarWindow.Handle,
                    string.Format(Loc.T("Failed to copy URL: {0}"), ex.Message),
                    Loc.T("Web Radar"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
