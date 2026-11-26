using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Mori.ForestSlice.ForestSurvivalSim;
using static Mori.ForestSlice.ForestTacticalArena;

namespace Mori.ForestSlice
{
    public sealed partial class ForestSurvivalHud : MonoBehaviour
    {
        // =====================================================================
        // 轻量自治 UI 动效组件（放射光旋转 / 礼袋与挂机箱呼吸晃动 / 弹窗果冻弹出）
        // =====================================================================
        sealed class WwOrdersUiAnimator : MonoBehaviour
        {
            public float spinDegPerSec;
            public bool bobActive;
            public float bobAmpY = 5f;
            public float bobTiltDeg = 5.5f;
            public Vector2 basePos;
            public bool popOnEnable;

            RectTransform _rt;
            float _popElapsed = 99f;

            void Awake()
            {
                _rt = GetComponent<RectTransform>();
            }

            void OnEnable()
            {
                if (_rt == null) _rt = GetComponent<RectTransform>();
                if (Application.isPlaying && popOnEnable && _rt != null)
                {
                    _popElapsed = 0f;
                    _rt.localScale = Vector3.one * 0.84f;
                }
            }

            void Update()
            {
                if (_rt == null) return;
                float dt = Mathf.Min(0.08f, Mathf.Max(Time.deltaTime, Time.unscaledDeltaTime));
                float t = Time.unscaledTime;

                if (Mathf.Abs(spinDegPerSec) > 0.01f)
                    _rt.Rotate(0f, 0f, spinDegPerSec * dt);

                if (popOnEnable && _popElapsed < 0.26f)
                {
                    _popElapsed += dt;
                    float u = Mathf.Clamp01(_popElapsed / 0.26f);
                    float s = u < 0.68f
                        ? Mathf.Lerp(0.84f, 1.05f, u / 0.68f)
                        : Mathf.Lerp(1.05f, 1.0f, (u - 0.68f) / 0.32f);
                    _rt.localScale = new Vector3(s, s, 1f);
                }

                if (bobActive)
                {
                    float wave = Mathf.Sin(t * 5.8f);
                    float hop = Mathf.Abs(Mathf.Sin(t * 2.9f)) * bobAmpY;
                    _rt.anchoredPosition = basePos + new Vector2(0f, hop);
                    _rt.localRotation = Quaternion.Euler(0f, 0f, wave * bobTiltDeg);
                    float pulse = 1f + Mathf.Sin(t * 5.8f) * 0.04f;
                    _rt.localScale = new Vector3(pulse, pulse, 1f);
                }
                else if (Mathf.Abs(spinDegPerSec) < 0.01f && !popOnEnable)
                {
                    _rt.anchoredPosition = basePos;
                    _rt.localRotation = Quaternion.identity;
                    _rt.localScale = Vector3.one;
                }
            }
        }

        struct PendingChestRewardBundle
        {
            public int coins;
            public int blueprints;
            public int recruitTickets;
            public int choiceBoxes;
        }

        struct CongratsDisplayItem
        {
            public SurvRes res;
            public int count;
            public Sprite icon;
            public string label;
            public int rarity0to5;
            public string ownSummary;
            public string desc;
        }

        struct PosterRewardEntry
        {
            public Sprite icon;
            public int rarity0to5;
            public string countText;
            public string title;
            public string ownText;
            public string desc;
            public Action onGo;
        }

        // ---------------- 水世界居民需求半屏抽屉与 5 单礼袋私有状态 ----------------
        int _wwSelectedOrderTab;
        int _wwPendingOrderChests;
        PendingChestRewardBundle _wwLastChestBundle;
        WwOrdersUiAnimator _wwGiftBagAnim;
        GameObject _wwGiftBagDotGo;

        readonly GameObject[] _wwOrdTabRoots = new GameObject[4];
        readonly Button[] _wwOrdTabBtns = new Button[4];
        readonly GameObject[] _wwOrdTabSelFrames = new GameObject[4];
        readonly Image[] _wwOrdTabBusts = new Image[4];
        readonly Text[] _wwOrdTabNames = new Text[4];
        readonly Outline[] _wwOrdTabNameOutlines = new Outline[4];
        readonly GameObject[] _wwOrdTabReadyDots = new GameObject[4];

        Text _wwOrdFlavorTxt;
        Text _wwOrdRwXpTxt, _wwOrdRwCoinTxt, _wwOrdRwBpTxt;
        GameObject _wwOrdRwBpGroup;
        readonly GameObject[] _wwOrdReqItems = new GameObject[3];
        readonly RectTransform[] _wwOrdReqRts = new RectTransform[3];
        readonly Button[] _wwOrdReqBtns = new Button[3];
        readonly Image[] _wwOrdReqIcons = new Image[3];
        readonly Text[] _wwOrdReqCntTxts = new Text[3];
        Button _wwOrdMainActionBtn;
        Image _wwOrdMainActionImg;
        Text _wwOrdMainActionLbl;

        // ---------------- 全屏【恭喜获得】高光爆奖遮罩私有状态（支持 1~10 连抽网格） ----------------
        const int MaxCongratsTiles = 10;
        GameObject _wwCongratsModal;
        Text _wwCgBannerFallbackTxt;
        Text _wwCgSubtitleTxt;
        readonly GameObject[] _wwCgTileRoots = new GameObject[MaxCongratsTiles];
        readonly RectTransform[] _wwCgTileRts = new RectTransform[MaxCongratsTiles];
        readonly Image[] _wwCgTileFrames = new Image[MaxCongratsTiles];
        readonly Image[] _wwCgTileIcons = new Image[MaxCongratsTiles];
        readonly Text[] _wwCgTileCounts = new Text[MaxCongratsTiles];
        readonly Text[] _wwCgTileLabels = new Text[MaxCongratsTiles];
        readonly GameObject[] _wwCgTileBrackets = new GameObject[MaxCongratsTiles];
        GameObject _wwCgDetailSheet;
        Image _wwCgDetailFrame;
        Image _wwCgDetailIcon;
        Text _wwCgDetailNameTxt;
        Text _wwCgDetailOwnTxt;
        Text _wwCgDetailDescTxt;
        CongratsDisplayItem[] _wwCgCurrentItems;
        int _wwCgSelectedIdx;

        // ---------------- 短波电台双页签：[广播招募] + [山口行商] ----------------
        int _wwRadioStationTab; // 0 = 广播招募, 1 = 山口行商
        Text _wwRadioModalHeaderTxt;
        GameObject _wwRadioGachaPage, _wwCaravanTradePage;
        readonly Image[] _wwRadioTabImgs = new Image[2];
        readonly Text[] _wwRadioTabLbls = new Text[2];
        readonly Outline[] _wwRadioTabOutlines = new Outline[2];
        GameObject _wwRadioTab0Dot;

        // Tab 0: [广播招募]
        Text _wwGachaTicketOwnTxt, _wwGachaTotalPullsTxt, _wwGachaPityTxt;
        Image _wwGachaPityFill;
        readonly Image[] _wwGachaFeatCards = new Image[5];
        readonly Image[] _wwGachaFeatBusts = new Image[5];
        readonly Image[] _wwGachaFeatFactions = new Image[5];
        readonly Text[] _wwGachaFeatStates = new Text[5];
        Image _wwGachaPull1BtnImg, _wwGachaPull10BtnImg;
        Text _wwGachaPull1Lbl, _wwGachaPull10Lbl;

        // Tab 1: [山口行商] + 2 项特供补给兑换
        readonly Image[] _wwCvRarityFrames = new Image[3];
        readonly Text[] _wwCvPackCntTxts = new Text[3];
        Image _wwCvExTicketBtnImg, _wwCvExBookBtnImg;
        Text _wwCvExTicketNeedTxt, _wwCvExTicketBtnLbl;
        Text _wwCvExBookNeedTxt, _wwCvExBookBtnLbl;

        // ---------------- 战前海报巡逻挂机箱 & 奖励详情卡 & 战术粉笔黑板布阵引用 ----------------
        Text _wwLvBannerFallbackTxt;
        Text _wwLvSubTitleTxt;

        readonly Image[] _wwSpLootFrames = new Image[4];
        readonly Button[] _wwSpLootBtns = new Button[4];
        readonly PosterRewardEntry[] _wwSpLootEntries = new PosterRewardEntry[4];
        readonly GameObject[] _wwSpEmptyBrackets = new GameObject[5];

        // 哨卡巡逻挂机箱
        WwOrdersUiAnimator _wwPatrolChestAnim;
        GameObject _wwPatrolChestDotGo;
        Text _wwPatrolStatusTxt, _wwPatrolYieldTxt, _wwPatrolClaimLbl;
        Image _wwPatrolClaimBtnImg;

        // 海报战利品点击详情浮窗 (_wwPosterItemPopup — 1:1 Ref media_1795609734848)
        GameObject _wwPosterItemPopup;
        Image _wwPosterPopCard, _wwPosterPopIcon;
        Text _wwPosterPopNameTxt, _wwPosterPopOwnTxt, _wwPosterPopDescTxt;
        Action _wwPosterPopGoCb;

        // 战术粉笔黑板九宫格布阵 (_tacticalFormationModal — 1:1 Ref media_1795609719722 & e8e8c63a)
        int _wwTfFactionFilterIdx; // 0=所有, 1=铁卫团, 2=山林盟, 3=废土科工
        readonly Image[] _wwTfFilterTabImgs = new Image[4];
        readonly Text[] _wwTfFilterTabLbls = new Text[4];
        readonly Outline[] _wwTfFilterTabOutlines = new Outline[4];
        readonly GameObject[] _wwTfTileYellowBorders = new GameObject[9];
        readonly GameObject[] _wwTfTileBrackets = new GameObject[9];
        readonly Image[] _wwTfTileFactionIcons = new Image[9];
        readonly Image[] _wwTfTileShadows = new Image[9];
        readonly WwHeroIdleBreather[] _wwTfTileBreathers = new WwHeroIdleBreather[9];
        RectTransform _wwTfRosterScrollContentRt;
        readonly Image[] _wwTfRosterPortraitFrames = new Image[MaxHeroUiSlots];
        readonly Image[] _wwTfRosterFactionIcons = new Image[MaxHeroUiSlots];
        readonly Image[] _wwTfRosterRoleIcons = new Image[MaxHeroUiSlots];
        readonly GameObject[] _wwTfRosterGreenChecks = new GameObject[MaxHeroUiSlots];
        readonly GameObject[] _wwTfRosterBrackets = new GameObject[MaxHeroUiSlots];

        // 左下圆形 [阵营光环] 徽章 & 说明浮窗 + 右下 [当前阵型] 芯片 & 6阵型选择浮窗
        Image _wwTfAuraCircleBg, _wwTfAuraCrestImg;
        Text _wwTfAuraStateTxt, _wwTfAuraSubLbl;
        GameObject _wwFactionAuraPopup;
        Text _wwAuraPopupHdrTxt, _wwAuraPopupCountTxt;
        readonly Text[] _wwAuraPopupTierTxts = new Text[3];

        Image _wwTfCurFmtChipIcon;
        Text _wwTfCurFmtChipNameTxt;
        GameObject _wwFormationPickerPopup;
        Text _wwFmtPopupGlobalPctTxt;
        readonly Image[] _wwFmtRowBgs = new Image[6];
        readonly Image[] _wwFmtRowIcons = new Image[6];
        readonly Text[] _wwFmtRowNames = new Text[6];
        readonly Text[] _wwFmtRowCollectTxts = new Text[6];
        readonly Text[] _wwFmtRowSlotDescs = new Text[6];
        readonly Image[] _wwFmtRowBtnImgs = new Image[6];
        readonly Text[] _wwFmtRowBtnLbls = new Text[6];

        // 战场 HUD & 结算引用
        readonly Image[] _wwWcPortraitFrames = new Image[5];
        readonly GameObject[] _wwWcUltBrackets = new GameObject[5];
        Text _wwArBannerFallbackTxt;
        Image _wwArMvpRarityFrame;
        readonly GameObject[] _wwArLootTiles = new GameObject[4];
        readonly Image[] _wwArLootFrames = new Image[4];
        readonly Image[] _wwArLootIcons = new Image[4];
        readonly Text[] _wwArLootCounts = new Text[4];

        internal void CloseOrdersAndCombatSubPopups()
        {
            if (_wwPosterItemPopup != null) _wwPosterItemPopup.SetActive(false);
            if (_wwFactionAuraPopup != null) _wwFactionAuraPopup.SetActive(false);
            if (_wwFormationPickerPopup != null) _wwFormationPickerPopup.SetActive(false);
        }

        // ---------------- 水世界通用按钮与描边工具 ----------------
        static Button MakeWwBackRedButton(Transform parent, UnityAction onClick, Vector2 pos, Vector2 size)
        {
            Sprite sp = ForestComicUiSkin.GetUiArt("ww_btn_back_red", ForestComicUiSkin.BtnRed);
            RectTransform rt = ForestUiWidgets.MakeRect("WwBtnBackRed", parent, Vector2.zero, Vector2.zero, Vector2.zero, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sp;
            img.preserveAspect = true;
            img.type = Image.Type.Simple;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var cols = btn.colors;
            cols.highlightedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
            cols.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            btn.colors = cols;
            if (onClick != null) btn.onClick.AddListener(onClick);
            return btn;
        }

        static bool HasRawForestArtAsset(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return Resources.Load<Sprite>("ForestArt/" + key) != null ||
                   Resources.Load<Texture2D>("ForestArt/" + key) != null;
        }

        static void SetOutlineEnabled(Text txt, bool enabled)
        {
            if (txt == null) return;
            var ol = txt.GetComponent<Outline>();
            if (ol != null) ol.enabled = enabled;
        }

        static string FormatVillagerTabShortName(string rawName, int fallbackIdx)
        {
            string[] defaults = { "拾荒老周", "采药桂婶", "巡山阿木", "织布小苗" };
            if (string.IsNullOrEmpty(rawName))
                return defaults[Mathf.Clamp(fallbackIdx, 0, defaults.Length - 1)];
            string clean = rawName.Replace("·", "").Replace(" ", "");
            if (clean.Contains("木工学徒阿顺")) return "巡山阿顺";
            if (clean.Contains("拾荒少女小苗")) return "织布小苗";
            if (clean.Length > 5) clean = clean.Substring(clean.Length - 4);
            return clean;
        }

        // =====================================================================
        // 1. 居民需求半屏抽屉 + 【今日满足需求】红礼袋进度栏
        // =====================================================================
        void BuildResidentDemandDrawer()
        {
            _residentDrawer = ForestUiWidgets.Stretch("ResidentDemandRoot", _canvasRt).gameObject;

            Image topDismiss = ForestUiWidgets.MakeSlicedImage("TopDismiss", _residentDrawer.transform, null,
                new Color(0.06f, 0.09f, 0.20f, 0.14f),
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true);
            var dismissBtn = topDismiss.gameObject.AddComponent<Button>();
            dismissBtn.transition = Selectable.Transition.None;
            dismissBtn.onClick.AddListener(CloseAllModals);

            RectTransform milestoneRoot = ForestUiWidgets.MakeRect("MilestoneBarRoot", _residentDrawer.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 696f), new Vector2(704f, 76f));

            ForestUiWidgets.MakeIcon("SmileyBadge", milestoneRoot,
                ForestComicUiSkin.GetUiArt("ww_smiley_morale"),
                new Vector2(-312f, 0f), new Vector2(64f, 64f));

            ForestUiWidgets.MakeText("MilestoneLbl", milestoneRoot, "今日满足需求:", 21, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(-270f, 17f), new Vector2(240f, 30f), outline: true);

            Image moodBg = ForestUiWidgets.MakeSlicedImage("MoodBar", milestoneRoot, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(-272f, -15f), new Vector2(288f, 28f), false);
            RectTransform mf = ForestUiWidgets.MakeRect("Fill", moodBg.transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6f, -6f));
            _resMoodFill = mf.gameObject.AddComponent<Image>();
            _resMoodFill.sprite = ForestComicUiSkin.BtnOrange;
            _resMoodFill.color = new Color32(255, 214, 62, 255);
            _resMoodFill.type = Image.Type.Filled;
            _resMoodFill.fillMethod = Image.FillMethod.Horizontal;
            _resMoodFill.raycastTarget = false;

            _resMoodTxt = ForestUiWidgets.MakeText("MoodTxt", moodBg.transform, "0/5", 17, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

            RectTransform giftRt = ForestUiWidgets.MakeRect("OrderGiftBoxBtn", milestoneRoot,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(54f, 4f), new Vector2(84f, 84f));
            var giftImg = giftRt.gameObject.AddComponent<Image>();
            giftImg.sprite = ForestComicUiSkin.GetUiArt("ww_order_gift_box");
            giftImg.preserveAspect = true;
            var giftBtn = giftRt.gameObject.AddComponent<Button>();
            giftBtn.targetGraphic = giftImg;
            giftBtn.onClick.AddListener(OnOrderGiftBoxClicked);

            _wwGiftBagAnim = giftRt.gameObject.AddComponent<WwOrdersUiAnimator>();
            _wwGiftBagAnim.basePos = new Vector2(54f, 4f);
            _wwGiftBagAnim.bobAmpY = 5.5f;
            _wwGiftBagAnim.bobTiltDeg = 6.5f;

            Image giftDot = ForestUiWidgets.MakeIcon("ClaimRedDot", giftRt, ForestComicUiSkin.RedDotBadge,
                new Vector2(26f, 26f), new Vector2(30f, 30f));
            ForestUiWidgets.MakeText("Bang", giftDot.transform, "!", 20, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: false);
            _wwGiftBagDotGo = giftDot.gameObject;
            _wwGiftBagDotGo.SetActive(false);

            ForestUiWidgets.MakeJellyButton("BtnRefreshOrders", milestoneRoot, ForestComicUiSkin.DarkPill, "换一批", 16,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, -2f), new Vector2(128f, 44f), () =>
                {
                    if (!_sim.RefreshAllOrders())
                        SpawnScreenCenterBanner($"换一批冷却中（还剩 {Mathf.CeilToInt(_sim.OrderRefreshCooldown)} 秒）");
                    else
                        _wwSelectedOrderTab = 0;
                    RefreshResidentDrawer();
                }, out _resRefreshBtnLbl);

            Image sheet = ForestUiWidgets.MakeSlicedImage("BottomSheet", _residentDrawer.transform,
                ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                Vector2.zero, new Vector2(720f, 660f), true);

            Image orderSheet = ForestUiWidgets.MakeSlicedImage("SelectedOrderSheet", sheet.transform,
                ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -148f), new Vector2(704f, 396f), false);

