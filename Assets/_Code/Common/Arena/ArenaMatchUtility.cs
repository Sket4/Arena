using Arena.GameSceneCode;
using System;
using Arena.Maze;
using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using TzarGames.MatchFramework;
using TzarGames.MatchFramework.Server;
using TzarGames.MultiplayerKit;
using TzarGames.MultiplayerKit.Server;
using TzarGames.StateMachineECS;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using SceneLoadingState = Arena.GameSceneCode.SceneLoadingState;

namespace Arena.Server
{
    public static class ArenaMatchUtility
    {
        public static Entity CreateCharacter(Entity playerPrefab, Entity playerOwner, float3 position, quaternion spawnRotation, NativeArray<IdToEntity> database, CharacterData data, EntityCommandBuffer commands)
        {
            var playerCharacter = commands.Instantiate(playerPrefab);

            commands.AddComponent(playerCharacter, new PlayerController { Value = playerOwner });
            commands.SetComponent(playerCharacter, new Group { ID = 1 });
            commands.SetComponent(playerCharacter, LocalTransform.FromPositionRotation(position, spawnRotation));
            commands.SetComponent(playerCharacter, new ViewDirection { Value = math.forward(spawnRotation) });

            ApplyData(playerCharacter, data, commands, database);

            return playerCharacter;
        }

