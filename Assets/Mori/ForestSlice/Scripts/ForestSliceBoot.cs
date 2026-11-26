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
            // 新手温馨起步林间草甸：严格按 ChunkSize=3 区块对齐开垦 (x=12..20 即 chunk 4..6, y=15..23 即 chunk 5..7)，杜绝向外拓荒时残留孤树条带！
            m.ClearRectInstant(12, 15, 20, 23);

            if (stage >= 1)
            {
                m.ClearRectInstant(7, 11, 26, 24);

                sim.CampLevel = 4;
                sim.CampXp = 45;
                sim.Coins = 420;
                sim.SatisfiedToday = 2;
                sim.QuestIndex = 6;
                sim.HordeWarningTriggered = true;
                sim.HordePosterShown = true;

                sim.SyncRevealedNodes(m, silent: true);
                ForceActive(m, sim, "sawmill", 2);
                ForceActive(m, sim, "smithy", 1);
                ForceActive(m, sim, "tailor", 1);
                ForceActive(m, sim, "house_1", 2);
                ForceActive(m, sim, "house_2", 1);
                ForceActive(m, sim, "heat_tower", 2);
                ForceActive(m, sim, "warehouse", 2);
                ForceActive(m, sim, "relic_well", 1);
                CampNode lootSw = sim.Find("relic_loot_sw");
                if (lootSw != null) lootSw.state = NodeState.Depleted;
                ForceActive(m, sim, "farm_a", 2);
                sim.FarmPlots[0].phase = FarmPhase.Ripe;
                sim.FarmPlots[0].seedKind = CropSeedKind.Veggie;
                sim.FarmPlots[1].phase = FarmPhase.Growing;
                sim.FarmPlots[1].seedKind = CropSeedKind.Herb;
                sim.FarmPlots[1].watered = true;
                sim.FarmPlots[1].timer = 4.2f;
                sim.SyncFarmVisualPhase();

                ForceActive(m, sim, "beast_ranch", 1);
                sim.RanchPens[0].animal = RanchAnimalKind.SnowRabbit;
                sim.RanchPens[0].readyToCollect = true;
                sim.RanchPens[1].animal = RanchAnimalKind.SnowRabbit;
                sim.RanchPens[1].feeding = true;
                sim.RanchPens[1].timer = 3.5f;

                ForceActive(m, sim, "shelter", 2);
                if (sim.Find("shelter") != null)
                    sim.Find("shelter").activeTitle = "加固原木议事厅";
                ForceActive(m, sim, "watchtower", 1);
                ForceActive(m, sim, "barricade", 1);
                if (sim.Find("relic_well") != null)
                {
                    sim.Find("relic_well").storedRes = SurvRes.Water;
                    sim.Find("relic_well").storedAmount = 3;
                }

                sim.UnlockHero("dachui", silent: true);
                sim.UnlockHero("xiaotang", silent: true);
                sim.UnlockHero("leimeng", silent: true);
                sim.UnlockHero("linyan", silent: true);
                SetHeroLvStar(sim, "dachui", 3, 2);
                SetHeroLvStar(sim, "xiaotang", 3, 2);
                SetHeroLvStar(sim, "leimeng", 3, 2);
                SetHeroLvStar(sim, "linyan", 2, 1);
                sim.Add(SurvRes.HeroMedal, 5);
                sim.AutoAssignHeroes();
                sim.EnsureCombatSquad();
                sim.AutoDeployRecommendedFormation();

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
                sim.Add(SurvRes.SeedVeggie, 4);
                sim.Add(SurvRes.SeedFlax, 4);
                sim.Add(SurvRes.SeedHerb, 3);
                sim.Add(SurvRes.SeedGolden, 1);
                sim.Add(SurvRes.Compost, 2);
                sim.RefreshAllOrders(freeForce: true);
            }

            if (stage >= 2)
            {
                m.ClearRectInstant(4, 3, 28, 28);

                sim.CampLevel = 8;
                sim.CampXp = 120;
                sim.Coins = 1480;
                sim.SatisfiedToday = 4;
                sim.QuestIndex = 9;

                sim.SyncRevealedNodes(m, silent: true);
                ForceActive(m, sim, "herb_hut", 2);
                ForceActive(m, sim, "tailor", 3);
                ForceActive(m, sim, "smithy", 3);
                ForceActive(m, sim, "sawmill", 3);
                ForceActive(m, sim, "warehouse", 3);
                ForceActive(m, sim, "heat_tower", 3);
                ForceActive(m, sim, "house_3", 1);
                ForceActive(m, sim, "relic_generator", 1);
                ForceActive(m, sim, "radio_post", 1);
                ForceActive(m, sim, "relic_truck", 1);
                ForceActive(m, sim, "relic_road_gate", 1);
                ForceActive(m, sim, "shelter", 3);
                if (sim.Find("shelter") != null)
                    sim.Find("shelter").activeTitle = "末日避难所木堡";
                ForceActive(m, sim, "kitchen", 3);
                if (sim.Find("kitchen") != null)
                    sim.Find("kitchen").activeTitle = "野外熏肉大厨房";
                ForceActive(m, sim, "farm_a", 3);
                ForceActive(m, sim, "beast_ranch", 2);
                sim.RanchPens[2].animal = RanchAnimalKind.WildBoar;
                sim.RanchPens[2].feeding = true;
                sim.RanchPens[2].timer = 5.0f;
                ForceActive(m, sim, "relic_well", 2);
                ForceActive(m, sim, "river_dock", 1);

                sim.UnlockHero("dachui", silent: true);
                sim.UnlockHero("xiaotang", silent: true);
                sim.UnlockHero("leimeng", silent: true);
                sim.UnlockHero("linyan", silent: true);
                sim.UnlockHero("tieshu", silent: true);
                sim.UnlockHero("laochentou", silent: true);
                SetHeroLvStar(sim, "dachui", 6, 3);
                SetHeroLvStar(sim, "xiaotang", 5, 2);
                SetHeroLvStar(sim, "leimeng", 5, 3);
                SetHeroLvStar(sim, "linyan", 5, 2);
                SetHeroLvStar(sim, "tieshu", 4, 2);
                SetHeroLvStar(sim, "laochentou", 4, 2);
                sim.Add(SurvRes.HeroMedal, 14);
                sim.HordeWaveIndex = 3;
                sim.MaxClearedWave = 2;
                sim.AutoAssignHeroes();
                sim.EnsureCombatSquad();
                sim.AutoDeployRecommendedFormation();

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
            world.SyncCivilianVillagers();
            v.DecorateCleared();

            if (stage == 0)
            {
                world.SpawnWildPrey();
                v.FrameCamera(new Vector2(16.4f, 18.5f), 5.5f);
            }
            else if (stage == 1)
            {
                world.SpawnWildPrey();
                world.SpawnWanderingZombie();
                v.FrameCamera(new Vector2(16.2f, 17.6f), 6.8f);
            }
            else
            {
                world.SpawnWildPrey();
                v.FrameCamera(new Vector2(16.0f, 16.0f), 8.2f);
            }
        }

        static void ForceActive(ForestMap m, ForestSurvivalSim sim, string id, int lv)
        {
            CampNode n = sim.Find(id);
            if (n == null) return;
            n.state = NodeState.Active;
            n.level = lv;
            n.siteUncovered = true;
            n.siteSalvageCleared = true;
            if (m != null)
            {
                int x0 = Mathf.Min(n.occupyX - 1, Mathf.FloorToInt(n.gx) - 1);
                int y0 = Mathf.Min(n.occupyY - 1, Mathf.FloorToInt(n.gy) - 1);
                int x1 = Mathf.Max(n.occupyX + n.occupyW, Mathf.FloorToInt(n.gx) + 1);
                int y1 = Mathf.Max(n.occupyY + n.occupyH, Mathf.FloorToInt(n.gy) + 1);
                m.ClearRectInstant(x0, y0, x1, y1);
            }
        }

        static void SetHeroLvStar(ForestSurvivalSim sim, string id, int lv, int stars)
        {
            HeroSpec h = sim.FindHero(id);
            if (h == null) return;
            h.level = lv;
            h.stars = stars;
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
        ForestGmConsole _gm;
        Camera _cam;
        Vector3 _down, _camAtDown;
        bool _drag;
        bool _downOnUi;

        public ForestSliceView View => _view;
        public ForestSurvivalSim Sim => _sim;
        public ForestSurvivalWorld World => _world;
        public ForestSurvivalHud Hud => _hud;
        public ForestGmConsole Gm => _gm;

        void Start()
        {
            _autoTest = Application.isEditor && System.IO.File.Exists(AutoTestFlag);
            InitAll(null, startStage, playIntroInStage0: !_autoTest, showBootLoading: !_autoTest);
            if (!_autoTest) Mori.Analytics.Begin();
            if (_autoTest && startStage == 0)
                _sim.TriggerIntroDialog();
        }

        public void InitAll(Camera externalCam, int stage, bool playIntroInStage0 = false, bool showBootLoading = false)
        {
            _cam = externalCam != null ? externalCam : Camera.main;
            if (_cam == null) _cam = new GameObject("MainCamera", typeof(Camera)).GetComponent<Camera>();

            _view = gameObject.AddComponent<ForestSliceView>();
            _view.Build(_cam);

            _sim = new ForestSurvivalSim();
            _world = new ForestSurvivalWorld(_view, _sim, _cam);
            ForestScenario.Apply(_view, _sim, _world, stage);

            _hud = gameObject.AddComponent<ForestSurvivalHud>();
            _hud.Init(_cam, _sim, _world);
            if (showBootLoading)
            {
                _hud.StartBootLoading(1.9f, () =>
                {
                    if (playIntroInStage0 && stage == 0)
                        _sim.TriggerIntroDialog();
                });
            }
            else if (playIntroInStage0 && stage == 0)
            {
                _sim.TriggerIntroDialog();
            }
            _hud.Tick(0.02f);

            _gm = gameObject.AddComponent<ForestGmConsole>();
            _gm.Init(_sim, _world, _hud);
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

            bool inArena = _world != null && _world.InWaveDefense;
            bool modalBlocked = (_hud != null && _hud.IsModalOpen) || ForestGmConsole.IsGmPanelOpen || inArena;

            // 先响应键盘 WASD / 方向键操控输入，再统一推进 _world.Tick(dt)，避免同一帧内 Run 动画被重置为 Idle 造成顿挫！
            if (!modalBlocked)
            {
                float kx = Input.GetAxisRaw("Horizontal");
                float ky = Input.GetAxisRaw("Vertical");
                if (Mathf.Abs(kx) > 0.01f || Mathf.Abs(ky) > 0.01f)
                    _world.DriveHeroByInput(new Vector2(kx, ky), dt);
            }

            _world.Tick(dt);
            _view.Tick(dt);
            _hud.Tick(dt);

            if (Input.GetMouseButtonDown(0))
            {
                _down = Input.mousePosition;
                _camAtDown = _cam.transform.position;
                _drag = false;
                _downOnUi = ForestGmConsole.IsGmPanelOpen || inArena || (_hud != null && _hud.IsPointerOverUi());
                if (!_downOnUi && _hud != null) _hud.CancelPan();
            }

            if (Input.GetMouseButton(0) && !_downOnUi && !modalBlocked)
            {
                Vector3 d = Input.mousePosition - _down;
                float dragThreshold = Mathf.Max(18f, Mathf.Min(Screen.width, Screen.height) * 0.024f);
                if (d.magnitude > dragThreshold) _drag = true;
                if (_drag)
                {
                    float k = _cam.orthographicSize * 2f / Mathf.Max(1, Screen.height);
                    Vector3 next = _camAtDown - new Vector3(d.x * k, d.y * k, 0f);
                    next.x = Mathf.Clamp(next.x, -11.5f, 11.5f);
                    next.y = Mathf.Clamp(next.y, 0.5f, 15.5f);
                    next.z = -20f;
                    _cam.transform.position = next;
                }
            }

            if (Input.GetMouseButtonUp(0) && !_drag && !_downOnUi && !modalBlocked)
            {
                _world.HandleWorldClick(_cam, Input.mousePosition);
            }

            if (!modalBlocked)
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
