using UnityEngine;

namespace Mori.ForestSlice
{
    /// <summary>
    /// 深山末日生存预设阶段构建器（升级版）：
    /// stage = 0：完整新手引导开局（双人漫画对话开场 -> 白手套引导多选打勾砍树捡木 -> 破框卡造木工棚 -> 烟尘敲金锤 -> 半屏5格队列切木板 -> 满足居民升星 -> 尸潮预警 -> 钓鱼与进城！）
    /// stage = 1：中期营地（2级加固木屋、左侧尸潮倒计时挂件、居民饥饿气泡、密林废发电机与破皮卡待改造）
    /// stage = 2：4级完全体避难所（3级双层木堡、全英雄驻守、可直接体验南岸推镜甩杆钓鱼与丧尸城虚拟摇杆开车撞尸搜刮！）
    /// </summary>
    public static class ForestScenario
    {
        public static void Apply(ForestSliceView v, ForestSurvivalSim sim, ForestSurvivalWorld world, int stage)
        {
            ForestMap m = v.Map;
            // 初始开阔林间草坪（12x11 共 132 格，环绕中央净空大广场！）
            m.ClearRectInstant(10, 11, 21, 21);

            if (stage >= 1)
            {
                m.ClearRectInstant(18, 8, 22, 14);  // 东南古井 + 锻造坊 + 裁缝防具坊
                m.ClearRectInstant(8, 11, 12, 23);  // 西南物资箱 + 菜麻田 + 2号民居 + 哨塔
                m.ClearRectInstant(14, 21, 17, 24); // 西北破皮卡
                m.ClearRectInstant(21, 16, 24, 22); // 东北山涧发电房 + 行商电台

                sim.CampLevel = 4;
                sim.CampXp = 45;
                sim.Coins = 420;
                sim.SatisfiedToday = 2;
                sim.QuestIndex = 6;
                sim.HordeWarningTriggered = true;
                sim.HordePosterShown = true;

                sim.SyncRevealedNodes(m, silent: true);
                ForceActive(sim, "sawmill", 2);
                ForceActive(sim, "smithy", 1);
                ForceActive(sim, "tailor", 1);
                ForceActive(sim, "house_1", 2);
                ForceActive(sim, "house_2", 1);
                ForceActive(sim, "heat_tower", 2);
                ForceActive(sim, "warehouse", 2);
                ForceActive(sim, "relic_well", 1);
                sim.Find("relic_loot_sw").state = NodeState.Depleted;
                ForceActive(sim, "farm_a", 1);
                sim.Find("farm_a").farmPhase = FarmPhase.Ripe;
                ForceActive(sim, "shelter", 2);
                sim.Find("shelter").activeTitle = "加固原木议事厅";
                ForceActive(sim, "watchtower", 1);
                ForceActive(sim, "barricade", 1);
                sim.Find("relic_well").storedRes = SurvRes.Water;
                sim.Find("relic_well").storedAmount = 3;

                sim.UnlockHero("laochentou", silent: true);
                sim.UnlockHero("linyan", silent: true);
                sim.AutoAssignHeroes();

                sim.Add(SurvRes.Wood, 24);
                sim.Add(SurvRes.Plank, 14);
                sim.Add(SurvRes.Water, 10);
                sim.Add(SurvRes.Veggie, 8);
                sim.Add(SurvRes.Cloth, 6);
                sim.Add(SurvRes.Pelt, 4);
                sim.Add(SurvRes.Nails, 6);
                sim.Add(SurvRes.LinenShirt, 2);
                sim.Add(SurvRes.Fish, 4);
                sim.Add(SurvRes.Ration, 14);
                sim.Add(SurvRes.Scrap, 16);
                sim.Add(SurvRes.Parts, 3);
                sim.Add(SurvRes.Blueprint, 3);
                sim.RefreshAllOrders(freeForce: true);
            }

            if (stage >= 2)
            {
                m.ClearRectInstant(17, 21, 22, 27); // 正北山道关卡 + 3号木楼
                m.ClearRectInstant(8, 0, 18, 14);   // 南侧宽阔河谷走廊 + 钓鱼台 + 草药坊 + 药圃

                sim.CampLevel = 8;
                sim.CampXp = 120;
                sim.Coins = 1480;
                sim.SatisfiedToday = 4;
                sim.QuestIndex = 9;

                sim.SyncRevealedNodes(m, silent: true);
                ForceActive(sim, "herb_hut", 2);
                ForceActive(sim, "tailor", 3);
                ForceActive(sim, "smithy", 3);
                ForceActive(sim, "sawmill", 3);
                ForceActive(sim, "warehouse", 3);
                ForceActive(sim, "heat_tower", 3);
                ForceActive(sim, "house_3", 1);
                ForceActive(sim, "relic_generator", 1);
                ForceActive(sim, "radio_post", 1);
                ForceActive(sim, "relic_truck", 1);
                ForceActive(sim, "relic_road_gate", 1);
                ForceActive(sim, "shelter", 3);
                sim.Find("shelter").activeTitle = "末日避难所木堡";
                ForceActive(sim, "kitchen", 3);
                sim.Find("kitchen").activeTitle = "野外熏肉大厨房";
                ForceActive(sim, "farm_b", 1);
                sim.Find("farm_b").farmPhase = FarmPhase.Growing;
                ForceActive(sim, "river_dock", 1);

                sim.UnlockHero("laochentou", silent: true);
                sim.UnlockHero("linyan", silent: true);
                sim.UnlockHero("dachui", silent: true);
                sim.AutoAssignHeroes();

                sim.Add(SurvRes.Wood, 40);
                sim.Add(SurvRes.Plank, 24);
                sim.Add(SurvRes.IronBeam, 8);
                sim.Add(SurvRes.WoodBed, 4);
                sim.Add(SurvRes.Nails, 12);
                sim.Add(SurvRes.IronIngot, 8);
                sim.Add(SurvRes.MetalPipe, 6);
                sim.Add(SurvRes.Herb, 10);
                sim.Add(SurvRes.HerbPoultice, 4);
                sim.Add(SurvRes.FurCoat, 3);
                sim.Add(SurvRes.Fish, 8);
                sim.Add(SurvRes.Meat, 8);
                sim.Add(SurvRes.SmokedMeat, 3);
                sim.Add(SurvRes.Ration, 28);
                sim.Add(SurvRes.Scrap, 30);
                sim.Add(SurvRes.Parts, 8);
                sim.Add(SurvRes.Blueprint, 8);
                sim.RefreshAllOrders(freeForce: true);
                sim.RefreshCaravanOffers();
            }

            m.RebuildMask();
            v.RefreshEdges(instant: true);
            for (int i = 0; i < sim.Heroes.Count; i++)
            {
                if (sim.Heroes[i].unlocked)
                    sim.Heroes[i].skinTier = sim.MaxUnlockedSkinTier;
            }
            world.BuildInitialVisuals(spawnStarterPiles: stage == 0);
            v.DecorateCleared();

            if (stage == 0)
            {
                world.SpawnWildPrey();
                v.FrameCamera(new Vector2(16.2f, 16.5f), 5.5f);
            }
            else if (stage == 1)
            {
                world.SpawnWildPrey();
                world.SpawnWanderingZombie();
                v.FrameCamera(new Vector2(16.0f, 16.0f), 6.8f);
            }
            else
            {
                world.SpawnWildPrey();
                v.FrameCamera(new Vector2(16.0f, 14.5f), 8.2f);
            }
        }

