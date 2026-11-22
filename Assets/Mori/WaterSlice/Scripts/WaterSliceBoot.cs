using UnityEngine;

namespace Mori.WaterSlice
{
    /// <summary>Play 模式入口：搭建水上样板并驱动动画，支持拖动镜头。</summary>
    public sealed class WaterSliceBoot : MonoBehaviour
    {
        WaterSliceView _view;
        Camera _cam;
        Vector3 _down, _camAtDown;

        void Start()
        {
            _cam = Camera.main;
            if (_cam == null) _cam = new GameObject("MainCamera", typeof(Camera)).GetComponent<Camera>();
            _view = gameObject.AddComponent<WaterSliceView>();
            _view.Build(_cam);
        }

        void Update()
        {
            if (_view == null) return;
            if (Input.GetMouseButtonDown(0)) { _down = Input.mousePosition; _camAtDown = _cam.transform.position; }
            if (Input.GetMouseButton(0))
            {
                float k = _cam.orthographicSize * 2f / Mathf.Max(1, Screen.height);
                Vector3 d = (Input.mousePosition - _down) * k;
                _cam.transform.position = _camAtDown - new Vector3(d.x, d.y, 0f);
            }
            _view.Tick(Time.deltaTime);
        }
    }
}
