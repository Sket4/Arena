using TzarGames.Common;
using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using TzarGames.GameCore.ScriptViz;
using TzarGames.MultiplayerKit;
using Unity.Entities;

namespace Arena
{
    public interface IServerArenaCommands
    {
        [RemoteCall(canBeCalledFromClient: true, canBeCalledByNonOwner: true)]
        void RequestContinueGame(NetMessageInfo info);
        [RemoteCall(canBeCalledFromClient: true, canBeCalledByNonOwner: true)]
        void NotifyExitingFromGame(NetMessageInfo info);
    }
    
    public enum ArenaMatchState
    {
        Preparing,
        Fighting,
        WaitingForNextStep,
        Finished
    }

    // @VARENA
    [Sync]
    public struct ArenaMatchStateData : IComponentData
    {
        public ArenaMatchState State;
        public bool Saved;
        public ushort CurrentStage;

        public bool IsMatchComplete;
        public double MatchEndTime;
        public float DecisionWaitTime;
        public bool IsNextSceneAvailable;
        public bool IsMatchFailed;
    }

    public struct SceneData : IComponentData
    {
        public Entity GameSceneInstance;
    }

    public struct InitialPlayerDataLoad : IComponentData {}

    public static class Utils
    {
        public static void AddDataSyncers(DataSyncSystemBase dataSyncSystem)
        {
            dataSyncSystem.AddDataSync(new PrefabIdDataSync());
            dataSyncSystem.AddDataSync(new ItemCreationDataSync());
            dataSyncSystem.AddDataSync(new ItemOwnerDataSync());
            dataSyncSystem.AddDataSync(new TranslationDataSync());
            dataSyncSystem.AddDataSync(new NetworkPlayerDataSync());
            dataSyncSystem.AddDataSync(new SmoothMovementNetSync());
            dataSyncSystem.AddDataSync(new DeathDataSync());
            dataSyncSystem.AddDataSync(new ScriptVizDataSync());
        }

        public static void AddSharedSystems(GameLoopBase gameLoop, bool isAuthoritative, string abilitySystemTag)
        {
            gameLoop.AddPreSimGameSystem<TimeSystem>();
            gameLoop.AddGameSystem<GameCommandBufferSystem>();

            gameLoop.AddPreSimGameSystem<CharacterAbilityStateSystem>();
            gameLoop.AddPreSimGameSystem<CommonEarlyGameSystem>();

            var abilitySystem = gameLoop.AddPreSimGameSystem<AbilitySystem>(abilitySystemTag);
            abilitySystem.SingleThreadMode = true;

            var itemModSystem = gameLoop.AddPreSimGameSystem<ItemModificatorSystem>();
            itemModSystem.RegisterDefaultModificatorJobs();

            gameLoop.AddPreSimGameSystem<CharacteristicSystem>();

            var itemActivationGroup = gameLoop.AddGameSystem<ItemActivationSystemGroup>();
            {
                gameLoop.AddGameSystem<CharacterWearingItemRequestSystem>(itemActivationGroup);
                gameLoop.AddGameSystem<ActivateItemRequestSystem>(itemActivationGroup);
            }
            
            gameLoop.AddPreSimGameSystemUnmanaged<WritePreviousTranslationSystem>();
            gameLoop.AddPreSimGameSystem<ApplyXpSystem>();
            gameLoop.AddPreSimGameSystem<SimpleMovementSystem>();
            gameLoop.AddPreSimGameSystem<LevelSystem>();
            
            if(isAuthoritative)
            {    
                gameLoop.AddPreSimGameSystem<MovementAISystem>();
                gameLoop.AddPreSimGameSystem<SpawnerSystem>();
                gameLoop.AddPreSimGameSystem<PathMovementSystem>();
                gameLoop.AddPreSimGameSystem<TzarGames.GameCore.RVO.SimulatorSystem>();
            }

            gameLoop.AddGameSystem<SceneLoaderSystem>();
            gameLoop.AddGameSystem<ItemUsageSystem>();
            gameLoop.AddGameSystem<ProcessUsedItemRequestSystem>();
            gameLoop.AddGameSystem<UseItemRequestSystem>();
            gameLoop.AddGameSystem<InventorySystem>();
            var kinematicCharacterGroup = gameLoop.World.GetExistingSystemManaged<Unity.CharacterController.KinematicCharacterPhysicsUpdateGroup>();
            gameLoop.AddGameSystemUnmanaged<TzarGames.GameCore.CharacterController.CharacterControllerPhysicsUpdateSystem>(kinematicCharacterGroup);

            // only for rotation
            //gameLoop.AddGameSystemUnmanaged<TzarGames.GameCore.CharacterController.CharacterControllerVariableUpdateSystem>(kinematicCharacterGroup);

            gameLoop.AddGameSystem<CharacterRotationSystem>();
            gameLoop.AddGameSystem<HitQuerySystem>();
            gameLoop.AddGameSystem<HitFilterSystem>();
            gameLoop.AddGameSystem<HitProcessSystem>();
            gameLoop.AddGameSystem<HitCreatorSystem>();
            gameLoop.AddGameSystem<HitReceiverSystem>();
            gameLoop.AddGameSystem<CriticalDamageSystem>();
            gameLoop.AddGameSystem<DefenseSystem>();
            gameLoop.AddGameSystemUnmanaged<ModifyHealthSystem>();
            gameLoop.AddGameSystem<ItemPickupSystem>();
            gameLoop.AddGameSystem<StatefulTriggerEventSystem>();
            gameLoop.AddGameSystem<TriggerEventSystem>();
            gameLoop.AddGameSystem<ModifyCharacteristicOnTriggerEventSystem>();
            gameLoop.AddGameSystem<StoreSystem>();
            gameLoop.AddGameSystemUnmanaged<WaterStateSystem>();

            var damageSystem = gameLoop.AddGameSystem<ApplyDamageSystem>();
            damageSystem.IsAuthority = isAuthoritative;

            if (isAuthoritative)
            {
                gameLoop.AddGameSystem<AbilityAISystem>();                
                gameLoop.AddGameSystem<FilterCraftRequestSystem>();
                gameLoop.AddGameSystem<FilterPurchaseRequestSystem>();
                gameLoop.AddGameSystem<MainCurrencySystem>();
                gameLoop.AddGameSystem<CraftSystem>();
                gameLoop.AddGameSystem<LootDropSystem>();
                gameLoop.AddGameSystem<XpKillRewardSystem>();
                gameLoop.AddGameSystem<LootObjectSpawnSystem>();
            }

            gameLoop.AddGameSystem<CharacterSystem>();
            gameLoop.AddGameSystem<ScriptVizSystem>();

            gameLoop.AddGameSystem<MainCurrencySystem>();
            gameLoop.AddGameSystem<StunSystem>();
            gameLoop.AddGameSystem<BlockMovementSystem>();
            gameLoop.AddGameSystem<DestroyHitSystem>();
            gameLoop.AddGameSystem<LinkEntityRequestSystem>();
            gameLoop.AddGameSystem<DestroyOnDeadSystem>();
            gameLoop.AddGameSystem<DestroyTimerSystem>();

            gameLoop.AddGameSystem<MessageDispatcherSystem>();
            gameLoop.AddGameSystem<MessageDispatherCleanSystem>();
            gameLoop.AddGameSystem<CommonEventHandlerSystem>();
            gameLoop.AddGameSystem<EventCleanSystem>();
            gameLoop.AddGameSystem<DistanceToGroundSystem>();
        }
    }
}
