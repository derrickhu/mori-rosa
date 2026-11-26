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
    public sealed partial class ForestSurvivalHud : MonoBehaviour
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
            public Image icon2;
            public Text label;
            public Text subTxt1;
            public Text subTxt2;
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
        GameObject _leaderAlertDot;
        RectTransform _rightOrderEntryRt;
        GameObject _orderRightDot;
        RectTransform _caravanEntryRt;
        Text _caravanSubTxt;
        GameObject _caravanRedDot;
        readonly Dictionary<SurvRes, RectTransform> _resPillRts = new Dictionary<SurvRes, RectTransform>();
        readonly Dictionary<SurvRes, Text> _resPillTxts = new Dictionary<SurvRes, Text>();
        readonly Dictionary<SurvRes, float> _resPulseTimers = new Dictionary<SurvRes, float>();

        // 左侧尸潮挂件、左下任务浮窗、底部 6 宫格 Dock
        GameObject _hordeBadgeGo;
        Text _hordeBadgeTimerTxt;
        Image _questCardBg, _questCheckImg, _questFillImg;
        Text _questTitleTxt;
        GameObject _questRewardTagGo;
        Text _questRewardTxt;
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

        // Tab 1: 9 名幸存者英雄（3×3 三阵营矩阵）养成与防具换装行
        readonly GameObject[] _haRows = new GameObject[MaxHeroUiSlots];
        readonly Image[] _haRarityFrames = new Image[MaxHeroUiSlots];
        readonly Image[] _haBusts = new Image[MaxHeroUiSlots];
        readonly Text[] _haStarBadges = new Text[MaxHeroUiSlots];
        readonly Text[] _haNames = new Text[MaxHeroUiSlots];
        readonly Text[] _haDescs = new Text[MaxHeroUiSlots];
        readonly Button[] _haLvUpBtns = new Button[MaxHeroUiSlots];
        readonly Text[] _haLvUpLbls = new Text[MaxHeroUiSlots];
        readonly Button[] _haStarUpBtns = new Button[MaxHeroUiSlots];
        readonly Text[] _haStarUpLbls = new Text[MaxHeroUiSlots];
        readonly Button[,] _haSkinBtns = new Button[MaxHeroUiSlots, 3];
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
        readonly GameObject[] _tfRosterCards = new GameObject[MaxHeroUiSlots];
        readonly Image[] _tfRosterFrames = new Image[MaxHeroUiSlots];
        readonly Image[] _tfRosterTagBgs = new Image[MaxHeroUiSlots];
        readonly Text[] _tfRosterTagTxts = new Text[MaxHeroUiSlots];
        readonly Image[] _tfRosterBusts = new Image[MaxHeroUiSlots];
        readonly Text[] _tfRosterNames = new Text[MaxHeroUiSlots];
        readonly Text[] _tfRosterRoles = new Text[MaxHeroUiSlots];
        readonly Text[] _tfRosterPowers = new Text[MaxHeroUiSlots];

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
        GameObject _bagInventoryModal;
        GameObject _heroDetailModal;
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
            (_wwCongratsModal != null && _wwCongratsModal.activeSelf) ||
            (_heroSpotlightModal != null && _heroSpotlightModal.activeSelf) ||
            (_heroAssignModal != null && _heroAssignModal.activeSelf) ||
            (_wavePrepModal != null && _wavePrepModal.activeSelf) ||
            (_tacticalFormationModal != null && _tacticalFormationModal.activeSelf) ||
            (_arenaResultModal != null && _arenaResultModal.activeSelf) ||
            (_quickListModal != null && _quickListModal.activeSelf) ||
            (_bagInventoryModal != null && _bagInventoryModal.activeSelf) ||
            (_heroDetailModal != null && _heroDetailModal.activeSelf) ||
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
            _haSkinTexts = new Text[MaxHeroUiSlots, 3];
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
            BuildBagInventoryModal();
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
            Image ico2 = ForestUiWidgets.MakeIcon("Ico2", rt, ForestComicUiSkin.GetArt("ico_check"), new Vector2(26f, 10f), new Vector2(32f, 32f));
            ico2.gameObject.SetActive(false);

            // 白色漫画气泡上统一使用无黑描边清爽深靛字 (ForestComicUiSkin.InkDark, outline: false)
            Text lbl = ForestUiWidgets.MakeText("Txt", rt, "", 17, ForestComicUiSkin.InkDark, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 5f), new Vector2(-14f, -12f), outline: false);
            Text sub1 = ForestUiWidgets.MakeText("Sub1", rt, "", 14, ForestComicUiSkin.InkDark, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -11f), new Vector2(64f, 20f), outline: false);
            Text sub2 = ForestUiWidgets.MakeText("Sub2", rt, "", 14, ForestComicUiSkin.InkDark, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(26f, -11f), new Vector2(60f, 20f), outline: false);
            sub1.gameObject.SetActive(false);
            sub2.gameObject.SetActive(false);

            var tb = new TrackBubble { rt = rt, bg = bg, icon = ico, icon2 = ico2, label = lbl, subTxt1 = sub1, subTxt2 = sub2 };
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
        // 2. 主营地 HUD（1:1《疯狂水世界》紧凑领主顶栏 + 左下浮动任务卡 + 底部 6 格石板漫画 Dock）
        // =====================================================================
        void BuildMainCampHud()
        {
            _mainHudLayer = ForestUiWidgets.Stretch("MainCampHud", _canvasRt).gameObject;
            Transform root = _mainHudLayer.transform;

            // 适配刘海屏/灵动岛与微信右上角胶囊安全区（顶部 116px 内严格保证全部元素右沿 x <= 390f！）
            float safeTopPx = Mathf.Max(0f, Screen.height - Screen.safeArea.yMax);
            float topPad = Mathf.Max(EditorTopSafePadOverride, Mathf.Clamp(safeTopPx * (1280f / Mathf.Max(1f, Screen.height)), 0f, 96f));
#if !UNITY_EDITOR && (UNITY_WEBGL || PLATFORM_WEIXINMINIGAME)
            topPad = Mathf.Max(topPad, 56f);
#endif
            _topSafePad = topPad;

            // --- 左上角领主阿澈切角品质头像框 (78x78, ClippedCardBlue + WwHeaderShell) ---
            Image avatarFrame = ForestUiWidgets.MakeSlicedImage("AvatarFrame", root, ForestComicUiSkin.ClippedCardBlue, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -14f - topPad), new Vector2(78f, 78f), true);
            avatarFrame.gameObject.AddComponent<Button>().onClick.AddListener(OpenHeroAssignModal);
            Image avatarInner = ForestUiWidgets.MakeSlicedImage("AvatarBg", avatarFrame.transform, ForestComicUiSkin.WwHeaderShell, Color.white,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), new Vector2(-10f, -10f), false);
            avatarInner.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            ForestUiWidgets.MakeIcon("LeaderBust", avatarInner.transform, ForestComicUiSkin.GetArt("bust_leader", ForestComicUiSkin.RedDotBadge),
                new Vector2(0f, -6f), new Vector2(82f, 82f));
            Image leaderAlert = ForestUiWidgets.MakeIcon("AlertBadge", avatarFrame.transform, ForestComicUiSkin.RedDotBadge,
                new Vector2(31f, 31f), new Vector2(24f, 24f));
            ForestUiWidgets.MakeText("Bang", leaderAlert.transform, "!", 16, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: false);
            _leaderAlertDot = leaderAlert.gameObject;

            // --- Row 1 (x = 104..378, y = -14 - topPad)：水世界闪耀经验星 ww_xp_star + 深海靛蓝经验槽 + 幸存者人口胶囊 ---
            Image xpBg = ForestUiWidgets.MakeSlicedImage("XpPill", root, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(104f, -14f - topPad), new Vector2(146f, 34f), false);
            RectTransform xpFillMask = ForestUiWidgets.MakeRect("XpFillArea", xpBg.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8f, -8f));
            _xpFillImg = xpFillMask.gameObject.AddComponent<Image>();
            _xpFillImg.sprite = ForestComicUiSkin.BtnOrange;
            _xpFillImg.type = Image.Type.Filled;
            _xpFillImg.fillMethod = Image.FillMethod.Horizontal;
            _xpFillImg.raycastTarget = false;
            _xpBarTxt = ForestUiWidgets.MakeText("XpTxt", xpBg.transform, "0/70", 14, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(10f, 0f), new Vector2(-22f, 0f));

            Image starIcon = ForestUiWidgets.MakeIcon("LvStar", xpBg.transform, ForestComicUiSkin.GetUiArt("ww_xp_star", ForestComicUiSkin.RedDotBadge),
                new Vector2(-69f, 1f), new Vector2(46f, 46f));
            _lvStarTxt = ForestUiWidgets.MakeText("LvNum", starIcon.transform, "1", 20, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -1f), Vector2.zero);

            Image popBg = ForestUiWidgets.MakeSlicedImage("PopPill", root, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(256f, -14f - topPad), new Vector2(122f, 34f), true);
            popBg.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenSourceRouteForCondition(CondKind.PopCur, SurvRes.Wood));
            ForestUiWidgets.MakeIcon("PopIco", popBg.transform, ForestComicUiSkin.GetArt("bust_cook", ForestComicUiSkin.RedDotBadge),
                new Vector2(-41f, 1f), new Vector2(32f, 32f));
            _popTxt = ForestUiWidgets.MakeText("PopTxt", popBg.transform, "4/8", 16, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(11f, 0f), new Vector2(-24f, 0f));

            // --- Row 2 (x = 98..378, y = -56 - topPad)：紧凑三联胶囊 [铜币] + [手稿] + [仓 Cur/Cap -> 打开道具背包] ---
            Image coinBg = ForestUiWidgets.MakeSlicedImage("CoinPill", root, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(98f, -56f - topPad), new Vector2(94f, 32f), true);
            coinBg.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenSourceRouteForCondition(CondKind.Coins, SurvRes.Wood));
            ForestUiWidgets.MakeIcon("CoinIco", coinBg.transform, ForestComicUiSkin.GetArt("ico_coin", ForestComicUiSkin.RedDotBadge),
                new Vector2(-32f, 0f), new Vector2(24f, 24f));
            _coinTxt = ForestUiWidgets.MakeText("CoinTxt", coinBg.transform, "120", 15, ForestComicUiSkin.AccentGold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(10f, 0f), new Vector2(-22f, 0f));

            Image bpBg = ForestUiWidgets.MakeSlicedImage("BlueprintPill", root, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(196f, -56f - topPad), new Vector2(80f, 32f), true);
            bpBg.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenSourceRouteForCondition(CondKind.Resource, SurvRes.Blueprint));
            ForestUiWidgets.MakeIcon("BpIco", bpBg.transform, ForestComicUiSkin.ResIcon(SurvRes.Blueprint),
                new Vector2(-24f, 0f), new Vector2(22f, 22f));
            _blueprintTxt = ForestUiWidgets.MakeText("BpTxt", bpBg.transform, "0", 15, new Color32(148, 214, 255, 255), TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(9f, 0f), new Vector2(-20f, 0f));

            Image capBg = ForestUiWidgets.MakeSlicedImage("CapPill", root, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(280f, -56f - topPad), new Vector2(98f, 32f), true);
            capBg.gameObject.AddComponent<Button>().onClick.AddListener(OpenBagInventoryModal);
            ForestUiWidgets.MakeIcon("CapIco", capBg.transform, ForestComicUiSkin.GetUiArt("ww_dock_bag", ForestComicUiSkin.GetArt("dock_bag")),
                new Vector2(-34f, 0f), new Vector2(22f, 22f));
            _storageCapTxt = ForestUiWidgets.MakeText("CapTxt", capBg.transform, "仓10/80", 13, new Color32(220, 238, 255, 255), TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(9f, 0f), new Vector2(-20f, 0f));

            // 头像下方紧凑温饱笑脸微章（108x30，与左侧 108x32 竖向资源栈等宽对齐，右沿 x = 122 <= 390）
            Image moraleBg = ForestUiWidgets.MakeSlicedImage("MoralePill", root, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -96f - topPad), new Vector2(108f, 30f), true);
            moraleBg.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                SpawnScreenCenterBanner($"营地状态：{_sim.MoraleStatusSummary}（炊事房存足口粮+草药坊备药可维持最高+32%全营提速！）");
            });
            ForestUiWidgets.MakeIcon("MoraleIco", moraleBg.transform, ForestComicUiSkin.GetUiArt("ww_smiley_morale", ForestComicUiSkin.ResIcon(SurvRes.Ration)),
                new Vector2(-37f, 0f), new Vector2(23f, 23f));
            _moralePillTxt = ForestUiWidgets.MakeText("MoraleTxt", moraleBg.transform, "温饱+20%", 13, ForestComicUiSkin.AccentGreen, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(11f, 0f), Vector2.zero);

            // --- 右侧常驻快捷挂件（严格置于 y <= -132 避让微信胶囊，并避让 y ~ -198..-238 的 [GM调试]！） ---
            float gmRefTop = Mathf.Min(-112f, -16f - topPad - 56f);
            float rightOrderTop = Mathf.Min(-132f, gmRefTop);
            Image orderEntry = ForestUiWidgets.MakeSlicedImage("RightOrderEntry", root, ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, rightOrderTop), new Vector2(138f, 60f), true);
            _rightOrderEntryRt = orderEntry.rectTransform;
            orderEntry.gameObject.AddComponent<Button>().onClick.AddListener(OpenResidentDemandDrawer);
            ForestUiWidgets.MakeIcon("OrdGift", orderEntry.transform, ForestComicUiSkin.GetUiArt("ww_order_gift_box", ForestComicUiSkin.GetArt("bust_cook")),
                new Vector2(-44f, 2f), new Vector2(48f, 48f));
            ForestUiWidgets.MakeText("OrdTitle", orderEntry.transform, "居民需求", 16, ForestComicUiSkin.AccentGold, TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(22f, -6f), new Vector2(-50f, -8f));
            _orderBadgeCntTxt = ForestUiWidgets.MakeText("OrdSub", orderEntry.transform, "0/5", 14, Color.white, TextAnchor.LowerLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(22f, 6f), new Vector2(-50f, -8f));
            _orderRightDot = ForestUiWidgets.MakeIcon("Dot", orderEntry.transform, ForestComicUiSkin.RedDotBadge, new Vector2(58f, 22f), new Vector2(22f, 22f)).gameObject;

            // 右侧【行商电台】快捷胶囊（置于 [GM调试] 下方 y <= -252，零重叠！）
            float caravanTop = Mathf.Min(-252f, gmRefTop - 134f);
            Image caravanEntry = ForestUiWidgets.MakeSlicedImage("CaravanRightEntry", root, ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, caravanTop), new Vector2(128f, 52f), true);
            _caravanEntryRt = caravanEntry.rectTransform;
            caravanEntry.gameObject.AddComponent<Button>().onClick.AddListener(OpenCaravanModal);
            ForestUiWidgets.MakeIcon("RadioIco", caravanEntry.transform, ForestComicUiSkin.GetUiArt("ww_dept_trade", ForestComicUiSkin.GetArt("radio_post")),
                new Vector2(-40f, 1f), new Vector2(38f, 38f));
            ForestUiWidgets.MakeText("CvTitle", caravanEntry.transform, "行商电台", 15, ForestComicUiSkin.AccentGold, TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(18f, -5f), new Vector2(-44f, -6f));
            _caravanSubTxt = ForestUiWidgets.MakeText("CvSub", caravanEntry.transform, "大宗贸易", 12, new Color32(214, 228, 255, 255), TextAnchor.LowerLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(18f, 5f), new Vector2(-44f, -6f));
            _caravanRedDot = ForestUiWidgets.MakeIcon("Dot", caravanEntry.transform, ForestComicUiSkin.RedDotBadge, new Vector2(54f, 18f), new Vector2(20f, 20f)).gameObject;
            _caravanEntryRt.gameObject.SetActive(false);

            // --- 左侧竖向精简核心资源微胶囊栈 (y = -132f - _topSafePad, 108x32 清透半透明) ---
            for (int i = 0; i < HudPinnedRes.Length; i++)
            {
                SurvRes r = HudPinnedRes[i];
                Image pill = ForestUiWidgets.MakeSlicedImage("ResPill_" + r, root, ForestComicUiSkin.DarkPill, new Color(1f, 1f, 1f, 0.90f),
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -132f - topPad - i * 35f), new Vector2(108f, 32f), false);
                ForestUiWidgets.MakeIcon("Ico", pill.transform, ForestComicUiSkin.ResIcon(r), new Vector2(-35f, 0f), new Vector2(25f, 25f));
                Text valTxt = ForestUiWidgets.MakeText("Val", pill.transform, "0", 16, Color.white, TextAnchor.MiddleLeft,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(17f, 0f), new Vector2(-34f, 0f));
                _resPillRts[r] = pill.rectTransform;
                _resPillTxts[r] = valTxt;
                _resPulseTimers[r] = 0f;
            }

            // --- 左侧尸潮来袭倒计时挂件 ---
            Image hordeBg = ForestUiWidgets.MakeSlicedImage("HordeLeftBadge", root, ForestComicUiSkin.DarkPill, new Color(1f, 0.85f, 0.85f, 1f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -356f - topPad), new Vector2(134f, 54f), true);
            hordeBg.gameObject.AddComponent<Button>().onClick.AddListener(OpenWavePrepModal);
            ForestUiWidgets.MakeIcon("ZomIco", hordeBg.transform, ForestComicUiSkin.GetArt("bust_zombie_boss", ForestComicUiSkin.RedDotBadge),
                new Vector2(-42f, 2f), new Vector2(42f, 42f));
            ForestUiWidgets.MakeText("Title", hordeBg.transform, "尸潮将至", 15, new Color32(255, 196, 120, 255), TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(18f, -5f), new Vector2(-44f, -6f));
            _hordeBadgeTimerTxt = ForestUiWidgets.MakeText("Timer", hordeBg.transform, "29:59", 16, new Color32(255, 86, 76, 255), TextAnchor.LowerLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(18f, 5f), new Vector2(-44f, -6f));
            _hordeBadgeGo = hordeBg.gameObject;
            _hordeBadgeGo.SetActive(false);

            // --- 左下角水世界悬浮漫画主线任务卡 (476x76, 左侧微倾写字板 + 底进度条 + 右上浮动 +XP 奖励角标) ---
            _questCardBg = ForestUiWidgets.MakeSlicedImage("FloatingQuestCard", root, ForestComicUiSkin.QuestCard, Color.white,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(14f, 128f), new Vector2(476f, 76f), true);
            var qBtn = _questCardBg.gameObject.AddComponent<Button>();
            qBtn.targetGraphic = _questCardBg;
            qBtn.onClick.AddListener(OnQuestCardClicked);

            Image qTrack = ForestUiWidgets.MakeSlicedImage("QTrack", _questCardBg.transform, ForestComicUiSkin.DarkPill, new Color(0.06f, 0.09f, 0.18f, 0.72f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(18f, 5f), new Vector2(-54f, 8f), false);
            RectTransform qFillArea = ForestUiWidgets.Stretch("QFill", qTrack.transform, 1f, 1f, 1f, 1f);
            _questFillImg = qFillArea.gameObject.AddComponent<Image>();
            _questFillImg.sprite = ForestComicUiSkin.BtnGreen;
            _questFillImg.type = Image.Type.Filled;
            _questFillImg.fillMethod = Image.FillMethod.Horizontal;
            _questFillImg.raycastTarget = false;

            Image clipImg = ForestUiWidgets.MakeIcon("ClipIco", _questCardBg.transform, ForestComicUiSkin.GetArt("ico_clipboard", ForestComicUiSkin.RedDotBadge),
                new Vector2(-204f, 5f), new Vector2(52f, 52f));
            clipImg.rectTransform.localEulerAngles = new Vector3(0f, 0f, 8f);
            _questCheckImg = ForestUiWidgets.MakeIcon("DoneCheck", clipImg.transform, ForestComicUiSkin.GetUiArt("ww_check_green"),
                new Vector2(14f, -12f), new Vector2(28f, 28f));
            _questTitleTxt = ForestUiWidgets.MakeText("QuestTxt", _questCardBg.transform, "加载主线任务...", 18, Color.white, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(28f, 4f), new Vector2(-84f, -18f));

            // 右上角浮动经验预览微标签 (ww_xp_star + +XP)
            Image qRwTag = ForestUiWidgets.MakeSlicedImage("QuestRewardTag", _questCardBg.transform, ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-10f, 2f), new Vector2(96f, 26f), false);
            ForestUiWidgets.MakeIcon("RwStar", qRwTag.transform, ForestComicUiSkin.GetUiArt("ww_xp_star"),
                new Vector2(-32f, 0f), new Vector2(24f, 24f));
            _questRewardTxt = ForestUiWidgets.MakeText("RwVal", qRwTag.transform, "+35XP", 13, ForestComicUiSkin.AccentGold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(9f, 0f), Vector2.zero);
            _questRewardTagGo = qRwTag.gameObject;

            // --- 底部 1:1《疯狂水世界》6 槽石板漫画导航栏 (height 118px, 首尾槽破框突出！) ---
            Image dockShelf = ForestUiWidgets.MakeSlicedImage("BottomComicDock", root, ForestComicUiSkin.WwStoneTray, new Color32(28, 34, 58, 252),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 118f), true);

            string[] dockNames = { "迎击", "建造", "英雄", "道具", "委任", "丧尸城" };
            string[] dockIcons = { "ww_dock_battle", "ww_dock_build", "ww_dock_heroes", "ww_dock_bag", "ww_dock_delegate", "ww_dock_citymap" };
            string[] dockFallbacks = { "dock_defense", "dock_build", "dock_heroes", "dock_bag", "dock_radio", "dock_map" };
            for (int i = 0; i < 6; i++)
            {
                int tabIdx = i;
                bool isEndCap = (i == 0 || i == 5);
                float x = -297.5f + i * 119f;
                float slotH = isEndCap ? 112f : 106f;
                float slotY = isEndCap ? 6f : 4f;

                Image slot = ForestUiWidgets.MakeSlicedImage("DockTab_" + i, dockShelf.transform, ForestComicUiSkin.WwDockSlate, Color.white,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x, slotY), new Vector2(116f, slotH), true);
                var btn = slot.gameObject.AddComponent<Button>();
                btn.targetGraphic = slot;
                btn.onClick.AddListener(() => OnBottomDockClicked(tabIdx));
                _dockBtns[i] = btn;

                float icoSize = isEndCap ? 92f : 76f;
                float icoY = isEndCap ? 24f : 14f;
                Sprite dockSp = ForestComicUiSkin.GetUiArt(dockIcons[i], ForestComicUiSkin.GetArt(dockFallbacks[i], ForestComicUiSkin.RedDotBadge));
                Image artImg = ForestUiWidgets.MakeIcon("Art", slot.transform, dockSp, new Vector2(0f, icoY), new Vector2(icoSize, icoSize));
                if (i == 0) artImg.rectTransform.localEulerAngles = new Vector3(0f, 0f, 2f);
                else if (i == 5) artImg.rectTransform.localEulerAngles = new Vector3(0f, 0f, -3f);

                // 首槽专属《疯狂水世界》斜切漫画 "BANG!" 激战标牌（仅当 ww_dock_battle 未烘焙 BANG! 时作为程序化兜底）
                if (i == 0 && !HasRawForestArtAsset("ww_dock_battle"))
                {
                    Image bangTag = ForestUiWidgets.MakeSlicedImage("BangTag", slot.transform, ForestComicUiSkin.BtnRed, Color.white,
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-12f, 8f), new Vector2(68f, 24f), false);
                    bangTag.rectTransform.localEulerAngles = new Vector3(0f, 0f, 9f);
                    ForestUiWidgets.MakeText("BangTxt", bangTag.transform, "BANG!", 13, ForestComicUiSkin.AccentGold, TextAnchor.MiddleCenter,
                        Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                }

                // 石板底部清爽无脏描边深靛标签
                ForestUiWidgets.MakeText("Name", slot.transform, dockNames[i], 16, ForestComicUiSkin.InkDark, TextAnchor.LowerCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0f), new Vector2(0f, 5f), new Vector2(-4f, -8f), outline: false);

                Image lk = ForestUiWidgets.MakeIcon("Lock", slot.transform, ForestComicUiSkin.GetArt("ico_lock"), new Vector2(34f, -28f), new Vector2(28f, 28f));
                _dockLocks[i] = lk;

                Image dot = ForestUiWidgets.MakeIcon("RedDot", slot.transform, ForestComicUiSkin.RedDotBadge,
                    i == 0 ? new Vector2(46f, 45f) : new Vector2(42f, 40f), new Vector2(22f, 22f));
                _dockRedDots[i] = dot.gameObject;
            }
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

        static RectTransform MakeHorizontalScrollArea(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size, float contentWidth)
        {
            RectTransform viewRt = ForestUiWidgets.MakeRect(name, parent, anchorMin, anchorMax, pivot, anchoredPos, size);
            var maskImg = viewRt.gameObject.AddComponent<Image>();
            maskImg.color = new Color(1f, 1f, 1f, 0.01f);
            viewRt.gameObject.AddComponent<RectMask2D>();

            RectTransform contentRt = ForestUiWidgets.MakeRect("Content", viewRt,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(contentWidth, 0f));

            var sr = viewRt.gameObject.AddComponent<ScrollRect>();
            sr.viewport = viewRt;
            sr.content = contentRt;
            sr.horizontal = true;
            sr.vertical = false;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.elasticity = 0.08f;
            sr.scrollSensitivity = 28f;
            return contentRt;
        }

        // 共享快捷 UI 构建包装器（供各 partial 模块直接调用）
        static Image MakeImg(string name, Transform parent, Sprite sprite, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size, bool raycast = false)
            => ForestUiWidgets.MakeSlicedImage(name, parent, sprite, color, anchorMin, anchorMax, pivot, pos, size, raycast);

        static Text MakeText(string name, Transform parent, string content, int fontSize, Color color,
            TextAnchor align, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size, bool outline = false, FontStyle style = FontStyle.Bold)
            => ForestUiWidgets.MakeText(name, parent, content, fontSize, color, align, anchorMin, anchorMax, pivot, pos, size, outline, style);

        static Button MakeBtn(string name, Transform parent, Sprite skin, string label, int fontSize,
            Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick, out Text labelOut)
            => ForestUiWidgets.MakeJellyButton(name, parent, skin, label, fontSize, anchor, pivot, pos, size, onClick, out labelOut);

        // =====================================================================
        // 9. 丧尸城街区选择 + 武装皮卡车载射击/氮气冲撞 HUD + 【恭喜获得】放射结算
        // =====================================================================
        void BuildCitySelectAndDriveHud()
        {
            _citySelectModal = MakeModalMask("CityDistrictSelectModal", 0.68f, CloseAllModals);
            Image card = ForestUiWidgets.MakeSlicedImage("Card", _citySelectModal.transform, ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(648f, 828f), true);
            ForestUiWidgets.MakeIcon("BreakTruck", card.transform, ForestComicUiSkin.GetArt("truck_active"), new Vector2(-212f, 392f), new Vector2(184f, 184f));
            ForestUiWidgets.MakeText("Title", card.transform, "选择丧尸城搜刮街区", 27, Color.white, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(208f, -18f), new Vector2(-272f, 40f));
            ForestUiWidgets.MakeText("Sub", card.transform, "随车小队自动扫射尸群！击败十字路口尸王可夺取【英雄勋章+工程手稿】黄金空投箱！", 15, ForestComicUiSkin.InkHeaderLight, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(208f, -56f), new Vector2(-240f, 44f), outline: false);
            ForestUiWidgets.MakeJellyButton("Close", card.transform, ForestComicUiSkin.BtnRed, "×", 25,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -14f), new Vector2(46f, 46f), CloseAllModals, out _);

            Image paper = ForestUiWidgets.MakeSlicedImage("Paper", card.transform, ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -38f), new Vector2(604f, 676f), false);

            for (int i = 0; i < 4; i++)
            {
                int sIdx = i;
                CitySiteDef s = _sim.CitySites[i];
                Image row = ForestUiWidgets.MakeSlicedImage("Site_" + i, paper.transform, ForestComicUiSkin.WwSubPanel, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f - i * 162f), new Vector2(568f, 148f), false);
                ForestUiWidgets.MakeIcon("Building", row.transform, ForestComicUiSkin.GetArt(s.spriteKey), new Vector2(-208f, 2f), new Vector2(124f, 124f));
                ForestUiWidgets.MakeText("Name", row.transform, $"{s.name}  ({s.dangerTag})", 21, ForestComicUiSkin.InkDark, TextAnchor.UpperLeft,
                    new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(148f, -16f), new Vector2(-310f, 34f), outline: false);
                ForestUiWidgets.MakeText("Loot", row.transform, s.lootSummary, 15, ForestComicUiSkin.InkMuted, TextAnchor.MiddleLeft,
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
            _cdLootSummaryTxt = ForestUiWidgets.MakeText("LootSum", sumPill.transform, "随车英雄自动开火！拖动摇杆搜刮物资与黄金空投箱！", 15, ForestComicUiSkin.AccentGold, TextAnchor.MiddleCenter,
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

            _lootSettleModal = MakeModalMask("LootSettleModal", 0.90f, () =>
            {
                _lootSettleModal.SetActive(false);
                _cityDriveHud.SetActive(false);
                _mainHudLayer.SetActive(true);
                _worldTrackLayer.SetActive(true);
                _cityArena.FinishAndReturnCamp();
            });
            _lootSettleModal.GetComponent<Image>().color = new Color32(18, 24, 52, 236);
            _lsSunburst = ForestUiWidgets.MakeIcon("Sunburst", _lootSettleModal.transform, ForestComicUiSkin.Sunburst, new Vector2(0f, 60f), new Vector2(660f, 660f));
            Image congratsBanner = ForestUiWidgets.MakeIcon("CongratsBanner", _lootSettleModal.transform, ForestComicUiSkin.GetUiArt("ww_banner_congrats"),
                new Vector2(0f, 340f), new Vector2(420f, 140f));
            if (!HasRawForestArtAsset("ww_banner_congrats"))
            {
                ForestUiWidgets.MakeText("CongratsTitle", congratsBanner.transform, "恭喜获得", 44, ForestComicUiSkin.AccentGold, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), Vector2.zero);
            }

            for (int i = 0; i < 5; i++)
            {
                Image box = ForestUiWidgets.MakeSlicedImage("LootCard_" + i, _lootSettleModal.transform, ForestComicUiSkin.ClippedCardBlue, Color.white,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-240f + i * 120f, 40f), new Vector2(108f, 118f), false);
                _lsCards[i] = box.gameObject;
                _lsCardIcons[i] = ForestUiWidgets.MakeIcon("Ico", box.transform, ForestComicUiSkin.ResIcon(SurvRes.Scrap), new Vector2(0f, 14f), new Vector2(64f, 64f));
                _lsCardTxts[i] = ForestUiWidgets.MakeText("Cnt", box.transform, "+12", 16, Color.white, TextAnchor.LowerCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(-6f, -6f), outline: true);
            }

            ForestUiWidgets.MakeText("Hint", _lootSettleModal.transform, "点击空白区域满载回营", 20, ForestComicUiSkin.InkHeaderLight, TextAnchor.LowerCenter,
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

            Image fastPill = ForestUiWidgets.MakeSlicedImage("FastTapTag", castBtn.transform, ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(142f, 36f), false);
            _fishFastTapTag = ForestUiWidgets.MakeText("FastTxt", fastPill.transform, "快速点击", 19, ForestComicUiSkin.InkDark, TextAnchor.MiddleCenter,
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
            _comicDialogOverlay = MakeModalMask("ComicBustDialogOverlay", 0.68f, AdvanceComicDialog);

            _cdLeftBust = ForestUiWidgets.MakeIcon("LeftBust", _comicDialogOverlay.transform, ForestComicUiSkin.GetArt("bust_leader"),
                new Vector2(-176f, -52f), new Vector2(304f, 304f));
            _cdRightBust = ForestUiWidgets.MakeIcon("RightBust", _comicDialogOverlay.transform, ForestComicUiSkin.GetArt("bust_mechanic"),
                new Vector2(176f, -52f), new Vector2(304f, 304f));

            // 皇家靛蓝漫画外框 + 清爽冷白内衬卡 (WwHeaderShell + WwLightSheet)
            Image shell = ForestUiWidgets.MakeSlicedImage("DialogShell", _comicDialogOverlay.transform, ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 212f), new Vector2(672f, 214f), false);
            Image box = ForestUiWidgets.MakeSlicedImage("SpeechPanel", shell.transform, ForestComicUiSkin.WwLightSheet, Color.white,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-18f, -18f), false);

            // 左上破框皇家靛蓝角色名牌
            Image nameBadge = ForestUiWidgets.MakeSlicedImage("SpeakerTag", shell.transform, ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(22f, 4f), new Vector2(162f, 44f), false);
            _cdSpeakerNameTxt = ForestUiWidgets.MakeText("Speaker", nameBadge.transform, "小棠", 21, ForestComicUiSkin.AccentGold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // 无黑描边清爽深靛台词正文 (ForestComicUiSkin.InkDark, outline: false)
            _cdLineBodyTxt = ForestUiWidgets.MakeText("LineBody", box.transform, "...", 21, ForestComicUiSkin.InkDark, TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), new Vector2(-48f, -52f), outline: false);
            _cdLineBodyTxt.lineSpacing = 1.24f;

            ForestUiWidgets.MakeText("NextHint", box.transform, "点击任意处继续 ▶", 15, ForestComicUiSkin.InkMuted, TextAnchor.LowerRight,
                Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(-18f, 10f), new Vector2(-40f, -20f), outline: false);

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
            if (_wwCongratsModal != null) _wwCongratsModal.SetActive(false);
            if (_heroSpotlightModal != null) _heroSpotlightModal.SetActive(false);
            if (_heroAssignModal != null) _heroAssignModal.SetActive(false);
            if (_wavePrepModal != null) _wavePrepModal.SetActive(false);
            if (_tacticalFormationModal != null) _tacticalFormationModal.SetActive(false);
            if (_arenaResultModal != null && (_world == null || !_world.InWaveDefense)) _arenaResultModal.SetActive(false);
            if (_quickListModal != null) _quickListModal.SetActive(false);
            if (_bagInventoryModal != null) _bagInventoryModal.SetActive(false);
            if (_heroDetailModal != null) _heroDetailModal.SetActive(false);
            if (_citySelectModal != null) _citySelectModal.SetActive(false);
            CloseHeroAndBagSubPopups();
            CloseOrdersAndCombatSubPopups();
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
                Image cardBg = _lsCards[i].GetComponent<Image>();
                if (cardBg != null)
                    cardBg.sprite = ForestComicUiSkin.GetClippedCard(ForestComicUiSkin.GetResRarity0To5(pairs[i].Key));
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
                case 2: OpenHeroModal(1); break; // 英雄：角色养成与图鉴总览
                case 3: OpenBagInventoryModal(); break; // 道具：全屏 4 列切角品质背包
                case 4: OpenHeroModal(0); break; // 委任：水世界 6 大岗位多槽委任抽屉
                case 5: OpenCityDistrictSelector(); break;
            }
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
            Vector2 toCanvas = new Vector2(59.5f, -580f); // 默认飞向底部第4槽【道具】背包
            if (_cityArena != null && _cityArena.IsRunning)
                toCanvas = new Vector2(0f, 540f);
            else if (res == SurvRes.Blueprint)
                toCanvas = new Vector2(-118f, 568f - _topSafePad);
            else if (_resPillRts.TryGetValue(res, out RectTransform pillRt) && pillRt.gameObject.activeSelf)
                toCanvas = new Vector2(-360f + pillRt.anchoredPosition.x + 26f, 640f + pillRt.anchoredPosition.y - 16f);

            Image fi = ForestUiWidgets.MakeIcon("FlyRes", _canvasRt, ForestComicUiSkin.ResIcon(res), fromCanvas, new Vector2(36f, 36f));
            _flyIcons.Add(new FlyIconItem { rt = fi.rectTransform, img = fi, from = fromCanvas, to = toCanvas, t = 0f, dur = 0.45f, targetRes = res });
        }

        void SpawnXpFlyStar(int xp, Vector2 worldGrid)
        {
            if (_world != null && _world.InWaveDefense) return;
            Vector2 from = WorldToCanvas(ForestSliceView.Iso(worldGrid.x, worldGrid.y) + new Vector3(0f, 0.45f, 0f));
            Vector2 to = new Vector2(-248f, 609f - _topSafePad);
            Image star = ForestUiWidgets.MakeIcon("FlyXpStar", _canvasRt, ForestComicUiSkin.GetUiArt("ww_xp_star", ForestComicUiSkin.RedDotBadge), from, new Vector2(46f, 46f));
            ForestUiWidgets.MakeText("XpLbl", star.transform, $"+{xp}XP", 17, ForestComicUiSkin.AccentGold, TextAnchor.LowerCenter,
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

        static int GetQuestXpPreview(int questIndex)
        {
            switch (questIndex)
            {
                case 0: return 35;
                case 1: return 40;
                case 2: return 45;
                case 3: return 45;
                case 4: return 45;
                case 5: return 55;
                case 6: return 55;
                case 7: return 60;
                case 8: return 70;
                case 9: return 75;
                case 10: return 80;
                case 11: return 85;
                case 12: return 95;
                case 13: return 105;
                case 14: return 115;
                case 15: return 125;
                case 16: return 135;
                case 17: return 150;
                case 18: return 165;
                case 19: return 180;
                case 20: return 200;
                default: return 250;
            }
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
                _moralePillTxt.text = mt == 2 ? "沸腾+32%" : (mt == 1 ? "温饱+20%" : "士气+0%");
                _moralePillTxt.color = mt == 2 ? (Color)ForestComicUiSkin.AccentGold : (mt == 1 ? (Color)ForestComicUiSkin.AccentGreen : (Color)ForestComicUiSkin.InkHeaderLight);
            }

            int readyOrders = 0;
            for (int i = 0; i < _sim.ActiveOrders.Count; i++)
                if (_sim.CanSatisfyOrder(i)) readyOrders++;
            bool chestReady = _sim.SatisfiedToday >= ForestSurvivalSim.SatisfiedGoal;
            _orderBadgeCntTxt.text = chestReady
                ? $"{_sim.SatisfiedToday}/{ForestSurvivalSim.SatisfiedGoal} 领宝箱"
                : (readyOrders > 0 ? $"可交({readyOrders}) {_sim.SatisfiedToday}/{ForestSurvivalSim.SatisfiedGoal}" : $"{_sim.SatisfiedToday}/{ForestSurvivalSim.SatisfiedGoal}");
            _orderBadgeCntTxt.color = (chestReady || readyOrders > 0) ? (Color)ForestComicUiSkin.AccentGreen : Color.white;
            _orderRightDot.SetActive(readyOrders > 0 || chestReady);

            int slot = 0;
            for (int i = 0; i < HudPinnedRes.Length; i++)
            {
                SurvRes r = HudPinnedRes[i];
                RectTransform rt = _resPillRts[r];
                bool vis = _sim.IsResRevealed(r);
                rt.gameObject.SetActive(vis);
                if (!vis) continue;

                rt.anchoredPosition = new Vector2(14f, -132f - _topSafePad - slot * 35f);
                int val = _sim.Get(r);
                _resPillTxts[r].text = val.ToString();
                _resPillTxts[r].color = (ForestSurvivalSim.IsWarehouseCapped(r) && val >= _sim.StorageCap)
                    ? (Color)ForestComicUiSkin.AccentRed
                    : Color.white;

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
                _hordeBadgeGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(14f, -136f - _topSafePad - slot * 35f);
                int sec = Mathf.CeilToInt(_sim.HordeTimerSec);
                _hordeBadgeTimerTxt.text = $"{sec / 60:00}:{sec % 60:00}";
            }

            var q = _sim.GetCurrentQuest();
            _questCardBg.sprite = q.done ? ForestComicUiSkin.QuestCardDone : ForestComicUiSkin.QuestCard;
            _questFillImg.sprite = q.done ? ForestComicUiSkin.BtnGreen : ForestComicUiSkin.BtnOrange;
            _questCheckImg.gameObject.SetActive(q.done);
            string qDisplay = q.done ? $"✓ {q.text} [点击领奖]" : q.text;
            _questTitleTxt.fontSize = qDisplay != null && qDisplay.Length > 21 ? 16 : 18;
            _questTitleTxt.text = qDisplay;
            _questFillImg.fillAmount = q.max > 0 ? Mathf.Clamp01(q.cur / (float)q.max) : 1f;
            if (_questRewardTagGo != null)
            {
                bool showRwTag = _sim.QuestIndex < 22;
                _questRewardTagGo.SetActive(showRwTag);
                if (showRwTag && _questRewardTxt != null)
                    _questRewardTxt.text = $"+{GetQuestXpPreview(_sim.QuestIndex)}XP";
            }
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

            // 右侧【行商电台】快捷入口（CampLevel >= 3 或持有招募卡 或电台揭开/修复 或主线推进至行商时显示）
            if (_caravanEntryRt != null)
            {
                CampNode radioNode = _sim.Find("radio_post");
                bool radioRevealed = radioNode != null && radioNode.state != NodeState.HiddenInForest && !_world.IsGridCoveredByForest(radioNode.gx, radioNode.gy);
                bool hasRecruitCard = _sim.Get(SurvRes.RecruitTicket) > 0;
                bool showCaravan = _sim.CampLevel >= 3 || hasRecruitCard || _sim.RadioRepaired || radioRevealed || (!q.done && q.actionKind == ForestSurvivalSim.QuestActionKind.OpenCaravan);
                _caravanEntryRt.gameObject.SetActive(showCaravan);
                if (showCaravan)
                {
                    bool anyTradeReady = false;
                    if (_sim.RadioRepaired)
                    {
                        for (int i = 0; i < _sim.CaravanOffers.Count; i++)
                        {
                            CaravanOffer co = _sim.CaravanOffers[i];
                            if (!co.traded && _sim.Get(co.giveRes) >= co.giveAmt)
                            {
                                anyTradeReady = true;
                                break;
                            }
                        }
                    }
                    if (_caravanSubTxt != null)
                        _caravanSubTxt.text = hasRecruitCard ? $"招募({_sim.Get(SurvRes.RecruitTicket)})" : (!_sim.RadioRepaired ? "广播招募" : (anyTradeReady ? "可交割!" : "广播/行商"));
                    if (_caravanRedDot != null)
                        _caravanRedDot.SetActive(anyTradeReady || hasRecruitCard);
                }
            }

            _dockLocks[0].gameObject.SetActive(false); // 迎击
            _dockLocks[1].gameObject.SetActive(false); // 建造
            _dockLocks[2].gameObject.SetActive(false); // 英雄养成图鉴
            _dockLocks[3].gameObject.SetActive(false); // 道具背包
            _dockLocks[4].gameObject.SetActive(false); // 委任抽屉
            _dockLocks[5].gameObject.SetActive(!_sim.CanLaunchCityRun); // 丧尸城

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

            bool anyHeroGrowth = _sim.Get(SurvRes.RecruitTicket) > 0;
            bool anyHeroUnassigned = false;
            for (int i = 0; i < _sim.Heroes.Count; i++)
            {
                HeroSpec h = _sim.Heroes[i];
                if (IsHeroUpgradeableNow(i)) anyHeroGrowth = true;
                if (!h.unlocked) continue;
                if (!h.assigned) anyHeroUnassigned = true;
            }
            if (_leaderAlertDot != null)
                _leaderAlertDot.SetActive(anyHeroUnassigned || anyHeroGrowth);

            BattleStageSpec curStageSpec = _sim.GetBattleStageSpec(_sim.CurrentBattleStage);
            _sim.GetPatrolPendingRewards(out int patExp, out _, out _, out int patMins);
            bool waveReady = _sim.CalcDeployedSquadPower() >= curStageSpec.recommendedPower || showHorde || (patMins >= 5 && patExp > 0);
            _dockRedDots[0].SetActive(waveReady);
            _dockRedDots[1].SetActive(anyAffordableBuild);
            _dockRedDots[2].SetActive(anyHeroGrowth);
            _dockRedDots[3].SetActive(_sim.CurrentMaxSingleStock >= _sim.StorageCap || _sim.Get(SurvRes.ChoiceResBox) > 0);
            _dockRedDots[4].SetActive(anyHeroUnassigned);
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
                    if (cm.icon2 != null) cm.icon2.gameObject.SetActive(false);
                    if (cm.subTxt1 != null) cm.subTxt1.gameObject.SetActive(false);
                    if (cm.subTxt2 != null) cm.subTxt2.gameObject.SetActive(false);
                    cm.icon.gameObject.SetActive(true);
                    cm.icon.sprite = ForestComicUiSkin.GetUiArt("ww_check_green", ForestComicUiSkin.GetArt("ico_check"));
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
                    new Vector2(136f, 50f), true, ForestComicUiSkin.ResIcon(SurvRes.Wood), "砍伐扩地", ForestComicUiSkin.AccentGreen,
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
                        new Vector2(142f, 44f), true, null, $"施工 {rem}秒 [秒完]", ForestComicUiSkin.AccentGreen, () => _sim.SpeedUpNodeBuild(cap), zoomScale);
                }
                else if (n.state == NodeState.Discovered && n.kind == NodeKind.RelicLoot)
                {
                    CampNode cap = n;
                    TryBindBubble(ref used, ForestSliceView.Iso(n.gx, n.gy) + new Vector3(0f, 0.55f, 0f), Vector2.zero,
                        new Vector2(124f, 46f), true, ForestComicUiSkin.ResIcon(SurvRes.Blueprint), "拆解寻宝", ForestComicUiSkin.InkDark, () => _sim.StartBuildOrUpgrade(cap, out _), zoomScale);
                }
                else if (n.state == NodeState.Discovered && _sim.CampLevel >= n.unlockPlayerLv)
                {
                    CampNode cap = n;
                    float bob = Mathf.Sin(Time.time * 5.2f) * 3.5f;
                    TryBindBubble(ref used, ForestSliceView.Iso(n.gx, n.gy) + new Vector3(0f, 0.62f, 0f), new Vector2(0f, bob),
                        new Vector2(122f, 46f), true, ForestComicUiSkin.GetArt("ico_hammer"), "可建造", ForestComicUiSkin.AccentGreen, () => OpenBuildConfirmCard(cap), zoomScale);
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
                                new Vector2(118f, 46f), true, ForestComicUiSkin.ResIcon(SurvRes.Veggie), $"收割({ripe})", ForestComicUiSkin.AccentGreen,
                                () => _sim.HarvestAllRipeFarmPlots(out _), zoomScale);
                        }
                        else if (empty > 0)
                        {
                            TryBindBubble(ref used, ForestSliceView.Iso(n.gx, n.gy) + new Vector3(0f, 0.42f, 0f), Vector2.zero,
                                new Vector2(118f, 46f), true, ForestComicUiSkin.ResIcon(SurvRes.SeedVeggie), $"选种({empty})", ForestComicUiSkin.InkDark,
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
                                new Vector2(124f, 46f), true, ForestComicUiSkin.ResIcon(SurvRes.Pelt), $"收获({rdy})", ForestComicUiSkin.AccentGreen,
                                () => _sim.CollectAllReadyRanchPens(out _), zoomScale);
                        }
                        else if (idl > 0)
                        {
                            TryBindBubble(ref used, ForestSliceView.Iso(n.gx, n.gy) + new Vector3(0f, 0.78f, 0f), Vector2.zero,
                                new Vector2(118f, 46f), true, ForestComicUiSkin.ResIcon(SurvRes.Veggie), $"投喂({idl})", ForestComicUiSkin.InkDark,
                                OpenRanchDrawer, zoomScale);
                        }
                    }
                    else if (n.storedAmount > 0)
                    {
                        TryBindBubble(ref used, ForestSliceView.Iso(n.gx, n.gy) + new Vector3(0f, 0.78f, 0f), Vector2.zero,
                            new Vector2(104f, 46f), true, ForestComicUiSkin.ResIcon(n.storedRes), $"+{n.storedAmount}", ForestComicUiSkin.InkDark, () => HandleNodeClicked(cap), zoomScale);
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
                    new Vector2(bWidth, 50f), true, sIco, sLbl, ForestComicUiSkin.AccentGreen,
                    () => _world.QueueGroundPile(capPile), zoomScale);
            }

            // 1:1《疯狂水世界》居民需求纯白气泡：挂载在避难居民头顶，清爽展示 1~2 个图标 + 真实 have/need（充足 InkDark，不足 AccentRed，零黑描边！）
            Vector2 ordVilPos0 = new Vector2(-999f, -999f);
            Vector2 ordVilPos1 = new Vector2(-999f, -999f);
            if (_sim.ActiveOrders.Count > 0 && used < _worldBubbles.Count)
            {
                if (_world.CivilianVillagers.Count > 0)
                {
                    int maxVilBubbles = Mathf.Min(_sim.ActiveOrders.Count, 2);
                    int boundCount = 0;
                    for (int oi = 0; oi < _sim.ActiveOrders.Count && boundCount < maxVilBubbles && used < _worldBubbles.Count; oi++)
                    {
                        ResidentDemand od = _sim.ActiveOrders[oi];
                        int vi = (od.walkerIndex + oi) % _world.CivilianVillagers.Count;
                        var v = _world.CivilianVillagers[vi];
                        if (_world.IsGridCoveredByForest(v.gx, v.gy)) continue;
                        Vector2 vp = new Vector2(v.gx, v.gy);
                        if (boundCount == 1 && Vector2.Distance(vp, ordVilPos0) < 2.2f) continue;

                        if (TryBindOrderDemandBubble(ref used, ForestSliceView.Iso(v.gx, v.gy) + new Vector3(0f, 0.68f, 0f), od, zoomScale))
                        {
                            if (boundCount == 0) ordVilPos0 = vp;
                            else ordVilPos1 = vp;
                            boundCount++;
                        }
                    }
                }
                else if (_world.Walkers.Count > 0 && _sim.ActiveDemand != null)
                {
                    int wi = Mathf.Clamp(_sim.ActiveDemand.walkerIndex, 0, _world.Walkers.Count - 1);
                    var w = _world.Walkers[wi];
                    if (!_world.IsGridCoveredByForest(w.gx, w.gy))
                        TryBindOrderDemandBubble(ref used, ForestSliceView.Iso(w.gx, w.gy) + new Vector3(0f, 0.70f, 0f), _sim.ActiveDemand, zoomScale);
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
                        new Vector2(122f, 46f), true, ForestComicUiSkin.ResIcon(SurvRes.Wood), "捡原木", ForestComicUiSkin.InkDark,
                        () => _world.QueueGroundPile(capPile), zoomScale);
                }
            }

            // 普通居民日常烤火感谢闲聊气泡（自动避让头顶挂有需求气泡的居民，确保零重叠！）
            for (int i = 0; i < _world.CivilianVillagers.Count && used < _worldBubbles.Count; i++)
            {
                var v = _world.CivilianVillagers[i];
                if (_world.IsGridCoveredByForest(v.gx, v.gy)) continue;
                Vector2 vp = new Vector2(v.gx, v.gy);
                if (Vector2.Distance(vp, ordVilPos0) < 1.8f || Vector2.Distance(vp, ordVilPos1) < 1.8f) continue;
                if (_cam.orthographicSize < 7.2f && v.chatterTimer > 0f && !string.IsNullOrEmpty(v.chatterText))
                {
                    TryBindBubble(ref used, ForestSliceView.Iso(v.gx, v.gy) + new Vector3(0f, 0.66f, 0f), Vector2.zero,
                        new Vector2(216f, 48f), true, null, v.chatterText, ForestComicUiSkin.InkDark, null, zoomScale);
                }
            }

            for (int i = 0; i < _world.Walkers.Count && used < _worldBubbles.Count; i++)
            {
                var w = _world.Walkers[i];
                if (_world.IsGridCoveredByForest(w.gx, w.gy)) continue;
                if (_cam.orthographicSize < 6.8f && w.chatterTimer > 0f && !string.IsNullOrEmpty(w.chatterText))
                {
                    TryBindBubble(ref used, ForestSliceView.Iso(w.gx, w.gy) + new Vector3(0f, 0.68f, 0f), Vector2.zero,
                        new Vector2(198f, 48f), true, null, w.chatterText, ForestComicUiSkin.InkDark, null, zoomScale);
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

        bool TryBindOrderDemandBubble(ref int used, Vector3 worldPos, ResidentDemand d, float scale)
        {
            if (d == null || used >= _worldBubbles.Count) return false;
            Vector2 cp = WorldToCanvas(worldPos);
            if (cp.x < -345f || cp.x > 345f || cp.y < -490f || cp.y > 515f) return false;

            bool hasTwo = d.amt2 > 0;
            Vector2 size = hasTwo ? new Vector2(126f, 68f) : new Vector2(78f, 68f);
            int have1 = _sim.Get(d.res1);
            string s1 = $"{have1}/{d.amt1}";
            Color c1 = have1 >= d.amt1 ? (Color)ForestComicUiSkin.InkDark : (Color)ForestComicUiSkin.AccentRed;

            TrackBubble tb = _worldBubbles[used++];
            tb.rt.gameObject.SetActive(true);
            tb.rt.localScale = Vector3.one * scale;
            tb.rt.sizeDelta = size;
            tb.rt.anchoredPosition = cp;
            tb.bg.enabled = true;
            tb.bg.raycastTarget = true;
            tb.icon.raycastTarget = false;
            tb.onClick = OpenResidentDemandDrawer;
            if (tb.btn != null)
            {
                tb.btn.interactable = true;
                tb.btn.targetGraphic = tb.bg;
            }

            tb.label.enabled = false;
            tb.icon.gameObject.SetActive(true);
            tb.icon.sprite = ForestComicUiSkin.ResIcon(d.res1);
            tb.subTxt1.gameObject.SetActive(true);
            tb.subTxt1.text = s1;
            tb.subTxt1.color = c1;

            if (hasTwo)
            {
                int have2 = _sim.Get(d.res2);
                string s2 = $"{have2}/{d.amt2}";
                Color c2 = have2 >= d.amt2 ? (Color)ForestComicUiSkin.InkDark : (Color)ForestComicUiSkin.AccentRed;

                tb.icon.rectTransform.anchoredPosition = new Vector2(-27f, 11f);
                tb.icon.rectTransform.sizeDelta = new Vector2(32f, 32f);
                tb.subTxt1.rectTransform.anchoredPosition = new Vector2(-27f, -12f);

                tb.icon2.gameObject.SetActive(true);
                tb.icon2.sprite = ForestComicUiSkin.ResIcon(d.res2);
                tb.icon2.rectTransform.anchoredPosition = new Vector2(27f, 11f);
                tb.icon2.rectTransform.sizeDelta = new Vector2(32f, 32f);

                tb.subTxt2.gameObject.SetActive(true);
                tb.subTxt2.text = s2;
                tb.subTxt2.color = c2;
                tb.subTxt2.rectTransform.anchoredPosition = new Vector2(27f, -12f);
                tb.label.text = $"{s1} {s2}";
            }
            else
            {
                tb.icon.rectTransform.anchoredPosition = new Vector2(0f, 11f);
                tb.icon.rectTransform.sizeDelta = new Vector2(34f, 34f);
                tb.subTxt1.rectTransform.anchoredPosition = new Vector2(0f, -12f);
                tb.icon2.gameObject.SetActive(false);
                tb.subTxt2.gameObject.SetActive(false);
                tb.label.text = s1;
            }
            return true;
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

            if (tb.icon2 != null) tb.icon2.gameObject.SetActive(false);
            if (tb.subTxt1 != null) tb.subTxt1.gameObject.SetActive(false);
            if (tb.subTxt2 != null) tb.subTxt2.gameObject.SetActive(false);
            tb.label.enabled = true;

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
                (_bagInventoryModal != null && _bagInventoryModal.activeSelf) ||
                (_heroDetailModal != null && _heroDetailModal.activeSelf) ||
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
                        if (_sim.Get(SurvRes.HeroExp) >= _sim.GetHeroLevelUpExpCost(h.level))
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
                            _uiGlove.ShowAt(GetUiGloveTarget(_rightOrderEntryRt, new Vector2(277f, 480f), new Vector2(-10f, -6f)), dt);
                            return;

                        case ForestSurvivalSim.QuestActionKind.OpenHeroGrowth:
                            _worldGlove.Hide();
                            _uiGlove.ShowAt(GetUiGloveTarget(_dockBtns[2] != null ? _dockBtns[2].GetComponent<RectTransform>() : null, new Vector2(-59f, -582f)), dt);
                            return;

                        case ForestSurvivalSim.QuestActionKind.OpenHeroDelegation:
                            _worldGlove.Hide();
                            _uiGlove.ShowAt(GetUiGloveTarget(_dockBtns[4] != null ? _dockBtns[4].GetComponent<RectTransform>() : null, new Vector2(178f, -582f)), dt);
                            return;

                        case ForestSurvivalSim.QuestActionKind.OpenCaravan:
                        {
                            if (_caravanEntryRt != null && _caravanEntryRt.gameObject.activeSelf && _sim.RadioRepaired)
                            {
                                _worldGlove.Hide();
                                _uiGlove.ShowAt(GetUiGloveTarget(_caravanEntryRt, new Vector2(282f, 362f), new Vector2(-8f, -4f)), dt);
                                return;
                            }
                            CampNode rn = _sim.Find("radio_post");
                            if (rn != null && rn.state != NodeState.HiddenInForest && !_world.IsGridCoveredByForest(rn.gx, rn.gy))
                            {
                                Vector2 rp = WorldToCanvas(ForestSliceView.Iso(rn.gx, rn.gy) + new Vector3(0f, 0.55f, 0f));
                                if (Mathf.Abs(rp.x) < 330f && rp.y > -460f && rp.y < 540f)
                                {
                                    _uiGlove.Hide();
                                    _worldGlove.ShowAt(rp, dt);
                                    return;
                                }
                            }
                            if (_caravanEntryRt != null && _caravanEntryRt.gameObject.activeSelf)
                            {
                                _worldGlove.Hide();
                                _uiGlove.ShowAt(GetUiGloveTarget(_caravanEntryRt, new Vector2(282f, 362f), new Vector2(-8f, -4f)), dt);
                                return;
                            }
                            _worldGlove.Hide();
                            _uiGlove.ShowAt(GetUiGloveTarget(_questCardBg != null ? _questCardBg.rectTransform : null, new Vector2(-130f, -482f)), dt);
                            return;
                        }

                        case ForestSurvivalSim.QuestActionKind.OpenCityRun:
                            _worldGlove.Hide();
                            _uiGlove.ShowAt(GetUiGloveTarget(_dockBtns[5] != null ? _dockBtns[5].GetComponent<RectTransform>() : null, new Vector2(297f, -582f)), dt);
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
