using System.Collections.Generic;
using UnityEngine;

namespace Mori.ForestSlice
{
    /// <summary>
    /// 深山开荒样板（全实体 Y-Sort 树木图集版）：
    /// 层级（sortingOrder 从低到高）：
    ///   -10000 连续地面（唯一一种荒土 + 固定河道 + 林缘赛璐璐地面投影带 + 深林林下暗底）
    ///    -9150 树根与建筑的落地接触阴影（紧贴树根中心，杜绝悬空感）
    ///    -9000 地面贴花（树桩、草丛）
    ///    -8900 平铺农田模块
    ///     0 附近 全部森林树木 + 营地建筑 + 幸存者（统一按脚底世界 Y 坐标实时排序，单张 trees_atlas 合批）
    ///    15000+ 雾、暖色调、暗角
    /// </summary>
    public sealed class ForestSliceView : MonoBehaviour
    {
        public const float CellW = 1f;
        public const float CellH = 0.5f;

        public ForestMap Map { get; private set; }
        Camera _cam;
        Transform _root, _shadowRoot, _treeRoot, _decalRoot, _moduleRoot, _fxRoot;
        Material _groundMat;
        readonly System.Random _rng = new System.Random(42);

        sealed class TreeInst
        {
            public int cellX, cellY;
            public float gx, gy;
            public int kind;
            public float baseScale;
            public Transform tr;
            public SpriteRenderer sr;
            public GameObject shadowGo;
        }

        readonly Dictionary<int, List<TreeInst>> _cellTrees = new Dictionary<int, List<TreeInst>>();
        readonly HashSet<int> _decorated = new HashSet<int>();
        readonly HashSet<int> _occupied = new HashSet<int>();
        public readonly List<Vector2> StumpWorldCenters = new List<Vector2>();
        public System.Func<float, float, bool> CanPlaceStumpFilter;
        readonly List<Falling> _falling = new List<Falling>();
        readonly List<Particle> _particles = new List<Particle>();
        readonly List<Transform> _fog = new List<Transform>();

        Sprite[] _litTrees, _deepTrees, _stumpSprites, _tuftSprites, _leafSprites;
        Sprite _treeShadow;
        Transform _tint, _vig;
        float _lastSyncedOrtho = -1f, _lastSyncedAspect = -1f;
        float _t;

        sealed class Falling
        {
            public Transform tr;
            public SpriteRenderer sr;
            public Transform shadowTr;
            public SpriteRenderer shadowSr;
            public float delay;
            public float t;
            public float dir;
        }

        sealed class Particle { public Transform tr; public Vector3 v; public float life; public SpriteRenderer sr; }

        public static Vector3 Iso(float gx, float gy)
            => new Vector3((gx - gy) * CellW * 0.5f, (gx + gy) * CellH * 0.5f, 0f);

        public static Vector2 WorldToGrid(Vector3 w)
        {
            float a = w.x / (CellW * 0.5f), b = w.y / (CellH * 0.5f);
            return new Vector2((a + b) * 0.5f, (b - a) * 0.5f);
        }

        static int Key(int x, int y) => (x + 32) * 1000 + (y + 32);

        public void Build(Camera cam)
        {
            _cam = cam;
            _cam.orthographic = true;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color32(38, 35, 23, 255);
            _cam.transform.rotation = Quaternion.identity;

            Map = new ForestMap();
            _root = new GameObject("ForestSlice").transform;
            _root.SetParent(transform, false);
            _shadowRoot = Child("L1_Shadows");
            _decalRoot = Child("L2_GroundDecals");
            _treeRoot = Child("L3_ForestTrees");
            _moduleRoot = Child("L4_Modules");
            _fxRoot = Child("FX");

            if (!ForestArt.LoadTreeAtlas(out _litTrees, out _deepTrees, out _treeShadow))
            {
                _litTrees = ForestArt.Series("tree_", 8);
                if (_litTrees == null)
                {
                    _litTrees = new Sprite[5];
                    for (int i = 0; i < 5; i++) _litTrees[i] = ForestPlaceholderArt.EdgeTree(100 + i);
                }
                _deepTrees = _litTrees;
            }

            _stumpSprites = ForestArt.Series("stump_", 4) ?? new[] { ForestPlaceholderArt.Stump(1), ForestPlaceholderArt.Stump(2), ForestPlaceholderArt.Stump(3) };
            _tuftSprites = ForestArt.Series("tuft_", 4) ?? new[] { ForestPlaceholderArt.GrassTuft(1), ForestPlaceholderArt.GrassTuft(2), ForestPlaceholderArt.GrassTuft(3) };
            _leafSprites = new[] { ForestPlaceholderArt.Leaf(1), ForestPlaceholderArt.Leaf(2), ForestPlaceholderArt.Leaf(3) };

            BuildGround();
            PopulateForest();
            BuildAtmosphere();
        }

