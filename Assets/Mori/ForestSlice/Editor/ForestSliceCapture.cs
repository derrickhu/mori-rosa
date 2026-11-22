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

        [MenuItem("废土狂想营/深山样板/打开并运行", priority = 61)]
        public static void OpenAndPlay()
        {
            AssetDatabase.Refresh();
            if (!File.Exists(ScenePath)) Run();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
        }

        [MenuItem("废土狂想营/深山样板/重建并截图", priority = 60)]
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

            // 保存默认可玩场景（startStage = 0 新手完整引导开局）
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var c = new GameObject("MainCamera", typeof(Camera));
            c.tag = "MainCamera";
            new GameObject("ForestSliceBoot", typeof(ForestSliceBoot));
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            Debug.Log("[ForestSlice] All 8 UI/UX overhaul screenshots captured -> " + OutDir);
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
    }
}
