Shader "Custom/WaterFoam"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    	_CoastEdgeColor ("Cost edge color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
		[NoScaleOffset] _BumpMap ("Normalmap", 2D) = "bump" {}
		_BumpInt("Normal Intensity", float) = 1.0
		_FoamColor("Foam Color", Color) = (1,1,1,1)
		_FoamTex("Foam texture", 2D) = "white" {}
    	_FoamGradient("Foam gradient", 2D) = "white" {}
		_WavesScalePan1("Waves 1 Scale (RG) Speed (BA)", Vector) = (1,1,0,0)
		_WavesScalePan2("Waves 2 Scale (RG) Speed (BA)", Vector) = (1,1,0,0)	
		_FoamStr("Foam strength", Range(0,1)) = 1
		_FoamSpeedScale("Foam speed scale", Float) = 0.2
		_Cube ("Cubemap", CUBE) = "" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" }
        LOD 200
        
        
        Pass
        {
        	Blend SrcAlpha OneMinusSrcAlpha
        //Cull Back
        //ZWrite On

        HLSLPROGRAM

        #pragma target 4.5
        #pragma require 2darray
        #pragma require cubearray
        #pragma exclude_renderers gles //excluded shader from OpenGL ES 2.0 because it uses non-square matrices
        #pragma vertex vert
        #pragma fragment frag
        // make fog work
        #pragma multi_compile_fog
        #pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma shader_feature __ TG_TRANSPARENT
        #pragma shader_feature TG_USE_ALPHACLIP
        #pragma shader_feature USE_UNDERWATER
        #pragma shader_feature DIFFUSE_ALPHA_AS_SMOOTHNESS
        #pragma shader_feature USE_SURFACE_BLEND
        #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.tzargames.rendering/Shaders/Lighting.hlsl"

        struct appdata
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float4 tangent : TANGENT;
            float2 uv : TEXCOORD0;
#if LIGHTMAP_ON
            TG_DECLARE_LIGHTMAP_UV(1)
#endif

            half4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct v2f
        {
            half4 vertex : SV_POSITION;
            float2 uv_MainTex : TEXCOORD0;
            nointerpolation half4 instanceData : TEXCOORD1;

            float3 normalWS : TEXCOORD2;
            float4 tangentWS : TEXCOORD3;
            float3 bitangentWS : TEXCOORD4;
            float4 positionWS_fog : TEXCOORD5;

        	float2 uv_FoamTex : TEXCOORD6;
            half4 color : TEXCOORD7;
#if LIGHTMAP_ON
            TG_DECLARE_LIGHTMAP_UV(8)
#endif
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        CBUFFER_START(UnityPerMaterial)
            half _BumpInt;
			half4 _MainTex_ST;
			half4 _FoamTex_ST;
			half4 _WavesScalePan1;
			half4 _WavesScalePan2;
			half _FoamStr;
			half _FoamSpeedScale;
	        half4 _Color;
			half4 _FoamColor;
	        half4 _CoastEdgeColor;
        CBUFFER_END

        sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _FoamTex;
		sampler2D _FoamGradient;
		samplerCUBE _Cube;
        

        v2f vert (appdata v)
        { 
            v2f o;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_TRANSFER_INSTANCE_ID(v, o);

            float3 positionOS = v.vertex;
            float3 normalOS = v.normal;
            float4 tangentOS = v.tangent;
            
            VertexPositionInputs vertInputs = GetVertexPositionInputs(positionOS);    //This function calculates all the relative spaces of the objects vertices
            o.vertex = vertInputs.positionCS;
            o.positionWS_fog.xyz = vertInputs.positionWS;

            o.uv_MainTex = TRANSFORM_TEX(v.uv, _MainTex);
        	o.uv_FoamTex = TRANSFORM_TEX(v.uv, _FoamTex);
            o.positionWS_fog.w = ComputeFogFactor(o.vertex.z);

            float4 instanceData = tg_InstanceData;
            o.instanceData = instanceData;

            VertexNormalInputs normalInputs = GetVertexNormalInputs(normalOS, tangentOS);

            o.normalWS = normalInputs.normalWS;
            o.tangentWS = float4(normalInputs.tangentWS, tangentOS.w);
            o.bitangentWS = normalInputs.bitangentWS;

#if LIGHTMAP_ON
            TG_TRANSFORM_LIGHTMAP_TEX(v.lightmapUV, o.lightmapUV)
            o.color.rgb = 0;
#endif

        	o.color = v.color;
            
            return o;
        }

        half4 frag(v2f i) : SV_Target
		{
			UNITY_SETUP_INSTANCE_ID(i);

			//return half4(i.color.rgb, 1);
			
			half4 c1 = tex2D(_MainTex, i.uv_MainTex * _WavesScalePan1.rg + _Time * _WavesScalePan1.ba);
			half4 c2 = tex2D(_MainTex, i.uv_MainTex * _WavesScalePan2.rg + _Time * _WavesScalePan2.ba);
			half4 c = (c1 + c2) * 0.5;

        	half intensityFactor = i.color.r;
        	
        	c.rgb = lerp(c.rgb * _Color.rgb, c.rgb * _CoastEdgeColor.rgb, intensityFactor);
			
			half3 foamGradient = 1 - tex2D(_FoamGradient, float2(intensityFactor - _Time.y * _FoamSpeedScale, 0) /*+ bump.xy * 0.15*/);
			//float2 foamDistortUV = bump.xy * 0.2;

        	half fakeFoamDisp = foamGradient.r * 0.02;
			half3 foamColor = _FoamColor.rgb * tex2D(_FoamTex, i.uv_FoamTex.xy + float2(fakeFoamDisp, 0)).rgb;
			foamColor += foamGradient * (intensityFactor) * foamColor;

        	half foamBlend = intensityFactor * foamGradient.r * _FoamStr;

			half3 b1 = UnpackNormal(tex2D(_BumpMap, i.uv_MainTex * _WavesScalePan1.rg + _Time * _WavesScalePan1.ba));
			half3 b2 = UnpackNormal(tex2D(_BumpMap, i.uv_MainTex * _WavesScalePan2.rg + _Time * _WavesScalePan2.ba));
			half3 b = b1 + b2;
			b.rg *= _BumpInt;
			b = normalize(b);
			
			half3 Normal = b;
        	
			c.rgb = lerp(c.rgb, foamColor.rgb, foamBlend);
			
        	//o.Albedo = half3(IN.vertexColor.r, IN.vertexColor.r, IN.vertexColor.r);

			// reflections!!!!
			//o.Emission = texCUBE (_Cube, WorldReflectionVector (IN, o.Normal)).rgb * (1.0 - intensityFactor);

			return c;
        }
        ENDHLSL
        }
    }
}
