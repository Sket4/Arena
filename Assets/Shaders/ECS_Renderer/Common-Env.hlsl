#ifndef UG_COMMON_ENV_INCLUDED
#define UG_COMMON_ENV_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.tzargames.rendering/Shaders/Lighting.hlsl"
#include "Common.hlsl"

sampler2D _BaseMap;
sampler2D _SurfaceMap;
sampler2D _BumpMap;
sampler2D _MetallicGlossMap;

#if defined(DOTS_INSTANCING_ON)
UNITY_DOTS_INSTANCING_START(UserPropertyMetadata)
	//UNITY_DOTS_INSTANCED_PROP_OVERRIDE_REQUIRED(float4, tg_CommonInstanceData)
UNITY_DOTS_INSTANCING_END(UserPropertyMetadata)
#endif

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
	float3 bitangentWS : TEXCOORD4;
	float4 positionWS_fog : TEXCOORD5;

	half alpha : TEXCOORD6;
#if LIGHTMAP_ON
	TG_DECLARE_LIGHTMAP_UV(7)
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

v2f env_vert (appdata v)
{
	v2f o;
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_TRANSFER_INSTANCE_ID(v, o);

	float3 positionOS = v.vertex.xyz;
	float3 normalOS = v.normal;
	float4 tangentOS = v.tangent;

	VertexPositionInputs vertInputs = GetVertexPositionInputs(positionOS);    //This function calculates all the relative spaces of the objects vertices
	o.vertex = vertInputs.positionCS;
	o.positionWS_fog.xyz = vertInputs.positionWS;

	o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
	o.positionWS_fog.w = ComputeFogFactor(o.vertex.z);

	float4 instanceData = tg_InstanceData;
	o.instanceData = instanceData;

	VertexNormalInputs normalInputs = GetVertexNormalInputs(normalOS, tangentOS);

	o.normalWS = normalInputs.normalWS;
	o.tangentWS = float4(normalInputs.tangentWS, tangentOS.w);
	o.bitangentWS = normalInputs.bitangentWS;

	#if LIGHTMAP_ON
	TG_TRANSFORM_LIGHTMAP_TEX(v.lightmapUV, o.lightmapUV)
	#endif

	#if USE_UNDERWATER
	o.alpha = v.color.r;
	#else
	o.alpha = 1;
	#endif

	#if USE_HEIGHT_FOG
	o.positionWS_fog.w *= saturate((_FogHeight - o.positionWS_fog.y) * _HeightFogFade); 
	#endif

	return o;
}

half4 env_frag(v2f i) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(i);

	half4 diffuse = tex2D(_BaseMap, i.uv) * _BaseColor;

	#if defined(TG_USE_ALPHACLIP)
	clip(diffuse.a - _Cutoff);
	#endif

	half3 normalTS = UnpackNormal(tex2D(_BumpMap, i.uv));
	//normalTS.xy *= 2;
	half3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.positionWS_fog.xyz);

	half3x3 tangentToWorld = half3x3(i.tangentWS.xyz, i.bitangentWS.xyz, i.normalWS.xyz); 

	half3 normalWS = TransformTangentToWorld(normalTS.xyz, tangentToWorld, true);

	half3 ambientLight = ARENA_COMPUTE_AMBIENT_LIGHT(i, normalWS);
	
	#if USE_SURFACE_BLEND
	float2 surfaceUV = TRANSFORM_TEX(i.positionWS_fog.xz, _SurfaceMap);
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

	half3 envMapColor = TG_ReflectionProbe_half(viewDirWS, normalWS, i.instanceData.y,roughness * 4);

	half3 remEnvMapColor = clamp(envMapColor - 0.5, 0, 10);
	remEnvMapColor = remEnvMapColor * _HighlightRemove;
	remEnvMapColor = envMapColor - remEnvMapColor;

	half lum = tg_luminance(ambientLight);
	envMapColor = lerp(remEnvMapColor, envMapColor, saturate(lum * lum * lum));

	ARENA_DYN_LIGHT(normalWS, i.positionWS_fog.xyz, ambientLight, viewDirWS, envMapColor, true);

	half4 finalColor = LightingPBR(diffuse, ambientLight, viewDirWS, normalWS, mesm.rrr, roughness, envMapColor);

	#else

	half4 finalColor = diffuse;
	half3 envMapColor = 0;
	ARENA_DYN_LIGHT(normalWS, i.positionWS_fog.xyz, ambientLight, 0,0, true); 
	finalColor.rgb *= ambientLight;

	#endif

	#if USE_UNDERWATER
	finalColor.rgb = lerp(finalColor.rgb, _Underwater_color.rgb * ambientLight, i.alpha);
	#endif


	// apply fog
	return half4(MixFog(finalColor.rgb, i.positionWS_fog.w), finalColor.a);
}

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
	output.positionCS = TransformObjectToHClip(positionOS);
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

#endif //UG_COMMON_ENV_INCLUDED

