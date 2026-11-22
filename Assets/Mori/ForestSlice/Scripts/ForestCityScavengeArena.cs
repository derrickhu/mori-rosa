using System.Collections.Generic;
using UnityEngine;

namespace Mori.ForestSlice
{
    /// <summary>
    /// 丧尸城虚拟摇杆实景开车撞尸搜刮子场景（对标《疯狂水世界》拾荒船摇杆搜刮玩法）：
    /// - 单手虚拟摇杆（或底部【自动】巡航）驾驶改装武装皮卡穿梭破败十字街区
    /// - 磁吸沿街超市推车、汽修轮胎箱、急诊医药箱，撞飞挡路丧尸爆额外废铁零件
    /// - 搜刮物资在皮卡后车斗一层层真实垒高并随转向惯性摇晃！
    /// </summary>
    public sealed class ForestCityScavengeArena
    {
        sealed class StreetLoot
        {
            public Transform tr;
            public SurvRes resA;
            public int amtA;
            public SurvRes resB;
            public int amtB;
            public int cargoWeight;
            public bool magnetizing;
            public float magT;
            public Vector3 startPos;
        }

        sealed class StreetZombie
        {
            public Transform tr;
            public SpriteRenderer sr;
            public Vector2 vel;
            public float phase;
            public bool launched;
            public float launchT;
            public Vector3 launchVel;
        }

        sealed class PuffFx
        {
            public Transform tr;
            public SpriteRenderer sr;
            public float life;
            public Vector3 vel;
        }

        readonly Camera _cam;
        readonly ForestSurvivalSim _sim;
        readonly ForestSurvivalWorld _world;
        readonly System.Random _rng = new System.Random(2026);

        GameObject _arenaRoot;
        Transform _truckRoot;
        SpriteRenderer _truckSr;
        readonly List<Transform> _cargoLayers = new List<Transform>();
        readonly List<StreetLoot> _loots = new List<StreetLoot>();
        readonly List<StreetZombie> _zombies = new List<StreetZombie>();
        readonly List<Vector2> _blockers = new List<Vector2>();
        readonly List<Transform> _barrels = new List<Transform>();
        readonly List<PuffFx> _puffs = new List<PuffFx>();

        static Texture2D _streetTex;

        public bool IsRunning { get; private set; }
        public bool IsSettling { get; private set; }
        public bool AutoPilot { get; set; }
        public CitySiteDef CurrentSite { get; private set; }
        public int CurrentCargo { get; private set; }
        public int MaxCargo { get; private set; }
        public readonly Dictionary<SurvRes, int> SessionGains = new Dictionary<SurvRes, int>();

        Vector2 _truckPos;
        Vector2 _truckVel;
        Vector2 _joyInput;
        float _swayVel;
        float _dustTick;
        float _shakeTimer;
        Vector3 _savedCamPos;
        float _savedOrtho;
        Color _savedBg;

        public event System.Action<SurvRes, int> OnStreetPickup;
        public event System.Action OnCargoFullSettlement;

        const float HalfBound = 10.5f;

        public ForestCityScavengeArena(Camera cam, ForestSurvivalSim sim, ForestSurvivalWorld world)
        {
            _cam = cam;
            _sim = sim;
            _world = world;
        }

        public void SetJoystickInput(Vector2 dir)
        {
            _joyInput = Vector2.ClampMagnitude(dir, 1f);
            if (_joyInput.sqrMagnitude > 0.04f)
                AutoPilot = false;
        }

