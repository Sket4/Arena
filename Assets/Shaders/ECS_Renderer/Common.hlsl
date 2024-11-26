#ifndef UG_COMMON_INCLUDED
#define UG_COMMON_INCLUDED

#if DOTS_INSTANCING_ON
#if LIGHTMAP_ON
#define ARENA_COMPUTE_AMBIENT_LIGHT(input,normalWS) Arena_ComputeAmbientLight(input.lightmapUV, input.instanceData.x, normalWS)
#else
#define ARENA_COMPUTE_AMBIENT_LIGHT(input,normalWS) Arena_ComputeAmbientLight(normalWS)
#endif
#else
#if LIGHTMAP_ON
#define ARENA_COMPUTE_AMBIENT_LIGHT(input,normalWS) Arena_ComputeAmbientLight(input.lightmapUV, 0, normalWS)
#else
#define ARENA_COMPUTE_AMBIENT_LIGHT(input,normalWS) Arena_ComputeAmbientLight(normalWS)
#endif
#endif

#if defined(UG_QUALITY_HIGH) || defined(UG_QUALITY_MED)
#define ARENA_DYN_LIGHT(normalWS, positionWS, lighting, viewDirWS, envMapColor, useShadowMap) ApplyDynamicLighting(normalWS, positionWS, lighting, viewDirWS, envMapColor, useShadowMap)
#else
#define ARENA_DYN_LIGHT(normalWS, positionWS, lighting, viewDirWS, envMapColor, useShadowMap) ApplyDynamicLighting(normalWS, positionWS, lighting, useShadowMap)
#endif

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.tzargames.rendering/Shaders/Lighting.hlsl"


real3 MixLightWithRealtimeShadow(real realtimeShadow, real3 ambientLight)
{
    real3 shadowColor = lerp(_SubtractiveShadowColor.xyz, 1, realtimeShadow);
    real3 minLight = min(shadowColor, ambientLight);

    return lerp(minLight, ambientLight, realtimeShadow);
}

half3 Arena_ComputeAmbientLight(
#if LIGHTMAP_ON
    float2 lightmapUV,
    half slice,
#endif
    half3 normalWS)
{
    half3 ambientLight;
    
#if LIGHTMAP_ON
    ambientLight = TG_SAMPLE_LIGHTMAP(lightmapUV, slice, normalWS);
#else
    #if defined(ARENA_USE_DARK_MODE)
    ambientLight = 0;
    #else
    ambientLight = TG_ComputeAmbientLight_half(normalWS);
    #endif
#endif

    return ambientLight;
}

void ApplyDynamicLighting(
    half3 normalWS,
    float3 positionWS,
    inout float3 diffuseLight,
    
    #if defined(UG_QUALITY_HIGH) || defined(UG_QUALITY_MED)
    half3 viewDirWS,
    inout half3 specularLight,
    #endif
    
    bool useShadowMap)
{
    #if !DOTS_INSTANCING_ON && !LIGHTMAPS_ON
    Light mainLight = GetMainLight();
    diffuseLight += LightingLambert(mainLight.color, mainLight.direction, normalWS.xyz);
    
    #endif

    if(useShadowMap)
    {
        float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
        half shadow = MainLightRealtimeShadow(shadowCoord);
        diffuseLight = MixLightWithRealtimeShadow(shadow, diffuseLight);    
    }
    
    
    #ifdef ARENA_USE_ADD_LIGHT
    Light addLight = GetAdditionalLight(0, positionWS, 1);
	
    half3 addLighting = LightingLambert(addLight.color, addLight.direction, normalWS.xyz) * addLight.distanceAttenuation;

    if(useShadowMap)
    {
        addLighting *= AdditionalLightRealtimeShadow(0, positionWS, addLight.direction);    
    }
    
    diffuseLight += addLighting;

    #if defined(UG_QUALITY_HIGH) || defined(UG_QUALITY_MED)
    #if ARENA_USE_DARK_MODE
    specularLight *= addLight.distanceAttenuation * 16;
    #endif
    #endif

    //smoothness = exp2(10 * smoothness + 1);
    //half3 spec = LightingSpecular(addLight.color, addLight.direction, normalWS.xyz, viewDirWS, 1, smoothness);

    //spec = spec * addLight.distanceAttenuation * 1;
    //specularLight = spec;
    
    #endif
}

#endif //UG_COMMON_INCLUDED

