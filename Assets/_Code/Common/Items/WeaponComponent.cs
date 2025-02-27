using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    public struct Weapon : IComponentData
    {
    }

    public class WeaponComponent : ComponentDataBehaviour<Weapon>
    {
    }
}