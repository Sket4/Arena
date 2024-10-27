Shader"Arena/Character Fading"
{
    Properties
    {
        [KeywordEnum(One, Four)] _BoneCount ("Bone count", Integer) = 0
        _BaseMap ("Texture", 2D) = "white" {}
        _BumpMap ("Normal map", 2D) = "bump" {}
        _MeSmAO_Map("Me Sm AO map", 2D) = "white" {} 
        
        [Toggle(USE_RIM)]
        _UseRim("Use rim", float) = 0.0
        [Toggle(USE_DISTANCE_LIGHT)]
        _UseDistLight("Use distance light", float) = 0.0
        _RimColor("Rim color", Color) = (1,1,1,1)
        _RimStr("Rim strength", Range(0,1)) = 1
        
        _FadeMap ("Fade map", 2D) = "white" {}

        _Smoothness("Smoothness", Range(0,1)) = 1
        _Metallic("Metallic", Range(0,1)) = 1
        
        [HideInInspector] _SkinningData("SkinData", Vector) = (0, 1, 0, 0)
        [HideInInspector] _Cutoff("Cutoff", Float) = 1 
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="AlphaTest"
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            HLSLPROGRAM
            #pragma target 4.5
            #pragma require 2darray
            #pragma require cubearray
            #pragma exclude_renderers gles //excluded shader from OpenGL ES 2.0 because it uses non-square matrices
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma shader_feature __ USE_RIM
            #pragma shader_feature __ USE_DISTANCE_LIGHT
            #pragma multi_compile __ ARENA_USE_MAIN_LIGHT
            #pragma multi_compile __ ARENA_USE_ADD_LIGHT
            
            #pragma multi_compile _BONECOUNT_ONE _BONECOUNT_FOUR
            
            //#pragma multi_compile_instancing

            //#define UNITY_DOTS_INSTANCED_PROP_OVERRIDE_DISABLED_BY_DEFAULT

            #define TG_SKINNING
            #define TG_FADING
            #define TG_USE_ALPHACLIP

            #include "Input-Character.hlsl"
            #include "Common-Character.hlsl" 
            
            ENDHLSL
        }
        UsePass "Hidden/Arena/ShadowCaster-Skinned/SHADOWCASTER-FADING"
    }
}
