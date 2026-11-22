using System.Collections.Generic;
using UnityEngine;

namespace Mori.WaterSlice
{
    /// <summary>
    /// 水上验证样板：连续底面 + 自带底座模块 + 全屏氛围。
    /// 层级（sortingOrder 从低到高）：
    ///   L0 水面底（平铺纹理，缓慢漂移）
    ///   L1 远景沉船/废墟剪影（低对比，视差）
    ///   L2 水面贴花（波纹、漂浮物、鱼影，轻微浮动）
    ///   L3/L4 栈道连接件 + 建筑模块 + L5 角色（按脚底 y 排序）
    ///   L6 雨丝 + 冷色调 + 暗角
    /// 网格只做逻辑定位，画面上不出现任何格子。
    /// </summary>
    public sealed class WaterSliceView : MonoBehaviour
    {
        // 统一像素密度：所有素材按同一 PPU 导入，不做逐个缩放，保证线宽一致
        public const float PPU = 256f;
        // 等轴测 2:1 一格宽（世界单位）
        public const float CellW = 1.375f;
        public const float CellH = 0.6875f;

        static readonly Color WaterBase = new Color32(0x3E, 0x5C, 0x7E, 0xFF);
        static readonly Color RuinTint = new Color32(0x2C, 0x45, 0x63, 0xFF);

        Camera _cam;
        Transform _root;
        SpriteRenderer _water;
        Transform _ruins;
        Vector3 _ruinsBase;
        Vector3 _camBase;
        readonly List<Bobber> _bobbers = new List<Bobber>();
        readonly List<Transform> _rain = new List<Transform>();
        Rect _rainArea;
        float _t;
        System.Random _rng = new System.Random(7);

        sealed class Bobber
        {
            public Transform tr;
            public Vector3 basePos;
            public float phase, amp, rot;
        }

        public static Sprite Art(string key)
        {
            return Resources.Load<Sprite>("WaterArt/" + key);
        }

        public static Vector3 Iso(float gx, float gy)
        {
            return new Vector3((gx - gy) * CellW * 0.5f, (gx + gy) * CellH * 0.5f, 0f);
        }

        public void Build(Camera cam)
        {
            _cam = cam;
            _cam.orthographic = true;
            _cam.orthographicSize = 5.7f;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = WaterBase;
            _cam.transform.rotation = Quaternion.identity;
            _cam.transform.position = new Vector3(0f, 0.6f, -20f);
            _camBase = _cam.transform.position;

            _root = new GameObject("WaterSlice").transform;
            _root.SetParent(transform, false);

            BuildWater();
            BuildRuins();
            BuildDecals();
            BuildSettlement();
            BuildAtmosphere();
        }

        // ---------------- L0 水面 ----------------
        void BuildWater()
        {
            Sprite tile = Art("water_tile");
            var go = new GameObject("L0_Water");
            go.transform.SetParent(_root, false);
            _water = go.AddComponent<SpriteRenderer>();
            _water.sortingOrder = -10000;
            if (tile != null)
            {
                _water.sprite = tile;
                _water.drawMode = SpriteDrawMode.Tiled;
                _water.tileMode = SpriteTileMode.Continuous;
                _water.size = new Vector2(40f, 40f);
            }
            go.transform.position = new Vector3(0f, 2f, 5f);
        }

