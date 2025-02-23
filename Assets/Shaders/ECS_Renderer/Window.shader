Shader"Arena/Window"
{
    Properties
    {
        _BaseColor("Base color", Color) = (1,1,1,1)
        _Color2("Color 2", Color) = (1,1,1,1)
        _BaseMap ("Mask", 2D) = "white" {}
        _BumpMap ("Normal map", 2D) = "bump" {}
        [HDR] _EmissionColor("Emission color", Color) = (0,0,0)

        _Roughness("Roughness", Range(0,1)) = 1
        _Metallic("Metallic", Range(0,1)) = 1
        
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {} 
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {} 
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass 
        {
            Tags
            {
                "LightMode" = "gbuffer" 
            }
//            Stencil 
//            {
//                Ref 128
//                Comp Always
//                Pass Replace
//                Fail Keep
//                ZFail Keep
//            }
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma require 2darray
            #pragma require cubearray
            #pragma exclude_renderers gles nomrt//excluded shader from OpenGL ES 2.0 because it uses non-square matrices
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            //#pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Common.hlsl"
            #include "Packages/com.dgx.srp/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.tzargames.rendering/Shaders/Lighting.hlsl"

            struct appdata
            {
                float3 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
                TG_DECLARE_LIGHTMAP_UV(1)
                
                //uint vid : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                half2 uv : TEXCOORD0;
                nointerpolation half2 instanceData : TEXCOORD1;
                half4 vertex : SV_POSITION;
                
                float3 normalWS : TEXCOORD3;
                float4 tangentWS : TEXCOORD4;
                float3 bitangentWS : TEXCOORD5;
                float3 positionWS : TEXCOORD6;
#if LIGHTMAP_ON
                TG_DECLARE_LIGHTMAP_UV(7)
#endif
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _EmissionColor;
                half4 _Color2;
                half _Roughness;
                half _Metallic;
            CBUFFER_END
            
            sampler2D _BaseMap;
            sampler2D _BumpMap;
            
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);

                float3 positionOS = v.vertex;
                float3 normalOS = v.normal;
                float4 tangentOS = v.tangent;

                o.positionWS.xyz = TransformObjectToWorld(positionOS);
                o.vertex = TransformWorldToHClip(o.positionWS.xyz);

                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);

                float4 instanceData = tg_InstanceData;
                o.instanceData = instanceData.xy;
    
                half sign = half(tangentOS.w);
                float3 normalWS = TransformObjectToWorldNormal(normalOS);
                half3 tangentWS = half3(TransformObjectToWorldDir(tangentOS.xyz));
                half3 bitangentWS = half3(cross(normalWS, float3(tangentWS))) * sign;

                o.normalWS = normalWS;
                o.tangentWS = float4(tangentWS, tangentOS.w);
                o.bitangentWS = bitangentWS;

#if LIGHTMAP_ON
                TG_TRANSFORM_LIGHTMAP_TEX(v.lightmapUV, o.lightmapUV)
#endif
                
                return o;
            }

            GBufferFragmentOutput frag (v2f i)
            {
                //UNITY_SETUP_INSTANCE_ID(i);
                half mask = tex2D(_BaseMap, i.uv).r;
                half4 diffuse = lerp(_BaseColor, _Color2, mask);
    
                half3 normalTS = UnpackNormal(tex2D(_BumpMap, i.uv));
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.positionWS.xyz);

                half3x3 tangentToWorld = half3x3(i.tangentWS.xyz, i.bitangentWS.xyz, i.normalWS.xyz);

                float3 normalWS = TransformTangentToWorld(normalTS.xyz, tangentToWorld, true);

                half roughness = _Roughness * lerp(_BaseColor.a, _Color2.a, mask);

                half3 lighting = ARENA_COMPUTE_AMBIENT_LIGHT(i, normalWS);

                SurfaceHalf surface;
                surface.Albedo = diffuse.rgb;
                surface.Alpha = diffuse.a;
                surface.Metallic = _Metallic;
                surface.Roughness = roughness;
                surface.NormalWS = normalWS;
                surface.EnvCubemapIndex = i.instanceData.y;
                surface.AmbientLight = lighting;

                return SurfaceToGBufferOutputHalf(surface);
            }
            ENDHLSL
        }

//        Pass
//        {
//            Name "Meta"
//            Tags { "LightMode" = "Meta" }
//            
//            Cull Off
//            HLSLPROGRAM
//
//            #pragma target 2.0
//            
//            #pragma vertex UniversalVertexMeta
//            #pragma fragment UniversalFragmentMetaCustom
//            #pragma shader_feature_local_fragment _SPECULAR_SETUP
//            #pragma shader_feature_local_fragment _EMISSION
//            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
//            #pragma shader_feature_local_fragment _ALPHATEST_ON
//            #pragma shader_feature_local_fragment _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
//            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
//            #pragma shader_feature_local_fragment _SPECGLOSSMAP
//            #pragma shader_feature EDITOR_VISUALIZATION
//            
//            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//            
//            CBUFFER_START(UnityPerMaterial)
//                half4 _BaseMap_ST;
//                half4 _BaseColor;
//                half4 _EmissionColor;
//                half4 _Color2;
//                half _Roughness;
//                half _Metallic;
//            CBUFFER_END
//
//            #include "Packages/com.tzargames.rendering/Shaders/MetaPass.hlsl"
//            
//            ENDHLSL
//        }
    }
}
