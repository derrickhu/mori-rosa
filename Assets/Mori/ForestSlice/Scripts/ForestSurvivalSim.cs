using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mori.ForestSlice
{
    public enum SurvRes
    {
        // --- L1 原生态采集原料（占仓库储量） ---
        Wood = 0,          // 原木（林缘砍伐、再生松木、木艺坊巡林）
        Scrap = 1,         // 废铁（山道锈铁残骸、丧尸城汽修街、击毙丧尸）
        Water = 2,         // 山泉净水（净水石井、丧尸城超市）
        Veggie = 3,        // 山果鲜蔬（开荒菜地、林边浆果丛）
        Cloth = 4,         // 粗麻旧布（开荒菜麻田伴生、林间麻草、丧尸城超市）
        Herb = 5,          // 深山草药（林间药圃、砍林伴生采摘）
        Fish = 6,          // 山溪鲜鱼（河畔钓鱼台甩杆与鱼篓）
        Meat = 7,          // 野味鲜肉（捕猎木哨塔、林缘狩猎野鹿野猪）
        Pelt = 8,          // 野兽毛皮（捕猎木哨塔、狩猎野鹿野猪同源双掉！）

        // --- L2 重工与营房建材（锯木木艺坊 + 废铁锻造坊，占仓库储量） ---
        Plank = 9,         // 厚木板（锯木木艺坊 Lv.1：原木×2）
        WoodBed = 10,      // 防潮木床铺（锯木木艺坊 Lv.2：厚木板×2 + 粗麻旧布×1）
        IronBeam = 11,     // 包铁承重梁（锯木木艺坊 Lv.3：厚木板×2 + 铁钉铆件×2）
        Nails = 12,        // 铁钉铆件（废铁锻造坊 Lv.1：废铁×2，核心卡点件！）
        IronIngot = 13,    // 锻打铁锭（废铁锻造坊 Lv.2：废铁×3 + 原木×1）
        MetalPipe = 14,    // 导流金属管（废铁锻造坊 Lv.3：锻打铁锭×1 + 铁钉铆件×1）

        // --- L3 裁缝防具坊成品（居民穿戴订单 + 行商紧俏货 + 角色换装解锁） ---
        LinenShirt = 15,   // 耐磨麻布衣（裁缝防具坊 Lv.1：粗麻旧布×2）
        LeatherBoots = 16, // 防荆棘猎皮靴（裁缝防具坊 Lv.2：野兽毛皮×2 + 粗麻旧布×1）
        FurCoat = 17,      // 加绒御寒皮衣（裁缝防具坊 Lv.3：野兽毛皮×2 + 耐磨麻布衣×1 + 铁钉铆件×1）
        PlateArmor = 18,   // 废土钢板护甲（裁缝防具坊 Lv.4：加绒御寒皮衣×1 + 锻打铁锭×2）

        // --- L3 野味炊事房料理（居民餐饮订单 + 自动同步增加总口粮体力） ---
        RoastVeggie = 19,  // 炭烤山果串（野味炊事房 Lv.1：山果鲜蔬×2）
        VeggieSoup = 20,   // 山泉鲜蔬汤（野味炊事房 Lv.1：山果鲜蔬×2 + 山泉净水×1）
        GrillFish = 21,    // 香烤山溪鱼（野味炊事房 Lv.2：山溪鲜鱼×2 + 原木×1）
        SmokedMeat = 22,   // 松木秘制熏肉（野味炊事房 Lv.3：野味鲜肉×2 + 原木×1）

        // --- L3 山林草药坊制剂（高倍经验伤病订单 + 行商王牌货） ---
        HerbPoultice = 23, // 止血草药敷包（山林草药坊 Lv.1：深山草药×2 + 粗麻旧布×1）
        WarmTea = 24,      // 驱寒草本热饮（山林草药坊 Lv.2：深山草药×2 + 山泉净水×2）
        MedKit = 25,       // 抗尸毒急救箱（山林草药坊 Lv.3：止血草药敷包×1 + 医用电子件×1 + 锻打铁锭×1）

        // --- 战略通货与综合体力（不占基础仓库容量） ---
        Ration = 26,       // 熟食口粮体力（砍树与进城消耗，烹饪任意菜品同步增加）
        Parts = 27,        // 医用/电子件（丧尸城药房/防卫总库、密林通信残骸）
        Blueprint = 28,    // 工程手稿（中后期建筑升阶核心蓝图，产自进城搜刮/尸潮/行商/升星）
    }

    public enum NodeKind
    {
        Shelter,          // ① 避难所议事厅（营地中枢）
        HeatTower,        // ② 篝火供暖塔（决定【供暖人口上限 MAX】！）
        Dwelling,         // ③ 林间避风民居 1~3号（消耗人口上限，提升【当前实有人口】！）
        Warehouse,        // ④ 营地物资仓库（强依赖当前人口，提升【原料储量上限】！）
        Sawmill,          // ⑤ 锯木木艺坊（厚木板 / 防潮木床铺 / 包铁承重梁）
        Smithy,           // ⑥ 废铁锻造坊（铁钉铆件 / 锻打铁锭 / 导流金属管）
        Tailor,           // ⑦ 裁缝防具坊（麻布衣 / 猎皮靴 / 御寒皮衣 / 钢板护甲 -> 居民穿戴订单+换装）
        Kitchen,          // ⑧ 野味炊事房（烤山果串 / 鲜蔬汤 / 香烤鱼 / 松木熏肉）
        HerbHut,          // ⑨ 山林草药坊（止血敷包 / 驱寒热饮 / 抗尸毒急救箱）
        RelicWell,        // ⑩ 净水石井（持续产出山泉净水）
        Farm,             // ⑪ 开荒菜麻田 & 药圃（产出山果鲜蔬 + 粗麻旧布 / 深山草药）
        RiverDock,        // ⑫ 河畔钓鱼台（产出山溪鲜鱼）
        Watchtower,       // ⑬ 捕猎木哨塔（自动射猎产出野味鲜肉 + 野兽毛皮，协防丧尸）
        Barricade,        // ⑭ 尖木拒马防线（拦截偷粮丧尸与尸潮）
        RelicGenerator,   // ⑮ 山涧发电房（全作坊队列电力提速 +25%~+65%）
        RadioPost,        // ⑯ 短波电台·山口行商驿站（大宗出售服装/医药/熏肉换铜币与工程手稿）
        RelicTruck,       // ⑰ 末日武装皮卡（赴丧尸城撞尸搜刮手稿/废铁/零件）
        RelicRoadGate,    // ⑱ 盘山公路关卡
        RelicLoot,        // 一次性密林废墟箱（拆解得废铁/零件/工程手稿）
    }

    public enum NodeState
    {
        HiddenInForest = 0,
        Discovered = 1,
        Active = 2,
        Depleted = 3,
        Constructing = 4,
        ReadyToHammer = 5,
    }

    public enum FarmPhase
    {
        DryTilled = 0,
        Growing = 1,
        Ripe = 2,
    }

    public enum OrderCategory
    {
        Food = 0,      // 温饱餐饮单
        Clothing = 1,  // 御寒穿戴单
        Medical = 2,   // 防疫伤病单（高经验）
        Housing = 3,   // 营房安居单（概率掉手稿）
    }

    public enum CondKind
    {
        PopCur = 0,    // 当前人口 >= need
        PopCap = 1,    // 供暖人口上限 >= need
        Coins = 2,     // 营地铜币 >= need
        Resource = 3,  // 指定物品库存 >= need
    }

    public struct BuildCondition
    {
        public CondKind kind;
        public SurvRes res;
        public int own;
        public int need;
        public bool met => own >= need;
    }

    public sealed class CostPair
    {
        public SurvRes res;
        public int amount;
        public CostPair(SurvRes r, int a) { res = r; amount = a; }
    }

    public sealed class WorkshopRecipe
    {
        public string id;
        public string name;
        public string iconKey;
        public string sourceHint;
        public int unlockBuildingLv = 1;
        public int unlockCampLv = 1;
        public SurvRes inRes1;
        public int inAmt1;
        public SurvRes inRes2;
        public int inAmt2;
        public SurvRes inRes3;
        public int inAmt3;
        public SurvRes outRes;
        public int outAmt;
        public int bonusRation; // 炊事料理同步折算的综合体力口粮
        public float duration;
    }

    public sealed class QueueItem
    {
        public WorkshopRecipe recipe;
        public float timer;
        public float duration;
    }

    public sealed class ResidentDemand
    {
        public int slotIndex;
        public OrderCategory category;
        public int walkerIndex;
        public string speakerName;
        public string portraitKey;
        public string categoryTag;
        public string complaint;
        public string thanksLine;
        public SurvRes res1;
        public int amt1;
        public SurvRes res2;
        public int amt2;
        public int xpReward;
        public int coinReward;
        public int blueprintBonus;
    }

    public sealed class CaravanOffer
    {
        public string title;
        public SurvRes giveRes;
        public int giveAmt;
        public int coinGain;
        public int blueprintGain;
        public bool traded;
    }

    public sealed class HeroSpec
    {
        public string id;
        public string characterId;
        public string name;
        public string roleTitle;
        public string portraitKey;
        public string buffDesc;
        public bool unlocked;
        public bool assigned;
        public string targetBuildingTitle;
        public OutfitTier skinTier = OutfitTier.Refugee;
    }

    public sealed class ComicLine
    {
        public string leftBust;
        public string rightBust;
        public bool rightSpeaking;
        public string speaker;
        public string text;
    }

    public sealed class CampNode
    {
        public string id;
        public NodeKind kind;
        public NodeState state;
        public int level;
        public int maxLevel = 4;
        public int unlockPlayerLv = 1;

        public string hintLabel;
        public string ruinTitle;
        public string activeTitle;
        public string desc;
        public string benefitLabel;
        public int benefitFrom;
        public int benefitTo;
        public SurvRes benefitRes;

        public float gx, gy;
        public int occupyX, occupyY;
        public int occupyW = 1, occupyH = 1;
        public int revealChunkX, revealChunkY;

        public int requireShelterLv = 1;
        public bool requireGenerator;
        public bool isHerbPlot; // 区分 1号菜麻田 vs 2号草药田

        public CostPair[] buildCosts;
        public int buildCoinCost;
        public int buildReqPop;
        public int buildAddPop;

        public float prodInterval;
        public float prodTimer;
        public SurvRes prodOutRes;
        public int prodOutCount;
        public SurvRes prodOutRes2;
        public int prodOutCount2;
        public SurvRes prodInRes;
        public int prodInCount;

        public SurvRes storedRes;
        public int storedAmount;
        public int maxStored = 16;

        public bool isUpgradingExisting;
        public float buildTimer;
        public float buildDuration = 3.5f;

        public readonly List<QueueItem> queue = new List<QueueItem>();

        public FarmPhase farmPhase;
        public float farmTimer;
    }

    public sealed class CitySiteDef
    {
        public string id;
        public string name;
        public string spriteKey;
        public string dangerTag;
        public string lootSummary;
        public int rationCost;
        public int requireShelterLv;
        public SurvRes mainRes;
        public int mainMin, mainMax;
        public SurvRes subRes;
        public int subMin, subMax;
        public SurvRes bonusRes;
        public int bonusChancePct;
        public int bonusCount;
        public int blueprintDrop;
    }

    /// <summary>
    /// 《深山末日开荒》V2 核心数值与双轮驱动经济状态机：
    /// 1. 双轨人口门控：【篝火供暖塔】扩充供暖人口上限 MAX -> 【1~3号林间民居】消耗上限提升当前实有人口 -> 当前人口卡仓库与五大作坊升级 + 自动收菜！
    /// 2. 仓库储量硬约束：【物资仓库】决定基础原料与建材储量上限 (80->180->360->700->1200)
    /// 3. 五大特色生产作坊：【锯木木艺坊】、【废铁锻造坊】、【裁缝防具坊（服装护甲）】、【野味炊事房】、【山林草药坊】
    /// 4. 四维居民生计订单（餐饮/穿戴/伤病/安居）+【短波电台·山口行商驿站】产出铜币、星级经验与工程手稿！
    /// </summary>
    public sealed class ForestSurvivalSim
    {
        public const int ChopRationCost = 1;
        public const int ChopWoodGain = 8;

        public const int MinPlayableChunk = 1;
        public const int MaxPlayableChunk = 9;

        readonly Dictionary<SurvRes, int> _res = new Dictionary<SurvRes, int>();
        readonly HashSet<SurvRes> _revealedRes = new HashSet<SurvRes>();

        public readonly List<CampNode> Nodes = new List<CampNode>();
        public readonly CitySiteDef[] CitySites;
        public readonly WorkshopRecipe[] SawmillRecipes;
        public readonly WorkshopRecipe[] SmithyRecipes;
        public readonly WorkshopRecipe[] TailorRecipes;
        public readonly WorkshopRecipe[] KitchenRecipes;
        public readonly WorkshopRecipe[] HerbHutRecipes;
        public readonly List<HeroSpec> Heroes = new List<HeroSpec>();
        public readonly List<ResidentDemand> ActiveOrders = new List<ResidentDemand>();
        public readonly List<CaravanOffer> CaravanOffers = new List<CaravanOffer>();

        public int CampLevel = 1;
        public int CampXp = 0;
        public int Coins = 120;

        public ResidentDemand ActiveDemand => ActiveOrders.Count > 0 ? ActiveOrders[0] : null;
        public int SatisfiedToday;
        public const int SatisfiedGoal = 5;
        public float OrderRefreshCooldown;
        public float CaravanRefreshTimer = 90f;
        int _demandSeq;

        public bool HordeWarningTriggered;
        public bool HordePosterShown;
        public bool HordeRepelled;
        public float HordeTimerSec = 1799f;

        public int QuestIndex;
        public bool QuestRewardPending;
        public int PlanksCraftedTotal;
        public int RationsCookedTotal;
        public int ClothesCraftedTotal;
        public int FurCoatsCraftedTotal;
        public int PlateArmorsCraftedTotal;
        public int ScrapSalvagedTotal;
        public int FishCaughtTotal;
        public int CityRunsCompleted;
        float _patrolWoodTimer;
        readonly HashSet<string> _storyFlags = new HashSet<string>();

        public event Action<string, Vector2> OnWorldToast;
        public event Action<SurvRes, int> OnResourceGained;
        public event Action<int, Vector2> OnXpBurst;
        public event Action<int, string[]> OnCampLevelUp;
        public event Action<CampNode> OnNodeVisualChanged;
        public event Action<CampNode> OnNodeHammeredComplete;
        public event Action<HeroSpec> OnHeroUnlocked;
        public event Action<HeroSpec, OutfitTier> OnHeroSkinChanged;
        public event Action OnHordeCrisisTriggered;
        public event Action<ComicLine[]> OnPlayComicDialog;

        // ---------------- 双轨人口 & 仓库储量上限计算 ----------------
        /// <summary>当前营地实有入驻难民人口（由 1~3 号民居提供，用于解锁作坊/仓库升级与自动收菜）。</summary>
        public int CurrentPop
        {
            get
            {
                int p = 0;
                for (int i = 0; i < Nodes.Count; i++)
                {
                    CampNode n = Nodes[i];
                    if (n.kind == NodeKind.Dwelling && n.state == NodeState.Active && n.level > 0)
                        p += GetDwellingPopForLevel(n.id, n.level);
                }
                return Mathf.Max(4, p);
            }
        }

        /// <summary>顶栏展示用当前人口（与 CurrentPop 一致）。</summary>
        public int SurvivorsCount => CurrentPop;

        /// <summary>当前营地供暖承载人口上限 MAX（由【篝火供暖塔】与【避难所议事厅】共同决定）。</summary>
        public int PopCap
        {
            get
            {
                int heatLv = GetNodeLevel("heat_tower", 1);
                int baseCap = heatLv >= 4 ? 38 : (heatLv == 3 ? 26 : (heatLv == 2 ? 15 : 8));
                int shLv = ShelterLevel;
                int shBonus = shLv >= 3 ? 8 : (shLv == 2 ? 3 : 0);
                return baseCap + shBonus;
            }
        }

        /// <summary>当前营地原料与加工件单品储量上限（由【物资仓库】等级决定）。</summary>
        public int StorageCap
        {
            get
            {
                int wLv = GetNodeLevel("warehouse", 1);
                switch (wLv)
                {
                    case 1: return 80;
                    case 2: return 180;
                    case 3: return 360;
                    case 4: return 700;
                    default: return 1200;
                }
            }
        }

        public static bool IsWarehouseCapped(SurvRes r)
        {
            return r != SurvRes.Ration && r != SurvRes.Blueprint && r != SurvRes.Parts;
        }

        public int CurrentMaxSingleStock
        {
            get
            {
                int m = 0;
                foreach (var kv in _res)
                    if (IsWarehouseCapped(kv.Key) && kv.Value > m) m = kv.Value;
                return m;
            }
        }

        public static int GetDwellingPopForLevel(string id, int lv)
        {
            if (lv <= 0) return 0;
            if (id == "house_1")
            {
                int[] v = { 0, 4, 7, 10, 14 };
                return v[Mathf.Clamp(lv, 0, v.Length - 1)];
            }
            if (id == "house_2")
            {
                int[] v = { 0, 5, 8, 12, 16 };
                return v[Mathf.Clamp(lv, 0, v.Length - 1)];
            }
            int[] v3 = { 0, 8, 12, 16, 20 };
            return v3[Mathf.Clamp(lv, 0, v3.Length - 1)];
        }

        public int GetNodeLevel(string id, int fallback = 0)
        {
            CampNode n = Find(id);
            if (n == null || n.state == NodeState.HiddenInForest || n.state == NodeState.Discovered) return fallback;
            return Mathf.Max(fallback, n.level);
        }

        public int ShelterLevel => GetNodeLevel("shelter", 1);

        // 角色防具皮肤：既可通过【裁缝防具坊】缝制御寒皮衣/钢板护甲解锁，也可随避难所中枢升级保底解锁！
        public OutfitTier MaxUnlockedSkinTier
        {
            get
            {
                if (PlateArmorsCraftedTotal > 0 || ShelterLevel >= 3) return OutfitTier.Armored;
                if (FurCoatsCraftedTotal > 0 || ShelterLevel >= 2) return OutfitTier.Reinforced;
                return OutfitTier.Refugee;
            }
        }

        public bool SetHeroSkinTier(string heroId, OutfitTier targetTier, out string msg)
        {
            HeroSpec h = FindHero(heroId);
            if (h == null) { msg = "未找到骨干伙伴"; return false; }
            OutfitTier maxT = MaxUnlockedSkinTier;
            if (targetTier > maxT)
            {
                msg = targetTier == OutfitTier.Reinforced
                    ? "需在【裁缝防具坊】缝制【加绒御寒皮衣】（或避难所升至 Lv.2）解锁此装束！"
                    : "需在【裁缝防具坊】打造【废土钢板护甲】（或避难所升至 Lv.3）解锁此重甲！";
                return false;
            }
            h.skinTier = targetTier;
            h.portraitKey = PuppetRegistry.ResolveBustKey(h.characterId, targetTier);
            OutfitTierDef def = PuppetRegistry.GetProfile(h.characterId).GetTier(targetTier);
            string tierName = def != null ? def.tierName : targetTier.ToString();
            OnHeroSkinChanged?.Invoke(h, targetTier);
            msg = $"{h.name} 已换装为【{tierName}】！";
            return true;
        }

        public bool GeneratorRepaired => IsActiveOrUpgrading("relic_generator");
        public bool RadioRepaired => IsActiveOrUpgrading("radio_post");
        public bool TruckRepaired => IsActiveOrUpgrading("relic_truck");
        public bool RoadOpened => IsActiveOrUpgrading("relic_road_gate");
        public bool CanLaunchCityRun => TruckRepaired && RoadOpened;
        public bool HasWatchtower => IsActiveOrUpgrading("watchtower");
        public bool HasBarricade => IsActiveOrUpgrading("barricade");
        public bool HasDock => IsActiveOrUpgrading("river_dock");
        public bool AutoCollectUnlocked => CurrentPop >= 7; // 难民人口达 7 人后自动派驻帮工汲水收粮！

        bool IsActiveOrUpgrading(string id)
        {
            CampNode n = Find(id);
            if (n == null) return false;
            if (n.state == NodeState.Active) return true;
            return (n.state == NodeState.Constructing || n.state == NodeState.ReadyToHammer) && n.isUpgradingExisting;
        }

        public float ProdSpeedMultiplier
        {
            get
            {
                int genLv = GetNodeLevel("relic_generator", 0);
                float m = genLv >= 3 ? 1.65f : (genLv == 2 ? 1.45f : (genLv == 1 ? 1.25f : 1.0f));
                HeroSpec xt = FindHero("xiaotang");
                if (xt != null && xt.assigned) m *= 1.30f;
                return m;
            }
        }

        public float BuildTimeMultiplier
        {
            get
            {
                HeroSpec dc = FindHero("dachui");
                return (dc != null && dc.assigned) ? 0.70f : 1.0f;
            }
        }

        public int TruckMaxCargo
        {
            get
            {
                int trLv = GetNodeLevel("relic_truck", 1);
                int baseC = trLv >= 2 ? 95 : 60;
                HeroSpec dc = FindHero("dachui");
                return (dc != null && dc.assigned) ? baseC + 20 : baseC;
            }
        }

        public static int GetWorkshopSlotCap(CampNode workshop)
        {
            if (workshop == null) return 3;
            int lv = Mathf.Max(1, workshop.level);
            return Mathf.Clamp(2 + lv, 3, 5); // Lv.1=3格, Lv.2=4格, Lv.3+=5格！
        }

        public ForestSurvivalSim()
        {
            foreach (SurvRes r in Enum.GetValues(typeof(SurvRes)))
                _res[r] = 0;

            // 开局基础逃难物资：原木×10、口粮×6、山果鲜蔬×2、粗麻旧布×2、铜币×120
            _res[SurvRes.Wood] = 10;
            _res[SurvRes.Ration] = 6;
            _res[SurvRes.Veggie] = 2;
            _res[SurvRes.Cloth] = 2;

            _revealedRes.Add(SurvRes.Wood);
            _revealedRes.Add(SurvRes.Ration);

            InitNodes();
            CitySites = BuildCitySites();
            SawmillRecipes = BuildSawmillRecipes();
            SmithyRecipes = BuildSmithyRecipes();
            TailorRecipes = BuildTailorRecipes();
            KitchenRecipes = BuildKitchenRecipes();
            HerbHutRecipes = BuildHerbHutRecipes();
            InitHeroes();
            EnsureActiveOrders();
            RefreshCaravanOffers();
        }

        public static int GetXpNeed(int level)
        {
            int[] table = { 0, 60, 120, 190, 280, 390, 520, 680, 880, 1120, 1400, 1800 };
            return level > 0 && level < table.Length ? table[level] : 2200;
        }

        public void AddXp(int amount, Vector2 worldGrid)
        {
            if (amount <= 0) return;
            CampXp += amount;
            OnXpBurst?.Invoke(amount, worldGrid);

            while (CampLevel < 12 && CampXp >= GetXpNeed(CampLevel))
            {
                CampXp -= GetXpNeed(CampLevel);
                CampLevel++;
                // 每次升星保底赠送一笔铜币，Lv.4 起额外奖励工程手稿×2 防卡死！
                Coins += 80 * CampLevel;
                if (CampLevel >= 4)
                    Add(SurvRes.Blueprint, 2, notifyToast: true);

                string[] unlocks = GetUnlocksForLevel(CampLevel);
                OnCampLevelUp?.Invoke(CampLevel, unlocks);

                if (CampLevel == 2)
                {
                    UnlockHero("laochentou");
                }
                else if (CampLevel == 4)
                {
                    UnlockHero("linyan");
                    if (!HordeWarningTriggered)
                    {
                        HordeWarningTriggered = true;
                        OnHordeCrisisTriggered?.Invoke();
                    }
                }
                else if (CampLevel == 5)
                {
                    UnlockHero("dachui");
                }

                EnsureActiveOrders();
            }
        }

        static string[] GetUnlocksForLevel(int lv)
        {
            switch (lv)
            {
                case 2: return new[] { "净水石井", "1号菜麻田", "山泉鲜蔬汤" };
                case 3: return new[] { "废铁锻造坊", "裁缝防具坊", "河畔钓鱼台" };
                case 4: return new[] { "2号林间民居", "捕猎木哨塔", "防潮木床铺" };
                case 5: return new[] { "山林草药坊", "末日武装皮卡", "丧尸城搜刮" };
                case 6: return new[] { "山涧发电房", "山口行商电台", "锻打铁锭" };
                case 7: return new[] { "3号幸存者木楼", "加绒御寒皮衣", "驱寒草本热饮" };
                case 8: return new[] { "导流金属管", "高阶通水民居", "石井Lv.3" };
                case 9: return new[] { "废土钢板护甲", "抗尸毒急救箱", "旧城防卫总库" };
                default: return new[] { "全营地高阶等级上限开放" };
            }
        }

        public bool IsResRevealed(SurvRes r) => _revealedRes.Contains(r);

        public static string ResName(SurvRes r)
        {
            switch (r)
            {
                case SurvRes.Wood: return "原木";
                case SurvRes.Scrap: return "废铁";
                case SurvRes.Water: return "净水";
                case SurvRes.Veggie: return "鲜蔬";
                case SurvRes.Cloth: return "粗麻布";
                case SurvRes.Herb: return "山草药";
                case SurvRes.Fish: return "鲜鱼";
                case SurvRes.Meat: return "鲜肉";
                case SurvRes.Pelt: return "兽毛皮";
                case SurvRes.Plank: return "厚木板";
                case SurvRes.WoodBed: return "木床铺";
                case SurvRes.IronBeam: return "承重梁";
                case SurvRes.Nails: return "铁钉铆件";
                case SurvRes.IronIngot: return "锻打铁锭";
                case SurvRes.MetalPipe: return "导流铁管";
                case SurvRes.LinenShirt: return "麻布衣";
                case SurvRes.LeatherBoots: return "猎皮靴";
                case SurvRes.FurCoat: return "御寒皮衣";
                case SurvRes.PlateArmor: return "钢板护甲";
                case SurvRes.RoastVeggie: return "烤山果串";
                case SurvRes.VeggieSoup: return "鲜蔬汤";
                case SurvRes.GrillFish: return "香烤溪鱼";
                case SurvRes.SmokedMeat: return "松木熏肉";
                case SurvRes.HerbPoultice: return "止血药包";
                case SurvRes.WarmTea: return "驱寒热饮";
                case SurvRes.MedKit: return "抗毒药箱";
                case SurvRes.Ration: return "口粮";
                case SurvRes.Parts: return "医疗电子件";
                case SurvRes.Blueprint: return "工程手稿";
                default: return r.ToString();
            }
        }

        public int Get(SurvRes r) => _res.TryGetValue(r, out int v) ? v : 0;

        public void Add(SurvRes r, int delta, bool notifyToast = false)
        {
            if (delta == 0) return;
            if (delta > 0) _revealedRes.Add(r);
            int cur = Get(r);
            int next = Mathf.Max(0, cur + delta);
            if (delta > 0 && IsWarehouseCapped(r))
            {
                int cap = StorageCap;
                if (next > cap)
                {
                    next = cap;
                    if (cur >= cap)
                        OnWorldToast?.Invoke($"【{ResName(r)}】已达仓库上限({cap})！请升级物资仓库！", new Vector2(19.4f, 19.2f));
                }
            }
            _res[r] = next;
            if (delta > 0 && notifyToast && next > cur)
                OnResourceGained?.Invoke(r, next - cur);
        }

        public bool CanAfford(CostPair[] costs)
        {
            if (costs == null) return true;
            for (int i = 0; i < costs.Length; i++)
                if (Get(costs[i].res) < costs[i].amount) return false;
            return true;
        }

        public void Pay(CostPair[] costs)
        {
            if (costs == null) return;
            for (int i = 0; i < costs.Length; i++)
                Add(costs[i].res, -costs[i].amount);
        }

        public static string FormatCosts(CostPair[] costs, int coinCost = 0)
        {
            var list = new List<string>();
            if (coinCost > 0) list.Add($"铜币×{coinCost}");
            if (costs != null)
            {
                for (int i = 0; i < costs.Length; i++)
                    list.Add($"{ResName(costs[i].res)}×{costs[i].amount}");
            }
            return list.Count == 0 ? "免费" : string.Join("  ", list);
        }

        public CampNode Find(string id)
        {
            for (int i = 0; i < Nodes.Count; i++)
                if (Nodes[i].id == id) return Nodes[i];
            return null;
        }

        public CampNode FindByKind(NodeKind kind)
        {
            for (int i = 0; i < Nodes.Count; i++)
                if (Nodes[i].kind == kind) return Nodes[i];
            return null;
        }

        public HeroSpec FindHero(string id)
        {
            for (int i = 0; i < Heroes.Count; i++)
                if (Heroes[i].id == id) return Heroes[i];
            return null;
        }

        public void UnlockHero(string id, bool silent = false)
        {
            HeroSpec h = FindHero(id);
            if (h == null || h.unlocked) return;
            h.unlocked = true;
            if (!silent) OnHeroUnlocked?.Invoke(h);
        }

        public int AutoAssignHeroes()
        {
            int c = 0;
            for (int i = 0; i < Heroes.Count; i++)
            {
                if (Heroes[i].unlocked && !Heroes[i].assigned)
                {
                    Heroes[i].assigned = true;
                    c++;
                }
            }
            return c;
        }

        public void UnassignAllHeroes()
        {
            for (int i = 0; i < Heroes.Count; i++) Heroes[i].assigned = false;
        }

        void InitHeroes()
        {
            Heroes.Add(new HeroSpec
            {
                id = "ache", characterId = "ache", name = "阿澈", roleTitle = "落魄开荒青年 · 队长",
                portraitKey = "bust_leader", targetBuildingTitle = "避难所中枢 / 拓荒带队",
                buffDesc = "点空地或 WASD 直控；裁缝坊缝制皮衣与钢甲可全队换装",
                unlocked = true, assigned = true, skinTier = OutfitTier.Refugee,
            });
            Heroes.Add(new HeroSpec
            {
                id = "xiaotang", characterId = "xiaotang", name = "小棠", roleTitle = "汽修学徒宅女 · 技师",
                portraitKey = "bust_xiaotang", targetBuildingTitle = "废铁锻造坊 / 发电房",
                buffDesc = "全作坊加工与冶炼提速 +30%；Lv.3 解锁铆钉与防具线",
                unlocked = true, assigned = true, skinTier = OutfitTier.Refugee,
            });
            Heroes.Add(new HeroSpec
            {
                id = "laochentou", characterId = "laochentou", name = "老陈头", roleTitle = "夜宵炒粉大叔 · 主厨",
                portraitKey = "bust_laochentou", targetBuildingTitle = "野味炊事房",
                buffDesc = "料理产出效率 +25%，订单铜币加成；星级 Lv.2 入队",
                unlocked = false, assigned = false, skinTier = OutfitTier.Refugee,
            });
            Heroes.Add(new HeroSpec
            {
                id = "linyan", characterId = "linyan", name = "林砚", roleTitle = "近视体育生 · 猎手",
                portraitKey = "bust_linyan", targetBuildingTitle = "捕猎木哨塔 / 钓鱼台",
                buffDesc = "狩猎【鲜肉+兽毛皮】与溪流鲜鱼产量 +35%；星级 Lv.4 入队",
                unlocked = false, assigned = false, skinTier = OutfitTier.Refugee,
            });
            Heroes.Add(new HeroSpec
            {
                id = "dachui", characterId = "dachui", name = "大锤", roleTitle = "搬家公司力工 · 护卫",
                portraitKey = "bust_dachui", targetBuildingTitle = "锯木木艺坊 / 武装皮卡",
                buffDesc = "全建筑施工耗时 -30%，进城皮卡载重 +20；星级 Lv.5 入队",
                unlocked = false, assigned = false, skinTier = OutfitTier.Refugee,
            });
        }

        // =====================================================================
        // 五大生产作坊配方定义（重工基建 × 裁缝服装 × 野味炊事 × 山林草药）
        // =====================================================================
        static WorkshopRecipe[] BuildSawmillRecipes()
        {
            return new[]
            {
                new WorkshopRecipe
                {
                    id = "plank", name = "厚木板", iconKey = "ico_plank", unlockBuildingLv = 1, unlockCampLv = 1,
                    sourceHint = "砍伐林缘树木获取原木",
                    inRes1 = SurvRes.Wood, inAmt1 = 2, outRes = SurvRes.Plank, outAmt = 2, duration = 3.2f,
                },
                new WorkshopRecipe
                {
                    id = "wood_bed", name = "防潮木床铺", iconKey = "ico_woodbed", unlockBuildingLv = 2, unlockCampLv = 4,
                    sourceHint = "需锯木坊 Lv.2 + 厚木板 + 菜麻田收获的【粗麻布】",
                    inRes1 = SurvRes.Plank, inAmt1 = 2, inRes2 = SurvRes.Cloth, inAmt2 = 1,
                    outRes = SurvRes.WoodBed, outAmt = 1, duration = 5.0f,
                },
                new WorkshopRecipe
                {
                    id = "iron_beam", name = "包铁承重梁", iconKey = "rec_beam", unlockBuildingLv = 3, unlockCampLv = 6,
                    sourceHint = "需锯木坊 Lv.3 + 厚木板 + 废铁锻造坊的【铁钉铆件】",
                    inRes1 = SurvRes.Plank, inAmt1 = 2, inRes2 = SurvRes.Nails, inAmt2 = 2,
                    outRes = SurvRes.IronBeam, outAmt = 1, duration = 6.5f,
                },
            };
        }

        static WorkshopRecipe[] BuildSmithyRecipes()
        {
            return new[]
            {
                new WorkshopRecipe
                {
                    id = "nails", name = "铁钉铆件", iconKey = "ico_nails", unlockBuildingLv = 1, unlockCampLv = 3,
                    sourceHint = "拆解山道锈铁残骸或赴丧尸城汽修街搜刮【废铁】",
                    inRes1 = SurvRes.Scrap, inAmt1 = 2, outRes = SurvRes.Nails, outAmt = 2, duration = 4.0f,
                },
                new WorkshopRecipe
                {
                    id = "iron_ingot", name = "锻打铁锭", iconKey = "ico_ingot", unlockBuildingLv = 2, unlockCampLv = 6,
                    sourceHint = "需锻造坊 Lv.2 + 废铁 + 原木作炭火",
                    inRes1 = SurvRes.Scrap, inAmt1 = 3, inRes2 = SurvRes.Wood, inAmt2 = 1,
                    outRes = SurvRes.IronIngot, outAmt = 1, duration = 6.0f,
                },
                new WorkshopRecipe
                {
                    id = "metal_pipe", name = "导流金属管", iconKey = "ico_pipe", unlockBuildingLv = 3, unlockCampLv = 8,
                    sourceHint = "需锻造坊 Lv.3 + 锻打铁锭 + 铁钉铆件",
                    inRes1 = SurvRes.IronIngot, inAmt1 = 1, inRes2 = SurvRes.Nails, inAmt2 = 1,
                    outRes = SurvRes.MetalPipe, outAmt = 1, duration = 7.5f,
                },
            };
        }

        static WorkshopRecipe[] BuildTailorRecipes()
        {
            return new[]
            {
                new WorkshopRecipe
                {
                    id = "linen_shirt", name = "耐磨麻布衣", iconKey = "ico_shirt", unlockBuildingLv = 1, unlockCampLv = 3,
                    sourceHint = "收割【1号菜麻田】获取粗麻布，满足居民穿戴订单！",
                    inRes1 = SurvRes.Cloth, inAmt1 = 2, outRes = SurvRes.LinenShirt, outAmt = 1, duration = 4.2f,
                },
                new WorkshopRecipe
                {
                    id = "leather_boots", name = "防荆棘猎皮靴", iconKey = "ico_boots", unlockBuildingLv = 2, unlockCampLv = 5,
                    sourceHint = "需裁缝坊 Lv.2 + 狩猎野兽/哨塔获取的【兽毛皮】",
                    inRes1 = SurvRes.Pelt, inAmt1 = 2, inRes2 = SurvRes.Cloth, inAmt2 = 1,
                    outRes = SurvRes.LeatherBoots, outAmt = 1, duration = 5.5f,
                },
                new WorkshopRecipe
                {
                    id = "fur_coat", name = "加绒御寒皮衣", iconKey = "ico_coat", unlockBuildingLv = 3, unlockCampLv = 7,
                    sourceHint = "需裁缝坊 Lv.3；首次缝制即解锁全队【2阶加固皮衣换装】！",
                    inRes1 = SurvRes.Pelt, inAmt1 = 2, inRes2 = SurvRes.LinenShirt, inAmt2 = 1, inRes3 = SurvRes.Nails, inAmt3 = 1,
                    outRes = SurvRes.FurCoat, outAmt = 1, duration = 7.5f,
                },
                new WorkshopRecipe
                {
                    id = "plate_armor", name = "废土钢板护甲", iconKey = "ico_armor", unlockBuildingLv = 4, unlockCampLv = 9,
                    sourceHint = "需裁缝坊 Lv.4；首次打造即解锁全队【3阶重装铁甲换装】！",
                    inRes1 = SurvRes.FurCoat, inAmt1 = 1, inRes2 = SurvRes.IronIngot, inAmt2 = 2,
                    outRes = SurvRes.PlateArmor, outAmt = 1, duration = 9.5f,
                },
            };
        }

        static WorkshopRecipe[] BuildKitchenRecipes()
        {
            return new[]
            {
                new WorkshopRecipe
                {
                    id = "roast_veggie", name = "炭烤山果串", iconKey = "ico_veggie", unlockBuildingLv = 1, unlockCampLv = 1,
                    sourceHint = "采摘林边野生浆果丛或收割菜麻田获取【鲜蔬】",
                    inRes1 = SurvRes.Veggie, inAmt1 = 2, outRes = SurvRes.RoastVeggie, outAmt = 1, bonusRation = 2, duration = 3.0f,
                },
                new WorkshopRecipe
                {
                    id = "veggie_soup", name = "山泉鲜蔬汤", iconKey = "rec_stew", unlockBuildingLv = 1, unlockCampLv = 2,
                    sourceHint = "需鲜蔬×2 + 东南【净水石井】汲取的净水×1",
                    inRes1 = SurvRes.Veggie, inAmt1 = 2, inRes2 = SurvRes.Water, inAmt2 = 1,
                    outRes = SurvRes.VeggieSoup, outAmt = 1, bonusRation = 4, duration = 4.2f,
                },
                new WorkshopRecipe
                {
                    id = "grill_fish", name = "香烤山溪鱼", iconKey = "rec_roast_fish", unlockBuildingLv = 2, unlockCampLv = 3,
                    sourceHint = "需炊事房 Lv.2 + 南岸【河畔钓鱼台】垂钓的鲜鱼×2",
                    inRes1 = SurvRes.Fish, inAmt1 = 2, inRes2 = SurvRes.Wood, inAmt2 = 1,
                    outRes = SurvRes.GrillFish, outAmt = 1, bonusRation = 6, duration = 5.0f,
                },
                new WorkshopRecipe
                {
                    id = "smoked_meat", name = "松木秘制熏肉", iconKey = "ico_ration", unlockBuildingLv = 3, unlockCampLv = 6,
                    sourceHint = "需炊事房 Lv.3 + 狩猎野兽/捕猎哨塔获取的【鲜肉】×2",
                    inRes1 = SurvRes.Meat, inAmt1 = 2, inRes2 = SurvRes.Wood, inAmt2 = 1,
                    outRes = SurvRes.SmokedMeat, outAmt = 1, bonusRation = 10, duration = 6.5f,
                },
            };
        }

        static WorkshopRecipe[] BuildHerbHutRecipes()
        {
            return new[]
            {
                new WorkshopRecipe
                {
                    id = "poultice", name = "止血草药敷包", iconKey = "ico_poultice", unlockBuildingLv = 1, unlockCampLv = 5,
                    sourceHint = "收割【林间药圃】或砍林伴生获取【山草药】+【粗麻布】",
                    inRes1 = SurvRes.Herb, inAmt1 = 2, inRes2 = SurvRes.Cloth, inAmt2 = 1,
                    outRes = SurvRes.HerbPoultice, outAmt = 1, duration = 4.8f,
                },
                new WorkshopRecipe
                {
                    id = "warm_tea", name = "驱寒草本热饮", iconKey = "ico_tea", unlockBuildingLv = 2, unlockCampLv = 7,
                    sourceHint = "需草药坊 Lv.2 + 山草药×2 + 净水×2",
                    inRes1 = SurvRes.Herb, inAmt1 = 2, inRes2 = SurvRes.Water, inAmt2 = 2,
                    outRes = SurvRes.WarmTea, outAmt = 1, duration = 6.0f,
                },
                new WorkshopRecipe
                {
                    id = "medkit", name = "抗尸毒急救箱", iconKey = "ico_medkit", unlockBuildingLv = 3, unlockCampLv = 9,
                    sourceHint = "需草药坊 Lv.3 + 止血药包 + 丧尸城药房搜刮的【医疗电子件】",
                    inRes1 = SurvRes.HerbPoultice, inAmt1 = 1, inRes2 = SurvRes.Parts, inAmt2 = 1, inRes3 = SurvRes.IronIngot, inAmt3 = 1,
                    outRes = SurvRes.MedKit, outAmt = 1, duration = 8.5f,
                },
            };
        }

        // =====================================================================
        // 全营地 15+ 座建筑与空间分层落位（中央大广场净空！）
        // =====================================================================
        void InitNodes()
        {
            // --- 内环核心区（开局可见，环绕中央广场外缘） ---
            Nodes.Add(new CampNode
            {
                id = "shelter", kind = NodeKind.Shelter, state = NodeState.Active, level = 1, maxLevel = 3, unlockPlayerLv = 1,
                activeTitle = "避难所议事厅", ruinTitle = "避难所议事厅",
                desc = "逃难小队统筹指挥中枢。升级可额外增加供暖人口承载并开放高阶防区。",
                benefitLabel = "中枢额外人口承载", benefitFrom = 0, benefitTo = 3, benefitRes = SurvRes.Blueprint,
                gx = 16.8f, gy = 20.5f, occupyX = 15, occupyY = 19, occupyW = 3, occupyH = 3,
                revealChunkX = 5, revealChunkY = 6, buildDuration = 4.5f,
            });

            Nodes.Add(new CampNode
            {
                id = "heat_tower", kind = NodeKind.HeatTower, state = NodeState.Active, level = 1, maxLevel = 4, unlockPlayerLv = 1,
                activeTitle = "篝火供暖塔", ruinTitle = "篝火供暖塔",
                desc = "深山入秋寒气逼人！升级供暖炉塔扩大温暖覆盖半径，是提升营地【供暖人口上限 MAX】的核心基建！",
                benefitLabel = "营地供暖人口上限", benefitFrom = 8, benefitTo = 15, benefitRes = SurvRes.Wood,
                gx = 14.3f, gy = 18.8f, occupyX = 14, occupyY = 18, occupyW = 2, occupyH = 2,
                revealChunkX = 4, revealChunkY = 6, buildDuration = 4.0f,
            });

            Nodes.Add(new CampNode
            {
                id = "house_1", kind = NodeKind.Dwelling, state = NodeState.Active, level = 1, maxLevel = 4, unlockPlayerLv = 1,
                activeTitle = "1号避风民居", ruinTitle = "1号避风民居",
                desc = "一顶补丁帆布加几捆干草，总比冻在深山里强。消耗供暖上限吸纳新难民，提升【当前实有人口】！",
                benefitLabel = "入驻当前人口", benefitFrom = 4, benefitTo = 7, benefitRes = SurvRes.WoodBed,
                gx = 12.1f, gy = 19.3f, occupyX = 11, occupyY = 18, occupyW = 2, occupyH = 2,
                revealChunkX = 4, revealChunkY = 6, buildDuration = 3.5f,
            });

            Nodes.Add(new CampNode
            {
                id = "warehouse", kind = NodeKind.Warehouse, state = NodeState.Active, level = 1, maxLevel = 5, unlockPlayerLv = 1,
                activeTitle = "营地物资仓库", ruinTitle = "营地物资仓库",
                desc = "虽然捡来的多是废木烂铁，也得码放整齐。需足够【当前人口】看守，升级大幅提升全原料【储量上限】！",
                benefitLabel = "单品原料储量上限", benefitFrom = 80, benefitTo = 180, benefitRes = SurvRes.Plank,
                gx = 19.3f, gy = 19.0f, occupyX = 18, occupyY = 18, occupyW = 2, occupyH = 2,
                revealChunkX = 6, revealChunkY = 6, buildDuration = 3.5f,
            });

            Nodes.Add(new CampNode
            {
                id = "kitchen", kind = NodeKind.Kitchen, state = NodeState.Active, level = 1, maxLevel = 3, unlockPlayerLv = 1,
                activeTitle = "野味炊事房", ruinTitle = "野味炊事房",
                desc = "将鲜蔬、山泉、溪鱼与野味烹制为热腾腾的山林料理，既满足居民餐饮订单赚铜币，又补充拓荒口粮！",
                benefitLabel = "开放高阶菜谱与队列槽", benefitFrom = 3, benefitTo = 4, benefitRes = SurvRes.RoastVeggie,
                gx = 12.8f, gy = 16.0f, occupyX = 12, occupyY = 15, occupyW = 2, occupyH = 2,
                revealChunkX = 4, revealChunkY = 5, buildDuration = 4.0f,
            });

            Nodes.Add(new CampNode
            {
                id = "sawmill", kind = NodeKind.Sawmill, state = NodeState.Discovered, level = 0, maxLevel = 3, unlockPlayerLv = 1,
                ruinTitle = "锯木木艺坊", activeTitle = "锯木木艺坊",
                desc = "加工【厚木板】、【防潮木床铺】与【包铁承重梁】，建成后学徒还会定期巡林带回再生原木！",
                benefitLabel = "木艺队列槽位与床铺配方", benefitFrom = 0, benefitTo = 3, benefitRes = SurvRes.Plank,
                gx = 20.0f, gy = 15.6f, occupyX = 19, occupyY = 14, occupyW = 3, occupyH = 3,
                revealChunkX = 6, revealChunkY = 5, buildDuration = 3.5f,
                buildCoinCost = 40, buildReqPop = 4,
                buildCosts = new[] { new CostPair(SurvRes.Wood, 12) },
            });

            // --- 中环生态与五大作坊扩展区（随主角星级 Lv.2 ~ Lv.5 逐步显现！） ---
            Nodes.Add(new CampNode
            {
                id = "relic_well", kind = NodeKind.RelicWell, state = NodeState.HiddenInForest, level = 0, maxLevel = 3, unlockPlayerLv = 2,
                hintLabel = "坍塌古井", ruinTitle = "净水石井", activeTitle = "净水石井",
                desc = "深山古泉旧井，修好辘轳可持续汲取【山泉净水】，灌溉菜麻田并熬制浓汤草药饮。",
                benefitLabel = "储水上限与汲水效率", benefitFrom = 0, benefitTo = 10, benefitRes = SurvRes.Water,
                gx = 19.2f, gy = 9.6f, occupyX = 19, occupyY = 9, occupyW = 1, occupyH = 1,
                revealChunkX = 6, revealChunkY = 3, buildDuration = 3.5f,
                buildCoinCost = 80, buildReqPop = 4,
                buildCosts = new[] { new CostPair(SurvRes.Wood, 12), new CostPair(SurvRes.Plank, 4) },
                prodInterval = 6.0f, prodOutRes = SurvRes.Water, prodOutCount = 3, maxStored = 12,
            });

            Nodes.Add(new CampNode
            {
                id = "farm_a", kind = NodeKind.Farm, state = NodeState.HiddenInForest, level = 0, maxLevel = 3, unlockPlayerLv = 2,
                hintLabel = "向阳沃土", ruinTitle = "1号开荒菜麻田", activeTitle = "1号开荒菜麻田",
                desc = "引山泉净水浇灌，每轮同时收获【山果鲜蔬】与【粗麻旧布】，直供炊事房与裁缝防具坊！",
                benefitLabel = "单轮鲜蔬与粗麻收成", benefitFrom = 0, benefitTo = 8, benefitRes = SurvRes.Veggie,
                gx = 10.0f, gy = 14.8f, occupyX = 9, occupyY = 14, occupyW = 2, occupyH = 2,
                revealChunkX = 3, revealChunkY = 4, buildDuration = 3.0f,
                buildCoinCost = 60, buildReqPop = 4,
                buildCosts = new[] { new CostPair(SurvRes.Wood, 10) },
            });

            Nodes.Add(new CampNode
            {
                id = "smithy", kind = NodeKind.Smithy, state = NodeState.HiddenInForest, level = 0, maxLevel = 3, unlockPlayerLv = 3,
                hintLabel = "旧锻铁台", ruinTitle = "废铁锻造坊", activeTitle = "废铁锻造坊",
                desc = "全营地五金重工心脏！将废铁锤炼为【铁钉铆件】、【锻打铁锭】与【导流金属管】，一切进阶建筑必经之地！",
                benefitLabel = "五金冶炼队列槽位", benefitFrom = 0, benefitTo = 3, benefitRes = SurvRes.Nails,
                gx = 20.4f, gy = 12.6f, occupyX = 19, occupyY = 12, occupyW = 2, occupyH = 2,
                revealChunkX = 6, revealChunkY = 4, buildDuration = 4.0f,
                buildCoinCost = 240, buildReqPop = 6,
                buildCosts = new[] { new CostPair(SurvRes.Plank, 8), new CostPair(SurvRes.Scrap, 6) },
            });

            Nodes.Add(new CampNode
            {
                id = "tailor", kind = NodeKind.Tailor, state = NodeState.HiddenInForest, level = 0, maxLevel = 4, unlockPlayerLv = 3,
                hintLabel = "破旧缝纫棚", ruinTitle = "裁缝防具坊", activeTitle = "裁缝防具坊",
                desc = "用粗麻布与野兽毛皮缝制【麻布衣】、【猎皮靴】、【御寒皮衣】与【钢板护甲】，既交高金居民订单，又解锁全队角色换装！",
                benefitLabel = "防具制衣队列槽位", benefitFrom = 0, benefitTo = 3, benefitRes = SurvRes.LinenShirt,
                gx = 17.2f, gy = 12.0f, occupyX = 16, occupyY = 11, occupyW = 2, occupyH = 2,
                revealChunkX = 5, revealChunkY = 4, buildDuration = 4.0f,
                buildCoinCost = 220, buildReqPop = 6,
                buildCosts = new[] { new CostPair(SurvRes.Plank, 8), new CostPair(SurvRes.Cloth, 2) },
            });

            Nodes.Add(new CampNode
            {
                id = "river_dock", kind = NodeKind.RiverDock, state = NodeState.HiddenInForest, level = 0, maxLevel = 2, unlockPlayerLv = 3,
                hintLabel = "潺潺溪水声", ruinTitle = "河畔钓鱼台", activeTitle = "河畔钓鱼台",
                desc = "南坡清溪回水湾的木栈钓台，甩杆垂钓与鱼篓挂机持续产出【山溪鲜鱼】。",
                benefitLabel = "鱼篓储量与鲜鱼产出", benefitFrom = 0, benefitTo = 10, benefitRes = SurvRes.Fish,
                gx = 12.6f, gy = 4.3f, occupyX = 12, occupyY = 3, occupyW = 2, occupyH = 1,
                revealChunkX = 4, revealChunkY = 1, buildDuration = 3.5f,
                buildCoinCost = 200, buildReqPop = 6,
                buildCosts = new[] { new CostPair(SurvRes.Plank, 6), new CostPair(SurvRes.Nails, 2) },
                prodInterval = 7.5f, prodOutRes = SurvRes.Fish, prodOutCount = 2, maxStored = 12,
            });

            Nodes.Add(new CampNode
            {
                id = "house_2", kind = NodeKind.Dwelling, state = NodeState.HiddenInForest, level = 0, maxLevel = 4, unlockPlayerLv = 4,
                hintLabel = "背风林地", ruinTitle = "2号原木民居", activeTitle = "2号原木民居",
                desc = "加厚原木墙体的第二座宿营屋，大幅扩充【当前实有人口】以支撑中后期工厂运转。",
                benefitLabel = "新增入驻人口", benefitFrom = 0, benefitTo = 5, benefitRes = SurvRes.WoodBed,
                gx = 10.6f, gy = 17.5f, occupyX = 10, occupyY = 17, occupyW = 2, occupyH = 2,
                revealChunkX = 3, revealChunkY = 6, buildDuration = 4.0f,
                buildCoinCost = 360, buildAddPop = 5,
                buildCosts = new[] { new CostPair(SurvRes.Plank, 10), new CostPair(SurvRes.Nails, 4) },
            });

            Nodes.Add(new CampNode
            {
                id = "watchtower", kind = NodeKind.Watchtower, state = NodeState.HiddenInForest, level = 0, maxLevel = 2, unlockPlayerLv = 4,
                hintLabel = "制高树台", ruinTitle = "捕猎木哨塔", activeTitle = "捕猎木哨塔",
                desc = "林缘高架瞭望塔：自动射猎野鹿野猪同时产出【野味鲜肉 + 野兽毛皮】，并在尸潮来袭时远程狙击！",
                benefitLabel = "自动狩猎鲜肉毛皮与城防", benefitFrom = 0, benefitTo = 120, benefitRes = SurvRes.Pelt,
                gx = 12.2f, gy = 21.8f, occupyX = 11, occupyY = 21, occupyW = 2, occupyH = 2,
                revealChunkX = 4, revealChunkY = 7, buildDuration = 4.0f,
                buildCoinCost = 420, buildReqPop = 9,
                buildCosts = new[] { new CostPair(SurvRes.Plank, 10), new CostPair(SurvRes.Nails, 4) },
                prodInterval = 9.0f, prodOutRes = SurvRes.Meat, prodOutCount = 2, prodOutRes2 = SurvRes.Pelt, prodOutCount2 = 2, maxStored = 14,
            });

            Nodes.Add(new CampNode
            {
                id = "barricade", kind = NodeKind.Barricade, state = NodeState.HiddenInForest, level = 0, maxLevel = 2, unlockPlayerLv = 4,
                ruinTitle = "尖木拒马防线", activeTitle = "尖木拒马防线",
                desc = "沿营地南侧山口布设削尖原木拒马与铁蒺藜，拦截游荡丧尸偷粮并稳固尸潮防线。",
                benefitLabel = "营地山口城防坚固度", benefitFrom = 10, benefitTo = 100, benefitRes = SurvRes.Plank,
                gx = 14.6f, gy = 11.8f, occupyX = 14, occupyY = 11, occupyW = 2, occupyH = 2,
                revealChunkX = 4, revealChunkY = 4, buildDuration = 3.5f,
                buildCoinCost = 280, buildReqPop = 8,
                buildCosts = new[] { new CostPair(SurvRes.Plank, 6), new CostPair(SurvRes.Nails, 2) },
            });

            // --- 外环医药、电力、行商与丧尸城远征区（Lv.5 ~ Lv.7 解锁） ---
            Nodes.Add(new CampNode
            {
                id = "herb_hut", kind = NodeKind.HerbHut, state = NodeState.HiddenInForest, level = 0, maxLevel = 3, unlockPlayerLv = 5,
                hintLabel = "采药人旧棚", ruinTitle = "山林草药坊", activeTitle = "山林草药坊",
                desc = "熬制【止血草药敷包】、【驱寒草本热饮】与【抗尸毒急救箱】，伤病居民订单经验极高，更是山口行商重金求购的王牌货！",
                benefitLabel = "防疫制药队列槽位", benefitFrom = 0, benefitTo = 3, benefitRes = SurvRes.HerbPoultice,
                gx = 11.8f, gy = 12.6f, occupyX = 11, occupyY = 12, occupyW = 2, occupyH = 2,
                revealChunkX = 3, revealChunkY = 4, buildDuration = 4.2f,
                buildCoinCost = 520, buildReqPop = 11,
                buildCosts = new[] { new CostPair(SurvRes.Plank, 10), new CostPair(SurvRes.Nails, 4), new CostPair(SurvRes.Cloth, 3) },
            });

            Nodes.Add(new CampNode
            {
                id = "farm_b", kind = NodeKind.Farm, state = NodeState.HiddenInForest, level = 0, maxLevel = 2, unlockPlayerLv = 5, isHerbPlot = true,
                hintLabel = "山阴药畦", ruinTitle = "2号林间药蔬圃", activeTitle = "2号林间药蔬圃",
                desc = "背风湿润的山阴药圃，浇灌山泉即可稳定收获大批【深山草药】与【山果鲜蔬】，直供草药坊熬药！",
                benefitLabel = "单轮山草药与鲜蔬收成", benefitFrom = 0, benefitTo = 8, benefitRes = SurvRes.Herb,
                gx = 14.2f, gy = 9.2f, occupyX = 13, occupyY = 8, occupyW = 2, occupyH = 2,
                revealChunkX = 4, revealChunkY = 3, buildDuration = 3.5f,
                buildCoinCost = 450, buildReqPop = 10,
                buildCosts = new[] { new CostPair(SurvRes.Plank, 8), new CostPair(SurvRes.Nails, 2) },
            });

            Nodes.Add(new CampNode
            {
                id = "relic_loot_sw", kind = NodeKind.RelicLoot, state = NodeState.HiddenInForest, level = 0, unlockPlayerLv = 2,
                hintLabel = "废弃救援车箱", ruinTitle = "遇难车队物资箱", activeTitle = "已拆解",
                desc = "早先逃难车队翻车遗落的金属箱，拆解可回收首批【废铁】、【医疗电子件】与【工程手稿】。",
                gx = 9.4f, gy = 12.0f, occupyX = 9, occupyY = 12, occupyW = 1, occupyH = 1,
                revealChunkX = 3, revealChunkY = 4,
            });

            Nodes.Add(new CampNode
            {
                id = "relic_truck", kind = NodeKind.RelicTruck, state = NodeState.HiddenInForest, level = 0, maxLevel = 2, unlockPlayerLv = 5,
                hintLabel = "报废破皮卡", ruinTitle = "末日武装皮卡", activeTitle = "末日武装皮卡",
                desc = "焊上包铁撞角并修好引擎，即可开车下山进【丧尸城 4 大街区】大宗搜刮【工程手稿】、废铁与医疗件！",
                benefitLabel = "单趟进城搜刮载重上限", benefitFrom = 0, benefitTo = 60, benefitRes = SurvRes.Blueprint,
                gx = 15.4f, gy = 23.0f, occupyX = 14, occupyY = 22, occupyW = 2, occupyH = 2,
                revealChunkX = 5, revealChunkY = 7, buildDuration = 4.5f,
                buildCoinCost = 620, buildReqPop = 11,
                buildCosts = new[] { new CostPair(SurvRes.Plank, 10), new CostPair(SurvRes.Nails, 6), new CostPair(SurvRes.Scrap, 10) },
            });

            Nodes.Add(new CampNode
            {
                id = "relic_road_gate", kind = NodeKind.RelicRoadGate, state = NodeState.HiddenInForest, level = 0, maxLevel = 1, unlockPlayerLv = 5,
                hintLabel = "丧尸城山道", ruinTitle = "盘山公路关卡", activeTitle = "盘山公路关卡",
                desc = "深山通往沦陷市区的唯一公路闸口。清理路障后武装皮卡即可随时发车下山搜刮！",
                benefitLabel = "开放丧尸城搜刮街区", benefitFrom = 0, benefitTo = 4, benefitRes = SurvRes.Blueprint,
                gx = 18.8f, gy = 25.2f, occupyX = 18, occupyY = 24, occupyW = 2, occupyH = 2,
                revealChunkX = 6, revealChunkY = 8, buildDuration = 3.5f,
                buildCoinCost = 320, buildReqPop = 10,
                buildCosts = new[] { new CostPair(SurvRes.Plank, 6), new CostPair(SurvRes.Nails, 4) },
            });

            Nodes.Add(new CampNode
            {
                id = "relic_generator", kind = NodeKind.RelicGenerator, state = NodeState.HiddenInForest, level = 0, maxLevel = 3, unlockPlayerLv = 6,
                hintLabel = "废弃水轮机房", ruinTitle = "山涧发电房", activeTitle = "山涧发电房",
                desc = "修复林间柴油水轮并网机组，为全营地五大生产作坊提供强劲电力加速（+25% → +65%）！",
                benefitLabel = "全作坊电力提速加成(%)", benefitFrom = 0, benefitTo = 25, benefitRes = SurvRes.Parts,
                gx = 22.8f, gy = 17.6f, occupyX = 22, occupyY = 17, occupyW = 2, occupyH = 2,
                revealChunkX = 7, revealChunkY = 5, buildDuration = 5.0f,
                buildCoinCost = 1200, buildReqPop = 14,
                buildCosts = new[] { new CostPair(SurvRes.Blueprint, 4), new CostPair(SurvRes.IronIngot, 3), new CostPair(SurvRes.Parts, 3) },
            });

            Nodes.Add(new CampNode
            {
                id = "radio_post", kind = NodeKind.RadioPost, state = NodeState.HiddenInForest, level = 0, maxLevel = 2, unlockPlayerLv = 6,
                hintLabel = "断线电台塔", ruinTitle = "山口行商电台", activeTitle = "山口行商电台",
                desc = "修复短波求生电台联络邻山聚落，招来【山口行商驼队】高价收购皮衣、急救箱与熏肉，换取巨额铜币与工程手稿！",
                benefitLabel = "行商收购溢价与手稿配额", benefitFrom = 0, benefitTo = 100, benefitRes = SurvRes.Blueprint,
                gx = 22.6f, gy = 21.2f, occupyX = 22, occupyY = 20, occupyW = 2, occupyH = 2,
                revealChunkX = 7, revealChunkY = 7, buildDuration = 4.2f,
                buildCoinCost = 980, buildReqPop = 13,
                buildCosts = new[] { new CostPair(SurvRes.Blueprint, 3), new CostPair(SurvRes.IronBeam, 3), new CostPair(SurvRes.Parts, 2) },
            });

            Nodes.Add(new CampNode
            {
                id = "house_3", kind = NodeKind.Dwelling, state = NodeState.HiddenInForest, level = 0, maxLevel = 3, unlockPlayerLv = 7,
                hintLabel = "向阳山坪", ruinTitle = "3号幸存者木楼", activeTitle = "3号幸存者木楼",
                desc = "双层防寒木结构宿营楼，铺设木床与供暖铁管，是中后期突破 30+ 人口大关的主力居所！",
                benefitLabel = "新增入驻人口", benefitFrom = 0, benefitTo = 8, benefitRes = SurvRes.WoodBed,
                gx = 20.6f, gy = 22.8f, occupyX = 20, occupyY = 22, occupyW = 2, occupyH = 2,
                revealChunkX = 6, revealChunkY = 7, buildDuration = 4.5f,
                buildCoinCost = 1500, buildAddPop = 8,
                buildCosts = new[] { new CostPair(SurvRes.Blueprint, 4), new CostPair(SurvRes.IronBeam, 4), new CostPair(SurvRes.WoodBed, 2) },
            });
        }

        static CitySiteDef[] BuildCitySites()
        {
            return new[]
            {
                new CitySiteDef
                {
                    id = "market", name = "废弃生鲜超市街区", spriteKey = "city_market",
                    dangerTag = "危险 ★☆☆", lootSummary = "主产：粗麻旧布 · 山泉净水 · 熟食口粮 · 工程手稿×2",
                    rationCost = 3, requireShelterLv = 1,
                    mainRes = SurvRes.Cloth, mainMin = 10, mainMax = 15,
                    subRes = SurvRes.Water, subMin = 8, subMax = 12,
                    bonusRes = SurvRes.Ration, bonusChancePct = 100, bonusCount = 8,
                    blueprintDrop = 2,
                },
                new CitySiteDef
                {
                    id = "garage", name = "五金汽修工业街", spriteKey = "city_garage",
                    dangerTag = "危险 ★★☆", lootSummary = "主产：大宗废铁(主力产地) · 厚木板 · 工程手稿×3",
                    rationCost = 4, requireShelterLv = 1,
                    mainRes = SurvRes.Scrap, mainMin = 18, mainMax = 26,
                    subRes = SurvRes.Plank, subMin = 8, subMax = 12,
                    bonusRes = SurvRes.Parts, bonusChancePct = 85, bonusCount = 3,
                    blueprintDrop = 3,
                },
                new CitySiteDef
                {
                    id = "pharmacy", name = "街角急诊药房区", spriteKey = "city_pharmacy",
                    dangerTag = "危险 ★★☆", lootSummary = "主产：医疗电子件 · 深山草药 · 工程手稿×4",
                    rationCost = 4, requireShelterLv = 2,
                    mainRes = SurvRes.Parts, mainMin = 5, mainMax = 9,
                    subRes = SurvRes.Herb, subMin = 8, subMax = 12,
                    bonusRes = SurvRes.Water, bonusChancePct = 90, bonusCount = 8,
                    blueprintDrop = 4,
                },
                new CitySiteDef
                {
                    id = "depot", name = "旧城防卫物资总库", spriteKey = "city_depot",
                    dangerTag = "危险 ★★★", lootSummary = "主产：工程手稿×6 · 重型废铁 · 高阶医疗电子件",
                    rationCost = 6, requireShelterLv = 3,
                    mainRes = SurvRes.Scrap, mainMin = 24, mainMax = 34,
                    subRes = SurvRes.Parts, subMin = 8, subMax = 12,
                    bonusRes = SurvRes.Ration, bonusChancePct = 100, bonusCount = 12,
                    blueprintDrop = 6,
                },
            };
        }

        // =====================================================================
        // 完整建筑升级消耗、星级门槛、人口卡点与【缺料一键溯源】查询系统
        // =====================================================================
        public int GetRequiredPlayerLvForNext(CampNode n)
        {
            if (n == null) return 1;
            if (n.state == NodeState.Discovered || n.state == NodeState.HiddenInForest)
                return Mathf.Max(1, n.unlockPlayerLv);

            int nextLv = n.level + 1;
            switch (n.kind)
            {
                case NodeKind.Shelter: return nextLv == 2 ? 3 : 7;
                case NodeKind.HeatTower: return nextLv == 2 ? 3 : (nextLv == 3 ? 6 : 9);
                case NodeKind.Dwelling:
                    if (n.id == "house_1") return nextLv == 2 ? 2 : (nextLv == 3 ? 4 : 8);
                    if (n.id == "house_2") return nextLv == 2 ? 5 : (nextLv == 3 ? 8 : 10);
                    return nextLv == 2 ? 9 : 11;
                case NodeKind.Warehouse: return nextLv == 2 ? 2 : (nextLv == 3 ? 5 : (nextLv == 4 ? 7 : 10));
                case NodeKind.Sawmill: return nextLv == 2 ? 4 : 6;
                case NodeKind.Smithy: return nextLv == 2 ? 6 : 8;
                case NodeKind.Tailor: return nextLv == 2 ? 5 : (nextLv == 3 ? 7 : 9);
                case NodeKind.Kitchen: return nextLv == 2 ? 3 : 6;
                case NodeKind.HerbHut: return nextLv == 2 ? 7 : 9;
                case NodeKind.RelicWell: return nextLv == 2 ? 4 : 8;
                case NodeKind.Farm: return nextLv == 2 ? 4 : 7;
                case NodeKind.RiverDock: return 5;
                case NodeKind.Watchtower: return 7;
                case NodeKind.Barricade: return 6;
                case NodeKind.RelicGenerator: return nextLv == 2 ? 8 : 10;
                case NodeKind.RadioPost: return 8;
                case NodeKind.RelicTruck: return 9;
                default: return 2;
            }
        }

        public void GetNextUpgradeSpec(CampNode n, out int coinCost, out int reqPopCur, out int reqPopCap, out CostPair[] matCosts)
        {
            coinCost = 0; reqPopCur = 0; reqPopCap = 0; matCosts = null;
            if (n == null) return;

            if (n.state == NodeState.Discovered || n.state == NodeState.HiddenInForest)
            {
                coinCost = n.buildCoinCost;
                reqPopCur = n.buildReqPop;
                if (n.kind == NodeKind.Dwelling && n.buildAddPop > 0)
                    reqPopCap = CurrentPop + n.buildAddPop;
                matCosts = n.buildCosts ?? Array.Empty<CostPair>();
                return;
            }

            if (n.state != NodeState.Active || n.level >= n.maxLevel) return;
            int nextLv = n.level + 1;

            switch (n.kind)
            {
                case NodeKind.HeatTower:
                    if (nextLv == 2) { coinCost = 320; reqPopCur = 6; matCosts = new[] { new CostPair(SurvRes.Plank, 10), new CostPair(SurvRes.Nails, 4) }; }
                    else if (nextLv == 3) { coinCost = 1100; reqPopCur = 12; matCosts = new[] { new CostPair(SurvRes.Blueprint, 4), new CostPair(SurvRes.IronBeam, 4), new CostPair(SurvRes.IronIngot, 3) }; }
                    else { coinCost = 2800; reqPopCur = 24; matCosts = new[] { new CostPair(SurvRes.Blueprint, 8), new CostPair(SurvRes.MetalPipe, 6), new CostPair(SurvRes.IronIngot, 6) }; }
                    break;

                case NodeKind.Shelter:
                    if (nextLv == 2) { coinCost = 420; reqPopCur = 7; matCosts = new[] { new CostPair(SurvRes.Plank, 12), new CostPair(SurvRes.Nails, 4) }; }
                    else { coinCost = 2200; reqPopCur = 18; matCosts = new[] { new CostPair(SurvRes.Blueprint, 6), new CostPair(SurvRes.IronBeam, 6), new CostPair(SurvRes.IronIngot, 4) }; }
                    break;

                case NodeKind.Dwelling:
                {
                    int curHousePop = GetDwellingPopForLevel(n.id, n.level);
                    int nextHousePop = GetDwellingPopForLevel(n.id, nextLv);
                    reqPopCap = CurrentPop - curHousePop + nextHousePop;
                    if (nextLv == 2)
                    {
                        coinCost = n.id == "house_1" ? 120 : 680;
                        matCosts = n.id == "house_1"
                            ? new[] { new CostPair(SurvRes.Wood, 14), new CostPair(SurvRes.Plank, 6) }
                            : new[] { new CostPair(SurvRes.Plank, 10), new CostPair(SurvRes.WoodBed, 2), new CostPair(SurvRes.Nails, 4) };
                    }
                    else if (nextLv == 3)
                    {
                        coinCost = 520;
                        matCosts = new[] { new CostPair(SurvRes.WoodBed, 2), new CostPair(SurvRes.LinenShirt, 2), new CostPair(SurvRes.Nails, 4) };
                    }
                    else
                    {
                        coinCost = 1800;
                        matCosts = new[] { new CostPair(SurvRes.Blueprint, 4), new CostPair(SurvRes.MetalPipe, 4), new CostPair(SurvRes.FurCoat, 2) };
                    }
                    break;
                }

                case NodeKind.Warehouse:
                    if (nextLv == 2) { coinCost = 150; reqPopCur = 6; matCosts = new[] { new CostPair(SurvRes.Plank, 8) }; }
                    else if (nextLv == 3) { coinCost = 650; reqPopCur = 12; matCosts = new[] { new CostPair(SurvRes.Blueprint, 3), new CostPair(SurvRes.Nails, 6) }; }
                    else if (nextLv == 4) { coinCost = 1800; reqPopCur = 20; matCosts = new[] { new CostPair(SurvRes.Blueprint, 6), new CostPair(SurvRes.IronBeam, 5), new CostPair(SurvRes.IronIngot, 4) }; }
                    else { coinCost = 3800; reqPopCur = 30; matCosts = new[] { new CostPair(SurvRes.Blueprint, 10), new CostPair(SurvRes.MetalPipe, 6), new CostPair(SurvRes.IronIngot, 6) }; }
                    break;

                case NodeKind.Sawmill:
                    if (nextLv == 2) { coinCost = 460; reqPopCur = 9; matCosts = new[] { new CostPair(SurvRes.Blueprint, 2), new CostPair(SurvRes.Plank, 10), new CostPair(SurvRes.Nails, 4) }; }
                    else { coinCost = 1400; reqPopCur = 16; matCosts = new[] { new CostPair(SurvRes.Blueprint, 5), new CostPair(SurvRes.IronIngot, 4), new CostPair(SurvRes.Nails, 6) }; }
                    break;

                case NodeKind.Smithy:
                    if (nextLv == 2) { coinCost = 980; reqPopCur = 14; matCosts = new[] { new CostPair(SurvRes.Blueprint, 4), new CostPair(SurvRes.IronBeam, 4), new CostPair(SurvRes.Scrap, 14) }; }
                    else { coinCost = 2200; reqPopCur = 22; matCosts = new[] { new CostPair(SurvRes.Blueprint, 7), new CostPair(SurvRes.IronIngot, 5), new CostPair(SurvRes.Parts, 3) }; }
                    break;

                case NodeKind.Tailor:
                    if (nextLv == 2) { coinCost = 580; reqPopCur = 10; matCosts = new[] { new CostPair(SurvRes.Blueprint, 2), new CostPair(SurvRes.Plank, 10), new CostPair(SurvRes.Nails, 4) }; }
                    else if (nextLv == 3) { coinCost = 1450; reqPopCur = 18; matCosts = new[] { new CostPair(SurvRes.Blueprint, 5), new CostPair(SurvRes.IronBeam, 4), new CostPair(SurvRes.Pelt, 6) }; }
                    else { coinCost = 2900; reqPopCur = 26; matCosts = new[] { new CostPair(SurvRes.Blueprint, 8), new CostPair(SurvRes.IronIngot, 6), new CostPair(SurvRes.MetalPipe, 4) }; }
                    break;

                case NodeKind.Kitchen:
                    if (nextLv == 2) { coinCost = 300; reqPopCur = 7; matCosts = new[] { new CostPair(SurvRes.Plank, 8), new CostPair(SurvRes.Nails, 3) }; }
                    else { coinCost = 1250; reqPopCur = 15; matCosts = new[] { new CostPair(SurvRes.Blueprint, 4), new CostPair(SurvRes.IronIngot, 4), new CostPair(SurvRes.Plank, 10) }; }
                    break;

                case NodeKind.HerbHut:
                    if (nextLv == 2) { coinCost = 1350; reqPopCur = 17; matCosts = new[] { new CostPair(SurvRes.Blueprint, 4), new CostPair(SurvRes.IronBeam, 4), new CostPair(SurvRes.Herb, 6) }; }
                    else { coinCost = 2600; reqPopCur = 25; matCosts = new[] { new CostPair(SurvRes.Blueprint, 7), new CostPair(SurvRes.MetalPipe, 4), new CostPair(SurvRes.Parts, 4) }; }
                    break;

                case NodeKind.RelicWell:
                    if (nextLv == 2) { coinCost = 380; reqPopCur = 8; matCosts = new[] { new CostPair(SurvRes.Plank, 8), new CostPair(SurvRes.Nails, 4) }; }
                    else { coinCost = 1600; reqPopCur = 20; matCosts = new[] { new CostPair(SurvRes.Blueprint, 4), new CostPair(SurvRes.MetalPipe, 4) }; }
                    break;

                case NodeKind.Farm:
                    coinCost = 360 * nextLv;
                    reqPopCur = 7 + nextLv * 3;
                    matCosts = nextLv == 2
                        ? new[] { new CostPair(SurvRes.Plank, 8), new CostPair(SurvRes.Water, 4) }
                        : new[] { new CostPair(SurvRes.Blueprint, 3), new CostPair(SurvRes.MetalPipe, 2) };
                    break;

                default:
                    coinCost = 750 * nextLv;
                    reqPopCur = 10 + nextLv * 4;
                    matCosts = new[] { new CostPair(SurvRes.Blueprint, 3), new CostPair(SurvRes.IronBeam, 3), new CostPair(SurvRes.IronIngot, 3) };
                    break;
            }
        }

        public CostPair[] GetNextUpgradeCosts(CampNode n)
        {
            GetNextUpgradeSpec(n, out _, out _, out _, out CostPair[] mats);
            return mats;
        }

        public List<BuildCondition> GetUpgradeConditions(CampNode n, out int reqPlayerLv)
        {
            var list = new List<BuildCondition>(4);
            reqPlayerLv = GetRequiredPlayerLvForNext(n);
            GetNextUpgradeSpec(n, out int coins, out int reqPopCur, out int reqPopCap, out CostPair[] mats);
            if (mats == null) return list;

            if (reqPopCap > 0)
                list.Add(new BuildCondition { kind = CondKind.PopCap, own = PopCap, need = reqPopCap });
            else if (reqPopCur > 0)
                list.Add(new BuildCondition { kind = CondKind.PopCur, own = CurrentPop, need = reqPopCur });

            if (coins > 0)
                list.Add(new BuildCondition { kind = CondKind.Coins, own = Coins, need = coins });

            for (int i = 0; i < mats.Length; i++)
                list.Add(new BuildCondition { kind = CondKind.Resource, res = mats[i].res, own = Get(mats[i].res), need = mats[i].amount });

            return list;
        }

        public bool CheckRequirements(CampNode n, out string lockReason)
        {
            int reqLv = GetRequiredPlayerLvForNext(n);
            if (CampLevel < reqLv)
            {
                lockReason = $"玩家星级达到 {reqLv} 级后解锁建筑等级上限";
                return false;
            }
            GetNextUpgradeSpec(n, out _, out int reqPopCur, out int reqPopCap, out CostPair[] mats);
            if (mats == null)
            {
                lockReason = "已达最高等级";
                return false;
            }
            if (reqPopCap > 0 && PopCap < reqPopCap)
            {
                lockReason = $"供暖人口上限不足 ({PopCap}/{reqPopCap})，请先升级【篝火供暖塔】";
                return false;
            }
            if (reqPopCur > 0 && CurrentPop < reqPopCur)
            {
                lockReason = $"当前入驻人口不足 ({CurrentPop}/{reqPopCur})，请先升级【民居】";
                return false;
            }
            lockReason = null;
            return true;
        }

        public bool CanAffordUpgradeAll(CampNode n)
        {
            if (!CheckRequirements(n, out _)) return false;
            GetNextUpgradeSpec(n, out int coins, out _, out _, out CostPair[] mats);
            if (mats == null || Coins < coins) return false;
            return CanAfford(mats);
        }

        /// <summary>查询任意物品的生产工坊或采集来源（支撑水世界同款【去生产】/【获取途径】一键跳转！）。</summary>
        public string ResolveResourceProducer(SurvRes r, out string targetNodeId, out int recipeIdx, out bool isWorkshopCraft)
        {
            targetNodeId = null;
            recipeIdx = 0;
            isWorkshopCraft = false;

            if (TryMatchWorkshop(SawmillRecipes, "sawmill", "锯木木艺坊", r, out targetNodeId, out recipeIdx, out string desc))
            { isWorkshopCraft = true; return desc; }
            if (TryMatchWorkshop(SmithyRecipes, "smithy", "废铁锻造坊", r, out targetNodeId, out recipeIdx, out desc))
            { isWorkshopCraft = true; return desc; }
            if (TryMatchWorkshop(TailorRecipes, "tailor", "裁缝防具坊", r, out targetNodeId, out recipeIdx, out desc))
            { isWorkshopCraft = true; return desc; }
            if (TryMatchWorkshop(KitchenRecipes, "kitchen", "野味炊事房", r, out targetNodeId, out recipeIdx, out desc))
            { isWorkshopCraft = true; return desc; }
            if (TryMatchWorkshop(HerbHutRecipes, "herb_hut", "山林草药坊", r, out targetNodeId, out recipeIdx, out desc))
            { isWorkshopCraft = true; return desc; }

            switch (r)
            {
                case SurvRes.Wood: return "点击营地林缘树木【砍伐扩地】或采集散落原木堆";
                case SurvRes.Scrap: targetNodeId = "relic_truck"; return "拆解山道【锈铁残骸】或驾驶武装皮卡赴【五金汽修街】搜刮";
                case SurvRes.Water: targetNodeId = "relic_well"; return "在东南【净水石井】中持续汲取获得";
                case SurvRes.Veggie: targetNodeId = "farm_a"; return "在【1号开荒菜麻田】种植或林边浆果丛采摘";
                case SurvRes.Cloth: targetNodeId = "farm_a"; return "在【1号开荒菜麻田】轮作收获或赴【生鲜超市】搜刮";
                case SurvRes.Herb: targetNodeId = "farm_b"; return "在【2号林间药蔬圃】种植或砍林伴生采摘";
                case SurvRes.Fish: targetNodeId = "river_dock"; return "前往南岸【河畔钓鱼台】甩杆垂钓与鱼篓收获";
                case SurvRes.Meat:
                case SurvRes.Pelt: targetNodeId = "watchtower"; return "点击狩猎林缘野鹿/野猪，或建造【捕猎木哨塔】自动射猎双掉";
                case SurvRes.Parts: targetNodeId = "relic_truck"; return "赴丧尸城【急诊药房/汽修街】搜刮或拆解密林物资箱";
                case SurvRes.Blueprint: targetNodeId = "relic_truck"; return "赴【丧尸城搜刮】、【迎击尸潮】、【山口行商】或【提升星级】获得";
                default: return "在营地探索与生产中获得";
            }
        }

        static bool TryMatchWorkshop(WorkshopRecipe[] list, string nodeId, string shopTitle, SurvRes target, out string outNodeId, out int outIdx, out string outDesc)
        {
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i].outRes == target)
                {
                    outNodeId = nodeId;
                    outIdx = i;
                    outDesc = $"在【{shopTitle}】中生产获得（Lv.{list[i].unlockBuildingLv} 配方）";
                    return true;
                }
            }
            outNodeId = null; outIdx = 0; outDesc = null;
            return false;
        }

        public static bool IsWorkshopKind(NodeKind k)
        {
            return k == NodeKind.Sawmill || k == NodeKind.Smithy || k == NodeKind.Tailor || k == NodeKind.Kitchen || k == NodeKind.HerbHut;
        }

        public WorkshopRecipe[] GetWorkshopRecipes(CampNode n)
        {
            if (n == null) return Array.Empty<WorkshopRecipe>();
            switch (n.kind)
            {
                case NodeKind.Sawmill: return SawmillRecipes;
                case NodeKind.Smithy: return SmithyRecipes;
                case NodeKind.Tailor: return TailorRecipes;
                case NodeKind.Kitchen: return KitchenRecipes;
                case NodeKind.HerbHut: return HerbHutRecipes;
                default: return Array.Empty<WorkshopRecipe>();
            }
        }

        // =====================================================================
        // 建筑揭露、建造施工与敲锤落成
        // =====================================================================
        public void SyncRevealedNodes(ForestMap map, bool silent = false)
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                CampNode n = Nodes[i];
                if (n.state != NodeState.HiddenInForest) continue;
                if (CampLevel < n.unlockPlayerLv) continue;

                int cx = n.revealChunkX * ForestMap.ChunkSize + 1;
                int cy = n.revealChunkY * ForestMap.ChunkSize + 1;
                // 当达到对应主角星级且所在区块或邻近区块已开拓时自动显露地基！
                bool nearCleared = map.IsCleared(cx, cy) || map.IsCleared(cx - 2, cy) || map.IsCleared(cx + 2, cy) ||
                                   map.IsCleared(cx, cy - 2) || map.IsCleared(cx, cy + 2);
                if (nearCleared)
                {
                    n.state = NodeState.Discovered;
                    OnNodeVisualChanged?.Invoke(n);
                    if (!silent)
                    {
                        OnWorldToast?.Invoke($"解锁新地基：【{n.ruinTitle}】！", new Vector2(n.gx, n.gy));
                        TriggerDiscoveryDialog(n);
                    }
                }
            }
        }

        void TriggerDiscoveryDialog(CampNode n)
        {
            if (_storyFlags.Contains(n.id)) return;
            _storyFlags.Add(n.id);

            if (n.id == "tailor")
            {
                OnPlayComicDialog?.Invoke(new[]
                {
                    new ComicLine { leftBust = "bust_leader", rightBust = "bust_xiaotang", rightSpeaking = true, speaker = "小棠", text = "太好啦！支起这座【裁缝防具坊】，把菜麻田收的粗麻布和打猎剥的兽皮缝成防寒衣靴，难民们抢着拿铜币换，还能给咱们全队做护甲！" },
                });
            }
            else if (n.id == "relic_truck")
            {
                OnPlayComicDialog?.Invoke(new[]
                {
                    new ComicLine { leftBust = "bust_leader", rightBust = "", rightSpeaking = false, speaker = "阿澈", text = "嚯！旧皮卡大梁完好！打齐铆钉木梁修好它，就能下山去丧尸城搜刮急需的【工程手稿】与废铁零件了！" },
                });
            }
        }

        public void TriggerIntroDialog()
        {
            if (_storyFlags.Contains("intro")) return;
            _storyFlags.Add("intro");
            OnPlayComicDialog?.Invoke(new[]
            {
                new ComicLine { leftBust = "bust_leader", rightBust = "bust_xiaotang", rightSpeaking = false, speaker = "阿澈", text = "呼……总算甩开城里的尸群了！入秋山里夜风刺骨，先靠篝火供暖塔聚拢幸存者，把锯木坊支起来。" },
                new ComicLine { leftBust = "bust_leader", rightBust = "bust_xiaotang", rightSpeaking = true, speaker = "小棠", text = "嗯！只要多盖民居招揽人手，升级仓库、锻造坊、裁缝防具坊和草药坊，满足大家的吃穿伤病订单就能赚足铜币和经验！" },
            });
        }

        public bool StartBuildOrUpgrade(CampNode n, out string msg)
        {
            if (n.kind == NodeKind.RelicLoot && n.state == NodeState.Discovered)
            {
                n.state = NodeState.Depleted;
                Add(SurvRes.Scrap, 12, true);
                Add(SurvRes.Parts, 2, true);
                Add(SurvRes.Blueprint, 2, true);
                ScrapSalvagedTotal += 12;
                AddXp(25, new Vector2(n.gx, n.gy));
                msg = "拆解物资箱：废铁+12 医疗电子件+2 工程手稿+2";
                OnNodeVisualChanged?.Invoke(n);
                return true;
            }

            if (n.state == NodeState.Constructing || n.state == NodeState.ReadyToHammer)
            {
                msg = "正在施工中";
                return false;
            }

            if (!CheckRequirements(n, out string lockReason))
            {
                msg = lockReason;
                return false;
            }

            GetNextUpgradeSpec(n, out int coinCost, out _, out _, out CostPair[] costs);
            if (costs == null)
            {
                msg = "已达最高等级";
                return false;
            }
            if (Coins < coinCost)
            {
                msg = $"营地铜币不足 ({Coins}/{coinCost})，请完成【居民订单】获取！";
                return false;
            }
            if (!CanAfford(costs))
            {
                msg = "材料不足：" + FormatCosts(costs);
                return false;
            }

            Coins -= coinCost;
            Pay(costs);
            n.isUpgradingExisting = (n.state == NodeState.Active);
            n.state = NodeState.Constructing;
            n.buildTimer = 0f;
            n.buildDuration = Mathf.Max(2.0f, n.buildDuration * BuildTimeMultiplier);
            msg = "开始施工！";
            OnNodeVisualChanged?.Invoke(n);
            return true;
        }

        public void SpeedUpNodeBuild(CampNode n)
        {
            if (n == null || n.state != NodeState.Constructing) return;
            n.buildTimer = n.buildDuration;
            n.state = NodeState.ReadyToHammer;
            OnNodeVisualChanged?.Invoke(n);
        }

        public bool CompleteHammerNode(CampNode n)
        {
            if (n == null || n.state != NodeState.ReadyToHammer) return false;

            if (!n.isUpgradingExisting)
            {
                n.state = NodeState.Active;
                n.level = 1;
                if (n.kind == NodeKind.Farm)
                    n.farmPhase = FarmPhase.DryTilled;
            }
            else
            {
                n.state = NodeState.Active;
                n.level++;
                if (n.kind == NodeKind.Shelter)
                    n.activeTitle = n.level == 2 ? "加固原木议事厅" : "末日避难所木堡";
                else if (n.kind == NodeKind.Kitchen)
                    n.activeTitle = n.level == 2 ? "山林大灶房" : "野外熏肉大厨房";
                else if (n.kind == NodeKind.Warehouse)
                    n.activeTitle = n.level >= 3 ? "包铁加固总仓" : "木架物资仓库";
                else if (n.kind == NodeKind.HeatTower)
                    n.activeTitle = n.level >= 3 ? "高压锅炉供暖塔" : "石砌篝火供暖塔";
                else if (n.kind == NodeKind.RelicWell)
                {
                    n.prodOutCount = 3 + n.level * 2;
                    n.maxStored = 10 + n.level * 8;
                }
            }

            n.isUpgradingExisting = false;
            AddXp(30, new Vector2(n.gx, n.gy));
            OnNodeVisualChanged?.Invoke(n);
            OnNodeHammeredComplete?.Invoke(n);
            SyncUnlockedSkinsToHeroes(n);
            EnsureActiveOrders();
            return true;
        }

        void SyncUnlockedSkinsToHeroes(CampNode triggerNode)
        {
            OutfitTier unlockedTier = MaxUnlockedSkinTier;
            bool anyUpgraded = false;
            for (int i = 0; i < Heroes.Count; i++)
            {
                if (!Heroes[i].unlocked || Heroes[i].skinTier >= unlockedTier) continue;
                Heroes[i].skinTier = unlockedTier;
                Heroes[i].portraitKey = PuppetRegistry.ResolveBustKey(Heroes[i].characterId, unlockedTier);
                OnHeroSkinChanged?.Invoke(Heroes[i], unlockedTier);
                anyUpgraded = true;
            }
            if (anyUpgraded && triggerNode != null)
            {
                string tierLabel = unlockedTier == OutfitTier.Armored ? "3阶废土钢板护甲" : "2阶加固御寒皮衣";
                OnWorldToast?.Invoke($"防具升级！小队解锁【{tierLabel}】换装！", new Vector2(triggerNode.gx, triggerNode.gy));
            }
        }

        // =====================================================================
        // 作坊生产队列排产与交付
        // =====================================================================
        public bool IsRecipeUnlocked(CampNode workshop, WorkshopRecipe r)
        {
            if (workshop == null || r == null) return false;
            return workshop.level >= r.unlockBuildingLv && CampLevel >= r.unlockCampLv;
        }

        public bool CanAffordRecipe(WorkshopRecipe r)
        {
            if (r == null) return false;
            if (r.inAmt1 > 0 && Get(r.inRes1) < r.inAmt1) return false;
            if (r.inAmt2 > 0 && Get(r.inRes2) < r.inAmt2) return false;
            if (r.inAmt3 > 0 && Get(r.inRes3) < r.inAmt3) return false;
            return true;
        }

        public bool TryEnqueueRecipe(CampNode workshop, WorkshopRecipe r, out string msg)
        {
            if (workshop == null || r == null) { msg = "无效配方"; return false; }
            int slotCap = GetWorkshopSlotCap(workshop);
            if (workshop.queue.Count >= slotCap) { msg = $"生产队列已满 ({slotCap}/{slotCap})，升级作坊可扩充槽位"; return false; }
            if (workshop.level < r.unlockBuildingLv) { msg = $"需将【{workshop.activeTitle}】升至 Lv.{r.unlockBuildingLv} 解锁"; return false; }
            if (CampLevel < r.unlockCampLv) { msg = $"需主角星级达到 Lv.{r.unlockCampLv} 解锁"; return false; }
            if (!CanAffordRecipe(r))
            {
                msg = !string.IsNullOrEmpty(r.sourceHint)
                    ? r.sourceHint
                    : $"原料不足：需 {ResName(r.inRes1)}×{r.inAmt1}";
                return false;
            }

            if (r.inAmt1 > 0) Add(r.inRes1, -r.inAmt1);
            if (r.inAmt2 > 0) Add(r.inRes2, -r.inAmt2);
            if (r.inAmt3 > 0) Add(r.inRes3, -r.inAmt3);
            float dur = Mathf.Max(1.4f, r.duration / ProdSpeedMultiplier);
            if (workshop.kind == NodeKind.Kitchen && FindHero("laochentou")?.assigned == true)
                dur *= 0.80f;
            workshop.queue.Add(new QueueItem { recipe = r, timer = 0f, duration = dur });
            msg = $"已排产：{r.name}";
            return true;
        }

        public bool SpeedUpWorkshopFirstSlot(CampNode workshop)
        {
            if (workshop == null || workshop.queue.Count == 0) return false;
            QueueItem q = workshop.queue[0];
            workshop.queue.RemoveAt(0);
            DeliverRecipeOutput(workshop, q.recipe);
            return true;
        }

        void DeliverRecipeOutput(CampNode workshop, WorkshopRecipe r)
        {
            Add(r.outRes, r.outAmt, true);
            if (r.bonusRation > 0)
            {
                Add(SurvRes.Ration, r.bonusRation, false);
                RationsCookedTotal += r.bonusRation;
            }
            if (r.outRes == SurvRes.Plank) PlanksCraftedTotal += r.outAmt;
            if (r.outRes == SurvRes.LinenShirt || r.outRes == SurvRes.LeatherBoots || r.outRes == SurvRes.FurCoat || r.outRes == SurvRes.PlateArmor)
                ClothesCraftedTotal += r.outAmt;
            if (r.outRes == SurvRes.FurCoat)
            {
                FurCoatsCraftedTotal += r.outAmt;
                SyncUnlockedSkinsToHeroes(workshop);
            }
            if (r.outRes == SurvRes.PlateArmor)
            {
                PlateArmorsCraftedTotal += r.outAmt;
                SyncUnlockedSkinsToHeroes(workshop);
            }
            AddXp(12, new Vector2(workshop.gx, workshop.gy));
        }

        public bool TryCollectOrInteractNode(CampNode n, out string msg)
        {
            msg = null;
            if (n.state == NodeState.ReadyToHammer)
            {
                CompleteHammerNode(n);
                msg = $"落成：【{n.activeTitle}】！";
                return true;
            }
            if (n.state != NodeState.Active) return false;

            if (n.kind == NodeKind.Farm)
            {
                if (n.farmPhase == FarmPhase.DryTilled)
                {
                    if (Get(SurvRes.Water) < 1)
                    {
                        msg = "需要 净水×1 灌溉（请前往东南【净水石井】汲水）";
                        return false;
                    }
                    Add(SurvRes.Water, -1);
                    n.farmPhase = FarmPhase.Growing;
                    n.farmTimer = 0f;
                    msg = "已引山泉灌溉，作物生长中！";
                    OnWorldToast?.Invoke("山泉灌溉！", new Vector2(n.gx, n.gy));
                    OnNodeVisualChanged?.Invoke(n);
                    return true;
                }
                if (n.farmPhase == FarmPhase.Ripe)
                {
                    HarvestFarmNode(n);
                    msg = n.isHerbPlot ? "药圃丰收！山草药+4 · 鲜蔬+3" : "菜麻田丰收！鲜蔬+5 · 粗麻布+3";
                    return true;
                }
                msg = "作物幼苗生长中…";
                return false;
            }

            if (n.storedAmount > 0)
            {
                int amt = n.storedAmount;
                SurvRes r = n.storedRes;
                n.storedAmount = 0;
                Add(r, amt, true);
                if (n.prodOutCount2 > 0)
                    Add(n.prodOutRes2, Mathf.Max(1, amt / 2), true);
                msg = $"收取 {ResName(r)} +{amt}";
                return true;
            }

            return false;
        }

        void HarvestFarmNode(CampNode n)
        {
            int bonus = Mathf.Max(0, n.level - 1) * 2;
            if (n.isHerbPlot)
            {
                Add(SurvRes.Herb, 4 + bonus, true);
                Add(SurvRes.Veggie, 3 + bonus / 2, true);
            }
            else
            {
                Add(SurvRes.Veggie, 5 + bonus, true);
                Add(SurvRes.Cloth, 3 + bonus / 2, true);
            }
            AddXp(16, new Vector2(n.gx, n.gy));
            n.farmPhase = FarmPhase.DryTilled;
            OnNodeVisualChanged?.Invoke(n);
        }

        // =====================================================================
        // 四维居民生计订单系统（餐饮 / 穿戴服装 / 草药医疗 / 营房安居）+ 山口行商
        // =====================================================================
        public void EnsureActiveOrders()
        {
            while (ActiveOrders.Count < 3)
            {
                ActiveOrders.Add(GenerateOneOrder(ActiveOrders.Count));
            }
            for (int i = 0; i < ActiveOrders.Count; i++)
                ActiveOrders[i].slotIndex = i;
        }

        public bool RefreshAllOrders(bool freeForce = false)
        {
            if (!freeForce && OrderRefreshCooldown > 0f) return false;
            ActiveOrders.Clear();
            EnsureActiveOrders();
            OrderRefreshCooldown = CampLevel >= 6 ? 15f : 30f;
            return true;
        }

        ResidentDemand GenerateOneOrder(int slot)
        {
            _demandSeq++;
            OrderCategory cat = OrderCategory.Food;
            if (CampLevel >= 5 && slot == 2) cat = OrderCategory.Medical;
            else if (CampLevel >= 3 && slot == 1) cat = OrderCategory.Clothing;
            else if (CampLevel >= 4 && (_demandSeq % 4 == 0)) cat = OrderCategory.Housing;

            string[] speakers = { "阿澈", "小棠", "老陈头", "林砚", "投奔难民·老周", "采药婶子" };
            string[] busts = { "bust_leader", "bust_xiaotang", "bust_laochentou", "bust_linyan", "bust_cook", "bust_hunter" };
            int sIdx = (_demandSeq + slot) % speakers.Length;
            int wIdx = slot % 2;

            if (cat == OrderCategory.Clothing)
            {
                bool high = CampLevel >= 7 && (_demandSeq % 3 == 0);
                bool mid = CampLevel >= 5 && (_demandSeq % 2 == 0);
                SurvRes clothItem = high ? SurvRes.FurCoat : (mid ? SurvRes.LeatherBoots : SurvRes.LinenShirt);
                return new ResidentDemand
                {
                    slotIndex = slot, category = cat, walkerIndex = wIdx,
                    speakerName = speakers[sIdx], portraitKey = busts[sIdx],
                    categoryTag = "御寒穿戴单",
                    complaint = clothItem == SurvRes.FurCoat
                        ? "半夜山风往领口里灌，快去裁缝坊缝件厚实的加绒皮衣救命啊！"
                        : (clothItem == SurvRes.LeatherBoots ? "进林子捡柴全是荆棘刺，没双猎皮靴脚底板都扎烂了！" : "逃难穿来的卫衣全刮成布条了，求件耐磨麻布衣遮风！"),
                    thanksLine = "新衣裳太合身暖和了！干活浑身是劲！",
                    res1 = clothItem, amt1 = 1,
                    res2 = SurvRes.Water, amt2 = CampLevel >= 2 ? 1 : 0,
                    coinReward = high ? 420 : (mid ? 240 : 150),
                    xpReward = high ? 75 : (mid ? 48 : 34),
                };
            }

            if (cat == OrderCategory.Medical)
            {
                bool tea = CampLevel >= 7 && (_demandSeq % 2 == 1);
                return new ResidentDemand
                {
                    slotIndex = slot, category = cat, walkerIndex = wIdx,
                    speakerName = speakers[sIdx], portraitKey = busts[sIdx],
                    categoryTag = "伤病防疫单 · 高经验",
                    complaint = tea
                        ? "淋着秋雨巡了一宿山道，直打喷嚏，熬碗驱寒草本热饮发发汗吧！"
                        : "搬废铁不小心蹭破了胳膊，急需止血草药敷包消炎包扎！",
                    thanksLine = "草药敷上立马不疼了，救命之恩记心上了！",
                    res1 = tea ? SurvRes.WarmTea : SurvRes.HerbPoultice, amt1 = 1,
                    res2 = SurvRes.RoastVeggie, amt2 = 1,
                    coinReward = tea ? 320 : 210,
                    xpReward = tea ? 95 : 68, // 伤病单经验倍率最高！
                };
            }

            if (cat == OrderCategory.Housing)
            {
                return new ResidentDemand
                {
                    slotIndex = slot, category = cat, walkerIndex = wIdx,
                    speakerName = "新投奔难民", portraitKey = "bust_driver",
                    categoryTag = "营房安居单 · 赠手稿",
                    complaint = "拖家带口刚投奔山谷，地面返潮太冷，能给配张防潮木床铺和铆钉修屋顶吗？",
                    thanksLine = "终于睡上踏实木床了！这卷旧城搜来的工程手稿送给营地！",
                    res1 = SurvRes.WoodBed, amt1 = 1,
                    res2 = SurvRes.Nails, amt2 = 2,
                    coinReward = 280,
                    xpReward = 55,
                    blueprintBonus = 1,
                };
            }

            // 默认：温饱餐饮单（严格匹配当前星级可做菜品）
            SurvRes dish = SurvRes.RoastVeggie;
            int coin = 95, xp = 28;
            if (CampLevel >= 6 && (_demandSeq % 3 == 0)) { dish = SurvRes.SmokedMeat; coin = 340; xp = 65; }
            else if (CampLevel >= 3 && (_demandSeq % 2 == 0)) { dish = SurvRes.GrillFish; coin = 195; xp = 42; }
            else if (CampLevel >= 2 && (_demandSeq % 2 == 1)) { dish = SurvRes.VeggieSoup; coin = 140; xp = 34; }

            return new ResidentDemand
            {
                slotIndex = slot, category = OrderCategory.Food, walkerIndex = wIdx,
                speakerName = speakers[sIdx], portraitKey = busts[sIdx],
                categoryTag = "温饱餐饮单",
                complaint = $"忙活大半天肚子咕咕叫，最馋炊事房刚出锅的【{ResName(dish)}】！",
                thanksLine = "吃饱喝足！给你留了一袋营地铜币！",
                res1 = dish, amt1 = 1,
                res2 = SurvRes.Water, amt2 = (CampLevel >= 2 && dish == SurvRes.RoastVeggie) ? 1 : 0,
                coinReward = coin,
                xpReward = xp,
            };
        }

        public bool CanSatisfyOrder(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= ActiveOrders.Count) return false;
            ResidentDemand d = ActiveOrders[slotIndex];
            if (Get(d.res1) < d.amt1) return false;
            if (d.amt2 > 0 && Get(d.res2) < d.amt2) return false;
            return true;
        }

        public bool CanSatisfyDemand() => CanSatisfyOrder(0);

        public bool TrySatisfyOrder(int slotIndex, Vector2 survivorGrid, out string thanks)
        {
            thanks = null;
            if (!CanSatisfyOrder(slotIndex)) return false;
            ResidentDemand d = ActiveOrders[slotIndex];
            Add(d.res1, -d.amt1);
            if (d.amt2 > 0) Add(d.res2, -d.amt2);

            float chefBonus = FindHero("laochentou")?.assigned == true ? 1.15f : 1.0f;
            int finalCoins = Mathf.RoundToInt(d.coinReward * chefBonus);
            Coins += finalCoins;
            if (d.blueprintBonus > 0)
                Add(SurvRes.Blueprint, d.blueprintBonus, notifyToast: true);

            SatisfiedToday++;
            if (SatisfiedToday >= SatisfiedGoal)
            {
                SatisfiedToday = 0;
                Coins += 260;
                Add(SurvRes.Blueprint, 2, notifyToast: true);
                OnWorldToast?.Invoke("达成 5 单居民满意宝箱！铜币+260 · 工程手稿+2！", survivorGrid);
            }

            thanks = d.thanksLine;
            AddXp(d.xpReward, survivorGrid);
            ActiveOrders[slotIndex] = GenerateOneOrder(slotIndex);
            return true;
        }

        public bool TrySatisfyDemand(Vector2 survivorGrid, out string thanks)
            => TrySatisfyOrder(0, survivorGrid, out thanks);

        // ---------------- 山口行商大宗贸易（对标水世界货轮港口/不差钱商人） ----------------
        public void RefreshCaravanOffers()
        {
            CaravanOffers.Clear();
            CaravanOffers.Add(new CaravanOffer
            {
                title = "邻山伐木营采购御寒装",
                giveRes = CampLevel >= 7 ? SurvRes.FurCoat : SurvRes.LinenShirt,
                giveAmt = 2,
                coinGain = CampLevel >= 7 ? 780 : 320,
                blueprintGain = 1,
            });
            CaravanOffers.Add(new CaravanOffer
            {
                title = "巡山猎队换购急救草药",
                giveRes = CampLevel >= 9 ? SurvRes.MedKit : SurvRes.HerbPoultice,
                giveAmt = 2,
                coinGain = CampLevel >= 9 ? 960 : 420,
                blueprintGain = 2,
            });
            CaravanOffers.Add(new CaravanOffer
            {
                title = "商队干粮与床铺整批收购",
                giveRes = CampLevel >= 6 ? SurvRes.SmokedMeat : SurvRes.WoodBed,
                giveAmt = 2,
                coinGain = 560,
                blueprintGain = 1,
            });
        }

        public bool TryTradeCaravanOffer(int idx, out string msg)
        {
            if (!RadioRepaired)
            {
                msg = "请先在东北高地建造【山口行商电台】（Lv.6解锁）！";
                return false;
            }
            if (idx < 0 || idx >= CaravanOffers.Count) { msg = "无效商单"; return false; }
            CaravanOffer o = CaravanOffers[idx];
            if (o.traded) { msg = "该批货柜本轮已交割"; return false; }
            if (Get(o.giveRes) < o.giveAmt)
            {
                msg = $"库存不足：需 {ResName(o.giveRes)}×{o.giveAmt}";
                return false;
            }
            Add(o.giveRes, -o.giveAmt);
            o.traded = true;
            Coins += o.coinGain;
            if (o.blueprintGain > 0) Add(SurvRes.Blueprint, o.blueprintGain, notifyToast: true);
            AddXp(40, new Vector2(22.6f, 21.2f));
            msg = $"行商交割成功！铜币+{o.coinGain}" + (o.blueprintGain > 0 ? $" · 工程手稿+{o.blueprintGain}" : "");
            return true;
        }

        // =====================================================================
        // 每帧推进与自动帮工收菜
        // =====================================================================
        public void Tick(float dt)
        {
            float mul = ProdSpeedMultiplier;

            if (OrderRefreshCooldown > 0f)
                OrderRefreshCooldown = Mathf.Max(0f, OrderRefreshCooldown - dt);

            if (RadioRepaired)
            {
                CaravanRefreshTimer -= dt;
                if (CaravanRefreshTimer <= 0f)
                {
                    CaravanRefreshTimer = 90f;
                    RefreshCaravanOffers();
                }
            }

            if (HordeWarningTriggered && !HordeRepelled && HordeTimerSec > 0f)
                HordeTimerSec = Mathf.Max(0f, HordeTimerSec - dt);

            // 锯木木艺坊建成后：学徒定期巡林运回再生原木保底
            CampNode sawNode = Find("sawmill");
            if (sawNode != null && sawNode.state == NodeState.Active)
            {
                _patrolWoodTimer += dt * mul;
                if (_patrolWoodTimer >= 12f)
                {
                    _patrolWoodTimer = 0f;
                    if (Get(SurvRes.Wood) < StorageCap - 4)
                        Add(SurvRes.Wood, 2 + sawNode.level, notifyToast: false);
                }
            }

            for (int i = 0; i < Nodes.Count; i++)
            {
                CampNode n = Nodes[i];

                if (n.state == NodeState.Constructing)
                {
                    n.buildTimer += dt;
                    if (n.buildTimer >= n.buildDuration)
                    {
                        n.buildTimer = n.buildDuration;
                        n.state = NodeState.ReadyToHammer;
                        OnNodeVisualChanged?.Invoke(n);
                    }
                    continue;
                }

                if (n.state != NodeState.Active) continue;

                // 作坊队列推进
                if (n.queue.Count > 0)
                {
                    QueueItem q = n.queue[0];
                    q.timer += dt;
                    if (q.timer >= q.duration)
                    {
                        n.queue.RemoveAt(0);
                        DeliverRecipeOutput(n, q.recipe);
                    }
                    continue;
                }

                // 农田生长与难民帮工自动浇水/收割（当前人口 >= 7 自动托管！）
                if (n.kind == NodeKind.Farm)
                {
                    if (n.farmPhase == FarmPhase.DryTilled && AutoCollectUnlocked && Get(SurvRes.Water) >= 2)
                    {
                        Add(SurvRes.Water, -1);
                        n.farmPhase = FarmPhase.Growing;
                        n.farmTimer = 0f;
                        OnNodeVisualChanged?.Invoke(n);
                    }
                    else if (n.farmPhase == FarmPhase.Growing)
                    {
                        n.farmTimer += dt * mul;
                        if (n.farmTimer >= 6.0f)
                        {
                            if (AutoCollectUnlocked)
                                HarvestFarmNode(n);
                            else
                            {
                                n.farmPhase = FarmPhase.Ripe;
                                OnNodeVisualChanged?.Invoke(n);
                                OnWorldToast?.Invoke($"{n.activeTitle}成熟可收割！", new Vector2(n.gx, n.gy));
                            }
                        }
                    }
                    continue;
                }

                // 挂机采集建筑（净水石井 / 钓鱼台鱼篓 / 捕猎哨塔）
                if (n.prodInterval <= 0f || n.storedAmount >= n.maxStored) continue;
                if (n.prodInCount > 0 && Get(n.prodInRes) < n.prodInCount + 4) continue;

                n.prodTimer += dt * mul;
                if (n.prodTimer >= n.prodInterval)
                {
                    n.prodTimer = 0f;
                    if (n.prodInCount > 0) Add(n.prodInRes, -n.prodInCount);
                    n.storedRes = n.prodOutRes;
                    int outCnt = n.prodOutCount;
                    if ((n.kind == NodeKind.RiverDock || n.kind == NodeKind.Watchtower) && FindHero("linyan")?.assigned == true)
                        outCnt += 1;

                    // 难民人口达标后帮工自动搬运一半进仓，保留少量气泡供手动点按爽感
                    if (AutoCollectUnlocked && Get(n.prodOutRes) < StorageCap)
                    {
                        Add(n.prodOutRes, outCnt, notifyToast: false);
                        if (n.prodOutCount2 > 0) Add(n.prodOutRes2, n.prodOutCount2, notifyToast: false);
                    }
                    else
                    {
                        n.storedAmount = Mathf.Min(n.maxStored, n.storedAmount + outCnt);
                    }
                }
            }

            EvaluateQuestProgress();
        }

        // =====================================================================
        // 主线引导任务链（串起 砍树 -> 锯木坊 -> 炊事订单 -> 民居与仓库 -> 锻造与裁缝防具 -> 进城拿手稿！）
        // =====================================================================
        public struct QuestInfo
        {
            public int step;
            public string text;
            public int cur;
            public int max;
            public bool done;
            public Vector2 focusGrid;
            public string targetNodeId;
        }

        public QuestInfo GetCurrentQuest()
        {
            switch (QuestIndex)
            {
                case 0:
                {
                    int w = Get(SurvRes.Wood);
                    return new QuestInfo
                    {
                        step = 0, text = $"点选林缘树木砍伐收集原木 ({Mathf.Min(w, 20)}/20)",
                        cur = Mathf.Min(w, 20), max = 20, done = w >= 20,
                        focusGrid = new Vector2(18.6f, 19.8f), targetNodeId = null,
                    };
                }
                case 1:
                {
                    CampNode saw = Find("sawmill");
                    bool ok = saw != null && saw.state == NodeState.Active;
                    return new QuestInfo
                    {
                        step = 1, text = $"建造【锯木木艺坊】开启建材加工 ({(ok ? 1 : 0)}/1)",
                        cur = ok ? 1 : 0, max = 1, done = ok,
                        focusGrid = new Vector2(20.0f, 15.6f), targetNodeId = "sawmill",
                    };
                }
                case 2:
                {
                    int p = Mathf.Max(PlanksCraftedTotal, Get(SurvRes.Plank));
                    return new QuestInfo
                    {
                        step = 2, text = $"在锯木木艺坊切割 6 份厚木板 ({Mathf.Min(p, 6)}/6)",
                        cur = Mathf.Min(p, 6), max = 6, done = p >= 6,
                        focusGrid = new Vector2(20.0f, 15.6f), targetNodeId = "sawmill",
                    };
                }
                case 3:
                {
                    bool ok = CampXp > 0 && (SatisfiedToday >= 1 || CampLevel >= 2);
                    bool hasFood = Get(SurvRes.RoastVeggie) >= 1 || RationsCookedTotal >= 2;
                    return new QuestInfo
                    {
                        step = 3,
                        text = !hasFood
                            ? "在【野味炊事房】烹制炭烤山果串 (0/1)"
                            : $"完成 1 单【居民需求订单】赚取铜币与经验 ({(ok ? 1 : 0)}/1)",
                        cur = ok ? 1 : 0, max = 1, done = ok,
                        focusGrid = new Vector2(12.8f, 16.0f),
                        targetNodeId = !hasFood ? "kitchen" : null,
                    };
                }
                case 4:
                {
                    int hLv = GetNodeLevel("house_1", 1);
                    int wLv = GetNodeLevel("warehouse", 1);
                    bool hOk = hLv >= 2;
                    bool ok = hOk && wLv >= 2;
                    return new QuestInfo
                    {
                        step = 4,
                        text = !hOk
                            ? "升级【1号避风民居】至 Lv.2 扩充当前人口 (0/1)"
                            : "升级【营地物资仓库】至 Lv.2 提升储量上限 (0/1)",
                        cur = (hOk ? 1 : 0) + (wLv >= 2 ? 1 : 0), max = 2, done = ok,
                        focusGrid = !hOk ? new Vector2(12.1f, 19.3f) : new Vector2(19.3f, 19.0f),
                        targetNodeId = !hOk ? "house_1" : "warehouse",
                    };
                }
                case 5:
                {
                    CampNode well = Find("relic_well");
                    CampNode farm = Find("farm_a");
                    bool wOk = well != null && well.state == NodeState.Active;
                    bool fOk = farm != null && farm.state == NodeState.Active;
                    return new QuestInfo
                    {
                        step = 5,
                        text = !wOk ? "修复东南【净水石井】汲取山泉 (0/1)" : "开垦西南【1号菜麻田】产鲜蔬粗麻 (0/1)",
                        cur = (wOk ? 1 : 0) + (fOk ? 1 : 0), max = 2, done = wOk && fOk,
                        focusGrid = !wOk ? new Vector2(19.2f, 9.6f) : new Vector2(10.0f, 14.8f),
                        targetNodeId = !wOk ? "relic_well" : "farm_a",
                    };
                }
                case 6:
                {
                    CampNode sm = Find("smithy");
                    CampNode tl = Find("tailor");
                    bool smOk = sm != null && sm.state == NodeState.Active;
                    bool tlOk = tl != null && tl.state == NodeState.Active;
                    return new QuestInfo
                    {
                        step = 6,
                        text = !smOk ? "建造【废铁锻造坊】生产铁钉铆件 (0/1)" : "建造【裁缝防具坊】缝制麻布衣交订单 (0/1)",
                        cur = (smOk ? 1 : 0) + (tlOk ? 1 : 0), max = 2, done = smOk && tlOk,
                        focusGrid = !smOk ? new Vector2(20.4f, 12.6f) : new Vector2(17.2f, 12.0f),
                        targetNodeId = !smOk ? "smithy" : "tailor",
                    };
                }
                case 7:
                {
                    int htLv = GetNodeLevel("heat_tower", 1);
                    bool ok = htLv >= 2;
                    return new QuestInfo
                    {
                        step = 7, text = $"升级【篝火供暖塔】至 Lv.2 扩充人口上限 ({(ok ? 1 : 0)}/1)",
                        cur = ok ? 1 : 0, max = 1, done = ok,
                        focusGrid = new Vector2(14.3f, 18.8f), targetNodeId = "heat_tower",
                    };
                }
                case 8:
                {
                    bool ok = TruckRepaired && RoadOpened;
                    Vector2 fg = !TruckRepaired ? new Vector2(15.4f, 23.0f) : new Vector2(18.8f, 25.2f);
                    string tid = !TruckRepaired ? "relic_truck" : "relic_road_gate";
                    return new QuestInfo
                    {
                        step = 8, text = !TruckRepaired
                            ? "改装【末日武装皮卡】开启进城搜刮 (0/1)"
                            : "清理正北【盘山公路关卡】路障 (0/1)",
                        cur = ok ? 1 : 0, max = 1, done = ok,
                        focusGrid = fg, targetNodeId = tid,
                    };
                }
                default:
                {
                    bool ok = CityRunsCompleted >= 1;
                    return new QuestInfo
                    {
                        step = 9, text = $"驾车赴【丧尸城】搜刮工程手稿与工业物资 ({(ok ? 1 : 0)}/1)",
                        cur = ok ? 1 : 0, max = 1, done = ok,
                        focusGrid = new Vector2(15.4f, 23.0f), targetNodeId = "relic_truck",
                    };
                }
            }
        }

        void EvaluateQuestProgress()
        {
            QuestInfo q = GetCurrentQuest();
            QuestRewardPending = q.done;
        }

        public bool ClaimCurrentQuestReward(out string rewardMsg)
        {
            QuestInfo q = GetCurrentQuest();
            if (!q.done) { rewardMsg = null; return false; }

            int coinRw = 100 + QuestIndex * 40;
            Coins += coinRw;
            Add(SurvRes.Wood, 8, true);
            if (QuestIndex >= 3) Add(SurvRes.Blueprint, 1, true);
            AddXp(35, q.focusGrid);

            if (QuestIndex < 9) QuestIndex++;
            QuestRewardPending = false;
            rewardMsg = $"主线奖励：经验+35 · 铜币+{coinRw}" + (QuestIndex > 3 ? " · 工程手稿+1" : " · 原木+8");
            return true;
        }
    }
}
