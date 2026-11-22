using System.Collections.Generic;
using UnityEngine;

namespace Mori.ForestSlice
{
    /// <summary>
    /// 深山末日主世界表现与真实生态采集控制器：
    /// 1. 四类地面真实采集点（散落原木堆 / 野生浆果蘑菇丛[产鲜蔬] / 山道锈铁残骸[产废铁零件] / 林缘再生松木[可持续原木]）
    /// 2. 有限山谷拓荒边界（7x7共49块）+ 砍开树林显露被掩埋的锈铁残骸与浆果丛（砍树本身只产原木，绝不凭空掉废铁！）
    /// 3. 冷色调高辨识度现代逃难小队（陆沉、唐果、苏凛、陈伯、雷猛）跑腿搬运与头顶气泡
    /// 4. 狩猎野鹿野猪专产【野味鲜肉】、溪流钓鱼专产【山溪鲜鱼】
    /// </summary>
    public sealed class ForestSurvivalWorld
    {
        public enum FieldKind
        {
            LogPile = 0,       // 散落原木堆 -> 原木
            BerryBush = 1,     // 野生浆果蘑菇丛 -> 山果鲜蔬
            ScrapWreck = 2,    // 山道锈铁残骸 -> 废铁 (+小概率零件)
            RegrownTimber = 3, // 林缘再生松木 -> 可持续原木
        }

        public sealed class NodeVisual
        {
            public CampNode node;
            public GameObject go;
            public SpriteRenderer sr;
            public GameObject shadowGo;
            public Vector3 baseScale = Vector3.one;
            public float bounceTimer;
            public readonly List<GameObject> extras = new List<GameObject>();
            public readonly List<Transform> dustPuffs = new List<Transform>();
        }

        public sealed class WildActor
        {
            public bool isZombie;
            public bool isHordeRaid;
            public string label;
            public float gx, gy;
            public float targetGx, targetGy;
            public float timer;
            public float phase;
            public Transform tr;
            public SpriteRenderer sr;
            public Transform shadowTr;
        }

        public sealed class GroundPile
        {
            public FieldKind kind;
            public float gx, gy;
            public int amount;
            public bool queued;
            public Rect visualRect;
            public GameObject go;
            public GameObject shadowGo;
        }

        public struct ObstacleEllipse
        {
            public Vector2 center;
            public float rx;
            public float ry;
        }

        public sealed class ChopTask
        {
            public bool isPile;
            public GroundPile pile;
            public int chunkX, chunkY;
            public float gx, gy;
            public int claimedByWalker = -1;
            public float chopProgress;
            public float fallbackTimer;
        }

        public sealed class Walker
        {
            public int index;
            public string characterId;
            public float gx, gy;
            public float targetGx, targetGy;
            public Vector2 patrolA, patrolB;
            public bool headingToB;
            public bool hasManualMove;
            public float pauseTimer;
            public float cheerTimer;
            public float phase;
            public Transform tr;
            public ForestHeroPuppet puppet;
            public Transform shadowTr;
            public ChopTask assignedTask;
            public float carryHomeTimer;
            public float stuckTimer;
            public string chatterText;
            public float chatterTimer;
        }

        public readonly ForestSliceView View;
        public readonly ForestSurvivalSim Sim;
        public readonly Dictionary<string, NodeVisual> Visuals = new Dictionary<string, NodeVisual>();
        public readonly List<WildActor> WildActors = new List<WildActor>();
        public readonly List<GroundPile> GroundPiles = new List<GroundPile>();
        public readonly List<ChopTask> ChopQueue = new List<ChopTask>();
        public readonly List<Walker> Walkers = new List<Walker>();

        readonly System.Random _rng = new System.Random(99);
        Transform _actorRoot;
        float _preyCooldown = 6f;
        float _zombieCooldown = 22f;
        float _chatterCooldown = 3.5f;
        float _berryRegrowTimer = 14f;
        float _timberRegrowTimer = 16f;
        int _chopsCount;
        bool _hordeWaveRunning;
        int _hordeRemaining;

        // 林缘砍树二次确认防误触状态
        public bool HasPendingChop;
        public int PendingChopCx, PendingChopCy;
        public float PendingChopTimer;

        // 推镜实景钓鱼演员
        GameObject _fishRoot;
        Transform _anglerTr;
        Transform _bobberTr;
        Transform _fishShadowTr;
        SpriteRenderer _fishShadowSr;
        Transform _leapingFishTr;
        float _leapTimer;
        Vector3 _leapFrom, _leapTo;
        bool _fishingActive;
        public bool FishBobberInWater;
        public bool FishBitingNow;
        float _fishAnimTime;

        public GameObject SliceRootGo => View.transform.GetChild(0).gameObject;

        public event System.Action<CampNode> OnNodeClicked;
        public event System.Action OnResidentDemandClicked;
        public event System.Action OnEmptyGroundClicked;
        public event System.Action<string, Vector2> OnCustomToast;

        static readonly string[] AmbientChatters =
        {
            "呼……深山空气总比丧尸城强多了！",
            "这把胶带绑的自制铁斧还挺顺手！",
            "林边又长出野生浆果丛和再生木了！",
            "山道边的旧护栏能拆出不少废铁！",
            "早晚把这破卫衣换成防刮皮夹克！"
        };

        public ForestSurvivalWorld(ForestSliceView view, ForestSurvivalSim sim)
        {
            View = view;
            Sim = sim;
            _actorRoot = new GameObject("L4_DynamicActors").transform;
            _actorRoot.SetParent(view.transform.GetChild(0), false);

            for (int i = 0; i < Sim.Nodes.Count; i++)
            {
                CampNode n = Sim.Nodes[i];
                View.OccupyCells(n.occupyX, n.occupyY, n.occupyW, n.occupyH);
            }

            View.CanPlaceStumpFilter = (sx, sy) => !OverlapsAnyBuildingOrPile(ForestSliceView.Iso(sx, sy), 0.30f, 0.28f);

            Sim.OnNodeVisualChanged += RefreshNodeVisual;
            Sim.OnNodeHammeredComplete += OnNodeHammered;
            Sim.OnHeroSkinChanged += OnHeroSkinChanged;
            Sim.OnHeroUnlocked += _ => SyncSurvivorWalkers();
        }

        void OnHeroSkinChanged(HeroSpec h, OutfitTier tier)
        {
            if (h == null) return;
            for (int i = 0; i < Walkers.Count; i++)
            {
                if (Walkers[i].characterId == h.characterId && Walkers[i].puppet != null)
                {
                    Walkers[i].puppet.SetSkinTier(tier, autoFallbackToBase: true);
                    Walkers[i].cheerTimer = 0.85f;
                    View.BurstChips(ForestSliceView.Iso(Walkers[i].gx, Walkers[i].gy) + new Vector3(0f, 0.45f, 0f), 12);
                }
            }
        }

        public void BuildInitialVisuals(bool spawnStarterPiles = false)
        {
            Sim.SyncRevealedNodes(View.Map, silent: true);
            for (int i = 0; i < Sim.Nodes.Count; i++)
                RefreshNodeVisual(Sim.Nodes[i]);
            SyncSurvivorWalkers();

            if (spawnStarterPiles)
            {
                // 仅在开阔营地最外圈林缘点缀 3 处采集物，彻底留空中央大广场供操作与行走！
                SpawnFieldNode(FieldKind.LogPile, 18.6f, 19.8f, 8);
                SpawnFieldNode(FieldKind.BerryBush, 12.6f, 19.4f, 4);
                SpawnFieldNode(FieldKind.ScrapWreck, 16.8f, 12.8f, 6);
            }
            else
            {
                // 中后期场景也仅在外围林缘点缀两处生态采集物
                SpawnFieldNode(FieldKind.BerryBush, 11.4f, 17.6f, 3);
                SpawnFieldNode(FieldKind.ScrapWreck, 20.6f, 13.2f, 6);
            }
        }

        public void SpawnFieldNode(FieldKind kind, float gx, float gy, int amount)
        {
            Sprite sp = null;
            float scale = 0.60f;
            if (kind == FieldKind.LogPile)
            {
                sp = ForestArt.Get("logs", ForestPlaceholderArt.LogPile);
                scale = 0.56f;
            }
            else if (kind == FieldKind.BerryBush)
            {
                sp = ForestArt.Get("prop_berry_bush", () => ForestPlaceholderArt.GrassTuft(1));
                scale = 0.50f;
            }
            else if (kind == FieldKind.ScrapWreck)
            {
                sp = ForestArt.Get("prop_scrap_wreck", () => ForestArt.Get("loot_pile", ForestPlaceholderArt.LogPile));
                scale = 0.54f;
            }
            else
            {
                Sprite[] lit; Sprite[] deep; Sprite sh2;
                if (ForestArt.LoadTreeAtlas(out lit, out deep, out sh2) && lit != null && lit.Length > 0)
                    sp = lit[0];
                else
                    sp = ForestArt.Get("tree_0", () => ForestPlaceholderArt.EdgeTree(1));
                scale = 0.58f;
            }

            // 像素级防穿模选点：自动检测与所有建筑、现有采集物、角色的真实贴图包围盒，若重叠则螺旋外推找净空！
            if (!TryFindCleanPropSpot(sp, scale, gx, gy, out float cleanGx, out float cleanGy, out Rect visRect))
                return;

            gx = cleanGx;
            gy = cleanGy;
            View.OccupyCells(Mathf.FloorToInt(gx), Mathf.FloorToInt(gy), 1, 1);

            var go = new GameObject("Field_" + kind);
            go.transform.SetParent(View.ModuleRoot, false);
            Vector3 p = ForestSliceView.Iso(gx, gy);
            go.transform.localPosition = p;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = Mathf.RoundToInt(-p.y * 100f);
            GameObject sh = View.CreateShadow(gx, gy, 0.54f, 0.46f);
            GroundPiles.Add(new GroundPile { kind = kind, gx = gx, gy = gy, amount = amount, visualRect = visRect, go = go, shadowGo = sh });
        }

        int CountFieldKind(FieldKind kind)
        {
            int c = 0;
            for (int i = 0; i < GroundPiles.Count; i++)
                if (GroundPiles[i].kind == kind) c++;
            return c;
        }

        void OnNodeHammered(CampNode n)
        {
            if (Visuals.TryGetValue(n.id, out NodeVisual nv))
            {
                nv.bounceTimer = 0.42f;
                View.BurstChips(ForestSliceView.Iso(n.gx, n.gy) + new Vector3(0f, 0.45f, 0f), 20);
            }
            for (int i = 0; i < Walkers.Count; i++)
                Walkers[i].cheerTimer = 1.0f;
        }

        public void TriggerBuildingTapBounce(CampNode n)
        {
            if (n != null && Visuals.TryGetValue(n.id, out NodeVisual nv) && nv.bounceTimer <= 0f)
                nv.bounceTimer = 0.24f;
        }

