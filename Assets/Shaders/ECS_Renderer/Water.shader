Shader"Arena/Water"
{
    Properties
    {
        _Tiling_and_speed_1("Tiling and speed 1", Vector) = (1,1,1,1)
        _Tiling_and_speed_2("Tiling and speed 1", Vector) = (1,1,1,1)
        _Tiling_and_speed_3("Tiling and speed 3", Vector) = (1,1,1,1)
        _BaseColor("Color", Color) = (1,1,1,1)
        _BumpMap ("Normal map", 2D) = "bump" {}
        _Roughness("Roughness", Range(0,1)) = 1
        _Metallic("Metallic", Range(0,1)) = 1
        _Rim_mult("Rim multiplier", Float) = 1
        _Normal_strength("Normal strenght", Float) = 1
        
        [Toggle(USE_CUSTOM_FOG_COLOR)]
        _UseCustomFogColor("Use custom fog color", Float) = 0.0
        _CustomFogColor("Custom fog color", Color) = (1,1,1,1)
        [Toggle(USE_CUSTOM_REFLECTIONS)]
        _UseCustomReflections("Use custom reflections", Float) = 0
        [NoScaleOffset] _CustomReflectionTex("Custom reflection  (HDR)", Cube) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

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
            #pragma shader_feature __ USE_CUSTOM_REFLECTIONS
            #pragma shader_feature __ USE_CUSTOM_FOG_COLOR
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
                half3 color : COLOR;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
                //uint vid : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                half fogCoords : TEXCOORD1;
                half4 vertex : SV_POSITION;
                half3 color : TEXCOORD2;
                
                float3 normalWS : TEXCOORD3;
                float4 tangentWS : TEXCOORD4;
                float3 bitangentWS : TEXCOORD5;
                float3 positionWS : TEXCOORD6;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _CustomFogColor;
                half4 _Tiling_and_speed_1;
                half4 _Tiling_and_speed_2;
                half4 _Tiling_and_speed_3;
                half _Roughness;
                half _Metallic;
                half _Rim_mult;
                half _Normal_strength;
            CBUFFER_END

#if defined(DOTS_INSTANCING_ON)
                UNITY_DOTS_INSTANCING_START(UserPropertyMetadata)
                UNITY_DOTS_INSTANCING_END(UserPropertyMetadata)
#endif

            sampler2D _BumpMap;

            #if USE_CUSTOM_REFLECTIONS
            TEXTURECUBE(_CustomReflectionTex);
            SAMPLER(sampler_CustomReflectionTex);
            #endif
            
            
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

                o.uv = v.uv;//TRANSFORM_TEX(v.uv, _Bum);
                o.fogCoords = ComputeFogFactor(o.vertex.z);

    
                VertexNormalInputs normalInputs = GetVertexNormalInputs(normalOS, tangentOS);

                o.normalWS = normalInputs.normalWS;
                o.tangentWS = float4(normalInputs.tangentWS, tangentOS.w);
                o.bitangentWS = normalInputs.bitangentWS;

                o.color.rgb = v.color;
                
                return o;
            }

            void NormalStrength_float(float3 In, float Strength, out float3 Out)
            {
                Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
            }

            void NormalReconstructZ_float(float2 In, out float3 Out)
            {
                float reconstructZ = sqrt(1.0 - saturate(dot(In.xy, In.xy)));
                float3 normalVector = float3(In.x, In.y, reconstructZ);
                Out = normalize(normalVector);
            }

            half4 frag (v2f i) : SV_Target
            {
                //UNITY_SETUP_INSTANCE_ID(i);
                //float4 bc = UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, _BaseColor);

                //half3 normalTS = UnpackNormal(tex2D(_BumpMap, i.uv));

                float2 uv1 = i.uv * _Tiling_and_speed_1.xy;
                uv1 += _Tiling_and_speed_1.zw * _Time.y;
                float3 normalTS = tex2D(_BumpMap, uv1);

                float2 uv2 = i.uv * _Tiling_and_speed_2.xy;
                uv2 += _Tiling_and_speed_2.zw * _Time.y;
                normalTS.xy += tex2D(_BumpMap, uv2);

                float2 uv3 = i.uv * _Tiling_and_speed_3.xy;
                uv3 += _Tiling_and_speed_3.zw * _Time.y;
                normalTS.xy += tex2D(_BumpMap, uv3);

                normalTS.xy *= 0.3333333;
                
                NormalReconstructZ_float(normalTS.xy, normalTS);
                normalTS = UnpackNormal(float4(normalTS,0));

                NormalStrength_float(normalTS, _Normal_strength, normalTS);
                normalTS = normalize(normalTS);
                
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.positionWS);

                half3x3 tangentToWorld = half3x3(i.tangentWS.xyz, i.bitangentWS.xyz, i.normalWS.xyz);

                float3 normalWS = TransformTangentToWorld(normalTS.xyz, tangentToWorld, true);

                //mesm.rgb *= _Metallic;
                float rim = saturate(dot(normalWS, viewDirWS) * _Rim_mult);

                half reflectionLOD = _Roughness * 4;
                #if USE_CUSTOM_REFLECTIONS
                float3 reflectVec = reflect(-viewDirWS, normalWS);
	            //half3 reflColor = texCUBE(_CustomReflectionTex, reflectVec);
	            half3 reflColor = DecodeHDREnvironment(SAMPLE_TEXTURECUBE_LOD(_CustomReflectionTex, sampler_CustomReflectionTex, reflectVec, reflectionLOD), unity_SpecCube0_HDR);
                #else
                half3 reflColor = TG_ReflectionProbe(viewDirWS, normalWS, reflectionLOD);
                #endif
                
                half4 diffuse;
                diffuse.rgb = lerp(reflColor, _BaseColor.rgb, rim);
                diffuse.a = i.color.g;
                
                half4 finalColor = LightingPBR(diffuse, 1, viewDirWS, normalWS, _Metallic, _Roughness);

                // apply fog
                #if USE_CUSTOM_FOG_COLOR
                return half4(MixFogColor(diffuse, _CustomFogColor.rgb, i.fogCoords), finalColor.a);
                #else
                return half4(MixFogColor(diffuse, unity_FogColor.rgb, i.fogCoords), finalColor.a);
                #endif
                
            }
            ENDHLSL
        }
    }
}
