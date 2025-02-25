﻿using Arena.Quests;
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
        public byte MazeSize;
    }
    
    public class ArenaGameSessionInfo : GameSessionInfo
    {
        public int GameSceneId { get; private set; }
        public bool IsLocalGame { get; private set; }
        public int SpawnPointId { get; private set; }
        public GameParameter[] Parameters { get; private set; }

        public ArenaGameSessionInfo(int gameSceneId, int spawnPointId, bool isLocalGame, GameParameter[] parameters)
        {
            GameSceneId = gameSceneId;
            IsLocalGame = isLocalGame;
            SpawnPointId = spawnPointId;
            Parameters = parameters;
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
            
            var parameters = manager.AddBuffer<GameParameter>(entity);
            if (Parameters != null)
            {
                foreach (var parameter in Parameters)
                {
                    parameters.Add(parameter);    
                }
            }
        }
    }
}