        public void RefreshNodeVisual(CampNode n)
        {
            if (!Visuals.TryGetValue(n.id, out NodeVisual nv))
            {
                var go = new GameObject("Node_" + n.id);
                go.transform.SetParent(View.ModuleRoot, false);
                var sr = go.AddComponent<SpriteRenderer>();
                GameObject sh = n.kind == NodeKind.Farm
                    ? null
                    : View.CreateShadow(n.gx, n.gy, 0.85f + n.occupyW * 0.32f, 0.80f + n.occupyH * 0.22f);
                nv = new NodeVisual { node = n, go = go, sr = sr, shadowGo = sh };
                Visuals[n.id] = nv;
            }

            ClearExtras(nv);

            if (n.state == NodeState.HiddenInForest || n.state == NodeState.Depleted)
            {
                nv.go.SetActive(false);
                if (nv.shadowGo != null) nv.shadowGo.SetActive(false);
                return;
            }

            Sprite sp = ResolveNodeSprite(n, out Color tint, out bool isFlat);
            if (sp == null)
            {
                nv.go.SetActive(false);
                if (nv.shadowGo != null) nv.shadowGo.SetActive(false);
                return;
            }

            nv.go.SetActive(true);
            Vector3 pos = ForestSliceView.Iso(n.gx, n.gy);
            nv.go.transform.localPosition = pos;
            nv.go.transform.localScale = nv.baseScale;
            nv.sr.sprite = sp;
            nv.sr.color = tint;
            nv.sr.sortingOrder = isFlat ? -8900 : Mathf.RoundToInt(-pos.y * 100f);

            if (nv.shadowGo != null)
            {
                nv.shadowGo.SetActive(!isFlat && tint.a > 0.65f);
                nv.shadowGo.transform.localPosition = pos;
            }

            if (n.state == NodeState.Constructing || n.state == NodeState.ReadyToHammer)
            {
                Sprite dustSp = ForestArt.Get("ico_dust", ForestPlaceholderArt.FogBlob);
                Vector3[] offsets =
                {
                    new Vector3(-0.22f, 0.28f, 0f),
                    new Vector3(0.22f, 0.34f, 0f),
                    new Vector3(0.0f, 0.52f, 0f)
                };
                for (int i = 0; i < offsets.Length; i++)
                {
                    var dgo = new GameObject("dust_" + i);
                    dgo.transform.SetParent(nv.go.transform, false);
                    dgo.transform.localPosition = offsets[i];
                    var dsr = dgo.AddComponent<SpriteRenderer>();
                    dsr.sprite = dustSp;
                    dsr.color = new Color(1f, 0.99f, 0.95f, n.state == NodeState.Constructing ? 0.94f : 0.68f);
                    dsr.sortingOrder = nv.sr.sortingOrder + 8 + i;
                    nv.extras.Add(dgo);
                    nv.dustPuffs.Add(dgo.transform);
                }
            }
            else if (n.state == NodeState.Active && n.kind == NodeKind.Barricade)
            {
                AddExtraProp(nv, "barricade_b", 14.1f, 12.8f, false);
                AddExtraProp(nv, "fence", 13.0f, 14.2f, true);
                AddExtraProp(nv, "fence", 17.2f, 12.4f, false);
            }
            else if (n.state == NodeState.Active && (n.kind == NodeKind.RelicGenerator || n.kind == NodeKind.HeatTower))
            {
                var glow = new GameObject(n.kind == NodeKind.HeatTower ? "heat_glow" : "gen_glow");
                glow.transform.SetParent(nv.go.transform, false);
                glow.transform.localPosition = new Vector3(0.0f, n.kind == NodeKind.HeatTower ? 0.52f : 0.92f, 0f);
                float gs = n.kind == NodeKind.HeatTower ? (1.8f + n.level * 0.35f) : 2.2f;
                glow.transform.localScale = new Vector3(gs, gs, 1f);
                var gsr = glow.AddComponent<SpriteRenderer>();
                gsr.sprite = ForestPlaceholderArt.FogBlob();
                gsr.color = n.kind == NodeKind.HeatTower
                    ? new Color(1f, 0.58f, 0.22f, 0.28f)
                    : new Color(1f, 0.86f, 0.38f, 0.32f);
                gsr.sortingOrder = nv.sr.sortingOrder + 2;
                nv.extras.Add(glow);
            }

            SyncSurvivorWalkers();
        }

        void AddExtraProp(NodeVisual nv, string key, float gx, float gy, bool flip)
        {
            Sprite sp = ForestArt.Get(key, null);
            if (sp == null) return;
            var go = new GameObject("extra_" + key);
            go.transform.SetParent(View.ModuleRoot, false);
            Vector3 p = ForestSliceView.Iso(gx, gy);
            go.transform.localPosition = p;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.flipX = flip;
            sr.sortingOrder = Mathf.RoundToInt(-p.y * 100f);
            nv.extras.Add(go);
        }

        static void ClearExtras(NodeVisual nv)
        {
            for (int i = 0; i < nv.extras.Count; i++)
                if (nv.extras[i] != null) DestroyGo(nv.extras[i]);
            nv.extras.Clear();
            nv.dustPuffs.Clear();
        }

        static Sprite ResolveNodeSprite(CampNode n, out Color tint, out bool isFlat)
        {
            tint = Color.white;
            isFlat = false;
            bool isGhost = n.state == NodeState.Discovered ||
                           ((n.state == NodeState.Constructing || n.state == NodeState.ReadyToHammer) && !n.isUpgradingExisting);

            switch (n.kind)
            {
                case NodeKind.Shelter:
                    if (n.level >= 3) return ForestArt.Get("shelter_lv3", ForestPlaceholderArt.Tent);
                    if (n.level == 2) return ForestArt.Get("shelter_lv2", ForestPlaceholderArt.Tent);
                    return ForestArt.Get("tent", ForestPlaceholderArt.Tent);

                case NodeKind.HeatTower:
                    if (isGhost) tint = new Color(0.85f, 0.95f, 1f, 0.55f);
                    return ForestArt.Get("heating_tower", () => ForestArt.Get("campfire", ForestPlaceholderArt.Campfire));

                case NodeKind.Dwelling:
                    if (isGhost) tint = new Color(0.85f, 0.95f, 1f, 0.52f);
                    if (n.level >= 2) return ForestArt.Get("refugee_cabin", () => ForestArt.Get("shelter_lv2", ForestPlaceholderArt.Tent));
                    return ForestArt.Get("refugee_cabin", () => ForestArt.Get("tent", ForestPlaceholderArt.Tent));

                case NodeKind.Warehouse:
                    if (isGhost) tint = new Color(0.85f, 0.95f, 1f, 0.52f);
                    return ForestArt.Get("camp_warehouse", () => ForestArt.Get("cargo_stack_unit", () => ForestArt.Get("loot_pile", ForestPlaceholderArt.LogPile)));

                case NodeKind.Kitchen:
                    if (n.level >= 2) return ForestArt.Get("smokehouse_kitchen", ForestPlaceholderArt.Campfire);
                    return ForestArt.Get("campfire", ForestPlaceholderArt.Campfire);

                case NodeKind.Sawmill:
                    if (isGhost) tint = new Color(0.85f, 0.95f, 1f, 0.56f);
                    return ForestArt.Get("sawmill_shed", ForestPlaceholderArt.LogPile);

                case NodeKind.Smithy:
                    if (isGhost) tint = new Color(0.85f, 0.95f, 1f, 0.52f);
                    return ForestArt.Get("scrap_smithy", () => ForestArt.Get("ruin_generator", ForestPlaceholderArt.Well));

                case NodeKind.Tailor:
                    if (isGhost) tint = new Color(0.85f, 0.95f, 1f, 0.52f);
                    return ForestArt.Get("tailor_shop", () => ForestArt.Get("tent", ForestPlaceholderArt.Tent));

                case NodeKind.HerbHut:
                    if (isGhost) tint = new Color(0.85f, 0.95f, 1f, 0.52f);
                    return ForestArt.Get("herb_hut", () => ForestArt.Get("tent", ForestPlaceholderArt.Tent));

                case NodeKind.RadioPost:
                    if (isGhost) tint = new Color(0.85f, 0.95f, 1f, 0.52f);
                    return ForestArt.Get("radio_post", () => ForestArt.Get("hunter_tower", ForestPlaceholderArt.Well));

                case NodeKind.Farm:
                    isFlat = true;
                    if (isGhost)
                    {
                        tint = new Color(0.9f, 0.96f, 1f, 0.48f);
                        return ForestArt.Get("farm_0", () => ForestPlaceholderArt.FarmPlot(2, 2, 0));
                    }
                    if (n.farmPhase == FarmPhase.Growing)
                        return ForestArt.Get("farm_1", () => ForestPlaceholderArt.FarmPlot(2, 2, 1));
                    if (n.farmPhase == FarmPhase.Ripe)
                        return ForestArt.Get("farm_2", () => ForestPlaceholderArt.FarmPlot(2, 2, 2));
                    return ForestArt.Get("farm_0", () => ForestPlaceholderArt.FarmPlot(2, 2, 0));

                case NodeKind.Watchtower:
                    if (isGhost) tint = new Color(0.85f, 0.95f, 1f, 0.48f);
                    return ForestArt.Get("hunter_tower", ForestPlaceholderArt.Well);

                case NodeKind.Barricade:
                    if (isGhost) tint = new Color(0.85f, 0.95f, 1f, 0.48f);
                    return ForestArt.Get("barricade", ForestPlaceholderArt.LogPile);

                case NodeKind.RelicWell:
                    if (isGhost) return ForestArt.Get("ruin_well", ForestPlaceholderArt.Well);
                    return ForestArt.Get("well", ForestPlaceholderArt.Well);

                case NodeKind.RelicLoot:
                    return ForestArt.Get("loot_pile", ForestPlaceholderArt.LogPile);

                case NodeKind.RelicGenerator:
                    if (isGhost) return ForestArt.Get("ruin_generator", ForestPlaceholderArt.LogPile);
                    return ForestArt.Get("generator_active", ForestPlaceholderArt.Well);

                case NodeKind.RelicTruck:
                    if (isGhost) return ForestArt.Get("ruin_truck", ForestPlaceholderArt.Tent);
                    return ForestArt.Get("truck_active", ForestPlaceholderArt.Tent);

                case NodeKind.RelicRoadGate:
                    if (isGhost) tint = new Color(0.80f, 0.78f, 0.74f, 1f);
                    return ForestArt.Get("road_gate", ForestPlaceholderArt.LogPile);

                case NodeKind.RiverDock:
                    if (isGhost) tint = new Color(0.85f, 0.95f, 1f, 0.55f);
                    return ForestArt.Get("fishing_dock", ForestPlaceholderArt.FishingDock);
            }
            return null;
        }

        // ---------------- 60FPS 多角色骨骼动画初始化与操控（阿澈 + 小棠） ----------------
        ForestHeroPuppet _fishPuppet;

