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
            public bool isBoss;
            public bool isRunner;
            public string label;
            public float gx, gy;
            public float targetGx, targetGy;
            public float hp = 40f;
            public float maxHp = 40f;
            public float moveSpeed = 0.7f;
            public float hitFlashTimer;
            public float stunTimer;
            public float gnawCooldown;
            public float timer;
            public float phase;
            public Vector2 anchorGrid;
            public Vector2 patrolTangentWorld;
            public float patrolOffset;
            public float patrolTargetOffset;
            public float grazePauseTimer;
            public float baseScale = 0.90f;
            public Transform tr;
            public SpriteRenderer sr;
            public Transform shadowTr;
            public ForestSpriteFlipbook flipbook;
        }

        public sealed class VillagerActor
        {
            public int index;
            public int kindIdx; // 1..3 -> villager_1..3
            public float gx, gy;
            public float targetGx, targetGy;
            public float pauseTimer;
            public float cheerTimer;
            public float phase;
            public string chatterText;
            public float chatterTimer;
            public float baseScale = 0.86f;
            public Transform tr;
            public SpriteRenderer sr;
            public Transform shadowTr;
            public ForestSpriteFlipbook flipbook;
        }

        /// <summary>驻守在已建成建筑旁固定工位劳作的工匠 NPC（3 帧 PingPong 循环，零碰撞体积绝不挡路）。</summary>
        public sealed class StationedWorkerActor
        {
            public CampNode node;
            public string stripKey;
            public Vector2 worldPos;
            public float baseScale = 0.88f;
            public bool onDockDeck;
            public float phase;
            public GameObject go;
            public SpriteRenderer sr;
            public GameObject shadowGo;
            public SpriteRenderer shadowSr;
            public ForestSpriteFlipbook flipbook;
        }

        /// <summary>山林畜牧栏建成后在围栏内外欢快蹦跳觅食的微缩圈养动物（零碰撞体积）。</summary>
        public sealed class RanchPenAnimalActor
        {
            public CampNode ranchNode;
            public string stripKey;
            public Vector2 anchorWorld;
            public Vector2 patrolAxis;
            public float offset;
            public float targetOffset;
            public float pauseTimer;
            public float baseScale;
            public float phase;
            public GameObject go;
            public SpriteRenderer sr;
            public GameObject shadowGo;
            public ForestSpriteFlipbook flipbook;
        }

        public sealed class SquadCombatSlot
        {
            public HeroSpec hero;
            public Walker walker;
            public Vector2 defensePos;
            public float atkTimer;
            public float rage; // 0..100
        }

        public sealed class CombatProjectile
        {
            public Transform tr;
            public SpriteRenderer sr;
            public Vector3 fromWorld;
            public Vector3 toWorld;
            public WildActor target;
            public float damage;
            public bool piercing;
            public bool spin;
            public float t;
            public float dur;
        }

        public sealed class FireHazardZone
        {
            public Transform tr;
            public SpriteRenderer sr;
            public float gx, gy;
            public float radius;
            public float dps;
            public float life;
            public float tickTimer;
        }

        public sealed class GroundPile
        {
            public FieldKind kind;
            public float gx, gy;
            public int amount;
            public bool queued;
            public string salvageNodeId;
            public string customBubbleLabel;
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
            public string heroId;
            public string characterId;
            public float gx, gy;
            public float targetGx, targetGy;
            public Vector2 patrolA, patrolB;
            public bool headingToB;
            public bool hasManualMove;
            public int avoidSide;
            public float manualDriveHold;
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
        public readonly List<VillagerActor> CivilianVillagers = new List<VillagerActor>();
        public readonly Dictionary<string, StationedWorkerActor> StationedWorkers = new Dictionary<string, StationedWorkerActor>();
        public readonly List<RanchPenAnimalActor> RanchPenAnimals = new List<RanchPenAnimalActor>(4);
        public readonly List<SquadCombatSlot> SquadSlots = new List<SquadCombatSlot>();
        public readonly List<CombatProjectile> Projectiles = new List<CombatProjectile>();
        public readonly List<FireHazardZone> FireZones = new List<FireHazardZone>();

        // 独立策略战场控制器（《疯狂水世界》式 3×3 九宫格零树木遮挡战斗舞台）
        public ForestTacticalArena Arena { get; private set; }
        public bool InWaveDefense => Arena != null && Arena.IsBattleActive;
        public bool IsWaveDefenseActive => InWaveDefense;

        public bool WaveAutoUlt
        {
            get => Arena == null || Arena.AutoCastUlt;
            set { if (Arena != null) Arena.AutoCastUlt = value; }
        }

        public bool AutoCastUlt
        {
            get => WaveAutoUlt;
            set => WaveAutoUlt = value;
        }

        public int WaveSpeedMul => Arena != null ? Arena.SpeedMultiplier : 1;

        public float WaveTimeScale
        {
            get => WaveSpeedMul;
            set
            {
                if (Arena != null && ((value > 1.4f) != (Arena.SpeedMultiplier == 2)))
                    Arena.ToggleSpeed();
            }
        }

        public HordeWaveSpec ActiveWaveSpec;
        public float BarricadeHp;
        public float BarricadeMaxHpCached;
        public int WaveSpawnPending;
        public float WaveSpawnTimer;
        public int WaveTotalZombies;
        public int WaveKilledZombies;
        public float TeamAtkSpeedBuffTimer;
        public WildActor ActiveBossZombie;

        public event System.Action<bool, HordeWaveSpec> OnWaveDefenseEnded;
        public event System.Action<Vector3, int, bool> OnCombatDamageNumber;

        public bool HasDamageNumberListeners => OnCombatDamageNumber != null;
        public void RaiseCombatDamageNumber(Vector3 worldPos, int dmg, bool isCrit) => OnCombatDamageNumber?.Invoke(worldPos, dmg, isCrit);

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

        public ForestSurvivalWorld(ForestSliceView view, ForestSurvivalSim sim, Camera cam = null)
        {
            View = view;
            Sim = sim;
            _actorRoot = new GameObject("L4_DynamicActors").transform;
            _actorRoot.SetParent(view.transform.GetChild(0), false);
            Arena = new ForestTacticalArena(cam != null ? cam : Camera.main, sim, this);

            for (int i = 0; i < Sim.Nodes.Count; i++)
            {
                CampNode n = Sim.Nodes[i];
                if (n.state != NodeState.HiddenInForest || n.siteUncovered)
                    View.OccupyCells(n.occupyX, n.occupyY, n.occupyW, n.occupyH);
            }

            View.CanPlaceStumpFilter = (sx, sy) => !OverlapsAnyBuildingOrPile(ForestSliceView.Iso(sx, sy), 0.30f, 0.28f);

            Sim.OnNodeVisualChanged += RefreshNodeVisual;
            Sim.OnNodeForestUncovered += OnNodeForestUncovered;
            Sim.OnNodeHammeredComplete += OnNodeHammered;
            Sim.OnHeroSkinChanged += OnHeroSkinChanged;
            Sim.OnHeroUnlocked += _ => { SyncSurvivorWalkers(); RefreshWalkerDelegationStations(); };
            Sim.OnDelegationChanged += RefreshWalkerDelegationStations;
            Sim.OnCampLevelUp += (_, __) =>
            {
                if (Walkers.Count > 0 && Walkers[0].puppet != null)
                    Walkers[0].puppet.SetSkinTier(Sim.MaxUnlockedSkinTier, autoFallbackToBase: true);
            };
        }

        /// <summary>解析幸存者骨干伙伴专属 9 部件多骨骼模型档案（大锤、小棠、雷猛、林砚、铁叔、老陈头、青禾、阿飒、周博士全员使用真实骨骼，绝不回退为阿澈或单张流民静态图）。</summary>
        public static PuppetRigProfile ResolveCompanionRigProfile(string heroId, string characterId)
        {
            string key = !string.IsNullOrEmpty(characterId) ? characterId : heroId;
            if (heroId == "leimeng" || key == "hunter" || key == "leimeng")
                key = "leimeng";
            else if (heroId == "tieshu" || key == "tieshu")
                key = "tieshu";
            else if (heroId == "asa" || heroId == "qinghe" || heroId == "zhoudr")
                key = heroId;

            if (string.IsNullOrEmpty(key) || key == "ache")
                return PuppetRegistry.GetProfile("dachui");

            PuppetRigProfile prof = PuppetRegistry.GetProfile(key);
            if (prof == null || prof.characterId == "ache")
                return PuppetRegistry.GetProfile("dachui");
            return prof;
        }

        void OnHeroSkinChanged(HeroSpec h, OutfitTier tier)
        {
            if (h == null) return;
            for (int i = 1; i < Walkers.Count; i++)
            {
                if ((Walkers[i].heroId == h.id || Walkers[i].characterId == h.characterId) && Walkers[i].puppet != null)
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
            {
                CampNode n = Sim.Nodes[i];
                RefreshNodeVisual(n);
                if (n.siteUncovered && n.requiresSiteSalvage && !n.siteSalvageCleared && FindSalvagePileForNode(n.id) == null)
                    SpawnSiteSalvagePile(n, playBurst: false);
            }
            SyncSurvivorWalkers();

            if (spawnStarterPiles)
            {
                // 开局在主角(15.8,18.4)南侧开阔草甸放置 2 处醒目原木堆（紧邻 y=15 林缘无遮挡一触即采），并在起步草坪可见林缘点缀浆果丛与废铁残骸
                SpawnFieldNode(FieldKind.LogPile, 15.2f, 16.2f, 5, allowInnerMeadow: true);
                SpawnFieldNode(FieldKind.LogPile, 17.6f, 16.4f, 5, allowInnerMeadow: true);
                SpawnFieldNode(FieldKind.BerryBush, 12.6f, 20.6f, 4, allowInnerMeadow: true);
                SpawnFieldNode(FieldKind.ScrapWreck, 19.6f, 15.8f, 6, allowInnerMeadow: true);
            }
            else
            {
                // 中后期场景仅在远离建筑的外圈已开垦林缘点缀两处生态采集物，绝不刷进未砍密林！
                if (TryPickOuterResourceEdgeCell(5.5f, out float bx, out float by))
                    SpawnFieldNode(FieldKind.BerryBush, bx, by, 3);
                if (TryPickOuterResourceEdgeCell(5.5f, out float sx, out float sy))
                    SpawnFieldNode(FieldKind.ScrapWreck, sx, sy, 6);
            }
        }

        void OnNodeForestUncovered(CampNode n)
        {
            if (n == null) return;
            EnsureNodePlotMarginCleared(n);
            SpawnSiteSalvagePile(n, playBurst: true);
        }

        void EnsureNodePlotMarginCleared(CampNode n)
        {
            if (n == null || View == null || View.Map == null) return;
            View.OccupyCells(n.occupyX, n.occupyY, n.occupyW, n.occupyH);
            int x0 = Mathf.Min(n.occupyX - 1, Mathf.FloorToInt(n.gx) - 2);
            int y0 = Mathf.Min(n.occupyY - 1, Mathf.FloorToInt(n.gy) - 2);
            int x1 = Mathf.Max(n.occupyX + n.occupyW, Mathf.FloorToInt(n.gx) + 1);
            int y1 = Mathf.Max(n.occupyY + n.occupyH, Mathf.FloorToInt(n.gy) + 1);
            View.Map.ClearRectInstant(x0, y0, x1, y1);
            View.Map.SweepOrphanForestIslands(instant: true);
            View.Map.RebuildMask();
            View.RefreshEdges(instant: false);
            View.DecorateCleared();
        }

        void SpawnSiteSalvagePile(CampNode n, bool playBurst)
        {
            if (n == null || string.IsNullOrEmpty(n.id)) return;
            if (FindSalvagePileForNode(n.id) != null) return;

            View.OccupyCells(n.occupyX, n.occupyY, n.occupyW, n.occupyH);

            // 若刚砍开的同一位置恰有普通随机采集物，移除普通散件以给专属遗迹发现物腾出干净地基中心！
            for (int i = GroundPiles.Count - 1; i >= 0; i--)
            {
                GroundPile gp = GroundPiles[i];
                if (!string.IsNullOrEmpty(gp.salvageNodeId)) continue;
                if (Vector2.Distance(new Vector2(gp.gx, gp.gy), new Vector2(n.gx, n.gy)) < 1.15f)
                {
                    if (gp.go != null) DestroyGo(gp.go);
                    if (gp.shadowGo != null) DestroyGo(gp.shadowGo);
                    GroundPiles.RemoveAt(i);
                }
            }

            Sprite sp;
            FieldKind kind;
            float scale = 0.58f;
            SurvRes bonusRes = n.salvageBonusRes;
            if (bonusRes == SurvRes.Scrap || bonusRes == SurvRes.Nails || bonusRes == SurvRes.IronIngot || bonusRes == SurvRes.MetalPipe)
            {
                sp = ForestArt.Get("prop_scrap_wreck", () => ForestArt.Get("loot_pile", ForestPlaceholderArt.LogPile));
                kind = FieldKind.ScrapWreck;
                scale = 0.56f;
            }
            else if (bonusRes == SurvRes.Veggie || bonusRes == SurvRes.Herb)
            {
                sp = ForestArt.Get("prop_berry_bush", () => ForestArt.Get("loot_pile", ForestPlaceholderArt.LogPile));
                kind = FieldKind.BerryBush;
                scale = 0.54f;
            }
            else if (bonusRes == SurvRes.Wood)
            {
                sp = ForestArt.Get("logs", () => ForestArt.Get("loot_pile", ForestPlaceholderArt.LogPile));
                kind = FieldKind.LogPile;
                scale = 0.60f;
            }
            else
            {
                sp = ForestArt.Get("loot_pile", () => ForestArt.Get("logs", ForestPlaceholderArt.LogPile));
                kind = FieldKind.LogPile;
                scale = 0.58f;
            }

            float gx = n.gx;
            float gy = n.gy;
            Vector3 p = ForestSliceView.Iso(gx, gy);
            Rect visRect = ComputeSpriteWorldRect(sp, p, scale, scale, pad: 0.20f);

            var go = new GameObject("Salvage_" + n.id);
            go.transform.SetParent(View.ModuleRoot, false);
            go.transform.localPosition = p;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = Mathf.RoundToInt(-p.y * 100f) + 1;
            GameObject sh = View.CreateShadow(gx, gy, 0.58f, 0.48f);

            string label = string.IsNullOrEmpty(n.salvageLabel) ? "林间散落物资" : n.salvageLabel;
            GroundPiles.Add(new GroundPile
            {
                kind = kind,
                gx = gx,
                gy = gy,
                amount = Mathf.Max(1, n.salvageBonusAmt),
                salvageNodeId = n.id,
                customBubbleLabel = "收集:" + label,
                visualRect = visRect,
                go = go,
                shadowGo = sh,
            });

            if (playBurst)
                View.BurstChips(p + new Vector3(0f, 0.35f, 0f), 18);
        }

        public GroundPile FindSalvagePileForNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return null;
            for (int i = 0; i < GroundPiles.Count; i++)
            {
                if (GroundPiles[i].salvageNodeId == nodeId)
                    return GroundPiles[i];
            }
            return null;
        }

        public bool IsGridCoveredByForest(float gx, float gy)
            => View != null && View.Map != null && View.Map.At(Mathf.FloorToInt(gx), Mathf.FloorToInt(gy)) > 0.25f;

        public bool TryFindBestChopChunkToward(float targetGx, float targetGy, out int bestCx, out int bestCy, out Vector2 chunkCenterGrid)
        {
            bestCx = -1;
            bestCy = -1;
            chunkCenterGrid = new Vector2(targetGx, targetGy);
            if (View == null) return false;

            int maxChunk = ForestMap.N / ForestMap.ChunkSize - 1;
            Vector2 target = new Vector2(targetGx, targetGy);

            for (int pass = 0; pass < 2; pass++)
            {
                int minC = pass == 0 ? ForestSurvivalSim.MinPlayableChunk : 0;
                int maxC = pass == 0 ? Mathf.Min(maxChunk, ForestSurvivalSim.MaxPlayableChunk) : maxChunk;
                float bestDistSq = float.MaxValue;

                for (int cx = minC; cx <= maxC; cx++)
                {
                    for (int cy = minC; cy <= maxC; cy++)
                    {
                        if (!View.CanChopChunk(cx, cy)) continue;
                        Vector2 center = new Vector2(cx * ForestMap.ChunkSize + 1.5f, cy * ForestMap.ChunkSize + 1.5f);
                        float d2 = (center - target).sqrMagnitude;
                        if (d2 < bestDistSq)
                        {
                            bestDistSq = d2;
                            bestCx = cx;
                            bestCy = cy;
                            chunkCenterGrid = center;
                        }
                    }
                }
                if (bestCx >= 0) return true;
            }
            return false;
        }

        public void SpawnFieldNode(FieldKind kind, float gx, float gy, int amount, bool allowInnerMeadow = false)
        {
            if (kind == FieldKind.RegrownTimber && IsInPermanentCampTerritory(gx, gy))
                return;

            Sprite sp = null;
            float scale = 0.60f;
            if (kind == FieldKind.LogPile)
            {
                sp = ForestArt.Get("logs", ForestPlaceholderArt.LogPile);
                scale = 0.60f;
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

            // 像素级防穿模选点：自动检测与所有建筑、现有采集物、角色的真实贴图包围盒，且禁止在未砍密林或永久营地内再生树木！
            if (!TryFindCleanPropSpot(kind, sp, scale, gx, gy, allowInnerMeadow, out float cleanGx, out float cleanGy, out Rect visRect))
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
            SyncCivilianVillagers();
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
                SyncStationedArtisanWorker(n);
                if (n.kind == NodeKind.Ranch) SyncRanchPenAnimals(n);
                return;
            }

            GroundPile staleSalvage = FindSalvagePileForNode(n.id);
            if (staleSalvage != null)
            {
                if (staleSalvage.go != null) DestroyGo(staleSalvage.go);
                if (staleSalvage.shadowGo != null) DestroyGo(staleSalvage.shadowGo);
                GroundPiles.Remove(staleSalvage);
            }

            View.OccupyCells(n.occupyX, n.occupyY, n.occupyW, n.occupyH);

            Sprite sp = ResolveNodeSprite(n, out Color tint, out bool isFlat);
            if (sp == null)
            {
                nv.go.SetActive(false);
                if (nv.shadowGo != null) nv.shadowGo.SetActive(false);
                SyncStationedArtisanWorker(n);
                if (n.kind == NodeKind.Ranch) SyncRanchPenAnimals(n);
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
                AddExtraProp(nv, "barricade_b", n.gx - 1.1f, n.gy + 0.4f, false);
                AddExtraProp(nv, "fence", n.gx - 2.0f, n.gy + 1.0f, true);
                AddExtraProp(nv, "fence", n.gx + 1.3f, n.gy - 0.5f, false);
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

            SyncStationedArtisanWorker(n);
            if (n.kind == NodeKind.Ranch)
                SyncRanchPenAnimals(n);

            SyncSurvivorWalkers();
            if (Walkers.Count > 0 && Walkers[0].puppet != null)
                Walkers[0].puppet.SetSkinTier(Sim.MaxUnlockedSkinTier, autoFallbackToBase: true);
            RefreshWalkerDelegationStations();
            SyncCivilianVillagers();
        }

        // ---------------- 建筑旁驻点劳作工匠 NPC 与畜牧栏微缩圈养动物（零碰撞体积！） ----------------
        static bool TryResolveStationedWorkerConfig(CampNode n, out string stripKey, out Vector2 worldPos, out bool flipX, out bool onDockDeck)
        {
            stripKey = null;
            worldPos = Vector2.zero;
            flipX = false;
            onDockDeck = false;
            if (n == null) return false;

            switch (n.kind)
            {
                case NodeKind.RiverDock:
                    stripKey = "anim_work_fishing";
                    // 站在木栈钓台前缘甲板上向溪流甩竿垂钓
                    worldPos = (Vector2)ForestSliceView.Iso(n.gx, n.gy) + new Vector2(0.06f, -0.06f);
                    flipX = false;
                    onDockDeck = true;
                    return true;

                case NodeKind.Kitchen:
                    stripKey = "anim_work_cooking";
                    worldPos = ForestSliceView.Iso(n.gx + 0.85f, n.gy - 0.55f);
                    flipX = true;
                    return true;

                case NodeKind.Farm:
                    stripKey = "anim_work_farming";
                    worldPos = ForestSliceView.Iso(n.gx + 1.05f, n.gy - 0.85f);
                    flipX = true;
                    return true;

                case NodeKind.Sawmill:
                    stripKey = "anim_work_sawing";
                    worldPos = ForestSliceView.Iso(n.gx - 0.85f, n.gy - 0.75f);
                    flipX = false;
                    return true;

                case NodeKind.Smithy:
                    stripKey = "anim_work_forging";
                    worldPos = ForestSliceView.Iso(n.gx - 0.80f, n.gy - 0.65f);
                    flipX = false;
                    return true;

                case NodeKind.Tailor:
                    stripKey = "anim_work_herbs";
                    worldPos = ForestSliceView.Iso(n.gx - 0.60f, n.gy - 0.85f);
                    flipX = false;
                    return true;

                case NodeKind.HerbHut:
                    stripKey = "anim_work_herbs";
                    worldPos = ForestSliceView.Iso(n.gx - 0.60f, n.gy - 0.85f);
                    flipX = false;
                    return true;

                case NodeKind.RelicWell:
                    stripKey = "anim_work_herbs";
                    worldPos = ForestSliceView.Iso(n.gx + 0.65f, n.gy - 0.65f);
                    flipX = true;
                    return true;

                default:
                    return false;
            }
        }

        void SyncStationedArtisanWorker(CampNode n)
        {
            if (n == null || string.IsNullOrEmpty(n.id)) return;

            if (!TryResolveStationedWorkerConfig(n, out string stripKey, out Vector2 workWorldPos, out bool flipX, out bool onDockDeck))
                return;

            bool shouldExistAndShow = n.state == NodeState.Active && n.level >= 1;
            if (!StationedWorkers.TryGetValue(n.id, out StationedWorkerActor sw))
            {
                if (!shouldExistAndShow) return;

                int fallbackKind = 1 + (Mathf.Abs(n.id.GetHashCode()) % 3);
                Sprite fallbackSp = ForestArt.Get("villager_" + fallbackKind, () => ForestPlaceholderArt.Survivor(fallbackKind + 1));
                float phaseOff = (Mathf.Abs(n.id.GetHashCode()) % 17) * 0.23f;

                var fb = new ForestSpriteFlipbook(
                    stripKey,
                    frameCount: 3,
                    mode: FlipbookPlayMode.PingPongAll,
                    fps: 5.2f,
                    fallbackSprite: fallbackSp,
                    ppu: 256f,
                    pivotY: 0.08f,
                    phaseOffset: phaseOff);

                var go = new GameObject("StationedArtisan_" + n.id);
                go.transform.SetParent(_actorRoot, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.flipX = flipX;
                fb.ApplyTo(sr, 0f, moving: true);

                Vector2 wg = ForestSliceView.WorldToGrid(workWorldPos);
                GameObject shGo = View.CreateShadow(wg.x, wg.y, 0.48f, 0.44f);
                SpriteRenderer shSr = shGo != null ? shGo.GetComponent<SpriteRenderer>() : null;
                if (onDockDeck && shGo != null)
                    shGo.transform.SetParent(_actorRoot, false);

                sw = new StationedWorkerActor
                {
                    node = n,
                    stripKey = stripKey,
                    worldPos = workWorldPos,
                    baseScale = onDockDeck ? 0.72f : 0.88f,
                    onDockDeck = onDockDeck,
                    phase = phaseOff,
                    go = go,
                    sr = sr,
                    shadowGo = shGo,
                    shadowSr = shSr,
                    flipbook = fb,
                };
                StationedWorkers[n.id] = sw;
            }

            sw.node = n;
            sw.worldPos = workWorldPos;
            sw.onDockDeck = onDockDeck;

            bool visibleNow = shouldExistAndShow && !InWaveDefense && !(_fishingActive && n.kind == NodeKind.RiverDock);
            if (sw.go != null) sw.go.SetActive(visibleNow);
            if (sw.shadowGo != null) sw.shadowGo.SetActive(visibleNow);

            if (visibleNow && sw.go != null)
            {
                sw.sr.flipX = flipX;
                SyncStationedWorkerSortingAndTransform(sw, 1f, 1f);
            }
        }

        void SyncStationedWorkerSortingAndTransform(StationedWorkerActor sw, float scaleXMul, float scaleYMul)
        {
            Vector3 p = new Vector3(sw.worldPos.x, sw.worldPos.y, 0f);
            sw.go.transform.localPosition = p;
            sw.go.transform.localScale = new Vector3(sw.baseScale * scaleXMul, sw.baseScale * scaleYMul, 1f);

            if (sw.onDockDeck && sw.node != null && Visuals.TryGetValue(sw.node.id, out NodeVisual dockVis) && dockVis.sr != null)
            {
                int dockOrder = dockVis.sr.sortingOrder;
                sw.sr.sortingOrder = dockOrder + 6;
                if (sw.shadowGo != null)
                {
                    sw.shadowGo.transform.localPosition = p;
                    if (sw.shadowSr != null) sw.shadowSr.sortingOrder = dockOrder + 3;
                }
            }
            else
            {
                sw.sr.sortingOrder = Mathf.RoundToInt(-p.y * 100f) + 2;
                if (sw.shadowGo != null)
                    sw.shadowGo.transform.localPosition = p;
            }
        }

        void TickStationedWorkers(float dt)
        {
            foreach (var kv in StationedWorkers)
            {
                StationedWorkerActor sw = kv.Value;
                if (sw == null || sw.go == null || sw.node == null) continue;

                bool visibleNow = sw.node.state == NodeState.Active &&
                                  sw.node.level >= 1 &&
                                  !InWaveDefense &&
                                  !(_fishingActive && sw.node.kind == NodeKind.RiverDock);

                if (sw.go.activeSelf != visibleNow)
                    sw.go.SetActive(visibleNow);
                if (sw.shadowGo != null && sw.shadowGo.activeSelf != visibleNow)
                    sw.shadowGo.SetActive(visibleNow);
                if (!visibleNow) continue;

                sw.phase += dt;
                sw.flipbook?.ApplyTo(sw.sr, dt, moving: true);

                float pulse = Mathf.Sin(sw.phase * 5.2f);
                float bounceY = 1.0f + 0.02f * pulse;
                float squashX = 1.0f - 0.012f * pulse;
                SyncStationedWorkerSortingAndTransform(sw, squashX, bounceY);
            }
        }

        void SyncRanchPenAnimals(CampNode ranchNode)
        {
            if (ranchNode == null || ranchNode.kind != NodeKind.Ranch) return;
            bool active = ranchNode.state == NodeState.Active && ranchNode.level >= 1;

            if (active && RanchPenAnimals.Count == 0)
            {
                Vector2 ranchIso = ForestSliceView.Iso(ranchNode.gx, ranchNode.gy);
                string[] keys = { "anim_rabbit", "anim_boar", "anim_deer" };
                string[] fallbacks = { "boar", "boar", "deer" };
                float[] scales = { 0.44f, 0.47f, 0.51f };
                float[] fpsArr = { 6.2f, 5.4f, 5.2f };
                Vector2[] localSpots =
                {
                    new Vector2(-0.44f, -0.14f),
                    new Vector2(0.06f, -0.24f),
                    new Vector2(0.54f, -0.12f),
                };
                Vector2[] axes =
                {
                    new Vector2(0.92f, -0.39f).normalized,
                    new Vector2(1f, 0f),
                    new Vector2(0.92f, 0.39f).normalized,
                };

                for (int i = 0; i < 3; i++)
                {
                    string fbKey = fallbacks[i];
                    Sprite fbSp = ForestArt.Get(fbKey, () => ForestPlaceholderArt.Survivor(9));
                    var flip = new ForestSpriteFlipbook(
                        keys[i],
                        frameCount: 3,
                        mode: FlipbookPlayMode.PingPongAll,
                        fps: fpsArr[i],
                        fallbackSprite: fbSp,
                        ppu: 256f,
                        pivotY: 0.08f,
                        phaseOffset: i * 1.15f);

                    Vector2 spotW = ranchIso + localSpots[i];
                    Vector2 spotG = ForestSliceView.WorldToGrid(spotW);
                    var go = new GameObject("RanchPen_" + keys[i] + "_" + i);
                    go.transform.SetParent(_actorRoot, false);
                    go.transform.localPosition = new Vector3(spotW.x, spotW.y, 0f);
                    go.transform.localScale = new Vector3(scales[i], scales[i], 1f);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.flipX = i % 2 == 1;
                    flip.ApplyTo(sr, 0f, moving: true);
                    sr.sortingOrder = Mathf.RoundToInt(-spotW.y * 100f) + 3;

                    GameObject sh = View.CreateShadow(spotG.x, spotG.y, scales[i] * 0.68f, scales[i] * 0.58f);

                    RanchPenAnimals.Add(new RanchPenAnimalActor
                    {
                        ranchNode = ranchNode,
                        stripKey = keys[i],
                        anchorWorld = spotW,
                        patrolAxis = axes[i],
                        offset = 0f,
                        targetOffset = i % 2 == 0 ? 0.16f : -0.16f,
                        pauseTimer = 0.6f + i * 0.8f,
                        baseScale = scales[i],
                        phase = i * 1.9f,
                        go = go,
                        sr = sr,
                        shadowGo = sh,
                        flipbook = flip,
                    });
                }
            }

            Vector2 curRanchIso = ForestSliceView.Iso(ranchNode.gx, ranchNode.gy);
            Vector2[] spotsUpdate =
            {
                new Vector2(-0.44f, -0.14f),
                new Vector2(0.06f, -0.24f),
                new Vector2(0.54f, -0.12f),
            };
            bool show = active && !InWaveDefense;
            for (int i = 0; i < RanchPenAnimals.Count; i++)
            {
                RanchPenAnimalActor pa = RanchPenAnimals[i];
                pa.ranchNode = ranchNode;
                if (i < spotsUpdate.Length)
                    pa.anchorWorld = curRanchIso + spotsUpdate[i];
                if (pa.go != null) pa.go.SetActive(show);
                if (pa.shadowGo != null) pa.shadowGo.SetActive(show);
            }
        }

        void TickRanchPenAnimals(float dt)
        {
            for (int i = 0; i < RanchPenAnimals.Count; i++)
            {
                RanchPenAnimalActor pa = RanchPenAnimals[i];
                if (pa == null || pa.go == null || pa.ranchNode == null) continue;
                bool show = pa.ranchNode.state == NodeState.Active && pa.ranchNode.level >= 1 && !InWaveDefense;
                if (pa.go.activeSelf != show) pa.go.SetActive(show);
                if (pa.shadowGo != null && pa.shadowGo.activeSelf != show) pa.shadowGo.SetActive(show);
                if (!show) continue;

                pa.phase += dt;
                bool movingNow = false;
                if (pa.pauseTimer > 0f)
                {
                    pa.pauseTimer -= dt;
                    pa.flipbook?.ApplyTo(pa.sr, dt, moving: true, speedMul: 0.52f);
                    float br = Mathf.Sin(pa.phase * 3.4f) * 0.02f;
                    pa.go.transform.localScale = new Vector3(pa.baseScale * (1f - br * 0.5f), pa.baseScale * (1f + br), 1f);
                    if (pa.pauseTimer <= 0f)
                        pa.targetOffset = pa.offset >= 0f ? -0.16f : 0.16f;
                }
                else
                {
                    float prev = pa.offset;
                    pa.offset = Mathf.MoveTowards(pa.offset, pa.targetOffset, 0.22f * dt);
                    float dx = pa.patrolAxis.x * (pa.targetOffset - prev);
                    if (Mathf.Abs(dx) > 0.0001f)
                        pa.sr.flipX = dx > 0f;
                    movingNow = Mathf.Abs(pa.offset - prev) > 0.00005f;
                    if (Mathf.Abs(pa.offset - pa.targetOffset) < 0.008f)
                        pa.pauseTimer = 1.4f + (float)_rng.NextDouble() * 1.6f;

                    pa.flipbook?.ApplyTo(pa.sr, dt, moving: true, speedMul: 1.10f);
                    pa.go.transform.localScale = new Vector3(pa.baseScale, pa.baseScale, 1f);
                }

                Vector2 curW = pa.anchorWorld + pa.patrolAxis * pa.offset;
                float hop = movingNow ? Mathf.Abs(Mathf.Sin(pa.phase * 8.5f)) * 0.022f : 0f;
                pa.go.transform.localPosition = new Vector3(curW.x, curW.y + hop, 0f);
                pa.sr.sortingOrder = Mathf.RoundToInt(-curW.y * 100f) + 3;
                if (pa.shadowGo != null)
                    pa.shadowGo.transform.localPosition = new Vector3(curW.x, curW.y, 0f);
            }
        }

        void AddExtraProp(NodeVisual nv, string key, float gx, float gy, bool flip)
        {
            if (IsGridCoveredByForest(gx, gy)) return;
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

                case NodeKind.Ranch:
                    if (isGhost) tint = new Color(0.85f, 0.95f, 1f, 0.52f);
                    return ForestArt.Get("beast_ranch", () => ForestArt.Get("hunter_tower", ForestPlaceholderArt.Tent));

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

        // ---------------- 60FPS 多角色骨骼动画初始化与操控（Walkers[0]=领主阿澈，Walkers[1..]=6名幸存者骨干伙伴） ----------------
        ForestHeroPuppet _fishPuppet;

        void SyncSurvivorWalkers()
        {
            // Walkers[0] 永远是玩家直控的营地领主【阿澈】(ache)，出生在温馨起步草坪中央 (15.8, 18.4)！
            if (Walkers.Count == 0)
            {
                PuppetRigProfile acheProf = PuppetRegistry.GetProfile("ache");
                Vector2 acheHome = new Vector2(15.8f, 18.4f);
                Vector2 safeAcheW = ClampAndSlideWorldPos(ForestSliceView.Iso(acheHome.x, acheHome.y), ForestSliceView.Iso(acheHome.x, acheHome.y), 0.16f, 0.12f);
                Vector2 sc0 = ForestSliceView.WorldToGrid(safeAcheW);

                var achePuppet = new ForestHeroPuppet(_actorRoot, acheProf, "Leader_ache_0");
                achePuppet.SetSkinTier(Sim.MaxUnlockedSkinTier, autoFallbackToBase: true);
                achePuppet.SetFacing(true);
                GameObject sh0 = View.CreateShadow(sc0.x, sc0.y, 0.62f, 0.54f);

                var leader = new Walker
                {
                    index = 0,
                    heroId = "ache",
                    characterId = "ache",
                    gx = sc0.x, gy = sc0.y,
                    targetGx = sc0.x, targetGy = sc0.y,
                    patrolA = acheHome,
                    patrolB = acheHome,
                    headingToB = true,
                    pauseTimer = 2.0f,
                    phase = 0f,
                    tr = achePuppet.Root,
                    puppet = achePuppet,
                    shadowTr = sh0 != null ? sh0.transform : null,
                };

                BindWalkerPuppetEvents(leader, achePuppet);
                achePuppet.SyncWorldPos(ForestSliceView.Iso(leader.gx, leader.gy), leader.shadowTr);
                achePuppet.Tick(0.016f);
                Walkers.Add(leader);
            }

            // 收集已招募的幸存者骨干伙伴（大锤、小棠、雷猛、林砚、铁叔、老陈头，绝不混入阿澈！）
            var unlocked = new List<HeroSpec>();
            for (int i = 0; i < Sim.Heroes.Count; i++)
            {
                HeroSpec hs = Sim.Heroes[i];
                if (hs != null && hs.unlocked && hs.id != "ache")
                    unlocked.Add(hs);
            }

            // 伙伴初始巡逻路线全部严格落在起步开垦草甸 (12..20, 16..22) 内的平整空地上，绝不踏入未砍树林！
            Vector2[] compStarts =
            {
                new Vector2(14.3f, 19.3f), // 1: 大锤（供暖塔与议事厅前坪 A）
                new Vector2(17.8f, 19.2f), // 2: 小棠（物资仓库前坪 A）
                new Vector2(14.2f, 17.5f), // 3: 雷猛（西侧草坪哨位 A）
                new Vector2(17.0f, 20.0f), // 4: 林砚（议事厅东侧走廊 A）
                new Vector2(18.2f, 17.6f), // 5: 铁叔（东侧物资走廊 A）
                new Vector2(14.7f, 20.1f), // 6: 老陈头（议事厅西侧走廊 A）
            };
            Vector2[] compEnds =
            {
                new Vector2(15.0f, 18.8f), // 1: 大锤 B
                new Vector2(18.4f, 18.5f), // 2: 小棠 B
                new Vector2(14.9f, 16.9f), // 3: 雷猛 B
                new Vector2(17.7f, 19.5f), // 4: 林砚 B
                new Vector2(18.8f, 17.1f), // 5: 铁叔 B
                new Vector2(15.3f, 19.6f), // 6: 老陈头 B
            };

            for (int u = 0; u < unlocked.Count; u++)
            {
                HeroSpec spec = unlocked[u];
                bool alreadySpawned = false;
                for (int k = 1; k < Walkers.Count; k++)
                {
                    if (Walkers[k].heroId == spec.id)
                    {
                        alreadySpawned = true;
                        break;
                    }
                }
                if (alreadySpawned) continue;

                int idx = Walkers.Count;
                int slotIdx = Mathf.Max(0, idx - 1);
                string cid = string.IsNullOrEmpty(spec.characterId) ? spec.id : spec.characterId;
                PuppetRigProfile prof = ResolveCompanionRigProfile(spec.id, cid);
                Vector2 pA = slotIdx < compStarts.Length ? compStarts[slotIdx] : new Vector2(15.0f + (slotIdx % 3) * 1.1f, 18.2f);
                Vector2 pB = slotIdx < compEnds.Length ? compEnds[slotIdx] : pA + new Vector2(0.6f, -0.4f);
                Vector2 safeW = ClampAndSlideWorldPos(ForestSliceView.Iso(15.8f, 18.4f), ForestSliceView.Iso(pA.x, pA.y), 0.20f, 0.14f);
                Vector2 sc = ForestSliceView.WorldToGrid(safeW);

                var puppet = new ForestHeroPuppet(_actorRoot, prof, "Companion_" + spec.id + "_" + idx);
                puppet.SetSkinTier(spec.skinTier, autoFallbackToBase: true);
                puppet.SetFacing(idx % 2 == 1);
                GameObject sh = View.CreateShadow(sc.x, sc.y, 0.56f, 0.54f);

                var w = new Walker
                {
                    index = idx,
                    heroId = spec.id,
                    characterId = cid,
                    gx = sc.x, gy = sc.y,
                    targetGx = sc.x, targetGy = sc.y,
                    patrolA = pA,
                    patrolB = pB,
                    headingToB = true,
                    pauseTimer = 2.0f + idx * 1.2f,
                    phase = idx * 1.4f,
                    tr = puppet.Root,
                    puppet = puppet,
                    shadowTr = sh != null ? sh.transform : null,
                };

                BindWalkerPuppetEvents(w, puppet);
                puppet.SyncWorldPos(ForestSliceView.Iso(w.gx, w.gy), w.shadowTr);
                puppet.Tick(0.016f);
                Walkers.Add(w);
            }
        }

        void BindWalkerPuppetEvents(Walker w, ForestHeroPuppet puppet)
        {
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
        }

        static readonly Vector2[] VillagerWaypoints =
        {
            new Vector2(14.3f, 18.8f), // 供暖塔暖火旁
            new Vector2(17.8f, 18.8f), // 物资仓库前坪
            new Vector2(15.2f, 19.9f), // 议事厅前坪西
            new Vector2(16.9f, 19.8f), // 议事厅前坪东
            new Vector2(14.8f, 17.6f), // 起步草甸西南
            new Vector2(17.4f, 17.8f), // 起步草甸东南
            new Vector2(16.0f, 18.9f), // 起步草甸中心北
            new Vector2(16.3f, 17.6f), // 起步草甸中心南
        };

        static readonly string[] VillagerAmbientLines =
        {
            "供暖塔烧得真旺，山里夜风也不怕了！",
            "裁缝坊缝的新麻布衣真结实耐穿！",
            "仓库堆得满满当当，心里太踏实啦！",
            "炊事房炖的鲜蔬汤隔老远都闻着香！",
            "多亏阿澈领主收留，总算有个安稳家了！"
        };

        public void SyncCivilianVillagers()
        {
            int target = Sim.VisibleVillagerCount;
            while (CivilianVillagers.Count < target)
            {
                int idx = CivilianVillagers.Count;
                int kind = 1 + (idx % 3);
                int walkerStripIdx = idx % 3;
                Vector2 wp = VillagerWaypoints[idx % VillagerWaypoints.Length];
                Vector2 nextWp = VillagerWaypoints[(idx + 3) % VillagerWaypoints.Length];

                Sprite fallbackSp = ForestArt.Get("villager_" + kind, () => ForestPlaceholderArt.Survivor(kind + 1));
                var flip = new ForestSpriteFlipbook(
                    "anim_walker_" + walkerStripIdx,
                    frameCount: 4,
                    mode: FlipbookPlayMode.LoopAll,
                    fps: 7.5f,
                    fallbackSprite: fallbackSp,
                    ppu: 256f,
                    pivotY: 0.08f,
                    phaseOffset: idx * 0.31f);

                const float roadWalkerScale = 0.86f;
                var go = new GameObject($"CivilianVillager_{idx}_k{kind}");
                go.transform.SetParent(_actorRoot, false);
                go.transform.localScale = new Vector3(roadWalkerScale, roadWalkerScale, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                flip.ApplyTo(sr, 0f, moving: false);
                GameObject sh = View.CreateShadow(wp.x, wp.y, 0.50f, 0.46f);

                var v = new VillagerActor
                {
                    index = idx,
                    kindIdx = kind,
                    gx = wp.x + (idx % 2 == 0 ? 0.16f : -0.16f),
                    gy = wp.y + (idx % 3 == 0 ? 0.14f : -0.14f),
                    targetGx = nextWp.x,
                    targetGy = nextWp.y,
                    pauseTimer = 1.2f + (idx % 4) * 1.1f,
                    phase = idx * 1.7f,
                    baseScale = roadWalkerScale,
                    tr = go.transform,
                    sr = sr,
                    shadowTr = sh != null ? sh.transform : null,
                    flipbook = flip,
                };
                SyncActorTransform(v.tr, v.sr, v.shadowTr, v.gx, v.gy, 0f, 0f);
                CivilianVillagers.Add(v);
            }
        }

        public void RefreshWalkerDelegationStations()
        {
            for (int i = 1; i < Walkers.Count; i++)
            {
                Walker w = Walkers[i];
                DelegationPostKind? post = Sim.GetHeroAssignedPost(!string.IsNullOrEmpty(w.heroId) ? w.heroId : w.characterId);
                if (!post.HasValue) continue;

                string anchorId = "warehouse";
                switch (post.Value)
                {
                    case DelegationPostKind.WorkshopSpeed: anchorId = "sawmill"; break;
                    case DelegationPostKind.BuildSpeed: anchorId = "warehouse"; break;
                    case DelegationPostKind.FarmYield: anchorId = "farm_a"; break;
                    case DelegationPostKind.DefensePower: anchorId = "barricade"; break;
                    case DelegationPostKind.TruckArmor: anchorId = "relic_truck"; break;
                    case DelegationPostKind.CaravanBonus: anchorId = "radio_post"; break;
                }

                CampNode anchor = Sim.Find(anchorId);
                if (anchor != null && anchor.state == NodeState.Active)
                {
                    Vector2 candA = new Vector2(anchor.gx - 0.8f, anchor.gy - 0.8f);
                    Vector2 candB = new Vector2(anchor.gx + 0.6f, anchor.gy - 1.1f);
                    if (IsWalkableWorldPoint(ForestSliceView.Iso(candA.x, candA.y), 0.16f, 0.12f) &&
                        IsWalkableWorldPoint(ForestSliceView.Iso(candB.x, candB.y), 0.16f, 0.12f))
                    {
                        w.patrolA = candA;
                        w.patrolB = candB;
                        continue;
                    }
                }

                // 若目标工位建筑尚未砍开建造，稳妥留在起步营地中心草坪巡逻！
                w.patrolA = new Vector2(14.4f + (i % 3) * 1.5f, 18.9f - (i % 2) * 0.9f);
                w.patrolB = w.patrolA + new Vector2(0.65f, -0.45f);
            }
        }

        /// <summary>支持玩家用 WASD / 方向键直接操控主角阿澈在营地自由奔跑（带林缘边界切向滑动与建筑实体滑移）！</summary>
        public void DriveHeroByInput(Vector2 screenDir, float dt)
        {
            if (_fishingActive || InWaveDefense || Walkers.Count == 0 || screenDir.sqrMagnitude < 0.01f) return;
            Walker w = Walkers[0];
            w.hasManualMove = false;
            w.assignedTask = null;
            w.avoidSide = 0;

            Vector2 curWorld = ForestSliceView.Iso(w.gx, w.gy);
            Vector2 desiredWorld = curWorld + screenDir.normalized * (3.05f * dt);
            Vector2 clampedWorld = ClampAndSlideWorldPos(curWorld, desiredWorld, 0.08f, 0.05f);
            Vector2 nextGrid = ForestSliceView.WorldToGrid(clampedWorld);

            w.gx = nextGrid.x;
            w.gy = nextGrid.y;
            w.targetGx = w.gx;
            w.targetGy = w.gy;
            w.manualDriveHold = 0.06f;
            w.stuckTimer = 0f;

            if (Mathf.Abs(screenDir.x) > 0.02f)
                w.puppet.SetFacing(screenDir.x >= 0f);
            bool actuallyMoved = (clampedWorld - curWorld).sqrMagnitude > 0.000004f;
            w.puppet.SetMode(!actuallyMoved ? HeroAnimMode.Idle : (w.carryHomeTimer > 0f ? HeroAnimMode.CarryRun : HeroAnimMode.Run));
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

        bool IsNearAnyUncoveredOrActiveNode(float gx, float gy, float minDist)
        {
            float r2 = minDist * minDist;
            for (int i = 0; i < Sim.Nodes.Count; i++)
            {
                CampNode n = Sim.Nodes[i];
                if (!n.siteUncovered && n.state == NodeState.HiddenInForest) continue;
                float dx = gx - n.gx;
                float dy = gy - n.gy;
                if (dx * dx + dy * dy < r2) return true;
            }
            return false;
        }

        bool IsInPermanentCampTerritory(float gx, float gy)
        {
            if (gx >= 4.5f && gx <= 27.5f && gy >= 4.5f && gy <= 27.5f)
                return true;
            return IsNearAnyUncoveredOrActiveNode(gx, gy, 4.8f);
        }

        bool TryFindCleanPropSpot(FieldKind kind, Sprite sp, float scale, float prefGx, float prefGy, bool allowInnerMeadow, out float outGx, out float outGy, out Rect outVisRect)
        {
            const float visualPad = 0.22f;
            float margin = allowInnerMeadow ? 0.22f : 0.42f;
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

                    // 严禁在未砍开的密林中生成任何采集物！
                    if (IsGridCoveredByForest(gx, gy)) continue;

                    // 永久扩展营地领地内严禁长出再生松木！
                    if (kind == FieldKind.RegrownTimber && IsInPermanentCampTerritory(gx, gy)) continue;

                    // 随机再生的野果/废铁/原木禁止刷进起步核心草坪或紧贴已解锁建筑！
                    if (!allowInnerMeadow)
                    {
                        if (gx > 12.2f && gx < 20.8f && gy > 16.0f && gy < 22.8f) continue;
                        if (IsNearAnyUncoveredOrActiveNode(gx, gy, 4.8f)) continue;
                    }

                    Vector2 wPos = ForestSliceView.Iso(gx, gy);
                    if (!IsCleanForPropSpawn(wPos, gridMargin: margin, allowEdgeGrass: allowInnerMeadow)) continue;

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

        bool IsCleanForPropSpawn(Vector2 worldPos, float gridMargin = 0.45f, bool allowEdgeGrass = false)
        {
            Vector2 g = ForestSliceView.WorldToGrid(worldPos);
            int cx = Mathf.FloorToInt(g.x), cy = Mathf.FloorToInt(g.y);
            if (!View.Map.IsCleared(cx, cy) || ForestMap.RiverDistance(g.x, g.y) < 1.65f) return false;

            float backM = gridMargin;
            float frontM = allowEdgeGrass ? Mathf.Max(gridMargin, 0.28f) : Mathf.Max(gridMargin, 0.85f);
            if (!View.Map.IsCleared(Mathf.FloorToInt(g.x - frontM), Mathf.FloorToInt(g.y))) return false;
            if (!View.Map.IsCleared(Mathf.FloorToInt(g.x), Mathf.FloorToInt(g.y - frontM))) return false;
            if (!View.Map.IsCleared(Mathf.FloorToInt(g.x + backM), Mathf.FloorToInt(g.y + backM))) return false;
            return true;
        }

        static bool IsPhysicalRelicKind(NodeKind k)
        {
            return k == NodeKind.RelicWell ||
                   k == NodeKind.RelicLoot ||
                   k == NodeKind.RelicGenerator ||
                   k == NodeKind.RelicTruck ||
                   k == NodeKind.RelicRoadGate;
        }

        void GatherObstacles(List<ObstacleEllipse> list)
        {
            // 仅对真实立体的实体建筑底座与采集物核心建立紧凑脚底椭圆（平铺农田、未建半透明蓝图虚影、地面装饰矮树桩一律不阻挡行走！）
            list.Clear();
            for (int i = 0; i < Sim.Nodes.Count; i++)
            {
                CampNode n = Sim.Nodes[i];
                if (n.state == NodeState.HiddenInForest || n.state == NodeState.Depleted) continue;
                if (n.kind == NodeKind.Farm) continue;
                if (n.state == NodeState.Discovered && !IsPhysicalRelicKind(n.kind)) continue;

                Vector2 iso = ForestSliceView.Iso(n.gx, n.gy);
                ObstacleEllipse ob;
                switch (n.kind)
                {
                    case NodeKind.Shelter:
                    case NodeKind.Sawmill:
                    case NodeKind.Warehouse:
                        ob = new ObstacleEllipse { center = iso + new Vector2(0f, 0.10f), rx = 0.50f, ry = 0.25f };
                        break;
                    case NodeKind.Dwelling:
                    case NodeKind.Smithy:
                    case NodeKind.Tailor:
                    case NodeKind.HerbHut:
                    case NodeKind.Ranch:
                    case NodeKind.RadioPost:
                        ob = new ObstacleEllipse { center = iso + new Vector2(0f, 0.08f), rx = 0.44f, ry = 0.23f };
                        break;
                    case NodeKind.HeatTower:
                        ob = new ObstacleEllipse { center = iso + new Vector2(0f, 0.06f), rx = 0.36f, ry = 0.20f };
                        break;
                    case NodeKind.Kitchen:
                        ob = n.level >= 2
                            ? new ObstacleEllipse { center = iso + new Vector2(0f, 0.08f), rx = 0.46f, ry = 0.24f }
                            : new ObstacleEllipse { center = iso + new Vector2(0f, 0.02f), rx = 0.26f, ry = 0.16f };
                        break;
                    case NodeKind.RelicGenerator:
                    case NodeKind.RelicTruck:
                    case NodeKind.RelicRoadGate:
                        ob = new ObstacleEllipse { center = iso + new Vector2(0f, 0.08f), rx = 0.52f, ry = 0.26f };
                        break;
                    case NodeKind.RiverDock:
                        ob = new ObstacleEllipse { center = iso + new Vector2(0f, 0.02f), rx = 0.40f, ry = 0.20f };
                        break;
                    default:
                        ob = new ObstacleEllipse { center = iso + new Vector2(0f, 0.04f), rx = 0.34f, ry = 0.18f };
                        break;
                }
                list.Add(ob);
            }

            for (int i = 0; i < GroundPiles.Count; i++)
            {
                GroundPile p = GroundPiles[i];
                Vector2 iso = ForestSliceView.Iso(p.gx, p.gy);
                list.Add(new ObstacleEllipse { center = iso + new Vector2(0f, 0.02f), rx = 0.18f, ry = 0.11f });
            }
        }

        public bool IsInsideClearedBoundary(Vector2 worldPos, float gridMargin = 0.14f)
        {
            Vector2 g = ForestSliceView.WorldToGrid(worldPos);
            int cx = Mathf.FloorToInt(g.x), cy = Mathf.FloorToInt(g.y);
            if (!View.Map.IsCleared(cx, cy) || ForestMap.RiverDistance(g.x, g.y) < 1.18f) return false;

            // 彻底移除旧版 1.35 格远距离空气墙！仅保留 0.12~0.16 格贴边脚底净空，让主角可以直接走到每一排林缘树干旁！
            float m = Mathf.Clamp(gridMargin, 0.04f, 0.22f);
            if (!View.Map.IsCleared(Mathf.FloorToInt(g.x - m), cy)) return false;
            if (!View.Map.IsCleared(Mathf.FloorToInt(g.x + m), cy)) return false;
            if (!View.Map.IsCleared(cx, Mathf.FloorToInt(g.y - m))) return false;
            if (!View.Map.IsCleared(cx, Mathf.FloorToInt(g.y + m))) return false;
            return true;
        }

        public bool IsWalkableWorldPoint(Vector2 worldPos, float padX = 0.08f, float padY = 0.05f)
        {
            if (!IsInsideClearedBoundary(worldPos, 0.14f)) return false;
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
            Vector2 moveVec = desiredWorld - prevWorld;
            float stepDist = moveVec.magnitude;
            Vector2 p = desiredWorld;

            // 1. 林缘与河岸切向顺滑滑行：斜撞任何方向树林边缘时，沿等距网格轴或切向保留最大前向速度丝滑滑过！
            if (!IsInsideClearedBoundary(p, 0.14f))
            {
                Vector2 prevG = ForestSliceView.WorldToGrid(prevWorld);
                Vector2 desG = ForestSliceView.WorldToGrid(desiredWorld);
                Vector2 moveDir = stepDist > 0.0001f ? moveVec / stepDist : Vector2.right;

                Vector2 bestCand = prevWorld;
                float bestScore = -0.0001f;

                Vector2[] slideCands =
                {
                    ForestSliceView.Iso(desG.x, prevG.y),
                    ForestSliceView.Iso(prevG.x, desG.y),
                    new Vector2(desiredWorld.x, prevWorld.y),
                    new Vector2(prevWorld.x, desiredWorld.y),
                    prevWorld + RotateVec(moveDir, 28f) * stepDist,
                    prevWorld + RotateVec(moveDir, -28f) * stepDist,
                    prevWorld + RotateVec(moveDir, 56f) * stepDist,
                    prevWorld + RotateVec(moveDir, -56f) * stepDist,
                    prevWorld + RotateVec(moveDir, 82f) * (stepDist * 0.85f),
                    prevWorld + RotateVec(moveDir, -82f) * (stepDist * 0.85f),
                };

                for (int c = 0; c < slideCands.Length; c++)
                {
                    Vector2 cand = slideCands[c];
                    if (!IsInsideClearedBoundary(cand, 0.14f)) continue;
                    float score = Vector2.Dot(cand - prevWorld, moveDir);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestCand = cand;
                    }
                }
                p = bestCand;
            }

            // 2. 建筑与采集物底座椭圆：精确法线推出 + 沿椭圆切线补偿滑移，撞上任何建筑边缘都自动顺势滑开，绝不卡死！
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
                        float ux, uy;
                        if (d2 < 0.0001f)
                        {
                            ux = prevWorld.x >= ob.center.x ? 1f : -1f;
                            uy = 0f;
                        }
                        else
                        {
                            float inv = 1f / Mathf.Sqrt(d2);
                            ux = nx * inv;
                            uy = ny * inv;
                        }

                        // 椭圆外法线与表面最近点
                        Vector2 boundPt = ob.center + new Vector2(ux * rx, uy * ry);
                        if (iter == 0 && stepDist > 0.0001f)
                        {
                            Vector2 outNormal = new Vector2(ux / rx, uy / ry).normalized;
                            Vector2 tangent = new Vector2(-outNormal.y, outNormal.x);
                            float dot = Vector2.Dot(tangent, moveVec);
                            if (Mathf.Abs(dot) < stepDist * 0.12f)
                            {
                                // 正面顶撞建筑中心时按角色当前左右偏置自动择侧切向滑开
                                float side = (prevWorld.x - ob.center.x) >= 0f ? 1f : -1f;
                                if (tangent.x * side < 0f) tangent = -tangent;
                            }
                            else if (dot < 0f)
                            {
                                tangent = -tangent;
                            }

                            Vector2 slid = boundPt + tangent * (stepDist * 0.85f);
                            // 再次轻推回椭圆外侧 1.01 倍包络线上，保持圆润弧线滑行
                            float snx = (slid.x - ob.center.x) / rx;
                            float sny = (slid.y - ob.center.y) / ry;
                            float sd = Mathf.Sqrt(Mathf.Max(0.0001f, snx * snx + sny * sny));
                            if (sd < 1.005f)
                                slid = ob.center + new Vector2((snx / sd) * rx * 1.008f, (sny / sd) * ry * 1.008f);
                            p = slid;
                        }
                        else
                        {
                            p = ob.center + new Vector2(ux * rx * 1.008f, uy * ry * 1.008f);
                        }
                    }
                }
            }

            if (!IsInsideClearedBoundary(p, 0.10f) && IsInsideClearedBoundary(prevWorld, 0.10f))
                p = prevWorld;
            return p;
        }

        static Vector2 RotateVec(Vector2 v, float deg)
        {
            float rad = deg * Mathf.Deg2Rad;
            float cs = Mathf.Cos(rad), sn = Mathf.Sin(rad);
            return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
        }

        static readonly float[] FanAnglesDeg = { 18f, 36f, 54f, 72f, 92f, 114f, 136f };

        Vector2 StepWalkerAroundObstacles(Walker self, Vector2 curW, Vector2 goalW, float worldSpeed, float dt)
        {
            Vector2 toGoal = goalW - curW;
            float dist = toGoal.magnitude;
            if (dist < 0.001f) return curW;

            const float padX = 0.08f;
            const float padY = 0.05f;
            float stepLen = Mathf.Min(dist, worldSpeed * dt);
            Vector2 dir = toGoal / dist;

            // 1. 先测直线路径是否畅通：若前方短前瞻与本帧步长均无障碍，立即解除绕障侧记忆并直线高速奔向目标！
            float lookDist = Mathf.Min(dist, Mathf.Max(stepLen * 1.5f, 0.34f));
            Vector2 directStep = curW + dir * stepLen;
            Vector2 directProbe = curW + dir * lookDist;
            if (IsWalkableWorldPoint(directStep, padX, padY) && IsWalkableWorldPoint(directProbe, padX, padY))
            {
                self.avoidSide = 0;
                return ClampAndSlideWorldPos(curW, directStep, padX, padY);
            }

            // 2. 前方有建筑或林缘拐角阻挡：执行多角度扇形触须探测，并锁定同一绕行侧 (avoidSide)，彻底杜绝左右摇摆死锁！
            for (int a = 0; a < FanAnglesDeg.Length; a++)
            {
                float ang = FanAnglesDeg[a];
                int firstSign = self.avoidSide != 0 ? self.avoidSide : 1;

                Vector2 dirA = RotateVec(dir, ang * firstSign);
                Vector2 dirB = RotateVec(dir, -ang * firstSign);
                bool okA = IsWalkableWorldPoint(curW + dirA * stepLen, padX, padY) &&
                           IsWalkableWorldPoint(curW + dirA * Mathf.Min(0.32f, lookDist), padX, padY);
                bool okB = IsWalkableWorldPoint(curW + dirB * stepLen, padX, padY) &&
                           IsWalkableWorldPoint(curW + dirB * Mathf.Min(0.32f, lookDist), padX, padY);

                if (self.avoidSide != 0)
                {
                    if (okA) return ClampAndSlideWorldPos(curW, curW + dirA * stepLen, padX, padY);
                    if (okB)
                    {
                        self.avoidSide = -firstSign;
                        return ClampAndSlideWorldPos(curW, curW + dirB * stepLen, padX, padY);
                    }
                }
                else if (okA || okB)
                {
                    int chosenSign;
                    Vector2 chosenDir;
                    if (okA && okB)
                    {
                        float dA = Vector2.Distance(curW + dirA * lookDist, goalW);
                        float dB = Vector2.Distance(curW + dirB * lookDist, goalW);
                        if (dA <= dB) { chosenSign = firstSign; chosenDir = dirA; }
                        else { chosenSign = -firstSign; chosenDir = dirB; }
                    }
                    else if (okA)
                    {
                        chosenSign = firstSign;
                        chosenDir = dirA;
                    }
                    else
                    {
                        chosenSign = -firstSign;
                        chosenDir = dirB;
                    }
                    self.avoidSide = chosenSign;
                    return ClampAndSlideWorldPos(curW, curW + chosenDir * stepLen, padX, padY);
                }
            }

            return ClampAndSlideWorldPos(curW, directStep, padX, padY);
        }

        Vector2 GetChopStandWorldPos(Walker w, ChopTask task)
        {
            Vector2 targetW = ForestSliceView.Iso(task.gx, task.gy);
            Vector2 curW = ForestSliceView.Iso(w.gx, w.gy);
            if (task.isPile)
            {
                // 站在采集点/遗迹发现物的紧邻可行走外侧挥斧或拆解
                Vector2[] offsets =
                {
                    new Vector2(-0.42f, -0.02f),
                    new Vector2(0.42f, -0.02f),
                    new Vector2(0f, 0.28f),
                    new Vector2(0f, -0.28f),
                };
                Vector2 best = curW;
                float bestDist = float.MaxValue;
                bool found = false;
                for (int k = 0; k < offsets.Length; k++)
                {
                    Vector2 cand = targetW + offsets[k];
                    if (!IsInsideClearedBoundary(cand, 0.12f)) continue;
                    float d = Vector2.Distance(curW, cand);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = cand;
                        found = true;
                    }
                }
                if (found) return best;

                for (int s = 1; s <= 12; s++)
                {
                    Vector2 cand = Vector2.Lerp(targetW, curW, s / 12f);
                    if (IsInsideClearedBoundary(cand, 0.12f))
                        return cand;
                }
                return targetW;
            }
            else
            {
                // 砍伐外围树林区块：直接走到紧贴林缘树根处挥斧，不再停在老远之外！
                for (int s = 1; s <= 16; s++)
                {
                    float t = s / 16f;
                    Vector2 cand = Vector2.Lerp(targetW, curW, t);
                    if (IsWalkableWorldPoint(cand, 0.08f, 0.05f))
                        return cand;
                }
                return curW;
            }
        }

        void ResolveActorSeparation()
        {
            // 柔和肩宽防重叠（sepX=0.30, sepY=0.20）：主角阿澈 (index==0) 拥有最高路权，路过同伴时由同伴轻巧让步，绝不把主角弹开或挡停！
            const float sepX = 0.30f;
            const float sepY = 0.20f;

            for (int iter = 0; iter < 2; iter++)
            {
                for (int i = 0; i < Walkers.Count; i++)
                {
                    for (int j = i + 1; j < Walkers.Count; j++)
                    {
                        Walker anchor = Walkers[i];
                        Walker yielder = Walkers[j];
                        if (yielder.index == 0)
                        {
                            Walker tmp = anchor; anchor = yielder; yielder = tmp;
                        }
                        else if (anchor.index > 0)
                        {
                            bool aMoving = anchor.puppet.Mode == HeroAnimMode.Walk || anchor.puppet.Mode == HeroAnimMode.Run || anchor.puppet.Mode == HeroAnimMode.CarryRun;
                            bool bMoving = yielder.puppet.Mode == HeroAnimMode.Walk || yielder.puppet.Mode == HeroAnimMode.Run || yielder.puppet.Mode == HeroAnimMode.CarryRun;
                            if (aMoving && !bMoving)
                            {
                                Walker tmp = anchor; anchor = yielder; yielder = tmp;
                            }
                        }

                        Vector2 wa = ForestSliceView.Iso(anchor.gx, anchor.gy);
                        Vector2 wb = ForestSliceView.Iso(yielder.gx, yielder.gy);
                        float dx = wb.x - wa.x;
                        float dy = wb.y - wa.y;
                        float nx = dx / sepX;
                        float ny = dy / sepY;
                        float d2 = nx * nx + ny * ny;
                        if (d2 >= 1f) continue;

                        Vector2 pushOut;
                        if (d2 < 0.0001f)
                        {
                            pushOut = wa + new Vector2(sepX, 0f);
                        }
                        else
                        {
                            float inv = 1f / Mathf.Sqrt(d2);
                            pushOut = wa + new Vector2(nx * inv * sepX * 1.02f, ny * inv * sepY * 1.02f);
                        }

                        Vector2 resolved = ClampAndSlideWorldPos(wb, pushOut, 0.08f, 0.05f);
                        Vector2 gb = ForestSliceView.WorldToGrid(resolved);
                        yielder.gx = gb.x;
                        yielder.gy = gb.y;
                    }
                }
            }
        }

        Vector2 GetFishingDockGrid()
        {
            CampNode dock = Sim.Find("river_dock");
            return dock != null ? new Vector2(dock.gx, dock.gy) : new Vector2(13.2f, 5.2f);
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

            Vector2 dg = GetFishingDockGrid();
            View.Map.ClearRectInstant(Mathf.FloorToInt(dg.x) - 3, Mathf.FloorToInt(dg.y) - 3, Mathf.FloorToInt(dg.x) + 2, Mathf.FloorToInt(dg.y) + 2);
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
            Vector3 dockPos = ForestSliceView.Iso(dg.x, dg.y);
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
            Vector2 dg = GetFishingDockGrid();
            if (_bobberTr != null)
            {
                _bobberTr.gameObject.SetActive(true);
                _bobberTr.localPosition = ForestSliceView.Iso(dg.x - 0.85f, dg.y - 0.80f);
            }
            if (_fishShadowTr != null)
            {
                _fishShadowTr.gameObject.SetActive(true);
                _fishShadowTr.localPosition = ForestSliceView.Iso(dg.x - 1.65f, dg.y - 1.25f);
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
                Vector2 dg = GetFishingDockGrid();
                _leapFrom = ForestSliceView.Iso(dg.x - 0.85f, dg.y - 0.80f);
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
            float phaseOff = (float)_rng.NextDouble() * 6f;
            var flip = new ForestSpriteFlipbook(
                isDeer ? "anim_deer" : "anim_boar",
                frameCount: 3,
                mode: FlipbookPlayMode.PingPongAll,
                fps: 5.5f,
                fallbackSprite: sp,
                ppu: 256f,
                pivotY: 0.08f,
                phaseOffset: phaseOff);

            float sc = isDeer ? 0.92f : 0.88f;
            var go = new GameObject(isDeer ? "WildDeer" : "WildBoar");
            go.transform.SetParent(_actorRoot, false);
            go.transform.localScale = new Vector3(sc, sc, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            flip.ApplyTo(sr, 0f, moving: true);
            GameObject sh = View.CreateShadow(gx, gy, 0.58f, 0.55f);

            // 选取林缘安全往返切线（±0.45 世界单位漫步啃草）
            Vector2 baseW = ForestSliceView.Iso(gx, gy);
            Vector2[] candDirs =
            {
                new Vector2(0.894f, 0.447f),
                new Vector2(0.894f, -0.447f),
                new Vector2(1f, 0f),
            };
            Vector2 chosenTangent = candDirs[0];
            for (int d = 0; d < candDirs.Length; d++)
            {
                Vector2 td = candDirs[d];
                if (IsInsideClearedBoundary(baseW + td * 0.45f, 0.12f) &&
                    IsInsideClearedBoundary(baseW - td * 0.45f, 0.12f))
                {
                    chosenTangent = td;
                    break;
                }
            }

            var a = new WildActor
            {
                isZombie = false,
                label = isDeer ? "林间野鹿" : "山林野猪",
                gx = gx, gy = gy,
                anchorGrid = new Vector2(gx, gy),
                patrolTangentWorld = chosenTangent,
                patrolOffset = 0f,
                patrolTargetOffset = _rng.NextDouble() < 0.5 ? 0.45f : -0.45f,
                grazePauseTimer = 0.9f + (float)_rng.NextDouble() * 1.2f,
                baseScale = sc,
                targetGx = gx,
                targetGy = gy,
                timer = 28f,
                phase = phaseOff,
                tr = go.transform, sr = sr,
                shadowTr = sh != null ? sh.transform : null,
                flipbook = flip,
            };
            SyncActorTransform(a.tr, a.sr, a.shadowTr, a.gx, a.gy, 0f, 0f);
            WildActors.Add(a);
        }

        public void SpawnWanderingZombie(bool hordeMember = false)
        {
            if (!TryPickClearedEdgeCell(out float gx, out float gy)) return;
            bool kindB = _rng.NextDouble() < 0.5;
            Sprite sp = ForestArt.Get(kindB ? "zombie_1" : "zombie_0", () => ForestPlaceholderArt.Survivor(8));
            float phaseOff = (float)_rng.NextDouble() * 6f;
            var flip = new ForestSpriteFlipbook(
                kindB ? "anim_zom_armored" : "anim_zom_shambler",
                frameCount: 4,
                mode: FlipbookPlayMode.SplitMoveAttack,
                fps: kindB ? 6.0f : 6.5f,
                fallbackSprite: sp,
                ppu: 256f,
                pivotY: 0.08f,
                phaseOffset: phaseOff);

            const float zScale = 0.92f;
            var go = new GameObject("WanderingZombie");
            go.transform.SetParent(_actorRoot, false);
            go.transform.localScale = new Vector3(zScale, zScale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            flip.ApplyTo(sr, 0f, moving: true, attacking: false);
            GameObject sh = View.CreateShadow(gx, gy, 0.52f, 0.50f);

            CampNode fire = Sim.Find("kitchen");
            var a = new WildActor
            {
                isZombie = true,
                isHordeRaid = hordeMember,
                label = hordeMember ? "尸潮前锋" : "游荡丧尸",
                gx = gx, gy = gy,
                baseScale = zScale,
                targetGx = fire != null ? fire.gx : 14.8f,
                targetGy = fire != null ? fire.gy : 14.8f,
                timer = 35f,
                phase = phaseOff,
                tr = go.transform, sr = sr,
                shadowTr = sh != null ? sh.transform : null,
                flipbook = flip,
            };
            SyncActorTransform(a.tr, a.sr, a.shadowTr, a.gx, a.gy, 0f, 0f);
            WildActors.Add(a);
        }

        // ---------------- 独立策略战场兼容包装接口（统一委托至 ForestTacticalArena） ----------------
        public void StartHordeDefenseWave()
        {
            if (Arena == null) return;
            ActiveWaveSpec = Sim.GetWaveSpec(Sim.HordeWaveIndex);
            Arena.StartStageBattle(Sim.CurrentBattleStage, out _);
            SyncLegacySquadSlotsFromArena();
        }

        public void RetreatHordeDefenseWave()
        {
            Arena?.ExitArenaToCamp();
        }

        public void ToggleWaveAutoUlt()
        {
            if (Arena != null)
                Arena.AutoCastUlt = !Arena.AutoCastUlt;
        }

        public void ToggleWaveSpeed()
        {
            Arena?.ToggleSpeed();
        }

        public bool TryCastWaveHeroSkill(int heroIdx)
        {
            return Arena != null && Arena.TryCastHeroUlt(heroIdx);
        }

        public bool TriggerSquadUltimate(int slotIdx)
        {
            if (Arena == null || !Arena.IsBattleActive) return false;
            if (slotIdx >= 0 && slotIdx < SquadSlots.Count && SquadSlots[slotIdx].hero != null)
            {
                int hIdx = Sim.Heroes.IndexOf(SquadSlots[slotIdx].hero);
                if (hIdx >= 0) return Arena.TryCastHeroUlt(hIdx);
            }
            if (slotIdx >= 0 && slotIdx < Arena.HeroStates.Count)
                return Arena.TryCastHeroUlt(Arena.HeroStates[slotIdx].heroIdx);
            return false;
        }

        public void EndWaveDefense(bool victory)
        {
            Arena?.ExitArenaToCamp();
        }

        public void NotifyArenaWaveSettled(bool victory)
        {
            SyncLegacySquadSlotsFromArena();
            if (victory)
            {
                for (int i = 0; i < Walkers.Count; i++)
                    Walkers[i].cheerTimer = 1.8f;
            }
        }

        public void NotifyArenaExitedToCamp(bool victory)
        {
            SyncCivilianVillagers();
            for (int i = 0; i < CivilianVillagers.Count; i++)
            {
                if (CivilianVillagers[i].tr != null)
                {
                    CivilianVillagers[i].tr.gameObject.SetActive(true);
                    if (victory) CivilianVillagers[i].cheerTimer = 2.2f;
                }
            }
            RefreshWalkerDelegationStations();
            OnWaveDefenseEnded?.Invoke(victory, ActiveWaveSpec);
        }

        void SyncLegacySquadSlotsFromArena()
        {
            if (Arena == null || !Arena.IsBattleActive) return;

            BarricadeMaxHpCached = Mathf.Max(1f, Arena.TeamTotalMaxHp);
            BarricadeHp = Arena.TeamTotalHp;
            WaveTotalZombies = Arena.EnemiesTotal;
            WaveSpawnPending = Arena.EnemiesAliveAndQueued;
            WaveKilledZombies = Mathf.Max(0, Arena.EnemiesTotal - Arena.EnemiesAliveAndQueued);

            var hs = Arena.HeroStates;
            while (SquadSlots.Count > hs.Count)
                SquadSlots.RemoveAt(SquadSlots.Count - 1);
            while (SquadSlots.Count < hs.Count)
                SquadSlots.Add(new SquadCombatSlot());

            for (int i = 0; i < hs.Count; i++)
            {
                ArenaHeroHudState st = hs[i];
                SquadCombatSlot sl = SquadSlots[i];
                sl.hero = (st.heroIdx >= 0 && st.heroIdx < Sim.Heroes.Count) ? Sim.Heroes[st.heroIdx] : null;
                sl.rage = st.rage01 * 100f;
            }
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
            if (list.Count == 0) { gx = 13f; gy = 17f; return false; }
            Vector2Int c = list[_rng.Next(list.Count)];
            gx = c.x + 0.5f;
            gy = c.y + 0.5f;
            return true;
        }

        /// <summary>仅在最外圈未分配生态环带（gx &lt; 4.5 || gx &gt; 27.5 || gy &lt; 4.5 || gy &gt; 27.5 且远离所有已发现建筑）选点再生松木/野果！</summary>
        bool TryPickOuterPerimeterRegrowCell(out float gx, out float gy)
        {
            var list = new List<Vector2Int>();
            for (int x = 1; x < ForestMap.N - 1; x++)
            {
                for (int y = 1; y < ForestMap.N - 1; y++)
                {
                    float cx = x + 0.5f, cy = y + 0.5f;
                    if (IsInPermanentCampTerritory(cx, cy)) continue;
                    if (!View.Map.IsCleared(x, y) || IsGridCoveredByForest(cx, cy) || ForestMap.IsNearRiver(x, y)) continue;
                    if (!View.Map.IsCleared(x - 1, y) || !View.Map.IsCleared(x + 1, y) ||
                        !View.Map.IsCleared(x, y - 1) || !View.Map.IsCleared(x, y + 1))
                    {
                        if (IsWalkableWorldPoint(ForestSliceView.Iso(cx, cy), 0.16f, 0.12f))
                            list.Add(new Vector2Int(x, y));
                    }
                }
            }
            if (list.Count == 0) { gx = 0f; gy = 0f; return false; }
            Vector2Int pick = list[_rng.Next(list.Count)];
            gx = pick.x + 0.5f;
            gy = pick.y + 0.5f;
            return true;
        }

        /// <summary>在距离所有已发现/激活建筑 &gt;= minNodeDist 的远端已开垦林缘选点刷新散落木堆或野生浆果，绝不侵入营地广场！</summary>
        bool TryPickOuterResourceEdgeCell(float minNodeDist, out float gx, out float gy)
        {
            var list = new List<Vector2Int>();
            for (int x = 1; x < ForestMap.N - 1; x++)
            {
                for (int y = 1; y < ForestMap.N - 1; y++)
                {
                    float cx = x + 0.5f, cy = y + 0.5f;
                    if (!View.Map.IsCleared(x, y) || IsGridCoveredByForest(cx, cy) || ForestMap.IsNearRiver(x, y)) continue;
                    if (IsNearAnyUncoveredOrActiveNode(cx, cy, minNodeDist)) continue;
                    if (!View.Map.IsCleared(x - 1, y) || !View.Map.IsCleared(x + 1, y) ||
                        !View.Map.IsCleared(x, y - 1) || !View.Map.IsCleared(x, y + 1))
                    {
                        if (IsWalkableWorldPoint(ForestSliceView.Iso(cx, cy), 0.16f, 0.12f))
                            list.Add(new Vector2Int(x, y));
                    }
                }
            }
            if (list.Count == 0) { gx = 0f; gy = 0f; return false; }
            Vector2Int pick = list[_rng.Next(list.Count)];
            gx = pick.x + 0.5f;
            gy = pick.y + 0.5f;
            return true;
        }

        int ClearUncoveredNodeMarginsAfterChop(int choppedCx, int choppedCy)
        {
            int uncoveredCount = 0;
            for (int i = 0; i < Sim.Nodes.Count; i++)
            {
                CampNode n = Sim.Nodes[i];
                if (n.state != NodeState.HiddenInForest) continue;

                int nodeChunkX = Mathf.FloorToInt(n.gx) / ForestMap.ChunkSize;
                int nodeChunkY = Mathf.FloorToInt(n.gy) / ForestMap.ChunkSize;
                bool matchesChop = (n.revealChunkX == choppedCx && n.revealChunkY == choppedCy) ||
                                   (nodeChunkX == choppedCx && nodeChunkY == choppedCy);

                if (!matchesChop)
                {
                    int rx0 = n.revealChunkX * ForestMap.ChunkSize;
                    int ry0 = n.revealChunkY * ForestMap.ChunkSize;
                    int clearedInChunk = 0;
                    for (int x = rx0; x < rx0 + ForestMap.ChunkSize; x++)
                        for (int y = ry0; y < ry0 + ForestMap.ChunkSize; y++)
                            if (View.Map.IsCleared(x, y)) clearedInChunk++;

                    if (clearedInChunk >= 6 && View.Map.IsCleared(Mathf.FloorToInt(n.gx), Mathf.FloorToInt(n.gy)))
                        matchesChop = true;
                }

                if (!matchesChop) continue;

                EnsureNodePlotMarginCleared(n);
                uncoveredCount++;
            }
            return uncoveredCount;
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
                        Sim.AddXp(60, new Vector2(15.8f, 18.4f));
                        OnCustomToast?.Invoke("成功击退尸潮！获工程手稿+3 · 铜币+350 · 废铁+12！", new Vector2(15.8f, 18.4f));
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

        // ---------------- 采集 / 拆解 / 砍树结算（严格区分产出，支持遗迹发现物收集整合！） ----------------
        SurvRes FinishChopTask(ChopTask task)
        {
            SurvRes carried = SurvRes.Wood;

            if (task.isPile && task.pile != null)
            {
                GroundPile p = task.pile;
                Vector3 burstPos = ForestSliceView.Iso(p.gx, p.gy);
                string salvageId = p.salvageNodeId;

                if (p.go != null) DestroyGo(p.go);
                if (p.shadowGo != null) DestroyGo(p.shadowGo);
                GroundPiles.Remove(p);

                if (!string.IsNullOrEmpty(salvageId))
                {
                    CampNode sn = Sim.Find(salvageId);
                    carried = sn != null ? sn.salvageBonusRes : SurvRes.Wood;
                    View.BurstChips(burstPos, 18);
                    Sim.CompleteNodeSiteSalvage(salvageId);
                }
                else if (p.kind == FieldKind.BerryBush)
                {
                    carried = SurvRes.Veggie;
                    Sim.Add(SurvRes.Veggie, p.amount, true);
                    Sim.Add(SurvRes.Cloth, 2, true);
                    Sim.AwardRandomWildSeed(1, notify: true);
                    if (Sim.CampLevel >= 4) Sim.Add(SurvRes.Herb, 2, true);
                    Sim.AddXp(8, new Vector2(p.gx, p.gy));
                    View.BurstChips(burstPos, 12);
                }
                else if (p.kind == FieldKind.ScrapWreck)
                {
                    carried = SurvRes.Scrap;
                    Sim.Add(SurvRes.Scrap, p.amount, true);
                    Sim.ScrapSalvagedTotal += p.amount;
                    if (_rng.NextDouble() < 0.45)
                        Sim.Add(SurvRes.Parts, 1, true);
                    Sim.AddXp(10, new Vector2(p.gx, p.gy));
                    View.BurstChips(burstPos, 12);
                }
                else
                {
                    carried = SurvRes.Wood;
                    Sim.Add(SurvRes.Wood, p.amount, true);
                    Sim.AddXp(6, new Vector2(p.gx, p.gy));
                    View.BurstChips(burstPos, 12);
                }
            }
            else
            {
                if (View.ExecuteChopChunk(task.chunkX, task.chunkY))
                {
                    _chopsCount++;
                    if (Sim.Get(SurvRes.Ration) >= ForestSurvivalSim.ChopRationCost)
                        Sim.Add(SurvRes.Ration, -ForestSurvivalSim.ChopRationCost);

                    // 砍伐树林产出原木 + 保底掉落作物种子，中后期伴生采获野生草药！
                    Sim.Add(SurvRes.Wood, ForestSurvivalSim.ChopWoodGain, true);
                    Sim.AwardRandomWildSeed(1, notify: true);
                    if (Sim.CampLevel >= 4 && _rng.NextDouble() < 0.55)
                        Sim.Add(SurvRes.Herb, 2, true);
                    Sim.AddXp(10, new Vector2(task.gx, task.gy));

                    // 若本次砍伐揭露了新建筑区域，同步清理其地基四周 1 格余量确保地基利落无残留树干，并触发发现物生成！
                    int uncoveredNodes = ClearUncoveredNodeMarginsAfterChop(task.chunkX, task.chunkY);
                    Sim.SyncRevealedNodes(View.Map, silent: false);

                    // 仅当未揭露建筑遗迹且位于远离建筑的外围林缘时，才概率显露额外野生残骸/浆果丛
                    if (uncoveredNodes == 0 && !IsNearAnyUncoveredOrActiveNode(task.gx, task.gy, 5.2f))
                    {
                        double roll = _rng.NextDouble();
                        if (roll < 0.45 && CountFieldKind(FieldKind.ScrapWreck) < 3)
                        {
                            SpawnFieldNode(FieldKind.ScrapWreck, task.gx + 0.3f, task.gy - 0.3f, 6);
                            OnCustomToast?.Invoke("林下显露出旧山道【锈铁残骸】，可点击拆解废铁！", new Vector2(task.gx, task.gy));
                        }
                        else if (roll < 0.76 && CountFieldKind(FieldKind.BerryBush) < 3)
                        {
                            SpawnFieldNode(FieldKind.BerryBush, task.gx - 0.3f, task.gy + 0.2f, 3);
                        }
                    }
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

            // 永久领地不复林规则：已扩展营地腹地绝不长回任何树木！仅最外圈生态环或远离所有建筑 >=5.5 格的远端林缘稀疏刷新资源
            _berryRegrowTimer -= dt;
            if (_berryRegrowTimer <= 0f)
            {
                _berryRegrowTimer = 22f;
                if (CountFieldKind(FieldKind.BerryBush) < 2)
                {
                    if (TryPickOuterPerimeterRegrowCell(out float bx, out float by) ||
                        TryPickOuterResourceEdgeCell(5.5f, out bx, out by))
                    {
                        SpawnFieldNode(FieldKind.BerryBush, bx, by, 3);
                    }
                }
            }

            _timberRegrowTimer -= dt;
            if (_timberRegrowTimer <= 0f)
            {
                _timberRegrowTimer = 24f;
                if (CountFieldKind(FieldKind.RegrownTimber) < 2 && TryPickOuterPerimeterRegrowCell(out float tx, out float ty))
                {
                    SpawnFieldNode(FieldKind.RegrownTimber, tx, ty, 7);
                }
                else if (CountFieldKind(FieldKind.LogPile) < 2 && TryPickOuterResourceEdgeCell(5.5f, out float lx, out float ly))
                {
                    SpawnFieldNode(FieldKind.LogPile, lx, ly, 6);
                }
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
                    Vector2 dg = GetFishingDockGrid();
                    Vector3 baseBob = ForestSliceView.Iso(dg.x - 0.85f, dg.y - 0.80f);
                    float dip = FishBitingNow ? Mathf.Sin(_fishAnimTime * 22f) * 0.08f - 0.05f : Mathf.Sin(_fishAnimTime * 4f) * 0.025f;
                    _bobberTr.localPosition = baseBob + new Vector3(0f, dip, 0f);

                    if (_fishShadowTr != null && _fishShadowTr.gameObject.activeSelf)
                    {
                        Vector3 target = FishBitingNow ? baseBob + new Vector3(-0.08f, -0.04f, 0f) : ForestSliceView.Iso(dg.x - 1.35f + Mathf.Sin(_fishAnimTime * 1.8f) * 0.35f, dg.y - 1.0f);
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
                        Vector2 nextW = StepWalkerAroundObstacles(w, curW, standW, 2.65f, dt);
                        Vector2 moveDelta = nextW - curW;
                        if (Mathf.Abs(moveDelta.x) > 0.002f) w.puppet.SetFacing(moveDelta.x >= 0f);
                        Vector2 ng = ForestSliceView.WorldToGrid(nextW);
                        w.gx = ng.x;
                        w.gy = ng.y;
                        w.puppet.SetMode(HeroAnimMode.Run);

                        if (moveDelta.magnitude < 0.15f * dt)
                        {
                            w.stuckTimer += dt;
                            if (w.stuckTimer > 0.65f) task.chopProgress = 0.001f;
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
                                Vector2 homeStep = Vector2.MoveTowards(new Vector2(w.gx, w.gy), new Vector2(15.8f, 18.4f), 1.35f);
                                w.targetGx = homeStep.x;
                                w.targetGy = homeStep.y;
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

                if (w.manualDriveHold > 0f)
                {
                    w.manualDriveHold = Mathf.Max(0f, w.manualDriveHold - dt);
                    preTickGoals[i] = ForestSliceView.Iso(w.gx, w.gy);
                    continue;
                }

                Vector2 curWorld = ForestSliceView.Iso(w.gx, w.gy);
                Vector2 goalWorld = ForestSliceView.Iso(w.targetGx, w.targetGy);
                if (Vector2.Distance(curWorld, goalWorld) >= 0.06f && !IsWalkableWorldPoint(goalWorld, 0.08f, 0.05f))
                {
                    if (TryResolveWalkDestination(curWorld, goalWorld, out Vector2 resolvedGoal))
                    {
                        Vector2 rg = ForestSliceView.WorldToGrid(resolvedGoal);
                        w.targetGx = rg.x;
                        w.targetGy = rg.y;
                        goalWorld = resolvedGoal;
                    }
                }
                preTickGoals[i] = goalWorld;
                float wDist = Vector2.Distance(curWorld, goalWorld);
                if (wDist < 0.06f)
                {
                    w.hasManualMove = false;
                    w.avoidSide = 0;
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
                    float worldSpd = brisk ? 2.55f : 0.78f;
                    Vector2 nextWorld = StepWalkerAroundObstacles(w, curWorld, goalWorld, worldSpd, dt);
                    Vector2 stepDelta = nextWorld - curWorld;
                    if (Mathf.Abs(stepDelta.x) > 0.002f) w.puppet.SetFacing(stepDelta.x >= 0f);

                    Vector2 ng = ForestSliceView.WorldToGrid(nextWorld);
                    w.gx = ng.x;
                    w.gy = ng.y;
                    w.puppet.SetMode(w.carryHomeTimer > 0f ? HeroAnimMode.CarryRun : (w.hasManualMove ? HeroAnimMode.Run : HeroAnimMode.Walk));
                }
            }

            // 统一执行柔和肩宽分离，仅在真实世界位移近乎 0 时才判定阻滞（绝不误杀沿建筑弧线切向滑行的绕障动作！）
            ResolveActorSeparation();
            for (int i = 0; i < Walkers.Count; i++)
            {
                Walker w = Walkers[i];
                Vector2 postWorld = ForestSliceView.Iso(w.gx, w.gy);
                float actualSpeed = dt > 0.0001f ? Vector2.Distance(preTickWorlds[i], postWorld) / dt : 0f;

                HeroAnimMode curMode = w.puppet.Mode;
                if (w.manualDriveHold <= 0f && (curMode == HeroAnimMode.Walk || curMode == HeroAnimMode.Run || curMode == HeroAnimMode.CarryRun))
                {
                    if (actualSpeed < 0.10f)
                    {
                        w.stuckTimer += dt;
                        if (w.stuckTimer > 0.12f)
                            w.puppet.SetMode(w.cheerTimer > 0f ? HeroAnimMode.Cheer : HeroAnimMode.Idle);

                        if (w.stuckTimer > 0.90f)
                        {
                            w.stuckTimer = 0f;
                            w.avoidSide = 0;
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

            if (Walkers.Count > 0)
            {
                Vector2 hw = ForestSliceView.Iso(Walkers[0].gx, Walkers[0].gy);
                View.UpdateHeroCanopyFade(hw, dt);
                UpdateHeroBuildingFade(hw, dt);
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
                if (a.isHordeRaid && _hordeWaveRunning) continue;
                a.phase += dt;
                a.timer -= dt;
                if (a.timer <= 0f) { RemoveWildActor(a); continue; }

                bool actorMoving = false;
                if (!a.isZombie)
                {
                    // 林缘野生鹿/猪：围绕林缘锚点 ±0.45 世界单位轻盈往返漫步 + 停步低头啃草！
                    Vector2 baseW = ForestSliceView.Iso(a.anchorGrid.x, a.anchorGrid.y);
                    if (a.grazePauseTimer > 0f)
                    {
                        a.grazePauseTimer -= dt;
                        a.flipbook?.ApplyTo(a.sr, dt, moving: true, speedMul: 0.48f);
                        float br = Mathf.Sin(a.phase * 3.2f) * 0.015f;
                        a.tr.localScale = new Vector3(a.baseScale * (1f - br * 0.5f), a.baseScale * (1f + br), 1f);
                        if (a.grazePauseTimer <= 0f)
                            a.patrolTargetOffset = a.patrolOffset >= 0f ? -0.45f : 0.45f;
                    }
                    else
                    {
                        float prevOff = a.patrolOffset;
                        a.patrolOffset = Mathf.MoveTowards(a.patrolOffset, a.patrolTargetOffset, 0.36f * dt);
                        Vector2 candW = baseW + a.patrolTangentWorld * a.patrolOffset;
                        if (!IsInsideClearedBoundary(candW, 0.12f))
                        {
                            a.patrolOffset = prevOff;
                            a.grazePauseTimer = 1.8f + (float)_rng.NextDouble() * 1.5f;
                        }
                        else
                        {
                            float deltaX = a.patrolTangentWorld.x * (a.patrolTargetOffset - prevOff);
                            if (Mathf.Abs(deltaX) > 0.0001f)
                                a.sr.flipX = deltaX > 0f;
                            actorMoving = Mathf.Abs(a.patrolOffset - prevOff) > 0.00005f;
                            if (Mathf.Abs(a.patrolOffset - a.patrolTargetOffset) < 0.01f)
                                a.grazePauseTimer = 1.8f + (float)_rng.NextDouble() * 1.8f;
                        }
                        a.flipbook?.ApplyTo(a.sr, dt, moving: true, speedMul: actorMoving ? 1.10f : 0.48f);
                        a.tr.localScale = new Vector3(a.baseScale, a.baseScale, 1f);
                    }

                    Vector2 curGrid = ForestSliceView.WorldToGrid(baseW + a.patrolTangentWorld * a.patrolOffset);
                    a.gx = curGrid.x;
                    a.gy = curGrid.y;

                    if (Sim.HasWatchtower && a.timer < 20f)
                    {
                        ResolveWildActor(a, autoByTower: true);
                        continue;
                    }

                    float hop = actorMoving ? Mathf.Abs(Mathf.Sin(a.phase * 7.8f)) * 0.025f : 0f;
                    SyncActorTransform(a.tr, a.sr, a.shadowTr, a.gx, a.gy, hop, 0f);
                }
                else
                {
                    float dx = a.targetGx - a.gx, dy = a.targetGy - a.gy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float speed = a.isHordeRaid ? 0.68f : 0.42f;
                    if (dist > 0.1f)
                    {
                        float step = Mathf.Min(dist, dt * speed);
                        a.gx += dx / dist * step;
                        a.gy += dy / dist * step;
                        actorMoving = step > 0.0001f;
                        Vector3 sd = ForestSliceView.Iso(a.targetGx, a.targetGy) - ForestSliceView.Iso(a.gx, a.gy);
                        if (Mathf.Abs(sd.x) > 0.01f) a.sr.flipX = sd.x > 0f;
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

                    if ((Sim.HasWatchtower || Sim.HasBarricade) && dist < 2.8f)
                    {
                        ResolveWildActor(a, autoByTower: true);
                        continue;
                    }

                    a.flipbook?.ApplyTo(a.sr, dt, moving: actorMoving, attacking: false);
                    bool hasFlipFrames = a.flipbook != null && a.flipbook.IsLoaded;
                    float hop = Mathf.Abs(Mathf.Sin(a.phase * 5.5f)) * (hasFlipFrames ? 0.014f : 0.035f);
                    float tilt = Mathf.Sin(a.phase * 5.5f) * (hasFlipFrames ? 2.0f : 6f);
                    SyncActorTransform(a.tr, a.sr, a.shadowTr, a.gx, a.gy, hop, tilt);
                }
            }

            TickStationedWorkers(dt);
            TickRanchPenAnimals(dt);
            TickCivilianVillagers(dt);
            if (Arena != null && Arena.IsBattleActive)
            {
                Arena.Tick(dt);
                SyncLegacySquadSlotsFromArena();
            }
        }

        void TickCivilianVillagers(float dt)
        {
            SyncCivilianVillagers();
            if (InWaveDefense) return;

            for (int i = 0; i < CivilianVillagers.Count; i++)
            {
                VillagerActor v = CivilianVillagers[i];
                if (v.tr == null || !v.tr.gameObject.activeSelf) continue;
                v.phase += dt;
                if (v.chatterTimer > 0f) v.chatterTimer = Mathf.Max(0f, v.chatterTimer - dt);

                if (v.cheerTimer > 0f)
                {
                    v.cheerTimer = Mathf.Max(0f, v.cheerTimer - dt);
                    v.flipbook?.ApplyTo(v.sr, dt, moving: true, speedMul: 1.25f);
                    v.tr.localScale = new Vector3(v.baseScale, v.baseScale, 1f);
                    float jump = Mathf.Abs(Mathf.Sin(v.phase * 9.5f)) * 0.09f;
                    SyncActorTransform(v.tr, v.sr, v.shadowTr, v.gx, v.gy, jump, 0f);
                    continue;
                }

                if (v.pauseTimer > 0f)
                {
                    v.pauseTimer -= dt;
                    // 停驻烤火/歇息：稳落第 0 帧并做温和自然呼吸，绝不原地滑步！
                    v.flipbook?.ApplyTo(v.sr, dt, moving: false);
                    float br = Mathf.Sin(v.phase * 2.6f);
                    v.tr.localScale = new Vector3(v.baseScale * (1f - br * 0.008f), v.baseScale * (1f + br * 0.013f), 1f);
                    SyncActorTransform(v.tr, v.sr, v.shadowTr, v.gx, v.gy, 0f, 0f);
                    if (v.pauseTimer <= 0f)
                    {
                        Vector2 wp = VillagerWaypoints[(i + _rng.Next(1, VillagerWaypoints.Length)) % VillagerWaypoints.Length];
                        v.targetGx = wp.x + ((float)_rng.NextDouble() - 0.5f) * 0.5f;
                        v.targetGy = wp.y + ((float)_rng.NextDouble() - 0.5f) * 0.5f;
                        if (_rng.NextDouble() < 0.22)
                        {
                            v.chatterText = VillagerAmbientLines[_rng.Next(VillagerAmbientLines.Length)];
                            v.chatterTimer = 2.8f;
                        }
                    }
                    continue;
                }

                float dx = v.targetGx - v.gx, dy = v.targetGy - v.gy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > 0.12f)
                {
                    float step = Mathf.Min(dist, dt * 0.82f);
                    float nx = v.gx + dx / dist * step;
                    float ny = v.gy + dy / dist * step;
                    if (!IsInsideClearedBoundary(ForestSliceView.Iso(nx, ny), 0.14f))
                    {
                        v.pauseTimer = 1.6f;
                        v.flipbook?.ApplyTo(v.sr, dt, moving: false);
                        continue;
                    }
                    v.gx = nx;
                    v.gy = ny;
                    Vector3 sd = ForestSliceView.Iso(v.targetGx, v.targetGy) - ForestSliceView.Iso(v.gx, v.gy);
                    if (Mathf.Abs(sd.x) > 0.01f) v.sr.flipX = sd.x < 0f;
                    v.flipbook?.ApplyTo(v.sr, dt, moving: true);
                    bool hasWalkStrip = v.flipbook != null && v.flipbook.IsLoaded;
                    float bob = Mathf.Abs(Mathf.Sin(v.phase * 7.5f)) * (hasWalkStrip ? 0.015f : 0.032f);
                    float tilt = Mathf.Sin(v.phase * 7.5f) * (hasWalkStrip ? 1.1f : 2.6f);
                    v.tr.localScale = new Vector3(v.baseScale, v.baseScale, 1f);
                    SyncActorTransform(v.tr, v.sr, v.shadowTr, v.gx, v.gy, bob, tilt);
                }
                else
                {
                    v.pauseTimer = 2.6f + (float)_rng.NextDouble() * 3.0f;
                    v.flipbook?.ApplyTo(v.sr, dt, moving: false);
                }
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

        public bool QueueGroundPile(GroundPile p)
        {
            if (p == null || p.queued || !GroundPiles.Contains(p)) return false;
            HasPendingChop = false;
            p.queued = true;
            ChopQueue.Add(new ChopTask { isPile = true, pile = p, gx = p.gx, gy = p.gy });
            return true;
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
                if (CountFieldKind(FieldKind.BerryBush) == 0)
                {
                    if (TryPickOuterResourceEdgeCell(5.5f, out float bx, out float by))
                        SpawnFieldNode(FieldKind.BerryBush, bx, by, 3);
                    else
                        SpawnFieldNode(FieldKind.BerryBush, 12.6f, 20.6f, 3, allowInnerMeadow: true);
                }
                OnCustomToast?.Invoke("口粮不足！请先采摘林边【野生浆果丛】去营地炊事房烤制口粮！", new Vector2(15.8f, 18.4f));
                return;
            }

            float gx = (cx + 0.5f) * ForestMap.ChunkSize;
            float gy = (cy + 0.5f) * ForestMap.ChunkSize;
            ChopQueue.Add(new ChopTask { isPile = false, chunkX = cx, chunkY = cy, gx = gx, gy = gy });
        }

        // ---------------- 地面 / 幸存者 / 生态采集点 / 建筑 / 防误触二次确认砍树 点击分发 ----------------
        public void HandleWorldClick(Camera cam, Vector2 screenPos)
        {
            if (_fishingActive || InWaveDefense) return;
            Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));

            // 1. 优先精确点中营地外围猎物或游荡丧尸（含 ForestSliceView 常驻林缘巡游鹿猪）
            for (int i = 0; i < WildActors.Count; i++)
            {
                WildActor a = WildActors[i];
                Vector3 c = ForestSliceView.Iso(a.gx, a.gy) + new Vector3(0f, 0.26f, 0f);
                if (Vector2.Distance(wp, c) < 0.52f)
                {
                    HasPendingChop = false;
                    ResolveWildActor(a, autoByTower: false);
                    return;
                }
            }

            if (View != null && View.TryHuntEdgeWildlifeAtWorld(wp, 0.52f, out Vector2 huntedEdgeGrid))
            {
                HasPendingChop = false;
                bool archerBuff = Sim.FindHero("linyan")?.assigned == true;
                Sim.Add(SurvRes.Meat, 4 + (archerBuff ? 2 : 0), true);
                Sim.Add(SurvRes.Pelt, 3 + (archerBuff ? 1 : 0), true);
                Sim.AddXp(12, huntedEdgeGrid);
                return;
            }

            // 2. 精确点中头顶冒订单气泡的普通居民
            for (int i = 0; i < CivilianVillagers.Count; i++)
            {
                VillagerActor cv = CivilianVillagers[i];
                if (cv.tr == null || !cv.tr.gameObject.activeSelf) continue;
                Vector3 vc = ForestSliceView.Iso(cv.gx, cv.gy) + new Vector3(0f, 0.34f, 0f);
                if (Vector2.Distance(wp, vc) < 0.45f)
                {
                    HasPendingChop = false;
                    OnResidentDemandClicked?.Invoke();
                    return;
                }
            }

            // 3. 精确点中地面采集/拆解点（点选物品本体或头顶气泡均可立即派工采集！）
            for (int i = 0; i < GroundPiles.Count; i++)
            {
                GroundPile p = GroundPiles[i];
                if (p.queued) continue;
                Vector3 pc = ForestSliceView.Iso(p.gx, p.gy) + new Vector3(0f, 0.18f, 0f);
                if (Vector2.Distance(wp, pc) < 0.48f)
                {
                    QueueGroundPile(p);
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

            // 5. 若点击点落在已开垦的营地空地内 -> 100% 归属玩家操控主角移动 & 收起悬浮菜单，即便点在建筑底座边或树根边也自动吸附最近可行走点！
            Vector2 rawGrid = ForestSliceView.WorldToGrid(wp);
            if (View.Map.IsCleared(Mathf.FloorToInt(rawGrid.x), Mathf.FloorToInt(rawGrid.y)))
            {
                HasPendingChop = false;
                OnEmptyGroundClicked?.Invoke();
                if (Walkers.Count > 0)
                {
                    Walker hero = Walkers[0];
                    Vector2 heroWorld = ForestSliceView.Iso(hero.gx, hero.gy);
                    if (TryResolveWalkDestination(heroWorld, wp, out Vector2 safeWorld))
                    {
                        Vector2 clickGrid = ForestSliceView.WorldToGrid(safeWorld);
                        hero.targetGx = clickGrid.x;
                        hero.targetGy = clickGrid.y;
                        hero.hasManualMove = true;
                        hero.avoidSide = 0;
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
                    // 点选林缘树木时同步让空闲主角跑向该处树根边缘，既满足“走到树边”的直觉，又让二次确认砍树零等待挥斧！
                    if (Walkers.Count > 0 && Walkers[0].assignedTask == null)
                    {
                        Walker hero = Walkers[0];
                        Vector2 heroWorld = ForestSliceView.Iso(hero.gx, hero.gy);
                        if (TryResolveWalkDestination(heroWorld, wp, out Vector2 edgeStand))
                        {
                            Vector2 eg = ForestSliceView.WorldToGrid(edgeStand);
                            hero.targetGx = eg.x;
                            hero.targetGy = eg.y;
                            hero.hasManualMove = true;
                            hero.avoidSide = 0;
                            hero.stuckTimer = 0f;
                        }
                    }
                }
                return;
            }

            // 点在更外围树海或河岸边时，同样让空闲主角顺着点击方向一直跑到最近的林缘/河岸边！
            if (Walkers.Count > 0 && Walkers[0].assignedTask == null)
            {
                Walker hero = Walkers[0];
                Vector2 heroWorld = ForestSliceView.Iso(hero.gx, hero.gy);
                if (TryResolveWalkDestination(heroWorld, wp, out Vector2 edgeStand))
                {
                    Vector2 eg = ForestSliceView.WorldToGrid(edgeStand);
                    hero.targetGx = eg.x;
                    hero.targetGy = eg.y;
                    hero.hasManualMove = true;
                    hero.avoidSide = 0;
                    hero.stuckTimer = 0f;
                    View.BurstChips(edgeStand, 3);
                }
            }
            HasPendingChop = false;
        }

        bool TryResolveWalkDestination(Vector2 heroWorld, Vector2 clickWorld, out Vector2 outWorld)
        {
            const float padX = 0.08f;
            const float padY = 0.05f;

            if (IsWalkableWorldPoint(clickWorld, padX, padY))
            {
                outWorld = clickWorld;
                return true;
            }

            // 1. 若点在建筑脚底边缘或林缘树根线上，先在点击点周围小半径环内吸附最近可行走点
            float[] radii = { 0.14f, 0.28f, 0.44f, 0.64f };
            Vector2 toHero = (heroWorld - clickWorld);
            float baseAng = toHero.sqrMagnitude > 0.0001f ? Mathf.Atan2(toHero.y, toHero.x) : 0f;
            for (int rIdx = 0; rIdx < radii.Length; rIdx++)
            {
                float r = radii[rIdx];
                float bestScore = float.MaxValue;
                Vector2 bestPt = clickWorld;
                bool found = false;
                for (int s = 0; s < 12; s++)
                {
                    float ang = baseAng + s * (Mathf.PI * 2f / 12f);
                    Vector2 cand = clickWorld + new Vector2(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r);
                    if (!IsWalkableWorldPoint(cand, padX, padY)) continue;
                    float d = Vector2.Distance(cand, clickWorld) + Vector2.Distance(cand, heroWorld) * 0.08f;
                    if (d < bestScore)
                    {
                        bestScore = d;
                        bestPt = cand;
                        found = true;
                    }
                }
                if (found)
                {
                    outWorld = bestPt;
                    return true;
                }
            }

            // 2. 若点在远端密林或河道内，沿【点击点 -> 主角】连线回退找到最贴近林缘/河岸的可站立落点！
            for (int step = 1; step <= 28; step++)
            {
                float t = step / 28f;
                Vector2 cand = Vector2.Lerp(clickWorld, heroWorld, t);
                if (IsWalkableWorldPoint(cand, padX, padY))
                {
                    outWorld = cand;
                    return true;
                }
            }

            outWorld = heroWorld;
            return false;
        }

        void UpdateHeroBuildingFade(Vector2 heroWorld, float dt)
        {
            float k = Mathf.Clamp01(dt * 10f);
            foreach (var kv in Visuals)
            {
                NodeVisual nv = kv.Value;
                if (nv == null || nv.node == null || nv.sr == null || nv.go == null || !nv.go.activeSelf) continue;
                if (nv.node.kind == NodeKind.Farm) continue;
                bool isGhost = nv.node.state == NodeState.Discovered && !IsPhysicalRelicKind(nv.node.kind);
                if (isGhost) continue;

                Vector3 bp = nv.go.transform.localPosition;
                bool occludingHero = bp.y < heroWorld.y - 0.08f &&
                                     (heroWorld.y - bp.y) < 1.15f &&
                                     Mathf.Abs(bp.x - heroWorld.x) < 0.56f;
                float targetA = occludingHero ? 0.54f : 1f;
                Color c = nv.sr.color;
                if (Mathf.Abs(c.a - targetA) > 0.004f)
                {
                    c.a = Mathf.MoveTowards(c.a, targetA, k);
                    nv.sr.color = c;
                }
            }
        }

        static void DestroyGo(GameObject go)
        {
            if (Application.isPlaying) Object.Destroy(go);
            else Object.DestroyImmediate(go);
        }
    }
}
