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
        // ---------------- 水世界建造/作坊/农牧扩展视图字段 ----------------
        Text _bcCondHeaderTxt;

        Image _itCardFrame;
        Text _itHeaderTxt;
        Image _srCardFrame;

        Image _wsSpecialistFrame, _wsSpecialistBust;
        Text _wsSpecialistTagTxt, _wsLockNoticeTxt;
        Image _wsSelBigFrame;
        Text _wsSelAmtBadgeTxt;
        readonly GameObject[] _wsQueueProgBgs = new GameObject[5];
        readonly Image[] _wsQueueProgFills = new Image[5];
        readonly Image[] _wsQueueLocks = new Image[5];

        const int WsMaxGridRecipes = 10;
        readonly GameObject[] _wsGridSlots = new GameObject[WsMaxGridRecipes];
        readonly RectTransform[] _wsGridSlotRts = new RectTransform[WsMaxGridRecipes];
        readonly Image[] _wsGridFrames = new Image[WsMaxGridRecipes];
        readonly Image[] _wsGridIcons = new Image[WsMaxGridRecipes];
        readonly Image[] _wsGridLocks = new Image[WsMaxGridRecipes];
        readonly Text[] _wsGridBadges = new Text[WsMaxGridRecipes];
        readonly GameObject[] _wsGridBrackets = new GameObject[WsMaxGridRecipes];

        RectTransform _wsReqBox1, _wsReqBox2, _wsReqBox3;

        readonly Image[] _fmPlotIconCards = new Image[4];
        readonly GameObject[] _fmSeedBrackets = new GameObject[4];
        Image _fmSpecBust;

        readonly Image[] _rnPenIconCards = new Image[4];
        readonly GameObject[] _rnBreedBrackets = new GameObject[3];
        Image _rnSpecBust;

        static readonly NodeKind[] WorkshopCycleOrder =
        {
            NodeKind.Sawmill,
            NodeKind.Kitchen,
            NodeKind.Smithy,
            NodeKind.Tailor,
            NodeKind.HerbHut
        };

        // ---------------- 共享水世界无描边排版与装饰小工具（Bw 前缀避免 partial 冲突） ----------------
        static Text BwMakeCleanText(string name, Transform parent, string content, int fontSize, Color color,
            TextAnchor align, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size,
            FontStyle style = FontStyle.Bold)
        {
            Text t = ForestUiWidgets.MakeText(name, parent, content, fontSize, color, align,
                anchorMin, anchorMax, pivot, pos, size, outline: false, style: style);
            var ol = t.GetComponent<Outline>();
            if (ol != null) ol.enabled = false;
            return t;
        }

        static Image BwMakeDivider(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            RectTransform rt = ForestUiWidgets.MakeRect(name, parent, anchorMin, anchorMax, pivot, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = ForestComicUiSkin.DividerLine;
            img.raycastTarget = false;
            return img;
        }

        static Button BwMakeHeaderCloseX(string name, Transform parent, Vector2 pos, UnityAction onClick)
        {
            RectTransform rt = ForestUiWidgets.MakeRect(name, parent, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), pos, new Vector2(52f, 52f));
            var hitImg = rt.gameObject.AddComponent<Image>();
            hitImg.color = new Color(1f, 1f, 1f, 0.01f);
            hitImg.raycastTarget = true;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = hitImg;
            if (onClick != null) btn.onClick.AddListener(onClick);

            Text xTxt = ForestUiWidgets.MakeText("CrossGlyph", rt, "×", 38, ForestComicUiSkin.InkHeaderLight, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);
            var ol = xTxt.GetComponent<Outline>();
            if (ol != null)
            {
                ol.effectColor = new Color32(16, 20, 46, 235);
                ol.effectDistance = new Vector2(2f, -2f);
            }
            return btn;
        }

        static Button BwMakeBackRedBtn(Transform parent, UnityAction onClick, Vector2 pos, Vector2 size)
        {
            Sprite sp = ForestComicUiSkin.GetUiArt("ww_btn_back_red", ForestComicUiSkin.BtnRed);
            RectTransform rt = ForestUiWidgets.MakeRect("BtnWwBackRed", parent, Vector2.zero, Vector2.zero, Vector2.zero, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sp;
            img.preserveAspect = (sp != ForestComicUiSkin.BtnRed);
            img.type = (sp == ForestComicUiSkin.BtnRed) ? Image.Type.Sliced : Image.Type.Simple;
            img.raycastTarget = true;

            if (sp == ForestComicUiSkin.BtnRed)
            {
                ForestUiWidgets.MakeText("Arrow", rt, "←", 34, Color.white, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);
            }

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(onClick);
            return btn;
        }

        static Button BwMakeHeaderSwitchArrow(string name, Transform parent, string arrowTxt, Vector2 pos, UnityAction onClick)
        {
            Image bg = ForestUiWidgets.MakeSlicedImage(name, parent, ForestComicUiSkin.WwSubPanel, new Color(0.86f, 0.90f, 1f, 0.22f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(44f, 42f), true);
            var btn = bg.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            if (onClick != null) btn.onClick.AddListener(onClick);

            ForestUiWidgets.MakeText("Arr", bg.transform, arrowTxt, 24, ForestComicUiSkin.InkHeaderLight, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), Vector2.zero, outline: true);
            return btn;
        }

        static GameObject BwMakeSelectionBrackets(Transform parent, Vector2 expand = default)
        {
            Vector2 pad = expand == default ? new Vector2(10f, 10f) : expand;
            Image br = ForestUiWidgets.MakeSlicedImage("SelBrackets", parent, ForestComicUiSkin.SelectionCornerBrackets, Color.white,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, pad, false);
            return br.gameObject;
        }

        static string BwGetConciseNodeFlavor(CampNode n)
        {
            if (n == null) return "";
            switch (n.kind)
            {
                case NodeKind.Warehouse:
                    return "虽然捡来的物资五花八门，但码放整齐就能囤下整座山寨的家底。";
                case NodeKind.Shelter:
                    return "山寨最高议事中枢，统筹幸存者分工与防线扩建。";
                case NodeKind.HeatTower:
                    return "昼夜不熄的松木暖炉，为更多投奔营地的幸存者驱散山寒。";
                case NodeKind.Dwelling:
                    return "遮风挡雨的原木营房，吸引山林流民安心定居出力。";
                case NodeKind.Sawmill:
                    return "飞转圆锯将原木裁成厚木板与家具，支撑全营基建扩建。";
                case NodeKind.Smithy:
                    return "高温锻炉熔炼废铁，打造铁钉、铁锭与导流管件。";
                case NodeKind.Tailor:
                    return "缝制耐磨衣靴与废土重甲，兼顾居民御寒与小队防护。";
                case NodeKind.Kitchen:
                    return "铁锅慢炖山野鲜材，源源不断供应热腾腾的熟食口粮。";
                case NodeKind.HerbHut:
                    return "研磨深山草药调配敷包热饮，守护营地免受疫病侵扰。";
                case NodeKind.Farm:
                    return "开垦肥沃山垄轮作鲜蔬亚麻与草药，收获保底返种自给自足。";
                case NodeKind.Ranch:
                    return "圈养山林禽兽稳定产出鲜肉毛皮，副产有机堆肥反哺农田。";
                default:
                    if (string.IsNullOrEmpty(n.desc)) return "加固修缮营地设施，稳步提升末日山寨生存实力。";
                    return n.desc.Length > 34 ? n.desc.Substring(0, 33) + "…" : n.desc;
            }
        }

        static Sprite GetNodeBenefitSprite(CampNode n)
        {
            if (n == null) return ForestComicUiSkin.ResIcon(SurvRes.Plank);
            switch (n.kind)
            {
                case NodeKind.HeatTower:
                    return ForestComicUiSkin.GetArt("campfire", ForestComicUiSkin.ResIcon(n.benefitRes));
                case NodeKind.Dwelling:
                    return ForestComicUiSkin.GetArt("bust_cook", ForestComicUiSkin.ResIcon(n.benefitRes));
                case NodeKind.Warehouse:
                    return ForestComicUiSkin.GetArt("camp_warehouse", ForestComicUiSkin.ResIcon(n.benefitRes));
                default:
                    return ForestComicUiSkin.ResIcon(n.benefitRes);
            }
        }

        static int GetNodeCardRarity(CampNode n)
        {
            if (n == null) return 1;
            switch (n.kind)
            {
                case NodeKind.Shelter:
                case NodeKind.Warehouse:
                case NodeKind.HeatTower:
                    return 4; // 橙金核心中枢
                case NodeKind.RelicTruck:
                case NodeKind.RadioPost:
                case NodeKind.Barricade:
                case NodeKind.RelicGenerator:
                    return 3; // 史诗紫战略设施
                case NodeKind.Sawmill:
                case NodeKind.Smithy:
                case NodeKind.Tailor:
                case NodeKind.Kitchen:
                case NodeKind.HerbHut:
                    return 2; // 宝蓝生产作坊
                default:
                    return 1; // 翠绿生态民生
            }
        }

        // =====================================================================
        // 3. 建筑脚下【轻气泡圆钮菜单】
        // =====================================================================
        void BuildFootRingMenu()
        {
            _footMenuRt = ForestUiWidgets.MakeRect("FootRingMenu", _mainHudLayer.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260f, 220f));

            Image titlePill = ForestUiWidgets.MakeSlicedImage("TitlePill", _footMenuRt, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 14f), new Vector2(204f, 36f), false);
            _footTitleTxt = ForestUiWidgets.MakeText("Title", titlePill.transform, "河畔钓鱼台", 18, ForestComicUiSkin.InkHeaderLight, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

            Image ratePill = ForestUiWidgets.MakeSlicedImage("RatePill", _footMenuRt, ForestComicUiSkin.DarkPill, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(232f, 30f), false);
            _footRateTxt = ForestUiWidgets.MakeText("Rate", ratePill.transform, "储水上限 +10", 15, ForestComicUiSkin.AccentGold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

            _footBtnPrimary = CreateCircleActionBtn("PrimaryRingBtn", _footMenuRt, new Vector2(0f, -92f), 84f, out _footIconPrimary, out _footLblPrimary);
            _footBtnSecondary = CreateCircleActionBtn("SecondaryRingBtn", _footMenuRt, new Vector2(92f, -92f), 74f, out _footIconSecondary, out _footLblSecondary);
            _footMenuRt.gameObject.SetActive(false);
        }

        static Button CreateCircleActionBtn(string name, Transform parent, Vector2 pos, float diameter, out Image innerIcon, out Text bottomLabel)
        {
            RectTransform rt = ForestUiWidgets.MakeRect(name, parent,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(diameter, diameter));
            var ringImg = rt.gameObject.AddComponent<Image>();
            ringImg.sprite = ForestComicUiSkin.CircleActionRing;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = ringImg;

            innerIcon = ForestUiWidgets.MakeIcon("Ico", rt, ForestComicUiSkin.GetArt("ico_fish_cast"), new Vector2(0f, 4f), new Vector2(diameter * 0.58f, diameter * 0.58f));
            bottomLabel = ForestUiWidgets.MakeText("Lbl", rt, "钓鱼", 17, Color.white, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 8f), new Vector2(120f, 26f), outline: true);
            return btn;
        }

        // =====================================================================
        // 4. 破框立体【建造 / 升级确认弹窗】（1:1 对标《疯狂水世界》仓库升级弹窗）
        // =====================================================================
        void BuildBreakOutConfirmModal()
        {
            _buildCardModal = MakeModalMask("BuildConfirmModal", 0.62f, CloseAllModals);

            // 636x780 皇家深靛蓝主壳
            Image card = ForestUiWidgets.MakeSlicedImage("Card", _buildCardModal.transform, ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(636f, 780f), true);

            // Header Right (x = +68, y = -48 区域)：粗体亮白标题 + 2行浅薰衣草氛围说明 + 右上角纯白 ✖
            _bcTitleTxt = ForestUiWidgets.MakeText("Title", card.transform, "营地物资仓库", 28, Color.white, TextAnchor.UpperLeft,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(68f, -30f), new Vector2(336f, 40f), outline: true);

            _bcDescTxt = BwMakeCleanText("Desc", card.transform, "虽然捡来的物资五花八门，但码放整齐就能囤下整座山寨的家底。", 17,
                new Color32(214, 222, 248, 255), TextAnchor.UpperLeft,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(68f, -76f), new Vector2(348f, 68f),
                style: FontStyle.Normal);
            _bcDescTxt.lineSpacing = 1.18f;

            BwMakeHeaderCloseX("BtnClose", card.transform, new Vector2(-12f, -12f), CloseAllModals);

            // 清透冷白内衬卡 (596x480，大留白高呼吸感)
            Image sheet = ForestUiWidgets.MakeSlicedImage("LightSheet", card.transform, ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -186f), new Vector2(596f, 480f), false);

            // 左上破框等轴测建筑大图 (200x200，置于 LightSheet 之后使底座与屋顶立体破出外框上沿 ~64px！)
            _bcIllustImg = ForestUiWidgets.MakeIcon("BreakOutArt", card.transform, ForestComicUiSkin.GetArt("camp_warehouse"),
                new Vector2(118f, -36f), new Vector2(200f, 200f), anchor: new Vector2(0f, 1f), raycast: false);

            // 1) 顶部收益提升行 (height 84px)
            RectTransform benRow = ForestUiWidgets.MakeRect("BenefitRow", sheet.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(548f, 84f));
            _bcBenefitIcon = ForestUiWidgets.MakeIcon("BenIco", benRow, ForestComicUiSkin.ResIcon(SurvRes.Plank),
                new Vector2(-242f, 0f), new Vector2(52f, 52f));
            _bcBenefitNameTxt = BwMakeCleanText("BenName", benRow, "原料储量上限", 21, ForestComicUiSkin.InkDark, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0.60f, 1f), new Vector2(0f, 0.5f), new Vector2(68f, 0f), Vector2.zero);
            _bcBenefitFromTxt = BwMakeCleanText("BenFrom", benRow, "+80", 22, ForestComicUiSkin.InkDark, TextAnchor.MiddleRight,
                new Vector2(0.52f, 0f), new Vector2(0.73f, 1f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);
            BwMakeCleanText("BenArrow", benRow, "▶▶", 18, ForestComicUiSkin.InkMuted, TextAnchor.MiddleCenter,
                new Vector2(0.73f, 0f), new Vector2(0.83f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _bcBenefitToTxt = BwMakeCleanText("BenTo", benRow, "+180", 24, ForestComicUiSkin.AccentGreen, TextAnchor.MiddleRight,
                new Vector2(0.82f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-4f, 0f), Vector2.zero);

            // 2) 居中“建造条件 / 升级条件”标题 + 2px 清爽分割线
            _bcCondHeaderTxt = BwMakeCleanText("CondHeader", sheet.transform, "建造条件", 18, ForestComicUiSkin.InkMuted, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(0f, 26f));
            BwMakeDivider("TopCondDivider", sheet.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -126f), new Vector2(548f, 2f));

            // 3) 最多 5 行条件清单 (每行 height 68px，底部分隔 1px DividerLine)
            for (int i = 0; i < 5; i++)
            {
                RectTransform crow = ForestUiWidgets.MakeRect("CostRow_" + i, sheet.transform,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -128f - i * 68f), new Vector2(548f, 68f));
                _bcCostRows[i] = crow.gameObject;

                BwMakeDivider("RowDiv", crow, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 1f));

                Image ico = ForestUiWidgets.MakeIcon("Ico", crow, ForestComicUiSkin.ResIcon(SurvRes.Wood),
                    new Vector2(-244f, 0f), new Vector2(46f, 46f), raycast: true);
                _bcCostIcons[i] = ico;
                _bcCostIconBtns[i] = ico.gameObject.AddComponent<Button>();

                _bcCostTxts[i] = BwMakeCleanText("Amt", crow, "当前人口 9/12", 21, ForestComicUiSkin.InkDark, TextAnchor.MiddleLeft,
                    new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(68f, 0f), new Vector2(246f, 0f));

                // 居中偏右固定列 (x = +50)：满足显示 ww_check_green，不足显示 ww_cross_red
                _bcCostChecks[i] = ForestUiWidgets.MakeIcon("Chk", crow, ForestComicUiSkin.GetUiArt("ww_check_green"),
                    new Vector2(50f, 0f), new Vector2(36f, 36f));

                // 右侧 (x = +196)：126x46 [前往] / [去生产]，仅在条件未满足时显示
                _bcJumpBtns[i] = ForestUiWidgets.MakeJellyButton("BtnJump_" + i, crow, ForestComicUiSkin.BtnBlue, "去生产", 18,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(196f, 0f), new Vector2(126f, 46f), null, out _bcJumpLbls[i]);
            }

            // 4) 底部深靛蓝页脚 (height 114px)
            _bcLvGateRedTxt = ForestUiWidgets.MakeText("LvGateNotice", card.transform, "", 21, new Color32(255, 86, 82, 255), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 56f), new Vector2(580f, 40f), outline: true);

            _bcTimeTxt = ForestUiWidgets.MakeText("BuildDuration", card.transform, "耗时 4秒", 16, ForestComicUiSkin.InkHeaderLight, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 86f), new Vector2(260f, 24f), outline: true);

            _bcConfirmBtn = ForestUiWidgets.MakeJellyButton("BtnConfirmBuild", card.transform, ForestComicUiSkin.BtnBlue, "立即建造", 24,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(256f, 70f), OnConfirmBuildCardClicked, out _bcBtnLbl);

            _buildCardModal.SetActive(false);
        }

        // =====================================================================
        // 4B. 所需产品不足 / 获取途径 居中漫画弹窗（540x420 水世界标准样式）
        // =====================================================================
        void BuildItemAndSourcePopups()
        {
            // 1. 单品缺料/溯源弹窗（540x420 WwHeaderShell + WwLightSheet + 切角品质卡 + BtnGreen [前往获取]）
            _itemTipPopup = MakeModalMask("ItemSourceTipPopup", 0.50f, () => _itemTipPopup.SetActive(false));
            Image tipCard = ForestUiWidgets.MakeSlicedImage("TipCard", _itemTipPopup.transform, ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), new Vector2(540f, 420f), true);

            _itHeaderTxt = ForestUiWidgets.MakeText("Hdr", tipCard.transform, "获取途径", 26, ForestComicUiSkin.InkHeaderLight, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(0f, 42f), outline: true);
            BwMakeHeaderCloseX("BtnCloseTipX", tipCard.transform, new Vector2(-10f, -10f), () => _itemTipPopup.SetActive(false));

            Image tipSheet = ForestUiWidgets.MakeSlicedImage("LightSheet", tipCard.transform, ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(500f, 254f), false);

            _itCardFrame = ForestUiWidgets.MakeSlicedImage("ClippedItemBox", tipSheet.transform, ForestComicUiSkin.ClippedCardBlue, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -20f), new Vector2(96f, 96f), false);
            _itIcon = ForestUiWidgets.MakeIcon("Ico", _itCardFrame.transform, ForestComicUiSkin.ResIcon(SurvRes.Nails), Vector2.zero, new Vector2(68f, 68f));

            _itNameTxt = BwMakeCleanText("Name", tipSheet.transform, "铁钉铆件", 24, ForestComicUiSkin.InkDark, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(140f, -28f), new Vector2(-164f, 34f));
            _itOwnTxt = BwMakeCleanText("Own", tipSheet.transform, "当前拥有：0", 18, ForestComicUiSkin.InkMuted, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(140f, -68f), new Vector2(-164f, 28f));

            BwMakeDivider("TipDiv", tipSheet.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -132f), new Vector2(452f, 1f));

            Image srcBox = ForestUiWidgets.MakeSlicedImage("SrcSubBox", tipSheet.transform, ForestComicUiSkin.WwSubPanel, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(456f, 96f), false);
            BwMakeCleanText("SrcTag", srcBox.transform, "产出与采集来源", 15, ForestComicUiSkin.InkMuted, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(18f, -10f), new Vector2(-36f, 22f));
            _itSourceTxt = BwMakeCleanText("Src", srcBox.transform, "在【废铁锻造坊】中生产获得", 19, ForestComicUiSkin.InkDark, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(1f, 0.72f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(-36f, 0f));

            ForestUiWidgets.MakeJellyButton("BtnGoGet", tipCard.transform, ForestComicUiSkin.BtnGreen, "前往获取", 22,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(228f, 66f), () =>
                {
                    _itemTipPopup.SetActive(false);
                    _itGoAction?.Invoke();
                }, out _);
            _itemTipPopup.SetActive(false);

            // 2. 多途径【获取途径】路由弹窗（针对铜币 / 工程手稿 / 英雄勋章 / 人口）
            _sourceRouteModal = MakeModalMask("SourceRouteModal", 0.56f, () => _sourceRouteModal.SetActive(false));
            Image srCard = ForestUiWidgets.MakeSlicedImage("SrCard", _sourceRouteModal.transform, ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(564f, 572f), true);

            ForestUiWidgets.MakeText("Hdr", srCard.transform, "获取途径", 26, ForestComicUiSkin.InkHeaderLight, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(0f, 42f), outline: true);
            BwMakeHeaderCloseX("BtnCloseSr", srCard.transform, new Vector2(-10f, -10f), () => _sourceRouteModal.SetActive(false));

            Image srPaper = ForestUiWidgets.MakeSlicedImage("LightSheet", srCard.transform, ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(524f, 486f), false);

            _srCardFrame = ForestUiWidgets.MakeSlicedImage("TopClippedCard", srPaper.transform, ForestComicUiSkin.ClippedCardPurple, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -18f), new Vector2(92f, 92f), false);
            _srIcon = ForestUiWidgets.MakeIcon("Ico", _srCardFrame.transform, ForestComicUiSkin.ResIcon(SurvRes.Blueprint), Vector2.zero, new Vector2(66f, 66f));

            _srNameTxt = BwMakeCleanText("Name", srPaper.transform, "工程手稿", 23, ForestComicUiSkin.InkDark, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(132f, -26f), new Vector2(-152f, 34f));
            _srOwnTxt = BwMakeCleanText("Own", srPaper.transform, "当前拥有：0", 17, ForestComicUiSkin.InkMuted, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(132f, -64f), new Vector2(-152f, 28f));

            BwMakeDivider("SrTopDiv", srPaper.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -124f), new Vector2(480f, 1f));

            for (int i = 0; i < 3; i++)
            {
                int rIdx = i;
                Image row = ForestUiWidgets.MakeSlicedImage("Route_" + i, srPaper.transform, ForestComicUiSkin.WwSubPanel, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -138f - i * 112f), new Vector2(484f, 100f), false);
                _srRows[i] = row.gameObject;

                Image rIcoCard = ForestUiWidgets.MakeSlicedImage("IcoCard", row.transform, ForestComicUiSkin.WwLightSheet, Color.white,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(72f, 72f), false);
                _srRowIcons[i] = ForestUiWidgets.MakeIcon("Ico", rIcoCard.transform, ForestComicUiSkin.GetArt("dock_map"), Vector2.zero, new Vector2(54f, 54f));

                _srRowTitles[i] = BwMakeCleanText("Title", row.transform, "丧尸城开车搜刮", 19, ForestComicUiSkin.InkDark, TextAnchor.MiddleLeft,
                    new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-18f, 0f), new Vector2(-240f, -12f));

                _srRowLockTxts[i] = BwMakeCleanText("Lock", row.transform, "5级开启", 18, ForestComicUiSkin.AccentRed, TextAnchor.MiddleRight,
                    new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(140f, 0f));

                _srRowBtns[i] = ForestUiWidgets.MakeJellyButton("BtnGo_" + i, row.transform, ForestComicUiSkin.BtnGreen, "前往获取", 18,
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(118f, 48f), () =>
                    {
                        _sourceRouteModal.SetActive(false);
                        _srRowActions[rIdx]?.Invoke();
                    }, out _);
            }
            _sourceRouteModal.SetActive(false);
        }

        // =====================================================================
        // 5. 半屏五大作坊通用生产队列抽屉（1:1 对标《疯狂水世界》切鱼厂 / 服装作坊）
        // =====================================================================
        void BuildWorkshopQueueDrawer()
        {
            _workshopDrawer = ForestUiWidgets.Stretch("WorkshopDrawerRoot", _canvasRt).gameObject;

            // 上半屏轻透遮罩（保留 630px 实时营地视野，轻触空白处收起抽屉）
            Image topDismiss = ForestUiWidgets.MakeSlicedImage("TopDismiss", _workshopDrawer.transform, null, new Color(0.06f, 0.08f, 0.16f, 0.20f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true);
            topDismiss.gameObject.AddComponent<Button>().onClick.AddListener(CloseAllModals);

            // 抽屉顶心上方悬浮 [升 级] 蓝钮 (y = +688 from bottom)
            _wsUpgradeMiniBtn = ForestUiWidgets.MakeJellyButton("BtnWsFloatingUpgrade", _workshopDrawer.transform, ForestComicUiSkin.BtnBlue, "升 级", 20,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 688f), new Vector2(136f, 48f), () =>
                {
                    CampNode target = _activeWorkshop;
                    CloseAllModals();
                    if (target != null) OpenBuildConfirmCard(target);
                }, out _);

            // 720x650 底部半屏主壳 (WwHeaderShell)
            Image sheet = ForestUiWidgets.MakeSlicedImage("BottomSheet", _workshopDrawer.transform, ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 650f), true);

            // Header (height 64px)：< 左切换 + 居中“锯木木艺坊 1级” + > 右切换 + 右侧委任专家头像小槽
            RectTransform hdrRt = ForestUiWidgets.MakeRect("WsHeader", sheet.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 64f));
            BwMakeHeaderSwitchArrow("BtnPrevWs", hdrRt, "<", new Vector2(-196f, 0f), () => CycleActiveWorkshop(-1));
            _wsTitleTxt = ForestUiWidgets.MakeText("WsTitle", hdrRt, "锯木木艺坊 1级", 26, ForestComicUiSkin.InkHeaderLight, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(328f, 44f), outline: true);
            BwMakeHeaderSwitchArrow("BtnNextWs", hdrRt, ">", new Vector2(196f, 0f), () => CycleActiveWorkshop(1));

            _wsSpecialistFrame = ForestUiWidgets.MakeSlicedImage("SpecialistMiniSlot", hdrRt, ForestComicUiSkin.ClippedCardPurple, Color.white,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(52f, 52f), true);
            _wsSpecialistFrame.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenHeroModal(0));
            _wsSpecialistBust = ForestUiWidgets.MakeIcon("Bust", _wsSpecialistFrame.transform,
                ForestComicUiSkin.GetUiArt("ww_dept_workshop"), new Vector2(0f, 4f), new Vector2(40f, 40f));
            _wsSpecialistTagTxt = ForestUiWidgets.MakeText("Tag", _wsSpecialistFrame.transform, "委任", 12, Color.white, TextAnchor.LowerCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), Vector2.zero, outline: true);

            // 清透冷白主体面板 (692x476)
            Image lightSheet = ForestUiWidgets.MakeSlicedImage("WsLightSheet", sheet.transform, ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(692f, 476f), false);

            // Row 1 — 生产队列槽条 (height 96px，5 格 78x78)
            for (int i = 0; i < 5; i++)
            {
                float x = -192f + i * 96f;
                Image qSlot = ForestUiWidgets.MakeSlicedImage("QueueSlot_" + i, lightSheet.transform, ForestComicUiSkin.WwSubPanel, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -10f), new Vector2(78f, 78f), true);
                _wsQueueSlotBgs[i] = qSlot;
                _wsQueueIcons[i] = ForestUiWidgets.MakeIcon("Ico", qSlot.transform, null, new Vector2(0f, 8f), new Vector2(46f, 46f));
                _wsQueueLocks[i] = ForestUiWidgets.MakeIcon("Lock", qSlot.transform, ForestComicUiSkin.GetArt("ico_lock"),
                    new Vector2(0f, 10f), new Vector2(28f, 28f));

                // 生产中底部绿色进度条
                Image pBg = ForestUiWidgets.MakeSlicedImage("ProgBg", qSlot.transform, ForestComicUiSkin.DarkPill, Color.white,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 5f), new Vector2(68f, 18f), false);
                _wsQueueProgBgs[i] = pBg.gameObject;
                RectTransform pFillRt = ForestUiWidgets.MakeRect("Fill", pBg.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4f, -4f));
                _wsQueueProgFills[i] = pFillRt.gameObject.AddComponent<Image>();
                _wsQueueProgFills[i].sprite = ForestPlaceholderArt.Solid(new Color32(46, 208, 96, 255));
                _wsQueueProgFills[i].type = Image.Type.Filled;
                _wsQueueProgFills[i].fillMethod = Image.FillMethod.Horizontal;

                _wsQueueTimers[i] = BwMakeCleanText("Timer", qSlot.transform, "空闲", 13, ForestComicUiSkin.InkMuted, TextAnchor.LowerCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), Vector2.zero);
                var qOl = _wsQueueTimers[i].gameObject.AddComponent<Outline>();
                qOl.effectColor = new Color32(16, 22, 46, 225);
                qOl.effectDistance = new Vector2(1.2f, -1.2f);
                qOl.enabled = false;

                if (i == 0)
                {
                    qSlot.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                    {
                        if (_activeWorkshop != null && _activeWorkshop.queue.Count > 0)
                            _sim.SpeedUpWorkshopFirstSlot(_activeWorkshop);
                    });
                    Button spBtn = ForestUiWidgets.MakeJellyButton("FreeSpeedUp", qSlot.transform, ForestComicUiSkin.BtnGreen, "秒完", 13,
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(68f, 22f),
                        () => { if (_activeWorkshop != null) _sim.SpeedUpWorkshopFirstSlot(_activeWorkshop); }, out _);
                    _wsSpeedUpPill = spBtn.gameObject;
                }
            }

            BwMakeDivider("QueueDiv", lightSheet.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -98f), new Vector2(656f, 1f));

            // Row 2 — 5列 × 1~2行 切角配方网格 (height 204px，每格 98x98 + 青绿四角选中括号框)
            for (int i = 0; i < WsMaxGridRecipes; i++)
            {
                int rIdx = i;
                int col = i % 5;
                int row = i / 5;
                float x = -244f + col * 122f;
                float y = -106f - row * 102f;

                Image rTile = ForestUiWidgets.MakeSlicedImage("RecipeTile_" + i, lightSheet.transform, ForestComicUiSkin.ClippedCardBlue, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(98f, 98f), true);
                rTile.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                {
                    _selectedRecipeIdx = rIdx;
                    RefreshWorkshopDrawer();
                });

                _wsGridSlots[i] = rTile.gameObject;
                _wsGridSlotRts[i] = rTile.rectTransform;
                _wsGridFrames[i] = rTile;
                _wsGridIcons[i] = ForestUiWidgets.MakeIcon("Ico", rTile.transform, ForestComicUiSkin.ResIcon(SurvRes.Plank),
                    new Vector2(0f, 8f), new Vector2(58f, 58f));

                Image badgePill = ForestUiWidgets.MakeSlicedImage("StockBadge", rTile.transform, ForestComicUiSkin.DarkPill, new Color(1f, 1f, 1f, 0.88f),
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(84f, 22f), false);
                _wsGridBadges[i] = ForestUiWidgets.MakeText("Txt", badgePill.transform, "存:0", 13, Color.white, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

                _wsGridLocks[i] = ForestUiWidgets.MakeIcon("Lock", rTile.transform, ForestComicUiSkin.GetArt("ico_lock"),
                    new Vector2(-28f, 28f), new Vector2(26f, 26f));
                _wsGridBrackets[i] = BwMakeSelectionBrackets(rTile.transform, new Vector2(12f, 12f));

                if (i < 4)
                {
                    _wsRecipeSlots[i] = rTile.gameObject;
                    _wsRecipeFrames[i] = rTile;
                    _wsRecipeIcons[i] = _wsGridIcons[i];
                    _wsRecipeLocks[i] = _wsGridLocks[i];
                    _wsRecipeSubTxts[i] = _wsGridBadges[i];
                }
            }

            // Row 3 — 选中产物详情 & 生产所需原料 (660x146 WwSubPanel)
            Image detail = ForestUiWidgets.MakeSlicedImage("SelDetailPanel", lightSheet.transform, ForestComicUiSkin.WwSubPanel, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(660f, 146f), false);

            _wsSelBigFrame = ForestUiWidgets.MakeSlicedImage("SelOutputCard", detail.transform, ForestComicUiSkin.ClippedCardGreen, Color.white,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(80f, 80f), false);
            _wsSelBigIcon = ForestUiWidgets.MakeIcon("BigIco", _wsSelBigFrame.transform, ForestComicUiSkin.ResIcon(SurvRes.Plank),
                new Vector2(0f, 2f), new Vector2(56f, 56f));
            _wsSelAmtBadgeTxt = ForestUiWidgets.MakeText("Amt", _wsSelBigFrame.transform, "×1", 15, Color.white, TextAnchor.LowerRight,
                Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(-6f, 5f), new Vector2(-8f, -8f), outline: true);

            _wsSelNameTxt = BwMakeCleanText("SelName", detail.transform, "厚木板", 22, ForestComicUiSkin.InkDark, TextAnchor.UpperLeft,
                new Vector2(0f, 0.5f), new Vector2(0.50f, 1f), new Vector2(0f, 1f), new Vector2(112f, -24f), Vector2.zero);
            _wsSelStockTxt = BwMakeCleanText("SelStock", detail.transform, "库存: 0 / 80", 17, ForestComicUiSkin.InkMuted, TextAnchor.LowerLeft,
                new Vector2(0f, 0f), new Vector2(0.50f, 0.5f), new Vector2(0f, 0f), new Vector2(112f, 26f), Vector2.zero);

            BwMakeDivider("DetailMidDiv", detail.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(6f, 0f), new Vector2(1f, 112f));

            BwMakeCleanText("ReqTitle", detail.transform, "生产所需", 17, ForestComicUiSkin.InkMuted, TextAnchor.UpperCenter,
                new Vector2(0.51f, 0.5f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(0f, 24f));

            _wsReqBox1 = BuildIngredientMiniCell("Req1", detail.transform, new Vector2(84f, -10f), out _wsReqIcon1, out _wsReqTxt1, out _wsReqBtn1);
            _wsReqBox2 = BuildIngredientMiniCell("Req2", detail.transform, new Vector2(172f, -10f), out _wsReqIcon2, out _wsReqTxt2, out _wsReqBtn2);
            _wsReqBox3 = BuildIngredientMiniCell("Req3", detail.transform, new Vector2(260f, -10f), out _wsReqIcon3, out _wsReqTxt3, out _wsReqBtn3);

            // Bottom Bar (height 110px)：左下 ww_btn_back_red (96x72) + 居中解锁提示或 ⏱ X秒 + [生 产] (236x68)
            BwMakeBackRedBtn(sheet.transform, CloseAllModals, new Vector2(20f, 18f), new Vector2(96f, 72f));

            _wsLockNoticeTxt = ForestUiWidgets.MakeText("WsLockNotice", sheet.transform, "", 20, new Color32(255, 102, 98, 255), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(24f, 54f), new Vector2(460f, 36f), outline: true);

            _wsTimeTxt = ForestUiWidgets.MakeText("WsDur", sheet.transform, "耗时 4秒", 17, ForestComicUiSkin.InkHeaderLight, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 82f), new Vector2(220f, 24f), outline: true);

            _wsProduceBtn = ForestUiWidgets.MakeJellyButton("BtnProduce", sheet.transform, ForestComicUiSkin.BtnBlue, "生 产", 25,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(236f, 68f), OnWorkshopProduceClicked, out _wsBtnLbl);

            _workshopDrawer.SetActive(false);
        }

        static RectTransform BuildIngredientMiniCell(string name, Transform parent, Vector2 centerOffset, out Image icoOut, out Text txtOut, out Button btnOut)
        {
            Image card = ForestUiWidgets.MakeSlicedImage(name, parent, ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), centerOffset, new Vector2(60f, 60f), true);
            btnOut = card.gameObject.AddComponent<Button>();
            btnOut.targetGraphic = card;

            icoOut = ForestUiWidgets.MakeIcon("Ico", card.transform, ForestComicUiSkin.ResIcon(SurvRes.Wood), Vector2.zero, new Vector2(48f, 48f));
            txtOut = BwMakeCleanText("Amt", card.transform, "0/2", 16, ForestComicUiSkin.InkDark, TextAnchor.UpperCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -3f), new Vector2(86f, 22f));
            return card.rectTransform;
        }

        void CycleActiveWorkshop(int delta)
        {
            var activeList = new List<CampNode>();
            for (int k = 0; k < WorkshopCycleOrder.Length; k++)
            {
                for (int i = 0; i < _sim.Nodes.Count; i++)
                {
                    CampNode n = _sim.Nodes[i];
                    if (n.kind == WorkshopCycleOrder[k] && n.state == NodeState.Active)
                        activeList.Add(n);
                }
            }
            if (activeList.Count <= 1) return;

            int curIdx = activeList.IndexOf(_activeWorkshop);
            if (curIdx < 0) curIdx = 0;
            int nextIdx = (curIdx + delta + activeList.Count) % activeList.Count;
            OpenWorkshopDrawer(activeList[nextIdx], 0);
        }

        // =====================================================================
        // 5B. 【生态选种农田】与【山林畜牧栏】双半屏水世界抽屉
        // =====================================================================
        void BuildFarmAndRanchDrawers()
        {
            // ---------------- A. 生态选种农田半屏抽屉 (_farmDrawer) ----------------
            _farmDrawer = ForestUiWidgets.Stretch("FarmSeedDrawerRoot", _canvasRt).gameObject;
            Image fmDismiss = ForestUiWidgets.MakeSlicedImage("TopDismiss", _farmDrawer.transform, null, new Color(0.06f, 0.08f, 0.16f, 0.20f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true);
            fmDismiss.gameObject.AddComponent<Button>().onClick.AddListener(CloseAllModals);

            // 顶心悬浮 [升 级] 按钮
            _fmUpgradeBtn = ForestUiWidgets.MakeJellyButton("BtnUpFarm", _farmDrawer.transform, ForestComicUiSkin.BtnBlue, "升 级", 20,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 718f), new Vector2(136f, 48f), () =>
                {
                    CampNode fn = _sim.Find("farm_a");
                    CloseAllModals();
                    if (fn != null) OpenBuildConfirmCard(fn);
                }, out _);

            Image fmSheet = ForestUiWidgets.MakeSlicedImage("BottomSheet", _farmDrawer.transform, ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 680f), true);

            RectTransform fmHdr = ForestUiWidgets.MakeRect("FmHeader", fmSheet.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 62f));
            BwMakeHeaderSwitchArrow("PrevToRanch", fmHdr, "<", new Vector2(-216f, 0f), OpenRanchDrawer);
            _fmTitleTxt = ForestUiWidgets.MakeText("Title", fmHdr, "生态选种农田 1级 (2/4垄)", 24, ForestComicUiSkin.InkHeaderLight, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(376f, 40f), outline: true);
            BwMakeHeaderSwitchArrow("NextToRanch", fmHdr, ">", new Vector2(216f, 0f), OpenRanchDrawer);

            Image fmSpecSlot = ForestUiWidgets.MakeSlicedImage("FmSpecSlot", fmHdr, ForestComicUiSkin.ClippedCardGreen, Color.white,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(50f, 50f), true);
            fmSpecSlot.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenHeroModal(0));
            _fmSpecBust = ForestUiWidgets.MakeIcon("Bust", fmSpecSlot.transform, ForestComicUiSkin.GetUiArt("ww_dept_farm"),
                new Vector2(0f, 3f), new Vector2(38f, 38f));
            ForestUiWidgets.MakeText("Tag", fmSpecSlot.transform, "委任", 11, Color.white, TextAnchor.LowerCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), Vector2.zero, outline: true);

            Image fmBody = ForestUiWidgets.MakeSlicedImage("FmLightSheet", fmSheet.transform, ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(692f, 512f), false);

            // 生态联动状态横幅（浅蓝底深字无描边）
            Image synBar = ForestUiWidgets.MakeSlicedImage("SynergyBar", fmBody.transform, ForestComicUiSkin.WwSubPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(664f, 34f), true);
            synBar.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                if (!_sim.HasAutoIrrigation) JumpToNodeById("relic_well");
                else JumpToNodeById("beast_ranch");
            });
            _fmSynergyTxt = BwMakeCleanText("SynTxt", synBar.transform, "", 14, ForestComicUiSkin.AccentPurple, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16f, 0f));

            // 4 块独立田垄卡
            for (int i = 0; i < 4; i++)
            {
                int pIdx = i;
                float x = -249f + i * 166f;
                Image pCard = ForestUiWidgets.MakeSlicedImage("FarmPlot_" + i, fmBody.transform, ForestComicUiSkin.WwSubPanel, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -48f), new Vector2(158f, 214f), false);
                _fmPlotBgs[i] = pCard;

                BwMakeCleanText("Hdr", pCard.transform, $"{i + 1}号田垄", 16, ForestComicUiSkin.InkDark, TextAnchor.UpperCenter,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -7f), new Vector2(0f, 22f));

                _fmPlotIconCards[i] = ForestUiWidgets.MakeSlicedImage("IcoCard", pCard.transform, ForestComicUiSkin.ClippedCardGreen, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(62f, 62f), false);
                _fmPlotIcons[i] = ForestUiWidgets.MakeIcon("Ico", _fmPlotIconCards[i].transform, ForestComicUiSkin.ResIcon(SurvRes.SeedVeggie),
                    Vector2.zero, new Vector2(46f, 46f));

                Image barBg = ForestUiWidgets.MakeSlicedImage("ProgBg", pCard.transform, ForestComicUiSkin.DarkPill, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(138f, 22f), false);
                RectTransform fRt = ForestUiWidgets.MakeRect("Fill", barBg.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6f, -6f));
                _fmPlotProgressFills[i] = fRt.gameObject.AddComponent<Image>();
                _fmPlotProgressFills[i].sprite = ForestPlaceholderArt.Solid(new Color32(52, 202, 88, 240));
                _fmPlotProgressFills[i].type = Image.Type.Filled;
                _fmPlotProgressFills[i].fillMethod = Image.FillMethod.Horizontal;
                _fmPlotStateTxts[i] = ForestUiWidgets.MakeText("St", barBg.transform, "待播种", 12, Color.white, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

                _fmPlotSubBtns[i] = ForestUiWidgets.MakeJellyButton("BtnSub_" + i, pCard.transform, ForestComicUiSkin.BtnOrange, "施肥催熟", 13,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(136f, 32f),
                    () => OnFarmPlotSubAction(pIdx), out _fmPlotSubLbls[i]);

                _fmPlotMainBtns[i] = ForestUiWidgets.MakeJellyButton("BtnMain_" + i, pCard.transform, ForestComicUiSkin.BtnGreen, "播种", 15,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(138f, 36f),
                    () => OnFarmPlotMainAction(pIdx), out _fmPlotMainLbls[i]);
            }

            // 下半部：4 种切角品质种子卡 + 青绿四角选中括号
            Image seedBox = ForestUiWidgets.MakeSlicedImage("SeedSelectorBox", fmBody.transform, ForestComicUiSkin.WwSubPanel, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(664f, 234f), false);

            BwMakeCleanText("SeedHdr", seedBox.transform, "选择播种作物（收获100%保底返种 · 畜牧堆肥翻倍丰产）", 15, ForestComicUiSkin.InkMuted, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-16f, 22f));

            string[] seedShortNames = { "山薯鲜蔬种", "韧性亚麻种", "深山草药种", "变异黄金种" };
            int[] seedRarities = { 1, 2, 3, 4 };
            for (int i = 0; i < 4; i++)
            {
                CropSeedKind sk = (CropSeedKind)i;
                float x = -237f + i * 158f;
                Image sSlot = ForestUiWidgets.MakeSlicedImage("Seed_" + i, seedBox.transform, ForestComicUiSkin.GetClippedCard(seedRarities[i]), Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -36f), new Vector2(144f, 142f), true);
                sSlot.gameObject.AddComponent<Button>().onClick.AddListener(() => { _selectedSeedKind = sk; RefreshFarmDrawer(); });
                _fmSeedFrames[i] = sSlot;

                ForestUiWidgets.MakeIcon("Ico", sSlot.transform, ForestComicUiSkin.ResIcon(ForestSurvivalSim.SeedResFor(sk)),
                    new Vector2(0f, 22f), new Vector2(62f, 62f));
                ForestUiWidgets.MakeText("Nm", sSlot.transform, seedShortNames[i], 15, Color.white, TextAnchor.MiddleCenter,
                    new Vector2(0f, 0f), new Vector2(1f, 0.48f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), Vector2.zero, outline: true);

                Image cntPill = ForestUiWidgets.MakeSlicedImage("CntPill", sSlot.transform, ForestComicUiSkin.DarkPill, new Color(1f, 1f, 1f, 0.86f),
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 7f), new Vector2(124f, 22f), false);
                _fmSeedCntTxts[i] = ForestUiWidgets.MakeText("Cnt", cntPill.transform, "拥有: 3", 13, Color.white, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

                _fmSeedBrackets[i] = BwMakeSelectionBrackets(sSlot.transform, new Vector2(12f, 12f));
            }

            _fmYieldPreviewTxt = BwMakeCleanText("YieldPrev", seedBox.transform, "", 15, ForestComicUiSkin.InkDark, TextAnchor.LowerCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(-24f, 38f));

            // 底部操作栏：左下 ww_btn_back_red + 自动连种 + 一键全播
            BwMakeBackRedBtn(fmSheet.transform, CloseAllModals, new Vector2(20f, 16f), new Vector2(96f, 72f));

            ForestUiWidgets.MakeJellyButton("BtnAutoReplant", fmSheet.transform, ForestComicUiSkin.BtnBlue, "自动连种:开", 16,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-102f, 20f), new Vector2(154f, 54f),
                () => { _sim.AutoReplantFarm = !_sim.AutoReplantFarm; RefreshFarmDrawer(); }, out _fmAutoLbl);

            _fmMainBtn = ForestUiWidgets.MakeJellyButton("BtnFarmBatch", fmSheet.transform, ForestComicUiSkin.BtnGreen, "一键全播", 22,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(116f, 16f), new Vector2(236f, 64f),
                OnFarmBatchActionClicked, out _fmMainBtnLbl);

            _farmDrawer.SetActive(false);

            // ---------------- B. 山林畜牧栏半屏抽屉 (_ranchDrawer) ----------------
            _ranchDrawer = ForestUiWidgets.Stretch("BeastRanchDrawerRoot", _canvasRt).gameObject;
            Image rnDismiss = ForestUiWidgets.MakeSlicedImage("TopDismiss", _ranchDrawer.transform, null, new Color(0.06f, 0.08f, 0.16f, 0.20f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true);
            rnDismiss.gameObject.AddComponent<Button>().onClick.AddListener(CloseAllModals);

            // 顶心悬浮 [升 级] 按钮
            _rnUpgradeBtn = ForestUiWidgets.MakeJellyButton("BtnUpRanch", _ranchDrawer.transform, ForestComicUiSkin.BtnBlue, "升 级", 20,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 718f), new Vector2(136f, 48f), () =>
                {
                    CampNode rn = _sim.Find("beast_ranch");
                    CloseAllModals();
                    if (rn != null) OpenBuildConfirmCard(rn);
                }, out _);

            Image rnSheet = ForestUiWidgets.MakeSlicedImage("BottomSheet", _ranchDrawer.transform, ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 680f), true);

            RectTransform rnHdr = ForestUiWidgets.MakeRect("RnHeader", rnSheet.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 62f));
            BwMakeHeaderSwitchArrow("PrevToFarm", rnHdr, "<", new Vector2(-216f, 0f), () => OpenFarmDrawer());
            _rnTitleTxt = ForestUiWidgets.MakeText("Title", rnHdr, "山林畜牧栏 1级 (2/4栏)", 24, ForestComicUiSkin.InkHeaderLight, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(376f, 40f), outline: true);
            BwMakeHeaderSwitchArrow("NextToFarm", rnHdr, ">", new Vector2(216f, 0f), () => OpenFarmDrawer());

            Image rnSpecSlot = ForestUiWidgets.MakeSlicedImage("RnSpecSlot", rnHdr, ForestComicUiSkin.ClippedCardBlue, Color.white,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(50f, 50f), true);
            rnSpecSlot.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenHeroModal(0));
            _rnSpecBust = ForestUiWidgets.MakeIcon("Bust", rnSpecSlot.transform, ForestComicUiSkin.GetUiArt("ww_dept_farm"),
                new Vector2(0f, 3f), new Vector2(38f, 38f));
            ForestUiWidgets.MakeText("Tag", rnSpecSlot.transform, "委任", 11, Color.white, TextAnchor.LowerCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), Vector2.zero, outline: true);

            Image rnBody = ForestUiWidgets.MakeSlicedImage("RnLightSheet", rnSheet.transform, ForestComicUiSkin.WwLightSheet, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(692f, 512f), false);

            Image rnSynBar = ForestUiWidgets.MakeSlicedImage("SynergyBar", rnBody.transform, ForestComicUiSkin.WwSubPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(664f, 34f), true);
            rnSynBar.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenFarmDrawer(CropSeedKind.Veggie));
            _rnSynergyTxt = BwMakeCleanText("SynTxt", rnSynBar.transform, "", 14, ForestComicUiSkin.AccentPurple, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16f, 0f));

            // 4 个动物饲养栏位卡
            for (int i = 0; i < 4; i++)
            {
                int penIdx = i;
                float x = -249f + i * 166f;
                Image pCard = ForestUiWidgets.MakeSlicedImage("RanchPen_" + i, rnBody.transform, ForestComicUiSkin.WwSubPanel, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -48f), new Vector2(158f, 202f), false);
                _rnPenBgs[i] = pCard;

                BwMakeCleanText("Hdr", pCard.transform, $"{i + 1}号兽栏", 16, ForestComicUiSkin.InkDark, TextAnchor.UpperCenter,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -7f), new Vector2(0f, 22f));

                _rnPenIconCards[i] = ForestUiWidgets.MakeSlicedImage("IcoCard", pCard.transform, ForestComicUiSkin.ClippedCardBlue, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(64f, 64f), false);
                _rnPenIcons[i] = ForestUiWidgets.MakeIcon("Ico", _rnPenIconCards[i].transform, ForestComicUiSkin.ResIcon(SurvRes.Pelt),
                    Vector2.zero, new Vector2(48f, 48f));

                Image barBg = ForestUiWidgets.MakeSlicedImage("ProgBg", pCard.transform, ForestComicUiSkin.DarkPill, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(138f, 22f), false);
                RectTransform fRt = ForestUiWidgets.MakeRect("Fill", barBg.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6f, -6f));
                _rnPenProgressFills[i] = fRt.gameObject.AddComponent<Image>();
                _rnPenProgressFills[i].sprite = ForestPlaceholderArt.Solid(new Color32(236, 146, 42, 240));
                _rnPenProgressFills[i].type = Image.Type.Filled;
                _rnPenProgressFills[i].fillMethod = Image.FillMethod.Horizontal;
                _rnPenStateTxts[i] = ForestUiWidgets.MakeText("St", barBg.transform, "待投喂", 12, Color.white, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

                _rnPenBtns[i] = ForestUiWidgets.MakeJellyButton("BtnAct_" + i, pCard.transform, ForestComicUiSkin.BtnGreen, "投喂", 15,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(138f, 40f),
                    () => OnRanchPenAction(penIdx), out _rnPenBtnLbls[i]);
            }

            // 下半部：3 种切角品质禽兽卡 + 青绿四角选中括号
            Image breedBox = ForestUiWidgets.MakeSlicedImage("BreedSelectorBox", rnBody.transform, ForestComicUiSkin.WwSubPanel, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(664f, 246f), false);

            BwMakeCleanText("BreedHdr", breedBox.transform, "选择饲养品种（每栏消耗：鲜蔬×2 + 净水×1 → 产鲜肉+毛皮+堆肥）", 15, ForestComicUiSkin.InkMuted, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-16f, 22f));

            string[] breedIcons = { "ico_pelt", "boar", "deer" };
            int[] breedRarities = { 1, 2, 3 };
            for (int i = 0; i < 3; i++)
            {
                RanchAnimalKind ak = (RanchAnimalKind)i;
                float x = -208f + i * 208f;
                Image bSlot = ForestUiWidgets.MakeSlicedImage("Breed_" + i, breedBox.transform, ForestComicUiSkin.GetClippedCard(breedRarities[i]), Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -36f), new Vector2(194f, 142f), true);
                bSlot.gameObject.AddComponent<Button>().onClick.AddListener(() => { _selectedRanchAnimal = ak; RefreshRanchDrawer(); });
                _rnBreedFrames[i] = bSlot;

                Sprite bSp = ForestComicUiSkin.GetArt(breedIcons[i], ForestComicUiSkin.ResIcon(i == 1 ? SurvRes.Meat : SurvRes.Pelt));
                ForestUiWidgets.MakeIcon("Ico", bSlot.transform, bSp, new Vector2(0f, 24f), new Vector2(62f, 62f));
                ForestUiWidgets.MakeText("Nm", bSlot.transform, ForestSurvivalSim.RanchAnimalName(ak), 17, Color.white, TextAnchor.MiddleCenter,
                    new Vector2(0f, 0f), new Vector2(1f, 0.48f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), Vector2.zero, outline: true);

                Image subPill = ForestUiWidgets.MakeSlicedImage("SubPill", bSlot.transform, ForestComicUiSkin.DarkPill, new Color(1f, 1f, 1f, 0.88f),
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 7f), new Vector2(174f, 22f), false);
                _rnBreedSubTxts[i] = ForestUiWidgets.MakeText("Sub", subPill.transform, "", 13, Color.white, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outline: true);

                _rnBreedBrackets[i] = BwMakeSelectionBrackets(bSlot.transform, new Vector2(12f, 12f));
            }

            _rnBreedPreviewTxt = BwMakeCleanText("BreedPrev", breedBox.transform, "", 14, ForestComicUiSkin.InkDark, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0.72f, 0f), new Vector2(0f, 0f), new Vector2(16f, 12f), new Vector2(0f, 44f));

            ForestUiWidgets.MakeJellyButton("BtnGoFertilizeFarm", breedBox.transform, ForestComicUiSkin.BtnOrange, "去农田撒肥", 15,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-14f, 14f), new Vector2(150f, 42f),
                () => OpenFarmDrawer(), out _);

            BwMakeBackRedBtn(rnSheet.transform, CloseAllModals, new Vector2(20f, 16f), new Vector2(96f, 72f));

            ForestUiWidgets.MakeJellyButton("BtnAutoFeed", rnSheet.transform, ForestComicUiSkin.BtnBlue, "自动续喂:开", 16,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-102f, 20f), new Vector2(154f, 54f),
                () => { _sim.AutoFeedRanch = !_sim.AutoFeedRanch; RefreshRanchDrawer(); }, out _rnAutoLbl);

            _rnMainBtn = ForestUiWidgets.MakeJellyButton("BtnRanchBatch", rnSheet.transform, ForestComicUiSkin.BtnGreen, "一键全喂", 22,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(116f, 16f), new Vector2(236f, 64f),
                OnRanchBatchActionClicked, out _rnMainBtnLbl);

            _ranchDrawer.SetActive(false);
        }

        // =====================================================================
        // 5C. 【营地基建总览】全列表弹窗（108px 高宽敞 WwLightSheet 卡片 + 切角建筑图标）
        // =====================================================================
        void BuildQuickListModal()
        {
            _quickListModal = MakeModalMask("QuickListModal", 0.62f, CloseAllModals);
            Image card = ForestUiWidgets.MakeSlicedImage("Card", _quickListModal.transform, ForestComicUiSkin.WwHeaderShell, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(656f, 864f), true);

            _qlTitleTxt = ForestUiWidgets.MakeText("Title", card.transform, "营地基建总览", 28, ForestComicUiSkin.InkHeaderLight, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(26f, -16f), new Vector2(-210f, 40f), outline: true);
            _qlSubCapTxt = ForestUiWidgets.MakeText("SubCap", card.transform, "", 15, ForestComicUiSkin.AccentGold, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(26f, -56f), new Vector2(-86f, 26f), outline: true);

            _qlUpgradeWhBtn = ForestUiWidgets.MakeJellyButton("BtnUpWh", card.transform, ForestComicUiSkin.BtnBlue, "升级仓库", 17,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-76f, -18f), new Vector2(118f, 42f), () =>
                {
                    CloseAllModals();
                    CampNode wh = _sim.Find("warehouse");
                    if (wh != null) { SmoothFocusGrid(new Vector2(wh.gx, wh.gy)); OpenBuildConfirmCard(wh); }
                }, out _);

            BwMakeHeaderCloseX("Close", card.transform, new Vector2(-12f, -12f), CloseAllModals);

            Image well = ForestUiWidgets.MakeSlicedImage("ScrollWell", card.transform, ForestComicUiSkin.WwSubPanel, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -94f), new Vector2(616f, 744f), false);
            _qlContentRt = MakeVerticalScrollArea("QuickListScroll", well.transform, Vector2.zero, new Vector2(598f, 724f), 724f);
            _quickListModal.SetActive(false);
        }

        // =====================================================================
        // 交互打开与数据刷新逻辑
        // =====================================================================
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
                _footIconPrimary.sprite = ForestComicUiSkin.GetUiArt("ww_dock_delegate", ForestComicUiSkin.GetArt("dock_heroes"));
                _footLblPrimary.text = "骨干委任";
                _footBtnPrimary.onClick.AddListener(OpenHeroAssignModal);
                if (canUp) BindSecondaryUpgradeFootBtn(n);
            }
            else if (n.kind == NodeKind.Warehouse)
            {
                _footBtnPrimary.GetComponent<RectTransform>().anchoredPosition = new Vector2(canUp ? -52f : 0f, -92f);
                _footIconPrimary.sprite = ForestComicUiSkin.GetUiArt("ww_dock_bag", ForestComicUiSkin.GetArt("dock_bag"));
                _footLblPrimary.text = "查看库存";
                _footBtnPrimary.onClick.AddListener(() => OpenQuickBuildList(showWarehouse: true));
                if (canUp) BindSecondaryUpgradeFootBtn(n);
            }
            else if (n.kind == NodeKind.Barricade)
            {
                _footBtnPrimary.GetComponent<RectTransform>().anchoredPosition = new Vector2(canUp ? -52f : 0f, -92f);
                _footIconPrimary.sprite = ForestComicUiSkin.GetUiArt("ww_dock_battle", ForestComicUiSkin.GetArt("dock_defense"));
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
            string baseTitle = !string.IsNullOrEmpty(n.activeTitle) ? n.activeTitle : n.ruinTitle;
            string dispTitle = !isUpgrade ? baseTitle : $"{baseTitle} Lv.{n.level}";

            _bcIllustImg.sprite = ForestComicUiSkin.NodePreviewSprite(n);
            _bcTitleTxt.text = dispTitle;
            _bcDescTxt.text = BwGetConciseNodeFlavor(n);
            _bcBenefitIcon.sprite = GetNodeBenefitSprite(n);
            _bcBenefitNameTxt.text = string.IsNullOrEmpty(n.benefitLabel) ? "营地综合实力" : n.benefitLabel;
            if (_bcCondHeaderTxt != null)
                _bcCondHeaderTxt.text = isUpgrade ? "升级条件" : "建造条件";

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
                _bcLvGateRedTxt.text = $"玩家等级达到 {reqPlayerLv} 级后解锁建筑等级上限";
            }
            else
            {
                bool canAll = _sim.CanAffordUpgradeAll(n);
                int sec = Mathf.CeilToInt(n.buildDuration * _sim.BuildTimeMultiplier);
                _bcTimeTxt.text = $"耗时 {sec}秒";
                _bcBtnLbl.text = isUpgrade ? "立即升级" : "立即建造";
                _bcConfirmBtn.GetComponent<Image>().sprite = canAll ? ForestComicUiSkin.BtnGreen : ForestComicUiSkin.BtnBlue;
                _bcConfirmBtn.GetComponent<Image>().color = canAll ? Color.white : new Color(0.82f, 0.85f, 0.94f, 0.92f);
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
            _bcCostTxts[idx].color = met ? (Color)ForestComicUiSkin.InkDark : (Color)ForestComicUiSkin.AccentRed;
            _bcCostChecks[idx].sprite = met
                ? ForestComicUiSkin.GetUiArt("ww_check_green")
                : ForestComicUiSkin.GetUiArt("ww_cross_red");
            _bcCostChecks[idx].color = Color.white;
            _bcCostChecks[idx].rectTransform.anchoredPosition = new Vector2(50f, 0f);
            _bcJumpBtns[idx].gameObject.SetActive(!met);

            if (c.kind == CondKind.PopCur)
            {
                _bcCostIcons[idx].sprite = ForestComicUiSkin.GetArt("bust_cook", ForestComicUiSkin.RedDotBadge);
                _bcCostTxts[idx].text = $"当前人口 {c.own}/{c.need}";
                _bcJumpLbls[idx].text = "前往";
                _bcJumpBtns[idx].onClick.AddListener(JumpToBestDwelling);
                _bcCostIconBtns[idx].onClick.AddListener(() => OpenSourceRouteForCondition(CondKind.PopCur, SurvRes.Wood));
            }
            else if (c.kind == CondKind.PopCap)
            {
                _bcCostIcons[idx].sprite = ForestComicUiSkin.GetArt("campfire", ForestComicUiSkin.RedDotBadge);
                _bcCostTxts[idx].text = $"人口上限 {c.own}/{c.need}";
                _bcJumpLbls[idx].text = "前往";
                _bcJumpBtns[idx].onClick.AddListener(JumpToHeatTower);
                _bcCostIconBtns[idx].onClick.AddListener(() => OpenSourceRouteForCondition(CondKind.PopCap, SurvRes.Wood));
            }
            else if (c.kind == CondKind.Coins)
            {
                _bcCostIcons[idx].sprite = ForestComicUiSkin.GetArt("ico_coin", ForestComicUiSkin.RedDotBadge);
                _bcCostTxts[idx].text = $"{c.own}/{c.need}";
                _bcJumpLbls[idx].text = "前往";
                _bcJumpBtns[idx].onClick.AddListener(() => OpenSourceRouteForCondition(CondKind.Coins, SurvRes.Wood));
                _bcCostIconBtns[idx].onClick.AddListener(() => OpenSourceRouteForCondition(CondKind.Coins, SurvRes.Wood));
            }
            else
            {
                SurvRes r = c.res;
                _bcCostIcons[idx].sprite = ForestComicUiSkin.ResIcon(r);
                _bcCostTxts[idx].text = $"{ForestSurvivalSim.ResName(r)} {c.own}/{c.need}";

                _sim.ResolveResourceProducer(r, out _, out _, out bool isCraft);
                if (r == SurvRes.Blueprint)
                {
                    _bcJumpLbls[idx].text = "前往";
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
            int own = _sim.Get(r);
            int rarity = Mathf.Max(1, ForestComicUiSkin.GetResRarity0To5(r));

            if (_itCardFrame != null) _itCardFrame.sprite = ForestComicUiSkin.GetClippedCard(rarity);
            if (_itHeaderTxt != null) _itHeaderTxt.text = own <= 0 ? "所需产品不足" : "获取途径";
            _itIcon.sprite = ForestComicUiSkin.ResIcon(r);
            _itNameTxt.text = ForestSurvivalSim.ResName(r);
            _itOwnTxt.text = $"当前拥有：{own}";
            _itOwnTxt.color = own > 0 ? (Color)ForestComicUiSkin.InkMuted : (Color)ForestComicUiSkin.AccentRed;
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
                if (_srCardFrame != null) _srCardFrame.sprite = ForestComicUiSkin.ClippedCardOrange;
                _srIcon.sprite = ForestComicUiSkin.GetArt("ico_coin", ForestComicUiSkin.RedDotBadge);
                _srNameTxt.text = "营地铜币";
                _srOwnTxt.text = $"当前拥有：{_sim.Coins}";
                ConfigureRouteRow(0, ForestComicUiSkin.GetArt("bust_vil_0", ForestComicUiSkin.GetArt("bust_cook")), "居民生计需求订单（主力稳定产出）", true, "", OpenResidentDemandDrawer);
                ConfigureRouteRow(1, ForestComicUiSkin.GetUiArt("ww_dock_battle", ForestComicUiSkin.GetArt("dock_defense")), "山口防线尸潮迎击 & 巡山清剿", true, "", OpenWavePrepModal);
                ConfigureRouteRow(2, ForestComicUiSkin.GetUiArt("ww_dept_trade", ForestComicUiSkin.GetArt("dock_radio")), "山口行商驿站大宗贸易", _sim.CampLevel >= radioLv, $"{radioLv}级开启", OpenCaravanModal);
            }
            else if (kind == CondKind.PopCur)
            {
                if (_srCardFrame != null) _srCardFrame.sprite = ForestComicUiSkin.ClippedCardBlue;
                _srIcon.sprite = ForestComicUiSkin.GetArt("bust_vil_1", ForestComicUiSkin.RedDotBadge);
                _srNameTxt.text = "当前入驻人口";
                _srOwnTxt.text = $"当前人口：{_sim.CurrentPop} / 上限 {_sim.PopCap}（采集+{Mathf.RoundToInt((_sim.PopulationGatherMultiplier - 1f) * 100f)}%）";
                ConfigureRouteRow(0, ForestComicUiSkin.GetArt("refugee_cabin", ForestComicUiSkin.GetArt("tent")), "升级【1号避风民居】招募流民", true, "", JumpToBestDwelling);
                ConfigureRouteRow(1, ForestComicUiSkin.GetArt("refugee_cabin", ForestComicUiSkin.GetArt("shelter_lv2")), "建造【2号原木民居】扩充人口", _sim.CampLevel >= h2Lv, $"{h2Lv}级开启", () => JumpToNodeById("house_2"));
                ConfigureRouteRow(2, ForestComicUiSkin.GetArt("heating_tower", ForestComicUiSkin.GetArt("campfire")), "升级【篝火供暖塔】提高上限", true, "", JumpToHeatTower);
            }
            else if (kind == CondKind.PopCap)
            {
                if (_srCardFrame != null) _srCardFrame.sprite = ForestComicUiSkin.ClippedCardOrange;
                _srIcon.sprite = ForestComicUiSkin.GetArt("heating_tower", ForestComicUiSkin.RedDotBadge);
                _srNameTxt.text = "供暖人口上限";
                _srOwnTxt.text = $"当前上限：{_sim.PopCap}";
                ConfigureRouteRow(0, ForestComicUiSkin.GetArt("heating_tower", ForestComicUiSkin.GetArt("campfire")), "升级【篝火供暖塔】（核心途径）", true, "", JumpToHeatTower);
                ConfigureRouteRow(1, ForestComicUiSkin.GetArt("shelter_lv2"), "升级【避难所议事厅】", _sim.CampLevel >= shReqLv, $"{shReqLv}级开启", () => JumpToNodeById("shelter"));
                _srRows[2].SetActive(false);
                _sourceRouteModal.SetActive(true);
                return;
            }
            else if (res == SurvRes.HeroMedal)
            {
                if (_srCardFrame != null) _srCardFrame.sprite = ForestComicUiSkin.ClippedCardOrange;
                _srIcon.sprite = ForestComicUiSkin.ResIcon(SurvRes.HeroMedal);
                _srNameTxt.text = "英雄勋章";
                _srOwnTxt.text = $"当前拥有：{_sim.Get(SurvRes.HeroMedal)}（骨干升星材料）";
                ConfigureRouteRow(0, ForestComicUiSkin.GetUiArt("ww_dock_battle", ForestComicUiSkin.GetArt("dock_defense")), "山口防线闯关迎击（主力必掉）", true, "", OpenWavePrepModal);
                ConfigureRouteRow(1, ForestComicUiSkin.GetUiArt("ww_dock_citymap", ForestComicUiSkin.GetArt("dock_map")), "丧尸城击败尸王夺黄金空投箱", _sim.CampLevel >= trLv, $"{trLv}级开启", () => { CloseAllModals(); OpenCityDistrictSelector(); });
                _srRows[2].SetActive(false);
                _sourceRouteModal.SetActive(true);
                return;
            }
            else
            {
                if (_srCardFrame != null) _srCardFrame.sprite = ForestComicUiSkin.ClippedCardPurple;
                _srIcon.sprite = ForestComicUiSkin.ResIcon(SurvRes.Blueprint);
                _srNameTxt.text = "工程手稿";
                _srOwnTxt.text = $"当前拥有：{_sim.Get(SurvRes.Blueprint)}";
                ConfigureRouteRow(0, ForestComicUiSkin.GetUiArt("ww_dock_citymap", ForestComicUiSkin.GetArt("dock_map")), "丧尸城开车搜刮 & 黄金空投箱", _sim.CampLevel >= trLv, $"{trLv}级开启", () => { CloseAllModals(); OpenCityDistrictSelector(); });
                ConfigureRouteRow(1, ForestComicUiSkin.GetUiArt("ww_dock_battle", ForestComicUiSkin.GetArt("dock_defense")), "山口防线尸潮波次迎击", true, "", OpenWavePrepModal);
                ConfigureRouteRow(2, ForestComicUiSkin.GetUiArt("ww_order_gift_box", ForestComicUiSkin.GetArt("dock_radio")), "居民5单礼袋 & 行商驿站兑换", true, "", OpenResidentDemandDrawer);
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
                    SpawnWorldRisingText($"空地已清理！等级达 Lv.{target.unlockPlayerLv} 即可建造【{target.ruinTitle}】", new Vector2(target.gx, target.gy));
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
            _panTarget = new Vector3(iso.x, iso.y - _cam.orthographicSize * 0.36f, -20f);
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
            _panTarget = new Vector3(iso.x, iso.y - _cam.orthographicSize * 0.36f, -20f);
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

        Sprite ResolvePostLeadBust(DelegationPostKind postKind, string fallbackUiKey)
        {
            for (int i = 0; i < _sim.DelegationPosts.Count; i++)
            {
                DelegationPostState st = _sim.DelegationPosts[i];
                if (st.kind != postKind) continue;
                for (int s = 0; s < st.assignedHeroIds.Length; s++)
                {
                    HeroSpec h = _sim.FindHero(st.assignedHeroIds[s]);
                    if (h != null && h.unlocked)
                        return ForestComicUiSkin.GetArt(h.portraitKey, ForestComicUiSkin.GetUiArt(fallbackUiKey));
                }
            }
            return ForestComicUiSkin.GetUiArt(fallbackUiKey);
        }

        void RefreshFarmDrawer()
        {
            CampNode fn = _sim.Find("farm_a");
            if (fn == null) return;
            int cap = _sim.GetFarmPlotCap();
            _fmTitleTxt.text = $"{fn.activeTitle} {fn.level}级 ({cap}/4垄)";
            _fmUpgradeBtn.gameObject.SetActive(fn.level < fn.maxLevel);
            if (_fmSpecBust != null)
                _fmSpecBust.sprite = ResolvePostLeadBust(DelegationPostKind.FarmYield, "ww_dept_farm");

            string irr = _sim.HasAutoIrrigation ? "石井滴灌+35%(已通)" : "滴灌未通(点此升石井)";
            string heat = $"供暖温室+{_sim.GreenhouseBonusPct}%";
            string morale = $"士气+{Mathf.RoundToInt((_sim.CampMoraleMultiplier - 1f) * 100f)}%";
            _fmSynergyTxt.text = $"{irr}  ·  {heat}  ·  {morale}  ·  有机堆肥:{_sim.Get(SurvRes.Compost)}";

            for (int i = 0; i < 4; i++)
            {
                bool unlocked = i < cap;
                if (!unlocked)
                {
                    _fmPlotBgs[i].sprite = ForestComicUiSkin.WwSubPanel;
                    if (_fmPlotIconCards[i] != null) _fmPlotIconCards[i].sprite = ForestComicUiSkin.ClippedCardGrey;
                    _fmPlotIcons[i].sprite = ForestComicUiSkin.GetArt("ico_lock", ForestComicUiSkin.CrossBadge);
                    _fmPlotProgressFills[i].fillAmount = 0f;
                    _fmPlotStateTxts[i].text = i == 2 ? "农田2级解锁" : "农田3级解锁";
                    _fmPlotSubBtns[i].gameObject.SetActive(false);
                    _fmPlotMainBtns[i].image.sprite = ForestComicUiSkin.BtnBlue;
                    _fmPlotMainLbls[i].text = "升级扩垄";
                    continue;
                }

                FarmPlotSlot sl = _sim.FarmPlots[i];
                _fmPlotBgs[i].sprite = sl.phase == FarmPhase.Ripe ? ForestComicUiSkin.SubSlotSelected : ForestComicUiSkin.WwLightSheet;
                SurvRes dispRes = sl.phase == FarmPhase.DryTilled
                    ? ForestSurvivalSim.SeedResFor(_selectedSeedKind)
                    : ForestSurvivalSim.MainCropResFor(sl.seedKind);
                if (_fmPlotIconCards[i] != null)
                    _fmPlotIconCards[i].sprite = ForestComicUiSkin.GetClippedCard(Mathf.Max(1, ForestComicUiSkin.GetResRarity0To5(dispRes)));
                _fmPlotIcons[i].sprite = ForestComicUiSkin.ResIcon(dispRes);

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
                    _fmPlotStateTxts[i].text = sl.fertilized ? "✓ 丰产成熟" : "✓ 可收割";
                    _fmPlotSubBtns[i].gameObject.SetActive(false);
                    _fmPlotMainBtns[i].image.sprite = ForestComicUiSkin.BtnGreen;
                    _fmPlotMainLbls[i].text = "立即收割";
                }
            }

            for (int i = 0; i < 4; i++)
            {
                CropSeedKind sk = (CropSeedKind)i;
                bool sel = sk == _selectedSeedKind;
                if (_fmSeedBrackets[i] != null) _fmSeedBrackets[i].SetActive(sel);
                int own = _sim.Get(ForestSurvivalSim.SeedResFor(sk));
                _fmSeedCntTxts[i].text = own > 0 ? $"库存: {own}" : (sk == CropSeedKind.Golden ? "稀有掉落" : "25币补种");
                _fmSeedCntTxts[i].color = own > 0 ? Color.white : (Color)ForestComicUiSkin.AccentGold;
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
            _rnTitleTxt.text = $"{rn.activeTitle} {rLv}级 ({cap}/4栏)";
            _rnUpgradeBtn.gameObject.SetActive(rLv < rn.maxLevel);
            if (_rnSpecBust != null)
                _rnSpecBust.sprite = ResolvePostLeadBust(DelegationPostKind.FarmYield, "ww_dept_farm");

            string wtTag = _sim.HasWatchtower ? "哨塔良种+1肉皮(已通)" : "哨塔良种未激活";
            _rnSynergyTxt.text = $"农田鲜蔬:{_sim.Get(SurvRes.Veggie)}  ·  石井净水:{_sim.Get(SurvRes.Water)}  ·  {wtTag}  ·  供暖防寒+{_sim.GreenhouseBonusPct}%";

            for (int i = 0; i < 4; i++)
            {
                bool unlocked = i < cap;
                if (!unlocked)
                {
                    _rnPenBgs[i].sprite = ForestComicUiSkin.WwSubPanel;
                    if (_rnPenIconCards[i] != null) _rnPenIconCards[i].sprite = ForestComicUiSkin.ClippedCardGrey;
                    _rnPenIcons[i].sprite = ForestComicUiSkin.GetArt("ico_lock", ForestComicUiSkin.CrossBadge);
                    _rnPenProgressFills[i].fillAmount = 0f;
                    _rnPenStateTxts[i].text = i == 2 ? "兽栏2级解锁" : "兽栏3级解锁";
                    _rnPenBtns[i].image.sprite = ForestComicUiSkin.BtnBlue;
                    _rnPenBtnLbls[i].text = "升级扩栏";
                    continue;
                }

                RanchPenSlot pen = _sim.RanchPens[i];
                _rnPenBgs[i].sprite = pen.readyToCollect ? ForestComicUiSkin.SubSlotSelected : ForestComicUiSkin.WwLightSheet;
                RanchAnimalKind dispA = (pen.feeding || pen.readyToCollect) ? pen.animal : _selectedRanchAnimal;
                string spKey = dispA == RanchAnimalKind.WildBoar ? "boar" : (dispA == RanchAnimalKind.AntlerDeer ? "deer" : "ico_pelt");
                if (_rnPenIconCards[i] != null)
                    _rnPenIconCards[i].sprite = ForestComicUiSkin.GetClippedCard(1 + (int)dispA);
                _rnPenIcons[i].sprite = ForestComicUiSkin.GetArt(spKey, ForestComicUiSkin.ResIcon(SurvRes.Pelt));

                if (pen.readyToCollect)
                {
                    _rnPenProgressFills[i].fillAmount = 1f;
                    _rnPenStateTxts[i].text = $"✓ {ForestSurvivalSim.RanchAnimalName(pen.animal)}已肥";
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

            int[] breedRarities = { 1, 2, 3 };
            for (int i = 0; i < 3; i++)
            {
                RanchAnimalKind ak = (RanchAnimalKind)i;
                int reqLv = ForestSurvivalSim.RanchAnimalUnlockLv(ak);
                bool unl = rLv >= reqLv;
                _rnBreedFrames[i].sprite = unl ? ForestComicUiSkin.GetClippedCard(breedRarities[i]) : ForestComicUiSkin.ClippedCardGrey;
                if (_rnBreedBrackets[i] != null) _rnBreedBrackets[i].SetActive(_selectedRanchAnimal == ak);
                _sim.GetRanchYield(ak, out int m, out int p, out int c);
                _rnBreedSubTxts[i].text = unl ? $"肉+{m} 皮+{p} 肥+{c}" : $"畜牧栏{reqLv}级解锁";
                _rnBreedSubTxts[i].color = unl ? Color.white : (Color)ForestComicUiSkin.AccentGold;
            }

            _sim.GetRanchYield(_selectedRanchAnimal, out int sm, out int sp, out int sc);
            _rnBreedPreviewTxt.text = $"【{ForestSurvivalSim.RanchAnimalName(_selectedRanchAnimal)}】产出：鲜肉+{sm} · 毛皮+{sp} · 堆肥+{sc} (库存:{_sim.Get(SurvRes.Compost)})";
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
            _panTarget = new Vector3(iso.x, iso.y - _cam.orthographicSize * 0.36f, -20f);
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

        public void OpenQuickBuildList(bool showWarehouse)
        {
            if (showWarehouse)
            {
                OpenBagInventoryModal();
                return;
            }

            CloseAllModals();
            for (int i = 0; i < _qlSpawnedRows.Count; i++) SafeDestroy(_qlSpawnedRows[i]);
            _qlSpawnedRows.Clear();

            _qlUpgradeWhBtn.gameObject.SetActive(false);
            _qlSubCapTxt.rectTransform.sizeDelta = new Vector2(-86f, 26f);

            _qlTitleTxt.text = "营地基建总览";
            _qlSubCapTxt.text = $"★玩家等级 Lv.{_sim.CampLevel}/{ForestSurvivalSim.MaxCampLevel}   ·   人口 {_sim.CurrentPop}/{_sim.PopCap}   ·   铜币 {_sim.Coins}";

            var candidates = new List<CampNode>();
            for (int i = 0; i < _sim.Nodes.Count; i++)
            {
                CampNode n = _sim.Nodes[i];
                if (n.kind == NodeKind.RelicLoot || n.state == NodeState.Depleted) continue;
                if (n.state == NodeState.Active && n.level >= n.maxLevel) continue;
                candidates.Add(n);
            }

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

            const float cardH = 108f;
            const float rowPitch = 118f;
            _qlContentRt.sizeDelta = new Vector2(0f, Mathf.Max(724f, 16f + candidates.Count * rowPitch));
            _qlContentRt.anchoredPosition = Vector2.zero;

            for (int i = 0; i < candidates.Count; i++)
            {
                CampNode n = candidates[i];
                CampNode capNode = n;

                // 宽敞 108px WwLightSheet 建筑路线图卡片
                Image row = ForestUiWidgets.MakeSlicedImage("BuildRow_" + n.id, _qlContentRt, ForestComicUiSkin.WwLightSheet, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f - i * rowPitch), new Vector2(582f, cardH), false);
                _qlSpawnedRows.Add(row.gameObject);

                int reqLv = _sim.GetRequiredPlayerLvForNext(n);
                bool lvLocked = _sim.CampLevel < reqLv;
                bool inBuild = n.state == NodeState.Constructing || n.state == NodeState.ReadyToHammer;
                bool hiddenInForest = n.state == NodeState.HiddenInForest;
                bool coveredByTrees = hiddenInForest && (!n.siteUncovered || _world.IsGridCoveredByForest(n.gx, n.gy));
                bool waitingSalvage = hiddenInForest && !coveredByTrees && n.requiresSiteSalvage && !n.siteSalvageCleared;

                Sprite iconFrameSp = lvLocked ? ForestComicUiSkin.ClippedCardGrey : ForestComicUiSkin.GetClippedCard(GetNodeCardRarity(n));
                Image icoBox = ForestUiWidgets.MakeSlicedImage("ClippedBldBox", row.transform, iconFrameSp, Color.white,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(82f, 82f), false);
                Image bldIco = ForestUiWidgets.MakeIcon("Ico", icoBox.transform, ForestComicUiSkin.NodePreviewSprite(n), Vector2.zero, new Vector2(66f, 66f));
                bldIco.color = lvLocked ? new Color(0.75f, 0.78f, 0.86f, 0.92f) : Color.white;

                string dispTitle = n.state == NodeState.Active ? n.activeTitle : (!string.IsNullOrEmpty(n.activeTitle) ? n.activeTitle : n.ruinTitle);
                string stateTag = inBuild
                    ? "施工中"
                    : (coveredByTrees ? "密林探索"
                    : (waitingSalvage ? "待收集"
                    : (n.state == NodeState.Active ? $"Lv.{n.level} → Lv.{n.level + 1}" : "新建筑")));

                BwMakeCleanText("Name", row.transform, $"{dispTitle}  ({stateTag})", 19, ForestComicUiSkin.InkDark, TextAnchor.UpperLeft,
                    new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(110f, -16f), new Vector2(-258f, 0f));

                string subLine;
                Color subCol;
                Sprite btnSkin;
                string btnTxt;
                Action onRowGo;

                if (coveredByTrees)
                {
                    string dir = string.IsNullOrEmpty(n.compassDir) ? "外围" : n.compassDir;
                    subLine = $"★Lv.{reqLv}  ·  藏于{dir}密林，砍树扩地后显露";
                    subCol = ForestComicUiSkin.InkMuted;
                    btnSkin = ForestComicUiSkin.BtnBlue;
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
                    subLine = $"空地已开！收集【{sLbl}】即可建造";
                    subCol = ForestComicUiSkin.AccentGreen;
                    btnSkin = ForestComicUiSkin.BtnBlue;
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
                        ? "施工进行中，点击前往立即加速或敲锤落成"
                        : (lvLocked ? $"需玩家等级 {reqLv} 级开放  ·  {costFmt}" : costFmt);
                    subCol = inBuild
                        ? (Color)ForestComicUiSkin.AccentGreen
                        : (lvLocked ? (Color)ForestComicUiSkin.AccentRed : (canNow ? (Color)ForestComicUiSkin.AccentGreen : (Color)ForestComicUiSkin.InkMuted));
                    btnSkin = lvLocked
                        ? ForestComicUiSkin.BtnGrey
                        : ((canNow || inBuild) ? ForestComicUiSkin.BtnGreen : ForestComicUiSkin.BtnBlue);
                    btnTxt = lvLocked
                        ? $"{reqLv}级解锁"
                        : (inBuild ? "前往落成" : (n.state == NodeState.Active ? "前往升级" : "前往建造"));
                    onRowGo = () => JumpToNodeById(capNode.id);
                }

                BwMakeCleanText("Cost", row.transform, subLine, 14, subCol, TextAnchor.LowerLeft,
                    new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(110f, 16f), new Vector2(-258f, 0f));

                ForestUiWidgets.MakeJellyButton("Go", row.transform, btnSkin,
                    btnTxt, 17, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(128f, 50f),
                    () => onRowGo?.Invoke(), out _);
            }
            _quickListModal.SetActive(true);
        }

        void RefreshWorkshopDrawer()
        {
            if (_activeWorkshop == null) return;
            int slotCap = ForestSurvivalSim.GetWorkshopSlotCap(_activeWorkshop);
            _wsTitleTxt.text = $"{_activeWorkshop.activeTitle} {_activeWorkshop.level}级";
            _wsUpgradeMiniBtn.gameObject.SetActive(_activeWorkshop.level < _activeWorkshop.maxLevel);

            // 刷新右上专家委任槽头像与提速加成
            if (_wsSpecialistBust != null)
            {
                _wsSpecialistBust.sprite = ResolvePostLeadBust(DelegationPostKind.WorkshopSpeed, "ww_dept_workshop");
                int spdPct = Mathf.Max(0, Mathf.RoundToInt((_sim.ProdSpeedMultiplier - 1f) * 100f));
                if (_wsSpecialistTagTxt != null)
                    _wsSpecialistTagTxt.text = spdPct > 0 ? $"+{spdPct}%" : "委任";
            }

            // Row 1：5 格队列槽状态
            for (int i = 0; i < 5; i++)
            {
                bool slotOpen = i < slotCap;
                bool has = slotOpen && i < _activeWorkshop.queue.Count;
                var qTimerOl = _wsQueueTimers[i].GetComponent<Outline>();
                if (qTimerOl != null) qTimerOl.enabled = has;

                if (!slotOpen)
                {
                    _wsQueueSlotBgs[i].sprite = ForestComicUiSkin.WwSubPanel;
                    _wsQueueSlotBgs[i].color = new Color(0.88f, 0.90f, 0.96f, 0.85f);
                    _wsQueueIcons[i].gameObject.SetActive(false);
                    _wsQueueLocks[i].gameObject.SetActive(true);
                    _wsQueueProgBgs[i].SetActive(false);
                    _wsQueueTimers[i].gameObject.SetActive(true);
                    _wsQueueTimers[i].color = ForestComicUiSkin.InkMuted;
                    _wsQueueTimers[i].text = $"Lv.{i - 1}解锁";
                }
                else if (has)
                {
                    QueueItem q = _activeWorkshop.queue[i];
                    _wsQueueSlotBgs[i].sprite = ForestComicUiSkin.ClippedCardBlue;
                    _wsQueueSlotBgs[i].color = Color.white;
                    _wsQueueIcons[i].gameObject.SetActive(true);
                    _wsQueueIcons[i].sprite = ForestComicUiSkin.ResIcon(q.recipe.outRes);
                    _wsQueueLocks[i].gameObject.SetActive(false);
                    _wsQueueProgBgs[i].SetActive(true);
                    float prog = i == 0 ? Mathf.Clamp01(q.timer / Mathf.Max(0.1f, q.duration)) : 0f;
                    _wsQueueProgFills[i].fillAmount = prog;

                    int rem = Mathf.CeilToInt(Mathf.Max(0.1f, q.duration - q.timer));
                    _wsQueueTimers[i].gameObject.SetActive(true);
                    _wsQueueTimers[i].color = Color.white;
                    _wsQueueTimers[i].text = i == 0 ? $"{rem}秒" : "排队";
                }
                else
                {
                    _wsQueueSlotBgs[i].sprite = ForestComicUiSkin.WwSubPanel;
                    _wsQueueSlotBgs[i].color = Color.white;
                    _wsQueueIcons[i].gameObject.SetActive(false);
                    _wsQueueLocks[i].gameObject.SetActive(false);
                    _wsQueueProgBgs[i].SetActive(false);
                    _wsQueueTimers[i].gameObject.SetActive(true);
                    _wsQueueTimers[i].color = ForestComicUiSkin.InkMuted;
                    _wsQueueTimers[i].text = "空闲";
                }
            }
            if (_wsSpeedUpPill != null)
                _wsSpeedUpPill.SetActive(_activeWorkshop.queue.Count > 0);

            // Row 2：5列 × 1~2行 切角配方网格
            WorkshopRecipe[] recipes = _sim.GetWorkshopRecipes(_activeWorkshop);
            bool singleRow = recipes.Length <= 5;
            for (int i = 0; i < WsMaxGridRecipes; i++)
            {
                bool exist = i < recipes.Length;
                _wsGridSlots[i].SetActive(exist);
                if (!exist) continue;

                int col = i % 5;
                int row = i / 5;
                float spacing = singleRow ? (recipes.Length <= 4 ? 142f : 122f) : 122f;
                float startX = singleRow ? -(recipes.Length - 1) * spacing * 0.5f : -244f;
                float x = startX + col * spacing;
                float y = singleRow ? -155f : (-106f - row * 102f);
                _wsGridSlotRts[i].anchoredPosition = new Vector2(x, y);

                WorkshopRecipe r = recipes[i];
                bool unlocked = _sim.IsRecipeUnlocked(_activeWorkshop, r);
                int rarity = Mathf.Max(1, ForestComicUiSkin.GetResRarity0To5(r.outRes));

                _wsGridFrames[i].sprite = unlocked ? ForestComicUiSkin.GetClippedCard(rarity) : ForestComicUiSkin.ClippedCardGrey;
                _wsGridFrames[i].color = unlocked ? Color.white : new Color(0.78f, 0.81f, 0.88f, 0.94f);
                _wsGridIcons[i].sprite = ForestComicUiSkin.ResIcon(r.outRes);
                _wsGridIcons[i].color = unlocked ? Color.white : new Color(0.16f, 0.20f, 0.32f, 0.78f);
                _wsGridLocks[i].gameObject.SetActive(!unlocked);
                _wsGridBrackets[i].SetActive(i == _selectedRecipeIdx);

                if (unlocked)
                {
                    _wsGridBadges[i].text = $"存:{_sim.Get(r.outRes)}";
                    _wsGridBadges[i].color = Color.white;
                }
                else
                {
                    _wsGridBadges[i].text = _activeWorkshop.level < r.unlockBuildingLv
                        ? $"{r.unlockBuildingLv}级解锁"
                        : $"玩家{r.unlockCampLv}级";
                    _wsGridBadges[i].color = new Color32(255, 214, 148, 255);
                }
            }

            // Row 3 & Bottom Bar：选中产物详情、原料溯源与生产按钮
            if (_selectedRecipeIdx >= 0 && _selectedRecipeIdx < recipes.Length)
            {
                WorkshopRecipe sel = recipes[_selectedRecipeIdx];
                bool unlocked = _sim.IsRecipeUnlocked(_activeWorkshop, sel);
                bool canAff = unlocked && _sim.CanAffordRecipe(sel);
                int selRarity = Mathf.Max(1, ForestComicUiSkin.GetResRarity0To5(sel.outRes));

                if (_wsSelBigFrame != null)
                    _wsSelBigFrame.sprite = unlocked ? ForestComicUiSkin.GetClippedCard(selRarity) : ForestComicUiSkin.ClippedCardGrey;
                _wsSelBigIcon.sprite = ForestComicUiSkin.ResIcon(sel.outRes);
                _wsSelBigIcon.color = unlocked ? Color.white : new Color(0.22f, 0.26f, 0.38f, 0.80f);
                if (_wsSelAmtBadgeTxt != null)
                    _wsSelAmtBadgeTxt.text = $"×{sel.outAmt}";

                string ratTag = sel.bonusRation > 0 ? $" (+口粮{sel.bonusRation})" : "";
                _wsSelNameTxt.text = $"{sel.name}{ratTag}";

                int curStock = _sim.Get(sel.outRes);
                bool capped = ForestSurvivalSim.IsWarehouseCapped(sel.outRes);
                _wsSelStockTxt.text = capped ? $"库存: {curStock} / {_sim.StorageCap}" : $"库存: {curStock}";
                _wsSelStockTxt.color = (capped && curStock >= _sim.StorageCap)
                    ? (Color)ForestComicUiSkin.AccentRed
                    : (Color)ForestComicUiSkin.InkMuted;

                // 动态居中排布 1~3 种生产所需原料
                int reqCount = 1 + (sel.inAmt2 > 0 ? 1 : 0) + (sel.inAmt3 > 0 ? 1 : 0);
                if (reqCount == 1)
                {
                    _wsReqBox1.anchoredPosition = new Vector2(170f, -10f);
                }
                else if (reqCount == 2)
                {
                    _wsReqBox1.anchoredPosition = new Vector2(122f, -10f);
                    _wsReqBox2.anchoredPosition = new Vector2(218f, -10f);
                }
                else
                {
                    _wsReqBox1.anchoredPosition = new Vector2(82f, -10f);
                    _wsReqBox2.anchoredPosition = new Vector2(170f, -10f);
                    _wsReqBox3.anchoredPosition = new Vector2(258f, -10f);
                }

                BindWorkshopIngredient(_wsReqBox1.gameObject, _wsReqIcon1, _wsReqTxt1, _wsReqBtn1, sel.inRes1, sel.inAmt1, true);
                BindWorkshopIngredient(_wsReqBox2.gameObject, _wsReqIcon2, _wsReqTxt2, _wsReqBtn2, sel.inRes2, sel.inAmt2, sel.inAmt2 > 0);
                BindWorkshopIngredient(_wsReqBox3.gameObject, _wsReqIcon3, _wsReqTxt3, _wsReqBtn3, sel.inRes3, sel.inAmt3, sel.inAmt3 > 0);

                if (!unlocked)
                {
                    _wsProduceBtn.gameObject.SetActive(false);
                    _wsTimeTxt.gameObject.SetActive(false);
                    if (_wsLockNoticeTxt != null)
                    {
                        _wsLockNoticeTxt.gameObject.SetActive(true);
                        _wsLockNoticeTxt.text = _activeWorkshop.level < sel.unlockBuildingLv
                            ? $"建筑等级达到 {sel.unlockBuildingLv} 级解锁"
                            : $"玩家等级达到 {sel.unlockCampLv} 级解锁";
                    }
                }
                else
                {
                    if (_wsLockNoticeTxt != null) _wsLockNoticeTxt.gameObject.SetActive(false);
                    _wsProduceBtn.gameObject.SetActive(true);
                    _wsTimeTxt.gameObject.SetActive(true);

                    float dur = Mathf.Max(1.4f, sel.duration / _sim.ProdSpeedMultiplier);
                    _wsTimeTxt.text = $"耗时 {Mathf.CeilToInt(dur)}秒";
                    bool canProd = canAff && _activeWorkshop.queue.Count < slotCap;
                    _wsProduceBtn.GetComponent<Image>().sprite = canProd ? ForestComicUiSkin.BtnBlue : ForestComicUiSkin.BtnGrey;
                    _wsBtnLbl.text = "生 产";
                }
            }
        }

        void BindWorkshopIngredient(GameObject boxGo, Image ico, Text txt, Button btn, SurvRes res, int need, bool active)
        {
            boxGo.SetActive(active);
            if (!active) return;
            ico.sprite = ForestComicUiSkin.ResIcon(res);
            int own = _sim.Get(res);
            txt.text = $"{own}/{need}";
            txt.color = own >= need ? (Color)ForestComicUiSkin.InkDark : (Color)ForestComicUiSkin.AccentRed;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OpenItemSourceTip(res));
        }
    }
}
