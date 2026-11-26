using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static Mori.ForestSlice.ForestSurvivalSim;

namespace Mori.ForestSlice
{
    /// <summary>
    /// 《疯狂水世界》商业级漫画 UI — 幸存者英雄总览（三阵营筛选·拼图合成·广播招募）·
    /// 独立船舱养成详情页（英雄经验独立升级·无损重置·5×5星瓣·4圆章技能升级弹窗·3槽战术徽章·4部位工坊战备）·
    /// 部门驻守委任抽屉 · 4分类切角道具仓库（加工品 / 消耗品+自选原料箱 / 碎片+阵型图谱 / 装备+徽章）
    /// </summary>
    public sealed partial class ForestSurvivalHud : MonoBehaviour
    {
        // =====================================================================
        // 共享独立经济 UI 辅助函数（供 HeroesAndBag / OrdersCombatAndRewards / 主 HUD 共用）
        // =====================================================================
        internal static string FormatEconCount(int amount)
        {
            if (amount >= 10000)
                return $"{amount / 10000f:0.##}万";
            return amount.ToString();
        }

        internal static Sprite GetEconArt(string key, Sprite fallback = null)
        {
            if (string.IsNullOrEmpty(key)) return fallback;
            Sprite s = ForestComicUiSkin.GetArt(key, null);
            if (s != null) return s;
            return ForestComicUiSkin.GetUiArt(key, fallback);
        }

        internal static Sprite GetEconResIcon(SurvRes r)
        {
            switch (r)
            {
                case SurvRes.HeroExp:
                    return GetEconArt("ico_hero_exp", ForestComicUiSkin.GetUiArt("ww_xp_star"));
                case SurvRes.RecruitTicket:
                    return GetEconArt("ico_recruit_card", ForestComicUiSkin.ResIcon(SurvRes.HeroMedal));
                case SurvRes.SkillBook:
                    return GetEconArt("ico_skill_book_1", ForestComicUiSkin.ResIcon(SurvRes.Blueprint));
                case SurvRes.ShardUnivEpic:
                    return GetEconArt("ico_shard_univ_epic", ForestComicUiSkin.ResIcon(SurvRes.HeroMedal));
                case SurvRes.ShardUnivLeg:
                    return GetEconArt("ico_shard_univ_leg", ForestComicUiSkin.ResIcon(SurvRes.HeroMedal));
                case SurvRes.ChoiceResBox:
                    return GetEconArt("ico_choice_crate", ForestComicUiSkin.ResIcon(SurvRes.Plank));
                case SurvRes.SpeedHourglass:
                    return GetEconArt("ico_speed_hourglass", ForestComicUiSkin.GetUiArt("ww_xp_star"));
                default:
                    return ForestComicUiSkin.ResIcon(r);
            }
        }

        internal static int GetEconResRarity(SurvRes r)
        {
            switch (r)
            {
                case SurvRes.RecruitTicket:
                case SurvRes.ShardUnivLeg:
                    return 4; // 橙
                case SurvRes.ShardUnivEpic:
                    return 3; // 紫
                case SurvRes.HeroExp:
                case SurvRes.SkillBook:
                case SurvRes.SpeedHourglass:
                    return 2; // 蓝
                case SurvRes.ChoiceResBox:
                    return 1; // 绿
                default:
                    return ForestComicUiSkin.GetResRarity0To5(r);
            }
        }

        internal static Sprite GetFactionCrestSprite(HeroFaction f)
        {
            string k = ForestSurvivalSim.GetFactionIconKey(f);
            return GetEconArt(k, ForestComicUiSkin.GetUiArt("ww_badge_power_fist"));
        }

        internal static Sprite GetRoleMiniSprite(HeroCombatRole role)
        {
            switch (role)
            {
                case HeroCombatRole.Guardian: return ForestComicUiSkin.GetUiArt("ww_skill_shield");
                case HeroCombatRole.Vanguard: return ForestComicUiSkin.GetUiArt("ww_skill_cleave");
                case HeroCombatRole.Demolition: return ForestComicUiSkin.GetUiArt("ww_skill_fire");
                case HeroCombatRole.Ranger: return ForestComicUiSkin.GetUiArt("ww_skill_arrow");
                case HeroCombatRole.Gunsmith: return ForestComicUiSkin.GetUiArt("ww_skill_shotgun");
                default: return ForestComicUiSkin.GetUiArt("ww_skill_heal");
            }
        }

        internal const int MaxHeroUiSlots = 9;

        /// <summary>
        /// 《疯狂水世界》漫画全身立绘解析器：优先读取 fullbody_&lt;id&gt;，仅当缺失时优雅回退至 bust_&lt;id&gt;。
        /// </summary>
        internal static Sprite GetHeroFullBodySprite(HeroSpec h)
        {
            if (h == null)
                return GetEconArt("fullbody_dachui", ForestComicUiSkin.GetArt("bust_dachui"));

            string fbKey = !string.IsNullOrEmpty(h.FullBodyKey) ? h.FullBodyKey : ("fullbody_" + h.id);
            Sprite sp = GetEconArt(fbKey, null);
            if (sp != null) return sp;

            return ForestComicUiSkin.GetArt(h.portraitKey, ForestComicUiSkin.GetArt("bust_dachui"));
        }

        internal static void ApplyHeroFullBodySprite(Image img, HeroSpec h)
        {
            if (img == null) return;
            img.sprite = GetHeroFullBodySprite(h);
            img.preserveAspect = true;
        }

        /// <summary>
        /// 轻量漫画全身像呼吸与点按弹跳组件（锚定脚底：待机 1.00-&gt;1.022 scaleY / 0.992 scaleX + 点击果冻轻跃）。
        /// </summary>
        internal sealed class WwHeroIdleBreather : MonoBehaviour
        {
            public Vector2 baseAnchoredPos;
            public float phaseOffset;
            public float hopHeight = 14f;

            RectTransform _rt;
            float _hopElapsed = 99f;
            const float HopDuration = 0.34f;

            void Awake()
            {
                _rt = GetComponent<RectTransform>();
            }

            void OnEnable()
            {
                if (_rt == null) _rt = GetComponent<RectTransform>();
                _hopElapsed = 99f;
                if (_rt != null)
                {
                    _rt.anchoredPosition = baseAnchoredPos;
                    _rt.localScale = Vector3.one;
                }
            }

            public void TriggerHop()
            {
                if (_rt == null) _rt = GetComponent<RectTransform>();
                _hopElapsed = 0f;
            }

            void Update()
            {
                if (_rt == null) return;
                float dt = Mathf.Min(0.08f, Mathf.Max(Time.deltaTime, Time.unscaledDeltaTime));
                float t = Time.unscaledTime;

                float w = 0.5f + 0.5f * Mathf.Sin(t * 2.8f + phaseOffset);
                float sx = Mathf.Lerp(1.000f, 0.992f, w);
                float sy = Mathf.Lerp(1.000f, 1.022f, w);
                float hopY = 0f;

                if (_hopElapsed < HopDuration)
                {
                    _hopElapsed += dt;
                    float u = Mathf.Clamp01(_hopElapsed / HopDuration);
                    if (u < 0.20f)
                    {
                        float k = u / 0.20f;
                        sx = Mathf.Lerp(1.00f, 1.08f, k);
                        sy = Mathf.Lerp(1.00f, 0.91f, k);
                    }
                    else if (u < 0.65f)
                    {
                        float k = (u - 0.20f) / 0.45f;
                        float arc = Mathf.Sin(k * Mathf.PI);
                        hopY = arc * hopHeight;
                        sx = Mathf.Lerp(1.08f, 0.94f, Mathf.Sin(k * Mathf.PI * 0.85f));
                        sy = Mathf.Lerp(0.91f, 1.07f, Mathf.Sin(k * Mathf.PI * 0.85f));
                    }
                    else
                    {
                        float k = (u - 0.65f) / 0.35f;
                        sx = Mathf.Lerp(1.04f, sx, k);
                        sy = Mathf.Lerp(0.96f, sy, k);
                    }
                }

                _rt.anchoredPosition = baseAnchoredPos + new Vector2(0f, hopY);
                _rt.localScale = new Vector3(sx, sy, 1f);
            }
        }

        internal static Sprite GetHeroJigsawShardSprite(HeroSpec h)
        {
            if (h == null) return GetEconArt("ico_shard_univ_epic");
            return GetEconArt("ico_shard_" + h.id, ForestComicUiSkin.GetArt(h.portraitKey, ForestComicUiSkin.GetArt("bust_dachui")));
        }

        internal static Sprite GetFormationChipSprite(ForestSurvivalSim sim, TacticalFormationId fmt)
        {
            string k = sim != null ? sim.GetFormationIconKey(fmt) : "ico_fmt_default";
            return GetEconArt(k, ForestComicUiSkin.GetUiArt("ww_xp_star"));
        }

        // =====================================================================
        // 内部状态与 UI 引用（严格独立前缀，100% 兼容自动化反射字段）
        // =====================================================================

        // --- Screen 1: 【英雄总览】全屏 3×3 九英雄漫画名册网格 ---
        int _wwRosterFilterIdx;
        readonly Button[] _wwRosterFilterBtns = new Button[4];
        readonly Image[] _wwRosterFilterImgs = new Image[4];
        readonly Text[] _wwRosterFilterLbls = new Text[4];
        readonly Outline[] _wwRosterFilterOutlines = new Outline[4];
        Text _wwRosterCountTxt, _wwRosterSquadPowerTxt;
        RectTransform _wwRosterScrollContentRt;
        readonly Image[] _wwRosterFactionCrests = new Image[MaxHeroUiSlots];
        readonly Image[] _wwRosterRarityRibbons = new Image[MaxHeroUiSlots];
        readonly Text[] _wwRosterRarityShortTxts = new Text[MaxHeroUiSlots];
        readonly GameObject[] _wwRosterDeployTags = new GameObject[MaxHeroUiSlots];
        readonly GameObject[] _wwRosterAssignTags = new GameObject[MaxHeroUiSlots];
        readonly GameObject[] _wwRosterRedDots = new GameObject[MaxHeroUiSlots];
        readonly Text[] _wwRosterStarsTxts = new Text[MaxHeroUiSlots];
        readonly GameObject[] _wwRosterLockPills = new GameObject[MaxHeroUiSlots];
        readonly Image[] _wwRosterLockPillImgs = new Image[MaxHeroUiSlots];
        readonly Text[] _wwRosterLockTxts = new Text[MaxHeroUiSlots];
        Button _wwRosterRadioRecruitBtn;
        Text _wwRosterRadioRecruitLbl;
        GameObject _wwRosterRadioRedDot;

        // --- Screen 2: 【英雄独立舱室养成详情页】(_heroDetailModal) ---
        int _wwHdHeroIdx;
        int _wwHdTabIdx;
        int _wwHdSelectedSkillIdx;
        int _wwHdSelectedBadgeSlot;
        Text _wwHdHeroExpTxt, _wwHdSkillBookTxt;
        Text _wwHdNameTxt, _wwHdRarityTxt;
        Image _wwHdRarityRibbonImg, _wwHdCabinPortrait, _wwHdCabinShadow, _wwHdStageFactionCrest;
        WwHeroIdleBreather _wwHdCabinBreather;
        GameObject _wwHdMainSquadBadgeGo;
        Text _wwHdMainSquadBadgeTxt;
        Image _wwHdMainSquadTriImg;
        GameObject _wwHdCapBannerGo;
        Text _wwHdCapBannerTxt;
        Text _wwHdPowerTxt, _wwHdRoleTagTxt, _wwHdLevelTxt;
        Image _wwHdHeaderFactionImg, _wwHdHeaderRoleImg;
        Button _wwHdResetLvBtn;
        readonly Image[] _wwHdTopStarBaseImgs = new Image[5];
        readonly Image[] _wwHdTopStarPetalFills = new Image[5];
        readonly GameObject[] _wwHdSubPages = new GameObject[4];
        readonly Image[] _wwHdBottomTabImgs = new Image[4];
        readonly Text[] _wwHdBottomTabLbls = new Text[4];
        readonly Outline[] _wwHdBottomTabOutlines = new Outline[4];
        readonly GameObject[] _wwHdBottomTabDots = new GameObject[4];

        // Sub-tab 0: 属性 + 无损重置确认弹窗 + 4圆章技能升级详情弹窗
        readonly Text[] _wwHdStatVals = new Text[6];
        readonly Image[] _wwHdSkillCircles = new Image[4];
        readonly GameObject[] _wwHdSkillSelectFrames = new GameObject[4];
        readonly GameObject[] _wwHdSkillBottomPills = new GameObject[4];
        readonly Text[] _wwHdSkillBottomPillTxts = new Text[4];
        readonly Text[] _wwHdSkillLockOverlayTxts = new Text[4];
        readonly GameObject[] _wwHdSkillUpgradeDots = new GameObject[4];
        Text _wwHdCampSpecBriefTxt;
        Button _wwHdLvUpMainBtn;
        Image _wwHdLvUpMainBtnImg, _wwHdLvUpCostIco;
        Text _wwHdLvUpMainBtnLbl;
        GameObject _wwHdLvUpRedDot;

        // 独立弹窗 A：4技能圆章详情与技能书升级弹窗 (OpenHeroSkillDetailPopup)
        GameObject _wwSkillDetailPopup;
        int _wwSkPopupSlotIdx;
        Image _wwSkPopupIcon, _wwSkPopupBookIco, _wwSkPopupUpBtnImg;
        Text _wwSkPopupTagTxt, _wwSkPopupNameTxt, _wwSkPopupLvTxt, _wwSkPopupBookCntTxt;
        Text _wwSkPopupDescTxt, _wwSkPopupMetricLbl;
        readonly Text[] _wwSkPopupTierTxts = new Text[5];
        GameObject _wwSkPopupBookGroup;
        Button _wwSkPopupUpBtn;
        Text _wwSkPopupUpBtnLbl;
        Text _wwSkPopupRedGateTxt;

        // 独立弹窗 B：100%无损等级重置确认窗
        GameObject _wwHdResetConfirmPopup;
        Text _wwHdResetConfirmDescTxt;

        // Sub-tab 1: 升星（5星×5星瓣 + 三维绿色箭头预览 + 拼图碎片进度 + 万能兑换 + 自动兑换）
        readonly Image[] _wwHdStarLeftBases = new Image[5];
        readonly Image[] _wwHdStarLeftFills = new Image[5];
        readonly Image[] _wwHdStarRightBases = new Image[5];
        readonly Image[] _wwHdStarRightFills = new Image[5];
        Text _wwHdStarStageTagTxt;
        readonly Text[] _wwHdStarCurVals = new Text[3];
        readonly Text[] _wwHdStarNextVals = new Text[3];
        Image _wwHdStarMilestoneIco;
        Text _wwHdStarMilestoneTxt;
        Image _wwHdShardCardFrame, _wwHdShardIcon, _wwHdShardBarFill;
        Text _wwHdShardTitleTxt, _wwHdShardBarTxt;
        Button _wwHdUnivExchangeBtn;
        Image _wwHdUnivExchangeIco;
        Text _wwHdUnivExchangeCntTxt;
        Button _wwHdAutoExchangeToggleBtn;
        GameObject _wwHdAutoExchangeCheckGo;
        Button _wwHdStarUpMainBtn;
        Image _wwHdStarUpMainBtnImg;
        Text _wwHdStarUpMainBtnLbl;
        GameObject _wwHdStarUpRedDot;

        // Sub-tab 2: 徽章（顶部 3 槽 1★/3★/5★ + 下方 6 种战地徽章装配列表）
        readonly Image[] _wwHdBadgeSlotFrames = new Image[3];
        readonly Image[] _wwHdBadgeSlotIcons = new Image[3];
        readonly Image[] _wwHdBadgeSlotLocks = new Image[3];
        readonly Text[] _wwHdBadgeSlotPlusTxts = new Text[3];
        readonly Text[] _wwHdBadgeSlotTitles = new Text[3];
        readonly GameObject[] _wwHdBadgeSlotBrackets = new GameObject[3];
        readonly Image[] _wwHdBadgeRowCards = new Image[6];
        readonly Image[] _wwHdBadgeRowIcons = new Image[6];
        readonly Text[] _wwHdBadgeRowNames = new Text[6];
        readonly Text[] _wwHdBadgeRowCounts = new Text[6];
        readonly Text[] _wwHdBadgeRowDescs = new Text[6];
        readonly Image[] _wwHdBadgeRowBtnImgs = new Image[6];
        readonly Text[] _wwHdBadgeRowBtnLbls = new Text[6];

        // Sub-tab 3: 装备（4部位工坊战备 2×2 打造升阶网格：武器/护甲/战靴/战术镜）
        readonly Image[] _wwHdGearSlotCards = new Image[4];
        readonly Image[] _wwHdGearClipTiles = new Image[4];
        readonly Image[] _wwHdGearIcons = new Image[4];
        readonly Text[] _wwHdGearSlotHeaders = new Text[4];
        readonly Text[] _wwHdGearTierTags = new Text[4];
        readonly Text[] _wwHdGearNames = new Text[4];
        readonly Text[] _wwHdGearStats = new Text[4];
        readonly Text[] _wwHdGearCosts = new Text[4];
        readonly Image[] _wwHdGearBtnImgs = new Image[4];
        readonly Text[] _wwHdGearBtnLbls = new Text[4];

        // --- Screen 3: 【英雄委任】半屏部门抽屉 + 绿勾选择器 ---
        GameObject _wwDelUpdateBannerGo;
        CanvasGroup _wwDelUpdateBannerCg;
        Text _wwDelUpdateBannerTxt;
        Coroutine _wwDelBannerCoroutine;
        readonly GameObject[] _wwDelVignetteBadgeGos = new GameObject[6];
        readonly Text[] _wwDelVignetteBadgeTxts = new Text[6];
        readonly Text[,] _wwDelSlotPlusTxts = new Text[6, 4];
        readonly GameObject[,] _wwDelSlotRedDots = new GameObject[6, 4];

        GameObject _wwDelPickerModal;
        int _wwPickerPostIdx;
        int _wwPickerSlotIdx;
        string _wwPickerSelectedHeroId;
        Text _wwPickerSubTitleTxt, _wwPickerEffectTxt;
        RectTransform _wwPickerScrollContentRt;
        readonly GameObject[] _wwPickerHeroCards = new GameObject[MaxHeroUiSlots];
        readonly Image[] _wwPickerHeroFrames = new Image[MaxHeroUiSlots];
        readonly Image[] _wwPickerHeroBusts = new Image[MaxHeroUiSlots];
        readonly GameObject[] _wwPickerSpecBadges = new GameObject[MaxHeroUiSlots];
        readonly GameObject[] _wwPickerBusyBadges = new GameObject[MaxHeroUiSlots];
        readonly Text[] _wwPickerHeroBonusTxts = new Text[MaxHeroUiSlots];
        readonly GameObject[] _wwPickerCheckIcons = new GameObject[MaxHeroUiSlots];
        readonly GameObject[] _wwPickerSelectBrackets = new GameObject[MaxHeroUiSlots];
        readonly int[] _wwPickerBoundHeroIdxs = new int[MaxHeroUiSlots];

        // --- Screen 4: 【道具仓库】全屏 4 列切角库存（加工品 / 消耗品 / 碎片 / 装备） + 自选原料箱弹窗 ---
        enum BagEntryKind
        {
            Resource = 0,
            HeroShard = 1,
            FormationCard = 2,
            BadgeItem = 3,
            EquipSummary = 4,
        }

        struct BagEntry
        {
            public BagEntryKind kind;
            public SurvRes res;
            public int subId;
            public string keyId;
            public string name;
            public Sprite icon;
            public int rarity0to5;
            public int count;
            public bool isCappedRes;
            public string stockTag;
            public string desc;
            public string actionLabel;
            public Sprite actionSkin;
            public Action onAction;
        }

        int _wwBagTabIdx;
        string _wwBagSelectedKey = "res_Plank";
        Text _wwBagCapHeaderTxt;
        RectTransform _wwBagGridContentRt;
        readonly List<GameObject> _wwBagSpawnedTiles = new List<GameObject>();
        readonly List<BagEntry> _wwBagCurrentEntries = new List<BagEntry>(32);
        Image _wwBagInspCardImg, _wwBagInspIconImg, _wwBagActionBtnImg;
        Text _wwBagInspCountTxt, _wwBagInspNameTxt, _wwBagInspStockTagTxt, _wwBagInspDescTxt, _wwBagActionBtnLbl;
        Action _wwBagActionCallback;
        readonly Image[] _wwBagBottomTabImgs = new Image[4];
        readonly Text[] _wwBagBottomTabLbls = new Text[4];
        readonly Outline[] _wwBagBottomTabOutlines = new Outline[4];
        readonly GameObject[] _wwBagBottomTabDots = new GameObject[4];

        // 自选原料补给箱兑换弹窗 (_wwChoiceBoxPopup)
        GameObject _wwChoiceBoxPopup;
        SurvRes _wwChoiceSelectedRes = SurvRes.Wood;
        Text _wwChoiceSubTxt;
        readonly GameObject[] _wwChoiceBrackets = new GameObject[5];
        readonly GameObject[] _wwChoiceChecks = new GameObject[5];
        readonly Text[] _wwChoiceOwnTxts = new Text[5];

        static readonly SurvRes[] ChoiceBoxCandidateRes =
        {
            SurvRes.Wood, SurvRes.Scrap, SurvRes.Water, SurvRes.Cloth, SurvRes.Herb
        };

        static readonly SurvRes[] BagCraftedAndRawItems =
        {
            // 加工成品优先 + 基础采集原料 (0..25)
            SurvRes.Plank, SurvRes.Nails, SurvRes.IronIngot, SurvRes.MetalPipe,
            SurvRes.WoodBed, SurvRes.IronBeam,
            SurvRes.LinenShirt, SurvRes.LeatherBoots, SurvRes.FurCoat, SurvRes.PlateArmor,
            SurvRes.RoastVeggie, SurvRes.VeggieSoup, SurvRes.GrillFish, SurvRes.SmokedMeat,
            SurvRes.HerbPoultice, SurvRes.WarmTea, SurvRes.MedKit,
            SurvRes.Wood, SurvRes.Scrap, SurvRes.Water, SurvRes.Veggie,
            SurvRes.Cloth, SurvRes.Herb, SurvRes.Fish, SurvRes.Meat, SurvRes.Pelt,
        };

        static readonly SurvRes[] BagConsumableItems =
        {
            SurvRes.ChoiceResBox,
            SurvRes.HeroExp,
            SurvRes.RecruitTicket,
            SurvRes.SkillBook,
            SurvRes.SpeedHourglass,
            SurvRes.Blueprint,
            SurvRes.Ration,
            SurvRes.Parts,
            SurvRes.SeedGolden,
            SurvRes.SeedVeggie,
            SurvRes.SeedFlax,
            SurvRes.SeedHerb,
            SurvRes.Compost,
        };

        // =====================================================================
        // 入口构建总控与子浮窗统一关闭
        // =====================================================================
        void BuildHeroModals()
        {
            BuildHeroSpotlightOverlay();
            BuildHeroRosterAndDelegationModals();
            BuildHeroDetailModal();
            BuildBagInventoryModal();
        }

        internal void CloseHeroAndBagSubPopups()
        {
            if (_wwSkillDetailPopup != null) _wwSkillDetailPopup.SetActive(false);
            if (_wwHdResetConfirmPopup != null) _wwHdResetConfirmPopup.SetActive(false);
            if (_wwChoiceBoxPopup != null) _wwChoiceBoxPopup.SetActive(false);
            if (_wwDelPickerModal != null) _wwDelPickerModal.SetActive(false);
        }

