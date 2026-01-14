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

namespace LoneEftDmaRadar.Tarkov.World.Exits
{
    /// <summary>
    /// Manages voice alerts for exfiltration point status changes.
    /// </summary>
    public class ExfilAlertManager
    {
        private DateTime? _raidStartTime = null;
        private const int ALERT_DELAY_SECONDS = 30;

        /// <summary>
        /// Notifies that the raid has started.
        /// </summary>
        public void OnRaidStarted()
        {
            // Only set the timestamp on the first call to avoid resetting the countdown
            if (_raidStartTime == null)
            {
                _raidStartTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Checks if voice alerts should be enabled (30 seconds after raid start).
        /// </summary>
        private bool ShouldPlayAlerts()
        {
            if (!Program.Config.Voice.Enabled)
                return false;

            if (_raidStartTime == null)
                return false;

            var elapsed = (DateTime.Now - _raidStartTime.Value).TotalSeconds;
            return elapsed >= ALERT_DELAY_SECONDS;
        }

        /// <summary>
        /// Handles exfil status change notifications.
        /// </summary>
        /// <param name="exfilName">Name of the exfil that changed</param>
        /// <param name="oldStatus">Previous status</param>
        /// <param name="newStatus">New status</param>
        public void OnExfilStatusChanged(string exfilName, Exfil.EStatus oldStatus, Exfil.EStatus newStatus)
        {
            if (!ShouldPlayAlerts())
                return;

            // Only alert on Open <-> Closed transitions
            // Ignore Pending state to reduce noise
            if (oldStatus == Exfil.EStatus.Closed && newStatus == Exfil.EStatus.Open)
            {
                Misc.VoiceManager.Play("撤离点已开启");
            }
            else if (oldStatus == Exfil.EStatus.Open && newStatus == Exfil.EStatus.Closed)
            {
                Misc.VoiceManager.Play("撤离点已关闭");
            }
        }
    }
}
