Shader"Arena/Window"
{
    Properties
    {
        [Toggle(USE_LIGHTING)]
        _UseLighting("Use lighting", float) = 1
        
        _BaseColor("Base color", Color) = (1,1,1,1)
        _Color2("Color 2", Color) = (1,1,1,1)
        _BaseMap ("Mask", 2D) = "white" {}
        _BumpMap ("Normal map", 2D) = "bump" {}
        [HDR] _EmissionColor("Emission color", Color) = (0,0,0)

        _Roughness("Roughness", Range(0,1)) = 1
        _Metallic("Metallic", Range(0,1)) = 1
        
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {} 
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass 
        {
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
            #pragma multi_compile _ LIGHTMAP_ON
            //#pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl" 
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.tzargames.renderer/Shaders/Lighting.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 normal : NORMAL;
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
                float4 positionWS_fog : TEXCOORD6;
#if LIGHTMAP_ON
                TG_DECLARE_LIGHTMAP_UV(7)
#else
                half3 color : TEXCOORD7;
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

#if defined(DOTS_INSTANCING_ON)
                UNITY_DOTS_INSTANCING_START(UserPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float4, tg_CommonInstanceData)
                UNITY_DOTS_INSTANCING_END(UserPropertyMetadata)
#endif
            
            sampler2D _BaseMap;
            sampler2D _BumpMap;
            
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);

                float3 positionOS = v.vertex;
                float3 normalOS = v.normal;
                float4 tangentOS = v.tangent;

                VertexPositionInputs vertInputs = GetVertexPositionInputs(positionOS);    //This function calculates all the relative spaces of the objects vertices
                o.vertex = vertInputs.positionCS;
                o.positionWS_fog.xyz = vertInputs.positionWS;

                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.positionWS_fog.w = ComputeFogFactor(o.vertex.z);

                float4 instanceData = tg_InstanceData;
                o.instanceData = instanceData.xy;
    
                VertexNormalInputs normalInputs = GetVertexNormalInputs(normalOS, tangentOS);

                o.normalWS = normalInputs.normalWS;
                o.tangentWS = float4(normalInputs.tangentWS, tangentOS.w);
                o.bitangentWS = normalInputs.bitangentWS;

#if LIGHTMAP_ON
                TG_TRANSFORM_LIGHTMAP_TEX(v.lightmapUV, o.lightmapUV)
#else
                half3 bakedGI_Color = SampleSH(normalInputs.normalWS);
                o.color.rgb = bakedGI_Color.rgb;
#endif
                
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                //UNITY_SETUP_INSTANCE_ID(i);
                half mask = tex2D(_BaseMap, i.uv).r;
                half4 diffuse = lerp(_BaseColor, _Color2, mask);
    
                half3 normalTS = UnpackNormal(tex2D(_BumpMap, i.uv));
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.positionWS_fog.xyz);

                half3x3 tangentToWorld = half3x3(i.tangentWS.xyz, i.bitangentWS.xyz, i.normalWS.xyz);

                float3 normalWS = TransformTangentToWorld(normalTS.xyz, tangentToWorld, true);

                half roughness = _Roughness * lerp(_BaseColor.a, _Color2.a, mask);

#if LIGHTMAP_ON
                half3 lighting = TG_SAMPLE_LIGHTMAP(i.lightmapUV, i.instanceData.x);
#else
                half3 lighting = i.color.rgb;
#endif

                half3 envMapColor = TG_ReflectionProbe(viewDirWS, normalWS, i.instanceData.y, roughness * 4);
                half4 finalColor = LightingPBR(diffuse, lighting, viewDirWS, normalWS, _Metallic, roughness, envMapColor);

                // apply fog
                return half4(MixFog(finalColor, i.positionWS_fog.w), finalColor.a);
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
                half4 _EmissionColor;
                half4 _Color2;
                half _Roughness;
                half _Metallic;
            CBUFFER_END

            #include "Packages/com.tzargames.renderer/Shaders/MetaPass.hlsl"
            
            ENDHLSL
        }
    }
}
