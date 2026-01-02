using LoneEftDmaRadar.DMA;
using LoneEftDmaRadar.Tarkov.Unity;
using LoneEftDmaRadar.Tarkov.Unity.Structures;
using SDK;
using System.Runtime.CompilerServices;

namespace LoneEftDmaRadar.Tarkov.Features
{
    public static class AntiAfk
    {
        private static ulong _afkMonitorPtr = 0;
        private static ulong _lastGom = 0;
        private const float AFK_DELAY = 604800f; // 1 week

        public static void Update()
        {
            if (!Program.Config.Misc.AntiAfk)
                return;

            try
            {
                if (Memory.GOM == 0) return;

                // Reset cache if GOM changed (new game instance)
                if (Memory.GOM != _lastGom)
                {
                    _afkMonitorPtr = 0;
                    _lastGom = Memory.GOM;
                }

                if (_afkMonitorPtr == 0)
                {
                    FindAfkMonitor();
                }

                if (_afkMonitorPtr != 0)
                {
                    // Check current value to avoid unnecessary writes
                    float currentDelay = Memory.ReadValue<float>(_afkMonitorPtr + Offsets.AfkMonitor.Delay);
                    if (Math.Abs(currentDelay - AFK_DELAY) > 1f)
                    {
                        Memory.WriteValue(_afkMonitorPtr + Offsets.AfkMonitor.Delay, AFK_DELAY);
                    }
                }
            }
            catch { }
        }

        private static void FindAfkMonitor()
        {
            // 1. Find "Application" GameObject
            ulong applicationGO = FindGameObject("Application");
            if (applicationGO == 0) return;

            // 2. Find "TarkovApplication" Component
            ulong tarkovAppPtr = FindComponent(applicationGO, "TarkovApplication");
            if (tarkovAppPtr == 0) return;

            // 3. Get MenuOperation -> AfkMonitor
            ulong menuOp = Memory.ReadPtr(tarkovAppPtr + Offsets.TarkovApplication.MenuOperation);
            if (menuOp == 0) return;

            ulong afkMonitor = Memory.ReadPtr(menuOp + Offsets.MenuOperation.AfkMonitor);
            if (afkMonitor != 0)
            {
                _afkMonitorPtr = afkMonitor;
            }
        }

        private static ulong FindGameObject(string name)
        {
            var gom = Memory.ReadValue<GameObjectManager>(Memory.GOM);
            ulong activeNode = gom.ActiveNodes;
            
            int maxIterations = 5000; 
            
            while (activeNode != 0 && maxIterations-- > 0)
            {
                var node = Memory.ReadValue<LinkedListObject>(activeNode);
                ulong gameObjectPtr = node.ThisObject;
                
                if (gameObjectPtr != 0)
                {
                    ulong namePtr = Memory.ReadPtr(gameObjectPtr + UnitySDK.UnityOffsets.GameObject_NameOffset);
                    string goName = Memory.ReadUtf8String(namePtr, 64);
                    
                    if (string.Equals(goName, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return gameObjectPtr;
                    }
                }
                
                if (node.NextObjectLink == activeNode || node.NextObjectLink == 0) break;
                activeNode = node.NextObjectLink;
            }
            return 0;
        }

        private static ulong FindComponent(ulong gameObjectPtr, string componentName)
        {
            ulong componentsPtr = Memory.ReadPtr(gameObjectPtr + UnitySDK.UnityOffsets.GameObject_ComponentsOffset);
            var componentArr = Memory.ReadValue<DynamicArray>(componentsPtr);
            
            int size = (int)componentArr.Size;
            if (size > 100) size = 100; // Limit check

            if (size <= 0) return 0;

            using var compsBuf = Memory.ReadPooled<DynamicArray.Entry>(componentArr.FirstValue, size);
            
            foreach (var comp in compsBuf.Memory.Span)
            {
                ulong compPtr = comp.Component;
                if (compPtr == 0) continue;

                ulong objectClassPtr = Memory.ReadPtr(compPtr + UnitySDK.UnityOffsets.Component_ObjectClassOffset);
                if (objectClassPtr == 0) continue;

                string name = ObjectClass.ReadName(objectClassPtr);
                if (string.Equals(name, componentName, StringComparison.OrdinalIgnoreCase))
                {
                    return objectClassPtr;
                }
            }
            return 0;
        }
    }
}