        public void StartRun(CitySiteDef site)
        {
            if (IsRunning) return;
            CurrentSite = site ?? _sim.CitySites[0];
            IsRunning = true;
            IsSettling = false;
            AutoPilot = false;
            CurrentCargo = 0;
            MaxCargo = _sim.TruckMaxCargo;
            SessionGains.Clear();

            _savedCamPos = _cam.transform.position;
            _savedOrtho = _cam.orthographicSize;
            _savedBg = _cam.backgroundColor;

            // 隐藏深山主营地，展示丧尸城街区舞台
            _world.SliceRootGo.SetActive(false);
            _cam.backgroundColor = new Color32(42, 44, 48, 255);
            _cam.orthographicSize = 4.7f;

            BuildArenaWorld();
            _truckPos = new Vector2(0f, -3.2f);
            _truckVel = Vector2.zero;
            SyncTruckVisual(0.016f);
            _cam.transform.position = new Vector3(_truckPos.x, _truckPos.y + 0.35f, -20f);
        }

        public void FinishAndReturnCamp()
        {
            if (!IsRunning) return;

            // 将本趟搜刮物资入库
            foreach (var kv in SessionGains)
                _sim.Add(kv.Key, kv.Value, notifyToast: true);
            _sim.CityRunsCompleted++;
            _sim.AddXp(35, new Vector2(13.4f, 19.3f));

            ClearArenaObjects();
            if (_arenaRoot != null) _arenaRoot.SetActive(false);

            _world.SliceRootGo.SetActive(true);
            _cam.backgroundColor = _savedBg;
            _cam.orthographicSize = _savedOrtho;
            _cam.transform.position = _savedCamPos;

            IsRunning = false;
            IsSettling = false;
        }

        public void TriggerManualSettlement()
        {
            if (IsRunning && !IsSettling)
            {
                if (SessionGains.Count == 0)
                {
                    RecordGain(CurrentSite.mainRes, 4);
                    RecordGain(SurvRes.Scrap, 3);
                }
                if (CurrentSite != null && CurrentSite.blueprintDrop > 0 && !SessionGains.ContainsKey(SurvRes.Blueprint))
                    RecordGain(SurvRes.Blueprint, Mathf.Max(1, CurrentSite.blueprintDrop / 2));
                IsSettling = true;
                OnCargoFullSettlement?.Invoke();
            }
        }

