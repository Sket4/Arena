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

float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_MatrixP;
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



TEXTURECUBE_ARRAY(tg_ReflectionProbes);
SAMPLER(samplertg_ReflectionProbes);
float4 tg_ReflectionProbeDecodeInstructions;



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

float4x4 OptimizeProjectionMatrix(float4x4 M)
{
    // Matrix format (x = non-constant value).
    // Orthographic Perspective  Combined(OR)
    // | x 0 0 x |  | x 0 x 0 |  | x 0 x x |
    // | 0 x 0 x |  | 0 x x 0 |  | 0 x x x |
    // | x x x x |  | x x x x |  | x x x x | <- oblique projection row
    // | 0 0 0 1 |  | 0 0 x 0 |  | 0 0 x x |
    // Notice that some values are always 0.
    // We can avoid loading and doing math with constants.
    M._21_41 = 0;
    M._12_42 = 0;
    return M;
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
    
    return o;
}

half3 Sample_ReflectionProbe_half(half3 viewDir, half3 normalWS, half index, half lod)
{
    half3 reflectVec = reflect(-viewDir, normalWS);

    float4 color = SAMPLE_TEXTURECUBE_ARRAY_LOD(tg_ReflectionProbes, samplertg_ReflectionProbes, reflectVec, index, lod);
    return DecodeHDREnvironment(color, tg_ReflectionProbeDecodeInstructions);
}

half4 frag (v2f i) : SV_Target
{
    float2 screen_uv = (i.screenUV.xy / i.screenUV.z);

    // Gbuffer
    float4 g0 = tex2D(_GT0, screen_uv);
    float4 g1 = tex2D(_GT1, screen_uv);
    float4 g2 = tex2D(_GT2, screen_uv);

    SurfaceHalf surface = GBufferToSurfaceHalf(g0, g1, g2);
    
    #if UNITY_REVERSED_Z
        float depth = _LinearDepth.Sample(sampler_PointClamp, screen_uv).r;
        float linDepth = Linear01Depth(depth, _ZBufferParams);
    #else
        // Adjust z to match NDC for OpenGL
        float depth = _LinearDepth.Sample(sampler_PointClamp, screen_uv).r;
        float linDepth = Linear01Depth(depth, _ZBufferParams);
        linDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, linDepth);
    #endif

    // worldpos
    float4 ndcPos = float4(i.positionCS.xy, linDepth, 1);
    float4 worldPos = mul(_ScreenToWorld, ndcPos);
    worldPos.xyz *= rcp(worldPos.w);
    
    //float3 L = normalize(_WorldSpaceLightPos0.xyz);
    //surface.AmbientLight += saturate(dot(surface.NormalWS, L));
    
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

    result.rgb = MixFog(result.rgb, unity_FogColor.rgb, fogFactor);
    //result = saturate(dot(surface.NormalWS, V));
    //result.rgb = surface.AmbientLight;
    return result; 
}

half4 fragFog(v2f i) : SV_Target
{
    //UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

// #if _RENDER_PASS_ENABLED
//     float d = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, input.positionCS.xy).x;
// #else
//     float d = LOAD_TEXTURE2D_X(_CameraDepthTexture, input.positionCS.xy).x;
// #endif
    float2 screen_uv = (i.screenUV.xy / i.screenUV.z);

    #if UNITY_REVERSED_Z
    float depth = _Depth.Sample(sampler_PointClamp, screen_uv).r;
    
    float eye_z = Linear01Depth(depth, _ZBufferParams);
    #else
    // Adjust z to match NDC for OpenGL
    float depth = Linear01Depth(_Depth.Load(sampler_PointClamp, screen_uv).r, _ZBufferParams);
    eye_z = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
    #endif
    
    // float d = LOAD_TEXTURE2D(_Depth, screen_uv.xy).x;
    // eye_z = LinearEyeDepth(d, _ZBufferParams);
    
    float clip_z = UNITY_MATRIX_P[2][2] * -eye_z + UNITY_MATRIX_P[2][3];
    half fogFactor = ComputeFogFactor(clip_z);
    half fogIntensity = ComputeFogIntensity(fogFactor);
    
    return half4(unity_FogColor.rgb, fogIntensity);
}

#endif