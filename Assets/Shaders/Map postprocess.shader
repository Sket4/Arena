Shader "Arena/Map postprocess"
{
    Properties
    {
        _MainTex ("Main texture", 2D) = "white" {}
        _ColorTint ("Color tint", Color) = (1,1,1,1)
        _FinalColorMult ("Final color mult", Float) = 1
        _Saturation("Saturation", FLoat) = 1.0
        _EdgeDetectionSampleSize("Edge detection sample size", Float) = 0.001
        _EdgeStrength("Edge strength", Float) = 1.0
        _EdgeDetectionHeight("Edge detection height", Float) = 5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM

            #pragma target 4.5
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma exclude_renderers gles

            //#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            //#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
            #include "ECS_Renderer/Common.hlsl"
            
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 screenUV : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
            half _Saturation;
            half4 _ColorTint;
            half _EdgeDetectionSampleSize;
            half _EdgeDetectionHeight;
            half _EdgeStrength;
            half _FinalColorMult;
            CBUFFER_END

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4x4 unity_MatrixInvVP;
            
            #define SHADOW_SAMPLER sampler_linear_clamp_compare
            SAMPLER_CMP(SHADOW_SAMPLER);

            #include <HLSLSupport.cginc>
            UNITY_DECLARE_TEX2D_FLOAT(_Depth);

            const static int numOfSamples = 8;

            const static float sampleMargins[numOfSamples*2] = 
            {
                1,1,
                1,-1,
                -1,1,
                -1,-1,
                1,0,
                -1,0,
                0,1,
                0,-1
            };
            
            v2f vert (appdata v)
            {
                v2f o;

                float3 positionWS = TransformObjectToWorld(v.vertex);
                o.vertex = TransformWorldToHClip(positionWS);
                o.uv = v.uv;
                o.screenUV.xy = v.vertex.xy;

                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0)
                    o.screenUV.y = 1-o.screenUV.y;
                #endif
                
                return o;
            }

            float3 WorldSpacePositionFromDepth(float2 screenUV, float rawDepth)
            {
                #if UNITY_REVERSED_Z
                float deviceDepth = rawDepth;
                #else
                float deviceDepth = rawDepth * 2.0 - 1.0;
                #endif
                
                float4 positionCS = float4(screenUV.xy * 2 - 1, deviceDepth, 1);
                float4 hpositionWS = mul(unity_MatrixInvVP, positionCS);
                return hpositionWS.xyz / hpositionWS.w;
            }

            float3 calcWorldPos(float2 screenUV, float x, float y)
            {
                float2 uv = screenUV.xy + float2(x,y);
                float rawDepth = _Depth.Sample(sampler_Depth, uv).r;
                float3 worldPos = WorldSpacePositionFromDepth(uv, rawDepth);
                return worldPos;
            }

            half4 frag (v2f i) : SV_Target
            {
                float2 screen_uv = i.screenUV.xy;

                float3 worldPos = calcWorldPos(screen_uv, 0, 0);

                //return half4(worldPos.yyy / 100,1);

                float edge = 0;
                
                for(int it=0; it<numOfSamples; it++)
                {
                    float2 margins = float2(sampleMargins[it*2] * _EdgeDetectionSampleSize, sampleMargins[it*2+1] * _EdgeDetectionSampleSize); 
                    float3 wPosNear = calcWorldPos(screen_uv, margins.x, margins.y);
                    edge += saturate(abs(worldPos.y - wPosNear.y) / _EdgeDetectionHeight);
                }
                
                edge *= 1.0 / numOfSamples;

                edge = 1-saturate(edge * _EdgeStrength);

                //return half4(edge.rrr, 1);
                
                half4 color = tex2D(_MainTex, i.uv);
                color.rgb *= edge;

                half lum = tg_luminance(color);

                color = half4(lerp(lum.rrr, color.rgb, _Saturation), 1) * _ColorTint;

                color.rgb *= _FinalColorMult;

                return color;
            }
            ENDHLSL
        }
    }
}
