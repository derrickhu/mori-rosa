using System.Collections.Generic;
using UnityEngine;

namespace Mori.ForestSlice
{
    /// <summary>
    /// 暖调末日深山漫画风 UI 皮肤与图集加载器：
    /// 1. 生成高精度抗锯齿 9-Slice 漫画描边底板（深炭褐木铁弹窗外框、暖羊皮纸内衬、果冻立体按钮、半透明胶囊、白底漫画对话泡、虚拟摇杆、弧形钓鱼张力槽、放射光芒）
    /// 2. 统一检索 Resources/ForestArt/ 下的手绘资源图标、底部 Dock 图标、角色半身立绘与危机海报。
    /// </summary>
    public static class ForestComicUiSkin
    {
        static bool _ready;

        public static Sprite DialogOuter { get; private set; }      // 炭褐木铁粗描边弹窗外框
        public static Sprite ParchmentPanel { get; private set; }   // 暖米白帆布/羊皮纸内衬面板
        public static Sprite SubSlot { get; private set; }          // 配方/需求浅灰米内凹槽
        public static Sprite SubSlotSelected { get; private set; }  // 荧光绿高亮选中槽（对标切鱼厂选中框）
        public static Sprite DarkPill { get; private set; }         // 左上HUD/左侧资源栈/中央飘字深色半透明胶囊
        public static Sprite QuestCard { get; private set; }        // 左下浮动主线任务卡（进行中）
        public static Sprite QuestCardDone { get; private set; }    // 左下浮动主线任务卡（已完成绿光底）
        public static Sprite SpeechBubble { get; private set; }     // 头顶白底黑边漫画台词/需求气泡

        public static Sprite BtnBlue { get; private set; }          // 建造 / 生产 / 前往 立体蓝钮
        public static Sprite BtnGreen { get; private set; }         // 满足TA / 领取 / 改造 立体绿钮
        public static Sprite BtnOrange { get; private set; }        // 加速 / 出发搜刮 立体橙黄钮
        public static Sprite BtnRed { get; private set; }           // 关闭 / 卸任 立体红钮
        public static Sprite BtnGrey { get; private set; }          // 置灰不可点立体钮

        public static Sprite CircleActionRing { get; private set; } // 建筑脚下 / 甩杆收线大圆钮底托
        public static Sprite RedDotBadge { get; private set; }      // 醒目小红点提示
        public static Sprite CrossBadge { get; private set; }       // 条件未满足红叉 ✖
        public static Sprite JoystickRing { get; private set; }     // 虚拟摇杆底座环
        public static Sprite JoystickThumb { get; private set; }    // 虚拟摇杆摇球
        public static Sprite ArcBarBg { get; private set; }         // 钓鱼竖向弧形张力槽底框
        public static Sprite ArcBarFill { get; private set; }       // 钓鱼竖向弧形张力槽绿条
        public static Sprite Sunburst { get; private set; }         // 恭喜获得 / 新英雄放射光效
        public static Sprite WhiteSoftCircle { get; private set; }  // 波纹点击圈

        static Font _font;
        static readonly Dictionary<string, Sprite> UiSpriteCache = new Dictionary<string, Sprite>();

        public static Font UiFont
        {
            get
            {
                if (_font != null) return _font;
                string[] candidates = { "PingFang SC", "Heiti SC", "Hiragino Sans GB", "Microsoft YaHei", "Arial Unicode MS", "Arial" };
                _font = Font.CreateDynamicFontFromOSFont(candidates, 24);
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return _font;
            }
        }

