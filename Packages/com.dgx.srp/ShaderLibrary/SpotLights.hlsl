#ifndef DGX_SPOTLIGHTS_INCLUDED
#define DGX_SPOTLIGHTS_INCLUDED
#include "Lighting.hlsl"

// spot lights
float4 _SpotLightDirs[4];
float4 _SpotLightPositions[4];
half4 _SpotLightColors[4];
sampler2D _SpotLightCookieTex;
half4x4 _SpotLight_M_Inv;

half3 calculateSpotLight(float3 worldPos, half3 normalWS)
{
    float3 spotLightPos = _SpotLightPositions[0].xyz;
    float3 spotLightRayDir = worldPos - spotLightPos;
    
    half spotLightAtten = dot(spotLightRayDir,spotLightRayDir);
    half spotLightRange = _SpotLightDirs[0].w;
    spotLightRange *= spotLightRange;
    spotLightAtten = 1-saturate(spotLightAtten / spotLightRange);

    float3 spotLightDir = _SpotLightDirs[0].xyz;

    spotLightRayDir = normalize(spotLightRayDir);
    half lightRayAngle = saturate(dot(spotLightRayDir, spotLightDir));
    half angleRange = 1 - (1 - lightRayAngle) * _SpotLightPositions[0].w;
    angleRange = saturate(angleRange);
    spotLightAtten *= angleRange;
    spotLightAtten *= saturate(-dot(spotLightRayDir, normalWS));

    // cookie texture
    half2 cookieTexUV;

    half3 localSpotLightDir = mul(_SpotLight_M_Inv, half4(spotLightRayDir, 0)).xyz;
    
    cookieTexUV.x = ((localSpotLightDir.x + 1) * 0.5);
    cookieTexUV.y = ((localSpotLightDir.y + 1) * 0.5);
    
    half3 cookieColor = tex2D(_SpotLightCookieTex, cookieTexUV);

    return _SpotLightColors[0].rgb * cookieColor * spotLightAtten;

    // half3 reflectDir = reflect(-spotLightRayDir, surface.NormalWS);
    // half spec = pow(saturate(dot(-viewDir, reflectDir)), surface.Roughness * 128);
    // specular += spec * _SpotLightColors[0].rgb * spotLightAtten;
}

void calculateSpotLightForSurface(float3 worldPos, inout SurfaceHalf surface)
{
    surface.AmbientLight += calculateSpotLight(worldPos, surface.NormalWS);
}

#endif