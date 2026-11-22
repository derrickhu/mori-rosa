using UnityEngine;
using UnityEngine.UI;

namespace Mori
{
    /// <summary>
    /// 【废土熏肉灶】工坊生产弹窗：
    /// 直观展示对标《疯狂水世界》并优化后的三大设计：
    /// 1. 英雄委任与蓝紫英雄专属提速横幅（一鱼两吃）
    /// 2. 生产队列与激励视频「开启4h自动生产+第2队列」高转化痛点位
    /// 3. 最多2层的清晰配方链 + 缺料时显式弹出「去蒸馏站」一键溯源跳转按钮
    /// </summary>
    public sealed class WorkshopModalView : MonoBehaviour
    {
        MoriApp _app;
        RectTransform _root;

        public void Init(MoriApp app, RectTransform root)
        {
            _app = app;
            _root = root;
            Rebuild();
        }

        public void Rebuild()
        {
            MoriUiKit.ClearChildren(_root);

            // 半透明暖暗遮罩
            var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
            dimGo.transform.SetParent(_root, false);
            var dimRt = dimGo.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            dimGo.GetComponent<Image>().color = new Color(0.08f, 0.05f, 0.03f, 0.62f);
            dimGo.GetComponent<Button>().onClick.AddListener(() => _app.OpenModal(ModalKind.None));

            // 铆钉暖沙米主面板壳子 (656 x 900)
            RectTransform panel = MoriUiKit.Node(_root, "ModalShell", new Vector2(656f, 900f), new Vector2(0f, 18f));
            Image shellImg = MoriUiKit.SpriteImg(panel, "ShellBg", "ui/modal_shell", new Vector2(656f, 900f), Vector2.zero, sliced: true);
            shellImg.raycastTarget = true;

            // 标题栏与关闭钮
            MoriUiKit.Label(panel, "Title", "废土熏肉灶·Lv.2", 30, MoriUiKit.WarmCream,
                new Vector2(420f, 54f), new Vector2(0f, 396f));
            MoriUiKit.ActionButton(panel, "BtnClose", "ui/btn_close_red", "X",
                new Vector2(54f, 56f), new Vector2(288f, 398f), 24, sliced: false, darkText: false,
                () => _app.OpenModal(ModalKind.None));

            // 1. 建筑预览 + 英雄提速横幅 (y = 285)
            RectTransform heroBanner = MoriUiKit.Node(panel, "BoostBanner", new Vector2(596f, 118f), new Vector2(0f, 282f));
            MoriUiKit.SpriteImg(heroBanner, "Bg", "ui/card_well_beige", new Vector2(596f, 118f), Vector2.zero, sliced: true);
            MoriUiKit.SpriteImg(heroBanner, "BuildThumb", "b_smokehouse", new Vector2(106f, 106f), new Vector2(-232f, 4f), preserveAspect: true);

            MoriUiKit.Label(heroBanner, "BoostTitle", "驻守与技能提速生效中(+40%)", 20, MoriUiKit.AccentGreen,
                new Vector2(340f, 28f), new Vector2(-4f, 28f), TextAnchor.MiddleLeft, stroked: true);
            MoriUiKit.Label(heroBanner, "BoostSub1", "·指挥大巴【铁锅山姆】全工坊耗时减免", 16, MoriUiKit.CocoaText,
                new Vector2(350f, 24f), new Vector2(-4f, -2f), TextAnchor.MiddleLeft, stroked: false);
            MoriUiKit.Label(heroBanner, "BoostSub2", "·稀有学徒【火头小布】熏肉灶专精+40%", 16, MoriUiKit.CocoaText,
                new Vector2(350f, 24f), new Vector2(-4f, -28f), TextAnchor.MiddleLeft, stroked: false);

            MoriUiKit.ActionButton(heroBanner, "BtnHero", "ui/btn_round_gold", "查看英雄",
                new Vector2(114f, 48f), new Vector2(226f, 0f), 17, sliced: true, darkText: true,
                () => _app.OpenModal(ModalKind.HeroSam));

            // 2. 生产队列槽与 IAA 自动生产解锁条 (y = 148)
            RectTransform queueBox = MoriUiKit.Node(panel, "QueueSection", new Vector2(596f, 124f), new Vector2(0f, 146f));
            MoriUiKit.SpriteImg(queueBox, "Bg", "ui/card_well_beige", new Vector2(596f, 124f), Vector2.zero, sliced: true);

            MoriUiKit.Label(queueBox, "QLabel", "当前生产队列(1/" + _app.Sim.MaxWorkshopQueueSlots + ")", 18, MoriUiKit.CocoaText,
                new Vector2(220f, 24f), new Vector2(-172f, 42f), TextAnchor.MiddleLeft, stroked: false);

            // 队列槽1：正在烤制
            MoriUiKit.ItemSlot(queueBox, "QSlot1", Rarity.Rare, "ico_smoked_ribs", "x2", 78f, new Vector2(-238f, -10f));
            MoriUiKit.ProgressBar(queueBox, "QProg", 216f, 30f, 0.68f, goldFill: false, "炭烤熏肉排·剩10秒", new Vector2(-74f, -4f));
            MoriUiKit.Label(queueBox, "QTip", "产线运转中，完成后飘出气泡", 14, MoriUiKit.MutedSubText,
                new Vector2(216f, 22f), new Vector2(-74f, -34f), TextAnchor.MiddleCenter, stroked: false);

            // 队列槽2：IAA激励广告开启自动连续生产+第2队列
            string adBtnText = _app.Sim.AutoProdAdActive ? "自动生产生效(4h)" : "看广告·开自动+第2队列";
            MoriUiKit.ActionButton(queueBox, "BtnAutoAd", "ui/btn_hex_gold", adBtnText,
                new Vector2(224f, 64f), new Vector2(170f, -8f), 17, sliced: false, darkText: true,
                () =>
                {
                    _app.Sim.ActivateAutoProdAd();
                    Rebuild();
                    _app.Hud.Refresh();
                });

            // 3. 清晰配方列表（3行，展示一阶直产与二阶缺料一键溯源跳转）
            MoriUiKit.Label(panel, "RecHeader", "工坊配方清单（跨厂依赖不超过2层）", 19, MoriUiKit.CocoaText,
                new Vector2(580f, 28f), new Vector2(0f, 60f), TextAnchor.MiddleLeft, stroked: false);

            float startY = -12f;
            float rowGap = 138f;
            for (int i = 0; i < MoriCatalog.SmokehouseRecipes.Length; i++)
            {
                BuildRecipeRow(panel, MoriCatalog.SmokehouseRecipes[i], startY - i * rowGap);
            }
        }

