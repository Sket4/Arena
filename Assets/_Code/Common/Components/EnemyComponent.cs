using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    public struct Enemy : IComponentData
    {
    }

    public class EnemyComponent : ComponentDataBehaviour<Enemy>
    {
    }
}
