using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mori
{
    /// <summary>
    /// 新手期 demo。节奏对齐《疯狂水世界》开场约 7 分钟：
    /// 对话 → 捡料 → 建第一座产出 → 小玩法囤原料 → 升级加工 → 交订单补缺料
    /// → 升级揭新建筑 → 限时灾厄 → 拾荒小玩法 → 再交货 → 获得英雄并委任。
    /// 内容换成陆地营地：捕猎、熏肉、清水、沙暴、废路拾荒、铁锅山姆。
    /// 数量缩短，方便一次玩完。画面是代码绘制的等轴测格子。
    /// </summary>
    public sealed class TutorialDemo : MonoBehaviour
    {
        enum Beat
        {
            TalkArrive, Collect, BuildHunt, HuntGoal, HuntPlay,
            Level2, BuildKitchen, Cook, OrderWater, DrawWater, Deliver,
            Level3, Storm, BuildBus, SalvagePlay, Deliver2,
            TalkHero, HeroGet, Appoint, Done
        }

        sealed class Node
        {
            public string id;
            public string action;
            public Transform tr;
            public SpriteRenderer sr;
            public bool on;
            public Vector3 baseScale;
        }

        Camera _cam;
        Font _font;
        Canvas _canvas;
        RectTransform _overlay;
        Text _top;
        Text _quest;
        Text _toast;
        float _toastT;
        Beat _beat;
        int _line;
        readonly List<(string who, string text)> _lines = new List<(string, string)>();

        int _wood, _iron, _meat, _ribs, _water, _coin, _canvasRes;
        int _level = 1;
        int _collected;
        int _cooked;
        bool _appointed;
        float _cookLeft = -1f;
        float _cookTotal = 8f;

        readonly List<Node> _nodes = new List<Node>();
        string _focus;

        RectTransform _playRoot;
        float _marker;
        int _huntHits;
        float _salvageT;
        int _salvageGot;
        float _truckX;
        readonly List<Vector2> _crates = new List<Vector2>();
        float _spawnT;

        public void Begin(Camera cam)
        {
            _cam = cam;
            _cam.orthographic = true;
            _cam.orthographicSize = 3.55f;
            _cam.backgroundColor = new Color32(0x5C, 0x64, 0x6C, 255);
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 40f;
            _cam.transform.rotation = Quaternion.identity;
            _cam.transform.position = new Vector3(0.15f, 1.15f, -20f);
            _font = WxBridge.GetUiFont();
            BuildMap();
            BuildUi();
            Enter(Beat.TalkArrive);
        }

        public void Tick(float dt)
        {
            if (_cookLeft > 0f)
            {
                _cookLeft -= dt;
                if (_cookLeft <= 0f)
                {
                    _cookLeft = -1f;
                    _meat = Mathf.Max(0, _meat - 2);
                    _ribs += 1;
                    _cooked++;
                    Toast("熏肉出锅 +1");
                    if (_beat == Beat.Cook && _ribs >= 2) Enter(Beat.OrderWater);
                }
            }

            if (_beat == Beat.HuntPlay) _marker += dt * 1.35f;
            if (_beat == Beat.SalvagePlay) TickSalvage(dt);
            if (_toastT > 0f) _toastT -= dt;
            if (_toast != null) _toast.gameObject.SetActive(_toastT > 0f);

            PulseFocus();
            RefreshTop();
            if (_beat != Beat.HuntPlay && _beat != Beat.SalvagePlay && Input.GetMouseButtonUp(0))
                OnWorldClick();
        }

        void Enter(Beat beat)
        {
            _beat = beat;
            _focus = null;
            ClearOverlay();
            SetAll("pile", false);
            switch (beat)
            {
                case Beat.TalkArrive:
                    _lines.Clear();
                    _line = 0;
                    Say("山姆", "老大，木料和废铁捡了不少，下一步干什么？");
                    Say("老大", "先把空地边上的废料收干净。能盖的东西都在那儿。");
                    ShowTalk();
                    break;
                case Beat.Collect:
                    _quest.text = "捡起散落废料  " + _collected + "/5";
                    SetAll("pile", true);
                    _focus = "pile";
                    break;
                case Beat.BuildHunt:
                    _quest.text = "建造捕猎桩（木料 6 / 废铁 4）";
                    Enable("hunt");
                    _focus = "hunt";
                    break;
                case Beat.HuntGoal:
                    _quest.text = "去捕猎桩储备生肉  " + _meat + "/6";
                    _focus = "hunt";
                    break;
                case Beat.HuntPlay:
                    _quest.text = "看准绿区再出手";
                    OpenHunt();
                    break;
                case Beat.Level2:
                    _level = 2;
                    ShowUnlock("升到 2 级", "熏肉灶解锁了。生肉得做成能吃的。", () => Enter(Beat.BuildKitchen));
                    break;
                case Beat.BuildKitchen:
                    _quest.text = "建造熏肉灶";
                    Enable("kitchen");
                    _focus = "kitchen";
                    break;
                case Beat.Cook:
                    _quest.text = "熏制 2 份熏肉  " + _ribs + "/2";
                    _focus = "kitchen";
                    break;
                case Beat.OrderWater:
                    _quest.text = "老胡要熏肉和清水";
                    ShowOrder("老胡", "哨上站一天了，给份熏肉，再来瓶清水。", 1, 1, false);
                    break;
                case Beat.DrawWater:
                    _quest.text = "河边打水  " + _water + "/1";
                    Enable("well");
                    _focus = "well";
                    break;
                case Beat.Deliver:
                    _quest.text = "把吃的送给老胡";
                    ShowOrder("老胡", "这就对了。", 1, 1, true);
                    break;
                case Beat.Level3:
                    _level = 3;
                    ShowUnlock("升到 3 级", "新地方可以清了：废料回收台、菜地。先对付眼前的沙暴。", () => Enter(Beat.Storm));
                    break;
                case Beat.Storm:
                    ShowStorm();
                    break;
                case Beat.BuildBus:
                    _quest.text = "建造指挥巴士，当避难所";
                    Enable("bus");
                    _focus = "bus";
                    break;
                case Beat.SalvagePlay:
                    _quest.text = "开车把路上的箱子捡回来";
                    OpenSalvage();
                    break;
                case Beat.Deliver2:
                    _quest.text = "再满足一位居民";
                    if (_ribs < 1) { _ribs = 1; Toast("路上捡到一份熏肉"); }
                    ShowOrder("阿花", "沙过去了，有口热的就行。", 1, 0, true);
                    break;
                case Beat.TalkHero:
                    _lines.Clear();
                    _line = 0;
                    Say("山姆", "老大，避难所立住了。我跟着你干。");
                    Say("老大", "去熏肉灶看着火。");
                    ShowTalk();
                    break;
                case Beat.HeroGet:
                    _level = 4;
                    ShowHero();
                    break;
                case Beat.Appoint:
                    _cookTotal = 5f;
                    _appointed = true;
                    ShowUnlock("委任完成", "铁锅山姆看着熏肉灶，熏制从 8 秒变成 5 秒。", () => Enter(Beat.Done));
                    break;
                case Beat.Done:
                    _quest.text = "新手营地稳住了。空地可以继续往外清。";
                    ShowUnlock("前期结束", "后面就是扩建、做菜、接订单。沙暴和拾荒以后还会再来。", null);
                    break;
            }
            RefreshTop();
        }

        void OnWorldClick()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (_beat == Beat.TalkArrive || _beat == Beat.TalkHero)
            {
                AdvanceTalk();
                return;
            }
            Vector3 wp = _cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 20f));
            Node hit = null;
            float best = 0.42f;
            for (int i = 0; i < _nodes.Count; i++)
            {
                Node n = _nodes[i];
                if (!n.on) continue;
                float d = Vector2.Distance(wp, n.tr.position);
                if (d < best) { best = d; hit = n; }
            }
            if (hit == null) return;
            if (hit.action == "pile" && _beat == Beat.Collect) CollectPile(hit);
            else if (hit.action == "hunt" && _beat == Beat.BuildHunt) ShowBuild("捕猎桩", "往废土里打生肉的地方。", "木料 6    废铁 4", _wood >= 6 && _iron >= 4, () =>
            {
                _wood -= 6; _iron -= 4;
                PaintNode("hunt", "捕猎桩", new Color32(140, 90, 50, 255));
                Toast("捕猎桩建好了");
                Enter(Beat.HuntGoal);
            });
            else if (hit.action == "hunt" && _beat == Beat.HuntGoal) Enter(Beat.HuntPlay);
            else if (hit.action == "kitchen" && _beat == Beat.BuildKitchen) ShowBuild("熏肉灶", "生肉进去，熏肉出来。", "木料 4", _wood >= 4, () =>
            {
                _wood -= 4;
                PaintNode("kitchen", "熏肉灶", new Color32(150, 70, 40, 255));
                Toast("灶升起来了");
                Enter(Beat.Cook);
            });
            else if (hit.action == "kitchen" && _beat == Beat.Cook) OpenCook();
            else if (hit.action == "well" && _beat == Beat.DrawWater)
            {
                _water += 1;
                hit.on = false;
                hit.sr.color = new Color(1f, 1f, 1f, 0.35f);
                Toast("打到清水 +1");
                Enter(Beat.Deliver);
            }
            else if (hit.action == "bus" && _beat == Beat.BuildBus) ShowBuild("指挥巴士", "沙暴来的时候，人往这儿撤。", "木料 2    废铁 2", _wood >= 2 && _iron >= 2, () =>
            {
                _wood -= 2; _iron -= 2;
                PaintNode("bus", "指挥巴士", new Color32(210, 170, 40, 255));
                _coin += 300; _water += 1;
                Toast("避难所建好了  瓶盖 +300");
                Enter(Beat.SalvagePlay);
            });
        }

        void AdvanceTalk()
        {
            _line++;
            if (_line >= _lines.Count)
            {
                _lines.Clear();
                Enter(_beat == Beat.TalkArrive ? Beat.Collect : Beat.HeroGet);
                return;
            }
            ShowTalk();
        }

        void CollectPile(Node n)
        {
            n.on = false;
            n.sr.color = new Color(1f, 1f, 1f, 0.25f);
            if (_collected % 2 == 0) { _wood += 4; Toast("木料 +4"); }
            else { _iron += 3; Toast("废铁 +3"); }
            _collected++;
            _quest.text = "捡起散落废料  " + _collected + "/5";
            if (_collected >= 5) Enter(Beat.BuildHunt);
        }

        void OpenHunt()
        {
            _huntHits = 0;
            _marker = 0f;
            var panel = Panel("捕猎", 560, 420);
            AddText(panel, "绿区里出手，打中就有生肉", 22, new Vector2(0, 120));
            var track = UiBox(panel, new Vector2(440, 36), new Vector2(0, 40), new Color32(40, 40, 48, 255));
            var zone = UiBox(track, new Vector2(90, 36), Vector2.zero, new Color32(50, 170, 80, 255));
            var marker = UiBox(track, new Vector2(14, 48), new Vector2(-200, 0), new Color32(240, 220, 80, 255));
            var count = AddText(panel, "生肉 0/6", 26, new Vector2(0, -20));
            Button(panel, "出手", new Vector2(0, -110), new Color32(40, 120, 210, 255), () =>
            {
                float x = Mathf.Lerp(-200f, 200f, Mathf.PingPong(_marker, 1f));
                marker.anchoredPosition = new Vector2(x, 0);
                bool ok = Mathf.Abs(x) < 45f;
                if (ok) { _meat += 2; _huntHits++; Toast("打中  生肉 +2"); }
                else Toast("打空了");
                count.text = "生肉 " + _meat + "/6";
                if (_meat >= 6)
                {
                    ClearOverlay();
                    Enter(Beat.Level2);
                }
            });
            // 标记每帧由 Tick 推，这里记到字段上用一个小更新器
            var ticker = panel.gameObject.AddComponent<MarkerTick>();
            ticker.marker = marker;
            ticker.demo = this;
        }

        public float MarkerPhase => _marker;

        void OpenCook()
        {
            if (_cookLeft > 0f) { Toast("灶上还忙着"); return; }
            var panel = Panel("熏肉灶", 520, 360);
            string speed = _appointed ? "5 秒（山姆在看火）" : "8 秒";
            AddText(panel, "2 生肉 → 1 熏肉\n这一炉 " + speed, 24, new Vector2(0, 70));
            AddText(panel, "库存  生肉 " + _meat + "   熏肉 " + _ribs, 22, new Vector2(0, 0));
            Button(panel, _meat >= 2 ? "开始熏制" : "生肉不够", new Vector2(0, -100), new Color32(190, 90, 40, 255), () =>
            {
                if (_meat < 2) { Toast("先去捕猎"); return; }
                _cookLeft = _cookTotal;
                ClearOverlay();
                Toast("上灶了");
            });
        }

        void OpenSalvage()
        {
            _salvageT = 16f;
            _salvageGot = 0;
            _truckX = 0f;
            _crates.Clear();
            _spawnT = 0f;
            var panel = Panel("废路拾荒", 620, 780);
            AddText(panel, "左右挪车，撞上箱子", 22, new Vector2(0, 320));
            var field = UiBox(panel, new Vector2(520, 460), new Vector2(0, 40), new Color32(70, 64, 52, 255));
            var truck = UiBox(field, new Vector2(70, 36), new Vector2(0, -190), new Color32(200, 140, 40, 255));
            var info = AddText(panel, "", 22, new Vector2(0, -230));
            var tick = panel.gameObject.AddComponent<SalvageTick>();
            tick.demo = this;
            tick.truck = truck;
            tick.info = info;
            tick.field = field;
            Button(panel, "左", new Vector2(-160, -320), new Color32(60, 60, 70, 255), () => _truckX = Mathf.Max(-1f, _truckX - 0.34f));
            Button(panel, "右", new Vector2(160, -320), new Color32(60, 60, 70, 255), () => _truckX = Mathf.Min(1f, _truckX + 0.34f));
        }

        void TickSalvage(float dt)
        {
            _salvageT -= dt;
            _spawnT -= dt;
            if (_spawnT <= 0f)
            {
                _spawnT = 0.55f;
                _crates.Add(new Vector2(Random.Range(-1f, 1f), 1.15f));
            }
            for (int i = _crates.Count - 1; i >= 0; i--)
            {
                Vector2 c = _crates[i];
                c.y -= dt * 0.85f;
                if (c.y < -0.85f && Mathf.Abs(c.x - _truckX) < 0.28f)
                {
                    _salvageGot++;
                    _crates.RemoveAt(i);
                    if (_salvageGot % 2 == 0) _wood += 1; else _iron += 1;
                    if (_salvageGot % 3 == 0) _canvasRes += 1;
                    continue;
                }
                if (c.y < -1.3f) { _crates.RemoveAt(i); continue; }
                _crates[i] = c;
            }
            if (_salvageGot >= 6 || _salvageT <= 0f)
            {
                int got = _salvageGot;
                ClearOverlay();
                _coin += 120;
                Toast("拾荒结束，箱子 " + got + "  瓶盖 +120");
                Enter(Beat.Deliver2);
            }
        }

        public void SyncSalvageVisual(RectTransform truck, Text info, RectTransform field)
        {
            if (_beat != Beat.SalvagePlay) return;
            if (truck != null) truck.anchoredPosition = new Vector2(_truckX * 200f, -190f);
            if (info != null) info.text = "箱子 " + _salvageGot + "/6    剩余 " + Mathf.CeilToInt(Mathf.Max(0f, _salvageT)) + " 秒";
            if (field == null) return;
            for (int i = field.childCount - 1; i >= 1; i--) Destroy(field.GetChild(i).gameObject);
            for (int i = 0; i < _crates.Count; i++)
            {
                var box = UiBox(field, new Vector2(28, 28), new Vector2(_crates[i].x * 200f, _crates[i].y * 180f), new Color32(150, 110, 60, 255));
                box.gameObject.name = "crate";
            }
        }

        void ShowBuild(string title, string desc, string cost, bool ok, UnityEngine.Events.UnityAction onBuild)
        {
            var panel = Panel(title, 560, 420);
            AddText(panel, desc, 24, new Vector2(0, 80));
            AddText(panel, cost, 26, new Vector2(0, 10));
            Button(panel, ok ? "建造" : "材料不够", new Vector2(0, -110), new Color32(40, 120, 210, 255), () =>
            {
                if (!ok) { Toast("再去捡一点"); return; }
                ClearOverlay();
                onBuild();
            });
        }

        void ShowOrder(string who, string line, int needRibs, int needWater, bool canTry)
        {
            var panel = Panel(who, 560, 460);
            AddText(panel, line, 24, new Vector2(0, 100));
            string need = "要：熏肉 " + _ribs + "/" + needRibs;
            if (needWater > 0) need += "    清水 " + _water + "/" + needWater;
            AddText(panel, need, 26, new Vector2(0, 20));
            bool ok = _ribs >= needRibs && _water >= needWater;
            Button(panel, ok ? "满足TA" : "还缺", new Vector2(-110, -130), new Color32(40, 160, 80, 255), () =>
            {
                if (!ok) { Toast(needWater > 0 && _water < needWater ? "去河边打水" : "熏肉不够"); if (_beat == Beat.OrderWater) Enter(Beat.DrawWater); return; }
                _ribs -= needRibs;
                _water -= needWater;
                _coin += 80;
                Toast("老居民满意了  瓶盖 +80");
                ClearOverlay();
                Enter(_beat == Beat.Deliver2 ? Beat.TalkHero : Beat.Level3);
            });
            if (!canTry)
                Button(panel, "去准备", new Vector2(140, -130), new Color32(60, 60, 70, 255), () => Enter(Beat.DrawWater));
        }

        void ShowStorm()
        {
            var panel = Panel("沙暴来袭", 600, 520);
            AddText(panel, "空地还挡不住风。\n把指挥巴士立起来当避难所。", 26, new Vector2(0, 80));
            AddText(panel, "奖励  瓶盖 300    清水 1", 22, new Vector2(0, -10));
            Button(panel, "去建造", new Vector2(0, -140), new Color32(180, 60, 40, 255), () => Enter(Beat.BuildBus));
        }

        void ShowUnlock(string title, string body, UnityEngine.Events.UnityAction next)
        {
            var panel = Panel(title, 560, 400);
            AddText(panel, body, 26, new Vector2(0, 40));
            if (next != null)
                Button(panel, "继续", new Vector2(0, -120), new Color32(40, 120, 210, 255), () => { ClearOverlay(); next(); });
        }

        void ShowHero()
        {
            var panel = Panel("获得英雄", 560, 460);
            AddText(panel, "铁锅山姆", 36, new Vector2(0, 80));
            AddText(panel, "看着灶火的人。委任之后，熏肉更快。", 24, new Vector2(0, 10));
            Button(panel, "委任到熏肉灶", new Vector2(0, -130), new Color32(200, 140, 40, 255), () => Enter(Beat.Appoint));
        }

        void ShowTalk()
        {
            ClearOverlay();
            var dim = UiBox(_overlay, new Vector2(720, 1280), Vector2.zero, new Color(0f, 0f, 0f, 0.45f));
            dim.gameObject.name = "talk";
            var talkBtn = dim.gameObject.AddComponent<Button>();
            talkBtn.targetGraphic = dim.GetComponent<Image>();
            talkBtn.onClick.AddListener(AdvanceTalk);
            var box = UiBox(dim, new Vector2(640, 220), new Vector2(0, -380), new Color32(245, 240, 230, 255));
            var line = _lines[_line];
            AddText(box, line.who, 22, new Vector2(-230, 60), new Color32(90, 50, 20, 255), TextAnchor.MiddleLeft);
            AddText(box, line.text, 28, new Vector2(0, -10), new Color32(30, 24, 18, 255), TextAnchor.MiddleCenter);
            AddText(box, "点一下继续", 18, new Vector2(0, -75), new Color32(120, 100, 80, 255), TextAnchor.MiddleCenter);
        }

        void Say(string who, string text) { _lines.Add((who, text)); }

        void BuildMap()
        {
            var root = new GameObject("DrawnCamp").transform;
            root.SetParent(transform, false);
            Sprite tile = Diamond(new Color32(150, 112, 72, 255), new Color32(120, 86, 52, 255));
            Sprite straw = Diamond(new Color32(186, 154, 78, 255), new Color32(150, 120, 55, 255));
            Sprite water = Diamond(new Color32(70, 140, 170, 255), new Color32(40, 100, 130, 255));
            for (int y = 0; y < 6; y++)
            {
                for (int x = 0; x < 6; x++)
                {
                    Sprite sp = tile;
                    if (y == 1 && x >= 1 && x <= 4) sp = water;
                    else if (x >= 2 && x <= 4 && y >= 2 && y <= 4) sp = straw;
                    var go = new GameObject($"t{x}_{y}");
                    go.transform.SetParent(root, false);
                    go.transform.position = Iso(x, y);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = sp;
                    sr.sortingOrder = -300;
                }
            }
            AddNode(root, "pile0", "pile", Iso(1, 3), Block("废料", new Color32(120, 80, 50, 255)), true);
            AddNode(root, "pile1", "pile", Iso(4, 2), Block("废料", new Color32(110, 100, 90, 255)), true);
            AddNode(root, "pile2", "pile", Iso(5, 4), Block("废料", new Color32(130, 90, 55, 255)), true);
            AddNode(root, "pile3", "pile", Iso(0, 2), Block("废料", new Color32(100, 90, 80, 255)), true);
            AddNode(root, "pile4", "pile", Iso(3, 5), Block("废料", new Color32(140, 100, 60, 255)), true);
            AddNode(root, "hunt", "hunt", Iso(2, 3), Block("建捕猎", new Color32(90, 70, 50, 180)), false);
            AddNode(root, "kitchen", "kitchen", Iso(4, 3), Block("建灶", new Color32(90, 60, 45, 160)), false);
            AddNode(root, "well", "well", Iso(2, 1), Block("打水", new Color32(80, 150, 180, 255)), false);
            AddNode(root, "bus", "bus", Iso(3, 4), Block("建巴士", new Color32(170, 140, 40, 160)), false);
            AddNode(root, "tree", "deco", Iso(0, 5), Block("树", new Color32(50, 110, 60, 255)), false);
            AddNode(root, "tree2", "deco", Iso(5, 5), Block("树", new Color32(70, 90, 55, 255)), false);
        }

        void AddNode(Transform root, string id, string action, Vector3 pos, Sprite sp, bool on)
        {
            var go = new GameObject(id);
            go.transform.SetParent(root, false);
            go.transform.position = pos + new Vector3(0f, 0.18f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100f);
            var n = new Node { id = id, action = action, tr = go.transform, sr = sr, on = on, baseScale = Vector3.one * 0.55f };
            go.transform.localScale = n.baseScale;
            if (!on && action != "deco") sr.color = new Color(1f, 1f, 1f, 0.15f);
            _nodes.Add(n);
        }

        void PaintNode(string id, string label, Color color)
        {
            Node n = NodeOf(id);
            n.sr.sprite = Block(label, color);
            n.sr.color = Color.white;
            n.on = true;
        }

        void Enable(string id)
        {
            Node n = NodeOf(id);
            n.on = true;
            n.sr.color = Color.white;
        }

        Node NodeOf(string id)
        {
            for (int i = 0; i < _nodes.Count; i++)
                if (_nodes[i].id == id) return _nodes[i];
            return null;
        }

        void SetAll(string action, bool on)
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i].action != action) continue;
                _nodes[i].on = on;
                if (on) _nodes[i].sr.color = Color.white;
            }
        }

        void PulseFocus()
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                Node n = _nodes[i];
                bool hot = n.on && _focus != null && n.action == _focus;
                float s = hot ? 0.55f * (1f + Mathf.Sin(Time.time * 5f) * 0.08f) : 0.55f;
                n.tr.localScale = new Vector3(s, s, 1f);
            }
        }

        static Vector3 Iso(int x, int y) { return new Vector3((x - y) * 0.52f, (x + y) * 0.26f, 0f); }

        void BuildUi()
        {
            var canvasGo = new GameObject("TutorialCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = _cam;
            _canvas.planeDistance = 5f;
            _canvas.sortingOrder = 100;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            scaler.matchWidthOrHeight = 0f;
            _overlay = Full(canvasGo.transform, "Overlay");
            var top = UiBox(_overlay, new Vector2(680, 72), new Vector2(0, 580), new Color32(30, 28, 36, 220));
            _top = AddText(top, "", 22, Vector2.zero);
            var quest = UiBox(_overlay, new Vector2(680, 64), new Vector2(0, 500), new Color32(40, 48, 70, 230));
            _quest = AddText(quest, "", 24, Vector2.zero);
            var toastGo = UiBox(_overlay, new Vector2(420, 56), new Vector2(0, 200), new Color32(20, 20, 20, 200));
            _toast = AddText(toastGo, "", 24, Vector2.zero);
            _toast.gameObject.SetActive(false);
        }

        void RefreshTop()
        {
            if (_top == null) return;
            _top.text = "Lv." + _level + "   木" + _wood + "  铁" + _iron + "  肉" + _meat + "  熏" + _ribs + "  水" + _water + "  盖" + _coin;
            if (_beat == Beat.Cook)
                _quest.text = (_cookLeft > 0f ? "熏制中 " + Mathf.CeilToInt(_cookLeft) + " 秒    " : "熏制 2 份熏肉  ") + _ribs + "/2";
            if (_beat == Beat.HuntGoal)
                _quest.text = "去捕猎桩储备生肉  " + _meat + "/6";
        }

        void Toast(string msg) { _toast.text = msg; _toastT = 1.4f; }

        RectTransform Panel(string title, float w, float h)
        {
            ClearOverlay();
            var dim = UiBox(_overlay, new Vector2(720, 1280), Vector2.zero, new Color(0f, 0f, 0f, 0.35f));
            var panel = UiBox(dim, new Vector2(w, h), Vector2.zero, new Color32(36, 34, 64, 245));
            AddText(panel, title, 32, new Vector2(0, h * 0.5f - 48));
            _playRoot = panel;
            return panel;
        }

        void ClearOverlay()
        {
            if (_overlay == null) return;
            for (int i = _overlay.childCount - 1; i >= 0; i--)
            {
                Transform c = _overlay.GetChild(i);
                if (c.name == "keep") continue;
                if (_top != null && c == _top.transform.parent) continue;
                if (_quest != null && c == _quest.transform.parent) continue;
                if (_toast != null && c == _toast.transform.parent) continue;
                Destroy(c.gameObject);
            }
        }

        RectTransform Full(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        RectTransform UiBox(RectTransform parent, Vector2 size, Vector2 pos, Color color)
        {
            var go = new GameObject("box", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            go.GetComponent<Image>().color = color;
            return rt;
        }

        Text AddText(RectTransform parent, string msg, int size, Vector2 pos)
        {
            return AddText(parent, msg, size, pos, Color.white, TextAnchor.MiddleCenter);
        }

        Text AddText(RectTransform parent, string msg, int size, Vector2 pos, Color color, TextAnchor anchor)
        {
            var go = new GameObject("txt", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(parent.sizeDelta.x - 40f, 160f);
            rt.anchoredPosition = pos;
            var t = go.GetComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.text = msg;
            return t;
        }

        void Button(RectTransform parent, string label, Vector2 pos, Color color, UnityEngine.Events.UnityAction act)
        {
            var rt = UiBox(parent, new Vector2(220, 72), pos, color);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = rt.GetComponent<Image>();
            btn.onClick.AddListener(act);
            AddText(rt, label, 28, Vector2.zero);
        }

        static Sprite Diamond(Color fill, Color edge)
        {
            const int W = 128, H = 64;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    float nx = (x - W * 0.5f) / (W * 0.5f);
                    float ny = (y - H * 0.5f) / (H * 0.5f);
                    float d = Mathf.Abs(nx) + Mathf.Abs(ny);
                    if (d > 1f) { px[y * W + x] = new Color32(0, 0, 0, 0); continue; }
                    Color c = d > 0.9f ? edge : fill;
                    px[y * W + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 128f);
        }

        static Sprite Block(string label, Color color)
        {
            const int W = 96, H = 64;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var px = new Color32[W * H];
            Color32 c = color;
            Color32 ink = new Color32(30, 24, 18, 255);
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                bool edge = x < 3 || y < 3 || x > W - 4 || y > H - 4;
                px[y * W + x] = edge ? ink : c;
            }
            tex.SetPixels32(px);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.15f), 96f);
        }
    }

    public sealed class MarkerTick : MonoBehaviour
    {
        public RectTransform marker;
        public TutorialDemo demo;
        void Update()
        {
            if (marker == null || demo == null) return;
            float x = Mathf.Lerp(-200f, 200f, Mathf.PingPong(demo.MarkerPhase, 1f));
            marker.anchoredPosition = new Vector2(x, 0f);
        }
    }

    public sealed class SalvageTick : MonoBehaviour
    {
        public TutorialDemo demo;
        public RectTransform truck;
        public Text info;
        public RectTransform field;
        void Update()
        {
            if (demo == null) return;
            demo.SyncSalvageVisual(truck, info, field);
        }
    }
}
