#ifndef TG_SHADOW_CASTER_PASS_INCLUDED
#define TG_SHADOW_CASTER_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.tzargames.rendering/Shaders/Input.hlsl"

#if defined(TG_USE_URP)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#endif

#if defined(TG_SKINNING)
#include "Packages/com.tzargames.rendering/Shaders/Skinning.hlsl"
#endif

// Shadow Casting Light geometric parameters. These variables are used when applying the shadow Normal Bias and are set by UnityEngine.Rendering.Universal.ShadowUtils.SetupShadowCasterConstantBuffer in com.unity.render-pipelines.universal/Runtime/ShadowUtils.cs
// For Directional lights, _LightDirection is used when applying shadow Normal Bias.
// For Spot lights and Point lights, _LightPosition is used to compute the actual light direction because it is different at each shadow caster geometry vertex.
float3 _LightDirection;
float3 _LightPosition;

#if defined(TG_USE_ALPHACLIP)
sampler2D _BaseMap;
#endif

#if defined(TG_FADING)
sampler2D _FadeMap;
#endif

struct Attributes
{
    float3 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 texcoord     : TEXCOORD0;

    #if defined(TG_SKINNING)
    uint4 BoneIndices : BLENDINDICES;
    #endif
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    #if defined(TG_USE_ALPHACLIP)
    float2 uv       : TEXCOORD0;
    #endif

    #if defined(TG_FADING)
    half fade : TEXCOORD1;
    #endif
    
    //UNITY_VERTEX_INPUT_INSTANCE_ID
};

float4 GetShadowPositionHClip(Attributes input)
{
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

#if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif

    #if defined(TG_USE_URP)
    float3 biased = ApplyShadowBias(positionWS, normalWS, lightDirectionWS);
    #else
    float3 biased = positionWS;
    #endif
    float4 positionCS = TransformWorldToHClip(biased);

#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif

    return positionCS;
}

Varyings ShadowPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    //UNITY_TRANSFER_INSTANCE_ID(input, output);

    #if defined(TG_FADING)
    float4 instanceData = tg_InstanceData;
    output.fade = instanceData.w;
    #endif
    
    #if defined(TG_USE_ALPHACLIP)
        output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    #endif

    #if defined(TG_SKINNING)
    ComputeSkinning_OneBone_NoTangent(input.BoneIndices.x, input.positionOS.xyz, input.normalOS);
    #endif
 
    output.positionCS = GetShadowPositionHClip(input);
    return output;
}

half4 ShadowPassFragment(Varyings input) : SV_TARGET
{
    //UNITY_SETUP_INSTANCE_ID(input);
    
    #if defined(TG_FADING)
    half4 fadeColor = tex2D(_FadeMap, input.uv);
    clip(fadeColor.r - input.fade);
    #endif
    
    #if defined(TG_USE_ALPHACLIP)
    half alpha = tex2D(_BaseMap, input.uv).a;
    clip(alpha - _Cutoff);
    #endif
    
    #if defined(LOD_FADE_CROSSFADE)
        LODFadeCrossFade(input.positionCS);
    #endif

    return 0;
}

#endif
