using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Mori.EditorTools
{
    /// <summary>
    /// Play 时运行“当前打开的场景”（以前强制绑定 MoriMain，导致打开样板场景按 Play 还是旧画面）。
    /// 首次打开工程停留在空 Untitled 场景时，自动切到当前主推的深山样板场景。
    /// </summary>
    [InitializeOnLoad]
    public static class MoriSceneAutoBind
    {
        const string MainScenePath = "Assets/Mori/MoriMain.unity";
        const string DefaultScenePath = "Assets/Mori/ForestSlice/ForestSlice.unity";

        static MoriSceneAutoBind()
        {
            EditorApplication.delayCall += EnsureSceneBound;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EnsureSceneBound();
        }

        static void EnsureSceneBound()
        {
            if (EditorSceneManager.playModeStartScene != null)
                EditorSceneManager.playModeStartScene = null;

            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            Scene active = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(active.path) && !active.isDirty)
            {
                string path = File.Exists(DefaultScenePath) ? DefaultScenePath : MainScenePath;
                if (File.Exists(path)) EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            }
        }

        [MenuItem("废土狂想营/打开主场景 MoriMain", priority = 0)]
        public static void OpenMainSceneMenu()
        {
            if (EditorApplication.isPlaying)
                EditorApplication.isPlaying = false;
            if (File.Exists(MainScenePath))
                EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        }
    }
}
