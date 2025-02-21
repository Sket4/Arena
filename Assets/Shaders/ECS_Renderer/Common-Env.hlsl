#ifndef UG_COMMON_ENV_INCLUDED
#define UG_COMMON_ENV_INCLUDED

#ifdef TG_USE_URP
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#endif
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.tzargames.rendering/Shaders/Lighting.hlsl"
#include "Common.hlsl"

//#ifdef ARENA_DEFERRED
#include "Packages/com.dgx.srp/ShaderLibrary/Lighting.hlsl"
//#endif

#if defined(DOTS_INSTANCING_ON)
	 UNITY_DOTS_INSTANCING_START(UserPropertyMetadata)
#ifdef USE_BASECOLOR_INSTANCE
			UNITY_DOTS_INSTANCED_PROP_OVERRIDE_REQUIRED(float4, _BaseColor)
#endif
	 UNITY_DOTS_INSTANCING_END(UserPropertyMetadata)
#endif

#if defined(DOTS_INSTANCING_ON) & defined(USE_BASECOLOR_INSTANCE)
#define BASE_COLOR UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, _BaseColor)
#else
#define BASE_COLOR _BaseColor
#endif



sampler2D _BaseMap;
sampler2D _SurfaceMap;
sampler2D _BumpMap;
sampler2D _MetallicGlossMap;

struct appdata
{
	float4 vertex : POSITION;
	float3 normal : NORMAL;
	float4 tangent : TANGENT;
	float2 uv : TEXCOORD0;
	
#if LIGHTMAP_ON
	TG_DECLARE_LIGHTMAP_UV(1)
#endif

	#if USE_UNDERWATER
	half4 color : COLOR;
	#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
	half4 vertex : SV_POSITION;
	float2 uv : TEXCOORD0;
	TG_DECLARE_INSTANCE_DATA(1)

	float3 normalWS : TEXCOORD2;
	float4 tangentWS : TEXCOORD3;

	#if USE_UNDERWATER
	half UnderwaterFade : TEXCOORD4;
	#endif
	
#if LIGHTMAP_ON
	TG_DECLARE_LIGHTMAP_UV(5)
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID

	#ifdef USE_SURFACE_BLEND
	float2 surfaceBlendUV : TEXCOORD6;
	#endif

	#ifndef ARENA_DEFERRED
	float4 positionWS : TEXCOORD7;
	#endif
};

