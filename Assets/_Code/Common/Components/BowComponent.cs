using Unity.Entities;

namespace Arena
{
    public struct Bow : IComponentData
    {
    }

    public class BowComponent : TzarGames.GameCore.ComponentDataBehaviour<Bow>
    {
    }
}
