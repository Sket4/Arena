#ifndef DGX_LIGHTING_INCLUDED
#define DGX_LIGHTING_INCLUDED

#include "Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

struct GBufferFragmentOutput
{
    half4 GBuffer0 : SV_Target0;
    half4 GBuffer1 : SV_Target1;

    #ifdef DGX_PBR_RENDERING
    half4 GBuffer2 : SV_Target2;
    #endif
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
    
    result.GBuffer0.xyz = surface.Albedo;
    result.GBuffer1.rgb = surface.AmbientLight / 5;

    float2 encodedNormal = PackNormalOctQuadEncode(surface.NormalWS);
    encodedNormal = encodedNormal * 0.5 + 0.5;
    result.GBuffer0.w = encodedNormal.x;
    result.GBuffer1.w = encodedNormal.y;

    #ifdef DGX_PBR_RENDERING
    result.GBuffer2.x = surface.Metallic;
    result.GBuffer2.y = surface.Roughness;
    result.GBuffer2.z = surface.EnvCubemapIndex * MAX_ENVMAP_INDEX_INV + MAX_ENVMAP_INDEX_INV * 0.1;

    // unused
    result.GBuffer2.w = 0;
    #endif
    
    return result;
}

SurfaceHalf GBufferToSurfaceHalf(float4 gbuffer0, float4 gbuffer1, float4 gbuffer2)
{
    SurfaceHalf result;

    result.Albedo = gbuffer0.rgb;
    #ifdef DGX_DARK_MODE
    result.AmbientLight = 0;
    #else
    result.AmbientLight = gbuffer1.rgb * 5;
    #endif
    result.NormalWS = UnpackNormalOctQuadEncode(float2(gbuffer0.w, gbuffer1.w) * 2 - 1);

    #ifdef DGX_PBR_RENDERING
    result.Metallic = gbuffer2.x;
    result.Roughness = gbuffer2.y;
    result.EnvCubemapIndex = gbuffer2.z * MAX_ENVMAP_INDEX;
    #else
    result.Metallic = 0;
    result.Roughness = 1;
    result.EnvCubemapIndex = 0;
    #endif
    
    result.Alpha = 1;
    
    return  result;
}

#endif