Shader "CustomRenderTexture/Ellurs light light"
{
    Properties
    {
        _MainTex("InputTex", 2D) = "white" {}
        _BackgroundTex("Background", 2D) = "white" {}
     }

     SubShader
     {
        //Blend One Zero

        Pass
        {
            Name "Ellurs light light"

            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            sampler2D   _MainTex;
            sampler2D   _BackgroundTex;

            half4 frag(v2f_customrendertexture IN) : SV_Target
            {
                half color = tex2D(_BackgroundTex, IN.localTexcoord.xy + float2(_Time.x, -_Time.x)).r;
                color *= tex2D(_BackgroundTex, IN.localTexcoord.xy + float2(-_Time.x, _Time.x)).r;
                color *= tex2D(_BackgroundTex, IN.localTexcoord.xy + float2(_SinTime.x, _SinTime.x)).r;

                return half4(color,color,color,1);
            }
            ENDCG
        }
    }
}
