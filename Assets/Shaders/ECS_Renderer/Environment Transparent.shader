Shader "Arena/Environment Transparent"
{
    Properties
    {
        [Toggle] _ZWrite("ZWrite", int) = 1
        //[Enum(Off,0,On,1)] _AlphaToMask("Alpha to Mask", Int) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Blend Source", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Blend Destination", float) = 0

        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", int) = 2

        [Toggle(USE_BASECOLOR_INSTANCE)] _UseBaseColorInstance("Use color instance", Float) = 0.0
        _BaseColor("Color tint", Color) = (1,1,1,1)
        _BaseMap ("Main color", 2D) = "white" {}
        _BumpMap ("Normal map", 2D) = "bump" {}
        _MetallicGlossMap("Metallic/Smoothness map", 2D) = "white" {}
        _Metallic("Metallic", Range(0,1)) = 1.0
    	_Smoothness ("Smoothness", Range(0,1)) = 0.5
        _HighlightRemove("Highlight remove", Float) = 0
        //[Toggle(USE_SCALE_REFLECTIONS)] _ScaleReflections("Scale reflections", Float) = 0.0
        [HDR] _EmissionColor("Emission color", Color) = (0,0,0)
        [Toggle(USE_SPECULAR_MULT)] _UseSpecularMult("Use specular multiplier", Float) = 0.0
        _SpecularMultiplier("Specular multiplier", Float) = 1.0
        
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
        
        // чтобы не ругался батчер
        [HideInInspector] _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
    }
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        LOD 100
        
        Pass
        {   
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "DGXForward"
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
            #pragma vertex env_vert
            #pragma fragment env_frag
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile _ DOTS_INSTANCING_ON
            //#pragma shader_feature TG_USE_ALPHACLIP
			#pragma multi_compile UG_QUALITY_LOW UG_QUALITY_MED UG_QUALITY_HIGH
            //#pragma shader_feature USE_UNDERWATER
            //#pragma shader_feature DIFFUSE_ALPHA_AS_SMOOTHNESS
            //#pragma shader_feature USE_SURFACE_BLEND
            #pragma shader_feature_local_fragment USE_BASECOLOR_INSTANCE
            #pragma multi_compile_local_fragment USE_SPECULAR_MULT
            
            //#pragma multi_compile_fragment __ ARENA_USE_MAIN_LIGHT
            //#pragma multi_compile_fragment __ ARENA_USE_ADD_LIGHT
            #pragma multi_compile_fragment _ DGX_DARK_MODE
            
            #pragma multi_compile_fragment _ DGX_SPOT_LIGHTS
            
            //#pragma multi_compile_fwdbase
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ DIRLIGHTMAP_COMBINED

            #define TG_TRANSPARENT

            #include "Input-Env.hlsl"
            #include "Common-Env.hlsl"

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
//            #include "Input-Env.hlsl"
//            #include "Packages/com.tzargames.rendering/Shaders/MetaPass.hlsl"
//            
//            ENDHLSL
//        }
    }
}
