using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace DGX.SRP
{
    public class RenderPipeline : UnityEngine.Rendering.RenderPipeline
    {
        public RenderPipelineAsset Asset { get; }

        Material LightingPassMaterial;
        Material LinearizeDepthMaterial;
        private Mesh fullscreenTriangle;
        private Mesh fullscreenQuad;
        private bool isOpenGL;
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

        public static event Action<Camera, CommandBuffer> OnBeforeDraw;
        public static event Action<Camera, CommandBuffer> OnAfterDraw;
        
        class CameraData
        {
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
            public RenderTargetIdentifier[] GBufferIDs = new RenderTargetIdentifier[2];
            public RenderTexture Depth;
            public RenderTargetIdentifier Depth_ID;
            public RenderTexture LinearDepth;
            public RenderTargetIdentifier LinearDepth_ID;

            public bool isHDR;
            // RenderTexture Color0;
            //public RenderTargetIdentifier Color0_ID;
            // public RenderTexture Color1;
            // public RenderTargetIdentifier Color1_ID;

            //private bool isUsingColor1 = true;

            // public RenderTargetIdentifier SwapAndGetColorRT()
            // {
            //     isUsingColor1 = !isUsingColor1;
            //     return isUsingColor1 ? Color0_ID : Color1_ID;
            // }

            public void Release()
            {
                RenderTexture.ReleaseTemporary(GBuffer0);
                RenderTexture.ReleaseTemporary(GBuffer1);
                //RenderTexture.ReleaseTemporary(GBuffer2);
                RenderTexture.ReleaseTemporary(Depth);
                RenderTexture.ReleaseTemporary(LinearDepth);
                //RenderTexture.ReleaseTemporary(Color0);
                //RenderTexture.ReleaseTemporary(Color1);
            }
        }

        private List<CameraData> cameraRenderTextures = new();
        
        public RenderPipeline(RenderPipelineAsset asset)
        {
            GraphicsSettings.useScriptableRenderPipelineBatching = true;
            Asset = asset;

            checkResources();
            
            isOpenGL = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore
                || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2
                || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2;
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

            foreach (var cameraRT in cameraRenderTextures)
            {
                cameraRT.Release();    
            }
            
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
        
        void SetupMatrixConstants(CommandBuffer cmd, Camera camera)
        {
            Matrix4x4 proj = camera.projectionMatrix;
            Matrix4x4 view = camera.worldToCameraMatrix;
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
                            continue;
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
                            continue;
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
                    continue;
                }

                cullingParameters.maximumVisibleLights = int.MaxValue;
                cullingParameters.shadowDistance = Mathf.Min(Asset.ShadowSettings.MaxDistance, camera.farClipPlane);

                // Use the culling parameters to perform a cull operation, and store the results
                var cullingResults = context.Cull(ref cullingParameters);

                var shouldRenderShadows = enableShadows_global && rt.RenderSettings.RenderShadows;

                if (enableLights_global)
                {
                    RenderLights(context, cullingResults, shouldRenderShadows);    
                }
                
                var depthTextureID = rt.Depth_ID;
                
                
                var clearFlags = camera.clearFlags;
                bool shouldClearColor = clearFlags == CameraClearFlags.Color;
                
#if UNITY_EDITOR
                if (camera.cameraType == CameraType.Preview)
                {
                    shouldClearColor = true;
                }
#endif
                
                context.SetupCameraProperties(camera);
                
                // Tell Unity how to sort the geometry, based on the current Camera
                var sortingSettings = new SortingSettings(camera);
                DrawingSettings drawingSettings;
                
                // Tell Unity how to filter the culling results, to further specify which geometry to draw
                // Use FilteringSettings.defaultValue to specify no filtering
                var filteringSettings = FilteringSettings.defaultValue;

                var cmd = new CommandBuffer();
                RenderTexture colorTarget = null;
                RenderTargetIdentifier colorTextureID;
                var cameraRect = camera.rect;
                var useDedicatedColorTarget = shouldDrawToDedicatedColorTarget(camera, rt.RenderSettings);

                Mesh fullscreenMesh;
                    
                if (useDedicatedColorTarget)
                {
                    // TODO support gamma space
                    var colorTargetFormat = rt.isHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
                        
                    colorTarget = RenderTexture.GetTemporary(rt.Depth.width, rt.Depth.height, 0, colorTargetFormat, RenderTextureReadWrite.sRGB);
                    colorTextureID = colorTarget;
                    cmd.SetGlobalVector("_DrawScale", new Vector4(1, 1,0,0));
                    fullscreenMesh = fullscreenTriangle;
                }
                else
                {
                    colorTextureID = BuiltinRenderTextureType.CameraTarget;
                        
                    var drawScale = new Vector4(cameraRect.width, cameraRect.height, 0, 0);
                    cmd.SetGlobalVector("_DrawScale", drawScale);
                    bool isFullRect = cameraRect == fullRect;
                    fullscreenMesh = isFullRect ? fullscreenTriangle : fullscreenQuad;
                }
                cmd.SetGlobalTexture("_LinearDepth", rt.LinearDepth_ID);
                cmd.SetGlobalTexture("_Depth", depthTextureID);

                OnBeforeDraw?.Invoke(camera, cmd);

                if (rt.RenderSettings.SkipDeferredPass == false)
                {
                    // GBUFFER
                    cmd.name = "gbuffer";
                    cmd.SetGlobalTexture("_GT0", rt.GBuffer0);
                    cmd.SetGlobalTexture("_GT1", rt.GBuffer1);
                
                    //RenderTargetIdentifier depthTextureID = rt.Depth_ID;
                
                    cmd.SetRenderTarget(rt.GBufferIDs, depthTextureID);
                    cmd.ClearRenderTarget(true, 
                        false,
                        camera.backgroundColor);
                
                    context.ExecuteCommandBuffer(cmd); 
                    cmd.Release();
                
                    // Tell Unity which geometry to draw, based on its LightMode Pass tag value
                    var shaderTagId = new ShaderTagId("gbuffer");

                    // Create a DrawingSettings struct that describes which geometry to draw and how to draw it
                    drawingSettings = new DrawingSettings(shaderTagId, sortingSettings)
                    {
                        perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe,
                        enableInstancing = true
                    };

                    filteringSettings.renderQueueRange = RenderQueueRange.opaque;
        
                    // Schedule a command to draw the geometry, based on the settings you have defined
                    context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);    
                    
                    // LINEAR DEPTH
                    // cmd = new CommandBuffer();
                    // cmd.name = "Linearize depth";
                    // cmd.SetGlobalVector("_BlitScaleBias", new Vector4(1,1,0,0));
                    // cmd.Blit(rt.Depth, rt.LinearDepth, LinearizeDepthMaterial);
                    // context.ExecuteCommandBuffer(cmd);
                    // cmd.Release();
                    
                    
                    // DEFERRED LIGHTING
                    cmd = new CommandBuffer();
                    cmd.name = "lightpass";

                    SetupMatrixConstants(cmd, camera);
                    
                    cmd.SetRenderTarget(colorTextureID, rt.GBuffer0TargetId);
                    if (shouldClearColor)
                    {
                        cmd.ClearRenderTarget(false, true, camera.backgroundColor);    
                    }

                    if (lightRenderer.VisibleSpotLights.Count > 0)
                    {
                        cmd.EnableShaderKeyword(SPOT_LIGHTS);
                    }
                    else
                    {
                        cmd.DisableShaderKeyword(SPOT_LIGHTS);
                    }

                    int deferredPass;
                    bool isFogEnabled = RenderSettings.fog && camera.orthographic == false;
                    
                    if (isFogEnabled)
                    {
                        deferredPass = 0;
                    }
                    else
                    {
                        deferredPass = 1;
                    }
                    
                    cmd.DrawMesh(fullscreenMesh, Matrix4x4.identity, LightingPassMaterial, 0, deferredPass);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Release();
                }
                else
                {
                    cmd.SetRenderTarget(colorTextureID, depthTextureID);
                    cmd.ClearRenderTarget(true, 
                        shouldClearColor,
                        camera.backgroundColor);
                    
                    context.ExecuteCommandBuffer(cmd); 
                    cmd.Release();
                }
                
                // FORWARD UNLIT
                //cmd.SetViewport(camera.pixelRect);
                cmd = new CommandBuffer();
                cmd.name = "Forward unlit";
                sortingSettings.criteria = SortingCriteria.CommonOpaque;
                drawingSettings = new DrawingSettings(srpDefaultUnlitShaderTag, sortingSettings)
                {
                    enableInstancing = true,
                };
                filteringSettings.renderQueueRange = RenderQueueRange.opaque;
                
                cmd.SetRenderTarget(colorTextureID, depthTextureID);
                context.ExecuteCommandBuffer(cmd);
                cmd.Release();

                context.DrawRenderers(
                    cullingResults, ref drawingSettings, ref filteringSettings
                );
                
                // SKYBOX
                skyboxPass(context, camera, colorTextureID, depthTextureID);

                // FORWARD TRANSPARENTS
                cmd = new CommandBuffer();
                cmd.name = "forward transparents";
                cmd.SetRenderTarget(colorTextureID, depthTextureID);
                context.ExecuteCommandBuffer(cmd);
                cmd.Release();
                
                sortingSettings.criteria = SortingCriteria.CommonTransparent;
                drawingSettings = new DrawingSettings(srpDefaultUnlitShaderTag, sortingSettings)
                {
                    perObjectData = PerObjectData.Lightmaps | PerObjectData.ReflectionProbes,
                    enableInstancing = true
                };
                drawingSettings.SetShaderPassName(1, dgxForwardShaderTag);
                filteringSettings.renderQueueRange = RenderQueueRange.transparent;
                

                context.DrawRenderers(
                    cullingResults, ref drawingSettings, ref filteringSettings
                );

                if (colorTarget)
                {
                    cmd = new CommandBuffer();
                    cmd.name = "final blit";
                    //cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                    cmd.Blit(colorTarget, BuiltinRenderTextureType.CameraTarget, new Vector2(1.0f/camera.rect.width, 1.0f/camera.rect.height), new Vector2());
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Release();
                }
                
#if UNITY_EDITOR
                if (UnityEditor.Handles.ShouldRenderGizmos()  
                    && camera.sceneViewFilterMode != Camera.SceneViewFilterMode.ShowFiltered) 
                {
                    context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                    context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
                }
#endif
                if (OnAfterDraw != null)
                {
                    cmd = new CommandBuffer();
                    cmd.name = "on after draw";
                    OnBeforeDraw(camera, cmd);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Release();
                }
                
                context.Submit();

                if (enableLights_global)
                {
                    lightRenderer.Cleanup();    
                }

                if (colorTarget)
                {
                    RenderTexture.ReleaseTemporary(colorTarget);
                }
            }
        }

        private static void skyboxPass(ScriptableRenderContext context, Camera camera, RenderTargetIdentifier colorTextureID,
            RenderTargetIdentifier depthTextureID)
        {
            if (camera.clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null)
            {
                var cmd = new CommandBuffer();
                cmd.name = "skybox";
                cmd.SetRenderTarget(colorTextureID, depthTextureID);
                context.ExecuteCommandBuffer(cmd);
                cmd.Release();

                context.DrawSkybox(camera);
            }
        }

        bool shouldDrawToDedicatedColorTarget(Camera camera, CameraRenderSettingsData settings)
        {
            return SystemInfo.graphicsUVStartsAtTop || camera.rect != fullRect || settings.LowResRendering;
        }

        private void RenderLights(ScriptableRenderContext context, CullingResults cullingResults, bool renderShadows)
        {
            lightRenderer.Setup(context, cullingResults, Asset.ShadowSettings, renderShadows);
            lightRenderer.Render();
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
                    rt.Release();
                    createNew = true;
                }
            }
            else
            {
                createNew = true;
            }

            if (createNew)
            {
                var name = camera.name;
                
                rt.Depth = RenderTexture.GetTemporary(width, height, 32, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
                rt.Depth.name = $"Depth ({name})";
                rt.Depth_ID = rt.Depth;
                
                rt.LinearDepth = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.R16, RenderTextureReadWrite.Linear);
                rt.LinearDepth.name = $"Linear depth ({name})";
                rt.LinearDepth_ID = rt.LinearDepth;

                bool isLinear = QualitySettings.activeColorSpace == ColorSpace.Linear;

                // var gBufferDescriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32);
                // gBufferDescriptor.sRGB = !isLinear;
                // gBufferDescriptor.depthBufferBits = 0;
                var rwMode = isLinear ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear;

                // TODO TEMP FIX
                rt.isHDR = camera.name.ToLower().Contains("reflection probe");
                var gbufferFormat = rt.isHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
                
                rt.GBuffer0 = RenderTexture.GetTemporary(width, height, 0, gbufferFormat, rwMode);
                rt.GBuffer0.name = $"GBuffer 0 ({name})";
                rt.GBuffer1 = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                rt.GBuffer1.name = $"GBuffer 1 ({name})";
                //rt.GBuffer2 = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                //rt.GBuffer2.name = $"GBuffer 2 ({name})";

                rt.GBuffer0TargetId = rt.GBuffer0;
                rt.GBuffer1TargetId = rt.GBuffer1;
                //rt.GBuffer2TargetId = rt.GBuffer2;
                
                rt.GBufferIDs[0] = rt.GBuffer0TargetId;
                rt.GBufferIDs[1] = rt.GBuffer1TargetId;
                //rt.GBufferIDs[2] = rt.GBuffer2TargetId;

                // rt.Color0 = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32,
                //     rwMode);
                //
                // rt.Color0.name = $"Color A ({name})";
                // rt.Color0_ID = rt.Color0;
            }

            return rt;
        }
    }
}