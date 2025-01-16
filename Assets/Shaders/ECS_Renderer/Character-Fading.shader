Shader"Arena/Character Fading"
{
    Properties
    {
        [KeywordEnum(One, Four)] _BoneCount ("Bone count", Integer) = 0
        _BaseMap ("Texture", 2D) = "white" {}
        _BumpMap ("Normal map", 2D) = "bump" {}
        _MeSmAO_Map("Me Sm AO map", 2D) = "white" {} 
        
        _FadeMap ("Fade map", 2D) = "white" {}

        _Smoothness("Smoothness", Range(0,1)) = 1
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
            "RenderType"="Opaque"
            "Queue"="AlphaTest"
        }
        LOD 100

        Pass
        {
            Name "gbuffer"
            
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
            #pragma exclude_renderers gles nomrt//excluded shader from OpenGL ES 2.0 because it uses non-square matrices
            #pragma require 2darray
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile_fog
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #pragma shader_feature __ ARENA_SKIN_COLOR

            #pragma multi_compile UG_QUALITY_LOW UG_QUALITY_MED UG_QUALITY_HIGH
            #pragma multi_compile _BONECOUNT_ONE _BONECOUNT_FOUR
            #pragma multi_compile_fragment _ DGX_DARK_MODE
            
            #pragma multi_compile _BONECOUNT_ONE _BONECOUNT_FOUR
            
            //#pragma multi_compile_instancing

            //#define UNITY_DOTS_INSTANCED_PROP_OVERRIDE_DISABLED_BY_DEFAULT

            #define UG_QUALITY_HIGH
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
