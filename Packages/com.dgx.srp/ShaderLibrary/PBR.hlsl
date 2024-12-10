#ifndef DGX_PBR_INCLUDED
#define DGX_PBR_INCLUDED

#include "Lighting.hlsl"

half2 EnvBRDFApproxLazarov(half Roughness, half NoV)
{
	// [ Lazarov 2013, "Getting More Physical in Call of Duty: Black Ops II" ]
	// Adaptation to fit our G term.
	const half4 c0 = { -1, -0.0275, -0.572, 0.022 };
	const half4 c1 = { 1, 0.0425, 1.04, -0.04 };
	half4 r = Roughness * c0 + c1;
	half a004 = min(r.x * r.x, exp2(-9.28 * NoV)) * r.x + r.y;
	half2 AB = half2(-1.04, 1.04) * a004 + r.zw;
	return AB;
}

half3 EnvBRDFApprox(half3 SpecularColor, half Roughness, half NoV)
{
	half2 AB = EnvBRDFApproxLazarov(Roughness, NoV);

	// Anything less than 2% is physically impossible and is instead considered to be shadowing
	// Note: this is needed for the 'specular' show flag to work, since it uses a SpecularColor of 0
	float F90 = saturate(50 * SpecularColor.g);

	return SpecularColor * AB.x + F90 * AB.y;
}

half3 EnvBRDFApprox(half3 F0, half3 F90, half Roughness, half NoV)
{
	half2 AB = EnvBRDFApproxLazarov(Roughness, NoV);
	return F0 * AB.x + F90 * AB.y;
}

half EnvBRDFApproxNonmetal(half Roughness, half NoV)
{
	// Same as EnvBRDFApprox( 0.04, Roughness, NoV )
	const half2 c0 = { -1, -0.0275 };
	const half2 c1 = { 1, 0.0425 };
	half2 r = Roughness * c0 + c1;
	return min(r.x * r.x, exp2(-9.28 * NoV)) * r.x + r.y;
}

void EnvBRDFApproxFullyRough(inout half3 DiffuseColor, inout half3 SpecularColor)
{
	// Factors derived from EnvBRDFApprox( SpecularColor, 1, 1 ) == SpecularColor * 0.4524 - 0.0024
	DiffuseColor += SpecularColor * 0.45;
	SpecularColor = 0;
	// We do not modify Roughness here as this is done differently at different places.
}

void EnvBRDFApproxFullyRough(inout half3 DiffuseColor, inout half SpecularColor)
{
	DiffuseColor += SpecularColor * 0.45;
	SpecularColor = 0;
}

void EnvBRDFApproxFullyRough(inout half3 DiffuseColor, inout half3 F0, inout half3 F90)
{
	DiffuseColor += F0 * 0.45;
	F0 = F90 = 0;
}

void PBR_Half(half3 envMapColor, half NoV, inout SurfaceHalf surface, inout half3 specular)
{
	half3 originalDiffuse = surface.Albedo;

	surface.Albedo = originalDiffuse - originalDiffuse * surface.Metallic;

	specular = lerp(half3(0.02, 0.02, 0.02), originalDiffuse, surface.Metallic);
	half3 envSpecColor = EnvBRDFApprox(specular, surface.Roughness, NoV);
	specular = envSpecColor * envMapColor;
}

half dgx_luminance(half3 color)
{
	return (color.r * 0.3) + (color.g * 0.59) + (color.b * 0.11);
}

half4 LightingPBR_Half(SurfaceHalf surface, half3 worldViewDir, half3 envMapColor)
{
	half NoV = saturate(dot(surface.NormalWS, worldViewDir));

	half3 specular = 0;
	PBR_Half(envMapColor, NoV, surface, specular);

	half4 result = half4(surface.Albedo, surface.Alpha);
	//result.rgb *= surface.AmbientLight;
	result.rgb += specular;

	#if defined(DGX_TRANSPARENT) 
	result.a = saturate(max(result.a, dgx_luminance(specular)));
	#endif

	return result;
}

#endif