            RectTransform tabsStrip = ForestUiWidgets.MakeRect("VillagerTabsStrip", sheet.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -12f), new Vector2(688f, 144f));

            for (int i = 0; i < 4; i++)
            {
                int tabIdx = i;
                float tx = -246f + i * 136f;
                RectTransform tabRt = ForestUiWidgets.MakeRect("VillagerTab_" + i, tabsStrip,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(tx, 0f), new Vector2(118f, 128f));
                _wwOrdTabRoots[i] = tabRt.gameObject;

                var tabHit = tabRt.gameObject.AddComponent<Image>();
                tabHit.color = new Color(0f, 0f, 0f, 0.01f);
                var tabBtn = tabRt.gameObject.AddComponent<Button>();
                tabBtn.targetGraphic = tabHit;
                tabBtn.onClick.AddListener(() =>
                {
                    _wwSelectedOrderTab = tabIdx;
                    RefreshResidentDrawer();
                });
                _wwOrdTabBtns[i] = tabBtn;

                Image selBridge = ForestUiWidgets.MakeSlicedImage("SelLightBridge", tabRt,
                    ForestComicUiSkin.WwLightSheet, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, 4f), new Vector2(124f, 148f), false);
                ForestUiWidgets.MakeSlicedImage("SeamlessJoin", selBridge.transform,
                    null, new Color32(240, 243, 251, 255),
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                    Vector2.zero, new Vector2(-6f, 14f), false);
                _wwOrdTabSelFrames[i] = selBridge.gameObject;

                Image portraitOuter = ForestUiWidgets.MakeSlicedImage("PortraitOuter", tabRt,
                    ForestComicUiSkin.DialogOuter, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -4f), new Vector2(94f, 94f), false);
                Image portraitBg = ForestUiWidgets.MakeSlicedImage("PortraitBg", portraitOuter.transform,
                    null, new Color32(106, 118, 204, 255),
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8f, -8f), false);
                portraitBg.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                _wwOrdTabBusts[i] = ForestUiWidgets.MakeIcon("Bust", portraitBg.transform,
                    ForestComicUiSkin.GetArt("bust_vil_" + i, ForestComicUiSkin.GetArt("bust_cook")),
                    new Vector2(0f, -5f), new Vector2(94f, 94f));

                Text nameTxt = ForestUiWidgets.MakeText("VilName", tabRt, "拾荒老周", 17, Color.white, TextAnchor.MiddleCenter,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 2f), new Vector2(8f, 26f), outline: true);
                _wwOrdTabNames[i] = nameTxt;
                _wwOrdTabNameOutlines[i] = nameTxt.GetComponent<Outline>();

                Image readyBadge = ForestUiWidgets.MakeIcon("ReadyDot", tabRt,
                    ForestComicUiSkin.GetUiArt("ww_check_green"),
                    new Vector2(42f, 48f), new Vector2(28f, 28f));
                _wwOrdTabReadyDots[i] = readyBadge.gameObject;
                if (i < 3) _ordBusts[i] = _wwOrdTabBusts[i];
            }

            _wwOrdFlavorTxt = ForestUiWidgets.MakeText("FlavorQuote", orderSheet.transform,
                "哎，饥寒交迫的山谷逃难生活什么时候才是尽头。", 20, ForestComicUiSkin.InkMuted, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -20f), new Vector2(-64f, 58f), outline: false);
            SetOutlineEnabled(_wwOrdFlavorTxt, false);
            _wwOrdFlavorTxt.lineSpacing = 1.2f;

            RectTransform rwStrip = ForestUiWidgets.MakeRect("RewardRow", orderSheet.transform,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-34f, -74f), new Vector2(320f, 68f));

            Text rwLbl = ForestUiWidgets.MakeText("RwLabel", rwStrip, "奖励:", 19, ForestComicUiSkin.InkMuted, TextAnchor.MiddleRight,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(118f, -4f), new Vector2(84f, 30f), outline: false);
            SetOutlineEnabled(rwLbl, false);

            RectTransform xpGrp = ForestUiWidgets.MakeRect("XpBadge", rwStrip,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(162f, 0f), new Vector2(64f, 66f));
            ForestUiWidgets.MakeIcon("StarIco", xpGrp, ForestComicUiSkin.GetUiArt("ww_xp_star"),
                new Vector2(0f, 12f), new Vector2(42f, 42f));
            _wwOrdRwXpTxt = ForestUiWidgets.MakeText("XpVal", xpGrp, "34", 20, ForestComicUiSkin.InkDark, TextAnchor.LowerCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -2f), Vector2.zero, outline: false);
            SetOutlineEnabled(_wwOrdRwXpTxt, false);

            RectTransform coinGrp = ForestUiWidgets.MakeRect("CoinBadge", rwStrip,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(236f, 0f), new Vector2(72f, 66f));
            ForestUiWidgets.MakeIcon("CoinIco", coinGrp, ForestComicUiSkin.GetArt("ico_coin"),
                new Vector2(0f, 12f), new Vector2(40f, 40f));
            _wwOrdRwCoinTxt = ForestUiWidgets.MakeText("CoinVal", coinGrp, "150", 20, ForestComicUiSkin.InkDark, TextAnchor.LowerCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -2f), Vector2.zero, outline: false);
            SetOutlineEnabled(_wwOrdRwCoinTxt, false);

            RectTransform bpGrp = ForestUiWidgets.MakeRect("BpBadge", rwStrip,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(298f, 0f), new Vector2(58f, 66f));
            ForestUiWidgets.MakeIcon("BpIco", bpGrp, ForestComicUiSkin.ResIcon(SurvRes.Blueprint),
                new Vector2(0f, 12f), new Vector2(38f, 38f));
            _wwOrdRwBpTxt = ForestUiWidgets.MakeText("BpVal", bpGrp, "+1", 19, ForestComicUiSkin.InkDark, TextAnchor.LowerCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -2f), Vector2.zero, outline: false);
            SetOutlineEnabled(_wwOrdRwBpTxt, false);
            _wwOrdRwBpGroup = bpGrp.gameObject;

            Image reqBox = ForestUiWidgets.MakeSlicedImage("DemandSubPanel", orderSheet.transform,
                ForestComicUiSkin.WwSubPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -150f), new Vector2(636f, 222f), false);

            Text subHdr = ForestUiWidgets.MakeText("SubHdr", reqBox.transform, "居民需求", 20, ForestComicUiSkin.InkMuted, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -6f), new Vector2(0f, 32f), outline: false);
            SetOutlineEnabled(subHdr, false);

            ForestUiWidgets.MakeSlicedImage("HdrDivider", reqBox.transform, null, ForestComicUiSkin.DividerLine,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -42f), new Vector2(-16f, 2f), false);

            for (int r = 0; r < 3; r++)
            {
                RectTransform itemRt = ForestUiWidgets.MakeRect("ReqItem_" + r, reqBox.transform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2((r - 1) * 190f, -18f), new Vector2(148f, 152f));
                _wwOrdReqItems[r] = itemRt.gameObject;
                _wwOrdReqRts[r] = itemRt;

                var hitImg = itemRt.gameObject.AddComponent<Image>();
                hitImg.color = new Color(1f, 1f, 1f, 0.01f);
                var btn = itemRt.gameObject.AddComponent<Button>();
                btn.targetGraphic = hitImg;
                _wwOrdReqBtns[r] = btn;

                _wwOrdReqIcons[r] = ForestUiWidgets.MakeIcon("Ico", itemRt,
                    ForestComicUiSkin.ResIcon(SurvRes.RoastVeggie),
                    new Vector2(0f, 18f), new Vector2(88f, 88f));

                Text cntTxt = ForestUiWidgets.MakeText("HaveNeed", itemRt, "1/1", 22, ForestComicUiSkin.InkDark, TextAnchor.MiddleCenter,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 8f), new Vector2(0f, 32f), outline: false);
                SetOutlineEnabled(cntTxt, false);
                cntTxt.supportRichText = true;
                _wwOrdReqCntTxts[r] = cntTxt;
            }

            MakeWwBackRedButton(sheet.transform, CloseAllModals, new Vector2(18f, 17f), new Vector2(100f, 74f));

            _wwOrdMainActionBtn = ForestUiWidgets.MakeJellyButton("BtnOrderMainAction", sheet.transform,
                ForestComicUiSkin.BtnBlue, "获取物品", 26,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(248f, 72f),
                () => OnOrderSlotActionClicked(_wwSelectedOrderTab), out _wwOrdMainActionLbl);
            _wwOrdMainActionImg = _wwOrdMainActionBtn.GetComponent<Image>();

            for (int i = 0; i < 3; i++)
            {
                _ordDeliverBtns[i] = _wwOrdMainActionBtn;
                _ordDeliverLbls[i] = _wwOrdMainActionLbl;
            }

            _residentDrawer.SetActive(false);
        }

        public void OpenResidentDemandDrawer()
        {
            _sim.EnsureActiveOrders();
            CloseAllModals();
            if (_wwCongratsModal != null) _wwCongratsModal.SetActive(false);

            int pick = Mathf.Clamp(_wwSelectedOrderTab, 0, Mathf.Max(0, _sim.ActiveOrders.Count - 1));
            if (!_sim.CanSatisfyOrder(pick))
            {
                for (int i = 0; i < _sim.ActiveOrders.Count; i++)
                {
                    if (_sim.CanSatisfyOrder(i))
                    {
                        pick = i;
                        break;
                    }
                }
            }
            _wwSelectedOrderTab = pick;
            _residentDrawer.SetActive(true);
            RefreshResidentDrawer();
        }

        void RefreshResidentDrawer()
        {
            _sim.EnsureActiveOrders();
            int orderCnt = Mathf.Clamp(_sim.ActiveOrders.Count, 1, 4);
            _wwSelectedOrderTab = Mathf.Clamp(_wwSelectedOrderTab, 0, orderCnt - 1);

            bool simDirectReady = _sim.SatisfiedToday >= ForestSurvivalSim.SatisfiedGoal;
            bool chestClaimable = _wwPendingOrderChests > 0 || simDirectReady;
            int shownDone = _wwPendingOrderChests > 0
                ? (ForestSurvivalSim.SatisfiedGoal * _wwPendingOrderChests + _sim.SatisfiedToday)
                : _sim.SatisfiedToday;
            _resMoodFill.fillAmount = chestClaimable
                ? 1f
                : Mathf.Clamp01(_sim.SatisfiedToday / (float)ForestSurvivalSim.SatisfiedGoal);
            _resMoodTxt.text = chestClaimable
                ? $"{shownDone}/{ForestSurvivalSim.SatisfiedGoal}"
                : $"{_sim.SatisfiedToday}/{ForestSurvivalSim.SatisfiedGoal}";

            if (_wwGiftBagDotGo != null) _wwGiftBagDotGo.SetActive(chestClaimable);
            if (_wwGiftBagAnim != null) _wwGiftBagAnim.bobActive = chestClaimable;

            if (_resRefreshBtnLbl != null)
            {
                _resRefreshBtnLbl.text = _sim.OrderRefreshCooldown > 0.1f
                    ? $"冷却 {Mathf.CeilToInt(_sim.OrderRefreshCooldown)}s"
                    : "换一批";
            }

            string[] fallbackKeys = { "bust_vil_0", "bust_vil_1", "bust_vil_2", "bust_vil_3" };
            string[] fallbackNames = { "拾荒老周", "采药桂婶", "织布小苗", "巡山阿顺" };
            var usedNames = new HashSet<string>();
            var usedPortraits = new HashSet<string>();
            for (int i = 0; i < 4; i++)
            {
                bool activeTab = i < orderCnt;
                _wwOrdTabRoots[i].SetActive(activeTab);
                if (!activeTab) continue;

                ResidentDemand td = _sim.ActiveOrders[i];
                bool isSel = (i == _wwSelectedOrderTab);
                bool tabReady = _sim.CanSatisfyOrder(i);

                string shortNm = FormatVillagerTabShortName(td.speakerName, i);
                string pKey = string.IsNullOrEmpty(td.portraitKey) ? fallbackKeys[i % 4] : td.portraitKey;
                if (!usedNames.Add(shortNm) || !usedPortraits.Add(pKey))
                {
                    for (int f = 0; f < 4; f++)
                    {
                        if (!usedNames.Contains(fallbackNames[f]) && !usedPortraits.Contains(fallbackKeys[f]))
                        {
                            shortNm = fallbackNames[f];
                            pKey = fallbackKeys[f];
                            usedNames.Add(shortNm);
                            usedPortraits.Add(pKey);
                            break;
                        }
                    }
                }

                _wwOrdTabSelFrames[i].SetActive(isSel);
                _wwOrdTabBusts[i].sprite = ForestComicUiSkin.GetArt(pKey, ForestComicUiSkin.GetArt(fallbackKeys[i % 4], ForestComicUiSkin.GetArt("bust_cook")));
                _wwOrdTabNames[i].text = shortNm;
                _wwOrdTabNames[i].color = isSel ? (Color)ForestComicUiSkin.InkDark : Color.white;
                if (_wwOrdTabNameOutlines[i] != null)
                    _wwOrdTabNameOutlines[i].enabled = !isSel;
                _wwOrdTabReadyDots[i].SetActive(tabReady);

                if (i < 3)
                    _ordDeliverBtns[i] = isSel ? _wwOrdMainActionBtn : _wwOrdTabBtns[i];
            }

            ResidentDemand d = _sim.ActiveOrders[_wwSelectedOrderTab];
            float chefBonus = _sim.FindHero("laochentou")?.assigned == true ? 1.15f : 1.0f;
            float popMul = _sim.OrderProsperityMultiplier;
            int dispCoins = Mathf.RoundToInt(d.coinReward * chefBonus * popMul);
            int dispXp = Mathf.RoundToInt(d.xpReward * Mathf.Lerp(1f, popMul, 0.5f));

            _wwOrdFlavorTxt.text = string.IsNullOrEmpty(d.complaint)
                ? "哎，山里寒气重，大伙都盼着热乎吃食和结实衣物度日呢。"
                : d.complaint;
            _wwOrdRwXpTxt.text = dispXp.ToString();
            _wwOrdRwCoinTxt.text = dispCoins.ToString();
            bool hasBpBonus = d.blueprintBonus > 0;
            _wwOrdRwBpGroup.SetActive(hasBpBonus);
            if (hasBpBonus)
                _wwOrdRwBpTxt.text = $"+{d.blueprintBonus}";

            var reqList = new List<KeyValuePair<SurvRes, int>>(3);
            if (d.amt1 > 0) reqList.Add(new KeyValuePair<SurvRes, int>(d.res1, d.amt1));
            if (d.amt2 > 0) reqList.Add(new KeyValuePair<SurvRes, int>(d.res2, d.amt2));

            int reqCnt = reqList.Count;
            float spacing = reqCnt >= 3 ? 192f : 216f;
            float startX = -(reqCnt - 1) * spacing * 0.5f;

            for (int r = 0; r < 3; r++)
            {
                bool show = r < reqCnt;
                _wwOrdReqItems[r].SetActive(show);
                if (!show) continue;

                SurvRes res = reqList[r].Key;
                int need = reqList[r].Value;
                int have = _sim.Get(res);

                _wwOrdReqRts[r].anchoredPosition = new Vector2(startX + r * spacing, -18f);
                _wwOrdReqIcons[r].sprite = ForestComicUiSkin.ResIcon(res);
                _wwOrdReqCntTxts[r].color = ForestComicUiSkin.InkDark;
                _wwOrdReqCntTxts[r].text = have >= need
                    ? $"{have}/{need}"
                    : $"<color=#E43A40>{have}</color>/{need}";

                _wwOrdReqBtns[r].onClick.RemoveAllListeners();
                _wwOrdReqBtns[r].onClick.AddListener(() => OpenItemSourceTip(res));
            }

            bool canFulfill = _sim.CanSatisfyOrder(_wwSelectedOrderTab);
            _wwOrdMainActionImg.sprite = canFulfill ? ForestComicUiSkin.BtnGreen : ForestComicUiSkin.BtnBlue;
            _wwOrdMainActionLbl.text = canFulfill ? "满足需求" : "获取物品";
        }

        void OnOrderSlotActionClicked(int slot)
        {
            if (slot < 0 || slot >= _sim.ActiveOrders.Count) return;
            ResidentDemand d = _sim.ActiveOrders[slot];

            if (_sim.CanSatisfyOrder(slot))
            {
                int preSatisfied = _sim.SatisfiedToday;
                float popMul = _sim.OrderProsperityMultiplier;
                Vector2 g = new Vector2(15.6f, 16.0f);
                if (_world != null && _world.CivilianVillagers.Count > 0)
                {
                    int vIdx = Mathf.Clamp(d.walkerIndex, 0, _world.CivilianVillagers.Count - 1);
                    g = new Vector2(_world.CivilianVillagers[vIdx].gx, _world.CivilianVillagers[vIdx].gy);
                }

                if (_sim.TrySatisfyOrder(slot, g, out string thanks))
                {
                    Vector2 btnPos = new Vector2(0f, -560f);
                    Vector2 coinHudTarget = new Vector2(-188f, 569f - _topSafePad);
                    Image flyCoin = ForestUiWidgets.MakeIcon("FlyOrderCoin", _canvasRt,
                        ForestComicUiSkin.GetArt("ico_coin"), btnPos, new Vector2(40f, 40f));
                    _flyIcons.Add(new FlyIconItem
                    {
                        rt = flyCoin.rectTransform,
                        img = flyCoin,
                        from = btnPos,
                        to = coinHudTarget,
                        t = 0f,
                        dur = 0.48f,
                        targetRes = SurvRes.Wood
                    });

                    if (_world != null && _world.CivilianVillagers.Count > 0)
                    {
                        int vIdx = Mathf.Clamp(d.walkerIndex, 0, _world.CivilianVillagers.Count - 1);
                        _world.CivilianVillagers[vIdx].chatterText = thanks;
                        _world.CivilianVillagers[vIdx].chatterTimer = 3.8f;
                        _world.CivilianVillagers[vIdx].cheerTimer = 1.6f;
                    }

                    if (preSatisfied + 1 >= ForestSurvivalSim.SatisfiedGoal)
                    {
                        _wwPendingOrderChests++;
                        _wwLastChestBundle = new PendingChestRewardBundle
                        {
                            coins = Mathf.RoundToInt(260 * popMul),
                            blueprints = 2,
                            recruitTickets = 1,
                            choiceBoxes = 1,
                        };
                        SpawnScreenCenterBanner("★ 今日满足满 5 单！点击上方抖动【红补给礼袋】开启大奖！");
                    }

                    if (!_sim.CanSatisfyOrder(_wwSelectedOrderTab))
                    {
                        for (int i = 0; i < _sim.ActiveOrders.Count; i++)
                        {
                            if (_sim.CanSatisfyOrder(i))
                            {
                                _wwSelectedOrderTab = i;
                                break;
                            }
                        }
                    }

                    RefreshResidentDrawer();
                }
            }
            else
            {
                SurvRes missing = _sim.Get(d.res1) < d.amt1 ? d.res1 : d.res2;
                JumpToProduceOrGather(missing);
            }
        }

        void OnOrderGiftBoxClicked()
        {
            bool simDirectReady = _sim.SatisfiedToday >= ForestSurvivalSim.SatisfiedGoal;
            if (_wwPendingOrderChests <= 0 && !simDirectReady)
            {
                int needMore = Mathf.Max(1, ForestSurvivalSim.SatisfiedGoal - _sim.SatisfiedToday);
                int estCoins = Mathf.RoundToInt(260 * _sim.OrderProsperityMultiplier);
                SpawnScreenCenterBanner($"再满足 {needMore} 单居民需求即可拆开补给礼袋（铜币+{estCoins} · 工程手稿×2 · 招募卡×1 · 自选箱×1）！");
                return;
            }

            PendingChestRewardBundle b;
            if (_wwPendingOrderChests > 0)
            {
                _wwPendingOrderChests = Mathf.Max(0, _wwPendingOrderChests - 1);
                b = _wwLastChestBundle.coins > 0
                    ? _wwLastChestBundle
                    : new PendingChestRewardBundle
                    {
                        coins = Mathf.RoundToInt(260 * _sim.OrderProsperityMultiplier),
                        blueprints = 2,
                        recruitTickets = 1,
                        choiceBoxes = 1,
                    };
            }
            else
            {
                _sim.SatisfiedToday = Mathf.Max(0, _sim.SatisfiedToday - ForestSurvivalSim.SatisfiedGoal);
                int chestCoins = Mathf.RoundToInt(260 * _sim.OrderProsperityMultiplier);
                _sim.Coins += chestCoins;
                _sim.Add(SurvRes.Blueprint, 2, notifyToast: false);
                _sim.Add(SurvRes.RecruitTicket, 1, notifyToast: false);
                _sim.Add(SurvRes.ChoiceResBox, 1, notifyToast: false);
                b = new PendingChestRewardBundle
                {
                    coins = chestCoins,
                    blueprints = 2,
                    recruitTickets = 1,
                    choiceBoxes = 1,
                };
            }

            var rwList = new (SurvRes res, int count, Sprite customIcon, string customLabel)[]
            {
                (SurvRes.Wood, b.coins, ForestComicUiSkin.GetArt("ico_coin"), "营地铜币"),
                (SurvRes.Blueprint, b.blueprints, GetEconResIcon(SurvRes.Blueprint), "工程手稿"),
                (SurvRes.RecruitTicket, b.recruitTickets, GetEconResIcon(SurvRes.RecruitTicket), "电台招募卡"),
                (SurvRes.ChoiceResBox, b.choiceBoxes, GetEconResIcon(SurvRes.ChoiceResBox), "原料自选箱"),
            };

            RefreshResidentDrawer();
            if (_residentDrawer != null) _residentDrawer.SetActive(false);
            ShowCongratsRewardModal("今日满足 5 单居民需求 · 补给礼袋奖励", rwList);
        }

        // =====================================================================
        // 2. 全屏【恭喜获得】放射爆奖遮罩（支持 1~4 单排 & 5~10 十连抽双排网格）
        // =====================================================================
        void BuildCongratsRewardModal()
        {
            if (_wwCongratsModal != null) return;

            RectTransform maskRt = ForestUiWidgets.Stretch("WwCongratsRewardModal", _canvasRt);
            var maskImg = maskRt.gameObject.AddComponent<Image>();
            maskImg.color = new Color32(20, 26, 52, 236);
            maskImg.raycastTarget = true;
            var closeBtn = maskRt.gameObject.AddComponent<Button>();
            closeBtn.transition = Selectable.Transition.None;
            closeBtn.onClick.AddListener(() => _wwCongratsModal.SetActive(false));
            _wwCongratsModal = maskRt.gameObject;

            Image sunburst = ForestUiWidgets.MakeIcon("CongratsSunburst", maskRt,
                ForestComicUiSkin.Sunburst, new Vector2(0f, 340f), new Vector2(600f, 600f));
            sunburst.color = new Color(0.90f, 0.86f, 1.0f, 0.62f);
            var spin = sunburst.gameObject.AddComponent<WwOrdersUiAnimator>();
            spin.spinDegPerSec = 28f;

            RectTransform bodyRt = ForestUiWidgets.MakeRect("CongratsBody", maskRt,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(720f, 1280f));
            var bodyPop = bodyRt.gameObject.AddComponent<WwOrdersUiAnimator>();
            bodyPop.popOnEnable = true;

            Image banner = ForestUiWidgets.MakeIcon("CongratsBanner", bodyRt,
                ForestComicUiSkin.GetUiArt("ww_banner_congrats"),
                new Vector2(0f, 412f), new Vector2(420f, 136f));
            _wwCgBannerFallbackTxt = ForestUiWidgets.MakeText("BannerFallbackTxt", banner.transform,
                "恭喜获得", 50, new Color32(255, 162, 244, 255), TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), Vector2.zero, outline: true);
            var banOl = _wwCgBannerFallbackTxt.GetComponent<Outline>();
            if (banOl != null)
            {
                banOl.effectColor = new Color32(24, 16, 58, 255);
                banOl.effectDistance = new Vector2(3f, -3f);
            }

            _wwCgSubtitleTxt = ForestUiWidgets.MakeText("CongratsSub", bodyRt, "", 20, ForestComicUiSkin.AccentGold,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 326f), new Vector2(640f, 34f), outline: true);

            for (int i = 0; i < MaxCongratsTiles; i++)
            {
                int idx = i;
                RectTransform tileRt = ForestUiWidgets.MakeRect("RewardTile_" + i, bodyRt,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2((i - 1.5f) * 156f, 188f), new Vector2(132f, 132f));
                _wwCgTileRoots[i] = tileRt.gameObject;
                _wwCgTileRts[i] = tileRt;

                var frameImg = tileRt.gameObject.AddComponent<Image>();
                frameImg.sprite = ForestComicUiSkin.ClippedCardPurple;
                frameImg.type = Image.Type.Sliced;
                frameImg.raycastTarget = true;
                _wwCgTileFrames[i] = frameImg;

                var tileBtn = tileRt.gameObject.AddComponent<Button>();
                tileBtn.targetGraphic = frameImg;
                tileBtn.onClick.AddListener(() => SelectCongratsRewardTile(idx));

                _wwCgTileIcons[i] = ForestUiWidgets.MakeIcon("Ico", tileRt,
                    GetEconResIcon(SurvRes.Blueprint),
                    new Vector2(0f, 6f), new Vector2(78f, 78f));

                _wwCgTileCounts[i] = ForestUiWidgets.MakeText("Cnt", tileRt, "2", 22, Color.white, TextAnchor.LowerRight,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                    new Vector2(-8f, 6f), new Vector2(-12f, -10f), outline: true);

                _wwCgTileLabels[i] = ForestUiWidgets.MakeText("Lbl", tileRt, "工程手稿", 15, ForestComicUiSkin.InkHeaderLight,
                    TextAnchor.UpperCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -4f), new Vector2(138f, 24f), outline: true);

                Image brk = ForestUiWidgets.MakeSlicedImage("SelBrackets", tileRt,
                    ForestComicUiSkin.SelectionCornerBrackets, Color.white,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f), false);
                _wwCgTileBrackets[i] = brk.gameObject;
            }

            Image detailCard = ForestUiWidgets.MakeSlicedImage("RewardDetailSheet", bodyRt,
                ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -196f), new Vector2(584f, 236f), true);
            _wwCgDetailSheet = detailCard.gameObject;

            _wwCgDetailFrame = ForestUiWidgets.MakeSlicedImage("DetailItemCard", detailCard.transform,
                ForestComicUiSkin.ClippedCardGreen, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(26f, -20f), new Vector2(98f, 98f), false);
            _wwCgDetailIcon = ForestUiWidgets.MakeIcon("Ico", _wwCgDetailFrame.transform,
                GetEconResIcon(SurvRes.Blueprint), Vector2.zero, new Vector2(68f, 68f));

            _wwCgDetailNameTxt = ForestUiWidgets.MakeText("ItemName", detailCard.transform,
                "工程手稿", 24, ForestComicUiSkin.InkDark, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(142f, -24f), new Vector2(-168f, 34f), outline: false);
            SetOutlineEnabled(_wwCgDetailNameTxt, false);

            _wwCgDetailOwnTxt = ForestUiWidgets.MakeText("ItemOwn", detailCard.transform,
                "拥有: 2", 19, ForestComicUiSkin.InkMuted, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f) - new Vector2(0.5f, 0f),
                new Vector2(142f, -72f), new Vector2(-168f, 28f), outline: false);
            SetOutlineEnabled(_wwCgDetailOwnTxt, false);

            ForestUiWidgets.MakeSlicedImage("Divider", detailCard.transform, null, ForestComicUiSkin.DividerLine,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -132f), new Vector2(-44f, 2f), false);

            _wwCgDetailDescTxt = ForestUiWidgets.MakeText("ItemDesc", detailCard.transform,
                "营地核心升阶蓝图，可用于升级高阶作坊、仓库与供暖中枢。", 18, ForestComicUiSkin.InkMuted, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -148f), new Vector2(-52f, 74f), outline: false);
            SetOutlineEnabled(_wwCgDetailDescTxt, false);
            _wwCgDetailDescTxt.lineSpacing = 1.22f;

            ForestUiWidgets.MakeText("TapBlankClose", maskRt, "点击空白区域关闭", 21,
                new Color32(236, 240, 255, 240), TextAnchor.LowerCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 76f), new Vector2(0f, 34f), outline: true);

            _wwCongratsModal.SetActive(false);
        }

        public void ShowCongratsRewardModal(string subtitle, (SurvRes res, int count, Sprite customIcon, string customLabel)[] rewards)
        {
            if (rewards == null || rewards.Length == 0) return;
            var items = new CongratsDisplayItem[Mathf.Min(MaxCongratsTiles, rewards.Length)];
            for (int i = 0; i < items.Length; i++)
            {
                var r = rewards[i];
                bool isCoin = r.customLabel != null && r.customLabel.Contains("铜币");
                int rar = isCoin ? 1 : Mathf.Max(1, GetEconResRarity(r.res));
                int own = isCoin ? (_sim != null ? _sim.Coins : r.count) : (_sim != null ? _sim.Get(r.res) : r.count);
                items[i] = new CongratsDisplayItem
                {
                    res = r.res,
                    count = r.count,
                    icon = r.customIcon != null ? r.customIcon : GetEconResIcon(r.res),
                    label = !string.IsNullOrEmpty(r.customLabel) ? r.customLabel : ForestSurvivalSim.ResName(r.res),
                    rarity0to5 = rar,
                    ownSummary = $"拥有:  {FormatEconCount(own)}",
                    desc = GetRewardFlavorDescription(r.res, isCoin),
                };
            }
            ShowCongratsDisplayItems(subtitle, items);
        }

        void ShowCongratsDisplayItems(string subtitle, CongratsDisplayItem[] items)
        {
            if (items == null || items.Length == 0) return;
            BuildCongratsRewardModal();

            _wwCgCurrentItems = items;
            _wwCgSelectedIdx = 0;

            if (_wwCgBannerFallbackTxt != null)
                _wwCgBannerFallbackTxt.gameObject.SetActive(!HasRawForestArtAsset("ww_banner_congrats"));

            _wwCgSubtitleTxt.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
            _wwCgSubtitleTxt.text = subtitle ?? "";

            int cnt = Mathf.Clamp(items.Length, 1, MaxCongratsTiles);
            bool multiRow = cnt > 4;
            float tileSize = multiRow ? 112f : 132f;
            float icoSize = multiRow ? 68f : 80f;

            for (int i = 0; i < MaxCongratsTiles; i++)
            {
                bool show = i < cnt;
                _wwCgTileRoots[i].SetActive(show);
                if (!show) continue;

                var entry = items[i];
                Vector2 pos;
                if (!multiRow)
                {
                    float step = cnt >= 4 ? 154f : 164f;
                    float startX = -(cnt - 1) * step * 0.5f;
                    pos = new Vector2(startX + i * step, 148f);
                }
                else
                {
                    int cols = Mathf.Min(5, cnt);
                    int col = i % 5;
                    int row = i / 5;
                    int rowCnt = row == 0 ? cols : (cnt - 5);
                    float stepX = 132f;
                    float startX = -(rowCnt - 1) * stepX * 0.5f;
                    pos = new Vector2(startX + col * stepX, row == 0 ? 214f : 56f);
                }

                _wwCgTileRts[i].anchoredPosition = pos;
                _wwCgTileRts[i].sizeDelta = new Vector2(tileSize, tileSize);
                _wwCgTileIcons[i].rectTransform.sizeDelta = new Vector2(icoSize, icoSize);
                _wwCgTileFrames[i].sprite = ForestComicUiSkin.GetClippedCard(entry.rarity0to5);
                _wwCgTileIcons[i].sprite = entry.icon;
                _wwCgTileCounts[i].text = entry.count > 0 ? FormatEconCount(entry.count) : "★";
                _wwCgTileLabels[i].text = entry.label;
            }

            SelectCongratsRewardTile(0);
            _wwCongratsModal.transform.SetAsLastSibling();
            if (_loadingOverlay != null && _loadingOverlay.activeSelf)
                _loadingOverlay.transform.SetAsLastSibling();
            _wwCongratsModal.SetActive(true);
        }

        void SelectCongratsRewardTile(int idx)
        {
            if (_wwCgCurrentItems == null || _wwCgCurrentItems.Length == 0) return;
            int cnt = Mathf.Min(MaxCongratsTiles, _wwCgCurrentItems.Length);
            _wwCgSelectedIdx = Mathf.Clamp(idx, 0, cnt - 1);

            for (int i = 0; i < MaxCongratsTiles; i++)
            {
                if (_wwCgTileBrackets[i] != null)
                    _wwCgTileBrackets[i].SetActive(i == _wwCgSelectedIdx && i < cnt);
            }

            var sel = _wwCgCurrentItems[_wwCgSelectedIdx];
            _wwCgDetailSheet.SetActive(true);
            _wwCgDetailFrame.sprite = ForestComicUiSkin.GetClippedCard(sel.rarity0to5);
            _wwCgDetailIcon.sprite = sel.icon;
            _wwCgDetailNameTxt.text = sel.label;
            _wwCgDetailOwnTxt.text = sel.ownSummary;
            _wwCgDetailDescTxt.text = sel.desc;
        }

        static string GetRewardFlavorDescription(SurvRes r, bool isCoin)
        {
            if (isCoin)
                return "营地通用流通铜币，专供新建与升级营房工坊，或在行商处兑换招募补给。";
            switch (r)
            {
                case SurvRes.HeroExp:
                    return "高纯度英雄经验晶簇，专供【英雄】舱室提升伙伴等级（支持100%无损重置）。";
                case SurvRes.RecruitTicket:
                    return "短波广播站幸存者呼叫卡，可在【广播招募】单抽或十连抽取传说/史诗英雄与拼图碎片。";
                case SurvRes.SkillBook:
                    return "旧世特种战术技能书，用于在【英雄】属性页升级 4 大主被动技能等级。";
                case SurvRes.ChoiceResBox:
                    return "紧俏原料自选补给箱，在【道具->消耗品】中打开可任选木/铁/水/布/草药×5。";
                case SurvRes.SpeedHourglass:
                    return "精密工坊加速沙漏，使用后立即缩短建造、五大工坊与农牧倒计时 5 分钟。";
                case SurvRes.ShardUnivEpic:
                case SurvRes.ShardUnivLeg:
                    return "珍稀万能拼图碎片，可在英雄【升星】页 1:1 补齐同品质专属碎片点亮星瓣。";
                case SurvRes.Blueprint:
                    return "珍贵的旧城工程手稿，用于解锁中后期核心工坊、仓库扩容与专武升阶。";
                default:
                    return $"营地紧俏储备物资【{ForestSurvivalSim.ResName(r)}】，可投入工坊制造或满足居民日常需求。";
            }
        }

        // =====================================================================
        // 3. 全屏【等级提升】红金绶带新功能解锁弹窗
        // =====================================================================
        void BuildLevelUpModal()
        {
            _levelUpModal = MakeModalMask("LevelUpModal", 0.92f, CloseAllModals);
            var maskImg = _levelUpModal.GetComponent<Image>();
            if (maskImg != null) maskImg.color = new Color32(20, 26, 52, 236);

            Image sunburst = ForestUiWidgets.MakeIcon("LvUpSunburst", _levelUpModal.transform,
                ForestComicUiSkin.Sunburst, new Vector2(0f, 320f), new Vector2(580f, 580f));
            sunburst.color = new Color(1.0f, 0.90f, 0.55f, 0.55f);
            var spin = sunburst.gameObject.AddComponent<WwOrdersUiAnimator>();
            spin.spinDegPerSec = 26f;

            RectTransform bodyRt = ForestUiWidgets.MakeRect("LvUpBody", _levelUpModal.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(720f, 1280f));
            var pop = bodyRt.gameObject.AddComponent<WwOrdersUiAnimator>();
            pop.popOnEnable = true;

            Image ribbon = ForestUiWidgets.MakeIcon("LevelUpBanner", bodyRt,
                ForestComicUiSkin.GetUiArt("ww_banner_levelup"),
                new Vector2(0f, 392f), new Vector2(436f, 126f));
            _wwLvBannerFallbackTxt = ForestUiWidgets.MakeText("RibTxt", ribbon.transform,
                "等级提升", 46, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), Vector2.zero, outline: true);
            var ribOl = _wwLvBannerFallbackTxt.GetComponent<Outline>();
            if (ribOl != null)
            {
                ribOl.effectColor = new Color32(52, 16, 22, 255);
                ribOl.effectDistance = new Vector2(2.8f, -2.8f);
            }

            _lvUpNumTxt = ForestUiWidgets.MakeText("LvNum", bodyRt, "2级", 56, ForestComicUiSkin.AccentGold,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 278f), new Vector2(420f, 74f), outline: true);

            _wwLvSubTitleTxt = ForestUiWidgets.MakeText("Sub", bodyRt, "新功能解锁！", 24, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 206f), new Vector2(560f, 40f), outline: true);

            Image sheet = ForestUiWidgets.MakeSlicedImage("UnlockLightSheet", bodyRt,
                ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -14f), new Vector2(612f, 360f), true);

            Text sHdr = ForestUiWidgets.MakeText("SheetHdr", sheet.transform, "本次升级解锁内容", 19, ForestComicUiSkin.InkMuted,
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -12f), new Vector2(0f, 32f), outline: false);
            SetOutlineEnabled(sHdr, false);

            ForestUiWidgets.MakeSlicedImage("SheetDivider", sheet.transform, null, ForestComicUiSkin.DividerLine,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -50f), new Vector2(-44f, 2f), false);

            for (int i = 0; i < 3; i++)
            {
                float x = -188f + i * 188f;
                Image uCard = ForestUiWidgets.MakeSlicedImage("Unlock_" + i, sheet.transform,
                    ForestComicUiSkin.WwSubPanel, Color.white,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(x, -22f), new Vector2(174f, 262f), false);
                _lvUpUnlockCards[i] = uCard.gameObject;

                _lvUpUnlockIcons[i] = ForestUiWidgets.MakeIcon("Ico", uCard.transform,
                    ForestComicUiSkin.GetArt("well"),
                    new Vector2(0f, 26f), new Vector2(132f, 132f));

                _lvUpUnlockNames[i] = ForestUiWidgets.MakeText("Name", uCard.transform,
                    "净水石井", 20, ForestComicUiSkin.InkDark, TextAnchor.MiddleCenter,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 12f), new Vector2(-12f, 56f), outline: false);
                SetOutlineEnabled(_lvUpUnlockNames[i], false);
            }

            ForestUiWidgets.MakeText("TapHint", _levelUpModal.transform, "点击空白区域关闭", 21,
                new Color32(236, 240, 255, 240), TextAnchor.LowerCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 108f), new Vector2(0f, 34f), outline: true);

            _levelUpModal.SetActive(false);
            BuildCongratsRewardModal();
        }

        public void ShowLevelUpModal(int newLv, string[] unlockedNames)
        {
            if (_world != null && _world.InWaveDefense)
            {
                SpawnScreenCenterBanner($"★ 营地升至 {newLv} 级！英雄上限提升至 {newLv * 5} 级！");
                return;
            }
            CloseAllModals();
            if (_wwCongratsModal != null) _wwCongratsModal.SetActive(false);

            if (_wwLvBannerFallbackTxt != null)
                _wwLvBannerFallbackTxt.gameObject.SetActive(!HasRawForestArtAsset("ww_banner_levelup"));

            _lvUpNumTxt.text = $"{newLv}级";

            string[] displayList = unlockedNames;
            if (displayList == null || displayList.Length == 0)
            {
                displayList = new[]
                {
                    $"铜币 +{80 * newLv}",
                    $"英雄上限 Lv.{newLv * 5}",
                    newLv >= 3 ? "电台招募卡补给" : "营地繁荣度提升"
                };
                if (_wwLvSubTitleTxt != null) _wwLvSubTitleTxt.text = "领主统率上限突破！";
            }
            else
            {
                if (_wwLvSubTitleTxt != null) _wwLvSubTitleTxt.text = "新功能解锁！";
            }

            int count = Mathf.Min(3, displayList.Length);
            float spacing = count >= 3 ? 188f : 210f;
            float startX = -(count - 1) * spacing * 0.5f;

            for (int i = 0; i < 3; i++)
            {
                bool show = i < count;
                _lvUpUnlockCards[i].SetActive(show);
                if (!show) continue;
                var cardRt = _lvUpUnlockCards[i].GetComponent<RectTransform>();
                if (cardRt != null) cardRt.anchoredPosition = new Vector2(startX + i * spacing, -22f);
                _lvUpUnlockIcons[i].sprite = ResolveUnlockCardSprite(displayList[i]);
                _lvUpUnlockNames[i].text = displayList[i];
            }
            _levelUpModal.transform.SetAsLastSibling();
            if (_loadingOverlay != null && _loadingOverlay.activeSelf)
                _loadingOverlay.transform.SetAsLastSibling();
            _levelUpModal.SetActive(true);
        }

        static Sprite ResolveUnlockCardSprite(string title)
        {
            if (string.IsNullOrEmpty(title))
                return ForestComicUiSkin.GetUiArt("ww_xp_star", ForestComicUiSkin.ResIcon(SurvRes.Blueprint));
            if (title.Contains("招募"))
                return GetEconResIcon(SurvRes.RecruitTicket);
            if (title.Contains("铜币"))
                return ForestComicUiSkin.GetArt("ico_coin", ForestComicUiSkin.ResIcon(SurvRes.Blueprint));
            if (title.Contains("伙伴") || title.Contains("英雄"))
                return ForestComicUiSkin.GetUiArt("ww_dock_heroes", ForestComicUiSkin.GetArt("bust_dachui"));
            if (title.Contains("农田") || title.Contains("种") || title.Contains("垄"))
                return ForestComicUiSkin.GetArt("farm_2", ForestComicUiSkin.ResIcon(SurvRes.Veggie));
            if (title.Contains("石井") || title.Contains("滴灌") || title.Contains("净水"))
                return ForestComicUiSkin.GetArt("well", ForestComicUiSkin.ResIcon(SurvRes.Water));
            if (title.Contains("畜牧") || title.Contains("兽栏"))
                return ForestComicUiSkin.GetArt("beast_ranch", ForestComicUiSkin.GetArt("boar", ForestComicUiSkin.ResIcon(SurvRes.Pelt)));
            if (title.Contains("锯木") || title.Contains("木板"))
                return ForestComicUiSkin.GetArt("sawmill_shed", ForestComicUiSkin.ResIcon(SurvRes.Plank));
            if (title.Contains("锻造") || title.Contains("铁钉"))
                return ForestComicUiSkin.GetArt("scrap_smithy", ForestComicUiSkin.ResIcon(SurvRes.Nails));
            if (title.Contains("裁缝") || title.Contains("麻布衣"))
                return ForestComicUiSkin.GetArt("tailor_shop", ForestComicUiSkin.ResIcon(SurvRes.LinenShirt));
            if (title.Contains("汤") || title.Contains("炊事") || title.Contains("厨房") || title.Contains("灶"))
                return ForestComicUiSkin.GetArt("smokehouse_kitchen", ForestComicUiSkin.ResIcon(SurvRes.VeggieSoup));
            if (title.Contains("草药") || title.Contains("敷包"))
                return ForestComicUiSkin.GetArt("herb_hut", ForestComicUiSkin.ResIcon(SurvRes.HerbPoultice));
            if (title.Contains("电台") || title.Contains("行商") || title.Contains("广播"))
                return ForestComicUiSkin.GetArt("radio_post", GetEconResIcon(SurvRes.RecruitTicket));
            if (title.Contains("皮卡") || title.Contains("丧尸城") || title.Contains("公路"))
                return ForestComicUiSkin.GetArt("truck_active", ForestComicUiSkin.GetArt("dock_map"));
            return ForestComicUiSkin.GetUiArt("ww_xp_star", ForestComicUiSkin.ResIcon(SurvRes.Blueprint));
        }

        // =====================================================================
        // 4A. 短波电台双页签中枢：【广播招募】(Tab 0) + 【山口行商】(Tab 1)
        // =====================================================================
        void BuildCaravanModal()
        {
            _caravanModal = MakeModalMask("RadioAndCaravanModal", 0.80f, CloseAllModals);
            var maskImg = _caravanModal.GetComponent<Image>();
            if (maskImg != null) maskImg.color = new Color32(18, 24, 50, 220);

            Image card = ForestUiWidgets.MakeSlicedImage("Card", _caravanModal.transform,
                ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(672f, 944f), true);

            _wwRadioModalHeaderTxt = ForestUiWidgets.MakeText("Title", card.transform, "短波广播站 · 幸存者招募", 27,
                ForestComicUiSkin.InkHeaderLight, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -14f), new Vector2(-104f, 38f), outline: true);

            ForestUiWidgets.MakeJellyButton("Close", card.transform, ForestComicUiSkin.BtnRed, "×", 25,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -12f), new Vector2(46f, 46f), CloseAllModals, out _);

            // 顶部双页签切换条：[广播招募] vs [山口行商]
            string[] tabTitles = { "广播招募", "山口行商" };
            Sprite[] tabSprites =
            {
                GetEconResIcon(SurvRes.RecruitTicket),
                ForestComicUiSkin.GetUiArt("ww_dept_trade", ForestComicUiSkin.GetArt("radio_post")),
            };
            for (int t = 0; t < 2; t++)
            {
                int tabIdx = t;
                float tx = t == 0 ? -142f : 142f;
                Image tBg = ForestUiWidgets.MakeSlicedImage("RadioTab_" + t, card.transform,
                    ForestComicUiSkin.WwTabIdle, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(tx, -60f), new Vector2(264f, 54f), true);
                _wwRadioTabImgs[t] = tBg;
                var tBtn = tBg.gameObject.AddComponent<Button>();
                tBtn.targetGraphic = tBg;
                tBtn.onClick.AddListener(() =>
                {
                    _wwRadioStationTab = tabIdx;
                    RefreshCaravanModal();
                });

                ForestUiWidgets.MakeIcon("Ico", tBg.transform, tabSprites[t], new Vector2(-78f, 0f), new Vector2(36f, 36f));
                _wwRadioTabLbls[t] = ForestUiWidgets.MakeText("Lbl", tBg.transform, tabTitles[t], 21,
                    ForestComicUiSkin.InkDark, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(18f, 0f), Vector2.zero, outline: true);
                _wwRadioTabOutlines[t] = _wwRadioTabLbls[t].GetComponent<Outline>();
                if (_wwRadioTabOutlines[t] != null) _wwRadioTabOutlines[t].enabled = false;

                if (t == 0)
                {
                    Image d = ForestUiWidgets.MakeIcon("Dot", tBg.transform, ForestComicUiSkin.RedDotBadge,
                        new Vector2(118f, 18f), new Vector2(24f, 24f));
                    ForestUiWidgets.MakeText("Bang", d.transform, "!", 15, Color.white, TextAnchor.MiddleCenter,
                        Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: false);
                    _wwRadioTab0Dot = d.gameObject;
                }
            }

            // -----------------------------------------------------------------
            // Page 0: 【广播招募】幸存者电台抽卡演出页 (Ref Image 5)
            // -----------------------------------------------------------------
            _wwRadioGachaPage = ForestUiWidgets.MakeRect("RadioGachaPage", card.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -124f), new Vector2(636f, 804f)).gameObject;

            Image gSheet = ForestUiWidgets.MakeSlicedImage("GachaLightSheet", _wwRadioGachaPage.transform,
                ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(636f, 706f), false);

            // A) 短波频段氛围舞台 + 招募卡存量 + 10抽保底进度条
            Image radioStage = ForestUiWidgets.MakeSlicedImage("RadioBanner", gSheet.transform,
                ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -14f), new Vector2(604f, 198f), false);

            ForestUiWidgets.MakeIcon("RadioArt", radioStage.transform,
                ForestComicUiSkin.GetArt("radio_post", GetEconResIcon(SurvRes.RecruitTicket)),
                new Vector2(-218f, 10f), new Vector2(132f, 132f));

            // 右上拥有招募卡胶囊
            Image tkPill = ForestUiWidgets.MakeSlicedImage("TicketPill", radioStage.transform,
                ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-14f, -12f), new Vector2(198f, 38f), false);
            ForestUiWidgets.MakeIcon("TkIco", tkPill.transform, GetEconResIcon(SurvRes.RecruitTicket),
                new Vector2(-68f, 0f), new Vector2(28f, 28f));
            _wwGachaTicketOwnTxt = ForestUiWidgets.MakeText("TkVal", tkPill.transform, "招募卡: 5", 17,
                ForestComicUiSkin.AccentGold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(14f, 0f), Vector2.zero, outline: true);

            ForestUiWidgets.MakeText("RadioSlogan", radioStage.transform, "高功率短波电台呼叫", 24,
                Color.white, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(154f, -16f), new Vector2(-366f, 34f), outline: true);

            _wwGachaTotalPullsTxt = ForestUiWidgets.MakeText("TotalPulls", radioStage.transform,
                "全频段搜寻深山强力幸存者与拼图碎片（已累计呼叫 0 次）", 15,
                new Color32(196, 222, 255, 255), TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(154f, -56f), new Vector2(-172f, 42f), outline: true);

            // 保底进度槽
            Image pityBar = ForestUiWidgets.MakeSlicedImage("PityBar", radioStage.transform,
                ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(44f, 16f), new Vector2(476f, 36f), false);
            RectTransform pfRt = ForestUiWidgets.MakeRect("Fill", pityBar.transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6f, -6f));
            _wwGachaPityFill = pfRt.gameObject.AddComponent<Image>();
            _wwGachaPityFill.sprite = ForestComicUiSkin.BtnOrange;
            _wwGachaPityFill.type = Image.Type.Filled;
            _wwGachaPityFill.fillMethod = Image.FillMethod.Horizontal;
            _wwGachaPityFill.raycastTarget = false;
            _wwGachaPityTxt = ForestUiWidgets.MakeText("PityTxt", pityBar.transform,
                "再招募 10 次必出整卡英雄或大额高阶碎片！", 15, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

            // B) 热门传说/史诗英雄 UP 全身立绘展示橱窗（5 名王牌：阿飒、周博士、青禾、大锤、林砚）
            Image featBox = ForestUiWidgets.MakeSlicedImage("FeaturedShowcase", gSheet.transform,
                ForestComicUiSkin.WwSubPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -224f), new Vector2(604f, 272f), false);

            Text featHdr = ForestUiWidgets.MakeText("FeatHdr", featBox.transform,
                "★ 频段核心响应王牌英雄（点击全身立绘卡可预览技能与定位）", 17,
                ForestComicUiSkin.InkDark, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -10f), new Vector2(-20f, 28f), outline: false);
            SetOutlineEnabled(featHdr, false);

            for (int i = 0; i < 5; i++)
            {
                int slotCopy = i;
                float fx = -232f + i * 116f;
                Image fCard = ForestUiWidgets.MakeSlicedImage("FeatHero_" + i, featBox.transform,
                    ForestComicUiSkin.ClippedCardOrange, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(fx, -40f), new Vector2(110f, 186f), true);
                _wwGachaFeatCards[i] = fCard;
                fCard.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                    OpenHeroDetailModal(GetRadioFeaturedHeroIndex(slotCopy), 0));

                Image fMask = ForestUiWidgets.MakeSlicedImage("Mask", fCard.transform, null, new Color(1f, 1f, 1f, 0.22f),
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -6f), new Vector2(98f, 128f), false);
                fMask.gameObject.AddComponent<Mask>().showMaskGraphic = true;

                Image fShadow = ForestUiWidgets.MakeIcon("FloorShadow", fMask.transform, ForestComicUiSkin.WhiteSoftCircle,
                    new Vector2(0f, -52f), new Vector2(64f, 14f));
                fShadow.preserveAspect = false;
                fShadow.color = new Color(0.05f, 0.07f, 0.16f, 0.36f);

                _wwGachaFeatBusts[i] = ForestUiWidgets.MakeIcon("FullBody", fMask.transform,
                    GetEconArt("fullbody_dachui", ForestComicUiSkin.GetArt("bust_dachui")),
                    new Vector2(0f, 1f), new Vector2(94f, 122f));
                _wwGachaFeatBusts[i].preserveAspect = true;

                _wwGachaFeatFactions[i] = ForestUiWidgets.MakeIcon("Fac", fCard.transform,
                    GetFactionCrestSprite(HeroFaction.IronGuard), new Vector2(6f, -6f), new Vector2(24f, 24f),
                    anchor: new Vector2(0f, 1f));

                Image botPill = ForestUiWidgets.MakeSlicedImage("BotTag", fCard.transform,
                    ForestComicUiSkin.WwHeaderShell, Color.white,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 5f), new Vector2(100f, 42f), false);
                _wwGachaFeatStates[i] = ForestUiWidgets.MakeText("Txt", botPill.transform, "阿飒\n待招募", 12,
                    Color.white, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);
            }

            Text rateNote = ForestUiWidgets.MakeText("RateNote", featBox.transform,
                "呼叫概率公示：整卡英雄 16%  ·  专属拼图碎片 56%  ·  英雄经验与技能书 28%",
                14, ForestComicUiSkin.InkMuted, TextAnchor.LowerCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 8f), new Vector2(-16f, 24f), outline: false);
            SetOutlineEnabled(rateNote, false);

            // C) 单抽与十连呼叫大按钮区
            Button pull1Btn = ForestUiWidgets.MakeJellyButton("BtnRecruitPull1", gSheet.transform,
                ForestComicUiSkin.BtnBlue, "呼叫 1 次\n消耗 1 卡", 20,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-150f, 34f), new Vector2(268f, 92f),
                () => OnPerformRadioRecruitClicked(1), out _wwGachaPull1Lbl);
            _wwGachaPull1BtnImg = pull1Btn.GetComponent<Image>();
            ForestUiWidgets.MakeIcon("Tk1", pull1Btn.transform, GetEconResIcon(SurvRes.RecruitTicket),
                new Vector2(-94f, 0f), new Vector2(44f, 44f));
            _wwGachaPull1Lbl.rectTransform.anchoredPosition = new Vector2(18f, 0f);

            Button pull10Btn = ForestUiWidgets.MakeJellyButton("BtnRecruitPull10", gSheet.transform,
                ForestComicUiSkin.BtnOrange, "十连呼叫\n消耗 10 卡", 21,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(150f, 34f), new Vector2(276f, 92f),
                () => OnPerformRadioRecruitClicked(10), out _wwGachaPull10Lbl);
            _wwGachaPull10BtnImg = pull10Btn.GetComponent<Image>();
            ForestUiWidgets.MakeIcon("Tk10", pull10Btn.transform, GetEconResIcon(SurvRes.RecruitTicket),
                new Vector2(-98f, 0f), new Vector2(44f, 44f));
            _wwGachaPull10Lbl.rectTransform.anchoredPosition = new Vector2(18f, 0f);

            Image tenTag = ForestUiWidgets.MakeSlicedImage("GuaranteeTag", pull10Btn.transform,
                ForestComicUiSkin.BtnRed, Color.white,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                new Vector2(-8f, 4f), new Vector2(118f, 28f), false);
            ForestUiWidgets.MakeText("Txt", tenTag.transform, "必出整卡/高阶!", 13, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

            MakeWwBackRedButton(_wwRadioGachaPage.transform, CloseAllModals, new Vector2(4f, 8f), new Vector2(96f, 70f));
            ForestUiWidgets.MakeJellyButton("BtnGachaGoHeroes", _wwRadioGachaPage.transform,
                ForestComicUiSkin.BtnBlue, "查看英雄名册 ▶", 19,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-8f, 12f), new Vector2(216f, 62f),
                () => OpenHeroModal(1), out _);

            // -----------------------------------------------------------------
            // Page 1: 【山口行商】大宗收购 + 2 项行商特供补给兑换
            // -----------------------------------------------------------------
            _wwCaravanTradePage = ForestUiWidgets.MakeRect("CaravanTradePage", card.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -124f), new Vector2(636f, 804f)).gameObject;

            _cvSubTxt = ForestUiWidgets.MakeText("Sub", _wwCaravanTradePage.transform,
                "邻山商队高价收购成衣草药与熏肉，并限量供应打折招募卡与战术技能书！", 15,
                ForestComicUiSkin.AccentGold, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -2f), new Vector2(-16f, 28f), outline: true);

            Image cSheet = ForestUiWidgets.MakeSlicedImage("CaravanLightSheet", _wwCaravanTradePage.transform,
                ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -34f), new Vector2(636f, 676f), false);

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                Image row = ForestUiWidgets.MakeSlicedImage("Offer_" + i, cSheet.transform,
                    ForestComicUiSkin.WwSubPanel, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -12f - i * 144f), new Vector2(604f, 134f), false);

                Image itemFrame = ForestUiWidgets.MakeSlicedImage("RarityCard", row.transform,
                    ForestComicUiSkin.ClippedCardBlue, Color.white,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(14f, 0f), new Vector2(96f, 96f), true);
                _wwCvRarityFrames[i] = itemFrame;
                int capIdx = i;
                itemFrame.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                {
                    if (capIdx < _sim.CaravanOffers.Count)
                        OpenItemSourceTip(_sim.CaravanOffers[capIdx].giveRes);
                });

                _cvIcons[i] = ForestUiWidgets.MakeIcon("Ico", itemFrame.transform,
                    ForestComicUiSkin.ResIcon(SurvRes.LinenShirt), new Vector2(0f, 4f), new Vector2(64f, 64f));
                _wwCvPackCntTxts[i] = ForestUiWidgets.MakeText("PackCnt", itemFrame.transform, "x2", 18, Color.white,
                    TextAnchor.LowerRight, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                    new Vector2(-6f, 4f), new Vector2(-10f, -8f), outline: true);

                _cvTitles[i] = ForestUiWidgets.MakeText("Title", row.transform, "邻山伐木营采购御寒装", 19,
                    ForestComicUiSkin.InkDark, TextAnchor.UpperLeft,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                    new Vector2(124f, -14f), new Vector2(-276f, 28f), outline: false);
                SetOutlineEnabled(_cvTitles[i], false);

                _cvNeedTxts[i] = ForestUiWidgets.MakeText("Need", row.transform, "装箱物资: 耐磨麻布衣 0/2", 16,
                    ForestComicUiSkin.InkMuted, TextAnchor.MiddleLeft,
                    new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(124f, 0f), new Vector2(-276f, 26f), outline: false);
                SetOutlineEnabled(_cvNeedTxts[i], false);
                _cvNeedTxts[i].supportRichText = true;

                _cvRewardTxts[i] = ForestUiWidgets.MakeText("Rw", row.transform, "报酬: 铜币 +340  ·  手稿 +1", 16,
                    ForestComicUiSkin.AccentGreen, TextAnchor.LowerLeft,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f),
                    new Vector2(124f, 12f), new Vector2(-276f, 26f), outline: false);
                SetOutlineEnabled(_cvRewardTxts[i], false);

                _cvTradeBtns[i] = ForestUiWidgets.MakeJellyButton("BtnTrade_" + i, row.transform,
                    ForestComicUiSkin.BtnGreen, "装车交货", 19,
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(132f, 56f),
                    () => OnCaravanTradeClicked(idx), out _cvTradeLbls[i]);
            }

            // 下半部 2 张【行商特供养成补给兑换】卡片（铜币换招募卡 / 口粮换技能书）
            Text spHdr = ForestUiWidgets.MakeText("SpecExHdr", cSheet.transform, "【行商特供补给】使用盈余营地铜币与熟食兑换英雄养成物资", 15,
                ForestComicUiSkin.AccentPurple, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -448f), new Vector2(-20f, 24f), outline: false);
            SetOutlineEnabled(spHdr, false);

            // 特供 1: 180铜币 -> 电台招募卡×1
            Image exCard1 = ForestUiWidgets.MakeSlicedImage("ExTicketCard", cSheet.transform,
                ForestComicUiSkin.WwSubPanel, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-152f, 14f), new Vector2(294f, 182f), false);
            Image exClip1 = ForestUiWidgets.MakeSlicedImage("Clip", exCard1.transform,
                ForestComicUiSkin.ClippedCardOrange, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(12f, -14f), new Vector2(78f, 78f), false);
            ForestUiWidgets.MakeIcon("Ico", exClip1.transform, GetEconResIcon(SurvRes.RecruitTicket), Vector2.zero, new Vector2(56f, 56f));
            Text exT1 = ForestUiWidgets.MakeText("Nm", exCard1.transform, "电台招募卡 ×1", 18, ForestComicUiSkin.InkDark,
                TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(102f, -20f), new Vector2(-112f, 28f), outline: false);
            SetOutlineEnabled(exT1, false);
            _wwCvExTicketNeedTxt = ForestUiWidgets.MakeText("Cost", exCard1.transform, "铜币 120/180", 15, ForestComicUiSkin.InkMuted,
                TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(102f, -54f), new Vector2(-112f, 28f), outline: false);
            SetOutlineEnabled(_wwCvExTicketNeedTxt, false);
            _wwCvExTicketNeedTxt.supportRichText = true;
            Button exBtn1 = ForestUiWidgets.MakeJellyButton("BtnBuyTicket", exCard1.transform,
                ForestComicUiSkin.BtnOrange, "180铜币兑换", 18,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(256f, 50f),
                OnBuyCaravanRecruitTicketClicked, out _wwCvExTicketBtnLbl);
            _wwCvExTicketBtnImg = exBtn1.GetComponent<Image>();

            // 特供 2: 6口粮 -> 战术技能书×5
            Image exCard2 = ForestUiWidgets.MakeSlicedImage("ExBookCard", cSheet.transform,
                ForestComicUiSkin.WwSubPanel, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(152f, 14f), new Vector2(294f, 182f), false);
            Image exClip2 = ForestUiWidgets.MakeSlicedImage("Clip", exCard2.transform,
                ForestComicUiSkin.ClippedCardBlue, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(12f, -14f), new Vector2(78f, 78f), false);
            ForestUiWidgets.MakeIcon("Ico", exClip2.transform, GetEconResIcon(SurvRes.SkillBook), Vector2.zero, new Vector2(56f, 56f));
            Text exT2 = ForestUiWidgets.MakeText("Nm", exCard2.transform, "战术技能书 ×5", 18, ForestComicUiSkin.InkDark,
                TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(102f, -20f), new Vector2(-112f, 28f), outline: false);
            SetOutlineEnabled(exT2, false);
            _wwCvExBookNeedTxt = ForestUiWidgets.MakeText("Cost", exCard2.transform, "熟食口粮 6/6", 15, ForestComicUiSkin.InkMuted,
                TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(102f, -54f), new Vector2(-112f, 28f), outline: false);
            SetOutlineEnabled(_wwCvExBookNeedTxt, false);
            _wwCvExBookNeedTxt.supportRichText = true;
            Button exBtn2 = ForestUiWidgets.MakeJellyButton("BtnBuyBooks", exCard2.transform,
                ForestComicUiSkin.BtnBlue, "6份口粮兑换", 18,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(256f, 50f),
                OnBuyCaravanSkillBooksClicked, out _wwCvExBookBtnLbl);
            _wwCvExBookBtnImg = exBtn2.GetComponent<Image>();

            MakeWwBackRedButton(_wwCaravanTradePage.transform, CloseAllModals, new Vector2(4f, 8f), new Vector2(96f, 70f));
            ForestUiWidgets.MakeText("FooterTip", _wwCaravanTradePage.transform,
                "委任骨干进驻【行商贸易】部门可额外提高铜币收购溢价", 15,
                ForestComicUiSkin.InkHeaderLight, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f),
                new Vector2(114f, 16f), new Vector2(-132f, 54f), outline: true);

            _caravanModal.SetActive(false);
        }

        public void OpenRadioRecruitModal()
        {
            CloseAllModals();
            if (_wwCongratsModal != null) _wwCongratsModal.SetActive(false);
            _wwRadioStationTab = 0;
            RefreshCaravanModal();
            _caravanModal.SetActive(true);
        }

        public void OpenCaravanModal()
        {
            CloseAllModals();
            if (_wwCongratsModal != null) _wwCongratsModal.SetActive(false);
            if (!_sim.RadioRepaired)
            {
                if (_sim.Get(SurvRes.RecruitTicket) > 0)
                {
                    _wwRadioStationTab = 0;
                    RefreshCaravanModal();
                    _caravanModal.SetActive(true);
                    return;
                }
                JumpToNodeById("radio_post");
                return;
            }
            _wwRadioStationTab = 1;
            RefreshCaravanModal();
            _caravanModal.SetActive(true);
        }

        int GetRadioFeaturedHeroIndex(int slot)
        {
            // 优先展示王牌新英雄与三阵营核心：阿飒(传)、周博士(传)、青禾(史)、大锤(传)、林砚(传)
            int[] preferred9 = { 7, 8, 6, 0, 3 };
            int[] fallback6 = { 0, 3, 1, 2, 5 };
            int s = Mathf.Clamp(slot, 0, 4);
            int idx = (_sim != null && _sim.Heroes.Count >= 9) ? preferred9[s] : fallback6[s];
            return _sim != null && _sim.Heroes.Count > 0 ? Mathf.Clamp(idx, 0, _sim.Heroes.Count - 1) : 0;
        }

        void RefreshCaravanModal()
        {
            bool isGacha = _wwRadioStationTab == 0;
            if (_wwRadioGachaPage != null) _wwRadioGachaPage.SetActive(isGacha);
            if (_wwCaravanTradePage != null) _wwCaravanTradePage.SetActive(!isGacha);

            if (_wwRadioModalHeaderTxt != null)
                _wwRadioModalHeaderTxt.text = isGacha ? "短波广播站 · 幸存者招募" : "山口行商驿站 · 大宗收购";

            for (int t = 0; t < 2; t++)
            {
                bool active = t == _wwRadioStationTab;
                if (_wwRadioTabImgs[t] != null)
                    _wwRadioTabImgs[t].sprite = active ? ForestComicUiSkin.WwTabActive : ForestComicUiSkin.WwTabIdle;
                if (_wwRadioTabLbls[t] != null)
                    _wwRadioTabLbls[t].color = active ? Color.white : (Color)ForestComicUiSkin.InkDark;
                if (_wwRadioTabOutlines[t] != null)
                    _wwRadioTabOutlines[t].enabled = active;
            }

            int tickets = _sim.Get(SurvRes.RecruitTicket);
            if (_wwRadioTab0Dot != null) _wwRadioTab0Dot.SetActive(tickets > 0);

            // --- 刷新 Tab 0: [广播招募] ---
            _wwGachaTicketOwnTxt.text = $"招募卡: {tickets}";
            _wwGachaTotalPullsTxt.text = $"全频段搜寻深山强力幸存者与拼图碎片（已累计呼叫 {_sim.TotalRecruitPulls} 次）";

            int remPity = _sim.GetRecruitPullsUntilGuaranteed();
            int curPityProg = 10 - remPity;
            _wwGachaPityFill.fillAmount = Mathf.Clamp01(curPityProg / 10f);
            _wwGachaPityTxt.text = $"再招募 {remPity} 次必出整卡英雄或大额高阶碎片！({curPityProg}/10)";

            for (int i = 0; i < 5; i++)
            {
                int hIdx = GetRadioFeaturedHeroIndex(i);
                if (hIdx >= _sim.Heroes.Count) continue;
                HeroSpec h = _sim.Heroes[hIdx];
                _wwGachaFeatCards[i].sprite = ForestComicUiSkin.GetClippedCard(h.rarity);
                ApplyHeroFullBodySprite(_wwGachaFeatBusts[i], h);
                _wwGachaFeatBusts[i].color = Color.white;
                _wwGachaFeatFactions[i].sprite = GetFactionCrestSprite(h.faction);
                _wwGachaFeatStates[i].text = h.unlocked
                    ? $"{h.name} ★{h.star}\n<color=#86EFAC>已入队</color>"
                    : $"{h.name}\n<color=#FDE047>碎片 {h.shards}/10</color>";
            }

            _wwGachaPull1BtnImg.sprite = tickets >= 1 ? ForestComicUiSkin.BtnBlue : ForestComicUiSkin.BtnGrey;
            _wwGachaPull1Lbl.text = $"呼叫 1 次\n消耗 1 卡 ({tickets}/1)";
            _wwGachaPull10BtnImg.sprite = tickets >= 10 ? ForestComicUiSkin.BtnOrange : ForestComicUiSkin.BtnBlue;
            _wwGachaPull10Lbl.text = $"十连呼叫\n消耗 10 卡 ({tickets}/10)";

            // --- 刷新 Tab 1: [山口行商] ---
            _cvSubTxt.text = $"下一班商队刷新: {Mathf.CeilToInt(_sim.CaravanRefreshTimer)}秒  ·  铜币 {_sim.Coins}  ·  口粮 {_sim.Get(SurvRes.Ration)}  ·  手稿 {_sim.Get(SurvRes.Blueprint)}";
            for (int i = 0; i < 3 && i < _sim.CaravanOffers.Count; i++)
            {
                CaravanOffer o = _sim.CaravanOffers[i];
                int rarity = Mathf.Max(1, ForestComicUiSkin.GetResRarity0To5(o.giveRes));
                _wwCvRarityFrames[i].sprite = ForestComicUiSkin.GetClippedCard(rarity);
                _cvIcons[i].sprite = ForestComicUiSkin.ResIcon(o.giveRes);
                _wwCvPackCntTxts[i].text = $"x{o.giveAmt}";
                _cvTitles[i].text = o.title;

                int own = _sim.Get(o.giveRes);
                _cvNeedTxts[i].color = ForestComicUiSkin.InkMuted;
                _cvNeedTxts[i].text = own >= o.giveAmt
                    ? $"装箱物资: {ForestSurvivalSim.ResName(o.giveRes)}  {own}/{o.giveAmt}"
                    : $"装箱物资: {ForestSurvivalSim.ResName(o.giveRes)}  <color=#E43A40>{own}</color>/{o.giveAmt}";
                _cvRewardTxts[i].text = $"报酬: 铜币 +{o.coinGain}  ·  工程手稿 +{o.blueprintGain}";

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
                    _cvTradeLbls[i].text = "获取物品";
                }
            }

            bool okCoins = _sim.Coins >= 180;
            _wwCvExTicketNeedTxt.text = okCoins ? $"铜币 {_sim.Coins}/180" : $"铜币 <color=#E43A40>{_sim.Coins}</color>/180";
            _wwCvExTicketBtnImg.sprite = okCoins ? ForestComicUiSkin.BtnOrange : ForestComicUiSkin.BtnGrey;
            _wwCvExTicketBtnLbl.text = "180铜币兑换";

            int ratOwn = _sim.Get(SurvRes.Ration);
            bool okRat = ratOwn >= 6;
            _wwCvExBookNeedTxt.text = okRat ? $"熟食口粮 {ratOwn}/6" : $"熟食口粮 <color=#E43A40>{ratOwn}</color>/6";
            _wwCvExBookBtnImg.sprite = okRat ? ForestComicUiSkin.BtnBlue : ForestComicUiSkin.BtnGrey;
            _wwCvExBookBtnLbl.text = "6份口粮兑换";
        }

        void OnPerformRadioRecruitClicked(int pullCount)
        {
            List<GachaPullResult> pulls = _sim.PerformRadioRecruit(pullCount, out string errMsg);
            if (pulls == null || pulls.Count == 0)
            {
                SpawnScreenCenterBanner(!string.IsNullOrEmpty(errMsg) ? errMsg : "电台招募卡不足！可切至【山口行商】兑换或前往闯关获取！");
                return;
            }

            RefreshCaravanModal();

            var displayItems = new CongratsDisplayItem[Mathf.Min(MaxCongratsTiles, pulls.Count)];
            for (int i = 0; i < displayItems.Length; i++)
            {
                GachaPullResult p = pulls[i];
                HeroSpec h = (p.heroIdx >= 0 && p.heroIdx < _sim.Heroes.Count) ? _sim.Heroes[p.heroIdx] : null;
                Sprite sp;
                int dispCnt;
                string ownStr;
                string descStr;

                if (p.isFullHero && p.isNewUnlock && h != null)
                {
                    sp = GetHeroFullBodySprite(h);
                    dispCnt = 1;
                    ownStr = $"已招募入队 · {ForestSurvivalSim.GetFactionName(h.faction)}";
                    descStr = $"★ 新幸存者英雄【{h.name}】响应短波号召正式入队！绝技：{h.skillName}";
                }
                else if (p.shardCount > 0 && h != null)
                {
                    sp = GetHeroJigsawShardSprite(h);
                    dispCnt = p.shardCount;
                    ownStr = $"当前碎片:  {h.shards}";
                    descStr = p.isFullHero
                        ? $"抽中已入队【{h.name}】整卡，自动折算为 {p.shardCount} 片专属拼图碎片助力升星！"
                        : $"用于【{h.name}】点亮星瓣或集满 10 片合成招募。";
                }
                else
                {
                    sp = GetEconArt(p.iconKey, GetEconResIcon(p.bonusItem));
                    dispCnt = Mathf.Max(1, p.bonusCount);
                    ownStr = $"拥有:  {FormatEconCount(_sim.Get(p.bonusItem))}";
                    descStr = GetRewardFlavorDescription(p.bonusItem, false);
                }

                displayItems[i] = new CongratsDisplayItem
                {
                    res = p.bonusItem,
                    count = dispCnt,
                    icon = sp,
                    label = p.label,
                    rarity0to5 = Mathf.Clamp(p.rarity0to5, 1, 5),
                    ownSummary = ownStr,
                    desc = descStr,
                };
            }

            ShowCongratsDisplayItems($"短波广播站 {pulls.Count} 连呼叫结果", displayItems);
        }

        void OnBuyCaravanRecruitTicketClicked()
        {
            if (_sim.Coins < 180)
            {
                SpawnScreenCenterBanner("营地铜币不足 180！可先完成居民需求单或装车交割商队货物赚取铜币！");
                return;
            }
            _sim.Coins -= 180;
            _sim.Add(SurvRes.RecruitTicket, 1, notifyToast: false);
            RefreshCaravanModal();
            ShowCongratsRewardModal("山口行商特供兑换", new[]
            {
                (SurvRes.RecruitTicket, 1, GetEconResIcon(SurvRes.RecruitTicket), "电台招募卡"),
            });
        }

        void OnBuyCaravanSkillBooksClicked()
        {
            if (_sim.Get(SurvRes.Ration) < 6)
            {
                SpawnScreenCenterBanner("熟食口粮不足 6 份！可前往【野味炊事房】烹制料理补充口粮！");
                return;
            }
            _sim.Add(SurvRes.Ration, -6);
            _sim.Add(SurvRes.SkillBook, 5, notifyToast: false);
            RefreshCaravanModal();
            ShowCongratsRewardModal("山口行商特供兑换", new[]
            {
                (SurvRes.SkillBook, 5, GetEconResIcon(SurvRes.SkillBook), "战术技能书"),
            });
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
            int gainCoins = o.coinGain;
            int gainBp = o.blueprintGain;
            string offerTitle = o.title;
            if (_sim.TryTradeCaravanOffer(idx, out string msg))
            {
                RefreshCaravanModal();
                ShowCongratsRewardModal($"{offerTitle} · 大宗贸易交割完成", new[]
                {
                    (SurvRes.Wood, gainCoins, ForestComicUiSkin.GetArt("ico_coin"), "营地铜币"),
                    (SurvRes.Blueprint, gainBp, ForestComicUiSkin.ResIcon(SurvRes.Blueprint), "工程手稿"),
                });
            }
            else
            {
                SpawnWorldRisingText(msg, new Vector2(22.6f, 21.2f));
            }
        }

        // =====================================================================
        // 4B. 全屏危机预警海报《尸潮来袭》
        // =====================================================================
        void BuildCrisisHordeModal()
        {
            _crisisModal = ForestUiWidgets.Stretch("CrisisHordeModal", _canvasRt).gameObject;
            Image bg = _crisisModal.AddComponent<Image>();
            bg.sprite = ForestComicUiSkin.GetArt("crisis_horde_bg", ForestComicUiSkin.WwHeaderShell);
            bg.color = Color.white;

            ForestUiWidgets.MakeText("BigTitle", _crisisModal.transform, "尸潮来袭", 58,
                ForestComicUiSkin.InkHeaderLight, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -78f), new Vector2(0f, 76f), outline: true);
            _crisisTimerBigTxt = ForestUiWidgets.MakeText("BigTimer", _crisisModal.transform, "00:29:59", 40,
                new Color32(255, 82, 76, 255), TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -152f), new Vector2(0f, 52f), outline: true);

            Image callout = ForestUiWidgets.MakeSlicedImage("Callout", _crisisModal.transform,
                ForestComicUiSkin.SpeechBubble, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f),
                new Vector2(78f, 36f), new Vector2(376f, 92f), false);
            Text qTxt = ForestUiWidgets.MakeText("Quip", callout.transform,
                "山下的尸群闻着炊烟爬上来了！守住山口防线可缴获【英雄经验】、【招募卡】与【工程手稿】！", 18,
                ForestComicUiSkin.InkDark, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(0f, 6f), new Vector2(-26f, -18f), outline: false);
            SetOutlineEnabled(qTxt, false);

            Image t1 = ForestUiWidgets.MakeSlicedImage("PrepRow1", _crisisModal.transform,
                ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 372f), new Vector2(644f, 70f), false);
            ForestUiWidgets.MakeIcon("Ico", t1.transform, ForestComicUiSkin.ResIcon(SurvRes.Ration),
                new Vector2(-276f, 0f), new Vector2(46f, 46f));
            _crisisTask1Txt = ForestUiWidgets.MakeText("Txt", t1.transform, "储备 16 份熟食口粮 (10/16)", 20,
                Color.white, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(16f, 0f), new Vector2(-180f, 0f), outline: true);
            ForestUiWidgets.MakeJellyButton("Go1", t1.transform, ForestComicUiSkin.BtnBlue, "前往", 19,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(104f, 48f), () =>
                {
                    CloseAllModals();
                    CampNode k = _sim.Find("kitchen");
                    if (k != null && k.state == NodeState.Active) OpenWorkshopDrawer(k);
                    else JumpToNodeById("kitchen");
                }, out _);

            Image t2 = ForestUiWidgets.MakeSlicedImage("PrepRow2", _crisisModal.transform,
                ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 290f), new Vector2(644f, 70f), false);
            ForestUiWidgets.MakeIcon("Ico", t2.transform, ForestComicUiSkin.GetArt("hunter_tower", ForestComicUiSkin.RedDotBadge),
                new Vector2(-276f, 0f), new Vector2(48f, 48f));
            _crisisTask2Txt = ForestUiWidgets.MakeText("Txt", t2.transform, "建造捕猎哨塔或尖木拒马 (0/1)", 20,
                Color.white, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(16f, 0f), new Vector2(-180f, 0f), outline: true);
            ForestUiWidgets.MakeJellyButton("Go2", t2.transform, ForestComicUiSkin.BtnBlue, "前往", 19,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(104f, 48f),
                () => JumpToNodeById("barricade"), out _);

            Image rwBox = ForestUiWidgets.MakeSlicedImage("CrisisRewards", _crisisModal.transform,
                ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 118f), new Vector2(488f, 154f), false);
            ForestUiWidgets.MakeText("Hdr", rwBox.transform, "击退尸潮战利品", 19, ForestComicUiSkin.AccentGold,
                TextAnchor.UpperCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -8f), new Vector2(0f, 28f), outline: true);
            MakeWwClippedLootBadge(rwBox.transform, 2, GetEconResIcon(SurvRes.HeroExp), "600", new Vector2(-132f, -14f), 96f);
            MakeWwClippedLootBadge(rwBox.transform, 4, GetEconResIcon(SurvRes.RecruitTicket), "2", new Vector2(0f, -14f), 96f);
            MakeWwClippedLootBadge(rwBox.transform, 3, ForestComicUiSkin.ResIcon(SurvRes.Blueprint), "3", new Vector2(132f, -14f), 96f);

            MakeWwBackRedButton(_crisisModal.transform, CloseAllModals, new Vector2(20f, 20f), new Vector2(100f, 74f));
            ForestUiWidgets.MakeJellyButton("BtnFightNow", _crisisModal.transform, ForestComicUiSkin.BtnOrange, "立即迎击尸潮", 24,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(28f, 22f), new Vector2(268f, 70f),
                StartWaveDefenseBattle, out _);

            _crisisModal.SetActive(false);
        }

        static void MakeWwClippedLootBadge(Transform parent, int rarity, Sprite ico, string count, Vector2 pos, float size)
        {
            Image slot = ForestUiWidgets.MakeSlicedImage("ClippedLoot", parent,
                ForestComicUiSkin.GetClippedCard(rarity), Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                pos, new Vector2(size, size), false);
            ForestUiWidgets.MakeIcon("Ico", slot.transform, ico, new Vector2(0f, 6f), new Vector2(size * 0.62f, size * 0.62f));
            ForestUiWidgets.MakeText("Cnt", slot.transform, count, 19, Color.white, TextAnchor.LowerRight,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(-7f, 5f), new Vector2(-12f, -8f), outline: true);
        }

        public void OpenCrisisPoster()
        {
            CloseAllModals();
            if (_wwCongratsModal != null) _wwCongratsModal.SetActive(false);
            _sim.HordePosterShown = true;
            _crisisModal.SetActive(true);
            RefreshCrisisPoster();
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

        // =====================================================================
        // 4C. 水世界品质策略战场四大界面：
        //     1) 战前关卡海报 + 巡逻挂机箱 + 战利品详情浮窗
        //     2) 深青战术粉笔黑板九宫格布阵 + 阵营光环浮窗 + 6阵型图谱选择浮窗
        //     3) 胶囊净空战斗 HUD
        //     4) MVP 连战结算
        // =====================================================================
        void BuildWaveDefenseModalsAndHud()
        {
            BuildStagePosterModalWithPatrolChest();
            BuildChalkboardTacticalFormationModal();
            BuildArenaCombatHudLayer();
            BuildArenaResultSettlementModal();
        }

        // ---------------------------------------------------------------------
        // 1. 【山口闯关 · 战前关卡海报 + 哨卡巡逻挂机箱】
        // ---------------------------------------------------------------------
        void BuildStagePosterModalWithPatrolChest()
        {
            _wavePrepModal = MakeModalMask("StagePosterModal", 0.80f, CloseAllModals);
            var spMask = _wavePrepModal.GetComponent<Image>();
            if (spMask != null) spMask.color = new Color32(18, 24, 50, 218);

            Image pCard = ForestUiWidgets.MakeSlicedImage("Card", _wavePrepModal.transform,
                ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(676f, 996f), true);

            Image stageBadge = ForestUiWidgets.MakeSlicedImage("StageBadge", pCard.transform,
                ForestComicUiSkin.RarityRibbon(4), Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(18f, -14f), new Vector2(166f, 38f), false);
            _spStageBadgeTxt = ForestUiWidgets.MakeText("Txt", stageBadge.transform, "第 1 / 40 关", 17,
                Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

            ForestUiWidgets.MakeText("Title", pCard.transform, "山口哨卡闯关", 28,
                ForestComicUiSkin.InkHeaderLight, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(24f, -12f), new Vector2(320f, 40f), outline: true);

            ForestUiWidgets.MakeJellyButton("Close", pCard.transform, ForestComicUiSkin.BtnRed, "×", 25,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -11f), new Vector2(46f, 46f), CloseAllModals, out _);

            Image pSheet = ForestUiWidgets.MakeSlicedImage("LightSheet", pCard.transform,
                ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -64f), new Vector2(640f, 830f), false);

            // A. 关卡首领与威胁情报横幅 (608x128)
            Image stageBanner = ForestUiWidgets.MakeSlicedImage("StageBanner", pSheet.transform,
                ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -12f), new Vector2(608f, 128f), false);

            Image bossCard = ForestUiWidgets.MakeSlicedImage("BossClippedCard", stageBanner.transform,
                ForestComicUiSkin.ClippedCardRed, Color.white,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(16f, 0f), new Vector2(96f, 96f), false);
            _spBossIconImg = ForestUiWidgets.MakeIcon("ZomIco", bossCard.transform,
                ForestComicUiSkin.GetArt("bust_zombie_boss", ForestComicUiSkin.GetUiArt("ww_dock_battle")),
                Vector2.zero, new Vector2(74f, 74f));

            _spStageTitleTxt = ForestUiWidgets.MakeText("StageTitle", stageBanner.transform, "第 1 关 · 山口游尸", 22,
                ForestComicUiSkin.AccentGold, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(130f, -12f), new Vector2(-146f, 32f), outline: true);

            _spStageSubTxt = ForestUiWidgets.MakeText("StageSub", stageBanner.transform, "清剿逼近断桥防线的先遣尸群", 15,
                ForestComicUiSkin.InkHeaderLight, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(130f, -46f), new Vector2(-146f, 26f), outline: true);

            _spThreatTag1Bg = ForestUiWidgets.MakeSlicedImage("ThreatTag1", stageBanner.transform,
                ForestComicUiSkin.BtnRed, Color.white,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(130f, 12f), new Vector2(184f, 34f), false);
            _spThreatTag1Txt = ForestUiWidgets.MakeText("T1", _spThreatTag1Bg.transform, "[常规哨卡战]", 14,
                Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

            _spThreatTag2Bg = ForestUiWidgets.MakeSlicedImage("ThreatTag2", stageBanner.transform,
                ForestComicUiSkin.BtnOrange, Color.white,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(326f, 12f), new Vector2(226f, 34f), false);
            _spThreatTag2Txt = ForestUiWidgets.MakeText("T2", _spThreatTag2Bg.transform, "[正面前排推进战]", 14,
                Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

            // B. 敌我战力对比浅蓝框 (608x80)
            Image pwrBanner = ForestUiWidgets.MakeSlicedImage("PowerCompareBanner", pSheet.transform,
                ForestComicUiSkin.WwSubPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -150f), new Vector2(608f, 80f), false);
            ForestUiWidgets.MakeIcon("PwrFist", pwrBanner.transform,
                ForestComicUiSkin.GetUiArt("ww_badge_power_fist"),
                new Vector2(-256f, 14f), new Vector2(34f, 34f));
            _spPowerCompareTxt = ForestUiWidgets.MakeText("PwrVs", pwrBanner.transform,
                "我方小队战力 360   VS   敌方推荐战力 220", 18, ForestComicUiSkin.InkDark,
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(12f, -6f), new Vector2(-64f, 32f), outline: false);
            SetOutlineEnabled(_spPowerCompareTxt, false);

            _spPowerStatePillBg = ForestUiWidgets.MakeSlicedImage("PwrStatePill", pwrBanner.transform,
                ForestComicUiSkin.BtnGreen, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 7f), new Vector2(288f, 32f), false);
            _spPowerStateTxt = ForestUiWidgets.MakeText("StateTxt", _spPowerStatePillBg.transform,
                "★ 战力压制 · 稳操胜券", 15, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

            // C. 已上阵小队预览与阵营光环条 (608x224)
            Image sqBox = ForestUiWidgets.MakeSlicedImage("SquadPreviewBox", pSheet.transform,
                ForestComicUiSkin.WwSubPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -240f), new Vector2(608f, 224f), false);
            _spSquadSummaryTxt = ForestUiWidgets.MakeText("SquadHdr", sqBox.transform,
                "已上阵 2/2 人  ·  当前阵型：【默认防线阵】", 16, ForestComicUiSkin.InkDark,
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -6f), new Vector2(-20f, 26f), outline: false);
            SetOutlineEnabled(_spSquadSummaryTxt, false);

            for (int i = 0; i < 5; i++)
            {
                float px = -228f + i * 114f;
                RectTransform slotRt = ForestUiWidgets.MakeRect("PvHeroSlot_" + i, sqBox.transform,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(px, -34f), new Vector2(104f, 132f));

                Image pvCard = ForestUiWidgets.MakeSlicedImage("ClippedFrame", slotRt,
                    ForestComicUiSkin.ClippedCardBlue, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    Vector2.zero, new Vector2(88f, 88f), true);
                _spPreviewFrames[i] = pvCard;
                pvCard.gameObject.AddComponent<Button>().onClick.AddListener(OpenTacticalFormationModal);

                Image msk = ForestUiWidgets.MakeSlicedImage("Mask", pvCard.transform, null, new Color(1f, 1f, 1f, 0.02f),
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-10f, -10f), false);
                msk.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                _spPreviewBusts[i] = ForestUiWidgets.MakeIcon("Bust", msk.transform,
                    ForestComicUiSkin.GetArt("bust_dachui"), new Vector2(0f, -3f), new Vector2(74f, 74f));
                _spPreviewLocks[i] = ForestUiWidgets.MakeIcon("Lock", pvCard.transform,
                    ForestComicUiSkin.GetArt("ico_lock"), Vector2.zero, new Vector2(32f, 32f));

                Image emptyBrk = ForestUiWidgets.MakeSlicedImage("EmptyBrackets", pvCard.transform,
                    ForestComicUiSkin.SelectionCornerBrackets, Color.white,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6f, 6f), false);
                _wwSpEmptyBrackets[i] = emptyBrk.gameObject;

                _spPreviewNames[i] = ForestUiWidgets.MakeText("Name", slotRt, "大锤 ★0", 13,
                    ForestComicUiSkin.InkDark, TextAnchor.MiddleCenter,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 20f), new Vector2(6f, 20f), outline: false);
                SetOutlineEnabled(_spPreviewNames[i], false);
                _spPreviewRoles[i] = ForestUiWidgets.MakeText("Role", slotRt, "前排·重卫", 12,
                    ForestComicUiSkin.InkMuted, TextAnchor.MiddleCenter,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 2f), new Vector2(6f, 18f), outline: false);
                SetOutlineEnabled(_spPreviewRoles[i], false);
            }

            Image synStrip = ForestUiWidgets.MakeSlicedImage("SynergyStrip", sqBox.transform,
                ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 7f), new Vector2(580f, 40f), false);
            _spSynergyStripTxt = ForestUiWidgets.MakeText("SynTxt", synStrip.transform, "阵营光环：", 14,
                ForestComicUiSkin.AccentGold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16f, -4f), outline: true);

            // D. 独立战利品切角品质卡（点击任意奖励弹出详情卡！）与里程碑横幅 (608x204)
            Image rwPanel = ForestUiWidgets.MakeSlicedImage("PosterRwPanel", pSheet.transform,
                ForestComicUiSkin.WwSubPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -474f), new Vector2(608f, 204f), false);
            Text rwHdr = ForestUiWidgets.MakeText("RwHdr", rwPanel.transform, "本关通关战利品（点击图标查看详情）", 17,
                ForestComicUiSkin.InkDark, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -6f), new Vector2(-20f, 26f), outline: false);
            SetOutlineEnabled(rwHdr, false);

            int[] defaultLootRarities = { 2, 4, 2, 3 };
            for (int r = 0; r < 4; r++)
            {
                int rIdx = r;
                float rx = -207f + r * 138f;
                Image rSlot = ForestUiWidgets.MakeSlicedImage("PRw_" + r, rwPanel.transform,
                    ForestComicUiSkin.GetClippedCard(defaultLootRarities[r]), Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(rx, -36f), new Vector2(114f, 102f), true);
                _wwSpLootFrames[r] = rSlot;
                var rBtn = rSlot.gameObject.AddComponent<Button>();
                rBtn.targetGraphic = rSlot;
                rBtn.onClick.AddListener(() => OpenPosterRewardItemPopup(rIdx));
                _wwSpLootBtns[r] = rBtn;

                _spRewardIcons[r] = ForestUiWidgets.MakeIcon("Ico", rSlot.transform,
                    GetEconResIcon(SurvRes.HeroExp), new Vector2(0f, 8f), new Vector2(60f, 60f));
                _spRewardTxts[r] = ForestUiWidgets.MakeText("Cnt", rSlot.transform, "+300", 18,
                    Color.white, TextAnchor.LowerRight,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                    new Vector2(-8f, 6f), new Vector2(-12f, -8f), outline: true);
            }

            _spMilestoneBg = ForestUiWidgets.MakeSlicedImage("MilestoneBanner", rwPanel.transform,
                ForestComicUiSkin.BtnOrange, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 8f), new Vector2(580f, 44f), false);
            _spMilestoneTxt = ForestUiWidgets.MakeText("MileTxt", _spMilestoneBg.transform, "★ 首通里程碑大奖", 15,
                Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-18f, -4f), outline: true);

            // E. 新增【哨卡巡逻挂机箱】横幅 (608x130, 随时领取英雄经验 + 铜币 + 技能书！)
            Image patrolBox = ForestUiWidgets.MakeSlicedImage("PatrolIdleChestStrip", pSheet.transform,
                ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 12f), new Vector2(608f, 128f), false);

            RectTransform chestIcoRt = ForestUiWidgets.MakeRect("PatrolChestIco", patrolBox.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(58f, 0f), new Vector2(88f, 88f));
            var chestImg = chestIcoRt.gameObject.AddComponent<Image>();
            chestImg.sprite = GetEconArt("ico_patrol_chest", ForestComicUiSkin.GetUiArt("ww_order_gift_box"));
            chestImg.preserveAspect = true;
            chestIcoRt.gameObject.AddComponent<Button>().onClick.AddListener(OnClaimPatrolChestClicked);
            _wwPatrolChestAnim = chestIcoRt.gameObject.AddComponent<WwOrdersUiAnimator>();
            _wwPatrolChestAnim.basePos = new Vector2(58f, 0f);
            _wwPatrolChestAnim.bobAmpY = 4.5f;
            _wwPatrolChestAnim.bobTiltDeg = 5.0f;

            Image pDot = ForestUiWidgets.MakeIcon("PatrolDot", chestIcoRt, ForestComicUiSkin.RedDotBadge,
                new Vector2(30f, 30f), new Vector2(26f, 26f));
            ForestUiWidgets.MakeText("Bang", pDot.transform, "!", 16, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: false);
            _wwPatrolChestDotGo = pDot.gameObject;

            _wwPatrolStatusTxt = ForestUiWidgets.MakeText("PatrolHdr", patrolBox.transform,
                "哨卡巡逻挂机箱（已挂机 0 分钟 · 第1关）", 16,
                ForestComicUiSkin.AccentGold, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(110f, -18f), new Vector2(-262f, 28f), outline: true);

            _wwPatrolYieldTxt = ForestUiWidgets.MakeText("PatrolYield", patrolBox.transform,
                "经验 +0 · 铜币 +0", 14,
                new Color32(204, 236, 255, 255), TextAnchor.LowerLeft,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f),
                new Vector2(110f, 18f), new Vector2(-262f, 44f), outline: true);

            Button patClaimBtn = ForestUiWidgets.MakeJellyButton("BtnClaimPatrol", patrolBox.transform,
                ForestComicUiSkin.BtnGreen, "领取收益", 20,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(142f, 62f),
                OnClaimPatrolChestClicked, out _wwPatrolClaimLbl);
            _wwPatrolClaimBtnImg = patClaimBtn.GetComponent<Image>();

            // 底部按钮行
            MakeWwBackRedButton(pCard.transform, CloseAllModals, new Vector2(18f, 14f), new Vector2(98f, 70f));
            ForestUiWidgets.MakeJellyButton("BtnPosterFormation", pCard.transform, ForestComicUiSkin.BtnBlue, "阵容布阵", 22,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-66f, 16f), new Vector2(212f, 66f),
                OpenTacticalFormationModal, out _);
            ForestUiWidgets.MakeJellyButton("BtnPosterFight", pCard.transform, ForestComicUiSkin.BtnRed, "开 战", 25,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(176f, 16f), new Vector2(242f, 66f),
                StartWaveDefenseBattle, out _);

            // 海报战利品点击详情白卡浮窗 (1:1 Ref media_1795609734848)
            BuildPosterRewardItemPopupOverlay(_wavePrepModal.transform);

            _wavePrepModal.SetActive(false);
        }

        void BuildPosterRewardItemPopupOverlay(Transform posterRoot)
        {
            RectTransform maskRt = ForestUiWidgets.Stretch("PosterRewardItemPopup", posterRoot);
            var mImg = maskRt.gameObject.AddComponent<Image>();
            mImg.color = new Color32(12, 16, 34, 156);
            mImg.raycastTarget = true;
            maskRt.gameObject.AddComponent<Button>().onClick.AddListener(() => _wwPosterItemPopup.SetActive(false));
            _wwPosterItemPopup = maskRt.gameObject;

            Image outer = ForestUiWidgets.MakeSlicedImage("Outer", maskRt, ForestComicUiSkin.DialogOuter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -64f), new Vector2(564f, 384f), true);
            Image sheet = ForestUiWidgets.MakeSlicedImage("Sheet", outer.transform, ForestComicUiSkin.WwLightSheet, Color.white,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-10f, -10f), false);

            _wwPosterPopCard = ForestUiWidgets.MakeSlicedImage("ItemCard", sheet.transform, ForestComicUiSkin.ClippedCardBlue, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(26f, -24f), new Vector2(108f, 108f), false);
            _wwPosterPopIcon = ForestUiWidgets.MakeIcon("Ico", _wwPosterPopCard.transform,
                GetEconResIcon(SurvRes.HeroExp), Vector2.zero, new Vector2(76f, 76f));

            _wwPosterPopNameTxt = ForestUiWidgets.MakeText("Name", sheet.transform, "英雄经验", 26, ForestComicUiSkin.InkDark,
                TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(154f, -32f), new Vector2(-178f, 36f), outline: false);
            SetOutlineEnabled(_wwPosterPopNameTxt, false);

            _wwPosterPopOwnTxt = ForestUiWidgets.MakeText("Own", sheet.transform, "拥有: 600", 20, ForestComicUiSkin.InkMuted,
                TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(154f, -80f), new Vector2(-178f, 30f), outline: false);
            SetOutlineEnabled(_wwPosterPopOwnTxt, false);

            ForestUiWidgets.MakeSlicedImage("Div", sheet.transform, null, ForestComicUiSkin.DividerLine,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -148f), new Vector2(-48f, 2f), false);

            _wwPosterPopDescTxt = ForestUiWidgets.MakeText("Desc", sheet.transform, "用于提升英雄等级", 20, ForestComicUiSkin.InkMuted,
                TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -166f), new Vector2(-56f, 96f), outline: false);
            SetOutlineEnabled(_wwPosterPopDescTxt, false);
            _wwPosterPopDescTxt.lineSpacing = 1.25f;

            ForestUiWidgets.MakeJellyButton("BtnGoAcquire", sheet.transform, ForestComicUiSkin.BtnGreen, "前往获取", 21,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(196f, 58f), () =>
                {
                    _wwPosterItemPopup.SetActive(false);
                    _wwPosterPopGoCb?.Invoke();
                }, out _);

            _wwPosterItemPopup.SetActive(false);
        }

        // ---------------------------------------------------------------------
        // 2. 【深青战术粉笔黑板九宫格布阵】 (1:1 Ref media_1795609719722 & e8e8c63a)
        // ---------------------------------------------------------------------
        void BuildChalkboardTacticalFormationModal()
        {
            RectTransform rootRt = ForestUiWidgets.Stretch("TacticalFormationModal", _canvasRt);
            var backdrop = rootRt.gameObject.AddComponent<Image>();
            backdrop.color = new Color32(22, 55, 72, 255); // #163748 深青战术黑板底色
            backdrop.raycastTarget = true;
            _tacticalFormationModal = rootRt.gameObject;

            // --- A. 上半屏深青战术粉笔黑板舞台 (720x742, y = 0 .. -742) ---
            RectTransform boardStageRt = ForestUiWidgets.MakeRect("ChalkboardStage", rootRt,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(720f, 742f));

            // 顶部主阵容与战力条（严格 x <= 388, top 116px 内 100% 避让微信右上角胶囊！）
            Image topRibbon = ForestUiWidgets.MakeSlicedImage("MainSquadHeaderBar", boardStageRt,
                ForestComicUiSkin.WwHeaderShell, new Color(1f, 1f, 1f, 0.94f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -20f), new Vector2(216f, 44f), false);
            _tfDeployCountTxt = ForestUiWidgets.MakeText("DeployCnt", topRibbon.transform,
                "主阵容英雄: 2/2", 20, Color.white,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(-10f, 0f), outline: true);

            Image pwrPill = ForestUiWidgets.MakeSlicedImage("BoardPowerPill", boardStageRt,
                ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(240f, -20f), new Vector2(148f, 44f), false);
            ForestUiWidgets.MakeIcon("PwrFist", pwrPill.transform,
                ForestComicUiSkin.GetUiArt("ww_badge_power_fist"),
                new Vector2(-50f, 0f), new Vector2(36f, 36f));
            _tfTotalPowerTxt = ForestUiWidgets.MakeText("TotPwr", pwrPill.transform, "13727", 21,
                ForestComicUiSkin.AccentGold, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(18f, 0f), new Vector2(-36f, 0f), outline: true);

            // 黑板粉笔装饰横线 (置于 y = -74, 左段 x <= 388)
            ForestUiWidgets.MakeSlicedImage("ChalkTopRule", boardStageRt, null, new Color32(78, 92, 198, 210),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                new Vector2(16f, -72f), new Vector2(372f, 4f), false);

            // 粉笔砖墙/棋盘网格衬底框 (636x456 at y = -118)
            Image chalkGridOuter = ForestUiWidgets.MakeSlicedImage("ChalkGridFrame", boardStageRt,
                null, new Color32(28, 70, 92, 235),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -118f), new Vector2(648f, 462f), false);

            string[] colTitles = { "后排 (远程/支援)", "中排 (火力/策应)", "前排 (迎敌抗伤 →)" };
            Color32[] colColors =
            {
                new Color32(158, 224, 255, 255),
                new Color32(255, 226, 132, 255),
                new Color32(255, 184, 152, 255),
            };
            for (int c = 0; c < 3; c++)
            {
                float cx = -206f + c * 206f;
                ForestUiWidgets.MakeText("ColHdr_" + c, chalkGridOuter.transform, colTitles[c], 15, colColors[c],
                    TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(cx, -8f), new Vector2(198f, 26f), outline: true);
            }

            string[] rowNames = { "上路", "中路", "下路" };
            string[] colShortNames = { "后排", "中排", "前排" };
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    int slotIdx = r * 3 + c;
                    float tx = -206f + c * 206f;
                    float ty = -40f - r * 136f;

                    // 粉笔蓝白细边格位
                    Image chalkBorder = ForestUiWidgets.MakeSlicedImage($"ChalkCell_{r}_{c}", chalkGridOuter.transform,
                        null, new Color32(84, 142, 172, 225),
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(tx, ty), new Vector2(196f, 128f), true);
                    Image cellInner = ForestUiWidgets.MakeSlicedImage("Inner", chalkBorder.transform,
                        null, new Color32(26, 66, 86, 255),
                        Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4f, -4f), false);
                    _tfTileFrames[slotIdx] = cellInner;
                    chalkBorder.gameObject.AddComponent<Button>().onClick.AddListener(() => OnTacticalGridTileClicked(slotIdx));

                    // 已部署角色专属亮黄战术地板框（1:1 还原《疯狂水世界》黑板脚下亮黄方框，角色上半身破框高挑挺立！）
                    Image yellowRim = ForestUiWidgets.MakeSlicedImage("OccupiedYellowRim", chalkBorder.transform,
                        null, new Color32(255, 208, 52, 255),
                        new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                        new Vector2(0f, 6f), new Vector2(158f, 76f), false);
                    ForestUiWidgets.MakeSlicedImage("YellowCutout", yellowRim.transform,
                        null, new Color32(34, 88, 114, 255),
                        Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8f, -8f), false);
                    _wwTfTileYellowBorders[slotIdx] = yellowRim.gameObject;

                    _tfTileLaneHints[slotIdx] = ForestUiWidgets.MakeText("LaneHint", chalkBorder.transform,
                        $"{rowNames[r]}·{colShortNames[c]}", 12, new Color32(138, 188, 212, 210),
                        TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(8f, -6f), new Vector2(94f, 20f), outline: false);

                    Image buffBg = ForestUiWidgets.MakeSlicedImage("BuffTag", chalkBorder.transform,
                        ForestComicUiSkin.BtnOrange, Color.white,
                        new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-4f, -4f), new Vector2(76f, 20f), false);
                    _tfTileBuffGos[slotIdx] = buffBg.gameObject;
                    _tfTileBuffBgs[slotIdx] = buffBg;
                    _tfTileBuffTxts[slotIdx] = ForestUiWidgets.MakeText("Txt", buffBg.transform, "护甲+80", 11,
                        Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

                    // 脚底暗椭圆阴影 + 全身像站立容器 (112x106 稳站黄色地板框中央偏左避让右上增益签) + 左上阵营微章 + 底部居中 `XX级`
                    RectTransform shRt = ForestUiWidgets.MakeRect("BootShadow", chalkBorder.transform,
                        new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
                        new Vector2(-5f, 22f), new Vector2(80f, 16f));
                    Image shImg = shRt.gameObject.AddComponent<Image>();
                    shImg.sprite = ForestComicUiSkin.WhiteSoftCircle;
                    shImg.preserveAspect = false;
                    shImg.color = new Color(0.03f, 0.08f, 0.12f, 0.50f);
                    shImg.raycastTarget = false;
                    _wwTfTileShadows[slotIdx] = shImg;

                    RectTransform pBox = ForestUiWidgets.MakeRect("HeroFigureBox", chalkBorder.transform,
                        new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.05f),
                        new Vector2(-5f, 20f), new Vector2(112f, 106f));
                    _tfTilePortraitBoxes[slotIdx] = pBox.gameObject;

                    var breather = pBox.gameObject.AddComponent<WwHeroIdleBreather>();
                    breather.baseAnchoredPos = new Vector2(-5f, 20f);
                    breather.phaseOffset = slotIdx * 0.85f;
                    breather.hopHeight = 10f;
                    _wwTfTileBreathers[slotIdx] = breather;

                    RectTransform figImgRt = ForestUiWidgets.Stretch("FullBody", pBox);
                    Image figImg = figImgRt.gameObject.AddComponent<Image>();
                    figImg.sprite = GetEconArt("fullbody_dachui", ForestComicUiSkin.GetArt("bust_dachui"));
                    figImg.preserveAspect = true;
                    figImg.raycastTarget = false;
                    _tfTileBusts[slotIdx] = figImg;

                    _wwTfTileFactionIcons[slotIdx] = ForestUiWidgets.MakeIcon("Fac", chalkBorder.transform,
                        GetFactionCrestSprite(HeroFaction.IronGuard), new Vector2(6f, -6f), new Vector2(22f, 22f),
                        anchor: new Vector2(0f, 1f));

                    _tfTileRoles[slotIdx] = ForestUiWidgets.MakeText("HeroMiniName", chalkBorder.transform, "大锤", 12,
                        ForestComicUiSkin.AccentGold, TextAnchor.UpperLeft,
                        new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(31f, -6f), new Vector2(68f, 20f), outline: true);

                    // 脚下等级标签 `26级`（锚定黄色底框下沿正中，1:1 还原水世界参考图）
                    _tfTileNames[slotIdx] = ForestUiWidgets.MakeText("HeroLvTag", chalkBorder.transform, "1级", 20,
                        Color.white, TextAnchor.LowerCenter,
                        new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                        new Vector2(0f, -1f), new Vector2(0f, 26f), outline: true);

                    _tfTileEmptyTxts[slotIdx] = ForestUiWidgets.MakeText("EmptyHint", chalkBorder.transform, "＋ 部署", 15,
                        new Color32(138, 188, 212, 200), TextAnchor.MiddleCenter,
                        Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                        new Vector2(0f, -6f), new Vector2(-12f, -24f), outline: false);

                    Image selBrk = ForestUiWidgets.MakeSlicedImage("SelBrackets", chalkBorder.transform,
                        ForestComicUiSkin.SelectionCornerBrackets, Color.white,
                        Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 8f), false);
                    _wwTfTileBrackets[slotIdx] = selBrk.gameObject;
                    selBrk.gameObject.SetActive(false);

                    Button rmBtn = ForestUiWidgets.MakeJellyButton("BtnRemove", chalkBorder.transform, ForestComicUiSkin.BtnRed, "×", 15,
                        new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-4f, 4f), new Vector2(28f, 26f),
                        () => OnTacticalTileRemoveClicked(slotIdx), out _);
                    _tfTileRemoveGos[slotIdx] = rmBtn.gameObject;
                }
            }

            // 黑板左下圆形【阵营光环】徽章 (1:1 Ref media_1795609721520)
            RectTransform auraBtnRt = ForestUiWidgets.MakeRect("BtnFactionAuraBadge", boardStageRt,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(28f, 32f), new Vector2(96f, 114f));
            _wwTfAuraCircleBg = ForestUiWidgets.MakeIcon("CircleBg", auraBtnRt,
                ForestComicUiSkin.CircleActionRing, new Vector2(0f, 12f), new Vector2(88f, 88f), raycast: true);
            _wwTfAuraCircleBg.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                if (_wwFormationPickerPopup != null) _wwFormationPickerPopup.SetActive(false);
                bool show = !_wwFactionAuraPopup.activeSelf;
                if (show) RefreshFactionAuraPopup();
                _wwFactionAuraPopup.transform.SetAsLastSibling();
                _wwFactionAuraPopup.SetActive(show);
            });
            _wwTfAuraCrestImg = ForestUiWidgets.MakeIcon("AuraCrest", _wwTfAuraCircleBg.transform,
                GetFactionCrestSprite(HeroFaction.IronGuard), new Vector2(0f, 10f), new Vector2(44f, 44f));
            _wwTfAuraStateTxt = ForestUiWidgets.MakeText("State", _wwTfAuraCircleBg.transform, "未激活", 16,
                Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), Vector2.zero, outline: true);
            _wwTfAuraSubLbl = ForestUiWidgets.MakeText("Sub", auraBtnRt, "阵营光环", 16,
                new Color32(218, 232, 248, 255), TextAnchor.LowerCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 0f), new Vector2(20f, 24f), outline: true);

            // 黑板中部阵型与收集全局加成提示条
            _tfFormationSummaryTxt = ForestUiWidgets.MakeText("BoardMidSummary", boardStageRt,
                "", 14, new Color32(174, 232, 255, 255), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 56f), new Vector2(448f, 66f), outline: true);

            // 黑板右下【当前阵型】方形芯片按钮 (1:1 Ref media_1795609721520)
            Image fmtChipBg = ForestUiWidgets.MakeSlicedImage("BtnCurrentFormationChip", boardStageRt,
                ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-24f, 34f), new Vector2(98f, 108f), true);
            fmtChipBg.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                if (_wwFactionAuraPopup != null) _wwFactionAuraPopup.SetActive(false);
                bool show = !_wwFormationPickerPopup.activeSelf;
                if (show) RefreshFormationPickerPopup();
                _wwFormationPickerPopup.transform.SetAsLastSibling();
                _wwFormationPickerPopup.SetActive(show);
            });
            _wwTfCurFmtChipIcon = ForestUiWidgets.MakeIcon("FmtChipIco", fmtChipBg.transform,
                GetEconArt("ico_fmt_default", ForestComicUiSkin.GetUiArt("ww_xp_star")),
                new Vector2(0f, 13f), new Vector2(74f, 74f));
            _wwTfCurFmtChipNameTxt = ForestUiWidgets.MakeText("FmtName", fmtChipBg.transform, "默认阵型", 14,
                ForestComicUiSkin.InkDark, TextAnchor.LowerCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 3f), new Vector2(0f, 22f), outline: false);
            SetOutlineEnabled(_wwTfCurFmtChipNameTxt, false);

            // --- B. 下半屏浅薰衣草选人面板 + 左上阵营石托筛选栏 + 底部操作台 (720x538) ---
            Image lowerRosterPanel = ForestUiWidgets.MakeSlicedImage("LowerLavenderSheet", rootRt,
                ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                Vector2.zero, new Vector2(0f, 526f), true);

            // 左上石质阵营筛选托盘 ([所有] [铁卫团] [山林盟] [废土科工])
            Image filterTray = ForestUiWidgets.MakeSlicedImage("FactionFilterTray", lowerRosterPanel.transform,
                ForestComicUiSkin.WwStoneTray, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                new Vector2(0f, 4f), new Vector2(368f, 62f), true);

            string[] tfFilterLabels = { "所有", "", "", "" };
            for (int f = 0; f < 4; f++)
            {
                int fIdx = f;
                float fx = 16f + f * 86f;
                Image fTab = ForestUiWidgets.MakeSlicedImage("TfFacTab_" + f, filterTray.transform,
                    ForestComicUiSkin.WwTabIdle, Color.white,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(fx, 0f), new Vector2(76f, 48f), true);
                _wwTfFilterTabImgs[f] = fTab;
                fTab.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                {
                    _wwTfFactionFilterIdx = fIdx;
                    RefreshTacticalFormationModal();
                });

                if (f == 0)
                {
                    _wwTfFilterTabLbls[f] = ForestUiWidgets.MakeText("Lbl", fTab.transform, tfFilterLabels[f], 18,
                        ForestComicUiSkin.InkDark, TextAnchor.MiddleCenter,
                        Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);
                    _wwTfFilterTabOutlines[f] = _wwTfFilterTabLbls[f].GetComponent<Outline>();
                    if (_wwTfFilterTabOutlines[f] != null) _wwTfFilterTabOutlines[f].enabled = false;
                }
                else
                {
                    ForestUiWidgets.MakeIcon("Crest", fTab.transform,
                        GetFactionCrestSprite((HeroFaction)(f - 1)), Vector2.zero, new Vector2(36f, 36f));
                }
            }

            _tfRosterHintTxt = ForestUiWidgets.MakeText("RosterSwapHint", lowerRosterPanel.transform,
                "点选英雄一键上阵/下阵，或选中后点上方黑板格调防", 16, ForestComicUiSkin.InkMuted,
                TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -44f), new Vector2(-48f, 30f), outline: false);
            SetOutlineEnabled(_tfRosterHintTxt, false);

            // 9 张方形英雄近景肖像卡（置于横向 ScrollRect 内：[所有] 顺滑横滚浏览全部 9 名英雄，阵营筛选精准左对齐 3 人！）
            _wwTfRosterScrollContentRt = MakeHorizontalScrollArea("TfHeroRosterScroll", lowerRosterPanel.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -82f), new Vector2(688f, 198f), 688f);

            for (int i = 0; i < MaxHeroUiSlots; i++)
            {
                int hIdx = i;
                float hx = 8f + i * 116f;

                RectTransform cardWrap = ForestUiWidgets.MakeRect("RosHeroWrap_" + i, _wwTfRosterScrollContentRt,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(hx, -4f), new Vector2(106f, 188f));
                _tfRosterCards[i] = cardWrap.gameObject;

                Image sqFrame = ForestUiWidgets.MakeSlicedImage("PortraitSquare", cardWrap,
                    ForestComicUiSkin.ClippedCardPurple, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    Vector2.zero, new Vector2(102f, 102f), true);
                _tfRosterFrames[i] = sqFrame;
                _wwTfRosterPortraitFrames[i] = sqFrame;
                sqFrame.gameObject.AddComponent<Button>().onClick.AddListener(() => OnRosterHeroCardClicked(hIdx));

                Image pMask = ForestUiWidgets.MakeSlicedImage("Mask", sqFrame.transform, null, new Color(1f, 1f, 1f, 0.04f),
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8f, -8f), false);
                pMask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                _tfRosterBusts[i] = ForestUiWidgets.MakeIcon("Bust", pMask.transform,
                    ForestComicUiSkin.GetArt("bust_dachui"), new Vector2(0f, -4f), new Vector2(94f, 94f));

                // 右上阵营微章 + 职业微章
                _wwTfRosterFactionIcons[i] = ForestUiWidgets.MakeIcon("FacIco", sqFrame.transform,
                    GetFactionCrestSprite(HeroFaction.IronGuard), new Vector2(-28f, -6f), new Vector2(22f, 22f),
                    anchor: new Vector2(1f, 1f));
                _wwTfRosterRoleIcons[i] = ForestUiWidgets.MakeIcon("RoleIco", sqFrame.transform,
                    GetRoleMiniSprite(HeroCombatRole.Guardian), new Vector2(-6f, -6f), new Vector2(22f, 22f),
                    anchor: new Vector2(1f, 1f));

                // 居中大号绿勾覆盖（上阵态醒目点亮！）
                Image bigChk = ForestUiWidgets.MakeIcon("DeployedGreenCheck", sqFrame.transform,
                    ForestComicUiSkin.GetUiArt("ww_check_green"), new Vector2(0f, -2f), new Vector2(58f, 58f));
                _wwTfRosterGreenChecks[i] = bigChk.gameObject;

                Image rBrk = ForestUiWidgets.MakeSlicedImage("SelBrackets", sqFrame.transform,
                    ForestComicUiSkin.SelectionCornerBrackets, Color.white,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f), false);
                _wwTfRosterBrackets[i] = rBrk.gameObject;

                _tfRosterNames[i] = ForestUiWidgets.MakeText("Name", cardWrap, "大锤 1级", 15,
                    ForestComicUiSkin.InkDark, TextAnchor.UpperCenter,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -108f), new Vector2(8f, 24f), outline: false);
                SetOutlineEnabled(_tfRosterNames[i], false);

                _tfRosterRoles[i] = ForestUiWidgets.MakeText("Role", cardWrap, "铁壁重卫", 13,
                    ForestComicUiSkin.InkMuted, TextAnchor.UpperCenter,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -132f), new Vector2(8f, 20f), outline: false);
                SetOutlineEnabled(_tfRosterRoles[i], false);

                _tfRosterPowers[i] = ForestUiWidgets.MakeText("Pwr", cardWrap, "战力 5146", 13,
                    ForestComicUiSkin.AccentGreen, TextAnchor.UpperCenter,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -154f), new Vector2(8f, 20f), outline: false);
                SetOutlineEnabled(_tfRosterPowers[i], false);
            }

            // 底部深灰蓝操作底座 (118px)
            Image botSlate = ForestUiWidgets.MakeSlicedImage("BottomFormationBar", lowerRosterPanel.transform,
                ForestComicUiSkin.WwHeaderShell, new Color32(48, 56, 78, 255),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                Vector2.zero, new Vector2(0f, 116f), true);

            MakeWwBackRedButton(botSlate.transform, OpenStagePosterModal, new Vector2(18f, 19f), new Vector2(102f, 76f));

            ForestUiWidgets.MakeText("AutoSaveHint", botSlate.transform, "自动保存布阵结果", 18,
                new Color32(204, 214, 236, 255), TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                new Vector2(134f, 0f), new Vector2(180f, 0f), outline: true);

            ForestUiWidgets.MakeJellyButton("BtnAutoDeploy", botSlate.transform, ForestComicUiSkin.WwTabIdle, "推荐阵容", 19,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-214f, 0f), new Vector2(156f, 68f),
                () =>
                {
                    _sim.AutoDeployRecommendedFormation();
                    _selectedFormationHeroIdx = -1;
                    RefreshTacticalFormationModal();
                    SpawnScreenCenterBanner("已按阵营共鸣与职业职责自动排布最优阵容！");
                }, out Text recBtnLbl);
            recBtnLbl.color = ForestComicUiSkin.InkDark;
            SetOutlineEnabled(recBtnLbl, false);

            ForestUiWidgets.MakeJellyButton("BtnSaveAndFight", botSlate.transform, ForestComicUiSkin.BtnGreen, "保存并开战", 21,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(184f, 68f),
                StartWaveDefenseBattle, out _);

            // 叠加浮窗 1：【阵营光环】三档规则说明卡 (1:1 Ref media_1795609719722)
            BuildFactionAuraPopupOverlay(rootRt);
            // 叠加浮窗 2：【战术阵型图谱】6 套收集+上阵选择器
            BuildFormationPickerPopupOverlay(rootRt);

            _tacticalFormationModal.SetActive(false);
        }

        void BuildFactionAuraPopupOverlay(Transform tfRoot)
        {
            RectTransform maskRt = ForestUiWidgets.Stretch("FactionAuraPopup", tfRoot);
            var mImg = maskRt.gameObject.AddComponent<Image>();
            mImg.color = new Color32(10, 16, 28, 128);
            mImg.raycastTarget = true;
            maskRt.gameObject.AddComponent<Button>().onClick.AddListener(() => _wwFactionAuraPopup.SetActive(false));
            _wwFactionAuraPopup = maskRt.gameObject;

            Image outer = ForestUiWidgets.MakeSlicedImage("Outer", maskRt, ForestComicUiSkin.DialogOuter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -18f), new Vector2(560f, 382f), true);
            Image sheet = ForestUiWidgets.MakeSlicedImage("Sheet", outer.transform, ForestComicUiSkin.WwLightSheet, Color.white,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-10f, -10f), false);

            _wwAuraPopupHdrTxt = ForestUiWidgets.MakeText("Hdr", sheet.transform, "同阵营协同光环", 23, ForestComicUiSkin.InkDark,
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -12f), new Vector2(-24f, 34f), outline: false);
            SetOutlineEnabled(_wwAuraPopupHdrTxt, false);

            _wwAuraPopupCountTxt = ForestUiWidgets.MakeText("Cnts", sheet.transform, "", 16, ForestComicUiSkin.AccentPurple,
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -48f), new Vector2(-24f, 26f), outline: false);
            SetOutlineEnabled(_wwAuraPopupCountTxt, false);

            ForestUiWidgets.MakeSlicedImage("Div", sheet.transform, null, ForestComicUiSkin.DividerLine,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -80f), new Vector2(-36f, 2f), false);

            Color32[] starTints =
            {
                new Color32(86, 168, 255, 255),
                new Color32(204, 98, 255, 255),
                new Color32(255, 196, 56, 255),
            };
            string[] tierRules =
            {
                "上阵 2 名同阵营英雄：全队生命、攻击、技能强度 +10%（2+2双阵营共鸣 +18%）",
                "上阵 3 名同阵营史诗及以上英雄：全队生命、攻击、技能强度 +15%",
                "上阵 3 名同阵营传说（或 3★ 觉醒）英雄：全队生命、攻击、技能强度 +25%",
            };
            for (int t = 0; t < 3; t++)
            {
                float ry = -96f - t * 84f;
                Image starIco = ForestUiWidgets.MakeIcon("Star_" + t, sheet.transform,
                    ForestComicUiSkin.GetUiArt("ww_xp_star"), new Vector2(36f, ry - 24f), new Vector2(32f, 32f),
                    anchor: new Vector2(0f, 1f));
                starIco.color = starTints[t];

                _wwAuraPopupTierTxts[t] = ForestUiWidgets.MakeText("Rule_" + t, sheet.transform, tierRules[t], 18,
                    ForestComicUiSkin.InkMuted, TextAnchor.MiddleLeft,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                    new Vector2(64f, ry), new Vector2(-84f, 68f), outline: false);
                SetOutlineEnabled(_wwAuraPopupTierTxts[t], false);
                _wwAuraPopupTierTxts[t].lineSpacing = 1.2f;
            }

            _wwFactionAuraPopup.SetActive(false);
        }

        void BuildFormationPickerPopupOverlay(Transform tfRoot)
        {
            RectTransform maskRt = ForestUiWidgets.Stretch("FormationPickerPopup", tfRoot);
            var mImg = maskRt.gameObject.AddComponent<Image>();
            mImg.color = new Color32(10, 16, 28, 148);
            mImg.raycastTarget = true;
            maskRt.gameObject.AddComponent<Button>().onClick.AddListener(() => _wwFormationPickerPopup.SetActive(false));
            _wwFormationPickerPopup = maskRt.gameObject;

            Image outer = ForestUiWidgets.MakeSlicedImage("Card", maskRt, ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(636f, 772f), true);

            ForestUiWidgets.MakeText("Hdr", outer.transform, "战术阵型图谱", 26, ForestComicUiSkin.InkHeaderLight,
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -12f), new Vector2(-80f, 38f), outline: true);
            ForestUiWidgets.MakeJellyButton("CloseFmtPop", outer.transform, ForestComicUiSkin.BtnRed, "×", 24,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -10f), new Vector2(42f, 42f),
                () => _wwFormationPickerPopup.SetActive(false), out _);

            _wwFmtPopupGlobalPctTxt = ForestUiWidgets.MakeText("GlobalPct", outer.transform,
                "已收集图谱全局永久加成：全英雄三维 +0%", 15, ForestComicUiSkin.AccentGold,
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -52f), new Vector2(-32f, 26f), outline: true);

            Image body = ForestUiWidgets.MakeSlicedImage("Sheet", outer.transform, ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -84f), new Vector2(604f, 668f), false);

            for (int i = 0; i < 6; i++)
            {
                TacticalFormationId fmt = (TacticalFormationId)i;
                float ry = -10f - i * 108f;
                Image row = ForestUiWidgets.MakeSlicedImage("FmtRow_" + i, body.transform,
                    ForestComicUiSkin.WwSubPanel, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, ry), new Vector2(580f, 100f), false);
                _wwFmtRowBgs[i] = row;

                Image chipCard = ForestUiWidgets.MakeSlicedImage("ChipCard", row.transform,
                    ForestComicUiSkin.ClippedCardPurple, Color.white,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(12f, 0f), new Vector2(76f, 76f), false);
                _wwFmtRowIcons[i] = ForestUiWidgets.MakeIcon("Ico", chipCard.transform,
                    GetEconArt("ico_fmt_default"), Vector2.zero, new Vector2(58f, 58f));

                _wwFmtRowNames[i] = ForestUiWidgets.MakeText("Name", row.transform, "默认防线阵", 18,
                    ForestComicUiSkin.InkDark, TextAnchor.UpperLeft,
                    new Vector2(0f, 1f), new Vector2(0.55f, 1f), new Vector2(0f, 1f),
                    new Vector2(100f, -10f), new Vector2(0f, 26f), outline: false);
                SetOutlineEnabled(_wwFmtRowNames[i], false);

                _wwFmtRowCollectTxts[i] = ForestUiWidgets.MakeText("ColBonus", row.transform, "收集三维+2%", 14,
                    ForestComicUiSkin.AccentPurple, TextAnchor.UpperRight,
                    new Vector2(0.45f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-138f, -12f), new Vector2(0f, 24f), outline: false);
                SetOutlineEnabled(_wwFmtRowCollectTxts[i], false);

                _wwFmtRowSlotDescs[i] = ForestUiWidgets.MakeText("SlotDesc", row.transform, "", 14,
                    ForestComicUiSkin.InkMuted, TextAnchor.LowerLeft,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f),
                    new Vector2(100f, 10f), new Vector2(-234f, 44f), outline: false);
                SetOutlineEnabled(_wwFmtRowSlotDescs[i], false);

                Button selBtn = ForestUiWidgets.MakeJellyButton("BtnUseFmt_" + i, row.transform,
                    ForestComicUiSkin.BtnBlue, "启 用", 17,
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(114f, 50f),
                    () => OnFormationButtonClicked(fmt), out _wwFmtRowBtnLbls[i]);
                _wwFmtRowBtnImgs[i] = selBtn.GetComponent<Image>();
            }

            _wwFormationPickerPopup.SetActive(false);
        }

        // ---------------------------------------------------------------------
        // 3. 【独立策略战场 HUD】 (_waveCombatHud — 严格保障微信右上角胶囊安全区净空)
        // ---------------------------------------------------------------------
        void BuildArenaCombatHudLayer()
        {
            _waveCombatHud = ForestUiWidgets.Stretch("TacticalArenaCombatHud", _canvasRt).gameObject;
            float topRowY = -16f - _topSafePad;

            ForestUiWidgets.MakeJellyButton("BtnRetreatArena", _waveCombatHud.transform, ForestComicUiSkin.BtnRed, "撤退", 17,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, topRowY), new Vector2(88f, 42f),
                RetreatArenaToCamp, out _);

            Image stageTimerPill = ForestUiWidgets.MakeSlicedImage("StageTimerPill", _waveCombatHud.transform,
                ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(112f, topRowY), new Vector2(272f, 42f), false);
            _wcStageTimerTxt = ForestUiWidgets.MakeText("Txt", stageTimerPill.transform, "第 1 关  ·  01:30", 17,
                ForestComicUiSkin.AccentGold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

            float vsBarTop = Mathf.Min(-142f, -92f - _topSafePad);
            Image vsBar = ForestUiWidgets.MakeSlicedImage("SymmetricVsBar", _waveCombatHud.transform,
                ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, vsBarTop), new Vector2(688f, 76f), false);

            ForestUiWidgets.MakeIcon("PwrBadge", vsBar.transform,
                ForestComicUiSkin.GetUiArt("ww_badge_power_fist"),
                new Vector2(-316f, 18f), new Vector2(28f, 28f));
            _wcTeamPowerTxt = ForestUiWidgets.MakeText("TeamPwr", vsBar.transform, "我方战力 360", 15,
                new Color32(164, 255, 186, 255), TextAnchor.MiddleLeft,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-164f, -8f), new Vector2(262f, 24f), outline: true);

            Image teamHpBg = ForestUiWidgets.MakeSlicedImage("TeamHpBg", vsBar.transform,
                ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-182f, -38f), new Vector2(292f, 28f), false);
            RectTransform teamFillRt = ForestUiWidgets.MakeRect("Fill", teamHpBg.transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6f, -6f));
            _wcTeamHpFill = teamFillRt.gameObject.AddComponent<Image>();
            _wcTeamHpFill.sprite = ForestComicUiSkin.BtnGreen;
            _wcTeamHpFill.type = Image.Type.Filled;
            _wcTeamHpFill.fillMethod = Image.FillMethod.Horizontal;
            _wcTeamHpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _wcTeamHpTxt = ForestUiWidgets.MakeText("Val", teamHpBg.transform, "500 / 500", 13, Color.white,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

            _wcEnemyTitleTxt = ForestUiWidgets.MakeText("EnemyHdr", vsBar.transform, "尸潮军团 (剩余 12/12)", 15,
                new Color32(255, 178, 168, 255), TextAnchor.MiddleRight,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(182f, -8f), new Vector2(292f, 24f), outline: true);

            Image enemyHpBg = ForestUiWidgets.MakeSlicedImage("EnemyHpBg", vsBar.transform,
                ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(182f, -38f), new Vector2(292f, 28f), false);
            RectTransform enemyFillRt = ForestUiWidgets.MakeRect("Fill", enemyHpBg.transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6f, -6f));
            _wcEnemyHpFill = enemyFillRt.gameObject.AddComponent<Image>();
            _wcEnemyHpFill.sprite = ForestComicUiSkin.BtnRed;
            _wcEnemyHpFill.type = Image.Type.Filled;
            _wcEnemyHpFill.fillMethod = Image.FillMethod.Horizontal;
            _wcEnemyHpFill.fillOrigin = (int)Image.OriginHorizontal.Right;
            _wcEnemyHpTxt = ForestUiWidgets.MakeText("Val", enemyHpBg.transform, "600 / 600", 13, Color.white,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

            Image vsBadge = ForestUiWidgets.MakeSlicedImage("VsBadge", vsBar.transform,
                ForestComicUiSkin.ClippedCardOrange, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(56f, 56f), false);
            ForestUiWidgets.MakeText("VsTxt", vsBadge.transform, "VS", 22, Color.white,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

            _wcSkipBtn = ForestUiWidgets.MakeJellyButton("BtnSkipBattle", _waveCombatHud.transform,
                ForestComicUiSkin.BtnGrey, "5秒后可跳过", 15,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 216f), new Vector2(186f, 44f),
                OnSkipBattleClicked, out _wcSkipLbl);
            _wcSkipBtnImg = _wcSkipBtn.GetComponent<Image>();

            _wcAutoBtn = ForestUiWidgets.MakeJellyButton("BtnAutoUlt", _waveCombatHud.transform,
                ForestComicUiSkin.BtnGreen, "自动绝技:开", 15,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-146f, 216f), new Vector2(152f, 44f),
                () =>
                {
                    if (_world != null && _world.Arena != null)
                        _world.Arena.AutoCastUlt = !_world.Arena.AutoCastUlt;
                }, out _wcAutoLbl);
            _wcAutoBtnImg = _wcAutoBtn.GetComponent<Image>();

            _wcSpeedBtn = ForestUiWidgets.MakeJellyButton("BtnArenaSpeed", _waveCombatHud.transform,
                ForestComicUiSkin.BtnBlue, "倍速 1x", 16,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 216f), new Vector2(118f, 44f),
                () =>
                {
                    if (_world != null && _world.Arena != null)
                        _world.Arena.ToggleSpeed();
                }, out _wcSpeedLbl);
            _wcSpeedBtnImg = _wcSpeedBtn.GetComponent<Image>();

            for (int i = 0; i < 5; i++)
            {
                int slotIdx = i;
                _wcBoundHeroIdxs[i] = -1;
                Image skCard = ForestUiWidgets.MakeSlicedImage("ArenaHeroCard_" + i, _waveCombatHud.transform,
                    ForestComicUiSkin.WwHeaderShell, Color.white,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(-272f + i * 136f, 20f), new Vector2(128f, 184f), true);
                _wcHeroCards[i] = skCard.gameObject;
                _wcHeroCardRts[i] = skCard.rectTransform;
                _wcHeroFrames[i] = skCard;
                skCard.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                {
                    int hIdx = _wcBoundHeroIdxs[slotIdx];
                    if (hIdx >= 0 && _world != null && _world.Arena != null)
                        _world.Arena.TryCastHeroUlt(hIdx);
                });

                _wcHeroNames[i] = ForestUiWidgets.MakeText("Name", skCard.transform, "大锤 ★0", 14,
                    ForestComicUiSkin.InkHeaderLight, TextAnchor.MiddleCenter,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -5f), new Vector2(-8f, 22f), outline: true);

                Image pFrame = ForestUiWidgets.MakeSlicedImage("ClippedPortrait", skCard.transform,
                    ForestComicUiSkin.ClippedCardBlue, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -29f), new Vector2(78f, 72f), false);
                _wwWcPortraitFrames[i] = pFrame;

                Image msk = ForestUiWidgets.MakeSlicedImage("Mask", pFrame.transform, null, new Color(1f, 1f, 1f, 0.02f),
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8f, -8f), false);
                msk.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                _wcHeroBusts[i] = ForestUiWidgets.MakeIcon("Bust", msk.transform,
                    ForestComicUiSkin.GetArt("bust_dachui"), new Vector2(0f, -3f), new Vector2(68f, 68f));

                Image hpBg = ForestUiWidgets.MakeSlicedImage("HpBg", skCard.transform,
                    ForestComicUiSkin.DarkPill, Color.white,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 56f), new Vector2(-14f, 18f), false);
                RectTransform hpFillRt = ForestUiWidgets.MakeRect("HpFill", hpBg.transform,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4f, -4f));
                _wcHeroHpFills[i] = hpFillRt.gameObject.AddComponent<Image>();
                _wcHeroHpFills[i].sprite = ForestComicUiSkin.BtnGreen;
                _wcHeroHpFills[i].type = Image.Type.Filled;
                _wcHeroHpFills[i].fillMethod = Image.FillMethod.Horizontal;

                RectTransform shFillRt = ForestUiWidgets.MakeRect("ShieldFill", hpBg.transform,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4f, -4f));
                _wcHeroShieldFills[i] = shFillRt.gameObject.AddComponent<Image>();
                _wcHeroShieldFills[i].sprite = ForestComicUiSkin.BtnBlue;
                _wcHeroShieldFills[i].color = new Color(0.75f, 0.95f, 1f, 0.72f);
                _wcHeroShieldFills[i].type = Image.Type.Filled;
                _wcHeroShieldFills[i].fillMethod = Image.FillMethod.Horizontal;
                _wcHeroHpTxts[i] = ForestUiWidgets.MakeText("HpTxt", hpBg.transform, "240", 11, Color.white,
                    TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

                Image rgBg = ForestUiWidgets.MakeSlicedImage("RageBg", skCard.transform,
                    ForestComicUiSkin.DarkPill, Color.white,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 35f), new Vector2(-14f, 18f), false);
                RectTransform rgFillRt = ForestUiWidgets.MakeRect("RageFill", rgBg.transform,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4f, -4f));
                _wcHeroRageFills[i] = rgFillRt.gameObject.AddComponent<Image>();
                _wcHeroRageFills[i].sprite = ForestComicUiSkin.BtnOrange;
                _wcHeroRageFills[i].type = Image.Type.Filled;
                _wcHeroRageFills[i].fillMethod = Image.FillMethod.Horizontal;
                _wcHeroRageTxts[i] = ForestUiWidgets.MakeText("RageTxt", rgBg.transform, "怒气 0%", 11, Color.white,
                    TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

                Image actBg = ForestUiWidgets.MakeSlicedImage("ActTag", skCard.transform,
                    ForestComicUiSkin.DarkPill, Color.white,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 6f), new Vector2(-12f, 25f), false);
                _wcHeroActionBgs[i] = actBg;
                _wcHeroActionTxts[i] = ForestUiWidgets.MakeText("ActTxt", actBg.transform, "狂风劈", 12,
                    ForestComicUiSkin.AccentGold, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4f, 0f), outline: true);

                Image ultBrk = ForestUiWidgets.MakeSlicedImage("UltBrackets", skCard.transform,
                    ForestComicUiSkin.SelectionCornerBrackets, Color.white,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, false);
                _wwWcUltBrackets[i] = ultBrk.gameObject;
                ultBrk.gameObject.SetActive(false);
            }
            _waveCombatHud.SetActive(false);
        }

        // ---------------------------------------------------------------------
        // 4. 【挑战胜利 MVP + 切角战利品 + 自动连战结算弹窗】 (_arenaResultModal)
        // ---------------------------------------------------------------------
        void BuildArenaResultSettlementModal()
        {
            _arenaResultModal = MakeModalMask("ArenaResultModal", 0.88f, null);
            var arMask = _arenaResultModal.GetComponent<Image>();
            if (arMask != null) arMask.color = new Color32(18, 24, 50, 232);

            _arSunburst = ForestUiWidgets.MakeIcon("Sunburst", _arenaResultModal.transform,
                ForestComicUiSkin.Sunburst, new Vector2(0f, 90f), new Vector2(650f, 650f));
            _arSunburst.color = new Color(1f, 0.92f, 0.68f, 0.62f);

            Image rCard = ForestUiWidgets.MakeSlicedImage("Card", _arenaResultModal.transform,
                ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -12f), new Vector2(644f, 768f), true);

            _arRibbonImg = ForestUiWidgets.MakeSlicedImage("Ribbon", rCard.transform,
                ForestComicUiSkin.GetUiArt("ww_banner_congrats", ForestComicUiSkin.BtnOrange), Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 14f), new Vector2(396f, 94f), false);
            _arRibbonImg.type = Image.Type.Simple;
            _arRibbonImg.preserveAspect = true;

            _arRibbonTxt = ForestUiWidgets.MakeText("RibTxt", _arRibbonImg.transform, "挑战胜利!", 36, Color.white,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), Vector2.zero, outline: true);
            _wwArBannerFallbackTxt = _arRibbonTxt;

            _arStageSubTxt = ForestUiWidgets.MakeText("StageSub", rCard.transform, "第 1 关 · 山口游尸", 19,
                ForestComicUiSkin.AccentGold, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -50f), new Vector2(-32f, 30f), outline: true);

            _arVictoryRoot = ForestUiWidgets.MakeRect("VictoryBody", rCard.transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject;
            Image vSheet = ForestUiWidgets.MakeSlicedImage("VLightSheet", _arVictoryRoot.transform,
                ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -90f), new Vector2(604f, 568f), false);

            Image mvpCard = ForestUiWidgets.MakeSlicedImage("MvpCard", vSheet.transform,
                ForestComicUiSkin.SubSlotSelected, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -14f), new Vector2(572f, 162f), false);
            ForestUiWidgets.MakeSlicedImage("MvpBrackets", mvpCard.transform,
                ForestComicUiSkin.SelectionCornerBrackets, Color.white,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4f, 4f), false);

            Image mvpBadge = ForestUiWidgets.MakeSlicedImage("MvpBadge", mvpCard.transform,
                ForestComicUiSkin.RarityRibbon(4), Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(14f, -10f), new Vector2(168f, 32f), false);
            ForestUiWidgets.MakeText("Txt", mvpBadge.transform, "★ MVP 本场最佳", 15, Color.white,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

            Image mvpFrame = ForestUiWidgets.MakeSlicedImage("MvpClippedFrame", mvpCard.transform,
                ForestComicUiSkin.ClippedCardOrange, Color.white,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(20f, 12f), new Vector2(104f, 104f), false);
            _wwArMvpRarityFrame = mvpFrame;

            Image mvpMask = ForestUiWidgets.MakeSlicedImage("MvpMask", mvpFrame.transform, null, new Color(1f, 1f, 1f, 0.02f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-10f, -10f), false);
            mvpMask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            _arMvpBust = ForestUiWidgets.MakeIcon("Bust", mvpMask.transform,
                ForestComicUiSkin.GetArt("bust_dachui"), new Vector2(0f, -4f), new Vector2(92f, 92f));

            _arMvpNameTxt = ForestUiWidgets.MakeText("MvpName", mvpCard.transform, "大锤（铁壁重卫）", 24,
                ForestComicUiSkin.InkDark, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(142f, -46f), new Vector2(-158f, 36f), outline: false);
            SetOutlineEnabled(_arMvpNameTxt, false);
            _arMvpDmgTxt = ForestUiWidgets.MakeText("MvpDmg", mvpCard.transform, "本场总贡献伤害/治疗: 1280", 18,
                ForestComicUiSkin.AccentGreen, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(142f, -94f), new Vector2(-158f, 32f), outline: false);
            SetOutlineEnabled(_arMvpDmgTxt, false);

            Image rwSumBox = ForestUiWidgets.MakeSlicedImage("RewardSummaryBox", vSheet.transform,
                ForestComicUiSkin.WwSubPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -188f), new Vector2(572f, 306f), false);
            Text vHdr = ForestUiWidgets.MakeText("Hdr", rwSumBox.transform, "通关战利品与首通里程碑", 18,
                ForestComicUiSkin.InkDark, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -8f), new Vector2(-20f, 28f), outline: false);
            SetOutlineEnabled(vHdr, false);

            int[] defaultVicRarities = { 2, 4, 2, 3 };
            for (int r = 0; r < 4; r++)
            {
                float lx = -192f + r * 128f;
                Image lTile = ForestUiWidgets.MakeSlicedImage("VicLoot_" + r, rwSumBox.transform,
                    ForestComicUiSkin.GetClippedCard(defaultVicRarities[r]), Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(lx, -42f), new Vector2(108f, 108f), false);
                _wwArLootTiles[r] = lTile.gameObject;
                _wwArLootFrames[r] = lTile;
                _wwArLootIcons[r] = ForestUiWidgets.MakeIcon("Ico", lTile.transform,
                    GetEconResIcon(SurvRes.HeroExp), new Vector2(0f, 8f), new Vector2(64f, 64f));
                _wwArLootCounts[r] = ForestUiWidgets.MakeText("Cnt", lTile.transform, "+300", 19, Color.white,
                    TextAnchor.LowerRight, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                    new Vector2(-8f, 6f), new Vector2(-12f, -8f), outline: true);
            }

            _arRewardSummaryTxt = ForestUiWidgets.MakeText("Body", rwSumBox.transform,
                "英雄经验 +300 · 招募卡 +1 · 技能书 +2", 16, ForestComicUiSkin.InkDark,
                TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 12f), new Vector2(-32f, 136f), outline: false);
            SetOutlineEnabled(_arRewardSummaryTxt, false);
            _arRewardSummaryTxt.lineSpacing = 1.24f;

            Button autoChainBtn = ForestUiWidgets.MakeJellyButton("BtnAutoChainToggle", vSheet.transform,
                ForestComicUiSkin.BtnGrey, "[ ] 自动连续闯关", 16,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(348f, 46f),
                () =>
                {
                    _sim.AutoChainNextStage = !_sim.AutoChainNextStage;
                    if (_sim.AutoChainNextStage) _autoChainCountdown = 5.0f;
                    RefreshArenaResultModalVisual();
                }, out _arAutoChainLbl);
            _arAutoChainBtnImg = autoChainBtn.GetComponent<Image>();

            MakeWwBackRedButton(_arVictoryRoot.transform, ExitArenaAndCloseResult, new Vector2(18f, 18f), new Vector2(92f, 66f));
            ForestUiWidgets.MakeJellyButton("BtnVictoryBackCamp", _arVictoryRoot.transform,
                ForestComicUiSkin.BtnBlue, "返回营地", 21,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-64f, 20f), new Vector2(204f, 62f),
                ExitArenaAndCloseResult, out _);
            ForestUiWidgets.MakeJellyButton("BtnVictoryNextStage", _arVictoryRoot.transform,
                ForestComicUiSkin.BtnGreen, "下一关", 22,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(174f, 20f), new Vector2(246f, 62f),
                OnResultNextStageClicked, out _arNextStageBtnLbl);

            // --- 战败复盘主体 ---
            _arDefeatRoot = ForestUiWidgets.MakeRect("DefeatBody", rCard.transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject;
            Image dSheet = ForestUiWidgets.MakeSlicedImage("DLightSheet", _arDefeatRoot.transform,
                ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -90f), new Vector2(604f, 568f), false);

            Text advHdr = ForestUiWidgets.MakeText("AdvHdr", dSheet.transform, "【战术复盘建议】调整黑板阵型或强化英雄即可破局！", 19,
                ForestComicUiSkin.AccentRed, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -18f), new Vector2(-24f, 34f), outline: false);
            SetOutlineEnabled(advHdr, false);

            Image advPanel = ForestUiWidgets.MakeSlicedImage("AdvPanel", dSheet.transform,
                ForestComicUiSkin.WwSubPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -66f), new Vector2(564f, 384f), false);

            _arDefeatAdviceTxt = ForestUiWidgets.MakeText("AdvBody", advPanel.transform,
                "1. 前排部署【大锤 / 雷猛】吸收火力，远程主C置于后排安全格。\n" +
                "2. 凑齐 2~3 名同阵营英雄激活【阵营光环 +10%~+25%】。\n" +
                "3. 切换克制【战术阵型】（如抗性增强阵克制元素首领）。\n" +
                "4. 领取【哨卡巡逻挂机箱】英雄经验，升级英雄与点亮星瓣！",
                18, ForestComicUiSkin.InkDark, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -24f), new Vector2(-48f, 260f), outline: false);
            SetOutlineEnabled(_arDefeatAdviceTxt, false);
            _arDefeatAdviceTxt.lineSpacing = 1.42f;

            ForestUiWidgets.MakeJellyButton("BtnDefeatGoHero", advPanel.transform,
                ForestComicUiSkin.BtnBlue, "去升级 / 升星幸存者英雄 ▶", 19,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(340f, 56f),
                () => { ExitArenaAndCloseResult(); OpenHeroModal(1); }, out _);

            MakeWwBackRedButton(_arDefeatRoot.transform, ExitArenaAndCloseResult, new Vector2(18f, 18f), new Vector2(92f, 66f));
            ForestUiWidgets.MakeJellyButton("BtnDefeatBackCamp", _arDefeatRoot.transform,
                ForestComicUiSkin.BtnGrey, "返回营地", 21,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-66f, 20f), new Vector2(198f, 62f),
                ExitArenaAndCloseResult, out _);
            ForestUiWidgets.MakeJellyButton("BtnDefeatAdjustFmt", _arDefeatRoot.transform,
                ForestComicUiSkin.BtnOrange, "调整黑板布阵", 21,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(170f, 20f), new Vector2(252f, 62f),
                () => { ExitArenaAndCloseResult(); OpenTacticalFormationModal(); }, out _);

            _arenaResultModal.SetActive(false);
        }

        // =====================================================================
        // 策略战场交互与动态刷新逻辑
        // =====================================================================
        public void OpenWavePrepModal() => OpenStagePosterModal();

        public void OpenStagePosterModal()
        {
            CloseAllModals();
            if (_wwCongratsModal != null) _wwCongratsModal.SetActive(false);
            if (_wwPosterItemPopup != null) _wwPosterItemPopup.SetActive(false);
            ClearFloatingWorldToasts();
            if (_sim.GetDeployedHeroCount() <= 0)
                _sim.AutoDeployRecommendedFormation();
            RefreshStagePosterModal();
            _wavePrepModal.SetActive(true);
        }

        void OpenPosterRewardItemPopup(int slotIdx)
        {
            if (slotIdx < 0 || slotIdx >= _wwSpLootEntries.Length) return;
            PosterRewardEntry e = _wwSpLootEntries[slotIdx];
            if (string.IsNullOrEmpty(e.title)) return;

            _wwPosterPopCard.sprite = ForestComicUiSkin.GetClippedCard(e.rarity0to5);
            _wwPosterPopIcon.sprite = e.icon;
            _wwPosterPopNameTxt.text = e.title;
            _wwPosterPopOwnTxt.text = e.ownText;
            _wwPosterPopDescTxt.text = e.desc;
            _wwPosterPopGoCb = e.onGo;

            _wwPosterItemPopup.transform.SetAsLastSibling();
            _wwPosterItemPopup.SetActive(true);
        }

        void OnClaimPatrolChestClicked()
        {
            if (_sim.TryClaimPatrolChest(out int heroExp, out int coins, out int books, out string sumMsg))
            {
                RefreshStagePosterModal();
                var rw = new List<(SurvRes res, int count, Sprite customIcon, string customLabel)>(3)
                {
                    (SurvRes.HeroExp, heroExp, GetEconResIcon(SurvRes.HeroExp), "英雄经验"),
                    (SurvRes.Wood, coins, ForestComicUiSkin.GetArt("ico_coin"), "营地铜币"),
                };
                if (books > 0)
                    rw.Add((SurvRes.SkillBook, books, GetEconResIcon(SurvRes.SkillBook), "战术技能书"));
                ShowCongratsRewardModal("哨卡巡逻挂机收益领取", rw.ToArray());
            }
            else if (!string.IsNullOrEmpty(sumMsg))
            {
                SpawnScreenCenterBanner(sumMsg);
            }
        }

        void RefreshStagePosterModal()
        {
            int curStage = _sim.CurrentBattleStage;
            BattleStageSpec spec = _sim.GetBattleStageSpec(curStage);
            _spStageBadgeTxt.text = $"第 {spec.stage} / {MaxBattleStages} 关";
            _spBossIconImg.sprite = spec.isBossStage
                ? ForestComicUiSkin.GetArt("bust_zombie_boss", ForestComicUiSkin.GetUiArt("ww_dock_battle"))
                : ForestComicUiSkin.GetUiArt("ww_dock_battle", ForestComicUiSkin.GetArt("dock_defense"));
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
                    _spPreviewFrames[i].sprite = ForestComicUiSkin.GetClippedCard(h.rarity);
                    _spPreviewBusts[i].gameObject.SetActive(true);
                    _spPreviewBusts[i].sprite = ForestComicUiSkin.GetArt(h.portraitKey, ForestComicUiSkin.GetArt("bust_dachui"));
                    _spPreviewLocks[i].gameObject.SetActive(false);
                    if (_wwSpEmptyBrackets[i] != null) _wwSpEmptyBrackets[i].SetActive(false);
                    _spPreviewNames[i].text = $"{h.name} Lv.{h.level}";
                    _spPreviewNames[i].color = ForestComicUiSkin.InkDark;
                    _spPreviewRoles[i].text = $"{colTags[sl % 3]}·{ForestSurvivalSim.GetFactionName(h.faction)}";
                    _spPreviewRoles[i].color = ForestComicUiSkin.InkMuted;
                }
                else if (i < maxDep)
                {
                    _spPreviewFrames[i].sprite = ForestComicUiSkin.WwLightSheet;
                    _spPreviewBusts[i].gameObject.SetActive(false);
                    _spPreviewLocks[i].gameObject.SetActive(false);
                    if (_wwSpEmptyBrackets[i] != null) _wwSpEmptyBrackets[i].SetActive(true);
                    _spPreviewNames[i].text = "＋ 空位待命";
                    _spPreviewNames[i].color = ForestComicUiSkin.AccentGreen;
                    _spPreviewRoles[i].text = "点击上阵";
                    _spPreviewRoles[i].color = ForestComicUiSkin.InkMuted;
                }
                else
                {
                    _spPreviewFrames[i].sprite = ForestComicUiSkin.ClippedCardGrey;
                    _spPreviewBusts[i].gameObject.SetActive(false);
                    _spPreviewLocks[i].gameObject.SetActive(true);
                    if (_wwSpEmptyBrackets[i] != null) _wwSpEmptyBrackets[i].SetActive(false);
                    _spPreviewNames[i].text = $"Lv.{slotExpandLvs[i]}扩编";
                    _spPreviewNames[i].color = ForestComicUiSkin.InkMuted;
                    _spPreviewRoles[i].text = "提升营地";
                    _spPreviewRoles[i].color = ForestComicUiSkin.InkMuted;
                }
            }

            _sim.GetFactionAuraState(out string auraTitle, out _, out float auraPct, out _, out _, out _);
            if (auraPct > 0.001f)
            {
                _spSynergyStripTxt.text = $"★ 阵营光环：{auraTitle} — 全队生命 / 攻击 / 技能强度 +{Mathf.RoundToInt(auraPct * 100f)}%";
                _spSynergyStripTxt.color = new Color32(158, 255, 182, 255);
            }
            else
            {
                _spSynergyStripTxt.text = "阵营光环：上阵 ≥2 名同阵营英雄激活全队 +10%~+25% 三维加成（点击阵容排布）";
                _spSynergyStripTxt.color = ForestComicUiSkin.AccentGold;
            }

            // 绑定 4 张独立养成奖励卡 + 点击说明数据
            _wwSpLootEntries[0] = new PosterRewardEntry
            {
                icon = GetEconResIcon(SurvRes.HeroExp),
                rarity0to5 = 2,
                countText = $"+{FormatEconCount(spec.rewardHeroExp)}",
                title = "英雄经验",
                ownText = $"拥有:  {FormatEconCount(_sim.Get(SurvRes.HeroExp))}",
                desc = "用于提升英雄等级，支持 100% 免费无损重置。",
                onGo = () => OpenHeroModal(1),
            };

            _wwSpLootEntries[1] = new PosterRewardEntry
            {
                icon = GetEconResIcon(SurvRes.RecruitTicket),
                rarity0to5 = 4,
                countText = $"+{Mathf.Max(1, spec.rewardRecruitTickets)}",
                title = "招募卡",
                ownText = $"拥有:  {_sim.Get(SurvRes.RecruitTicket)}",
                desc = "用于在广播站中招募新英雄与专属拼图碎片。",
                onGo = OpenRadioRecruitModal,
            };

            bool firstClearPending = _sim.HordeWaveCleared < spec.stage;
            if (firstClearPending && spec.unlockFormationIdx >= 0)
            {
                TacticalFormationId fmt = (TacticalFormationId)spec.unlockFormationIdx;
                string fName = _sim.GetFormationName(fmt);
                int cPct = Mathf.RoundToInt(_sim.GetFormationCollectBonusFor(fmt) * 100f);
                _wwSpLootEntries[2] = new PosterRewardEntry
                {
                    icon = GetFormationChipSprite(_sim, fmt),
                    rarity0to5 = 3,
                    countText = "1",
                    title = $"阵型·{fName}",
                    ownText = $"拥有:  {(_sim.IsFormationUnlocked(fmt) ? 1 : 0)}",
                    desc = $"用于激活阵型【{fName}】。获取该阵型后，所有英雄生命、攻击、技能强度+{cPct}%。\n使用该阵型站位：{_sim.GetFormationSummary(fmt)}",
                    onGo = OpenTacticalFormationModal,
                };
            }
            else
            {
                _wwSpLootEntries[2] = new PosterRewardEntry
                {
                    icon = GetEconResIcon(SurvRes.SkillBook),
                    rarity0to5 = 2,
                    countText = $"+{Mathf.Max(1, spec.rewardSkillBooks)}",
                    title = "战术技能书",
                    ownText = $"拥有:  {_sim.Get(SurvRes.SkillBook)}",
                    desc = "用于在【英雄】属性页升级主被动技能等级。",
                    onGo = () => OpenHeroModal(1),
                };
            }

            if (firstClearPending && spec.unlockHeroIdx >= 0 && spec.unlockHeroIdx < _sim.Heroes.Count)
            {
                HeroSpec uh = _sim.Heroes[spec.unlockHeroIdx];
                int hIdx = spec.unlockHeroIdx;
                _wwSpLootEntries[3] = new PosterRewardEntry
                {
                    icon = ForestComicUiSkin.GetArt(uh.portraitKey, ForestComicUiSkin.GetArt("bust_dachui")),
                    rarity0to5 = uh.rarity == HeroRarity.Legendary ? 4 : 3,
                    countText = "整卡",
                    title = $"英雄·{uh.name}",
                    ownText = uh.unlocked ? "状态: 已入队" : "首通直接入队",
                    desc = $"首通本关立即免费招募【{ForestSurvivalSim.GetFactionName(uh.faction)}·{uh.name}】（{uh.roleTitle}）！",
                    onGo = () => OpenHeroDetailModal(hIdx, 0),
                };
            }
            else if (spec.rewardShardCount > 0 && spec.rewardShardHeroIdx >= 0 && spec.rewardShardHeroIdx < _sim.Heroes.Count)
            {
                HeroSpec sh = _sim.Heroes[spec.rewardShardHeroIdx];
                int sIdx = spec.rewardShardHeroIdx;
                _wwSpLootEntries[3] = new PosterRewardEntry
                {
                    icon = GetHeroJigsawShardSprite(sh),
                    rarity0to5 = sh.rarity == HeroRarity.Legendary ? 4 : (sh.rarity == HeroRarity.Epic ? 3 : 2),
                    countText = $"+{spec.rewardShardCount}",
                    title = $"{sh.name}碎片",
                    ownText = $"当前拥有:  {sh.shards}",
                    desc = $"用于英雄【{sh.name}】点亮星瓣与升星突破（集满10片可直接合成英雄）。",
                    onGo = () => OpenHeroDetailModal(sIdx, 1),
                };
            }
            else
            {
                _wwSpLootEntries[3] = new PosterRewardEntry
                {
                    icon = ForestComicUiSkin.GetArt("ico_coin"),
                    rarity0to5 = 1,
                    countText = $"+{spec.rewardCoins}",
                    title = "营地铜币",
                    ownText = $"拥有:  {_sim.Coins}",
                    desc = "用于新建与升级营地设施，或在山口行商处兑换招募卡。",
                    onGo = () => OpenQuickBuildList(showWarehouse: false),
                };
            }

            for (int r = 0; r < 4; r++)
            {
                _wwSpLootFrames[r].sprite = ForestComicUiSkin.GetClippedCard(_wwSpLootEntries[r].rarity0to5);
                _spRewardIcons[r].sprite = _wwSpLootEntries[r].icon;
                _spRewardTxts[r].text = _wwSpLootEntries[r].countText;
            }

            if (!string.IsNullOrEmpty(spec.milestoneTag))
            {
                _spMilestoneBg.sprite = ForestComicUiSkin.BtnOrange;
                _spMilestoneTxt.text = $"★ 里程碑大奖：{spec.milestoneTag}";
                _spMilestoneTxt.color = Color.white;
            }
            else
            {
                _spMilestoneBg.sprite = ForestComicUiSkin.WwHeaderShell;
                _spMilestoneTxt.text = "提示：每通关更高关卡都会永久提升下方【巡逻挂机箱】每分钟英雄经验产量！";
                _spMilestoneTxt.color = ForestComicUiSkin.AccentGold;
            }

            // 刷新【哨卡巡逻挂机箱】
            _sim.GetPatrolPendingRewards(out int patExp, out int patCoins, out int patBooks, out int patMins);
            bool patReady = patMins >= 1 && patExp > 0;
            _wwPatrolStatusTxt.text = $"哨卡巡逻挂机箱（已挂机 {patMins} 分钟 · 第{Mathf.Max(1, _sim.HordeWaveCleared)}关速率）";
            _wwPatrolYieldTxt.text = patReady
                ? $"经验 <color=#86EFAC>+{FormatEconCount(patExp)}</color> · 铜币 <color=#FDE047>+{patCoins}</color>" +
                  (patBooks > 0 ? $" · 技能书 <color=#93C5FD>+{patBooks}</color>" : "")
                : $"巡逻自动积累中（每分钟英雄经验 +{18 + 12 * Mathf.Max(1, _sim.HordeWaveCleared)} · 满1分钟可领）";
            _wwPatrolYieldTxt.supportRichText = true;
            _wwPatrolClaimBtnImg.sprite = patReady ? ForestComicUiSkin.BtnGreen : ForestComicUiSkin.BtnGrey;
            _wwPatrolClaimLbl.text = patReady ? "领取收益" : "巡逻中";
            if (_wwPatrolChestDotGo != null) _wwPatrolChestDotGo.SetActive(patReady);
            if (_wwPatrolChestAnim != null) _wwPatrolChestAnim.bobActive = patReady;
        }

        public void OpenTacticalFormationModal()
        {
            CloseAllModals();
            if (_wwCongratsModal != null) _wwCongratsModal.SetActive(false);
            if (_wwFactionAuraPopup != null) _wwFactionAuraPopup.SetActive(false);
            if (_wwFormationPickerPopup != null) _wwFormationPickerPopup.SetActive(false);
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
                case 2: return "第2关/招募";
                case 3: return "第5关/招募";
                case 4: return "第3关/招募";
                case 5: return "第12关/招募";
                case 6: return "第8关/招募";
                case 7: return "第15关/招募";
                case 8: return "第20关/招募";
                default: return "初始入队";
            }
        }

        void RefreshFactionAuraPopup()
        {
            if (_wwFactionAuraPopup == null) return;
            _sim.GetFactionAuraState(out string auraTitle, out _, out float bonusPct,
                out int ironCnt, out int forestCnt, out int techCnt);
            _wwAuraPopupHdrTxt.text = bonusPct > 0.001f ? $"阵营光环：{auraTitle}" : "上阵同阵营英雄激活全队光环";
            _wwAuraPopupCountTxt.text = $"当前上阵：铁卫团 {ironCnt} 人  ·  山林盟 {forestCnt} 人  ·  废土科工 {techCnt} 人";

            int activeTier = bonusPct >= 0.24f ? 2 : (bonusPct >= 0.145f && bonusPct < 0.175f ? 1 : (bonusPct >= 0.09f ? 0 : -1));
            for (int t = 0; t < 3; t++)
            {
                bool lit = t == activeTier;
                _wwAuraPopupTierTxts[t].color = lit ? (Color)ForestComicUiSkin.AccentGreen : (Color)ForestComicUiSkin.InkDark;
                _wwAuraPopupTierTxts[t].fontStyle = lit ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        void RefreshFormationPickerPopup()
        {
            if (_wwFormationPickerPopup == null) return;
            int globPct = Mathf.RoundToInt(_sim.GetFormationGlobalBonusPct() * 100f);
            _wwFmtPopupGlobalPctTxt.text = $"已收集阵型图谱全局永久加成：全英雄生命/攻击/技能强度 +{globPct}%";

            int[] unlockStageHints = { 1, 4, 10, 16, 20, 24 };
            for (int i = 0; i < 6; i++)
            {
                TacticalFormationId fmt = (TacticalFormationId)i;
                bool unlocked = _sim.IsFormationUnlocked(fmt);
                bool selected = _sim.SelectedFormation == fmt;
                int colPct = Mathf.RoundToInt(_sim.GetFormationCollectBonusFor(fmt) * 100f);

                _wwFmtRowBgs[i].sprite = selected ? ForestComicUiSkin.SubSlotSelected : ForestComicUiSkin.WwSubPanel;
                _wwFmtRowIcons[i].sprite = GetFormationChipSprite(_sim, fmt);
                _wwFmtRowIcons[i].color = unlocked ? Color.white : new Color(0.55f, 0.58f, 0.68f, 0.80f);
                _wwFmtRowNames[i].text = _sim.GetFormationName(fmt);
                _wwFmtRowCollectTxts[i].text = colPct > 0
                    ? (unlocked ? $"✓ 收集三维+{colPct}%" : $"收集三维+{colPct}%")
                    : "初始基础阵型";
                _wwFmtRowCollectTxts[i].color = unlocked ? (Color)ForestComicUiSkin.AccentGreen : (Color)ForestComicUiSkin.AccentPurple;
                _wwFmtRowSlotDescs[i].text = _sim.GetFormationSummary(fmt);

                if (selected)
                {
                    _wwFmtRowBtnImgs[i].sprite = ForestComicUiSkin.BtnGreen;
                    _wwFmtRowBtnLbls[i].text = "使用中";
                }
                else if (unlocked)
                {
                    _wwFmtRowBtnImgs[i].sprite = ForestComicUiSkin.BtnBlue;
                    _wwFmtRowBtnLbls[i].text = "启 用";
                }
                else
                {
                    _wwFmtRowBtnImgs[i].sprite = ForestComicUiSkin.BtnGrey;
                    _wwFmtRowBtnLbls[i].text = $"第{unlockStageHints[i]}关";
                }
            }
        }

        void RefreshTacticalFormationModal()
        {
            int depCnt = _sim.GetDeployedHeroCount();
            int maxDep = _sim.GetMaxDeployHeroCount();
            _tfDeployCountTxt.text = $"主阵容英雄: {depCnt}/{maxDep}";
            _tfTotalPowerTxt.text = _sim.CalcDeployedSquadPower().ToString();

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

                if (_wwTfTileYellowBorders[slot] != null)
                    _wwTfTileYellowBorders[slot].SetActive(occupied);
                if (_wwTfTileBrackets[slot] != null)
                    _wwTfTileBrackets[slot].SetActive(isSelTile);

                _tfTileFrames[slot].color = buff.highlighted
                    ? new Color32(36, 98, 128, 255)
                    : new Color32(26, 66, 86, 255);

                if (occupied)
                {
                    HeroSpec h = _sim.Heroes[hIdx];
                    _tfTileLaneHints[slot].gameObject.SetActive(false);
                    _tfTileEmptyTxts[slot].gameObject.SetActive(false);
                    if (_wwTfTileShadows[slot] != null) _wwTfTileShadows[slot].gameObject.SetActive(true);
                    _tfTilePortraitBoxes[slot].SetActive(true);
                    ApplyHeroFullBodySprite(_tfTileBusts[slot], h);
                    _wwTfTileFactionIcons[slot].gameObject.SetActive(true);
                    _wwTfTileFactionIcons[slot].sprite = GetFactionCrestSprite(h.faction);

                    _tfTileNames[slot].gameObject.SetActive(true);
                    _tfTileNames[slot].text = $"{h.level}级";
                    _tfTileRoles[slot].gameObject.SetActive(true);
                    _tfTileRoles[slot].text = h.name;
                    _tfTileRemoveGos[slot].SetActive(true);
                }
                else
                {
                    _tfTileLaneHints[slot].gameObject.SetActive(true);
                    if (_wwTfTileShadows[slot] != null) _wwTfTileShadows[slot].gameObject.SetActive(false);
                    _tfTilePortraitBoxes[slot].SetActive(false);
                    _wwTfTileFactionIcons[slot].gameObject.SetActive(false);
                    _tfTileNames[slot].gameObject.SetActive(false);
                    _tfTileRoles[slot].gameObject.SetActive(false);
                    _tfTileRemoveGos[slot].SetActive(false);
                    _tfTileEmptyTxts[slot].gameObject.SetActive(true);
                    if (hasSelHero)
                    {
                        _tfTileEmptyTxts[slot].text = selAlreadyDeployed ? $"换位\n【{selHeroName}】" : $"＋部署\n【{selHeroName}】";
                        _tfTileEmptyTxts[slot].color = new Color32(134, 255, 178, 255);
                    }
                    else
                    {
                        _tfTileEmptyTxts[slot].text = "＋ 部署";
                        _tfTileEmptyTxts[slot].color = new Color32(138, 188, 212, 195);
                    }
                }
            }

            // 刷新左下圆形【阵营光环】徽章 & 右下【当前阵型】芯片
            _sim.GetFactionAuraState(out string auraTitle, out string auraDesc, out float auraPct,
                out int ironCnt, out int forestCnt, out int techCnt);
            if (auraPct > 0.001f)
            {
                HeroFaction domFac = forestCnt >= ironCnt && forestCnt >= techCnt ? HeroFaction.ForestPact
                    : (techCnt >= ironCnt ? HeroFaction.WasteTech : HeroFaction.IronGuard);
                _wwTfAuraCircleBg.color = new Color32(255, 224, 128, 255);
                _wwTfAuraCrestImg.gameObject.SetActive(true);
                _wwTfAuraCrestImg.sprite = GetFactionCrestSprite(domFac);
                _wwTfAuraStateTxt.text = $"+{Mathf.RoundToInt(auraPct * 100f)}%";
                _wwTfAuraStateTxt.color = ForestComicUiSkin.AccentGold;
                _wwTfAuraStateTxt.rectTransform.anchoredPosition = new Vector2(0f, -20f);
                _wwTfAuraSubLbl.text = "阵营光环";
            }
            else
            {
                _wwTfAuraCircleBg.color = new Color32(182, 194, 216, 255);
                _wwTfAuraCrestImg.gameObject.SetActive(false);
                _wwTfAuraStateTxt.text = "未激活";
                _wwTfAuraStateTxt.color = Color.white;
                _wwTfAuraStateTxt.rectTransform.anchoredPosition = Vector2.zero;
                _wwTfAuraSubLbl.text = "阵营光环";
            }

            _wwTfCurFmtChipIcon.sprite = GetFormationChipSprite(_sim, _sim.SelectedFormation);
            _wwTfCurFmtChipNameTxt.text = _sim.GetFormationName(_sim.SelectedFormation);

            int globPct = Mathf.RoundToInt(_sim.GetFormationGlobalBonusPct() * 100f);
            _tfFormationSummaryTxt.text =
                $"{_sim.GetFormationSummary(_sim.SelectedFormation)}\n" +
                $"<color=#FDE047>{(auraPct > 0.001f ? auraTitle : "同阵营≥2人激活光环")}</color>" +
                (globPct > 0 ? $"  ·  图谱全局+{globPct}%" : "");
            _tfFormationSummaryTxt.supportRichText = true;

            // 刷新阵营筛选条
            for (int f = 0; f < 4; f++)
            {
                bool active = f == _wwTfFactionFilterIdx;
                _wwTfFilterTabImgs[f].sprite = active ? ForestComicUiSkin.WwTabActive : ForestComicUiSkin.WwTabIdle;
                if (f == 0 && _wwTfFilterTabLbls[0] != null)
                {
                    _wwTfFilterTabLbls[0].color = active ? Color.white : (Color)ForestComicUiSkin.InkDark;
                    if (_wwTfFilterTabOutlines[0] != null) _wwTfFilterTabOutlines[0].enabled = active;
                }
            }

            if (hasSelHero)
            {
                _tfRosterHintTxt.text = $"已选中【{selHeroName}】 — 点击黑板任意格换位，或再次点击头像卸阵";
                _tfRosterHintTxt.color = ForestComicUiSkin.AccentGreen;
            }
            else
            {
                string expHint = maxDep < 5
                    ? $"（领主 Lv.{(_sim.CampLevel < 4 ? 4 : (_sim.CampLevel < 8 ? 8 : 12))} 扩编至 {maxDep + 1} 人）"
                    : "（已满5人统率）";
                _tfRosterHintTxt.text = $"点选下方英雄头像上阵/换位 {expHint}";
                _tfRosterHintTxt.color = ForestComicUiSkin.InkMuted;
            }

            // 刷新下方 9 张方形英雄近景肖像卡（支持横向滚动浏览全 9 人 + 阵营筛选 3 人左对齐）
            var visIndices = new List<int>(MaxHeroUiSlots);
            for (int i = 0; i < _sim.Heroes.Count && i < MaxHeroUiSlots; i++)
            {
                if (_wwTfFactionFilterIdx <= 0 || _sim.Heroes[i].faction == (HeroFaction)(_wwTfFactionFilterIdx - 1))
                    visIndices.Add(i);
            }

            float stepX = 116f;
            float viewW = 688f;
            float contentW = Mathf.Max(viewW, 16f + visIndices.Count * stepX - 10f);
            if (_wwTfRosterScrollContentRt != null)
            {
                _wwTfRosterScrollContentRt.sizeDelta = new Vector2(contentW, 0f);
                if (contentW <= viewW + 0.5f)
                {
                    _wwTfRosterScrollContentRt.anchoredPosition = Vector2.zero;
                }
                else
                {
                    float clampedX = Mathf.Clamp(_wwTfRosterScrollContentRt.anchoredPosition.x, -(contentW - viewW), 0f);
                    _wwTfRosterScrollContentRt.anchoredPosition = new Vector2(clampedX, 0f);
                }
            }

            for (int i = 0; i < MaxHeroUiSlots; i++)
            {
                bool show = visIndices.Contains(i);
                _tfRosterCards[i].SetActive(show);
                if (!show) continue;

                int visOrder = visIndices.IndexOf(i);
                RectTransform wrapRt = _tfRosterCards[i].GetComponent<RectTransform>();
                wrapRt.anchoredPosition = new Vector2(8f + visOrder * stepX, -4f);

                HeroSpec h = _sim.Heroes[i];
                int curSlot = _sim.FindHeroSlot(i);
                bool deployed = curSlot >= 0;
                bool selected = _selectedFormationHeroIdx == i;

                _wwTfRosterFactionIcons[i].sprite = GetFactionCrestSprite(h.faction);
                _wwTfRosterRoleIcons[i].sprite = GetRoleMiniSprite(_sim.GetHeroCombatRole(i));
                _wwTfRosterGreenChecks[i].SetActive(h.unlocked && deployed);
                _wwTfRosterBrackets[i].SetActive(selected);

                if (!h.unlocked)
                {
                    _wwTfRosterPortraitFrames[i].sprite = ForestComicUiSkin.ClippedCardGrey;
                    _tfRosterBusts[i].sprite = ForestComicUiSkin.GetArt(h.portraitKey, ForestComicUiSkin.GetArt("bust_driver"));
                    _tfRosterBusts[i].preserveAspect = true;
                    _tfRosterBusts[i].color = new Color(0.32f, 0.34f, 0.42f, 0.80f);
                    _tfRosterNames[i].text = h.name;
                    _tfRosterNames[i].color = ForestComicUiSkin.InkMuted;
                    _tfRosterRoles[i].text = $"碎片 {h.shards}/10";
                    _tfRosterRoles[i].color = ForestComicUiSkin.InkMuted;
                    _tfRosterPowers[i].text = GetHeroStageUnlockHint(i);
                    _tfRosterPowers[i].color = ForestComicUiSkin.AccentRed;
                }
                else
                {
                    _wwTfRosterPortraitFrames[i].sprite = ForestComicUiSkin.GetClippedCard(h.rarity);
                    _tfRosterBusts[i].sprite = ForestComicUiSkin.GetArt(h.portraitKey, ForestComicUiSkin.GetArt("bust_dachui"));
                    _tfRosterBusts[i].preserveAspect = true;
                    _tfRosterBusts[i].color = Color.white;
                    _tfRosterNames[i].text = $"{h.name} {h.level}级";
                    _tfRosterNames[i].color = ForestComicUiSkin.InkDark;
                    _tfRosterRoles[i].text = ForestSurvivalSim.GetFactionName(h.faction);
                    _tfRosterRoles[i].color = ForestComicUiSkin.InkMuted;
                    _tfRosterPowers[i].text = $"战力 {_sim.CalcHeroCombatPower(i, curSlot)}";
                    _tfRosterPowers[i].color = ForestComicUiSkin.AccentGreen;
                }
            }

            if (_wwFactionAuraPopup != null && _wwFactionAuraPopup.activeSelf)
                RefreshFactionAuraPopup();
            if (_wwFormationPickerPopup != null && _wwFormationPickerPopup.activeSelf)
                RefreshFormationPickerPopup();
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
                    _wwTfTileBreathers[slotIdx]?.TriggerHop();
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
                _wwTfTileBreathers[slotIdx]?.TriggerHop();
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
                    _wwTfTileBreathers[slotIdx]?.TriggerHop();
                    if (!string.IsNullOrEmpty(placeMsg)) SpawnScreenCenterBanner(placeMsg);
                    return;
                }
            }

            SpawnScreenCenterBanner("请先点击下方英雄头像或黑板上的角色，再点击此格换位部署！");
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
                if (_wwFormationPickerPopup != null) _wwFormationPickerPopup.SetActive(false);
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
                if (h.shards >= 10 && _sim.TryRecruitHeroWithShards(heroIdx, out string sMsg))
                {
                    RefreshTacticalFormationModal();
                    SpawnScreenCenterBanner(sMsg);
                    return;
                }
                SpawnScreenCenterBanner($"【{h.name}】尚未入队：{GetHeroStageUnlockHint(heroIdx)}或广播电台招募！");
                return;
            }

            int existingSlot = _sim.FindHeroSlot(heroIdx);
            // 若该英雄尚未上阵且当前还有空编，一键按射程自动摆入最优空位！
            if (existingSlot < 0 && _sim.GetDeployedHeroCount() < _sim.GetMaxDeployHeroCount())
            {
                int prefCol = h.rangeCells <= 1 ? 2 : (h.rangeCells <= 3 ? 1 : 0);
                int[] colOrder = prefCol == 2 ? new[] { 2, 1, 0 } : (prefCol == 1 ? new[] { 1, 0, 2 } : new[] { 0, 1, 2 });
                int[] rowOrder = { 1, 0, 2 };
                for (int c = 0; c < 3; c++)
                {
                    for (int r = 0; r < 3; r++)
                    {
                        int sl = rowOrder[r] * 3 + colOrder[c];
                        if (_sim.FormationSlots[sl] < 0 && _sim.TryPlaceHeroInFormation(heroIdx, sl, out string pMsg))
                        {
                            _selectedFormationHeroIdx = -1;
                            RefreshTacticalFormationModal();
                            _wwTfTileBreathers[sl]?.TriggerHop();
                            if (!string.IsNullOrEmpty(pMsg)) SpawnScreenCenterBanner(pMsg);
                            return;
                        }
                    }
                }
            }

            // 若再次点击当前已选中的已上阵角色，尝试卸下（保底留1人）
            if (_selectedFormationHeroIdx == heroIdx && existingSlot >= 0)
            {
                if (_sim.RemoveHeroFromFormation(existingSlot, out string rmMsg))
                {
                    _selectedFormationHeroIdx = -1;
                    RefreshTacticalFormationModal();
                    return;
                }
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
            if (_wwCongratsModal != null) _wwCongratsModal.SetActive(false);
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
                _arRibbonImg.sprite = ForestComicUiSkin.GetUiArt("ww_banner_congrats", ForestComicUiSkin.BtnOrange);
                if (_wwArBannerFallbackTxt != null)
                {
                    _wwArBannerFallbackTxt.gameObject.SetActive(true);
                    _wwArBannerFallbackTxt.text = HasRawForestArtAsset("ww_banner_congrats") ? "" : "挑战胜利!";
                }
                _arStageSubTxt.text = $"{stTitle}  ·  战斗用时 {Mathf.Max(1, Mathf.RoundToInt(res.durationSec))} 秒";
                _arStageSubTxt.color = ForestComicUiSkin.AccentGold;

                HeroSpec mvpHero = (res.mvpHeroIdx >= 0 && res.mvpHeroIdx < _sim.Heroes.Count)
                    ? _sim.Heroes[res.mvpHeroIdx]
                    : (_sim.Heroes.Count > 0 ? _sim.Heroes[0] : null);
                if (_wwArMvpRarityFrame != null)
                    _wwArMvpRarityFrame.sprite = mvpHero != null
                        ? ForestComicUiSkin.GetClippedCard(mvpHero.rarity)
                        : ForestComicUiSkin.ClippedCardOrange;
                _arMvpBust.sprite = mvpHero != null
                    ? ForestComicUiSkin.GetArt(mvpHero.portraitKey, ForestComicUiSkin.GetArt("bust_dachui"))
                    : ForestComicUiSkin.GetArt("bust_dachui");

                string mvpRole = (res.mvpHeroIdx >= 0 && res.mvpHeroIdx < _sim.Heroes.Count)
                    ? $"（{_sim.GetHeroRoleName(res.mvpHeroIdx)}）"
                    : "";
                string mvpDisp = !string.IsNullOrEmpty(res.mvpName)
                    ? res.mvpName
                    : (mvpHero != null ? mvpHero.name : "幸存者小队");
                _arMvpNameTxt.text = $"{mvpDisp}{mvpRole}";
                _arMvpDmgTxt.text = $"本场总贡献伤害/治疗: {Mathf.Max(1, res.mvpDamage)}";

                // 同步渲染 4 枚新版独立英雄经济战利品卡
                BattleStageSpec spec = _sim.GetBattleStageSpec(Mathf.Clamp(res.stage, 1, MaxBattleStages));
                _wwArLootFrames[0].sprite = ForestComicUiSkin.ClippedCardBlue;
                _wwArLootIcons[0].sprite = GetEconResIcon(SurvRes.HeroExp);
                _wwArLootCounts[0].text = $"+{FormatEconCount(spec.rewardHeroExp)}";

                _wwArLootFrames[1].sprite = ForestComicUiSkin.ClippedCardOrange;
                _wwArLootIcons[1].sprite = GetEconResIcon(SurvRes.RecruitTicket);
                _wwArLootCounts[1].text = $"+{Mathf.Max(1, spec.rewardRecruitTickets)}";

                _wwArLootFrames[2].sprite = ForestComicUiSkin.ClippedCardBlue;
                _wwArLootIcons[2].sprite = GetEconResIcon(SurvRes.SkillBook);
                _wwArLootCounts[2].text = $"+{Mathf.Max(1, spec.rewardSkillBooks)}";

                if (spec.rewardShardCount > 0 && spec.rewardShardHeroIdx >= 0 && spec.rewardShardHeroIdx < _sim.Heroes.Count)
                {
                    HeroSpec sh = _sim.Heroes[spec.rewardShardHeroIdx];
                    _wwArLootFrames[3].sprite = ForestComicUiSkin.GetClippedCard(sh.rarity);
                    _wwArLootIcons[3].sprite = GetHeroJigsawShardSprite(sh);
                    _wwArLootCounts[3].text = $"+{spec.rewardShardCount}";
                }
                else
                {
                    _wwArLootFrames[3].sprite = ForestComicUiSkin.ClippedCardGreen;
                    _wwArLootIcons[3].sprite = ForestComicUiSkin.GetArt("ico_coin");
                    _wwArLootCounts[3].text = $"+{spec.rewardCoins}";
                }

                _arRewardSummaryTxt.text = !string.IsNullOrEmpty(res.rewardSummary)
                    ? res.rewardSummary
                    : "缴获英雄经验、电台招募卡与战术技能书！";

                _arAutoChainBtnImg.sprite = _sim.AutoChainNextStage ? ForestComicUiSkin.BtnGreen : ForestComicUiSkin.BtnGrey;
                _arAutoChainLbl.text = _sim.AutoChainNextStage ? "[✓] 自动连续闯关：已开启" : "[ ] 自动连续闯关：未开启";
                UpdateArenaNextStageBtnLabel(res);
            }
            else
            {
                _arRibbonImg.sprite = ForestComicUiSkin.RarityRibbon(5);
                if (_wwArBannerFallbackTxt != null)
                {
                    _wwArBannerFallbackTxt.gameObject.SetActive(true);
                    _wwArBannerFallbackTxt.text = "防线失守";
                }
                _arStageSubTxt.text = $"{stTitle}  ·  营地物资零损失撤回";
                _arStageSubTxt.color = new Color32(255, 186, 174, 255);
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
                _wcHeroCardRts[i].sizeDelta = new Vector2(cardW, 184f);

                HeroSpec h = (st.heroIdx >= 0 && st.heroIdx < _sim.Heroes.Count) ? _sim.Heroes[st.heroIdx] : null;
                if (_wwWcPortraitFrames[i] != null)
                {
                    _wwWcPortraitFrames[i].sprite = !st.alive
                        ? ForestComicUiSkin.ClippedCardGrey
                        : (h != null ? ForestComicUiSkin.GetClippedCard(h.rarity) : ForestComicUiSkin.ClippedCardBlue);
                }
                _wcHeroBusts[i].sprite = h != null
                    ? ForestComicUiSkin.GetArt(h.portraitKey, ForestComicUiSkin.GetArt("bust_dachui"))
                    : ForestComicUiSkin.GetArt("bust_dachui");
                _wcHeroBusts[i].color = st.alive ? Color.white : new Color(0.32f, 0.34f, 0.42f, 0.78f);
                _wcHeroNames[i].text = $"{st.name} Lv.{(h != null ? h.level : 1)}";

                float hMax = Mathf.Max(1f, st.maxHp);
                _wcHeroHpFills[i].fillAmount = st.alive ? Mathf.Clamp01(st.hp / hMax) : 0f;
                _wcHeroShieldFills[i].fillAmount = st.alive ? Mathf.Clamp01(st.shield / hMax) : 0f;
                _wcHeroHpTxts[i].text = st.alive
                    ? (st.shield > 1f ? $"{Mathf.CeilToInt(st.hp)}+{Mathf.CeilToInt(st.shield)}" : $"{Mathf.CeilToInt(st.hp)}")
                    : "阵亡";

                _wcHeroRageFills[i].fillAmount = st.alive ? st.rage01 : 0f;
                _wcHeroRageTxts[i].text = !st.alive
                    ? "--"
                    : (st.ultReady ? "绝技就绪!" : $"怒气 {Mathf.RoundToInt(st.rage01 * 100f)}%");

                bool ultGlow = st.alive && st.ultReady;
                if (_wwWcUltBrackets[i] != null)
                    _wwWcUltBrackets[i].SetActive(ultGlow);

                if (!st.alive)
                {
                    _wcHeroActionBgs[i].sprite = ForestComicUiSkin.BtnGrey;
                    _wcHeroActionTxts[i].text = "倒地待援";
                }
                else if (st.ultReady)
                {
                    _wcHeroActionBgs[i].sprite = ForestComicUiSkin.BtnOrange;
                    _wcHeroActionTxts[i].text = $"释放·{st.skillName}";
                }
                else
                {
                    _wcHeroActionBgs[i].sprite = ForestComicUiSkin.DarkPill;
                    _wcHeroActionTxts[i].text = st.skillName;
                }
            }
        }
    }
}
