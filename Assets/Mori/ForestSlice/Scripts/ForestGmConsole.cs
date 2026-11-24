using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mori.ForestSlice
{
    /// <summary>
    /// 《深山末日开荒》开发者 GM 调试台（适配微信小游戏竖屏安全区、模拟器与编辑器热键）：
    /// 1. 独立顶层 Canvas（常规模式 ScreenSpaceOverlay sortingOrder=650；离屏截图自动切至同相机最高层）
    /// 2. 静态模态锁 IsGmPanelOpen：拦截主世界点击、拖拽与 WASD 移动，杜绝误触穿透
    /// 3. 角色星级经验 / 直升跳级 (Lv.1~15) / 骨干英雄成长 / 一键秒完建造生产农牧 / 仓库快捷扩容
    /// 4. 全 35 种 SurvRes 材料 4 大分类页签 + [+5 / +20 / +100] 数量档位精准发放
    /// </summary>
    public sealed class ForestGmConsole : MonoBehaviour
    {
        public static bool IsGmPanelOpen { get; private set; }

        static readonly SurvRes[][] CategoryItems =
        {
            // Tab 0: 原材采集 (9)
            new[]
            {
                SurvRes.Wood, SurvRes.Scrap, SurvRes.Water,
                SurvRes.Veggie, SurvRes.Cloth, SurvRes.Herb,
                SurvRes.Fish, SurvRes.Meat, SurvRes.Pelt,
            },
            // Tab 1: 建材五金 (6)
            new[]
            {
                SurvRes.Plank, SurvRes.WoodBed, SurvRes.IronBeam,
                SurvRes.Nails, SurvRes.IronIngot, SurvRes.MetalPipe,
            },
            // Tab 2: 餐衣医药 (11)
            new[]
            {
                SurvRes.RoastVeggie, SurvRes.VeggieSoup, SurvRes.GrillFish,
                SurvRes.SmokedMeat, SurvRes.LinenShirt, SurvRes.LeatherBoots,
                SurvRes.FurCoat, SurvRes.PlateArmor, SurvRes.HerbPoultice,
                SurvRes.WarmTea, SurvRes.MedKit,
            },
            // Tab 3: 种子通货 (9)
            new[]
            {
                SurvRes.SeedVeggie, SurvRes.SeedFlax, SurvRes.SeedHerb,
                SurvRes.SeedGolden, SurvRes.Compost, SurvRes.Ration,
                SurvRes.Parts, SurvRes.Blueprint, SurvRes.HeroMedal,
            },
        };

        static readonly string[] CategoryTitles =
        {
            "原材采集(9)",
            "建材五金(6)",
            "餐衣医药(11)",
            "种子通货(9)",
        };

        static readonly int[] AmountOptions = { 5, 20, 100 };

        sealed class MaterialCellUi
        {
            public GameObject root;
            public Image bg;
            public Image icon;
            public Text nameTxt;
            public Text countTxt;
            public Text addBadgeTxt;
            public Button btn;
            public SurvRes boundRes;
        }

        ForestSurvivalSim _sim;
        ForestSurvivalWorld _world;
        ForestSurvivalHud _hud;
        Camera _hudCam;

        Canvas _canvas;
        RectTransform _canvasRt;
        GameObject _entryBtnGo;
        RectTransform _entryBtnRt;
        GameObject _modalRootGo;

        Text _statusTxt;
        Text _whQuickBtnLbl;
        Text _feedbackTxt;

        int _selectedAmount = 20;
        int _activeTab = 0;

        readonly Image[] _amtBtnImgs = new Image[3];
        readonly Text[] _amtBtnLbls = new Text[3];
        readonly Image[] _tabBtnImgs = new Image[4];
        readonly Text[] _tabBtnLbls = new Text[4];
        readonly MaterialCellUi[] _cells = new MaterialCellUi[12];

        bool _subscribedSimEvents;

        public int SelectedAmount => _selectedAmount;
        public int ActiveTab => _activeTab;

        public void Init(ForestSurvivalSim sim, ForestSurvivalWorld world, ForestSurvivalHud hud)
        {
            IsGmPanelOpen = false;
            _sim = sim;
            _world = world;
            _hud = hud;
            _hudCam = ResolveTargetCamera();

            ForestComicUiSkin.Ensure();
            BuildCanvasRoot();
            BuildFloatingEntryButton();
            BuildModalPanel();

            BindSimEvents();
            Canvas.willRenderCanvases -= OnWillRenderCanvases;
            Canvas.willRenderCanvases += OnWillRenderCanvases;

            SyncCanvasRenderMode();
            RefreshAll();
            SyncEntryVisibility();
        }

        void OnDisable()
        {
            if (IsGmPanelOpen)
                IsGmPanelOpen = false;
        }

        void OnDestroy()
        {
            IsGmPanelOpen = false;
            Canvas.willRenderCanvases -= OnWillRenderCanvases;
            UnbindSimEvents();
        }

        void BindSimEvents()
        {
            if (_sim == null || _subscribedSimEvents) return;
            _subscribedSimEvents = true;
            _sim.OnResourceGained += OnSimResChanged;
            _sim.OnXpBurst += OnSimXpChanged;
            _sim.OnCampLevelUp += OnSimLevelUp;
            _sim.OnHeroUnlocked += OnSimHeroChanged;
        }

        void UnbindSimEvents()
        {
            if (_sim == null || !_subscribedSimEvents) return;
            _subscribedSimEvents = false;
            _sim.OnResourceGained -= OnSimResChanged;
            _sim.OnXpBurst -= OnSimXpChanged;
            _sim.OnCampLevelUp -= OnSimLevelUp;
            _sim.OnHeroUnlocked -= OnSimHeroChanged;
        }

        void OnSimResChanged(SurvRes r, int delta)
        {
            if (IsGmPanelOpen) RefreshAll();
        }

        void OnSimXpChanged(int xp, Vector2 pos)
        {
            if (IsGmPanelOpen) RefreshStatusBar();
        }

        void OnSimLevelUp(int lv, string[] unlocks)
        {
            if (IsGmPanelOpen) RefreshStatusBar();
        }

        void OnSimHeroChanged(HeroSpec h)
        {
            if (IsGmPanelOpen) RefreshStatusBar();
        }

        void OnWillRenderCanvases()
        {
            if (this == null || _canvas == null)
            {
                Canvas.willRenderCanvases -= OnWillRenderCanvases;
                return;
            }
            SyncCanvasRenderMode();
            SyncEntryVisibility();
            if (IsGmPanelOpen)
                RefreshStatusBar();
        }

        void Update()
        {
            if (_canvas == null || _sim == null) return;

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.BackQuote))
            {
                TogglePanel();
            }
            else if (IsGmPanelOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                ClosePanel();
            }
#endif

            SyncCanvasRenderMode();
            SyncEntryVisibility();
            if (IsGmPanelOpen)
                RefreshStatusBar();
        }

        public void TogglePanel()
        {
            if (IsGmPanelOpen) ClosePanel();
            else OpenPanel();
        }

        public void OpenPanel()
        {
            if (_modalRootGo == null) return;
            IsGmPanelOpen = true;
            SyncCanvasRenderMode();
            _modalRootGo.SetActive(true);
            SyncEntryVisibility();
            RefreshAll();
        }

        public void ClosePanel()
        {
            IsGmPanelOpen = false;
            if (_modalRootGo != null)
                _modalRootGo.SetActive(false);
            SyncEntryVisibility();
        }

        Camera ResolveTargetCamera()
        {
            if (_hud != null)
            {
                Canvas hc = _hud.GetComponentInChildren<Canvas>(true);
                if (hc != null && hc.worldCamera != null)
                    return hc.worldCamera;
            }
            return Camera.main;
        }

        void SyncCanvasRenderMode()
        {
            if (_canvas == null) return;
            if (_hudCam == null) _hudCam = ResolveTargetCamera();

            // 离屏 RenderTexture 渲染截图时切换至同相机顶层排序，常规运行维持独立 ScreenSpaceOverlay(650)
            bool offscreenCapture = _hudCam != null && _hudCam.targetTexture != null;
            if (offscreenCapture)
            {
                if (_canvas.renderMode != RenderMode.ScreenSpaceCamera || _canvas.worldCamera != _hudCam)
                {
                    _canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    _canvas.worldCamera = _hudCam;
                    _canvas.planeDistance = 4.5f;
                }
                _canvas.sortingOrder = 25650;
            }
            else
            {
                if (_canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    _canvas.worldCamera = null;
                }
                _canvas.sortingOrder = 650;
            }
        }

        void SyncEntryVisibility()
        {
            if (_entryBtnGo == null) return;
            bool showEntry = !IsGmPanelOpen && (_hud == null || !_hud.IsModalOpen);
            if (_entryBtnGo.activeSelf != showEntry)
                _entryBtnGo.SetActive(showEntry);
        }

        void BuildCanvasRoot()
        {
            var cgo = new GameObject("ForestGmCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.transform.SetParent(transform, false);
            _canvas = cgo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 650;

            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 1280f);
            scaler.matchWidthOrHeight = 0.5f;

            _canvasRt = cgo.GetComponent<RectTransform>();
        }

        void BuildFloatingEntryButton()
        {
            float safeTopPx = Mathf.Max(0f, Screen.height - Screen.safeArea.yMax);
            float topPad = Mathf.Clamp(safeTopPx * (1280f / Mathf.Max(1f, Screen.height)), 0f, 96f);
#if !UNITY_EDITOR && (UNITY_WEBGL || PLATFORM_WEIXINMINIGAME)
            topPad = Mathf.Max(topPad, 56f);
#endif
            float rightOrderTop = Mathf.Min(-112f, -16f - topPad - 56f);
            Vector2 entryPos = new Vector2(-14f, rightOrderTop - 86f);
            Image pill = ForestUiWidgets.MakeSlicedImage(
                "BtnGmEntry", _canvasRt, ForestComicUiSkin.DarkPill, new Color(1f, 0.97f, 0.90f, 0.92f),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                entryPos, new Vector2(118f, 40f), true);

            _entryBtnGo = pill.gameObject;
            _entryBtnRt = pill.rectTransform;

            var btn = pill.gameObject.AddComponent<Button>();
            btn.targetGraphic = pill;
            btn.onClick.AddListener(OpenPanel);

            Sprite badgeSp = ForestComicUiSkin.GetArt("ico_hammer", ForestComicUiSkin.GetArt("ico_xp_star", ForestComicUiSkin.RedDotBadge));
            ForestUiWidgets.MakeIcon("Ico", pill.transform, badgeSp, new Vector2(-38f, 0f), new Vector2(24f, 24f));
            ForestUiWidgets.MakeText("Lbl", pill.transform, "GM调试", 16, ForestUiWidgets.WarmGold,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(12f, 0f), Vector2.zero, outline: true);
        }

        void BuildModalPanel()
        {
            RectTransform backdropRt = ForestUiWidgets.Stretch("GmModalRoot", _canvasRt);
            var backdropImg = backdropRt.gameObject.AddComponent<Image>();
            backdropImg.color = new Color(0.05f, 0.04f, 0.03f, 0.70f);
            backdropImg.raycastTarget = true;
            var backdropBtn = backdropRt.gameObject.AddComponent<Button>();
            backdropBtn.transition = Selectable.Transition.None;
            backdropBtn.onClick.AddListener(ClosePanel);
            _modalRootGo = backdropRt.gameObject;

            // 主卡片外框 (668 x 968)
            Image cardOuter = ForestUiWidgets.MakeSlicedImage(
                "GmCardOuter", backdropRt, ForestComicUiSkin.DialogOuter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(668f, 968f), true);

            ForestUiWidgets.MakeText(
                "HeaderTitle", cardOuter.transform, "★ GM 开发者调试台", 24, ForestUiWidgets.WarmGold,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-16f, -14f), new Vector2(520f, 38f), outline: true);

            ForestUiWidgets.MakeJellyButton(
                "BtnClose", cardOuter.transform, ForestComicUiSkin.BtnRed, "✕", 22,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -11f), new Vector2(50f, 44f),
                ClosePanel, out _);

            // 暖羊皮纸内衬 (640 x 886)
            Image inner = ForestUiWidgets.MakeSlicedImage(
                "ParchmentBody", cardOuter.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -66f), new Vector2(640f, 886f), false);
            Transform body = inner.transform;

            // --- 顶部实时状态读数栏 ---
            Image statusPill = ForestUiWidgets.MakeSlicedImage(
                "TopStatusPill", body, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -10f), new Vector2(616f, 44f), false);
            _statusTxt = ForestUiWidgets.MakeText(
                "StatusTxt", statusPill.transform, "", 15, ForestUiWidgets.CreamLight,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(-16f, 0f), outline: true);

            // --- Section A: 人物星级经验 & 英雄成长 & 快捷调试 ---
            ForestUiWidgets.MakeText(
                "SecATitle", body, "★ 领主经验 · 九宫 · 快捷调试", 17, ForestUiWidgets.InkDark,
                TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -60f), new Vector2(300f, 30f), outline: false);

            ForestUiWidgets.MakeJellyButton(
                "BtnClearAllForest", body, ForestComicUiSkin.BtnGreen, "一键拓荒全地图", 13,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-172f, -59f), new Vector2(150f, 32f),
                ClearFullMapTerritory, out _);

            ForestUiWidgets.MakeJellyButton(
                "BtnUpgradeWarehouse", body, ForestComicUiSkin.BtnBlue, "仓库扩容+1级", 14,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -59f), new Vector2(152f, 32f),
                OnQuickUpgradeWarehouse, out _whQuickBtnLbl);

            Image secABox = ForestUiWidgets.MakeSlicedImage(
                "SecABox", body, ForestComicUiSkin.SubSlot, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -94f), new Vector2(616f, 260f), false);
            Transform secA = secABox.transform;

            // Row 1 (3 列: 193 x 44, 间距 8.5)
            MakeRow3Button(secA, 0, -8f, ForestComicUiSkin.BtnOrange, "主角经验 +50", 15, () => AddPlayerXp(50));
            MakeRow3Button(secA, 1, -8f, ForestComicUiSkin.BtnOrange, "主角经验 +250", 15, () => AddPlayerXp(250));
            MakeRow3Button(secA, 2, -8f, ForestComicUiSkin.BtnGreen, "主角直升 +1级", 15, () => JumpToPlayerLevel(_sim != null ? _sim.CampLevel + 1 : 2));

            // Row 2 (3 列: 193 x 44)
            MakeRow3Button(secA, 0, -58f, ForestComicUiSkin.BtnBlue, "跳至 Lv.5", 15, () => JumpToPlayerLevel(5));
            MakeRow3Button(secA, 1, -58f, ForestComicUiSkin.BtnBlue, "跳至 Lv.10", 15, () => JumpToPlayerLevel(10));
            MakeRow3Button(secA, 2, -58f, ForestComicUiSkin.BtnBlue, "跳至 Lv.15", 15, () => JumpToPlayerLevel(ForestSurvivalSim.MaxCampLevel));

            // Row 3 (4 列: 143 x 44, 间距 8)
            MakeRow4Button(secA, 0, -108f, ForestComicUiSkin.BtnBlue, "全队英雄 +1级", 14, () => AddAllHeroLevels(1));
            MakeRow4Button(secA, 1, -108f, ForestComicUiSkin.BtnBlue, "全队英雄 +5级", 14, () => AddAllHeroLevels(5));
            MakeRow4Button(secA, 2, -108f, ForestComicUiSkin.BtnOrange, "英雄勋章 +10", 14, () => GrantMaterial(SurvRes.HeroMedal, 10));
            MakeRow4Button(secA, 3, -108f, ForestComicUiSkin.BtnGreen, "解锁全英雄", 14, UnlockAllHeroes);

            // Row 4 (铜币 +500 / 铜币 +5000 / 秒完建造生产农牧 / 全资源补满+20)
            MakeCustomButton(secA, "BtnCoin500", ForestComicUiSkin.BtnOrange, "铜币 +500", 14,
                10f, -158f, 116f, 44f, () => AddCoins(500));
            MakeCustomButton(secA, "BtnCoin5000", ForestComicUiSkin.BtnOrange, "铜币 +5000", 14,
                134f, -158f, 124f, 44f, () => AddCoins(5000));
            MakeCustomButton(secA, "BtnInstantAll", ForestComicUiSkin.BtnGreen, "一键秒完建造/生产/农牧", 13,
                266f, -158f, 186f, 44f, InstantCompleteAll);
            MakeCustomButton(secA, "BtnFillAll20", ForestComicUiSkin.BtnBlue, "全资源补满+20", 14,
                460f, -158f, 146f, 44f, FillAllResources20);

            // Row 5 (4 列: 九宫格阵型与 40 关闯关快捷调试)
            MakeRow4Button(secA, 0, -208f, ForestComicUiSkin.BtnGreen, "全英雄+全阵型", 13, UnlockAllHeroesAndFormations);
            MakeRow4Button(secA, 1, -208f, ForestComicUiSkin.BtnBlue, "关卡:第1关", 13, () => SetBattleStage(1));
            MakeRow4Button(secA, 2, -208f, ForestComicUiSkin.BtnOrange, "关卡:第5关Boss", 13, () => SetBattleStage(5));
            MakeRow4Button(secA, 3, -208f, ForestComicUiSkin.BtnBlue, "关卡:第12关", 13, () => SetBattleStage(12));

            // --- Section B: 指定材料精准发放 ---
            ForestUiWidgets.MakeText(
                "SecBTitle", body, "★ 指定材料精准发放 (点击即领)", 17, ForestUiWidgets.InkDark,
                TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -364f), new Vector2(300f, 34f), outline: false);

            ForestUiWidgets.MakeText(
                "AmtHint", body, "单次数量:", 15, ForestUiWidgets.InkMuted,
                TextAnchor.MiddleRight, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-266f, -364f), new Vector2(86f, 34f), outline: false);

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                int amt = AmountOptions[i];
                float rightX = -182f + i * 85f;
                Button b = ForestUiWidgets.MakeJellyButton(
                    "BtnAmt_" + amt, body, ForestComicUiSkin.BtnGrey, "+" + amt, 16,
                    new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(rightX, -363f), new Vector2(78f, 36f),
                    () => SetAmountMultiplier(amt), out Text lbl);
                _amtBtnImgs[idx] = b.GetComponent<Image>();
                _amtBtnLbls[idx] = lbl;
            }

            // 4 大分类页签栏 (4 x 148 + 3 x 8 = 616)
            for (int t = 0; t < 4; t++)
            {
                int tabIdx = t;
                float leftX = 12f + t * 156f;
                Button tb = ForestUiWidgets.MakeJellyButton(
                    "BtnTab_" + t, body, ForestComicUiSkin.SubSlot, CategoryTitles[t], 15,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(leftX, -408f), new Vector2(148f, 42f),
                    () => SelectCategoryTab(tabIdx), out Text tLbl);
                _tabBtnImgs[t] = tb.GetComponent<Image>();
                _tabBtnLbls[t] = tLbl;
            }

            // 3 列 x 4 行 = 12 格材料网格 (200 x 87, 水平间距 8, 垂直间距 8)
            for (int i = 0; i < 12; i++)
            {
                int cellIdx = i;
                int col = i % 3;
                int row = i / 3;
                float x = 12f + col * 208f;
                float y = -460f - row * 95f;

                Image cellBg = ForestUiWidgets.MakeSlicedImage(
                    "MatCell_" + i, body, ForestComicUiSkin.SubSlot, Color.white,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(x, y), new Vector2(200f, 87f), true);

                var cellBtn = cellBg.gameObject.AddComponent<Button>();
                cellBtn.targetGraphic = cellBg;

                Image ico = ForestUiWidgets.MakeIcon(
                    "Ico", cellBg.transform, ForestComicUiSkin.ResIcon(SurvRes.Wood),
                    new Vector2(32f, 0f), new Vector2(50f, 50f), new Vector2(0f, 0.5f), false);

                Text nameTxt = ForestUiWidgets.MakeText(
                    "Name", cellBg.transform, "原木", 16, ForestUiWidgets.InkDark,
                    TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(62f, -8f), new Vector2(132f, 26f), outline: false);

                Text cntTxt = ForestUiWidgets.MakeText(
                    "Cnt", cellBg.transform, "拥有: 0", 14, ForestUiWidgets.InkMuted,
                    TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(62f, -34f), new Vector2(132f, 22f), outline: false);

                Image badgeBg = ForestUiWidgets.MakeSlicedImage(
                    "AddBadge", cellBg.transform, ForestComicUiSkin.BtnGreen, Color.white,
                    new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                    new Vector2(-8f, 6f), new Vector2(72f, 24f), false);
                Text badgeTxt = ForestUiWidgets.MakeText(
                    "BadgeTxt", badgeBg.transform, "+20", 14, Color.white,
                    TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 1f), Vector2.zero, outline: true);

                var ui = new MaterialCellUi
                {
                    root = cellBg.gameObject,
                    bg = cellBg,
                    icon = ico,
                    nameTxt = nameTxt,
                    countTxt = cntTxt,
                    addBadgeTxt = badgeTxt,
                    btn = cellBtn,
                };
                cellBtn.onClick.AddListener(() => GrantMaterial(_cells[cellIdx].boundRes));
                _cells[i] = ui;
            }

            // 底部实时操作反馈胶囊
            Image footPill = ForestUiWidgets.MakeSlicedImage(
                "FeedbackPill", body, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -842f), new Vector2(616f, 34f), false);
            _feedbackTxt = ForestUiWidgets.MakeText(
                "FeedbackTxt", footPill.transform,
                "提示: 点击任意材料格立即入库，编辑器支持 F1 或 ~ 键快捷开关",
                14, ForestUiWidgets.WarmGold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16f, 0f), outline: true);

            _modalRootGo.SetActive(false);
        }

        static void MakeRow3Button(Transform parent, int col, float y, Sprite skin, string label, int fontSize, UnityEngine.Events.UnityAction onClick)
        {
            float x = 10f + col * 201.5f;
            MakeCustomButton(parent, "Btn3_" + col + "_" + Mathf.Abs(Mathf.RoundToInt(y)), skin, label, fontSize, x, y, 193f, 44f, onClick);
        }

        static void MakeRow4Button(Transform parent, int col, float y, Sprite skin, string label, int fontSize, UnityEngine.Events.UnityAction onClick)
        {
            float x = 10f + col * 151f;
            MakeCustomButton(parent, "Btn4_" + col + "_" + Mathf.Abs(Mathf.RoundToInt(y)), skin, label, fontSize, x, y, 143f, 44f, onClick);
        }

        static void MakeCustomButton(Transform parent, string name, Sprite skin, string label, int fontSize,
            float leftX, float topY, float w, float h, UnityEngine.Events.UnityAction onClick)
        {
            ForestUiWidgets.MakeJellyButton(
                name, parent, skin, label, fontSize,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(leftX, topY), new Vector2(w, h),
                onClick, out _);
        }

        // =====================================================================
        // GM 操作逻辑与全界面同步刷新
        // =====================================================================
        public void SetAmountMultiplier(int amount)
        {
            if (amount <= 0) return;
            _selectedAmount = amount;
            RefreshAmountButtons();
            RefreshMaterialGrid();
            SetFeedback("已切换单次发放数量档位: +" + _selectedAmount);
        }

        public void SelectCategoryTab(int tabIndex)
        {
            _activeTab = Mathf.Clamp(tabIndex, 0, CategoryItems.Length - 1);
            RefreshCategoryTabs();
            RefreshMaterialGrid();
        }

        public void GrantMaterial(SurvRes res)
        {
            GrantMaterial(res, _selectedAmount);
        }

        public void GrantMaterial(SurvRes res, int amount)
        {
            if (_sim == null || amount <= 0) return;
            int before = _sim.Get(res);
            _sim.Add(res, amount, notifyToast: true);
            int after = _sim.Get(res);
            int gained = after - before;

            string rName = ForestSurvivalSim.ResName(res);
            if (gained < amount && ForestSurvivalSim.IsWarehouseCapped(res))
            {
                SetFeedback($"[{rName}] 已达仓库上限({_sim.StorageCap})! 可点右上[仓库扩容+1级]提升储量");
            }
            else
            {
                SetFeedback($"已发放: {rName} +{gained} (当前拥有: {after})");
            }
            RefreshAll();
        }

        public void AddPlayerXp(int xp)
        {
            if (_sim == null || xp <= 0) return;
            _sim.GmAddPlayerXp(xp);
            SyncWorldRevealedNodes();
            SetFeedback($"已增加主角星级经验 +{xp} (当前 ★Lv.{_sim.CampLevel})");
            RefreshAll();
        }

        public void JumpToPlayerLevel(int targetLv)
        {
            if (_sim == null) return;
            int clamped = Mathf.Clamp(targetLv, 1, ForestSurvivalSim.MaxCampLevel);
            ForestMap map = (_world != null && _world.View != null) ? _world.View.Map : null;
            _sim.GmSetPlayerLevel(clamped, map);
            RefreshAllWorldNodes();
            SetFeedback($"已将主角星级设为 ★Lv.{_sim.CampLevel}，对应新建筑地基已同步刷新");
            RefreshAll();
        }

        void AddAllHeroLevels(int addLv)
        {
            if (_sim == null || addLv <= 0) return;
            _sim.GmAddHeroLevels(null, addLv);
            SetFeedback($"全部已解锁骨干英雄等级 +{addLv}");
            RefreshAll();
        }

        void UnlockAllHeroes()
        {
            if (_sim == null) return;
            _sim.GmUnlockAllHeroes();
            SetFeedback("已解锁全部 6 名骨干英雄并同步驻营小队");
            RefreshAll();
        }

        public void UnlockAllHeroesAndFormations()
        {
            if (_sim == null) return;
            _sim.GmUnlockAllHeroesAndFormations();
            SetFeedback("已解锁全部 6 名骨干英雄、4 大战术阵型与 5 人满编推荐布阵!");
            RefreshAll();
        }

        public void SetBattleStage(int stage1Based)
        {
            if (_sim == null) return;
            _sim.GmSetBattleStage(stage1Based);
            BattleStageSpec sp = _sim.GetBattleStageSpec(_sim.CurrentBattleStage);
            SetFeedback($"已切换挑战关卡至: {sp.title} (推荐战力 {sp.recommendedPower})");
            RefreshAll();
        }

        void AddCoins(int delta)
        {
            if (_sim == null || delta <= 0) return;
            _sim.Coins += delta;
            SetFeedback($"已发放营地铜币 +{delta} (当前铜币: {_sim.Coins})");
            RefreshAll();
        }

        void InstantCompleteAll()
        {
            if (_sim == null) return;
            _sim.GmInstantCompleteAll();
            RefreshAllWorldNodes();
            SetFeedback("已秒完所有建造/升级工程、作坊生产队列与农牧成熟状态!");
            RefreshAll();
        }

        void FillAllResources20()
        {
            if (_sim == null) return;
            foreach (SurvRes r in Enum.GetValues(typeof(SurvRes)))
                _sim.Add(r, 20, notifyToast: false);
            SetFeedback($"全 35 种材料均已补充 +20 (基础仓上限: {_sim.StorageCap})");
            RefreshAll();
        }

        void OnQuickUpgradeWarehouse()
        {
            if (_sim == null) return;
            CampNode wh = _sim.Find("warehouse");
            if (wh == null) return;

            if (wh.state != NodeState.Active)
            {
                wh.state = NodeState.Active;
                wh.isUpgradingExisting = false;
                wh.level = Mathf.Max(2, wh.level);
            }
            else if (wh.level < Mathf.Max(wh.maxLevel, 5))
            {
                wh.level++;
            }
            else
            {
                SetFeedback($"物资仓库已达最高级 (储量上限: {_sim.StorageCap})");
                RefreshAll();
                return;
            }

            if (_world != null)
                _world.RefreshNodeVisual(wh);
            SetFeedback($"物资仓库已升至 Lv.{wh.level}，单品储量上限扩至 {_sim.StorageCap}!");
            RefreshAll();
        }

        public void ClearFullMapTerritory()
        {
            if (_sim == null || _world == null || _world.View == null || _world.View.Map == null) return;
            ForestSliceView View = _world.View;
            ForestSurvivalSim Sim = _sim;
            View.Map.ClearRectInstant(3, 3, 28, 28);
            View.Map.RebuildMask();
            View.RefreshEdges(instant: true);
            View.DecorateCleared();
            Sim.SyncRevealedNodes(View.Map, silent: true);
            RefreshAllWorldNodes();
            SetFeedback("已一键拓荒全地图(3..28)，全部林间建筑地基已显露!");
            RefreshAll();
        }

        void SyncWorldRevealedNodes()
        {
            if (_sim == null || _world == null || _world.View == null || _world.View.Map == null) return;
            _sim.SyncRevealedNodes(_world.View.Map, silent: true);
            RefreshAllWorldNodes();
        }

        void RefreshAllWorldNodes()
        {
            if (_sim == null || _world == null) return;
            for (int i = 0; i < _sim.Nodes.Count; i++)
                _world.RefreshNodeVisual(_sim.Nodes[i]);
            _world.SyncCivilianVillagers();
        }

        void SetFeedback(string msg)
        {
            if (_feedbackTxt != null && !string.IsNullOrEmpty(msg))
                _feedbackTxt.text = msg;
        }

        public void RefreshAll()
        {
            if (_sim == null) return;
            RefreshStatusBar();
            RefreshAmountButtons();
            RefreshCategoryTabs();
            RefreshMaterialGrid();
        }

        void RefreshStatusBar()
        {
            if (_sim == null || _statusTxt == null) return;
            bool isMax = _sim.CampLevel >= ForestSurvivalSim.MaxCampLevel;
            string xpPart = isMax
                ? "已满级"
                : $"XP {_sim.CampXp}/{ForestSurvivalSim.GetXpNeed(_sim.CampLevel)}";

            _statusTxt.text =
                $"★Lv.{_sim.CampLevel} ({xpPart}) · 铜币:{_sim.Coins} · 人口:{_sim.CurrentPop}/{_sim.PopCap} · 关卡:{_sim.CurrentBattleStage}/{ForestSurvivalSim.MaxBattleStages}";

            if (_whQuickBtnLbl != null)
            {
                CampNode wh = _sim.Find("warehouse");
                bool maxed = wh != null && wh.state == NodeState.Active && wh.level >= Mathf.Max(wh.maxLevel, 5);
                _whQuickBtnLbl.text = maxed ? $"仓上限:{_sim.StorageCap}" : $"仓库扩容({ _sim.StorageCap })";
            }
        }

        void RefreshAmountButtons()
        {
            for (int i = 0; i < AmountOptions.Length; i++)
            {
                bool sel = _selectedAmount == AmountOptions[i];
                if (_amtBtnImgs[i] != null)
                    _amtBtnImgs[i].sprite = sel ? ForestComicUiSkin.BtnGreen : ForestComicUiSkin.BtnGrey;
                if (_amtBtnLbls[i] != null)
                {
                    _amtBtnLbls[i].text = (sel ? "★+" : "+") + AmountOptions[i];
                    _amtBtnLbls[i].color = sel ? Color.white : new Color32(232, 226, 216, 255);
                }
            }
        }

        void RefreshCategoryTabs()
        {
            for (int i = 0; i < 4; i++)
            {
                bool sel = i == _activeTab;
                if (_tabBtnImgs[i] != null)
                    _tabBtnImgs[i].sprite = sel ? ForestComicUiSkin.BtnOrange : ForestComicUiSkin.SubSlot;
                if (_tabBtnLbls[i] != null)
                {
                    _tabBtnLbls[i].color = sel ? Color.white : ForestUiWidgets.InkDark;
                    var ol = _tabBtnLbls[i].GetComponent<Outline>();
                    if (ol != null) ol.enabled = sel;
                }
            }
        }

        void RefreshMaterialGrid()
        {
            if (_sim == null) return;
            SurvRes[] items = CategoryItems[Mathf.Clamp(_activeTab, 0, CategoryItems.Length - 1)];
            int cap = _sim.StorageCap;

            for (int i = 0; i < _cells.Length; i++)
            {
                MaterialCellUi c = _cells[i];
                if (c == null || c.root == null) continue;

                if (i >= items.Length)
                {
                    c.root.SetActive(false);
                    continue;
                }

                SurvRes r = items[i];
                c.boundRes = r;
                c.root.SetActive(true);
                c.icon.sprite = ForestComicUiSkin.ResIcon(r);
                c.nameTxt.text = ForestSurvivalSim.ResName(r);

                int own = _sim.Get(r);
                bool cappedType = ForestSurvivalSim.IsWarehouseCapped(r);
                bool atCap = cappedType && own >= cap;

                c.countTxt.text = cappedType ? $"拥有: {own}/{cap}" : $"拥有: {own}";
                c.countTxt.color = atCap
                    ? ForestUiWidgets.AlertRed
                    : (own > 0 ? new Color32(36, 128, 46, 255) : ForestUiWidgets.InkMuted);
                c.bg.sprite = own > 0 ? ForestComicUiSkin.ParchmentPanel : ForestComicUiSkin.SubSlot;
                c.addBadgeTxt.text = "+" + _selectedAmount;
            }
        }
    }
}
