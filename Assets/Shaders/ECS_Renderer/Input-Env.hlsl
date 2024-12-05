#ifndef UG_INPUT_INCLUDED
#define UG_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

CBUFFER_START(UnityPerMaterial)
half4 _BaseMap_ST;
half4 _BaseColor;
half4 _SurfaceMap_ST;
half _SurfaceBlendFactor;
half4 _Underwater_color;
half4 _EmissionColor;
half _Metallic;
half _Smoothness;
half _Cutoff;
half _HighlightRemove;
half _HeightFogFade;
half _FogHeight;
CBUFFER_END

#endif //UG_INPUT_INCLUDED

