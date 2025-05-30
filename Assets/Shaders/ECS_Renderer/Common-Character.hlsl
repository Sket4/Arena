#ifndef UG_COMMON_CHARACTER_INCLUDED
#define UG_COMMON_CHARACTER_INCLUDED

#include "Packages/com.dgx.srp/ShaderLibrary/Lighting.hlsl"
#include "Common.hlsl"

struct appdata
{
    float3 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float2 uv : TEXCOORD0;

    #ifdef TG_SKINNING
    uint4 BoneIndices : BLENDINDICES;
 
    #if defined _BONECOUNT_FOUR
    half4 BoneWeights : BLENDWEIGHTS;
    #endif
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    half2 uv : TEXCOORD0;
    half4 vertex : SV_POSITION;
    nointerpolation half4 instanceData : TEXCOORD1;

    float3 normalWS : TEXCOORD2;
    
    #if defined(UG_QUALITY_MED) || defined(UG_QUALITY_HIGH)
    float4 tangentWS : TEXCOORD3;
    #endif
    //UNITY_VERTEX_INPUT_INSTANCE_ID

    #ifdef ARENA_SKIN_COLOR
    nointerpolation half3 skinColor : TEXCOORD4;
    #endif
};

#include "Packages/com.tzargames.rendering/Shaders/Lighting.hlsl"
#include "Packages/com.tzargames.rendering/Shaders/Skinning.hlsl"

#if defined(DOTS_INSTANCING_ON)
     UNITY_DOTS_INSTANCING_START(UserPropertyMetadata)
            UNITY_DOTS_INSTANCED_PROP_OVERRIDE_REQUIRED(float4, _SkinColor) 
     UNITY_DOTS_INSTANCING_END(UserPropertyMetadata)
#endif

#if defined(DOTS_INSTANCING_ON)
#define SKIN_COLOR UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, _SkinColor)
#else
#define SKIN_COLOR _SkinColor
#endif

sampler2D _BaseMap;
#ifdef TG_FADING
sampler2D _FadeMap;
#endif
sampler2D _BumpMap;
sampler2D _MetallicSmoothnessMap;

v2f vert (appdata v)
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    //UNITY_TRANSFER_INSTANCE_ID(v, o);
    
    float3 positionOS = v.vertex;
    float3 normalOS = v.normal;
    float4 tangentOS = v.tangent;

    // skinning
    #ifdef TG_SKINNING 
    #if defined _BONECOUNT_FOUR
    ComputeSkinning(v.BoneIndices, v.BoneWeights, 4, positionOS, normalOS, tangentOS.xyz);
    #else
    ComputeSkinning_OneBone(v.BoneIndices.x, positionOS, normalOS, tangentOS.xyz);
    #endif
    #endif

    float3 positionWS = TransformObjectToWorld(positionOS);
    o.vertex = TransformWorldToHClip(positionWS);

    o.uv = TRANSFORM_TEX(v.uv, _BaseMap);


    
    float3 normalWS = TransformObjectToWorldNormal(normalOS);
    real3 tangentWS = real3(TransformObjectToWorldDir(tangentOS.xyz));

    o.normalWS = normalWS;
    #if defined(UG_QUALITY_MED) || defined(UG_QUALITY_HIGH)
    o.tangentWS = float4(tangentWS, tangentOS.w);
    #endif

    float4 instanceData = tg_InstanceData;
    o.instanceData = instanceData;

    #ifdef ARENA_SKIN_COLOR
    o.skinColor = SKIN_COLOR.rgb;
    #endif
    
    return o;
}

GBufferFragmentOutput frag (v2f i)
{
    //UNITY_SETUP_INSTANCE_ID(i);
    
    half4 diffuse = tex2D(_BaseMap, i.uv);

    #if defined(ARENA_SKIN_COLOR)
    diffuse.rgb = lerp(diffuse.rgb, diffuse.rgb * i.skinColor.rgb, diffuse.a);
    #endif

    #if defined(TG_FADING)
    half4 fadeColor = tex2D(_FadeMap, i.uv);
    clip(fadeColor.r * diffuse.a - i.instanceData.w);
    #endif

    #if defined(TG_USE_ALPHACLIP)
    clip(diffuse.a - _Cutoff);
    #endif

    #if defined(UG_QUALITY_MED) || defined(UG_QUALITY_HIGH)
    half3 normalTS = UnpackNormal(tex2D(_BumpMap, i.uv));

    real sign = real(i.tangentWS.w);
    real3 bitangentWS = real3(cross(i.normalWS, i.tangentWS.xyz) * sign);
    half3x3 tangentToWorld = half3x3(i.tangentWS.xyz, bitangentWS, i.normalWS.xyz);

    float3 normalWS = TransformTangentToWorld(normalTS.xyz, tangentToWorld, true);

    half4 mesm = tex2D(_MetallicSmoothnessMap, i.uv);
    half3 metallic = mesm.rgb * _Metallic;
    mesm.a *= _Smoothness;

    half roughness = 1.0 - mesm.a;
    #else
    float3 normalWS = i.normalWS;
    half metallic = 0;
    half roughness = 1;
    #endif
    
    half3 lighting = ARENA_COMPUTE_AMBIENT_LIGHT(i, normalWS);

    SurfaceHalf surface;
    surface.Albedo = diffuse.rgb;
    surface.Alpha = diffuse.a;
    surface.Metallic = metallic;
    surface.Roughness = roughness;
    surface.AmbientLight = lighting;
    surface.EnvCubemapIndex = i.instanceData.y;
    surface.NormalWS = normalWS;

    return SurfaceToGBufferOutputHalf(surface);
}

struct appdata_depthonly
{
    float3 vertex : POSITION;
    //float3 normal : NORMAL;
    //float4 tangent : TANGENT;
    //float2 uv : TEXCOORD0;
    
    uint4 BoneIndices : BLENDINDICES;

    #if defined _BONECOUNT_FOUR
    half4 BoneWeights : BLENDWEIGHTS;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f_depthonly
{
    half4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

v2f_depthonly DepthOnlyVertex(appdata_depthonly v)
{
    v2f_depthonly output = (v2f_depthonly)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, output);
    //UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionOS = v.vertex;

    // skinning
    #if defined _BONECOUNT_FOUR
    ComputeSkinning_Position(v.BoneIndices, v.BoneWeights, 4, positionOS);
    #else
    ComputeSkinning_OneBone_Position(v.BoneIndices.x, positionOS);
    #endif

    #if defined(_ALPHATEST_ON)
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
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

#endif //UG_COMMON_CHARACTER_INCLUDED

