using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Öğren ekranının ikinci yarısı: yaşa göre seçenek sayısı ve yeni oyunlar.
    ///  • Şekiller kartında: büyük–küçük, uzun–kısa.
    ///  • Resimler kartında: sınıflama ("Hangisi denizde yaşar?").
    ///  • Sayılar kartında: çıkarma ve 10–20 (onluk çerçeve).
    ///  • Duygular kartında: "Ne yapabilir?" (duyguyla baş etme).
    ///  • Harfler: aynı harfi bul, resmin ilk harfi.  • Örüntü: sırada ne var?  • Hafıza: eşini bul.
    /// </summary>
    public partial class LearnScreen
    {
        int _streak, _roundNo;
        bool _missed;      // bu turda yanlış dokunuş oldu mu (yıldız yalnızca ilk denemede)
        bool _recorded;    // turun ilk cevabı rapora yazıldı mı
        string _wantKey;   // sorulan şeyin anahtarı (rapordaki karışıklık kaydı için)

        static readonly string[] BasicKeys = { "red", "orange", "yellow", "green", "blue", "purple", "pink", "brown" };

        // ================= Ortak: seçenekler =================

        /// <summary>
        /// Doğru cevap + çeldiriciler. Benzer olanlar (turuncu–kırmızı, kare–dikdörtgen) yaşa göre:
        /// 3–4 yaş hiç gelmez, 5–6 yaş art arda 3 doğrudan sonra gelir, 7+ yaş bilerek konur.
        /// </summary>
        List<int> PickOptions(int target, int total, int n, System.Func<int, int, bool> confusable)
        {
            n = Mathf.Min(n, total);
            var picks = new List<int> { target };
            var near = new List<int>();
            var far = new List<int>();
            for (int i = 0; i < total; i++)
            {
                if (i == target) continue;
                if (confusable != null && confusable(target, i)) near.Add(i); else far.Add(i);
            }
            Shuffle(near);
            Shuffle(far);
            bool allowNear = SaveSystem.AgeBand >= 2 || (SaveSystem.AgeBand == 1 && _streak >= 3);
            if (allowNear && near.Count > 0) { picks.Add(near[0]); near.RemoveAt(0); }
            foreach (var i in far) { if (picks.Count >= n) break; picks.Add(i); }
            // uzak seçenek yetmezse (az eleman varsa) benzerlerle tamamla
            foreach (var i in near) { if (picks.Count >= n) break; picks.Add(i); }
            Shuffle(picks);
            return picks;
        }

        static bool ColorConfusable(int a, int b)
        {
            int x = Mathf.Min(a, b), y = Mathf.Max(a, b);
            // kırmızı–turuncu, turuncu–sarı, mavi–mor, mor–pembe, kırmızı–pembe, turuncu–kahverengi
            return (x == 0 && y == 1) || (x == 1 && y == 2) || (x == 4 && y == 5) || (x == 5 && y == 6) || (x == 0 && y == 6) || (x == 1 && y == 7);
        }

        static readonly string[][] AnimalFamilies =
        {
            new[] { "lion", "tiger" }, new[] { "dog", "puppy", "wolf", "fox" }, new[] { "duck", "chick", "hen" },
            new[] { "bear", "polarbear", "teddybear", "panda" }, new[] { "dolphin", "whale", "fish", "seal" },
            new[] { "flower", "flowers", "rose", "tulip", "lily", "sunflower" }, new[] { "cupcake", "ice" },
        };

        static bool AnimalConfusable(string a, string b)
        {
            foreach (var f in AnimalFamilies)
                if (System.Array.IndexOf(f, a) >= 0 && System.Array.IndexOf(f, b) >= 0) return true;
            return false;
        }

        /// <summary>Seçenek kartlarının yeri ve boyu: 2 yan yana, 3 üçgen, 4 ikiye iki.</summary>
        static (Vector2 pos, Vector2 size) OptionSlot(int i, int n)
        {
            if (n <= 2) return (new Vector2((i == 0 ? -1 : 1) * 235, -120), new Vector2(430, 460));
            if (n == 3)
                return i < 2 ? (new Vector2((i == 0 ? -1 : 1) * 225, 60), new Vector2(400, 420))
                             : (new Vector2(0, -400), new Vector2(400, 420));
            return (new Vector2((i % 2 == 0 ? -1 : 1) * 220, 120 - (i / 2) * 440), new Vector2(390, 400));
        }

        static void ColorMark(RectTransform crayon, int basicIdx, float size) => ColorMarks.AddBasic(crayon, basicIdx, size);

        /// <summary>Sayı seçenekleri: yaşa göre 2–4 buton, cevabın çevresinden.</summary>
        void NumberOptions(int answer, int min, int max, float delay)
        {
            int n = SaveSystem.OptionCount;
            var picks = new List<int> { answer };
            int guard = 0;
            while (picks.Count < n && guard++ < 200)
            {
                int o = Mathf.Clamp(answer + Random.Range(-3, 4), min, max);
                if (!picks.Contains(o)) picks.Add(o);
            }
            picks.Sort();
            float gap = picks.Count >= 4 ? 236 : 270, size = picks.Count >= 4 ? 200 : 210;
            for (int i = 0; i < picks.Count; i++)
            {
                int val = picks[i];
                var bt = UIKit.Button(_area, val.ToString(), Theme.Card, Theme.Text, 104, null, 0.5f);
                var rt = (RectTransform)bt.transform;
                rt.Place(new Vector2(0.5f, 0.5f), new Vector2((i - (picks.Count - 1) * 0.5f) * gap, -440), new Vector2(size, size));
                UIKit.ButtonText(bt).fontStyle = FontStyle.Bold;
                bt.onClick.AddListener(() => CountAnswer(val, rt));
                rt.localScale = Vector3.zero;
                Tween.Pop(rt, delay + i * 0.08f, 0.45f);
            }
        }

        /// <summary>7+ yaş için tahtanın altında işlem: "3 + 2 = ?"</summary>
        void Equation(string text)
        {
            var eq = UIKit.Label(_area, text, 76, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            eq.raycastTarget = false;
            eq.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -290), new Vector2(700, 100));
        }

        Text BigText(Transform parent, string text, int size)
        {
            var t = UIKit.Label(parent, text, size, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Stretch();
            t.raycastTarget = false;
            return t;
        }

        // ================= Menü: yeni kartlar =================

        void MoreMenuCards()
        {
            // Harfler: alfabenin ilk iki harfi
            var letters = MenuCard(Cell(5), Loc.T("letters"), Loc.T("letters_sub"), Theme.Hex("F5C6E0"), 0.58f, () => StartGame(Mode.Letters));
            string ab = Alphabet;
            for (int i = 0; i < 2 && i < ab.Length; i++)
            {
                var l = UIKit.Label(letters, ab[i].ToString(), 92, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                l.raycastTarget = false;
                var lr = l.rectTransform;
                lr.anchorMin = lr.anchorMax = new Vector2(0.5f, 0.80f);
                lr.sizeDelta = new Vector2(100, 130);
                lr.anchoredPosition = new Vector2((i == 0 ? -1 : 1) * 42, i == 0 ? 6 : -6);
                lr.localRotation = Quaternion.Euler(0, 0, i == 0 ? 8 : -8);
            }

            // Örüntü: kırmızı-mavi-kırmızı-mavi
            var pat = MenuCard(Cell(7), Loc.T("patterns"), Loc.T("patterns_sub"), Theme.Hex("CDEBB5"), 0.66f, () => StartGame(Mode.Patterns));
            for (int i = 0; i < 4; i++)
            {
                var d = UIKit.Box(pat, "Dot", i % 2 == 0 ? Names.Basic[0].color : Names.Basic[4].color, i % 2 == 0 ? Art.Shape("circle") : Art.Shape("square"));
                d.type = Image.Type.Simple;
                d.raycastTarget = false;
                d.rectTransform.anchorMin = d.rectTransform.anchorMax = new Vector2(0.5f, 0.80f);
                d.rectTransform.sizeDelta = new Vector2(48, 48);
                d.rectTransform.anchoredPosition = new Vector2((i - 1.5f) * 56, 0);
            }

            // Hafıza: biri kapalı biri açık iki kart
            var mem = MenuCard(Cell(8), Loc.T("memory"), Loc.T("memory_sub"), Theme.Hex("EBD9C4"), 0.74f, () => StartGame(Mode.Memory));
            for (int i = 0; i < 2; i++)
            {
                var c = UIKit.Box(mem, "Card", i == 0 ? Theme.Lavender : Color.white, null, 0.4f);
                c.raycastTarget = false;
                var cr = c.rectTransform;
                cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 0.80f);
                cr.sizeDelta = new Vector2(70, 92);
                cr.anchoredPosition = new Vector2((i == 0 ? -1 : 1) * 42, 0);
                cr.localRotation = Quaternion.Euler(0, 0, i == 0 ? 8 : -8);
                var ic = UIKit.Box(cr, "Icon", i == 0 ? Color.white : Names.Basic[0].color, i == 0 ? Art.Star5 : Art.Heart);
                ic.type = Image.Type.Simple;
                ic.raycastTarget = false;
                ic.rectTransform.Stretch(14, 22, 14, 22);
            }
        }

        // ================= Şekiller 2: büyük–küçük, uzun–kısa =================

        void SizeRound()
        {
            int n = SaveSystem.OptionCount;
            int kind = Random.Range(0, SaveSystem.AgeBand == 0 ? 2 : 4);   // 0 büyük, 1 küçük, 2 uzun, 3 kısa
            _prompt.text = Loc.T(kind switch { 0 => "size_big", 1 => "size_small", 2 => "size_long", _ => "size_short" });
            // boylar birbirinden belirgin farklı (yaş büyüdükçe fark azalır)
            float minScale = SaveSystem.AgeBand == 0 ? 0.45f : SaveSystem.AgeBand == 1 ? 0.55f : 0.62f;
            var scales = new List<float>();
            for (int i = 0; i < n; i++) scales.Add(n == 1 ? 1f : Mathf.Lerp(minScale, 1f, i / (float)(n - 1)));
            var order = new List<int>();
            for (int i = 0; i < n; i++) order.Add(i);
            Shuffle(order);
            int want = kind == 0 || kind == 2 ? n - 1 : 0;   // en büyük ya da en küçük ölçek
            var color = CountColor();
            // ekran okuyucu ve yanlış cevaptaki ad etiketi: "en büyük", "orta", "en kısa"…
            string SizeName(int si) => Loc.T(si == n - 1 ? (kind >= 2 ? "sz_longest" : "sz_biggest")
                                           : si == 0 ? (kind >= 2 ? "sz_shortest" : "sz_smallest") : "sz_middle");

            if (kind >= 2)
            {
                // kalemler yan yana, tabanları aynı hizada
                float gap = n >= 4 ? 236 : 300;
                for (int i = 0; i < n; i++)
                {
                    int si = order[i];
                    var slot = UIKit.Node("Option", _area);
                    slot.Place(new Vector2(0.5f, 0.5f), new Vector2((i - (n - 1) * 0.5f) * gap, -120), new Vector2(gap - 30, 720));
                    slot.gameObject.AddComponent<Image>().color = Color.clear;
                    var cr = Crayon(slot, color, new Vector2(n >= 4 ? 140 : 160, 660 * scales[si]));
                    cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 0f);
                    cr.pivot = new Vector2(0.5f, 0f);
                    cr.anchoredPosition = new Vector2(0, 20);
                    var b = slot.gameObject.AddComponent<Button>();
                    b.transition = Selectable.Transition.None;
                    bool right = si == want;
                    string nm = SizeName(si);
                    b.onClick.AddListener(() => Answer(right, slot, nm));
                    A11y.Label(b, Names.Cap(nm));
                    cr.localScale = Vector3.zero;
                    Tween.Pop(cr, 0.1f + i * 0.08f, 0.45f);
                }
                return;
            }

            var sprite = CountSprite(Random.Range(0, 4));
            for (int i = 0; i < n; i++)
            {
                int si = order[i];
                var slot = UIKit.Node("Option", _area);
                var (pos, size) = OptionSlot(i, n);
                slot.Place(new Vector2(0.5f, 0.5f), pos, size);
                slot.gameObject.AddComponent<Image>().color = Color.clear;
                var img = UIKit.Box(slot, "Item", color, sprite);
                img.type = Image.Type.Simple;
                img.raycastTarget = false;
                float s = (Mathf.Min(size.x, size.y) - 40) * scales[si];
                img.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(s, s));
                var b = slot.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                bool right = si == want;
                string nm = SizeName(si);
                b.onClick.AddListener(() => Answer(right, slot, nm));
                A11y.Label(b, Names.Cap(nm));
                slot.localScale = Vector3.zero;
                Tween.Pop(slot, 0.1f + i * 0.08f, 0.45f);
            }
        }

        // ================= Resimler 2: sınıflama =================

        static readonly (string cat, string[] keys)[] Categories =
        {
            ("sea", new[] { "dolphin", "whale", "octopus", "crab", "seahorse", "starfish", "jellyfish", "fish" }),
            ("farm", new[] { "cow", "hen", "sheep", "chick", "pony" }),
            ("fruit", new[] { "strawberry", "watermelon", "apple", "pineapple" }),
            ("vehicle", new[] { "car", "bus", "train", "airplane", "ship", "rocket", "tractor" }),
            ("bug", new[] { "bee", "ladybug", "butterfly", "cricket" }),
            ("flower", new[] { "tulip", "rose", "sunflower", "lily" }),   // "çiçek" resmi sorulmaz: "Çiçek bir çiçek" olmasın
        };
        // iki gruba da girebilecekler: çeldirici olarak kullanılmaz (kafa karıştırmasın)
        static readonly HashSet<string> Ambiguous = new()
        {
            "turtle", "penguin", "seal", "duck", "frog", "snail", "alligator", "hippo", "polarbear", "acorn", "vegbasket",
            "garden", "mushroom", "beach", "reef", "zenfish", "flowergarden", "scarecrow", "farm", "flower", "flowers",
        };

        static int CategoryOf(string key)
        {
            for (int c = 0; c < Categories.Length; c++)
                if (System.Array.IndexOf(Categories[c].keys, key) >= 0) return c;
            return -1;
        }

        /// <summary>"Hangisi denizde yaşar?" Uygun resim yoksa false (yerine normal tur oynanır).</summary>
        bool ClassifyRound()
        {
            int n = SaveSystem.OptionCount;
            var cats = new List<int>();
            for (int c = 0; c < Categories.Length; c++) cats.Add(c);
            Shuffle(cats);
            foreach (int c in cats)
            {
                var inside = new List<int>();
                var outside = new List<int>();
                for (int i = 0; i < _animals.Count; i++)
                {
                    var k = _animals[i].key;
                    int ck = CategoryOf(k);
                    if (ck == c) inside.Add(i);
                    else if (!Ambiguous.Contains(k) && ck != c) outside.Add(i);
                }
                if (inside.Count == 0 || outside.Count < n - 1) continue;
                int target = inside[Random.Range(0, inside.Count)];
                Shuffle(outside);
                // çeldiriciler başka gruplardan olsun (aynı gruptan iki doğru olmasın diye zaten dışarıdan)
                var picks = new List<int> { target };
                for (int i = 0; i < outside.Count && picks.Count < n; i++) picks.Add(outside[i]);
                Shuffle(picks);
                var cat = Categories[c].cat;
                _prompt.text = Loc.T("cls_" + cat);
                _promptVoice = _prompt.text;
                _wantKey = null;
                PictureOptions(picks, target, Loc.T("cls_yes_" + cat, Names.Cap(_animals[target].name)));
                return true;
            }
            return false;
        }

        // ================= Duygular 3: "Ne yapabilir?" =================

        // durum, Pofuduk'un yüzü, doğru davranış, iki yardımcı olmayan davranış (metin anahtarları)
        static readonly (int q, MascotMood mood)[] Actions =
        {
            (0, MascotMood.Angry), (1, MascotMood.Sad), (2, MascotMood.Scared),
            (3, MascotMood.Normal), (4, MascotMood.Shy), (5, MascotMood.Shy),
        };
        readonly List<int> _actBag = new();

        void ActionRound()
        {
            if (_actBag.Count == 0)
            {
                for (int k = 0; k < Actions.Length; k++) _actBag.Add(k);
                Shuffle(_actBag);
            }
            int a = _actBag[0];
            _actBag.RemoveAt(0);
            var (q, mood) = Actions[a];

            _prompt.text = Loc.T("act_q" + q);
            _prompt.resizeTextMinSize = 36;
            _prompt.resizeTextMaxSize = 60;
            _promptVoice = _prompt.text;

            var m = Mascot.Create(_area, 300, false);
            m.SetMood(mood);
            m.SetInteractive(false);
            var mrt = (RectTransform)m.transform;
            mrt.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 330), mrt.sizeDelta);

            // 0: doğru, 1–2: yardımcı olmayan; küçüklerde iki seçenek
            var opts = new List<int> { 0, 1, 2 };
            if (SaveSystem.AgeBand == 0) opts.RemoveAt(Random.Range(1, 3));
            Shuffle(opts);
            for (int i = 0; i < opts.Count; i++)
            {
                int o = opts[i];
                string text = Loc.T(o == 0 ? "act_r" + q : "act_w" + q + (o == 1 ? "a" : "b"));
                var bt = UIKit.Button(_area, text, Theme.Card, Theme.Text, 50, null, 0.6f);
                var rt = (RectTransform)bt.transform;
                rt.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 40 - i * 210), new Vector2(860, 170));
                var label = UIKit.ButtonText(bt);
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 30;
                label.resizeTextMaxSize = 50;
                label.rectTransform.Stretch(30, 10, 30, 10);
                bool right = o == 0;
                bt.onClick.AddListener(() =>
                {
                    if (right && !_locked) { m.SetMood(MascotMood.Happy); m.Jump(); }
                    Answer(right, rt, "", right ? Loc.T("cheer_" + Random.Range(0, 4)) + " " + Loc.T("act_good") : null, 3.4f);
                });
                rt.localScale = Vector3.zero;
                Tween.Pop(rt, 0.2f + i * 0.1f, 0.45f);
            }
        }

        // ================= Sayılar: çıkarma =================

        void SubRound()
        {
            _countItems.Clear();
            _countingTogether = false;
            int maxA = Mathf.Clamp(4 + (_countLevel - 9) / 2, 4, 9);
            int a = Random.Range(3, maxA + 1), b = Random.Range(1, a);
            _countAnswer = a - b;
            _prompt.text = Loc.T("sub_q", a, b);
            _prompt.resizeTextMinSize = 36;
            _prompt.resizeTextMaxSize = 64;
            var color = CountColor();

            var board = UIKit.Card(_area, "Board", Theme.Card.WithAlpha(0.85f), 0.6f, 4f);
            var brt = (RectTransform)board.transform.parent;
            brt.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(880, 560));
            var items = new List<RectTransform>();
            for (int i = 0; i < a; i++)
            {
                int col = i % 5, row = i / 5;
                var item = UIKit.Box(board.transform, "Balloon", color, Art.Balloon);
                item.type = Image.Type.Simple;
                item.raycastTarget = false;
                var irt = item.rectTransform;
                irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f);
                int rows = (a + 4) / 5;
                irt.anchoredPosition = new Vector2(-320 + col * 160, (rows - 1) * 100f - row * 200);
                irt.sizeDelta = new Vector2(130, 130);
                irt.localScale = Vector3.zero;
                Tween.Pop(irt, 0.1f + i * 0.06f, 0.4f);
                items.Add(irt);
            }
            // son b balon uçup gider
            for (int i = a - b; i < a; i++)
            {
                var irt = items[i];
                var cg = irt.gameObject.AddComponent<CanvasGroup>();
                var start = irt.anchoredPosition;
                Tween.To(irt, 1.1f, k =>
                {
                    irt.anchoredPosition = start + new Vector2(Mathf.Sin(k * 6f) * 20f, k * 420f);
                    cg.alpha = 1f - k;
                }, Tween.InOutSine, 1.4f + (i - (a - b)) * 0.25f);
            }
            if (SaveSystem.AgeBand >= 2) Equation(a + " − " + b + " = ?");
            NumberOptions(_countAnswer, 0, 9, 2.4f);
        }

        // ================= Sayılar: 10–20, onluk çerçeve =================

        void TeenRound()
        {
            _countItems.Clear();
            _countingTogether = false;
            int top = Mathf.Clamp(12 + (_countLevel - 11), 12, 20);
            int n;
            do n = Random.Range(10, top + 1); while (n == _countAnswer);
            _countAnswer = n;
            _prompt.text = Loc.T("teen_q");
            var color = CountColor();

            var board = UIKit.Card(_area, "Board", Theme.Card.WithAlpha(0.85f), 0.6f, 4f);
            var brt = (RectTransform)board.transform.parent;
            brt.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(900, 520));
            for (int f = 0; f < 2; f++)
            {
                int filled = f == 0 ? 10 : n - 10;
                var frame = UIKit.Box(board.transform, "Frame", Theme.Text.WithAlpha(0.08f), null, 0.6f);
                frame.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2((f == 0 ? -1 : 1) * 218, 40), new Vector2(410, 172));
                for (int c = 0; c < 10; c++)
                {
                    var cell = UIKit.Box(frame.transform, "Cell", Color.white, null, 2f);
                    cell.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(-160 + (c % 5) * 80, 40 - (c / 5) * 80), new Vector2(72, 72));
                    if (c >= filled) continue;
                    var dot = UIKit.Box(cell.transform, "Dot", color, UIKit.Circle);
                    dot.type = Image.Type.Simple;
                    dot.rectTransform.Stretch(8, 8, 8, 8);
                    dot.rectTransform.localScale = Vector3.zero;
                    Tween.Pop(dot.rectTransform, 0.1f + f * 0.6f + c * 0.04f, 0.35f);
                }
                if (f == 0)
                {
                    // "10" yazısı: dolu çerçeve bir onluk demek (onluk–birlik fikri)
                    var ten = UIKit.Label(board.transform, "10", 64, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                    ten.raycastTarget = false;
                    ten.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(-218, -120), new Vector2(200, 90));
                }
            }
            if (SaveSystem.AgeBand >= 2) Equation("10 + " + (n - 10) + " = ?");
            NumberOptions(n, 10, 20, 0.9f);
        }

        // ================= Harfler =================

        /// <summary>Seçili dilin büyük harfleri.</summary>
        static string Alphabet => Loc.Lang switch
        {
            "tr" => "ABCÇDEFGĞHIİJKLMNOÖPRSŞTUÜVYZ",
            "ru" => "АБВГДЕЖЗИКЛМНОПРСТУФХЦЧШЭЮЯ",
            "ar" => "ابتثجحخدذرزسشصضطظعغفقكلمنهوي",
            "hi" => "अआइईउएओकखगघचछजझटडतथदधनपफबभमयरलवशसह",
            _ => "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
        };

        // şekli birbirine benzeyen harfler: küçüklerde aynı turda gösterilmez
        static readonly string[] LetterPairs =
        {
            "BD", "EF", "MN", "OQ", "CG", "PR", "IL", "UV", "VY", "CÇ", "GĞ", "Iİ", "OÖ", "SŞ", "UÜ", "KX", "PB", "MW",
            "ШЩ", "ЕЁ", "ИЙ", "ЬЪ", "بت", "تث", "جح", "حخ", "دذ", "رز", "سش", "صض", "طظ", "عغ",
        };

        static bool LetterConfusable(string ab, int a, int b)
        {
            char x = ab[a], y = ab[b];
            foreach (var p in LetterPairs)
                if ((p[0] == x && p[1] == y) || (p[0] == y && p[1] == x)) return true;
            return false;
        }

        void LetterOptions(int target, string ab, float y, float delay)
        {
            var picks = PickOptions(target, ab.Length, SaveSystem.OptionCount, (a, b) => LetterConfusable(ab, a, b));
            int n = picks.Count;
            float gap = n >= 4 ? 236 : 270;
            for (int i = 0; i < n; i++)
            {
                int idx = picks[i];
                var bt = UIKit.Button(_area, ab[idx].ToString(), Theme.Card, Theme.Text, 130, null, 0.5f);
                var rt = (RectTransform)bt.transform;
                rt.Place(new Vector2(0.5f, 0.5f), new Vector2((i - (n - 1) * 0.5f) * gap, y), new Vector2(gap - 30, gap - 30));
                UIKit.ButtonText(bt).fontStyle = FontStyle.Bold;
                bool right = idx == target;
                string key = ab[idx].ToString();
                bt.onClick.AddListener(() => Answer(right, rt, "", null, 2.2f, key));
                rt.localScale = Vector3.zero;
                Tween.Pop(rt, delay + i * 0.08f, 0.45f);
            }
        }

        /// <summary>"Aynı harfi bul!" — okuma gerektirmez, harf şekillerini tanıma.</summary>
        void LetterMatchRound()
        {
            string ab = Alphabet;
            int target;
            do target = Random.Range(0, ab.Length); while (target == _lastTarget && ab.Length > 1);
            _lastTarget = target;
            _wantKey = ab[target].ToString();
            _prompt.text = Loc.T("letter_same");

            var card = UIKit.Card(_area, "Big", Color.white, 0.6f, 6f);
            var crt = (RectTransform)card.transform.parent;
            crt.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 230), new Vector2(380, 380));
            BigText(card.transform, ab[target].ToString(), 260);
            crt.localScale = Vector3.zero;
            Tween.Pop(crt, 0.05f, 0.45f);
            LetterOptions(target, ab, -340, 0.25f);
        }

        /// <summary>"Hangi harfle başlıyor?" — resmin adının ilk harfi.</summary>
        void FirstLetterRound()
        {
            string ab = Alphabet;
            var pool = new List<int>();
            for (int i = 0; i < _animals.Count; i++)
            {
                var nm = Names.Cap(_animals[i].name);
                if (!string.IsNullOrEmpty(nm) && ab.IndexOf(nm[0]) >= 0 && !nm.Contains(" ")) pool.Add(i);
            }
            if (pool.Count == 0) { LetterMatchRound(); return; }
            int pi;
            do pi = pool[Random.Range(0, pool.Count)]; while (pi == _lastTarget && pool.Count > 1);
            _lastTarget = pi;
            var e = _animals[pi];
            string word = Names.Cap(e.name);
            int target = ab.IndexOf(word[0]);
            _wantKey = ab[target].ToString();
            _prompt.text = Loc.T("letter_first");

            var card = UIKit.Card(_area, "Pic", Color.white, 0.5f, 6f);
            var crt = (RectTransform)card.transform.parent;
            crt.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(460, 460));
            var tex = SaveSystem.LoadThumb(e.g + "_" + e.id) ?? Level.LoadThumb(e.g, e.id);
            if (tex != null)
            {
                _textures.Add(tex);
                var raw = UIKit.Node("Photo", card.transform).Stretch(24, 24, 24, 24).gameObject.AddComponent<RawImage>();
                raw.texture = tex;
                raw.raycastTarget = false;
                var fit = raw.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fit.aspectRatio = (float)tex.width / tex.height;
            }
            crt.localScale = Vector3.zero;
            Tween.Pop(crt, 0.05f, 0.45f);

            // doğru bilince kelime ilk harfi renkli yazılır: "Aslan"
            string reveal = Loc.IsArabic ? word : "<color=#E8697F>" + word.Substring(0, 1) + "</color>" + word.Substring(1);
            var picks = PickOptions(target, ab.Length, SaveSystem.OptionCount, (a, b) => LetterConfusable(ab, a, b));
            int n = picks.Count;
            float gap = n >= 4 ? 236 : 270;
            for (int i = 0; i < n; i++)
            {
                int idx = picks[i];
                var bt = UIKit.Button(_area, ab[idx].ToString(), Theme.Card, Theme.Text, 130, null, 0.5f);
                var rt = (RectTransform)bt.transform;
                rt.Place(new Vector2(0.5f, 0.5f), new Vector2((i - (n - 1) * 0.5f) * gap, -380), new Vector2(gap - 30, gap - 30));
                UIKit.ButtonText(bt).fontStyle = FontStyle.Bold;
                bool right = idx == target;
                string key = ab[idx].ToString();
                bt.onClick.AddListener(() => Answer(right, rt, "", right ? reveal : null, 2.6f, key));
                rt.localScale = Vector3.zero;
                Tween.Pop(rt, 0.25f + i * 0.08f, 0.45f);
            }
        }

        // ================= Örüntü: "Sırada ne var?" =================

        static readonly string[] PatternShapes = { "circle", "square", "triangle", "star", "heart" };

        void PatternRound()
        {
            string[] types = SaveSystem.AgeBand switch
            {
                0 => new[] { "AB" },
                1 => new[] { "AB", "AAB", "ABB" },
                _ => new[] { "ABC", "AABB", "AAB", "ABB" },
            };
            string type = types[Random.Range(0, types.Length)];
            int k = type.Contains("C") ? 3 : 2;

            // birimler: küçüklerde yalnızca renk ya da yalnızca şekil değişir; büyüklerde ikisi birden
            int mode = SaveSystem.AgeBand == 0 ? Random.Range(0, 2) : Random.Range(0, 3);
            var colorIdx = new List<int> { 0, 1, 2, 3, 4, 5, 6 };
            var shapeIdx = new List<int> { 0, 1, 2, 3, 4 };
            Shuffle(colorIdx);
            Shuffle(shapeIdx);
            // A, B, C + seçenekler için birkaç fazladan birim
            // ci: temel renk sırası (renk körü desteğinde işaret için)
            var units = new List<(Sprite s, Color c, int ci)>();
            var unitNames = new List<string>();
            for (int u = 0; u < k + 2; u++)
            {
                int ci = mode == 1 ? colorIdx[0] : colorIdx[u % colorIdx.Count];
                string shKey = PatternShapes[mode == 0 ? shapeIdx[0] : shapeIdx[u % shapeIdx.Count]];
                units.Add((Art.Shape(shKey), Names.Basic[ci].color, ci));
                unitNames.Add(Names.Cap(Names.Shape(shKey).name) + ", " + Names.Basic[ci].name);
            }
            // yalnızca rengin değiştiği örüntüde renk körü çocuk ayırt edebilsin: her birimin altında renk işareti
            bool colorMatters = mode != 1;

            int len = Mathf.Min(7, type.Length * 2 + Random.Range(0, type.Length));
            int answer = type[len % type.Length] - 'A';
            _prompt.text = Loc.T("pattern_q");

            var board = UIKit.Card(_area, "Board", Theme.Card.WithAlpha(0.85f), 0.6f, 4f);
            var brt = (RectTransform)board.transform.parent;
            brt.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 160), new Vector2(1000, 260));
            float step = 118f, x0 = -(len) * step * 0.5f;
            for (int i = 0; i < len; i++)
            {
                var (s, c, ci) = units[type[i % type.Length] - 'A'];
                var img = UIKit.Box(board.transform, "Item", c, s);
                img.type = Image.Type.Simple;
                img.raycastTarget = false;
                img.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(x0 + i * step, 0), new Vector2(98, 98));
                if (colorMatters) ColorMarks.AddBasic(img.rectTransform, ci, 60, -0.3f);
                img.rectTransform.localScale = Vector3.zero;
                Tween.Pop(img.rectTransform, 0.1f + i * 0.12f, 0.35f);
            }
            // boş yer: soru işaretli halka
            var slot = UIKit.Box(board.transform, "Slot", Theme.Text.WithAlpha(0.35f), UIKit.Ring);
            slot.type = Image.Type.Simple;
            slot.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(x0 + len * step, 0), new Vector2(104, 104));
            var qm = BigText(slot.transform, "?", 64);
            qm.color = Theme.Text.WithAlpha(0.6f);
            slot.gameObject.AddComponent<Floaty>().Init(5f, 1.2f);

            // seçenekler: örüntüdeki birimler + gerekirse fazladan biri
            int n = Mathf.Max(SaveSystem.OptionCount, 2);
            var opts = new List<int>();
            for (int u = 0; u < k; u++) opts.Add(u);
            for (int u = k; opts.Count < n && u < units.Count; u++) opts.Add(u);
            while (opts.Count > n) { int r = Random.Range(0, opts.Count); if (opts[r] != answer) opts.RemoveAt(r); }
            Shuffle(opts);
            float gap = opts.Count >= 4 ? 236 : 270;
            for (int i = 0; i < opts.Count; i++)
            {
                int u = opts[i];
                var bt = UIKit.Button(_area, "", Theme.Card, Theme.Text, 10, null, 0.5f);
                var rt = (RectTransform)bt.transform;
                rt.Place(new Vector2(0.5f, 0.5f), new Vector2((i - (opts.Count - 1) * 0.5f) * gap, -300), new Vector2(gap - 30, gap - 30));
                var face = bt.transform.Find("Face");
                var img = UIKit.Box(face, "Unit", units[u].c, units[u].s);
                img.type = Image.Type.Simple;
                img.raycastTarget = false;
                img.rectTransform.Stretch(40, 40, 40, 40);
                if (colorMatters) ColorMarks.AddBasic(img.rectTransform, units[u].ci, 66, -0.2f);
                bool right = u == answer;
                bt.onClick.AddListener(() =>
                {
                    if (right && !_locked)
                    {
                        // cevap boş yere oturur
                        slot.sprite = units[u].s;
                        slot.color = units[u].c;
                        if (colorMatters) ColorMarks.AddBasic(slot.rectTransform, units[u].ci, 60, -0.3f);
                        qm.text = "";
                        Tween.Punch(brt, 0.12f, 0.4f);
                    }
                    Answer(right, rt, unitNames[u], null, 2.4f);
                });
                A11y.Label(bt, unitNames[u]);
                rt.localScale = Vector3.zero;
                Tween.Pop(rt, 0.1f + len * 0.12f + i * 0.08f, 0.45f);
            }
        }

        // ================= Hafıza: "Eşini bul!" =================

        class MemCard { public RectTransform Rt; public Image Back, Front; public int Kind; public bool Open, Done; }
        readonly List<MemCard> _mem = new();
        MemCard _memFirst;
        bool _memBusy;

        void MemoryRound()
        {
            _mem.Clear();
            _memFirst = null;
            _memBusy = false;
            int pairs = SaveSystem.AgeBand == 0 ? 3 : SaveSystem.AgeBand == 1 ? 4 : 6;
            int cols = pairs == 3 ? 3 : 4, rows = pairs * 2 / cols;
            _prompt.text = Loc.T("memory_q");

            var sprites = new List<Sprite> { Art.Heart, Art.Star5, Art.Flower, Art.Balloon, Art.Shape("circle"), Art.Shape("triangle"), Art.Shape("square") };
            // çoğu turda gerçek nesneler: aynı kategoriden (meyveler, hayvanlar, taşıtlar…) farklı resimler
            var deck = MemoryDeck(pairs);
            var colors = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };
            var sOrder = new List<int> { 0, 1, 2, 3, 4, 5, 6 };
            Shuffle(sOrder);
            Shuffle(colors);
            var kinds = new List<int>();
            for (int p = 0; p < pairs; p++) { kinds.Add(p); kinds.Add(p); }
            Shuffle(kinds);

            float cw = cols == 3 ? 280 : 220, ch = rows == 3 ? 270 : 320, gx = 20, gy = 26;
            float w = cols * cw + (cols - 1) * gx, h = rows * ch + (rows - 1) * gy;
            for (int i = 0; i < kinds.Count; i++)
            {
                int kind = kinds[i];
                int c = i % cols, r = i / cols;
                var holder = UIKit.Node("Card", _area);
                holder.Place(new Vector2(0.5f, 0.5f), new Vector2(-w * 0.5f + cw * 0.5f + c * (cw + gx), -80 + h * 0.5f - ch * 0.5f - r * (ch + gy)), new Vector2(cw, ch));
                var front = UIKit.Card(holder, "Front", Color.white, 0.5f, 4f);
                ((RectTransform)front.transform.parent).Stretch();
                var icon = UIKit.Box(front.transform, "Icon", deck != null ? Color.white : Names.Basic[colors[kind]].color,
                    deck != null ? Emoji.Get(deck[kind]) : sprites[sOrder[kind]]);
                icon.type = Image.Type.Simple;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                float pad = deck != null ? 0.12f : 0.18f;
                icon.rectTransform.Stretch(cw * pad, ch * pad, cw * pad, ch * pad);
                if (deck == null) ColorMarks.AddBasic(front.rectTransform, colors[kind], 54, 0.14f);
                var back = UIKit.Card(holder, "Back", Theme.Lavender, 0.5f, 4f);
                ((RectTransform)back.transform.parent).Stretch();
                var star = UIKit.Box(back.transform, "Star", Color.white.WithAlpha(0.85f), Art.Star5);
                star.type = Image.Type.Simple;
                star.raycastTarget = false;
                star.rectTransform.Stretch(cw * 0.3f, ch * 0.32f, cw * 0.3f, ch * 0.32f);
                back.raycastTarget = true;
                front.transform.parent.gameObject.SetActive(false);
                var mc = new MemCard { Rt = holder, Back = back, Front = front, Kind = kind };
                _mem.Add(mc);
                var b = holder.gameObject.AddComponent<Button>();
                b.targetGraphic = back;
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => MemoryTap(mc));
                int cardNo = i + 1;
                string colorName = deck != null ? null : Names.Basic[colors[kind]].name;
                A11y.Label(b, () => (mc.Open || mc.Done) && colorName != null ? Loc.T("a11y_card", cardNo) + ", " + colorName : Loc.T("a11y_card", cardNo));
                holder.localScale = Vector3.zero;
                Tween.Pop(holder, 0.05f + i * 0.05f, 0.4f);
            }
        }

        void Flip(MemCard mc, bool open)
        {
            mc.Open = open;
            Sfx.Tap();
            Tween.To(mc.Rt, 0.12f, k => mc.Rt.localScale = new Vector3(1f - k, 1f, 1f), null, 0f, () =>
            {
                if (mc.Rt == null) return;
                mc.Front.transform.parent.gameObject.SetActive(open);
                mc.Back.transform.parent.gameObject.SetActive(!open);
                Tween.To(mc.Rt, 0.12f, k => mc.Rt.localScale = new Vector3(k, 1f, 1f));
            });
        }

        void MemoryTap(MemCard mc)
        {
            if (_memBusy || _locked || mc.Open || mc.Done) return;
            Flip(mc, true);
            if (_memFirst == null) { _memFirst = mc; return; }
            var first = _memFirst;
            _memFirst = null;
            _memBusy = true;
            bool match = first.Kind == mc.Kind;   // eşleşmeyen çift keşiftir, yanlış sayılmaz
            Tween.Delay(match ? 0.35f : 1.0f, () =>
            {
                if (this == null || mc.Rt == null) return;
                _memBusy = false;
                if (match)
                {
                    first.Done = mc.Done = true;
                    Sfx.Star();
                    Tween.Punch(first.Rt, 0.15f, 0.4f);
                    Tween.Punch(mc.Rt, 0.15f, 0.4f);
                    if (_mem.TrueForAll(x => x.Done))
                    {
                        Stats.Done("memory");
                        Celebrate(_prompt.rectTransform);
                        _prompt.text = Loc.T("memory_done");
                        Tween.Delay(2.6f, () => { if (this != null && _mode == Mode.Memory) NextRound(); });
                    }
                }
                else
                {
                    Flip(first, false);
                    Flip(mc, false);
                }
            });
        }
    }
}
