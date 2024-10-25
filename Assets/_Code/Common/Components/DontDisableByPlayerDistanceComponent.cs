using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    [TemporaryBakingType]
    public struct DontDisableByPlayerDistance : IComponentData
    {
    }

    public class DontDisableByPlayerDistanceComponent : ComponentDataBehaviour<DontDisableByPlayerDistance>
    {
    }
}
