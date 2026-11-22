using UnityEngine;

namespace Mori.ForestSlice
{
    /// <summary>
    /// 程序生成的占位美术（暖色山林调）。只用来验证引擎搭建，正式素材之后用内置生图替换。
    /// 所有占位图都带深色墨线，和正式风格的“墨线 + 平涂”保持一致的读感。
    /// </summary>
    public static class ForestPlaceholderArt
    {
        public const float PPU = 128f;

        static readonly Color Ink = new Color32(0x2B, 0x20, 0x1A, 0xFF);

        // ---------- 极简光栅器（带 1px 抗锯齿） ----------
        sealed class Canvas
        {
            public readonly int w, h;
            public readonly Color[] px;
            public readonly bool wrap;

            public Canvas(int w, int h, Color clear, bool wrap = false)
            {
                this.w = w; this.h = h; this.wrap = wrap;
                px = new Color[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = clear;
            }

            public void Blend(int x, int y, Color c, float cov)
            {
                if (wrap) { x = ((x % w) + w) % w; y = ((y % h) + h) % h; }
                else if (x < 0 || y < 0 || x >= w || y >= h) return;
                float a = c.a * Mathf.Clamp01(cov);
                if (a <= 0f) return;
                int k = y * w + x;
                Color d = px[k];
                float oa = a + d.a * (1f - a);
                if (oa <= 0f) return;
                Color o = (c * a + d * d.a * (1f - a)) / oa;
                o.a = oa;
                px[k] = o;
            }

            public void Ellipse(float cx, float cy, float rx, float ry, Color c)
            {
                int x0 = Mathf.FloorToInt(cx - rx - 1), x1 = Mathf.CeilToInt(cx + rx + 1);
                int y0 = Mathf.FloorToInt(cy - ry - 1), y1 = Mathf.CeilToInt(cy + ry + 1);
                float r = Mathf.Min(rx, ry);
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                        float d = (Mathf.Sqrt(dx * dx + dy * dy) - 1f) * r;
                        Blend(x, y, c, 0.5f - d);
                    }
            }

            public void InkEllipse(float cx, float cy, float rx, float ry, Color fill, float line = 2.2f)
            {
                Ellipse(cx, cy, rx + line, ry + line, Ink);
                Ellipse(cx, cy, rx, ry, fill);
            }

            public void Poly(Vector2[] p, Color c)
            {
                float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                foreach (var v in p) { minX = Mathf.Min(minX, v.x); maxX = Mathf.Max(maxX, v.x); minY = Mathf.Min(minY, v.y); maxY = Mathf.Max(maxY, v.y); }
                for (int y = Mathf.FloorToInt(minY) - 1; y <= Mathf.CeilToInt(maxY) + 1; y++)
                    for (int x = Mathf.FloorToInt(minX) - 1; x <= Mathf.CeilToInt(maxX) + 1; x++)
                    {
                        Vector2 q = new Vector2(x + 0.5f, y + 0.5f);
                        float d = SignedDist(p, q);
                        Blend(x, y, c, 0.5f - d);
                    }
            }

            public void InkPoly(Vector2[] p, Color fill, float line = 2.2f)
            {
                float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                foreach (var v in p) { minX = Mathf.Min(minX, v.x); maxX = Mathf.Max(maxX, v.x); minY = Mathf.Min(minY, v.y); maxY = Mathf.Max(maxY, v.y); }
                for (int y = Mathf.FloorToInt(minY - line) - 1; y <= Mathf.CeilToInt(maxY + line) + 1; y++)
                    for (int x = Mathf.FloorToInt(minX - line) - 1; x <= Mathf.CeilToInt(maxX + line) + 1; x++)
                    {
                        Vector2 q = new Vector2(x + 0.5f, y + 0.5f);
                        float d = SignedDist(p, q);
                        Blend(x, y, Ink, 0.5f - (d - line));
                        Blend(x, y, fill, 0.5f - d);
                    }
            }

