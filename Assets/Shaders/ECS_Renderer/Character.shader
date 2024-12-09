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
        _Smoothness("Smoothness", Range(0,1)) = 1
        _Metallic("Metallic", Range(0,1)) = 1
        
        [Toggle(ARENA_SKIN_COLOR)] _UseSkinColor("Use skin color", float) = 0.0
        _SkinColor("Skin color", Color) = (1,1,1,1)
        
        [HideInInspector] _SkinningData("SkinData", Vector) = (0, 1, 0, 0)
        [HideInInspector] _BaseColor("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "IgnoreProjector" = "True"
            "Queue"="Geometry"
        }
        LOD 100
        
        Pass
        {
            Name "gbuffer"
            Tags
            {
                "LightMode" = "gbuffer"
            }
            
            Cull[_Cull]
            
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
            #pragma require cubearray
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #pragma shader_feature TG_USE_ALPHACLIP
            #pragma shader_feature __ ARENA_SKIN_COLOR
            
            #pragma multi_compile _BONECOUNT_ONE _BONECOUNT_FOUR

            //#pragma multi_compile_instancing

            //#define UNITY_DOTS_INSTANCED_PROP_OVERRIDE_DISABLED_BY_DEFAULT

            #define TG_SKINNING

            #include "Input-Character.hlsl"
            #include "Common-Character.hlsl" 
            
            ENDHLSL
        }

//        Pass
//        {
//            Name "ForwardLit"
//            Tags
//            {
//                "LightMode" = "UniversalForward"
//            }
//            
//            Cull[_Cull]
//            
//            HLSLPROGRAM
//            #pragma target 4.5
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
//            #pragma shader_feature TG_USE_ALPHACLIP
//            #pragma shader_feature __ ARENA_SKIN_COLOR
//            #pragma shader_feature __ USE_LIGHTING
//            #pragma shader_feature __ USE_RIM
//            #pragma shader_feature __ USE_DISTANCE_LIGHT
//            #pragma multi_compile_fragment __ ARENA_USE_MAIN_LIGHT
//            #pragma multi_compile_fragment __ ARENA_USE_ADD_LIGHT
//            #pragma multi_compile_fragment __ ARENA_USE_DARK_MODE
//            
//            #pragma multi_compile _BONECOUNT_ONE _BONECOUNT_FOUR
//
//            //#pragma multi_compile_instancing
//
//            //#define UNITY_DOTS_INSTANCED_PROP_OVERRIDE_DISABLED_BY_DEFAULT
//
//            #define UG_QUALITY_HIGH
//            #define TG_SKINNING
//
//            #include "Input-Character.hlsl"
//            #include "Common-Character.hlsl" 
//            
//            ENDHLSL
//        }
        UsePass "Hidden/Arena/ShadowCaster-Skinned/SHADOWCASTER"
//
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
//            #pragma require cubearray       // из-за использования tg_ReflectionProbes
//            #pragma exclude_renderers gles //excluded shader from OpenGL ES 2.0 because it uses non-square matrices
//            
//            #pragma multi_compile _ DOTS_INSTANCING_ON
//            #pragma shader_feature TG_USE_ALPHACLIP
//
//            #pragma multi_compile _BONECOUNT_ONE _BONECOUNT_FOUR
//            
//            // -------------------------------------
//            // Shader Stages
//            #pragma vertex DepthOnlyVertex
//            #pragma fragment DepthOnlyFragment
//
//            // -------------------------------------
//            // Includes
//            #include "Input-Character.hlsl"
//            #include "Common-Character.hlsl"
//            ENDHLSL
//        }

//        Pass
//        {
//            Name "DepthNormalsOnly"
//            Tags
//            {
//                "LightMode" = "DepthNormalsOnly"
//            }
//
//            // -------------------------------------
//            // Render State Commands
//            ZWrite On
//
//            HLSLPROGRAM
//            #pragma target 2.0
//
//            // -------------------------------------
//            // Shader Stages
//            #pragma vertex DepthNormalsVertex
//            #pragma fragment DepthNormalsFragment
//
//            // -------------------------------------
//            // Material Keywords
//            #pragma shader_feature_local _ALPHATEST_ON
//
//            // -------------------------------------
//            // Universal Pipeline keywords
//            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT // forward-only variant
//            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
//            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
//
//            //--------------------------------------
//            // GPU Instancing
//            #pragma multi_compile_instancing
//            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
//
//            // -------------------------------------
//            // Includes
//            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
//            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitDepthNormalsPass.hlsl"
//            ENDHLSL
//        }
    }
}
