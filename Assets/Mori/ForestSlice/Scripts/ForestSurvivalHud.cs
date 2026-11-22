using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mori.ForestSlice
{
    /// <summary>
    /// 《深山末日开荒》V2 商业级漫画风 UI 与全链路闭环控制器：
    /// 1. 左上星级/双轨人口(当前/供暖上限)/铜币/工程手稿/仓库上限 HUD + 右侧常驻【居民订单】入口 + 底部 6 宫格 Dock
    /// 2. 破框立体建造/升级卡：完整支持人口/供暖上限/铜币/手稿/加工件校验，缺料显示红叉 ✖ 与【去生产】【获取】【前往】一键溯源跳转！
    /// 3. 物品详情小弹窗 & 【获取途径】路由弹窗（精准还原铜币/工程手稿多途径跳转）
    /// 4. 五大生产作坊通用队列抽屉（锯木木艺坊/废铁锻造坊/裁缝防具坊/野味炊事房/山林草药坊，支持 3 原料与跨厂跳转）
    /// 5. 三槽并行【四维居民生计订单板】（餐饮/服装/医药/安居 + 5单宝箱 + 冷却刷新）+ 【山口行商驿站】大宗贸易面板
    /// </summary>
    public sealed class ForestSurvivalHud : MonoBehaviour
    {
        sealed class PickupToastItem
        {
            public RectTransform rt;
            public Image icon;
            public Text text;
            public CanvasGroup cg;
            public SurvRes res;
            public float age;
            public bool spawnedFlyer;
        }

        sealed class FlyIconItem
        {
            public RectTransform rt;
            public Image img;
            public Vector2 from, to;
            public float t, dur;
            public SurvRes targetRes;
        }

        sealed class WorldTextToast
        {
            public RectTransform rt;
            public Text txt;
            public Vector2 startPos;
            public float age;
        }

        sealed class TrackBubble
        {
            public RectTransform rt;
            public Image bg;
            public Image icon;
            public Text label;
            public Button btn;
            public Action onClick;
        }

        Camera _cam;
        ForestSurvivalSim _sim;
        ForestSurvivalWorld _world;
        ForestCityScavengeArena _cityArena;

        Canvas _canvas;
        RectTransform _canvasRt;

        GameObject _worldTrackLayer;
        GameObject _mainHudLayer;

        // 顶栏与左侧精简资源栈
        Text _lvStarTxt, _xpBarTxt, _popTxt, _coinTxt, _blueprintTxt, _storageCapTxt, _orderBadgeCntTxt;
        Image _xpFillImg;
        GameObject _orderRightDot;
        readonly Dictionary<SurvRes, RectTransform> _resPillRts = new Dictionary<SurvRes, RectTransform>();
        readonly Dictionary<SurvRes, Text> _resPillTxts = new Dictionary<SurvRes, Text>();
        readonly Dictionary<SurvRes, float> _resPulseTimers = new Dictionary<SurvRes, float>();

        // 左侧尸潮挂件、左下任务浮窗、底部 6 宫格 Dock
        GameObject _hordeBadgeGo;
        Text _hordeBadgeTimerTxt;
        Image _questCardBg, _questCheckImg, _questFillImg;
        Text _questTitleTxt;
        readonly Image[] _dockLocks = new Image[6];
        readonly GameObject[] _dockRedDots = new GameObject[6];

        // 建筑脚下轻气泡圆钮菜单
        CampNode _footMenuNode;
        RectTransform _footMenuRt;
        Text _footTitleTxt, _footRateTxt;
        Button _footBtnPrimary, _footBtnSecondary;
        Image _footIconPrimary, _footIconSecondary;
        Text _footLblPrimary, _footLblSecondary;

        // 破框立体建造/升级确认卡（支持 5 行条件 + 缺料一键跳转 + 底部星级上限红字）
        GameObject _buildCardModal;
        CampNode _pendingBuildNode;
        Image _bcIllustImg, _bcBenefitIcon;
        Text _bcTitleTxt, _bcDescTxt, _bcBenefitNameTxt, _bcBenefitFromTxt, _bcBenefitToTxt, _bcTimeTxt, _bcLvGateRedTxt, _bcBtnLbl;
        Button _bcConfirmBtn;
        readonly GameObject[] _bcCostRows = new GameObject[5];
        readonly Image[] _bcCostIcons = new Image[5];
        readonly Button[] _bcCostIconBtns = new Button[5];
        readonly Text[] _bcCostTxts = new Text[5];
        readonly Image[] _bcCostChecks = new Image[5];
        readonly Button[] _bcJumpBtns = new Button[5];
        readonly Text[] _bcJumpLbls = new Text[5];

        // 物品来源小浮窗 & 【获取途径】路由弹窗（叠在建造卡上方或独立打开！）
        GameObject _itemTipPopup;
        Image _itIcon;
        Text _itNameTxt, _itOwnTxt, _itSourceTxt;
        Action _itGoAction;

        GameObject _sourceRouteModal;
        Image _srIcon;
        Text _srNameTxt, _srOwnTxt;
        readonly GameObject[] _srRows = new GameObject[3];
        readonly Image[] _srRowIcons = new Image[3];
        readonly Text[] _srRowTitles = new Text[3];
        readonly Text[] _srRowLockTxts = new Text[3];
        readonly Button[] _srRowBtns = new Button[3];
        readonly Action[] _srRowActions = new Action[3];

        // 半屏五大作坊通用 5 格队列抽屉
        GameObject _workshopDrawer;
        CampNode _activeWorkshop;
        int _selectedRecipeIdx;
        Text _wsTitleTxt, _wsSelNameTxt, _wsSelStockTxt, _wsTimeTxt, _wsBtnLbl;
        Image _wsSelBigIcon, _wsReqIcon1, _wsReqIcon2, _wsReqIcon3;
        Text _wsReqTxt1, _wsReqTxt2, _wsReqTxt3;
        Button _wsReqBtn1, _wsReqBtn2, _wsReqBtn3;
        Button _wsProduceBtn, _wsUpgradeMiniBtn;
        readonly Image[] _wsQueueSlotBgs = new Image[5];
        readonly Image[] _wsQueueIcons = new Image[5];
        readonly Text[] _wsQueueTimers = new Text[5];
        GameObject _wsSpeedUpPill;
        readonly GameObject[] _wsRecipeSlots = new GameObject[4];
        readonly Image[] _wsRecipeFrames = new Image[4];
        readonly Image[] _wsRecipeIcons = new Image[4];
        readonly Image[] _wsRecipeLocks = new Image[4];
        readonly Text[] _wsRecipeSubTxts = new Text[4];

        // 三槽并行【四维居民生计订单板】抽屉
        GameObject _residentDrawer;
        Text _resMoodTxt, _resRefreshBtnLbl;
        Image _resMoodFill;
        readonly Image[] _ordBusts = new Image[3];
        readonly Text[] _ordTitles = new Text[3];
        readonly Text[] _ordComplaints = new Text[3];
        readonly Text[] _ordRewards = new Text[3];
        readonly Image[] _ordReqIco1 = new Image[3];
        readonly Text[] _ordReqTxt1 = new Text[3];
        readonly Button[] _ordReqBtn1 = new Button[3];
        readonly GameObject[] _ordReqBox2 = new GameObject[3];
        readonly Image[] _ordReqIco2 = new Image[3];
        readonly Text[] _ordReqTxt2 = new Text[3];
        readonly Button[] _ordReqBtn2 = new Button[3];
        readonly Button[] _ordDeliverBtns = new Button[3];
        readonly Text[] _ordDeliverLbls = new Text[3];

        // 【短波电台 · 山口行商驿站】大宗贸易弹窗
        GameObject _caravanModal;
        Text _cvSubTxt;
        readonly Image[] _cvIcons = new Image[3];
        readonly Text[] _cvTitles = new Text[3];
        readonly Text[] _cvNeedTxts = new Text[3];
        readonly Text[] _cvRewardTxts = new Text[3];
        readonly Button[] _cvTradeBtns = new Button[3];
        readonly Text[] _cvTradeLbls = new Text[3];

        // 全屏《尸潮来袭》漫画海报
        GameObject _crisisModal;
        Text _crisisTimerBigTxt, _crisisTask1Txt, _crisisTask2Txt;

        // 等级提升解锁弹窗
        GameObject _levelUpModal;
        Text _lvUpNumTxt;
        readonly GameObject[] _lvUpUnlockCards = new GameObject[3];
        readonly Image[] _lvUpUnlockIcons = new Image[3];
        readonly Text[] _lvUpUnlockNames = new Text[3];

        // 新英雄高光与幸存者驻守委任面板
        GameObject _heroSpotlightModal;
        Image _heroSpotSunburst, _heroSpotBust;
        Text _heroSpotNameTxt, _heroSpotRoleTxt, _heroSpotBuffTxt;

        GameObject _heroAssignModal;
        readonly GameObject[] _haRows = new GameObject[5];
        readonly Image[] _haBusts = new Image[5];
        readonly Text[] _haNames = new Text[5];
        readonly Text[] _haDescs = new Text[5];
        readonly Text[] _haStates = new Text[5];
        readonly Button[,] _haSkinBtns = new Button[5, 3];
        readonly Text[,] _haSkinLbls = new ButtonTextPair().Txts; // unused helper avoided below
        Text[,] _haSkinTexts;

        // 快捷建造/分类仓库总览弹窗
        GameObject _quickListModal;
        Text _qlTitleTxt, _qlSubCapTxt;
        Button _qlUpgradeWhBtn;
        RectTransform _qlContentRt;
        readonly List<GameObject> _qlSpawnedRows = new List<GameObject>();

        // 丧尸城街区选择卡 + 虚拟摇杆开车 HUD + 【恭喜获得】放射结算
        GameObject _citySelectModal;
        readonly Button[] _csSiteBtns = new Button[4];
        readonly Text[] _csSiteBtnLbls = new Text[4];

        GameObject _cityDriveHud;
        Image _cdCargoFill;
        Text _cdCargoTxt, _cdLootSummaryTxt, _cdAutoBtnLbl;
        RectTransform _joyBaseRt, _joyKnobRt;
        bool _joyDragging;
        Vector2 _joyOriginCanvas;

        GameObject _lootSettleModal;
        Image _lsSunburst;
        readonly GameObject[] _lsCards = new GameObject[5];
        readonly Image[] _lsCardIcons = new Image[5];
        readonly Text[] _lsCardTxts = new Text[5];

        // 南岸溪流推镜钓鱼 HUD
        GameObject _fishingHud;
        bool _inFishingMode;
        int _fishPhase;
        float _fishWaitTimer;
        float _fishTension;
        bool _fishAuto;
        Vector3 _savedPreFishCamPos;
        float _savedPreFishOrtho;
        Text _fishTaskTopTxt, _fishBtnMainLbl, _fishFastTapTag, _fishAutoLbl;
        Image _fishBtnIcon, _fishArcFill;
        GameObject _fishArcRoot, _fishFastTapPill;

        // 双人半身漫画对话遮罩
        GameObject _comicDialogOverlay;
        Image _cdLeftBust, _cdRightBust;
        Text _cdSpeakerNameTxt, _cdLineBodyTxt;
        readonly Queue<ComicLine> _dialogQueue = new Queue<ComicLine>();

        // 追踪池与动效池
        readonly List<TrackBubble> _checkMarks = new List<TrackBubble>();
        readonly List<TrackBubble> _worldBubbles = new List<TrackBubble>();
        readonly List<PickupToastItem> _pickupToasts = new List<PickupToastItem>();
        readonly List<FlyIconItem> _flyIcons = new List<FlyIconItem>();
        readonly List<WorldTextToast> _worldToasts = new List<WorldTextToast>();

        ComicGlovePointer _worldGlove;
        ComicGlovePointer _uiGlove;

        bool _panning;
        Vector3 _panTarget;
        float _panTargetOrtho;
        bool _panChangeOrtho;

        sealed class ButtonTextPair { public readonly Text[,] Txts = new Text[5, 3]; }

        static readonly SurvRes[] HudPinnedRes =
        {
            SurvRes.Wood, SurvRes.Ration, SurvRes.Plank, SurvRes.Scrap, SurvRes.Nails, SurvRes.Cloth
        };

        public ForestCityScavengeArena CityArena => _cityArena;

        public bool IsModalOpen =>
            (_buildCardModal != null && _buildCardModal.activeSelf) ||
            (_itemTipPopup != null && _itemTipPopup.activeSelf) ||
            (_sourceRouteModal != null && _sourceRouteModal.activeSelf) ||
            (_workshopDrawer != null && _workshopDrawer.activeSelf) ||
            (_residentDrawer != null && _residentDrawer.activeSelf) ||
            (_caravanModal != null && _caravanModal.activeSelf) ||
            (_crisisModal != null && _crisisModal.activeSelf) ||
            (_levelUpModal != null && _levelUpModal.activeSelf) ||
            (_heroSpotlightModal != null && _heroSpotlightModal.activeSelf) ||
            (_heroAssignModal != null && _heroAssignModal.activeSelf) ||
            (_quickListModal != null && _quickListModal.activeSelf) ||
            (_citySelectModal != null && _citySelectModal.activeSelf) ||
            (_lootSettleModal != null && _lootSettleModal.activeSelf) ||
            (_comicDialogOverlay != null && _comicDialogOverlay.activeSelf) ||
            _inFishingMode ||
            (_cityArena != null && _cityArena.IsRunning);

        public void Init(Camera cam, ForestSurvivalSim sim, ForestSurvivalWorld world)
        {
            _cam = cam;
            _sim = sim;
            _world = world;
            _haSkinTexts = new Text[5, 3];
            ForestComicUiSkin.Ensure();

            EnsureEventSystem();
            BuildCanvasRoot();

            _cityArena = new ForestCityScavengeArena(_cam, _sim, _world);
            _cityArena.OnStreetPickup += EnqueueCenterPickupToast;
            _cityArena.OnCargoFullSettlement += ShowCityLootSettlement;

            BuildWorldTrackingLayer();
            BuildMainCampHud();
            BuildFootRingMenu();
            BuildBreakOutConfirmModal();
            BuildWorkshopQueueDrawer();
            BuildResidentDemandDrawer();
            BuildCaravanModal();
            BuildCrisisHordeModal();
            BuildLevelUpModal();
            BuildHeroModals();
            BuildQuickListModal();
            BuildCitySelectAndDriveHud();
            BuildFishingPushInHud();
            BuildComicDialogOverlay();
            BuildItemAndSourcePopups(); // 置于顶层，可直接叠在建造确认卡之上！

            _worldGlove = new ComicGlovePointer(_worldTrackLayer.transform, "WorldGlove");
            _uiGlove = new ComicGlovePointer(_canvasRt, "UiGlove");

            _sim.OnResourceGained += EnqueueCenterPickupToast;
            _sim.OnXpBurst += SpawnXpFlyStar;
            _sim.OnWorldToast += SpawnWorldRisingText;
            _sim.OnCampLevelUp += ShowLevelUpModal;
            _sim.OnHeroUnlocked += ShowHeroSpotlight;
            _sim.OnHordeCrisisTriggered += () => { if (!_sim.HordePosterShown) OpenCrisisPoster(); };
            _sim.OnPlayComicDialog += EnqueueComicDialog;

            _world.OnNodeClicked += HandleNodeClicked;
            _world.OnResidentDemandClicked += OpenResidentDemandDrawer;
            _world.OnEmptyGroundClicked += () =>
            {
                _footMenuNode = null;
                if (_footMenuRt != null) _footMenuRt.gameObject.SetActive(false);
            };
            _world.OnCustomToast += SpawnWorldRisingText;
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            if (Application.isPlaying) DontDestroyOnLoad(es);
        }

        static void SafeDestroy(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }

        public bool IsPointerOverUi()
        {
            if (EventSystem.current == null) return false;
            return EventSystem.current.IsPointerOverGameObject();
        }

        public void CancelPan() => _panning = false;

        public void SmoothFocusGrid(Vector2 gridPos, float targetOrtho = -1f)
        {
            Vector3 iso = ForestSliceView.Iso(gridPos.x, gridPos.y);
            _panTarget = new Vector3(iso.x, iso.y, -20f);
            _panChangeOrtho = targetOrtho > 0f;
            _panTargetOrtho = targetOrtho;
            _panning = true;
        }

        void BuildCanvasRoot()
        {
            var cgo = new GameObject("SurvivalComicCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.transform.SetParent(transform, false);
            _canvas = cgo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = _cam;
            _canvas.planeDistance = 5f;
            _canvas.sortingOrder = 25000;

            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 1280f);
            scaler.matchWidthOrHeight = 0.5f;

            _canvasRt = cgo.GetComponent<RectTransform>();
        }

        Vector2 WorldToCanvas(Vector3 worldPos)
        {
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(_cam, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, sp, _cam, out Vector2 lp);
            return lp;
        }

        // =====================================================================
        // 1. 世界追踪层
        // =====================================================================
        void BuildWorldTrackingLayer()
        {
            _worldTrackLayer = ForestUiWidgets.Stretch("WorldTrackLayer", _canvasRt).gameObject;
            for (int i = 0; i < 10; i++)
                _checkMarks.Add(CreateTrackItem("CheckMark_" + i, new Vector2(46f, 46f), false));
            for (int i = 0; i < 24; i++)
                _worldBubbles.Add(CreateTrackItem("WorldBubble_" + i, new Vector2(150f, 54f), true));
        }

        TrackBubble CreateTrackItem(string name, Vector2 size, bool clickable)
        {
            RectTransform rt = ForestUiWidgets.MakeRect(name, _worldTrackLayer.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, size);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.sprite = ForestComicUiSkin.SpeechBubble;
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = clickable;

            Image ico = ForestUiWidgets.MakeIcon("Ico", rt, ForestComicUiSkin.GetArt("ico_check"), new Vector2(0f, 6f), new Vector2(36f, 36f));
            Text lbl = ForestUiWidgets.MakeText("Txt", rt, "", 18, ForestUiWidgets.InkDark, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 5f), new Vector2(-14f, -12f), outline: false);

            var tb = new TrackBubble { rt = rt, bg = bg, icon = ico, label = lbl };
            if (clickable)
            {
                var btn = rt.gameObject.AddComponent<Button>();
                btn.targetGraphic = bg;
                btn.onClick.AddListener(() => tb.onClick?.Invoke());
                tb.btn = btn;
            }
            rt.gameObject.SetActive(false);
            return tb;
        }

        // =====================================================================
        // 2. 主营地 HUD（双轨人口 + 铜币/手稿/仓库上限 + 右侧订单板入口 + 底部 6 宫格）
        // =====================================================================
        void BuildMainCampHud()
        {
            _mainHudLayer = ForestUiWidgets.Stretch("MainCampHud", _canvasRt).gameObject;
            Transform root = _mainHudLayer.transform;

            // --- 左上角主角头像框（点击打开骨干委任与换装） ---
            Image avatarFrame = ForestUiWidgets.MakeSlicedImage("AvatarFrame", root, ForestComicUiSkin.DialogOuter, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -14f), new Vector2(84f, 84f), true);
            avatarFrame.gameObject.AddComponent<Button>().onClick.AddListener(OpenHeroAssignModal);
            Image avatarInner = ForestUiWidgets.MakeSlicedImage("AvatarBg", avatarFrame.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-10f, -10f), false);
            avatarInner.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            ForestUiWidgets.MakeIcon("LeaderBust", avatarInner.transform, ForestComicUiSkin.GetArt("bust_leader", ForestComicUiSkin.RedDotBadge),
                new Vector2(0f, -8f), new Vector2(86f, 86f));

            // --- 第一排：星级经验条 + 双轨人口胶囊 (当前人口/供暖上限) + 仓库储量胶囊 ---
            Image xpBg = ForestUiWidgets.MakeSlicedImage("XpPill", root, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(110f, -16f), new Vector2(156f, 35f), false);
            RectTransform xpFillMask = ForestUiWidgets.MakeRect("XpFillArea", xpBg.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-10f, -10f));
            _xpFillImg = xpFillMask.gameObject.AddComponent<Image>();
            _xpFillImg.sprite = ForestComicUiSkin.BtnOrange;
            _xpFillImg.type = Image.Type.Filled;
            _xpFillImg.fillMethod = Image.FillMethod.Horizontal;
            _xpFillImg.raycastTarget = false;
            _xpBarTxt = ForestUiWidgets.MakeText("XpTxt", xpBg.transform, "0/60", 15, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(10f, 0f), Vector2.zero);

            Image starIcon = ForestUiWidgets.MakeIcon("LvStar", xpBg.transform, ForestComicUiSkin.GetArt("ico_xp_star", ForestComicUiSkin.RedDotBadge),
                new Vector2(-70f, 1f), new Vector2(46f, 46f));
            _lvStarTxt = ForestUiWidgets.MakeText("LvNum", starIcon.transform, "1", 21, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -1f), Vector2.zero);

            Image popBg = ForestUiWidgets.MakeSlicedImage("PopPill", root, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(274f, -16f), new Vector2(124f, 35f), true);
            popBg.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenSourceRouteForCondition(CondKind.PopCur, SurvRes.Wood));
            ForestUiWidgets.MakeIcon("PopIco", popBg.transform, ForestComicUiSkin.GetArt("bust_cook", ForestComicUiSkin.RedDotBadge),
                new Vector2(-40f, 0f), new Vector2(30f, 30f));
            _popTxt = ForestUiWidgets.MakeText("PopTxt", popBg.transform, "4/8", 17, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(12f, 0f), Vector2.zero);

            Image capBg = ForestUiWidgets.MakeSlicedImage("CapPill", root, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(406f, -16f), new Vector2(138f, 35f), true);
            capBg.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenQuickBuildList(showWarehouse: true));
            ForestUiWidgets.MakeIcon("CapIco", capBg.transform, ForestComicUiSkin.GetArt("dock_bag", ForestComicUiSkin.RedDotBadge),
                new Vector2(-46f, 0f), new Vector2(28f, 28f));
            _storageCapTxt = ForestUiWidgets.MakeText("CapTxt", capBg.transform, "仓10/80", 16, new Color32(216, 238, 255, 255), TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(12f, 0f), Vector2.zero);

            // --- 第二排：营地铜币胶囊 + 工程手稿(蓝图)胶囊（点击直接弹【获取途径】！） ---
            Image coinBg = ForestUiWidgets.MakeSlicedImage("CoinPill", root, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(110f, -58f), new Vector2(132f, 34f), true);
            coinBg.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenSourceRouteForCondition(CondKind.Coins, SurvRes.Wood));
            ForestUiWidgets.MakeIcon("CoinIco", coinBg.transform, ForestComicUiSkin.GetArt("ico_coin", ForestComicUiSkin.RedDotBadge),
                new Vector2(-44f, 0f), new Vector2(26f, 26f));
            _coinTxt = ForestUiWidgets.MakeText("CoinTxt", coinBg.transform, "120", 17, ForestUiWidgets.WarmGold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(10f, 0f), Vector2.zero);

            Image bpBg = ForestUiWidgets.MakeSlicedImage("BlueprintPill", root, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(250f, -58f), new Vector2(126f, 34f), true);
            bpBg.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenSourceRouteForCondition(CondKind.Resource, SurvRes.Blueprint));
            ForestUiWidgets.MakeIcon("BpIco", bpBg.transform, ForestComicUiSkin.ResIcon(SurvRes.Blueprint),
                new Vector2(-40f, 0f), new Vector2(26f, 26f));
            _blueprintTxt = ForestUiWidgets.MakeText("BpTxt", bpBg.transform, "0", 17, new Color32(138, 206, 255, 255), TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(12f, 0f), Vector2.zero);

            // --- 右上侧常驻【居民生计订单】快捷挂件（赚铜币与经验主入口！） ---
            Image orderEntry = ForestUiWidgets.MakeSlicedImage("RightOrderEntry", root, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -16f), new Vector2(138f, 74f), true);
            orderEntry.gameObject.AddComponent<Button>().onClick.AddListener(OpenResidentDemandDrawer);
            ForestUiWidgets.MakeIcon("OrdBust", orderEntry.transform, ForestComicUiSkin.GetArt("bust_laochentou", ForestComicUiSkin.GetArt("bust_cook")),
                new Vector2(-40f, 2f), new Vector2(52f, 52f));
            ForestUiWidgets.MakeText("OrdTitle", orderEntry.transform, "居民订单", 17, ForestUiWidgets.WarmGold, TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(22f, -10f), new Vector2(-52f, -12f));
            _orderBadgeCntTxt = ForestUiWidgets.MakeText("OrdSub", orderEntry.transform, "赚铜币XP", 14, Color.white, TextAnchor.LowerLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(22f, 10f), new Vector2(-52f, -12f));
            _orderRightDot = ForestUiWidgets.MakeIcon("Dot", orderEntry.transform, ForestComicUiSkin.RedDotBadge, new Vector2(58f, 26f), new Vector2(22f, 22f)).gameObject;

            // --- 左侧精简核心资源追踪栈（最多 6 枚胶囊，全库存走底部仓库总览） ---
            for (int i = 0; i < HudPinnedRes.Length; i++)
            {
                SurvRes r = HudPinnedRes[i];
                Image pill = ForestUiWidgets.MakeSlicedImage("ResPill_" + r, root, ForestComicUiSkin.DarkPill, Color.white,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -108f - i * 36f), new Vector2(114f, 33f), false);
                ForestUiWidgets.MakeIcon("Ico", pill.transform, ForestComicUiSkin.ResIcon(r), new Vector2(-36f, 0f), new Vector2(27f, 27f));
                Text valTxt = ForestUiWidgets.MakeText("Val", pill.transform, "0", 17, Color.white, TextAnchor.MiddleLeft,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(18f, 0f), new Vector2(-36f, 0f));
                _resPillRts[r] = pill.rectTransform;
                _resPillTxts[r] = valTxt;
                _resPulseTimers[r] = 0f;
            }

            // --- 左侧尸潮来袭倒计时挂件 ---
            Image hordeBg = ForestUiWidgets.MakeSlicedImage("HordeLeftBadge", root, ForestComicUiSkin.DarkPill, new Color(1f, 0.85f, 0.85f, 1f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -340f), new Vector2(138f, 58f), true);
            hordeBg.gameObject.AddComponent<Button>().onClick.AddListener(OpenCrisisPoster);
            ForestUiWidgets.MakeIcon("ZomIco", hordeBg.transform, ForestComicUiSkin.GetArt("bust_zombie_boss", ForestComicUiSkin.RedDotBadge),
                new Vector2(-44f, 2f), new Vector2(44f, 44f));
            ForestUiWidgets.MakeText("Title", hordeBg.transform, "尸潮将至", 16, new Color32(255, 196, 120, 255), TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(18f, -6f), new Vector2(-46f, -8f));
            _hordeBadgeTimerTxt = ForestUiWidgets.MakeText("Timer", hordeBg.transform, "29:59", 17, new Color32(255, 86, 76, 255), TextAnchor.LowerLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(18f, 6f), new Vector2(-46f, -8f));
            _hordeBadgeGo = hordeBg.gameObject;
            _hordeBadgeGo.SetActive(false);

            // --- 左下角浮动主线任务卡 ---
            _questCardBg = ForestUiWidgets.MakeSlicedImage("FloatingQuestCard", root, ForestComicUiSkin.QuestCard, Color.white,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(14f, 122f), new Vector2(432f, 72f), true);
            var qBtn = _questCardBg.gameObject.AddComponent<Button>();
            qBtn.targetGraphic = _questCardBg;
            qBtn.onClick.AddListener(OnQuestCardClicked);

            RectTransform qFillArea = ForestUiWidgets.MakeRect("QFill", _questCardBg.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(-16f, 6f));
            _questFillImg = qFillArea.gameObject.AddComponent<Image>();
            _questFillImg.sprite = ForestComicUiSkin.BtnGreen;
            _questFillImg.type = Image.Type.Filled;
            _questFillImg.fillMethod = Image.FillMethod.Horizontal;
            _questFillImg.raycastTarget = false;

            Image clipImg = ForestUiWidgets.MakeIcon("ClipIco", _questCardBg.transform, ForestComicUiSkin.GetArt("ico_clipboard", ForestComicUiSkin.RedDotBadge),
                new Vector2(-180f, 4f), new Vector2(54f, 54f));
            _questCheckImg = ForestUiWidgets.MakeIcon("DoneCheck", clipImg.transform, ForestComicUiSkin.GetArt("ico_check"),
                new Vector2(14f, -12f), new Vector2(28f, 28f));
            _questTitleTxt = ForestUiWidgets.MakeText("QuestTxt", _questCardBg.transform, "加载主线任务...", 18, Color.white, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(26f, 2f), new Vector2(-82f, -12f));

            // --- 底部手绘漫画 6 宫格导航栏 (Dock) ---
            Image dockShelf = ForestUiWidgets.MakeSlicedImage("BottomComicDock", root, ForestComicUiSkin.DarkPill, new Color(1f, 1f, 1f, 0.92f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(-16f, 104f), true);

            string[] dockNames = { "迎击", "建造", "骨干", "仓库", "行商", "丧尸城" };
            string[] dockIcons = { "dock_defense", "dock_build", "dock_heroes", "dock_bag", "dock_radio", "dock_map" };
            for (int i = 0; i < 6; i++)
            {
                int tabIdx = i;
                float x = -288f + i * 115.2f;
                Image slot = ForestUiWidgets.MakeSlicedImage("DockTab_" + i, dockShelf.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 2f), new Vector2(106f, 90f), true);
                var btn = slot.gameObject.AddComponent<Button>();
                btn.targetGraphic = slot;
                btn.onClick.AddListener(() => OnBottomDockClicked(tabIdx));

                ForestUiWidgets.MakeIcon("Art", slot.transform, ForestComicUiSkin.GetArt(dockIcons[i], ForestComicUiSkin.RedDotBadge),
                    new Vector2(0f, 15f), new Vector2(58f, 58f));
                ForestUiWidgets.MakeText("Name", slot.transform, dockNames[i], 16, ForestUiWidgets.InkDark, TextAnchor.LowerCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(-6f, -10f), outline: false);

                Image lk = ForestUiWidgets.MakeIcon("Lock", slot.transform, ForestComicUiSkin.GetArt("ico_lock"), new Vector2(30f, -24f), new Vector2(28f, 28f));
                _dockLocks[i] = lk;

                Image dot = ForestUiWidgets.MakeIcon("RedDot", slot.transform, ForestComicUiSkin.RedDotBadge, new Vector2(38f, 34f), new Vector2(22f, 22f));
                _dockRedDots[i] = dot.gameObject;
            }
        }

        // =====================================================================
        // 3. 建筑脚下【轻气泡圆钮菜单】
        // =====================================================================
        void BuildFootRingMenu()
        {
            _footMenuRt = ForestUiWidgets.MakeRect("FootRingMenu", _mainHudLayer.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260f, 220f));

            Image titlePill = ForestUiWidgets.MakeSlicedImage("TitlePill", _footMenuRt, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 14f), new Vector2(196f, 34f), false);
            _footTitleTxt = ForestUiWidgets.MakeText("Title", titlePill.transform, "河畔钓鱼台", 18, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            Image ratePill = ForestUiWidgets.MakeSlicedImage("RatePill", _footMenuRt, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(226f, 30f), false);
            _footRateTxt = ForestUiWidgets.MakeText("Rate", ratePill.transform, "储水上限 +10", 15, ForestUiWidgets.WarmGold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            _footBtnPrimary = CreateCircleActionBtn("PrimaryRingBtn", _footMenuRt, new Vector2(0f, -92f), 84f, out _footIconPrimary, out _footLblPrimary);
            _footBtnSecondary = CreateCircleActionBtn("SecondaryRingBtn", _footMenuRt, new Vector2(92f, -92f), 74f, out _footIconSecondary, out _footLblSecondary);
            _footMenuRt.gameObject.SetActive(false);
        }

        static Button CreateCircleActionBtn(string name, Transform parent, Vector2 pos, float diameter, out Image innerIcon, out Text bottomLabel)
        {
            RectTransform rt = ForestUiWidgets.MakeRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(diameter, diameter));
            var ringImg = rt.gameObject.AddComponent<Image>();
            ringImg.sprite = ForestComicUiSkin.CircleActionRing;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = ringImg;

            innerIcon = ForestUiWidgets.MakeIcon("Ico", rt, ForestComicUiSkin.GetArt("ico_fish_cast"), new Vector2(0f, 4f), new Vector2(diameter * 0.58f, diameter * 0.58f));
            bottomLabel = ForestUiWidgets.MakeText("Lbl", rt, "钓鱼", 17, Color.white, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 8f), new Vector2(120f, 26f));
            return btn;
        }

        // =====================================================================
        // 4. 破框立体【建造 / 升级确认卡】（完整对标参考图：人口/铜币/手稿/加工件 + [去生产]/[获取]/[前往]！）
        // =====================================================================
        void BuildBreakOutConfirmModal()
        {
            _buildCardModal = MakeModalMask("BuildConfirmModal", 0.62f, CloseAllModals);
            Image card = ForestUiWidgets.MakeSlicedImage("Card", _buildCardModal.transform, ForestComicUiSkin.DialogOuter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(608f, 736f), true);

            _bcIllustImg = ForestUiWidgets.MakeIcon("BreakOutArt", card.transform, ForestComicUiSkin.GetArt("sawmill_shed"),
                new Vector2(-190f, 326f), new Vector2(196f, 196f));

            _bcTitleTxt = ForestUiWidgets.MakeText("Title", card.transform, "锯木木艺坊", 27, Color.white, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(56f, -18f), new Vector2(-196f, 38f));
            _bcDescTxt = ForestUiWidgets.MakeText("Desc", card.transform, "描述文案", 16, new Color32(226, 214, 198, 255), TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(56f, -58f), new Vector2(-196f, 56f), outline: false);

            ForestUiWidgets.MakeJellyButton("BtnClose", card.transform, ForestComicUiSkin.BtnRed, "✕", 22,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -14f), new Vector2(46f, 46f), CloseAllModals, out _);

            Image paper = ForestUiWidgets.MakeSlicedImage("PaperInner", card.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), new Vector2(564f, 468f), false);

            // 收益提升对比行
            Image benRow = ForestUiWidgets.MakeSlicedImage("BenefitRow", paper.transform, ForestComicUiSkin.SubSlot, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(530f, 60f), false);
            _bcBenefitIcon = ForestUiWidgets.MakeIcon("BenIco", benRow.transform, ForestComicUiSkin.ResIcon(SurvRes.Plank), new Vector2(-226f, 0f), new Vector2(42f, 42f));
            _bcBenefitNameTxt = ForestUiWidgets.MakeText("BenName", benRow.transform, "单品原料储量上限", 19, ForestUiWidgets.InkDark, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0.65f, 1f), new Vector2(0f, 0.5f), new Vector2(68f, 0f), Vector2.zero, outline: false);
            _bcBenefitFromTxt = ForestUiWidgets.MakeText("BenFrom", benRow.transform, "+80", 20, ForestUiWidgets.InkMuted, TextAnchor.MiddleRight,
                new Vector2(0.55f, 0f), new Vector2(0.75f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: false);
            ForestUiWidgets.MakeText("BenArrow", benRow.transform, "▶▶", 17, ForestUiWidgets.InkMuted, TextAnchor.MiddleCenter,
                new Vector2(0.75f, 0f), new Vector2(0.84f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: false);
            _bcBenefitToTxt = ForestUiWidgets.MakeText("BenTo", benRow.transform, "+180", 21, ForestUiWidgets.LushGreen, TextAnchor.MiddleRight,
                new Vector2(0.82f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), Vector2.zero, outline: false);

            ForestUiWidgets.MakeText("CondHeader", paper.transform, "建造条件", 17, ForestUiWidgets.InkMuted, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(0f, 26f), outline: false);

            for (int i = 0; i < 5; i++)
            {
                Image crow = ForestUiWidgets.MakeSlicedImage("CostRow_" + i, paper.transform, ForestComicUiSkin.SubSlot, new Color(1f, 1f, 1f, 0.78f),
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -114f - i * 68f), new Vector2(530f, 60f), false);
                _bcCostRows[i] = crow.gameObject;

                Image ico = ForestUiWidgets.MakeIcon("Ico", crow.transform, ForestComicUiSkin.ResIcon(SurvRes.Wood), new Vector2(-228f, 0f), new Vector2(44f, 44f), raycast: true);
                _bcCostIcons[i] = ico;
                _bcCostIconBtns[i] = ico.gameObject.AddComponent<Button>();

                _bcCostTxts[i] = ForestUiWidgets.MakeText("Amt", crow.transform, "厚木板  2/8", 20, ForestUiWidgets.InkDark, TextAnchor.MiddleLeft,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(16f, 0f), new Vector2(-210f, 0f), outline: false);
                _bcCostChecks[i] = ForestUiWidgets.MakeIcon("Chk", crow.transform, ForestComicUiSkin.GetArt("ico_check"), new Vector2(106f, 0f), new Vector2(34f, 34f));
                _bcJumpBtns[i] = ForestUiWidgets.MakeJellyButton("BtnJump_" + i, crow.transform, ForestComicUiSkin.BtnBlue, "去生产", 16,
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(112f, 42f), null, out _bcJumpLbls[i]);
            }

            _bcLvGateRedTxt = ForestUiWidgets.MakeText("LvGateNotice", card.transform, "", 19, new Color32(255, 92, 82, 255), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 92f), new Vector2(560f, 30f));
            _bcTimeTxt = ForestUiWidgets.MakeText("BuildDuration", card.transform, "⏱ 4秒", 18, ForestUiWidgets.CreamLight, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(260f, 28f));

            _bcConfirmBtn = ForestUiWidgets.MakeJellyButton("BtnConfirmBuild", card.transform, ForestComicUiSkin.BtnBlue, "建造", 25,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(220f, 66f), OnConfirmBuildCardClicked, out _bcBtnLbl);

            _buildCardModal.SetActive(false);
        }

        // =====================================================================
        // 4B. 物品来源详情小卡 & 【获取途径】多路跳转弹窗（复刻截图 1, 3, 4, 5！）
        // =====================================================================
        void BuildItemAndSourcePopups()
        {
            // 1. 物品点击小弹窗（“铁钉铆件 · 拥有:0 · 在废铁锻造坊中生产获得 -> [前往获取]”）
            _itemTipPopup = MakeModalMask("ItemSourceTipPopup", 0.45f, () => _itemTipPopup.SetActive(false));
            Image tipCard = ForestUiWidgets.MakeSlicedImage("TipCard", _itemTipPopup.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(468f, 310f), true);
            Image icoBox = ForestUiWidgets.MakeSlicedImage("IcoBox", tipCard.transform, ForestComicUiSkin.SubSlotSelected, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -22f), new Vector2(96f, 96f), false);
            _itIcon = ForestUiWidgets.MakeIcon("Ico", icoBox.transform, ForestComicUiSkin.ResIcon(SurvRes.Nails), Vector2.zero, new Vector2(72f, 72f));
            _itNameTxt = ForestUiWidgets.MakeText("Name", tipCard.transform, "铁钉铆件", 24, ForestUiWidgets.InkDark, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(138f, -28f), new Vector2(-160f, 34f), outline: false);
            _itOwnTxt = ForestUiWidgets.MakeText("Own", tipCard.transform, "拥有：0", 19, ForestUiWidgets.InkMuted, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(138f, -68f), new Vector2(-160f, 30f), outline: false);
            _itSourceTxt = ForestUiWidgets.MakeText("Src", tipCard.transform, "在【废铁锻造坊】中生产获得", 19, ForestUiWidgets.InkDark, TextAnchor.MiddleLeft,
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), new Vector2(-52f, 64f), outline: false);

            ForestUiWidgets.MakeJellyButton("BtnCloseTip", tipCard.transform, ForestComicUiSkin.BtnGrey, "关闭", 20,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-96f, 22f), new Vector2(156f, 54f), () => _itemTipPopup.SetActive(false), out _);
            ForestUiWidgets.MakeJellyButton("BtnGoGet", tipCard.transform, ForestComicUiSkin.BtnGreen, "前往获取", 20,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(96f, 22f), new Vector2(172f, 54f), () =>
                {
                    _itemTipPopup.SetActive(false);
                    _itGoAction?.Invoke();
                }, out _);
            _itemTipPopup.SetActive(false);

            // 2. 【获取途径】多路跳转弹窗（针对营地铜币 / 工程手稿 / 当前人口）
            _sourceRouteModal = MakeModalMask("SourceRouteModal", 0.55f, () => _sourceRouteModal.SetActive(false));
            Image srCard = ForestUiWidgets.MakeSlicedImage("SrCard", _sourceRouteModal.transform, ForestComicUiSkin.DialogOuter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 560f), true);
            ForestUiWidgets.MakeText("Hdr", srCard.transform, "获取途径", 26, Color.white, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(0f, 38f));
            ForestUiWidgets.MakeJellyButton("BtnCloseSr", srCard.transform, ForestComicUiSkin.BtnRed, "✕", 21,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -14f), new Vector2(44f, 44f), () => _sourceRouteModal.SetActive(false), out _);

            Image srPaper = ForestUiWidgets.MakeSlicedImage("Paper", srCard.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(516f, 470f), false);

            Image topBox = ForestUiWidgets.MakeSlicedImage("TopBox", srPaper.transform, ForestComicUiSkin.SubSlot, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -18f), new Vector2(102f, 102f), false);
            _srIcon = ForestUiWidgets.MakeIcon("Ico", topBox.transform, ForestComicUiSkin.ResIcon(SurvRes.Blueprint), Vector2.zero, new Vector2(74f, 74f));
            _srNameTxt = ForestUiWidgets.MakeText("Name", srPaper.transform, "工程手稿", 24, ForestUiWidgets.InkDark, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(142f, -28f), new Vector2(-160f, 34f), outline: false);
            _srOwnTxt = ForestUiWidgets.MakeText("Own", srPaper.transform, "当前拥有：0", 19, ForestUiWidgets.InkMuted, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(142f, -68f), new Vector2(-160f, 30f), outline: false);

            for (int i = 0; i < 3; i++)
            {
                int rIdx = i;
                Image row = ForestUiWidgets.MakeSlicedImage("Route_" + i, srPaper.transform, ForestComicUiSkin.SubSlot, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -138f - i * 104f), new Vector2(476f, 92f), false);
                _srRows[i] = row.gameObject;
                _srRowIcons[i] = ForestUiWidgets.MakeIcon("Ico", row.transform, ForestComicUiSkin.GetArt("dock_map"), new Vector2(-186f, 0f), new Vector2(68f, 68f));
                _srRowTitles[i] = ForestUiWidgets.MakeText("Title", row.transform, "丧尸城开车搜刮", 20, ForestUiWidgets.InkDark, TextAnchor.MiddleLeft,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(18f, 0f), new Vector2(-230f, 0f), outline: false);
                _srRowLockTxts[i] = ForestUiWidgets.MakeText("Lock", row.transform, "5级开启", 18, ForestUiWidgets.AlertRed, TextAnchor.MiddleRight,
                    new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(140f, 0f), outline: false);
                _srRowBtns[i] = ForestUiWidgets.MakeJellyButton("BtnGo_" + i, row.transform, ForestComicUiSkin.BtnBlue, "前往", 19,
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(112f, 48f), () =>
                    {
                        _sourceRouteModal.SetActive(false);
                        _srRowActions[rIdx]?.Invoke();
                    }, out _);
            }
            _sourceRouteModal.SetActive(false);
        }

        // =====================================================================
        // 5. 半屏五大作坊通用【5 格生产队列抽屉】（支持 3 原料与点击缺料跨厂直跳！）
        // =====================================================================
        void BuildWorkshopQueueDrawer()
        {
            _workshopDrawer = ForestUiWidgets.Stretch("WorkshopDrawerRoot", _canvasRt).gameObject;
            Image topDismiss = ForestUiWidgets.MakeSlicedImage("TopDismiss", _workshopDrawer.transform, null, new Color(0, 0, 0, 0.01f),
                new Vector2(0f, 0.54f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true);
            topDismiss.gameObject.AddComponent<Button>().onClick.AddListener(CloseAllModals);

            Image sheet = ForestUiWidgets.MakeSlicedImage("BottomSheet", _workshopDrawer.transform, ForestComicUiSkin.DialogOuter, Color.white,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 686f), true);

            _wsTitleTxt = ForestUiWidgets.MakeText("WsTitle", sheet.transform, "锯木木艺坊 1级", 25, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(0f, 40f));

            _wsUpgradeMiniBtn = ForestUiWidgets.MakeJellyButton("BtnWsUpgrade", sheet.transform, ForestComicUiSkin.BtnOrange, "升级作坊", 17,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -12f), new Vector2(118f, 40f), () =>
                {
                    CampNode target = _activeWorkshop;
                    CloseAllModals();
                    if (target != null) OpenBuildConfirmCard(target);
                }, out _);

            for (int i = 0; i < 5; i++)
            {
                float x = -236f + i * 118f;
                Image qSlot = ForestUiWidgets.MakeSlicedImage("QueueSlot_" + i, sheet.transform,
                    i == 0 ? ForestComicUiSkin.ParchmentPanel : ForestComicUiSkin.SubSlot, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -62f), new Vector2(104f, 100f), true);
                _wsQueueSlotBgs[i] = qSlot;
                _wsQueueIcons[i] = ForestUiWidgets.MakeIcon("Ico", qSlot.transform, null, new Vector2(0f, 8f), new Vector2(60f, 60f));
                _wsQueueTimers[i] = ForestUiWidgets.MakeText("Timer", qSlot.transform, "", 15, ForestUiWidgets.InkDark, TextAnchor.LowerCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), Vector2.zero, outline: false);

                if (i == 0)
                {
                    Button spBtn = ForestUiWidgets.MakeJellyButton("FreeSpeedUp", qSlot.transform, ForestComicUiSkin.BtnGreen, "秒完>>", 15,
                        new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(96f, 28f),
                        () => { if (_activeWorkshop != null) _sim.SpeedUpWorkshopFirstSlot(_activeWorkshop); }, out _);
                    _wsSpeedUpPill = spBtn.gameObject;
                }
            }

            Image body = ForestUiWidgets.MakeSlicedImage("WsBody", sheet.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -198f), new Vector2(684f, 356f), false);

            for (int i = 0; i < 4; i++)
            {
                int rIdx = i;
                float x = -234f + i * 156f;
                Image rSlot = ForestUiWidgets.MakeSlicedImage("Recipe_" + i, body.transform, ForestComicUiSkin.SubSlot, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -18f), new Vector2(140f, 148f), true);
                rSlot.gameObject.AddComponent<Button>().onClick.AddListener(() => { _selectedRecipeIdx = rIdx; });
                _wsRecipeSlots[i] = rSlot.gameObject;
                _wsRecipeFrames[i] = rSlot;
                _wsRecipeIcons[i] = ForestUiWidgets.MakeIcon("Ico", rSlot.transform, ForestComicUiSkin.ResIcon(SurvRes.Plank), new Vector2(0f, 16f), new Vector2(74f, 74f));
                _wsRecipeSubTxts[i] = ForestUiWidgets.MakeText("Name", rSlot.transform, "厚木板", 16, ForestUiWidgets.InkDark, TextAnchor.LowerCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(-8f, -8f), outline: false);
                _wsRecipeLocks[i] = ForestUiWidgets.MakeIcon("Lock", rSlot.transform, ForestComicUiSkin.GetArt("ico_lock"), new Vector2(42f, 44f), new Vector2(32f, 32f));
            }

            Image detail = ForestUiWidgets.MakeSlicedImage("SelDetail", body.transform, ForestComicUiSkin.SubSlot, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(648f, 152f), false);
            Image selBox = ForestUiWidgets.MakeSlicedImage("SelBigBox", detail.transform, ForestComicUiSkin.SubSlotSelected, Color.white,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(112f, 112f), false);
            _wsSelBigIcon = ForestUiWidgets.MakeIcon("BigIco", selBox.transform, ForestComicUiSkin.ResIcon(SurvRes.Plank), Vector2.zero, new Vector2(82f, 82f));
            _wsSelNameTxt = ForestUiWidgets.MakeText("SelName", detail.transform, "厚木板 ×2", 21, ForestUiWidgets.InkDark, TextAnchor.UpperLeft,
                new Vector2(0f, 0.5f), new Vector2(0.56f, 1f), new Vector2(0f, 1f), new Vector2(140f, -16f), Vector2.zero, outline: false);
            _wsSelStockTxt = ForestUiWidgets.MakeText("SelStock", detail.transform, "当前库存: 0", 16, ForestUiWidgets.InkMuted, TextAnchor.LowerLeft,
                new Vector2(0f, 0f), new Vector2(0.56f, 0.5f), new Vector2(0f, 0f), new Vector2(140f, 20f), Vector2.zero, outline: false);

            ForestUiWidgets.MakeText("ReqTitle", detail.transform, "生产所需（点图标溯源）", 16, ForestUiWidgets.InkMuted, TextAnchor.UpperCenter,
                new Vector2(0.54f, 0.5f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), Vector2.zero, outline: false);

            _wsReqIcon1 = ForestUiWidgets.MakeIcon("ReqIco1", detail.transform, ForestComicUiSkin.ResIcon(SurvRes.Wood), new Vector2(124f, -8f), new Vector2(46f, 46f), raycast: true);
            _wsReqBtn1 = _wsReqIcon1.gameObject.AddComponent<Button>();
            _wsReqTxt1 = ForestUiWidgets.MakeText("ReqTxt1", _wsReqIcon1.transform, "8/2", 16, ForestUiWidgets.InkDark, TextAnchor.UpperCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(88f, 22f), outline: false);

            _wsReqIcon2 = ForestUiWidgets.MakeIcon("ReqIco2", detail.transform, ForestComicUiSkin.ResIcon(SurvRes.Water), new Vector2(204f, -8f), new Vector2(46f, 46f), raycast: true);
            _wsReqBtn2 = _wsReqIcon2.gameObject.AddComponent<Button>();
            _wsReqTxt2 = ForestUiWidgets.MakeText("ReqTxt2", _wsReqIcon2.transform, "3/1", 16, ForestUiWidgets.InkDark, TextAnchor.UpperCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(88f, 22f), outline: false);

            _wsReqIcon3 = ForestUiWidgets.MakeIcon("ReqIco3", detail.transform, ForestComicUiSkin.ResIcon(SurvRes.Nails), new Vector2(280f, -8f), new Vector2(46f, 46f), raycast: true);
            _wsReqBtn3 = _wsReqIcon3.gameObject.AddComponent<Button>();
            _wsReqTxt3 = ForestUiWidgets.MakeText("ReqTxt3", _wsReqIcon3.transform, "1/1", 16, ForestUiWidgets.InkDark, TextAnchor.UpperCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(88f, 22f), outline: false);

            ForestUiWidgets.MakeRedBackButton(sheet.transform, CloseAllModals, new Vector2(20f, 18f));
            _wsTimeTxt = ForestUiWidgets.MakeText("WsDur", sheet.transform, "⏱ 4秒", 18, ForestUiWidgets.CreamLight, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 88f), new Vector2(220f, 26f));
            _wsProduceBtn = ForestUiWidgets.MakeJellyButton("BtnProduce", sheet.transform, ForestComicUiSkin.BtnBlue, "生产", 25,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(226f, 66f), OnWorkshopProduceClicked, out _wsBtnLbl);

            _workshopDrawer.SetActive(false);
        }

        // =====================================================================
        // 6. 三槽并行【四维居民生计订单板】（餐饮 / 御寒服装 / 伤病草药 / 安居木床！）
        // =====================================================================
        void BuildResidentDemandDrawer()
        {
            _residentDrawer = ForestUiWidgets.Stretch("ResidentDemandRoot", _canvasRt).gameObject;
            Image topDismiss = ForestUiWidgets.MakeSlicedImage("TopDismiss", _residentDrawer.transform, null, new Color(0, 0, 0, 0.01f),
                new Vector2(0f, 0.62f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true);
            topDismiss.gameObject.AddComponent<Button>().onClick.AddListener(CloseAllModals);

            Image sheet = ForestUiWidgets.MakeSlicedImage("BottomSheet", _residentDrawer.transform, ForestComicUiSkin.DialogOuter, Color.white,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 780f), true);

            ForestUiWidgets.MakeText("Title", sheet.transform, "营地居民生计订单板", 25, Color.white, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(24f, -14f), new Vector2(-240f, 36f));

            // 5单满意宝箱进度条
            Image moodBg = ForestUiWidgets.MakeSlicedImage("MoodBar", sheet.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -54f), new Vector2(440f, 34f), false);
            RectTransform mf = ForestUiWidgets.MakeRect("Fill", moodBg.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8f, -8f));
            _resMoodFill = mf.gameObject.AddComponent<Image>();
            _resMoodFill.sprite = ForestComicUiSkin.BtnOrange;
            _resMoodFill.type = Image.Type.Filled;
            _resMoodFill.fillMethod = Image.FillMethod.Horizontal;
            _resMoodTxt = ForestUiWidgets.MakeText("MoodTxt", moodBg.transform, "满 5 单赠【铜币+260 + 手稿x2】 0/5", 15, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            ForestUiWidgets.MakeJellyButton("BtnRefreshOrders", sheet.transform, ForestComicUiSkin.BtnOrange, "刷新订单", 17,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f, -34f), new Vector2(138f, 48f), () =>
                {
                    if (!_sim.RefreshAllOrders())
                        SpawnWorldRisingText($"刷新冷却中（{Mathf.CeilToInt(_sim.OrderRefreshCooldown)}秒）", new Vector2(16.4f, 16.2f));
                    RefreshResidentDrawer();
                }, out _resRefreshBtnLbl);

            Image paper = ForestUiWidgets.MakeSlicedImage("Paper", sheet.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(680f, 578f), false);

            for (int i = 0; i < 3; i++)
            {
                int slot = i;
                Image row = ForestUiWidgets.MakeSlicedImage("OrderCard_" + i, paper.transform, ForestComicUiSkin.SubSlot, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f - i * 186f), new Vector2(648f, 174f), false);

                Image bustBox = ForestUiWidgets.MakeSlicedImage("BustBox", row.transform, ForestComicUiSkin.DialogOuter, Color.white,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -12f), new Vector2(86f, 86f), false);
                bustBox.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                _ordBusts[i] = ForestUiWidgets.MakeIcon("Bust", bustBox.transform, ForestComicUiSkin.GetArt("bust_cook"), new Vector2(0f, -6f), new Vector2(92f, 92f));

                _ordTitles[i] = ForestUiWidgets.MakeText("Title", row.transform, "小棠 · [御寒穿戴单]", 17, ForestUiWidgets.InkDark, TextAnchor.UpperLeft,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(108f, -10f), new Vector2(-240f, 26f), outline: false);
                _ordComplaints[i] = ForestUiWidgets.MakeText("Line", row.transform, "逃难穿来的卫衣全刮破了...", 14, ForestUiWidgets.InkMuted, TextAnchor.UpperLeft,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(108f, -38f), new Vector2(-398f, 66f), outline: false);
                _ordRewards[i] = ForestUiWidgets.MakeText("Rw", row.transform, "奖励：铜币 +150   经验 +34XP", 16, new Color32(186, 92, 16, 255), TextAnchor.LowerLeft,
                    new Vector2(0f, 0f), new Vector2(0.65f, 0.5f), new Vector2(0f, 0f), new Vector2(16f, 12f), Vector2.zero, outline: false);

                // 所需物品 1 & 2（点击可直接跳去对应作坊生产！）
                Image req1Bg = ForestUiWidgets.MakeSlicedImage("Req1", row.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-214f, 8f), new Vector2(68f, 78f), true);
                _ordReqBtn1[i] = req1Bg.gameObject.AddComponent<Button>();
                _ordReqIco1[i] = ForestUiWidgets.MakeIcon("Ico", req1Bg.transform, ForestComicUiSkin.ResIcon(SurvRes.LinenShirt), new Vector2(0f, 10f), new Vector2(46f, 46f));
                _ordReqTxt1[i] = ForestUiWidgets.MakeText("Txt", req1Bg.transform, "0/1", 15, ForestUiWidgets.AlertRed, TextAnchor.LowerCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), Vector2.zero, outline: false);

                Image req2Bg = ForestUiWidgets.MakeSlicedImage("Req2", row.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-138f, 8f), new Vector2(68f, 78f), true);
                _ordReqBox2[i] = req2Bg.gameObject;
                _ordReqBtn2[i] = req2Bg.gameObject.AddComponent<Button>();
                _ordReqIco2[i] = ForestUiWidgets.MakeIcon("Ico", req2Bg.transform, ForestComicUiSkin.ResIcon(SurvRes.Water), new Vector2(0f, 10f), new Vector2(46f, 46f));
                _ordReqTxt2[i] = ForestUiWidgets.MakeText("Txt", req2Bg.transform, "1/1", 15, ForestUiWidgets.InkDark, TextAnchor.LowerCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), Vector2.zero, outline: false);

                _ordDeliverBtns[i] = ForestUiWidgets.MakeJellyButton("BtnDeliver_" + i, row.transform, ForestComicUiSkin.BtnGreen, "交付订单", 19,
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(116f, 58f),
                    () => OnOrderSlotActionClicked(slot), out _ordDeliverLbls[i]);
            }

            ForestUiWidgets.MakeRedBackButton(sheet.transform, CloseAllModals, new Vector2(20f, 14f));
            _residentDrawer.SetActive(false);
        }

        // =====================================================================
        // 6B. 【短波电台 · 山口行商驿站】大宗贸易面板
        // =====================================================================
        void BuildCaravanModal()
        {
            _caravanModal = MakeModalMask("CaravanTradeModal", 0.65f, CloseAllModals);
            Image card = ForestUiWidgets.MakeSlicedImage("Card", _caravanModal.transform, ForestComicUiSkin.DialogOuter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(636f, 690f), true);
            ForestUiWidgets.MakeText("Title", card.transform, "山口行商驿站 · 大宗收购", 26, Color.white, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(0f, 38f));
            _cvSubTxt = ForestUiWidgets.MakeText("Sub", card.transform, "邻山幸存者驼队高价收购防具成衣、草药包与熏肉，结算大额铜币与工程手稿！", 16, ForestUiWidgets.WarmGold, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(-48f, 36f));
            ForestUiWidgets.MakeJellyButton("Close", card.transform, ForestComicUiSkin.BtnRed, "✕", 21,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -14f), new Vector2(44f, 44f), CloseAllModals, out _);

            Image paper = ForestUiWidgets.MakeSlicedImage("Paper", card.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -24f), new Vector2(592f, 520f), false);

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                Image row = ForestUiWidgets.MakeSlicedImage("Offer_" + i, paper.transform, ForestComicUiSkin.SubSlot, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f - i * 164f), new Vector2(556f, 150f), false);
                _cvIcons[i] = ForestUiWidgets.MakeIcon("Ico", row.transform, ForestComicUiSkin.ResIcon(SurvRes.LinenShirt), new Vector2(-216f, 4f), new Vector2(78f, 78f));
                _cvTitles[i] = ForestUiWidgets.MakeText("Title", row.transform, "邻山伐木营采购御寒装", 20, ForestUiWidgets.InkDark, TextAnchor.UpperLeft,
                    new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(118f, -14f), new Vector2(-270f, 32f), outline: false);
                _cvNeedTxts[i] = ForestUiWidgets.MakeText("Need", row.transform, "收购需求：麻布衣 0/2", 18, ForestUiWidgets.InkMuted, TextAnchor.MiddleLeft,
                    new Vector2(0f, 0f), new Vector2(1f, 0.7f), new Vector2(0f, 0.5f), new Vector2(118f, 8f), new Vector2(-270f, 0f), outline: false);
                _cvRewardTxts[i] = ForestUiWidgets.MakeText("Rw", row.transform, "报酬：铜币+320  ·  工程手稿+1", 17, ForestUiWidgets.LushGreen, TextAnchor.LowerLeft,
                    new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(118f, 14f), new Vector2(-270f, 26f), outline: false);
                _cvTradeBtns[i] = ForestUiWidgets.MakeJellyButton("BtnTrade_" + i, row.transform, ForestComicUiSkin.BtnGreen, "装车交货", 19,
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(126f, 56f),
                    () => OnCaravanTradeClicked(idx), out _cvTradeLbls[i]);
            }
            _caravanModal.SetActive(false);
        }

        // =====================================================================
        // 7. 全屏危机预警海报《尸潮来袭》
        // =====================================================================
        void BuildCrisisHordeModal()
        {
            _crisisModal = ForestUiWidgets.Stretch("CrisisHordeModal", _canvasRt).gameObject;
            Image bg = _crisisModal.AddComponent<Image>();
            bg.sprite = ForestComicUiSkin.GetArt("crisis_horde_bg", ForestComicUiSkin.DialogOuter);
            bg.color = Color.white;

            ForestUiWidgets.MakeText("BigTitle", _crisisModal.transform, "尸潮来袭", 58, new Color32(236, 246, 255, 255), TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(0f, 76f));
            _crisisTimerBigTxt = ForestUiWidgets.MakeText("BigTimer", _crisisModal.transform, "00:29:59", 40, new Color32(255, 72, 62, 255), TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -152f), new Vector2(0f, 52f));

            Image callout = ForestUiWidgets.MakeSlicedImage("Callout", _crisisModal.transform, ForestComicUiSkin.SpeechBubble, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), new Vector2(78f, 36f), new Vector2(360f, 88f), false);
            ForestUiWidgets.MakeText("Quip", callout.transform, "山下的尸群闻着烟味爬上来了！守住营地可缴获珍贵的【工程手稿】！", 18, ForestUiWidgets.InkDark, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(-24f, -18f), outline: false);

            Image t1 = ForestUiWidgets.MakeSlicedImage("PrepRow1", _crisisModal.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 360f), new Vector2(636f, 68f), false);
            ForestUiWidgets.MakeIcon("Ico", t1.transform, ForestComicUiSkin.ResIcon(SurvRes.Ration), new Vector2(-276f, 0f), new Vector2(44f, 44f));
            _crisisTask1Txt = ForestUiWidgets.MakeText("Txt", t1.transform, "储备16份熟食口粮 (10/16)", 20, Color.white, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(16f, 0f), new Vector2(-180f, 0f));
            ForestUiWidgets.MakeJellyButton("Go1", t1.transform, ForestComicUiSkin.BtnBlue, "前往", 19,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(96f, 46f), () =>
                {
                    CloseAllModals();
                    CampNode k = _sim.Find("kitchen");
                    if (k != null) OpenWorkshopDrawer(k);
                }, out _);

            Image t2 = ForestUiWidgets.MakeSlicedImage("PrepRow2", _crisisModal.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 280f), new Vector2(636f, 68f), false);
            ForestUiWidgets.MakeIcon("Ico", t2.transform, ForestComicUiSkin.GetArt("hunter_tower", ForestComicUiSkin.RedDotBadge), new Vector2(-276f, 0f), new Vector2(48f, 48f));
            _crisisTask2Txt = ForestUiWidgets.MakeText("Txt", t2.transform, "建造捕猎哨塔或尖木拒马 (0/1)", 20, Color.white, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(16f, 0f), new Vector2(-180f, 0f));
            ForestUiWidgets.MakeJellyButton("Go2", t2.transform, ForestComicUiSkin.BtnBlue, "前往", 19,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(96f, 46f), () =>
                {
                    CloseAllModals();
                    CampNode br = _sim.Find("barricade") ?? _sim.Find("shelter");
                    if (br != null) { SmoothFocusGrid(new Vector2(br.gx, br.gy)); OpenBuildConfirmCard(br); }
                }, out _);

            Image rwBox = ForestUiWidgets.MakeSlicedImage("CrisisRewards", _crisisModal.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 126f), new Vector2(450f, 126f), false);
            ForestUiWidgets.MakeText("Hdr", rwBox.transform, "击退尸潮战利品", 18, ForestUiWidgets.WarmGold, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(0f, 26f));
            MakeRewardBadge(rwBox.transform, ForestComicUiSkin.ResIcon(SurvRes.Blueprint), "3", new Vector2(-118f, -12f));
            MakeRewardBadge(rwBox.transform, ForestComicUiSkin.ResIcon(SurvRes.Scrap), "12", new Vector2(0f, -12f));
            MakeRewardBadge(rwBox.transform, ForestComicUiSkin.GetArt("ico_coin"), "350", new Vector2(118f, -12f));

            ForestUiWidgets.MakeRedBackButton(_crisisModal.transform, CloseAllModals, new Vector2(22f, 22f));
            ForestUiWidgets.MakeJellyButton("BtnFightNow", _crisisModal.transform, ForestComicUiSkin.BtnOrange, "立即迎击尸潮", 23,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(40f, 26f), new Vector2(256f, 66f), () =>
                {
                    CloseAllModals();
                    SmoothFocusGrid(new Vector2(16.4f, 16.2f), 6.0f);
                    _world.StartHordeDefenseWave();
                }, out _);

            _crisisModal.SetActive(false);
        }

        static void MakeRewardBadge(Transform parent, Sprite ico, string count, Vector2 pos)
        {
            Image slot = ForestUiWidgets.MakeSlicedImage("Rw", parent, ForestComicUiSkin.ParchmentPanel, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(82f, 78f), false);
            ForestUiWidgets.MakeIcon("Ico", slot.transform, ico, new Vector2(0f, 8f), new Vector2(48f, 48f));
            ForestUiWidgets.MakeText("Cnt", slot.transform, count, 17, ForestUiWidgets.InkDark, TextAnchor.LowerCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), Vector2.zero, outline: false);
        }

        // =====================================================================
        // 8. 等级提升解锁弹窗、新英雄高光、骨干驻守委任、快捷建造与分类仓库总览
        // =====================================================================
        void BuildLevelUpModal()
        {
            _levelUpModal = MakeModalMask("LevelUpModal", 0.68f, CloseAllModals);
            Image card = ForestUiWidgets.MakeSlicedImage("Card", _levelUpModal.transform, ForestComicUiSkin.DialogOuter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540f, 480f), true);

            Image ribbon = ForestUiWidgets.MakeSlicedImage("Ribbon", card.transform, ForestComicUiSkin.BtnOrange, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(320f, 56f), false);
            ForestUiWidgets.MakeText("RibTxt", ribbon.transform, "星级提升", 26, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            _lvUpNumTxt = ForestUiWidgets.MakeText("LvNum", card.transform, "2级", 46, ForestUiWidgets.WarmGold, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(0f, 60f));
            ForestUiWidgets.MakeText("Sub", card.transform, "新建筑与等级上限解锁！", 20, Color.white, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -102f), new Vector2(0f, 32f));

            Image paper = ForestUiWidgets.MakeSlicedImage("Paper", card.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -34f), new Vector2(492f, 240f), false);

            for (int i = 0; i < 3; i++)
            {
                float x = -152f + i * 152f;
                Image uCard = ForestUiWidgets.MakeSlicedImage("Unlock_" + i, paper.transform, ForestComicUiSkin.SubSlot, Color.white,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 4f), new Vector2(134f, 168f), false);
                _lvUpUnlockCards[i] = uCard.gameObject;
                _lvUpUnlockIcons[i] = ForestUiWidgets.MakeIcon("Ico", uCard.transform, ForestComicUiSkin.GetArt("well"), new Vector2(0f, 18f), new Vector2(92f, 92f));
                _lvUpUnlockNames[i] = ForestUiWidgets.MakeText("Name", uCard.transform, "净水石井", 17, ForestUiWidgets.InkDark, TextAnchor.LowerCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(-8f, -8f), outline: false);
            }

            ForestUiWidgets.MakeText("TapHint", card.transform, "点击任意区域继续", 17, new Color32(210, 198, 182, 220), TextAnchor.LowerCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -40f), new Vector2(0f, 28f));

            _levelUpModal.SetActive(false);
        }

        void BuildHeroModals()
        {
            _heroSpotlightModal = MakeModalMask("HeroSpotlightModal", 0.84f, CloseAllModals);
            _heroSpotSunburst = ForestUiWidgets.MakeIcon("Sunburst", _heroSpotlightModal.transform, ForestComicUiSkin.Sunburst, new Vector2(0f, 40f), new Vector2(680f, 680f));
            ForestUiWidgets.MakeText("Hdr", _heroSpotlightModal.transform, "获得新骨干伙伴", 46, ForestUiWidgets.WarmGold, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(0f, 62f));
            _heroSpotNameTxt = ForestUiWidgets.MakeText("Name", _heroSpotlightModal.transform, "大锤", 34, Color.white, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -220f), new Vector2(0f, 44f));
            _heroSpotRoleTxt = ForestUiWidgets.MakeText("Role", _heroSpotlightModal.transform, "重装护卫 · 史诗", 20, new Color32(150, 220, 255, 255), TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -264f), new Vector2(0f, 32f));
            _heroSpotBust = ForestUiWidgets.MakeIcon("HeroBust", _heroSpotlightModal.transform, ForestComicUiSkin.GetArt("bust_driver"), new Vector2(0f, 20f), new Vector2(310f, 310f));
            _heroSpotBuffTxt = ForestUiWidgets.MakeText("Buff", _heroSpotlightModal.transform, "加成说明", 22, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 240f), new Vector2(580f, 44f));
            ForestUiWidgets.MakeJellyButton("BtnGreat", _heroSpotlightModal.transform, ForestComicUiSkin.BtnOrange, "太棒了！", 25,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(220f, 66f), CloseAllModals, out _);
            _heroSpotlightModal.SetActive(false);

            _heroAssignModal = MakeModalMask("HeroAssignModal", 0.62f, CloseAllModals);
            Image sheet = ForestUiWidgets.MakeSlicedImage("Sheet", _heroAssignModal.transform, ForestComicUiSkin.DialogOuter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(640f, 804f), true);
            ForestUiWidgets.MakeText("Title", sheet.transform, "骨干小队专长委任 & 防具换装", 25, Color.white, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(0f, 38f));

            Image paper = ForestUiWidgets.MakeSlicedImage("Paper", sheet.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(596f, 628f), false);

            for (int i = 0; i < 5; i++)
            {
                int rowIdx = i;
                Image row = ForestUiWidgets.MakeSlicedImage("HeroRow_" + i, paper.transform, ForestComicUiSkin.SubSlot, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f - i * 121f), new Vector2(564f, 114f), false);
                _haRows[i] = row.gameObject;
                Image box = ForestUiWidgets.MakeSlicedImage("PortraitBox", row.transform, ForestComicUiSkin.DialogOuter, Color.white,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(88f, 94f), false);
                box.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                _haBusts[i] = ForestUiWidgets.MakeIcon("Bust", box.transform, ForestComicUiSkin.GetArt("bust_leader"), new Vector2(0f, -6f), new Vector2(94f, 94f));
                _haNames[i] = ForestUiWidgets.MakeText("Name", row.transform, "阿澈 · 避难所中枢", 18, ForestUiWidgets.InkDark, TextAnchor.MiddleLeft,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(110f, -8f), new Vector2(-210f, 24f), outline: false);
                _haDescs[i] = ForestUiWidgets.MakeText("Desc", row.transform, "加成说明", 13, ForestUiWidgets.InkMuted, TextAnchor.UpperLeft,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(110f, -34f), new Vector2(-124f, 36f), outline: false);
                _haStates[i] = ForestUiWidgets.MakeText("State", row.transform, "✔ 驻守中", 15, ForestUiWidgets.LushGreen, TextAnchor.MiddleRight,
                    new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -8f), new Vector2(100f, 24f), outline: false);

                for (int t = 0; t < 3; t++)
                {
                    OutfitTier tier = (OutfitTier)t;
                    float bx = 110f + t * 144f;
                    _haSkinBtns[i, t] = ForestUiWidgets.MakeJellyButton($"Skin_{i}_{t}", row.transform, ForestComicUiSkin.BtnGrey, "初装", 13,
                        new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(bx, 7f), new Vector2(138f, 30f),
                        () => OnHeroSkinTierClicked(rowIdx, tier), out _haSkinTexts[i, t]);
                }
            }

            ForestUiWidgets.MakeRedBackButton(sheet.transform, CloseAllModals, new Vector2(20f, 18f));
            ForestUiWidgets.MakeJellyButton("BtnUnassign", sheet.transform, ForestComicUiSkin.BtnRed, "一键卸任", 21,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-70f, 20f), new Vector2(176f, 60f),
                () => { _sim.UnassignAllHeroes(); RefreshHeroAssignModal(); }, out _);
            ForestUiWidgets.MakeJellyButton("BtnAutoAssign", sheet.transform, ForestComicUiSkin.BtnBlue, "一键委任", 22,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(138f, 20f), new Vector2(196f, 60f),
                () =>
                {
                    int n = _sim.AutoAssignHeroes();
                    RefreshHeroAssignModal();
                    if (n > 0) SpawnWorldRisingText("骨干小队已全部驻守上岗！全作坊效率提升！", new Vector2(16.4f, 16.2f));
                }, out _);

            _heroAssignModal.SetActive(false);
        }

        void BuildQuickListModal()
        {
            _quickListModal = MakeModalMask("QuickListModal", 0.62f, CloseAllModals);
            Image card = ForestUiWidgets.MakeSlicedImage("Card", _quickListModal.transform, ForestComicUiSkin.DialogOuter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 820f), true);
            _qlTitleTxt = ForestUiWidgets.MakeText("Title", card.transform, "营地建造与升级总览", 26, Color.white, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(24f, -16f), new Vector2(-240f, 38f));
            _qlSubCapTxt = ForestUiWidgets.MakeText("SubCap", card.transform, "", 16, ForestUiWidgets.WarmGold, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(24f, -54f), new Vector2(-240f, 28f));

            _qlUpgradeWhBtn = ForestUiWidgets.MakeJellyButton("BtnUpWh", card.transform, ForestComicUiSkin.BtnOrange, "升级仓库", 17,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-72f, -20f), new Vector2(120f, 44f), () =>
                {
                    CloseAllModals();
                    CampNode wh = _sim.Find("warehouse");
                    if (wh != null) { SmoothFocusGrid(new Vector2(wh.gx, wh.gy)); OpenBuildConfirmCard(wh); }
                }, out _);

            ForestUiWidgets.MakeJellyButton("Close", card.transform, ForestComicUiSkin.BtnRed, "✕", 22,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -14f), new Vector2(46f, 46f), CloseAllModals, out _);

            Image paper = ForestUiWidgets.MakeSlicedImage("Paper", card.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -28f), new Vector2(596f, 694f), false);
            _qlContentRt = paper.rectTransform;
            _quickListModal.SetActive(false);
        }

        // =====================================================================
        // 9. 丧尸城街区选择 + 虚拟摇杆开车 HUD + 【恭喜获得】放射结算
        // =====================================================================
        void BuildCitySelectAndDriveHud()
        {
            _citySelectModal = MakeModalMask("CityDistrictSelectModal", 0.65f, CloseAllModals);
            Image card = ForestUiWidgets.MakeSlicedImage("Card", _citySelectModal.transform, ForestComicUiSkin.DialogOuter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(648f, 820f), true);
            ForestUiWidgets.MakeIcon("BreakTruck", card.transform, ForestComicUiSkin.GetArt("truck_active"), new Vector2(-210f, 386f), new Vector2(180f, 180f));
            ForestUiWidgets.MakeText("Title", card.transform, "选择丧尸城搜刮街区", 27, Color.white, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(210f, -18f), new Vector2(-270f, 40f));
            ForestUiWidgets.MakeText("Sub", card.transform, "驾驶武装皮卡撞飞丧尸，搜刮建筑升阶急需的【工程手稿】、废铁与医疗件！", 16, new Color32(226, 214, 198, 255), TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(210f, -56f), new Vector2(-240f, 44f), outline: false);
            ForestUiWidgets.MakeJellyButton("Close", card.transform, ForestComicUiSkin.BtnRed, "✕", 22,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -14f), new Vector2(46f, 46f), CloseAllModals, out _);

            Image paper = ForestUiWidgets.MakeSlicedImage("Paper", card.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -38f), new Vector2(604f, 672f), false);

            for (int i = 0; i < 4; i++)
            {
                int sIdx = i;
                CitySiteDef s = _sim.CitySites[i];
                Image row = ForestUiWidgets.MakeSlicedImage("Site_" + i, paper.transform, ForestComicUiSkin.SubSlot, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f - i * 162f), new Vector2(568f, 148f), false);
                ForestUiWidgets.MakeIcon("Building", row.transform, ForestComicUiSkin.GetArt(s.spriteKey), new Vector2(-208f, 2f), new Vector2(124f, 124f));
                ForestUiWidgets.MakeText("Name", row.transform, $"{s.name}  ({s.dangerTag})", 21, ForestUiWidgets.InkDark, TextAnchor.UpperLeft,
                    new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(148f, -16f), new Vector2(-310f, 34f), outline: false);
                ForestUiWidgets.MakeText("Loot", row.transform, s.lootSummary, 15, ForestUiWidgets.InkMuted, TextAnchor.MiddleLeft,
                    new Vector2(0f, 0f), new Vector2(1f, 0.65f), new Vector2(0f, 0.5f), new Vector2(148f, 4f), new Vector2(-310f, 0f), outline: false);

                _csSiteBtns[i] = ForestUiWidgets.MakeJellyButton("BtnGo_" + i, row.transform, ForestComicUiSkin.BtnOrange, $"发车(粮×{s.rationCost})", 18,
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(144f, 56f),
                    () => LaunchCityRunForSite(sIdx), out _csSiteBtnLbls[i]);
            }
            _citySelectModal.SetActive(false);

            _cityDriveHud = ForestUiWidgets.Stretch("CityJoystickDriveHud", _canvasRt).gameObject;
            ForestUiWidgets.MakeText("TopTitle", _cityDriveHud.transform, "搜刮收获", 26, Color.white, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(300f, 36f));
            Image cargoBg = ForestUiWidgets.MakeSlicedImage("CargoBarBg", _cityDriveHud.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(440f, 38f), false);
            RectTransform cf = ForestUiWidgets.MakeRect("CargoFill", cargoBg.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-10f, -10f));
            _cdCargoFill = cf.gameObject.AddComponent<Image>();
            _cdCargoFill.sprite = ForestComicUiSkin.BtnOrange;
            _cdCargoFill.type = Image.Type.Filled;
            _cdCargoFill.fillMethod = Image.FillMethod.Horizontal;
            _cdCargoTxt = ForestUiWidgets.MakeText("CargoNum", cargoBg.transform, "0 / 60", 19, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            Image sumPill = ForestUiWidgets.MakeSlicedImage("LootSumPill", _cityDriveHud.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(460f, 34f), false);
            _cdLootSummaryTxt = ForestUiWidgets.MakeText("LootSum", sumPill.transform, "按住屏幕拖动摇杆驾驶武装皮卡搜刮！", 16, ForestUiWidgets.WarmGold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            Image joyBase = ForestUiWidgets.MakeIcon("JoystickBase", _cityDriveHud.transform, ForestComicUiSkin.JoystickRing, new Vector2(0f, -250f), new Vector2(156f, 156f));
            _joyBaseRt = joyBase.rectTransform;
            Image joyKnob = ForestUiWidgets.MakeIcon("JoystickKnob", _joyBaseRt, ForestComicUiSkin.JoystickThumb, Vector2.zero, new Vector2(70f, 70f));
            _joyKnobRt = joyKnob.rectTransform;

            ForestUiWidgets.MakeRedBackButton(_cityDriveHud.transform, () => _cityArena.TriggerManualSettlement(), new Vector2(22f, 22f));
            ForestUiWidgets.MakeJellyButton("BtnAutoDrive", _cityDriveHud.transform, ForestComicUiSkin.BtnBlue, "自动巡航", 19,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-22f, 24f), new Vector2(138f, 64f),
                () => { _cityArena.AutoPilot = !_cityArena.AutoPilot; }, out _cdAutoBtnLbl);

            _cityDriveHud.SetActive(false);

            _lootSettleModal = MakeModalMask("LootSettleModal", 0.85f, () =>
            {
                _lootSettleModal.SetActive(false);
                _cityDriveHud.SetActive(false);
                _mainHudLayer.SetActive(true);
                _worldTrackLayer.SetActive(true);
                _cityArena.FinishAndReturnCamp();
            });
            _lsSunburst = ForestUiWidgets.MakeIcon("Sunburst", _lootSettleModal.transform, ForestComicUiSkin.Sunburst, new Vector2(0f, 60f), new Vector2(640f, 640f));
            ForestUiWidgets.MakeText("CongratsTitle", _lootSettleModal.transform, "恭喜获得", 50, new Color32(255, 224, 96, 255), TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -220f), new Vector2(0f, 68f));

            for (int i = 0; i < 5; i++)
            {
                Image box = ForestUiWidgets.MakeSlicedImage("LootCard_" + i, _lootSettleModal.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-220f + i * 110f, 40f), new Vector2(98f, 118f), false);
                _lsCards[i] = box.gameObject;
                _lsCardIcons[i] = ForestUiWidgets.MakeIcon("Ico", box.transform, ForestComicUiSkin.ResIcon(SurvRes.Scrap), new Vector2(0f, 14f), new Vector2(62f, 62f));
                _lsCardTxts[i] = ForestUiWidgets.MakeText("Cnt", box.transform, "+12", 18, ForestUiWidgets.InkDark, TextAnchor.LowerCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), Vector2.zero, outline: false);
            }

            ForestUiWidgets.MakeText("Hint", _lootSettleModal.transform, "点击任意区域满载回营", 20, new Color32(220, 214, 204, 235), TextAnchor.LowerCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(0f, 34f));
            _lootSettleModal.SetActive(false);
        }

        // =====================================================================
        // 10. 南岸溪流推镜钓鱼 HUD & 11. 双人半身漫画对话层
        // =====================================================================
        void BuildFishingPushInHud()
        {
            _fishingHud = ForestUiWidgets.Stretch("FishingPushInHud", _canvasRt).gameObject;

            Image topPill = ForestUiWidgets.MakeSlicedImage("FishTopPill", _fishingHud.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -24f), new Vector2(240f, 42f), false);
            ForestUiWidgets.MakeIcon("FishIco", topPill.transform, ForestComicUiSkin.GetArt("ico_fish", ForestComicUiSkin.ResIcon(SurvRes.Fish)),
                new Vector2(-88f, 0f), new Vector2(36f, 36f));
            _fishTaskTopTxt = ForestUiWidgets.MakeText("Txt", topPill.transform, "已钓鲜鱼: 0 条", 18, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(16f, 0f), Vector2.zero);

            RectTransform arcRt = ForestUiWidgets.MakeRect("ArcTensionBar", _fishingHud.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(118f, 38f), new Vector2(64f, 168f));
            var arcBgImg = arcRt.gameObject.AddComponent<Image>();
            arcBgImg.sprite = ForestComicUiSkin.ArcBarBg;
            arcBgImg.raycastTarget = false;
            RectTransform arcFillRt = ForestUiWidgets.Stretch("ArcFill", arcRt);
            _fishArcFill = arcFillRt.gameObject.AddComponent<Image>();
            _fishArcFill.sprite = ForestComicUiSkin.ArcBarFill;
            _fishArcFill.type = Image.Type.Filled;
            _fishArcFill.fillMethod = Image.FillMethod.Vertical;
            _fishArcFill.fillOrigin = (int)Image.OriginVertical.Bottom;
            _fishArcFill.fillAmount = 0.35f;
            _fishArcFill.raycastTarget = false;
            _fishArcRoot = arcRt.gameObject;

            Button castBtn = CreateCircleActionBtn("BtnCastReel", _fishingHud.transform, new Vector2(0f, -330f), 150f, out _fishBtnIcon, out _fishBtnMainLbl);
            _fishBtnIcon.rectTransform.anchoredPosition = new Vector2(0f, 14f);
            _fishBtnMainLbl.fontSize = 24;
            _fishBtnMainLbl.rectTransform.anchoredPosition = new Vector2(0f, 42f);
            castBtn.onClick.AddListener(OnFishingMainBtnClicked);

            Image fastPill = ForestUiWidgets.MakeSlicedImage("FastTapTag", castBtn.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(142f, 36f), false);
            _fishFastTapTag = ForestUiWidgets.MakeText("FastTxt", fastPill.transform, "快速点击", 19, ForestUiWidgets.InkDark, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: false);
            _fishFastTapPill = fastPill.gameObject;

            ForestUiWidgets.MakeRedBackButton(_fishingHud.transform, ExitFishingMode, new Vector2(22f, 22f));
            ForestUiWidgets.MakeJellyButton("BtnAutoFish", _fishingHud.transform, ForestComicUiSkin.BtnBlue, "自动垂钓", 19,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-22f, 24f), new Vector2(138f, 64f),
                () => { _fishAuto = !_fishAuto; }, out _fishAutoLbl);

            _fishingHud.SetActive(false);
        }

        void BuildComicDialogOverlay()
        {
            _comicDialogOverlay = MakeModalMask("ComicBustDialogOverlay", 0.65f, AdvanceComicDialog);

            _cdLeftBust = ForestUiWidgets.MakeIcon("LeftBust", _comicDialogOverlay.transform, ForestComicUiSkin.GetArt("bust_leader"),
                new Vector2(-176f, -60f), new Vector2(300f, 300f));
            _cdRightBust = ForestUiWidgets.MakeIcon("RightBust", _comicDialogOverlay.transform, ForestComicUiSkin.GetArt("bust_mechanic"),
                new Vector2(176f, -60f), new Vector2(300f, 300f));

            Image box = ForestUiWidgets.MakeSlicedImage("SpeechPanel", _comicDialogOverlay.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 220f), new Vector2(656f, 196f), false);

            Image nameBadge = ForestUiWidgets.MakeSlicedImage("SpeakerTag", box.transform, ForestComicUiSkin.DialogOuter, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(24f, 4f), new Vector2(148f, 42f), false);
            _cdSpeakerNameTxt = ForestUiWidgets.MakeText("Speaker", nameBadge.transform, "小棠", 22, ForestUiWidgets.WarmGold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            _cdLineBodyTxt = ForestUiWidgets.MakeText("LineBody", box.transform, "...", 21, ForestUiWidgets.InkDark, TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), new Vector2(-48f, -54f), outline: false);

            ForestUiWidgets.MakeText("NextHint", box.transform, "点击任意处继续 ▸", 16, ForestUiWidgets.InkMuted, TextAnchor.LowerRight,
                Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(-18f, 12f), new Vector2(-40f, -20f), outline: false);

            _comicDialogOverlay.SetActive(false);
        }

        GameObject MakeModalMask(string name, float alpha, UnityEngine.Events.UnityAction onMaskClick)
        {
            RectTransform rt = ForestUiWidgets.Stretch(name, _canvasRt);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.05f, 0.04f, 0.04f, alpha);
            img.raycastTarget = true;
            if (onMaskClick != null)
            {
                var btn = rt.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(onMaskClick);
            }
            return rt.gameObject;
        }

        // =====================================================================
        // 交互动作与全链路溯源跳转调度
        // =====================================================================
        public void CloseAllModals()
        {
            _footMenuNode = null;
            if (_footMenuRt != null) _footMenuRt.gameObject.SetActive(false);
            if (_buildCardModal != null) _buildCardModal.SetActive(false);
            if (_itemTipPopup != null) _itemTipPopup.SetActive(false);
            if (_sourceRouteModal != null) _sourceRouteModal.SetActive(false);
            if (_workshopDrawer != null) _workshopDrawer.SetActive(false);
            if (_residentDrawer != null) _residentDrawer.SetActive(false);
            if (_caravanModal != null) _caravanModal.SetActive(false);
            if (_crisisModal != null) _crisisModal.SetActive(false);
            if (_levelUpModal != null) _levelUpModal.SetActive(false);
            if (_heroSpotlightModal != null) _heroSpotlightModal.SetActive(false);
            if (_heroAssignModal != null) _heroAssignModal.SetActive(false);
            if (_quickListModal != null) _quickListModal.SetActive(false);
            if (_citySelectModal != null) _citySelectModal.SetActive(false);
        }

        void HandleNodeClicked(CampNode n)
        {
            if (n == null) return;

            if (n.state == NodeState.ReadyToHammer)
            {
                _sim.CompleteHammerNode(n);
                return;
            }
            if (n.state == NodeState.Constructing)
            {
                _sim.SpeedUpNodeBuild(n);
                return;
            }
            if (n.kind == NodeKind.RelicLoot && n.state == NodeState.Discovered)
            {
                _sim.StartBuildOrUpgrade(n, out _);
                return;
            }
            if (n.state == NodeState.Discovered)
            {
                OpenBuildConfirmCard(n);
                return;
            }

            if (n.kind == NodeKind.Farm || n.storedAmount > 0)
            {
                if (_sim.TryCollectOrInteractNode(n, out string msg) && !string.IsNullOrEmpty(msg))
                    return;
            }

            // 五大生产作坊 -> 打开半屏生产队列抽屉！
            if (ForestSurvivalSim.IsWorkshopKind(n.kind))
            {
                OpenWorkshopDrawer(n, 0);
                return;
            }

            if (n.kind == NodeKind.RadioPost)
            {
                OpenCaravanModal();
                return;
            }

            OpenFootRingMenu(n);
        }

        public void OpenFootRingMenu(CampNode n)
        {
            CloseAllModals();
            _footMenuNode = n;
            SmoothFocusGrid(new Vector2(n.gx, n.gy));
            _footTitleTxt.text = $"{n.activeTitle}" + (n.maxLevel > 1 ? $" Lv.{n.level}" : "");

            if (n.kind == NodeKind.HeatTower) _footRateTxt.text = $"当前供暖人口上限：{_sim.PopCap}";
            else if (n.kind == NodeKind.Dwelling) _footRateTxt.text = $"本屋入驻人口：+{ForestSurvivalSim.GetDwellingPopForLevel(n.id, n.level)}";
            else if (n.kind == NodeKind.Warehouse) _footRateTxt.text = $"当前单品储量上限：{_sim.StorageCap}";
            else _footRateTxt.text = $"{n.benefitLabel} +{n.benefitTo}";

            _footBtnSecondary.gameObject.SetActive(false);
            _footBtnPrimary.onClick.RemoveAllListeners();
            _footBtnSecondary.onClick.RemoveAllListeners();

            bool canUp = _sim.GetNextUpgradeCosts(n) != null && n.level < n.maxLevel;

            if (n.kind == NodeKind.RiverDock)
            {
                _footBtnPrimary.GetComponent<RectTransform>().anchoredPosition = new Vector2(canUp ? -50f : 0f, -92f);
                _footIconPrimary.sprite = ForestComicUiSkin.GetArt("ico_fish_cast");
                _footLblPrimary.text = "甩杆钓鱼";
                _footBtnPrimary.onClick.AddListener(EnterFishingMode);
                if (canUp) BindSecondaryUpgradeFootBtn(n);
            }
            else if (n.kind == NodeKind.RelicTruck || n.kind == NodeKind.RelicRoadGate)
            {
                _footBtnPrimary.GetComponent<RectTransform>().anchoredPosition = new Vector2(canUp ? -50f : 0f, -92f);
                _footIconPrimary.sprite = ForestComicUiSkin.GetArt("ico_wheel_go");
                _footLblPrimary.text = "进城搜刮";
                _footBtnPrimary.onClick.AddListener(OpenCityDistrictSelector);
                if (canUp) BindSecondaryUpgradeFootBtn(n);
            }
            else if (n.kind == NodeKind.Shelter)
            {
                _footBtnPrimary.GetComponent<RectTransform>().anchoredPosition = new Vector2(canUp ? -52f : 0f, -92f);
                _footIconPrimary.sprite = ForestComicUiSkin.GetArt("dock_heroes");
                _footLblPrimary.text = "骨干委任";
                _footBtnPrimary.onClick.AddListener(OpenHeroAssignModal);
                if (canUp) BindSecondaryUpgradeFootBtn(n);
            }
            else if (n.kind == NodeKind.Warehouse)
            {
                _footBtnPrimary.GetComponent<RectTransform>().anchoredPosition = new Vector2(canUp ? -52f : 0f, -92f);
                _footIconPrimary.sprite = ForestComicUiSkin.GetArt("dock_bag");
                _footLblPrimary.text = "查看库存";
                _footBtnPrimary.onClick.AddListener(() => OpenQuickBuildList(showWarehouse: true));
                if (canUp) BindSecondaryUpgradeFootBtn(n);
            }
            else
            {
                _footBtnPrimary.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -92f);
                _footIconPrimary.sprite = ForestComicUiSkin.GetArt("ico_hammer");
                _footLblPrimary.text = canUp ? "升级建筑" : "已满级";
                _footBtnPrimary.onClick.AddListener(() =>
                {
                    if (canUp) OpenBuildConfirmCard(n);
                    else CloseAllModals();
                });
            }

            _footMenuRt.gameObject.SetActive(true);
        }

        void BindSecondaryUpgradeFootBtn(CampNode n)
        {
            _footBtnSecondary.gameObject.SetActive(true);
            _footBtnSecondary.GetComponent<RectTransform>().anchoredPosition = new Vector2(56f, -92f);
            _footIconSecondary.sprite = ForestComicUiSkin.GetArt("ico_hammer");
            _footLblSecondary.text = "升级";
            _footBtnSecondary.onClick.AddListener(() => OpenBuildConfirmCard(n));
        }

        public void OpenBuildConfirmCard(CampNode n)
        {
            if (n == null) return;
            CloseAllModals();
            _pendingBuildNode = n;

            bool isUpgrade = n.state == NodeState.Active;
            string nextTitle = !isUpgrade ? n.ruinTitle : $"{n.activeTitle}  Lv.{n.level} → Lv.{n.level + 1}";

            _bcIllustImg.sprite = ForestComicUiSkin.NodePreviewSprite(n);
            _bcTitleTxt.text = nextTitle;
            _bcDescTxt.text = n.desc;
            _bcBenefitIcon.sprite = ForestComicUiSkin.ResIcon(n.benefitRes);
            _bcBenefitNameTxt.text = string.IsNullOrEmpty(n.benefitLabel) ? "营地综合实力" : n.benefitLabel;

            ComputeBenefitPreview(n, isUpgrade, out int fromVal, out int toVal);
            _bcBenefitFromTxt.text = $"+{fromVal}";
            _bcBenefitToTxt.text = $"+{toVal}";

            List<BuildCondition> conds = _sim.GetUpgradeConditions(n, out int reqPlayerLv);
            for (int i = 0; i < 5; i++)
            {
                bool has = i < conds.Count;
                _bcCostRows[i].SetActive(has);
                if (!has) continue;

                BuildCondition c = conds[i];
                BindBuildConditionRow(i, c);
            }

            bool lvLocked = _sim.CampLevel < reqPlayerLv;
            _bcLvGateRedTxt.gameObject.SetActive(lvLocked);
            _bcTimeTxt.gameObject.SetActive(!lvLocked);
            _bcConfirmBtn.gameObject.SetActive(!lvLocked);

            if (lvLocked)
            {
                _bcLvGateRedTxt.text = $"玩家星级达到 {reqPlayerLv} 级后解锁建筑等级上限";
            }
            else
            {
                bool canAll = _sim.CanAffordUpgradeAll(n);
                _bcTimeTxt.text = $"⏱ {Mathf.CeilToInt(n.buildDuration * _sim.BuildTimeMultiplier)}秒";
                _bcBtnLbl.text = isUpgrade ? "升级建筑" : "建造";
                _bcConfirmBtn.GetComponent<Image>().sprite = canAll ? ForestComicUiSkin.BtnBlue : ForestComicUiSkin.BtnGrey;
            }

            _buildCardModal.SetActive(true);
        }

        void ComputeBenefitPreview(CampNode n, bool isUpgrade, out int fromVal, out int toVal)
        {
            if (n.kind == NodeKind.HeatTower)
            {
                int[] caps = { 0, 8, 15, 26, 38 };
                int cur = isUpgrade ? n.level : 0;
                fromVal = caps[Mathf.Clamp(cur, 0, 4)];
                toVal = caps[Mathf.Clamp(cur + 1, 1, 4)];
                return;
            }
            if (n.kind == NodeKind.Dwelling)
            {
                int cur = isUpgrade ? n.level : 0;
                fromVal = ForestSurvivalSim.GetDwellingPopForLevel(n.id, cur);
                toVal = ForestSurvivalSim.GetDwellingPopForLevel(n.id, cur + 1);
                return;
            }
            if (n.kind == NodeKind.Warehouse)
            {
                int[] caps = { 0, 80, 180, 360, 700, 1200 };
                int cur = isUpgrade ? n.level : 0;
                fromVal = caps[Mathf.Clamp(cur, 0, 5)];
                toVal = caps[Mathf.Clamp(cur + 1, 1, 5)];
                return;
            }
            if (ForestSurvivalSim.IsWorkshopKind(n.kind))
            {
                fromVal = isUpgrade ? Mathf.Clamp(2 + n.level, 3, 5) : 0;
                toVal = Mathf.Clamp(2 + (isUpgrade ? n.level + 1 : 1), 3, 5);
                return;
            }
            fromVal = isUpgrade ? n.benefitTo : n.benefitFrom;
            toVal = isUpgrade ? Mathf.RoundToInt(n.benefitTo * 1.6f + 4) : n.benefitTo;
        }

        void BindBuildConditionRow(int idx, BuildCondition c)
        {
            _bcCostIconBtns[idx].onClick.RemoveAllListeners();
            _bcJumpBtns[idx].onClick.RemoveAllListeners();

            bool met = c.met;
            _bcCostTxts[idx].color = met ? ForestUiWidgets.InkDark : ForestUiWidgets.AlertRed;
            _bcCostChecks[idx].sprite = met ? ForestComicUiSkin.GetArt("ico_check") : ForestComicUiSkin.CrossBadge;
            _bcCostChecks[idx].color = Color.white;
            _bcCostChecks[idx].rectTransform.anchoredPosition = new Vector2(met ? 210f : 86f, 0f);
            _bcJumpBtns[idx].gameObject.SetActive(!met);

            if (c.kind == CondKind.PopCur)
            {
                _bcCostIcons[idx].sprite = ForestComicUiSkin.GetArt("bust_cook", ForestComicUiSkin.RedDotBadge);
                _bcCostTxts[idx].text = $"当前人口  {c.own}/{c.need}";
                _bcJumpLbls[idx].text = "前往";
                _bcJumpBtns[idx].onClick.AddListener(JumpToBestDwelling);
                _bcCostIconBtns[idx].onClick.AddListener(() => OpenSourceRouteForCondition(CondKind.PopCur, SurvRes.Wood));
            }
            else if (c.kind == CondKind.PopCap)
            {
                _bcCostIcons[idx].sprite = ForestComicUiSkin.GetArt("campfire", ForestComicUiSkin.RedDotBadge);
                _bcCostTxts[idx].text = $"人口上限  {c.own}/{c.need}";
                _bcJumpLbls[idx].text = "前往";
                _bcJumpBtns[idx].onClick.AddListener(JumpToHeatTower);
                _bcCostIconBtns[idx].onClick.AddListener(() => OpenSourceRouteForCondition(CondKind.PopCap, SurvRes.Wood));
            }
            else if (c.kind == CondKind.Coins)
            {
                _bcCostIcons[idx].sprite = ForestComicUiSkin.GetArt("ico_coin", ForestComicUiSkin.RedDotBadge);
                _bcCostTxts[idx].text = $"{c.own}/{c.need}";
                _bcJumpLbls[idx].text = "获取";
                _bcJumpBtns[idx].onClick.AddListener(() => OpenSourceRouteForCondition(CondKind.Coins, SurvRes.Wood));
                _bcCostIconBtns[idx].onClick.AddListener(() => OpenSourceRouteForCondition(CondKind.Coins, SurvRes.Wood));
            }
            else
            {
                SurvRes r = c.res;
                _bcCostIcons[idx].sprite = ForestComicUiSkin.ResIcon(r);
                _bcCostTxts[idx].text = $"{ForestSurvivalSim.ResName(r)}  {c.own}/{c.need}";

                _sim.ResolveResourceProducer(r, out _, out _, out bool isCraft);
                if (r == SurvRes.Blueprint)
                {
                    _bcJumpLbls[idx].text = "获取";
                    _bcJumpBtns[idx].onClick.AddListener(() => OpenSourceRouteForCondition(CondKind.Resource, SurvRes.Blueprint));
                    _bcCostIconBtns[idx].onClick.AddListener(() => OpenSourceRouteForCondition(CondKind.Resource, SurvRes.Blueprint));
                }
                else
                {
                    _bcJumpLbls[idx].text = isCraft ? "去生产" : "前往";
                    _bcJumpBtns[idx].onClick.AddListener(() => JumpToProduceOrGather(r));
                    _bcCostIconBtns[idx].onClick.AddListener(() => OpenItemSourceTip(r));
                }
            }
        }

        public void OpenItemSourceTip(SurvRes r)
        {
            if (r == SurvRes.Blueprint)
            {
                OpenSourceRouteForCondition(CondKind.Resource, SurvRes.Blueprint);
                return;
            }
            string desc = _sim.ResolveResourceProducer(r, out _, out _, out _);
            _itIcon.sprite = ForestComicUiSkin.ResIcon(r);
            _itNameTxt.text = ForestSurvivalSim.ResName(r);
            _itOwnTxt.text = $"拥有：{_sim.Get(r)}";
            _itSourceTxt.text = desc;
            _itGoAction = () => JumpToProduceOrGather(r);
            _itemTipPopup.SetActive(true);
        }

        public void OpenSourceRouteForCondition(CondKind kind, SurvRes res)
        {
            if (kind == CondKind.Coins)
            {
                _srIcon.sprite = ForestComicUiSkin.GetArt("ico_coin", ForestComicUiSkin.RedDotBadge);
                _srNameTxt.text = "营地铜币";
                _srOwnTxt.text = $"当前拥有：{_sim.Coins}";
                ConfigureRouteRow(0, ForestComicUiSkin.GetArt("bust_laochentou", ForestComicUiSkin.GetArt("bust_cook")), "居民生计订单（主力）", true, "", OpenResidentDemandDrawer);
                ConfigureRouteRow(1, ForestComicUiSkin.GetArt("dock_radio"), "山口行商大宗贸易", _sim.CampLevel >= 6, "6级开启", OpenCaravanModal);
                ConfigureRouteRow(2, ForestComicUiSkin.GetArt("ico_clipboard"), "完成左下主线目标领奖", true, "", () => { CloseAllModals(); OnQuestCardClicked(); });
            }
            else if (kind == CondKind.PopCur)
            {
                _srIcon.sprite = ForestComicUiSkin.GetArt("bust_cook", ForestComicUiSkin.RedDotBadge);
                _srNameTxt.text = "当前入驻人口";
                _srOwnTxt.text = $"当前人口：{_sim.CurrentPop} / 上限 {_sim.PopCap}";
                ConfigureRouteRow(0, ForestComicUiSkin.GetArt("refugee_cabin", ForestComicUiSkin.GetArt("tent")), "升级【1号避风民居】", true, "", JumpToBestDwelling);
                ConfigureRouteRow(1, ForestComicUiSkin.GetArt("refugee_cabin", ForestComicUiSkin.GetArt("shelter_lv2")), "建造【2号原木民居】", _sim.CampLevel >= 4, "4级开启", () => JumpToNodeById("house_2"));
                ConfigureRouteRow(2, ForestComicUiSkin.GetArt("heating_tower", ForestComicUiSkin.GetArt("campfire")), "升级【篝火供暖塔】扩上限", true, "", JumpToHeatTower);
            }
            else if (kind == CondKind.PopCap)
            {
                _srIcon.sprite = ForestComicUiSkin.GetArt("heating_tower", ForestComicUiSkin.RedDotBadge);
                _srNameTxt.text = "供暖人口上限";
                _srOwnTxt.text = $"当前上限：{_sim.PopCap}";
                ConfigureRouteRow(0, ForestComicUiSkin.GetArt("heating_tower", ForestComicUiSkin.GetArt("campfire")), "升级【篝火供暖塔】（核心）", true, "", JumpToHeatTower);
                ConfigureRouteRow(1, ForestComicUiSkin.GetArt("shelter_lv2"), "升级【避难所议事厅】", _sim.CampLevel >= 3, "3级开启", () => JumpToNodeById("shelter"));
                _srRows[2].SetActive(false);
                _sourceRouteModal.SetActive(true);
                return;
            }
            else // 工程手稿 Blueprint
            {
                _srIcon.sprite = ForestComicUiSkin.ResIcon(SurvRes.Blueprint);
                _srNameTxt.text = "工程手稿";
                _srOwnTxt.text = $"当前拥有：{_sim.Get(SurvRes.Blueprint)}";
                ConfigureRouteRow(0, ForestComicUiSkin.GetArt("dock_map"), "丧尸城开车搜刮（主力）", _sim.CampLevel >= 5, "5级开启", () => { CloseAllModals(); OpenCityDistrictSelector(); });
                ConfigureRouteRow(1, ForestComicUiSkin.GetArt("dock_radio"), "山口行商驿站兑换", _sim.CampLevel >= 6, "6级开启", OpenCaravanModal);
                ConfigureRouteRow(2, ForestComicUiSkin.GetArt("bust_cook"), "居民订单满5单宝箱 & 升星奖励", true, "", OpenResidentDemandDrawer);
            }

            _srRows[2].SetActive(true);
            _sourceRouteModal.SetActive(true);
        }

        void ConfigureRouteRow(int idx, Sprite ico, string title, bool unlocked, string lockText, Action onGo)
        {
            _srRows[idx].SetActive(true);
            _srRowIcons[idx].sprite = ico;
            _srRowTitles[idx].text = title;
            _srRowBtns[idx].gameObject.SetActive(unlocked);
            _srRowLockTxts[idx].gameObject.SetActive(!unlocked);
            _srRowLockTxts[idx].text = lockText;
            _srRowActions[idx] = onGo;
        }

        void JumpToBestDwelling()
        {
            CloseAllModals();
            string[] ids = { "house_1", "house_2", "house_3" };
            for (int i = 0; i < ids.Length; i++)
            {
                CampNode h = _sim.Find(ids[i]);
                if (h == null || h.state == NodeState.HiddenInForest) continue;
                if (h.state == NodeState.Discovered || h.level < h.maxLevel)
                {
                    SmoothFocusGrid(new Vector2(h.gx, h.gy));
                    OpenBuildConfirmCard(h);
                    return;
                }
            }
        }

        void JumpToHeatTower() => JumpToNodeById("heat_tower");

        void JumpToNodeById(string id)
        {
            CloseAllModals();
            CampNode n = _sim.Find(id);
            if (n == null) return;
            if (n.state == NodeState.HiddenInForest && _sim.CampLevel >= n.unlockPlayerLv)
            {
                n.state = NodeState.Discovered;
                _world.RefreshNodeVisual(n);
            }
            SmoothFocusGrid(new Vector2(n.gx, n.gy));
            if (n.state != NodeState.HiddenInForest)
                OpenBuildConfirmCard(n);
            else
                SpawnWorldRisingText($"需主角升至 {n.unlockPlayerLv} 级并砍开周边树林解锁【{n.ruinTitle}】", new Vector2(n.gx, n.gy));
        }

        public void JumpToProduceOrGather(SurvRes r)
        {
            _sim.ResolveResourceProducer(r, out string nodeId, out int recipeIdx, out bool isCraft);
            CloseAllModals();

            if (string.IsNullOrEmpty(nodeId))
            {
                SmoothFocusGrid(new Vector2(18.6f, 19.8f));
                SpawnWorldRisingText("请点击营地林缘树木或散落原木堆采集！", new Vector2(18.6f, 19.8f));
                return;
            }

            CampNode target = _sim.Find(nodeId);
            if (target == null) return;

            if (target.state == NodeState.HiddenInForest && _sim.CampLevel >= target.unlockPlayerLv)
            {
                target.state = NodeState.Discovered;
                _world.RefreshNodeVisual(target);
            }

            if (target.state == NodeState.HiddenInForest)
            {
                SmoothFocusGrid(new Vector2(target.gx, target.gy));
                SpawnWorldRisingText($"星级达到 Lv.{target.unlockPlayerLv} 并开拓该处林地即可建造【{target.ruinTitle}】！", new Vector2(target.gx, target.gy));
                return;
            }

            if (target.state != NodeState.Active)
            {
                SmoothFocusGrid(new Vector2(target.gx, target.gy));
                OpenBuildConfirmCard(target);
                return;
            }

            if (isCraft && ForestSurvivalSim.IsWorkshopKind(target.kind))
            {
                OpenWorkshopDrawer(target, recipeIdx);
            }
            else if (target.kind == NodeKind.RiverDock)
            {
                EnterFishingMode();
            }
            else if (target.kind == NodeKind.RelicTruck)
            {
                OpenCityDistrictSelector();
            }
            else
            {
                SmoothFocusGrid(new Vector2(target.gx, target.gy));
                _sim.TryCollectOrInteractNode(target, out string msg);
                if (!string.IsNullOrEmpty(msg))
                    SpawnWorldRisingText(msg, new Vector2(target.gx, target.gy));
            }
        }

        void OnConfirmBuildCardClicked()
        {
            if (_pendingBuildNode == null) return;
            CampNode target = _pendingBuildNode;
            if (_sim.StartBuildOrUpgrade(target, out string msg))
            {
                CloseAllModals();
                SmoothFocusGrid(new Vector2(target.gx, target.gy));
            }
            else
            {
                SpawnWorldRisingText(msg, new Vector2(target.gx, target.gy));
            }
        }

        public void OpenWorkshopDrawer(CampNode workshop, int selectRecipe = 0)
        {
            if (workshop == null) return;
            CloseAllModals();
            _activeWorkshop = workshop;
            WorkshopRecipe[] list = _sim.GetWorkshopRecipes(workshop);
            _selectedRecipeIdx = Mathf.Clamp(selectRecipe, 0, Mathf.Max(0, list.Length - 1));

            Vector3 iso = ForestSliceView.Iso(workshop.gx, workshop.gy);
            _panTarget = new Vector3(iso.x, iso.y - _cam.orthographicSize * 0.42f, -20f);
            _panChangeOrtho = false;
            _panning = true;

            _workshopDrawer.SetActive(true);
            RefreshWorkshopDrawer();
        }

        void OnWorkshopProduceClicked()
        {
            if (_activeWorkshop == null) return;
            WorkshopRecipe[] list = _sim.GetWorkshopRecipes(_activeWorkshop);
            if (_selectedRecipeIdx < 0 || _selectedRecipeIdx >= list.Length) return;
            if (!_sim.TryEnqueueRecipe(_activeWorkshop, list[_selectedRecipeIdx], out string msg))
                SpawnWorldRisingText(msg, new Vector2(_activeWorkshop.gx, _activeWorkshop.gy));
            RefreshWorkshopDrawer();
        }

        public void OpenResidentDemandDrawer()
        {
            _sim.EnsureActiveOrders();
            CloseAllModals();
            _residentDrawer.SetActive(true);
            RefreshResidentDrawer();
        }

        void OnOrderSlotActionClicked(int slot)
        {
            if (slot < 0 || slot >= _sim.ActiveOrders.Count) return;
            ResidentDemand d = _sim.ActiveOrders[slot];

            if (_sim.CanSatisfyOrder(slot))
            {
                int wIdx = Mathf.Clamp(d.walkerIndex, 0, Mathf.Max(0, _world.Walkers.Count - 1));
                Vector2 g = wIdx < _world.Walkers.Count
                    ? new Vector2(_world.Walkers[wIdx].gx, _world.Walkers[wIdx].gy)
                    : new Vector2(16.4f, 16.2f);
                if (_sim.TrySatisfyOrder(slot, g, out string thanks))
                {
                    if (wIdx < _world.Walkers.Count)
                    {
                        _world.Walkers[wIdx].chatterText = thanks;
                        _world.Walkers[wIdx].chatterTimer = 3.8f;
                    }
                    RefreshResidentDrawer();
                }
            }
            else
            {
                // 缺少订单物品时：一键直接跳往对应作坊生产该物品！
                SurvRes missing = _sim.Get(d.res1) < d.amt1 ? d.res1 : d.res2;
                JumpToProduceOrGather(missing);
            }
        }

        public void OpenCaravanModal()
        {
            CloseAllModals();
            if (!_sim.RadioRepaired)
            {
                JumpToNodeById("radio_post");
                return;
            }
            RefreshCaravanModal();
            _caravanModal.SetActive(true);
        }

        void OnCaravanTradeClicked(int idx)
        {
            if (idx < 0 || idx >= _sim.CaravanOffers.Count) return;
            CaravanOffer o = _sim.CaravanOffers[idx];
            if (!o.traded && _sim.Get(o.giveRes) < o.giveAmt)
            {
                JumpToProduceOrGather(o.giveRes);
                return;
            }
            if (_sim.TryTradeCaravanOffer(idx, out string msg))
                RefreshCaravanModal();
            SpawnWorldRisingText(msg, new Vector2(22.6f, 21.2f));
        }

        public void OpenCrisisPoster()
        {
            CloseAllModals();
            _sim.HordePosterShown = true;
            _crisisModal.SetActive(true);
            RefreshCrisisPoster();
        }

        void ShowLevelUpModal(int newLv, string[] unlockedNames)
        {
            CloseAllModals();
            _lvUpNumTxt.text = $"{newLv}级";
            string[] iconKeys = newLv == 2 ? new[] { "well", "farm_2", "rec_stew" } :
                                newLv == 3 ? new[] { "ruin_generator", "tent", "fishing_dock" } :
                                newLv == 4 ? new[] { "shelter_lv2", "hunter_tower", "ico_plank" } :
                                             new[] { "truck_active", "generator_active", "dock_radio" };
            for (int i = 0; i < 3; i++)
            {
                bool show = unlockedNames != null && i < unlockedNames.Length;
                _lvUpUnlockCards[i].SetActive(show);
                if (!show) continue;
                _lvUpUnlockIcons[i].sprite = ForestComicUiSkin.GetArt(i < iconKeys.Length ? iconKeys[i] : "ico_xp_star", ForestComicUiSkin.ResIcon(SurvRes.Blueprint));
                _lvUpUnlockNames[i].text = unlockedNames[i];
            }
            _levelUpModal.SetActive(true);
        }

        void ShowHeroSpotlight(HeroSpec h)
        {
            if (h == null) return;
            CloseAllModals();
            _heroSpotBust.sprite = ForestComicUiSkin.GetArt(h.portraitKey, ForestComicUiSkin.GetArt("bust_driver"));
            _heroSpotNameTxt.text = h.name;
            _heroSpotRoleTxt.text = h.roleTitle;
            _heroSpotBuffTxt.text = h.buffDesc;
            _heroSpotlightModal.SetActive(true);
        }

        public void OpenHeroAssignModal()
        {
            CloseAllModals();
            RefreshHeroAssignModal();
            _heroAssignModal.SetActive(true);
        }

        void OnHeroSkinTierClicked(int rowIdx, OutfitTier tier)
        {
            if (rowIdx < 0 || rowIdx >= _sim.Heroes.Count) return;
            HeroSpec h = _sim.Heroes[rowIdx];
            if (_sim.SetHeroSkinTier(h.id, tier, out string msg))
                RefreshHeroAssignModal();
            SpawnWorldRisingText(msg, new Vector2(16.4f, 16.2f));
        }

        void RefreshHeroAssignModal()
        {
            OutfitTier maxTier = _sim.MaxUnlockedSkinTier;
            for (int i = 0; i < 5; i++)
            {
                bool show = i < _sim.Heroes.Count;
                if (_haRows[i] != null) _haRows[i].SetActive(show);
                if (!show) continue;

                HeroSpec h = _sim.Heroes[i];
                _haBusts[i].sprite = ForestComicUiSkin.GetArt(h.portraitKey);
                _haBusts[i].color = h.unlocked ? Color.white : new Color(0.25f, 0.25f, 0.25f, 0.85f);
                _haNames[i].text = $"{h.name}  ·  驻守：{h.targetBuildingTitle}";
                _haDescs[i].text = h.buffDesc;
                _haStates[i].text = !h.unlocked ? "未招募" : (h.assigned ? "✔ 驻守中" : "待委任");
                _haStates[i].color = !h.unlocked ? ForestUiWidgets.InkMuted : (h.assigned ? ForestUiWidgets.LushGreen : ForestUiWidgets.AlertRed);

                PuppetRigProfile prof = PuppetRegistry.GetProfile(h.characterId);
                for (int t = 0; t < 3; t++)
                {
                    OutfitTier tier = (OutfitTier)t;
                    OutfitTierDef tdef = prof.GetTier(tier);
                    bool unlocked = h.unlocked && tier <= maxTier;
                    bool equipped = h.skinTier == tier;
                    string baseName = tdef != null ? tdef.tierName : tier.ToString();

                    _haSkinBtns[i, t].GetComponent<Image>().sprite =
                        equipped ? ForestComicUiSkin.BtnGreen : (unlocked ? ForestComicUiSkin.BtnBlue : ForestComicUiSkin.BtnGrey);
                    _haSkinTexts[i, t].text = equipped ? $"已穿·{baseName}" : (unlocked ? baseName : $"裁缝解锁·{baseName}");
                }
            }
        }

        // ---------------- 丧尸城选点与虚拟摇杆开车搜刮 ----------------
        public void OpenCityDistrictSelector()
        {
            CloseAllModals();
            if (!_sim.CanLaunchCityRun)
            {
                string tid = !_sim.TruckRepaired ? "relic_truck" : "relic_road_gate";
                JumpToNodeById(tid);
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                CitySiteDef s = _sim.CitySites[i];
                bool lvOk = _sim.ShelterLevel >= s.requireShelterLv;
                _csSiteBtns[i].GetComponent<Image>().sprite = lvOk ? ForestComicUiSkin.BtnOrange : ForestComicUiSkin.BtnGrey;
                _csSiteBtnLbls[i].text = lvOk ? $"发车(粮×{s.rationCost})" : $"需中枢Lv.{s.requireShelterLv}";
            }
            _citySelectModal.SetActive(true);
        }

        public void LaunchCityRunForSite(int siteIdx)
        {
            CitySiteDef s = _sim.CitySites[Mathf.Clamp(siteIdx, 0, _sim.CitySites.Length - 1)];
            if (_sim.ShelterLevel < s.requireShelterLv)
            {
                SpawnWorldRisingText($"需避难所议事厅升至 Lv.{s.requireShelterLv} 才可进入该高危区", new Vector2(16.8f, 20.5f));
                return;
            }
            if (_sim.Get(SurvRes.Ration) >= s.rationCost)
                _sim.Add(SurvRes.Ration, -s.rationCost);

            CloseAllModals();
            _mainHudLayer.SetActive(false);
            _worldTrackLayer.SetActive(false);
            _cityDriveHud.SetActive(true);
            _joyBaseRt.anchoredPosition = new Vector2(0f, -260f);
            _joyKnobRt.anchoredPosition = Vector2.zero;
            _cityArena.StartRun(s);
        }

        void ShowCityLootSettlement()
        {
            var pairs = new List<KeyValuePair<SurvRes, int>>(_cityArena.SessionGains);
            pairs.Sort((a, b) => (b.Key == SurvRes.Blueprint ? 1000 : b.Value).CompareTo(a.Key == SurvRes.Blueprint ? 1000 : a.Value));
            for (int i = 0; i < 5; i++)
            {
                bool show = i < pairs.Count;
                _lsCards[i].SetActive(show);
                if (!show) continue;
                _lsCardIcons[i].sprite = ForestComicUiSkin.ResIcon(pairs[i].Key);
                _lsCardTxts[i].text = $"{ForestSurvivalSim.ResName(pairs[i].Key)} +{pairs[i].Value}";
            }
            _lootSettleModal.SetActive(true);
        }

        // ---------------- 南岸溪流推镜甩杆钓鱼 ----------------
        public void EnterFishingMode()
        {
            CloseAllModals();
            _inFishingMode = true;
            _fishPhase = 0;
            _fishTension = 0f;
            _fishAuto = false;
            _panning = false;

            _savedPreFishCamPos = _cam.transform.position;
            _savedPreFishOrtho = _cam.orthographicSize;

            Vector3 focus = ForestSliceView.Iso(12.05f, 4.05f);
            _cam.transform.position = new Vector3(focus.x, focus.y, -20f);
            _cam.orthographicSize = 3.1f;

            _mainHudLayer.SetActive(false);
            _worldTrackLayer.SetActive(false);
            _world.SetFishingStageActive(true);
            _fishingHud.SetActive(true);
            SyncFishingUiState();
        }

        public void ExitFishingMode()
        {
            if (!_inFishingMode) return;
            _inFishingMode = false;
            _world.SetFishingStageActive(false);
            _fishingHud.SetActive(false);
            _mainHudLayer.SetActive(true);
            _worldTrackLayer.SetActive(true);

            _cam.transform.position = _savedPreFishCamPos;
            _cam.orthographicSize = _savedPreFishOrtho;
        }

        void OnFishingMainBtnClicked()
        {
            if (_fishPhase == 0)
            {
                _fishPhase = 1;
                _fishWaitTimer = 1.15f;
                _world.CastFishingBobber();
                SyncFishingUiState();
            }
            else if (_fishPhase == 2)
            {
                _fishTension += 0.30f;
                if (_fishTension >= 1f)
                    CompleteOneFishCatch();
            }
        }

        void CompleteOneFishCatch()
        {
            _world.PlayCatchFishLeap();
            bool archer = _sim.FindHero("linyan")?.assigned == true;
            int fish = archer ? 4 : 2;
            _sim.Add(SurvRes.Fish, fish, notifyToast: true);
            _sim.FishCaughtTotal++;
            _sim.AddXp(16, new Vector2(12.4f, 4.3f));

            _fishPhase = 0;
            _fishTension = 0f;
            SyncFishingUiState();
        }

        void SyncFishingUiState()
        {
            _fishTaskTopTxt.text = $"已钓山溪鱼: {_sim.FishCaughtTotal} 条";
            _fishArcRoot.SetActive(_fishPhase == 2);
            _fishFastTapPill.SetActive(_fishPhase == 2);
            _fishBtnIcon.sprite = ForestComicUiSkin.GetArt("ico_fish_cast");
            _fishBtnMainLbl.text = _fishPhase == 0 ? "甩杆" : (_fishPhase == 1 ? "等待咬钩…" : "收线");
            _fishAutoLbl.text = _fishAuto ? "自动中:开" : "自动垂钓";
        }

        // ---------------- 底部 6 宫格 Dock 与主线任务点击 ----------------
        void OnQuestCardClicked()
        {
            var q = _sim.GetCurrentQuest();
            if (q.done)
            {
                if (_sim.ClaimCurrentQuestReward(out string rw))
                    SpawnWorldRisingText(rw, q.focusGrid);
                return;
            }

            if (q.step == 3 && string.IsNullOrEmpty(q.targetNodeId))
            {
                OpenResidentDemandDrawer();
                return;
            }
            if (q.step == 9 && _sim.CanLaunchCityRun)
            {
                OpenCityDistrictSelector();
                return;
            }

            if (!string.IsNullOrEmpty(q.targetNodeId))
            {
                CampNode tn = _sim.Find(q.targetNodeId);
                if (tn != null)
                {
                    if (tn.state == NodeState.HiddenInForest && _sim.CampLevel >= tn.unlockPlayerLv)
                    {
                        tn.state = NodeState.Discovered;
                        _world.RefreshNodeVisual(tn);
                    }
                    SmoothFocusGrid(new Vector2(tn.gx, tn.gy));
                    if (tn.state == NodeState.Discovered || (tn.state == NodeState.Active && q.step != 2 && q.step != 3))
                        OpenBuildConfirmCard(tn);
                    else if (tn.state == NodeState.Active && ForestSurvivalSim.IsWorkshopKind(tn.kind))
                        OpenWorkshopDrawer(tn, 0);
                    return;
                }
            }

            SmoothFocusGrid(q.focusGrid);
        }

        void OnBottomDockClicked(int tabIdx)
        {
            switch (tabIdx)
            {
                case 0: OpenCrisisPoster(); break;
                case 1: OpenQuickBuildList(showWarehouse: false); break;
                case 2: OpenHeroAssignModal(); break;
                case 3: OpenQuickBuildList(showWarehouse: true); break;
                case 4: OpenCaravanModal(); break;
                case 5: OpenCityDistrictSelector(); break;
            }
        }

        public void OpenQuickBuildList(bool showWarehouse)
        {
            CloseAllModals();
            for (int i = 0; i < _qlSpawnedRows.Count; i++) SafeDestroy(_qlSpawnedRows[i]);
            _qlSpawnedRows.Clear();

            _qlUpgradeWhBtn.gameObject.SetActive(showWarehouse);

            if (showWarehouse)
            {
                _qlTitleTxt.text = "营地物资与产成品总仓库";
                _qlSubCapTxt.text = $"单品储量上限：{_sim.StorageCap}（升级【物资仓库】可扩容）  ·  工程手稿：{_sim.Get(SurvRes.Blueprint)}";

                SurvRes[] shownItems =
                {
                    SurvRes.Wood, SurvRes.Scrap, SurvRes.Water, SurvRes.Veggie, SurvRes.Cloth, SurvRes.Herb,
                    SurvRes.Fish, SurvRes.Meat, SurvRes.Pelt, SurvRes.Plank, SurvRes.Nails, SurvRes.WoodBed,
                    SurvRes.IronBeam, SurvRes.IronIngot, SurvRes.MetalPipe, SurvRes.LinenShirt, SurvRes.LeatherBoots, SurvRes.FurCoat,
                    SurvRes.PlateArmor, SurvRes.RoastVeggie, SurvRes.VeggieSoup, SurvRes.GrillFish, SurvRes.SmokedMeat, SurvRes.HerbPoultice
                };

                for (int i = 0; i < shownItems.Length; i++)
                {
                    SurvRes r = shownItems[i];
                    int col = i % 3;
                    int row = i / 3;
                    float x = -190f + col * 190f;
                    float y = -12f - row * 82f;

                    Image cell = ForestUiWidgets.MakeSlicedImage("Stock_" + r, _qlContentRt, ForestComicUiSkin.SubSlot, Color.white,
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(180f, 74f), true);
                    _qlSpawnedRows.Add(cell.gameObject);
                    SurvRes capR = r;
                    cell.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenItemSourceTip(capR));

                    ForestUiWidgets.MakeIcon("Ico", cell.transform, ForestComicUiSkin.ResIcon(r), new Vector2(-58f, 0f), new Vector2(44f, 44f));
                    ForestUiWidgets.MakeText("Name", cell.transform, ForestSurvivalSim.ResName(r), 16, ForestUiWidgets.InkDark, TextAnchor.UpperLeft,
                        Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(22f, -8f), new Vector2(-54f, -10f), outline: false);
                    int amt = _sim.Get(r);
                    ForestUiWidgets.MakeText("Cnt", cell.transform, $"{amt}/{_sim.StorageCap}", 15, amt >= _sim.StorageCap ? ForestUiWidgets.AlertRed : ForestUiWidgets.LushGreen, TextAnchor.LowerLeft,
                        Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(22f, 8f), new Vector2(-54f, -10f), outline: false);
                }
            }
            else
            {
                _qlTitleTxt.text = "营地建筑解锁与升阶总览";
                _qlSubCapTxt.text = $"当前星级：★{_sim.CampLevel}   ·   人口：{_sim.CurrentPop}/{_sim.PopCap}   ·   铜币：{_sim.Coins}";

                int shown = 0;
                for (int i = 0; i < _sim.Nodes.Count && shown < 8; i++)
                {
                    CampNode n = _sim.Nodes[i];
                    if (n.kind == NodeKind.RelicLoot || n.state == NodeState.Depleted) continue;
                    if (n.state == NodeState.Active && n.level >= n.maxLevel) continue;

                    CampNode capNode = n;
                    Image row = ForestUiWidgets.MakeSlicedImage("BuildRow_" + n.id, _qlContentRt, ForestComicUiSkin.SubSlot, Color.white,
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f - shown * 84f), new Vector2(564f, 76f), false);
                    _qlSpawnedRows.Add(row.gameObject);
                    ForestUiWidgets.MakeIcon("Ico", row.transform, ForestComicUiSkin.NodePreviewSprite(n), new Vector2(-238f, 0f), new Vector2(56f, 56f));

                    int reqLv = _sim.GetRequiredPlayerLvForNext(n);
                    bool lvLocked = _sim.CampLevel < reqLv;
                    string stateTag = n.state == NodeState.Active ? $"Lv.{n.level}→{n.level + 1}" : "待建造";
                    ForestUiWidgets.MakeText("Name", row.transform, $"{n.ruinTitle} ({stateTag})", 18, ForestUiWidgets.InkDark, TextAnchor.UpperLeft,
                        Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(20f, -8f), new Vector2(-216f, -14f), outline: false);

                    _sim.GetNextUpgradeSpec(n, out int cc, out _, out _, out CostPair[] costs);
                    string subLine = lvLocked ? $"★ 玩家星级 {reqLv} 级开放" : ForestSurvivalSim.FormatCosts(costs, cc);
                    ForestUiWidgets.MakeText("Cost", row.transform, subLine, 14, lvLocked ? ForestUiWidgets.AlertRed : ForestUiWidgets.InkMuted, TextAnchor.LowerLeft,
                        Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(20f, 8f), new Vector2(-216f, -14f), outline: false);

                    bool canNow = !lvLocked && _sim.CanAffordUpgradeAll(n);
                    ForestUiWidgets.MakeJellyButton("Go", row.transform, lvLocked ? ForestComicUiSkin.BtnGrey : (canNow ? ForestComicUiSkin.BtnGreen : ForestComicUiSkin.BtnBlue),
                        lvLocked ? $"{reqLv}级解锁" : "前往", 17, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(104f, 46f), () =>
                        {
                            JumpToNodeById(capNode.id);
                        }, out _);
                    shown++;
                }
            }
            _quickListModal.SetActive(true);
        }

        // ---------------- 漫画对话播放 ----------------
        public void EnqueueComicDialog(ComicLine[] lines)
        {
            if (lines == null || lines.Length == 0) return;
            for (int i = 0; i < lines.Length; i++)
                _dialogQueue.Enqueue(lines[i]);
            if (!_comicDialogOverlay.activeSelf)
                AdvanceComicDialog();
        }

        void AdvanceComicDialog()
        {
            if (_dialogQueue.Count == 0)
            {
                _comicDialogOverlay.SetActive(false);
                return;
            }
            ComicLine line = _dialogQueue.Dequeue();
            _cdLeftBust.sprite = ForestComicUiSkin.GetArt(line.leftBust, ForestComicUiSkin.GetArt("bust_leader"));
            bool hasRight = !string.IsNullOrEmpty(line.rightBust);
            _cdRightBust.gameObject.SetActive(hasRight);
            if (hasRight)
                _cdRightBust.sprite = ForestComicUiSkin.GetArt(line.rightBust, ForestComicUiSkin.GetArt("bust_leader"));

            _cdLeftBust.color = (hasRight && line.rightSpeaking) ? new Color(0.45f, 0.45f, 0.45f, 0.92f) : Color.white;
            _cdRightBust.color = line.rightSpeaking ? Color.white : new Color(0.45f, 0.45f, 0.45f, 0.92f);
            _cdLeftBust.rectTransform.localScale = (hasRight && line.rightSpeaking) ? new Vector3(0.92f, 0.92f, 1f) : new Vector3(1.08f, 1.08f, 1f);
            _cdRightBust.rectTransform.localScale = line.rightSpeaking ? new Vector3(1.08f, 1.08f, 1f) : new Vector3(0.92f, 0.92f, 1f);

            _cdSpeakerNameTxt.text = line.speaker;
            _cdLineBodyTxt.text = line.text;
            _comicDialogOverlay.SetActive(true);
        }

        // =====================================================================
        // 屏幕中央获得物资胶囊流 + 飞入资源栏 + 经验飞星 + 世界飘字
        // =====================================================================
        void EnqueueCenterPickupToast(SurvRes res, int amount)
        {
            if (amount <= 0) return;
            for (int i = 0; i < _pickupToasts.Count; i++)
                if (_pickupToasts[i].res == res && _pickupToasts[i].age < 0.25f) return;

            Image bg = ForestUiWidgets.MakeSlicedImage("PickupToast", _canvasRt, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 56f + _pickupToasts.Count * 40f), new Vector2(226f, 36f), false);
            var cg = bg.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            Image ico = ForestUiWidgets.MakeIcon("Ico", bg.transform, ForestComicUiSkin.ResIcon(res), new Vector2(-66f, 0f), new Vector2(28f, 28f));
            Text txt = ForestUiWidgets.MakeText("Txt", bg.transform, $"{ForestSurvivalSim.ResName(res)} x{amount}", 18, Color.white, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(22f, 0f), new Vector2(-64f, 0f));

            _pickupToasts.Add(new PickupToastItem { rt = bg.rectTransform, icon = ico, text = txt, cg = cg, res = res, age = 0f });
        }

        void SpawnFlyIconToHud(SurvRes res, Vector2 fromCanvas)
        {
            Vector2 toCanvas = new Vector2(0f, -580f); // 默认飞向底部仓库图标
            if (_cityArena != null && _cityArena.IsRunning)
                toCanvas = new Vector2(0f, 540f);
            else if (res == SurvRes.Blueprint)
                toCanvas = new Vector2(-50f, 582f);
            else if (_resPillRts.TryGetValue(res, out RectTransform pillRt) && pillRt.gameObject.activeSelf)
                toCanvas = new Vector2(-360f + pillRt.anchoredPosition.x + 28f, 640f + pillRt.anchoredPosition.y - 17f);

            Image fi = ForestUiWidgets.MakeIcon("FlyRes", _canvasRt, ForestComicUiSkin.ResIcon(res), fromCanvas, new Vector2(36f, 36f));
            _flyIcons.Add(new FlyIconItem { rt = fi.rectTransform, img = fi, from = fromCanvas, to = toCanvas, t = 0f, dur = 0.45f, targetRes = res });
        }

        void SpawnXpFlyStar(int xp, Vector2 worldGrid)
        {
            Vector2 from = WorldToCanvas(ForestSliceView.Iso(worldGrid.x, worldGrid.y) + new Vector3(0f, 0.45f, 0f));
            Vector2 to = new Vector2(-235f, 604f);
            Image star = ForestUiWidgets.MakeIcon("FlyXpStar", _canvasRt, ForestComicUiSkin.GetArt("ico_xp_star", ForestComicUiSkin.RedDotBadge), from, new Vector2(46f, 46f));
            ForestUiWidgets.MakeText("XpLbl", star.transform, $"+{xp}XP", 17, ForestUiWidgets.WarmGold, TextAnchor.LowerCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(90f, 24f));
            _flyIcons.Add(new FlyIconItem { rt = star.rectTransform, img = star, from = from, to = to, t = 0f, dur = 0.65f, targetRes = SurvRes.Wood });
        }

        void SpawnWorldRisingText(string msg, Vector2 worldGrid)
        {
            if (string.IsNullOrEmpty(msg)) return;
            Vector2 cp = WorldToCanvas(ForestSliceView.Iso(worldGrid.x, worldGrid.y) + new Vector3(0f, 0.55f, 0f));
            cp.x = Mathf.Clamp(cp.x, -220f, 220f);
            cp.y = Mathf.Clamp(cp.y, -380f, 440f);

            Image pill = ForestUiWidgets.MakeSlicedImage("WorldToast", _canvasRt, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), cp, new Vector2(430f, 40f), false);
            Text txt = ForestUiWidgets.MakeText("Txt", pill.transform, msg, 16, ForestUiWidgets.WarmGold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16f, 0f));
            _worldToasts.Add(new WorldTextToast { rt = pill.rectTransform, txt = txt, startPos = cp, age = 0f });
        }

        // =====================================================================
        // 每帧刷新同步
        // =====================================================================
        public void Tick(float dt)
        {
            if (_panning && !_inFishingMode && (_cityArena == null || !_cityArena.IsRunning))
            {
                _cam.transform.position = Vector3.Lerp(_cam.transform.position, _panTarget, dt * 7.5f);
                if (_panChangeOrtho)
                    _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _panTargetOrtho, dt * 6.5f);
                if (Vector2.Distance(_cam.transform.position, _panTarget) < 0.04f)
                    _panning = false;
            }

            if (_cityArena != null && _cityArena.IsRunning)
            {
                TickCityJoystickInput();
                _cityArena.Tick(dt);
                RefreshCityDriveHud();
                if (_lootSettleModal.activeSelf && _lsSunburst != null)
                    _lsSunburst.rectTransform.Rotate(0f, 0f, 28f * dt);
                TickToastsAndFlyers(dt);
                return;
            }

            if (_inFishingMode)
            {
                TickFishingMode(dt);
                TickToastsAndFlyers(dt);
                return;
            }

            RefreshMainHud(dt);
            RefreshWorldTrackBubbles();
            if (_footMenuNode != null && _footMenuRt.gameObject.activeSelf)
            {
                Vector2 fp = WorldToCanvas(ForestSliceView.Iso(_footMenuNode.gx, _footMenuNode.gy) + new Vector3(0f, 0.35f, 0f));
                _footMenuRt.anchoredPosition = fp;
            }
            if (_workshopDrawer.activeSelf) RefreshWorkshopDrawer();
            if (_residentDrawer.activeSelf) RefreshResidentDrawer();
            if (_caravanModal.activeSelf) RefreshCaravanModal();
            if (_crisisModal.activeSelf) RefreshCrisisPoster();
            if (_heroSpotlightModal.activeSelf && _heroSpotSunburst != null)
                _heroSpotSunburst.rectTransform.Rotate(0f, 0f, 30f * dt);

            UpdateTutorialGloves(dt);
            TickToastsAndFlyers(dt);
        }

        void RefreshMainHud(float dt)
        {
            _lvStarTxt.text = _sim.CampLevel.ToString();
            int xpNeed = ForestSurvivalSim.GetXpNeed(_sim.CampLevel);
            _xpFillImg.fillAmount = Mathf.Clamp01(_sim.CampXp / (float)xpNeed);
            _xpBarTxt.text = $"{_sim.CampXp}/{xpNeed}";
            _popTxt.text = $"{_sim.CurrentPop}/{_sim.PopCap}";
            _coinTxt.text = _sim.Coins.ToString();
            _blueprintTxt.text = _sim.Get(SurvRes.Blueprint).ToString();
            _storageCapTxt.text = $"仓{_sim.CurrentMaxSingleStock}/{_sim.StorageCap}";

            int readyOrders = 0;
            for (int i = 0; i < _sim.ActiveOrders.Count; i++)
                if (_sim.CanSatisfyOrder(i)) readyOrders++;
            _orderBadgeCntTxt.text = readyOrders > 0 ? $"可交付({readyOrders})" : $"{_sim.SatisfiedToday}/{ForestSurvivalSim.SatisfiedGoal}宝箱";
            _orderRightDot.SetActive(readyOrders > 0);

            int slot = 0;
            for (int i = 0; i < HudPinnedRes.Length; i++)
            {
                SurvRes r = HudPinnedRes[i];
                RectTransform rt = _resPillRts[r];
                bool vis = _sim.IsResRevealed(r);
                rt.gameObject.SetActive(vis);
                if (!vis) continue;

                rt.anchoredPosition = new Vector2(14f, -108f - slot * 36f);
                int val = _sim.Get(r);
                _resPillTxts[r].text = val.ToString();
                _resPillTxts[r].color = (ForestSurvivalSim.IsWarehouseCapped(r) && val >= _sim.StorageCap) ? ForestUiWidgets.AlertRed : Color.white;

                float pt = _resPulseTimers[r];
                if (pt > 0f)
                {
                    pt = Mathf.Max(0f, pt - dt);
                    _resPulseTimers[r] = pt;
                    float s = 1f + Mathf.Sin((1f - pt / 0.28f) * Mathf.PI) * 0.24f;
                    rt.localScale = new Vector3(s, s, 1f);
                }
                else rt.localScale = Vector3.one;
                slot++;
            }

            bool showHorde = _sim.HordeWarningTriggered && !_sim.HordeRepelled;
            _hordeBadgeGo.SetActive(showHorde);
            if (showHorde)
            {
                _hordeBadgeGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(14f, -114f - slot * 37f);
                int sec = Mathf.CeilToInt(_sim.HordeTimerSec);
                _hordeBadgeTimerTxt.text = $"{sec / 60:00}:{sec % 60:00}";
            }

            var q = _sim.GetCurrentQuest();
            _questCardBg.sprite = q.done ? ForestComicUiSkin.QuestCardDone : ForestComicUiSkin.QuestCard;
            _questCheckImg.gameObject.SetActive(q.done);
            _questTitleTxt.text = q.done ? $"✔ {q.text} [领奖]" : q.text;
            _questFillImg.fillAmount = q.max > 0 ? Mathf.Clamp01(q.cur / (float)q.max) : 1f;

            _dockLocks[0].gameObject.SetActive(!_sim.HordeWarningTriggered);
            _dockLocks[1].gameObject.SetActive(false);
            _dockLocks[2].gameObject.SetActive(false);
            _dockLocks[3].gameObject.SetActive(false);
            _dockLocks[4].gameObject.SetActive(!_sim.RadioRepaired);
            _dockLocks[5].gameObject.SetActive(!_sim.CanLaunchCityRun);

            bool anyAffordableBuild = false;
            for (int i = 0; i < _sim.Nodes.Count; i++)
            {
                CampNode n = _sim.Nodes[i];
                if ((n.state == NodeState.Discovered || (n.state == NodeState.Active && n.level < n.maxLevel)) && _sim.CanAffordUpgradeAll(n))
                { anyAffordableBuild = true; break; }
            }
            bool anyUnassignedHero = false;
            for (int i = 0; i < _sim.Heroes.Count; i++)
                if (_sim.Heroes[i].unlocked && !_sim.Heroes[i].assigned) anyUnassignedHero = true;

            _dockRedDots[0].SetActive(showHorde);
            _dockRedDots[1].SetActive(anyAffordableBuild);
            _dockRedDots[2].SetActive(anyUnassignedHero);
            _dockRedDots[3].SetActive(_sim.CurrentMaxSingleStock >= _sim.StorageCap);
            _dockRedDots[4].SetActive(_sim.RadioRepaired);
            _dockRedDots[5].SetActive(_sim.CanLaunchCityRun);
        }

        void RefreshWorldTrackBubbles()
        {
            float zoomScale = Mathf.Clamp(5.3f / Mathf.Max(2.5f, _cam.orthographicSize), 0.56f, 1.0f);

            for (int i = 0; i < _checkMarks.Count; i++)
            {
                if (i < _world.ChopQueue.Count)
                {
                    var task = _world.ChopQueue[i];
                    Vector2 cp = WorldToCanvas(ForestSliceView.Iso(task.gx, task.gy) + new Vector3(0f, task.isPile ? 0.42f : 0.72f, 0f));
                    TrackBubble cm = _checkMarks[i];
                    cm.rt.gameObject.SetActive(true);
                    cm.rt.localScale = Vector3.one * zoomScale;
                    cm.bg.enabled = false;
                    cm.label.text = "";
                    cm.icon.gameObject.SetActive(true);
                    cm.icon.sprite = ForestComicUiSkin.GetArt("ico_check");
                    cm.icon.rectTransform.sizeDelta = new Vector2(44f, 44f);
                    cm.rt.anchoredPosition = cp;
                }
                else _checkMarks[i].rt.gameObject.SetActive(false);
            }

            int used = 0;

            if (_world.HasPendingChop && used < _worldBubbles.Count)
            {
                float cgx = (_world.PendingChopCx + 0.5f) * ForestMap.ChunkSize;
                float cgy = (_world.PendingChopCy + 0.5f) * ForestMap.ChunkSize;
                float bob = Mathf.Sin(Time.time * 6f) * 4f;
                TryBindBubble(ref used, ForestSliceView.Iso(cgx, cgy) + new Vector3(0f, 0.55f, 0f), new Vector2(0f, bob),
                    new Vector2(136f, 50f), true, ForestComicUiSkin.ResIcon(SurvRes.Wood), "砍伐扩地", ForestUiWidgets.LushGreen,
                    () => _world.ConfirmPendingChop(), zoomScale, clampInsideHud: true);
            }

            for (int i = 0; i < _sim.Nodes.Count && used < _worldBubbles.Count; i++)
            {
                CampNode n = _sim.Nodes[i];
                if (n.state == NodeState.ReadyToHammer)
                {
                    CampNode cap = n;
                    float bob = Mathf.Sin(Time.time * 6.5f) * 7f;
                    TryBindBubble(ref used, ForestSliceView.Iso(n.gx, n.gy) + new Vector3(0f, 0.75f, 0f), new Vector2(0f, bob),
                        new Vector2(78f, 78f), false, ForestComicUiSkin.GetArt("ico_hammer"), "", Color.white, () => _sim.CompleteHammerNode(cap), zoomScale);
                }
                else if (n.state == NodeState.Constructing)
                {
                    CampNode cap = n;
                    int rem = Mathf.CeilToInt(Mathf.Max(0.1f, n.buildDuration - n.buildTimer));
                    TryBindBubble(ref used, ForestSliceView.Iso(n.gx, n.gy) + new Vector3(0f, 0.65f, 0f), Vector2.zero,
                        new Vector2(142f, 44f), true, null, $"施工 {rem}秒 [秒完]", ForestUiWidgets.LushGreen, () => _sim.SpeedUpNodeBuild(cap), zoomScale);
                }
                else if (n.state == NodeState.Discovered && n.kind == NodeKind.RelicLoot)
                {
                    CampNode cap = n;
                    TryBindBubble(ref used, ForestSliceView.Iso(n.gx, n.gy) + new Vector3(0f, 0.55f, 0f), Vector2.zero,
                        new Vector2(124f, 46f), true, ForestComicUiSkin.ResIcon(SurvRes.Blueprint), "拆解寻宝", ForestUiWidgets.InkDark, () => _sim.StartBuildOrUpgrade(cap, out _), zoomScale);
                }
                else if (n.state == NodeState.Active)
                {
                    CampNode cap = n;
                    if (n.kind == NodeKind.Farm && n.farmPhase != FarmPhase.Growing)
                    {
                        Sprite ico = ForestComicUiSkin.ResIcon(n.farmPhase == FarmPhase.DryTilled ? SurvRes.Water : (n.isHerbPlot ? SurvRes.Herb : SurvRes.Veggie));
                        string txt = n.farmPhase == FarmPhase.DryTilled ? "浇水" : "收割";
                        TryBindBubble(ref used, ForestSliceView.Iso(n.gx, n.gy) + new Vector3(0f, 0.38f, 0f), Vector2.zero,
                            new Vector2(108f, 46f), true, ico, txt, ForestUiWidgets.InkDark, () => HandleNodeClicked(cap), zoomScale);
                    }
                    else if (n.storedAmount > 0)
                    {
                        TryBindBubble(ref used, ForestSliceView.Iso(n.gx, n.gy) + new Vector3(0f, 0.78f, 0f), Vector2.zero,
                            new Vector2(108f, 46f), true, ForestComicUiSkin.ResIcon(n.storedRes), $"+{n.storedAmount}", ForestUiWidgets.InkDark, () => HandleNodeClicked(cap), zoomScale);
                    }
                }
            }

            // 仅在第一顺位伙伴头顶挂 1 个轻量订单气泡（不重复挂满全场）
            if (_sim.ActiveDemand != null && _world.Walkers.Count > 0 && used < _worldBubbles.Count)
            {
                int wi = Mathf.Clamp(_sim.ActiveDemand.walkerIndex, 0, _world.Walkers.Count - 1);
                var w = _world.Walkers[wi];
                TryBindBubble(ref used, ForestSliceView.Iso(w.gx, w.gy) + new Vector3(0f, 0.68f, 0f), Vector2.zero,
                    new Vector2(92f, 50f), true, ForestComicUiSkin.ResIcon(_sim.ActiveDemand.res1),
                    $"{Mathf.Min(_sim.Get(_sim.ActiveDemand.res1), _sim.ActiveDemand.amt1)}/{_sim.ActiveDemand.amt1}",
                    ForestUiWidgets.InkDark, OpenResidentDemandDrawer, zoomScale);
            }

            for (int i = 0; i < _world.Walkers.Count && used < _worldBubbles.Count; i++)
            {
                var w = _world.Walkers[i];
                if (_sim.ActiveDemand != null && _sim.ActiveDemand.walkerIndex == w.index) continue;
                if (_cam.orthographicSize < 6.8f && w.chatterTimer > 0f && !string.IsNullOrEmpty(w.chatterText))
                {
                    TryBindBubble(ref used, ForestSliceView.Iso(w.gx, w.gy) + new Vector3(0f, 0.68f, 0f), Vector2.zero,
                        new Vector2(198f, 48f), true, null, w.chatterText, ForestUiWidgets.InkDark, null, zoomScale);
                }
            }

            if (_cam.orthographicSize < 6.6f)
            {
                for (int i = 0; i < _world.GroundPiles.Count && used < _worldBubbles.Count; i++)
                {
                    var p = _world.GroundPiles[i];
                    if (p.queued || p.kind == ForestSurvivalWorld.FieldKind.LogPile) continue;
                    Sprite ico = p.kind == ForestSurvivalWorld.FieldKind.BerryBush ? ForestComicUiSkin.ResIcon(SurvRes.Veggie) :
                                 p.kind == ForestSurvivalWorld.FieldKind.ScrapWreck ? ForestComicUiSkin.ResIcon(SurvRes.Scrap) :
                                 ForestComicUiSkin.ResIcon(SurvRes.Wood);
                    TryBindBubble(ref used, ForestSliceView.Iso(p.gx, p.gy) + new Vector3(0f, 0.42f, 0f), Vector2.zero,
                        new Vector2(32f, 32f), false, ico, "", Color.white, null, zoomScale);
                }
            }

            var curQuest = _sim.GetCurrentQuest();
            if (!string.IsNullOrEmpty(curQuest.targetNodeId) && _cam.orthographicSize < 7.2f)
            {
                CampNode qNode = _sim.Find(curQuest.targetNodeId);
                if (qNode != null && qNode.state == NodeState.HiddenInForest && !string.IsNullOrEmpty(qNode.hintLabel))
                {
                    CampNode cap = qNode;
                    TryBindBubble(ref used, ForestSliceView.Iso(qNode.gx, qNode.gy) + new Vector3(0f, 0.82f, 0f), Vector2.zero,
                        new Vector2(148f, 44f), true, null, $"🔍 {qNode.hintLabel}", ForestUiWidgets.InkDark, () => SmoothFocusGrid(new Vector2(cap.gx, cap.gy)), zoomScale);
                }
            }

            for (int i = used; i < _worldBubbles.Count; i++)
                _worldBubbles[i].rt.gameObject.SetActive(false);
        }

        bool TryBindBubble(ref int used, Vector3 worldPos, Vector2 extraOffset, Vector2 size, bool showPaperBubble, Sprite ico, string text, Color txtCol, Action onClick, float scale = 1f, bool clampInsideHud = false)
        {
            if (used >= _worldBubbles.Count) return false;
            Vector2 cp = WorldToCanvas(worldPos) + extraOffset;
            if (clampInsideHud)
            {
                cp.x = Mathf.Clamp(cp.x, -260f, 260f);
                cp.y = Mathf.Clamp(cp.y, -420f, 430f);
            }
            else if (cp.x < -345f || cp.x > 345f || cp.y < -490f || cp.y > 515f) return false;

            TrackBubble tb = _worldBubbles[used++];
            tb.rt.gameObject.SetActive(true);
            tb.rt.localScale = Vector3.one * scale;
            tb.rt.sizeDelta = size;
            tb.rt.anchoredPosition = cp;
            tb.bg.enabled = showPaperBubble;
            tb.bg.raycastTarget = (onClick != null);
            tb.icon.raycastTarget = (onClick != null && !showPaperBubble);
            tb.onClick = onClick;
            if (tb.btn != null)
            {
                tb.btn.interactable = (onClick != null);
                tb.btn.targetGraphic = showPaperBubble ? tb.bg : tb.icon;
            }

            if (ico != null && string.IsNullOrEmpty(text))
            {
                tb.icon.gameObject.SetActive(true);
                tb.icon.sprite = ico;
                tb.icon.rectTransform.anchoredPosition = Vector2.zero;
                tb.icon.rectTransform.sizeDelta = size;
                tb.label.text = "";
            }
            else if (ico != null)
            {
                tb.icon.gameObject.SetActive(true);
                tb.icon.sprite = ico;
                tb.icon.rectTransform.anchoredPosition = new Vector2(-size.x * 0.26f, 5f);
                tb.icon.rectTransform.sizeDelta = new Vector2(30f, 30f);
                tb.label.text = text;
                tb.label.color = txtCol;
                tb.label.rectTransform.offsetMin = new Vector2(40f, 12f);
                tb.label.rectTransform.offsetMax = new Vector2(-8f, -4f);
            }
            else
            {
                tb.icon.gameObject.SetActive(false);
                tb.label.text = text;
                tb.label.color = txtCol;
                tb.label.rectTransform.offsetMin = new Vector2(10f, 10f);
                tb.label.rectTransform.offsetMax = new Vector2(-10f, -4f);
            }
            return true;
        }

        void RefreshWorkshopDrawer()
        {
            if (_activeWorkshop == null) return;
            int slotCap = ForestSurvivalSim.GetWorkshopSlotCap(_activeWorkshop);
            _wsTitleTxt.text = $"{_activeWorkshop.activeTitle}  {_activeWorkshop.level}级（队列 {_activeWorkshop.queue.Count}/{slotCap}）";
            _wsUpgradeMiniBtn.gameObject.SetActive(_activeWorkshop.level < _activeWorkshop.maxLevel);

            for (int i = 0; i < 5; i++)
            {
                bool slotOpen = i < slotCap;
                bool has = slotOpen && i < _activeWorkshop.queue.Count;
                _wsQueueSlotBgs[i].color = slotOpen ? Color.white : new Color(0.65f, 0.62f, 0.58f, 0.85f);
                _wsQueueIcons[i].gameObject.SetActive(has);
                if (!slotOpen)
                {
                    _wsQueueTimers[i].text = $"Lv.{i - 1}扩槽";
                }
                else if (has)
                {
                    QueueItem q = _activeWorkshop.queue[i];
                    _wsQueueIcons[i].sprite = ForestComicUiSkin.ResIcon(q.recipe.outRes);
                    int rem = Mathf.CeilToInt(Mathf.Max(0.1f, q.duration - q.timer));
                    _wsQueueTimers[i].text = i == 0 ? $"{rem}秒" : "排队中";
                }
                else _wsQueueTimers[i].text = "空闲";
            }
            _wsSpeedUpPill.SetActive(_activeWorkshop.queue.Count > 0);

            WorkshopRecipe[] recipes = _sim.GetWorkshopRecipes(_activeWorkshop);
            for (int i = 0; i < 4; i++)
            {
                bool exist = i < recipes.Length;
                _wsRecipeSlots[i].SetActive(exist);
                if (!exist) continue;

                WorkshopRecipe r = recipes[i];
                bool unlocked = _sim.IsRecipeUnlocked(_activeWorkshop, r);
                _wsRecipeFrames[i].sprite = (i == _selectedRecipeIdx) ? ForestComicUiSkin.SubSlotSelected : ForestComicUiSkin.SubSlot;
                _wsRecipeIcons[i].sprite = ForestComicUiSkin.ResIcon(r.outRes);
                _wsRecipeIcons[i].color = unlocked ? Color.white : new Color(0.4f, 0.4f, 0.4f, 0.75f);
                _wsRecipeLocks[i].gameObject.SetActive(!unlocked);
                _wsRecipeSubTxts[i].text = unlocked ? r.name : (_activeWorkshop.level < r.unlockBuildingLv ? $"本坊Lv.{r.unlockBuildingLv}" : $"星级Lv.{r.unlockCampLv}");
            }

            if (_selectedRecipeIdx >= 0 && _selectedRecipeIdx < recipes.Length)
            {
                WorkshopRecipe sel = recipes[_selectedRecipeIdx];
                _wsSelBigIcon.sprite = ForestComicUiSkin.ResIcon(sel.outRes);
                string ratTag = sel.bonusRation > 0 ? $" +口粮{sel.bonusRation}" : "";
                _wsSelNameTxt.text = $"{sel.name} ×{sel.outAmt}{ratTag}";
                bool unlocked = _sim.IsRecipeUnlocked(_activeWorkshop, sel);
                bool canAff = unlocked && _sim.CanAffordRecipe(sel);
                _wsSelStockTxt.text = canAff
                    ? $"库存：{_sim.Get(sel.outRes)}/{_sim.StorageCap}"
                    : (!string.IsNullOrEmpty(sel.sourceHint) ? sel.sourceHint : $"当前库存：{_sim.Get(sel.outRes)}");
                _wsSelStockTxt.color = canAff ? ForestUiWidgets.InkMuted : ForestUiWidgets.AlertRed;

                BindWorkshopIngredient(_wsReqIcon1, _wsReqTxt1, _wsReqBtn1, sel.inRes1, sel.inAmt1, true);
                BindWorkshopIngredient(_wsReqIcon2, _wsReqTxt2, _wsReqBtn2, sel.inRes2, sel.inAmt2, sel.inAmt2 > 0);
                BindWorkshopIngredient(_wsReqIcon3, _wsReqTxt3, _wsReqBtn3, sel.inRes3, sel.inAmt3, sel.inAmt3 > 0);

                float dur = Mathf.Max(1.4f, sel.duration / _sim.ProdSpeedMultiplier);
                _wsTimeTxt.text = $"耗时 {Mathf.CeilToInt(dur)}秒";
                bool canProd = canAff && _activeWorkshop.queue.Count < slotCap;
                _wsProduceBtn.GetComponent<Image>().sprite = canProd ? ForestComicUiSkin.BtnBlue : ForestComicUiSkin.BtnGrey;
                _wsBtnLbl.text = !unlocked
                    ? (_activeWorkshop.level < sel.unlockBuildingLv ? $"需作坊Lv.{sel.unlockBuildingLv}" : $"需星级Lv.{sel.unlockCampLv}")
                    : "排产制造";
            }
        }

        void BindWorkshopIngredient(Image ico, Text txt, Button btn, SurvRes res, int need, bool active)
        {
            ico.gameObject.SetActive(active);
            if (!active) return;
            ico.sprite = ForestComicUiSkin.ResIcon(res);
            int own = _sim.Get(res);
            txt.text = $"{ForestSurvivalSim.ResName(res)} {own}/{need}";
            txt.color = own >= need ? ForestUiWidgets.InkDark : ForestUiWidgets.AlertRed;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OpenItemSourceTip(res));
        }

        void RefreshResidentDrawer()
        {
            _sim.EnsureActiveOrders();
            _resMoodFill.fillAmount = _sim.SatisfiedToday / (float)ForestSurvivalSim.SatisfiedGoal;
            _resMoodTxt.text = $"满 {ForestSurvivalSim.SatisfiedGoal} 单赠【铜币+260 + 手稿x2】 {_sim.SatisfiedToday}/{ForestSurvivalSim.SatisfiedGoal}";
            _resRefreshBtnLbl.text = _sim.OrderRefreshCooldown > 0.1f ? $"冷却 {Mathf.CeilToInt(_sim.OrderRefreshCooldown)}s" : "刷新订单";

            for (int i = 0; i < 3; i++)
            {
                if (i >= _sim.ActiveOrders.Count) continue;
                ResidentDemand d = _sim.ActiveOrders[i];
                _ordBusts[i].sprite = ForestComicUiSkin.GetArt(d.portraitKey, ForestComicUiSkin.GetArt("bust_cook"));
                _ordTitles[i].text = $"{d.speakerName}  ·  【{d.categoryTag}】";
                _ordComplaints[i].text = d.complaint;
                string bpStr = d.blueprintBonus > 0 ? $"   工程手稿 +{d.blueprintBonus}" : "";
                _ordRewards[i].text = $"奖励：铜币 +{d.coinReward}   经验 +{d.xpReward}XP{bpStr}";

                SurvRes r1 = d.res1;
                int o1 = _sim.Get(r1);
                _ordReqIco1[i].sprite = ForestComicUiSkin.ResIcon(r1);
                _ordReqTxt1[i].text = $"{o1}/{d.amt1}";
                _ordReqTxt1[i].color = o1 >= d.amt1 ? ForestUiWidgets.LushGreen : ForestUiWidgets.AlertRed;
                _ordReqBtn1[i].onClick.RemoveAllListeners();
                _ordReqBtn1[i].onClick.AddListener(() => OpenItemSourceTip(r1));

                bool has2 = d.amt2 > 0;
                _ordReqBox2[i].SetActive(has2);
                if (has2)
                {
                    SurvRes r2 = d.res2;
                    int o2 = _sim.Get(r2);
                    _ordReqIco2[i].sprite = ForestComicUiSkin.ResIcon(r2);
                    _ordReqTxt2[i].text = $"{o2}/{d.amt2}";
                    _ordReqTxt2[i].color = o2 >= d.amt2 ? ForestUiWidgets.LushGreen : ForestUiWidgets.AlertRed;
                    _ordReqBtn2[i].onClick.RemoveAllListeners();
                    _ordReqBtn2[i].onClick.AddListener(() => OpenItemSourceTip(r2));
                }

                bool ok = _sim.CanSatisfyOrder(i);
                _ordDeliverBtns[i].GetComponent<Image>().sprite = ok ? ForestComicUiSkin.BtnGreen : ForestComicUiSkin.BtnBlue;
                _ordDeliverLbls[i].text = ok ? "交付订单" : "去准备";
            }
        }

        void RefreshCaravanModal()
        {
            _cvSubTxt.text = $"下一班邻山行商刷新：{Mathf.CeilToInt(_sim.CaravanRefreshTimer)}秒  ·  当前铜币：{_sim.Coins}  ·  手稿：{_sim.Get(SurvRes.Blueprint)}";
            for (int i = 0; i < 3 && i < _sim.CaravanOffers.Count; i++)
            {
                CaravanOffer o = _sim.CaravanOffers[i];
                _cvIcons[i].sprite = ForestComicUiSkin.ResIcon(o.giveRes);
                _cvTitles[i].text = o.title;
                int own = _sim.Get(o.giveRes);
                _cvNeedTxts[i].text = $"装箱物资：{ForestSurvivalSim.ResName(o.giveRes)}  {own}/{o.giveAmt}";
                _cvNeedTxts[i].color = own >= o.giveAmt ? ForestUiWidgets.InkDark : ForestUiWidgets.AlertRed;
                _cvRewardTxts[i].text = $"结算回报：铜币 +{o.coinGain}   ·   工程手稿 +{o.blueprintGain}";

                if (o.traded)
                {
                    _cvTradeBtns[i].GetComponent<Image>().sprite = ForestComicUiSkin.BtnGrey;
                    _cvTradeLbls[i].text = "已交割";
                }
                else if (own >= o.giveAmt)
                {
                    _cvTradeBtns[i].GetComponent<Image>().sprite = ForestComicUiSkin.BtnGreen;
                    _cvTradeLbls[i].text = "装车交货";
                }
                else
                {
                    _cvTradeBtns[i].GetComponent<Image>().sprite = ForestComicUiSkin.BtnBlue;
                    _cvTradeLbls[i].text = "去生产";
                }
            }
        }

        void RefreshCrisisPoster()
        {
            int sec = Mathf.CeilToInt(_sim.HordeTimerSec);
            _crisisTimerBigTxt.text = $"00:{sec / 60:00}:{sec % 60:00}";
            int rat = _sim.Get(SurvRes.Ration);
            _crisisTask1Txt.text = $"储备 16 份熟食口粮 ({Mathf.Min(rat, 16)}/16)";
            bool fort = _sim.HasWatchtower || _sim.HasBarricade;
            _crisisTask2Txt.text = $"建造捕猎木哨塔或尖木拒马 ({(fort ? 1 : 0)}/1)";
        }

        void TickCityJoystickInput()
        {
            if (_cityArena.IsSettling)
            {
                _cityArena.SetJoystickInput(Vector2.zero);
                return;
            }

            if (Input.GetMouseButtonDown(0) && !IsPointerOverBottomCityButtons())
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, Input.mousePosition, _cam, out Vector2 lp);
                if (lp.y < 360f)
                {
                    _joyDragging = true;
                    _joyOriginCanvas = lp;
                    _joyBaseRt.anchoredPosition = lp;
                    _joyKnobRt.anchoredPosition = Vector2.zero;
                }
            }
            else if (Input.GetMouseButton(0) && _joyDragging)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, Input.mousePosition, _cam, out Vector2 lp);
                Vector2 delta = lp - _joyOriginCanvas;
                Vector2 clamped = Vector2.ClampMagnitude(delta, 56f);
                _joyKnobRt.anchoredPosition = clamped;
                _cityArena.SetJoystickInput(clamped / 56f);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _joyDragging = false;
                _joyKnobRt.anchoredPosition = Vector2.zero;
                _cityArena.SetJoystickInput(Vector2.zero);
            }

            float kx = Input.GetAxisRaw("Horizontal");
            float ky = Input.GetAxisRaw("Vertical");
            if (!_joyDragging && (Mathf.Abs(kx) > 0.01f || Mathf.Abs(ky) > 0.01f))
            {
                Vector2 kd = Vector2.ClampMagnitude(new Vector2(kx, ky), 1f);
                _joyKnobRt.anchoredPosition = kd * 50f;
                _cityArena.SetJoystickInput(kd);
            }
        }

        bool IsPointerOverBottomCityButtons()
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, Input.mousePosition, _cam, out Vector2 lp);
            return lp.y < -520f && (lp.x < -220f || lp.x > 160f);
        }

        void RefreshCityDriveHud()
        {
            _cdCargoFill.fillAmount = _cityArena.MaxCargo > 0 ? _cityArena.CurrentCargo / (float)_cityArena.MaxCargo : 0f;
            _cdCargoTxt.text = $"{_cityArena.CurrentCargo} / {_cityArena.MaxCargo}";
            _cdAutoBtnLbl.text = _cityArena.AutoPilot ? "自动中:开" : "自动巡航";

            if (_cityArena.SessionGains.Count > 0)
            {
                var parts = new List<string>();
                foreach (var kv in _cityArena.SessionGains)
                    parts.Add($"{ForestSurvivalSim.ResName(kv.Key)}+{kv.Value}");
                _cdLootSummaryTxt.text = "车斗已装：" + string.Join("  ", parts);
            }
            else
            {
                _cdLootSummaryTxt.text = "拖动摇杆（或点自动巡航）撞击丧尸、搜刮工程手稿与废铁！";
            }
        }

        void TickFishingMode(float dt)
        {
            if (_fishAuto && _fishPhase == 0)
                OnFishingMainBtnClicked();

            if (_fishPhase == 1)
            {
                _fishWaitTimer -= dt;
                if (_fishWaitTimer <= 0f)
                {
                    _fishPhase = 2;
                    _fishTension = 0.34f;
                    _world.SetFishBitingState(true);
                    SyncFishingUiState();
                }
            }
            else if (_fishPhase == 2)
            {
                _fishTension = Mathf.Max(0.08f, _fishTension - dt * 0.16f);
                if (_fishAuto)
                {
                    _fishTension += dt * 0.62f;
                    if (_fishTension >= 1f) CompleteOneFishCatch();
                }
                _fishArcFill.fillAmount = Mathf.Clamp01(_fishTension);
            }
        }

        void UpdateTutorialGloves(float dt)
        {
            if (_comicDialogOverlay.activeSelf || _levelUpModal.activeSelf || _heroSpotlightModal.activeSelf ||
                _crisisModal.activeSelf || _itemTipPopup.activeSelf || _sourceRouteModal.activeSelf)
            {
                _uiGlove.Hide();
                _worldGlove.Hide();
                return;
            }

            if (_buildCardModal.activeSelf)
            {
                _worldGlove.Hide();
                _uiGlove.ShowAt(new Vector2(20f, -310f), dt);
                return;
            }

            if (_workshopDrawer.activeSelf && _activeWorkshop != null)
            {
                _worldGlove.Hide();
                if (_activeWorkshop.queue.Count > 0 && _sim.QuestIndex <= 2)
                    _uiGlove.ShowAt(new Vector2(-236f, -120f), dt);
                else
                    _uiGlove.ShowAt(new Vector2(20f, -565f), dt);
                return;
            }

            if (_residentDrawer.activeSelf)
            {
                _worldGlove.Hide();
                _uiGlove.ShowAt(new Vector2(250f, 30f), dt);
                return;
            }

            var q = _sim.GetCurrentQuest();
            if (!IsModalOpen && q.done)
            {
                _worldGlove.Hide();
                _uiGlove.ShowAt(new Vector2(-150f, -482f), dt);
                return;
            }

            _uiGlove.Hide();

            if (!IsModalOpen)
            {
                if (_world.HasPendingChop)
                {
                    float cgx = (_world.PendingChopCx + 0.5f) * ForestMap.ChunkSize;
                    float cgy = (_world.PendingChopCy + 0.5f) * ForestMap.ChunkSize;
                    Vector2 cp = WorldToCanvas(ForestSliceView.Iso(cgx, cgy) + new Vector3(0f, 0.55f, 0f));
                    cp.x = Mathf.Clamp(cp.x, -260f, 260f);
                    cp.y = Mathf.Clamp(cp.y, -420f, 430f);
                    _worldGlove.ShowAt(cp, dt);
                    return;
                }

                for (int i = 0; i < _sim.Nodes.Count; i++)
                {
                    if (_sim.Nodes[i].state == NodeState.ReadyToHammer)
                    {
                        Vector2 hp = WorldToCanvas(ForestSliceView.Iso(_sim.Nodes[i].gx, _sim.Nodes[i].gy) + new Vector3(0f, 0.9f, 0f));
                        _worldGlove.ShowAt(hp, dt);
                        return;
                    }
                }

                if (_sim.QuestIndex <= 5)
                {
                    Vector2 tp = WorldToCanvas(ForestSliceView.Iso(q.focusGrid.x, q.focusGrid.y) + new Vector3(0f, 0.45f, 0f));
                    if (Mathf.Abs(tp.x) < 330f && tp.y > -460f && tp.y < 540f)
                    {
                        _worldGlove.ShowAt(tp, dt);
                        return;
                    }
                }
            }
            _worldGlove.Hide();
        }

        void TickToastsAndFlyers(float dt)
        {
            for (int i = _pickupToasts.Count - 1; i >= 0; i--)
            {
                PickupToastItem pt = _pickupToasts[i];
                pt.age += dt;
                pt.rt.anchoredPosition = new Vector2(0f, 56f + i * 40f + pt.age * 24f);
                if (pt.age > 0.42f && !pt.spawnedFlyer)
                {
                    pt.spawnedFlyer = true;
                    SpawnFlyIconToHud(pt.res, pt.rt.anchoredPosition + new Vector2(-62f, 0f));
                }
                pt.cg.alpha = 1f - Mathf.Clamp01((pt.age - 0.85f) / 0.35f);
                if (pt.age >= 1.2f)
                {
                    SafeDestroy(pt.rt.gameObject);
                    _pickupToasts.RemoveAt(i);
                }
            }

            for (int i = _flyIcons.Count - 1; i >= 0; i--)
            {
                FlyIconItem f = _flyIcons[i];
                f.t += dt;
                float u = Mathf.Clamp01(f.t / f.dur);
                float ease = u * u * (3f - 2f * u);
                Vector2 pos = Vector2.Lerp(f.from, f.to, ease) + new Vector2(0f, Mathf.Sin(u * Mathf.PI) * 65f);
                f.rt.anchoredPosition = pos;
                f.rt.localScale = Vector3.one * Mathf.Lerp(1.1f, 0.72f, u);
                if (u >= 1f)
                {
                    if (_resPulseTimers.ContainsKey(f.targetRes))
                        _resPulseTimers[f.targetRes] = 0.28f;
                    SafeDestroy(f.rt.gameObject);
                    _flyIcons.RemoveAt(i);
                }
            }

            for (int i = _worldToasts.Count - 1; i >= 0; i--)
            {
                WorldTextToast wt = _worldToasts[i];
                wt.age += dt;
                wt.rt.anchoredPosition = wt.startPos + new Vector2(0f, wt.age * 46f);
                var cg = wt.rt.GetComponent<Image>();
                float a = 1f - Mathf.Clamp01((wt.age - 1.2f) / 0.45f);
                if (cg != null) cg.color = new Color(1f, 1f, 1f, a);
                wt.txt.color = new Color(1f, 0.86f, 0.38f, a);
                if (wt.age >= 1.65f)
                {
                    SafeDestroy(wt.rt.gameObject);
                    _worldToasts.RemoveAt(i);
                }
            }
        }
    }
}
