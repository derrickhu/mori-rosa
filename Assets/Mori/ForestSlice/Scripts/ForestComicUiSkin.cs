using System.Collections.Generic;
using UnityEngine;

namespace Mori.ForestSlice
{
    /// <summary>
    /// 《疯狂水世界》级商业漫画风 UI 设计系统与图集加载器：
    /// 1. 皇家靛蓝深色外框 (#2B3566 -> #212952 + 3px #11162E 漫画墨线) + 清透冷白/薰衣草内衬 (#EAEEF9 -> #E1E7F6 / #EEF1FA)
    /// 2. 水世界专属原语：切角品质卡 (ClippedCard 0..5)、青绿发光四角选中框 (SelectionCornerBrackets)、断线漫画道具内框 (WwBrokenFrame)、
    ///    石质底托与紫/灰页签 (WwStoneTray / WwTabActive / WwTabIdle)、斜切品质绶带 (RarityRibbon)、斑马属性条 (WwZebraRow)
    /// 3. 统一检索 Resources/ForestArt/ 下的 ww_* 新美术与既有手绘资源，缺失时自动回退至高精度抗锯齿程序化漫画精灵。
    /// </summary>
    public static class ForestComicUiSkin
    {
        static bool _ready;

        // ---------------- 水世界高对比可读性色板常量 ----------------
        public static readonly Color32 InkDark = new Color32(34, 41, 68, 255);          // 浅色底卡主文字（深靛蓝黑，无需脏描边）
        public static readonly Color32 InkMuted = new Color32(96, 108, 142, 255);       // 浅色底卡次级说明文字
        public static readonly Color32 AccentGreen = new Color32(34, 178, 76, 255);     // +800 / 材料充足绿字
        public static readonly Color32 AccentRed = new Color32(228, 58, 64, 255);       // 0/2 / 条件不足红字
        public static readonly Color32 AccentPurple = new Color32(107, 92, 231, 255);   // 品质/星级/页签高亮紫
        public static readonly Color32 AccentGold = new Color32(255, 212, 76, 255);     // 战力/玩家等级高亮金
        public static readonly Color32 InkHeaderLight = new Color32(246, 248, 255, 255);// 深靛蓝顶栏亮白标题字
        public static readonly Color32 DividerLine = new Color32(184, 195, 226, 255);   // #B8C3E2 清爽内部分割线

        // ---------------- 既有通用属性（全量保留兼容 + 升级水世界色板） ----------------
        public static Sprite DialogOuter { get; private set; }      // 皇家深靛蓝漫画墨线弹窗外框 (#2B3566 -> #212952)
        public static Sprite ParchmentPanel { get; private set; }   // 清透冷白/薰衣草内衬面板 (#EAEEF9 -> #E1E7F6)
        public static Sprite SubSlot { get; private set; }          // 浅蓝灰内凹槽位底板
        public static Sprite SubSlotSelected { get; private set; }  // 青薄荷绿高亮选中槽
        public static Sprite DarkPill { get; private set; }         // 左上HUD/资源栈深海靛蓝半透明胶囊
        public static Sprite QuestCard { get; private set; }        // 左下浮动主线任务卡（进行中）
        public static Sprite QuestCardDone { get; private set; }    // 左下浮动主线任务卡（已完成绿光底）
        public static Sprite SpeechBubble { get; private set; }     // 头顶冷白底深墨边漫画需求/台词气泡

        public static Sprite BtnBlue { get; private set; }          // 升级 / 前往 / 去生产 / 使用 立体漫画蓝钮
        public static Sprite BtnGreen { get; private set; }         // 满足 / 领取 / 可建造 立体漫画绿钮
        public static Sprite BtnOrange { get; private set; }        // 加速 / 出发搜刮 立体漫画橙黄钮
        public static Sprite BtnRed { get; private set; }           // 一键卸任 / 关闭 立体漫画玫红钮
        public static Sprite BtnGrey { get; private set; }          // 置灰不可点冷灰立体钮

        public static Sprite CircleActionRing { get; private set; } // 建筑脚下 / 甩杆收线大圆钮底托
        public static Sprite RedDotBadge { get; private set; }      // 醒目小红点提示
        public static Sprite CrossBadge { get; private set; }       // 条件未满足红叉 ✘
        public static Sprite CheckBadge { get; private set; }       // 条件已满足绿勾 ✔
        public static Sprite JoystickRing { get; private set; }     // 虚拟摇杆底座环
        public static Sprite JoystickThumb { get; private set; }    // 虚拟摇杆摇球
        public static Sprite ArcBarBg { get; private set; }         // 钓鱼竖向弧形张力槽底框
        public static Sprite ArcBarFill { get; private set; }       // 钓鱼竖向弧形张力槽绿条
        public static Sprite Sunburst { get; private set; }         // 恭喜获得 / 新英雄放射光效
        public static Sprite WhiteSoftCircle { get; private set; }  // 波纹点击圈

        // ---------------- 《疯狂水世界》专属新增 UI 原语 ----------------
        public static Sprite WwHeaderShell { get; private set; }    // 深皇家靛蓝半屏抽屉/弹窗主壳 (#2A3468)
        public static Sprite WwLightSheet { get; private set; }     // 清爽冷白内衬卡 (#EEF1FA + #B8C3E2 细描边)
        public static Sprite WwSubPanel { get; private set; }       // 柔和长春花蓝灰内嵌框 (#DCE3F5，居民需求/委任部门/属性底框)
        public static Sprite WwZebraRow { get; private set; }       // 英雄6维属性表交替浅蓝横条 (#D4DDF2)
        public static Sprite WwBrokenFrame { get; private set; }    // 道具背包全屏双层断线漫画框（对标参考图6/7）
        public static Sprite WwStoneTray { get; private set; }      // 右下角石质/金属凹槽页签托盘 (#B8B7C4 + #43424F 墨线)
        public static Sprite WwTabActive { get; private set; }      // 激活态紫靛蓝带纯白内框方页签 (#6B5CE7)
        public static Sprite WwTabIdle { get; private set; }        // 常态浅石灰带四角铆钉方页签 (#D8D9E4)
        public static Sprite WwDockSlate { get; private set; }      // 底部主界面漫画石板导航底砖 (#C4CADB -> #9EA4B8)

        // 右上 45° 切角漫画品质方卡 (0=灰, 1=绿, 2=蓝, 3=紫, 4=橙, 5=红)
        public static Sprite ClippedCardGrey { get; private set; }
        public static Sprite ClippedCardGreen { get; private set; }
        public static Sprite ClippedCardBlue { get; private set; }
        public static Sprite ClippedCardPurple { get; private set; }
        public static Sprite ClippedCardOrange { get; private set; }
        public static Sprite ClippedCardRed { get; private set; }

        // 青薄荷绿发光四角选中括号框 [ ]（9-slice border: 24，对标道具/切鱼厂选中态）
        public static Sprite SelectionCornerBrackets { get; private set; }

        static Sprite[] _clippedCardsByRarity;
        static Sprite[] _rarityRibbons;

        static Font _font;
        static readonly Dictionary<string, Sprite> UiSpriteCache = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Sprite> ProcWwFallbackCache = new Dictionary<string, Sprite>();
        static readonly Dictionary<SurvRes, Sprite> ProcResIcons = new Dictionary<SurvRes, Sprite>();

        public static Font UiFont
        {
            get
            {
                if (_font != null) return _font;
                _font = Resources.Load<Font>("Fonts/MoriBold");
                if (_font != null) return _font;
                string[] candidates = { "PingFang SC", "Heiti SC", "Hiragino Sans GB", "Microsoft YaHei", "Arial Unicode MS", "Arial" };
                _font = Font.CreateDynamicFontFromOSFont(candidates, 24);
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return _font;
            }
        }

        public static void ApplyRuntimeFont(Font runtimeFont)
        {
            if (runtimeFont == null) return;
            _font = runtimeFont;
            var allTexts = Resources.FindObjectsOfTypeAll<UnityEngine.UI.Text>();
            for (int i = 0; i < allTexts.Length; i++)
            {
                if (allTexts[i] != null && allTexts[i].gameObject.scene.IsValid())
                {
                    allTexts[i].font = runtimeFont;
                    allTexts[i].SetAllDirty();
                }
            }
        }

