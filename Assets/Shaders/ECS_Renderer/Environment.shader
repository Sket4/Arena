Shader "Arena/Environment"
{
    Properties
    {
        [Toggle(USE_LIGHTING)]
        _UseLighting("Use lighting", float) = 1.0
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
        
        [Toggle(USE_UNDERWATER)]
        _UseUnderwater("Underwater", Float) = 0
        _Underwater_color("Underwater color", Color) = (1,1,1,1)
        _Underwater_fog_density_factor("Underwater fog density factor", Float) = 1
        _Underwater_fog_height_mult("Underwater fog height mult", Float) = 1

        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
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
            #pragma exclude_renderers gles //excluded shader from OpenGL ES 2.0 because it uses non-square matrices
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma shader_feature __ USE_LIGHTING
            #pragma shader_feature __ TG_TRANSPARENT
            #pragma shader_feature TG_USE_ALPHACLIP
            #pragma shader_feature USE_UNDERWATER
            #pragma multi_compile _ LIGHTMAP_ON
            //#pragma require 2darray

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.tzargames.renderer/Shaders/Lighting.hlsl"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
#if LIGHTMAP_ON
                float2 uv2 : TEXCOORD1;
#endif

                #if USE_UNDERWATER
                half4 color : COLOR;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                half4 vertex : SV_POSITION;
                half2 uv : TEXCOORD0;
                half fogCoords : TEXCOORD1;

                float3 normalWS : TEXCOORD3;
                float4 tangentWS : TEXCOORD4;
                float3 bitangentWS : TEXCOORD5;
                float3 positionWS : TEXCOORD6;

                half4 color : TEXCOORD7;
#if LIGHTMAP_ON
                float3 uv2 : TEXCOORD8;
#endif
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _Underwater_color;
				half _Metallic;
				half _Smoothness;
                half _Cutoff;
            CBUFFER_END

            sampler2D _BaseMap;
            sampler2D _BumpMap;
            sampler2D _MetallicGlossMap;

#if defined(DOTS_INSTANCING_ON)
            //UNITY_DOTS_INSTANCING_START(UserPropertyMetadata)
                //UNITY_DOTS_INSTANCED_PROP(uint, unity_LightmapIndex)
                //UNITY_DOTS_INSTANCED_PROP(float4, unity_LightmapST)
            //UNITY_DOTS_INSTANCING_END(UserPropertyMetadata)

            UNITY_DOTS_INSTANCING_START(BuiltinPropertyMetadata)
            UNITY_DOTS_INSTANCING_END(BuiltinPropertyMetadata)
#endif

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);

                float3 positionOS = v.vertex;
                float3 normalOS = v.normal;
                float4 tangentOS = v.tangent;
                
                VertexPositionInputs vertInputs = GetVertexPositionInputs(positionOS);    //This function calculates all the relative spaces of the objects vertices
                o.vertex = vertInputs.positionCS;
                o.positionWS = vertInputs.positionWS;

                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fogCoords = ComputeFogFactor(o.vertex.z);

#if LIGHTMAP_ON
                float4 lightmapST = unity_LightmapST;
                o.uv2.xy = v.uv2.xy * lightmapST.xy + lightmapST.zw;
                //o.uv2.z = unity_LightmapIndex;
#endif

                VertexNormalInputs normalInputs = GetVertexNormalInputs(normalOS, tangentOS);

                o.normalWS = normalInputs.normalWS;
                o.tangentWS = float4(normalInputs.tangentWS, tangentOS.w);
                o.bitangentWS = normalInputs.bitangentWS;
                
#if !LIGHTMAP_ON
                half3 bakedGI_Color = SampleSH(normalInputs.normalWS);//SHADERGRAPH_BAKED_GI(vertInputs.positionWS, normalInputs.normalWS, half2(0, 0), half2(0, 0), true);
                o.color.rgb = bakedGI_Color.rgb;
#else
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
            	half4 diffuse = tex2D(_BaseMap, i.uv) * _BaseColor;

#if defined(TG_USE_ALPHACLIP)
                clip(diffuse.a - _Cutoff);
#endif
            	
            	half3 normalTS = UnpackNormal(tex2D(_BumpMap, i.uv));
                //normalTS.xy *= 2;
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.positionWS);

                half3x3 tangentToWorld = half3x3(i.tangentWS.xyz, i.bitangentWS.xyz, i.normalWS.xyz); 

                float3 normalWS = TransformTangentToWorld(normalTS.xyz, tangentToWorld, true);

//#if defined(UNITY_DOTS_INSTANCING_ENABLED)
                //return half4(unity_LightmapIndex.x, 0,0,1);
//#endif

                half3 ambientLight;

#if LIGHTMAP_ON
                ambientLight = SampleLightmap(i.uv2.xy, float2(0, 0), normalWS);
#else
                ambientLight = i.color.rgb;
#endif

                half4 mesm = tex2D(_MetallicGlossMap, i.uv);
                mesm.rgb *= _Metallic;

                half smoothness = mesm.a * _Smoothness;
                half roughness = 1 - smoothness;

                half4 finalColor = LightingPBR(diffuse, ambientLight, viewDirWS, normalWS, mesm.rgb, roughness);

                #if USE_UNDERWATER
                finalColor.rgb = lerp(finalColor.rgb, _Underwater_color, i.color.a);
                #endif
                
                
                // apply fog
                return half4(MixFog(finalColor.rgb, i.fogCoords), finalColor.a);
            }
            ENDHLSL
        }
    }
}
