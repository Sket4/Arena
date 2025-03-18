#ifndef DGX_LIGHTING_INCLUDED
#define DGX_LIGHTING_INCLUDED

#include "Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

struct GBufferFragmentOutput
{
    half4 GBuffer0 : SV_Target0;
    half4 GBuffer1 : SV_Target1;
    //half4 GBuffer2 : SV_Target2;
};

struct SurfaceHalf
{
    half3 Albedo;
    float3 NormalWS;
    half3 AmbientLight;
    half3 Metallic;
    half Roughness;
    half Alpha;
    half EnvCubemapIndex;
};

#define MAX_ENVMAP_INDEX 128.0
#define MAX_ENVMAP_INDEX_INV (1.0 / 128.0)

GBufferFragmentOutput SurfaceToGBufferOutputHalf(SurfaceHalf surface)
{
    GBufferFragmentOutput result;

    float2 encodedNormal = OctahedronEncode(surface.NormalWS);
    result.GBuffer0.xyz = surface.Albedo;
    result.GBuffer0.w = LinearToSRGB(surface.Metallic.r);
    result.GBuffer1.x = encodedNormal.x;
    result.GBuffer1.y = encodedNormal.y;
    result.GBuffer1.z = surface.Roughness;
    result.GBuffer1.w = surface.EnvCubemapIndex * MAX_ENVMAP_INDEX_INV + MAX_ENVMAP_INDEX_INV * 0.1;

    return result;
}

SurfaceHalf GBufferToSurfaceHalf(float4 gbuffer0, float4 gbuffer1)
{
    SurfaceHalf result;

    result.Albedo = gbuffer0.rgb;
    #ifdef DGX_DARK_MODE
    result.AmbientLight = 0;
    #else
    result.AmbientLight = 1;
    #endif
    result.NormalWS = OctahedronDecode(float2(gbuffer1.x, gbuffer1.y));
    result.Metallic = SRGBToLinear(gbuffer0.w);
    result.Roughness = gbuffer1.z;
    result.EnvCubemapIndex = gbuffer1.w * MAX_ENVMAP_INDEX;
    result.Alpha = 1;
    
    return  result;
}

#endif