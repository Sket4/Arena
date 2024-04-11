using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    public struct LootTag : IComponentData {}

    public class LootTagComponent : ComponentDataBehaviour<LootTag>
    {
    }
}
