Shader"Arena/Water"
{
    Properties
    {
        [Toggle(USE_THIRD_WAVE)]
        _UseCustomFogColor("Use third wave", Float) = 1.0
        
        _Tiling_and_speed_1("Tiling and speed 1", Vector) = (1,1,1,1)
        _Tiling_and_speed_2("Tiling and speed 1", Vector) = (1,1,1,1)
        _Tiling_and_speed_3("Tiling and speed 3", Vector) = (1,1,1,1)
        _BaseColor("Color", Color) = (1,1,1,1)
        _PackedNormalMap ("Packed normal map", 2D) = "white" {}
        _Roughness("Roughness", Range(0,1)) = 1
        _Rim_mult("Rim multiplier", Float) = 1
        
        [Toggle(USE_LOWER_NORMAL_FOG)]
        _UseLowerNormalFogByFog("Lower normal strength by fog", Float) = 1
        _Normal_strength("Normal strenght", Float) = 1
        _Normal_Y_Scale("Normal Y scale", Float) = 1
        
        [Toggle(USE_FOG)]
        _UseFog("Use fog", Float) = 1.0
        [Toggle(USE_CUSTOM_FOG_COLOR)]
        _UseCustomFogColor("Use custom fog color", Float) = 0.0
        _CustomFogColor("Custom fog color", Color) = (1,1,1,1)
        [Toggle(USE_CUSTOM_REFLECTIONS)]
        _UseCustomReflections("Use custom reflections", Float) = 0
        [NoScaleOffset] _CustomReflectionTex("Custom reflection  (HDR)", Cube) = "white" {}
        
        [Toggle(USE_FOAM)]
        _UseFoam("Use foam", Float) = 1.0
        [NoScaleOffset] _FoamGradientTex("Foam gradient", 2D) = "black" {}
        _FoamTex("Foam color", 2D) = "black" {}
        _FoamParameters("Foam params (x - scale, y - spd, zw - foam spd)", Vector) = (1,1,1,1)
        
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {} 
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {} 
        _TransparencyLM ("Transmissive Texture", 2D) = "white" {}
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
            #pragma require cubearray
            #pragma require 2darray
            #pragma exclude_renderers gles //excluded shader from OpenGL ES 2.0 because it uses non-square matrices
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma shader_feature __ USE_CUSTOM_REFLECTIONS
            #pragma shader_feature __ USE_CUSTOM_FOG_COLOR
            #pragma shader_feature __ USE_THIRD_WAVE
            #pragma shader_feature __ USE_LOWER_NORMAL_FOG
            #pragma shader_feature __ USE_FOG
            #pragma shader_feature __ USE_FOAM
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            //#pragma multi_compile_fragment __ ARENA_USE_MAIN_LIGHT
            //#pragma multi_compile_fragment __ ARENA_USE_ADD_LIGHT
            
            //#pragma multi_compile_fragment _ _SHADOWS_SOFT
            //#pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            //#pragma multi_compile_fragment _ _LIGHT_COOKIES
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
            #include "Packages/com.tzargames.rendering/Shaders/Lighting.hlsl"
            #include "Common.hlsl"

            struct appdata
            {
                float3 vertex : POSITION;
                half3 color : COLOR;
                float2 uv : TEXCOORD0;
#if LIGHTMAP_ON
                float2 uv2 : TEXCOORD1;
#endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 wave1_uv : TEXCOORD0;
                float2 wave2_uv : TEXCOORD1;
                nointerpolation half2 instanceData : TEXCOORD2;
                half3 color : TEXCOORD3;
                half3 foamData : TEXCOORD4;

                float4 positionWS_fog : TEXCOORD5;

                #if defined(USE_THIRD_WAVE)
                float2 wave3_uv : TEXCOORD6;
                #endif

#if LIGHTMAP_ON
                TG_DECLARE_LIGHTMAP_UV(7)
#endif
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _CustomFogColor;
                half4 _Tiling_and_speed_1;
                half4 _Tiling_and_speed_2;
                half4 _Tiling_and_speed_3;
                half _Roughness;
                half _Rim_mult;
                half _Normal_strength;
                half _Normal_Y_Scale;
                half4 _FoamParameters;
                half4 _FoamTex_ST;
            CBUFFER_END

#if defined(DOTS_INSTANCING_ON)
                UNITY_DOTS_INSTANCING_START(UserPropertyMetadata)
                UNITY_DOTS_INSTANCING_END(UserPropertyMetadata)
#endif

            sampler2D _PackedNormalMap;
            sampler2D _FoamTex;
            sampler2D _FoamGradientTex;

            #if USE_CUSTOM_REFLECTIONS
            TEXTURECUBE(_CustomReflectionTex);
            SAMPLER(sampler_CustomReflectionTex);
            #endif
            
            
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);

                float3 positionOS = v.vertex.xyz;

                o.positionWS_fog.xyz = TransformObjectToWorld(positionOS);
                o.vertex = TransformWorldToHClip(o.positionWS_fog.xyz); //This function calculates all the relative spaces of the objects vertices
                
                o.wave1_uv = v.uv * _Tiling_and_speed_1.xy;
                o.wave1_uv += _Tiling_and_speed_1.zw * _Time.y;

                o.wave2_uv = v.uv * _Tiling_and_speed_2.xy;
                o.wave2_uv += _Tiling_and_speed_2.zw * _Time.y;

                #if defined(USE_THIRD_WAVE)
                o.wave3_uv = v.uv * _Tiling_and_speed_3.xy;
                o.wave3_uv += _Tiling_and_speed_3.zw * _Time.y;
                #endif
                
                o.positionWS_fog.a = ComputeFogFactor(o.vertex.z);
                
                float4 instanceData = tg_InstanceData;
                o.instanceData = instanceData.xy;

                o.foamData.r = v.color.g * _FoamParameters.x + _Time.x * _FoamParameters.y;
                o.foamData.gb = TRANSFORM_TEX(v.uv, _FoamTex);
                o.foamData.gb += half2(_SinTime.w * _FoamParameters.z, _SinTime.w * _FoamParameters.w);
                
                o.color = v.color;

#if LIGHTMAP_ON
                TG_TRANSFORM_LIGHTMAP_TEX(v.uv2, o.lightmapUV)
#endif
                
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

                //half3 normalTS = UnpackNormal(tex2D(_PackedNormalMap, i.uv));

                //return half4(i.color.bbb, 1);

                float3 normalTS = tex2D(_PackedNormalMap, i.wave1_uv).xyz;
                 normalTS.xy += tex2D(_PackedNormalMap, i.wave2_uv).xy;

                #if defined(USE_THIRD_WAVE)
                 normalTS.xy += tex2D(_PackedNormalMap, i.wave3_uv).xy;
                 normalTS.xy *= 0.333333;
                #else
                normalTS.xy *= 0.5;
                #endif
                
                //NormalReconstructZ_float(normalTS.xy, normalTS);
                normalTS = UnpackNormal(float4(normalTS,0));

                #if defined(USE_LOWER_NORMAL_FOG) && (defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2))
                half normalFogStr = saturate(i.positionWS_fog.a);
                #else
                half normalFogStr = 1;
                #endif
                
                NormalStrength_float(normalTS, _Normal_strength * normalFogStr * i.color.b, normalTS);
                normalTS = normalize(normalTS);

                half3 normalWS = half3(0,1,0);
                half3 tangentWS = half3(1,0,0);
                half3 bitangentWS = half3(0,0,1);

                half3 tspace0 = half3(tangentWS.x, bitangentWS.x, normalWS.x);
                half3 tspace1 = half3(tangentWS.y, bitangentWS.y, normalWS.y);
                half3 tspace2 = half3(tangentWS.z, bitangentWS.z, normalWS.z);
                
                normalWS.x = dot(tspace0, normalTS);
                normalWS.y = dot(tspace1, normalTS);
                normalWS.z = dot(tspace2, normalTS);

                //mesm.rgb *= _Metallic;
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.positionWS_fog.xyz);
                
                float rim = saturate(dot(normalWS, viewDirWS) * _Rim_mult);
                //rim = sqrt(rim);


                half reflectionLOD = _Roughness * 4;
                #if USE_CUSTOM_REFLECTIONS
                float3 reflectVec = reflect(-viewDirWS, normalWS);

                #if defined(DOTS_INSTANCING_ON)
	            half3 reflColor = DecodeHDREnvironment(SAMPLE_TEXTURECUBE_LOD(_CustomReflectionTex, sampler_CustomReflectionTex, reflectVec, reflectionLOD), tg_ReflectionProbeDecodeInstructions);
                #else
                half3 reflColor = DecodeHDREnvironment(SAMPLE_TEXTURECUBE_LOD(_CustomReflectionTex, sampler_CustomReflectionTex, reflectVec, reflectionLOD), unity_SpecCube0_HDR);
                #endif
                #else
                half3 reflColor = TG_ReflectionProbe(viewDirWS, normalWS, i.instanceData.y, reflectionLOD);  
                #endif

                half waterEdgeFadeInv = 1.0 - i.color.g;
                
                half4 diffuse;
                diffuse.rgb = lerp(reflColor, _BaseColor.rgb, rim);
                diffuse.a = lerp(1, _BaseColor.a, rim);

                #if USE_FOAM
                half foam = tex2D(_FoamGradientTex, half2(i.foamData.r, 0)).r;
                foam *= tex2D(_FoamTex, i.foamData.gb).r;
                
                diffuse.rgb = lerp(diffuse.rgb, diffuse.rgb + foam.rrr, waterEdgeFadeInv);
                #endif
                

                
#if LIGHTMAP_ON
                half3 lighting = TG_SAMPLE_LIGHTMAP(i.lightmapUV, i.instanceData.x, normalWS);
#else
                half3 lighting = half3(1,1,1);
#endif

                ARENA_DYN_LIGHT(normalWS, i.positionWS_fog.xyz, lighting, viewDirWS, reflColor, true);
                
                diffuse.rgb *= lighting;

                //return diffuse.a;
                diffuse.a *= (i.color.g + i.color.r) * i.color.b;

                half4 finalColor = diffuse;
                
                // apply fog
                #if USE_FOG
                #if USE_CUSTOM_FOG_COLOR
                return half4(MixFog(finalColor.rgb, _CustomFogColor.rgb, i.positionWS_fog.a), finalColor.a);
                #else
                return half4(MixFog(finalColor.rgb, unity_FogColor.rgb, i.positionWS_fog.a), finalColor.a);
                #endif
                #else
                return finalColor;
                #endif
                
            }
            ENDHLSL
        }

