using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WeChatWASM;

namespace Mori.ForestSlice.EditorTools
{
    /// <summary>
    /// 《末日囤货狂》微信小游戏一键导出器：
    /// 既可通过顶部菜单【末日囤货狂/打包导出微信小游戏】手动触发，也可通过 /tmp/mori_wx_build.flag 自动触发。
    /// </summary>
    [InitializeOnLoad]
    public static class WeChatMiniGameBuilder
    {
        const string BuildFlag = "/tmp/mori_wx_build.flag";
        const string ResultPath = "/tmp/mori_wx_build_result.txt";
        const string ScenePath = "Assets/Mori/ForestSlice/ForestSlice.unity";
        const string CoverPath = "Assets/Mori/AppIcon/background.jpg";
        const string AppIconPath = "Assets/Mori/AppIcon/app_icon_hoarder_512.png";
        const string WeChatAppId = "wx06d4ac75bc87dec2";
        const string DefaultCdnUrl = "https://726f-rosa-env-d7grf78r5dbd37323-1414200063.tcb.qcloud.la/mori-rosa/StreamingAssets/";

        static double _checkAt;

        static WeChatMiniGameBuilder()
        {
            _checkAt = EditorApplication.timeSinceStartup + 2.0;
            EditorApplication.update += PollFlag;
        }

        static void PollFlag()
        {
            if (EditorApplication.timeSinceStartup < _checkAt) return;
            _checkAt = EditorApplication.timeSinceStartup + 1.5;

            if (!File.Exists(BuildFlag)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return; // 等下一拍完全退回 EditMode 再执行打包
            }

            File.Delete(BuildFlag);
            RunExportInternal();
        }