        void SyncSurvivorWalkers()
        {
            var unlocked = new List<HeroSpec>();
            for (int i = 0; i < Sim.Heroes.Count; i++)
                if (Sim.Heroes[i].unlocked) unlocked.Add(Sim.Heroes[i]);

            // 主角阿澈驻守中央开阔广场（仅受玩家操控/派工才移动）；其余伙伴各自在工位旁走短距离无障碍固定直线巡逻！
            Vector2[] patrolStarts =
            {
                new Vector2(16.4f, 16.2f), // 0: 阿澈（中央广场自由受控）
                new Vector2(18.3f, 15.0f), // 1: 小棠（锯木/锻造工坊前坪 A）
                new Vector2(14.1f, 15.6f), // 2: 老陈头（野味炊事房灶台旁 A）
                new Vector2(13.9f, 17.2f), // 3: 林砚（西侧哨位走廊 A）
                new Vector2(17.5f, 17.8f), // 4: 大锤（东侧仓库车场 A）
            };
            Vector2[] patrolEnds =
            {
                new Vector2(16.4f, 16.2f), // 0: 阿澈不自动乱逛
                new Vector2(19.0f, 14.0f), // 1: 小棠（锯木/锻造工坊前坪 B）
                new Vector2(14.8f, 14.8f), // 2: 老陈头（野味炊事房灶台旁 B）
                new Vector2(14.9f, 17.5f), // 3: 林砚（西侧哨位走廊 B）
                new Vector2(18.4f, 17.1f), // 4: 大锤（东侧仓库车场 B）
            };

            while (Walkers.Count < unlocked.Count)
            {
                int idx = Walkers.Count;
                HeroSpec spec = unlocked[idx];
                string cid = string.IsNullOrEmpty(spec.characterId) ? "ache" : spec.characterId;
                PuppetRigProfile prof = PuppetRegistry.GetProfile(cid);
                Vector2 pA = idx < patrolStarts.Length ? patrolStarts[idx] : new Vector2(16.0f + idx * 0.4f, 15.5f);
                Vector2 pB = idx < patrolEnds.Length ? patrolEnds[idx] : pA + new Vector2(0.7f, -0.4f);
                Vector2 safeW = ClampAndSlideWorldPos(ForestSliceView.Iso(16.4f, 16.4f), ForestSliceView.Iso(pA.x, pA.y), 0.20f, 0.14f);
                Vector2 sc = ForestSliceView.WorldToGrid(safeW);

                var puppet = new ForestHeroPuppet(_actorRoot, prof, "Hero_" + cid + "_" + idx);
                puppet.SetSkinTier(spec.skinTier, autoFallbackToBase: true);
                puppet.SetFacing(idx % 2 == 0);
                GameObject sh = View.CreateShadow(sc.x, sc.y, idx == 0 ? 0.62f : 0.56f, 0.54f);

                var w = new Walker
                {
                    index = idx,
                    characterId = cid,
                    gx = sc.x, gy = sc.y,
                    targetGx = sc.x, targetGy = sc.y,
                    patrolA = pA,
                    patrolB = pB,
                    headingToB = true,
                    pauseTimer = 2.0f + idx * 1.3f,
                    phase = idx * 1.4f,
                    tr = puppet.Root,
                    puppet = puppet,
                    shadowTr = sh != null ? sh.transform : null,
                };

                // 挥斧/挥扳手下砸命中帧回调：爆出木屑/火星并令目标物件剧烈抖动！
                puppet.OnChopImpact += () =>
                {
                    if (w.assignedTask != null)
                    {
                        Vector3 hitPos = ForestSliceView.Iso(w.assignedTask.gx, w.assignedTask.gy) + new Vector3(0f, 0.35f, 0f);
                        View.BurstChips(hitPos, 9);
                        if (w.assignedTask.isPile && w.assignedTask.pile != null && w.assignedTask.pile.go != null)
                        {
                            w.assignedTask.pile.go.transform.localPosition =
                                ForestSliceView.Iso(w.assignedTask.gx, w.assignedTask.gy) + (Vector3)(Random.insideUnitCircle * 0.08f);
                        }
                    }
                };
                puppet.OnFootstep += () =>
                {
                    View.BurstChips(ForestSliceView.Iso(w.gx, w.gy) + new Vector3(0f, 0.05f, 0f), 1);
                };

                puppet.SyncWorldPos(ForestSliceView.Iso(w.gx, w.gy), w.shadowTr);
                puppet.Tick(0.016f);
                Walkers.Add(w);
            }
        }

        /// <summary>支持玩家用 WASD / 方向键直接操控主角阿澈在营地自由奔跑（带林缘边界滑动与建筑/物品实体碰撞）！</summary>
        public void DriveHeroByInput(Vector2 screenDir, float dt)
        {
            if (_fishingActive || Walkers.Count == 0 || screenDir.sqrMagnitude < 0.01f) return;
            Walker w = Walkers[0];
            w.hasManualMove = false;
            w.assignedTask = null;

            Vector2 curWorld = ForestSliceView.Iso(w.gx, w.gy);
            Vector2 desiredWorld = curWorld + screenDir.normalized * (3.1f * dt);
            Vector2 clampedWorld = ClampAndSlideWorldPos(curWorld, desiredWorld, 0.20f, 0.14f);
            Vector2 nextGrid = ForestSliceView.WorldToGrid(clampedWorld);

            w.gx = nextGrid.x;
            w.gy = nextGrid.y;
            w.targetGx = w.gx;
            w.targetGy = w.gy;
            ResolveActorSeparation();

            if (Mathf.Abs(screenDir.x) > 0.02f)
                w.puppet.SetFacing(screenDir.x >= 0f);
            bool actuallyMoved = (clampedWorld - curWorld).sqrMagnitude > 0.00001f;
            w.puppet.SetMode(!actuallyMoved ? HeroAnimMode.Idle : (w.carryHomeTimer > 0f ? HeroAnimMode.CarryRun : HeroAnimMode.Run));
            w.puppet.SyncWorldPos(ForestSliceView.Iso(w.gx, w.gy), w.shadowTr);
        }

        void PickNextWalkerTarget(Walker w)
        {
            // 主角阿澈 (index == 0) 绝不自动漫无目的乱走，仅响应玩家点地移动、WASD 或采集/砍树指令！
            if (w.index == 0)
            {
                w.targetGx = w.gx;
                w.targetGy = w.gy;
                return;
            }

            // 其余伙伴只在各自工位前的 A <-> B 短直线上往返慢走，绝不跨营地乱跑或撞进障碍物！
            Vector2 nextPt = w.headingToB ? w.patrolB : w.patrolA;
            w.headingToB = !w.headingToB;
            Vector2 wp = ForestSliceView.Iso(nextPt.x, nextPt.y);
            if (IsWalkableWorldPoint(wp, 0.16f, 0.12f))
            {
                w.targetGx = nextPt.x;
                w.targetGy = nextPt.y;
            }
            else
            {
                w.targetGx = w.gx;
                w.targetGy = w.gy;
            }
        }

        // =====================================================================
        // 2D 等距实体碰撞、贴图防穿模选点与角色互避核心系统
        // =====================================================================
        readonly List<ObstacleEllipse> _tempObstacles = new List<ObstacleEllipse>(32);

        static Rect ComputeSpriteWorldRect(Sprite sp, Vector2 anchorWorld, float scaleX, float scaleY, float pad = 0f)
        {
            if (sp == null)
                return new Rect(anchorWorld.x - 0.35f - pad, anchorWorld.y - 0.05f - pad, 0.70f + pad * 2f, 0.55f + pad * 2f);
            float w = (sp.rect.width / sp.pixelsPerUnit) * Mathf.Abs(scaleX);
            float h = (sp.rect.height / sp.pixelsPerUnit) * Mathf.Abs(scaleY);
            float px = sp.pivot.x / Mathf.Max(1f, sp.rect.width);
            float py = sp.pivot.y / Mathf.Max(1f, sp.rect.height);
            return new Rect(anchorWorld.x - w * px - pad, anchorWorld.y - h * py - pad, w + pad * 2f, h + pad * 2f);
        }

        bool OverlapsAnyBuildingOrPile(Vector2 testWorld, float halfW, float height)
        {
            Rect testRect = new Rect(testWorld.x - halfW, testWorld.y - 0.06f, halfW * 2f, height + 0.06f);
            return OverlapsAnyVisualRect(testRect);
        }

        bool OverlapsAnyVisualRect(Rect testRect)
        {
            for (int i = 0; i < Sim.Nodes.Count; i++)
            {
                CampNode n = Sim.Nodes[i];
                if (n.state == NodeState.HiddenInForest || n.state == NodeState.Depleted) continue;
                Vector2 nIso = ForestSliceView.Iso(n.gx, n.gy);
                Rect nr;
                if (Visuals.TryGetValue(n.id, out NodeVisual nv) && nv.sr != null && nv.sr.sprite != null)
                    nr = ComputeSpriteWorldRect(nv.sr.sprite, nIso, nv.baseScale.x, nv.baseScale.y, pad: 0.08f);
                else
                    nr = new Rect(nIso.x - 0.65f, nIso.y - 0.10f, 1.30f, 0.90f);
                if (testRect.Overlaps(nr)) return true;
            }

            for (int i = 0; i < GroundPiles.Count; i++)
            {
                if (testRect.Overlaps(GroundPiles[i].visualRect)) return true;
            }

            for (int i = 0; i < Walkers.Count; i++)
            {
                Vector2 wIso = ForestSliceView.Iso(Walkers[i].gx, Walkers[i].gy);
                Rect wr = new Rect(wIso.x - 0.28f, wIso.y - 0.06f, 0.56f, 0.72f);
                if (testRect.Overlaps(wr)) return true;
            }
            return false;
        }

        bool TryFindCleanPropSpot(Sprite sp, float scale, float prefGx, float prefGy, out float outGx, out float outGy, out Rect outVisRect)
        {
            const float visualPad = 0.24f;
            float[] radii = { 0f, 0.55f, 1.05f, 1.55f, 2.10f, 2.70f, 3.30f };
            for (int rIdx = 0; rIdx < radii.Length; rIdx++)
            {
                float r = radii[rIdx];
                int steps = rIdx == 0 ? 1 : 12;
                for (int s = 0; s < steps; s++)
                {
                    float ang = s * (Mathf.PI * 2f / steps) + rIdx * 0.37f;
                    float gx = prefGx + Mathf.Cos(ang) * r;
                    float gy = prefGy + Mathf.Sin(ang) * r;
                    // 严禁采集物刷进营地中央开阔广场与伙伴固定巡逻走廊！
                    if (gx > 14.2f && gx < 18.8f && gy > 13.6f && gy < 18.8f) continue;

                    Vector2 wPos = ForestSliceView.Iso(gx, gy);
                    if (!IsInsideClearedBoundary(wPos, gridMargin: 0.52f)) continue;

                    Rect vr = ComputeSpriteWorldRect(sp, wPos, scale, scale, pad: visualPad);
                    if (OverlapsAnyVisualRect(vr)) continue;

                    outGx = gx;
                    outGy = gy;
                    outVisRect = vr;
                    return true;
                }
            }
            outGx = prefGx;
            outGy = prefGy;
            outVisRect = default;
            return false;
        }

