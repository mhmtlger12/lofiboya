using System;
using UnityEngine;

namespace LofiBoya
{
    /// <summary>
    /// Kodla çizilen görseller: boya kalemi, araç ikonları ve kâğıt/kalem dokusu.
    /// Hepsi beyaz/gri tonlu üretilir; Image.color ile istenen renge boyanır.
    /// </summary>
    public static class Art
    {
        static Sprite _crayon, _crayonShine, _bucket, _pencil, _eraser, _note, _dot;
        static Texture2D _grain;

        public static Sprite CrayonBody => _crayon != null ? _crayon : (_crayon = MakeCrayon(false));
        public static Sprite CrayonShine => _crayonShine != null ? _crayonShine : (_crayonShine = MakeCrayon(true));
        public static Sprite Bucket => _bucket != null ? _bucket : (_bucket = Icon(BucketSdf));
        public static Sprite Pencil => _pencil != null ? _pencil : (_pencil = Icon(PencilSdf));
        public static Sprite Eraser => _eraser != null ? _eraser : (_eraser = Icon(EraserSdf));
        public static Sprite Note => _note != null ? _note : (_note = Icon(NoteSdf));
        public static Sprite Dot => _dot != null ? _dot : (_dot = Icon(p => p.magnitude - 0.8f));

        // ------------------------------------------------------------------
        // Boya kalemi (uç yukarıda). Gövde gri tonlu: renk ile çarpılınca gölgeli görünür.
        // shine=true: sadece beyaz parlama katmanı (renklendirilmez).
        // ------------------------------------------------------------------
        const int CW = 96, CH = 384;