        [MenuItem("末日囤货狂/打包导出微信小游戏 (wechat-minigame)", priority = 80)]
        public static void ExportFromMenu()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                File.WriteAllText(BuildFlag, "1");
                return;
            }
            RunExportInternal();
        }

        public static void RunExportBatch()
        {
            RunExportInternal();
        }

        static void RunExportInternal()
        {
            try
            {
                File.WriteAllText(ResultPath, "RUNNING\n");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                if (File.Exists(CoverPath))
                    AssetDatabase.ImportAsset(CoverPath, ImportAssetOptions.ForceSynchronousImport);
                if (File.Exists(AppIconPath))
                    AssetDatabase.ImportAsset(AppIconPath, ImportAssetOptions.ForceSynchronousImport);
                EnsureWeixinTextureCompression();

                // 1. 确保默认场景存在且作为唯一打包首场景
                if (!File.Exists(ScenePath))
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    var c = new GameObject("MainCamera", typeof(Camera)) { tag = "MainCamera" };
                    new GameObject("ForestSliceBoot", typeof(ForestSliceBoot));
                    EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
                }
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(ScenePath, true)
                };

                // 2. 同步产品名称、竖屏朝向与选定 B 方案默认图标
                PlayerSettings.productName = "末日囤货狂";
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
                var iconTex = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
                if (iconTex != null)
                    PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { iconTex });

                // 3. 同步微信小游戏转换器配置（输出至当前工程 wechat-minigame/）
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                string dstPath = Path.Combine(projectRoot, "wechat-minigame");
                Directory.CreateDirectory(dstPath);

                WXEditorScriptObject conf = UnityUtil.GetEditorConf();
                conf.ProjectConf.projectName = "mori-rosa";
                conf.ProjectConf.Appid = WeChatAppId;
                conf.ProjectConf.relativeDST = "wechat-minigame";
                conf.ProjectConf.DST = dstPath;
                conf.ProjectConf.CDN = DefaultCdnUrl;
                conf.ProjectConf.Orientation = WXScreenOritation.Portrait;
                conf.ProjectConf.assetLoadType = 0; // CDN 加载首资源包（同 black-rosa CloudBase COS 方案），代码包仅 ~5MB，后续内容无限扩展！
                conf.ProjectConf.compressDataPackage = true;
                conf.ProjectConf.VideoUrl = string.Empty;
                conf.ProjectConf.bgImageSrc = CoverPath;
                conf.ProjectConf.MemorySize = 320;
                conf.ProjectConf.HideAfterCallMain = true;
                conf.SDKOptions.PreloadWXFont = false; // 已内嵌 MoriBold.ttf，关闭 preloadWXFont 避免真机 font/index.js 抛 MiniProgramError "invalid usage"
                conf.CompileOptions.DevelopBuild = false;
                conf.CompileOptions.CleanBuild = false;
                conf.CompileOptions.Il2CppOptimizeSize = true;

                EditorUtility.SetDirty(conf);
                AssetDatabase.SaveAssets();

                // 清理旧分包模式残留的本地 data-package 大文件，确保微信小游戏代码包保持 ~5MB 超轻量
                string oldSubpkgDataDir = Path.Combine(dstPath, "minigame", "data-package");
                if (Directory.Exists(oldSubpkgDataDir))
                {
                    foreach (string f in Directory.GetFiles(oldSubpkgDataDir, "*.bin*"))
                        File.Delete(f);
                }

                Debug.Log("[WeChatMiniGameBuilder] Starting WXConvertCore.DoExport (CDN mode) -> " + dstPath);
                WXConvertCore.WXExportError err = WXConvertCore.DoExport(true);
                if (err == WXConvertCore.WXExportError.SUCCEED)
                {
                    if (File.Exists(CoverPath))
                    {
                        string kitBg = Path.Combine(dstPath, "minigame", "wx-game-kit", "images", "background.jpg");
                        string rootBg = Path.Combine(dstPath, "minigame", "images", "background.jpg");
                        if (Directory.Exists(Path.GetDirectoryName(kitBg))) File.Copy(CoverPath, kitBg, true);
                        if (Directory.Exists(Path.GetDirectoryName(rootBg))) File.Copy(CoverPath, rootBg, true);
                    }
                    EnsureExportedAppId(Path.Combine(dstPath, "minigame"));
                }
                bool uploaded = err == WXConvertCore.WXExportError.SUCCEED && UploadDataPackageToCdn(projectRoot);
                string msg = $"RESULT={err}\nCDN_UPLOADED={uploaded}\nCDN={DefaultCdnUrl}\nDST={dstPath}\nTIME={DateTime.Now:O}\n";
                File.WriteAllText(ResultPath, msg);
                Debug.Log("[WeChatMiniGameBuilder] Finished with result: " + err + ", cdnUploaded=" + uploaded);
            }
            catch (Exception ex)
            {
                string errText = $"EXCEPTION={ex}\n";
                File.WriteAllText(ResultPath, errText);
                Debug.LogException(ex);
            }
        }

        static void EnsureExportedAppId(string minigameRoot)
        {
            string projCfg = Path.Combine(minigameRoot, "project.config.json");
            if (File.Exists(projCfg))
            {
                string txt = File.ReadAllText(projCfg);
                txt = txt.Replace("\"appid\": \"\"", $"\"appid\": \"{WeChatAppId}\"")
                         .Replace("\"projectname\": \"mori-rosa\"", "\"projectname\": \"末日囤货狂\"");
                File.WriteAllText(projCfg, txt);
            }
            string kitCfg = Path.Combine(minigameRoot, "wx-game-kit", "config.json");
            if (File.Exists(kitCfg))
            {
                string txt = File.ReadAllText(kitCfg);
                txt = txt.Replace("\"appID\": \"\"", $"\"appID\": \"{WeChatAppId}\"");
                File.WriteAllText(kitCfg, txt);
            }
        }

        static bool UploadDataPackageToCdn(string projectRoot)
        {
            string[] pyCandidates = { "/opt/homebrew/bin/python3", "/usr/local/bin/python3", "/usr/bin/python3" };
            string python = null;
            for (int i = 0; i < pyCandidates.Length; i++)
            {
                if (File.Exists(pyCandidates[i])) { python = pyCandidates[i]; break; }
            }
            if (python == null)
            {
                Debug.LogError("[WeChatMiniGameBuilder] 找不到 python3，无法自动上传 CDN 首包");
                return false;
            }

            string script = Path.Combine(projectRoot, "tools/upload_wx_data.py");
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = python,
                Arguments = "\"" + script + "\"",
                WorkingDirectory = projectRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using (var proc = System.Diagnostics.Process.Start(psi))
            {
                string stdout = proc.StandardOutput.ReadToEnd();
                string stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                if (!string.IsNullOrEmpty(stdout)) Debug.Log("[CDN Upload] " + stdout.Trim());
                if (proc.ExitCode != 0)
                {
                    Debug.LogError("[CDN Upload Failed] " + stderr.Trim());
                    return false;
                }
                return true;
            }
        }

        static void EnsureWeixinTextureCompression()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Mori" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.StartsWith("Assets/Mori/AppIcon/")) continue;
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                string key = Path.GetFileNameWithoutExtension(path);
                bool isAnimStrip = key.StartsWith("anim_");
                bool isPuppetAtlas = key.Contains("_puppet_atlas");
                bool isFullBody = key.StartsWith("fullbody_");
                bool useUiMipmaps = isFullBody || key.StartsWith("bust_");
                int maxSz = (key == "trees_atlas" || key == "battle_arena_bg" || key == "ww_hero_cabin_bg" || isAnimStrip)
                    ? 2048
                    : (key.StartsWith("ww_") ? 512 : ((isPuppetAtlas || isFullBody || key.StartsWith("loading_") || key == "crisis_horde_bg") ? 1024 : 512));
                bool highDetail = key == "battle_arena_bg" || key.StartsWith("ww_") || isPuppetAtlas || isAnimStrip || isFullBody || key.StartsWith("bust_") || key.StartsWith("ico_") || key.StartsWith("dock_");
                TextureImporterFormat targetFmt = highDetail ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.ASTC_6x6;
                var ps = imp.GetPlatformTextureSettings("WeixinMiniGame");
                bool needNpotFix = (isAnimStrip || isPuppetAtlas) && imp.npotScale != TextureImporterNPOTScale.None;
                bool needMipFix = useUiMipmaps && (!imp.mipmapEnabled || imp.filterMode != FilterMode.Trilinear);
                if (needNpotFix || needMipFix || !ps.overridden || ps.format != targetFmt || ps.maxTextureSize != maxSz || ps.crunchedCompression)
                {
                    if (needNpotFix)
                    {
                        imp.npotScale = TextureImporterNPOTScale.None;
                        imp.wrapMode = TextureWrapMode.Clamp;
                        imp.filterMode = FilterMode.Bilinear;
                        imp.maxTextureSize = 2048;
                    }
                    if (useUiMipmaps)
                    {
                        imp.mipmapEnabled = true;
                        imp.filterMode = FilterMode.Trilinear;
                        imp.mipMapBias = -0.35f;
                    }
                    ps.overridden = true;
                    ps.maxTextureSize = maxSz;
                    ps.format = targetFmt;
                    ps.compressionQuality = 50;
                    ps.crunchedCompression = false;
                    ps.textureCompression = TextureImporterCompression.Compressed;
                    imp.SetPlatformTextureSettings(ps);
                    imp.SaveAndReimport();
                }
            }
        }
    }
}
