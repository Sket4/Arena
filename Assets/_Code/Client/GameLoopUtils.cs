using Arena.ArenaGame;
using Arena.Client.Physics;
using Arena.GameSceneCode;
using TzarGames.AnimationFramework;
using Unity.CharacterController;
using TzarGames.GameCore;
using TzarGames.GameCore.Client;
using TzarGames.GameCore.Client.Abilities;
using TzarGames.MultiplayerKit;
using TzarGames.MultiplayerKit.Client;
using TzarGames.Rendering;
using Unity.Entities;

namespace Arena.Client
{
    public static class GameLoopUtils
    {
        public static void AddSystems(GameLoopBase gameLoop, bool isBot, bool isLocalGame, ClientGameSettings clientGameSettings)
        {   
            var presentGroup = gameLoop.World.GetExistingSystemManaged<PresentationSystemGroup>();

            //gameLoop.AddGameSystem<SyncHybridTransformSystem>()

            if (isBot == false)
            {
                gameLoop.AddPreSimGameSystem<TouchInputSystem>();
                gameLoop.AddPreSimGameSystem<KeyboardInputSystem>();
                gameLoop.AddPreSimGameSystem<CharacterControlSystem>();
                gameLoop.AddPreSimGameSystem<PlayerCharacterTargetSystem>();
                gameLoop.AddPreSimGameSystem<AutoAimSystem>();
                gameLoop.AddPreSimGameSystem<ClientCommonGameplaySystem>();

                gameLoop.AddGameSystem<PlaySoundSystem>(presentGroup);
                gameLoop.AddGameSystem<HitSoundSystem>(clientGameSettings.SurfaceSoundsLibrary);
                gameLoop.AddGameSystem<HitEffectSystem>();
                gameLoop.AddGameSystem<InstantiateOnHitSystem>();

                gameLoop.AddGameSystem<UISystem>(presentGroup);
                gameLoop.AddGameSystem<AnimationSystem>();
                gameLoop.AddGameSystem<MaterialRenderingSystem>(presentGroup);
                gameLoop.AddGameSystem<AnimationEventHandlerSystem>();
                gameLoop.AddGameSystem<EffectSpawnSystem>();
                gameLoop.AddGameSystem<LevelUpEffectSpawnSystem>();
                gameLoop.AddGameSystem<CharacterSoundSystem>();

                if (isLocalGame)
                {
                    gameLoop.AddGameSystem<LocalGameItemPickupSoundSystem>();    
                }
                
                gameLoop.AddGameSystem<DropAnimationSystem>(presentGroup);
                gameLoop.AddGameSystem<CharacterAppearanceSystem>();
                gameLoop.AddGameSystemUnmanaged<CharacterAppearanceNativeSystem>();

                gameLoop.AddGameSystem<ThirdPersonCameraSystem>(presentGroup);
                gameLoop.AddGameSystem<AlertSystem>();
                gameLoop.AddGameSystem<RagdollAndBreakableSystem>();
                gameLoop.AddGameSystem<CharacterModelSmoothMovementSystem>();
                gameLoop.AddGameSystem<MusicMixerSystem>(presentGroup);
                gameLoop.AddGameSystem<SceneSettingsSystem>();
                gameLoop.AddGameSystem<AudioSystem>(presentGroup);
            }

            if(isLocalGame)
            {
                gameLoop.AddGameSystem<GameSceneSystem>(gameLoop.World.GetExistingSystemManaged<InitializationSystemGroup>());
                gameLoop.AddGameSystem<ArenaPlayerDataLocalStoreSystem>();
            }

            gameLoop.AddGameSystem<ClientArenaMatchSystem>();
            gameLoop.AddGameSystem<EnemyDetectionSystem>();
            gameLoop.AddGameSystem<DifficultySystem>(isLocalGame);

            if(gameLoop is GameClient client)
            {
                AddClientOnlySystems(client,isBot);
            }
        }
        
        public static void AddSystemsForPlayerPreview(GameLoopBase gameLoop, string systemTag)
        {
            Utils.AddSharedSystems(gameLoop, true, systemTag);

            gameLoop.World.GetExistingSystemManaged<CharacterRotationSystem>().Enabled = false;
            gameLoop.World.GetExistingSystemManaged<CharacterWearingItemRequestSystem>().ValidateWearRequests = false;
            
            gameLoop.AddGameSystem<CharacterAppearanceSystem>();
            gameLoop.AddGameSystemUnmanaged<CharacterAppearanceNativeSystem>();
            gameLoop.AddGameSystem<AnimationSystem>();
            gameLoop.AddGameSystem<CharacterModelSmoothMovementSystem>();
            gameLoop.AddGameSystem<MaterialRenderingSystem>(gameLoop.World.GetExistingSystemManaged<PresentationSystemGroup>());
        }

