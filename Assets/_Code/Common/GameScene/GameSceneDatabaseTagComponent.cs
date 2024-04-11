using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    public struct GameSceneDatabaseTag : IComponentData
    {
    }

    public class GameSceneDatabaseTagComponent : ComponentDataBehaviour<GameSceneDatabaseTag>
    {
    }
}