        Transform Child(string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(_root, false);
            return t;
        }

        // ---------------- L0 地面 ----------------
        void BuildGround()
        {
            Shader ground = Resources.Load<Shader>("ForestShaders/ForestGround");
            if (ground == null) ground = Shader.Find("Mori/ForestGround");

            _groundMat = new Material(ground);
            Texture2D groundTex = ForestArt.Tex("ground_tile");
            _groundMat.SetTexture("_MainTex", groundTex != null ? groundTex : ForestPlaceholderArt.GroundTile());
            _groundMat.SetTexture("_River", Map.RiverTex);
            _groundMat.SetTexture("_Mask", Map.MaskTex);
            _groundMat.SetFloat("_Tiling", groundTex != null ? 4f : 3f);
            // 左上方阳光投向右下方的 UV 偏移（约 0.30 世界单位）
            _groundMat.SetVector("_SunOffset", new Vector4(0.18f / ForestMap.Domain, 0.72f / ForestMap.Domain, 0f, 0f));

            float lo = -ForestMap.Margin, hi = ForestMap.N + ForestMap.Margin;
            var mesh = new Mesh { name = "L0_Ground" };
            mesh.vertices = new[] { Iso(lo, lo), Iso(hi, lo), Iso(hi, hi), Iso(lo, hi) };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();

            var go = new GameObject("L0_Ground");
            go.transform.SetParent(_root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _groundMat;
            mr.sortingOrder = -10000;
        }

        // ---------------- L3 全实体森林 ----------------
        void PopulateForest()
        {
            int lo = -14, hi = ForestMap.N + 13;
            for (int x = lo; x <= hi; x++)
            {
                for (int y = lo; y <= hi; y++)
                {
                    Vector3 cellCenter = Iso(x + 0.5f, y + 0.5f);
                    // 覆盖大范围缩放视野矩形域，缩放到最远端也不会露出边缘空白
                    if (cellCenter.x < -16.5f || cellCenter.x > 17.5f || cellCenter.y < -9.5f || cellCenter.y > 25.5f)
                        continue;

                    bool nearPlayable = x >= -3 && x < ForestMap.N + 3 && y >= -3 && y < ForestMap.N + 3;
                    var list = new List<TreeInst>(nearPlayable ? 2 : 1);

                    if (nearPlayable)
                    {
                        // 奇偶棋盘交错排布：任意方向相邻林缘都有树恰好落在离边界 ~0.27 格处，且屏幕横纵双向高密度交错无死角
                        bool even = ((x + y) & 1) == 0;
                        float u0 = 0.27f, v0 = even ? 0.27f : 0.73f;
                        float u1 = 0.73f, v1 = even ? 0.73f : 0.27f;
                        list.Add(CreateTree(x, y, x + u0 + Jit(0.08f), y + v0 + Jit(0.08f), 0.90f + (float)_rng.NextDouble() * 0.16f, true));
                        list.Add(CreateTree(x, y, x + u1 + Jit(0.08f), y + v1 + Jit(0.08f), 0.90f + (float)_rng.NextDouble() * 0.16f, true));
                    }
                    else
                    {
                        // 外圈远景密林：每格 1 棵稍大的树即可严密铺满树海
                        list.Add(CreateTree(x, y, x + 0.5f + Jit(0.18f), y + 0.5f + Jit(0.18f), 1.24f + (float)_rng.NextDouble() * 0.20f, false));
                    }

                    _cellTrees[Key(x, y)] = list;
                }
            }
        }

        TreeInst CreateTree(int cx, int cy, float gx, float gy, float scale, bool withShadow)
        {
            int kind = _rng.Next(_litTrees.Length);
            bool flip = _rng.NextDouble() > 0.5;
            Vector3 p = Iso(gx, gy);

            var go = new GameObject("t");
            go.transform.SetParent(_treeRoot, false);
            go.transform.localPosition = p;
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _deepTrees[kind];
            sr.flipX = flip;
            sr.sortingOrder = Mathf.RoundToInt(-p.y * 100f);

            GameObject shGo = null;
            if (withShadow && _treeShadow != null)
            {
                shGo = new GameObject("ts");
                shGo.transform.SetParent(_shadowRoot, false);
                shGo.transform.localPosition = p;
                shGo.transform.localScale = new Vector3(scale * 0.95f, scale * 0.95f, 1f);
                var shSr = shGo.AddComponent<SpriteRenderer>();
                shSr.sprite = _treeShadow;
                shSr.sortingOrder = -9150;
                shGo.SetActive(false);
            }

            return new TreeInst
            {
                cellX = cx,
                cellY = cy,
                gx = gx,
                gy = gy,
                kind = kind,
                baseScale = scale,
                tr = go.transform,
                sr = sr,
                shadowGo = shGo,
            };
        }

        /// <summary>同步森林状态：移除已开垦格的树，更新林缘第 1 排向阳树干、第 2 排林下阴影树干与树根接触阴影。</summary>
        public void RefreshEdges(bool instant = true)
        {
            var removedKeys = new List<int>();
            foreach (var kv in _cellTrees)
            {
                List<TreeInst> list = kv.Value;
                if (list.Count == 0) continue;
                int cx = list[0].cellX, cy = list[0].cellY;

                if (Map.IsCleared(cx, cy))
                {
                    removedKeys.Add(kv.Key);
                    float cellDelay = Map.GetChopDelay(cx, cy);
                    for (int i = 0; i < list.Count; i++)
                    {
                        TreeInst t = list[i];
                        if (instant)
                        {
                            if (t.shadowGo != null) Kill(t.shadowGo);
                            Kill(t.tr.gameObject);
                        }
                        else
                        {
                            _falling.Add(new Falling
                            {
                                tr = t.tr,
                                sr = t.sr,
                                shadowTr = t.shadowGo != null ? t.shadowGo.transform : null,
                                shadowSr = t.shadowGo != null ? t.shadowGo.GetComponent<SpriteRenderer>() : null,
                                delay = cellDelay + i * 0.06f,
                                t = 0f,
                                dir = _rng.NextDouble() > 0.5 ? 1f : -1f,
                            });
                        }
                    }
                    continue;
                }

                // 判断与已开垦区的距离
                bool dist1 = false, dist2 = false;
                bool clearedBehind = false, clearedInFront = false;
                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        if (!Map.IsCleared(cx + dx, cy + dy)) continue;
                        dist2 = true;
                        if (Mathf.Abs(dx) <= 1 && Mathf.Abs(dy) <= 1)
                        {
                            dist1 = true;
                            if (dx + dy > 0) clearedBehind = true;
                            if (dx + dy < 0) clearedInFront = true;
                        }
                    }
                }

                // 纯前排林缘（开垦区在树的屏幕上方）略微收矮 10%，既完全保留自然树形又多露出营地空地
                float frontFactor = (dist1 && clearedBehind && !clearedInFront) ? 0.88f : 1f;

                for (int i = 0; i < list.Count; i++)
                {
                    TreeInst t = list[i];
                    bool sunlit = dist1 && IsNearClearedPoint(t.gx, t.gy, 0.62f);

                    // 紧挨已开垦河口正中央的林缘树隐藏掉，让小河自然从两岸树林间流出
                    bool inExposedRiverChannel = dist1 && clearedInFront && ForestMap.RiverDistance(t.gx, t.gy) < 0.72f;
                    t.tr.gameObject.SetActive(!inExposedRiverChannel);

                    t.sr.sprite = sunlit ? _litTrees[t.kind] : _deepTrees[t.kind];
                    float sc = t.baseScale * frontFactor;
                    t.tr.localScale = new Vector3(sc, sc, 1f);

                    if (t.shadowGo != null)
                        t.shadowGo.SetActive(!inExposedRiverChannel && (dist1 || dist2));
                }
            }

