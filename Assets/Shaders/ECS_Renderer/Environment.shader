Shader "Arena/Environment"
{
    Properties
    {
        [Toggle] _ZWrite("ZWrite", int) = 1
        [Toggle(TG_USE_ALPHACLIP)] _AlphaClip("Use alpha clipping", float) = 0.0
        //[Enum(Off,0,On,1)] _AlphaToMask("Alpha to Mask", Int) = 0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

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
            "Queue"="Geometry"
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
            //AlphaToMask[_AlphaToMask]
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
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            
            #pragma shader_feature TG_USE_ALPHACLIP
			#pragma multi_compile UG_QUALITY_LOW UG_QUALITY_MED UG_QUALITY_HIGH
            #pragma shader_feature USE_UNDERWATER
            #pragma shader_feature DIFFUSE_ALPHA_AS_SMOOTHNESS
            #pragma shader_feature USE_SURFACE_BLEND
            //#pragma multi_compile_fwdbase
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED

            #if defined(UG_QUALITY_LOW)
            #undef DIRLIGHTMAP_COMBINED
            #endif
            
            #include "Input-Env.hlsl"
            #include "Common-Env.hlsl"

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

            #include "Input-Env.hlsl"
            #include "Packages/com.tzargames.rendering/Shaders/MetaPass.hlsl"
            
            ENDHLSL
        }
        UsePass "Hidden/Arena/ShadowCaster/SHADOWCASTER"
    }
}
