using UnityEngine;
using UnityEngine.UI;

namespace Mori
{
    /// <summary>
    /// 【招牌玩梗英雄档案·铁锅山姆】弹窗：
    /// 展示美式废土彩漫半身立绘、25小星(5大星)养成、战斗大招与指挥大巴生产委任「一鱼两吃」定位。
    /// </summary>
    public sealed class HeroSpotlightModalView : MonoBehaviour
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

            HeroDef sam = MoriCatalog.Heroes[0];

            var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
            dimGo.transform.SetParent(_root, false);
            var dimRt = dimGo.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            dimGo.GetComponent<Image>().color = new Color(0.08f, 0.05f, 0.03f, 0.65f);
            dimGo.GetComponent<Button>().onClick.AddListener(() => _app.OpenModal(ModalKind.None));

            RectTransform panel = MoriUiKit.Node(_root, "HeroShell", new Vector2(656f, 920f), new Vector2(0f, 18f));
            MoriUiKit.SpriteImg(panel, "Bg", "ui/modal_shell", new Vector2(656f, 920f), Vector2.zero, sliced: true).raycastTarget = true;

            MoriUiKit.Label(panel, "Title", "传说英雄·铁锅山姆", 30, MoriUiKit.WarmCream, new Vector2(420f, 54f), new Vector2(0f, 406f));
            MoriUiKit.ActionButton(panel, "BtnClose", "ui/btn_close_red", "X",
                new Vector2(54f, 56f), new Vector2(288f, 408f), 24, sliced: false, darkText: false,
                () => _app.OpenModal(ModalKind.None));

            // 上半：立绘展示台 + 职业阵营战力属性
            RectTransform stage = MoriUiKit.Node(panel, "HeroStage", new Vector2(596f, 340f), new Vector2(0f, 182f));
            MoriUiKit.SpriteImg(stage, "Bg", "ui/card_well_beige", new Vector2(596f, 340f), Vector2.zero, sliced: true);

            // 铁锅山姆高清透明立绘（原图直入 Unity，不经 Figma 重采样）
            MoriUiKit.SpriteImg(stage, "Portrait", sam.portraitKey, new Vector2(285f, 336f), new Vector2(-142f, 4f), preserveAspect: true);

            MoriUiKit.Label(stage, "Tag", "【拾荒兄弟会·前排重装盾卫】", 18, MoriUiKit.AccentGold,
                new Vector2(300f, 26f), new Vector2(132f, 128f), TextAnchor.MiddleLeft, stroked: true);
            MoriUiKit.Label(stage, "Stars", "★★★☆☆·15/25小星·Lv.25", 20, MoriUiKit.WarmCream,
                new Vector2(300f, 28f), new Vector2(132f, 92f), TextAnchor.MiddleLeft, stroked: true);

            MoriUiKit.Label(stage, "Power", "综合战力：68,400", 22, MoriUiKit.CocoaText,
                new Vector2(280f, 30f), new Vector2(132f, 44f), TextAnchor.MiddleLeft, stroked: false);
            MoriUiKit.Label(stage, "Hp", $"生命上限：{sam.baseHp:N0}", 18, MoriUiKit.CocoaText,
                new Vector2(280f, 26f), new Vector2(132f, 8f), TextAnchor.MiddleLeft, stroked: false);
            MoriUiKit.Label(stage, "Atk", $"物理攻击：{sam.baseAtk:N0}", 18, MoriUiKit.CocoaText,
                new Vector2(280f, 26f), new Vector2(132f, -24f), TextAnchor.MiddleLeft, stroked: false);

            // 职业战术徽章4槽预览
            MoriUiKit.ItemSlot(stage, "Badge1", Rarity.Legendary, "ico_nav_hero", "Lv.20", 64f, new Vector2(24f, -104f));
            MoriUiKit.ItemSlot(stage, "Badge2", Rarity.Epic, "ico_blueprint", "Lv.15", 64f, new Vector2(96f, -104f));
            MoriUiKit.ItemSlot(stage, "Badge3", Rarity.Rare, "ico_coin", "70级开", 64f, new Vector2(168f, -104f));
            MoriUiKit.ItemSlot(stage, "Badge4", Rarity.Rare, "ico_gem", "90级开", 64f, new Vector2(240f, -104f));

            // 中部：双栖职能卡（战斗推图 + 营地生产委任）
            RectTransform skillCard = MoriUiKit.Node(panel, "CombatSkillCard", new Vector2(596f, 126f), new Vector2(0f, -72f));
            MoriUiKit.SpriteImg(skillCard, "Bg", "ui/card_row_cream", new Vector2(596f, 126f), Vector2.zero, sliced: true);
            MoriUiKit.Label(skillCard, "Title", $"战斗绝技·【{sam.combatSkillName}】", 20, MoriUiKit.WarmCream,
                new Vector2(540f, 28f), new Vector2(-10f, 34f), TextAnchor.MiddleLeft, stroked: true);
            MoriUiKit.Label(skillCard, "Desc", sam.combatSkillDesc, 16, MoriUiKit.CocoaText,
                new Vector2(548f, 54f), new Vector2(0f, -14f), TextAnchor.MiddleLeft, stroked: false);

            RectTransform campCard = MoriUiKit.Node(panel, "CampBoostCard", new Vector2(596f, 118f), new Vector2(0f, -210f));
            MoriUiKit.SpriteImg(campCard, "Bg", "ui/card_row_cream", new Vector2(596f, 118f), Vector2.zero, sliced: true);
            MoriUiKit.Label(campCard, "Title", "营地内政·【大巴总指挥官】(已驻守)", 20, MoriUiKit.AccentGreen,
                new Vector2(540f, 28f), new Vector2(-10f, 30f), TextAnchor.MiddleLeft, stroked: true);
            MoriUiKit.Label(campCard, "Desc", sam.campBoostDesc, 16, MoriUiKit.CocoaText,
                new Vector2(548f, 48f), new Vector2(0f, -14f), TextAnchor.MiddleLeft, stroked: false);

            // 底部按钮：100%无损重生 + 升级/升星
            MoriUiKit.ActionButton(panel, "BtnReset", "ui/btn_round_blue", "100%无损重生",
                new Vector2(210f, 62f), new Vector2(-148f, -356f), 19, sliced: true, darkText: false, null);
            MoriUiKit.ActionButton(panel, "BtnUpgrade", "ui/btn_hex_gold", "英雄升级(2,400金币)",
                new Vector2(280f, 66f), new Vector2(118f, -356f), 20, sliced: false, darkText: true, null);
        }
    }
}
