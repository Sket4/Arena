using UnityEngine;
using UnityEngine.Rendering;

namespace DGX.SRP
{
    public class Shadows
    {
        private const string bufferName = "Shadows";
        static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");

        private CommandBuffer commands = new()
        {
            name = bufferName
        };
        private ScriptableRenderContext context;
        private CullingResults cullingResults;
        private ShadowSettings settings;

        public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings settings)
        {
            this.context = context;
            this.cullingResults = cullingResults;
            this.settings = settings;
        }

        public void Render()
        {
            renderDirectionalShadows();
        }

        void renderDirectionalShadows()
        {
            var atlasSize = (int)settings.DirectionalMapSize;
            commands.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 16, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            commands.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            commands.ClearRenderTarget(true, false, Color.clear);

            bool isMainLightRendered = false;
            
            for(int i=0; i<cullingResults.visibleLights.Length; i++)
            {
                var visibleLight = cullingResults.visibleLights[i];
                var light = visibleLight.light;

                if (light.shadows == LightShadows.None || light.shadowStrength <= 0)
                {
                    continue;
                }

                if (cullingResults.GetShadowCasterBounds(i, out var shadowCasterBounds) == false)
                {
                    continue;
                }

                if (light.type != LightType.Directional)
                {
                    continue;
                }

                var lightDir = -visibleLight.localToWorldMatrix.GetColumn(2);
                var shadowDrawingSettings = new ShadowDrawingSettings(cullingResults, i, BatchCullingProjectionType.Orthographic);

                cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                    i,
                    0,
                    1,
                    Vector3.zero,
                    atlasSize,
                    0,
                    out var viewMatrix,
                    out var projMatrix,
                    out var shadowSplitData
                );

                shadowDrawingSettings.splitData = shadowSplitData;
                
                commands.SetViewProjectionMatrices(viewMatrix, projMatrix);
                var mtx = projMatrix * viewMatrix;
                convertShadowMatrix(ref mtx);
                commands.SetGlobalMatrix("_ShadowVP", mtx);
                var mainLightShadowParams = new Vector4();
                mainLightShadowParams.x = light.shadowStrength;
                mainLightShadowParams.y = settings.MaxDistance;

                // TODO
                int shadowedDirectionalLightCount = 1;
                int split = shadowedDirectionalLightCount <= 1 ? 1 : 2;
                int tileSize = atlasSize / split;
                var cullingSphere = shadowSplitData.cullingSphere;
                var texelSize = 2f * cullingSphere.w / tileSize;
                texelSize *= 1.4142136f;
                
                // пока поддерживается только один каскад
                var cascadeData = new Vector4(
                    1f / cullingSphere.w,
                    texelSize
                );

                var normalBias = light.shadowNormalBias * cascadeData.y;
                mainLightShadowParams.z = normalBias;
                
                commands.SetGlobalVector("dgx_MainLightShadowParams", mainLightShadowParams);
                
                commands.SetGlobalDepthBias(0, light.shadowBias);
                
                ExecuteBuffer();
                
                context.DrawShadows(ref shadowDrawingSettings);
                commands.SetGlobalDepthBias(0, 0);

                isMainLightRendered = true;
            }

            commands.SetGlobalVector("_SubtractiveShadowColor", RenderSettings.subtractiveShadowColor);
            
            if (isMainLightRendered == false)
            {
                var mainLightShadowParams = new Vector4();
                mainLightShadowParams.x = 0;
                mainLightShadowParams.y = 1;
                commands.SetGlobalVector("dgx_MainLightShadowParams", mainLightShadowParams);
            }
            
            ExecuteBuffer();
        }

        static void convertShadowMatrix(ref Matrix4x4 m)
        {
            if (SystemInfo.usesReversedZBuffer)
            {
                m.m20 = -m.m20;
                m.m21 = -m.m21;
                m.m22 = -m.m22;
                m.m23 = -m.m23;
            }

            int split = 1;
            Vector2 offset = new Vector2(0, 0);
            
            float scale = 1f / split;
            m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
            m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
            m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
            m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
            m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
            m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
            m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
            m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
            m.m20 = 0.5f * (m.m20 + m.m30);
            m.m21 = 0.5f * (m.m21 + m.m31);
            m.m22 = 0.5f * (m.m22 + m.m32);
            m.m23 = 0.5f * (m.m23 + m.m33);
        }

        public void Cleanup()
        {
            commands.ReleaseTemporaryRT(dirShadowAtlasId);
            ExecuteBuffer();
        }

        public void ExecuteBuffer()
        {
            context.ExecuteCommandBuffer(commands);
            commands.Clear();
        }
    }
}