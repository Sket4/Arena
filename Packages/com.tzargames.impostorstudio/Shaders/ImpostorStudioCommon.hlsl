#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

struct appdata
{
	float4 vertex : POSITION;
	float2 uv : TEXCOORD0;
};

struct v2f
{
	float2 uv : TEXCOORD0;
	float4 vertex : SV_POSITION;
};

struct v2f_gauss
{
	float4 pos : POSITION;
	float2 uv : TEXCOORD0;
	float4 blur[7] : TEXCOORD1;
};

sampler2D _MainTex;
sampler2D _FirstTex;
sampler2D _SecondTex;
float2 blurCoords[14];
float _Blur;
float _BlurAlphaMultiply;

v2f_gauss vert_gausblur_h(appdata v)
{
	v2f_gauss o;
	VertexPositionInputs vertInputs = GetVertexPositionInputs(v.vertex);
			
	o.pos = vertInputs.positionCS;
	o.uv = v.uv.xy;

#if UNITY_UV_STARTS_AT_TOP
	if (o.uv.y < 0)
		o.uv.y = 1 - o.uv.y;
#endif
	// blur
	o.blur[0].xy = o.uv + float2(-0.028, 0.0) * _Blur;
	o.blur[0].zw = o.uv + float2(-0.024, 0.0) * _Blur;
	o.blur[1].xy = o.uv + float2(-0.020, 0.0) * _Blur;
	o.blur[1].zw = o.uv + float2(-0.016, 0.0) * _Blur;
	o.blur[2].xy = o.uv + float2(-0.012, 0.0) * _Blur;
	o.blur[2].zw = o.uv + float2(-0.008, 0.0) * _Blur;
	o.blur[3].xy = o.uv + float2(-0.004, 0.0) * _Blur;
	o.blur[3].zw = o.uv + float2(0.004, 0.0) * _Blur;
	o.blur[4].xy = o.uv + float2(0.008, 0.0) * _Blur;
	o.blur[4].zw = o.uv + float2(0.012, 0.0) * _Blur;
	o.blur[5].xy = o.uv + float2(0.016, 0.0) * _Blur;
	o.blur[5].zw = o.uv + float2(0.020, 0.0) * _Blur;
	o.blur[6].xy = o.uv + float2(0.024, 0.0) * _Blur;
	o.blur[6].zw = o.uv + float2(0.028, 0.0) * _Blur;


	return o;
}

v2f_gauss vert_gausblur_v(appdata v)
{
	v2f_gauss o;
	VertexPositionInputs vertInputs = GetVertexPositionInputs(v.vertex);
			
	o.pos = vertInputs.positionCS;
	o.uv = v.uv.xy;

#if UNITY_UV_STARTS_AT_TOP
	if (o.uv.y < 0)
		o.uv.y = 1 - o.uv.y;
#endif

	o.blur[0].xy = o.uv + float2(0.0, -0.028) * _Blur;
	o.blur[0].zw = o.uv + float2(0.0, -0.024) * _Blur;
	o.blur[1].xy = o.uv + float2(0.0, -0.020) * _Blur;
	o.blur[1].zw = o.uv + float2(0.0, -0.016) * _Blur;
	o.blur[2].xy = o.uv + float2(0.0, -0.012) * _Blur;
	o.blur[2].zw = o.uv + float2(0.0, -0.008) * _Blur;
	o.blur[3].xy = o.uv + float2(0.0, -0.004) * _Blur;
	o.blur[3].zw = o.uv + float2(0.0, 0.004) * _Blur;
	o.blur[4].xy = o.uv + float2(0.0, 0.008) * _Blur;
	o.blur[4].zw = o.uv + float2(0.0, 0.012) * _Blur;
	o.blur[5].xy = o.uv + float2(0.0, 0.016) * _Blur;
	o.blur[5].zw = o.uv + float2(0.0, 0.020) * _Blur;
	o.blur[6].xy = o.uv + float2(0.0, 0.024) * _Blur;
	o.blur[6].zw = o.uv + float2(0.0, 0.028) * _Blur;
	
	return o;
}

inline float4 computeBlurPixel(float4 source, float4 dest, float coeff)
{
	float4 result;
	result = dest + source * coeff;
	return result;
}

