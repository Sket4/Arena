Shader "Arena/Terrain (for beach)"
{
    Properties
    {
        [Toggle(ARENA_SCALE_UV_X)] _ScaleUVX("Scale UV X", float) = 0.0
        [Toggle(ARENA_SCALE_UV_Y)] _ScaleUVY("Scale UV Y", float) = 0.0
        [NoScaleOffset] _SplatMap ("Splat map", 2D) = "white" {}
        [NoScaleOffset] _Color1("Color 1", 2D) = "white" {}
        [NoScaleOffset] _Normal1("Normal 1", 2D) = "white" {}
        [NoScaleOffset] _SAH1("Sm AO HGHT 1", 2D) = "white" {}
        
        [NoScaleOffset] _Color2("Sand color", 2D) = "white" {}
        [NoScaleOffset] _Normal2("Sand normal", 2D) = "white" {}
        [NoScaleOffset] _SAH2("Sand Sm AO HGHT", 2D) = "white" {}
        
        [NoScaleOffset] _Color3("Color 3", 2D) = "white" {}
        [NoScaleOffset] _Normal3("Normal 3", 2D) = "white" {}
        [NoScaleOffset] _SAH3("Sm AO HGHT 3", 2D) = "white" {}
        
        //_Layers_Roughness("Layers roughness", Vector) = (0.5,0.5,0.5,0.5)
        _Layers_Tiling("Layers tiling", Vector) = (1,1,1,1)
        
        _UnderwaterColor("Underwater color", Color) = (1,1,1,1)
        _WaterHeight("Water height", Float) = 5
        _WaterHeightFade("Water height fade", Float) = 1
        _HighlightRemove("Highlight remove", Float) = 0
        
        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {} 
    }
    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "DGX"
            "RenderType"="Opaque" 
        }
        LOD 100
        
//        Pass
//        {   
//            Name "ForwardLit"
//            Tags
//            {
//                "LightMode" = "UniversalForward"
//            }
//
//            HLSLPROGRAM
//            #pragma target 4.5
//            #pragma require 2darray
//            #pragma require cubearray
//            #pragma exclude_renderers gles //excluded shader from OpenGL ES 2.0 because it uses non-square matrices
//            #pragma vertex vert
//            #pragma fragment frag
//            // make fog work
//            #pragma multi_compile_fog
//            #pragma multi_compile _ DOTS_INSTANCING_ON
//            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
//            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
//            
//            #pragma shader_feature __ TG_TRANSPARENT
//            #pragma shader_feature TG_USE_ALPHACLIP
//            #pragma multi_compile UG_QUALITY_LOW UG_QUALITY_MED UG_QUALITY_HIGH
//            #pragma multi_compile __ ARENA_USE_MAIN_LIGHT
//            #pragma multi_compile __ ARENA_USE_ADD_LIGHT
//            #pragma shader_feature USE_UNDERWATER
//            #pragma shader_feature DIFFUSE_ALPHA_AS_SMOOTHNESS
//            #pragma shader_feature USE_SURFACE_BLEND
//            #pragma shader_feature_local_vertex ARENA_SCALE_UV_X
//            #pragma shader_feature_local_vertex ARENA_SCALE_UV_Y
//            //#pragma multi_compile_fwdbase
//            #pragma multi_compile _ LIGHTMAP_ON
//            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
//
//            #pragma multi_compile_fragment _ _SHADOWS_SOFT
//            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
//            #pragma multi_compile_fragment _ _LIGHT_COOKIES
//
//            #if defined(UG_QUALITY_LOW)
//            #undef DIRLIGHTMAP_COMBINED
//            #endif
//
//            #include "Terrain-Common.hlsl"
//
//            ENDHLSL
//        }

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
            #pragma exclude_renderers gles //excluded shader from OpenGL ES 2.0 because it uses non-square matrices
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ DOTS_INSTANCING_ON
            
            #pragma multi_compile UG_QUALITY_LOW UG_QUALITY_MED UG_QUALITY_HIGH
            #pragma multi_compile __ ARENA_USE_MAIN_LIGHT
            #pragma multi_compile __ ARENA_USE_ADD_LIGHT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile_fragment _ ARENA_MAP_RENDER

            #pragma shader_feature_local_vertex ARENA_SCALE_UV_X
            #pragma shader_feature_local_vertex ARENA_SCALE_UV_Y

            #if defined(UG_QUALITY_LOW)
            #undef DIRLIGHTMAP_COMBINED
            #endif

            #define ARENA_DEFERRED
            #define ARENA_UNDERWATER

            #if defined(UG_QUALITY_MED) || defined(UG_QUALITY_HIGH)
            #define DGX_PBR_RENDERING 1
            #endif

            
            #include "Terrain-Common.hlsl"
            
            ENDHLSL
        }

        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }
            
            Cull Off
            HLSLPROGRAM

            #pragma target 2.0
            
            #pragma vertex vert
            #pragma fragment FragmentMeta
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _SPECGLOSSMAP
            #pragma shader_feature EDITOR_VISUALIZATION

            #pragma shader_feature_local_vertex ARENA_SCALE_UV_X
            #pragma shader_feature_local_vertex ARENA_SCALE_UV_Y

            #define ARENA_META_PASS

            #include "Terrain-Common.hlsl" 
            
            ENDHLSL
        }
    }
}