//        Pass
//        {
//            Name "Meta"
//            Tags { "LightMode" = "Meta" }
//            
//            Cull Off
//            HLSLPROGRAM
//            
//            #pragma target 4.5
//            
//            #pragma vertex UniversalVertexMeta
//            #pragma fragment UniversalFragmentMetaCustom
//            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
//            #pragma shader_feature_local_fragment _SPECGLOSSMAP
//            #pragma shader_feature EDITOR_VISUALIZATION
//            
//            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" 
//            
//             
//            CBUFFER_START(UnityPerMaterial)
//                half4 _BaseColor;
//                half4 _CustomFogColor;
//                half4 _Tiling_and_speed_1;
//                half4 _Tiling_and_speed_2;
//                half4 _Tiling_and_speed_3;
//                half _Roughness;
//                half _Rim_mult;
//                half _Normal_strength;
//                half4 _FoamParameters;
//                half4 _FoamTex_ST;
//            CBUFFER_END
//
//            half4 _BaseMap_ST;
//            half4 _EmissionColor;
//            
//            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl"
//                         
//            half4 UniversalFragmentMetaCustom(Varyings fragIn) : SV_Target
//            {
//                MetaInput metaInput;
//                            
//                metaInput.Albedo = float3(1,1,1);
//                metaInput.Emission = 1;
//                            
//                #ifdef EDITOR_VISUALIZATION
//                metaInput.VizUV = fragIn.VizUV;
//                metaInput.LightCoord = fragIn.LightCoord;
//                #endif
//
//                half4 result = UnityMetaFragment(metaInput);
//                return result;
//            }
//            
//            ENDHLSL
//        }
    }
}