        public static void AddClientOnlySystems(GameClient gameLoop, bool isBot)
        {
            UnityEngine.Debug.LogError("CharacterNetSyncSystem");
            //gameLoop.AddPreSimGameSystem<CharacterNetSyncSystem>();

            // для обработки ввода и пересылки на сервер
            gameLoop.AddGameSystem<PostMovementNetSyncSystem>();

            // отключаем некоторый функционал на клиенте
            gameLoop.World.GetExistingSystemManaged<ItemPickupSystem>().RunTriggerCheck = false;

            // для аутентификации игрока на сервере
            gameLoop.AddGameSystem<TzarGames.MatchFramework.Client.ClientAuthenticationSystem>(new ClientAuthenticationService());

            // для синхронизации состояния умений
            gameLoop.AddPreSimGameSystem<AbilityStateClientNetSyncSystem>();

            // для синхронизации попадания снарядов
            //AddGameSystem<HitNetSyncClientSystem>();

            // для оповещения сервера о состоянии загруженных сцен
            gameLoop.AddGameSystem<SceneLoaderClientNotificationSystem>();

            // для обработки и применения данных о мире, полученных от сервера
            var commandSystem = gameLoop.World.GetExistingSystemManaged<GameCommandBufferSystem>();

            var dataSyncSystem = gameLoop.AddPreSimGameSystem<ClientDataSyncSystem>(gameLoop.ClientSystem, commandSystem);
            dataSyncSystem.ApplyCommandsAfterUpdate = true;
            Utils.AddDataSyncers(dataSyncSystem);

            // для синхронизации движения объектов
            gameLoop.AddPreSimGameSystem<MovementNetSyncSystem>();

            // для корректировки позиции игрока в случае рассинхронизации с свервером
            gameLoop.AddGameSystem<ClientPositionCorrectionSystem>(systemGroup: gameLoop.World.GetExistingSystemManaged<KinematicCharacterPhysicsUpdateGroup>());

            // собирает команды ввода для дальнейшей пересылки на сервер
            gameLoop.AddPreSimGameSystem<PlayerInputCollectSystem>();

            // для пересылки команд ввода на сервер
            gameLoop.AddGameSystem<PlayerInputSendSystem>();

            // обработка сетевых данных для создания игровых сущностей (игроки, предметы и прочее)
            var processSerializeDataSystemGroup = gameLoop.World.GetExistingSystemManaged<SerializedDataProcessSystemGroup>();
            processSerializeDataSystemGroup.ActivateJobType<NetSyncEntityCreationJob>();
            processSerializeDataSystemGroup.ActivateJobType<NetSyncItemCreationJob>();
            processSerializeDataSystemGroup.ActivateJobType<NetSyncAbilityCreationJob>();

            if(isBot == false)
            {
                // для синхронизации попаданий и их урона
                gameLoop.AddGameSystem<HitSyncSystem>(false);
            }

            // для получения оповещений о подобранных предметах
            gameLoop.AddPreSimGameSystem<ItemPickupNetworkEventSystem>();
            gameLoop.AddGameSystem<ItemPickupEventSoundSystem>();
        }

        public async static System.Threading.Tasks.Task<bool> WaitForResourcesLoad(World world)
        {
            var rs = world.GetExistingSystemManaged<RenderingSystem>();
            int noLoadFrameCounter = 0;

            while (true)
            {
                await System.Threading.Tasks.Task.Yield();

                if (world.IsCreated == false)
                {
                    return false;
                }
                
                if (rs.LoadingMaterialCount > 0 || rs.LoadingMeshCount > 0)
                {
                    noLoadFrameCounter = 0;
                    continue;
                }

                noLoadFrameCounter++;

                if (noLoadFrameCounter >= 5)
                {
                    break;
                }
            }

            return true;
        }
    }

    [DisableAutoCreation]
    [UpdateBefore(typeof(MovementNetSyncSystem))]
    [UpdateInGroup(typeof(PreSimulationSystemGroup), OrderFirst = true)]
    //[UpdateAfter(typeof(AbilityStateClientNetSyncSystem))]
    partial class ClientDataSyncSystem : DataSyncSystem
    {
        public ClientDataSyncSystem(INetworkInfoProvider provider, EntityCommandBufferSystem commandSystem) : base(provider, commandSystem)
        {
        }
    }
}