        void GatherObstacles(List<ObstacleEllipse> list)
        {
            // 2D 闵可夫斯基和碰撞椭圆：因为人物身高约 0.68u 向上延伸，将椭圆中心略向下偏 (-0.06~-0.10u) 并扩大纵向半径，
            // 确保角色不仅脚底踩不到物品上，连从物品正下方走近时头顶/胸口也不会盖住篝火、原木堆或帐篷贴图！
            list.Clear();
            for (int i = 0; i < Sim.Nodes.Count; i++)
            {
                CampNode n = Sim.Nodes[i];
                if (n.state == NodeState.HiddenInForest || n.state == NodeState.Depleted) continue;
                if (n.kind == NodeKind.Farm && n.state == NodeState.Discovered) continue;

                Vector2 iso = ForestSliceView.Iso(n.gx, n.gy);
                ObstacleEllipse ob;
                switch (n.kind)
                {
                    case NodeKind.Shelter:
                    case NodeKind.Sawmill:
                        ob = new ObstacleEllipse { center = iso + new Vector2(0f, 0.08f), rx = 0.84f, ry = 0.54f };
                        break;
                    case NodeKind.Dwelling:
                    case NodeKind.Warehouse:
                    case NodeKind.Smithy:
                    case NodeKind.Tailor:
                    case NodeKind.HerbHut:
                    case NodeKind.RadioPost:
                        ob = new ObstacleEllipse { center = iso + new Vector2(0f, 0.06f), rx = 0.72f, ry = 0.48f };
                        break;
                    case NodeKind.HeatTower:
                        ob = new ObstacleEllipse { center = iso + new Vector2(0f, 0.02f), rx = 0.58f, ry = 0.46f };
                        break;
                    case NodeKind.Kitchen:
                        ob = n.level >= 2
                            ? new ObstacleEllipse { center = iso + new Vector2(0f, 0.06f), rx = 0.76f, ry = 0.50f }
                            : new ObstacleEllipse { center = iso + new Vector2(0f, -0.06f), rx = 0.42f, ry = 0.48f };
                        break;
                    case NodeKind.Farm:
                        ob = new ObstacleEllipse { center = iso + new Vector2(0f, -0.08f), rx = 0.72f, ry = 0.44f };
                        break;
                    case NodeKind.RelicGenerator:
                    case NodeKind.RelicTruck:
                    case NodeKind.RelicRoadGate:
                        ob = new ObstacleEllipse { center = iso + new Vector2(0f, 0.06f), rx = 0.80f, ry = 0.52f };
                        break;
                    case NodeKind.RiverDock:
                        ob = new ObstacleEllipse { center = iso + new Vector2(0f, -0.02f), rx = 0.62f, ry = 0.42f };
                        break;
                    default:
                        ob = new ObstacleEllipse { center = iso + new Vector2(0f, -0.04f), rx = 0.50f, ry = 0.44f };
                        break;
                }
                list.Add(ob);
            }

            for (int i = 0; i < GroundPiles.Count; i++)
            {
                GroundPile p = GroundPiles[i];
                Vector2 iso = ForestSliceView.Iso(p.gx, p.gy);
                list.Add(new ObstacleEllipse { center = iso + new Vector2(0f, -0.08f), rx = 0.38f, ry = 0.44f });
            }

            for (int i = 0; i < View.StumpWorldCenters.Count; i++)
            {
                Vector2 st = View.StumpWorldCenters[i];
                list.Add(new ObstacleEllipse { center = st + new Vector2(0f, -0.10f), rx = 0.24f, ry = 0.32f });
            }
        }

        public bool IsInsideClearedBoundary(Vector2 worldPos, float gridMargin = 0.38f)
        {
            Vector2 g = ForestSliceView.WorldToGrid(worldPos);
            int cx = Mathf.FloorToInt(g.x), cy = Mathf.FloorToInt(g.y);
            if (!View.Map.IsCleared(cx, cy) || ForestMap.IsNearRiver(cx, cy)) return false;

            // 后排林缘只需 0.42 格净空，前排林缘（屏幕下方树冠高 1.5u）预留 1.35 格防遮挡安全线，角色和物品绝不会钻进前排树叶底下！
            float backM = gridMargin;
            float frontM = Mathf.Max(gridMargin, 1.35f);
            if (!View.Map.IsCleared(Mathf.FloorToInt(g.x - frontM), Mathf.FloorToInt(g.y))) return false;
            if (!View.Map.IsCleared(Mathf.FloorToInt(g.x), Mathf.FloorToInt(g.y - frontM))) return false;
            if (!View.Map.IsCleared(Mathf.FloorToInt(g.x - frontM * 0.8f), Mathf.FloorToInt(g.y - frontM * 0.8f))) return false;
            if (!View.Map.IsCleared(Mathf.FloorToInt(g.x + backM), Mathf.FloorToInt(g.y + backM))) return false;
            if (!View.Map.IsCleared(Mathf.FloorToInt(g.x + backM), Mathf.FloorToInt(g.y - backM))) return false;
            if (!View.Map.IsCleared(Mathf.FloorToInt(g.x - backM), Mathf.FloorToInt(g.y + backM))) return false;
            return true;
        }

        public bool IsWalkableWorldPoint(Vector2 worldPos, float padX = 0.20f, float padY = 0.14f)
        {
            if (!IsInsideClearedBoundary(worldPos, 0.38f)) return false;
            GatherObstacles(_tempObstacles);
            for (int i = 0; i < _tempObstacles.Count; i++)
            {
                ObstacleEllipse ob = _tempObstacles[i];
                float rx = ob.rx + padX, ry = ob.ry + padY;
                float nx = (worldPos.x - ob.center.x) / rx;
                float ny = (worldPos.y - ob.center.y) / ry;
                if (nx * nx + ny * ny < 1f) return false;
            }
            return true;
        }

        Vector2 ClampAndSlideWorldPos(Vector2 prevWorld, Vector2 desiredWorld, float padX, float padY)
        {
            Vector2 p = desiredWorld;

            // 1. 林缘与河岸边界顺滑滑动（斜撞树林时自动沿林缘切线滑行）
            if (!IsInsideClearedBoundary(p, 0.38f))
            {
                Vector2 prevG = ForestSliceView.WorldToGrid(prevWorld);
                Vector2 desG = ForestSliceView.WorldToGrid(desiredWorld);
                Vector2 tryGx = ForestSliceView.Iso(desG.x, prevG.y);
                Vector2 tryGy = ForestSliceView.Iso(prevG.x, desG.y);
                Vector2 tryWx = new Vector2(desiredWorld.x, prevWorld.y);
                Vector2 tryWy = new Vector2(prevWorld.x, desiredWorld.y);

                if (IsInsideClearedBoundary(tryGx, 0.38f)) p = tryGx;
                else if (IsInsideClearedBoundary(tryGy, 0.38f)) p = tryGy;
                else if (IsInsideClearedBoundary(tryWx, 0.38f)) p = tryWx;
                else if (IsInsideClearedBoundary(tryWy, 0.38f)) p = tryWy;
                else if (IsInsideClearedBoundary(prevWorld, 0.25f)) p = prevWorld;
            }

            // 2. 建筑、采集物、树桩椭圆实体推出（3 次迭代保证多障碍交界处也绝不穿入贴图！）
            GatherObstacles(_tempObstacles);
            for (int iter = 0; iter < 3; iter++)
            {
                for (int i = 0; i < _tempObstacles.Count; i++)
                {
                    ObstacleEllipse ob = _tempObstacles[i];
                    float rx = ob.rx + padX;
                    float ry = ob.ry + padY;
                    float dx = p.x - ob.center.x;
                    float dy = p.y - ob.center.y;
                    float nx = dx / rx;
                    float ny = dy / ry;
                    float d2 = nx * nx + ny * ny;
                    if (d2 < 1f)
                    {
                        if (d2 < 0.0001f)
                            p = ob.center + new Vector2(rx, 0f);
                        else
                        {
                            // 横向优先顺滑推出：从下方或上方撞向篝火/物品时自然向左右两侧绕开，绝不停在物品正前方遮挡贴图！
                            Vector2 biased = new Vector2(nx * 1.45f, ny * 0.85f);
                            if (biased.sqrMagnitude < 0.0001f) biased = new Vector2(1f, 0f);
                            biased.Normalize();
                            p = ob.center + new Vector2(biased.x * rx, biased.y * ry);
                        }
                    }
                }
            }

            if (!IsInsideClearedBoundary(p, 0.28f) && IsInsideClearedBoundary(prevWorld, 0.28f))
                p = prevWorld;
            return p;
        }

        Vector2 StepWalkerAroundObstacles(Walker self, Vector2 curW, Vector2 goalW, float worldSpeed, float dt)
        {
            Vector2 toGoal = goalW - curW;
            float dist = toGoal.magnitude;
            if (dist < 0.001f) return curW;

            Vector2 dir = toGoal / dist;
            Vector2 steer = Vector2.zero;

            // 前瞻切向绕障：遇到建筑、原木堆或其他同伴挡在正前方时，自动向空旷侧平滑绕行！
            GatherObstacles(_tempObstacles);
            float lookAhead = Mathf.Min(dist, 0.62f);
            Vector2 probe = curW + dir * (lookAhead * 0.65f);

            for (int i = 0; i < _tempObstacles.Count; i++)
            {
                ObstacleEllipse ob = _tempObstacles[i];
                float rx = ob.rx + 0.30f, ry = ob.ry + 0.22f;
                float nx = (probe.x - ob.center.x) / rx;
                float ny = (probe.y - ob.center.y) / ry;
                float d2 = nx * nx + ny * ny;
                if (d2 < 1.15f)
                {
                    Vector2 perpA = new Vector2(-dir.y, dir.x);
                    Vector2 perpB = new Vector2(dir.y, -dir.x);
                    Vector2 fromOb = (curW - ob.center).normalized;
                    Vector2 chosen = Vector2.Dot(perpA, fromOb) >= 0f ? perpA : perpB;
                    steer += chosen * (1.25f - Mathf.Min(1.15f, Mathf.Sqrt(d2))) * 1.4f;
                }
            }

            for (int i = 0; i < Walkers.Count; i++)
            {
                Walker other = Walkers[i];
                if (other == self) continue;
                Vector2 oPos = ForestSliceView.Iso(other.gx, other.gy);
                Vector2 diff = probe - oPos;
                float nx = diff.x / 0.62f;
                float ny = diff.y / 0.72f;
                float d2 = nx * nx + ny * ny;
                if (d2 < 1.1f)
                {
                    float side = (curW.x >= oPos.x) ? 1f : -1f;
                    if (Mathf.Abs(curW.x - oPos.x) < 0.04f) side = self.index == 0 ? -1f : 1f;
                    steer += new Vector2(side * 1.2f, 0f);
                }
            }

            Vector2 moveDir = (dir + steer).normalized;
            float stepLen = Mathf.Min(dist, worldSpeed * dt);
            Vector2 desired = curW + moveDir * stepLen;
            return ClampAndSlideWorldPos(curW, desired, 0.20f, 0.14f);
        }

