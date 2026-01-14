using LoneEftDmaRadar.Tarkov.World.Player;

namespace LoneEftDmaRadar.Tarkov.World.Hazards
{
    /// <summary>
    /// Manages voice alerts for proximity to hazards (mines, snipers).
    /// </summary>
    public class HazardsAlertManager
    {
        private readonly IReadOnlyList<IWorldHazard> _hazards;
        private readonly HashSet<IWorldHazard> _alertedMines = new();
        private readonly HashSet<IWorldHazard> _alertedSnipers = new();
        private const float ALERT_DISTANCE = 20f;

        public HazardsAlertManager(IReadOnlyList<IWorldHazard> hazards)
        {
            _hazards = hazards ?? throw new ArgumentNullException(nameof(hazards));
        }

        /// <summary>
        /// Updates hazard alerts based on local player position.
        /// </summary>
        public void Update(LocalPlayer localPlayer)
        {
            if (localPlayer is null)
                return;

            // Skip if voice is disabled
            if (!Program.Config.Voice.Enabled)
                return;

            var playerPos = localPlayer.Position;
            var tempMines = new HashSet<IWorldHazard>();
            var tempSnipers = new HashSet<IWorldHazard>();

            foreach (var hazard in _hazards)
            {
                float distance = Vector3.Distance(playerPos, hazard.Position);
                
                if (distance <= ALERT_DISTANCE)
                {
                    if (IsMinefield(hazard))
                    {
                        tempMines.Add(hazard);
                        // Alert if not already alerted
                        if (!_alertedMines.Contains(hazard))
                        {
                            Misc.VoiceManager.Play("000DANGER", true);
                            Misc.VoiceManager.Play("接近地雷");
                            _alertedMines.Add(hazard);
                            break; // One alert per update is enough
                        }
                    }
                    else if (IsSniperZone(hazard))
                    {
                        tempSnipers.Add(hazard);
                        // Alert if not already alerted
                        if (!_alertedSnipers.Contains(hazard))
                        {
                            Misc.VoiceManager.Play("000DANGER", true);
                            Misc.VoiceManager.Play("接近狙击手");
                            _alertedSnipers.Add(hazard);
                            break; // One alert per update is enough
                        }
                    }
                }
            }

            // Remove alerts for hazards no longer in range
            _alertedMines.RemoveWhere(h => !tempMines.Contains(h));
            _alertedSnipers.RemoveWhere(h => !tempSnipers.Contains(h));
        }

        /// <summary>
        /// Checks if a hazard is a minefield.
        /// </summary>
        private static bool IsMinefield(IWorldHazard hazard)
        {
            if (string.IsNullOrEmpty(hazard.HazardType))
                return false;

            var type = hazard.HazardType.ToLowerInvariant();
            return type.Contains("mine") || type.Contains("minefield") || 
                   type.Contains("地雷") || type.Contains("雷区");
        }

        /// <summary>
        /// Checks if a hazard is a sniper zone.
        /// </summary>
        private static bool IsSniperZone(IWorldHazard hazard)
        {
            if (string.IsNullOrEmpty(hazard.HazardType))
                return false;

            var type = hazard.HazardType.ToLowerInvariant();
            return type.Contains("sniper") || type.Contains("狙击") || 
                   type.Contains("射手") || type.Contains("zryachiy");
        }
    }
}
