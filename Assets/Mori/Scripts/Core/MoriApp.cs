using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mori
{
    public enum ModalKind
    {
        None = 0,
        Smokehouse = 1,
        Orders = 2,
        HeroSam = 3,
    }

    /// <summary>
    /// 游戏单场景总控（支持真机/PlayMode实时交互 + 编辑器非Play模式离屏 RenderTexture 一键渲染验收）。
    /// </summary>
    public sealed class MoriApp : MonoBehaviour
    {
        public SimEngine Sim { get; private set; }
        public CampWorldView World { get; private set; }
        public CampHudView Hud { get; private set; }
        public TutorialDemo Tutorial { get; private set; }

        Canvas _canvas;
        RectTransform _modalRoot;
        WorkshopModalView _workshopModal;
        OrderBoardModalView _orderModal;
        HeroSpotlightModalView _heroModal;
        ModalKind _currentModal = ModalKind.None;
        bool _booted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBootIfMissing()
        {
            if (FindObjectOfType<MoriApp>() != null) return;
            // 只在主场景里自动启动，样板场景（ForestSlice / WaterSlice）有自己的入口
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MoriMain") return;
            var go = new GameObject("MoriApp");
            go.AddComponent<MoriApp>();
        }

        void Awake()
        {
            if (Application.isPlaying)
                Boot(null);
        }

        void Start()
        {
            if (_booted && Hud != null)
                Hud.Refresh();
        }

        public void Boot(Camera externalCaptureCamera)
        {
            if (_booted) return;
            _booted = true;

            Camera cam = externalCaptureCamera;
            if (cam == null)
            {
                cam = Camera.main;
                if (cam == null)
                {
                    var camGo = new GameObject("MainWorldCamera", typeof(Camera));
                    cam = camGo.GetComponent<Camera>();
                }
            }

            if (Application.isPlaying && FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                es.transform.SetParent(transform, false);
            }

            var tutorialGo = new GameObject("TutorialDemo");
            tutorialGo.transform.SetParent(transform, false);
            Tutorial = tutorialGo.AddComponent<TutorialDemo>();
            Tutorial.Begin(cam);
        }

        void Update()
        {
            if (!_booted) return;
            if (Tutorial != null)
            {
                Tutorial.Tick(Time.deltaTime);
                return;
            }
            Sim.Tick(Time.deltaTime);
            World.TickVisuals(Time.deltaTime);

            // 松手时才点建筑。拖动地图时不打开弹窗。
            if (_currentModal == ModalKind.None && Input.GetMouseButtonUp(0) && !World.GestureIsDrag)
            {
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                {
                    PlotDef hit = World.PickPlotAtScreenPos(Input.mousePosition);
                    if (hit != null)
                    {
                        if (hit.buildingId == "smokehouse") OpenModal(ModalKind.Smokehouse);
                        else if (hit.buildingId == "command_bus") OpenModal(ModalKind.HeroSam);
                        else OpenModal(ModalKind.Orders);
                    }
                }
            }
        }

        public void OpenModal(ModalKind kind)
        {
            _currentModal = kind;
            MoriUiKit.ClearChildren(_modalRoot);

            if (kind == ModalKind.None)
            {
                Hud.SetWorldHudVisible(true);
                Hud.SelectTabVisual(BottomTab.Camp);
                return;
            }

            Hud.SetWorldHudVisible(false);

            var host = new GameObject("Modal_" + kind, typeof(RectTransform));
            host.transform.SetParent(_modalRoot, false);
            var rt = host.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            if (kind == ModalKind.Smokehouse)
            {
                Hud.SelectTabVisual(BottomTab.Camp);
                _workshopModal = host.AddComponent<WorkshopModalView>();
                _workshopModal.Init(this, rt);
            }
            else if (kind == ModalKind.Orders)
            {
                Hud.SelectTabVisual(BottomTab.Camp);
                _orderModal = host.AddComponent<OrderBoardModalView>();
                _orderModal.Init(this, rt);
            }
            else if (kind == ModalKind.HeroSam)
            {
                Hud.SelectTabVisual(BottomTab.Hero);
                _heroModal = host.AddComponent<HeroSpotlightModalView>();
                _heroModal.Init(this, rt);
            }
        }
    }
}
