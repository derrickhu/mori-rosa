using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Mori.ForestSlice
{
    /// <summary>
    /// 漫画风 UGUI 构件工厂与全局反馈动效池：
    /// - 统一创建九宫格面板、粗描边漫画文字、果冻立体按钮、左下角红底返回键
    /// - 驱动屏幕中央黑底拾取飘窗流（如 [原木] 原木 x8）+ 飞入左侧资源栏弹跳 + 经验星标飞入左上角 + 跳动白手套指针
    /// </summary>
    public static class ForestUiWidgets
    {
        public static readonly Color32 InkDark = new Color32(36, 26, 18, 255);
        public static readonly Color32 InkMuted = new Color32(102, 86, 70, 255);
        public static readonly Color32 CreamLight = new Color32(252, 246, 232, 255);
        public static readonly Color32 WarmGold = new Color32(255, 214, 88, 255);
        public static readonly Color32 LushGreen = new Color32(42, 168, 54, 255);
        public static readonly Color32 AlertRed = new Color32(226, 58, 48, 255);

        public static RectTransform MakeRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent, float padL = 0f, float padB = 0f, float padR = 0f, float padT = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padL, padB);
            rt.offsetMax = new Vector2(-padR, -padT);
            return rt;
        }

        public static Image MakeSlicedImage(string name, Transform parent, Sprite sprite, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size, bool raycast = true)
        {
            RectTransform rt = MakeRect(name, parent, anchorMin, anchorMax, pivot, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = (sprite != null && sprite.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
            img.raycastTarget = raycast;
            return img;
        }

        public static Image MakeIcon(string name, Transform parent, Sprite sp, Vector2 pos, Vector2 size, Vector2? anchor = null, bool raycast = false)
        {
            Vector2 a = anchor ?? new Vector2(0.5f, 0.5f);
            RectTransform rt = MakeRect(name, parent, a, a, new Vector2(0.5f, 0.5f), pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sp;
            img.preserveAspect = true;
            img.raycastTarget = raycast;
            return img;
        }

        public static Text MakeText(string name, Transform parent, string content, int fontSize, Color color,
            TextAnchor align, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size, bool outline = true, FontStyle style = FontStyle.Bold)
        {
            RectTransform rt = MakeRect(name, parent, anchorMin, anchorMax, pivot, pos, size);
            var txt = rt.gameObject.AddComponent<Text>();
            txt.font = ForestComicUiSkin.UiFont;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = align;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;

            if (outline)
            {
                var ol = rt.gameObject.AddComponent<Outline>();
                ol.effectColor = new Color(0.08f, 0.06f, 0.04f, 0.90f);
                ol.effectDistance = new Vector2(1.5f, -1.5f);
            }
            return txt;
        }

        public static Button MakeJellyButton(string name, Transform parent, Sprite skin, string label, int fontSize,
            Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, UnityAction onClick, out Text labelOut)
        {
            Image img = MakeSlicedImage(name, parent, skin, Color.white, anchor, anchor, pivot, pos, size, true);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var cols = btn.colors;
            cols.highlightedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
            cols.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            btn.colors = cols;
            if (onClick != null) btn.onClick.AddListener(onClick);

            labelOut = MakeText("Label", img.transform, label, fontSize, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), Vector2.zero, outline: true);
            return btn;
        }

        /// <summary>左下角标志性的红底圆角返回弯箭头按钮（对标视频所有半屏/子玩法面板左下角）。</summary>
        public static Button MakeRedBackButton(Transform parent, UnityAction onClick, Vector2? customPos = null)
        {
            Vector2 pos = customPos ?? new Vector2(18f, 16f);
            Sprite sp = ForestComicUiSkin.GetArt("btn_back_red", ForestComicUiSkin.BtnRed);
            RectTransform rt = MakeRect("BtnBackRed", parent, Vector2.zero, Vector2.zero, Vector2.zero, pos, new Vector2(78f, 74f));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sp;
            img.preserveAspect = (sp != ForestComicUiSkin.BtnRed);
            img.type = (sp == ForestComicUiSkin.BtnRed) ? Image.Type.Sliced : Image.Type.Simple;

            if (sp == ForestComicUiSkin.BtnRed)
            {
                MakeText("Arrow", rt, "←", 36, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            }

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(onClick);
            return btn;
        }
    }

    /// <summary>
    /// 全局漫画白手套引导组件（呼吸弹跳 + 点击波纹圈）。
    /// </summary>
    public sealed class ComicGlovePointer
    {
        public readonly RectTransform Root;
        readonly RectTransform _handRt;
        readonly Image _rippleImg;
        float _t;

        public ComicGlovePointer(Transform parent, string name = "GlovePointer")
        {
            Root = ForestUiWidgets.MakeRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84f, 84f));
            var cg = Root.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            _rippleImg = ForestUiWidgets.MakeIcon("Ripple", Root, ForestComicUiSkin.WhiteSoftCircle, Vector2.zero, new Vector2(58f, 58f));
            Image hand = ForestUiWidgets.MakeIcon("Hand", Root, ForestComicUiSkin.GetArt("ico_glove", ForestComicUiSkin.RedDotBadge), new Vector2(20f, -24f), new Vector2(68f, 68f));
            _handRt = hand.rectTransform;
            Root.gameObject.SetActive(false);
        }

        public void ShowAt(Vector2 canvasPos, float dt)
        {
            if (!Root.gameObject.activeSelf) Root.gameObject.SetActive(true);
            Root.anchoredPosition = canvasPos;
            TickAnim(dt);
        }

        public void Hide()
        {
            if (Root.gameObject.activeSelf) Root.gameObject.SetActive(false);
        }

        public void TickAnim(float dt)
        {
            if (!Root.gameObject.activeSelf) return;
            _t += dt * 4.2f;
            float tap = Mathf.Sin(_t);
            _handRt.anchoredPosition = new Vector2(20f - tap * 5f, -24f + tap * 7f);
            float sc = 0.94f + tap * 0.08f;
            _handRt.localScale = new Vector3(sc, sc, 1f);

            float rip = Mathf.Repeat(_t * 0.35f, 1f);
            _rippleImg.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.45f, 1.35f, rip);
            _rippleImg.color = new Color(1f, 0.95f, 0.72f, (1f - rip) * 0.85f);
        }
    }
}
