#ifndef DGX_LIGHTING_INCLUDED
#define DGX_LIGHTING_INCLUDED

#include "Common.hlsl"

struct GBufferFragmentOutput
{
    half4 GBuffer0 : SV_Target0;
    half4 GBuffer1 : SV_Target1;
    half4 GBuffer2 : SV_Target2;
};

struct SurfaceHalf
{
    half3 Albedo;
    half3 NormalWS;
    half3 AmbientLight;
    half3 Metallic;
    half Roughness;
    half Alpha;
    uint EnvCubemapIndex;
};

const half MAX_ENVMAP_INDEX = 512;

GBufferFragmentOutput SurfaceToGBufferOutputHalf(SurfaceHalf surface)
{
    GBufferFragmentOutput result;

    float2 encodedNormal = OctahedronEncode(surface.NormalWS);
    result.GBuffer0.xyz = surface.Albedo;
    result.GBuffer0.w = encodedNormal.x;
    result.GBuffer1.xyz = surface.AmbientLight.xyz;
    result.GBuffer1.w = encodedNormal.y; 
    result.GBuffer2.x = surface.Metallic;
    result.GBuffer2.y = surface.Roughness;
    result.GBuffer2.z = (surface.EnvCubemapIndex / MAX_ENVMAP_INDEX) + (1 / MAX_ENVMAP_INDEX) * 0.0001;
    result.GBuffer2.w = 0;

    return result;
}

SurfaceHalf GBufferToSurfaceHalf(float4 gbuffer0, float4 gbuffer1, float4 gbuffer2)
{
    SurfaceHalf result;

    result.Albedo = gbuffer0.rgb;
    result.AmbientLight = gbuffer1.rgb;
    result.NormalWS = OctahedronDecode(float2(gbuffer0.w, gbuffer1.w));
    result.Metallic = gbuffer2.x;
    result.Roughness = gbuffer2.y;
    result.EnvCubemapIndex = gbuffer2.z * MAX_ENVMAP_INDEX;
    result.Alpha = 1;
    
    return  result;
}

#endif