        public static void ApplyData(Entity entity, CharacterData data, EntityCommandBuffer commands, NativeArray<IdToEntity> database)
        {
            commands.SetComponent(entity, Name30.CreateFromStringWithClamp(data.Name));
            commands.SetComponent(entity, new XP() { Value = (uint)data.XP });
            commands.SetComponent(entity, new CharacterClassData() { Value = (CharacterClass)data.Class });
            commands.SetComponent(entity, new UniqueID { Value = data.ID });
            commands.SetComponent(entity, new CharacterHead { ModelID = new PrefabID(data.HeadID) });
            commands.SetComponent(entity, new CharacterHairstyle { ID = new PrefabID(data.HairstyleID) });
            commands.SetComponent(entity, new CharacterSkinColor { Value = new PackedColor(data.SkinColor) });
            commands.SetComponent(entity, new CharacterHairColor { Value = new PackedColor(data.HairColor) });
            commands.SetComponent(entity, new CharacterEyeColor { Value = new PackedColor(data.EyeColor) });
            commands.SetComponent(entity, new Gender(data.Gender));

            commands.AddBuffer<InventoryElement>(entity);
            commands.AddBuffer<AbilityArray>(entity);

            var gameProgressEntity = commands.CreateEntity();
            commands.AppendToBuffer(entity, new LinkedEntityGroup { Value = gameProgressEntity });
            commands.SetComponent(entity, new CharacterGameProgressReference { Value = gameProgressEntity });
            
            var gameProgress = new CharacterGameProgress();
            gameProgress.CurrentStage = data.Progress.CurrentStage;
            gameProgress.CurrentBaseLocationID = data.Progress.CurrentBaseLocation;
            gameProgress.CurrentBaseLocationSpawnPointID = data.Progress.CurrentBaseLocationSpawnPoint;
            commands.AddComponent(gameProgressEntity, gameProgress);
            commands.AddComponent(gameProgressEntity, new Owner(entity));

            var gameProgressFlags = commands.AddBuffer<CharacterGameProgressFlags>(gameProgressEntity);
            
            foreach (var flag in data.Progress.Flags)
            {
                gameProgressFlags.Add(new CharacterGameProgressFlags
                {
                    Value = (ushort)flag
                });
            }

            var gameProgressKeyValues = commands.AddBuffer<CharacterGameProgressKeyValue>(gameProgressEntity);

            foreach (var keyValue in data.Progress.KeyValueStorage)
            {
                gameProgressKeyValues.Add(new CharacterGameProgressKeyValue
                {
                    Key = (ushort)keyValue.Key,
                    Value = keyValue.Value
                });
            }

            var gameProgressQuests = commands.AddBuffer<CharacterGameProgressQuests>(gameProgressEntity);

            foreach (var quest in data.Progress.Quests)
            {
                gameProgressQuests.Add(new CharacterGameProgressQuests { QuestID = (ushort)quest.ID, QuestState = quest.State });
            }
            
            var requestEntity = commands.CreateEntity();
            commands.AddComponent<InventoryTransaction>(requestEntity);
            commands.AddComponent<EventTag>(requestEntity);
            commands.AddComponent<InitialPlayerDataLoad>(requestEntity);
            commands.AddComponent(requestEntity, new Target { Value = entity });
            
            var toAdd = commands.AddBuffer<ItemsToAdd>(requestEntity);

            if (data.ItemsData != null)
            {
                // TODO bag index???
                foreach (var bag in data.ItemsData.Bags)
                {
                    foreach (var item in bag.Items)
                    {
                        var prefab = IdToEntity.GetEntityByID(database, item.TypeID);
                        if (prefab == Entity.Null)
                        {
                            UnityEngine.Debug.LogError($"Null prefab for item with type {item.TypeID}");
                            continue;
                        }
                        var itemEntity = commands.Instantiate(prefab);
                        toAdd.Add(new ItemsToAdd { Item = itemEntity });
                        commands.SetComponent(itemEntity, new UniqueID { Value = item.ID });

                        if (item.Data != null)
                        {
                            if (item.Data.BoolKeyValues.TryGet(ItemMetaKeys.Activated.ToString(), out bool activated))
                            {
                                commands.AddComponent(itemEntity, new ActivatedState(activated));
                            }

                            if (item.Data.IntKeyValues.TryGet(ItemMetaKeys.Color.ToString(), out var color))
                            {
                                commands.AddComponent(itemEntity, new SyncedColor(color));
                            }
                        }
                    }

                    foreach (var item in bag.ConsumableItems)
                    {
                        var prefab = IdToEntity.GetEntityByID(database, item.TypeID);
                        var itemEntity = commands.Instantiate(prefab);

                        toAdd.Add(new ItemsToAdd { Item = itemEntity });

                        commands.AddComponent(itemEntity, new UniqueID { Value = item.ID });
                        commands.SetComponent(itemEntity, new Consumable { Count = (uint)item.Count });
                    }
                }
            }

            PlayerAbilities playerAbilities = default;
            playerAbilities.Reset();
            
            foreach (var ability in data.AbilityData.Abilities)
            {
                Entity abilityPrefab = Entity.Null;
                try
                {
                    abilityPrefab = IdToEntity.GetEntityByID(database, ability.TypeID);
                    var abilityEntity = commands.Instantiate(abilityPrefab);
                    //commands.AddComponent(abilityEntity, new UniqueID { Value = ability.ID });
                    commands.SetComponent(abilityEntity, new AbilityOwner { Value = entity });
                    commands.SetComponent(abilityEntity, new Level { Value = (ushort)ability.Level });
                    //abilities.Add(new AbilityElement { AbilityEntity = abilityEntity });

                    if (ability.TypeID == data.AbilityData.ActiveAbility1)
                    {
                        playerAbilities.Ability1.ID = new AbilityID(ability.TypeID);
                        playerAbilities.Ability1.Ability = abilityEntity;
                    }
                    else if (ability.TypeID == data.AbilityData.ActiveAbility2)
                    {
                        playerAbilities.Ability2.ID = new AbilityID(ability.TypeID);
                        playerAbilities.Ability2.Ability = abilityEntity;
                    }
                    else if (ability.TypeID == data.AbilityData.ActiveAbility3)
                    {
                        playerAbilities.Ability3.ID = new AbilityID(ability.TypeID);
                        playerAbilities.Ability3.Ability = abilityEntity;
                    }
                    else if (ability.TypeID == data.AbilityData.AttackAbility)
                    {
                        playerAbilities.AttackAbility.ID = new AbilityID(ability.TypeID);
                        playerAbilities.AttackAbility.Ability = abilityEntity;
                    }
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"Failed to instantiate ability with id {ability.TypeID} prefab {abilityPrefab}");
                    UnityEngine.Debug.LogException(ex);
                }
            }
            
