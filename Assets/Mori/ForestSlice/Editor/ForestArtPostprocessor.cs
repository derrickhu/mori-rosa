using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mori.ForestSlice.EditorTools
{
    /// <summary>ForestArt 导入设置：平铺图走 Default + Repeat；树木图集走 Default + Clamp 2048；其余为 Sprite PPU 256。</summary>
    public sealed class ForestArtPostprocessor : AssetPostprocessor
    {
        const string Root = "Assets/Mori/ForestSlice/Resources/ForestArt/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            string key = Path.GetFileNameWithoutExtension(assetPath);
            var imp = (TextureImporter)assetImporter;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.maxTextureSize = 2048;
            imp.alphaIsTransparency = true;

            if (key.EndsWith("_tile"))
            {
                imp.textureType = TextureImporterType.Default;
                imp.wrapMode = TextureWrapMode.Repeat;
                imp.alphaIsTransparency = false;
                return;
            }

            if (key == "trees_atlas" || key.Contains("_puppet_atlas"))
            {
                imp.textureType = TextureImporterType.Default;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.alphaIsTransparency = true;
                return;
            }

            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.spritePixelsPerUnit = ForestArt.PPU;
            var st = new TextureImporterSettings();
            imp.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect;
            st.spriteAlignment = (int)SpriteAlignment.Custom;
            st.spritePivot = PivotFor(key);
            imp.SetTextureSettings(st);
        }

        static Vector2 PivotFor(string key)
        {
            if (key.StartsWith("ico_") || key.StartsWith("dock_") || key.StartsWith("btn_") ||
                key.StartsWith("rec_") || key.StartsWith("bust_") || key.StartsWith("fx_") ||
                key == "crisis_horde_bg" || key == "cargo_stack_unit")
                return new Vector2(0.5f, 0.5f);
            if (key.StartsWith("prop_")) return new Vector2(0.5f, 0.12f);
            if (key.StartsWith("farm_") || key.StartsWith("city_")) return new Vector2(0.5f, 0.5f);
            if (key == "fishing_dock") return new Vector2(0.5f, 0.3f);
            if (key == "road_gate") return new Vector2(0.5f, 0.2f);
            if (key.StartsWith("tuft_")) return new Vector2(0.5f, 0.2f);
            if (key.StartsWith("tree_")) return new Vector2(0.5f, 0.035f);
            if (key == "deer" || key == "boar" || key.StartsWith("zombie_") || key.StartsWith("survivor_"))
                return new Vector2(0.5f, 0.04f);
            return new Vector2(0.5f, 0.08f);
        }
    }
}
