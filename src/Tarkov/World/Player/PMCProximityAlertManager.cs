using LoneEftDmaRadar.Tarkov.World.Player;

namespace LoneEftDmaRadar.Tarkov.World.Player
{
    /// <summary>
    /// Manages voice alerts for proximity to hostile PMCs.
    /// </summary>
    public class PMCProximityAlertManager
    {
        private const float DISTANCE_200M = 200f;
        private const float DISTANCE_100M = 100f;
        private const float DISTANCE_50M = 50f;

        /// <summary>
        /// Tracks the alert level for each hostile PMC.
        /// Key: Player Base Address, Value: Current alert level (0=none, 1=200m, 2=100m, 3=50m)
        /// </summary>
        private readonly Dictionary<ulong, int> _pmcAlertLevels = new();

        /// <summary>
        /// Updates proximity alerts for hostile PMCs.
        /// </summary>
        public void Update(IReadOnlyCollection<AbstractPlayer> players, LocalPlayer localPlayer)
        {
            if (localPlayer is null)
                return;

            // Skip if voice is disabled
            if (!Program.Config.Voice.Enabled)
                return;

            var playerPos = localPlayer.Position;
            var activePmcAddrs = new HashSet<ulong>();

            foreach (var player in players)
            {
                // Only check hostile PMCs that are human-controlled, alive, and active
                if (!player.IsHostilePmc || !player.IsHumanActive)
                    continue;

                ulong playerAddr = player; // Implicit conversion to ulong
                activePmcAddrs.Add(playerAddr);
                float distance = Vector3.Distance(playerPos, player.Position);

                // Get current alert level for this PMC (0 if not tracked)
                _pmcAlertLevels.TryGetValue(playerAddr, out int currentLevel);

                int newLevel = GetAlertLevel(distance);

                // Only alert if we've progressed to a closer range
                if (newLevel > currentLevel)
                {
                    TriggerAlert(newLevel);
                    _pmcAlertLevels[playerAddr] = newLevel;
                    break; // Only one alert per update cycle
                }
                else if (newLevel < currentLevel)
                {
                    // PMC moved away, update to current level (allows re-alerting if they come back)
                    _pmcAlertLevels[playerAddr] = newLevel;
                }
            }

            // Clean up alert levels for PMCs that are no longer active
            var addrsToRemove = _pmcAlertLevels.Keys.Where(addr => !activePmcAddrs.Contains(addr)).ToList();
            foreach (var addr in addrsToRemove)
            {
                _pmcAlertLevels.Remove(addr);
            }
        }

        /// <summary>
        /// Determines the alert level based on distance.
        /// </summary>
        /// <param name="distance">Distance to PMC</param>
        /// <returns>Alert level: 0=none, 1=200m, 2=100m, 3=50m</returns>
        private static int GetAlertLevel(float distance)
        {
            if (distance <= DISTANCE_50M)
                return 3;
            if (distance <= DISTANCE_100M)
                return 2;
            if (distance <= DISTANCE_200M)
                return 1;
            return 0;
        }

        /// <summary>
        /// Triggers the appropriate voice alert based on the alert level.
        /// </summary>
        /// <param name="level">Alert level (1=200m, 2=100m, 3=50m)</param>
        private static void TriggerAlert(int level)
        {
            switch (level)
            {
                case 1: // 200 meters
                    Misc.VoiceManager.Play("000C200", true);
                    Misc.VoiceManager.Play("两百米内有PMC");
                    break;
                case 2: // 100 meters
                    Misc.VoiceManager.Play("000C100", true);
                    Misc.VoiceManager.Play("一百米内有PMC");
                    break;
                case 3: // 50 meters
                    Misc.VoiceManager.Play("000C50", true);
                    Misc.VoiceManager.Play("五十米内有PMC");
                    break;
            }
        }
    }
}
