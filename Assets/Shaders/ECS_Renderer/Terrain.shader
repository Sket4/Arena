Shader "Arena/Terrain"
{
    Properties
    {
        [NoScaleOffset] _SplatMap ("Splat map", 2D) = "white" {}
        [NoScaleOffset] _Color1("Color 1", 2D) = "black" {}
        [NoScaleOffset] _Normal1("Normal 1", 2D) = "white" {}
        [NoScaleOffset] _SAH1("Sm AO HGHT 1", 2D) = "black" {}
        
        [NoScaleOffset] _Color2("Color 2", 2D) = "black" {}
        [NoScaleOffset] _Normal2("Normal 2", 2D) = "white" {}
        [NoScaleOffset] _SAH2("Sm AO HGHT 2", 2D) = "black" {}
        
        [NoScaleOffset] _Color3("Color 3", 2D) = "black" {}
        [NoScaleOffset] _Normal3("Normal 3", 2D) = "white" {}
        [NoScaleOffset] _SAH3("Sm AO HGHT 3", 2D) = "black" {}
        
        [NoScaleOffset] _Color4("Color 4", 2D) = "black" {}
        [NoScaleOffset] _Normal4("Normal 4", 2D) = "white" {}
        [NoScaleOffset] _SAH4("Sm AO HGHT 4", 2D) = "black" {}
        
        //_Layers_Roughness("Layers roughness", Vector) = (0.5,0.5,0.5,0.5)
        _Layers_Tiling("Layers tiling", Vector) = (1,1,1,1)
        _HighlightRemove("Highlight remove", Float) = 0
        
//        _UnderwaterColor("Underwater color", Color) = (1,1,1,1)
//        _WaterHeight("Water height", Float) = 5
//        _WaterHeightFade("Water height fade", Float) = 1
        
        
        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {} 
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
        }
        LOD 100
        
        Pass
        {   
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma require 2darray
            #pragma require cubearray
            #pragma exclude_renderers gles //excluded shader from OpenGL ES 2.0 because it uses non-square matrices
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile __ ARENA_USE_MAIN_LIGHT
            #pragma multi_compile __ ARENA_USE_ADD_LIGHT
            
            #pragma multi_compile UG_QUALITY_LOW UG_QUALITY_MED UG_QUALITY_HIGH
            //#pragma multi_compile_fwdbase
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED

            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES

            #if defined(UG_QUALITY_LOW)
            #undef DIRLIGHTMAP_COMBINED
            #endif

            #ifndef TG_USE_URP
                #define TG_USE_URP
            #endif
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "HLSLSupport.cginc"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.tzargames.rendering/Shaders/Lighting.hlsl"
            #include "Common.hlsl"
            
            
            struct appdata
            {
                float3 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
#if LIGHTMAP_ON
                TG_DECLARE_LIGHTMAP_UV(1)
#endif
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                half4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                TG_DECLARE_INSTANCE_DATA(1)

                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float4 positionWS_fog : TEXCOORD5;
#if LIGHTMAP_ON
                TG_DECLARE_LIGHTMAP_UV(7)
#endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Layers_Tiling;
                half _HighlightRemove;
                //half4 _UnderwaterColor;
                //half _WaterHeight;
                //half _WaterHeightFade;
            CBUFFER_END

            sampler2D _SplatMap;

            UNITY_DECLARE_TEX2D(_Color1);
            UNITY_DECLARE_TEX2D(_Normal1);
            UNITY_DECLARE_TEX2D(_SAH1);

            UNITY_DECLARE_TEX2D_NOSAMPLER(_Color2);
            UNITY_DECLARE_TEX2D_NOSAMPLER(_Normal2);
            UNITY_DECLARE_TEX2D_NOSAMPLER(_SAH2);

            UNITY_DECLARE_TEX2D_NOSAMPLER(_Color3);
            UNITY_DECLARE_TEX2D_NOSAMPLER(_Normal3);
            UNITY_DECLARE_TEX2D_NOSAMPLER(_SAH3);

            UNITY_DECLARE_TEX2D_NOSAMPLER(_Color4);
            UNITY_DECLARE_TEX2D_NOSAMPLER(_Normal4);
            UNITY_DECLARE_TEX2D_NOSAMPLER(_SAH4);

#if defined(DOTS_INSTANCING_ON)
            UNITY_DOTS_INSTANCING_START(UserPropertyMetadata)
                //UNITY_DOTS_INSTANCED_PROP_OVERRIDE_REQUIRED(float4, tg_CommonInstanceData)
            UNITY_DOTS_INSTANCING_END(UserPropertyMetadata)
#endif

            v2f vert (appdata v)
            { 
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o); 

                float3 positionOS = v.vertex;
                float3 normalOS = v.normal;
                float4 tangentOS = v.tangent;
                
                VertexPositionInputs vertInputs = GetVertexPositionInputs(positionOS);    //This function calculates all the relative spaces of the objects vertices
                o.vertex = vertInputs.positionCS;
                o.positionWS_fog.xyz = vertInputs.positionWS;

                o.uv = v.uv;
                o.positionWS_fog.w = ComputeFogFactor(o.vertex.z);

                float4 instanceData = tg_InstanceData;
                o.instanceData = instanceData;

                VertexNormalInputs normalInputs = GetVertexNormalInputs(normalOS, tangentOS);

                o.normalWS = normalInputs.normalWS;
                o.tangentWS = float4(normalInputs.tangentWS, tangentOS.w);
                o.bitangentWS = normalInputs.bitangentWS;

#if LIGHTMAP_ON
                TG_TRANSFORM_LIGHTMAP_TEX(v.lightmapUV, o.lightmapUV)
                //o.color.rgb = 0;
#endif

                //o.color.a = 1;
                
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                half4 splat = tex2D(_SplatMap, i.uv);

                half2 layer1_uv = i.uv * _Layers_Tiling.x;
                half2 layer2_uv = i.uv * _Layers_Tiling.y;
                half2 layer3_uv = i.uv * _Layers_Tiling.z;
                half2 layer4_uv = i.uv * _Layers_Tiling.w;

                //layer3_uv += half2(0, i.positionWS_fog.x * 0.02);
                
                half4 diffuse = UNITY_SAMPLE_TEX2D_SAMPLER(_Color1, _Color1, layer1_uv) * splat.x;

                half4 color2 = UNITY_SAMPLE_TEX2D_SAMPLER(_Color2, _Color1, layer2_uv); 
                diffuse += color2 * splat.y;
                diffuse += UNITY_SAMPLE_TEX2D_SAMPLER(_Color3, _Color1, layer3_uv) * splat.z;
                diffuse += UNITY_SAMPLE_TEX2D_SAMPLER(_Color4, _Color1, layer4_uv) * splat.w;

            	
            	half3 normalTS = UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_Normal1, _Normal1, layer1_uv)) * splat.r;
                half3 normal2 = UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_Normal2, _Normal1, layer2_uv)); 
                normalTS += normal2 * splat.g;
                normalTS += UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_Normal3, _Normal1, layer3_uv)) * splat.b;
                normalTS += UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_Normal4, _Normal1, layer4_uv)) * splat.w;

                normalTS = normalize(normalTS);

                half3x3 tangentToWorld = half3x3(i.tangentWS.xyz, i.bitangentWS.xyz, i.normalWS.xyz); 

                half3 normalWS = TransformTangentToWorld(normalTS.xyz, tangentToWorld, true);

                half3 ambientLight = ARENA_COMPUTE_AMBIENT_LIGHT(i, normalWS);
                
                //diffuse.rgb = 1;
                #if defined(UG_QUALITY_MED) || defined(UG_QUALITY_HIGH)
                half4 Sm_AO_HGHT = UNITY_SAMPLE_TEX2D_SAMPLER(_SAH1, _SAH1, layer1_uv) * splat.r;
                half4 Sm_AO_HGHT_2 = UNITY_SAMPLE_TEX2D_SAMPLER(_SAH2, _SAH1, layer2_uv);
                Sm_AO_HGHT += Sm_AO_HGHT_2 * splat.g;
                Sm_AO_HGHT += UNITY_SAMPLE_TEX2D_SAMPLER(_SAH3, _SAH1, layer3_uv) * splat.b;
                Sm_AO_HGHT += UNITY_SAMPLE_TEX2D_SAMPLER(_SAH4, _SAH1, layer4_uv) * splat.w;

                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.positionWS_fog.xyz);
                half roughness = 1.0 - Sm_AO_HGHT.r;
                half3 envMapColor = TG_ReflectionProbe_half(viewDirWS, normalWS, i.instanceData.y,roughness * 4);
                envMapColor.rgb *= Sm_AO_HGHT.g;

                half3 remEnvMapColor = clamp(envMapColor - 0.5, 0, 10);
                remEnvMapColor = remEnvMapColor * _HighlightRemove;
                remEnvMapColor = envMapColor - remEnvMapColor;

                half lum = tg_luminance(ambientLight);

                envMapColor = lerp(remEnvMapColor, envMapColor, saturate(lum * lum * lum));

                ARENA_DYN_LIGHT(normalWS, i.positionWS_fog.xyz, ambientLight, viewDirWS, envMapColor, true);
                
                half4 finalColor = LightingPBR(diffuse, ambientLight, viewDirWS, normalWS, 0, roughness, envMapColor);
                #else
                half4 finalColor = diffuse;
                ARENA_DYN_LIGHT(normalWS, i.positionWS_fog.xyz, ambientLight, 0,0, true);
                finalColor.rgb *= ambientLight;
                #endif

                // water height
                //finalColor.rgb = lerp(finalColor.rgb, _UnderwaterColor.rgb, saturate(_WaterHeight - i.positionWS_fog.y));
                
                // apply fog
                return half4(MixFog(finalColor.rgb, i.positionWS_fog.w), finalColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }
            
            Cull Off
            HLSLPROGRAM

            #pragma target 2.0
            
            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMetaCustom
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _SPECGLOSSMAP
            #pragma shader_feature EDITOR_VISUALIZATION

            #include "HLSLSupport.cginc"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" 
            
             
            CBUFFER_START(UnityPerMaterial)
                half4 _Layers_Tiling;
                half _HighlightRemove;
            CBUFFER_END

            half4 _BaseColor;
            half4 _BaseMap_ST;
            half4 _EmissionColor; 

            sampler2D _BaseMap;
            sampler2D _SplatMap;
            UNITY_DECLARE_TEX2D(_Color1);
            UNITY_DECLARE_TEX2D_NOSAMPLER(_Color2);
            UNITY_DECLARE_TEX2D_NOSAMPLER(_Color3); 

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl"
           
            
            half4 UniversalFragmentMetaCustom(Varyings fragIn) : SV_Target
            {
                MetaInput metaInput;

                half4 splat = tex2D(_SplatMap, fragIn.uv);

                half2 layer1_uv = fragIn.uv * _Layers_Tiling.x;
                half2 layer2_uv = fragIn.uv * _Layers_Tiling.y;
                half2 layer3_uv = fragIn.uv * _Layers_Tiling.z;

                //layer3_uv += half2(0, i.positionWS_fog.x * 0.02);
                
                half4 diffuse = UNITY_SAMPLE_TEX2D_SAMPLER(_Color1, _Color1, layer1_uv) * splat.x;

                half4 color2 = UNITY_SAMPLE_TEX2D_SAMPLER(_Color2, _Color1, layer2_uv); 
                diffuse += color2 * splat.y;
                diffuse += UNITY_SAMPLE_TEX2D_SAMPLER(_Color3, _Color1, layer3_uv) * splat.z;
                            
                metaInput.Albedo = diffuse.rgb;
                metaInput.Emission = _EmissionColor.rgb;
                            
                #ifdef EDITOR_VISUALIZATION
                metaInput.VizUV = fragIn.VizUV;
                metaInput.LightCoord = fragIn.LightCoord;
                #endif

                half4 result = UnityMetaFragment(metaInput);
                return result;
            }
            
            ENDHLSL
        }
    }
}
