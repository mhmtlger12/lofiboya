using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Çizgi takibi (yazıya hazırlık): noktalı rakamın ya da harfin üzerinden parmakla geçilir.
    /// • Yazım SIRASI ve YÖNÜ önemlidir: yalnızca sıradaki noktalar sayılır. Ters yönde ya da başka
    ///   çizgiden başlamak ilerletmez; parlayan halka "buradan devam et" der.
    /// • Her çizginin başında numaralı yeşil nokta vardır; sıradaki çizgininki parlak, diğerleri soluk.
    /// • Yoldan taşmak ceza değildir (mürekkep her yere çıkar), ama ilerleme saymaz.
    /// Yollar 0..1 kutusunda (y yukarı) çizgiler olarak tanımlıdır.
    /// </summary>
    public class TraceBoard : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public event Action Completed;

        const float SampleStep = 30f, InkStep = 9f, InkSize = 52f;
        const int MaxInk = 1600, LookAhead = 3;
        static readonly Color RoadColor = new(0.94f, 0.92f, 0.95f);

        RectTransform _rt, _inkRoot;
        readonly List<Vector2> _samples = new();
        readonly List<Image> _road = new();
        readonly List<Image> _guides = new();
        readonly List<Image> _starts = new();
        readonly List<int> _strokeEnd = new();   // her çizginin son örneğinin (dahil değil) sırası
        Image _cursor;
        int _next, _ink;
        Vector2? _last;
        bool _done;
        Color _inkColor;
        float _hitRadius;

        /// <summary>Kartın içine rakam tahtası kurar. size: çizim alanı (kanvas birimi).</summary>
        public static TraceBoard Create(Transform parent, Vector2 size, int digit, Color ink) => Make(parent, size, Digit(digit), ink);

        /// <summary>Harf tahtası (yalnızca Latin alfabesi olan dillerde).</summary>
        public static TraceBoard CreateLetter(Transform parent, Vector2 size, char letter, Color ink) => Make(parent, size, Letter(letter), ink);

        static TraceBoard Make(Transform parent, Vector2 size, List<List<Vector2>> strokes, Color ink)
        {
            var rt = UIKit.Node("Trace", parent);
            rt.sizeDelta = size;
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var b = rt.gameObject.AddComponent<TraceBoard>();
            b.Build(strokes, size, ink);
            return b;
        }

        void Build(List<List<Vector2>> strokes, Vector2 size, Color ink)
        {
            _rt = (RectTransform)transform;
            _inkColor = ink;
            // küçük parmaklara (3–4 yaş) daha geniş pay
            _hitRadius = SaveSystem.AgeBand == 0 ? 72f : 58f;
            Vector2 Map(Vector2 u) => new((u.x - 0.5f) * size.x * 0.78f, (u.y - 0.5f) * size.y * 0.84f);

            var road = UIKit.Node("Road", _rt);
            var dots = UIKit.Node("Dots", _rt);
            _inkRoot = UIKit.Node("Ink", _rt);
            var marks = UIKit.Node("Marks", _rt);

            for (int si = 0; si < strokes.Count; si++)
            {
                var pts = strokes[si].ConvertAll(Map);
                var even = Resample(pts, SampleStep);
                foreach (var p in even)
                {
                    _samples.Add(p);
                    _road.Add(Dot(road, p, 86f, RoadColor));   // opak: üst üste binince koyulaşmasın
                }
                _strokeEnd.Add(_samples.Count);
                var guide = Resample(pts, 40f);
                foreach (var p in guide) _guides.Add(Dot(dots, p, 24f, Theme.Text.WithAlpha(0.45f)));
                // yazım yönü: ilk birkaç nokta yeşil
                for (int k = 1; k < Mathf.Min(4, guide.Count); k++)
                    _guides[_guides.Count - guide.Count + k].color = Theme.Mint;

                // başlangıç: yeşil nokta + sıra numarası
                var start = Dot(marks, pts[0], 66f, Theme.Mint);
                var n = UIKit.Label(start.transform, (si + 1).ToString(), 40, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                n.rectTransform.Stretch();
                n.raycastTarget = false;
                _starts.Add(start);
            }
            // aynı noktadan başlayan çizgilerde (A, M...) önceki çizginin numarası üstte görünsün
            for (int s2 = _starts.Count - 1; s2 >= 0; s2--) _starts[s2].transform.SetAsLastSibling();
            // "buradan devam et" halkası
            _cursor = UIKit.Box(marks, "Cursor", Theme.AccentButton, UIKit.Ring);
            _cursor.type = Image.Type.Simple;
            _cursor.raycastTarget = false;
            _cursor.rectTransform.anchorMin = _cursor.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _cursor.rectTransform.sizeDelta = new Vector2(96, 96);
            RefreshMarks();
        }

        int StrokeOf(int sample)
        {
            for (int s = 0; s < _strokeEnd.Count; s++) if (sample < _strokeEnd[s]) return s;
            return _strokeEnd.Count - 1;
        }

        void RefreshMarks()
        {
            int cur = _done ? -1 : StrokeOf(_next);
            for (int s = 0; s < _starts.Count; s++)
            {
                bool started = s < cur || (s == cur && _next > (s == 0 ? 0 : _strokeEnd[s - 1]));
                var c = Theme.Mint;
                _starts[s].color = s == cur && !started ? c : c.WithAlpha(s < cur || started ? 0f : 0.35f);
                _starts[s].transform.GetChild(0).gameObject.SetActive(_starts[s].color.a > 0.01f);
            }
            _cursor.enabled = !_done && _next < _samples.Count;
            if (_cursor.enabled) _cursor.rectTransform.anchoredPosition = _samples[_next];
        }

        void Update()
        {
            if (_cursor != null && _cursor.enabled && !SaveSystem.CalmMode)
                _cursor.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.08f);
        }

        static Image Dot(Transform parent, Vector2 pos, float size, Color c)
        {
            var img = UIKit.Box(parent, "Dot", c, UIKit.Circle);
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(size, size);
            return img;
        }

        static List<Vector2> Resample(List<Vector2> pts, float step)
        {
            var res = new List<Vector2> { pts[0] };
            float carry = 0;
            for (int i = 1; i < pts.Count; i++)
            {
                Vector2 a = pts[i - 1], b = pts[i];
                float len = Vector2.Distance(a, b), t = step - carry;
                while (t <= len)
                {
                    res.Add(Vector2.Lerp(a, b, t / len));
                    t += step;
                }
                carry = len - (t - step);
            }
            if (Vector2.Distance(res[^1], pts[^1]) > step * 0.4f) res.Add(pts[^1]);
            return res;
        }

        // ---------------- Parmak ----------------

        Vector2? Local(PointerEventData e) =>
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rt, e.position, e.pressEventCamera, out var p) ? p : null;

        public void OnPointerDown(PointerEventData e)
        {
            if (_done) return;
            _last = Local(e);
            if (_last.HasValue) Stamp(_last.Value);
        }

        public void OnDrag(PointerEventData e)
        {
            if (_done || !_last.HasValue) return;
            var p = Local(e);
            if (!p.HasValue) return;
            Vector2 a = _last.Value, b = p.Value;
            float len = Vector2.Distance(a, b);
            int n = Mathf.Max(1, Mathf.CeilToInt(len / InkStep));
            for (int i = 1; i <= n; i++) Stamp(Vector2.Lerp(a, b, i / (float)n));
            _last = b;
        }

        public void OnPointerUp(PointerEventData e)
        {
            _last = null;
            // son bir iki nokta kaçtıysa yine de tamam say
            if (!_done && _next >= _samples.Count - 2) Finish();
        }

        public float Progress => _samples.Count == 0 ? 0 : _next / (float)_samples.Count;

        void Stamp(Vector2 p)
        {
            if (_ink < MaxInk)
            {
                Dot(_inkRoot, p, InkSize, _inkColor);
                _ink++;
            }
            // yalnızca sıradaki birkaç nokta sayılır: doğru sıra ve yön
            int end = Mathf.Min(_samples.Count, _next + LookAhead, _strokeEnd[StrokeOf(_next)]);
            int hit = -1;
            for (int i = _next; i < end; i++)
                if ((_samples[i] - p).sqrMagnitude < _hitRadius * _hitRadius) hit = i;
            if (hit < 0) return;
            for (int i = _next; i <= hit; i++) _road[i].color = Color.Lerp(RoadColor, _inkColor, 0.3f);
            int before = StrokeOf(_next);
            _next = hit + 1;
            if (_next >= _samples.Count) { Finish(); return; }
            if (StrokeOf(_next) != before) Sfx.Pop();   // bir çizgi bitti: sıradaki çizginin numarası parlar
            RefreshMarks();
        }

        void Finish()
        {
            if (_done) return;
            _done = true;
            _next = _samples.Count;
            RefreshMarks();
            foreach (var g in _guides)
            {
                var img = g;
                Tween.To(img, 0.4f, k => img.color = Color.Lerp(Theme.Text.WithAlpha(0.45f), _inkColor, k));
            }
            Completed?.Invoke();
        }

        /// <summary>Mürekkebi siler, baştan başlar.</summary>
        public void Clear()
        {
            if (_done) return;
            for (int i = _inkRoot.childCount - 1; i >= 0; i--) Destroy(_inkRoot.GetChild(i).gameObject);
            _ink = 0;
            _next = 0;
            foreach (var r in _road) r.color = RoadColor;
            RefreshMarks();
        }

        /// <summary>Menü kartı süsü: rakamın küçük noktalı hali.</summary>
        public static void Mini(Transform parent, int digit, Vector2 size, Color c)
        {
            foreach (var stroke in Digit(digit))
            {
                var pts = stroke.ConvertAll(u => new Vector2((u.x - 0.5f) * size.x, (u.y - 0.5f) * size.y));
                foreach (var p in Resample(pts, size.y * 0.09f)) Dot(parent, p, size.y * 0.07f, c);
            }
        }

        // ---------------- Yollar ----------------

        static List<Vector2> Arc(float cx, float cy, float rx, float ry, float a0, float a1)
        {
            var l = new List<Vector2>();
            int n = Mathf.Max(8, Mathf.CeilToInt(Mathf.Abs(a1 - a0) / 8f));
            for (int i = 0; i <= n; i++)
            {
                float a = Mathf.Deg2Rad * Mathf.Lerp(a0, a1, i / (float)n);
                l.Add(new Vector2(cx + Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry));
            }
            return l;
        }

        static List<Vector2> L(params float[] xy)
        {
            var l = new List<Vector2>();
            for (int i = 0; i + 1 < xy.Length; i += 2) l.Add(new Vector2(xy[i], xy[i + 1]));
            return l;
        }

        static List<Vector2> Join(params List<Vector2>[] parts)
        {
            var l = new List<Vector2>();
            foreach (var p in parts) l.AddRange(p);
            return l;
        }

        /// <summary>Rakamın yazım çizgileri (sırasıyla). 0..1 kutusu, y yukarı.</summary>
        public static List<List<Vector2>> Digit(int d) => d switch
        {
            1 => new() { L(0.30f, 0.76f, 0.56f, 0.97f, 0.56f, 0.03f) },
            2 => new() { Join(Arc(0.5f, 0.70f, 0.28f, 0.25f, 160f, -25f), L(0.20f, 0.03f, 0.82f, 0.03f)) },
            3 => new() { Join(Arc(0.5f, 0.75f, 0.25f, 0.21f, 150f, -90f), Arc(0.5f, 0.29f, 0.29f, 0.25f, 90f, -150f)) },
            4 => new() { L(0.55f, 0.97f, 0.14f, 0.32f, 0.86f, 0.32f), L(0.66f, 0.70f, 0.66f, 0.03f) },
            5 => new() { Join(L(0.30f, 0.97f, 0.27f, 0.57f), Arc(0.48f, 0.31f, 0.30f, 0.27f, 125f, -150f)), L(0.30f, 0.97f, 0.80f, 0.97f) },
            6 => new() { Join(Arc(0.56f, 0.50f, 0.34f, 0.46f, 70f, 195f), Arc(0.50f, 0.29f, 0.28f, 0.26f, 180f, 540f)) },
            7 => new() { L(0.18f, 0.97f, 0.82f, 0.97f, 0.40f, 0.03f) },
            8 => new() { Arc(0.5f, 0.75f, 0.22f, 0.20f, 90f, 450f), Arc(0.5f, 0.29f, 0.27f, 0.25f, 90f, -270f) },
            9 => new() { Join(Arc(0.48f, 0.70f, 0.26f, 0.25f, 0f, 360f), L(0.74f, 0.70f, 0.66f, 0.03f)) },
            _ => new() { Arc(0.5f, 0.5f, 0.31f, 0.46f, 90f, 450f) },
        };

        // ---------------- Harfler (büyük harf) ----------------

        /// <summary>Yazma oyununda sırayla çıkan harfler. Latin alfabesi olmayan dillerde boş (yalnızca rakam).</summary>
        public static string Letters => Loc.Lang switch
        {
            "tr" => "ABCÇDEFGĞHIİJKLMNOÖPRSŞTUÜVYZ",
            "en" or "de" or "fr" or "es" or "it" or "pt" => "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            _ => "",
        };

        public static bool HasLetters => Letters.Length > 0;

        /// <summary>
        /// Harf çizgileri: önce harf gövdesi 0..1 "büyük harf kutusunda" tanımlanır, sonra üstte nokta/şapka,
        /// altta çengel için yer kalsın diye y = 0.12..0.85 aralığına sıkıştırılır.
        /// </summary>
        public static List<List<Vector2>> Letter(char c)
        {
            var body = Body(char.ToUpperInvariant(c) switch
            {
                'Ç' => 'C', 'Ğ' => 'G', 'İ' => 'I', 'Ö' => 'O', 'Ş' => 'S', 'Ü' => 'U', _ => c,
            });
            var res = body.ConvertAll(s => s.ConvertAll(p => new Vector2(p.x, 0.12f + p.y * 0.73f)));
            switch (c)
            {
                case 'İ': res.Add(L(0.5f, 0.95f, 0.5f, 0.94f)); break;
                case 'Ö': case 'Ü': res.Add(L(0.38f, 0.95f, 0.38f, 0.94f)); res.Add(L(0.62f, 0.95f, 0.62f, 0.94f)); break;
                case 'Ğ': res.Add(Arc(0.55f, 1.0f, 0.12f, 0.07f, 200f, 340f)); break;
                case 'Ç': case 'Ş': res.Add(Join(L(0.52f, 0.12f, 0.52f, 0.06f), Arc(0.47f, 0.05f, 0.06f, 0.04f, 30f, -150f))); break;
            }
            return res;
        }

        static List<List<Vector2>> Body(char c) => c switch
        {
            'A' => new() { L(0.5f, 1f, 0.15f, 0f), L(0.5f, 1f, 0.85f, 0f), L(0.29f, 0.38f, 0.71f, 0.38f) },
            'B' => new() { L(0.25f, 1f, 0.25f, 0f), Join(L(0.25f, 1f, 0.55f, 1f), Arc(0.55f, 0.76f, 0.22f, 0.24f, 90f, -90f), L(0.55f, 0.52f, 0.25f, 0.52f)),
                           Join(L(0.25f, 0.52f, 0.58f, 0.52f), Arc(0.58f, 0.26f, 0.25f, 0.26f, 90f, -90f), L(0.58f, 0f, 0.25f, 0f)) },
            'C' => new() { Arc(0.55f, 0.5f, 0.38f, 0.5f, 45f, 315f) },
            'D' => new() { L(0.25f, 1f, 0.25f, 0f), Join(L(0.25f, 1f, 0.45f, 1f), Arc(0.45f, 0.5f, 0.35f, 0.5f, 90f, -90f), L(0.45f, 0f, 0.25f, 0f)) },
            'E' => new() { L(0.28f, 1f, 0.28f, 0f), L(0.28f, 1f, 0.78f, 1f), L(0.28f, 0.5f, 0.7f, 0.5f), L(0.28f, 0f, 0.78f, 0f) },
            'F' => new() { L(0.28f, 1f, 0.28f, 0f), L(0.28f, 1f, 0.78f, 1f), L(0.28f, 0.5f, 0.7f, 0.5f) },
            'G' => new() { Join(Arc(0.55f, 0.5f, 0.38f, 0.5f, 45f, 360f), L(0.93f, 0.5f, 0.6f, 0.5f)) },
            'H' => new() { L(0.22f, 1f, 0.22f, 0f), L(0.78f, 1f, 0.78f, 0f), L(0.22f, 0.5f, 0.78f, 0.5f) },
            'I' => new() { L(0.5f, 1f, 0.5f, 0f) },
            'J' => new() { Join(L(0.65f, 1f, 0.65f, 0.28f), Arc(0.42f, 0.28f, 0.23f, 0.28f, 0f, -180f)) },
            'K' => new() { L(0.25f, 1f, 0.25f, 0f), L(0.78f, 1f, 0.27f, 0.45f), L(0.42f, 0.6f, 0.8f, 0f) },
            'L' => new() { L(0.28f, 1f, 0.28f, 0f, 0.78f, 0f) },
            'M' => new() { L(0.15f, 0f, 0.15f, 1f, 0.5f, 0.4f, 0.85f, 1f, 0.85f, 0f) },
            'N' => new() { L(0.2f, 0f, 0.2f, 1f, 0.8f, 0f, 0.8f, 1f) },
            'O' => new() { Arc(0.5f, 0.5f, 0.36f, 0.5f, 90f, 450f) },
            'P' => new() { L(0.25f, 1f, 0.25f, 0f), Join(L(0.25f, 1f, 0.55f, 1f), Arc(0.55f, 0.76f, 0.23f, 0.24f, 90f, -90f), L(0.55f, 0.52f, 0.25f, 0.52f)) },
            'Q' => new() { Arc(0.5f, 0.5f, 0.36f, 0.5f, 90f, 450f), L(0.6f, 0.25f, 0.88f, -0.04f) },
            'R' => new() { L(0.25f, 1f, 0.25f, 0f), Join(L(0.25f, 1f, 0.55f, 1f), Arc(0.55f, 0.76f, 0.23f, 0.24f, 90f, -90f), L(0.55f, 0.52f, 0.25f, 0.52f)), L(0.5f, 0.52f, 0.8f, 0f) },
            'S' => new() { Join(Arc(0.5f, 0.76f, 0.28f, 0.24f, 30f, 270f), Arc(0.5f, 0.26f, 0.3f, 0.26f, 90f, -150f)) },
            'T' => new() { L(0.15f, 1f, 0.85f, 1f), L(0.5f, 1f, 0.5f, 0f) },
            'U' => new() { Join(L(0.22f, 1f, 0.22f, 0.32f), Arc(0.5f, 0.32f, 0.28f, 0.32f, 180f, 360f), L(0.78f, 0.32f, 0.78f, 1f)) },
            'V' => new() { L(0.15f, 1f, 0.5f, 0f, 0.85f, 1f) },
            'W' => new() { L(0.08f, 1f, 0.27f, 0f, 0.5f, 0.7f, 0.73f, 0f, 0.92f, 1f) },
            'X' => new() { L(0.18f, 1f, 0.82f, 0f), L(0.82f, 1f, 0.18f, 0f) },
            'Y' => new() { L(0.18f, 1f, 0.5f, 0.5f), L(0.82f, 1f, 0.5f, 0.5f, 0.5f, 0f) },
            'Z' => new() { L(0.18f, 1f, 0.82f, 1f, 0.18f, 0f, 0.82f, 0f) },
            _ => new() { Arc(0.5f, 0.5f, 0.36f, 0.5f, 90f, 450f) },
        };
    }
}