        public static void Ensure()
        {
            if (_ready && DialogOuter != null && WwHeaderShell != null && ClippedCardBlue != null) return;
            _ready = true;

            // 1. 皇家深靛蓝主外框 (#2B3566 -> #212952 + 3px #11162E 墨线 + #4A5BA3 顶高光)
            DialogOuter = Make9SliceBevel(
                96, 18f, 3.0f, 5f,
                new Color32(43, 53, 102, 254),
                new Color32(33, 41, 82, 254),
                new Color32(17, 22, 46, 255),
                new Color32(74, 91, 163, 195),
                new Color32(20, 26, 56, 255),
                border: 24);

            WwHeaderShell = Make9SliceBevel(
                96, 18f, 3.0f, 4f,
                new Color32(46, 57, 112, 255),
                new Color32(36, 45, 92, 255),
                new Color32(17, 22, 46, 255),
                new Color32(82, 100, 178, 190),
                new Color32(22, 28, 62, 255),
                border: 24);

            // 2. 清透冷白/薰衣草内衬板 (#EAEEF9 -> #E1E7F6 + #5F6EA8 边框) & 轻量白卡 (#EEF1FA + #B8C3E2 细边)
            ParchmentPanel = Make9SliceBevel(
                80, 14f, 2.2f, 2f,
                new Color32(234, 238, 249, 255),
                new Color32(225, 231, 246, 255),
                new Color32(95, 110, 168, 235),
                new Color32(255, 255, 255, 225),
                new Color32(198, 208, 234, 255),
                border: 20);

            WwLightSheet = Make9SliceBevel(
                80, 14f, 2.0f, 2f,
                new Color32(242, 244, 252, 255),
                new Color32(235, 239, 250, 255),
                new Color32(184, 195, 226, 255),
                new Color32(255, 255, 255, 235),
                new Color32(208, 217, 238, 255),
                border: 20);

            // 3. 长春花蓝灰内嵌子面板 & 槽位 & 斑马条
            WwSubPanel = Make9SliceBevel(
                72, 12f, 2.0f, 2f,
                new Color32(223, 230, 246, 255),
                new Color32(214, 223, 243, 255),
                new Color32(152, 168, 214, 245),
                new Color32(246, 249, 255, 175),
                new Color32(192, 204, 232, 255),
                border: 18);

            SubSlot = Make9SliceBevel(
                64, 11f, 2.0f, 2f,
                new Color32(224, 231, 247, 255),
                new Color32(212, 221, 242, 255),
                new Color32(134, 150, 198, 235),
                new Color32(248, 250, 255, 180),
                new Color32(188, 200, 228, 255),
                border: 16);

            SubSlotSelected = Make9SliceBevel(
                64, 11f, 3.2f, 2f,
                new Color32(232, 255, 248, 255),
                new Color32(206, 250, 238, 255),
                new Color32(46, 226, 180, 255),
                new Color32(255, 255, 255, 230),
                new Color32(118, 232, 200, 255),
                border: 16);

            WwZebraRow = MakeZebraStripe(48, 8f, new Color32(212, 221, 242, 255), border: 10);

            // 4. 道具背包双层断线漫画框 + 底部石质托盘 + 紫/灰方页签 + 主界面石板底砖
            WwBrokenFrame = MakeBrokenComicSheet(128, border: 28);

            WwStoneTray = Make9SliceBevel(
                84, 16f, 3.0f, 4.5f,
                new Color32(194, 193, 206, 255),
                new Color32(166, 165, 179, 255),
                new Color32(67, 66, 79, 255),
                new Color32(232, 231, 240, 210),
                new Color32(116, 115, 130, 255),
                border: 22);

            WwTabActive = MakeActivePurpleTab(76, border: 18);
            WwTabIdle = MakeIdleStoneTab(76, border: 18);

            WwDockSlate = Make9SliceBevel(
                84, 14f, 3.0f, 4.5f,
                new Color32(196, 202, 219, 255),
                new Color32(158, 164, 184, 255),
                new Color32(20, 24, 42, 255),
                new Color32(230, 235, 246, 210),
                new Color32(108, 115, 136, 255),
                border: 20);

            // 5. 半透明深海靛蓝 HUD 胶囊与主线任务浮窗
            DarkPill = Make9SliceBevel(
                64, 16f, 2.2f, 2f,
                new Color32(28, 37, 74, 224),
                new Color32(18, 24, 52, 232),
                new Color32(10, 14, 32, 245),
                new Color32(92, 114, 192, 115),
                new Color32(12, 16, 36, 238),
                border: 18);

            QuestCard = Make9SliceBevel(
                72, 15f, 2.6f, 3f,
                new Color32(26, 38, 68, 236),
                new Color32(18, 26, 48, 242),
                new Color32(10, 15, 30, 252),
                new Color32(84, 128, 196, 135),
                new Color32(12, 18, 34, 248),
                border: 20);

            QuestCardDone = Make9SliceBevel(
                72, 15f, 2.8f, 3f,
                new Color32(42, 178, 102, 244),
                new Color32(26, 128, 72, 248),
                new Color32(12, 42, 24, 255),
                new Color32(156, 250, 192, 190),
                new Color32(18, 86, 48, 255),
                border: 20);

            SpeechBubble = MakeSpeechBubbleTex(96, 68);

            // 6. 水世界商业级立体漫画果冻按钮（3px 深靛黑墨线 + 顶反光 + 底厚唇）
            BtnBlue = MakeJellyButton(80, 15f, new Color32(86, 154, 255, 255), new Color32(52, 116, 240, 255), new Color32(28, 72, 168, 255));
            BtnGreen = MakeJellyButton(80, 15f, new Color32(82, 224, 118, 255), new Color32(40, 182, 80, 255), new Color32(20, 116, 48, 255));
            BtnOrange = MakeJellyButton(80, 15f, new Color32(255, 186, 66, 255), new Color32(238, 132, 28, 255), new Color32(162, 78, 10, 255));
            BtnRed = MakeJellyButton(80, 15f, new Color32(255, 84, 100, 255), new Color32(228, 46, 66, 255), new Color32(154, 22, 38, 255));
            BtnGrey = MakeJellyButton(80, 15f, new Color32(174, 182, 202, 255), new Color32(134, 142, 164, 255), new Color32(86, 94, 116, 255));

            // 7. 6 档右上 45° 切角品质方卡 (128x128)
            ClippedCardGrey = MakeClippedRarityCard(128, new Color32(184, 194, 214, 255), new Color32(146, 156, 180, 255), new Color32(94, 104, 126, 255));
            ClippedCardGreen = MakeClippedRarityCard(128, new Color32(94, 240, 152, 255), new Color32(52, 204, 114, 255), new Color32(26, 138, 72, 255));
            ClippedCardBlue = MakeClippedRarityCard(128, new Color32(104, 164, 255, 255), new Color32(64, 124, 245, 255), new Color32(34, 76, 168, 255));
            ClippedCardPurple = MakeClippedRarityCard(128, new Color32(224, 118, 255, 255), new Color32(184, 70, 242, 255), new Color32(118, 34, 166, 255));
            ClippedCardOrange = MakeClippedRarityCard(128, new Color32(255, 180, 82, 255), new Color32(242, 136, 42, 255), new Color32(168, 78, 14, 255));
            ClippedCardRed = MakeClippedRarityCard(128, new Color32(255, 108, 118, 255), new Color32(232, 62, 76, 255), new Color32(156, 28, 38, 255));

            _clippedCardsByRarity = new[]
            {
                ClippedCardGrey,
                ClippedCardGreen,
                ClippedCardBlue,
                ClippedCardPurple,
                ClippedCardOrange,
                ClippedCardRed
            };

            // 8. 青薄荷发光四角选中括号框 & 品质斜切绶带
            SelectionCornerBrackets = MakeSelectionBrackets(128, border: 24);

            _rarityRibbons = new[]
            {
                MakeRarityRibbonSprite(168, 44, new Color32(168, 178, 202, 255), new Color32(122, 132, 158, 255)), // 0: 灰
                MakeRarityRibbonSprite(168, 44, new Color32(76, 224, 138, 255), new Color32(38, 176, 96, 255)),   // 1: 绿
                MakeRarityRibbonSprite(168, 44, new Color32(85, 146, 255, 255), new Color32(46, 98, 226, 255)),   // 2: 稀有(蓝)
                MakeRarityRibbonSprite(168, 44, new Color32(218, 76, 255, 255), new Color32(158, 36, 228, 255)),  // 3: 史诗(紫)
                MakeRarityRibbonSprite(168, 44, new Color32(255, 176, 56, 255), new Color32(232, 116, 24, 255)),  // 4: 传说(橙金)
                MakeRarityRibbonSprite(168, 44, new Color32(255, 94, 104, 255), new Color32(214, 42, 56, 255)),   // 5: 神话(红)
            };

            // 9. 圆形操作钮、红点、红叉、绿勾、摇杆、钓鱼弧形槽、放射光芒
            CircleActionRing = MakeCircleBadge(128, new Color32(38, 50, 96, 246), new Color32(240, 244, 255, 255), new Color32(107, 92, 231, 255));
            RedDotBadge = MakeAlertRedDot(48);
            CrossBadge = MakeCrossIcon(52);
            CheckBadge = MakeCheckIcon(52);
            JoystickRing = MakeJoystickBase(160);
            JoystickThumb = MakeCircleBadge(80, new Color32(244, 246, 255, 238), new Color32(24, 30, 58, 225), new Color32(118, 168, 255, 210));
            WhiteSoftCircle = MakeRippleRing(96);
            MakeArcGaugeSprites(out var arcBg, out var arcFill);
            ArcBarBg = arcBg;
            ArcBarFill = arcFill;
            Sunburst = MakeSunburst(256);
        }

