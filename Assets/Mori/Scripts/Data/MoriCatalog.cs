using System.Collections.Generic;

namespace Mori
{
    /// <summary>
    /// 静态配置总表：一期首屏核心扇区建地坐标、最多2层工坊配方链、玩梗英雄与物品图标映射。
    /// 坐标可在 Unity Scene 视图通过 CampMapEditor 拖拽微调后回写。
    /// </summary>
    public static class MoriCatalog
    {
        public static readonly PlotDef[] Plots =
        {
            new PlotDef
            {
                plotId = 1, buildingId = "command_bus", displayName = "指挥大巴",
                spriteKey = "b_command_bus", kind = BuildingKind.CoreHq,
                unlockPlayerLv = 1, requirePrePlotId = 0,
                worldX = -0.15f, worldY = 0.85f, visualScale = 0.92f,
                costRes = ResourceId.ScrapWood, costAmount = 0, buildSeconds = 10, finishExp = 50,
            },
            new PlotDef
            {
                plotId = 2, buildingId = "smokehouse", displayName = "废土熏肉灶",
                spriteKey = "b_smokehouse", kind = BuildingKind.Workshop,
                unlockPlayerLv = 2, requirePrePlotId = 1, requirePopulation = 6,
                worldX = 1.92f, worldY = 0.18f, visualScale = 0.86f,
                costRes = ResourceId.ScrapWood, costAmount = 25, buildSeconds = 30, finishExp = 80,
            },
            new PlotDef
            {
                plotId = 3, buildingId = "hunt_tower", displayName = "捕猎哨塔",
                spriteKey = "b_hunt_tower", kind = BuildingKind.Collector,
                unlockPlayerLv = 1, requirePrePlotId = 0,
                worldX = -1.58f, worldY = 2.12f, visualScale = 0.82f,
                costRes = ResourceId.ScrapWood, costAmount = 15, buildSeconds = 15, finishExp = 45,
            },
            new PlotDef
            {
                plotId = 4, buildingId = "water_still", displayName = "净水蒸馏站",
                spriteKey = "b_water_still", kind = BuildingKind.Workshop,
                unlockPlayerLv = 3, requirePrePlotId = 1, requirePopulation = 9,
                worldX = -1.48f, worldY = -0.62f, visualScale = 0.82f,
                costRes = ResourceId.RustIron, costAmount = 20, buildSeconds = 45, finishExp = 90,
            },
            new PlotDef
            {
                plotId = 5, buildingId = "tent_house_1", displayName = "补丁帐篷屋",
                spriteKey = "b_tent_house", kind = BuildingKind.Housing,
                unlockPlayerLv = 2, requirePrePlotId = 1, providePopulation = 6,
                worldX = 1.68f, worldY = 1.95f, visualScale = 0.78f,
                costRes = ResourceId.ScrapWood, costAmount = 20, buildSeconds = 20, finishExp = 60,
            },
            new PlotDef
            {
                plotId = 6, buildingId = "recycle_bench", displayName = "废料回收台",
                spriteKey = "plot_scaffold", kind = BuildingKind.Workshop,
                unlockPlayerLv = 5, requirePrePlotId = 2, requirePopulation = 12,
                worldX = 1.45f, worldY = -1.62f, visualScale = 0.78f,
                costRes = ResourceId.Blueprint, costAmount = 5, buildSeconds = 90, finishExp = 120,
            },
        };

