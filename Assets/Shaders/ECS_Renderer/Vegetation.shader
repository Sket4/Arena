Shader "Arena/Vegetation"
{
    Properties
    {
        [Toggle(DEBUG_VERTEX_COLOR)]
        _DebugVertexColor("Debug vertex color", float) = 0.0
    	
    	[Toggle(USE_BILLBOARD)]
    	_UseBillboard("Use billboard", int) = 0
        
        [Toggle] _ZWrite("ZWrite", int) = 1
        [Toggle(TG_USE_ALPHACLIP)] _AlphaClip("Use alpha clipping", float) = 0.0
        //[Enum(Off,0,On,1)] _AlphaToMask("Alpha to Mask", Int) = 0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        //[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Blend Source", float) = 1
        //[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Blend Destination", float) = 0

        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", int) = 2

        _BaseColor("Color tint", Color) = (1,1,1,1)
        _BaseColorMult("Base color mult", Float) = 1 
        _BaseMap ("Main color", 2D) = "white" {}
        _BumpMap ("Normal map", 2D) = "bump" {}
        _MetallicGlossMap("Metallic/Smoothness map", 2D) = "white" {}
        _Metallic("Metallic", Range(0,1)) = 1.0
    	_Smoothness ("Smoothness", Range(0,1)) = 0.5
        _AOAdd("AO add", Float) = 0
        _WindForce("Wind force", Float) = 1
        
        [HideInInspector]_EmissionColor("Emission color", Color) = (1,1,1,1)
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {} 
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {} 
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="TransparentCutout" 
        	"Queue"="AlphaTest"
        }
        LOD 100
        
        Pass
        {
        	Name "GBuffer"
            Tags
            {
                "LightMode" = "gbuffer"
            }
            
            //AlphaToMask[_AlphaToMask]
            //Blend[_SrcBlend][_DstBlend]
            Cull[_Cull]
            ZWrite[_ZWrite]
            
            Stencil 
            {
                Ref 128
                Comp Always
                Pass Replace
                Fail Keep
                ZFail Keep
            }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma require 2darray
            #pragma require cubearray
            #pragma exclude_renderers gles nomrt //excluded shader from OpenGL ES 2.0 because it uses non-square matrices
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma shader_feature_local __ DEBUG_VERTEX_COLOR
            #pragma shader_feature_local __ TG_TRANSPARENT
            #pragma shader_feature_local TG_USE_ALPHACLIP
            #pragma shader_feature_local_vertex __ USE_BILLBOARD
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            
            #pragma multi_compile UG_QUALITY_LOW UG_QUALITY_MED UG_QUALITY_HIGH
            #pragma multi_compile_fragment _ ARENA_MAP_RENDER

            
            #include "Common.hlsl"
            #include "Packages/com.tzargames.rendering/Shaders/Lighting.hlsl"
            #include "Packages/com.dgx.srp/ShaderLibrary/Lighting.hlsl"
            
            struct appdata
            {
                half3 vertex : POSITION;
                half3 normal : NORMAL;
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

            	normal = normalDir;
            	tangent = right;
            }

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                half3 positionOS = v.vertex;
				
                float4 time = _Time;
                half wind = (sin(time.z + (positionOS.x + positionOS.z) * 2) + sin(time.y) + sin(time.w)) * 0.05 * v.color.x;
                wind *= _WindForce;
                positionOS.x += wind;
                positionOS.z += wind;
                
                half3 normalOS = v.normal;
                half4 tangentOS = v.tangent;

            	#if USE_BILLBOARD
            	billboard(positionOS, normalOS, tangentOS.xyz);
            	#endif

                float3 positionWS = TransformObjectToWorld(positionOS);
				o.vertex = TransformWorldToHClip(positionWS);

                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                //o.positionWS_fog.a = ComputeFogFactor(o.vertex.z);

                float4 instanceData = tg_InstanceData;
                o.instanceData = instanceData;

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
                return i.normalWS_occl.w;
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
            	surface.Albedo = diffuse.rgb * lighting;
            	surface.Alpha = diffuse.a;
            	surface.NormalWS = normalWS;
            	surface.AmbientLight = 0;
            	

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
//            #pragma target 2.0
//            
//            #pragma vertex UniversalVertexMeta
//            #pragma fragment UniversalFragmentMetaCustom
//            #pragma shader_feature_local_fragment _SPECULAR_SETUP
//            #pragma shader_feature_local_fragment _EMISSION
//            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
//            #pragma shader_feature_local_fragment _ALPHATEST_ON
//            #pragma shader_feature_local_fragment _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
//            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
//            #pragma shader_feature_local_fragment _SPECGLOSSMAP
//            #pragma shader_feature EDITOR_VISUALIZATION
//            
//            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" 
//            
//            
//            CBUFFER_START(UnityPerMaterial)
//                half4 _BaseMap_ST;
//                half4 _BaseColor;
//                half4 _Underwater_color;
//                half _BaseColorMult;
//				half _Metallic;
//				half _Smoothness;
//                half _Cutoff;
//                half4 _EmissionColor;
//                half _WindForce;
//                half _AOAdd;
//            CBUFFER_END
//
//            #include "Packages/com.tzargames.rendering/Shaders/MetaPass.hlsl"
//            
//            ENDHLSL
//        }
    }
}
