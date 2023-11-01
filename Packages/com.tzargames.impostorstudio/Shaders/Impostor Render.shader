Shader "Impostor Studio/Render"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_BlurAlphaMultiply("Blur alpha multiply", float) = 1.0
	}

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			
			//Name "Default"
			HLSLPROGRAM
			#include "ImpostorStudioCommon.hlsl"
			#pragma vertex vert
			#pragma fragment frag_default
			ENDHLSL
		}

		Pass
		{ 
			//Name "Normals"
			HLSLPROGRAM
			#include "ImpostorStudioCommon.hlsl"
			#pragma vertex vert
			#pragma fragment frag_norm
			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM
			#include "ImpostorStudioCommon.hlsl"
			#pragma vertex vert_gausblur_h
			#pragma fragment frag_gausblur
			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM
			#include "ImpostorStudioCommon.hlsl"
			#pragma vertex vert_gausblur_v
			#pragma fragment frag_gausblur
			ENDHLSL
		}

		Pass
		{
			Name "MIXER"
			HLSLPROGRAM
			#include "ImpostorStudioCommon.hlsl"
			#pragma vertex vert
			#pragma fragment frag_mix_by_alpha
			ENDHLSL
		}

		Pass
		{
			Name "RESTORE_ALPHA"
			HLSLPROGRAM
			#include "ImpostorStudioCommon.hlsl"
			#pragma vertex vert
			#pragma fragment frag_restore_alpha
			ENDHLSL
		}
	}
}
