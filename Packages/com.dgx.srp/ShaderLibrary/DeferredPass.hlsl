//#include <HLSLSupport.cginc>
#ifndef DGX_DEFERRED_INCLUDED
#define DGX_DEFERRED_INCLUDED

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float4 positionCS : SV_POSITION;
    float3 screenUV : TEXCOORD0;
    float3 viewDir : TEXCOORD1;
    //float2 pixelCoords : TEXCOORD2;
};

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Common.hlsl"
#include "Lighting.hlsl"
#include "PBR.hlsl"
#include "SpotLights.hlsl"

#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P OptimizeProjectionMatrix(glstate_matrix_projection)
#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_V     unity_MatrixV
#define UNITY_MATRIX_I_V   unity_MatrixInvV
#define UNITY_MATRIX_I_P   _InvProjMatrix

float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_MatrixP;
float4x4 _InvProjMatrix;
float4x4 unity_MatrixV;
float4x4 unity_MatrixInvV;
float4x4 unity_MatrixInvVP;
float4x4 _ScreenToWorld;
float4x4 glstate_matrix_projection;

sampler2D _GT0;
sampler2D _GT1;
sampler2D _GT2;

float4 _WorldSpaceLightPos0;
float4 _WorldSpaceCameraPos;
float4 _ZBufferParams;
float4 unity_FogParams;
real4  unity_FogColor;
float4 _ProjectionParams;
float4 _DrawScale;
half4 _SubtractiveShadowColor;

// x - shadow intensity, y - shadow distance
float4 dgx_MainLightShadowParams;

half4       _MainLightShadowOffsets0;
half4       _MainLightShadowOffsets1;

// low left
// upper left
// upper right
// lower right
float4x4 _WorldSpaceFrustumCorners;

TEXTURE2D(tg_ReflProbes_Atlas);
SAMPLER(samplertg_ReflProbes_Atlas);
float4 tg_ReflProbes_MipScaleOffset[32 * 7];

//TEXTURECUBE_ARRAY(tg_ReflectionProbes);
//SAMPLER(samplertg_ReflectionProbes);
float4 tg_ReflectionProbeDecodeInstructions;

float4x4 _ShadowVP;
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

#define UNITY_DECLARE_TEX2D_FLOAT(tex) TEXTURE2D_FLOAT(tex); SAMPLER(sampler##tex)
UNITY_DECLARE_TEX2D_FLOAT(_Depth);
UNITY_DECLARE_TEX2D_FLOAT(_LinearDepth);

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

float ComputeFogDistance(float depth)
{
    float dist = depth * _ProjectionParams.z;
    dist -= _ProjectionParams.y;
    return dist;
}

float lerpCorner(float2 screenUV, int axis)
{
    float3 lowerLeftCorner = _WorldSpaceFrustumCorners[0].xyz;
    float3 upperLeftCorner = _WorldSpaceFrustumCorners[1].xyz;
    float3 upperRightCorner = _WorldSpaceFrustumCorners[2].xyz;
    float3 lowerRightCorner = _WorldSpaceFrustumCorners[3].xyz;

    float lowerX = lerp(lowerLeftCorner[axis], lowerRightCorner[axis], screenUV.x);
    float upperX = lerp(upperLeftCorner[axis], upperRightCorner[axis], screenUV.x);

    return lerp(lowerX, upperX, screenUV.y);
}

v2f vert (appdata v)
{
    v2f o;
    //o.vertex = mul(UNITY_MATRIX_VP, mul(UNITY_MATRIX_M, float4(v.vertex.xyz, 1.0)));
    //_DrawScale.xy = 1;
    o.positionCS = float4(v.vertex.x * _DrawScale.x, v.vertex.y * _DrawScale.y, UNITY_RAW_FAR_CLIP_VALUE, 1.0); // Force triangle to be on zfar
    o.positionCS.x -= 1;
    o.positionCS.y -= 1;

    o.screenUV.xyz = o.positionCS.xyw;
    
    #if UNITY_UV_STARTS_AT_TOP
    o.screenUV.xy = o.screenUV.xy * float2(0.5, -0.5) + 0.5 * o.screenUV.z;
    #else
    o.screenUV.xy = o.screenUV.xy * 0.5 + 0.5 * o.screenUV.z;
    #endif

    o.screenUV.x /= _DrawScale.x;
    o.screenUV.y /= +_DrawScale.y;

    //o.pixelCoords.x = o.screenUV.x * _DrawScale.x;
    //o.pixelCoords.y = o.screenUV.y * _DrawScale.y;
    
    return o;
}

