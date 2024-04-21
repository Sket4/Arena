Shader "Arena/Environment"
{
    Properties
    {
        [Toggle(DIFFUSE_ALPHA_AS_SMOOTHNESS)] _UseAlphaAsSmoothness("Use diffuse alpha as smoothess", int) = 0
        [Toggle] _ZWrite("ZWrite", int) = 1
        [Toggle(TG_USE_ALPHACLIP)] _AlphaClip("Use alpha clipping", float) = 0.0
        //[Enum(Off,0,On,1)] _AlphaToMask("Alpha to Mask", Int) = 0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Blend Source", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Blend Destination", float) = 0

        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", int) = 2

        _BaseColor("Color tint", Color) = (1,1,1,1)
        _BaseMap ("Main color", 2D) = "white" {}
        _BumpMap ("Normal map", 2D) = "bump" {}
        _MetallicGlossMap("Metallic/Smoothness map", 2D) = "white" {}
        _Metallic("Metallic", Range(0,1)) = 1.0
    	_Smoothness ("Smoothness", Range(0,1)) = 0.5
        _HighlightRemove("Highlight remove", Float) = 0
        [HDR] _EmissionColor("Emission color", Color) = (0,0,0)
        
        [Toggle(USE_UNDERWATER)]
        _UseUnderwater("Underwater", Float) = 0
        _Underwater_color("Underwater color", Color) = (1,1,1,1)
        _Underwater_fog_density_factor("Underwater fog density factor", Float) = 1
        _Underwater_fog_height_mult("Underwater fog height mult", Float) = 1
        
        [Toggle(USE_SURFACE_BLEND)]
        _UseSurfaceBlend("Use surface blend", int) = 0.0
        _SurfaceMap("Surface", 2D) = "white" {}
        _SurfaceBlendFactor("Blend factor", Float) = 1
        
        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {} 
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="TransparentCutout" 
            //"RenderType" = "Opaque"
        }
        LOD 100
        
        Pass
        {   
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            //AlphaToMask[_AlphaToMask]
            Blend[_SrcBlend][_DstBlend]
            Cull[_Cull]
            ZWrite[_ZWrite]

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
            #pragma shader_feature __ TG_TRANSPARENT
            #pragma shader_feature TG_USE_ALPHACLIP
            #pragma shader_feature USE_UNDERWATER
            #pragma shader_feature DIFFUSE_ALPHA_AS_SMOOTHNESS
            #pragma shader_feature USE_SURFACE_BLEND
            //#pragma multi_compile_fwdbase
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED

            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.tzargames.rendering/Shaders/Lighting.hlsl"
            
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
#if LIGHTMAP_ON
                TG_DECLARE_LIGHTMAP_UV(1)
#endif

                #if USE_UNDERWATER
                half4 color : COLOR;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                half4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                nointerpolation half4 instanceData : TEXCOORD1;

                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float4 positionWS_fog : TEXCOORD5;

                half4 color : TEXCOORD6;
#if LIGHTMAP_ON
                TG_DECLARE_LIGHTMAP_UV(7)
#endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _SurfaceMap_ST;
                half _SurfaceBlendFactor;
                half4 _Underwater_color;
                half4 _EmissionColor;
				half _Metallic;
				half _Smoothness;
                half _Cutoff;
                half _HighlightRemove;
            CBUFFER_END

            sampler2D _BaseMap;
            sampler2D _SurfaceMap;
            sampler2D _BumpMap;
            sampler2D _MetallicGlossMap;

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

                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.positionWS_fog.w = ComputeFogFactor(o.vertex.z);

                float4 instanceData = tg_InstanceData;
                o.instanceData = instanceData;

                VertexNormalInputs normalInputs = GetVertexNormalInputs(normalOS, tangentOS);

                o.normalWS = normalInputs.normalWS;
                o.tangentWS = float4(normalInputs.tangentWS, tangentOS.w);
                o.bitangentWS = normalInputs.bitangentWS;

#if LIGHTMAP_ON
                TG_TRANSFORM_LIGHTMAP_TEX(v.lightmapUV, o.lightmapUV)
                o.color.rgb = 0;
#endif

                #if USE_UNDERWATER
                o.color.a = v.color.r;
                #else
                o.color.a = 1;
                #endif
                
                
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                
            	half4 diffuse = tex2D(_BaseMap, i.uv) * _BaseColor;

#if defined(TG_USE_ALPHACLIP)
                clip(diffuse.a - _Cutoff);
#endif
            	
            	half3 normalTS = UnpackNormal(tex2D(_BumpMap, i.uv));
                //normalTS.xy *= 2;
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.positionWS_fog.xyz);

                half3x3 tangentToWorld = half3x3(i.tangentWS.xyz, i.bitangentWS.xyz, i.normalWS.xyz); 

                half3 normalWS = TransformTangentToWorld(normalTS.xyz, tangentToWorld, true);

                half3 ambientLight;

#if LIGHTMAP_ON
                ambientLight = TG_SAMPLE_LIGHTMAP(i.lightmapUV, i.instanceData.x, normalWS);
#else
                ambientLight = TG_ComputeAmbientLight_half(normalWS);
#endif

                half4 mesm = tex2D(_MetallicGlossMap, i.uv);
                mesm.rgb *= _Metallic;
                
                #if DIFFUSE_ALPHA_AS_SMOOTHNESS
                half roughness = 1.0f - (diffuse.a * _Smoothness);
                
                #else
                half smoothness = mesm.a * _Smoothness;
                half roughness = 1 - smoothness;
                #endif
                
                

                half3 envMapColor = TG_ReflectionProbe_half(viewDirWS, normalWS, i.instanceData.y,roughness * 4);

                half3 remEnvMapColor = clamp(envMapColor - 0.5, 0, 10);
                remEnvMapColor = remEnvMapColor * _HighlightRemove;
                remEnvMapColor = envMapColor - remEnvMapColor;

                half lum = tg_luminance(ambientLight);

                envMapColor = lerp(remEnvMapColor, envMapColor, saturate(lum * lum * lum));

                #if USE_SURFACE_BLEND
                float2 surfaceUV = TRANSFORM_TEX(i.positionWS_fog.xz, _SurfaceMap);
                half4 surfaceColor = tex2D(_SurfaceMap, surfaceUV);
                float surfaceBlend = saturate(dot(half3(normalWS.x, normalWS.y, normalWS.z), half3(0.0,1,0.0)));

                // pow 4
                surfaceBlend *= surfaceBlend;
                surfaceBlend  *= surfaceBlend;
                
                diffuse.rgb = lerp(diffuse.rgb, surfaceColor.rgb, surfaceBlend  * _SurfaceBlendFactor);
                #endif

                //diffuse.rgb = 1;
                half4 finalColor = LightingPBR(diffuse, ambientLight, viewDirWS, normalWS, mesm.rrr, roughness, envMapColor);

                #if USE_UNDERWATER
                finalColor.rgb = lerp(finalColor.rgb, _Underwater_color * ambientLight, i.color.a);
                #endif
                
                
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
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" 
            
            
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _SurfaceMap_ST;
                half _SurfaceBlendFactor;
                half4 _Underwater_color;
                half4 _EmissionColor;
				half _Metallic;
				half _Smoothness;
                half _Cutoff;
                half _HighlightRemove;
            CBUFFER_END

            #include "Packages/com.tzargames.rendering/Shaders/MetaPass.hlsl"
            
            ENDHLSL
        }
    }
}
