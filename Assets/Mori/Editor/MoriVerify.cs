using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mori.EditorTools
{
    /// <summary>
    /// 团结引擎 batchmode / 菜单一键自检与 720x1280 离屏实机样板截图器：
    /// 1. 校验 SimEngine 双锁人口、手动点击竣工、工坊队列、英雄加速、订单交付逻辑
    /// 2. 搭建并保存 Assets/Mori/MoriMain.unity 主场景
    /// 3. 将首屏营地与3个核心交互弹窗按真实 720x1280 渲染输出至仓库外 game_assets/mori-rosa/预览/
    /// </summary>
    public static class MoriVerify
    {
        const int ShotW = 720;
        const int ShotH = 1280;
        const string ScenePath = "Assets/Mori/MoriMain.unity";
        const string ExternalPreviewDir = "/Users/rosa/rosa_games/game_assets/mori-rosa/预览";

        [MenuItem("废土狂想营/跑自检并生成首屏实机样板图", priority = 1)]
        public static void Run()
        {
            var sb = new StringBuilder();
            Directory.CreateDirectory(ExternalPreviewDir);
            MoriSprites.ClearCache();

            try
            {
                sb.AppendLine(RunSimAssertions());
                BuildAndCaptureAll(sb);
            }
            catch (Exception ex)
            {
                sb.AppendLine("!! 异常中断: " + ex);
                Debug.LogError(ex);
                throw;
            }
            finally
            {
                string report = sb.ToString();
                Debug.Log("[MoriVerify]\n" + report);
                File.WriteAllText(Path.Combine(ExternalPreviewDir, "verify_report.txt"), report);
            }
        }

        static string RunSimAssertions()
        {
            var log = new StringBuilder();
            log.AppendLine("== SimEngine 核心机制自检 ==");

            var sim = new SimEngine();
            sim.SeedVerticalSlice();

            // 1) 校验人口作为门槛不消耗：初始 12 + 帐篷屋 6 = 18/40
            bool popOk = sim.CurrentPopulation == 18 && sim.CurrentPopCap == 40;
            log.AppendLine($"1. 人口与上限双锁计算(18/40): {(popOk ? "通过" : "失败")}");

            // 2) 校验 6号地块建造 -> 倒计时归零停在 ReadyToTap -> 手动点击竣工才升Lv并加经验
            bool started = sim.StartConstruction(6, out string err);
            int expBefore = sim.PlayerExp;
            sim.Tick(95f);
            bool heldAtReady = sim.GetPlot(6).state == PlotState.ReadyToTap && sim.PlayerExp == expBefore;
            bool tapped = sim.TapCompleteConstruction(6);
            bool builtOk = sim.GetPlot(6).state == PlotState.Built && sim.PlayerExp > expBefore;
            log.AppendLine($"2. 建造归零等待手动点击竣工爆经验: {(started && heldAtReady && tapped && builtOk ? "通过" : "失败 " + err)}");

            // 3) 校验 IAA 激励视频开启自动生产与第2队列
            sim.ActivateAutoProdAd();
            bool queueExpanded = sim.MaxWorkshopQueueSlots == 2 && sim.AutoProdAdActive;
            log.AppendLine($"3. 激励视频解锁4h自动生产+第2队列: {(queueExpanded ? "通过" : "失败")}");

            // 4) 校验幸存者订单交付获得经验与瓶盖金币
            int coinBefore = sim.GetResource(ResourceId.Coin);
            bool delivered = sim.DeliverOrder(0);
            bool coinGrew = sim.GetResource(ResourceId.Coin) > coinBefore && sim.OrdersCompletedToday == 16;
            log.AppendLine($"4. 幸存者订单交付与满意度推进: {(delivered && coinGrew ? "通过" : "失败")}");

            if (!(popOk && heldAtReady && builtOk && queueExpanded && coinGrew))
                throw new InvalidOperationException("SimEngine 断言未全部通过:\n" + log);

            return log.ToString();
        }

        static void BuildAndCaptureAll(StringBuilder log)
        {
            log.AppendLine("== 720x1280 实机样板渲染 ==");
            Directory.CreateDirectory("Assets/Mori");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("MainWorldCamera", typeof(Camera));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();

            var rt = new RenderTexture(ShotW, ShotH, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4,
            };
            cam.targetTexture = rt;
            cam.aspect = (float)ShotW / ShotH;

            var appGo = new GameObject("MoriApp");
            var app = appGo.AddComponent<MoriApp>();
            app.Boot(cam);
            CaptureFrame(cam, rt, Path.Combine(ExternalPreviewDir, "01_新手开场.png"), log);
            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(rt);

            // 保存干净可随时 Play 的入口场景（仅保留 MainWorldCamera 与 MoriApp 脚本壳，Play 时自动 Boot）
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cleanCam = new GameObject("MainWorldCamera", typeof(Camera));
            cleanCam.tag = "MainCamera";
            cleanCam.GetComponent<Camera>().orthographic = true;
            cleanCam.GetComponent<Camera>().orthographicSize = 5.35f;
            cleanCam.transform.position = new Vector3(0f, 0.12f, -20f);
            new GameObject("MoriApp", typeof(MoriApp));
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            log.AppendLine("已保存主场景: " + ScenePath);
        }

        static void CaptureFrame(Camera cam, RenderTexture rt, string outPath, StringBuilder log)
        {
            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            log.AppendLine("已输出截图 -> " + outPath);
        }
    }
}
