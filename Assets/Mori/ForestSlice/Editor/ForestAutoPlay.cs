using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Mori.ForestSlice.EditorTools
{
    /// <summary>
    /// 自动化入口：存在 /tmp/mori_forest_play.flag 时，编辑器加载完成后自动打开深山样板并进入 Play。
    /// （-executeMethod 里的延迟调用会被启动时的域重载吃掉，所以改成每次域重载后检查标记文件。）
    /// </summary>
    [InitializeOnLoad]
    static class ForestAutoPlay
    {
        const string Flag = "/tmp/mori_forest_play.flag";
        const string ScenePath = "Assets/Mori/ForestSlice/ForestSlice.unity";
        static double _at;

        static ForestAutoPlay()
        {
            if (!File.Exists(Flag) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            _at = EditorApplication.timeSinceStartup + 3.0;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _at) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= Tick;
            if (!File.Exists(Flag)) return;
            File.Delete(Flag);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }
    }
}