        static void ForceActive(ForestSurvivalSim sim, string id, int lv)
        {
            CampNode n = sim.Find(id);
            if (n == null) return;
            n.state = NodeState.Active;
            n.level = lv;
        }
    }

    /// <summary>Play 模式总入口：整合森林渲染、商业级漫画 UI、多选派工、半屏抽屉、推镜钓鱼与摇杆开车搜刮。</summary>
    public sealed class ForestSliceBoot : MonoBehaviour
    {
        public int startStage = 0;

        ForestSliceView _view;
        ForestSurvivalSim _sim;
        ForestSurvivalWorld _world;
        ForestSurvivalHud _hud;
        Camera _cam;
        Vector3 _down, _camAtDown;
        bool _drag;
        bool _downOnUi;

        public ForestSliceView View => _view;
        public ForestSurvivalSim Sim => _sim;
        public ForestSurvivalWorld World => _world;
        public ForestSurvivalHud Hud => _hud;

        void Start()
        {
            InitAll(null, startStage, playIntroInStage0: true);
            _autoTest = Application.isEditor && System.IO.File.Exists(AutoTestFlag);
        }

        public void InitAll(Camera externalCam, int stage, bool playIntroInStage0 = false)
        {
            _cam = externalCam != null ? externalCam : Camera.main;
            if (_cam == null) _cam = new GameObject("MainCamera", typeof(Camera)).GetComponent<Camera>();

            _view = gameObject.AddComponent<ForestSliceView>();
            _view.Build(_cam);

            _sim = new ForestSurvivalSim();
            _world = new ForestSurvivalWorld(_view, _sim);
            ForestScenario.Apply(_view, _sim, _world, stage);

            _hud = gameObject.AddComponent<ForestSurvivalHud>();
            _hud.Init(_cam, _sim, _world);
            if (playIntroInStage0 && stage == 0)
                _sim.TriggerIntroDialog();
            _hud.Tick(0.02f);
        }

