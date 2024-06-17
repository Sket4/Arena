#ifndef UG_INPUT_CHAR_INCLUDED
#define UG_INPUT_CHAR_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

CBUFFER_START(UnityPerMaterial)
    half4 _BaseMap_ST;  
    uint4 _SkinningData;
    half4 _RimColor;
    half _RimStr;
    half _Roughness;
    half _Metallic;
    half4 _BaseColor;
    half _Cutoff;
CBUFFER_END

#endif //UG_INPUT_INCLUDED

