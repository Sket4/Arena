#ifndef DGX_GRAPHPASS_INCLUDED
#define DGX_GRAPHPASS_INCLUDED

#include "GraphInput.hlsl"

Varyings vert (Attributes v)
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
    
    return o; 
}

half4 frag(Varyings varyings) : SV_Target
{
    SurfaceDescriptionInputs surfDescInputs = BuildSurfaceDescriptionInputs(varyings);
    SurfaceDescription surface = SurfaceDescriptionFunction(surfDescInputs);
    return half4(surface.BaseColor, surface.Alpha);
}

#endif