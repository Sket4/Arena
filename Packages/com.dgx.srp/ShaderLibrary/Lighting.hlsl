#ifndef DGX_LIGHTING_INCLUDED
#define DGX_LIGHTING_INCLUDED

#include "Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

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
    half EnvCubemapIndex;
};

#define MAX_ENVMAP_INDEX 128.0
#define MAX_ENVMAP_INDEX_INV (1.0 / 128.0)

GBufferFragmentOutput SurfaceToGBufferOutputHalf(SurfaceHalf surface)
{
    GBufferFragmentOutput result;

    float2 encodedNormal = OctahedronEncode(surface.NormalWS);
    result.GBuffer0.xyz = surface.Albedo;
    result.GBuffer0.w = LinearToSRGB(encodedNormal.x);
    result.GBuffer1.xyz = surface.AmbientLight.xyz;
    result.GBuffer1.w = LinearToSRGB(encodedNormal.y); 
    result.GBuffer2.x = surface.Metallic.r;
    result.GBuffer2.y = surface.Roughness;
    //TODO
    result.GBuffer2.z = surface.EnvCubemapIndex * MAX_ENVMAP_INDEX_INV + MAX_ENVMAP_INDEX_INV * 0.1;
    //result.GBuffer2.z = surface.EnvCubemapIndex * 0.1;
    result.GBuffer2.w = 0;

    return result;
}

SurfaceHalf GBufferToSurfaceHalf(float4 gbuffer0, float4 gbuffer1, float4 gbuffer2)
{
    SurfaceHalf result;

    result.Albedo = gbuffer0.rgb;
    result.AmbientLight = gbuffer1.rgb;
    result.NormalWS = OctahedronDecode(float2(SRGBToLinear(gbuffer0.w), SRGBToLinear(gbuffer1.w)));
    result.Metallic = gbuffer2.x;
    result.Roughness = gbuffer2.y;
    // TODO
    result.EnvCubemapIndex = gbuffer2.z * MAX_ENVMAP_INDEX;
    //result.EnvCubemapIndex = gbuffer2.z * 10;
    result.Alpha = 1;
    
    return  result;
}

#endif