        Vector2 GetChopStandWorldPos(Walker w, ChopTask task)
        {
            Vector2 targetW = ForestSliceView.Iso(task.gx, task.gy);
            if (task.isPile)
            {
                // 站在原木堆/浆果丛/废铁残骸的左右外侧挥斧/挥管钳，绝不踩到物品贴图上！
                Vector2 leftStand = targetW + new Vector2(-0.62f, -0.02f);
                Vector2 rightStand = targetW + new Vector2(0.62f, -0.02f);
                Vector2 curW = ForestSliceView.Iso(w.gx, w.gy);
                bool leftOk = IsInsideClearedBoundary(leftStand, 0.30f);
                bool rightOk = IsInsideClearedBoundary(rightStand, 0.30f);
                if (leftOk && !rightOk) return leftStand;
                if (rightOk && !leftOk) return rightStand;
                return Vector2.Distance(curW, leftStand) <= Vector2.Distance(curW, rightStand) ? leftStand : rightStand;
            }
            else
            {
                // 砍伐外围树林区块：停在已开垦林缘内侧安全线挥斧
                Vector2 curW = ForestSliceView.Iso(w.gx, w.gy);
                for (int s = 1; s <= 12; s++)
                {
                    float t = s / 12f;
                    Vector2 cand = Vector2.Lerp(targetW, curW, t);
                    if (IsWalkableWorldPoint(cand, 0.18f, 0.12f))
                        return cand;
                }
                return curW;
            }
        }

        void ResolveActorSeparation()
        {
            // 严格不穿模分离椭圆（sepX=0.58, sepY=0.72）：优先让正在移动的角色绕开驻守待机的伙伴，不把伙伴推离固定工位！
            const float sepX = 0.58f;
            const float sepY = 0.72f;

            for (int iter = 0; iter < 3; iter++)
            {
                for (int i = 0; i < Walkers.Count; i++)
                {
                    for (int j = i + 1; j < Walkers.Count; j++)
                    {
                        Walker a = Walkers[i];
                        Walker b = Walkers[j];
                        bool aMoving = a.puppet.Mode == HeroAnimMode.Walk || a.puppet.Mode == HeroAnimMode.Run || a.puppet.Mode == HeroAnimMode.CarryRun;
                        bool bMoving = b.puppet.Mode == HeroAnimMode.Walk || b.puppet.Mode == HeroAnimMode.Run || b.puppet.Mode == HeroAnimMode.CarryRun;
                        if (aMoving && !bMoving)
                        {
                            Walker tmp = a; a = b; b = tmp; // 让固定站桩的伙伴充当锚点 a，移动方 b 主动绕开
                        }

                        Vector2 wa = ForestSliceView.Iso(a.gx, a.gy);
                        Vector2 wb = ForestSliceView.Iso(b.gx, b.gy);
                        float dx = wb.x - wa.x;
                        float dy = wb.y - wa.y;
                        float nx = dx / sepX;
                        float ny = dy / sepY;
                        float d2 = nx * nx + ny * ny;
                        if (d2 >= 1f) continue;

                        float clampedY = Mathf.Clamp(dy, -0.18f, 0.18f);
                        float inv = 1f / Mathf.Sqrt(Mathf.Max(0.0001f, d2));
                        Vector2[] candidatesForB =
                        {
                            wa + new Vector2(dx >= 0f ? sepX : -sepX, clampedY),
                            wa + new Vector2(dx >= 0f ? -sepX : sepX, clampedY),
                            wa + new Vector2(dx * inv * 1.04f, dy * inv * 1.04f),
                            wa + new Vector2(0f, dy >= 0f ? sepY : -sepY),
                            wa + new Vector2(0f, dy >= 0f ? -sepY : sepY),
                        };

                        bool resolvedB = false;
                        float bestDist = float.MaxValue;
                        Vector2 bestB = wb;
                        for (int c = 0; c < candidatesForB.Length; c++)
                        {
                            Vector2 cand = candidatesForB[c];
                            if (!IsWalkableWorldPoint(cand, 0.18f, 0.12f)) continue;
                            float d = Vector2.Distance(wb, cand);
                            if (d < bestDist)
                            {
                                bestDist = d;
                                bestB = cand;
                                resolvedB = true;
                            }
                        }

                        if (resolvedB)
                        {
                            Vector2 gb = ForestSliceView.WorldToGrid(bestB);
                            b.gx = gb.x;
                            b.gy = gb.y;
                        }
                        else
                        {
                            Vector2 altA = ClampAndSlideWorldPos(wa, wb + new Vector2(dx >= 0f ? -sepX : sepX, 0f), 0.18f, 0.12f);
                            Vector2 ga = ForestSliceView.WorldToGrid(altA);
                            a.gx = ga.x;
                            a.gy = ga.y;
                        }
                    }
                }
            }
        }

        // ---------------- 推镜实景钓鱼舞台（骨骼主角阿澈亲自登台执竿！） ----------------
        public void SetFishingStageActive(bool active)
        {
            _fishingActive = active;
            FishBobberInWater = false;
            FishBitingNow = false;
            _leapTimer = 0f;

            if (!active)
            {
                if (_fishRoot != null) _fishRoot.SetActive(false);
                if (Walkers.Count > 0 && Walkers[0].tr != null) Walkers[0].tr.gameObject.SetActive(true);
                return;
            }

            if (Walkers.Count > 0 && Walkers[0].tr != null) Walkers[0].tr.gameObject.SetActive(false);

            View.Map.ClearRectInstant(10, 1, 14, 4);
            View.Map.RebuildMask();
            View.RefreshEdges(instant: true);

            if (_fishRoot == null)
            {
                _fishRoot = new GameObject("FishingStageActors");
                _fishRoot.transform.SetParent(_actorRoot, false);

                _fishPuppet = new ForestHeroPuppet(_fishRoot.transform, "FishingHeroPuppet");
                _fishPuppet.SetFacing(false);
                _anglerTr = _fishPuppet.Root;

                var bobGo = new GameObject("Bobber");
                bobGo.transform.SetParent(_fishRoot.transform, false);
                var bsr = bobGo.AddComponent<SpriteRenderer>();
                bsr.sprite = ForestComicUiSkin.RedDotBadge;
                bobGo.transform.localScale = new Vector3(0.32f, 0.32f, 1f);
                bsr.sortingOrder = 6000;
                _bobberTr = bobGo.transform;

                var fshGo = new GameObject("UnderwaterFish");
                fshGo.transform.SetParent(_fishRoot.transform, false);
                _fishShadowSr = fshGo.AddComponent<SpriteRenderer>();
                _fishShadowSr.sprite = ForestArt.Get("fx_fish_big", () => ForestPlaceholderArt.Leaf(1));
                _fishShadowSr.color = new Color(0.06f, 0.14f, 0.22f, 0.65f);
                fshGo.transform.localScale = new Vector3(0.55f, 0.42f, 1f);
                _fishShadowSr.sortingOrder = 5990;
                _fishShadowTr = fshGo.transform;

                var leapGo = new GameObject("LeapingTrout");
                leapGo.transform.SetParent(_fishRoot.transform, false);
                var lsr = leapGo.AddComponent<SpriteRenderer>();
                lsr.sprite = ForestArt.Get("fx_fish_big", () => ForestPlaceholderArt.Leaf(1));
                lsr.sortingOrder = 7500;
                leapGo.transform.localScale = new Vector3(0.68f, 0.68f, 1f);
                _leapingFishTr = leapGo.transform;
            }

            _fishRoot.SetActive(true);
            Vector3 dockPos = ForestSliceView.Iso(12.45f, 4.45f);
            _fishPuppet.SyncWorldPos(dockPos + new Vector3(-0.08f, 0.12f, 0f), null, sortingBias: 30);
            _fishPuppet.SetMode(HeroAnimMode.Idle);
            _fishPuppet.Tick(0.02f);
            _bobberTr.gameObject.SetActive(false);
            _fishShadowTr.gameObject.SetActive(false);
            _leapingFishTr.gameObject.SetActive(false);
        }

        public void CastFishingBobber()
        {
            FishBobberInWater = true;
            FishBitingNow = false;
            _fishAnimTime = 0f;
            if (_bobberTr != null)
            {
                _bobberTr.gameObject.SetActive(true);
                _bobberTr.localPosition = ForestSliceView.Iso(11.6f, 3.65f);
            }
            if (_fishShadowTr != null)
            {
                _fishShadowTr.gameObject.SetActive(true);
                _fishShadowTr.localPosition = ForestSliceView.Iso(10.8f, 3.2f);
            }
        }

        public void SetFishBitingState(bool biting)
        {
            FishBitingNow = biting;
            if (biting && _bobberTr != null)
                View.BurstChips(_bobberTr.position, 8);
        }

        public void PlayCatchFishLeap()
        {
            FishBobberInWater = false;
            FishBitingNow = false;
            if (_bobberTr != null) _bobberTr.gameObject.SetActive(false);
            if (_fishShadowTr != null) _fishShadowTr.gameObject.SetActive(false);
            if (_leapingFishTr != null && _anglerTr != null)
            {
                _leapFrom = ForestSliceView.Iso(11.6f, 3.65f);
                _leapTo = _anglerTr.localPosition + new Vector3(0.05f, 0.35f, 0f);
                _leapTimer = 0.55f;
                _leapingFishTr.gameObject.SetActive(true);
                View.BurstChips(_leapFrom, 14);
            }
        }

