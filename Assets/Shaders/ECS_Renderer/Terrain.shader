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
            Name "GBuffer"
            Tags 
            { 
                "LightMode" = "gbuffer"
            }
            
            Stencil 
            {
                Ref 128
                Comp Always
                Pass Replace
                Fail Keep
                ZFail Keep
            }
            HLSLPROGRAM
            #pragma target 4.5
            #pragma exclude_renderers nomrt
            #pragma require 2darray
            #pragma require cubearray
            #pragma exclude_renderers gles //excluded shader from OpenGL ES 2.0 because it uses non-square matrices
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ DOTS_INSTANCING_ON
            
            #pragma multi_compile UG_QUALITY_LOW UG_QUALITY_MED UG_QUALITY_HIGH
            #pragma multi_compile __ ARENA_USE_MAIN_LIGHT
            #pragma multi_compile __ ARENA_USE_ADD_LIGHT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED

            #pragma shader_feature_local_fragment ARENA_SCALE_UV_X
            #pragma shader_feature_local_fragment ARENA_SCALE_UV_Y

            #if defined(UG_QUALITY_LOW)
            #undef DIRLIGHTMAP_COMBINED
            #endif

            #define ARENA_DEFERRED
            #define ARENA_USE_FOUR_CHANNEL
            
            #include "Terrain-Common.hlsl"
            
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
//            #include "HLSLSupport.cginc"
//            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" 
//            
//             
//            CBUFFER_START(UnityPerMaterial)
//                half4 _Layers_Tiling;
//                half _HighlightRemove;
//            CBUFFER_END
//
//            half4 _BaseColor;
//            half4 _BaseMap_ST;
//            half4 _EmissionColor; 
//
//            sampler2D _BaseMap;
//            sampler2D _SplatMap;
//            UNITY_DECLARE_TEX2D(_Color1);
//            UNITY_DECLARE_TEX2D_NOSAMPLER(_Color2);
//            UNITY_DECLARE_TEX2D_NOSAMPLER(_Color3); 
//
//            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl"
//           
//            
//            half4 UniversalFragmentMetaCustom(Varyings fragIn) : SV_Target
//            {
//                MetaInput metaInput;
//
//                half4 splat = tex2D(_SplatMap, fragIn.uv);
//
//                half2 layer1_uv = fragIn.uv * _Layers_Tiling.x;
//                half2 layer2_uv = fragIn.uv * _Layers_Tiling.y;
//                half2 layer3_uv = fragIn.uv * _Layers_Tiling.z;
//
//                //layer3_uv += half2(0, i.positionWS_fog.x * 0.02);
//                
//                half4 diffuse = UNITY_SAMPLE_TEX2D_SAMPLER(_Color1, _Color1, layer1_uv) * splat.x;
//
//                half4 color2 = UNITY_SAMPLE_TEX2D_SAMPLER(_Color2, _Color1, layer2_uv); 
//                diffuse += color2 * splat.y;
//                diffuse += UNITY_SAMPLE_TEX2D_SAMPLER(_Color3, _Color1, layer3_uv) * splat.z;
//                            
//                metaInput.Albedo = diffuse.rgb;
//                metaInput.Emission = _EmissionColor.rgb;
//                            
//                #ifdef EDITOR_VISUALIZATION
//                metaInput.VizUV = fragIn.VizUV;
//                metaInput.LightCoord = fragIn.LightCoord;
//                #endif
//
//                half4 result = UnityMetaFragment(metaInput);
//                return result;
//            }
//            
//            ENDHLSL
//        }
    }
}
