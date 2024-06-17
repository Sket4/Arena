Shader "Hidden/Arena/ShadowCaster-Skinned"
{
    Properties
    {
        //[KeywordEnum(One, Four)] _BoneCount ("Bone count", Integer) = 0
        
        [Toggle] _ZWrite("ZWrite", int) = 1
        [Toggle(TG_USE_ALPHACLIP)] _AlphaClip("Use alpha clipping", float) = 0.0

        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", int) = 2

        //_BaseMap ("Main color", 2D) = "white" {}
        //_BumpMap ("Normal map", 2D) = "bump" {}
        //_MetallicGlossMap("Metallic/Smoothness map", 2D) = "white" {}
        //_Metallic("Metallic", Range(0,1)) = 1.0
    	//_Smoothness ("Smoothness", Range(0,1)) = 0.5
        _DynamicShadowStrength("Dynamic shadow strength", Range(0,1)) = 1.0
        
        [HideInInspector] _SkinningData("SkinData", Vector) = (0, 1, 0, 0)
        [HideInInspector] _BaseColor("Color", Color) = (1,1,1,1)
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

            // -------------------------------------
            // Shader Stages
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #pragma multi_compile _ DOTS_INSTANCING_ON
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            // -------------------------------------
            // Includes
            #define TG_SKINNING
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

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

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #pragma multi_compile _ DOTS_INSTANCING_ON
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            // -------------------------------------
            // Includes
            #define TG_SKINNING
            #define TG_FADING
            #define TG_USE_ALPHACLIP

            #include "Input-Character.hlsl"
            #include "ShadowCasterPass.hlsl"  
            ENDHLSL
        }
    }
}
