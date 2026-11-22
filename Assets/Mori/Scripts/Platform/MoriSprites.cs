using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Mori
{
    /// <summary>
    /// 统一美术加载器：
    /// 1. 优先读取已确认入库的 Resources/MoriArt/{key}
    /// 2. 编辑器样板验收阶段（美术未确认入库前），自动从仓库外 game_assets/mori-rosa/assets/final/ 读取原图，
    ///    并赋予精确的九宫格 border 与脚底中心 pivot（Y轴动态遮挡所需）。
    /// </summary>
    public static class MoriSprites
    {
        public const string ExternalFinalRoot = "/Users/rosa/rosa_games/game_assets/mori-rosa/assets/final";

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static void ClearCache()
        {
            Cache.Clear();
        }

        public static Sprite Load(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (Cache.TryGetValue(key, out Sprite hit) && hit != null) return hit;

            Sprite res = Resources.Load<Sprite>("MoriArt/" + key);
            if (res != null)
            {
                Cache[key] = res;
                return res;
            }

#if UNITY_EDITOR
            string fullPath = Path.Combine(ExternalFinalRoot, key + ".png");
            if (File.Exists(fullPath))
            {
                byte[] bytes = File.ReadAllBytes(fullPath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(bytes);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                tex.name = key;

                Vector2 pivot = ResolvePivot(key);
                Vector4 border = ResolveBorder(key);
                // 菱形比格子略大一点，盖住接缝
                float ppu = key.StartsWith("iso_") ? 242f : 100f;

                Sprite sp = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    pivot,
                    ppu,
                    0,
                    SpriteMeshType.FullRect,
                    border);
                sp.name = key;
                Cache[key] = sp;
                return sp;
            }
#endif
            return null;
        }

        public static Vector2 ResolvePivot(string key)
        {
            // 营地建筑、建地脚手架与幸存者小人统一锚在贴地脚底中心，保证 Y-Sorting 前后遮挡自然准确
            if (key.StartsWith("iso_"))
                return new Vector2(0.5f, 0.5f);
            if (key.StartsWith("b_") || key.StartsWith("plot_") || key.StartsWith("walker_") || key.StartsWith("p_"))
                return new Vector2(0.5f, 0.08f);
            if (key == "bg_horizon")
                return new Vector2(0.5f, 0.02f);
            if (key.StartsWith("ui/bubble_"))
                return new Vector2(0.5f, 0f);
            return new Vector2(0.5f, 0.5f);
        }

        public static Vector4 ResolveBorder(string key)
        {
            // Unity Sprite.border 顺序为 (left, bottom, right, top)，对应 2x 图的实际像素切线
            switch (key)
            {
                case "ui/modal_shell":
                    return new Vector4(110f, 84f, 110f, 136f);
                case "ui/card_well_beige":
                case "ui/card_row_cream":
                    return new Vector4(48f, 48f, 48f, 48f);
                case "ui/hud_quest_bar":
                    return new Vector4(52f, 36f, 52f, 36f);
                case "ui/bar_track_dark":
                    return new Vector4(28f, 16f, 28f, 16f);
                case "ui/bar_fill_green":
                case "ui/bar_fill_gold":
                    return new Vector4(24f, 12f, 24f, 12f);
                case "ui/dock_bg":
                    return new Vector4(24f, 16f, 24f, 24f);
                case "ui/btn_round_gold":
                case "ui/btn_round_green":
                case "ui/btn_round_blue":
                    return new Vector4(36f, 28f, 36f, 28f);
                default:
                    return Vector4.zero;
            }
        }
    }
}
