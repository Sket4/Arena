using TzarGames.MatchFramework.Server;
using Unity.Entities;

namespace Arena
{
    public struct SessionInitializationData : IComponentData
    {
        public int GameSceneId;
        public int SpawnPointId;
        public bool IsLocalGame;
    }

    public struct MazeSessionInitializationData : IComponentData
    {
        public uint GenerationSeed;
    }

    public class ArenaMazeGameSessionInfo : ArenaGameSessionInfo
    {
        public uint MazeGenerationSeed { get; private set; }

        public ArenaMazeGameSessionInfo(int gameSceneId, int spawnPointId, bool isLocalGame, uint mazeGenerationSeed) : base(gameSceneId, spawnPointId, isLocalGame)
        {
            MazeGenerationSeed = mazeGenerationSeed;
        }

        public override void SetupSessionEntity(EntityManager manager, Entity entity)
        {
            base.SetupSessionEntity(manager, entity);

            manager.AddComponentData(entity, new MazeSessionInitializationData
            {
                GenerationSeed = MazeGenerationSeed
            });
        }
    }
    
    public class ArenaGameSessionInfo : GameSessionInfo
    {
        public int GameSceneId { get; private set; }
        public bool IsLocalGame { get; private set; }
        public int SpawnPointId { get; private set; }

        public ArenaGameSessionInfo(int gameSceneId, int spawnPointId, bool isLocalGame)
        {
            GameSceneId = gameSceneId;
            IsLocalGame = isLocalGame;
            SpawnPointId = spawnPointId;
        }
        
        public override void SetupSessionEntity(EntityManager manager, Entity entity)
        {
            base.SetupSessionEntity(manager, entity);
            manager.AddComponentData(entity, new SessionInitializationData 
            { 
                GameSceneId = GameSceneId,
                IsLocalGame = IsLocalGame,
                SpawnPointId = SpawnPointId
            });
        }
    }
}