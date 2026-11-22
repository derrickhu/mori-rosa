Shader "Mori/ForestGround"
{
    // 唯一的连续地面：荒土平铺 + 固定河道 + 林缘赛璐璐地面投影带 + 深林林下腐殖暗底
    // 关键保证：第一排林缘树根处 (Mask.g ≈ 0) 永远是扎实的暖黄褐土地，深林暗底只从第二排树后方柔和过渡展开。
    Properties
    {
        _MainTex ("Ground (A = noise)", 2D) = "white" {}
        _River ("River Mask (R)", 2D) = "black" {}
        _Mask ("Forest Mask (R=edge, G=deep)", 2D) = "black" {}
        _Tiling ("World Units Per Repeat", Float) = 4
        _SunOffset ("Sun UV Offset", Vector) = (0.003, 0.013, 0, 0)
        _BankCol ("Bank", Color) = (0.42,0.36,0.25,1)
        _WaterCol ("Water", Color) = (0.36,0.52,0.52,1)
        _DeepCol ("Deep Water", Color) = (0.24,0.38,0.42,1)
        _InkCol ("Ink", Color) = (0.17,0.13,0.11,1)
        _UnderCol ("Deep Forest Floor", Color) = (0.15,0.14,0.09,1)
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
            sampler2D _River;
            sampler2D _Mask;
            float _Tiling;
            float4 _SunOffset;
            fixed4 _BankCol, _WaterCol, _DeepCol, _InkCol, _UnderCol;

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
                fixed4 d = tex2D(_MainTex, i.wp / _Tiling);
                float n = d.a;
                float r = tex2D(_River, i.uv).r;
                fixed4 fm = tex2D(_Mask, i.uv);
                float m = fm.r;
                float deep = fm.g;
                float mSun = tex2D(_Mask, i.uv + _SunOffset.xy).r;

                // 未砍开的树林下方隐藏河道蓝光，砍开后自然露出小河
                float riverVis = 1.0 - smoothstep(0.45, 0.72, m);

                float bank = smoothstep(0.18, 0.36, r + (n - 0.5) * 0.18) * riverVis;
                float rr = r + (n - 0.5) * 0.08;
                float water = smoothstep(0.50, 0.53, rr) * riverVis;
                float ink = smoothstep(0.47, 0.50, rr) * (1.0 - smoothstep(0.53, 0.56, rr)) * riverVis;

                fixed3 col = lerp(d.rgb, _BankCol.rgb * (0.85 + 0.3 * n), bank * 0.85);

                float flow = sin(i.wp.x * 5.0 - i.wp.y * 2.0 + _Time.y * 1.4 + n * 5.0);
                fixed3 w = lerp(_WaterCol.rgb, _DeepCol.rgb, smoothstep(0.62, 0.95, r));
                w += smoothstep(0.93, 1.0, flow) * 0.10;
                col = lerp(col, w, water);
                col = lerp(col, _InkCol.rgb, ink * 0.85);

                // 1) 林缘两阶赛璐璐地面投影带（树根稳稳踩在这片暖暗土地上）
                float rim = max(m, mSun);
                float cel = smoothstep(0.36, 0.45, rim + (n - 0.5) * 0.08);
                col = lerp(col, col * fixed3(0.76, 0.72, 0.60), cel * (1.0 - water * 0.65));

                // 2) 深林腐殖暗底：仅从第 2 排树后方 (deep > 0.22) 渐变压暗，绝不伸到第 1 排树根前方
                float under = smoothstep(0.22, 0.74, deep + (n - 0.5) * 0.06);
                col = lerp(col, _UnderCol.rgb * (0.88 + 0.24 * n), under);

                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
