using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Öğren: iki mini oyun.
    ///  • Renkler — "Kırmızıyı bul!": üç büyük boya kaleminden doğrusunu seç.
    ///  • Hayvanlar — "Aslanı bul!": üç resimden doğrusunu seç.
    /// Doğru cevap yıldız kazandırır; yanlışta kızmayız, rengin/hayvanın adını söyleriz.
    /// </summary>
    public class LearnScreen : MonoBehaviour, IScreen
    {
        enum Mode { Menu, Colors, Animals }

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
                    if (Names.TryAnimal(l.id, out var n, out var acc) && seen.Add(n))
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

            var colors = MenuCard(new Vector2(0, 230), Loc.T("colors"), Loc.T("colors_sub"), Theme.Accent, 0.1f, () => StartGame(Mode.Colors));
            // kartın üstünde yelpaze gibi açılmış üç kalem
            for (int i = 0; i < 3; i++)
            {
                var c = Names.Basic[new[] { 0, 2, 4 }[i]].color;
                var cr = Crayon(colors, c, new Vector2(110, 300));
                cr.anchorMin = cr.anchorMax = new Vector2(0, 0.5f);
                cr.pivot = new Vector2(0.5f, 0.15f);
                cr.anchoredPosition = new Vector2(170 + (i - 1) * 40, -120);
                cr.localRotation = Quaternion.Euler(0, 0, (1 - i) * 20f);
            }

            var animals = MenuCard(new Vector2(0, -230), Loc.T("pictures"), Loc.T("pictures_sub"), Theme.Mint, 0.2f, () => StartGame(Mode.Animals));
            if (_animals.Count > 0)
            {
                var a = _animals[Random.Range(0, _animals.Count)];
                var tex = SaveSystem.LoadThumb(a.g + "_" + a.id) ?? Level.LoadThumb(a.g, a.id);
                if (tex != null)
                {
                    _textures.Add(tex);
                    var pol = UIKit.Card(animals, "Pol", Color.white, 0.5f, 5f);
                    var prt = (RectTransform)pol.transform.parent;
                    prt.anchorMin = prt.anchorMax = new Vector2(0, 0.5f);
                    prt.sizeDelta = new Vector2(230, 250);
                    prt.anchoredPosition = new Vector2(170, 0);
                    prt.localRotation = Quaternion.Euler(0, 0, 6);
                    var raw = UIKit.Node("Photo", pol.transform).Stretch(14, 14, 14, 14).gameObject.AddComponent<RawImage>();
                    raw.texture = tex;
                    raw.raycastTarget = false;
                    var fit = raw.gameObject.AddComponent<AspectRatioFitter>();
                    fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    fit.aspectRatio = (float)tex.width / tex.height;
                }
            }
        }

        RectTransform MenuCard(Vector2 pos, string title, string sub, Color color, float delay, System.Action onClick)
        {
            var b = UIKit.Button(_area, "", color, Theme.Text, 10, onClick, 0.7f);
            var rt = (RectTransform)b.transform;
            rt.Place(new Vector2(0.5f, 0.5f), pos, new Vector2(860, 360));
            var face = b.transform.Find("Face");
            var t = UIKit.Label(face, title, 84, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            t.rectTransform.Stretch(380, 0, 20, 60);
            var s = UIKit.Label(face, sub, 44, Theme.Text.WithAlpha(0.7f), TextAnchor.MiddleLeft);
            s.rectTransform.Stretch(384, 210, 20, 0);
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
