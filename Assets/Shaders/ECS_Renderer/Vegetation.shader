Shader "Arena/Vegetation"
{
    Properties
    {
        [Toggle(DEBUG_VERTEX_COLOR)]
        _DebugVertexColor("Debug vertex color", float) = 0.0
        
        [Toggle] _ZWrite("ZWrite", int) = 1
        [Toggle(TG_USE_ALPHACLIP)] _AlphaClip("Use alpha clipping", float) = 0.0
        //[Enum(Off,0,On,1)] _AlphaToMask("Alpha to Mask", Int) = 0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Blend Source", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Blend Destination", float) = 0

        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", int) = 2

        _BaseColor("Color tint", Color) = (1,1,1,1)
        _BaseColorMult("Base color mult", Float) = 1
        _BaseMap ("Main color", 2D) = "white" {}
        _BumpMap ("Normal map", 2D) = "bump" {}
        _MetallicGlossMap("Metallic/Smoothness map", 2D) = "white" {}
        _Metallic("Metallic", Range(0,1)) = 1.0
    	_Smoothness ("Smoothness", Range(0,1)) = 0.5
        _AOAdd("AO add", Float) = 0
        _WindForce("Wind force", Float) = 1
        
        [HideInInspector]_EmissionColor("Emission color", Color) = (1,1,1,1)
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {} 
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {} 
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" }
        LOD 100
        
        Pass
        {
            //AlphaToMask[_AlphaToMask]
            Blend[_SrcBlend][_DstBlend]
            Cull[_Cull]
            ZWrite[_ZWrite]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma require cubearray
            #pragma require 2darray
            #pragma exclude_renderers gles //excluded shader from OpenGL ES 2.0 because it uses non-square matrices
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma shader_feature __ USE_LIGHTING
            #pragma shader_feature __ DEBUG_VERTEX_COLOR
            #pragma shader_feature __ TG_TRANSPARENT
            #pragma shader_feature TG_USE_ALPHACLIP
            #pragma multi_compile _ LIGHTMAP_ON
            

            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.tzargames.rendering/Shaders/Lighting.hlsl"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
#if LIGHTMAP_ON
                TG_DECLARE_LIGHTMAP_UV(1)
#endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                half4 vertex : SV_POSITION;
                half2 uv : TEXCOORD0;
                nointerpolation half4 instanceData : TEXCOORD1;

                half4 normalWS_occl : TEXCOORD3;
                half4 tangentWS : TEXCOORD4;
                half3 bitangentWS : TEXCOORD5;
                float4 positionWS_fog : TEXCOORD6;

#if LIGHTMAP_ON
                TG_DECLARE_LIGHTMAP_UV(7)
#endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _Underwater_color;
                half _BaseColorMult;
				half _Metallic;
				half _Smoothness;
                half _Cutoff;
                half4 _EmissionColor;
                half _WindForce;
                half _AOAdd;
            CBUFFER_END

            sampler2D _BaseMap;
            sampler2D _BumpMap;
            sampler2D _MetallicGlossMap;

#if defined(DOTS_INSTANCING_ON)
            UNITY_DOTS_INSTANCING_START(UserPropertyMetadata)
            UNITY_DOTS_INSTANCING_END(UserPropertyMetadata)
#endif

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float3 positionOS = v.vertex;

                float4 time = _Time;
                half wind = (sin(time.z + (positionOS.x + positionOS.z) * 2) + sin(time.y) + sin(time.w)) * 0.05 * v.color.x;
                wind *= _WindForce;
                positionOS.x += wind;
                positionOS.z += wind;
                
                float3 normalOS = v.normal;
                float4 tangentOS = v.tangent;
                
                VertexPositionInputs vertInputs = GetVertexPositionInputs(positionOS);    //This function calculates all the relative spaces of the objects vertices
                o.vertex = vertInputs.positionCS;
                o.positionWS_fog.xyz = vertInputs.positionWS;

                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.positionWS_fog.a = ComputeFogFactor(o.vertex.z);

                float4 instanceData = tg_InstanceData;
                o.instanceData = instanceData;

                
                
                VertexNormalInputs normalInputs = GetVertexNormalInputs(normalOS, tangentOS);

                o.normalWS_occl.xyz = normalInputs.normalWS;
                o.normalWS_occl.w = saturate(v.color.x + _AOAdd);
                o.tangentWS = float4(normalInputs.tangentWS, tangentOS.w);
                o.bitangentWS = normalInputs.bitangentWS;

#if LIGHTMAP_ON
                TG_TRANSFORM_LIGHTMAP_TEX(v.lightmapUV, o.lightmapUV)
#endif

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                
                #if DEBUG_VERTEX_COLOR
                return i.normalWS_occl.w;
                #endif
                
            	half4 diffuse = tex2D(_BaseMap, i.uv);
                diffuse.rgb *= _BaseColor.rgb * _BaseColorMult;

#if defined(TG_USE_ALPHACLIP)
                clip(diffuse.a - _Cutoff);
#endif
            	
            	half3 normalTS = UnpackNormal(tex2D(_BumpMap, i.uv));
                //normalTS.xy *= 2;
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.positionWS_fog.xyz);

                half3x3 tangentToWorld = half3x3(i.tangentWS.xyz, i.bitangentWS.xyz, i.normalWS_occl.xyz); 

                float3 normalWS = TransformTangentToWorld(normalTS.xyz, tangentToWorld, true);

                real3 ambientLight;

                half ao =  i.normalWS_occl.w;
                
#if LIGHTMAP_ON
                ambientLight = TG_SAMPLE_LIGHTMAP(i.lightmapUV, i.instanceData.x, normalWS);
#else
                ambientLight = TG_ComputeAmbientLight_half(normalWS);
                ambientLight *= ao;
#endif

                half4 mesm = tex2D(_MetallicGlossMap, i.uv);
                mesm.rgb *= _Metallic;

                
                half lum = tg_luminance(ambientLight);
                //return lum;

                half smoothness = mesm.a * _Smoothness * lum;
                half roughness = 1 - smoothness;

                half3 envMapColor = TG_ReflectionProbe(viewDirWS, normalWS, i.instanceData.y, roughness * 4);
                envMapColor *= ao;
                half4 finalColor = LightingPBR(diffuse, ambientLight, viewDirWS, normalWS, mesm.rgb, roughness, envMapColor);
                
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
                half4 _Underwater_color;
                half _BaseColorMult;
				half _Metallic;
				half _Smoothness;
                half _Cutoff;
                half4 _EmissionColor;
                half _WindForce;
                half _AOAdd;
            CBUFFER_END

            #include "Packages/com.tzargames.rendering/Shaders/MetaPass.hlsl"
            
            ENDHLSL
        }
    }
}
