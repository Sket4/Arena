#ifndef UG_COMMON_INCLUDED
#define UG_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.tzargames.rendering/Shaders/Lighting.hlsl"

real3 MixLightWithRealtimeShadow(real realtimeShadow, real3 ambientLight)
{
    return  min(lerp(_SubtractiveShadowColor.xyz, 1, realtimeShadow), ambientLight);
}

#endif //UG_COMMON_INCLUDED

