using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mori
{
    public enum BottomTab { Camp = 0, Hero = 1, Explore = 2, Trial = 3, Wasteland = 4 }

    /// <summary>
    /// 营地首屏 HUD（严格避让右上微信胶囊安全区，保持中场营地视野通透）：
    /// - 顶栏：领袖等级盾牌 + 人口条 + 斜切瓶盖金币槽 + 斜切辐射晶钻槽
    /// - 建筑世界锚点投射层：地标铭牌 + 绿色/蓝色成品收取气泡 + 金色可建气泡
    /// - 左侧快捷入口：幸存者订单(红点) + 自动生产特权(IAA)
    /// - 左下主线指引条 + 底部5槽奶油贴纸Dock栏
    /// </summary>
    public sealed class CampHudView : MonoBehaviour
    {
        MoriApp _app;
        RectTransform _root;
        RectTransform _bubbleLayer;
        RectTransform _questBarRoot;
        RectTransform _quickCraftRoot;
        RectTransform _activeTabBlock;

        Text _lvText;
        Text _popText;
        Text _coinText;
        Text _gemText;
        Text _questText;
        Text _autoProdSubText;

        static readonly string[] TabNames = { "营地", "英雄", "探险", "副本", "废土" };
        static readonly string[] TabIcons =
        {
            "ico_nav_camp", "ico_nav_hero", "ico_nav_explore", "ico_nav_trial", "ico_nav_wasteland"
        };

        public void Init(MoriApp app, RectTransform root)
        {
            _app = app;
            _root = root;
            BuildLayout();
            Refresh();
        }

        void BuildLayout()
        {
            // 1. 世界建筑气泡与铭牌投射层（压在场景之上、顶底HUD之下）
            _bubbleLayer = MoriUiKit.Node(_root, "WorldBubbleLayer",
                new Vector2(MoriUiKit.DesignW, MoriUiKit.DesignH), Vector2.zero,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            _bubbleLayer.offsetMin = Vector2.zero;
            _bubbleLayer.offsetMax = Vector2.zero;

            // 2. 顶部资源安全栏（y=46 起，右侧预留 185px 给微信右上胶囊按钮）
            float topY = 46f;

            // 左上领袖等级盾牌
            RectTransform lvBox = MoriUiKit.TopLeftNode(_root, "PlayerLvShield", 18f, topY - 4f, 76f, 76f);
            MoriUiKit.SpriteImg(lvBox, "Bg", "ui/hud_level_shield", new Vector2(76f, 76f), Vector2.zero, preserveAspect: true);
            MoriUiKit.Label(lvBox, "Tag", "领袖", 15, MoriUiKit.WarmCream, new Vector2(70f, 22f), new Vector2(0f, 17f));
            _lvText = MoriUiKit.Label(lvBox, "LvNum", "Lv.5", 24, MoriUiKit.WarmCream, new Vector2(72f, 30f), new Vector2(0f, -8f));

            // 人口门槛状态槽
            RectTransform popPill = MoriUiKit.TopLeftNode(_root, "PopPill", 102f, topY + 2f, 134f, 38f);
            MoriUiKit.SpriteImg(popPill, "Bg", "ui/hud_currency_pill", new Vector2(134f, 38f), Vector2.zero);
            MoriUiKit.SpriteImg(popPill, "Ico", "ico_nav_camp", new Vector2(36f, 36f), new Vector2(-46f, 1f), preserveAspect: true);
            _popText = MoriUiKit.Label(popPill, "Val", "18/40人", 18, MoriUiKit.WarmCream,
                new Vector2(92f, 32f), new Vector2(12f, 1f));

            // 瓶盖金币斜切槽
            RectTransform coinPill = MoriUiKit.TopLeftNode(_root, "CoinPill", 244f, topY + 2f, 148f, 38f);
            MoriUiKit.SpriteImg(coinPill, "Bg", "ui/hud_currency_pill", new Vector2(148f, 38f), Vector2.zero);
            MoriUiKit.SpriteImg(coinPill, "Ico", "ico_coin", new Vector2(40f, 40f), new Vector2(-54f, 1f), preserveAspect: true);
            _coinText = MoriUiKit.Label(coinPill, "Val", "12,850", 19, MoriUiKit.WarmCream,
                new Vector2(96f, 32f), new Vector2(12f, 1f));

            // 辐射晶钻斜切槽（右缘停在 x=526，完美避开微信胶囊 535..705 区域）
            RectTransform gemPill = MoriUiKit.TopLeftNode(_root, "GemPill", 398f, topY + 2f, 128f, 38f);
            MoriUiKit.SpriteImg(gemPill, "Bg", "ui/hud_currency_pill", new Vector2(128f, 38f), Vector2.zero);
            MoriUiKit.SpriteImg(gemPill, "Ico", "ico_gem", new Vector2(38f, 38f), new Vector2(-44f, 2f), preserveAspect: true);
            _gemText = MoriUiKit.Label(gemPill, "Val", "680", 19, MoriUiKit.WarmCream,
                new Vector2(82f, 32f), new Vector2(10f, 1f));

            // 第二排小经验条贴在人口槽下方
            RectTransform expWrap = MoriUiKit.TopLeftNode(_root, "ExpTrack", 104f, topY + 46f, 210f, 22f);
            MoriUiKit.ProgressBar(expWrap, "Bar", 210f, 22f, 0.60f, goldFill: true, "升级经验360/600", Vector2.zero);

            // 3. 左侧快捷功能竖列（参考《疯狂水世界》清爽侧边入口）
            BuildSideButton("SideOrders", 18f, 152f, "ico_orders", "居民订单", "2单可交", true,
                () => _app.OpenModal(ModalKind.Orders));
            _autoProdSubText = BuildSideButton("SideAutoProd", 18f, 256f, "ico_scrap_wood", "自动生产", "免费4h", false,
                () =>
                {
                    _app.Sim.ActivateAutoProdAd();
                    _app.OpenModal(ModalKind.Smokehouse);
                });

            // 4. 左下主线任务引导胶囊（贴在 Dock 上方 18px）
            RectTransform questBar = MoriUiKit.Node(_root, "MainQuestBar", new Vector2(486f, 74f),
                new Vector2(18f, 148f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            _questBarRoot = questBar;
            MoriUiKit.SpriteImg(questBar, "Bg", "ui/hud_quest_bar", new Vector2(486f, 74f), Vector2.zero, sliced: true);
            MoriUiKit.ItemSlot(questBar, "QuestReward", Rarity.Rare, "ico_smoked_ribs", "x2", 56f, new Vector2(-202f, 2f));

            MoriUiKit.Label(questBar, "Tag", "主线任务·第5章", 15, MoriUiKit.AccentGold,
                new Vector2(260f, 22f), new Vector2(-34f, 16f), TextAnchor.MiddleLeft);
            _questText = MoriUiKit.Label(questBar, "Desc", "收取熏肉灶烤排并完成1单居民交付", 18, MoriUiKit.WarmCream,
                new Vector2(285f, 28f), new Vector2(-22f, -10f), TextAnchor.MiddleLeft);

            MoriUiKit.ActionButton(questBar, "BtnGo", "ui/btn_hex_gold", "前往",
                new Vector2(106f, 48f), new Vector2(178f, 1f), 20, sliced: false, darkText: true,
                () => _app.OpenModal(ModalKind.Smokehouse));

            // 右下快捷「熏肉工坊」直达按钮
            RectTransform quickCraft = MoriUiKit.Node(_root, "QuickWorkshopBtn", new Vector2(180f, 64f),
                new Vector2(-18f, 152f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));
            _quickCraftRoot = quickCraft;
            MoriUiKit.ActionButton(quickCraft, "Btn", "ui/btn_hex_green", "工坊队列(1)",
                new Vector2(180f, 64f), Vector2.zero, 20, sliced: false, darkText: true,
                () => _app.OpenModal(ModalKind.Smokehouse));

            // 5. 底部 5 槽奶油贴纸主导航 Dock（高 130px）
            RectTransform dock = MoriUiKit.Node(_root, "BottomDock", new Vector2(MoriUiKit.DesignW, 130f),
                Vector2.zero, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
            dock.sizeDelta = new Vector2(0f, 130f);
            MoriUiKit.SpriteImg(dock, "DockBg", "ui/dock_bg", new Vector2(MoriUiKit.DesignW, 130f), Vector2.zero, sliced: true);

            float cellW = MoriUiKit.DesignW / 5f;
            _activeTabBlock = MoriUiKit.Node(dock, "ActiveHighlight", new Vector2(136f, 114f),
                new Vector2(-MoriUiKit.DesignW * 0.5f + cellW * 0.5f, 4f));
            MoriUiKit.SpriteImg(_activeTabBlock, "Img", "ui/dock_tab_active", new Vector2(136f, 114f), Vector2.zero, preserveAspect: false);

            for (int i = 0; i < 5; i++)
            {
                int idx = i;
                float cx = -MoriUiKit.DesignW * 0.5f + cellW * (i + 0.5f);
                RectTransform tabRt = MoriUiKit.Node(dock, "Tab_" + i, new Vector2(cellW, 122f), new Vector2(cx, 2f));
                var hitImg = tabRt.gameObject.AddComponent<Image>();
                hitImg.color = new Color(1f, 1f, 1f, 0.001f);
                var btn = tabRt.gameObject.AddComponent<Button>();
                btn.targetGraphic = hitImg;
                btn.onClick.AddListener(() => OnClickTab((BottomTab)idx));

                MoriUiKit.SpriteImg(tabRt, "Ico", TabIcons[i], new Vector2(68f, 68f), new Vector2(0f, 16f), preserveAspect: true);
                MoriUiKit.Label(tabRt, "Txt", TabNames[i], 20, MoriUiKit.WarmCream,
                    new Vector2(cellW, 28f), new Vector2(0f, -34f), stroked: true);
            }
        }

        Text BuildSideButton(string name, float x, float yFromTop, string iconKey, string title, string badgeText,
            bool hotRed, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform box = MoriUiKit.TopLeftNode(_root, name, x, yFromTop, 82f, 88f);
            Image bg = MoriUiKit.SpriteImg(box, "Bg", "ui/hud_side_btn", new Vector2(76f, 78f), new Vector2(0f, 5f), preserveAspect: true);
            bg.raycastTarget = true;
            var btn = bg.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(onClick);

            MoriUiKit.SpriteImg(box, "Ico", iconKey, new Vector2(52f, 52f), new Vector2(0f, 10f), preserveAspect: true);
            MoriUiKit.Label(box, "Title", title, 16, MoriUiKit.WarmCream, new Vector2(92f, 24f), new Vector2(0f, -32f));

            // 右上角标胶囊
            RectTransform tag = MoriUiKit.Node(box, "Tag", new Vector2(68f, 22f), new Vector2(18f, 34f));
            MoriUiKit.SpriteImg(tag, "TagBg", hotRed ? "ui/btn_close_red" : "ui/btn_round_green",
                new Vector2(68f, 22f), Vector2.zero, sliced: !hotRed);
            return MoriUiKit.Label(tag, "TagTxt", badgeText, 13, MoriUiKit.WarmCream, new Vector2(66f, 22f), new Vector2(0f, 1f));
        }

        void OnClickTab(BottomTab tab)
        {
            float cellW = MoriUiKit.DesignW / 5f;
            _activeTabBlock.anchoredPosition = new Vector2(-MoriUiKit.DesignW * 0.5f + cellW * ((int)tab + 0.5f), 4f);

            if (tab == BottomTab.Camp) _app.OpenModal(ModalKind.None);
            else if (tab == BottomTab.Hero) _app.OpenModal(ModalKind.HeroSam);
            else if (tab == BottomTab.Explore || tab == BottomTab.Trial || tab == BottomTab.Wasteland)
                _app.OpenModal(ModalKind.Orders);
        }

        public void SelectTabVisual(BottomTab tab)
        {
            float cellW = MoriUiKit.DesignW / 5f;
            _activeTabBlock.anchoredPosition = new Vector2(-MoriUiKit.DesignW * 0.5f + cellW * ((int)tab + 0.5f), 4f);
        }

        public void SetWorldHudVisible(bool visible)
        {
            if (_bubbleLayer != null) _bubbleLayer.gameObject.SetActive(visible);
            if (_questBarRoot != null) _questBarRoot.gameObject.SetActive(visible);
            if (_quickCraftRoot != null) _quickCraftRoot.gameObject.SetActive(visible);
        }

        public void Refresh()
        {
            SimEngine sim = _app.Sim;
            _lvText.text = "Lv." + sim.PlayerLevel;
            _popText.text = $"{sim.CurrentPopulation}/{sim.CurrentPopCap}人";
            _coinText.text = sim.GetResource(ResourceId.Coin).ToString("N0");
            _gemText.text = sim.GetResource(ResourceId.Gem).ToString("N0");
            if (_autoProdSubText != null)
                _autoProdSubText.text = sim.AutoProdAdActive ? "生效中" : "免费4h";

            RebuildWorldBubbles();
        }

        void RebuildWorldBubbles()
        {
            MoriUiKit.ClearChildren(_bubbleLayer);

            Camera cam = _app.World.WorldCamera;
            if (cam == null) return;

            IReadOnlyList<CampWorldView.PlotVisual> visuals = _app.World.PlotVisuals;
            for (int i = 0; i < visuals.Count; i++)
            {
                CampWorldView.PlotVisual pv = visuals[i];
                PlotRuntime rt = _app.Sim.GetPlot(pv.def.plotId);
                if (rt == null || rt.state == PlotState.Hidden) continue;

                // 世界坐标换算到 720x1280 Canvas 中心原点坐标
                Vector3 baseVp = cam.WorldToViewportPoint(pv.root.position);
                Vector2 baseUi = new Vector2((baseVp.x - 0.5f) * MoriUiKit.DesignW, (baseVp.y - 0.5f) * MoriUiKit.DesignH);

                // 建筑脚下铭牌小黑条
                string plateStr = rt.state == PlotState.Built
                    ? $"{pv.def.displayName}·Lv.{rt.level}"
                    : $"{pv.def.displayName}(待建)";
                RectTransform plate = MoriUiKit.Node(_bubbleLayer, "Name_" + pv.def.plotId,
                    new Vector2(138f, 26f), baseUi + new Vector2(0f, -10f));
                MoriUiKit.SpriteImg(plate, "Bg", "ui/bar_track_dark", new Vector2(138f, 26f), Vector2.zero, sliced: true);
                MoriUiKit.Label(plate, "Txt", plateStr, 14,
                    rt.state == PlotState.Built ? MoriUiKit.WarmCream : MoriUiKit.AccentGold,
                    new Vector2(134f, 24f), new Vector2(0f, 1f));

                // 建筑头顶交互气泡
                Vector3 topWorld = pv.root.position + new Vector3(0f, (pv.def.kind == BuildingKind.CoreHq ? 2.15f : 1.75f) * pv.def.visualScale, 0f);
                Vector3 topVp = cam.WorldToViewportPoint(topWorld);
                Vector2 topUi = new Vector2((topVp.x - 0.5f) * MoriUiKit.DesignW, (topVp.y - 0.5f) * MoriUiKit.DesignH);

                if (rt.readyCollectCount > 0)
                {
                    string bKey = rt.readyCollectRes == ResourceId.CleanWater ? "ui/bubble_blue" : "ui/bubble_green";
                    string ico = MoriCatalog.ResourceIcon(rt.readyCollectRes);
                    int pid = pv.def.plotId;
                    RectTransform bub = MoriUiKit.Node(_bubbleLayer, "Bub_" + pid, new Vector2(72f, 80f), topUi);
                    Image bImg = MoriUiKit.SpriteImg(bub, "Bg", bKey, new Vector2(72f, 80f), Vector2.zero, preserveAspect: true);
                    bImg.raycastTarget = true;
                    var btn = bImg.gameObject.AddComponent<Button>();
                    btn.onClick.AddListener(() =>
                    {
                        _app.Sim.CollectPlotOutput(pid, out _, out _);
                        if (pid == 2) _app.OpenModal(ModalKind.Smokehouse);
                        Refresh();
                    });
                    MoriUiKit.SpriteImg(bub, "Ico", ico, new Vector2(42f, 42f), new Vector2(0f, 6f), preserveAspect: true);
                    MoriUiKit.Label(bub, "Cnt", "x" + rt.readyCollectCount, 15, MoriUiKit.WarmCream,
                        new Vector2(64f, 22f), new Vector2(0f, -14f));
                }
                else if (rt.state == PlotState.Outline)
                {
                    RectTransform bub = MoriUiKit.Node(_bubbleLayer, "BubBuild_" + pv.def.plotId, new Vector2(72f, 80f), topUi);
                    MoriUiKit.SpriteImg(bub, "Bg", "ui/bubble_gold", new Vector2(72f, 80f), Vector2.zero, preserveAspect: true);
                    MoriUiKit.SpriteImg(bub, "Ico", "ico_blueprint", new Vector2(40f, 40f), new Vector2(0f, 8f), preserveAspect: true);
                    MoriUiKit.Label(bub, "Txt", "可建造", 14, MoriUiKit.WarmCream, new Vector2(68f, 22f), new Vector2(0f, -14f));
                }
                else if (pv.def.kind == BuildingKind.CoreHq)
                {
                    RectTransform bub = MoriUiKit.Node(_bubbleLayer, "BubHq", new Vector2(122f, 34f), topUi + new Vector2(0f, -6f));
                    MoriUiKit.SpriteImg(bub, "Bg", "ui/btn_round_gold", new Vector2(122f, 34f), Vector2.zero, sliced: true);
                    MoriUiKit.Label(bub, "Txt", "铁锅山姆驻守", 15, MoriUiKit.InkBrown, new Vector2(118f, 30f), new Vector2(0f, 2f), stroked: false);
                }
            }
        }
    }
}
