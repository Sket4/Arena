Shader "Arena/Sky Vertex Color"
{
    Properties
    {
        _TopColor("Top color", Color) = (1,1,1,1)
        _BottomColor("Bottom color", Color) = (0,0,0,1)
        _Height("Height", Float) = 20
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half3 color : COLOR0;
            };

            CBUFFER_START(UnityPerMaterial)
            half4 _TopColor;
            half4 _BottomColor;
            half _Height;
            CBUFFER_END
            
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);

                float3 positionOS = v.vertex;
                
                VertexPositionInputs vertInputs = GetVertexPositionInputs(positionOS);

                o.vertex = vertInputs.positionCS;
                
                half alpha = saturate(((vertInputs.positionWS.y / _Height) + 1.0) * 0.5);
                o.color = lerp(_BottomColor, _TopColor, alpha);
                
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                return half4(i.color,1);
            }
            ENDHLSL
        }
    }
}
