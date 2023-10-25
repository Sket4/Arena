Shader"Arena/Window"
{
    Properties
    {
        [Toggle(USE_LIGHTING)]
        _UseLighting("Use lighting", float) = 1
        
        _Color1("Color 1", Color) = (1,1,1,1)
        _Color2("Color 2", Color) = (1,1,1,1)
        _BaseMap ("Mask", 2D) = "white" {}
        _BumpMap ("Normal map", 2D) = "bump" {}

        _Roughness("Roughness", Range(0,1)) = 1
        _Metallic("Metallic", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma target 4.5
            #pragma exclude_renderers gles //excluded shader from OpenGL ES 2.0 because it uses non-square matrices
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma shader_feature __ USE_LIGHTING
            //#pragma multi_compile_instancing

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl" 
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.tzargames.renderer/Shaders/Lighting.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
                
                //uint vid : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                half2 uv : TEXCOORD0;
                half fogCoords : TEXCOORD1;
                half4 vertex : SV_POSITION;
                half3 color : TEXCOORD2;
                
                float3 normalWS : TEXCOORD3;
                float4 tangentWS : TEXCOORD4;
                float3 bitangentWS : TEXCOORD5;
                float3 positionWS : TEXCOORD6;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseMap_ST;
                half4 _Color1;
                half4 _Color2;
                half _Roughness;
                half _Metallic;
            CBUFFER_END

#if defined(DOTS_INSTANCING_ON)
                UNITY_DOTS_INSTANCING_START(UserPropertyMetadata)
                UNITY_DOTS_INSTANCING_END(UserPropertyMetadata)
#endif
            
            sampler2D _BaseMap;
            sampler2D _BumpMap;
            
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);

                float3 positionOS = v.vertex;
                float3 normalOS = v.normal;
                float4 tangentOS = v.tangent;

                VertexPositionInputs vertInputs = GetVertexPositionInputs(positionOS);    //This function calculates all the relative spaces of the objects vertices
                o.vertex = vertInputs.positionCS;
                o.positionWS = vertInputs.positionWS;

                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fogCoords = ComputeFogFactor(o.vertex.z);

    
                 VertexNormalInputs normalInputs = GetVertexNormalInputs(normalOS, tangentOS);

                o.normalWS = normalInputs.normalWS;
                o.tangentWS = float4(normalInputs.tangentWS, tangentOS.w);
                o.bitangentWS = normalInputs.bitangentWS;

                //float4 bc = UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, _BaseColor);
                half3 bakedGI_Color = SHADERGRAPH_BAKED_GI(vertInputs.positionWS, normalInputs.normalWS, half2(0, 0), half2(0, 0), true);
                o.color.rgb = bakedGI_Color;
                
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                //UNITY_SETUP_INSTANCE_ID(i);
                half mask = tex2D(_BaseMap, i.uv).r;
                half4 diffuse = lerp(_Color1, _Color2, mask);
    
                half3 normalTS = UnpackNormal(tex2D(_BumpMap, i.uv));
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.positionWS);

                half3x3 tangentToWorld = half3x3(i.tangentWS.xyz, i.bitangentWS.xyz, i.normalWS.xyz);

                float3 normalWS = TransformTangentToWorld(normalTS.xyz, tangentToWorld, true);

                half roughness = _Roughness * lerp(_Color1.a, _Color2.a, mask);

                half4 finalColor = LightingPBR(diffuse, i.color.rgb, viewDirWS, normalWS, _Metallic, roughness);

                // apply fog
                return half4(MixFog(finalColor, i.fogCoords), finalColor.a);
            }
            ENDHLSL
        }
    }
}