            public void Line(Vector2 a, Vector2 b, float width, Color c)
            {
                float minX = Mathf.Min(a.x, b.x) - width, maxX = Mathf.Max(a.x, b.x) + width;
                float minY = Mathf.Min(a.y, b.y) - width, maxY = Mathf.Max(a.y, b.y) + width;
                for (int y = Mathf.FloorToInt(minY); y <= Mathf.CeilToInt(maxY); y++)
                    for (int x = Mathf.FloorToInt(minX); x <= Mathf.CeilToInt(maxX); x++)
                    {
                        Vector2 q = new Vector2(x + 0.5f, y + 0.5f);
                        float d = SegDist(a, b, q) - width * 0.5f;
                        Blend(x, y, c, 0.5f - d);
                    }
            }

            static float SegDist(Vector2 a, Vector2 b, Vector2 p)
            {
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-5f, ab.sqrMagnitude));
                return Vector2.Distance(p, a + ab * t);
            }

            static float SignedDist(Vector2[] poly, Vector2 p)
            {
                float d = float.MaxValue;
                bool inside = false;
                for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                {
                    Vector2 a = poly[j], b = poly[i];
                    d = Mathf.Min(d, SegDist(a, b, p));
                    if ((b.y > p.y) != (a.y > p.y) && p.x < (a.x - b.x) * (p.y - b.y) / (a.y - b.y) + b.x)
                        inside = !inside;
                }
                return inside ? -d : d;
            }

            public Texture2D ToTexture(string name, bool repeat = false)
            {
                var t = new Texture2D(w, h, TextureFormat.RGBA32, false)
                {
                    name = name,
                    wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                };
                t.SetPixels(px);
                t.Apply();
                return t;
            }

