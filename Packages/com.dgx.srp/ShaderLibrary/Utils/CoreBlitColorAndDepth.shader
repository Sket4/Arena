Shader "Hidden/DGX/CoreBlitColorAndDepth"
{
    HLSLINCLUDE

	// FROM URP 14.0.12
        #pragma target 2.0
        #pragma editor_sync_compilation
        // Core.hlsl for XR dependencies
        //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/BlitColorAndDepth.hlsl"
    ENDHLSL

    SubShader
    {
        //Tags{ "RenderPipeline" = "UniversalPipeline" }

        // 0: Color Only
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off
            Name "ColorOnly"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragColorOnly
            ENDHLSL
        }

        // 1:  Color Only and Depth
        Pass
        {
            ZWrite On ZTest Always Blend Off Cull Off
            Name "ColorAndDepth"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragColorAndDepth
            ENDHLSL
        }

    }

    Fallback Off
}