        public static void Ensure()
        {
            if (_ready && DialogOuter != null) return;
            _ready = true;

            // 1. 弹窗主外框：深炭褐木铁质感 + 3.5px 纯黑手绘墨线 + 顶部暖高光 + 底部立体厚边
            DialogOuter = Make9SliceBevel(
                96, 20f, 3.5f, 5f,
                new Color32(66, 52, 43, 252),
                new Color32(43, 33, 27, 252),
                new Color32(23, 17, 13, 255),
                new Color32(115, 92, 75, 160),
                new Color32(28, 20, 16, 255),
                border: 24);

            // 2. 暖米白内衬（高对比易读，对标参考视频白内衬区）
            ParchmentPanel = Make9SliceBevel(
                80, 14f, 2.2f, 2f,
                new Color32(248, 243, 233, 255),
                new Color32(238, 230, 216, 255),
                new Color32(62, 48, 37, 220),
                new Color32(255, 255, 255, 200),
                new Color32(212, 201, 184, 255),
                border: 20);

            // 3. 槽位小卡与荧光绿选中框
            SubSlot = Make9SliceBevel(
                64, 12f, 2.0f, 2f,
                new Color32(231, 223, 209, 255),
                new Color32(219, 209, 193, 255),
                new Color32(112, 96, 80, 235),
                new Color32(248, 244, 236, 150),
                new Color32(192, 180, 163, 255),
                border: 16);

            SubSlotSelected = Make9SliceBevel(
                64, 12f, 3.5f, 2f,
                new Color32(238, 255, 232, 255),
                new Color32(214, 248, 205, 255),
                new Color32(54, 212, 68, 255),
                new Color32(255, 255, 255, 220),
                new Color32(128, 226, 132, 255),
                border: 16);

            // 4. 半透明深色 HUD 胶囊与任务浮窗
            DarkPill = Make9SliceBevel(
                64, 16f, 2.2f, 2f,
                new Color32(34, 28, 23, 216),
                new Color32(22, 18, 15, 224),
                new Color32(14, 11, 9, 240),
                new Color32(120, 102, 82, 95),
                new Color32(12, 9, 7, 230),
                border: 18);

            QuestCard = Make9SliceBevel(
                72, 16f, 2.5f, 3f,
                new Color32(42, 36, 30, 232),
                new Color32(27, 23, 19, 238),
                new Color32(18, 14, 11, 250),
                new Color32(148, 125, 94, 120),
                new Color32(16, 12, 9, 245),
                border: 20);

            QuestCardDone = Make9SliceBevel(
                72, 16f, 2.8f, 3f,
                new Color32(52, 156, 76, 242),
                new Color32(32, 112, 52, 246),
                new Color32(16, 42, 20, 255),
                new Color32(158, 245, 172, 180),
                new Color32(20, 74, 34, 255),
                border: 20);

            SpeechBubble = MakeSpeechBubbleTex(96, 68);

            // 5. 高饱和漫画果冻立体按钮（带顶部亮白反光条与底部立体厚唇）
            BtnBlue = MakeJellyButton(80, 16f, new Color32(74, 156, 255, 255), new Color32(37, 106, 219, 255), new Color32(20, 64, 148, 255));
            BtnGreen = MakeJellyButton(80, 16f, new Color32(84, 219, 78, 255), new Color32(41, 174, 46, 255), new Color32(21, 112, 25, 255));
            BtnOrange = MakeJellyButton(80, 16f, new Color32(255, 186, 56, 255), new Color32(232, 128, 22, 255), new Color32(152, 72, 8, 255));
            BtnRed = MakeJellyButton(80, 16f, new Color32(244, 88, 78, 255), new Color32(204, 48, 42, 255), new Color32(128, 24, 20, 255));
            BtnGrey = MakeJellyButton(80, 16f, new Color32(142, 138, 132, 255), new Color32(106, 102, 96, 255), new Color32(68, 64, 60, 255));

            // 6. 圆形操作钮、红点、红叉、摇杆、钓鱼弧形槽、放射光芒
            CircleActionRing = MakeCircleBadge(128, new Color32(36, 48, 76, 242), new Color32(246, 238, 216, 255), new Color32(244, 188, 66, 255));
            RedDotBadge = MakeCircleBadge(36, new Color32(238, 52, 44, 255), new Color32(255, 255, 255, 255), new Color32(255, 140, 130, 255));
            CrossBadge = MakeCrossIcon(48);
            JoystickRing = MakeJoystickBase(160);
            JoystickThumb = MakeCircleBadge(80, new Color32(248, 244, 234, 235), new Color32(42, 34, 28, 220), new Color32(255, 212, 112, 200));
            WhiteSoftCircle = MakeRippleRing(96);
            MakeArcGaugeSprites(out var arcBg, out var arcFill);
            ArcBarBg = arcBg;
            ArcBarFill = arcFill;
            Sunburst = MakeSunburst(256);
        }

        // ---------------- 手绘图标与立绘检索 ----------------
        public static Sprite GetArt(string key, Sprite fallback = null)
        {
            if (string.IsNullOrEmpty(key)) return fallback;
            if (UiSpriteCache.TryGetValue(key, out Sprite s) && s != null) return s;
            s = Resources.Load<Sprite>("ForestArt/" + key);
            if (s == null)
            {
                Texture2D t = Resources.Load<Texture2D>("ForestArt/" + key);
                if (t != null)
                    s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
            }
            if (s != null) UiSpriteCache[key] = s;
            return s != null ? s : fallback;
        }

        static readonly Dictionary<SurvRes, Sprite> ProcResIcons = new Dictionary<SurvRes, Sprite>();

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

        static Sprite MakeCrossIcon(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            Color ink = new Color32(42, 14, 12, 255);
            Color red = new Color32(242, 64, 56, 255);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float d1 = SegDist(new Vector2(-13f, -13f), new Vector2(13f, 13f), p);
                    float d2 = SegDist(new Vector2(-13f, 13f), new Vector2(13f, -13f), p);
                    float d = Mathf.Min(d1, d2);
                    float a = Mathf.Clamp01(6.2f - d);
                    if (a <= 0f) { px[y * size + x] = new Color32(0, 0, 0, 0); continue; }
                    Color c = Color.Lerp(ink, red, Mathf.Clamp01(3.8f - d));
                    c.a *= a;
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static float SegDist(Vector2 a, Vector2 b, Vector2 p)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + ab * t);
        }

