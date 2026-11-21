using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Çizgi takibi (yazıya hazırlık): noktalı rakamın üzerinden parmakla geçilir.
    /// • Her çizginin başında yeşil bir nokta ve sıra numarası vardır (yazım yönü ve sırası).
    /// • Yolun ~%88'i parmakla geçilince tamamlanır. Yoldan taşmak ceza değildir.
    /// Rakam yolları 0..1 kutusunda (y yukarı) çizgiler olarak tanımlıdır.
    /// </summary>
    public class TraceBoard : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public event Action Completed;

        const float SampleStep = 30f, HitRadius = 58f, InkStep = 9f, InkSize = 52f;
        const int MaxInk = 1600;

        RectTransform _rt, _inkRoot;
        readonly List<Vector2> _samples = new();
        readonly List<Image> _guides = new();
        bool[] _hit;
        int _hitCount, _ink;
        Vector2? _last;
        bool _done;
        Color _inkColor;

        /// <summary>Kartın içine kurar. size: çizim alanı (kanvas birimi).</summary>
        public static TraceBoard Create(Transform parent, Vector2 size, int digit, Color ink)
        {
            var rt = UIKit.Node("Trace", parent);
            rt.sizeDelta = size;
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var b = rt.gameObject.AddComponent<TraceBoard>();
            b.Build(Digit(digit), size, ink);
            return b;
        }

        void Build(List<List<Vector2>> strokes, Vector2 size, Color ink)
        {
            _rt = (RectTransform)transform;
            _inkColor = ink;
            Vector2 Map(Vector2 u) => new((u.x - 0.5f) * size.x * 0.78f, (u.y - 0.5f) * size.y * 0.84f);

            var road = UIKit.Node("Road", _rt);
            var dots = UIKit.Node("Dots", _rt);
            _inkRoot = UIKit.Node("Ink", _rt);
            var marks = UIKit.Node("Marks", _rt);

            for (int si = 0; si < strokes.Count; si++)
            {
                var pts = strokes[si].ConvertAll(Map);
                // yol üzerinde eşit aralıklı noktalar
                var even = Resample(pts, SampleStep);
                foreach (var p in even)
                {
                    _samples.Add(p);
                    var r = Dot(road, p, 86f, new Color(0.94f, 0.92f, 0.95f));   // opak: üst üste binince koyulaşmasın
                    r.raycastTarget = false;
                }
                var guide = Resample(pts, 40f);
                foreach (var p in guide) _guides.Add(Dot(dots, p, 24f, Theme.Text.WithAlpha(0.45f)));

                // başlangıç: yeşil nokta + sıra numarası
                var start = Dot(marks, pts[0], 66f, Theme.Mint);
                var n = UIKit.Label(start.transform, (si + 1).ToString(), 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                n.rectTransform.Stretch();
                n.raycastTarget = false;
                start.gameObject.AddComponent<Floaty>().Init(4f, 1.2f);
                // yön oku yerine: ilk birkaç noktayı biraz daha belirgin yap
                for (int k = 1; k < Mathf.Min(4, guide.Count); k++)
                    _guides[_guides.Count - guide.Count + k].color = Theme.Mint;
            }
            _hit = new bool[_samples.Count];
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
            if (!_done && Progress >= 0.88f) Finish();
        }

        public float Progress => _samples.Count == 0 ? 0 : _hitCount / (float)_samples.Count;

        void Stamp(Vector2 p)
        {
            if (_ink < MaxInk)
            {
                Dot(_inkRoot, p, InkSize, _inkColor);
                _ink++;
            }
            for (int i = 0; i < _samples.Count; i++)
                if (!_hit[i] && (_samples[i] - p).sqrMagnitude < HitRadius * HitRadius)
                {
                    _hit[i] = true;
                    _hitCount++;
                }
            if (Progress >= 0.97f) Finish();
        }

        void Finish()
        {
            if (_done) return;
            _done = true;
            // noktalar rengine boyanır, rakam "yazılmış" olur
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
            _hitCount = 0;
            for (int i = 0; i < _hit.Length; i++) _hit[i] = false;
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

        // ---------------- Rakam yolları ----------------

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
    }
}