v2f env_vert (appdata v)
{
	v2f o;
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_TRANSFER_INSTANCE_ID(v, o);

	float3 positionOS = v.vertex.xyz;
	float3 normalOS = v.normal;
	float4 tangentOS = v.tangent;

	float3 positionWS = TransformObjectToWorld(positionOS);
	o.vertex = TransformWorldToHClip(positionWS);
	
	#ifdef ARENA_DEFERRED
	#ifdef USE_SURFACE_BLEND
	o.surfaceBlendUV = TRANSFORM_TEX(positionWS.xz, _SurfaceMap);
	#endif
	
	#else
	o.positionWS.xyz = positionWS;
	o.positionWS.w = ComputeFogFactor(o.vertex.z);
	#endif

	o.uv = TRANSFORM_TEX(v.uv, _BaseMap);

	float3 normalWS = TransformObjectToWorldNormal(normalOS);
	real3 tangentWS = real3(TransformObjectToWorldDir(tangentOS.xyz));

	float4 instanceData = tg_InstanceData;
	o.instanceData = instanceData;

	o.normalWS = normalWS;
	o.tangentWS = float4(tangentWS, tangentOS.w);

	#if LIGHTMAP_ON
	TG_TRANSFORM_LIGHTMAP_TEX(v.lightmapUV, o.lightmapUV)
	#endif

	#if USE_UNDERWATER
	o.UnderwaterFade = v.color.r;
	#endif

	// #if USE_HEIGHT_FOG
	// o.positionWS.w *= saturate((_FogHeight - o.positionWS_fog.y) * _HeightFogFade); 
	// #endif

	return o;
}
#ifdef ARENA_DEFERRED
GBufferFragmentOutput env_frag_deferred(v2f i)
{
	UNITY_SETUP_INSTANCE_ID(i);

	half4 diffuse = tex2D(_BaseMap, i.uv) * BASE_COLOR;

	#if defined(TG_USE_ALPHACLIP)
	clip(diffuse.a - _Cutoff);
	#endif

	#if defined(UG_QUALITY_MED) || defined(UG_QUALITY_HIGH) || defined(USE_SURFACE_BLEND)
	half3 normalTS = UnpackNormal(tex2D(_BumpMap, i.uv));
	real sign = real(i.tangentWS.w);
	real3 bitangentWS = real3(cross(i.normalWS, i.tangentWS.xyz)) * sign;
	half3x3 tangentToWorld = half3x3(i.tangentWS.xyz, bitangentWS, i.normalWS.xyz); 
	half3 normalWS = TransformTangentToWorld(normalTS.xyz, tangentToWorld, true);
	#else
	half3 normalWS = i.normalWS;
	#endif

	half3 ambientLight = ARENA_COMPUTE_AMBIENT_LIGHT(i, normalWS);
	
	#if USE_SURFACE_BLEND
	half4 surfaceColor = tex2D(_SurfaceMap, i.surfaceBlendUV);
	float surfaceBlend = saturate(dot(half3(normalWS.x, normalWS.y, normalWS.z), half3(0.0,1,0.0)));

	// pow 4
	surfaceBlend *= surfaceBlend;
	surfaceBlend  *= surfaceBlend;

	diffuse.rgb = lerp(diffuse.rgb, surfaceColor.rgb, surfaceBlend  * _SurfaceBlendFactor);
	#endif

	SurfaceHalf surface;
	surface.Albedo = diffuse.rgb;
	surface.Alpha = diffuse.a;
	surface.AmbientLight = ambientLight;
	
	
	#if defined(UG_QUALITY_MED) || defined(UG_QUALITY_HIGH)
	half4 mesm = tex2D(_MetallicGlossMap, i.uv);
	surface.Metallic = mesm.r * _Metallic; 
		
	half smoothness = mesm.a * _Smoothness;
	surface.Roughness = 1 - smoothness;

	surface.EnvCubemapIndex = i.instanceData.y;
	
	
	#else
	surface.Metallic = 0;
	surface.Roughness = 1;
	surface.EnvCubemapIndex = 0;

	#endif
	
	surface.Albedo *= surface.AmbientLight;
	surface.AmbientLight = 1;
	surface.NormalWS = normalWS;
	
	
	#if USE_UNDERWATER
	surface.Albedo = lerp(surface.Albedo, _Underwater_color.rgb * ambientLight, i.UnderwaterFade);
	surface.Roughness = lerp(surface.Roughness, 1, i.UnderwaterFade);
	#endif

	return SurfaceToGBufferOutputHalf(surface);
}
#endif