        // ---------------- 水世界红底弯箭头返回键工厂 (104x78) ----------------
        static Button MakeWwBackRedBtn(Transform parent, UnityAction onClick, Vector2 pos, Vector2 size)
        {
            Sprite sp = ForestComicUiSkin.GetUiArt("ww_btn_back_red", ForestComicUiSkin.BtnRed);
            RectTransform rt = ForestUiWidgets.MakeRect("WwBtnBackRed", parent, Vector2.zero, Vector2.zero, Vector2.zero, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = true;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var cols = btn.colors;
            cols.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
            btn.colors = cols;
            if (onClick != null) btn.onClick.AddListener(onClick);
            return btn;
        }

        static Text MakeCleanDarkText(string name, Transform parent, string content, int fontSize, Color color,
            TextAnchor align, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size,
            FontStyle style = FontStyle.Bold)
        {
            Text t = MakeText(name, parent, content, fontSize, color, align, anchorMin, anchorMax, pivot, pos, size, outline: false, style: style);
            Outline ol = t.GetComponent<Outline>();
            if (ol != null) ol.enabled = false;
            return t;
        }

        static void BuildFivePetalStarStrip(Transform parent, Vector2 startXStep, Vector2 starSize, Image[] baseOut, Image[] fillOut)
        {
            Sprite starSp = ForestComicUiSkin.GetUiArt("ww_xp_star");
            for (int s = 0; s < 5; s++)
            {
                Vector2 p = new Vector2(startXStep.x + s * startXStep.y, 0f);
                Image bImg = ForestUiWidgets.MakeIcon("StarBase_" + s, parent, starSp, p, starSize);
                bImg.color = new Color32(132, 140, 168, 220);
                baseOut[s] = bImg;

                RectTransform fRt = ForestUiWidgets.MakeRect("PetalFill_" + s, bImg.transform,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                Image fImg = fRt.gameObject.AddComponent<Image>();
                fImg.sprite = starSp;
                fImg.type = Image.Type.Filled;
                fImg.fillMethod = Image.FillMethod.Radial360;
                fImg.fillOrigin = (int)Image.Origin360.Top;
                fImg.fillClockwise = true;
                fImg.fillAmount = 0f;
                fImg.raycastTarget = false;
                fillOut[s] = fImg;
            }
        }

        static void ApplyFivePetalStarVisuals(Image[] baseImgs, Image[] fillImgs, bool unlocked, int fullStars, int petals0to4, bool highlightNextPetal = false)
        {
            int st = unlocked ? Mathf.Clamp(fullStars, 0, 5) : 0;
            int pt = unlocked ? Mathf.Clamp(petals0to4, 0, 5) : 0;
            if (st >= 5) pt = 0;

            for (int s = 0; s < 5; s++)
            {
                if (baseImgs[s] != null)
                    baseImgs[s].color = new Color32(132, 140, 168, 215);
                if (fillImgs[s] == null) continue;

                if (s < st)
                {
                    fillImgs[s].fillAmount = 1f;
                    fillImgs[s].color = Color.white;
                }
                else if (s == st && pt > 0)
                {
                    fillImgs[s].fillAmount = Mathf.Clamp01(pt / 5f);
                    fillImgs[s].color = highlightNextPetal
                        ? new Color32(255, 196, 52, 255)
                        : new Color32(255, 128, 42, 255);
                }
                else
                {
                    fillImgs[s].fillAmount = 0f;
                }
            }
        }

        static Button MakeWwStoneSquareTab(
            string name,
            Transform trayParent,
            Vector2 pos,
            Vector2 size,
            Sprite iconSp,
            string title,
            int fontSize,
            UnityAction onClick,
            out Image tabBgOut,
            out Text tabLblOut,
            out Outline tabOutlineOut,
            out GameObject redDotOut)
        {
            Image bg = MakeImg(name, trayParent, ForestComicUiSkin.WwTabIdle, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size, true);
            var btn = bg.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            var cols = btn.colors;
            cols.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            btn.colors = cols;
            if (onClick != null) btn.onClick.AddListener(onClick);

            ForestUiWidgets.MakeIcon("Ico", bg.transform, iconSp, new Vector2(0f, 11f), new Vector2(44f, 44f));

            Text lbl = MakeText("Lbl", bg.transform, title, fontSize, ForestComicUiSkin.InkDark, TextAnchor.LowerCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(-6f, -6f), outline: true);
            Outline ol = lbl.GetComponent<Outline>();
            if (ol != null) ol.enabled = false;

            Image dot = ForestUiWidgets.MakeIcon("RedDot", bg.transform, ForestComicUiSkin.RedDotBadge,
                new Vector2(size.x * 0.5f - 10f, size.y * 0.5f - 8f), new Vector2(24f, 24f));
            MakeText("Bang", dot.transform, "!", 16, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: false);
            dot.gameObject.SetActive(false);

            tabBgOut = bg;
            tabLblOut = lbl;
            tabOutlineOut = ol;
            redDotOut = dot.gameObject;
            return btn;
        }

        static void ApplyStoneTabVisual(Image bg, Text lbl, Outline ol, bool active)
        {
            if (bg != null) bg.sprite = active ? ForestComicUiSkin.WwTabActive : ForestComicUiSkin.WwTabIdle;
            if (lbl != null) lbl.color = active ? Color.white : (Color)ForestComicUiSkin.InkDark;
            if (ol != null) ol.enabled = active;
        }

        // ---------------- 新伙伴入队全屏高光弹层 ----------------
        void BuildHeroSpotlightOverlay()
        {
            _heroSpotlightModal = MakeModalMask("HeroSpotlightModal", 0.88f, CloseAllModals);
            _heroSpotlightModal.GetComponent<Image>().color = new Color32(18, 24, 52, 236);

            _heroSpotSunburst = ForestUiWidgets.MakeIcon("Sunburst", _heroSpotlightModal.transform, ForestComicUiSkin.Sunburst, new Vector2(0f, 46f), new Vector2(680f, 680f));
            Image topPlaque = ForestUiWidgets.MakeIcon("Banner", _heroSpotlightModal.transform,
                ForestComicUiSkin.GetUiArt("ww_banner_congrats"), new Vector2(0f, 456f), new Vector2(420f, 148f));
            if (!HasRawForestArtAsset("ww_banner_congrats"))
            {
                MakeText("HdrFallback", topPlaque.transform, "恭喜获得", 44, ForestComicUiSkin.AccentGold, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), Vector2.zero, outline: true);
            }
            MakeText("SubHdr", _heroSpotlightModal.transform, "招募幸存者英雄", 22, ForestComicUiSkin.AccentGold, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -242f), new Vector2(0f, 34f), outline: true);

            _heroSpotNameTxt = MakeText("Name", _heroSpotlightModal.transform, "大锤", 36, Color.white, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -282f), new Vector2(0f, 46f), outline: true);
            _heroSpotRoleTxt = MakeText("Role", _heroSpotlightModal.transform, "铁卫团 · 铁壁重卫 · 传说", 21, new Color32(168, 218, 255, 255), TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(0f, 32f), outline: true);

            Image frame = MakeImg("HeroCard", _heroSpotlightModal.transform, ForestComicUiSkin.ClippedCardOrange, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 34f), new Vector2(292f, 344f), false);
            Image spotShadow = ForestUiWidgets.MakeIcon("SpotShadow", frame.transform, ForestComicUiSkin.WhiteSoftCircle,
                new Vector2(0f, -138f), new Vector2(180f, 34f));
            spotShadow.preserveAspect = false;
            spotShadow.color = new Color(0.05f, 0.07f, 0.16f, 0.44f);
            _heroSpotBust = ForestUiWidgets.MakeIcon("HeroBust", frame.transform, GetEconArt("fullbody_dachui", ForestComicUiSkin.GetArt("bust_dachui")),
                new Vector2(0f, 6f), new Vector2(262f, 314f));
            _heroSpotBust.preserveAspect = true;

