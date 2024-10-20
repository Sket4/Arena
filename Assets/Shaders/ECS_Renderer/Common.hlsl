#ifndef UG_COMMON_INCLUDED
#define UG_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.tzargames.rendering/Shaders/Lighting.hlsl"

real3 MixLightWithRealtimeShadow(real realtimeShadow, real3 ambientLight)
{
    real3 shadowColor = lerp(_SubtractiveShadowColor.xyz, 1, realtimeShadow);
    real3 minLight = min(shadowColor, ambientLight);

    return lerp(minLight, ambientLight, realtimeShadow);
}

void ApplyDynamicLighting(half3 viewDirWS, half3 normalWS, float3 positionWS, half smoothness, inout float3 diffuseLight, inout half3 specularLight, bool multiplySpecByAtten)
{
    #ifdef ARENA_USE_ADD_LIGHT

    //Light light = GetMainLight();
    Light addLight = GetAdditionalLight(0, positionWS);

    diffuseLight *= addLight.distanceAttenuation;
	
    diffuseLight += LightingLambert(addLight.color, addLight.direction, normalWS.xyz) * addLight.distanceAttenuation;

    
    if(multiplySpecByAtten)
    {
        specularLight *= addLight.distanceAttenuation * 16;
    }

    //smoothness = exp2(10 * smoothness + 1);
    //half3 spec = LightingSpecular(addLight.color, addLight.direction, normalWS.xyz, viewDirWS, 1, smoothness);

    //spec = spec * addLight.distanceAttenuation * 1;
    //specularLight = spec;
    
    #endif
}

#endif //UG_COMMON_INCLUDED

