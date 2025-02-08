#ifndef UG_TERRAIN_COMMON_INCLUDED
#define UG_TERRAIN_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.tzargames.rendering/Shaders/Lighting.hlsl"
#include "Packages/com.dgx.srp/ShaderLibrary/Lighting.hlsl"
#include "Common.hlsl"

struct appdata
{
    float3 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float2 uv : TEXCOORD0;
    #if LIGHTMAP_ON
    TG_DECLARE_LIGHTMAP_UV(1)
#endif
                
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    half4 vertex : SV_POSITION;
    half2 splatmapUV : TEXCOORD0;
    
    TG_DECLARE_INSTANCE_DATA(1)

    float3 normalWS : TEXCOORD2;
    float4 tangentWS : TEXCOORD3;
    float4 positionWS_fog : TEXCOORD4;

    half2 layer1_uv : TEXCOORD5;
    half2 layer2_uv : TEXCOORD6;
    half2 layer3_uv : TEXCOORD7;
    #ifdef ARENA_USE_FOUR_CHANNEL
    half2 layer4_uv  : TEXCOORD8;
    #endif
    
#if LIGHTMAP_ON
    TG_DECLARE_LIGHTMAP_UV(9)
#endif
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

CBUFFER_START(UnityPerMaterial)
half4 _Layers_Tiling;
half _HighlightRemove;
half4 _UnderwaterColor;
half _WaterHeight;
half _WaterHeightFade;
CBUFFER_END

sampler2D _SplatMap;
sampler2D _Color1;
sampler2D _Normal1;
sampler2D _SAH1;

sampler2D _Color2;
sampler2D _Normal2;
sampler2D _SAH2;

sampler2D _Color3;
sampler2D _Normal3;
sampler2D _SAH3;

#ifdef ARENA_USE_FOUR_CHANNEL
sampler2D _Color4;
sampler2D _Normal4;
sampler2D _SAH4;
#endif

v2f vert (appdata v)
{ 
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o); 

    float3 positionOS = v.vertex;
    float3 normalOS = v.normal;
    float4 tangentOS = v.tangent;
    
    o.positionWS_fog.xyz = TransformObjectToWorld(positionOS);
    o.vertex = TransformWorldToHClip(o.positionWS_fog.xyz);

    o.splatmapUV = v.uv;
    
    o.positionWS_fog.w = ComputeFogFactor(o.vertex.z);

    float4 instanceData = tg_InstanceData;
    o.instanceData = instanceData;

    
    float3 normalWS = TransformObjectToWorldNormal(normalOS);
    real3 tangentWS = real3(TransformObjectToWorldDir(tangentOS.xyz));

    o.normalWS = normalWS;
    o.tangentWS = float4(tangentWS, tangentOS.w);

#if LIGHTMAP_ON
    TG_TRANSFORM_LIGHTMAP_TEX(v.lightmapUV, o.lightmapUV)
    //o.color.rgb = 0;
#endif

    //o.color.a = 1;

    float2 uv = v.uv;

    #ifdef ARENA_SCALE_UV_X
    uv.x += uv.x;
    #endif

    #ifdef ARENA_SCALE_UV_Y
    uv.y += uv.y;
    #endif
    
    o.layer1_uv = uv * _Layers_Tiling.x;
    o.layer2_uv = uv * _Layers_Tiling.y;
    o.layer3_uv = uv * _Layers_Tiling.z;
    #ifdef ARENA_USE_FOUR_CHANNEL
    o.layer4_uv = uv * _Layers_Tiling.w;
    #endif
    
    return o;
}

#ifdef ARENA_MAP_RENDER
#define DETILING(texture,original,uv) \
    half3 _##texture = tex2D(##texture, ##uv + half2(##original.x, ##original.y) * 10).rgb; \
    _##texture += tex2D(##texture, ##uv + half2(##original.z, ##original.x) * 10).rgb; \
    _##texture += tex2D(##texture, ##uv + half2(##original.y, ##original.z) * 10).rgb; \
    _##texture += tex2D(##texture, ##uv + half2(##original.z, ##original.y) * 10).rgb; \
    _##texture += tex2D(##texture, ##uv + half2(##original.x, ##original.z) * 10).rgb; \
    _##texture += tex2D(##texture, ##uv + half2(##original.y, ##original.x) * 10).rgb; \
    ##original = (_##texture + ##original) * (1.0 / 7);
#else
#define DETILING(texture,original,uv)
#endif

