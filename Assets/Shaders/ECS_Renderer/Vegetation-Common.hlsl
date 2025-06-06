#include "Common.hlsl"
#include "Packages/com.tzargames.rendering/Shaders/Lighting.hlsl"
#include "Packages/com.dgx.srp/ShaderLibrary/Lighting.hlsl"

struct appdata
{
    half3 vertex : POSITION;

    #if !USE_UP_NORMAL
    half3 normal : NORMAL;
    #endif
    half4 tangent : TANGENT;
    
    half4 color : COLOR;
    half2 uv : TEXCOORD0;
#if LIGHTMAP_ON
    TG_DECLARE_LIGHTMAP_UV(1)
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    half4 vertex : SV_POSITION;
    half2 uv : TEXCOORD0;
    nointerpolation half4 instanceData : TEXCOORD1;

    half4 normalWS_occl : TEXCOORD2;
    half4 tangentWS : TEXCOORD3;
    half3 bitangentWS : TEXCOORD4;

#if LIGHTMAP_ON
    TG_DECLARE_LIGHTMAP_UV(6)
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

CBUFFER_START(UnityPerMaterial)
    half4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _Underwater_color;
    half _BaseColorMult;
    half _Metallic;
    half _Smoothness;
    half _Cutoff;
    half4 _EmissionColor;
    half _WindForce;
    half _AOAdd;
CBUFFER_END

sampler2D _BaseMap;
sampler2D _BumpMap;
sampler2D _MetallicGlossMap;

void billboard(inout half3 vertex, inout half3 normal, inout half3 tangent)
{
    half3 normalDir = mul(UNITY_MATRIX_I_M, half4(_WorldSpaceCameraPos, 1));
    normalDir.y = 0;
    normalDir = normalize(normalDir);
    
    //break out the axis
    half3 up = half3(0,1,0);
    half3 right = normalize(cross(normalDir, up));
    half3 forward = normalDir;
    
    //get the rotation parts of the matrix
    half4x4 rotationMatrix = half4x4(right, 0,
        up, 0,
        forward, 0,
        0, 0, 0, 1);
    
    vertex = mul(rotationMatrix, vertex);

    #if USE_UP_NORMAL
    normal = half3(0,1,0);
    tangent = half3(1,0,0);
    #else
    normal = normalDir;
    tangent = right;
    #endif
}

v2f vert (appdata v)
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);

    half3 positionOS = v.vertex;
    
    float4 time = _Time;
    float4 instanceData = tg_InstanceData;
    o.instanceData = instanceData;
    #if USE_MULT_WINDFORCE_BY_UV
    half windForceMult = v.uv.y;
    #else
    half windForceMult = v.color.x;
    #endif

    float3 baseWorldPos = UNITY_MATRIX_M._m03_m13_m23;
    half wind = (sin(time.z + (baseWorldPos.x + baseWorldPos.z + positionOS.x + positionOS.z) * 2) + sin(time.y) + sin(time.w)) * 0.05 * windForceMult;
    wind *= _WindForce * (1.0 - instanceData.w);
    positionOS.x += wind;
    positionOS.z += wind;
 
    #if USE_UP_NORMAL
    half3 normalOS = half3(0,1,0);
    #else
    half3 normalOS = v.normal;
    #endif
    half4 tangentOS = v.tangent;

    #if USE_BILLBOARD
    billboard(positionOS, normalOS, tangentOS.xyz);
    #endif

    float3 positionWS = TransformObjectToWorld(positionOS);
    o.vertex = TransformWorldToHClip(positionWS);

    o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
    //o.positionWS_fog.a = ComputeFogFactor(o.vertex.z);

    

    real sign = real(tangentOS.w);
    float3 normalWS = TransformObjectToWorldNormal(normalOS);
    real3 tangentWS = real3(TransformObjectToWorldDir(tangentOS.xyz));
    real3 bitangentWS = real3(cross(normalWS, float3(tangentWS))) * sign;

    o.normalWS_occl.xyz = normalWS;
    o.normalWS_occl.w = saturate(v.color.x + _AOAdd);
    o.tangentWS = float4(tangentWS, tangentOS.w);
    o.bitangentWS = bitangentWS;

