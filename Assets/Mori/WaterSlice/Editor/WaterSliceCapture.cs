using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mori.WaterSlice.EditorTools
{
    /// <summary>
    /// 水上样板：导入设置 + batchmode 截图。
    /// Tuanjie -batchmode -quit -projectPath ... -executeMethod Mori.WaterSlice.EditorTools.WaterSliceCapture.Run
    /// </summary>
    public sealed class WaterArtPostprocessor : AssetPostprocessor
    {
        const string Root = "Assets/Mori/WaterSlice/Resources/WaterArt/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            string key = Path.GetFileNameWithoutExtension(assetPath);
            var imp = (TextureImporter)assetImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Bilinear;
            imp.wrapMode = key == "water_tile" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            imp.spritePixelsPerUnit = WaterSliceView.PPU;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.maxTextureSize = 2048;

            var st = new TextureImporterSettings();
            imp.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect;
            st.spriteAlignment = (int)SpriteAlignment.Custom;
            st.spritePivot = new Vector2(0.5f, 0.5f);
            imp.SetTextureSettings(st);
        }
    }

    public static class WaterSliceCapture
    {
        const string ScenePath = "Assets/Mori/WaterSlice/WaterSlice.unity";
        const string OutDir = "/Users/rosa/rosa_games/game_assets/mori-rosa/预览/water_slice";

        [MenuItem("废土狂想营/水上样板/重建并截图", priority = 50)]
        public static void Run()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Directory.CreateDirectory(OutDir);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("MainCamera", typeof(Camera));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();

            var rt = new RenderTexture(1080, 1920, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.aspect = 1080f / 1920f;

            var go = new GameObject("WaterSliceView");
            var view = go.AddComponent<WaterSliceView>();
            view.Build(cam);
            for (int i = 0; i < 30; i++) view.Tick(1f / 30f);

            Capture(cam, rt, Path.Combine(OutDir, "water_slice.png"));
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);

            // 可直接 Play 的样板场景
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var c2 = new GameObject("MainCamera", typeof(Camera));
            c2.tag = "MainCamera";
            new GameObject("WaterSliceBoot", typeof(WaterSliceBoot));
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            Debug.Log("[WaterSlice] done -> " + OutDir);
        }

        static void Capture(Camera cam, RenderTexture rt, string path)
        {
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