            commands.SetComponent(entity, playerAbilities);
        }

        public static void SetupGameSceneForGameSessionEntity(StateSystemBase.State state, Entity entity, EntityCommandBuffer Commands)
        {
            var sceneData = state.GetComponent<SessionInitializationData>(entity);
            var dbEntity = state.System.GetSingletonEntity<GameSceneDatabaseTag>();
            var db = state.GetBuffer<IdToEntity>(dbEntity);
            var gameSceneEntity = IdToEntity.GetEntityByID(db, sceneData.GameSceneId);

            if (gameSceneEntity == Entity.Null)
            {
                UnityEngine.Debug.LogError($"Game scene entity is null (targetID: {sceneData.GameSceneId})");

                foreach (var idToEntity in db)
                {
                    Debug.LogError($"db entry: {idToEntity.Entity.Index}, id: {idToEntity.ObjectID}");
                }
                return;
            }

            UnityEngine.Debug.Log($"Setting up game scene {sceneData.GameSceneId} loader");
            var gameSceneInstance = Commands.Instantiate(gameSceneEntity);

            var matchData = new SceneData
            {
                GameSceneInstance = gameSceneInstance
            };
            Commands.SetComponent(gameSceneInstance, new SceneLoadingState { Value = SceneLoadingStates.PendingStartLoading });
            Commands.SetComponent(gameSceneInstance, new SessionEntityReference { Value = entity });

            Commands.AddComponent(entity, matchData);
        }

        public static EntityQuery CreatePlayerSpawnPointsQuery(EntityManager entityManager)
        {
            return entityManager.CreateEntityQuery(ComponentType.ReadOnly<PlayerSpawnPoint>(), ComponentType.ReadOnly<LocalToWorld>(), ComponentType.ReadOnly<SpawnPointIdData>());
        }

        public static void TurnOffNetChannelsForPlayer(StateSystemBase.State state, Entity playerEntity, ref EntityCommandBuffer Commands)
        {
            // выключаем сетевой канал по умолчанию, чтобы объекты с этим каналом не синхронихировались по сети до тех пор, пока загружаются сцены
            DynamicBuffer<PlayerNetworkChannelState> channelStates;
            if (state.HasBuffer<PlayerNetworkChannelState>(playerEntity))
            {
                channelStates = Commands.SetBuffer<PlayerNetworkChannelState>(playerEntity);
            }
            else
            {
                channelStates = Commands.AddBuffer<PlayerNetworkChannelState>(playerEntity);
            }
            channelStates.Add(new PlayerNetworkChannelState { ChannelId = NetworkChannelId.Default, Enabled = false });
            channelStates.Add(new PlayerNetworkChannelState { ChannelId = ArenaNetworkChannelIds.SceneLoading, Enabled = true });
        }

        public static void TurnOffNetChannelsForPlayer(Entity playerEntity, ref EntityCommandBuffer Commands, bool hasChannelStateBuffer)
        {
            // выключаем сетевой канал по умолчанию, чтобы объекты с этим каналом не синхронихировались по сети до тех пор, пока загружаются сцены
            DynamicBuffer<PlayerNetworkChannelState> channelStates;
            if (hasChannelStateBuffer)
            {
                channelStates = Commands.SetBuffer<PlayerNetworkChannelState>(playerEntity);
            }
            else
            {
                channelStates = Commands.AddBuffer<PlayerNetworkChannelState>(playerEntity);
            }
            channelStates.Add(new PlayerNetworkChannelState { ChannelId = NetworkChannelId.Default, Enabled = false });
            channelStates.Add(new PlayerNetworkChannelState { ChannelId = ArenaNetworkChannelIds.SceneLoading, Enabled = true });
        }