        // ---------------- 野生猎物（专产野味鲜肉）与游荡丧尸（掉落旧铁器） ----------------
        public void SpawnWildPrey()
        {
            if (!TryPickClearedEdgeCell(out float gx, out float gy)) return;
            bool isDeer = _rng.NextDouble() < 0.55;
            Sprite sp = ForestArt.Get(isDeer ? "deer" : "boar", () => ForestPlaceholderArt.Survivor(9));
            var go = new GameObject(isDeer ? "WildDeer" : "WildBoar");
            go.transform.SetParent(_actorRoot, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            GameObject sh = View.CreateShadow(gx, gy, 0.58f, 0.55f);

            var a = new WildActor
            {
                isZombie = false,
                label = isDeer ? "林间野鹿" : "山林野猪",
                gx = gx, gy = gy,
                targetGx = gx + ((float)_rng.NextDouble() - 0.5f) * 1.4f,
                targetGy = gy + ((float)_rng.NextDouble() - 0.5f) * 1.4f,
                timer = 24f,
                phase = (float)_rng.NextDouble() * 6f,
                tr = go.transform, sr = sr,
                shadowTr = sh != null ? sh.transform : null,
            };
            SyncActorTransform(a.tr, a.sr, a.shadowTr, a.gx, a.gy, 0f, 0f);
            WildActors.Add(a);
        }

        public void SpawnWanderingZombie(bool hordeMember = false)
        {
            if (!TryPickClearedEdgeCell(out float gx, out float gy)) return;
            bool kindB = _rng.NextDouble() < 0.5;
            Sprite sp = ForestArt.Get(kindB ? "zombie_1" : "zombie_0", () => ForestPlaceholderArt.Survivor(8));
            var go = new GameObject("WanderingZombie");
            go.transform.SetParent(_actorRoot, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            GameObject sh = View.CreateShadow(gx, gy, 0.52f, 0.50f);

            CampNode fire = Sim.Find("kitchen");
            var a = new WildActor
            {
                isZombie = true,
                isHordeRaid = hordeMember,
                label = hordeMember ? "尸潮前锋" : "游荡丧尸",
                gx = gx, gy = gy,
                targetGx = fire != null ? fire.gx : 14.8f,
                targetGy = fire != null ? fire.gy : 14.8f,
                timer = 35f,
                phase = (float)_rng.NextDouble() * 6f,
                tr = go.transform, sr = sr,
                shadowTr = sh != null ? sh.transform : null,
            };
            SyncActorTransform(a.tr, a.sr, a.shadowTr, a.gx, a.gy, 0f, 0f);
            WildActors.Add(a);
        }

        public void StartHordeDefenseWave()
        {
            if (_hordeWaveRunning || Sim.HordeRepelled) return;
            _hordeWaveRunning = true;
            _hordeRemaining = 5;
            for (int i = 0; i < 5; i++)
                SpawnWanderingZombie(hordeMember: true);
            OnCustomToast?.Invoke("尸潮突袭！点击丧尸或靠哨塔拒马迎击！", new Vector2(14.8f, 15.0f));
        }

        bool TryPickClearedEdgeCell(out float gx, out float gy)
        {
            var list = new List<Vector2Int>();
            for (int x = 1; x < ForestMap.N - 1; x++)
            {
                for (int y = 1; y < ForestMap.N - 1; y++)
                {
                    if (!View.Map.IsCleared(x, y) || ForestMap.IsNearRiver(x, y)) continue;
                    if (!View.Map.IsCleared(x - 1, y) || !View.Map.IsCleared(x + 1, y) ||
                        !View.Map.IsCleared(x, y - 1) || !View.Map.IsCleared(x, y + 1))
                    {
                        if (IsWalkableWorldPoint(ForestSliceView.Iso(x + 0.5f, y + 0.5f), 0.16f, 0.12f))
                            list.Add(new Vector2Int(x, y));
                    }
                }
            }
            if (list.Count == 0) { gx = 13f; gy = 13f; return false; }
            Vector2Int c = list[_rng.Next(list.Count)];
            gx = c.x + 0.5f;
            gy = c.y + 0.5f;
            return true;
        }

        public void ResolveWildActor(WildActor a, bool autoByTower = false)
        {
            if (a == null) return;
            Vector2 pos = new Vector2(a.gx, a.gy);
            View.BurstChips(ForestSliceView.Iso(a.gx, a.gy) + new Vector3(0f, 0.3f, 0f), 10);

            if (!a.isZombie)
            {
                bool archerBuff = Sim.FindHero("linyan")?.assigned == true;
                int meat = (autoByTower ? 3 : 4) + (archerBuff ? 2 : 0);
                int pelt = (autoByTower ? 2 : 3) + (archerBuff ? 1 : 0);
                // 狩猎野鹿/野猪同源双掉【野味鲜肉（进炊事房）】+【野兽毛皮（进裁缝防具坊）】！
                Sim.Add(SurvRes.Meat, meat, true);
                Sim.Add(SurvRes.Pelt, pelt, true);
                Sim.AddXp(12, pos);
            }
            else
            {
                Sim.Add(SurvRes.Scrap, 2, true);
                Sim.ScrapSalvagedTotal += 2;
                if (_rng.NextDouble() < 0.35) Sim.Add(SurvRes.Parts, 1, true);
                Sim.AddXp(12, pos);

                if (a.isHordeRaid && _hordeWaveRunning)
                {
                    _hordeRemaining--;
                    if (_hordeRemaining <= 0)
                    {
                        _hordeWaveRunning = false;
                        Sim.HordeRepelled = true;
                        Sim.Coins += 350;
                        Sim.Add(SurvRes.Blueprint, 3, true);
                        Sim.Add(SurvRes.Scrap, 12, true);
                        Sim.Add(SurvRes.Parts, 3, true);
                        Sim.AddXp(60, new Vector2(16.4f, 16.2f));
                        OnCustomToast?.Invoke("成功击退尸潮！获工程手稿+3 · 铜币+350 · 废铁+12！", new Vector2(16.4f, 16.2f));
                    }
                }
            }
            RemoveWildActor(a);
        }

        void RemoveWildActor(WildActor a)
        {
            if (a.shadowTr != null) DestroyGo(a.shadowTr.gameObject);
            if (a.tr != null) DestroyGo(a.tr.gameObject);
            WildActors.Remove(a);
        }

        // ---------------- 采集 / 拆解 / 砍树结算（严格区分产出，绝不串味！） ----------------
        SurvRes FinishChopTask(ChopTask task)
        {
            SurvRes carried = SurvRes.Wood;

            if (task.isPile && task.pile != null)
            {
                GroundPile p = task.pile;
                if (p.kind == FieldKind.BerryBush)
                {
                    carried = SurvRes.Veggie;
                    Sim.Add(SurvRes.Veggie, p.amount, true);
                    Sim.Add(SurvRes.Cloth, 2, true);
                    if (Sim.CampLevel >= 4) Sim.Add(SurvRes.Herb, 2, true);
                    Sim.AddXp(8, new Vector2(p.gx, p.gy));
                }
                else if (p.kind == FieldKind.ScrapWreck)
                {
                    carried = SurvRes.Scrap;
                    Sim.Add(SurvRes.Scrap, p.amount, true);
                    Sim.ScrapSalvagedTotal += p.amount;
                    if (_rng.NextDouble() < 0.45)
                        Sim.Add(SurvRes.Parts, 1, true);
                    Sim.AddXp(10, new Vector2(p.gx, p.gy));
                }
                else
                {
                    carried = SurvRes.Wood;
                    Sim.Add(SurvRes.Wood, p.amount, true);
                    Sim.AddXp(6, new Vector2(p.gx, p.gy));
                }

                View.BurstChips(ForestSliceView.Iso(p.gx, p.gy), 12);
                if (p.go != null) DestroyGo(p.go);
                if (p.shadowGo != null) DestroyGo(p.shadowGo);
                GroundPiles.Remove(p);
            }
            else
            {
                if (View.ExecuteChopChunk(task.chunkX, task.chunkY))
                {
                    _chopsCount++;
                    if (Sim.Get(SurvRes.Ration) >= ForestSurvivalSim.ChopRationCost)
                        Sim.Add(SurvRes.Ration, -ForestSurvivalSim.ChopRationCost);

                    // 砍伐树林产出原木，中后期砍林伴生采获少量野生草药！
                    Sim.Add(SurvRes.Wood, ForestSurvivalSim.ChopWoodGain, true);
                    if (Sim.CampLevel >= 4 && _rng.NextDouble() < 0.55)
                        Sim.Add(SurvRes.Herb, 2, true);
                    Sim.AddXp(10, new Vector2(task.gx, task.gy));

                    // 砍开密林后，地面有机会显露出原本被树冠遮挡的【山道锈铁残骸】或【野生浆果丛】（需要玩家再派人去拆解/采摘）
                    double roll = _rng.NextDouble();
                    if (roll < 0.48 && CountFieldKind(FieldKind.ScrapWreck) < 4)
                    {
                        SpawnFieldNode(FieldKind.ScrapWreck, task.gx + 0.3f, task.gy - 0.3f, 6);
                        OnCustomToast?.Invoke("林下显露出旧山道【锈铁残骸】，可点击拆解废铁！", new Vector2(task.gx, task.gy));
                    }
                    else if (roll < 0.80 && CountFieldKind(FieldKind.BerryBush) < 4)
                    {
                        SpawnFieldNode(FieldKind.BerryBush, task.gx - 0.3f, task.gy + 0.2f, 3);
                    }

                    Sim.SyncRevealedNodes(View.Map, silent: false);
                }
            }
            ChopQueue.Remove(task);
            return carried;
        }

        // ---------------- 帧推进 ----------------
        public void Tick(float dt)
        {
            foreach (var kv in Visuals)
            {
                NodeVisual nv = kv.Value;
                if (nv.go == null || !nv.go.activeSelf) continue;

                if (nv.bounceTimer > 0f)
                {
                    nv.bounceTimer = Mathf.Max(0f, nv.bounceTimer - dt);
                    float u = 1f - nv.bounceTimer / 0.42f;
                    float s = 1f + Mathf.Sin(u * Mathf.PI * 2.2f) * (1f - u) * 0.22f;
                    nv.go.transform.localScale = new Vector3(nv.baseScale.x * s, nv.baseScale.y * (2f - s * 0.92f), 1f);
                    if (nv.bounceTimer <= 0f) nv.go.transform.localScale = nv.baseScale;
                }

                for (int i = 0; i < nv.dustPuffs.Count; i++)
                {
                    Transform dtTr = nv.dustPuffs[i];
                    if (dtTr == null) continue;
                    float ph = Time.time * 4.5f + i * 2.1f;
                    float sc = 0.62f + Mathf.Sin(ph) * 0.14f;
                    dtTr.localScale = new Vector3(sc, sc, 1f);
                    dtTr.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(ph * 0.7f) * 14f);
                }
            }

            // 林缘砍树二次确认气泡超时自动收起
            if (HasPendingChop)
            {
                PendingChopTimer -= dt;
                if (PendingChopTimer <= 0f || !View.CanChopChunk(PendingChopCx, PendingChopCy))
                    HasPendingChop = false;
            }

            // 林缘自然生态再生：仅在最外圈林缘稀疏萌发（上限 2 处），保持营地干净开阔！
            _berryRegrowTimer -= dt;
            if (_berryRegrowTimer <= 0f)
            {
                _berryRegrowTimer = 22f;
                if (CountFieldKind(FieldKind.BerryBush) < 2 && TryPickClearedEdgeCell(out float bx, out float by))
                    SpawnFieldNode(FieldKind.BerryBush, bx, by, 3);
            }

            _timberRegrowTimer -= dt;
            if (_timberRegrowTimer <= 0f)
            {
                _timberRegrowTimer = 24f;
                if (CountFieldKind(FieldKind.RegrownTimber) < 2 && TryPickClearedEdgeCell(out float tx, out float ty))
                    SpawnFieldNode(FieldKind.RegrownTimber, tx, ty, 7);
            }

            if (_fishingActive && _fishRoot != null)
            {
                _fishAnimTime += dt;
                if (_fishPuppet != null)
                {
                    _fishPuppet.SetMode(FishBitingNow ? HeroAnimMode.Chop : (_leapTimer > 0f ? HeroAnimMode.Cheer : HeroAnimMode.Idle));
                    _fishPuppet.Tick(dt);
                }
                if (FishBobberInWater && _bobberTr != null)
                {
                    Vector3 baseBob = ForestSliceView.Iso(11.6f, 3.65f);
                    float dip = FishBitingNow ? Mathf.Sin(_fishAnimTime * 22f) * 0.08f - 0.05f : Mathf.Sin(_fishAnimTime * 4f) * 0.025f;
                    _bobberTr.localPosition = baseBob + new Vector3(0f, dip, 0f);

                    if (_fishShadowTr != null && _fishShadowTr.gameObject.activeSelf)
                    {
                        Vector3 target = FishBitingNow ? baseBob + new Vector3(-0.08f, -0.04f, 0f) : ForestSliceView.Iso(11.1f + Mathf.Sin(_fishAnimTime * 1.8f) * 0.35f, 3.45f);
                        _fishShadowTr.localPosition = Vector3.Lerp(_fishShadowTr.localPosition, target, dt * 4.5f);
                    }
                }
                if (_leapTimer > 0f && _leapingFishTr != null)
                {
                    _leapTimer = Mathf.Max(0f, _leapTimer - dt);
                    float u = 1f - _leapTimer / 0.55f;
                    Vector3 p = Vector3.Lerp(_leapFrom, _leapTo, u) + new Vector3(0f, Mathf.Sin(u * Mathf.PI) * 0.85f, 0f);
                    _leapingFishTr.localPosition = p;
                    _leapingFishTr.localRotation = Quaternion.Euler(0f, 0f, u * 320f);
                    if (_leapTimer <= 0f) _leapingFishTr.gameObject.SetActive(false);
                }
            }

            for (int i = ChopQueue.Count - 1; i >= 0; i--)
            {
                ChopTask t = ChopQueue[i];
                t.fallbackTimer += dt;
                if (t.fallbackTimer > 6.5f && t.claimedByWalker < 0)
                    FinishChopTask(t);
            }

            _chatterCooldown -= dt;
            if (_chatterCooldown <= 0f && Walkers.Count > 0)
            {
                _chatterCooldown = 11f + (float)_rng.NextDouble() * 7f;
                Walker rw = Walkers[_rng.Next(Walkers.Count)];
                if (Sim.ActiveDemand == null || Sim.ActiveDemand.walkerIndex != rw.index)
                {
                    rw.chatterText = AmbientChatters[_rng.Next(AmbientChatters.Length)];
                    rw.chatterTimer = 2.6f;
                }
            }

            Vector2[] preTickWorlds = new Vector2[Walkers.Count];
            Vector2[] preTickGoals = new Vector2[Walkers.Count];
            for (int i = 0; i < Walkers.Count; i++)
            {
                Walker w = Walkers[i];
                preTickWorlds[i] = ForestSliceView.Iso(w.gx, w.gy);
                w.phase += dt;
                if (w.chatterTimer > 0f) w.chatterTimer = Mathf.Max(0f, w.chatterTimer - dt);
                if (w.cheerTimer > 0f) w.cheerTimer = Mathf.Max(0f, w.cheerTimer - dt);

                // 仅主角阿澈 (index == 0) 自动接取玩家点选的采集/砍树任务，其余伙伴专注各自工位固定巡逻！
                if (w.index == 0 && w.assignedTask == null && w.carryHomeTimer <= 0f)
                {
                    for (int q = 0; q < ChopQueue.Count; q++)
                    {
                        if (ChopQueue[q].claimedByWalker < 0)
                        {
                            ChopQueue[q].claimedByWalker = w.index;
                            w.assignedTask = ChopQueue[q];
                            w.hasManualMove = false;
                            break;
                        }
                    }
                }

                if (w.assignedTask != null)
                {
                    ChopTask task = w.assignedTask;
                    if (!ChopQueue.Contains(task))
                    {
                        w.assignedTask = null;
                        preTickGoals[i] = preTickWorlds[i];
                        continue;
                    }

                    Vector2 curW = ForestSliceView.Iso(w.gx, w.gy);
                    Vector2 taskW = ForestSliceView.Iso(task.gx, task.gy);
                    Vector2 standW = GetChopStandWorldPos(w, task);
                    preTickGoals[i] = standW;
                    float distToStand = Vector2.Distance(curW, standW);

                    if (distToStand > 0.14f && task.chopProgress <= 0f)
                    {
                        Vector2 nextW = StepWalkerAroundObstacles(w, curW, standW, 2.35f, dt);
                        Vector2 moveDelta = nextW - curW;
                        if (Mathf.Abs(moveDelta.x) > 0.002f) w.puppet.SetFacing(moveDelta.x >= 0f);
                        Vector2 ng = ForestSliceView.WorldToGrid(nextW);
                        w.gx = ng.x;
                        w.gy = ng.y;
                        w.puppet.SetMode(HeroAnimMode.Run);

                        if (moveDelta.magnitude < 0.25f * dt)
                        {
                            w.stuckTimer += dt;
                            if (w.stuckTimer > 0.35f) task.chopProgress = 0.001f;
                        }
                        else w.stuckTimer = 0f;
                    }
                    else
                    {
                        // 站在物品外侧安全边缘朝向目标：播放蓄力挥砍/管钳敲击动画！
                        w.stuckTimer = 0f;
                        if (Mathf.Abs(taskW.x - curW.x) > 0.01f) w.puppet.SetFacing(taskW.x >= curW.x);
                        w.puppet.SetMode(HeroAnimMode.Chop);
                        task.chopProgress += dt;

                        if (task.chopProgress >= 0.90f)
                        {
                            SurvRes gotRes = FinishChopTask(task);
                            w.assignedTask = null;
                            w.carryHomeTimer = 0.95f;
                            if (w.index == 0)
                            {
                                w.targetGx = 16.4f;
                                w.targetGy = 16.4f;
                            }
                            else
                            {
                                w.targetGx = w.patrolA.x;
                                w.targetGy = w.patrolA.y;
                            }
                            w.puppet.SetCarrySprite(ForestComicUiSkin.ResIcon(gotRes));
                            w.puppet.SetMode(HeroAnimMode.CarryRun);
                        }
                    }

                    continue;
                }

                if (w.carryHomeTimer > 0f)
                {
                    w.carryHomeTimer = Mathf.Max(0f, w.carryHomeTimer - dt);
                    if (w.carryHomeTimer <= 0f)
                    {
                        w.puppet.SetCarrySprite(null);
                        if (w.index == 0 && !w.hasManualMove)
                        {
                            w.targetGx = w.gx;
                            w.targetGy = w.gy;
                        }
                    }
                }

                Vector2 curWorld = ForestSliceView.Iso(w.gx, w.gy);
                Vector2 goalWorld = ForestSliceView.Iso(w.targetGx, w.targetGy);
                preTickGoals[i] = goalWorld;
                float wDist = Vector2.Distance(curWorld, goalWorld);
                if (wDist < 0.06f)
                {
                    w.hasManualMove = false;
                    w.stuckTimer = 0f;
                    w.puppet.SetMode(w.cheerTimer > 0f ? HeroAnimMode.Cheer : HeroAnimMode.Idle);
                    if (w.index > 0)
                    {
                        w.pauseTimer -= dt;
                        if (w.pauseTimer <= 0f)
                        {
                            w.pauseTimer = 4.8f + (float)_rng.NextDouble() * 3.0f;
                            PickNextWalkerTarget(w);
                        }
                    }
                }
                else
                {
                    bool brisk = w.hasManualMove || w.carryHomeTimer > 0f;
                    float worldSpd = brisk ? 2.10f : 0.76f;
                    Vector2 nextWorld = StepWalkerAroundObstacles(w, curWorld, goalWorld, worldSpd, dt);
                    Vector2 stepDelta = nextWorld - curWorld;
                    if (Mathf.Abs(stepDelta.x) > 0.002f) w.puppet.SetFacing(stepDelta.x >= 0f);

                    Vector2 ng = ForestSliceView.WorldToGrid(nextWorld);
                    w.gx = ng.x;
                    w.gy = ng.y;
                    w.puppet.SetMode(w.carryHomeTimer > 0f ? HeroAnimMode.CarryRun : (w.hasManualMove ? HeroAnimMode.Run : HeroAnimMode.Walk));
                }
            }

            // 统一执行全员防穿模互推解算，并严格校验【向目标推进速率】，彻底根除原地踏步！
            ResolveActorSeparation();
            for (int i = 0; i < Walkers.Count; i++)
            {
                Walker w = Walkers[i];
                Vector2 postWorld = ForestSliceView.Iso(w.gx, w.gy);
                float actualSpeed = dt > 0.0001f ? Vector2.Distance(preTickWorlds[i], postWorld) / dt : 0f;
                float preGoalDist = Vector2.Distance(preTickWorlds[i], preTickGoals[i]);
                float postGoalDist = Vector2.Distance(postWorld, preTickGoals[i]);
                float progressSpeed = dt > 0.0001f ? (preGoalDist - postGoalDist) / dt : 0f;

                HeroAnimMode curMode = w.puppet.Mode;
                if (curMode == HeroAnimMode.Walk || curMode == HeroAnimMode.Run || curMode == HeroAnimMode.CarryRun)
                {
                    bool stalled = actualSpeed < 0.20f || (postGoalDist > 0.08f && progressSpeed < 0.06f);
                    if (stalled)
                    {
                        w.stuckTimer += dt;
                        // 只要没有实质向前移动，当帧立即切回 Idle 待机呼吸，绝不播放哪怕 1 帧原地走路！
                        w.puppet.SetMode(w.cheerTimer > 0f ? HeroAnimMode.Cheer : HeroAnimMode.Idle);
                        if (w.stuckTimer > 0.22f)
                        {
                            w.stuckTimer = 0f;
                            if (w.assignedTask != null)
                            {
                                w.assignedTask.chopProgress = Mathf.Max(w.assignedTask.chopProgress, 0.001f);
                            }
                            else
                            {
                                w.hasManualMove = false;
                                w.carryHomeTimer = 0f;
                                w.puppet.SetCarrySprite(null);
                                w.targetGx = w.gx;
                                w.targetGy = w.gy;
                                w.pauseTimer = 4.2f;
                            }
                        }
                    }
                    else
                    {
                        w.stuckTimer = 0f;
                    }
                }

                w.puppet.SyncWorldPos(postWorld, w.shadowTr);
                w.puppet.Tick(dt);
            }

            _preyCooldown -= dt;
            if (_preyCooldown <= 0f)
            {
                _preyCooldown = 16f + (float)_rng.NextDouble() * 8f;
                int preyCount = 0;
                for (int i = 0; i < WildActors.Count; i++) if (!WildActors[i].isZombie) preyCount++;
                if (preyCount < 2) SpawnWildPrey();
            }

            if (_chopsCount >= 2)
            {
                _zombieCooldown -= dt;
                if (_zombieCooldown <= 0f)
                {
                    _zombieCooldown = 26f + (float)_rng.NextDouble() * 10f;
                    int zCount = 0;
                    for (int i = 0; i < WildActors.Count; i++) if (WildActors[i].isZombie) zCount++;
                    if (zCount < 2) SpawnWanderingZombie();
                }
            }

            for (int i = WildActors.Count - 1; i >= 0; i--)
            {
                WildActor a = WildActors[i];
                a.phase += dt;
                a.timer -= dt;
                if (a.timer <= 0f) { RemoveWildActor(a); continue; }

                float dx = a.targetGx - a.gx, dy = a.targetGy - a.gy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float speed = a.isZombie ? (a.isHordeRaid ? 0.68f : 0.42f) : 0.55f;
                if (dist > 0.1f)
                {
                    float step = Mathf.Min(dist, dt * speed);
                    a.gx += dx / dist * step;
                    a.gy += dy / dist * step;
                    Vector3 sd = ForestSliceView.Iso(a.targetGx, a.targetGy) - ForestSliceView.Iso(a.gx, a.gy);
                    if (Mathf.Abs(sd.x) > 0.01f) a.sr.flipX = sd.x > 0f;
                }
                else if (!a.isZombie)
                {
                    float nx = a.gx + ((float)_rng.NextDouble() - 0.5f) * 2.0f;
                    float ny = a.gy + ((float)_rng.NextDouble() - 0.5f) * 2.0f;
                    if (IsWalkableWorldPoint(ForestSliceView.Iso(nx, ny), 0.25f, 0.20f) &&
                        !(nx > 13.0f && nx < 20.2f && ny > 12.8f && ny < 19.6f))
                    {
                        a.targetGx = nx;
                        a.targetGy = ny;
                    }
                }
                else
                {
                    if (a.isHordeRaid) { ResolveWildActor(a, autoByTower: true); continue; }
                    int steal = Mathf.Min(1, Sim.Get(SurvRes.Ration));
                    if (steal > 0) Sim.Add(SurvRes.Ration, -steal);
                    OnCustomToast?.Invoke("游荡丧尸偷走口粮！快点击驱赶或建拒马", new Vector2(a.gx, a.gy));
                    RemoveWildActor(a);
                    continue;
                }

                if (!a.isZombie && Sim.HasWatchtower && a.timer < 20f)
                {
                    ResolveWildActor(a, autoByTower: true);
                    continue;
                }
                if (a.isZombie && (Sim.HasWatchtower || Sim.HasBarricade) && dist < 2.8f)
                {
                    ResolveWildActor(a, autoByTower: true);
                    continue;
                }

                float hop = Mathf.Abs(Mathf.Sin(a.phase * (a.isZombie ? 5.5f : 7.5f))) * 0.035f;
                float tilt = Mathf.Sin(a.phase * 5.5f) * (a.isZombie ? 6f : 2.5f);
                SyncActorTransform(a.tr, a.sr, a.shadowTr, a.gx, a.gy, hop, tilt);
            }
        }

        static void SyncActorTransform(Transform tr, SpriteRenderer sr, Transform sh, float gx, float gy, float hopY, float tiltZ)
        {
            Vector3 p = ForestSliceView.Iso(gx, gy);
            tr.localPosition = p + new Vector3(0f, hopY, 0f);
            tr.localRotation = Quaternion.Euler(0f, 0f, tiltZ);
            sr.sortingOrder = Mathf.RoundToInt(-p.y * 100f) + 2;
            if (sh != null) sh.localPosition = p;
        }

        public void ConfirmPendingChop()
        {
            if (!HasPendingChop) return;
            int cx = PendingChopCx, cy = PendingChopCy;
            HasPendingChop = false;

            if (!View.CanChopChunk(cx, cy)) return;
            for (int i = 0; i < ChopQueue.Count; i++)
                if (!ChopQueue[i].isPile && ChopQueue[i].chunkX == cx && ChopQueue[i].chunkY == cy)
                    return;

            if (Sim.Get(SurvRes.Ration) + ChopQueue.Count < ForestSurvivalSim.ChopRationCost)
            {
                if (CountFieldKind(FieldKind.BerryBush) == 0 && TryPickClearedEdgeCell(out float bx, out float by))
                    SpawnFieldNode(FieldKind.BerryBush, bx, by, 3);
                OnCustomToast?.Invoke("口粮不足！请先采摘林边【野生浆果丛】去营地篝火烤制口粮！", new Vector2(16.4f, 16.4f));
                return;
            }

            float gx = (cx + 0.5f) * ForestMap.ChunkSize;
            float gy = (cy + 0.5f) * ForestMap.ChunkSize;
            ChopQueue.Add(new ChopTask { isPile = false, chunkX = cx, chunkY = cy, gx = gx, gy = gy });
        }

        // ---------------- 地面 / 幸存者 / 生态采集点 / 建筑 / 防误触二次确认砍树 点击分发 ----------------
        public void HandleWorldClick(Camera cam, Vector2 screenPos)
        {
            if (_fishingActive) return;
            Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));

