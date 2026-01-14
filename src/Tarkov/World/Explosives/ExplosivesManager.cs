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

using LoneEftDmaRadar.Tarkov.Unity.Collections;

namespace LoneEftDmaRadar.Tarkov.World.Explosives
{
    public sealed class ExplosivesManager : IReadOnlyCollection<IExplosiveItem>
    {
        private static readonly uint[] _toSyncObjects = [
            Offsets.GameWorld.SynchronizableObjectLogicProcessor,
            Offsets.SynchronizableObjectLogicProcessor._staticSynchronizableObjects];
        private readonly ulong _gameWorld;
        private readonly ConcurrentDictionary<ulong, IExplosiveItem> _explosives = new();
        private readonly HashSet<ulong> _alerted = new(); // Track alerted explosives

        public ExplosivesManager(ulong gameWorld)
        {
            _gameWorld = gameWorld;
        }

        /// <summary>
        /// Check for "hot" explosives in World if due.
        /// </summary>
        public void Refresh(CancellationToken ct)
        {
            GetGrenades(ct);
            GetTripwires(ct);
            var explosives = _explosives.Values;
            if (explosives.Count == 0)
            {
                return;
            }

            using var scatter = Memory.CreateScatter(VmmSharpEx.Options.VmmFlags.NOCACHE);
            foreach (var explosive in explosives)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    explosive.OnRefresh(scatter);
                }
                catch (Exception ex)
                {
                    Logging.WriteLine($"Error Refreshing Explosive @ 0x{explosive.Addr.ToString("X")}: {ex}");
                }
            }
            scatter.Execute();

            // Voice Warnings - check after position updates
            var localPlayer = Memory.LocalPlayer;
            if (localPlayer is not null && localPlayer.IsAlive)
            {
                foreach (var explosive in explosives)
                {
                    // Skip if already alerted for this explosive
                    if (_alerted.Contains(explosive.Addr))
                        continue;

                    float distance = Vector3.Distance(localPlayer.Position, explosive.Position);
                    if (distance <= 15f)
                    {
                        if (explosive is Grenade)
                        {
                            LoneEftDmaRadar.Misc.VoiceManager.Play("000DANGER", true);
                            LoneEftDmaRadar.Misc.VoiceManager.Play("发现手雷");
                            _alerted.Add(explosive.Addr);
                            break; // Alert once per loop is enough
                        }
                        else if (explosive is Tripwire)
                        {
                            LoneEftDmaRadar.Misc.VoiceManager.Play("000DANGER", true);
                            LoneEftDmaRadar.Misc.VoiceManager.Play("发现阔剑");
                            _alerted.Add(explosive.Addr);
                            break;
                        }
                    }
                }

                // Clean up alerted items that are no longer in explosives list
                _alerted.RemoveWhere(addr => !_explosives.ContainsKey(addr));
            }
        }

        private void GetGrenades(CancellationToken ct)
        {
            try
            {
                var grenades = Memory.ReadPtr(_gameWorld + Offsets.GameWorld.Grenades);
                var grenadesListPtr = Memory.ReadPtr(grenades + 0x18);
                using var grenadesList = UnityList<ulong>.Create(grenadesListPtr, false);
                foreach (var grenade in grenadesList)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        _ = _explosives.GetOrAdd(
                            grenade,
                            addr => new Grenade(addr, _explosives));
                    }
                    catch (Exception ex)
                    {
                        Logging.WriteLine($"Error Processing Grenade @ 0x{grenade.ToString("X")}: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"Grenades Error: {ex}");
            }
        }

        private void GetTripwires(CancellationToken ct)
        {
            try
            {
                var syncObjectsPtr = Memory.ReadPtrChain(_gameWorld, true, _toSyncObjects);
                using var syncObjects = UnityList<ulong>.Create(syncObjectsPtr, true);
                foreach (var syncObject in syncObjects)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        var type = (Enums.SynchronizableObjectType)Memory.ReadValue<int>(syncObject + Offsets.SynchronizableObject.Type);
                        if (type is not Enums.SynchronizableObjectType.Tripwire)
                            continue;
                        _ = _explosives.GetOrAdd(
                            syncObject,
                            addr => new Tripwire(addr));
                    }
                    catch (Exception ex)
                    {
                        Logging.WriteLine($"Error Processing SyncObject @ 0x{syncObject.ToString("X")}: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"Sync Objects Error: {ex}");
            }
        }

        #region IReadOnlyCollection

        public int Count => _explosives.Values.Count;
        public IEnumerator<IExplosiveItem> GetEnumerator() => _explosives.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }
}