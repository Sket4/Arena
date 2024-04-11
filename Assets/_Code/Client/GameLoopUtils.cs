using Arena.ArenaGame;
using Arena.Client.Physics;
using Arena.GameSceneCode;
using Unity.CharacterController;
using TzarGames.GameCore;
using TzarGames.GameCore.Client;
using TzarGames.GameCore.Client.Abilities;
using TzarGames.MultiplayerKit;
using TzarGames.MultiplayerKit.Client;
using Unity.Entities;

namespace Arena.Client
{
    public static class GameLoopUtils
    {
        public static void AddSystems(GameLoopBase gameLoop, bool isBot, bool isLocalGame, ClientGameSettings clientGameSettings)
        {   
            var presentGroup = gameLoop.World.GetExistingSystemManaged<PresentationSystemGroup>();

            //gameLoop.AddGameSystem<SyncHybridTransformSystem>();
            gameLoop.AddGameSystem<TargetSystem>();

            if (isBot == false)
            {
                gameLoop.AddPreSimGameSystem<TouchInputSystem>();
                gameLoop.AddPreSimGameSystem<KeyboardInputSystem>();
                gameLoop.AddPreSimGameSystem<CharacterControlSystem>();
                gameLoop.AddPreSimGameSystem<PlayerCharacterTargetSystem>();
                gameLoop.AddPreSimGameSystem<AutoAimSystem>();

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
                
                gameLoop.AddGameSystem<DropAnimationSystem>();
                gameLoop.AddGameSystem<CharacterItemAppearanceSystem>();

                gameLoop.AddGameSystem<ThirdPersonCameraSystem>(presentGroup);
                gameLoop.AddGameSystem<AlertSystem>();
                gameLoop.AddGameSystem<RagdollAndBreakableSystem>();
                gameLoop.AddGameSystem<CharacterModelSmoothMovementSystem>();
                gameLoop.AddGameSystem<MusicMixerSystem>(presentGroup);
                gameLoop.AddGameSystem<SceneRenderSettingsSystem>();
            }

            if(isLocalGame)
            {
                gameLoop.AddGameSystem<GameSceneSystem>();
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