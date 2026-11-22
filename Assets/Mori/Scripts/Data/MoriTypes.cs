using System;

namespace Mori
{
    /// <summary>
    /// 资源枚举：同时覆盖一期模拟经营+放置卡牌闭环，并预留二期大世界轻SLG战略资源槽位。
    /// </summary>
    public enum ResourceId
    {
        // 基础货币与经验
        Coin = 0,           // 瓶盖金币
        Gem = 1,            // 辐射蓝钻
        PlayerExp = 2,      // 领袖经验
        Blueprint = 3,      // 建造蓝图

        // 6种陆地基础生存原料（对应狩猎/拾荒车/地铁探洞）
        RawMeat = 10,       // 变异生肉排（捕猎哨塔）
        ScrapWood = 11,     // 废木料（荒野拾荒车）
        TornCanvas = 12,    // 破帆布（荒野拾荒车）
        WastePlastic = 13,  // 废塑料（荒野拾荒车）
        RustIron = 14,      // 锈废铁（地铁探洞井）
        OldChip = 15,       // 旧电子件（地铁探洞井）

        // 代表性工坊一阶/二阶加工品（最多两层跨厂依赖）
        SmokedRibs = 20,    // 炭烤熏肉排（熏肉灶）
        CannedStew = 21,    // 废土肉罐头（熏肉灶：生肉+锈铁）
        WastelandBurger = 22,// 末日巨无霸（熏肉灶：熏肉排+纯净水）
        CleanWater = 23,    // 纯净水（净水蒸馏站）
        AntiRadDrink = 24,  // 抗辐特饮（净水蒸馏站：纯净水+电子件）
        RecycledPlank = 25, // 再生厚木板（废料回收台）

        // 二期联盟与大世界轻SLG无损预留资源（一期由商队悬赏与遗迹远征小量产出）
        Merit = 40,         // 废土军功勋章（通兑传说万能碎片/蓝图）
        MutantSeed = 41,    // 变异作物种子（预留大地图绿洲）
        TechPoint = 42,     // 旧世科技点（预留个人联盟科技）
        DieselFuel = 43,    // 战车柴油（预留装甲车队出征耐久维修）
    }

    public enum BuildingKind
    {
        CoreHq,         // 指挥大巴（英雄委任中枢）
        Collector,      // 采集设施（捕猎哨塔/拾荒车/探洞井）
        Workshop,       // 加工工坊（熏肉灶/蒸馏站/回收台等）
        Housing,        // 民居（提供人口门槛值，不消耗）
        PopCapPublic,   // 公共设施（拉高人口上限）
        Functional,     // 功能建筑（广播站招募/公路贸易站/装甲车库）
    }

    public enum PlotState
    {
        Hidden = 0,         // 未达前置等级
        Outline = 1,        // 已达条件待建造（脚手架围栏）
        Constructing = 2,   // 建造倒计时中
        ReadyToTap = 3,     // 倒计时归零，等待玩家手动点击竣工爆经验
        Built = 4,          // 已竣工正常运转
    }

    public enum HeroClass { Tank, Warrior, Gunner, Mechanist }
    public enum HeroFaction { Brotherhood, TechInstitute, FreeRiders }
    public enum Rarity { Rare = 1, Epic = 2, Legendary = 3, Mythic = 4 }

    [Serializable]
    public sealed class PlotDef
    {
        public int plotId;
        public string buildingId;
        public string displayName;
        public string spriteKey;
        public BuildingKind kind;
        public int unlockPlayerLv;
        public int requirePrePlotId;
        public int requirePopulation;
        public int providePopulation;
        public int providePopCap;
        public float worldX;
        public float worldY;
        public float visualScale = 1f;
        public ResourceId costRes;
        public int costAmount;
        public int buildSeconds;
        public int finishExp;
    }

    [Serializable]
    public sealed class RecipeDef
    {
        public string recipeId;
        public string buildingId;
        public string displayName;
        public string iconKey;
        public Rarity quality;
        public int unlockBuildingLv;
        public int craftSeconds;
        public ResourceId outputRes;
        public int outputCount;
        public ResourceId inputA;
        public int countA;
        public ResourceId inputB;
        public int countB;
        public string sourceHintA;
        public string sourceHintB;
    }

    [Serializable]
    public sealed class HeroDef
    {
        public string heroId;
        public string displayName;
        public string titleTag;
        public string portraitKey;
        public Rarity rarity;
        public HeroClass heroClass;
        public HeroFaction faction;
        public int stars; // 0..25 小星（每5小星=1大星）
        public int baseHp;
        public int baseAtk;
        public string combatSkillName;
        public string combatSkillDesc;
        public string campBoostBuildingId;
        public string campBoostDesc;
        public int reduceProdSeconds;
    }

    [Serializable]
    public sealed class SurvivorOrder
    {
        public int slotIndex;
        public string npcName;
        public string moodNote;
        public ResourceId reqResA;
        public int reqCountA;
        public ResourceId reqResB;
        public int reqCountB;
        public int rewardExp;
        public int rewardCoin;
        public bool delivered;
    }
}