            // 1. 优先精确点中猎物或丧尸（收紧判定半径避免误触）
            for (int i = 0; i < WildActors.Count; i++)
            {
                WildActor a = WildActors[i];
                Vector3 c = ForestSliceView.Iso(a.gx, a.gy) + new Vector3(0f, 0.26f, 0f);
                if (Vector2.Distance(wp, c) < 0.48f)
                {
                    HasPendingChop = false;
                    ResolveWildActor(a, autoByTower: false);
                    return;
                }
            }

            // 2. 精确点中饥饿求食的幸存者
            if (Sim.ActiveDemand != null && Sim.ActiveDemand.walkerIndex < Walkers.Count)
            {
                Walker hw = Walkers[Sim.ActiveDemand.walkerIndex];
                Vector3 hc = ForestSliceView.Iso(hw.gx, hw.gy) + new Vector3(0f, 0.35f, 0f);
                if (Vector2.Distance(wp, hc) < 0.46f)
                {
                    HasPendingChop = false;
                    OnResidentDemandClicked?.Invoke();
                    return;
                }
            }

            // 3. 精确点中地面采集/拆解点（半径收紧至 0.42u，只有点在物品本体上才触发采集！）
            for (int i = 0; i < GroundPiles.Count; i++)
            {
                GroundPile p = GroundPiles[i];
                if (p.queued) continue;
                Vector3 pc = ForestSliceView.Iso(p.gx, p.gy) + new Vector3(0f, 0.18f, 0f);
                if (Vector2.Distance(wp, pc) < 0.42f)
                {
                    HasPendingChop = false;
                    p.queued = true;
                    ChopQueue.Add(new ChopTask { isPile = true, pile = p, gx = p.gx, gy = p.gy });
                    return;
                }
            }