#if LIGHTMAP_ON
    TG_TRANSFORM_LIGHTMAP_TEX(v.lightmapUV, o.lightmapUV)
#endif

    return o;
}

GBufferFragmentOutput frag(v2f i) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i);
    
    #if DEBUG_VERTEX_COLOR
    SurfaceHalf vcSurface;
    vcSurface.Albedo = i.normalWS_occl.w;
    vcSurface.Alpha = 1;
    vcSurface.NormalWS = i.normalWS_occl.xyz;
    vcSurface.AmbientLight = 1;
    vcSurface.Metallic = 0;
    vcSurface.Roughness = 1;
    vcSurface.EnvCubemapIndex = 0;
    return SurfaceToGBufferOutputHalf(vcSurface);
    #endif
    
    half4 diffuse = tex2D(_BaseMap, i.uv);
    #ifndef ARENA_MAP_RENDER
    diffuse.rgb *= _BaseColor.rgb * _BaseColorMult;
    #endif

#if defined(TG_USE_ALPHACLIP)
    clip(diffuse.a - _Cutoff);
#endif
    
    #if defined(UG_QUALITY_MED) || defined(UG_QUALITY_HIGH)
    half3 normalTS = UnpackNormal(tex2D(_BumpMap, i.uv));
    half3x3 tangentToWorld = half3x3(i.tangentWS.xyz, i.bitangentWS.xyz, i.normalWS_occl.xyz); 
    half3 normalWS = TransformTangentToWorld(normalTS.xyz, tangentToWorld, true);
    #else
    half3 normalWS = i.normalWS_occl.xyz;
    #endif

    half ao =  i.normalWS_occl.w;

    half3 lighting = ARENA_COMPUTE_AMBIENT_LIGHT(i, normalWS);
    
#if !defined(LIGHTMAP_ON) && !defined(ARENA_MAP_RENDER)
    lighting *= ao;          
#endif

    SurfaceHalf surface;
    surface.Albedo = diffuse.rgb;
    surface.Alpha = diffuse.a;
    surface.NormalWS = normalWS;
    surface.AmbientLight = lighting;
    

    #if !defined(ARENA_MAP_RENDER) && (defined(UG_QUALITY_MED) || defined(UG_QUALITY_HIGH))
    half4 mesm = tex2D(_MetallicGlossMap, i.uv);
    mesm.rgb *= _Metallic;

    half lum = tg_luminance(lighting);
    //return lum;

    half smoothness = mesm.a * _Smoothness * lum;
    half roughness = 1 - smoothness;

    //half3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.positionWS_fog.xyz);
    //half3 envMapColor = TG_ReflectionProbe(viewDirWS, normalWS, i.instanceData.y, roughness * 4);
    //envMapColor *= ao;
    
    surface.Metallic = mesm.rgb;
    surface.Roughness = roughness;
    surface.EnvCubemapIndex = i.instanceData.y;
    
    #else
    surface.Metallic = 0;
    surface.Roughness = 1;
    surface.EnvCubemapIndex = 0;
    #endif
    
    return SurfaceToGBufferOutputHalf(surface);
}

#ifdef ARENA_META_PASS 
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/MetaPass.hlsl"
             
half4 metaFragment(v2f fragIn) : SV_Target
{
    UnityMetaInput metaInput;

    half4 diffuse = tex2D(_BaseMap, fragIn.uv);
    diffuse.rgb *= _BaseColor.rgb * _BaseColorMult;
                
    metaInput.Albedo = diffuse.rgb;
    metaInput.Emission = _EmissionColor.rgb;
                
    #ifdef EDITOR_VISUALIZATION
    metaInput.VizUV = fragIn.VizUV;
    metaInput.LightCoord = fragIn.LightCoord;
    #endif

    half4 result = UnityMetaFragment(metaInput);
    result.a = diffuse.a;
    return result;
}
#endif