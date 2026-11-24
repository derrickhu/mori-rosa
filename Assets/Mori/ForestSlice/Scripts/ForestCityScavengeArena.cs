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
            public bool isGoldAirdrop;
            public bool magnetizing;
            public float magT;
            public Vector3 startPos;
        }

        sealed class StreetZombie
        {
            public Transform tr;
            public SpriteRenderer sr;
            public bool isBoss;
            public bool isRunner;
            public float hp = 55f;
            public float maxHp = 55f;
            public float hitFlash;
            public float contactCd;
            public Vector2 vel;
            public float phase;
            public bool launched;
            public float launchT;
            public Vector3 launchVel;
        }

        sealed class MountedGunner
        {
            public HeroSpec hero;
            public ForestHeroPuppet puppet;
            public Vector2 bedOffset;
            public float fireTimer;
        }

        sealed class CityProjectile
        {
            public Transform tr;
            public SpriteRenderer sr;
            public Vector3 fromPos;
            public Vector3 toPos;
            public StreetZombie targetZombie;
            public Transform targetBarrel;
            public float damage;
            public bool spin;
            public float t;
            public float dur;
        }

        sealed class PuffFx
        {
            public Transform tr;
            public SpriteRenderer sr;
            public float life;
            public Vector3 vel;
            public Color tint = new Color(1f, 0.98f, 0.92f, 1f);
        }

        readonly Camera _cam;
        readonly ForestSurvivalSim _sim;
        readonly ForestSurvivalWorld _world;
        readonly System.Random _rng = new System.Random(2026);

        GameObject _arenaRoot;
        Transform _truckRoot;
        SpriteRenderer _truckSr;
        readonly List<Transform> _cargoLayers = new List<Transform>();
        readonly List<MountedGunner> _gunners = new List<MountedGunner>();
        readonly List<CityProjectile> _projectiles = new List<CityProjectile>();
        readonly List<StreetLoot> _loots = new List<StreetLoot>();
        readonly List<StreetZombie> _zombies = new List<StreetZombie>();
        readonly List<Vector2> _blockers = new List<Vector2>();
        readonly List<Transform> _barrels = new List<Transform>();
        readonly List<PuffFx> _puffs = new List<PuffFx>();
        StreetZombie _bossZombie;
        StreetLoot _goldAirdropLoot;

        static Texture2D _streetTex;

        public const float NitroMaxCooldown = 8.0f;

        public bool IsRunning { get; private set; }
        public bool IsSettling { get; private set; }
        public bool AutoPilot { get; set; }
        public CitySiteDef CurrentSite { get; private set; }
        public int CurrentCargo { get; private set; }
        public int MaxCargo { get; private set; }
        public float TruckHp { get; private set; }
        public float MaxTruckHp { get; private set; }
        public float NitroCooldown { get; private set; }
        public float NitroActiveTimer { get; private set; }
        public bool IsBossAlive => _bossZombie != null && !_bossZombie.launched && _bossZombie.hp > 0f;
        public float BossHpRatio => IsBossAlive && _bossZombie.maxHp > 0f ? Mathf.Clamp01(_bossZombie.hp / _bossZombie.maxHp) : 0f;
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
        public event System.Action<Vector3, int, bool> OnCityDamageNumber;
        public event System.Action<string> OnCityToast;

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
            MaxTruckHp = _sim.TruckMaxHp;
            TruckHp = MaxTruckHp;
            NitroCooldown = 0f;
            NitroActiveTimer = 0f;
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

        public bool TriggerNitroBoost()
        {
            if (!IsRunning || IsSettling) return false;
            if (NitroCooldown > 0.05f || NitroActiveTimer > 0.05f) return false;
            NitroActiveTimer = 1.85f;
            NitroCooldown = NitroMaxCooldown;
            _shakeTimer = 0.25f;
            OnCityToast?.Invoke("【氮气冲撞】启动！无视碰撞强行碾碎前方尸群！");
            return true;
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

            // 4. 18 只街头丧尸 + 十字路口中央【变异巨锤尸王】守护【黄金空投箱】！
            for (int i = 0; i < 18; i++)
            {
                Vector2 p = PickOpenStreetSpot();
                if (Vector2.Distance(p, new Vector2(0f, -3.2f)) < 2.4f) p += new Vector2(3.5f, 3.5f);
                SpawnStreetZombie(p, i, isBoss: false);
            }
            SpawnGoldAirdropAndBoss();

            // 5. 玩家驾驶的武装皮卡 + 随车 360° 集火小队 + 7 层车斗堆高货架
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

            // 随车出战英雄站在皮卡车顶/车斗护栏上，沿途 360° 自动集火周边尸群与红油桶！
            _sim.EnsureCombatSquad();
            _gunners.Clear();
            Vector2[] gunnerOffsets = { new Vector2(-0.16f, 0.56f), new Vector2(0.24f, 0.52f), new Vector2(0.04f, 0.72f) };
            for (int i = 0; i < _sim.CombatSquadIds.Count && i < 3; i++)
            {
                HeroSpec h = _sim.FindHero(_sim.CombatSquadIds[i]);
                if (h == null) continue;
                var pup = new ForestHeroPuppet(_arenaRoot.transform, PuppetRegistry.GetProfile(h.characterId), "TruckGunner_" + h.id);
                pup.SetSkinTier(h.skinTier);
                pup.Root.localScale = new Vector3(0.56f, 0.56f, 1f);
                pup.SetMode(HeroAnimMode.Idle);
                _gunners.Add(new MountedGunner
                {
                    hero = h,
                    puppet = pup,
                    bedOffset = gunnerOffsets[i],
                    fireTimer = 0.12f + i * 0.16f,
                });
            }
        }

        void SpawnGoldAirdropAndBoss()
        {
            int tierBonus = CurrentSite != null && (CurrentSite.id == "depot" || CurrentSite.id == "pharmacy") ? 1 : 0;
            Vector2 cratePos = new Vector2(0.0f, 1.35f);
            var go = new GameObject("GoldAirdropCrate");
            go.transform.SetParent(_arenaRoot.transform, false);
            go.transform.localPosition = cratePos;
            go.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ForestArt.Get("city_gold_airdrop", () => ForestArt.Get("city_crate_med", ForestPlaceholderArt.LogPile));
            sr.sortingOrder = Mathf.RoundToInt(-cratePos.y * 100f);

            _goldAirdropLoot = new StreetLoot
            {
                tr = go.transform,
                isGoldAirdrop = true,
                cargoWeight = 10,
                resA = SurvRes.HeroMedal,
                amtA = 2 + tierBonus,
                resB = SurvRes.Blueprint,
                amtB = 2 + tierBonus,
            };
            _loots.Add(_goldAirdropLoot);

            _bossZombie = SpawnStreetZombie(new Vector2(0.0f, 0.55f), 99, isBoss: true);
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

        int GetSiteDangerIndex()
        {
            if (CurrentSite == null) return 0;
            if (CurrentSite.id == "depot") return 3;
            if (CurrentSite.id == "pharmacy") return 2;
            if (CurrentSite.id == "garage") return 1;
            return 0;
        }

        StreetZombie SpawnStreetZombie(Vector2 pos, int idx, bool isBoss)
        {
            bool runner = !isBoss && (idx % 4 == 1);
            string spKey = isBoss ? "zombie_boss_brute" : (runner ? "zombie_runner" : (idx % 2 == 0 ? "zombie_0" : "zombie_1"));
            var go = new GameObject(isBoss ? "CityBossBrute" : ("CityZombie_" + idx));
            go.transform.SetParent(_arenaRoot.transform, false);
            go.transform.localPosition = pos;
            float sc = isBoss ? 1.20f : (runner ? 0.90f : 0.96f);
            go.transform.localScale = new Vector3(sc, sc, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ForestArt.Get(spKey, () => ForestArt.Get("zombie_0", () => ForestPlaceholderArt.Survivor(8)));
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100f);
            float ang = (float)_rng.NextDouble() * Mathf.PI * 2f;
            int tier = GetSiteDangerIndex();
            float hp = isBoss ? (280f + tier * 95f) : ((runner ? 38f : 52f) + tier * 16f);
            float spd = isBoss ? 0.48f : (runner ? 0.95f : 0.56f);
            var z = new StreetZombie
            {
                tr = go.transform,
                sr = sr,
                isBoss = isBoss,
                isRunner = runner,
                hp = hp,
                maxHp = hp,
                vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd,
                phase = idx * 0.9f,
            };
            _zombies.Add(z);
            return z;
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

        // ---------------- 帧推进：皮卡操控、随车英雄集火、氮气冲撞、装甲血量、磁吸堆货 ----------------
        public void Tick(float dt)
        {
            if (!IsRunning || IsSettling) return;

            if (NitroCooldown > 0f) NitroCooldown = Mathf.Max(0f, NitroCooldown - dt);
            if (NitroActiveTimer > 0f) NitroActiveTimer = Mathf.Max(0f, NitroActiveTimer - dt);

            Vector2 desiredDir = _joyInput;

            // 自动驾驶模式：优先靠近中央空投/首领区输出并自动搜刮沿街物资箱！
            if (AutoPilot && desiredDir.sqrMagnitude < 0.02f)
            {
                StreetLoot nearest = null;
                float bestD = 999f;
                for (int i = 0; i < _loots.Count; i++)
                {
                    StreetLoot cand = _loots[i];
                    if (cand.magnetizing) continue;
                    if (cand.isGoldAirdrop && IsBossAlive) continue;
                    float d = Vector2.Distance(_truckPos, cand.tr.localPosition);
                    if (cand.isGoldAirdrop) d *= 0.45f; // 尸王击毙后优先抢黄金空投箱！
                    if (d < bestD) { bestD = d; nearest = cand; }
                }
                if (IsBossAlive && Vector2.Distance(_truckPos, _bossZombie.tr.localPosition) < 5.5f)
                {
                    // 环绕尸王保持火力射程扫射！
                    Vector2 fromBoss = _truckPos - (Vector2)_bossZombie.tr.localPosition;
                    Vector2 tangent = new Vector2(-fromBoss.y, fromBoss.x).normalized;
                    desiredDir = (tangent * 0.85f + (fromBoss.magnitude < 2.2f ? fromBoss.normalized * 0.5f : -fromBoss.normalized * 0.35f)).normalized;
                }
                else if (nearest != null)
                {
                    Vector2 to = (Vector2)nearest.tr.localPosition - _truckPos;
                    desiredDir = to.normalized;
                }
            }

            bool nitroOn = NitroActiveTimer > 0f;
            float maxSpeed = nitroOn ? 8.0f : 4.8f;
            Vector2 prevVel = _truckVel;
            _truckVel = Vector2.Lerp(_truckVel, desiredDir * maxSpeed, dt * (nitroOn ? 11f : 7.5f));
            _truckPos += _truckVel * dt;
            _truckPos.x = Mathf.Clamp(_truckPos.x, -HalfBound, HalfBound);
            _truckPos.y = Mathf.Clamp(_truckPos.y, -HalfBound, HalfBound);

            // 路障轻柔弹开（氮气冲刺期间强行滑过不减速）
            for (int i = 0; i < _blockers.Count; i++)
            {
                Vector2 delta = _truckPos - _blockers[i];
                float d = delta.magnitude;
                if (d < 1.25f && d > 0.001f)
                {
                    _truckPos = _blockers[i] + delta / d * 1.25f;
                    if (!nitroOn) _truckVel *= 0.65f;
                }
            }

            float lateralAccel = (_truckVel.x - prevVel.x) / Mathf.Max(0.001f, dt);
            _swayVel = Mathf.Lerp(_swayVel, Mathf.Clamp(-lateralAccel * 0.025f, -0.18f, 0.18f), dt * 8f);

            SyncTruckVisual(dt);
            TickMountedGunners(dt);
            TickCityProjectiles(dt);

            // 尾气烟尘粒子（氮气冲撞时喷射橙金尾焰！）
            if (_truckVel.sqrMagnitude > 0.5f || nitroOn)
            {
                _dustTick -= dt;
                if (_dustTick <= 0f)
                {
                    _dustTick = nitroOn ? 0.06f : 0.14f;
                    float tailX = _truckSr.flipX ? -0.62f : 0.62f;
                    SpawnExhaustPuff(_truckPos + new Vector2(tailX, 0.22f), nitroOn ? 0.34f : 0.20f,
                        nitroOn ? new Color(1f, 0.62f, 0.18f, 1f) : new Color(1f, 0.98f, 0.92f, 1f));
                }
            }

            // 磁吸拾取沿街物资堆（黄金空投箱需先击毙守护尸王或开启氮气强抢！）
            for (int i = _loots.Count - 1; i >= 0; i--)
            {
                StreetLoot l = _loots[i];
                if (!l.magnetizing)
                {
                    if (l.isGoldAirdrop)
                    {
                        float pulse = 0.94f + Mathf.Sin(Time.time * 5.5f) * 0.06f;
                        l.tr.localScale = new Vector3(pulse, pulse, 1f);
                        if (IsBossAlive && !nitroOn) continue;
                    }
                    if (Vector2.Distance(_truckPos, l.tr.localPosition) < 1.32f)
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
                    l.tr.localScale = Vector3.one * Mathf.Lerp(l.isGoldAirdrop ? 0.95f : 0.76f, 0.32f, l.magT);
                    if (l.magT >= 1f)
                    {
                        if (l.isGoldAirdrop)
                            OnCityToast?.Invoke("夺取【黄金空投箱】！获英雄勋章与工程手稿！");
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
                if (bt == null) { _barrels.RemoveAt(i); continue; }
                if (Vector2.Distance(_truckPos, bt.localPosition) < 0.95f)
                {
                    DetonateBarrel(i);
                }
            }

            // 街头丧尸游荡、追扑皮卡与撞击结算！
            for (int i = _zombies.Count - 1; i >= 0; i--)
            {
                StreetZombie z = _zombies[i];
                if (!z.launched)
                {
                    z.phase += dt;
                    if (z.contactCd > 0f) z.contactCd -= dt;
                    if (z.hitFlash > 0f)
                    {
                        z.hitFlash -= dt;
                        z.sr.color = z.hitFlash > 0f ? new Color(1f, 0.45f, 0.40f, 1f) : Color.white;
                    }

                    Vector2 p = z.tr.localPosition;
                    float distToTruck = Vector2.Distance(_truckPos, p);
                    if (z.isBoss)
                    {
                        // 尸王镇守十字路口中央，皮卡靠近时狂暴追击
                        Vector2 anchor = distToTruck < 4.5f ? _truckPos : new Vector2(0f, 0.8f);
                        Vector2 toA = anchor - p;
                        if (toA.sqrMagnitude > 0.08f) z.vel = Vector2.Lerp(z.vel, toA.normalized * 0.75f, dt * 3f);
                    }
                    else if (distToTruck < 3.6f)
                    {
                        float chaseSpd = z.isRunner ? 1.35f : 0.82f;
                        z.vel = Vector2.Lerp(z.vel, (_truckPos - p).normalized * chaseSpd, dt * 3.5f);
                    }

                    p += z.vel * dt;
                    if (Mathf.Abs(p.x) > HalfBound - 0.5f) z.vel.x = -z.vel.x;
                    if (Mathf.Abs(p.y) > HalfBound - 0.5f) z.vel.y = -z.vel.y;
                    z.sr.flipX = z.vel.x > 0f;
                    z.tr.localPosition = new Vector3(p.x, p.y + Mathf.Abs(Mathf.Sin(z.phase * 6f)) * 0.04f, 0f);
                    z.sr.sortingOrder = Mathf.RoundToInt(-p.y * 100f);

                    float hitRadius = z.isBoss ? 1.18f : 0.98f;
                    if (distToTruck < hitRadius)
                    {
                        Vector2 hitDir = (_truckVel.sqrMagnitude > 0.2f ? _truckVel.normalized : (p - _truckPos).normalized);
                        if (nitroOn)
                        {
                            DamageCityZombie(z, 220f, hitDir, isCrit: true);
                        }
                        else if (!z.isBoss && _truckVel.magnitude > 2.1f)
                        {
                            // 高速冲撞普通丧尸：直接撞飞，仅损耗极少量保险杠耐久
                            DamageTruckArmor(4.5f);
                            LaunchZombie(z, hitDir);
                        }
                        else if (z.contactCd <= 0f)
                        {
                            z.contactCd = 0.65f;
                            DamageTruckArmor(z.isBoss ? 24f : 11f);
                            _truckPos += (_truckPos - p).normalized * 0.28f;
                            if (IsSettling) return;
                        }
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
                float a = Mathf.Clamp01(pf.life * 1.4f) * 0.68f;
                pf.sr.color = new Color(pf.tint.r, pf.tint.g, pf.tint.b, a);
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

        void TickMountedGunners(float dt)
        {
            for (int i = 0; i < _gunners.Count; i++)
            {
                MountedGunner g = _gunners[i];
                if (g.puppet == null || g.hero == null) continue;
                g.puppet.Tick(dt);
                g.fireTimer -= dt;
                if (g.fireTimer > 0f) continue;

                // 寻找射程 4.9f 内高优目标：尸王 > 最近红油桶（旁有丧尸） > 最近丧尸
                Transform hitBarrel = null;
                for (int b = 0; b < _barrels.Count; b++)
                {
                    Transform bt = _barrels[b];
                    if (bt == null || Vector2.Distance(_truckPos, bt.localPosition) > 4.4f) continue;
                    for (int z = 0; z < _zombies.Count; z++)
                    {
                        if (!_zombies[z].launched && Vector2.Distance(bt.localPosition, _zombies[z].tr.localPosition) < 2.6f)
                        { hitBarrel = bt; break; }
                    }
                    if (hitBarrel != null) break;
                }

                StreetZombie bestZ = null;
                float bestDist = 4.9f;
                if (hitBarrel == null)
                {
                    for (int z = 0; z < _zombies.Count; z++)
                    {
                        StreetZombie cand = _zombies[z];
                        if (cand.launched) continue;
                        float d = Vector2.Distance(_truckPos, cand.tr.localPosition);
                        if (cand.isBoss && d < 5.2f) { bestZ = cand; break; }
                        if (d < bestDist) { bestDist = d; bestZ = cand; }
                    }
                }

                if (hitBarrel == null && bestZ == null)
                {
                    g.fireTimer = 0.18f;
                    continue;
                }

                g.fireTimer = 0.42f + (i * 0.05f);
                Vector3 fromPos = g.puppet.Root.localPosition + new Vector3(0f, 0.32f, 0f);
                Vector3 toPos = hitBarrel != null
                    ? hitBarrel.localPosition + new Vector3(0f, 0.25f, 0f)
                    : bestZ.tr.localPosition + new Vector3(0f, 0.30f, 0f);

                g.puppet.SetFacing(toPos.x >= fromPos.x);
                g.puppet.SetMode(HeroAnimMode.Chop);
                SpawnCityProjectile(g.hero.projKey, fromPos, toPos, bestZ, hitBarrel, g.hero.Attack * 1.15f, g.hero.id == "ache" || g.hero.id == "xiaotang");
            }
        }

        void SpawnCityProjectile(string spKey, Vector3 fromPos, Vector3 toPos, StreetZombie targetZ, Transform targetBarrel, float dmg, bool spin)
        {
            var go = new GameObject("CityProj_" + spKey);
            go.transform.SetParent(_arenaRoot.transform, false);
            go.transform.localPosition = fromPos;
            go.transform.localScale = new Vector3(0.76f, 0.76f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ForestArt.Get(spKey, () => ForestArt.Get("proj_arrow", ForestPlaceholderArt.LogPile));
            sr.sortingOrder = 9500;
            Vector3 dir = toPos - fromPos;
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, ang);

            _projectiles.Add(new CityProjectile
            {
                tr = go.transform,
                sr = sr,
                fromPos = fromPos,
                toPos = toPos,
                targetZombie = targetZ,
                targetBarrel = targetBarrel,
                damage = dmg,
                spin = spin,
                t = 0f,
                dur = Mathf.Clamp(dir.magnitude / 11f, 0.12f, 0.30f),
            });
        }

        void TickCityProjectiles(float dt)
        {
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                CityProjectile p = _projectiles[i];
                if (p.tr == null) { _projectiles.RemoveAt(i); continue; }
                p.t += dt;
                if (p.targetZombie != null && !p.targetZombie.launched && p.targetZombie.tr != null)
                    p.toPos = p.targetZombie.tr.localPosition + new Vector3(0f, 0.30f, 0f);
                else if (p.targetBarrel != null)
                    p.toPos = p.targetBarrel.localPosition + new Vector3(0f, 0.25f, 0f);

                float u = Mathf.Clamp01(p.t / Mathf.Max(0.05f, p.dur));
                p.tr.localPosition = Vector3.Lerp(p.fromPos, p.toPos, u);
                if (p.spin) p.tr.Rotate(0f, 0f, -720f * dt);

                if (u >= 1f)
                {
                    if (p.targetBarrel != null)
                    {
                        int bIdx = _barrels.IndexOf(p.targetBarrel);
                        if (bIdx >= 0) DetonateBarrel(bIdx);
                    }
                    else if (p.targetZombie != null && !p.targetZombie.launched)
                    {
                        Vector2 dir = ((Vector2)(p.toPos - p.fromPos)).normalized;
                        DamageCityZombie(p.targetZombie, p.damage, dir, isCrit: false);
                    }
                    KillGo(p.tr.gameObject);
                    _projectiles.RemoveAt(i);
                }
            }
        }

        void DetonateBarrel(int idx)
        {
            if (idx < 0 || idx >= _barrels.Count) return;
            Transform bt = _barrels[idx];
            if (bt == null) { _barrels.RemoveAt(idx); return; }
            Vector2 bp = bt.localPosition;
            for (int p = 0; p < 10; p++)
                SpawnExhaustPuff(bp + Random.insideUnitCircle * 0.7f, 0.78f, new Color(1f, 0.52f, 0.16f, 1f));
            _shakeTimer = 0.24f;
            for (int z = 0; z < _zombies.Count; z++)
            {
                if (!_zombies[z].launched && Vector2.Distance(bp, _zombies[z].tr.localPosition) < 3.0f)
                {
                    Vector2 push = ((Vector2)_zombies[z].tr.localPosition - bp).normalized;
                    DamageCityZombie(_zombies[z], 185f, push, isCrit: true);
                }
            }
            KillGo(bt.gameObject);
            _barrels.RemoveAt(idx);
        }

        void DamageCityZombie(StreetZombie z, float dmg, Vector2 hitDir, bool isCrit)
        {
            if (z == null || z.launched) return;
            z.hp -= dmg;
            z.hitFlash = 0.12f;
            if (z.tr != null)
                OnCityDamageNumber?.Invoke(z.tr.position + new Vector3(0f, 0.45f, 0f), Mathf.Max(1, Mathf.RoundToInt(dmg)), isCrit);
            if (z.hp <= 0f)
            {
                LaunchZombie(z, hitDir.sqrMagnitude > 0.01f ? hitDir : Vector2.up);
            }
        }

        void DamageTruckArmor(float dmg)
        {
            if (IsSettling || NitroActiveTimer > 0f) return;
            TruckHp = Mathf.Max(0f, TruckHp - dmg);
            _shakeTimer = Mathf.Max(_shakeTimer, 0.12f);
            if (TruckHp <= 0f && !IsSettling)
            {
                // 紧急突围撤离：保留已装车 50% 战利品，零爆车挫败感！
                var keys = new List<SurvRes>(SessionGains.Keys);
                for (int i = 0; i < keys.Count; i++)
                    SessionGains[keys[i]] = Mathf.Max(1, SessionGains[keys[i]] / 2);
                if (SessionGains.Count == 0)
                    SessionGains[SurvRes.Scrap] = 3;
                OnCityToast?.Invoke("皮卡装甲受损告警！已紧急突围返营（保留半数物资）");
                IsSettling = true;
                OnCargoFullSettlement?.Invoke();
            }
        }

        void LaunchZombie(StreetZombie z, Vector2 dir)
        {
            if (z.launched) return;
            z.launched = true;
            z.launchT = 0f;
            z.launchVel = new Vector3(dir.x * 5.5f, Mathf.Abs(dir.y) * 2.5f + 4.2f, 0f);
            _shakeTimer = 0.14f;
            if (z.isBoss)
            {
                RecordGain(SurvRes.HeroMedal, 2);
                RecordGain(SurvRes.Scrap, 8);
                RecordGain(SurvRes.Parts, 2);
                OnCityToast?.Invoke("击毙【变异巨锤尸王】！中央黄金空投箱已解锁！");
            }
            else
            {
                RecordGain(SurvRes.Scrap, 2);
                if (_rng.NextDouble() < 0.35) RecordGain(SurvRes.Parts, 1);
            }
            CurrentCargo = Mathf.Min(MaxCargo, CurrentCargo + (z.isBoss ? 6 : 1));
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
            _truckSr.color = NitroActiveTimer > 0f ? new Color(1f, 0.92f, 0.72f, 1f) : Color.white;

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

            // 同步随车射手站在皮卡顶棚/护栏上的位置与排序层级
            float dirSign = _truckSr.flipX ? -1f : 1f;
            for (int i = 0; i < _gunners.Count; i++)
            {
                MountedGunner g = _gunners[i];
                if (g.puppet == null) continue;
                Vector3 gp = new Vector3(_truckPos.x + g.bedOffset.x * dirSign, _truckPos.y + g.bedOffset.y + rumble, 0f);
                g.puppet.SyncWorldPos(gp, null, sortingBias: 14 + i * 2);
            }
        }

        void SpawnExhaustPuff(Vector2 pos, float scale, Color tint)
        {
            if (_arenaRoot == null) return;
            var go = new GameObject("Puff");
            go.transform.SetParent(_arenaRoot.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ForestArt.Get("ico_dust", ForestPlaceholderArt.FogBlob);
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100f) - 1;
            _puffs.Add(new PuffFx { tr = go.transform, sr = sr, life = 0.5f, vel = new Vector3(0f, 0.45f, 0f), tint = tint });
        }

        static void KillGo(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Object.Destroy(go);
            else Object.DestroyImmediate(go);
        }

        void ClearArenaObjects()
        {
            for (int i = 0; i < _projectiles.Count; i++) if (_projectiles[i].tr != null) KillGo(_projectiles[i].tr.gameObject);
            _projectiles.Clear();
            for (int i = 0; i < _gunners.Count; i++) if (_gunners[i].puppet != null && _gunners[i].puppet.Root != null) KillGo(_gunners[i].puppet.Root.gameObject);
            _gunners.Clear();
            for (int i = 0; i < _loots.Count; i++) if (_loots[i].tr != null) KillGo(_loots[i].tr.gameObject);
            _loots.Clear();
            for (int i = 0; i < _zombies.Count; i++) if (_zombies[i].tr != null) KillGo(_zombies[i].tr.gameObject);
            _zombies.Clear();
            _bossZombie = null;
            _goldAirdropLoot = null;
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
