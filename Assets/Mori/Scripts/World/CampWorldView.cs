using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Mori
{
    /// <summary>
    /// 2:1 等轴测菱形格子。镜头不旋转，斜视角画在地块和物件上。
    /// 地面用 Unity Isometric Tilemap 刷，建筑和树是脚底对齐的独立精灵。
    /// </summary>
    public sealed class CampWorldView : MonoBehaviour
    {
        public sealed class PlotVisual
        {
            public PlotDef def;
            public Transform root;
            public SpriteRenderer renderer;
        }

        const int Cols = 10;
        const int Rows = 8;

        Camera _worldCam;
        Grid _grid;
        Tilemap _ground;
        Tile _dirt, _straw, _river, _rubble;
        Transform _propRoot;
        readonly List<PlotVisual> _plotVisuals = new List<PlotVisual>();
        float _animTime;
        bool _gestureIsDrag;
        Vector3 _pointerDown;
        Vector3 _camAtPointerDown;
        Vector2 _minCam, _maxCam;

        public Camera WorldCamera => _worldCam;
        public IReadOnlyList<PlotVisual> PlotVisuals => _plotVisuals;
        public bool GestureIsDrag => _gestureIsDrag;

        public void Build(Camera worldCam, SimEngine sim)
        {
            _worldCam = worldCam;
            _worldCam.orthographic = true;
            _worldCam.orthographicSize = 2.75f;
            _worldCam.nearClipPlane = 0.1f;
            _worldCam.farClipPlane = 80f;
            _worldCam.clearFlags = CameraClearFlags.SolidColor;
            _worldCam.backgroundColor = new Color32(0x6A, 0x73, 0x7C, 0xFF);
            _worldCam.transform.rotation = Quaternion.identity;
            _worldCam.transparencySortMode = TransparencySortMode.CustomAxis;
            _worldCam.transparencySortAxis = new Vector3(0f, 1f, -0.26f);

            var gridGo = new GameObject("IsoGrid");
            gridGo.transform.SetParent(transform, false);
            _grid = gridGo.AddComponent<Grid>();
            _grid.cellLayout = GridLayout.CellLayout.Isometric;
            _grid.cellSize = new Vector3(1f, 0.5f, 1f);

            var mapGo = new GameObject("GroundTilemap");
            mapGo.transform.SetParent(gridGo.transform, false);
            _ground = mapGo.AddComponent<Tilemap>();
            var renderer = mapGo.AddComponent<TilemapRenderer>();
            renderer.sortOrder = TilemapRenderer.SortOrder.TopRight;
            renderer.mode = TilemapRenderer.Mode.Chunk;
            renderer.sortingOrder = -200;

            _dirt = MakeTile("iso_dirt");
            _straw = MakeTile("iso_straw");
            _river = MakeTile("iso_river");
            _rubble = MakeTile("iso_rubble");
            PaintGround();

            var propGo = new GameObject("Props");
            propGo.transform.SetParent(gridGo.transform, false);
            _propRoot = propGo.transform;
            PlaceProps();

            FrameHome();
        }

        static Tile MakeTile(string key)
        {
            Sprite sp = MoriSprites.Load(key);
            if (sp == null) return null;
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sp;
            tile.color = Color.white;
            return tile;
        }

        static string GroundKey(int x, int y)
        {
            if (y == 2 && x >= 1 && x <= 8) return "river";
            if (x >= 3 && x <= 6 && y >= 3 && y <= 5) return "straw";
            if ((x == 9 && y == 4) || (x == 8 && y == 6) || (x == 1 && y == 1) || (x == 0 && y == 5))
                return "rubble";
            return "dirt";
        }

        Tile TileFor(string kind)
        {
            if (kind == "straw") return _straw;
            if (kind == "river") return _river;
            if (kind == "rubble") return _rubble;
            return _dirt;
        }

        void PaintGround()
        {
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                    _ground.SetTile(new Vector3Int(x, y, 0), TileFor(GroundKey(x, y)));
            }
            _ground.RefreshAllTiles();
        }

        void PlaceProps()
        {
            _plotVisuals.Clear();
            Place("bus", "p_iso_bus", 5, 4, 1.15f, "command_bus");
            Place("shack", "p_iso_shack", 6, 5, 1.05f, "smokehouse");
            Place("tanks", "p_iso_tanks", 7, 4, 0.82f, "water_still");
            Place("people", "p_iso_people", 4, 4, 0.78f, null);
            Place("pad", "p_iso_foundation", 5, 3, 0.48f, null);
            Place("pine_a", "p_iso_pine", 1, 6, 1.35f, null);
            Place("pine_b", "p_iso_pine", 2, 7, 1.55f, null);
            Place("pine_c", "p_iso_pine", 0, 6, 1.15f, null);
            Place("dead_a", "p_iso_dead", 4, 6, 1.25f, null);
            Place("dead_b", "p_iso_dead", 7, 6, 1.1f, null);
            Place("car", "p_iso_car", 8, 3, 0.62f, null);
        }

        void Place(string id, string key, int cx, int cy, float worldHeight, string buildingId)
        {
            Sprite sp = MoriSprites.Load(key);
            var go = new GameObject("Prop_" + id);
            go.transform.SetParent(_propRoot, false);
            Vector3 pos = _grid.GetCellCenterWorld(new Vector3Int(cx, cy, 0));
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = -(cx + cy) * 20;
            if (sp != null && sp.bounds.size.y > 0.01f)
            {
                float s = worldHeight / sp.bounds.size.y;
                go.transform.localScale = new Vector3(s, s, 1f);
            }

            if (string.IsNullOrEmpty(buildingId)) return;
            PlotDef def = null;
            for (int i = 0; i < MoriCatalog.Plots.Length; i++)
            {
                if (MoriCatalog.Plots[i].buildingId == buildingId)
                {
                    def = MoriCatalog.Plots[i];
                    break;
                }
            }
            if (def != null)
                _plotVisuals.Add(new PlotVisual { def = def, root = go.transform, renderer = sr });
        }

        void FrameHome()
        {
            Vector3 focus = _grid.GetCellCenterWorld(new Vector3Int(5, 4, 0));
            _worldCam.transform.position = new Vector3(focus.x + 0.15f, focus.y - 0.05f, -20f);

            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    Vector3 p = _grid.GetCellCenterWorld(new Vector3Int(x, y, 0));
                    minX = Mathf.Min(minX, p.x);
                    maxX = Mathf.Max(maxX, p.x);
                    minY = Mathf.Min(minY, p.y);
                    maxY = Mathf.Max(maxY, p.y);
                }
            }
            _minCam = new Vector2(minX - 0.2f, minY + 0.2f);
            _maxCam = new Vector2(maxX + 0.2f, maxY + 0.6f);
            ClampCamera();
        }

        public void TickVisuals(float dt)
        {
            _animTime += dt;
            HandleDrag();
            Transform people = _propRoot != null ? _propRoot.Find("Prop_people") : null;
            if (people != null)
            {
                float mag = Mathf.Abs(people.localScale.x);
                float breath = 1f + Mathf.Sin(_animTime * 2.1f) * 0.02f;
                people.localScale = new Vector3(Mathf.Sign(people.localScale.x) * mag, mag * breath, 1f);
            }
        }

        void HandleDrag()
        {
            if (!Application.isPlaying || _worldCam == null) return;
            if (Input.GetMouseButtonDown(0))
            {
                _pointerDown = Input.mousePosition;
                _camAtPointerDown = _worldCam.transform.position;
                _gestureIsDrag = false;
            }
            if (!Input.GetMouseButton(0)) return;
            Vector3 delta = Input.mousePosition - _pointerDown;
            if (delta.magnitude > 14f) _gestureIsDrag = true;
            if (!_gestureIsDrag) return;
            float worldPerPixel = (_worldCam.orthographicSize * 2f) / Mathf.Max(1f, Screen.height);
            Vector3 next = _camAtPointerDown - new Vector3(delta.x, delta.y, 0f) * worldPerPixel;
            next.z = -20f;
            _worldCam.transform.position = next;
            ClampCamera();
        }

        void ClampCamera()
        {
            float halfH = _worldCam.orthographicSize;
            float halfW = halfH * _worldCam.aspect;
            float x = Mathf.Clamp(_worldCam.transform.position.x, _minCam.x + halfW, Mathf.Max(_minCam.x + halfW, _maxCam.x - halfW));
            float y = Mathf.Clamp(_worldCam.transform.position.y, _minCam.y + halfH, Mathf.Max(_minCam.y + halfH, _maxCam.y - halfH));
            _worldCam.transform.position = new Vector3(x, y, -20f);
        }

        public void ClearPatch(int x, int y)
        {
            if (_straw == null) return;
            _ground.SetTile(new Vector3Int(x, y, 0), _straw);
        }

        public PlotDef PickPlotAtScreenPos(Vector2 screenPos)
        {
            if (_worldCam == null || _gestureIsDrag) return null;
            Vector3 wp = _worldCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 20f));
            PlotDef best = null;
            float bestScore = 1f;
            for (int i = 0; i < _plotVisuals.Count; i++)
            {
                PlotVisual pv = _plotVisuals[i];
                Vector3 center = pv.root.position + new Vector3(0f, 0.35f, 0f);
                float dx = (wp.x - center.x) / 0.55f;
                float dy = (wp.y - center.y) / 0.4f;
                float d2 = dx * dx + dy * dy;
                if (d2 <= bestScore)
                {
                    bestScore = d2;
                    best = pv.def;
                }
            }
            return best;
        }
    }
}
