Shader "Hidden/Arena/ShadowCaster-Skinned"
{
    Properties
    {
        [KeywordEnum(One, Four)] _BoneCount ("Bone count", Integer) = 0
        _BaseMap ("Texture", 2D) = "white" {}
        _BumpMap ("Normal map", 2D) = "bump" {}
        _MeSmAO_Map("Me Sm AO map", 2D) = "white" {} 
        
        _FadeMap ("Fade map", 2D) = "white" {}

        _Roughness("Roughness", Range(0,1)) = 1
        _Metallic("Metallic", Range(0,1)) = 1
        
        [Toggle(ARENA_SKIN_COLOR)] _UseSkinColor("Use skin color", float) = 0.0
        _SkinColor("Skin color", Color) = (1,1,1,1)
        
        [HideInInspector] _SkinningData("SkinData", Vector) = (0, 1, 0, 0)
        [HideInInspector] _Cutoff("Cutoff", Float) = 1 
    }
    SubShader
    {
        Tags 
        {
            "Queue"="Geometry" 
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
            #pragma multi_compile _ DOTS_INSTANCING_ON
            
            // Shader Stages
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #define TG_SKINNING

            #include "Input-Character.hlsl"
            #include "ShadowCasterPass.hlsl" 
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster-Fading"
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
            
            #define TG_SKINNING
            #define TG_FADING
            #define TG_USE_ALPHACLIP


            #include "Input-Character.hlsl"
            #include "ShadowCasterPass.hlsl"  
            ENDHLSL
        }
    }
}