        public static readonly RecipeDef[] SmokehouseRecipes =
        {
            new RecipeDef
            {
                recipeId = "smoked_ribs", buildingId = "smokehouse",
                displayName = "炭烤熏肉排", iconKey = "ico_smoked_ribs", quality = Rarity.Rare,
                unlockBuildingLv = 1, craftSeconds = 45,
                outputRes = ResourceId.SmokedRibs, outputCount = 2,
                inputA = ResourceId.RawMeat, countA = 3,
                inputB = ResourceId.ScrapWood, countB = 2,
                sourceHintA = "捕猎哨塔采集", sourceHintB = "荒野拾荒车打捞",
            },
            new RecipeDef
            {
                recipeId = "wasteland_burger", buildingId = "smokehouse",
                displayName = "末日巨无霸", iconKey = "ico_smoked_ribs", quality = Rarity.Epic,
                unlockBuildingLv = 2, craftSeconds = 120,
                outputRes = ResourceId.WastelandBurger, outputCount = 1,
                inputA = ResourceId.SmokedRibs, countA = 2,
                inputB = ResourceId.CleanWater, countB = 2,
                sourceHintA = "熏肉灶一阶产出", sourceHintB = "净水蒸馏站产出",
            },
            new RecipeDef
            {
                recipeId = "canned_stew", buildingId = "smokehouse",
                displayName = "废土肉罐头", iconKey = "ico_raw_meat", quality = Rarity.Legendary,
                unlockBuildingLv = 3, craftSeconds = 240,
                outputRes = ResourceId.CannedStew, outputCount = 1,
                inputA = ResourceId.RawMeat, countA = 6,
                inputB = ResourceId.RustIron, countB = 4,
                sourceHintA = "捕猎哨塔采集", sourceHintB = "地铁探洞井搜刮",
            },
        };

        public static readonly HeroDef[] Heroes =
        {
            new HeroDef
            {
                heroId = "ironpot_sam", displayName = "铁锅山姆", titleTag = "40号公路盾卫",
                portraitKey = "hero_ironpot_sam", rarity = Rarity.Legendary,
                heroClass = HeroClass.Tank, faction = HeroFaction.Brotherhood,
                stars = 15, baseHp = 48500, baseAtk = 3620,
                combatSkillName = "红油铁锅嘲讽",
                combatSkillDesc = "敲响头顶黑铁锅震慑全场敌军2.5秒，并获得自身最大生命28%的公路路牌护盾",
                campBoostBuildingId = "command_bus",
                campBoostDesc = "委任于【指挥大巴】：全部加工坊单次制作耗时减少320秒",
                reduceProdSeconds = 320,
            },
            new HeroDef
            {
                heroId = "chef_bobby", displayName = "火头小布", titleTag = "熏肉学徒",
                portraitKey = "walker_mechanic", rarity = Rarity.Rare,
                heroClass = HeroClass.Warrior, faction = HeroFaction.Brotherhood,
                stars = 10, baseHp = 16200, baseAtk = 1890,
                combatSkillName = "猛火旋风铲",
                combatSkillDesc = "挥舞铁铲对前方扇形造成210%物理伤害",
                campBoostBuildingId = "smokehouse",
                campBoostDesc = "专属工坊技能：【废土熏肉灶】生产速度提升+40%",
                reduceProdSeconds = 90,
            },
        };

        public static string ResourceName(ResourceId id)
        {
            switch (id)
            {
                case ResourceId.Coin: return "瓶盖金币";
                case ResourceId.Gem: return "辐射晶钻";
                case ResourceId.Blueprint: return "建造蓝图";
                case ResourceId.RawMeat: return "变异生肉";
                case ResourceId.ScrapWood: return "废木料";
                case ResourceId.RustIron: return "锈废铁";
                case ResourceId.SmokedRibs: return "炭烤熏肉排";
                case ResourceId.WastelandBurger: return "末日巨无霸";
                case ResourceId.CannedStew: return "废土肉罐头";
                case ResourceId.CleanWater: return "纯净水";
                case ResourceId.Merit: return "废土勋章";
                default: return id.ToString();
            }
        }

        public static string ResourceIcon(ResourceId id)
        {
            switch (id)
            {
                case ResourceId.Coin: return "ico_coin";
                case ResourceId.Gem: return "ico_gem";
                case ResourceId.Blueprint: return "ico_blueprint";
                case ResourceId.RawMeat: return "ico_raw_meat";
                case ResourceId.ScrapWood:
                case ResourceId.RustIron: return "ico_scrap_wood";
                case ResourceId.SmokedRibs:
                case ResourceId.WastelandBurger:
                case ResourceId.CannedStew: return "ico_smoked_ribs";
                case ResourceId.CleanWater: return "ico_clean_water";
                default: return "ico_coin";
            }
        }

        public static PlotDef FindPlot(int plotId)
        {
            for (int i = 0; i < Plots.Length; i++)
                if (Plots[i].plotId == plotId) return Plots[i];
            return null;
        }
    }
}