        public static bool IsGameSceneLoadedOnPlayer(EntityManager em, Entity matchEntity,
            in DynamicBuffer<PlayerSceneLoadingState> playerSceneStates)
        {
            if (em.HasComponent<SceneData>(matchEntity) == false)
            {
                return false;
            }
            var sceneData = em.GetComponentData<SceneData>(matchEntity);
            
            if (em.Exists(sceneData.GameSceneInstance) == false)
            {
                return false;
            }

            if (playerSceneStates.Length == 0)
            {
                return false;
            }

            var sceneLoaders = em.GetBuffer<SceneAssetInstance>(sceneData.GameSceneInstance);
            
            foreach (var sceneLoader in sceneLoaders)
            {
                if (em.HasComponent<SceneLoadingStateData>(sceneLoader.Value) == false)
                {
                    return false;
                }

                var sceneLoadingState = em.GetComponentData<SceneLoadingStateData>(sceneLoader.Value);

                if (sceneLoadingState.LoadingState != TzarGames.GameCore.SceneLoadingState.Loaded)
                {
                    return false;
                }
                
                var sceneId = em.GetComponentData<PrefabID>(sceneLoader.Value);
                bool containsScene = false;

                foreach (var playerSceneLoadingState in playerSceneStates)
                {
                    if (playerSceneLoadingState.SceneID != sceneId)
                    {
                        continue;
                    }
                    
                    if (playerSceneLoadingState.IsLoaded == false)
                    {
                        return false;
                    }
                    
                    containsScene = true;
                    break;
                }

                if (containsScene == false)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsGameSceneLoaded(StateSystemBase.State state, Entity matchEntity)
        {
            if (state.HasComponent<SceneData>(matchEntity) == false)
            {
                return false;
            }

            var matchData = state.GetComponent<SceneData>(matchEntity);

            if (state.Exists(matchData.GameSceneInstance) == false)
            {
                return false;
            }

            //if(state.HasComponent<SceneLoadingState>(matchData.GameSceneInstance) == false)
            //{
            //    return false;
            //}

            var sceneLoadingState = state.GetComponent<SceneLoadingState>(matchData.GameSceneInstance);

            if (sceneLoadingState.Value != SceneLoadingStates.Running)
            {
                return false;
            }

            return true;
        }

        public static void EnableDefaultChannelsForPlayer(EntityManager em, Entity player)
        {
            // включаем сетевой канал по умолчанию, так как все сцены уже загружены
            var channels = em.GetBuffer<PlayerNetworkChannelState>(player);
            for (var channelIndex = 0; channelIndex < channels.Length; channelIndex++)
            {
                var channel = channels[channelIndex];
                if (channel.ChannelId.Value == NetworkChannelId.Default.Value)
                {
                    channel.Enabled = true;
                    channels[channelIndex] = channel;
                }
            }
        }

        public static CharacterData SetupPlayerCharacter(StateSystemBase.State state, bool isLocalGame, Entity characterPrefab, float3 spawnPosition, quaternion spawnRotation, Entity player, TzarGames.MultiplayerKit.NetworkPlayer netPlayer, ref EntityCommandBuffer Commands)
        {
            if(isLocalGame == false)
            {
                EnableDefaultChannelsForPlayer(state.System.EntityManager, player);
            }

            if (state.HasComponent<Player>(player) == false)
            {
                Commands.AddComponent(player, new Player(netPlayer.ID, isLocalGame, true));
            }
            if (state.HasComponent<NetworkID>(player) == false)
            {
                Commands.AddComponent(player, new NetworkID());
            }

            var databaseEntity = state.System.GetSingletonEntity<MainDatabaseTag>();
            var databaseBuffer = state.GetBuffer<IdToEntity>(databaseEntity);

            var playerData = state.GetComponentObject<PlayerData>(player);
            var characterData = playerData.Data as CharacterData;

            using (var database = databaseBuffer.ToNativeArray(Allocator.Temp))
            {
                var characterEntity = CreateCharacter(characterPrefab, player, spawnPosition, spawnRotation, database, characterData, Commands);

                if (state.HasComponent<ControlledCharacter>(player))
                {
                    Commands.SetComponent(player, new ControlledCharacter { Entity = characterEntity });
                }
                else
                {
                    Commands.AddComponent(player, new ControlledCharacter { Entity = characterEntity });
                }
            }

            return characterData;
        }
    }
}