        void BuildArenaWorld()
        {
            if (_arenaRoot == null)
            {
                _arenaRoot = new GameObject("ZombieCityScavengeArena");
                BuildStreetGround(_arenaRoot.transform);
            }
            _arenaRoot.SetActive(true);
            ClearArenaObjects();

            // 1. 四大街角标志性废墟建筑（超市、汽修厂、药房、防卫仓库）
            PlaceLandmark("city_market", new Vector2(-5.4f, 4.6f), 1.25f);
            PlaceLandmark("city_garage", new Vector2(5.5f, 4.5f), 1.25f);
            PlaceLandmark("city_pharmacy", new Vector2(-5.5f, -5.2f), 1.22f);
            PlaceLandmark("city_depot", new Vector2(5.6f, -5.0f), 1.25f);

            // 外围行道枯树点缀
            Sprite[] lit; Sprite[] deep; Sprite sh;
            if (ForestArt.LoadTreeAtlas(out lit, out deep, out sh) && lit != null && lit.Length > 0)
            {
                Vector2[] treeSpots =
                {
                    new Vector2(-9.2f, 7.5f), new Vector2(-8.8f, 1.8f), new Vector2(-9.0f, -2.5f),
                    new Vector2(9.2f, 7.2f), new Vector2(9.0f, 1.5f), new Vector2(8.8f, -2.8f),
                    new Vector2(-2.8f, 9.2f), new Vector2(3.0f, 9.2f)
                };
                for (int i = 0; i < treeSpots.Length; i++)
                {
                    var tgo = new GameObject("StreetTree");
                    tgo.transform.SetParent(_arenaRoot.transform, false);
                    tgo.transform.localPosition = treeSpots[i];
                    tgo.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
                    var sr = tgo.AddComponent<SpriteRenderer>();
                    sr.sprite = lit[i % lit.Length];
                    sr.sortingOrder = Mathf.RoundToInt(-treeSpots[i].y * 100f);
                    _spawnedStatic.Add(tgo);
                }
            }

            // 2. 路障报废轿车与红油桶
            Vector2[] wreckSpots = { new Vector2(-2.6f, 1.6f), new Vector2(2.8f, -1.5f), new Vector2(0.2f, 5.6f) };
            for (int i = 0; i < wreckSpots.Length; i++)
            {
                var wgo = new GameObject("WreckBlocker");
                wgo.transform.SetParent(_arenaRoot.transform, false);
                wgo.transform.localPosition = wreckSpots[i];
                var sr = wgo.AddComponent<SpriteRenderer>();
                sr.sprite = ForestArt.Get("city_wreck_blocker", ForestPlaceholderArt.LogPile);
                sr.flipX = (i % 2 == 1);
                sr.sortingOrder = Mathf.RoundToInt(-wreckSpots[i].y * 100f);
                _blockers.Add(wreckSpots[i]);
                _spawnedStatic.Add(wgo);
            }

            Vector2[] barrelSpots = { new Vector2(1.8f, 2.2f), new Vector2(-2.1f, -2.2f), new Vector2(4.2f, 0.5f) };
            for (int i = 0; i < barrelSpots.Length; i++)
            {
                var bgo = new GameObject("BoomBarrel");
                bgo.transform.SetParent(_arenaRoot.transform, false);
                bgo.transform.localPosition = barrelSpots[i];
                var sr = bgo.AddComponent<SpriteRenderer>();
                sr.sprite = ForestArt.Get("city_barrel_boom", () => ForestPlaceholderArt.Stump(0));
                sr.sortingOrder = Mathf.RoundToInt(-barrelSpots[i].y * 100f);
                _barrels.Add(bgo.transform);
            }

            // 3. 满街散落的 30 处可搜刮物资堆（购物车食物、轮胎五金箱、急诊零件箱）
            for (int i = 0; i < 30; i++)
            {
                Vector2 p = PickOpenStreetSpot();
                SpawnLootPile(p, i);
            }

            // 4. 16 只街头游荡丧尸（开车直接撞飞！）
            for (int i = 0; i < 16; i++)
            {
                Vector2 p = PickOpenStreetSpot();
                if (Vector2.Distance(p, new Vector2(0f, -3.2f)) < 2.2f) p += new Vector2(3.5f, 3.5f);
                SpawnStreetZombie(p, i);
            }

            // 5. 玩家驾驶的武装皮卡 + 7 层车斗堆高货架
            var truckGo = new GameObject("PlayerPickupTruck");
            truckGo.transform.SetParent(_arenaRoot.transform, false);
            _truckRoot = truckGo.transform;
            _truckSr = truckGo.AddComponent<SpriteRenderer>();
            _truckSr.sprite = ForestArt.Get("truck_active", ForestPlaceholderArt.Tent);
            _truckRoot.localScale = new Vector3(0.88f, 0.88f, 1f);

            var shGo = new GameObject("TruckShadow");
            shGo.transform.SetParent(_truckRoot, false);
            shGo.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            shGo.transform.localScale = new Vector3(1.5f, 0.9f, 1f);
            var shSr = shGo.AddComponent<SpriteRenderer>();
            shSr.sprite = ForestPlaceholderArt.FogBlob();
            shSr.color = new Color(0f, 0f, 0f, 0.45f);
            shSr.sortingOrder = -9500;

            Sprite cargoSp = ForestArt.Get("cargo_stack_unit", () => ForestArt.Get("loot_pile", ForestPlaceholderArt.LogPile));
            _cargoLayers.Clear();
            for (int k = 0; k < 7; k++)
            {
                var cgo = new GameObject("CargoStack_" + k);
                cgo.transform.SetParent(_truckRoot, false);
                cgo.transform.localScale = new Vector3(0.74f - k * 0.02f, 0.74f - k * 0.02f, 1f);
                var csr = cgo.AddComponent<SpriteRenderer>();
                csr.sprite = cargoSp;
                cgo.SetActive(false);
                _cargoLayers.Add(cgo.transform);
            }
        }

