using Unity.Entities;

namespace Arena
{
    public struct OneHandedItem : IComponentData
    {
    }

    public class OneHandedItemComponent : TzarGames.GameCore.ComponentDataBehaviour<OneHandedItem>
    {
    }
}