        // ---------------- 切角品质卡与斜切品质绶带查询 ----------------
        public static Sprite GetClippedCard(int rarity0to5)
        {
            Ensure();
            int idx = Mathf.Clamp(rarity0to5, 0, _clippedCardsByRarity.Length - 1);
            return _clippedCardsByRarity[idx];
        }

        public static Sprite GetClippedCard(HeroRarity rarity)
        {
            switch (rarity)
            {
                case HeroRarity.Legendary: return GetClippedCard(4); // 橙
                case HeroRarity.Epic: return GetClippedCard(3);      // 紫
                default: return GetClippedCard(2);                   // 蓝
            }
        }

        /// <summary>按 0..5 品质档位获取斜切漫画绶带（2=稀有蓝，3=史诗紫，4=传说橙金）。</summary>
        public static Sprite RarityRibbon(int rarity)
        {
            Ensure();
            int idx = Mathf.Clamp(rarity, 0, _rarityRibbons.Length - 1);
            return _rarityRibbons[idx];
        }

        /// <summary>按 HeroRarity 枚举直接获取英雄舱详情头部的斜切品质绶带。</summary>
        public static Sprite RarityRibbon(HeroRarity rarity)
        {
            switch (rarity)
            {
                case HeroRarity.Legendary: return RarityRibbon(4); // 传说 · 橙金
                case HeroRarity.Epic: return RarityRibbon(3);      // 史诗 · 紫红
                default: return RarityRibbon(2);                   // 稀有 · 宝蓝
            }
        }

        /// <summary>为背包/结算物资提供统一的 0..5 水世界品质色阶映射。</summary>
        public static int GetResRarity0To5(SurvRes r)
        {
            switch (r)
            {
                case SurvRes.HeroMedal:
                case SurvRes.PlateArmor:
                case SurvRes.SeedGolden:
                    return 4; // 橙
                case SurvRes.Blueprint:
                case SurvRes.FurCoat:
                case SurvRes.MedKit:
                case SurvRes.SmokedMeat:
                case SurvRes.Parts:
                    return 3; // 紫
                case SurvRes.IronBeam:
                case SurvRes.IronIngot:
                case SurvRes.MetalPipe:
                case SurvRes.LeatherBoots:
                case SurvRes.VeggieSoup:
                case SurvRes.GrillFish:
                case SurvRes.WarmTea:
                case SurvRes.Ration:
                case SurvRes.SeedHerb:
                    return 2; // 蓝
                case SurvRes.Plank:
                case SurvRes.Nails:
                case SurvRes.WoodBed:
                case SurvRes.LinenShirt:
                case SurvRes.RoastVeggie:
                case SurvRes.HerbPoultice:
                case SurvRes.SeedFlax:
                case SurvRes.Compost:
                    return 1; // 绿
                default:
                    return 0; // 灰蓝基础原材
            }
        }

