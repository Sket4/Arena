using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;
using Object = UnityEngine.Object;

namespace DGX.SRP
{
    public class RenderPipeline : UnityEngine.Rendering.RenderPipeline
    {
        public RenderPipelineAsset Asset { get; }
        
        private readonly RenderGraph renderGraph = new("DGX render graph");

        Material LightingPassMaterial;
        Material LinearizeDepthMaterial;
        private static Mesh fullscreenTriangle;
        private static Mesh fullscreenQuad;
        private bool isOpenGL;
        public ReflectionProbeManager ReflectionProbeManager;
        static readonly ShaderTagId srpDefaultUnlitShaderTag = new("SRPDefaultUnlit");
        static readonly ShaderTagId dgxForwardShaderTag = new("DGXForward");
        private const string PBR_RENDERING_ENABLED = "DGX_PBR_RENDERING";
        private const string SHADOWS_ENABLED = "DGX_SHADOWS_ENABLED";
        private const string DARK_MODE = "DGX_DARK_MODE";
        private const string SPOT_LIGHTS = "DGX_SPOT_LIGHTS";
        
        private LightsRenderer lightRenderer = new();
        private static readonly Rect fullRect = new Rect(0, 0, 1, 1);
        private static bool enableShadows_global = true;
        private static bool enableLights_global = true;
        public static bool IsLightsEnabled => enableLights_global;

        private DgxRenderPipelineGlobalSettings m_GlobalSettings;
        
        public override RenderPipelineGlobalSettings defaultSettings => m_GlobalSettings;
        public static event Action<Camera, CommandBuffer> OnBeforeDraw;
        public static event Action<Camera, CommandBuffer> OnAfterDraw;
        public bool IsValid { get; private set; }

        class CameraData
        {
            public string TargetCameraName;
            public Camera TargetCamera;
            public CameraRenderSettingsData RenderSettings;
            public float LastUpdateTime = -1000;
            public uint RenderFrameCounter;
            public RenderTexture GBuffer0;
            public RenderTargetIdentifier GBuffer0TargetId;
            public RenderTexture GBuffer1;
            public RenderTargetIdentifier GBuffer1TargetId;
            public RenderTexture GBuffer2;
            public RenderTargetIdentifier GBuffer2TargetId;
            public RenderTargetIdentifier[] GBufferIDs = new RenderTargetIdentifier[3];
            public RenderTexture Depth;
            public RenderTargetIdentifier Depth_ID;
            public RenderTexture LinearDepth;
            public RenderTargetIdentifier LinearDepth_ID;
            public RenderTexture FinalBlit;
            public RenderTargetIdentifier FinalBlit_ID;

            public bool isHDR;

            public void Release()
            {
                if(GBuffer0) RenderTexture.ReleaseTemporary(GBuffer0);
                if(GBuffer1) RenderTexture.ReleaseTemporary(GBuffer1);
                if(GBuffer2) RenderTexture.ReleaseTemporary(GBuffer2);
                if(Depth) RenderTexture.ReleaseTemporary(Depth);
                if(LinearDepth) RenderTexture.ReleaseTemporary(LinearDepth);
                if(FinalBlit) RenderTexture.ReleaseTemporary(FinalBlit);
            }
        }

        private List<CameraData> cameraRenderTextures = new();
        
        public RenderPipeline(RenderPipelineAsset asset)
        {
            GraphicsSettings.useScriptableRenderPipelineBatching = true;
            m_GlobalSettings = DgxRenderPipelineGlobalSettings.instance;
                
            Asset = asset;
            checkResources();

            ReflectionProbeManager = ReflectionProbeManager.Create();
            
            isOpenGL = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore
                || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2
                || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2;

            IsValid = true;
        }

        public static void EnablePBR(bool enable)
        {
            if(enable)
                Shader.EnableKeyword(PBR_RENDERING_ENABLED);
            else
            {
                Shader.DisableKeyword(PBR_RENDERING_ENABLED);
            }
        }
        
        public static void EnableDarkMode(bool enable)
        {
            if(enable)
                Shader.EnableKeyword(DARK_MODE);
            else
            {
                Shader.DisableKeyword(DARK_MODE);
            }
        }
        
        public static void EnableShadows(bool enable)
        {
            enableShadows_global = enable;
            
            if(enable)
                Shader.EnableKeyword(SHADOWS_ENABLED);
            else
            {
                Shader.DisableKeyword(SHADOWS_ENABLED);
            }
        }
        