        void BuildRecipeRow(Transform parent, RecipeDef rec, float yPos)
        {
            SimEngine sim = _app.Sim;
            RectTransform row = MoriUiKit.Node(parent, "Recipe_" + rec.recipeId, new Vector2(596f, 124f), new Vector2(0f, yPos));
            MoriUiKit.SpriteImg(row, "RowBg", "ui/card_row_cream", new Vector2(596f, 124f), Vector2.zero, sliced: true);

            // 左侧成品品质框
            MoriUiKit.ItemSlot(row, "OutSlot", rec.quality, rec.iconKey, "x" + rec.outputCount, 88f, new Vector2(-236f, 0f));

            // 中部名称、耗时与原料配比
            float actualSec = sim.CalcActualCraftSeconds(rec);
            MoriUiKit.Label(row, "Name", $"{rec.displayName}·耗时{Mathf.RoundToInt(actualSec)}秒", 21, MoriUiKit.WarmCream,
                new Vector2(270f, 28f), new Vector2(-42f, 34f), TextAnchor.MiddleLeft, stroked: true);

            int haveA = sim.GetResource(rec.inputA);
            bool okA = haveA >= rec.countA;
            BuildIngredientChip(row, -172f, -16f, rec.inputA, haveA, rec.countA, okA);

            bool okB = true;
            if (rec.countB > 0)
            {
                int haveB = sim.GetResource(rec.inputB);
                okB = haveB >= rec.countB;
                BuildIngredientChip(row, -34f, -16f, rec.inputB, haveB, rec.countB, okB);
            }

            bool unlocked = sim.GetPlot(2).level >= rec.unlockBuildingLv;
            if (!unlocked)
            {
                MoriUiKit.ActionButton(row, "BtnLock", "ui/btn_hex_grey", $"熏肉灶Lv.{rec.unlockBuildingLv}解锁",
                    new Vector2(164f, 56f), new Vector2(196f, 0f), 16, sliced: false, darkText: true, null);
            }
            else if (!okA || !okB)
            {
                // 核心体验优化：缺料不让玩家满图瞎找，直接给出对应工坊一键溯源按钮
                string jumpHint = !okB ? "缺水·去蒸馏站" : "缺肉·去哨塔";
                MoriUiKit.ActionButton(row, "BtnTrace", "ui/btn_round_blue", jumpHint,
                    new Vector2(164f, 56f), new Vector2(196f, 6f), 17, sliced: true, darkText: false,
                    () =>
                    {
                        // 点击溯源自动收取蒸馏站现成纯净水并刷新
                        sim.CollectPlotOutput(4, out _, out _);
                        Rebuild();
                        _app.Hud.Refresh();
                    });
                MoriUiKit.Label(row, "TraceSub", rec.sourceHintB, 13, MoriUiKit.MutedSubText,
                    new Vector2(168f, 20f), new Vector2(196f, -34f), TextAnchor.MiddleCenter, stroked: false);
            }
            else
            {
                MoriUiKit.ActionButton(row, "BtnCraft", "ui/btn_hex_green", "开始制作",
                    new Vector2(164f, 58f), new Vector2(196f, 0f), 19, sliced: false, darkText: true,
                    () =>
                    {
                        sim.EnqueueSmokehouse(rec, out _);
                        Rebuild();
                        _app.Hud.Refresh();
                    });
            }
        }

        static void BuildIngredientChip(Transform parent, float x, float y, ResourceId res, int have, int need, bool enough)
        {
            RectTransform chip = MoriUiKit.Node(parent, "Ing_" + res, new Vector2(128f, 44f), new Vector2(x + 56f, y));
            MoriUiKit.SpriteImg(chip, "Bg", "ui/card_well_beige", new Vector2(128f, 44f), Vector2.zero, sliced: true);
            MoriUiKit.SpriteImg(chip, "Ico", MoriCatalog.ResourceIcon(res), new Vector2(34f, 34f), new Vector2(-40f, 0f), preserveAspect: true);
            Color numCol = enough ? (Color)MoriUiKit.CocoaText : (Color)MoriUiKit.DangerRed;
            MoriUiKit.Label(chip, "Cnt", $"{have}/{need}", 17, numCol,
                new Vector2(80f, 30f), new Vector2(14f, 0f), TextAnchor.MiddleCenter, stroked: false);
        }
    }
}
