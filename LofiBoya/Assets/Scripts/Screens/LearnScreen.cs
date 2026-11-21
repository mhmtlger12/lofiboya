using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Öğren: dört mini oyun.
    ///  • Renkler — "Kırmızıyı bul!": üç büyük boya kaleminden doğrusunu seç.
    ///  • Hayvanlar — "Aslanı bul!": üç resimden doğrusunu seç.
    ///  • Şekiller — "Üçgeni boya!": doğru şekle dokununca şekil renklenir.
    ///  • Sayılar — "Kaç tane yıldız var?": yanlışta birlikte tek tek sayarız; doğru bildikçe sayılar büyür.
    /// Doğru cevap yıldız kazandırır; yanlışta kızmayız, rengin/hayvanın adını söyleriz.
    /// </summary>
    public class LearnScreen : MonoBehaviour, IScreen
    {
        enum Mode { Menu, Colors, Animals, Shapes, Count }

        Mode _mode = Mode.Menu;
        RectTransform _root, _area;
        Text _prompt, _starText;
        Mascot _mascot;
        string _promptVoice;
        int _lastTarget = -1;
        bool _locked;
        readonly List<Texture2D> _textures = new();
        readonly List<(string g, string id, string name, string acc)> _animals = new();

        void Start()
        {
            _root = (RectTransform)transform;
            var back = UIKit.CircleButton(_root, "‹", Theme.Card, Theme.Text, 120, 90, () => OnBack());
            ((RectTransform)back.transform).Place(new Vector2(0, 1), new Vector2(110, -110), new Vector2(120, 120));
            _starText = StickerScreen.StarPill(_root, SaveSystem.Stars);

            // Bilinen hayvan/nesne adı olan resimler (aynı ad bir kez)
            var seen = new HashSet<string>();
            foreach (var g in Level.Index.groups)
                foreach (var l in g.levels)
                    if (Names.IsLearnable(l.id) && Names.TryAnimal(l.id, out var n, out var acc) && seen.Add(n))
                        _animals.Add((g.name, l.id, n, acc));

            _mascot = Mascot.Create(_root, 240);
            var mrt = (RectTransform)_mascot.transform;
            mrt.Place(new Vector2(0, 0), new Vector2(170, 170), mrt.sizeDelta);

            ShowMenu();
        }

        void ClearArea()
        {
            if (_area != null) Destroy(_area.gameObject);
            _area = UIKit.Node("Area", _root).Stretch();
            _area.SetSiblingIndex(1);
            _locked = false;
            foreach (var t in _textures) if (t) Destroy(t);
            _textures.Clear();
        }

        // ================= Menü =================

        void ShowMenu()
        {
            _mode = Mode.Menu;
            ClearArea();
            var title = UIKit.Label(_area, Loc.T("learn"), 96, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Band(true, 120, 200);
            var sub = UIKit.Label(_area, Loc.T("learn_sub"), 44, Theme.TextSoft);
            sub.rectTransform.Band(true, 60, 315);

            // 2x2 kart: Renkler, Resimler, Şekiller, Sayılar
            var colors = MenuCard(new Vector2(-222, 250), Loc.T("colors"), Loc.T("colors_sub"), Theme.Accent, 0.1f, () => StartGame(Mode.Colors));
            for (int i = 0; i < 3; i++)
            {
                var c = Names.Basic[new[] { 0, 2, 4 }[i]].color;
                var cr = Crayon(colors, c, new Vector2(80, 220));
                cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 0.62f);
                cr.pivot = new Vector2(0.5f, 0.15f);
                cr.anchoredPosition = new Vector2((i - 1) * 34, -80);
                cr.localRotation = Quaternion.Euler(0, 0, (1 - i) * 20f);
            }

            var animals = MenuCard(new Vector2(222, 250), Loc.T("pictures"), Loc.T("pictures_sub"), Theme.Mint, 0.18f, () => StartGame(Mode.Animals));
            if (_animals.Count > 0)
            {
                var a = _animals[Random.Range(0, _animals.Count)];
                var tex = SaveSystem.LoadThumb(a.g + "_" + a.id) ?? Level.LoadThumb(a.g, a.id);
                if (tex != null)
                {
                    _textures.Add(tex);
                    var pol = UIKit.Card(animals, "Pol", Color.white, 0.5f, 5f);
                    var prt = (RectTransform)pol.transform.parent;
                    prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.62f);
                    prt.sizeDelta = new Vector2(170, 190);
                    prt.anchoredPosition = Vector2.zero;
                    prt.localRotation = Quaternion.Euler(0, 0, 6);
                    var raw = UIKit.Node("Photo", pol.transform).Stretch(12, 12, 12, 12).gameObject.AddComponent<RawImage>();
                    raw.texture = tex;
                    raw.raycastTarget = false;
                    var fit = raw.gameObject.AddComponent<AspectRatioFitter>();
                    fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    fit.aspectRatio = (float)tex.width / tex.height;
                }
            }

            var shapes = MenuCard(new Vector2(-222, -230), Loc.T("shapes"), Loc.T("shapes_sub"), Theme.Lavender, 0.26f, () => StartGame(Mode.Shapes));
            var deco = new[] { ("circle", Names.Basic[4].color, new Vector2(-62, 34)), ("triangle", Names.Basic[2].color, new Vector2(56, 40)), ("square", Names.Basic[6].color, new Vector2(0, -50)) };
            foreach (var (key, col, pos) in deco)
            {
                var img = UIKit.Box(shapes, "Shape", col, Art.Shape(key));
                img.type = Image.Type.Simple;
                img.raycastTarget = false;
                img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.62f);
                img.rectTransform.anchoredPosition = pos;
                img.rectTransform.sizeDelta = new Vector2(100, 100);
            }

            var count = MenuCard(new Vector2(222, -230), Loc.T("counting"), Loc.T("counting_sub"), Theme.Butter, 0.34f, () => StartGame(Mode.Count));
            for (int i = 0; i < 3; i++)
            {
                var d = UIKit.Label(count, (i + 1).ToString(), 96, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                d.raycastTarget = false;
                d.rectTransform.anchorMin = d.rectTransform.anchorMax = new Vector2(0.5f, 0.62f);
                d.rectTransform.anchoredPosition = new Vector2((i - 1) * 80, i == 1 ? 14 : -6);
                d.rectTransform.sizeDelta = new Vector2(90, 130);
                d.rectTransform.localRotation = Quaternion.Euler(0, 0, (1 - i) * 10f);
            }
        }

        RectTransform MenuCard(Vector2 pos, string title, string sub, Color color, float delay, System.Action onClick)
        {
            var b = UIKit.Button(_area, "", color, Theme.Text, 10, onClick, 0.7f);
            var rt = (RectTransform)b.transform;
            rt.Place(new Vector2(0.5f, 0.5f), pos, new Vector2(420, 440));
            var face = b.transform.Find("Face");
            var t = UIKit.Label(face, title, 60, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.anchorMin = new Vector2(0, 0);
            t.rectTransform.anchorMax = new Vector2(1, 0);
            t.rectTransform.pivot = new Vector2(0.5f, 0);
            t.rectTransform.offsetMin = new Vector2(16, 78);
            t.rectTransform.offsetMax = new Vector2(-16, 158);
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 34;
            t.resizeTextMaxSize = 60;
            var s = UIKit.Label(face, sub, 34, Theme.Text.WithAlpha(0.7f), TextAnchor.MiddleCenter);
            s.rectTransform.anchorMin = new Vector2(0, 0);
            s.rectTransform.anchorMax = new Vector2(1, 0);
            s.rectTransform.pivot = new Vector2(0.5f, 0);
            s.rectTransform.offsetMin = new Vector2(16, 24);
            s.rectTransform.offsetMax = new Vector2(-16, 84);
            s.resizeTextForBestFit = true;
            s.resizeTextMinSize = 22;
            s.resizeTextMaxSize = 34;
            Tween.Pop(rt, delay, 0.5f);
            return (RectTransform)face;
        }

        static RectTransform Crayon(Transform parent, Color c, Vector2 size)
        {
            var rt = UIKit.Node("Crayon", parent);
            rt.sizeDelta = size;
            var body = UIKit.Box(rt, "Body", c, Art.CrayonBody);
            body.type = Image.Type.Simple;
            body.rectTransform.Stretch();
            var shine = UIKit.Box(rt, "Shine", Color.white, Art.CrayonShine);
            shine.type = Image.Type.Simple;
            shine.rectTransform.Stretch();
            return rt;
        }

        // ================= Oyun =================

        void StartGame(Mode mode)
        {
            _mode = mode;
            if (mode == Mode.Animals && _animals.Count < 3) { Popup.Toast(Loc.T("soon")); _mode = Mode.Menu; return; }
            NextRound();
        }

        void NextRound()
        {
            ClearArea();
            // soru + tekrar dinle butonu
            _prompt = UIKit.Label(_area, "", 88, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            _prompt.rectTransform.Band(true, 140, 250);
            var sh = _prompt.gameObject.AddComponent<Shadow>();
            sh.effectColor = Theme.Accent.WithAlpha(0.45f);
            sh.effectDistance = new Vector2(0, -6);
            var again = UIKit.IconButton(_area, Art.Note, Theme.Butter, Theme.Text, 110, () => Speech.Say(_promptVoice), 0.46f);
            ((RectTransform)again.transform).Place(new Vector2(0.5f, 1), new Vector2(0, -470), new Vector2(110, 110));

            if (_mode == Mode.Colors) ColorRound();
            else if (_mode == Mode.Shapes) ShapeRound();
            else if (_mode == Mode.Count) CountRound();
            else AnimalRound();
            Tween.Pop(_prompt.rectTransform, 0.05f, 0.4f);
            Speech.Say(_promptVoice);
        }

        void ColorRound()
        {
            int target;
            do target = Random.Range(0, Names.Basic.Length); while (target == _lastTarget);
            _lastTarget = target;
            var picks = new List<int> { target };
            while (picks.Count < 3)
            {
                int o = Random.Range(0, Names.Basic.Length);
                if (!picks.Contains(o)) picks.Add(o);
            }
            Shuffle(picks);
            var t = Names.Basic[target];
            _promptVoice = Names.Cap(Loc.T("find", t.acc));
            _prompt.text = _promptVoice;

            for (int i = 0; i < 3; i++)
            {
                int idx = picks[i];
                var slot = UIKit.Node("Option", _area);
                slot.Place(new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 300, -120), new Vector2(240, 720));
                var hit = slot.gameObject.AddComponent<Image>();
                hit.color = Color.clear;
                var cr = Crayon(slot, Names.Basic[idx].color, new Vector2(170, 640));
                cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 0.5f);
                cr.anchoredPosition = Vector2.zero;
                cr.gameObject.AddComponent<Floaty>().Init(10f, 0.6f + i * 0.15f);
                var b = slot.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => Answer(idx == target, slot, Names.Basic[idx].name));
                cr.localScale = Vector3.zero;
                Tween.Pop(cr, 0.1f + i * 0.08f, 0.45f);
            }
        }

        void AnimalRound()
        {
            int target;
            do target = Random.Range(0, _animals.Count); while (target == _lastTarget && _animals.Count > 1);
            _lastTarget = target;
            var picks = new List<int> { target };
            while (picks.Count < 3)
            {
                int o = Random.Range(0, _animals.Count);
                if (!picks.Contains(o)) picks.Add(o);
            }
            Shuffle(picks);
            var a = _animals[target];
            _promptVoice = Names.Cap(Loc.T("find", a.acc));
            _prompt.text = _promptVoice;

            for (int i = 0; i < 3; i++)
            {
                int idx = picks[i];
                var e = _animals[idx];
                var slot = UIKit.Node("Option", _area);
                // ikisi üstte, biri altta ortada
                var pos = i < 2 ? new Vector2((i == 0 ? -1 : 1) * 225, 60) : new Vector2(0, -420);
                slot.Place(new Vector2(0.5f, 0.5f), pos, new Vector2(400, 440));
                slot.localRotation = Quaternion.Euler(0, 0, (i - 1) * -4f);
                var face = UIKit.Card(slot, "Pol", Color.white, 0.5f, 6f);
                ((RectTransform)face.transform.parent).Stretch();
                face.raycastTarget = true;
                var tex = SaveSystem.LoadThumb(e.g + "_" + e.id) ?? Level.LoadThumb(e.g, e.id);
                if (tex != null)
                {
                    _textures.Add(tex);
                    var raw = UIKit.Node("Photo", face.transform).Stretch(22, 22, 22, 22).gameObject.AddComponent<RawImage>();
                    raw.texture = tex;
                    raw.raycastTarget = false;
                    var fit = raw.gameObject.AddComponent<AspectRatioFitter>();
                    fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    fit.aspectRatio = (float)tex.width / tex.height;
                }
                var b = slot.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => Answer(idx == target, slot, e.name));
                slot.localScale = Vector3.zero;
                Tween.Pop(slot, 0.1f + i * 0.08f, 0.45f);
            }
        }

        // ================= Şekiller: "Üçgeni boya!" =================

        static readonly string[] ShapeKeys = { "circle", "square", "triangle", "rectangle", "star", "heart" };

        void ShapeRound()
        {
            int target;
            do target = Random.Range(0, ShapeKeys.Length); while (target == _lastTarget);
            _lastTarget = target;
            var picks = new List<int> { target };
            while (picks.Count < 3)
            {
                int o = Random.Range(0, ShapeKeys.Length);
                if (!picks.Contains(o)) picks.Add(o);
            }
            Shuffle(picks);
            var (_, acc) = Names.Shape(ShapeKeys[target]);
            _promptVoice = Names.Cap(Loc.T("paint_shape", acc));
            _prompt.text = _promptVoice;

            // her şekil boyanınca farklı bir renk alsın
            var colors = new List<int> { 0, 1, 2, 3, 4, 5, 6 };
            Shuffle(colors);
            for (int i = 0; i < 3; i++)
            {
                int idx = picks[i];
                string key = ShapeKeys[idx];
                var fill = Names.Basic[colors[i]].color;
                var slot = UIKit.Node("Option", _area);
                // ikisi üstte, biri altta ortada (büyük dokunma alanları)
                var pos = i < 2 ? new Vector2((i == 0 ? -1 : 1) * 230, 80) : new Vector2(0, -360);
                slot.Place(new Vector2(0.5f, 0.5f), pos, new Vector2(380, 380));
                var hit = slot.gameObject.AddComponent<Image>();
                hit.color = Color.clear;
                // çizgi (koyu) + içi (beyaz): boyama sayfası gibi
                var line = UIKit.Box(slot, "Line", Theme.Text, Art.Shape(key));
                line.type = Image.Type.Simple;
                line.raycastTarget = false;
                line.rectTransform.Stretch(20, 20, 20, 20);
                var inner = UIKit.Box(line.transform, "Inner", Color.white, Art.Shape(key));
                inner.type = Image.Type.Simple;
                inner.raycastTarget = false;
                inner.rectTransform.Stretch();
                inner.rectTransform.localScale = Vector3.one * (key == "triangle" ? 0.82f : key == "star" ? 0.8f : 0.9f);
                if (key == "triangle") inner.rectTransform.anchoredPosition = new Vector2(0, -8);
                slot.gameObject.AddComponent<Floaty>().Init(8f, 0.6f + i * 0.15f);
                var b = slot.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                var name = Names.Shape(key).name;
                b.onClick.AddListener(() =>
                {
                    bool right = idx == target;
                    // doğru şekil parmağın altında renklenir
                    if (right && !_locked) Tween.To(inner, 0.45f, k => inner.color = Color.Lerp(Color.white, fill, k));
                    Answer(right, slot, name);
                });
                slot.localScale = Vector3.zero;
                Tween.Pop(slot, 0.1f + i * 0.08f, 0.45f);
            }
        }

        // ================= Sayılar: "Kaç tane yıldız var?" =================

        int _countLevel;          // doğru cevaplarla artar: sayı aralığı çocuğun hızına göre büyür
        int _countAnswer;
        bool _countingTogether;
        readonly List<RectTransform> _countItems = new();

        static Sprite CountSprite(int k) => k switch { 0 => Art.Heart, 1 => Art.Star5, 2 => Art.Flower, _ => Art.Balloon };

        void CountRound()
        {
            _countItems.Clear();
            _countingTogether = false;
            int max = Mathf.Clamp(4 + _countLevel / 2, 4, 9);   // 1-4 ile başlar, en çok 9
            int n;
            do n = Random.Range(1, max + 1); while (n == _countAnswer && max > 1);
            _countAnswer = n;
            int kind = Random.Range(0, 4);
            _promptVoice = Loc.T("count_q_" + kind);
            _prompt.text = _promptVoice;
            var color = Names.Basic[Random.Range(0, Names.Basic.Length)].color;

            // nesneleri 4x3 ızgaraya karışık yerleştir (üst üste binmesin)
            var board = UIKit.Card(_area, "Board", Theme.Card.WithAlpha(0.85f), 0.6f, 4f);
            var brt = (RectTransform)board.transform.parent;
            brt.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(880, 600));
            var cells = new List<int>();
            for (int c = 0; c < 12; c++) cells.Add(c);
            Shuffle(cells);
            var spots = new List<Vector2>();
            for (int i = 0; i < n; i++)
            {
                int c = cells[i];
                spots.Add(new Vector2(-300 + (c % 4) * 200 + Random.Range(-20f, 20f), 170 - (c / 4) * 170 + Random.Range(-15f, 15f)));
            }
            // soldan sağa, yukarıdan aşağı sırala: birlikte sayarken göz takip etsin
            spots.Sort((a, b) => Mathf.Abs(a.y - b.y) > 60 ? b.y.CompareTo(a.y) : a.x.CompareTo(b.x));
            for (int i = 0; i < n; i++)
            {
                var item = UIKit.Box(board.transform, "Item", color, CountSprite(kind));
                item.type = Image.Type.Simple;
                item.raycastTarget = false;
                var irt = item.rectTransform;
                irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f);
                irt.anchoredPosition = spots[i];
                irt.sizeDelta = new Vector2(140, 140);
                irt.localRotation = Quaternion.Euler(0, 0, Random.Range(-14f, 14f));
                irt.localScale = Vector3.zero;
                Tween.Pop(irt, 0.1f + i * 0.06f, 0.4f);
                _countItems.Add(irt);
            }

            // üç sayı seçeneği (biri doğru)
            var picks = new List<int> { n };
            while (picks.Count < 3)
            {
                int o = Mathf.Clamp(n + Random.Range(-2, 3), 1, max + 1);
                if (!picks.Contains(o)) picks.Add(o);
            }
            picks.Sort();
            for (int i = 0; i < 3; i++)
            {
                int val = picks[i];
                var bt = UIKit.Button(_area, val.ToString(), Theme.Card, Theme.Text, 110, null, 0.5f);
                var rt = (RectTransform)bt.transform;
                rt.Place(new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 270, -420), new Vector2(210, 210));
                UIKit.ButtonText(bt).fontStyle = FontStyle.Bold;
                bt.onClick.AddListener(() => CountAnswer(val, rt));
                rt.localScale = Vector3.zero;
                Tween.Pop(rt, 0.3f + i * 0.08f, 0.45f);
            }
        }

        void CountAnswer(int value, RectTransform option)
        {
            if (_locked || _countingTogether) return;
            if (value == _countAnswer)
            {
                _countLevel++;
                _locked = true;
                Sfx.Star();
                Tween.Delay(0.15f, Sfx.Win);
                Tween.Punch(option, 0.25f, 0.5f);
                Confetti.Burst(_area, 50);
                _mascot.Jump();
                _mascot.Wave(1.2f);
                SaveSystem.Stars += 1;
                _starText.text = SaveSystem.Stars.ToString();
                Tween.Punch(_starText.transform.parent, 0.25f);
                string cheer = Loc.T("cheer_" + Random.Range(0, 4));
                Speech.Say(Loc.T("count_right", cheer, _countAnswer));
                _prompt.text = cheer;
                foreach (var it in _countItems) Tween.Punch(it, 0.2f, 0.4f);
                Tween.Delay(2.2f, () => { if (this != null && _mode == Mode.Count) NextRound(); });
                return;
            }
            // Yanlış: kızmayız, birlikte tek tek sayarız (her nesneye bir sayı)
            _countLevel = Mathf.Max(0, _countLevel - 1);
            Sfx.Wrong();
            if (!option.TryGetComponent<CanvasGroup>(out var cg)) cg = option.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0.45f;
            cg.interactable = false;
            _countingTogether = true;
            Speech.Say(Loc.T("count_together"));
            float t0 = 1.6f;
            for (int i = 0; i < _countItems.Count; i++)
            {
                int k = i;
                Tween.Delay(t0 + i * 0.9f, () =>
                {
                    if (this == null || k >= _countItems.Count || _countItems[k] == null) return;
                    var it = _countItems[k];
                    Tween.Punch(it, 0.3f, 0.45f);
                    Sfx.Pop();
                    var num = UIKit.Label(it.parent, (k + 1).ToString(), 64, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                    num.raycastTarget = false;
                    num.rectTransform.anchorMin = num.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    num.rectTransform.anchoredPosition = it.anchoredPosition + new Vector2(0, -4);
                    num.rectTransform.sizeDelta = new Vector2(120, 90);
                    var o = num.gameObject.AddComponent<Outline>();
                    o.effectColor = Color.white;
                    o.effectDistance = new Vector2(3, -3);
                    Speech.Say((k + 1).ToString());
                });
            }
            Tween.Delay(t0 + _countItems.Count * 0.9f + 0.3f, () => { if (this != null) _countingTogether = false; });
        }

        void Answer(bool right, RectTransform option, string name)
        {
            if (_locked) return;
            if (right)
            {
                _locked = true;
                Sfx.Star();
                Tween.Delay(0.15f, Sfx.Win);
                Tween.Punch(option, 0.25f, 0.5f);
                Confetti.Burst(_area, 50);
                _mascot.Jump();
                _mascot.Wave(1.2f);
                SaveSystem.Stars += 1;
                _starText.text = SaveSystem.Stars.ToString();
                Tween.Punch(_starText.transform.parent, 0.25f);
                string cheer = Loc.T("cheer_" + Random.Range(0, 4));
                Speech.Say(Loc.T("right_say", cheer, Loc.Lang == "tr" ? name : Names.Cap(name)));
                _prompt.text = cheer;
                Tween.Delay(2.0f, () => { if (this != null && _mode != Mode.Menu) NextRound(); });
            }
            else
            {
                Sfx.Wrong();
                var basePos = option.anchoredPosition;
                Tween.To(option, 0.4f, k => option.anchoredPosition = basePos + new Vector2(Mathf.Sin(k * Mathf.PI * 6) * 18f * (1 - k), 0));
                Speech.Say(Loc.T("wrong_say", Loc.Lang == "tr" ? name : Names.Cap(name)));
                // yanlış seçenek biraz solar ki çocuk diğerlerine baksın
                if (!option.TryGetComponent<CanvasGroup>(out var cg)) cg = option.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0.45f;
            }
        }

        static void Shuffle(List<int> l)
        {
            for (int i = 0; i < l.Count; i++) { int j = Random.Range(i, l.Count); (l[i], l[j]) = (l[j], l[i]); }
        }

        public bool OnBack()
        {
            if (_mode != Mode.Menu) ShowMenu();
            else App.I.ShowWelcome();
            return true;
        }

        void OnDestroy()
        {
            foreach (var t in _textures) if (t) Destroy(t);
        }
    }
}
