// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Tree Pack/Standard/Leaf/Cull OFF/Leaf"
{
	Properties
	{
		[Header(Translucency)]
		_Translucency("Strength", Range( 0 , 50)) = 1
		_TransNormalDistortion("Normal Distortion", Range( 0 , 1)) = 0.1
		_TransScattering("Scaterring Falloff", Range( 1 , 50)) = 2
		_TransDirect("Direct", Range( 0 , 1)) = 1
		_TransAmbient("Ambient", Range( 0 , 1)) = 0.2
		_TransShadow("Shadow", Range( 0 , 1)) = 0.9
		_Cutoff( "Mask Clip Value", Float ) = 0.6
		_Color("Color", Color) = (1,1,1,1)
		_ColorIntensity("Color Intensity", Range( 0 , 10)) = 1
		_Glossiness("_Glossiness", Range( 0 , 3)) = 0
		_LocalWindScale("Local Wind Scale", Range( 0.03 , 1)) = 0.354
		_LocalWindIntensity("Local Wind Intensity", Range( 0 , 1)) = 0.191
		_LocalWindSpeed("Local Wind Speed", Range( 0 , 7)) = 0.43
		_MainTex("Albedo", 2D) = "white" {}
		_BumpMap("Normal", 2D) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "AlphaTest+0" }
		Cull Off
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#include "UnityPBSLighting.cginc"
		#pragma target 3.0
		#pragma surface surf StandardSpecularCustom keepalpha addshadow fullforwardshadows exclude_path:deferred vertex:vertexDataFunc 
		struct Input
		{
			float3 worldPos;
			float2 uv_texcoord;
		};

		struct SurfaceOutputStandardSpecularCustom
		{
			half3 Albedo;
			half3 Normal;
			half3 Emission;
			half3 Specular;
			half Smoothness;
			half Occlusion;
			half Alpha;
			half3 Translucency;
		};

		uniform float WindDirection;
		uniform float WorldFrequency;
		uniform float WindSpeed;
		uniform float BendAmount;
		uniform float _LocalWindSpeed;
		uniform float _LocalWindScale;
		uniform float _LocalWindIntensity;
		uniform sampler2D _BumpMap;
		uniform float4 _BumpMap_ST;
		uniform float4 _Color;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform float _ColorIntensity;
		uniform float _Glossiness;
		uniform half _Translucency;
		uniform half _TransNormalDistortion;
		uniform half _TransScattering;
		uniform half _TransDirect;
		uniform half _TransAmbient;
		uniform half _TransShadow;
		uniform float _Cutoff = 0.6;


		float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
		{
			original -= center;
			float C = cos( angle );
			float S = sin( angle );
			float t = 1 - C;
			float m00 = t * u.x * u.x + C;
			float m01 = t * u.x * u.y - S * u.z;
			float m02 = t * u.x * u.z + S * u.y;
			float m10 = t * u.x * u.y + S * u.z;
			float m11 = t * u.y * u.y + C;
			float m12 = t * u.y * u.z - S * u.x;
			float m20 = t * u.x * u.z - S * u.y;
			float m21 = t * u.y * u.z + S * u.x;
			float m22 = t * u.z * u.z + C;
			float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
			return mul( finalMatrix, original ) + center;
		}


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 ase_vertex3Pos = v.vertex.xyz;
			float3 ase_worldPos = mul( unity_ObjectToWorld, v.vertex );
			float temp_output_20_0 = ( ( ase_vertex3Pos.y * cos( ( ( ( ase_worldPos.x + ase_worldPos.z ) * WorldFrequency ) + ( _Time.z * WindSpeed ) ) ) ) * BendAmount );
			float4 appendResult23 = (float4(temp_output_20_0 , 0.0 , temp_output_20_0 , 0.0));
			float4 break29 = mul( appendResult23, unity_ObjectToWorld );
			float4 appendResult32 = (float4(break29.x , 0 , break29.z , 0.0));
			float3 rotatedValue38 = RotateAroundAxis( float3( 0,0,0 ), appendResult32.xyz, float3( 0,0,0 ), WindDirection );
			float2 appendResult33 = (float2(0.0 , ( v.texcoord.xy.x * sin( ( ( ase_vertex3Pos.x + 0.0 + ( _Time.y * _LocalWindSpeed ) ) / _LocalWindScale ) ) )));
			v.vertex.xyz += ( rotatedValue38 + float3( ( appendResult33 * _LocalWindIntensity ) ,  0.0 ) );
			v.vertex.w = 1;
		}

		inline half4 LightingStandardSpecularCustom(SurfaceOutputStandardSpecularCustom s, half3 viewDir, UnityGI gi )
		{
			#if !defined(DIRECTIONAL)
			float3 lightAtten = gi.light.color;
			#else
			float3 lightAtten = lerp( _LightColor0.rgb, gi.light.color, _TransShadow );
			#endif
			half3 lightDir = gi.light.dir + s.Normal * _TransNormalDistortion;
			half transVdotL = pow( saturate( dot( viewDir, -lightDir ) ), _TransScattering );
			half3 translucency = lightAtten * (transVdotL * _TransDirect + gi.indirect.diffuse * _TransAmbient) * s.Translucency;
			half4 c = half4( s.Albedo * translucency * _Translucency, 0 );

			SurfaceOutputStandardSpecular r;
			r.Albedo = s.Albedo;
			r.Normal = s.Normal;
			r.Emission = s.Emission;
			r.Specular = s.Specular;
			r.Smoothness = s.Smoothness;
			r.Occlusion = s.Occlusion;
			r.Alpha = s.Alpha;
			return LightingStandardSpecular (r, viewDir, gi) + c;
		}

		inline void LightingStandardSpecularCustom_GI(SurfaceOutputStandardSpecularCustom s, UnityGIInput data, inout UnityGI gi )
		{
			#if defined(UNITY_PASS_DEFERRED) && UNITY_ENABLE_REFLECTION_BUFFERS
				gi = UnityGlobalIllumination(data, s.Occlusion, s.Normal);
			#else
				UNITY_GLOSSY_ENV_FROM_SURFACE( g, s, data );
				gi = UnityGlobalIllumination( data, s.Occlusion, s.Normal, g );
			#endif
		}

		void surf( Input i , inout SurfaceOutputStandardSpecularCustom o )
		{
			float2 uv_BumpMap = i.uv_texcoord * _BumpMap_ST.xy + _BumpMap_ST.zw;
			o.Normal = tex2D( _BumpMap, uv_BumpMap ).rgb;
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float4 tex2DNode39 = tex2D( _MainTex, uv_MainTex );
			o.Albedo = ( ( _Color * tex2DNode39 ) * _ColorIntensity ).rgb;
			float3 temp_cast_2 = (0.0).xxx;
			o.Specular = temp_cast_2;
			o.Smoothness = ( _Glossiness * tex2DNode39.g );
			float3 temp_cast_3 = (0.1).xxx;
			o.Translucency = temp_cast_3;
			o.Alpha = 1;
			clip( tex2DNode39.a - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18935
0;0;800;539;413.7962;1469.17;1.22754;True;False
Node;AmplifyShaderEditor.CommentaryNode;2;-2473.914,-10.64709;Inherit;False;2402.447;754.1009;Bark Wind;21;38;35;32;31;29;27;24;23;20;18;17;14;12;11;9;8;7;6;5;4;3;Bark Wind;1,1,1,1;0;0
Node;AmplifyShaderEditor.WorldPosInputsNode;3;-2423.914,79.44489;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;4;-2441.969,556.2927;Inherit;False;Global;WindSpeed;Wind Speed;5;0;Create;True;0;0;0;False;0;False;1;0;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;5;-2410.348,252.0758;Inherit;False;Global;WorldFrequency;World Frequency;2;0;Create;True;0;0;0;False;0;False;0.07;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;6;-2207.97,103.0235;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TimeNode;7;-2439.624,386.9828;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;-2101.968,472.2927;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;9;-2065.022,153.6048;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;10;-699.5921,999.6538;Inherit;False;1512.816;522.3508;Leaf Animation;13;40;36;33;30;28;26;25;22;21;19;16;15;13;Leaf Animation;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode;11;-1907.551,268.3898;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CosOpNode;14;-1824.745,388.8967;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;15;-604.6747,1346.917;Inherit;False;Property;_LocalWindSpeed;Local Wind Speed;14;0;Create;True;0;0;0;False;0;False;0.43;0.17;0;7;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;13;-566.3862,1228.437;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;12;-1817.802,39.35291;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;16;-327.2271,1256.7;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;-1636.252,233.3058;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;18;-1690.255,406.0758;Inherit;False;Global;BendAmount;Bend Amount;3;0;Create;True;0;0;0;False;0;False;0.04;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;19;-538.9321,1049.654;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;20;-1417.895,265.0328;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;21;-189.9297,1134.266;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;22;-290.4052,1383.676;Inherit;False;Property;_LocalWindScale;Local Wind Scale;12;0;Create;True;0;0;0;False;0;False;0.354;0.085;0.03;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;23;-1240.089,217.8398;Inherit;True;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.ObjectToWorldMatrixNode;24;-1256.139,491.8497;Inherit;False;0;1;FLOAT4x4;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;25;-27.25961,1219.466;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;26;32.82529,1053.37;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;27;-1015.821,395.1168;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT4x4;0,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SinOpNode;28;135.3083,1268.005;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;29;-842.1532,399.5708;Inherit;True;FLOAT4;1;0;FLOAT4;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;30;361.7313,1082.444;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;31;-960.5889,161.6968;Inherit;False;Constant;_Vector0;Vector 0;3;0;Create;True;0;0;0;False;0;False;0,0,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.CommentaryNode;34;-906.6581,-1143.709;Inherit;False;713.8566;533.4713;Albedo;5;49;48;46;42;39;Albedo;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;35;-680.3416,627.4537;Inherit;False;Global;WindDirection;Wind Direction;4;0;Create;True;0;0;0;False;0;False;2.85;0;0;6.25;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;49;-821.4894,-1095.054;Inherit;False;Property;_Color;Color;9;0;Create;False;0;0;0;False;0;False;1,1,1,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;33;470.8846,1195.173;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;36;341.0022,1322.982;Inherit;False;Property;_LocalWindIntensity;Local Wind Intensity;13;0;Create;True;0;0;0;False;0;False;0.191;0.191;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;32;-561.1001,303.5808;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SamplerNode;39;-856.6581,-915.2299;Inherit;True;Property;_MainTex;Albedo;15;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;42;-846.6075,-726.2377;Inherit;False;Property;_ColorIntensity;Color Intensity;10;0;Create;True;0;0;0;False;0;False;1;1;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;37;93.68759,-1154.125;Inherit;False;Property;_Glossiness;_Glossiness;11;0;Create;True;0;0;0;False;0;False;0;0;0;3;0;1;FLOAT;0
Node;AmplifyShaderEditor.RotateAboutAxisNode;38;-391.4667,448.8218;Inherit;False;False;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;40;651.2242,1208.141;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;48;-532.3825,-966.3231;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;43;378.8407,-417.324;Inherit;False;Property;_AlphaClip;Alpha Clip;8;0;Create;True;0;0;0;False;0;False;0.5;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;46;-354.8015,-860.7579;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;45;737.8892,-130.1877;Inherit;False;Constant;_Float0;Float 0;9;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;50;611.8445,71.75665;Inherit;False;Constant;_TranslucencyIntensity;Translucency Intensity;17;0;Create;True;0;0;0;False;0;False;0.1;0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;41;-874.496,-519.9604;Inherit;True;Property;_BumpMap;Normal;16;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;True;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;44;289.0999,270.5687;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT2;0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;47;406.0828,-1145.46;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;976.4384,-186.9776;Float;False;True;-1;2;ASEMaterialInspector;0;0;StandardSpecular;Tree Pack/Standard/Leaf/Cull OFF/Leaf;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Off;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.6;True;True;0;True;Opaque;;AlphaTest;ForwardOnly;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;7;0;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;6;0;3;1
WireConnection;6;1;3;3
WireConnection;8;0;7;3
WireConnection;8;1;4;0
WireConnection;9;0;6;0
WireConnection;9;1;5;0
WireConnection;11;0;9;0
WireConnection;11;1;8;0
WireConnection;14;0;11;0
WireConnection;16;0;13;0
WireConnection;16;1;15;0
WireConnection;17;0;12;2
WireConnection;17;1;14;0
WireConnection;20;0;17;0
WireConnection;20;1;18;0
WireConnection;21;0;19;1
WireConnection;21;2;16;0
WireConnection;23;0;20;0
WireConnection;23;2;20;0
WireConnection;25;0;21;0
WireConnection;25;1;22;0
WireConnection;27;0;23;0
WireConnection;27;1;24;0
WireConnection;28;0;25;0
WireConnection;29;0;27;0
WireConnection;30;0;26;1
WireConnection;30;1;28;0
WireConnection;33;1;30;0
WireConnection;32;0;29;0
WireConnection;32;1;31;2
WireConnection;32;2;29;2
WireConnection;38;1;35;0
WireConnection;38;3;32;0
WireConnection;40;0;33;0
WireConnection;40;1;36;0
WireConnection;48;0;49;0
WireConnection;48;1;39;0
WireConnection;46;0;48;0
WireConnection;46;1;42;0
WireConnection;44;0;38;0
WireConnection;44;1;40;0
WireConnection;47;0;37;0
WireConnection;47;1;39;2
WireConnection;0;0;46;0
WireConnection;0;1;41;0
WireConnection;0;3;45;0
WireConnection;0;4;47;0
WireConnection;0;7;50;0
WireConnection;0;10;39;4
WireConnection;0;11;44;0
ASEEND*/
//CHKSM=628F0BF269A325F72031BF6DB12F1E51E33E9D8B