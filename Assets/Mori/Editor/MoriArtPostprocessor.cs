using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mori.EditorTools
{
    /// <summary>
    /// 纹理导入防糊守卫（继承 black-rosa 与 method.md 规范）：
    /// 当用户确认样板后执行「同步已确认美术入库」时，强制为 Standalone / WebGL / WeixinMiniGame
    /// 勾选平台覆盖：UI 零件与图标使用未压缩 RGBA32 + MaxSize 1024，地表底图 MaxSize 2048，
    /// 彻底杜绝 2px 深墨褐描边被块压缩咬碎发糊。
    /// </summary>
    public sealed class MoriArtPostprocessor : AssetPostprocessor
    {
        const string InRepoArtRoot = "Assets/Mori/Resources/MoriArt/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(InRepoArtRoot)) return;

            string relKey = assetPath.Substring(InRepoArtRoot.Length);
            int dot = relKey.LastIndexOf('.');
            if (dot > 0) relKey = relKey.Substring(0, dot);

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = !relKey.StartsWith("camp_base_");
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 100f;

            Vector2 pivot = MoriSprites.ResolvePivot(relKey);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);

            importer.spriteBorder = MoriSprites.ResolveBorder(relKey);

            int maxSize = relKey.StartsWith("camp_base_") ? 2048 : 1024;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            ApplyPlatformOverride(importer, "Standalone", maxSize);
            ApplyPlatformOverride(importer, "WebGL", maxSize);
            ApplyPlatformOverride(importer, "WeixinMiniGame", maxSize);
        }

        static void ApplyPlatformOverride(TextureImporter importer, string platform, int maxSize)
        {
            TextureImporterPlatformSettings ps = importer.GetPlatformTextureSettings(platform);
            ps.overridden = true;
            ps.maxTextureSize = maxSize;
            ps.format = TextureImporterFormat.RGBA32;
            ps.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(ps);
        }

        [MenuItem("废土狂想营/美术管线/同步已确认美术入库 (用户确认后点)", priority = 30)]
        public static void SyncConfirmedArtToRepo()
        {
            string srcRoot = MoriSprites.ExternalFinalRoot;
            if (!Directory.Exists(srcRoot))
            {
                Debug.LogWarning("[废土狂想营] 未找到外部美术目录：" + srcRoot);
                return;
            }
            CopyDirRecursive(srcRoot, InRepoArtRoot);
            MoriSprites.ClearCache();
            AssetDatabase.Refresh();
            Debug.Log("[废土狂想营] 已将确认后的美术从 game_assets 同步至 " + InRepoArtRoot);
        }

        static void CopyDirRecursive(string srcDir, string dstDir)
        {
            Directory.CreateDirectory(dstDir);
            foreach (string file in Directory.GetFiles(srcDir, "*.png"))
            {
                string dst = Path.Combine(dstDir, Path.GetFileName(file));
                File.Copy(file, dst, overwrite: true);
            }
            foreach (string sub in Directory.GetDirectories(srcDir))
            {
                CopyDirRecursive(sub, Path.Combine(dstDir, Path.GetFileName(sub)));
            }
        }
    }
}