            public Sprite ToSprite(string name, Vector2 pivot)
            {
                Texture2D t = ToTexture(name);
                var sp = Sprite.Create(t, new Rect(0, 0, w, h), pivot, PPU, 0, SpriteMeshType.FullRect);
                sp.name = name;
                return sp;
            }
        }

        static Color C(int r, int g, int b, int a = 255) => new Color32((byte)r, (byte)g, (byte)b, (byte)a);

        // 暖色山林配色
        static readonly Color[] CrownDark = { C(52, 78, 50), C(60, 86, 46), C(72, 70, 38) };
        static readonly Color[] CrownMid = { C(86, 116, 60), C(100, 124, 56), C(150, 112, 48), C(122, 128, 58) };
        static readonly Color[] CrownLight = { C(150, 160, 84), C(172, 164, 82), C(214, 160, 72) };

        // ---------- 林冠平铺图（A 通道 = 树冠高度） ----------
        public static Texture2D CanopyTile(int seed = 11)
        {
            const int S = 256;
            var cv = new Canvas(S, S, C(34, 50, 36, 255), wrap: true);
            var hgt = new float[S * S];
            var rng = new System.Random(seed);
            for (int n = 0; n < 70; n++)
            {
                float cx = (float)rng.NextDouble() * S, cy = (float)rng.NextDouble() * S;
                float r = 16f + (float)rng.NextDouble() * 14f;
                int pal = rng.Next(CrownMid.Length);
                Color mid = CrownMid[pal];
                Color dark = CrownDark[rng.Next(CrownDark.Length)];
                Color light = CrownLight[pal % CrownLight.Length];
                // 墨线圈 → 暗面 → 中间调 → 左上高光
                cv.Ellipse(cx, cy, r + 2f, r * 0.86f + 2f, Ink);
                cv.Ellipse(cx, cy, r, r * 0.86f, dark);
                cv.Ellipse(cx - r * 0.12f, cy + r * 0.1f, r * 0.82f, r * 0.7f, mid);
                cv.Ellipse(cx - r * 0.35f, cy + r * 0.32f, r * 0.34f, r * 0.26f, light);
                for (int y = (int)(cy - r); y <= cy + r; y++)
                    for (int x = (int)(cx - r); x <= cx + r; x++)
                    {
                        float dx = (x - cx) / r, dy = (y - cy) / (r * 0.86f);
                        float d = 1f - (dx * dx + dy * dy);
                        if (d <= 0f) continue;
                        int k = (((y % S) + S) % S) * S + (((x % S) + S) % S);
                        // 后画的树冠在上面，高度直接覆盖
                        hgt[k] = Mathf.Max(hgt[k] * 0.6f, Mathf.Sqrt(d));
                    }
            }
            for (int k = 0; k < cv.px.Length; k++) cv.px[k].a = Mathf.Clamp01(hgt[k]);
            return cv.ToTexture("canopy_tile", true);
        }

        // ---------- 树干带平铺图（林缘露出的一排树干） ----------
        public static Texture2D TrunkTile(int seed = 5)
        {
            const int S = 128;
            var cv = new Canvas(S, S, C(30, 30, 26, 255), wrap: true);
            var rng = new System.Random(seed);
            for (int n = 0; n < 16; n++)
            {
                float x = (float)rng.NextDouble() * S;
                float w = 3f + (float)rng.NextDouble() * 4f;
                Color bark = Color.Lerp(C(84, 62, 44), C(120, 88, 58), (float)rng.NextDouble());
                cv.Line(new Vector2(x, -4), new Vector2(x + 2, S + 4), w + 2.4f, Ink);
                cv.Line(new Vector2(x, -4), new Vector2(x + 2, S + 4), w, bark);
            }
            for (int k = 0; k < cv.px.Length; k++) cv.px[k].a = 0.5f;
            return cv.ToTexture("trunk_tile", true);
        }

        // ---------- 地面平铺图（A 通道 = 噪声） ----------
        public static Texture2D GroundTile(int seed = 3)
        {
            const int S = 256;
            var cv = new Canvas(S, S, C(164, 132, 88), wrap: true);
            var rng = new System.Random(seed);
            // 低频色块
            for (int n = 0; n < 40; n++)
            {
                float cx = (float)rng.NextDouble() * S, cy = (float)rng.NextDouble() * S;
                float r = 20f + (float)rng.NextDouble() * 40f;
                Color c = rng.NextDouble() > 0.5 ? C(150, 118, 76, 90) : C(176, 146, 98, 90);
                cv.Ellipse(cx, cy, r, r * 0.55f, c);
            }
            // 干草茬
            for (int n = 0; n < 140; n++)
            {
                float x = (float)rng.NextDouble() * S, y = (float)rng.NextDouble() * S;
                Color g = rng.NextDouble() > 0.4 ? C(138, 132, 70, 200) : C(110, 120, 62, 200);
                for (int b = 0; b < 3; b++)
                    cv.Line(new Vector2(x + b * 2, y), new Vector2(x + b * 2 + (b - 1) * 2, y + 6), 1.3f, g);
            }
            // 小石子
            for (int n = 0; n < 30; n++)
            {
                float x = (float)rng.NextDouble() * S, y = (float)rng.NextDouble() * S;
                cv.Ellipse(x, y, 2.6f, 1.8f, C(110, 96, 80, 220));
                cv.Ellipse(x - 0.6f, y + 0.5f, 1.4f, 0.9f, C(190, 176, 150, 200));
            }
            var noise = new System.Random(seed + 1);
            for (int k = 0; k < cv.px.Length; k++) cv.px[k].a = 0.35f + 0.3f * (float)noise.NextDouble();
            // 噪声稍微平滑一点
            var t = cv.ToTexture("ground_tile", true);
            return t;
        }

        // ---------- 林缘单棵树（带树干，脚底 pivot） ----------
        public static Sprite EdgeTree(int seed)
        {
            var rng = new System.Random(seed);
            int W = 112, H = 172;
            var cv = new Canvas(W, H, new Color(0, 0, 0, 0));
            float cx = W * 0.5f;
            // 树干
            cv.InkPoly(new[] { new Vector2(cx - 6, 6), new Vector2(cx + 6, 6), new Vector2(cx + 4, 80), new Vector2(cx - 4, 80) }, C(110, 80, 54));
            cv.Ellipse(cx, 8, 18, 5, C(20, 16, 12, 70));
            int pal = rng.Next(CrownMid.Length);
            Color mid = CrownMid[pal], dark = CrownDark[rng.Next(CrownDark.Length)], light = CrownLight[pal % CrownLight.Length];
            bool pine = rng.NextDouble() < 0.4;
            if (pine)
            {
                for (int k = 0; k < 4; k++)
                {
                    float y0 = 52 + k * 26, hw = 44 - k * 9;
                    cv.InkPoly(new[] { new Vector2(cx - hw, y0), new Vector2(cx + hw, y0), new Vector2(cx, y0 + 46) }, dark);
                    cv.Poly(new[] { new Vector2(cx - hw + 6, y0 + 3), new Vector2(cx + 2, y0 + 3), new Vector2(cx - 2, y0 + 38) }, mid);
                }
            }
            else
            {
                Vector3[] blobs = { new Vector3(0, 108, 36), new Vector3(-22, 92, 26), new Vector3(22, 94, 27), new Vector3(-8, 132, 26), new Vector3(14, 124, 24) };
                foreach (var b in blobs) cv.Ellipse(cx + b.x, b.y, b.z + 2.4f, b.z * 0.9f + 2.4f, Ink);
                foreach (var b in blobs) cv.Ellipse(cx + b.x, b.y, b.z, b.z * 0.9f, dark);
                foreach (var b in blobs) cv.Ellipse(cx + b.x - b.z * 0.15f, b.y + b.z * 0.12f, b.z * 0.78f, b.z * 0.66f, mid);
                cv.Ellipse(cx - 14, 128, 12, 9, light);
                cv.Ellipse(cx - 28, 100, 7, 5, light);
            }
            return cv.ToSprite("edge_tree_" + seed, new Vector2(0.5f, 6f / H));
        }

        public static Sprite Stump(int seed)
        {
            var rng = new System.Random(seed);
            var cv = new Canvas(40, 32, new Color(0, 0, 0, 0));
            float r = 9 + (float)rng.NextDouble() * 3;
            cv.Ellipse(20, 9, r + 6, 4, C(20, 16, 12, 60));
            cv.InkPoly(new[] { new Vector2(20 - r, 9), new Vector2(20 + r, 9), new Vector2(20 + r, 17), new Vector2(20 - r, 17) }, C(110, 78, 50));
            cv.InkEllipse(20, 17, r, r * 0.5f, C(206, 170, 112), 1.8f);
            cv.Ellipse(20, 17, r * 0.45f, r * 0.22f, C(170, 128, 80));
            return cv.ToSprite("stump_" + seed, new Vector2(0.5f, 9f / 32f));
        }

        public static Sprite GrassTuft(int seed)
        {
            var rng = new System.Random(seed);
            var cv = new Canvas(28, 22, new Color(0, 0, 0, 0));
            Color g = rng.NextDouble() > 0.5 ? C(112, 128, 62) : C(140, 136, 66);
            for (int b = 0; b < 5; b++)
            {
                float x = 6 + b * 4;
                cv.Line(new Vector2(x, 3), new Vector2(x + (b - 2) * 2.5f, 15 + (float)rng.NextDouble() * 4), 3.6f, Ink);
                cv.Line(new Vector2(x, 3), new Vector2(x + (b - 2) * 2.5f, 15 + (float)rng.NextDouble() * 4), 1.8f, g);
            }
            return cv.ToSprite("tuft_" + seed, new Vector2(0.5f, 3f / 22f));
        }

        // ---------- 模块（自带底座）：按等轴测格子尺寸绘制 ----------
        static Vector2 IsoPx(float gx, float gy, float cellPx, Vector2 origin)
            => origin + new Vector2((gx - gy) * cellPx * 0.5f, (gx + gy) * cellPx * 0.25f);

        /// <summary>农田：w×h 格，带田埂和垄沟，stage 0 空地 / 1 幼苗 / 2 成熟。</summary>
        public static Sprite FarmPlot(int w, int h, int stage)
        {
            float cp = PPU; // 1 格宽 = 1 世界单位 = 128 px
            int W = Mathf.CeilToInt((w + h) * cp * 0.5f) + 12;
            int H = Mathf.CeilToInt((w + h) * cp * 0.25f) + 40;
            var cv = new Canvas(W, H, new Color(0, 0, 0, 0));
            Vector2 o = new Vector2(h * cp * 0.5f + 6, 8);
            Vector2[] rim = { IsoPx(0, 0, cp, o), IsoPx(w, 0, cp, o), IsoPx(w, h, cp, o), IsoPx(0, h, cp, o) };
            cv.InkPoly(rim, C(132, 98, 62));
            Vector2[] soil = { IsoPx(0.12f, 0.12f, cp, o), IsoPx(w - 0.12f, 0.12f, cp, o), IsoPx(w - 0.12f, h - 0.12f, cp, o), IsoPx(0.12f, h - 0.12f, cp, o) };
            cv.Poly(soil, C(98, 66, 42));
            int rows = h * 3;
            for (int r = 1; r < rows; r++)
            {
                float gy = 0.12f + (h - 0.24f) * r / rows;
                cv.Line(IsoPx(0.2f, gy, cp, o), IsoPx(w - 0.2f, gy, cp, o), 3f, C(74, 48, 30));
                if (stage == 0) continue;
                for (float gx = 0.35f; gx < w - 0.2f; gx += 0.3f)
                {
                    Vector2 p = IsoPx(gx, gy, cp, o);
                    if (stage == 1)
                    {
                        cv.Ellipse(p.x, p.y + 3, 3.4f, 2.4f, Ink);
                        cv.Ellipse(p.x, p.y + 3, 2.4f, 1.6f, C(130, 170, 70));
                    }
                    else
                    {
                        cv.InkEllipse(p.x, p.y + 7, 6f, 5f, C(120, 150, 60), 1.6f);
                        cv.Ellipse(p.x - 1.5f, p.y + 9, 2.6f, 2f, C(214, 168, 70));
                    }
                }
            }
            Vector2 ctr = IsoPx(w * 0.5f, h * 0.5f, cp, o);
            return cv.ToSprite($"farm_{w}x{h}_{stage}", new Vector2(ctr.x / W, ctr.y / H));
        }

        public static Sprite Well()
        {
            int W = 140, H = 170;
            var cv = new Canvas(W, H, new Color(0, 0, 0, 0));
            float cx = 70;
            cv.Ellipse(cx, 26, 52, 18, C(20, 16, 12, 60));
            cv.InkPoly(new[] { new Vector2(cx - 40, 30), new Vector2(cx + 40, 30), new Vector2(cx + 40, 56), new Vector2(cx - 40, 56) }, C(140, 132, 120));
            cv.InkEllipse(cx, 30, 40, 14, C(140, 132, 120));
            for (int k = -3; k <= 3; k++) cv.Line(new Vector2(cx + k * 11, 22), new Vector2(cx + k * 11, 50), 1.4f, C(96, 90, 82));
            cv.InkEllipse(cx, 56, 40, 14, C(120, 112, 100));
            cv.Ellipse(cx, 56, 30, 9, C(46, 70, 76));
            // 支架 + 顶棚
            cv.InkPoly(new[] { new Vector2(cx - 36, 56), new Vector2(cx - 30, 56), new Vector2(cx - 30, 128), new Vector2(cx - 36, 128) }, C(116, 82, 52));
            cv.InkPoly(new[] { new Vector2(cx + 30, 56), new Vector2(cx + 36, 56), new Vector2(cx + 36, 128), new Vector2(cx + 30, 128) }, C(116, 82, 52));
            cv.Line(new Vector2(cx - 30, 104), new Vector2(cx + 30, 104), 4f, C(116, 82, 52));
            cv.Line(new Vector2(cx, 104), new Vector2(cx, 70), 1.4f, Ink);
            cv.InkPoly(new[] { new Vector2(cx - 8, 62), new Vector2(cx + 8, 62), new Vector2(cx + 6, 74), new Vector2(cx - 6, 74) }, C(150, 110, 70));
            cv.InkPoly(new[] { new Vector2(cx - 54, 124), new Vector2(cx + 54, 124), new Vector2(cx, 162) }, C(176, 92, 58));
            cv.Poly(new[] { new Vector2(cx - 44, 128), new Vector2(cx - 4, 128), new Vector2(cx - 4, 154) }, C(204, 116, 72));
            return cv.ToSprite("well", new Vector2(0.5f, 26f / H));
        }

        public static Sprite Tent()
        {
            int W = 180, H = 150;
            var cv = new Canvas(W, H, new Color(0, 0, 0, 0));
            cv.Ellipse(90, 26, 82, 22, C(20, 16, 12, 60));
            cv.InkPoly(new[] { new Vector2(14, 30), new Vector2(110, 10), new Vector2(170, 34), new Vector2(98, 128) }, C(150, 136, 92));
            cv.InkPoly(new[] { new Vector2(14, 30), new Vector2(110, 10), new Vector2(98, 128) }, C(186, 168, 112));
            cv.InkPoly(new[] { new Vector2(54, 24), new Vector2(84, 18), new Vector2(84, 72) }, C(56, 44, 34));
            cv.InkPoly(new[] { new Vector2(118, 50), new Vector2(150, 40), new Vector2(140, 70), new Vector2(112, 76) }, C(120, 140, 92));
            return cv.ToSprite("tent", new Vector2(0.5f, 24f / H));
        }

        public static Sprite Campfire()
        {
            int W = 72, H = 72;
            var cv = new Canvas(W, H, new Color(0, 0, 0, 0));
            cv.Ellipse(36, 16, 30, 10, C(20, 16, 12, 60));
            for (int k = 0; k < 7; k++)
            {
                float a = k / 7f * Mathf.PI * 2f;
                cv.InkEllipse(36 + Mathf.Cos(a) * 20, 18 + Mathf.Sin(a) * 8, 6, 4, C(130, 124, 112), 1.5f);
            }
            cv.Line(new Vector2(22, 16), new Vector2(50, 24), 6f, Ink);
            cv.Line(new Vector2(22, 16), new Vector2(50, 24), 4f, C(110, 76, 48));
            cv.InkPoly(new[] { new Vector2(26, 20), new Vector2(46, 20), new Vector2(36, 56) }, C(236, 140, 50));
            cv.Poly(new[] { new Vector2(31, 22), new Vector2(41, 22), new Vector2(36, 42) }, C(255, 214, 110));
            return cv.ToSprite("campfire", new Vector2(0.5f, 16f / H));
        }

        public static Sprite LogPile()
        {
            int W = 110, H = 70;
            var cv = new Canvas(W, H, new Color(0, 0, 0, 0));
            cv.Ellipse(55, 14, 50, 10, C(20, 16, 12, 60));
            float[,] logs = { { 20, 18 }, { 42, 16 }, { 64, 14 }, { 31, 32 }, { 53, 30 }, { 42, 46 } };
            for (int k = 0; k < logs.GetLength(0); k++)
            {
                float x = logs[k, 0], y = logs[k, 1];
                cv.InkPoly(new[] { new Vector2(x, y - 7), new Vector2(x + 40, y + 2), new Vector2(x + 40, y + 16), new Vector2(x, y + 7) }, C(122, 86, 54));
                cv.InkEllipse(x, y, 7, 7, C(212, 172, 112), 1.6f);
                cv.Ellipse(x, y, 3, 3, C(170, 126, 78));
            }
            return cv.ToSprite("logs", new Vector2(0.5f, 12f / H));
        }

        public static Sprite FishingDock()
        {
            int W = 170, H = 130;
            var cv = new Canvas(W, H, new Color(0, 0, 0, 0));
            Vector2[] deck = { new Vector2(20, 40), new Vector2(100, 10), new Vector2(150, 34), new Vector2(70, 64) };
            for (int k = 0; k < 4; k++) cv.InkPoly(new[] { deck[k] + new Vector2(-3, -24), deck[k] + new Vector2(3, -24), deck[k] + new Vector2(3, 0), deck[k] + new Vector2(-3, 0) }, C(92, 66, 44));
            cv.InkPoly(deck, C(176, 136, 86));
            for (int k = 1; k < 6; k++)
            {
                float t = k / 6f;
                cv.Line(Vector2.Lerp(deck[0], deck[1], t), Vector2.Lerp(deck[3], deck[2], t), 1.4f, C(120, 88, 56));
            }
            // 钓鱼的人
            cv.InkEllipse(96, 60, 9, 12, C(170, 70, 52));
            cv.InkEllipse(96, 80, 8, 8, C(230, 190, 150));
            cv.Line(new Vector2(102, 66), new Vector2(150, 110), 2f, Ink);
            cv.Line(new Vector2(150, 110), new Vector2(160, 40), 1f, C(230, 230, 220, 200));
            return cv.ToSprite("fishing_dock", new Vector2(0.45f, 14f / H));
        }

        public static Sprite Survivor(int seed)
        {
            var rng = new System.Random(seed);
            int W = 40, H = 64;
            var cv = new Canvas(W, H, new Color(0, 0, 0, 0));
            Color shirt = new[] { C(170, 70, 52), C(70, 110, 140), C(200, 160, 70), C(110, 130, 80) }[rng.Next(4)];
            cv.Ellipse(20, 6, 12, 4, C(20, 16, 12, 70));
            cv.InkPoly(new[] { new Vector2(13, 6), new Vector2(18, 6), new Vector2(18, 20), new Vector2(13, 20) }, C(70, 64, 60), 1.6f);
            cv.InkPoly(new[] { new Vector2(22, 6), new Vector2(27, 6), new Vector2(27, 20), new Vector2(22, 20) }, C(70, 64, 60), 1.6f);
            cv.InkEllipse(20, 28, 10, 11, shirt, 1.8f);
            cv.InkEllipse(20, 46, 11, 11, C(232, 192, 152), 1.8f);
            cv.Ellipse(20, 52, 11, 6, C(70, 50, 38));
            return cv.ToSprite("survivor_" + seed, new Vector2(0.5f, 6f / H));
        }

        public static Sprite Leaf(int seed)
        {
            var rng = new System.Random(seed);
            var cv = new Canvas(14, 10, new Color(0, 0, 0, 0));
            cv.InkEllipse(7, 5, 4.5f, 2.6f, CrownMid[rng.Next(CrownMid.Length)], 1.2f);
            return cv.ToSprite("leaf_" + seed, new Vector2(0.5f, 0.5f));
        }

        public static Sprite FogBlob()
        {
            const int S = 128;
            var cv = new Canvas(S, S / 2, new Color(1, 1, 1, 0));
            for (int y = 0; y < S / 2; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = (x + 0.5f) / S * 2f - 1f, dy = (y + 0.5f) / (S / 2f) * 2f - 1f;
                    float d = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    cv.px[y * S + x] = new Color(1f, 0.97f, 0.9f, d * d);
                }
            return cv.ToSprite("fog", new Vector2(0.5f, 0.5f));
        }

        public static Sprite Solid(Color c)
        {
            var cv = new Canvas(4, 4, c);
            Texture2D t = cv.ToTexture("solid");
            return Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        public static Sprite Vignette(Color col, float strength)
        {
            const int S = 128;
            var cv = new Canvas(S, S, new Color(0, 0, 0, 0));
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = (x + 0.5f) / S * 2f - 1f, dy = (y + 0.5f) / S * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy * 0.9f);
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.35f, d)) * strength;
                    cv.px[y * S + x] = new Color(col.r, col.g, col.b, a);
                }
            Texture2D t = cv.ToTexture("vignette");
            return Sprite.Create(t, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S / 2f);
        }
    }
}
