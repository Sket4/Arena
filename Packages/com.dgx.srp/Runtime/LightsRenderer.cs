using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace DGX.SRP
{
    public struct LightInfo
    {
        private Vector4 data1;
        private Vector4 data2;
        private Vector4 data3;

        public Light Light { get; private set; }
        public DgxLightOptions LightOptions { get; private set; }

        public Vector4 DirectionAndRange => data1;
        public Vector4 PositionAndAngle => data2;
        public Vector4 Color => data3;
        public float Intensity => data3.w;
        public Matrix4x4 LocalToWorld { get; private set; }

        public bool HasShadows { get; private set; }

        public int Index { get; private set; }

        public Vector3 Direction
        {
            get => data1;
            set
            {
                data1.x = value.x;
                data1.y = value.y;
                data1.z = value.z;
            }
        }
        public Vector3 Position
        {
            get => data2;
            set
            {
                data2.x = value.x;
                data2.y = value.y;
                data2.z = value.z;
            }
        }

        public float Range
        {
            get => data1.w;
            set
            {
                data1.w = value;
            }
        }

        public LightInfo(VisibleLight light, int index)
        {
            Light = light.light;
            LightOptions = light.light.GetComponent<DgxLightOptions>();
            
            LocalToWorld = default;
            
            data1 = default;
            data2 = default;
            data3 = default;

            HasShadows = false;

            Index = -1;
            
            Update(light, index);
        }

        public void Update(VisibleLight light, int index)
        {
            Index = index;
            data1 = light.localToWorldMatrix.GetColumn(2);
            data1.w = light.range;
            data1.w *= data1.w;

            LocalToWorld = light.localToWorldMatrix;
            data2 = light.localToWorldMatrix.GetPosition();
            data2.w = 1.0f / ((light.spotAngle * 0.5f) / 180.0f);
            
            data3.x = light.finalColor.r;
            data3.y = light.finalColor.g;
            data3.z = light.finalColor.b;
            data3.w = 0;

            HasShadows = light.light.shadows != LightShadows.None && light.light.shadowStrength > 0;
        }
    }
    public class LightsRenderer
    {
        static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
        private static int spotLightDirectionId = Shader.PropertyToID("_SpotLightDirs");
        private static int spotLightPositionId = Shader.PropertyToID("_SpotLightPositions");
        private static int spotLightColorId = Shader.PropertyToID("_SpotLightColors");
        private static int spotLightCookieTexId = Shader.PropertyToID("_SpotLightCookieTex");
        private static int spotLightLocalToWorldInvMatrixId = Shader.PropertyToID("_SpotLight_M_Inv");
        private static int mainLightShadowOffsets0 = Shader.PropertyToID("_MainLightShadowOffsets0");
        private static int mainLightShadowOffsets1 = Shader.PropertyToID("_MainLightShadowOffsets1");


        private CullingResults cullingResults;
        private ShadowSettings settings;

        private List<LightInfo> visibleSpotLights = new();
        private List<LightInfo> visibleDirectionalLights = new();
        public IReadOnlyList<LightInfo> VisibleSpotLights => visibleSpotLights;
        private List<Vector4> tempArray = new();

        private static Texture2D spotLightCookieEmptyTexture;
        private bool renderShadows = true;

        private NativeArray<LightShadowCasterCullingInfo> cullingInfoPerLight;
        private int shadowedSpotLights = 0;
        private int shadowedDirectionalLights = 0;
        private NativeArray<ShadowSplitData> shadowSplitDataPerLight;
        private bool needCleanup = false;

        public void Setup(ref CullingResults cullingResults, ShadowSettings settings, bool renderShadows)
        {
            this.cullingResults = cullingResults;
            this.settings = settings;
            this.renderShadows = renderShadows;
            
            cullingInfoPerLight = new NativeArray<LightShadowCasterCullingInfo>(
                cullingResults.visibleLights.Length, Allocator.Temp);
            shadowSplitDataPerLight = new NativeArray<ShadowSplitData>(
                cullingInfoPerLight.Length, //* maxTilesPerLight,
                Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        }

        [RuntimeInitializeOnLoadMethod]
        static void init()
        {
            if (spotLightCookieEmptyTexture == null)
            {
                spotLightCookieEmptyTexture = Texture2D.whiteTexture;
            }
        }

        public void Render(ref RenderGraphContext context)
        {
            needCleanup = true;
            
            prepareSpotLights();
            prepareDirectionalLights();

            if (shadowedDirectionalLights + shadowedSpotLights > 0)
            {
                context.renderContext.CullShadowCasters(
                    cullingResults,
                    new ShadowCastersCullingInfos
                    {
                        perLightInfos = cullingInfoPerLight,
                        splitBuffer = shadowSplitDataPerLight
                    });
            }
            
            renderDirectionalLights(ref context);
            renderSpotLights(ref context);
        }

        private void prepareSpotLights()
        {
            for (var index = visibleSpotLights.Count - 1; index >= 0; index--)
            {
                var visibleSpotLight = visibleSpotLights[index];
                bool contains = false;

                foreach (var light in cullingResults.visibleLights)
                {
                    if (light.lightType != LightType.Spot)
                    {
                        continue;
                    }

                    if (visibleSpotLight.Light == light.light)
                    {
                        contains = true;
                    }
                }

                if (contains == false)
                {
                    visibleSpotLights.RemoveAt(index);
                }
            }

            for (var i = 0; i < cullingResults.visibleLights.Length; i++)
            {
                var light = cullingResults.visibleLights[i];
                if (light.lightType != LightType.Spot)
                {
                    continue;
                }

                int lightInfoIndex = -1;

                for (var index = 0; index < visibleSpotLights.Count; index++)
                {
                    var visibleSpotLight = visibleSpotLights[index];

                    if (visibleSpotLight.Light == light.light)
                    {
                        lightInfoIndex = index;
                    }
                }

                if (lightInfoIndex >= 0)
                {
                    var info = visibleSpotLights[lightInfoIndex];
                    info.Update(light, lightInfoIndex);
                    visibleSpotLights[lightInfoIndex] = info;
                    continue;
                }

                var lightInfo = new LightInfo(light, i);
                visibleSpotLights.Add(lightInfo);
            }

            shadowedSpotLights = 0;
            
            foreach (var spotLight in visibleSpotLights)
            {
                if (spotLight.HasShadows)
                {
                    shadowedSpotLights++;
                }
            }
        }

        private void renderSpotLights(ref RenderGraphContext context)
        {
            tempArray.Clear();
            
            foreach (var visibleSpotLight in visibleSpotLights)
            {
                tempArray.Add(visibleSpotLight.DirectionAndRange);    
            }

            var commands = context.cmd;

            if (tempArray.Count > 0)
            {
                commands.SetGlobalVectorArray(spotLightDirectionId, tempArray);
            }
            
            tempArray.Clear();
            
            foreach (var visibleSpotLight in visibleSpotLights)
            {
                tempArray.Add(visibleSpotLight.PositionAndAngle);    
            }

            if (tempArray.Count > 0)
            {
                commands.SetGlobalVectorArray(spotLightPositionId, tempArray);
            }
            
            tempArray.Clear();
            
            foreach (var visibleSpotLight in visibleSpotLights)
            {
                tempArray.Add(visibleSpotLight.Color);    
            }

            if (tempArray.Count > 0)
            {
                commands.SetGlobalVectorArray(spotLightColorId, tempArray);
            }

            if (visibleSpotLights.Count > 0)
            {
                var firstVisible = visibleSpotLights[0];

                if (firstVisible.Light.cookie)
                {
                    commands.SetGlobalTexture(spotLightCookieTexId, firstVisible.Light.cookie);    
                }
                else
                {
                    commands.SetGlobalTexture(spotLightCookieTexId, spotLightCookieEmptyTexture);
                }
                
                commands.SetGlobalMatrix(spotLightLocalToWorldInvMatrixId, firstVisible.LocalToWorld.inverse);
            }
            
            ExecuteBuffer(ref context);
        }

        void prepareDirectionalLights()
        {
            visibleDirectionalLights.Clear();

            for (int i = 0; i < cullingResults.visibleLights.Length; i++)
            {
                var visibleLight = cullingResults.visibleLights[i];
                var light = visibleLight.light;
                
                if (light.type != LightType.Directional)
                {
                    continue;
                }
                
                visibleDirectionalLights.Add(new LightInfo(visibleLight, i));
            }

            shadowedDirectionalLights = 0;

            foreach (var directionalLight in visibleDirectionalLights)
            {
                if (directionalLight.HasShadows)
                {
                    shadowedDirectionalLights++;
                }
            }
        }

        void renderDirectionalLights(ref RenderGraphContext context)
        {
            var commands = context.cmd;
            
            var atlasSize = (int)settings.DirectionalMapSize;
            commands.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 16, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            commands.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            commands.ClearRenderTarget(true, false, Color.clear);

            bool isMainLightRendered = false;

            foreach (var visibleLight in visibleDirectionalLights)
            {
                if (visibleLight.HasShadows == false)
                {
                    continue;
                }
                
                var light = visibleLight.Light;

                if (cullingResults.GetShadowCasterBounds(visibleLight.Index, out var shadowCasterBounds) == false)
                {
                    continue;
                }

                if (renderShadows)
                {
                    var shadowDrawingSettings = new ShadowDrawingSettings(cullingResults, visibleLight.Index);

                    cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                        visibleLight.Index,
                        0,
                        1,
                        Vector3.zero,
                        atlasSize,
                        0,
                        out var viewMatrix,
                        out var projMatrix,
                        out var shadowSplitData
                    );
                    
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
                    
                    ExecuteBuffer(ref context);
                    
                    context.renderContext.DrawShadows(ref shadowDrawingSettings);
                    commands.SetGlobalDepthBias(0, 0);
                }

                isMainLightRendered = true;
            }

            commands.SetGlobalVector("_SubtractiveShadowColor", RenderSettings.subtractiveShadowColor);
            
            if (isMainLightRendered == false && renderShadows)
            {
                var mainLightShadowParams = new Vector4();
                mainLightShadowParams.x = 0;
                mainLightShadowParams.y = 1;
                commands.SetGlobalVector("dgx_MainLightShadowParams", mainLightShadowParams);

                float invShadowAtlasSize = 1.0f / atlasSize;
                commands.SetGlobalVector(mainLightShadowOffsets0, 
                    new Vector4(-invShadowAtlasSize, invShadowAtlasSize, 
                        invShadowAtlasSize,invShadowAtlasSize));
                
                commands.SetGlobalVector(mainLightShadowOffsets1, 
                    new Vector4(-invShadowAtlasSize, -invShadowAtlasSize, 
                        invShadowAtlasSize,-invShadowAtlasSize));
            }
            
            ExecuteBuffer(ref context);
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

        public void Cleanup(ref RenderGraphContext context)
        {
            if (needCleanup == false)
            {
                return;
            }

            needCleanup = false;
            visibleSpotLights.Clear();
            context.cmd.ReleaseTemporaryRT(dirShadowAtlasId);
        }

        public void ExecuteBuffer(ref RenderGraphContext context)
        {
            context.renderContext.ExecuteCommandBuffer(context.cmd);
            context.cmd.Clear();
        }
    }
}