        public static void EnableLights(bool enable)
        {
            enableLights_global = enable;
        }

        void checkResources()
        {
            if (LightingPassMaterial == false)
            {
                if (Asset.LightingPassShader)
                {
                    LightingPassMaterial = new Material(Asset.LightingPassShader);
                }
                else
                {
                    LightingPassMaterial = new Material(Shader.Find("Hidden/DGX/DEFERRED"));
                }
            }

            if (LinearizeDepthMaterial == false)
            {
                if (Asset.LinearizeDepthShader)
                {
                    LinearizeDepthMaterial = new Material(Asset.LinearizeDepthShader);
                }
                else
                {
                    LinearizeDepthMaterial = new Material(Shader.Find("Hidden/DGX/LINEARIZE_DEPTH"));
                }
            }

            if(fullscreenTriangle == false)
                fullscreenTriangle = CreateFullscreenTriangle();
            if (fullscreenQuad == false)
                fullscreenQuad = CreateFullscreenQuad();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            IsValid = false;
            
            renderGraph.Cleanup();

            foreach (var cameraRT in cameraRenderTextures)
            {
                cameraRT.Release();    
            }
            
            ReflectionProbeManager.Dispose();
            
            for(int i=0; i<2; i++) 
                Shader.SetGlobalTexture("_GT"+i, null);

            if (LightingPassMaterial)
            {
                destroyObject(ref LightingPassMaterial);
            }

            if (LinearizeDepthMaterial)
            {
                destroyObject(ref LinearizeDepthMaterial);
            }
            
            destroyObject(ref fullscreenTriangle);
            destroyObject(ref fullscreenQuad);
        }

        static void destroyObject<T>(ref T obj) where T : UnityEngine.Object
        {
            if (obj == false)
            {
                obj = null;
                return;
            }
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
            obj = null;
        }
        
        static Mesh CreateFullscreenTriangle()
        {
            // TODO reorder for pre&post-transform cache optimisation.
            // Simple full-screen triangle
            Vector3[] positions =
            {
                new Vector3(0,  2.0f, 0.0f),
                new Vector3(0, -2.0f, 0.0f),
                new Vector3(4.0f,  2.0f, 0.0f) 
            };

            int[] indices = { 0, 1, 2 };

            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt16;
            mesh.vertices = positions;
            mesh.triangles = indices;

            return mesh;
        }
        
        static Mesh CreateFullscreenQuad()
        {
            // TODO reorder for pre&post-transform cache optimisation.
            Vector3[] positions =
            {
                new Vector3(0,  0.0f, 0.0f),
                new Vector3(0, 2.0f, 0.0f),
                new Vector3(2.0f,  2.0f, 0.0f),
                new Vector3(2.0f,  0.0f, 0.0f),
            };

            int[] indices = { 0, 1, 2, 2, 3, 0 };

            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt16;
            mesh.vertices = positions;
            mesh.triangles = indices;

            return mesh;
        }
        
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            checkResources();
            
            for (var i = cameraRenderTextures.Count - 1; i >= 0; i--)
            {
                var camRT = cameraRenderTextures[i];
                if (camRT.TargetCamera == false)
                {
                    camRT.Release();
                    cameraRenderTextures.RemoveAt(i);
                }
            }

            var currentTime = Time.realtimeSinceStartup;

            foreach (Camera camera in cameras)
            {
                var renderGraphParameters = new RenderGraphParameters
                {
                    commandBuffer = CommandBufferPool.Get(),
                    currentFrameIndex = Time.frameCount,
                    executionName = "Render camera",
                    scriptableRenderContext = context
                };
                renderGraph.BeginRecording(renderGraphParameters);
                {
                    RenderCamera(context, camera, currentTime);    
                }
                renderGraph.EndRecordingAndExecute();
                
                context.ExecuteCommandBuffer(renderGraphParameters.commandBuffer);
                context.Submit();
                CommandBufferPool.Release(renderGraphParameters.commandBuffer);
            }
            
            renderGraph.EndFrame();
        }

