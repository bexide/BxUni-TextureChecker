Shader "Unlit/Highpass3x3"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 delta = float4(1, 1,	0, -1) * _MainTex_TexelSize.xyxy;
            #if 1
                fixed4 col =
                    tex2D(_MainTex, i.uv - delta.xy) * -1/9
                  + tex2D(_MainTex, i.uv - delta.zy) * -1/9
                  + tex2D(_MainTex, i.uv + delta.xw) * -1/9
                  + tex2D(_MainTex, i.uv - delta.xz) * -1/9
                  + tex2D(_MainTex, i.uv + delta.zz) *  8/9
                  + tex2D(_MainTex, i.uv + delta.xz) * -1/9
                  + tex2D(_MainTex, i.uv - delta.xw) * -1/9
                  + tex2D(_MainTex, i.uv + delta.zy) * -1/9
                  + tex2D(_MainTex, i.uv + delta.xy) * -1/9;
                col = abs(col);
            #else
                fixed4 col =
                  tex2D(_MainTex, i.uv + delta.zz) * 2/3
                + tex2D(_MainTex, i.uv - delta.xz) * -1/3
                + tex2D(_MainTex, i.uv + delta.xz) * -1/3;
                col = abs(col);
            #endif
                return col;
            }
            ENDCG
        }
    }
}