            for (int i = 0; i < removedKeys.Count; i++)
                _cellTrees.Remove(removedKeys[i]);
        }

        bool IsNearClearedPoint(float gx, float gy, float maxDist)
        {
            int cx = Mathf.FloorToInt(gx), cy = Mathf.FloorToInt(gy);
            float r2 = maxDist * maxDist;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = cx + dx, ny = cy + dy;
                    if (!Map.IsCleared(nx, ny)) continue;
                    // 点 (gx, gy) 到已开垦格 [nx, nx+1] x [ny, ny+1] 的最近距离
                    float px = Mathf.Clamp(gx, nx, nx + 1f);
                    float py = Mathf.Clamp(gy, ny, ny + 1f);
                    float d2 = (gx - px) * (gx - px) + (gy - py) * (gy - py);
                    if (d2 <= r2) return true;
                }
            }
            return false;
        }

        // ---------------- L2 贴花与模块 ----------------
        public void DecorateCleared()
        {
            for (int x = 0; x < ForestMap.N; x++)
            {
                for (int y = 0; y < ForestMap.N; y++)
                {
                    int k = Key(x, y);
                    if (!Map.IsCleared(x, y) || _decorated.Contains(k)) continue;
                    _decorated.Add(k);
                    if (_occupied.Contains(k) || ForestMap.IsNearRiver(x, y)) continue;

                    // 营地核心活动广场与工位走廊保留干净平整泥地，树桩仅少量点缀最外圈边缘
                    bool inCorePlaza = x >= 12 && x <= 20 && y >= 12 && y <= 20;
                    if (!inCorePlaza && _rng.NextDouble() < 0.10)
                    {
                        float sx = x + 0.5f + Jit(0.24f);
                        float sy = y + 0.5f + Jit(0.24f);
                        Vector2 sw = Iso(sx, sy);
                        bool tooClose = false;
                        for (int i = 0; i < StumpWorldCenters.Count; i++)
                        {
                            if (Vector2.Distance(StumpWorldCenters[i], sw) < 1.05f) { tooClose = true; break; }
                        }
                        if (!tooClose && (CanPlaceStumpFilter == null || CanPlaceStumpFilter(sx, sy)))
                        {
                            PlaceDecal(_stumpSprites[_rng.NextDouble() < 0.8 ? 0 : _rng.Next(_stumpSprites.Length)], sx, sy);
                            StumpWorldCenters.Add(sw);
                        }
                    }

                    if (_rng.NextDouble() < 0.42)
                    {
                        float tx = x + 0.5f + Jit(0.32f);
                        float ty = y + 0.5f + Jit(0.32f);
                        if (CanPlaceStumpFilter == null || CanPlaceStumpFilter(tx, ty))
                            PlaceDecal(_tuftSprites[_rng.Next(_tuftSprites.Length)], tx, ty);
                    }
                }
            }
        }

        float Jit(float a) => ((float)_rng.NextDouble() * 2f - 1f) * a;

        void PlaceDecal(Sprite sp, float gx, float gy)
        {
            var sr = PlaceSprite(_decalRoot, sp, gx, gy, 1f, _rng.NextDouble() > 0.5);
            sr.sortingOrder = -9000 + Mathf.RoundToInt(-Iso(gx, gy).y * 10f);
        }

        SpriteRenderer PlaceSprite(Transform parent, Sprite sp, float gx, float gy, float scale = 1f, bool flip = false)
        {
            var go = new GameObject(sp.name);
            go.transform.SetParent(parent, false);
            Vector3 p = Iso(gx, gy);
            go.transform.localPosition = p;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.flipX = flip;
            sr.sortingOrder = Mathf.RoundToInt(-p.y * 100f);
            return sr;
        }

        public Transform ModuleRoot => _moduleRoot;
        public Transform ShadowRoot => _shadowRoot;

        public void OccupyCells(int gx, int gy, int w, int h)
        {
            for (int x = gx; x < gx + w; x++)
                for (int y = gy; y < gy + h; y++)
                    _occupied.Add(Key(x, y));
        }

        public GameObject CreateShadow(float gx, float gy, float scaleX, float scaleY)
        {
            if (_treeShadow == null) return null;
            var sh = new GameObject("prop_shadow");
            sh.transform.SetParent(_shadowRoot, false);
            sh.transform.localPosition = Iso(gx, gy);
            sh.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            var sr = sh.AddComponent<SpriteRenderer>();
            sr.sprite = _treeShadow;
            sr.sortingOrder = -9150;
            return sh;
        }

        void AddContactShadow(float gx, float gy, float scaleX, float scaleY)
        {
            CreateShadow(gx, gy, scaleX, scaleY);
        }

        public SpriteRenderer PlaceModule(Sprite sp, int gx, int gy, int w, int h, float footGx, float footGy)
        {
            for (int x = gx; x < gx + w; x++)
                for (int y = gy; y < gy + h; y++)
                    _occupied.Add(Key(x, y));
            AddContactShadow(footGx, footGy, 0.9f + w * 0.35f, 0.85f + h * 0.25f);
            return PlaceSprite(_moduleRoot, sp, footGx, footGy);
        }

        public SpriteRenderer PlaceFlat(Sprite sp, int gx, int gy, int w, int h)
        {
            for (int x = gx; x < gx + w; x++)
                for (int y = gy; y < gy + h; y++)
                    _occupied.Add(Key(x, y));
            var sr = PlaceSprite(_moduleRoot, sp, gx + w * 0.5f, gy + h * 0.5f);
            sr.sortingOrder = -8900;
            return sr;
        }

        public void PlaceProp(Sprite sp, float gx, float gy)
        {
            _occupied.Add(Key(Mathf.FloorToInt(gx), Mathf.FloorToInt(gy)));
            AddContactShadow(gx, gy, 0.55f, 0.55f);
            PlaceSprite(_moduleRoot, sp, gx, gy);
        }

        // ---------------- 砍树交互 ----------------
        public bool TryFindChopChunkAtScreen(Vector2 screen, out int outCx, out int outCy)
        {
            Vector3 w = _cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            Vector2 rawG = WorldToGrid(w);
            // 点击营地内部平整空地时绝不向下探入前排树林误触砍树！
            if (Map.IsCleared(Mathf.FloorToInt(rawG.x), Mathf.FloorToInt(rawG.y)) &&
                Map.IsCleared(Mathf.FloorToInt(rawG.x - 0.55f), Mathf.FloorToInt(rawG.y - 0.55f)))
            {
                outCx = outCy = -1;
                return false;
            }

            float[] lifts = { 0f, 0.36f };
            for (int i = 0; i < lifts.Length; i++)
            {
                Vector2 g = WorldToGrid(w - new Vector3(0f, lifts[i], 0f));
                if (Map.IsCleared(Mathf.FloorToInt(g.x), Mathf.FloorToInt(g.y))) continue;
                int cx = Mathf.FloorToInt(g.x / ForestMap.ChunkSize);
                int cy = Mathf.FloorToInt(g.y / ForestMap.ChunkSize);
                if (CanChopChunk(cx, cy))
                {
                    outCx = cx;
                    outCy = cy;
                    return true;
                }
            }
            outCx = outCy = -1;
            return false;
        }

        public bool CanChopChunk(int cx, int cy)
        {
            int x0 = cx * ForestMap.ChunkSize, y0 = cy * ForestMap.ChunkSize;
            bool anyForest = false, touches = false;
            for (int x = x0; x < x0 + ForestMap.ChunkSize; x++)
            {
                for (int y = y0; y < y0 + ForestMap.ChunkSize; y++)
                {
                    if (!Map.InBounds(x, y)) continue;
                    if (Map.target[x, y] > 0.5f) anyForest = true;
                    if (Map.IsCleared(x - 1, y) || Map.IsCleared(x + 1, y) || Map.IsCleared(x, y - 1) || Map.IsCleared(x, y + 1))
                        touches = true;
                }
            }
            return anyForest && touches;
        }

        public bool ExecuteChopChunk(int cx, int cy)
        {
            if (!Map.TryClearChunk(cx, cy, _rng)) return false;
            RefreshEdges(instant: false);
            DecorateCleared();
            Vector3 c = Iso((cx + 0.5f) * ForestMap.ChunkSize, (cy + 0.5f) * ForestMap.ChunkSize) + new Vector3(0, 0.55f, 0);
            BurstLeaves(c, 32);
            return true;
        }

        public void BurstChips(Vector3 worldPos, int count = 10)
        {
            BurstLeaves(worldPos, count);
        }

        public bool ChopAtScreen(Vector2 screen)
        {
            if (TryFindChopChunkAtScreen(screen, out int cx, out int cy))
                return ExecuteChopChunk(cx, cy);
            return false;
        }

        void BurstLeaves(Vector3 at, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("leaf");
                go.transform.SetParent(_fxRoot, false);
                go.transform.position = at + new Vector3(Jit(0.9f), Jit(0.4f), 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _leafSprites[_rng.Next(_leafSprites.Length)];
                sr.sortingOrder = 14000;
                _particles.Add(new Particle { tr = go.transform, sr = sr, v = new Vector3(Jit(1.2f), 0.6f + (float)_rng.NextDouble() * 1.2f, 0f), life = 1.4f + (float)_rng.NextDouble() * 0.6f });
            }
        }

        // ---------------- L6 氛围 ----------------
        void BuildAtmosphere()
        {
            Transform camT = _cam.transform;
            Sprite fog = ForestPlaceholderArt.FogBlob();
            for (int i = 0; i < 9; i++)
            {
                var go = new GameObject("fog");
                go.transform.SetParent(_fxRoot, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = fog;
                sr.sortingOrder = 15000;
                sr.color = new Color(1f, 1f, 1f, 0.08f + (float)_rng.NextDouble() * 0.06f);
                float s = 5f + (float)_rng.NextDouble() * 4f;
                go.transform.localScale = new Vector3(s, s, 1f);
                _fog.Add(go.transform);
            }

            var tint = new GameObject("L6_WarmTint").AddComponent<SpriteRenderer>();
            tint.sprite = ForestPlaceholderArt.Solid(new Color(1f, 0.78f, 0.5f, 1f));
            tint.color = new Color(1f, 1f, 1f, 0.05f);
            tint.sortingOrder = 20000;
            tint.transform.SetParent(camT, false);
            tint.transform.localPosition = new Vector3(0f, 0f, 10f);
            _tint = tint.transform;

            var vig = new GameObject("L6_Vignette").AddComponent<SpriteRenderer>();
            vig.sprite = ForestPlaceholderArt.Vignette(new Color(0.08f, 0.07f, 0.04f), 0.26f);
            vig.sortingOrder = 20001;
            vig.transform.SetParent(camT, false);
            vig.transform.localPosition = new Vector3(0f, 0f, 10f);
            _vig = vig.transform;
            SyncCameraOverlays(force: true);
            LayoutFog();
        }

        public void SyncCameraOverlays(bool force = false)
        {
            if (_cam == null) return;
            float ortho = _cam.orthographicSize;
            float aspect = Mathf.Max(0.4f, _cam.aspect);
            if (!force && Mathf.Abs(ortho - _lastSyncedOrtho) < 0.001f && Mathf.Abs(aspect - _lastSyncedAspect) < 0.001f)
                return;

            _lastSyncedOrtho = ortho;
            _lastSyncedAspect = aspect;
            float h = ortho * 2f;
            float w = h * aspect;
            // 随镜头缩放实时同步暖色调遮罩与柔和暗角尺寸，杜绝缩小画面时屏幕中央残留固定黑框！
            if (_tint != null) _tint.localScale = new Vector3(w * 1.35f, h * 1.35f, 1f);
            if (_vig != null) _vig.localScale = new Vector3(w * 1.15f / 2f, h * 1.15f / 2f, 1f);
        }

        void LateUpdate()
        {
            SyncCameraOverlays();
        }

        void LayoutFog()
        {
            for (int i = 0; i < _fog.Count; i++)
            {
                Vector3 c = _cam.transform.position;
                float h = _cam.orthographicSize;
                _fog[i].position = new Vector3(c.x + Jit(h * 0.6f), c.y + Jit(h * 0.9f), 0f);
            }
        }

        public void FrameCamera(Vector2 centerGrid, float orthoSize)
        {
            _cam.orthographicSize = orthoSize;
            Vector3 c = Iso(centerGrid.x, centerGrid.y);
            _cam.transform.position = new Vector3(c.x, c.y, -20f);
            SyncCameraOverlays(force: true);
            LayoutFog();
        }

        /// <summary>当主角走到南侧/前排树冠后方时，自动将遮挡主角的 1~2 棵林缘树半透明化，既能贴树根行走又不挡视线。</summary>
        public void UpdateHeroCanopyFade(Vector2 heroWorld, float dt)
        {
            float k = Mathf.Clamp01(dt * 10f);
            foreach (var kv in _cellTrees)
            {
                List<TreeInst> list = kv.Value;
                if (list.Count == 0) continue;
                Vector3 c0 = list[0].tr != null ? list[0].tr.localPosition : Vector3.zero;
                if (Mathf.Abs(c0.x - heroWorld.x) > 1.8f || Mathf.Abs(c0.y - heroWorld.y) > 2.4f)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        SpriteRenderer sr = list[i].sr;
                        if (sr != null && sr.color.a < 0.995f)
                        {
                            Color col = sr.color;
                            col.a = Mathf.MoveTowards(col.a, 1f, k);
                            sr.color = col;
                        }
                    }
                    continue;
                }

                for (int i = 0; i < list.Count; i++)
                {
                    TreeInst t = list[i];
                    if (t.tr == null || t.sr == null || !t.tr.gameObject.activeSelf) continue;
                    Vector3 tp = t.tr.localPosition;
                    bool occludingHero = tp.y < heroWorld.y - 0.02f &&
                                         (heroWorld.y - tp.y) < 1.52f &&
                                         Mathf.Abs(tp.x - heroWorld.x) < 0.58f;
                    float targetA = occludingHero ? 0.45f : 1f;
                    Color c = t.sr.color;
                    if (Mathf.Abs(c.a - targetA) > 0.004f)
                    {
                        c.a = Mathf.MoveTowards(c.a, targetA, k);
                        t.sr.color = c;
                    }
                }
            }
        }

        public void Tick(float dt)
        {
            _t += dt;
            SyncCameraOverlays();
            Map.Tick(dt);

            for (int i = _falling.Count - 1; i >= 0; i--)
            {
                Falling f = _falling[i];
                if (f.delay > 0f)
                {
                    f.delay -= dt;
                    continue;
                }
                f.t += dt;
                float k = Mathf.Clamp01(f.t / 0.48f);
                f.tr.localRotation = Quaternion.Euler(0f, 0f, -f.dir * 85f * k * k);
                float alpha = 1f - Mathf.Clamp01((f.t - 0.42f) / 0.25f);
                f.sr.color = new Color(1f, 1f, 1f, alpha);
                if (f.shadowSr != null)
                    f.shadowSr.color = new Color(1f, 1f, 1f, 1f - k);
                if (f.t > 0.68f)
                {
                    if (f.shadowTr != null) Kill(f.shadowTr.gameObject);
                    Kill(f.tr.gameObject);
                    _falling.RemoveAt(i);
                }
            }

            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                Particle p = _particles[i];
                p.life -= dt;
                p.v += new Vector3(Mathf.Sin(_t * 3f + i) * 0.6f, -1.6f, 0f) * dt;
                p.tr.position += p.v * dt;
                p.tr.Rotate(0f, 0f, 120f * dt);
                p.sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(p.life));
                if (p.life <= 0f)
                {
                    Kill(p.tr.gameObject);
                    _particles.RemoveAt(i);
                }
            }

            for (int i = 0; i < _fog.Count; i++)
                _fog[i].position += new Vector3(0.06f * dt * (1f + i % 3 * 0.3f), 0f, 0f);
        }

        static void Kill(GameObject go)
        {
            if (Application.isPlaying) Object.Destroy(go);
            else Object.DestroyImmediate(go);
        }
    }
}
