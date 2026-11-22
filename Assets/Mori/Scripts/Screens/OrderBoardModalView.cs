using UnityEngine;
using UnityEngine.UI;

namespace Mori
{
    /// <summary>
    /// 【幸存者订单板】弹窗：
    /// 复刻《疯狂水世界》主角升级经验第一出口 + 6/12/18/26/35 每日心情阶段宝箱（产钻石与传说英雄碎片）。
    /// </summary>
    public sealed class OrderBoardModalView : MonoBehaviour
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

            var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
            dimGo.transform.SetParent(_root, false);
            var dimRt = dimGo.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            dimGo.GetComponent<Image>().color = new Color(0.08f, 0.05f, 0.03f, 0.62f);
            dimGo.GetComponent<Button>().onClick.AddListener(() => _app.OpenModal(ModalKind.None));

            RectTransform panel = MoriUiKit.Node(_root, "OrderShell", new Vector2(656f, 880f), new Vector2(0f, 18f));
            MoriUiKit.SpriteImg(panel, "Bg", "ui/modal_shell", new Vector2(656f, 880f), Vector2.zero, sliced: true).raycastTarget = true;

            MoriUiKit.Label(panel, "Title", "幸存者订单板", 30, MoriUiKit.WarmCream, new Vector2(400f, 54f), new Vector2(0f, 386f));
            MoriUiKit.ActionButton(panel, "BtnClose", "ui/btn_close_red", "X",
                new Vector2(54f, 56f), new Vector2(288f, 388f), 24, sliced: false, darkText: false,
                () => _app.OpenModal(ModalKind.None));

            // 顶部每日心情阶段进度条（对标原版每日40单阶段宝箱）
            RectTransform moodBox = MoriUiKit.Node(panel, "MoodBox", new Vector2(596f, 128f), new Vector2(0f, 268f));
            MoriUiKit.SpriteImg(moodBox, "Bg", "ui/card_well_beige", new Vector2(596f, 128f), Vector2.zero, sliced: true);

            int done = _app.Sim.OrdersCompletedToday;
            MoriUiKit.Label(moodBox, "MoodTitle", $"今日已完成：{done}/35单·再交3单领橙卡碎片箱",
                18, MoriUiKit.CocoaText, new Vector2(556f, 26f), new Vector2(0f, 38f), TextAnchor.MiddleLeft, stroked: false);

            MoriUiKit.ProgressBar(moodBox, "MoodBar", 440f, 30f, Mathf.Clamp01(done / 35f),
                goldFill: true, $"营地满意度·非常满意({done}/35)", new Vector2(-62f, -12f));
            MoriUiKit.ItemSlot(moodBox, "StageChest", Rarity.Legendary, "ico_gem", "+300钻", 82f, new Vector2(228f, -4f));

            // 订单列表
            float startY = 108f;
            float gap = 166f;
            for (int i = 0; i < _app.Sim.Orders.Count; i++)
            {
                BuildOrderCard(panel, _app.Sim.Orders[i], startY - i * gap);
            }
        }

        void BuildOrderCard(Transform parent, SurvivorOrder ord, float y)
        {
            SimEngine sim = _app.Sim;
            RectTransform card = MoriUiKit.Node(parent, "Order_" + ord.slotIndex, new Vector2(596f, 152f), new Vector2(0f, y));
            MoriUiKit.SpriteImg(card, "Bg", "ui/card_row_cream", new Vector2(596f, 152f), Vector2.zero, sliced: true);

            // 左侧居民小头像与寄语
            string avatar = ord.slotIndex == 0 ? "walker_mechanic"
                : ord.slotIndex == 1 ? "walker_napper"
                : "hero_ironpot_sam";
            MoriUiKit.ItemSlot(card, "Avatar", Rarity.Rare, avatar, "", 92f, new Vector2(-232f, 14f));
            MoriUiKit.Label(card, "Npc", ord.npcName, 20, MoriUiKit.WarmCream,
                new Vector2(280f, 26f), new Vector2(-34f, 44f), TextAnchor.MiddleLeft, stroked: true);
            MoriUiKit.Label(card, "Note", ord.moodNote, 15, MoriUiKit.MutedSubText,
                new Vector2(360f, 24f), new Vector2(6f, 18f), TextAnchor.MiddleLeft, stroked: false);

            // 需求物品槽
            int haveA = sim.GetResource(ord.reqResA);
            int haveB = sim.GetResource(ord.reqResB);
            MoriUiKit.ItemSlot(card, "ReqA", haveA >= ord.reqCountA ? Rarity.Rare : Rarity.Epic,
                MoriCatalog.ResourceIcon(ord.reqResA), $"{haveA}/{ord.reqCountA}", 64f, new Vector2(-136f, -34f));
            MoriUiKit.ItemSlot(card, "ReqB", haveB >= ord.reqCountB ? Rarity.Rare : Rarity.Epic,
                MoriCatalog.ResourceIcon(ord.reqResB), $"{haveB}/{ord.reqCountB}", 64f, new Vector2(-58f, -34f));

            // 奖励展示
            MoriUiKit.Label(card, "RewardTxt", $"奖励：+{ord.rewardExp}经验·+{ord.rewardCoin}金币",
                16, MoriUiKit.CocoaText, new Vector2(236f, 26f), new Vector2(102f, -18f), TextAnchor.MiddleLeft, stroked: false);

            bool canDeliver = sim.CanDeliverOrder(ord);
            if (ord.delivered)
            {
                MoriUiKit.ActionButton(card, "BtnDone", "ui/btn_hex_grey", "已交付",
                    new Vector2(146f, 52f), new Vector2(204f, -36f), 18, sliced: false, darkText: true, null);
            }
            else if (canDeliver)
            {
                int idx = ord.slotIndex;
                MoriUiKit.ActionButton(card, "BtnDeliver", "ui/btn_hex_green", "立即交付",
                    new Vector2(146f, 54f), new Vector2(204f, -36f), 19, sliced: false, darkText: true,
                    () =>
                    {
                        sim.DeliverOrder(idx);
                        Rebuild();
                        _app.Hud.Refresh();
                    });
            }
            else
            {
                MoriUiKit.ActionButton(card, "BtnMake", "ui/btn_round_gold", "去工坊制作",
                    new Vector2(146f, 52f), new Vector2(204f, -36f), 17, sliced: true, darkText: true,
                    () => _app.OpenModal(ModalKind.Smokehouse));
            }
        }
    }
}
