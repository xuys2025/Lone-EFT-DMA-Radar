using System.Numerics;

namespace LoneEftDmaRadar.UI.Misc
{
    public static class LootFinder
    {
        private static Vector3? _targetPosition;
        private static DateTime _endTime;
        private static readonly TimeSpan _duration = TimeSpan.FromSeconds(3);

        public static void SetTarget(Vector3 position)
        {
            _targetPosition = position;
            _endTime = DateTime.Now.Add(_duration);
        }

        public static Vector3? GetActiveTarget()
        {
            if (_targetPosition.HasValue && DateTime.Now < _endTime)
            {
                return _targetPosition.Value;
            }
            _targetPosition = null; // Expired
            return null;
        }
    }
}
