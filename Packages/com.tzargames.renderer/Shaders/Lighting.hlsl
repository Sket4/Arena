#ifndef TG_LIGHTING_INCLUDED
#define TG_LIGHTING_INCLUDED

TEXTURECUBE(_ReflProbe0);
SAMPLER(sampler_ReflProbe0);
float _ReflProbe0_Intensity;

float3 TG_ReflectionProbe(float3 viewDir, float3 normalOS, float lod)
{
	float3 reflectVec = reflect(-viewDir, normalOS);
	return SAMPLE_TEXTURECUBE_LOD(_ReflProbe0, sampler_ReflProbe0, reflectVec, lod);
	//return DecodeHDREnvironment(SAMPLE_TEXTURECUBE_LOD(_ReflProbe0, sampler_ReflProbe0, reflectVec, lod), unity_SpecCube0_HDR);
}

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
	float F90 = saturate(50.0 * SpecularColor.g);

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

void PBR(half3 envMapColor, half3 metallic, half roughness, half NoV, inout half3 diffuse, inout half3 specular)
{
	half3 originalDiffuse = diffuse;

	diffuse = originalDiffuse - originalDiffuse * metallic;

	specular = lerp(half3(0.02, 0.02, 0.02), originalDiffuse, metallic);
	half3 envSpecColor = EnvBRDFApprox(specular, roughness, NoV);
	specular = envSpecColor * envMapColor;
}

half tg_luminance(half3 color)
{
	return (color.r * 0.3) + (color.g * 0.59) + (color.b * 0.11);
}

half4 LightingPBR(half4 baseColor, half3 ambientColor, float3 worldViewDir, float3 normalWS, half3 metallic, half roughness)
{
	half3 envMapColor = TG_ReflectionProbe(worldViewDir, normalWS, roughness * 4);
	envMapColor *= _ReflProbe0_Intensity;
	half NoV = saturate(dot(normalWS, worldViewDir));

	half3 specular = 0;
	half4 diffuse = baseColor;
	PBR(envMapColor, metallic, roughness, NoV, diffuse.rgb, specular);

	half3 lighting = ambientColor;

#ifdef USE_LIGHTING
	Light light = GetMainLight();
	half NoL = saturate(dot(normalWS, light.direction));
	lighting += light.color * NoL;
#endif
	diffuse.rgb *= lighting;

	diffuse.rgb += specular;

	 
	diffuse.a = saturate(max(diffuse.a, tg_luminance(specular)));

	return diffuse;
}

#endif