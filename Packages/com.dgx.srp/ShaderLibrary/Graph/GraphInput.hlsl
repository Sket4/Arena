#ifndef DGX_GRAPHINPUT_INCLUDED
#define DGX_GRAPHINPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

CBUFFER_START(UnityPerDraw)
// Space block Feature
float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
float4 unity_LODFade; // x is the fade value ranging within [0,1]. y is x quantized into 16 levels
real4 unity_WorldTransformParams; // w is usually 1.0, or -1.0 for odd-negative scale transforms

// Light Indices block feature
// These are set internally by the engine upon request by RendererConfiguration.
real4 unity_LightData;
real4 unity_LightIndices[2];

float4 unity_ProbesOcclusion;

// Reflection Probe 0 block feature
// HDR environment map decode instructions
real4 unity_SpecCube0_HDR;

// Lightmap block feature
float4 unity_LightmapST;
float4 unity_LightmapIndex;
float4 unity_DynamicLightmapST;

// SH block feature
real4 unity_SHAr;
real4 unity_SHAg;
real4 unity_SHAb;
real4 unity_SHBr;
real4 unity_SHBg;
real4 unity_SHBb;
real4 unity_SHC;
CBUFFER_END

#if defined(USING_STEREO_MATRICES)
CBUFFER_START(UnityStereoViewBuffer)
float4x4 unity_StereoMatrixP[2];
float4x4 unity_StereoMatrixInvP[2];
float4x4 unity_StereoMatrixV[2];
float4x4 unity_StereoMatrixInvV[2];
float4x4 unity_StereoMatrixVP[2];
float4x4 unity_StereoMatrixInvVP[2];

float4x4 unity_StereoCameraProjection[2];
float4x4 unity_StereoCameraInvProjection[2];

float3   unity_StereoWorldSpaceCameraPos[2];
float4   unity_StereoScaleOffset[2];
#endif

TEXTURECUBE(unity_SpecCube0);
SAMPLER(samplerunity_SpecCube0);


float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 unity_MatrixP;
float4 unity_FogParams;
real4  unity_FogColor;
float4 _ProjectionParams;
float3 _WorldSpaceCameraPos;
float4 unity_OrthoParams;

float4x4 unity_MatrixPreviousM;
float4x4 unity_MatrixPreviousMI;
float4x4 unity_MatrixInvV;
float4x4 glstate_matrix_projection;
float4 _ScaledScreenParams;

#define UNITY_MATRIX_M     unity_ObjectToWorld
#define UNITY_MATRIX_I_M   unity_WorldToObject
#define UNITY_MATRIX_V     unity_MatrixV
#define UNITY_MATRIX_I_V   unity_MatrixInvV
//#define UNITY_MATRIX_P     OptimizeProjectionMatrix(glstate_matrix_projection)
#define UNITY_MATRIX_P     unity_MatrixP
#define UNITY_MATRIX_I_P   (float4x4)0
#define UNITY_MATRIX_VP    unity_MatrixVP
#define UNITY_MATRIX_I_VP  (float4x4)0
#define UNITY_MATRIX_MV    mul(UNITY_MATRIX_V, UNITY_MATRIX_M)
#define UNITY_MATRIX_T_MV  transpose(UNITY_MATRIX_MV)
#define UNITY_MATRIX_IT_MV transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V))
#define UNITY_MATRIX_MVP   mul(UNITY_MATRIX_VP, UNITY_MATRIX_M)
#define UNITY_PREV_MATRIX_M   unity_MatrixPreviousM
#define UNITY_PREV_MATRIX_I_M unity_MatrixPreviousMI

struct Varyings
{
    float4 positionCS : SV_POSITION;

    #ifdef VARYINGS_NEED_TEXCOORD0
    float4 texCoord0 : TEXCOORD0;
    #endif
    
    #ifdef VARYINGS_NEED_TEXCOORD1
    float4 texCoord1 : TEXCOORD1;
    #endif

    #ifdef VARYINGS_NEED_TEXCOORD2
    float4 texCoord2 : TEXCOORD2;
    #endif

    #ifdef VARYINGS_NEED_COLOR
    real4 color : COLOR0; 
    #endif

    #ifdef VARYINGS_NEED_NORMAL_WS
    float3 normalWS : COLOR1;
    #endif

    #ifdef VARYINGS_NEED_POSITION_WS
    float3 positionWS : COLOR2;
    #endif
};

#ifndef USE_VERY_FAST_SRGB
#if defined(SHADER_API_MOBILE)
#define USE_VERY_FAST_SRGB 1
#else
#define USE_VERY_FAST_SRGB 0
#endif
#endif

#ifndef USE_FAST_SRGB
#if defined(SHADER_API_CONSOLE)
#define USE_FAST_SRGB 1
#else
#define USE_FAST_SRGB 0
#endif
#endif

half3 LinearToSRGB(half3 c)
{
    #if USE_VERY_FAST_SRGB
    return sqrt(c);
    #elif USE_FAST_SRGB
    return max(1.055 * PositivePow(c, 0.416666667) - 0.055, 0.0);
    #else
    half3 sRGBLo = c * 12.92;
    half3 sRGBHi = (PositivePow(c, half3(1.0 / 2.4, 1.0 / 2.4, 1.0 / 2.4)) * 1.055) - 0.055;
    half3 sRGB = half3((c.x <= 0.0031308) ? sRGBLo.x : sRGBHi.x, (c.y <= 0.0031308) ? sRGBLo.y : sRGBHi.y, (c.z <= 0.0031308) ? sRGBLo.z : sRGBHi.z);
    return sRGB;
    #endif
}

half4 LinearToSRGB(half4 c)
{
    return half4(LinearToSRGB(c.rgb), c.a);
}

#endif