        private void RenderCamera(ScriptableRenderContext context, Camera camera, float currentTime)
        {
            var rt = getCameraRenderTextures(camera);

            switch (rt.RenderSettings.IntervalMode)
            {
                case CameraRenderIntervalMode.None:
                    break;
                case CameraRenderIntervalMode.Time:
                {
                    var interval = rt.RenderSettings.RenderTimeInverval;
                    if (interval > 0 && (currentTime - rt.LastUpdateTime < interval))
                    {
                        return;
                    }
                }
                    break;
                case CameraRenderIntervalMode.Frame:
                {
                    var interval = rt.RenderSettings.RenderFrameInverval;
                    rt.RenderFrameCounter++;

                    if (rt.RenderFrameCounter > interval)
                    {
                        rt.RenderFrameCounter = 0;
                    }
                    else
                    {
                        return;
                    }
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            rt.LastUpdateTime = currentTime;

#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
#endif

            // Get the culling parameters from the current Camera
            if (camera.TryGetCullingParameters(out var cullingParameters) == false)
            {
                return;
            }

            cullingParameters.maximumVisibleLights = int.MaxValue;
            cullingParameters.shadowDistance = Mathf.Min(Asset.ShadowSettings.MaxDistance, camera.farClipPlane);

            // Use the culling parameters to perform a cull operation, and store the results
            var cullingResults = context.Cull(ref cullingParameters);

            ReflectionProbeManagerPass.Record(renderGraph, rt.TargetCamera, ReflectionProbeManager, ref cullingResults);

            
            var shouldRenderShadows = enableShadows_global && rt.RenderSettings.RenderShadows;

            if (enableLights_global)
            {
                LightRenderPass.Record(renderGraph, context, rt.TargetCamera, lightRenderer, Asset.ShadowSettings, shouldRenderShadows, ref cullingResults);
            }

            var depthTextureID = rt.Depth_ID;


            var clearFlags = camera.clearFlags;
            bool shouldClearColor = clearFlags == CameraClearFlags.Color;

#if UNITY_EDITOR
            if (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.SceneView)
            {
                shouldClearColor = true;
            }
#endif

            // Tell Unity how to filter the culling results, to further specify which geometry to draw
            // Use FilteringSettings.defaultValue to specify no filtering

            RenderTargetIdentifier colorTextureID;

            var useFinalBlit = rt.FinalBlit == true;

            if (useFinalBlit)
            {
                colorTextureID = rt.FinalBlit_ID;
            }
            else
            {
                colorTextureID = BuiltinRenderTextureType.CameraTarget;
            }
            
            SetupPass.Record(renderGraph, camera, depthTextureID, rt);

            if (rt.RenderSettings.SkipDeferredPass == false)
            {
                GBufferPass.Record(renderGraph, camera, rt, depthTextureID, in cullingResults);
                
                DeferredLightingPass.Record(
                    renderGraph, 
                    camera, 
                    rt,
                    colorTextureID, 
                    LightingPassMaterial,
                    lightRenderer.VisibleSpotLights.Count > 0, 
                    shouldClearColor,
                    useFinalBlit);
            }
            else
            {
                ClearPass.Record(renderGraph, camera, colorTextureID, depthTextureID, shouldClearColor);
            }

            // FORWARD UNLIT
            GeometryPass.Record(renderGraph, "Forward unlit", camera, 
                colorTextureID, depthTextureID, in cullingResults, true, false);

            // SKYBOX
            SkyBoxPass.Record(renderGraph, camera, colorTextureID, depthTextureID);

            // FORWARD TRANSPARENTS
            GeometryPass.Record(renderGraph, "Forward transparent", camera, 
                colorTextureID, depthTextureID, in cullingResults, false, true);

            if (useFinalBlit)
            {
                FinalBlitPass.Record(renderGraph, rt.TargetCamera, rt.FinalBlit_ID);
            }

            GizmosPass.Record(renderGraph, rt.TargetCamera);

            CleanupPass.Record(renderGraph, camera, lightRenderer);
        }

        abstract class BasePass
        {
            protected Camera Camera;
            
            public abstract void Render(RenderGraphContext context);
        }

        class SetupPass : BasePass
        {
            private CameraData cameraData;
            private RenderTargetIdentifier depthTextureID;
            
            public override void Render(RenderGraphContext context)
            {
                context.renderContext.SetupCameraProperties(Camera);

                if (cameraData.LinearDepth)
                {
                    context.cmd.SetGlobalTexture("_LinearDepth", cameraData.LinearDepth_ID);
                }

                context.cmd.SetGlobalTexture("_Depth", depthTextureID);

                OnBeforeDraw?.Invoke(Camera, context.cmd);
            }
            
            public static void Record(
                RenderGraph renderGraph,
                Camera camera,
                RenderTargetIdentifier depthTextureID,
                CameraData cameraData)
            {
                using RenderGraphBuilder builder =
                    renderGraph.AddRenderPass("Setup", out SetupPass newPass);
                newPass.Camera = camera;
                newPass.depthTextureID = depthTextureID;
                newPass.cameraData = cameraData;
                builder.SetRenderFunc<SetupPass>((pass, context) => pass.Render(context));
            }
        }

        class ClearPass : BasePass
        {
            RenderTargetIdentifier colorTextureID;
            RenderTargetIdentifier depthTextureID;
            private bool clearColor;
            
            public override void Render(RenderGraphContext context)
            {
                context.cmd.SetRenderTarget(colorTextureID, depthTextureID);
                context.cmd.ClearRenderTarget(true,
                    clearColor,
                    Camera.backgroundColor);

                context.renderContext.ExecuteCommandBuffer(context.cmd);
                context.cmd.Clear();
            }
            
            public static void Record(
                RenderGraph renderGraph,
                Camera camera,
                RenderTargetIdentifier colorTextureID,
                RenderTargetIdentifier depthTextureID,
                bool clearColor)
            {
                using RenderGraphBuilder builder =
                    renderGraph.AddRenderPass("Clear", out ClearPass newPass);
                newPass.Camera = camera;
                newPass.colorTextureID = colorTextureID;
                newPass.depthTextureID = depthTextureID;
                newPass.clearColor = clearColor;
                builder.SetRenderFunc<ClearPass>((pass, context) => pass.Render(context));
            }
        }

        class GBufferPass : BasePass
        {
            RenderTargetIdentifier depthTextureID;
            private CullingResults cullingResults;
            private CameraData cameraData;
            ShaderTagId shaderTagId = new("gbuffer");
            
            public override void Render(RenderGraphContext context)
            {
                // GBUFFER
                context.cmd.SetGlobalTexture("_GT0", cameraData.GBuffer0);
                context.cmd.SetGlobalTexture("_GT1", cameraData.GBuffer1);
                context.cmd.SetGlobalTexture("_GT2", cameraData.GBuffer2);

                //RenderTargetIdentifier depthTextureID = rt.Depth_ID;
                context.cmd.SetRenderTarget(cameraData.GBufferIDs, depthTextureID);

#if UNITY_EDITOR
                if (Camera.cameraType == CameraType.Preview || Camera.cameraType == CameraType.SceneView)
                {
                    context.cmd.ClearRenderTarget(true, true, Camera.backgroundColor);
                }
                else
                {
                    context.cmd.ClearRenderTarget(true, false, Camera.backgroundColor);
                }
#else
                context.cmd.ClearRenderTarget(true, false, Camera.backgroundColor);
#endif
                
                // Schedule a command to draw the geometry, based on the settings you have defined
                var rendererList = context.renderContext.CreateRendererList(new RendererListDesc(shaderTagId, cullingResults, Camera)
                {
                    sortingCriteria = SortingCriteria.CommonOpaque,
                    rendererConfiguration = PerObjectData.Lightmaps | PerObjectData.LightProbe,
                    renderQueueRange = RenderQueueRange.opaque,
                });
                context.cmd.DrawRendererList(rendererList);
                
                context.renderContext.ExecuteCommandBuffer(context.cmd);
                context.cmd.Clear();
                
                // LINEAR DEPTH
                // cmd = new CommandBuffer();
                // cmd.name = "Linearize depth";
                // cmd.SetGlobalVector("_BlitScaleBias", new Vector4(1,1,0,0));
                // cmd.Blit(rt.Depth, rt.LinearDepth, LinearizeDepthMaterial);
                // context.ExecuteCommandBuffer(cmd);
                // cmd.Release();
                
                
            }
            
            public static void Record(
                RenderGraph renderGraph,
                Camera camera,
                CameraData cameraData,
                RenderTargetIdentifier depthTextureID,
                in CullingResults cullingResults)
            {
                using RenderGraphBuilder builder =
                    renderGraph.AddRenderPass("GBuffer", out GBufferPass newPass);
                newPass.Camera = camera;
                newPass.cameraData = cameraData;
                newPass.depthTextureID = depthTextureID;
                newPass.cullingResults = cullingResults;
                builder.SetRenderFunc<GBufferPass>((pass, context) => pass.Render(context));
            }
        }
        
        class DeferredLightingPass : BasePass
        {
            RenderTargetIdentifier colorTextureID;
            private CameraData cameraData;
            private bool hasVisibleSpotLights;
            private bool clearColor;
            private Material lightingPassMaterial;
            private bool useDedicatedColorTarget;

            void SetupMatrixConstants(CommandBuffer cmd)
            {
                Matrix4x4 proj = Camera.projectionMatrix;
                Matrix4x4 view = Camera.worldToCameraMatrix;
                Matrix4x4 gpuProj = GL.GetGPUProjectionMatrix(proj, false);
                var vp = gpuProj * view;
                cmd.SetGlobalMatrix("unity_MatrixInvVP", Matrix4x4.Inverse(vp));
            
                // var frustumCorners = new Vector3[4];
                // camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
                // var cornerMatrix = new Matrix4x4();
                //
                // for (int i = 0; i < 4; i++)
                // {
                //     var worldSpaceCorner = camera.transform.TransformVector(frustumCorners[i]);
                //     //Debug.DrawRay(camera.transform.position, worldSpaceCorner, Color.blue);
                //     cornerMatrix.SetRow(i, worldSpaceCorner);
                // }
                // cmd.SetGlobalMatrix("_WorldSpaceFrustumCorners", cornerMatrix);
            }
            
            public override void Render(RenderGraphContext context)
            {
                // DEFERRED LIGHTING
                SetupMatrixConstants(context.cmd);

                context.cmd.SetRenderTarget(colorTextureID, cameraData.GBuffer0TargetId);
                if (clearColor)
                {
                    context.cmd.ClearRenderTarget(false, true, Camera.backgroundColor);
                }

                if (hasVisibleSpotLights)
                {
                    context.cmd.EnableShaderKeyword(SPOT_LIGHTS);
                }
                else
                {
                    context.cmd.DisableShaderKeyword(SPOT_LIGHTS);
                }

                int deferredPass;
                bool isFogEnabled = RenderSettings.fog && Camera.orthographic == false;

                if (isFogEnabled)
                {
                    deferredPass = 0;
                }
                else
                {
                    deferredPass = 1;
                }
                
                var cameraRect = Camera.rect;
                Mesh fullscreenMesh;

                if (useDedicatedColorTarget)
                {
                    context.cmd.SetGlobalVector("_DrawScale", new Vector4(1, 1, 0, 0));
                    fullscreenMesh = fullscreenTriangle;
                }
                else
                {
                    var drawScale = new Vector4(cameraRect.width, cameraRect.height, 0, 0);
                    context.cmd.SetGlobalVector("_DrawScale", drawScale);
                    bool isFullRect = cameraRect == fullRect;
                    fullscreenMesh = isFullRect ? fullscreenTriangle : fullscreenQuad;
                }

                context.cmd.DrawMesh(fullscreenMesh, Matrix4x4.identity, lightingPassMaterial, 0, deferredPass);
                context.renderContext.ExecuteCommandBuffer(context.cmd);
                context.cmd.Clear();
            }
            
            public static void Record(
                RenderGraph renderGraph,
                Camera camera,
                CameraData cameraData,
                RenderTargetIdentifier colorTextureID,
                Material lightingPassMaterial,
                bool hasVisibleSpotlights,
                bool clearColor,
                bool useDedicatedColorTarget)
            {
                using RenderGraphBuilder builder =
                    renderGraph.AddRenderPass("Deferred lighting", out DeferredLightingPass newPass);
                newPass.Camera = camera;
                newPass.cameraData = cameraData;
                newPass.clearColor = clearColor;
                newPass.colorTextureID = colorTextureID;
                newPass.hasVisibleSpotLights = hasVisibleSpotlights;
                newPass.lightingPassMaterial = lightingPassMaterial;
                newPass.useDedicatedColorTarget = useDedicatedColorTarget;
                builder.SetRenderFunc<DeferredLightingPass>((pass, context) => pass.Render(context));
            }
        }

        class GeometryPass : BasePass
        {
            RenderTargetIdentifier colorTextureID;
            RenderTargetIdentifier depthTextureID;

            private RendererListHandle rendererList;

            private static readonly ShaderTagId[] shaderTagIds =
            {
                srpDefaultUnlitShaderTag,
                dgxForwardShaderTag
            };
            
            public override void Render(RenderGraphContext context)
            {
                context.cmd.SetRenderTarget(colorTextureID, depthTextureID);
                context.cmd.DrawRendererList(rendererList);
                
                context.renderContext.ExecuteCommandBuffer(context.cmd);
                context.cmd.Clear();
            }
            
            public static void Record(
                RenderGraph renderGraph,
                string passName,
                Camera camera,
                RenderTargetIdentifier colorTextureID,
                RenderTargetIdentifier depthTextureID,
                in CullingResults cullingResults,
                bool opaque,
                bool lighting)
            {
                using RenderGraphBuilder builder =
                    renderGraph.AddRenderPass(passName, out GeometryPass newPass);
                newPass.Camera = camera;
                newPass.colorTextureID = colorTextureID;
                newPass.depthTextureID = depthTextureID;

                PerObjectData perObjectData;
                if (lighting)
                {
                    perObjectData = PerObjectData.Lightmaps | PerObjectData.ReflectionProbes;    
                }
                else
                {
                    perObjectData = PerObjectData.None;
                }

                var list = renderGraph.CreateRendererList(new RendererListDesc(shaderTagIds, cullingResults, camera)
                {
                    sortingCriteria = opaque ? SortingCriteria.CommonOpaque : SortingCriteria.CommonTransparent,
                    rendererConfiguration = perObjectData,
                    renderQueueRange = opaque ? RenderQueueRange.opaque : RenderQueueRange.transparent,
                });
                newPass.rendererList = builder.UseRendererList(list);
                
                builder.SetRenderFunc<GeometryPass>((pass, context) => pass.Render(context));
            }
        }

        class SkyBoxPass : BasePass
        {
            RenderTargetIdentifier colorTextureID;
            RenderTargetIdentifier depthTextureID;
            
            public override void Render(RenderGraphContext context)
            {
                context.cmd.SetRenderTarget(colorTextureID, depthTextureID);
                context.renderContext.ExecuteCommandBuffer(context.cmd);
                context.cmd.Clear();

                context.renderContext.DrawSkybox(Camera);
            }
            
            public static void Record(RenderGraph renderGraph, Camera camera, RenderTargetIdentifier colorTextureID,
                RenderTargetIdentifier depthTextureID)
            {
                var renderSkybox = camera.clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null;

                if (renderSkybox == false)
                {
                    return;
                }

                // if (camera.cameraType == CameraType.SceneView || camera.cameraType == CameraType.Preview)
                // {
                //     renderSkybox = true;
                // }
                
                using RenderGraphBuilder builder =
                    renderGraph.AddRenderPass("Skybox", out SkyBoxPass newPass);
                newPass.Camera = camera;
                newPass.colorTextureID = colorTextureID;
                newPass.depthTextureID = depthTextureID;
                builder.SetRenderFunc<SkyBoxPass>((pass, context) => pass.Render(context));
            }
        }
        
        class ReflectionProbeManagerPass : BasePass
        {
            private ReflectionProbeManager manager;
            private CullingResults cullingResults;
            
            public override void Render(RenderGraphContext context)
            {
                manager.UpdateGpuData(context.cmd, ref cullingResults);
                context.renderContext.ExecuteCommandBuffer(context.cmd);
                context.cmd.Clear();
            }
            
            public static void Record(
                RenderGraph renderGraph,
                Camera camera,
                ReflectionProbeManager manager,
                ref CullingResults cullingResults)
            {
                using RenderGraphBuilder builder =
                    renderGraph.AddRenderPass("Reflection probe manager", out ReflectionProbeManagerPass newPass);
                newPass.Camera = camera;
                newPass.manager = manager;
                newPass.cullingResults = cullingResults;
                builder.AllowPassCulling(false);
                
                builder.SetRenderFunc<ReflectionProbeManagerPass>((pass, context) => pass.Render(context));
            }
        }

        class LightRenderPass : BasePass
        {
            private LightsRenderer renderer;
            private bool renderShadows;
            private ShadowSettings shadowSettings;
            private CullingResults cullingResults;
            
            public override void Render(RenderGraphContext context)
            {
                renderer.Render(ref context);
            }
            
            public static void Record(
                RenderGraph renderGraph,
                ScriptableRenderContext context,
                Camera camera,
                LightsRenderer lightsRenderer,
                ShadowSettings shadowSettings,
                bool renderShadows,
                ref CullingResults cullingResults)
            {
                using RenderGraphBuilder builder =
                    renderGraph.AddRenderPass("Light render", out LightRenderPass newPass);
                newPass.Camera = camera;
                newPass.renderer = lightsRenderer;
                newPass.cullingResults = cullingResults;
                newPass.shadowSettings = shadowSettings;
                newPass.renderShadows = renderShadows;
                
                lightsRenderer.Setup(renderGraph, context, builder, ref cullingResults, shadowSettings, renderShadows);
                
                builder.SetRenderFunc<LightRenderPass>((pass, context) => pass.Render(context));
            }
        }

        class CleanupPass : BasePass
        {
            private LightsRenderer lightsRenderer;
            private RenderTexture colorTarget;
            
            public override void Render(RenderGraphContext context)
            {
                if (OnAfterDraw != null)
                {
                    OnAfterDraw(Camera, context.cmd);
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                }
                
                lightsRenderer.Cleanup(ref context);
                
                context.renderContext.ExecuteCommandBuffer(context.cmd);
                context.cmd.Clear();
            }
            public static void Record(
                RenderGraph renderGraph,
                Camera camera,
                LightsRenderer lightsRenderer)
            {
                using RenderGraphBuilder builder =
                    renderGraph.AddRenderPass("Cleanup", out CleanupPass newPass);
                newPass.Camera = camera;
                newPass.lightsRenderer = lightsRenderer;
                builder.SetRenderFunc<CleanupPass>((pass, context) => pass.Render(context));
            }
        }

        class FinalBlitPass : BasePass
        {
            private RenderTargetIdentifier colorTarget;
            
            public override void Render(RenderGraphContext context)
            {
                //cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                context.cmd.Blit(colorTarget, BuiltinRenderTextureType.CameraTarget,
                    new Vector2(1.0f / Camera.rect.width, 1.0f / Camera.rect.height), new Vector2());
                context.renderContext.ExecuteCommandBuffer(context.cmd); 
                context.cmd.Clear();
            }
            
            public static void Record(
                RenderGraph renderGraph,
                Camera camera,
                RenderTargetIdentifier colorTarget)
            {
                using RenderGraphBuilder builder =
                    renderGraph.AddRenderPass("Final blit", out FinalBlitPass newPass);
                newPass.Camera = camera;
                newPass.colorTarget = colorTarget;
                builder.SetRenderFunc<FinalBlitPass>((pass, context) => pass.Render(context));
            }
        }

        class GizmosPass : BasePass
        {
            private RendererListHandle postImageRenderList;
            private RendererListHandle preImageRenderList;

            public override void Render(RenderGraphContext context)
            {
#if UNITY_EDITOR
                context.cmd.DrawRendererList(preImageRenderList);
                context.cmd.DrawRendererList(postImageRenderList);
#endif
            }

            [Conditional("UNITY_EDITOR")]
            public static void Record(RenderGraph graph, Camera camera)
            {
#if UNITY_EDITOR
                if (UnityEditor.Handles.ShouldRenderGizmos()
                    && camera.sceneViewFilterMode != Camera.SceneViewFilterMode.ShowFiltered)
                {
                    using var builder = graph.AddRenderPass("Gizmos", out GizmosPass gizmosPass);
                    gizmosPass.Camera = camera;
                    
                    var preImageList = graph.CreateGizmoRendererList(camera, GizmoSubset.PostImageEffects);
                    var postImageList = graph.CreateGizmoRendererList(camera, GizmoSubset.PostImageEffects);
                    gizmosPass.postImageRenderList = builder.UseRendererList(postImageList);
                    gizmosPass.preImageRenderList = builder.UseRendererList(preImageList);
                    
                    builder.SetRenderFunc<GizmosPass>((pass, context) => pass.Render(context));
                }
#endif
            }
        }

        CameraData getCameraRenderTextures(Camera camera)
        {
            CameraData rt = null;

            foreach (var cameraRenderTexture in cameraRenderTextures)
            {
                if (cameraRenderTexture.TargetCamera == camera)
                {
                    rt = cameraRenderTexture;
                    break;
                }
            }

            if (rt == null)
            {
                rt = new CameraData
                {
                    TargetCamera = camera,
                    TargetCameraName = camera.name
                };
                var settings = camera.GetComponent<CameraRenderSettings>();
                if (settings)
                {
                    rt.RenderSettings = settings.Settings;
                }
                else
                {
                    rt.RenderSettings = new()
                    {
                        IntervalMode = CameraRenderIntervalMode.None
                    };
                }
                cameraRenderTextures.Add(rt);
            }

            bool createNew;
            var width = camera.pixelWidth;
            var height = camera.pixelHeight;

            if (rt.RenderSettings != null && rt.RenderSettings.LowResRendering)
            {
                width /= 2;
                height /= 2;
            }
            
            if (rt.Depth)
            {
                if (rt.Depth.width == width && rt.Depth.height == height)
                {
                    createNew = false;
                }
                else
                {
                    createNew = true;
                }
            }
            else
            {
                createNew = true;
            }

            if (rt.RenderSettings.SkipDeferredPass == false && rt.GBuffer0 == false)
            {
                createNew = true;    
            }
            if (rt.RenderSettings.DisableDepth == false && rt.Depth == false)
            {
                createNew = true;
            }
            
            var shouldDrawToDedicatedColorTarget = SystemInfo.graphicsUVStartsAtTop || camera.rect != fullRect || rt.RenderSettings.LowResRendering;

            if (shouldDrawToDedicatedColorTarget)
            {
                if (rt.FinalBlit == false)
                {
                    createNew = true;
                }
            }
            else
            {
                if (rt.FinalBlit)
                {
                    RenderTexture.ReleaseTemporary(rt.FinalBlit);
                    rt.FinalBlit = null;
                    rt.FinalBlit_ID = default;
                }
            }
            
            if (createNew)
            {
                rt.Release();
            }

            if (createNew)
            {
                var name = camera.name;

                if (rt.RenderSettings == null || rt.RenderSettings.DisableDepth == false)
                {
                    rt.Depth = RenderTexture.GetTemporary(width, height, 32, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
                }
                else
                {
                    rt.Depth = RenderTexture.GetTemporary(4, 4, 32, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
                }
                
                rt.Depth.name = $"Depth ({name})";
                rt.Depth_ID = rt.Depth;
                
                // пока пропускаем создание LinearDepth, чтобы не расходовать память
                //rt.LinearDepth = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
                //rt.LinearDepth.name = $"Linear depth ({name})";
                //rt.LinearDepth_ID = rt.LinearDepth;

                bool isLinear = QualitySettings.activeColorSpace == ColorSpace.Linear;

                var rwMode = isLinear ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear;

                // TODO TEMP FIX
                rt.isHDR = camera.name.ToLower().Contains("reflection probe");
                //var gbufferFormat = rt.isHDR ? RenderTextureFormat.ARGBFloat : RenderTextureFormat.Default;
                //var gbufferFormat = isLinear ? RenderTextureFormat.ARGBFloat : RenderTextureFormat.Default;

                if (rt.RenderSettings.SkipDeferredPass == false)
                {
                    rt.GBuffer0 = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, rwMode);
                    rt.GBuffer0.name = $"GBuffer 0 ({name})";
                    rt.GBuffer1 = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, rwMode);
                    rt.GBuffer1.name = $"GBuffer 1 ({name})";
                    rt.GBuffer2 = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                    rt.GBuffer2.name = $"GBuffer 2 ({name})";

                    rt.GBuffer0TargetId = rt.GBuffer0;
                    rt.GBuffer1TargetId = rt.GBuffer1;
                    rt.GBuffer2TargetId = rt.GBuffer2;
                
                    rt.GBufferIDs[0] = rt.GBuffer0TargetId;
                    rt.GBufferIDs[1] = rt.GBuffer1TargetId;
                    rt.GBufferIDs[2] = rt.GBuffer2TargetId;
                }

                if (shouldDrawToDedicatedColorTarget)
                {
                    // TODO support gamma space
                    var colorTargetFormat = rt.isHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
                    
                    rt.FinalBlit = RenderTexture.GetTemporary(rt.Depth.width, rt.Depth.height, 0, colorTargetFormat,
                        RenderTextureReadWrite.sRGB);
                    rt.FinalBlit_ID = rt.FinalBlit;
                }
            }

            return rt;
        }
    }
}