using System.Collections;
using Arena.Server;
using TzarGames.Common;
using TzarGames.GameCore;
using TzarGames.GameCore.Client;
using TzarGames.GameCore.ScriptViz;
using TzarGames.MatchFramework;
using TzarGames.MatchFramework.Client;
using TzarGames.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using Console = System.Console;
using Hash128 = Unity.Entities.Hash128;
using RenderPipeline = DGX.SRP.RenderPipeline;

namespace Arena.Client.PreviewRendering
{
    public struct PreviewCamera : IComponentData
    {
        public Vector3 Center;
        public float OrthoSize;
    }
    
    public class PreviewRenderWorldLoop : GameLoopBase
    {
        PresentationSystemGroup presentationSystemGroup;
        private RenderingSystem renderingSystem;
        private SystemHandle previewSystem;
        private Camera previewCamera;
        
        private BaseClientGameLauncher mainGameLauncher;
        
        private bool enableRendering = false;
        private bool isSubscribed = false;
        private Entity mainCharacter;
        public UtilitySystem UtilSystem { get; private set; }

        public bool EnableRendering
        {
            get
            {
                return enableRendering;
            }
            set
            {
                if (enableRendering == value)
                {
                    return;
                }
                enableRendering = value;
                previewCamera.enabled = value;
                renderingSystem.Enabled = value;

                if (value && isSubscribed == false)
                {
                    RenderPipeline.OnBeforeDraw += RenderPipelineOnOnBeforeDraw;
                    isSubscribed = true;
                }
            }
        }
        
        public PreviewRenderWorldLoop(string name, Camera previewCamera, BaseClientGameLauncher mainGameLauncher, Hash128[] additionalScenes) : base(name)
        {
            InitSceneLoading(additionalScenes);

            this.previewCamera = previewCamera;
            this.mainGameLauncher = mainGameLauncher;

            presentationSystemGroup = World.GetOrCreateSystemManaged<PresentationSystemGroup>();
            previewSystem = AddGameSystemUnmanaged<PreviewRenderingSystem>();
            AddGameSystem<PreviewRenderingManagedSystem>();
            AddGameSystem<ScriptVizSystem>();
            
            GameLoopUtils.AddSystemsForPlayerPreview(this, "Preview");

            if (World.GetExistingSystemManaged<UtilitySystem>() == null)
            {
                AddGameSystem<UtilitySystem>();
            }

            UtilSystem = World.GetExistingSystemManaged<UtilitySystem>();

            var cameraEntity = World.EntityManager.CreateEntity(typeof(PreviewCamera));
            World.EntityManager.SetName(cameraEntity, "Preview camera");
            World.EntityManager.AddComponentObject(cameraEntity, this.previewCamera);
            World.EntityManager.AddComponentObject(cameraEntity, this.previewCamera.transform);

            renderingSystem = World.GetExistingSystemManaged<RenderingSystem>();
            renderingSystem.Enabled = false;

            this.previewCamera.enabled = false;
        }

        public void CreateCharacter(CharacterData characterData)
        {
            var utilSystem = World.GetExistingSystemManaged<UtilitySystem>();
            var playerPrefab = utilSystem.GetSingleton<PlayerPrefab>().Value;
            var databaseEntity = utilSystem.GetSingletonEntity<MainDatabaseTag>();
            var database = utilSystem.EntityManager.GetBuffer<IdToEntity>(databaseEntity).ToNativeArray(Allocator.Temp);
            var commands = new EntityCommandBuffer(Allocator.Temp);
            var spawnPositionEntity = utilSystem.GetSingletonEntity<PlayerSpawnPoint>();
            var spawnPos = utilSystem.EntityManager.GetComponentData<LocalToWorld>(spawnPositionEntity);
            var previewPlayerEntuty = utilSystem.EntityManager.CreateEntity();
            utilSystem.EntityManager.SetName(previewPlayerEntuty, "Preview player");
            var dataCopy = new CharacterData(characterData);
            foreach (var bag in dataCopy.ItemsData.Bags)
            {
                foreach (var item in bag.Items)
                {
                    var key = ItemMetaKeys.Activated.ToString();

                    foreach (var boolKeyValue in item.Data.BoolKeyValues)
                    {
                        if (boolKeyValue.Key == key)
                        {
                            boolKeyValue.Value = false;
                        }
                    }
                }
            }
            var createdCharacter = ArenaMatchUtility.CreateCharacter(playerPrefab, previewPlayerEntuty, spawnPos.Position, default, database, dataCopy, commands);
            commands.AddComponent(createdCharacter, new Parent { Value = spawnPositionEntity });
                    
            commands.Playback(utilSystem.EntityManager);

            mainCharacter = utilSystem.GetSingletonEntity<PlayerController>();
            utilSystem.EntityManager.SetComponentData(mainCharacter, LocalTransform.FromPositionRotation(spawnPos.Position, spawnPos.Rotation));
                    
            utilSystem.EntityManager.SetName(mainCharacter, $"Preview character {dataCopy.Name}");
        }
        