// half3 Sample_ReflectionProbe_half(half3 viewDir, half3 normalWS, half index, half lod)
// {
//     half3 reflectVec = reflect(-viewDir, normalWS);
//
//     float4 color = SAMPLE_TEXTURECUBE_ARRAY_LOD(tg_ReflectionProbes, samplertg_ReflectionProbes, reflectVec, index, lod);
//     return DecodeHDREnvironment(color, tg_ReflectionProbeDecodeInstructions);
// }

half3 SampleReflectionProbeAtlas(half3 reflectVector, half probeIndex, half mip)
{
    half3 sampleVector = reflectVector;
    
    half2 uv = saturate(PackNormalOctQuadEncode(sampleVector) * 0.5 + 0.5);

    half mip0 = floor(mip);
    float4 scaleOffset0 = tg_ReflProbes_MipScaleOffset[floor(probeIndex) * 7 + (uint)mip0];

    return half4(SAMPLE_TEXTURE2D_LOD(tg_ReflProbes_Atlas, samplertg_ReflProbes_Atlas, uv * scaleOffset0.xy + scaleOffset0.zw, 0.0)).rgb;
}

float3 WorldSpacePositionFromDepth(float2 screenUV, float rawDepth)
{
    #if UNITY_REVERSED_Z
    float deviceDepth = rawDepth;
    #else
    float deviceDepth = rawDepth * 2.0 - 1.0;
    #endif
    
    float4 positionCS = float4(screenUV.xy * 2 - 1, deviceDepth, 1);
    float4 hpositionWS = mul(unity_MatrixInvVP, positionCS);
    return hpositionWS.xyz / hpositionWS.w;
}

half hash(half x)
{
    return frac(x * 43758.5453);
}

half3 randomnormal_tangent(half3 x)
{
    half3 normal;

    normal.x = hash(x.x);
    normal.y = hash(x.y);
    normal.z = hash(x.z);

    normal = normalize(normal);

    normal.y = abs(normal.y);

    return normal;
}

void softNormalBias(float3 main, inout half3 normalBias)
{
    half3 softBias;
    
    softBias.x = hash(main.x);
    softBias.y = hash(main.y);
    softBias.z = hash(main.z);
    
    softBias = normalize(softBias);
    softBias = abs(softBias);
    normalBias += softBias * 0.02;
    //
    //normalBias += randomnormal_tangent(surface.NormalWS) * 0.02;
}

half SampleShadow(half3 positionSTS, half2 offset)
{
    //return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS + half3(offset / 1024, 0));
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS + half3(offset, 0));
}

void drawShadows(
    float3 worldPos,
    //float2 pixelCoords,
    half viewDirDistanceSq, SurfaceHalf surface, inout half3 result)
{
    half shadowDistance = dgx_MainLightShadowParams.y;
    half3 normalBias = dgx_MainLightShadowParams.z * surface.NormalWS;

    // плохо работает на мобиле, тестил на vulkan (samsung s20)
    //softNormalBias(worldPos, normalBias);
    
    half3 positionSTS = mul(
        _ShadowVP,
        float4(worldPos.xyz + normalBias, 1.0)
    ).xyz;

    half4 attenuation4;
    attenuation4.x = SampleShadow(positionSTS.xyz, _MainLightShadowOffsets0.xy);
    attenuation4.y = SampleShadow(positionSTS.xyz, _MainLightShadowOffsets0.zw);
    attenuation4.z = SampleShadow(positionSTS.xyz, _MainLightShadowOffsets1.xy);
    attenuation4.w = SampleShadow(positionSTS.xyz, _MainLightShadowOffsets1.zw);

    // half2 offset = (float)(frac(pixelCoords.xy * 0.5) > 0.25);
    // if (offset.y > 1.1)
    //     offset.y = 0;

    // attenuation4.x = SampleShadow(positionSTS.xyz, offset + half2(-1.5, 0.5));
    // attenuation4.y = SampleShadow(positionSTS.xyz, offset + half2(0.5, 0.5));
    // attenuation4.z = SampleShadow(positionSTS.xyz, offset + half2(-1.5, -1.5));
    // attenuation4.w = SampleShadow(positionSTS.xyz, offset + half2(0.5, -1.5));
    
    half shadowAtten = dot(attenuation4, 0.25);
    

    half shadowIntensity = dgx_MainLightShadowParams.x;

    // remove shadowmap on back surface
    half3 L = normalize(half3(_WorldSpaceLightPos0.xyz));
    half NdotL = dot(surface.NormalWS, L);
    shadowIntensity *= saturate(NdotL + 0.1);
    
    half fadeSize = 0.1;
    half shadowDistanceSq = shadowDistance * shadowDistance;
    half distDiff = shadowDistanceSq - viewDirDistanceSq;
    half shadowFadePart = shadowDistanceSq * fadeSize;
    half distanceFade = saturate(distDiff / shadowFadePart);
    
    //return half4(shadowAtten, distanceFade, 0,0);
    shadowAtten = lerp(1, shadowAtten, shadowIntensity * distanceFade);
    
    result *= lerp(_SubtractiveShadowColor.rgb, half3(1,1,1), shadowAtten);
}

