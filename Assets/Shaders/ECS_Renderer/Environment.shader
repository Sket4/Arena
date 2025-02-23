Shader "Arena/Environment"
{
    Properties
    {
        [Toggle] _ZWrite("ZWrite", int) = 1
        [Toggle(TG_USE_ALPHACLIP)] _AlphaClip("Use alpha clipping", float) = 0.0
        //[Enum(Off,0,On,1)] _AlphaToMask("Alpha to Mask", Int) = 0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", int) = 2

        [Toggle(USE_BASECOLOR_INSTANCE)] _UseBaseColorInstance("Use color instance", Float) = 0.0
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
        
        [Toggle(USE_HEIGHT_FOG)]
        _UseHeightFog("Use height fog", int) = 0
        _FogHeight("Fog height", float) = 0.0
        _HeightFogFade("Height fog fade", float) = 0.005
        
        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {} 
    }
    SubShader
    {
        Tags 
        { 
            "Queue"="Geometry"
        }
        LOD 100
        
        Pass
        {
            Name "GBuffer"
            Tags 
            { 
                "LightMode" = "gbuffer"
            }
            
            Cull[_Cull]
            ZWrite[_ZWrite]
            
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
            #pragma exclude_renderers nomrt
            #pragma require 2darray
            #pragma exclude_renderers gles //excluded shader from OpenGL ES 2.0 because it uses non-square matrices
            #pragma vertex env_vert
            #pragma fragment env_frag_deferred
            
            #pragma multi_compile _ DOTS_INSTANCING_ON
            
            #pragma shader_feature_local TG_USE_ALPHACLIP
			#pragma multi_compile UG_QUALITY_LOW UG_QUALITY_MED UG_QUALITY_HIGH
            #pragma multi_compile_fragment _ DGX_DARK_MODE
            #pragma multi_compile_fragment _ ARENA_MAP_RENDER
            #pragma shader_feature_local USE_UNDERWATER
            #pragma shader_feature_local USE_SURFACE_BLEND
            #pragma shader_feature_local_fragment USE_BASECOLOR_INSTANCE
            
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            
            #if defined(UG_QUALITY_LOW)
            #undef DIRLIGHTMAP_COMBINED
            #endif

            #define ARENA_DEFERRED
            
            #include "Input-Env.hlsl"
            #include "Common-Env.hlsl"
            
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            // -------------------------------------
            // Render State Commands
            Cull Back
            ZTest LEqual
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.5
            #pragma exclude_renderers gles

            // -------------------------------------
            // Shader Stages
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            #pragma multi_compile _ DOTS_INSTANCING_ON
            //#pragma multi_compile_shadowcaster
            
            #include "Input-Env.hlsl"
            #include "ShadowCasterPass.hlsl" 
            ENDHLSL
        }
//        Pass
//        {
//            Name "ShadowCaster"
//            Tags
//            {
//                "LightMode" = "ShadowCaster"
//            }
//
//            // -------------------------------------
//            // Render State Commands
//            Cull Back
//            ZTest LEqual
//            ZWrite On
//            ColorMask 0
//
//            HLSLPROGRAM
//            #pragma target 4.5
//            #pragma exclude_renderers gles
//
//            // -------------------------------------
//            // Shader Stages
//            #pragma vertex ShadowPassVertex
//            #pragma fragment ShadowPassFragment
//
//            #pragma shader_feature_local _ALPHATEST_ON
//
//            //--------------------------------------
//            // GPU Instancing
//            #pragma multi_compile_instancing
//            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
//
//            // -------------------------------------
//            // Universal Pipeline keywords
//
//            // -------------------------------------
//            // Unity defined keywords
//            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
//
//            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
//            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
//
//            // -------------------------------------
//            // Includes
//            //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//            // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
//            
//            #include "Input-Env.hlsl"
//            #include "ShadowCasterPass.hlsl"
//            ENDHLSL
//        }

//        Pass
//        {
//            Name "DepthOnly"
//            Tags
//            {
//                "LightMode" = "DepthOnly"
//            }
//
//            // -------------------------------------
//            // Render State Commands
//            ZWrite On
//            ColorMask R
//
//            HLSLPROGRAM
//            #pragma target 4.5
//            #pragma require 2darray
//            #pragma require cubearray       // из-за использования tg_ReflectionProbes
//            #pragma exclude_renderers gles //excluded shader from OpenGL ES 2.0 because it uses non-square matrices
//            
//            #pragma multi_compile _ DOTS_INSTANCING_ON
//            #pragma shader_feature TG_USE_ALPHACLIP
//            
//            // -------------------------------------
//            // Shader Stages
//            #pragma vertex DepthOnlyVertex
//            #pragma fragment DepthOnlyFragment 
//
//            // -------------------------------------
//            // Includes
//            #include "Input-Env.hlsl"
//            #include "Common-Env.hlsl"
//            ENDHLSL
//        }
//        
        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }
            
            Cull Off
            HLSLPROGRAM

            #pragma target 2.0

            #define ARENA_META_PASS
            
            #pragma vertex env_vert
            #pragma fragment FragmentMeta
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature EDITOR_VISUALIZATION
           
            #include "Input-Env.hlsl"
            #include "Common-Env.hlsl"
            
            ENDHLSL
        }
    }
}
