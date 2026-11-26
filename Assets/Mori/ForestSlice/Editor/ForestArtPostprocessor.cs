using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mori.ForestSlice.EditorTools
{
    /// <summary>ForestArt 导入设置：平铺图走 Default + Repeat；树木图集走 Default + Clamp 2048；其余为 Sprite PPU 256。</summary>
    public sealed class ForestArtPostprocessor : AssetPostprocessor
    {
        const string Root = "Assets/Mori/ForestSlice/Resources/ForestArt/";
        const string DefaultAppIconPath = "Assets/Mori/AppIcon/app_icon_hoarder_512.png";

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            for (int i = 0; i < imported.Length; i++)
            {
                if (imported[i] == DefaultAppIconPath)
                {
                    var iconTex = AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultAppIconPath);
                    if (iconTex != null)
                        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { iconTex });
                    break;
                }
            }
        }

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
                ApplyWeixinCompression(imp, key, 512);
                return;
            }

            if (key == "trees_atlas" || key.Contains("_puppet_atlas"))
            {
                imp.npotScale = TextureImporterNPOTScale.None;
                imp.textureType = TextureImporterType.Default;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.alphaIsTransparency = true;
                ApplyWeixinCompression(imp, key, key == "trees_atlas" ? 2048 : 1024);
                return;
            }

            bool isAnimStrip = key.StartsWith("anim_");
            bool isWwUi = key.StartsWith("ww_");
            bool useUiMipmaps = key.StartsWith("fullbody_") || key.StartsWith("bust_");
            imp.mipmapEnabled = useUiMipmaps;
            imp.filterMode = useUiMipmaps ? FilterMode.Trilinear : FilterMode.Bilinear;
            imp.mipMapBias = useUiMipmaps ? -0.35f : 0f;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.spritePixelsPerUnit = (key == "battle_arena_bg" || isWwUi) ? 100f : ForestArt.PPU;
            imp.maxTextureSize = isWwUi ? (key == "ww_hero_cabin_bg" ? 2048 : 512) : 2048;
            var st = new TextureImporterSettings();
            imp.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect;
            st.spriteAlignment = (int)SpriteAlignment.Custom;
            st.spritePivot = PivotFor(key);
            imp.SetTextureSettings(st);
            int weixinMaxSize = (key == "battle_arena_bg" || key == "ww_hero_cabin_bg" || isAnimStrip)
                ? 2048
                : (isWwUi ? 512 : ((key.StartsWith("fullbody_") || key.StartsWith("loading_") || key == "crisis_horde_bg") ? 1024 : 512));
            ApplyWeixinCompression(imp, key, weixinMaxSize);
        }

        static void ApplyWeixinCompression(TextureImporter imp, string key, int maxSize)
        {
            // iOS / Android 微信小游戏 WebGL1 原生硬件支持 ASTC（且 DecompressASTC 桌面兜底开启），彻底解决 iOS 不支持 ETC2 花屏报错！
            bool highDetail = key == "battle_arena_bg" || key.StartsWith("ww_") || key.Contains("_puppet_atlas") || key.StartsWith("anim_") || key.StartsWith("fullbody_") || key.StartsWith("bust_") || key.StartsWith("ico_") || key.StartsWith("dock_");
            TextureImporterPlatformSettings ps = imp.GetPlatformTextureSettings("WeixinMiniGame");
            ps.overridden = true;
            ps.maxTextureSize = maxSize;
            ps.format = highDetail ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.ASTC_6x6;
            ps.compressionQuality = 50;
            ps.crunchedCompression = false;
            ps.textureCompression = TextureImporterCompression.Compressed;
            imp.SetPlatformTextureSettings(ps);
        }

        static Vector2 PivotFor(string key)
        {
            if (key.StartsWith("ww_") || key.StartsWith("ico_") || key.StartsWith("dock_") || key.StartsWith("btn_") ||
                key.StartsWith("rec_") || key.StartsWith("bust_") || key.StartsWith("fullbody_") || key.StartsWith("fx_") ||
                key.StartsWith("proj_") || key.StartsWith("loading_") || key.StartsWith("wx_icon_") ||
                key == "crisis_horde_bg" || key == "battle_arena_bg" || key == "cargo_stack_unit" || key == "city_gold_airdrop")
                return new Vector2(0.5f, 0.5f);
            if (key.StartsWith("prop_")) return new Vector2(0.5f, 0.12f);
            if (key.StartsWith("farm_") || key.StartsWith("city_")) return new Vector2(0.5f, 0.5f);
            if (key == "fishing_dock") return new Vector2(0.5f, 0.3f);
            if (key == "road_gate") return new Vector2(0.5f, 0.2f);
            if (key.StartsWith("tuft_")) return new Vector2(0.5f, 0.2f);
            if (key.StartsWith("tree_")) return new Vector2(0.5f, 0.035f);
            if (key == "deer" || key == "boar" || key.StartsWith("zombie_") || key.StartsWith("survivor_") || key.StartsWith("villager_"))
                return new Vector2(0.5f, 0.04f);
            return new Vector2(0.5f, 0.08f);
        }
    }
}
