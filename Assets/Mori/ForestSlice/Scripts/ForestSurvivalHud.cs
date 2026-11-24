using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Mori.ForestSlice.ForestSurvivalSim;
using static Mori.ForestSlice.ForestTacticalArena;

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
        public static float EditorTopSafePadOverride;
        float _topSafePad;
        Text _lvStarTxt, _xpBarTxt, _popTxt, _coinTxt, _blueprintTxt, _storageCapTxt, _orderBadgeCntTxt;
        Image _xpFillImg;
        RectTransform _rightOrderEntryRt;
        GameObject _orderRightDot;
        readonly Dictionary<SurvRes, RectTransform> _resPillRts = new Dictionary<SurvRes, RectTransform>();
        readonly Dictionary<SurvRes, Text> _resPillTxts = new Dictionary<SurvRes, Text>();
        readonly Dictionary<SurvRes, float> _resPulseTimers = new Dictionary<SurvRes, float>();

        // 左侧尸潮挂件、左下任务浮窗、底部 6 宫格 Dock
        GameObject _hordeBadgeGo;
        Text _hordeBadgeTimerTxt;
        Image _questCardBg, _questCheckImg, _questFillImg;
        Text _questTitleTxt;
        float _questPulseTimer;
        readonly Button[] _dockBtns = new Button[6];
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

        // 新英雄高光与双页签【英雄委任(6大岗位多槽) / 英雄养成(升级·升星·防具换装)】面板
        GameObject _heroSpotlightModal;
        Image _heroSpotSunburst, _heroSpotBust;
        Text _heroSpotNameTxt, _heroSpotRoleTxt, _heroSpotBuffTxt;

        GameObject _heroAssignModal;
        int _heroActiveTab; // 0 = 英雄委任(水世界6岗位卡), 1 = 英雄养成(等级·星级·换装)
        Text _haHeaderTitleTxt;
        Text _haLeaderBannerTxt;
        Button _haTabBtn0, _haTabBtn1;
        Text _haTabLbl0, _haTabLbl1;
        GameObject _haDelegationPage, _haGrowthPage;
        Text _haGrowthResTxt;

        // Tab 0: 6 大岗位多槽委任卡
        readonly Text[] _delPostEffectTxts = new Text[6];
        readonly GameObject[] _delPostLockedHintGos = new GameObject[6];
        readonly Text[] _delPostLockedHintTxts = new Text[6];
        readonly GameObject[,] _delSlotBoxes = new GameObject[6, 4];
        readonly Image[,] _delSlotFrames = new Image[6, 4];
        readonly Image[,] _delSlotBusts = new Image[6, 4];
        readonly Image[,] _delSlotLocks = new Image[6, 4];
        readonly Text[,] _delSlotStarTags = new Text[6, 4];
        readonly Text[,] _delSlotSubTxts = new Text[6, 4];

        // Tab 1: 6 名骨干英雄养成与 3 档防具换装行
        readonly GameObject[] _haRows = new GameObject[6];
        readonly Image[] _haRarityFrames = new Image[6];
        readonly Image[] _haBusts = new Image[6];
        readonly Text[] _haStarBadges = new Text[6];
        readonly Text[] _haNames = new Text[6];
        readonly Text[] _haDescs = new Text[6];
        readonly Button[] _haLvUpBtns = new Button[6];
        readonly Text[] _haLvUpLbls = new Text[6];
        readonly Button[] _haStarUpBtns = new Button[6];
        readonly Text[] _haStarUpLbls = new Text[6];
        readonly Button[,] _haSkinBtns = new Button[6, 3];
        Text[,] _haSkinTexts;

        // 1. 山口闯关战前海报 (_stagePosterModal / _wavePrepModal)
        GameObject _wavePrepModal;
        Text _spStageBadgeTxt, _spStageTitleTxt, _spStageSubTxt;
        Image _spBossIconImg, _spThreatTag1Bg, _spThreatTag2Bg, _spPowerStatePillBg, _spMilestoneBg;
        Text _spThreatTag1Txt, _spThreatTag2Txt, _spPowerCompareTxt, _spPowerStateTxt;
        Text _spSquadSummaryTxt, _spSynergyStripTxt, _spMilestoneTxt;
        readonly Image[] _spPreviewFrames = new Image[5];
        readonly Image[] _spPreviewBusts = new Image[5];
        readonly Image[] _spPreviewLocks = new Image[5];
        readonly Text[] _spPreviewNames = new Text[5];
        readonly Text[] _spPreviewRoles = new Text[5];
        readonly Image[] _spRewardIcons = new Image[4];
        readonly Text[] _spRewardTxts = new Text[4];

        // 2. 3x3 九宫格战术布阵弹窗 (_tacticalFormationModal)
        GameObject _tacticalFormationModal;
        int _selectedFormationHeroIdx = -1;
        Text _tfDeployCountTxt, _tfTotalPowerTxt, _tfFormationSummaryTxt, _tfRosterHintTxt;
        readonly Image[] _tfTileFrames = new Image[9];
        readonly GameObject[] _tfTileBuffGos = new GameObject[9];
        readonly Image[] _tfTileBuffBgs = new Image[9];
        readonly Text[] _tfTileBuffTxts = new Text[9];
        readonly GameObject[] _tfTilePortraitBoxes = new GameObject[9];
        readonly Image[] _tfTileBusts = new Image[9];
        readonly Text[] _tfTileNames = new Text[9];
        readonly Text[] _tfTileRoles = new Text[9];
        readonly Text[] _tfTileEmptyTxts = new Text[9];
        readonly Text[] _tfTileLaneHints = new Text[9];
        readonly GameObject[] _tfTileRemoveGos = new GameObject[9];
        readonly Button[] _tfFormationBtns = new Button[4];
        readonly Image[] _tfFormationBtnImgs = new Image[4];
        readonly Text[] _tfFormationBtnLbls = new Text[4];
        readonly Image[] _tfSynergyBgs = new Image[4];
        readonly Text[] _tfSynergyTitles = new Text[4];
        readonly Text[] _tfSynergyDescs = new Text[4];
        readonly GameObject[] _tfRosterCards = new GameObject[6];
        readonly Image[] _tfRosterFrames = new Image[6];
        readonly Image[] _tfRosterTagBgs = new Image[6];
        readonly Text[] _tfRosterTagTxts = new Text[6];
        readonly Image[] _tfRosterBusts = new Image[6];
        readonly Text[] _tfRosterNames = new Text[6];
        readonly Text[] _tfRosterRoles = new Text[6];
        readonly Text[] _tfRosterPowers = new Text[6];

        // 3. 独立战场 HUD (_waveCombatHud — 100% 微信右上角胶囊安全区避让)
        GameObject _waveCombatHud;
        Text _wcStageTimerTxt, _wcTeamPowerTxt, _wcTeamHpTxt, _wcEnemyTitleTxt, _wcEnemyHpTxt;
        Image _wcTeamHpFill, _wcEnemyHpFill;
        Button _wcSkipBtn, _wcAutoBtn, _wcSpeedBtn;
        Image _wcSkipBtnImg, _wcAutoBtnImg, _wcSpeedBtnImg;
        Text _wcSkipLbl, _wcAutoLbl, _wcSpeedLbl;
        readonly GameObject[] _wcHeroCards = new GameObject[5];
        readonly RectTransform[] _wcHeroCardRts = new RectTransform[5];
        readonly Image[] _wcHeroFrames = new Image[5];
        readonly Image[] _wcHeroBusts = new Image[5];
        readonly Text[] _wcHeroNames = new Text[5];
        readonly Image[] _wcHeroHpFills = new Image[5];
        readonly Image[] _wcHeroShieldFills = new Image[5];
        readonly Text[] _wcHeroHpTxts = new Text[5];
        readonly Image[] _wcHeroRageFills = new Image[5];
        readonly Text[] _wcHeroRageTxts = new Text[5];
        readonly Image[] _wcHeroActionBgs = new Image[5];
        readonly Text[] _wcHeroActionTxts = new Text[5];
        readonly int[] _wcBoundHeroIdxs = new int[5];

        // 4. 挑战胜利 MVP / 战败复盘结算弹窗 (_arenaResultModal)
        GameObject _arenaResultModal;
        Image _arSunburst, _arRibbonImg, _arMvpBust, _arAutoChainBtnImg;
        Text _arRibbonTxt, _arStageSubTxt, _arMvpNameTxt, _arMvpDmgTxt, _arRewardSummaryTxt;
        Text _arAutoChainLbl, _arNextStageBtnLbl, _arDefeatAdviceTxt;
        GameObject _arVictoryRoot, _arDefeatRoot;
        bool _wasInArenaBattle;
        bool _wasArenaResultShown;
        float _autoChainCountdown = 5f;

        // 快捷建造/分类仓库总览弹窗
        GameObject _quickListModal;
        Text _qlTitleTxt, _qlSubCapTxt;
        Button _qlUpgradeWhBtn;
        RectTransform _qlContentRt;
        readonly List<GameObject> _qlSpawnedRows = new List<GameObject>();

        // 农牧自循环双半屏抽屉：生态选种农田 (_farmDrawer) + 山林畜牧栏 (_ranchDrawer) + 主界面温饱士气胶囊
        Text _moralePillTxt;
        GameObject _farmDrawer;
        CropSeedKind _selectedSeedKind = CropSeedKind.Veggie;
        Text _fmTitleTxt, _fmSynergyTxt, _fmYieldPreviewTxt, _fmAutoLbl, _fmMainBtnLbl;
        Button _fmMainBtn, _fmUpgradeBtn;
        readonly Image[] _fmPlotBgs = new Image[4];
        readonly Image[] _fmPlotIcons = new Image[4];
        readonly Image[] _fmPlotProgressFills = new Image[4];
        readonly Text[] _fmPlotStateTxts = new Text[4];
        readonly Button[] _fmPlotMainBtns = new Button[4];
        readonly Text[] _fmPlotMainLbls = new Text[4];
        readonly Button[] _fmPlotSubBtns = new Button[4];
        readonly Text[] _fmPlotSubLbls = new Text[4];
        readonly Image[] _fmSeedFrames = new Image[4];
        readonly Text[] _fmSeedCntTxts = new Text[4];

        GameObject _ranchDrawer;
        RanchAnimalKind _selectedRanchAnimal = RanchAnimalKind.SnowRabbit;
        Text _rnTitleTxt, _rnSynergyTxt, _rnBreedPreviewTxt, _rnAutoLbl, _rnMainBtnLbl;
        Button _rnMainBtn, _rnUpgradeBtn;
        readonly Image[] _rnPenBgs = new Image[4];
        readonly Image[] _rnPenIcons = new Image[4];
        readonly Image[] _rnPenProgressFills = new Image[4];
        readonly Text[] _rnPenStateTxts = new Text[4];
        readonly Button[] _rnPenBtns = new Button[4];
        readonly Text[] _rnPenBtnLbls = new Text[4];
        readonly Image[] _rnBreedFrames = new Image[3];
        readonly Text[] _rnBreedSubTxts = new Text[3];

        // 丧尸城街区选择卡 + 虚拟摇杆开车 HUD + 【恭喜获得】放射结算
        GameObject _citySelectModal;
        readonly Button[] _csSiteBtns = new Button[4];
        readonly Text[] _csSiteBtnLbls = new Text[4];

        GameObject _cityDriveHud;
        Image _cdCargoFill, _cdArmorFill, _cdBossFill;
        GameObject _cdBossBox;
        Text _cdCargoTxt, _cdArmorTxt, _cdLootSummaryTxt, _cdAutoBtnLbl, _cdNitroLbl;
        Image _cdNitroRing;
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

        // 启动《末日囤货狂》Loading 页（含进度条与底部完整《健康游戏忠告》全文）
        GameObject _loadingOverlay;
        CanvasGroup _loadingCg;
        Image _loadingFillImg;
        Text _loadingTipTxt;
        bool _bootLoadingActive;
        float _bootLoadingElapsed;
        float _bootLoadingDuration = 2.0f;
        Action _onBootLoadingDone;

        // 双人半身漫画对话遮罩
        GameObject _comicDialogOverlay;
        Image _cdLeftBust, _cdRightBust;
        Text _cdSpeakerNameTxt, _cdLineBodyTxt;
        readonly Queue<ComicLine> _dialogQueue = new Queue<ComicLine>();

        sealed class DamagePopupItem
        {
            public RectTransform rt;
            public Text txt;
            public Vector2 startPos;
            public float age;
        }

        // 追踪池与动效池
        readonly List<TrackBubble> _checkMarks = new List<TrackBubble>();
        readonly List<TrackBubble> _worldBubbles = new List<TrackBubble>();
        readonly List<PickupToastItem> _pickupToasts = new List<PickupToastItem>();
        readonly List<FlyIconItem> _flyIcons = new List<FlyIconItem>();
        readonly List<WorldTextToast> _worldToasts = new List<WorldTextToast>();
        readonly List<DamagePopupItem> _dmgPopups = new List<DamagePopupItem>();

        ComicGlovePointer _worldGlove;
        ComicGlovePointer _uiGlove;

        bool _panning;
        Vector3 _panTarget;
        float _panTargetOrtho;
        bool _panChangeOrtho;

        static readonly SurvRes[] HudPinnedRes =
        {
            SurvRes.Wood, SurvRes.Ration, SurvRes.Plank, SurvRes.Scrap, SurvRes.Nails, SurvRes.Cloth
        };

        public ForestCityScavengeArena CityArena => _cityArena;

        public bool IsModalOpen =>
            ForestGmConsole.IsGmPanelOpen ||
            (_loadingOverlay != null && _loadingOverlay.activeSelf) ||
            (_buildCardModal != null && _buildCardModal.activeSelf) ||
            (_itemTipPopup != null && _itemTipPopup.activeSelf) ||
            (_sourceRouteModal != null && _sourceRouteModal.activeSelf) ||
            (_workshopDrawer != null && _workshopDrawer.activeSelf) ||
            (_farmDrawer != null && _farmDrawer.activeSelf) ||
            (_ranchDrawer != null && _ranchDrawer.activeSelf) ||
            (_residentDrawer != null && _residentDrawer.activeSelf) ||
            (_caravanModal != null && _caravanModal.activeSelf) ||
            (_crisisModal != null && _crisisModal.activeSelf) ||
            (_levelUpModal != null && _levelUpModal.activeSelf) ||
            (_heroSpotlightModal != null && _heroSpotlightModal.activeSelf) ||
            (_heroAssignModal != null && _heroAssignModal.activeSelf) ||
            (_wavePrepModal != null && _wavePrepModal.activeSelf) ||
            (_tacticalFormationModal != null && _tacticalFormationModal.activeSelf) ||
            (_arenaResultModal != null && _arenaResultModal.activeSelf) ||
            (_quickListModal != null && _quickListModal.activeSelf) ||
            (_citySelectModal != null && _citySelectModal.activeSelf) ||
            (_lootSettleModal != null && _lootSettleModal.activeSelf) ||
            (_comicDialogOverlay != null && _comicDialogOverlay.activeSelf) ||
            _inFishingMode ||
            (_world != null && _world.InWaveDefense) ||
            (_cityArena != null && _cityArena.IsRunning);

        public void Init(Camera cam, ForestSurvivalSim sim, ForestSurvivalWorld world)
        {
            _cam = cam;
            _sim = sim;
            _world = world;
            _haSkinTexts = new Text[6, 3];
            ForestComicUiSkin.Ensure();

            EnsureEventSystem();
            BuildCanvasRoot();

            _cityArena = new ForestCityScavengeArena(_cam, _sim, _world);
            _cityArena.OnStreetPickup += EnqueueCenterPickupToast;
            _cityArena.OnCargoFullSettlement += ShowCityLootSettlement;
            _cityArena.OnCityDamageNumber += SpawnFloatingDamageNumber;
            _cityArena.OnCityToast += SpawnScreenCenterBanner;

            BuildWorldTrackingLayer();
            BuildMainCampHud();
            BuildFootRingMenu();
            BuildBreakOutConfirmModal();
            BuildWorkshopQueueDrawer();
            BuildFarmAndRanchDrawers();
            BuildResidentDemandDrawer();
            BuildCaravanModal();
            BuildCrisisHordeModal();
            BuildLevelUpModal();
            BuildHeroModals();
            BuildWaveDefenseModalsAndHud();
            BuildQuickListModal();
            BuildCitySelectAndDriveHud();
            BuildFishingPushInHud();
            BuildComicDialogOverlay();
            BuildItemAndSourcePopups(); // 置于顶层，可直接叠在建造确认卡之上！

            _worldGlove = new ComicGlovePointer(_worldTrackLayer.transform, "WorldGlove");
            _uiGlove = new ComicGlovePointer(_canvasRt, "UiGlove");
            BuildBootLoadingOverlay(); // 最顶层启动 Loading 页（含《末日囤货狂》主视觉 + 进度条 + 健康游戏忠告全文）

            _sim.OnResourceGained += EnqueueCenterPickupToast;
            _sim.OnXpBurst += SpawnXpFlyStar;
            _sim.OnWorldToast += SpawnWorldRisingText;
            _sim.OnCampLevelUp += ShowLevelUpModal;
            _sim.OnHeroUnlocked += ShowHeroSpotlight;
            _sim.OnHordeCrisisTriggered += () => { if (!_sim.HordePosterShown && (_world == null || !_world.InWaveDefense)) OpenCrisisPoster(); };
            _sim.OnPlayComicDialog += EnqueueComicDialog;

            _world.OnNodeClicked += HandleNodeClicked;
            _world.OnResidentDemandClicked += OpenResidentDemandDrawer;
            _world.OnEmptyGroundClicked += () =>
            {
                _footMenuNode = null;
                if (_footMenuRt != null) _footMenuRt.gameObject.SetActive(false);
            };
            _world.OnCustomToast += SpawnWorldRisingText;
            _world.OnCombatDamageNumber += SpawnFloatingDamageNumber;
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

        readonly List<RaycastResult> _uiRaycastHits = new List<RaycastResult>(16);

        public bool IsPointerOverUi()
        {
            if (EventSystem.current == null) return false;
            Vector2 pos = Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
            var ped = new PointerEventData(EventSystem.current) { position = pos };
            _uiRaycastHits.Clear();
            EventSystem.current.RaycastAll(ped, _uiRaycastHits);
            return _uiRaycastHits.Count > 0;
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
            Vector3 vp = _cam.WorldToViewportPoint(worldPos);
            Rect r = _canvasRt.rect;
            float w = r.width > 1f ? r.width : 720f;
            float h = r.height > 1f ? r.height : 1280f;
            return new Vector2((vp.x - 0.5f) * w, (vp.y - 0.5f) * h);
        }

        // =====================================================================
        // 1. 世界追踪层
        // =====================================================================
        void BuildWorldTrackingLayer()
        {
            _worldTrackLayer = ForestUiWidgets.Stretch("WorldTrackLayer", _canvasRt).gameObject;
            for (int i = 0; i < 10; i++)
                _checkMarks.Add(CreateTrackItem("CheckMark_" + i, new Vector2(46f, 46f), false));
            for (int i = 0; i < 32; i++)
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

            // 适配刘海屏/灵动岛与微信右上角胶囊安全区
            float safeTopPx = Mathf.Max(0f, Screen.height - Screen.safeArea.yMax);
            float topPad = Mathf.Max(EditorTopSafePadOverride, Mathf.Clamp(safeTopPx * (1280f / Mathf.Max(1f, Screen.height)), 0f, 96f));
#if !UNITY_EDITOR && (UNITY_WEBGL || PLATFORM_WEIXINMINIGAME)
            topPad = Mathf.Max(topPad, 56f);
#endif
            _topSafePad = topPad;

            // --- 左上角主角头像框（点击打开骨干委任与换装） ---
            Image avatarFrame = ForestUiWidgets.MakeSlicedImage("AvatarFrame", root, ForestComicUiSkin.DialogOuter, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -14f - topPad), new Vector2(84f, 84f), true);
            avatarFrame.gameObject.AddComponent<Button>().onClick.AddListener(OpenHeroAssignModal);
            Image avatarInner = ForestUiWidgets.MakeSlicedImage("AvatarBg", avatarFrame.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-10f, -10f), false);
            avatarInner.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            ForestUiWidgets.MakeIcon("LeaderBust", avatarInner.transform, ForestComicUiSkin.GetArt("bust_leader", ForestComicUiSkin.RedDotBadge),
                new Vector2(0f, -8f), new Vector2(86f, 86f));

            // --- 左上紧凑 3 排状态栏（全胶囊右边缘严格控制在 x <= 396f，100% 避让微信右上角游戏圈与胶囊按钮！） ---
            // 第一排 (y = -14 - topPad)：星级经验条 + 双轨人口胶囊 (当前人口/供暖上限)
            Image xpBg = ForestUiWidgets.MakeSlicedImage("XpPill", root, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(108f, -14f - topPad), new Vector2(148f, 34f), false);
            RectTransform xpFillMask = ForestUiWidgets.MakeRect("XpFillArea", xpBg.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-10f, -10f));
            _xpFillImg = xpFillMask.gameObject.AddComponent<Image>();
            _xpFillImg.sprite = ForestComicUiSkin.BtnOrange;
            _xpFillImg.type = Image.Type.Filled;
            _xpFillImg.fillMethod = Image.FillMethod.Horizontal;
            _xpFillImg.raycastTarget = false;
            _xpBarTxt = ForestUiWidgets.MakeText("XpTxt", xpBg.transform, "0/70", 15, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(12f, 0f), Vector2.zero);

            Image starIcon = ForestUiWidgets.MakeIcon("LvStar", xpBg.transform, ForestComicUiSkin.GetArt("ico_xp_star", ForestComicUiSkin.RedDotBadge),
                new Vector2(-66f, 1f), new Vector2(44f, 44f));
            _lvStarTxt = ForestUiWidgets.MakeText("LvNum", starIcon.transform, "1", 20, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -1f), Vector2.zero);

            Image popBg = ForestUiWidgets.MakeSlicedImage("PopPill", root, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(262f, -14f - topPad), new Vector2(120f, 34f), true);
            popBg.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenSourceRouteForCondition(CondKind.PopCur, SurvRes.Wood));
            ForestUiWidgets.MakeIcon("PopIco", popBg.transform, ForestComicUiSkin.GetArt("bust_cook", ForestComicUiSkin.RedDotBadge),
                new Vector2(-38f, 0f), new Vector2(28f, 28f));
            _popTxt = ForestUiWidgets.MakeText("PopTxt", popBg.transform, "4/8", 16, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(12f, 0f), Vector2.zero);

            // 第二排 (y = -54 - topPad)：营地铜币胶囊 + 工程手稿(蓝图)胶囊（点击直接弹【获取途径】！）
            Image coinBg = ForestUiWidgets.MakeSlicedImage("CoinPill", root, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(108f, -54f - topPad), new Vector2(126f, 34f), true);
            coinBg.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenSourceRouteForCondition(CondKind.Coins, SurvRes.Wood));
            ForestUiWidgets.MakeIcon("CoinIco", coinBg.transform, ForestComicUiSkin.GetArt("ico_coin", ForestComicUiSkin.RedDotBadge),
                new Vector2(-41f, 0f), new Vector2(26f, 26f));
            _coinTxt = ForestUiWidgets.MakeText("CoinTxt", coinBg.transform, "120", 16, ForestUiWidgets.WarmGold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(11f, 0f), Vector2.zero);

            Image bpBg = ForestUiWidgets.MakeSlicedImage("BlueprintPill", root, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(240f, -54f - topPad), new Vector2(114f, 34f), true);
            bpBg.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenSourceRouteForCondition(CondKind.Resource, SurvRes.Blueprint));
            ForestUiWidgets.MakeIcon("BpIco", bpBg.transform, ForestComicUiSkin.ResIcon(SurvRes.Blueprint),
                new Vector2(-34f, 0f), new Vector2(25f, 25f));
            _blueprintTxt = ForestUiWidgets.MakeText("BpTxt", bpBg.transform, "0", 16, new Color32(138, 206, 255, 255), TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(11f, 0f), Vector2.zero);

            // 第三排 (y = -94 - topPad)：仓库储量胶囊 + 温饱士气胶囊（右沿 396f）
            Image capBg = ForestUiWidgets.MakeSlicedImage("CapPill", root, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(108f, -94f - topPad), new Vector2(130f, 34f), true);
            capBg.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenQuickBuildList(showWarehouse: true));
            ForestUiWidgets.MakeIcon("CapIco", capBg.transform, ForestComicUiSkin.GetArt("dock_bag", ForestComicUiSkin.RedDotBadge),
                new Vector2(-43f, 0f), new Vector2(26f, 26f));
            _storageCapTxt = ForestUiWidgets.MakeText("CapTxt", capBg.transform, "仓10/80", 15, new Color32(216, 238, 255, 255), TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(12f, 0f), Vector2.zero);

            Image moraleBg = ForestUiWidgets.MakeSlicedImage("MoralePill", root, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(244f, -94f - topPad), new Vector2(152f, 34f), true);
            moraleBg.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                SpawnScreenCenterBanner($"营地状态：{_sim.MoraleStatusSummary}（炊事房存足口粮+草药坊备药可维持最高+32%全营提速！）");
            });
            ForestUiWidgets.MakeIcon("MoraleIco", moraleBg.transform, ForestComicUiSkin.ResIcon(SurvRes.Ration),
                new Vector2(-55f, 0f), new Vector2(24f, 24f));
            _moralePillTxt = ForestUiWidgets.MakeText("MoraleTxt", moraleBg.transform, "温饱+20%", 14, ForestUiWidgets.LushGreen, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(11f, 0f), Vector2.zero);

            // --- 右侧常驻【居民生计订单】快捷挂件（统一置于 y <= -132，彻底避让微信右上角 126px 游戏圈与胶囊安全区！） ---
            float rightOrderTop = Mathf.Min(-132f, -16f - topPad - 62f);
            Image orderEntry = ForestUiWidgets.MakeSlicedImage("RightOrderEntry", root, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, rightOrderTop), new Vector2(138f, 74f), true);
            _rightOrderEntryRt = orderEntry.rectTransform;
            orderEntry.gameObject.AddComponent<Button>().onClick.AddListener(OpenResidentDemandDrawer);
            ForestUiWidgets.MakeIcon("OrdBust", orderEntry.transform, ForestComicUiSkin.GetArt("bust_laochentou", ForestComicUiSkin.GetArt("bust_cook")),
                new Vector2(-40f, 2f), new Vector2(52f, 52f));
            ForestUiWidgets.MakeText("OrdTitle", orderEntry.transform, "居民订单", 17, ForestUiWidgets.WarmGold, TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(22f, -10f), new Vector2(-52f, -12f));
            _orderBadgeCntTxt = ForestUiWidgets.MakeText("OrdSub", orderEntry.transform, "赚铜币XP", 14, Color.white, TextAnchor.LowerLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(22f, 10f), new Vector2(-52f, -12f));
            _orderRightDot = ForestUiWidgets.MakeIcon("Dot", orderEntry.transform, ForestComicUiSkin.RedDotBadge, new Vector2(58f, 26f), new Vector2(22f, 22f)).gameObject;

            // --- 左侧精简核心资源追踪栈（置于头像与第3排下方 y = -136 - topPad 起，零重叠） ---
            for (int i = 0; i < HudPinnedRes.Length; i++)
            {
                SurvRes r = HudPinnedRes[i];
                Image pill = ForestUiWidgets.MakeSlicedImage("ResPill_" + r, root, ForestComicUiSkin.DarkPill, Color.white,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -136f - topPad - i * 36f), new Vector2(114f, 33f), false);
                ForestUiWidgets.MakeIcon("Ico", pill.transform, ForestComicUiSkin.ResIcon(r), new Vector2(-36f, 0f), new Vector2(27f, 27f));
                Text valTxt = ForestUiWidgets.MakeText("Val", pill.transform, "0", 17, Color.white, TextAnchor.MiddleLeft,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(18f, 0f), new Vector2(-36f, 0f));
                _resPillRts[r] = pill.rectTransform;
                _resPillTxts[r] = valTxt;
                _resPulseTimers[r] = 0f;
            }

            // --- 左侧尸潮来袭倒计时挂件 ---
            Image hordeBg = ForestUiWidgets.MakeSlicedImage("HordeLeftBadge", root, ForestComicUiSkin.DarkPill, new Color(1f, 0.85f, 0.85f, 1f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -368f - topPad), new Vector2(138f, 58f), true);
            hordeBg.gameObject.AddComponent<Button>().onClick.AddListener(OpenWavePrepModal);
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
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(14f, 122f), new Vector2(476f, 72f), true);
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
                new Vector2(-202f, 4f), new Vector2(54f, 54f));
            _questCheckImg = ForestUiWidgets.MakeIcon("DoneCheck", clipImg.transform, ForestComicUiSkin.GetArt("ico_check"),
                new Vector2(14f, -12f), new Vector2(28f, 28f));
            _questTitleTxt = ForestUiWidgets.MakeText("QuestTxt", _questCardBg.transform, "加载主线任务...", 18, Color.white, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(26f, 2f), new Vector2(-80f, -12f));

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
                _dockBtns[i] = btn;

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
        // 5B. 【生态选种农田】与【山林畜牧栏】双半屏联动管理面板（选种·施肥·投喂·水暖士气联动可视化）
        // =====================================================================
        void BuildFarmAndRanchDrawers()
        {
            // ---------------- A. 生态选种农田半屏面板 (_farmDrawer) ----------------
            _farmDrawer = ForestUiWidgets.Stretch("FarmSeedDrawerRoot", _canvasRt).gameObject;
            Image fmDismiss = ForestUiWidgets.MakeSlicedImage("TopDismiss", _farmDrawer.transform, null, new Color(0, 0, 0, 0.01f),
                new Vector2(0f, 0.58f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true);
            fmDismiss.gameObject.AddComponent<Button>().onClick.AddListener(CloseAllModals);

            Image fmSheet = ForestUiWidgets.MakeSlicedImage("BottomSheet", _farmDrawer.transform, ForestComicUiSkin.DialogOuter, Color.white,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 736f), true);

            _fmTitleTxt = ForestUiWidgets.MakeText("Title", fmSheet.transform, "生态选种农田 Lv.1 (2/4垄)", 24, Color.white, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(24f, -12f), new Vector2(-180f, 36f));

            _fmUpgradeBtn = ForestUiWidgets.MakeJellyButton("BtnUpFarm", fmSheet.transform, ForestComicUiSkin.BtnOrange, "升级扩垄", 16,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -12f), new Vector2(120f, 38f), () =>
                {
                    CampNode fn = _sim.Find("farm_a");
                    CloseAllModals();
                    if (fn != null) OpenBuildConfirmCard(fn);
                }, out _);

            // 跨建筑联动实时增益横幅（点击可跳转石井/畜牧栏查看）
            Image synBar = ForestUiWidgets.MakeSlicedImage("SynergyBar", fmSheet.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(684f, 38f), true);
            synBar.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                if (!_sim.HasAutoIrrigation) JumpToNodeById("relic_well");
                else JumpToNodeById("beast_ranch");
            });
            _fmSynergyTxt = ForestUiWidgets.MakeText("SynTxt", synBar.transform, "", 14, ForestUiWidgets.WarmGold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-14f, 0f));

            // 4 块独立田垄槽位卡
            for (int i = 0; i < 4; i++)
            {
                int pIdx = i;
                float x = -252f + i * 168f;
                Image pCard = ForestUiWidgets.MakeSlicedImage("FarmPlot_" + i, fmSheet.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -104f), new Vector2(158f, 242f), false);
                _fmPlotBgs[i] = pCard;

                ForestUiWidgets.MakeText("Hdr", pCard.transform, $"{i + 1}号田垄", 16, ForestUiWidgets.InkDark, TextAnchor.UpperCenter,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(0f, 24f), outline: false);

                _fmPlotIcons[i] = ForestUiWidgets.MakeIcon("Ico", pCard.transform, ForestComicUiSkin.ResIcon(SurvRes.SeedVeggie), new Vector2(0f, 34f), new Vector2(64f, 64f));

                Image barBg = ForestUiWidgets.MakeSlicedImage("ProgBg", pCard.transform, ForestComicUiSkin.DarkPill, Color.white,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), new Vector2(136f, 22f), false);
                RectTransform fRt = ForestUiWidgets.MakeRect("Fill", barBg.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6f, -6f));
                _fmPlotProgressFills[i] = fRt.gameObject.AddComponent<Image>();
                _fmPlotProgressFills[i].sprite = ForestPlaceholderArt.Solid(new Color32(62, 186, 72, 235));
                _fmPlotProgressFills[i].type = Image.Type.Filled;
                _fmPlotProgressFills[i].fillMethod = Image.FillMethod.Horizontal;
                _fmPlotStateTxts[i] = ForestUiWidgets.MakeText("St", barBg.transform, "待播种", 13, Color.white, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

                _fmPlotSubBtns[i] = ForestUiWidgets.MakeJellyButton("BtnSub_" + i, pCard.transform, ForestComicUiSkin.BtnOrange, "施肥催熟", 14,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 54f), new Vector2(136f, 34f),
                    () => OnFarmPlotSubAction(pIdx), out _fmPlotSubLbls[i]);

                _fmPlotMainBtns[i] = ForestUiWidgets.MakeJellyButton("BtnMain_" + i, pCard.transform, ForestComicUiSkin.BtnGreen, "播种", 16,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(138f, 38f),
                    () => OnFarmPlotMainAction(pIdx), out _fmPlotMainLbls[i]);
            }

            // 下半部：4 种作物种子选种栏
            Image seedBox = ForestUiWidgets.MakeSlicedImage("SeedSelectorBox", fmSheet.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -358f), new Vector2(684f, 272f), false);

            ForestUiWidgets.MakeText("SeedHdr", seedBox.transform, "选择播种作物（收获100%保底返种 · 砍树采果掉种 · 畜牧堆肥翻倍丰产）", 15, ForestUiWidgets.InkDark, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-16f, 24f), outline: false);

            string[] seedShortNames = { "山薯鲜蔬种", "韧性亚麻种", "深山草药种", "变异黄金种" };
            for (int i = 0; i < 4; i++)
            {
                CropSeedKind sk = (CropSeedKind)i;
                float x = -243f + i * 162f;
                Image sSlot = ForestUiWidgets.MakeSlicedImage("Seed_" + i, seedBox.transform, ForestComicUiSkin.SubSlot, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -42f), new Vector2(150f, 156f), true);
                sSlot.gameObject.AddComponent<Button>().onClick.AddListener(() => { _selectedSeedKind = sk; RefreshFarmDrawer(); });
                _fmSeedFrames[i] = sSlot;

                ForestUiWidgets.MakeIcon("Ico", sSlot.transform, ForestComicUiSkin.ResIcon(ForestSurvivalSim.SeedResFor(sk)), new Vector2(0f, 22f), new Vector2(66f, 66f));
                ForestUiWidgets.MakeText("Nm", sSlot.transform, seedShortNames[i], 15, ForestUiWidgets.InkDark, TextAnchor.MiddleCenter,
                    new Vector2(0f, 0f), new Vector2(1f, 0.45f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), Vector2.zero, outline: false);
                _fmSeedCntTxts[i] = ForestUiWidgets.MakeText("Cnt", sSlot.transform, "库存: 3", 14, ForestUiWidgets.LushGreen, TextAnchor.LowerCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), Vector2.zero, outline: false);
            }

            _fmYieldPreviewTxt = ForestUiWidgets.MakeText("YieldPrev", seedBox.transform, "", 15, ForestUiWidgets.InkDark, TextAnchor.LowerCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(-24f, 44f), outline: false);

            ForestUiWidgets.MakeRedBackButton(fmSheet.transform, CloseAllModals, new Vector2(20f, 16f));

            ForestUiWidgets.MakeJellyButton("BtnAutoReplant", fmSheet.transform, ForestComicUiSkin.BtnBlue, "自动连种:开", 16,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-118f, 20f), new Vector2(156f, 54f),
                () => { _sim.AutoReplantFarm = !_sim.AutoReplantFarm; RefreshFarmDrawer(); }, out _fmAutoLbl);

            _fmMainBtn = ForestUiWidgets.MakeJellyButton("BtnFarmBatch", fmSheet.transform, ForestComicUiSkin.BtnGreen, "一键全播", 22,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(108f, 18f), new Vector2(236f, 60f),
                OnFarmBatchActionClicked, out _fmMainBtnLbl);

            _farmDrawer.SetActive(false);

            // ---------------- B. 山林畜牧栏半屏面板 (_ranchDrawer) ----------------
            _ranchDrawer = ForestUiWidgets.Stretch("BeastRanchDrawerRoot", _canvasRt).gameObject;
            Image rnDismiss = ForestUiWidgets.MakeSlicedImage("TopDismiss", _ranchDrawer.transform, null, new Color(0, 0, 0, 0.01f),
                new Vector2(0f, 0.58f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true);
            rnDismiss.gameObject.AddComponent<Button>().onClick.AddListener(CloseAllModals);

            Image rnSheet = ForestUiWidgets.MakeSlicedImage("BottomSheet", _ranchDrawer.transform, ForestComicUiSkin.DialogOuter, Color.white,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 736f), true);

            _rnTitleTxt = ForestUiWidgets.MakeText("Title", rnSheet.transform, "山林畜牧栏 Lv.1 (2/4栏)", 24, Color.white, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(24f, -12f), new Vector2(-180f, 36f));

            _rnUpgradeBtn = ForestUiWidgets.MakeJellyButton("BtnUpRanch", rnSheet.transform, ForestComicUiSkin.BtnOrange, "升级扩栏", 16,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -12f), new Vector2(120f, 38f), () =>
                {
                    CampNode rn = _sim.Find("beast_ranch");
                    CloseAllModals();
                    if (rn != null) OpenBuildConfirmCard(rn);
                }, out _);

            Image rnSynBar = ForestUiWidgets.MakeSlicedImage("SynergyBar", rnSheet.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(684f, 38f), true);
            rnSynBar.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenFarmDrawer(CropSeedKind.Veggie));
            _rnSynergyTxt = ForestUiWidgets.MakeText("SynTxt", rnSynBar.transform, "", 14, ForestUiWidgets.WarmGold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-14f, 0f));

            // 4 个动物饲养栏位
            for (int i = 0; i < 4; i++)
            {
                int penIdx = i;
                float x = -252f + i * 168f;
                Image pCard = ForestUiWidgets.MakeSlicedImage("RanchPen_" + i, rnSheet.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -104f), new Vector2(158f, 224f), false);
                _rnPenBgs[i] = pCard;

                ForestUiWidgets.MakeText("Hdr", pCard.transform, $"{i + 1}号兽栏", 16, ForestUiWidgets.InkDark, TextAnchor.UpperCenter,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(0f, 24f), outline: false);

                _rnPenIcons[i] = ForestUiWidgets.MakeIcon("Ico", pCard.transform, ForestComicUiSkin.ResIcon(SurvRes.Pelt), new Vector2(0f, 28f), new Vector2(64f, 64f));

                Image barBg = ForestUiWidgets.MakeSlicedImage("ProgBg", pCard.transform, ForestComicUiSkin.DarkPill, Color.white,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(136f, 22f), false);
                RectTransform fRt = ForestUiWidgets.MakeRect("Fill", barBg.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6f, -6f));
                _rnPenProgressFills[i] = fRt.gameObject.AddComponent<Image>();
                _rnPenProgressFills[i].sprite = ForestPlaceholderArt.Solid(new Color32(226, 138, 36, 235));
                _rnPenProgressFills[i].type = Image.Type.Filled;
                _rnPenProgressFills[i].fillMethod = Image.FillMethod.Horizontal;
                _rnPenStateTxts[i] = ForestUiWidgets.MakeText("St", barBg.transform, "待投喂", 13, Color.white, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

                _rnPenBtns[i] = ForestUiWidgets.MakeJellyButton("BtnAct_" + i, pCard.transform, ForestComicUiSkin.BtnGreen, "投喂", 15,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(138f, 40f),
                    () => OnRanchPenAction(penIdx), out _rnPenBtnLbls[i]);
            }

            // 下半部：3 种饲养禽兽品种选择 + 一键去农田施肥联动钮
            Image breedBox = ForestUiWidgets.MakeSlicedImage("BreedSelectorBox", rnSheet.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -340f), new Vector2(684f, 290f), false);

            ForestUiWidgets.MakeText("BreedHdr", breedBox.transform, "选择饲养品种（每栏投喂消耗：农田鲜蔬×2 + 石井净水×1 → 产鲜肉+毛皮+有机堆肥）", 15, ForestUiWidgets.InkDark, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-16f, 24f), outline: false);

            string[] breedIcons = { "ico_pelt", "boar", "deer" };
            for (int i = 0; i < 3; i++)
            {
                RanchAnimalKind ak = (RanchAnimalKind)i;
                float x = -212f + i * 212f;
                Image bSlot = ForestUiWidgets.MakeSlicedImage("Breed_" + i, breedBox.transform, ForestComicUiSkin.SubSlot, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -42f), new Vector2(198f, 152f), true);
                bSlot.gameObject.AddComponent<Button>().onClick.AddListener(() => { _selectedRanchAnimal = ak; RefreshRanchDrawer(); });
                _rnBreedFrames[i] = bSlot;

                Sprite bSp = ForestComicUiSkin.GetArt(breedIcons[i], ForestComicUiSkin.ResIcon(i == 1 ? SurvRes.Meat : SurvRes.Pelt));
                ForestUiWidgets.MakeIcon("Ico", bSlot.transform, bSp, new Vector2(0f, 24f), new Vector2(68f, 68f));
                ForestUiWidgets.MakeText("Nm", bSlot.transform, ForestSurvivalSim.RanchAnimalName(ak), 17, ForestUiWidgets.InkDark, TextAnchor.MiddleCenter,
                    new Vector2(0f, 0f), new Vector2(1f, 0.46f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), Vector2.zero, outline: false);
                _rnBreedSubTxts[i] = ForestUiWidgets.MakeText("Sub", bSlot.transform, "", 13, ForestUiWidgets.InkMuted, TextAnchor.LowerCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(-8f, 0f), outline: false);
            }

            _rnBreedPreviewTxt = ForestUiWidgets.MakeText("BreedPrev", breedBox.transform, "", 15, ForestUiWidgets.InkDark, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0.72f, 0f), new Vector2(0f, 0f), new Vector2(18f, 14f), new Vector2(0f, 44f), outline: false);

            ForestUiWidgets.MakeJellyButton("BtnGoFertilizeFarm", breedBox.transform, ForestComicUiSkin.BtnOrange, "去农田撒肥>>", 15,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 16f), new Vector2(158f, 42f),
                () => OpenFarmDrawer(), out _);

            ForestUiWidgets.MakeRedBackButton(rnSheet.transform, CloseAllModals, new Vector2(20f, 16f));

            ForestUiWidgets.MakeJellyButton("BtnAutoFeed", rnSheet.transform, ForestComicUiSkin.BtnBlue, "自动续喂:开", 16,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-118f, 20f), new Vector2(156f, 54f),
                () => { _sim.AutoFeedRanch = !_sim.AutoFeedRanch; RefreshRanchDrawer(); }, out _rnAutoLbl);

            _rnMainBtn = ForestUiWidgets.MakeJellyButton("BtnRanchBatch", rnSheet.transform, ForestComicUiSkin.BtnGreen, "一键全喂", 22,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(108f, 18f), new Vector2(236f, 60f),
                OnRanchBatchActionClicked, out _rnMainBtnLbl);

            _ranchDrawer.SetActive(false);
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
                    if (k != null && k.state == NodeState.Active) OpenWorkshopDrawer(k);
                    else JumpToNodeById("kitchen");
                }, out _);

            Image t2 = ForestUiWidgets.MakeSlicedImage("PrepRow2", _crisisModal.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 280f), new Vector2(636f, 68f), false);
            ForestUiWidgets.MakeIcon("Ico", t2.transform, ForestComicUiSkin.GetArt("hunter_tower", ForestComicUiSkin.RedDotBadge), new Vector2(-276f, 0f), new Vector2(48f, 48f));
            _crisisTask2Txt = ForestUiWidgets.MakeText("Txt", t2.transform, "建造捕猎哨塔或尖木拒马 (0/1)", 20, Color.white, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(16f, 0f), new Vector2(-180f, 0f));
            ForestUiWidgets.MakeJellyButton("Go2", t2.transform, ForestComicUiSkin.BtnBlue, "前往", 19,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(96f, 46f), () =>
                {
                    JumpToNodeById("barricade");
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
                    StartWaveDefenseBattle();
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
        // 8. 等级提升解锁弹窗、新英雄高光、水世界式 6 岗位多槽委任 & 养成面板、山口波次迎击 UI
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

        static RectTransform MakeVerticalScrollArea(string name, Transform parent, Vector2 anchoredPos, Vector2 size, float contentHeight)
        {
            RectTransform viewRt = ForestUiWidgets.MakeRect(name, parent,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPos, size);
            var maskImg = viewRt.gameObject.AddComponent<Image>();
            maskImg.color = new Color(1f, 1f, 1f, 0.01f);
            viewRt.gameObject.AddComponent<RectMask2D>();

            RectTransform contentRt = ForestUiWidgets.MakeRect("Content", viewRt,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, contentHeight));

            var sr = viewRt.gameObject.AddComponent<ScrollRect>();
            sr.viewport = viewRt;
            sr.content = contentRt;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 28f;
            return contentRt;
        }

        void BuildHeroModals()
        {
            _heroSpotlightModal = MakeModalMask("HeroSpotlightModal", 0.84f, CloseAllModals);
            _heroSpotSunburst = ForestUiWidgets.MakeIcon("Sunburst", _heroSpotlightModal.transform, ForestComicUiSkin.Sunburst, new Vector2(0f, 40f), new Vector2(680f, 680f));
            ForestUiWidgets.MakeText("Hdr", _heroSpotlightModal.transform, "获得新骨干伙伴", 46, ForestUiWidgets.WarmGold, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(0f, 62f));
            _heroSpotNameTxt = ForestUiWidgets.MakeText("Name", _heroSpotlightModal.transform, "大锤", 34, Color.white, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -220f), new Vector2(0f, 44f));
            _heroSpotRoleTxt = ForestUiWidgets.MakeText("Role", _heroSpotlightModal.transform, "重装护卫 · 传说", 20, new Color32(150, 220, 255, 255), TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -264f), new Vector2(0f, 32f));
            _heroSpotBust = ForestUiWidgets.MakeIcon("HeroBust", _heroSpotlightModal.transform, ForestComicUiSkin.GetArt("bust_driver"), new Vector2(0f, 20f), new Vector2(310f, 310f));
            _heroSpotBuffTxt = ForestUiWidgets.MakeText("Buff", _heroSpotlightModal.transform, "加成说明", 22, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 240f), new Vector2(580f, 44f));
            ForestUiWidgets.MakeJellyButton("BtnGreat", _heroSpotlightModal.transform, ForestComicUiSkin.BtnOrange, "太棒了！", 25,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(220f, 66f), CloseAllModals, out _);
            _heroSpotlightModal.SetActive(false);

            // 双页签主面板：【英雄委任 (6大岗位多槽)】 & 【英雄养成 (升级·升星·3档防具换装)】
            _heroAssignModal = MakeModalMask("HeroAssignModal", 0.65f, CloseAllModals);
            Image sheet = ForestUiWidgets.MakeSlicedImage("Sheet", _heroAssignModal.transform, ForestComicUiSkin.DialogOuter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), new Vector2(656f, 956f), true);

            _haHeaderTitleTxt = ForestUiWidgets.MakeText("Title", sheet.transform, "骨干委任与养成", 26, Color.white, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(0f, 34f));
            ForestUiWidgets.MakeJellyButton("CloseTop", sheet.transform, ForestComicUiSkin.BtnRed, "✕", 21,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -10f), new Vector2(44f, 44f), CloseAllModals, out _);

            Image leaderBanner = ForestUiWidgets.MakeSlicedImage("LeaderNotePill", sheet.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(616f, 32f), false);
            _haLeaderBannerTxt = ForestUiWidgets.MakeText("LeaderNoteTxt", leaderBanner.transform,
                "领主【阿澈】等级即左上角玩家等级 (Lv.1) · 招募幸存者骨干负责驻守与闯关出战", 14, ForestUiWidgets.WarmGold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16f, 0f));

            _haTabBtn0 = ForestUiWidgets.MakeJellyButton("TabDelegation", sheet.transform, ForestComicUiSkin.BtnOrange, "骨干委任 (6岗)", 19,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-118f, -88f), new Vector2(218f, 44f),
                () => SwitchHeroModalTab(0), out _haTabLbl0);
            _haTabBtn1 = ForestUiWidgets.MakeJellyButton("TabGrowth", sheet.transform, ForestComicUiSkin.BtnBlue, "骨干养成·换装", 19,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(118f, -88f), new Vector2(218f, 44f),
                () => SwitchHeroModalTab(1), out _haTabLbl1);

            Image paper = ForestUiWidgets.MakeSlicedImage("Paper", sheet.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -138f), new Vector2(616f, 684f), false);

            // -------------------- Page 0: 水世界式 6 大岗位多槽委任卡 --------------------
            _haDelegationPage = ForestUiWidgets.Stretch("DelegationPage", paper.transform).gameObject;
            RectTransform delContent = MakeVerticalScrollArea("DelScroll", _haDelegationPage.transform, Vector2.zero, new Vector2(598f, 668f), 756f);

            for (int p = 0; p < 6 && p < _sim.DelegationPosts.Count; p++)
            {
                int postIdx = p;
                DelegationPostState st = _sim.DelegationPosts[p];
                Image card = ForestUiWidgets.MakeSlicedImage("PostCard_" + p, delContent, ForestComicUiSkin.SubSlot, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f - p * 123f), new Vector2(582f, 116f), false);

                ForestUiWidgets.MakeText("PostTitle", card.transform, st.title, 18, ForestUiWidgets.InkDark, TextAnchor.UpperLeft,
                    new Vector2(0f, 1f), new Vector2(0.68f, 1f), new Vector2(0f, 1f), new Vector2(14f, -6f), new Vector2(0f, 24f), outline: false);
                _delPostEffectTxts[p] = ForestUiWidgets.MakeText("PostEff", card.transform, "提速 +0%", 17, ForestUiWidgets.LushGreen, TextAnchor.UpperRight,
                    new Vector2(0.62f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -6f), new Vector2(0f, 24f), outline: false);

                // 左侧岗位建筑缩略图框
                Image bldBox = ForestUiWidgets.MakeSlicedImage("PostIconBox", card.transform, ForestComicUiSkin.ParchmentPanel, new Color(0.92f, 0.94f, 0.98f, 1f),
                    new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(12f, 8f), new Vector2(82f, 76f), false);
                ForestUiWidgets.MakeIcon("PostIcon", bldBox.transform, ForestComicUiSkin.GetArt(st.iconKey, ForestComicUiSkin.GetArt("sawmill_shed")),
                    Vector2.zero, new Vector2(68f, 68f));

                // 未达首槽玩家星级时：居中显示“玩家X级解锁”（还原水世界清爽未解锁态！）
                GameObject lockHintGo = ForestUiWidgets.MakeRect("LockedPostHint", card.transform,
                    new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(44f, -10f), new Vector2(-110f, -30f)).gameObject;
                _delPostLockedHintGos[p] = lockHintGo;
                _delPostLockedHintTxts[p] = ForestUiWidgets.MakeText("Txt", lockHintGo.transform, $"玩家 {st.slotUnlockLevels[0]} 级解锁",
                    21, ForestUiWidgets.InkMuted, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: false);

                // 右侧 2~4 个英雄委任槽位
                for (int s = 0; s < 4; s++)
                {
                    int slotIdx = s;
                    float sx = 112f + s * 114f;
                    Image slotFrame = ForestUiWidgets.MakeSlicedImage($"Slot_{p}_{s}", card.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                        new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(sx, 8f), new Vector2(104f, 78f), true);
                    _delSlotBoxes[p, s] = slotFrame.gameObject;
                    _delSlotFrames[p, s] = slotFrame;
                    slotFrame.gameObject.AddComponent<Button>().onClick.AddListener(() => OnDelegationSlotClicked(postIdx, slotIdx));

                    Image maskInner = ForestUiWidgets.MakeSlicedImage("Mask", slotFrame.transform, ForestComicUiSkin.SubSlot, new Color(1f, 1f, 1f, 0.15f),
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(92f, 52f), false);
                    maskInner.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                    _delSlotBusts[p, s] = ForestUiWidgets.MakeIcon("Bust", maskInner.transform, ForestComicUiSkin.GetArt("bust_dachui"),
                        new Vector2(0f, -4f), new Vector2(62f, 62f));

                    _delSlotLocks[p, s] = ForestUiWidgets.MakeIcon("Lock", slotFrame.transform, ForestComicUiSkin.GetArt("ico_lock"),
                        new Vector2(0f, 8f), new Vector2(32f, 32f));
                    _delSlotStarTags[p, s] = ForestUiWidgets.MakeText("Stars", slotFrame.transform, "★1", 13, ForestUiWidgets.WarmGold, TextAnchor.UpperRight,
                        Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-4f, -3f), new Vector2(-8f, -6f));
                    _delSlotSubTxts[p, s] = ForestUiWidgets.MakeText("Sub", slotFrame.transform, "+14%", 14, ForestUiWidgets.InkDark, TextAnchor.LowerCenter,
                        Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), new Vector2(-4f, -4f), outline: false);
                }
            }

            // -------------------- Page 1: 6 名骨干英雄升级·升星·防具换装 --------------------
            _haGrowthPage = ForestUiWidgets.Stretch("GrowthPage", paper.transform).gameObject;
            Image resBar = ForestUiWidgets.MakeSlicedImage("GrowthResBar", _haGrowthPage.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(588f, 38f), false);
            _haGrowthResTxt = ForestUiWidgets.MakeText("Txt", resBar.transform, "铜币: 120   ·   熟食口粮: 6   ·   英雄勋章: 0", 16, ForestUiWidgets.WarmGold,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            RectTransform grContent = MakeVerticalScrollArea("GrowthScroll", _haGrowthPage.transform, new Vector2(0f, -22f), new Vector2(598f, 618f), 812f);
            for (int i = 0; i < 6; i++)
            {
                int rowIdx = i;
                Image row = ForestUiWidgets.MakeSlicedImage("HeroRow_" + i, grContent, ForestComicUiSkin.SubSlot, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f - i * 133f), new Vector2(582f, 126f), false);
                _haRows[i] = row.gameObject;

                Image box = ForestUiWidgets.MakeSlicedImage("PortraitBox", row.transform, ForestComicUiSkin.BtnOrange, Color.white,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 2f), new Vector2(86f, 104f), false);
                _haRarityFrames[i] = box;
                Image pInner = ForestUiWidgets.MakeSlicedImage("Inner", box.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(74f, 72f), false);
                pInner.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                _haBusts[i] = ForestUiWidgets.MakeIcon("Bust", pInner.transform, ForestComicUiSkin.GetArt("bust_dachui"), new Vector2(0f, -4f), new Vector2(78f, 78f));
                _haStarBadges[i] = ForestUiWidgets.MakeText("StarLv", box.transform, "★1 Lv.1", 13, Color.white, TextAnchor.LowerCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), Vector2.zero);

                _haNames[i] = ForestUiWidgets.MakeText("Name", row.transform, "大锤 [传说] · 战力 303", 17, ForestUiWidgets.InkDark, TextAnchor.UpperLeft,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(102f, -8f), new Vector2(-336f, 26f), outline: false);
                _haDescs[i] = ForestUiWidgets.MakeText("Desc", row.transform, "绝技：路牌重盾震击", 13, ForestUiWidgets.InkMuted, TextAnchor.UpperLeft,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(102f, -34f), new Vector2(-336f, 42f), outline: false);

                _haLvUpBtns[i] = ForestUiWidgets.MakeJellyButton($"LvUp_{i}", row.transform, ForestComicUiSkin.BtnGreen, "升级", 14,
                    new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-120f, -8f), new Vector2(106f, 46f),
                    () => OnHeroLevelUpClicked(rowIdx), out _haLvUpLbls[i]);
                _haStarUpBtns[i] = ForestUiWidgets.MakeJellyButton($"StarUp_{i}", row.transform, ForestComicUiSkin.BtnOrange, "升星", 14,
                    new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(106f, 46f),
                    () => OnHeroStarUpClicked(rowIdx), out _haStarUpLbls[i]);

                for (int t = 0; t < 3; t++)
                {
                    OutfitTier tier = (OutfitTier)t;
                    float bx = 102f + t * 158f;
                    _haSkinBtns[i, t] = ForestUiWidgets.MakeJellyButton($"Skin_{i}_{t}", row.transform, ForestComicUiSkin.BtnGrey, "初装", 12,
                        new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(bx, 7f), new Vector2(152f, 30f),
                        () => OnHeroSkinTierClicked(rowIdx, tier), out _haSkinTexts[i, t]);
                }
            }
            _haGrowthPage.SetActive(false);

            ForestUiWidgets.MakeText("FooterNote", sheet.transform, "委任效果受英雄品质、等级和星级影响（与出战小队零冲突共享）", 15,
                new Color32(236, 214, 176, 255), TextAnchor.LowerCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(-24f, 24f));

            ForestUiWidgets.MakeRedBackButton(sheet.transform, CloseAllModals, new Vector2(20f, 14f));
            ForestUiWidgets.MakeJellyButton("BtnUnassign", sheet.transform, ForestComicUiSkin.BtnRed, "一键卸任", 19,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-132f, 16f), new Vector2(156f, 54f),
                () => { _sim.UnassignAllHeroes(); RefreshHeroAssignModal(); }, out _);
            ForestUiWidgets.MakeJellyButton("BtnAutoAssign", sheet.transform, ForestComicUiSkin.BtnBlue, "一键委任", 19,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(38f, 16f), new Vector2(164f, 54f),
                () =>
                {
                    int n = _sim.AutoAssignHeroes();
                    RefreshHeroAssignModal();
                    if (n > 0) SpawnWorldRisingText("已按最高评分一键委任全部空闲英雄！", new Vector2(16.4f, 16.2f));
                }, out _);
            ForestUiWidgets.MakeJellyButton("BtnOpenTacticalFmt", sheet.transform, ForestComicUiSkin.BtnGreen, "战术布阵", 19,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(218f, 16f), new Vector2(176f, 54f),
                OpenTacticalFormationModal, out _);

            _heroAssignModal.SetActive(false);
        }

        // -------------------- 独立策略战场四大 UI：战前海报 + 3x3九宫格布阵 + 微信胶囊安全战斗HUD + MVP连战结算 --------------------
        void BuildWaveDefenseModalsAndHud()
        {
            // =========================================================================
            // 1. 【山口闯关 · 战前关卡海报弹窗】 (_wavePrepModal / OpenStagePosterModal, 652x880)
            // =========================================================================
            _wavePrepModal = MakeModalMask("StagePosterModal", 0.72f, CloseAllModals);
            Image pCard = ForestUiWidgets.MakeSlicedImage("Card", _wavePrepModal.transform, ForestComicUiSkin.DialogOuter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(652f, 880f), true);

            Image stageBadge = ForestUiWidgets.MakeSlicedImage("StageBadge", pCard.transform, ForestComicUiSkin.BtnOrange, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -14f), new Vector2(146f, 38f), false);
            _spStageBadgeTxt = ForestUiWidgets.MakeText("Txt", stageBadge.transform, "第 1 / 40 关", 17, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            ForestUiWidgets.MakeText("Title", pCard.transform, "山口哨卡闯关", 27, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(26f, -14f), new Vector2(300f, 38f));
            ForestUiWidgets.MakeJellyButton("Close", pCard.transform, ForestComicUiSkin.BtnRed, "✕", 21,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -12f), new Vector2(44f, 44f), CloseAllModals, out _);

            Image pPaper = ForestUiWidgets.MakeSlicedImage("Paper", pCard.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(612f, 716f), false);

            // A. 关卡首领/情报横幅 (584x136)
            Image stageBanner = ForestUiWidgets.MakeSlicedImage("StageBanner", pPaper.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(584f, 136f), false);
            _spBossIconImg = ForestUiWidgets.MakeIcon("ZomIco", stageBanner.transform,
                ForestComicUiSkin.GetArt("bust_zombie_boss", ForestComicUiSkin.GetArt("dock_defense")),
                new Vector2(-236f, 0f), new Vector2(82f, 82f));
            _spStageTitleTxt = ForestUiWidgets.MakeText("StageTitle", stageBanner.transform, "第 1 关 · 山口游尸", 22, ForestUiWidgets.WarmGold,
                TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(42f, -10f), new Vector2(-128f, 34f));
            _spStageSubTxt = ForestUiWidgets.MakeText("StageSub", stageBanner.transform, "清剿逼近断桥防线的先遣尸群", 15, ForestUiWidgets.CreamLight,
                TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(42f, -46f), new Vector2(-128f, 28f));

            _spThreatTag1Bg = ForestUiWidgets.MakeSlicedImage("ThreatTag1", stageBanner.transform, ForestComicUiSkin.BtnRed, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-94f, -88f), new Vector2(168f, 34f), false);
            _spThreatTag1Txt = ForestUiWidgets.MakeText("T1", _spThreatTag1Bg.transform, "[常规哨卡战]", 14, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            _spThreatTag2Bg = ForestUiWidgets.MakeSlicedImage("ThreatTag2", stageBanner.transform, ForestComicUiSkin.BtnOrange, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(106f, -88f), new Vector2(216f, 34f), false);
            _spThreatTag2Txt = ForestUiWidgets.MakeText("T2", _spThreatTag2Bg.transform, "[正面前排推进]", 14, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // B. 敌我战力对比横幅 (584x84)
            Image pwrBanner = ForestUiWidgets.MakeSlicedImage("PowerCompareBanner", pPaper.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -158f), new Vector2(584f, 84f), false);
            _spPowerCompareTxt = ForestUiWidgets.MakeText("PwrVs", pwrBanner.transform, "我方小队战力 360   VS   敌方推荐战力 220", 18, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-24f, 32f));
            _spPowerStatePillBg = ForestUiWidgets.MakeSlicedImage("PwrStatePill", pwrBanner.transform, ForestComicUiSkin.BtnGreen, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -45f), new Vector2(276f, 31f), false);
            _spPowerStateTxt = ForestUiWidgets.MakeText("StateTxt", _spPowerStatePillBg.transform, "★ 战力压制 · 稳操胜券", 15, Color.white,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // C. 已上阵小队预览与激活羁绊条 (584x226)
            Image sqBox = ForestUiWidgets.MakeSlicedImage("SquadPreviewBox", pPaper.transform, ForestComicUiSkin.SubSlot, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -252f), new Vector2(584f, 226f), false);
            _spSquadSummaryTxt = ForestUiWidgets.MakeText("SquadHdr", sqBox.transform, "已上阵 2/2 人  ·  当前阵型：【基础防线阵】", 16, ForestUiWidgets.InkDark,
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-20f, 26f), outline: false);

            for (int i = 0; i < 5; i++)
            {
                float px = -224f + i * 112f;
                Image pvCard = ForestUiWidgets.MakeSlicedImage("PvHero_" + i, sqBox.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(px, -40f), new Vector2(104f, 122f), true);
                _spPreviewFrames[i] = pvCard;
                pvCard.gameObject.AddComponent<Button>().onClick.AddListener(OpenTacticalFormationModal);

                Image msk = ForestUiWidgets.MakeSlicedImage("Mask", pvCard.transform, ForestComicUiSkin.SubSlot, new Color(1f, 1f, 1f, 0.25f),
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(88f, 66f), false);
                msk.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                _spPreviewBusts[i] = ForestUiWidgets.MakeIcon("Bust", msk.transform, ForestComicUiSkin.GetArt("bust_dachui"), new Vector2(0f, -4f), new Vector2(68f, 68f));
                _spPreviewLocks[i] = ForestUiWidgets.MakeIcon("Lock", pvCard.transform, ForestComicUiSkin.GetArt("ico_lock"), new Vector2(0f, 16f), new Vector2(30f, 30f));

                _spPreviewNames[i] = ForestUiWidgets.MakeText("Name", pvCard.transform, "大锤 ★1", 13, ForestUiWidgets.InkDark, TextAnchor.MiddleCenter,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 25f), new Vector2(-6f, 20f), outline: false);
                _spPreviewRoles[i] = ForestUiWidgets.MakeText("Role", pvCard.transform, "前排·重卫", 12, ForestUiWidgets.InkMuted, TextAnchor.MiddleCenter,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(-6f, 18f), outline: false);
            }

            Image synStrip = ForestUiWidgets.MakeSlicedImage("SynergyStrip", sqBox.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(560f, 46f), false);
            _spSynergyStripTxt = ForestUiWidgets.MakeText("SynTxt", synStrip.transform, "已激活羁绊：【同心协力】", 14, ForestUiWidgets.WarmGold,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-18f, -4f));

            // D. 战利品与首通里程碑横幅 (584x214)
            Image rwPanel = ForestUiWidgets.MakeSlicedImage("PosterRwPanel", pPaper.transform, ForestComicUiSkin.SubSlot, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -488f), new Vector2(584f, 214f), false);
            ForestUiWidgets.MakeText("RwHdr", rwPanel.transform, "本关通关战利品", 17, ForestUiWidgets.InkDark,
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-20f, 26f), outline: false);

            for (int r = 0; r < 4; r++)
            {
                float rx = -204f + r * 136f;
                Image rSlot = ForestUiWidgets.MakeSlicedImage("PRw_" + r, rwPanel.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(rx, -38f), new Vector2(126f, 96f), false);
                _spRewardIcons[r] = ForestUiWidgets.MakeIcon("Ico", rSlot.transform, ForestComicUiSkin.ResIcon(SurvRes.HeroMedal), new Vector2(0f, 12f), new Vector2(48f, 48f));
                _spRewardTxts[r] = ForestUiWidgets.MakeText("Cnt", rSlot.transform, "勋章 +2", 15, ForestUiWidgets.InkDark, TextAnchor.LowerCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(-6f, -4f), outline: false);
            }

            _spMilestoneBg = ForestUiWidgets.MakeSlicedImage("MilestoneBanner", rwPanel.transform, ForestComicUiSkin.BtnOrange, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -146f), new Vector2(556f, 52f), false);
            _spMilestoneTxt = ForestUiWidgets.MakeText("MileTxt", _spMilestoneBg.transform, "★ 首通里程碑大奖", 15, Color.white,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-18f, -4f));

            // 底部按钮行
            ForestUiWidgets.MakeRedBackButton(pCard.transform, CloseAllModals, new Vector2(18f, 14f));
            ForestUiWidgets.MakeJellyButton("BtnPosterFormation", pCard.transform, ForestComicUiSkin.BtnBlue, "阵容布阵", 22,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-72f, 18f), new Vector2(212f, 62f),
                OpenTacticalFormationModal, out _);
            ForestUiWidgets.MakeJellyButton("BtnPosterFight", pCard.transform, ForestComicUiSkin.BtnGreen, "立即开战!", 24,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(172f, 18f), new Vector2(240f, 62f),
                StartWaveDefenseBattle, out _);
            _wavePrepModal.SetActive(false);

            // =========================================================================
            // 2. 【3x3 九宫格战术布阵弹窗】 (_tacticalFormationModal / OpenTacticalFormationModal, 676x1060)
            // =========================================================================
            _tacticalFormationModal = MakeModalMask("TacticalFormationModal", 0.76f, CloseAllModals);
            Image fCard = ForestUiWidgets.MakeSlicedImage("Card", _tacticalFormationModal.transform, ForestComicUiSkin.DialogOuter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(676f, 1060f), true);

            ForestUiWidgets.MakeText("Title", fCard.transform, "小队九宫格布阵", 26, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-14f, -10f), new Vector2(420f, 34f));
            ForestUiWidgets.MakeJellyButton("Close", fCard.transform, ForestComicUiSkin.BtnRed, "✕", 21,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -10f), new Vector2(44f, 44f), CloseAllModals, out _);

            Image fHdrBar = ForestUiWidgets.MakeSlicedImage("HdrInfoBar", fCard.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(640f, 32f), false);
            _tfDeployCountTxt = ForestUiWidgets.MakeText("DeployCnt", fHdrBar.transform, "上阵人数: 2 / 2（营地 Lv.4 扩编至 3 人）", 15, ForestUiWidgets.CreamLight,
                TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(0.68f, 1f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(-14f, 0f));
            _tfTotalPowerTxt = ForestUiWidgets.MakeText("TotPwr", fHdrBar.transform, "总战力 360", 16, ForestUiWidgets.WarmGold,
                TextAnchor.MiddleRight, new Vector2(0.65f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(-14f, 0f));

            Image fPaper = ForestUiWidgets.MakeSlicedImage("Paper", fCard.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(644f, 888f), false);

            // Section A: 3x3 九宫格列标题 + 9 个交互格 (y = -8 .. -360)
            string[] colTitles = { "后排 (远程/支援)", "中排 (火力/策应)", "前排 (迎敌抗伤 →)" };
            Color32[] colColors =
            {
                new Color32(168, 224, 255, 255),
                new Color32(255, 218, 108, 255),
                new Color32(255, 168, 128, 255),
            };
            for (int c = 0; c < 3; c++)
            {
                float cx = -202f + c * 202f;
                Image cHdr = ForestUiWidgets.MakeSlicedImage("ColHdr_" + c, fPaper.transform, ForestComicUiSkin.DarkPill, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(cx, -8f), new Vector2(192f, 28f), false);
                ForestUiWidgets.MakeText("Lbl", cHdr.transform, colTitles[c], 14, colColors[c], TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            }

            string[] rowNames = { "上路", "中路", "下路" };
            string[] colShortNames = { "后排", "中排", "前排" };
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    int slotIdx = r * 3 + c;
                    float tx = -202f + c * 202f;
                    float ty = -42f - r * 108f;

                    Image tile = ForestUiWidgets.MakeSlicedImage($"GridTile_{r}_{c}", fPaper.transform, ForestComicUiSkin.SubSlot, Color.white,
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(tx, ty), new Vector2(192f, 102f), true);
                    _tfTileFrames[slotIdx] = tile;
                    tile.gameObject.AddComponent<Button>().onClick.AddListener(() => OnTacticalGridTileClicked(slotIdx));

                    _tfTileLaneHints[slotIdx] = ForestUiWidgets.MakeText("LaneHint", tile.transform, $"{rowNames[r]}·{colShortNames[c]}", 12, ForestUiWidgets.InkMuted,
                        TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -6f), new Vector2(88f, 20f), outline: false);

                    Image buffBg = ForestUiWidgets.MakeSlicedImage("BuffTag", tile.transform, ForestComicUiSkin.BtnOrange, Color.white,
                        new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-4f, -4f), new Vector2(84f, 22f), false);
                    _tfTileBuffGos[slotIdx] = buffBg.gameObject;
                    _tfTileBuffBgs[slotIdx] = buffBg;
                    _tfTileBuffTxts[slotIdx] = ForestUiWidgets.MakeText("Txt", buffBg.transform, "生命+15%", 12, Color.white,
                        TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

                    Image pBox = ForestUiWidgets.MakeSlicedImage("PortraitBox", tile.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                        new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, -4f), new Vector2(58f, 58f), false);
                    pBox.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                    _tfTilePortraitBoxes[slotIdx] = pBox.gameObject;
                    _tfTileBusts[slotIdx] = ForestUiWidgets.MakeIcon("Bust", pBox.transform, ForestComicUiSkin.GetArt("bust_dachui"), new Vector2(0f, -3f), new Vector2(62f, 62f));

                    _tfTileNames[slotIdx] = ForestUiWidgets.MakeText("HeroName", tile.transform, "大锤 ★1", 15, ForestUiWidgets.InkDark,
                        TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(72f, -28f), new Vector2(114f, 24f), outline: false);
                    _tfTileRoles[slotIdx] = ForestUiWidgets.MakeText("HeroRole", tile.transform, "铁壁重卫\n303战", 12, ForestUiWidgets.InkMuted,
                        TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(72f, 8f), new Vector2(80f, 38f), outline: false);

                    _tfTileEmptyTxts[slotIdx] = ForestUiWidgets.MakeText("EmptyHint", tile.transform, "＋ 点击部署", 15, ForestUiWidgets.InkMuted,
                        TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(-12f, -24f), outline: false);

                    Button rmBtn = ForestUiWidgets.MakeJellyButton("BtnRemove", tile.transform, ForestComicUiSkin.BtnRed, "下", 13,
                        new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-5f, 5f), new Vector2(32f, 28f),
                        () => OnTacticalTileRemoveClicked(slotIdx), out _);
                    _tfTileRemoveGos[slotIdx] = rmBtn.gameObject;
                }
            }

            // Section B: 4 大可解锁战术阵型条 (y = -370 .. -494, height = 124)
            Image fmtBox = ForestUiWidgets.MakeSlicedImage("FormationBox", fPaper.transform, ForestComicUiSkin.SubSlot, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -370f), new Vector2(616f, 124f), false);
            ForestUiWidgets.MakeText("FmtHdr", fmtBox.transform, "【战术阵型】切换阵型激活对应格位战术增益", 15, ForestUiWidgets.InkDark,
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -5f), new Vector2(-16f, 24f), outline: false);

            for (int f = 0; f < 4; f++)
            {
                int fmtIdx = f;
                float fx = -222f + f * 148f;
                _tfFormationBtns[f] = ForestUiWidgets.MakeJellyButton("FmtBtn_" + f, fmtBox.transform, ForestComicUiSkin.BtnBlue, "基础防线阵", 14,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(fx, -33f), new Vector2(142f, 46f),
                    () => OnFormationButtonClicked((TacticalFormationId)fmtIdx), out _tfFormationBtnLbls[f]);
                _tfFormationBtnImgs[f] = _tfFormationBtns[f].GetComponent<Image>();
            }

            Image fmtSumBar = ForestUiWidgets.MakeSlicedImage("FmtSumBar", fmtBox.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(596f, 31f), false);
            _tfFormationSummaryTxt = ForestUiWidgets.MakeText("FmtSum", fmtSumBar.transform, "前排生命+15%，中后排攻击+10%", 13, ForestUiWidgets.WarmGold,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-14f, 0f));

            // Section C: 幸存者羁绊光环 2x2 面板 (y = -502 .. -672, height = 170)
            Image synBox = ForestUiWidgets.MakeSlicedImage("SynergyAuraBox", fPaper.transform, ForestComicUiSkin.SubSlot, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -502f), new Vector2(616f, 170f), false);
            ForestUiWidgets.MakeText("SynHdr", synBox.transform, "【幸存者羁绊光环】满足职业编组条件自动叠加全队加成", 15, ForestUiWidgets.InkDark,
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -5f), new Vector2(-16f, 24f), outline: false);

            for (int s = 0; s < 4; s++)
            {
                int sc = s % 2;
                int sr = s / 2;
                float sx = sc == 0 ? -150f : 150f;
                float sy = -33f - sr * 66f;
                Image aCard = ForestUiWidgets.MakeSlicedImage("Aura_" + s, synBox.transform, ForestComicUiSkin.DarkPill, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(sx, sy), new Vector2(294f, 62f), false);
                _tfSynergyBgs[s] = aCard;
                _tfSynergyTitles[s] = ForestUiWidgets.MakeText("Title", aCard.transform, "同心协力", 14, ForestUiWidgets.WarmGold,
                    TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(-20f, 22f));
                _tfSynergyDescs[s] = ForestUiWidgets.MakeText("Desc", aCard.transform, "全队攻防加成", 12, Color.white,
                    TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(-20f, 30f));
            }

            // Section D: 点击骨干上阵/换位 6 卡横排 (y = -680 .. -880, height = 200)
            Image rosBox = ForestUiWidgets.MakeSlicedImage("HeroRosterBox", fPaper.transform, ForestComicUiSkin.SubSlot, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -680f), new Vector2(616f, 200f), false);
            _tfRosterHintTxt = ForestUiWidgets.MakeText("RosHdr", rosBox.transform, "【点击骨干上阵/换位】先点选骨干，再点上方九宫格任意位置部署", 14, ForestUiWidgets.InkDark,
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -5f), new Vector2(-16f, 24f), outline: false);

            for (int i = 0; i < 6; i++)
            {
                int hIdx = i;
                float hx = -250f + i * 100f;
                Image hCard = ForestUiWidgets.MakeSlicedImage("RosHero_" + i, rosBox.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(hx, -34f), new Vector2(96f, 158f), true);
                _tfRosterCards[i] = hCard.gameObject;
                _tfRosterFrames[i] = hCard;
                hCard.gameObject.AddComponent<Button>().onClick.AddListener(() => OnRosterHeroCardClicked(hIdx));

                Image tagBg = ForestUiWidgets.MakeSlicedImage("StatusTag", hCard.transform, ForestComicUiSkin.BtnGreen, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(84f, 20f), false);
                _tfRosterTagBgs[i] = tagBg;
                _tfRosterTagTxts[i] = ForestUiWidgets.MakeText("Tag", tagBg.transform, "已上阵", 11, Color.white,
                    TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

                Image pMask = ForestUiWidgets.MakeSlicedImage("Mask", hCard.transform, ForestComicUiSkin.SubSlot, new Color(1f, 1f, 1f, 0.25f),
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -27f), new Vector2(80f, 64f), false);
                pMask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                _tfRosterBusts[i] = ForestUiWidgets.MakeIcon("Bust", pMask.transform, ForestComicUiSkin.GetArt("bust_dachui"), new Vector2(0f, -4f), new Vector2(66f, 66f));

                _tfRosterNames[i] = ForestUiWidgets.MakeText("Name", hCard.transform, "大锤 ★1", 13, ForestUiWidgets.InkDark,
                    TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -94f), new Vector2(-4f, 20f), outline: false);
                _tfRosterRoles[i] = ForestUiWidgets.MakeText("Role", hCard.transform, "铁壁重卫", 11, ForestUiWidgets.InkMuted,
                    TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -114f), new Vector2(-4f, 18f), outline: false);
                _tfRosterPowers[i] = ForestUiWidgets.MakeText("Pwr", hCard.transform, "战力 303", 11, ForestUiWidgets.LushGreen,
                    TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -134f), new Vector2(-4f, 18f), outline: false);
            }

            // 底部 3 按钮行
            ForestUiWidgets.MakeJellyButton("BtnAutoDeploy", fCard.transform, ForestComicUiSkin.BtnBlue, "一键推荐布阵", 19,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-206f, 16f), new Vector2(202f, 56f),
                () =>
                {
                    _sim.AutoDeployRecommendedFormation();
                    _selectedFormationHeroIdx = -1;
                    RefreshTacticalFormationModal();
                    SpawnScreenCenterBanner("已按职业克制与最高战力自动排布九宫格阵型！");
                }, out _);
            ForestUiWidgets.MakeJellyButton("BtnBackPoster", fCard.transform, ForestComicUiSkin.BtnGrey, "返回海报", 19,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(178f, 56f),
                OpenStagePosterModal, out _);
            ForestUiWidgets.MakeJellyButton("BtnSaveAndFight", fCard.transform, ForestComicUiSkin.BtnOrange, "保存并开战", 20,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(204f, 16f), new Vector2(204f, 56f),
                StartWaveDefenseBattle, out _);
            _tacticalFormationModal.SetActive(false);

            // =========================================================================
            // 3. 【独立策略战场 HUD】 (_waveCombatHud — 100% 避让微信右上角 126px 胶囊与游戏圈安全区!)
            // =========================================================================
            _waveCombatHud = ForestUiWidgets.Stretch("TacticalArenaCombatHud", _canvasRt).gameObject;
            float topRowY = -16f - _topSafePad;

            // 左上：[撤退] + 关卡倒计时胶囊（严格 x = 16..386 <= 396，右上 x > 400 100% 净空！）
            ForestUiWidgets.MakeJellyButton("BtnRetreatArena", _waveCombatHud.transform, ForestComicUiSkin.BtnRed, "撤退", 17,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, topRowY), new Vector2(88f, 42f),
                RetreatArenaToCamp, out _);

            Image stageTimerPill = ForestUiWidgets.MakeSlicedImage("StageTimerPill", _waveCombatHud.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(112f, topRowY), new Vector2(274f, 42f), false);
            _wcStageTimerTxt = ForestUiWidgets.MakeText("Txt", stageTimerPill.transform, "第 1 关  ·  01:30", 17, ForestUiWidgets.WarmGold,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // 对称 VS 战力与总血条横幅（强制下移至 y = Mathf.Min(-142f, -92f - _topSafePad)，确保右侧尸潮军团完全位于微信右上角胶囊下方！）
            float vsBarTop = Mathf.Min(-142f, -92f - _topSafePad);
            Image vsBar = ForestUiWidgets.MakeSlicedImage("SymmetricVsBar", _waveCombatHud.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, vsBarTop), new Vector2(688f, 74f), false);

            // 左侧：我方小队战力 + 绿色总血条
            _wcTeamPowerTxt = ForestUiWidgets.MakeText("TeamPwr", vsBar.transform, "我方战力 360", 15, new Color32(168, 255, 172, 255),
                TextAnchor.MiddleLeft, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-182f, -8f), new Vector2(292f, 24f));
            Image teamHpBg = ForestUiWidgets.MakeSlicedImage("TeamHpBg", vsBar.transform, ForestComicUiSkin.SubSlot, new Color(0.18f, 0.22f, 0.18f, 0.92f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-182f, -38f), new Vector2(292f, 26f), false);
            RectTransform teamFillRt = ForestUiWidgets.MakeRect("Fill", teamHpBg.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6f, -6f));
            _wcTeamHpFill = teamFillRt.gameObject.AddComponent<Image>();
            _wcTeamHpFill.sprite = ForestComicUiSkin.BtnGreen;
            _wcTeamHpFill.type = Image.Type.Filled;
            _wcTeamHpFill.fillMethod = Image.FillMethod.Horizontal;
            _wcTeamHpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _wcTeamHpTxt = ForestUiWidgets.MakeText("Val", teamHpBg.transform, "500 / 500", 13, Color.white,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // 右侧：尸潮军团剩余数 + 红色总血条
            _wcEnemyTitleTxt = ForestUiWidgets.MakeText("EnemyHdr", vsBar.transform, "尸潮军团 (剩余 12/12)", 15, new Color32(255, 174, 152, 255),
                TextAnchor.MiddleRight, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(182f, -8f), new Vector2(292f, 24f));
            Image enemyHpBg = ForestUiWidgets.MakeSlicedImage("EnemyHpBg", vsBar.transform, ForestComicUiSkin.SubSlot, new Color(0.24f, 0.16f, 0.16f, 0.92f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(182f, -38f), new Vector2(292f, 26f), false);
            RectTransform enemyFillRt = ForestUiWidgets.MakeRect("Fill", enemyHpBg.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6f, -6f));
            _wcEnemyHpFill = enemyFillRt.gameObject.AddComponent<Image>();
            _wcEnemyHpFill.sprite = ForestComicUiSkin.BtnRed;
            _wcEnemyHpFill.type = Image.Type.Filled;
            _wcEnemyHpFill.fillMethod = Image.FillMethod.Horizontal;
            _wcEnemyHpFill.fillOrigin = (int)Image.OriginHorizontal.Right;
            _wcEnemyHpTxt = ForestUiWidgets.MakeText("Val", enemyHpBg.transform, "600 / 600", 13, Color.white,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // 中央 VS 徽章
            Image vsBadge = ForestUiWidgets.MakeSlicedImage("VsBadge", vsBar.transform, ForestComicUiSkin.BtnOrange, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(54f, 54f), false);
            ForestUiWidgets.MakeText("VsTxt", vsBadge.transform, "VS", 21, Color.white,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // 底部战术操控栏（y = 214f 紧贴英雄卡上方，远离右上角胶囊！）
            _wcSkipBtn = ForestUiWidgets.MakeJellyButton("BtnSkipBattle", _waveCombatHud.transform, ForestComicUiSkin.BtnGrey, "5秒后可跳过", 15,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 214f), new Vector2(186f, 44f),
                OnSkipBattleClicked, out _wcSkipLbl);
            _wcSkipBtnImg = _wcSkipBtn.GetComponent<Image>();

            _wcAutoBtn = ForestUiWidgets.MakeJellyButton("BtnAutoUlt", _waveCombatHud.transform, ForestComicUiSkin.BtnGreen, "自动绝技:开", 15,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-146f, 214f), new Vector2(152f, 44f),
                () =>
                {
                    if (_world != null && _world.Arena != null)
                        _world.Arena.AutoCastUlt = !_world.Arena.AutoCastUlt;
                }, out _wcAutoLbl);
            _wcAutoBtnImg = _wcAutoBtn.GetComponent<Image>();

            _wcSpeedBtn = ForestUiWidgets.MakeJellyButton("BtnArenaSpeed", _waveCombatHud.transform, ForestComicUiSkin.BtnBlue, "倍速 1x", 16,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 214f), new Vector2(118f, 44f),
                () =>
                {
                    if (_world != null && _world.Arena != null)
                        _world.Arena.ToggleSpeed();
                }, out _wcSpeedLbl);
            _wcSpeedBtnImg = _wcSpeedBtn.GetComponent<Image>();

            // 底部 1..5 张动态居中上阵英雄绝技卡（5人满编 5*128 + 4*8 = 672 <= 720 零溢出！）
            for (int i = 0; i < 5; i++)
            {
                int slotIdx = i;
                _wcBoundHeroIdxs[i] = -1;
                Image skCard = ForestUiWidgets.MakeSlicedImage("ArenaHeroCard_" + i, _waveCombatHud.transform, ForestComicUiSkin.DialogOuter, Color.white,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-272f + i * 136f, 20f), new Vector2(128f, 182f), true);
                _wcHeroCards[i] = skCard.gameObject;
                _wcHeroCardRts[i] = skCard.rectTransform;
                _wcHeroFrames[i] = skCard;
                skCard.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                {
                    int hIdx = _wcBoundHeroIdxs[slotIdx];
                    if (hIdx >= 0 && _world != null && _world.Arena != null)
                        _world.Arena.TryCastHeroUlt(hIdx);
                });

                _wcHeroNames[i] = ForestUiWidgets.MakeText("Name", skCard.transform, "大锤 ★1", 14, ForestUiWidgets.CreamLight,
                    TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(-8f, 22f));

                Image msk = ForestUiWidgets.MakeSlicedImage("Portrait", skCard.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(74f, 70f), false);
                msk.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                _wcHeroBusts[i] = ForestUiWidgets.MakeIcon("Bust", msk.transform, ForestComicUiSkin.GetArt("bust_dachui"), new Vector2(0f, -4f), new Vector2(76f, 76f));

                // 血条 + 护盾叠加条
                Image hpBg = ForestUiWidgets.MakeSlicedImage("HpBg", skCard.transform, ForestComicUiSkin.DarkPill, Color.white,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 56f), new Vector2(-14f, 18f), false);
                RectTransform hpFillRt = ForestUiWidgets.MakeRect("HpFill", hpBg.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4f, -4f));
                _wcHeroHpFills[i] = hpFillRt.gameObject.AddComponent<Image>();
                _wcHeroHpFills[i].sprite = ForestComicUiSkin.BtnGreen;
                _wcHeroHpFills[i].type = Image.Type.Filled;
                _wcHeroHpFills[i].fillMethod = Image.FillMethod.Horizontal;

                RectTransform shFillRt = ForestUiWidgets.MakeRect("ShieldFill", hpBg.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4f, -4f));
                _wcHeroShieldFills[i] = shFillRt.gameObject.AddComponent<Image>();
                _wcHeroShieldFills[i].sprite = ForestComicUiSkin.BtnBlue;
                _wcHeroShieldFills[i].color = new Color(0.75f, 0.95f, 1f, 0.72f);
                _wcHeroShieldFills[i].type = Image.Type.Filled;
                _wcHeroShieldFills[i].fillMethod = Image.FillMethod.Horizontal;
                _wcHeroHpTxts[i] = ForestUiWidgets.MakeText("HpTxt", hpBg.transform, "240", 11, Color.white,
                    TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

                // 金色怒气条
                Image rgBg = ForestUiWidgets.MakeSlicedImage("RageBg", skCard.transform, ForestComicUiSkin.DarkPill, Color.white,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 35f), new Vector2(-14f, 18f), false);
                RectTransform rgFillRt = ForestUiWidgets.MakeRect("RageFill", rgBg.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4f, -4f));
                _wcHeroRageFills[i] = rgFillRt.gameObject.AddComponent<Image>();
                _wcHeroRageFills[i].sprite = ForestComicUiSkin.BtnOrange;
                _wcHeroRageFills[i].type = Image.Type.Filled;
                _wcHeroRageFills[i].fillMethod = Image.FillMethod.Horizontal;
                _wcHeroRageTxts[i] = ForestUiWidgets.MakeText("RageTxt", rgBg.transform, "怒气 0%", 11, Color.white,
                    TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

                // 底部绝技状态标签
                Image actBg = ForestUiWidgets.MakeSlicedImage("ActTag", skCard.transform, ForestComicUiSkin.DarkPill, Color.white,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(-12f, 25f), false);
                _wcHeroActionBgs[i] = actBg;
                _wcHeroActionTxts[i] = ForestUiWidgets.MakeText("ActTxt", actBg.transform, "狂风劈", 12, ForestUiWidgets.WarmGold,
                    TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4f, 0f));
            }
            _waveCombatHud.SetActive(false);

            // =========================================================================
            // 4. 【挑战胜利 MVP / 战败复盘结算弹窗】 (_arenaResultModal, 624x720)
            // =========================================================================
            _arenaResultModal = MakeModalMask("ArenaResultModal", 0.80f, null);
            _arSunburst = ForestUiWidgets.MakeIcon("Sunburst", _arenaResultModal.transform, ForestComicUiSkin.Sunburst, new Vector2(0f, 60f), new Vector2(650f, 650f));
            Image rCard = ForestUiWidgets.MakeSlicedImage("Card", _arenaResultModal.transform, ForestComicUiSkin.DialogOuter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(624f, 720f), true);

            _arRibbonImg = ForestUiWidgets.MakeSlicedImage("Ribbon", rCard.transform, ForestComicUiSkin.BtnOrange, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(360f, 58f), false);
            _arRibbonTxt = ForestUiWidgets.MakeText("RibTxt", _arRibbonImg.transform, "挑战胜利!", 30, Color.white,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            _arStageSubTxt = ForestUiWidgets.MakeText("StageSub", rCard.transform, "第 1 关 · 山口游尸", 19, ForestUiWidgets.WarmGold,
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(-32f, 30f));

            // 胜利结算主体
            _arVictoryRoot = ForestUiWidgets.MakeRect("VictoryBody", rCard.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject;
            Image vPaper = ForestUiWidgets.MakeSlicedImage("VPaper", _arVictoryRoot.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(584f, 466f), false);

            // MVP 本场最佳卡片 (552x156)
            Image mvpCard = ForestUiWidgets.MakeSlicedImage("MvpCard", vPaper.transform, ForestComicUiSkin.SubSlotSelected, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(552f, 156f), false);
            Image mvpBadge = ForestUiWidgets.MakeSlicedImage("MvpBadge", mvpCard.transform, ForestComicUiSkin.BtnOrange, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -10f), new Vector2(154f, 30f), false);
            ForestUiWidgets.MakeText("Txt", mvpBadge.transform, "★ MVP 本场最佳", 15, Color.white,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            Image mvpMask = ForestUiWidgets.MakeSlicedImage("MvpMask", mvpCard.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 12f), new Vector2(96f, 96f), false);
            mvpMask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            _arMvpBust = ForestUiWidgets.MakeIcon("Bust", mvpMask.transform, ForestComicUiSkin.GetArt("bust_dachui"), new Vector2(0f, -4f), new Vector2(102f, 102f));

            _arMvpNameTxt = ForestUiWidgets.MakeText("MvpName", mvpCard.transform, "大锤（铁壁重卫）", 23, ForestUiWidgets.InkDark,
                TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(134f, -44f), new Vector2(-150f, 34f), outline: false);
            _arMvpDmgTxt = ForestUiWidgets.MakeText("MvpDmg", mvpCard.transform, "本场总贡献伤害/治疗: 1280", 18, ForestUiWidgets.LushGreen,
                TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(134f, -88f), new Vector2(-150f, 32f), outline: false);

            // 战利品与里程碑奖励框 (552x210)
            Image rwSumBox = ForestUiWidgets.MakeSlicedImage("RewardSummaryBox", vPaper.transform, ForestComicUiSkin.SubSlot, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -182f), new Vector2(552f, 210f), false);
            ForestUiWidgets.MakeText("Hdr", rwSumBox.transform, "通关战利品与首通里程碑", 17, ForestUiWidgets.InkDark,
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-20f, 26f), outline: false);
            _arRewardSummaryTxt = ForestUiWidgets.MakeText("Body", rwSumBox.transform, "经验 +50 · 铜币 +120 · 英雄勋章 +2", 17, ForestUiWidgets.InkDark,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(-36f, -44f), outline: false);
            _arRewardSummaryTxt.lineSpacing = 1.28f;

            // [✓ 自动连续闯关] 勾选开关胶囊
            Button autoChainBtn = ForestUiWidgets.MakeJellyButton("BtnAutoChainToggle", vPaper.transform, ForestComicUiSkin.BtnGrey, "[ ] 自动连续闯关", 16,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(336f, 46f),
                () =>
                {
                    _sim.AutoChainNextStage = !_sim.AutoChainNextStage;
                    if (_sim.AutoChainNextStage) _autoChainCountdown = 5.0f;
                    RefreshArenaResultModalVisual();
                }, out _arAutoChainLbl);
            _arAutoChainBtnImg = autoChainBtn.GetComponent<Image>();

            ForestUiWidgets.MakeJellyButton("BtnVictoryBackCamp", _arVictoryRoot.transform, ForestComicUiSkin.BtnBlue, "返回营地", 21,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-136f, 22f), new Vector2(220f, 60f),
                ExitArenaAndCloseResult, out _);
            ForestUiWidgets.MakeJellyButton("BtnVictoryNextStage", _arVictoryRoot.transform, ForestComicUiSkin.BtnOrange, "下一关", 22,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(132f, 22f), new Vector2(256f, 60f),
                OnResultNextStageClicked, out _arNextStageBtnLbl);

            // 战败复盘主体
            _arDefeatRoot = ForestUiWidgets.MakeRect("DefeatBody", rCard.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject;
            Image dPaper = ForestUiWidgets.MakeSlicedImage("DPaper", _arDefeatRoot.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(584f, 466f), false);
            ForestUiWidgets.MakeText("AdvHdr", dPaper.transform, "【战术复盘建议】调整站位或强化骨干即可破局！", 18, ForestUiWidgets.AlertRed,
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(-24f, 30f), outline: false);
            _arDefeatAdviceTxt = ForestUiWidgets.MakeText("AdvBody", dPaper.transform,
                "1. 前排部署【大锤/雷猛】吸收火力，远程输出置于后排安全格。\n" +
                "2. 切换合适【战术阵型】，让核心骨干踩中发光增益格位。\n" +
                "3. 若遭遇跳尸切后排，可在中后排安排近战或医官协防。\n" +
                "4. 前往【骨干】消耗铜币口粮升级、用勋章升星大幅提战力！",
                17, ForestUiWidgets.InkDark, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(-48f, 250f), outline: false);
            _arDefeatAdviceTxt.lineSpacing = 1.35f;

            ForestUiWidgets.MakeJellyButton("BtnDefeatGoHero", dPaper.transform, ForestComicUiSkin.BtnBlue, "去升级 / 升星骨干英雄 ▶", 18,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(320f, 52f),
                () => { ExitArenaAndCloseResult(); OpenHeroModal(1); }, out _);

            ForestUiWidgets.MakeJellyButton("BtnDefeatBackCamp", _arDefeatRoot.transform, ForestComicUiSkin.BtnGrey, "返回营地", 21,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-138f, 22f), new Vector2(216f, 60f),
                ExitArenaAndCloseResult, out _);
            ForestUiWidgets.MakeJellyButton("BtnDefeatAdjustFmt", _arDefeatRoot.transform, ForestComicUiSkin.BtnOrange, "调整九宫格布阵", 21,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(128f, 22f), new Vector2(264f, 60f),
                () => { ExitArenaAndCloseResult(); OpenTacticalFormationModal(); }, out _);

            _arenaResultModal.SetActive(false);
        }

        void BuildQuickListModal()
        {
            _quickListModal = MakeModalMask("QuickListModal", 0.62f, CloseAllModals);
            Image card = ForestUiWidgets.MakeSlicedImage("Card", _quickListModal.transform, ForestComicUiSkin.DialogOuter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 820f), true);
            _qlTitleTxt = ForestUiWidgets.MakeText("Title", card.transform, "营地建造与升级总览", 26, Color.white, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(24f, -16f), new Vector2(-216f, 38f));
            _qlSubCapTxt = ForestUiWidgets.MakeText("SubCap", card.transform, "", 15, ForestUiWidgets.WarmGold, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(24f, -54f), new Vector2(-84f, 28f));

            _qlUpgradeWhBtn = ForestUiWidgets.MakeJellyButton("BtnUpWh", card.transform, ForestComicUiSkin.BtnOrange, "升级仓库", 17,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-72f, -16f), new Vector2(120f, 40f), () =>
                {
                    CloseAllModals();
                    CampNode wh = _sim.Find("warehouse");
                    if (wh != null) { SmoothFocusGrid(new Vector2(wh.gx, wh.gy)); OpenBuildConfirmCard(wh); }
                }, out _);

            ForestUiWidgets.MakeJellyButton("Close", card.transform, ForestComicUiSkin.BtnRed, "✕", 22,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -14f), new Vector2(46f, 46f), CloseAllModals, out _);

            Image paper = ForestUiWidgets.MakeSlicedImage("Paper", card.transform, ForestComicUiSkin.ParchmentPanel, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -28f), new Vector2(596f, 694f), false);
            _qlContentRt = MakeVerticalScrollArea("QuickListScroll", paper.transform, Vector2.zero, new Vector2(584f, 680f), 680f);
            _quickListModal.SetActive(false);
        }

        // =====================================================================
        // 9. 丧尸城街区选择 + 武装皮卡车载射击/氮气冲撞 HUD + 【恭喜获得】放射结算
        // =====================================================================
        void BuildCitySelectAndDriveHud()
        {
            _citySelectModal = MakeModalMask("CityDistrictSelectModal", 0.65f, CloseAllModals);
            Image card = ForestUiWidgets.MakeSlicedImage("Card", _citySelectModal.transform, ForestComicUiSkin.DialogOuter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(648f, 820f), true);
            ForestUiWidgets.MakeIcon("BreakTruck", card.transform, ForestComicUiSkin.GetArt("truck_active"), new Vector2(-210f, 386f), new Vector2(180f, 180f));
            ForestUiWidgets.MakeText("Title", card.transform, "选择丧尸城搜刮街区", 27, Color.white, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(210f, -18f), new Vector2(-270f, 40f));
            ForestUiWidgets.MakeText("Sub", card.transform, "随车小队自动扫射尸群！击败十字路口尸王可夺取【英雄勋章+工程手稿】黄金空投箱！", 15, new Color32(226, 214, 198, 255), TextAnchor.UpperLeft,
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
            ForestUiWidgets.MakeText("TopTitle", _cityDriveHud.transform, "丧尸城武装突袭", 24, Color.white, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(320f, 32f));

            Image cargoBg = ForestUiWidgets.MakeSlicedImage("CargoBarBg", _cityDriveHud.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(440f, 34f), false);
            RectTransform cf = ForestUiWidgets.MakeRect("CargoFill", cargoBg.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8f, -8f));
            _cdCargoFill = cf.gameObject.AddComponent<Image>();
            _cdCargoFill.sprite = ForestComicUiSkin.BtnOrange;
            _cdCargoFill.type = Image.Type.Filled;
            _cdCargoFill.fillMethod = Image.FillMethod.Horizontal;
            _cdCargoTxt = ForestUiWidgets.MakeText("CargoNum", cargoBg.transform, "车斗载重  0 / 60", 16, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            Image armorBg = ForestUiWidgets.MakeSlicedImage("ArmorBarBg", _cityDriveHud.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(440f, 32f), false);
            RectTransform af = ForestUiWidgets.MakeRect("ArmorFill", armorBg.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8f, -8f));
            _cdArmorFill = af.gameObject.AddComponent<Image>();
            _cdArmorFill.sprite = ForestComicUiSkin.BtnGreen;
            _cdArmorFill.type = Image.Type.Filled;
            _cdArmorFill.fillMethod = Image.FillMethod.Horizontal;
            _cdArmorTxt = ForestUiWidgets.MakeText("ArmorNum", armorBg.transform, "皮卡装甲  360 / 360", 15, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            Image sumPill = ForestUiWidgets.MakeSlicedImage("LootSumPill", _cityDriveHud.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -148f), new Vector2(480f, 32f), false);
            _cdLootSummaryTxt = ForestUiWidgets.MakeText("LootSum", sumPill.transform, "随车英雄自动开火！拖动摇杆搜刮物资与黄金空投箱！", 15, ForestUiWidgets.WarmGold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            Image cBossBg = ForestUiWidgets.MakeSlicedImage("CityBossBar", _cityDriveHud.transform, ForestComicUiSkin.DarkPill, new Color(1f, 0.85f, 0.85f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -184f), new Vector2(420f, 28f), false);
            RectTransform cbf = ForestUiWidgets.MakeRect("Fill", cBossBg.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6f, -6f));
            _cdBossFill = cbf.gameObject.AddComponent<Image>();
            _cdBossFill.sprite = ForestComicUiSkin.BtnRed;
            _cdBossFill.type = Image.Type.Filled;
            _cdBossFill.fillMethod = Image.FillMethod.Horizontal;
            ForestUiWidgets.MakeText("Lbl", cBossBg.transform, "十字路口尸王 · 击毙解锁黄金空投箱", 13, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _cdBossBox = cBossBg.gameObject;

            Image joyBase = ForestUiWidgets.MakeIcon("JoystickBase", _cityDriveHud.transform, ForestComicUiSkin.JoystickRing, new Vector2(0f, -250f), new Vector2(156f, 156f));
            _joyBaseRt = joyBase.rectTransform;
            Image joyKnob = ForestUiWidgets.MakeIcon("JoystickKnob", _joyBaseRt, ForestComicUiSkin.JoystickThumb, Vector2.zero, new Vector2(70f, 70f));
            _joyKnobRt = joyKnob.rectTransform;

            // 右下主动技能按钮：【氮气冲撞】
            Button nitroBtn = CreateCircleActionBtn("BtnNitroRam", _cityDriveHud.transform, new Vector2(246f, -380f), 118f, out Image nitroIco, out _cdNitroLbl);
            _cdNitroRing = nitroBtn.GetComponent<Image>();
            nitroIco.sprite = ForestComicUiSkin.GetArt("ico_nitro", ForestComicUiSkin.GetArt("ico_wheel_go"));
            _cdNitroLbl.text = "氮气冲撞";
            nitroBtn.onClick.AddListener(() => _cityArena.TriggerNitroBoost());

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
            if (_farmDrawer != null) _farmDrawer.SetActive(false);
            if (_ranchDrawer != null) _ranchDrawer.SetActive(false);
            if (_residentDrawer != null) _residentDrawer.SetActive(false);
            if (_caravanModal != null) _caravanModal.SetActive(false);
            if (_crisisModal != null) _crisisModal.SetActive(false);
            if (_levelUpModal != null) _levelUpModal.SetActive(false);
            if (_heroSpotlightModal != null) _heroSpotlightModal.SetActive(false);
            if (_heroAssignModal != null) _heroAssignModal.SetActive(false);
            if (_wavePrepModal != null) _wavePrepModal.SetActive(false);
            if (_tacticalFormationModal != null) _tacticalFormationModal.SetActive(false);
            if (_arenaResultModal != null && (_world == null || !_world.InWaveDefense)) _arenaResultModal.SetActive(false);
            if (_quickListModal != null) _quickListModal.SetActive(false);
            if (_citySelectModal != null) _citySelectModal.SetActive(false);
        }

        void HandleNodeClicked(CampNode n)
        {
            if (n == null || n.state == NodeState.HiddenInForest || (_world != null && _world.IsGridCoveredByForest(n.gx, n.gy)))
                return;

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

            if (n.kind == NodeKind.Farm)
            {
                OpenFarmDrawer();
                return;
            }

            if (n.kind == NodeKind.Ranch)
            {
                OpenRanchDrawer();
                return;
            }

            if (n.storedAmount > 0)
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
            else if (n.kind == NodeKind.Barricade)
            {
                _footBtnPrimary.GetComponent<RectTransform>().anchoredPosition = new Vector2(canUp ? -52f : 0f, -92f);
                _footIconPrimary.sprite = ForestComicUiSkin.GetArt("dock_defense");
                _footLblPrimary.text = "哨卡闯关";
                _footBtnPrimary.onClick.AddListener(OpenStagePosterModal);
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
            if (n.state == NodeState.HiddenInForest || (_world != null && _world.IsGridCoveredByForest(n.gx, n.gy)))
            {
                if (!n.siteUncovered || (_world != null && _world.IsGridCoveredByForest(n.gx, n.gy)))
                    GuideChopTowardNode(n);
                else
                    GuideCollectSalvageForNode(n);
                return;
            }
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
            if (r == SurvRes.Blueprint || r == SurvRes.HeroMedal)
            {
                OpenSourceRouteForCondition(CondKind.Resource, r);
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
            int radioLv = _sim.Find("radio_post")?.unlockPlayerLv ?? 6;
            int h2Lv = _sim.Find("house_2")?.unlockPlayerLv ?? 4;
            int trLv = _sim.Find("relic_truck")?.unlockPlayerLv ?? 5;
            CampNode shNode = _sim.Find("shelter");
            int shReqLv = shNode != null ? _sim.GetRequiredPlayerLvForNext(shNode) : 3;

            if (kind == CondKind.Coins)
            {
                _srIcon.sprite = ForestComicUiSkin.GetArt("ico_coin", ForestComicUiSkin.RedDotBadge);
                _srNameTxt.text = "营地铜币";
                _srOwnTxt.text = $"当前拥有：{_sim.Coins}";
                ConfigureRouteRow(0, ForestComicUiSkin.GetArt("bust_vil_0", ForestComicUiSkin.GetArt("bust_cook")), "居民生计订单（主力·含繁荣加成）", true, "", OpenResidentDemandDrawer);
                ConfigureRouteRow(1, ForestComicUiSkin.GetArt("dock_defense"), "山口防线尸潮迎击 & 巡山清剿", true, "", OpenWavePrepModal);
                ConfigureRouteRow(2, ForestComicUiSkin.GetArt("dock_radio"), "山口行商大宗贸易", _sim.CampLevel >= radioLv, $"{radioLv}级开启", OpenCaravanModal);
            }
            else if (kind == CondKind.PopCur)
            {
                _srIcon.sprite = ForestComicUiSkin.GetArt("bust_vil_1", ForestComicUiSkin.RedDotBadge);
                _srNameTxt.text = "当前入驻人口";
                _srOwnTxt.text = $"当前人口：{_sim.CurrentPop} / 上限 {_sim.PopCap}（采集+{Mathf.RoundToInt((_sim.PopulationGatherMultiplier - 1f) * 100f)}%）";
                ConfigureRouteRow(0, ForestComicUiSkin.GetArt("refugee_cabin", ForestComicUiSkin.GetArt("tent")), "升级【1号避风民居】", true, "", JumpToBestDwelling);
                ConfigureRouteRow(1, ForestComicUiSkin.GetArt("refugee_cabin", ForestComicUiSkin.GetArt("shelter_lv2")), "建造【2号原木民居】", _sim.CampLevel >= h2Lv, $"{h2Lv}级开启", () => JumpToNodeById("house_2"));
                ConfigureRouteRow(2, ForestComicUiSkin.GetArt("heating_tower", ForestComicUiSkin.GetArt("campfire")), "升级【篝火供暖塔】扩上限", true, "", JumpToHeatTower);
            }
            else if (kind == CondKind.PopCap)
            {
                _srIcon.sprite = ForestComicUiSkin.GetArt("heating_tower", ForestComicUiSkin.RedDotBadge);
                _srNameTxt.text = "供暖人口上限";
                _srOwnTxt.text = $"当前上限：{_sim.PopCap}";
                ConfigureRouteRow(0, ForestComicUiSkin.GetArt("heating_tower", ForestComicUiSkin.GetArt("campfire")), "升级【篝火供暖塔】（核心）", true, "", JumpToHeatTower);
                ConfigureRouteRow(1, ForestComicUiSkin.GetArt("shelter_lv2"), "升级【避难所议事厅】", _sim.CampLevel >= shReqLv, $"{shReqLv}级开启", () => JumpToNodeById("shelter"));
                _srRows[2].SetActive(false);
                _sourceRouteModal.SetActive(true);
                return;
            }
            else if (res == SurvRes.HeroMedal)
            {
                _srIcon.sprite = ForestComicUiSkin.ResIcon(SurvRes.HeroMedal);
                _srNameTxt.text = "英雄勋章（升星核心）";
                _srOwnTxt.text = $"当前拥有：{_sim.Get(SurvRes.HeroMedal)}";
                ConfigureRouteRow(0, ForestComicUiSkin.GetArt("dock_defense"), "山口防线尸潮迎击（主力必掉）", true, "", OpenWavePrepModal);
                ConfigureRouteRow(1, ForestComicUiSkin.GetArt("dock_map"), "丧尸城击败尸王夺黄金空投箱", _sim.CampLevel >= trLv, $"{trLv}级开启", () => { CloseAllModals(); OpenCityDistrictSelector(); });
                _srRows[2].SetActive(false);
                _sourceRouteModal.SetActive(true);
                return;
            }
            else // 工程手稿 Blueprint
            {
                _srIcon.sprite = ForestComicUiSkin.ResIcon(SurvRes.Blueprint);
                _srNameTxt.text = "工程手稿";
                _srOwnTxt.text = $"当前拥有：{_sim.Get(SurvRes.Blueprint)}";
                ConfigureRouteRow(0, ForestComicUiSkin.GetArt("dock_map"), "丧尸城开车搜刮 & 黄金空投箱", _sim.CampLevel >= trLv, $"{trLv}级开启", () => { CloseAllModals(); OpenCityDistrictSelector(); });
                ConfigureRouteRow(1, ForestComicUiSkin.GetArt("dock_defense"), "山口防线尸潮波次迎击", true, "", OpenWavePrepModal);
                ConfigureRouteRow(2, ForestComicUiSkin.GetArt("dock_radio"), "山口行商驿站兑换 & 5单宝箱", true, "", OpenResidentDemandDrawer);
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

        void GuideChopTowardNode(CampNode n)
        {
            CloseAllModals();
            if (n == null || _world == null) return;
            if (_world.HasPendingChop)
            {
                Vector2 pend = new Vector2((_world.PendingChopCx + 0.5f) * ForestMap.ChunkSize, (_world.PendingChopCy + 0.5f) * ForestMap.ChunkSize);
                SmoothFocusGrid(pend);
                SpawnWorldRisingText("请点击树冠上的【砍伐扩地】气泡确认砍树！", pend);
                return;
            }
            if (_world.ChopQueue.Count > 0)
            {
                Vector2 qPos = new Vector2(_world.ChopQueue[0].gx, _world.ChopQueue[0].gy);
                SmoothFocusGrid(qPos);
                SpawnWorldRisingText("正在砍伐林木扩展领地，请稍候…", qPos);
                return;
            }
            if (_world.TryFindBestChopChunkToward(n.gx, n.gy, out int bcx, out int bcy, out Vector2 chopFocus))
            {
                _world.HasPendingChop = true;
                _world.PendingChopCx = bcx;
                _world.PendingChopCy = bcy;
                _world.PendingChopTimer = 5.0f;
                SmoothFocusGrid(chopFocus);
                SpawnWorldRisingText("点击这片林缘树木砍伐扩地，即可发现新区域！", chopFocus);
                return;
            }
            Vector2 safeCamp = _world.Walkers.Count > 0
                ? new Vector2(_world.Walkers[0].gx, _world.Walkers[0].gy)
                : new Vector2(16.0f, 18.5f);
            SmoothFocusGrid(safeCamp);
            SpawnWorldRisingText("请先在营地林缘点击树木砍伐扩地！", safeCamp);
        }

        void GuideCollectSalvageForNode(CampNode n)
        {
            CloseAllModals();
            if (n == null || _world == null) return;
            ForestSurvivalWorld.GroundPile sp = _world.FindSalvagePileForNode(n.id);
            if (sp != null)
            {
                Vector2 spPos = new Vector2(sp.gx, sp.gy);
                SmoothFocusGrid(spPos);
                if (!sp.queued)
                    _world.QueueGroundPile(sp);
                string lbl = !string.IsNullOrEmpty(n.salvageLabel) ? n.salvageLabel : "林间散落物";
                SpawnWorldRisingText($"前往收集整合【{lbl}】以显露建造点！", spPos);
                return;
            }
            if (!_world.IsGridCoveredByForest(n.gx, n.gy))
            {
                if (n.requiresSiteSalvage && !n.siteSalvageCleared)
                    _sim.CompleteNodeSiteSalvage(n.id);
                SmoothFocusGrid(new Vector2(n.gx, n.gy));
                if (n.state != NodeState.HiddenInForest)
                    OpenBuildConfirmCard(n);
            }
            else
            {
                GuideChopTowardNode(n);
            }
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
            for (int i = 0; i < ids.Length; i++)
            {
                CampNode h = _sim.Find(ids[i]);
                if (h != null && h.state == NodeState.HiddenInForest && _sim.CampLevel >= h.unlockPlayerLv)
                {
                    JumpToNodeById(h.id);
                    return;
                }
            }
            JumpToNodeById("house_1");
        }

        void JumpToHeatTower() => JumpToNodeById("heat_tower");

        void JumpToNodeById(string id)
        {
            CloseAllModals();
            CampNode n = _sim.Find(id);
            if (n == null) return;

            if (n.state == NodeState.HiddenInForest)
            {
                if (!n.siteUncovered || (_world != null && _world.IsGridCoveredByForest(n.gx, n.gy)))
                {
                    GuideChopTowardNode(n);
                    return;
                }
                if (n.requiresSiteSalvage && !n.siteSalvageCleared)
                {
                    GuideCollectSalvageForNode(n);
                    return;
                }
                if (_sim.CampLevel >= n.unlockPlayerLv)
                {
                    n.state = NodeState.Discovered;
                    _world.RefreshNodeVisual(n);
                }
                else
                {
                    SmoothFocusGrid(new Vector2(n.gx, n.gy));
                    SpawnWorldRisingText($"空地已清理！需领主升至 Lv.{n.unlockPlayerLv} 解锁【{n.ruinTitle}】", new Vector2(n.gx, n.gy));
                    return;
                }
            }

            if (n.state == NodeState.Constructing)
            {
                SmoothFocusGrid(new Vector2(n.gx, n.gy));
                _sim.SpeedUpNodeBuild(n);
                return;
            }
            if (n.state == NodeState.ReadyToHammer)
            {
                SmoothFocusGrid(new Vector2(n.gx, n.gy));
                _sim.CompleteHammerNode(n);
                return;
            }

            SmoothFocusGrid(new Vector2(n.gx, n.gy));
            OpenBuildConfirmCard(n);
        }

        public void JumpToProduceOrGather(SurvRes r)
        {
            _sim.ResolveResourceProducer(r, out string nodeId, out int recipeIdx, out bool isCraft);
            CloseAllModals();

            if (string.IsNullOrEmpty(nodeId))
            {
                Vector2 fallbackPos = new Vector2(16.0f, 18.0f);
                if (_world != null && _world.TryFindBestChopChunkToward(22.0f, 18.0f, out _, out _, out Vector2 borderChunk))
                    fallbackPos = borderChunk;
                SmoothFocusGrid(fallbackPos);
                SpawnWorldRisingText("请点击营地林缘树木或散落原木堆采集！", fallbackPos);
                return;
            }

            CampNode target = _sim.Find(nodeId);
            if (target == null) return;

            if (target.state == NodeState.HiddenInForest)
            {
                if (!target.siteUncovered || (_world != null && _world.IsGridCoveredByForest(target.gx, target.gy)))
                {
                    GuideChopTowardNode(target);
                    return;
                }
                if (target.requiresSiteSalvage && !target.siteSalvageCleared)
                {
                    GuideCollectSalvageForNode(target);
                    return;
                }
                if (_sim.CampLevel >= target.unlockPlayerLv)
                {
                    target.state = NodeState.Discovered;
                    _world.RefreshNodeVisual(target);
                }
                else
                {
                    SmoothFocusGrid(new Vector2(target.gx, target.gy));
                    SpawnWorldRisingText($"空地已清理！星级达 Lv.{target.unlockPlayerLv} 即可建造【{target.ruinTitle}】", new Vector2(target.gx, target.gy));
                    return;
                }
            }

            if (target.state != NodeState.Active)
            {
                JumpToNodeById(target.id);
                return;
            }

            if (isCraft && ForestSurvivalSim.IsWorkshopKind(target.kind))
            {
                OpenWorkshopDrawer(target, recipeIdx);
            }
            else if (target.kind == NodeKind.Farm)
            {
                CropSeedKind sk = r == SurvRes.Cloth || r == SurvRes.SeedFlax ? CropSeedKind.Flax
                    : (r == SurvRes.Herb || r == SurvRes.SeedHerb ? CropSeedKind.Herb
                    : (r == SurvRes.SeedGolden ? CropSeedKind.Golden : CropSeedKind.Veggie));
                OpenFarmDrawer(sk);
            }
            else if (target.kind == NodeKind.Ranch)
            {
                OpenRanchDrawer();
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

        public void OpenFarmDrawer(CropSeedKind? focusSeed = null)
        {
            CampNode fn = _sim.Find("farm_a");
            if (fn == null) return;
            CloseAllModals();
            if (fn.state != NodeState.Active)
            {
                JumpToNodeById(fn.id);
                return;
            }
            if (focusSeed.HasValue) _selectedSeedKind = focusSeed.Value;

            Vector3 iso = ForestSliceView.Iso(fn.gx, fn.gy);
            _panTarget = new Vector3(iso.x, iso.y - _cam.orthographicSize * 0.42f, -20f);
            _panChangeOrtho = false;
            _panning = true;

            _farmDrawer.SetActive(true);
            RefreshFarmDrawer();
        }

        public void OpenRanchDrawer()
        {
            CampNode rn = _sim.Find("beast_ranch");
            if (rn == null) return;
            CloseAllModals();
            if (rn.state != NodeState.Active)
            {
                JumpToNodeById(rn.id);
                return;
            }

            Vector3 iso = ForestSliceView.Iso(rn.gx, rn.gy);
            _panTarget = new Vector3(iso.x, iso.y - _cam.orthographicSize * 0.42f, -20f);
            _panChangeOrtho = false;
            _panning = true;

            _ranchDrawer.SetActive(true);
            RefreshRanchDrawer();
        }

        void OnFarmPlotMainAction(int plotIdx)
        {
            int cap = _sim.GetFarmPlotCap();
            CampNode fn = _sim.Find("farm_a");
            if (plotIdx >= cap)
            {
                CloseAllModals();
                if (fn != null) OpenBuildConfirmCard(fn);
                return;
            }
            FarmPlotSlot sl = _sim.FarmPlots[plotIdx];
            string msg;
            if (sl.phase == FarmPhase.Ripe)
            {
                if (_sim.TryHarvestFarmPlot(plotIdx, out msg) && fn != null)
                    SpawnWorldRisingText(msg, new Vector2(fn.gx, fn.gy));
            }
            else if (sl.phase == FarmPhase.DryTilled)
            {
                if (!_sim.TryPlantFarmPlot(plotIdx, _selectedSeedKind, allowCoinEmergencyBuy: true, out msg) && fn != null)
                    SpawnWorldRisingText(msg, new Vector2(fn.gx, fn.gy));
                else if (fn != null)
                    SpawnWorldRisingText(msg, new Vector2(fn.gx, fn.gy));
            }
            else
            {
                // 生长中主按钮：免费催熟秒完单垄（兼顾爽快体验！）
                sl.timer = sl.duration;
                sl.phase = FarmPhase.Ripe;
                _sim.SyncFarmVisualPhase();
            }
            RefreshFarmDrawer();
        }

        void OnFarmPlotSubAction(int plotIdx)
        {
            int cap = _sim.GetFarmPlotCap();
            if (plotIdx < 0 || plotIdx >= cap) return;
            FarmPlotSlot sl = _sim.FarmPlots[plotIdx];
            CampNode fn = _sim.Find("farm_a");
            if (sl.phase != FarmPhase.Growing) return;

            if (!sl.fertilized)
            {
                if (_sim.Get(SurvRes.Compost) <= 0)
                {
                    OpenRanchDrawer();
                    SpawnScreenCenterBanner("前往【山林畜牧栏】投喂动物即可稳定副产【有机堆肥】反哺农田！");
                    return;
                }
                if (_sim.TryFertilizeFarmPlot(plotIdx, out string msg) && fn != null)
                    SpawnWorldRisingText(msg, new Vector2(fn.gx, fn.gy));
            }
            else if (!sl.watered)
            {
                if (_sim.Get(SurvRes.Water) <= 0)
                {
                    JumpToNodeById("relic_well");
                    return;
                }
                if (_sim.TryWaterFarmPlot(plotIdx, out string wMsg) && fn != null)
                    SpawnWorldRisingText(wMsg, new Vector2(fn.gx, fn.gy));
            }
            RefreshFarmDrawer();
        }

        void OnFarmBatchActionClicked()
        {
            CampNode fn = _sim.Find("farm_a");
            if (_sim.CountRipeFarmPlots() > 0)
            {
                _sim.HarvestAllRipeFarmPlots(out _);
                RefreshFarmDrawer();
                return;
            }
            int cap = _sim.GetFarmPlotCap();
            int planted = 0;
            string lastErr = null;
            for (int i = 0; i < cap && i < _sim.FarmPlots.Count; i++)
            {
                if (_sim.FarmPlots[i].phase == FarmPhase.DryTilled)
                {
                    if (_sim.TryPlantFarmPlot(i, _selectedSeedKind, allowCoinEmergencyBuy: true, out string m))
                        planted++;
                    else
                        lastErr = m;
                }
            }
            if (fn != null)
            {
                if (planted > 0) SpawnWorldRisingText($"已播下 {planted} 垄【{ForestSurvivalSim.ResName(ForestSurvivalSim.SeedResFor(_selectedSeedKind))}】！", new Vector2(fn.gx, fn.gy));
                else if (!string.IsNullOrEmpty(lastErr)) SpawnWorldRisingText(lastErr, new Vector2(fn.gx, fn.gy));
            }
            RefreshFarmDrawer();
        }

        void RefreshFarmDrawer()
        {
            CampNode fn = _sim.Find("farm_a");
            if (fn == null) return;
            int cap = _sim.GetFarmPlotCap();
            _fmTitleTxt.text = $"{fn.activeTitle} Lv.{fn.level} ({cap}/4垄)";
            _fmUpgradeBtn.gameObject.SetActive(fn.level < fn.maxLevel);

            string irr = _sim.HasAutoIrrigation ? "石井滴灌+35%(已通)" : "滴灌未通(点此升石井)";
            string heat = $"供暖温室+{_sim.GreenhouseBonusPct}%";
            string morale = $"士气+{Mathf.RoundToInt((_sim.CampMoraleMultiplier - 1f) * 100f)}%";
            _fmSynergyTxt.text = $"{irr}  ·  {heat}  ·  {morale}  ·  有机堆肥:{_sim.Get(SurvRes.Compost)}";

            for (int i = 0; i < 4; i++)
            {
                bool unlocked = i < cap;
                if (!unlocked)
                {
                    _fmPlotBgs[i].sprite = ForestComicUiSkin.SubSlot;
                    _fmPlotIcons[i].sprite = ForestComicUiSkin.GetArt("ico_lock", ForestComicUiSkin.CrossBadge);
                    _fmPlotProgressFills[i].fillAmount = 0f;
                    _fmPlotStateTxts[i].text = i == 2 ? "农田Lv.2解锁" : "农田Lv.3解锁";
                    _fmPlotSubBtns[i].gameObject.SetActive(false);
                    _fmPlotMainBtns[i].image.sprite = ForestComicUiSkin.BtnOrange;
                    _fmPlotMainLbls[i].text = "去扩垄";
                    continue;
                }

                FarmPlotSlot sl = _sim.FarmPlots[i];
                _fmPlotBgs[i].sprite = sl.phase == FarmPhase.Ripe ? ForestComicUiSkin.SubSlotSelected : ForestComicUiSkin.ParchmentPanel;
                _fmPlotIcons[i].sprite = sl.phase == FarmPhase.DryTilled
                    ? ForestComicUiSkin.ResIcon(ForestSurvivalSim.SeedResFor(_selectedSeedKind))
                    : ForestComicUiSkin.ResIcon(ForestSurvivalSim.MainCropResFor(sl.seedKind));

                if (sl.phase == FarmPhase.DryTilled)
                {
                    _fmPlotProgressFills[i].fillAmount = 0f;
                    _fmPlotStateTxts[i].text = "空置待播";
                    _fmPlotSubBtns[i].gameObject.SetActive(false);
                    int sCnt = _sim.Get(ForestSurvivalSim.SeedResFor(_selectedSeedKind));
                    _fmPlotMainBtns[i].image.sprite = (sCnt > 0 || (_selectedSeedKind != CropSeedKind.Golden && _sim.Coins >= 25))
                        ? ForestComicUiSkin.BtnBlue : ForestComicUiSkin.BtnGrey;
                    _fmPlotMainLbls[i].text = sCnt > 0 ? "播种" : (_selectedSeedKind == CropSeedKind.Golden ? "缺金种" : "补种(25币)");
                }
                else if (sl.phase == FarmPhase.Growing)
                {
                    float prog = Mathf.Clamp01(sl.timer / Mathf.Max(0.1f, sl.duration));
                    float spd = _sim.FarmSpeedMultiplier * (sl.watered ? 1f : 0.68f);
                    int rem = Mathf.CeilToInt(Mathf.Max(0.1f, (sl.duration - sl.timer) / Mathf.Max(0.2f, spd)));
                    _fmPlotProgressFills[i].fillAmount = prog;
                    _fmPlotStateTxts[i].text = (sl.fertilized ? "丰产×1.5 · " : (sl.watered ? "湿润 · " : "缺水 · ")) + $"{rem}秒";

                    _fmPlotSubBtns[i].gameObject.SetActive(!sl.fertilized || !sl.watered);
                    if (!sl.fertilized)
                    {
                        bool hasComp = _sim.Get(SurvRes.Compost) > 0;
                        _fmPlotSubBtns[i].image.sprite = hasComp ? ForestComicUiSkin.BtnOrange : ForestComicUiSkin.BtnBlue;
                        _fmPlotSubLbls[i].text = hasComp ? "撒堆肥(×1.5)" : "去畜牧产肥";
                    }
                    else if (!sl.watered)
                    {
                        _fmPlotSubBtns[i].image.sprite = ForestComicUiSkin.BtnBlue;
                        _fmPlotSubLbls[i].text = "引水浇灌";
                    }

                    _fmPlotMainBtns[i].image.sprite = ForestComicUiSkin.BtnGreen;
                    _fmPlotMainLbls[i].text = "秒熟>>";
                }
                else
                {
                    _fmPlotProgressFills[i].fillAmount = 1f;
                    _fmPlotStateTxts[i].text = sl.fertilized ? "✔ 丰产成熟!" : "✔ 可收割!";
                    _fmPlotSubBtns[i].gameObject.SetActive(false);
                    _fmPlotMainBtns[i].image.sprite = ForestComicUiSkin.BtnGreen;
                    _fmPlotMainLbls[i].text = "立即收割";
                }
            }

            for (int i = 0; i < 4; i++)
            {
                CropSeedKind sk = (CropSeedKind)i;
                bool sel = sk == _selectedSeedKind;
                _fmSeedFrames[i].sprite = sel ? ForestComicUiSkin.SubSlotSelected : ForestComicUiSkin.SubSlot;
                int own = _sim.Get(ForestSurvivalSim.SeedResFor(sk));
                _fmSeedCntTxts[i].text = own > 0 ? $"拥有: {own}" : (sk == CropSeedKind.Golden ? "稀有掉落" : "25币补种");
                _fmSeedCntTxts[i].color = own > 0 ? ForestUiWidgets.LushGreen : ForestUiWidgets.InkMuted;
            }

            _fmYieldPreviewTxt.text = $"选中【{ForestSurvivalSim.CropTitleFor(_selectedSeedKind)}】单垄预计产出：{_sim.GetCropYieldPreview(_selectedSeedKind, false)}";
            _fmAutoLbl.text = _sim.AutoReplantFarm ? "自动连种:开" : "自动连种:关";

            int ripeCnt = _sim.CountRipeFarmPlots();
            int emptyCnt = _sim.CountEmptyFarmPlots();
            if (ripeCnt > 0)
            {
                _fmMainBtn.image.sprite = ForestComicUiSkin.BtnGreen;
                _fmMainBtnLbl.text = $"一键全收 ({ripeCnt}垄)";
            }
            else if (emptyCnt > 0)
            {
                _fmMainBtn.image.sprite = ForestComicUiSkin.BtnBlue;
                _fmMainBtnLbl.text = $"一键全播 ({emptyCnt}垄)";
            }
            else
            {
                _fmMainBtn.image.sprite = ForestComicUiSkin.BtnGrey;
                _fmMainBtnLbl.text = "作物生长中";
            }
        }

        void OnRanchPenAction(int penIdx)
        {
            int cap = _sim.GetRanchPenCap();
            CampNode rn = _sim.Find("beast_ranch");
            if (penIdx >= cap)
            {
                CloseAllModals();
                if (rn != null) OpenBuildConfirmCard(rn);
                return;
            }
            RanchPenSlot pen = _sim.RanchPens[penIdx];
            if (pen.readyToCollect)
            {
                if (_sim.TryCollectRanchPen(penIdx, out string msg) && rn != null)
                    SpawnWorldRisingText(msg, new Vector2(rn.gx, rn.gy));
            }
            else if (pen.feeding)
            {
                pen.timer = pen.duration;
                pen.feeding = false;
                pen.readyToCollect = true;
            }
            else
            {
                if (_sim.Get(SurvRes.Veggie) < 2)
                {
                    OpenFarmDrawer(CropSeedKind.Veggie);
                    SpawnScreenCenterBanner("饲草不足！先在【生态选种农田】播下鲜蔬种收获鲜蔬投喂动物！");
                    return;
                }
                if (_sim.Get(SurvRes.Water) < 1)
                {
                    JumpToNodeById("relic_well");
                    return;
                }
                if (!_sim.TryFeedRanchPen(penIdx, _selectedRanchAnimal, out string fMsg) && rn != null)
                    SpawnWorldRisingText(fMsg, new Vector2(rn.gx, rn.gy));
                else if (rn != null)
                    SpawnWorldRisingText(fMsg, new Vector2(rn.gx, rn.gy));
            }
            RefreshRanchDrawer();
        }

        void OnRanchBatchActionClicked()
        {
            CampNode rn = _sim.Find("beast_ranch");
            if (_sim.CountReadyRanchPens() > 0)
            {
                _sim.CollectAllReadyRanchPens(out _);
                RefreshRanchDrawer();
                return;
            }
            if (_sim.Get(SurvRes.Veggie) < 2)
            {
                OpenFarmDrawer(CropSeedKind.Veggie);
                SpawnScreenCenterBanner("缺少农田【鲜蔬】饲料！为您跳转至农田播种！");
                return;
            }
            if (_sim.Get(SurvRes.Water) < 1)
            {
                JumpToNodeById("relic_well");
                return;
            }
            int cap = _sim.GetRanchPenCap();
            int fed = 0;
            string lastErr = null;
            for (int i = 0; i < cap && i < _sim.RanchPens.Count; i++)
            {
                if (!_sim.RanchPens[i].feeding && !_sim.RanchPens[i].readyToCollect)
                {
                    if (_sim.TryFeedRanchPen(i, _selectedRanchAnimal, out string m)) fed++;
                    else lastErr = m;
                }
            }
            if (rn != null)
            {
                if (fed > 0) SpawnWorldRisingText($"已向 {fed} 栏投喂【{ForestSurvivalSim.RanchAnimalName(_selectedRanchAnimal)}】饲料！", new Vector2(rn.gx, rn.gy));
                else if (!string.IsNullOrEmpty(lastErr)) SpawnWorldRisingText(lastErr, new Vector2(rn.gx, rn.gy));
            }
            RefreshRanchDrawer();
        }

        void RefreshRanchDrawer()
        {
            CampNode rn = _sim.Find("beast_ranch");
            if (rn == null) return;
            int cap = _sim.GetRanchPenCap();
            int rLv = Mathf.Max(1, rn.level);
            _rnTitleTxt.text = $"{rn.activeTitle} Lv.{rLv} ({cap}/4栏)";
            _rnUpgradeBtn.gameObject.SetActive(rLv < rn.maxLevel);

            string wtTag = _sim.HasWatchtower ? "哨塔良种+1肉皮(已通)" : "哨塔良种未激活";
            _rnSynergyTxt.text = $"农田鲜蔬:{_sim.Get(SurvRes.Veggie)}  ·  石井净水:{_sim.Get(SurvRes.Water)}  ·  {wtTag}  ·  供暖防寒+{_sim.GreenhouseBonusPct}%";

            for (int i = 0; i < 4; i++)
            {
                bool unlocked = i < cap;
                if (!unlocked)
                {
                    _rnPenBgs[i].sprite = ForestComicUiSkin.SubSlot;
                    _rnPenIcons[i].sprite = ForestComicUiSkin.GetArt("ico_lock", ForestComicUiSkin.CrossBadge);
                    _rnPenProgressFills[i].fillAmount = 0f;
                    _rnPenStateTxts[i].text = i == 2 ? "兽栏Lv.2解锁" : "兽栏Lv.3解锁";
                    _rnPenBtns[i].image.sprite = ForestComicUiSkin.BtnOrange;
                    _rnPenBtnLbls[i].text = "去扩栏";
                    continue;
                }

                RanchPenSlot pen = _sim.RanchPens[i];
                _rnPenBgs[i].sprite = pen.readyToCollect ? ForestComicUiSkin.SubSlotSelected : ForestComicUiSkin.ParchmentPanel;
                RanchAnimalKind dispA = (pen.feeding || pen.readyToCollect) ? pen.animal : _selectedRanchAnimal;
                string spKey = dispA == RanchAnimalKind.WildBoar ? "boar" : (dispA == RanchAnimalKind.AntlerDeer ? "deer" : "ico_pelt");
                _rnPenIcons[i].sprite = ForestComicUiSkin.GetArt(spKey, ForestComicUiSkin.ResIcon(SurvRes.Pelt));

                if (pen.readyToCollect)
                {
                    _rnPenProgressFills[i].fillAmount = 1f;
                    _rnPenStateTxts[i].text = $"✔ {ForestSurvivalSim.RanchAnimalName(pen.animal)}已肥";
                    _rnPenBtns[i].image.sprite = ForestComicUiSkin.BtnGreen;
                    _rnPenBtnLbls[i].text = "收取肉皮肥";
                }
                else if (pen.feeding)
                {
                    float prog = Mathf.Clamp01(pen.timer / Mathf.Max(0.1f, pen.duration));
                    int rem = Mathf.CeilToInt(Mathf.Max(0.1f, (pen.duration - pen.timer) / Mathf.Max(0.2f, _sim.RanchSpeedMultiplier)));
                    _rnPenProgressFills[i].fillAmount = prog;
                    _rnPenStateTxts[i].text = $"{ForestSurvivalSim.RanchAnimalName(pen.animal)} {rem}秒";
                    _rnPenBtns[i].image.sprite = ForestComicUiSkin.BtnOrange;
                    _rnPenBtnLbls[i].text = "秒肥>>";
                }
                else
                {
                    _rnPenProgressFills[i].fillAmount = 0f;
                    _rnPenStateTxts[i].text = "空槽待喂";
                    bool afford = _sim.Get(SurvRes.Veggie) >= 2 && _sim.Get(SurvRes.Water) >= 1 && rLv >= ForestSurvivalSim.RanchAnimalUnlockLv(_selectedRanchAnimal);
                    _rnPenBtns[i].image.sprite = afford ? ForestComicUiSkin.BtnBlue : ForestComicUiSkin.BtnGrey;
                    _rnPenBtnLbls[i].text = _sim.Get(SurvRes.Veggie) < 2 ? "去种鲜蔬" : (_sim.Get(SurvRes.Water) < 1 ? "去打井水" : "投喂(蔬2水1)");
                }
            }

            for (int i = 0; i < 3; i++)
            {
                RanchAnimalKind ak = (RanchAnimalKind)i;
                int reqLv = ForestSurvivalSim.RanchAnimalUnlockLv(ak);
                bool unl = rLv >= reqLv;
                _rnBreedFrames[i].sprite = (_selectedRanchAnimal == ak) ? ForestComicUiSkin.SubSlotSelected : ForestComicUiSkin.SubSlot;
                _sim.GetRanchYield(ak, out int m, out int p, out int c);
                _rnBreedSubTxts[i].text = unl ? $"肉+{m} 皮+{p} 肥+{c}" : $"畜牧栏Lv.{reqLv}解锁";
                _rnBreedSubTxts[i].color = unl ? ForestUiWidgets.LushGreen : ForestUiWidgets.AlertRed;
            }

            _sim.GetRanchYield(_selectedRanchAnimal, out int sm, out int sp, out int sc);
            _rnBreedPreviewTxt.text = $"【{ForestSurvivalSim.RanchAnimalName(_selectedRanchAnimal)}】产出：鲜肉+{sm} · 兽毛皮+{sp} · 有机堆肥+{sc} (当前堆肥:{_sim.Get(SurvRes.Compost)})";
            _rnAutoLbl.text = _sim.AutoFeedRanch ? "自动续喂:开" : "自动续喂:关";

            int readyCnt = _sim.CountReadyRanchPens();
            int idleCnt = _sim.CountIdleRanchPens();
            if (readyCnt > 0)
            {
                _rnMainBtn.image.sprite = ForestComicUiSkin.BtnGreen;
                _rnMainBtnLbl.text = $"一键全收 ({readyCnt}栏)";
            }
            else if (idleCnt > 0)
            {
                _rnMainBtn.image.sprite = ForestComicUiSkin.BtnBlue;
                _rnMainBtnLbl.text = $"一键全喂 ({idleCnt}栏)";
            }
            else
            {
                _rnMainBtn.image.sprite = ForestComicUiSkin.BtnGrey;
                _rnMainBtnLbl.text = "动物育肥中";
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
                Vector2 g = new Vector2(15.6f, 16.0f);
                if (_world.CivilianVillagers.Count > 0)
                {
                    int vIdx = Mathf.Clamp(d.walkerIndex, 0, _world.CivilianVillagers.Count - 1);
                    g = new Vector2(_world.CivilianVillagers[vIdx].gx, _world.CivilianVillagers[vIdx].gy);
                }
                if (_sim.TrySatisfyOrder(slot, g, out string thanks))
                {
                    if (_world.CivilianVillagers.Count > 0)
                    {
                        int vIdx = Mathf.Clamp(d.walkerIndex, 0, _world.CivilianVillagers.Count - 1);
                        _world.CivilianVillagers[vIdx].chatterText = thanks;
                        _world.CivilianVillagers[vIdx].chatterTimer = 3.8f;
                        _world.CivilianVillagers[vIdx].cheerTimer = 1.6f;
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
            if (_world != null && _world.InWaveDefense)
            {
                SpawnScreenCenterBanner($"★ 营地升至 {newLv} 级！");
                return;
            }
            CloseAllModals();
            _lvUpNumTxt.text = $"{newLv}级";
            int count = unlockedNames != null ? Mathf.Min(3, unlockedNames.Length) : 0;
            float startX = -(count - 1) * 76f;
            for (int i = 0; i < 3; i++)
            {
                bool show = i < count;
                _lvUpUnlockCards[i].SetActive(show);
                if (!show) continue;
                var cardRt = _lvUpUnlockCards[i].GetComponent<RectTransform>();
                if (cardRt != null) cardRt.anchoredPosition = new Vector2(startX + i * 152f, 4f);
                _lvUpUnlockIcons[i].sprite = ResolveUnlockCardSprite(unlockedNames[i]);
                _lvUpUnlockNames[i].text = unlockedNames[i];
            }
            _levelUpModal.SetActive(true);
        }

        static Sprite ResolveUnlockCardSprite(string title)
        {
            if (string.IsNullOrEmpty(title))
                return ForestComicUiSkin.GetArt("ico_xp_star", ForestComicUiSkin.ResIcon(SurvRes.Blueprint));
            if (title.Contains("农田") || title.Contains("种") || title.Contains("垄"))
                return ForestComicUiSkin.GetArt("farm_2", ForestComicUiSkin.ResIcon(SurvRes.Veggie));
            if (title.Contains("石井") || title.Contains("滴灌") || title.Contains("净水"))
                return ForestComicUiSkin.GetArt("well", ForestComicUiSkin.ResIcon(SurvRes.Water));
            if (title.Contains("畜牧") || title.Contains("兽栏"))
                return ForestComicUiSkin.GetArt("boar", ForestComicUiSkin.ResIcon(SurvRes.Pelt));
            if (title.Contains("锯木") || title.Contains("木板"))
                return ForestComicUiSkin.GetArt("sawmill_shed", ForestComicUiSkin.ResIcon(SurvRes.Plank));
            if (title.Contains("木床"))
                return ForestComicUiSkin.ResIcon(SurvRes.WoodBed);
            if (title.Contains("承重梁"))
                return ForestComicUiSkin.ResIcon(SurvRes.IronBeam);
            if (title.Contains("锻造") || title.Contains("铁钉"))
                return ForestComicUiSkin.ResIcon(SurvRes.Nails);
            if (title.Contains("铁锭"))
                return ForestComicUiSkin.ResIcon(SurvRes.IronIngot);
            if (title.Contains("管"))
                return ForestComicUiSkin.ResIcon(SurvRes.MetalPipe);
            if (title.Contains("裁缝") || title.Contains("麻布衣"))
                return ForestComicUiSkin.ResIcon(SurvRes.LinenShirt);
            if (title.Contains("皮靴"))
                return ForestComicUiSkin.ResIcon(SurvRes.LeatherBoots);
            if (title.Contains("皮衣"))
                return ForestComicUiSkin.ResIcon(SurvRes.FurCoat);
            if (title.Contains("护甲") || title.Contains("重甲"))
                return ForestComicUiSkin.ResIcon(SurvRes.PlateArmor);
            if (title.Contains("汤") || title.Contains("炊事") || title.Contains("厨房") || title.Contains("灶"))
                return ForestComicUiSkin.ResIcon(SurvRes.VeggieSoup);
            if (title.Contains("鱼") || title.Contains("钓"))
                return ForestComicUiSkin.GetArt("fishing_dock", ForestComicUiSkin.ResIcon(SurvRes.Fish));
            if (title.Contains("熏肉"))
                return ForestComicUiSkin.ResIcon(SurvRes.SmokedMeat);
            if (title.Contains("草药") || title.Contains("敷包"))
                return ForestComicUiSkin.ResIcon(SurvRes.HerbPoultice);
            if (title.Contains("热饮"))
                return ForestComicUiSkin.ResIcon(SurvRes.WarmTea);
            if (title.Contains("急救箱") || title.Contains("药箱"))
                return ForestComicUiSkin.ResIcon(SurvRes.MedKit);
            if (title.Contains("民居") || title.Contains("木楼"))
                return ForestComicUiSkin.GetArt("refugee_cabin", ForestComicUiSkin.GetArt("tent"));
            if (title.Contains("供暖"))
                return ForestComicUiSkin.GetArt("heating_tower", ForestComicUiSkin.GetArt("campfire"));
            if (title.Contains("仓库"))
                return ForestComicUiSkin.GetArt("dock_bag", ForestComicUiSkin.ResIcon(SurvRes.Plank));
            if (title.Contains("议事厅") || title.Contains("避难所") || title.Contains("木堡"))
                return ForestComicUiSkin.GetArt("shelter_lv2", ForestComicUiSkin.ResIcon(SurvRes.Blueprint));
            if (title.Contains("哨塔"))
                return ForestComicUiSkin.GetArt("hunter_tower", ForestComicUiSkin.ResIcon(SurvRes.Meat));
            if (title.Contains("拒马") || title.Contains("防线"))
                return ForestComicUiSkin.GetArt("dock_defense", ForestComicUiSkin.ResIcon(SurvRes.HeroMedal));
            if (title.Contains("皮卡") || title.Contains("丧尸城") || title.Contains("关卡") || title.Contains("公路"))
                return ForestComicUiSkin.GetArt("truck_active", ForestComicUiSkin.GetArt("dock_map"));
            if (title.Contains("发电"))
                return ForestComicUiSkin.GetArt("generator_active", ForestComicUiSkin.GetArt("ruin_generator"));
            if (title.Contains("电台") || title.Contains("行商"))
                return ForestComicUiSkin.GetArt("dock_radio", ForestComicUiSkin.ResIcon(SurvRes.Blueprint));
            if (title.Contains("雷猛"))
                return ForestComicUiSkin.GetArt("bust_vil_0", ForestComicUiSkin.GetArt("bust_dachui"));
            if (title.Contains("林砚"))
                return ForestComicUiSkin.GetArt("bust_linyan", ForestComicUiSkin.GetArt("bust_dachui"));
            if (title.Contains("铁叔"))
                return ForestComicUiSkin.GetArt("survivor_tieshu", ForestComicUiSkin.GetArt("bust_dachui"));
            if (title.Contains("老陈头"))
                return ForestComicUiSkin.GetArt("bust_laochentou", ForestComicUiSkin.GetArt("bust_dachui"));
            if (title.Contains("大锤"))
                return ForestComicUiSkin.GetArt("bust_dachui", ForestComicUiSkin.GetArt("bust_driver"));
            if (title.Contains("小棠"))
                return ForestComicUiSkin.GetArt("bust_xiaotang", ForestComicUiSkin.GetArt("bust_driver"));
            return ForestComicUiSkin.GetArt("ico_xp_star", ForestComicUiSkin.ResIcon(SurvRes.Blueprint));
        }

        void ShowHeroSpotlight(HeroSpec h)
        {
            if (h == null) return;
            if (_world != null && _world.InWaveDefense) return;
            CloseAllModals();
            _heroSpotBust.sprite = ForestComicUiSkin.GetArt(h.portraitKey, ForestComicUiSkin.GetArt("bust_driver"));
            _heroSpotNameTxt.text = h.name;
            _heroSpotRoleTxt.text = h.roleTitle;
            _heroSpotBuffTxt.text = h.buffDesc;
            _heroSpotlightModal.SetActive(true);
        }

        public void OpenHeroAssignModal() => OpenHeroModal(0);

        public void OpenHeroModal(int tabIndex)
        {
            CloseAllModals();
            _heroActiveTab = Mathf.Clamp(tabIndex, 0, 1);
            RefreshHeroAssignModal();
            _heroAssignModal.SetActive(true);
        }

        void SwitchHeroModalTab(int tabIndex)
        {
            _heroActiveTab = Mathf.Clamp(tabIndex, 0, 1);
            RefreshHeroAssignModal();
        }

        void OnDelegationSlotClicked(int postIdx, int slotIdx)
        {
            if (postIdx < 0 || postIdx >= _sim.DelegationPosts.Count) return;
            DelegationPostState st = _sim.DelegationPosts[postIdx];
            if (!st.IsSlotUnlocked(slotIdx, _sim.CampLevel))
            {
                int req = slotIdx < st.slotUnlockLevels.Length ? st.slotUnlockLevels[slotIdx] : 99;
                SpawnWorldRisingText($"该委任槽位需营地星级达到 {req} 级开放！", new Vector2(16.4f, 16.2f));
                return;
            }

            string curId = st.assignedHeroIds[slotIdx];
            if (!string.IsNullOrEmpty(curId))
            {
                // 点击已驻守槽位：卸下该槽位英雄，方便手动调配
                _sim.AssignHeroToPostSlot(st.kind, slotIdx, null);
                RefreshHeroAssignModal();
                return;
            }

            // 点击空槽：优先选尚未分配且对本岗位评分最高的已招募英雄；若全员已在岗则调来评分最高者
            HeroSpec bestFree = null;
            float bestFreeSc = -1f;
            HeroSpec bestAny = null;
            float bestAnySc = -1f;
            for (int i = 0; i < _sim.Heroes.Count; i++)
            {
                HeroSpec h = _sim.Heroes[i];
                if (!h.unlocked) continue;
                float sc = h.GetDelegationScore(st.kind);
                if (sc > bestAnySc) { bestAnySc = sc; bestAny = h; }
                if (!h.assigned && sc > bestFreeSc) { bestFreeSc = sc; bestFree = h; }
            }
            HeroSpec chosen = bestFree ?? bestAny;
            if (chosen != null)
            {
                _sim.AssignHeroToPostSlot(st.kind, slotIdx, chosen.id);
                RefreshHeroAssignModal();
                SpawnWorldRisingText($"已委任【{chosen.name}】驻守【{st.title}】！", new Vector2(16.4f, 16.2f));
            }
        }

        void OnHeroLevelUpClicked(int rowIdx)
        {
            if (rowIdx < 0 || rowIdx >= _sim.Heroes.Count) return;
            HeroSpec h = _sim.Heroes[rowIdx];
            if (_sim.TryLevelUpHero(h.id, out string msg))
                RefreshHeroAssignModal();
            SpawnWorldRisingText(msg, new Vector2(16.4f, 16.2f));
        }

        void OnHeroStarUpClicked(int rowIdx)
        {
            if (rowIdx < 0 || rowIdx >= _sim.Heroes.Count) return;
            HeroSpec h = _sim.Heroes[rowIdx];
            if (!h.unlocked)
            {
                SpawnWorldRisingText("该骨干伙伴随营地星级提升自动招募入队！", new Vector2(16.4f, 16.2f));
                return;
            }
            if (_sim.Get(SurvRes.HeroMedal) < ForestSurvivalSim.GetHeroStarUpMedals(h.stars) && h.stars < 5)
            {
                OpenSourceRouteForCondition(CondKind.Resource, SurvRes.HeroMedal);
                return;
            }
            if (_sim.TryStarUpHero(h.id, out string msg))
                RefreshHeroAssignModal();
            SpawnWorldRisingText(msg, new Vector2(16.4f, 16.2f));
        }

        void OnHeroSkinTierClicked(int rowIdx, OutfitTier tier)
        {
            if (rowIdx < 0 || rowIdx >= _sim.Heroes.Count) return;
            HeroSpec h = _sim.Heroes[rowIdx];
            if (_sim.SetHeroSkinTier(h.id, tier, out string msg))
                RefreshHeroAssignModal();
            SpawnWorldRisingText(msg, new Vector2(16.4f, 16.2f));
        }

        static string FormatHeroSinglePostContribution(HeroSpec h, DelegationPostKind kind)
        {
            float s = h.GetDelegationScore(kind);
            switch (kind)
            {
                case DelegationPostKind.WorkshopSpeed: return $"+{Mathf.RoundToInt(s * 1.4f)}%";
                case DelegationPostKind.BuildSpeed: return $"+{Mathf.RoundToInt(s * 1.6f)}%";
                case DelegationPostKind.FarmYield: return $"+{Mathf.RoundToInt(s * 1.6f)}%";
                case DelegationPostKind.DefensePower: return $"+{Mathf.RoundToInt(s * 1.8f)}%";
                case DelegationPostKind.TruckArmor: return $"+{Mathf.RoundToInt(s * 0.9f)}载";
                default: return $"+{Mathf.RoundToInt(s * 1.2f)}%";
            }
        }

        void RefreshHeroAssignModal()
        {
            bool showDel = _heroActiveTab == 0;
            _haHeaderTitleTxt.text = showDel ? "骨干驻守委任" : "骨干养成 · 升星换装";
            if (_haLeaderBannerTxt != null)
                _haLeaderBannerTxt.text = $"领主【阿澈】等级即左上角玩家等级 (Lv.{_sim.CampLevel}) · 招募幸存者骨干负责驻守与闯关出战";
            _haDelegationPage.SetActive(showDel);
            _haGrowthPage.SetActive(!showDel);
            _haTabBtn0.GetComponent<Image>().sprite = showDel ? ForestComicUiSkin.BtnOrange : ForestComicUiSkin.BtnGrey;
            _haTabBtn1.GetComponent<Image>().sprite = !showDel ? ForestComicUiSkin.BtnOrange : ForestComicUiSkin.BtnGrey;

            // 刷新 Tab 0：水世界式 6 大岗位多槽委任
            for (int p = 0; p < 6 && p < _sim.DelegationPosts.Count; p++)
            {
                DelegationPostState st = _sim.DelegationPosts[p];
                bool postOpen = st.IsSlotUnlocked(0, _sim.CampLevel);
                _delPostEffectTxts[p].text = postOpen ? _sim.GetPostEffectSummary(st.kind) : $"Lv.{st.slotUnlockLevels[0]}开放";
                _delPostEffectTxts[p].color = postOpen ? ForestUiWidgets.LushGreen : ForestUiWidgets.InkMuted;

                _delPostLockedHintGos[p].SetActive(!postOpen);
                if (!postOpen)
                    _delPostLockedHintTxts[p].text = $"玩家 {st.slotUnlockLevels[0]} 级解锁";

                for (int s = 0; s < 4; s++)
                {
                    bool hasSlot = postOpen && s < st.slotUnlockLevels.Length;
                    _delSlotBoxes[p, s].SetActive(hasSlot);
                    if (!hasSlot) continue;

                    bool slotOpen = st.IsSlotUnlocked(s, _sim.CampLevel);
                    HeroSpec h = slotOpen ? _sim.FindHero(st.assignedHeroIds[s]) : null;
                    if (!slotOpen)
                    {
                        _delSlotFrames[p, s].sprite = ForestComicUiSkin.BtnGrey;
                        _delSlotBusts[p, s].gameObject.SetActive(false);
                        _delSlotLocks[p, s].gameObject.SetActive(true);
                        _delSlotStarTags[p, s].text = "";
                        _delSlotSubTxts[p, s].text = $"{st.slotUnlockLevels[s]}级解锁";
                        _delSlotSubTxts[p, s].color = Color.white;
                    }
                    else if (h != null && h.unlocked)
                    {
                        _delSlotFrames[p, s].sprite = ForestComicUiSkin.RarityButtonSprite(h.rarity);
                        _delSlotBusts[p, s].gameObject.SetActive(true);
                        _delSlotBusts[p, s].sprite = ForestComicUiSkin.GetArt(h.portraitKey, ForestComicUiSkin.GetArt("bust_dachui"));
                        _delSlotLocks[p, s].gameObject.SetActive(false);
                        _delSlotStarTags[p, s].text = $"★{h.stars}";
                        _delSlotSubTxts[p, s].text = $"{h.name} {FormatHeroSinglePostContribution(h, st.kind)}";
                        _delSlotSubTxts[p, s].color = Color.white;
                    }
                    else
                    {
                        _delSlotFrames[p, s].sprite = ForestComicUiSkin.ParchmentPanel;
                        _delSlotBusts[p, s].gameObject.SetActive(false);
                        _delSlotLocks[p, s].gameObject.SetActive(false);
                        _delSlotStarTags[p, s].text = "";
                        _delSlotSubTxts[p, s].text = "＋ 可委任";
                        _delSlotSubTxts[p, s].color = ForestUiWidgets.LushGreen;
                    }
                }
            }

            // 刷新 Tab 1：6 名英雄升级、升星与 3 档防具皮肤
            _haGrowthResTxt.text = $"铜币: {_sim.Coins}   ·   熟食口粮: {_sim.Get(SurvRes.Ration)}   ·   英雄勋章: {_sim.Get(SurvRes.HeroMedal)}";
            OutfitTier maxTier = _sim.MaxUnlockedSkinTier;
            for (int i = 0; i < 6; i++)
            {
                bool show = i < _sim.Heroes.Count;
                if (_haRows[i] != null) _haRows[i].SetActive(show);
                if (!show) continue;

                HeroSpec h = _sim.Heroes[i];
                _haRarityFrames[i].sprite = h.unlocked ? ForestComicUiSkin.RarityButtonSprite(h.rarity) : ForestComicUiSkin.BtnGrey;
                _haBusts[i].sprite = ForestComicUiSkin.GetArt(h.portraitKey, ForestComicUiSkin.GetArt("bust_driver"));
                _haBusts[i].color = h.unlocked ? Color.white : new Color(0.28f, 0.28f, 0.28f, 0.85f);
                _haStarBadges[i].text = h.unlocked ? $"★{h.stars} Lv.{h.level}" : "未入队";
                _haNames[i].text = $"{h.name} [{h.RarityTag}]  ·  战力 {h.CombatPower}";
                _haDescs[i].text = $"绝技:{h.skillName}  (攻{h.Attack}/血{h.MaxHp})\n{h.buffDesc}";

                int cNeed = ForestSurvivalSim.GetHeroLevelUpCoins(h.level);
                int rNeed = ForestSurvivalSim.GetHeroLevelUpRations(h.level);
                bool canLv = h.unlocked && h.level < _sim.MaxHeroLevelCap && _sim.Coins >= cNeed && _sim.Get(SurvRes.Ration) >= rNeed;
                _haLvUpBtns[i].GetComponent<Image>().sprite = canLv ? ForestComicUiSkin.BtnGreen : ForestComicUiSkin.BtnGrey;
                _haLvUpLbls[i].text = !h.unlocked ? "待招募" : (h.level >= _sim.MaxHeroLevelCap ? "达上限" : $"升级\n{cNeed}币/{rNeed}粮");

                int mNeed = ForestSurvivalSim.GetHeroStarUpMedals(h.stars);
                int mOwn = _sim.Get(SurvRes.HeroMedal);
                bool canStar = h.unlocked && h.stars < 5 && mOwn >= mNeed;
                _haStarUpBtns[i].GetComponent<Image>().sprite = canStar ? ForestComicUiSkin.BtnOrange : (h.unlocked && h.stars < 5 ? ForestComicUiSkin.BtnBlue : ForestComicUiSkin.BtnGrey);
                _haStarUpLbls[i].text = !h.unlocked ? "待招募" : (h.stars >= 5 ? "★满星" : $"升星\n勋章{mOwn}/{mNeed}");

                string[] skinBuffTags = { "初装", "皮衣+30%血", "重甲+75%血" };
                for (int t = 0; t < 3; t++)
                {
                    OutfitTier tier = (OutfitTier)t;
                    bool unlocked = h.unlocked && tier <= maxTier;
                    bool equipped = h.skinTier == tier;
                    _haSkinBtns[i, t].GetComponent<Image>().sprite =
                        equipped ? ForestComicUiSkin.BtnGreen : (unlocked ? ForestComicUiSkin.BtnBlue : ForestComicUiSkin.BtnGrey);
                    _haSkinTexts[i, t].text = equipped ? $"已穿·{skinBuffTags[t]}" : (unlocked ? skinBuffTags[t] : $"未开·{skinBuffTags[t]}");
                }
            }
        }

        // ---------------- 独立策略战场：战前海报 + 九宫格布阵 + 实时大招 HUD + MVP 连战结算 ----------------
        public void OpenWavePrepModal() => OpenStagePosterModal();

        public void OpenStagePosterModal()
        {
            CloseAllModals();
            ClearFloatingWorldToasts();
            if (_sim.GetDeployedHeroCount() <= 0)
                _sim.AutoDeployRecommendedFormation();
            RefreshStagePosterModal();
            _wavePrepModal.SetActive(true);
        }

        void RefreshStagePosterModal()
        {
            int curStage = _sim.CurrentBattleStage;
            BattleStageSpec spec = _sim.GetBattleStageSpec(curStage);
            _spStageBadgeTxt.text = $"第 {spec.stage} / {MaxBattleStages} 关";
            _spBossIconImg.sprite = spec.isBossStage
                ? ForestComicUiSkin.GetArt("bust_zombie_boss", ForestComicUiSkin.GetArt("dock_defense"))
                : ForestComicUiSkin.GetArt("dock_defense");
            _spStageTitleTxt.text = spec.title;
            _spStageSubTxt.text = string.IsNullOrEmpty(spec.subtitle) ? "清剿逼近山口断桥防线的感染尸群" : spec.subtitle;

            _spThreatTag1Bg.sprite = spec.isBossStage ? ForestComicUiSkin.BtnRed : ForestComicUiSkin.BtnBlue;
            _spThreatTag1Txt.text = spec.isBossStage ? "[Boss 首领战]" : $"[哨卡敌军 · {spec.enemyTotalCount}名]";

            _spThreatTag2Bg.sprite = spec.hasBacklineFlanker ? ForestComicUiSkin.BtnOrange : ForestComicUiSkin.BtnGrey;
            _spThreatTag2Txt.text = spec.hasBacklineFlanker ? "[! 跳尸切后排警告]" : "[正面前排推进战]";

            int myPwr = _sim.CalcDeployedSquadPower();
            int recPwr = Mathf.Max(1, spec.recommendedPower);
            _spPowerCompareTxt.text = $"我方小队战力 {myPwr}   VS   敌方推荐战力 {recPwr}";
            if (myPwr >= recPwr)
            {
                _spPowerStatePillBg.sprite = ForestComicUiSkin.BtnGreen;
                _spPowerStateTxt.text = "★ 战力压制 · 稳操胜券";
            }
            else if (myPwr >= Mathf.RoundToInt(recPwr * 0.8f))
            {
                _spPowerStatePillBg.sprite = ForestComicUiSkin.BtnOrange;
                _spPowerStateTxt.text = "势均力敌 · 巧用阵型";
            }
            else
            {
                _spPowerStatePillBg.sprite = ForestComicUiSkin.BtnRed;
                _spPowerStateTxt.text = "高危挑战 · 建议强阵升星";
            }

            int depCnt = _sim.GetDeployedHeroCount();
            int maxDep = _sim.GetMaxDeployHeroCount();
            string fmtName = _sim.GetFormationName(_sim.SelectedFormation);
            _spSquadSummaryTxt.text = $"已上阵 {depCnt}/{maxDep} 人  ·  当前阵型：【{fmtName}】";

            var deployedPairs = new List<KeyValuePair<int, int>>(5);
            for (int c = 2; c >= 0; c--)
            {
                for (int r = 0; r < 3; r++)
                {
                    int sl = r * 3 + c;
                    int hIdx = _sim.FormationSlots[sl];
                    if (hIdx >= 0 && hIdx < _sim.Heroes.Count)
                        deployedPairs.Add(new KeyValuePair<int, int>(hIdx, sl));
                }
            }

            string[] colTags = { "后排", "中排", "前排" };
            int[] slotExpandLvs = { 1, 1, 4, 8, 12 };
            for (int i = 0; i < 5; i++)
            {
                if (i < deployedPairs.Count)
                {
                    int hIdx = deployedPairs[i].Key;
                    int sl = deployedPairs[i].Value;
                    HeroSpec h = _sim.Heroes[hIdx];
                    _spPreviewFrames[i].sprite = ForestComicUiSkin.SubSlotSelected;
                    _spPreviewBusts[i].gameObject.SetActive(true);
                    _spPreviewBusts[i].sprite = ForestComicUiSkin.GetArt(h.portraitKey, ForestComicUiSkin.GetArt("bust_dachui"));
                    _spPreviewLocks[i].gameObject.SetActive(false);
                    _spPreviewNames[i].text = $"{h.name} ★{h.stars}";
                    _spPreviewNames[i].color = ForestUiWidgets.InkDark;
                    _spPreviewRoles[i].text = $"{colTags[sl % 3]}·{_sim.GetHeroRoleName(hIdx)}";
                    _spPreviewRoles[i].color = ForestUiWidgets.InkMuted;
                }
                else if (i < maxDep)
                {
                    _spPreviewFrames[i].sprite = ForestComicUiSkin.ParchmentPanel;
                    _spPreviewBusts[i].gameObject.SetActive(false);
                    _spPreviewLocks[i].gameObject.SetActive(false);
                    _spPreviewNames[i].text = "＋ 空位待命";
                    _spPreviewNames[i].color = ForestUiWidgets.LushGreen;
                    _spPreviewRoles[i].text = "点击上阵";
                    _spPreviewRoles[i].color = ForestUiWidgets.InkMuted;
                }
                else
                {
                    _spPreviewFrames[i].sprite = ForestComicUiSkin.BtnGrey;
                    _spPreviewBusts[i].gameObject.SetActive(false);
                    _spPreviewLocks[i].gameObject.SetActive(true);
                    _spPreviewNames[i].text = $"Lv.{slotExpandLvs[i]}扩编";
                    _spPreviewNames[i].color = Color.white;
                    _spPreviewRoles[i].text = "提升营地";
                    _spPreviewRoles[i].color = new Color32(226, 220, 212, 255);
                }
            }

            List<SynergyAuraInfo> auras = _sim.GetSynergyAuras();
            var activeSynNames = new List<string>(4);
            if (auras != null)
            {
                for (int i = 0; i < auras.Count; i++)
                {
                    if (auras[i].active)
                        activeSynNames.Add($"【{auras[i].title}】");
                }
            }
            if (activeSynNames.Count > 0)
            {
                _spSynergyStripTxt.text = "已激活羁绊：" + string.Join("  ", activeSynNames);
                _spSynergyStripTxt.color = new Color32(158, 255, 174, 255);
            }
            else
            {
                _spSynergyStripTxt.text = "幸存者羁绊：上阵 ≥2 名骨干即可激活小队光环（点击上方卡片布阵）";
                _spSynergyStripTxt.color = ForestUiWidgets.WarmGold;
            }

            _spRewardIcons[0].sprite = ForestComicUiSkin.GetArt("ico_xp_star", ForestComicUiSkin.RedDotBadge);
            _spRewardTxts[0].text = $"经验 +{spec.rewardXp}";
            _spRewardIcons[1].sprite = ForestComicUiSkin.GetArt("ico_coin");
            _spRewardTxts[1].text = $"铜币 +{spec.rewardCoins}";
            _spRewardIcons[2].sprite = ForestComicUiSkin.ResIcon(SurvRes.HeroMedal);
            _spRewardTxts[2].text = $"勋章 +{spec.rewardMedals}";
            _spRewardIcons[3].sprite = ForestComicUiSkin.ResIcon(SurvRes.Blueprint);
            _spRewardTxts[3].text = $"手稿 +{spec.rewardBlueprints}";

            if (!string.IsNullOrEmpty(spec.milestoneTag))
            {
                _spMilestoneBg.sprite = ForestComicUiSkin.BtnOrange;
                _spMilestoneTxt.text = $"★ 里程碑大奖：{spec.milestoneTag}";
                _spMilestoneTxt.color = Color.white;
            }
            else
            {
                _spMilestoneBg.sprite = ForestComicUiSkin.DarkPill;
                _spMilestoneTxt.text = "提示：通关指定里程碑关卡可免费招募新骨干与解锁新阵型！";
                _spMilestoneTxt.color = ForestUiWidgets.WarmGold;
            }
        }

        public void OpenTacticalFormationModal()
        {
            CloseAllModals();
            ClearFloatingWorldToasts();
            if (_sim.GetDeployedHeroCount() <= 0)
                _sim.AutoDeployRecommendedFormation();
            _selectedFormationHeroIdx = -1;
            RefreshTacticalFormationModal();
            _tacticalFormationModal.SetActive(true);
        }

        static string GetHeroStageUnlockHint(int heroIdx)
        {
            switch (heroIdx)
            {
                case 2: return "通关第2关招募";
                case 3: return "通关第5关招募";
                case 4: return "通关第9关招募";
                case 5: return "通关第13关招募";
                default: return "初始入队";
            }
        }

        void RefreshTacticalFormationModal()
        {
            int depCnt = _sim.GetDeployedHeroCount();
            int maxDep = _sim.GetMaxDeployHeroCount();
            string expandHint = maxDep >= 5
                ? "已达最大 5 人满编"
                : (_sim.CampLevel < 4 ? "营地 Lv.4 扩编至 3 人"
                : (_sim.CampLevel < 8 ? "营地 Lv.8 扩编至 4 人" : "营地 Lv.12 扩编至 5 人"));
            _tfDeployCountTxt.text = $"上阵人数: {depCnt} / {maxDep}（{expandHint}）";
            _tfTotalPowerTxt.text = $"总战力 {_sim.CalcDeployedSquadPower()}";

            bool hasSelHero = _selectedFormationHeroIdx >= 0 && _selectedFormationHeroIdx < _sim.Heroes.Count;
            string selHeroName = hasSelHero ? _sim.Heroes[_selectedFormationHeroIdx].name : "";
            bool selAlreadyDeployed = hasSelHero && _sim.FindHeroSlot(_selectedFormationHeroIdx) >= 0;

            for (int slot = 0; slot < 9; slot++)
            {
                SlotStatBuff buff = _sim.GetFormationSlotBuff(_sim.SelectedFormation, slot);
                int hIdx = _sim.FormationSlots[slot];
                bool occupied = hIdx >= 0 && hIdx < _sim.Heroes.Count;
                bool isSelTile = occupied && hIdx == _selectedFormationHeroIdx;

                bool showBuff = !string.IsNullOrEmpty(buff.shortTag);
                _tfTileBuffGos[slot].SetActive(showBuff);
                if (showBuff)
                {
                    _tfTileBuffBgs[slot].sprite = buff.highlighted ? ForestComicUiSkin.BtnOrange : ForestComicUiSkin.DarkPill;
                    _tfTileBuffTxts[slot].text = buff.shortTag;
                }

                if (occupied)
                {
                    HeroSpec h = _sim.Heroes[hIdx];
                    _tfTileFrames[slot].sprite = isSelTile
                        ? ForestComicUiSkin.BtnOrange
                        : (buff.highlighted ? ForestComicUiSkin.SubSlotSelected : ForestComicUiSkin.ParchmentPanel);
                    _tfTileLaneHints[slot].gameObject.SetActive(false);
                    _tfTileEmptyTxts[slot].gameObject.SetActive(false);
                    _tfTilePortraitBoxes[slot].SetActive(true);
                    _tfTileBusts[slot].sprite = ForestComicUiSkin.GetArt(h.portraitKey, ForestComicUiSkin.GetArt("bust_dachui"));
                    _tfTileNames[slot].gameObject.SetActive(true);
                    _tfTileNames[slot].text = $"{h.name} ★{h.stars}";
                    _tfTileNames[slot].color = isSelTile ? Color.white : ForestUiWidgets.InkDark;
                    _tfTileRoles[slot].gameObject.SetActive(true);
                    _tfTileRoles[slot].text = $"{_sim.GetHeroRoleName(hIdx)}\n{_sim.CalcHeroCombatPower(hIdx, slot)}战";
                    _tfTileRoles[slot].color = isSelTile ? ForestUiWidgets.CreamLight : ForestUiWidgets.InkMuted;
                    _tfTileRemoveGos[slot].SetActive(true);
                }
                else
                {
                    _tfTileFrames[slot].sprite = buff.highlighted ? ForestComicUiSkin.SubSlotSelected : ForestComicUiSkin.SubSlot;
                    _tfTileLaneHints[slot].gameObject.SetActive(true);
                    _tfTilePortraitBoxes[slot].SetActive(false);
                    _tfTileNames[slot].gameObject.SetActive(false);
                    _tfTileRoles[slot].gameObject.SetActive(false);
                    _tfTileRemoveGos[slot].SetActive(false);
                    _tfTileEmptyTxts[slot].gameObject.SetActive(true);
                    if (hasSelHero)
                    {
                        _tfTileEmptyTxts[slot].text = selAlreadyDeployed ? $"点击换位\n【{selHeroName}】" : $"＋ 部署\n【{selHeroName}】";
                        _tfTileEmptyTxts[slot].color = ForestUiWidgets.LushGreen;
                    }
                    else
                    {
                        _tfTileEmptyTxts[slot].text = "＋ 点击部署";
                        _tfTileEmptyTxts[slot].color = ForestUiWidgets.InkMuted;
                    }
                }
            }

            // 刷新 4 大阵型按钮与摘要
            for (int f = 0; f < 4; f++)
            {
                TacticalFormationId fmt = (TacticalFormationId)f;
                bool unlocked = _sim.IsFormationUnlocked(fmt);
                bool selected = _sim.SelectedFormation == fmt;
                _tfFormationBtnImgs[f].sprite = selected
                    ? ForestComicUiSkin.BtnOrange
                    : (unlocked ? ForestComicUiSkin.BtnBlue : ForestComicUiSkin.BtnGrey);
                string fName = _sim.GetFormationName(fmt);
                _tfFormationBtnLbls[f].text = unlocked ? (selected ? $"★ {fName}" : fName) : $"[未开]{fName}";
            }
            _tfFormationSummaryTxt.text = $"当前阵型：{_sim.GetFormationName(_sim.SelectedFormation)} — {_sim.GetFormationSummary(_sim.SelectedFormation)}";

            // 刷新 4 条幸存者羁绊光环
            List<SynergyAuraInfo> auras = _sim.GetSynergyAuras();
            for (int s = 0; s < 4; s++)
            {
                bool has = auras != null && s < auras.Count;
                _tfSynergyBgs[s].gameObject.SetActive(has);
                if (!has) continue;
                SynergyAuraInfo a = auras[s];
                var titleOutline = _tfSynergyTitles[s].GetComponent<Outline>();
                var descOutline = _tfSynergyDescs[s].GetComponent<Outline>();
                if (titleOutline != null) titleOutline.enabled = !a.active;
                if (descOutline != null) descOutline.enabled = !a.active;
                if (a.active)
                {
                    _tfSynergyBgs[s].sprite = ForestComicUiSkin.SubSlotSelected;
                    _tfSynergyBgs[s].color = Color.white;
                    _tfSynergyTitles[s].text = $"✔ {a.title} [已激活]";
                    _tfSynergyTitles[s].color = ForestUiWidgets.LushGreen;
                    _tfSynergyDescs[s].text = a.desc;
                    _tfSynergyDescs[s].color = ForestUiWidgets.InkDark;
                }
                else
                {
                    _tfSynergyBgs[s].sprite = ForestComicUiSkin.DarkPill;
                    _tfSynergyBgs[s].color = new Color(1f, 1f, 1f, 0.80f);
                    _tfSynergyTitles[s].text = $"[未激活] {a.title}";
                    _tfSynergyTitles[s].color = new Color32(204, 198, 188, 255);
                    _tfSynergyDescs[s].text = a.desc;
                    _tfSynergyDescs[s].color = new Color32(168, 162, 154, 255);
                }
            }

            // 刷新底部 6 张骨干名册选人卡
            if (hasSelHero)
            {
                _tfRosterHintTxt.text = $"已选中【{selHeroName}】 — 点击上方九宫格任意格子即可部署或换位！";
                _tfRosterHintTxt.color = ForestUiWidgets.LushGreen;
            }
            else
            {
                _tfRosterHintTxt.text = "【点击骨干上阵/换位】点选骨干卡片或格子中角色，再点目标格调防";
                _tfRosterHintTxt.color = ForestUiWidgets.InkDark;
            }

            for (int i = 0; i < 6; i++)
            {
                bool exist = i < _sim.Heroes.Count;
                _tfRosterCards[i].SetActive(exist);
                if (!exist) continue;

                HeroSpec h = _sim.Heroes[i];
                int curSlot = _sim.FindHeroSlot(i);
                bool deployed = curSlot >= 0;
                bool selected = _selectedFormationHeroIdx == i;

                if (!h.unlocked)
                {
                    _tfRosterFrames[i].sprite = ForestComicUiSkin.BtnGrey;
                    _tfRosterTagBgs[i].sprite = ForestComicUiSkin.DarkPill;
                    _tfRosterTagTxts[i].text = "未解锁";
                    _tfRosterBusts[i].sprite = ForestComicUiSkin.GetArt(h.portraitKey, ForestComicUiSkin.GetArt("bust_driver"));
                    _tfRosterBusts[i].color = new Color(0.30f, 0.30f, 0.30f, 0.82f);
                    _tfRosterNames[i].text = h.name;
                    _tfRosterNames[i].color = Color.white;
                    _tfRosterRoles[i].text = _sim.GetHeroRoleName(i);
                    _tfRosterRoles[i].color = new Color32(218, 212, 204, 255);
                    _tfRosterPowers[i].text = GetHeroStageUnlockHint(i);
                    _tfRosterPowers[i].color = ForestUiWidgets.WarmGold;
                }
                else
                {
                    _tfRosterFrames[i].sprite = selected
                        ? ForestComicUiSkin.BtnOrange
                        : (deployed ? ForestComicUiSkin.SubSlotSelected : ForestComicUiSkin.ParchmentPanel);
                    _tfRosterTagBgs[i].sprite = selected
                        ? ForestComicUiSkin.DarkPill
                        : (deployed ? ForestComicUiSkin.BtnGreen : ForestComicUiSkin.BtnBlue);
                    _tfRosterTagTxts[i].text = selected ? "已选中" : (deployed ? "已上阵" : "待命");
                    _tfRosterBusts[i].sprite = ForestComicUiSkin.GetArt(h.portraitKey, ForestComicUiSkin.GetArt("bust_dachui"));
                    _tfRosterBusts[i].color = Color.white;
                    _tfRosterNames[i].text = $"{h.name} ★{h.stars}";
                    _tfRosterNames[i].color = selected ? Color.white : ForestUiWidgets.InkDark;
                    _tfRosterRoles[i].text = _sim.GetHeroRoleName(i);
                    _tfRosterRoles[i].color = selected ? ForestUiWidgets.CreamLight : ForestUiWidgets.InkMuted;
                    _tfRosterPowers[i].text = $"战力 {_sim.CalcHeroCombatPower(i, curSlot)}";
                    _tfRosterPowers[i].color = selected ? Color.white : ForestUiWidgets.LushGreen;
                }
            }
        }

        void OnTacticalGridTileClicked(int slotIdx)
        {
            if (slotIdx < 0 || slotIdx >= 9) return;

            if (_selectedFormationHeroIdx >= 0 && _selectedFormationHeroIdx < _sim.Heroes.Count)
            {
                if (_sim.FormationSlots[slotIdx] == _selectedFormationHeroIdx)
                {
                    _selectedFormationHeroIdx = -1;
                    RefreshTacticalFormationModal();
                    return;
                }
                if (_sim.TryPlaceHeroInFormation(_selectedFormationHeroIdx, slotIdx, out string msg))
                {
                    _selectedFormationHeroIdx = -1;
                    RefreshTacticalFormationModal();
                    if (!string.IsNullOrEmpty(msg)) SpawnScreenCenterBanner(msg);
                }
                else
                {
                    SpawnScreenCenterBanner(msg);
                }
                return;
            }

            int curHero = _sim.FormationSlots[slotIdx];
            if (curHero >= 0)
            {
                _selectedFormationHeroIdx = curHero;
                RefreshTacticalFormationModal();
                return;
            }

            if (_sim.GetDeployedHeroCount() < _sim.GetMaxDeployHeroCount())
            {
                int bestIdle = -1;
                int bestPwr = -1;
                for (int i = 0; i < _sim.Heroes.Count; i++)
                {
                    if (!_sim.Heroes[i].unlocked || _sim.FindHeroSlot(i) >= 0) continue;
                    int p = _sim.CalcHeroCombatPower(i, slotIdx);
                    if (p > bestPwr) { bestPwr = p; bestIdle = i; }
                }
                if (bestIdle >= 0 && _sim.TryPlaceHeroInFormation(bestIdle, slotIdx, out string placeMsg))
                {
                    RefreshTacticalFormationModal();
                    if (!string.IsNullOrEmpty(placeMsg)) SpawnScreenCenterBanner(placeMsg);
                    return;
                }
            }

            SpawnScreenCenterBanner("请先点击下方骨干卡片或已上阵角色，再点击此格换位部署！");
        }

        void OnTacticalTileRemoveClicked(int slotIdx)
        {
            if (_sim.RemoveHeroFromFormation(slotIdx, out string msg))
            {
                _selectedFormationHeroIdx = -1;
                RefreshTacticalFormationModal();
            }
            else if (!string.IsNullOrEmpty(msg))
            {
                SpawnScreenCenterBanner(msg);
            }
        }

        void OnFormationButtonClicked(TacticalFormationId fmt)
        {
            if (_sim.TrySelectFormation(fmt, out string msg))
            {
                RefreshTacticalFormationModal();
                if (!string.IsNullOrEmpty(msg)) SpawnScreenCenterBanner(msg);
            }
            else
            {
                string hint = _sim.GetFormationUnlockHint(fmt);
                SpawnScreenCenterBanner(!string.IsNullOrEmpty(hint) ? hint : msg);
                RefreshTacticalFormationModal();
            }
        }

        void OnRosterHeroCardClicked(int heroIdx)
        {
            if (heroIdx < 0 || heroIdx >= _sim.Heroes.Count) return;
            HeroSpec h = _sim.Heroes[heroIdx];
            if (!h.unlocked)
            {
                SpawnScreenCenterBanner($"【{h.name}】尚未入队：{GetHeroStageUnlockHint(heroIdx)}或提升营地星级解锁！");
                return;
            }
            _selectedFormationHeroIdx = (_selectedFormationHeroIdx == heroIdx) ? -1 : heroIdx;
            RefreshTacticalFormationModal();
        }

        public void StartWaveDefenseBattle()
        {
            LaunchArenaStageBattle(_sim != null ? _sim.CurrentBattleStage : 1);
        }

        public void LaunchArenaStageBattle(int stage1Based)
        {
            if (_world == null || _world.Arena == null) return;
            CloseAllModals();
            ClearFloatingWorldToasts();
            _panning = false;

            if (_sim.GetDeployedHeroCount() <= 0)
                _sim.AutoDeployRecommendedFormation();

            if (!_world.Arena.StartStageBattle(stage1Based, out string failReason))
            {
                SpawnScreenCenterBanner(string.IsNullOrEmpty(failReason) ? "当前无法开启战斗" : failReason);
                return;
            }

            _wasInArenaBattle = true;
            _wasArenaResultShown = false;
            if (_arenaResultModal != null) _arenaResultModal.SetActive(false);
            if (_mainHudLayer != null) _mainHudLayer.SetActive(false);
            if (_worldTrackLayer != null) _worldTrackLayer.SetActive(false);
            if (_waveCombatHud != null) _waveCombatHud.SetActive(true);
            RefreshWaveCombatHud();
        }

        void OnSkipBattleClicked()
        {
            if (_world == null || _world.Arena == null) return;
            if (!_world.Arena.CanSkipBattle)
            {
                int rem = Mathf.Max(1, Mathf.CeilToInt(5f - _world.Arena.ElapsedBattleSec));
                SpawnScreenCenterBanner($"开战满 5 秒后即可一键跳过战斗结算（还剩 {rem} 秒）");
                return;
            }
            _world.Arena.SkipToBattleResolution();
            SyncArenaHudAndResult(0f);
        }

        void RetreatArenaToCamp()
        {
            ExitArenaAndCloseResult();
        }

        void ExitArenaAndCloseResult()
        {
            _wasInArenaBattle = false;
            _wasArenaResultShown = false;
            if (_arenaResultModal != null) _arenaResultModal.SetActive(false);
            if (_waveCombatHud != null) _waveCombatHud.SetActive(false);
            if (_world != null && _world.Arena != null && _world.Arena.IsBattleActive)
                _world.Arena.ExitArenaToCamp();
            if (!_inFishingMode && (_cityArena == null || !_cityArena.IsRunning))
            {
                if (_mainHudLayer != null) _mainHudLayer.SetActive(true);
                if (_worldTrackLayer != null) _worldTrackLayer.SetActive(true);
            }
        }

        void OnResultNextStageClicked()
        {
            if (_world == null || _world.Arena == null) return;
            _wasArenaResultShown = false;
            if (_arenaResultModal != null) _arenaResultModal.SetActive(false);
            LaunchArenaStageBattle(_sim.CurrentBattleStage);
        }

        void SyncArenaHudAndResult(float dt)
        {
            _wasInArenaBattle = true;
            var arena = _world != null ? _world.Arena : null;
            if (arena == null || !arena.IsBattleActive) return;

            if (arena.IsResultPending)
            {
                if (_waveCombatHud != null && _waveCombatHud.activeSelf)
                    _waveCombatHud.SetActive(false);

                if (!_wasArenaResultShown)
                {
                    _wasArenaResultShown = true;
                    _autoChainCountdown = 5.0f;
                    RefreshArenaResultModalVisual();
                    _arenaResultModal.SetActive(true);
                }

                if (_arSunburst != null && _arSunburst.gameObject.activeSelf)
                    _arSunburst.rectTransform.Rotate(0f, 0f, 30f * dt);

                ArenaResultSummary res = arena.LastResult;
                if (res.victory && _sim.AutoChainNextStage && res.stage < MaxBattleStages)
                {
                    float step = Mathf.Min(0.1f, Mathf.Max(dt, Time.unscaledDeltaTime));
                    _autoChainCountdown -= step;
                    if (_autoChainCountdown <= 0f)
                    {
                        OnResultNextStageClicked();
                        return;
                    }
                    UpdateArenaNextStageBtnLabel(res);
                }
            }
            else
            {
                _wasArenaResultShown = false;
                if (_arenaResultModal != null && _arenaResultModal.activeSelf)
                    _arenaResultModal.SetActive(false);
                if (_waveCombatHud != null && !_waveCombatHud.activeSelf)
                    _waveCombatHud.SetActive(true);
                RefreshWaveCombatHud();
            }
        }

        void RefreshArenaResultModalVisual()
        {
            ClearFloatingWorldToasts();
            var arena = _world != null ? _world.Arena : null;
            if (arena == null) return;
            ArenaResultSummary res = arena.LastResult;

            _arSunburst.gameObject.SetActive(res.victory);
            _arVictoryRoot.SetActive(res.victory);
            _arDefeatRoot.SetActive(!res.victory);

            string stTitle = string.IsNullOrEmpty(res.stageTitle) ? $"第 {res.stage} 关" : res.stageTitle;
            if (res.victory)
            {
                _arRibbonImg.sprite = ForestComicUiSkin.BtnOrange;
                _arRibbonTxt.text = "挑战胜利!";
                _arStageSubTxt.text = $"{stTitle}  ·  战斗用时 {Mathf.Max(1, Mathf.RoundToInt(res.durationSec))} 秒";
                _arStageSubTxt.color = ForestUiWidgets.WarmGold;

                HeroSpec mvpHero = (res.mvpHeroIdx >= 0 && res.mvpHeroIdx < _sim.Heroes.Count)
                    ? _sim.Heroes[res.mvpHeroIdx]
                    : (_sim.Heroes.Count > 0 ? _sim.Heroes[0] : null);
                _arMvpBust.sprite = mvpHero != null
                    ? ForestComicUiSkin.GetArt(mvpHero.portraitKey, ForestComicUiSkin.GetArt("bust_dachui"))
                    : ForestComicUiSkin.GetArt("bust_dachui");
                string mvpRole = (res.mvpHeroIdx >= 0 && res.mvpHeroIdx < _sim.Heroes.Count)
                    ? $"（{_sim.GetHeroRoleName(res.mvpHeroIdx)}）"
                    : "";
                string mvpDisp = !string.IsNullOrEmpty(res.mvpName)
                    ? res.mvpName
                    : (mvpHero != null ? mvpHero.name : "骨干小队");
                _arMvpNameTxt.text = $"{mvpDisp}{mvpRole}";
                _arMvpDmgTxt.text = $"本场总贡献伤害/治疗: {Mathf.Max(1, res.mvpDamage)}";

                _arRewardSummaryTxt.text = !string.IsNullOrEmpty(res.rewardSummary)
                    ? res.rewardSummary
                    : "缴获通关经验、铜币、英雄勋章与工程手稿！";

                _arAutoChainBtnImg.sprite = _sim.AutoChainNextStage ? ForestComicUiSkin.BtnGreen : ForestComicUiSkin.BtnGrey;
                _arAutoChainLbl.text = _sim.AutoChainNextStage ? "[✓] 自动连续闯关：已开启" : "[ ] 自动连续闯关：未开启";
                UpdateArenaNextStageBtnLabel(res);
            }
            else
            {
                _arRibbonImg.sprite = ForestComicUiSkin.BtnRed;
                _arRibbonTxt.text = "防线失守";
                _arStageSubTxt.text = $"{stTitle}  ·  营地物资零损失撤回";
                _arStageSubTxt.color = new Color32(255, 186, 168, 255);
            }
        }

        void UpdateArenaNextStageBtnLabel(ArenaResultSummary res)
        {
            if (_arNextStageBtnLbl == null) return;
            if (res.stage >= MaxBattleStages && _sim.HordeWaveCleared >= MaxBattleStages)
            {
                _arNextStageBtnLbl.text = "再次挑战第40关";
            }
            else if (_sim.AutoChainNextStage && res.stage < MaxBattleStages)
            {
                int sec = Mathf.Clamp(Mathf.CeilToInt(_autoChainCountdown), 1, 5);
                _arNextStageBtnLbl.text = $"下一关 ({sec}s)";
            }
            else
            {
                _arNextStageBtnLbl.text = $"下一关 (第{_sim.CurrentBattleStage}关)";
            }
        }

        void RefreshWaveCombatHud()
        {
            var arena = _world != null ? _world.Arena : null;
            if (arena == null || !arena.IsBattleActive) return;

            int remSec = Mathf.Max(0, Mathf.CeilToInt(arena.StageTimerRemaining));
            _wcStageTimerTxt.text = $"第 {arena.ActiveStage} 关  ·  {remSec / 60:00}:{remSec % 60:00}";

            _wcTeamPowerTxt.text = $"我方战力 {arena.SquadPower}";
            float tMax = Mathf.Max(1f, arena.TeamTotalMaxHp);
            _wcTeamHpFill.fillAmount = Mathf.Clamp01(arena.TeamTotalHp / tMax);
            _wcTeamHpTxt.text = $"{Mathf.CeilToInt(arena.TeamTotalHp)} / {Mathf.CeilToInt(tMax)}";

            _wcEnemyTitleTxt.text = $"尸潮军团 (剩余 {arena.EnemiesAliveAndQueued}/{arena.EnemiesTotal})";
            float eMax = Mathf.Max(1f, arena.EnemyTotalMaxHp);
            _wcEnemyHpFill.fillAmount = Mathf.Clamp01(arena.EnemyTotalHp / eMax);
            _wcEnemyHpTxt.text = $"{Mathf.CeilToInt(arena.EnemyTotalHp)} / {Mathf.CeilToInt(eMax)}";

            if (arena.CanSkipBattle)
            {
                _wcSkipBtnImg.sprite = ForestComicUiSkin.BtnOrange;
                _wcSkipLbl.text = "跳过战斗 ▶";
            }
            else
            {
                int waitSec = Mathf.Clamp(Mathf.CeilToInt(5f - arena.ElapsedBattleSec), 1, 5);
                _wcSkipBtnImg.sprite = ForestComicUiSkin.BtnGrey;
                _wcSkipLbl.text = $"{waitSec}秒后可跳过";
            }

            _wcAutoBtnImg.sprite = arena.AutoCastUlt ? ForestComicUiSkin.BtnGreen : ForestComicUiSkin.BtnGrey;
            _wcAutoLbl.text = arena.AutoCastUlt ? "自动绝技:开" : "自动绝技:关";

            _wcSpeedBtnImg.sprite = arena.SpeedMultiplier >= 2 ? ForestComicUiSkin.BtnOrange : ForestComicUiSkin.BtnBlue;
            _wcSpeedLbl.text = $"倍速 {arena.SpeedMultiplier}x";

            IReadOnlyList<ArenaHeroHudState> states = arena.HeroStates;
            int cnt = states != null ? Mathf.Clamp(states.Count, 0, 5) : 0;
            float cardW = cnt >= 5 ? 128f : (cnt == 4 ? 152f : 166f);
            float gap = cnt >= 5 ? 8f : (cnt == 4 ? 12f : 14f);
            float totalW = cnt > 0 ? (cnt * cardW + (cnt - 1) * gap) : 0f;

            for (int i = 0; i < 5; i++)
            {
                bool show = i < cnt;
                _wcHeroCards[i].SetActive(show);
                if (!show)
                {
                    _wcBoundHeroIdxs[i] = -1;
                    continue;
                }

                ArenaHeroHudState st = states[i];
                _wcBoundHeroIdxs[i] = st.heroIdx;

                float cx = -totalW * 0.5f + cardW * 0.5f + i * (cardW + gap);
                _wcHeroCardRts[i].anchoredPosition = new Vector2(cx, 20f);
                _wcHeroCardRts[i].sizeDelta = new Vector2(cardW, 182f);

                HeroSpec h = (st.heroIdx >= 0 && st.heroIdx < _sim.Heroes.Count) ? _sim.Heroes[st.heroIdx] : null;
                _wcHeroBusts[i].sprite = h != null
                    ? ForestComicUiSkin.GetArt(h.portraitKey, ForestComicUiSkin.GetArt("bust_dachui"))
                    : ForestComicUiSkin.GetArt("bust_dachui");
                _wcHeroBusts[i].color = st.alive ? Color.white : new Color(0.32f, 0.32f, 0.32f, 0.78f);
                _wcHeroNames[i].text = $"{st.name} ★{Mathf.Max(1, st.star)}";

                float hMax = Mathf.Max(1f, st.maxHp);
                _wcHeroHpFills[i].fillAmount = st.alive ? Mathf.Clamp01(st.hp / hMax) : 0f;
                _wcHeroShieldFills[i].fillAmount = st.alive ? Mathf.Clamp01(st.shield / hMax) : 0f;
                _wcHeroHpTxts[i].text = st.alive
                    ? (st.shield > 1f ? $"{Mathf.CeilToInt(st.hp)}+{Mathf.CeilToInt(st.shield)}" : $"{Mathf.CeilToInt(st.hp)}")
                    : "阵亡";

                _wcHeroRageFills[i].fillAmount = st.alive ? Mathf.Clamp01(st.rage01) : 0f;
                _wcHeroRageTxts[i].text = st.alive ? $"怒气 {Mathf.FloorToInt(Mathf.Clamp01(st.rage01) * 100f)}%" : "怒气 0%";

                if (!st.alive)
                {
                    _wcHeroFrames[i].sprite = ForestComicUiSkin.BtnGrey;
                    _wcHeroCardRts[i].localScale = Vector3.one;
                    _wcHeroActionBgs[i].sprite = ForestComicUiSkin.DarkPill;
                    _wcHeroActionTxts[i].text = "已阵亡";
                    _wcHeroActionTxts[i].color = new Color32(204, 194, 194, 255);
                }
                else if (st.ultReady)
                {
                    _wcHeroFrames[i].sprite = ForestComicUiSkin.BtnOrange;
                    float pulse = 1f + Mathf.Sin(Time.unscaledTime * 7.5f) * 0.03f;
                    _wcHeroCardRts[i].localScale = new Vector3(pulse, pulse, 1f);
                    _wcHeroActionBgs[i].sprite = ForestComicUiSkin.BtnGreen;
                    _wcHeroActionTxts[i].text = "★ 释放绝技";
                    _wcHeroActionTxts[i].color = Color.white;
                }
                else
                {
                    _wcHeroFrames[i].sprite = ForestComicUiSkin.DialogOuter;
                    _wcHeroCardRts[i].localScale = Vector3.one;
                    _wcHeroActionBgs[i].sprite = ForestComicUiSkin.DarkPill;
                    _wcHeroActionTxts[i].text = string.IsNullOrEmpty(st.skillName) ? "战术绝技" : st.skillName;
                    _wcHeroActionTxts[i].color = ForestUiWidgets.WarmGold;
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
            pairs.Sort((a, b) =>
            {
                int pa = a.Key == SurvRes.HeroMedal ? 2000 : (a.Key == SurvRes.Blueprint ? 1000 : a.Value);
                int pb = b.Key == SurvRes.HeroMedal ? 2000 : (b.Key == SurvRes.Blueprint ? 1000 : b.Value);
                return pb.CompareTo(pa);
            });
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
                {
                    _questPulseTimer = 0.28f;
                    Vector2 rwPos = (_world != null && _world.IsGridCoveredByForest(q.focusGrid.x, q.focusGrid.y))
                        ? (_world.Walkers.Count > 0 ? new Vector2(_world.Walkers[0].gx, _world.Walkers[0].gy) : new Vector2(16.0f, 18.5f))
                        : q.focusGrid;
                    SpawnWorldRisingText(rw, rwPos);
                }
                return;
            }

            CampNode tn = !string.IsNullOrEmpty(q.targetNodeId) ? _sim.Find(q.targetNodeId) : null;
            if (tn != null)
            {
                // Case A: 目标建筑仍被未砍伐的树林遮盖 -> 绝不拉镜到密林，改为指引最近的可砍伐林缘块！
                if ((tn.state == NodeState.HiddenInForest && !tn.siteUncovered) || _world.IsGridCoveredByForest(tn.gx, tn.gy))
                {
                    GuideChopTowardNode(tn);
                    return;
                }

                // Case B: 树林已砍开，地面待收集整合发现物 -> 拉镜到发现物并自动派阿澈前往收集！
                if ((tn.requiresSiteSalvage && !tn.siteSalvageCleared) || tn.state == NodeState.HiddenInForest)
                {
                    ForestSurvivalWorld.GroundPile sp = _world.FindSalvagePileForNode(tn.id);
                    if (sp != null)
                    {
                        GuideCollectSalvageForNode(tn);
                        return;
                    }
                    if (!tn.requiresSiteSalvage || tn.siteSalvageCleared)
                    {
                        if (_sim.CampLevel >= tn.unlockPlayerLv)
                        {
                            tn.state = NodeState.Discovered;
                            _world.RefreshNodeVisual(tn);
                        }
                        else
                        {
                            SmoothFocusGrid(new Vector2(tn.gx, tn.gy));
                            SpawnWorldRisingText($"空地已清理！需领主星级达到 Lv.{tn.unlockPlayerLv} 解锁【{tn.ruinTitle}】", new Vector2(tn.gx, tn.gy));
                            return;
                        }
                    }
                    else
                    {
                        GuideCollectSalvageForNode(tn);
                        return;
                    }
                }

                // Case C: 空地已清理且显露地基/已建成 -> 正常建造、敲锤或打开作坊面板
                if (tn.state == NodeState.Discovered)
                {
                    SmoothFocusGrid(new Vector2(tn.gx, tn.gy));
                    if (tn.kind == NodeKind.RelicLoot)
                    {
                        if (_sim.StartBuildOrUpgrade(tn, out string lootMsg))
                            SpawnWorldRisingText(lootMsg, new Vector2(tn.gx, tn.gy));
                    }
                    else
                    {
                        OpenBuildConfirmCard(tn);
                    }
                    return;
                }

                if (tn.state == NodeState.Constructing)
                {
                    SmoothFocusGrid(new Vector2(tn.gx, tn.gy));
                    _sim.SpeedUpNodeBuild(tn);
                    return;
                }

                if (tn.state == NodeState.ReadyToHammer)
                {
                    SmoothFocusGrid(new Vector2(tn.gx, tn.gy));
                    _sim.CompleteHammerNode(tn);
                    return;
                }
            }

            switch (q.actionKind)
            {
                case ForestSurvivalSim.QuestActionKind.BuildOrUpgradeNode:
                    if (tn != null)
                    {
                        SmoothFocusGrid(new Vector2(tn.gx, tn.gy));
                        OpenBuildConfirmCard(tn);
                    }
                    else
                    {
                        OpenQuickBuildList(showWarehouse: false);
                    }
                    break;

                case ForestSurvivalSim.QuestActionKind.OpenWorkshop:
                {
                    CampNode ws = (tn != null && ForestSurvivalSim.IsWorkshopKind(tn.kind)) ? tn : _sim.Find("sawmill");
                    if (ws != null)
                    {
                        int recipeIdx = 0;
                        WorkshopRecipe[] recipes = _sim.GetWorkshopRecipes(ws);
                        if (!string.IsNullOrEmpty(q.text))
                        {
                            for (int i = 0; i < recipes.Length; i++)
                            {
                                if (q.text.Contains(recipes[i].name))
                                {
                                    recipeIdx = i;
                                    break;
                                }
                            }
                        }
                        OpenWorkshopDrawer(ws, recipeIdx);
                    }
                    break;
                }

                case ForestSurvivalSim.QuestActionKind.OpenFarmDrawer:
                {
                    CropSeedKind? seedHint = null;
                    if (!string.IsNullOrEmpty(q.text))
                    {
                        if (q.text.Contains("亚麻") || q.text.Contains("粗麻")) seedHint = CropSeedKind.Flax;
                        else if (q.text.Contains("草药")) seedHint = CropSeedKind.Herb;
                        else if (q.text.Contains("黄金") || q.text.Contains("金种")) seedHint = CropSeedKind.Golden;
                    }
                    OpenFarmDrawer(seedHint);
                    break;
                }

                case ForestSurvivalSim.QuestActionKind.OpenRanchDrawer:
                    OpenRanchDrawer();
                    break;

                case ForestSurvivalSim.QuestActionKind.OpenResidentOrders:
                    OpenResidentDemandDrawer();
                    break;

                case ForestSurvivalSim.QuestActionKind.OpenHeroGrowth:
                    OpenHeroModal(1);
                    break;

                case ForestSurvivalSim.QuestActionKind.OpenHeroDelegation:
                    OpenHeroModal(0);
                    break;

                case ForestSurvivalSim.QuestActionKind.OpenFishing:
                {
                    CampNode dock = tn ?? _sim.Find("river_dock");
                    if (dock != null && dock.state != NodeState.Active)
                        JumpToNodeById(dock.id);
                    else
                        EnterFishingMode();
                    break;
                }

                case ForestSurvivalSim.QuestActionKind.OpenCaravan:
                    OpenCaravanModal();
                    break;

                case ForestSurvivalSim.QuestActionKind.OpenCityRun:
                    OpenCityDistrictSelector();
                    break;

                case ForestSurvivalSim.QuestActionKind.FocusWorld:
                default:
                {
                    if (q.step == 0)
                    {
                        if (_world.HasPendingChop)
                        {
                            Vector2 pend = new Vector2((_world.PendingChopCx + 0.5f) * ForestMap.ChunkSize, (_world.PendingChopCy + 0.5f) * ForestMap.ChunkSize);
                            SmoothFocusGrid(pend);
                            SpawnWorldRisingText("请点击树冠上的【砍伐扩地】气泡确认砍树！", pend);
                            break;
                        }
                        if (TryResolveStep0WorldTarget(out Vector2 step0Target, out _))
                        {
                            SmoothFocusGrid(step0Target);
                            SpawnWorldRisingText("请点击白手套指向的【原木堆】或林缘树木收集原木！", step0Target);
                            break;
                        }
                    }

                    Vector2 focus = tn != null ? new Vector2(tn.gx, tn.gy) : q.focusGrid;
                    if (_world.IsGridCoveredByForest(focus.x, focus.y))
                    {
                        if (_world.TryFindBestChopChunkToward(focus.x, focus.y, out int fcx, out int fcy, out Vector2 safeChop))
                        {
                            _world.HasPendingChop = true;
                            _world.PendingChopCx = fcx;
                            _world.PendingChopCy = fcy;
                            _world.PendingChopTimer = 5.0f;
                            SmoothFocusGrid(safeChop);
                            SpawnWorldRisingText("点击这片林缘树木砍伐扩地，即可发现新区域！", safeChop);
                            break;
                        }
                        focus = _world.Walkers.Count > 0 ? new Vector2(_world.Walkers[0].gx, _world.Walkers[0].gy) : new Vector2(16.0f, 18.5f);
                    }
                    SmoothFocusGrid(focus);
                    if (tn != null && tn.storedAmount > 0 && _sim.TryCollectOrInteractNode(tn, out string colMsg) && !string.IsNullOrEmpty(colMsg))
                        SpawnWorldRisingText(colMsg, focus);
                    else
                        SpawnWorldRisingText("请点选视野内的树木、物资堆或目标设施推进主线！", focus);
                    break;
                }
            }
        }

        bool TryResolveStep0WorldTarget(out Vector2 outGrid, out float outLiftY)
        {
            Vector2 heroPos = _world.Walkers.Count > 0
                ? new Vector2(_world.Walkers[0].gx, _world.Walkers[0].gy)
                : new Vector2(15.8f, 18.4f);

            ForestSurvivalWorld.GroundPile bestPile = null;
            float bestPileDist = float.MaxValue;
            for (int i = 0; i < _world.GroundPiles.Count; i++)
            {
                var p = _world.GroundPiles[i];
                if (p.queued || !string.IsNullOrEmpty(p.salvageNodeId)) continue;
                if (_world.IsGridCoveredByForest(p.gx, p.gy)) continue;
                if (p.kind != ForestSurvivalWorld.FieldKind.LogPile &&
                    p.kind != ForestSurvivalWorld.FieldKind.RegrownTimber) continue;
                float d = Vector2.Distance(heroPos, new Vector2(p.gx, p.gy));
                if (d < bestPileDist)
                {
                    bestPileDist = d;
                    bestPile = p;
                }
            }
            if (bestPile != null)
            {
                outGrid = new Vector2(bestPile.gx, bestPile.gy);
                outLiftY = 0.22f;
                return true;
            }

            Vector2Int[] prefChunks =
            {
                new Vector2Int(4, 3),
                new Vector2Int(6, 7),
                new Vector2Int(7, 5),
                new Vector2Int(5, 7),
                new Vector2Int(3, 5),
            };
            for (int i = 0; i < prefChunks.Length; i++)
            {
                if (TryPickChunkForestPoint(prefChunks[i].x, prefChunks[i].y, heroPos, out outGrid))
                {
                    outLiftY = 0.35f;
                    return true;
                }
            }
            for (int cx = ForestSurvivalSim.MinPlayableChunk; cx <= ForestSurvivalSim.MaxPlayableChunk; cx++)
            {
                for (int cy = ForestSurvivalSim.MinPlayableChunk; cy <= ForestSurvivalSim.MaxPlayableChunk; cy++)
                {
                    if (TryPickChunkForestPoint(cx, cy, heroPos, out outGrid))
                    {
                        outLiftY = 0.35f;
                        return true;
                    }
                }
            }

            outGrid = new Vector2(15.8f, 17.4f);
            outLiftY = 0.30f;
            return false;
        }

        bool TryPickChunkForestPoint(int cx, int cy, Vector2 heroPos, out Vector2 outGrid)
        {
            outGrid = default;
            if (cx < ForestSurvivalSim.MinPlayableChunk || cx > ForestSurvivalSim.MaxPlayableChunk ||
                cy < ForestSurvivalSim.MinPlayableChunk || cy > ForestSurvivalSim.MaxPlayableChunk)
                return false;
            if (!_world.View.CanChopChunk(cx, cy)) return false;
            for (int q = 0; q < _world.ChopQueue.Count; q++)
                if (!_world.ChopQueue[q].isPile && _world.ChopQueue[q].chunkX == cx && _world.ChopQueue[q].chunkY == cy)
                    return false;

            int x0 = cx * ForestMap.ChunkSize;
            int y0 = cy * ForestMap.ChunkSize;
            float bestScore = float.MaxValue;
            bool found = false;
            for (int x = x0; x < x0 + ForestMap.ChunkSize; x++)
            {
                for (int y = y0; y < y0 + ForestMap.ChunkSize; y++)
                {
                    if (!_world.View.Map.InBounds(x, y) || _world.View.Map.IsCleared(x, y) || ForestMap.IsRiverCell(x, y))
                        continue;
                    bool adjCleared = _world.View.Map.IsCleared(x - 1, y) || _world.View.Map.IsCleared(x + 1, y) ||
                                      _world.View.Map.IsCleared(x, y - 1) || _world.View.Map.IsCleared(x, y + 1);
                    Vector2 cellCenter = new Vector2(x + 0.5f, y + 0.5f);
                    float score = Vector2.Distance(heroPos, cellCenter) + (adjCleared ? 0f : 8f);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        outGrid = cellCenter;
                        found = true;
                    }
                }
            }
            return found;
        }

        void OnBottomDockClicked(int tabIdx)
        {
            switch (tabIdx)
            {
                case 0: OpenStagePosterModal(); break;
                case 1: OpenQuickBuildList(showWarehouse: false); break;
                case 2:
                {
                    var q = _sim.GetCurrentQuest();
                    OpenHeroModal((!q.done && q.actionKind == ForestSurvivalSim.QuestActionKind.OpenHeroGrowth) ? 1 : 0);
                    break;
                }
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
            _qlSubCapTxt.rectTransform.sizeDelta = new Vector2(showWarehouse ? -204f : -84f, 28f);

            if (showWarehouse)
            {
                _qlTitleTxt.text = "营地物资与产成品总仓库";
                _qlSubCapTxt.text = $"储量上限:{_sim.StorageCap}  ·  口粮:{_sim.Get(SurvRes.Ration)}  ·  手稿:{_sim.Get(SurvRes.Blueprint)}  ·  勋章:{_sim.Get(SurvRes.HeroMedal)}";
                _qlContentRt.sizeDelta = new Vector2(0f, 680f);
                _qlContentRt.anchoredPosition = Vector2.zero;

                SurvRes[] shownItems =
                {
                    // 原材采集 (9)
                    SurvRes.Wood, SurvRes.Scrap, SurvRes.Water, SurvRes.Veggie,
                    SurvRes.Cloth, SurvRes.Herb, SurvRes.Fish, SurvRes.Meat,
                    SurvRes.Pelt,
                    // 建材五金 (6)
                    SurvRes.Plank, SurvRes.Nails, SurvRes.WoodBed,
                    SurvRes.IronBeam, SurvRes.IronIngot, SurvRes.MetalPipe,
                    // 裁缝防具 (4)
                    SurvRes.LinenShirt, SurvRes.LeatherBoots, SurvRes.FurCoat, SurvRes.PlateArmor,
                    // 炊事料理 (4)
                    SurvRes.RoastVeggie, SurvRes.VeggieSoup, SurvRes.GrillFish, SurvRes.SmokedMeat,
                    // 草药制剂 (3)
                    SurvRes.HerbPoultice, SurvRes.WarmTea, SurvRes.MedKit,
                    // 生态种肥与精密零件 (6) -> 合计 32 格 (4列 × 8行)
                    SurvRes.Parts, SurvRes.SeedVeggie, SurvRes.SeedFlax, SurvRes.SeedHerb, SurvRes.SeedGolden, SurvRes.Compost
                };

                for (int i = 0; i < shownItems.Length; i++)
                {
                    SurvRes r = shownItems[i];
                    int col = i % 4;
                    int row = i / 4;
                    float x = -216f + col * 144f;
                    float y = -8f - row * 83f;

                    Image cell = ForestUiWidgets.MakeSlicedImage("Stock_" + r, _qlContentRt, ForestComicUiSkin.SubSlot, Color.white,
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(138f, 77f), true);
                    _qlSpawnedRows.Add(cell.gameObject);
                    SurvRes capR = r;
                    cell.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenItemSourceTip(capR));

                    ForestUiWidgets.MakeIcon("Ico", cell.transform, ForestComicUiSkin.ResIcon(r), new Vector2(-45f, 0f), new Vector2(36f, 36f));
                    string rName = ForestSurvivalSim.ResName(r);
                    int nameSize = rName.Length >= 5 ? 13 : 14;
                    ForestUiWidgets.MakeText("Name", cell.transform, rName, nameSize, ForestUiWidgets.InkDark, TextAnchor.UpperLeft,
                        new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(44f, -6f), new Vector2(-48f, -4f), outline: false);

                    int amt = _sim.Get(r);
                    bool isCapped = ForestSurvivalSim.IsWarehouseCapped(r);
                    string cntStr = isCapped ? $"{amt}/{_sim.StorageCap}" : $"拥有: {amt}";
                    Color cntCol = isCapped
                        ? (amt >= _sim.StorageCap ? ForestUiWidgets.AlertRed : ForestUiWidgets.LushGreen)
                        : (amt > 0 ? ForestUiWidgets.LushGreen : ForestUiWidgets.InkMuted);
                    ForestUiWidgets.MakeText("Cnt", cell.transform, cntStr, 13, cntCol, TextAnchor.LowerLeft,
                        new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(44f, 6f), new Vector2(-48f, -4f), outline: false);
                }
            }
            else
            {
                _qlTitleTxt.text = "营地建筑解锁与升阶总览";
                _qlSubCapTxt.text = $"当前星级：★Lv.{_sim.CampLevel}/{ForestSurvivalSim.MaxCampLevel}   ·   人口：{_sim.CurrentPop}/{_sim.PopCap}   ·   铜币：{_sim.Coins}";

                var candidates = new List<CampNode>();
                for (int i = 0; i < _sim.Nodes.Count; i++)
                {
                    CampNode n = _sim.Nodes[i];
                    if (n.kind == NodeKind.RelicLoot || n.state == NodeState.Depleted) continue;
                    if (n.state == NodeState.Active && n.level >= n.maxLevel) continue;
                    candidates.Add(n);
                }

                // 按解锁/升阶所需玩家星级 (Lv.1 -> Lv.15) 升序排列；同星级内优先展示新解锁建筑，再展示升阶项
                candidates.Sort((a, b) =>
                {
                    int lvA = _sim.GetRequiredPlayerLvForNext(a);
                    int lvB = _sim.GetRequiredPlayerLvForNext(b);
                    if (lvA != lvB) return lvA.CompareTo(lvB);
                    bool newA = a.state != NodeState.Active;
                    bool newB = b.state != NodeState.Active;
                    if (newA != newB) return newA ? -1 : 1;
                    return _sim.Nodes.IndexOf(a).CompareTo(_sim.Nodes.IndexOf(b));
                });

                _qlContentRt.sizeDelta = new Vector2(0f, Mathf.Max(680f, 16f + candidates.Count * 82f));
                _qlContentRt.anchoredPosition = Vector2.zero;

                for (int i = 0; i < candidates.Count; i++)
                {
                    CampNode n = candidates[i];
                    CampNode capNode = n;
                    Image row = ForestUiWidgets.MakeSlicedImage("BuildRow_" + n.id, _qlContentRt, ForestComicUiSkin.SubSlot, Color.white,
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f - i * 82f), new Vector2(564f, 76f), false);
                    _qlSpawnedRows.Add(row.gameObject);
                    ForestUiWidgets.MakeIcon("Ico", row.transform, ForestComicUiSkin.NodePreviewSprite(n), new Vector2(-240f, 0f), new Vector2(54f, 54f));

                    int reqLv = _sim.GetRequiredPlayerLvForNext(n);
                    bool lvLocked = _sim.CampLevel < reqLv;
                    bool inBuild = n.state == NodeState.Constructing || n.state == NodeState.ReadyToHammer;
                    bool hiddenInForest = n.state == NodeState.HiddenInForest;
                    bool coveredByTrees = hiddenInForest && (!n.siteUncovered || _world.IsGridCoveredByForest(n.gx, n.gy));
                    bool waitingSalvage = hiddenInForest && !coveredByTrees && n.requiresSiteSalvage && !n.siteSalvageCleared;

                    string dispTitle = n.state == NodeState.Active ? n.activeTitle : (!string.IsNullOrEmpty(n.activeTitle) ? n.activeTitle : n.ruinTitle);
                    string stateTag = inBuild
                        ? "施工中"
                        : (coveredByTrees ? "密林隐藏"
                        : (waitingSalvage ? "待收集"
                        : (n.state == NodeState.Active ? $"Lv.{n.level}→{n.level + 1}" : "新建筑")));
                    ForestUiWidgets.MakeText("Name", row.transform, $"[★Lv.{reqLv}] {dispTitle} ({stateTag})", 17, ForestUiWidgets.InkDark, TextAnchor.UpperLeft,
                        new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(76f, -7f), new Vector2(-202f, -4f), outline: false);

                    string subLine;
                    Color subCol;
                    Sprite btnSkin;
                    string btnTxt;
                    Action onRowGo;

                    if (coveredByTrees)
                    {
                        string dir = string.IsNullOrEmpty(n.compassDir) ? "外围" : n.compassDir;
                        subLine = "藏于" + dir + "密林（砍树扩地后发现）";
                        subCol = ForestUiWidgets.InkMuted;
                        btnSkin = ForestComicUiSkin.BtnOrange;
                        btnTxt = "指引砍树";
                        onRowGo = () =>
                        {
                            CloseAllModals();
                            GuideChopTowardNode(capNode);
                        };
                    }
                    else if (waitingSalvage)
                    {
                        string sLbl = string.IsNullOrEmpty(n.salvageLabel) ? "林间散落物" : n.salvageLabel;
                        subLine = "已砍出空地！待清理【" + sLbl + "】";
                        subCol = ForestUiWidgets.LushGreen;
                        btnSkin = ForestComicUiSkin.BtnGreen;
                        btnTxt = "前往收集";
                        onRowGo = () =>
                        {
                            CloseAllModals();
                            GuideCollectSalvageForNode(capNode);
                        };
                    }
                    else
                    {
                        _sim.GetNextUpgradeSpec(n, out int cc, out _, out _, out CostPair[] costs);
                        bool canNow = !lvLocked && !inBuild && _sim.CanAffordUpgradeAll(n);
                        string costFmt = ForestSurvivalSim.FormatCosts(costs, cc);
                        subLine = inBuild
                            ? "施工进行中，点击前往立即加速或落成"
                            : (lvLocked ? $"★ 玩家星级 {reqLv} 级开放  ·  {costFmt}" : costFmt);
                        subCol = inBuild ? ForestUiWidgets.LushGreen : (lvLocked ? ForestUiWidgets.AlertRed : (canNow ? ForestUiWidgets.LushGreen : ForestUiWidgets.InkMuted));
                        btnSkin = lvLocked ? ForestComicUiSkin.BtnGrey : ((canNow || inBuild) ? ForestComicUiSkin.BtnGreen : ForestComicUiSkin.BtnBlue);
                        btnTxt = lvLocked ? $"{reqLv}级解锁" : (inBuild ? "去落成" : (canNow ? (n.state == NodeState.Active ? "可升级" : "可建造") : "前往"));
                        onRowGo = () => JumpToNodeById(capNode.id);
                    }

                    ForestUiWidgets.MakeText("Cost", row.transform, subLine, 13, subCol, TextAnchor.LowerLeft,
                        new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(76f, 7f), new Vector2(-202f, -4f), outline: false);

                    ForestUiWidgets.MakeJellyButton("Go", row.transform, btnSkin,
                        btnTxt, 16, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(108f, 46f),
                        () => onRowGo?.Invoke(), out _);
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
        void ClearFloatingWorldToasts()
        {
            for (int i = _worldToasts.Count - 1; i >= 0; i--)
            {
                if (_worldToasts[i].rt != null)
                    SafeDestroy(_worldToasts[i].rt.gameObject);
            }
            _worldToasts.Clear();

            for (int i = _pickupToasts.Count - 1; i >= 0; i--)
            {
                if (_pickupToasts[i].rt != null)
                    SafeDestroy(_pickupToasts[i].rt.gameObject);
            }
            _pickupToasts.Clear();
        }

        void EnqueueCenterPickupToast(SurvRes res, int amount)
        {
            if (amount <= 0) return;
            if (_world != null && _world.InWaveDefense) return;
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
                toCanvas = new Vector2(-63f, 569f - _topSafePad);
            else if (_resPillRts.TryGetValue(res, out RectTransform pillRt) && pillRt.gameObject.activeSelf)
                toCanvas = new Vector2(-360f + pillRt.anchoredPosition.x + 28f, 640f + pillRt.anchoredPosition.y - 17f);

            Image fi = ForestUiWidgets.MakeIcon("FlyRes", _canvasRt, ForestComicUiSkin.ResIcon(res), fromCanvas, new Vector2(36f, 36f));
            _flyIcons.Add(new FlyIconItem { rt = fi.rectTransform, img = fi, from = fromCanvas, to = toCanvas, t = 0f, dur = 0.45f, targetRes = res });
        }

        void SpawnXpFlyStar(int xp, Vector2 worldGrid)
        {
            if (_world != null && _world.InWaveDefense) return;
            Vector2 from = WorldToCanvas(ForestSliceView.Iso(worldGrid.x, worldGrid.y) + new Vector3(0f, 0.45f, 0f));
            Vector2 to = new Vector2(-246f, 609f - _topSafePad);
            Image star = ForestUiWidgets.MakeIcon("FlyXpStar", _canvasRt, ForestComicUiSkin.GetArt("ico_xp_star", ForestComicUiSkin.RedDotBadge), from, new Vector2(46f, 46f));
            ForestUiWidgets.MakeText("XpLbl", star.transform, $"+{xp}XP", 17, ForestUiWidgets.WarmGold, TextAnchor.LowerCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(90f, 24f));
            _flyIcons.Add(new FlyIconItem { rt = star.rectTransform, img = star, from = from, to = to, t = 0f, dur = 0.65f, targetRes = SurvRes.Wood });
        }

        void SpawnWorldRisingText(string msg, Vector2 worldGrid)
        {
            if (string.IsNullOrEmpty(msg)) return;
            if (_world != null && _world.InWaveDefense) return;
            Vector2 cp = WorldToCanvas(ForestSliceView.Iso(worldGrid.x, worldGrid.y) + new Vector3(0f, 0.55f, 0f));
            cp.x = Mathf.Clamp(cp.x, -220f, 220f);
            cp.y = Mathf.Clamp(cp.y, -380f, 440f);

            Image pill = ForestUiWidgets.MakeSlicedImage("WorldToast", _canvasRt, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), cp, new Vector2(460f, 40f), false);
            Text txt = ForestUiWidgets.MakeText("Txt", pill.transform, msg, 16, ForestUiWidgets.WarmGold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16f, 0f));
            _worldToasts.Add(new WorldTextToast { rt = pill.rectTransform, txt = txt, startPos = cp, age = 0f });
        }

        void SpawnScreenCenterBanner(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            Vector2 cp = new Vector2(0f, 180f);
            Image pill = ForestUiWidgets.MakeSlicedImage("CenterToast", _canvasRt, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), cp, new Vector2(480f, 42f), false);
            Text txt = ForestUiWidgets.MakeText("Txt", pill.transform, msg, 16, ForestUiWidgets.WarmGold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16f, 0f));
            _worldToasts.Add(new WorldTextToast { rt = pill.rectTransform, txt = txt, startPos = cp, age = 0f });
        }

        void SpawnFloatingDamageNumber(Vector3 worldPos, int dmg, bool isCrit)
        {
            if (_dmgPopups.Count > 28) return;
            Vector2 cp = WorldToCanvas(worldPos) + new Vector2(UnityEngine.Random.Range(-16f, 16f), UnityEngine.Random.Range(-6f, 10f));
            RectTransform rt = ForestUiWidgets.MakeRect("DmgNum", _canvasRt,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), cp, new Vector2(140f, 36f));
            Color col = isCrit ? new Color32(255, 208, 64, 255) : new Color32(255, 246, 232, 255);
            Text txt = ForestUiWidgets.MakeText("Val", rt, isCrit ? $"暴击 -{dmg}" : $"-{dmg}", isCrit ? 22 : 18, col, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            txt.raycastTarget = false;
            _dmgPopups.Add(new DamagePopupItem { rt = rt, txt = txt, startPos = cp, age = 0f });
        }

        // =====================================================================
        // 每帧刷新同步
        // =====================================================================
        public void Tick(float dt)
        {
            if (_bootLoadingActive)
            {
                TickBootLoading(dt);
                return;
            }

            bool inArena = _world != null && _world.InWaveDefense;
            if (_panning && !_inFishingMode && !inArena && (_cityArena == null || !_cityArena.IsRunning))
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

            if (inArena)
            {
                if (_mainHudLayer != null && _mainHudLayer.activeSelf) _mainHudLayer.SetActive(false);
                if (_worldTrackLayer != null && _worldTrackLayer.activeSelf) _worldTrackLayer.SetActive(false);
                _uiGlove?.Hide();
                _worldGlove?.Hide();
                SyncArenaHudAndResult(dt);
                TickToastsAndFlyers(dt);
                return;
            }
            else if (_wasInArenaBattle)
            {
                _wasInArenaBattle = false;
                _wasArenaResultShown = false;
                if (_waveCombatHud != null && _waveCombatHud.activeSelf) _waveCombatHud.SetActive(false);
                if (_arenaResultModal != null && _arenaResultModal.activeSelf) _arenaResultModal.SetActive(false);
                if (!_inFishingMode)
                {
                    if (_mainHudLayer != null && !_mainHudLayer.activeSelf) _mainHudLayer.SetActive(true);
                    if (_worldTrackLayer != null && !_worldTrackLayer.activeSelf) _worldTrackLayer.SetActive(true);
                }
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
            if (_farmDrawer != null && _farmDrawer.activeSelf) RefreshFarmDrawer();
            if (_ranchDrawer != null && _ranchDrawer.activeSelf) RefreshRanchDrawer();
            if (_residentDrawer.activeSelf) RefreshResidentDrawer();
            if (_caravanModal.activeSelf) RefreshCaravanModal();
            if (_crisisModal.activeSelf) RefreshCrisisPoster();
            if (_wavePrepModal != null && _wavePrepModal.activeSelf) RefreshStagePosterModal();
            if (_tacticalFormationModal != null && _tacticalFormationModal.activeSelf) RefreshTacticalFormationModal();
            if (_heroSpotlightModal.activeSelf && _heroSpotSunburst != null)
                _heroSpotSunburst.rectTransform.Rotate(0f, 0f, 30f * dt);

            UpdateTutorialGloves(dt);
            TickToastsAndFlyers(dt);
        }

        void RefreshMainHud(float dt)
        {
            _lvStarTxt.text = _sim.CampLevel.ToString();
            bool isMaxLv = _sim.CampLevel >= ForestSurvivalSim.MaxCampLevel;
            int xpNeed = Mathf.Max(1, ForestSurvivalSim.GetXpNeed(_sim.CampLevel));
            _xpFillImg.fillAmount = isMaxLv ? 1f : Mathf.Clamp01(_sim.CampXp / (float)xpNeed);
            _xpBarTxt.text = isMaxLv ? "MAX" : $"{_sim.CampXp}/{xpNeed}";
            _popTxt.text = $"{_sim.CurrentPop}/{_sim.PopCap}";
            _coinTxt.text = _sim.Coins.ToString();
            _blueprintTxt.text = _sim.Get(SurvRes.Blueprint).ToString();
            _storageCapTxt.text = $"仓{_sim.CurrentMaxSingleStock}/{_sim.StorageCap}";
            if (_moralePillTxt != null)
            {
                int mt = _sim.MoraleTier;
                _moralePillTxt.text = mt == 2 ? "士气沸腾+32%" : (mt == 1 ? "温饱士气+20%" : "普通士气+0%");
                _moralePillTxt.color = mt == 2 ? ForestUiWidgets.WarmGold : (mt == 1 ? ForestUiWidgets.LushGreen : ForestUiWidgets.CreamLight);
            }

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

                rt.anchoredPosition = new Vector2(14f, -136f - _topSafePad - slot * 36f);
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
                _hordeBadgeGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(14f, -142f - _topSafePad - slot * 36f);
                int sec = Mathf.CeilToInt(_sim.HordeTimerSec);
                _hordeBadgeTimerTxt.text = $"{sec / 60:00}:{sec % 60:00}";
            }

            var q = _sim.GetCurrentQuest();
            _questCardBg.sprite = q.done ? ForestComicUiSkin.QuestCardDone : ForestComicUiSkin.QuestCard;
            _questCheckImg.gameObject.SetActive(q.done);
            string qDisplay = q.done ? $"✔ {q.text} [领奖]" : q.text;
            _questTitleTxt.fontSize = qDisplay != null && qDisplay.Length > 21 ? 16 : 18;
            _questTitleTxt.text = qDisplay;
            _questFillImg.fillAmount = q.max > 0 ? Mathf.Clamp01(q.cur / (float)q.max) : 1f;
            if (_questPulseTimer > 0f)
            {
                _questPulseTimer = Mathf.Max(0f, _questPulseTimer - dt);
                float qs = 1f + Mathf.Sin((1f - _questPulseTimer / 0.28f) * Mathf.PI) * 0.10f;
                _questCardBg.rectTransform.localScale = new Vector3(qs, qs, 1f);
            }
            else
            {
                _questCardBg.rectTransform.localScale = Vector3.one;
            }

            _dockLocks[0].gameObject.SetActive(false); // 山口波次迎击全程开放，随时打勋章升星！
            _dockLocks[1].gameObject.SetActive(false);
            _dockLocks[2].gameObject.SetActive(false);
            _dockLocks[3].gameObject.SetActive(false);
            _dockLocks[4].gameObject.SetActive(!_sim.RadioRepaired);
            _dockLocks[5].gameObject.SetActive(!_sim.CanLaunchCityRun);

            bool anyAffordableBuild = false;
            for (int i = 0; i < _sim.Nodes.Count; i++)
            {
                CampNode n = _sim.Nodes[i];
                if (n.state == NodeState.HiddenInForest && n.siteUncovered && !n.siteSalvageCleared && !_world.IsGridCoveredByForest(n.gx, n.gy))
                { anyAffordableBuild = true; break; }
                if (n.state == NodeState.HiddenInForest || _world.IsGridCoveredByForest(n.gx, n.gy)) continue;
                if ((n.state == NodeState.Discovered || (n.state == NodeState.Active && n.level < n.maxLevel)) && _sim.CanAffordUpgradeAll(n))
                { anyAffordableBuild = true; break; }
            }
            bool anyHeroAction = false;
            for (int i = 0; i < _sim.Heroes.Count; i++)
            {
                HeroSpec h = _sim.Heroes[i];
                if (!h.unlocked) continue;
                if (!h.assigned) anyHeroAction = true;
                if (h.stars < 5 && _sim.Get(SurvRes.HeroMedal) >= ForestSurvivalSim.GetHeroStarUpMedals(h.stars))
                    anyHeroAction = true;
            }

            BattleStageSpec curStageSpec = _sim.GetBattleStageSpec(_sim.CurrentBattleStage);
            bool waveReady = _sim.CalcDeployedSquadPower() >= curStageSpec.recommendedPower || showHorde;
            _dockRedDots[0].SetActive(waveReady);
            _dockRedDots[1].SetActive(anyAffordableBuild);
            _dockRedDots[2].SetActive(anyHeroAction);
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
                // 铁律：未揭开或仍被树林覆盖的建筑点位严禁渲染任何悬浮气泡！
                if (n.state == NodeState.HiddenInForest || _world.IsGridCoveredByForest(n.gx, n.gy))
                    continue;

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
                else if (n.state == NodeState.Discovered && _sim.CampLevel >= n.unlockPlayerLv)
                {
                    CampNode cap = n;
                    float bob = Mathf.Sin(Time.time * 5.2f) * 3.5f;
                    TryBindBubble(ref used, ForestSliceView.Iso(n.gx, n.gy) + new Vector3(0f, 0.62f, 0f), new Vector2(0f, bob),
                        new Vector2(122f, 46f), true, ForestComicUiSkin.GetArt("ico_hammer"), "可建造", ForestUiWidgets.LushGreen, () => OpenBuildConfirmCard(cap), zoomScale);
                }
                else if (n.state == NodeState.Active)
                {
                    CampNode cap = n;
                    if (n.kind == NodeKind.Farm)
                    {
                        int ripe = _sim.CountRipeFarmPlots();
                        int empty = _sim.CountEmptyFarmPlots();
                        if (ripe > 0)
                        {
                            TryBindBubble(ref used, ForestSliceView.Iso(n.gx, n.gy) + new Vector3(0f, 0.42f, 0f), Vector2.zero,
                                new Vector2(118f, 46f), true, ForestComicUiSkin.ResIcon(SurvRes.Veggie), $"收割({ripe})", ForestUiWidgets.LushGreen,
                                () => _sim.HarvestAllRipeFarmPlots(out _), zoomScale);
                        }
                        else if (empty > 0)
                        {
                            TryBindBubble(ref used, ForestSliceView.Iso(n.gx, n.gy) + new Vector3(0f, 0.42f, 0f), Vector2.zero,
                                new Vector2(118f, 46f), true, ForestComicUiSkin.ResIcon(SurvRes.SeedVeggie), $"选种({empty})", ForestUiWidgets.InkDark,
                                () => OpenFarmDrawer(), zoomScale);
                        }
                    }
                    else if (n.kind == NodeKind.Ranch)
                    {
                        int rdy = _sim.CountReadyRanchPens();
                        int idl = _sim.CountIdleRanchPens();
                        if (rdy > 0)
                        {
                            TryBindBubble(ref used, ForestSliceView.Iso(n.gx, n.gy) + new Vector3(0f, 0.78f, 0f), Vector2.zero,
                                new Vector2(124f, 46f), true, ForestComicUiSkin.ResIcon(SurvRes.Pelt), $"收获({rdy})", ForestUiWidgets.LushGreen,
                                () => _sim.CollectAllReadyRanchPens(out _), zoomScale);
                        }
                        else if (idl > 0)
                        {
                            TryBindBubble(ref used, ForestSliceView.Iso(n.gx, n.gy) + new Vector3(0f, 0.78f, 0f), Vector2.zero,
                                new Vector2(118f, 46f), true, ForestComicUiSkin.ResIcon(SurvRes.Veggie), $"投喂({idl})", ForestUiWidgets.InkDark,
                                OpenRanchDrawer, zoomScale);
                        }
                    }
                    else if (n.storedAmount > 0)
                    {
                        TryBindBubble(ref used, ForestSliceView.Iso(n.gx, n.gy) + new Vector3(0f, 0.78f, 0f), Vector2.zero,
                            new Vector2(108f, 46f), true, ForestComicUiSkin.ResIcon(n.storedRes), $"+{n.storedAmount}", ForestUiWidgets.InkDark, () => HandleNodeClicked(cap), zoomScale);
                    }
                }
            }

            // 新砍开空地的【发现物/散落物资】高优醒目气泡：一触即派阿澈收集整合，完成后显露建筑地基！
            float salvageBob = Mathf.Sin(Time.time * 5.8f) * 4.5f;
            for (int i = 0; i < _world.GroundPiles.Count && used < _worldBubbles.Count; i++)
            {
                var p = _world.GroundPiles[i];
                if (p.queued || string.IsNullOrEmpty(p.salvageNodeId)) continue;
                var capPile = p;
                CampNode sn = _sim.Find(p.salvageNodeId);
                Sprite sIco = sn != null
                    ? ForestComicUiSkin.ResIcon(sn.salvageBonusRes)
                    : ForestComicUiSkin.GetArt("ico_blueprint", ForestComicUiSkin.ResIcon(SurvRes.Blueprint));
                string sLbl = !string.IsNullOrEmpty(p.customBubbleLabel)
                    ? p.customBubbleLabel
                    : (sn != null && !string.IsNullOrEmpty(sn.salvageLabel) ? $"收集:{sn.salvageLabel}" : $"收集:{p.salvageNodeId}");
                float bWidth = Mathf.Clamp(64f + sLbl.Length * 18f, 152f, 240f);
                TryBindBubble(ref used, ForestSliceView.Iso(p.gx, p.gy) + new Vector3(0f, 0.44f, 0f), new Vector2(0f, salvageBob),
                    new Vector2(bWidth, 50f), true, sIco, sLbl, ForestUiWidgets.LushGreen,
                    () => _world.QueueGroundPile(capPile), zoomScale);
            }

            // 居民生计订单气泡：精准挂载在营地真实【避难居民】头顶（英雄专注干活巡逻，居民负责生活与提单！）
            if (_sim.ActiveDemand != null && used < _worldBubbles.Count)
            {
                if (_world.CivilianVillagers.Count > 0)
                {
                    int vi = Mathf.Clamp(_sim.ActiveDemand.walkerIndex, 0, _world.CivilianVillagers.Count - 1);
                    var v = _world.CivilianVillagers[vi];
                    if (!_world.IsGridCoveredByForest(v.gx, v.gy))
                    {
                        TryBindBubble(ref used, ForestSliceView.Iso(v.gx, v.gy) + new Vector3(0f, 0.66f, 0f), Vector2.zero,
                            new Vector2(96f, 50f), true, ForestComicUiSkin.ResIcon(_sim.ActiveDemand.res1),
                            $"{Mathf.Min(_sim.Get(_sim.ActiveDemand.res1), _sim.ActiveDemand.amt1)}/{_sim.ActiveDemand.amt1}",
                            ForestUiWidgets.InkDark, OpenResidentDemandDrawer, zoomScale);
                    }
                }
                else if (_world.Walkers.Count > 0)
                {
                    int wi = Mathf.Clamp(_sim.ActiveDemand.walkerIndex, 0, _world.Walkers.Count - 1);
                    var w = _world.Walkers[wi];
                    if (!_world.IsGridCoveredByForest(w.gx, w.gy))
                    {
                        TryBindBubble(ref used, ForestSliceView.Iso(w.gx, w.gy) + new Vector3(0f, 0.68f, 0f), Vector2.zero,
                            new Vector2(92f, 50f), true, ForestComicUiSkin.ResIcon(_sim.ActiveDemand.res1),
                            $"{Mathf.Min(_sim.Get(_sim.ActiveDemand.res1), _sim.ActiveDemand.amt1)}/{_sim.ActiveDemand.amt1}",
                            ForestUiWidgets.InkDark, OpenResidentDemandDrawer, zoomScale);
                    }
                }
            }

            // 前期新手引导：未加入队列且不在密林内的原木堆/再生木头顶显示清晰可点的【捡原木】气泡
            bool showWoodPileGuideBubble = (_sim.QuestIndex == 0 || _sim.CampLevel <= 2) && _cam.orthographicSize < 6.8f;
            if (showWoodPileGuideBubble)
            {
                float woodBob = Mathf.Sin(Time.time * 5.5f) * 3.5f;
                for (int i = 0; i < _world.GroundPiles.Count && used < _worldBubbles.Count; i++)
                {
                    var p = _world.GroundPiles[i];
                    if (p.queued || !string.IsNullOrEmpty(p.salvageNodeId)) continue;
                    if (_world.IsGridCoveredByForest(p.gx, p.gy)) continue;
                    if (p.kind != ForestSurvivalWorld.FieldKind.LogPile &&
                        p.kind != ForestSurvivalWorld.FieldKind.RegrownTimber) continue;
                    var capPile = p;
                    TryBindBubble(ref used, ForestSliceView.Iso(p.gx, p.gy) + new Vector3(0f, 0.34f, 0f), new Vector2(0f, woodBob),
                        new Vector2(122f, 46f), true, ForestComicUiSkin.ResIcon(SurvRes.Wood), "捡原木", ForestUiWidgets.InkDark,
                        () => _world.QueueGroundPile(capPile), zoomScale);
                }
            }

            // 普通居民日常烤火感谢闲聊气泡（自动避让头顶挂有订单气泡的居民，确保零重叠！）
            Vector2 ordVilPos = new Vector2(-999f, -999f);
            if (_sim.ActiveDemand != null && _world.CivilianVillagers.Count > 0)
            {
                int ovi = Mathf.Clamp(_sim.ActiveDemand.walkerIndex, 0, _world.CivilianVillagers.Count - 1);
                ordVilPos = new Vector2(_world.CivilianVillagers[ovi].gx, _world.CivilianVillagers[ovi].gy);
            }
            for (int i = 0; i < _world.CivilianVillagers.Count && used < _worldBubbles.Count; i++)
            {
                var v = _world.CivilianVillagers[i];
                if (_world.IsGridCoveredByForest(v.gx, v.gy)) continue;
                if (Vector2.Distance(new Vector2(v.gx, v.gy), ordVilPos) < 1.8f) continue;
                if (_cam.orthographicSize < 7.2f && v.chatterTimer > 0f && !string.IsNullOrEmpty(v.chatterText))
                {
                    TryBindBubble(ref used, ForestSliceView.Iso(v.gx, v.gy) + new Vector3(0f, 0.66f, 0f), Vector2.zero,
                        new Vector2(216f, 48f), true, null, v.chatterText, ForestUiWidgets.InkDark, null, zoomScale);
                }
            }

            for (int i = 0; i < _world.Walkers.Count && used < _worldBubbles.Count; i++)
            {
                var w = _world.Walkers[i];
                if (_world.IsGridCoveredByForest(w.gx, w.gy)) continue;
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
                    if (p.queued || !string.IsNullOrEmpty(p.salvageNodeId) || p.kind == ForestSurvivalWorld.FieldKind.LogPile) continue;
                    if (_world.IsGridCoveredByForest(p.gx, p.gy)) continue;
                    if (showWoodPileGuideBubble && p.kind == ForestSurvivalWorld.FieldKind.RegrownTimber) continue;
                    var capPile = p;
                    Sprite ico = p.kind == ForestSurvivalWorld.FieldKind.BerryBush ? ForestComicUiSkin.ResIcon(SurvRes.Veggie) :
                                 p.kind == ForestSurvivalWorld.FieldKind.ScrapWreck ? ForestComicUiSkin.ResIcon(SurvRes.Scrap) :
                                 ForestComicUiSkin.ResIcon(SurvRes.Wood);
                    TryBindBubble(ref used, ForestSliceView.Iso(p.gx, p.gy) + new Vector3(0f, 0.42f, 0f), Vector2.zero,
                        new Vector2(36f, 36f), false, ico, "", Color.white, () => _world.QueueGroundPile(capPile), zoomScale);
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
                tb.icon.rectTransform.anchoredPosition = new Vector2(-size.x * 0.5f + 24f, 5f);
                tb.icon.rectTransform.sizeDelta = new Vector2(30f, 30f);
                tb.label.text = text;
                tb.label.color = txtCol;
                tb.label.rectTransform.offsetMin = new Vector2(42f, 12f);
                tb.label.rectTransform.offsetMax = new Vector2(-10f, -4f);
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
            int popBonusPct = Mathf.RoundToInt((_sim.OrderProsperityMultiplier - 1f) * 100f);
            string prosTag = popBonusPct > 0 ? $" · 繁荣溢价+{popBonusPct}%" : "";
            _resMoodTxt.text = $"满{ForestSurvivalSim.SatisfiedGoal}单赠【铜币+260·手稿x2】 {_sim.SatisfiedToday}/{ForestSurvivalSim.SatisfiedGoal}{prosTag}";
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
            if (lp.y < -520f && (lp.x < -220f || lp.x > 160f)) return true;
            // 右下【氮气冲撞】圆钮区域防摇杆抢夺
            if (Vector2.Distance(lp, new Vector2(246f, -380f)) < 76f) return true;
            return false;
        }

        void RefreshCityDriveHud()
        {
            _cdCargoFill.fillAmount = _cityArena.MaxCargo > 0 ? _cityArena.CurrentCargo / (float)_cityArena.MaxCargo : 0f;
            _cdCargoTxt.text = $"车斗载重  {_cityArena.CurrentCargo} / {_cityArena.MaxCargo}";

            float hpMax = Mathf.Max(1f, _cityArena.MaxTruckHp);
            float hpRatio = Mathf.Clamp01(_cityArena.TruckHp / hpMax);
            _cdArmorFill.fillAmount = hpRatio;
            _cdArmorFill.sprite = hpRatio > 0.35f ? ForestComicUiSkin.BtnGreen : ForestComicUiSkin.BtnRed;
            _cdArmorTxt.text = _cityArena.NitroActiveTimer > 0f
                ? $"★ 氮气无敌冲撞中  {Mathf.CeilToInt(_cityArena.TruckHp)}/{Mathf.CeilToInt(hpMax)}"
                : $"皮卡装甲  {Mathf.CeilToInt(_cityArena.TruckHp)} / {Mathf.CeilToInt(hpMax)}";

            if (_cdBossBox != null)
            {
                bool showBoss = _cityArena.IsBossAlive;
                _cdBossBox.SetActive(showBoss);
                if (showBoss) _cdBossFill.fillAmount = _cityArena.BossHpRatio;
            }

            if (_cdNitroLbl != null)
            {
                if (_cityArena.NitroActiveTimer > 0.05f)
                {
                    _cdNitroLbl.text = "冲撞中!";
                    _cdNitroRing.color = new Color(1f, 0.85f, 0.42f, 1f);
                }
                else if (_cityArena.NitroCooldown > 0.05f)
                {
                    _cdNitroLbl.text = $"冷却 {Mathf.CeilToInt(_cityArena.NitroCooldown)}s";
                    _cdNitroRing.color = new Color(0.6f, 0.6f, 0.6f, 0.85f);
                }
                else
                {
                    _cdNitroLbl.text = "氮气冲撞";
                    _cdNitroRing.color = Color.white;
                }
            }

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
                _cdLootSummaryTxt.text = "随车小队自动扫射尸群！点右下【氮气冲撞】碾碎路障夺空投！";
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

        Vector2 GetUiGloveTarget(RectTransform targetRt, Vector2 fallback9x16, Vector2 extraOffset = default)
        {
            if (targetRt != null && _canvasRt != null && _canvasRt.rect.height > 200f)
            {
                Vector3 wc = targetRt.TransformPoint(targetRt.rect.center);
                Vector2 lp = (Vector2)_canvasRt.InverseTransformPoint(wc) + extraOffset;
                float minY = -_canvasRt.rect.height * 0.5f + 56f;
                if (lp.y < minY) lp.y = minY;
                return lp;
            }
            return fallback9x16 + extraOffset;
        }

        void UpdateTutorialGloves(float dt)
        {
            if (ForestGmConsole.IsGmPanelOpen ||
                (_loadingOverlay != null && _loadingOverlay.activeSelf) ||
                _comicDialogOverlay.activeSelf || _levelUpModal.activeSelf || _heroSpotlightModal.activeSelf ||
                _crisisModal.activeSelf || _itemTipPopup.activeSelf || _sourceRouteModal.activeSelf ||
                (_wavePrepModal != null && _wavePrepModal.activeSelf) ||
                (_tacticalFormationModal != null && _tacticalFormationModal.activeSelf) ||
                (_arenaResultModal != null && _arenaResultModal.activeSelf) ||
                (_quickListModal != null && _quickListModal.activeSelf) ||
                (_caravanModal != null && _caravanModal.activeSelf) ||
                (_citySelectModal != null && _citySelectModal.activeSelf) ||
                (_lootSettleModal != null && _lootSettleModal.activeSelf))
            {
                _uiGlove.Hide();
                _worldGlove.Hide();
                return;
            }

            if (_buildCardModal.activeSelf)
            {
                _worldGlove.Hide();
                if (_bcConfirmBtn != null && _bcConfirmBtn.gameObject.activeSelf && _sim.CampLevel <= 5)
                    _uiGlove.ShowAt(GetUiGloveTarget(_bcConfirmBtn.GetComponent<RectTransform>(), new Vector2(20f, -310f)), dt);
                else
                    _uiGlove.Hide();
                return;
            }

            if (_farmDrawer != null && _farmDrawer.activeSelf)
            {
                _worldGlove.Hide();
                int plotCap = _sim.GetFarmPlotCap();
                bool allDryTilled = plotCap > 0 && _sim.CountEmptyFarmPlots() >= plotCap;
                if (_sim.CampLevel <= 2 && _fmMainBtn != null && (allDryTilled || _sim.CountRipeFarmPlots() > 0))
                    _uiGlove.ShowAt(GetUiGloveTarget(_fmMainBtn.GetComponent<RectTransform>(), new Vector2(108f, -584f)), dt);
                else
                    _uiGlove.Hide();
                return;
            }

            if (_ranchDrawer != null && _ranchDrawer.activeSelf)
            {
                _worldGlove.Hide();
                int penCap = _sim.GetRanchPenCap();
                bool allIdle = penCap > 0 && _sim.CountIdleRanchPens() >= penCap && _sim.Get(SurvRes.Veggie) >= 2 && _sim.Get(SurvRes.Water) >= 1;
                if (_sim.CampLevel <= 3 && _rnMainBtn != null && (allIdle || _sim.CountReadyRanchPens() > 0))
                    _uiGlove.ShowAt(GetUiGloveTarget(_rnMainBtn.GetComponent<RectTransform>(), new Vector2(108f, -584f)), dt);
                else
                    _uiGlove.Hide();
                return;
            }

            if (_workshopDrawer.activeSelf && _activeWorkshop != null)
            {
                _worldGlove.Hide();
                if (_sim.CampLevel <= 5)
                {
                    if (_activeWorkshop.queue.Count > 0 && _sim.CampLevel <= 2)
                        _uiGlove.ShowAt(new Vector2(-236f, -120f), dt);
                    else if (_wsProduceBtn != null)
                        _uiGlove.ShowAt(GetUiGloveTarget(_wsProduceBtn.GetComponent<RectTransform>(), new Vector2(20f, -565f)), dt);
                    else
                        _uiGlove.Hide();
                }
                else
                {
                    _uiGlove.Hide();
                }
                return;
            }

            if (_residentDrawer.activeSelf)
            {
                _worldGlove.Hide();
                if (_sim.CampLevel <= 5)
                {
                    int bestSlot = 0;
                    for (int i = 0; i < _sim.ActiveOrders.Count && i < 3; i++)
                    {
                        if (_sim.CanSatisfyOrder(i)) { bestSlot = i; break; }
                    }
                    if (_ordDeliverBtns[bestSlot] != null)
                        _uiGlove.ShowAt(GetUiGloveTarget(_ordDeliverBtns[bestSlot].GetComponent<RectTransform>(), new Vector2(250f, 30f)), dt);
                    else
                        _uiGlove.ShowAt(new Vector2(250f, 30f), dt);
                }
                else
                {
                    _uiGlove.Hide();
                }
                return;
            }

            if (_heroAssignModal != null && _heroAssignModal.activeSelf)
            {
                _worldGlove.Hide();
                var qHero = _sim.GetCurrentQuest();
                if (_sim.CampLevel <= 5 && !qHero.done && qHero.actionKind == ForestSurvivalSim.QuestActionKind.OpenHeroGrowth)
                {
                    if (_heroActiveTab != 1 && _haTabBtn1 != null)
                    {
                        _uiGlove.ShowAt(GetUiGloveTarget(_haTabBtn1.GetComponent<RectTransform>(), new Vector2(118f, 398f)), dt);
                        return;
                    }
                    int bestHeroIdx = 0;
                    for (int i = 0; i < _sim.Heroes.Count && i < _haLvUpBtns.Length; i++)
                    {
                        HeroSpec h = _sim.Heroes[i];
                        if (!h.unlocked) continue;
                        bestHeroIdx = i;
                        if (_sim.Coins >= ForestSurvivalSim.GetHeroLevelUpCoins(h.level) &&
                            _sim.Get(SurvRes.Ration) >= ForestSurvivalSim.GetHeroLevelUpRations(h.level))
                            break;
                    }
                    if (_haLvUpBtns[bestHeroIdx] != null)
                    {
                        _uiGlove.ShowAt(GetUiGloveTarget(_haLvUpBtns[bestHeroIdx].GetComponent<RectTransform>(), new Vector2(160f, 240f)), dt);
                        return;
                    }
                }
                _uiGlove.Hide();
                return;
            }

            var q = _sim.GetCurrentQuest();
            if (!IsModalOpen && q.done)
            {
                _worldGlove.Hide();
                Vector2 qClaimPos = GetUiGloveTarget(_questCardBg != null ? _questCardBg.rectTransform : null, new Vector2(-130f, -482f), new Vector2(34f, 0f));
                _uiGlove.ShowAt(qClaimPos, dt);
                return;
            }

            if (!IsModalOpen)
            {
                if (_world.HasPendingChop)
                {
                    _uiGlove.Hide();
                    float cgx = (_world.PendingChopCx + 0.5f) * ForestMap.ChunkSize;
                    float cgy = (_world.PendingChopCy + 0.5f) * ForestMap.ChunkSize;
                    Vector2 cp = WorldToCanvas(ForestSliceView.Iso(cgx, cgy) + new Vector3(0f, 0.55f, 0f));
                    cp.x = Mathf.Clamp(cp.x, -260f, 260f);
                    cp.y = Mathf.Clamp(cp.y, -420f, 430f) - 8f;
                    _worldGlove.ShowAt(cp, dt);
                    return;
                }

                for (int i = 0; i < _sim.Nodes.Count; i++)
                {
                    CampNode hn = _sim.Nodes[i];
                    if (hn.state == NodeState.ReadyToHammer && !_world.IsGridCoveredByForest(hn.gx, hn.gy))
                    {
                        Vector2 hp = WorldToCanvas(ForestSliceView.Iso(hn.gx, hn.gy) + new Vector3(0f, 0.9f, 0f));
                        if (Mathf.Abs(hp.x) < 340f && hp.y > -470f && hp.y < 540f)
                        {
                            _uiGlove.Hide();
                            _worldGlove.ShowAt(hp, dt);
                            return;
                        }
                    }
                }

                CampNode tn = !string.IsNullOrEmpty(q.targetNodeId) ? _sim.Find(q.targetNodeId) : null;
                if (tn != null)
                {
                    // Case A: 尚被密林遮挡 -> 白手套指向最近可砍伐的林缘树木块（阿澈砍树奔跑中自动隐藏）
                    if ((tn.state == NodeState.HiddenInForest && !tn.siteUncovered) || _world.IsGridCoveredByForest(tn.gx, tn.gy))
                    {
                        if (_world.ChopQueue.Count > 0)
                        {
                            _uiGlove.Hide();
                            _worldGlove.Hide();
                            return;
                        }
                        if (_world.TryFindBestChopChunkToward(tn.gx, tn.gy, out _, out _, out Vector2 chopFocus))
                        {
                            Vector2 cp = WorldToCanvas(ForestSliceView.Iso(chopFocus.x, chopFocus.y) + new Vector3(0f, 0.42f, 0f));
                            if (Mathf.Abs(cp.x) < 340f && cp.y > -470f && cp.y < 525f)
                            {
                                _uiGlove.Hide();
                                _worldGlove.ShowAt(cp, dt);
                                return;
                            }
                        }
                        _worldGlove.Hide();
                        _uiGlove.ShowAt(GetUiGloveTarget(_questCardBg != null ? _questCardBg.rectTransform : null, new Vector2(-130f, -482f)), dt);
                        return;
                    }

                    // Case B: 树林已砍开、地面待收集整合发现物 -> 白手套指向地面发现物（已在采集队列中则隐藏）
                    if ((tn.requiresSiteSalvage && !tn.siteSalvageCleared) || tn.state == NodeState.HiddenInForest)
                    {
                        ForestSurvivalWorld.GroundPile sp = _world.FindSalvagePileForNode(tn.id);
                        if (sp != null)
                        {
                            bool pileQueued = sp.queued;
                            if (!pileQueued)
                            {
                                for (int k = 0; k < _world.ChopQueue.Count; k++)
                                {
                                    if (_world.ChopQueue[k].isPile && _world.ChopQueue[k].pile == sp)
                                    {
                                        pileQueued = true;
                                        break;
                                    }
                                }
                            }
                            if (pileQueued)
                            {
                                _uiGlove.Hide();
                                _worldGlove.Hide();
                                return;
                            }
                            Vector2 spCanvas = WorldToCanvas(ForestSliceView.Iso(sp.gx, sp.gy) + new Vector3(0f, 0.42f, 0f));
                            if (Mathf.Abs(spCanvas.x) < 340f && spCanvas.y > -470f && spCanvas.y < 525f)
                            {
                                _uiGlove.Hide();
                                _worldGlove.ShowAt(spCanvas, dt);
                                return;
                            }
                            _worldGlove.Hide();
                            _uiGlove.ShowAt(GetUiGloveTarget(_questCardBg != null ? _questCardBg.rectTransform : null, new Vector2(-130f, -482f)), dt);
                            return;
                        }
                        _uiGlove.Hide();
                        _worldGlove.Hide();
                        return;
                    }
                }

                if (_sim.CampLevel <= 5)
                {
                    if (q.step == 0 && q.actionKind == ForestSurvivalSim.QuestActionKind.FocusWorld)
                    {
                        // 阿澈正在奔跑或挥斧采集时隐藏白手套，绝不半途干扰！
                        if (_world.ChopQueue.Count > 0)
                        {
                            _uiGlove.Hide();
                            _worldGlove.Hide();
                            return;
                        }

                        if (TryResolveStep0WorldTarget(out Vector2 step0Grid, out float step0Lift))
                        {
                            Vector2 tp = WorldToCanvas(ForestSliceView.Iso(step0Grid.x, step0Grid.y) + new Vector3(0f, step0Lift, 0f));
                            if (Mathf.Abs(tp.x) < 330f && tp.y > -460f && tp.y < 515f)
                            {
                                _uiGlove.Hide();
                                _worldGlove.ShowAt(tp, dt);
                                return;
                            }
                            _worldGlove.Hide();
                            _uiGlove.ShowAt(GetUiGloveTarget(_questCardBg != null ? _questCardBg.rectTransform : null, new Vector2(-130f, -482f)), dt);
                            return;
                        }
                    }

                    if (tn != null && !_world.IsGridCoveredByForest(tn.gx, tn.gy) &&
                        (tn.state == NodeState.Discovered || tn.state == NodeState.ReadyToHammer || tn.state == NodeState.Constructing || tn.state == NodeState.Active))
                    {
                        float yLift = tn.state == NodeState.ReadyToHammer ? 0.9f : 0.55f;
                        Vector2 np = WorldToCanvas(ForestSliceView.Iso(tn.gx, tn.gy) + new Vector3(0f, yLift, 0f));
                        if (Mathf.Abs(np.x) < 330f && np.y > -460f && np.y < 540f)
                        {
                            _uiGlove.Hide();
                            _worldGlove.ShowAt(np, dt);
                            return;
                        }
                        // 目标建筑尚在视野外时，轻点左下主线卡一键平移聚焦
                        _worldGlove.Hide();
                        _uiGlove.ShowAt(GetUiGloveTarget(_questCardBg != null ? _questCardBg.rectTransform : null, new Vector2(-130f, -482f)), dt);
                        return;
                    }

                    switch (q.actionKind)
                    {
                        case ForestSurvivalSim.QuestActionKind.OpenResidentOrders:
                            _worldGlove.Hide();
                            _uiGlove.ShowAt(GetUiGloveTarget(_rightOrderEntryRt, new Vector2(277f, 490f), new Vector2(-10f, -6f)), dt);
                            return;

                        case ForestSurvivalSim.QuestActionKind.OpenHeroGrowth:
                        case ForestSurvivalSim.QuestActionKind.OpenHeroDelegation:
                            _worldGlove.Hide();
                            _uiGlove.ShowAt(GetUiGloveTarget(_dockBtns[2] != null ? _dockBtns[2].GetComponent<RectTransform>() : null, new Vector2(-58f, -582f)), dt);
                            return;

                        case ForestSurvivalSim.QuestActionKind.OpenCaravan:
                            _worldGlove.Hide();
                            _uiGlove.ShowAt(GetUiGloveTarget(_dockBtns[4] != null ? _dockBtns[4].GetComponent<RectTransform>() : null, new Vector2(172f, -582f)), dt);
                            return;

                        case ForestSurvivalSim.QuestActionKind.OpenCityRun:
                            _worldGlove.Hide();
                            _uiGlove.ShowAt(GetUiGloveTarget(_dockBtns[5] != null ? _dockBtns[5].GetComponent<RectTransform>() : null, new Vector2(288f, -582f)), dt);
                            return;

                        case ForestSurvivalSim.QuestActionKind.FocusWorld:
                        {
                            if (!_world.IsGridCoveredByForest(q.focusGrid.x, q.focusGrid.y))
                            {
                                Vector2 tp = WorldToCanvas(ForestSliceView.Iso(q.focusGrid.x, q.focusGrid.y) + new Vector3(0f, 0.45f, 0f));
                                if (Mathf.Abs(tp.x) < 330f && tp.y > -460f && tp.y < 540f)
                                {
                                    _uiGlove.Hide();
                                    _worldGlove.ShowAt(tp, dt);
                                    return;
                                }
                            }
                            _worldGlove.Hide();
                            _uiGlove.ShowAt(GetUiGloveTarget(_questCardBg != null ? _questCardBg.rectTransform : null, new Vector2(-130f, -482f)), dt);
                            return;
                        }

                        default:
                            _worldGlove.Hide();
                            _uiGlove.ShowAt(GetUiGloveTarget(_questCardBg != null ? _questCardBg.rectTransform : null, new Vector2(-130f, -482f)), dt);
                            return;
                    }
                }
            }

            _uiGlove.Hide();
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

            for (int i = _dmgPopups.Count - 1; i >= 0; i--)
            {
                DamagePopupItem dp = _dmgPopups[i];
                dp.age += dt;
                dp.rt.anchoredPosition = dp.startPos + new Vector2(0f, dp.age * 54f);
                float scale = dp.age < 0.12f ? Mathf.Lerp(0.75f, 1.18f, dp.age / 0.12f) : Mathf.Lerp(1.18f, 0.92f, (dp.age - 0.12f) / 0.55f);
                dp.rt.localScale = Vector3.one * scale;
                Color c = dp.txt.color;
                c.a = 1f - Mathf.Clamp01((dp.age - 0.35f) / 0.32f);
                dp.txt.color = c;
                if (dp.age >= 0.68f)
                {
                    SafeDestroy(dp.rt.gameObject);
                    _dmgPopups.RemoveAt(i);
                }
            }
        }

        // =====================================================================
        // 顶层启动 Loading 页（《末日囤货狂》带标题插画 + 装车进度条 + 底部《健康游戏忠告》全文）
        // =====================================================================
        void BuildBootLoadingOverlay()
        {
            RectTransform rt = ForestUiWidgets.MakeRect("BootLoadingOverlay", _canvasRt,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var backing = rt.gameObject.AddComponent<Image>();
            backing.color = new Color32(18, 13, 9, 255);
            backing.raycastTarget = true;
            _loadingCg = rt.gameObject.AddComponent<CanvasGroup>();
            _loadingOverlay = rt.gameObject;

            // 全屏等比铺满 9:16 主视觉插画（顶部含《末日囤货狂》立体木铁牌标题）
            RectTransform artRt = ForestUiWidgets.MakeRect("KeyArt", rt,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var artImg = artRt.gameObject.AddComponent<Image>();
            artImg.sprite = ForestComicUiSkin.GetArt("loading_hoarder", ForestComicUiSkin.GetArt("crisis_horde_bg"));
            artImg.preserveAspect = false;
            artImg.raycastTarget = false;
            var fitter = artRt.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 9f / 16f;

            // 底部深色渐变衬底，确保进度条与 48 字《健康游戏忠告》全文在任何手机分辨率下清晰易读
            Image footerBg = ForestUiWidgets.MakeSlicedImage("NoticeFooterBg", rt, null, new Color(0.06f, 0.04f, 0.03f, 0.82f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 188f), false);

            // 漫画胶囊加载进度条
            Image barBg = ForestUiWidgets.MakeSlicedImage("LoadingBarBg", footerBg.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(580f, 38f), false);
            RectTransform fillRt = ForestUiWidgets.MakeRect("Fill", barBg.transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8f, -8f));
            _loadingFillImg = fillRt.gameObject.AddComponent<Image>();
            _loadingFillImg.sprite = ForestComicUiSkin.BtnOrange;
            _loadingFillImg.type = Image.Type.Filled;
            _loadingFillImg.fillMethod = Image.FillMethod.Horizontal;
            _loadingFillImg.fillAmount = 0f;

            _loadingTipTxt = ForestUiWidgets.MakeText("TipTxt", barBg.transform, "正在发动破皮卡下山进货... 0%", 16, Color.white,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // 国家新闻出版署标准《健康游戏忠告》完整全文（微信小游戏提审必检项）
            ForestUiWidgets.MakeText("HealthyHeader", footerBg.transform, "《健康游戏忠告》", 16, new Color32(236, 206, 152, 255),
                TextAnchor.UpperCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 92f), new Vector2(-24f, 26f));

            const string healthyFullText =
                "抵制不良游戏，拒绝盗版游戏。注意自我保护，谨防受骗上当。\n" +
                "适度游戏益脑，沉迷游戏伤身。合理安排时间，享受健康生活。";
            Text body = ForestUiWidgets.MakeText("HealthyBody", footerBg.transform, healthyFullText, 15, new Color32(226, 218, 206, 255),
                TextAnchor.UpperCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 22f), new Vector2(-20f, 64f));
            body.lineSpacing = 1.22f;

            _loadingOverlay.SetActive(false);
        }

        public void StartBootLoading(float duration = 2.0f, Action onComplete = null)
        {
            _bootLoadingDuration = Mathf.Max(0.5f, duration);
            _bootLoadingElapsed = 0f;
            _onBootLoadingDone = onComplete;
            _bootLoadingActive = true;
            _loadingCg.alpha = 1f;
            _loadingOverlay.SetActive(true);
            ApplyLoadingProgressVisual(0.05f);
        }

        public void ShowBootLoadingPreview(float progress = 0.72f)
        {
            _bootLoadingActive = false;
            _loadingCg.alpha = 1f;
            _loadingOverlay.SetActive(true);
            ApplyLoadingProgressVisual(Mathf.Clamp01(progress));
        }

        void ApplyLoadingProgressVisual(float p)
        {
            _loadingFillImg.fillAmount = p;
            int pct = Mathf.RoundToInt(p * 100f);
            string phase = p < 0.42f ? "正在发动破皮卡下山进货..."
                         : p < 0.85f ? "正在把整条街物资搬回山寨..."
                         : "满载归营，准备升火开张...";
            _loadingTipTxt.text = $"{phase} {pct}%";
        }

        void TickBootLoading(float dt)
        {
            _bootLoadingElapsed += Mathf.Min(0.1f, Mathf.Max(dt, Time.unscaledDeltaTime));
            float loadPortion = _bootLoadingDuration;
            float fadePortion = 0.32f;
            if (_bootLoadingElapsed <= loadPortion)
            {
                float u = Mathf.Clamp01(_bootLoadingElapsed / loadPortion);
                float smooth = 1f - Mathf.Pow(1f - u, 1.8f);
                ApplyLoadingProgressVisual(smooth);
            }
            else
            {
                ApplyLoadingProgressVisual(1f);
                float fu = Mathf.Clamp01((_bootLoadingElapsed - loadPortion) / fadePortion);
                _loadingCg.alpha = 1f - fu;
                if (fu >= 1f)
                {
                    _bootLoadingActive = false;
                    _loadingOverlay.SetActive(false);
                    var cb = _onBootLoadingDone;
                    _onBootLoadingDone = null;
                    cb?.Invoke();
                }
            }
        }
    }
}