        readonly List<GameObject> _spawnedStatic = new List<GameObject>();

        void PlaceLandmark(string key, Vector2 pos, float scale)
        {
            Sprite sp = ForestArt.Get(key, null);
            if (sp == null) return;
            var go = new GameObject("Landmark_" + key);
            go.transform.SetParent(_arenaRoot.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100f);
            _spawnedStatic.Add(go);
            _blockers.Add(pos);
        }

        Vector2 PickOpenStreetSpot()
        {
            for (int attempt = 0; attempt < 16; attempt++)
            {
                // 优先撒在十字主干道与环街路面上
                bool horizontal = _rng.NextDouble() < 0.5;
                float x = horizontal ? ((float)_rng.NextDouble() * 17f - 8.5f) : ((float)_rng.NextDouble() * 5.2f - 2.6f);
                float y = horizontal ? ((float)_rng.NextDouble() * 5.2f - 2.6f) : ((float)_rng.NextDouble() * 16f - 8.0f);
                Vector2 cand = new Vector2(x, y);
                bool ok = true;
                for (int b = 0; b < _blockers.Count; b++)
                    if (Vector2.Distance(cand, _blockers[b]) < 1.55f) { ok = false; break; }
                if (ok) return cand;
            }
            return new Vector2(((float)_rng.NextDouble() - 0.5f) * 8f, ((float)_rng.NextDouble() - 0.5f) * 8f);
        }

        void SpawnLootPile(Vector2 pos, int idx)
        {
            // 按所选街区偏重不同物资外观
            int kind = idx % 3;
            if (CurrentSite.id == "market" && idx % 2 == 0) kind = 0;
            else if (CurrentSite.id == "garage" && idx % 2 == 0) kind = 1;
            else if ((CurrentSite.id == "pharmacy" || CurrentSite.id == "depot") && idx % 2 == 0) kind = 2;

            string key = kind == 0 ? "city_crate_food" : (kind == 1 ? "city_crate_scrap" : "city_crate_med");
            var go = new GameObject("StreetLoot_" + idx);
            go.transform.SetParent(_arenaRoot.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(0.76f, 0.76f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ForestArt.Get(key, () => ForestArt.Get("loot_pile", ForestPlaceholderArt.LogPile));
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100f);

            var loot = new StreetLoot { tr = go.transform, cargoWeight = 6 };
            if (kind == 0)
            {
                loot.resA = SurvRes.Cloth; loot.amtA = 2;
                loot.resB = SurvRes.Water; loot.amtB = 2;
            }
            else if (kind == 1)
            {
                loot.resA = SurvRes.Scrap; loot.amtA = 4;
                loot.resB = idx % 4 == 0 ? SurvRes.Blueprint : SurvRes.Plank; loot.amtB = 1;
            }
            else
            {
                loot.resA = SurvRes.Parts; loot.amtA = 1;
                loot.resB = idx % 3 == 0 ? SurvRes.Blueprint : SurvRes.Herb; loot.amtB = idx % 3 == 0 ? 1 : 2;
            }
            _loots.Add(loot);
        }

        void SpawnStreetZombie(Vector2 pos, int idx)
        {
            var go = new GameObject("CityZombie_" + idx);
            go.transform.SetParent(_arenaRoot.transform, false);
            go.transform.localPosition = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ForestArt.Get(idx % 2 == 0 ? "zombie_0" : "zombie_1", () => ForestPlaceholderArt.Survivor(8));
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100f);
            float ang = (float)_rng.NextDouble() * Mathf.PI * 2f;
            _zombies.Add(new StreetZombie
            {
                tr = go.transform,
                sr = sr,
                vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 0.55f,
                phase = idx * 0.9f,
            });
        }

