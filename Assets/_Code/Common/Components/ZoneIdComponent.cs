using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    public struct ZoneId : IComponentData
    {
        public ushort Value;

        public ZoneId(ushort val)
        {
            Value = val;
        }
    }

    public class ZoneIdComponent : ComponentDataBehaviour<ZoneId>
    {
    }
}
