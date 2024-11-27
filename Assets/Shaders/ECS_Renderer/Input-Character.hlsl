#ifndef UG_INPUT_CHAR_INCLUDED
#define UG_INPUT_CHAR_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

CBUFFER_START(UnityPerMaterial)
    half4 _BaseMap_ST;
    half _Smoothness;
    half _Metallic;
    half4 _SkinColor;

    //half4 _BaseColor;

    


//#if defined(TG_USE_ALPHACLIP)
    half _Cutoff;
//#endif

CBUFFER_END

#endif //UG_INPUT_INCLUDED

//#if defined(ARENA_SKIN_COLOR)
    
//#endif