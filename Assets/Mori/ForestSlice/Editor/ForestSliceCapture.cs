using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mori.ForestSlice.EditorTools
{
    /// <summary>
    /// 《深山末日开荒》全套商业级漫画 UI 与交互体验 8 场景自动渲染截图验收器：
    /// 一次性验证主界面渐进HUD+多选打钩、漫画立绘对话、破框建造卡、半屏5格生产队列、居民需求、尸潮预警海报、推镜甩杆钓鱼、虚拟摇杆丧尸城开车堆货搜刮！
    /// </summary>
    public static class ForestSliceCapture
    {
        const string ScenePath = "Assets/Mori/ForestSlice/ForestSlice.unity";
        const string OutDir = "/Users/rosa/.gemini/antigravity/brain/1b3cbc05-17b2-44a9-ba22-41684be97e65";

        [MenuItem("末日囤货狂/深山样板/打开并运行", priority = 61)]
        public static void OpenAndPlay()
        {
            AssetDatabase.Refresh();
            if (!File.Exists(ScenePath)) Run();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
        }

        [MenuItem("末日囤货狂/深山样板/重建并截图", priority = 60)]
        public static void Run()
        {
            AssetDatabase.Refresh();
            Directory.CreateDirectory(OutDir);

            // 1. 开局渐进式 HUD + 多点打绿勾 ✔ 砍树捡木 + 白手套引导
            CaptureCustom(0, boot =>
            {
                if (boot.World.GroundPiles.Count > 0)
                {
                    var p = boot.World.GroundPiles[0];
                    p.queued = true;
                    boot.World.ChopQueue.Add(new ForestSurvivalWorld.ChopTask { isPile = true, pile = p, gx = p.gx, gy = p.gy });
                }
                boot.World.ChopQueue.Add(new ForestSurvivalWorld.ChopTask { isPile = false, chunkX = 6, chunkY = 3, gx = 19.5f, gy = 10.5f });
                boot.Sim.Add(SurvRes.Wood, 4, notifyToast: true);
            }, Path.Combine(OutDir, "ui_01_hud_chop_checks.png"));

            // 2. 双人半身漫画立绘剧情对话
            CaptureCustom(0, boot =>
            {
                boot.Sim.TriggerIntroDialog();
            }, Path.Combine(OutDir, "ui_02_comic_dialog.png"));

            // 3. 破框立体【建造 / 改造确认卡】（展示改装武装皮卡破框插画 + 绿勾条件）
            CaptureCustom(1, boot =>
            {
                boot.Sim.Add(SurvRes.Plank, 10);
                boot.Sim.Add(SurvRes.Scrap, 12);
                boot.Sim.Add(SurvRes.Parts, 4);
                boot.Hud.OpenBuildConfirmCard(boot.Sim.Find("relic_truck"));
            }, Path.Combine(OutDir, "ui_03_build_confirm_card.png"));

            // 4. 半屏【熏肉厨房 5 格生产队列抽屉】（验证真实细分食材：鲜蔬/鲜鱼/野味鲜肉对应菜谱！）
            CaptureCustom(2, boot =>
            {
                CampNode kit = boot.Sim.Find("kitchen");
                boot.Sim.TryEnqueueRecipe(kit, boot.Sim.KitchenRecipes[1], out _); // 香烤山溪鳟鱼
                boot.Sim.TryEnqueueRecipe(kit, boot.Sim.KitchenRecipes[3], out _); // 松木秘制熏肉
                boot.Hud.OpenWorkshopDrawer(kit);
                for (int i = 0; i < 8; i++) boot.Hud.Tick(0.12f);
            }, Path.Combine(OutDir, "ui_04_workshop_queue_drawer.png"));

            // 5. 半屏【满足居民需求】喂食升星面板
            CaptureCustom(1, boot =>
            {
                for (int i = 0; i < 10; i++) boot.Sim.Tick(0.5f);
                boot.Hud.OpenResidentDemandDrawer();
                for (int i = 0; i < 8; i++) boot.Hud.Tick(0.12f);
            }, Path.Combine(OutDir, "ui_05_resident_demand.png"));

            // 6. 全屏《尸潮来袭》漫画危机预警海报
            CaptureCustom(1, boot =>
            {
                boot.Hud.OpenCrisisPoster();
            }, Path.Combine(OutDir, "ui_06_crisis_horde_poster.png"));

            // 7. 南岸溪流【推镜实景甩杆钓鱼 + 弧形张力槽】
            CaptureCustom(2, boot =>
            {
                boot.Hud.EnterFishingMode();
                // 推进到鱼咬钩、弧形张力槽出现且弹出【收线 · 快速点击】状态！
                boot.Hud.CityArena?.SetJoystickInput(Vector2.zero);
                boot.World.CastFishingBobber();
                var hudType = typeof(ForestSurvivalHud);
                var mClick = hudType.GetMethod("OnFishingMainBtnClicked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                mClick?.Invoke(boot.Hud, null);
                for (int i = 0; i < 15; i++)
                {
                    boot.World.Tick(0.1f);
                    boot.Hud.Tick(0.1f);
                }
            }, Path.Combine(OutDir, "ui_07_fishing_pushin.png"));

            // 8. 丧尸城【虚拟摇杆实景驾驶武装皮卡 + 后车斗层层堆高搜刮奇观】
            CaptureCustom(2, boot =>
            {
                boot.Hud.LaunchCityRunForSite(0);
                boot.Hud.CityArena.AutoPilot = true;
                for (int i = 0; i < 42; i++)
                {
                    boot.Hud.Tick(0.1f);
                    if (boot.Hud.CityArena.CurrentCargo >= 36) break;
                }
            }, Path.Combine(OutDir, "ui_08_city_joystick_drive.png"));

            // 9. 新版荒诞美漫难民风骨骼主角【阿澈】四态实机特写验收！
            CaptureCustom(0, boot =>
            {
                var w = boot.World.Walkers[0];
                w.gx = 15.4f; w.gy = 14.6f; w.targetGx = w.gx; w.targetGy = w.gy;
                w.puppet.SetFacing(true);
                w.puppet.SetMode(HeroAnimMode.Idle);
                w.puppet.SyncWorldPos(ForestSliceView.Iso(w.gx, w.gy), w.shadowTr);
                boot.View.FrameCamera(new Vector2(15.2f, 14.7f), 3.1f);
            }, Path.Combine(OutDir, "hero_01_camp_idle.png"));

            CaptureCustom(0, boot =>
            {
                var w = boot.World.Walkers[0];
                w.gx = 15.1f; w.gy = 14.5f;
                w.targetGx = 16.5f; w.targetGy = 14.5f; w.hasManualMove = true;
                // 推进到 sin(phase * 12.5) ≈ 1.0 的最大跨步摆臂峰值帧！
                for (int i = 0; i < 5; i++) boot.World.Tick(0.022f);
                boot.View.FrameCamera(new Vector2(w.gx, w.gy + 0.25f), 2.25f);
            }, Path.Combine(OutDir, "hero_02_zoom_run.png"));

            CaptureCustom(0, boot =>
            {
                // 选取阳光开阔空地中央的原木堆 (GroundPiles[1]: 16.7, 15.6)，无前景树冠遮挡！
                var p = boot.World.GroundPiles.Count > 1 ? boot.World.GroundPiles[1] : boot.World.GroundPiles[0];
                p.queued = true;
                var task = new ForestSurvivalWorld.ChopTask { isPile = true, pile = p, gx = p.gx, gy = p.gy, claimedByWalker = 0 };
                boot.World.ChopQueue.Add(task);
                var w = boot.World.Walkers[0];
                w.gx = p.gx - 0.52f; w.gy = p.gy - 0.18f;
                w.assignedTask = task;
                w.puppet.SetFacing(true);
                w.puppet.SetMode(HeroAnimMode.Chop);
                // 注意 CaptureCustom 随后还会调用一次 boot.World.Tick(0.08f)！
                // 这里推进 0.245s，加上 CaptureCustom 的 0.08s = 0.325s (u = 0.58)，正中猛劈咬牙+青白刀光+木屑爆裂帧！
                for (int i = 0; i < 14; i++) boot.World.Tick(0.0175f);
                boot.View.FrameCamera(new Vector2(w.gx + 0.22f, w.gy + 0.22f), 2.2f);
            }, Path.Combine(OutDir, "hero_03_zoom_chop_cleave.png"));

            CaptureCustom(0, boot =>
            {
                boot.Sim.TriggerIntroDialog();
            }, Path.Combine(OutDir, "hero_04_comic_bust_dialog.png"));

            CaptureCustom(0, boot =>
            {
                var w = boot.World.Walkers[0];
                w.gx = 16.0f; w.gy = 15.1f;
                w.targetGx = 14.9f; w.targetGy = 14.5f;
                w.carryHomeTimer = 1.2f;
                w.puppet.SetCarrySprite(ForestComicUiSkin.ResIcon(SurvRes.Wood));
                w.puppet.SetFacing(false);
                w.puppet.SetMode(HeroAnimMode.CarryRun);
                for (int i = 0; i < 5; i++) boot.World.Tick(0.022f);
                boot.View.FrameCamera(new Vector2(w.gx, w.gy + 0.30f), 2.25f);
            }, Path.Combine(OutDir, "hero_05_carry_overhead.png"));

            // 10. 新人物【小棠】实机同框 + 扳手作业 + 换装皮肤档位面板验收！
            CaptureCustom(0, boot =>
            {
                var ache = boot.World.Walkers[0];
                ache.gx = 15.15f; ache.gy = 14.55f; ache.targetGx = ache.gx; ache.targetGy = ache.gy;
                ache.puppet.SetFacing(true);
                ache.puppet.SetMode(HeroAnimMode.Idle);
                ache.puppet.SyncWorldPos(ForestSliceView.Iso(ache.gx, ache.gy), ache.shadowTr);

                var xt = boot.World.Walkers[1];
                xt.gx = 15.95f; xt.gy = 14.35f; xt.targetGx = xt.gx; xt.targetGy = xt.gy;
                xt.puppet.SetFacing(false);
                xt.puppet.SetMode(HeroAnimMode.Idle);
                xt.puppet.SyncWorldPos(ForestSliceView.Iso(xt.gx, xt.gy), xt.shadowTr);

                boot.View.FrameCamera(new Vector2(15.55f, 14.55f), 2.6f);
            }, Path.Combine(OutDir, "xt_01_camp_duo_idle.png"));

            CaptureCustom(0, boot =>
            {
                // 小棠挥动铜线大管钳敲打山道锈铁残骸 + 阿澈高举原木路过！
                ForestSurvivalWorld.GroundPile scrap = null;
                for (int i = 0; i < boot.World.GroundPiles.Count; i++)
                    if (boot.World.GroundPiles[i].kind == ForestSurvivalWorld.FieldKind.ScrapWreck) { scrap = boot.World.GroundPiles[i]; break; }
                if (scrap == null) scrap = boot.World.GroundPiles[0];
                scrap.queued = true;
                var task = new ForestSurvivalWorld.ChopTask { isPile = true, pile = scrap, gx = scrap.gx, gy = scrap.gy, claimedByWalker = 1 };
                boot.World.ChopQueue.Add(task);

                var xt = boot.World.Walkers[1];
                xt.gx = scrap.gx + 0.50f; xt.gy = scrap.gy - 0.16f;
                xt.assignedTask = task;
                xt.puppet.SetFacing(false);
                xt.puppet.SetMode(HeroAnimMode.Chop);
                for (int i = 0; i < 11; i++) boot.World.Tick(0.0175f);

                boot.View.FrameCamera(new Vector2(xt.gx - 0.15f, xt.gy + 0.25f), 2.25f);
            }, Path.Combine(OutDir, "xt_02_wrench_action.png"));

            CaptureCustom(0, boot =>
            {
                boot.Sim.TriggerIntroDialog();
                // 推进到第2句，展示【阿澈】与【小棠】双人半身立绘对视！
                var mAdv = typeof(ForestSurvivalHud).GetMethod("AdvanceComicDialog", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                mAdv?.Invoke(boot.Hud, null);
            }, Path.Combine(OutDir, "xt_03_duo_dialog.png"));

            CaptureCustom(1, boot =>
            {
                // Stage 1 (木屋 Lv.2) 自动解锁第2档【加固皮衣 / 电工护具】皮肤，展示委任与换装档位面板！
                boot.Hud.OpenHeroAssignModal();
            }, Path.Combine(OutDir, "xt_04_hero_skin_modal.png"));

            CaptureCustom(0, boot =>
            {
                // 对向迎面交会测试（与原穿模截图完全相同的对冲路径，验证两人自动横向错身互避，绝不叠在一起！）
                var ache = boot.World.Walkers[0];
                ache.gx = 15.0f; ache.gy = 14.5f; ache.targetGx = 16.5f; ache.targetGy = 14.5f;
                ache.hasManualMove = false;
                var xt = boot.World.Walkers[1];
                xt.gx = 15.9f; xt.gy = 14.2f; xt.targetGx = 14.2f; xt.targetGy = 14.2f;
                xt.hasManualMove = true;
                for (int i = 0; i < 14; i++) boot.World.Tick(0.025f);
                boot.View.FrameCamera(new Vector2(15.45f, 14.4f), 2.55f);
            }, Path.Combine(OutDir, "xt_05_walk_natural.png"));

            CaptureCustom(0, boot =>
            {
                // 全营地鸟瞰全景：验证帐篷、篝火、锯木棚、原木堆、浆果丛、锈铁残骸与双角色 100% 零穿模净空布局！
                boot.View.FrameCamera(new Vector2(15.3f, 15.6f), 4.2f);
            }, Path.Combine(OutDir, "col_01_camp_clean_layout.png"));

            CaptureCustom(0, boot =>
            {
                // 强行朝篝火正中心连续按 WASD 冲撞 30 帧：验证实体碰撞边界阻挡与横向顺滑绕开，绝不踩到或挡住篝火贴图！
                var ache = boot.World.Walkers[0];
                ache.gx = 14.4f; ache.gy = 17.0f;
                Vector2 fireCenter = ForestSliceView.Iso(13.5f, 17.3f);
                for (int i = 0; i < 30; i++)
                {
                    Vector2 cur = ForestSliceView.Iso(ache.gx, ache.gy);
                    boot.World.DriveHeroByInput(fireCenter - cur, 0.03f);
                    boot.World.Tick(0.02f);
                }
                boot.View.FrameCamera(new Vector2(13.9f, 17.3f), 2.55f);
            }, Path.Combine(OutDir, "col_02_obstacle_block_test.png"));

            // 11. 全员 5 人骨骼小队（阿澈、小棠、老陈头、林砚、大锤）同屏合影与全员委任面板验收！
            CaptureCustom(2, boot =>
            {
                Vector2[] spots =
                {
                    new Vector2(12.2f, 15.1f), // 阿澈（最左）
                    new Vector2(15.1f, 15.1f), // 小棠（中左）
                    new Vector2(15.5f, 16.5f), // 老陈头（后排中）
                    new Vector2(16.4f, 14.7f), // 林砚（中右）
                    new Vector2(16.9f, 16.3f), // 大锤（后排右）
                };
                for (int i = 0; i < boot.World.Walkers.Count && i < spots.Length; i++)
                {
                    var w = boot.World.Walkers[i];
                    w.gx = spots[i].x; w.gy = spots[i].y;
                    w.targetGx = w.gx; w.targetGy = w.gy;
                    w.puppet.SetFacing(i < 2);
                    w.puppet.SetMode(HeroAnimMode.Idle);
                    w.puppet.SyncWorldPos(ForestSliceView.Iso(w.gx, w.gy), w.shadowTr);
                }
                boot.View.FrameCamera(new Vector2(15.0f, 15.0f), 3.6f);
            }, Path.Combine(OutDir, "cast_01_all5_lineup.png"));

            CaptureCustom(2, boot =>
            {
                boot.Hud.OpenHeroAssignModal();
            }, Path.Combine(OutDir, "cast_02_hero_assign_5cast.png"));

            // 12. 页面重规划专项验收：开阔中央广场 + 缩小画面无黑框 + 防误触砍树确认与固定工位巡逻！
            CaptureCustom(0, boot =>
            {
                // 默认 Stage 0 视野：展示扩大近 2 倍的开阔草坪、建筑合理分区、无杂乱气泡墙、中央留空操作区
            }, Path.Combine(OutDir, "replan_01_spacious_camp_default.png"));

            CaptureCustom(0, boot =>
            {
                // 模拟鼠标滚轮直接缩小镜头（不调 FrameCamera，直接改 cam.orthographicSize = 8.4f 复现原黑框场景！）
                Camera mainCam = boot.Hud.GetComponentInParent<ForestSliceBoot>().GetComponent<ForestSliceView>() != null
                    ? Camera.allCameras[0] : null;
                var camField = typeof(ForestSliceBoot).GetField("_cam", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Camera bCam = (Camera)camField.GetValue(boot);
                bCam.orthographicSize = 8.4f;
                boot.View.SyncCameraOverlays();
                boot.Sim.Add(SurvRes.Wood, 16);
                boot.Sim.Add(SurvRes.Veggie, 3);
                boot.Sim.Add(SurvRes.Scrap, 5);
            }, Path.Combine(OutDir, "replan_02_zoomed_out_clean.png"));

            CaptureCustom(0, boot =>
            {
                // 连续推进 20 秒真实帧（800 帧），检验伙伴在各自固定直线上自然慢走/待机呼吸零卡死，且点林缘先弹【砍伐扩地】确认泡！
                for (int i = 0; i < 800; i++)
                {
                    boot.World.Tick(0.025f);
                }
                boot.World.HasPendingChop = true;
                boot.World.PendingChopCx = 6;
                boot.World.PendingChopCy = 7;
                boot.World.PendingChopTimer = 3.5f;
            }, Path.Combine(OutDir, "replan_03_chop_confirm_and_patrol.png"));

            // 13. V2 深度经济大循环与五大作坊专项验收！
            CaptureCustom(1, boot =>
            {
                // 展示【物资仓库 Lv.2 -> Lv.3】升级卡：同时检验 当前人口 / 铜币 / 工程手稿 / 铁钉铆件 红叉 ✖ 与 [前往]/[获取]/[去生产] 按钮！
                boot.Sim.CampLevel = 5;
                boot.Sim.Coins = 380; // 故意少于 650 展示 [获取]
                boot.Sim.Add(SurvRes.Nails, -100); // 故意置 0 展示 [去生产]
                boot.Hud.OpenBuildConfirmCard(boot.Sim.Find("warehouse"));
            }, Path.Combine(OutDir, "econ_01_warehouse_build_card_jumps.png"));

            CaptureCustom(1, boot =>
            {
                // 展示水世界同款【工程手稿 · 获取途径】多路跳转弹窗（叠在建造卡上方！）
                boot.Sim.CampLevel = 5;
                boot.Hud.OpenBuildConfirmCard(boot.Sim.Find("warehouse"));
                boot.Hud.OpenSourceRouteForCondition(CondKind.Resource, SurvRes.Blueprint);
            }, Path.Combine(OutDir, "econ_02_source_route_popup.png"));

            CaptureCustom(2, boot =>
            {
                // 展示新增【裁缝防具坊】半屏队列抽屉（麻布衣 / 猎皮靴 / 御寒皮衣 / 钢板护甲，选中加绒御寒皮衣 3 原料！）
                CampNode tailor = boot.Sim.Find("tailor");
                boot.Sim.Add(SurvRes.Cloth, 8);
                boot.Sim.Add(SurvRes.Pelt, 8);
                boot.Sim.Add(SurvRes.LinenShirt, 3);
                boot.Sim.TryEnqueueRecipe(tailor, boot.Sim.TailorRecipes[0], out _);
                boot.Hud.OpenWorkshopDrawer(tailor, selectRecipe: 2);
            }, Path.Combine(OutDir, "econ_03_tailor_workshop_drawer.png"));

            CaptureCustom(2, boot =>
            {
                // 展示三槽并行【四维居民生计订单板】（温饱餐饮单 + 御寒穿戴单 + 伤病草药单 + 5单宝箱进度！）
                boot.Hud.OpenResidentDemandDrawer();
            }, Path.Combine(OutDir, "econ_04_multi_orders_board.png"));

            CaptureCustom(2, boot =>
            {
                // 展示【山口行商驿站】大宗贸易面板（收购御寒皮衣/急救草药/松木熏肉 -> 结算大额铜币与工程手稿！）
                boot.Hud.OpenCaravanModal();
            }, Path.Combine(OutDir, "econ_05_caravan_trade_modal.png"));

            CaptureCustom(2, boot =>
            {
                // 展示 V2 全营地鸟瞰：避难所木堡、供暖塔、物资仓库、1~3号民居、锯木/锻造/裁缝/草药/炊事五大作坊分区落位！
                boot.View.FrameCamera(new Vector2(16.0f, 16.0f), 7.6f);
            }, Path.Combine(OutDir, "econ_06_full_camp_v2_buildings.png"));

            // 16. 《末日囤货狂》实机启动 Loading 页（含立体标题 + 满载皮卡主视觉 + 装车进度条 + 底部完整《健康游戏忠告》全文！）
            CaptureCustom(0, boot =>
            {
                boot.Hud.ShowBootLoadingPreview(0.72f);
            }, Path.Combine(OutDir, "loading_01_ingame_screen.png"));

            // 17. V3 全新三大模块验收（居民生活生态 + 水世界式6岗位多槽委任&升星养成 + 山口波次迎击&丧尸城车载射击双战斗！）
            CaptureCustom(1, boot =>
            {
                // V3-01：营地中央篝火广场【4类避难居民】自然烤火散步 + 头顶生计订单气泡 + 英雄岗位巡逻分工同框！
                for (int i = 0; i < 36; i++) boot.World.Tick(0.05f);
                if (boot.World.CivilianVillagers.Count > 2)
                {
                    boot.World.CivilianVillagers[2].chatterText = "火炉真暖和，多亏营地收留咱！";
                    boot.World.CivilianVillagers[2].chatterTimer = 5f;
                }
                boot.View.FrameCamera(new Vector2(15.2f, 16.0f), 4.6f);
            }, Path.Combine(OutDir, "v3_01_camp_villagers_and_orders.png"));

            CaptureCustom(1, boot =>
            {
                // V3-02：水世界同款【英雄委任 (6大岗位多槽)】页签（展示已解锁品质头像槽、锁头槽与“玩家X级解锁”岗位卡！）
                boot.Hud.OpenHeroModal(0);
            }, Path.Combine(OutDir, "v3_02_hero_delegation_6posts.png"));

            CaptureCustom(2, boot =>
            {
                // V3-03：【骨干养成 · 升星换装】页签（6名传说/史诗/稀有英雄、英雄勋章升星、铜币口粮升级、3档防具加成！）
                boot.Hud.OpenHeroModal(1);
            }, Path.Combine(OutDir, "v3_03_hero_growth_stars_skins.png"));

            CaptureCustom(2, boot =>
            {
                // V3-04：A1【山口防线 · 第3波首领迎击战】（南山口拒马血条 + 变异尸王血条 + 汽油燃烧瓶火海 + 漫天飞箭管钳弹道 + 底部3怒气大招卡！）
                boot.Sim.CombatSquadIds.Clear();
                boot.Sim.CombatSquadIds.AddRange(new[] { "dachui", "xiaotang", "linyan" });
                boot.World.AutoCastUlt = false;
                boot.Hud.StartWaveDefenseBattle();
                for (int i = 0; i < 68; i++)
                {
                    boot.World.Tick(0.05f);
                    boot.Hud.Tick(0.05f);
                }
                for (int s = 0; s < boot.World.SquadSlots.Count; s++)
                    boot.World.SquadSlots[s].rage = 100f;
                int xtSlot = 0;
                for (int s = 0; s < boot.World.SquadSlots.Count; s++)
                    if (boot.World.SquadSlots[s].hero.id == "xiaotang") xtSlot = s;
                boot.World.TriggerSquadUltimate(xtSlot);
                for (int i = 0; i < 4; i++)
                {
                    boot.World.Tick(0.03f);
                    boot.Hud.Tick(0.03f);
                }
            }, Path.Combine(OutDir, "v3_04_mountain_wave_defense_combat.png"));

            CaptureCustom(2, boot =>
            {
                // V3-05：A2【丧尸城武装皮卡随车集火 + 氮气冲撞 + 尸王守护黄金空投箱】实机画面！
                boot.Hud.LaunchCityRunForSite(1);
                boot.Hud.CityArena.SetJoystickInput(new Vector2(0.15f, 0.92f));
                for (int i = 0; i < 10; i++)
                {
                    boot.Hud.Tick(0.06f);
                }
                boot.Hud.CityArena.TriggerNitroBoost();
                boot.Hud.CityArena.SetJoystickInput(new Vector2(0.45f, 0.65f));
                for (int i = 0; i < 4; i++)
                {
                    boot.Hud.Tick(0.04f);
                }
            }, Path.Combine(OutDir, "v3_05_city_truck_mounted_combat.png"));

            // 保存默认可玩场景（startStage = 0 新手完整引导开局）
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var c = new GameObject("MainCamera", typeof(Camera));
            c.tag = "MainCamera";
            new GameObject("ForestSliceBoot", typeof(ForestSliceBoot));
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            Debug.Log("[ForestSlice] All 8 UI/UX overhaul screenshots captured -> " + OutDir);
        }

        public static void VerifyMovementSmoothness()
        {
            AssetDatabase.Refresh();
            Directory.CreateDirectory(OutDir);
            var report = new System.Text.StringBuilder();

            CaptureCustom(2, boot =>
            {
                var w = boot.World.Walkers[0];
                Vector2[] goals =
                {
                    new Vector2(12.18f, 15.6f), // 1. 西南前排树根边缘（旧版卡在 13.35）
                    new Vector2(15.6f, 12.18f), // 2. 东南前排树根边缘（旧版卡在 13.35）
                    new Vector2(12.20f, 12.20f), // 3. 最南端林缘角落（验证树冠半透明）
                    new Vector2(20.78f, 18.4f), // 4. 斜穿整座满建筑营地直达东北林缘
                    new Vector2(14.4f, 20.78f), // 5. 绕过仓库与供暖塔直达西北林缘
                    new Vector2(19.2f, 13.5f),  // 6. 斜穿锯木坊与炊事房回东南侧
                };

                bool allPassed = true;
                for (int gIdx = 0; gIdx < goals.Length; gIdx++)
                {
                    // 直接模拟地面点击移动
                    w.targetGx = goals[gIdx].x;
                    w.targetGy = goals[gIdx].y;
                    w.hasManualMove = true;
                    w.avoidSide = 0;
                    w.stuckTimer = 0f;

                    int frames = 0;
                    for (; frames < 360; frames++)
                    {
                        boot.World.Tick(1f / 60f);
                        float rem = Vector2.Distance(ForestSliceView.Iso(w.gx, w.gy), ForestSliceView.Iso(w.targetGx, w.targetGy));
                        if (rem < 0.08f && !w.hasManualMove) break;
                    }
                    float endErr = Vector2.Distance(new Vector2(w.gx, w.gy), new Vector2(w.targetGx, w.targetGy));
                    bool ok = endErr < 0.25f && frames < 300;
                    if (!ok) allPassed = false;
                    report.AppendLine($"LEG_{gIdx + 1}: goal=({goals[gIdx].x:F2},{goals[gIdx].y:F2}) end=({w.gx:F2},{w.gy:F2}) err={endErr:F3} frames={frames} ok={ok}");
                }

                // 测试 WASD 斜推林缘滑行（贴着 Stage 2 西侧林缘 x=9.15 斜推 60 帧，应顺畅滑行而非原地锁死！）
                w.gx = 9.55f; w.gy = 14.0f; w.targetGx = w.gx; w.targetGy = w.gy;
                Vector2 startSlide = ForestSliceView.Iso(w.gx, w.gy);
                for (int f = 0; f < 60; f++)
                {
                    boot.World.DriveHeroByInput(new Vector2(-0.85f, 0.25f), 1f / 60f);
                    boot.World.Tick(1f / 60f);
                }
                Vector2 endSlide = ForestSliceView.Iso(w.gx, w.gy);
                float slideDist = Vector2.Distance(startSlide, endSlide);
                bool slideOk = slideDist > 0.65f && w.gx < 9.35f;
                if (!slideOk) allPassed = false;
                report.AppendLine($"WASD_EDGE_SLIDE: endGrid=({w.gx:F2},{w.gy:F2}) slideDist={slideDist:F2} mode={w.puppet.Mode} ok={slideOk}");
                report.AppendLine($"ALL_PASSED={allPassed}");

                // 定格主角站在 Stage 2 最南侧林缘树冠后方（展示紧贴树根 + 前排树冠自动半透明 + 严密 Y-Sort！）
                w.gx = 9.20f; w.gy = 14.5f; w.targetGx = w.gx; w.targetGy = w.gy;
                for (int f = 0; f < 18; f++) boot.World.Tick(0.05f);
                boot.View.FrameCamera(new Vector2(10.6f, 15.0f), 3.6f);
            }, Path.Combine(OutDir, "move_01_south_tree_edge_canopy_fade.png"));

            CaptureCustom(0, boot =>
            {
                var w = boot.World.Walkers[0];
                w.gx = 12.18f; w.gy = 16.0f; w.targetGx = w.gx; w.targetGy = w.gy;
                w.puppet.SetFacing(false);
                for (int f = 0; f < 16; f++) boot.World.Tick(0.05f);
                boot.View.FrameCamera(new Vector2(13.8f, 16.4f), 3.6f);
            }, Path.Combine(OutDir, "move_02_back_tree_edge_close.png"));

            // 重新保存干净的 ForestSlice.unity 场景
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var c = new GameObject("MainCamera", typeof(Camera));
            c.tag = "MainCamera";
            new GameObject("ForestSliceBoot", typeof(ForestSliceBoot));
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);

            File.WriteAllText("/tmp/mori_move_verify.txt", report.ToString());
            Debug.Log("[MoveVerify]\n" + report);
        }

        public static void VerifyEcoFarmAndRanch()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Directory.CreateDirectory(OutDir);
            var report = new System.Text.StringBuilder();

            // 1. 主世界俯瞰：同时展示西南【生态选种农田】与紧邻【山林畜牧栏】+ 气泡 + 顶部温饱士气胶囊
            CaptureCustom(1, boot =>
            {
                var sim = boot.Sim;
                sim.Add(SurvRes.HerbPoultice, 2); // 激活【士气沸腾 +32%】
                sim.FarmPlots[0].phase = FarmPhase.Ripe;
                sim.FarmPlots[0].seedKind = CropSeedKind.Veggie;
                sim.RanchPens[0].readyToCollect = true;
                sim.RanchPens[0].animal = RanchAnimalKind.SnowRabbit;
                sim.SyncFarmVisualPhase();
                boot.World.RefreshNodeVisual(sim.Find("farm_a"));
                boot.World.RefreshNodeVisual(sim.Find("beast_ranch"));
                boot.View.FrameCamera(new Vector2(12.2f, 14.2f), 5.0f);
            }, Path.Combine(OutDir, "eco_v4_01_camp_farm_and_ranch.png"));

            // 2. 半屏【生态选种农田 Lv.3 (4/4垄)】选种 + 堆肥催熟 + 滴灌/供暖/士气三联横幅特写
            CaptureCustom(2, boot =>
            {
                var sim = boot.Sim;
                sim.Add(SurvRes.Compost, 4);
                sim.Add(SurvRes.SeedVeggie, 6);
                sim.Add(SurvRes.SeedFlax, 5);
                sim.Add(SurvRes.SeedHerb, 4);
                sim.Add(SurvRes.SeedGolden, 2);

                sim.FarmPlots[0].phase = FarmPhase.Ripe;
                sim.FarmPlots[0].seedKind = CropSeedKind.Golden;
                sim.FarmPlots[0].fertilized = true;

                sim.FarmPlots[1].phase = FarmPhase.Growing;
                sim.FarmPlots[1].seedKind = CropSeedKind.Herb;
                sim.FarmPlots[1].watered = true;
                sim.FarmPlots[1].fertilized = true;
                sim.FarmPlots[1].timer = 6.2f;
                sim.FarmPlots[1].duration = 9.5f;

                sim.FarmPlots[2].phase = FarmPhase.Growing;
                sim.FarmPlots[2].seedKind = CropSeedKind.Flax;
                sim.FarmPlots[2].watered = true;
                sim.FarmPlots[2].fertilized = false;
                sim.FarmPlots[2].timer = 3.2f;
                sim.FarmPlots[2].duration = 8.5f;

                sim.FarmPlots[3].phase = FarmPhase.DryTilled;
                sim.SyncFarmVisualPhase();

                boot.Hud.OpenFarmDrawer(CropSeedKind.Herb);
            }, Path.Combine(OutDir, "eco_v4_02_seed_farm_drawer.png"));

            // 3. 半屏【山林畜牧栏 Lv.2 (3/4栏)】投喂鲜蔬+井水 -> 产肉/皮/堆肥 + 反哺农田按钮特写
            CaptureCustom(2, boot =>
            {
                var sim = boot.Sim;
                sim.Add(SurvRes.Veggie, 12);
                sim.Add(SurvRes.Water, 8);
                sim.Add(SurvRes.Compost, 3);
                sim.RanchPens[0].animal = RanchAnimalKind.SnowRabbit;
                sim.RanchPens[0].readyToCollect = true;
                sim.RanchPens[0].feeding = false;

                sim.RanchPens[1].animal = RanchAnimalKind.WildBoar;
                sim.RanchPens[1].feeding = true;
                sim.RanchPens[1].readyToCollect = false;
                sim.RanchPens[1].timer = 5.4f;
                sim.RanchPens[1].duration = 9.5f;

                sim.RanchPens[2].animal = RanchAnimalKind.WildBoar;
                sim.RanchPens[2].feeding = false;
                sim.RanchPens[2].readyToCollect = false;

                boot.Hud.OpenRanchDrawer();

                // 闭环自动化断言测试
                int compBefore = sim.Get(SurvRes.Compost);
                int meatBefore = sim.Get(SurvRes.Meat);
                int peltBefore = sim.Get(SurvRes.Pelt);
                bool colOk = sim.TryCollectRanchPen(0, out string rMsg);
                bool gotAllThree = sim.Get(SurvRes.Compost) > compBefore && sim.Get(SurvRes.Meat) > meatBefore && sim.Get(SurvRes.Pelt) > peltBefore;

                // 重新置回展示态供截图渲染
                sim.RanchPens[0].readyToCollect = true;
                sim.RanchPens[0].feeding = false;
                boot.Hud.OpenRanchDrawer();

                report.AppendLine($"AUTO_IRRIGATION={sim.HasAutoIrrigation}");
                report.AppendLine($"GREENHOUSE_PCT={sim.GreenhouseBonusPct}");
                report.AppendLine($"MORALE_TIER={sim.MoraleTier} MUL={sim.CampMoraleMultiplier:F2}");
                report.AppendLine($"RANCH_COLLECT_OK={colOk} GOT_MEAT_PELT_COMPOST={gotAllThree} MSG={rMsg}");
                report.AppendLine($"ECO_ALL_PASSED={sim.HasAutoIrrigation && sim.GreenhouseBonusPct >= 15 && sim.MoraleTier >= 1 && colOk && gotAllThree}");
            }, Path.Combine(OutDir, "eco_v4_03_beast_ranch_drawer.png"));

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var c = new GameObject("MainCamera", typeof(Camera));
            c.tag = "MainCamera";
            new GameObject("ForestSliceBoot", typeof(ForestSliceBoot));
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);

            File.WriteAllText("/tmp/mori_eco_verify.txt", report.ToString());
            Debug.Log("[EcoVerify]\n" + report);
        }

        [MenuItem("末日囤货狂/深山样板/验收Lv1-15成长与GM调试台", priority = 62)]
        public static void VerifyProgressionAndGm()
        {
            const string VerifierOutDir = "/Users/rosa/.gemini/antigravity/brain/9f490f01-e9ab-423e-a35d-4bb770856fbc";
            const string Verifier2OutDir = "/Users/rosa/.gemini/antigravity/brain/05187003-deaa-4573-9bd2-59ba8a299e18";
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Directory.CreateDirectory(VerifierOutDir);
            Directory.CreateDirectory(Verifier2OutDir);
            Directory.CreateDirectory(OutDir);

            var report = new System.Text.StringBuilder();
            bool allPassed = true;

            // =================================================================
            // 1. 静态数值表与 Lv.1 -> Lv.15 全链路零死锁 DAG 仿真校验
            // =================================================================
            var dagSim = new ForestSurvivalSim();
            if (ForestSurvivalSim.MaxCampLevel != 15)
            {
                allPassed = false;
                report.AppendLine($"FAIL: MaxCampLevel={ForestSurvivalSim.MaxCampLevel} (expected 15)");
            }

            // 检查所有建筑初始解锁等级与各级升阶解锁等级均在 Lv.1 ~ Lv.15 之间
            int checkedNodes = 0;
            int maxSeenUnlockLv = 0;
            for (int i = 0; i < dagSim.Nodes.Count; i++)
            {
                CampNode n = dagSim.Nodes[i];
                checkedNodes++;
                if (n.unlockPlayerLv < 1 || n.unlockPlayerLv > ForestSurvivalSim.MaxCampLevel)
                {
                    allPassed = false;
                    report.AppendLine($"FAIL_NODE_UNLOCK: {n.id} unlockPlayerLv={n.unlockPlayerLv}");
                }
                if (n.unlockPlayerLv > maxSeenUnlockLv) maxSeenUnlockLv = n.unlockPlayerLv;
            }

            // 模拟纯种田玩家从 Lv.1 逐级推进至 Lv.15：仅允许使用当级已解锁建筑产出的材料与当级可达人口
            var map = new ForestMap();
            map.ClearRectInstant(2, 1, 27, 27);
            for (int lv = 1; lv <= ForestSurvivalSim.MaxCampLevel; lv++)
            {
                dagSim.GmSetPlayerLevel(lv, map);
                bool progressed = true;
                int guard = 0;
                while (progressed && guard++ < 64)
                {
                    progressed = false;
                    // 计算当前已建成建筑在当前星级下可合法产出的全部资源集合
                    var producible = ComputeProducibleResources(dagSim);

                    for (int i = 0; i < dagSim.Nodes.Count; i++)
                    {
                        CampNode n = dagSim.Nodes[i];
                        if (n.kind == NodeKind.RelicLoot)
                        {
                            if (n.state == NodeState.Discovered && dagSim.CampLevel >= n.unlockPlayerLv)
                            {
                                dagSim.StartBuildOrUpgrade(n, out _);
                                progressed = true;
                            }
                            continue;
                        }
                        if (n.state == NodeState.Active && n.level >= n.maxLevel) continue;

                        int reqLv = dagSim.GetRequiredPlayerLvForNext(n);
                        if (reqLv > ForestSurvivalSim.MaxCampLevel)
                        {
                            allPassed = false;
                            report.AppendLine($"FAIL_UPGRADE_LV_OVER_15: {n.id} curLv={n.level} reqLv={reqLv}");
                        }
                        if (reqLv > maxSeenUnlockLv) maxSeenUnlockLv = reqLv;
                        if (reqLv > lv) continue;

                        dagSim.GetNextUpgradeSpec(n, out int coinCost, out int reqPopCur, out int reqPopCap, out CostPair[] mats);
                        if (mats == null) continue;

                        // 校验人口门槛与仓库单品上限是否满足
                        if (reqPopCap > 0 && dagSim.PopCap < reqPopCap) continue;
                        if (reqPopCur > 0 && dagSim.CurrentPop < reqPopCur) continue;

                        bool matsObtainable = true;
                        for (int m = 0; m < mats.Length; m++)
                        {
                            if (!producible.Contains(mats[m].res))
                            {
                                matsObtainable = false;
                                break;
                            }
                            if (ForestSurvivalSim.IsWarehouseCapped(mats[m].res) && mats[m].amount > dagSim.StorageCap)
                            {
                                matsObtainable = false;
                                allPassed = false;
                                report.AppendLine($"FAIL_STORAGE_CAP_DEADLOCK: lv={lv} node={n.id} res={mats[m].res} need={mats[m].amount} cap={dagSim.StorageCap}");
                                break;
                            }
                        }
                        if (!matsObtainable) continue;

                        // 补足当级可合法自循环产出的铜币与材料并完成建造/升阶
                        dagSim.Coins = Mathf.Max(dagSim.Coins, coinCost + 200);
                        for (int m = 0; m < mats.Length; m++)
                        {
                            int curOwn = dagSim.Get(mats[m].res);
                            if (curOwn < mats[m].amount)
                                dagSim.Add(mats[m].res, mats[m].amount - curOwn);
                        }

                        if (!dagSim.StartBuildOrUpgrade(n, out string failReason))
                        {
                            allPassed = false;
                            report.AppendLine($"FAIL_BUILD_START: lv={lv} node={n.id} reason={failReason}");
                            continue;
                        }
                        dagSim.SpeedUpNodeBuild(n);
                        dagSim.CompleteHammerNode(n);
                        // 防止升阶经验把 CampLevel 提前顶高，固定在当前测试星级 lv 验证严格 DAG
                        dagSim.CampLevel = lv;
                        dagSim.CampXp = 0;
                        progressed = true;
                    }
                }

                // 在每个等级结束时，确认所有 reqLv <= lv 的建筑/升阶均已顺利解锁建成（零互锁残留！）
                for (int i = 0; i < dagSim.Nodes.Count; i++)
                {
                    CampNode n = dagSim.Nodes[i];
                    if (n.kind == NodeKind.RelicLoot) continue;
                    if (n.state == NodeState.Active && n.level >= n.maxLevel) continue;
                    int reqLv = dagSim.GetRequiredPlayerLvForNext(n);
                    if (reqLv <= lv)
                    {
                        allPassed = false;
                        dagSim.GetNextUpgradeSpec(n, out _, out int rpc, out int rpp, out CostPair[] ms);
                        report.AppendLine($"DEADLOCK_AT_LV_{lv}: node={n.id} state={n.state} curBuildingLv={n.level} reqPopCur={rpc}/{dagSim.CurrentPop} reqPopCap={rpp}/{dagSim.PopCap} costs={ForestSurvivalSim.FormatCosts(ms)}");
                    }
                }
            }

            int maxedBuildings = 0;
            int upgradeableBuildings = 0;
            for (int i = 0; i < dagSim.Nodes.Count; i++)
            {
                CampNode n = dagSim.Nodes[i];
                if (n.kind == NodeKind.RelicLoot) continue;
                upgradeableBuildings++;
                if (n.state == NodeState.Active && n.level == n.maxLevel)
                    maxedBuildings++;
            }
            bool dagOk = maxedBuildings == upgradeableBuildings && dagSim.CurrentPop == 46 && dagSim.PopCap == 46 && dagSim.StorageCap == 1200;
            if (!dagOk) allPassed = false;
            report.AppendLine($"DAG_LV1_15: checkedNodes={checkedNodes} maxed={maxedBuildings}/{upgradeableBuildings} maxUnlockLv={maxSeenUnlockLv} finalPop={dagSim.CurrentPop}/{dagSim.PopCap} storageCap={dagSim.StorageCap} ok={dagOk}");

            // =================================================================
            // 2. 22 步全流程新手主线引导任务链 (step 0..21) 推进与奖励校验
            // =================================================================
            var qSim = new ForestSurvivalSim();
            var qMap = new ForestMap();
            qMap.ClearRectInstant(2, 1, 27, 27);
            bool questsOk = true;
            for (int step = 0; step < 22; step++)
            {
                if (qSim.QuestIndex != step)
                {
                    questsOk = false;
                    report.AppendLine($"QUEST_IDX_MISMATCH: expected={step} actual={qSim.QuestIndex}");
                    break;
                }
                var q0 = qSim.GetCurrentQuest();
                if (string.IsNullOrEmpty(q0.text) || !Enum.IsDefined(typeof(ForestSurvivalSim.QuestActionKind), q0.actionKind))
                {
                    questsOk = false;
                    report.AppendLine($"QUEST_INVALID_META: step={step} text='{q0.text}' action={q0.actionKind}");
                }
                if (!string.IsNullOrEmpty(q0.targetNodeId) && qSim.Find(q0.targetNodeId) == null)
                {
                    questsOk = false;
                    report.AppendLine($"QUEST_BAD_TARGET_NODE: step={step} targetNodeId={q0.targetNodeId}");
                }

                FulfillQuestStepForTest(qSim, qMap, step);
                var q1 = qSim.GetCurrentQuest();
                if (!q1.done)
                {
                    questsOk = false;
                    report.AppendLine($"QUEST_NOT_DONE: step={step} text='{q1.text}' cur={q1.cur}/{q1.max}");
                    break;
                }
                if (!qSim.ClaimCurrentQuestReward(out string rwMsg) || string.IsNullOrEmpty(rwMsg))
                {
                    questsOk = false;
                    report.AppendLine($"QUEST_CLAIM_FAIL: step={step}");
                    break;
                }
            }
            var postFinalQ = qSim.GetCurrentQuest();
            if (qSim.QuestIndex != 22 || string.IsNullOrEmpty(postFinalQ.text))
                questsOk = false;
            if (!questsOk) allPassed = false;
            report.AppendLine($"QUESTS_0_21: finalQuestIndex={qSim.QuestIndex} endText='{postFinalQ.text}' ok={questsOk}");

            // =================================================================
            // 3. 实机渲染验收截图 1~3 + GM 调试台与任务点击路由真机校验
            // =================================================================
            // Screenshot 1: Stage 0 Lv.1 新手开局白手套任务引导 + 右侧 [GM调试] 悬浮胶囊
            string p1 = Path.Combine(VerifierOutDir, "prog_01_lv1_onboarding_glove.png");
            CaptureCustom(0, boot =>
            {
                // 点选南侧林缘树木弹出【砍伐扩地】气泡，白手套精准指向林缘砍树目标，与左下主线任务文案完全呼应且零重叠！
                boot.World.HasPendingChop = true;
                boot.World.PendingChopCx = 5;
                boot.World.PendingChopCy = 3;
                boot.World.PendingChopTimer = 5.0f;
                for (int k = 0; k < 4; k++) boot.Hud.Tick(0.12f);
                bool gmInitOk = boot.Gm != null && !ForestGmConsole.IsGmPanelOpen;
                if (!gmInitOk)
                {
                    allPassed = false;
                    report.AppendLine("GM_INIT_FAIL_ON_STAGE0");
                }
            }, p1);
            File.Copy(p1, Path.Combine(OutDir, "prog_01_lv1_onboarding_glove.png"), true);

            // Screenshot 2: GM 开发者调试台展开（展示人物经验/直升跳级 + [+5/+20/+100] 指定材料精准发放网格）
            string p2 = Path.Combine(VerifierOutDir, "prog_02_gm_console_materials_and_xp.png");
            CaptureCustom(0, boot =>
            {
                var gm = boot.Gm;
                // 先验证 GM 跳级、解锁升级全英雄、一键秒完与开关状态复位
                gm.OpenPanel();
                gm.JumpToPlayerLevel(3);
                boot.Sim.GmUnlockAllHeroes();
                boot.Sim.GmAddHeroLevels(null, 2);
                boot.Sim.GmInstantCompleteAll();
                gm.ClosePanel();
                bool closeAndJumpOk = !ForestGmConsole.IsGmPanelOpen && boot.Sim.CampLevel == 3;

                // 复位至 ★Lv.1 重新打开 GM 面板，验证 (+5 / +100 / +20) 与 4 大分类页签发放具体材料
                boot.Sim.GmSetPlayerLevel(1, boot.View.Map);
                gm.OpenPanel();
                bool openOk = ForestGmConsole.IsGmPanelOpen && boot.Hud.IsModalOpen;

                gm.SetAmountMultiplier(5);
                gm.SelectCategoryTab(1);
                int plankBefore = boot.Sim.Get(SurvRes.Plank);
                gm.GrantMaterial(SurvRes.Plank);
                bool grant5Ok = boot.Sim.Get(SurvRes.Plank) == plankBefore + 5;

                gm.SetAmountMultiplier(100);
                gm.SelectCategoryTab(3);
                int bpBefore = boot.Sim.Get(SurvRes.Blueprint);
                gm.GrantMaterial(SurvRes.Blueprint);
                bool grant100Ok = boot.Sim.Get(SurvRes.Blueprint) == bpBefore + 100;

                // 切回默认 +20 档位与原材采集页签，增加 +50 XP、发放废铁 +20 供截图清晰展示
                gm.SetAmountMultiplier(20);
                gm.SelectCategoryTab(0);
                gm.AddPlayerXp(50);
                gm.GrantMaterial(SurvRes.Scrap);
                bool xpOk = boot.Sim.CampXp == 50 && boot.Sim.CampLevel == 1;
                bool scrapOk = boot.Sim.Get(SurvRes.Scrap) >= 20;

                if (!(closeAndJumpOk && openOk && grant5Ok && grant100Ok && xpOk && scrapOk))
                {
                    allPassed = false;
                    report.AppendLine($"GM_PANEL_VERIFY_FAIL: jumpClose={closeAndJumpOk} open={openOk} g5={grant5Ok} g100={grant100Ok} xp={xpOk} scrap={scrapOk}");
                }
                else
                {
                    report.AppendLine($"GM_CONSOLE: jumpClose={closeAndJumpOk} open={openOk} grant5={grant5Ok} grant100={grant100Ok} xp50={xpOk} scrap20={scrapOk} ok=True");
                }
            }, p2);
            File.Copy(p2, Path.Combine(OutDir, "prog_02_gm_console_materials_and_xp.png"), true);

            // Screenshot 3: 营地建筑解锁与升阶总览（按 Lv.1 -> Lv.15 解锁路线图升序排列，零飘字遮挡！）
            string p3 = Path.Combine(VerifierOutDir, "prog_03_build_roadmap_lv1_15.png");
            CaptureCustom(0, boot =>
            {
                boot.Hud.OpenQuickBuildList(showWarehouse: false);
            }, p3);
            File.Copy(p3, Path.Combine(OutDir, "prog_03_build_roadmap_lv1_15.png"), true);

            // =================================================================
            // 4. 新手开局真实零注入验收截图 + 点击原木堆 -> 采集隐藏手套 -> 切换第2木堆/林缘树全链路校验
            // =================================================================
            string pFix = Path.Combine(Verifier2OutDir, "fix_guide_01_step0_logpile_glove.png");
            CaptureCustom(0, boot =>
            {
                const System.Reflection.BindingFlags BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                // 模拟真实开场对话点完关闭后（零伪造队列注入！）
                boot.Sim.TriggerIntroDialog();
                var mAdv = typeof(ForestSurvivalHud).GetMethod("AdvanceComicDialog", BF);
                mAdv?.Invoke(boot.Hud, null);
                mAdv?.Invoke(boot.Hud, null);

                Canvas.ForceUpdateCanvases();
                boot.Hud.Tick(0.08f);
                Canvas.ForceUpdateCanvases();

                bool zeroInjected = boot.World.ChopQueue.Count == 0 && !boot.World.HasPendingChop && !boot.Hud.IsModalOpen;
                var achePos = new Vector2(boot.World.Walkers[0].gx, boot.World.Walkers[0].gy);
                ForestSurvivalWorld.GroundPile nearestPile = null;
                float nearestDist = float.MaxValue;
                int logPileCount = 0;
                for (int i = 0; i < boot.World.GroundPiles.Count; i++)
                {
                    var gp = boot.World.GroundPiles[i];
                    if (gp.kind != ForestSurvivalWorld.FieldKind.LogPile) continue;
                    logPileCount++;
                    float d = Vector2.Distance(achePos, new Vector2(gp.gx, gp.gy));
                    if (d < nearestDist) { nearestDist = d; nearestPile = gp; }
                }

                var fWorldGlove = typeof(ForestSurvivalHud).GetField("_worldGlove", BF);
                var fUiGlove = typeof(ForestSurvivalHud).GetField("_uiGlove", BF);
                var mW2C = typeof(ForestSurvivalHud).GetMethod("WorldToCanvas", BF);
                var worldGlove = (ComicGlovePointer)fWorldGlove.GetValue(boot.Hud);
                var uiGlove = (ComicGlovePointer)fUiGlove.GetValue(boot.Hud);

                Vector2 expectedPileCanvas = nearestPile != null
                    ? (Vector2)mW2C.Invoke(boot.Hud, new object[] { ForestSliceView.Iso(nearestPile.gx, nearestPile.gy) + new Vector3(0f, 0.22f, 0f) })
                    : Vector2.zero;
                float gloveErr = nearestPile != null ? Vector2.Distance(worldGlove.Root.anchoredPosition, expectedPileCanvas) : 999f;
                bool gloveOnLogPile = nearestPile != null && worldGlove.Root.gameObject.activeSelf && !uiGlove.Root.gameObject.activeSelf && gloveErr < 2.5f;

                int pickBubbleCount = 0;
                var allTexts = boot.Hud.GetComponentsInChildren<UnityEngine.UI.Text>(true);
                for (int i = 0; i < allTexts.Length; i++)
                {
                    if (allTexts[i].gameObject.activeInHierarchy && allTexts[i].text == "捡原木")
                        pickBubbleCount++;
                }
                bool bubbleOk = pickBubbleCount >= 2;

                // 校验左上紧凑 3 排胶囊全部严格落在 x <= 400（100% 避让微信右上角游戏圈与胶囊安全区）
                string[] topPillNames = { "XpPill", "PopPill", "CoinPill", "BlueprintPill", "CapPill", "MoralePill" };
                float maxPillRightX = 0f;
                int foundTopPills = 0;
                float rightOrderTopY = 0f;
                var allRts = boot.Hud.GetComponentsInChildren<RectTransform>(true);
                for (int i = 0; i < allRts.Length; i++)
                {
                    var rt = allRts[i];
                    for (int p = 0; p < topPillNames.Length; p++)
                    {
                        if (rt.name == topPillNames[p])
                        {
                            foundTopPills++;
                            float rx = rt.anchoredPosition.x + rt.sizeDelta.x;
                            if (rx > maxPillRightX) maxPillRightX = rx;
                        }
                    }
                    if (rt.name == "RightOrderEntry")
                        rightOrderTopY = rt.anchoredPosition.y;
                }
                bool safeZoneOk = foundTopPills == 6 && maxPillRightX <= 400f && rightOrderTopY <= -110f;

                bool step0ShotOk = zeroInjected && logPileCount >= 2 && gloveOnLogPile && bubbleOk && safeZoneOk;
                if (!step0ShotOk) allPassed = false;
                report.AppendLine($"STEP0_FRESH_SHOT: zeroInjected={zeroInjected} logPiles={logPileCount} nearest=({(nearestPile != null ? nearestPile.gx : -1f):F2},{(nearestPile != null ? nearestPile.gy : -1f):F2}) gloveActive={worldGlove.Root.gameObject.activeSelf} gloveErr={gloveErr:F2} pickBubbles={pickBubbleCount} maxTopPillRightX={maxPillRightX:F1} rightOrderTopY={rightOrderTopY:F1} ok={step0ShotOk}");
            }, pFix);
            File.Copy(pFix, Path.Combine(OutDir, "fix_guide_01_step0_logpile_glove.png"), true);

            // 模拟玩家真实点击第 1 处 LogPile -> 队列=1 & 奔跑采集期间隐藏白手套 -> 采完指向第 2 处 LogPile -> 再采完指向可砍林缘树！
            string tmpSimShot = "/tmp/mori_step0_click_sim.png";
            CaptureCustom(0, boot =>
            {
                const System.Reflection.BindingFlags BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var camField = typeof(ForestSliceBoot).GetField("_cam", BF);
                Camera bCam = (Camera)camField.GetValue(boot);
                var fWorldGlove = typeof(ForestSurvivalHud).GetField("_worldGlove", BF);
                var fUiGlove = typeof(ForestSurvivalHud).GetField("_uiGlove", BF);
                var mW2C = typeof(ForestSurvivalHud).GetMethod("WorldToCanvas", BF);
                var worldGlove = (ComicGlovePointer)fWorldGlove.GetValue(boot.Hud);
                var uiGlove = (ComicGlovePointer)fUiGlove.GetValue(boot.Hud);

                Canvas.ForceUpdateCanvases();
                boot.Hud.Tick(0.08f);

                // 找出第 1 处（最近）与第 2 处 LogPile
                var achePos = new Vector2(boot.World.Walkers[0].gx, boot.World.Walkers[0].gy);
                ForestSurvivalWorld.GroundPile pFirst = null, pSecond = null;
                float bestD = float.MaxValue;
                for (int i = 0; i < boot.World.GroundPiles.Count; i++)
                {
                    var gp = boot.World.GroundPiles[i];
                    if (gp.kind != ForestSurvivalWorld.FieldKind.LogPile) continue;
                    float d = Vector2.Distance(achePos, new Vector2(gp.gx, gp.gy));
                    if (d < bestD) { pSecond = pFirst; bestD = d; pFirst = gp; }
                    else if (pSecond == null) pSecond = gp;
                }

                // 1) 模拟屏幕点击 pFirst
                Vector2 clickScreen1 = RectTransformUtility.WorldToScreenPoint(bCam, ForestSliceView.Iso(pFirst.gx, pFirst.gy) + new Vector3(0f, 0.18f, 0f));
                boot.World.HandleWorldClick(bCam, clickScreen1);
                bool queued1Ok = pFirst.queued && boot.World.ChopQueue.Count == 1;

                // 2) 阿澈奔跑/采集途中，白手套必须自动隐藏不干扰
                boot.Sim.Tick(0.05f);
                boot.World.Tick(0.05f);
                boot.View.Tick(0.05f);
                boot.Hud.Tick(0.05f);
                bool hiddenDuringGather = boot.World.ChopQueue.Count == 1 && !worldGlove.Root.gameObject.activeSelf && !uiGlove.Root.gameObject.activeSelf;

                // 3) 推进帧直到第 1 处木堆采集完毕（原木 10 -> 15，队列清空）
                int safety = 0;
                while (boot.World.ChopQueue.Count > 0 && safety++ < 360)
                {
                    boot.Sim.Tick(0.05f);
                    boot.World.Tick(0.05f);
                    boot.View.Tick(0.05f);
                    boot.Hud.Tick(0.05f);
                }
                boot.Hud.Tick(0.08f);
                bool gathered1Ok = boot.World.ChopQueue.Count == 0 && boot.Sim.Get(SurvRes.Wood) == 15;

                // 4) 采完第 1 处后，白手套精准切向第 2 处 LogPile
                Vector2 p2Canvas = (Vector2)mW2C.Invoke(boot.Hud, new object[] { ForestSliceView.Iso(pSecond.gx, pSecond.gy) + new Vector3(0f, 0.22f, 0f) });
                float err2 = Vector2.Distance(worldGlove.Root.anchoredPosition, p2Canvas);
                bool point2ndPileOk = worldGlove.Root.gameObject.activeSelf && err2 < 2.5f;

                // 5) 继续点击采完第 2 处木堆，若此时木头未满 18（回扣至 16 测试边界回退），白手套应精准指向可砍林缘树！
                Vector2 clickScreen2 = RectTransformUtility.WorldToScreenPoint(bCam, ForestSliceView.Iso(pSecond.gx, pSecond.gy) + new Vector3(0f, 0.18f, 0f));
                boot.World.HandleWorldClick(bCam, clickScreen2);
                safety = 0;
                while (boot.World.ChopQueue.Count > 0 && safety++ < 360)
                {
                    boot.Sim.Tick(0.05f);
                    boot.World.Tick(0.05f);
                    boot.View.Tick(0.05f);
                    boot.Hud.Tick(0.05f);
                }
                bool questDoneAt20 = boot.Sim.Get(SurvRes.Wood) == 20 && boot.Sim.GetCurrentQuest().done;

                // 测试当两堆木头均被采空且原木 16/18 时，回退指向真实可砍林缘树区块
                boot.Sim.Add(SurvRes.Wood, -4);
                var mResolve0 = typeof(ForestSurvivalHud).GetMethod("TryResolveStep0WorldTarget", BF);
                object[] args = { Vector2.zero, 0f };
                bool resolvedBorderTree = (bool)mResolve0.Invoke(boot.Hud, args);
                Vector2 treeGrid = (Vector2)args[0];
                int tcx = Mathf.FloorToInt(treeGrid.x / ForestMap.ChunkSize);
                int tcy = Mathf.FloorToInt(treeGrid.y / ForestMap.ChunkSize);
                bool treeChunkValid = resolvedBorderTree && boot.View.CanChopChunk(tcx, tcy);
                boot.Hud.Tick(0.08f);
                bool borderGloveActive = worldGlove.Root.gameObject.activeSelf || uiGlove.Root.gameObject.activeSelf;

                bool simFlowOk = queued1Ok && hiddenDuringGather && gathered1Ok && point2ndPileOk && questDoneAt20 && treeChunkValid && borderGloveActive;
                if (!simFlowOk) allPassed = false;
                report.AppendLine($"STEP0_CLICK_FLOW: queued1={queued1Ok} hiddenMidGather={hiddenDuringGather} woodAfter1={boot.Sim.Get(SurvRes.Wood) + 4 - 5} point2ndPile={point2ndPileOk}(err={err2:F2}) questDoneAt20={questDoneAt20} fallbackTreeChunk=({tcx},{tcy},valid={treeChunkValid}) ok={simFlowOk}");
            }, tmpSimShot);
            if (File.Exists(tmpSimShot)) File.Delete(tmpSimShot);

            // 保存干净的默认可玩场景 (ForestSlice.unity)
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var c = new GameObject("MainCamera", typeof(Camera));
            c.tag = "MainCamera";
            new GameObject("ForestSliceBoot", typeof(ForestSliceBoot));
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);

            report.AppendLine($"ALL_PASSED={allPassed}");
            Directory.CreateDirectory(Path.Combine(VerifierOutDir, "scratch"));
            File.WriteAllText(Path.Combine(VerifierOutDir, "scratch", "prog_verify_report.txt"), report.ToString());
            File.WriteAllText("/tmp/mori_prog_verify_report.txt", report.ToString());
            Debug.Log("[VerifyProgressionAndGm]\n" + report);
            if (!allPassed)
            {
                Debug.LogError("[VerifyProgressionAndGm] FAILED! See /tmp/mori_prog_verify_report.txt");
                EditorApplication.Exit(1);
            }
        }

        static System.Collections.Generic.HashSet<SurvRes> ComputeProducibleResources(ForestSurvivalSim sim)
        {
            var set = new System.Collections.Generic.HashSet<SurvRes>
            {
                // Lv.1 林地砍伐、残骸拾荒、浆果采摘、初始三色种子与选种农田保底自循环产出
                SurvRes.Wood,
                SurvRes.Scrap,
                SurvRes.Veggie,
                SurvRes.Ration,
                SurvRes.SeedVeggie,
                SurvRes.SeedFlax,
                SurvRes.SeedHerb,
            };

            if (sim.GetNodeLevel("farm_a", 0) >= 1)
            {
                set.Add(SurvRes.Cloth);
                set.Add(SurvRes.Herb);
                set.Add(SurvRes.SeedGolden);
            }
            if (sim.GetNodeLevel("relic_well", 0) >= 1)
                set.Add(SurvRes.Water);
            if (sim.GetNodeLevel("beast_ranch", 0) >= 1)
            {
                set.Add(SurvRes.Meat);
                set.Add(SurvRes.Pelt);
                set.Add(SurvRes.Compost);
            }
            if (sim.GetNodeLevel("river_dock", 0) >= 1)
                set.Add(SurvRes.Fish);
            if (sim.GetNodeLevel("watchtower", 0) >= 1)
            {
                set.Add(SurvRes.Meat);
                set.Add(SurvRes.Pelt);
            }
            if (sim.CampLevel >= 2)
            {
                // Lv.2 车队物资箱 + 5单居民宝箱 + 升星奖励保底产出工程手稿、医疗电子件与英雄勋章
                set.Add(SurvRes.Blueprint);
                set.Add(SurvRes.Parts);
                set.Add(SurvRes.HeroMedal);
            }

            // 迭代闭包：加入所有已激活且建筑等级、玩家星级、前置原料均满足的五大作坊配方产物
            bool added = true;
            while (added)
            {
                added = false;
                for (int i = 0; i < sim.Nodes.Count; i++)
                {
                    CampNode ws = sim.Nodes[i];
                    if (ws.state != NodeState.Active || !ForestSurvivalSim.IsWorkshopKind(ws.kind)) continue;
                    WorkshopRecipe[] recipes = sim.GetWorkshopRecipes(ws);
                    for (int r = 0; r < recipes.Length; r++)
                    {
                        WorkshopRecipe rec = recipes[r];
                        if (!sim.IsRecipeUnlocked(ws, rec)) continue;
                        if (rec.inAmt1 > 0 && !set.Contains(rec.inRes1)) continue;
                        if (rec.inAmt2 > 0 && !set.Contains(rec.inRes2)) continue;
                        if (rec.inAmt3 > 0 && !set.Contains(rec.inRes3)) continue;
                        if (set.Add(rec.outRes)) added = true;
                    }
                }
            }
            return set;
        }

        static void ForceNodeLevelForTest(ForestSurvivalSim sim, string id, int lv)
        {
            CampNode n = sim.Find(id);
            if (n == null) return;
            n.state = NodeState.Active;
            n.level = Mathf.Max(n.level, lv);
        }

        static void FulfillQuestStepForTest(ForestSurvivalSim sim, ForestMap map, int step)
        {
            switch (step)
            {
                case 0: sim.Add(SurvRes.Wood, 18); break;
                case 1: ForceNodeLevelForTest(sim, "sawmill", 1); break;
                case 2: sim.PlanksCraftedTotal = 4; sim.Add(SurvRes.Plank, 4); break;
                case 3: ForceNodeLevelForTest(sim, "farm_a", 1); sim.FarmHarvestedTotal = 1; break;
                case 4: sim.RationsCookedTotal = 2; sim.Add(SurvRes.RoastVeggie, 1); break;
                case 5: sim.OrdersCompletedTotal = 1; sim.GmSetPlayerLevel(2, map); break;
                case 6: sim.GmAddHeroLevels("dachui", 1); break;
                case 7: ForceNodeLevelForTest(sim, "relic_well", 1); break;
                case 8: ForceNodeLevelForTest(sim, "house_1", 2); ForceNodeLevelForTest(sim, "warehouse", 2); break;
                case 9: ForceNodeLevelForTest(sim, "beast_ranch", 1); sim.RanchCollectedTotal = 1; sim.GmSetPlayerLevel(3, map); break;
                case 10: sim.CompostUsedTotal = 1; break;
                case 11: ForceNodeLevelForTest(sim, "smithy", 1); sim.NailsCraftedTotal = 2; break;
                case 12: ForceNodeLevelForTest(sim, "tailor", 1); sim.ClothesCraftedTotal = 1; sim.GmSetPlayerLevel(4, map); break;
                case 13: ForceNodeLevelForTest(sim, "river_dock", 1); sim.FishCaughtTotal = 1; break;
                case 14: ForceNodeLevelForTest(sim, "relic_well", 2); ForceNodeLevelForTest(sim, "heat_tower", 2); sim.GmSetPlayerLevel(5, map); break;
                case 15: ForceNodeLevelForTest(sim, "house_2", 1); ForceNodeLevelForTest(sim, "barricade", 1); sim.GmSetPlayerLevel(6, map); break;
                case 16: ForceNodeLevelForTest(sim, "watchtower", 1); sim.GmSetPlayerLevel(7, map); break;
                case 17: ForceNodeLevelForTest(sim, "herb_hut", 1); sim.MedsCraftedTotal = 1; sim.GmSetPlayerLevel(8, map); break;
                case 18: ForceNodeLevelForTest(sim, "relic_truck", 1); ForceNodeLevelForTest(sim, "relic_road_gate", 1); sim.GmSetPlayerLevel(9, map); break;
                case 19: ForceNodeLevelForTest(sim, "relic_generator", 1); sim.GmSetPlayerLevel(10, map); break;
                case 20: ForceNodeLevelForTest(sim, "radio_post", 1); sim.CaravanTradesTotal = 1; break;
                case 21: ForceNodeLevelForTest(sim, "house_3", 1); ForceNodeLevelForTest(sim, "shelter", 3); sim.GmSetPlayerLevel(15, map); break;
            }
        }

        [MenuItem("末日囤货狂/深山样板/验收独立策略战场与九宫格布阵", priority = 63)]
        public static void VerifyTacticalCombatArena()
        {
            const string CombatVerifierOutDir = "/Users/rosa/.gemini/antigravity/brain/4d3026b4-c306-423b-996b-c63d1b425a19";
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Directory.CreateDirectory(CombatVerifierOutDir);
            Directory.CreateDirectory(Path.Combine(CombatVerifierOutDir, "scratch"));
            Directory.CreateDirectory(OutDir);

            var report = new System.Text.StringBuilder();
            bool allPassed = true;
            const System.Reflection.BindingFlags BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            // 1. arena_01_stage_poster.png: Stage 10 Boss + Flanker + Milestone Banner
            CaptureArena720x1280(1, (boot, cam) =>
            {
                boot.Sim.GmSetPlayerLevel(8, boot.View.Map);
                boot.Sim.GmSetBattleStage(10);
                boot.Sim.AutoDeployRecommendedFormation();
                boot.Hud.OpenStagePosterModal();
                boot.Hud.Tick(0.08f);

                var spec = boot.Sim.GetBattleStageSpec(boot.Sim.CurrentBattleStage);
                bool ok = boot.Hud.IsModalOpen
                    && spec.stage == 10
                    && spec.isBossStage
                    && spec.hasBacklineFlanker
                    && !string.IsNullOrEmpty(spec.milestoneTag)
                    && boot.Sim.GetDeployedHeroCount() >= 4;
                if (!ok) allPassed = false;
                report.AppendLine($"SHOT_01_POSTER: stage={spec.stage} boss={spec.isBossStage} flanker={spec.hasBacklineFlanker} milestone='{spec.milestoneTag}' deployed={boot.Sim.GetDeployedHeroCount()} ok={ok}");
            }, null, "arena_01_stage_poster.png", CombatVerifierOutDir, OutDir);

            // 2. arena_02_nine_grid_formation.png: SpearAssault + 5 heroes + all 4 synergies active + 6 roster cards
            CaptureArena720x1280(2, (boot, cam) =>
            {
                boot.Sim.GmUnlockAllHeroesAndFormations();
                // Ensure the 5-hero tactical lineup (leimeng, dachui, xiaotang, linyan, laochentou) ranks top-5 so all 4 synergies light up
                boot.Sim.FindHero("leimeng").level = 6; boot.Sim.FindHero("leimeng").stars = 3;
                boot.Sim.FindHero("dachui").level = 6; boot.Sim.FindHero("dachui").stars = 3;
                boot.Sim.FindHero("xiaotang").level = 5; boot.Sim.FindHero("xiaotang").stars = 2;
                boot.Sim.FindHero("linyan").level = 5; boot.Sim.FindHero("linyan").stars = 2;
                boot.Sim.FindHero("laochentou").level = 6; boot.Sim.FindHero("laochentou").stars = 3;
                boot.Sim.FindHero("tieshu").level = 2; boot.Sim.FindHero("tieshu").stars = 1;

                boot.Sim.SelectedFormation = TacticalFormationId.SpearAssault;

                // Test manual swap & remove rules first, then auto-deploy recommended formation
                bool swapOk = boot.Sim.TryPlaceHeroInFormation(0, 8, out _);
                bool remOk = boot.Sim.RemoveHeroFromFormation(8, out _);
                boot.Sim.AutoDeployRecommendedFormation();

                boot.Hud.OpenTacticalFormationModal();
                boot.Hud.Tick(0.08f);

                var auras = boot.Sim.GetSynergyAuras();
                int activeSynergies = 0;
                for (int i = 0; i < auras.Count; i++)
                    if (auras[i].active) activeSynergies++;

                int highlightedSlots = 0;
                for (int s = 0; s < 9; s++)
                    if (boot.Sim.GetFormationSlotBuff(TacticalFormationId.SpearAssault, s).highlighted)
                        highlightedSlots++;

                bool ok = swapOk && remOk
                    && boot.Sim.SelectedFormation == TacticalFormationId.SpearAssault
                    && boot.Sim.GetDeployedHeroCount() == 5
                    && activeSynergies == 4
                    && highlightedSlots >= 3;
                if (!ok) allPassed = false;
                report.AppendLine($"SHOT_02_FORMATION: fmt={boot.Sim.SelectedFormation} deployed={boot.Sim.GetDeployedHeroCount()} activeSynergies={activeSynergies}/4 highlightedSlots={highlightedSlots} swapOk={swapOk} remOk={remOk} ok={ok}");
            }, null, "arena_02_nine_grid_formation.png", CombatVerifierOutDir, OutDir);

            // 3. arena_03_unobstructed_clash_2hero.png: Fresh Lv.1 2-hero squad on Stage 1 ~2.2s into battle
            CaptureArena720x1280(0, (boot, cam) =>
            {
                boot.Hud.StartWaveDefenseBattle();
                var fPopups = typeof(ForestTacticalArena).GetField("_numberPopups", BF);
                var popupsList = (System.Collections.ICollection)fPopups.GetValue(boot.World.Arena);
                for (int i = 0; i < 52; i++)
                {
                    boot.Sim.Tick(0.05f);
                    boot.World.Tick(0.05f);
                    boot.View.Tick(0.05f);
                    boot.Hud.Tick(0.05f);
                    if (boot.World.Arena.ElapsedBattleSec >= 2.15f && popupsList != null && popupsList.Count >= 1)
                        break;
                }
                Canvas.ForceUpdateCanvases();

                var arena = boot.World.Arena;
                bool camIsolated = Vector2.Distance(new Vector2(cam.transform.position.x, cam.transform.position.y),
                                                    new Vector2(ForestTacticalArena.ArenaCenter.x, ForestTacticalArena.ArenaCenter.y)) < 0.6f
                                   && Mathf.Abs(cam.orthographicSize - ForestTacticalArena.ArenaOrthoSize) < 0.05f;

                var fCanvasRt = typeof(ForestSurvivalHud).GetField("_canvasRt", BF);
                var fWaveHud = typeof(ForestSurvivalHud).GetField("_waveCombatHud", BF);
                var fMainHud = typeof(ForestSurvivalHud).GetField("_mainHudLayer", BF);
                var canvasRt = (RectTransform)fCanvasRt.GetValue(boot.Hud);
                var waveHud = (GameObject)fWaveHud.GetValue(boot.Hud);
                var mainHud = (GameObject)fMainHud.GetValue(boot.Hud);

                // Check WeChat top-right capsule zone: x > 396px (localX > +36f), top 112px (localY > +528f)
                bool capsuleClear = true;
                string offender = "";
                var graphics = waveHud.GetComponentsInChildren<UnityEngine.UI.Graphic>(false);
                for (int g = 0; g < graphics.Length; g++)
                {
                    var gr = graphics[g];
                    if (gr.gameObject == waveHud) continue;
                    Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRt, gr.rectTransform);
                    if (b.max.y > 528.5f && b.max.x > 36.5f)
                    {
                        capsuleClear = false;
                        offender = $"{gr.name}(maxX={b.max.x:F1},maxY={b.max.y:F1})";
                        break;
                    }
                }

                bool ok = boot.World.InWaveDefense
                    && arena.IsBattleActive
                    && !arena.IsResultPending
                    && arena.HeroStates.Count == 2
                    && camIsolated
                    && !mainHud.activeSelf
                    && waveHud.activeSelf
                    && capsuleClear;
                if (!ok) allPassed = false;
                report.AppendLine($"SHOT_03_CLASH_2HERO: elapsed={arena.ElapsedBattleSec:F2}s heroes={arena.HeroStates.Count} enemiesLeft={arena.EnemiesAliveAndQueued}/{arena.EnemiesTotal} camIsolated={camIsolated} capsuleClear={capsuleClear} offender='{offender}' ok={ok}");
            }, null, "arena_03_unobstructed_clash_2hero.png", CombatVerifierOutDir, OutDir);

            // 4. arena_04_full_squad_boss_battle.png: 5-hero full squad vs Stage 5 Boss mid-action (2x speed, CanSkipBattle lit)
            CaptureArena720x1280(2, (boot, cam) =>
            {
                boot.Sim.GmUnlockAllHeroesAndFormations();
                string[] fiveIds = { "leimeng", "dachui", "xiaotang", "linyan", "laochentou" };
                for (int i = 0; i < fiveIds.Length; i++)
                {
                    var h = boot.Sim.FindHero(fiveIds[i]);
                    h.level = 1;
                    h.stars = 1;
                    h.skinTier = 0;
                }
                boot.Sim.FindHero("tieshu").level = 1;
                boot.Sim.FindHero("tieshu").stars = 1;
                boot.Sim.FindHero("tieshu").skinTier = 0;

                boot.Sim.SelectedFormation = TacticalFormationId.SpearAssault;
                for (int s = 0; s < 9; s++) boot.Sim.FormationSlots[s] = -1;
                boot.Sim.FormationSlots[1 * 3 + 2] = 0; // 大锤 Front-Mid (Spear tip)
                boot.Sim.FormationSlots[0 * 3 + 2] = 2; // 雷猛 Front-Top
                boot.Sim.FormationSlots[1 * 3 + 1] = 1; // 小棠 Mid-Mid
                boot.Sim.FormationSlots[0 * 3 + 0] = 3; // 林砚 Back-Top
                boot.Sim.FormationSlots[2 * 3 + 0] = 5; // 老陈头 Back-Bot

                boot.Sim.GmSetBattleStage(5);
                boot.Hud.StartWaveDefenseBattle();
                if (boot.World.Arena.SpeedMultiplier == 1)
                    boot.World.Arena.ToggleSpeed();

                // GiantBoss on Stage 5 spawns at 3.6s alongside BacklineJumper; step at 2x until ~5.5s so Boss is mid-road & Skip is lit
                for (int step = 0; step < 80; step++)
                {
                    if (boot.World.Arena.ElapsedBattleSec >= 2.2f)
                        boot.World.Arena.AutoCastUlt = false;
                    boot.Sim.Tick(0.04f);
                    boot.World.Tick(0.04f);
                    boot.View.Tick(0.04f);
                    boot.Hud.Tick(0.04f);
                    if (boot.World.Arena.IsResultPending || boot.World.Arena.ElapsedBattleSec >= 5.52f)
                        break;
                }
                Canvas.ForceUpdateCanvases();

                var arena = boot.World.Arena;
                var fCanvasRt = typeof(ForestSurvivalHud).GetField("_canvasRt", BF);
                var fHeroCards = typeof(ForestSurvivalHud).GetField("_wcHeroCardRts", BF);
                var canvasRt = (RectTransform)fCanvasRt.GetValue(boot.Hud);
                var cardRts = (RectTransform[])fHeroCards.GetValue(boot.Hud);
                bool cardsFit720 = true;
                int activeCards = 0;
                for (int i = 0; i < cardRts.Length; i++)
                {
                    if (!cardRts[i].gameObject.activeInHierarchy) continue;
                    activeCards++;
                    Bounds cb = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRt, cardRts[i]);
                    if (cb.min.x < -360.5f || cb.max.x > 360.5f)
                        cardsFit720 = false;
                }

                bool ok = arena.IsBattleActive
                    && !arena.IsResultPending
                    && arena.SpeedMultiplier == 2
                    && arena.CanSkipBattle
                    && arena.ElapsedBattleSec >= 5.2f
                    && activeCards == 5
                    && cardsFit720;
                if (!ok) allPassed = false;
                report.AppendLine($"SHOT_04_BOSS_5HERO: stage={arena.ActiveStage} elapsed={arena.ElapsedBattleSec:F2}s speed={arena.SpeedMultiplier}x canSkip={arena.CanSkipBattle} activeCards={activeCards} cardsFit720={cardsFit720} enemiesLeft={arena.EnemiesAliveAndQueued}/{arena.EnemiesTotal} ok={ok}");
            }, null, "arena_04_full_squad_boss_battle.png", CombatVerifierOutDir, OutDir);

            // 5. arena_05_mvp_victory_autochain.png: Stage 5 Victory + MVP + Milestone Unlock + Auto-Chain countdown + ExitArenaToCamp restore check
            Vector3 savedCampCamPos = Vector3.zero;
            float savedCampOrtho = 0f;
            CaptureArena720x1280(0, (boot, cam) =>
            {
                boot.Sim.GmSetPlayerLevel(4, boot.View.Map);
                boot.Sim.GmSetBattleStage(4); // Stage 4 first-clear unlocks SpearAssault formation milestone!
                boot.Sim.UnlockHero("dachui", silent: true);
                boot.Sim.UnlockHero("leimeng", silent: true);
                boot.Sim.FindHero("leimeng").level = 8; boot.Sim.FindHero("leimeng").stars = 3;
                boot.Sim.FindHero("dachui").level = 7; boot.Sim.FindHero("dachui").stars = 3;
                boot.Sim.FindHero("xiaotang").level = 7; boot.Sim.FindHero("xiaotang").stars = 3;
                boot.Sim.AutoDeployRecommendedFormation();
                boot.Sim.AutoChainNextStage = true;

                savedCampCamPos = cam.transform.position;
                savedCampOrtho = cam.orthographicSize;

                boot.Hud.StartWaveDefenseBattle();
                for (int i = 0; i < 12; i++)
                {
                    boot.Sim.Tick(0.05f);
                    boot.World.Tick(0.05f);
                    boot.View.Tick(0.05f);
                    boot.Hud.Tick(0.05f);
                }
                boot.World.Arena.SkipToBattleResolution();
                boot.Hud.Tick(0.15f);
                Canvas.ForceUpdateCanvases();
            }, (boot, cam) =>
            {
                var arena = boot.World.Arena;
                var res = arena.LastResult;
                var fResModal = typeof(ForestSurvivalHud).GetField("_arenaResultModal", BF);
                var fMainHud = typeof(ForestSurvivalHud).GetField("_mainHudLayer", BF);
                var resModal = (GameObject)fResModal.GetValue(boot.Hud);
                var mainHud = (GameObject)fMainHud.GetValue(boot.Hud);

                bool modalVictoryOk = arena.IsBattleActive
                    && arena.IsResultPending
                    && res.finished
                    && res.victory
                    && !string.IsNullOrEmpty(res.mvpName)
                    && res.mvpDamage > 0
                    && !string.IsNullOrEmpty(res.rewardSummary)
                    && boot.Sim.AutoChainNextStage
                    && resModal.activeSelf;

                // Now invoke ExitArenaAndCloseResult and verify 100% clean camp camera & HUD restoration!
                var mExit = typeof(ForestSurvivalHud).GetMethod("ExitArenaAndCloseResult", BF);
                mExit.Invoke(boot.Hud, null);
                boot.Hud.Tick(0.08f);

                bool camRestored = Vector3.Distance(cam.transform.position, savedCampCamPos) < 0.02f
                                   && Mathf.Abs(cam.orthographicSize - savedCampOrtho) < 0.02f;
                bool hudRestored = !boot.World.InWaveDefense
                                   && !arena.IsBattleActive
                                   && !resModal.activeSelf
                                   && mainHud.activeSelf
                                   && !boot.Hud.IsModalOpen;

                bool ok = modalVictoryOk && camRestored && hudRestored;
                if (!ok) allPassed = false;
                report.AppendLine($"SHOT_05_MVP_VICTORY_AND_EXIT: victory={res.victory} mvp='{res.mvpName}' mvpDmg={res.mvpDamage} reward='{res.rewardSummary.Replace("\n", " | ")}' modalOk={modalVictoryOk} camRestored={camRestored} hudRestored={hudRestored} ok={ok}");
            }, "arena_05_mvp_victory_autochain.png", CombatVerifierOutDir, OutDir);

            // Restore clean default playable scene
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cleanCam = new GameObject("MainCamera", typeof(Camera)) { tag = "MainCamera" };
            new GameObject("ForestSliceBoot", typeof(ForestSliceBoot));
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);

            report.AppendLine($"ALL_PASSED={allPassed}");
            File.WriteAllText(Path.Combine(CombatVerifierOutDir, "scratch", "arena_verify_report.txt"), report.ToString());
            Debug.Log("[VerifyTacticalCombatArena]\n" + report);
            if (!allPassed)
            {
                Debug.LogError("[VerifyTacticalCombatArena] FAILED! Check scratch/arena_verify_report.txt");
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("末日囤货狂/深山样板/验收领地拓荒与桥心战场", priority = 64)]
        public static void VerifyTerritoryAndArenaFixes()
        {
            const string Verifier4OutDir = "/Users/rosa/.gemini/antigravity/brain/bbfc6f74-e665-4b50-9a42-1881cbfd022b";
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Directory.CreateDirectory(Verifier4OutDir);
            Directory.CreateDirectory(Path.Combine(Verifier4OutDir, "scratch"));
            Directory.CreateDirectory(OutDir);

            var report = new System.Text.StringBuilder();
            bool allPassed = true;
            const System.Reflection.BindingFlags BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            // Helper to check zero bubbles over uncut forest & zero HiddenInForest buildings visible
            int CountForestBubblesAndCheckSources(ForestSliceBoot boot, out string detail)
            {
                detail = "";
                int bad = 0;
                for (int i = 0; i < boot.Sim.Nodes.Count; i++)
                {
                    var n = boot.Sim.Nodes[i];
                    if (n.state == NodeState.HiddenInForest && boot.World.Visuals.TryGetValue(n.id, out var nv) && nv.go != null && nv.go.activeSelf)
                    {
                        bad++;
                        detail = $"HiddenNodeVisible:{n.id}";
                        return bad;
                    }
                }
                for (int i = 0; i < boot.World.GroundPiles.Count; i++)
                {
                    var gp = boot.World.GroundPiles[i];
                    if (boot.World.IsGridCoveredByForest(gp.gx, gp.gy))
                    {
                        bad++;
                        detail = $"PileInForest:{gp.kind}@({gp.gx:F1},{gp.gy:F1})";
                        return bad;
                    }
                }

                var fWorldBubbles = typeof(ForestSurvivalHud).GetField("_worldBubbles", BF);
                var wbList = (System.Collections.IList)fWorldBubbles.GetValue(boot.Hud);
                var fCanvasRt = typeof(ForestSurvivalHud).GetField("_canvasRt", BF);
                var canvasRt = (RectTransform)fCanvasRt.GetValue(boot.Hud);
                var fCam = typeof(ForestSliceBoot).GetField("_cam", BF);
                var cam = (Camera)fCam.GetValue(boot);

                // Every active bubble must not say HiddenInForest hint text and its anchor ground must be near cleared territory
                for (int i = 0; i < wbList.Count; i++)
                {
                    object tbObj = wbList[i];
                    var rt = (RectTransform)tbObj.GetType().GetField("rt").GetValue(tbObj);
                    if (rt == null || !rt.gameObject.activeInHierarchy) continue;
                    var lbl = (UnityEngine.UI.Text)tbObj.GetType().GetField("label").GetValue(tbObj);
                    string txt = lbl != null ? (lbl.text ?? "") : "";
                    if (txt == "砍伐扩地") continue; // Explicit border chop confirmation bubble on border trees

                    // Check world pos below bubble (lifting down ~0.55u to ground)
                    Vector3 worldAnchor = rt.position - new Vector3(0f, 0.50f, 0f);
                    Vector2 g = ForestSliceView.WorldToGrid(worldAnchor);
                    bool anyNearCleared = false;
                    int gx0 = Mathf.FloorToInt(g.x);
                    int gy0 = Mathf.FloorToInt(g.y);
                    for (int dx = -1; dx <= 1 && !anyNearCleared; dx++)
                        for (int dy = -1; dy <= 1 && !anyNearCleared; dy++)
                            if (!boot.World.IsGridCoveredByForest(gx0 + dx + 0.5f, gy0 + dy + 0.5f))
                                anyNearCleared = true;

                    if (!anyNearCleared)
                    {
                        bad++;
                        detail = $"BubbleOverUncutForest:'{txt}'@grid({g.x:F1},{g.y:F1})";
                    }
                }
                return bad;
            }

            void ClearHudToasts(ForestSurvivalHud hud)
            {
                string[] listFields = { "_worldToasts", "_pickupToasts", "_flyIcons" };
                for (int f = 0; f < listFields.Length; f++)
                {
                    var fi = typeof(ForestSurvivalHud).GetField(listFields[f], BF);
                    if (fi == null) continue;
                    var list = (System.Collections.IList)fi.GetValue(hud);
                    if (list == null) continue;
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        var rtField = list[i].GetType().GetField("rt");
                        if (rtField != null)
                        {
                            var rt = (RectTransform)rtField.GetValue(list[i]);
                            if (rt != null) UnityEngine.Object.DestroyImmediate(rt.gameObject);
                        }
                    }
                    list.Clear();
                }
            }

            try
            {
                ForestSurvivalHud.EditorTopSafePadOverride = 0f;

                // =============================================================
                // 1. fix4_01_spacious_starter_camp.png
                // =============================================================
                CaptureArena720x1280(0, (boot, cam) =>
                {
                    boot.Sim.TriggerIntroDialog();
                    var mAdv = typeof(ForestSurvivalHud).GetMethod("AdvanceComicDialog", BF);
                    for (int k = 0; k < 4 && boot.Hud.IsModalOpen; k++)
                        mAdv?.Invoke(boot.Hud, null);

                    boot.Sim.Tick(0.08f);
                    boot.World.Tick(0.08f);
                    boot.View.Tick(0.08f);
                    boot.Hud.Tick(0.08f);
                    Canvas.ForceUpdateCanvases();

                    int visibleBuildings = 0;
                    bool onlyStarter3 = true;
                    for (int i = 0; i < boot.Sim.Nodes.Count; i++)
                    {
                        CampNode n = boot.Sim.Nodes[i];
                        bool vis = boot.World.Visuals.TryGetValue(n.id, out var nv) && nv.go != null && nv.go.activeSelf;
                        bool expectedStarter = n.id == "shelter" || n.id == "heat_tower" || n.id == "warehouse";
                        if (vis) visibleBuildings++;
                        if (vis != expectedStarter || (expectedStarter && n.state != NodeState.Active) || (!expectedStarter && n.state != NodeState.HiddenInForest))
                            onlyStarter3 = false;
                    }

                    bool sixCompanionsZeroAche = boot.Sim.Heroes.Count == 6 && boot.Sim.FindHero("ache") == null;
                    for (int i = 0; i < boot.Sim.Heroes.Count; i++)
                        if (boot.Sim.Heroes[i].id == "ache" || boot.Sim.Heroes[i].name.Contains("阿澈"))
                            sixCompanionsZeroAche = false;

                    int badBubbles = CountForestBubblesAndCheckSources(boot, out string bDetail);
                    bool ok = !boot.Hud.IsModalOpen && visibleBuildings == 3 && onlyStarter3 && sixCompanionsZeroAche && badBubbles == 0;
                    if (!ok) allPassed = false;
                    report.AppendLine($"FIX4_01_STARTER: visibleBuildings={visibleBuildings} onlyStarter3={onlyStarter3} heroes={boot.Sim.Heroes.Count} zeroAche={sixCompanionsZeroAche} badBubbles={badBubbles}({bDetail}) ok={ok}");
                }, null, "fix4_01_spacious_starter_camp.png", Verifier4OutDir, OutDir);

                // =============================================================
                // 2. fix4_02_dynamic_chop_guide_to_well.png
                // =============================================================
                CaptureArena720x1280(0, (boot, cam) =>
                {
                    boot.Sim.CampLevel = 2;
                    boot.Sim.QuestIndex = 7; // relic_well quest step while southeast chunk (7, 4) is still UNCUT!

                    CampNode well = boot.Sim.Find("relic_well");
                    bool chunk74Uncut = !boot.View.Map.IsCleared(22, 12) && !well.siteUncovered && well.state == NodeState.HiddenInForest;
                    string qText = boot.Sim.GetCurrentQuest().text;
                    bool questTextOk = qText == "向东南砍伐林木扩展领地，寻找【净水石井】";

                    var mQuestClick = typeof(ForestSurvivalHud).GetMethod("OnQuestCardClicked", BF);
                    mQuestClick.Invoke(boot.Hud, null);

                    for (int f = 0; f < 36; f++)
                    {
                        boot.Sim.Tick(0.05f);
                        boot.World.Tick(0.05f);
                        boot.View.Tick(0.05f);
                        boot.Hud.Tick(0.05f);
                    }
                    Canvas.ForceUpdateCanvases();

                    bool pendingBorderOk = boot.World.HasPendingChop && boot.View.CanChopChunk(boot.World.PendingChopCx, boot.World.PendingChopCy);
                    Vector2 chopChunkGrid = new Vector2((boot.World.PendingChopCx + 0.5f) * ForestMap.ChunkSize, (boot.World.PendingChopCy + 0.5f) * ForestMap.ChunkSize);
                    Vector2 camIso = new Vector2(cam.transform.position.x, cam.transform.position.y);
                    float distToBorderChunk = Vector2.Distance(camIso, ForestSliceView.Iso(chopChunkGrid.x, chopChunkGrid.y));
                    float distToHiddenWell = Vector2.Distance(camIso, ForestSliceView.Iso(well.gx, well.gy));
                    bool camAtBorderNotHidden = distToBorderChunk < 0.65f && distToBorderChunk < distToHiddenWell;

                    var fWorldGlove = typeof(ForestSurvivalHud).GetField("_worldGlove", BF);
                    var mW2C = typeof(ForestSurvivalHud).GetMethod("WorldToCanvas", BF);
                    var worldGlove = (ComicGlovePointer)fWorldGlove.GetValue(boot.Hud);
                    Vector2 rawChopCanvas = (Vector2)mW2C.Invoke(boot.Hud, new object[] { ForestSliceView.Iso(chopChunkGrid.x, chopChunkGrid.y) + new Vector3(0f, 0.55f, 0f) });
                    Vector2 expectedGloveCanvas = new Vector2(
                        Mathf.Clamp(rawChopCanvas.x, -260f, 260f),
                        Mathf.Clamp(rawChopCanvas.y, -420f, 430f) - 8f);
                    float gloveErr = Vector2.Distance(worldGlove.Root.anchoredPosition, expectedGloveCanvas);
                    bool gloveOk = worldGlove.Root.gameObject.activeSelf && gloveErr < 28f;

                    int badBubbles = CountForestBubblesAndCheckSources(boot, out string bDetail);
                    bool ok = chunk74Uncut && questTextOk && pendingBorderOk && camAtBorderNotHidden && gloveOk && badBubbles == 0;
                    if (!ok) allPassed = false;
                    report.AppendLine($"FIX4_02_CHOP_GUIDE: chunk74Uncut={chunk74Uncut} qText='{qText}' pendingChunk=({boot.World.PendingChopCx},{boot.World.PendingChopCy},valid={pendingBorderOk}) camDistBorder={distToBorderChunk:F2} gloveActive={worldGlove.Root.gameObject.activeSelf}(err={gloveErr:F1}) badBubbles={badBubbles}({bDetail}) ok={ok}");
                }, null, "fix4_02_dynamic_chop_guide_to_well.png", Verifier4OutDir, OutDir);

                // =============================================================
                // 3. fix4_03_uncovered_salvage_to_build.png
                // =============================================================
                CampNode wellRef = null;
                ForestSurvivalWorld.GroundPile salvagePileRef = null;
                bool preCollectOk = false;
                CaptureArena720x1280(0, (boot, cam) =>
                {
                    boot.Sim.CampLevel = 2;
                    boot.Sim.QuestIndex = 7;
                    boot.Sim.Add(SurvRes.Ration, 20);
                    wellRef = boot.Sim.Find("relic_well");

                    // Chop border chunks step-by-step toward relic_well until chunk (7, 4) uncovers it
                    int guard = 0;
                    while (!wellRef.siteUncovered && guard++ < 8)
                    {
                        if (!boot.World.TryFindBestChopChunkToward(wellRef.gx, wellRef.gy, out int bcx, out int bcy, out Vector2 cGrid))
                            break;
                        boot.World.ChopQueue.Add(new ForestSurvivalWorld.ChopTask
                        {
                            isPile = false,
                            chunkX = bcx,
                            chunkY = bcy,
                            gx = cGrid.x,
                            gy = cGrid.y,
                        });
                        int stepSafety = 0;
                        while (boot.World.ChopQueue.Count > 0 && stepSafety++ < 420)
                        {
                            boot.Sim.Tick(0.05f);
                            boot.World.Tick(0.05f);
                            boot.View.Tick(0.05f);
                            boot.Hud.Tick(0.05f);
                        }
                    }

                    // Let falling tree animations clear off the new meadow & settle Ache nearby idle
                    for (int k = 0; k < 32; k++)
                    {
                        boot.World.Tick(0.05f);
                        boot.View.Tick(0.05f);
                    }
                    boot.Hud.CloseAllModals();
                    var ache = boot.World.Walkers[0];
                    ache.carryHomeTimer = 0f;
                    ache.puppet.SetCarrySprite(null);
                    ache.gx = wellRef.gx - 1.15f;
                    ache.gy = wellRef.gy + 0.75f;
                    ache.targetGx = ache.gx;
                    ache.targetGy = ache.gy;
                    ache.puppet.SetFacing(true);
                    ache.puppet.SetMode(HeroAnimMode.Idle);
                    ache.puppet.SyncWorldPos(ForestSliceView.Iso(ache.gx, ache.gy), ache.shadowTr);

                    salvagePileRef = boot.World.FindSalvagePileForNode("relic_well");
                    boot.View.FrameCamera(new Vector2(wellRef.gx - 0.35f, wellRef.gy + 0.45f), 4.5f);
                    boot.Hud.CancelPan();
                    ClearHudToasts(boot.Hud);

                    Canvas.ForceUpdateCanvases();
                    boot.Hud.Tick(0.08f);
                    Canvas.ForceUpdateCanvases();

                    bool hiddenBuilding = !boot.World.Visuals["relic_well"].go.activeSelf && wellRef.state == NodeState.HiddenInForest;
                    bool salvageStateOk = wellRef.siteUncovered && !wellRef.siteSalvageCleared && salvagePileRef != null && !salvagePileRef.queued;
                    string qSub2 = boot.Sim.GetCurrentQuest().text;
                    bool questSub2Ok = qSub2.Contains("收集整合新空地发现的") && qSub2.Contains(wellRef.salvageLabel);

                    bool collectBubbleVisible = false;
                    var allTxts = boot.Hud.GetComponentsInChildren<UnityEngine.UI.Text>(false);
                    for (int i = 0; i < allTxts.Length; i++)
                    {
                        if (allTxts[i].text != null && allTxts[i].text.StartsWith("收集:"))
                        {
                            collectBubbleVisible = true;
                            break;
                        }
                    }

                    var fWorldGlove = typeof(ForestSurvivalHud).GetField("_worldGlove", BF);
                    var mW2C = typeof(ForestSurvivalHud).GetMethod("WorldToCanvas", BF);
                    var worldGlove = (ComicGlovePointer)fWorldGlove.GetValue(boot.Hud);
                    Vector2 expectedSalvageCanvas = salvagePileRef != null
                        ? (Vector2)mW2C.Invoke(boot.Hud, new object[] { ForestSliceView.Iso(salvagePileRef.gx, salvagePileRef.gy) + new Vector3(0f, 0.42f, 0f) })
                        : Vector2.zero;
                    float gloveDist = salvagePileRef != null ? Vector2.Distance(worldGlove.Root.anchoredPosition, expectedSalvageCanvas) : 999f;
                    bool gloveAtSalvage = worldGlove.Root.gameObject.activeSelf && gloveDist < 28f;

                    preCollectOk = hiddenBuilding && salvageStateOk && questSub2Ok && collectBubbleVisible && gloveAtSalvage;
                    report.AppendLine($"FIX4_03_SALVAGE_PRE: uncovered={wellRef.siteUncovered} cleared={wellRef.siteSalvageCleared} state={wellRef.state} hiddenVis={hiddenBuilding} bubbleVis={collectBubbleVisible} gloveDist={gloveDist:F1} q='{qSub2}' ok={preCollectOk}");
                }, (boot, cam) =>
                {
                    // Now simulate collecting the salvage pile and assert state -> Discovered!
                    bool queuedOk = salvagePileRef != null && boot.World.QueueGroundPile(salvagePileRef);
                    int safety = 0;
                    while (boot.World.ChopQueue.Count > 0 && safety++ < 360)
                    {
                        boot.Sim.Tick(0.05f);
                        boot.World.Tick(0.05f);
                        boot.View.Tick(0.05f);
                        boot.Hud.Tick(0.05f);
                    }
                    bool postCollectOk = queuedOk
                        && wellRef.siteSalvageCleared
                        && wellRef.state == NodeState.Discovered
                        && boot.World.Visuals["relic_well"].go.activeSelf
                        && boot.World.FindSalvagePileForNode("relic_well") == null;

                    bool ok = preCollectOk && postCollectOk;
                    if (!ok) allPassed = false;
                    report.AppendLine($"FIX4_03_SALVAGE_POST: queuedOk={queuedOk} salvageCleared={wellRef.siteSalvageCleared} state={wellRef.state} foundationVis={boot.World.Visuals["relic_well"].go.activeSelf} ok={ok}");
                }, "fix4_03_uncovered_salvage_to_build.png", Verifier4OutDir, OutDir);

                // =============================================================
                // 4. fix4_04_expanded_spacious_camp.png
                // =============================================================
                CaptureArena720x1280(0, (boot, cam) =>
                {
                    boot.Sim.CampLevel = 3;
                    boot.Sim.Coins = 6000;
                    boot.Sim.Add(SurvRes.Ration, 40);
                    boot.Sim.Add(SurvRes.Wood, 160);
                    boot.Sim.Add(SurvRes.Plank, 80);
                    boot.Sim.Add(SurvRes.Scrap, 80);
                    boot.Sim.Add(SurvRes.Nails, 30);
                    boot.Sim.Add(SurvRes.Water, 30);

                    // Expand 8 chunks around the starter camp uncovering sawmill, farm_a, kitchen, house_1, relic_well
                    int[,] expandChunks =
                    {
                        { 7, 5 }, { 6, 4 }, { 7, 4 }, { 4, 4 },
                        { 3, 6 }, { 3, 7 }, { 5, 4 }, { 6, 5 }
                    };
                    int choppedCount = 0;
                    for (int c = 0; c < expandChunks.GetLength(0); c++)
                    {
                        int cx = expandChunks[c, 0];
                        int cy = expandChunks[c, 1];
                        boot.View.Map.ClearRectInstant(cx * 3, cy * 3, cx * 3 + 2, cy * 3 + 2);
                        choppedCount++;
                    }
                    string[] buildTargets = { "sawmill", "farm_a", "kitchen", "house_1", "relic_well" };
                    for (int i = 0; i < buildTargets.Length; i++)
                    {
                        CampNode tn = boot.Sim.Find(buildTargets[i]);
                        int x0 = Mathf.Min(tn.occupyX - 1, Mathf.FloorToInt(tn.gx) - 1);
                        int y0 = Mathf.Min(tn.occupyY - 1, Mathf.FloorToInt(tn.gy) - 1);
                        int x1 = Mathf.Max(tn.occupyX + tn.occupyW, Mathf.FloorToInt(tn.gx) + 1);
                        int y1 = Mathf.Max(tn.occupyY + tn.occupyH, Mathf.FloorToInt(tn.gy) + 1);
                        boot.View.Map.ClearRectInstant(x0, y0, x1, y1);
                    }
                    boot.View.Map.SweepOrphanForestIslands(instant: true);
                    boot.View.Map.RebuildMask();
                    boot.View.RefreshEdges(instant: true);
                    boot.View.DecorateCleared();
                    boot.Sim.SyncRevealedNodes(boot.View.Map, silent: false);

                    // Clear salvage & construct all 5 expanded buildings cleanly
                    for (int i = 0; i < buildTargets.Length; i++)
                    {
                        CampNode tn = boot.Sim.Find(buildTargets[i]);
                        if (!tn.siteSalvageCleared)
                            boot.Sim.CompleteNodeSiteSalvage(tn.id);
                        boot.Sim.CampLevel = Mathf.Max(boot.Sim.CampLevel, 3);
                        boot.Sim.Coins = 6000;
                        boot.Sim.Add(SurvRes.Wood, 60);
                        boot.Sim.Add(SurvRes.Plank, 40);
                        boot.Sim.Add(SurvRes.Scrap, 40);
                        if (boot.Sim.StartBuildOrUpgrade(tn, out _))
                        {
                            boot.Sim.SpeedUpNodeBuild(tn);
                            boot.Sim.CompleteHammerNode(tn);
                        }
                    }

                    // Also upgrade warehouse to Lv.2 so 120s simulation has ample storage cap
                    CampNode wh = boot.Sim.Find("warehouse");
                    if (wh != null && boot.Sim.StartBuildOrUpgrade(wh, out _))
                    {
                        boot.Sim.SpeedUpNodeBuild(wh);
                        boot.Sim.CompleteHammerNode(wh);
                    }

                    // Step 120 simulated camp seconds (1200 x 0.1s) to test RegrownTimber territory exclusion
                    for (int step = 0; step < 1200; step++)
                    {
                        boot.Sim.Tick(0.1f);
                        boot.World.Tick(0.1f);
                    }
                    boot.View.Tick(0.2f);
                    boot.Hud.CloseAllModals();
                    ClearHudToasts(boot.Hud);
                    boot.Sim.QuestIndex = 8; // Past step 0..2 so early tutorial bubbles don't clutter wide overview
                    boot.View.FrameCamera(new Vector2(16.0f, 17.2f), 7.5f);
                    boot.Hud.CancelPan();
                    Canvas.ForceUpdateCanvases();
                    boot.Hud.Tick(0.08f);
                    Canvas.ForceUpdateCanvases();

                    int regrownInsideCamp = 0;
                    for (int i = 0; i < boot.World.GroundPiles.Count; i++)
                    {
                        var gp = boot.World.GroundPiles[i];
                        if (gp.kind == ForestSurvivalWorld.FieldKind.RegrownTimber &&
                            gp.gx >= 4.5f && gp.gx <= 27.5f && gp.gy >= 4.5f && gp.gy <= 27.5f)
                        {
                            regrownInsideCamp++;
                        }
                    }

                    float minPairwiseDist = float.MaxValue;
                    for (int i = 0; i < boot.Sim.Nodes.Count; i++)
                    {
                        for (int j = i + 1; j < boot.Sim.Nodes.Count; j++)
                        {
                            float d = Vector2.Distance(
                                new Vector2(boot.Sim.Nodes[i].gx, boot.Sim.Nodes[i].gy),
                                new Vector2(boot.Sim.Nodes[j].gx, boot.Sim.Nodes[j].gy));
                            if (d < minPairwiseDist) minPairwiseDist = d;
                        }
                    }

                    bool all5Built = true;
                    for (int i = 0; i < buildTargets.Length; i++)
                        if (boot.Sim.GetNodeLevel(buildTargets[i], 0) < 1) all5Built = false;

                    int orphanTreeCells = 0;
                    for (int x = 1; x < ForestMap.N - 1; x++)
                    {
                        for (int y = 1; y < ForestMap.N - 1; y++)
                        {
                            if (boot.View.Map.IsCleared(x, y)) continue;
                            int cn = (boot.View.Map.IsCleared(x - 1, y) ? 1 : 0)
                                   + (boot.View.Map.IsCleared(x + 1, y) ? 1 : 0)
                                   + (boot.View.Map.IsCleared(x, y - 1) ? 1 : 0)
                                   + (boot.View.Map.IsCleared(x, y + 1) ? 1 : 0);
                            if (cn >= 3) orphanTreeCells++;
                        }
                    }

                    int badBubbles = CountForestBubblesAndCheckSources(boot, out string bDetail);
                    bool ok = choppedCount >= 8 && all5Built && regrownInsideCamp == 0 && orphanTreeCells == 0 && minPairwiseDist >= 3.999f && badBubbles == 0;
                    if (!ok) allPassed = false;
                    report.AppendLine($"FIX4_04_EXPANDED_120S: all5Built={all5Built} regrownInsideCamp={regrownInsideCamp} orphanTreeCells={orphanTreeCells} minBuildingDist={minPairwiseDist:F3} badBubbles={badBubbles}({bDetail}) ok={ok}");
                }, null, "fix4_04_expanded_spacious_camp.png", Verifier4OutDir, OutDir);

                // =============================================================
                // 5. fix4_05_heroes_no_protagonist.png (_topSafePad = 48f)
                // =============================================================
                ForestSurvivalHud.EditorTopSafePadOverride = 48f;
                CaptureArena720x1280(1, (boot, cam) =>
                {
                    boot.Sim.GmUnlockAllHeroes();
                    boot.Hud.CloseAllModals();
                    ClearHudToasts(boot.Hud);
                    boot.Hud.OpenHeroModal(1);
                    boot.Hud.Tick(0.08f);
                    Canvas.ForceUpdateCanvases();

                    var fTopPad = typeof(ForestSurvivalHud).GetField("_topSafePad", BF);
                    var fBanner = typeof(ForestSurvivalHud).GetField("_haLeaderBannerTxt", BF);
                    var fNames = typeof(ForestSurvivalHud).GetField("_haNames", BF);
                    var fBusts = typeof(ForestSurvivalHud).GetField("_haBusts", BF);
                    float padVal = (float)fTopPad.GetValue(boot.Hud);
                    var bannerTxt = (UnityEngine.UI.Text)fBanner.GetValue(boot.Hud);
                    var nameTxts = (UnityEngine.UI.Text[])fNames.GetValue(boot.Hud);
                    var bustImgs = (UnityEngine.UI.Image[])fBusts.GetValue(boot.Hud);

                    bool bannerOk = bannerTxt != null && bannerTxt.gameObject.activeInHierarchy &&
                                    bannerTxt.text.StartsWith("领主【阿澈】等级即左上角玩家等级");

                    string[] expectedNames = { "大锤", "小棠", "雷猛", "林砚", "铁叔", "老陈头" };
                    bool cardsOk = boot.Sim.Heroes.Count == 6 && nameTxts != null && nameTxts.Length == 6;
                    for (int i = 0; i < 6 && cardsOk; i++)
                    {
                        string cardTitle = nameTxts[i].text ?? "";
                        if (!cardTitle.Contains(expectedNames[i]) || cardTitle.Contains("阿澈") || boot.Sim.Heroes[i].id == "ache")
                            cardsOk = false;
                    }

                    bool portraitsUnifiedOk = boot.Sim.FindHero("leimeng")?.portraitKey == "bust_leimeng"
                        && boot.Sim.FindHero("tieshu")?.portraitKey == "bust_tieshu"
                        && bustImgs != null
                        && bustImgs[2].sprite != null && bustImgs[2].sprite.name.Contains("bust_leimeng")
                        && bustImgs[4].sprite != null && bustImgs[4].sprite.name.Contains("bust_tieshu");

                    bool ok = Mathf.Abs(padVal - 48f) < 0.1f && bannerOk && cardsOk && portraitsUnifiedOk;
                    if (!ok) allPassed = false;
                    report.AppendLine($"FIX4_05_HEROES_MODAL: topSafePad={padVal:F1} banner='{bannerTxt?.text}' cardsOk={cardsOk} portraitsUnified={portraitsUnifiedOk}(lm={bustImgs?[2]?.sprite?.name},ts={bustImgs?[4]?.sprite?.name}) ok={ok}");
                }, null, "fix4_05_heroes_no_protagonist.png", Verifier4OutDir, OutDir);

                // =============================================================
                // 6. fix4_06_center_bridge_combat_narrow_safe.png (_topSafePad = 50f + 590x1280 bounds check)
                // =============================================================
                bool narrow590BoundsOk = false;
                {
                    // Explicit 590x1280 narrow-phone camera bounds verification in code
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    var nCam = new GameObject("NarrowCam", typeof(Camera)).GetComponent<Camera>();
                    nCam.tag = "MainCamera";
                    var nRt = new RenderTexture(590, 1280, 24, RenderTextureFormat.ARGB32);
                    nCam.targetTexture = nRt;
                    nCam.aspect = 590f / 1280f;

                    ForestSurvivalHud.EditorTopSafePadOverride = 50f;
                    var nBoot = new GameObject("NarrowBoot").AddComponent<ForestSliceBoot>();
                    nBoot.InitAll(nCam, 1, playIntroInStage0: false);
                    nBoot.Sim.CampLevel = 8;
                    nBoot.Sim.UnlockHero("dachui", silent: true);
                    nBoot.Sim.UnlockHero("leimeng", silent: true);
                    nBoot.Sim.UnlockHero("xiaotang", silent: true);
                    nBoot.Sim.UnlockHero("linyan", silent: true);
                    for (int s = 0; s < 9; s++) nBoot.Sim.FormationSlots[s] = -1;
                    nBoot.Sim.FormationSlots[1 * 3 + 2] = 0; // 大锤
                    nBoot.Sim.FormationSlots[0 * 3 + 2] = 2; // 雷猛
                    nBoot.Sim.FormationSlots[1 * 3 + 1] = 1; // 小棠
                    nBoot.Sim.FormationSlots[0 * 3 + 0] = 3; // 林砚
                    nBoot.Sim.GmSetBattleStage(5);
                    nBoot.Hud.StartWaveDefenseBattle();
                    for (int step = 0; step < 86; step++)
                    {
                        nBoot.World.Tick(0.05f);
                        nBoot.Hud.Tick(0.05f);
                    }
                    float halfW = nCam.orthographicSize * nCam.aspect;
                    var fArtBg = typeof(ForestTacticalArena).GetField("_artBgSr", BF);
                    var artSr = (SpriteRenderer)fArtBg.GetValue(nBoot.World.Arena);
                    bool bgCovers = artSr == null || !artSr.gameObject.activeInHierarchy ||
                                    (artSr.bounds.size.x >= halfW * 2f - 0.02f && artSr.bounds.size.y >= nCam.orthographicSize * 2f - 0.02f);
                    narrow590BoundsOk = halfW >= 3.59f && nCam.orthographicSize >= 7.75f && bgCovers;
                    report.AppendLine($"FIX4_06_NARROW_590x1280: ortho={nCam.orthographicSize:F2} halfWidth={halfW:F2} bgCovers={bgCovers} ok={narrow590BoundsOk}");

                    nCam.targetTexture = null;
                    UnityEngine.Object.DestroyImmediate(nRt);
                }

                ForestSurvivalHud.EditorTopSafePadOverride = 50f;
                CaptureArena720x1280(1, (boot, cam) =>
                {
                    boot.Sim.CampLevel = 8; // unlocks 4 deployment slots
                    boot.Sim.UnlockHero("dachui", silent: true);
                    boot.Sim.UnlockHero("leimeng", silent: true);
                    boot.Sim.UnlockHero("xiaotang", silent: true);
                    boot.Sim.UnlockHero("linyan", silent: true);
                    for (int i = 0; i < 4; i++)
                    {
                        boot.Sim.Heroes[i].level = 2;
                        boot.Sim.Heroes[i].stars = 1;
                    }
                    for (int s = 0; s < 9; s++) boot.Sim.FormationSlots[s] = -1;
                    boot.Sim.TryPlaceHeroInFormation(0, 1 * 3 + 2, out _); // 大锤 Front-Mid
                    boot.Sim.TryPlaceHeroInFormation(2, 0 * 3 + 2, out _); // 雷猛 Front-Top
                    boot.Sim.TryPlaceHeroInFormation(1, 1 * 3 + 1, out _); // 小棠 Mid-Mid
                    boot.Sim.TryPlaceHeroInFormation(3, 0 * 3 + 0, out _); // 林砚 Back-Top

                    boot.Sim.GmSetBattleStage(5); // Boss stage (GiantBoss + BacklineJumper + horde!)
                    boot.Hud.StartWaveDefenseBattle();

                    // Step ~4.15s (83 x 0.05s) so Wave 0, Wave 1, BacklineJumper & GiantBoss (spawns at 3.6s) are all clashing on center bridge!
                    for (int step = 0; step < 83; step++)
                    {
                        boot.Sim.Tick(0.05f);
                        boot.World.Tick(0.05f);
                        boot.View.Tick(0.05f);
                        boot.Hud.Tick(0.05f);
                        if (boot.World.Arena.IsResultPending) break;
                    }
                    Canvas.ForceUpdateCanvases();

                    var arena = boot.World.Arena;
                    var fHeroes = typeof(ForestTacticalArena).GetField("_heroes", BF);
                    var fEnemies = typeof(ForestTacticalArena).GetField("_aliveEnemies", BF);
                    var heroUnits = (System.Collections.IList)fHeroes.GetValue(arena);
                    var enemyUnits = (System.Collections.IList)fEnemies.GetValue(arena);

                    float maxHeroX = -99f;
                    bool zeroAcheInArena = heroUnits.Count == 4;
                    for (int i = 0; i < heroUnits.Count; i++)
                    {
                        object hu = heroUnits[i];
                        string hid = (string)hu.GetType().GetField("heroId").GetValue(hu);
                        Vector2 hpos = (Vector2)hu.GetType().GetField("pos").GetValue(hu);
                        if (hid == "ache") zeroAcheInArena = false;
                        if (hpos.x > maxHeroX) maxHeroX = hpos.x;
                    }

                    float maxEnemyX = -99f;
                    float bossX = -99f;
                    bool hasBossAlive = false;
                    for (int i = 0; i < enemyUnits.Count; i++)
                    {
                        object eu = enemyUnits[i];
                        bool eAlive = (bool)eu.GetType().GetField("alive").GetValue(eu);
                        if (!eAlive) continue;
                        Vector2 epos = (Vector2)eu.GetType().GetField("pos").GetValue(eu);
                        float entry = (float)eu.GetType().GetField("elapsedEntry").GetValue(eu);
                        string kindName = eu.GetType().GetField("kind").GetValue(eu).ToString();
                        if (entry > 0.45f || epos.x <= 2.25f)
                        {
                            if (epos.x > maxEnemyX) maxEnemyX = epos.x;
                        }
                        if (kindName == "GiantBoss")
                        {
                            hasBossAlive = true;
                            bossX = epos.x;
                        }
                    }

                    var fWaveHud = typeof(ForestSurvivalHud).GetField("_waveCombatHud", BF);
                    var waveHud = (GameObject)fWaveHud.GetValue(boot.Hud);
                    Transform vsBarTr = waveHud.transform.Find("SymmetricVsBar");
                    float vsBarAnchoredY = vsBarTr != null ? ((RectTransform)vsBarTr).anchoredPosition.y : 0f;

                    var fCanvasRt = typeof(ForestSurvivalHud).GetField("_canvasRt", BF);
                    var canvasRt = (RectTransform)fCanvasRt.GetValue(boot.Hud);
                    bool capsuleClear = true;
                    string offender = "";
                    var graphics = waveHud.GetComponentsInChildren<UnityEngine.UI.Graphic>(false);
                    for (int g = 0; g < graphics.Length; g++)
                    {
                        var gr = graphics[g];
                        if (gr.gameObject == waveHud) continue;
                        Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRt, gr.rectTransform);
                        // Top-right WeChat capsule zone on 720x1280: x > 396px (localX > +36), top 132px (localY > +508)
                        if (b.max.y > 508.5f && b.max.x > 36.5f)
                        {
                            capsuleClear = false;
                            offender = $"{gr.name}(maxX={b.max.x:F1},maxY={b.max.y:F1})";
                            break;
                        }
                    }

                    bool ok = narrow590BoundsOk
                        && arena.IsBattleActive
                        && !arena.IsResultPending
                        && zeroAcheInArena
                        && maxHeroX <= 1.051f
                        && enemyUnits.Count >= 3
                        && hasBossAlive
                        && maxEnemyX <= 2.201f
                        && bossX <= 1.76f
                        && vsBarAnchoredY <= -142f
                        && capsuleClear;
                    if (!ok) allPassed = false;
                    report.AppendLine($"FIX4_06_BRIDGE_COMBAT: elapsed={arena.ElapsedBattleSec:F2}s heroes={heroUnits.Count} zeroAche={zeroAcheInArena} maxHeroX={maxHeroX:F2} aliveEnemies={enemyUnits.Count} hasBoss={hasBossAlive}(bossX={bossX:F2}) maxEnemyX={maxEnemyX:F2} vsBarY={vsBarAnchoredY:F1} capsuleClear={capsuleClear}({offender}) ok={ok}");
                }, null, "fix4_06_center_bridge_combat_narrow_safe.png", Verifier4OutDir, OutDir);
            }
            finally
            {
                ForestSurvivalHud.EditorTopSafePadOverride = 0f;
            }

            // Restore clean default playable scene
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cleanCamFinal = new GameObject("MainCamera", typeof(Camera)) { tag = "MainCamera" };
            new GameObject("ForestSliceBoot", typeof(ForestSliceBoot));
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);

            report.AppendLine($"ALL_PASSED={allPassed}");
            File.WriteAllText(Path.Combine(Verifier4OutDir, "scratch", "fix4_verify_report.txt"), report.ToString());
            File.WriteAllText("/tmp/mori_fix4_verify_report.txt", report.ToString());
            Debug.Log("[VerifyTerritoryAndArenaFixes]\n" + report);
            if (!allPassed)
            {
                Debug.LogError("[VerifyTerritoryAndArenaFixes] FAILED! Check /tmp/mori_fix4_verify_report.txt");
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("末日囤货狂/深山样板/验收《疯狂水世界》9屏商业UI重制", priority = 66)]
        public static void VerifyWaterWorldUiOverhaul()
        {
            const string WwVerifierDir = "/Users/rosa/.gemini/antigravity/brain/ffe3cc49-dd8a-423d-9186-a75d07d9f9ee";
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Directory.CreateDirectory(WwVerifierDir);
            Directory.CreateDirectory(OutDir);

            // 强制重导所有 ww_* 贴图，确保 ForestArtPostprocessor 写入 FullRect + Pivot(0.5,0.5) + WeixinMiniGame ASTC_4x4
            string[] wwGuids = AssetDatabase.FindAssets("ww_ t:Texture", new[] { "Assets/Mori/ForestSlice/Resources/ForestArt" });
            var report = new System.Text.StringBuilder();
            int wwCount = 0;
            bool wwImportOk = true;
            for (int i = 0; i < wwGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(wwGuids[i]);
                string key = Path.GetFileNameWithoutExtension(path);
                if (!key.StartsWith("ww_")) continue;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                wwCount++;
                var st = new TextureImporterSettings();
                imp.ReadTextureSettings(st);
                var ps = imp.GetPlatformTextureSettings("WeixinMiniGame");
                int expectedMax = key == "ww_hero_cabin_bg" ? 2048 : 512;
                bool ok = imp.textureType == TextureImporterType.Sprite
                    && st.spriteMeshType == SpriteMeshType.FullRect
                    && Vector2.Distance(st.spritePivot, new Vector2(0.5f, 0.5f)) < 0.001f
                    && ps.overridden
                    && ps.format == TextureImporterFormat.ASTC_4x4
                    && ps.maxTextureSize == expectedMax;
                if (!ok)
                {
                    wwImportOk = false;
                    report.AppendLine($"WW_IMPORT_FAIL: {key} fmt={ps.format} max={ps.maxTextureSize} pivot={st.spritePivot}");
                }
            }
            report.AppendLine($"WW_SPRITES_CHECKED={wwCount} IMPORT_OK={wwImportOk}");

            const System.Reflection.BindingFlags BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
            ForestSurvivalHud.EditorTopSafePadOverride = 48f;
            try
            {
                // -----------------------------------------------------------------
                // 1. ww_01_main_hud_and_bubbles.png
                // 扩展营地 + 活跃居民头顶纯白无描边图标气泡(have/need) + 左上双排紧凑领主HUD(x<=390) + 左下悬浮任务卡 + 底部6槽石板导航栏
                // -----------------------------------------------------------------
                CaptureArena720x1280(2, (boot, cam) =>
                {
                    var sim = boot.Sim;
                    sim.EnsureActiveOrders();
                    // 让居民需求既有已满足(深色)也有差1件(红色 0/1)展示对比
                    if (sim.ActiveOrders.Count > 0)
                    {
                        var o0 = sim.ActiveOrders[0];
                        sim.Add(o0.res1, 4, notifyToast: false);
                        if (o0.amt2 > 0) sim.Add(o0.res2, -999, notifyToast: false);
                    }
                    if (sim.ActiveOrders.Count > 1)
                    {
                        var o1 = sim.ActiveOrders[1];
                        sim.Add(o1.res1, 3, notifyToast: false);
                    }
                    for (int i = 0; i < 32; i++)
                    {
                        sim.Tick(0.05f);
                        boot.World.Tick(0.05f);
                        boot.View.Tick(0.05f);
                        boot.Hud.Tick(0.06f);
                    }
                    // 确保开局后台生产飘出的 XP 飞星已完全收束落袋，主画面干净清爽
                    for (int i = 0; i < 16; i++) boot.Hud.Tick(0.08f);
                    boot.View.FrameCamera(new Vector2(15.3f, 15.8f), 4.5f);
                    boot.View.Tick(0.05f);
                    boot.Hud.Tick(0.05f);
                }, (boot, _) =>
                {
                    CheckTopRightCapsuleClear(boot.Hud, "MainCampHud", report);
                }, "ww_01_main_hud_and_bubbles.png", WwVerifierDir, OutDir);

                // -----------------------------------------------------------------
                // 2. ww_02_building_upgrade_modal.png
                // 对标《疯狂水世界》仓库升级弹窗：左上破框立体建筑插画 + +80 ▶▶ +180 绿字提升 + 满足✔/不足✘ + 蓝钮[前往]/[去生产] + 底部等级提示
                // -----------------------------------------------------------------
                CaptureArena720x1280(0, (boot, cam) =>
                {
                    var sim = boot.Sim;
                    sim.CampLevel = 1; // 仓库升2级需领主Lv.2 -> 底部显示红字等级上限提示
                    sim.Coins = 380;   // 铜币 380/120 ✔
                    sim.Add(SurvRes.Plank, -999, notifyToast: false);
                    sim.Add(SurvRes.Plank, 2, notifyToast: false); // 精锯木板 2/8 ✘ [去生产]
                    // 当前人口 4/6 ✘ [前往]
                    CampNode wh = sim.Find("warehouse");
                    boot.View.FrameCamera(new Vector2(17.2f, 17.2f), 4.2f);
                    boot.Hud.OpenBuildConfirmCard(wh);
                    boot.Hud.Tick(0.05f);
                }, null, "ww_02_building_upgrade_modal.png", WwVerifierDir, OutDir);

                // -----------------------------------------------------------------
                // 3. ww_03_workshop_half_drawer.png
                // 对标《疯狂水世界》切鱼厂/服装作坊：上半屏实时营地可见 + 悬浮[升 级] + 队列0生产中 + 5×2切角配方格(已解锁&未解锁锁头) + 青绿四角选框 + 原料 have/need
                // -----------------------------------------------------------------
                CaptureArena720x1280(2, (boot, cam) =>
                {
                    var sim = boot.Sim;
                    CampNode tailor = sim.Find("tailor");
                    if (tailor != null)
                    {
                        // 先清空后台队列飞星，确保标题栏与上半屏营地干净无遮挡
                        for (int i = 0; i < 18; i++)
                        {
                            sim.Tick(0.08f);
                            boot.World.Tick(0.05f);
                            boot.Hud.Tick(0.08f);
                        }
                        for (int i = 0; i < 14; i++) boot.Hud.Tick(0.08f);
                        tailor.level = 2; // Lv.2 解锁前2个配方，第3/4个配方(3级/4级解锁)显示锁头剪影！
                        tailor.queue.Clear();
                        sim.Add(SurvRes.Cloth, 12, notifyToast: false);
                        sim.Add(SurvRes.Pelt, 6, notifyToast: false);
                        sim.TryEnqueueRecipe(tailor, sim.TailorRecipes[0], out _);
                        tailor.queue[0].timer = 1.8f; // 队列槽0稳定展示绿色生产进度条与倒计时
                        // 选中已解锁第2配方(防寒猎皮靴)，同时设铁钉 0/need 展示充足+缺料对比与底部[生 产]按钮！
                        sim.Add(SurvRes.Nails, -999, notifyToast: false);
                        boot.View.FrameCamera(new Vector2(tailor.gx, tailor.gy - 1.4f), 4.2f);
                        boot.Hud.OpenWorkshopDrawer(tailor, selectRecipe: 1);
                        boot.Hud.Tick(0.05f);
                    }
                }, null, "ww_03_workshop_half_drawer.png", WwVerifierDir, OutDir);

                // -----------------------------------------------------------------
                // 4. ww_04_hero_roster_grid.png
                // 全屏 3×2 切角品质英雄总览网格：4名已招募彩色英雄 + 2名未招募暗影剪影与招募条件胶囊 + 职业筛选标签 + 底部红返回键+[闯关招募英雄]+[战术布阵]
                // -----------------------------------------------------------------
                CaptureArena720x1280(2, (boot, cam) =>
                {
                    var sim = boot.Sim;
                    sim.Coins = 1280;
                    sim.Add(SurvRes.Ration, 24, notifyToast: false);
                    sim.Add(SurvRes.HeroMedal, 4, notifyToast: false);
                    for (int i = 0; i < sim.Heroes.Count; i++)
                    {
                        sim.Heroes[i].unlocked = (i < 4); // 前4名彩色解锁，后2名暗影剪影待招募
                        if (i < 4)
                        {
                            sim.Heroes[i].level = 5 + i;
                            sim.Heroes[i].stars = (i == 0) ? 3 : ((i == 1) ? 2 : 1);
                        }
                    }
                    boot.Hud.OpenHeroModal(1);
                    boot.Hud.Tick(0.05f);
                }, (boot, _) =>
                {
                    CheckTopRightCapsuleClear(boot.Hud, "HeroAssignModal", report);
                }, "ww_04_hero_roster_grid.png", WwVerifierDir, OutDir);

                // -----------------------------------------------------------------
                // 5. ww_05_hero_cabin_detail.png
                // 对标《疯狂水世界》赏金猎人舱室详情：阳光木质军械舱全幅背景 + 史诗紫绶带 + 已达上限横幅 + 蓝拳头战力徽章 + 5星条 + 2×3斑马纹六维属性表 + 4枚圆章技能/被动 + 底部石板四页签
                // -----------------------------------------------------------------
                CaptureArena720x1280(2, (boot, cam) =>
                {
                    var sim = boot.Sim;
                    sim.CampLevel = 8;
                    sim.Coins = 1650;
                    sim.Add(SurvRes.Ration, 18, notifyToast: false);
                    sim.Add(SurvRes.HeroMedal, 3, notifyToast: false);
                    HeroSpec xt = sim.Heroes[1]; // 小棠（史诗紫）
                    xt.unlocked = true;
                    xt.stars = 1; // 展示 1枚点亮主动技圆章 + 3枚 "1星/2星/3星解锁" 被动圆章！
                    xt.level = sim.MaxHeroLevelCap; // 8/8 满上限，展示 "已达上限，需要提升玩家等级" 横幅！
                    boot.Hud.OpenHeroDetailModal(1, 0);
                    boot.Hud.Tick(0.05f);
                }, (boot, _) =>
                {
                    CheckTopRightCapsuleClear(boot.Hud, "HeroDetailModal", report);
                }, "ww_05_hero_cabin_detail.png", WwVerifierDir, OutDir);

                // -----------------------------------------------------------------
                // 6. ww_06_hero_delegation_and_picker.png
                // 对标《疯狂水世界》英雄委任半屏部门抽屉：上半屏实时营地可见 + ww_dept_* 漫画工坊/建造/农牧插画 + 已驻守品质卡 + 空闲[+]槽 + 锁头槽 + 玩家X级解锁 + 底部[一键卸任]/[一键委任]
                // -----------------------------------------------------------------
                CaptureArena720x1280(1, (boot, cam) =>
                {
                    var sim = boot.Sim;
                    sim.CampLevel = 4; // 展示已解锁部门卡(已驻守+空槽+锁头槽)与下方 "玩家X级解锁" 部门卡同框！
                    for (int i = 0; i < sim.Heroes.Count; i++)
                        sim.Heroes[i].unlocked = (i < 3);
                    for (int p = 0; p < sim.DelegationPosts.Count; p++)
                    {
                        var st = sim.DelegationPosts[p];
                        for (int s = 0; s < st.assignedHeroIds.Length; s++)
                            sim.AssignHeroToPostSlot(st.kind, s, null);
                    }
                    if (sim.DelegationPosts.Count > 0)
                        sim.AssignHeroToPostSlot(sim.DelegationPosts[0].kind, 0, sim.Heroes[1].id); // 小棠驻守第1部门槽0，槽1留出 [+] 号！
                    boot.View.FrameCamera(new Vector2(15.4f, 16.2f), 4.5f);
                    boot.Hud.OpenHeroModal(0);
                    boot.Hud.Tick(0.05f);
                }, null, "ww_06_hero_delegation_and_picker.png", WwVerifierDir, OutDir);

                // -----------------------------------------------------------------
                // 7. ww_07_resident_needs_drawer.png
                // 对标《疯狂水世界》居民需求半屏抽屉：上半屏营地居民气泡可见 + 笑脸徽章 + 今日满足需求 5/5 满金条 + 抖动红补给礼袋(!) + 方形居民头像页签无痕融入浅白卡 + 台词 + 星标铜币奖励 + 大图标需求框 + 底部红返回键+[获取物品]
                // -----------------------------------------------------------------
                CaptureArena720x1280(2, (boot, cam) =>
                {
                    var sim = boot.Sim;
                    sim.EnsureActiveOrders();
                    sim.SatisfiedToday = 5; // 激活 5/5 满金条 + 红礼袋红点可领态！
                    if (sim.ActiveOrders.Count > 0)
                    {
                        var o0 = sim.ActiveOrders[0];
                        sim.Add(o0.res1, 6, notifyToast: false);
                        if (o0.amt2 > 0)
                        {
                            sim.Add(o0.res2, -999, notifyToast: false); // 第2项设为 0/need 红字，展示 [获取物品] 蓝按钮！
                        }
                        else
                        {
                            o0.res2 = SurvRes.LinenShirt;
                            o0.amt2 = 1;
                            sim.Add(SurvRes.LinenShirt, -999, notifyToast: false);
                        }
                    }
                    if (sim.ActiveOrders.Count > 1)
                    {
                        var o1 = sim.ActiveOrders[1];
                        sim.Add(o1.res1, 6, notifyToast: false);
                        if (o1.amt2 > 0) sim.Add(o1.res2, 6, notifyToast: false); // 第2位居民绿勾可交付！
                    }
                    for (int i = 0; i < 16; i++)
                    {
                        boot.World.Tick(0.05f);
                        boot.Hud.Tick(0.05f);
                    }
                    boot.View.FrameCamera(new Vector2(15.2f, 16.4f), 4.5f);
                    boot.Hud.OpenResidentDemandDrawer();
                    // 切到第0位居民展示已齐+缺货红字对比（第1位居民头像右上同时亮起绿勾！）
                    var fSelTab = typeof(ForestSurvivalHud).GetField("_wwSelectedOrderTab", BF);
                    var mRefresh = typeof(ForestSurvivalHud).GetMethod("RefreshResidentDrawer", BF);
                    fSelTab?.SetValue(boot.Hud, 0);
                    mRefresh?.Invoke(boot.Hud, null);
                    boot.View.Tick(0.05f);
                    boot.Hud.Tick(0.05f);
                }, null, "ww_07_resident_needs_drawer.png", WwVerifierDir, OutDir);

                // -----------------------------------------------------------------
                // 8. ww_08_bag_clipped_inventory.png
                // 对标《疯狂水世界》道具仓库：顶部道具标题(避让胶囊) + WwBrokenFrame双线断墨画框 + 4列多彩切角品质卡 + 青绿四角选框 + 底部固定选中道具详情栏+[前往获取] + 红返回键+4页签石板托盘
                // -----------------------------------------------------------------
                CaptureArena720x1280(2, (boot, cam) =>
                {
                    var sim = boot.Sim;
                    sim.Add(SurvRes.Plank, 28, notifyToast: false);
                    sim.Add(SurvRes.Nails, 16, notifyToast: false);
                    sim.Add(SurvRes.IronBeam, 9, notifyToast: false);
                    sim.Add(SurvRes.IronIngot, 12, notifyToast: false);
                    sim.Add(SurvRes.MetalPipe, 6, notifyToast: false);
                    sim.Add(SurvRes.Cloth, 22, notifyToast: false);
                    sim.Add(SurvRes.LinenShirt, 5, notifyToast: false);
                    sim.Add(SurvRes.LeatherBoots, 4, notifyToast: false);
                    sim.Add(SurvRes.FurCoat, 3, notifyToast: false);
                    sim.Add(SurvRes.PlateArmor, 2, notifyToast: false);
                    sim.Add(SurvRes.SmokedMeat, 14, notifyToast: false);
                    sim.Add(SurvRes.MedKit, 3, notifyToast: false);
                    sim.Add(SurvRes.RoastVeggie, 9, notifyToast: false);
                    sim.Add(SurvRes.VeggieSoup, 7, notifyToast: false);
                    sim.Add(SurvRes.GrillFish, 5, notifyToast: false);
                    sim.Add(SurvRes.WarmTea, 6, notifyToast: false);
                    sim.Add(SurvRes.HerbPoultice, 4, notifyToast: false);
                    sim.Add(SurvRes.Blueprint, 8, notifyToast: false);
                    sim.Add(SurvRes.HeroMedal, 5, notifyToast: false);
                    boot.Hud.OpenBagInventoryModal(0);
                    boot.Hud.Tick(0.05f);
                }, (boot, _) =>
                {
                    CheckTopRightCapsuleClear(boot.Hud, "BagInventoryModal", report);
                }, "ww_08_bag_clipped_inventory.png", WwVerifierDir, OutDir);

                // -----------------------------------------------------------------
                // 9. ww_09_congrats_reward_burst.png
                // 对标《疯狂水世界》恭喜获得全屏爆奖遮罩：ww_banner_congrats 立体粉紫大字(零重影叠字！) + 旋转放射光 + 4张切角品质奖励卡 + 青绿四角选框 + 下方白底道具说明卡 + 点击空白区域关闭
                // -----------------------------------------------------------------
                CaptureArena720x1280(2, (boot, cam) =>
                {
                    var sim = boot.Sim;
                    sim.CampLevel = 7;
                    sim.SatisfiedToday = 5;
                    boot.Hud.OpenResidentDemandDrawer();
                    var mClaim = typeof(ForestSurvivalHud).GetMethod("OnOrderGiftBoxClicked", BF);
                    mClaim?.Invoke(boot.Hud, null);
                    // 选中工程手稿卡展示丰富品质色彩与白底详情说明
                    var mSel = typeof(ForestSurvivalHud).GetMethod("SelectCongratsRewardTile", BF);
                    mSel?.Invoke(boot.Hud, new object[] { 1 });
                    boot.Hud.Tick(0.05f);
                }, null, "ww_09_congrats_reward_burst.png", WwVerifierDir, OutDir);
            }
            finally
            {
                ForestSurvivalHud.EditorTopSafePadOverride = 0f;
            }

            // 恢复干净可运行的默认场景
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cleanCam = new GameObject("MainCamera", typeof(Camera)) { tag = "MainCamera" };
            new GameObject("ForestSliceBoot", typeof(ForestSliceBoot));
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);

            File.WriteAllText("/tmp/mori_ww_ui_report.txt", report.ToString());
            Debug.Log("[VerifyWaterWorldUiOverhaul]\n" + report);
        }

        static void CheckTopRightCapsuleClear(ForestSurvivalHud hud, string rootObjName, System.Text.StringBuilder report)
        {
            const System.Reflection.BindingFlags BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
            var fCanvasRt = typeof(ForestSurvivalHud).GetField("_canvasRt", BF);
            var canvasRt = (RectTransform)fCanvasRt?.GetValue(hud);
            if (canvasRt == null) return;
            Transform targetRoot = canvasRt.Find(rootObjName);
            if (targetRoot == null) targetRoot = canvasRt;

            bool clear = true;
            string offender = "";
            var graphics = targetRoot.GetComponentsInChildren<UnityEngine.UI.Graphic>(false);
            for (int i = 0; i < graphics.Length; i++)
            {
                var gr = graphics[i];
                if (!gr.raycastTarget && gr.color.a < 0.05f) continue;
                if (gr.rectTransform.rect.width >= 700f) continue; // 跳过全屏遮罩或通栏顶底背板本身
                Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRt, gr.rectTransform);
                // 720x1280 微信右上胶囊安全禁区：实际胶囊位于 x = 526..700 (localX > +166)；左置顶栏严格限制 x <= 396 (localX <= +36.5)，居中窄铭牌限制 x <= 456 (localX <= +96.5)
                float maxAllowedLocalX = Mathf.Abs(b.center.x) < 12f ? 96.5f : 36.5f;
                if (b.max.y > 524.5f && b.max.x > maxAllowedLocalX)
                {
                    clear = false;
                    offender = $"{gr.name}(maxX={b.max.x + 360f:F1},topY={640f - b.max.y:F1})";
                    break;
                }
            }
            report.AppendLine($"CAPSULE_CHECK[{rootObjName}]: clear={clear} offender={offender}");
        }

        static void CaptureArena720x1280(
            int stage,
            Action<ForestSliceBoot, Camera> setupBeforeRender,
            Action<ForestSliceBoot, Camera> verifyAfterRender,
            string fileName,
            params string[] targetDirs)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("MainCamera", typeof(Camera)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            var rt = new RenderTexture(720, 1280, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.aspect = 720f / 1280f;

            var boot = new GameObject("ForestSliceBoot").AddComponent<ForestSliceBoot>();
            boot.InitAll(cam, stage, playIntroInStage0: false);

            setupBeforeRender?.Invoke(boot, cam);

            Canvas.ForceUpdateCanvases();
            cam.Render();

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            byte[] png = tex.EncodeToPNG();
            for (int i = 0; i < targetDirs.Length; i++)
            {
                if (string.IsNullOrEmpty(targetDirs[i])) continue;
                Directory.CreateDirectory(targetDirs[i]);
                File.WriteAllBytes(Path.Combine(targetDirs[i], fileName), png);
            }

            verifyAfterRender?.Invoke(boot, cam);

            UnityEngine.Object.DestroyImmediate(tex);
            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(rt);
        }

        static void CaptureCustom(int stage, Action<ForestSliceBoot> setup, string path)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("MainCamera", typeof(Camera)).GetComponent<Camera>();
            var rt = new RenderTexture(1080, 1920, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.aspect = 1080f / 1920f;

            var boot = new GameObject("ForestSliceBoot").AddComponent<ForestSliceBoot>();
            boot.InitAll(cam, stage, playIntroInStage0: false);

            setup?.Invoke(boot);

            Canvas.ForceUpdateCanvases();
            boot.Sim.Tick(0.08f);
            boot.World.Tick(0.08f);
            boot.View.Tick(0.08f);
            boot.Hud.Tick(0.08f);
            Canvas.ForceUpdateCanvases();

            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(rt);
        }

        // =====================================================================
        // Living-World & Full 9-Part Skeletal Hero Unification Verification
        // =====================================================================
        public static void VerifyLivingWorldAndRigs()
        {
            const string OwnBrainDir = "/Users/rosa/.gemini/antigravity/brain/21562628-a212-42f6-94cf-eece5bf07b7a";
            const string CoordBrainDir = "/Users/rosa/.gemini/antigravity/brain/1b3cbc05-17b2-44a9-ba22-41684be97e65";
            Directory.CreateDirectory(OwnBrainDir);
            Directory.CreateDirectory(CoordBrainDir);
            Directory.CreateDirectory(OutDir);

            AssetDatabase.Refresh();
            var report = new System.Text.StringBuilder();
            bool allPassed = true;

            const System.Reflection.BindingFlags BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;

            // -----------------------------------------------------------------
            // A. Programmatic Assertions: 6 Skeletal Hero Atlases + Portraits + 16 Flipbook Strips
            // -----------------------------------------------------------------
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var tmpRoot = new GameObject("VerifyRigRoot").transform;
                string[] heroIds = { "dachui", "xiaotang", "leimeng", "linyan", "tieshu", "laochentou" };
                string[] expectedAtlases =
                {
                    "dachui_puppet_atlas", "xiaotang_puppet_atlas", "leimeng_puppet_atlas",
                    "linyan_puppet_atlas", "tieshu_puppet_atlas", "laochentou_puppet_atlas"
                };
                string[] expectedBusts =
                {
                    "bust_dachui", "bust_xiaotang", "bust_leimeng",
                    "bust_linyan", "bust_tieshu", "bust_laochentou"
                };

                for (int i = 0; i < heroIds.Length; i++)
                {
                    string cid = heroIds[i];
                    PuppetRigProfile prof = PuppetRegistry.GetProfile(cid);
                    OutfitTierDef baseDef = prof.GetTier(OutfitTier.Refugee);
                    Texture2D atlasTex = Resources.Load<Texture2D>("ForestArt/" + expectedAtlases[i]);
                    Texture2D bustTex = Resources.Load<Texture2D>("ForestArt/" + expectedBusts[i]);
                    Sprite bustSp = ForestComicUiSkin.GetArt(expectedBusts[i]);

                    var puppet = new ForestHeroPuppet(tmpRoot, prof, "Verify_" + cid);
                    puppet.SetSkinTier(OutfitTier.Refugee, autoFallbackToBase: true);

                    bool singleBodyEmpty = string.IsNullOrEmpty(prof.singleBodySpriteKey);
                    bool atlasValid = atlasTex != null && atlasTex.width == 1024 && atlasTex.height == 1024 && puppet.ActiveAtlasKey == expectedAtlases[i];
                    bool bustValid = bustTex != null && bustSp != null;
                    bool heroOk = singleBodyEmpty && atlasValid && bustValid && baseDef != null && baseDef.bustKey == expectedBusts[i];
                    if (!heroOk) allPassed = false;
                    report.AppendLine($"HERO_RIG[{cid}]: singleBodyEmpty={singleBodyEmpty} activeAtlas={puppet.ActiveAtlasKey} tex={(atlasTex != null ? $"{atlasTex.width}x{atlasTex.height}" : "null")} bust={expectedBusts[i]}({(bustTex != null ? $"{bustTex.width}x{bustTex.height}" : "null")}) ok={heroOk}");
                }
                UnityEngine.Object.DestroyImmediate(tmpRoot.gameObject);

                (string key, int frames, float ppu, FlipbookPlayMode mode)[] strips =
                {
                    ("anim_walker_0", 4, 256f, FlipbookPlayMode.LoopAll),
                    ("anim_walker_1", 4, 256f, FlipbookPlayMode.LoopAll),
                    ("anim_walker_2", 4, 256f, FlipbookPlayMode.LoopAll),
                    ("anim_work_fishing", 3, 256f, FlipbookPlayMode.PingPongAll),
                    ("anim_work_cooking", 3, 256f, FlipbookPlayMode.PingPongAll),
                    ("anim_work_farming", 3, 256f, FlipbookPlayMode.PingPongAll),
                    ("anim_work_sawing", 3, 256f, FlipbookPlayMode.PingPongAll),
                    ("anim_work_forging", 3, 256f, FlipbookPlayMode.PingPongAll),
                    ("anim_work_herbs", 3, 256f, FlipbookPlayMode.PingPongAll),
                    ("anim_zom_shambler", 4, 256f, FlipbookPlayMode.SplitMoveAttack),
                    ("anim_zom_armored", 4, 256f, FlipbookPlayMode.SplitMoveAttack),
                    ("anim_zom_runner", 4, 256f, FlipbookPlayMode.SplitMoveAttack),
                    ("anim_zom_boss", 4, 384f, FlipbookPlayMode.SplitMoveAttack),
                    ("anim_rabbit", 3, 256f, FlipbookPlayMode.PingPongAll),
                    ("anim_boar", 3, 256f, FlipbookPlayMode.PingPongAll),
                    ("anim_deer", 3, 256f, FlipbookPlayMode.PingPongAll),
                };

                for (int i = 0; i < strips.Length; i++)
                {
                    var s = strips[i];
                    Sprite[] arr = ForestSpriteFlipbook.LoadStripFrames(s.key, s.frames, s.ppu, 0.08f);
                    var fb = new ForestSpriteFlipbook(s.key, s.frames, s.mode, fps: 6f, fallbackSprite: null, ppu: s.ppu, pivotY: 0.08f, phaseOffset: 0f);
                    Sprite f0 = fb.Sample(0f, moving: true, attacking: false);
                    Sprite f1 = fb.Sample(0.25f, moving: true, attacking: false);
                    bool nonNullAll = arr != null && arr.Length == s.frames;
                    if (nonNullAll)
                    {
                        for (int k = 0; k < arr.Length; k++)
                            if (arr[k] == null) nonNullAll = false;
                    }
                    bool frameChanged = f0 != null && f1 != null && f0 != f1;
                    bool stripOk = nonNullAll && fb.IsLoaded && frameChanged;
                    if (!stripOk) allPassed = false;
                    report.AppendLine($"STRIP[{s.key}]: frames={(arr != null ? arr.Length : 0)}/{s.frames} f0={f0?.name} f1={f1?.name} changed={frameChanged} ok={stripOk}");
                }
            }

            ForestSurvivalHud.EditorTopSafePadOverride = 48f;
            try
            {
                // -----------------------------------------------------------------
                // 1. live_01_unified_hero_roster.png
                // -----------------------------------------------------------------
                CaptureArena720x1280(2, (boot, cam) =>
                {
                    boot.Sim.GmUnlockAllHeroesAndFormations();
                    var mClearToast = typeof(ForestSurvivalHud).GetMethod("ClearFloatingWorldToasts", BF);
                    mClearToast?.Invoke(boot.Hud, null);
                    boot.Hud.OpenHeroModal(1);
                    boot.Hud.Tick(0.05f);
                }, (boot, _) =>
                {
                    var fBusts = typeof(ForestSurvivalHud).GetField("_haBusts", BF);
                    var bustImgs = (UnityEngine.UI.Image[])fBusts?.GetValue(boot.Hud);
                    string[] exp = { "bust_dachui", "bust_xiaotang", "bust_leimeng", "bust_linyan", "bust_tieshu", "bust_laochentou" };
                    bool rosterOk = bustImgs != null && bustImgs.Length == 6;
                    for (int i = 0; i < 6 && rosterOk; i++)
                    {
                        if (bustImgs[i].sprite == null || !bustImgs[i].sprite.name.Contains(exp[i]))
                            rosterOk = false;
                    }
                    if (!rosterOk) allPassed = false;
                    report.AppendLine($"LIVE_01_ROSTER: rosterOk={rosterOk} lm={bustImgs?[2]?.sprite?.name} ts={bustImgs?[4]?.sprite?.name}");
                }, "live_01_unified_hero_roster.png", OwnBrainDir, CoordBrainDir, OutDir);

                // -----------------------------------------------------------------
                // 2. live_02_tieshu_cabin_detail.png
                // -----------------------------------------------------------------
                CaptureArena720x1280(2, (boot, cam) =>
                {
                    boot.Sim.GmUnlockAllHeroesAndFormations();
                    var mClearToast = typeof(ForestSurvivalHud).GetMethod("ClearFloatingWorldToasts", BF);
                    mClearToast?.Invoke(boot.Hud, null);
                    boot.Hud.OpenHeroDetailModal(4, 0);
                    boot.Hud.Tick(0.05f);
                }, (boot, _) =>
                {
                    var fCabin = typeof(ForestSurvivalHud).GetField("_wwHdCabinPortrait", BF);
                    var img = (UnityEngine.UI.Image)fCabin?.GetValue(boot.Hud);
                    bool cabinOk = img != null && img.sprite != null && img.sprite.name.Contains("bust_tieshu");
                    if (!cabinOk) allPassed = false;
                    report.AppendLine($"LIVE_02_CABIN: cabinSprite={img?.sprite?.name} ok={cabinOk}");
                }, "live_02_tieshu_cabin_detail.png", OwnBrainDir, CoordBrainDir, OutDir);

                // -----------------------------------------------------------------
                // 3. live_03_camp_artisans_and_ranch.png
                // -----------------------------------------------------------------
                CaptureArena720x1280(2, (boot, cam) =>
                {
                    boot.View.Map.ClearRectInstant(3, 9, 30, 25);
                    boot.View.Map.RebuildMask();
                    boot.View.RefreshEdges(instant: true);

                    // Hide ready-to-collect star badge on Ranch so all 3 ranch pen critters are 100% unobstructed
                    for (int p = 0; p < boot.Sim.RanchPens.Count; p++)
                    {
                        boot.Sim.RanchPens[p].readyToCollect = false;
                        boot.Sim.RanchPens[p].feeding = true;
                        boot.Sim.RanchPens[p].timer = 5.0f;
                    }
                    boot.World.RefreshNodeVisual(boot.Sim.Find("beast_ranch"));
                    boot.World.RefreshNodeVisual(boot.Sim.Find("farm_a"));

                    boot.Sim.GmUnlockAllHeroesAndFormations();
                    // Tick camp so stationed workers, ranch animals, road walkers & skeletal companion heroes animate mid-stride
                    for (int i = 0; i < 24; i++)
                    {
                        boot.Sim.Tick(0.05f);
                        boot.World.Tick(0.05f);
                        boot.View.Tick(0.05f);
                        boot.Hud.Tick(0.05f);
                    }

                    // Ensure skeletal Leimeng & Tieshu are briskly walking in the central camp plaza between Farm, Ranch, Shelter & Sawmill
                    for (int i = 1; i < boot.World.Walkers.Count; i++)
                    {
                        var w = boot.World.Walkers[i];
                        if (w.heroId == "leimeng")
                        {
                            w.gx = 15.0f; w.gy = 15.8f;
                            w.patrolA = new Vector2(14.2f, 15.4f);
                            w.patrolB = new Vector2(16.4f, 16.4f);
                            w.targetGx = 16.4f; w.targetGy = 16.4f;
                            w.pauseTimer = 0f;
                            w.puppet?.SetFacing(true);
                            w.puppet?.SetMode(HeroAnimMode.Run);
                            w.puppet?.SyncWorldPos(ForestSliceView.Iso(w.gx, w.gy), w.shadowTr);
                            w.puppet?.Tick(0.18f);
                        }
                        else if (w.heroId == "tieshu")
                        {
                            w.gx = 19.2f; w.gy = 15.6f;
                            w.patrolA = new Vector2(20.0f, 16.0f);
                            w.patrolB = new Vector2(17.8f, 15.0f);
                            w.targetGx = 17.8f; w.targetGy = 15.0f;
                            w.pauseTimer = 0f;
                            w.puppet?.SetFacing(false);
                            w.puppet?.SetMode(HeroAnimMode.Run);
                            w.puppet?.SyncWorldPos(ForestSliceView.Iso(w.gx, w.gy), w.shadowTr);
                            w.puppet?.Tick(0.22f);
                        }
                    }

                    for (int i = 0; i < boot.World.CivilianVillagers.Count; i++)
                        boot.World.CivilianVillagers[i].pauseTimer = 0f;

                    boot.World.Tick(0.08f);
                    boot.View.Tick(0.08f);

                    var mClearToast = typeof(ForestSurvivalHud).GetMethod("ClearFloatingWorldToasts", BF);
                    mClearToast?.Invoke(boot.Hud, null);
                    var fBanner = typeof(ForestSurvivalHud).GetField("_centerBannerGo", BF);
                    ((GameObject)fBanner?.GetValue(boot.Hud))?.SetActive(false);

                    // Frame Kitchen + Ranch + Farm + Road Walkers + Leimeng/Tieshu + Sawmill + Smithy cleanly
                    boot.View.FrameCamera(new Vector2(16.9f, 15.0f), 9.4f);
                    boot.Hud.Tick(0.02f);
                }, (boot, _) =>
                {
                    bool workersOk = boot.World.StationedWorkers.ContainsKey("sawmill")
                                     && boot.World.StationedWorkers.ContainsKey("kitchen")
                                     && boot.World.StationedWorkers.ContainsKey("farm_a")
                                     && boot.World.StationedWorkers.ContainsKey("smithy")
                                     && boot.World.RanchPenAnimals.Count >= 3
                                     && boot.World.CivilianVillagers.Count >= 3;
                    if (!workersOk) allPassed = false;
                    report.AppendLine($"LIVE_03_CAMP: stationed={boot.World.StationedWorkers.Count} ranchAnimals={boot.World.RanchPenAnimals.Count} roadWalkers={boot.World.CivilianVillagers.Count} companionWalkers={boot.World.Walkers.Count - 1} ok={workersOk}");
                }, "live_03_camp_artisans_and_ranch.png", OwnBrainDir, CoordBrainDir, OutDir);

                // -----------------------------------------------------------------
                // 4. live_04_river_fishing_and_wildlife.png
                // -----------------------------------------------------------------
                CaptureArena720x1280(1, (boot, cam) =>
                {
                    // Clear riverbanks so the blue creek in front of RiverDock is 100% open & unobstructed, flanked by pine edges
                    boot.View.Map.ClearRectInstant(5, 1, 21, 14);
                    boot.View.Map.RebuildMask();
                    boot.View.RefreshEdges(instant: true);

                    CampNode dock = boot.Sim.Find("river_dock");
                    if (dock != null)
                    {
                        dock.state = NodeState.Active;
                        dock.level = 1;
                        dock.siteUncovered = true;
                        dock.siteSalvageCleared = true;
                        boot.World.RefreshNodeVisual(dock);
                    }

                    // Ensure we have both a wild deer and a wild boar patrolling along the creekside meadow
                    for (int k = 0; k < 4; k++)
                        boot.World.SpawnWildPrey();
                    Vector2[] spots = { new Vector2(11.1f, 6.2f), new Vector2(15.6f, 6.1f) };
                    int preyIdx = 0;
                    for (int i = 0; i < boot.World.WildActors.Count; i++)
                    {
                        var wa = boot.World.WildActors[i];
                        if (wa.isZombie || preyIdx >= 2)
                        {
                            if (wa.tr != null) wa.tr.gameObject.SetActive(false);
                            if (wa.shadowTr != null) wa.shadowTr.gameObject.SetActive(false);
                            continue;
                        }
                        // Guarantee 1 deer (anim_deer) + 1 boar (anim_boar)
                        bool wantDeer = preyIdx == 0;
                        wa.flipbook = new ForestSpriteFlipbook(
                            wantDeer ? "anim_deer" : "anim_boar",
                            frameCount: 3,
                            mode: FlipbookPlayMode.PingPongAll,
                            fps: 5.5f,
                            fallbackSprite: null,
                            ppu: 256f,
                            pivotY: 0.08f,
                            phaseOffset: preyIdx * 0.7f);
                        wa.baseScale = wantDeer ? 0.92f : 0.88f;
                        Vector2 sp = spots[preyIdx];
                        wa.anchorGrid = sp;
                        wa.gx = sp.x;
                        wa.gy = sp.y;
                        wa.grazePauseTimer = 0f;
                        wa.patrolOffset = 0f;
                        wa.patrolTargetOffset = preyIdx == 0 ? 0.45f : -0.45f;
                        preyIdx++;
                    }

                    for (int i = 0; i < 18; i++)
                    {
                        boot.Sim.Tick(0.05f);
                        boot.World.Tick(0.05f);
                        boot.View.Tick(0.05f);
                        boot.Hud.Tick(0.05f);
                    }

                    var mClearToast = typeof(ForestSurvivalHud).GetMethod("ClearFloatingWorldToasts", BF);
                    mClearToast?.Invoke(boot.Hud, null);
                    var fBanner = typeof(ForestSurvivalHud).GetField("_centerBannerGo", BF);
                    ((GameObject)fBanner?.GetValue(boot.Hud))?.SetActive(false);

                    boot.View.FrameCamera(new Vector2(13.3f, 5.1f), 4.0f);
                    boot.Hud.Tick(0.02f);
                }, (boot, _) =>
                {
                    bool dockWorkerOk = boot.World.StationedWorkers.TryGetValue("river_dock", out var dw)
                                        && dw.go != null && dw.go.activeInHierarchy;
                    if (!dockWorkerOk) allPassed = false;
                    report.AppendLine($"LIVE_04_RIVER_FISHING: dockWorkerActive={dockWorkerOk}");
                }, "live_04_river_fishing_and_wildlife.png", OwnBrainDir, CoordBrainDir, OutDir);

                // -----------------------------------------------------------------
                // 5 & 6. live_05_skeletal_heroes_and_zombies_f1.png (t = 2.60s)
                //      & live_06_skeletal_heroes_and_zombies_f2.png (t = 2.95s, +0.35s)
                // -----------------------------------------------------------------
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    var cam = new GameObject("MainCamera", typeof(Camera)).GetComponent<Camera>();
                    cam.tag = "MainCamera";
                    var rt = new RenderTexture(720, 1280, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                    cam.targetTexture = rt;
                    cam.aspect = 720f / 1280f;

                    var boot = new GameObject("ForestSliceBoot").AddComponent<ForestSliceBoot>();
                    boot.InitAll(cam, 1, playIntroInStage0: false);
                    boot.Sim.GmUnlockAllHeroesAndFormations();
                    for (int i = 0; i < boot.Sim.Heroes.Count; i++)
                    {
                        boot.Sim.Heroes[i].level = 2;
                        boot.Sim.Heroes[i].stars = 1;
                        boot.Sim.Heroes[i].skinTier = OutfitTier.Refugee;
                        boot.Sim.Heroes[i].portraitKey = PuppetRegistry.ResolveBustKey(boot.Sim.Heroes[i].characterId, OutfitTier.Refugee);
                    }

                    // Deploy 5 heroes cleanly separated across all 3 lanes:
                    // Row 0 (Top): 林砚(Back), 雷猛(Front) -> crimson fire-axe cleaving unobstructed!
                    // Row 1 (Mid): 铁叔(Mid) -> blue-taped shotgun aiming/blasting center-stage!
                    // Row 2 (Bot): 小棠(Back), 大锤(Front) -> heavy riot tanking bottom lane!
                    for (int s = 0; s < 9; s++) boot.Sim.FormationSlots[s] = -1;
                    boot.Sim.TryPlaceHeroInFormation(2, 0 * 3 + 2, out _); // 雷猛 Front-Top
                    boot.Sim.TryPlaceHeroInFormation(4, 1 * 3 + 1, out _); // 铁叔 Mid-Center
                    boot.Sim.TryPlaceHeroInFormation(0, 2 * 3 + 2, out _); // 大锤 Front-Bottom
                    boot.Sim.TryPlaceHeroInFormation(3, 0 * 3 + 0, out _); // 林砚 Back-Top
                    boot.Sim.TryPlaceHeroInFormation(1, 2 * 3 + 0, out _); // 小棠 Back-Bottom

                    boot.Sim.GmSetBattleStage(10); // Boss stage (recPower 2280): Shambler + HardhatArmored + FastRunner + GiantBoss all alive & clashing!
                    boot.Hud.StartWaveDefenseBattle();
                    boot.World.WaveAutoUlt = false; // Keep bridge clear of giant ult fire pools so skeletal limbs & zombie frames are crystal clear!

                    // Step exactly 52 * 0.05s = 2.60s for live_05
                    for (int step = 0; step < 52; step++)
                    {
                        boot.Sim.Tick(0.05f);
                        boot.World.Tick(0.05f);
                        boot.View.Tick(0.05f);
                        boot.Hud.Tick(0.05f);
                    }
                    Canvas.ForceUpdateCanvases();
                    cam.Render();

                    string SnapshotArenaState(out int aliveEnemyCount, out bool bossVisible)
                    {
                        var arena = boot.World.Arena;
                        var fHeroes = typeof(ForestTacticalArena).GetField("_heroes", BF);
                        var fEnemies = typeof(ForestTacticalArena).GetField("_aliveEnemies", BF);
                        var fArmNear = typeof(ForestHeroPuppet).GetField("_armNear", BF);
                        var heroUnits = (System.Collections.IList)fHeroes.GetValue(arena);
                        var enemyUnits = (System.Collections.IList)fEnemies.GetValue(arena);

                        var sb = new System.Text.StringBuilder();
                        for (int i = 0; i < heroUnits.Count; i++)
                        {
                            object hu = heroUnits[i];
                            string hid = (string)hu.GetType().GetField("heroId").GetValue(hu);
                            var pup = (ForestHeroPuppet)hu.GetType().GetField("puppet").GetValue(hu);
                            var armTr = pup != null ? (Transform)fArmNear?.GetValue(pup) : null;
                            float zRot = armTr != null ? armTr.localEulerAngles.z : 0f;
                            sb.Append($"{hid}(atlas={pup?.ActiveAtlasKey},armZ={zRot:F1}) ");
                        }
                        aliveEnemyCount = 0;
                        bossVisible = false;
                        for (int i = 0; i < enemyUnits.Count; i++)
                        {
                            object eu = enemyUnits[i];
                            if (!(bool)eu.GetType().GetField("alive").GetValue(eu)) continue;
                            aliveEnemyCount++;
                            string kindName = eu.GetType().GetField("kind").GetValue(eu).ToString();
                            Vector2 pos = (Vector2)eu.GetType().GetField("pos").GetValue(eu);
                            var sr = (SpriteRenderer)eu.GetType().GetField("sr").GetValue(eu);
                            if (kindName == "GiantBoss" && pos.x <= 2.45f) bossVisible = true;
                            sb.Append($"{kindName}(x={pos.x:F2},sp={sr?.sprite?.name},flipX={sr?.flipX}) ");
                        }
                        return sb.ToString();
                    }

                    string stateF1 = SnapshotArenaState(out int eCnt1, out bool bossVis1);

                    var prev = RenderTexture.active;
                    RenderTexture.active = rt;
                    var tex1 = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                    tex1.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    tex1.Apply();
                    RenderTexture.active = prev;
                    byte[] png1 = tex1.EncodeToPNG();
                    File.WriteAllBytes(Path.Combine(OwnBrainDir, "live_05_skeletal_heroes_and_zombies_f1.png"), png1);
                    File.WriteAllBytes(Path.Combine(CoordBrainDir, "live_05_skeletal_heroes_and_zombies_f1.png"), png1);
                    File.WriteAllBytes(Path.Combine(OutDir, "live_05_skeletal_heroes_and_zombies_f1.png"), png1);
                    UnityEngine.Object.DestroyImmediate(tex1);

                    // Step +0.35s (7 * 0.05s -> t = 2.95s) for live_06
                    for (int step = 0; step < 7; step++)
                    {
                        boot.Sim.Tick(0.05f);
                        boot.World.Tick(0.05f);
                        boot.View.Tick(0.05f);
                        boot.Hud.Tick(0.05f);
                    }
                    Canvas.ForceUpdateCanvases();
                    cam.Render();

                    string stateF2 = SnapshotArenaState(out int eCnt2, out bool bossVis2);

                    RenderTexture.active = rt;
                    var tex2 = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                    tex2.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    tex2.Apply();
                    RenderTexture.active = prev;
                    byte[] png2 = tex2.EncodeToPNG();
                    File.WriteAllBytes(Path.Combine(OwnBrainDir, "live_06_skeletal_heroes_and_zombies_f2.png"), png2);
                    File.WriteAllBytes(Path.Combine(CoordBrainDir, "live_06_skeletal_heroes_and_zombies_f2.png"), png2);
                    File.WriteAllBytes(Path.Combine(OutDir, "live_06_skeletal_heroes_and_zombies_f2.png"), png2);
                    UnityEngine.Object.DestroyImmediate(tex2);

                    cam.targetTexture = null;
                    UnityEngine.Object.DestroyImmediate(rt);

                    bool arenaOk = eCnt1 >= 3 && eCnt2 >= 3 && bossVis1 && bossVis2 && stateF1 != stateF2;
                    if (!arenaOk) allPassed = false;
                    report.AppendLine($"LIVE_05_ARENA_F1(t={boot.World.Arena.ElapsedBattleSec - 0.35f:F2}s): enemies={eCnt1} bossVis={bossVis1} -> {stateF1}");
                    report.AppendLine($"LIVE_06_ARENA_F2(t={boot.World.Arena.ElapsedBattleSec:F2}s): enemies={eCnt2} bossVis={bossVis2} -> {stateF2}");
                    report.AppendLine($"ARENA_MULTI_FRAME_DIFF_OK={arenaOk}");
                }
            }
            finally
            {
                ForestSurvivalHud.EditorTopSafePadOverride = 0f;
            }

            // Restore clean runnable scene
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("MainCamera", typeof(Camera)) { tag = "MainCamera" };
            new GameObject("ForestSliceBoot", typeof(ForestSliceBoot));
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);

            report.AppendLine($"ALL_LIVING_WORLD_VERIFICATIONS_PASSED={allPassed}");
            File.WriteAllText("/tmp/mori_living_verify_report.txt", report.ToString());
            Debug.Log("[VerifyLivingWorldAndRigs]\n" + report);
            if (!allPassed)
                EditorApplication.Exit(1);
        }

        // =====================================================================
        // Hero Economy, 5x5 Star-Petals, 4-Skill Books, Badges, Gear, Chalkboard Formation,
        // Radio Gacha, Patrol Chest & 4-Tab Bag — Full Audit & 8 Benchmark 720x1280 Captures
        // =====================================================================
        public static void VerifyHeroEconomyAndSystems()
        {
            const string OwnBrainDir = "/Users/rosa/.gemini/antigravity/brain/a6d7c4f2-6b0c-48c4-a14d-a196878be7f4";
            const string CoordBrainDir = "/Users/rosa/.gemini/antigravity/brain/1b3cbc05-17b2-44a9-ba22-41684be97e65";
            Directory.CreateDirectory(OwnBrainDir);
            Directory.CreateDirectory(CoordBrainDir);
            Directory.CreateDirectory(OutDir);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var report = new System.Text.StringBuilder();
            bool allPassed = true;

            const System.Reflection.BindingFlags BF =
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;

            // -----------------------------------------------------------------
            // A. Audit all 33 new Hero Economy ico_*.png sprites & WeixinMiniGame ASTC_4x4 import
            // -----------------------------------------------------------------
            string[] econSpriteKeys =
            {
                "ico_shard_dachui", "ico_shard_xiaotang", "ico_shard_leimeng",
                "ico_shard_linyan", "ico_shard_tieshu", "ico_shard_laochentou",
                "ico_hero_exp", "ico_recruit_card", "ico_skill_book_1", "ico_skill_book_2",
                "ico_shard_univ_epic", "ico_shard_univ_leg", "ico_choice_crate",
                "ico_speed_hourglass", "ico_patrol_chest",
                "ico_faction_iron", "ico_faction_forest", "ico_faction_tech",
                "ico_badge_energy", "ico_badge_thorn", "ico_badge_eagle",
                "ico_badge_pyro", "ico_badge_berserk", "ico_badge_medic",
                "ico_gear_weapon", "ico_gear_armor", "ico_gear_boots", "ico_gear_goggles",
                "ico_fmt_default", "ico_fmt_spear", "ico_fmt_resist", "ico_fmt_bastion", "ico_fmt_energy"
            };

            int okSprites = 0;
            for (int i = 0; i < econSpriteKeys.Length; i++)
            {
                string key = econSpriteKeys[i];
                string assetPath = "Assets/Mori/ForestSlice/Resources/ForestArt/" + key + ".png";
                var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (imp != null)
                {
                    var wxPs = imp.GetPlatformTextureSettings("WeixinMiniGame");
                    if (imp.textureType != TextureImporterType.Sprite || !wxPs.overridden || wxPs.format != TextureImporterFormat.ASTC_4x4)
                    {
                        imp.textureType = TextureImporterType.Sprite;
                        imp.spriteImportMode = SpriteImportMode.Single;
                        imp.alphaIsTransparency = true;
                        wxPs.overridden = true;
                        wxPs.maxTextureSize = 512;
                        wxPs.format = TextureImporterFormat.ASTC_4x4;
                        wxPs.compressionQuality = 50;
                        wxPs.crunchedCompression = false;
                        wxPs.textureCompression = TextureImporterCompression.Compressed;
                        imp.SetPlatformTextureSettings(wxPs);
                        imp.SaveAndReimport();
                    }
                }

                Sprite sp = Resources.Load<Sprite>("ForestArt/" + key);
                var impCheck = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                var psCheck = impCheck != null ? impCheck.GetPlatformTextureSettings("WeixinMiniGame") : null;
                bool valid = sp != null && impCheck != null
                             && impCheck.textureType == TextureImporterType.Sprite
                             && psCheck != null && psCheck.overridden
                             && psCheck.format == TextureImporterFormat.ASTC_4x4;
                if (valid) okSprites++;
                else
                {
                    allPassed = false;
                    report.AppendLine($"FAIL_SPRITE[{key}]: sp={(sp != null)} type={impCheck?.textureType} wxFmt={psCheck?.format}");
                }
            }
            report.AppendLine($"ECON_SPRITES_VERIFIED={okSprites}/{econSpriteKeys.Length}");

            // -----------------------------------------------------------------
            // B. Functional Economy Smoke Assertions (Lossless Reset, Star-Petals, Gacha, Patrol, ChoiceBox)
            // -----------------------------------------------------------------
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var tmpCam = new GameObject("TmpCam", typeof(Camera)).GetComponent<Camera>();
                tmpCam.tag = "MainCamera";
                var tmpBoot = new GameObject("TmpBoot").AddComponent<ForestSliceBoot>();
                tmpBoot.InitAll(tmpCam, 1, playIntroInStage0: false);
                var sim = tmpBoot.Sim;
                sim.CampLevel = 5;
                int coinsBefore = sim.Coins;
                int rationBefore = sim.Get(SurvRes.Ration);
                sim.Add(SurvRes.HeroExp, 5000);
                sim.Add(SurvRes.SkillBook, 40);
                var h0 = sim.Heroes[0];
                h0.star = 2;
                h0.skillLevels[1] = 1;
                h0.skillLevels[2] = 1;

                int expStart = sim.Get(SurvRes.HeroExp);
                int booksStart = sim.Get(SurvRes.SkillBook);
                bool lvOk = sim.TryLevelUpHeroWithExp(0, out _) && sim.TryLevelUpHeroWithExp(0, out _);
                bool skOk = sim.TryUpgradeHeroSkill(0, 0, out _);
                bool noCampDrain = sim.Coins == coinsBefore && sim.Get(SurvRes.Ration) == rationBefore;
                bool resetOk = sim.TryResetHeroLevel(0, out _)
                               && h0.level == 1
                               && sim.Get(SurvRes.HeroExp) == expStart
                               && sim.Get(SurvRes.SkillBook) == booksStart;

                sim.Add(SurvRes.RecruitTicket, 10);
                var pulls = sim.PerformRadioRecruit(10, out _);
                bool gachaOk = pulls != null && pulls.Count == 10;

                sim.PatrolAccumSec = 3600f;
                bool patrolOk = sim.TryClaimPatrolChest(out int pExp, out int pCoins, out _, out _) && pExp > 0 && pCoins > 0;

                int woodBefore = sim.Get(SurvRes.Wood);
                bool boxOk = sim.TryOpenChoiceResBox(SurvRes.Wood, 1, out _) && sim.Get(SurvRes.Wood) == woodBefore + 5;

                bool logicPassed = lvOk && skOk && noCampDrain && resetOk && gachaOk && patrolOk && boxOk;
                if (!logicPassed) allPassed = false;
                report.AppendLine($"ECON_LOGIC_SMOKE: lv={lvOk} sk={skOk} dualTrack={noCampDrain} losslessReset={resetOk} gacha10={gachaOk} patrol={patrolOk} choiceBox={boxOk}");
            }

            static void ClearOverlays(ForestSliceBoot boot, System.Reflection.BindingFlags flags)
            {
                boot.Hud.Tick(0.02f);
                var mClearToast = typeof(ForestSurvivalHud).GetMethod("ClearFloatingWorldToasts", flags);
                mClearToast?.Invoke(boot.Hud, null);
                var fBanner = typeof(ForestSurvivalHud).GetField("_centerBannerGo", flags);
                ((GameObject)fBanner?.GetValue(boot.Hud))?.SetActive(false);
            }

            static void SetupRichHeroCabinBase(ForestSliceBoot boot, int targetStar, int targetPetals)
            {
                var sim = boot.Sim;
                sim.CampLevel = 5; // MaxHeroLevelCap = 25
                sim.Add(SurvRes.HeroExp, 18000 - sim.Get(SurvRes.HeroExp));
                sim.Add(SurvRes.SkillBook, 45 - sim.Get(SurvRes.SkillBook));
                sim.Add(SurvRes.ShardUnivLeg, 12);
                sim.Add(SurvRes.ShardUnivEpic, 18);
                sim.AutoExchangeUniversalShards = true;

                sim.AddBadge(TacticalBadgeId.EnergyCore, 2);
                sim.AddBadge(TacticalBadgeId.ThornShield, 2);
                sim.AddBadge(TacticalBadgeId.EagleScope, 1);

                var h = sim.Heroes[0]; // 大锤
                h.star = targetStar;
                h.starPetals = targetPetals;
                h.shards = 8;
                h.skillLevels[0] = 2;
                h.skillLevels[1] = targetStar >= 1 ? 2 : 0;
                h.skillLevels[2] = targetStar >= 2 ? 1 : 0;
                h.skillLevels[3] = targetStar >= 3 ? 1 : 0;
                h.spentSkillBooks = 20;
                h.equipTiers[0] = 2;
                h.equipTiers[1] = 2;
                h.equipTiers[2] = 1;
                h.equipTiers[3] = 1;
                if (targetStar >= 1) h.equippedBadges[0] = TacticalBadgeId.ThornShield;
                if (targetStar >= 3) h.equippedBadges[1] = TacticalBadgeId.EnergyCore;

                while (h.level < 15 && sim.TryLevelUpHeroWithExp(0, out _)) { }
                // Top up HeroExp to a crisp display figure (15,800 -> 1.58万)
                int curExp = sim.Get(SurvRes.HeroExp);
                sim.Add(SurvRes.HeroExp, 15800 - curExp);
            }

            ForestSurvivalHud.EditorTopSafePadOverride = 48f;
            try
            {
                // 1. econ_01_cabin_exp_and_reset.png
                CaptureArena720x1280(1, (boot, _) =>
                {
                    SetupRichHeroCabinBase(boot, targetStar: 1, targetPetals: 3);
                    boot.Hud.OpenHeroDetailModal(0, 0);
                    ClearOverlays(boot, BF);
                }, (boot, _) =>
                {
                    var h = boot.Sim.Heroes[0];
                    report.AppendLine($"SHOT_01: hero={h.name} lv={h.level}/{boot.Sim.MaxHeroLevelCap} star={h.star}+{h.starPetals}/5 exp={boot.Sim.Get(SurvRes.HeroExp)}");
                }, "econ_01_cabin_exp_and_reset.png", OwnBrainDir, CoordBrainDir);

                // 2. econ_02_skill_upgrade_popup.png
                CaptureArena720x1280(1, (boot, _) =>
                {
                    SetupRichHeroCabinBase(boot, targetStar: 2, targetPetals: 2);
                    boot.Hud.OpenHeroDetailModal(0, 0);
                    boot.Hud.OpenHeroSkillDetailPopup(0, 0);
                    ClearOverlays(boot, BF);
                }, (boot, _) =>
                {
                    var sk = boot.Sim.GetHeroSkillDef(0, 0);
                    report.AppendLine($"SHOT_02: skill={sk.name} lv={sk.currentLv}/{sk.maxLv} cost={sk.bookCostNext} books={boot.Sim.Get(SurvRes.SkillBook)}");
                }, "econ_02_skill_upgrade_popup.png", OwnBrainDir, CoordBrainDir);

                // 3. econ_03_star_petals_and_shards.png
                CaptureArena720x1280(1, (boot, _) =>
                {
                    SetupRichHeroCabinBase(boot, targetStar: 1, targetPetals: 3);
                    boot.Hud.OpenHeroDetailModal(0, 1);
                    ClearOverlays(boot, BF);
                }, (boot, _) =>
                {
                    var h = boot.Sim.Heroes[0];
                    report.AppendLine($"SHOT_03: star={h.star} petals={h.starPetals} shards={h.shards} univLeg={boot.Sim.Get(SurvRes.ShardUnivLeg)}");
                }, "econ_03_star_petals_and_shards.png", OwnBrainDir, CoordBrainDir);

                // 4. econ_04_badges_and_workshop_gear.png
                CaptureArena720x1280(2, (boot, _) =>
                {
                    SetupRichHeroCabinBase(boot, targetStar: 3, targetPetals: 1);
                    var sim = boot.Sim;
                    CampNode smithy = sim.Find("smithy");
                    if (smithy != null) { smithy.state = NodeState.Active; smithy.level = 3; }
                    CampNode tailor = sim.Find("tailor");
                    if (tailor != null) { tailor.state = NodeState.Active; tailor.level = 3; }

                    // Stock workshop materials so Tier 2->3 and Tier 1->2 upgrades show green craft/upgrade states alongside material counts!
                    sim.Add(SurvRes.MetalPipe, 6);
                    sim.Add(SurvRes.Blueprint, 8);
                    sim.Add(SurvRes.FurCoat, 2);
                    sim.Add(SurvRes.IronIngot, 6);
                    sim.Add(SurvRes.LeatherBoots, 2);
                    sim.Add(SurvRes.Nails, 8);
                    sim.Add(SurvRes.Parts, 4);

                    var h = sim.Heroes[0];
                    h.equipTiers[0] = 2; // 锻钢强化武器 -> 合金破甲重武 (ready!)
                    h.equipTiers[1] = 3; // 加绒御寒皮甲 (紫品3阶!) -> 废土复合钢甲
                    h.equipTiers[2] = 2; // 防荆棘猎皮靴 -> 铁掌行军战靴 (ready!)
                    h.equipTiers[3] = 1; // 防尘风镜 -> 裂片瞄准护目镜 (ready!)

                    boot.Hud.OpenHeroDetailModal(0, 3);
                    ClearOverlays(boot, BF);
                }, (boot, _) =>
                {
                    var h = boot.Sim.Heroes[0];
                    report.AppendLine($"SHOT_04: gears=[{h.equipTiers[0]},{h.equipTiers[1]},{h.equipTiers[2]},{h.equipTiers[3]}] badges=[{h.equippedBadges[0]},{h.equippedBadges[1]}]");
                }, "econ_04_badges_and_workshop_gear.png", OwnBrainDir, CoordBrainDir);

                // 5. econ_05_chalkboard_formation_aura.png
                CaptureArena720x1280(2, (boot, cam) =>
                {
                    var sim = boot.Sim;
                    sim.CampLevel = 12; // 5-hero squad cap!
                    sim.GmUnlockAllHeroesAndFormations();
                    int[] levels = { 22, 20, 19, 21, 18, 18 };
                    int[] stars = { 3, 2, 2, 3, 2, 2 };
                    for (int i = 0; i < sim.Heroes.Count && i < levels.Length; i++)
                    {
                        var h = sim.Heroes[i];
                        h.level = levels[i];
                        h.star = stars[i];
                        h.starPetals = 2;
                        h.equipTiers[0] = 2;
                        h.equipTiers[1] = 2;
                    }

                    // Deploy 5 heroes across 3x3 chalkboard activating Faction Resonance Aura (2 IronGuard + 2 ForestPact + 1 WasteTech -> +18%!)
                    for (int s = 0; s < 9; s++) sim.FormationSlots[s] = -1;
                    sim.TryPlaceHeroInFormation(0, 1 * 3 + 2, out _); // 大锤 (Iron) Mid-Front
                    sim.TryPlaceHeroInFormation(2, 0 * 3 + 2, out _); // 雷猛 (Iron) Top-Front
                    sim.TryPlaceHeroInFormation(1, 2 * 3 + 1, out _); // 小棠 (Tech) Bot-Mid
                    sim.TryPlaceHeroInFormation(3, 0 * 3 + 0, out _); // 林砚 (Forest) Top-Back
                    sim.TryPlaceHeroInFormation(5, 2 * 3 + 0, out _); // 老陈头 (Forest) Bot-Back
                    sim.TrySelectFormation(TacticalFormationId.ResistBarrier, out _);

                    boot.Hud.OpenTacticalFormationModal();
                    ClearOverlays(boot, BF);
                }, (boot, cam) =>
                {
                    boot.Sim.GetFactionAuraState(out string auraTitle, out _, out float pct, out _, out _, out _);
                    report.AppendLine($"SHOT_05: deployed={boot.Sim.GetDeployedHeroCount()}/{boot.Sim.GetMaxDeployHeroCount()} fmt={boot.Sim.SelectedFormation} aura={auraTitle}({pct:P0})");
                }, "econ_05_chalkboard_formation_aura.png", OwnBrainDir, CoordBrainDir);

                // 6. econ_06_radio_broadcast_gacha.png
                CaptureArena720x1280(1, (boot, cam) =>
                {
                    var sim = boot.Sim;
                    CampNode rp = sim.Find("radio_post");
                    if (rp != null) { rp.state = NodeState.Active; rp.level = 1; }
                    sim.CampLevel = 5;
                    sim.TotalRecruitPulls = 6;
                    sim.RecruitPityCounter = 6; // 6/10 pity progress (4 pulls until guaranteed!)
                    int curTk = sim.Get(SurvRes.RecruitTicket);
                    sim.Add(SurvRes.RecruitTicket, 18 - curTk);

                    // Unlock 3 heroes and show rich shard counts on remaining featured heroes
                    sim.Heroes[0].star = 2;
                    sim.Heroes[1].star = 1;
                    sim.Heroes[2].unlocked = true;
                    sim.Heroes[2].star = 1;
                    sim.Heroes[3].shards = 7;
                    sim.Heroes[5].shards = 6;

                    boot.Hud.OpenRadioRecruitModal();
                    ClearOverlays(boot, BF);
                }, (boot, cam) =>
                {
                    report.AppendLine($"SHOT_06: tickets={boot.Sim.Get(SurvRes.RecruitTicket)} totalPulls={boot.Sim.TotalRecruitPulls} pityRem={boot.Sim.GetRecruitPullsUntilGuaranteed()}");
                }, "econ_06_radio_broadcast_gacha.png", OwnBrainDir, CoordBrainDir);

                // 7. econ_07_stage_poster_patrol_chest.png
                CaptureArena720x1280(2, (boot, cam) =>
                {
                    var sim = boot.Sim;
                    sim.CampLevel = 9; // 4-5 squad slots
                    sim.GmUnlockAllHeroesAndFormations();
                    for (int i = 0; i < sim.Heroes.Count; i++)
                    {
                        sim.Heroes[i].level = 14 + (5 - i);
                        sim.Heroes[i].star = 2;
                    }
                    sim.AutoDeployRecommendedFormation();

                    // Stage 10 Boss stage with ResistBarrier blueprint pending first clear + 60 mins accumulated Patrol Chest!
                    sim.GmSetBattleStage(10);
                    sim.HordeWaveCleared = 9;
                    sim.PatrolAccumSec = 3600f; // 60 minutes accumulated!

                    boot.Hud.OpenStagePosterModal();
                    ClearOverlays(boot, BF);
                }, (boot, cam) =>
                {
                    boot.Sim.GetPatrolPendingRewards(out int pExp, out int pCoins, out int pBooks, out int pMins);
                    report.AppendLine($"SHOT_07: stage={boot.Sim.CurrentBattleStage} patrolMins={pMins} exp={pExp} coins={pCoins} books={pBooks}");
                }, "econ_07_stage_poster_patrol_chest.png", OwnBrainDir, CoordBrainDir);

                // 8. econ_08_bag_four_tabs_shards_choicebox.png
                CaptureArena720x1280(2, (boot, _) =>
                {
                    var sim = boot.Sim;
                    sim.CampLevel = 6;
                    sim.Add(SurvRes.HeroExp, 12500 - sim.Get(SurvRes.HeroExp));
                    sim.Add(SurvRes.RecruitTicket, 12 - sim.Get(SurvRes.RecruitTicket));
                    sim.Add(SurvRes.ShardUnivLeg, 15);
                    sim.Add(SurvRes.ShardUnivEpic, 24);
                    sim.Add(SurvRes.ChoiceResBox, 3);

                    // Give all 6 heroes shards; make 雷猛 (idx 2) locked with 12/10 shards to highlight [可合成] + [合成英雄]!
                    sim.Heroes[0].star = 2; sim.Heroes[0].shards = 9;
                    sim.Heroes[1].star = 1; sim.Heroes[1].shards = 6;
                    sim.Heroes[2].unlocked = false; sim.Heroes[2].shards = 12; // 可合成!
                    sim.Heroes[3].unlocked = true; sim.Heroes[3].star = 1; sim.Heroes[3].shards = 8;
                    sim.Heroes[4].unlocked = true; sim.Heroes[4].star = 1; sim.Heroes[4].shards = 14;
                    sim.Heroes[5].unlocked = false; sim.Heroes[5].shards = 7;

                    // Unlock formation blueprints via HordeWaveCleared = 16 so [碎片] tab displays both active & uncollected formation chips!
                    sim.HordeWaveCleared = 16;

                    boot.Hud.OpenBagInventoryModal(2); // Tab 2 = [碎片], select 雷猛碎片 to show [合成英雄] in inspector!
                    var fSelKey = typeof(ForestSurvivalHud).GetField("_wwBagSelectedKey", BF);
                    var mRefreshBag = typeof(ForestSurvivalHud).GetMethod("RefreshBagInventoryModal", BF);
                    fSelKey?.SetValue(boot.Hud, "shard_hero_leimeng");
                    mRefreshBag?.Invoke(boot.Hud, null);
                    ClearOverlays(boot, BF);
                }, (boot, _) =>
                {
                    report.AppendLine($"SHOT_08: bagTab=2 univLeg={boot.Sim.Get(SurvRes.ShardUnivLeg)} univEpic={boot.Sim.Get(SurvRes.ShardUnivEpic)} leimengShards={boot.Sim.Heroes[2].shards}");
                }, "econ_08_bag_four_tabs_shards_choicebox.png", OwnBrainDir, CoordBrainDir);
            }
            finally
            {
                ForestSurvivalHud.EditorTopSafePadOverride = 0f;
            }

            // Restore clean runnable scene
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("MainCamera", typeof(Camera)) { tag = "MainCamera" };
            new GameObject("ForestSliceBoot", typeof(ForestSliceBoot));
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);

            report.AppendLine($"ALL_HERO_ECONOMY_VERIFICATIONS_PASSED={allPassed}");
            File.WriteAllText("/tmp/mori_econ_verify_report.txt", report.ToString());
            Debug.Log("[VerifyHeroEconomyAndSystems]\n" + report);
            if (!allPassed)
                EditorApplication.Exit(1);
        }

        // =====================================================================
        // Phase-2 Full-Body Standing Heroes, 3x3 9-Hero Matrix (`阿飒`, `青禾`, `周博士`),
        // 3-Faction Wangpai Aura & 9-Part Skeletal Combat — 8 Benchmark 720x1280 Captures
        // =====================================================================
        public static void VerifyPhase2FullBodyAndNineHeroes()
        {
            const string OwnBrainDir = "/Users/rosa/.gemini/antigravity/brain/11dc9d30-a35b-4424-ae19-34ec4abdc193";
            const string CoordBrainDir = "/Users/rosa/.gemini/antigravity/brain/1b3cbc05-17b2-44a9-ba22-41684be97e65";
            Directory.CreateDirectory(OwnBrainDir);
            Directory.CreateDirectory(CoordBrainDir);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var report = new System.Text.StringBuilder();
            bool allPassed = true;

            const System.Reflection.BindingFlags BF =
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;

            // -----------------------------------------------------------------
            // A. Audit all 18 Phase-2 Assets (9 fullbody_*, 3 bust_*, 3 ico_shard_*, 3 *_puppet_atlas)
            // -----------------------------------------------------------------
            string[] heroIds =
            {
                "dachui", "xiaotang", "leimeng",
                "linyan", "tieshu", "laochentou",
                "qinghe", "asa", "zhoudr"
            };
            string[] newHeroIds = { "asa", "qinghe", "zhoudr" };

            int verifiedFullBody = 0;
            for (int i = 0; i < heroIds.Length; i++)
            {
                string[] mipKeys = { "fullbody_" + heroIds[i], "bust_" + heroIds[i] };
                for (int m = 0; m < mipKeys.Length; m++)
                {
                    string mPath = "Assets/Mori/ForestSlice/Resources/ForestArt/" + mipKeys[m] + ".png";
                    var mImp = AssetImporter.GetAtPath(mPath) as TextureImporter;
                    if (mImp != null && (!mImp.mipmapEnabled || mImp.filterMode != FilterMode.Trilinear))
                    {
                        mImp.mipmapEnabled = true;
                        mImp.filterMode = FilterMode.Trilinear;
                        mImp.mipMapBias = -0.35f;
                        mImp.SaveAndReimport();
                    }
                }

                string key = "fullbody_" + heroIds[i];
                string assetPath = "Assets/Mori/ForestSlice/Resources/ForestArt/" + key + ".png";
                var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                var ps = imp != null ? imp.GetPlatformTextureSettings("WeixinMiniGame") : null;
                Sprite sp = Resources.Load<Sprite>("ForestArt/" + key);
                bool ok = sp != null && imp != null
                          && imp.textureType == TextureImporterType.Sprite
                          && ps != null && ps.overridden
                          && ps.format == TextureImporterFormat.ASTC_4x4
                          && ps.maxTextureSize == 1024;
                if (ok) verifiedFullBody++;
                else
                {
                    allPassed = false;
                    report.AppendLine($"FAIL_FULLBODY[{key}]: sp={(sp != null)} type={imp?.textureType} wxFmt={ps?.format} wxMax={ps?.maxTextureSize}");
                }
            }

            int verifiedNewSprites = 0;
            for (int i = 0; i < newHeroIds.Length; i++)
            {
                string[] keys = { "bust_" + newHeroIds[i], "ico_shard_" + newHeroIds[i] };
                for (int k = 0; k < keys.Length; k++)
                {
                    string key = keys[k];
                    string assetPath = "Assets/Mori/ForestSlice/Resources/ForestArt/" + key + ".png";
                    var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    var ps = imp != null ? imp.GetPlatformTextureSettings("WeixinMiniGame") : null;
                    Sprite sp = Resources.Load<Sprite>("ForestArt/" + key);
                    bool ok = sp != null && imp != null
                              && imp.textureType == TextureImporterType.Sprite
                              && ps != null && ps.overridden
                              && ps.format == TextureImporterFormat.ASTC_4x4;
                    if (ok) verifiedNewSprites++;
                    else
                    {
                        allPassed = false;
                        report.AppendLine($"FAIL_NEW_SPRITE[{key}]: sp={(sp != null)} type={imp?.textureType} wxFmt={ps?.format}");
                    }
                }
            }

            int verifiedNewAtlases = 0;
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var tmpRoot = new GameObject("VerifyPhase2Rigs").transform;
                for (int i = 0; i < newHeroIds.Length; i++)
                {
                    string cid = newHeroIds[i];
                    string atlasKey = cid + "_puppet_atlas";
                    string assetPath = "Assets/Mori/ForestSlice/Resources/ForestArt/" + atlasKey + ".png";
                    var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    var ps = imp != null ? imp.GetPlatformTextureSettings("WeixinMiniGame") : null;
                    Texture2D tex = Resources.Load<Texture2D>("ForestArt/" + atlasKey);
                    var prof = PuppetRegistry.GetProfile(cid);
                    var pup = new ForestHeroPuppet(tmpRoot, prof, "Verify_" + cid);
                    pup.SetSkinTier(OutfitTier.Refugee, autoFallbackToBase: true);

                    bool ok = tex != null && tex.width == 1024 && tex.height == 1024
                              && imp != null && imp.textureType == TextureImporterType.Default
                              && ps != null && ps.overridden
                              && ps.format == TextureImporterFormat.ASTC_4x4
                              && ps.maxTextureSize == 1024
                              && pup.ActiveAtlasKey == atlasKey;
                    if (ok) verifiedNewAtlases++;
                    else
                    {
                        allPassed = false;
                        report.AppendLine($"FAIL_NEW_ATLAS[{atlasKey}]: tex={(tex != null ? $"{tex.width}x{tex.height}" : "null")} type={imp?.textureType} wxFmt={ps?.format} wxMax={ps?.maxTextureSize} active={pup.ActiveAtlasKey}");
                    }
                }
                UnityEngine.Object.DestroyImmediate(tmpRoot.gameObject);
            }
            report.AppendLine($"PHASE2_ASSETS: fullbody={verifiedFullBody}/9 newBustAndShards={verifiedNewSprites}/6 newAtlases={verifiedNewAtlases}/3");

            static void ClearHudOverlays(ForestSliceBoot boot, System.Reflection.BindingFlags flags)
            {
                boot.Hud.Tick(0.02f);
                var mClearToast = typeof(ForestSurvivalHud).GetMethod("ClearFloatingWorldToasts", flags);
                mClearToast?.Invoke(boot.Hud, null);
                var fBanner = typeof(ForestSurvivalHud).GetField("_centerBannerGo", flags);
                ((GameObject)fBanner?.GetValue(boot.Hud))?.SetActive(false);
            }

            ForestSurvivalHud.EditorTopSafePadOverride = 48f;
            try
            {
                // -----------------------------------------------------------------
                // 1. p2_01_cabin_fullbody_dachui.png
                // Hero Cabin Detail (OpenHeroDetailModal(0, 0)) showing 大锤 standing FULL-BODY (fullbody_dachui)
                // head-to-boots on the cabin woven rug over a grounded oval shadow (zero banner overlap!).
                // -----------------------------------------------------------------
                CaptureArena720x1280(1, (boot, _) =>
                {
                    var sim = boot.Sim;
                    sim.CampLevel = 5; // MaxHeroLevelCap = 25 -> Lv.16/25 keeps LevelCapBanner hidden!
                    sim.Add(SurvRes.HeroExp, 18500 - sim.Get(SurvRes.HeroExp));
                    sim.Add(SurvRes.SkillBook, 46 - sim.Get(SurvRes.SkillBook));
                    sim.AddBadge(TacticalBadgeId.ThornShield, 2);
                    sim.AddBadge(TacticalBadgeId.EnergyCore, 2);

                    var h = sim.Heroes[0]; // 大锤
                    h.unlocked = true;
                    h.level = 16;
                    h.star = 2;
                    h.starPetals = 3;
                    h.shards = 9;
                    h.skillLevels[0] = 3;
                    h.skillLevels[1] = 2;
                    h.skillLevels[2] = 2;
                    h.equipTiers[0] = 2;
                    h.equipTiers[1] = 2;
                    h.equipTiers[2] = 2;
                    h.equipTiers[3] = 1;
                    h.equippedBadges[0] = TacticalBadgeId.ThornShield;

                    boot.Hud.OpenHeroDetailModal(0, 0);
                    ClearHudOverlays(boot, BF);
                }, (boot, _) =>
                {
                    var fCabin = typeof(ForestSurvivalHud).GetField("_wwHdCabinPortrait", BF);
                    var fCapBanner = typeof(ForestSurvivalHud).GetField("_wwHdCapBannerGo", BF);
                    var img = (UnityEngine.UI.Image)fCabin?.GetValue(boot.Hud);
                    var capGo = (GameObject)fCapBanner?.GetValue(boot.Hud);
                    bool ok = img != null && img.sprite != null && img.sprite.name.Contains("fullbody_dachui")
                              && (capGo == null || !capGo.activeInHierarchy);
                    if (!ok) allPassed = false;
                    CheckTopRightCapsuleClear(boot.Hud, "HeroCabinDetailModal", report);
                    report.AppendLine($"P2_01_DACHUI_CABIN: sprite={img?.sprite?.name} capBannerActive={capGo?.activeInHierarchy} ok={ok}");
                }, "p2_01_cabin_fullbody_dachui.png", OwnBrainDir, CoordBrainDir);

                // -----------------------------------------------------------------
                // 2. p2_02_cabin_fullbody_asa.png
                // Hero Cabin Detail (OpenHeroDetailModal(7, 0)) showing new Legendary IronGuard hero 阿飒
                // (fullbody_asa, Lv.20/25, 3★) with all 4 unlocked circular skill medallions!
                // -----------------------------------------------------------------
                CaptureArena720x1280(2, (boot, cam) =>
                {
                    var sim = boot.Sim;
                    sim.CampLevel = 5; // MaxHeroLevelCap = 25
                    sim.GmUnlockAllHeroesAndFormations();
                    sim.Add(SurvRes.HeroExp, 22400 - sim.Get(SurvRes.HeroExp));
                    sim.Add(SurvRes.SkillBook, 52 - sim.Get(SurvRes.SkillBook));
                    sim.AddBadge(TacticalBadgeId.EagleScope, 2);
                    sim.AddBadge(TacticalBadgeId.EnergyCore, 2);

                    var asa = sim.Heroes[7]; // 阿飒 (idx 7)
                    asa.unlocked = true;
                    asa.level = 20;
                    asa.star = 3;
                    asa.starPetals = 2;
                    asa.shards = 12;
                    asa.skillLevels[0] = 3; // 飞爪夺命钩
                    asa.skillLevels[1] = 2; // 猎杀本能
                    asa.skillLevels[2] = 2; // 孤狼猎手
                    asa.skillLevels[3] = 2; // 【夺命钩】增强
                    asa.equipTiers[0] = 3;
                    asa.equipTiers[1] = 2;
                    asa.equipTiers[2] = 2;
                    asa.equipTiers[3] = 2;
                    asa.equippedBadges[0] = TacticalBadgeId.EagleScope;
                    asa.equippedBadges[1] = TacticalBadgeId.EnergyCore;

                    sim.TryPlaceHeroInFormation(7, 1 * 3 + 2, out _);

                    boot.Hud.OpenHeroDetailModal(7, 0);
                    ClearHudOverlays(boot, BF);
                }, (boot, cam) =>
                {
                    var fCabin = typeof(ForestSurvivalHud).GetField("_wwHdCabinPortrait", BF);
                    var img = (UnityEngine.UI.Image)fCabin?.GetValue(boot.Hud);
                    var asa = boot.Sim.Heroes[7];
                    bool ok = img != null && img.sprite != null && img.sprite.name.Contains("fullbody_asa")
                              && asa.level == 20 && asa.star == 3;
                    if (!ok) allPassed = false;
                    CheckTopRightCapsuleClear(boot.Hud, "HeroCabinDetailModal", report);
                    report.AppendLine($"P2_02_ASA_CABIN: sprite={img?.sprite?.name} lv={asa.level}/{boot.Sim.MaxHeroLevelCap} star={asa.star} ok={ok}");
                }, "p2_02_cabin_fullbody_asa.png", OwnBrainDir, CoordBrainDir);

                // -----------------------------------------------------------------
                // 3. p2_03_cabin_zhoudr_skill_popup.png
                // Hero Cabin Detail for new Legendary WasteTech hero 周博士 (idx 8, fullbody_zhoudr)
                // + OpenHeroSkillDetailPopup(8, 0) (超载电磁风暴 5-tier table & highlighted description)!
                // -----------------------------------------------------------------
                CaptureArena720x1280(2, (boot, cam) =>
                {
                    var sim = boot.Sim;
                    sim.CampLevel = 5; // MaxHeroLevelCap = 25
                    sim.GmUnlockAllHeroesAndFormations();
                    sim.Add(SurvRes.HeroExp, 19600 - sim.Get(SurvRes.HeroExp));
                    sim.Add(SurvRes.SkillBook, 48 - sim.Get(SurvRes.SkillBook));

                    var zhou = sim.Heroes[8]; // 周博士 (idx 8)
                    zhou.unlocked = true;
                    zhou.level = 21;
                    zhou.star = 3;
                    zhou.starPetals = 1;
                    zhou.skillLevels[0] = 2; // 超载电磁风暴 Lv.2 -> Lv.3 highlighted!
                    zhou.skillLevels[1] = 2;
                    zhou.skillLevels[2] = 2;
                    zhou.skillLevels[3] = 1;
                    zhou.equipTiers[0] = 3;
                    zhou.equipTiers[1] = 2;
                    zhou.equipTiers[2] = 2;
                    zhou.equipTiers[3] = 3;

                    sim.TryPlaceHeroInFormation(8, 1 * 3 + 0, out _);

                    boot.Hud.OpenHeroDetailModal(8, 0);
                    boot.Hud.OpenHeroSkillDetailPopup(8, 0);
                    ClearHudOverlays(boot, BF);
                }, (boot, cam) =>
                {
                    var sk = boot.Sim.GetHeroSkillDef(8, 0);
                    bool ok = sk.name.Contains("超载电磁风暴");
                    if (!ok) allPassed = false;
                    CheckTopRightCapsuleClear(boot.Hud, "HeroCabinDetailModal", report);
                    report.AppendLine($"P2_03_ZHOUDR_SKILL_POPUP: skill={sk.name} lv={sk.currentLv}/{sk.maxLv} ok={ok}");
                }, "p2_03_cabin_zhoudr_skill_popup.png", OwnBrainDir, CoordBrainDir);

                // -----------------------------------------------------------------
                // 4. p2_04_roster_3x3_nine_heroes.png
                // Full-Screen Hero Roster (OpenHeroModal(1)) showing the 3x3 9-Hero Matrix
                // (8 unlocked vibrant full-body heroes including 青禾/阿飒/周博士 + 1 locked with shards, or all 9 visible!)
                // -----------------------------------------------------------------
                CaptureArena720x1280(2, (boot, cam) =>
                {
                    var sim = boot.Sim;
                    sim.CampLevel = 12;
                    sim.GmUnlockAllHeroesAndFormations();
                    sim.Add(SurvRes.HeroExp, 24800 - sim.Get(SurvRes.HeroExp));
                    sim.Add(SurvRes.SkillBook, 56 - sim.Get(SurvRes.SkillBook));
                    sim.Add(SurvRes.RecruitTicket, 15 - sim.Get(SurvRes.RecruitTicket));

                    int[] lvs = { 23, 20, 21, 22, 18, 1, 19, 22, 21 };
                    int[] sts = { 3, 2, 3, 3, 2, 1, 2, 3, 3 };
                    for (int i = 0; i < sim.Heroes.Count; i++)
                    {
                        var h = sim.Heroes[i];
                        h.unlocked = (i != 5); // 8 unlocked (incl. 青禾/阿飒/周博士!) + 老陈头 locked (8/10 shards)
                        h.level = lvs[i];
                        h.star = sts[i];
                        h.starPetals = (i % 3) + 1;
                        h.shards = (i == 5) ? 8 : 6;
                        if (h.unlocked)
                        {
                            h.equipTiers[0] = 2;
                            h.equipTiers[1] = 2;
                        }
                    }

                    for (int s = 0; s < 9; s++) sim.FormationSlots[s] = -1;
                    sim.TryPlaceHeroInFormation(0, 1 * 3 + 2, out _); // 大锤
                    sim.TryPlaceHeroInFormation(2, 2 * 3 + 2, out _); // 雷猛
                    sim.TryPlaceHeroInFormation(7, 0 * 3 + 2, out _); // 阿飒
                    sim.TryPlaceHeroInFormation(6, 1 * 3 + 1, out _); // 青禾
                    sim.TryPlaceHeroInFormation(8, 0 * 3 + 0, out _); // 周博士

                    boot.Hud.OpenHeroModal(1);
                    ClearHudOverlays(boot, BF);
                }, (boot, cam) =>
                {
                    var fBusts = typeof(ForestSurvivalHud).GetField("_haBusts", BF);
                    var imgs = (UnityEngine.UI.Image[])fBusts?.GetValue(boot.Hud);
                    bool ok = imgs != null && imgs.Length == 9 && boot.Sim.Heroes.Count == 9;
                    for (int i = 0; i < 9 && ok; i++)
                    {
                        if (imgs[i] == null || imgs[i].sprite == null || !imgs[i].sprite.name.StartsWith("fullbody_"))
                            ok = false;
                    }
                    if (!ok) allPassed = false;
                    CheckTopRightCapsuleClear(boot.Hud, "HeroAssignModal", report);
                    report.AppendLine($"P2_04_ROSTER_3X3: count={boot.Sim.Heroes.Count} fullbodyCardsOk={ok}");
                }, "p2_04_roster_3x3_nine_heroes.png", OwnBrainDir, CoordBrainDir);

                // -----------------------------------------------------------------
                // 5. p2_05_chalkboard_fullbody_3faction_aura.png
                // OpenTacticalFormationModal() showing 5 deployed heroes standing FULL-BODY on #163748 chalkboard cells
                // (including 阿飒, 青禾, 周博士, with 3-person pure-faction +25% Wangpai IronGuard Faction Aura lit up!),
                // XX级 at their boots, and the 9-hero scrollable bottom strip!
                // -----------------------------------------------------------------
                CaptureArena720x1280(2, (boot, cam) =>
                {
                    var sim = boot.Sim;
                    sim.CampLevel = 12; // 5-hero squad cap
                    sim.GmUnlockAllHeroesAndFormations();

                    int[] lvs = { 24, 19, 21, 20, 18, 18, 20, 22, 23 };
                    int[] sts = { 3, 2, 3, 2, 2, 2, 2, 3, 3 }; // 大锤(Leg), 雷猛(3★), 阿飒(Leg) -> 3 Ace IronGuard -> +25% 王牌铁卫团光环!
                    for (int i = 0; i < sim.Heroes.Count; i++)
                    {
                        var h = sim.Heroes[i];
                        h.unlocked = true;
                        h.level = lvs[i];
                        h.star = sts[i];
                        h.starPetals = 2;
                        h.equipTiers[0] = 2;
                        h.equipTiers[1] = 2;
                    }

                    for (int s = 0; s < 9; s++) sim.FormationSlots[s] = -1;
                    sim.TryPlaceHeroInFormation(8, 0 * 3 + 0, out _); // 周博士 (Row 0, Back)
                    sim.TryPlaceHeroInFormation(7, 0 * 3 + 2, out _); // 阿飒   (Row 0, Front)
                    sim.TryPlaceHeroInFormation(6, 1 * 3 + 0, out _); // 青禾   (Row 1, Back)
                    sim.TryPlaceHeroInFormation(0, 1 * 3 + 2, out _); // 大锤   (Row 1, Front)
                    sim.TryPlaceHeroInFormation(2, 2 * 3 + 1, out _); // 雷猛   (Row 2, Mid)
                    sim.TrySelectFormation(TacticalFormationId.SpearAssault, out _);

                    boot.Hud.OpenTacticalFormationModal();

                    // Scroll bottom 9-hero horizontal strip slightly right so new heroes (青禾/阿飒/周博士) & classic heroes both show cleanly!
                    var fScroll = typeof(ForestSurvivalHud).GetField("_wwTfRosterScrollContentRt", BF);
                    var scrollRt = (RectTransform)fScroll?.GetValue(boot.Hud);
                    if (scrollRt != null)
                        scrollRt.anchoredPosition = new Vector2(-348f, scrollRt.anchoredPosition.y);

                    ClearHudOverlays(boot, BF);
                }, (boot, cam) =>
                {
                    boot.Sim.GetFactionAuraState(out string auraTitle, out _, out float bonusPct, out int ironCnt, out _, out _);
                    bool ok = boot.Sim.GetDeployedHeroCount() == 5 && ironCnt == 3 && bonusPct >= 0.15f;
                    if (!ok) allPassed = false;
                    CheckTopRightCapsuleClear(boot.Hud, "TacticalFormationModal", report);
                    report.AppendLine($"P2_05_CHALKBOARD: deployed={boot.Sim.GetDeployedHeroCount()} iron={ironCnt} aura={auraTitle}({bonusPct:P0}) ok={ok}");
                }, "p2_05_chalkboard_fullbody_3faction_aura.png", OwnBrainDir, CoordBrainDir);

                // -----------------------------------------------------------------
                // 6. p2_06_radio_fullbody_showcase.png
                // OpenRadioRecruitModal() showing the 5 featured heroes (阿飒, 周博士, 青禾, 大锤, 林砚) standing FULL-BODY!
                // -----------------------------------------------------------------
                CaptureArena720x1280(1, (boot, _) =>
                {
                    var sim = boot.Sim;
                    CampNode rp = sim.Find("radio_post");
                    if (rp != null) { rp.state = NodeState.Active; rp.level = 2; }
                    sim.CampLevel = 6;
                    sim.TotalRecruitPulls = 17;
                    sim.RecruitPityCounter = 7; // 3 pulls until guaranteed!
                    sim.Add(SurvRes.RecruitTicket, 20 - sim.Get(SurvRes.RecruitTicket));

                    sim.Heroes[0].unlocked = true; sim.Heroes[0].star = 2; // 大锤
                    sim.Heroes[3].unlocked = true; sim.Heroes[3].star = 2; // 林砚
                    sim.Heroes[6].unlocked = true; sim.Heroes[6].star = 1; // 青禾
                    sim.Heroes[7].unlocked = false; sim.Heroes[7].shards = 8; // 阿飒 8/10
                    sim.Heroes[8].unlocked = false; sim.Heroes[8].shards = 6; // 周博士 6/10

                    boot.Hud.OpenRadioRecruitModal();
                    ClearHudOverlays(boot, BF);
                }, (boot, _) =>
                {
                    var fFeat = typeof(ForestSurvivalHud).GetField("_wwGachaFeatBusts", BF);
                    var featImgs = (UnityEngine.UI.Image[])fFeat?.GetValue(boot.Hud);
                    bool ok = featImgs != null && featImgs.Length == 5;
                    for (int i = 0; i < 5 && ok; i++)
                    {
                        if (featImgs[i] == null || featImgs[i].sprite == null || !featImgs[i].sprite.name.StartsWith("fullbody_"))
                            ok = false;
                    }
                    if (!ok) allPassed = false;
                    CheckTopRightCapsuleClear(boot.Hud, "RadioRecruitModal", report);
                    report.AppendLine($"P2_06_RADIO_SHOWCASE: fullbodyFeatOk={ok} s0={featImgs?[0]?.sprite?.name} s1={featImgs?[1]?.sprite?.name} s2={featImgs?[2]?.sprite?.name}");
                }, "p2_06_radio_fullbody_showcase.png", OwnBrainDir, CoordBrainDir);

                // -----------------------------------------------------------------
                // 7. p2_07_bag_9hero_jigsaw_shards.png
                // OpenBagInventoryModal(2) on [碎片] tab showing all 9 Hero Jigsaw Shards
                // (including ico_shard_asa, ico_shard_qinghe, ico_shard_zhoudr!) + Universal Shards!
                // -----------------------------------------------------------------
                CaptureArena720x1280(2, (boot, _) =>
                {
                    var sim = boot.Sim;
                    sim.CampLevel = 8;
                    sim.Add(SurvRes.ShardUnivLeg, 18 - sim.Get(SurvRes.ShardUnivLeg));
                    sim.Add(SurvRes.ShardUnivEpic, 26 - sim.Get(SurvRes.ShardUnivEpic));

                    int[] shardCounts = { 9, 7, 11, 8, 6, 5, 14, 12, 8 };
                    for (int i = 0; i < sim.Heroes.Count; i++)
                    {
                        var h = sim.Heroes[i];
                        h.shards = shardCounts[i];
                        if (i == 7)
                        {
                            // Keep 阿飒 locked with 12/10 shards to highlight [合成英雄] in the bottom inspector!
                            h.unlocked = false;
                        }
                        else if (i == 8)
                        {
                            h.unlocked = false;
                        }
                        else
                        {
                            h.unlocked = true;
                            h.star = (i == 0 || i == 3) ? 2 : 1;
                        }
                    }

                    sim.HordeWaveCleared = 12;
                    boot.Hud.OpenBagInventoryModal(2); // [碎片] tab
                    var fSelKey = typeof(ForestSurvivalHud).GetField("_wwBagSelectedKey", BF);
                    var mRefreshBag = typeof(ForestSurvivalHud).GetMethod("RefreshBagInventoryModal", BF);
                    fSelKey?.SetValue(boot.Hud, "shard_hero_asa");
                    mRefreshBag?.Invoke(boot.Hud, null);
                    ClearHudOverlays(boot, BF);
                }, (boot, _) =>
                {
                    Sprite sAsa = Resources.Load<Sprite>("ForestArt/ico_shard_asa");
                    Sprite sQh = Resources.Load<Sprite>("ForestArt/ico_shard_qinghe");
                    Sprite sZd = Resources.Load<Sprite>("ForestArt/ico_shard_zhoudr");
                    bool ok = sAsa != null && sQh != null && sZd != null && boot.Sim.Heroes.Count == 9;
                    if (!ok) allPassed = false;
                    CheckTopRightCapsuleClear(boot.Hud, "BagInventoryModal", report);
                    report.AppendLine($"P2_07_BAG_SHARDS: heroes={boot.Sim.Heroes.Count} asaShards={boot.Sim.Heroes[7].shards} iconsOk={ok}");
                }, "p2_07_bag_9hero_jigsaw_shards.png", OwnBrainDir, CoordBrainDir);

                // -----------------------------------------------------------------
                // 8. p2_08_arena_new_heroes_skeletal_combat.png
                // Live Tactical Bridge Arena battle with 阿飒 (asa_puppet_atlas), 青禾 (qinghe_puppet_atlas),
                // and 周博士 (zhoudr_puppet_atlas) fighting alongside allies in full 9-part skeletal animation
                // triggering 飞爪夺命钩 / 霜寒捕兽夹阵 / 超载电磁风暴 VFX in-frame!
                // -----------------------------------------------------------------
                CaptureArena720x1280(2, (boot, cam) =>
                {
                    var sim = boot.Sim;
                    sim.CampLevel = 12;
                    sim.GmUnlockAllHeroesAndFormations();

                    for (int i = 0; i < sim.Heroes.Count; i++)
                    {
                        var h = sim.Heroes[i];
                        h.unlocked = true;
                        h.level = 10; // Tuned against Stage 18 so enemies survive all 3 ultimates mid-frame!
                        h.star = 3;   // Unlock all 2★/3★ awakenings!
                        h.skillLevels[0] = 2;
                        h.skillLevels[1] = 2;
                        h.skillLevels[2] = 2;
                        h.skillLevels[3] = 2;
                        h.skinTier = OutfitTier.Refugee;
                        h.portraitKey = PuppetRegistry.ResolveBustKey(h.characterId, OutfitTier.Refugee);
                    }

                    // Spread 5 heroes cleanly across the 3 lanes so zero puppet silhouettes overlap:
                    // Row 0 (Top): 周博士 (Back-Top), 雷猛 (Front-Top)
                    // Row 1 (Mid): 阿飒   (Mid-Center) -> center-stage skirmisher grappling hook!
                    // Row 2 (Bot): 青禾   (Back-Bot), 大锤 (Front-Bot)
                    for (int s = 0; s < 9; s++) sim.FormationSlots[s] = -1;
                    sim.TryPlaceHeroInFormation(8, 0 * 3 + 0, out _); // 周博士
                    sim.TryPlaceHeroInFormation(2, 0 * 3 + 2, out _); // 雷猛
                    sim.TryPlaceHeroInFormation(7, 1 * 3 + 1, out _); // 阿飒
                    sim.TryPlaceHeroInFormation(6, 2 * 3 + 0, out _); // 青禾
                    sim.TryPlaceHeroInFormation(0, 2 * 3 + 2, out _); // 大锤
                    sim.TrySelectFormation(TacticalFormationId.SpearAssault, out _);

                    sim.GmSetBattleStage(18);
                    boot.Hud.StartWaveDefenseBattle();
                    var arena = boot.World.Arena;
                    arena.AutoCastUlt = false;

                    // Advance 2.35s (47 * 0.05s) so zombies rush well inside the bridge combat zone
                    for (int step = 0; step < 47; step++)
                    {
                        boot.Sim.Tick(0.05f);
                        boot.World.Tick(0.05f);
                        boot.View.Tick(0.05f);
                        boot.Hud.Tick(0.05f);
                    }

                    // Helper to top up rage01 on deployed HeroUnits
                    var fHeroes = typeof(ForestTacticalArena).GetField("_heroes", BF);
                    var heroUnits = (System.Collections.IList)fHeroes.GetValue(arena);
                    void FillHeroRage(int targetHeroIdx)
                    {
                        for (int u = 0; u < heroUnits.Count; u++)
                        {
                            object hu = heroUnits[u];
                            int hIdx = (int)hu.GetType().GetField("heroIdx").GetValue(hu);
                            if (hIdx == targetHeroIdx)
                                hu.GetType().GetField("rage01").SetValue(hu, 1.0f);
                        }
                    }

                    // 1) Cast 青禾's 霜寒捕兽夹阵 first and advance 0.25s so cryo-flask impacts & spawns ground frost circle + "寒霜冻结!"
                    FillHeroRage(6);
                    arena.TryCastHeroUlt(6);
                    for (int step = 0; step < 5; step++)
                    {
                        boot.Sim.Tick(0.05f);
                        boot.World.Tick(0.05f);
                        boot.View.Tick(0.05f);
                        boot.Hud.Tick(0.05f);
                    }

                    // 2) Cast 阿飒's 飞爪夺命钩 AND 周博士's 超载电磁风暴, then advance 0.12s so hook chain, frost circle & chain lightning are all live simultaneously with 阿飒 unobstructed!
                    FillHeroRage(7);
                    FillHeroRage(8);
                    arena.TryCastHeroUlt(7);
                    arena.TryCastHeroUlt(8);
                    for (int step = 0; step < 3; step++)
                    {
                        boot.Sim.Tick(0.04f);
                        boot.World.Tick(0.04f);
                        boot.View.Tick(0.04f);
                        boot.Hud.Tick(0.04f);
                    }

                    var mClearToast = typeof(ForestSurvivalHud).GetMethod("ClearFloatingWorldToasts", BF);
                    mClearToast?.Invoke(boot.Hud, null);
                    var fBanner = typeof(ForestSurvivalHud).GetField("_centerBannerGo", BF);
                    ((GameObject)fBanner?.GetValue(boot.Hud))?.SetActive(false);
                }, (boot, _) =>
                {
                    var arena = boot.World.Arena;
                    var fHeroes = typeof(ForestTacticalArena).GetField("_heroes", BF);
                    var fEnemies = typeof(ForestTacticalArena).GetField("_aliveEnemies", BF);
                    var heroUnits = (System.Collections.IList)fHeroes.GetValue(arena);
                    var enemyUnits = (System.Collections.IList)fEnemies.GetValue(arena);

                    bool asaOk = false, qingheOk = false, zhoudrOk = false;
                    for (int i = 0; i < heroUnits.Count; i++)
                    {
                        object hu = heroUnits[i];
                        string hid = (string)hu.GetType().GetField("heroId").GetValue(hu);
                        var pup = (ForestHeroPuppet)hu.GetType().GetField("puppet").GetValue(hu);
                        if (hid == "asa" && pup?.ActiveAtlasKey == "asa_puppet_atlas") asaOk = true;
                        if (hid == "qinghe" && pup?.ActiveAtlasKey == "qinghe_puppet_atlas") qingheOk = true;
                        if (hid == "zhoudr" && pup?.ActiveAtlasKey == "zhoudr_puppet_atlas") zhoudrOk = true;
                    }
                    int aliveZ = 0;
                    for (int i = 0; i < enemyUnits.Count; i++)
                    {
                        object eu = enemyUnits[i];
                        if ((bool)eu.GetType().GetField("alive").GetValue(eu)) aliveZ++;
                    }
                    bool ok = arena.IsBattleActive && !arena.IsResultPending && asaOk && qingheOk && zhoudrOk && aliveZ >= 2;
                    if (!ok) allPassed = false;
                    CheckTopRightCapsuleClear(boot.Hud, "WaveCombatHud", report);
                    report.AppendLine($"P2_08_ARENA_COMBAT: active={arena.IsBattleActive} asa={asaOk} qinghe={qingheOk} zhoudr={zhoudrOk} aliveEnemies={aliveZ} ok={ok}");
                }, "p2_08_arena_new_heroes_skeletal_combat.png", OwnBrainDir, CoordBrainDir);
            }
            finally
            {
                ForestSurvivalHud.EditorTopSafePadOverride = 0f;
            }

            // Restore clean runnable scene
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("MainCamera", typeof(Camera)) { tag = "MainCamera" };
            new GameObject("ForestSliceBoot", typeof(ForestSliceBoot));
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);

            report.AppendLine($"ALL_PHASE2_VERIFICATIONS_PASSED={allPassed}");
            File.WriteAllText("/tmp/mori_phase2_verify_report.txt", report.ToString());
            Debug.Log("[VerifyPhase2FullBodyAndNineHeroes]\n" + report);
            if (!allPassed)
                EditorApplication.Exit(1);
        }
    }
}