        // ---------------- 手绘图标、立绘与水世界 ww_* UI 图集检索 ----------------
        static Sprite LoadRawForestSprite(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (UiSpriteCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;
            Sprite s = Resources.Load<Sprite>("ForestArt/" + key);
            if (s == null)
            {
                Texture2D t = Resources.Load<Texture2D>("ForestArt/" + key);
                if (t != null)
                    s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
            }
            if (s != null) UiSpriteCache[key] = s;
            return s;
        }

        public static Sprite GetArt(string key, Sprite fallback = null)
        {
            if (string.IsNullOrEmpty(key)) return fallback;
            // 若旧组件请求 btn_back_red / ico_xp_star / ico_check 且水世界新版 ww_* 已落盘，自动优先采用新版水世界美术！
            if (key == "btn_back_red")
            {
                Sprite wwBack = LoadRawForestSprite("ww_btn_back_red");
                if (wwBack != null) return wwBack;
            }
            else if (key == "ico_xp_star")
            {
                Sprite wwStar = LoadRawForestSprite("ww_xp_star");
                if (wwStar != null) return wwStar;
            }
            else if (key == "ico_check")
            {
                Sprite wwChk = LoadRawForestSprite("ww_check_green");
                if (wwChk != null) return wwChk;
            }

            Sprite s = LoadRawForestSprite(key);
            if (s != null) return s;
            if (key.StartsWith("ww_")) return GetUiArt(key, fallback);
            return fallback;
        }

        /// <summary>
        /// 加载 Resources/ForestArt/ 下的水世界 ww_* 精灵（支持 Sprite 或 Texture2D 居中创建并缓存）；
        /// 若某张 ww_* 贴图尚未导入或缺失，自动无缝返回高精度程序化漫画兜底图，确保永不空指针或紫块。
        /// </summary>
        public static Sprite GetUiArt(string key, Sprite fallback = null)
        {
            Ensure();
            if (string.IsNullOrEmpty(key)) return fallback;

            Sprite s = LoadRawForestSprite(key);
            if (s != null) return s;

            Sprite proc = GetOrCreateWwFallback(key);
            if (proc != null && (fallback == null || fallback == RedDotBadge))
                return proc;

            return fallback != null ? fallback : proc;
        }

        static Sprite GetOrCreateWwFallback(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (ProcWwFallbackCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            Sprite sp = null;
            switch (key)
            {
                case "ww_check_green":
                    sp = CheckBadge != null ? CheckBadge : (LoadRawForestSprite("ico_check") ?? MakeCheckIcon(52));
                    break;
                case "ww_cross_red":
                    sp = CrossBadge != null ? CrossBadge : MakeCrossIcon(52);
                    break;
                case "ww_btn_back_red":
                    sp = LoadRawForestSprite("btn_back_red") ?? MakeWwBackRedButton(92);
                    break;
                case "ww_xp_star":
                    sp = LoadRawForestSprite("ico_xp_star") ?? MakeWwXpStar(64);
                    break;
                case "ww_badge_power_fist":
                    sp = MakeWwPowerFistBadge(64);
                    break;
                case "ww_order_gift_box":
                    sp = MakeWwOrderGiftBox(80);
                    break;
                case "ww_smiley_morale":
                    sp = MakeWwSmileyMorale(64);
                    break;
                case "ww_banner_congrats":
                    sp = MakeWwBannerPlaque(360, 96, new Color32(255, 134, 232, 255), new Color32(52, 40, 118, 255));
                    break;
                case "ww_banner_levelup":
                    sp = MakeWwBannerPlaque(360, 96, new Color32(255, 98, 108, 255), new Color32(238, 162, 38, 255));
                    break;
                case "ww_hero_cabin_bg":
                    sp = MakeWwHeroCabinBackdrop(256, 256);
                    break;
                case "ww_dock_battle":
                    sp = LoadRawForestSprite("dock_defense") ?? MakeSkillMedallion(96, new Color32(246, 86, 72, 255), 2);
                    break;
                case "ww_dock_heroes":
                    sp = LoadRawForestSprite("dock_heroes") ?? MakeSkillMedallion(96, new Color32(107, 92, 231, 255), 0);
                    break;
                case "ww_dock_bag":
                    sp = LoadRawForestSprite("dock_bag") ?? MakeSkillMedallion(96, new Color32(242, 162, 52, 255), 0);
                    break;
                case "ww_dock_build":
                    sp = LoadRawForestSprite("dock_build") ?? MakeSkillMedallion(96, new Color32(72, 148, 255, 255), 2);
                    break;
                case "ww_dock_delegate":
                    sp = LoadRawForestSprite("dock_radio") ?? MakeSkillMedallion(96, new Color32(58, 204, 132, 255), 5);
                    break;
                case "ww_dock_citymap":
                    sp = LoadRawForestSprite("dock_map") ?? MakeSkillMedallion(96, new Color32(192, 96, 246, 255), 3);
                    break;
                case "ww_dept_workshop":
                    sp = LoadRawForestSprite("sawmill_shed") ?? WwSubPanel;
                    break;
                case "ww_dept_build":
                    sp = LoadRawForestSprite("ico_hammer") ?? WwSubPanel;
                    break;
                case "ww_dept_farm":
                    sp = LoadRawForestSprite("farm_2") ?? WwSubPanel;
                    break;
                case "ww_dept_trade":
                    sp = LoadRawForestSprite("radio_post") ?? WwSubPanel;
                    break;
                case "ww_skill_shield":
                    sp = MakeSkillMedallion(128, new Color32(74, 148, 255, 255), 0);
                    break;
                case "ww_skill_fire":
                    sp = MakeSkillMedallion(128, new Color32(255, 118, 48, 255), 1);
                    break;
                case "ww_skill_cleave":
                    sp = MakeSkillMedallion(128, new Color32(238, 76, 82, 255), 2);
                    break;
                case "ww_skill_arrow":
                    sp = MakeSkillMedallion(128, new Color32(62, 208, 146, 255), 3);
                    break;
                case "ww_skill_shotgun":
                    sp = MakeSkillMedallion(128, new Color32(255, 168, 52, 255), 4);
                    break;
                case "ww_skill_heal":
                    sp = MakeSkillMedallion(128, new Color32(74, 224, 118, 255), 5);
                    break;
                case "ww_passive_sword":
                    sp = MakeSkillMedallion(128, new Color32(138, 112, 246, 255), 2);
                    break;
                case "ww_passive_heart":
                    sp = MakeSkillMedallion(128, new Color32(246, 96, 138, 255), 0);
                    break;
                case "ww_passive_fury":
                    sp = MakeSkillMedallion(128, new Color32(255, 192, 58, 255), 4);
                    break;
                default:
                    if (key.StartsWith("ww_skill_") || key.StartsWith("ww_passive_"))
                        sp = MakeSkillMedallion(128, new Color32(107, 92, 231, 255), 4);
                    else if (key.StartsWith("ww_"))
                        sp = WwSubPanel;
                    break;
            }

            if (sp != null) ProcWwFallbackCache[key] = sp;
            return sp;
        }

        public static Sprite ResIcon(SurvRes r)
        {
            Ensure();
            string key = null;
            switch (r)
            {
                case SurvRes.Wood: key = "ico_wood"; break;
                case SurvRes.Scrap: key = "ico_scrap"; break;
                case SurvRes.Water: key = "ico_water"; break;
                case SurvRes.Veggie: key = "ico_veggie"; break;
                case SurvRes.Cloth: key = "ico_cloth"; break;
                case SurvRes.Herb: key = "ico_herb"; break;
                case SurvRes.Fish: key = "ico_fish"; break;
                case SurvRes.Meat: key = "ico_meat"; break;
                case SurvRes.Pelt: key = "ico_pelt"; break;
                case SurvRes.Plank: key = "ico_plank"; break;
                case SurvRes.WoodBed: key = "ico_woodbed"; break;
                case SurvRes.IronBeam: key = "rec_beam"; break;
                case SurvRes.Nails: key = "ico_nails"; break;
                case SurvRes.IronIngot: key = "ico_ingot"; break;
                case SurvRes.MetalPipe: key = "ico_pipe"; break;
                case SurvRes.LinenShirt: key = "ico_shirt"; break;
                case SurvRes.LeatherBoots: key = "ico_boots"; break;
                case SurvRes.FurCoat: key = "ico_coat"; break;
                case SurvRes.PlateArmor: key = "ico_armor"; break;
                case SurvRes.RoastVeggie: key = "ico_veggie"; break;
                case SurvRes.VeggieSoup: key = "rec_stew"; break;
                case SurvRes.GrillFish: key = "rec_roast_fish"; break;
                case SurvRes.SmokedMeat: key = "ico_ration"; break;
                case SurvRes.HerbPoultice: key = "ico_poultice"; break;
                case SurvRes.WarmTea: key = "ico_tea"; break;
                case SurvRes.MedKit: key = "ico_medkit"; break;
                case SurvRes.Ration: key = "ico_ration"; break;
                case SurvRes.Parts: key = "ico_parts"; break;
                case SurvRes.Blueprint: key = "ico_blueprint"; break;
                case SurvRes.HeroMedal: key = "ico_hero_medal"; break;
                case SurvRes.SeedVeggie: key = "ico_seed_veggie"; break;
                case SurvRes.SeedFlax: key = "ico_seed_flax"; break;
                case SurvRes.SeedHerb: key = "ico_seed_herb"; break;
                case SurvRes.SeedGolden: key = "ico_seed_golden"; break;
                case SurvRes.Compost: key = "ico_compost"; break;
            }
            Sprite art = GetArt(key, null);
            if (art != null) return art;
            if (!ProcResIcons.TryGetValue(r, out Sprite proc) || proc == null)
            {
                proc = MakeProceduralResBadge(r);
                ProcResIcons[r] = proc;
            }
            return proc;
        }

        public static Color32 RarityColor(HeroRarity r)
        {
            switch (r)
            {
                case HeroRarity.Legendary: return new Color32(255, 164, 66, 255); // 水世界传说橙 #FFA442
                case HeroRarity.Epic: return new Color32(208, 98, 255, 255);      // 水世界史诗紫 #D062FF
                default: return new Color32(85, 146, 255, 255);                   // 水世界稀有蓝 #5592FF
            }
        }

        public static Sprite RarityButtonSprite(HeroRarity r)
        {
            switch (r)
            {
                case HeroRarity.Legendary: return BtnOrange;
                case HeroRarity.Epic: return BtnBlue;
                default: return BtnBlue;
            }
        }

        public static Sprite NodePreviewSprite(CampNode n)
        {
            if (n == null) return null;
            switch (n.kind)
            {
                case NodeKind.Shelter:
                    int nextLv = n.state == NodeState.Discovered ? 1 : Mathf.Min(3, n.level + 1);
                    if (nextLv >= 3) return GetArt("shelter_lv3", GetArt("tent"));
                    if (nextLv == 2) return GetArt("shelter_lv2", GetArt("tent"));
                    return GetArt("tent");
                case NodeKind.HeatTower:
                    return GetArt("heating_tower", GetArt("campfire"));
                case NodeKind.Dwelling:
                    return GetArt("refugee_cabin", GetArt(n.level >= 1 ? "shelter_lv2" : "tent"));
                case NodeKind.Warehouse:
                    return GetArt("camp_warehouse", GetArt("cargo_stack_unit", GetArt("loot_pile")));
                case NodeKind.Sawmill:
                    return GetArt("sawmill_shed");
                case NodeKind.Smithy:
                    return GetArt("scrap_smithy", GetArt("ruin_generator", GetArt("sawmill_shed")));
                case NodeKind.Tailor:
                    return GetArt("tailor_shop", GetArt("tent"));
                case NodeKind.Kitchen:
                    return (n.state == NodeState.Active && n.level >= 1) ? GetArt("smokehouse_kitchen") : GetArt("campfire");
                case NodeKind.HerbHut:
                    return GetArt("herb_hut", GetArt("tent"));
                case NodeKind.Farm:
                    return GetArt("farm_2");
                case NodeKind.Ranch:
                    return GetArt("beast_ranch", GetArt("boar", GetArt("hunter_tower")));
                case NodeKind.Watchtower:
                    return GetArt("hunter_tower");
                case NodeKind.Barricade:
                    return GetArt("barricade");
                case NodeKind.RelicWell:
                    return GetArt("well");
                case NodeKind.RelicLoot:
                    return GetArt("loot_pile");
                case NodeKind.RelicGenerator:
                    return GetArt("generator_active");
                case NodeKind.RadioPost:
                    return GetArt("radio_post", GetArt("dock_radio", GetArt("hunter_tower")));
                case NodeKind.RelicTruck:
                    return GetArt("truck_active");
                case NodeKind.RelicRoadGate:
                    return GetArt("road_gate");
                case NodeKind.RiverDock:
                    return GetArt("fishing_dock");
            }
            return GetArt("tent");
        }

        // =====================================================================
        // 程序化 SDF 抗锯齿漫画精灵生成器
        // =====================================================================

        static float SegDist(Vector2 a, Vector2 b, Vector2 p)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + ab * t);
        }