        static void BuildStreetGround(Transform parent)
        {
            if (_streetTex == null)
            {
                int S = 512;
                _streetTex = new Texture2D(S, S, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                };
                var px = new Color32[S * S];
                Color blockDirt = new Color32(78, 72, 60, 255);
                Color sidewalk = new Color32(126, 122, 114, 255);
                Color asphalt = new Color32(62, 65, 70, 255);
                Color yellowLine = new Color32(226, 178, 58, 235);
                Color zebra = new Color32(215, 214, 206, 200);

                for (int y = 0; y < S; y++)
                {
                    float wy = (y / (float)S - 0.5f) * 24f;
                    for (int x = 0; x < S; x++)
                    {
                        float wx = (x / (float)S - 0.5f) * 24f;
                        // 等轴距 2:1 斜交城市十字街道（与等轴距皮卡、废墟建筑透视严丝合缝！）
                        float u = wx * 0.5f + wy;
                        float v = wy - wx * 0.5f;
                        float n = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.10f - 0.05f;

                        bool inRoadU = Mathf.Abs(u) < 2.6f;
                        bool inRoadV = Mathf.Abs(v) < 2.6f;
                        bool inSidewalk = Mathf.Abs(u) < 3.25f || Mathf.Abs(v) < 3.25f;

                        Color c = blockDirt;
                        if (inSidewalk) c = sidewalk;
                        if (inRoadU || inRoadV)
                        {
                            c = asphalt;
                            if (inRoadU && !inRoadV && Mathf.Abs(u) < 0.11f && Mathf.Abs(Mathf.Sin(v * 2.4f)) > 0.38f)
                                c = yellowLine;
                            if (inRoadV && !inRoadU && Mathf.Abs(v) < 0.11f && Mathf.Abs(Mathf.Sin(u * 2.4f)) > 0.38f)
                                c = yellowLine;
                            if (inRoadU && Mathf.Abs(Mathf.Abs(v) - 2.9f) < 0.32f && Mathf.Abs(Mathf.Sin(u * 6.0f)) > 0.25f)
                                c = zebra;
                            if (inRoadV && Mathf.Abs(Mathf.Abs(u) - 2.9f) < 0.32f && Mathf.Abs(Mathf.Sin(v * 6.0f)) > 0.25f)
                                c = zebra;
                        }

                        c.r = Mathf.Clamp01(c.r + n);
                        c.g = Mathf.Clamp01(c.g + n);
                        c.b = Mathf.Clamp01(c.b + n);
                        px[y * S + x] = c;
                    }
                }
                _streetTex.SetPixels32(px);
                _streetTex.Apply();
            }

            var go = new GameObject("CityAsphaltFloor");
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(_streetTex, new Rect(0, 0, _streetTex.width, _streetTex.height), new Vector2(0.5f, 0.5f), 512f / 24f);
            sr.sortingOrder = -10000;
        }