        private void RenderPipelineOnOnBeforeDraw(Camera camera, CommandBuffer commandBuffer)
        {
            if (camera != previewCamera)
            {
                if (mainGameLauncher.IsLoadingScenes() || mainGameLauncher.GameLoop == null)
                {
                    return;
                }

                var mainRenderingSystem = mainGameLauncher.GameLoop.World.GetExistingSystemManaged<RenderingSystem>();
                if (mainRenderingSystem == null)
                {
                    return;
                }
                mainRenderingSystem.SetupShaderGlobals(commandBuffer);

                if (EnableRendering == false)
                {
                    if (isSubscribed)
                    {
                        isSubscribed = false;
                        RenderPipeline.OnBeforeDraw -= RenderPipelineOnOnBeforeDraw;
                    }
                }
                return;
            }

            if (EnableRendering)
            {
                renderingSystem.SetupShaderGlobals(commandBuffer);
            }
        }
        
        protected override void OnUpdate()
        {
            base.OnUpdate();
            presentationSystemGroup.Update();
        }

        public override void Dispose()
        {
            base.Dispose();
            if (isSubscribed)
            {
                isSubscribed = false;
                RenderPipeline.OnBeforeDraw -= RenderPipelineOnOnBeforeDraw;
            }
        }
    }
    
    public class PreviewRenderGameWorldLauncher : BaseClientGameLauncher
    {
        public Camera PreviewCameraPrefab;
        public BaseClientGameLauncher MainGameLoopLauncher;
        private Camera previewCamera;
        public RenderTexture PreviewCameraTexture { get; private set; }
        public int CameraTextureSize = 1024;
        public static PreviewRenderGameWorldLauncher Instance { get; private set; }
        private PreviewRenderWorldLoop loop;

        private void Awake()
        {
            Instance = this;
            StartCoroutine(createCharacter());
        }

        IEnumerator createCharacter()
        {
            while (IsLoadingScenes())
            {
                yield return null;
            }

            while (MainGameLoopLauncher.IsLoadingScenes() || MainGameLoopLauncher.GameLoop == null)
            {
                yield return null;
            }

            while (loop.UtilSystem.HasSingleton<PlayerPrefab>() == false)
            {
                yield return null;
            }

            var mainEM = MainGameLoopLauncher.GameLoop.World.EntityManager;
            var playerQuery = mainEM.CreateEntityQuery(ComponentType.ReadOnly<Player>());
            try
            {
                while (true)
                {
                    var players = playerQuery.ToEntityArray(Allocator.Temp);
                    var targetPlayer = Entity.Null;

                    if (players.IsCreated && players.Length > 0)
                    {
                        foreach (var playerEntity in players)
                        {
                            var player = mainEM.GetComponentData<Player>(playerEntity);
                            if (player.ItsMe)
                            {
                                targetPlayer = playerEntity;
                                break;
                            }
                        }

                        if (targetPlayer != Entity.Null && mainEM.HasComponent<PlayerData>(targetPlayer))
                        {
                            var characterData = mainEM.GetComponentData<PlayerData>(targetPlayer).Data as CharacterData;
                            loop.CreateCharacter(characterData);
                            break;
                        }
                    }
                    
                    yield return null;    
                }
            }
            finally
            {
                playerQuery.Dispose();    
            }
        }

        public bool EnableRendering
        {
            get => loop.EnableRendering;
            set
            {
                loop.EnableRendering = value;
            }
        }

        protected override void Update()
        {
            if (IsLoadingScenes() == false && loop.EnableRendering == false)
            {
                return;
            }
            base.Update();
        }

        protected override GameLoopBase CreateGameLoop(Hash128[] additionalScenes)
        {
            previewCamera = Instantiate(PreviewCameraPrefab);
            if (PreviewCameraTexture == false)
            {
                PreviewCameraTexture = RenderTexture.GetTemporary(CameraTextureSize, CameraTextureSize);
                PreviewCameraTexture.antiAliasing = 8;
            }
            previewCamera.targetTexture = PreviewCameraTexture;
            loop = new PreviewRenderWorldLoop("Preview rendering", previewCamera, MainGameLoopLauncher, additionalScenes);
            return loop;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (PreviewCameraTexture != null)
            {
                RenderTexture.ReleaseTemporary(PreviewCameraTexture);
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        [ConsoleCommand]
        public void ShowPreviewItem(int prefabID)
        {
            var requestEntity = GameLoop.World.EntityManager.CreateEntity();
            GameLoop.World.EntityManager.AddComponentData(requestEntity, new CreatePreviewItemRequest
            {
                PrefabID = prefabID,
                Color = new PackedColor(1,1,1)
            });
        }
        
        public void ShowPreviewItemWithColor(int prefabID, PackedColor color)
        {
            var requestEntity = GameLoop.World.EntityManager.CreateEntity();
            GameLoop.World.EntityManager.AddComponentData(requestEntity, new CreatePreviewItemRequest
            {
                PrefabID = prefabID,
                Color = color,
            });
        }

        public void ChangeColor(PackedColor color)
        {
            var requestEntity = GameLoop.World.EntityManager.CreateEntity();
            GameLoop.World.EntityManager.AddComponentData(requestEntity, new ChangeColorRequest
            {
                Color = color
            });
        }
    }
}