half4 frag (v2f i) : SV_Target
{
    float2 screen_uv = (i.screenUV.xy / i.screenUV.z);

    // Gbuffer
    float4 g0 = tex2D(_GT0, screen_uv.xy);
    float4 g1 = tex2D(_GT1, screen_uv.xy);

    #ifdef DGX_PBR_RENDERING
    float4 g2 = tex2D(_GT2, screen_uv.xy);
    #else
    float4 g2 = 0;
    #endif

    SurfaceHalf surface = GBufferToSurfaceHalf(g0, g1, g2);
    
    
    float rawDepth = _Depth.Sample(sampler_Depth, screen_uv.xy).r;
    
    float3 worldPos = WorldSpacePositionFromDepth(screen_uv, rawDepth);
    float3 viewDirWithDistance = worldPos.xyz - _WorldSpaceCameraPos.xyz;

    #ifdef DGX_SHADOWS_ENABLED
    half viewDirDistanceSq = dot(viewDirWithDistance,viewDirWithDistance);
    drawShadows(
        worldPos,
        //i.pixelCoords,
        viewDirDistanceSq, surface, surface.AmbientLight);
    #endif

    #ifdef DGX_PBR_RENDERING
    half3 viewDir = normalize(viewDirWithDistance);
    #else
    float3 viewDir = 0;
    #endif

    half3 specular = 0;
    
    #ifdef DGX_SPOT_LIGHTS
    calculateSpotLightForSurface(worldPos, viewDir, surface, specular);
    #endif
    
    #ifdef DGX_PBR_RENDERING
    half3 reflectVec = reflect(viewDir, surface.NormalWS);
    half3 envMapColor = SampleReflectionProbeAtlas(reflectVec, surface.EnvCubemapIndex, surface.Roughness * 6.99);
    envMapColor += specular; 
    #ifdef DGX_DARK_MODE
    envMapColor *= surface.AmbientLight;
    #endif
    
    half4 result = LightingPBR_Half(surface, -viewDir, envMapColor);
    #else
    half4 result = half4(surface.Albedo * surface.AmbientLight, surface.Alpha);
    #endif

    float linDepth = Linear01Depth(rawDepth, _ZBufferParams);
    #ifdef DGX_FOG_ENABLED
    //float linDepth = _LinearDepth.Sample(sampler_LinearDepth, screen_uv.xy).r;
    float dist = ComputeFogDistance(linDepth);
    half fog = ComputeFogFactorZ0ToFar(dist);
    result.rgb = MixFog(result.rgb, unity_FogColor.rgb, fog);
    #endif
    //result.rgb = worldPos.xyz;
    result.a = (1.0 - linDepth) * 4;

    return result; 
}



// half4 fragFog(v2f i) : SV_Target
// {
//     //UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
//
// // #if _RENDER_PASS_ENABLED
// //     float d = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, input.positionCS.xy).x;
// // #else
// //     float d = LOAD_TEXTURE2D_X(_CameraDepthTexture, input.positionCS.xy).x;
// // #endif
//     float2 screen_uv = (i.screenUV.xy / i.screenUV.z);
//
//     #if UNITY_REVERSED_Z
//     float depth = _Depth.Sample(sampler_PointClamp, screen_uv).r;
//     
//     float eye_z = Linear01Depth(depth, _ZBufferParams);
//     #else
//     // Adjust z to match NDC for OpenGL
//     float depth = Linear01Depth(_Depth.Load(sampler_PointClamp, screen_uv).r, _ZBufferParams);
//     eye_z = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
//     #endif
//     
//     // float d = LOAD_TEXTURE2D(_Depth, screen_uv.xy).x;
//     // eye_z = LinearEyeDepth(d, _ZBufferParams);
//     
//     float clip_z = UNITY_MATRIX_P[2][2] * -eye_z + UNITY_MATRIX_P[2][3];
//     half fogFactor = ComputeFogFactor(clip_z);
//     half fogIntensity = ComputeFogIntensity(fogFactor);
//     
//     return half4(unity_FogColor.rgb, fogIntensity);
// }

#endif