float4 frag_gausblur(v2f_gauss i) : COLOR
{
	float4 c = float4(0,0,0,0);
	float4 tmp = 0;

	tmp = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(i.blur[0].xy));
	c = computeBlurPixel(tmp, c, 0.0044299121055113265);

	tmp = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(i.blur[0].zw));
	c = computeBlurPixel(tmp, c, 0.00895781211794);

	tmp = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(i.blur[1].xy));
	c = computeBlurPixel(tmp, c, 0.0215963866053);

	tmp = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(i.blur[1].zw));
	c = computeBlurPixel(tmp, c, 0.0443683338718);

	tmp = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(i.blur[2].xy));
	c = computeBlurPixel(tmp, c, 0.0776744219933);

	tmp = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(i.blur[2].zw));
	c = computeBlurPixel(tmp, c, 0.115876621105);

	tmp = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(i.blur[3].xy));
	c = computeBlurPixel(tmp, c, 0.147308056121);

	tmp = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(i.uv));
	c = computeBlurPixel(tmp, c, 0.159576912161) ;

	tmp = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(i.blur[3].zw));
	c = computeBlurPixel(tmp, c, 0.147308056121);

	tmp = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(i.blur[4].xy));
	c = computeBlurPixel(tmp, c, 0.115876621105);

	tmp = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(i.blur[4].zw));
	c = computeBlurPixel(tmp, c, 0.0776744219933);

	tmp = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(i.blur[5].xy));
	c = computeBlurPixel(tmp, c, 0.0443683338718);

	tmp = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(i.blur[5].zw));
	c = computeBlurPixel(tmp, c, 0.0215963866053);

	tmp = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(i.blur[6].xy));
	c = computeBlurPixel(tmp, c, 0.00895781211794);

	tmp = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(i.blur[6].zw));
	c = computeBlurPixel(tmp, c, 0.0044299121055113265);

	float maxC = max(c.r, c.g);
	maxC = max(maxC, c.b);

	if (maxC > 0)
	{
		c.rgb /= maxC;
	}
			
	c.a *= maxC;

	c.a *= _BlurAlphaMultiply;
	c.a = clamp(c.a, 0.0, 1.0);

	return c;
}

float4 getSampledPixel(sampler2D source, float2 uv)
{
	float4 result = float4(0, 0, 0, 0);
	float4 temp;
	float4 orig = tex2D(source, uv);
			
	float sampleSize = 0.001;

	uint iterations = 1;
	uint radialIterations = 64;
	const float pi = 3.141592653589;

	for (uint i = 1; i <= iterations; i++)
	{
		float s = sampleSize * ((float)i / (float)iterations);

		temp = orig;

		for (uint j = 0; j < radialIterations; j++)
		{
			float angle =  2 * pi * ((float)j / (float)radialIterations);
			float2 radCoords = float2(sin(angle), cos(angle));
			radCoords *= s;

			temp += tex2D(source, uv + radCoords);
		}

		temp /= radialIterations + 1;

		result += temp;
	}

	result /= iterations;
	result = lerp(result, orig, result.a);
			
	return result;
}

v2f vert (appdata v)
{
	v2f o;
	VertexPositionInputs vertInputs = GetVertexPositionInputs(v.vertex);
			
	o.vertex = vertInputs.positionCS;
	o.uv = v.uv.xy;
	return o;
}

float4 frag_default (v2f i) : SV_Target
{
	float4 col = tex2D(_MainTex, i.uv);
	col.a = pow(col.a, 1.0 / 2.2);
	return col;
}	

float4 frag_mix_by_alpha(v2f i) : SV_Target
{
	float4 col = tex2D(_MainTex, i.uv);
	float4 col2 = tex2D(_SecondTex, i.uv);
	col2.rgb = lerp(col.rgb * col.a, col2.rgb, col2.a);
	return col2;
}

float4 frag_restore_alpha(v2f i) : SV_Target
{
	float4 col = tex2D(_FirstTex, i.uv);
	float4 col2 = tex2D(_SecondTex, i.uv);
	col.a = col2.a;
	return col;
}

float4 frag_norm (v2f i) : SV_Target
{
	float3 normalWS = SampleSceneNormals(i.uv);

	if(normalWS.z < 0.0)
	{
		normalWS.z = -normalWS.z;
	}
	else
	{
		normalWS.x = -normalWS.x;
	}

	float3 normal = TransformWorldToViewNormal(normalWS, true);
	
	normal = normal * 0.5 + 0.5;

	float4 col = float4(0,0,0,0);

	if(length(normalWS) > 0.000001)
	{
		col.rgb = normal;
	}
	else
	{
		col.rgb = float3(0.5,0.5,1);
	}
	
	col.rgb = pow(col.rgb, 2.2);	
	col.a = 1;

	return col;
}