        static Sprite MakeCrayon(bool shine)
        {
            var tex = new Texture2D(CW, CH, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[CW * CH];
            float aa = 1.2f / CW;
            for (int y = 0; y < CH; y++)
            for (int x = 0; x < CW; x++)
            {
                float u = (x + 0.5f) / CW;
                float v = (y + 0.5f) / CH;
                // v'yi u ile aynı ölçekte (en/boy oranı) ölçmek için
                var p = new Vector2(u, v * CH / CW);
                float d = CrayonSdf(p);
                float a = Mathf.Clamp01(0.5f - d / aa);
                if (a <= 0) { px[y * CW + x] = new Color32(255, 255, 255, 0); continue; }

                float nx = (u - 0.5f) / 0.38f;                     // -1..1 gövde genişliğinde
                float cyl = Mathf.Sqrt(Mathf.Clamp01(1f - nx * nx));
                float vy = v * CH / CW;                             // 0..4
                bool tip = vy > 2.80f;

                if (shine)
                {
                    // ince dikey parlama + uçta küçük ışıltı
                    float s = Mathf.Exp(-Mathf.Pow((u - 0.33f) / 0.045f, 2)) * 0.55f;
                    s += Mathf.Exp(-Mathf.Pow((u - 0.70f) / 0.03f, 2)) * 0.12f;
                    if (tip) s *= 0.8f;
                    float glint = Mathf.Exp(-((p - new Vector2(0.44f, 3.55f)).sqrMagnitude) / 0.0016f) * 0.7f;
                    float alpha = Mathf.Clamp01(s + glint) * a;
                    px[y * CW + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                    continue;
                }

                // Silindir gölgesi: ışık soldan
                float shade = 0.70f + 0.30f * cyl - 0.10f * Mathf.Max(0, nx);
                // Kâğıt sargı
                if (vy > 0.35f && vy < 2.15f)
                {
                    shade *= 0.90f;
                    float edge = Mathf.Min(Mathf.Abs(vy - 0.38f), Mathf.Abs(vy - 2.12f));
                    if (edge < 0.035f) shade *= 0.66f;
                    // ortada zikzak desen
                    float zig = 1.25f + 0.10f * Mathf.Abs((u * 7f % 2f) - 1f) * 2f - 0.1f;
                    if (Mathf.Abs(vy - zig) < 0.03f) shade *= 0.72f;
                    if (Mathf.Abs(vy - zig - 0.22f) < 0.018f || Mathf.Abs(vy - zig + 0.22f) < 0.018f) shade *= 0.82f;
                }
                // Gövde-uç omzu
                if (Mathf.Abs(vy - 2.80f) < 0.03f) shade *= 0.80f;
                // Uç: biraz koyu, yontma çizgileri
                if (tip)
                {
                    shade *= 0.90f;
                    float facet = Mathf.Abs(Mathf.Sin((u - 0.5f) * 38f));
                    shade *= 0.94f + 0.06f * facet;
                }
                // Alt kenarda hafif koyuluk
                if (vy < 0.08f) shade *= 0.85f;
                byte g = (byte)(Mathf.Clamp01(shade) * 255);
                px[y * CW + x] = new Color32(g, g, g, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, CW, CH), new Vector2(0.5f, 0f), 100);
        }

        static float CrayonSdf(Vector2 p)
        {
            // gövde: u 0.12..0.88, y 0..2.82 (alt köşeler yuvarlak)
            float body = RoundBox(p, new Vector2(0.5f, 1.41f), new Vector2(0.38f, 1.41f), 0.07f);
            // uç: konik + yuvarlak tepe
            var cone = new[] { new Vector2(0.14f, 2.78f), new Vector2(0.86f, 2.78f), new Vector2(0.60f, 3.62f), new Vector2(0.40f, 3.62f) };
            float tip = Polygon(p, cone);
            float top = (p - new Vector2(0.5f, 3.62f)).magnitude - 0.105f;
            return Mathf.Min(body, Mathf.Min(tip, top));
        }

        // ------------------------------------------------------------------
        // İkonlar: [-1,1] uzayında SDF, beyaz siluet
        // ------------------------------------------------------------------
        static Sprite Icon(Func<Vector2, float> sdf, int size = 128)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            float aa = 2.4f / size;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                float a = Mathf.Clamp01(0.5f - sdf(p) / aa);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }

        static float BucketSdf(Vector2 p)
        {
            var q = Rotate(p - new Vector2(-0.05f, 0f), 18f);
            var body = new[] { new Vector2(-0.52f, 0.28f), new Vector2(0.52f, 0.28f), new Vector2(0.40f, -0.62f), new Vector2(-0.40f, -0.62f) };
            float b = Polygon(q, body) - 0.05f;
            // ağzın içi (elips) boşluk
            float mouth = Ellipse(q - new Vector2(0, 0.28f), new Vector2(0.44f, 0.11f));
            b = Mathf.Max(b, -mouth);
            float rim = Mathf.Abs(Ellipse(q - new Vector2(0, 0.28f), new Vector2(0.55f, 0.17f))) - 0.05f;
            b = Mathf.Min(b, rim);
            // sap
            float handle = Mathf.Abs((q - new Vector2(0, 0.28f)).magnitude - 0.62f) - 0.055f;
            handle = Mathf.Max(handle, -(q.y - 0.34f));
            b = Mathf.Min(b, handle);
            // akan boya damlası
            float drip = Capsule(p, new Vector2(0.56f, 0.10f), new Vector2(0.70f, -0.38f), 0.07f);
            float drop = (p - new Vector2(0.72f, -0.52f)).magnitude - 0.15f;
            return Mathf.Min(b, Mathf.Min(drip, drop));
        }

        static float PencilSdf(Vector2 p)
        {
            var q = Rotate(p, -45f);
            float body = RoundBox(q, new Vector2(0, -0.22f), new Vector2(0.22f, 0.58f), 0.06f);
            float cap = RoundBox(q, new Vector2(0, -0.88f), new Vector2(0.22f, 0.10f), 0.08f);
            body = Mathf.Max(body, -RoundBox(q, new Vector2(0, -0.70f), new Vector2(0.3f, 0.025f), 0f));
            var tipPoly = new[] { new Vector2(-0.22f, 0.42f), new Vector2(0.22f, 0.42f), new Vector2(0.0f, 0.92f) };
            float tip = Polygon(q, tipPoly) - 0.02f;
            tip = Mathf.Max(tip, -RoundBox(q, new Vector2(0, 0.40f), new Vector2(0.3f, 0.03f), 0f));
            return Mathf.Min(Mathf.Min(body, cap), tip);
        }

        static float EraserSdf(Vector2 p)
        {
            var q = Rotate(p, -40f);
            float e = RoundBox(q, Vector2.zero, new Vector2(0.36f, 0.70f), 0.16f);
            float split = RoundBox(q, new Vector2(0, 0.08f), new Vector2(0.5f, 0.035f), 0f);
            return Mathf.Max(e, -split);
        }

        static float NoteSdf(Vector2 p)
        {
            float head1 = Ellipse(Rotate(p - new Vector2(-0.38f, -0.52f), 25f), new Vector2(0.27f, 0.20f));
            float head2 = Ellipse(Rotate(p - new Vector2(0.42f, -0.34f), 25f), new Vector2(0.27f, 0.20f));
            float stem1 = Capsule(p, new Vector2(-0.15f, -0.48f), new Vector2(-0.15f, 0.62f), 0.065f);
            float stem2 = Capsule(p, new Vector2(0.65f, -0.30f), new Vector2(0.65f, 0.80f), 0.065f);
            float beam = Capsule(p, new Vector2(-0.15f, 0.62f), new Vector2(0.65f, 0.80f), 0.11f);
            return Mathf.Min(Mathf.Min(head1, head2), Mathf.Min(Mathf.Min(stem1, stem2), beam));
        }

        // ------------------------------------------------------------------
        // Kâğıt/kalem dokusu: döşenebilir, yumuşak gürültü (fırça izi için)
        // ------------------------------------------------------------------
        public static Texture2D Grain
        {
            get
            {
                if (_grain != null) return _grain;
                const int n = 128;
                var rnd = new System.Random(5);
                var white = new float[n * n];
                for (int i = 0; i < white.Length; i++) white[i] = (float)rnd.NextDouble();
                float Sample(float[] src, int x, int y) => src[((y % n + n) % n) * n + ((x % n + n) % n)];
                // iki ölçekte bulanıklaştırıp karıştır: hem iri lekeler hem ince kâğıt dokusu
                var soft = new float[n * n];
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float s = 0;
                    for (int dy = -2; dy <= 2; dy++)
                    for (int dx = -2; dx <= 2; dx++) s += Sample(white, x + dx, y + dy);
                    soft[y * n + x] = s / 25f;
                }
                var px = new Color32[n * n];
                for (int i = 0; i < px.Length; i++)
                {
                    float v = Mathf.Clamp01((soft[i] - 0.5f) * 3.2f + 0.5f) * 0.6f + white[i] * 0.4f;
                    byte b = (byte)(v * 255);
                    px[i] = new Color32(b, b, b, 255);
                }
                _grain = new Texture2D(n, n, TextureFormat.RGBA32, true, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, name = "Grain" };
                _grain.SetPixels32(px);
                _grain.Apply(true, true);
                return _grain;
            }
        }

        // ---------------- SDF yardımcıları ----------------
        static Vector2 Rotate(Vector2 p, float deg)
        {
            float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(c * p.x - s * p.y, s * p.x + c * p.y);
        }

        static float RoundBox(Vector2 p, Vector2 c, Vector2 half, float r)
        {
            var q = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + new Vector2(r, r);
            return new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - r;
        }

        static float Capsule(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            var pa = p - a; var ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - r;
        }

        static float Ellipse(Vector2 p, Vector2 r)
        {
            // yaklaşık elips mesafesi (ikon için yeterli)
            float k = new Vector2(p.x / r.x, p.y / r.y).magnitude;
            return (k - 1f) * Mathf.Min(r.x, r.y);
        }

        static float Polygon(Vector2 p, Vector2[] v)
        {
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                var e = v[j] - v[i];
                var w = p - v[i];
                var b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }
    }
}
