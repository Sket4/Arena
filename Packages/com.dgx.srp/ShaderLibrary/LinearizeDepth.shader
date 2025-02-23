Shader "Hidden/DGX/LINEARIZE_DEPTH"
{
    Properties
    {
    }
    SubShader
    {
        ZTest Always
        ZWrite Off
        Cull Off
        
        Pass
        {
            Name "linearize"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #define SLICE_ARRAY_INDEX   unity_StereoEyeIndex

            #define TEXTURE2D_X(textureName)                                        TEXTURE2D_ARRAY(textureName)
            #define TEXTURE2D_X_PARAM(textureName, samplerName)                     TEXTURE2D_ARRAY_PARAM(textureName, samplerName)
            #define TEXTURE2D_X_ARGS(textureName, samplerName)                      TEXTURE2D_ARRAY_ARGS(textureName, samplerName)
            #define TEXTURE2D_X_HALF(textureName)                                   TEXTURE2D_ARRAY_HALF(textureName)
            #define TEXTURE2D_X_FLOAT(textureName)                                  TEXTURE2D_ARRAY_FLOAT(textureName)

            #define LOAD_TEXTURE2D_X(textureName, unCoord2)                         LOAD_TEXTURE2D_ARRAY(textureName, unCoord2, SLICE_ARRAY_INDEX)
            #define LOAD_TEXTURE2D_X_LOD(textureName, unCoord2, lod)                LOAD_TEXTURE2D_ARRAY_LOD(textureName, unCoord2, SLICE_ARRAY_INDEX, lod)
            #define SAMPLE_TEXTURE2D_X(textureName, samplerName, coord2)            SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, coord2, SLICE_ARRAY_INDEX)
            #define SAMPLE_TEXTURE2D_X_LOD(textureName, samplerName, coord2, lod)   SAMPLE_TEXTURE2D_ARRAY_LOD(textureName, samplerName, coord2, SLICE_ARRAY_INDEX, lod)
            #define GATHER_TEXTURE2D_X(textureName, samplerName, coord2)            GATHER_TEXTURE2D_ARRAY(textureName, samplerName, coord2, SLICE_ARRAY_INDEX)
            #define GATHER_RED_TEXTURE2D_X(textureName, samplerName, coord2)        GATHER_RED_TEXTURE2D(textureName, samplerName, float3(coord2, SLICE_ARRAY_INDEX))
            #define GATHER_GREEN_TEXTURE2D_X(textureName, samplerName, coord2)      GATHER_GREEN_TEXTURE2D(textureName, samplerName, float3(coord2, SLICE_ARRAY_INDEX))
            #define GATHER_BLUE_TEXTURE2D_X(textureName, samplerName, coord2)       GATHER_BLUE_TEXTURE2D(textureName, samplerName, float3(coord2, SLICE_ARRAY_INDEX))

            int unity_StereoEyeIndex;

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            
            #define UNITY_DECLARE_TEX2D_FLOAT(tex) TEXTURE2D_FLOAT(tex); SAMPLER(sampler##tex)
            UNITY_DECLARE_TEX2D_FLOAT(_Depth);
            float4 _ZBufferParams;

            half4 frag (Varyings input) : SV_Target
            {
                //UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float rawDepth = _Depth.Sample(sampler_Depth, input.texcoord).r;
                return Linear01Depth(rawDepth, _ZBufferParams).r;
            }
            
            ENDHLSL
        }
    }
}