

#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED
//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


void GetLightProbe_float(float3 normalWS, out float3 color)
{
    OUTPUT_SH(normalWS, color);
}

void GetLightProbe_half(half3 normalWS, out half3 color)
{
    OUTPUT_SH(normalWS, color);
}
 
#endif