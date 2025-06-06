Shader "Arena/Vegetation"
{
    Properties
    {
        [Toggle(DEBUG_VERTEX_COLOR)]
        _DebugVertexColor("Debug vertex color", float) = 0.0
    	
    	[Toggle(USE_BILLBOARD)]
    	_UseBillboard("Use billboard", int) = 0
        
        [Toggle(USE_UP_NORMAL)]
    	_UseUpNormal("Use up normal", int) = 0
        
        [Toggle(USE_MULT_WINDFORCE_BY_UV)]
    	_UseMultWindForceByUV("Multiply wind force by UV.y", int) = 0
        
        [Toggle] _ZWrite("ZWrite", int) = 1
        [Toggle(TG_USE_ALPHACLIP)] _AlphaClip("Use alpha clipping", float) = 0.0
        //[Enum(Off,0,On,1)] _AlphaToMask("Alpha to Mask", Int) = 0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        //[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Blend Source", float) = 1
        //[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Blend Destination", float) = 0

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
        _TransparencyLM ("Transmissive Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="TransparentCutout" 
        	"Queue"="AlphaTest"
        }
        LOD 100
        
        Pass
        {
        	Name "GBuffer"
            Tags
            {
                "LightMode" = "gbuffer"
            }
            
            //AlphaToMask[_AlphaToMask]
            //Blend[_SrcBlend][_DstBlend]
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
            #pragma require 2darray
            //#pragma require cubearray
            #pragma exclude_renderers gles nomrt //excluded shader from OpenGL ES 2.0 because it uses non-square matrices
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma shader_feature_local __ DEBUG_VERTEX_COLOR
            #pragma shader_feature_local __ TG_TRANSPARENT
            #pragma shader_feature_local TG_USE_ALPHACLIP
            #pragma shader_feature_local_vertex __ USE_BILLBOARD
            #pragma shader_feature_local_vertex __ USE_UP_NORMAL
            #pragma shader_feature_local_vertex __ USE_MULT_WINDFORCE_BY_UV
            
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            
            #pragma multi_compile UG_QUALITY_LOW UG_QUALITY_MED UG_QUALITY_HIGH
            #pragma multi_compile_fragment _ ARENA_MAP_RENDER

            #if defined(UG_QUALITY_MED) || defined(UG_QUALITY_HIGH)
            #define DGX_PBR_RENDERING 1
            #endif

            #include "Vegetation-Common.hlsl"
            
            ENDHLSL
        }

        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }
            
            Cull Off
            HLSLPROGRAM

            #pragma target 2.0

            #define ARENA_META_PASS
            
            #pragma vertex vert
            #pragma fragment metaFragment
            #pragma shader_feature_local _ TG_TRANSPARENT
            //#pragma shader_feature_local TG_USE_ALPHACLIP
            #pragma shader_feature_local_vertex _ USE_BILLBOARD
            #pragma shader_feature EDITOR_VISUALIZATION
            
            #include "Vegetation-Common.hlsl"
            
            ENDHLSL
        }
    }
}
