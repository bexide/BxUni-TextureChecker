Shader "Unlit/Highpass5x5"
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
                float4 d1 = float4(1, 1, 0, 0) * _MainTex_TexelSize.xyxy;
                float4 d2 = float4(2, 2, 0, 0) * _MainTex_TexelSize.xyxy;

                fixed4 col =
                    tex2D(_MainTex, i.uv - d2.xz - d2.zy) * 0
                  + tex2D(_MainTex, i.uv - d1.xz - d2.zy) * -1/25
                  + tex2D(_MainTex, i.uv - d1.zz - d2.zy) *  1/25
                  + tex2D(_MainTex, i.uv + d1.xz - d2.zy) * -1/25
                  + tex2D(_MainTex, i.uv + d2.xz - d2.zy) * 0

                  + tex2D(_MainTex, i.uv - d2.xz - d1.zy) * -1/25
                  + tex2D(_MainTex, i.uv - d1.xz - d1.zy) *  2/25
                  + tex2D(_MainTex, i.uv - d1.zz - d1.zy) * -4/25
                  + tex2D(_MainTex, i.uv + d1.xz - d1.zy) *  2/25
                  + tex2D(_MainTex, i.uv + d2.xz - d1.zy) * -1/25

                  + tex2D(_MainTex, i.uv - d2.xz - d1.zz) *  1/25
                  + tex2D(_MainTex, i.uv - d1.xz - d1.zz) * -4/25
                  + tex2D(_MainTex, i.uv - d1.zz - d1.zz) * 13/25
                  + tex2D(_MainTex, i.uv + d1.xz - d1.zz) * -4/25
                  + tex2D(_MainTex, i.uv + d2.xz - d1.zz) *  1/25

                  + tex2D(_MainTex, i.uv - d2.xz + d1.zy) * -1/25
                  + tex2D(_MainTex, i.uv - d1.xz + d1.zy) *  2/25
                  + tex2D(_MainTex, i.uv - d1.zz + d1.zy) * -4/25
                  + tex2D(_MainTex, i.uv + d1.xz + d1.zy) *  2/25
                  + tex2D(_MainTex, i.uv + d2.xz + d1.zy) * -1/25

                  + tex2D(_MainTex, i.uv - d2.xz + d2.zy) * 0
                  + tex2D(_MainTex, i.uv - d1.xz + d2.zy) * -1/25
                  + tex2D(_MainTex, i.uv - d1.zz + d2.zy) *  1/25
                  + tex2D(_MainTex, i.uv + d1.xz + d2.zy) * -1/25
                  + tex2D(_MainTex, i.uv + d2.xz + d2.zy) * 0;

                col = abs(col);
                return col;
            }
            ENDCG
        }
    }
}