        // ---------------- 帧推进：皮卡操控、撞飞丧尸、磁吸堆货 ----------------
        public void Tick(float dt)
        {
            if (!IsRunning || IsSettling) return;

            Vector2 desiredDir = _joyInput;

            // 自动驾驶模式：自动导航飞奔向最近的物资堆！
            if (AutoPilot && desiredDir.sqrMagnitude < 0.02f)
            {
                StreetLoot nearest = null;
                float bestD = 999f;
                for (int i = 0; i < _loots.Count; i++)
                {
                    if (_loots[i].magnetizing) continue;
                    float d = Vector2.Distance(_truckPos, _loots[i].tr.localPosition);
                    if (d < bestD) { bestD = d; nearest = _loots[i]; }
                }
                if (nearest != null)
                {
                    Vector2 to = (Vector2)nearest.tr.localPosition - _truckPos;
                    desiredDir = to.normalized;
                }
            }

            float maxSpeed = 4.8f;
            Vector2 prevVel = _truckVel;
            _truckVel = Vector2.Lerp(_truckVel, desiredDir * maxSpeed, dt * 7.5f);
            _truckPos += _truckVel * dt;
            _truckPos.x = Mathf.Clamp(_truckPos.x, -HalfBound, HalfBound);
            _truckPos.y = Mathf.Clamp(_truckPos.y, -HalfBound, HalfBound);

            // 路障轻柔弹开
            for (int i = 0; i < _blockers.Count; i++)
            {
                Vector2 delta = _truckPos - _blockers[i];
                float d = delta.magnitude;
                if (d < 1.25f && d > 0.001f)
                {
                    _truckPos = _blockers[i] + delta / d * 1.25f;
                    _truckVel *= 0.65f;
                }
            }

            float lateralAccel = (_truckVel.x - prevVel.x) / Mathf.Max(0.001f, dt);
            _swayVel = Mathf.Lerp(_swayVel, Mathf.Clamp(-lateralAccel * 0.025f, -0.18f, 0.18f), dt * 8f);

            SyncTruckVisual(dt);

            // 尾气烟尘粒子（严格从车尾排气管飘出，不遮挡车头撞角！）
            if (_truckVel.sqrMagnitude > 0.6f)
            {
                _dustTick -= dt;
                if (_dustTick <= 0f)
                {
                    _dustTick = 0.14f;
                    float tailX = _truckSr.flipX ? -0.62f : 0.62f;
                    SpawnExhaustPuff(_truckPos + new Vector2(tailX, 0.22f), 0.20f);
                }
            }

            // 磁吸拾取沿街物资堆
            for (int i = _loots.Count - 1; i >= 0; i--)
            {
                StreetLoot l = _loots[i];
                if (!l.magnetizing)
                {
                    if (Vector2.Distance(_truckPos, l.tr.localPosition) < 1.25f)
                    {
                        l.magnetizing = true;
                        l.magT = 0f;
                        l.startPos = l.tr.localPosition;
                    }
                }
                else
                {
                    l.magT += dt * 4.8f;
                    Vector3 bedTop = _truckRoot.localPosition + new Vector3(0f, 0.45f + ActiveCargoLayerCount() * 0.22f, 0f);
                    l.tr.localPosition = Vector3.Lerp(l.startPos, bedTop, Mathf.Clamp01(l.magT)) + new Vector3(0f, Mathf.Sin(Mathf.Clamp01(l.magT) * Mathf.PI) * 0.45f, 0f);
                    l.tr.localScale = Vector3.one * Mathf.Lerp(0.76f, 0.32f, l.magT);
                    if (l.magT >= 1f)
                    {
                        CollectStreetLoot(l);
                        KillGo(l.tr.gameObject);
                        _loots.RemoveAt(i);
                        if (IsSettling) return;
                    }
                }
            }

            // 红油桶碰撞爆破（震飞周围丧尸！）
            for (int i = _barrels.Count - 1; i >= 0; i--)
            {
                Transform bt = _barrels[i];
                if (bt == null) continue;
                if (Vector2.Distance(_truckPos, bt.localPosition) < 0.95f)
                {
                    Vector2 bp = bt.localPosition;
                    for (int p = 0; p < 8; p++)
                        SpawnExhaustPuff(bp + Random.insideUnitCircle * 0.6f, 0.7f);
                    _shakeTimer = 0.22f;
                    for (int z = 0; z < _zombies.Count; z++)
                    {
                        if (!_zombies[z].launched && Vector2.Distance(bp, _zombies[z].tr.localPosition) < 2.8f)
                            LaunchZombie(_zombies[z], ((Vector2)_zombies[z].tr.localPosition - bp).normalized);
                    }
                    KillGo(bt.gameObject);
                    _barrels.RemoveAt(i);
                }
            }

            // 撞飞街头丧尸！
            for (int i = _zombies.Count - 1; i >= 0; i--)
            {
                StreetZombie z = _zombies[i];
                if (!z.launched)
                {
                    z.phase += dt;
                    Vector2 p = z.tr.localPosition;
                    p += z.vel * dt;
                    if (Mathf.Abs(p.x) > HalfBound - 0.5f) z.vel.x = -z.vel.x;
                    if (Mathf.Abs(p.y) > HalfBound - 0.5f) z.vel.y = -z.vel.y;
                    z.sr.flipX = z.vel.x > 0f;
                    z.tr.localPosition = new Vector3(p.x, p.y + Mathf.Abs(Mathf.Sin(z.phase * 6f)) * 0.04f, 0f);
                    z.sr.sortingOrder = Mathf.RoundToInt(-p.y * 100f);

                    if (Vector2.Distance(_truckPos, p) < 0.98f)
                    {
                        Vector2 hitDir = (_truckVel.sqrMagnitude > 0.2f ? _truckVel.normalized : (p - _truckPos).normalized);
                        LaunchZombie(z, hitDir);
                    }
                }
                else
                {
                    z.launchT += dt;
                    z.tr.localPosition += z.launchVel * dt;
                    z.launchVel.y -= 9.5f * dt;
                    z.tr.Rotate(0f, 0f, 540f * dt);
                    z.sr.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((z.launchT - 0.35f) / 0.35f));
                    if (z.launchT >= 0.7f)
                    {
                        KillGo(z.tr.gameObject);
                        _zombies.RemoveAt(i);
                    }
                }
            }

