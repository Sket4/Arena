using TzarGames.Common;
using TzarGames.GameCore;
using TzarGames.GameCore.ScriptViz;
using TzarGames.MatchFramework.Client;
using TzarGames.Rendering;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;
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
            AddGameSystem<GameCommandBufferSystem>();

            var cameraEntity = World.EntityManager.CreateEntity(typeof(PreviewCamera));
            World.EntityManager.SetName(cameraEntity, "Preview camera");
            World.EntityManager.AddComponentObject(cameraEntity, this.previewCamera);
            World.EntityManager.AddComponentObject(cameraEntity, this.previewCamera.transform);

            renderingSystem = World.GetExistingSystemManaged<RenderingSystem>();

            this.previewCamera.enabled = false;
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
            if (loop.EnableRendering == false)
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
                PrefabID = prefabID
            });
        }
    }
}
