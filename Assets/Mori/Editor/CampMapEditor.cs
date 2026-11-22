using UnityEditor;
using UnityEngine;

namespace Mori.EditorTools
{
    /// <summary>
    /// 自定义轻量化 SceneView 营地建地可视化辅助器（替代笨重的 Tilemap）：
    /// 在 Scene 视图中实时绘制每个固定建地的椭圆占地范围、编号名称与前置解锁连线。
    /// </summary>
    [InitializeOnLoad]
    public static class CampMapEditor
    {
        static CampMapEditor()
        {
            SceneView.duringSceneGui -= OnSceneGui;
            SceneView.duringSceneGui += OnSceneGui;
        }

        static void OnSceneGui(SceneView sceneView)
        {
            var world = Object.FindObjectOfType<CampWorldView>();
            if (world == null) return;

            for (int i = 0; i < MoriCatalog.Plots.Length; i++)
            {
                PlotDef def = MoriCatalog.Plots[i];
                Vector3 center = new Vector3(def.worldX, def.worldY, 0f);

                Handles.color = def.kind == BuildingKind.CoreHq
                    ? new Color(1f, 0.78f, 0.22f, 0.9f)
                    : new Color(0.45f, 0.90f, 0.25f, 0.85f);

                DrawEllipse(center, 1.05f * def.visualScale, 0.52f * def.visualScale);
                Handles.Label(center + new Vector3(-0.5f, 0.25f, 0f), $"#{def.plotId} {def.displayName}");

                if (def.requirePrePlotId > 0)
                {
                    PlotDef pre = MoriCatalog.FindPlot(def.requirePrePlotId);
                    if (pre != null)
                    {
                        Handles.color = new Color(1f, 0.92f, 0.55f, 0.55f);
                        Handles.DrawDottedLine(new Vector3(pre.worldX, pre.worldY, 0f), center, 4f);
                    }
                }
            }
        }

        static void DrawEllipse(Vector3 center, float rx, float ry)
        {
            const int segs = 28;
            Vector3 prev = center + new Vector3(rx, 0f, 0f);
            for (int i = 1; i <= segs; i++)
            {
                float rad = i * Mathf.PI * 2f / segs;
                Vector3 next = center + new Vector3(Mathf.Cos(rad) * rx, Mathf.Sin(rad) * ry, 0f);
                Handles.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
