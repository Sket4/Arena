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


#ifdef DGX_SPOT_LIGHTS
#include "Packages/com.dgx.srp/ShaderLibrary/SpotLights.hlsl"
#endif

#if defined(UG_QUALITY_HIGH) || defined(UG_QUALITY_MED)
#define ARENA_DYN_LIGHT(normalWS, positionWS, lighting, viewDirWS, envMapColor, roughness, useShadowMap) ApplyDynamicLighting(normalWS, positionWS, lighting, roughness, viewDirWS, envMapColor, useShadowMap)
#else
#define ARENA_DYN_LIGHT(normalWS, positionWS, lighting, viewDirWS, envMapColor, roughness, useShadowMap) ApplyDynamicLighting(normalWS, positionWS, lighting, useShadowMap)
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

#ifdef TG_USE_URP
real3 MixLightWithRealtimeShadow(real realtimeShadow, real3 ambientLight)
{
    real3 shadowColor = lerp(_SubtractiveShadowColor.xyz, 1, realtimeShadow);
    real3 minLight = min(shadowColor, ambientLight);

    return lerp(minLight, ambientLight, realtimeShadow);
}
#else


#ifdef DOTS_INSTANCING_ON
#include "Packages/ECS-Renderer/Shaders/UnityDOTSInstancing.hlsl"
#endif

#include "Packages/com.dgx.srp/ShaderLibrary/Common.hlsl"
#include "Packages/com.tzargames.rendering/Shaders/Lighting.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

#if UNITY_REVERSED_Z
	#if (defined(SHADER_API_GLCORE) && !defined(SHADER_API_SWITCH)) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
		//GL with reversed z => z clip range is [near, -far] -> remapping to [0, far]
		#define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max((coord - _ProjectionParams.y)/(-_ProjectionParams.z-_ProjectionParams.y)*_ProjectionParams.z, 0)
	#else
		//D3d with reversed Z => z clip range is [near, 0] -> remapping to [0, far]
		//max is required to protect ourselves from near plane not being correct/meaningful in case of oblique matrices.
		#define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(((1.0-(coord)/_ProjectionParams.y)*_ProjectionParams.z),0)
	#endif
#elif UNITY_UV_STARTS_AT_TOP
	//D3d without reversed z => z clip range is [0, far] -> nothing to do
	#define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) (coord)
#else
	//Opengl => z clip range is [-near, far] -> remapping to [0, far]
	#define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(((coord + _ProjectionParams.y)/(_ProjectionParams.z+_ProjectionParams.y))*_ProjectionParams.z, 0)
#endif

// Returns 'true' if the current view performs a perspective projection.
bool IsPerspectiveProjection()
{
	#if defined(SHADERPASS) && (SHADERPASS != SHADERPASS_SHADOWS)
	return (unity_OrthoParams.w == 0);
	#else
	// TODO: set 'unity_OrthoParams' during the shadow pass.
	return UNITY_MATRIX_P[3][3] == 0;
	#endif
}

float3 GetViewForwardDir()
{
	float4x4 viewMat = GetWorldToViewMatrix();
	return -viewMat[2].xyz;
}

// Could be e.g. the position of a primary camera or a shadow-casting light.
float3 GetCurrentViewPosition()
{
	#if defined(SHADERPASS) && (SHADERPASS != SHADERPASS_SHADOWS)
	return GetPrimaryCameraPosition();
	#else
	// This is a generic solution.
	// However, for the primary camera, using '_WorldSpaceCameraPos' is better for cache locality,
	// and in case we enable camera-relative rendering, we can statically set the position is 0.
	return UNITY_MATRIX_I_V._14_24_34;
	#endif
}

// Computes the world space view direction (pointing towards the viewer).
float3 GetWorldSpaceViewDir(float3 positionWS)
{
	if (IsPerspectiveProjection())
	{
		// Perspective
		return GetCurrentViewPosition() - positionWS;
	}
	else
	{
		// Orthographic
		return -GetViewForwardDir();
	}
}

float3 GetWorldSpaceNormalizeViewDir(float3 positionWS)
{
	if (IsPerspectiveProjection())
	{
		// Perspective
		float3 V = GetCurrentViewPosition() - positionWS;
		return normalize(V);
	}
	else
	{
		// Orthographic
		return -GetViewForwardDir();
	}
}