#ifndef ARENA_DEFERRED
half4 env_frag(v2f i) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(i);

	half4 diffuse = tex2D(_BaseMap, i.uv) * BASE_COLOR;

	#if defined(TG_USE_ALPHACLIP)
	clip(diffuse.a - _Cutoff);
	#endif

	half3 normalTS = UnpackNormal(tex2D(_BumpMap, i.uv));
	//normalTS.xy *= 2;

	real sign = real(i.tangentWS.w);
	real3 bitangentWS = real3(cross(i.normalWS, i.tangentWS.xyz)) * sign;
	
	half3x3 tangentToWorld = half3x3(i.tangentWS.xyz, bitangentWS.xyz, i.normalWS.xyz); 

	half3 normalWS = TransformTangentToWorld(normalTS.xyz, tangentToWorld, true);

	half3 ambientLight = ARENA_COMPUTE_AMBIENT_LIGHT(i, normalWS);
	
	#if USE_SURFACE_BLEND
	float2 surfaceUV = TRANSFORM_TEX(i.positionWS.xz, _SurfaceMap);
	half4 surfaceColor = tex2D(_SurfaceMap, surfaceUV);
	float surfaceBlend = saturate(dot(half3(normalWS.x, normalWS.y, normalWS.z), half3(0.0,1,0.0)));

	// pow 4
	surfaceBlend *= surfaceBlend;
	surfaceBlend  *= surfaceBlend;

	diffuse.rgb = lerp(diffuse.rgb, surfaceColor.rgb, surfaceBlend  * _SurfaceBlendFactor);
	#endif

	#if defined(UG_QUALITY_MED) || defined(UG_QUALITY_HIGH)
	half4 mesm = tex2D(_MetallicGlossMap, i.uv);
	mesm.r *= _Metallic;

	half smoothness = mesm.a * _Smoothness;
	half roughness = 1 - smoothness;

	half3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.positionWS.xyz);
	half3 envMapColor = TG_ReflectionProbe_half(viewDirWS, normalWS, i.instanceData.y,roughness * 4);

	#ifdef USE_SPECULAR_MULT
	envMapColor = lerp(envMapColor * smoothness, envMapColor, _SpecularMultiplier);
	#endif

	half3 remEnvMapColor = clamp(envMapColor - 0.5, 0, 10);
	remEnvMapColor = remEnvMapColor * _HighlightRemove;
	remEnvMapColor = envMapColor - remEnvMapColor;

	half lum = tg_luminance(ambientLight);
	envMapColor = lerp(remEnvMapColor, envMapColor, saturate(lum * lum * lum));

	ARENA_DYN_LIGHT(normalWS, i.positionWS.xyz, ambientLight, viewDirWS, envMapColor, true);

	half4 finalColor = LightingPBR(diffuse, ambientLight, viewDirWS, normalWS, mesm.rrr, roughness, envMapColor);

	#else

	half4 finalColor = diffuse;
	half3 envMapColor = 0;
	ARENA_DYN_LIGHT(normalWS, i.positionWS.xyz, ambientLight, 0,0, true); 
	finalColor.rgb *= ambientLight;

	#endif

	#if USE_UNDERWATER
	finalColor.rgb = lerp(finalColor.rgb, _Underwater_color.rgb * ambientLight, i.UnderwaterFade);
	#endif

	// apply fog
	return half4(MixFog(finalColor.rgb, unity_FogColor.rgb, i.positionWS.w), finalColor.a);
}
#endif

struct appdata_depthonly
{
	float3 vertex : POSITION;
	//float3 normal : NORMAL;
	//float4 tangent : TANGENT;
	float2 uv : TEXCOORD0;
	
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f_depthonly
{
	half4 positionCS : SV_POSITION;
	float2 uv : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

v2f_depthonly DepthOnlyVertex(appdata_depthonly v)
{
	v2f_depthonly output = (v2f_depthonly)0;
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_TRANSFER_INSTANCE_ID(v, output);
	//UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	float3 positionOS = v.vertex;

	#if defined(TG_USE_ALPHACLIP)
	output.uv = TRANSFORM_TEX(v.uv, _BaseMap);
	#endif
	output.positionCS = mul(UNITY_MATRIX_VP, mul(UNITY_MATRIX_M, float4(positionOS, 1.0)));
	return output;
}

half DepthOnlyFragment(v2f_depthonly input) : SV_TARGET
{
	UNITY_SETUP_INSTANCE_ID(input);
	//UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

	#if defined(TG_USE_ALPHACLIP)
	//Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
	#endif

	#if defined(LOD_FADE_CROSSFADE)
	LODFadeCrossFade(input.positionCS);
	#endif

	return input.positionCS.z;
}

#ifdef ARENA_META_PASS 
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/MetaPass.hlsl"
             
half4 FragmentMeta(v2f fragIn) : SV_Target
{
	UnityMetaInput metaInput;

	half4 diffuse = tex2D(_BaseMap, fragIn.uv) * _BaseColor;
                
	metaInput.Albedo = diffuse.rgb;
	metaInput.Emission = _EmissionColor.rgb;
                
	#ifdef EDITOR_VISUALIZATION
	metaInput.VizUV = fragIn.VizUV;
	metaInput.LightCoord = fragIn.LightCoord;
	#endif

	half4 result = UnityMetaFragment(metaInput);
	return result;
}
#endif

#endif //UG_COMMON_ENV_INCLUDED