        // ---- 编辑器自动实机测试 ----
        public const string AutoTestFlag = "/tmp/mori_forest_autotest.flag";
        bool _autoTest;
        float _autoT;
        int _autoStep;

        void TickAutoTest()
        {
            if (!_autoTest) return;
            _autoT += Time.unscaledDeltaTime;
            if (_autoStep == 0 && _autoT > 1.2f)
            {
                Debug.Log("[ForestAutoTest] Play mode HUD & Comic Dialog active=" + _hud.IsModalOpen);
                _autoStep = 1;
            }
            else if (_autoStep == 1 && _autoT > 2.2f)
            {
                _hud.CloseAllModals();
                // 模拟点击一处地面原木堆加入打勾采集队列
                if (_world.GroundPiles.Count > 0)
                {
                    var p = _world.GroundPiles[0];
                    Vector3 sp = _cam.WorldToScreenPoint(ForestSliceView.Iso(p.gx, p.gy));
                    _world.HandleWorldClick(_cam, new Vector2(sp.x, sp.y));
                }
                Debug.Log("[ForestAutoTest] queued chop tasks=" + _world.ChopQueue.Count);
                _autoStep = 2;
            }
            else if (_autoStep == 2 && _autoT > 3.6f)
            {
                _autoStep = 3;
                System.IO.File.Delete(AutoTestFlag);
                Debug.Log("[ForestAutoTest] done");
            }
        }

        void Update()
        {
            if (_view == null) return;
            float dt = Time.deltaTime;
            TickAutoTest();

            _sim.Tick(dt);
            _world.Tick(dt);
            _view.Tick(dt);
            _hud.Tick(dt);

            // 支持主营地里直接用键盘 WASD / 方向键控制主角阿澈奔跑！
            if (_hud == null || !_hud.IsModalOpen)
            {
                float kx = Input.GetAxisRaw("Horizontal");
                float ky = Input.GetAxisRaw("Vertical");
                if (Mathf.Abs(kx) > 0.01f || Mathf.Abs(ky) > 0.01f)
                    _world.DriveHeroByInput(new Vector2(kx, ky), dt);
            }

            if (Input.GetMouseButtonDown(0))
            {
                _down = Input.mousePosition;
                _camAtDown = _cam.transform.position;
                _drag = false;
                _downOnUi = _hud != null && _hud.IsPointerOverUi();
                if (!_downOnUi && _hud != null) _hud.CancelPan();
            }

            if (Input.GetMouseButton(0) && !_downOnUi && (_hud == null || !_hud.IsModalOpen))
            {
                Vector3 d = Input.mousePosition - _down;
                if (d.magnitude > 12f) _drag = true;
                if (_drag)
                {
                    float k = _cam.orthographicSize * 2f / Mathf.Max(1, Screen.height);
                    Vector3 next = _camAtDown - new Vector3(d.x * k, d.y * k, 0f);
                    next.x = Mathf.Clamp(next.x, -6.5f, 8.5f);
                    next.y = Mathf.Clamp(next.y, -0.5f, 16.0f);
                    next.z = -20f;
                    _cam.transform.position = next;
                }
            }

            if (Input.GetMouseButtonUp(0) && !_drag && !_downOnUi && (_hud == null || !_hud.IsModalOpen))
            {
                _world.HandleWorldClick(_cam, Input.mousePosition);
            }

            if (_hud == null || !_hud.IsModalOpen)
            {
                float wheel = Input.mouseScrollDelta.y;
                if (Mathf.Abs(wheel) > 0.01f)
                {
                    _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize - wheel * 0.35f, 3.2f, 8.4f);
                    _view.SyncCameraOverlays();
                }
            }
        }
    }
}