GBufferFragmentOutput frag(v2f i)
{
    UNITY_SETUP_INSTANCE_ID(i);

    half4 splat = tex2D(_SplatMap, i.splatmapUV);

    #ifdef ARENA_MAP_RENDER
    half smoothScale = 0.001;
    splat += tex2D(_SplatMap, i.splatmapUV + half2(smoothScale,smoothScale));
    splat += tex2D(_SplatMap, i.splatmapUV + half2(-smoothScale,-smoothScale));
    splat += tex2D(_SplatMap, i.splatmapUV + half2(smoothScale,-smoothScale));
    splat += tex2D(_SplatMap, i.splatmapUV + half2(-smoothScale,smoothScale));
    splat *= 0.2;
    #endif

    half3 color1 = tex2D(_Color1, i.layer1_uv).rgb;
    DETILING(_Color1, color1, i.layer1_uv)
    
    half3 diffuse = color1 * splat.x;
    half3 color2 = tex2D(_Color2, i.layer2_uv).rgb;
    DETILING(_Color2, color2, i.layer2_uv)
    
    diffuse += color2 * splat.y;
    half3 color3 = tex2D(_Color3, i.layer3_uv).rgb;
    DETILING(_Color3, color3, i.layer3_uv)
    diffuse += color3 * splat.z;

    #ifdef ARENA_USE_FOUR_CHANNEL
    half3 color4 = tex2D(_Color4, i.layer4_uv).rgb;
    DETILING(_Color4, color4, i.layer4_uv)
    diffuse += color4 * splat.w; 
    #else
    // wet sand
    diffuse += color2 * splat.w * 0.75;
    #endif

    #if defined(UG_QUALITY_MED) || defined(UG_QUALITY_HIGH)
    half3 normalTS = UnpackNormal(tex2D(_Normal1, i.layer1_uv)) * splat.r;
    half3 normal2 = UnpackNormal(tex2D(_Normal2, i.layer2_uv)); 
    normalTS += normal2 * splat.g;
    normalTS += UnpackNormal(tex2D(_Normal3, i.layer3_uv)) * splat.b;

    #ifdef ARENA_USE_FOUR_CHANNEL
    normalTS += UnpackNormal(tex2D(_Normal4, i.layer4_uv)) * splat.w;
    #else
    normalTS += normal2 * splat.w * 0.5;
    #endif

    normalTS = normalize(normalTS);
    real sign = real(i.tangentWS.w);
    real3 bitangentWS = real3(cross(i.normalWS, i.tangentWS.xyz)) * sign;
    half3x3 tangentToWorld = half3x3(i.tangentWS.xyz, bitangentWS.xyz, i.normalWS.xyz);
    half3 normalWS = TransformTangentToWorld(normalTS.xyz, tangentToWorld, true);
    #else
    half3 normalWS = i.normalWS;
    #endif
    
    SurfaceHalf surface;
    
    surface.NormalWS = normalWS;
    half3 ambientLight = ARENA_COMPUTE_AMBIENT_LIGHT(i, surface.NormalWS);
    surface.Albedo = diffuse.rgb * ambientLight;
    surface.AmbientLight = 1;
    surface.Alpha = 0;
    
    //diffuse.rgb = 1;
    #if !defined(ARENA_MAP_RENDER) && (defined(UG_QUALITY_MED) || defined(UG_QUALITY_HIGH))
    half4 Sm_AO_HGHT = tex2D(_SAH1, i.layer1_uv) * splat.r;
    half4 Sm_AO_HGHT_2 = tex2D(_SAH2, i.layer2_uv);
    Sm_AO_HGHT += Sm_AO_HGHT_2 * splat.g;
    Sm_AO_HGHT += tex2D(_SAH3, i.layer3_uv) * splat.b;

    #ifdef ARENA_USE_FOUR_CHANNEL
    Sm_AO_HGHT += tex2D(_SAH4, i.layer4_uv) * splat.b;
    #else
    // sand
    Sm_AO_HGHT += 0.7 * splat.a;
    #endif
    
    half roughness = 1.0 - Sm_AO_HGHT.r;

    surface.Roughness = roughness;
    surface.Metallic = 0;
    surface.EnvCubemapIndex = i.instanceData.y;
    // half3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.positionWS_fog.xyz);
    // half3 envMapColor = TG_ReflectionProbe_half(viewDirWS, normalWS, i.instanceData.y,roughness * 4);
    // envMapColor.rgb *= Sm_AO_HGHT.g;
    //
    // half3 remEnvMapColor = clamp(envMapColor - 0.5, 0, 10);
    // remEnvMapColor = remEnvMapColor * _HighlightRemove;
    // remEnvMapColor = envMapColor - remEnvMapColor;
    // half lum = tg_luminance(ambientLight);
    // envMapColor = lerp(remEnvMapColor, envMapColor, saturate(lum * lum * lum));

    
    //ARENA_DYN_LIGHT(normalWS, i.positionWS_fog.xyz, ambientLight, viewDirWS, envMapColor, true);
    //half4 finalColor = LightingPBR(diffuse, ambientLight, viewDirWS, normalWS, 0, roughness, envMapColor);
    
    #else
    surface.Metallic = 0;
    surface.Roughness = 1;
    surface.EnvCubemapIndex = 0;
    #endif

    #ifdef ARENA_UNDERWATER
    // water height
    half heightAlpha = saturate(_WaterHeight - i.positionWS_fog.y * _WaterHeightFade);
    surface.Albedo.rgb = lerp(surface.Albedo.rgb, _UnderwaterColor.rgb, heightAlpha);
    surface.Roughness = lerp(surface.Roughness, 1, heightAlpha);
    #endif

    return SurfaceToGBufferOutputHalf(surface);
}

#endif