            // 尾气消散
            for (int i = _puffs.Count - 1; i >= 0; i--)
            {
                PuffFx pf = _puffs[i];
                pf.life -= dt;
                pf.tr.localPosition += pf.vel * dt;
                pf.tr.localScale += Vector3.one * (dt * 0.5f);
                pf.sr.color = new Color(1f, 0.98f, 0.92f, Mathf.Clamp01(pf.life * 1.4f) * 0.65f);
                if (pf.life <= 0f)
                {
                    KillGo(pf.tr.gameObject);
                    _puffs.RemoveAt(i);
                }
            }

            // 镜头平滑跟随皮卡 + 撞击微震
            Vector3 shake = Vector3.zero;
            if (_shakeTimer > 0f)
            {
                _shakeTimer = Mathf.Max(0f, _shakeTimer - dt);
                shake = (Vector3)(Random.insideUnitCircle * 0.14f);
            }
            Vector3 targetCam = new Vector3(_truckPos.x, _truckPos.y + 0.35f, -20f) + shake;
            _cam.transform.position = Vector3.Lerp(_cam.transform.position, targetCam, dt * 8.5f);
        }

        void LaunchZombie(StreetZombie z, Vector2 dir)
        {
            z.launched = true;
            z.launchT = 0f;
            z.launchVel = new Vector3(dir.x * 5.5f, Mathf.Abs(dir.y) * 2.5f + 4.2f, 0f);
            _shakeTimer = 0.14f;
            RecordGain(SurvRes.Scrap, 2);
            if (_rng.NextDouble() < 0.35) RecordGain(SurvRes.Parts, 1);
            CurrentCargo = Mathf.Min(MaxCargo, CurrentCargo + 3);
            CheckCargoCap();
        }

        void CollectStreetLoot(StreetLoot l)
        {
            RecordGain(l.resA, l.amtA);
            if (l.amtB > 0) RecordGain(l.resB, l.amtB);
            CurrentCargo = Mathf.Min(MaxCargo, CurrentCargo + l.cargoWeight);
            CheckCargoCap();
        }

        void RecordGain(SurvRes r, int amt)
        {
            if (amt <= 0) return;
            SessionGains[r] = (SessionGains.TryGetValue(r, out int cur) ? cur : 0) + amt;
            OnStreetPickup?.Invoke(r, amt);
        }