            Image infoCard = MakeImg("BuffSheet", _heroSpotlightModal.transform, ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 224f), new Vector2(560f, 78f), false);
            _heroSpotBuffTxt = MakeCleanDarkText("Buff", infoCard.transform, "专长说明", 19, ForestComicUiSkin.InkDark, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-28f, -12f));

            MakeBtn("BtnGreat", _heroSpotlightModal.transform, ForestComicUiSkin.BtnBlue, "收入麾下", 25,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 124f), new Vector2(236f, 70f), CloseAllModals, out _);
            _heroSpotlightModal.SetActive(false);
        }

        // =====================================================================
        // Screen 1 (英雄总览·三阵营筛选) & Screen 3 (英雄委任抽屉 + 选择委任英雄弹窗)
        // =====================================================================
        void BuildHeroRosterAndDelegationModals()
        {
            RectTransform rootRt = ForestUiWidgets.Stretch("HeroAssignModal", _canvasRt);
            _heroAssignModal = rootRt.gameObject;

            // -----------------------------------------------------------------
            // Screen 3 (Page 0): 【英雄委任】半屏部门抽屉
            // -----------------------------------------------------------------
            _haDelegationPage = ForestUiWidgets.Stretch("DelegationDrawerPage", rootRt).gameObject;

            Image topCampMask = MakeImg("UpperCampClickDismiss", _haDelegationPage.transform, null, new Color(0.05f, 0.07f, 0.16f, 0.22f),
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, -900f), true);
            topCampMask.gameObject.AddComponent<Button>().onClick.AddListener(CloseAllModals);

            Image bannerPill = MakeImg("DelUpdateBanner", _haDelegationPage.transform, ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -316f), new Vector2(476f, 52f), false);
            _wwDelUpdateBannerGo = bannerPill.gameObject;
            _wwDelUpdateBannerCg = bannerPill.gameObject.AddComponent<CanvasGroup>();
            _wwDelUpdateBannerCg.blocksRaycasts = false;
            _wwDelUpdateBannerTxt = MakeText("Txt", bannerPill.transform, "委任增益已更新！", 20, ForestComicUiSkin.AccentGold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-24f, 0f), outline: true);
            _wwDelUpdateBannerGo.SetActive(false);

            Image drawerShell = MakeImg("DrawerShell", _haDelegationPage.transform, ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 900f), true);

            Button delInfoBtn = MakeBtn("BtnDelInfo", drawerShell.transform, ForestComicUiSkin.WwTabIdle, "i", 22,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -13f), new Vector2(42f, 42f),
                () => FlashDelegationBanner("驻守英雄可同时编入出战小队，升星可大幅提高委任效率！"), out Text delInfoLbl);
            delInfoLbl.color = ForestComicUiSkin.InkDark;
            Outline delInfoOl = delInfoLbl.GetComponent<Outline>();
            if (delInfoOl != null) delInfoOl.enabled = false;

            _haHeaderTitleTxt = MakeText("DrawerTitle", drawerShell.transform, "英雄委任", 28, ForestComicUiSkin.InkHeaderLight, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(0f, 48f), outline: true);

            _haTabBtn1 = MakeBtn("BtnToRoster", drawerShell.transform, ForestComicUiSkin.BtnBlue, "英雄总览 ▶", 16,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -13f), new Vector2(124f, 42f),
                () => SwitchHeroModalTab(1), out _haTabLbl1);

            Image delLightSheet = MakeImg("DelLightSheet", drawerShell.transform, ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(692f, 726f), false);

            int postCount = Mathf.Min(6, _sim.DelegationPosts.Count);
            float scrollContentH = postCount * 164f + 12f;
            RectTransform delContent = MakeVerticalScrollArea("DelDeptScroll", delLightSheet.transform,
                new Vector2(0f, 18f), new Vector2(668f, 672f), scrollContentH);

            for (int p = 0; p < postCount; p++)
            {
                int postIdx = p;
                DelegationPostState st = _sim.DelegationPosts[p];
                Image deptCard = MakeImg("DeptCard_" + p, delContent, ForestComicUiSkin.WwSubPanel, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -8f - p * 164f), new Vector2(654f, 152f), false);

                MakeCleanDarkText("DeptTitle", deptCard.transform, GetDepartmentDisplayTitle(st.kind), 20, ForestComicUiSkin.InkDark, TextAnchor.UpperLeft,
                    new Vector2(0f, 1f), new Vector2(0.66f, 1f), new Vector2(0f, 1f), new Vector2(16f, -8f), new Vector2(0f, 28f));

                _delPostEffectTxts[p] = MakeCleanDarkText("DeptLiveBonus", deptCard.transform, "", 18, ForestComicUiSkin.AccentGreen, TextAnchor.UpperRight,
                    new Vector2(0.58f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -8f), new Vector2(0f, 28f));

                Image vigFrame = MakeImg("VignetteBox", deptCard.transform, ForestComicUiSkin.WwLightSheet, Color.white,
                    new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(14f, 10f), new Vector2(104f, 104f), false);
                ForestUiWidgets.MakeIcon("VigArt", vigFrame.transform, GetDepartmentVignetteSprite(st.kind),
                    new Vector2(0f, 4f), new Vector2(88f, 88f));

                Image vigPill = MakeImg("VigValPill", vigFrame.transform, ForestComicUiSkin.DarkPill, Color.white,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 3f), new Vector2(92f, 24f), false);
                _wwDelVignetteBadgeGos[p] = vigPill.gameObject;
                _wwDelVignetteBadgeTxts[p] = MakeText("Val", vigPill.transform, "+0%", 14, Color.white, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

                GameObject lockHintGo = ForestUiWidgets.MakeRect("LockedDeptHint", deptCard.transform,
                    new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(58f, -12f), new Vector2(-132f, -34f)).gameObject;
                _delPostLockedHintGos[p] = lockHintGo;
                _delPostLockedHintTxts[p] = MakeCleanDarkText("LockTxt", lockHintGo.transform, $"玩家{st.slotUnlockLevels[0]}级解锁",
                    24, ForestComicUiSkin.InkMuted, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

                for (int s = 0; s < 4; s++)
                {
                    int slotIdx = s;
                    float sx = 132f + s * 126f;
                    RectTransform slotBoxRt = ForestUiWidgets.MakeRect($"SlotBox_{p}_{s}", deptCard.transform,
                        new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(sx, 8f), new Vector2(114f, 108f));
                    _delSlotBoxes[p, s] = slotBoxRt.gameObject;

                    Image cardSquare = MakeImg("SlotCard", slotBoxRt, ForestComicUiSkin.WwLightSheet, Color.white,
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(88f, 84f), true);
                    _delSlotFrames[p, s] = cardSquare;
                    var slotBtn = cardSquare.gameObject.AddComponent<Button>();
                    slotBtn.targetGraphic = cardSquare;
                    slotBtn.onClick.AddListener(() => OnDelegationSlotClicked(postIdx, slotIdx));

                    RectTransform bustMaskRt = ForestUiWidgets.MakeRect("Mask", cardSquare.transform,
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), new Vector2(74f, 70f));
                    var maskImg = bustMaskRt.gameObject.AddComponent<Image>();
                    maskImg.color = new Color(1f, 1f, 1f, 0.01f);
                    maskImg.raycastTarget = false;
                    bustMaskRt.gameObject.AddComponent<Mask>().showMaskGraphic = false;

                    _delSlotBusts[p, s] = ForestUiWidgets.MakeIcon("HeroBust", bustMaskRt, ForestComicUiSkin.GetArt("bust_dachui"),
                        new Vector2(0f, -4f), new Vector2(76f, 76f));

                    _delSlotLocks[p, s] = ForestUiWidgets.MakeIcon("Padlock", cardSquare.transform, ForestComicUiSkin.GetArt("ico_lock"),
                        new Vector2(0f, 2f), new Vector2(38f, 38f));

                    _wwDelSlotPlusTxts[p, s] = MakeCleanDarkText("PlusSign", cardSquare.transform, "+", 38, ForestComicUiSkin.AccentPurple, TextAnchor.MiddleCenter,
                        Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), Vector2.zero);

                    _delSlotStarTags[p, s] = MakeText("StarTag", cardSquare.transform, "★1", 13, ForestComicUiSkin.AccentGold, TextAnchor.UpperRight,
                        Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-7f, -5f), new Vector2(-8f, -6f), outline: true);

                    Image slotDot = ForestUiWidgets.MakeIcon("RedDot", cardSquare.transform, ForestComicUiSkin.RedDotBadge,
                        new Vector2(38f, 36f), new Vector2(24f, 24f));
                    MakeText("Bang", slotDot.transform, "!", 16, Color.white, TextAnchor.MiddleCenter,
                        Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: false);
                    _wwDelSlotRedDots[p, s] = slotDot.gameObject;

                    _delSlotSubTxts[p, s] = MakeCleanDarkText("SlotSub", slotBoxRt, "", 14, ForestComicUiSkin.InkDark, TextAnchor.LowerCenter,
                        new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 1f), new Vector2(8f, 22f));
                }
            }

            Text delFooterHint = MakeCleanDarkText("DelFooterHint", delLightSheet.transform,
                "委任效果受英雄<color=#6B5CE7>品质、等级和星瓣突破</color>影响（与出战小队零冲突共享）",
                16, ForestComicUiSkin.InkMuted, TextAnchor.LowerCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(-20f, 28f));
            delFooterHint.supportRichText = true;

            MakeWwBackRedBtn(drawerShell.transform, CloseAllModals, new Vector2(18f, 14f), new Vector2(104f, 78f));

            MakeBtn("BtnUnassignAll", drawerShell.transform, ForestComicUiSkin.BtnRed, "一键卸任", 22,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-64f, 19f), new Vector2(196f, 66f),
                () =>
                {
                    _sim.UnassignAllHeroes();
                    RefreshHeroAssignModal();
                    FlashDelegationBanner("已一键卸任全部岗位英雄！");
                }, out _);

            MakeBtn("BtnAutoAssignAll", drawerShell.transform, ForestComicUiSkin.BtnBlue, "一键委任", 22,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(166f, 19f), new Vector2(196f, 66f),
                () =>
                {
                    int assigned = _sim.AutoAssignHeroes();
                    RefreshHeroAssignModal();
                    FlashDelegationBanner(assigned > 0 ? $"委任增益已更新！已按最优专长派驻 {assigned} 名英雄" : "暂无可委任英雄");
                }, out _);

            BuildDelegationPickerPopup();

            // -----------------------------------------------------------------
            // Screen 1 (Page 1): 【英雄总览】全屏三阵营筛选 + 3×2 高挑立绘名册 (720x1280)
            // -----------------------------------------------------------------
            _haGrowthPage = ForestUiWidgets.Stretch("HeroRosterFullPage", rootRt).gameObject;
            Image rosterBackdrop = _haGrowthPage.AddComponent<Image>();
            rosterBackdrop.color = new Color32(26, 33, 66, 255);
            rosterBackdrop.raycastTarget = true;

            // 顶部深靛蓝标题壳（严格 x <= 390，100% 避让微信右上角胶囊！）
            Image rosterHeader = MakeImg("RosterHeader", _haGrowthPage.transform, ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 114f), false);

            MakeText("RosterMainTitle", rosterHeader.transform, "幸存者英雄", 31, ForestComicUiSkin.InkHeaderLight, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -16f), new Vector2(360f, 42f), outline: true);

            Image leaderNotePill = MakeImg("LeaderNotePill", rosterHeader.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -62f), new Vector2(370f, 38f), false);
            _haLeaderBannerTxt = MakeText("LeaderNoteTxt", leaderNotePill.transform,
                "领主【阿澈】等级即左上角玩家等级 (Lv.1)", 14, ForestComicUiSkin.AccentGold, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-18f, 0f), outline: true);

            // 三大幸存者阵营筛选页签条：[全部] [铁卫团] [山林盟] [废土科工]
            string[] filterNames = { "全部", "铁卫团", "山林盟", "废土科工" };
            for (int f = 0; f < 4; f++)
            {
                int fIdx = f;
                float fx = -252f + f * 168f;
                Image fImg = MakeImg("FactionFilter_" + f, _haGrowthPage.transform, ForestComicUiSkin.WwTabIdle, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(fx, -146f), new Vector2(158f, 48f), true);
                _wwRosterFilterImgs[f] = fImg;
                var fBtn = fImg.gameObject.AddComponent<Button>();
                fBtn.targetGraphic = fImg;
                fBtn.onClick.AddListener(() =>
                {
                    _wwRosterFilterIdx = fIdx;
                    RefreshHeroAssignModal();
                });
                _wwRosterFilterBtns[f] = fBtn;

                if (f > 0)
                {
                    Sprite fSp = GetFactionCrestSprite((HeroFaction)(f - 1));
                    ForestUiWidgets.MakeIcon("Crest", fImg.transform, fSp, new Vector2(-48f, 0f), new Vector2(30f, 30f));
                }

                _wwRosterFilterLbls[f] = MakeText("Lbl", fImg.transform, filterNames[f], 18, ForestComicUiSkin.InkDark, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(f > 0 ? 14f : 0f, 0f), Vector2.zero, outline: true);
                _wwRosterFilterOutlines[f] = _wwRosterFilterLbls[f].GetComponent<Outline>();
                if (_wwRosterFilterOutlines[f] != null) _wwRosterFilterOutlines[f].enabled = false;
            }

            // 名册主体浅色底板 (684x976，避让顶部阵营筛选栏与底部 118px 操作栏)
            Image rosterSheet = MakeImg("RosterLightSheet", _haGrowthPage.transform, ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -178f), new Vector2(684f, 976f), false);

            _wwRosterCountTxt = MakeCleanDarkText("RosterCnt", rosterSheet.transform, "已招募伙伴：2 / 9（点击卡片进入船舱养成详情）",
                17, ForestComicUiSkin.InkMuted, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0.68f, 1f), new Vector2(0f, 1f), new Vector2(24f, -12f), new Vector2(0f, 28f));

            _wwRosterSquadPowerTxt = MakeCleanDarkText("SquadPower", rosterSheet.transform, "主力总战力 0",
                18, ForestComicUiSkin.AccentPurple, TextAnchor.UpperRight,
                new Vector2(0.62f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -12f), new Vector2(0f, 28f));

            _haTabBtn0 = MakeBtn("BtnQuickGoDelegate", rosterSheet.transform, ForestComicUiSkin.BtnBlue, "驻守委任", 17,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(176f, 44f),
                () => SwitchHeroModalTab(0), out _haTabLbl0);

            _haGrowthResTxt = MakeCleanDarkText("RosterResSummary", rosterSheet.transform, "", 15, ForestComicUiSkin.InkMuted, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(-36f, 24f));

            // 3列 × 3行 (9英雄三阵营完整矩阵) 切角品质全身立绘卡 (202x268 each, 位于 656x842 垂直滚动视口内)
            _wwRosterScrollContentRt = MakeVerticalScrollArea("RosterGridScroll", rosterSheet.transform,
                new Vector2(0f, 19f), new Vector2(656f, 842f), 846f);

            for (int i = 0; i < MaxHeroUiSlots; i++)
            {
                int heroIdx = i;
                int col = i % 3;
                int row = i / 3;
                float cx = -216f + col * 216f;
                float cy = -6f - row * 278f;

                Image card = MakeImg("HeroPortraitCard_" + i, _wwRosterScrollContentRt, ForestComicUiSkin.ClippedCardOrange, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(cx, cy), new Vector2(202f, 268f), true);
                _haRows[i] = card.gameObject;
                _haRarityFrames[i] = card;

                var cardBtn = card.gameObject.AddComponent<Button>();
                cardBtn.targetGraphic = card;
                var cCols = cardBtn.colors;
                cCols.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
                cardBtn.colors = cCols;
                cardBtn.onClick.AddListener(() => OnRosterPortraitCardClicked(heroIdx));
                _haLvUpBtns[i] = cardBtn;

                Image innerBack = MakeImg("PortraitStage", card.transform, ForestComicUiSkin.WwLightSheet, new Color(1f, 1f, 1f, 0.34f),
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(184f, 192f), false);
                innerBack.gameObject.AddComponent<Mask>().showMaskGraphic = true;

                Image cardFloorShadow = ForestUiWidgets.MakeIcon("FloorShadow", innerBack.transform, ForestComicUiSkin.WhiteSoftCircle,
                    new Vector2(0f, -66f), new Vector2(106f, 20f));
                cardFloorShadow.preserveAspect = false;
                cardFloorShadow.color = new Color(0.05f, 0.07f, 0.16f, 0.36f);

                _haBusts[i] = ForestUiWidgets.MakeIcon("Portrait", innerBack.transform,
                    GetEconArt("fullbody_dachui", ForestComicUiSkin.GetArt("bust_dachui")),
                    new Vector2(0f, 11f), new Vector2(164f, 162f));
                _haBusts[i].preserveAspect = true;

                // 右上品质绶带角标 (传 / 史 / 稀) + 阵营徽章 (ico_faction_*)
                Image rarEmblem = MakeImg("RarityBadge", card.transform, ForestComicUiSkin.RarityRibbon(4), Color.white,
                    new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-6f, -24f), new Vector2(50f, 26f), false);
                _wwRosterRarityRibbons[i] = rarEmblem;
                _wwRosterRarityShortTxts[i] = MakeText("RarChar", rarEmblem.transform, "传", 15, Color.white, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(2f, 0f), Vector2.zero, outline: true);

                _wwRosterFactionCrests[i] = ForestUiWidgets.MakeIcon("FactionCrest", card.transform,
                    GetFactionCrestSprite(HeroFaction.IronGuard), new Vector2(-68f, -35f), new Vector2(26f, 26f), anchor: new Vector2(1f, 1f));

                // 左上 `上阵` 绿签 + `驻守` 深蓝签
                Image depTag = MakeImg("DeployTag", card.transform, ForestComicUiSkin.BtnGreen, Color.white,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(6f, -8f), new Vector2(60f, 24f), false);
                MakeText("Txt", depTag.transform, "上阵", 12, Color.white, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);
                _wwRosterDeployTags[i] = depTag.gameObject;

                Image asgTag = MakeImg("AssignTag", card.transform, ForestComicUiSkin.DarkPill, Color.white,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(6f, -34f), new Vector2(60f, 22f), false);
                MakeText("Txt", asgTag.transform, "驻守", 12, ForestComicUiSkin.AccentGold, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);
                _wwRosterAssignTags[i] = asgTag.gameObject;

                // 可升级/可升星/可合成招募红点 `!`
                Image rDot = ForestUiWidgets.MakeIcon("UpgradeDot", card.transform, ForestComicUiSkin.RedDotBadge,
                    new Vector2(90f, 122f), new Vector2(26f, 26f));
                MakeText("Bang", rDot.transform, "!", 17, Color.white, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: false);
                _wwRosterRedDots[i] = rDot.gameObject;

                // 星级与星瓣横排
                _wwRosterStarsTxts[i] = MakeText("StarsRow", card.transform, "★", 15, ForestComicUiSkin.AccentGold, TextAnchor.MiddleCenter,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 65f), new Vector2(0f, 22f), outline: true);
                _wwRosterStarsTxts[i].supportRichText = true;

                // 未解锁碎片进度 / 可合成招募胶囊
                Image lockPill = MakeImg("LockProgPill", card.transform, ForestComicUiSkin.DarkPill, Color.white,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 68f), new Vector2(184f, 38f), false);
                _wwRosterLockPills[i] = lockPill.gameObject;
                _wwRosterLockPillImgs[i] = lockPill;
                _wwRosterLockTxts[i] = MakeText("LockProgTxt", lockPill.transform, "碎片 0/10", 12, ForestComicUiSkin.AccentGold, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8f, -4f), outline: true);

                // 底部深色铭牌带 (Name + Lv.X & 阵营·职业·战力)
                Image botRibbon = MakeImg("BottomDarkRibbon", card.transform, ForestComicUiSkin.WwHeaderShell, Color.white,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 7f), new Vector2(188f, 56f), false);
                _haNames[i] = MakeText("HeroNameLv", botRibbon.transform, "大锤  Lv.1", 18, Color.white, TextAnchor.UpperCenter,
                    new Vector2(0f, 0.45f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(-8f, 0f), outline: true);
                _haStarBadges[i] = MakeText("RoleAndPower", botRibbon.transform, "铁卫团 · 战力300", 12, new Color32(196, 214, 255, 255), TextAnchor.LowerCenter,
                    new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(-6f, 0f), outline: true);
            }

            // 底部 118px 导航栏：左下红弯箭头 (104x78) + 居中 [广播招募英雄] (252x72) + 右下 [战术布阵] (142x72)
            RectTransform rosterBotBar = ForestUiWidgets.MakeRect("RosterBottomBar", _haGrowthPage.transform,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 118f));

            MakeWwBackRedBtn(rosterBotBar, CloseAllModals, new Vector2(18f, 18f), new Vector2(104f, 78f));

            _wwRosterRadioRecruitBtn = MakeBtn("BtnRadioRecruitHeroes", rosterBotBar, ForestComicUiSkin.BtnOrange, "广播招募英雄", 22,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-10f, 21f), new Vector2(252f, 72f),
                OpenRadioRecruitModal, out _wwRosterRadioRecruitLbl);
            ForestUiWidgets.MakeIcon("TicketIco", _wwRosterRadioRecruitBtn.transform, GetEconResIcon(SurvRes.RecruitTicket),
                new Vector2(-94f, 0f), new Vector2(38f, 38f));
            _wwRosterRadioRecruitLbl.rectTransform.anchoredPosition = new Vector2(16f, 0f);
            Image recDot = ForestUiWidgets.MakeIcon("RecDot", _wwRosterRadioRecruitBtn.transform, ForestComicUiSkin.RedDotBadge,
                new Vector2(116f, 28f), new Vector2(26f, 26f));
            MakeText("Bang", recDot.transform, "!", 16, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: false);
            _wwRosterRadioRedDot = recDot.gameObject;

            MakeBtn("BtnOpenFormation", rosterBotBar, ForestComicUiSkin.WwTabActive, "战术布阵", 21,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 21f), new Vector2(142f, 72f),
                OpenTacticalFormationModal, out _);

            _haGrowthPage.SetActive(false);
            _heroAssignModal.SetActive(false);
        }

        void BuildDelegationPickerPopup()
        {
            RectTransform maskRt = ForestUiWidgets.Stretch("DelegationHeroPickerModal", _haDelegationPage.transform);
            var maskImg = maskRt.gameObject.AddComponent<Image>();
            maskImg.color = new Color(0.04f, 0.06f, 0.16f, 0.62f);
            maskImg.raycastTarget = true;
            var dismissBtn = maskRt.gameObject.AddComponent<Button>();
            dismissBtn.transition = Selectable.Transition.None;
            dismissBtn.onClick.AddListener(() => _wwDelPickerModal.SetActive(false));
            _wwDelPickerModal = maskRt.gameObject;

            Image dialog = MakeImg("PickerCard", maskRt, ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(624f, 468f), true);

            MakeText("Title", dialog.transform, "选择委任英雄", 26, ForestComicUiSkin.InkHeaderLight, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(0f, 44f), outline: true);

            MakeBtn("BtnClosePicker", dialog.transform, ForestComicUiSkin.BtnRed, "×", 24,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -12f), new Vector2(42f, 42f),
                () => _wwDelPickerModal.SetActive(false), out _);

            Image sheet = MakeImg("LightBody", dialog.transform, ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(588f, 308f), false);

            _wwPickerSubTitleTxt = MakeCleanDarkText("SubHdr", sheet.transform, "岗位：作坊生产提速（左右滑动选择，点击头像勾选或取消）",
                17, ForestComicUiSkin.InkDark, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-24f, 28f));

            _wwPickerScrollContentRt = MakeHorizontalScrollArea("PickerHeroScroll", sheet.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -44f), new Vector2(564f, 166f), 564f);

            for (int i = 0; i < MaxHeroUiSlots; i++)
            {
                int cardSlot = i;
                Image hCard = MakeImg("PickHero_" + i, _wwPickerScrollContentRt, ForestComicUiSkin.ClippedCardBlue, Color.white,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(8f + i * 92f, -6f), new Vector2(86f, 150f), true);
                _wwPickerHeroCards[i] = hCard.gameObject;
                _wwPickerHeroFrames[i] = hCard;

                var hBtn = hCard.gameObject.AddComponent<Button>();
                hBtn.targetGraphic = hCard;
                hBtn.onClick.AddListener(() => OnPickerHeroCardTapped(cardSlot));

                Image pMask = MakeImg("Mask", hCard.transform, ForestComicUiSkin.WwLightSheet, new Color(1f, 1f, 1f, 0.25f),
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(74f, 94f), false);
                pMask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                _wwPickerHeroBusts[i] = ForestUiWidgets.MakeIcon("Bust", pMask.transform, ForestComicUiSkin.GetArt("bust_dachui"),
                    new Vector2(0f, -4f), new Vector2(78f, 78f));

                Image specTag = MakeImg("SpecTag", hCard.transform, ForestComicUiSkin.BtnOrange, Color.white,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(3f, -4f), new Vector2(36f, 22f), false);
                MakeText("Txt", specTag.transform, "专长", 11, Color.white, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);
                _wwPickerSpecBadges[i] = specTag.gameObject;

                Image busyTag = MakeImg("BusyTag", hCard.transform, ForestComicUiSkin.DarkPill, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(70f, 20f), false);
                MakeText("Txt", busyTag.transform, "在岗", 11, ForestComicUiSkin.AccentGold, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);
                _wwPickerBusyBadges[i] = busyTag.gameObject;

                Image botBar = MakeImg("BotBar", hCard.transform, ForestComicUiSkin.WwHeaderShell, Color.white,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 5f), new Vector2(78f, 40f), false);
                _wwPickerHeroBonusTxts[i] = MakeText("BonusTxt", botBar.transform, "大锤\n+22%", 12, Color.white, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

                Image brackets = MakeImg("SelBrackets", hCard.transform, ForestComicUiSkin.SelectionCornerBrackets, Color.white,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(94f, 158f), false);
                _wwPickerSelectBrackets[i] = brackets.gameObject;

                Image chk = ForestUiWidgets.MakeIcon("GreenCheck", hCard.transform,
                    ForestComicUiSkin.GetUiArt("ww_check_green"), new Vector2(0f, 12f), new Vector2(46f, 46f));
                _wwPickerCheckIcons[i] = chk.gameObject;
            }

            Image effPanel = MakeImg("EffectBox", sheet.transform, ForestComicUiSkin.WwSubPanel, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(552f, 68f), false);
            _wwPickerEffectTxt = MakeCleanDarkText("EffTxt", effPanel.transform, "委任效果：", 18, ForestComicUiSkin.InkDark, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-24f, -8f));
            _wwPickerEffectTxt.supportRichText = true;

            MakeBtn("BtnConfirmPick", dialog.transform, ForestComicUiSkin.BtnBlue, "确 定", 24,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(228f, 64f),
                OnConfirmDelegationPicker, out _);

            _wwDelPickerModal.SetActive(false);
        }

        // =====================================================================
        // Screen 2: 【英雄独立舱室养成详情页】(_heroDetailModal — 1:1 疯狂水世界)
        // =====================================================================
        void BuildHeroDetailModal()
        {
            if (_heroDetailModal != null) return;
            RectTransform rootRt = ForestUiWidgets.Stretch("HeroCabinDetailModal", _canvasRt);
            var backdrop = rootRt.gameObject.AddComponent<Image>();
            backdrop.color = new Color32(22, 27, 62, 255);
            backdrop.raycastTarget = true;
            _heroDetailModal = rootRt.gameObject;

            // ---------------- 1. 上半部船舱插画舞台 (y = 0 .. -540, 720x540) ----------------
            RectTransform stageRt = ForestUiWidgets.MakeRect("CabinStage", rootRt,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(720f, 540f));
            var cabinImg = stageRt.gameObject.AddComponent<Image>();
            cabinImg.sprite = ForestComicUiSkin.GetUiArt("ww_hero_cabin_bg");
            cabinImg.type = Image.Type.Simple;
            cabinImg.preserveAspect = false;
            cabinImg.raycastTarget = false;

            // 左上双独立养成货币胶囊（x = 18..372 <= 392，100% 避开微信右上角胶囊安全区！）
            Image expPill = MakeImg("HdHeroExpPill", stageRt, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -20f), new Vector2(174f, 38f), true);
            expPill.gameObject.AddComponent<Button>().onClick.AddListener(OpenStagePosterModal);
            ForestUiWidgets.MakeIcon("Ico", expPill.transform, GetEconResIcon(SurvRes.HeroExp), new Vector2(-62f, 0f), new Vector2(30f, 30f));
            _wwHdHeroExpTxt = MakeText("Val", expPill.transform, "600", 17, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(14f, 0f), new Vector2(-28f, 0f), outline: true);

            Image bookPill = MakeImg("HdSkillBookPill", stageRt, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(200f, -20f), new Vector2(170f, 38f), true);
            bookPill.gameObject.AddComponent<Button>().onClick.AddListener(OpenStagePosterModal);
            ForestUiWidgets.MakeIcon("Ico", bookPill.transform, GetEconResIcon(SurvRes.SkillBook), new Vector2(-60f, 0f), new Vector2(30f, 30f));
            _wwHdSkillBookTxt = MakeText("Val", bookPill.transform, "15", 17, new Color32(168, 220, 255, 255), TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(14f, 0f), new Vector2(-28f, 0f), outline: true);

            // 左侧 `主阵容 ▼` 绿标
            RectTransform squadBadgeRt = ForestUiWidgets.MakeRect("MainSquadBadge", stageRt,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(58f, -158f), new Vector2(88f, 72f));
            _wwHdMainSquadBadgeGo = squadBadgeRt.gameObject;
            var sqBg = squadBadgeRt.gameObject.AddComponent<Image>();
            sqBg.color = new Color(1f, 1f, 1f, 0.01f);
            sqBg.raycastTarget = true;
            squadBadgeRt.gameObject.AddComponent<Button>().onClick.AddListener(OpenTacticalFormationModal);
            _wwHdMainSquadBadgeTxt = MakeText("Lbl", squadBadgeRt, "主阵容", 16, Color.white, TextAnchor.UpperCenter,
                new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), Vector2.zero, outline: true);
            _wwHdMainSquadTriImg = MakeImg("TriPill", squadBadgeRt, ForestComicUiSkin.BtnGreen, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(52f, 38f), false);
            MakeText("Tri", _wwHdMainSquadTriImg.transform, "▼", 20, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

            // 英雄居中姓名 + 斜切品质绶带
            _wwHdNameTxt = MakeText("HeroCabinName", stageRt, "大锤", 33, ForestComicUiSkin.AccentGold, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -116f), new Vector2(180f, 44f), outline: true);
            _wwHdNameTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            Outline nameOl = _wwHdNameTxt.GetComponent<Outline>();
            if (nameOl != null) nameOl.effectDistance = new Vector2(2.2f, -2.2f);

            _wwHdRarityRibbonImg = MakeImg("HeroRarityRibbon", stageRt, ForestComicUiSkin.RarityRibbon(HeroRarity.Legendary), Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -156f), new Vector2(152f, 34f), false);
            _wwHdStageFactionCrest = ForestUiWidgets.MakeIcon("StageFaction", _wwHdRarityRibbonImg.transform,
                GetFactionCrestSprite(HeroFaction.IronGuard), new Vector2(-56f, 0f), new Vector2(26f, 26f));
            _wwHdRarityTxt = MakeText("Txt", _wwHdRarityRibbonImg.transform, "传说", 19, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(10f, 0f), Vector2.zero, outline: true);

            // 船舱织毯暗色椭圆落地阴影 + 300x332 全身站立英雄（双靴稳稳踩在下方浅色卡 y=-520 以上的地毯椭圆阴影中心！）
            RectTransform shadowRt = ForestUiWidgets.MakeRect("FloorShadow", stageRt,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -490f), new Vector2(216f, 44f));
            _wwHdCabinShadow = shadowRt.gameObject.AddComponent<Image>();
            _wwHdCabinShadow.sprite = ForestComicUiSkin.WhiteSoftCircle;
            _wwHdCabinShadow.preserveAspect = false;
            _wwHdCabinShadow.color = new Color(0.05f, 0.07f, 0.16f, 0.52f);
            _wwHdCabinShadow.raycastTarget = false;

            RectTransform heroFigRt = ForestUiWidgets.MakeRect("CabinHeroFigure", stageRt,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.04f), new Vector2(0f, -492f), new Vector2(300f, 332f));
            _wwHdCabinPortrait = heroFigRt.gameObject.AddComponent<Image>();
            _wwHdCabinPortrait.sprite = GetEconArt("fullbody_dachui", ForestComicUiSkin.GetArt("bust_dachui"));
            _wwHdCabinPortrait.preserveAspect = true;
            _wwHdCabinPortrait.raycastTarget = true;

            _wwHdCabinBreather = heroFigRt.gameObject.AddComponent<WwHeroIdleBreather>();
            _wwHdCabinBreather.baseAnchoredPos = new Vector2(0f, -492f);
            _wwHdCabinBreather.hopHeight = 16f;

            var cabinHeroTapBtn = heroFigRt.gameObject.AddComponent<Button>();
            cabinHeroTapBtn.transition = Selectable.Transition.None;
            cabinHeroTapBtn.onClick.AddListener(() => _wwHdCabinBreather?.TriggerHop());

            // 左右循环切换箭头 (x = 44 & x = 676, y = -320，无缝循环全部 9 名英雄)
            MakeBtn("BtnPrevHero", stageRt, ForestComicUiSkin.DarkPill, "◀", 28,
                new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(44f, -320f), new Vector2(56f, 88f),
                () => CycleDetailHero(-1), out _);
            MakeBtn("BtnNextHero", stageRt, ForestComicUiSkin.DarkPill, "▶", 28,
                new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(676f, -320f), new Vector2(56f, 88f),
                () => CycleDetailHero(1), out _);

            // 等级上限横幅 (y = -432 半透悬浮于小腿处，1:1 复刻水世界参考图，双靴与椭圆地影在下方完整露出！)
            Image capBar = MakeImg("LevelCapBanner", stageRt, ForestComicUiSkin.DarkPill, new Color(1f, 1f, 1f, 0.86f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -432f), new Vector2(624f, 42f), false);
            _wwHdCapBannerGo = capBar.gameObject;
            _wwHdCapBannerTxt = MakeText("CapTxt", capBar.transform,
                "已达上限，需要提升<color=#FFD44C>玩家</color>等级",
                18, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20f, 0f), outline: true);
            _wwHdCapBannerTxt.supportRichText = true;

            // ---------------- 2. 下半部圆角浅色养成卡 (y = -520 .. -1136, 676x616, WwLightSheet) ----------------
            Image infoCard = MakeImg("LowerInfoCard", rootRt, ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -520f), new Vector2(676f, 616f), true);

            // 左上破框战力拳头铭牌 (216x52)
            Image powerBadge = MakeImg("PowerBadge", infoCard.transform, ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(18f, 10f), new Vector2(216f, 52f), false);
            ForestUiWidgets.MakeIcon("FistIco", powerBadge.transform, ForestComicUiSkin.GetUiArt("ww_badge_power_fist"),
                new Vector2(-76f, 2f), new Vector2(44f, 44f));
            _wwHdPowerTxt = MakeText("PowerVal", powerBadge.transform, "战力 5146", 23, ForestComicUiSkin.AccentGold, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(28f, 0f), new Vector2(-58f, 0f), outline: true);

            // 右上 5 颗五瓣星级条（满金星至 h.star + 下一颗星橙色 Radial 花瓣进度 h.starPetals / 5！）
            RectTransform starStripRt = ForestUiWidgets.MakeRect("TopStarStrip", infoCard.transform,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-22f, 6f), new Vector2(216f, 42f));
            BuildFivePetalStarStrip(starStripRt, new Vector2(-84f, 42f), new Vector2(38f, 38f), _wwHdTopStarBaseImgs, _wwHdTopStarPetalFills);

            // 卡内头部 (1:1 Ref media_1795609856810)：左侧 [阵营徽标 + 职业胶囊] + 居中 `等级: X / Max` + 右侧 `[重置]` + `[i]`
            Image rolePill = MakeImg("RoleTagPill", infoCard.transform, ForestComicUiSkin.WwSubPanel, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(20f, -56f), new Vector2(186f, 42f), false);
            _wwHdHeaderFactionImg = ForestUiWidgets.MakeIcon("FacIco", rolePill.transform,
                GetFactionCrestSprite(HeroFaction.IronGuard), new Vector2(-70f, 0f), new Vector2(30f, 30f));
            _wwHdHeaderRoleImg = ForestUiWidgets.MakeIcon("RoleIco", rolePill.transform,
                GetRoleMiniSprite(HeroCombatRole.Guardian), new Vector2(-38f, 0f), new Vector2(26f, 26f));
            _wwHdRoleTagTxt = MakeCleanDarkText("RoleTxt", rolePill.transform, "铁壁重卫", 16, ForestComicUiSkin.InkDark, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(36f, 0f), new Vector2(-74f, 0f));

            _wwHdLevelTxt = MakeCleanDarkText("LevelHeaderTxt", infoCard.transform, "等级: 1 / 5", 25, ForestComicUiSkin.InkDark, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(8f, -56f), new Vector2(236f, 42f));

            // 无损等级重置按钮 (1:1 Ref media_1795609856810)
            _wwHdResetLvBtn = MakeBtn("BtnResetHeroLv", infoCard.transform, ForestComicUiSkin.WwSubPanel, "重置", 17,
                new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-72f, -56f), new Vector2(76f, 42f),
                OnHeroResetLevelBtnClicked, out Text rstBtnLbl);
            rstBtnLbl.color = ForestComicUiSkin.AccentPurple;
            Outline rstOl = rstBtnLbl.GetComponent<Outline>();
            if (rstOl != null) rstOl.enabled = false;

            Button roleInfoBtn = MakeBtn("BtnHeroInfoTip", infoCard.transform, ForestComicUiSkin.WwSubPanel, "i", 21,
                new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-22f, -56f), new Vector2(42f, 42f),
                () =>
                {
                    if (_wwHdHeroIdx >= 0 && _wwHdHeroIdx < _sim.Heroes.Count)
                    {
                        HeroSpec cur = _sim.Heroes[_wwHdHeroIdx];
                        string fac = ForestSurvivalSim.GetFactionName(cur.faction);
                        SpawnScreenCenterBanner($"【{cur.name} · {fac} · {_sim.GetHeroRoleName(_wwHdHeroIdx)}】领主等级×5 = 英雄上限(Lv.{_sim.MaxHeroLevelCap}) · 委任专长：{cur.buffDesc}");
                    }
                }, out Text infoBtnLbl);
            infoBtnLbl.color = ForestComicUiSkin.InkDark;
            Outline infoOl = infoBtnLbl.GetComponent<Outline>();
            if (infoOl != null) infoOl.enabled = false;

            MakeImg("HdrDivider", infoCard.transform, null, ForestComicUiSkin.DividerLine,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -88f), new Vector2(628f, 2f), false);

            // ---------------- 4 个子页签容器 (y = -94 .. -606) ----------------
            BuildHeroDetailSubTab0_Stats(infoCard.transform);
            BuildHeroDetailSubTab1_StarUp(infoCard.transform);
            BuildHeroDetailSubTab2_Badges(infoCard.transform);
            BuildHeroDetailSubTab3_Equip(infoCard.transform);

            // ---------------- 3. 底部 122px 石质导航栏：[属性] [升星] [徽章] [装备] ----------------
            RectTransform botNavRt = ForestUiWidgets.MakeRect("DetailBottomNav", rootRt,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 122f));

            MakeWwBackRedBtn(botNavRt, () => OpenHeroModal(1), new Vector2(16f, 18f), new Vector2(104f, 78f));

            Image stoneTray = MakeImg("DetailStoneTray", botNavRt, ForestComicUiSkin.WwStoneTray, Color.white,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-14f, 10f), new Vector2(496f, 104f), true);

            string[] hdTabNames = { "属性", "升星", "徽章", "装备" };
            Sprite[] hdTabIcons =
            {
                ForestComicUiSkin.GetUiArt("ww_passive_sword"),
                ForestComicUiSkin.GetUiArt("ww_xp_star"),
                GetEconArt("ico_badge_energy", ForestComicUiSkin.GetUiArt("ww_badge_power_fist")),
                GetEconArt("ico_gear_armor", ForestComicUiSkin.ResIcon(SurvRes.PlateArmor)),
            };
            for (int t = 0; t < 4; t++)
            {
                int tabIdx = t;
                float tx = -178.5f + t * 119f;
                MakeWwStoneSquareTab(
                    "HdTab_" + t, stoneTray.transform, new Vector2(tx, 0f), new Vector2(108f, 86f),
                    hdTabIcons[t], hdTabNames[t], 18,
                    () =>
                    {
                        _wwHdTabIdx = tabIdx;
                        RefreshHeroDetailModal();
                    },
                    out _wwHdBottomTabImgs[t],
                    out _wwHdBottomTabLbls[t],
                    out _wwHdBottomTabOutlines[t],
                    out _wwHdBottomTabDots[t]);
            }

            // 叠加在英雄舱室顶层的 2 个弹窗：4技能升级弹窗 & 无损重置确认窗
            BuildHeroSkillDetailPopupOverlay(rootRt);
            BuildHeroResetConfirmOverlay(rootRt);

            _heroDetailModal.SetActive(false);
        }

        // ---------------- Sub-Tab 0: [属性] (6维斑马属性表 + 4圆章技能点开详情 + 英雄经验独立升级) ----------------
        void BuildHeroDetailSubTab0_Stats(Transform cardParent)
        {
            RectTransform page = ForestUiWidgets.MakeRect("SubPage0_Stats", cardParent,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -94f), new Vector2(636f, 512f));
            _wwHdSubPages[0] = page.gameObject;

            string[] statNames = { "生命", "攻击范围", "攻击", "护甲", "技能强度", "抗性" };
            Sprite[] statIcons =
            {
                ForestComicUiSkin.GetUiArt("ww_passive_heart"),
                ForestComicUiSkin.GetUiArt("ww_skill_arrow"),
                ForestComicUiSkin.GetUiArt("ww_passive_sword"),
                ForestComicUiSkin.GetUiArt("ww_skill_shield"),
                ForestComicUiSkin.GetUiArt("ww_xp_star"),
                ForestComicUiSkin.GetUiArt("ww_passive_fury"),
            };

            for (int r = 0; r < 3; r++)
            {
                float ry = -6f - r * 54f;
                RectTransform bandRt;
                if (r % 2 == 0)
                {
                    Image zebra = MakeImg("ZebraRow_" + r, page, ForestComicUiSkin.WwZebraRow, Color.white,
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, ry), new Vector2(628f, 52f), false);
                    bandRt = zebra.rectTransform;
                }
                else
                {
                    bandRt = ForestUiWidgets.MakeRect("PlainRow_" + r, page,
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, ry), new Vector2(628f, 52f));
                }

                for (int c = 0; c < 2; c++)
                {
                    int idx = r * 2 + c;
                    float xOffset = c == 0 ? 12f : 326f;
                    RectTransform cellRt = ForestUiWidgets.MakeRect("StatCell_" + idx, bandRt,
                        new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(xOffset, 0f), new Vector2(290f, 0f));

                    ForestUiWidgets.MakeIcon("Ico", cellRt, statIcons[idx], new Vector2(18f, 0f), new Vector2(28f, 28f), new Vector2(0f, 0.5f));
                    MakeCleanDarkText("StatName", cellRt, statNames[idx], 20, ForestComicUiSkin.InkMuted, TextAnchor.MiddleLeft,
                        new Vector2(0f, 0f), new Vector2(0.64f, 1f), new Vector2(0f, 0.5f), new Vector2(40f, 0f), Vector2.zero);
                    _wwHdStatVals[idx] = MakeCleanDarkText("StatVal", cellRt, "0", 21, ForestComicUiSkin.InkDark, TextAnchor.MiddleRight,
                        new Vector2(0.45f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), Vector2.zero);
                }
            }

            // 4 枚 112x112 圆形技能勋章横排（点击任意圆章弹出 568x624 独立技能详情与升级卡！）
            for (int m = 0; m < 4; m++)
            {
                int medIdx = m;
                float mx = -216f + m * 144f;
                RectTransform medBox = ForestUiWidgets.MakeRect("SkillMedallion_" + m, page,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(mx, -242f), new Vector2(112f, 112f));

                var cirImg = medBox.gameObject.AddComponent<Image>();
                cirImg.sprite = ForestComicUiSkin.GetUiArt("ww_skill_shield");
                cirImg.preserveAspect = true;
                cirImg.raycastTarget = true;
                _wwHdSkillCircles[m] = cirImg;

                var mBtn = medBox.gameObject.AddComponent<Button>();
                mBtn.targetGraphic = cirImg;
                mBtn.onClick.AddListener(() => OpenHeroSkillDetailPopup(_wwHdHeroIdx, medIdx));

                Image selBr = MakeImg("SelBr", medBox, ForestComicUiSkin.SelectionCornerBrackets, Color.white,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(122f, 122f), false);
                _wwHdSkillSelectFrames[m] = selBr.gameObject;

                _wwHdSkillLockOverlayTxts[m] = MakeText("LockOverlay", medBox, m == 0 ? "" : $"{m}星解锁", 17, Color.white, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6f, 0f), outline: true);

                Image botPill = MakeImg("LvPill", medBox, ForestComicUiSkin.DarkPill, Color.white,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(74f, 24f), false);
                _wwHdSkillBottomPills[m] = botPill.gameObject;
                _wwHdSkillBottomPillTxts[m] = MakeText("Txt", botPill.transform, "1级", 14, Color.white, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

                Image skDot = ForestUiWidgets.MakeIcon("SkUpDot", medBox, ForestComicUiSkin.RedDotBadge,
                    new Vector2(42f, 42f), new Vector2(24f, 24f));
                MakeText("Bang", skDot.transform, "!", 15, Color.white, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: false);
                _wwHdSkillUpgradeDots[m] = skDot.gameObject;
            }

            // 委任专长简明联动条（保留点击跳转委任，无描边 InkMuted）
            Image specStrip = MakeImg("CampSpecMiniStrip", page, ForestComicUiSkin.WwSubPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -316f), new Vector2(620f, 68f), true);
            specStrip.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenHeroModal(0));
            _wwHdCampSpecBriefTxt = MakeCleanDarkText("SpecBriefTxt", specStrip.transform, "", 16, ForestComicUiSkin.InkMuted, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-24f, -8f));
            _wwHdCampSpecBriefTxt.supportRichText = true;

            // 底部主升级按钮（唯一消耗 ico_hero_exp 英雄经验，或未解锁时合成招募）
            _wwHdLvUpMainBtn = MakeBtn("BtnDetailLevelUp", page, ForestComicUiSkin.BtnBlue, "升级 · 英雄经验 50", 20,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(276f, 76f),
                OnDetailLevelUpClicked, out _wwHdLvUpMainBtnLbl);
            _wwHdLvUpMainBtnImg = _wwHdLvUpMainBtn.GetComponent<Image>();
            _wwHdLvUpCostIco = ForestUiWidgets.MakeIcon("ExpIco", _wwHdLvUpMainBtn.transform,
                GetEconResIcon(SurvRes.HeroExp), new Vector2(-102f, 0f), new Vector2(34f, 34f));
            _wwHdLvUpMainBtnLbl.rectTransform.anchoredPosition = new Vector2(14f, 0f);

            Image lvDot = ForestUiWidgets.MakeIcon("LvRedDot", _wwHdLvUpMainBtn.transform, ForestComicUiSkin.RedDotBadge,
                new Vector2(128f, 30f), new Vector2(28f, 28f));
            MakeText("Bang", lvDot.transform, "!", 18, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: false);
            _wwHdLvUpRedDot = lvDot.gameObject;
        }

        // ---------------- 4技能独立详情与技能书升级卡 (1:1 Ref media_1795609871150 / 8090c9eb) ----------------
        void BuildHeroSkillDetailPopupOverlay(Transform detailRoot)
        {
            RectTransform maskRt = ForestUiWidgets.Stretch("HeroSkillDetailPopup", detailRoot);
            var maskImg = maskRt.gameObject.AddComponent<Image>();
            maskImg.color = new Color32(12, 16, 36, 178);
            maskImg.raycastTarget = true;
            var closeBtn = maskRt.gameObject.AddComponent<Button>();
            closeBtn.transition = Selectable.Transition.None;
            closeBtn.onClick.AddListener(() => _wwSkillDetailPopup.SetActive(false));
            _wwSkillDetailPopup = maskRt.gameObject;

            // 外层深色漫画描边衬框 + WwLightSheet 568x624 主卡
            Image outerBorder = MakeImg("SkPopupOuter", maskRt, ForestComicUiSkin.DialogOuter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(576f, 628f), true);
            Image card = MakeImg("SkPopupSheet", outerBorder.transform, ForestComicUiSkin.WwLightSheet, Color.white,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-10f, -10f), false);

            MakeBtn("BtnCloseSkPopup", outerBorder.transform, ForestComicUiSkin.WwSubPanel, "×", 26,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -14f), new Vector2(42f, 42f),
                () => _wwSkillDetailPopup.SetActive(false), out Text xLbl);
            xLbl.color = ForestComicUiSkin.InkMuted;
            Outline xOl = xLbl.GetComponent<Outline>();
            if (xOl != null) xOl.enabled = false;

            // 左上圆形技能勋章 (114x114) + 底部主动/被动小签
            RectTransform medRt = ForestUiWidgets.MakeRect("SkMedallion", card.transform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -24f), new Vector2(114f, 114f));
            _wwSkPopupIcon = medRt.gameObject.AddComponent<Image>();
            _wwSkPopupIcon.sprite = ForestComicUiSkin.GetUiArt("ww_skill_shield");
            _wwSkPopupIcon.preserveAspect = true;

            Image tagPill = MakeImg("TypeTag", medRt, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(68f, 24f), false);
            _wwSkPopupTagTxt = MakeText("Txt", tagPill.transform, "主动", 13, new Color32(158, 226, 255, 255), TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

            // 技能名称 + 等级 + 右侧技能书拥有/消耗
            _wwSkPopupNameTxt = MakeCleanDarkText("SkName", card.transform, "贯日散射箭雨", 28, ForestComicUiSkin.InkDark, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(162f, -34f), new Vector2(-230f, 40f));

            _wwSkPopupLvTxt = MakeCleanDarkText("SkLv", card.transform, "1级", 22, ForestComicUiSkin.InkMuted, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(162f, -86f), new Vector2(120f, 32f));

            RectTransform bookGrp = ForestUiWidgets.MakeRect("BookCostGrp", card.transform,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -82f), new Vector2(180f, 38f));
            _wwSkPopupBookGroup = bookGrp.gameObject;
            _wwSkPopupBookIco = ForestUiWidgets.MakeIcon("BookIco", bookGrp, GetEconResIcon(SurvRes.SkillBook),
                new Vector2(-46f, 0f), new Vector2(32f, 32f), anchor: new Vector2(0.5f, 0.5f));
            _wwSkPopupBookCntTxt = MakeCleanDarkText("BookCnt", bookGrp, "15/10", 20, ForestComicUiSkin.InkDark, TextAnchor.MiddleRight,
                new Vector2(0.35f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);
            _wwSkPopupBookCntTxt.supportRichText = true;

            MakeImg("HeaderLine", card.transform, null, ForestComicUiSkin.DividerLine,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(-190f, 2f), false)
                .rectTransform.anchoredPosition = new Vector2(68f, -132f);

            // 技能描述浅蓝内框 (WwSubPanel, 512x122)
            Image descBox = MakeImg("SkDescBox", card.transform, ForestComicUiSkin.WwSubPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -158f), new Vector2(512f, 122f), false);
            _wwSkPopupDescTxt = MakeCleanDarkText("Desc", descBox.transform, "", 19, ForestComicUiSkin.InkDark, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-36f, -20f));
            _wwSkPopupDescTxt.lineSpacing = 1.28f;
            _wwSkPopupDescTxt.supportRichText = true;

            // 升级效果标题 + 5档横向预览表
            MakeCleanDarkText("UpEffHdr", card.transform, "升级效果", 21, ForestComicUiSkin.InkMuted, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -304f), new Vector2(0f, 34f));

            RectTransform tierRowRt = ForestUiWidgets.MakeRect("TierTableRow", card.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -352f), new Vector2(512f, 54f));
            MakeImg("RowZebra", tierRowRt, ForestComicUiSkin.WwZebraRow, new Color(1f, 1f, 1f, 0.72f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, false);

            _wwSkPopupMetricLbl = MakeCleanDarkText("MetricLbl", tierRowRt, "技能强度系数", 17, ForestComicUiSkin.InkMuted, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(142f, 0f));

            for (int t = 0; t < 5; t++)
            {
                float tx = 162f + t * 69f;
                _wwSkPopupTierTxts[t] = MakeCleanDarkText("Tier_" + t, tierRowRt, "100%", 18, ForestComicUiSkin.InkMuted, TextAnchor.MiddleCenter,
                    new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(tx, 0f), new Vector2(68f, 0f));
            }

            // 底部：升级技能按钮 OR 红色星级门槛提示
            _wwSkPopupRedGateTxt = MakeCleanDarkText("StarGateRedTxt", card.transform, "英雄升星到 ★☆☆☆☆ 可升级", 21, ForestComicUiSkin.AccentRed,
                TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 42f), new Vector2(-32f, 44f));
            _wwSkPopupRedGateTxt.supportRichText = true;

            _wwSkPopupUpBtn = MakeBtn("BtnUpgradeSkill", card.transform, ForestComicUiSkin.BtnBlue, "升级技能", 23,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(256f, 68f),
                OnUpgradeSkillInPopupClicked, out _wwSkPopupUpBtnLbl);
            _wwSkPopupUpBtnImg = _wwSkPopupUpBtn.GetComponent<Image>();

            _wwSkillDetailPopup.SetActive(false);
        }

        void BuildHeroResetConfirmOverlay(Transform detailRoot)
        {
            RectTransform maskRt = ForestUiWidgets.Stretch("HeroResetConfirmPopup", detailRoot);
            var maskImg = maskRt.gameObject.AddComponent<Image>();
            maskImg.color = new Color32(12, 16, 36, 178);
            maskImg.raycastTarget = true;
            maskRt.gameObject.AddComponent<Button>().onClick.AddListener(() => _wwHdResetConfirmPopup.SetActive(false));
            _wwHdResetConfirmPopup = maskRt.gameObject;

            Image card = MakeImg("ResetDialog", maskRt, ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540f, 380f), true);
            MakeText("Hdr", card.transform, "100% 无损等级重置", 26, ForestComicUiSkin.InkHeaderLight, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(0f, 42f), outline: true);

            Image body = MakeImg("Sheet", card.transform, ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(500f, 214f), false);
            _wwHdResetConfirmDescTxt = MakeCleanDarkText("Desc", body.transform, "", 19, ForestComicUiSkin.InkDark, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-36f, -24f));
            _wwHdResetConfirmDescTxt.lineSpacing = 1.3f;
            _wwHdResetConfirmDescTxt.supportRichText = true;

            MakeBtn("BtnCancelReset", card.transform, ForestComicUiSkin.BtnGrey, "取 消", 22,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-118f, 18f), new Vector2(196f, 62f),
                () => _wwHdResetConfirmPopup.SetActive(false), out _);

            MakeBtn("BtnConfirmReset", card.transform, ForestComicUiSkin.BtnBlue, "免费重置", 22,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(118f, 18f), new Vector2(196f, 62f),
                OnConfirmHeroResetLevel, out _);

            _wwHdResetConfirmPopup.SetActive(false);
        }

        // ---------------- Sub-Tab 1: [升星] (1:1 Ref media_1795609880015 — 五瓣星 + 绿箭头 + 拼图碎片 + 万能兑换) ----------------
        void BuildHeroDetailSubTab1_StarUp(Transform cardParent)
        {
            RectTransform page = ForestUiWidgets.MakeRect("SubPage1_StarUp", cardParent,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -94f), new Vector2(636f, 512f));
            _wwHdSubPages[1] = page.gameObject;

            // 1. 顶部 5瓣星对比栏 (620x74 WwSubPanel)
            Image topStarBar = MakeImg("TopPetalCmpBar", page, ForestComicUiSkin.WwSubPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(620f, 74f), false);

            RectTransform leftStarsRt = ForestUiWidgets.MakeRect("LeftStars", topStarBar.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-168f, 8f), new Vector2(210f, 38f));
            BuildFivePetalStarStrip(leftStarsRt, new Vector2(-80f, 40f), new Vector2(36f, 36f), _wwHdStarLeftBases, _wwHdStarLeftFills);

            MakeCleanDarkText("MidArrows", topStarBar.transform, "▶▶", 22, ForestComicUiSkin.InkMuted, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(64f, 34f));

            RectTransform rightStarsRt = ForestUiWidgets.MakeRect("RightStars", topStarBar.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(168f, 8f), new Vector2(210f, 38f));
            BuildFivePetalStarStrip(rightStarsRt, new Vector2(-80f, 40f), new Vector2(36f, 36f), _wwHdStarRightBases, _wwHdStarRightFills);

            _wwHdStarStageTagTxt = MakeCleanDarkText("PetalTag", topStarBar.transform, "★0 · 1/5瓣  ▶▶  ★0 · 2/5瓣", 14,
                ForestComicUiSkin.AccentPurple, TextAnchor.LowerCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 3f), new Vector2(-16f, 22f));

            // 2. 3行斑马属性跳涨对比框 (620x148)
            Image statCmpBox = MakeImg("ThreeStatCmpBox", page, ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(620f, 148f), false);
            string[] cmpNames = { "生命", "攻击", "技能强度" };
            Sprite[] cmpIcons =
            {
                ForestComicUiSkin.GetUiArt("ww_passive_heart"),
                ForestComicUiSkin.GetUiArt("ww_passive_sword"),
                ForestComicUiSkin.GetUiArt("ww_xp_star"),
            };
            for (int r = 0; r < 3; r++)
            {
                float ry = -4f - r * 47f;
                RectTransform rowRt;
                if (r % 2 == 0)
                {
                    Image zb = MakeImg("Zb_" + r, statCmpBox.transform, ForestComicUiSkin.WwZebraRow, Color.white,
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, ry), new Vector2(608f, 44f), false);
                    rowRt = zb.rectTransform;
                }
                else
                {
                    rowRt = ForestUiWidgets.MakeRect("Pl_" + r, statCmpBox.transform,
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, ry), new Vector2(608f, 44f));
                }

                ForestUiWidgets.MakeIcon("Ico", rowRt, cmpIcons[r], new Vector2(28f, 0f), new Vector2(28f, 28f), new Vector2(0f, 0.5f));
                MakeCleanDarkText("Lbl", rowRt, cmpNames[r], 20, ForestComicUiSkin.InkMuted, TextAnchor.MiddleLeft,
                    new Vector2(0f, 0f), new Vector2(0.45f, 1f), new Vector2(0f, 0.5f), new Vector2(54f, 0f), Vector2.zero);

                _wwHdStarCurVals[r] = MakeCleanDarkText("Cur", rowRt, "2043", 21, ForestComicUiSkin.InkDark, TextAnchor.MiddleRight,
                    new Vector2(0.40f, 0f), new Vector2(0.62f, 1f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);

                MakeCleanDarkText("Arr", rowRt, "▶▶", 17, ForestComicUiSkin.InkMuted, TextAnchor.MiddleCenter,
                    new Vector2(0.63f, 0f), new Vector2(0.74f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

                _wwHdStarNextVals[r] = MakeCleanDarkText("Nxt", rowRt, "2554", 21, ForestComicUiSkin.AccentGreen, TextAnchor.MiddleLeft,
                    new Vector2(0.75f, 0f), new Vector2(0.98f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            }

            // 3. 下一整星解锁里程碑条 (620x52 WwSubPanel)
            Image mileStrip = MakeImg("MilestoneStrip", page, ForestComicUiSkin.WwSubPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(620f, 50f), false);
            _wwHdStarMilestoneIco = ForestUiWidgets.MakeIcon("MileIco", mileStrip.transform,
                ForestComicUiSkin.GetUiArt("ww_passive_fury"), new Vector2(-282f, 0f), new Vector2(36f, 36f));
            _wwHdStarMilestoneTxt = MakeCleanDarkText("MileTxt", mileStrip.transform, "", 16, ForestComicUiSkin.InkDark, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(22f, 0f), new Vector2(-64f, 0f));
            _wwHdStarMilestoneTxt.supportRichText = true;

            // 4. 英雄专属拼图碎片进度行 + 右侧 [万能碎片兑换] 方块钮 (1:1 Ref media_1795609880015)
            Image shardRow = MakeImg("JigsawShardRow", page, ForestComicUiSkin.WwSubPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -296f), new Vector2(620f, 96f), false);

            _wwHdShardCardFrame = MakeImg("ShardClipCard", shardRow.transform, ForestComicUiSkin.ClippedCardOrange, Color.white,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(74f, 74f), false);
            _wwHdShardIcon = ForestUiWidgets.MakeIcon("ShardIco", _wwHdShardCardFrame.transform,
                GetEconArt("ico_shard_dachui", ForestComicUiSkin.GetArt("bust_dachui")), Vector2.zero, new Vector2(60f, 60f));

            _wwHdShardTitleTxt = MakeCleanDarkText("ShardName", shardRow.transform, "大锤碎片", 20, ForestComicUiSkin.InkDark, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(102f, -14f), new Vector2(-206f, 30f));

            Image barBg = MakeImg("ShardProgBar", shardRow.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(102f, 14f), new Vector2(414f, 30f), false);
            RectTransform bfRt = ForestUiWidgets.MakeRect("Fill", barBg.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6f, -6f));
            _wwHdShardBarFill = bfRt.gameObject.AddComponent<Image>();
            _wwHdShardBarFill.sprite = ForestComicUiSkin.BtnOrange;
            _wwHdShardBarFill.type = Image.Type.Filled;
            _wwHdShardBarFill.fillMethod = Image.FillMethod.Horizontal;
            _wwHdShardBarFill.raycastTarget = false;
            _wwHdShardBarTxt = MakeText("ProgTxt", barBg.transform, "4/2", 16, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

            // 右侧 78x78 万能拼图兑换钮
            Image univBg = MakeImg("BtnUnivExchange", shardRow.transform, ForestComicUiSkin.WwTabIdle, Color.white,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(78f, 78f), true);
            _wwHdUnivExchangeBtn = univBg.gameObject.AddComponent<Button>();
            _wwHdUnivExchangeBtn.targetGraphic = univBg;
            _wwHdUnivExchangeBtn.onClick.AddListener(OnUniversalShardExchangeClicked);

            _wwHdUnivExchangeIco = ForestUiWidgets.MakeIcon("UnivIco", univBg.transform,
                GetEconResIcon(SurvRes.ShardUnivLeg), new Vector2(0f, 10f), new Vector2(46f, 46f));
            _wwHdUnivExchangeCntTxt = MakeText("UnivCnt", univBg.transform, "0", 15, Color.white, TextAnchor.LowerRight,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-2f, 8f), new Vector2(-14f, -8f), outline: true);
            MakeCleanDarkText("ExLbl", univBg.transform, "兑换", 14, ForestComicUiSkin.InkDark, TextAnchor.LowerCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0f), new Vector2(0f, 3f), Vector2.zero);

            // 5. 底部居中 [升 星] (240x72 BtnBlue) + 右下 [✓ 自动兑换] 复选框
            _wwHdStarUpMainBtn = MakeBtn("BtnDetailStarUp", page, ForestComicUiSkin.BtnBlue, "升 星", 25,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(240f, 72f),
                OnDetailStarUpClicked, out _wwHdStarUpMainBtnLbl);
            _wwHdStarUpMainBtnImg = _wwHdStarUpMainBtn.GetComponent<Image>();

            Image stDot = ForestUiWidgets.MakeIcon("StarRedDot", _wwHdStarUpMainBtn.transform, ForestComicUiSkin.RedDotBadge,
                new Vector2(110f, 28f), new Vector2(28f, 28f));
            MakeText("Bang", stDot.transform, "!", 18, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: false);
            _wwHdStarUpRedDot = stDot.gameObject;

            RectTransform autoRt = ForestUiWidgets.MakeRect("BtnAutoExchangeToggle", page,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(-12f, 52f), new Vector2(148f, 44f));
            var autoHit = autoRt.gameObject.AddComponent<Image>();
            autoHit.color = new Color(1f, 1f, 1f, 0.01f);
            _wwHdAutoExchangeToggleBtn = autoRt.gameObject.AddComponent<Button>();
            _wwHdAutoExchangeToggleBtn.targetGraphic = autoHit;
            _wwHdAutoExchangeToggleBtn.onClick.AddListener(() =>
            {
                _sim.AutoExchangeUniversalShards = !_sim.AutoExchangeUniversalShards;
                RefreshHeroDetailModal();
            });

            Image chkBox = MakeImg("ChkBox", autoRt, ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(6f, 0f), new Vector2(32f, 32f), false);
            Image chkMark = ForestUiWidgets.MakeIcon("Chk", chkBox.transform,
                ForestComicUiSkin.GetUiArt("ww_check_green"), Vector2.zero, new Vector2(28f, 28f));
            _wwHdAutoExchangeCheckGo = chkMark.gameObject;
            MakeCleanDarkText("AutoLbl", autoRt, "自动兑换", 17, ForestComicUiSkin.InkMuted, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(44f, 0f), new Vector2(-46f, 0f));
        }

        // ---------------- Sub-Tab 2: [徽章] (3槽 1★/3★/5★ + 6枚战术徽章装配列表) ----------------
        void BuildHeroDetailSubTab2_Badges(Transform cardParent)
        {
            RectTransform page = ForestUiWidgets.MakeRect("SubPage2_Badges", cardParent,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -94f), new Vector2(636f, 512f));
            _wwHdSubPages[2] = page.gameObject;

            // 顶部 3 个徽章装备槽 (1★解锁 / 3★解锁 / 5★解锁)
            Image topSlotsBox = MakeImg("BadgeSlotsBar", page, ForestComicUiSkin.WwSubPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(620f, 126f), false);

            int[] slotUnlockStars = { 1, 3, 5 };
            for (int s = 0; s < 3; s++)
            {
                int slotIdx = s;
                float sx = -196f + s * 196f;
                RectTransform slotRt = ForestUiWidgets.MakeRect("BadgeSlot_" + s, topSlotsBox.transform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(sx, 0f), new Vector2(180f, 112f));

                Image sCard = MakeImg("Card", slotRt, ForestComicUiSkin.WwLightSheet, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(78f, 78f), true);
                _wwHdBadgeSlotFrames[s] = sCard;
                var sBtn = sCard.gameObject.AddComponent<Button>();
                sBtn.targetGraphic = sCard;
                sBtn.onClick.AddListener(() => OnBadgeSlotClicked(slotIdx));

                _wwHdBadgeSlotIcons[s] = ForestUiWidgets.MakeIcon("Ico", sCard.transform,
                    GetEconArt("ico_badge_energy"), Vector2.zero, new Vector2(58f, 58f));
                _wwHdBadgeSlotLocks[s] = ForestUiWidgets.MakeIcon("Lock", sCard.transform,
                    ForestComicUiSkin.GetArt("ico_lock"), Vector2.zero, new Vector2(34f, 34f));
                _wwHdBadgeSlotPlusTxts[s] = MakeCleanDarkText("Plus", sCard.transform, "+", 36, ForestComicUiSkin.AccentPurple,
                    TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

                Image sBrk = MakeImg("SelBrk", sCard.transform, ForestComicUiSkin.SelectionCornerBrackets, Color.white,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f), false);
                _wwHdBadgeSlotBrackets[s] = sBrk.gameObject;

                _wwHdBadgeSlotTitles[s] = MakeCleanDarkText("Title", slotRt, $"{slotUnlockStars[s]}星解锁", 14, ForestComicUiSkin.InkMuted,
                    TextAnchor.LowerCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(0f, 24f));
            }

            // 下半部 6 种战地徽章滚动装配列表 (viewport 620x364)
            RectTransform listContent = MakeVerticalScrollArea("BadgeListScroll", page,
                new Vector2(0f, -68f), new Vector2(620f, 364f), 6 * 96f + 12f);

            for (int i = 0; i < 6; i++)
            {
                TacticalBadgeId bId = (TacticalBadgeId)(i + 1);
                Image row = MakeImg("BadgeRow_" + i, listContent, ForestComicUiSkin.WwSubPanel, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -6f - i * 96f), new Vector2(604f, 88f), false);

                _wwHdBadgeRowCards[i] = MakeImg("Clip", row.transform, ForestComicUiSkin.ClippedCardPurple, Color.white,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(68f, 68f), false);
                _wwHdBadgeRowIcons[i] = ForestUiWidgets.MakeIcon("Ico", _wwHdBadgeRowCards[i].transform,
                    GetEconArt(ForestSurvivalSim.GetBadgeIconKey(bId)), Vector2.zero, new Vector2(52f, 52f));

                _wwHdBadgeRowNames[i] = MakeCleanDarkText("Name", row.transform, ForestSurvivalSim.GetBadgeName(bId), 18,
                    ForestComicUiSkin.InkDark, TextAnchor.UpperLeft,
                    new Vector2(0f, 1f), new Vector2(0.65f, 1f), new Vector2(0f, 1f), new Vector2(92f, -12f), new Vector2(0f, 26f));

                _wwHdBadgeRowCounts[i] = MakeCleanDarkText("Cnt", row.transform, "空闲 0 / 拥有 0", 14,
                    ForestComicUiSkin.AccentPurple, TextAnchor.UpperRight,
                    new Vector2(0.45f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-142f, -14f), new Vector2(0f, 24f));

                _wwHdBadgeRowDescs[i] = MakeCleanDarkText("Desc", row.transform, ForestSurvivalSim.GetBadgeDesc(bId), 14,
                    ForestComicUiSkin.InkMuted, TextAnchor.LowerLeft,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(92f, 10f), new Vector2(-228f, 38f));

                Button actBtn = MakeBtn("BtnBadgeAct_" + i, row.transform, ForestComicUiSkin.BtnBlue, "装 配", 18,
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(118f, 52f),
                    () => OnBadgeRowActionClicked(bId), out _wwHdBadgeRowBtnLbls[i]);
                _wwHdBadgeRowBtnImgs[i] = actBtn.GetComponent<Image>();
            }
        }

        // ---------------- Sub-Tab 3: [装备] (4部位工坊战备 2×2 打造升阶网格：武器/护甲/战靴/战术镜) ----------------
        void BuildHeroDetailSubTab3_Equip(Transform cardParent)
        {
            RectTransform page = ForestUiWidgets.MakeRect("SubPage3_Equip", cardParent,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -94f), new Vector2(636f, 512f));
            _wwHdSubPages[3] = page.gameObject;

            string[] slotHeaders = { "武器槽 (锻造坊)", "护甲槽 (裁缝坊·换装)", "战靴槽 (裁缝坊)", "战术镜槽 (锻造电子)" };
            string[] defaultGearKeys = { "ico_gear_weapon", "ico_gear_armor", "ico_gear_boots", "ico_gear_goggles" };

            for (int i = 0; i < 4; i++)
            {
                HeroEquipSlot slot = (HeroEquipSlot)i;
                int col = i % 2;
                int row = i / 2;
                float gx = col == 0 ? -156f : 156f;
                float gy = -8f - row * 246f;

                Image gCard = MakeImg("GearSlotCard_" + i, page, ForestComicUiSkin.WwSubPanel, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(gx, gy), new Vector2(304f, 238f), false);
                _wwHdGearSlotCards[i] = gCard;

                _wwHdGearSlotHeaders[i] = MakeCleanDarkText("Hdr", gCard.transform, slotHeaders[i], 15, ForestComicUiSkin.InkMuted,
                    TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0.72f, 1f), new Vector2(0f, 1f), new Vector2(12f, -8f), new Vector2(0f, 22f));

                _wwHdGearTierTags[i] = MakeCleanDarkText("Tier", gCard.transform, "未穿戴", 14, ForestComicUiSkin.AccentPurple,
                    TextAnchor.UpperRight, new Vector2(0.65f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -8f), new Vector2(0f, 22f));

                _wwHdGearClipTiles[i] = MakeImg("ClipTile", gCard.transform, ForestComicUiSkin.ClippedCardGrey, Color.white,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -34f), new Vector2(76f, 76f), false);
                _wwHdGearIcons[i] = ForestUiWidgets.MakeIcon("Ico", _wwHdGearClipTiles[i].transform,
                    GetEconArt(defaultGearKeys[i], ForestComicUiSkin.ResIcon(SurvRes.PlateArmor)), Vector2.zero, new Vector2(56f, 56f));

                _wwHdGearNames[i] = MakeCleanDarkText("Name", gCard.transform, "铆接战备武器", 16, ForestComicUiSkin.InkDark,
                    TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(98f, -36f), new Vector2(-108f, 40f));

                _wwHdGearStats[i] = MakeCleanDarkText("Stat", gCard.transform, "攻击+15 · 技强+12", 14, ForestComicUiSkin.AccentGreen,
                    TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(98f, -78f), new Vector2(-108f, 36f));

                Image costStrip = MakeImg("CostStrip", gCard.transform, ForestComicUiSkin.WwLightSheet, Color.white,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(280f, 54f), false);
                _wwHdGearCosts[i] = MakeCleanDarkText("CostTxt", costStrip.transform, "", 13, ForestComicUiSkin.InkMuted,
                    TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-12f, -4f));
                _wwHdGearCosts[i].supportRichText = true;

                Button actBtn = MakeBtn("BtnGearAct_" + i, gCard.transform, ForestComicUiSkin.BtnBlue, "打造穿戴", 18,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(268f, 46f),
                    () => OnHeroGearSlotActionClicked(slot), out _wwHdGearBtnLbls[i]);
                _wwHdGearBtnImgs[i] = actBtn.GetComponent<Image>();
            }
        }

        // =====================================================================
        // Screen 4: 【道具仓库】全屏 4 列切角库存（[加工品] [消耗品] [碎片] [装备]）+ 自选箱弹窗
        // =====================================================================
        void BuildBagInventoryModal()
        {
            if (_bagInventoryModal != null) return;
            RectTransform rootRt = ForestUiWidgets.Stretch("BagInventoryFullModal", _canvasRt);
            var backdrop = rootRt.gameObject.AddComponent<Image>();
            backdrop.color = new Color32(34, 41, 70, 255);
            backdrop.raycastTarget = true;
            _bagInventoryModal = rootRt.gameObject;

            // 顶部深靛蓝标题栏（严格 x <= 390，右侧完全净空避让微信胶囊！）
            Image topBar = MakeImg("BagHeaderShell", rootRt, ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 110f), false);

            MakeText("BagTitle", topBar.transform, "道具", 34, ForestComicUiSkin.InkHeaderLight, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -14f), new Vector2(140f, 44f), outline: true);

            Image capPill = MakeImg("BagCapPill", topBar.transform, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -62f), new Vector2(366f, 36f), true);
            capPill.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                CampNode wh = _sim.Find("warehouse");
                if (wh != null) OpenBuildConfirmCard(wh);
            });
            _wwBagCapHeaderTxt = MakeText("CapTxt", capPill.transform, "仓库容量 10/80", 15, ForestComicUiSkin.AccentGold, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-22f, 0f), outline: true);

            // 主体断线双层漫画框 (668x988, ForestComicUiSkin.WwBrokenFrame)
            Image brokenFrame = MakeImg("BagBrokenSheet", rootRt, ForestComicUiSkin.WwBrokenFrame, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(668f, 988f), true);

            _wwBagGridContentRt = MakeVerticalScrollArea("BagGridScroll", brokenFrame.transform,
                new Vector2(0f, 94f), new Vector2(620f, 756f), 756f);

            MakeImg("InspectorDivider", brokenFrame.transform, null, new Color32(86, 98, 204, 255),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(604f, 3f), false);

            RectTransform inspectorRt = ForestUiWidgets.MakeRect("BagItemInspector", brokenFrame.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(612f, 168f));

            _wwBagInspCardImg = MakeImg("SelectedClipCard", inspectorRt, ForestComicUiSkin.ClippedCardBlue, Color.white,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(118f, 118f), false);
            _wwBagInspIconImg = ForestUiWidgets.MakeIcon("Ico", _wwBagInspCardImg.transform,
                ForestComicUiSkin.ResIcon(SurvRes.Plank), new Vector2(0f, 4f), new Vector2(72f, 72f));
            _wwBagInspCountTxt = MakeText("Cnt", _wwBagInspCardImg.transform, "0", 21, Color.white, TextAnchor.LowerRight,
                Vector2.zero, Vector2.one, new Vector2(1f, 0f), Vector2.zero, new Vector2(-18f, -14f), outline: true);

            _wwBagInspNameTxt = MakeCleanDarkText("ItemName", inspectorRt, "厚木板", 24, ForestComicUiSkin.InkDark, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(146f, -22f), new Vector2(290f, 34f));

            _wwBagInspStockTagTxt = MakeCleanDarkText("StockTag", inspectorRt, "储量 0/80", 16, ForestComicUiSkin.AccentPurple, TextAnchor.UpperRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-176f, -26f), new Vector2(160f, 26f));

            _wwBagInspDescTxt = MakeCleanDarkText("ItemDesc", inspectorRt, "", 16, ForestComicUiSkin.InkMuted, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(146f, -58f), new Vector2(304f, 96f));
            _wwBagInspDescTxt.lineSpacing = 1.15f;
            _wwBagInspDescTxt.supportRichText = true;

            Button bagActBtn = MakeBtn("BtnBagItemAction", inspectorRt, ForestComicUiSkin.BtnBlue, "前往获取", 21,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, -12f), new Vector2(148f, 60f),
                () => _wwBagActionCallback?.Invoke(), out _wwBagActionBtnLbl);
            _wwBagActionBtnImg = bagActBtn.GetComponent<Image>();

            // 底部 122px 导航栏：左下红弯箭头 + 右下石质托盘 4 新分类页签 ([加工品] [消耗品] [碎片] [装备])
            RectTransform bagBotBar = ForestUiWidgets.MakeRect("BagBottomNav", rootRt,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 122f));

            MakeWwBackRedBtn(bagBotBar, CloseAllModals, new Vector2(16f, 18f), new Vector2(104f, 78f));

            Image bagStoneTray = MakeImg("BagStoneTray", bagBotBar, ForestComicUiSkin.WwStoneTray, Color.white,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-14f, 10f), new Vector2(496f, 104f), true);

            string[] bagTabNames = { "加工品", "消耗品", "碎片", "装备" };
            Sprite[] bagTabIcons =
            {
                ForestComicUiSkin.ResIcon(SurvRes.Plank),
                GetEconResIcon(SurvRes.ChoiceResBox),
                GetEconResIcon(SurvRes.ShardUnivEpic),
                GetEconArt("ico_gear_armor", ForestComicUiSkin.ResIcon(SurvRes.PlateArmor)),
            };
            for (int t = 0; t < 4; t++)
            {
                int tabIdx = t;
                float tx = -178.5f + t * 119f;
                MakeWwStoneSquareTab(
                    "BagCatTab_" + t, bagStoneTray.transform, new Vector2(tx, 0f), new Vector2(108f, 86f),
                    bagTabIcons[t], bagTabNames[t], 17,
                    () => SwitchBagCategoryTab(tabIdx),
                    out _wwBagBottomTabImgs[t],
                    out _wwBagBottomTabLbls[t],
                    out _wwBagBottomTabOutlines[t],
                    out _wwBagBottomTabDots[t]);
            }

            BuildChoiceResBoxExchangePopup(rootRt);

            _bagInventoryModal.SetActive(false);
        }

        void BuildChoiceResBoxExchangePopup(Transform bagRoot)
        {
            RectTransform maskRt = ForestUiWidgets.Stretch("ChoiceResBoxPopup", bagRoot);
            var maskImg = maskRt.gameObject.AddComponent<Image>();
            maskImg.color = new Color32(14, 18, 40, 188);
            maskImg.raycastTarget = true;
            maskRt.gameObject.AddComponent<Button>().onClick.AddListener(() => _wwChoiceBoxPopup.SetActive(false));
            _wwChoiceBoxPopup = maskRt.gameObject;

            Image card = MakeImg("ChoiceCard", maskRt, ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(616f, 456f), true);

            MakeText("Hdr", card.transform, "原料自选补给箱", 27, ForestComicUiSkin.InkHeaderLight, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(0f, 42f), outline: true);

            MakeBtn("BtnCloseChoice", card.transform, ForestComicUiSkin.BtnRed, "×", 24,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -12f), new Vector2(42f, 42f),
                () => _wwChoiceBoxPopup.SetActive(false), out _);

            Image body = MakeImg("LightBody", card.transform, ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(576f, 286f), false);

            _wwChoiceSubTxt = MakeCleanDarkText("Sub", body.transform, "请选择要兑换的原料（每箱可领 5 份，当前拥有 1 箱）：",
                18, ForestComicUiSkin.InkDark, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(-24f, 30f));

            for (int i = 0; i < ChoiceBoxCandidateRes.Length; i++)
            {
                SurvRes cRes = ChoiceBoxCandidateRes[i];
                float cx = -216f + i * 108f;

                Image tile = MakeImg("ChoiceOpt_" + i, body.transform, ForestComicUiSkin.ClippedCardGreen, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(cx, -58f), new Vector2(98f, 142f), true);
                var tBtn = tile.gameObject.AddComponent<Button>();
                tBtn.targetGraphic = tile;
                tBtn.onClick.AddListener(() =>
                {
                    _wwChoiceSelectedRes = cRes;
                    RefreshChoiceResBoxPopup();
                });

                ForestUiWidgets.MakeIcon("Ico", tile.transform, ForestComicUiSkin.ResIcon(cRes),
                    new Vector2(0f, 22f), new Vector2(58f, 58f));
                MakeText("Amt", tile.transform, $"{ForestSurvivalSim.ResName(cRes)}\n×5", 14, Color.white, TextAnchor.LowerCenter,
                    new Vector2(0f, 0f), new Vector2(1f, 0.55f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), Vector2.zero, outline: true);

                Image ownPill = MakeImg("OwnPill", tile.transform, ForestComicUiSkin.DarkPill, Color.white,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 5f), new Vector2(86f, 20f), false);
                _wwChoiceOwnTxts[i] = MakeText("Own", ownPill.transform, "存:0", 12, ForestComicUiSkin.AccentGold, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

                Image brk = MakeImg("SelBrk", tile.transform, ForestComicUiSkin.SelectionCornerBrackets, Color.white,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f), false);
                _wwChoiceBrackets[i] = brk.gameObject;

                Image chk = ForestUiWidgets.MakeIcon("Chk", tile.transform, ForestComicUiSkin.GetUiArt("ww_check_green"),
                    new Vector2(30f, 48f), new Vector2(32f, 32f));
                _wwChoiceChecks[i] = chk.gameObject;
            }

            MakeCleanDarkText("Hint", body.transform, "提示：自选原料直接补充营地库存，助力快速交付订单或打造装备！",
                15, ForestComicUiSkin.InkMuted, TextAnchor.LowerCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(-24f, 26f));

            MakeBtn("BtnConfirmChoiceClaim", card.transform, ForestComicUiSkin.BtnGreen, "确认兑换 (×5)", 23,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(248f, 66f),
                OnConfirmChoiceResBoxClaim, out _);

            _wwChoiceBoxPopup.SetActive(false);
        }

        // =====================================================================
        // 公开入口与交互路由（含自动化验收接口 OpenHeroSkillDetailPopup）
        // =====================================================================
        void ShowHeroSpotlight(HeroSpec h)
        {
            if (h == null) return;
            if (_world != null && _world.InWaveDefense) return;
            CloseAllModals();
            ApplyHeroFullBodySprite(_heroSpotBust, h);
            _heroSpotBust.transform.parent.GetComponent<Image>().sprite = ForestComicUiSkin.GetClippedCard(h.rarity);
            _heroSpotNameTxt.text = h.name;
            _heroSpotRoleTxt.text = $"{ForestSurvivalSim.GetFactionName(h.faction)} · {h.roleTitle} · {h.RarityTag}";
            _heroSpotBuffTxt.text = $"{h.buffDesc}（绝技：{h.skillName}）";
            _heroSpotlightModal.SetActive(true);
        }

        public void OpenHeroAssignModal() => OpenHeroModal(0);

        public void OpenHeroModal(int tabIndex)
        {
            CloseAllModals();
            _heroActiveTab = tabIndex <= 0 ? 0 : 1;
            if (_wwDelPickerModal != null) _wwDelPickerModal.SetActive(false);
            RefreshHeroAssignModal();
            _heroAssignModal.SetActive(true);
        }

        void SwitchHeroModalTab(int tabIndex)
        {
            _heroActiveTab = tabIndex <= 0 ? 0 : 1;
            if (_wwDelPickerModal != null) _wwDelPickerModal.SetActive(false);
            RefreshHeroAssignModal();
        }

        public void OpenHeroDetailModal(int heroIdx, int detailTab = 0)
        {
            if (_sim.Heroes.Count == 0) return;
            CloseAllModals();
            _wwHdHeroIdx = Mathf.Clamp(heroIdx, 0, _sim.Heroes.Count - 1);
            _wwHdTabIdx = Mathf.Clamp(detailTab, 0, 3);
            _wwHdSelectedSkillIdx = 0;
            _wwHdSelectedBadgeSlot = 0;
            if (_wwSkillDetailPopup != null) _wwSkillDetailPopup.SetActive(false);
            if (_wwHdResetConfirmPopup != null) _wwHdResetConfirmPopup.SetActive(false);
            RefreshHeroDetailModal();
            _heroDetailModal.SetActive(true);
        }

        public void OpenHeroSkillDetailPopup(int heroIdx, int skillSlot)
        {
            if (_sim.Heroes.Count == 0) return;
            int clampedHero = Mathf.Clamp(heroIdx, 0, _sim.Heroes.Count - 1);
            int clampedSlot = Mathf.Clamp(skillSlot, 0, 3);

            if (_heroDetailModal == null || !_heroDetailModal.activeSelf || _wwHdHeroIdx != clampedHero || _wwHdTabIdx != 0)
                OpenHeroDetailModal(clampedHero, 0);

            _wwHdSelectedSkillIdx = clampedSlot;
            _wwSkPopupSlotIdx = clampedSlot;
            RefreshHeroDetailSkillStrip();
            RefreshHeroSkillDetailPopup();
            _wwSkillDetailPopup.transform.SetAsLastSibling();
            _wwSkillDetailPopup.SetActive(true);
        }

        public void OpenBagInventoryModal() => OpenBagInventoryModal(0);

        public void OpenBagInventoryModal(int bagTab)
        {
            CloseAllModals();
            _wwBagTabIdx = Mathf.Clamp(bagTab, 0, 3);
            if (_wwChoiceBoxPopup != null) _wwChoiceBoxPopup.SetActive(false);
            _wwBagSelectedKey = null;
            RefreshBagInventoryModal();
            _bagInventoryModal.SetActive(true);
        }

        void SwitchBagCategoryTab(int bagTab)
        {
            _wwBagTabIdx = Mathf.Clamp(bagTab, 0, 3);
            _wwBagSelectedKey = null;
            RefreshBagInventoryModal();
        }

        // =====================================================================
        // Screen 1 & Screen 3 逻辑与实时刷新
        // =====================================================================
        static string GetDepartmentDisplayTitle(DelegationPostKind kind)
        {
            switch (kind)
            {
                case DelegationPostKind.WorkshopSpeed: return "作坊生产提速";
                case DelegationPostKind.BuildSpeed: return "建筑建造加速";
                case DelegationPostKind.FarmYield: return "农牧采收增产";
                case DelegationPostKind.DefensePower: return "城防哨塔火力";
                case DelegationPostKind.TruckArmor: return "皮卡远征护甲";
                case DelegationPostKind.CaravanBonus: return "行商贸易增益";
                default: return "营地驻守增益";
            }
        }

        static Sprite GetDepartmentVignetteSprite(DelegationPostKind kind)
        {
            switch (kind)
            {
                case DelegationPostKind.WorkshopSpeed:
                    return ForestComicUiSkin.GetUiArt("ww_dept_workshop", ForestComicUiSkin.GetArt("sawmill_shed"));
                case DelegationPostKind.BuildSpeed:
                    return ForestComicUiSkin.GetUiArt("ww_dept_build", ForestComicUiSkin.GetArt("ico_hammer"));
                case DelegationPostKind.FarmYield:
                    return ForestComicUiSkin.GetUiArt("ww_dept_farm", ForestComicUiSkin.GetArt("farm_2"));
                case DelegationPostKind.DefensePower:
                    return ForestComicUiSkin.GetArt("hunter_tower", ForestComicUiSkin.GetUiArt("ww_dept_build"));
                case DelegationPostKind.TruckArmor:
                    return ForestComicUiSkin.GetArt("truck_active", ForestComicUiSkin.GetUiArt("ww_dept_trade"));
                default:
                    return ForestComicUiSkin.GetUiArt("ww_dept_trade", ForestComicUiSkin.GetArt("radio_post"));
            }
        }

        static string GetHeroRecruitConditionText(int heroIdx)
        {
            switch (heroIdx)
            {
                case 2: return "第2关首通/电台";
                case 3: return "第5关首通/电台";
                case 4: return "第3关首通/电台";
                case 5: return "第12关首通/电台";
                case 6: return "第8关首通/电台";
                case 7: return "第15关首通/电台";
                case 8: return "第20关首通/电台";
                default: return "初始入队";
            }
        }

        bool MatchesRosterFilter(int heroIdx, int filterIdx)
        {
            if (filterIdx <= 0 || heroIdx < 0 || heroIdx >= _sim.Heroes.Count) return true;
            HeroFaction target = (HeroFaction)(filterIdx - 1);
            return _sim.Heroes[heroIdx].faction == target;
        }

        internal bool IsHeroUpgradeableNow(int heroIdx)
        {
            if (heroIdx < 0 || heroIdx >= _sim.Heroes.Count) return false;
            HeroSpec h = _sim.Heroes[heroIdx];
            if (h == null) return false;
            if (!h.unlocked)
                return h.shards >= 10;

            int expNeed = _sim.GetHeroLevelUpExpCost(h.level);
            if (h.level < _sim.MaxHeroLevelCap && _sim.Get(SurvRes.HeroExp) >= expNeed)
                return true;

            if (h.star < 5)
            {
                int petalNeed = _sim.GetNextPetalShardCost(heroIdx);
                int totalShards = h.shards + (_sim.AutoExchangeUniversalShards ? _sim.GetAvailableUniversalShardsFor(heroIdx) : 0);
                if (petalNeed > 0 && totalShards >= petalNeed)
                    return true;
            }
            return false;
        }

        void OnRosterPortraitCardClicked(int heroIdx)
        {
            if (heroIdx < 0 || heroIdx >= _sim.Heroes.Count) return;
            HeroSpec h = _sim.Heroes[heroIdx];
            if (!h.unlocked && h.shards >= 10)
            {
                if (_sim.TryRecruitHeroWithShards(heroIdx, out string msg))
                {
                    if (!string.IsNullOrEmpty(msg)) SpawnScreenCenterBanner(msg);
                    OpenHeroDetailModal(heroIdx, 0);
                    return;
                }
            }
            OpenHeroDetailModal(heroIdx, 0);
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

        void FlashDelegationBanner(string msg)
        {
            if (_wwDelUpdateBannerGo == null) return;
            _wwDelUpdateBannerTxt.text = msg;
            _wwDelUpdateBannerCg.alpha = 1f;
            _wwDelUpdateBannerGo.SetActive(true);
            if (Application.isPlaying && isActiveAndEnabled)
            {
                if (_wwDelBannerCoroutine != null) StopCoroutine(_wwDelBannerCoroutine);
                _wwDelBannerCoroutine = StartCoroutine(CoFadeDelegationBanner());
            }
        }

        IEnumerator CoFadeDelegationBanner()
        {
            yield return new WaitForSecondsRealtime(1.8f);
            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.unscaledDeltaTime;
                if (_wwDelUpdateBannerCg != null)
                    _wwDelUpdateBannerCg.alpha = 1f - Mathf.Clamp01(t / 0.45f);
                yield return null;
            }
            if (_wwDelUpdateBannerGo != null) _wwDelUpdateBannerGo.SetActive(false);
            _wwDelBannerCoroutine = null;
        }

        void OnDelegationSlotClicked(int postIdx, int slotIdx)
        {
            if (postIdx < 0 || postIdx >= _sim.DelegationPosts.Count) return;
            DelegationPostState st = _sim.DelegationPosts[postIdx];
            if (!st.IsSlotUnlocked(slotIdx, _sim.CampLevel))
            {
                int req = slotIdx < st.slotUnlockLevels.Length ? st.slotUnlockLevels[slotIdx] : 99;
                FlashDelegationBanner($"该驻守槽位需玩家等级达到 {req} 级解锁！");
                return;
            }
            OpenDelegationPicker(postIdx, slotIdx);
        }

        void OpenDelegationPicker(int postIdx, int slotIdx)
        {
            _wwPickerPostIdx = postIdx;
            _wwPickerSlotIdx = slotIdx;
            DelegationPostState st = _sim.DelegationPosts[postIdx];
            string curId = st.assignedHeroIds[slotIdx];

            if (!string.IsNullOrEmpty(curId))
            {
                _wwPickerSelectedHeroId = curId;
            }
            else
            {
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
                HeroSpec def = bestFree ?? bestAny;
                _wwPickerSelectedHeroId = def != null ? def.id : null;
            }

            RefreshDelegationPickerVisuals();
            _wwDelPickerModal.SetActive(true);
        }

        void OnPickerHeroCardTapped(int cardSlot)
        {
            if (cardSlot < 0 || cardSlot >= _wwPickerBoundHeroIdxs.Length) return;
            int hIdx = _wwPickerBoundHeroIdxs[cardSlot];
            if (hIdx < 0 || hIdx >= _sim.Heroes.Count) return;
            HeroSpec h = _sim.Heroes[hIdx];
            if (!h.unlocked) return;

            _wwPickerSelectedHeroId = (_wwPickerSelectedHeroId == h.id) ? null : h.id;
            RefreshDelegationPickerVisuals();
        }

        void RefreshDelegationPickerVisuals()
        {
            if (_wwPickerPostIdx < 0 || _wwPickerPostIdx >= _sim.DelegationPosts.Count) return;
            DelegationPostState st = _sim.DelegationPosts[_wwPickerPostIdx];
            string postTitle = GetDepartmentDisplayTitle(st.kind);
            _wwPickerSubTitleTxt.text = $"岗位：{postTitle}（第 {_wwPickerSlotIdx + 1} 槽 · 点击头像切换绿勾 ✓）";

            var unlockedList = new List<int>();
            for (int i = 0; i < _sim.Heroes.Count; i++)
                if (_sim.Heroes[i].unlocked) unlockedList.Add(i);

            unlockedList.Sort((a, b) =>
                _sim.Heroes[b].GetDelegationScore(st.kind).CompareTo(_sim.Heroes[a].GetDelegationScore(st.kind)));

            int n = Mathf.Min(unlockedList.Count, MaxHeroUiSlots);
            float cardW = 86f;
            float spacing = 92f;
            float totalW = n > 0 ? (n * cardW + (n - 1) * (spacing - cardW)) : 0f;
            float viewW = 564f;
            float contentW = Mathf.Max(viewW, totalW + 16f);
            float startX = totalW < viewW - 16f ? (viewW - totalW) * 0.5f : 8f;
            if (_wwPickerScrollContentRt != null)
            {
                _wwPickerScrollContentRt.sizeDelta = new Vector2(contentW, 0f);
                if (contentW <= viewW + 0.5f)
                    _wwPickerScrollContentRt.anchoredPosition = Vector2.zero;
            }

            for (int c = 0; c < MaxHeroUiSlots; c++)
            {
                bool active = c < n;
                _wwPickerHeroCards[c].SetActive(active);
                _wwPickerBoundHeroIdxs[c] = active ? unlockedList[c] : -1;
                if (!active) continue;

                int hIdx = unlockedList[c];
                HeroSpec h = _sim.Heroes[hIdx];
                RectTransform rt = _wwPickerHeroCards[c].GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(startX + c * spacing, -6f);

                _wwPickerHeroFrames[c].sprite = ForestComicUiSkin.GetClippedCard(h.rarity);
                _wwPickerHeroBusts[c].sprite = ForestComicUiSkin.GetArt(h.portraitKey, ForestComicUiSkin.GetArt("bust_dachui"));
                _wwPickerHeroBusts[c].preserveAspect = true;

                bool isSpec = st.kind == h.prefPost1 || st.kind == h.prefPost2;
                _wwPickerSpecBadges[c].SetActive(isSpec);

                DelegationPostKind? curPost = _sim.GetHeroAssignedPost(h.id);
                bool onOtherSlot = curPost.HasValue && st.assignedHeroIds[_wwPickerSlotIdx] != h.id;
                _wwPickerBusyBadges[c].SetActive(onOtherSlot);

                _wwPickerHeroBonusTxts[c].text = $"{h.name}\n{FormatHeroSinglePostContribution(h, st.kind)}";

                bool checkedNow = _wwPickerSelectedHeroId == h.id;
                _wwPickerCheckIcons[c].SetActive(checkedNow);
                _wwPickerSelectBrackets[c].SetActive(checkedNow);
            }

            HeroSpec selHero = !string.IsNullOrEmpty(_wwPickerSelectedHeroId) ? _sim.FindHero(_wwPickerSelectedHeroId) : null;
            if (selHero != null)
            {
                string bonusStr = FormatHeroSinglePostContribution(selHero, st.kind);
                _wwPickerEffectTxt.text = $"委任效果：派驻【{selHero.name}】可使 {postTitle} <color=#22B24C>{bonusStr}</color>";
            }
            else
            {
                _wwPickerEffectTxt.text = "当前未勾选英雄，点击【确定】将卸下该槽位驻守伙伴";
            }
        }

        void OnConfirmDelegationPicker()
        {
            if (_wwPickerPostIdx >= 0 && _wwPickerPostIdx < _sim.DelegationPosts.Count)
            {
                DelegationPostState st = _sim.DelegationPosts[_wwPickerPostIdx];
                _sim.AssignHeroToPostSlot(st.kind, _wwPickerSlotIdx, _wwPickerSelectedHeroId);
                HeroSpec sel = !string.IsNullOrEmpty(_wwPickerSelectedHeroId) ? _sim.FindHero(_wwPickerSelectedHeroId) : null;
                string title = GetDepartmentDisplayTitle(st.kind);
                FlashDelegationBanner(sel != null
                    ? $"委任增益已更新！【{sel.name}】已入驻【{title}】({FormatHeroSinglePostContribution(sel, st.kind)})"
                    : $"委任增益已更新！【{title}】槽位已卸任");
            }
            _wwDelPickerModal.SetActive(false);
            RefreshHeroAssignModal();
        }

        void RefreshHeroAssignModal()
        {
            bool showDel = _heroActiveTab == 0;
            _haDelegationPage.SetActive(showDel);
            _haGrowthPage.SetActive(!showDel);

            if (_haHeaderTitleTxt != null)
                _haHeaderTitleTxt.text = "英雄委任";
            if (_haLeaderBannerTxt != null)
                _haLeaderBannerTxt.text = $"领主【阿澈】等级即左上角玩家等级 (Lv.{_sim.CampLevel} · 上限Lv.{_sim.MaxHeroLevelCap})";

            bool anyIdleUnlockedHero = false;
            int unlockedHeroCount = 0;
            for (int i = 0; i < _sim.Heroes.Count; i++)
            {
                if (_sim.Heroes[i].unlocked)
                {
                    unlockedHeroCount++;
                    if (!_sim.Heroes[i].assigned) anyIdleUnlockedHero = true;
                }
            }

            // ---------------- 刷新 Screen 3：英雄委任半屏部门抽屉 ----------------
            int postCount = Mathf.Min(6, _sim.DelegationPosts.Count);
            for (int p = 0; p < postCount; p++)
            {
                DelegationPostState st = _sim.DelegationPosts[p];
                bool postOpen = st.IsSlotUnlocked(0, _sim.CampLevel);
                float scoreSum = _sim.GetPostScoreSum(st.kind);
                string summary = _sim.GetPostEffectSummary(st.kind);

                _delPostEffectTxts[p].text = postOpen ? summary : "";
                _delPostEffectTxts[p].color = (postOpen && scoreSum > 0.01f)
                    ? (Color)ForestComicUiSkin.AccentGreen
                    : (Color)ForestComicUiSkin.InkMuted;

                _wwDelVignetteBadgeGos[p].SetActive(postOpen && scoreSum > 0.01f);
                if (postOpen && scoreSum > 0.01f)
                    _wwDelVignetteBadgeTxts[p].text = summary.Replace("提速 ", "").Replace("火力 ", "").Replace("载重 ", "").Replace("溢价 ", "");

                _delPostLockedHintGos[p].SetActive(!postOpen);
                if (!postOpen)
                    _delPostLockedHintTxts[p].text = $"玩家{st.slotUnlockLevels[0]}级解锁";

                for (int s = 0; s < 4; s++)
                {
                    bool hasSlot = postOpen && s < st.slotUnlockLevels.Length;
                    _delSlotBoxes[p, s].SetActive(hasSlot);
                    if (!hasSlot) continue;

                    bool slotOpen = st.IsSlotUnlocked(s, _sim.CampLevel);
                    HeroSpec h = slotOpen ? _sim.FindHero(st.assignedHeroIds[s]) : null;
                    if (!slotOpen)
                    {
                        _delSlotFrames[p, s].sprite = ForestComicUiSkin.WwLightSheet;
                        _delSlotFrames[p, s].color = new Color32(220, 225, 240, 255);
                        _delSlotBusts[p, s].gameObject.SetActive(false);
                        _delSlotLocks[p, s].gameObject.SetActive(true);
                        _wwDelSlotPlusTxts[p, s].gameObject.SetActive(false);
                        _wwDelSlotRedDots[p, s].SetActive(false);
                        _delSlotStarTags[p, s].text = "";
                        _delSlotSubTxts[p, s].text = $"{st.slotUnlockLevels[s]}级解锁";
                        _delSlotSubTxts[p, s].color = ForestComicUiSkin.InkMuted;
                    }
                    else if (h != null && h.unlocked)
                    {
                        _delSlotFrames[p, s].sprite = ForestComicUiSkin.GetClippedCard(h.rarity);
                        _delSlotFrames[p, s].color = Color.white;
                        _delSlotBusts[p, s].gameObject.SetActive(true);
                        _delSlotBusts[p, s].sprite = ForestComicUiSkin.GetArt(h.portraitKey, ForestComicUiSkin.GetArt("bust_dachui"));
                        _delSlotLocks[p, s].gameObject.SetActive(false);
                        _wwDelSlotPlusTxts[p, s].gameObject.SetActive(false);
                        _wwDelSlotRedDots[p, s].SetActive(false);
                        _delSlotStarTags[p, s].text = $"★{h.star}";
                        _delSlotSubTxts[p, s].text = $"{h.name} {FormatHeroSinglePostContribution(h, st.kind)}";
                        _delSlotSubTxts[p, s].color = ForestComicUiSkin.InkDark;
                    }
                    else
                    {
                        _delSlotFrames[p, s].sprite = ForestComicUiSkin.WwLightSheet;
                        _delSlotFrames[p, s].color = Color.white;
                        _delSlotBusts[p, s].gameObject.SetActive(false);
                        _delSlotLocks[p, s].gameObject.SetActive(false);
                        _wwDelSlotPlusTxts[p, s].gameObject.SetActive(true);
                        _wwDelSlotRedDots[p, s].SetActive(anyIdleUnlockedHero);
                        _delSlotStarTags[p, s].text = "";
                        _delSlotSubTxts[p, s].text = "点击委任";
                        _delSlotSubTxts[p, s].color = ForestComicUiSkin.AccentGreen;
                    }
                }
            }

            // ---------------- 刷新 Screen 1：英雄总览 3×2 三阵营立绘卡 ----------------
            for (int f = 0; f < 4; f++)
            {
                bool active = f == _wwRosterFilterIdx;
                ApplyStoneTabVisual(_wwRosterFilterImgs[f], _wwRosterFilterLbls[f], _wwRosterFilterOutlines[f], active);
            }

            if (_wwRosterCountTxt != null)
                _wwRosterCountTxt.text = $"已招募伙伴：{unlockedHeroCount} / {_sim.Heroes.Count}（点击卡片进入船舱养成详情）";
            if (_wwRosterSquadPowerTxt != null)
                _wwRosterSquadPowerTxt.text = $"主力战力 {_sim.CalcDeployedSquadPower()}";
            if (_haGrowthResTxt != null)
            {
                _haGrowthResTxt.text =
                    $"英雄经验 {FormatEconCount(_sim.Get(SurvRes.HeroExp))}   ·   " +
                    $"招募卡 {_sim.Get(SurvRes.RecruitTicket)}   ·   " +
                    $"技能书 {_sim.Get(SurvRes.SkillBook)}";
            }

            int tickets = _sim.Get(SurvRes.RecruitTicket);
            if (_wwRosterRadioRecruitLbl != null)
                _wwRosterRadioRecruitLbl.text = tickets > 0 ? $"广播招募 ({tickets}卡)" : "广播招募英雄";
            if (_wwRosterRadioRedDot != null)
                _wwRosterRadioRedDot.SetActive(tickets > 0);

            int visSlot = 0;
            for (int i = 0; i < MaxHeroUiSlots; i++)
            {
                bool exists = i < _sim.Heroes.Count;
                bool match = exists && MatchesRosterFilter(i, _wwRosterFilterIdx);
                if (_haRows[i] != null) _haRows[i].SetActive(match);
                if (!exists) continue;

                HeroSpec h = _sim.Heroes[i];
                ApplyHeroFullBodySprite(_haBusts[i], h);
                _haBusts[i].color = h.unlocked ? Color.white : new Color32(28, 34, 58, 236);
                _haNames[i].text = h.unlocked ? $"{h.name}  Lv.{h.level}" : $"{h.name}  待招募";

                if (!match) continue;

                int col = visSlot % 3;
                int row = visSlot / 3;
                visSlot++;
                RectTransform cardRt = _haRows[i].GetComponent<RectTransform>();
                cardRt.anchoredPosition = new Vector2(-216f + col * 216f, -6f - row * 278f);

                bool canSynthesize = !h.unlocked && h.shards >= 10;
                _haRarityFrames[i].sprite = h.unlocked
                    ? ForestComicUiSkin.GetClippedCard(h.rarity)
                    : (canSynthesize ? ForestComicUiSkin.ClippedCardGreen : ForestComicUiSkin.ClippedCardGrey);

                _wwRosterFactionCrests[i].sprite = GetFactionCrestSprite(h.faction);
                _wwRosterRarityRibbons[i].sprite = ForestComicUiSkin.RarityRibbon(h.rarity);
                _wwRosterRarityShortTxts[i].text = h.rarity == HeroRarity.Legendary ? "传"
                    : (h.rarity == HeroRarity.Epic ? "史" : "稀");

                bool inSquad = h.unlocked && _sim.FindHeroSlot(i) >= 0;
                _wwRosterDeployTags[i].SetActive(inSquad);
                _wwRosterAssignTags[i].SetActive(h.unlocked && h.assigned);
                _wwRosterRedDots[i].SetActive(IsHeroUpgradeableNow(i));

                string facName = ForestSurvivalSim.GetFactionName(h.faction);
                if (h.unlocked)
                {
                    string lit = h.star > 0 ? new string('★', Mathf.Clamp(h.star, 0, 5)) : "";
                    string dim = h.star < 5 ? new string('★', 5 - Mathf.Clamp(h.star, 0, 5)) : "";
                    string petalTag = (h.star < 5 && h.starPetals > 0) ? $" <color=#FF9838>{h.starPetals}/5瓣</color>" : "";
                    _wwRosterStarsTxts[i].gameObject.SetActive(true);
                    _wwRosterStarsTxts[i].text = $"<color=#FFD44C>{lit}</color><color=#5B658A>{dim}</color>{petalTag}";
                    _wwRosterLockPills[i].SetActive(false);
                    _haStarBadges[i].text = $"{facName} · 战力{_sim.CalcHeroCombatPower(i)}";
                }
                else
                {
                    _wwRosterStarsTxts[i].gameObject.SetActive(false);
                    _wwRosterLockPills[i].SetActive(true);
                    if (canSynthesize)
                    {
                        _wwRosterLockPillImgs[i].sprite = ForestComicUiSkin.BtnGreen;
                        _wwRosterLockTxts[i].text = $"可合成招募! ({h.shards}/10)";
                        _wwRosterLockTxts[i].color = Color.white;
                        _haStarBadges[i].text = $"{facName} · 点击合成入队";
                    }
                    else
                    {
                        _wwRosterLockPillImgs[i].sprite = ForestComicUiSkin.DarkPill;
                        _wwRosterLockTxts[i].text = $"碎片 {h.shards}/10 · {GetHeroRecruitConditionText(i)}";
                        _wwRosterLockTxts[i].color = ForestComicUiSkin.AccentGold;
                        _haStarBadges[i].text = $"{facName} · {_sim.GetHeroRoleName(i)}";
                    }
                }
            }

            if (_wwRosterScrollContentRt != null)
            {
                int visRows = Mathf.Max(1, Mathf.CeilToInt(visSlot / 3f));
                float reqH = Mathf.Max(842f, 12f + visRows * 278f);
                _wwRosterScrollContentRt.sizeDelta = new Vector2(0f, reqH);
                if (reqH <= 846f)
                    _wwRosterScrollContentRt.anchoredPosition = Vector2.zero;
            }
        }

        // =====================================================================
        // Screen 2 逻辑与实时刷新：【英雄独立舱室养成详情页】
        // =====================================================================
        void CycleDetailHero(int delta)
        {
            if (_sim.Heroes.Count == 0) return;
            _wwHdHeroIdx = (_wwHdHeroIdx + delta + _sim.Heroes.Count) % _sim.Heroes.Count;
            _wwHdSelectedSkillIdx = 0;
            _wwHdSelectedBadgeSlot = 0;
            if (_wwSkillDetailPopup != null) _wwSkillDetailPopup.SetActive(false);
            if (_wwHdResetConfirmPopup != null) _wwHdResetConfirmPopup.SetActive(false);
            RefreshHeroDetailModal();
            _wwHdCabinBreather?.TriggerHop();
        }

        static string GetHeroUltArtKey(int heroIdx, HeroSpec h)
        {
            string id = h != null ? h.id : "";
            switch (id)
            {
                case "dachui": return "ww_skill_shield";
                case "xiaotang": return "ww_skill_fire";
                case "leimeng": return "ww_skill_cleave";
                case "linyan": return "ww_skill_arrow";
                case "tieshu": return "ww_skill_shotgun";
                case "laochentou": return "ww_skill_heal";
                case "qinghe": return "ww_skill_arrow";
                case "asa": return "ww_skill_cleave";
                case "zhoudr": return "ww_skill_fire";
            }
            switch (heroIdx)
            {
                case 0: return "ww_skill_shield";
                case 1: return "ww_skill_fire";
                case 2: return "ww_skill_cleave";
                case 3: return "ww_skill_arrow";
                case 4: return "ww_skill_shotgun";
                case 6: return "ww_skill_arrow";
                case 7: return "ww_skill_cleave";
                case 8: return "ww_skill_fire";
                default: return "ww_skill_heal";
            }
        }

        void OnHeroResetLevelBtnClicked()
        {
            if (_wwHdHeroIdx < 0 || _wwHdHeroIdx >= _sim.Heroes.Count) return;
            HeroSpec h = _sim.Heroes[_wwHdHeroIdx];
            if (!h.unlocked || (h.level <= 1 && h.spentSkillBooks <= 0))
            {
                SpawnScreenCenterBanner("当前英雄处于 1 级初始状态，无需重置");
                return;
            }
            _wwHdResetConfirmDescTxt.text =
                $"是否将【{h.name}】免费重置回 <b>1级</b>？\n" +
                $"100% 全额返还：<color=#22B24C>英雄经验 +{FormatEconCount(h.spentHeroExp)}</color>" +
                (h.spentSkillBooks > 0 ? $"  ·  <color=#22B24C>技能书 +{h.spentSkillBooks}</color>" : "") +
                "\n（星级与星瓣完整保留，已装配徽章将自动卸回背包）";
            _wwHdResetConfirmPopup.transform.SetAsLastSibling();
            _wwHdResetConfirmPopup.SetActive(true);
        }

        void OnConfirmHeroResetLevel()
        {
            _wwHdResetConfirmPopup.SetActive(false);
            if (_sim.TryResetHeroLevel(_wwHdHeroIdx, out string msg))
            {
                RefreshHeroDetailModal();
                SpawnScreenCenterBanner(msg);
            }
            else if (!string.IsNullOrEmpty(msg))
            {
                SpawnScreenCenterBanner(msg);
            }
        }

        void OnDetailLevelUpClicked()
        {
            if (_wwHdHeroIdx < 0 || _wwHdHeroIdx >= _sim.Heroes.Count) return;
            HeroSpec h = _sim.Heroes[_wwHdHeroIdx];
            if (!h.unlocked)
            {
                if (h.shards >= 10 && _sim.TryRecruitHeroWithShards(_wwHdHeroIdx, out string recMsg))
                {
                    RefreshHeroDetailModal();
                    SpawnScreenCenterBanner(recMsg);
                }
                else
                {
                    OpenRadioRecruitModal();
                }
                return;
            }

            if (h.level >= _sim.MaxHeroLevelCap)
            {
                SpawnScreenCenterBanner($"已达当前英雄等级上限 Lv.{_sim.MaxHeroLevelCap}！回营地提升玩家领主等级（每+1级上限+5）！");
                return;
            }

            int expNeed = _sim.GetHeroLevelUpExpCost(h.level);
            if (_sim.Get(SurvRes.HeroExp) < expNeed)
            {
                SpawnScreenCenterBanner("英雄经验不足！前往【山口哨卡闯关】或领取【巡逻挂机箱】获取海量英雄经验！");
                OpenStagePosterModal();
                return;
            }

            if (_sim.TryLevelUpHeroWithExp(_wwHdHeroIdx, out string msg))
                RefreshHeroDetailModal();
            if (!string.IsNullOrEmpty(msg))
                SpawnScreenCenterBanner(msg);
        }

        void OnUniversalShardExchangeClicked()
        {
            if (_wwHdHeroIdx < 0 || _wwHdHeroIdx >= _sim.Heroes.Count) return;
            HeroSpec h = _sim.Heroes[_wwHdHeroIdx];
            int univOwn = _sim.GetAvailableUniversalShardsFor(_wwHdHeroIdx);
            if (univOwn <= 0)
            {
                SurvRes uRes = _sim.GetUniversalShardResFor(h.rarity);
                SpawnScreenCenterBanner($"暂无【{ForestSurvivalSim.ResName(uRes)}】！可通过首领关首通、广播招募或行商获取！");
                return;
            }
            int petalCost = h.unlocked ? _sim.GetNextPetalShardCost(_wwHdHeroIdx) : 10;
            int need = Mathf.Max(1, petalCost - h.shards);
            int toExchange = Mathf.Clamp(need, 1, univOwn);
            if (_sim.TryExchangeUniversalShards(_wwHdHeroIdx, toExchange, out string msg))
            {
                RefreshHeroDetailModal();
                SpawnScreenCenterBanner(msg);
            }
            else if (!string.IsNullOrEmpty(msg))
            {
                SpawnScreenCenterBanner(msg);
            }
        }

        void OnDetailStarUpClicked()
        {
            if (_wwHdHeroIdx < 0 || _wwHdHeroIdx >= _sim.Heroes.Count) return;
            HeroSpec h = _sim.Heroes[_wwHdHeroIdx];
            if (!h.unlocked)
            {
                if (h.shards >= 10 && _sim.TryRecruitHeroWithShards(_wwHdHeroIdx, out string rMsg))
                {
                    RefreshHeroDetailModal();
                    SpawnScreenCenterBanner(rMsg);
                }
                else
                {
                    OpenRadioRecruitModal();
                }
                return;
            }

            if (_sim.TryAdvanceHeroStarPetal(_wwHdHeroIdx, _sim.AutoExchangeUniversalShards, out bool crossedFullStar, out string msg))
            {
                RefreshHeroDetailModal();
                SpawnScreenCenterBanner(crossedFullStar ? $"★ 整星质变突破！{msg}" : msg);
            }
            else
            {
                SpawnScreenCenterBanner(!string.IsNullOrEmpty(msg) ? msg : "专属碎片不足，可前往【广播招募】获取碎片！");
            }
        }

        void OnUpgradeSkillInPopupClicked()
        {
            if (_wwHdHeroIdx < 0 || _wwHdHeroIdx >= _sim.Heroes.Count) return;
            if (_sim.TryUpgradeHeroSkill(_wwHdHeroIdx, _wwSkPopupSlotIdx, out string msg))
            {
                RefreshHeroDetailModal();
                RefreshHeroSkillDetailPopup();
                SpawnScreenCenterBanner(msg);
            }
            else if (!string.IsNullOrEmpty(msg))
            {
                SpawnScreenCenterBanner(msg);
            }
        }

        void OnBadgeSlotClicked(int slotIdx)
        {
            if (_wwHdHeroIdx < 0 || _wwHdHeroIdx >= _sim.Heroes.Count) return;
            HeroSpec h = _sim.Heroes[_wwHdHeroIdx];
            int[] reqStars = { 1, 3, 5 };
            if (!_sim.IsBadgeSlotUnlocked(h, slotIdx))
            {
                SpawnScreenCenterBanner($"第 {slotIdx + 1} 徽章槽需英雄升至 ★{reqStars[slotIdx]} 解锁！");
                return;
            }
            _wwHdSelectedBadgeSlot = slotIdx;
            RefreshHeroDetailModal();
        }

        void OnBadgeRowActionClicked(TacticalBadgeId badge)
        {
            if (_wwHdHeroIdx < 0 || _wwHdHeroIdx >= _sim.Heroes.Count) return;
            HeroSpec h = _sim.Heroes[_wwHdHeroIdx];
            if (!h.unlocked)
            {
                SpawnScreenCenterBanner("请先招募解锁该英雄后再装配战地徽章！");
                return;
            }

            // 若当前英雄已装备该徽章，则一键卸下
            for (int s = 0; s < 3; s++)
            {
                if (h.equippedBadges[s] == badge)
                {
                    if (_sim.TryEquipHeroBadge(_wwHdHeroIdx, s, TacticalBadgeId.None, out string uMsg))
                    {
                        RefreshHeroDetailModal();
                        SpawnScreenCenterBanner(uMsg);
                    }
                    return;
                }
            }

            if (_sim.GetOwnedBadgeCount(badge) <= 0)
            {
                OpenStagePosterModal();
                SpawnScreenCenterBanner("通关首领关卡或搜刮丧尸城高危空投可缴获稀有战地徽章！");
                return;
            }

            // 优先装入当前选中的已解锁槽，或自动寻找第一个空闲已解锁槽
            int targetSlot = _wwHdSelectedBadgeSlot;
            if (!_sim.IsBadgeSlotUnlocked(h, targetSlot))
            {
                targetSlot = -1;
                for (int s = 0; s < 3; s++)
                {
                    if (_sim.IsBadgeSlotUnlocked(h, s)) { targetSlot = s; break; }
                }
            }
            else if (h.equippedBadges[targetSlot] != TacticalBadgeId.None)
            {
                for (int s = 0; s < 3; s++)
                {
                    if (_sim.IsBadgeSlotUnlocked(h, s) && h.equippedBadges[s] == TacticalBadgeId.None)
                    {
                        targetSlot = s;
                        break;
                    }
                }
            }

            if (targetSlot < 0)
            {
                SpawnScreenCenterBanner("需先将英雄升至 ★1 解锁第 1 个战地徽章槽！");
                _wwHdTabIdx = 1;
                RefreshHeroDetailModal();
                return;
            }

            if (_sim.TryEquipHeroBadge(_wwHdHeroIdx, targetSlot, badge, out string msg))
            {
                _wwHdSelectedBadgeSlot = targetSlot;
                RefreshHeroDetailModal();
                SpawnScreenCenterBanner(msg);
            }
            else if (!string.IsNullOrEmpty(msg))
            {
                SpawnScreenCenterBanner(msg);
            }
        }

        void OnHeroGearSlotActionClicked(HeroEquipSlot slot)
        {
            if (_wwHdHeroIdx < 0 || _wwHdHeroIdx >= _sim.Heroes.Count) return;
            HeroSpec h = _sim.Heroes[_wwHdHeroIdx];
            if (!h.unlocked)
            {
                SpawnScreenCenterBanner("请先招募解锁该伙伴后再打造穿戴战备！");
                return;
            }

            _sim.GetHeroEquipSpec(_wwHdHeroIdx, slot, out int curTier, out _, out _, out _, out _, out _,
                out CostPair[] nextCosts, out string buildingReqHint, out bool canCraftNow);
            if (curTier >= 4)
            {
                SpawnScreenCenterBanner("该部位战备已升至最高传世橙品！");
                return;
            }

            if (canCraftNow)
            {
                if (_sim.TryCraftOrUpgradeHeroEquip(_wwHdHeroIdx, slot, out string msg))
                {
                    RefreshHeroDetailModal();
                    SpawnScreenCenterBanner(msg);
                }
                else if (!string.IsNullOrEmpty(msg))
                {
                    SpawnScreenCenterBanner(msg);
                }
                return;
            }

            // 若工坊等级不足则跳对应工坊，若缺材料则跳材料生产厂
            if (!string.IsNullOrEmpty(buildingReqHint) && !buildingReqHint.Contains("可打造"))
            {
                string wsId = (slot == HeroEquipSlot.Armor || slot == HeroEquipSlot.Boots) ? "tailor" : "smithy";
                JumpToNodeById(wsId);
                SpawnScreenCenterBanner(buildingReqHint);
                return;
            }

            if (nextCosts != null)
            {
                for (int i = 0; i < nextCosts.Length; i++)
                {
                    if (_sim.Get(nextCosts[i].res) < nextCosts[i].amount)
                    {
                        JumpToProduceOrGather(nextCosts[i].res);
                        return;
                    }
                }
            }
        }

        static string FormatStarReqStarsRichText(int reqStars)
        {
            int r = Mathf.Clamp(reqStars, 1, 5);
            string lit = new string('★', r);
            string dim = r < 5 ? new string('★', 5 - r) : "";
            return $"<color=#FF8826>{lit}</color><color=#7A829E>{dim}</color>";
        }

        void RefreshHeroSkillDetailPopup()
        {
            if (_wwSkillDetailPopup == null || _wwHdHeroIdx < 0 || _wwHdHeroIdx >= _sim.Heroes.Count) return;
            HeroSpec h = _sim.Heroes[_wwHdHeroIdx];
            HeroSkillDef sk = _sim.GetHeroSkillDef(_wwHdHeroIdx, _wwSkPopupSlotIdx);

            string fallbackKey = _wwSkPopupSlotIdx == 0 ? GetHeroUltArtKey(_wwHdHeroIdx, h)
                : (_wwSkPopupSlotIdx == 1 ? "ww_passive_sword" : (_wwSkPopupSlotIdx == 2 ? "ww_passive_heart" : "ww_passive_fury"));
            _wwSkPopupIcon.sprite = GetEconArt(sk.iconKey, ForestComicUiSkin.GetUiArt(fallbackKey));
            _wwSkPopupTagTxt.text = _wwSkPopupSlotIdx == 0 ? "主动" : "被动";
            _wwSkPopupNameTxt.text = sk.name;
            int shownLv = Mathf.Max(1, sk.currentLv);
            _wwSkPopupLvTxt.text = $"{shownLv}级";

            int ownBooks = _sim.Get(SurvRes.SkillBook);
            bool showBookCost = sk.currentLv < sk.maxLv && sk.bookCostNext > 0 && sk.isUnlocked;
            _wwSkPopupBookGroup.SetActive(showBookCost);
            if (showBookCost)
            {
                _wwSkPopupBookIco.sprite = GetEconResIcon(SurvRes.SkillBook);
                _wwSkPopupBookCntTxt.text = ownBooks >= sk.bookCostNext
                    ? $"{ownBooks}/{sk.bookCostNext}"
                    : $"<color=#E43A40>{ownBooks}</color>/{sk.bookCostNext}";
            }

            _wwSkPopupDescTxt.text = sk.descFormatted ?? "";
            _wwSkPopupMetricLbl.text = string.IsNullOrEmpty(sk.metricLabel) ? "等级加成" : sk.metricLabel;

            int highlightIdx = sk.isUnlocked ? Mathf.Clamp(sk.currentLv - 1, 0, 4) : 0;
            for (int t = 0; t < 5; t++)
            {
                string val = (sk.tierValues != null && t < sk.tierValues.Length) ? sk.tierValues[t] : "-";
                _wwSkPopupTierTxts[t].text = val;
                bool isCur = t == highlightIdx;
                _wwSkPopupTierTxts[t].color = isCur ? (Color)ForestComicUiSkin.InkDark : (Color)ForestComicUiSkin.InkMuted;
                _wwSkPopupTierTxts[t].fontSize = isCur ? 19 : 17;
                _wwSkPopupTierTxts[t].fontStyle = isCur ? FontStyle.Bold : FontStyle.Normal;
            }

            if (!h.unlocked)
            {
                _wwSkPopupUpBtn.gameObject.SetActive(false);
                _wwSkPopupRedGateTxt.gameObject.SetActive(true);
                _wwSkPopupRedGateTxt.color = ForestComicUiSkin.AccentRed;
                _wwSkPopupRedGateTxt.text = "招募解锁该英雄后可激活此技能";
            }
            else if (!sk.isUnlocked)
            {
                _wwSkPopupUpBtn.gameObject.SetActive(false);
                _wwSkPopupRedGateTxt.gameObject.SetActive(true);
                _wwSkPopupRedGateTxt.color = ForestComicUiSkin.AccentRed;
                _wwSkPopupRedGateTxt.text = $"英雄升星到 {FormatStarReqStarsRichText(sk.unlockStars)} 可解锁";
            }
            else if (sk.currentLv >= sk.maxLv)
            {
                _wwSkPopupUpBtn.gameObject.SetActive(false);
                _wwSkPopupRedGateTxt.gameObject.SetActive(true);
                _wwSkPopupRedGateTxt.color = ForestComicUiSkin.AccentGreen;
                _wwSkPopupRedGateTxt.text = "★ 该技能已升至最高满级 (5级)";
            }
            else if (h.star < sk.reqStarsForNextLv)
            {
                _wwSkPopupUpBtn.gameObject.SetActive(false);
                _wwSkPopupRedGateTxt.gameObject.SetActive(true);
                _wwSkPopupRedGateTxt.color = ForestComicUiSkin.AccentRed;
                _wwSkPopupRedGateTxt.text = $"英雄升星到 {FormatStarReqStarsRichText(sk.reqStarsForNextLv)} 可升级";
            }
            else
            {
                _wwSkPopupRedGateTxt.gameObject.SetActive(false);
                _wwSkPopupUpBtn.gameObject.SetActive(true);
                bool canAff = ownBooks >= sk.bookCostNext;
                _wwSkPopupUpBtnImg.sprite = canAff ? ForestComicUiSkin.BtnBlue : ForestComicUiSkin.BtnGrey;
                _wwSkPopupUpBtnLbl.fontSize = 20;
                _wwSkPopupUpBtnLbl.text = $"升级技能 ({sk.bookCostNext}本)";
            }
        }

        void RefreshHeroDetailSkillStrip()
        {
            if (_wwHdHeroIdx < 0 || _wwHdHeroIdx >= _sim.Heroes.Count) return;
            HeroSpec h = _sim.Heroes[_wwHdHeroIdx];
            int ownBooks = _sim.Get(SurvRes.SkillBook);

            for (int m = 0; m < 4; m++)
            {
                HeroSkillDef sk = _sim.GetHeroSkillDef(_wwHdHeroIdx, m);
                string fb = m == 0 ? GetHeroUltArtKey(_wwHdHeroIdx, h)
                    : (m == 1 ? "ww_passive_sword" : (m == 2 ? "ww_passive_heart" : "ww_passive_fury"));
                _wwHdSkillCircles[m].sprite = GetEconArt(sk.iconKey, ForestComicUiSkin.GetUiArt(fb));

                bool lit = h.unlocked && sk.isUnlocked;
                _wwHdSkillCircles[m].color = lit ? Color.white : new Color32(132, 138, 158, 238);
                _wwHdSkillSelectFrames[m].SetActive(m == _wwHdSelectedSkillIdx);

                if (lit)
                {
                    _wwHdSkillLockOverlayTxts[m].text = "";
                    _wwHdSkillBottomPills[m].SetActive(true);
                    _wwHdSkillBottomPillTxts[m].text = $"{Mathf.Max(1, sk.currentLv)}级";
                }
                else
                {
                    _wwHdSkillLockOverlayTxts[m].text = m == 0 ? "" : $"{sk.unlockStars}星解锁";
                    _wwHdSkillBottomPills[m].SetActive(m == 0);
                    if (m == 0) _wwHdSkillBottomPillTxts[m].text = "1级";
                }

                bool canUpSk = lit && sk.currentLv < sk.maxLv && h.star >= sk.reqStarsForNextLv && ownBooks >= sk.bookCostNext;
                _wwHdSkillUpgradeDots[m].SetActive(canUpSk);
            }
        }

        void RefreshHeroDetailModal()
        {
            if (_sim.Heroes.Count == 0) return;
            _wwHdHeroIdx = Mathf.Clamp(_wwHdHeroIdx, 0, _sim.Heroes.Count - 1);
            HeroSpec h = _sim.Heroes[_wwHdHeroIdx];

            // 1. 船舱顶部独立资源胶囊
            int ownExp = _sim.Get(SurvRes.HeroExp);
            int ownBooks = _sim.Get(SurvRes.SkillBook);
            _wwHdHeroExpTxt.text = FormatEconCount(ownExp);
            _wwHdSkillBookTxt.text = FormatEconCount(ownBooks);

            _wwHdNameTxt.text = h.name;
            _wwHdNameTxt.color = h.rarity == HeroRarity.Legendary
                ? new Color32(255, 206, 78, 255)
                : (h.rarity == HeroRarity.Epic ? new Color32(246, 94, 238, 255) : new Color32(118, 186, 255, 255));
            _wwHdRarityRibbonImg.sprite = ForestComicUiSkin.RarityRibbon(h.rarity);
            _wwHdStageFactionCrest.sprite = GetFactionCrestSprite(h.faction);
            _wwHdRarityTxt.text = h.RarityTag;

            bool inMainSquad = h.unlocked && _sim.FindHeroSlot(_wwHdHeroIdx) >= 0;
            _wwHdMainSquadBadgeGo.SetActive(inMainSquad);

            ApplyHeroFullBodySprite(_wwHdCabinPortrait, h);
            _wwHdCabinPortrait.color = h.unlocked ? Color.white : new Color32(34, 40, 68, 238);

            bool atCap = h.unlocked && h.level >= _sim.MaxHeroLevelCap;
            _wwHdCapBannerGo.SetActive(!h.unlocked || atCap);
            if (!h.unlocked)
                _wwHdCapBannerTxt.text = $"尚未入队 · 专属碎片 <color=#FFD44C>{h.shards}/10</color>（{GetHeroRecruitConditionText(_wwHdHeroIdx)}）";
            else if (atCap)
                _wwHdCapBannerTxt.text = $"已达上限，需要提升<color=#FFD44C>玩家</color>等级 (领主 Lv.{_sim.CampLevel} · 上限 Lv.{_sim.MaxHeroLevelCap})";

            // 2. 战力铭牌、右上五瓣星条、阵营职业与等级重置栏
            int slotInSquad = _sim.FindHeroSlot(_wwHdHeroIdx);
            HeroComputedStats fullStats = _sim.CalcHeroFullStats(_wwHdHeroIdx, slotInSquad, includeSquadSynergy: slotInSquad >= 0);
            _wwHdPowerTxt.text = $"{fullStats.combatPower}";

            ApplyFivePetalStarVisuals(_wwHdTopStarBaseImgs, _wwHdTopStarPetalFills, h.unlocked, h.star, h.starPetals);

            _wwHdHeaderFactionImg.sprite = GetFactionCrestSprite(h.faction);
            _wwHdHeaderRoleImg.sprite = GetRoleMiniSprite(_sim.GetHeroCombatRole(_wwHdHeroIdx));
            _wwHdRoleTagTxt.text = _sim.GetHeroRoleName(_wwHdHeroIdx);
            _wwHdLevelTxt.text = $"等级: {h.level}/{_sim.MaxHeroLevelCap}";
            _wwHdResetLvBtn.gameObject.SetActive(h.unlocked && (h.level > 1 || h.spentSkillBooks > 0));

            for (int t = 0; t < 4; t++)
            {
                _wwHdSubPages[t].SetActive(t == _wwHdTabIdx);
                ApplyStoneTabVisual(_wwHdBottomTabImgs[t], _wwHdBottomTabLbls[t], _wwHdBottomTabOutlines[t], t == _wwHdTabIdx);
            }

            // 底部 4 子页签红点计算
            int expCost = _sim.GetHeroLevelUpExpCost(h.level);
            bool canLvUp = h.unlocked && h.level < _sim.MaxHeroLevelCap && ownExp >= expCost;
            bool canSkillUp = false;
            if (h.unlocked)
            {
                for (int m = 0; m < 4; m++)
                {
                    HeroSkillDef sd = _sim.GetHeroSkillDef(_wwHdHeroIdx, m);
                    if (sd.isUnlocked && sd.currentLv < sd.maxLv && h.star >= sd.reqStarsForNextLv && ownBooks >= sd.bookCostNext)
                    { canSkillUp = true; break; }
                }
            }
            int petalCost = h.unlocked && h.star < 5 ? _sim.GetNextPetalShardCost(_wwHdHeroIdx) : 9999;
            int univOwn = _sim.GetAvailableUniversalShardsFor(_wwHdHeroIdx);
            bool canStarPetal = h.unlocked && h.star < 5 && (h.shards + (_sim.AutoExchangeUniversalShards ? univOwn : 0)) >= petalCost;

            bool canBadgeEquip = false;
            if (h.unlocked)
            {
                bool hasEmptyBadgeSlot = false;
                for (int s = 0; s < 3; s++)
                    if (_sim.IsBadgeSlotUnlocked(h, s) && h.equippedBadges[s] == TacticalBadgeId.None) hasEmptyBadgeSlot = true;
                if (hasEmptyBadgeSlot)
                {
                    for (int b = 1; b <= 6; b++)
                        if (_sim.GetFreeBadgeCount((TacticalBadgeId)b) > 0) { canBadgeEquip = true; break; }
                }
            }

            bool canGearCraft = false;
            if (h.unlocked)
            {
                for (int g = 0; g < 4; g++)
                {
                    _sim.GetHeroEquipSpec(_wwHdHeroIdx, (HeroEquipSlot)g, out _, out _, out _, out _, out _, out _, out _, out _, out bool okNow);
                    if (okNow) { canGearCraft = true; break; }
                }
            }

            _wwHdBottomTabDots[0].SetActive(canLvUp || canSkillUp || (!h.unlocked && h.shards >= 10));
            _wwHdBottomTabDots[1].SetActive(canStarPetal);
            _wwHdBottomTabDots[2].SetActive(canBadgeEquip);
            _wwHdBottomTabDots[3].SetActive(canGearCraft);

            // ---------------- Sub-tab 0: [属性] ----------------
            HeroComputedStats baseStats = _sim.CalcHeroFullStats(_wwHdHeroIdx, -1, includeSquadSynergy: false);
            _wwHdStatVals[0].text = baseStats.maxHp.ToString();
            _wwHdStatVals[1].text = baseStats.range.ToString();
            _wwHdStatVals[2].text = baseStats.atk.ToString();
            _wwHdStatVals[3].text = baseStats.armor.ToString();
            _wwHdStatVals[4].text = baseStats.skillPower.ToString();
            _wwHdStatVals[5].text = baseStats.resistance.ToString();

            RefreshHeroDetailSkillStrip();

            DelegationPostKind? assignedPost = _sim.GetHeroAssignedPost(h.id);
            string delNote = assignedPost.HasValue
                ? $"已驻守【{GetDepartmentDisplayTitle(assignedPost.Value)}】"
                : $"擅长【{GetDepartmentDisplayTitle(h.prefPost1)}】{FormatHeroSinglePostContribution(h, h.prefPost1)}（点击派驻）";
            _wwHdCampSpecBriefTxt.text = $"点击上方圆章查看/升级技能  ·  <color=#6B5CE7>{delNote}</color>";

            if (!h.unlocked)
            {
                bool rdy = h.shards >= 10;
                _wwHdLvUpMainBtnImg.sprite = rdy ? ForestComicUiSkin.BtnGreen : ForestComicUiSkin.BtnOrange;
                _wwHdLvUpCostIco.sprite = GetHeroJigsawShardSprite(h);
                _wwHdLvUpMainBtnLbl.text = rdy ? $"合成招募 ({h.shards}/10)" : $"招募获取 (碎片 {h.shards}/10)";
                _wwHdLvUpRedDot.SetActive(rdy);
            }
            else if (h.level >= _sim.MaxHeroLevelCap)
            {
                _wwHdLvUpMainBtnImg.sprite = ForestComicUiSkin.BtnGrey;
                _wwHdLvUpCostIco.sprite = GetEconResIcon(SurvRes.HeroExp);
                _wwHdLvUpMainBtnLbl.text = $"已达上限 ({FormatEconCount(expCost)})";
                _wwHdLvUpRedDot.SetActive(false);
            }
            else
            {
                _wwHdLvUpMainBtnImg.sprite = ownExp >= expCost ? ForestComicUiSkin.BtnBlue : ForestComicUiSkin.BtnGrey;
                _wwHdLvUpCostIco.sprite = GetEconResIcon(SurvRes.HeroExp);
                _wwHdLvUpMainBtnLbl.fontSize = 18;
                _wwHdLvUpMainBtnLbl.text = $"升 级\n英雄经验 {FormatEconCount(expCost)}";
                _wwHdLvUpRedDot.SetActive(ownExp >= expCost);
            }

            // ---------------- Sub-tab 1: [升星] (5星×5星瓣) ----------------
            if (h.star >= 5)
            {
                ApplyFivePetalStarVisuals(_wwHdStarLeftBases, _wwHdStarLeftFills, true, 5, 0);
                ApplyFivePetalStarVisuals(_wwHdStarRightBases, _wwHdStarRightFills, true, 5, 0);
                _wwHdStarStageTagTxt.text = "★5 传奇觉醒满星（25/25 星瓣全部点亮！）";

                _wwHdStarCurVals[0].text = baseStats.maxHp.ToString();
                _wwHdStarNextVals[0].text = "满阶";
                _wwHdStarCurVals[1].text = baseStats.atk.ToString();
                _wwHdStarNextVals[1].text = "满阶";
                _wwHdStarCurVals[2].text = baseStats.skillPower.ToString();
                _wwHdStarNextVals[2].text = "满阶";

                _wwHdStarMilestoneTxt.text = "★ 已解锁全部 4 大技能与全部 3 个战地徽章槽！";
                _wwHdShardBarFill.fillAmount = 1f;
                _wwHdShardBarTxt.text = $"{h.shards}/满星";
                _wwHdStarUpMainBtnImg.sprite = ForestComicUiSkin.BtnGrey;
                _wwHdStarUpMainBtnLbl.text = "★ 已满星";
                _wwHdStarUpRedDot.SetActive(false);
            }
            else
            {
                int nextStar = h.star;
                int nextPetals = h.starPetals + 1;
                bool willCrossStar = false;
                if (nextPetals >= 5)
                {
                    nextStar = Mathf.Min(5, h.star + 1);
                    nextPetals = 0;
                    willCrossStar = true;
                }

                ApplyFivePetalStarVisuals(_wwHdStarLeftBases, _wwHdStarLeftFills, true, h.star, h.starPetals);
                ApplyFivePetalStarVisuals(_wwHdStarRightBases, _wwHdStarRightFills, true, nextStar, nextPetals, highlightNextPetal: true);
                _wwHdStarStageTagTxt.text = willCrossStar
                    ? $"★{h.star}·{h.starPetals}/5瓣  ▶▶  ★{nextStar} 整星突破！"
                    : $"★{h.star}·{h.starPetals}/5瓣  ▶▶  ★{nextStar}·{nextPetals}/5瓣";

                // 精确预览下一瓣真实 HeroComputedStats（原地暂改后立即还原，零副作用）
                int savedStar = h.star;
                int savedPetals = h.starPetals;
                h.star = nextStar;
                h.starPetals = nextPetals;
                HeroComputedStats nextStats = _sim.CalcHeroFullStats(_wwHdHeroIdx, -1, includeSquadSynergy: false);
                h.star = savedStar;
                h.starPetals = savedPetals;

                _wwHdStarCurVals[0].text = baseStats.maxHp.ToString();
                _wwHdStarNextVals[0].text = nextStats.maxHp.ToString();
                _wwHdStarCurVals[1].text = baseStats.atk.ToString();
                _wwHdStarNextVals[1].text = nextStats.atk.ToString();
                _wwHdStarCurVals[2].text = baseStats.skillPower.ToString();
                _wwHdStarNextVals[2].text = nextStats.skillPower.ToString();

                int targetFullStar = h.star + 1;
                if (targetFullStar == 1)
                {
                    HeroSkillDef sk1 = _sim.GetHeroSkillDef(_wwHdHeroIdx, 1);
                    _wwHdStarMilestoneIco.sprite = GetEconArt(sk1.iconKey, ForestComicUiSkin.GetUiArt("ww_passive_sword"));
                    _wwHdStarMilestoneTxt.text = $"升至 <color=#FF8826>1★</color> 里程碑：解锁第2技能【{sk1.name}】 + 开放第1徽章槽";
                }
                else if (targetFullStar == 2)
                {
                    HeroSkillDef sk2 = _sim.GetHeroSkillDef(_wwHdHeroIdx, 2);
                    _wwHdStarMilestoneIco.sprite = GetEconArt(sk2.iconKey, ForestComicUiSkin.GetUiArt("ww_passive_heart"));
                    _wwHdStarMilestoneTxt.text = $"升至 <color=#FF8826>2★</color> 里程碑：解锁第3技能【{sk2.name}】 + 委任效率+25%";
                }
                else if (targetFullStar == 3)
                {
                    HeroSkillDef sk3 = _sim.GetHeroSkillDef(_wwHdHeroIdx, 3);
                    _wwHdStarMilestoneIco.sprite = GetEconArt(sk3.iconKey, ForestComicUiSkin.GetUiArt("ww_passive_fury"));
                    _wwHdStarMilestoneTxt.text = $"升至 <color=#FF8826>3★</color> 核心质变：解锁【{sk3.name}】 + 开放第2徽章槽！";
                }
                else if (targetFullStar == 4)
                {
                    _wwHdStarMilestoneIco.sprite = GetEconResIcon(SurvRes.SkillBook);
                    _wwHdStarMilestoneTxt.text = "升至 <color=#FF8826>4★</color> 高阶突破：全技能上限开放至满级 Lv.5！";
                }
                else
                {
                    _wwHdStarMilestoneIco.sprite = GetEconArt("ico_badge_eagle", ForestComicUiSkin.GetUiArt("ww_xp_star"));
                    _wwHdStarMilestoneTxt.text = "升至 <color=#FF8826>5★</color> 觉醒满星：开放第3徽章槽 + 觉醒满阶光环！";
                }

                int reqShards = h.unlocked ? Mathf.Max(1, petalCost) : 10;
                _wwHdShardBarFill.fillAmount = Mathf.Clamp01(h.shards / (float)reqShards);
                _wwHdShardBarTxt.text = $"{h.shards}/{reqShards}";

                _wwHdStarUpMainBtnImg.sprite = !h.unlocked
                    ? (h.shards >= 10 ? ForestComicUiSkin.BtnGreen : ForestComicUiSkin.BtnOrange)
                    : (canStarPetal ? ForestComicUiSkin.BtnBlue : ForestComicUiSkin.BtnGrey);
                _wwHdStarUpMainBtnLbl.text = !h.unlocked
                    ? (h.shards >= 10 ? "合成招募" : "去招募")
                    : (willCrossStar ? "整星突破" : "升 星");
                _wwHdStarUpRedDot.SetActive(canStarPetal);
            }

            _wwHdShardCardFrame.sprite = ForestComicUiSkin.GetClippedCard(h.rarity);
            _wwHdShardIcon.sprite = GetHeroJigsawShardSprite(h);
            _wwHdShardTitleTxt.text = $"{h.name}碎片";
            _wwHdShardTitleTxt.color = h.rarity == HeroRarity.Legendary
                ? new Color32(196, 102, 16, 255)
                : (h.rarity == HeroRarity.Epic ? new Color32(146, 42, 198, 255) : (Color)ForestComicUiSkin.InkDark);

            SurvRes univRes = _sim.GetUniversalShardResFor(h.rarity);
            _wwHdUnivExchangeIco.sprite = GetEconResIcon(univRes);
            _wwHdUnivExchangeCntTxt.text = univOwn.ToString();
            _wwHdAutoExchangeCheckGo.SetActive(_sim.AutoExchangeUniversalShards);

            // ---------------- Sub-tab 2: [徽章] ----------------
            int[] badgeUnlockStars = { 1, 3, 5 };
            for (int s = 0; s < 3; s++)
            {
                bool sOpen = h.unlocked && _sim.IsBadgeSlotUnlocked(h, s);
                TacticalBadgeId eq = sOpen ? h.equippedBadges[s] : TacticalBadgeId.None;
                _wwHdBadgeSlotBrackets[s].SetActive(sOpen && s == _wwHdSelectedBadgeSlot);

                if (!sOpen)
                {
                    _wwHdBadgeSlotFrames[s].sprite = ForestComicUiSkin.WwLightSheet;
                    _wwHdBadgeSlotIcons[s].gameObject.SetActive(false);
                    _wwHdBadgeSlotLocks[s].gameObject.SetActive(true);
                    _wwHdBadgeSlotPlusTxts[s].gameObject.SetActive(false);
                    _wwHdBadgeSlotTitles[s].text = $"{badgeUnlockStars[s]}★解锁";
                    _wwHdBadgeSlotTitles[s].color = ForestComicUiSkin.InkMuted;
                }
                else if (eq != TacticalBadgeId.None)
                {
                    _wwHdBadgeSlotFrames[s].sprite = ForestComicUiSkin.GetClippedCard(ForestSurvivalSim.GetBadgeRarity0To5(eq));
                    _wwHdBadgeSlotIcons[s].gameObject.SetActive(true);
                    _wwHdBadgeSlotIcons[s].sprite = GetEconArt(ForestSurvivalSim.GetBadgeIconKey(eq));
                    _wwHdBadgeSlotLocks[s].gameObject.SetActive(false);
                    _wwHdBadgeSlotPlusTxts[s].gameObject.SetActive(false);
                    _wwHdBadgeSlotTitles[s].text = ForestSurvivalSim.GetBadgeName(eq);
                    _wwHdBadgeSlotTitles[s].color = ForestComicUiSkin.InkDark;
                }
                else
                {
                    _wwHdBadgeSlotFrames[s].sprite = ForestComicUiSkin.WwLightSheet;
                    _wwHdBadgeSlotIcons[s].gameObject.SetActive(false);
                    _wwHdBadgeSlotLocks[s].gameObject.SetActive(false);
                    _wwHdBadgeSlotPlusTxts[s].gameObject.SetActive(true);
                    _wwHdBadgeSlotTitles[s].text = "＋ 点击装配";
                    _wwHdBadgeSlotTitles[s].color = ForestComicUiSkin.AccentGreen;
                }
            }

            for (int i = 0; i < 6; i++)
            {
                TacticalBadgeId bId = (TacticalBadgeId)(i + 1);
                int ownB = _sim.GetOwnedBadgeCount(bId);
                int freeB = _sim.GetFreeBadgeCount(bId);
                int rar = ForestSurvivalSim.GetBadgeRarity0To5(bId);

                _wwHdBadgeRowCards[i].sprite = ownB > 0 ? ForestComicUiSkin.GetClippedCard(rar) : ForestComicUiSkin.ClippedCardGrey;
                _wwHdBadgeRowIcons[i].sprite = GetEconArt(ForestSurvivalSim.GetBadgeIconKey(bId));
                _wwHdBadgeRowIcons[i].color = ownB > 0 ? Color.white : new Color(0.55f, 0.58f, 0.68f, 0.82f);
                _wwHdBadgeRowCounts[i].text = $"空闲 {freeB} / 拥有 {ownB}";

                bool equippedOnCur = false;
                for (int s = 0; s < 3; s++)
                    if (h.unlocked && h.equippedBadges[s] == bId) equippedOnCur = true;

                bool anySlotOpen = h.unlocked && _sim.IsBadgeSlotUnlocked(h, 0);
                if (equippedOnCur)
                {
                    _wwHdBadgeRowBtnImgs[i].sprite = ForestComicUiSkin.BtnRed;
                    _wwHdBadgeRowBtnLbls[i].text = "卸 下";
                }
                else if (ownB <= 0)
                {
                    _wwHdBadgeRowBtnImgs[i].sprite = ForestComicUiSkin.BtnOrange;
                    _wwHdBadgeRowBtnLbls[i].text = "闯关获取";
                }
                else if (!anySlotOpen)
                {
                    _wwHdBadgeRowBtnImgs[i].sprite = ForestComicUiSkin.BtnGrey;
                    _wwHdBadgeRowBtnLbls[i].text = "需1★槽";
                }
                else if (freeB > 0)
                {
                    _wwHdBadgeRowBtnImgs[i].sprite = ForestComicUiSkin.BtnBlue;
                    _wwHdBadgeRowBtnLbls[i].text = "装 配";
                }
                else
                {
                    _wwHdBadgeRowBtnImgs[i].sprite = ForestComicUiSkin.BtnGrey;
                    _wwHdBadgeRowBtnLbls[i].text = "已穿完";
                }
            }

            // ---------------- Sub-tab 3: [装备] (4部位工坊战备打造升阶) ----------------
            string[] tierNames = { "未穿戴", "1阶·绿品", "2阶·蓝品", "3阶·紫品", "4阶·橙品" };
            for (int i = 0; i < 4; i++)
            {
                HeroEquipSlot slot = (HeroEquipSlot)i;
                _sim.GetHeroEquipSpec(_wwHdHeroIdx, slot, out int curTier, out _, out string curName, out string nextName,
                    out string iconKey, out string statSummary, out CostPair[] nextCosts, out string buildingReqHint, out bool canCraftNow);

                int clampedTier = Mathf.Clamp(curTier, 0, 4);
                _wwHdGearTierTags[i].text = tierNames[clampedTier];
                _wwHdGearTierTags[i].color = clampedTier == 0 ? (Color)ForestComicUiSkin.InkMuted
                    : (clampedTier >= 3 ? (Color)ForestComicUiSkin.AccentPurple : (Color)ForestComicUiSkin.AccentGreen);

                _wwHdGearClipTiles[i].sprite = ForestComicUiSkin.GetClippedCard(clampedTier);
                _wwHdGearIcons[i].sprite = GetEconArt(iconKey, ForestComicUiSkin.ResIcon(SurvRes.PlateArmor));
                _wwHdGearIcons[i].color = clampedTier > 0 ? Color.white : new Color(0.58f, 0.62f, 0.72f, 0.85f);

                _wwHdGearNames[i].text = clampedTier == 0
                    ? $"待打造：{nextName}"
                    : (clampedTier >= 4 ? $"{curName} (满阶)" : $"{curName}\n▶ {nextName}");
                _wwHdGearStats[i].text = statSummary ?? "";

                if (clampedTier >= 4)
                {
                    _wwHdGearCosts[i].text = "<color=#22B24C>★ 已达最高传世4阶战备</color>";
                    _wwHdGearBtnImgs[i].sprite = ForestComicUiSkin.BtnGrey;
                    _wwHdGearBtnLbls[i].text = "已满阶";
                }
                else
                {
                    var costParts = new List<string>(3);
                    if (nextCosts != null)
                    {
                        for (int c = 0; c < nextCosts.Length; c++)
                        {
                            int have = _sim.Get(nextCosts[c].res);
                            int need = nextCosts[c].amount;
                            string nm = ForestSurvivalSim.ResName(nextCosts[c].res);
                            costParts.Add(have >= need ? $"{nm} {have}/{need}" : $"<color=#E43A40>{nm} {have}/{need}</color>");
                        }
                    }
                    string matLine = string.Join(" · ", costParts);
                    string bldgColor = (!string.IsNullOrEmpty(buildingReqHint) && buildingReqHint.Contains("可打造"))
                        ? "#22B24C"
                        : "#E43A40";
                    _wwHdGearCosts[i].fontSize = 12;
                    _wwHdGearCosts[i].text = !string.IsNullOrEmpty(buildingReqHint)
                        ? $"<color={bldgColor}>{buildingReqHint}</color>\n{matLine}"
                        : matLine;

                    if (!h.unlocked)
                    {
                        _wwHdGearBtnImgs[i].sprite = ForestComicUiSkin.BtnGrey;
                        _wwHdGearBtnLbls[i].text = "待招募伙伴";
                    }
                    else if (canCraftNow)
                    {
                        _wwHdGearBtnImgs[i].sprite = ForestComicUiSkin.BtnGreen;
                        _wwHdGearBtnLbls[i].text = clampedTier == 0 ? "打造穿戴" : "升阶强化";
                    }
                    else
                    {
                        _wwHdGearBtnImgs[i].sprite = ForestComicUiSkin.BtnOrange;
                        _wwHdGearBtnLbls[i].text = "去工坊生产";
                    }
                }
            }
        }

        // =====================================================================
        // Screen 4 逻辑与实时刷新：【道具仓库】4分类切角库存 + 自选箱兑换
        // =====================================================================
        void OpenChoiceResBoxModal()
        {
            if (_sim.Get(SurvRes.ChoiceResBox) <= 0)
            {
                SpawnScreenCenterBanner("当前暂无【原料自选补给箱】！完成5单居民需求或山口闯关可获取！");
                return;
            }
            RefreshChoiceResBoxPopup();
            _wwChoiceBoxPopup.transform.SetAsLastSibling();
            _wwChoiceBoxPopup.SetActive(true);
        }

        void RefreshChoiceResBoxPopup()
        {
            if (_wwChoiceBoxPopup == null) return;
            int boxes = _sim.Get(SurvRes.ChoiceResBox);
            _wwChoiceSubTxt.text = $"请选择要兑换的紧缺基础原料（每箱领 5 份，当前拥有 {boxes} 箱）：";
            for (int i = 0; i < ChoiceBoxCandidateRes.Length; i++)
            {
                SurvRes r = ChoiceBoxCandidateRes[i];
                bool sel = r == _wwChoiceSelectedRes;
                _wwChoiceBrackets[i].SetActive(sel);
                _wwChoiceChecks[i].SetActive(sel);
                _wwChoiceOwnTxts[i].text = $"存:{_sim.Get(r)}";
            }
        }

        void OnConfirmChoiceResBoxClaim()
        {
            SurvRes picked = _wwChoiceSelectedRes;
            if (_sim.TryOpenChoiceResBox(picked, 1, out string msg))
            {
                _wwChoiceBoxPopup.SetActive(false);
                RefreshBagInventoryModal();
                ShowCongratsRewardModal("开启原料自选补给箱", new[]
                {
                    (picked, 5, ForestComicUiSkin.ResIcon(picked), ForestSurvivalSim.ResName(picked)),
                });
            }
            else if (!string.IsNullOrEmpty(msg))
            {
                SpawnScreenCenterBanner(msg);
            }
        }

        static string GetBagResUsageNote(SurvRes r)
        {
            switch (r)
            {
                case SurvRes.ChoiceResBox: return "打开可任选兑换 5 份所需的紧缺基础原料（木/铁/水/布/草药）";
                case SurvRes.HeroExp: return "用于【英雄】舱室提升幸存者英雄等级（支持100%免费无损重置）";
                case SurvRes.RecruitTicket: return "用于在【广播招募站】单抽或十连呼叫幸存者英雄与专属碎片";
                case SurvRes.SkillBook: return "用于提升英雄 4 大主被动技能等级，大幅强化绝技倍率与属性";
                case SurvRes.SpeedHourglass: return "使用后立即缩短全营施工、五大工坊队列与农牧倒计时 5 分钟";
                case SurvRes.ShardUnivEpic: return "可在史诗/稀有英雄【升星】页 1:1 兑换补齐专属拼图碎片";
                case SurvRes.ShardUnivLeg: return "可在传说英雄【升星】页 1:1 兑换补齐专属拼图碎片";
                case SurvRes.Blueprint: return "用于中后期核心建筑升阶扩建与高阶专武锻造";
                case SurvRes.Parts: return "用于配制抗尸毒急救箱、修缮电台与制造红外战术镜";
                case SurvRes.Compost: return "用于【生态选种农田】施肥催熟，单垄产量翻 1.5 倍";
                case SurvRes.SeedVeggie:
                case SurvRes.SeedFlax:
                case SurvRes.SeedHerb:
                case SurvRes.SeedGolden: return "用于【生态选种农田】播种收获对应作物";
                case SurvRes.Ration: return "用于林缘砍树扩地、丧尸城发车搜刮与行商兑换补给";
                case SurvRes.LinenShirt:
                case SurvRes.LeatherBoots:
                case SurvRes.FurCoat:
                case SurvRes.PlateArmor: return "交付【居民穿戴订单】、行商贸易，或在英雄【装备】槽升阶防具";
                default: return "用于营地基建升级、工坊排产、英雄装备锻造或满足居民需求";
            }
        }

        void BuildBagEntriesForTab(int tabIdx, List<BagEntry> outList)
        {
            outList.Clear();
            switch (tabIdx)
            {
                // Tab 0: [加工品] (0..25 营地加工成品与原材)
                case 0:
                {
                    var owned = new List<SurvRes>();
                    var empty = new List<SurvRes>();
                    for (int i = 0; i < BagCraftedAndRawItems.Length; i++)
                    {
                        SurvRes r = BagCraftedAndRawItems[i];
                        if (_sim.Get(r) > 0) owned.Add(r);
                        else empty.Add(r);
                    }
                    owned.AddRange(empty);
                    for (int i = 0; i < owned.Count; i++)
                        outList.Add(MakeResBagEntry(owned[i]));
                    break;
                }

                // Tab 1: [消耗品] (自选箱、英雄经验、招募卡、技能书、加速沙漏、手稿、口粮、种子等)
                case 1:
                {
                    var owned = new List<SurvRes>();
                    var empty = new List<SurvRes>();
                    for (int i = 0; i < BagConsumableItems.Length; i++)
                    {
                        SurvRes r = BagConsumableItems[i];
                        if (_sim.Get(r) > 0) owned.Add(r);
                        else empty.Add(r);
                    }
                    owned.AddRange(empty);
                    for (int i = 0; i < owned.Count; i++)
                        outList.Add(MakeResBagEntry(owned[i]));
                    break;
                }

                // Tab 2: [碎片] (6名英雄拼图碎片 + 传说/史诗万能碎片 + 阵型图谱卡)
                case 2:
                {
                    for (int i = 0; i < _sim.Heroes.Count; i++)
                    {
                        int hIdx = i;
                        HeroSpec h = _sim.Heroes[i];
                        int rar = h.rarity == HeroRarity.Legendary ? 4 : (h.rarity == HeroRarity.Epic ? 3 : 2);
                        bool canSyn = !h.unlocked && h.shards >= 10;
                        string colorHex = h.rarity == HeroRarity.Legendary ? "#D97706" : (h.rarity == HeroRarity.Epic ? "#A21CAF" : "#2563EB");

                        string actLbl = canSyn ? "合成英雄" : (h.unlocked ? "去升星" : "去招募");
                        Sprite actSkin = canSyn ? ForestComicUiSkin.BtnGreen : (h.unlocked ? ForestComicUiSkin.BtnBlue : ForestComicUiSkin.BtnOrange);
                        Action actCb = () =>
                        {
                            if (!h.unlocked && h.shards >= 10)
                            {
                                if (_sim.TryRecruitHeroWithShards(hIdx, out string m))
                                {
                                    RefreshBagInventoryModal();
                                    SpawnScreenCenterBanner(m);
                                }
                            }
                            else if (h.unlocked)
                            {
                                OpenHeroDetailModal(hIdx, 1);
                            }
                            else
                            {
                                OpenRadioRecruitModal();
                            }
                        };

                        outList.Add(new BagEntry
                        {
                            kind = BagEntryKind.HeroShard,
                            subId = hIdx,
                            keyId = "shard_hero_" + h.id,
                            name = $"{h.name}碎片",
                            icon = GetHeroJigsawShardSprite(h),
                            rarity0to5 = rar,
                            count = h.shards,
                            stockTag = h.unlocked ? $"已招募 ★{h.star}" : $"合成进度 {h.shards}/10",
                            desc = h.unlocked
                                ? $"用于英雄 <color={colorHex}>{h.name}</color> 点亮星瓣与升星突破\n来源：广播招募、山口闯关、行商珍品"
                                : $"集齐 10 片可直接合成招募英雄 <color={colorHex}>{h.name}</color>！\n来源：广播招募、主线首通里程碑",
                            actionLabel = actLbl,
                            actionSkin = actSkin,
                            onAction = actCb,
                        });
                    }

                    outList.Add(MakeResBagEntry(SurvRes.ShardUnivLeg));
                    outList.Add(MakeResBagEntry(SurvRes.ShardUnivEpic));

                    // 5 张可收集阵型图谱芯片
                    TacticalFormationId[] atlasFmts =
                    {
                        TacticalFormationId.SpearAssault,
                        TacticalFormationId.ResistBarrier,
                        TacticalFormationId.IronBastion,
                        TacticalFormationId.VolleyFire,
                        TacticalFormationId.EnergySurge,
                    };
                    int[] fmtGlobalPcts = { 2, 2, 3, 3, 5 };
                    for (int f = 0; f < atlasFmts.Length; f++)
                    {
                        TacticalFormationId fmt = atlasFmts[f];
                        bool unl = _sim.IsFormationUnlocked(fmt);
                        string fName = _sim.GetFormationName(fmt);
                        int gPct = fmtGlobalPcts[f];
                        outList.Add(new BagEntry
                        {
                            kind = BagEntryKind.FormationCard,
                            subId = (int)fmt,
                            keyId = "fmt_" + fmt,
                            name = $"阵型·{fName}",
                            icon = GetFormationChipSprite(_sim, fmt),
                            rarity0to5 = gPct >= 5 ? 4 : 3,
                            count = unl ? 1 : 0,
                            stockTag = unl ? "✓ 已激活图谱" : "未收集",
                            desc = $"收集永久加成：全英雄生命/攻击/技能强度 <color=#22B24C>+{gPct}%</color>\n上阵站位效果：{_sim.GetFormationSummary(fmt)}",
                            actionLabel = unl ? "去布阵" : "闯关获取",
                            actionSkin = unl ? ForestComicUiSkin.BtnBlue : ForestComicUiSkin.BtnOrange,
                            onAction = () =>
                            {
                                if (unl) OpenTacticalFormationModal();
                                else OpenStagePosterModal();
                            },
                        });
                    }
                    break;
                }

                // Tab 3: [装备] (6种战地徽章 + 4部位工坊战备总览)
                default:
                {
                    for (int b = 1; b <= 6; b++)
                    {
                        TacticalBadgeId bId = (TacticalBadgeId)b;
                        int own = _sim.GetOwnedBadgeCount(bId);
                        int free = _sim.GetFreeBadgeCount(bId);
                        outList.Add(new BagEntry
                        {
                            kind = BagEntryKind.BadgeItem,
                            subId = b,
                            keyId = "badge_" + bId,
                            name = ForestSurvivalSim.GetBadgeName(bId),
                            icon = GetEconArt(ForestSurvivalSim.GetBadgeIconKey(bId)),
                            rarity0to5 = ForestSurvivalSim.GetBadgeRarity0To5(bId),
                            count = own,
                            stockTag = $"空闲 {free}/{own}",
                            desc = $"{ForestSurvivalSim.GetBadgeDesc(bId)}\n来源：首领关首通、丧尸城高危空投箱",
                            actionLabel = own > 0 ? "前往装配" : "闯关获取",
                            actionSkin = own > 0 ? ForestComicUiSkin.BtnBlue : ForestComicUiSkin.BtnOrange,
                            onAction = () =>
                            {
                                if (own > 0) OpenHeroDetailModal(0, 2);
                                else OpenStagePosterModal();
                            },
                        });
                    }

                    string[] gearSlotTitles = { "英雄武器战备", "英雄护甲战备", "英雄战靴战备", "英雄战术护目镜" };
                    string[] gearSlotIcons = { "ico_gear_weapon", "ico_gear_armor", "ico_gear_boots", "ico_gear_goggles" };
                    string[] gearSlotDescs =
                    {
                        "由【废铁锻造坊】材料打造升阶，大幅提升英雄攻击与技能强度。",
                        "由【裁缝防具坊】衣甲打造升阶，提升生命与护甲并同步激活高阶外观！",
                        "由【裁缝防具坊】皮料打造升阶，提升最大生命与元素抗性。",
                        "由【锻造坊+电子件】精密打造升阶，提升技能强度与暴击率。",
                    };
                    for (int g = 0; g < 4; g++)
                    {
                        int equippedPieces = 0;
                        int maxTier = 0;
                        for (int h = 0; h < _sim.Heroes.Count; h++)
                        {
                            int t = _sim.Heroes[h].equipTiers[g];
                            if (t > 0) equippedPieces++;
                            if (t > maxTier) maxTier = t;
                        }
                        outList.Add(new BagEntry
                        {
                            kind = BagEntryKind.EquipSummary,
                            subId = g,
                            keyId = "gear_slot_" + g,
                            name = gearSlotTitles[g],
                            icon = GetEconArt(gearSlotIcons[g], ForestComicUiSkin.ResIcon(SurvRes.PlateArmor)),
                            rarity0to5 = Mathf.Max(1, maxTier),
                            count = equippedPieces,
                            stockTag = maxTier > 0 ? $"最高 {maxTier} 阶" : "待打造",
                            desc = gearSlotDescs[g],
                            actionLabel = "前往穿戴",
                            actionSkin = ForestComicUiSkin.BtnBlue,
                            onAction = () => OpenHeroDetailModal(0, 3),
                        });
                    }
                    break;
                }
            }
        }

        BagEntry MakeResBagEntry(SurvRes res)
        {
            int amt = _sim.Get(res);
            int rarity = GetEconResRarity(res);
            bool capped = ForestSurvivalSim.IsWarehouseCapped(res);
            string stockTag = capped ? $"储量 {amt}/{_sim.StorageCap}" : $"拥有 {FormatEconCount(amt)}";
            string src = _sim.ResolveResourceProducer(res, out _, out _, out _);

            string actLbl = "前往获取";
            Sprite actSkin = ForestComicUiSkin.BtnBlue;
            Action actCb;

            if (res == SurvRes.ChoiceResBox && amt > 0)
            {
                actLbl = "使 用";
                actSkin = ForestComicUiSkin.BtnGreen;
                actCb = OpenChoiceResBoxModal;
            }
            else if (res == SurvRes.SpeedHourglass && amt > 0)
            {
                actLbl = "使 用";
                actSkin = ForestComicUiSkin.BtnGreen;
                actCb = () =>
                {
                    if (_sim.TryUseSpeedHourglass(out string m))
                    {
                        RefreshBagInventoryModal();
                        SpawnScreenCenterBanner(m);
                    }
                    else if (!string.IsNullOrEmpty(m))
                    {
                        SpawnScreenCenterBanner(m);
                    }
                };
            }
            else if (res == SurvRes.RecruitTicket)
            {
                actLbl = amt > 0 ? "去招募" : "前往获取";
                actSkin = amt > 0 ? ForestComicUiSkin.BtnOrange : ForestComicUiSkin.BtnBlue;
                actCb = amt > 0 ? (Action)OpenRadioRecruitModal : OpenStagePosterModal;
            }
            else if (res == SurvRes.HeroExp || res == SurvRes.SkillBook)
            {
                actLbl = amt > 0 ? "去升级" : "闯关获取";
                actCb = amt > 0 ? (Action)(() => OpenHeroModal(1)) : OpenStagePosterModal;
            }
            else if (res == SurvRes.ShardUnivEpic || res == SurvRes.ShardUnivLeg)
            {
                actLbl = amt > 0 ? "去兑换" : "前往获取";
                actCb = amt > 0 ? (Action)(() => OpenHeroDetailModal(0, 1)) : OpenRadioRecruitModal;
            }
            else if (res == SurvRes.Blueprint && amt > 0)
            {
                actLbl = "去升级";
                actCb = () => OpenQuickBuildList(showWarehouse: false);
            }
            else if (capped && amt >= _sim.StorageCap)
            {
                actLbl = "升级仓库";
                actCb = () =>
                {
                    CampNode wh = _sim.Find("warehouse");
                    if (wh != null) OpenBuildConfirmCard(wh);
                };
            }
            else if ((res == SurvRes.LinenShirt || res == SurvRes.LeatherBoots || res == SurvRes.FurCoat || res == SurvRes.PlateArmor) && amt > 0)
            {
                actLbl = "前往穿戴";
                actCb = () => OpenHeroDetailModal(0, 3);
            }
            else if (res == SurvRes.Compost && amt > 0)
            {
                actLbl = "去施肥";
                actCb = () => OpenFarmDrawer();
            }
            else
            {
                actLbl = "前往获取";
                actCb = () =>
                {
                    if (res == SurvRes.Blueprint)
                        OpenSourceRouteForCondition(CondKind.Resource, res);
                    else if (res >= SurvRes.HeroExp)
                        OpenStagePosterModal();
                    else
                        JumpToProduceOrGather(res);
                };
            }

            return new BagEntry
            {
                kind = BagEntryKind.Resource,
                res = res,
                keyId = "res_" + res,
                name = ForestSurvivalSim.ResName(res),
                icon = GetEconResIcon(res),
                rarity0to5 = rarity,
                count = amt,
                isCappedRes = capped,
                stockTag = stockTag,
                desc = $"{GetBagResUsageNote(res)}\n来源：{src}",
                actionLabel = actLbl,
                actionSkin = actSkin,
                onAction = actCb,
            };
        }

        void RefreshBagInventoryModal()
        {
            if (_wwBagCapHeaderTxt != null)
            {
                _wwBagCapHeaderTxt.text =
                    $"仓库容量 {_sim.CurrentMaxSingleStock}/{_sim.StorageCap}   ·   " +
                    $"英雄经验 {FormatEconCount(_sim.Get(SurvRes.HeroExp))}   ·   " +
                    $"招募卡 {_sim.Get(SurvRes.RecruitTicket)}";
            }

            for (int t = 0; t < 4; t++)
                ApplyStoneTabVisual(_wwBagBottomTabImgs[t], _wwBagBottomTabLbls[t], _wwBagBottomTabOutlines[t], t == _wwBagTabIdx);

            // 消耗品自选箱或招募卡有存量时点亮红点
            if (_wwBagBottomTabDots[1] != null)
                _wwBagBottomTabDots[1].SetActive(_sim.Get(SurvRes.ChoiceResBox) > 0 || _sim.Get(SurvRes.SpeedHourglass) > 0);
            bool anySynthShard = false;
            for (int i = 0; i < _sim.Heroes.Count; i++)
                if (!_sim.Heroes[i].unlocked && _sim.Heroes[i].shards >= 10) anySynthShard = true;
            if (_wwBagBottomTabDots[2] != null)
                _wwBagBottomTabDots[2].SetActive(anySynthShard);

            for (int i = 0; i < _wwBagSpawnedTiles.Count; i++)
                SafeDestroy(_wwBagSpawnedTiles[i]);
            _wwBagSpawnedTiles.Clear();

            BuildBagEntriesForTab(_wwBagTabIdx, _wwBagCurrentEntries);

            int selIdx = -1;
            if (!string.IsNullOrEmpty(_wwBagSelectedKey))
            {
                for (int i = 0; i < _wwBagCurrentEntries.Count; i++)
                    if (_wwBagCurrentEntries[i].keyId == _wwBagSelectedKey) { selIdx = i; break; }
            }
            if (selIdx < 0 && _wwBagCurrentEntries.Count > 0)
            {
                selIdx = 0;
                for (int i = 0; i < _wwBagCurrentEntries.Count; i++)
                    if (_wwBagCurrentEntries[i].count > 0) { selIdx = i; break; }
                _wwBagSelectedKey = _wwBagCurrentEntries[selIdx].keyId;
            }

            int rows = Mathf.Max(1, Mathf.CeilToInt(_wwBagCurrentEntries.Count / 4f));
            float contentH = Mathf.Max(756f, 24f + rows * 152f);
            _wwBagGridContentRt.sizeDelta = new Vector2(0f, contentH);
            _wwBagGridContentRt.anchoredPosition = Vector2.zero;

            for (int i = 0; i < _wwBagCurrentEntries.Count; i++)
            {
                BagEntry entry = _wwBagCurrentEntries[i];
                int col = i % 4;
                int row = i / 4;
                float x = -228f + col * 152f;
                float y = -14f - row * 152f;

                Image tile = MakeImg("BagTile_" + entry.keyId, _wwBagGridContentRt,
                    ForestComicUiSkin.GetClippedCard(entry.rarity0to5),
                    entry.count > 0 ? Color.white : new Color(0.82f, 0.85f, 0.92f, 0.82f),
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(x, y), new Vector2(136f, 136f), true);
                _wwBagSpawnedTiles.Add(tile.gameObject);

                string capturedKey = entry.keyId;
                var btn = tile.gameObject.AddComponent<Button>();
                btn.targetGraphic = tile;
                btn.onClick.AddListener(() =>
                {
                    _wwBagSelectedKey = capturedKey;
                    RefreshBagInventoryModal();
                });

                Image ico = ForestUiWidgets.MakeIcon("Ico", tile.transform,
                    entry.icon, new Vector2(0f, 4f), new Vector2(76f, 76f));
                ico.color = entry.count > 0 ? Color.white : new Color(0.58f, 0.62f, 0.72f, 0.82f);

                if (entry.isCappedRes && entry.count >= _sim.StorageCap)
                {
                    Image fullTag = MakeImg("FullTag", tile.transform, ForestComicUiSkin.BtnRed, Color.white,
                        new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -8f), new Vector2(44f, 24f), false);
                    MakeText("Txt", fullTag.transform, "满仓", 12, Color.white, TextAnchor.MiddleCenter,
                        Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);
                }
                else if (entry.kind == BagEntryKind.HeroShard && entry.subId >= 0 && entry.subId < _sim.Heroes.Count
                         && !_sim.Heroes[entry.subId].unlocked && entry.count >= 10)
                {
                    Image synTag = MakeImg("SynTag", tile.transform, ForestComicUiSkin.BtnGreen, Color.white,
                        new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(6f, -8f), new Vector2(58f, 24f), false);
                    MakeText("Txt", synTag.transform, "可合成", 12, Color.white, TextAnchor.MiddleCenter,
                        Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);
                }

                if (i == selIdx)
                {
                    MakeImg("SelBrackets", tile.transform, ForestComicUiSkin.SelectionCornerBrackets, Color.white,
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(154f, 154f), false);
                }

                MakeText("Cnt", tile.transform, FormatEconCount(entry.count), 22, Color.white, TextAnchor.LowerRight,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-10f, 8f), new Vector2(-16f, -12f), outline: true);
            }

            if (selIdx >= 0 && selIdx < _wwBagCurrentEntries.Count)
                BindBagInspector(_wwBagCurrentEntries[selIdx]);
        }

        void BindBagInspector(BagEntry entry)
        {
            _wwBagInspCardImg.sprite = ForestComicUiSkin.GetClippedCard(entry.rarity0to5);
            _wwBagInspIconImg.sprite = entry.icon;
            _wwBagInspCountTxt.text = FormatEconCount(entry.count);
            _wwBagInspNameTxt.text = entry.name;
            _wwBagInspStockTagTxt.text = entry.stockTag;
            _wwBagInspStockTagTxt.color = (entry.isCappedRes && entry.count >= _sim.StorageCap)
                ? (Color)ForestComicUiSkin.AccentRed
                : (Color)ForestComicUiSkin.AccentPurple;
            _wwBagInspDescTxt.text = entry.desc;
            _wwBagActionBtnImg.sprite = entry.actionSkin != null ? entry.actionSkin : ForestComicUiSkin.BtnBlue;
            _wwBagActionBtnLbl.text = entry.actionLabel;
            _wwBagActionCallback = entry.onAction;
        }
    }
}
