using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    public struct UniqueID : IComponentData
    {
        public long Value;
    }

    public class UniqueIDComponent : ComponentDataBehaviour<UniqueID> {}
}