        // ---------------- L1 远景 ----------------
        void BuildRuins()
        {
            Sprite sp = Art("bg_ruins");
            if (sp == null) return;
            var go = new GameObject("L1_Ruins");
            go.transform.SetParent(_root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = -9000;
            sr.color = Color.white;
            float w = sp.bounds.size.x;
            float s = 6.9f / Mathf.Max(0.01f, w);
            go.transform.localScale = new Vector3(s, s, 1f);
            go.transform.position = new Vector3(0f, 4.9f, 4f);
            _ruins = go.transform;
            _ruinsBase = go.transform.position;
        }

        // ---------------- L2 水面贴花 ----------------
        void BuildDecals()
        {
            var root = new GameObject("L2_Decals").transform;
            root.SetParent(_root, false);
            string[] ripples = { "fx_ripple_a", "fx_ripple_b" };
            string[] floats = { "deco_bottle", "deco_barrel", "deco_buoy", "deco_crate", "deco_can" };
            string[] fish = { "fx_fish_a", "fx_fish_b" };

            for (int i = 0; i < 46; i++)
                Scatter(root, ripples[i % ripples.Length], -8000, 0.75f, 0.03f, false);
            for (int i = 0; i < 12; i++)
                Scatter(root, floats[i % floats.Length], -7000, 1f, 0.05f, true);
            for (int i = 0; i < 5; i++)
                Scatter(root, fish[i % fish.Length], -8500, 0.45f, 0.02f, false);
        }

        void Scatter(Transform root, string key, int order, float alpha, float amp, bool tilt)
        {
            Sprite sp = Art(key);
            if (sp == null) return;
            Vector3 p;
            int guard = 0;
            do
            {
                p = new Vector3(Rand(-3.3f, 3.3f), Rand(-4.9f, 3.9f), 0f);
                guard++;
            } while (InsideSettlement(p) && guard < 30);

            var go = new GameObject(key);
            go.transform.SetParent(root, false);
            go.transform.position = p;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = order;
            sr.color = new Color(1f, 1f, 1f, alpha);
            sr.flipX = _rng.NextDouble() > 0.5;
            _bobbers.Add(new Bobber { tr = go.transform, basePos = p, phase = Rand(0f, 6.28f), amp = amp, rot = tilt ? 6f : 0f });
        }

        static bool InsideSettlement(Vector3 p)
        {
            // 栈道环的大致菱形范围（屏幕坐标）
            Vector3 c = Iso(1.35f, 1.35f);
            float dx = Mathf.Abs(p.x - c.x) / 3.0f;
            float dy = Mathf.Abs(p.y - c.y) / 1.5f;
            if (dx + dy < 1.1f) return true;
            // 下方三条木筏一带
            return p.y < c.y - 0.8f && p.y > c.y - 3.6f && Mathf.Abs(p.x) < 3.2f;
        }

        // ---------------- L3-L5 营地 ----------------
        // 逻辑网格：gx 往右上 ↗，gy 往左上 ↖；一格 = 一节栈道（浮桶 + 木板两小段）
        const float PadSize = 0.42f;

        void BuildSettlement()
        {
            var root = new GameObject("L3_Settlement").transform;
            root.SetParent(_root, false);

            // 栈道环：外圈 gx/gy ∈ {a, b}；四角 + 每边中点放转角平台
            float a = -0.7f, b = 3.4f, m = (a + b) * 0.5f;

            // 中央水泥屋顶岛（含坡道、角色已画在模块里）
            Place(root, "plaza", Iso(m, m), new Vector2(0.05f, 0.12f));

            var pads = new List<Vector2>
            {
                new Vector2(a, a), new Vector2(b, a), new Vector2(a, b), new Vector2(b, b),
                new Vector2(m, a), new Vector2(a, m), new Vector2(b, m), new Vector2(m, b),
            };
            foreach (Vector2 g in pads)
                Place(root, "pad_corner", Iso(g.x, g.y), new Vector2(0f, 0.04f), 5);

            float[] stops = { a, m, b };
            for (int i = 0; i < stops.Length - 1; i++)
            {
                RunX(root, stops[i], stops[i + 1], a);
                RunX(root, stops[i], stops[i + 1], b);
                RunY(root, stops[i], stops[i + 1], a);
                RunY(root, stops[i], stops[i + 1], b);
            }

            // 支线 1：底角往右下 → 晒鱼筏
            RunY(root, a, a - 1.6f, a);
            Place(root, "pad_corner", Iso(a, a - 1.6f), new Vector2(0f, 0.04f), 5);
            Place(root, "raft_drying", Iso(a + 0.05f, a - 2.75f), new Vector2(0f, 0.25f));

            // 支线 2：底角往左下 → 净水筏
            RunX(root, a, a - 1.6f, a);
            Place(root, "pad_corner", Iso(a - 1.6f, a), new Vector2(0f, 0.04f), 5);
            Place(root, "raft_purifier", Iso(a - 2.65f, a), new Vector2(0f, 0.22f));

            // 支线 3：右下边 → 钓鱼筏
            RunY(root, a - 0.1f, a - 0.9f, b - 1.7f);
            Place(root, "raft_fishing", Iso(b - 1.7f, a - 1.55f), new Vector2(0f, 0.15f));
        }

        // 沿 gx 方向铺（固定 gy）
        void RunX(Transform root, float from, float to, float gy)
        {
            float inner = Mathf.Abs(to - from) - PadSize;
            int n = Mathf.Max(1, Mathf.RoundToInt(inner));
            float step = inner / n;
            float dir = Mathf.Sign(to - from);
            for (int i = 0; i < n; i++)
            {
                float g = from + dir * (PadSize * 0.5f + step * (i + 0.5f));
                Walk(root, new Vector2(g, gy), true);
            }
        }

        // 沿 gy 方向铺（固定 gx）
        void RunY(Transform root, float from, float to, float gx)
        {
            float inner = Mathf.Abs(to - from) - PadSize;
            int n = Mathf.Max(1, Mathf.RoundToInt(inner));
            float step = inner / n;
            float dir = Mathf.Sign(to - from);
            for (int i = 0; i < n; i++)
            {
                float g = from + dir * (PadSize * 0.5f + step * (i + 0.5f));
                Walk(root, new Vector2(gx, g), false);
            }
        }

        void Walk(Transform root, Vector2 g, bool alongX)
        {
            // 素材本身画成沿 gy 轴（屏幕 ↘ / ↖）方向；沿 gx 轴时水平翻转复用
            SpriteRenderer sr = Place(root, "walk_straight", Iso(g.x, g.y), new Vector2(0f, 0.03f));
            if (sr != null) sr.flipX = alongX;
        }

        SpriteRenderer Place(Transform root, string key, Vector3 footWorld, Vector2 offset, int orderBias = 0)
        {
            Sprite sp = Art(key);
            if (sp == null) return null;
            var go = new GameObject(key);
            go.transform.SetParent(root, false);
            go.transform.position = footWorld + (Vector3)offset;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            // 脚底越靠下越靠前
            sr.sortingOrder = Mathf.RoundToInt(-footWorld.y * 100f) + orderBias;
            return sr;
        }

        // ---------------- L6 氛围 ----------------
        void BuildAtmosphere()
        {
            Transform camT = _cam.transform;
            float h = _cam.orthographicSize * 2f;
            float w = h * Mathf.Max(0.4f, _cam.aspect);

            // 冷色调整体压一层（不让任何素材颜色“跳出来”）
            var tint = MakeOverlay("L6_Tint", SolidSprite(new Color(0.16f, 0.25f, 0.38f, 1f)), 20000);
            tint.color = new Color(1f, 1f, 1f, 0.07f);
            tint.transform.SetParent(camT, false);
            tint.transform.localPosition = new Vector3(0f, 0f, 10f);
            tint.transform.localScale = new Vector3(w * 1.2f, h * 1.2f, 1f);

            // 暗角
            var vig = MakeOverlay("L6_Vignette", VignetteSprite(), 20001);
            vig.transform.SetParent(camT, false);
            vig.transform.localPosition = new Vector3(0f, 0f, 10f);
            vig.transform.localScale = new Vector3(w * 1.05f / 2f, h * 1.05f / 2f, 1f);

            // 斜雨
            Sprite streak = StreakSprite();
            var rainRoot = new GameObject("L6_Rain").transform;
            rainRoot.SetParent(camT, false);
            rainRoot.localPosition = new Vector3(0f, 0f, 9f);
            _rainArea = new Rect(-w * 0.6f, -h * 0.6f, w * 1.2f, h * 1.2f);
            for (int i = 0; i < 260; i++)
            {
                var go = new GameObject("drop");
                go.transform.SetParent(rainRoot, false);
                go.transform.localPosition = new Vector3(Rand(_rainArea.xMin, _rainArea.xMax), Rand(_rainArea.yMin, _rainArea.yMax), 0f);
                go.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
                float len = Rand(0.18f, 0.42f);
                go.transform.localScale = new Vector3(0.32f, len, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = streak;
                sr.sortingOrder = 19000;
                sr.color = new Color(0.86f, 0.92f, 1f, Rand(0.22f, 0.5f));
                _rain.Add(go.transform);
            }
        }

        SpriteRenderer MakeOverlay(string name, Sprite sp, int order)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = order;
            return sr;
        }

        public void Tick(float dt)
        {
            _t += dt;
            if (_water != null)
            {
                // 水面缓慢漂移（在一个贴图周期内循环）
                Vector2 tile = _water.sprite != null ? (Vector2)_water.sprite.bounds.size : Vector2.one;
                float ox = Mathf.Repeat(_t * 0.05f, tile.x);
                float oy = Mathf.Repeat(_t * 0.025f, tile.y);
                _water.transform.position = new Vector3(ox, 2f + oy, 5f);
            }
            if (_ruins != null)
            {
                Vector3 d = _cam.transform.position - _camBase;
                _ruins.position = _ruinsBase + new Vector3(d.x * 0.6f, d.y * 0.6f, 0f);
            }
            for (int i = 0; i < _bobbers.Count; i++)
            {
                Bobber b = _bobbers[i];
                float s = Mathf.Sin(_t * 1.6f + b.phase);
                b.tr.position = b.basePos + new Vector3(0f, s * b.amp, 0f);
                if (b.rot > 0f) b.tr.rotation = Quaternion.Euler(0f, 0f, s * b.rot);
            }
            Vector3 fall = new Vector3(-Mathf.Sin(18f * Mathf.Deg2Rad), -Mathf.Cos(18f * Mathf.Deg2Rad), 0f) * 11f * dt;
            for (int i = 0; i < _rain.Count; i++)
            {
                Transform r = _rain[i];
                Vector3 p = r.localPosition + fall;
                if (p.y < _rainArea.yMin) p.y += _rainArea.height;
                if (p.x < _rainArea.xMin) p.x += _rainArea.width;
                r.localPosition = p;
            }
        }

        float Rand(float a, float b) => a + (float)_rng.NextDouble() * (b - a);

        // ---------------- 程序生成的小贴图 ----------------
        static Sprite SolidSprite(Color c)
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = c;
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        static Sprite StreakSprite()
        {
            const int W = 4, H = 64;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            for (int y = 0; y < H; y++)
            {
                float a = Mathf.Sin(Mathf.PI * y / (H - 1));
                for (int x = 0; x < W; x++) tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), H);
        }

        static Sprite VignetteSprite()
        {
            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var col = new Color(0.06f, 0.1f, 0.18f, 1f);
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N * 2f - 1f;
                    float dy = (y + 0.5f) / N * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy * 0.9f);
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.35f, d)) * 0.55f;
                    tex.SetPixel(x, y, new Color(col.r, col.g, col.b, a));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N / 2f);
        }
    }
}