        static Sprite MakeProceduralResBadge(SurvRes r)
        {
            int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[s * s];
            float half = s * 0.5f;
            Color32 main = new Color32(168, 132, 86, 255);
            Color32 sub = new Color32(245, 226, 182, 255);
            int motif = 0; // 0=blueprint/grid, 1=nails, 2=ingot, 3=pipe, 4=cloth/shirt, 5=herb/med, 6=bed

            switch (r)
            {
                case SurvRes.Blueprint: main = new Color32(46, 118, 232, 255); sub = new Color32(236, 248, 255, 255); motif = 0; break;
                case SurvRes.Nails: main = new Color32(132, 146, 162, 255); sub = new Color32(218, 228, 238, 255); motif = 1; break;
                case SurvRes.IronIngot: main = new Color32(98, 112, 128, 255); sub = new Color32(188, 202, 218, 255); motif = 2; break;
                case SurvRes.MetalPipe: main = new Color32(198, 72, 58, 255); sub = new Color32(238, 148, 132, 255); motif = 3; break;
                case SurvRes.Cloth: main = new Color32(208, 192, 158, 255); sub = new Color32(246, 238, 218, 255); motif = 4; break;
                case SurvRes.Pelt: main = new Color32(158, 98, 54, 255); sub = new Color32(226, 174, 118, 255); motif = 4; break;
                case SurvRes.LinenShirt: main = new Color32(82, 148, 198, 255); sub = new Color32(188, 226, 248, 255); motif = 4; break;
                case SurvRes.LeatherBoots: main = new Color32(138, 78, 42, 255); sub = new Color32(212, 152, 98, 255); motif = 4; break;
                case SurvRes.FurCoat: main = new Color32(176, 108, 48, 255); sub = new Color32(246, 236, 214, 255); motif = 4; break;
                case SurvRes.PlateArmor: main = new Color32(78, 96, 118, 255); sub = new Color32(238, 168, 62, 255); motif = 2; break;
                case SurvRes.Herb: main = new Color32(64, 168, 76, 255); sub = new Color32(178, 238, 162, 255); motif = 5; break;
                case SurvRes.HerbPoultice: main = new Color32(86, 182, 98, 255); sub = new Color32(246, 242, 226, 255); motif = 5; break;
                case SurvRes.WarmTea: main = new Color32(216, 132, 48, 255); sub = new Color32(252, 218, 152, 255); motif = 5; break;
                case SurvRes.MedKit: main = new Color32(226, 62, 58, 255); sub = new Color32(255, 250, 246, 255); motif = 5; break;
                case SurvRes.WoodBed: main = new Color32(166, 108, 58, 255); sub = new Color32(236, 224, 196, 255); motif = 6; break;
            }

            Color ink = new Color32(26, 20, 15, 255);
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

        // ---------------- SDF 抗锯齿贴图合成 ----------------
        static float SdfRoundBox(Vector2 p, Vector2 b, float r)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x) - b.x + r, Mathf.Abs(p.y) - b.y + r);
            return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
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
                    // 底部立体暗边
                    float dShiftedUp = SdfRoundBox(p - new Vector2(0f, lip), box - new Vector2(stroke, stroke), Mathf.Max(2f, radius - stroke));
                    float bodyMask = Mathf.Clamp01(0.5f - dShiftedUp);

                    // 顶部细高光边
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
            Color borderCol = new Color32(22, 16, 12, 255);
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
                    // 上半部漫画高光横条
                    if (tY > 0.68f && tY < 0.87f && Mathf.Abs(p.x) < half - 11f)
                        face = Color.Lerp(face, Color.white, 0.28f);

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
            Color ink = new Color32(28, 22, 18, 255);
            Color paper = new Color32(255, 253, 248, 250);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float dBody = SdfRoundBox(p - center, halfBox, 12f);

                    // 底部中央小三角尖角
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
            Color ink = new Color32(18, 14, 11, 255);

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
                    Color c = onRing ? new Color(1f, 0.96f, 0.88f, 0.65f * outerA) : new Color(0.08f, 0.08f, 0.08f, 0.42f * outerA);
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
                    px[y * size + x] = new Color(1f, 0.96f, 0.78f, a * 0.9f);
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
                float ny = (y / (float)(h - 1)) * 2f - 1f; // -1..1
                float curveCenter = 20f + (1f - ny * ny) * 14f;
                for (int x = 0; x < w; x++)
                {
                    if (y < 8 || y > h - 9) continue;
                    float dx = Mathf.Abs(x - curveCenter);
                    float outerA = Mathf.Clamp01(11f - dx);
                    if (outerA > 0f)
                    {
                        float inA = Mathf.Clamp01(8.2f - dx);
                        Color b = Color.Lerp(new Color(0.08f, 0.06f, 0.05f, 1f), new Color(0.18f, 0.16f, 0.14f, 0.85f), inA);
                        b.a *= outerA;
                        pxBg[y * w + x] = b;
                    }
                    float fillA = Mathf.Clamp01(7.6f - dx);
                    if (fillA > 0f && y >= 11 && y <= h - 12)
                    {
                        float t = (y - 11f) / (h - 23f);
                        Color fc = Color.Lerp(new Color(0.95f, 0.72f, 0.18f, 1f), new Color(0.24f, 0.90f, 0.32f, 1f), t);
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
                    px[y * size + x] = new Color(1f, 0.90f, 0.52f, falloff * ray * 0.65f);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
