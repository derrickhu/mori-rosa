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
        Ration = 26,       // 熟食口粮体力（砍树、进城与英雄升级消耗，烹饪任意菜品同步增加）
        Parts = 27,        // 医用/电子件（丧尸城药房/防卫总库、密林通信残骸）
        Blueprint = 28,    // 工程手稿（中后期建筑升阶核心蓝图，产自进城搜刮/尸潮/行商/升星）
        HeroMedal = 29,    // 英雄勋章（英雄升星核心道具，产自山口尸潮迎击波次与丧尸城高危空投箱！）

        // --- 农牧生态种子与有机堆肥（不占基础仓库容量，永不卡仓） ---
        SeedVeggie = 30,   // 山薯鲜蔬种（采集/收获自循环，高产鲜蔬）
        SeedFlax = 31,     // 韧性亚麻种（采集/收获自循环，高产粗麻布+伴生鲜蔬）
        SeedHerb = 32,     // 深山草药种（采集/收获自循环，高产山草药）
        SeedGolden = 33,   // 变异黄金良种（搜刮/尸潮/施肥变异，双倍全品类大丰收+铜币）
        Compost = 34,      // 有机堆肥（山林畜牧栏副产，反哺农田瞬间催熟50%+增产1.5倍！）
    }

    public enum HeroRarity
    {
        Rare = 0,          // 蓝 · 稀有
        Epic = 1,          // 紫 · 史诗
        Legendary = 2,     // 橙 · 传说
    }

    public enum DelegationPostKind
    {
        WorkshopSpeed = 0, // 作坊生产时间减少（4槽）
        BuildSpeed = 1,    // 建筑建造速度提高（2槽）
        FarmYield = 2,     // 农田采收时间减少（2槽）
        DefensePower = 3,  // 城防哨塔火力提高（3槽）
        CaravanBonus = 4,  // 行商贸易收益提高（2槽）
        TruckArmor = 5,    // 远征皮卡载重与装甲（2槽）
    }

    public sealed class DelegationPostState
    {
        public DelegationPostKind kind;
        public string title;
        public string iconKey;
        public string unitFormat;
        public int[] slotUnlockLevels;
        public string[] assignedHeroIds;

        public bool IsSlotUnlocked(int slot, int playerLv)
            => slot >= 0 && slot < slotUnlockLevels.Length && playerLv >= slotUnlockLevels[slot];
    }

    public struct HordeWaveSpec
    {
        public int wave;
        public string title;
        public bool hasBoss;
        public int zombieCount;
        public float zombieHp;
        public float zombieSpeed;
        public int recommendPower;
        public int medalReward;
        public int blueprintReward;
        public int coinReward;
        public int scrapReward;
    }

    public enum TacticalFormationId
    {
        StandardLine = 0,   // 基础防线阵 (初始解锁)
        SpearAssault = 1,   // 尖锥突击阵 (通关第4关解锁)
        IronBastion = 2,    // 铁桶抗性阵 (通关第10关解锁)
        VolleyFire = 3,     // 火力齐射阵 (通关第18关解锁)
    }

    public enum HeroCombatRole
    {
        Vanguard = 0,       // 重装先锋 (雷猛: 近战冲锋劈砍)
        Guardian = 1,       // 铁壁重卫 (大锤: 前排嘲讽护盾)
        Demolition = 2,     // 爆破工匠 (小棠: 中程燃烧瓶范围灼烧)
        Ranger = 3,         // 山林猎手 (林砚: 后排高速连弩穿透)
        Gunsmith = 4,       // 火力枪匠 (铁叔: 中排霰弹击退压制)
        Medic = 5,          // 战地医官 (老陈头: 后排急救群疗护盾)
    }

    public struct SynergyAuraInfo
    {
        public string id;
        public string title;
        public string desc;
        public bool active;
        public float atkMulBonus;
        public float hpMulBonus;
        public float dmgRedBonus;
        public float atkSpdBonus;
        public float rageGainBonus;
    }

    public struct BattleStageSpec
    {
        public int stage;              // 1..40
        public string title;           // e.g. "第 1 关 · 山口游尸"
        public string subtitle;        // e.g. "清剿逼近断桥防线的先遣尸群"
        public bool isBossStage;       // stage == 3 || stage % 5 == 0
        public bool hasBacklineFlanker;// stage >= 4 && stage % 2 == 0 (跳尸突袭后排)
        public int recommendedPower;
        public int enemyTotalCount;
        public int rewardXp;
        public int rewardCoins;
        public int rewardMedals;
        public int rewardBlueprints;
        public int unlockHeroIdx;      // -1 if none, else hero idx unlocked on first clear
        public int unlockFormationIdx; // -1 if none, else (int)TacticalFormationId unlocked on first clear
        public string milestoneTag;    // e.g. "首通招募：大锤（铁壁重卫）" or "首通解锁：尖锥突击阵" or ""
    }

    public struct SlotStatBuff
    {
        public string shortTag;   // e.g. "生命+15%" / "减伤+25%" / "暴伤+25%" / "攻速+25%" / ""
        public bool highlighted;
        public float hpBonus;
        public float atkBonus;
        public float dmgReduction;
        public float atkSpeedBonus;
        public float critBonus;
        public float startRage;
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
        RelicWell,        // ⑩ 净水石井（持续产出山泉净水，Lv.2+接导流管解锁自动滴灌）
        Farm,             // ⑪ 生态选种农田（2→4垄自由播种鲜蔬/亚麻/草药/变异金种）
        Ranch,            // ⑫ 山林畜牧栏（投喂农田鲜蔬+井水 -> 稳定产鲜肉+兽毛皮+有机堆肥反哺农田）
        RiverDock,        // ⑬ 河畔钓鱼台（产出山溪鲜鱼）
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

    public enum CropSeedKind
    {
        Veggie = 0, // 山薯鲜蔬种
        Flax = 1,   // 韧性亚麻种
        Herb = 2,   // 深山草药种
        Golden = 3, // 变异黄金良种
    }

    public sealed class FarmPlotSlot
    {
        public int index;
        public FarmPhase phase = FarmPhase.DryTilled;
        public CropSeedKind seedKind = CropSeedKind.Veggie;
        public CropSeedKind lastSeedKind = CropSeedKind.Veggie;
        public float timer;
        public float duration = 7.5f;
        public bool watered;
        public bool fertilized;
    }

    public enum RanchAnimalKind
    {
        SnowRabbit = 0, // Lv.1 长毛雪兔（主毛皮+嫩肉+堆肥）
        WildBoar = 1,   // Lv.2 黑鬃山猪（高产鲜肉+毛皮+双堆肥）
        AntlerDeer = 2, // Lv.3 梅花绒鹿（高产毛皮+鲜肉+双堆肥）
    }

    public sealed class RanchPenSlot
    {
        public int index;
        public RanchAnimalKind animal = RanchAnimalKind.SnowRabbit;
        public bool feeding;
        public bool readyToCollect;
        public float timer;
        public float duration = 9.0f;
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
        public HeroRarity rarity = HeroRarity.Rare;
        public int level = 1;
        public int stars = 1;
        public int baseAtk = 24;
        public int baseHp = 180;
        public bool isMelee;
        public string projKey = "proj_arrow";
        public string skillName = "战术突击";
        public string skillDesc = "对前方尸群造成高额伤害";
        public DelegationPostKind prefPost1 = DelegationPostKind.WorkshopSpeed;
        public DelegationPostKind prefPost2 = DelegationPostKind.DefensePower;

        public string portraitKey;
        public string buffDesc;
        public bool unlocked;
        public bool assigned;
        public string targetBuildingTitle;
        public OutfitTier skinTier = OutfitTier.Refugee;

        public string RarityTag => rarity == HeroRarity.Legendary ? "传说" : (rarity == HeroRarity.Epic ? "史诗" : "稀有");

        public float StarMult
        {
            get
            {
                float[] m = { 1.0f, 1.0f, 1.25f, 1.55f, 1.90f, 2.35f };
                return m[Mathf.Clamp(stars, 1, 5)];
            }
        }

        public float SkinAtkMult => skinTier == OutfitTier.Armored ? 1.40f : (skinTier == OutfitTier.Reinforced ? 1.15f : 1.0f);
        public float SkinHpMult => skinTier == OutfitTier.Armored ? 1.75f : (skinTier == OutfitTier.Reinforced ? 1.30f : 1.0f);

        public int Attack => Mathf.RoundToInt(baseAtk * (1f + 0.08f * (level - 1)) * StarMult * SkinAtkMult);
        public int MaxHp => Mathf.RoundToInt(baseHp * (1f + 0.09f * (level - 1)) * StarMult * SkinHpMult);
        public int CombatPower => Attack * 4 + Mathf.RoundToInt(MaxHp * 0.5f);

        public float GetDelegationScore(DelegationPostKind post)
        {
            float rBase = rarity == HeroRarity.Legendary ? 16f : (rarity == HeroRarity.Epic ? 12f : 8.5f);
            float spec = (post == prefPost1 || post == prefPost2) ? 1.28f : 1.0f;
            return rBase * StarMult * (1f + 0.03f * (level - 1)) * spec;
        }
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

        public bool requiresSiteSalvage;
        public bool siteUncovered;
        public bool siteSalvageCleared;
        public string salvageLabel = "林间散落物资";
        public SurvRes salvageBonusRes = SurvRes.Wood;
        public int salvageBonusAmt = 5;
        public string compassDir = "林缘";

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
        public const int MaxCampLevel = 15;
        public const int MaxBattleStages = 40;

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
        public readonly List<DelegationPostState> DelegationPosts = new List<DelegationPostState>();
        public readonly List<string> CombatSquadIds = new List<string>(); // 最多3人出战（与岗位委任零冲突共享！）
        public readonly List<ResidentDemand> ActiveOrders = new List<ResidentDemand>();
        public readonly List<CaravanOffer> CaravanOffers = new List<CaravanOffer>();
        public readonly List<FarmPlotSlot> FarmPlots = new List<FarmPlotSlot>(4);
        public readonly List<RanchPenSlot> RanchPens = new List<RanchPenSlot>(4);
        public bool AutoReplantFarm = true;
        public bool AutoFeedRanch = true;
        readonly System.Random _ecoRng = new System.Random(2026);

        public int CampLevel = 1;
        public int CampXp = 0;
        public int Coins = 120;
        public int HordeWaveIndex = 1;
        int _hordeWaveCleared = 0;
        public int HordeWaveCleared
        {
            get => _hordeWaveCleared;
            set
            {
                _hordeWaveCleared = Mathf.Clamp(value, 0, MaxBattleStages);
                HordeWaveIndex = Mathf.Max(1, _hordeWaveCleared + 1);
            }
        }
        public int MaxClearedWave
        {
            get => _hordeWaveCleared;
            set => HordeWaveCleared = value;
        }
        public int CurrentBattleStage => Mathf.Clamp(HordeWaveCleared + 1, 1, MaxBattleStages);
        public bool AutoChainNextStage;
        public TacticalFormationId SelectedFormation = TacticalFormationId.StandardLine;
        public readonly int[] FormationSlots = new int[9];
        bool _gmUnlockFullSquad;
        bool _manualFormationEdited;
        public float SweepCooldownSec = 0f;

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
        public int NailsCraftedTotal;
        public int RationsCookedTotal;
        public int ClothesCraftedTotal;
        public int FurCoatsCraftedTotal;
        public int PlateArmorsCraftedTotal;
        public int MedsCraftedTotal;
        public int ScrapSalvagedTotal;
        public int FishCaughtTotal;
        public int FarmHarvestedTotal;
        public int RanchCollectedTotal;
        public int CompostUsedTotal;
        public int HeroLevelUpsTotal;
        public int OrdersCompletedTotal;
        public int MedicalOrdersCompletedTotal;
        public int CaravanTradesTotal;
        public int CityRunsCompleted;
        float _patrolWoodTimer;
        float _patrolScrapTimer;
        readonly HashSet<string> _storyFlags = new HashSet<string>();

        public event Action<string, Vector2> OnWorldToast;
        public event Action<SurvRes, int> OnResourceGained;
        public event Action<int, Vector2> OnXpBurst;
        public event Action<int, string[]> OnCampLevelUp;
        public event Action<CampNode> OnNodeVisualChanged;
        public event Action<CampNode> OnNodeForestUncovered;
        public event Action<CampNode> OnNodeHammeredComplete;
        public event Action<HeroSpec> OnHeroUnlocked;
        public event Action<HeroSpec, OutfitTier> OnHeroSkinChanged;
        public event Action OnDelegationChanged;
        public event Action OnHordeCrisisTriggered;
        public event Action<ComicLine[]> OnPlayComicDialog;

        // ---------------- 双轨人口 & 仓库储量上限计算 ----------------
        /// <summary>当前营地实有入驻难民人口（议事厅+供暖塔提供初始 4 人留守，1~3 号民居落成后递进累加，用于解锁作坊/仓库升级与自动收菜）。</summary>
        public int CurrentPop
        {
            get
            {
                int p = 4;
                for (int i = 0; i < Nodes.Count; i++)
                {
                    CampNode n = Nodes[i];
                    if (n.kind == NodeKind.Dwelling && n.state == NodeState.Active && n.level > 0)
                        p += GetDwellingPopForLevel(n.id, n.level);
                }
                return p;
            }
        }

        public int PopCur => CurrentPop;

        /// <summary>顶栏展示用当前人口（与 CurrentPop 一致）。</summary>
        public int SurvivorsCount => CurrentPop;

        /// <summary>当前营地供暖承载人口上限 MAX（由【篝火供暖塔】与【避难所议事厅】共同决定）。</summary>
        public int PopCap
        {
            get
            {
                int heatLv = GetNodeLevel("heat_tower", 1);
                int baseCap = heatLv >= 4 ? 38 : (heatLv == 3 ? 28 : (heatLv == 2 ? 15 : 8));
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
            return r != SurvRes.Ration && r != SurvRes.Blueprint && r != SurvRes.Parts && r != SurvRes.HeroMedal &&
                   r != SurvRes.SeedVeggie && r != SurvRes.SeedFlax && r != SurvRes.SeedHerb && r != SurvRes.SeedGolden &&
                   r != SurvRes.Compost;
        }

        /// <summary>① 水利滴灌联动：净水石井 Lv.2+（或石井激活且农田升至 Lv.3 铺设导流管）解锁全田垄自动滴灌 +35% 生长提速！</summary>
        public bool HasAutoIrrigation
        {
            get
            {
                if (!IsActiveOrUpgrading("relic_well")) return false;
                return GetNodeLevel("relic_well", 0) >= 2 || GetNodeLevel("farm_a", 0) >= 3;
            }
        }

        /// <summary>② 供暖塔温室防寒联动：供暖塔 Lv.2/3/4 为农田与畜牧栏辐射暖温，成熟与饲养提速 +15% / +25% / +40%！</summary>
        public int GreenhouseBonusPct
        {
            get
            {
                int htLv = GetNodeLevel("heat_tower", 1);
                if (htLv >= 4) return 40;
                if (htLv == 3) return 25;
                if (htLv == 2) return 15;
                return 0;
            }
        }
        public float GreenhouseMultiplier => 1f + GreenhouseBonusPct * 0.01f;

        /// <summary>③ 居民温饱与防疫士气联动：0=普通维持(1.0x)，1=温饱高涨(+20%)，2=丰衣足食士气沸腾(+32% & 订单铜币+15%)！</summary>
        public int MoraleTier
        {
            get
            {
                int needRation = Mathf.Max(4, CurrentPop);
                if (Get(SurvRes.Ration) < needRation) return 0;
                int medStock = Get(SurvRes.HerbPoultice) + Get(SurvRes.WarmTea) + Get(SurvRes.MedKit);
                if (CurrentPop >= 6 && medStock >= 1) return 2;
                return 1;
            }
        }

        public float CampMoraleMultiplier
        {
            get
            {
                int t = MoraleTier;
                return t == 2 ? 1.32f : (t == 1 ? 1.20f : 1.0f);
            }
        }

        public string MoraleStatusSummary
        {
            get
            {
                int t = MoraleTier;
                if (t == 2) return "士气沸腾 +32% (粮药充足)";
                if (t == 1) return "温饱高士气 +20% (口粮充沛)";
                return $"普通士气 (存够口粮≥{Mathf.Max(4, CurrentPop)}激活+20%提速)";
            }
        }

        /// <summary>普通居民民生劳力加成：每1名实有人口提升自然挂机与农田采集速率 +2.5%。</summary>
        public float PopulationGatherMultiplier => (1f + 0.025f * CurrentPop) * CampMoraleMultiplier;

        /// <summary>普通居民订单繁荣度溢价：每多1名居民提升订单铜币与经验报酬 +4%（士气沸腾额外 +15%）。</summary>
        public float OrderProsperityMultiplier => (1f + 0.04f * Mathf.Max(0, CurrentPop - 4)) * (MoraleTier == 2 ? 1.15f : 1.0f);

        /// <summary>同屏可见普通流民数量（随实有人口动态增加，封顶 10 人保证清爽不卡顿）。</summary>
        public int VisibleVillagerCount => Mathf.Clamp(CurrentPop / 3, 2, 10);

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
                // 避难所+供暖塔自带初始 4 人；1号民居建成 +3 (4->7)，升阶至 8/10/14 完美衔接全等级人口门控
                int[] v = { 0, 3, 4, 6, 10 };
                return v[Mathf.Clamp(lv, 0, v.Length - 1)];
            }
            if (id == "house_2")
            {
                int[] v = { 0, 5, 8, 12, 16 };
                return v[Mathf.Clamp(lv, 0, v.Length - 1)];
            }
            int[] v3 = { 0, 8, 12, 16, 16 };
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
            string resolvedBust = PuppetRegistry.ResolveBustKey(h.characterId, targetTier);
            if (h.id == "leimeng")
                resolvedBust = "bust_vil_0";
            else if (h.id == "tieshu")
                resolvedBust = "survivor_tieshu";
            h.portraitKey = resolvedBust;
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

        public float GetPostScoreSum(DelegationPostKind kind)
        {
            float sum = 0f;
            for (int i = 0; i < DelegationPosts.Count; i++)
            {
                DelegationPostState p = DelegationPosts[i];
                if (p.kind != kind) continue;
                for (int s = 0; s < p.assignedHeroIds.Length; s++)
                {
                    if (!p.IsSlotUnlocked(s, CampLevel)) continue;
                    HeroSpec h = FindHero(p.assignedHeroIds[s]);
                    if (h != null && h.unlocked) sum += h.GetDelegationScore(kind);
                }
            }
            return sum;
        }

        public float ProdSpeedMultiplier
        {
            get
            {
                int genLv = GetNodeLevel("relic_generator", 0);
                float m = genLv >= 3 ? 1.65f : (genLv == 2 ? 1.45f : (genLv == 1 ? 1.25f : 1.0f));
                float postScore = GetPostScoreSum(DelegationPostKind.WorkshopSpeed);
                return m * CampMoraleMultiplier * (1f + postScore * 0.014f);
            }
        }

        public float BuildTimeMultiplier
        {
            get
            {
                float postScore = GetPostScoreSum(DelegationPostKind.BuildSpeed);
                return Mathf.Clamp(1f / ((1f + postScore * 0.016f) * CampMoraleMultiplier), 0.32f, 1.0f);
            }
        }

        public float FarmSpeedMultiplier
            => PopulationGatherMultiplier * GreenhouseMultiplier * (HasAutoIrrigation ? 1.35f : 1.0f) * (1f + GetPostScoreSum(DelegationPostKind.FarmYield) * 0.016f);

        public float RanchSpeedMultiplier
            => PopulationGatherMultiplier * GreenhouseMultiplier * (1f + GetPostScoreSum(DelegationPostKind.FarmYield) * 0.016f);

        public float DefensePostBonusMultiplier
            => 1f + GetPostScoreSum(DelegationPostKind.DefensePower) * 0.018f;

        public float CaravanCoinBonusMultiplier
        {
            get
            {
                float rMul = GetNodeLevel("radio_post", 0) >= 2 ? 1.30f : 1.0f;
                return rMul * (1f + GetPostScoreSum(DelegationPostKind.CaravanBonus) * 0.012f);
            }
        }

        public int TruckMaxCargo
        {
            get
            {
                int trLv = GetNodeLevel("relic_truck", 1);
                int baseC = trLv >= 2 ? 145 : 95;
                int bonus = Mathf.RoundToInt(GetPostScoreSum(DelegationPostKind.TruckArmor) * 1.1f);
                return baseC + bonus;
            }
        }

        public int TruckMaxHp
        {
            get
            {
                int trLv = GetNodeLevel("relic_truck", 1);
                int baseHp = trLv >= 2 ? 420 : 260;
                int squadBonus = 0;
                EnsureCombatSquad();
                for (int i = 0; i < CombatSquadIds.Count; i++)
                {
                    HeroSpec h = FindHero(CombatSquadIds[i]);
                    if (h != null) squadBonus += Mathf.RoundToInt(h.MaxHp * 0.35f);
                }
                int postBonus = Mathf.RoundToInt(GetPostScoreSum(DelegationPostKind.TruckArmor) * 4.5f);
                return baseHp + squadBonus + postBonus;
            }
        }

        public int BarricadeMaxHp
        {
            get
            {
                int bLv = GetNodeLevel("barricade", 0);
                int baseHp = bLv >= 2 ? 560 : (bLv == 1 ? 340 : 200);
                return Mathf.RoundToInt(baseHp * DefensePostBonusMultiplier);
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

            // 开局基础逃难物资：原木×10、口粮×6、山果鲜蔬×2、粗麻旧布×2、铜币×120 + 新手初始种子包
            _res[SurvRes.Wood] = 10;
            _res[SurvRes.Ration] = 6;
            _res[SurvRes.Veggie] = 2;
            _res[SurvRes.Cloth] = 2;
            _res[SurvRes.SeedVeggie] = 3;
            _res[SurvRes.SeedFlax] = 2;
            _res[SurvRes.SeedHerb] = 1;

            _revealedRes.Add(SurvRes.Wood);
            _revealedRes.Add(SurvRes.Ration);

            InitNodes();
            InitFarmAndRanchSlots();
            CitySites = BuildCitySites();
            SawmillRecipes = BuildSawmillRecipes();
            SmithyRecipes = BuildSmithyRecipes();
            TailorRecipes = BuildTailorRecipes();
            KitchenRecipes = BuildKitchenRecipes();
            HerbHutRecipes = BuildHerbHutRecipes();
            InitHeroes();
            InitDelegationPosts();
            AutoAssignHeroes();
            EnsureCombatSquad();
            EnsureActiveOrders();
            RefreshCaravanOffers();
        }

        public static int GetXpNeed(int level)
        {
            int[] table = { 0, 70, 130, 200, 290, 400, 530, 680, 850, 1050, 1280, 1540, 1820, 2120, 2450 };
            return level > 0 && level < table.Length ? table[level] : 2800;
        }

        public void AddXp(int amount) => AddXp(amount, new Vector2(16.0f, 19.5f));

        public void AddXp(int amount, Vector2 worldGrid)
        {
            if (amount <= 0) return;
            CampXp += amount;
            OnXpBurst?.Invoke(amount, worldGrid);

            while (CampLevel < MaxCampLevel && CampXp >= GetXpNeed(CampLevel))
            {
                CampXp -= GetXpNeed(CampLevel);
                CampLevel++;
                // 每次升星保底赠送铜币，Lv.4+ 赠工程手稿，Lv.7+ 赠医疗电子件，确保纯种田玩家永不卡死！
                Coins += 80 * CampLevel;
                if (CampLevel >= 4)
                {
                    int bpRw = CampLevel >= 13 ? 7 : (CampLevel >= 9 ? 5 : 3);
                    Add(SurvRes.Blueprint, bpRw, notifyToast: true);
                }
                if (CampLevel >= 7)
                    Add(SurvRes.Parts, 2, notifyToast: true);

                string[] unlocks = GetUnlocksForLevel(CampLevel);
                OnCampLevelUp?.Invoke(CampLevel, unlocks);

                CheckLevelHeroUnlocks(silent: false);

                if (CampLevel == 2)
                {
                    Add(SurvRes.HeroMedal, 2, notifyToast: true);
                }
                else if (CampLevel == 3)
                {
                    Add(SurvRes.HeroMedal, 3, notifyToast: true);
                }
                else if (CampLevel == 4)
                {
                    if (!HordeWarningTriggered)
                    {
                        HordeWarningTriggered = true;
                        OnHordeCrisisTriggered?.Invoke();
                    }
                }
                else if (CampLevel == 5)
                {
                    Add(SurvRes.HeroMedal, 2, notifyToast: true);
                }
                else if (CampLevel == 7 || CampLevel == 9 || CampLevel == 10)
                {
                    Add(SurvRes.HeroMedal, 3, notifyToast: true);
                }
                else if (CampLevel == 12)
                {
                    Add(SurvRes.HeroMedal, 4, notifyToast: true);
                }
                else if (CampLevel == 15)
                {
                    Add(SurvRes.HeroMedal, 5, notifyToast: true);
                }
                EnsureCombatSquad();

                EnsureActiveOrders();
                RefreshCaravanOffers();
            }
        }

        public void CheckLevelHeroUnlocks(bool silent = false)
        {
            UnlockHero("dachui", silent: true);
            UnlockHero("xiaotang", silent: true);
            if (CampLevel >= 3 || HordeWaveCleared >= 2)
                UnlockHero("leimeng", silent);
            if (CampLevel >= 4 || HordeWaveCleared >= 5)
                UnlockHero("linyan", silent);
            if (CampLevel >= 7 || HordeWaveCleared >= 9)
                UnlockHero("tieshu", silent);
            if (CampLevel >= 10 || HordeWaveCleared >= 13)
                UnlockHero("laochentou", silent);
        }

        static string[] GetUnlocksForLevel(int lv)
        {
            switch (lv)
            {
                case 2: return new[] { "净水石井", "山林畜牧栏", "山泉鲜蔬汤" };
                case 3: return new[] { "废铁锻造坊", "裁缝防具坊", "生态农田Lv.2(3垄)" };
                case 4: return new[] { "河畔钓鱼台", "石井Lv.2自动滴灌", "篝火供暖塔Lv.2" };
                case 5: return new[] { "2号原木民居", "尖木拒马防线", "防潮木床铺" };
                case 6: return new[] { "捕猎木哨塔", "畜牧栏Lv.2(山猪)", "防荆棘猎皮靴" };
                case 7: return new[] { "山林草药坊", "包铁承重梁", "止血草药敷包" };
                case 8: return new[] { "末日武装皮卡", "盘山公路关卡", "锻打铁锭·松木熏肉" };
                case 9: return new[] { "山涧发电房", "供暖塔Lv.3", "加绒御寒皮衣" };
                case 10: return new[] { "山口行商电台", "驱寒草本热饮", "2号民居Lv.3" };
                case 11: return new[] { "3号幸存者木楼", "导流金属管", "农田Lv.3(4垄满田)" };
                case 12: return new[] { "供暖塔Lv.4(温室+40%)", "畜牧栏Lv.3(梅花鹿)", "发电房Lv.2" };
                case 13: return new[] { "抗尸毒急救箱", "重甲皮卡Lv.2", "行商电台Lv.2" };
                case 14: return new[] { "议事厅Lv.3末日木堡", "废土钢板护甲", "农田Lv.4满阶" };
                case 15: return new[] { "物资仓库Lv.5(1200)", "发电房Lv.3(+65%)", "3号木楼满阶" };
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
                case SurvRes.HeroMedal: return "英雄勋章";
                case SurvRes.SeedVeggie: return "山薯鲜蔬种";
                case SurvRes.SeedFlax: return "韧性亚麻种";
                case SurvRes.SeedHerb: return "深山草药种";
                case SurvRes.SeedGolden: return "变异黄金种";
                case SurvRes.Compost: return "有机堆肥";
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

        public void AddRes(SurvRes r, int delta, bool notifyToast = true) => Add(r, delta, notifyToast);

        public CampNode Find(string id)
        {
            for (int i = 0; i < Nodes.Count; i++)
                if (Nodes[i].id == id) return Nodes[i];
            return null;
        }

        public CampNode FindNode(string id) => Find(id);

        public CampNode FindByKind(NodeKind kind)
        {
            for (int i = 0; i < Nodes.Count; i++)
                if (Nodes[i].kind == kind) return Nodes[i];
            return null;
        }

        public int FindHeroIndex(string id)
        {
            for (int i = 0; i < Heroes.Count; i++)
                if (Heroes[i].id == id) return i;
            return -1;
        }

        public HeroSpec FindHero(string id)
        {
            int idx = FindHeroIndex(id);
            return idx >= 0 ? Heroes[idx] : null;
        }

        public void UnlockHero(string id, bool silent = false)
        {
            int idx = FindHeroIndex(id);
            if (idx < 0 || Heroes[idx].unlocked) return;
            HeroSpec h = Heroes[idx];
            h.unlocked = true;
            if (FindHeroSlot(idx) < 0 && GetDeployedHeroCount() < GetMaxDeployHeroCount())
            {
                PlaceHeroIntoBestOpenSlot(idx);
                SyncCombatSquadFromFormation();
            }
            EnsureCombatSquad();
            if (!silent) OnHeroUnlocked?.Invoke(h);
        }

        void InitDelegationPosts()
        {
            DelegationPosts.Clear();
            DelegationPosts.Add(new DelegationPostState
            {
                kind = DelegationPostKind.WorkshopSpeed, title = "作坊生产时间减少", iconKey = "sawmill_shed",
                slotUnlockLevels = new[] { 1, 3, 7, 12 }, assignedHeroIds = new string[4],
            });
            DelegationPosts.Add(new DelegationPostState
            {
                kind = DelegationPostKind.BuildSpeed, title = "建筑建造速度提高", iconKey = "ico_hammer",
                slotUnlockLevels = new[] { 2, 8 }, assignedHeroIds = new string[2],
            });
            DelegationPosts.Add(new DelegationPostState
            {
                kind = DelegationPostKind.FarmYield, title = "农牧庄园采收提速", iconKey = "farm_2",
                slotUnlockLevels = new[] { 3, 9 }, assignedHeroIds = new string[2],
            });
            DelegationPosts.Add(new DelegationPostState
            {
                kind = DelegationPostKind.DefensePower, title = "城防哨塔火力提高", iconKey = "hunter_tower",
                slotUnlockLevels = new[] { 4, 9, 13 }, assignedHeroIds = new string[3],
            });
            DelegationPosts.Add(new DelegationPostState
            {
                kind = DelegationPostKind.TruckArmor, title = "远征皮卡载重与装甲", iconKey = "truck_active",
                slotUnlockLevels = new[] { 6, 11 }, assignedHeroIds = new string[2],
            });
            DelegationPosts.Add(new DelegationPostState
            {
                kind = DelegationPostKind.CaravanBonus, title = "行商贸易收益提高", iconKey = "radio_post",
                slotUnlockLevels = new[] { 10, 14 }, assignedHeroIds = new string[2],
            });
        }

        public string GetPostEffectSummary(DelegationPostKind kind)
        {
            float s = GetPostScoreSum(kind);
            switch (kind)
            {
                case DelegationPostKind.WorkshopSpeed:
                    return $"提速 +{Mathf.RoundToInt(s * 1.4f)}%";
                case DelegationPostKind.BuildSpeed:
                    return $"提速 +{Mathf.RoundToInt(s * 1.6f)}%";
                case DelegationPostKind.FarmYield:
                    return $"提速 +{Mathf.RoundToInt(s * 1.6f)}%";
                case DelegationPostKind.DefensePower:
                    return $"火力 +{Mathf.RoundToInt(s * 1.8f)}%";
                case DelegationPostKind.TruckArmor:
                    return $"载重 +{Mathf.RoundToInt(s * 0.9f)}";
                case DelegationPostKind.CaravanBonus:
                    return $"溢价 +{Mathf.RoundToInt(s * 1.2f)}%";
                default: return "";
            }
        }

        public DelegationPostKind? GetHeroAssignedPost(string heroId)
        {
            if (string.IsNullOrEmpty(heroId)) return null;
            for (int p = 0; p < DelegationPosts.Count; p++)
            {
                var st = DelegationPosts[p];
                for (int s = 0; s < st.assignedHeroIds.Length; s++)
                    if (st.assignedHeroIds[s] == heroId) return st.kind;
            }
            return null;
        }

        void SyncHeroAssignedFlags()
        {
            for (int i = 0; i < Heroes.Count; i++)
            {
                DelegationPostKind? pk = GetHeroAssignedPost(Heroes[i].id);
                Heroes[i].assigned = pk.HasValue;
            }
        }

        public bool AssignHeroToPostSlot(DelegationPostKind kind, int slotIdx, string heroId)
        {
            DelegationPostState target = null;
            for (int i = 0; i < DelegationPosts.Count; i++)
                if (DelegationPosts[i].kind == kind) { target = DelegationPosts[i]; break; }
            if (target == null || !target.IsSlotUnlocked(slotIdx, CampLevel)) return false;

            // 先从旧槽位移除该英雄（防一英雄占多槽）
            if (!string.IsNullOrEmpty(heroId))
            {
                for (int p = 0; p < DelegationPosts.Count; p++)
                {
                    var st = DelegationPosts[p];
                    for (int s = 0; s < st.assignedHeroIds.Length; s++)
                        if (st.assignedHeroIds[s] == heroId) st.assignedHeroIds[s] = null;
                }
            }
            target.assignedHeroIds[slotIdx] = heroId;
            SyncHeroAssignedFlags();
            OnDelegationChanged?.Invoke();
            return true;
        }

        public int AutoAssignHeroes()
        {
            for (int p = 0; p < DelegationPosts.Count; p++)
                for (int s = 0; s < DelegationPosts[p].assignedHeroIds.Length; s++)
                    DelegationPosts[p].assignedHeroIds[s] = null;

            var pool = new List<HeroSpec>();
            for (int i = 0; i < Heroes.Count; i++)
                if (Heroes[i].unlocked) pool.Add(Heroes[i]);

            int assignedCnt = 0;
            // 贪心挑选每个已解锁空槽评分最高的未分配英雄
            while (pool.Count > 0)
            {
                int bestPost = -1, bestSlot = -1, bestHeroIdx = -1;
                float bestScore = -1f;
                for (int p = 0; p < DelegationPosts.Count; p++)
                {
                    var st = DelegationPosts[p];
                    for (int s = 0; s < st.assignedHeroIds.Length; s++)
                    {
                        if (!st.IsSlotUnlocked(s, CampLevel) || !string.IsNullOrEmpty(st.assignedHeroIds[s])) continue;
                        for (int h = 0; h < pool.Count; h++)
                        {
                            float sc = pool[h].GetDelegationScore(st.kind);
                            if (sc > bestScore)
                            {
                                bestScore = sc; bestPost = p; bestSlot = s; bestHeroIdx = h;
                            }
                        }
                    }
                }
                if (bestPost < 0 || bestHeroIdx < 0) break;
                DelegationPosts[bestPost].assignedHeroIds[bestSlot] = pool[bestHeroIdx].id;
                pool.RemoveAt(bestHeroIdx);
                assignedCnt++;
            }
            SyncHeroAssignedFlags();
            OnDelegationChanged?.Invoke();
            return assignedCnt;
        }

        public void UnassignAllHeroes()
        {
            for (int p = 0; p < DelegationPosts.Count; p++)
                for (int s = 0; s < DelegationPosts[p].assignedHeroIds.Length; s++)
                    DelegationPosts[p].assignedHeroIds[s] = null;
            SyncHeroAssignedFlags();
            OnDelegationChanged?.Invoke();
        }

        // ---------------- 英雄升级 / 升星 / 出战小队 ----------------
        public int MaxHeroLevelCap => Mathf.Max(5, CampLevel * 2);
        public static int GetHeroLevelUpCoins(int curLv) => 60 + curLv * 45;
        public static int GetHeroLevelUpRations(int curLv) => 2 + curLv / 3;
        public static int GetHeroStarUpMedals(int curStars)
        {
            int[] need = { 0, 3, 8, 15, 25, 999 };
            return need[Mathf.Clamp(curStars, 1, 5)];
        }

        public bool TryLevelUpHero(string heroId, out string msg)
        {
            HeroSpec h = FindHero(heroId);
            if (h == null || !h.unlocked) { msg = "该英雄尚未入队"; return false; }
            if (h.level >= MaxHeroLevelCap) { msg = "需先提升左上角领主等级！"; return false; }
            int cNeed = GetHeroLevelUpCoins(h.level);
            int rNeed = GetHeroLevelUpRations(h.level);
            if (Coins < cNeed) { msg = $"铜币不足 ({Coins}/{cNeed})，请交付居民订单获取"; return false; }
            if (Get(SurvRes.Ration) < rNeed) { msg = $"熟食口粮不足 ({Get(SurvRes.Ration)}/{rNeed})，请在炊事房烹饪获取"; return false; }

            Coins -= cNeed;
            Add(SurvRes.Ration, -rNeed);
            h.level++;
            HeroLevelUpsTotal++;
            OnDelegationChanged?.Invoke();
            EvaluateQuestProgress();
            msg = $"{h.name} 升至 Lv.{h.level}！战力与委任效率提升！";
            return true;
        }

        public bool TryStarUpHero(string heroId, out string msg)
        {
            HeroSpec h = FindHero(heroId);
            if (h == null || !h.unlocked) { msg = "该英雄尚未入队"; return false; }
            if (h.stars >= 5) { msg = "已达最高 5 星满阶！"; return false; }
            int need = GetHeroStarUpMedals(h.stars);
            int own = Get(SurvRes.HeroMedal);
            if (own < need) { msg = $"英雄勋章不足 ({own}/{need})，请前往【迎击】击退尸潮获取！"; return false; }

            Add(SurvRes.HeroMedal, -need);
            h.stars++;
            OnDelegationChanged?.Invoke();
            msg = $"★ 升星成功！{h.name} 晋升为 {h.stars}星，委任与大招倍率暴涨！";
            return true;
        }

        public void EnsureCombatSquad()
        {
            // 清理无效或未解锁的阵位角色
            for (int s = 0; s < FormationSlots.Length; s++)
            {
                int idx = FormationSlots[s];
                if (idx < 0 || idx >= Heroes.Count || !Heroes[idx].unlocked)
                    FormationSlots[s] = -1;
            }

            if (GetDeployedHeroCount() == 0 && Heroes.Count > 0)
            {
                for (int i = 0; i < Heroes.Count; i++)
                {
                    if (Heroes[i].unlocked)
                    {
                        PlaceHeroIntoBestOpenSlot(i);
                        break;
                    }
                }
            }

            if (!_manualFormationEdited)
                TryFillOpenFormationSlots();

            SyncCombatSquadFromFormation();
        }

        void SyncCombatSquadFromFormation()
        {
            CombatSquadIds.Clear();
            // 前排 -> 中排 -> 后排顺序同步至旧版小队列表，兼容皮卡搜刮等模块
            for (int col = 2; col >= 0; col--)
            {
                for (int row = 0; row < 3; row++)
                {
                    int idx = FormationSlots[row * 3 + col];
                    if (idx >= 0 && idx < Heroes.Count && Heroes[idx].unlocked && !CombatSquadIds.Contains(Heroes[idx].id))
                        CombatSquadIds.Add(Heroes[idx].id);
                }
            }
            if (CombatSquadIds.Count == 0)
            {
                for (int i = 0; i < Heroes.Count && CombatSquadIds.Count < 3; i++)
                    if (Heroes[i].unlocked) CombatSquadIds.Add(Heroes[i].id);
            }
        }

        void TryFillOpenFormationSlots()
        {
            int cap = GetMaxDeployHeroCount();
            if (GetDeployedHeroCount() >= cap) return;

            var cands = new List<int>();
            for (int i = 0; i < Heroes.Count; i++)
            {
                if (Heroes[i].unlocked && FindHeroSlot(i) < 0)
                    cands.Add(i);
            }
            cands.Sort((a, b) =>
            {
                int cmp = Heroes[b].CombatPower.CompareTo(Heroes[a].CombatPower);
                return cmp != 0 ? cmp : a.CompareTo(b);
            });

            for (int i = 0; i < cands.Count && GetDeployedHeroCount() < cap; i++)
                PlaceHeroIntoBestOpenSlot(cands[i]);
        }

        void PlaceHeroIntoBestOpenSlot(int heroIdx)
        {
            if (heroIdx < 0 || heroIdx >= Heroes.Count || FindHeroSlot(heroIdx) >= 0) return;
            HeroCombatRole role = GetHeroCombatRole(heroIdx);
            int[] prefOrder;
            if (role == HeroCombatRole.Vanguard || role == HeroCombatRole.Guardian)
                prefOrder = new[] { 5, 2, 8, 4, 1, 7, 3, 0, 6 }; // col=2 (row 1,0,2) 优先
            else if (role == HeroCombatRole.Demolition || role == HeroCombatRole.Gunsmith)
                prefOrder = new[] { 4, 1, 7, 3, 0, 6, 5, 2, 8 }; // col=1 (row 1,0,2) 优先
            else
                prefOrder = new[] { 3, 0, 6, 4, 1, 7, 5, 2, 8 }; // col=0 (row 1,0,2) 优先

            for (int i = 0; i < prefOrder.Length; i++)
            {
                int s = prefOrder[i];
                if (FormationSlots[s] < 0)
                {
                    FormationSlots[s] = heroIdx;
                    return;
                }
            }
        }

        public void ToggleSquadHero(string heroId)
        {
            int idx = FindHeroIndex(heroId);
            if (idx < 0 || !Heroes[idx].unlocked) return;
            int curSlot = FindHeroSlot(idx);
            if (curSlot >= 0)
            {
                RemoveHeroFromFormation(curSlot, out _);
            }
            else if (GetDeployedHeroCount() < GetMaxDeployHeroCount())
            {
                PlaceHeroIntoBestOpenSlot(idx);
                SyncCombatSquadFromFormation();
            }
            else
            {
                // 若已满编则替换最后一个已上阵位
                for (int s = 0; s < FormationSlots.Length; s++)
                {
                    if (FormationSlots[s] >= 0)
                    {
                        FormationSlots[s] = idx;
                        SyncCombatSquadFromFormation();
                        break;
                    }
                }
            }
        }

        public int SquadTotalPower => CalcDeployedSquadPower();

        // =====================================================================
        // 九宫格战术阵型 · 幸存者羁绊光环 · 40 关山口闯关数值体系
        // =====================================================================
        public int GetMaxDeployHeroCount()
        {
            if (_gmUnlockFullSquad) return 5;
            return CampLevel < 4 ? 2 : (CampLevel < 8 ? 3 : (CampLevel < 12 ? 4 : 5));
        }

        public int GetDeployedHeroCount()
        {
            int c = 0;
            for (int i = 0; i < FormationSlots.Length; i++)
            {
                int idx = FormationSlots[i];
                if (idx >= 0 && idx < Heroes.Count && Heroes[idx].unlocked)
                    c++;
            }
            return c;
        }

        public int FindHeroSlot(int heroIdx)
        {
            if (heroIdx < 0) return -1;
            for (int i = 0; i < FormationSlots.Length; i++)
                if (FormationSlots[i] == heroIdx) return i;
            return -1;
        }

        public HeroCombatRole GetHeroCombatRole(int heroIdx)
        {
            if (heroIdx >= 0 && heroIdx < Heroes.Count)
            {
                switch (Heroes[heroIdx].id)
                {
                    case "dachui": return HeroCombatRole.Guardian;
                    case "xiaotang": return HeroCombatRole.Demolition;
                    case "leimeng": return HeroCombatRole.Vanguard;
                    case "linyan": return HeroCombatRole.Ranger;
                    case "tieshu": return HeroCombatRole.Gunsmith;
                    case "laochentou": return HeroCombatRole.Medic;
                }
            }
            switch (heroIdx)
            {
                case 0: return HeroCombatRole.Guardian;
                case 1: return HeroCombatRole.Demolition;
                case 2: return HeroCombatRole.Vanguard;
                case 3: return HeroCombatRole.Ranger;
                case 4: return HeroCombatRole.Gunsmith;
                default: return HeroCombatRole.Medic;
            }
        }

        public string GetHeroRoleName(int heroIdx)
        {
            switch (GetHeroCombatRole(heroIdx))
            {
                case HeroCombatRole.Vanguard: return "重装先锋";
                case HeroCombatRole.Guardian: return "铁壁重卫";
                case HeroCombatRole.Demolition: return "爆破工匠";
                case HeroCombatRole.Ranger: return "山林猎手";
                case HeroCombatRole.Gunsmith: return "火力枪匠";
                case HeroCombatRole.Medic: return "战地医官";
                default: return "突击骨干";
            }
        }

        public string GetHeroSkillTitle(int heroIdx)
        {
            if (heroIdx >= 0 && heroIdx < Heroes.Count)
                return Heroes[heroIdx].skillName ?? "";
            return "";
        }

        public string GetHeroSkillDesc(int heroIdx)
        {
            if (heroIdx >= 0 && heroIdx < Heroes.Count)
                return Heroes[heroIdx].skillDesc ?? "";
            return "";
        }

        public bool IsHeroMelee(int heroIdx)
        {
            if (heroIdx >= 0 && heroIdx < Heroes.Count)
                return Heroes[heroIdx].isMelee;
            return heroIdx == 0 || heroIdx == 2;
        }

        public bool IsFormationUnlocked(TacticalFormationId fmt)
        {
            switch (fmt)
            {
                case TacticalFormationId.StandardLine: return true;
                case TacticalFormationId.SpearAssault: return HordeWaveCleared >= 4 || _gmUnlockFullSquad;
                case TacticalFormationId.IronBastion: return HordeWaveCleared >= 10 || _gmUnlockFullSquad;
                case TacticalFormationId.VolleyFire: return HordeWaveCleared >= 18 || _gmUnlockFullSquad;
                default: return false;
            }
        }

        public string GetFormationName(TacticalFormationId fmt)
        {
            switch (fmt)
            {
                case TacticalFormationId.StandardLine: return "基础防线阵";
                case TacticalFormationId.SpearAssault: return "尖锥突击阵";
                case TacticalFormationId.IronBastion: return "铁桶抗性阵";
                case TacticalFormationId.VolleyFire: return "火力齐射阵";
                default: return "基础防线阵";
            }
        }

        public string GetFormationSummary(TacticalFormationId fmt)
        {
            switch (fmt)
            {
                case TacticalFormationId.StandardLine:
                    return "前排全列生命+15% · 中后排全列攻击+10%";
                case TacticalFormationId.SpearAssault:
                    return "前中尖刀位减伤+25%/生命+15% · 中排暴伤+25%/攻击+15% · 后排攻击+12%";
                case TacticalFormationId.IronBastion:
                    return "前排护甲减伤+22%/生命+20% · 中后排生命+12%/减伤+8%（防跳尸切后）";
                case TacticalFormationId.VolleyFire:
                    return "中后排攻速+25%/开场怒气+35% · 前排生命+12%（速开绝技爆发）";
                default:
                    return "";
            }
        }

        public string GetFormationUnlockHint(TacticalFormationId fmt)
        {
            switch (fmt)
            {
                case TacticalFormationId.StandardLine:
                    return "初始默认解锁";
                case TacticalFormationId.SpearAssault:
                    return IsFormationUnlocked(fmt) ? "已解锁（第 4 关首通阵型）" : "通关第 4 关解锁【尖锥突击阵】";
                case TacticalFormationId.IronBastion:
                    return IsFormationUnlocked(fmt) ? "已解锁（第 10 关首通阵型）" : "通关第 10 关解锁【铁桶抗性阵】";
                case TacticalFormationId.VolleyFire:
                    return IsFormationUnlocked(fmt) ? "已解锁（第 18 关首通阵型）" : "通关第 18 关解锁【火力齐射阵】";
                default:
                    return "";
            }
        }

        public bool TrySelectFormation(TacticalFormationId fmt, out string msg)
        {
            if (!IsFormationUnlocked(fmt))
            {
                msg = GetFormationUnlockHint(fmt);
                return false;
            }
            SelectedFormation = fmt;
            msg = $"已启用【{GetFormationName(fmt)}】！";
            return true;
        }

        public SlotStatBuff GetFormationSlotBuff(TacticalFormationId fmt, int slot0to8)
        {
            int s = Mathf.Clamp(slot0to8, 0, 8);
            int col = s % 3;
            switch (fmt)
            {
                case TacticalFormationId.SpearAssault:
                    if (s == 5)
                    {
                        return new SlotStatBuff
                        {
                            shortTag = "减伤+25%",
                            highlighted = true,
                            dmgReduction = 0.25f,
                            hpBonus = 0.15f,
                        };
                    }
                    if (col == 1)
                    {
                        return new SlotStatBuff
                        {
                            shortTag = "暴伤+25%",
                            highlighted = true,
                            critBonus = 0.25f,
                            atkBonus = 0.15f,
                        };
                    }
                    if (col == 0)
                    {
                        return new SlotStatBuff
                        {
                            shortTag = "攻击+12%",
                            highlighted = false,
                            atkBonus = 0.12f,
                        };
                    }
                    return new SlotStatBuff { shortTag = "" };

                case TacticalFormationId.IronBastion:
                    if (col == 2)
                    {
                        return new SlotStatBuff
                        {
                            shortTag = "护甲+22%",
                            highlighted = true,
                            dmgReduction = 0.22f,
                            hpBonus = 0.20f,
                        };
                    }
                    return new SlotStatBuff
                    {
                        shortTag = "生命+12%",
                        highlighted = false,
                        hpBonus = 0.12f,
                        dmgReduction = 0.08f,
                    };

                case TacticalFormationId.VolleyFire:
                    if (col <= 1)
                    {
                        return new SlotStatBuff
                        {
                            shortTag = "攻速+25%",
                            highlighted = true,
                            atkSpeedBonus = 0.25f,
                            startRage = 0.35f,
                        };
                    }
                    return new SlotStatBuff
                    {
                        shortTag = "生命+12%",
                        highlighted = false,
                        hpBonus = 0.12f,
                    };

                default: // StandardLine
                    if (col == 2)
                    {
                        return new SlotStatBuff
                        {
                            shortTag = "生命+15%",
                            highlighted = true,
                            hpBonus = 0.15f,
                        };
                    }
                    return new SlotStatBuff
                    {
                        shortTag = "攻击+10%",
                        highlighted = true,
                        atkBonus = 0.10f,
                    };
            }
        }

        public bool TryPlaceHeroInFormation(int heroIdx, int targetSlot0to8, out string msg)
        {
            if (heroIdx < 0 || heroIdx >= Heroes.Count)
            {
                msg = "无效的骨干伙伴";
                return false;
            }
            HeroSpec h = Heroes[heroIdx];
            if (!h.unlocked)
            {
                msg = $"【{h.name}】尚未招募入队！";
                return false;
            }
            if (targetSlot0to8 < 0 || targetSlot0to8 >= FormationSlots.Length)
            {
                msg = "无效的战术阵位";
                return false;
            }

            int oldSlot = FindHeroSlot(heroIdx);
            if (oldSlot >= 0)
            {
                if (targetSlot0to8 == oldSlot)
                {
                    msg = $"{h.name} 已部署在该阵位";
                    return true;
                }
                int otherIdx = FormationSlots[targetSlot0to8];
                if (otherIdx >= 0 && otherIdx < Heroes.Count)
                {
                    FormationSlots[targetSlot0to8] = heroIdx;
                    FormationSlots[oldSlot] = otherIdx;
                    _manualFormationEdited = true;
                    SyncCombatSquadFromFormation();
                    msg = $"{h.name} 与 {Heroes[otherIdx].name} 已互换站位！";
                    return true;
                }
                FormationSlots[oldSlot] = -1;
                FormationSlots[targetSlot0to8] = heroIdx;
                _manualFormationEdited = true;
                SyncCombatSquadFromFormation();
                msg = $"{h.name} 已移动至新阵位！";
                return true;
            }

            int targetOccupant = FormationSlots[targetSlot0to8];
            if (targetOccupant >= 0 && targetOccupant < Heroes.Count)
            {
                FormationSlots[targetSlot0to8] = heroIdx;
                _manualFormationEdited = true;
                SyncCombatSquadFromFormation();
                msg = $"{h.name} 已替换 {Heroes[targetOccupant].name} 上阵！";
                return true;
            }

            if (GetDeployedHeroCount() >= GetMaxDeployHeroCount())
            {
                msg = "已达当前上阵人数上限，提升营地等级可扩编！";
                return false;
            }

            FormationSlots[targetSlot0to8] = heroIdx;
            _manualFormationEdited = true;
            SyncCombatSquadFromFormation();
            msg = $"{h.name} 已部署上阵！";
            return true;
        }

        public bool RemoveHeroFromFormation(int slot0to8, out string msg)
        {
            if (slot0to8 < 0 || slot0to8 >= FormationSlots.Length || FormationSlots[slot0to8] < 0)
            {
                msg = "该阵位暂无上阵骨干";
                return false;
            }
            if (GetDeployedHeroCount() <= 1)
            {
                msg = "至少需保留 1 名上阵骨干！";
                return false;
            }
            int removedIdx = FormationSlots[slot0to8];
            FormationSlots[slot0to8] = -1;
            _manualFormationEdited = true;
            SyncCombatSquadFromFormation();
            string rName = (removedIdx >= 0 && removedIdx < Heroes.Count) ? Heroes[removedIdx].name : "骨干";
            msg = $"{rName} 已撤下休整";
            return true;
        }

        public void AutoDeployRecommendedFormation()
        {
            for (int i = 0; i < FormationSlots.Length; i++)
                FormationSlots[i] = -1;

            var unlockedIdx = new List<int>();
            for (int i = 0; i < Heroes.Count; i++)
                if (Heroes[i].unlocked) unlockedIdx.Add(i);

            unlockedIdx.Sort((a, b) =>
            {
                int cmp = Heroes[b].CombatPower.CompareTo(Heroes[a].CombatPower);
                return cmp != 0 ? cmp : a.CompareTo(b);
            });

            int deployCap = Mathf.Min(GetMaxDeployHeroCount(), unlockedIdx.Count);
            var selected = new List<int>(deployCap);
            for (int i = 0; i < deployCap; i++)
                selected.Add(unlockedIdx[i]);

            // 先放近战前排 (col=2: row 1 -> 0 -> 2)，再放爆破/枪匠中排 (col=1)，最后猎手/医官后排 (col=0)
            for (int pass = 0; pass < 3; pass++)
            {
                for (int i = 0; i < selected.Count; i++)
                {
                    int hIdx = selected[i];
                    HeroCombatRole role = GetHeroCombatRole(hIdx);
                    bool isMeleePass = IsHeroMelee(hIdx);
                    bool isMidPass = !isMeleePass && (role == HeroCombatRole.Gunsmith || role == HeroCombatRole.Demolition);
                    bool isBackPass = !isMeleePass && !isMidPass;
                    if ((pass == 0 && isMeleePass) || (pass == 1 && isMidPass) || (pass == 2 && isBackPass))
                        PlaceHeroIntoBestOpenSlot(hIdx);
                }
            }

            _manualFormationEdited = false;
            SyncCombatSquadFromFormation();
        }

        public List<SynergyAuraInfo> GetSynergyAuras()
        {
            int deployed = 0;
            int meleeFrontCount = 0;
            int frontColCount = 0;
            int rangedFireCount = 0;
            bool hasMedic = false;

            for (int s = 0; s < FormationSlots.Length; s++)
            {
                int idx = FormationSlots[s];
                if (idx < 0 || idx >= Heroes.Count || !Heroes[idx].unlocked) continue;
                deployed++;
                if (s % 3 == 2) frontColCount++;
                HeroCombatRole role = GetHeroCombatRole(idx);
                if (IsHeroMelee(idx) || role == HeroCombatRole.Vanguard || role == HeroCombatRole.Guardian)
                    meleeFrontCount++;
                else if (role == HeroCombatRole.Demolition || role == HeroCombatRole.Ranger || role == HeroCombatRole.Gunsmith)
                    rangedFireCount++;
                else if (role == HeroCombatRole.Medic)
                    hasMedic = true;
            }

            float unityBonus = deployed >= 5 ? 0.22f : (deployed == 4 ? 0.15f : (deployed == 3 ? 0.10f : (deployed >= 2 ? 0.06f : 0f)));
            bool unityActive = deployed >= 2;
            bool bastionActive = meleeFrontCount >= 2;
            bool crossfireActive = rangedFireCount >= 2;
            bool medicActive = hasMedic && (meleeFrontCount >= 1 || frontColCount >= 1);

            var list = new List<SynergyAuraInfo>(4)
            {
                new SynergyAuraInfo
                {
                    id = "unity",
                    title = unityActive ? $"同心协力 ({deployed}人)" : "同心协力 (2~5人)",
                    desc = unityActive
                        ? $"全队攻血+{Mathf.RoundToInt(unityBonus * 100f)}% (2/3/4/5人递进)"
                        : "上阵2/3/4/5人：全队攻血+6%~22%",
                    active = unityActive,
                    atkMulBonus = unityActive ? unityBonus : 0f,
                    hpMulBonus = unityActive ? unityBonus : 0f,
                },
                new SynergyAuraInfo
                {
                    id = "bastion",
                    title = "铜墙铁壁",
                    desc = "≥2名近战前排：生命+18% · 减伤+12%",
                    active = bastionActive,
                    hpMulBonus = bastionActive ? 0.18f : 0f,
                    dmgRedBonus = bastionActive ? 0.12f : 0f,
                },
                new SynergyAuraInfo
                {
                    id = "crossfire",
                    title = "交叉火力",
                    desc = "≥2名远程火力：攻击+14% · 攻速+15%",
                    active = crossfireActive,
                    atkMulBonus = crossfireActive ? 0.14f : 0f,
                    atkSpdBonus = crossfireActive ? 0.15f : 0f,
                },
                new SynergyAuraInfo
                {
                    id = "medic_aid",
                    title = "战地救护",
                    desc = "老陈头+前排：生命+12% · 回怒+20%",
                    active = medicActive,
                    hpMulBonus = medicActive ? 0.12f : 0f,
                    dmgRedBonus = medicActive ? 0.08f : 0f,
                    rageGainBonus = medicActive ? 0.20f : 0f,
                },
            };
            return list;
        }

        public SynergyAuraInfo GetCombinedSynergyTotals()
        {
            List<SynergyAuraInfo> auras = GetSynergyAuras();
            var total = new SynergyAuraInfo
            {
                id = "combined",
                title = "羁绊总加成",
                desc = "",
                active = false,
            };
            for (int i = 0; i < auras.Count; i++)
            {
                SynergyAuraInfo a = auras[i];
                if (!a.active) continue;
                total.active = true;
                total.atkMulBonus += a.atkMulBonus;
                total.hpMulBonus += a.hpMulBonus;
                total.dmgRedBonus += a.dmgRedBonus;
                total.atkSpdBonus += a.atkSpdBonus;
                total.rageGainBonus += a.rageGainBonus;
            }
            total.dmgRedBonus = Mathf.Min(0.55f, total.dmgRedBonus);
            return total;
        }

        public int CalcHeroCombatPower(int heroIdx, int slot0to8 = -1)
        {
            if (heroIdx < 0 || heroIdx >= Heroes.Count) return 0;
            HeroSpec h = Heroes[heroIdx];
            int resolvedSlot = slot0to8 >= 0 ? slot0to8 : FindHeroSlot(heroIdx);
            if (resolvedSlot < 0 || resolvedSlot >= 9)
                return h.CombatPower;

            SlotStatBuff sb = GetFormationSlotBuff(SelectedFormation, resolvedSlot);
            SynergyAuraInfo syn = GetCombinedSynergyTotals();

            float atkMul = (1f + sb.atkBonus + syn.atkMulBonus)
                         * (1f + (sb.atkSpeedBonus + syn.atkSpdBonus) * 0.65f)
                         * (1f + sb.critBonus * 0.40f);
            float dmgRed = Mathf.Clamp(sb.dmgReduction + syn.dmgRedBonus, 0f, 0.65f);
            float hpMul = (1f + sb.hpBonus + syn.hpMulBonus) / Mathf.Max(0.35f, 1f - dmgRed * 0.65f);
            float rageFactor = 1f + sb.startRage * 0.15f + syn.rageGainBonus * 0.25f;

            float effAtk = h.Attack * atkMul * rageFactor;
            float effHp = h.MaxHp * hpMul;
            return Mathf.RoundToInt(effAtk * 4f + effHp * 0.5f);
        }

        public int CalcDeployedSquadPower()
        {
            int sum = 0;
            for (int s = 0; s < FormationSlots.Length; s++)
            {
                int idx = FormationSlots[s];
                if (idx >= 0 && idx < Heroes.Count && Heroes[idx].unlocked)
                    sum += CalcHeroCombatPower(idx, s);
            }
            return sum;
        }

        static readonly string[] StageNames40 =
        {
            "山口游尸", "断桥拾荒尸", "铁锤尸卫首领", "夜袭跳尸群", "盘山重甲尸王",
            "隧洞疾行尸", "安全帽工地尸", "迷雾猎杀跳尸", "汽修街狂暴尸", "裂谷暴君首领",
            "收费站尸群", "林场伐木恶尸", "断崖狂奔尸潮", "防空洞腐毒尸", "血月屠夫首领",
            "高架桥暗影尸", "废弃加油站尸群", "索桥突袭跳尸", "林区重装尸卫", "深涧巨骸尸王",
            "城区外环尸群", "地铁口奔袭尸", "急诊街变异尸", "天桥飞跃跳尸", "装甲警卫尸王",
            "旧货场铁甲尸", "冷链仓寒霜尸", "钟楼猎手跳尸", "防卫库狂尸群", "毁灭重锤尸皇",
            "焦土公路尸潮", "高压塔电弧尸", "矿道噬骨恶尸", "裂隙飞扑跳尸", "深渊督军尸王",
            "黑雾封锁尸潮", "废墟钢铁尸群", "绝壁狂袭跳尸", "血色前哨死士", "深渊尸潮暴君",
        };

        static readonly string[] StageSubs40 =
        {
            "清剿逼近断桥防线的先遣尸群",
            "击退围攻工兵营地的外围游尸",
            "迎战手持施工重锤的变异首领",
            "警惕跃过前排直扑后卫的夜行跳尸",
            "击溃身披路障钢板的盘山尸王",
            "狭窄山道隧洞涌出成群疾跑丧尸",
            "佩戴工地硬盔的尸群正面强压防线",
            "秋雾掩护下跳尸突袭我方远程后排",
            "扫荡盘踞旧公路汽修站的狂暴尸群",
            "裂谷重装尸王携跳尸精锐发起总攻",
            "突破盘山收费站残骸处的密集尸墙",
            "清剿废弃伐木场里游荡的壮硕恶尸",
            "在断崖窄道稳住阵脚迎击疾奔尸潮",
            "肃清山腰防空洞涌出的饥饿感染群",
            "血月之下重甲屠夫首领狂暴冲锋",
            "清理高架引桥上成建制的游荡尸群",
            "引爆火力防线阻击油站方向尸潮",
            "小心双路跳尸越过前排偷袭医官猎手",
            "盾甲重装尸卫列阵推进考验前排抗性",
            "击败镇守深涧大桥的巨型尸王",
            "推进至沦陷市区外环迎战大批尸群",
            "封堵坍塌地铁口源源不断的奔袭尸",
            "肃清医院街口高攻速变异感染者",
            "加固后排阵型抵御天桥跳尸连环切后",
            "击破身穿防暴重甲的警卫尸王",
            "重火力轰开旧货场集装箱间的铁甲尸",
            "迎战冷链仓库冲出的高耐久寒霜尸",
            "防备钟楼制高点扑下的精锐跳尸群",
            "清剿旧城防卫总库外围的狂暴尸潮",
            "正面决战挥舞巨型路灯的毁灭尸皇",
            "在焦土国道构筑交叉火力网歼灭尸潮",
            "顶住高压变电站周边的高危变异尸",
            "清理废弃矿道深处涌出的噬骨恶尸",
            "铁桶阵护卫后排粉碎跳尸集群突袭",
            "斩杀统领外城尸群的深渊督军尸王",
            "穿透浓重黑雾迎击四面合围的尸潮",
            "重甲先锋顶住废墟钢铁尸群强攻",
            "火力全开拦截绝壁跃下的狂袭跳尸",
            "拔除尸巢核心门户最后的死士尸卫",
            "决战深渊尸潮暴君，彻底肃清山口威胁！",
        };

        public BattleStageSpec GetBattleStageSpec(int stage1Based)
        {
            int s = Mathf.Clamp(stage1Based, 1, MaxBattleStages);
            bool isBoss = (s == 3) || (s % 5 == 0);
            bool hasFlanker = (s >= 4) && (s % 2 == 0);

            int recPower;
            switch (s)
            {
                case 1: recPower = 420; break;
                case 2: recPower = 510; break;
                case 3: recPower = 620; break;
                case 4: recPower = 780; break;
                case 5: recPower = 960; break;
                default:
                {
                    float raw = 960f + (s - 5) * 195f + Mathf.Pow(s - 5, 1.72f) * 20f;
                    if (isBoss) raw *= 1.07f;
                    recPower = Mathf.RoundToInt(raw / 10f) * 10;
                    break;
                }
            }

            int enemyCnt = s == 1 ? 6 : (s == 2 ? 8 : (s == 3 ? 9 : Mathf.Clamp(8 + s / 2 + (isBoss ? 2 : 0), 8, 32)));
            int rwXp = (isBoss ? 55 : 35) + s * 6;
            int rwCoins = (isBoss ? 220 : 140) + s * 45;
            int rwMedals = (isBoss ? 3 : 1) + s / 4;
            int rwBp = isBoss ? (2 + s / 8) : (1 + s / 12);

            int unlockHero = -1;
            if (s == 2) unlockHero = 2;       // 雷猛 (重装先锋)
            else if (s == 5) unlockHero = 3;  // 林砚 (山林猎手)
            else if (s == 9) unlockHero = 4;  // 铁叔 (火力枪匠)
            else if (s == 13) unlockHero = 5; // 老陈头 (战地医官)

            int unlockFmt = -1;
            if (s == 4) unlockFmt = (int)TacticalFormationId.SpearAssault;
            else if (s == 10) unlockFmt = (int)TacticalFormationId.IronBastion;
            else if (s == 18) unlockFmt = (int)TacticalFormationId.VolleyFire;

            string milestone = "";
            if (HordeWaveCleared < s)
            {
                if (unlockHero >= 0 && unlockHero < Heroes.Count && !Heroes[unlockHero].unlocked)
                    milestone = $"首通招募：{Heroes[unlockHero].name}（{GetHeroRoleName(unlockHero)}）";
                else if (unlockFmt >= 0)
                    milestone = $"首通解锁：{GetFormationName((TacticalFormationId)unlockFmt)}";
                else if (isBoss)
                    milestone = $"首领战里程碑：英雄勋章×{rwMedals} · 工程手稿×{rwBp}";
            }

            return new BattleStageSpec
            {
                stage = s,
                title = $"第 {s} 关 · {StageNames40[s - 1]}",
                subtitle = StageSubs40[s - 1],
                isBossStage = isBoss,
                hasBacklineFlanker = hasFlanker,
                recommendedPower = recPower,
                enemyTotalCount = enemyCnt,
                rewardXp = rwXp,
                rewardCoins = rwCoins,
                rewardMedals = rwMedals,
                rewardBlueprints = rwBp,
                unlockHeroIdx = unlockHero,
                unlockFormationIdx = unlockFmt,
                milestoneTag = milestone,
            };
        }

        public string GrantBattleStageVictory(int stage1Based)
        {
            int s = Mathf.Clamp(stage1Based, 1, MaxBattleStages);
            bool firstClear = s > HordeWaveCleared;
            BattleStageSpec spec = GetBattleStageSpec(s);

            Coins += spec.rewardCoins;
            if (spec.rewardMedals > 0)
                Add(SurvRes.HeroMedal, spec.rewardMedals, notifyToast: true);
            if (spec.rewardBlueprints > 0)
                Add(SurvRes.Blueprint, spec.rewardBlueprints, notifyToast: true);
            Add(SurvRes.Scrap, 5 + s * 2, notifyToast: false);
            if (firstClear && spec.isBossStage)
                Add(SurvRes.SeedGolden, 1, notifyToast: true);

            HordeWaveCleared = Mathf.Max(HordeWaveCleared, s);
            HordeRepelled = true;
            HordePosterShown = false;
            HordeTimerSec = 1799f;

            AddXp(spec.rewardXp, new Vector2(15.2f, 11.4f));

            string extra = "";
            if (spec.unlockHeroIdx >= 0 && spec.unlockHeroIdx < Heroes.Count)
            {
                HeroSpec uh = Heroes[spec.unlockHeroIdx];
                bool wasLocked = !uh.unlocked;
                UnlockHero(uh.id, silent: false);
                if (wasLocked)
                    extra += $"\n★ 新骨干入队：{uh.name}（{GetHeroRoleName(spec.unlockHeroIdx)}）！";
            }
            if (firstClear && spec.unlockFormationIdx >= 0)
            {
                var fmt = (TacticalFormationId)spec.unlockFormationIdx;
                extra += $"\n★ 新阵型解锁：【{GetFormationName(fmt)}】！";
            }

            EvaluateQuestProgress();
            return $"经验 +{spec.rewardXp} · 铜币 +{spec.rewardCoins} · 勋章 +{spec.rewardMedals} · 手稿 +{spec.rewardBlueprints}{extra}";
        }

        // ---------------- 山口尸潮旧版兼容包装 ----------------
        public HordeWaveSpec GetWaveSpec(int wave)
        {
            BattleStageSpec bs = GetBattleStageSpec(wave);
            return new HordeWaveSpec
            {
                wave = bs.stage,
                title = bs.title,
                hasBoss = bs.isBossStage,
                zombieCount = bs.enemyTotalCount,
                zombieHp = 95f * Mathf.Pow(1.22f, bs.stage - 1),
                zombieSpeed = Mathf.Min(1.75f, 0.95f + bs.stage * 0.04f),
                recommendPower = bs.recommendedPower,
                medalReward = bs.rewardMedals,
                blueprintReward = bs.rewardBlueprints,
                coinReward = bs.rewardCoins,
                scrapReward = 5 + bs.stage * 2,
            };
        }

        public HordeWaveSpec CompleteHordeWaveVictory()
        {
            int stg = CurrentBattleStage;
            HordeWaveSpec sp = GetWaveSpec(stg);
            GrantBattleStageVictory(stg);
            return sp;
        }

        public bool TrySweepClearedWave(out string msg)
        {
            if (HordeWaveCleared <= 0)
            {
                msg = "请先亲自迎击通关第 1 关后再开启巡山扫荡！";
                return false;
            }
            if (SweepCooldownSec > 0.1f)
            {
                msg = $"巡山小队整备冷却中（{Mathf.CeilToInt(SweepCooldownSec)}秒）";
                return false;
            }
            SweepCooldownSec = 25f;
            int coins = 140 + HordeWaveCleared * 45;
            int medals = 1 + HordeWaveCleared / 4;
            int scrap = 6 + HordeWaveCleared * 2;
            Coins += coins;
            Add(SurvRes.HeroMedal, medals, notifyToast: true);
            Add(SurvRes.Scrap, scrap, notifyToast: true);
            AddXp(28, new Vector2(14.6f, 11.8f));
            msg = $"巡山扫荡第 {HordeWaveCleared} 关成功！勋章+{medals} · 铜币+{coins} · 废铁+{scrap}";
            return true;
        }

        void InitHeroes()
        {
            Heroes.Clear();
            // 0: 大锤 (铁壁重卫 · 初始解锁主盾，前排中路 slot=5)
            Heroes.Add(new HeroSpec
            {
                id = "dachui", characterId = "dachui", name = "大锤", roleTitle = "铁壁重卫 · 嘲讽护盾",
                rarity = HeroRarity.Legendary, baseAtk = 32, baseHp = 350, isMelee = true, projKey = "proj_wrench",
                skillName = "路牌重盾震击", skillDesc = "前排嘲讽聚怪，展开高额护盾并重锤震晕周围尸群！",
                prefPost1 = DelegationPostKind.BuildSpeed, prefPost2 = DelegationPostKind.TruckArmor,
                portraitKey = "bust_dachui", targetBuildingTitle = "建筑加速 / 皮卡重甲",
                buffDesc = "专长：建造加速 · 重甲主盾",
                unlocked = true, assigned = true, skinTier = OutfitTier.Refugee,
            });
            // 1: 小棠 (爆破工匠 · 初始解锁后排范围灼烧，后排中路 slot=3)
            Heroes.Add(new HeroSpec
            {
                id = "xiaotang", characterId = "xiaotang", name = "小棠", roleTitle = "爆破工匠 · 烈焰灼烧",
                rarity = HeroRarity.Epic, baseAtk = 31, baseHp = 195, isMelee = false, projKey = "proj_wrench",
                skillName = "高爆燃烧瓶", skillDesc = "投掷燃烧瓶在战场铺设烈焰火海，持续范围灼烧丧尸！",
                prefPost1 = DelegationPostKind.WorkshopSpeed, prefPost2 = DelegationPostKind.BuildSpeed,
                portraitKey = "bust_xiaotang", targetBuildingTitle = "废铁锻造坊 / 基建加速",
                buffDesc = "专长：作坊提速 · 建造加速",
                unlocked = true, assigned = true, skinTier = OutfitTier.Refugee,
            });
            // 2: 雷猛 (重装先锋 · 第2关首通或营地Lv.3招募)
            Heroes.Add(new HeroSpec
            {
                id = "leimeng", characterId = "hunter", name = "雷猛", roleTitle = "重装先锋 · 狂斧横扫",
                rarity = HeroRarity.Epic, baseAtk = 36, baseHp = 275, isMelee = true, projKey = "proj_wrench",
                skillName = "狂风破障斩", skillDesc = "冲锋挥动消防破拆重斧横扫尸群，造成高额范围劈砍伤害并击退！",
                prefPost1 = DelegationPostKind.WorkshopSpeed, prefPost2 = DelegationPostKind.DefensePower,
                portraitKey = "bust_vil_0", targetBuildingTitle = "作坊提速 / 城防主将",
                buffDesc = "专长：作坊提速 · 城防 (第2关/Lv.3)",
                unlocked = false, assigned = false, skinTier = OutfitTier.Refugee,
            });
            // 3: 林砚 (山林猎手 · 第5关首通或营地Lv.4招募)
            Heroes.Add(new HeroSpec
            {
                id = "linyan", characterId = "linyan", name = "林砚", roleTitle = "山林猎手 · 连弩穿透",
                rarity = HeroRarity.Epic, baseAtk = 36, baseHp = 175, isMelee = false, projKey = "proj_arrow",
                skillName = "贯日散射箭雨", skillDesc = "后排高速连弩点杀，大招倾泻穿云箭雨贯穿整条战线！",
                prefPost1 = DelegationPostKind.DefensePower, prefPost2 = DelegationPostKind.FarmYield,
                portraitKey = "bust_linyan", targetBuildingTitle = "城防哨塔 / 农牧巡林",
                buffDesc = "专长：哨塔火力 · 农田 (第5关/Lv.4)",
                unlocked = false, assigned = false, skinTier = OutfitTier.Refugee,
            });
            // 4: 铁叔 (火力枪匠 · 第9关首通或营地Lv.7招募)
            Heroes.Add(new HeroSpec
            {
                id = "tieshu", characterId = "tieshu", name = "铁叔", roleTitle = "火力枪匠 · 霰弹压制",
                rarity = HeroRarity.Rare, baseAtk = 26, baseHp = 220, isMelee = false, projKey = "proj_buckshot",
                skillName = "双管高爆霰弹", skillDesc = "中排扇形倾泻高爆铁砂弹，强力击退并压制逼近尸潮！",
                prefPost1 = DelegationPostKind.TruckArmor, prefPost2 = DelegationPostKind.CaravanBonus,
                portraitKey = "survivor_tieshu", targetBuildingTitle = "远征皮卡 / 行商护航",
                buffDesc = "专长：皮卡载重 · 行商 (第9关/Lv.7)",
                unlocked = false, assigned = false, skinTier = OutfitTier.Refugee,
            });
            // 5: 老陈头 (战地医官 · 第13关首通或营地Lv.10招募)
            Heroes.Add(new HeroSpec
            {
                id = "laochentou", characterId = "laochentou", name = "老陈头", roleTitle = "战地医官 · 急救群疗",
                rarity = HeroRarity.Rare, baseAtk = 23, baseHp = 210, isMelee = false, projKey = "proj_fire_pool",
                skillName = "战地急救热汤", skillDesc = "持续急救伤员，大招恢复全队生命并附加守护护盾！",
                prefPost1 = DelegationPostKind.FarmYield, prefPost2 = DelegationPostKind.CaravanBonus,
                portraitKey = "bust_laochentou", targetBuildingTitle = "农田丰收 / 行商议价",
                buffDesc = "专长：农田采收 · 行商 (第13关/Lv.10)",
                unlocked = false, assigned = false, skinTier = OutfitTier.Refugee,
            });

            for (int s = 0; s < FormationSlots.Length; s++)
                FormationSlots[s] = -1;
            FormationSlots[1 * 3 + 2] = 0; // 大锤：前排中路 (row 1, col 2 = slot 5)
            FormationSlots[1 * 3 + 0] = 1; // 小棠：后排中路 (row 1, col 0 = slot 3)
            _manualFormationEdited = false;
            SyncCombatSquadFromFormation();
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
                    id = "wood_bed", name = "防潮木床铺", iconKey = "ico_woodbed", unlockBuildingLv = 2, unlockCampLv = 5,
                    sourceHint = "需锯木坊 Lv.2 + 厚木板 + 选种农田收获的【粗麻布】",
                    inRes1 = SurvRes.Plank, inAmt1 = 2, inRes2 = SurvRes.Cloth, inAmt2 = 1,
                    outRes = SurvRes.WoodBed, outAmt = 1, duration = 5.0f,
                },
                new WorkshopRecipe
                {
                    id = "iron_beam", name = "包铁承重梁", iconKey = "rec_beam", unlockBuildingLv = 3, unlockCampLv = 7,
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
                    id = "iron_ingot", name = "锻打铁锭", iconKey = "ico_ingot", unlockBuildingLv = 2, unlockCampLv = 8,
                    sourceHint = "需锻造坊 Lv.2 + 废铁 + 原木作炭火",
                    inRes1 = SurvRes.Scrap, inAmt1 = 3, inRes2 = SurvRes.Wood, inAmt2 = 1,
                    outRes = SurvRes.IronIngot, outAmt = 1, duration = 6.0f,
                },
                new WorkshopRecipe
                {
                    id = "metal_pipe", name = "导流金属管", iconKey = "ico_pipe", unlockBuildingLv = 3, unlockCampLv = 11,
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
                    sourceHint = "收割【生态选种农田】亚麻获取粗麻布，满足居民穿戴订单！",
                    inRes1 = SurvRes.Cloth, inAmt1 = 2, outRes = SurvRes.LinenShirt, outAmt = 1, duration = 4.2f,
                },
                new WorkshopRecipe
                {
                    id = "leather_boots", name = "防荆棘猎皮靴", iconKey = "ico_boots", unlockBuildingLv = 2, unlockCampLv = 6,
                    sourceHint = "需裁缝坊 Lv.2 + 山林畜牧栏/捕猎哨塔获取的【兽毛皮】",
                    inRes1 = SurvRes.Pelt, inAmt1 = 2, inRes2 = SurvRes.Cloth, inAmt2 = 1,
                    outRes = SurvRes.LeatherBoots, outAmt = 1, duration = 5.5f,
                },
                new WorkshopRecipe
                {
                    id = "fur_coat", name = "加绒御寒皮衣", iconKey = "ico_coat", unlockBuildingLv = 3, unlockCampLv = 9,
                    sourceHint = "需裁缝坊 Lv.3；首次缝制即解锁全队【2阶加固皮衣换装】！",
                    inRes1 = SurvRes.Pelt, inAmt1 = 2, inRes2 = SurvRes.LinenShirt, inAmt2 = 1, inRes3 = SurvRes.Nails, inAmt3 = 1,
                    outRes = SurvRes.FurCoat, outAmt = 1, duration = 7.5f,
                },
                new WorkshopRecipe
                {
                    id = "plate_armor", name = "废土钢板护甲", iconKey = "ico_armor", unlockBuildingLv = 4, unlockCampLv = 14,
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
                    sourceHint = "采摘林边野生浆果丛或收割选种农田获取【鲜蔬】",
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
                    id = "grill_fish", name = "香烤山溪鱼", iconKey = "rec_roast_fish", unlockBuildingLv = 2, unlockCampLv = 4,
                    sourceHint = "需炊事房 Lv.2 + 南岸【河畔钓鱼台】垂钓的鲜鱼×2",
                    inRes1 = SurvRes.Fish, inAmt1 = 2, inRes2 = SurvRes.Wood, inAmt2 = 1,
                    outRes = SurvRes.GrillFish, outAmt = 1, bonusRation = 6, duration = 5.0f,
                },
                new WorkshopRecipe
                {
                    id = "smoked_meat", name = "松木秘制熏肉", iconKey = "ico_ration", unlockBuildingLv = 3, unlockCampLv = 8,
                    sourceHint = "需炊事房 Lv.3 + 山林畜牧栏/捕猎哨塔获取的【鲜肉】×2",
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
                    id = "poultice", name = "止血草药敷包", iconKey = "ico_poultice", unlockBuildingLv = 1, unlockCampLv = 7,
                    sourceHint = "在【生态选种农田】播下草药种收获【山草药】+【粗麻布】",
                    inRes1 = SurvRes.Herb, inAmt1 = 2, inRes2 = SurvRes.Cloth, inAmt2 = 1,
                    outRes = SurvRes.HerbPoultice, outAmt = 1, duration = 4.8f,
                },
                new WorkshopRecipe
                {
                    id = "warm_tea", name = "驱寒草本热饮", iconKey = "ico_tea", unlockBuildingLv = 2, unlockCampLv = 10,
                    sourceHint = "需草药坊 Lv.2 + 山草药×2 + 净水×2",
                    inRes1 = SurvRes.Herb, inAmt1 = 2, inRes2 = SurvRes.Water, inAmt2 = 2,
                    outRes = SurvRes.WarmTea, outAmt = 1, duration = 6.0f,
                },
                new WorkshopRecipe
                {
                    id = "medkit", name = "抗尸毒急救箱", iconKey = "ico_medkit", unlockBuildingLv = 3, unlockCampLv = 13,
                    sourceHint = "需草药坊 Lv.3 + 止血药包 + 医疗电子件 + 锻打铁锭",
                    inRes1 = SurvRes.HerbPoultice, inAmt1 = 1, inRes2 = SurvRes.Parts, inAmt2 = 1, inRes3 = SurvRes.IronIngot, inAmt3 = 1,
                    outRes = SurvRes.MedKit, outAmt = 1, duration = 8.5f,
                },
            };
        }

        // =====================================================================
        // 全营地 22 座建筑宽敞 32×32 分层落位（仅 3 座核心建筑开局可见，其余全随砍树扩地+收集小物件动态显露！）
        // =====================================================================
        void InitNodes()
        {
            // --- 开局林间草坪核心 3 建筑（state = Active，宽敞环绕初始营心 13..20, 17..21） ---
            Nodes.Add(new CampNode
            {
                id = "shelter", kind = NodeKind.Shelter, state = NodeState.Active, level = 1, maxLevel = 3, unlockPlayerLv = 1,
                activeTitle = "避难所议事厅", ruinTitle = "避难所议事厅",
                desc = "逃难小队统筹指挥中枢。升级可额外增加供暖人口承载并开放高阶防区。",
                benefitLabel = "中枢额外人口承载", benefitFrom = 0, benefitTo = 3, benefitRes = SurvRes.Blueprint,
                gx = 16.0f, gy = 21.0f, occupyX = 15, occupyY = 20, occupyW = 3, occupyH = 3,
                revealChunkX = 5, revealChunkY = 7, buildDuration = 4.5f,
                requiresSiteSalvage = false, siteUncovered = true, siteSalvageCleared = true, compassDir = "中枢",
            });

            Nodes.Add(new CampNode
            {
                id = "heat_tower", kind = NodeKind.HeatTower, state = NodeState.Active, level = 1, maxLevel = 4, unlockPlayerLv = 1,
                activeTitle = "篝火供暖塔", ruinTitle = "篝火供暖塔",
                desc = "深山入秋寒气逼人！升级供暖炉塔扩大温暖覆盖半径，是提升营地【供暖人口上限 MAX】的核心基建！",
                benefitLabel = "营地供暖人口上限", benefitFrom = 8, benefitTo = 15, benefitRes = SurvRes.Wood,
                gx = 13.0f, gy = 18.2f, occupyX = 12, occupyY = 17, occupyW = 2, occupyH = 2,
                revealChunkX = 4, revealChunkY = 6, buildDuration = 4.0f,
                requiresSiteSalvage = false, siteUncovered = true, siteSalvageCleared = true, compassDir = "营心",
            });

            Nodes.Add(new CampNode
            {
                id = "warehouse", kind = NodeKind.Warehouse, state = NodeState.Active, level = 1, maxLevel = 5, unlockPlayerLv = 1,
                activeTitle = "营地物资仓库", ruinTitle = "营地物资仓库",
                desc = "虽然捡来的多是废木烂铁，也得码放整齐。需足够【当前人口】看守，升级大幅提升全原料【储量上限】！",
                benefitLabel = "单品原料储量上限", benefitFrom = 80, benefitTo = 180, benefitRes = SurvRes.Plank,
                gx = 19.6f, gy = 18.4f, occupyX = 19, occupyY = 17, occupyW = 2, occupyH = 2,
                revealChunkX = 6, revealChunkY = 6, buildDuration = 3.5f,
                requiresSiteSalvage = false, siteUncovered = true, siteSalvageCleared = true, compassDir = "营心东侧",
            });

            // --- 近环 Tier 1（砍伐 1~2 块林缘树木 -> 发现散落小物件收集整合 -> 显露地基建造！） ---
            Nodes.Add(new CampNode
            {
                id = "sawmill", kind = NodeKind.Sawmill, state = NodeState.HiddenInForest, level = 0, maxLevel = 3, unlockPlayerLv = 1,
                hintLabel = "木工旧址", ruinTitle = "锯木木艺坊", activeTitle = "锯木木艺坊",
                desc = "加工【厚木板】、【防潮木床铺】与【包铁承重梁】，建成后学徒还会定期巡林带回再生原木！",
                benefitLabel = "木艺队列槽位与床铺配方", benefitFrom = 0, benefitTo = 3, benefitRes = SurvRes.Plank,
                gx = 22.9f, gy = 16.0f, occupyX = 21, occupyY = 15, occupyW = 3, occupyH = 3,
                revealChunkX = 7, revealChunkY = 5, buildDuration = 3.5f,
                buildCoinCost = 30, buildReqPop = 4,
                buildCosts = new[] { new CostPair(SurvRes.Wood, 10) },
                requiresSiteSalvage = true, siteUncovered = false, siteSalvageCleared = false,
                compassDir = "东侧", salvageLabel = "散落木工锯具", salvageBonusRes = SurvRes.Wood, salvageBonusAmt = 8,
            });

            Nodes.Add(new CampNode
            {
                id = "farm_a", kind = NodeKind.Farm, state = NodeState.HiddenInForest, level = 0, maxLevel = 4, unlockPlayerLv = 1,
                hintLabel = "向阳沃土", ruinTitle = "生态选种农田", activeTitle = "生态选种农田",
                desc = "营地核心选种良田（随升级扩至2→4垄）！自由播下鲜蔬种、亚麻种、草药种与变异金种，联接净水井滴灌、供暖塔暖温与畜牧栏堆肥，收获保底返种永不断粮断药！",
                benefitLabel = "解锁独立田垄与产量加成", benefitFrom = 0, benefitTo = 2, benefitRes = SurvRes.Veggie,
                gx = 12.6f, gy = 13.8f, occupyX = 12, occupyY = 13, occupyW = 2, occupyH = 2,
                revealChunkX = 4, revealChunkY = 4, buildDuration = 3.0f,
                buildCoinCost = 25, buildReqPop = 4,
                buildCosts = new[] { new CostPair(SurvRes.Wood, 8) },
                requiresSiteSalvage = true, siteUncovered = false, siteSalvageCleared = false,
                compassDir = "西南", salvageLabel = "向阳菜种包裹", salvageBonusRes = SurvRes.SeedVeggie, salvageBonusAmt = 3,
            });

            Nodes.Add(new CampNode
            {
                id = "kitchen", kind = NodeKind.Kitchen, state = NodeState.HiddenInForest, level = 0, maxLevel = 3, unlockPlayerLv = 1,
                hintLabel = "林边野灶", ruinTitle = "野味炊事房", activeTitle = "野味炊事房",
                desc = "将鲜蔬、山泉、溪鱼与野味烹制为热腾腾的山林料理，既满足居民餐饮订单赚铜币，又补充拓荒口粮！",
                benefitLabel = "开放高阶菜谱与队列槽", benefitFrom = 0, benefitTo = 3, benefitRes = SurvRes.RoastVeggie,
                gx = 9.0f, gy = 18.2f, occupyX = 8, occupyY = 17, occupyW = 2, occupyH = 2,
                revealChunkX = 3, revealChunkY = 6, buildDuration = 3.5f,
                buildCoinCost = 20, buildReqPop = 4,
                buildCosts = new[] { new CostPair(SurvRes.Wood, 6) },
                requiresSiteSalvage = true, siteUncovered = false, siteSalvageCleared = false,
                compassDir = "西侧", salvageLabel = "行军野炊铁锅", salvageBonusRes = SurvRes.Veggie, salvageBonusAmt = 4,
            });

            Nodes.Add(new CampNode
            {
                id = "house_1", kind = NodeKind.Dwelling, state = NodeState.HiddenInForest, level = 0, maxLevel = 4, unlockPlayerLv = 1,
                hintLabel = "避风缓坡", ruinTitle = "1号避风民居", activeTitle = "1号避风民居",
                desc = "一顶补丁帆布加几捆干草，总比冻在深山里强。消耗供暖上限吸纳新难民，提升【当前实有人口】！",
                benefitLabel = "新增入驻人口", benefitFrom = 0, benefitTo = 3, benefitRes = SurvRes.WoodBed,
                gx = 10.6f, gy = 22.4f, occupyX = 10, occupyY = 22, occupyW = 2, occupyH = 2,
                revealChunkX = 3, revealChunkY = 7, buildDuration = 3.5f,
                buildCoinCost = 30, buildAddPop = 3,
                buildCosts = new[] { new CostPair(SurvRes.Wood, 6), new CostPair(SurvRes.Plank, 2) },
                requiresSiteSalvage = true, siteUncovered = false, siteSalvageCleared = false,
                compassDir = "西北", salvageLabel = "旧帆布宿营包", salvageBonusRes = SurvRes.Wood, salvageBonusAmt = 6,
            });

            // --- 中环 Tier 2（向外拓荒 2~3 圈，宽间距舒展排布！） ---
            Nodes.Add(new CampNode
            {
                id = "relic_well", kind = NodeKind.RelicWell, state = NodeState.HiddenInForest, level = 0, maxLevel = 3, unlockPlayerLv = 2,
                hintLabel = "坍塌古井", ruinTitle = "净水石井", activeTitle = "净水石井",
                desc = "深山古泉旧井，修好辘轳可持续汲取【山泉净水】，灌溉菜麻田并熬制浓汤草药饮。",
                benefitLabel = "储水上限与汲水效率", benefitFrom = 0, benefitTo = 10, benefitRes = SurvRes.Water,
                gx = 22.6f, gy = 12.0f, occupyX = 22, occupyY = 12, occupyW = 1, occupyH = 1,
                revealChunkX = 7, revealChunkY = 4, buildDuration = 3.5f,
                buildCoinCost = 60, buildReqPop = 4,
                buildCosts = new[] { new CostPair(SurvRes.Wood, 10), new CostPair(SurvRes.Plank, 4) },
                prodInterval = 6.0f, prodOutRes = SurvRes.Water, prodOutCount = 3, maxStored = 12,
                requiresSiteSalvage = true, siteUncovered = false, siteSalvageCleared = false,
                compassDir = "东南", salvageLabel = "古井散落石料", salvageBonusRes = SurvRes.Plank, salvageBonusAmt = 3,
            });

            Nodes.Add(new CampNode
            {
                id = "beast_ranch", kind = NodeKind.Ranch, state = NodeState.HiddenInForest, level = 0, maxLevel = 3, unlockPlayerLv = 2,
                hintLabel = "避风草坡", ruinTitle = "山林畜牧栏", activeTitle = "山林畜牧栏",
                desc = "饲养雪兔、山猪与梅花绒鹿！投喂农田【鲜蔬】与石井【净水】，稳定产出急需的【鲜肉】、【兽毛皮】，并副产【有机堆肥】反哺农田催熟增产！",
                benefitLabel = "解锁饲养栏位与肉皮堆肥产出", benefitFrom = 0, benefitTo = 2, benefitRes = SurvRes.Pelt,
                gx = 7.4f, gy = 13.6f, occupyX = 6, occupyY = 13, occupyW = 2, occupyH = 2,
                revealChunkX = 2, revealChunkY = 4, buildDuration = 3.5f,
                buildCoinCost = 80, buildReqPop = 4,
                buildCosts = new[] { new CostPair(SurvRes.Plank, 6), new CostPair(SurvRes.Wood, 8) },
                requiresSiteSalvage = true, siteUncovered = false, siteSalvageCleared = false,
                compassDir = "正西", salvageLabel = "草坡旧围栏桩", salvageBonusRes = SurvRes.Veggie, salvageBonusAmt = 4,
            });

            Nodes.Add(new CampNode
            {
                id = "smithy", kind = NodeKind.Smithy, state = NodeState.HiddenInForest, level = 0, maxLevel = 3, unlockPlayerLv = 3,
                hintLabel = "旧锻铁台", ruinTitle = "废铁锻造坊", activeTitle = "废铁锻造坊",
                desc = "全营地五金重工心脏！将废铁锤炼为【铁钉铆件】、【锻打铁锭】与【导流金属管】，一切进阶建筑必经之地！",
                benefitLabel = "五金冶炼队列槽位", benefitFrom = 0, benefitTo = 3, benefitRes = SurvRes.Nails,
                gx = 26.9f, gy = 15.2f, occupyX = 26, occupyY = 14, occupyW = 2, occupyH = 2,
                revealChunkX = 8, revealChunkY = 5, buildDuration = 4.0f,
                buildCoinCost = 180, buildReqPop = 6,
                buildCosts = new[] { new CostPair(SurvRes.Plank, 8), new CostPair(SurvRes.Scrap, 6) },
                requiresSiteSalvage = true, siteUncovered = false, siteSalvageCleared = false,
                compassDir = "正东", salvageLabel = "旧铁砧与废铁", salvageBonusRes = SurvRes.Scrap, salvageBonusAmt = 6,
            });

            Nodes.Add(new CampNode
            {
                id = "tailor", kind = NodeKind.Tailor, state = NodeState.HiddenInForest, level = 0, maxLevel = 4, unlockPlayerLv = 3,
                hintLabel = "破旧缝纫棚", ruinTitle = "裁缝防具坊", activeTitle = "裁缝防具坊",
                desc = "用粗麻布与野兽毛皮缝制【麻布衣】、【猎皮靴】、【御寒皮衣】与【钢板护甲】，既交高金居民订单，又解锁全队角色换装！",
                benefitLabel = "防具制衣队列槽位", benefitFrom = 0, benefitTo = 3, benefitRes = SurvRes.LinenShirt,
                gx = 22.6f, gy = 22.2f, occupyX = 22, occupyY = 21, occupyW = 2, occupyH = 2,
                revealChunkX = 7, revealChunkY = 7, buildDuration = 4.0f,
                buildCoinCost = 160, buildReqPop = 6,
                buildCosts = new[] { new CostPair(SurvRes.Plank, 8), new CostPair(SurvRes.Cloth, 2) },
                requiresSiteSalvage = true, siteUncovered = false, siteSalvageCleared = false,
                compassDir = "东北", salvageLabel = "旧纺车针线箱", salvageBonusRes = SurvRes.Cloth, salvageBonusAmt = 3,
            });

            // --- 外环 Tier 3 & 4（全山谷 6..27 舒展分布，任意两节点间距 >= 4.0 格！） ---
            Nodes.Add(new CampNode
            {
                id = "river_dock", kind = NodeKind.RiverDock, state = NodeState.HiddenInForest, level = 0, maxLevel = 2, unlockPlayerLv = 4,
                hintLabel = "潺潺溪水声", ruinTitle = "河畔钓鱼台", activeTitle = "河畔钓鱼台",
                desc = "南坡清溪回水湾的木栈钓台，甩杆垂钓与鱼篓挂机持续产出【山溪鲜鱼】。",
                benefitLabel = "鱼篓储量与鲜鱼产出", benefitFrom = 0, benefitTo = 10, benefitRes = SurvRes.Fish,
                gx = 13.2f, gy = 4.6f, occupyX = 12, occupyY = 4, occupyW = 2, occupyH = 1,
                revealChunkX = 4, revealChunkY = 1, buildDuration = 3.5f,
                buildCoinCost = 180, buildReqPop = 6,
                buildCosts = new[] { new CostPair(SurvRes.Plank, 6), new CostPair(SurvRes.Nails, 2) },
                prodInterval = 7.5f, prodOutRes = SurvRes.Fish, prodOutCount = 2, maxStored = 12,
                requiresSiteSalvage = true, siteUncovered = false, siteSalvageCleared = false,
                compassDir = "南岸", salvageLabel = "溪岸旧渔具箱", salvageBonusRes = SurvRes.Wood, salvageBonusAmt = 6,
            });

            Nodes.Add(new CampNode
            {
                id = "house_2", kind = NodeKind.Dwelling, state = NodeState.HiddenInForest, level = 0, maxLevel = 4, unlockPlayerLv = 5,
                hintLabel = "背风林地", ruinTitle = "2号原木民居", activeTitle = "2号原木民居",
                desc = "加厚原木墙体的第二座宿营屋，大幅扩充【当前实有人口】以支撑中后期工厂运转。",
                benefitLabel = "新增入驻人口", benefitFrom = 0, benefitTo = 5, benefitRes = SurvRes.WoodBed,
                gx = 6.2f, gy = 22.2f, occupyX = 6, occupyY = 21, occupyW = 2, occupyH = 2,
                revealChunkX = 2, revealChunkY = 7, buildDuration = 4.0f,
                buildCoinCost = 320, buildAddPop = 5,
                buildCosts = new[] { new CostPair(SurvRes.Plank, 10), new CostPair(SurvRes.Nails, 4) },
                requiresSiteSalvage = true, siteUncovered = false, siteSalvageCleared = false,
                compassDir = "西北外林", salvageLabel = "伐木工宿营木料", salvageBonusRes = SurvRes.Plank, salvageBonusAmt = 4,
            });

            Nodes.Add(new CampNode
            {
                id = "watchtower", kind = NodeKind.Watchtower, state = NodeState.HiddenInForest, level = 0, maxLevel = 2, unlockPlayerLv = 6,
                hintLabel = "制高树台", ruinTitle = "捕猎木哨塔", activeTitle = "捕猎木哨塔",
                desc = "林缘高架瞭望塔：自动射猎野鹿野猪同时产出【野味鲜肉 + 野兽毛皮】，并在尸潮来袭时远程狙击！",
                benefitLabel = "自动狩猎鲜肉毛皮与城防", benefitFrom = 0, benefitTo = 120, benefitRes = SurvRes.Pelt,
                gx = 14.6f, gy = 25.8f, occupyX = 14, occupyY = 25, occupyW = 2, occupyH = 2,
                revealChunkX = 4, revealChunkY = 8, buildDuration = 4.0f,
                buildCoinCost = 380, buildReqPop = 9,
                buildCosts = new[] { new CostPair(SurvRes.Plank, 10), new CostPair(SurvRes.Nails, 4) },
                prodInterval = 9.0f, prodOutRes = SurvRes.Meat, prodOutCount = 2, prodOutRes2 = SurvRes.Pelt, prodOutCount2 = 2, maxStored = 14,
                requiresSiteSalvage = true, siteUncovered = false, siteSalvageCleared = false,
                compassDir = "北侧林岭", salvageLabel = "猎人遗落兽夹箭袋", salvageBonusRes = SurvRes.Pelt, salvageBonusAmt = 3,
            });

            Nodes.Add(new CampNode
            {
                id = "barricade", kind = NodeKind.Barricade, state = NodeState.HiddenInForest, level = 0, maxLevel = 2, unlockPlayerLv = 5,
                hintLabel = "南口隘道", ruinTitle = "尖木拒马防线", activeTitle = "尖木拒马防线",
                desc = "沿营地南侧山口布设削尖原木拒马与铁蒺藜，拦截游荡丧尸偷粮并稳固尸潮防线。",
                benefitLabel = "营地山口城防坚固度", benefitFrom = 10, benefitTo = 100, benefitRes = SurvRes.Plank,
                gx = 17.6f, gy = 9.6f, occupyX = 17, occupyY = 9, occupyW = 2, occupyH = 2,
                revealChunkX = 5, revealChunkY = 3, buildDuration = 3.5f,
                buildCoinCost = 240, buildReqPop = 8,
                buildCosts = new[] { new CostPair(SurvRes.Plank, 6), new CostPair(SurvRes.Nails, 2) },
                requiresSiteSalvage = true, siteUncovered = false, siteSalvageCleared = false,
                compassDir = "正南", salvageLabel = "旧哨卡削尖木桩", salvageBonusRes = SurvRes.Plank, salvageBonusAmt = 4,
            });

            Nodes.Add(new CampNode
            {
                id = "herb_hut", kind = NodeKind.HerbHut, state = NodeState.HiddenInForest, level = 0, maxLevel = 3, unlockPlayerLv = 7,
                hintLabel = "采药人旧棚", ruinTitle = "山林草药坊", activeTitle = "山林草药坊",
                desc = "熬制【止血草药敷包】、【驱寒草本热饮】与【抗尸毒急救箱】，伤病居民订单经验极高，更是山口行商重金求购的王牌货！",
                benefitLabel = "防疫制药队列槽位", benefitFrom = 0, benefitTo = 3, benefitRes = SurvRes.HerbPoultice,
                gx = 7.6f, gy = 4.8f, occupyX = 7, occupyY = 4, occupyW = 2, occupyH = 2,
                revealChunkX = 2, revealChunkY = 1, buildDuration = 4.2f,
                buildCoinCost = 480, buildReqPop = 11,
                buildCosts = new[] { new CostPair(SurvRes.Plank, 10), new CostPair(SurvRes.Nails, 4), new CostPair(SurvRes.Cloth, 3) },
                requiresSiteSalvage = true, siteUncovered = false, siteSalvageCleared = false,
                compassDir = "西南溪谷", salvageLabel = "采药人竹篓药臼", salvageBonusRes = SurvRes.Herb, salvageBonusAmt = 4,
            });

            Nodes.Add(new CampNode
            {
                id = "relic_loot_sw", kind = NodeKind.RelicLoot, state = NodeState.HiddenInForest, level = 0, unlockPlayerLv = 2,
                hintLabel = "废弃救援车箱", ruinTitle = "遇难车队物资箱", activeTitle = "遇难车队物资箱",
                desc = "早先逃难车队翻车遗落的金属箱，拆解可回收首批【废铁】、【医疗电子件】与【工程手稿】。",
                gx = 18.4f, gy = 5.0f, occupyX = 18, occupyY = 5, occupyW = 1, occupyH = 1,
                revealChunkX = 6, revealChunkY = 1,
                requiresSiteSalvage = true, siteUncovered = false, siteSalvageCleared = false,
                compassDir = "南侧山谷", salvageLabel = "翻车散落五金包", salvageBonusRes = SurvRes.Scrap, salvageBonusAmt = 6,
            });

            Nodes.Add(new CampNode
            {
                id = "relic_truck", kind = NodeKind.RelicTruck, state = NodeState.HiddenInForest, level = 0, maxLevel = 2, unlockPlayerLv = 8,
                hintLabel = "报废破皮卡", ruinTitle = "末日武装皮卡", activeTitle = "末日武装皮卡",
                desc = "焊上包铁撞角并修好引擎，即可开车下山进【丧尸城 4 大街区】大宗搜刮【工程手稿】、废铁与医疗件！",
                benefitLabel = "单趟进城搜刮载重上限", benefitFrom = 0, benefitTo = 60, benefitRes = SurvRes.Blueprint,
                gx = 10.2f, gy = 26.8f, occupyX = 9, occupyY = 26, occupyW = 2, occupyH = 2,
                revealChunkX = 3, revealChunkY = 8, buildDuration = 4.5f,
                buildCoinCost = 580, buildReqPop = 12,
                buildCosts = new[] { new CostPair(SurvRes.Plank, 10), new CostPair(SurvRes.Nails, 6), new CostPair(SurvRes.Scrap, 8) },
                requiresSiteSalvage = true, siteUncovered = false, siteSalvageCleared = false,
                compassDir = "北坡山道", salvageLabel = "越野车备胎工具箱", salvageBonusRes = SurvRes.Scrap, salvageBonusAmt = 6,
            });

            Nodes.Add(new CampNode
            {
                id = "relic_road_gate", kind = NodeKind.RelicRoadGate, state = NodeState.HiddenInForest, level = 0, maxLevel = 1, unlockPlayerLv = 8,
                hintLabel = "丧尸城山道", ruinTitle = "盘山公路关卡", activeTitle = "盘山公路关卡",
                desc = "深山通往沦陷市区的唯一公路闸口。清理路障后武装皮卡即可随时发车下山搜刮！",
                benefitLabel = "开放丧尸城搜刮街区", benefitFrom = 0, benefitTo = 4, benefitRes = SurvRes.Blueprint,
                gx = 19.2f, gy = 26.4f, occupyX = 18, occupyY = 25, occupyW = 2, occupyH = 2,
                revealChunkX = 6, revealChunkY = 8, buildDuration = 3.5f,
                buildCoinCost = 320, buildReqPop = 11,
                buildCosts = new[] { new CostPair(SurvRes.Plank, 8), new CostPair(SurvRes.Nails, 4) },
                requiresSiteSalvage = true, siteUncovered = false, siteSalvageCleared = false,
                compassDir = "东北山口", salvageLabel = "公路岗亭撬棍物资", salvageBonusRes = SurvRes.Nails, salvageBonusAmt = 3,
            });

            Nodes.Add(new CampNode
            {
                id = "relic_generator", kind = NodeKind.RelicGenerator, state = NodeState.HiddenInForest, level = 0, maxLevel = 3, unlockPlayerLv = 9,
                hintLabel = "废弃水轮机房", ruinTitle = "山涧发电房", activeTitle = "山涧发电房",
                desc = "修复林间柴油水轮并网机组，为全营地五大生产作坊提供强劲电力加速（+25% → +65%）！",
                benefitLabel = "全作坊电力提速加成(%)", benefitFrom = 0, benefitTo = 25, benefitRes = SurvRes.Parts,
                gx = 26.8f, gy = 20.2f, occupyX = 26, occupyY = 19, occupyW = 2, occupyH = 2,
                revealChunkX = 8, revealChunkY = 6, buildDuration = 5.0f,
                buildCoinCost = 980, buildReqPop = 15,
                buildCosts = new[] { new CostPair(SurvRes.Blueprint, 3), new CostPair(SurvRes.IronIngot, 3), new CostPair(SurvRes.Parts, 2) },
                requiresSiteSalvage = true, siteUncovered = false, siteSalvageCleared = false,
                compassDir = "正东高地", salvageLabel = "旧铜线与柴油滤芯", salvageBonusRes = SurvRes.Parts, salvageBonusAmt = 2,
            });

            Nodes.Add(new CampNode
            {
                id = "radio_post", kind = NodeKind.RadioPost, state = NodeState.HiddenInForest, level = 0, maxLevel = 2, unlockPlayerLv = 10,
                hintLabel = "断线电台塔", ruinTitle = "山口行商电台", activeTitle = "山口行商电台",
                desc = "修复短波求生电台联络邻山聚落，招来【山口行商驼队】高价收购皮衣、急救箱与熏肉，换取巨额铜币与工程手稿！",
                benefitLabel = "行商收购溢价与手稿配额", benefitFrom = 0, benefitTo = 100, benefitRes = SurvRes.Blueprint,
                gx = 26.4f, gy = 25.2f, occupyX = 25, occupyY = 24, occupyW = 2, occupyH = 2,
                revealChunkX = 8, revealChunkY = 8, buildDuration = 4.2f,
                buildCoinCost = 950, buildReqPop = 16,
                buildCosts = new[] { new CostPair(SurvRes.Blueprint, 3), new CostPair(SurvRes.IronBeam, 3), new CostPair(SurvRes.Parts, 2) },
                requiresSiteSalvage = true, siteUncovered = false, siteSalvageCleared = false,
                compassDir = "东北山巅", salvageLabel = "军用短波天线包", salvageBonusRes = SurvRes.Parts, salvageBonusAmt = 2,
            });

            Nodes.Add(new CampNode
            {
                id = "house_3", kind = NodeKind.Dwelling, state = NodeState.HiddenInForest, level = 0, maxLevel = 3, unlockPlayerLv = 11,
                hintLabel = "向阳山坪", ruinTitle = "3号幸存者木楼", activeTitle = "3号幸存者木楼",
                desc = "双层防寒木结构宿营楼，铺设木床与供暖铁管，是中后期突破 30+ 人口大关的主力居所！",
                benefitLabel = "新增入驻人口", benefitFrom = 0, benefitTo = 8, benefitRes = SurvRes.WoodBed,
                gx = 23.8f, gy = 7.4f, occupyX = 23, occupyY = 7, occupyW = 2, occupyH = 2,
                revealChunkX = 7, revealChunkY = 2, buildDuration = 4.5f,
                buildCoinCost = 1350, buildAddPop = 8,
                buildCosts = new[] { new CostPair(SurvRes.Blueprint, 4), new CostPair(SurvRes.IronBeam, 4), new CostPair(SurvRes.WoodBed, 2) },
                requiresSiteSalvage = true, siteUncovered = false, siteSalvageCleared = false,
                compassDir = "东南向阳坡", salvageLabel = "加固圆木榫卯料", salvageBonusRes = SurvRes.Plank, salvageBonusAmt = 6,
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
                case NodeKind.Shelter: return nextLv == 2 ? 5 : 14;
                case NodeKind.HeatTower: return nextLv == 2 ? 4 : (nextLv == 3 ? 9 : 12);
                case NodeKind.Dwelling:
                    if (n.id == "house_1") return nextLv == 2 ? 2 : (nextLv == 3 ? 6 : 12);
                    if (n.id == "house_2") return nextLv == 2 ? 7 : (nextLv == 3 ? 10 : 14);
                    return nextLv == 2 ? 13 : 15;
                case NodeKind.Warehouse: return nextLv == 2 ? 2 : (nextLv == 3 ? 6 : (nextLv == 4 ? 11 : 15));
                case NodeKind.Sawmill: return nextLv == 2 ? 5 : 7;
                case NodeKind.Smithy: return nextLv == 2 ? 8 : 11;
                case NodeKind.Tailor: return nextLv == 2 ? 6 : (nextLv == 3 ? 9 : 14);
                case NodeKind.Kitchen: return nextLv == 2 ? 4 : 8;
                case NodeKind.HerbHut: return nextLv == 2 ? 10 : 13;
                case NodeKind.RelicWell: return nextLv == 2 ? 4 : 12;
                case NodeKind.Farm: return nextLv == 2 ? 3 : (nextLv == 3 ? 11 : 14);
                case NodeKind.Ranch: return nextLv == 2 ? 6 : 12;
                case NodeKind.RiverDock: return 7;
                case NodeKind.Watchtower: return 10;
                case NodeKind.Barricade: return 9;
                case NodeKind.RelicGenerator: return nextLv == 2 ? 12 : 15;
                case NodeKind.RadioPost: return 13;
                case NodeKind.RelicTruck: return 13;
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
                    if (nextLv == 2) { coinCost = 260; reqPopCur = 6; matCosts = new[] { new CostPair(SurvRes.Plank, 10), new CostPair(SurvRes.Nails, 4) }; }
                    else if (nextLv == 3) { coinCost = 1050; reqPopCur = 15; matCosts = new[] { new CostPair(SurvRes.Blueprint, 4), new CostPair(SurvRes.IronBeam, 4), new CostPair(SurvRes.IronIngot, 3) }; }
                    else { coinCost = 2100; reqPopCur = 24; matCosts = new[] { new CostPair(SurvRes.Blueprint, 6), new CostPair(SurvRes.MetalPipe, 4), new CostPair(SurvRes.IronIngot, 5) }; }
                    break;

                case NodeKind.Shelter:
                    if (nextLv == 2) { coinCost = 380; reqPopCur = 8; matCosts = new[] { new CostPair(SurvRes.Plank, 12), new CostPair(SurvRes.Nails, 4) }; }
                    else { coinCost = 2400; reqPopCur = 28; matCosts = new[] { new CostPair(SurvRes.Blueprint, 6), new CostPair(SurvRes.IronBeam, 6), new CostPair(SurvRes.IronIngot, 5) }; }
                    break;

                case NodeKind.Dwelling:
                {
                    int curHousePop = GetDwellingPopForLevel(n.id, n.level);
                    int nextHousePop = GetDwellingPopForLevel(n.id, nextLv);
                    reqPopCap = CurrentPop - curHousePop + nextHousePop;
                    if (n.id == "house_1")
                    {
                        if (nextLv == 2) { coinCost = 100; matCosts = new[] { new CostPair(SurvRes.Wood, 12), new CostPair(SurvRes.Plank, 6) }; }
                        else if (nextLv == 3) { coinCost = 450; matCosts = new[] { new CostPair(SurvRes.WoodBed, 2), new CostPair(SurvRes.LinenShirt, 2), new CostPair(SurvRes.Nails, 4) }; }
                        else { coinCost = 1500; matCosts = new[] { new CostPair(SurvRes.Blueprint, 4), new CostPair(SurvRes.MetalPipe, 3), new CostPair(SurvRes.FurCoat, 2) }; }
                    }
                    else if (n.id == "house_2")
                    {
                        if (nextLv == 2) { coinCost = 560; matCosts = new[] { new CostPair(SurvRes.Plank, 10), new CostPair(SurvRes.WoodBed, 2), new CostPair(SurvRes.Nails, 4) }; }
                        else if (nextLv == 3) { coinCost = 720; matCosts = new[] { new CostPair(SurvRes.WoodBed, 2), new CostPair(SurvRes.LinenShirt, 2), new CostPair(SurvRes.Nails, 4) }; }
                        else { coinCost = 1950; matCosts = new[] { new CostPair(SurvRes.Blueprint, 5), new CostPair(SurvRes.MetalPipe, 4), new CostPair(SurvRes.FurCoat, 2) }; }
                    }
                    else
                    {
                        if (nextLv == 2) { coinCost = 1680; matCosts = new[] { new CostPair(SurvRes.Blueprint, 4), new CostPair(SurvRes.WoodBed, 2), new CostPair(SurvRes.MetalPipe, 3) }; }
                        else { coinCost = 2600; matCosts = new[] { new CostPair(SurvRes.Blueprint, 6), new CostPair(SurvRes.MetalPipe, 4), new CostPair(SurvRes.PlateArmor, 1) }; }
                    }
                    break;
                }

                case NodeKind.Warehouse:
                    if (nextLv == 2) { coinCost = 120; reqPopCur = 6; matCosts = new[] { new CostPair(SurvRes.Plank, 8) }; }
                    else if (nextLv == 3) { coinCost = 520; reqPopCur = 12; matCosts = new[] { new CostPair(SurvRes.Blueprint, 2), new CostPair(SurvRes.Nails, 6), new CostPair(SurvRes.Plank, 10) }; }
                    else if (nextLv == 4) { coinCost = 1600; reqPopCur = 20; matCosts = new[] { new CostPair(SurvRes.Blueprint, 5), new CostPair(SurvRes.IronBeam, 5), new CostPair(SurvRes.IronIngot, 4) }; }
                    else { coinCost = 3200; reqPopCur = 32; matCosts = new[] { new CostPair(SurvRes.Blueprint, 8), new CostPair(SurvRes.MetalPipe, 6), new CostPair(SurvRes.IronIngot, 6) }; }
                    break;

                case NodeKind.Sawmill:
                    if (nextLv == 2) { coinCost = 380; reqPopCur = 9; matCosts = new[] { new CostPair(SurvRes.Blueprint, 2), new CostPair(SurvRes.Plank, 10), new CostPair(SurvRes.Nails, 4) }; }
                    // 关键破锁点：锯木坊Lv.3仅需手稿+木板+铁钉即可产出包铁承重梁，不再与锻造坊Lv.2互锁！
                    else { coinCost = 780; reqPopCur = 13; matCosts = new[] { new CostPair(SurvRes.Blueprint, 3), new CostPair(SurvRes.Plank, 12), new CostPair(SurvRes.Nails, 6) }; }
                    break;

                case NodeKind.Smithy:
                    if (nextLv == 2) { coinCost = 820; reqPopCur = 14; matCosts = new[] { new CostPair(SurvRes.Blueprint, 3), new CostPair(SurvRes.IronBeam, 3), new CostPair(SurvRes.Scrap, 10) }; }
                    else { coinCost = 1550; reqPopCur = 20; matCosts = new[] { new CostPair(SurvRes.Blueprint, 5), new CostPair(SurvRes.IronIngot, 4), new CostPair(SurvRes.Parts, 3) }; }
                    break;

                case NodeKind.Tailor:
                    if (nextLv == 2) { coinCost = 480; reqPopCur = 10; matCosts = new[] { new CostPair(SurvRes.Blueprint, 2), new CostPair(SurvRes.Plank, 10), new CostPair(SurvRes.Nails, 4) }; }
                    else if (nextLv == 3) { coinCost = 1100; reqPopCur = 16; matCosts = new[] { new CostPair(SurvRes.Blueprint, 4), new CostPair(SurvRes.IronBeam, 3), new CostPair(SurvRes.Pelt, 5) }; }
                    else { coinCost = 2500; reqPopCur = 28; matCosts = new[] { new CostPair(SurvRes.Blueprint, 7), new CostPair(SurvRes.IronIngot, 5), new CostPair(SurvRes.MetalPipe, 4) }; }
                    break;

                case NodeKind.Kitchen:
                    if (nextLv == 2) { coinCost = 240; reqPopCur = 7; matCosts = new[] { new CostPair(SurvRes.Plank, 8), new CostPair(SurvRes.Nails, 3) }; }
                    else { coinCost = 860; reqPopCur = 15; matCosts = new[] { new CostPair(SurvRes.Blueprint, 3), new CostPair(SurvRes.IronBeam, 3), new CostPair(SurvRes.Plank, 10) }; }
                    break;

                case NodeKind.HerbHut:
                    if (nextLv == 2) { coinCost = 1150; reqPopCur = 17; matCosts = new[] { new CostPair(SurvRes.Blueprint, 4), new CostPair(SurvRes.IronBeam, 3), new CostPair(SurvRes.Herb, 6) }; }
                    else { coinCost = 2200; reqPopCur = 25; matCosts = new[] { new CostPair(SurvRes.Blueprint, 6), new CostPair(SurvRes.MetalPipe, 4), new CostPair(SurvRes.Parts, 4) }; }
                    break;

                case NodeKind.RelicWell:
                    if (nextLv == 2) { coinCost = 300; reqPopCur = 7; matCosts = new[] { new CostPair(SurvRes.Plank, 8), new CostPair(SurvRes.Nails, 4) }; }
                    else { coinCost = 1450; reqPopCur = 20; matCosts = new[] { new CostPair(SurvRes.Blueprint, 4), new CostPair(SurvRes.MetalPipe, 4) }; }
                    break;

                case NodeKind.Farm:
                    if (nextLv == 2) { coinCost = 180; reqPopCur = 6; matCosts = new[] { new CostPair(SurvRes.Plank, 8), new CostPair(SurvRes.Water, 4) }; }
                    else if (nextLv == 3) { coinCost = 980; reqPopCur = 18; matCosts = new[] { new CostPair(SurvRes.Blueprint, 3), new CostPair(SurvRes.MetalPipe, 2), new CostPair(SurvRes.Plank, 10) }; }
                    else { coinCost = 1850; reqPopCur = 26; matCosts = new[] { new CostPair(SurvRes.Blueprint, 5), new CostPair(SurvRes.MetalPipe, 4), new CostPair(SurvRes.IronBeam, 4) }; }
                    break;

                case NodeKind.Ranch:
                    if (nextLv == 2) { coinCost = 380; reqPopCur = 10; matCosts = new[] { new CostPair(SurvRes.Plank, 10), new CostPair(SurvRes.Nails, 4) }; }
                    else { coinCost = 1380; reqPopCur = 22; matCosts = new[] { new CostPair(SurvRes.Blueprint, 4), new CostPair(SurvRes.IronBeam, 4), new CostPair(SurvRes.MetalPipe, 2) }; }
                    break;

                case NodeKind.RiverDock:
                    coinCost = 460; reqPopCur = 12;
                    matCosts = new[] { new CostPair(SurvRes.Blueprint, 2), new CostPair(SurvRes.Plank, 10), new CostPair(SurvRes.Nails, 4) };
                    break;

                case NodeKind.Barricade:
                    coinCost = 680; reqPopCur = 14;
                    matCosts = new[] { new CostPair(SurvRes.Blueprint, 2), new CostPair(SurvRes.IronBeam, 3), new CostPair(SurvRes.Nails, 4) };
                    break;

                case NodeKind.Watchtower:
                    coinCost = 880; reqPopCur = 16;
                    matCosts = new[] { new CostPair(SurvRes.Blueprint, 3), new CostPair(SurvRes.IronBeam, 3), new CostPair(SurvRes.IronIngot, 2) };
                    break;

                case NodeKind.RelicGenerator:
                    if (nextLv == 2) { coinCost = 1650; reqPopCur = 22; matCosts = new[] { new CostPair(SurvRes.Blueprint, 5), new CostPair(SurvRes.MetalPipe, 3), new CostPair(SurvRes.Parts, 3) }; }
                    else { coinCost = 2800; reqPopCur = 32; matCosts = new[] { new CostPair(SurvRes.Blueprint, 7), new CostPair(SurvRes.MetalPipe, 5), new CostPair(SurvRes.Parts, 5) }; }
                    break;

                case NodeKind.RadioPost:
                    coinCost = 1750; reqPopCur = 24;
                    matCosts = new[] { new CostPair(SurvRes.Blueprint, 5), new CostPair(SurvRes.IronBeam, 4), new CostPair(SurvRes.Parts, 4) };
                    break;

                case NodeKind.RelicTruck:
                    coinCost = 1850; reqPopCur = 24;
                    matCosts = new[] { new CostPair(SurvRes.Blueprint, 5), new CostPair(SurvRes.IronIngot, 5), new CostPair(SurvRes.MetalPipe, 3) };
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
            if (n == null || n.state == NodeState.HiddenInForest) return false;
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
                case SurvRes.Veggie: targetNodeId = "farm_a"; return "在【生态选种农田】播下鲜蔬种收获，或林边浆果丛采摘";
                case SurvRes.Cloth: targetNodeId = "farm_a"; return "在【生态选种农田】播下亚麻种收获，或赴【生鲜超市】搜刮";
                case SurvRes.Herb: targetNodeId = "farm_a"; return "在【生态选种农田】播下草药种稳定收获，或砍林伴生采摘";
                case SurvRes.Fish: targetNodeId = "river_dock"; return "前往南岸【河畔钓鱼台】甩杆垂钓与鱼篓收获";
                case SurvRes.Meat:
                case SurvRes.Pelt: targetNodeId = "beast_ranch"; return "在【山林畜牧栏】投喂鲜蔬与井水稳定饲养双掉，或【捕猎哨塔】射猎";
                case SurvRes.Compost: targetNodeId = "beast_ranch"; return "在【山林畜牧栏】饲养雪兔/山猪/绒鹿收获时稳定副产";
                case SurvRes.SeedVeggie:
                case SurvRes.SeedFlax:
                case SurvRes.SeedHerb: targetNodeId = "farm_a"; return "砍树/采果必掉，且【生态选种农田】每次收获保底返种自循环";
                case SurvRes.SeedGolden: targetNodeId = "relic_truck"; return "农田施肥变异、迎击尸潮波次宝箱或赴丧尸城搜刮获得";
                case SurvRes.Parts: targetNodeId = "relic_truck"; return "赴丧尸城【急诊药房/汽修街】搜刮或拆解密林物资箱";
                case SurvRes.Blueprint: targetNodeId = "relic_truck"; return "赴【丧尸城搜刮】、【迎击尸潮】、【山口行商】或【提升星级】获得";
                case SurvRes.HeroMedal: targetNodeId = "barricade"; return "点击底部【迎击】击退山口尸潮，或赴丧尸城击杀守箱尸王获得";
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
        // 建筑三阶段林地探索揭露（砍树扩地 -> 发现小物件收集整合 -> 显露地基建造）
        // =====================================================================
        public void SyncRevealedNodes(ForestMap map, bool silent = false)
        {
            if (map == null) return;
            bool anyChanged = false;

            for (int i = 0; i < Nodes.Count; i++)
            {
                CampNode n = Nodes[i];
                if (n.state != NodeState.HiddenInForest) continue;

                int x0 = n.revealChunkX * ForestMap.ChunkSize;
                int y0 = n.revealChunkY * ForestMap.ChunkSize;
                int clearedCells = 0;
                for (int dx = 0; dx < ForestMap.ChunkSize; dx++)
                {
                    for (int dy = 0; dy < ForestMap.ChunkSize; dy++)
                    {
                        if (map.IsCleared(x0 + dx, y0 + dy))
                            clearedCells++;
                    }
                }

                bool chunkCleared = clearedCells >= 6;
                bool spotCleared = map.IsCleared(Mathf.FloorToInt(n.gx), Mathf.FloorToInt(n.gy));
                if (!chunkCleared || !spotCleared) continue;

                if (!n.siteUncovered)
                {
                    n.siteUncovered = true;
                    anyChanged = true;

                    if (n.requiresSiteSalvage && !n.siteSalvageCleared && !silent)
                    {
                        n.state = NodeState.HiddenInForest;
                        OnNodeForestUncovered?.Invoke(n);
                        OnWorldToast?.Invoke(
                            string.Format("领地扩展！在{0}空地发现【{1}】！", n.compassDir, n.salvageLabel),
                            new Vector2(n.gx, n.gy));
                    }
                    else
                    {
                        n.siteSalvageCleared = true;
                        n.state = NodeState.Discovered;
                        OnNodeVisualChanged?.Invoke(n);
                    }
                }
                else if (silent && !n.siteSalvageCleared)
                {
                    n.siteSalvageCleared = true;
                    n.state = NodeState.Discovered;
                    anyChanged = true;
                    OnNodeVisualChanged?.Invoke(n);
                }
            }

            if (anyChanged)
                EvaluateQuestProgress();
        }

        public bool CompleteNodeSiteSalvage(string nodeId)
        {
            CampNode n = FindNode(nodeId);
            if (n == null || n.siteSalvageCleared) return false;

            n.siteUncovered = true;
            n.siteSalvageCleared = true;

            AddRes(n.salvageBonusRes, Mathf.Max(1, n.salvageBonusAmt));
            Coins += 20;
            AddXp(15, new Vector2(n.gx, n.gy));

            if (n.state == NodeState.HiddenInForest)
            {
                n.state = NodeState.Discovered;
                OnNodeVisualChanged?.Invoke(n);
                OnWorldToast?.Invoke(
                    string.Format("收集整合【{0}】完毕！显露【{1}】建造点！", n.salvageLabel, n.activeTitle),
                    new Vector2(n.gx, n.gy));
                TriggerDiscoveryDialog(n);
            }

            EvaluateQuestProgress();
            return true;
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
            if (n == null)
            {
                msg = "无效建筑节点";
                return false;
            }

            if (n.state == NodeState.HiddenInForest)
            {
                msg = !n.siteUncovered
                    ? $"请先向{n.compassDir}砍伐林木扩展领地，寻找【{n.activeTitle}】！"
                    : $"请先收集整合新空地发现的【{n.salvageLabel}】！";
                return false;
            }

            if (n.kind == NodeKind.RelicLoot && n.state == NodeState.Discovered)
            {
                n.state = NodeState.Depleted;
                n.activeTitle = "已拆解";
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
                    n.activeTitle = n.level >= 5 ? "战略万吨总仓" : (n.level == 4 ? "重型钢梁总仓" : (n.level == 3 ? "包铁加固总仓" : "木架物资仓库"));
                else if (n.kind == NodeKind.HeatTower)
                    n.activeTitle = n.level >= 4 ? "聚能温室供暖塔" : (n.level == 3 ? "高压锅炉供暖塔" : "石砌篝火供暖塔");
                else if (n.kind == NodeKind.RelicWell)
                {
                    n.prodOutCount = 3 + n.level * 2;
                    n.maxStored = 10 + n.level * 8;
                }
                else if (n.kind == NodeKind.RiverDock)
                {
                    n.prodOutCount = 4;
                    n.maxStored = 20;
                    n.activeTitle = "深水渔栈码头";
                }
                else if (n.kind == NodeKind.Watchtower)
                {
                    n.prodOutCount = 4;
                    n.prodOutCount2 = 3;
                    n.maxStored = 24;
                    n.activeTitle = "包铁狙击哨塔";
                }
                else if (n.kind == NodeKind.Barricade)
                    n.activeTitle = "铁蒺藜重装防线";
                else if (n.kind == NodeKind.RelicGenerator)
                    n.activeTitle = n.level >= 3 ? "高压并网总电站" : "双联水轮电站";
                else if (n.kind == NodeKind.RadioPost)
                    n.activeTitle = "远距行商电台塔";
                else if (n.kind == NodeKind.RelicTruck)
                    n.activeTitle = "重甲冲锋皮卡";
            }

            n.isUpgradingExisting = false;
            AddXp(25 + n.level * 10, new Vector2(n.gx, n.gy));
            OnNodeVisualChanged?.Invoke(n);
            OnNodeHammeredComplete?.Invoke(n);
            SyncUnlockedSkinsToHeroes(n);
            EnsureActiveOrders();
            RefreshCaravanOffers();
            EvaluateQuestProgress();
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
                string resolvedBust = PuppetRegistry.ResolveBustKey(Heroes[i].characterId, unlockedTier);
                if (Heroes[i].id == "leimeng")
                    resolvedBust = "bust_vil_0";
                else if (Heroes[i].id == "tieshu")
                    resolvedBust = "survivor_tieshu";
                Heroes[i].portraitKey = resolvedBust;
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
            if (r.outRes == SurvRes.Nails) NailsCraftedTotal += r.outAmt;
            if (r.outRes == SurvRes.LinenShirt || r.outRes == SurvRes.LeatherBoots || r.outRes == SurvRes.FurCoat || r.outRes == SurvRes.PlateArmor)
                ClothesCraftedTotal += r.outAmt;
            if (r.outRes == SurvRes.HerbPoultice || r.outRes == SurvRes.WarmTea || r.outRes == SurvRes.MedKit)
                MedsCraftedTotal += r.outAmt;
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
            AddXp(10 + r.unlockBuildingLv * 4, new Vector2(workshop.gx, workshop.gy));
            EvaluateQuestProgress();
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
                if (HarvestAllRipeFarmPlots(out msg))
                    return true;
                msg = "打开农田面板选种管理";
                return false;
            }

            if (n.kind == NodeKind.Ranch)
            {
                if (CollectAllReadyRanchPens(out msg))
                    return true;
                msg = "打开畜牧栏投喂管理";
                return false;
            }

            if (n.storedAmount > 0)
            {
                int amt = n.storedAmount;
                SurvRes r = n.storedRes;
                n.storedAmount = 0;
                Add(r, amt, true);
                if (r == SurvRes.Fish) FishCaughtTotal += amt;
                if (n.prodOutCount2 > 0)
                    Add(n.prodOutRes2, Mathf.Max(1, amt / 2), true);
                msg = $"收取 {ResName(r)} +{amt}";
                EvaluateQuestProgress();
                return true;
            }

            return false;
        }

        // =====================================================================
        // 农牧自循环核心系统：4垄选种农田 + 4栏山林畜牧栏 + 堆肥反哺闭环
        // =====================================================================
        void InitFarmAndRanchSlots()
        {
            FarmPlots.Clear();
            CropSeedKind[] defaults = { CropSeedKind.Veggie, CropSeedKind.Flax, CropSeedKind.Herb, CropSeedKind.Veggie };
            for (int i = 0; i < 4; i++)
            {
                FarmPlots.Add(new FarmPlotSlot
                {
                    index = i,
                    phase = FarmPhase.DryTilled,
                    seedKind = defaults[i],
                    lastSeedKind = defaults[i],
                });
            }

            RanchPens.Clear();
            RanchAnimalKind[] animals = { RanchAnimalKind.SnowRabbit, RanchAnimalKind.SnowRabbit, RanchAnimalKind.WildBoar, RanchAnimalKind.AntlerDeer };
            for (int i = 0; i < 4; i++)
            {
                RanchPens.Add(new RanchPenSlot
                {
                    index = i,
                    animal = animals[i],
                    feeding = false,
                    readyToCollect = false,
                });
            }
        }

        public int GetFarmPlotCap()
        {
            int lv = GetNodeLevel("farm_a", 0);
            if (lv <= 0) return 0;
            if (lv == 1) return 2;
            if (lv == 2) return 3;
            return 4;
        }

        public int GetRanchPenCap()
        {
            int lv = GetNodeLevel("beast_ranch", 0);
            if (lv <= 0) return 0;
            if (lv == 1) return 2;
            if (lv == 2) return 3;
            return 4;
        }

        public int CountRipeFarmPlots()
        {
            int cap = GetFarmPlotCap();
            int c = 0;
            for (int i = 0; i < cap && i < FarmPlots.Count; i++)
                if (FarmPlots[i].phase == FarmPhase.Ripe) c++;
            return c;
        }

        public int CountEmptyFarmPlots()
        {
            int cap = GetFarmPlotCap();
            int c = 0;
            for (int i = 0; i < cap && i < FarmPlots.Count; i++)
                if (FarmPlots[i].phase == FarmPhase.DryTilled) c++;
            return c;
        }

        public int CountReadyRanchPens()
        {
            int cap = GetRanchPenCap();
            int c = 0;
            for (int i = 0; i < cap && i < RanchPens.Count; i++)
                if (RanchPens[i].readyToCollect) c++;
            return c;
        }

        public int CountIdleRanchPens()
        {
            int cap = GetRanchPenCap();
            int c = 0;
            for (int i = 0; i < cap && i < RanchPens.Count; i++)
                if (!RanchPens[i].feeding && !RanchPens[i].readyToCollect) c++;
            return c;
        }

        public static SurvRes SeedResFor(CropSeedKind k)
        {
            switch (k)
            {
                case CropSeedKind.Flax: return SurvRes.SeedFlax;
                case CropSeedKind.Herb: return SurvRes.SeedHerb;
                case CropSeedKind.Golden: return SurvRes.SeedGolden;
                default: return SurvRes.SeedVeggie;
            }
        }

        public static SurvRes MainCropResFor(CropSeedKind k)
        {
            switch (k)
            {
                case CropSeedKind.Flax: return SurvRes.Cloth;
                case CropSeedKind.Herb: return SurvRes.Herb;
                case CropSeedKind.Golden: return SurvRes.SeedGolden;
                default: return SurvRes.Veggie;
            }
        }

        public static string CropTitleFor(CropSeedKind k)
        {
            switch (k)
            {
                case CropSeedKind.Flax: return "韧性亚麻 (主产粗麻布)";
                case CropSeedKind.Herb: return "止血山草药 (主产草药)";
                case CropSeedKind.Golden: return "变异黄金果 (三料大丰收)";
                default: return "高产山薯南瓜 (主产鲜蔬)";
            }
        }

        public static float BaseCropDuration(CropSeedKind k)
        {
            switch (k)
            {
                case CropSeedKind.Flax: return 8.5f;
                case CropSeedKind.Herb: return 9.5f;
                case CropSeedKind.Golden: return 10.5f;
                default: return 7.0f;
            }
        }

        public string GetCropYieldPreview(CropSeedKind k, bool fertilized)
        {
            int lv = Mathf.Max(1, GetNodeLevel("farm_a", 1));
            float mul = (1f + (lv - 1) * 0.22f) * (fertilized ? 1.5f : 1.0f);
            switch (k)
            {
                case CropSeedKind.Flax:
                    return $"粗麻布+{Mathf.RoundToInt(4 * mul)} · 鲜蔬+{Mathf.RoundToInt(2 * mul)} · 保底返种";
                case CropSeedKind.Herb:
                    return $"山草药+{Mathf.RoundToInt(4 * mul)} · 鲜蔬+{Mathf.Max(1, Mathf.RoundToInt(1.5f * mul))} · 保底返种";
                case CropSeedKind.Golden:
                    return $"鲜蔬+{Mathf.RoundToInt(7 * mul)} · 草药+{Mathf.RoundToInt(5 * mul)} · 粗麻+{Mathf.RoundToInt(4 * mul)} · 铜币+80";
                default:
                    return $"山果鲜蔬+{Mathf.RoundToInt(5 * mul)} · 保底返还鲜蔬种";
            }
        }

        public void SyncFarmVisualPhase()
        {
            CampNode fn = Find("farm_a");
            if (fn == null) return;
            int cap = GetFarmPlotCap();
            bool anyRipe = false, anyGrow = false;
            for (int i = 0; i < cap && i < FarmPlots.Count; i++)
            {
                if (FarmPlots[i].phase == FarmPhase.Ripe) anyRipe = true;
                else if (FarmPlots[i].phase == FarmPhase.Growing) anyGrow = true;
            }
            FarmPhase next = anyRipe ? FarmPhase.Ripe : (anyGrow ? FarmPhase.Growing : FarmPhase.DryTilled);
            if (fn.farmPhase != next)
            {
                fn.farmPhase = next;
                OnNodeVisualChanged?.Invoke(fn);
            }
        }

        public bool TryPlantFarmPlot(int plotIdx, CropSeedKind seed, bool allowCoinEmergencyBuy, out string msg)
        {
            msg = null;
            int cap = GetFarmPlotCap();
            if (plotIdx < 0 || plotIdx >= cap || plotIdx >= FarmPlots.Count)
            {
                msg = "升级【生态选种农田】可扩建更多田垄！";
                return false;
            }
            FarmPlotSlot sl = FarmPlots[plotIdx];
            if (sl.phase != FarmPhase.DryTilled)
            {
                msg = sl.phase == FarmPhase.Ripe ? "该田垄已成熟，请先收割" : "该田垄正在生长中";
                return false;
            }

            SurvRes sRes = SeedResFor(seed);
            if (Get(sRes) <= 0)
            {
                if (allowCoinEmergencyBuy && seed != CropSeedKind.Golden && Coins >= 25)
                {
                    Coins -= 25;
                    Add(sRes, 1, notifyToast: false);
                }
                else
                {
                    msg = seed == CropSeedKind.Golden
                        ? "暂无【变异黄金种】（施肥变异、迎击尸潮或赴丧尸城搜刮可获）"
                        : $"{ResName(sRes)}不足（砍树采果必掉，或满25铜币应急补种）";
                    return false;
                }
            }

            Add(sRes, -1);
            sl.seedKind = seed;
            sl.lastSeedKind = seed;
            sl.phase = FarmPhase.Growing;
            sl.timer = 0f;
            sl.duration = BaseCropDuration(seed);
            sl.fertilized = false;
            sl.watered = HasAutoIrrigation;
            if (!sl.watered && Get(SurvRes.Water) >= 1)
            {
                Add(SurvRes.Water, -1);
                sl.watered = true;
            }
            SyncFarmVisualPhase();
            msg = $"第 {plotIdx + 1} 垄已播下【{ResName(sRes)}】" + (sl.watered ? "（山泉润泽提速中）" : "（露水慢生，修水井可提速）");
            return true;
        }

        public bool TryWaterFarmPlot(int plotIdx, out string msg)
        {
            msg = null;
            if (plotIdx < 0 || plotIdx >= GetFarmPlotCap()) return false;
            FarmPlotSlot sl = FarmPlots[plotIdx];
            if (sl.phase != FarmPhase.Growing || sl.watered)
            {
                msg = sl.watered ? "该田垄已处于湿润加速状态" : "请先播种后再浇灌";
                return false;
            }
            if (Get(SurvRes.Water) < 1)
            {
                msg = "净水不足！请前往东南【净水石井】汲水";
                return false;
            }
            Add(SurvRes.Water, -1);
            sl.watered = true;
            msg = $"第 {plotIdx + 1} 垄引山泉灌溉成功，生长提速 +40%！";
            return true;
        }

        public bool TryFertilizeFarmPlot(int plotIdx, out string msg)
        {
            msg = null;
            if (plotIdx < 0 || plotIdx >= GetFarmPlotCap()) return false;
            FarmPlotSlot sl = FarmPlots[plotIdx];
            if (sl.phase != FarmPhase.Growing)
            {
                msg = "仅对生长中的田垄可撒施【有机堆肥】";
                return false;
            }
            if (sl.fertilized)
            {
                msg = "本垄已施过肥，正在丰产孕育中";
                return false;
            }
            if (Get(SurvRes.Compost) < 1)
            {
                msg = "缺少【有机堆肥】！前往西侧【山林畜牧栏】饲养动物即可稳定产出";
                return false;
            }
            Add(SurvRes.Compost, -1);
            CompostUsedTotal++;
            sl.fertilized = true;
            sl.timer += sl.duration * 0.55f;
            if (sl.timer >= sl.duration)
            {
                sl.phase = FarmPhase.Ripe;
                SyncFarmVisualPhase();
            }
            EvaluateQuestProgress();
            msg = $"施入畜牧【有机堆肥】！立即催熟 +55% 且本垄收成翻 1.5 倍！";
            return true;
        }

        public bool TryHarvestFarmPlot(int plotIdx, out string msg, bool silentToast = false)
        {
            msg = null;
            int cap = GetFarmPlotCap();
            if (plotIdx < 0 || plotIdx >= cap || plotIdx >= FarmPlots.Count) return false;
            FarmPlotSlot sl = FarmPlots[plotIdx];
            if (sl.phase != FarmPhase.Ripe) return false;

            CampNode fn = Find("farm_a");
            int lv = Mathf.Max(1, fn != null ? fn.level : 1);
            float mul = (1f + (lv - 1) * 0.22f) * (sl.fertilized ? 1.5f : 1.0f);
            Vector2 pos = fn != null ? new Vector2(fn.gx, fn.gy) : new Vector2(13.6f, 13.6f);

            switch (sl.seedKind)
            {
                case CropSeedKind.Flax:
                    Add(SurvRes.Cloth, Mathf.RoundToInt(4 * mul), !silentToast);
                    Add(SurvRes.Veggie, Mathf.RoundToInt(2 * mul), false);
                    break;
                case CropSeedKind.Herb:
                    Add(SurvRes.Herb, Mathf.RoundToInt(4 * mul), !silentToast);
                    Add(SurvRes.Veggie, Mathf.Max(1, Mathf.RoundToInt(1.5f * mul)), false);
                    break;
                case CropSeedKind.Golden:
                    Add(SurvRes.Veggie, Mathf.RoundToInt(7 * mul), !silentToast);
                    Add(SurvRes.Herb, Mathf.RoundToInt(5 * mul), !silentToast);
                    Add(SurvRes.Cloth, Mathf.RoundToInt(4 * mul), false);
                    Coins += 80;
                    break;
                default:
                    Add(SurvRes.Veggie, Mathf.RoundToInt(5 * mul), !silentToast);
                    break;
            }

            // 留种自循环：普通作物 100% 保底返 1 颗同款种，施肥/农牧委任高概率返 2 颗并有概率变异出黄金良种！
            int returnedSeeds = 0;
            bool gotMutant = false;
            if (sl.seedKind != CropSeedKind.Golden)
            {
                SurvRes seedRes = SeedResFor(sl.seedKind);
                float bonusChance = 0.30f + GetPostScoreSum(DelegationPostKind.FarmYield) * 0.008f + (sl.fertilized ? 0.55f : 0f);
                returnedSeeds = _ecoRng.NextDouble() < bonusChance ? 2 : 1;
                Add(seedRes, returnedSeeds, false);
                if (_ecoRng.NextDouble() < (sl.fertilized ? 0.28 : 0.10))
                {
                    Add(SurvRes.SeedGolden, 1, !silentToast);
                    gotMutant = true;
                }
            }

            FarmHarvestedTotal++;
            AddXp(sl.seedKind == CropSeedKind.Golden ? 26 : 14, pos);
            sl.phase = FarmPhase.DryTilled;
            sl.timer = 0f;
            sl.watered = false;
            sl.fertilized = false;

            // 勾选自动续种且种子充足时，收割瞬间无缝连种！
            if (AutoReplantFarm && Get(SeedResFor(sl.lastSeedKind)) > 0)
            {
                TryPlantFarmPlot(plotIdx, sl.lastSeedKind, allowCoinEmergencyBuy: false, out _);
            }
            else
            {
                SyncFarmVisualPhase();
            }

            msg = gotMutant
                ? $"丰收暴击！额外变异出【变异黄金种×1】！"
                : $"田垄丰收！返还种子×{Mathf.Max(1, returnedSeeds)}";
            return true;
        }

        public bool HarvestAllRipeFarmPlots(out string msg)
        {
            msg = null;
            int cap = GetFarmPlotCap();
            int count = 0;
            string lastMsg = null;
            for (int i = 0; i < cap && i < FarmPlots.Count; i++)
            {
                if (FarmPlots[i].phase == FarmPhase.Ripe && TryHarvestFarmPlot(i, out string m))
                {
                    count++;
                    lastMsg = m;
                }
            }
            if (count > 0)
            {
                CampNode fn = Find("farm_a");
                msg = count > 1 ? $"一键收割 {count} 垄良田大丰收！" : lastMsg;
                if (fn != null) OnWorldToast?.Invoke(msg, new Vector2(fn.gx, fn.gy));
                return true;
            }
            return false;
        }

        // ---------------- 山林畜牧栏：饲养雪兔/山猪/绒鹿 -> 鲜肉+兽毛皮+有机堆肥 ----------------
        public static string RanchAnimalName(RanchAnimalKind k)
        {
            switch (k)
            {
                case RanchAnimalKind.WildBoar: return "黑鬃山猪";
                case RanchAnimalKind.AntlerDeer: return "梅花绒鹿";
                default: return "长毛雪兔";
            }
        }

        public static int RanchAnimalUnlockLv(RanchAnimalKind k)
        {
            switch (k)
            {
                case RanchAnimalKind.WildBoar: return 2;
                case RanchAnimalKind.AntlerDeer: return 3;
                default: return 1;
            }
        }

        public void GetRanchYield(RanchAnimalKind k, out int meat, out int pelt, out int compost)
        {
            int wtBonus = HasWatchtower ? 1 : 0; // 捕猎木哨塔野兽良种联动加成！
            switch (k)
            {
                case RanchAnimalKind.WildBoar:
                    meat = 4 + wtBonus; pelt = 1 + wtBonus; compost = 2;
                    break;
                case RanchAnimalKind.AntlerDeer:
                    meat = 3 + wtBonus; pelt = 4 + wtBonus; compost = 2;
                    break;
                default:
                    meat = 2 + wtBonus; pelt = 2 + wtBonus; compost = 1;
                    break;
            }
        }

        public bool TryFeedRanchPen(int penIdx, RanchAnimalKind animal, out string msg)
        {
            msg = null;
            int cap = GetRanchPenCap();
            if (penIdx < 0 || penIdx >= cap || penIdx >= RanchPens.Count)
            {
                msg = "升级【山林畜牧栏】可扩充更多饲养栏位！";
                return false;
            }
            int rLv = GetNodeLevel("beast_ranch", 1);
            if (rLv < RanchAnimalUnlockLv(animal))
            {
                msg = $"需将【山林畜牧栏】升至 Lv.{RanchAnimalUnlockLv(animal)} 解锁饲养{RanchAnimalName(animal)}";
                return false;
            }
            RanchPenSlot pen = RanchPens[penIdx];
            if (pen.readyToCollect)
            {
                msg = "该栏动物已养成，请先收取肉皮与堆肥";
                return false;
            }
            if (pen.feeding)
            {
                msg = "正在咀嚼饲草育肥中…";
                return false;
            }
            if (Get(SurvRes.Veggie) < 2)
            {
                msg = "饲草不足：需农田产出的【山果鲜蔬×2】！";
                return false;
            }
            if (Get(SurvRes.Water) < 1)
            {
                msg = "饮水不足：需净水石井产出的【山泉净水×1】！";
                return false;
            }

            Add(SurvRes.Veggie, -2);
            Add(SurvRes.Water, -1);
            pen.animal = animal;
            pen.feeding = true;
            pen.readyToCollect = false;
            pen.timer = 0f;
            pen.duration = animal == RanchAnimalKind.AntlerDeer ? 11.0f : (animal == RanchAnimalKind.WildBoar ? 9.5f : 8.0f);
            msg = $"已向第 {penIdx + 1} 栏【{RanchAnimalName(animal)}】投喂鲜蔬与山泉！";
            return true;
        }

        public bool TryCollectRanchPen(int penIdx, out string msg, bool silentToast = false)
        {
            msg = null;
            int cap = GetRanchPenCap();
            if (penIdx < 0 || penIdx >= cap || penIdx >= RanchPens.Count) return false;
            RanchPenSlot pen = RanchPens[penIdx];
            if (!pen.readyToCollect) return false;

            GetRanchYield(pen.animal, out int meat, out int pelt, out int compost);
            Add(SurvRes.Meat, meat, !silentToast);
            Add(SurvRes.Pelt, pelt, !silentToast);
            Add(SurvRes.Compost, compost, !silentToast);

            RanchCollectedTotal++;
            CampNode rn = Find("beast_ranch");
            Vector2 pos = rn != null ? new Vector2(rn.gx, rn.gy) : new Vector2(10.5f, 14.2f);
            AddXp(18, pos);

            pen.feeding = false;
            pen.readyToCollect = false;
            pen.timer = 0f;

            if (AutoFeedRanch && Get(SurvRes.Veggie) >= 4 && Get(SurvRes.Water) >= 2)
                TryFeedRanchPen(penIdx, pen.animal, out _);

            msg = $"畜牧收获：鲜肉+{meat} · 兽毛皮+{pelt} · 有机堆肥+{compost}！";
            return true;
        }

        public bool CollectAllReadyRanchPens(out string msg)
        {
            msg = null;
            int cap = GetRanchPenCap();
            int cnt = 0;
            string last = null;
            for (int i = 0; i < cap && i < RanchPens.Count; i++)
            {
                if (RanchPens[i].readyToCollect && TryCollectRanchPen(i, out string m))
                {
                    cnt++;
                    last = m;
                }
            }
            if (cnt > 0)
            {
                CampNode rn = Find("beast_ranch");
                msg = cnt > 1 ? $"畜牧栏大丰收！共收 {cnt} 栏鲜肉毛皮与有机堆肥！" : last;
                if (rn != null) OnWorldToast?.Invoke(msg, new Vector2(rn.gx, rn.gy));
                return true;
            }
            return false;
        }

        public void AwardRandomWildSeed(int count = 1, bool notify = true)
        {
            SurvRes[] pool = { SurvRes.SeedVeggie, SurvRes.SeedFlax, SurvRes.SeedHerb };
            for (int i = 0; i < count; i++)
            {
                SurvRes pick = pool[_ecoRng.Next(pool.Length)];
                Add(pick, 1, notify && i == 0);
            }
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
            if (CampLevel >= 7 && slot == 2) cat = OrderCategory.Medical;
            else if (CampLevel >= 3 && slot == 1) cat = OrderCategory.Clothing;
            else if (CampLevel >= 5 && (_demandSeq % 4 == 0)) cat = OrderCategory.Housing;

            string[] speakers = { "樵夫·老周", "采药·桂婶", "木工学徒·阿顺", "拾荒少女·小苗" };
            string[] busts = { "bust_vil_0", "bust_vil_1", "bust_vil_2", "bust_vil_3" };
            int sIdx = (_demandSeq + slot) % speakers.Length;
            int wIdx = sIdx;

            if (cat == OrderCategory.Clothing)
            {
                bool ultra = CampLevel >= 14 && (_demandSeq % 4 == 0);
                bool high = !ultra && CampLevel >= 9 && (_demandSeq % 3 == 0);
                bool mid = !ultra && !high && CampLevel >= 6 && (_demandSeq % 2 == 0);
                SurvRes clothItem = ultra ? SurvRes.PlateArmor : (high ? SurvRes.FurCoat : (mid ? SurvRes.LeatherBoots : SurvRes.LinenShirt));
                return new ResidentDemand
                {
                    slotIndex = slot, category = cat, walkerIndex = wIdx,
                    speakerName = speakers[sIdx], portraitKey = busts[sIdx],
                    categoryTag = "御寒穿戴单",
                    complaint = clothItem == SurvRes.PlateArmor
                        ? "巡山小队深入高危林线，求一套废土钢板护甲防丧尸扑咬！"
                        : (clothItem == SurvRes.FurCoat
                            ? "半夜山风往领口里灌，快去裁缝坊缝件厚实的加绒皮衣救命啊！"
                            : (clothItem == SurvRes.LeatherBoots ? "进林子捡柴全是荆棘刺，没双猎皮靴脚底板都扎烂了！" : "逃难穿来的卫衣全刮成布条了，求件耐磨麻布衣遮风！")),
                    thanksLine = "新衣甲太合身暖和了！干活浑身是劲！",
                    res1 = clothItem, amt1 = 1,
                    res2 = SurvRes.Water, amt2 = CampLevel >= 2 ? 1 : 0,
                    coinReward = ultra ? 680 : (high ? 420 : (mid ? 250 : 150)),
                    xpReward = ultra ? 115 : (high ? 80 : (mid ? 52 : 36)),
                };
            }

            if (cat == OrderCategory.Medical)
            {
                bool kit = CampLevel >= 13 && (_demandSeq % 3 == 0);
                bool tea = !kit && CampLevel >= 10 && (_demandSeq % 2 == 1);
                SurvRes medItem = kit ? SurvRes.MedKit : (tea ? SurvRes.WarmTea : SurvRes.HerbPoultice);
                return new ResidentDemand
                {
                    slotIndex = slot, category = cat, walkerIndex = wIdx,
                    speakerName = speakers[sIdx], portraitKey = busts[sIdx],
                    categoryTag = "伤病防疫单 · 高经验",
                    complaint = kit
                        ? "山道警戒哨被游尸抓伤，急需草药坊配制的抗尸毒急救箱！"
                        : (tea
                            ? "淋着秋雨巡了一宿山道，直打喷嚏，熬碗驱寒草本热饮发发汗吧！"
                            : "搬废铁不小心蹭破了胳膊，急需止血草药敷包消炎包扎！"),
                    thanksLine = "医药及时送达，救命之恩记心上了！",
                    res1 = medItem, amt1 = 1,
                    res2 = SurvRes.RoastVeggie, amt2 = 1,
                    coinReward = kit ? 540 : (tea ? 340 : 220),
                    xpReward = kit ? 135 : (tea ? 100 : 72), // 伤病单经验倍率最高！
                };
            }

            if (cat == OrderCategory.Housing)
            {
                return new ResidentDemand
                {
                    slotIndex = slot, category = cat, walkerIndex = wIdx,
                    speakerName = speakers[sIdx], portraitKey = busts[sIdx],
                    categoryTag = "营房安居单 · 赠手稿",
                    complaint = "拖家带口刚投奔山谷，地面返潮太冷，能给配张防潮木床铺和铆钉修屋顶吗？",
                    thanksLine = "终于睡上踏实木床了！这卷旧城搜来的工程手稿送给营地！",
                    res1 = SurvRes.WoodBed, amt1 = 1,
                    res2 = SurvRes.Nails, amt2 = 2,
                    coinReward = 290,
                    xpReward = 58,
                    blueprintBonus = 1,
                };
            }

            // 默认：温饱餐饮单（严格匹配当前星级可做菜品）
            SurvRes dish = SurvRes.RoastVeggie;
            int coin = 95, xp = 28;
            if (CampLevel >= 8 && (_demandSeq % 3 == 0)) { dish = SurvRes.SmokedMeat; coin = 350; xp = 68; }
            else if (CampLevel >= 4 && (_demandSeq % 2 == 0)) { dish = SurvRes.GrillFish; coin = 200; xp = 44; }
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
            float popProsperity = OrderProsperityMultiplier;
            int finalCoins = Mathf.RoundToInt(d.coinReward * chefBonus * popProsperity);
            int finalXp = Mathf.RoundToInt(d.xpReward * Mathf.Lerp(1f, popProsperity, 0.5f));
            Coins += finalCoins;
            if (d.blueprintBonus > 0)
                Add(SurvRes.Blueprint, d.blueprintBonus, notifyToast: true);

            OrdersCompletedTotal++;
            if (d.category == OrderCategory.Medical)
                MedicalOrdersCompletedTotal++;

            SatisfiedToday++;
            if (SatisfiedToday >= SatisfiedGoal)
            {
                SatisfiedToday = 0;
                int chestCoins = Mathf.RoundToInt(260 * popProsperity);
                Coins += chestCoins;
                Add(SurvRes.Blueprint, 2, notifyToast: true);
                Add(SurvRes.HeroMedal, 1, notifyToast: true);
                if (CampLevel >= 3) Add(SurvRes.Scrap, 6, notifyToast: false);
                if (CampLevel >= 6) Add(SurvRes.Parts, 1, notifyToast: true);
                OnWorldToast?.Invoke($"达成 5 单居民宝箱！铜币+{chestCoins} · 手稿+2 · 勋章+1！", survivorGrid);
            }

            thanks = d.thanksLine;
            AddXp(finalXp, survivorGrid);
            ActiveOrders[slotIndex] = GenerateOneOrder(slotIndex);
            EvaluateQuestProgress();
            return true;
        }

        public bool TrySatisfyDemand(Vector2 survivorGrid, out string thanks)
            => TrySatisfyOrder(0, survivorGrid, out thanks);

        // ---------------- 山口行商大宗贸易（对标水世界货轮港口/不差钱商人） ----------------
        public void RefreshCaravanOffers()
        {
            float cb = CaravanCoinBonusMultiplier;
            int radioBonusBp = GetNodeLevel("radio_post", 0) >= 2 ? 1 : 0;
            CaravanOffers.Clear();
            CaravanOffers.Add(new CaravanOffer
            {
                title = "邻山伐木营采购御寒装",
                giveRes = CampLevel >= 9 ? SurvRes.FurCoat : SurvRes.LinenShirt,
                giveAmt = 2,
                coinGain = Mathf.RoundToInt((CampLevel >= 9 ? 780 : 340) * cb),
                blueprintGain = 1 + radioBonusBp,
            });
            CaravanOffers.Add(new CaravanOffer
            {
                title = "巡山猎队换购急救草药",
                giveRes = CampLevel >= 13 ? SurvRes.MedKit : (CampLevel >= 10 ? SurvRes.WarmTea : SurvRes.HerbPoultice),
                giveAmt = 2,
                coinGain = Mathf.RoundToInt((CampLevel >= 13 ? 1050 : (CampLevel >= 10 ? 640 : 440)) * cb),
                blueprintGain = 2 + radioBonusBp,
            });
            CaravanOffers.Add(new CaravanOffer
            {
                title = "商队干粮与床铺整批收购",
                giveRes = CampLevel >= 8 ? SurvRes.SmokedMeat : SurvRes.WoodBed,
                giveAmt = 2,
                coinGain = Mathf.RoundToInt(580 * cb),
                blueprintGain = 1,
            });
        }

        public bool TryTradeCaravanOffer(int idx, out string msg)
        {
            if (!RadioRepaired)
            {
                msg = "请先在东北高地建造【山口行商电台】（Lv.10解锁）！";
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
            CaravanTradesTotal++;
            Coins += o.coinGain;
            if (o.blueprintGain > 0) Add(SurvRes.Blueprint, o.blueprintGain, notifyToast: true);
            AddXp(55, new Vector2(22.6f, 21.2f));
            EvaluateQuestProgress();
            msg = $"行商交割成功！铜币+{o.coinGain}" + (o.blueprintGain > 0 ? $" · 工程手稿+{o.blueprintGain}" : "");
            return true;
        }

        // =====================================================================
        // 每帧推进与自动帮工收菜
        // =====================================================================
        public void Tick(float dt)
        {
            float mul = ProdSpeedMultiplier;
            float gatherMul = PopulationGatherMultiplier;

            if (OrderRefreshCooldown > 0f)
                OrderRefreshCooldown = Mathf.Max(0f, OrderRefreshCooldown - dt);
            if (SweepCooldownSec > 0f)
                SweepCooldownSec = Mathf.Max(0f, SweepCooldownSec - dt);

            if (RadioRepaired)
            {
                CaravanRefreshTimer -= dt * CaravanCoinBonusMultiplier;
                if (CaravanRefreshTimer <= 0f)
                {
                    CaravanRefreshTimer = 90f;
                    RefreshCaravanOffers();
                }
            }

            if (HordeWarningTriggered && !HordeRepelled && HordeTimerSec > 0f)
                HordeTimerSec = Mathf.Max(0f, HordeTimerSec - dt);

            // 锯木木艺坊建成后 + 普通居民民生劳力加成：定期巡林运回再生原木保底
            CampNode sawNode = Find("sawmill");
            if (sawNode != null && sawNode.state == NodeState.Active)
            {
                _patrolWoodTimer += dt * gatherMul;
                if (_patrolWoodTimer >= 12f)
                {
                    _patrolWoodTimer = 0f;
                    if (Get(SurvRes.Wood) < StorageCap - 4)
                        Add(SurvRes.Wood, 2 + sawNode.level, notifyToast: false);
                }
            }

            // 废铁锻造坊建成后：学徒定期沿旧山道回收散落锈铁残片，确保纯种田玩家后期不卡废铁！
            CampNode smithyNode = Find("smithy");
            if (smithyNode != null && smithyNode.state == NodeState.Active)
            {
                _patrolScrapTimer += dt * gatherMul;
                if (_patrolScrapTimer >= 20f)
                {
                    _patrolScrapTimer = 0f;
                    if (Get(SurvRes.Scrap) < StorageCap / 2)
                        Add(SurvRes.Scrap, 1 + smithyNode.level, notifyToast: false);
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

                // 生态选种农田多田垄生长与自动托管（支持滴灌提速、供暖温室提速、帮工自动收种！）
                if (n.kind == NodeKind.Farm)
                {
                    int fCap = GetFarmPlotCap();
                    bool autoIrr = HasAutoIrrigation;
                    for (int p = 0; p < fCap && p < FarmPlots.Count; p++)
                    {
                        FarmPlotSlot sl = FarmPlots[p];
                        if (sl.phase == FarmPhase.DryTilled)
                        {
                            if (AutoCollectUnlocked && AutoReplantFarm && Get(SeedResFor(sl.lastSeedKind)) > 0)
                                TryPlantFarmPlot(p, sl.lastSeedKind, allowCoinEmergencyBuy: false, out _);
                        }
                        else if (sl.phase == FarmPhase.Growing)
                        {
                            if (autoIrr && !sl.watered) sl.watered = true;
                            float waterFactor = sl.watered ? 1.0f : 0.68f;
                            sl.timer += dt * FarmSpeedMultiplier * waterFactor;
                            if (sl.timer >= sl.duration)
                            {
                                sl.phase = FarmPhase.Ripe;
                                if (AutoCollectUnlocked && AutoReplantFarm)
                                    TryHarvestFarmPlot(p, out _, silentToast: true);
                                else
                                    SyncFarmVisualPhase();
                            }
                        }
                    }
                    continue;
                }

                // 山林畜牧栏多栏饲养推进与帮工托管（吃农田鲜蔬+井水 -> 产肉+皮+有机堆肥！）
                if (n.kind == NodeKind.Ranch)
                {
                    int rCap = GetRanchPenCap();
                    int rLv = Mathf.Max(1, n.level);
                    for (int p = 0; p < rCap && p < RanchPens.Count; p++)
                    {
                        RanchPenSlot pen = RanchPens[p];
                        if (!pen.feeding && !pen.readyToCollect)
                        {
                            if (AutoCollectUnlocked && AutoFeedRanch && Get(SurvRes.Veggie) >= 4 && Get(SurvRes.Water) >= 2)
                            {
                                RanchAnimalKind bestA = rLv >= 3 && p >= 2 ? RanchAnimalKind.AntlerDeer
                                    : (rLv >= 2 && p >= 1 ? RanchAnimalKind.WildBoar : RanchAnimalKind.SnowRabbit);
                                TryFeedRanchPen(p, bestA, out _);
                            }
                        }
                        else if (pen.feeding)
                        {
                            pen.timer += dt * RanchSpeedMultiplier;
                            if (pen.timer >= pen.duration)
                            {
                                pen.feeding = false;
                                pen.readyToCollect = true;
                                if (AutoCollectUnlocked && AutoFeedRanch && CountReadyRanchPens() > 1)
                                    TryCollectRanchPen(p, out _, silentToast: true);
                            }
                        }
                    }
                    continue;
                }

                // 挂机采集建筑（净水石井 / 钓鱼台鱼篓 / 捕猎哨塔）
                if (n.prodInterval <= 0f || n.storedAmount >= n.maxStored) continue;
                if (n.prodInCount > 0 && Get(n.prodInRes) < n.prodInCount + 4) continue;

                float nodeMul = n.kind == NodeKind.Watchtower
                    ? gatherMul * DefensePostBonusMultiplier
                    : gatherMul;
                n.prodTimer += dt * nodeMul;
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
        // 22 步全流程新手与主线引导任务链（Lv.1 → Lv.15 零死锁手把手教学）
        // =====================================================================
        public enum QuestActionKind
        {
            FocusWorld = 0,
            BuildOrUpgradeNode = 1,
            OpenWorkshop = 2,
            OpenFarmDrawer = 3,
            OpenRanchDrawer = 4,
            OpenResidentOrders = 5,
            OpenHeroGrowth = 6,
            OpenHeroDelegation = 7,
            OpenFishing = 8,
            OpenCaravan = 9,
            OpenCityRun = 10,
        }

        public struct QuestInfo
        {
            public int step;
            public string text;
            public int cur;
            public int max;
            public bool done;
            public Vector2 focusGrid;
            public string targetNodeId;
            public QuestActionKind actionKind;
        }

        QuestInfo WrapNodeDiscoveryQuest(QuestInfo baseQuest)
        {
            if (string.IsNullOrEmpty(baseQuest.targetNodeId))
                return baseQuest;

            CampNode n = FindNode(baseQuest.targetNodeId);
            if (n == null)
                return baseQuest;

            baseQuest.focusGrid = new Vector2(n.gx, n.gy);

            if (n.state == NodeState.HiddenInForest)
            {
                if (!n.siteUncovered)
                {
                    baseQuest.text = string.Format("向{0}砍伐林木扩展领地，寻找【{1}】", n.compassDir, n.activeTitle);
                    baseQuest.actionKind = QuestActionKind.FocusWorld;
                    baseQuest.cur = 0;
                    baseQuest.max = 1;
                    baseQuest.done = false;
                }
                else if (!n.siteSalvageCleared)
                {
                    baseQuest.text = string.Format("收集整合新空地发现的【{0}】以解锁【{1}】", n.salvageLabel, n.activeTitle);
                    baseQuest.actionKind = QuestActionKind.FocusWorld;
                    baseQuest.cur = 0;
                    baseQuest.max = 1;
                    baseQuest.done = false;
                }
            }
            return baseQuest;
        }

        public QuestInfo GetCurrentQuest()
        {
            QuestInfo q;
            switch (QuestIndex)
            {
                case 0: // 1. 砍树或捡木堆收集原木
                {
                    bool sawBuilt = GetNodeLevel("sawmill", 0) >= 1;
                    int w = sawBuilt ? 18 : Mathf.Min(Get(SurvRes.Wood), 18);
                    q = new QuestInfo
                    {
                        step = 0, text = $"点选林缘树木或木堆收集原木 ({w}/18)",
                        cur = w, max = 18, done = w >= 18,
                        focusGrid = new Vector2(15.8f, 17.2f), targetNodeId = null,
                        actionKind = QuestActionKind.FocusWorld,
                    };
                    break;
                }
                case 1: // 2. 向东砍树 -> 收集木工锯具 -> 建造锯木木艺坊
                {
                    bool ok = GetNodeLevel("sawmill", 0) >= 1;
                    q = new QuestInfo
                    {
                        step = 1, text = $"建造【锯木木艺坊】开启木板加工 ({(ok ? 1 : 0)}/1)",
                        cur = ok ? 1 : 0, max = 1, done = ok,
                        focusGrid = new Vector2(22.9f, 16.0f), targetNodeId = "sawmill",
                        actionKind = QuestActionKind.BuildOrUpgradeNode,
                    };
                    break;
                }
                case 2: // 3. 锯木坊切割厚木板
                {
                    int p = Mathf.Min(4, Mathf.Max(PlanksCraftedTotal, Get(SurvRes.Plank)));
                    q = new QuestInfo
                    {
                        step = 2, text = $"在【锯木木艺坊】切割 4 份厚木板 ({p}/4)",
                        cur = p, max = 4, done = p >= 4,
                        focusGrid = new Vector2(22.9f, 16.0f), targetNodeId = "sawmill",
                        actionKind = QuestActionKind.OpenWorkshop,
                    };
                    break;
                }
                case 3: // 4. 向西南砍树 -> 收集菜种包 -> 开垦选种农田并播种收获
                {
                    bool fBuilt = GetNodeLevel("farm_a", 0) >= 1;
                    bool fHarv = FarmHarvestedTotal >= 1 || GetNodeLevel("farm_a", 0) >= 2;
                    int cur = (fBuilt ? 1 : 0) + (fHarv ? 1 : 0);
                    q = new QuestInfo
                    {
                        step = 3,
                        text = !fBuilt
                            ? "开垦西南【生态选种农田】 (0/2)"
                            : $"在【生态农田】播下种子并收获 ({cur}/2)",
                        cur = cur, max = 2, done = fBuilt && fHarv,
                        focusGrid = new Vector2(12.6f, 13.8f), targetNodeId = "farm_a",
                        actionKind = !fBuilt ? QuestActionKind.BuildOrUpgradeNode : QuestActionKind.OpenFarmDrawer,
                    };
                    break;
                }
                case 4: // 5. 向西侧砍树 -> 收集行军铁锅 -> 建造野味炊事房并烹制口粮
                {
                    bool kBuilt = GetNodeLevel("kitchen", 0) >= 1;
                    bool cooked = RationsCookedTotal >= 2 || Get(SurvRes.RoastVeggie) >= 1 || Get(SurvRes.VeggieSoup) >= 1;
                    int cur = (kBuilt ? 1 : 0) + (cooked ? 1 : 0);
                    q = new QuestInfo
                    {
                        step = 4,
                        text = !kBuilt
                            ? "建造西侧【野味炊事房】支起灶锅 (0/2)"
                            : $"在【野味炊事房】烹制烤果串补口粮 ({cur}/2)",
                        cur = cur, max = 2, done = kBuilt && cooked,
                        focusGrid = new Vector2(9.0f, 18.2f), targetNodeId = "kitchen",
                        actionKind = !kBuilt ? QuestActionKind.BuildOrUpgradeNode : QuestActionKind.OpenWorkshop,
                    };
                    break;
                }
                case 5: // 6. 交付居民生计订单
                {
                    bool ok = OrdersCompletedTotal >= 1 || SatisfiedToday >= 1 || CampLevel >= 2;
                    q = new QuestInfo
                    {
                        step = 5, text = $"打开【居民订单】交付 1 单餐饮需求 ({(ok ? 1 : 0)}/1)",
                        cur = ok ? 1 : 0, max = 1, done = ok,
                        focusGrid = new Vector2(16.0f, 18.5f), targetNodeId = null,
                        actionKind = QuestActionKind.OpenResidentOrders,
                    };
                    break;
                }
                case 6: // 7. 培养骨干伙伴（大锤或小棠）升级
                {
                    bool anyLv2 = HeroLevelUpsTotal >= 1;
                    for (int i = 0; i < Heroes.Count; i++)
                        if (Heroes[i].unlocked && Heroes[i].level >= 2) { anyLv2 = true; break; }
                    q = new QuestInfo
                    {
                        step = 6, text = $"打开【骨干】将大锤或小棠升至 Lv.2 ({(anyLv2 ? 1 : 0)}/1)",
                        cur = anyLv2 ? 1 : 0, max = 1, done = anyLv2,
                        focusGrid = new Vector2(16.0f, 19.0f), targetNodeId = null,
                        actionKind = QuestActionKind.OpenHeroGrowth,
                    };
                    break;
                }
                case 7: // 8. 向东南砍树 -> 收集古井石料 -> 修缮净水石井
                {
                    bool ok = GetNodeLevel("relic_well", 0) >= 1;
                    q = new QuestInfo
                    {
                        step = 7, text = $"修缮东南【净水石井】汲取山泉水 ({(ok ? 1 : 0)}/1)",
                        cur = ok ? 1 : 0, max = 1, done = ok,
                        focusGrid = new Vector2(22.6f, 12.0f), targetNodeId = "relic_well",
                        actionKind = QuestActionKind.BuildOrUpgradeNode,
                    };
                    break;
                }
                case 8: // 9. 向西北拓荒建造1号避风民居 (+3人口->7人) 与升级物资仓库至Lv.2
                {
                    bool hOk = GetNodeLevel("house_1", 0) >= 1;
                    bool wOk = GetNodeLevel("warehouse", 1) >= 2;
                    int cur = (hOk ? 1 : 0) + (wOk ? 1 : 0);
                    q = new QuestInfo
                    {
                        step = 8,
                        text = !hOk
                            ? $"建造西北【1号避风民居】扩充人口 ({cur}/2)"
                            : $"升级【营地物资仓库】至 Lv.2 扩容量 ({cur}/2)",
                        cur = cur, max = 2, done = hOk && wOk,
                        focusGrid = !hOk ? new Vector2(10.6f, 22.4f) : new Vector2(19.6f, 18.4f),
                        targetNodeId = !hOk ? "house_1" : "warehouse",
                        actionKind = QuestActionKind.BuildOrUpgradeNode,
                    };
                    break;
                }
                case 9: // 10. 建造山林畜牧栏并投喂鲜蔬净水收获
                {
                    bool rBuilt = GetNodeLevel("beast_ranch", 0) >= 1;
                    bool rCol = RanchCollectedTotal >= 1 || Get(SurvRes.Compost) >= 1 || GetNodeLevel("beast_ranch", 0) >= 2;
                    int cur = (rBuilt ? 1 : 0) + (rCol ? 1 : 0);
                    q = new QuestInfo
                    {
                        step = 9,
                        text = !rBuilt
                            ? "建造正西【山林畜牧栏】开启饲养 (0/2)"
                            : $"在【山林畜牧栏】投喂鲜蔬净水收获 ({cur}/2)",
                        cur = cur, max = 2, done = rBuilt && rCol,
                        focusGrid = new Vector2(7.4f, 13.6f), targetNodeId = "beast_ranch",
                        actionKind = !rBuilt ? QuestActionKind.BuildOrUpgradeNode : QuestActionKind.OpenRanchDrawer,
                    };
                    break;
                }
                case 10: // 11. 在农田施用有机堆肥催熟增产
                {
                    bool ok = CompostUsedTotal >= 1 || GetNodeLevel("farm_a", 0) >= 3;
                    q = new QuestInfo
                    {
                        step = 10, text = $"在【生态农田】施用有机堆肥催熟 ({(ok ? 1 : 0)}/1)",
                        cur = ok ? 1 : 0, max = 1, done = ok,
                        focusGrid = new Vector2(12.6f, 13.8f), targetNodeId = "farm_a",
                        actionKind = QuestActionKind.OpenFarmDrawer,
                    };
                    break;
                }
                case 11: // 12. 建造废铁锻造坊并锤炼铁钉铆件
                {
                    bool smBuilt = GetNodeLevel("smithy", 0) >= 1;
                    bool nailsOk = NailsCraftedTotal >= 2 || Get(SurvRes.Nails) >= 2 || GetNodeLevel("smithy", 0) >= 2;
                    int cur = (smBuilt ? 1 : 0) + (nailsOk ? 1 : 0);
                    q = new QuestInfo
                    {
                        step = 11,
                        text = !smBuilt
                            ? "建造正东【废铁锻造坊】开启五金重工 (0/2)"
                            : $"在【废铁锻造坊】锤炼铁钉铆件 ({cur}/2)",
                        cur = cur, max = 2, done = smBuilt && nailsOk,
                        focusGrid = new Vector2(26.9f, 15.2f), targetNodeId = "smithy",
                        actionKind = !smBuilt ? QuestActionKind.BuildOrUpgradeNode : QuestActionKind.OpenWorkshop,
                    };
                    break;
                }
                case 12: // 13. 建造裁缝防具坊并缝制麻布衣
                {
                    bool tlBuilt = GetNodeLevel("tailor", 0) >= 1;
                    bool clothOk = ClothesCraftedTotal >= 1 || Get(SurvRes.LinenShirt) >= 1 || GetNodeLevel("tailor", 0) >= 2;
                    int cur = (tlBuilt ? 1 : 0) + (clothOk ? 1 : 0);
                    q = new QuestInfo
                    {
                        step = 12,
                        text = !tlBuilt
                            ? "建造东北【裁缝防具坊】开启防寒制衣 (0/2)"
                            : $"在【裁缝防具坊】缝制耐磨麻布衣 ({cur}/2)",
                        cur = cur, max = 2, done = tlBuilt && clothOk,
                        focusGrid = new Vector2(22.6f, 22.2f), targetNodeId = "tailor",
                        actionKind = !tlBuilt ? QuestActionKind.BuildOrUpgradeNode : QuestActionKind.OpenWorkshop,
                    };
                    break;
                }
                case 13: // 14. 修建河畔钓鱼台并垂钓收获鲜鱼
                {
                    bool dkBuilt = GetNodeLevel("river_dock", 0) >= 1;
                    bool fishOk = FishCaughtTotal >= 1 || Get(SurvRes.Fish) >= 2 || GetNodeLevel("river_dock", 0) >= 2;
                    int cur = (dkBuilt ? 1 : 0) + (fishOk ? 1 : 0);
                    q = new QuestInfo
                    {
                        step = 13,
                        text = !dkBuilt
                            ? "修建南岸【河畔钓鱼台】 (0/2)"
                            : $"前往【河畔钓鱼台】垂钓山溪鲜鱼 ({cur}/2)",
                        cur = cur, max = 2, done = dkBuilt && fishOk,
                        focusGrid = new Vector2(13.2f, 4.6f), targetNodeId = "river_dock",
                        actionKind = !dkBuilt ? QuestActionKind.BuildOrUpgradeNode : QuestActionKind.OpenFishing,
                    };
                    break;
                }
                case 14: // 15. 石井Lv.2自动滴灌 + 供暖塔Lv.2扩上限
                {
                    bool wOk = GetNodeLevel("relic_well", 0) >= 2;
                    bool htOk = GetNodeLevel("heat_tower", 1) >= 2;
                    int cur = (wOk ? 1 : 0) + (htOk ? 1 : 0);
                    q = new QuestInfo
                    {
                        step = 14,
                        text = !wOk
                            ? $"升级【净水石井】至 Lv.2 自动滴灌 ({cur}/2)"
                            : $"升级【篝火供暖塔】至 Lv.2 扩上限 ({cur}/2)",
                        cur = cur, max = 2, done = wOk && htOk,
                        focusGrid = !wOk ? new Vector2(22.6f, 12.0f) : new Vector2(13.0f, 18.2f),
                        targetNodeId = !wOk ? "relic_well" : "heat_tower",
                        actionKind = QuestActionKind.BuildOrUpgradeNode,
                    };
                    break;
                }
                case 15: // 16. 兴建2号原木民居与尖木拒马防线
                {
                    bool h2Ok = GetNodeLevel("house_2", 0) >= 1;
                    bool barOk = GetNodeLevel("barricade", 0) >= 1;
                    int cur = (h2Ok ? 1 : 0) + (barOk ? 1 : 0);
                    q = new QuestInfo
                    {
                        step = 15,
                        text = !h2Ok
                            ? $"建造【2号原木民居】扩充人口 ({cur}/2)"
                            : $"布设正南【尖木拒马防线】 ({cur}/2)",
                        cur = cur, max = 2, done = h2Ok && barOk,
                        focusGrid = !h2Ok ? new Vector2(6.2f, 22.2f) : new Vector2(17.6f, 9.6f),
                        targetNodeId = !h2Ok ? "house_2" : "barricade",
                        actionKind = QuestActionKind.BuildOrUpgradeNode,
                    };
                    break;
                }
                case 16: // 17. 架设捕猎木哨塔
                {
                    bool ok = GetNodeLevel("watchtower", 0) >= 1;
                    q = new QuestInfo
                    {
                        step = 16, text = $"架设北侧【捕猎木哨塔】自动狩猎 ({(ok ? 1 : 0)}/1)",
                        cur = ok ? 1 : 0, max = 1, done = ok,
                        focusGrid = new Vector2(14.6f, 25.8f), targetNodeId = "watchtower",
                        actionKind = QuestActionKind.BuildOrUpgradeNode,
                    };
                    break;
                }
                case 17: // 18. 建造山林草药坊熬药或交付防疫单
                {
                    bool hbBuilt = GetNodeLevel("herb_hut", 0) >= 1;
                    bool medOk = MedsCraftedTotal >= 1 || MedicalOrdersCompletedTotal >= 1 ||
                                 Get(SurvRes.HerbPoultice) >= 1 || GetNodeLevel("herb_hut", 0) >= 2;
                    int cur = (hbBuilt ? 1 : 0) + (medOk ? 1 : 0);
                    q = new QuestInfo
                    {
                        step = 17,
                        text = !hbBuilt
                            ? "建造【山林草药坊】开启防疫制药 (0/2)"
                            : $"在【山林草药坊】熬制止血草药包 ({cur}/2)",
                        cur = cur, max = 2, done = hbBuilt && medOk,
                        focusGrid = new Vector2(7.6f, 4.8f), targetNodeId = "herb_hut",
                        actionKind = !hbBuilt ? QuestActionKind.BuildOrUpgradeNode : QuestActionKind.OpenWorkshop,
                    };
                    break;
                }
                case 18: // 19. 改装武装皮卡并打通盘山公路关卡
                {
                    bool trOk = TruckRepaired;
                    bool rdOk = RoadOpened;
                    int cur = (trOk ? 1 : 0) + (rdOk ? 1 : 0);
                    q = new QuestInfo
                    {
                        step = 18,
                        text = !trOk
                            ? $"改装【末日武装皮卡】准备远征 ({cur}/2)"
                            : $"清理东北山口【盘山公路关卡】路障 ({cur}/2)",
                        cur = cur, max = 2, done = trOk && rdOk,
                        focusGrid = !trOk ? new Vector2(10.2f, 26.8f) : new Vector2(19.2f, 26.4f),
                        targetNodeId = !trOk ? "relic_truck" : "relic_road_gate",
                        actionKind = QuestActionKind.BuildOrUpgradeNode,
                    };
                    break;
                }
                case 19: // 20. 修复山涧发电房全作坊供电提速
                {
                    bool ok = GetNodeLevel("relic_generator", 0) >= 1;
                    q = new QuestInfo
                    {
                        step = 19, text = $"修复正东【山涧发电房】并网提速 ({(ok ? 1 : 0)}/1)",
                        cur = ok ? 1 : 0, max = 1, done = ok,
                        focusGrid = new Vector2(26.8f, 20.2f), targetNodeId = "relic_generator",
                        actionKind = QuestActionKind.BuildOrUpgradeNode,
                    };
                    break;
                }
                case 20: // 21. 建造山口行商电台并完成大宗贸易
                {
                    bool radOk = GetNodeLevel("radio_post", 0) >= 1;
                    bool trdOk = CaravanTradesTotal >= 1;
                    int cur = (radOk ? 1 : 0) + (trdOk ? 1 : 0);
                    q = new QuestInfo
                    {
                        step = 20,
                        text = !radOk
                            ? "建造东北【山口行商电台】联络驼队 (0/2)"
                            : $"打开【行商】完成 1 次大宗物资交割 ({cur}/2)",
                        cur = cur, max = 2, done = radOk && trdOk,
                        focusGrid = new Vector2(26.4f, 25.2f), targetNodeId = "radio_post",
                        actionKind = !radOk ? QuestActionKind.BuildOrUpgradeNode : QuestActionKind.OpenCaravan,
                    };
                    break;
                }
                case 21: // 22. 兴建3号木楼、升级Lv.3避难所木堡并达成★Lv.15繁荣营地！
                {
                    bool h3Ok = GetNodeLevel("house_3", 0) >= 1;
                    bool shOk = ShelterLevel >= 3;
                    bool lvOk = CampLevel >= MaxCampLevel;
                    int cur = (h3Ok ? 1 : 0) + (shOk ? 1 : 0) + (lvOk ? 1 : 0);
                    string txt = !h3Ok
                        ? $"兴建【3号幸存者木楼】冲刺30人口 ({cur}/3)"
                        : (!shOk
                            ? $"将【避难所议事厅】升至 Lv.3 木堡 ({cur}/3)"
                            : $"交付订单或扩建营地晋升至 ★Lv.15 ({CampLevel}/{MaxCampLevel})");
                    q = new QuestInfo
                    {
                        step = 21,
                        text = txt,
                        cur = cur, max = 3, done = h3Ok && shOk && lvOk,
                        focusGrid = !h3Ok ? new Vector2(23.8f, 7.4f) : new Vector2(16.0f, 21.0f),
                        targetNodeId = !h3Ok ? "house_3" : (!shOk ? "shelter" : null),
                        actionKind = (!h3Ok || !shOk) ? QuestActionKind.BuildOrUpgradeNode : QuestActionKind.OpenResidentOrders,
                    };
                    break;
                }
                default: // 全部 22 步主线已圆满领奖完毕
                {
                    q = new QuestInfo
                    {
                        step = 21,
                        text = "★Lv.15 传奇深山避难所已大成！自由经营",
                        cur = 1, max = 1, done = false,
                        focusGrid = new Vector2(16.0f, 21.0f), targetNodeId = null,
                        actionKind = QuestActionKind.OpenResidentOrders,
                    };
                    break;
                }
            }
            return WrapNodeDiscoveryQuest(q);
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

            int s = QuestIndex;
            int xpRw = 35 + s * 8;
            int coinRw = 80 + s * 30;
            string extra = "";

            switch (s)
            {
                case 0: xpRw = 35; coinRw = 80; Add(SurvRes.Wood, 8, true); Add(SurvRes.SeedVeggie, 2, true); extra = " · 原木+8 · 鲜蔬种+2"; break;
                case 1: xpRw = 40; coinRw = 100; Add(SurvRes.Wood, 10, true); extra = " · 原木+10"; break;
                case 2: xpRw = 45; coinRw = 110; Add(SurvRes.Plank, 4, true); extra = " · 厚木板+4"; break;
                case 3: xpRw = 45; coinRw = 120; Add(SurvRes.SeedVeggie, 2, true); Add(SurvRes.SeedFlax, 2, true); extra = " · 鲜蔬种+2 · 亚麻种+2"; break;
                case 4: xpRw = 45; coinRw = 120; Add(SurvRes.Ration, 4, true); extra = " · 口粮+4"; break;
                case 5: xpRw = 55; coinRw = 150; Add(SurvRes.Ration, 4, true); extra = " · 口粮+4"; break;
                case 6: xpRw = 55; coinRw = 160; Add(SurvRes.HeroMedal, 2, true); extra = " · 英雄勋章+2"; break;
                case 7: xpRw = 60; coinRw = 160; Add(SurvRes.Water, 6, true); extra = " · 净水+6"; break;
                case 8: xpRw = 70; coinRw = 180; Add(SurvRes.Plank, 6, true); Add(SurvRes.Scrap, 8, true); extra = " · 厚木板+6 · 废铁+8"; break;
                case 9: xpRw = 75; coinRw = 200; Add(SurvRes.Compost, 2, true); Add(SurvRes.Veggie, 4, true); extra = " · 有机堆肥+2 · 鲜蔬+4"; break;
                case 10: xpRw = 80; coinRw = 220; Add(SurvRes.SeedGolden, 1, true); Add(SurvRes.Scrap, 6, true); extra = " · 变异黄金种+1 · 废铁+6"; break;
                case 11: xpRw = 85; coinRw = 240; Add(SurvRes.Nails, 4, true); Add(SurvRes.Cloth, 4, true); extra = " · 铁钉+4 · 粗麻布+4"; break;
                case 12: xpRw = 95; coinRw = 260; Add(SurvRes.Blueprint, 2, true); extra = " · 工程手稿+2"; break;
                case 13: xpRw = 105; coinRw = 280; Add(SurvRes.Fish, 4, true); Add(SurvRes.Nails, 4, true); extra = " · 鲜鱼+4 · 铁钉+4"; break;
                case 14: xpRw = 115; coinRw = 320; Add(SurvRes.Blueprint, 2, true); Add(SurvRes.Plank, 8, true); extra = " · 工程手稿+2 · 厚木板+8"; break;
                case 15: xpRw = 125; coinRw = 360; Add(SurvRes.Blueprint, 2, true); Add(SurvRes.HeroMedal, 2, true); extra = " · 手稿+2 · 勋章+2"; break;
                case 16: xpRw = 135; coinRw = 400; Add(SurvRes.Blueprint, 2, true); Add(SurvRes.Herb, 4, true); extra = " · 手稿+2 · 山草药+4"; break;
                case 17: xpRw = 150; coinRw = 460; Add(SurvRes.Blueprint, 3, true); Add(SurvRes.Scrap, 10, true); extra = " · 手稿+3 · 废铁+10"; break;
                case 18: xpRw = 165; coinRw = 520; Add(SurvRes.Blueprint, 3, true); Add(SurvRes.Parts, 3, true); extra = " · 手稿+3 · 医疗件+3"; break;
                case 19: xpRw = 180; coinRw = 600; Add(SurvRes.Blueprint, 4, true); Add(SurvRes.Parts, 3, true); extra = " · 手稿+4 · 医疗件+3"; break;
                case 20: xpRw = 200; coinRw = 750; Add(SurvRes.Blueprint, 5, true); Add(SurvRes.HeroMedal, 3, true); extra = " · 手稿+5 · 勋章+3"; break;
                default: xpRw = 250; coinRw = 1500; Add(SurvRes.Blueprint, 8, true); Add(SurvRes.HeroMedal, 5, true); extra = " · 手稿+8 · 勋章+5"; break;
            }

            Coins += coinRw;
            if (QuestIndex < 22) QuestIndex++;
            AddXp(xpRw, q.focusGrid);
            EvaluateQuestProgress();
            rewardMsg = $"主线奖励：经验+{xpRw} · 铜币+{coinRw}{extra}";
            return true;
        }

        // =====================================================================
        // GM 调试台专用公共接口（供 ForestGmConsole 直接调用）
        // =====================================================================
        public void GmAddPlayerXp(int xp)
        {
            AddXp(xp, new Vector2(16.4f, 16.2f));
        }

        public void GmSetPlayerLevel(int targetLv, ForestMap map)
        {
            CampLevel = Mathf.Clamp(targetLv, 1, MaxCampLevel);
            CampXp = 0;

            CheckLevelHeroUnlocks(silent: true);
            if (CampLevel >= 4)
                HordeWarningTriggered = true;

            AutoAssignHeroes();
            EnsureCombatSquad();

            if (map != null)
                SyncRevealedNodes(map, silent: true);

            EnsureActiveOrders();
            RefreshCaravanOffers();
            EvaluateQuestProgress();
            OnWorldToast?.Invoke($"GM：主角星级已调整为 ★Lv.{CampLevel}", new Vector2(16.0f, 19.5f));
        }

        public void GmAddHeroLevels(string heroId, int addLevels)
        {
            if (heroId == null)
            {
                int cnt = 0;
                for (int i = 0; i < Heroes.Count; i++)
                {
                    if (!Heroes[i].unlocked) continue;
                    Heroes[i].level = Mathf.Clamp(Heroes[i].level + addLevels, 1, 30);
                    cnt++;
                }
                if (addLevels > 0 && cnt > 0) HeroLevelUpsTotal += cnt;
                OnDelegationChanged?.Invoke();
                EvaluateQuestProgress();
                OnWorldToast?.Invoke($"GM：全队 {cnt} 名英雄等级 +{addLevels}！", new Vector2(16.0f, 19.5f));
            }
            else
            {
                HeroSpec h = FindHero(heroId);
                if (h == null) return;
                h.level = Mathf.Clamp(h.level + addLevels, 1, 30);
                if (addLevels > 0) HeroLevelUpsTotal++;
                OnDelegationChanged?.Invoke();
                EvaluateQuestProgress();
                OnWorldToast?.Invoke($"GM：{h.name} 等级已调至 Lv.{h.level}！", new Vector2(16.0f, 19.5f));
            }
        }

        public void GmUnlockAllHeroes()
        {
            for (int i = 0; i < Heroes.Count; i++)
                UnlockHero(Heroes[i].id, silent: true);
            AutoAssignHeroes();
            EnsureCombatSquad();
            EvaluateQuestProgress();
            OnWorldToast?.Invoke("GM：已解锁全部 6 名骨干英雄！", new Vector2(16.4f, 16.2f));
        }

        public void GmUnlockAllHeroesAndFormations()
        {
            _gmUnlockFullSquad = true;
            for (int i = 0; i < Heroes.Count; i++)
                UnlockHero(Heroes[i].id, silent: true);
            AutoDeployRecommendedFormation();
            AutoAssignHeroes();
            EvaluateQuestProgress();
            OnWorldToast?.Invoke("GM：已解锁全部 6 名英雄、4 大战术阵型与 5 人满编上阵！", new Vector2(16.4f, 16.2f));
        }

        public void GmSetBattleStage(int stage1Based)
        {
            int targetStage = Mathf.Clamp(stage1Based, 1, MaxBattleStages);
            HordeWaveCleared = targetStage - 1;
            CheckLevelHeroUnlocks(silent: true);
            if (!IsFormationUnlocked(SelectedFormation))
                SelectedFormation = TacticalFormationId.StandardLine;
            HordeWarningTriggered = true;
            HordeRepelled = false;
            EnsureCombatSquad();
            EvaluateQuestProgress();
            OnWorldToast?.Invoke($"GM：当前闯关进度已设为 第 {CurrentBattleStage} 关", new Vector2(16.0f, 19.5f));
        }

        public void GmInstantCompleteAll()
        {
            int finishedBuilds = 0;
            int finishedCrafts = 0;
            int ripenedPlots = 0;
            int readyPens = 0;

            for (int i = 0; i < Nodes.Count; i++)
            {
                CampNode n = Nodes[i];
                if (n.state == NodeState.Constructing)
                {
                    n.buildTimer = n.buildDuration;
                    n.state = NodeState.ReadyToHammer;
                }
                if (n.state == NodeState.ReadyToHammer)
                {
                    if (CompleteHammerNode(n))
                        finishedBuilds++;
                }

                while (n.queue.Count > 0)
                {
                    QueueItem q = n.queue[0];
                    n.queue.RemoveAt(0);
                    DeliverRecipeOutput(n, q.recipe);
                    finishedCrafts++;
                }
            }

            int fCap = GetFarmPlotCap();
            for (int i = 0; i < fCap && i < FarmPlots.Count; i++)
            {
                FarmPlotSlot sl = FarmPlots[i];
                if (sl.phase == FarmPhase.Growing)
                {
                    sl.timer = sl.duration;
                    sl.phase = FarmPhase.Ripe;
                    ripenedPlots++;
                }
            }
            if (ripenedPlots > 0)
                SyncFarmVisualPhase();

            int rCap = GetRanchPenCap();
            for (int i = 0; i < rCap && i < RanchPens.Count; i++)
            {
                RanchPenSlot pen = RanchPens[i];
                if (pen.feeding)
                {
                    pen.feeding = false;
                    pen.timer = pen.duration;
                    pen.readyToCollect = true;
                    readyPens++;
                }
            }

            EvaluateQuestProgress();
            OnWorldToast?.Invoke($"GM秒完：建筑{finishedBuilds} · 生产{finishedCrafts} · 农田{ripenedPlots} · 畜牧{readyPens}", new Vector2(16.4f, 16.2f));
        }
    }
}
