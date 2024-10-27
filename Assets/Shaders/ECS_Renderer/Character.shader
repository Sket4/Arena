Shader"Arena/Character"
{
    Properties
    {
        [Toggle(TG_USE_ALPHACLIP)] _AlphaClip("Use alpha clipping", float) = 0.0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", int) = 2
        
        [KeywordEnum(One, Four)] _BoneCount ("Bone count", Integer) = 0
        _BaseMap ("Texture", 2D) = "white" {}
        _BumpMap ("Normal map", 2D) = "bump" {}
        _MetallicSmoothnessMap("Metallic Smoothness (A)", 2D) = "white" {}
        [Toggle(USE_RIM)]
        _UseRim("Use rim", float) = 0.0
        [Toggle(USE_DISTANCE_LIGHT)]
        _UseDistLight("Use distance light", float) = 0.0
        _RimColor("Rim color", Color) = (1,1,1,1)
        _RimStr("Rim strength", Range(0,1)) = 1
        _Smoothness("Smoothness", Range(0,1)) = 1
        _Metallic("Metallic", Range(0,1)) = 1
        
        [HideInInspector] _SkinningData("SkinData", Vector) = (0, 1, 0, 0)
        [HideInInspector] _BaseColor("Color", Color) = (1,1,1,1)
        [HideInInspector] _Cutoff("Cutoff", Float) = 1 
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Cull[_Cull]
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma require cubearray
            #pragma exclude_renderers gles //excluded shader from OpenGL ES 2.0 because it uses non-square matrices
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #pragma shader_feature TG_USE_ALPHACLIP
            #pragma shader_feature __ USE_LIGHTING
            #pragma shader_feature __ USE_RIM
            #pragma shader_feature __ USE_DISTANCE_LIGHT
            #pragma multi_compile __ ARENA_USE_MAIN_LIGHT
            #pragma multi_compile __ ARENA_USE_ADD_LIGHT
            
            #pragma multi_compile _BONECOUNT_ONE _BONECOUNT_FOUR
            
            //#pragma multi_compile_instancing

            //#define UNITY_DOTS_INSTANCED_PROP_OVERRIDE_DISABLED_BY_DEFAULT

            #define TG_SKINNING

            #include "Input-Character.hlsl"
            #include "Common-Character.hlsl" 
            
            ENDHLSL
        }
        UsePass "Hidden/Arena/ShadowCaster-Skinned/SHADOWCASTER"
    }
}
