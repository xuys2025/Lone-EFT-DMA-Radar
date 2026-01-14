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

using LoneEftDmaRadar.Tarkov.World.Player.Helpers;

namespace LoneEftDmaRadar.Tarkov.World.Player
{
    /// <summary>
    /// Manages voice alerts for players aiming at local player.
    /// </summary>
    public class AimingAlertManager
    {
        private const int MULTIPLE_AIMING_THRESHOLD = 3; // 3个及以上
        private const float SUSTAINED_AIMING_DURATION = 5.0f; // 5秒

        // Track aiming start times for sustained aiming detection
        private readonly Dictionary<ulong, DateTime> _pmcAimingStartTimes = new();
        private readonly Dictionary<ulong, DateTime> _scavAimingStartTimes = new();

        // Track if we've already alerted for multiple aiming
        private bool _alertedMultiplePMC = false;
        private bool _alertedMultipleSCAV = false;

        // Track which players we've alerted for sustained aiming
        private readonly HashSet<ulong> _alertedSustainedPMC = new();
        private readonly HashSet<ulong> _alertedSustainedSCAV = new();

        /// <summary>
        /// Updates aiming alerts based on current players.
        /// </summary>
        public void Update(IReadOnlyCollection<AbstractPlayer> players, LocalPlayer localPlayer)
        {
            if (localPlayer is null)
                return;

            // Skip if voice is disabled
            if (!Program.Config.Voice.Enabled)
                return;

            var pmcAimingNow = new HashSet<ulong>();
            var scavAimingNow = new HashSet<ulong>();

            // Check which hostile players are aiming at local player
            foreach (var player in players)
            {
                if (!player.IsHostileActive)
                    continue;

                ulong playerAddr = player;

                // Check if this player is aiming at local player
                if (player.IsFacingTarget(localPlayer, Program.Config.UI.MaxDistance))
                {
                    if (player.IsPmc)
                    {
                        pmcAimingNow.Add(playerAddr);
                        
                        // Track sustained aiming start time
                        if (!_pmcAimingStartTimes.ContainsKey(playerAddr))
                        {
                            _pmcAimingStartTimes[playerAddr] = DateTime.Now;
                        }
                    }
                    else if (player.IsScav || player.Type == PlayerType.PScav)
                    {
                        scavAimingNow.Add(playerAddr);
                        
                        // Track sustained aiming start time
                        if (!_scavAimingStartTimes.ContainsKey(playerAddr))
                        {
                            _scavAimingStartTimes[playerAddr] = DateTime.Now;
                        }
                    }
                }
            }

            // Check for multiple aiming alerts
            CheckMultipleAiming(pmcAimingNow.Count, scavAimingNow.Count);

            // Check for sustained aiming alerts
            CheckSustainedAiming(_pmcAimingStartTimes, pmcAimingNow, _alertedSustainedPMC, true);
            CheckSustainedAiming(_scavAimingStartTimes, scavAimingNow, _alertedSustainedSCAV, false);

            // Clean up players no longer aiming
            CleanupAimingTracking(_pmcAimingStartTimes, pmcAimingNow, _alertedSustainedPMC);
            CleanupAimingTracking(_scavAimingStartTimes, scavAimingNow, _alertedSustainedSCAV);

            // Reset multiple aiming alerts if count drops below threshold
            if (pmcAimingNow.Count < MULTIPLE_AIMING_THRESHOLD)
                _alertedMultiplePMC = false;
            if (scavAimingNow.Count < MULTIPLE_AIMING_THRESHOLD)
                _alertedMultipleSCAV = false;
        }

        /// <summary>
        /// Checks and triggers alerts for multiple players aiming.
        /// </summary>
        private void CheckMultipleAiming(int pmcCount, int scavCount)
        {
            // Alert for multiple PMCs aiming
            if (pmcCount >= MULTIPLE_AIMING_THRESHOLD && !_alertedMultiplePMC)
            {
                Misc.VoiceManager.Play("000CAUTION", true);
                Misc.VoiceManager.Play("多个PMC瞄准");
                _alertedMultiplePMC = true;
            }

            // Alert for multiple SCAVs aiming
            if (scavCount >= MULTIPLE_AIMING_THRESHOLD && !_alertedMultipleSCAV)
            {
                Misc.VoiceManager.Play("000CAUTION", true);
                Misc.VoiceManager.Play("多个SCAV瞄准");
                _alertedMultipleSCAV = true;
            }
        }

        /// <summary>
        /// Checks and triggers alerts for sustained aiming.
        /// </summary>
        private void CheckSustainedAiming(
            Dictionary<ulong, DateTime> aimingStartTimes,
            HashSet<ulong> currentlyAiming,
            HashSet<ulong> alerted,
            bool isPMC)
        {
            foreach (var playerAddr in currentlyAiming)
            {
                if (aimingStartTimes.TryGetValue(playerAddr, out var startTime))
                {
                    var duration = (DateTime.Now - startTime).TotalSeconds;
                    
                    // If sustained for 5+ seconds and not yet alerted
                    if (duration >= SUSTAINED_AIMING_DURATION && !alerted.Contains(playerAddr))
                    {
                        if (isPMC)
                        {
                            Misc.VoiceManager.Play("000DANGER", true);
                            Misc.VoiceManager.Play("PMC持续瞄准");
                        }
                        else
                        {
                            Misc.VoiceManager.Play("000DANGER", true);
                            Misc.VoiceManager.Play("SCAV持续瞄准");
                        }
                        alerted.Add(playerAddr);
                        break; // Only one alert per update cycle
                    }
                }
            }
        }

        /// <summary>
        /// Cleans up tracking for players no longer aiming.
        /// </summary>
        private static void CleanupAimingTracking(
            Dictionary<ulong, DateTime> aimingStartTimes,
            HashSet<ulong> currentlyAiming,
            HashSet<ulong> alerted)
        {
            var toRemove = aimingStartTimes.Keys.Where(addr => !currentlyAiming.Contains(addr)).ToList();
            foreach (var addr in toRemove)
            {
                aimingStartTimes.Remove(addr);
                alerted.Remove(addr);
            }
        }
    }
}
