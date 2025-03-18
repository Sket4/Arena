Shader "Hidden/DGX/DEFERRED"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        ZTest Always
        ZWrite Off
        Cull Off
        
//        Stencil 
//        {
//            Ref 128
//            Comp Equal
//        }
        
        //Blend One SrcAlpha, Zero One
        //BlendOp Add, Add

        Pass
        {
            Name "fog"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog
            #pragma multi_compile _ DGX_PBR_RENDERING
            #pragma multi_compile_fragment _ DGX_SHADOWS_ENABLED
            #pragma multi_compile_fragment _ DGX_SPOT_LIGHTS
            #pragma multi_compile_fragment _ DGX_DARK_MODE
            //#pragma require cubearray
            #pragma exclude_renderers gles
            //#if defined(DGX_USE_PBR)
            //#pragma require cubearray
            //#endif

            #define DGX_FOG_ENABLED
            
            #include "DeferredPass.hlsl"
            
            ENDHLSL
        }

        Pass
        {
            Name "no fog"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ DGX_PBR_RENDERING
            #pragma multi_compile_fragment _ DGX_SHADOWS_ENABLED
            #pragma multi_compile_fragment _ DGX_SPOT_LIGHTS
            #pragma multi_compile_fragment _ DGX_DARK_MODE
            //#pragma require cubearray
            #pragma exclude_renderers gles
            //#if defined(DGX_USE_PBR)
            //#pragma require cubearray
            //#endif
            
            #include "DeferredPass.hlsl"
            
            ENDHLSL
        }
    }
}