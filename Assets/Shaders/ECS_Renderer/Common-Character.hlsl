#ifndef UG_COMMON_CHARACTER_INCLUDED
#define UG_COMMON_CHARACTER_INCLUDED

#ifndef TG_USE_URP
#define TG_USE_URP
#endif

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl" 
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Common.hlsl"

struct appdata
{
    float3 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float2 uv : TEXCOORD0;
    
    uint4 BoneIndices : BLENDINDICES;

    #if defined _BONECOUNT_FOUR
    half4 BoneWeights : BLENDWEIGHTS;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    half2 uv : TEXCOORD0;
    half fogCoords : TEXCOORD1;
    half4 vertex : SV_POSITION;
    nointerpolation half4 instanceData : TEXCOORD2;
    
    float3 normalWS : TEXCOORD3;
    float4 tangentWS : TEXCOORD4;
    float3 bitangentWS : TEXCOORD5;
    float3 positionWS : TEXCOORD6;
    UNITY_VERTEX_INPUT_INSTANCE_ID
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
#if defined TG_FADING
sampler2D _FadeMap;
#endif
sampler2D _BumpMap;
sampler2D _MetallicSmoothnessMap;

v2f vert (appdata v)
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    
    float3 positionOS = v.vertex;
    float3 normalOS = v.normal;
    float4 tangentOS = v.tangent;

    // skinning
    #if defined _BONECOUNT_FOUR
    ComputeSkinning(v.BoneIndices, v.BoneWeights, 4, positionOS, normalOS, tangentOS.xyz);
    #else
    ComputeSkinning_OneBone(v.BoneIndices.x, positionOS, normalOS, tangentOS.xyz);
    #endif
     
    VertexPositionInputs vertInputs = GetVertexPositionInputs(positionOS);    //This function calculates all the relative spaces of the objects vertices
    o.vertex = vertInputs.positionCS;
    o.positionWS = vertInputs.positionWS;

    o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
    o.fogCoords = ComputeFogFactor(o.vertex.z);


    VertexNormalInputs normalInputs = GetVertexNormalInputs(normalOS, tangentOS);

    o.normalWS = normalInputs.normalWS;
    o.tangentWS = float4(normalInputs.tangentWS, tangentOS.w);
    o.bitangentWS = normalInputs.bitangentWS;

    float4 instanceData = tg_InstanceData;
    o.instanceData = instanceData;
    
    
    #if USE_DISTANCE_LIGHT
    float3 dirToPos = o.positionWS - _WorldSpaceCameraPos;
    half sqDistance = dot(dirToPos, dirToPos);
    half maxDistanceSq = 2500.0;
    half distanceMult = saturate(sqDistance * (1.0f / maxDistanceSq));
    o.color += distanceMult;
    #endif
    
    return o;
}

half4 frag (v2f i) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i);
    
    half4 diffuse = tex2D(_BaseMap, i.uv);

    #if defined(ARENA_SKIN_COLOR)
    diffuse.rgb = lerp(diffuse.rgb, diffuse.rgb * SKIN_COLOR.rgb, diffuse.a); 
    #endif

    #if defined(TG_FADING)
    half4 fadeColor = tex2D(_FadeMap, i.uv);
    clip(fadeColor.r * diffuse.a - i.instanceData.w);
    #endif

    #if defined(TG_USE_ALPHACLIP)
    clip(diffuse.a - _Cutoff);
    #endif

    half3 normalTS = UnpackNormal(tex2D(_BumpMap, i.uv));
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.positionWS);

    half3x3 tangentToWorld = half3x3(i.tangentWS.xyz, i.bitangentWS.xyz, i.normalWS.xyz);

    float3 normalWS = TransformTangentToWorld(normalTS.xyz, tangentToWorld, true);

    //mesm.rgb *= _Metallic;
    half4 mesm = tex2D(_MetallicSmoothnessMap, i.uv);
    half3 metallic = mesm.rgb * _Metallic;
    mesm.a *= _Smoothness;

    half roughness = 1.0 - mesm.a;
    
    half3 lighting = ARENA_COMPUTE_AMBIENT_LIGHT(i, normalWS);

    // shadowing
    //float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS.xyz);
	//half shadow = MainLightRealtimeShadow(shadowCoord);
    //lighting = MixLightWithRealtimeShadow(shadow, lighting);

    // experemental specular
    //half3 reflectDir = reflect(-normalWS, normalWS);
    //float spec = pow(max(dot(viewDirWS, reflectDir), 0.0), 32);
    //float3 specular = diffuse.a * spec * lighting;
    ////return half4(specular,1);
    //lighting += specular;

    half3 envMapColor = TG_ReflectionProbe(viewDirWS, normalWS, i.instanceData.y, roughness * 4);

    ARENA_DYN_LIGHT(normalWS, i.positionWS.xyz, lighting, viewDirWS, envMapColor, false);
    
    //envMapColor *= metallic.a;
    half4 finalColor = LightingPBR(diffuse, lighting, viewDirWS, normalWS, metallic, roughness, envMapColor);

    #if USE_RIM
    half ndotv = dot(viewDirWS, normalWS);

    half3 mixedRim = lerp(finalColor.rgb, _RimColor.rgb, _RimStr);
    finalColor.rgb = lerp(finalColor.rgb, mixedRim, saturate(1.0 - abs(ndotv) - _RimColor.a));
    #endif

    // apply fog
    return half4(MixFog(finalColor.rgb, i.fogCoords), finalColor.a);
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

