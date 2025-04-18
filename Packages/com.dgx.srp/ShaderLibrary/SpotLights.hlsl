#ifndef DGX_SPOTLIGHTS_INCLUDED
#define DGX_SPOTLIGHTS_INCLUDED
#include "Lighting.hlsl"

// spot lights
float4 _SpotLightDirs[4];
float4 _SpotLightPositions[4];
half4 _SpotLightColors[4];
sampler2D _SpotLightCookieTex;
half4x4 _SpotLight_M_Inv;

void calculateSpotLight(float3 worldPos, half3 normalWS, half3 viewDir, half roughness, inout half3 diffuseLight, inout half3 specular)
{
    float3 spotLightPos = _SpotLightPositions[0].xyz;
    half3 spotLightRayDir = worldPos - spotLightPos;
    
    half spotLightAtten = dot(spotLightRayDir,spotLightRayDir);

    // spotLightRange == squared
    half spotLightRange = _SpotLightDirs[0].w;
    //spotLightRange *= spotLightRange;
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

    half3 spotLightColor = _SpotLightColors[0].rgb * cookieColor * spotLightAtten;
    diffuseLight += spotLightColor;

    #ifdef DGX_PBR_RENDERING
    half3 reflectDir = reflect(-spotLightRayDir, normalWS);
    half spec = pow(saturate(dot(viewDir, reflectDir)), roughness * 128);
    specular += spec * spotLightColor;
    #endif
}

void calculateSpotLightForSurface(float3 worldPos, half3 viewDir, inout SurfaceHalf surface, inout half3 specular)
{
    calculateSpotLight(worldPos, surface.NormalWS, viewDir, surface.Roughness, surface.AmbientLight, specular);
}

#endif