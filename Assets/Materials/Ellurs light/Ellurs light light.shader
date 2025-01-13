Shader "CustomRenderTexture/Ellurs light light"
{
    Properties
    {
        _MainTex("InputTex", 2D) = "white" {}
        _BackgroundTex("Background", 2D) = "white" {}
     }

     SubShader
     {
        //Blend One Zero

        Pass
        {
            Name "Ellurs light light"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            sampler2D   _MainTex;
            sampler2D   _BackgroundTex;

            struct appdata_customrendertexture
            {
                uint    vertexID    : SV_VertexID;
            };

            struct v2f_customrendertexture
            {
                float4 vertex           : SV_POSITION;
                float2 uv0    : TEXCOORD0;
                float2 uv1    : TEXCOORD1;
                float2 uv2    : TEXCOORD2;
                //float3 localTexcoord    : TEXCOORD0;    // Texcoord local to the update zone (== globalTexcoord if no partial update zone is specified)
                //float3 globalTexcoord   : TEXCOORD1;    // Texcoord relative to the complete custom texture
                //uint primitiveID        : TEXCOORD2;    // Index of the update zone (correspond to the index in the updateZones of the Custom Texture)
                //float3 direction        : TEXCOORD3;    // For cube textures, direction of the pixel being rendered in the cubemap
            };

            #define UNITY_PI            3.14159265359f
            #define kCustomTextureBatchSize 16
            float4      CustomRenderTextureCenters[kCustomTextureBatchSize];
            float4      CustomRenderTextureSizesAndRotations[kCustomTextureBatchSize];
            float       CustomRenderTexturePrimitiveIDs[kCustomTextureBatchSize];

            float4      CustomRenderTextureParameters;
            #define     CustomRenderTextureUpdateSpace  CustomRenderTextureParameters.x // Normalized(0)/PixelSpace(1)
            #define     CustomRenderTexture3DTexcoordW  CustomRenderTextureParameters.y
            #define     CustomRenderTextureIs3D         CustomRenderTextureParameters.z

            float4 _Time;
            float4 _SinTime;

            // User facing uniform variables
            float4      _CustomRenderTextureInfo; // x = width, y = height, z = depth, w = face/3DSlice

            float2 CustomRenderTextureRotate2D(float2 pos, float angle)
            {
                float sn = sin(angle);
                float cs = cos(angle);

                return float2( pos.x * cs - pos.y * sn, pos.x * sn + pos.y * cs);
            }

            v2f_customrendertexture vert(appdata_customrendertexture IN)
            {
                v2f_customrendertexture OUT;

            #if UNITY_UV_STARTS_AT_TOP
                const float2 vertexPositions[6] =
                {
                    { -1.0f,  1.0f },
                    { -1.0f, -1.0f },
                    {  1.0f, -1.0f },
                    {  1.0f,  1.0f },
                    { -1.0f,  1.0f },
                    {  1.0f, -1.0f }
                };

                const float2 texCoords[6] =
                {
                    { 0.0f, 0.0f },
                    { 0.0f, 1.0f },
                    { 1.0f, 1.0f },
                    { 1.0f, 0.0f },
                    { 0.0f, 0.0f },
                    { 1.0f, 1.0f }
                };
            #else
                const float2 vertexPositions[6] =
                {
                    {  1.0f,  1.0f },
                    { -1.0f, -1.0f },
                    { -1.0f,  1.0f },
                    { -1.0f, -1.0f },
                    {  1.0f,  1.0f },
                    {  1.0f, -1.0f }
                };

                const float2 texCoords[6] =
                {
                    { 1.0f, 1.0f },
                    { 0.0f, 0.0f },
                    { 0.0f, 1.0f },
                    { 0.0f, 0.0f },
                    { 1.0f, 1.0f },
                    { 1.0f, 0.0f }
                };
            #endif

                uint primitiveID = (IN.vertexID / 6) % kCustomTextureBatchSize;
                uint vertexID = IN.vertexID % 6;
                float3 updateZoneCenter = CustomRenderTextureCenters[primitiveID].xyz;
                float3 updateZoneSize = CustomRenderTextureSizesAndRotations[primitiveID].xyz;
                float rotation = CustomRenderTextureSizesAndRotations[primitiveID].w * UNITY_PI / 180.0f;

            #if !UNITY_UV_STARTS_AT_TOP
                rotation = -rotation;
            #endif

                // Normalize rect if needed
                if (CustomRenderTextureUpdateSpace > 0.0) // Pixel space
                {
                    // Normalize xy because we need it in clip space.
                    updateZoneCenter.xy /= _CustomRenderTextureInfo.xy;
                    updateZoneSize.xy /= _CustomRenderTextureInfo.xy;
                }
                else // normalized space
                {
                    // Un-normalize depth because we need actual slice index for culling
                    updateZoneCenter.z *= _CustomRenderTextureInfo.z;
                    updateZoneSize.z *= _CustomRenderTextureInfo.z;
                }

                // Compute rotation

                // Compute quad vertex position
                float2 clipSpaceCenter = updateZoneCenter.xy * 2.0 - 1.0;
                float2 pos = vertexPositions[vertexID] * updateZoneSize.xy;
                pos = CustomRenderTextureRotate2D(pos, rotation);
                pos.x += clipSpaceCenter.x;
            #if UNITY_UV_STARTS_AT_TOP
                pos.y += clipSpaceCenter.y;
            #else
                pos.y -= clipSpaceCenter.y;
            #endif

                OUT.vertex = float4(pos, UNITY_NEAR_CLIP_VALUE, 1.0);
                //OUT.primitiveID = asuint(CustomRenderTexturePrimitiveIDs[primitiveID]);
                float2 uv = texCoords[vertexID];
                
                OUT.uv0 = uv + float2(_Time.x, -_Time.x);
                OUT.uv1 = uv + float2(-_Time.x, -_Time.x);
                OUT.uv2 = uv + float2(_SinTime.x, -_SinTime.x);

                return OUT;
            }

            half4 frag(v2f_customrendertexture IN) : SV_Target
            {
                half color = tex2D(_BackgroundTex, IN.uv0).r;
                color *= (half)tex2D(_BackgroundTex, IN.uv1).r;
                color *= (half)tex2D(_BackgroundTex, IN.uv2).r;

                return half4(color,color,color,1);
            }
            ENDHLSL
        }
    }
}