        static float SdfRoundBox(Vector2 p, Vector2 b, float r)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x) - b.x + r, Mathf.Abs(p.y) - b.y + r);
            return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
        }

        /// <summary>右上角 45° 斜切角方卡 SDF（精确还原《疯狂水世界》道具/结算品质卡造型）。</summary>
        static float SdfTopRightChamferBox(Vector2 p, Vector2 halfSize, float radius, float chamfer)
        {
            float dBox = SdfRoundBox(p, halfSize, radius);
            // 右上斜切平面法向 (1, 1) / sqrt(2)，切去右上角三角形区域
            float planeVal = (p.x + p.y) - (halfSize.x + halfSize.y - chamfer);
            float dPlane = planeVal * 0.70710678f;
            return Mathf.Max(dBox, dPlane);
        }

        static Sprite MakeClippedRarityCard(int size, Color32 topCol, Color32 botCol, Color32 lipCol)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            var box = new Vector2(half - 2.5f, half - 2.5f);
            float radius = 8f;
            float chamfer = 22f;
            float stroke = 3.0f;
            float lip = 5.5f;
            Color borderCol = new Color32(23, 27, 46, 255); // #171B2E

            for (int y = 0; y < size; y++)
            {
                float tY = y / (float)(size - 1);
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float d = SdfTopRightChamferBox(p, box, radius, chamfer);
                    float outerA = Mathf.Clamp01(0.5f - d);
                    if (outerA <= 0f)
                    {
                        px[y * size + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    float innerMask = Mathf.Clamp01(-d - stroke + 0.5f);
                    // 底部立体暗边厚唇
                    Vector2 pShifted = p - new Vector2(0f, lip * 0.5f);
                    Vector2 innerBox = box - new Vector2(stroke, stroke + lip * 0.5f);
                    float dFace = SdfTopRightChamferBox(pShifted, innerBox, Mathf.Max(2f, radius - stroke), Mathf.Max(6f, chamfer - stroke));
                    float faceMask = Mathf.Clamp01(0.5f - dFace);

                    Color face = Color.Lerp(botCol, topCol, Mathf.Clamp01((tY - 0.08f) / 0.88f));
                    // 漫画斜向双色调亮带（还原参考图 4/6/7 卡片左上-中部的斜切反光层）
                    float diag = (p.x * 0.72f + p.y * 0.69f);
                    if (diag > -18f && diag < 16f)
                        face = Color.Lerp(face, Color.white, 0.16f);
                    else if (p.y > box.y - stroke - 4.5f && (p.x + p.y) < (box.x + box.y - chamfer - 2f))
                        face = Color.Lerp(face, Color.white, 0.28f);

                    Color inside = Color.Lerp(lipCol, face, faceMask);
                    Color c = Color.Lerp(borderCol, inside, innerMask);
                    c.a *= outerA;
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            // 左/下 border 22，右/上 border 30（完整保护右上 22px 斜切角在 9-slice 时不拉伸变形）
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(22, 22, 30, 30));
        }

        static Sprite MakeSelectionBrackets(int size, int border)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            Vector2 box = new Vector2(half - 5f, half - 5f);
            float radius = 15f;
            Color coreMint = new Color32(69, 246, 200, 255);    // #45F6C8
            Color innerBright = new Color32(214, 255, 244, 255);
            Color darkOutline = new Color32(18, 52, 54, 235);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    // 仅保留四角括号区，裁开四边中段 (|x| < 24 或 |y| < 24 的中段留空)
                    float cornerKeep = Mathf.Min(Mathf.Abs(p.x), Mathf.Abs(p.y));
                    // 当点在水平边中段 (|x| < 24 且 |y| > 24) 或垂直边中段 (|y| < 24 且 |x| > 24) 时平滑裁断
                    float gapDist = Mathf.Max(23f - Mathf.Abs(p.x), 23f - Mathf.Abs(p.y));
                    if (gapDist > 1.5f && cornerKeep < 999f && !(Mathf.Abs(p.x) >= 23f && Mathf.Abs(p.y) >= 23f))
                    {
                        px[y * size + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }
                    float endCapMask = (Mathf.Abs(p.x) >= 23f && Mathf.Abs(p.y) >= 23f)
                        ? 1f
                        : Mathf.Clamp01(1.0f - gapDist);

                    float dRing = Mathf.Abs(SdfRoundBox(p, box, radius));
                    float outerA = Mathf.Clamp01(5.2f - dRing) * endCapMask;
                    if (outerA <= 0f)
                    {
                        px[y * size + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    float bodyA = Mathf.Clamp01(3.4f - dRing);
                    float highlightA = Mathf.Clamp01(1.4f - dRing);
                    Color c = Color.Lerp(darkOutline, coreMint, bodyA);
                    c = Color.Lerp(c, innerBright, highlightA * 0.55f);
                    c.a *= outerA;
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        static Sprite MakeRarityRibbonSprite(int w, int h, Color32 leftCol, Color32 rightCol)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[w * h];
            float halfW = w * 0.5f;
            float halfH = h * 0.5f;
            Color ink = new Color32(20, 16, 42, 255);

            for (int y = 0; y < h; y++)
            {
                float ny = (y + 0.5f - halfH) / halfH; // -1..1
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - halfW, y + 0.5f - halfH);
                    // 右侧斜切边 + 左侧燕尾内凹切口（对标参考图1【赏金猎人 · 史诗】紫底斜绶带）
                    float rightEdge = (halfW - 4f) + ny * 6f;
                    float leftNotch = (-halfW + 4f) + (1f - Mathf.Abs(ny)) * 11f;
                    float topBotDist = Mathf.Abs(p.y) - (halfH - 3f);
                    float d = Mathf.Max(topBotDist, Mathf.Max(p.x - rightEdge, leftNotch - p.x));

                    float a = Mathf.Clamp01(0.5f - d);
                    if (a <= 0f)
                    {
                        px[y * w + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    float inner = Mathf.Clamp01(-d - 2.6f + 0.5f);
                    float tx = Mathf.Clamp01((p.x + halfW) / w);
                    Color fill = Color.Lerp(leftCol, rightCol, tx);
                    if (ny > 0.35f) fill = Color.Lerp(fill, Color.white, 0.22f);
                    if (ny < -0.55f) fill = Color.Lerp(fill, Color.black, 0.22f);

                    Color c = Color.Lerp(ink, fill, inner);
                    c.a *= a;
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(22, 8, 18, 8));
        }

        static Sprite MakeBrokenComicSheet(int size, int border)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            Vector2 outerBox = new Vector2(half - 2.5f, half - 2.5f);
            Vector2 innerLineBox = new Vector2(half - 12f, half - 12f);

            Color outerBorder = new Color32(34, 42, 72, 255);
            Color topFill = new Color32(236, 240, 250, 255);
            Color botFill = new Color32(226, 232, 246, 255);
            Color innerRule = new Color32(76, 90, 198, 255); // 皇家靛紫内细框线

            for (int y = 0; y < size; y++)
            {
                float tY = y / (float)(size - 1);
                Color sheetFill = Color.Lerp(botFill, topFill, tY);
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    // 外边缘微做两处漫画纸张缺口（左上角外侧与左下外侧，严格落在 28px border 角区内）
                    float nick1 = 3.2f - Vector2.Distance(p, new Vector2(-half + 20f, half - 2.5f));
                    float nick2 = 3.0f - Vector2.Distance(p, new Vector2(-half + 2.5f, -half + 22f));
                    float dOuter = Mathf.Max(SdfRoundBox(p, outerBox, 4f), Mathf.Max(nick1, nick2));

                    float outerA = Mathf.Clamp01(0.5f - dOuter);
                    if (outerA <= 0f)
                    {
                        px[y * size + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    float innerSheetMask = Mathf.Clamp01(-dOuter - 2.5f + 0.5f);
                    Color inside = sheetFill;

                    // 内层靛紫双线断墨框（在右上角与右下角附近留出漫画断线感，对标参考图 6 & 7！）
                    float dRule = Mathf.Abs(SdfRoundBox(p, innerLineBox, 2f));
                    bool inBrokenGap =
                        (p.y > innerLineBox.y - 2f && p.x > half - 26f && p.x < half - 18f) ||
                        (p.y < -innerLineBox.y + 2f && p.x > half - 25f && p.x < half - 17f) ||
                        (p.x > innerLineBox.x - 2f && p.y > half - 26f && p.y < half - 20f);

                    if (!inBrokenGap && dRule < 1.6f)
                    {
                        float ruleA = Mathf.Clamp01(1.5f - dRule);
                        inside = Color.Lerp(inside, innerRule, ruleA * 0.92f);
                    }

                    Color c = Color.Lerp(outerBorder, inside, innerSheetMask);
                    c.a *= outerA;
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        static Sprite MakeActivePurpleTab(int size, int border)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            Vector2 box = new Vector2(half - 2f, half - 2f);
            Vector2 whiteRingBox = new Vector2(half - 6.5f, half - 6.5f);
            Color outerInk = new Color32(28, 22, 68, 255);
            Color topPurple = new Color32(122, 106, 246, 255); // #7A6AF6
            Color botPurple = new Color32(92, 74, 218, 255);   // #5C4ADA

            for (int y = 0; y < size; y++)
            {
                float tY = y / (float)(size - 1);
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float d = SdfRoundBox(p, box, 11f);
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a <= 0f) { px[y * size + x] = new Color32(0, 0, 0, 0); continue; }

                    float innerMask = Mathf.Clamp01(-d - 2.6f + 0.5f);
                    Color fill = Color.Lerp(botPurple, topPurple, tY);
                    if (p.x + p.y > 4f && p.x + p.y < 24f)
                        fill = Color.Lerp(fill, Color.white, 0.14f);

                    // 标志性的纯白细内框（对标参考图1/6/7底部激活紫页签！）
                    float dWhite = Mathf.Abs(SdfRoundBox(p, whiteRingBox, 7.5f));
                    float whiteA = Mathf.Clamp01(1.6f - dWhite);
                    fill = Color.Lerp(fill, Color.white, whiteA * 0.92f);

                    Color c = Color.Lerp(outerInk, fill, innerMask);
                    c.a *= a;
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        static Sprite MakeIdleStoneTab(int size, int border)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            Vector2 box = new Vector2(half - 2f, half - 2f);
            Color outerInk = new Color32(78, 80, 102, 255);    // #4E5066
            Color topStone = new Color32(224, 226, 236, 255);  // #E0E2EC
            Color botStone = new Color32(198, 200, 214, 255);  // #C6C8D6
            Color rivetCol = new Color32(142, 145, 166, 255);

            for (int y = 0; y < size; y++)
            {
                float tY = y / (float)(size - 1);
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float d = SdfRoundBox(p, box, 10f);
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a <= 0f) { px[y * size + x] = new Color32(0, 0, 0, 0); continue; }

                    float innerMask = Mathf.Clamp01(-d - 2.4f + 0.5f);
                    Color fill = Color.Lerp(botStone, topStone, tY);
                    if (p.y > box.y - 5.5f) fill = Color.Lerp(fill, Color.white, 0.45f);
                    if (p.y < -box.y + 5.5f) fill = Color.Lerp(fill, new Color32(150, 152, 170, 255), 0.55f);

                    // 四角小铆钉刻痕（对标参考图1/6/7石质页签边角细节）
                    Vector2 ap = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y));
                    float dRivet = Vector2.Distance(ap, new Vector2(half - 9.5f, half - 9.5f));
                    if (dRivet < 2.4f)
                        fill = Color.Lerp(fill, rivetCol, Mathf.Clamp01(2.4f - dRivet) * 0.85f);

                    Color c = Color.Lerp(outerInk, fill, innerMask);
                    c.a *= a;
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        static Sprite MakeZebraStripe(int size, float radius, Color32 col, int border)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            Vector2 box = new Vector2(half - 1f, half - 1f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float d = SdfRoundBox(p, box, radius);
                    float a = Mathf.Clamp01(0.5f - d);
                    Color c = col;
                    c.a *= a;
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        static Sprite MakeCrossIcon(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            Color ink = new Color32(32, 16, 36, 255);
            Color red = new Color32(246, 66, 78, 255);
            Color hi = new Color32(255, 152, 160, 255);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float d1 = SegDist(new Vector2(-13.5f, -13.5f), new Vector2(13.5f, 13.5f), p);
                    float d2 = SegDist(new Vector2(-13.5f, 13.5f), new Vector2(13.5f, -13.5f), p);
                    float d = Mathf.Min(d1, d2);
                    float a = Mathf.Clamp01(6.4f - d);
                    if (a <= 0f) { px[y * size + x] = new Color32(0, 0, 0, 0); continue; }
                    Color fill = p.y > 2f ? Color.Lerp(red, hi, 0.35f) : red;
                    Color c = Color.Lerp(ink, fill, Mathf.Clamp01(4.1f - d));
                    c.a *= a;
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeCheckIcon(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            Color ink = new Color32(20, 42, 28, 255);
            Color grn = new Color32(92, 236, 98, 255);
            Color hi = new Color32(198, 255, 192, 255);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float d1 = SegDist(new Vector2(-14f, -1f), new Vector2(-3f, -12f), p);
                    float d2 = SegDist(new Vector2(-3f, -12f), new Vector2(15f, 12f), p);
                    float d = Mathf.Min(d1, d2);
                    float a = Mathf.Clamp01(6.6f - d);
                    if (a <= 0f) { px[y * size + x] = new Color32(0, 0, 0, 0); continue; }
                    Color fill = p.y > 0f ? Color.Lerp(grn, hi, 0.38f) : grn;
                    Color c = Color.Lerp(ink, fill, Mathf.Clamp01(4.2f - d));
                    c.a *= a;
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeWwBackRedButton(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            Vector2 box = new Vector2(half - 3f, half - 5f);
            Color ink = new Color32(24, 18, 36, 255);
            Color topRed = new Color32(255, 88, 102, 255);
            Color botRed = new Color32(232, 48, 66, 255);
            Color lipRed = new Color32(152, 22, 38, 255);

            for (int y = 0; y < size; y++)
            {
                float tY = y / (float)(size - 1);
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float d = SdfRoundBox(p, box, 18f);
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a <= 0f) { px[y * size + x] = new Color32(0, 0, 0, 0); continue; }

                    float inner = Mathf.Clamp01(-d - 3.0f + 0.5f);
                    float dFace = SdfRoundBox(p - new Vector2(0f, 3f), box - new Vector2(3f, 6f), 15f);
                    Color face = Color.Lerp(botRed, topRed, tY);
                    Color inside = Color.Lerp(lipRed, face, Mathf.Clamp01(0.5f - dFace));

                    // 白色粗弯箭头
                    float dHead1 = SegDist(new Vector2(-19f, 5f), new Vector2(-5f, 17f), p);
                    float dHead2 = SegDist(new Vector2(-19f, 5f), new Vector2(-5f, -7f), p);
                    float dStem = SegDist(new Vector2(-13f, 5f), new Vector2(10f, 5f), p);
                    float dCurve = SegDist(new Vector2(10f, 5f), new Vector2(18f, -13f), p);
                    float dArrow = Mathf.Min(Mathf.Min(dHead1, dHead2), Mathf.Min(dStem, dCurve));
                    if (dArrow < 6.5f)
                    {
                        Color arrowCol = dArrow < 4.2f ? Color.white : new Color32(128, 22, 36, 255);
                        inside = Color.Lerp(inside, arrowCol, Mathf.Clamp01(6.2f - dArrow));
                    }

                    Color c = Color.Lerp(ink, inside, inner);
                    c.a *= a;
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeWwPowerFistBadge(int size)
        {
            return MakeSkillMedallion(size, new Color32(54, 108, 242, 255), 4);
        }

        static Sprite MakeWwOrderGiftBox(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            Color ink = new Color32(24, 18, 36, 255);
            Color bagRed = new Color32(238, 56, 72, 255);
            Color bagDark = new Color32(178, 30, 46, 255);
            Color gold = new Color32(255, 212, 68, 255);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float dBody = SdfRoundBox(p - new Vector2(0f, -6f), new Vector2(24f, 20f), 8f);
                    float dHandle = Mathf.Abs((p - new Vector2(0f, 14f)).magnitude - 10f) - 3.2f;
                    if (p.y < 10f) dHandle = 99f;
                    float d = Mathf.Min(dBody, dHandle);
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a <= 0f) { px[y * size + x] = new Color32(0, 0, 0, 0); continue; }
                    float inner = Mathf.Clamp01(-d - 2.8f + 0.5f);
                    Color fill = Color.Lerp(bagDark, bagRed, Mathf.Clamp01((p.y + 24f) / 40f));
                    if (Mathf.Abs(p.x) < 4.5f && p.y < 14f) fill = gold;
                    Color c = Color.Lerp(ink, fill, inner);
                    c.a *= a;
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeWwSmileyMorale(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            float rOuter = half - 2.5f;
            Color ink = new Color32(24, 20, 34, 255);
            Color topGold = new Color32(255, 228, 78, 255);
            Color botGold = new Color32(246, 178, 36, 255);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float dist = p.magnitude;
                    float a = Mathf.Clamp01(rOuter - dist + 0.5f);
                    if (a <= 0f) { px[y * size + x] = new Color32(0, 0, 0, 0); continue; }

                    Color fill = Color.Lerp(botGold, topGold, Mathf.Clamp01((p.y + rOuter) / (2f * rOuter)));
                    float dEyeL = Vector2.Distance(new Vector2(p.x + 8.5f, p.y * 0.7f - 4.5f), Vector2.zero);
                    float dEyeR = Vector2.Distance(new Vector2(p.x - 8.5f, p.y * 0.7f - 4.5f), Vector2.zero);
                    float dSmile = Mathf.Abs((p - new Vector2(0f, 1f)).magnitude - 13.5f);
                    if (dEyeL < 3.2f || dEyeR < 3.2f || (p.y < -2f && dSmile < 2.5f))
                        fill = ink;

                    Color c = dist > rOuter - 3.0f ? ink : fill;
                    c.a *= a;
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeWwXpStar(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            Color ink = new Color32(28, 18, 14, 255);
            Color topCol = new Color32(255, 224, 72, 255);
            Color botCol = new Color32(246, 138, 28, 255);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float ang = Mathf.Atan2(p.y, p.x) - Mathf.PI * 0.5f;
                    float starWave = Mathf.Cos(ang * 5f);
                    float maxR = Mathf.Lerp(half * 0.44f, half * 0.88f, (starWave + 1f) * 0.5f);
                    float d = p.magnitude - maxR;
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a <= 0f) { px[y * size + x] = new Color32(0, 0, 0, 0); continue; }
                    Color fill = Color.Lerp(botCol, topCol, Mathf.Clamp01((p.y + half) / size));
                    Color c = Color.Lerp(ink, fill, Mathf.Clamp01(-d - 2.6f + 0.5f));
                    c.a *= a;
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeWwBannerPlaque(int w, int h, Color32 accentTop, Color32 baseShell)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];
            float hw = w * 0.5f, hh = h * 0.5f;
            Color ink = new Color32(16, 14, 34, 255);
            for (int y = 0; y < h; y++)
            {
                float ny = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - hw, y + 0.5f - hh);
                    Vector2 skewed = new Vector2(p.x - p.y * 0.16f, p.y);
                    float d = SdfRoundBox(skewed, new Vector2(hw - 14f, hh - 6f), 10f);
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a <= 0f) { px[y * w + x] = new Color32(0, 0, 0, 0); continue; }
                    float inner = Mathf.Clamp01(-d - 3.5f + 0.5f);
                    Color fill = Color.Lerp(baseShell, accentTop, ny * 0.65f);
                    if (ny < 0.22f) fill = Color.Lerp(fill, Color.black, 0.35f);
                    Color c = Color.Lerp(ink, fill, inner);
                    c.a *= a;
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeWwHeroCabinBackdrop(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];
            Color wallTop = new Color32(62, 76, 118, 255);
            Color wallBot = new Color32(84, 98, 142, 255);
            Color floorCol = new Color32(138, 122, 118, 255);
            Color matCol = new Color32(112, 134, 178, 255);
            Color sunCol = new Color32(255, 246, 214, 255);

            for (int y = 0; y < h; y++)
            {
                float ny = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = x / (float)(w - 1);
                    Color c = ny < 0.28f ? floorCol : Color.Lerp(wallBot, wallTop, (ny - 0.28f) / 0.72f);
                    // 中央地垫
                    if (ny > 0.08f && ny < 0.24f && Mathf.Abs(nx - 0.5f) < 0.28f)
                        c = matCol;
                    // 上方小亮窗与暖阳光束（对标参考图1船舱立绘舞台）
                    if (ny > 0.56f && ny < 0.76f && Mathf.Abs(nx - 0.48f) < 0.13f)
                        c = sunCol;
                    else
                    {
                        float beamCenter = 0.48f - (0.68f - ny) * 0.18f;
                        float beamHalfW = 0.14f + (0.68f - ny) * 0.22f;
                        if (ny < 0.68f && ny > 0.14f && Mathf.Abs(nx - beamCenter) < beamHalfW)
                            c = Color.Lerp(c, sunCol, 0.22f * (1f - Mathf.Abs(nx - beamCenter) / beamHalfW));
                    }
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeSkillMedallion(int size, Color32 mainCol, int glyphKind)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            float rOuter = half - 2.5f;
            Color ink = new Color32(18, 20, 36, 255);
            Color rim = new Color32(238, 242, 255, 255);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float dist = p.magnitude;
                    float a = Mathf.Clamp01(rOuter - dist + 0.5f);
                    if (a <= 0f) { px[y * size + x] = new Color32(0, 0, 0, 0); continue; }

                    Color c = ink;
                    if (dist < rOuter - 3.2f) c = rim;
                    if (dist < rOuter - 6.5f) c = ink;
                    if (dist < rOuter - 8.5f)
                    {
                        float grad = Mathf.Clamp01((p.y + rOuter) / (2f * rOuter));
                        Color fill = Color.Lerp(Color.Lerp(mainCol, Color.black, 0.32f), Color.Lerp(mainCol, Color.white, 0.25f), grad);

                        // 简洁高辨识中心漫画纹章
                        Vector2 np = p / (rOuter * 0.55f);
                        bool inGlyph = false;
                        if (glyphKind == 0) inGlyph = SdfRoundBox(np, new Vector2(0.52f, 0.60f), 0.22f) < 0f; // 盾/心
                        else if (glyphKind == 1) inGlyph = (np - new Vector2(0f, -0.1f)).magnitude < 0.55f - Mathf.Max(0f, np.y) * 0.35f; // 烈焰
                        else if (glyphKind == 2) inGlyph = SegDist(new Vector2(-0.58f, -0.52f), new Vector2(0.58f, 0.58f), np) < 0.22f; // 斩击
                        else if (glyphKind == 3) inGlyph = Mathf.Abs(np.y - np.x * 0.45f) < 0.14f || Mathf.Abs(np.y - np.x * 0.45f + 0.36f) < 0.12f; // 连矢
                        else if (glyphKind == 4) inGlyph = Mathf.Abs(Mathf.Sin(Mathf.Atan2(np.y, np.x) * 4f)) * 0.65f > np.magnitude - 0.18f; // 爆星
                        else inGlyph = (Mathf.Abs(np.x) < 0.22f && Mathf.Abs(np.y) < 0.62f) || (Mathf.Abs(np.y) < 0.22f && Mathf.Abs(np.x) < 0.62f); // 十字群疗

                        if (inGlyph) fill = Color.Lerp(fill, Color.white, 0.85f);
                        c = fill;
                    }
                    c.a *= a;
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeProceduralResBadge(SurvRes r)
        {
            int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[s * s];
            float half = s * 0.5f;
            Color32 main = new Color32(168, 132, 86, 255);
            Color32 sub = new Color32(245, 226, 182, 255);
            int motif = 0;

            switch (r)
            {
                case SurvRes.Blueprint: main = new Color32(52, 124, 240, 255); sub = new Color32(236, 248, 255, 255); motif = 0; break;
                case SurvRes.Nails: main = new Color32(132, 146, 168, 255); sub = new Color32(218, 228, 242, 255); motif = 1; break;
                case SurvRes.IronIngot: main = new Color32(98, 112, 136, 255); sub = new Color32(188, 204, 226, 255); motif = 2; break;
                case SurvRes.MetalPipe: main = new Color32(198, 72, 64, 255); sub = new Color32(242, 152, 140, 255); motif = 3; break;
                case SurvRes.Cloth: main = new Color32(208, 192, 158, 255); sub = new Color32(246, 238, 218, 255); motif = 4; break;
                case SurvRes.Pelt: main = new Color32(158, 98, 54, 255); sub = new Color32(226, 174, 118, 255); motif = 4; break;
                case SurvRes.LinenShirt: main = new Color32(82, 148, 206, 255); sub = new Color32(192, 228, 252, 255); motif = 4; break;
                case SurvRes.LeatherBoots: main = new Color32(138, 78, 42, 255); sub = new Color32(212, 152, 98, 255); motif = 4; break;
                case SurvRes.FurCoat: main = new Color32(176, 108, 48, 255); sub = new Color32(246, 236, 214, 255); motif = 4; break;
                case SurvRes.PlateArmor: main = new Color32(78, 96, 126, 255); sub = new Color32(242, 174, 68, 255); motif = 2; break;
                case SurvRes.Herb: main = new Color32(64, 176, 86, 255); sub = new Color32(182, 242, 172, 255); motif = 5; break;
                case SurvRes.HerbPoultice: main = new Color32(86, 188, 106, 255); sub = new Color32(246, 246, 234, 255); motif = 5; break;
                case SurvRes.WarmTea: main = new Color32(216, 132, 48, 255); sub = new Color32(252, 218, 152, 255); motif = 5; break;
                case SurvRes.MedKit: main = new Color32(232, 62, 68, 255); sub = new Color32(255, 250, 248, 255); motif = 5; break;
                case SurvRes.WoodBed: main = new Color32(166, 108, 58, 255); sub = new Color32(236, 224, 196, 255); motif = 6; break;
                case SurvRes.HeroMedal: main = new Color32(242, 172, 42, 255); sub = new Color32(255, 242, 178, 255); motif = 0; break;
            }

            Color ink = new Color32(20, 24, 42, 255);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float d = SdfRoundBox(p, new Vector2(24f, 24f), 9f);
                    if (motif == 1)
                        d = Mathf.Min(SegDist(new Vector2(-14f, -12f), new Vector2(12f, 14f), p) - 4.5f,
                                      SegDist(new Vector2(-10f, 12f), new Vector2(14f, -10f), p) - 4.5f);
                    else if (motif == 2)
                        d = SdfRoundBox(new Vector2(p.x - p.y * 0.18f, p.y), new Vector2(22f, 14f), 5f);
                    else if (motif == 3)
                        d = Mathf.Min(SegDist(new Vector2(-18f, -8f), new Vector2(2f, -8f), p),
                                      Mathf.Min(SegDist(new Vector2(2f, -8f), new Vector2(2f, 10f), p),
                                                SegDist(new Vector2(2f, 10f), new Vector2(18f, 10f), p))) - 5.5f;

                    float a = Mathf.Clamp01(0.5f - d);
                    if (a <= 0f) { px[y * s + x] = new Color32(0, 0, 0, 0); continue; }
                    float inner = Mathf.Clamp01(-d - 2.6f + 0.5f);
                    Color fill = Color.Lerp(main, sub, Mathf.Clamp01((p.y + 20f) / 44f) * 0.45f);
                    if (motif == 0 && (Mathf.Abs(p.x % 10f) < 1.1f || Mathf.Abs(p.y % 10f) < 1.1f) && Mathf.Abs(p.x) < 18f && Mathf.Abs(p.y) < 18f)
                        fill = Color.Lerp(fill, sub, 0.78f);
                    if (motif == 5 && (Mathf.Abs(p.x) < 4.2f && Mathf.Abs(p.y) < 13f || Mathf.Abs(p.y) < 4.2f && Mathf.Abs(p.x) < 13f))
                        fill = sub;
                    if (motif == 4 && p.y > -8f && p.y < 8f && Mathf.Abs(p.x) < 12f)
                        fill = Color.Lerp(fill, sub, 0.55f);
                    if (motif == 6 && p.y > -4f && p.y < 10f && Mathf.Abs(p.x) < 17f)
                        fill = sub;

                    Color c = Color.Lerp(ink, fill, inner);
                    c.a *= a;
                    px[y * s + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite Make9SliceBevel(int size, float radius, float stroke, float lip,
            Color32 topFill, Color32 botFill, Color32 borderCol, Color32 topRim, Color32 lipCol, int border)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            var box = new Vector2(half - 2f, half - 2f);

            for (int y = 0; y < size; y++)
            {
                float tY = y / (float)(size - 1);
                Color fill = Color.Lerp(botFill, topFill, tY);
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float d = SdfRoundBox(p, box, radius);
                    float outerA = Mathf.Clamp01(0.5f - d);
                    if (outerA <= 0f)
                    {
                        px[y * size + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    float innerMask = Mathf.Clamp01(-d - stroke + 0.5f);
                    float dShiftedUp = SdfRoundBox(p - new Vector2(0f, lip), box - new Vector2(stroke, stroke), Mathf.Max(2f, radius - stroke));
                    float bodyMask = Mathf.Clamp01(0.5f - dShiftedUp);

                    float dShiftedDown = SdfRoundBox(p + new Vector2(0f, 2.2f), box - new Vector2(stroke, stroke), Mathf.Max(2f, radius - stroke));
                    float rimMask = bodyMask * (1f - Mathf.Clamp01(0.5f - dShiftedDown));

                    Color inside = Color.Lerp(lipCol, fill, bodyMask);
                    inside = Color.Lerp(inside, topRim, rimMask * (topRim.a / 255f));
                    Color c = Color.Lerp(borderCol, inside, innerMask);
                    c.a *= outerA;
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        static Sprite MakeJellyButton(int size, float radius, Color32 topCol, Color32 botCol, Color32 lipCol)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            var box = new Vector2(half - 2f, half - 2f);
            Color borderCol = new Color32(18, 22, 42, 255); // 水世界深靛黑墨线
            float stroke = 3.0f;
            float lip = 6.0f;

            for (int y = 0; y < size; y++)
            {
                float tY = y / (float)(size - 1);
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float d = SdfRoundBox(p, box, radius);
                    float outerA = Mathf.Clamp01(0.5f - d);
                    if (outerA <= 0f)
                    {
                        px[y * size + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    float innerMask = Mathf.Clamp01(-d - stroke + 0.5f);
                    float dLip = SdfRoundBox(p - new Vector2(0f, lip * 0.5f), box - new Vector2(stroke, stroke + lip * 0.5f), Mathf.Max(2f, radius - stroke));
                    float faceMask = Mathf.Clamp01(0.5f - dLip);

                    Color face = Color.Lerp(botCol, topCol, Mathf.Clamp01((tY - 0.12f) / 0.85f));
                    if (tY > 0.70f && tY < 0.88f && Mathf.Abs(p.x) < half - 11f)
                        face = Color.Lerp(face, Color.white, 0.25f);

                    Color inside = Color.Lerp(lipCol, face, faceMask);
                    Color c = Color.Lerp(borderCol, inside, innerMask);
                    c.a *= outerA;
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(22, 22, 22, 22));
        }

        static Sprite MakeSpeechBubbleTex(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[w * h];
            float tailH = 12f;
            float bodyH = h - tailH;
            Vector2 center = new Vector2(w * 0.5f, tailH + bodyH * 0.5f);
            Vector2 halfBox = new Vector2(w * 0.5f - 2f, bodyH * 0.5f - 2f);
            Color ink = new Color32(22, 28, 52, 255);
            Color paper = new Color32(247, 249, 255, 252);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float dBody = SdfRoundBox(p - center, halfBox, 11f);

                    float ty = Mathf.Clamp01((tailH + 3f - p.y) / (tailH + 1f));
                    float halfTailW = Mathf.Lerp(9f, 0.5f, ty);
                    float dTail = (p.y <= tailH + 4f && p.y >= 2f)
                        ? Mathf.Abs(p.x - w * 0.5f) - halfTailW
                        : 99f;

                    float d = Mathf.Min(dBody, dTail);
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a <= 0f) { px[y * w + x] = new Color32(0, 0, 0, 0); continue; }
                    float inner = Mathf.Clamp01(-d - 2.4f + 0.5f);
                    Color c = Color.Lerp(ink, paper, inner);
                    c.a *= a;
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect, new Vector4(18, 18, 18, 16));
        }

        static Sprite MakeAlertRedDot(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            float rOuter = half - 1.5f;
            Color ink = new Color32(22, 24, 48, 255);
            Color redTop = new Color32(255, 68, 78, 255);
            Color redBot = new Color32(218, 28, 42, 255);
            Color rimHi = new Color32(255, 168, 174, 255);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float dist = p.magnitude;
                    float a = Mathf.Clamp01(rOuter - dist + 0.6f);
                    if (a <= 0f) { px[y * size + x] = new Color32(0, 0, 0, 0); continue; }

                    Color c = ink;
                    if (dist < rOuter - 2.8f)
                    {
                        float grad = Mathf.Clamp01((p.y + rOuter) / (2f * rOuter));
                        c = Color.Lerp(redBot, redTop, grad);
                        if (dist > rOuter - 4.6f && p.y > 0f)
                            c = Color.Lerp(c, rimHi, 0.55f);
                    }
                    c.a *= a;
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeCircleBadge(int size, Color32 fillCol, Color32 ringCol, Color32 innerRimCol)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            float rOuter = half - 2f;
            Color ink = new Color32(17, 22, 46, 255);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float dist = p.magnitude;
                    float a = Mathf.Clamp01(rOuter - dist + 0.5f);
                    if (a <= 0f) { px[y * size + x] = new Color32(0, 0, 0, 0); continue; }

                    Color c = ink;
                    if (dist < rOuter - 2.6f) c = ringCol;
                    if (dist < rOuter - 7.0f) c = innerRimCol;
                    if (dist < rOuter - 9.5f)
                    {
                        float grad = Mathf.Clamp01((p.y + rOuter) / (2f * rOuter));
                        c = Color.Lerp(Color.Lerp(fillCol, Color.black, 0.25f), fillCol, grad);
                    }
                    c.a *= a;
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeJoystickBase(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            float r = half - 3f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = new Vector2(x + 0.5f - half, y + 0.5f - half).magnitude;
                    if (d > r + 1f) { px[y * size + x] = new Color32(0, 0, 0, 0); continue; }
                    float outerA = Mathf.Clamp01(r - d + 0.5f);
                    bool onRing = d > r - 5f;
                    Color c = onRing ? new Color(0.92f, 0.95f, 1.0f, 0.72f * outerA) : new Color(0.08f, 0.11f, 0.22f, 0.45f * outerA);
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeRippleRing(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            float r = half - 4f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Abs(new Vector2(x + 0.5f - half, y + 0.5f - half).magnitude - r);
                    float a = Mathf.Clamp01(3.2f - d);
                    px[y * size + x] = new Color(0.65f, 0.98f, 0.88f, a * 0.92f);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static void MakeArcGaugeSprites(out Sprite bgSp, out Sprite fillSp)
        {
            int w = 56, h = 148;
            var bgTex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var flTex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pxBg = new Color32[w * h];
            var pxFl = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                float ny = (y / (float)(h - 1)) * 2f - 1f;
                float curveCenter = 20f + (1f - ny * ny) * 14f;
                for (int x = 0; x < w; x++)
                {
                    if (y < 8 || y > h - 9) continue;
                    float dx = Mathf.Abs(x - curveCenter);
                    float outerA = Mathf.Clamp01(11f - dx);
                    if (outerA > 0f)
                    {
                        float inA = Mathf.Clamp01(8.2f - dx);
                        Color b = Color.Lerp(new Color(0.06f, 0.08f, 0.16f, 1f), new Color(0.16f, 0.20f, 0.36f, 0.88f), inA);
                        b.a *= outerA;
                        pxBg[y * w + x] = b;
                    }
                    float fillA = Mathf.Clamp01(7.6f - dx);
                    if (fillA > 0f && y >= 11 && y <= h - 12)
                    {
                        float t = (y - 11f) / (h - 23f);
                        Color fc = Color.Lerp(new Color(0.95f, 0.72f, 0.18f, 1f), new Color(0.24f, 0.92f, 0.52f, 1f), t);
                        fc.a *= fillA;
                        pxFl[y * w + x] = fc;
                    }
                }
            }
            bgTex.SetPixels32(pxBg); bgTex.Apply();
            flTex.SetPixels32(pxFl); flTex.Apply();
            bgSp = Sprite.Create(bgTex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            fillSp = Sprite.Create(flTex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeSunburst(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float r = p.magnitude / half;
                    if (r > 1f) { px[y * size + x] = new Color32(0, 0, 0, 0); continue; }
                    float ang = Mathf.Atan2(p.y, p.x);
                    float ray = Mathf.Sin(ang * 12f) > 0f ? 1f : 0.25f;
                    float falloff = Mathf.Pow(1f - r, 1.35f);
                    px[y * size + x] = new Color(0.92f, 0.88f, 1.0f, falloff * ray * 0.55f);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
