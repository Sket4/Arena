#ifndef UG_INPUT_CHAR_INCLUDED
#define UG_INPUT_CHAR_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

CBUFFER_START(UnityPerMaterial)
    half4 _BaseMap_ST;
    half _Smoothness;
    half _Metallic;
    half4 _SkinColor;

//#if defined(TG_USE_ALPHACLIP)
    half _Cutoff;
//#endif
CBUFFER_END

#endif