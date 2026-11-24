using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Öğren: altı mini oyun.
    ///  • Renkler — "Kırmızıyı bul!": üç büyük boya kaleminden doğrusunu seç.
    ///  • Hayvanlar — "Aslanı bul!": üç resimden doğrusunu seç.
    ///  • Şekiller — "Üçgeni boya!": doğru şekle dokununca şekil renklenir.
    ///  • Sayılar — "Kaç tane yıldız var?", ilerledikçe "Hangi tarafta daha çok?" ve "Hepsi kaç tane?" (toplama).
    ///    Yanlışta birlikte tek tek sayarız; doğru bildikçe sayılar büyür. Başlangıç seviyesi yaş ayarından.
    ///  • Duygular — yüz tanıma ("Üzgün yüzü bul!") ile durum–duygu eşleştirme ("Balonu uçtu. Nasıl hisseder?") sırayla.
    ///  • Rakam yaz — noktalı rakamın üzerinden parmakla geçilir (yazıya hazırlık).
    /// Yanlışta kızmayız: dokunulan seçeneğin adı altında yazar ("Turuncu"), seçenek solar.
    /// Yıldız yalnızca turun İLK denemesinde doğru bilinince verilir (deneme-yanılma ödüllenmez);
    /// sonradan bulunca da sevinilir ama yıldız çıkmaz. Rapor da tur başına ilk denemeyi sayar.
    /// </summary>
    public partial class LearnScreen : MonoBehaviour, IScreen
    {
        enum Mode { Menu, Colors, Animals, Shapes, Count, Feelings, Trace, Letters, Patterns, Memory, Opposites, Sequence, Shadows, Seasons, Sort }

        Mode _mode = Mode.Menu;
        RectTransform _root, _area;
        Transform _back;
        Text _prompt, _starText;
        Mascot _mascot;
        string _promptVoice;
        int _lastTarget = -1;
        bool _locked;
        readonly List<Texture2D> _textures = new();
        readonly List<(string g, string id, string name, string acc, string key)> _animals = new();

        void Start()
        {
            _root = (RectTransform)transform;
            Emoji.Use();
            var back = UIKit.CircleButton(_root, "‹", Theme.Card, Theme.Text, 120, 90, () => OnBack());
            ((RectTransform)back.transform).Place(new Vector2(0, 1), new Vector2(110, -110), new Vector2(120, 120));
            _back = back.transform;
            _starText = StickerScreen.StarPill(_root, SaveSystem.Stars);

            // Bilinen hayvan/nesne adı olan resimler (aynı ad bir kez)
            var seen = new HashSet<string>();
            foreach (var g in Level.Index.groups)
                foreach (var l in g.levels)
                    if (Names.IsLearnable(l.id) && Names.TryAnimal(l.id, out var n, out var acc) && seen.Add(n))
                        _animals.Add((g.name, l.id, n, acc, Names.KeyOf(l.id)));


            _mascot = Mascot.Create(_root, 240);
            var mrt = (RectTransform)_mascot.transform;
            mrt.Place(new Vector2(0, 0), new Vector2(170, 170), mrt.sizeDelta);

            ShowMenu();
        }

        int _areaGen;   // her tur / menüde artar: önceki turun gecikmeli işleri yeni turu etkilemesin

        /// <summary>
        /// Gecikmeli iş; arada yeni tur başladıysa ya da menüye dönüldüyse çalışmaz
        /// (eski turun "sıradaki" çağrısı yeni oyunun turunu atlıyordu, eski sayılar yeni nesnelerin üstüne yazılıyordu).
        /// </summary>
        void Later(float seconds, System.Action action)
        {
            int gen = _areaGen;
            Tween.Delay(seconds, () => { if (this != null && gen == _areaGen) action(); });
        }

        void ClearArea()
        {
            _areaGen++;
            if (_area != null) Destroy(_area.gameObject);
            _area = UIKit.Node("Area", _root).Stretch();
            _area.SetSiblingIndex(1);
            _locked = false;
            foreach (var t in _textures) if (t) Destroy(t);
            _textures.Clear();
        }

        // ================= Menü =================

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
            if (_mascot != null) _mascot.gameObject.SetActive(true);
            _mode = mode;
            _streak = 0;
            _roundNo = 0;
            if (mode == Mode.Animals && _animals.Count < 3) { Popup.Toast(Loc.T("soon")); _mode = Mode.Menu; return; }
            NextRound();
        }

        void NextRound()
        {
            ClearArea();
            // soru + tekrar dinle butonu
            _prompt = UIKit.Label(_area, "", 88, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            _prompt.rectTransform.Band(true, 140, 250);
            _prompt.rectTransform.offsetMin = new Vector2(40, _prompt.rectTransform.offsetMin.y);
            _prompt.rectTransform.offsetMax = new Vector2(-40, _prompt.rectTransform.offsetMax.y);
            _prompt.resizeTextForBestFit = true;
            _prompt.resizeTextMinSize = 48;
            _prompt.resizeTextMaxSize = 88;
            _prompt.horizontalOverflow = HorizontalWrapMode.Wrap;
            _prompt.verticalOverflow = VerticalWrapMode.Truncate;   // sığmazsa küçülsün (best fit)
            var sh = _prompt.gameObject.AddComponent<Shadow>();
            sh.effectColor = Theme.Accent.WithAlpha(0.45f);
            sh.effectDistance = new Vector2(0, -6);

            _wantKey = null;
            _missed = false;
            _recorded = false;
            _promptVoice = null;
            int r = _roundNo++;
            bool emoji = Emoji.Available;
            if (_mode == Mode.Colors) { if (emoji && r % 2 == 1) ColorThingRound(); else ColorRound(); }
            else if (_mode == Mode.Shapes) { if (r % 2 == 0) ShapeRound(); else SizeRound(); }
            else if (_mode == Mode.Count) CountDispatch();
            else if (_mode == Mode.Feelings) { int k = r % (SaveSystem.AgeBand == 0 ? 2 : 3); if (k == 0) FeelingsRound(); else if (k == 1) SituationRound(); else ActionRound(); }
            else if (_mode == Mode.Trace) TraceRound(SaveSystem.AgeBand > 0 && TraceBoard.HasLetters && r % 2 == 1);
            else if (_mode == Mode.Letters) { if (SaveSystem.AgeBand == 0 || r % 2 == 0) LetterMatchRound(); else FirstLetterRound(); }
            else if (_mode == Mode.Patterns) PatternRound();
            else if (_mode == Mode.Memory) MemoryRound();
            else if (_mode == Mode.Opposites) OppositeRound();
            else if (_mode == Mode.Sequence) SequenceRound();
            else if (_mode == Mode.Shadows) { if (emoji && r % 2 == 1) EmojiShadowRound(); else ShadowRound(); }
            else if (_mode == Mode.Sort) SortRound();
            else if (_mode == Mode.Seasons) SeasonRound();
            else { if (r % 2 == 0 || !ClassifyRound()) AnimalRound(); }
            Tween.Pop(_prompt.rectTransform, 0.05f, 0.4f);
            Speech.Say(string.IsNullOrEmpty(_promptVoice) ? _prompt.text : _promptVoice);
        }

        void ColorRound()
        {
            int target;
            do target = Random.Range(0, Names.Basic.Length); while (target == _lastTarget);
            _lastTarget = target;
            var picks = PickOptions(target, Names.Basic.Length, SaveSystem.OptionCount, ColorConfusable);
            var t = Names.Basic[target];
            _wantKey = BasicKeys[target];
            _promptVoice = Names.Cap(Loc.T("find", t.acc));
            _prompt.text = _promptVoice;

            int n = picks.Count;
            float gap = n >= 4 ? 236 : 300;
            for (int i = 0; i < n; i++)
            {
                int idx = picks[i];
                var slot = UIKit.Node("Option", _area);
                slot.Place(new Vector2(0.5f, 0.5f), new Vector2((i - (n - 1) * 0.5f) * gap, -120), new Vector2(gap - 30, 720));
                var hit = slot.gameObject.AddComponent<Image>();
                hit.color = Color.clear;
                var cr = Crayon(slot, Names.Basic[idx].color, new Vector2(n >= 4 ? 150 : 170, 640));
                ColorMark(cr, idx, 90);
                cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 0.5f);
                cr.anchoredPosition = Vector2.zero;
                cr.gameObject.AddComponent<Floaty>().Init(10f, 0.6f + i * 0.15f);
                var b = slot.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => Answer(idx == target, slot, Names.Basic[idx].name, key: BasicKeys[idx]));
                A11y.Label(b, Names.Cap(Names.Basic[idx].name));
                cr.localScale = Vector3.zero;
                Tween.Pop(cr, 0.1f + i * 0.08f, 0.45f);
            }
        }

        void AnimalRound()
        {
            int target;
            do target = Random.Range(0, _animals.Count); while (target == _lastTarget && _animals.Count > 1);
            _lastTarget = target;
            var picks = PickOptions(target, _animals.Count, SaveSystem.OptionCount, (x, y) => AnimalConfusable(_animals[x].key, _animals[y].key));
            var a = _animals[target];
            _wantKey = a.key;
            _promptVoice = Names.Cap(Loc.T("find", a.acc));
            _prompt.text = _promptVoice;
            PictureOptions(picks, target);
        }

        /// <summary>Resim kartları (2, 3 ya da 4). Doğru olan: target.</summary>
        void PictureOptions(List<int> picks, int target, string sayRight = null)
        {
            int n = picks.Count;
            for (int i = 0; i < n; i++)
            {
                int idx = picks[i];
                var e = _animals[idx];
                var slot = UIKit.Node("Option", _area);
                var (pos, size) = OptionSlot(i, n);
                slot.Place(new Vector2(0.5f, 0.5f), pos, size);
                slot.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0 ? 1 : -1) * 3f);
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
                b.onClick.AddListener(() => Answer(idx == target, slot, e.name, idx == target ? sayRight : null, sayRight != null ? 3f : 2f, e.key));
                A11y.Label(b, Names.Cap(e.name));
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
            var picks = PickOptions(target, ShapeKeys.Length, SaveSystem.OptionCount, (x, y) => (x == 1 && y == 3) || (x == 3 && y == 1));
            _wantKey = "shape_" + ShapeKeys[target];
            var (_, acc) = Names.Shape(ShapeKeys[target]);
            _promptVoice = Names.Cap(Loc.T("paint_shape", acc));
            _prompt.text = _promptVoice;

            // her şekil boyanınca farklı bir renk alsın
            var colors = new List<int> { 0, 1, 2, 3, 4, 5, 6 };
            Shuffle(colors);
            int n = picks.Count;
            for (int i = 0; i < n; i++)
            {
                int idx = picks[i];
                string key = ShapeKeys[idx];
                var fill = Names.Basic[colors[i]].color;
                var slot = UIKit.Node("Option", _area);
                var (pos, size) = OptionSlot(i, n);
                float side = Mathf.Min(size.x, size.y) - 20;
                slot.Place(new Vector2(0.5f, 0.5f), pos, new Vector2(side, side));
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
                A11y.Label(b, Names.Cap(name));
                b.onClick.AddListener(() =>
                {
                    bool right = idx == target;
                    // doğru şekil parmağın altında renklenir
                    if (right && !_locked) Tween.To(inner, 0.45f, k => inner.color = Color.Lerp(Color.white, fill, k));
                    Answer(right, slot, name, key: "shape_" + key);
                });
                slot.localScale = Vector3.zero;
                Tween.Pop(slot, 0.1f + i * 0.08f, 0.45f);
            }
        }

        // ================= Duygular: "Üzgün yüzü bul!" =================

        static readonly (string key, MascotMood mood)[] Moods =
        {
            ("happy", MascotMood.Happy), ("sad", MascotMood.Sad), ("angry", MascotMood.Angry), ("surprised", MascotMood.Surprised),
            ("scared", MascotMood.Scared), ("proud", MascotMood.Proud), ("calm", MascotMood.Calm), ("shy", MascotMood.Shy),
        };

        /// <summary>Yaşa göre duygu sayısı: 3–4 yaş temel dört duygu, 5–6 yaş korku ve gurur da, 7+ hepsi.</summary>
        static int MoodPool => SaveSystem.AgeBand == 0 ? 4 : SaveSystem.AgeBand == 1 ? 6 : 8;

        // olumlu duygular (mutlu, şaşkın, gururlu, sakin) / olumsuz (üzgün, kızgın, korkmuş, utangaç)
        static bool Positive(int m) => m == 0 || m == 3 || m == 5 || m == 6;
        // anlamca iç içe: hiçbir yaşta aynı turda sorulmaz (gururlu biri de mutludur)
        static bool MoodNever(int a, int b) => a != b && (a == 0 || a == 5 || a == 6) && (b == 0 || b == 5 || b == 6);

        // birbirine benzeyen yüzler: küçüklerde aynı turda gösterilmez
        static bool MoodConfusable(int a, int b)
        {
            int x = Mathf.Min(a, b), y = Mathf.Max(a, b);
            return (x == 0 && y == 3) || (x == 1 && y == 2) || (x == 3 && y == 4) || (x == 0 && y == 5) || (x == 0 && y == 6) || (x == 1 && y == 7);
        }

        readonly List<int> _feelBag = new();

        void FeelingsRound()
        {
            // torba: dört duygunun hepsi sorulmadan hiçbiri tekrar gelmez (art arda da gelmez)
            if (_feelBag.Count == 0)
            {
                for (int k = 0; k < MoodPool; k++) _feelBag.Add(k);
                Shuffle(_feelBag);
                if (_feelBag[0] == _lastTarget) { _feelBag.RemoveAt(0); _feelBag.Add(_lastTarget); }
            }
            int target = _feelBag[0];
            _feelBag.RemoveAt(0);
            _lastTarget = target;
            var picks = PickOptions(target, MoodPool, SaveSystem.OptionCount, MoodConfusable);
            // mutlu–gururlu–sakin birlikte çıkmasın: yerine başka bir duygu
            for (int i = 0; i < picks.Count; i++)
            {
                if (!MoodNever(target, picks[i])) continue;
                for (int o = 0; o < MoodPool; o++)
                    if (!picks.Contains(o) && !MoodNever(target, o)) { picks[i] = o; break; }
            }
            for (int i = picks.Count - 1; i >= 0; i--)
                if (MoodNever(target, picks[i])) picks.RemoveAt(i);
            _wantKey = "feel_" + Moods[target].key;
            var (_, acc) = Names.Feel(Moods[target].key);
            _promptVoice = Names.Cap(Loc.T("find", acc));
            _prompt.text = _promptVoice;

            int n = picks.Count;
            for (int i = 0; i < n; i++)
            {
                int idx = picks[i];
                var slot = UIKit.Node("Option", _area);
                var (pos, size) = OptionSlot(i, n);
                slot.Place(new Vector2(0.5f, 0.5f), pos, size);
                var hit = slot.gameObject.AddComponent<Image>();
                hit.color = Color.clear;
                var m = Mascot.Create(slot, Mathf.Min(size.x, size.y) - 10, false);
                m.SetMood(Moods[idx].mood);
                m.SetInteractive(false);
                var mrt = (RectTransform)m.transform;
                mrt.anchorMin = mrt.anchorMax = new Vector2(0.5f, 0.5f);
                mrt.anchoredPosition = Vector2.zero;
                var name = Names.Feel(Moods[idx].key).name;
                var b = slot.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { if (idx == target && !_locked) m.Jump(); Answer(idx == target, slot, name, key: "feel_" + Moods[idx].key); });
                A11y.Label(b, Names.Cap(name));
                slot.localScale = Vector3.zero;
                Tween.Pop(slot, 0.1f + i * 0.08f, 0.45f);
            }
        }

        // ================= Duygular 2: "Pofuduk'un balonu uçtu. Nasıl hisseder?" =================

        readonly List<int> _sitBag = new();
        int _lastSit = -1;

        // durum -> duygu (Moods dizini) ve küçük resim (-1: resimsiz)
        static readonly (int mood, int icon)[] Situations =
        {
            (1, 0), (0, 1), (2, 2), (3, 3), (0, 4), (1, -1), (2, -1), (3, 3),
            (4, 5), (5, 6), (6, 4), (7, -1),
        };

        void SituationRound()
        {
            if (_sitBag.Count == 0)
            {
                for (int k = 0; k < Situations.Length; k++) if (Situations[k].mood < MoodPool) _sitBag.Add(k);
                Shuffle(_sitBag);
                if (_sitBag[0] == _lastSit) { _sitBag.RemoveAt(0); _sitBag.Add(_lastSit); }
            }
            int sit = _sitBag[0];
            _sitBag.RemoveAt(0);
            _lastSit = sit;
            var (target, icon) = Situations[sit];

            _prompt.text = Loc.T("feel_s" + sit);
            _prompt.resizeTextForBestFit = true;
            _prompt.resizeTextMinSize = 36;
            _prompt.resizeTextMaxSize = 60;
            _prompt.horizontalOverflow = HorizontalWrapMode.Wrap;
            _prompt.verticalOverflow = VerticalWrapMode.Truncate;   // sığmazsa küçülsün (best fit)
            _promptVoice = Loc.T("feel_s" + sit) + " " + Loc.T("feelings_sub");

            // durumu anlatan küçük resim
            if (icon >= 0)
            {
                RectTransform pic;
                if (icon == 2)
                {
                    pic = Crayon(_area, Names.Basic[0].color, new Vector2(50, 150));
                    pic.localRotation = Quaternion.Euler(0, 0, -35);
                }
                else
                {
                    var (sprite, col) = icon switch
                    {
                        0 => (Art.Balloon, Names.Basic[0].color),
                        1 => (Art.Flower, Names.Basic[5].color),
                        4 => (Art.Heart, Names.Basic[0].color),
                        5 => (Art.CloudShape, Theme.Hex("8C8FA8")),
                        6 => (Art.Star5, Names.Basic[1].color),
                        _ => (Art.Star5, Names.Basic[2].color),
                    };
                    var img = UIKit.Box(_area, "Pic", col, sprite);
                    img.type = Image.Type.Simple;
                    img.raycastTarget = false;
                    pic = img.rectTransform;
                    pic.sizeDelta = new Vector2(130, 130);
                }
                pic.anchorMin = pic.anchorMax = new Vector2(0.5f, 0.5f);
                pic.anchoredPosition = new Vector2(-200, 340);
                pic.gameObject.AddComponent<Floaty>().Init(8f, 0.7f);
            }

            // durum sorusunda benzer yüzler hiç gösterilmez (cevap tek ve açık olsun)
            var picks = new List<int> { target };
            var others = new List<int>();
            // yalnızca öbür türden duygular: "gök gürledi" sorusunda "üzgün" de makul olurdu, sorulmaz
            for (int o = 0; o < MoodPool; o++)
                if (o != target && Positive(o) != Positive(target)) others.Add(o);
            Shuffle(others);
            for (int o = 0; o < others.Count && picks.Count < SaveSystem.OptionCount; o++) picks.Add(others[o]);
            Shuffle(picks);
            _wantKey = "feel_" + Moods[target].key;
            var tip = Loc.T("feel_tip_" + Moods[target].key);
            int n = picks.Count;
            for (int i = 0; i < n; i++)
            {
                int idx = picks[i];
                var slot = UIKit.Node("Option", _area);
                var (pos, size) = OptionSlot(i, n);
                slot.Place(new Vector2(0.5f, 0.5f), pos - new Vector2(0, 20), size);
                var hit = slot.gameObject.AddComponent<Image>();
                hit.color = Color.clear;
                var m = Mascot.Create(slot, Mathf.Min(size.x, size.y) - 30, false);
                m.SetMood(Moods[idx].mood);
                m.SetInteractive(false);
                var mrt = (RectTransform)m.transform;
                mrt.anchorMin = mrt.anchorMax = new Vector2(0.5f, 0.5f);
                mrt.anchoredPosition = Vector2.zero;
                var name = Names.Feel(Moods[idx].key).name;
                bool right = idx == target;
                var b = slot.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                A11y.Label(b, Names.Cap(name));
                b.onClick.AddListener(() =>
                {
                    if (right && !_locked) m.Jump();
                    // doğruda empati cümlesi: "Üzgün birine sarılmak iyi gelir."
                    Answer(right, slot, name, right ? Loc.T("cheer_" + Random.Range(0, 4)) + " " + tip : null, 3.8f, "feel_" + Moods[idx].key);
                });
                slot.localScale = Vector3.zero;
                Tween.Pop(slot, 0.1f + i * 0.08f, 0.45f);
            }
        }

        // ================= Rakam yaz: noktalı rakamın üzerinden geç =================

        void TraceRound(bool letter)
        {
            int d = PlayerPrefs.GetInt("traceNext", 1);
            if (d < 0 || d > 9) d = 1;   // sıra: 1, 2, … 9, sonra 0
            string letters = TraceBoard.Letters;
            int li = Mathf.Clamp(PlayerPrefs.GetInt("traceLetter", 0), 0, letters.Length - 1);
            var (name, acc) = Names.Num(d);
            if (letter) { name = letters[li].ToString(); _prompt.text = Loc.T("trace_letter_q", name); }
            else _prompt.text = Names.Cap(Loc.T("trace_q", acc));
            _promptVoice = _prompt.text;

            var card = UIKit.Card(_area, "Board", Theme.Card.WithAlpha(0.92f), 0.6f, 4f);
            var crt = (RectTransform)card.transform.parent;
            crt.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(780, 920));
            var tb = letter ? TraceBoard.CreateLetter(card.transform, new Vector2(780, 920), letters[li], Names.Basic[4].color)
                            : TraceBoard.Create(card.transform, new Vector2(780, 920), d, Names.Basic[4].color);
            var trt = (RectTransform)tb.transform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.anchoredPosition = Vector2.zero;
            crt.localScale = Vector3.zero;
            Tween.Pop(crt, 0.1f, 0.45f);

            var again = UIKit.Button(_area, Loc.T("trace_clear"), Theme.Paper, Theme.Text, 44, () => { Sfx.Tap(); tb.Clear(); }, 0.5f);
            ((RectTransform)again.transform).Place(new Vector2(0.5f, 0.5f), new Vector2(0, -610), new Vector2(320, 110));

            tb.Completed += () =>
            {
                if (this == null) return;
                if (letter) PlayerPrefs.SetInt("traceLetter", (li + 1) % letters.Length);
                else PlayerPrefs.SetInt("traceNext", d == 9 ? 0 : d == 0 ? 1 : d + 1);
                Stats.Done("trace");   // yazma doğru/yanlış değil: tamamlanan sayılır
                _locked = true;
                Sfx.Star();
                Tween.Delay(0.15f, Sfx.Win);
                Tween.Punch(crt, 0.15f, 0.5f);
                Confetti.Burst(_area, 50);
                _mascot.Jump();
                _mascot.Wave(1.2f);
                SaveSystem.Stars += 1;
                _starText.text = SaveSystem.Stars.ToString();
                Tween.Punch(_starText.transform.parent, 0.25f);
                string cheer = Loc.T("cheer_" + Random.Range(0, 4));
                Speech.Say(Loc.T("right_say", cheer, Loc.Lang == "tr" ? name : Names.Cap(name)));
                _prompt.text = cheer;
                Later(2.8f, () => { if (this != null && _mode == Mode.Trace) NextRound(); });
            };
        }

        // ================= Sayılar: "Kaç tane yıldız var?" =================

        // doğru cevaplarla artar: sayı aralığı çocuğun hızına göre büyür. Cihazda saklanır (her girişte başa sarmaz);
        // ilk kez: 3–4 yaş 1-4 ile, 5–6 karşılaştırmayla, 7+ toplamayla başlar
        int _countLevel { get => SaveSystem.CountLevel; set => SaveSystem.CountLevel = value; }
        int _countAnswer;
        bool _countingTogether;
        readonly List<RectTransform> _countItems = new();

        static Sprite CountSprite(int k) => k switch { 0 => Art.Heart, 1 => Art.Star5, 2 => Art.Flower, _ => Art.Balloon };

        int _countRoundNo;

        /// <summary>Sayı turları: önce sayma; ilerledikçe karşılaştırma, sonra toplama da araya girer.</summary>
        void CountDispatch()
        {
            var kinds = new List<int> { 0 };
            if (_countLevel >= 3) kinds.Add(1);
            if (_countLevel >= 6) kinds.Add(2);
            if (_countLevel >= 9) kinds.Add(3);                                // çıkarma
            if (_countLevel >= 11 && SaveSystem.AgeBand > 0) kinds.Add(4);     // 10–20 (onluk çerçeve)
            int k = kinds[_countRoundNo++ % kinds.Count];
            if (k == 1) CompareRound();
            else if (k == 2) AddRound();
            else if (k == 3) SubRound();
            else if (k == 4) TeenRound();
            else CountRound();
        }

        Color CountColor()
        {
            // açık renkler (beyaz, krem) kartta kaybolmasın
            var c = Names.Basic[Random.Range(0, Names.Basic.Length)].color;
            return c.grayscale > 0.85f ? Names.Basic[0].color : c;
        }

        // "Hangi tarafta daha çok var?" — iki kart, çok olana dokun
        bool _cmpShown, _cmpLess;

        void CompareRound()
        {
            _countItems.Clear();
            _countingTogether = false;
            _cmpShown = false;
            int max = Mathf.Clamp(4 + _countLevel / 2, 4, 9);
            int minDiff = _countLevel < 7 ? 2 : 1;   // önce fark belirgin, sonra yakın sayılar
            int a, b;
            do { a = Random.Range(1, max + 1); b = Random.Range(1, max + 1); } while (Mathf.Abs(a - b) < minDiff);
            int kind = Random.Range(0, 4);
            var color = CountColor();
            _cmpLess = _countLevel >= 5 && Random.value < 0.5f;
            _promptVoice = Loc.T(_cmpLess ? "cmp_q_less" : "cmp_q");
            _prompt.text = _promptVoice;
            int more = _cmpLess ? Mathf.Min(a, b) : Mathf.Max(a, b);

            for (int side = 0; side < 2; side++)
            {
                int n = side == 0 ? a : b;
                var slot = UIKit.Node("Option", _area);
                slot.Place(new Vector2(0.5f, 0.5f), new Vector2((side == 0 ? -1 : 1) * 225, -40), new Vector2(420, 760));
                var face = UIKit.Card(slot, "Board", Theme.Card.WithAlpha(0.9f), 0.6f, 4f);
                ((RectTransform)face.transform.parent).Stretch();
                face.raycastTarget = true;
                var cells = new List<int>();
                for (int c = 0; c < 10; c++) cells.Add(c);
                Shuffle(cells);
                for (int i = 0; i < n; i++)
                {
                    int c = cells[i];
                    var item = UIKit.Box(face.transform, "Item", color, CountSprite(kind));
                    item.type = Image.Type.Simple;
                    item.raycastTarget = false;
                    var irt = item.rectTransform;
                    irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f);
                    irt.anchoredPosition = new Vector2(-90 + (c % 2) * 180 + Random.Range(-14f, 14f), 280 - (c / 2) * 140 + Random.Range(-10f, 10f));
                    irt.sizeDelta = new Vector2(112, 112);
                    irt.localRotation = Quaternion.Euler(0, 0, Random.Range(-14f, 14f));
                    irt.localScale = Vector3.zero;
                    Tween.Pop(irt, 0.1f + i * 0.05f + side * 0.2f, 0.4f);
                }
                bool right = n == more;
                var bt = slot.gameObject.AddComponent<Button>();
                bt.transition = Selectable.Transition.None;
                bt.onClick.AddListener(() => CompareAnswer(right, slot, a, b));
                A11y.Label(bt, n.ToString());
                slot.localScale = Vector3.zero;
                Tween.Pop(slot, 0.05f + side * 0.1f, 0.4f);
            }
        }

        void CompareAnswer(bool right, RectTransform option, int a, int b)
        {
            if (_locked) return;
            _wantKey = null;
            Record(right, null);
            int more = _cmpLess ? Mathf.Min(a, b) : Mathf.Max(a, b);
            if (right)
            {
                if (!_missed) _countLevel++;
                Celebrate(option);
                string cheer = Loc.T("cheer_" + Random.Range(0, 4));
                Speech.Say(Loc.T(_cmpLess ? "cmp_right_less" : "cmp_right", cheer, more));
                _prompt.text = cheer;
                Later(2.4f, () => { if (this != null && _mode == Mode.Count) NextRound(); });
                return;
            }
            _missed = true;
            Sfx.Wrong();
            Shake(option);
            if (_cmpShown) return;
            // kızmadan gösteririz: iki kartın üstüne sayıları yaz
            _cmpShown = true;
            _countLevel = Mathf.Max(0, _countLevel - 1);
            for (int side = 0; side < 2; side++)
            {
                var num = UIKit.Label(_area, (side == 0 ? a : b).ToString(), 96, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                num.raycastTarget = false;
                num.rectTransform.anchorMin = num.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                num.rectTransform.anchoredPosition = new Vector2((side == 0 ? -1 : 1) * 225, 400);
                num.rectTransform.sizeDelta = new Vector2(200, 130);
                Tween.Pop(num.rectTransform, 0.1f + side * 0.5f, 0.4f);
            }
            Speech.Say(Loc.T(_cmpLess ? "cmp_show_less" : "cmp_show", a, b, more));
        }

        // "3 tane var, 2 tane daha geldi. Hepsi kaç tane?" — basit toplama
        void AddRound()
        {
            _countItems.Clear();
            _countingTogether = false;
            int sumMax = Mathf.Clamp(4 + (_countLevel - 6) / 2, 4, 9);
            int a = Random.Range(1, sumMax), b = Random.Range(1, sumMax - a + 1);
            _countAnswer = a + b;
            _prompt.text = Loc.T("add_q");
            _promptVoice = Loc.T("add_say", a, b);
            int kind = Random.Range(0, 4);
            var color = CountColor();

            var board = UIKit.Card(_area, "Board", Theme.Card.WithAlpha(0.85f), 0.6f, 4f);
            var brt = (RectTransform)board.transform.parent;
            brt.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(880, 600));
            var plus = UIKit.Label(board.transform, "+", 120, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            plus.raycastTarget = false;
            plus.rectTransform.anchorMin = plus.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            plus.rectTransform.sizeDelta = new Vector2(120, 160);
            plus.rectTransform.anchoredPosition = Vector2.zero;
            for (int side = 0; side < 2; side++)
            {
                int n = side == 0 ? a : b;
                for (int i = 0; i < n; i++)
                {
                    // her grup kendi yarısında 3 sütun x 3 satır, soldan sağa sıralı (birlikte sayarken göz takip etsin)
                    int col = i % 3, row = i / 3;
                    float x = (side == 0 ? -340 : 120) + col * 110;
                    var item = UIKit.Box(board.transform, "Item", color, CountSprite(kind));
                    item.type = Image.Type.Simple;
                    item.raycastTarget = false;
                    var irt = item.rectTransform;
                    irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f);
                    int rows = (n + 2) / 3;
                    irt.anchoredPosition = new Vector2(x, (rows - 1) * 85f - row * 170);
                    irt.sizeDelta = new Vector2(100, 100);
                    irt.localRotation = Quaternion.Euler(0, 0, Random.Range(-12f, 12f));
                    irt.localScale = Vector3.zero;
                    // ikinci grup sonradan "gelir"
                    Tween.Pop(irt, side == 0 ? 0.1f + i * 0.06f : 1.4f + i * 0.12f, 0.4f);
                    _countItems.Add(irt);
                }
            }

            if (SaveSystem.AgeBand >= 2) Equation(a + " + " + b + " = ?");
            NumberOptions(_countAnswer, 1, 10, 1.6f);
        }

        /// <summary>Doğru cevap kutlaması. Yıldız (ve konfeti) yalnızca turun ilk denemesinde doğru bilinince.</summary>
        void Celebrate(RectTransform option)
        {
            _locked = true;
            Sfx.Star();
            Tween.Punch(option, 0.25f, 0.5f);
            _mascot.Jump();
            _mascot.Wave(1.2f);
            if (_missed) return;   // sonradan buldu: sevinç var, yıldız yok
            Tween.Delay(0.15f, Sfx.Win);
            Confetti.Burst(_area, 50);
            SaveSystem.Stars += 1;
            _starText.text = SaveSystem.Stars.ToString();
            Tween.Punch(_starText.transform.parent, 0.25f);
        }

        /// <summary>Turun ilk cevabını rapora yazar (sonraki dokunuşlar deneme-yanılmadır, sayılmaz).</summary>
        void Record(bool right, string picked)
        {
            if (_recorded) return;
            _recorded = true;
            Stats.Answer(GameKey, right);
            if (!right) Stats.Confused(GameKey, _wantKey, picked);
        }

        /// <summary>Yanlış seçeneğin hafifçe sallanması (hareket azaltmada yok).</summary>
        static void Shake(RectTransform option)
        {
            if (Motion.Reduced) return;
            var basePos = option.anchoredPosition;
            Tween.To(option, 0.4f, k => option.anchoredPosition = basePos + new Vector2(Mathf.Sin(k * Mathf.PI * 6) * 18f * (1 - k), 0));
        }

        /// <summary>
        /// Yanlış seçeneğin alt kenarına adını yazar ("Turuncu"): çocuk neye dokunduğunu öğrenir.
        /// Etiket seçeneğin kardeşidir, seçenekle birlikte solmaz.
        /// </summary>
        void NameTag(RectTransform option, string name)
        {
            if (string.IsNullOrEmpty(name) || option == null || _area == null || option.parent != _area) return;
            string id = "Tag " + option.GetHashCode();
            if (_area.Find(id) != null) return;
            string text = Names.Cap(name);
            var tag = UIKit.Card(_area, id, Color.white, 0.5f, 4f);
            var trt = (RectTransform)tag.transform.parent;
            float w = Mathf.Clamp(90 + text.Length * 30, 200, Mathf.Max(200, option.sizeDelta.x + 40));
            float bottom = option.anchoredPosition.y - option.sizeDelta.y * 0.5f;
            trt.Place(new Vector2(0.5f, 0.5f), new Vector2(option.anchoredPosition.x, bottom + 34), new Vector2(w, 86));
            var l = UIKit.Label(tag.transform, text, 48, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            l.rectTransform.Stretch(12, 4, 12, 8);
            l.horizontalOverflow = HorizontalWrapMode.Wrap;
            l.verticalOverflow = VerticalWrapMode.Truncate;
            l.resizeTextForBestFit = true;
            l.resizeTextMinSize = 26;
            l.resizeTextMaxSize = 48;
            A11y.Announce(text);
            Tween.Pop(trt, 0f, 0.35f);
        }

        void CountRound()
        {
            _countItems.Clear();
            _countingTogether = false;
            int max = Mathf.Clamp(4 + _countLevel / 2, 4, 9);   // 1-4 ile başlar, en çok 9
            int n;
            do n = Random.Range(1, max + 1); while (n == _countAnswer && max > 1);
            _countAnswer = n;
            int kind = Random.Range(0, 4);
            // çoğu turda gerçek nesneler (meyveler, hayvanlar, taşıtlar…); bazen eski kalp/yıldız/çiçek/balon
            string emo = CountEmoji();
            _promptVoice = emo != null ? Loc.T("count_q_any") : Loc.T("count_q_" + kind);
            _prompt.text = _promptVoice;
            var color = emo != null ? Color.white : CountColor();

            // nesneleri 4x3 ızgaraya karışık yerleştir (üst üste binmesin)
            var board = UIKit.Card(_area, "Board", Theme.Card.WithAlpha(0.85f), 0.6f, 4f);
            var brt = (RectTransform)board.transform.parent;
            brt.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(880, 600));
            var cells = new List<int>();
            for (int c = 0; c < 12; c++) cells.Add(c);
            Shuffle(cells);
            // seçilen hücreler soldan sağa, yukarıdan aşağı sıralı: birlikte sayarken göz takip etsin
            var chosen = cells.GetRange(0, n);
            chosen.Sort();
            var spots = new List<Vector2>();
            foreach (int c in chosen)
                spots.Add(new Vector2(-300 + (c % 4) * 200 + Random.Range(-20f, 20f), 170 - (c / 4) * 170 + Random.Range(-15f, 15f)));
            for (int i = 0; i < n; i++)
            {
                var item = UIKit.Box(board.transform, "Item", color, emo != null ? Emoji.Get(emo) : CountSprite(kind));
                item.type = Image.Type.Simple;
                item.preserveAspect = true;
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

            // sayı seçenekleri (biri doğru), yaşa göre 2–4 tane
            NumberOptions(n, 1, max + 1, 0.3f);
        }

        void CountAnswer(int value, RectTransform option)
        {
            if (_locked || _countingTogether) return;
            _wantKey = _countAnswer.ToString();
            Record(value == _countAnswer, value.ToString());
            if (value == _countAnswer)
            {
                if (!_missed) _countLevel++;
                Celebrate(option);
                string cheer = Loc.T("cheer_" + Random.Range(0, 4));
                Speech.Say(Loc.T("count_right", cheer, _countAnswer));
                _prompt.text = cheer;
                foreach (var it in _countItems) Tween.Punch(it, 0.2f, 0.4f);
                Later(2.2f, () => { if (this != null && _mode == Mode.Count) NextRound(); });
                return;
            }
            // Yanlış: kızmayız, birlikte tek tek sayarız (her nesneye bir sayı)
            if (!_missed) _countLevel = Mathf.Max(0, _countLevel - 1);   // turda bir kez düşer
            _missed = true;
            Sfx.Wrong();
            if (!option.TryGetComponent<CanvasGroup>(out var cg)) cg = option.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0.45f;
            cg.interactable = false;
            if (_countItems.Count == 0 || _countItems.Count > 12) return;   // 10–20 ve çıkarmada tek tek saymayız
            _countingTogether = true;
            Speech.Say(Loc.T("count_together"));
            float t0 = 1.6f;
            for (int i = 0; i < _countItems.Count; i++)
            {
                int k = i;
                Later(t0 + i * 0.9f, () =>
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
            Later(t0 + _countItems.Count * 0.9f + 0.3f, () => { if (this != null) _countingTogether = false; });
        }

        string GameKey => _mode switch
        {
            Mode.Colors => "colors", Mode.Animals => "pictures", Mode.Shapes => "shapes",
            Mode.Count => "counting", Mode.Feelings => "feelings", Mode.Letters => "letters",
            Mode.Patterns => "patterns", Mode.Memory => "memory",
            Mode.Opposites => "opposites", Mode.Sequence => "sequence", Mode.Shadows => "shadows", Mode.Seasons => "seasons",
            Mode.Sort => "sorting",
            _ => "trace",
        };

        void Answer(bool right, RectTransform option, string name, string sayRight = null, float nextDelay = 2.0f, string key = null)
        {
            if (_locked) return;
            Record(right, key);
            _streak = right && !_missed ? _streak + 1 : 0;
            if (right)
            {
                Celebrate(option);
                string cheer = Loc.T("cheer_" + Random.Range(0, 4));
                if (sayRight != null) cheer = sayRight;   // ses yok: ipucunun tamamı ekranda
                _prompt.text = cheer;
                Later(nextDelay, () => { if (this != null && _mode != Mode.Menu) NextRound(); });
            }
            else
            {
                _missed = true;
                Sfx.Wrong();
                Shake(option);
                // yanlış seçenek biraz solar ki çocuk diğerlerine baksın; adı alt kenarında yazar
                if (!option.TryGetComponent<CanvasGroup>(out var cg)) cg = option.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0.45f;
                NameTag(option, name);
            }
        }

        static void Shuffle<T>(List<T> l)
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
            if (_skyTex) Destroy(_skyTex);
            if (_grassTex) Destroy(_grassTex);
            Emoji.Release();   // kullanan kalmadıysa atlasları bırak
        }
    }
}
