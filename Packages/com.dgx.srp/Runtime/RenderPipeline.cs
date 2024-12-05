using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DGX.SRP
{
    public class RenderPipeline : UnityEngine.Rendering.RenderPipeline
    {
        public RenderPipelineAsset Asset { get; }

        Material LightingPassMaterial;
        private Mesh fullscreenMesh;
        private bool isOpenGL;
        int _ScreenToWorld = Shader.PropertyToID("_ScreenToWorld");
        static readonly ShaderTagId srpDefaultUnlitShaderTag = new("SRPDefaultUnlit");
        static readonly ShaderTagId dgxForwardShaderTag = new("DGXForward");

        class CameraRenderTextures
        {
            public Camera TargetCamera;
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
            public RenderTexture Color0;
            public RenderTargetIdentifier Color0_ID;
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
                RenderTexture.ReleaseTemporary(GBuffer2);
                RenderTexture.ReleaseTemporary(Depth);
                RenderTexture.ReleaseTemporary(LinearDepth);
                RenderTexture.ReleaseTemporary(Color0);
                //RenderTexture.ReleaseTemporary(Color1);
            }
        }

        private List<CameraRenderTextures> cameraRenderTextures = new();
        
        public RenderPipeline(RenderPipelineAsset asset)
        {
            GraphicsSettings.useScriptableRenderPipelineBatching = true;
            Asset = asset;

            checkResources();
            
            isOpenGL = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore
                || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2
                || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2;
        }

        void checkResources()
        {
            if(LightingPassMaterial == false)
                LightingPassMaterial = new Material(Shader.Find("Hidden/DGX/DEFERRED"));

            if(fullscreenMesh == false)
                fullscreenMesh = CreateFullscreenMesh();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            foreach (var cameraRT in cameraRenderTextures)
            {
                cameraRT.Release();    
            }
            
            for(int i=0; i<3; i++) 
                Shader.SetGlobalTexture("_GT"+i, null);

            if (LightingPassMaterial)
            {
                destroyObject(ref LightingPassMaterial);
            }
            
            destroyObject(ref fullscreenMesh);
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
        
        static Mesh CreateFullscreenMesh()
        {
            // TODO reorder for pre&post-transform cache optimisation.
            // Simple full-screen triangle
            Vector3[] positions =
            {
                new Vector3(-1.0f,  1.0f, 0.0f),
                new Vector3(-1.0f, -3.0f, 0.0f),
                new Vector3(3.0f,  1.0f, 0.0f)
            };

            int[] indices = { 0, 1, 2 };

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

            var width = camera.pixelWidth;
            var height = camera.pixelHeight;

            // xy coordinates in range [-1; 1] go to pixel coordinates.
            Matrix4x4 toScreen = new Matrix4x4(
                new Vector4(0.5f * width, 0.0f, 0.0f, 0.0f),
                new Vector4(0.0f, 0.5f * height, 0.0f, 0.0f),
                new Vector4(0.0f, 0.0f, 1.0f, 0.0f),
                new Vector4(0.5f * width, 0.5f * height, 0.0f, 1.0f)
            );

            Matrix4x4 zScaleBias = Matrix4x4.identity;
                
            if (isOpenGL)
            {
                // We need to manunally adjust z in NDC space from [-1; 1] to [0; 1] (storage in depth texture).
                zScaleBias = new Matrix4x4(
                    new Vector4(1.0f, 0.0f, 0.0f, 0.0f),
                    new Vector4(0.0f, 1.0f, 0.0f, 0.0f),
                    new Vector4(0.0f, 0.0f, 0.5f, 0.0f),
                    new Vector4(0.0f, 0.0f, 0.5f, 1.0f)
                );
            }

            var screenToWorld = Matrix4x4.Inverse(toScreen * zScaleBias * gpuProj * view);
            
            cmd.SetGlobalMatrix(_ScreenToWorld, screenToWorld);
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

            foreach (Camera camera in cameras)
            {
                var rt = getCameraRenderTextures(camera);
                
                // GBUFFER
                Shader.SetGlobalTexture("_GT0", rt.GBuffer0);
                Shader.SetGlobalTexture("_GT1", rt.GBuffer1);
                Shader.SetGlobalTexture("_GT2", rt.GBuffer2);
                Shader.SetGlobalTexture("_Depth", rt.Depth);
                
                var clearFlags = camera.clearFlags;
                
                context.SetupCameraProperties(camera);
                
                var cmd = new CommandBuffer();
                cmd.name = "gbuffer";
                
                cmd.SetGlobalTexture("_LinearDepth", rt.LinearDepth_ID);
                
                cmd.SetRenderTarget(rt.GBufferIDs, rt.Depth_ID);
                cmd.ClearRenderTarget(true, 
                    clearFlags == CameraClearFlags.Color
                    , camera.backgroundColor);
                
                context.ExecuteCommandBuffer(cmd); 
                cmd.Release();
                
                // Get the culling parameters from the current Camera
                camera.TryGetCullingParameters(out var cullingParameters);

                // Use the culling parameters to perform a cull operation, and store the results
                var cullingResults = context.Cull(ref cullingParameters);
                
                // Tell Unity which geometry to draw, based on its LightMode Pass tag value
                ShaderTagId shaderTagId = new ShaderTagId("gbuffer");

                // Tell Unity how to sort the geometry, based on the current Camera
                var sortingSettings = new SortingSettings(camera);

                // Create a DrawingSettings struct that describes which geometry to draw and how to draw it
                DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings)
                {
                    perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe,
                    enableInstancing = true
                };

                // Tell Unity how to filter the culling results, to further specify which geometry to draw
                // Use FilteringSettings.defaultValue to specify no filtering
                FilteringSettings filteringSettings = FilteringSettings.defaultValue;
                filteringSettings.renderQueueRange = RenderQueueRange.opaque;
        
                // Schedule a command to draw the geometry, based on the settings you have defined
                context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
                
                // LINEAR DEPTH
                cmd = new CommandBuffer();
                cmd.name = "Linearize depth";
                cmd.Blit(rt.Depth, rt.LinearDepth);
                context.ExecuteCommandBuffer(cmd);
                cmd.Release();
                
                
                // DEFERRED LIGHTING
                cmd = new CommandBuffer();
                cmd.name = "lightpass";

                SetupMatrixConstants(cmd, camera);
                
                var colorTexture = rt.Color0_ID;
                cmd.SetRenderTarget(colorTexture, rt.Depth);

                cmd.DrawMesh(fullscreenMesh, Matrix4x4.identity, LightingPassMaterial, 0, 0);
                //cmd.DrawMesh(fullscreenMesh, Matrix4x4.identity, LightingPassMaterial, 0, 1);
                
                // FORWARD UNLIT
                sortingSettings.criteria = SortingCriteria.CommonOpaque;
                drawingSettings = new DrawingSettings(srpDefaultUnlitShaderTag, sortingSettings)
                {
                    enableInstancing = true,
                };
                filteringSettings.renderQueueRange = RenderQueueRange.opaque;
                
                context.ExecuteCommandBuffer(cmd);
                cmd.Release();

                context.DrawRenderers(
                    cullingResults, ref drawingSettings, ref filteringSettings
                );
                
                // SKYBOX
                context.SetupCameraProperties(camera);
                
                if (clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null)
                {
                    cmd = new CommandBuffer();
                    cmd.name = "skybox";
                    cmd.SetRenderTarget(colorTexture, rt.Depth_ID);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Release();
                    
                    context.DrawSkybox(camera);
                }
                
                // FORWARD TRANSPARENTS
                cmd = new CommandBuffer();
                cmd.name = "forward transparents";
                cmd.SetRenderTarget(colorTexture, rt.Depth_ID);
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

                cmd = new CommandBuffer();
                cmd.name = "final blit";
                //cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                cmd.Blit(colorTexture, BuiltinRenderTextureType.CameraTarget);
                context.ExecuteCommandBuffer(cmd);
                cmd.Release();
                
                context.Submit();
            }
        }
        
        CameraRenderTextures getCameraRenderTextures(Camera camera)
        {
            CameraRenderTextures rt = null;

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
                rt = new CameraRenderTextures
                {
                    TargetCamera = camera
                };
                cameraRenderTextures.Add(rt);
            }

            bool createNew;
            var width = camera.pixelWidth;
            var height = camera.pixelHeight;
            
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
                
                rt.LinearDepth = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
                rt.LinearDepth.name = $"Linear depth ({name})";
                rt.LinearDepth_ID = rt.LinearDepth;

                bool isLinear = QualitySettings.activeColorSpace == ColorSpace.Linear;

                var rwMode = isLinear ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear;
                
                rt.GBuffer0 = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, rwMode);
                rt.GBuffer0.name = $"GBuffer 0 ({name})";
                rt.GBuffer1 = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, rwMode);
                rt.GBuffer1.name = $"GBuffer 1 ({name})";
                rt.GBuffer2 = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                rt.GBuffer2.name = $"GBuffer 2 ({name})";

                rt.GBuffer0TargetId = rt.GBuffer0;
                rt.GBuffer1TargetId = rt.GBuffer1;
                rt.GBuffer2TargetId = rt.GBuffer2;
                
                rt.GBufferIDs[0] = rt.GBuffer0TargetId;
                rt.GBufferIDs[1] = rt.GBuffer1TargetId;
                rt.GBufferIDs[2] = rt.GBuffer2TargetId;

                rt.Color0 = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32,
                    rwMode);
                
                rt.Color0.name = $"Color A ({name})";
                rt.Color0_ID = rt.Color0;
                
                // rt.Color1 = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32,
                //     RenderTextureReadWrite.sRGB);
                //
                // rt.Color1.name = $"Color B ({name})";
                // rt.Color1_ID = rt.Color1;
            }

            return rt;
        }
    }
}