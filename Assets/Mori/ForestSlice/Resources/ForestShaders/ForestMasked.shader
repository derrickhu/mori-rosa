Shader "Mori/ForestMasked"
{
    // 林冠 / 树干带 / 林冠投影 共用：按开垦遮罩挖空，边缘用林冠高度图切出一团团树冠轮廓
    Properties
    {
        _MainTex ("Texture (A = crown height)", 2D) = "white" {}
        _Mask ("Forest Mask (R: 1 = forest)", 2D) = "white" {}
        _Tiling ("World Units Per Repeat", Float) = 4
        _Color ("Color", Color) = (1,1,1,1)
        _Threshold ("Threshold", Float) = 0.5
        _Soft ("Edge Softness", Float) = 0.03
        _HeightAmp ("Crown Height Influence", Float) = 0.6
        _UseTex ("Use Texture", Float) = 1
        _EdgeDark ("Edge Darken", Float) = 0.25
        _LiftUV ("Lift (mask uv offset)", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _Mask;
            float _Tiling, _Threshold, _Soft, _HeightAmp, _UseTex, _EdgeDark;
            float4 _LiftUV;
            fixed4 _Color;

            struct a2v { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float2 wp : TEXCOORD1; };

            v2f vert (a2v v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.wp = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 t = tex2D(_MainTex, i.wp / _Tiling);
                // 林冠抬高：取“自己”和“正下方 lift 处”两点的较小值。
                // 后排林缘被抬高、露出树干带；前排林缘不会盖进开垦地。
                float m = min(tex2D(_Mask, i.uv).r, tex2D(_Mask, i.uv + _LiftUV.xy).r);
                float v = m + (t.a - 0.5) * _HeightAmp;
                float a = smoothstep(_Threshold - _Soft, _Threshold + _Soft, v);
                fixed3 rgb = lerp(fixed3(1,1,1), t.rgb, _UseTex) * _Color.rgb;
                float inner = saturate((m - _Threshold) * 3.0);
                rgb *= lerp(1.0 - _EdgeDark, 1.0, inner);
                return fixed4(rgb, a * _Color.a);
            }
            ENDCG
        }
    }
}