        void CheckCargoCap()
        {
            if (CurrentCargo >= MaxCargo && !IsSettling)
            {
                if (CurrentSite != null && CurrentSite.blueprintDrop > 0)
                    RecordGain(SurvRes.Blueprint, CurrentSite.blueprintDrop);
                IsSettling = true;
                OnCargoFullSettlement?.Invoke();
            }
        }

        int ActiveCargoLayerCount()
        {
            float ratio = MaxCargo > 0 ? CurrentCargo / (float)MaxCargo : 0f;
            return Mathf.Clamp(Mathf.CeilToInt(ratio * 7f - 0.05f), 0, 7);
        }

        void SyncTruckVisual(float dt)
        {
            if (_truckRoot == null) return;
            float rumble = _truckVel.sqrMagnitude > 0.25f ? Mathf.Sin(Time.time * 32f) * 0.025f : 0f;
            _truckRoot.localPosition = new Vector3(_truckPos.x, _truckPos.y + rumble, 0f);
            if (Mathf.Abs(_truckVel.x) > 0.15f)
                _truckSr.flipX = _truckVel.x > 0f;

            float tilt = Mathf.Clamp(-_truckVel.x * 1.8f, -8f, 8f);
            _truckRoot.localRotation = Quaternion.Euler(0f, 0f, tilt);
            int baseSort = Mathf.RoundToInt(-_truckPos.y * 100f);
            _truckSr.sortingOrder = baseSort;

            // 更新车斗上高高垒起的物资堆层（精准落在皮卡后车斗位置！）
            int layers = ActiveCargoLayerCount();
            float bedOffsetX = _truckSr.flipX ? -0.34f : 0.34f;
            for (int k = 0; k < _cargoLayers.Count; k++)
            {
                Transform ct = _cargoLayers[k];
                bool show = k < layers;
                ct.gameObject.SetActive(show);
                if (!show) continue;
                float sway = _swayVel * (k + 1) * 0.45f;
                ct.localPosition = new Vector3(bedOffsetX + sway, 0.64f + k * 0.20f, 0f);
                ct.localRotation = Quaternion.Euler(0f, 0f, -sway * 38f);
                ct.GetComponent<SpriteRenderer>().sortingOrder = baseSort + 2 + k;
            }
        }

        void SpawnExhaustPuff(Vector2 pos, float scale)
        {
            if (_arenaRoot == null) return;
            var go = new GameObject("Puff");
            go.transform.SetParent(_arenaRoot.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ForestArt.Get("ico_dust", ForestPlaceholderArt.FogBlob);
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100f) - 1;
            _puffs.Add(new PuffFx { tr = go.transform, sr = sr, life = 0.5f, vel = new Vector3(0f, 0.45f, 0f) });
        }

        static void KillGo(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Object.Destroy(go);
            else Object.DestroyImmediate(go);
        }

        void ClearArenaObjects()
        {
            for (int i = 0; i < _loots.Count; i++) if (_loots[i].tr != null) KillGo(_loots[i].tr.gameObject);
            _loots.Clear();
            for (int i = 0; i < _zombies.Count; i++) if (_zombies[i].tr != null) KillGo(_zombies[i].tr.gameObject);
            _zombies.Clear();
            for (int i = 0; i < _barrels.Count; i++) if (_barrels[i] != null) KillGo(_barrels[i].gameObject);
            _barrels.Clear();
            for (int i = 0; i < _puffs.Count; i++) if (_puffs[i].tr != null) KillGo(_puffs[i].tr.gameObject);
            _puffs.Clear();
            for (int i = 0; i < _spawnedStatic.Count; i++) if (_spawnedStatic[i] != null) KillGo(_spawnedStatic[i]);
            _spawnedStatic.Clear();
            _blockers.Clear();
            _cargoLayers.Clear();
            if (_truckRoot != null) KillGo(_truckRoot.gameObject);
            _truckRoot = null;
        }
    }
}
