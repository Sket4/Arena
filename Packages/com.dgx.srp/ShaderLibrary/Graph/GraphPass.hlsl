#ifndef DGX_GRAPHPASS_INCLUDED
#define DGX_GRAPHPASS_INCLUDED

#include "GraphInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/MetaPass.hlsl"

PackedVaryings vert (Attributes v)
{
    Varyings o;

    #ifdef UNITY_ANY_INSTANCING_ENABLED
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    #endif
 
    float3 positionWS = TransformObjectToWorld(v.positionOS);
    o.positionCS = TransformWorldToHClip(positionWS);
    #ifdef VARYINGS_NEED_POSITION_WS
    o.positionWS = positionWS;
    #endif

    #ifdef VARYINGS_NEED_NORMAL_WS
    float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
    o.normalWS = normalWS;
    #endif
    
    #ifdef VARYINGS_NEED_TEXCOORD0
    o.texCoord0 = v.uv0;
    #endif

    #ifdef VARYINGS_NEED_TEXCOORD1
    o.texCoord1 = v.uv1;
    #endif

    #ifdef VARYINGS_NEED_TEXCOORD2
    o.texCoord2 = v.uv2;
    #endif

    #ifdef ATTRIBUTES_NEED_COLOR
    o.color = v.color;
    #endif

    return PackVaryings(o); 
}

half4 frag(PackedVaryings packed) : SV_Target
{
    Varyings varyings = UnpackVaryings(packed);
    SurfaceDescriptionInputs surfDescInputs = BuildSurfaceDescriptionInputs(varyings);
    SurfaceDescription surface = SurfaceDescriptionFunction(surfDescInputs);
    return half4(surface.BaseColor + surface.Emission, surface.Alpha);
}

half4 FragmentMetaCustom(PackedVaryings packed) : SV_Target
{
    Varyings varyings = UnpackVaryings(packed);
    SurfaceDescriptionInputs surfDescInputs = BuildSurfaceDescriptionInputs(varyings);
    SurfaceDescription surface = SurfaceDescriptionFunction(surfDescInputs);
    
    UnityMetaInput metaInput;
    
    metaInput.Albedo = surface.BaseColor;
    metaInput.Emission = surface.Emission;
                
    #ifdef EDITOR_VISUALIZATION
    metaInput.VizUV = fragIn.VizUV;
    metaInput.LightCoord = fragIn.LightCoord;
    #endif

    half4 result = UnityMetaFragment(metaInput);
    return result;
}

#endif