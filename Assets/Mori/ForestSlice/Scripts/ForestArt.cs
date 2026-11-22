using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mori.ForestSlice
{
    /// <summary>
    /// 正式素材加载：优先 Resources/ForestArt/{key}（内置生图 + 本地后处理），缺了就用代码占位图。
    /// 树木统一从单张 trees_atlas 切分（向阳林缘树 + 林下压暗树 + 树根接触影），保证整片森林合批为 1 个 DrawCall。
    /// </summary>
    public static class ForestArt
    {
        public const float PPU = 256f;
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        static Sprite[] _litTrees;
        static Sprite[] _deepTrees;
        static Sprite _treeShadow;
        static Texture2D _cachedAtlasTex;

        public static bool Has(string key) => Resources.Load<Texture2D>("ForestArt/" + key) != null;

        public static Sprite Get(string key, Func<Sprite> fallback)
        {
            if (Cache.TryGetValue(key, out Sprite s) && s != null) return s;
            s = Resources.Load<Sprite>("ForestArt/" + key);
            if (s == null && fallback != null) s = fallback();
            Cache[key] = s;
            return s;
        }

        public static Texture2D Tex(string key)
        {
            var t = Resources.Load<Texture2D>("ForestArt/" + key);
            if (t != null && key.EndsWith("_tile")) t.wrapMode = TextureWrapMode.Repeat;
            return t;
        }

        public static Sprite[] Series(string prefix, int max)
        {
            var list = new List<Sprite>();
            for (int i = 0; i < max; i++)
            {
                Sprite s = Resources.Load<Sprite>("ForestArt/" + prefix + i);
                if (s != null) list.Add(s);
            }
            return list.Count > 0 ? list.ToArray() : null;
        }

        /// <summary>从单张 trees_atlas.png 切出向阳树、林内阴影树与落地接触影。</summary>
        public static bool LoadTreeAtlas(out Sprite[] lit, out Sprite[] deep, out Sprite shadow)
        {
            var tex = Resources.Load<Texture2D>("ForestArt/trees_atlas");
            if (tex == null)
            {
                lit = null;
                deep = null;
                shadow = null;
                return false;
            }
            if (_cachedAtlasTex == tex && _litTrees != null && _litTrees[0] != null)
            {
                lit = _litTrees;
                deep = _deepTrees;
                shadow = _treeShadow;
                return true;
            }

            _cachedAtlasTex = tex;
            _litTrees = new Sprite[5];
            _deepTrees = new Sprite[5];
            // build_tree_atlas.py: cell = 320x480, 最低树根像素在 cell 底边上方 8px，树根中心在 ~18px
            var rootPivot = new Vector2(0.5f, 18f / 480f);
            for (int i = 0; i < 5; i++)
            {
                _litTrees[i] = Sprite.Create(tex, new Rect(i * 320f, 512f, 320f, 480f), rootPivot, PPU, 0, SpriteMeshType.FullRect);
                _litTrees[i].name = "tree_lit_" + i;
                _deepTrees[i] = Sprite.Create(tex, new Rect(i * 320f, 0f, 320f, 480f), rootPivot, PPU, 0, SpriteMeshType.FullRect);
                _deepTrees[i].name = "tree_deep_" + i;
            }
            _treeShadow = Sprite.Create(tex, new Rect(1632f, 64f, 256f, 128f), new Vector2(0.45f, 0.52f), PPU, 0, SpriteMeshType.FullRect);
            _treeShadow.name = "tree_shadow";

            lit = _litTrees;
            deep = _deepTrees;
            shadow = _treeShadow;
            return true;
        }
    }
}
