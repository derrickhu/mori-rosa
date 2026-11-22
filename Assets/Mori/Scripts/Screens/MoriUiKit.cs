using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Mori
{
    /// <summary>
    /// 废土彩漫贴纸风 UGUI 构建器：
    /// 1. 全部 2x UI 切片统一 pixelsPerUnitMultiplier = 2f，圆角与 2px 深墨褐描边 (#18120E) 锐利等宽
    /// 2. 文字杜绝 ASCII 空格（兼容微信默认字体），统一挂深墨褐描边，按钮文字按实心视觉区上抬
    /// </summary>
    public static class MoriUiKit
    {
        public const float DesignW = 720f;
        public const float DesignH = 1280f;

        public static readonly Color32 InkBrown = new Color32(0x18, 0x12, 0x0E, 0xFF);
        public static readonly Color32 WarmCream = new Color32(0xFF, 0xF9, 0xEB, 0xFF);
        public static readonly Color32 CocoaText = new Color32(0x3A, 0x28, 0x1B, 0xFF);
        public static readonly Color32 MutedSubText = new Color32(0x6E, 0x56, 0x42, 0xFF);
        public static readonly Color32 AccentGold = new Color32(0xFC, 0xC5, 0x4A, 0xFF);
        public static readonly Color32 AccentGreen = new Color32(0x6D, 0xB8, 0x24, 0xFF);
        public static readonly Color32 DangerRed = new Color32(0xD9, 0x3B, 0x2E, 0xFF);

        public static void ClearChildren(Transform root)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                GameObject child = root.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    child.transform.SetParent(null, false);
                    child.SetActive(false);
                    UnityEngine.Object.Destroy(child);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(child);
                }
            }
        }

        public static RectTransform Node(Transform parent, string name, Vector2 size, Vector2 anchoredPos,
            Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin ?? new Vector2(0.5f, 0.5f);
            rt.anchorMax = anchorMax ?? new Vector2(0.5f, 0.5f);
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            rt.localScale = Vector3.one;
            return rt;
        }

        public static RectTransform TopLeftNode(Transform parent, string name, float x, float yFromTop, float w, float h)
        {
            return Node(parent, name, new Vector2(w, h), new Vector2(x, -yFromTop),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        }

        public static Image SpriteImg(Transform parent, string name, string spriteKey, Vector2 size, Vector2 pos,
            bool sliced = false, bool preserveAspect = false,
            Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null)
        {
            RectTransform rt = Node(parent, name, size, pos, anchorMin, anchorMax, pivot);
            var img = rt.gameObject.AddComponent<Image>();
            Sprite sp = MoriSprites.Load(spriteKey);
            img.sprite = sp;
            img.color = Color.white;
            if (sliced && sp != null && sp.border != Vector4.zero)
            {
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 2f;
            }
            else
            {
                img.type = Image.Type.Simple;
                img.preserveAspect = preserveAspect;
            }
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(Transform parent, string name, string content, int fontSize, Color color,
            Vector2 size, Vector2 pos, TextAnchor align = TextAnchor.MiddleCenter, bool stroked = true,
            Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null)
        {
            RectTransform rt = Node(parent, name, size, pos, anchorMin, anchorMax, pivot);
            var txt = rt.gameObject.AddComponent<Text>();
            txt.font = WxBridge.GetUiFont();
            txt.fontStyle = FontStyle.Normal;
            txt.fontSize = fontSize;
            txt.text = content;
            txt.color = color;
            txt.alignment = align;
            txt.alignByGeometry = true;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;

            if (stroked)
            {
                var outline = rt.gameObject.AddComponent<Outline>();
                outline.effectColor = InkBrown;
                outline.effectDistance = new Vector2(1.8f, -1.8f);
            }
            return txt;
        }

        public static Button ActionButton(Transform parent, string name, string spriteKey, string text,
            Vector2 size, Vector2 pos, int fontSize, bool sliced, bool darkText, UnityAction onClick,
            Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null)
        {
            Image img = SpriteImg(parent, name, spriteKey, size, pos, sliced, !sliced, anchorMin, anchorMax, pivot);
            img.raycastTarget = true;
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(onClick);

            // 按钮下沿带厚度与轻投影，文字视觉中心上抬 3px（对齐 method.md）
            Color tc = darkText ? (Color)InkBrown : (Color)WarmCream;
            Label(img.transform, "Txt", text, fontSize, tc, size, new Vector2(0f, 3f),
                TextAnchor.MiddleCenter, stroked: !darkText);
            return btn;
        }

        public static RectTransform ItemSlot(Transform parent, string name, Rarity rarity, string iconKey,
            string countText, float boxSize, Vector2 pos,
            Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null)
        {
            string slotKey = rarity == Rarity.Mythic || rarity == Rarity.Legendary ? "ui/slot_q4_orange"
                : rarity == Rarity.Epic ? "ui/slot_q3_purple"
                : rarity == Rarity.Rare ? "ui/slot_q2_blue"
                : "ui/slot_q1_green";

            Image frame = SpriteImg(parent, name, slotKey, new Vector2(boxSize, boxSize), pos,
                sliced: false, preserveAspect: true, anchorMin: anchorMin, anchorMax: anchorMax, pivot: pivot);

            float icoSize = boxSize * 0.68f;
            SpriteImg(frame.transform, "Icon", iconKey, new Vector2(icoSize, icoSize), new Vector2(0f, 3f),
                sliced: false, preserveAspect: true);

            if (!string.IsNullOrEmpty(countText))
            {
                Label(frame.transform, "Count", countText, Mathf.RoundToInt(boxSize * 0.22f), WarmCream,
                    new Vector2(boxSize - 10f, 26f), new Vector2(0f, -boxSize * 0.30f),
                    TextAnchor.MiddleRight, stroked: true);
            }
            return frame.rectTransform;
        }

        public static Image ProgressBar(Transform parent, string name, float width, float height, float ratio01,
            bool goldFill, string text, Vector2 pos)
        {
            Image track = SpriteImg(parent, name, "ui/bar_track_dark", new Vector2(width, height), pos, sliced: true);
            float pad = 4f;
            float maxFillW = Mathf.Max(0f, width - pad * 2f);
            float fillW = Mathf.Clamp01(ratio01) * maxFillW;

            if (fillW > 6f)
            {
                string fillKey = goldFill ? "ui/bar_fill_gold" : "ui/bar_fill_green";
                Image fill = SpriteImg(track.transform, "Fill", fillKey,
                    new Vector2(Mathf.Max(22f, fillW), height - pad * 2f),
                    new Vector2(pad, 0f),
                    sliced: true,
                    anchorMin: new Vector2(0f, 0.5f),
                    anchorMax: new Vector2(0f, 0.5f),
                    pivot: new Vector2(0f, 0.5f));
            }

            if (!string.IsNullOrEmpty(text))
            {
                Label(track.transform, "BarTxt", text, Mathf.RoundToInt(height * 0.58f), WarmCream,
                    new Vector2(width, height), new Vector2(0f, 1f), TextAnchor.MiddleCenter, stroked: true);
            }
            return track;
        }
    }
}