real ComputeFogFactorZ0ToFar(float z)
{
	#if defined(FOG_LINEAR)
	// factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
	float fogFactor = saturate(z * unity_FogParams.z + unity_FogParams.w);
	return real(fogFactor);
	#elif defined(FOG_EXP) || defined(FOG_EXP2)
	// factor = exp(-(density*z)^2)
	// -density * z computed at vertex
	return real(unity_FogParams.x * z);
	#else
	return real(0.0);
	#endif
}

real ComputeFogFactor(float zPositionCS)
{
	float clipZ_0Far = UNITY_Z_0_FAR_FROM_CLIPSPACE(zPositionCS);
	return ComputeFogFactorZ0ToFar(clipZ_0Far);
}

half ComputeFogIntensity(half fogFactor)
{
	half fogIntensity = half(0.0);
	#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
	#if defined(FOG_EXP)
	// factor = exp(-density*z)
	// fogFactor = density*z compute at vertex
	fogIntensity = saturate(exp2(-fogFactor));
	#elif defined(FOG_EXP2)
	// factor = exp(-(density*z)^2)
	// fogFactor = density*z compute at vertex
	fogIntensity = saturate(exp2(-fogFactor * fogFactor));
	#elif defined(FOG_LINEAR)
	fogIntensity = fogFactor;
	#endif
	#endif
	return fogIntensity;
}
float3 MixFog(float3 fragColor, float3 fogColor, float fogFactor)
{
	#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
	//if (IsFogEnabled())
	{
		float fogIntensity = ComputeFogIntensity(fogFactor);
		fragColor = lerp(fogColor, fragColor, fogIntensity);
	}
	#endif
	return fragColor;
}

#endif

half3 Arena_ComputeAmbientLight(
#if LIGHTMAP_ON
    float2 lightmapUV,
    half slice,
#endif
    half3 normalWS)
{
    half3 ambientLight;
    


	#ifdef ARENA_MAP_RENDER
	ambientLight = 1;
	#else

	#if LIGHTMAP_ON
		ambientLight = TG_SAMPLE_LIGHTMAP(lightmapUV, slice, normalWS);
	#else
		#if defined(DGX_DARK_MODE)
			#ifdef TG_TRANSPARENT
			ambientLight = 0;
			#else
			ambientLight = 1;
			#endif
		#else
			#if defined(DOTS_INSTANCING_ON)
				ambientLight = TG_ComputeAmbientLight_half(normalWS);
			#else
				ambientLight = saturate(dot(normalWS, half3(0,0,1))) + half3(0.6,0.6,0.9) * 0.3;
			#endif
		#endif
	#endif
	
	#endif

    return ambientLight;
}

void ApplyDynamicLighting(
    half3 normalWS,
    float3 positionWS,
    inout float3 diffuseLight,
    
    #if defined(UG_QUALITY_HIGH) || defined(UG_QUALITY_MED)
    half roughness,
    half3 viewDirWS,
    inout half3 specularLight,
    #endif
    
    bool useShadowMap)
{
    #if !DOTS_INSTANCING_ON && !LIGHTMAPS_ON
    #ifdef TG_USE_URP
        Light mainLight = GetMainLight();
        diffuseLight += LightingLambert(mainLight.color, mainLight.direction, normalWS.xyz);
    #endif
    #endif

    #ifdef TG_USE_URP
    if(useShadowMap)
    {
        float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
        half shadow = MainLightRealtimeShadow(shadowCoord);
        diffuseLight = MixLightWithRealtimeShadow(shadow, diffuseLight);    
    }
    #endif

	#ifdef TG_TRANSPARENT
	#ifdef DGX_SPOT_LIGHTS
	#if !defined(UG_QUALITY_HIGH) && !defined(UG_QUALITY_MED)
	half3 viewDirWS = 0;
	half3 specularLight = 0;
	half roughness = 0;
	calculateSpotLight(positionWS, normalWS, viewDirWS, roughness, diffuseLight, specularLight);
	#endif
	calculateSpotLight(positionWS, normalWS, viewDirWS, roughness, diffuseLight, specularLight);
	#endif
	#endif
}

#endif

