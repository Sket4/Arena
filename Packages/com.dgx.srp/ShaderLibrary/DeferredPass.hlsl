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
};

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Common.hlsl"
#include "Lighting.hlsl"
#include "PBR.hlsl"

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
float4x4 _ScreenToWorld;
float4x4 glstate_matrix_projection;

sampler2D _GT0;
sampler2D _GT1;
sampler2D _GT2;

TEXTURE2D(_Depth);
TEXTURE2D(_LinearDepth);

float4 _WorldSpaceLightPos0;
float4 _WorldSpaceCameraPos;
float4 _ZBufferParams;
float4 unity_FogParams;
real4  unity_FogColor;
float4 _ProjectionParams;

// low left
// upper left
// upper right
// lower right
float4x4 _WorldSpaceFrustumCorners;

TEXTURECUBE_ARRAY(tg_ReflectionProbes);
SAMPLER(samplertg_ReflectionProbes);
float4 tg_ReflectionProbeDecodeInstructions;

float4x4 _ShadowVP;
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

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
    float clipZ_0Far = max(((1.0-(zPositionCS)/_ProjectionParams.y)*_ProjectionParams.z),0);
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
    o.positionCS = float4(v.vertex.xy, UNITY_RAW_FAR_CLIP_VALUE, 1.0); // Force triangle to be on zfar

    o.screenUV = o.positionCS.xyw;
    #if UNITY_UV_STARTS_AT_TOP
    o.screenUV.xy = o.screenUV.xy * float2(0.5, -0.5) + 0.5 * o.screenUV.z;
    //
    #else
    o.screenUV.xy = o.screenUV.xy * 0.5 + 0.5 * o.screenUV.z;
    #endif

    //o.screenUV.xy = DynamicScalingApplyScaleBias(o.screenUV.xy, float4(_RTHandleScale.xy, 0.0f, 0.0f));

    float3 corner;
    corner.x = lerpCorner(o.screenUV.xy, 0);
    corner.y = lerpCorner(o.screenUV.xy, 1);
    corner.z = lerpCorner(o.screenUV.xy, 2);

    o.viewDir = corner - _WorldSpaceCameraPos.xyz;
    
    return o;
}



half3 Sample_ReflectionProbe_half(half3 viewDir, half3 normalWS, half index, half lod)
{
    half3 reflectVec = reflect(-viewDir, normalWS);

    float4 color = SAMPLE_TEXTURECUBE_ARRAY_LOD(tg_ReflectionProbes, samplertg_ReflectionProbes, reflectVec, index, lod);
    return DecodeHDREnvironment(color, tg_ReflectionProbeDecodeInstructions);
}

float3 WorldPosFromDepth(float depth, float2 screenUV)
{
    float z = depth * 2.0 - 1.0;
    float4 clipSpacePosition = float4(screenUV * 2.0 - 1.0, z, 1.0);
    float4 viewSpacePosition = mul(UNITY_MATRIX_I_P, clipSpacePosition);

    // Perspective division
    viewSpacePosition /= viewSpacePosition.w;
    
    float4 worldSpacePosition = mul(UNITY_MATRIX_I_V, viewSpacePosition);

    return worldSpacePosition.xyz;
}

half4 frag (v2f i) : SV_Target
{
    float2 screen_uv = (i.screenUV.xy / i.screenUV.z);

    // Gbuffer
    float4 g0 = tex2D(_GT0, screen_uv);
    float4 g1 = tex2D(_GT1, screen_uv);
    //float4 g2 = tex2D(_GT2, screen_uv);

    SurfaceHalf surface = GBufferToSurfaceHalf(g0, g1);
    
    #if UNITY_REVERSED_Z
        float depth = _LinearDepth.Sample(sampler_PointClamp, screen_uv).r;
        float linDepth = Linear01Depth(depth, _ZBufferParams);
    #else
        // Adjust z to match NDC for OpenGL
        float depth = _LinearDepth.Sample(sampler_PointClamp, screen_uv).r;
        float linDepth = Linear01Depth(depth, _ZBufferParams);
        linDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, linDepth);
    #endif

    

    float3 worldPos = _WorldSpaceCameraPos.xyz + i.viewDir * linDepth;
    float3 V = normalize(_WorldSpaceCameraPos.xyz - worldPos.xyz);

    half3 envMapColor = Sample_ReflectionProbe_half(V, surface.NormalWS, surface.EnvCubemapIndex, surface.Roughness * 4);

    half4 result = LightingPBR_Half(surface, V, envMapColor);

    float eyeDepth = LinearEyeDepth(depth, _ZBufferParams);
    float clip_z = UNITY_MATRIX_P[2][2] * -eyeDepth + UNITY_MATRIX_P[2][3];
    half fogFactor = ComputeFogFactor(clip_z);
    
    //result.rgb = worldPos.xyz;
    //result.rgb = surface.NormalWS;
    //result.rgb = depth;
    // result.r = unity_FogParams.w * 0.875;
    // result.g = UNITY_MATRIX_P[2][3];
    // result.b = _ProjectionParams.z;
    //result.rgb = surface.EnvCubemapIndex;

    //result.rgb * _DirectionalShadowAtlas.Sample(sampler_PointClamp, screen_uv).r;

    

    
    
    half shadowDistance = 20;
    float3 normalBias = surface.NormalWS * 0.1;
    float3 positionSTS = mul(
        _ShadowVP,
        float4(worldPos.xyz + normalBias, 1.0)
    ).xyz;

    half shadow = SAMPLE_TEXTURE2D_SHADOW(
        _DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS
    ).r;

    half3 L = normalize(half3(_WorldSpaceLightPos0.xyz));
    half NdotL = saturate(dot(surface.NormalWS, -L));
    //surface.AmbientLight += ;
    
    half shadowStrength = lerp(0.5, 1, NdotL);
    half fadeSize = 0.05;
    
    half distanceFade = saturate((1 - eyeDepth / shadowDistance) / fadeSize);

    shadow = lerp(1, shadowStrength, (1-shadow) * distanceFade);
    
    result.rgb *= shadow;
    
    result.rgb = MixFog(result.rgb, unity_FogColor.rgb, fogFactor);
    //result.rgb = worldPos.xyz;
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