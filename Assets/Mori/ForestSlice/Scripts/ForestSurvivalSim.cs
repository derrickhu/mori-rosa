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

        // --- 英雄独立养成通货与功能补给箱（不占基础仓库容量） ---
        HeroExp = 35,        // 英雄经验（金色晶簇，唯一用于英雄升级！）
        RecruitTicket = 36,  // 电台招募卡（短波广播站抽英雄与碎片）
        SkillBook = 37,      // 战术技能书（提升英雄1~4技能等级）
        ShardUnivEpic = 38,  // 史诗万能碎片（1:1补齐史诗/稀有英雄碎片）
        ShardUnivLeg = 39,   // 传说万能碎片（1:1补齐传说英雄碎片）
        ChoiceResBox = 40,   // 原料自选补给箱（任选原木/废铁/净水/粗麻布/山草药）
        SpeedHourglass = 41, // 工坊加速沙漏（缩短作坊/建造/农牧倒计时5分钟）
    }

    public enum HeroFaction
    {
        IronGuard = 0,   // 铁卫团 (大锤, 雷猛, 阿飒)
        ForestPact = 1,  // 山林盟 (林砚, 老陈头, 青禾)
        WasteTech = 2,   // 废土科工 (小棠, 铁叔, 周博士)
    }

    public enum HeroEquipSlot
    {
        Weapon = 0,      // 武器槽 (锻造坊: 攻击 + 技能强度)
        Armor = 1,       // 护甲槽 (裁缝坊: 生命 + 护甲, 同步换装!)
        Boots = 2,       // 战靴槽 (裁缝坊: 生命 + 抗性)
        Goggles = 3,     // 战术镜槽 (锻造/电子: 技能强度 + 暴击率)
    }

    public enum TacticalBadgeId
    {
        None = 0,
        EnergyCore = 1,  // 先手电芯徽章 (ico_badge_energy): 开场怒气+40, 技能强度+10%
        ThornShield = 2, // 路障荆棘徽章 (ico_badge_thorn): 护甲+120, 近战反伤22%
        EagleScope = 3,  // 鹰眼狙击徽章 (ico_badge_eagle): 暴击率+15%, 攻击+12%
        PyroPowder = 4,  // 高爆火药徽章 (ico_badge_pyro): 技能强度+22%, 元素/范围伤害+18%
        BloodFury = 5,   // 狂战士血怒徽章 (ico_badge_berserk): 生命<50%时攻速+30%, 普攻吸血12%
        MedicCross = 6,  // 白衣圣手徽章 (ico_badge_medic): 治疗与护盾量+28%, 抗性+90
    }

    public struct HeroSkillDef
    {
        public int slotIndex;         // 0..3
        public string name;
        public string iconKey;
        public int unlockStars;       // 0, 1, 2, 3
        public int currentLv;         // 0 if locked, else 1..5
        public int maxLv;             // 5
        public int reqStarsForNextLv; // Lv1->2 needs 1★, Lv2->3 needs 2★, Lv3->4 needs 3★, Lv4->5 needs 4★
        public int bookCostNext;      // e.g. 10, 20, 35, 55
        public bool isUnlocked;
        public string descFormatted;
        public string metricLabel;
        public string[] tierValues;   // length 5 strings
    }

    public struct HeroComputedStats
    {
        public int maxHp;
        public int atk;
        public int skillPower;
        public int range;             // 1..5
        public int armor;
        public int resistance;
        public float critRate;        // e.g. 0.08f
        public float critMult;        // e.g. 1.50f
        public float startRage;       // 0..100 rage points (divide by 100f for rage01)
        public float thornsPct;
        public float lifestealPct;
        public float healShieldBonus;
        public float activeSkillMult; // e.g. 2.20f -> 3.50f scaling with skillLevels[0]
        public int combatPower;
    }

    public struct GachaPullResult
    {
        public bool isFullHero;
        public bool isNewUnlock;
        public int heroIdx;
        public int shardCount;
        public SurvRes bonusItem;
        public int bonusCount;
        public string label;
        public string iconKey;
        public int rarity0to5;
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
        StandardLine = 0,   // 默认防线阵 (ico_fmt_default, 初始解锁)
        SpearAssault = 1,   // 尖锥突击阵 (ico_fmt_spear, 第4关图谱, 收集全员+2%)
        ResistBarrier = 2,  // 抗性增强阵 (ico_fmt_resist, 第10关图谱, 收集全员+2%, 上阵全队抗性+120)
        IronBastion = 3,    // 铁桶护甲阵 (ico_fmt_bastion, 第16关图谱, 收集全员+3%, 前排护甲+180 & 回血)
        VolleyFire = 4,     // 火力齐射阵 (ico_fmt_energy, 第20关图谱, 收集全员+3%, 中后排攻速+25%)
        EnergySurge = 5,    // 能量激涌阵 (ico_fmt_energy, 第24关图谱, 收集全员+5%, 中后排开场怒气+40)
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
        public int rewardHeroExp;
        public int rewardRecruitTickets;
        public int rewardSkillBooks;
        public int rewardShardHeroIdx;
        public int rewardShardCount;
        public int unlockHeroIdx;      // -1 if none, else hero idx unlocked on first clear
        public int unlockFormationIdx; // -1 if none, else (int)TacticalFormationId unlocked on first clear
        public string milestoneTag;    // e.g. "首通招募：大锤（铁壁重卫）" or "首通解锁：尖锥突击阵" or ""
    }

    public struct SlotStatBuff
    {
        public string shortTag;   // e.g. "护甲+80" / "减伤+25%" / "抗性+120" / "怒气+40" / ""
        public bool highlighted;
        public float hpBonus;
        public float atkBonus;
        public int armorBonus;
        public int resistBonus;
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
        static readonly float[] StarCurveAnchors = { 1.00f, 1.25f, 1.60f, 2.10f, 2.75f, 3.65f };

        public string id;
        public string characterId;
        public string name;
        public string roleTitle;
        public HeroFaction faction = HeroFaction.IronGuard;
        public HeroRarity rarity = HeroRarity.Rare;
        public int level = 1;
        public int star = 0;
        public int starPetals = 0;
        public int shards = 0;
        public int spentHeroExp = 0;
        public int spentSkillBooks = 0;
        public int baseAtk = 24;
        public int baseHp = 180;
        public int baseSkillPower = 30;
        public int baseArmor = 50;
        public int baseResist = 40;
        public int rangeCells = 1;
        public int unlockStage = 0;
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

        public readonly int[] skillLevels = new int[4] { 1, 0, 0, 0 };
        public readonly int[] equipTiers = new int[4];
        public readonly TacticalBadgeId[] equippedBadges = new TacticalBadgeId[3];

        public int attackRange
        {
            get => rangeCells;
            set => rangeCells = value;
        }

        public int stars
        {
            get => star;
            set => star = Mathf.Clamp(value, 0, 5);
        }

        public string ShardIconKey => "ico_shard_" + id;
        public string FullBodyKey => "fullbody_" + id;

        public string RarityTag => rarity == HeroRarity.Legendary ? "传说" : (rarity == HeroRarity.Epic ? "史诗" : "稀有");

        public static float EvalStarPetalMultiplier(int s, int petals)
        {
            if (s >= 5) return StarCurveAnchors[5];
            int cs = Mathf.Clamp(s, 0, 4);
            int cp = Mathf.Clamp(petals, 0, 5);
            return Mathf.Lerp(StarCurveAnchors[cs], StarCurveAnchors[cs + 1], cp / 5f);
        }

        public float StarMult => EvalStarPetalMultiplier(star, starPetals);

        public float SkinAtkMult => skinTier == OutfitTier.Armored ? 1.25f : (skinTier == OutfitTier.Reinforced ? 1.12f : 1.0f);
        public float SkinHpMult => skinTier == OutfitTier.Armored ? 1.35f : (skinTier == OutfitTier.Reinforced ? 1.16f : 1.0f);

        public int Attack
        {
            get
            {
                int wFlat = equipTiers[0] >= 4 ? 105 : (equipTiers[0] == 3 ? 60 : (equipTiers[0] == 2 ? 32 : (equipTiers[0] == 1 ? 14 : 0)));
                return Mathf.RoundToInt(baseAtk * (1f + 0.060f * Mathf.Max(0, level - 1)) * StarMult) + wFlat;
            }
        }

        public int MaxHp
        {
            get
            {
                int aFlat = equipTiers[1] >= 4 ? 680 : (equipTiers[1] == 3 ? 390 : (equipTiers[1] == 2 ? 210 : (equipTiers[1] == 1 ? 90 : 0)));
                int bFlat = equipTiers[2] >= 4 ? 420 : (equipTiers[2] == 3 ? 240 : (equipTiers[2] == 2 ? 130 : (equipTiers[2] == 1 ? 55 : 0)));
                return Mathf.RoundToInt(baseHp * (1f + 0.065f * Mathf.Max(0, level - 1)) * StarMult) + aFlat + bFlat;
            }
        }

        public int SkillPower
        {
            get
            {
                int wFlat = equipTiers[0] >= 4 ? 90 : (equipTiers[0] == 3 ? 52 : (equipTiers[0] == 2 ? 28 : (equipTiers[0] == 1 ? 12 : 0)));
                int gFlat = equipTiers[3] >= 4 ? 115 : (equipTiers[3] == 3 ? 68 : (equipTiers[3] == 2 ? 36 : (equipTiers[3] == 1 ? 16 : 0)));
                return Mathf.RoundToInt(baseSkillPower * (1f + 0.060f * Mathf.Max(0, level - 1)) * StarMult) + wFlat + gFlat;
            }
        }

        public int CombatPower
        {
            get
            {
                int arm = baseArmor + Mathf.Max(0, level - 1) * 3;
                int res = baseResist + Mathf.Max(0, level - 1) * 2;
                int skillSum = 0;
                for (int i = 0; i < 4; i++)
                    if (star >= i) skillSum += Mathf.Max(0, skillLevels[i] - 1);
                float basePwr = MaxHp * 0.45f + Attack * 4.2f + SkillPower * 3.6f + (arm + res) * 1.5f;
                return Mathf.RoundToInt(basePwr * (1f + 0.08f * skillSum));
            }
        }

        public float GetDelegationScore(DelegationPostKind post)
        {
            float rBase = rarity == HeroRarity.Legendary ? 16f : (rarity == HeroRarity.Epic ? 12f : 8.5f);
            float spec = (post == prefPost1 || post == prefPost2) ? 1.28f : 1.0f;
            float star2Bonus = star >= 2 ? 1.25f : 1.0f;
            if (star >= 2 && (post == prefPost1 || post == prefPost2) &&
                (id == "asa" || id == "qinghe" || id == "zhoudr"))
            {
                star2Bonus *= 1.12f;
            }
            return rBase * StarMult * star2Bonus * (1f + 0.025f * Mathf.Max(0, level - 1)) * spec;
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
        public readonly Dictionary<TacticalBadgeId, int> OwnedBadges = new Dictionary<TacticalBadgeId, int>();
        readonly HashSet<TacticalFormationId> _unlockedFormations = new HashSet<TacticalFormationId>();
        public bool AutoExchangeUniversalShards = true;
        public float PatrolAccumSec = 480f;
        public int TotalRecruitPulls = 0;
        public int RecruitPityCounter = 0;
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

        public static bool IsStrategicRes(SurvRes r)
        {
            return r == SurvRes.Ration || r == SurvRes.Blueprint || r == SurvRes.Parts || r == SurvRes.HeroMedal ||
                   r == SurvRes.SeedVeggie || r == SurvRes.SeedFlax || r == SurvRes.SeedHerb || r == SurvRes.SeedGolden ||
                   r == SurvRes.Compost ||
                   r == SurvRes.HeroExp || r == SurvRes.RecruitTicket || r == SurvRes.SkillBook ||
                   r == SurvRes.ShardUnivEpic || r == SurvRes.ShardUnivLeg ||
                   r == SurvRes.ChoiceResBox || r == SurvRes.SpeedHourglass;
        }

        public static bool IsWarehouseCapped(SurvRes r)
        {
            return !IsStrategicRes(r);
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

            // 独立英雄养成初始体验包：英雄经验×600、招募卡×5、技能书×15、原料自选箱×2、工坊加速沙漏×2 + 先手电芯徽章×1
            _res[SurvRes.HeroExp] = 600;
            _res[SurvRes.RecruitTicket] = 5;
            _res[SurvRes.SkillBook] = 15;
            _res[SurvRes.ChoiceResBox] = 2;
            _res[SurvRes.SpeedHourglass] = 2;

            _revealedRes.Add(SurvRes.Wood);
            _revealedRes.Add(SurvRes.Ration);
            _revealedRes.Add(SurvRes.HeroExp);
            _revealedRes.Add(SurvRes.RecruitTicket);
            _revealedRes.Add(SurvRes.SkillBook);
            _revealedRes.Add(SurvRes.ChoiceResBox);
            _revealedRes.Add(SurvRes.SpeedHourglass);

            foreach (TacticalBadgeId b in Enum.GetValues(typeof(TacticalBadgeId)))
            {
                if (b != TacticalBadgeId.None)
                    OwnedBadges[b] = 0;
            }
            OwnedBadges[TacticalBadgeId.EnergyCore] = 1;
            _unlockedFormations.Add(TacticalFormationId.StandardLine);

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
                // 每次升星保底赠送铜币 + 独立英雄经验与招募补给，Lv.4+ 赠工程手稿，Lv.7+ 赠医疗电子件，确保纯种田玩家永不卡死！
                Coins += 80 * CampLevel;
                Add(SurvRes.HeroExp, 120 + CampLevel * 65, notifyToast: false);
                Add(SurvRes.SkillBook, 2 + CampLevel / 3, notifyToast: false);
                if (CampLevel % 2 == 0)
                    Add(SurvRes.RecruitTicket, 1, notifyToast: true);
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
                    Add(SurvRes.RecruitTicket, 2, notifyToast: true);
                }
                else if (CampLevel == 3)
                {
                    Add(SurvRes.ChoiceResBox, 1, notifyToast: true);
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
                    Add(SurvRes.ShardUnivEpic, 3, notifyToast: true);
                }
                else if (CampLevel == 7 || CampLevel == 9 || CampLevel == 10)
                {
                    Add(SurvRes.ShardUnivEpic, 4, notifyToast: true);
                }
                else if (CampLevel == 12)
                {
                    Add(SurvRes.ShardUnivLeg, 4, notifyToast: true);
                }
                else if (CampLevel == 15)
                {
                    Add(SurvRes.ShardUnivLeg, 8, notifyToast: true);
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
            if (CampLevel >= 4 || HordeWaveCleared >= 3)
                UnlockHero("tieshu", silent);
            if (CampLevel >= 5 || HordeWaveCleared >= 5)
                UnlockHero("linyan", silent);
            if (CampLevel >= 8 || HordeWaveCleared >= 8)
                UnlockHero("qinghe", silent);
            if (CampLevel >= 10 || HordeWaveCleared >= 12)
                UnlockHero("laochentou", silent);
            if (CampLevel >= 13 || HordeWaveCleared >= 15)
                UnlockHero("asa", silent);
            if (CampLevel >= 15 || HordeWaveCleared >= 20)
                UnlockHero("zhoudr", silent);
        }

        static string[] GetUnlocksForLevel(int lv)
        {
            switch (lv)
            {
                case 2: return new[] { "净水石井", "山林畜牧栏", "英雄上限Lv.10" };
                case 3: return new[] { "短波招募电台", "废铁锻造坊", "裁缝防具坊" };
                case 4: return new[] { "河畔钓鱼台", "石井Lv.2自动滴灌", "上阵扩编3人" };
                case 5: return new[] { "2号原木民居", "尖木拒马防线", "英雄上限Lv.25" };
                case 6: return new[] { "捕猎木哨塔", "畜牧栏Lv.2(山猪)", "防荆棘猎皮靴" };
                case 7: return new[] { "山林草药坊", "包铁承重梁", "止血草药敷包" };
                case 8: return new[] { "末日武装皮卡", "盘山公路关卡", "上阵扩编4人" };
                case 9: return new[] { "山涧发电房", "供暖塔Lv.3", "加绒御寒皮衣" };
                case 10: return new[] { "驱寒草本热饮", "2号民居Lv.3", "英雄上限Lv.50" };
                case 11: return new[] { "3号幸存者木楼", "导流金属管", "农田Lv.3(4垄满田)" };
                case 12: return new[] { "供暖塔Lv.4(温室+40%)", "上阵满编5人", "发电房Lv.2" };
                case 13: return new[] { "抗尸毒急救箱", "重甲皮卡Lv.2", "行商电台Lv.2" };
                case 14: return new[] { "议事厅Lv.3末日木堡", "废土钢板护甲", "农田Lv.4满阶" };
                case 15: return new[] { "物资仓库Lv.5(1200)", "发电房Lv.3(+65%)", "英雄上限Lv.75" };
                default: return new[] { "全营地高阶等级上限开放" };
            }
        }

        public bool IsResRevealed(SurvRes r) => _revealedRes.Contains(r);

        public static string GetResName(SurvRes r) => ResName(r);

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
                case SurvRes.HeroExp: return "英雄经验";
                case SurvRes.RecruitTicket: return "电台招募卡";
                case SurvRes.SkillBook: return "战术技能书";
                case SurvRes.ShardUnivEpic: return "史诗万能碎片";
                case SurvRes.ShardUnivLeg: return "传说万能碎片";
                case SurvRes.ChoiceResBox: return "原料自选箱";
                case SurvRes.SpeedHourglass: return "工坊加速沙漏";
                default: return r.ToString();
            }
        }

        public static string GetResIconKey(SurvRes r)
        {
            switch (r)
            {
                case SurvRes.Wood: return "ico_wood";
                case SurvRes.Scrap: return "ico_scrap";
                case SurvRes.Water: return "ico_water";
                case SurvRes.Veggie: return "ico_veggie";
                case SurvRes.Cloth: return "ico_cloth";
                case SurvRes.Herb: return "ico_herb";
                case SurvRes.Fish: return "ico_fish";
                case SurvRes.Meat: return "ico_meat";
                case SurvRes.Pelt: return "ico_pelt";
                case SurvRes.Plank: return "ico_plank";
                case SurvRes.WoodBed: return "ico_woodbed";
                case SurvRes.IronBeam: return "rec_beam";
                case SurvRes.Nails: return "ico_nails";
                case SurvRes.IronIngot: return "ico_ingot";
                case SurvRes.MetalPipe: return "ico_pipe";
                case SurvRes.LinenShirt: return "ico_shirt";
                case SurvRes.LeatherBoots: return "ico_boots";
                case SurvRes.FurCoat: return "ico_coat";
                case SurvRes.PlateArmor: return "ico_armor";
                case SurvRes.RoastVeggie: return "ico_veggie";
                case SurvRes.VeggieSoup: return "rec_stew";
                case SurvRes.GrillFish: return "rec_roast_fish";
                case SurvRes.SmokedMeat: return "ico_ration";
                case SurvRes.HerbPoultice: return "ico_poultice";
                case SurvRes.WarmTea: return "ico_tea";
                case SurvRes.MedKit: return "ico_medkit";
                case SurvRes.Ration: return "ico_ration";
                case SurvRes.Parts: return "ico_parts";
                case SurvRes.Blueprint: return "ico_blueprint";
                case SurvRes.HeroMedal: return "ico_hero_medal";
                case SurvRes.SeedVeggie: return "ico_seed_veggie";
                case SurvRes.SeedFlax: return "ico_seed_flax";
                case SurvRes.SeedHerb: return "ico_seed_herb";
                case SurvRes.SeedGolden: return "ico_seed_golden";
                case SurvRes.Compost: return "ico_compost";
                case SurvRes.HeroExp: return "ico_hero_exp";
                case SurvRes.RecruitTicket: return "ico_recruit_card";
                case SurvRes.SkillBook: return "ico_skill_book_1";
                case SurvRes.ShardUnivEpic: return "ico_shard_univ_epic";
                case SurvRes.ShardUnivLeg: return "ico_shard_univ_leg";
                case SurvRes.ChoiceResBox: return "ico_choice_crate";
                case SurvRes.SpeedHourglass: return "ico_speed_hourglass";
                default: return "ico_wood";
            }
        }

        public int Get(SurvRes r) => _res.TryGetValue(r, out int v) ? v : 0;
        public int GetRes(SurvRes r) => Get(r);

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

        // =====================================================================
        // 《疯狂水世界》级英雄独立经济 · 5×5星瓣 · 四技能 · 徽章 · 4槽工坊装备 · 阵营光环
        // =====================================================================
        public int MaxHeroLevelCap => Mathf.Max(5, CampLevel * 5);

        public int GetHeroLevelUpExpCost(int currentLevel)
            => Mathf.RoundToInt(50f * Mathf.Pow(Mathf.Max(1, currentLevel), 1.45f));

        // 保留旧版静态查询签名以保证工程零编译断链
        public static int GetHeroLevelUpCoins(int curLv) => 60 + curLv * 45;
        public static int GetHeroLevelUpRations(int curLv) => 2 + curLv / 3;
        public static int GetHeroStarUpMedals(int curStars)
        {
            int[] need = { 0, 3, 8, 15, 25, 999 };
            return need[Mathf.Clamp(curStars, 1, 5)];
        }

        public bool TryLevelUpHero(string heroId, out string msg)
            => TryLevelUpHeroWithExp(FindHeroIndex(heroId), out msg);

        public bool TryLevelUpHero(int heroIdx, out string msg)
            => TryLevelUpHeroWithExp(heroIdx, out msg);

        public bool TryLevelUpHeroWithExp(int heroIdx, out string msg)
        {
            if (heroIdx < 0 || heroIdx >= Heroes.Count || !Heroes[heroIdx].unlocked)
            {
                msg = "该英雄尚未招募入队！";
                return false;
            }
            HeroSpec h = Heroes[heroIdx];
            if (h.level >= MaxHeroLevelCap)
            {
                msg = $"已达上限(Lv.{MaxHeroLevelCap})，需要提升玩家领主等级！";
                return false;
            }
            int expNeed = GetHeroLevelUpExpCost(h.level);
            int expOwn = Get(SurvRes.HeroExp);
            if (expOwn < expNeed)
            {
                msg = $"英雄经验不足 ({expOwn}/{expNeed})，请前往【山口闯关】或领取【巡逻补给箱】！";
                return false;
            }

            Add(SurvRes.HeroExp, -expNeed);
            h.spentHeroExp += expNeed;
            h.level++;
            HeroLevelUpsTotal++;
            OnDelegationChanged?.Invoke();
            EvaluateQuestProgress();
            msg = $"{h.name} 升至 Lv.{h.level}！六维战力与委任效率同步提升！";
            return true;
        }

        public bool TryResetHeroLevel(int heroIdx, out string msg)
        {
            if (heroIdx < 0 || heroIdx >= Heroes.Count || !Heroes[heroIdx].unlocked)
            {
                msg = "该英雄尚未入队";
                return false;
            }
            HeroSpec h = Heroes[heroIdx];
            if (h.level <= 1 && h.spentSkillBooks <= 0 && h.spentHeroExp <= 0)
            {
                msg = $"{h.name} 当前为初始 1 级且未升级技能，无需重置";
                return false;
            }

            int refundExp = h.spentHeroExp;
            if (refundExp <= 0 && h.level > 1)
            {
                for (int lv = 1; lv < h.level; lv++)
                    refundExp += GetHeroLevelUpExpCost(lv);
            }
            int refundBooks = h.spentSkillBooks;

            if (refundExp > 0)
                Add(SurvRes.HeroExp, refundExp, notifyToast: true);
            if (refundBooks > 0)
                Add(SurvRes.SkillBook, refundBooks, notifyToast: true);

            h.level = 1;
            h.spentHeroExp = 0;
            h.spentSkillBooks = 0;
            for (int s = 0; s < 4; s++)
                h.skillLevels[s] = h.star >= s ? 1 : 0;

            bool unequippedAnyBadge = false;
            for (int b = 0; b < h.equippedBadges.Length; b++)
            {
                if (h.equippedBadges[b] != TacticalBadgeId.None)
                {
                    h.equippedBadges[b] = TacticalBadgeId.None;
                    unequippedAnyBadge = true;
                }
            }

            OnDelegationChanged?.Invoke();
            string bookPart = refundBooks > 0 ? $" · 技能书+{refundBooks}" : "";
            string badgePart = unequippedAnyBadge ? "（佩戴徽章已卸回空闲池，请记得重新装配！）" : "（若需调整徽章可前往徽章页装配）";
            msg = $"{h.name} 无损重置回 Lv.1！返还英雄经验+{refundExp}{bookPart} {badgePart}";
            return true;
        }

        static readonly int[][] StarPetalShardCosts =
        {
            new[] { 2, 2, 2, 2, 2 },       // 0★ -> 1★ (累计10)
            new[] { 4, 4, 6, 6, 8 },       // 1★ -> 2★ (累计28)
            new[] { 10, 12, 14, 16, 18 },  // 2★ -> 3★ (累计70)
            new[] { 22, 24, 26, 28, 30 },  // 3★ -> 4★ (累计130)
            new[] { 35, 40, 45, 50, 60 },  // 4★ -> 5★ (累计230)
        };

        public float GetHeroStarMultiplier(int star, int petals)
            => HeroSpec.EvalStarPetalMultiplier(star, petals);

        public int GetNextPetalShardCost(int heroIdx)
        {
            if (heroIdx < 0 || heroIdx >= Heroes.Count) return 0;
            HeroSpec h = Heroes[heroIdx];
            if (h.star >= 5) return 0;
            int s = Mathf.Clamp(h.star, 0, 4);
            int p = Mathf.Clamp(h.starPetals, 0, 4);
            return StarPetalShardCosts[s][p];
        }

        public SurvRes GetUniversalShardResFor(HeroRarity rarity)
            => rarity == HeroRarity.Legendary ? SurvRes.ShardUnivLeg : SurvRes.ShardUnivEpic;

        public int GetAvailableUniversalShardsFor(int heroIdx)
        {
            if (heroIdx < 0 || heroIdx >= Heroes.Count) return 0;
            return Get(GetUniversalShardResFor(Heroes[heroIdx].rarity));
        }

        public bool TryRecruitHeroWithShards(int heroIdx, out string msg)
        {
            if (heroIdx < 0 || heroIdx >= Heroes.Count)
            {
                msg = "无效的英雄索引";
                return false;
            }
            HeroSpec h = Heroes[heroIdx];
            if (h.unlocked)
            {
                msg = $"【{h.name}】已在队伍中，可使用碎片继续升星！";
                return false;
            }
            if (h.shards < 10)
            {
                msg = $"【{h.name}碎片】不足 ({h.shards}/10)，前往广播电台招募或闯关获取！";
                return false;
            }

            h.shards -= 10;
            h.skillLevels[0] = Mathf.Max(1, h.skillLevels[0]);
            UnlockHero(h.id, silent: false);
            AutoAssignHeroes();
            msg = $"★ 合成招募成功！【{h.name}】正式入列！";
            return true;
        }

        public bool TryExchangeUniversalShards(int heroIdx, int count, out string msg)
        {
            if (heroIdx < 0 || heroIdx >= Heroes.Count || count <= 0)
            {
                msg = "请选择有效的兑换数量";
                return false;
            }
            HeroSpec h = Heroes[heroIdx];
            SurvRes uRes = GetUniversalShardResFor(h.rarity);
            int have = Get(uRes);
            if (have <= 0)
            {
                msg = $"背包内暂无【{ResName(uRes)}】";
                return false;
            }
            int actual = Mathf.Min(count, have);
            Add(uRes, -actual);
            h.shards += actual;
            msg = $"成功将 {actual} 枚【{ResName(uRes)}】1:1 兑换为【{h.name}碎片×{actual}】！";
            return true;
        }

        public bool TryAdvanceHeroStarPetal(int heroIdx, bool allowAutoUniversal, out bool crossedFullStar, out string msg)
        {
            crossedFullStar = false;
            if (heroIdx < 0 || heroIdx >= Heroes.Count)
            {
                msg = "无效的英雄索引";
                return false;
            }
            HeroSpec h = Heroes[heroIdx];
            if (!h.unlocked)
                return TryRecruitHeroWithShards(heroIdx, out msg);

            if (h.star >= 5)
            {
                msg = $"【{h.name}】已达最高 5★ 觉醒满星！";
                return false;
            }

            int cost = GetNextPetalShardCost(heroIdx);
            int missing = cost - h.shards;
            int autoUsedUniv = 0;
            if (missing > 0)
            {
                SurvRes uRes = GetUniversalShardResFor(h.rarity);
                int availUniv = Get(uRes);
                if (allowAutoUniversal && availUniv >= missing)
                {
                    Add(uRes, -missing);
                    h.shards += missing;
                    autoUsedUniv = missing;
                }
                else
                {
                    msg = availUniv >= missing
                        ? $"【{h.name}碎片】不足 ({h.shards}/{cost})，请开启[自动兑换]或点击兑换【{ResName(uRes)}】！"
                        : $"【{h.name}碎片】不足 ({h.shards}/{cost})，前往【广播电台】招募或闯关获取！";
                    return false;
                }
            }

            h.shards -= cost;
            h.starPetals++;
            if (h.starPetals >= 5)
            {
                h.starPetals = 0;
                h.star = Mathf.Clamp(h.star + 1, 0, 5);
                crossedFullStar = true;
                if (h.star >= 1) h.skillLevels[1] = Mathf.Max(1, h.skillLevels[1]);
                if (h.star >= 2) h.skillLevels[2] = Mathf.Max(1, h.skillLevels[2]);
                if (h.star >= 3) h.skillLevels[3] = Mathf.Max(1, h.skillLevels[3]);
            }

            OnDelegationChanged?.Invoke();
            string univHint = autoUsedUniv > 0 ? $"（自动补齐万能碎片×{autoUsedUniv}）" : "";
            if (crossedFullStar)
            {
                string unlockMilestone = h.star == 1 ? "解锁第2技能 + 第1徽章槽！"
                    : (h.star == 2 ? "解锁第3技能 + 委任效率+25%！"
                    : (h.star == 3 ? "解锁第4核心质变技能 + 第2徽章槽！"
                    : (h.star == 4 ? "全技能上限突破至 Lv.5！" : "解锁第3徽章槽 · 满星觉醒！")));
                msg = $"★ 整星突破！{h.name} 晋升为 {h.star}★！{unlockMilestone}{univHint}";
            }
            else
            {
                msg = $"★ 星瓣点亮！{h.name} 升至 {h.star}★ · {h.starPetals}/5瓣，三维属性跳涨！{univHint}";
            }
            return true;
        }

        public bool TryStarUpHero(int heroIdx, out string msg)
            => TryAdvanceHeroStarPetal(heroIdx, AutoExchangeUniversalShards, out _, out msg);

        public bool TryStarUpHero(string heroId, out string msg)
            => TryAdvanceHeroStarPetal(FindHeroIndex(heroId), AutoExchangeUniversalShards, out _, out msg);

        // ---------------- 4 圆章技能定义与战术技能书升级 ----------------
        static readonly int[] SkillBookCostsByLv = { 10, 20, 35, 55 };

        public HeroSkillDef GetHeroSkillDef(int heroIdx, int skillSlot0to3)
        {
            int slot = Mathf.Clamp(skillSlot0to3, 0, 3);
            HeroSpec h = (heroIdx >= 0 && heroIdx < Heroes.Count) ? Heroes[heroIdx] : null;
            string hid = h != null ? h.id : "dachui";
            int unlockStarReq = slot; // 0, 1, 2, 3
            bool unlocked = h != null && h.star >= unlockStarReq;
            int curLv = 0;
            if (unlocked)
            {
                curLv = Mathf.Clamp(Mathf.Max(1, h.skillLevels[slot]), 1, 5);
                h.skillLevels[slot] = curLv;
            }
            int dispTierIdx = Mathf.Clamp(curLv > 0 ? curLv - 1 : 0, 0, 4);

            string sName = "战术绝技";
            string sIcon = "ww_skill_shield";
            string metric = "技能效果";
            string[] tiers = { "100%", "120%", "140%", "160%", "180%" };
            string descTemplate = "造成 <color=#22B24C>{0}</color> 技能伤害。";

            switch (hid)
            {
                case "dachui":
                    if (slot == 0)
                    {
                        sName = "路牌重盾震击"; sIcon = "ww_skill_shield"; metric = "护盾与震击倍率";
                        tiers = new[] { "220%", "250%", "285%", "320%", "360%" };
                        descTemplate = "展开吸收 <color=#22B24C>{0}</color> 技能强度的路牌重盾并嘲讽周围敌人，重锤猛砸造成范围<color=#E86A1C>物理伤害</color>。";
                    }
                    else if (slot == 1)
                    {
                        sName = "钢筋铁骨"; sIcon = "ww_passive_heart"; metric = "最大生命加成";
                        tiers = new[] { "12%", "18%", "24%", "30%", "36%" };
                        descTemplate = "前排重卫强健体魄，永久提高自身最大生命 <color=#22B24C>{0}</color>，并额外增加 <color=#22B24C>+80</color> 点物理护甲。";
                    }
                    else if (slot == 2)
                    {
                        sName = "焊甲反震"; sIcon = "ww_skill_shield"; metric = "护盾减伤比例";
                        tiers = new[] { "20%", "24%", "28%", "32%", "36%" };
                        descTemplate = "护盾持续期间受到伤害降低 <color=#22B24C>{0}</color>，并对近战撕咬目标反弹<color=#E86A1C>物理伤害</color>。";
                    }
                    else
                    {
                        sName = "【重盾震击】增强"; sIcon = "ww_passive_fury"; metric = "二次震晕时长";
                        tiers = new[] { "1.5秒", "1.7秒", "1.9秒", "2.2秒", "2.5秒" };
                        descTemplate = "护盾结束或被击破时引发二次震波，眩晕周围敌人 <color=#22B24C>{0}</color> 并造成震荡<color=#E86A1C>物理伤害</color>！";
                    }
                    break;

                case "xiaotang":
                    if (slot == 0)
                    {
                        sName = "高爆燃烧瓶"; sIcon = "ww_skill_fire"; metric = "火海总伤倍率";
                        tiers = new[] { "280%", "315%", "350%", "390%", "440%" };
                        descTemplate = "在敌群脚下铺设 3.5秒 烈焰火海，共造成 <color=#22B24C>{0}</color> 技能强度的范围<color=#E86A1C>火焰元素伤害</color>。";
                    }
                    else if (slot == 1)
                    {
                        sName = "高能燃料"; sIcon = "ww_passive_sword"; metric = "技能强度加成";
                        tiers = new[] { "12%", "18%", "24%", "30%", "36%" };
                        descTemplate = "改良高燃度汽油配比，永久提高自身技能强度 <color=#22B24C>{0}</color>。";
                    }
                    else if (slot == 2)
                    {
                        sName = "熔甲灼烧"; sIcon = "ww_skill_fire"; metric = "双防削弱比例";
                        tiers = new[] { "20%", "24%", "28%", "32%", "36%" };
                        descTemplate = "烈焰火海内的敌人护甲与抗性降低 <color=#22B24C>{0}</color>，克制重甲安全帽尸卫。";
                    }
                    else
                    {
                        sName = "【燃烧瓶】增强"; sIcon = "ww_passive_fury"; metric = "二次爆轰倍率";
                        tiers = new[] { "120%", "140%", "160%", "185%", "215%" };
                        descTemplate = "烈焰火海结束时触发二次连环爆轰，对范围内灼烧目标额外造成 <color=#22B24C>{0}</color> 技能强度的<color=#E86A1C>元素伤害</color>！";
                    }
                    break;

                case "leimeng":
                    if (slot == 0)
                    {
                        sName = "狂风破障斩"; sIcon = "ww_skill_cleave"; metric = "旋风重斧倍率";
                        tiers = new[] { "260%", "295%", "330%", "370%", "420%" };
                        descTemplate = "挥动消防重斧旋风横扫，造成 <color=#22B24C>{0}</color> 技能强度的范围<color=#E86A1C>物理伤害</color>并击退敌人。";
                    }
                    else if (slot == 1)
                    {
                        sName = "破障体魄"; sIcon = "ww_passive_sword"; metric = "攻击与生命加成";
                        tiers = new[] { "10%", "15%", "20%", "25%", "30%" };
                        descTemplate = "消防攻坚百战淬炼，永久提高自身攻击力与最大生命 <color=#22B24C>{0}</color>。";
                    }
                    else if (slot == 2)
                    {
                        sName = "撕裂伤口"; sIcon = "ww_skill_cleave"; metric = "顺劈流血伤害";
                        tiers = new[] { "35%", "45%", "55%", "65%", "80%" };
                        descTemplate = "普攻每第 3 次挥斧触发宽幅顺劈前方尸群，并附加 <color=#22B24C>{0}</color> 流血<color=#E86A1C>物理伤害</color>。";
                    }
                    else
                    {
                        sName = "【破障斩】增强"; sIcon = "ww_passive_fury"; metric = "命中已损回血";
                        tiers = new[] { "8%", "10%", "12%", "14%", "16%" };
                        descTemplate = "绝技每命中 1 名敌人恢复自身 <color=#22B24C>{0}</color> 已损生命，并叠加 +6% 攻击速度（最多5层）！";
                    }
                    break;

                case "linyan":
                    if (slot == 0)
                    {
                        sName = "贯日散射箭雨"; sIcon = "ww_skill_arrow"; metric = "箭雨总伤倍率";
                        tiers = new[] { "300%", "340%", "385%", "435%", "490%" };
                        descTemplate = "向敌阵倾泻 6 波穿云连弩箭，总计造成 <color=#22B24C>{0}</color> 技能强度的贯穿<color=#E86A1C>物理伤害</color>。";
                    }
                    else if (slot == 1)
                    {
                        sName = "疾风连矢"; sIcon = "ww_passive_sword"; metric = "攻击力加成";
                        tiers = new[] { "12%", "17%", "22%", "27%", "32%" };
                        descTemplate = "山林游猎高速机括，永久提高自身攻击力 <color=#22B24C>{0}</color> 与暴击率 <color=#22B24C>+8%</color>。";
                    }
                    else if (slot == 2)
                    {
                        sName = "弱点洞悉"; sIcon = "ww_skill_arrow"; metric = "穿透二段伤害";
                        tiers = new[] { "65%", "75%", "85%", "95%", "110%" };
                        descTemplate = "普攻触发暴击时弩箭直线穿透目标，对后方敌人造成 <color=#22B24C>{0}</color> <color=#E86A1C>物理伤害</color>。";
                    }
                    else
                    {
                        sName = "【散射箭雨】增强"; sIcon = "ww_passive_fury"; metric = "每次绝技加攻";
                        tiers = new[] { "6%", "8%", "10%", "12%", "15%" };
                        descTemplate = "每次释放绝技后，整场战斗永久叠加暴击率 +5% 与攻击力 <color=#22B24C>{0}</color>！";
                    }
                    break;

                case "tieshu":
                    if (slot == 0)
                    {
                        sName = "双管高爆霰弹"; sIcon = "ww_skill_shotgun"; metric = "高爆霰弹倍率";
                        tiers = new[] { "230%", "260%", "295%", "335%", "380%" };
                        descTemplate = "扇形倾泻高爆铁砂弹，造成 <color=#22B24C>{0}</color> 技能强度的<color=#E86A1C>物理伤害</color>并强力击退逼近尸潮。";
                    }
                    else if (slot == 1)
                    {
                        sName = "重装火药"; sIcon = "ww_passive_sword"; metric = "攻击力加成";
                        tiers = new[] { "10%", "15%", "20%", "25%", "30%" };
                        descTemplate = "军工火药增压填装，永久提高自身攻击力 <color=#22B24C>{0}</color>，并增加 <color=#22B24C>+60</color> 点护甲。";
                    }
                    else if (slot == 2)
                    {
                        sName = "压制装填"; sIcon = "ww_skill_shotgun"; metric = "双发速射倍率";
                        tiers = new[] { "70%", "82%", "95%", "110%", "125%" };
                        descTemplate = "每第 4 次普攻触发双管连发速射并击退近身敌人，附带 <color=#22B24C>{0}</color> 伤害。";
                    }
                    else
                    {
                        sName = "【高爆霰弹】增强"; sIcon = "ww_passive_fury"; metric = "贴脸暴伤加成";
                        tiers = new[] { "45%", "55%", "68%", "82%", "100%" };
                        descTemplate = "对逼近中前排的近身敌人，暴击伤害大幅提高 <color=#22B24C>{0}</color>！";
                    }
                    break;

                case "qinghe":
                    if (slot == 0)
                    {
                        sName = "霜寒捕兽夹阵"; sIcon = "ww_skill_arrow"; metric = "寒霜冻结倍率";
                        tiers = new[] { "190%", "220%", "250%", "280%", "310%" };
                        descTemplate = "向密集尸群投掷液氮寒霜瓶，造成 <color=#22B24C>{0}</color> 技能强度的范围<color=#E86A1C>寒霜元素伤害</color>并强控冻结 <color=#22B24C>2.0秒</color>。";
                    }
                    else if (slot == 1)
                    {
                        sName = "寒林屏息"; sIcon = "ww_passive_heart"; metric = "技强与抗性加成";
                        tiers = new[] { "12%", "17%", "22%", "27%", "32%" };
                        descTemplate = "雪山潜猎屏息凝神，永久提高自身技能强度与元素抗性 <color=#22B24C>{0}</color>。";
                    }
                    else if (slot == 2)
                    {
                        sName = "霜冻迟缓"; sIcon = "ww_skill_arrow"; metric = "减速减攻速比例";
                        tiers = new[] { "25%", "28%", "31%", "35%", "40%" };
                        descTemplate = "普攻淬霜冰矢令目标移速与攻速降低 <color=#22B24C>{0}</color>（持续2.8秒），且驻守农牧采收额外提速。";
                    }
                    else
                    {
                        sName = "【寒霜瓶】增强"; sIcon = "ww_passive_fury"; metric = "全队碎冰易伤";
                        tiers = new[] { "22%", "25%", "28%", "31%", "34%" };
                        descTemplate = "对处于冻结或霜冻减速状态的敌人，全队伤害提高 <color=#22B24C>{0}</color>！";
                    }
                    break;

                case "asa":
                    if (slot == 0)
                    {
                        sName = "飞爪夺命钩"; sIcon = "ww_skill_cleave"; metric = "夺命重劈倍率";
                        tiers = new[] { "240%", "275%", "310%", "345%", "380%" };
                        descTemplate = "甩出改装绞盘铁钩将敌方最远后排强行拉拽至身前重劈，造成 <color=#22B24C>{0}</color> 技能强度的<color=#E86A1C>物理伤害</color>。";
                    }
                    else if (slot == 1)
                    {
                        sName = "猎杀本能"; sIcon = "ww_passive_sword"; metric = "暴击与攻击加成";
                        tiers = new[] { "8%/8%", "11%/12%", "14%/16%", "17%/20%", "20%/24%" };
                        descTemplate = "荒野悬赏敏锐猎觉，永久提高暴击率与攻击力 <color=#22B24C>{0}</color>。";
                    }
                    else if (slot == 2)
                    {
                        sName = "孤狼猎手"; sIcon = "ww_skill_cleave"; metric = "击杀回复怒气";
                        tiers = new[] { "35点", "40点", "45点", "50点", "55点" };
                        descTemplate = "击败任意目标立即恢复自身 <color=#22B24C>{0}</color> 怒气，且远征皮卡与哨塔委任效率额外提升。";
                    }
                    else
                    {
                        sName = "【夺命钩】增强"; sIcon = "ww_passive_fury"; metric = "破甲眩晕强化";
                        tiers = new[] { "35%", "40%", "45%", "50%", "55%" };
                        descTemplate = "被拉拽目标物理护甲降低 <color=#22B24C>{0}</color>（持续5秒），并陷入眩晕沉默 <color=#22B24C>2.2秒</color>！";
                    }
                    break;

                case "zhoudr":
                    if (slot == 0)
                    {
                        sName = "超载电磁风暴"; sIcon = "ww_skill_fire"; metric = "单次电弧倍率";
                        tiers = new[] { "115%", "132%", "150%", "168%", "185%" };
                        descTemplate = "召唤高压电弧在敌群间连锁弹射 8 次，每次造成 <color=#22B24C>{0}</color> 技能强度的<color=#E86A1C>元素伤害</color>并麻痹打断。";
                    }
                    else if (slot == 1)
                    {
                        sName = "高压线圈"; sIcon = "ww_passive_sword"; metric = "技强与暴击加成";
                        tiers = new[] { "12%/6%", "17%/9%", "22%/12%", "27%/15%", "32%/18%" };
                        descTemplate = "背包高压电容持续增压，永久提高技能强度与暴击率 <color=#22B24C>{0}</color>。";
                    }
                    else if (slot == 2)
                    {
                        sName = "静电场域"; sIcon = "ww_skill_shield"; metric = "科工开场护盾";
                        tiers = new[] { "15%", "18%", "21%", "24%", "27%" };
                        descTemplate = "开场为全体上阵【废土科工】英雄提供 <color=#22B24C>{0}</color> 最大生命护盾，且工坊生产委任额外提速。";
                    }
                    else
                    {
                        sName = "【电磁风暴】增强"; sIcon = "ww_passive_fury"; metric = "同目标递增增伤";
                        tiers = new[] { "15%", "18%", "21%", "24%", "28%" };
                        descTemplate = "电弧命中同一目标时伤害递增 <color=#22B24C>{0}</color>，若击败目标则额外弹射 <color=#22B24C>2次</color>！";
                    }
                    break;

                default: // laochentou
                    if (slot == 0)
                    {
                        sName = "战地急救热汤"; sIcon = "ww_skill_heal"; metric = "全队恢复倍率";
                        tiers = new[] { "160%", "185%", "210%", "240%", "275%" };
                        descTemplate = "为全队恢复 <color=#22B24C>{0}</color> 技能强度的<color=#22B24C>生命值</color>，并施加持续 4秒 的草本护盾。";
                    }
                    else if (slot == 1)
                    {
                        sName = "药膳滋补"; sIcon = "ww_passive_heart"; metric = "技强与生命加成";
                        tiers = new[] { "10%", "15%", "20%", "25%", "30%" };
                        descTemplate = "深山百草温补秘方，永久提高自身技能强度与最大生命 <color=#22B24C>{0}</color>。";
                    }
                    else if (slot == 2)
                    {
                        sName = "振奋浓香"; sIcon = "ww_skill_heal"; metric = "主C攻速增益";
                        tiers = new[] { "18%", "22%", "26%", "30%", "36%" };
                        descTemplate = "开场及每 9秒 为全队攻击力最高的友军提升 <color=#22B24C>{0}</color> 攻击速度。";
                    }
                    else
                    {
                        sName = "【急救热汤】增强"; sIcon = "ww_passive_fury"; metric = "全队额外回怒";
                        tiers = new[] { "20点", "24点", "28点", "32点", "40点" };
                        descTemplate = "全队获得热汤治疗时，额外恢复 <color=#22B24C>{0}</color> 怒气并提升 +15% 元素抗性！";
                    }
                    break;
            }

            int reqNextStars = !unlocked ? unlockStarReq : (curLv < 5 ? Mathf.Max(unlockStarReq, curLv) : 5);
            int nextBookCost = !unlocked ? SkillBookCostsByLv[0] : (curLv < 5 ? SkillBookCostsByLv[Mathf.Clamp(curLv - 1, 0, 3)] : 0);

            return new HeroSkillDef
            {
                slotIndex = slot,
                name = sName,
                iconKey = sIcon,
                unlockStars = unlockStarReq,
                currentLv = curLv,
                maxLv = 5,
                reqStarsForNextLv = reqNextStars,
                bookCostNext = nextBookCost,
                isUnlocked = unlocked,
                descFormatted = string.Format(descTemplate, tiers[dispTierIdx]),
                metricLabel = metric,
                tierValues = tiers,
            };
        }

        public bool TryUpgradeHeroSkill(int heroIdx, int skillSlot0to3, out string msg)
        {
            if (heroIdx < 0 || heroIdx >= Heroes.Count || !Heroes[heroIdx].unlocked)
            {
                msg = "该英雄尚未入队";
                return false;
            }
            HeroSpec h = Heroes[heroIdx];
            HeroSkillDef def = GetHeroSkillDef(heroIdx, skillSlot0to3);
            if (!def.isUnlocked)
            {
                msg = $"英雄升星到 {def.unlockStars}★ 可解锁【{def.name}】！";
                return false;
            }
            if (def.currentLv >= def.maxLv)
            {
                msg = $"【{def.name}】已达最高等级 Lv.{def.maxLv}！";
                return false;
            }
            if (h.star < def.reqStarsForNextLv)
            {
                msg = $"英雄升星到 {def.reqStarsForNextLv}★ 才可继续升级【{def.name}】！";
                return false;
            }
            int haveBooks = GetRes(SurvRes.SkillBook);
            if (haveBooks < def.bookCostNext)
            {
                msg = $"战术技能书不足 ({haveBooks}/{def.bookCostNext})，可通过山口闯关、巡逻箱或行商获取！";
                return false;
            }

            Add(SurvRes.SkillBook, -def.bookCostNext);
            h.spentSkillBooks += def.bookCostNext;
            h.skillLevels[def.slotIndex] = Mathf.Clamp(def.currentLv + 1, 1, def.maxLv);
            OnDelegationChanged?.Invoke();
            msg = $"★ 技能升级成功！【{def.name}】提升至 Lv.{h.skillLevels[def.slotIndex]}！";
            return true;
        }

        // ---------------- 战术徽章系统（6大徽章 · 1★/3★/5★ 三槽装配） ----------------
        public void AddBadge(TacticalBadgeId b, int delta = 1)
        {
            if (b == TacticalBadgeId.None || delta == 0) return;
            int cur = GetOwnedBadgeCount(b);
            OwnedBadges[b] = Mathf.Max(0, cur + delta);
        }

        public static string GetBadgeName(TacticalBadgeId b)
        {
            switch (b)
            {
                case TacticalBadgeId.EnergyCore: return "先手电芯徽章";
                case TacticalBadgeId.ThornShield: return "路障荆棘徽章";
                case TacticalBadgeId.EagleScope: return "鹰眼狙击徽章";
                case TacticalBadgeId.PyroPowder: return "高爆火药徽章";
                case TacticalBadgeId.BloodFury: return "狂战士血怒徽章";
                case TacticalBadgeId.MedicCross: return "白衣圣手徽章";
                default: return "未装配";
            }
        }

        public static string GetBadgeIconKey(TacticalBadgeId b)
        {
            switch (b)
            {
                case TacticalBadgeId.EnergyCore: return "ico_badge_energy";
                case TacticalBadgeId.ThornShield: return "ico_badge_thorn";
                case TacticalBadgeId.EagleScope: return "ico_badge_eagle";
                case TacticalBadgeId.PyroPowder: return "ico_badge_pyro";
                case TacticalBadgeId.BloodFury: return "ico_badge_berserk";
                case TacticalBadgeId.MedicCross: return "ico_badge_medic";
                default: return "";
            }
        }

        public static string GetBadgeDesc(TacticalBadgeId b)
        {
            switch (b)
            {
                case TacticalBadgeId.EnergyCore: return "开场怒气 +40，技能强度 +10%";
                case TacticalBadgeId.ThornShield: return "物理护甲 +120，受到近战攻击反伤 22%";
                case TacticalBadgeId.EagleScope: return "暴击率 +15%，攻击力 +12%";
                case TacticalBadgeId.PyroPowder: return "技能强度 +22%，元素/范围伤害 +18%";
                case TacticalBadgeId.BloodFury: return "生命<50%时攻速 +30%，普攻吸血 12%";
                case TacticalBadgeId.MedicCross: return "治疗与护盾量 +28%，元素抗性 +90";
                default: return "装配战地徽章可赋予英雄强力战斗被动";
            }
        }

        public static int GetBadgeRarity0To5(TacticalBadgeId b)
        {
            switch (b)
            {
                case TacticalBadgeId.EnergyCore:
                case TacticalBadgeId.PyroPowder:
                    return 4; // 橙品
                case TacticalBadgeId.ThornShield:
                case TacticalBadgeId.EagleScope:
                case TacticalBadgeId.MedicCross:
                    return 3; // 紫品
                case TacticalBadgeId.BloodFury:
                    return 2; // 蓝品
                default:
                    return 0;
            }
        }

        public int GetOwnedBadgeCount(TacticalBadgeId b)
        {
            if (b == TacticalBadgeId.None) return 0;
            return OwnedBadges.TryGetValue(b, out int c) ? Mathf.Max(0, c) : 0;
        }

        public int GetFreeBadgeCount(TacticalBadgeId b)
        {
            if (b == TacticalBadgeId.None) return 0;
            int owned = GetOwnedBadgeCount(b);
            int used = 0;
            for (int i = 0; i < Heroes.Count; i++)
            {
                HeroSpec h = Heroes[i];
                for (int s = 0; s < h.equippedBadges.Length; s++)
                    if (h.equippedBadges[s] == b) used++;
            }
            return Mathf.Max(0, owned - used);
        }

        public bool IsBadgeSlotUnlocked(HeroSpec h, int badgeSlot0to2)
        {
            if (h == null) return false;
            if (badgeSlot0to2 == 0) return h.star >= 1;
            if (badgeSlot0to2 == 1) return h.star >= 3;
            if (badgeSlot0to2 == 2) return h.star >= 5;
            return false;
        }

        public bool TryEquipHeroBadge(int heroIdx, int badgeSlot0to2, TacticalBadgeId badge, out string msg)
        {
            if (heroIdx < 0 || heroIdx >= Heroes.Count || !Heroes[heroIdx].unlocked)
            {
                msg = "该英雄尚未入队";
                return false;
            }
            if (badgeSlot0to2 < 0 || badgeSlot0to2 >= 3)
            {
                msg = "无效的徽章槽位";
                return false;
            }
            HeroSpec h = Heroes[heroIdx];

            if (badge == TacticalBadgeId.None)
            {
                TacticalBadgeId prev = h.equippedBadges[badgeSlot0to2];
                if (prev == TacticalBadgeId.None)
                {
                    msg = "该槽位未佩戴徽章";
                    return false;
                }
                h.equippedBadges[badgeSlot0to2] = TacticalBadgeId.None;
                msg = $"已卸下【{GetBadgeName(prev)}】";
                return true;
            }

            if (!IsBadgeSlotUnlocked(h, badgeSlot0to2))
            {
                int reqStar = badgeSlot0to2 == 0 ? 1 : (badgeSlot0to2 == 1 ? 3 : 5);
                msg = $"第 {badgeSlot0to2 + 1} 徽章槽需英雄升至 {reqStar}★ 解锁！";
                return false;
            }

            if (h.equippedBadges[badgeSlot0to2] == badge)
            {
                msg = $"已装配【{GetBadgeName(badge)}】";
                return true;
            }

            // 同一英雄不重复佩戴两枚完全相同类型的徽章；若已在自身其他槽位则直接挪槽
            for (int s = 0; s < 3; s++)
            {
                if (s != badgeSlot0to2 && h.equippedBadges[s] == badge)
                {
                    h.equippedBadges[s] = h.equippedBadges[badgeSlot0to2];
                    h.equippedBadges[badgeSlot0to2] = badge;
                    msg = $"已将【{GetBadgeName(badge)}】移至第 {badgeSlot0to2 + 1} 槽！";
                    return true;
                }
            }

            if (GetFreeBadgeCount(badge) <= 0)
            {
                msg = GetOwnedBadgeCount(badge) > 0
                    ? $"【{GetBadgeName(badge)}】已全部被其他英雄佩戴，请先卸下或获取更多！"
                    : $"尚未获得【{GetBadgeName(badge)}】（首领关首通/奖励箱掉落）";
                return false;
            }

            h.equippedBadges[badgeSlot0to2] = badge;
            msg = $"{h.name} 已装配【{GetBadgeName(badge)}】！";
            return true;
        }

        // ---------------- 4 部位工坊战备打造与升阶（锻造坊 + 裁缝坊联动） ----------------
        static readonly string[][] EquipTierNames =
        {
            new[] { "未穿戴武器", "铆接战备武器", "锻钢强化武器", "合金破甲重武", "裂谷大师专武" },
            new[] { "未穿戴护甲", "耐磨麻布工衣", "防刺铆钉皮衣", "加绒御寒皮甲", "废土复合钢甲" },
            new[] { "未穿戴战靴", "绑腿草编靴", "防荆棘猎皮靴", "铁掌行军战靴", "液压突击重靴" },
            new[] { "未穿戴战术镜", "防尘风镜", "裂片瞄准护目镜", "红外测距战术镜", "神经辅助战术仪" },
        };

        static readonly int[] EquipWeaponAtk = { 0, 14, 32, 60, 105 };
        static readonly int[] EquipWeaponSp = { 0, 12, 28, 52, 90 };
        static readonly int[] EquipArmorHp = { 0, 90, 210, 390, 680 };
        static readonly int[] EquipArmorDef = { 0, 35, 75, 135, 220 };
        static readonly int[] EquipBootsHp = { 0, 55, 130, 240, 420 };
        static readonly int[] EquipBootsRes = { 0, 30, 68, 120, 195 };
        static readonly int[] EquipGogglesSp = { 0, 16, 36, 68, 115 };
        static readonly float[] EquipGogglesCrit = { 0f, 0.03f, 0.06f, 0.10f, 0.15f };

        static string GetHeroWeaponFlavorName(string heroId, int tier)
        {
            int t = Mathf.Clamp(tier, 0, 4);
            if (t == 0) return EquipTierNames[0][0];
            switch (heroId)
            {
                case "dachui":
                    return new[] { "", "铆接路牌锤盾", "锻钢防暴重盾", "合金震荡重锤", "裂谷泰坦巨盾" }[t];
                case "xiaotang":
                    return new[] { "", "改装高燃火瓶", "增压铝热火罐", "高爆凝固汽油箱", "裂谷炼狱爆破器" }[t];
                case "leimeng":
                    return new[] { "", "加固消防手斧", "锻钢破障战斧", "合金旋风巨斧", "裂谷狂澜斩首斧" }[t];
                case "linyan":
                    return new[] { "", "轻木猎手短弩", "复合滑轮连弩", "穿云破甲重弩", "裂谷贯日神机弩" }[t];
                case "tieshu":
                    return new[] { "", "锯短双管猎枪", "锻钢霰弹火铳", "军工高爆霰弹枪", "裂谷轰鸣铁砂炮" }[t];
                case "laochentou":
                    return new[] { "", "行军草药铜壶", "保温百草汤锅", "高压蒸汽急救釜", "裂谷神农续命鼎" }[t];
                case "qinghe":
                    return new[] { "", "寒铁猎兽弹弓", "霜淬轻羽手弩", "液氮急冻连发弩", "裂谷凛冬冰封弩" }[t];
                case "asa":
                    return new[] { "", "改装绞盘铁钩", "锻钢倒刺飞爪", "液压夺命链刃", "裂谷血月猎魂钩" }[t];
                case "zhoudr":
                    return new[] { "", "自制铜线电杖", "高压电容导流杖", "超导磁暴手杖", "裂谷雷神电弧枢" }[t];
                default:
                    return EquipTierNames[0][t];
            }
        }

        public void GetHeroEquipSpec(
            int heroIdx,
            HeroEquipSlot slot,
            out int curTier,
            out int nextTier,
            out string curName,
            out string nextName,
            out string iconKey,
            out string statSummary,
            out CostPair[] nextCosts,
            out string buildingReqHint,
            out bool canCraftNow)
        {
            int sIdx = Mathf.Clamp((int)slot, 0, 3);
            HeroSpec h = (heroIdx >= 0 && heroIdx < Heroes.Count) ? Heroes[heroIdx] : null;
            curTier = h != null ? Mathf.Clamp(h.equipTiers[sIdx], 0, 4) : 0;
            nextTier = Mathf.Min(4, curTier + 1);
            if (sIdx == 0 && h != null)
            {
                curName = GetHeroWeaponFlavorName(h.id, curTier);
                nextName = curTier < 4 ? GetHeroWeaponFlavorName(h.id, nextTier) : "已满阶";
            }
            else
            {
                curName = EquipTierNames[sIdx][curTier];
                nextName = curTier < 4 ? EquipTierNames[sIdx][nextTier] : "已满阶";
            }

            switch (slot)
            {
                case HeroEquipSlot.Weapon:
                    iconKey = "ico_gear_weapon";
                    statSummary = curTier == 0
                        ? $"打造: 攻击+{EquipWeaponAtk[1]} · 技强+{EquipWeaponSp[1]}"
                        : (curTier < 4
                            ? $"攻击+{EquipWeaponAtk[curTier]}→{EquipWeaponAtk[nextTier]} · 技强+{EquipWeaponSp[curTier]}→{EquipWeaponSp[nextTier]}"
                            : $"满阶: 攻击+{EquipWeaponAtk[4]} · 技强+{EquipWeaponSp[4]}");
                    if (curTier >= 4) nextCosts = Array.Empty<CostPair>();
                    else if (nextTier == 1) nextCosts = new[] { new CostPair(SurvRes.Scrap, 4), new CostPair(SurvRes.Nails, 2) };
                    else if (nextTier == 2) nextCosts = new[] { new CostPair(SurvRes.IronIngot, 2), new CostPair(SurvRes.Nails, 4) };
                    else if (nextTier == 3) nextCosts = new[] { new CostPair(SurvRes.MetalPipe, 2), new CostPair(SurvRes.Blueprint, 2) };
                    else nextCosts = new[] { new CostPair(SurvRes.MetalPipe, 4), new CostPair(SurvRes.Blueprint, 4), new CostPair(SurvRes.Parts, 2) };
                    break;

                case HeroEquipSlot.Armor:
                    iconKey = "ico_gear_armor";
                    statSummary = curTier == 0
                        ? $"打造: 生命+{EquipArmorHp[1]} · 护甲+{EquipArmorDef[1]}"
                        : (curTier < 4
                            ? $"生命+{EquipArmorHp[curTier]}→{EquipArmorHp[nextTier]} · 护甲+{EquipArmorDef[curTier]}→{EquipArmorDef[nextTier]}"
                            : $"满阶: 生命+{EquipArmorHp[4]} · 护甲+{EquipArmorDef[4]}");
                    if (curTier >= 4) nextCosts = Array.Empty<CostPair>();
                    else if (nextTier == 1) nextCosts = new[] { new CostPair(SurvRes.LinenShirt, 1), new CostPair(SurvRes.Cloth, 2) };
                    else if (nextTier == 2) nextCosts = new[] { new CostPair(SurvRes.Pelt, 3), new CostPair(SurvRes.Nails, 2) };
                    else if (nextTier == 3) nextCosts = new[] { new CostPair(SurvRes.FurCoat, 1), new CostPair(SurvRes.IronIngot, 2) };
                    else nextCosts = new[] { new CostPair(SurvRes.PlateArmor, 1), new CostPair(SurvRes.Blueprint, 3) };
                    break;

                case HeroEquipSlot.Boots:
                    iconKey = "ico_gear_boots";
                    statSummary = curTier == 0
                        ? $"打造: 生命+{EquipBootsHp[1]} · 抗性+{EquipBootsRes[1]}"
                        : (curTier < 4
                            ? $"生命+{EquipBootsHp[curTier]}→{EquipBootsHp[nextTier]} · 抗性+{EquipBootsRes[curTier]}→{EquipBootsRes[nextTier]}"
                            : $"满阶: 生命+{EquipBootsHp[4]} · 抗性+{EquipBootsRes[4]}");
                    if (curTier >= 4) nextCosts = Array.Empty<CostPair>();
                    else if (nextTier == 1) nextCosts = new[] { new CostPair(SurvRes.Cloth, 3), new CostPair(SurvRes.Pelt, 1) };
                    else if (nextTier == 2) nextCosts = new[] { new CostPair(SurvRes.LeatherBoots, 1), new CostPair(SurvRes.Nails, 2) };
                    else if (nextTier == 3) nextCosts = new[] { new CostPair(SurvRes.LeatherBoots, 1), new CostPair(SurvRes.IronIngot, 2), new CostPair(SurvRes.Parts, 1) };
                    else nextCosts = new[] { new CostPair(SurvRes.FurCoat, 1), new CostPair(SurvRes.MetalPipe, 2), new CostPair(SurvRes.Parts, 2) };
                    break;

                default: // Goggles
                    iconKey = "ico_gear_goggles";
                    statSummary = curTier == 0
                        ? $"打造: 技强+{EquipGogglesSp[1]} · 暴击+3%"
                        : (curTier < 4
                            ? $"技强+{EquipGogglesSp[curTier]}→{EquipGogglesSp[nextTier]} · 暴击+{Mathf.RoundToInt(EquipGogglesCrit[curTier] * 100f)}%→{Mathf.RoundToInt(EquipGogglesCrit[nextTier] * 100f)}%"
                            : $"满阶: 技强+{EquipGogglesSp[4]} · 暴击+15%");
                    if (curTier >= 4) nextCosts = Array.Empty<CostPair>();
                    else if (nextTier == 1) nextCosts = new[] { new CostPair(SurvRes.Scrap, 3), new CostPair(SurvRes.Cloth, 2) };
                    else if (nextTier == 2) nextCosts = new[] { new CostPair(SurvRes.Nails, 3), new CostPair(SurvRes.Parts, 1) };
                    else if (nextTier == 3) nextCosts = new[] { new CostPair(SurvRes.IronIngot, 2), new CostPair(SurvRes.Parts, 2), new CostPair(SurvRes.Blueprint, 2) };
                    else nextCosts = new[] { new CostPair(SurvRes.MetalPipe, 2), new CostPair(SurvRes.Parts, 4), new CostPair(SurvRes.Blueprint, 4) };
                    break;
            }

            bool isTailorSlot = slot == HeroEquipSlot.Armor || slot == HeroEquipSlot.Boots;
            string shopId = isTailorSlot ? "tailor" : "smithy";
            string shopTitle = isTailorSlot ? "裁缝防具坊" : "废铁锻造坊";
            int reqShopLv = isTailorSlot ? nextTier : Mathf.Min(3, nextTier);
            int curShopLv = GetNodeLevel(shopId, 0);

            if (curTier >= 4)
            {
                buildingReqHint = "已升至传世橙装满阶";
                canCraftNow = false;
                return;
            }

            bool afford = CanAfford(nextCosts);
            buildingReqHint = curShopLv >= reqShopLv
                ? $"【{shopTitle} Lv.{curShopLv}】可打造"
                : $"产自【{shopTitle} Lv.{reqShopLv}】";
            canCraftNow = h != null && h.unlocked && afford;
        }

        public bool TryCraftOrUpgradeHeroEquip(int heroIdx, HeroEquipSlot slot, out string msg)
        {
            if (heroIdx < 0 || heroIdx >= Heroes.Count || !Heroes[heroIdx].unlocked)
            {
                msg = "该英雄尚未入队";
                return false;
            }
            HeroSpec h = Heroes[heroIdx];
            GetHeroEquipSpec(heroIdx, slot, out int curTier, out int nextTier, out _, out string nextName,
                out _, out _, out CostPair[] nextCosts, out string buildingReqHint, out bool canCraftNow);

            if (curTier >= 4)
            {
                msg = "该部位战备已达最高 4 阶橙品！";
                return false;
            }
            if (!canCraftNow || !CanAfford(nextCosts))
            {
                msg = $"材料不足（{buildingReqHint}）：需 {FormatCosts(nextCosts)}";
                return false;
            }

            Pay(nextCosts);
            h.equipTiers[(int)slot] = nextTier;

            if (slot == HeroEquipSlot.Armor)
            {
                OutfitTier targetSkin = nextTier >= 3 ? OutfitTier.Armored : (nextTier == 2 ? OutfitTier.Reinforced : h.skinTier);
                if (targetSkin > h.skinTier)
                {
                    h.skinTier = targetSkin;
                    h.portraitKey = PuppetRegistry.ResolveBustKey(h.characterId, targetSkin);
                    OnHeroSkinChanged?.Invoke(h, targetSkin);
                }
            }

            OnDelegationChanged?.Invoke();
            msg = $"★ 打造升阶成功！{h.name} 穿戴【{nextName}】！";
            return true;
        }

        // ---------------- 三大幸存者阵营光环 & 完整六维战力计算 ----------------
        public static string GetFactionName(HeroFaction f)
        {
            switch (f)
            {
                case HeroFaction.IronGuard: return "铁卫团";
                case HeroFaction.ForestPact: return "山林盟";
                case HeroFaction.WasteTech: return "废土科工";
                default: return "幸存者";
            }
        }

        public static string GetFactionIconKey(HeroFaction f)
        {
            switch (f)
            {
                case HeroFaction.IronGuard: return "ico_faction_iron";
                case HeroFaction.ForestPact: return "ico_faction_forest";
                case HeroFaction.WasteTech: return "ico_faction_tech";
                default: return "ico_faction_iron";
            }
        }

        public void GetFactionAuraState(
            out string activeTitle,
            out string activeDesc,
            out float bonusPct,
            out int ironCount,
            out int forestCount,
            out int techCount)
        {
            ironCount = 0;
            forestCount = 0;
            techCount = 0;
            int[] counts = new int[3];
            int[] aceCounts = new int[3];

            for (int s = 0; s < FormationSlots.Length; s++)
            {
                int idx = FormationSlots[s];
                if (idx < 0 || idx >= Heroes.Count || !Heroes[idx].unlocked) continue;
                HeroSpec h = Heroes[idx];
                int f = Mathf.Clamp((int)h.faction, 0, 2);
                counts[f]++;
                if (h.rarity == HeroRarity.Legendary || h.star >= 3)
                    aceCounts[f]++;
            }

            ironCount = counts[0];
            forestCount = counts[1];
            techCount = counts[2];

            int best3Faction = -1;
            int pairsCount = 0;
            int bestPairFaction = -1;
            for (int f = 0; f < 3; f++)
            {
                if (counts[f] >= 3 && (best3Faction < 0 || counts[f] > counts[best3Faction]))
                    best3Faction = f;
                if (counts[f] >= 2)
                {
                    pairsCount++;
                    if (bestPairFaction < 0) bestPairFaction = f;
                }
            }

            if (best3Faction >= 0)
            {
                string fn = GetFactionName((HeroFaction)best3Faction);
                bool allAce = aceCounts[best3Faction] >= counts[best3Faction];
                bonusPct = allAce ? 0.25f : 0.15f;
                activeTitle = allAce ? $"王牌{fn}光环 (+25%)" : $"精锐{fn}光环 (+15%)";
                activeDesc = $"已上阵 {counts[best3Faction]} 名【{fn}】英雄：全队生命、攻击、技能强度 +{Mathf.RoundToInt(bonusPct * 100f)}%";
                return;
            }

            if (pairsCount >= 2)
            {
                bonusPct = 0.18f;
                activeTitle = "双阵营共鸣 (+18%)";
                activeDesc = "两组各 2 名同阵营英雄联合作战：全队生命、攻击、技能强度 +18%";
                return;
            }

            if (bestPairFaction >= 0)
            {
                string fn = GetFactionName((HeroFaction)bestPairFaction);
                bonusPct = 0.10f;
                activeTitle = $"{fn}协同 (+10%)";
                activeDesc = $"已上阵 2 名【{fn}】英雄：全队生命、攻击、技能强度 +10%";
                return;
            }

            bonusPct = 0f;
            activeTitle = "阵营光环未激活";
            activeDesc = "上阵 2 名同阵营激活 +10% · 2+2 双阵营激活 +18% · ≥3 名同阵营激活 +15%~25%！";
        }

        public HeroComputedStats CalcHeroFullStats(int heroIdx, int slot0to8 = -1, bool includeSquadSynergy = false)
        {
            if (heroIdx < 0 || heroIdx >= Heroes.Count)
                return default;

            HeroSpec h = Heroes[heroIdx];
            int lv = Mathf.Max(1, h.level);
            float starMul = GetHeroStarMultiplier(h.star, h.starPetals);

            float baseHpScaled = h.baseHp * (1f + 0.065f * (lv - 1)) * starMul;
            float baseAtkScaled = h.baseAtk * (1f + 0.060f * (lv - 1)) * starMul;
            float baseSpScaled = h.baseSkillPower * (1f + 0.060f * (lv - 1)) * starMul;

            int armor = h.baseArmor + (lv - 1) * 3;
            int resist = h.baseResist + (lv - 1) * 2;
            float critRate = h.id == "asa" ? 0.14f
                : (h.id == "linyan" ? 0.12f
                : (h.id == "zhoudr" ? 0.10f : 0.08f));
            float critMult = 1.50f;
            float startRage = 0f;
            float thornsPct = 0f;
            float lifestealPct = 0f;
            float healShieldBonus = 0f;

            float hpPct = GetFormationGlobalBonusPct();
            float atkPct = hpPct;
            float spPct = hpPct;

            // 1. 主动绝技倍率随第 1 技能等级 (1..5) 成长
            int lv0 = Mathf.Clamp(Mathf.Max(1, h.skillLevels[0]), 1, 5) - 1;
            float activeMult;
            switch (h.id)
            {
                case "dachui": activeMult = new[] { 2.20f, 2.50f, 2.85f, 3.20f, 3.60f }[lv0]; break;
                case "xiaotang": activeMult = new[] { 2.80f, 3.15f, 3.50f, 3.90f, 4.40f }[lv0]; break;
                case "leimeng": activeMult = new[] { 2.60f, 2.95f, 3.30f, 3.70f, 4.20f }[lv0]; break;
                case "linyan": activeMult = new[] { 3.00f, 3.40f, 3.85f, 4.35f, 4.90f }[lv0]; break;
                case "tieshu": activeMult = new[] { 2.30f, 2.60f, 2.95f, 3.35f, 3.80f }[lv0]; break;
                case "qinghe": activeMult = new[] { 1.90f, 2.20f, 2.50f, 2.80f, 3.10f }[lv0]; break;
                case "asa": activeMult = new[] { 2.40f, 2.75f, 3.10f, 3.45f, 3.80f }[lv0]; break;
                case "zhoudr": activeMult = new[] { 1.15f, 1.32f, 1.50f, 1.68f, 1.85f }[lv0]; break;
                default: activeMult = new[] { 1.60f, 1.85f, 2.10f, 2.40f, 2.75f }[lv0]; break;
            }

            // 2. 1★ 第二技能被动属性加成 & 2★ 大锤反伤基础
            if (h.star >= 1)
            {
                int lv1 = Mathf.Clamp(Mathf.Max(1, h.skillLevels[1]), 1, 5) - 1;
                switch (h.id)
                {
                    case "dachui": hpPct += 0.12f + 0.06f * lv1; armor += 80; break;
                    case "xiaotang": spPct += 0.12f + 0.06f * lv1; break;
                    case "leimeng": atkPct += 0.10f + 0.05f * lv1; hpPct += 0.10f + 0.05f * lv1; break;
                    case "linyan": atkPct += 0.12f + 0.05f * lv1; critRate += 0.08f; break;
                    case "tieshu": atkPct += 0.10f + 0.05f * lv1; armor += 60; break;
                    case "qinghe":
                    {
                        float qPct = 0.12f + 0.05f * lv1;
                        spPct += qPct;
                        resist = Mathf.RoundToInt(resist * (1f + qPct)) + 30;
                        break;
                    }
                    case "asa":
                        critRate += 0.08f + 0.03f * lv1;
                        atkPct += 0.08f + 0.04f * lv1;
                        break;
                    case "zhoudr":
                        spPct += 0.12f + 0.05f * lv1;
                        critRate += 0.06f + 0.03f * lv1;
                        break;
                    default: spPct += 0.10f + 0.05f * lv1; hpPct += 0.10f + 0.05f * lv1; break;
                }
            }
            if (h.id == "dachui" && h.star >= 2)
                thornsPct += 0.12f;

            // 3. 4 部位工坊装备固定加成
            int wt = Mathf.Clamp(h.equipTiers[0], 0, 4);
            int at = Mathf.Clamp(h.equipTiers[1], 0, 4);
            int bt = Mathf.Clamp(h.equipTiers[2], 0, 4);
            int gt = Mathf.Clamp(h.equipTiers[3], 0, 4);

            int flatAtk = EquipWeaponAtk[wt];
            int flatSp = EquipWeaponSp[wt] + EquipGogglesSp[gt];
            int flatHp = EquipArmorHp[at] + EquipBootsHp[bt];
            armor += EquipArmorDef[at];
            resist += EquipBootsRes[bt];
            critRate += EquipGogglesCrit[gt];

            // 4. 3 槽战术徽章被动加成
            for (int b = 0; b < h.equippedBadges.Length; b++)
            {
                if (!IsBadgeSlotUnlocked(h, b)) continue;
                switch (h.equippedBadges[b])
                {
                    case TacticalBadgeId.EnergyCore:
                        startRage += 40f;
                        spPct += 0.10f;
                        break;
                    case TacticalBadgeId.ThornShield:
                        armor += 120;
                        thornsPct += 0.22f;
                        break;
                    case TacticalBadgeId.EagleScope:
                        critRate += 0.15f;
                        atkPct += 0.12f;
                        break;
                    case TacticalBadgeId.PyroPowder:
                        spPct += 0.22f;
                        activeMult *= 1.18f;
                        break;
                    case TacticalBadgeId.BloodFury:
                        lifestealPct += 0.12f;
                        atkPct += 0.08f;
                        break;
                    case TacticalBadgeId.MedicCross:
                        healShieldBonus += 0.28f;
                        resist += 90;
                        break;
                }
            }

            // 5. 上阵阵营光环 + 阵位加成
            if (includeSquadSynergy)
            {
                GetFactionAuraState(out _, out _, out float facBonus, out _, out _, out _);
                hpPct += facBonus;
                atkPct += facBonus;
                spPct += facBonus;

                int resolvedSlot = slot0to8 >= 0 ? slot0to8 : FindHeroSlot(heroIdx);
                if (resolvedSlot >= 0 && resolvedSlot < 9)
                {
                    SlotStatBuff sb = GetFormationSlotBuff(SelectedFormation, resolvedSlot);
                    hpPct += sb.hpBonus;
                    atkPct += sb.atkBonus;
                    armor += sb.armorBonus;
                    resist += sb.resistBonus;
                    critMult += sb.critBonus;
                    if (sb.startRage > 0f)
                        startRage += sb.startRage > 1.0f ? sb.startRage : sb.startRage * 100f;
                }
            }

            int finalHp = Mathf.Max(100, Mathf.RoundToInt((baseHpScaled + flatHp) * (1f + hpPct)));
            int finalAtk = Mathf.Max(10, Mathf.RoundToInt((baseAtkScaled + flatAtk) * (1f + atkPct)));
            int finalSp = Mathf.Max(10, Mathf.RoundToInt((baseSpScaled + flatSp) * (1f + spPct)));
            int finalRange = Mathf.Clamp(h.rangeCells, 1, 5);
            startRage = Mathf.Clamp(startRage, 0f, 100f);
            critRate = Mathf.Clamp(critRate, 0f, 0.85f);

            int skillLevelBonusSteps = 0;
            for (int s = 0; s < 4; s++)
            {
                if (h.star >= s)
                    skillLevelBonusSteps += Mathf.Max(0, h.skillLevels[s] - 1);
            }
            int badgeCount = 0;
            for (int b = 0; b < h.equippedBadges.Length; b++)
                if (IsBadgeSlotUnlocked(h, b) && h.equippedBadges[b] != TacticalBadgeId.None) badgeCount++;

            float rawPower = finalHp * 0.45f + finalAtk * 4.2f + finalSp * 3.6f + (armor + resist) * 1.5f;
            float skillAndBadgeMult = 1f + 0.08f * skillLevelBonusSteps + 0.06f * badgeCount;
            int finalPower = Mathf.RoundToInt(rawPower * skillAndBadgeMult);

            return new HeroComputedStats
            {
                maxHp = finalHp,
                atk = finalAtk,
                skillPower = finalSp,
                range = finalRange,
                armor = armor,
                resistance = resist,
                critRate = critRate,
                critMult = critMult,
                startRage = startRage,
                thornsPct = thornsPct,
                lifestealPct = lifestealPct,
                healShieldBonus = healShieldBonus,
                activeSkillMult = activeMult,
                combatPower = finalPower,
            };
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
            HeroSpec h = Heroes[heroIdx];
            HeroCombatRole role = GetHeroCombatRole(heroIdx);
            int[] prefOrder;
            if (h.rangeCells <= 1 || role == HeroCombatRole.Guardian || (role == HeroCombatRole.Vanguard && h.id != "asa"))
                prefOrder = new[] { 5, 2, 8, 4, 1, 7, 3, 0, 6 }; // col=2 (row 1,0,2) 前排优先
            else if (h.id == "asa")
                prefOrder = new[] { 4, 5, 1, 2, 7, 8, 3, 0, 6 }; // 前中排突袭 (col=1/2)
            else if (h.rangeCells <= 4 || role == HeroCombatRole.Demolition || role == HeroCombatRole.Gunsmith)
                prefOrder = new[] { 4, 1, 7, 3, 0, 6, 5, 2, 8 }; // col=1 (row 1,0,2) 中排优先
            else
                prefOrder = new[] { 3, 0, 6, 4, 1, 7, 5, 2, 8 }; // col=0 (row 1,0,2) 后排优先

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
                    case "qinghe": return HeroCombatRole.Ranger;
                    case "asa": return HeroCombatRole.Vanguard;
                    case "zhoudr": return HeroCombatRole.Demolition;
                }
            }
            switch (heroIdx)
            {
                case 0: return HeroCombatRole.Guardian;
                case 1: return HeroCombatRole.Demolition;
                case 2: return HeroCombatRole.Vanguard;
                case 3: return HeroCombatRole.Ranger;
                case 4: return HeroCombatRole.Gunsmith;
                case 6: return HeroCombatRole.Ranger;
                case 7: return HeroCombatRole.Vanguard;
                case 8: return HeroCombatRole.Demolition;
                default: return HeroCombatRole.Medic;
            }
        }

        public string GetHeroRoleName(int heroIdx)
        {
            if (heroIdx >= 0 && heroIdx < Heroes.Count)
            {
                switch (Heroes[heroIdx].id)
                {
                    case "qinghe": return "寒霜猎手";
                    case "asa": return "飞爪猎袭";
                    case "zhoudr": return "电磁学者";
                }
            }
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
            if (_unlockedFormations.Contains(fmt) || _gmUnlockFullSquad) return true;
            switch (fmt)
            {
                case TacticalFormationId.StandardLine: return true;
                case TacticalFormationId.SpearAssault: return HordeWaveCleared >= 4;
                case TacticalFormationId.ResistBarrier: return HordeWaveCleared >= 10;
                case TacticalFormationId.IronBastion: return HordeWaveCleared >= 16;
                case TacticalFormationId.VolleyFire: return HordeWaveCleared >= 20;
                case TacticalFormationId.EnergySurge: return HordeWaveCleared >= 24;
                default: return false;
            }
        }

        public string GetFormationIconKey(TacticalFormationId fmt)
        {
            switch (fmt)
            {
                case TacticalFormationId.SpearAssault: return "ico_fmt_spear";
                case TacticalFormationId.ResistBarrier: return "ico_fmt_resist";
                case TacticalFormationId.IronBastion: return "ico_fmt_bastion";
                case TacticalFormationId.VolleyFire: return "ico_fmt_energy";
                case TacticalFormationId.EnergySurge: return "ico_fmt_energy";
                default: return "ico_fmt_default";
            }
        }

        public float GetFormationCollectBonusFor(TacticalFormationId fmt)
        {
            switch (fmt)
            {
                case TacticalFormationId.SpearAssault: return 0.02f;
                case TacticalFormationId.ResistBarrier: return 0.02f;
                case TacticalFormationId.IronBastion: return 0.03f;
                case TacticalFormationId.VolleyFire: return 0.03f;
                case TacticalFormationId.EnergySurge: return 0.05f;
                default: return 0f;
            }
        }

        public float GetFormationGlobalBonusPct()
        {
            float sum = 0f;
            foreach (TacticalFormationId fmt in Enum.GetValues(typeof(TacticalFormationId)))
            {
                if (IsFormationUnlocked(fmt))
                    sum += GetFormationCollectBonusFor(fmt);
            }
            return sum;
        }

        public string GetFormationName(TacticalFormationId fmt)
        {
            switch (fmt)
            {
                case TacticalFormationId.StandardLine: return "默认防线阵";
                case TacticalFormationId.SpearAssault: return "尖锥突击阵";
                case TacticalFormationId.ResistBarrier: return "抗性增强阵";
                case TacticalFormationId.IronBastion: return "铁桶护甲阵";
                case TacticalFormationId.VolleyFire: return "火力齐射阵";
                case TacticalFormationId.EnergySurge: return "能量激涌阵";
                default: return "默认防线阵";
            }
        }

        public string GetFormationSummary(TacticalFormationId fmt)
        {
            switch (fmt)
            {
                case TacticalFormationId.StandardLine:
                    return "前排护甲+80/生命+15% · 中后排攻击+10%";
                case TacticalFormationId.SpearAssault:
                    return "收集全员+2% · 前中格减伤+25% · 中排暴伤+25%/攻击+15%";
                case TacticalFormationId.ResistBarrier:
                    return "收集全员+2% · 上阵全队抗性+120/生命+10%~15%（克制酸毒与首领冲击）";
                case TacticalFormationId.IronBastion:
                    return "收集全员+3% · 前排护甲+180/减伤+20% · 后排生命+12%";
                case TacticalFormationId.VolleyFire:
                    return "收集全员+3% · 中后排攻速+25%/攻击+12% · 前排生命+12%";
                case TacticalFormationId.EnergySurge:
                    return "收集全员+5% · 中后排开场怒气+40（抢先手秒开绝技！）";
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
                    return IsFormationUnlocked(fmt) ? "已收集（全员三维+2%）" : "通关第 4 关掉落【尖锥突击阵】图谱";
                case TacticalFormationId.ResistBarrier:
                    return IsFormationUnlocked(fmt) ? "已收集（全员三维+2%）" : "通关第 10 关掉落【抗性增强阵】图谱";
                case TacticalFormationId.IronBastion:
                    return IsFormationUnlocked(fmt) ? "已收集（全员三维+3%）" : "通关第 16 关掉落【铁桶护甲阵】图谱";
                case TacticalFormationId.VolleyFire:
                    return IsFormationUnlocked(fmt) ? "已收集（全员三维+3%）" : "通关第 20 关掉落【火力齐射阵】图谱";
                case TacticalFormationId.EnergySurge:
                    return IsFormationUnlocked(fmt) ? "已收集（全员三维+5%）" : "通关第 24 关掉落【能量激涌阵】图谱";
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
                            armorBonus = 60,
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

                case TacticalFormationId.ResistBarrier:
                    return new SlotStatBuff
                    {
                        shortTag = "抗性+120",
                        highlighted = true,
                        resistBonus = 120,
                        hpBonus = col == 2 ? 0.15f : 0.10f,
                        dmgReduction = 0.10f,
                    };

                case TacticalFormationId.IronBastion:
                    if (col == 2)
                    {
                        return new SlotStatBuff
                        {
                            shortTag = "护甲+180",
                            highlighted = true,
                            armorBonus = 180,
                            dmgReduction = 0.20f,
                            hpBonus = 0.20f,
                        };
                    }
                    return new SlotStatBuff
                    {
                        shortTag = "生命+12%",
                        highlighted = false,
                        hpBonus = 0.12f,
                        resistBonus = 50,
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
                            atkBonus = 0.12f,
                        };
                    }
                    return new SlotStatBuff
                    {
                        shortTag = "生命+12%",
                        highlighted = false,
                        hpBonus = 0.12f,
                        armorBonus = 60,
                    };

                case TacticalFormationId.EnergySurge:
                    if (col <= 1)
                    {
                        return new SlotStatBuff
                        {
                            shortTag = "怒气+40",
                            highlighted = true,
                            startRage = 40f,
                            atkBonus = 0.10f,
                        };
                    }
                    return new SlotStatBuff
                    {
                        shortTag = "护甲+90",
                        highlighted = false,
                        armorBonus = 90,
                        hpBonus = 0.15f,
                    };

                default: // StandardLine
                    if (col == 2)
                    {
                        return new SlotStatBuff
                        {
                            shortTag = "护甲+80",
                            highlighted = true,
                            armorBonus = 80,
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

            // 先放近战前排 (col=2: row 1 -> 0 -> 2)，再放中前排突袭/枪匠/控场 (range 2..4 -> col=1)，最后远程后排 (range 5 -> col=0)
            for (int pass = 0; pass < 3; pass++)
            {
                for (int i = 0; i < selected.Count; i++)
                {
                    int hIdx = selected[i];
                    int rng = Heroes[hIdx].rangeCells;
                    bool isMeleePass = IsHeroMelee(hIdx) || rng <= 1;
                    bool isMidPass = !isMeleePass && rng <= 4;
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
            int resolvedSlot = slot0to8 >= 0 ? slot0to8 : FindHeroSlot(heroIdx);
            return CalcHeroFullStats(heroIdx, resolvedSlot, resolvedSlot >= 0).combatPower;
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
                case 1: recPower = 1320; break;
                case 2: recPower = 1650; break;
                case 3: recPower = 2120; break;
                case 4: recPower = 2680; break;
                case 5: recPower = 3350; break;
                default:
                {
                    float raw = 3350f + (s - 5) * 620f + Mathf.Pow(s - 5, 1.72f) * 62f;
                    if (isBoss) raw *= 1.08f;
                    recPower = Mathf.RoundToInt(raw / 10f) * 10;
                    break;
                }
            }

            int enemyCnt = s == 1 ? 6 : (s == 2 ? 8 : (s == 3 ? 9 : Mathf.Clamp(8 + s / 2 + (isBoss ? 2 : 0), 8, 32)));
            int rwXp = (isBoss ? 30 : 18) + s * 3;
            int rwCoins = (isBoss ? 180 : 110) + s * 35;
            int rwMedals = (isBoss ? 2 : 1) + s / 6;
            int rwBp = isBoss ? (2 + s / 8) : (1 + s / 14);

            int rwHeroExp;
            if (s <= 4) rwHeroExp = 320 + (s - 1) * 190;
            else if (s <= 10) rwHeroExp = 1200 + (s - 5) * 450;
            else if (s <= 20) rwHeroExp = 4200 + (s - 11) * 860;
            else rwHeroExp = 15000 + (s - 21) * 2600;
            if (isBoss) rwHeroExp = Mathf.RoundToInt(rwHeroExp * 1.15f);

            int rwTickets = isBoss ? (s >= 10 ? 3 : 2) : 1;
            int rwBooks = s <= 4 ? (2 + s / 2) : (s <= 10 ? (4 + (s - 4)) : (8 + (s - 10) * 3 / 2));

            int unlockHero = -1;
            if (s == 2) unlockHero = 2;       // 雷猛 (铁卫团·史诗)
            else if (s == 3) unlockHero = 4;  // 铁叔 (废土科工·稀有 + 碎片×10)
            else if (s == 5) unlockHero = 3;  // 林砚 (山林盟·传说)
            else if (s == 8) unlockHero = 6;  // 青禾 (山林盟·史诗)
            else if (s == 12) unlockHero = 5; // 老陈头 (山林盟·史诗)
            else if (s == 15) unlockHero = 7; // 阿飒 (铁卫团·传说)
            else if (s == 20) unlockHero = 8; // 周博士 (废土科工·传说)

            int shardHero;
            if (unlockHero >= 0 && unlockHero < Heroes.Count)
            {
                shardHero = unlockHero;
            }
            else if (Heroes.Count >= 9)
            {
                // 按稀有度权重覆盖全部 9 名英雄 (0..8)：铁叔(稀有)高频，小棠/雷猛/老陈头/青禾(史诗)中频，大锤/林砚/阿飒/周博士(传说)首领及精英关精选
                int[] bossPool = { 0, 3, 6, 7, 8, 1, 2, 5 };
                int[] stageWeighted9 = { 4, 1, 2, 6, 0, 4, 5, 3, 4, 7, 1, 6, 8, 2, 4, 5, 0, 3, 7, 8 };
                shardHero = isBoss
                    ? bossPool[(s / 5) % bossPool.Length]
                    : stageWeighted9[(s - 1) % stageWeighted9.Length];
            }
            else
            {
                shardHero = (s - 1) % Mathf.Max(1, Heroes.Count);
            }
            int shardCnt = s == 3 ? 10 : (isBoss ? (4 + s / 8) : (2 + s / 10));

            int unlockFmt = -1;
            if (s == 4) unlockFmt = (int)TacticalFormationId.SpearAssault;
            else if (s == 10) unlockFmt = (int)TacticalFormationId.ResistBarrier;
            else if (s == 16) unlockFmt = (int)TacticalFormationId.IronBastion;
            else if (s == 20) unlockFmt = (int)TacticalFormationId.VolleyFire;
            else if (s == 24) unlockFmt = (int)TacticalFormationId.EnergySurge;

            string milestone = "";
            if (HordeWaveCleared < s)
            {
                if (unlockHero >= 0 && unlockHero < Heroes.Count && !Heroes[unlockHero].unlocked)
                    milestone = $"首通招募：{Heroes[unlockHero].name}（{GetFactionName(Heroes[unlockHero].faction)}·{Heroes[unlockHero].RarityTag}）";
                else if (unlockFmt >= 0)
                    milestone = $"首通解锁图谱：【{GetFormationName((TacticalFormationId)unlockFmt)}】" + (s == 10 ? " + 先手电芯徽章" : "");
                else if (isBoss)
                    milestone = $"首领里程碑：招募卡×{rwTickets} · 战术徽章 · 万能碎片";
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
                rewardHeroExp = rwHeroExp,
                rewardRecruitTickets = rwTickets,
                rewardSkillBooks = rwBooks,
                rewardShardHeroIdx = shardHero,
                rewardShardCount = shardCnt,
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
            if (spec.rewardHeroExp > 0)
                Add(SurvRes.HeroExp, spec.rewardHeroExp, notifyToast: true);
            if (spec.rewardRecruitTickets > 0)
                Add(SurvRes.RecruitTicket, spec.rewardRecruitTickets, notifyToast: true);
            if (spec.rewardSkillBooks > 0)
                Add(SurvRes.SkillBook, spec.rewardSkillBooks, notifyToast: true);
            if (spec.rewardBlueprints > 0)
                Add(SurvRes.Blueprint, spec.rewardBlueprints, notifyToast: false);
            if (spec.rewardMedals > 0)
                Add(SurvRes.HeroMedal, spec.rewardMedals, notifyToast: false);
            Add(SurvRes.Scrap, 5 + s * 2, notifyToast: false);

            string shardExtra = "";
            if (spec.rewardShardHeroIdx >= 0 && spec.rewardShardHeroIdx < Heroes.Count && spec.rewardShardCount > 0)
            {
                HeroSpec shHero = Heroes[spec.rewardShardHeroIdx];
                shHero.shards += spec.rewardShardCount;
                shardExtra = $" · {shHero.name}碎片+{spec.rewardShardCount}";
            }

            string extra = "";
            if (firstClear)
            {
                if (s == 3)
                {
                    AddBadge(TacticalBadgeId.ThornShield, 1);
                    extra += "\n★ 首通战利品：【路障荆棘徽章】×1！";
                }
                else if (s == 5)
                {
                    AddBadge(TacticalBadgeId.EagleScope, 1);
                    Add(SurvRes.ShardUnivLeg, 3, notifyToast: true);
                    extra += "\n★ 首领大奖：【鹰眼狙击徽章】×1 · 传说万能碎片+3！";
                }
                else if (s == 10)
                {
                    AddBadge(TacticalBadgeId.EnergyCore, 1);
                    AddBadge(TacticalBadgeId.PyroPowder, 1);
                    Add(SurvRes.ShardUnivEpic, 5, notifyToast: true);
                    extra += "\n★ 首领大奖：【先手电芯徽章】+【高爆火药徽章】 · 史诗万能碎片+5！";
                }
                else if (s == 15)
                {
                    AddBadge(TacticalBadgeId.BloodFury, 1);
                    Add(SurvRes.ShardUnivLeg, 5, notifyToast: true);
                    extra += "\n★ 首领大奖：【狂战士血怒徽章】×1 · 传说万能碎片+5！";
                }
                else if (s == 20)
                {
                    AddBadge(TacticalBadgeId.MedicCross, 1);
                    Add(SurvRes.ShardUnivLeg, 10, notifyToast: true);
                    extra += "\n★ 首领大奖：【白衣圣手徽章】×1 · 传说万能碎片+10！";
                }
                else if (spec.isBossStage && s > 20)
                {
                    TacticalBadgeId rotBadge = (TacticalBadgeId)(1 + ((s / 5) % 6));
                    AddBadge(rotBadge, 1);
                    Add(SurvRes.ShardUnivLeg, 10, notifyToast: true);
                    Add(SurvRes.ShardUnivEpic, 6, notifyToast: false);
                    extra += $"\n★ 首领大奖：【{GetBadgeName(rotBadge)}】×1 · 传说万能碎片+10！";
                }

                if (spec.isBossStage)
                    Add(SurvRes.ChoiceResBox, 1, notifyToast: false);
            }

            HordeWaveCleared = Mathf.Max(HordeWaveCleared, s);
            HordeRepelled = true;
            HordePosterShown = false;
            HordeTimerSec = 1799f;

            AddXp(spec.rewardXp, new Vector2(15.2f, 11.4f));

            if (spec.unlockHeroIdx >= 0 && spec.unlockHeroIdx < Heroes.Count)
            {
                HeroSpec uh = Heroes[spec.unlockHeroIdx];
                bool wasLocked = !uh.unlocked;
                UnlockHero(uh.id, silent: false);
                if (wasLocked)
                    extra += $"\n★ 新英雄入队：{uh.name}（{GetFactionName(uh.faction)}·{GetHeroRoleName(spec.unlockHeroIdx)}）！";
            }
            if (firstClear && spec.unlockFormationIdx >= 0)
            {
                var fmt = (TacticalFormationId)spec.unlockFormationIdx;
                _unlockedFormations.Add(fmt);
                extra += $"\n★ 新阵型图谱激活：【{GetFormationName(fmt)}】（全员三维+{Mathf.RoundToInt(GetFormationCollectBonusFor(fmt) * 100f)}%）！";
            }

            EvaluateQuestProgress();
            return $"英雄经验 +{spec.rewardHeroExp} · 招募卡 +{spec.rewardRecruitTickets} · 技能书 +{spec.rewardSkillBooks}{shardExtra} · 铜币 +{spec.rewardCoins}{extra}";
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
            BattleStageSpec sp = GetBattleStageSpec(HordeWaveCleared);
            int heroExp = Mathf.Max(240, Mathf.RoundToInt(sp.rewardHeroExp * 0.75f));
            int coins = 110 + HordeWaveCleared * 35;
            int books = Mathf.Max(2, sp.rewardSkillBooks / 2);
            Coins += coins;
            Add(SurvRes.HeroExp, heroExp, notifyToast: true);
            Add(SurvRes.SkillBook, books, notifyToast: true);
            AddXp(18, new Vector2(14.6f, 11.8f));
            msg = $"巡山扫荡第 {HordeWaveCleared} 关成功！英雄经验+{heroExp} · 技能书+{books} · 铜币+{coins}";
            return true;
        }

        // ---------------- 哨卡巡逻挂机补给箱 & 广播电台招募 & 功能消耗品 ----------------
        public void GetPatrolPendingRewards(out int heroExp, out int coins, out int skillBooks, out int minutesAccum)
        {
            minutesAccum = Mathf.Clamp(Mathf.FloorToInt(PatrolAccumSec / 60f), 0, 720);
            int s = Mathf.Max(1, HordeWaveCleared);
            heroExp = minutesAccum * (18 + 12 * s);
            coins = minutesAccum * (8 + 5 * s);
            skillBooks = minutesAccum >= 15
                ? (minutesAccum / 15) * (1 + s / 10)
                : (minutesAccum >= 5 ? 1 : 0);
        }

        public bool TryClaimPatrolChest(out int heroExp, out int coins, out int skillBooks, out string summary)
        {
            GetPatrolPendingRewards(out heroExp, out coins, out skillBooks, out int mins);
            if (mins < 1 || heroExp <= 0)
            {
                heroExp = 0;
                coins = 0;
                skillBooks = 0;
                summary = "哨卡巡逻累积尚不足 1 分钟，稍后再来领取吧！";
                return false;
            }

            PatrolAccumSec = Mathf.Max(0f, PatrolAccumSec - mins * 60f);
            Add(SurvRes.HeroExp, heroExp, notifyToast: true);
            Coins += coins;
            if (skillBooks > 0)
                Add(SurvRes.SkillBook, skillBooks, notifyToast: true);

            summary = $"领取哨卡巡逻 {mins} 分钟收益：英雄经验+{heroExp} · 铜币+{coins}"
                    + (skillBooks > 0 ? $" · 技能书+{skillBooks}" : "");
            return true;
        }

        public int GetRecruitPullsUntilGuaranteed()
            => Mathf.Clamp(10 - (RecruitPityCounter % 10), 1, 10);

        int PickWeightedHeroIndexForGacha(List<int> subset, bool highRarityBias)
        {
            int count = subset != null ? subset.Count : Heroes.Count;
            if (count <= 0) return 0;

            int totalWeight = 0;
            for (int i = 0; i < count; i++)
            {
                int hIdx = subset != null ? subset[i] : i;
                HeroRarity rar = Heroes[Mathf.Clamp(hIdx, 0, Heroes.Count - 1)].rarity;
                int w = highRarityBias
                    ? (rar == HeroRarity.Legendary ? 3 : (rar == HeroRarity.Epic ? 3 : 2))
                    : (rar == HeroRarity.Rare ? 5 : (rar == HeroRarity.Epic ? 3 : 2));
                totalWeight += w;
            }

            int pick = _ecoRng.Next(Mathf.Max(1, totalWeight));
            int acc = 0;
            for (int i = 0; i < count; i++)
            {
                int hIdx = subset != null ? subset[i] : i;
                HeroRarity rar = Heroes[Mathf.Clamp(hIdx, 0, Heroes.Count - 1)].rarity;
                int w = highRarityBias
                    ? (rar == HeroRarity.Legendary ? 3 : (rar == HeroRarity.Epic ? 3 : 2))
                    : (rar == HeroRarity.Rare ? 5 : (rar == HeroRarity.Epic ? 3 : 2));
                acc += w;
                if (pick < acc)
                    return hIdx;
            }
            return subset != null ? subset[count - 1] : (Heroes.Count - 1);
        }

        public List<GachaPullResult> PerformRadioRecruit(int pullCount, out string errMsg)
        {
            int cnt = pullCount >= 10 ? 10 : Mathf.Max(1, pullCount);
            var results = new List<GachaPullResult>(cnt);

            int tickets = Get(SurvRes.RecruitTicket);
            if (tickets < cnt)
            {
                errMsg = $"电台招募卡不足 ({tickets}/{cnt})，可通过山口闯关、居民礼袋或行商兑换获取！";
                return results;
            }

            Add(SurvRes.RecruitTicket, -cnt);
            var newlyUnlockedHeroes = new List<HeroSpec>();

            for (int i = 0; i < cnt; i++)
            {
                TotalRecruitPulls++;
                RecruitPityCounter++;
                bool isPity10 = (RecruitPityCounter % 10 == 0);
                double roll = _ecoRng.NextDouble();

                if (isPity10 || roll < 0.16)
                {
                    // 保底整卡或高阶英雄大额碎片：涵盖全 9 名英雄并按稀有度加权，优先抽取尚未解锁英雄！
                    var lockedIndices = new List<int>();
                    for (int h = 0; h < Heroes.Count; h++)
                        if (!Heroes[h].unlocked) lockedIndices.Add(h);

                    int pickHeroIdx = (lockedIndices.Count > 0 && (isPity10 || _ecoRng.NextDouble() < 0.65))
                        ? PickWeightedHeroIndexForGacha(lockedIndices, highRarityBias: true)
                        : PickWeightedHeroIndexForGacha(null, highRarityBias: true);

                    HeroSpec hero = Heroes[Mathf.Clamp(pickHeroIdx, 0, Heroes.Count - 1)];
                    int rar05 = hero.rarity == HeroRarity.Legendary ? 4 : (hero.rarity == HeroRarity.Epic ? 3 : 2);

                    if (!hero.unlocked)
                    {
                        hero.unlocked = true;
                        hero.skillLevels[0] = Mathf.Max(1, hero.skillLevels[0]);
                        newlyUnlockedHeroes.Add(hero);
                        results.Add(new GachaPullResult
                        {
                            isFullHero = true,
                            isNewUnlock = true,
                            heroIdx = pickHeroIdx,
                            shardCount = 0,
                            bonusItem = SurvRes.HeroExp,
                            bonusCount = 0,
                            label = $"★整卡：{hero.name}",
                            iconKey = hero.portraitKey,
                            rarity0to5 = rar05,
                        });
                    }
                    else
                    {
                        int dupShards = 10;
                        hero.shards += dupShards;
                        results.Add(new GachaPullResult
                        {
                            isFullHero = true,
                            isNewUnlock = false,
                            heroIdx = pickHeroIdx,
                            shardCount = dupShards,
                            bonusItem = SurvRes.HeroExp,
                            bonusCount = 0,
                            label = $"{hero.name}碎片×{dupShards}",
                            iconKey = hero.ShardIconKey,
                            rarity0to5 = rar05,
                        });
                    }
                }
                else if (roll < 0.72)
                {
                    // 英雄专属拼图碎片 (涵盖全 9 名英雄按品质稀有度加权抽取，3~6片)
                    int pickHeroIdx = PickWeightedHeroIndexForGacha(null, highRarityBias: false);
                    HeroSpec hero = Heroes[pickHeroIdx];
                    int rar05 = hero.rarity == HeroRarity.Legendary ? 4 : (hero.rarity == HeroRarity.Epic ? 3 : 2);
                    int sc = hero.rarity == HeroRarity.Legendary ? _ecoRng.Next(3, 5) : _ecoRng.Next(4, 7);
                    hero.shards += sc;
                    results.Add(new GachaPullResult
                    {
                        isFullHero = false,
                        isNewUnlock = false,
                        heroIdx = pickHeroIdx,
                        shardCount = sc,
                        bonusItem = SurvRes.HeroExp,
                        bonusCount = 0,
                        label = $"{hero.name}碎片×{sc}",
                        iconKey = hero.ShardIconKey,
                        rarity0to5 = rar05,
                    });
                }
                else if (roll < 0.88)
                {
                    // 英雄经验晶簇或战术技能书
                    bool isBook = _ecoRng.NextDouble() < 0.5;
                    SurvRes bItem = isBook ? SurvRes.SkillBook : SurvRes.HeroExp;
                    int bCnt = isBook ? _ecoRng.Next(4, 7) : (_ecoRng.Next(26, 42) * 10);
                    Add(bItem, bCnt, notifyToast: false);
                    results.Add(new GachaPullResult
                    {
                        isFullHero = false,
                        isNewUnlock = false,
                        heroIdx = -1,
                        shardCount = 0,
                        bonusItem = bItem,
                        bonusCount = bCnt,
                        label = $"{ResName(bItem)}×{bCnt}",
                        iconKey = GetResIconKey(bItem),
                        rarity0to5 = isBook ? 2 : 2,
                    });
                }
                else
                {
                    // 史诗/传说万能碎片惊喜掉落
                    bool legUniv = _ecoRng.NextDouble() < 0.35;
                    SurvRes uItem = legUniv ? SurvRes.ShardUnivLeg : SurvRes.ShardUnivEpic;
                    int uCnt = legUniv ? 2 : 3;
                    Add(uItem, uCnt, notifyToast: false);
                    results.Add(new GachaPullResult
                    {
                        isFullHero = false,
                        isNewUnlock = false,
                        heroIdx = -1,
                        shardCount = 0,
                        bonusItem = uItem,
                        bonusCount = uCnt,
                        label = $"{ResName(uItem)}×{uCnt}",
                        iconKey = GetResIconKey(uItem),
                        rarity0to5 = legUniv ? 4 : 3,
                    });
                }
            }

            if (newlyUnlockedHeroes.Count > 0)
            {
                AutoAssignHeroes();
                EnsureCombatSquad();
                for (int u = 0; u < newlyUnlockedHeroes.Count; u++)
                    OnHeroUnlocked?.Invoke(newlyUnlockedHeroes[u]);
            }

            errMsg = null;
            return results;
        }

        public bool TryOpenChoiceResBox(SurvRes chosenRes, int count, out string msg)
        {
            if (chosenRes != SurvRes.Wood && chosenRes != SurvRes.Scrap &&
                chosenRes != SurvRes.Water && chosenRes != SurvRes.Cloth && chosenRes != SurvRes.Herb)
            {
                msg = "原料自选箱仅可兑换：原木、废铁、净水、粗麻布或山草药！";
                return false;
            }
            int haveBox = Get(SurvRes.ChoiceResBox);
            if (haveBox <= 0)
            {
                msg = "背包内暂无【原料自选箱】";
                return false;
            }
            int useCnt = Mathf.Clamp(Mathf.Max(1, count), 1, haveBox);
            if (Get(chosenRes) >= StorageCap)
            {
                msg = $"【{ResName(chosenRes)}】已达仓库储量上限({StorageCap})，请先消耗或升级物资仓库！";
                return false;
            }

            int rawGain = useCnt * 5;
            Add(SurvRes.ChoiceResBox, -useCnt);
            int before = Get(chosenRes);
            Add(chosenRes, rawGain, notifyToast: true);
            int actualGain = Mathf.Max(0, Get(chosenRes) - before);
            msg = $"开启【原料自选箱×{useCnt}】：获得 {ResName(chosenRes)} +{actualGain}！";
            return true;
        }

        public bool TryUseSpeedHourglass(out string msg)
        {
            if (Get(SurvRes.SpeedHourglass) <= 0)
            {
                msg = "背包内暂无【工坊加速沙漏】";
                return false;
            }

            const float skipSec = 300f;
            Add(SurvRes.SpeedHourglass, -1);

            int spedBuilds = 0;
            int spedCrafts = 0;
            int spedFarms = 0;
            int spedRanches = 0;

            for (int i = 0; i < Nodes.Count; i++)
            {
                CampNode n = Nodes[i];
                if (n.state == NodeState.Constructing)
                {
                    n.buildTimer += skipSec;
                    spedBuilds++;
                    if (n.buildTimer >= n.buildDuration)
                    {
                        n.buildTimer = n.buildDuration;
                        n.state = NodeState.ReadyToHammer;
                        OnNodeVisualChanged?.Invoke(n);
                    }
                }
                else if (n.state == NodeState.Active && n.queue.Count > 0)
                {
                    float rem = skipSec;
                    while (n.queue.Count > 0 && rem > 0.01f)
                    {
                        QueueItem q = n.queue[0];
                        float need = Mathf.Max(0f, q.duration - q.timer);
                        if (rem >= need)
                        {
                            rem -= need;
                            n.queue.RemoveAt(0);
                            DeliverRecipeOutput(n, q.recipe);
                            spedCrafts++;
                        }
                        else
                        {
                            q.timer += rem;
                            rem = 0f;
                            spedCrafts++;
                        }
                    }
                }
            }

            int fCap = GetFarmPlotCap();
            for (int i = 0; i < fCap && i < FarmPlots.Count; i++)
            {
                FarmPlotSlot sl = FarmPlots[i];
                if (sl.phase == FarmPhase.Growing)
                {
                    sl.timer += skipSec;
                    spedFarms++;
                    if (sl.timer >= sl.duration)
                        sl.phase = FarmPhase.Ripe;
                }
            }
            if (spedFarms > 0)
                SyncFarmVisualPhase();

            int rCap = GetRanchPenCap();
            for (int i = 0; i < rCap && i < RanchPens.Count; i++)
            {
                RanchPenSlot pen = RanchPens[i];
                if (pen.feeding)
                {
                    pen.timer += skipSec;
                    spedRanches++;
                    if (pen.timer >= pen.duration)
                    {
                        pen.feeding = false;
                        pen.readyToCollect = true;
                    }
                }
            }

            OrderRefreshCooldown = Mathf.Max(0f, OrderRefreshCooldown - skipSec);
            SweepCooldownSec = Mathf.Max(0f, SweepCooldownSec - skipSec);
            PatrolAccumSec = Mathf.Min(43200f, PatrolAccumSec + skipSec);
            EvaluateQuestProgress();

            int totalTargets = spedBuilds + spedCrafts + spedFarms + spedRanches;
            msg = totalTargets > 0
                ? $"使用【工坊加速沙漏】快进 5 分钟！加速建造{spedBuilds} · 工坊{spedCrafts} · 农牧{spedFarms + spedRanches}（并累积哨卡巡逻+5分钟）！"
                : "使用【工坊加速沙漏】快进 5 分钟！哨卡巡逻挂机收益已立即增加 5 分钟！";
            return true;
        }

        void InitHeroes()
        {
            Heroes.Clear();
            // 0: 大锤 (铁卫团 · 传说 · 前排重卫，初始解锁，0★1瓣 + 4片测试升瓣)
            Heroes.Add(new HeroSpec
            {
                id = "dachui", characterId = "dachui", name = "大锤", roleTitle = "铁壁重卫 · 嘲讽护盾",
                faction = HeroFaction.IronGuard, rarity = HeroRarity.Legendary,
                rangeCells = 1, baseHp = 420, baseAtk = 34, baseSkillPower = 38, baseArmor = 110, baseResist = 75,
                star = 0, starPetals = 1, shards = 4,
                isMelee = true, projKey = "proj_wrench",
                skillName = "路牌重盾震击", skillDesc = "展开220%技强路牌重盾并嘲讽聚怪，重锤猛击造成范围物理伤害！",
                prefPost1 = DelegationPostKind.BuildSpeed, prefPost2 = DelegationPostKind.TruckArmor,
                portraitKey = "bust_dachui", targetBuildingTitle = "建筑加速 / 皮卡重甲",
                buffDesc = "铁卫团 · 建造加速 · 重甲主盾",
                unlocked = true, assigned = true, skinTier = OutfitTier.Refugee,
            });
            // 1: 小棠 (废土科工 · 史诗 · 中后排爆破，初始解锁，0★1瓣 + 4片测试升瓣)
            Heroes.Add(new HeroSpec
            {
                id = "xiaotang", characterId = "xiaotang", name = "小棠", roleTitle = "爆破工匠 · 烈焰灼烧",
                faction = HeroFaction.WasteTech, rarity = HeroRarity.Epic,
                rangeCells = 4, baseHp = 230, baseAtk = 32, baseSkillPower = 46, baseArmor = 55, baseResist = 70,
                star = 0, starPetals = 1, shards = 4,
                isMelee = false, projKey = "proj_wrench",
                skillName = "高爆燃烧瓶", skillDesc = "投掷燃烧瓶铺设3.5秒烈焰火海，共造成280%技强火焰元素伤害！",
                prefPost1 = DelegationPostKind.WorkshopSpeed, prefPost2 = DelegationPostKind.BuildSpeed,
                portraitKey = "bust_xiaotang", targetBuildingTitle = "废铁锻造坊 / 基建加速",
                buffDesc = "废土科工 · 作坊提速 · 火焰破甲",
                unlocked = true, assigned = true, skinTier = OutfitTier.Refugee,
            });
            // 2: 雷猛 (铁卫团 · 史诗 · 前排狂斧先锋，第2关首通 / 电台招募 / 10碎片合成)
            Heroes.Add(new HeroSpec
            {
                id = "leimeng", characterId = "leimeng", name = "雷猛", roleTitle = "重装先锋 · 狂斧横扫",
                faction = HeroFaction.IronGuard, rarity = HeroRarity.Epic,
                rangeCells = 1, unlockStage = 2, baseHp = 330, baseAtk = 40, baseSkillPower = 42, baseArmor = 85, baseResist = 60,
                star = 0, starPetals = 0, shards = 2,
                isMelee = true, projKey = "proj_wrench",
                skillName = "狂风破障斩", skillDesc = "挥动消防破拆重斧旋风横扫，造成260%技强范围物理伤害并击退！",
                prefPost1 = DelegationPostKind.WorkshopSpeed, prefPost2 = DelegationPostKind.DefensePower,
                portraitKey = "bust_leimeng", targetBuildingTitle = "作坊提速 / 城防主将",
                buffDesc = "铁卫团 · 作坊提速 · 城防 (第2关/电台)",
                unlocked = false, assigned = false, skinTier = OutfitTier.Refugee,
            });
            // 3: 林砚 (山林盟 · 传说升格 · 后排连弩主C，第5关首通 / 电台招募 / 10碎片合成)
            Heroes.Add(new HeroSpec
            {
                id = "linyan", characterId = "linyan", name = "林砚", roleTitle = "山林猎手 · 连弩主C",
                faction = HeroFaction.ForestPact, rarity = HeroRarity.Legendary,
                rangeCells = 5, unlockStage = 5, baseHp = 220, baseAtk = 45, baseSkillPower = 48, baseArmor = 50, baseResist = 65,
                star = 0, starPetals = 0, shards = 0,
                isMelee = false, projKey = "proj_arrow",
                skillName = "贯日散射箭雨", skillDesc = "向敌阵倾泻6波穿云连弩箭，总计造成300%技强贯穿物理伤害！",
                prefPost1 = DelegationPostKind.DefensePower, prefPost2 = DelegationPostKind.FarmYield,
                portraitKey = "bust_linyan", targetBuildingTitle = "城防哨塔 / 农牧巡林",
                buffDesc = "山林盟 · 哨塔火力 · 连弩穿透 (第5关/电台)",
                unlocked = false, assigned = false, skinTier = OutfitTier.Refugee,
            });
            // 4: 铁叔 (废土科工 · 稀有 · 中排霰弹压制，第3关首通 / 电台招募 / 10碎片合成)
            Heroes.Add(new HeroSpec
            {
                id = "tieshu", characterId = "tieshu", name = "铁叔", roleTitle = "火力枪匠 · 霰弹压制",
                faction = HeroFaction.WasteTech, rarity = HeroRarity.Rare,
                rangeCells = 3, unlockStage = 3, baseHp = 270, baseAtk = 33, baseSkillPower = 36, baseArmor = 75, baseResist = 55,
                star = 0, starPetals = 0, shards = 4,
                isMelee = false, projKey = "proj_buckshot",
                skillName = "双管高爆霰弹", skillDesc = "中排扇形倾泻高爆铁砂弹，造成230%技强物理伤害并强力击退尸潮！",
                prefPost1 = DelegationPostKind.TruckArmor, prefPost2 = DelegationPostKind.CaravanBonus,
                portraitKey = "bust_tieshu", targetBuildingTitle = "远征皮卡 / 行商护航",
                buffDesc = "废土科工 · 皮卡载重 · 近战压制 (第3关/电台)",
                unlocked = false, assigned = false, skinTier = OutfitTier.Refugee,
            });
            // 5: 老陈头 (山林盟 · 史诗升格 · 后排群疗神辅，第12关首通 / 电台招募 / 10碎片合成)
            Heroes.Add(new HeroSpec
            {
                id = "laochentou", characterId = "laochentou", name = "老陈头", roleTitle = "战地医官 · 急救群疗",
                faction = HeroFaction.ForestPact, rarity = HeroRarity.Epic,
                rangeCells = 5, unlockStage = 12, baseHp = 255, baseAtk = 26, baseSkillPower = 50, baseArmor = 60, baseResist = 85,
                star = 0, starPetals = 0, shards = 0,
                isMelee = false, projKey = "proj_fire_pool",
                skillName = "战地急救热汤", skillDesc = "为全队恢复160%技强生命值，并施加持续4秒的守护草本护盾！",
                prefPost1 = DelegationPostKind.FarmYield, prefPost2 = DelegationPostKind.CaravanBonus,
                portraitKey = "bust_laochentou", targetBuildingTitle = "农田丰收 / 行商议价",
                buffDesc = "山林盟 · 农田采收 · 全队群疗 (第12关/电台)",
                unlocked = false, assigned = false, skinTier = OutfitTier.Refugee,
            });
            // 6: 青禾 (山林盟 · 史诗 · 中后排寒霜控场，第8关首通 / 电台招募 / 10碎片合成)
            Heroes.Add(new HeroSpec
            {
                id = "qinghe", characterId = "qinghe", name = "青禾", roleTitle = "寒霜猎手 · 冰封控场",
                faction = HeroFaction.ForestPact, rarity = HeroRarity.Epic,
                rangeCells = 4, unlockStage = 8, baseHp = 245, baseAtk = 34, baseSkillPower = 48, baseArmor = 58, baseResist = 88,
                star = 0, starPetals = 0, shards = 0,
                isMelee = false, projKey = "proj_arrow",
                skillName = "霜寒捕兽夹阵", skillDesc = "投掷液氮寒霜瓶造成190%技强元素伤害，并冻结范围内尸群2.0秒！",
                prefPost1 = DelegationPostKind.FarmYield, prefPost2 = DelegationPostKind.DefensePower,
                portraitKey = "bust_qinghe", targetBuildingTitle = "农牧庄园 / 城防哨塔",
                buffDesc = "山林盟 · 农牧提速 · 寒霜冰封 (第8关/电台)",
                unlocked = false, assigned = false, skinTier = OutfitTier.Refugee,
            });
            // 7: 阿飒 (铁卫团 · 传说 · 前中排飞爪突袭，第15关首通 / 电台招募 / 10碎片合成)
            Heroes.Add(new HeroSpec
            {
                id = "asa", characterId = "asa", name = "阿飒", roleTitle = "飞爪猎袭 · 夺命钩锁",
                faction = HeroFaction.IronGuard, rarity = HeroRarity.Legendary,
                rangeCells = 2, unlockStage = 15, baseHp = 350, baseAtk = 48, baseSkillPower = 46, baseArmor = 90, baseResist = 68,
                star = 0, starPetals = 0, shards = 0,
                isMelee = false, projKey = "proj_wrench",
                skillName = "飞爪夺命钩", skillDesc = "甩出改装绞盘铁钩将敌方最远后排拉至身前重劈，造成240%技强物理伤害！",
                prefPost1 = DelegationPostKind.TruckArmor, prefPost2 = DelegationPostKind.DefensePower,
                portraitKey = "bust_asa", targetBuildingTitle = "远征皮卡 / 城防哨塔",
                buffDesc = "铁卫团 · 皮卡装甲 · 飞爪切后 (第15关/电台)",
                unlocked = false, assigned = false, skinTier = OutfitTier.Refugee,
            });
            // 8: 周博士 (废土科工 · 传说 · 后排连锁电弧主C，第20关首通 / 电台招募 / 10碎片合成)
            Heroes.Add(new HeroSpec
            {
                id = "zhoudr", characterId = "zhoudr", name = "周博士", roleTitle = "电磁学者 · 超载电弧",
                faction = HeroFaction.WasteTech, rarity = HeroRarity.Legendary,
                rangeCells = 5, unlockStage = 20, baseHp = 240, baseAtk = 42, baseSkillPower = 56, baseArmor = 55, baseResist = 92,
                star = 0, starPetals = 0, shards = 0,
                isMelee = false, projKey = "proj_wrench",
                skillName = "超载电磁风暴", skillDesc = "召唤高压电弧在敌群间弹射8次，每次造成115%技强元素伤害并麻痹！",
                prefPost1 = DelegationPostKind.WorkshopSpeed, prefPost2 = DelegationPostKind.BuildSpeed,
                portraitKey = "bust_zhoudr", targetBuildingTitle = "全工坊提速 / 工程建造",
                buffDesc = "废土科工 · 工坊超频 · 连锁电弧 (第20关/电台)",
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
                id = "radio_post", kind = NodeKind.RadioPost, state = NodeState.HiddenInForest, level = 0, maxLevel = 2, unlockPlayerLv = 3,
                hintLabel = "断线电台塔", ruinTitle = "短波广播·行商电台", activeTitle = "短波广播·行商电台",
                desc = "修复短波求生电台呼叫幸存者英雄加盟，并联络【山口行商驼队】高价收购皮衣、急救箱与熏肉，换取铜币、招募卡与工程手稿！",
                benefitLabel = "广播招募与行商贸易溢价", benefitFrom = 0, benefitTo = 100, benefitRes = SurvRes.RecruitTicket,
                gx = 26.4f, gy = 25.2f, occupyX = 25, occupyY = 24, occupyW = 2, occupyH = 2,
                revealChunkX = 8, revealChunkY = 8, buildDuration = 3.8f,
                buildCoinCost = 140, buildReqPop = 4,
                buildCosts = new[] { new CostPair(SurvRes.Plank, 6), new CostPair(SurvRes.Scrap, 4) },
                requiresSiteSalvage = true, siteUncovered = false, siteSalvageCleared = false,
                compassDir = "东北山巅", salvageLabel = "军用短波天线包", salvageBonusRes = SurvRes.RecruitTicket, salvageBonusAmt = 2,
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
                case SurvRes.Blueprint: targetNodeId = "relic_truck"; return "赴【丧尸城搜刮】、【山口闯关】、【山口行商】或【提升星级】获得";
                case SurvRes.HeroMedal: targetNodeId = "barricade"; return "点击底部【战斗】闯关击退山口尸潮，或居民5单礼袋获得";
                case SurvRes.HeroExp: targetNodeId = "barricade"; return "点击底部【战斗】山口闯关或开启【哨卡巡逻挂机箱】大量产出，唯一用于英雄升级！";
                case SurvRes.RecruitTicket: targetNodeId = "radio_post"; return "山口闯关、居民5单礼袋或山口行商兑换获得，在【短波广播站】抽取英雄与碎片！";
                case SurvRes.SkillBook: targetNodeId = "barricade"; return "山口闯关、哨卡巡逻挂机箱或山口行商兑换获得，用于提升英雄4大技能等级！";
                case SurvRes.ShardUnivEpic:
                case SurvRes.ShardUnivLeg: targetNodeId = "radio_post"; return "首领关首通、广播电台十连或居民礼袋获得，可 1:1 兑换对应品质任意英雄碎片！";
                case SurvRes.ChoiceResBox: return "在背包点击【使用】可自由任选：原木、废铁、净水、粗麻布或山草药！";
                case SurvRes.SpeedHourglass: return "在背包点击【使用】立即缩短所有进行中的建造、作坊生产与农牧倒计时 5 分钟！";
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
                Add(SurvRes.RecruitTicket, 1, notifyToast: true);
                Add(SurvRes.ChoiceResBox, 1, notifyToast: false);
                Add(SurvRes.HeroExp, 200 + CampLevel * 40, notifyToast: false);
                if (CampLevel >= 3) Add(SurvRes.Scrap, 6, notifyToast: false);
                if (CampLevel >= 6) Add(SurvRes.Parts, 1, notifyToast: true);
                OnWorldToast?.Invoke($"达成 5 单居民礼袋！铜币+{chestCoins} · 招募卡+1 · 自选箱+1 · 手稿+2！", survivorGrid);
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
            if (!RadioRepaired && CampLevel < 3)
            {
                msg = "请先升至 Lv.3 或修缮东北【短波广播·行商电台】联络商队！";
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

            if (dt > 0f)
                PatrolAccumSec = Mathf.Min(43200f, PatrolAccumSec + dt);

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
            int maxCap = Mathf.Max(MaxHeroLevelCap, 75);
            if (heroId == null)
            {
                int cnt = 0;
                for (int i = 0; i < Heroes.Count; i++)
                {
                    if (!Heroes[i].unlocked) continue;
                    HeroSpec h = Heroes[i];
                    int target = Mathf.Clamp(h.level + addLevels, 1, maxCap);
                    for (int lv = h.level; lv < target; lv++)
                        h.spentHeroExp += GetHeroLevelUpExpCost(lv);
                    h.level = target;
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
                int target = Mathf.Clamp(h.level + addLevels, 1, maxCap);
                for (int lv = h.level; lv < target; lv++)
                    h.spentHeroExp += GetHeroLevelUpExpCost(lv);
                h.level = target;
                if (addLevels > 0) HeroLevelUpsTotal++;
                OnDelegationChanged?.Invoke();
                EvaluateQuestProgress();
                OnWorldToast?.Invoke($"GM：{h.name} 等级已调至 Lv.{h.level}！", new Vector2(16.0f, 19.5f));
            }
        }

        public void GmAddAllHeroShards(int shardDelta)
        {
            if (shardDelta <= 0) return;
            for (int i = 0; i < Heroes.Count; i++)
                Heroes[i].shards += shardDelta;
            OnWorldToast?.Invoke($"GM：全部 {Heroes.Count} 名英雄专属碎片 +{shardDelta}！", new Vector2(16.0f, 19.5f));
        }

        public void GmGrantAllBadgesAndBoxes()
        {
            foreach (TacticalBadgeId b in Enum.GetValues(typeof(TacticalBadgeId)))
            {
                if (b != TacticalBadgeId.None)
                    AddBadge(b, 2);
            }
            Add(SurvRes.ChoiceResBox, 5, notifyToast: true);
            Add(SurvRes.SpeedHourglass, 5, notifyToast: true);
            OnWorldToast?.Invoke("GM：全 6 类战术徽章各+2 · 原料自选箱+5 · 加速沙漏+5！", new Vector2(16.0f, 19.5f));
        }

        public void GmUnlockAllHeroes()
        {
            for (int i = 0; i < Heroes.Count; i++)
                UnlockHero(Heroes[i].id, silent: true);
            AutoAssignHeroes();
            EnsureCombatSquad();
            if (Heroes.Count > 0)
                OnHeroUnlocked?.Invoke(Heroes[Heroes.Count - 1]);
            EvaluateQuestProgress();
            OnWorldToast?.Invoke($"GM：已解锁全部 {Heroes.Count} 名骨干英雄！", new Vector2(16.4f, 16.2f));
        }

        public void GmUnlockAllHeroesAndFormations()
        {
            _gmUnlockFullSquad = true;
            foreach (TacticalFormationId fmt in Enum.GetValues(typeof(TacticalFormationId)))
                _unlockedFormations.Add(fmt);
            foreach (TacticalBadgeId b in Enum.GetValues(typeof(TacticalBadgeId)))
            {
                if (b != TacticalBadgeId.None)
                    OwnedBadges[b] = Mathf.Max(GetOwnedBadgeCount(b), 2);
            }

            for (int i = 0; i < Heroes.Count; i++)
            {
                UnlockHero(Heroes[i].id, silent: true);
                HeroSpec h = Heroes[i];
                if (h.star < 3)
                {
                    h.star = 3;
                    h.starPetals = 2;
                }
                h.shards = Mathf.Max(h.shards, 30);
                for (int s = 0; s < 4; s++)
                    h.skillLevels[s] = Mathf.Max(1, h.skillLevels[s]);
            }
            Add(SurvRes.HeroExp, 20000, notifyToast: false);
            Add(SurvRes.SkillBook, 50, notifyToast: false);
            Add(SurvRes.RecruitTicket, 10, notifyToast: false);
            Add(SurvRes.ShardUnivEpic, 15, notifyToast: false);
            Add(SurvRes.ShardUnivLeg, 15, notifyToast: false);

            AutoDeployRecommendedFormation();
            AutoAssignHeroes();
            if (Heroes.Count > 0)
                OnHeroUnlocked?.Invoke(Heroes[Heroes.Count - 1]);
            EvaluateQuestProgress();
            OnWorldToast?.Invoke($"GM：已解锁全{Heroes.Count}英雄(3★觉醒)、6大阵型图谱、全徽章与2万英雄经验！", new Vector2(16.4f, 16.2f));
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