            // 4. 精确点中已发现遗迹或营地建筑（半径收紧至 0.52u，留出大片自由走动与拖拽空地！）
            CampNode bestNode = null;
            float bestDist = 0.52f;
            for (int i = 0; i < Sim.Nodes.Count; i++)
            {
                CampNode n = Sim.Nodes[i];
                if (n.state == NodeState.HiddenInForest || n.state == NodeState.Depleted) continue;
                Vector3 c = ForestSliceView.Iso(n.gx, n.gy) + new Vector3(0f, n.kind == NodeKind.Farm ? 0f : 0.34f, 0f);
                float d = Vector2.Distance(wp, c);
                if (d < bestDist) { bestDist = d; bestNode = n; }
            }
            if (bestNode != null)
            {
                HasPendingChop = false;
                TriggerBuildingTapBounce(bestNode);
                OnNodeClicked?.Invoke(bestNode);
                return;
            }

            // 5. 若点击点落在已开垦的营地空地内 -> 100% 归属玩家操控主角移动 & 收起悬浮菜单，绝不误触砍树！
            Vector2 rawGrid = ForestSliceView.WorldToGrid(wp);
            if (View.Map.IsCleared(Mathf.FloorToInt(rawGrid.x), Mathf.FloorToInt(rawGrid.y)))
            {
                HasPendingChop = false;
                OnEmptyGroundClicked?.Invoke();
                if (Walkers.Count > 0)
                {
                    Walker hero = Walkers[0];
                    Vector2 safeWorld = ClampAndSlideWorldPos(ForestSliceView.Iso(hero.gx, hero.gy), wp, 0.22f, 0.16f);
                    if (IsInsideClearedBoundary(safeWorld, 0.30f))
                    {
                        Vector2 clickGrid = ForestSliceView.WorldToGrid(safeWorld);
                        hero.targetGx = clickGrid.x;
                        hero.targetGy = clickGrid.y;
                        hero.hasManualMove = true;
                        hero.stuckTimer = 0f;
                        View.BurstChips(safeWorld, 3);
                    }
                }
                return;
            }

            // 6. 点击外围林缘树木：首次点选弹出【砍伐扩地】确认气泡（避免划屏/点空白误砍树），再次点选或点气泡才正式派工！
            OnEmptyGroundClicked?.Invoke();
            if (View.TryFindChopChunkAtScreen(screenPos, out int cx, out int cy))
            {
                if (cx < ForestSurvivalSim.MinPlayableChunk || cx > ForestSurvivalSim.MaxPlayableChunk ||
                    cy < ForestSurvivalSim.MinPlayableChunk || cy > ForestSurvivalSim.MaxPlayableChunk)
                {
                    HasPendingChop = false;
                    Vector2 edgeG = new Vector2((cx + 0.5f) * ForestMap.ChunkSize, (cy + 0.5f) * ForestMap.ChunkSize);
                    OnCustomToast?.Invoke("已抵深山峡谷峭壁边界！可通过【再生松木】或木工棚巡林持续产木", edgeG);
                    return;
                }

                for (int i = 0; i < ChopQueue.Count; i++)
                    if (!ChopQueue[i].isPile && ChopQueue[i].chunkX == cx && ChopQueue[i].chunkY == cy)
                        return;

                if (HasPendingChop && PendingChopCx == cx && PendingChopCy == cy)
                {
                    ConfirmPendingChop();
                }
                else
                {
                    HasPendingChop = true;
                    PendingChopCx = cx;
                    PendingChopCy = cy;
                    PendingChopTimer = 4.0f;
                }
                return;
            }

            HasPendingChop = false;
        }

        static void DestroyGo(GameObject go)
        {
            if (Application.isPlaying) Object.Destroy(go);
            else Object.DestroyImmediate(go);
        }
    }
}
