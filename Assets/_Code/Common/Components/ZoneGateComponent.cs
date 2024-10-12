using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    public struct ZoneGate : IComponentData
    {
        public ZoneId Zone1;
        public ZoneId Zone2;
    }

    public class ZoneGateComponent : ComponentDataBehaviour<ZoneGate>
    {
    }
}
