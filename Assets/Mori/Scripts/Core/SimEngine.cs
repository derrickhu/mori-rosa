using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mori
{
    [Serializable]
    public sealed class PlotRuntime
    {
        public int plotId;
        public PlotState state;
        public int level;
        public float remainBuildSec;
        public ResourceId readyCollectRes;
        public int readyCollectCount;
    }

    [Serializable]
    public sealed class QueueJob
    {
        public string recipeId;
        public float totalSec;
        public float elapsedSec;
        public bool finished;
    }

    /// <summary>
    /// 纯C#营地经营模拟核心（不依赖MonoBehaviour，可直接单测与离线补偿推演）：
    /// 1. 复刻《疯狂水世界》「人口作门槛不消耗 + 公共建筑卡人口上限」双锁机制
    /// 2. 建筑倒计时归零停在 ReadyToTap，必须手动点击才算竣工并发放经验与人口
    /// 3. 工坊队列计算英雄委任减秒数与蓝紫英雄专属提速百分比
    /// </summary>
    public sealed class SimEngine
    {
        public int PlayerLevel { get; private set; } = 5;
        public int PlayerExp { get; private set; } = 320;
        public int NextLevelExp => PlayerLevel * 120;

        public int BasePopulation { get; private set; } = 12;
        public int BasePopCap { get; private set; } = 40;
        public bool AutoProdAdActive { get; private set; }
        public float AutoProdRemainSec { get; private set; }
        public int OrdersCompletedToday { get; private set; } = 14;

        readonly Dictionary<ResourceId, int> _stock = new Dictionary<ResourceId, int>();
        readonly Dictionary<int, PlotRuntime> _plots = new Dictionary<int, PlotRuntime>();
        readonly List<QueueJob> _smokehouseQueue = new List<QueueJob>();
        readonly List<SurvivorOrder> _orders = new List<SurvivorOrder>();

        public IReadOnlyList<QueueJob> SmokehouseQueue => _smokehouseQueue;
        public IReadOnlyList<SurvivorOrder> Orders => _orders;

        public void SeedVerticalSlice()
        {
            _stock.Clear();
            _plots.Clear();
            _smokehouseQueue.Clear();
            _orders.Clear();

            PlayerLevel = 5;
            PlayerExp = 360;
            OrdersCompletedToday = 15;
            AutoProdAdActive = false;
            AutoProdRemainSec = 0f;

            _stock[ResourceId.Coin] = 12850;
            _stock[ResourceId.Gem] = 680;
            _stock[ResourceId.Blueprint] = 14;
            _stock[ResourceId.RawMeat] = 18;
            _stock[ResourceId.ScrapWood] = 26;
            _stock[ResourceId.RustIron] = 9;
            _stock[ResourceId.SmokedRibs] = 5;
            _stock[ResourceId.CleanWater] = 1; // 故意少1瓶纯净水，展示「末日巨无霸」缺料一键溯源跳转
            _stock[ResourceId.Merit] = 420;

            // 初始化首屏6块核心建地状态（含正常运转、可收获成品、待建脚手架）
            _plots[1] = new PlotRuntime { plotId = 1, state = PlotState.Built, level = 3 };
            _plots[2] = new PlotRuntime
            {
                plotId = 2, state = PlotState.Built, level = 2,
                readyCollectRes = ResourceId.SmokedRibs, readyCollectCount = 2,
            };
            _plots[3] = new PlotRuntime { plotId = 3, state = PlotState.Built, level = 2 };
            _plots[4] = new PlotRuntime
            {
                plotId = 4, state = PlotState.Built, level = 1,
                readyCollectRes = ResourceId.CleanWater, readyCollectCount = 3,
            };
            _plots[5] = new PlotRuntime { plotId = 5, state = PlotState.Built, level = 1 };
            _plots[6] = new PlotRuntime { plotId = 6, state = PlotState.Outline, level = 0 };

            // 熏肉灶当前正在烤制一单
            _smokehouseQueue.Add(new QueueJob
            {
                recipeId = "smoked_ribs",
                totalSec = 32f,
                elapsedSec = 22f,
                finished = false,
            });

            // 幸存者订单板（3张活跃单：2张可立即交付，1张挑剔居民缺料单）
            _orders.Add(new SurvivorOrder
            {
                slotIndex = 0, npcName = "机修工阿杰", moodNote = "修了一上午发电车，急需烤排补体力！",
                reqResA = ResourceId.SmokedRibs, reqCountA = 2,
                reqResB = ResourceId.ScrapWood, reqCountB = 4,
                rewardExp = 85, rewardCoin = 460,
            });
            _orders.Add(new SurvivorOrder
            {
                slotIndex = 1, npcName = "巡逻员老胡", moodNote = "哨塔太阳太毒了，带点肉和木材加固下。",
                reqResA = ResourceId.RawMeat, reqCountA = 4,
                reqResB = ResourceId.SmokedRibs, reqCountB = 1,
                rewardExp = 70, rewardCoin = 380,
            });
            _orders.Add(new SurvivorOrder
            {
                slotIndex = 2, npcName = "挑剔的莉莉", moodNote = "今晚营火派对我想吃刚出炉的末日巨无霸！",
                reqResA = ResourceId.WastelandBurger, reqCountA = 1,
                reqResB = ResourceId.CleanWater, reqCountB = 2,
                rewardExp = 160, rewardCoin = 920,
            });
        }

        public int GetResource(ResourceId id)
        {
            return _stock.TryGetValue(id, out int v) ? v : 0;
        }

        public void AddResource(ResourceId id, int delta)
        {
            if (delta == 0) return;
            int cur = GetResource(id);
            _stock[id] = Mathf.Max(0, cur + delta);
        }

        public PlotRuntime GetPlot(int plotId)
        {
            return _plots.TryGetValue(plotId, out PlotRuntime p) ? p : null;
        }

        /// <summary>当前有效人口 = 基础人口 + 所有已点击竣工（Built）民居提供的人口。</summary>
        public int CurrentPopulation
        {
            get
            {
                int total = BasePopulation;
                foreach (var kv in _plots)
                {
                    if (kv.Value.state != PlotState.Built) continue;
                    PlotDef def = MoriCatalog.FindPlot(kv.Key);
                    if (def != null) total += def.providePopulation;
                }
                return total;
            }
        }

        public int CurrentPopCap
        {
            get
            {
                int cap = BasePopCap;
                foreach (var kv in _plots)
                {
                    if (kv.Value.state != PlotState.Built) continue;
                    PlotDef def = MoriCatalog.FindPlot(kv.Key);
                    if (def != null) cap += def.providePopCap;
                }
                return cap;
            }
        }

        public int MaxWorkshopQueueSlots => AutoProdAdActive ? 2 : 1;

        /// <summary>计算英雄委任与蓝紫专属技能折算后的实际工坊耗时。</summary>
        public float CalcActualCraftSeconds(RecipeDef recipe)
        {
            // 指挥大巴委任铁锅山姆（减秒）+ 火头小布熏肉灶专属技能（+40%速度）
            float baseSec = recipe.craftSeconds;
            float speedBonus = recipe.buildingId == "smokehouse" ? 0.40f : 0f;
            float reduced = Mathf.Max(8f, baseSec * 0.72f);
            return Mathf.Ceil(reduced / (1f + speedBonus));
        }

        public bool CanStartConstruction(int plotId, out string reason)
        {
            PlotDef def = MoriCatalog.FindPlot(plotId);
            PlotRuntime rt = GetPlot(plotId);
            if (def == null || rt == null || rt.state != PlotState.Outline)
            {
                reason = "当前地块不可建造";
                return false;
            }
            if (PlayerLevel < def.unlockPlayerLv)
            {
                reason = "需要领袖等级Lv." + def.unlockPlayerLv;
                return false;
            }
            if (CurrentPopulation < def.requirePopulation)
            {
                reason = "人口不足(" + CurrentPopulation + "/" + def.requirePopulation + ")";
                return false;
            }
            if (def.providePopulation > 0 && CurrentPopulation + def.providePopulation > CurrentPopCap)
            {
                reason = "人口已达上限，请先建公共设施";
                return false;
            }
            if (GetResource(def.costRes) < def.costAmount)
            {
                reason = MoriCatalog.ResourceName(def.costRes) + "不足";
                return false;
            }
            reason = "";
            return true;
        }

        public bool StartConstruction(int plotId, out string reason)
        {
            if (!CanStartConstruction(plotId, out reason)) return false;
            PlotDef def = MoriCatalog.FindPlot(plotId);
            PlotRuntime rt = GetPlot(plotId);
            AddResource(def.costRes, -def.costAmount);
            rt.state = PlotState.Constructing;
            rt.remainBuildSec = def.buildSeconds;
            return true;
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;

            if (AutoProdAdActive)
            {
                AutoProdRemainSec -= dt;
                if (AutoProdRemainSec <= 0f)
                {
                    AutoProdRemainSec = 0f;
                    AutoProdAdActive = false;
                }
            }

            foreach (var kv in _plots)
            {
                PlotRuntime p = kv.Value;
                if (p.state == PlotState.Constructing)
                {
                    p.remainBuildSec -= dt;
                    if (p.remainBuildSec <= 0f)
                    {
                        p.remainBuildSec = 0f;
                        // 倒计时归零不自动完成，等待玩家手动点击爆经验与新居民
                        p.state = PlotState.ReadyToTap;
                    }
                }
            }

            for (int i = 0; i < _smokehouseQueue.Count; i++)
            {
                QueueJob job = _smokehouseQueue[i];
                if (job.finished) continue;
                job.elapsedSec = Mathf.Min(job.totalSec, job.elapsedSec + dt);
                if (job.elapsedSec >= job.totalSec)
                    job.finished = true;
                break; // 按队列顺序逐个加工
            }
        }

        public bool TapCompleteConstruction(int plotId)
        {
            PlotRuntime rt = GetPlot(plotId);
            PlotDef def = MoriCatalog.FindPlot(plotId);
            if (rt == null || def == null || rt.state != PlotState.ReadyToTap) return false;
            rt.state = PlotState.Built;
            rt.level = 1;
            GainExp(def.finishExp);
            return true;
        }

        public bool CollectPlotOutput(int plotId, out ResourceId res, out int count)
        {
            PlotRuntime rt = GetPlot(plotId);
            if (rt == null || rt.readyCollectCount <= 0)
            {
                res = ResourceId.Coin;
                count = 0;
                return false;
            }
            res = rt.readyCollectRes;
            count = rt.readyCollectCount;
            rt.readyCollectCount = 0;
            AddResource(res, count);
            return true;
        }

        public bool EnqueueSmokehouse(RecipeDef recipe, out string failReason)
        {
            if (_smokehouseQueue.Count >= MaxWorkshopQueueSlots)
            {
                failReason = "生产队列已满，观看广告可解锁第2队列";
                return false;
            }
            if (GetResource(recipe.inputA) < recipe.countA)
            {
                failReason = "缺少" + MoriCatalog.ResourceName(recipe.inputA);
                return false;
            }
            if (recipe.countB > 0 && GetResource(recipe.inputB) < recipe.countB)
            {
                failReason = "缺少" + MoriCatalog.ResourceName(recipe.inputB);
                return false;
            }
            AddResource(recipe.inputA, -recipe.countA);
            if (recipe.countB > 0) AddResource(recipe.inputB, -recipe.countB);

            _smokehouseQueue.Add(new QueueJob
            {
                recipeId = recipe.recipeId,
                totalSec = CalcActualCraftSeconds(recipe),
                elapsedSec = 0f,
                finished = false,
            });
            failReason = "";
            return true;
        }

        public void ActivateAutoProdAd()
        {
            AutoProdAdActive = true;
            AutoProdRemainSec = Mathf.Min(12f * 3600f, AutoProdRemainSec + 4f * 3600f);
        }

        public bool CanDeliverOrder(SurvivorOrder order)
        {
            if (order == null || order.delivered) return false;
            if (GetResource(order.reqResA) < order.reqCountA) return false;
            if (order.reqCountB > 0 && GetResource(order.reqResB) < order.reqCountB) return false;
            return true;
        }

        public bool DeliverOrder(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _orders.Count) return false;
            SurvivorOrder ord = _orders[slotIndex];
            if (!CanDeliverOrder(ord)) return false;

            AddResource(ord.reqResA, -ord.reqCountA);
            if (ord.reqCountB > 0) AddResource(ord.reqResB, -ord.reqCountB);
            AddResource(ResourceId.Coin, ord.rewardCoin);
            GainExp(ord.rewardExp);
            ord.delivered = true;
            OrdersCompletedToday++;
            return true;
        }

        void GainExp(int exp)
        {
            PlayerExp += exp;
            while (PlayerExp >= NextLevelExp)
            {
                PlayerExp -= NextLevelExp;
                PlayerLevel++;
            }
        }
    }
}
