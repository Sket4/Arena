using Unity.Entities;

namespace Arena
{
    public struct Shield : IComponentData
    {
    }

    public class ShieldComponent : TzarGames.GameCore.ComponentDataBehaviour<Shield>
    {
    }
}
