Shader "Hidden/DGX/DEFERRED"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        ZTest NotEqual
        ZWrite Off
        Cull Off
        
        //Blend One SrcAlpha, Zero One
        //BlendOp Add, Add
        
        Stencil 
        {
            Ref 128
            Comp Equal
            Pass Keep
            Fail Keep
            ZFail Keep
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile FOG_LINEAR FOG_EXP FOG_EXP2
            #pragma require cubearray
            #pragma exclude_renderers gles
            //#if defined(DGX_USE_PBR)
            //#pragma require cubearray
            //#endif
            
            #include "DeferredPass.hlsl"
            
            ENDHLSL
        }

//        Pass
//        {
//            Name "Fog"
//            HLSLPROGRAM
//            #pragma vertex vert
//            #pragma fragment fragFog
//            #pragma multi_compile FOG_LINEAR FOG_EXP FOG_EXP2
//            #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
//            
//            //#if defined(DGX_USE_PBR)
//            //#pragma require cubearray
//            //#endif
//
//            #define _FOG
//            #define FOG_LINEAR
//            
//            #include "DeferredPass.hlsl"
//            
//            ENDHLSL
//        }
    }
}