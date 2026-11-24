using System;
using System.Collections;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;

namespace LofiBoya
{
    /// <summary>
    /// Hikaye kitaplarının resimleri: hepsi kodla, Pofuduk ile aynı çizim diliyle boyanır.
    /// Koyu mor kontur, yukarıdan aşağı yumuşak renk geçişi, parlama, yere düşen gölge; arkada uzaklaştıkça
    /// solan tepeler (derinlik) ve en sonda suluboya kâğıdı dokusu.
    /// Boyama arka planda (iş parçacığında) yapılır: burada Unity nesnesine dokunulmaz, yalnızca saf hesap var.
    /// Sahne dili için bkz. Books.cs.
    /// </summary>
    public static class StoryArt
    {
        // ======================= dışarıya açık =======================

        class Job { public string Scene; public int W, H; public UnityEngine.Object Owner; public Action<Texture2D> Done; }
        static readonly System.Collections.Generic.List<Job> _queue = new();
        static int _running;
        const int MaxParallel = 2;   // aynı anda en çok iki resim: düşük RAM'li telefonda bellek sıçramasın

        /// <summary>
        /// Sahneyi arka planda boyar, bitince ana iş parçacığında doku verir. İstekler sırayla (en çok ikisi aynı anda)
        /// işlenir; sırası gelmeden sahibi (ekran) kapanan istekler hiç başlamaz, biten iş de sahibi yoksa doku üretmez.
        /// </summary>
        public static void PaintAsync(string scene, int w, int h, UnityEngine.Object owner, Action<Texture2D> done)
        {
            _queue.Add(new Job { Scene = scene, W = w, H = h, Owner = owner, Done = done });
            Pump();
        }

        static void Pump()
        {
            while (_running < MaxParallel && _queue.Count > 0)
            {
                var job = _queue[0];
                _queue.RemoveAt(0);
                if (job.Owner == null) continue;   // ekran kapandı: boşuna boyama
                _running++;
#if UNITY_WEBGL && !UNITY_EDITOR
                var px = Paint(job.Scene, job.W, job.H);
                _running--;
                if (job.Owner != null) job.Done(ToTexture(px, job.W, job.H));
#else
                var task = Task.Run(() => Paint(job.Scene, job.W, job.H));
                Tween.Run(Wait(task, job));
#endif
            }
        }

        static IEnumerator Wait(Task<Color32[]> task, Job job)
        {
            while (!task.IsCompleted) yield return null;
            _running--;
            try
            {
                if (job.Owner == null) yield break;
                if (task.IsFaulted)
                {
                    Debug.LogException(task.Exception);
                    yield break;
                }
                job.Done(ToTexture(task.Result, job.W, job.H));
            }
            finally
            {
                Pump();
            }
        }

        /// <summary>Sahnedeki öğeler (ad, x, y, boy): dokunulabilir resimler için. Zemin dahil değil.</summary>
        public static System.Collections.Generic.List<(string name, float x, float y, float s)> Items(string scene)
        {
            var list = new System.Collections.Generic.List<(string, float, float, float)>();
            var parts = scene.Split(';');
            for (int i = 1; i < parts.Length; i++)
            {
                var t = parts[i].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (t.Length < 4) continue;
                list.Add((t[0], F(t[1]), F(t[2]), F(t[3])));
            }
            return list;
        }

        static Texture2D ToTexture(Color32[] px, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 2, name = "story" };
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return tex;
        }

        /// <summary>İş parçacığında güvenle çağrılabilir.</summary>
        public static Color32[] Paint(string scene, int w, int h)
        {
            var p = new P(w, h);
            var parts = scene.Split(';');
            string bg = parts[0].Trim(), arg = "";
            int colon = bg.IndexOf(':');
            if (colon >= 0) { arg = bg.Substring(colon + 1); bg = bg.Substring(0, colon); }
            var rnd = new System.Random(Hash(scene));
            Background(p, bg, arg, rnd);
            for (int i = 1; i < parts.Length; i++)
            {
                var t = parts[i].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (t.Length < 4) continue;
                float x = F(t[1]), y = F(t[2]), s = F(t[3]);
                bool flip = false;
                int mood = 0, n = -1;
                Color? c = null;
                for (int k = 4; k < t.Length; k++)
                {
                    var f = t[k];
                    if (f == "f") flip = true;
                    else if (f == "happy") mood = Happy;
                    else if (f == "sad") mood = Sad;
                    else if (f == "sleep") mood = Sleep;
                    else if (f == "wow") mood = Wow;
                    else if (f.StartsWith("n=")) n = (int)F(f.Substring(2));
                    else if (f.StartsWith("c=")) c = Col(f.Substring(2));
                }
                p.At(x * p.A, y, s, flip);
                Item(p, t[0], mood, n, c, y > 0.3f, bg);
            }
            After(p, bg, arg, rnd);
            return p.Finish();
        }

        // ======================= boya altyapısı =======================

        const int Normal = 0, Happy = 1, Sad = 2, Sleep = 3, Wow = 4;
        static readonly Color Ink = Col("5E4B6E");
        static readonly Color White = Color.white;
        static readonly Color Blush = Col("FF8FA3");
        static readonly Color ShadowCol = new Color(0.30f, 0.20f, 0.40f, 1f);
        const float LineW = 0.0042f;   // kontur kalınlığı (resim yüksekliğine oranla)

        static float F(string s) => float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);

        static int Hash(string s)
        {
            unchecked
            {
                int h = (int)2166136261;
                foreach (var ch in s) h = (h ^ ch) * 16777619;
                return h & 0x7fffffff;
            }
        }

        /// <summary>ColorUtility ana iş parçacığı ister; burada kendi çözümleyicimiz.</summary>
        static Color Col(string hex)
        {
            int v = int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return new Color(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f, 1f);
        }

        static Color Light(Color c, float k) { var r = Color.Lerp(c, White, k); r.a = c.a; return r; }
        static Color Dark(Color c, float k) { var r = Color.Lerp(c, Ink, k); r.a = c.a; return r; }
        static Color A(Color c, float a) { c.a = a; return c; }
        static float Rf(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

        /// <summary>Tuval. Koordinatlar resim yüksekliğine oranla (y yukarı). Çizimler yerel birimle yapılır: At() ile konum/boy.</summary>
        sealed class P
        {
            public readonly int W, H;
            public readonly float A;     // en/boy (genişlik birimi)
            public readonly Color[] Px;
            float _ox, _oy, _s = 1;
            bool _flip;

            public P(int w, int h)
            {
                W = w; H = h; A = (float)w / h;
                Px = new Color[w * h];
            }

            public void At(float x, float y, float s, bool flip) { _ox = x; _oy = y; _s = Mathf.Max(0.001f, s); _flip = flip; }
            public void Reset() => At(0, 0, 1, false);
            public float S => _s;

            Vector2 Local(int ix, int iy)
            {
                float lx = ((ix + 0.5f) / H - _ox) / _s;
                if (_flip) lx = -lx;
                return new Vector2(lx, ((iy + 0.5f) / H - _oy) / _s);
            }

            bool Range(Rect b, out int x0, out int x1, out int y0, out int y1)
            {
                float ax = _flip ? -b.xMax : b.xMin, bx = _flip ? -b.xMin : b.xMax;
                x0 = Mathf.Max(0, (int)((_ox + ax * _s) * H) - 2);
                x1 = Mathf.Min(W - 1, (int)((_ox + bx * _s) * H) + 2);
                y0 = Mathf.Max(0, (int)((_oy + b.yMin * _s) * H) - 2);
                y1 = Mathf.Min(H - 1, (int)((_oy + b.yMax * _s) * H) + 2);
                return x0 <= x1 && y0 <= y1;
            }

            void Blend(int i, Color c, float a)
            {
                if (a <= 0f) return;
                var d = Px[i];
                float oa = a + d.a * (1f - a);
                if (oa <= 0f) return;
                float k = a / oa;
                Px[i] = new Color(d.r + (c.r - d.r) * k, d.g + (c.g - d.g) * k, d.b + (c.b - d.b) * k, oa);
            }

            /// <summary>Kenarı yumuşatılmış dolgu (sdf &lt; 0 içi).</summary>
            public void Fill(Rect b, Func<Vector2, float> sdf, Func<Vector2, Color> col)
            {
                if (!Range(b, out int x0, out int x1, out int y0, out int y1)) return;
                float px = 1f / (H * _s);
                for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    var l = Local(x, y);
                    float a = Mathf.Clamp01(0.5f - sdf(l) / px);
                    if (a <= 0f) continue;
                    var c = col(l);
                    Blend(y * W + x, c, a * c.a);
                }
            }

            public void Solid(Rect b, Func<Vector2, float> sdf, Color c) => Fill(b, sdf, _ => c);

            /// <summary>Çok yumuşak kenarlı şekil (gölge, parıltı, yanak).</summary>
            public void Soft(Rect b, Func<Vector2, float> sdf, Color c, float blur)
            {
                if (!Range(b, out int x0, out int x1, out int y0, out int y1)) return;
                for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float d = sdf(Local(x, y));
                    float a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-blur, blur, d));
                    Blend(y * W + x, c, a * c.a);
                }
            }

            /// <summary>Konturlu, yukarıdan aşağı renk geçişli şekil.</summary>
            public void Shape(Rect b, Func<Vector2, float> sdf, Color top, Color bot, bool ink = true)
            {
                if (ink)
                {
                    float lw = LineW / _s;
                    Solid(Grow(b, lw), p => sdf(p) - lw, Ink);
                }
                float y0 = b.yMin, hh = Mathf.Max(1e-4f, b.height);
                Fill(b, sdf, p => Color.Lerp(bot, top, Mathf.Clamp01((p.y - y0) / hh)));
            }

            // ---- kısa yollar ----
            public void E(float cx, float cy, float rx, float ry, Color top, Color bot, float rot = 0, bool ink = true)
            {
                float m = Mathf.Max(rx, ry);
                Shape(new Rect(cx - m, cy - m, 2 * m, 2 * m), p => Ell(p, cx, cy, rx, ry, rot), top, bot, ink);
            }
            public void C(float cx, float cy, float r, Color top, Color bot, bool ink = true)
                => Shape(new Rect(cx - r, cy - r, 2 * r, 2 * r), p => Circ(p, cx, cy, r), top, bot, ink);
            public void Bx(float cx, float cy, float hx, float hy, float r, Color top, Color bot, float rot = 0, bool ink = true)
            {
                float m = rot != 0 ? Mathf.Max(hx, hy) * 1.42f : 0;
                var b = rot != 0 ? new Rect(cx - m, cy - m, 2 * m, 2 * m) : new Rect(cx - hx, cy - hy, 2 * hx, 2 * hy);
                Shape(b, p => Box(p, cx, cy, hx, hy, r, rot), top, bot, ink);
            }
            public void Cp(float ax, float ay, float bx, float by, float r, Color top, Color bot, bool ink = true)
                => Shape(Bounds(ax, ay, bx, by, r), p => Cap(p, ax, ay, bx, by, r), top, bot, ink);
            public void Pg(Vector2[] v, Color top, Color bot, bool ink = true)
                => Shape(Bounds(v), p => Poly(p, v), top, bot, ink);
            public void Dot(float cx, float cy, float r, Color c)
                => Solid(new Rect(cx - r, cy - r, 2 * r, 2 * r), p => Circ(p, cx, cy, r), c);
            public void Line(float ax, float ay, float bx, float by, float w, Color c)
                => Solid(Bounds(ax, ay, bx, by, w), p => Cap(p, ax, ay, bx, by, w * 0.5f), c);
            public void Arc(float cx, float cy, float r, float a0, float a1, float w, Color c)
                => Solid(new Rect(cx - r - w, cy - r - w, 2 * (r + w), 2 * (r + w)), p => ArcD(p, cx, cy, r, a0, a1) - w * 0.5f, c);
            public void SoftE(float cx, float cy, float rx, float ry, Color c, float blur)
            {
                float m = Mathf.Max(rx, ry) + blur;
                Soft(new Rect(cx - m, cy - m, 2 * m, 2 * m), p => Ell(p, cx, cy, rx, ry), c, blur);
            }
            public void Gloss(float cx, float cy, float rx, float ry, float a = 0.55f, float rot = 0)
            {
                float m = Mathf.Max(rx, ry) * 1.6f;
                Soft(new Rect(cx - m, cy - m, 2 * m, 2 * m), p => Ell(p, cx, cy, rx, ry, rot), A(White, a), Mathf.Min(rx, ry) * 0.5f);
            }
            /// <summary>Yerdeki yumuşak gölge (karakterin ayak hizasında).</summary>
            public void Ground(float rx, float cx = 0)
                => SoftE(cx, 0.005f, rx, rx * 0.16f, A(ShadowCol, 0.20f), rx * 0.18f);

            // ---- tüm tuval için doğrudan piksel işleri ----
            public void Set(int i, Color c) => Px[i] = c;
            public void Mix(int i, Color c, float a) => Blend(i, c, a);

            public Color32[] Finish()
            {
                var o = new Color32[W * H];
                float rad = 0.035f * H;   // yuvarlak köşe (piksel)
                for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    var c = Px[i];
                    // suluboya kâğıdı: iri lekeler + ince lif
                    float n = Noise(x * 0.012f, y * 0.012f) * 0.6f + Noise(x * 0.05f, y * 0.05f) * 0.3f + Hash01(x, y) * 0.1f;
                    float k = 1f + (n - 0.5f) * 0.10f;
                    // kenarlara doğru çok hafif kararma (fotoğraf baskısı gibi)
                    float vx = (x + 0.5f) / W - 0.5f, vy = (y + 0.5f) / H - 0.5f;
                    k *= 1f - Mathf.SmoothStep(0.25f, 0.75f, vx * vx * 1.6f + vy * vy * 2.2f) * 0.12f;
                    c.r *= k; c.g *= k; c.b *= k;
                    // yuvarlak köşe
                    float qx = Mathf.Max(0, Mathf.Max(rad - x - 0.5f, x + 0.5f - (W - rad)));
                    float qy = Mathf.Max(0, Mathf.Max(rad - y - 0.5f, y + 0.5f - (H - rad)));
                    float a = Mathf.Clamp01(rad + 0.5f - Mathf.Sqrt(qx * qx + qy * qy));
                    o[i] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), a);
                }
                return o;
            }
        }

        // ---------------- gürültü ----------------
        static float Hash01(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xffff) / 65535f;
            }
        }

        static float Noise(float x, float y)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            float a = Hash01(xi, yi), b = Hash01(xi + 1, yi), c = Hash01(xi, yi + 1), d = Hash01(xi + 1, yi + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        // ---------------- SDF ----------------
        static float Circ(Vector2 p, float cx, float cy, float r) => new Vector2(p.x - cx, p.y - cy).magnitude - r;

        static float Ell(Vector2 p, float cx, float cy, float rx, float ry, float rot = 0)
        {
            float x = p.x - cx, y = p.y - cy;
            if (rot != 0)
            {
                float a = rot * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
                float nx = c * x + s * y, ny = -s * x + c * y;
                x = nx; y = ny;
            }
            float k0 = Mathf.Sqrt((x / rx) * (x / rx) + (y / ry) * (y / ry));
            float k1 = Mathf.Sqrt((x / (rx * rx)) * (x / (rx * rx)) + (y / (ry * ry)) * (y / (ry * ry)));
            if (k1 < 1e-6f) return -Mathf.Min(rx, ry);
            return k0 * (k0 - 1f) / k1;
        }

        static float Box(Vector2 p, float cx, float cy, float hx, float hy, float r = 0, float rot = 0)
        {
            float x = p.x - cx, y = p.y - cy;
            if (rot != 0)
            {
                float a = rot * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
                float nx = c * x + s * y, ny = -s * x + c * y;
                x = nx; y = ny;
            }
            float qx = Mathf.Abs(x) - hx + r, qy = Mathf.Abs(y) - hy + r;
            return new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
        }

        static float Cap(Vector2 p, float ax, float ay, float bx, float by, float r)
        {
            var pa = new Vector2(p.x - ax, p.y - ay);
            var ba = new Vector2(bx - ax, by - ay);
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(1e-8f, Vector2.Dot(ba, ba)));
            return (pa - ba * h).magnitude - r;
        }

        static float Poly(Vector2 p, Vector2[] v)
        {
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                var e = v[j] - v[i];
                var w = p - v[i];
                var b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Mathf.Max(1e-8f, Vector2.Dot(e, e)));   // yinelenen köşe: 0/0 olmasın
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        static float ArcD(Vector2 p, float cx, float cy, float r, float a0, float a1)
        {
            float dx = p.x - cx, dy = p.y - cy;
            float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            while (ang < a0) ang += 360f;
            while (ang >= a0 + 360f) ang -= 360f;
            if (ang <= a1) return Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - r);
            float e0x = cx + r * Mathf.Cos(a0 * Mathf.Deg2Rad), e0y = cy + r * Mathf.Sin(a0 * Mathf.Deg2Rad);
            float e1x = cx + r * Mathf.Cos(a1 * Mathf.Deg2Rad), e1y = cy + r * Mathf.Sin(a1 * Mathf.Deg2Rad);
            return Mathf.Min(new Vector2(p.x - e0x, p.y - e0y).magnitude, new Vector2(p.x - e1x, p.y - e1y).magnitude);
        }

        static Rect Grow(Rect b, float k) => new Rect(b.xMin - k, b.yMin - k, b.width + 2 * k, b.height + 2 * k);
        static Rect Bounds(float ax, float ay, float bx, float by, float r)
            => new Rect(Mathf.Min(ax, bx) - r, Mathf.Min(ay, by) - r, Mathf.Abs(bx - ax) + 2 * r, Mathf.Abs(by - ay) + 2 * r);
        static Rect Bounds(Vector2[] v)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var q in v) { x0 = Mathf.Min(x0, q.x); y0 = Mathf.Min(y0, q.y); x1 = Mathf.Max(x1, q.x); y1 = Mathf.Max(y1, q.y); }
            return new Rect(x0, y0, x1 - x0, y1 - y0);
        }
        static Vector2 V(float x, float y) => new Vector2(x, y);
        static Rect R(float cx, float cy, float hx, float hy) => new Rect(cx - hx, cy - hy, 2 * hx, 2 * hy);

        // ======================= yüz =======================

        /// <summary>Göz: normal nokta (parlamalı), mutlu ^ , uyku ‿ , şaşkın iri nokta.</summary>
        static void Eye(P p, float x, float y, float r, int mood)
        {
            switch (mood)
            {
                case Happy:
                    p.Arc(x, y - r * 0.55f, r * 1.05f, 25, 155, r * 0.55f, Ink);
                    break;
                case Sleep:
                    p.Arc(x, y + r * 0.55f, r * 1.05f, 205, 335, r * 0.5f, Ink);
                    break;
                default:
                    float rr = mood == Wow ? r * 1.25f : r;
                    p.Dot(x, y, rr, Ink);
                    p.Dot(x - rr * 0.32f, y + rr * 0.35f, rr * 0.36f, White);
                    p.Dot(x + rr * 0.3f, y - rr * 0.3f, rr * 0.16f, A(White, 0.8f));
                    break;
            }
        }

        static void Mouth(P p, float x, float y, float w, int mood)
        {
            switch (mood)
            {
                case Happy:
                    // açık gülümseme: yarım ay, içinde dil
                    {
                        var b = R(x, y - w * 0.5f, w * 1.2f, w * 0.8f);
                        float lw = LineW * 0.9f / p.S;
                        p.Solid(Grow(b, lw), q => Mathf.Max(Circ(q, x, y + w * 0.15f, w), q.y - (y + w * 0.15f)) - lw, Ink);
                        p.Solid(b, q => Mathf.Max(Circ(q, x, y + w * 0.15f, w), q.y - (y + w * 0.15f)), Col("C9506A"));
                        p.Solid(b, q => Mathf.Max(Mathf.Max(Circ(q, x, y + w * 0.15f, w), q.y - (y + w * 0.15f)), Circ(q, x, y - w * 0.95f, w * 0.75f)), Col("FF8FA3"));
                    }
                    break;
                case Sad:
                    p.Arc(x, y - w * 0.85f, w * 0.8f, 50, 130, w * 0.32f, Ink);
                    break;
                case Wow:
                    p.E(x, y - w * 0.1f, w * 0.38f, w * 0.48f, Col("C9506A"), Col("A63E58"));
                    break;
                case Sleep:
                    p.Arc(x, y + w * 0.2f, w * 0.45f, 215, 325, w * 0.3f, Ink);
                    break;
                default:
                    p.Arc(x, y + w * 0.6f, w * 0.85f, 230, 310, w * 0.32f, Ink);
                    break;
            }
        }

        static void Cheeks(P p, float x, float y, float r)
        {
            p.SoftE(-x, y, r, r * 0.62f, A(Blush, 0.6f), r * 0.45f);
            p.SoftE(x, y, r, r * 0.62f, A(Blush, 0.6f), r * 0.45f);
        }

        static void Tear(P p, float x, float y, float r)
        {
            var tip = new[] { V(x - r * 0.85f, y + r * 0.5f), V(x, y + r * 1.9f), V(x + r * 0.85f, y + r * 0.5f) };   // lambda dışında: piksel başına dizi tahsisi olmasın
            p.Shape(R(x, y, r, r * 1.6f), q => Mathf.Min(Circ(q, x, y, r), Poly(q, tip)),
                Col("CDEBFF"), Col("8FCBF4"));
        }

        // ======================= karakterler =======================

        static void Item(P p, string name, int mood, int n, Color? c, bool air, string bg)
        {
            switch (name)
            {
                case "bunny": case "hare": case "bear": case "cat": case "dog": case "mouse":
                case "lion": case "fox": case "squirrel": case "hedgehog": case "frog":
                    Critter(p, name, mood); break;
                case "owl": Owl(p, mood); break;
                case "duck": case "gduck": case "swan": case "hen": case "crow": case "bird":
                    Bird(p, name, mood, name == "swan" || ((name == "duck" || name == "gduck") && bg == "pond")); break;
                case "ladybug": Ladybug(p, mood, n < 0 ? 7 : n); break;
                case "bee": Bee(p, mood, air); break;
                case "butterfly": Butterfly(p, mood, c ?? Col("FF8FAB")); break;
                case "ant": Ant(p, mood); break;
                case "cricket": Cricket(p, mood); break;
                case "snail": Snail(p, mood); break;
                case "fish": Fish(p, mood, c ?? Col("FF9F43")); break;
                case "turtle": Turtle(p, mood, air); break;
                case "star5": Starfish(p, mood); break;
                case "crab": Crab(p, mood); break;
                case "snowman": Snowman(p, mood, n < 0 ? 1 : n); break;
                // ---- nesneler ----
                case "sun": Sun(p, mood); break;
                case "moon": Moon(p, n < 0 ? 4 : n, mood); break;
                case "star": StarProp(p); break;
                case "cloud": Cloud(p, n == 1); break;
                case "rainbow": Rainbow(p); break;
                case "tree": Tree(p, n < 0 ? 2 : n); break;
                case "pine": Pine(p, n == 1); break;
                case "flower": Flower(p, c ?? Col("FF8FAB")); break;
                case "tuft": Tuft(p, Col("7CC66B")); break;
                case "mushroom": Mushroom(p); break;
                case "flag": Bunting(p); break;
                case "jug": Jug(p, n < 0 ? 3 : n); break;
                case "net": Net(p); break;
                case "rope": Rope(p); break;
                case "wheat": Wheat(p, n < 0 ? 3 : n); break;
                case "bread": Bread(p); break;
                case "leaf": BigLeaf(p, bg == "rain", c); break;
                case "sprout": Sprout(p, n < 0 ? 0 : n, mood); break;
                case "can": Can(p); break;
                case "kite": Kite(p, n != 0); break;
                case "hat": Hat(p, n == 1); break;
                case "branch": Branch(p); break;
                case "anthill": Anthill(p, n == 1); break;
                case "lily": Lily(p); break;
                case "honey": Honey(p); break;
                case "hive": Hive(p); break;
                case "cake": Cake(p, n < 0 ? 3 : n, mood != Sleep); break;
                case "gift": Gift(p, c ?? Col("FF8FAB")); break;
                case "balloon": Balloon(p, c ?? Col("FF8FAB")); break;
                case "ball": Ball(p); break;
                case "acorn": Acorn(p); break;
                case "nest": Nest(p, n < 0 ? 3 : n); break;
                case "bubble": Bubble(p); break;
                case "heart": Heart(p); break;
                case "note": Note(p); break;
                case "zz": Zz(p); break;
                case "spark": Spark(p); break;
                case "pebble": Pebbles(p, n < 0 ? 3 : n); break;
                case "blanket": Blanket(p); break;
                case "bedhead": BedHead(p); break;
                case "bed": Bed(p); break;
                case "vase": Vase(p); break;
            }
        }

        /// <summary>
        /// Önden bakan sevimli hayvanlar (tavşan, ayı, kedi, köpek, fare, aslan, tilki, sincap, kirpi, kurbağa).
        /// Ayaklar y=0'da, toplam boy ~1 (kulaklar hariç).
        /// </summary>
        static void Critter(P p, string kind, int mood)
        {
            Color body, belly = Col("FFF4E6"), earIn = Col("FFB3C7");
            switch (kind)
            {
                case "bunny": body = Col("F7F1F5"); belly = White; break;
                case "hare": body = Col("D8B48C"); belly = Col("FBEEDD"); break;
                case "bear": body = Col("B98356"); belly = Col("F2D7B6"); earIn = Col("E8B48E"); break;
                case "cat": body = Col("FFB26B"); belly = Col("FFF1E0"); break;
                case "dog": body = Col("F2D9B3"); belly = Col("FFF8EC"); break;
                case "mouse": body = Col("C9C2D9"); belly = Col("F4F0FA"); break;
                case "lion": body = Col("F6C453"); belly = Col("FFEDC2"); earIn = Col("E9A23B"); break;
                case "fox": body = Col("F7924A"); belly = White; earIn = Col("FFE1CC"); break;
                case "squirrel": body = Col("D98B55"); belly = Col("FBE3C8"); break;
                case "hedgehog": body = Col("F0CFA8"); belly = Col("FFF0DD"); break;
                default: body = Col("8FD16A"); belly = Col("E1F5C8"); break;   // frog
            }
            Color bt = Light(body, 0.28f), bb = Dark(body, 0.10f);
            bool frog = kind == "frog";

            p.Ground(0.3f);

            // ---- kuyruk (gövdenin arkasında, sağdan görünür) ----
            switch (kind)
            {
                case "bunny": case "hare":
                    p.C(0.2f, 0.17f, 0.075f, White, Col("EDE3EA")); break;
                case "cat":
                    p.Cp(0.18f, 0.12f, 0.31f, 0.2f, 0.045f, bt, bb);
                    p.Cp(0.31f, 0.2f, 0.33f, 0.4f, 0.045f, bt, bb); break;
                case "dog":
                    p.Cp(0.18f, 0.2f, 0.3f, 0.36f, 0.04f, bt, bb); break;
                case "mouse":
                    p.Arc(0.32f, 0.2f, 0.12f, 200, 360, 0.022f, Dark(body, 0.25f)); break;
                case "lion":
                    p.Cp(0.18f, 0.12f, 0.36f, 0.28f, 0.025f, bt, bb);
                    p.C(0.37f, 0.3f, 0.055f, Col("D98A3D"), Col("B8692C")); break;
                case "fox":
                    p.E(0.3f, 0.28f, 0.11f, 0.2f, bt, bb, -35);
                    p.Solid(R(0.41f, 0.44f, 0.09f, 0.09f), q => Mathf.Max(Ell(q, 0.3f, 0.28f, 0.11f, 0.2f, -35), Circ(q, 0.41f, 0.44f, 0.08f)), White); break;
                case "squirrel":
                    p.E(0.24f, 0.55f, 0.17f, 0.32f, bt, bb, -12);
                    p.E(0.27f, 0.6f, 0.1f, 0.22f, Light(body, 0.45f), Light(body, 0.2f), -12, false);
                    p.Arc(0.3f, 0.86f, 0.1f, 90, 250, 0.03f, A(Dark(body, 0.15f), 0.5f)); break;
            }

            // ---- kirpi dikenleri / aslan yelesi ----
            if (kind == "hedgehog")
            {
                var spikes = new Vector2[44];
                for (int i = 0; i < 44; i++)
                {
                    float a = i / 44f * Mathf.PI * 2f;
                    float r = i % 2 == 0 ? 1f : 0.8f;
                    spikes[i] = V(Mathf.Cos(a) * 0.43f * r, 0.5f + Mathf.Sin(a) * 0.42f * r);
                }
                p.Pg(spikes, Col("9A7458"), Col("6E5040"));
            }
            if (kind == "lion")
            {
                p.Shape(R(0, 0.64f, 0.42f, 0.42f), q =>
                {
                    float dx = q.x, dy = q.y - 0.64f;
                    float a = Mathf.Atan2(dy, dx);
                    return Mathf.Sqrt(dx * dx + dy * dy) - (0.35f + 0.035f * Mathf.Cos(a * 11f));
                }, Col("E59A45"), Col("C8742E"));
            }

            // ---- kulaklar (başın arkasında) ----
            switch (kind)
            {
                case "bunny": case "hare":
                    p.E(-0.1f, 0.98f, 0.065f, 0.2f, bt, bb, 8);
                    p.E(0.1f, 0.98f, 0.065f, 0.2f, bt, bb, -8);
                    p.E(-0.1f, 0.97f, 0.03f, 0.14f, earIn, Dark(earIn, 0.05f), 8, false);
                    p.E(0.1f, 0.97f, 0.03f, 0.14f, earIn, Dark(earIn, 0.05f), -8, false); break;
                case "bear": case "lion": case "hedgehog":
                    float er = kind == "hedgehog" ? 0.06f : 0.085f;
                    p.C(-0.18f, 0.84f, er, bt, bb); p.C(0.18f, 0.84f, er, bt, bb);
                    p.C(-0.18f, 0.84f, er * 0.55f, earIn, earIn, false); p.C(0.18f, 0.84f, er * 0.55f, earIn, earIn, false); break;
                case "mouse":
                    p.C(-0.21f, 0.84f, 0.12f, bt, bb); p.C(0.21f, 0.84f, 0.12f, bt, bb);
                    p.C(-0.21f, 0.84f, 0.075f, earIn, Dark(earIn, 0.05f), false); p.C(0.21f, 0.84f, 0.075f, earIn, Dark(earIn, 0.05f), false); break;
                case "cat": case "fox": case "squirrel":
                    for (int sgn = -1; sgn <= 1; sgn += 2)
                    {
                        float ex = 0.15f * sgn;
                        p.Pg(new[] { V(ex - 0.09f, 0.76f), V(ex + 0.02f * sgn, 0.98f), V(ex + 0.09f, 0.74f) }, bt, bb);
                        p.Pg(new[] { V(ex - 0.045f, 0.78f), V(ex + 0.015f * sgn, 0.92f), V(ex + 0.05f, 0.77f) }, kind == "fox" ? Col("5E4B6E") : earIn, kind == "fox" ? Col("4A3B58") : earIn, false);
                    }
                    break;
            }

            // ---- gövde ----
            p.E(0, 0.27f, 0.235f, 0.245f, bt, bb);
            p.E(0, 0.24f, 0.15f, 0.16f, belly, Dark(belly, 0.06f), 0, false);
            // kollar
            p.E(-0.21f, 0.3f, 0.06f, 0.095f, bt, bb, -28);
            p.E(0.21f, 0.3f, 0.06f, 0.095f, bt, bb, 28);
            // ayaklar
            p.E(-0.12f, 0.045f, 0.085f, 0.05f, Light(body, 0.15f), Dark(body, 0.15f));
            p.E(0.12f, 0.045f, 0.085f, 0.05f, Light(body, 0.15f), Dark(body, 0.15f));

            // ---- baş ----
            float hy = 0.63f;
            if (frog)
            {
                p.Shape(R(0, 0.68f, 0.32f, 0.24f), q => Mathf.Min(Ell(q, 0, 0.6f, 0.3f, 0.2f), Mathf.Min(Circ(q, -0.14f, 0.78f, 0.1f), Circ(q, 0.14f, 0.78f, 0.1f))), bt, bb);
            }
            else if (kind == "cat" || kind == "fox")
                p.E(0, hy, 0.27f, 0.235f, bt, bb);
            else
                p.C(0, hy, 0.25f, bt, bb);

            // köpek: sarkık kulaklar ve göz lekesi
            if (kind == "dog")
            {
                p.SoftE(0.1f, 0.68f, 0.07f, 0.06f, A(Col("D9A876"), 0.8f), 0.02f);
                p.E(-0.25f, 0.62f, 0.075f, 0.16f, Col("C99470"), Col("A8744E"), -18);
                p.E(0.25f, 0.62f, 0.075f, 0.16f, Col("C99470"), Col("A8744E"), 18);
            }
            // kedi çizgileri
            if (kind == "cat")
            {
                p.Line(0, 0.84f, 0, 0.78f, 0.022f, A(Col("E58A3A"), 0.8f));
                p.Line(-0.05f, 0.83f, -0.04f, 0.79f, 0.018f, A(Col("E58A3A"), 0.8f));
                p.Line(0.05f, 0.83f, 0.04f, 0.79f, 0.018f, A(Col("E58A3A"), 0.8f));
            }
            // tilki: beyaz yanaklar
            if (kind == "fox")
            {
                p.Solid(R(0, 0.56f, 0.27f, 0.12f), q => Mathf.Max(Ell(q, 0, hy, 0.27f, 0.235f), Mathf.Min(Ell(q, -0.12f, 0.55f, 0.15f, 0.1f), Ell(q, 0.12f, 0.55f, 0.15f, 0.1f))), White);
            }

            // ---- yüz ----
            float eyeY = frog ? 0.79f : 0.67f, eyeX = frog ? 0.14f : 0.095f, eyeR = frog ? 0.034f : 0.032f;
            if (frog)
            {
                p.C(-0.14f, 0.79f, 0.065f, White, Col("F1F1F6"), false);
                p.C(0.14f, 0.79f, 0.065f, White, Col("F1F1F6"), false);
            }
            if (!frog && kind != "lion" && kind != "fox" && kind != "hedgehog")
                p.E(0, 0.575f, 0.105f, 0.075f, belly, Dark(belly, 0.06f), 0, false);
            if (kind == "hedgehog")
                p.E(0, 0.57f, 0.12f, 0.08f, Light(body, 0.35f), body, 0, false);
            Eye(p, -eyeX, eyeY, eyeR, mood);
            Eye(p, eyeX, eyeY, eyeR, mood);
            if (mood == Sad)
            {
                p.Line(-eyeX - 0.04f, eyeY + 0.07f, -eyeX + 0.03f, eyeY + 0.09f, 0.016f, Ink);
                p.Line(eyeX + 0.04f, eyeY + 0.07f, eyeX - 0.03f, eyeY + 0.09f, 0.016f, Ink);
                Tear(p, eyeX + 0.01f, eyeY - 0.08f, 0.018f);
            }
            if (frog)
            {
                p.Dot(-0.03f, 0.64f, 0.01f, Ink); p.Dot(0.03f, 0.64f, 0.01f, Ink);
                Mouth(p, 0, 0.57f, 0.07f, mood);
                Cheeks(p, 0.2f, 0.58f, 0.045f);
            }
            else
            {
                float noseY = 0.605f;
                if (kind == "hedgehog") p.E(0, 0.6f, 0.035f, 0.028f, Col("5E4B6E"), Col("3F3150"), 0, false);
                else p.E(0, noseY, 0.034f, 0.024f, Col("7A5A7E"), Col("5E4B6E"), 0, false);
                p.Gloss(-0.01f, noseY + 0.008f, 0.012f, 0.006f, 0.6f);
                Mouth(p, 0, 0.555f, 0.045f, mood);
                Cheeks(p, 0.165f, 0.585f, 0.04f);
                if (kind == "cat" || kind == "mouse")
                {
                    for (int sgn = -1; sgn <= 1; sgn += 2)
                    {
                        p.Line(0.1f * sgn, 0.585f, 0.26f * sgn, 0.6f, 0.008f, A(Ink, 0.7f));
                        p.Line(0.1f * sgn, 0.57f, 0.25f * sgn, 0.55f, 0.008f, A(Ink, 0.7f));
                    }
                }
            }
            // parlama
            p.Gloss(-0.09f, hy + 0.15f, 0.06f, 0.03f, 0.5f, 20);
            p.Gloss(-0.11f, 0.38f, 0.04f, 0.06f, 0.35f, 30);
        }

        static void Owl(P p, int mood)
        {
            Color body = Col("A88466"), bt = Light(body, 0.25f), bb = Dark(body, 0.1f);
            p.Ground(0.22f);
            p.E(-0.24f, 0.42f, 0.09f, 0.2f, Dark(body, 0.05f), Dark(body, 0.2f), 18);
            p.E(0.24f, 0.42f, 0.09f, 0.2f, Dark(body, 0.05f), Dark(body, 0.2f), -18);
            p.Pg(new[] { V(-0.24f, 0.62f), V(-0.2f, 0.86f), V(-0.08f, 0.72f) }, bt, bb);
            p.Pg(new[] { V(0.24f, 0.62f), V(0.2f, 0.86f), V(0.08f, 0.72f) }, bt, bb);
            p.E(0, 0.42f, 0.26f, 0.37f, bt, bb);
            p.E(0, 0.3f, 0.17f, 0.2f, Col("F6E3C8"), Col("E8CFAE"), 0, false);
            for (int r = 0; r < 3; r++)
                for (int k = -1; k <= 1; k++)
                    p.Arc(k * 0.07f + (r % 2) * 0.035f - 0.0175f, 0.36f - r * 0.07f, 0.025f, 200, 340, 0.01f, A(Col("C9A27E"), 0.9f));
            p.C(-0.1f, 0.58f, 0.115f, Col("FFF5E6"), Col("F2E0C6"));
            p.C(0.1f, 0.58f, 0.115f, Col("FFF5E6"), Col("F2E0C6"));
            Eye(p, -0.1f, 0.58f, 0.05f, mood);
            Eye(p, 0.1f, 0.58f, 0.05f, mood);
            p.Pg(new[] { V(-0.035f, 0.52f), V(0.035f, 0.52f), V(0, 0.45f) }, Col("FFC145"), Col("F39C2D"));
            Cheeks(p, 0.19f, 0.47f, 0.035f);
            p.E(-0.07f, 0.04f, 0.05f, 0.03f, Col("FFC145"), Col("F39C2D"));
            p.E(0.07f, 0.04f, 0.05f, 0.03f, Col("FFC145"), Col("F39C2D"));
            p.Gloss(-0.1f, 0.72f, 0.06f, 0.03f, 0.4f, 15);
        }

        /// <summary>Yandan kuşlar (sağa bakar): ördek yavrusu, gri yavru, kuğu, tavuk, karga, serçe.</summary>
        static void Bird(P p, string kind, int mood, bool swim)
        {
            Color body, beak = Col("FFA94D");
            switch (kind)
            {
                case "duck": body = Col("FFD95A"); break;
                case "gduck": body = Col("B9B4C4"); beak = Col("9F98AD"); break;
                case "swan": body = Col("FFFFFF"); break;
                case "hen": body = Col("C8553D"); beak = Col("FFC145"); break;
                case "crow": body = Col("4F5577"); beak = Col("A5A0B5"); break;
                default: body = Col("7FB6E0"); beak = Col("FFC145"); break;
            }
            Color bt = Light(body, kind == "crow" ? 0.18f : 0.3f), bb = Dark(body, kind == "swan" ? 0.06f : 0.1f);
            bool round = kind == "duck" || kind == "gduck" || kind == "bird";

            if (swim)
            {
                p.SoftE(0, 0.1f, 0.42f, 0.05f, A(Col("FFFFFF"), 0.35f), 0.03f);
            }
            else
            {
                p.Ground(0.25f);
                p.Line(-0.05f, 0.14f, -0.06f, 0.02f, 0.025f, beak);
                p.Line(0.07f, 0.14f, 0.08f, 0.02f, 0.025f, beak);
                p.E(-0.04f, 0.02f, 0.05f, 0.02f, beak, Dark(beak, 0.1f));
                p.E(0.1f, 0.02f, 0.05f, 0.02f, beak, Dark(beak, 0.1f));
            }
            // kuyruk
            if (kind == "hen")
            {
                p.E(-0.3f, 0.5f, 0.06f, 0.16f, Col("4F5577"), Col("3D3B4F"), 30);
                p.E(-0.25f, 0.52f, 0.06f, 0.15f, Col("6E5B8E"), Col("4F4568"), 10);
            }
            else p.Pg(new[] { V(-0.2f, 0.36f), V(-0.42f, kind == "crow" ? 0.44f : 0.5f), V(-0.38f, 0.28f) }, bt, bb);

            float by = swim ? 0.24f : 0.3f;
            if (round) p.E(0, by, 0.23f, 0.18f, bt, bb);
            else p.E(0, by, 0.28f, 0.19f, bt, bb);

            float hx = round ? 0.14f : 0.18f, hy = round ? by + 0.24f : by + 0.26f, hr = round ? 0.165f : 0.15f;
            if (kind == "swan")
            {
                p.Shape(new Rect(0.1f, by, 0.25f, 0.6f), q => Mathf.Min(Cap(q, 0.16f, by + 0.06f, 0.27f, by + 0.26f, 0.05f),
                    Mathf.Min(Cap(q, 0.27f, by + 0.26f, 0.21f, by + 0.44f, 0.05f), Cap(q, 0.21f, by + 0.44f, 0.25f, by + 0.56f, 0.05f))), bt, bb);
                hx = 0.27f; hy = by + 0.6f; hr = 0.085f;
            }
            p.C(hx, hy, hr, bt, bb);

            // kanat
            p.E(-0.04f, by + 0.01f, 0.15f, 0.095f, Light(body, kind == "crow" ? 0.1f : 0.18f), Dark(body, 0.16f), 12);
            p.Gloss(-0.06f, by + 0.06f, 0.07f, 0.02f, 0.4f, 12);

            // gaga
            float bx = hx + hr * 0.95f, byy = hy - hr * 0.2f;
            switch (kind)
            {
                case "duck": case "gduck":
                    p.Bx(bx + 0.02f, byy, 0.07f, 0.035f, 0.03f, Light(beak, 0.2f), beak); break;
                case "swan":
                    p.Pg(new[] { V(bx - 0.02f, byy + 0.03f), V(bx + 0.11f, byy - 0.01f), V(bx - 0.02f, byy - 0.04f) }, Light(beak, 0.2f), beak);
                    p.Dot(bx - 0.015f, byy, 0.022f, Col("3D3B4F")); break;
                case "crow":
                    p.Pg(new[] { V(bx - 0.03f, byy + 0.04f), V(bx + 0.13f, byy - 0.01f), V(bx - 0.03f, byy - 0.04f) }, Light(beak, 0.2f), beak); break;
                default:
                    p.Pg(new[] { V(bx - 0.02f, byy + 0.03f), V(bx + 0.07f, byy), V(bx - 0.02f, byy - 0.03f) }, Light(beak, 0.2f), beak); break;
            }
            if (kind == "hen")
            {
                p.C(hx - 0.07f, hy + hr * 0.95f, 0.04f, Col("FF6B6E"), Col("E5484D"));
                p.C(hx - 0.02f, hy + hr * 1.05f, 0.045f, Col("FF6B6E"), Col("E5484D"));
                p.C(hx + 0.04f, hy + hr * 0.95f, 0.04f, Col("FF6B6E"), Col("E5484D"));
                p.E(bx - 0.01f, byy - 0.08f, 0.025f, 0.045f, Col("FF6B6E"), Col("E5484D"));
            }
            float er = kind == "swan" ? 0.022f : 0.03f;
            Eye(p, hx + hr * 0.3f, hy + hr * 0.2f, er, mood);
            if (mood == Sad) Tear(p, hx + hr * 0.3f, hy - hr * 0.3f, 0.018f);
            if (kind != "crow") p.SoftE(hx + hr * 0.15f, hy - hr * 0.35f, 0.03f, 0.02f, A(Blush, 0.55f), 0.015f);
            p.Gloss(hx - hr * 0.35f, hy + hr * 0.5f, hr * 0.3f, hr * 0.15f, 0.45f, 20);

            if (swim)
            {
                // suyun yüzeyi gövdenin altını keser
                p.Soft(R(0, 0.06f, 0.5f, 0.1f), q => Mathf.Max(q.y - 0.1f, Ell(q, 0, 0.08f, 0.44f, 0.09f)), A(Col("8FD3F4"), 0.75f), 0.015f);
                p.Arc(0, 0.02f, 0.4f, 70, 110, 0.012f, A(White, 0.7f));
            }
        }

        static void Ladybug(P p, int mood, int spots)
        {
            p.Ground(0.3f);
            for (int sgn = -1; sgn <= 1; sgn += 2)
                for (int k = 0; k < 3; k++)
                    p.Line(0.2f * sgn, 0.2f + k * 0.12f, 0.36f * sgn, 0.08f + k * 0.14f, 0.022f, Ink);
            p.C(0, 0.38f, 0.32f, Col("FF6B6E"), Col("D9363E"));
            p.Line(0, 0.07f, 0, 0.66f, 0.016f, Ink);
            var pos = new[] { V(-0.15f, 0.5f), V(0.15f, 0.5f), V(-0.2f, 0.3f), V(0.2f, 0.3f), V(-0.1f, 0.16f), V(0.1f, 0.16f), V(0.085f, 0.385f) };
            for (int i = 0; i < Mathf.Min(spots, pos.Length); i++) p.C(pos[i].x, pos[i].y, 0.055f, Col("4A4060"), Col("332B45"), false);
            p.Gloss(-0.12f, 0.56f, 0.09f, 0.04f, 0.5f, 25);
            p.C(0, 0.69f, 0.17f, Col("5A5070"), Col("3D3550"));
            p.Arc(-0.12f, 0.86f, 0.08f, 0, 80, 0.016f, Ink); p.Dot(-0.04f, 0.94f, 0.025f, Ink);
            p.Arc(0.12f, 0.86f, 0.08f, 100, 180, 0.016f, Ink); p.Dot(0.04f, 0.94f, 0.025f, Ink);
            p.C(-0.06f, 0.72f, 0.045f, White, Col("EEEAF4"), false);
            p.C(0.06f, 0.72f, 0.045f, White, Col("EEEAF4"), false);
            Eye(p, -0.06f, 0.72f, 0.025f, mood == Happy ? Normal : mood);
            Eye(p, 0.06f, 0.72f, 0.025f, mood == Happy ? Normal : mood);
            if (mood == Happy) p.Arc(0, 0.66f, 0.04f, 210, 330, 0.014f, White);
            else if (mood == Sad) { p.Arc(0, 0.6f, 0.035f, 40, 140, 0.014f, White); Tear(p, 0.07f, 0.64f, 0.016f); }
            else p.Arc(0, 0.665f, 0.03f, 220, 320, 0.012f, White);
            p.SoftE(-0.11f, 0.65f, 0.03f, 0.018f, A(Blush, 0.6f), 0.012f);
            p.SoftE(0.11f, 0.65f, 0.03f, 0.018f, A(Blush, 0.6f), 0.012f);
        }

        static void Bee(P p, int mood, bool air)
        {
            if (!air) p.Ground(0.25f);
            p.E(-0.17f, 0.72f, 0.12f, 0.17f, A(White, 0.85f), A(Col("DDEFFF"), 0.85f), 30);
            p.E(0.17f, 0.72f, 0.12f, 0.17f, A(White, 0.85f), A(Col("DDEFFF"), 0.85f), -30);
            p.Pg(new[] { V(-0.03f, 0.13f), V(0.03f, 0.13f), V(0, 0.06f) }, Ink, Ink);
            p.C(0, 0.42f, 0.3f, Col("FFE27A"), Col("F6B93B"));
            var dark = Col("4A3F5C");
            p.Solid(R(0, 0.3f, 0.3f, 0.06f), q => Mathf.Max(Circ(q, 0, 0.42f, 0.3f), Mathf.Abs(q.y - 0.3f) - 0.045f), dark);
            p.Solid(R(0, 0.17f, 0.3f, 0.06f), q => Mathf.Max(Circ(q, 0, 0.42f, 0.3f), Mathf.Abs(q.y - 0.17f) - 0.04f), dark);
            p.Line(-0.07f, 0.7f, -0.14f, 0.86f, 0.016f, Ink); p.Dot(-0.14f, 0.87f, 0.028f, Ink);
            p.Line(0.07f, 0.7f, 0.14f, 0.86f, 0.016f, Ink); p.Dot(0.14f, 0.87f, 0.028f, Ink);
            Eye(p, -0.1f, 0.52f, 0.035f, mood);
            Eye(p, 0.1f, 0.52f, 0.035f, mood);
            Mouth(p, 0, 0.44f, 0.045f, mood);
            Cheeks(p, 0.17f, 0.45f, 0.04f);
            p.Gloss(-0.12f, 0.6f, 0.08f, 0.035f, 0.55f, 25);
        }

        static void Butterfly(P p, int mood, Color c)
        {
            Color c2 = Light(c, 0.3f);
            p.E(-0.24f, 0.62f, 0.22f, 0.19f, Light(c, 0.25f), Dark(c, 0.06f), 25);
            p.E(0.24f, 0.62f, 0.22f, 0.19f, Light(c, 0.25f), Dark(c, 0.06f), -25);
            p.E(-0.17f, 0.36f, 0.15f, 0.13f, c2, c, -25);
            p.E(0.17f, 0.36f, 0.15f, 0.13f, c2, c, 25);
            p.Dot(-0.27f, 0.65f, 0.06f, A(White, 0.65f)); p.Dot(0.27f, 0.65f, 0.06f, A(White, 0.65f));
            p.Dot(-0.18f, 0.34f, 0.04f, A(White, 0.55f)); p.Dot(0.18f, 0.34f, 0.04f, A(White, 0.55f));
            p.Cp(0, 0.28f, 0, 0.62f, 0.05f, Col("7A6890"), Col("5E4B6E"));
            p.C(0, 0.71f, 0.085f, Col("7A6890"), Col("5E4B6E"));
            p.Line(-0.03f, 0.78f, -0.1f, 0.9f, 0.014f, Ink); p.Dot(-0.1f, 0.91f, 0.022f, Ink);
            p.Line(0.03f, 0.78f, 0.1f, 0.9f, 0.014f, Ink); p.Dot(0.1f, 0.91f, 0.022f, Ink);
            p.Dot(-0.03f, 0.72f, 0.016f, White); p.Dot(0.03f, 0.72f, 0.016f, White);
            p.Arc(0, 0.69f, 0.02f, 220, 320, 0.01f, White);
        }

        static void Ant(P p, int mood)
        {
            Color body = Col("C2604A"), bt = Light(body, 0.25f), bb = Dark(body, 0.12f);
            p.Ground(0.32f);
            p.Line(-0.05f, 0.28f, -0.14f, 0.02f, 0.02f, Ink);
            p.Line(0.0f, 0.28f, 0.02f, 0.02f, 0.02f, Ink);
            p.Line(0.05f, 0.28f, 0.16f, 0.03f, 0.02f, Ink);
            p.E(-0.27f, 0.3f, 0.18f, 0.15f, bt, bb);
            p.E(0.02f, 0.31f, 0.11f, 0.085f, bt, bb);
            p.C(0.24f, 0.42f, 0.16f, bt, bb);
            p.Line(0.26f, 0.56f, 0.34f, 0.74f, 0.016f, Ink); p.Dot(0.34f, 0.75f, 0.024f, Ink);
            p.Line(0.2f, 0.57f, 0.21f, 0.76f, 0.016f, Ink); p.Dot(0.21f, 0.77f, 0.024f, Ink);
            p.C(0.29f, 0.46f, 0.055f, White, Col("F2EEF6"), false);
            Eye(p, 0.3f, 0.46f, 0.03f, mood);
            Mouth(p, 0.31f, 0.36f, 0.035f, mood);
            p.SoftE(0.35f, 0.39f, 0.03f, 0.02f, A(Blush, 0.6f), 0.012f);
            p.Gloss(-0.3f, 0.38f, 0.07f, 0.03f, 0.45f, 15);
            p.Gloss(0.2f, 0.52f, 0.05f, 0.025f, 0.45f, 20);
        }

        static void Cricket(P p, int mood)
        {
            Color body = Col("7ACB55"), bt = Light(body, 0.25f), bb = Dark(body, 0.12f);
            p.Ground(0.34f);
            p.Line(0.12f, 0.24f, 0.2f, 0.02f, 0.02f, Ink);
            p.Line(0.02f, 0.24f, 0.04f, 0.02f, 0.02f, Ink);
            p.Cp(-0.05f, 0.3f, -0.26f, 0.5f, 0.06f, bt, bb);
            p.Cp(-0.26f, 0.5f, -0.36f, 0.04f, 0.032f, bt, bb);
            p.E(0, 0.3f, 0.3f, 0.14f, bt, bb);
            p.E(-0.06f, 0.35f, 0.25f, 0.085f, Col("5FB548"), Col("4A9A38"), 6);
            p.C(0.29f, 0.4f, 0.14f, bt, bb);
            p.Line(0.3f, 0.53f, 0.12f, 0.8f, 0.012f, Ink); p.Line(0.12f, 0.8f, -0.1f, 0.88f, 0.012f, Ink);
            p.Line(0.34f, 0.52f, 0.28f, 0.84f, 0.012f, Ink); p.Line(0.28f, 0.84f, 0.12f, 0.98f, 0.012f, Ink);
            p.C(0.33f, 0.44f, 0.05f, White, Col("F2EEF6"), false);
            Eye(p, 0.34f, 0.44f, 0.028f, mood);
            if (mood == Sad) Tear(p, 0.35f, 0.37f, 0.016f);
            Mouth(p, 0.35f, 0.35f, 0.035f, mood);
            p.SoftE(0.38f, 0.38f, 0.03f, 0.02f, A(Blush, 0.6f), 0.012f);
            p.Gloss(-0.06f, 0.4f, 0.12f, 0.02f, 0.45f, 6);
        }

        static void Snail(P p, int mood)
        {
            Color skin = Col("F6D7B5");
            p.Ground(0.38f);
            p.Line(0.33f, 0.36f, 0.3f, 0.54f, 0.02f, Ink); p.Dot(0.3f, 0.56f, 0.035f, Ink);
            p.Line(0.39f, 0.36f, 0.44f, 0.52f, 0.02f, Ink); p.Dot(0.44f, 0.54f, 0.035f, Ink);
            p.Shape(new Rect(-0.42f, 0, 0.88f, 0.44f), q => Mathf.Min(Cap(q, -0.36f, 0.075f, 0.3f, 0.075f, 0.075f), Cap(q, 0.3f, 0.08f, 0.36f, 0.3f, 0.09f)),
                Light(skin, 0.25f), Dark(skin, 0.08f));
            p.C(-0.04f, 0.4f, 0.27f, Col("C2AAEE"), Col("8E6FD0"));
            Vector2 prev = V(-0.04f, 0.4f);
            for (int i = 1; i <= 60; i++)
            {
                float t = i / 60f;
                float a = t * Mathf.PI * 4.2f;
                float r = 0.02f + t * 0.2f;
                var q = V(-0.04f + Mathf.Cos(a) * r, 0.4f + Mathf.Sin(a) * r);
                p.Line(prev.x, prev.y, q.x, q.y, 0.016f, A(Ink, 0.85f));
                prev = q;
            }
            p.Gloss(-0.13f, 0.55f, 0.08f, 0.035f, 0.5f, 25);
            Eye(p, 0.33f, 0.26f, 0.024f, mood);
            Eye(p, 0.4f, 0.26f, 0.024f, mood);
            Mouth(p, 0.365f, 0.18f, 0.03f, mood);
            p.SoftE(0.43f, 0.2f, 0.025f, 0.016f, A(Blush, 0.6f), 0.01f);
        }

        static void Fish(P p, int mood, Color c)
        {
            Color ct = Light(c, 0.3f), cb = Dark(c, 0.08f);
            p.Pg(new[] { V(-0.22f, 0.4f), V(-0.45f, 0.58f), V(-0.4f, 0.4f), V(-0.45f, 0.22f) }, Light(c, 0.15f), Dark(c, 0.15f));
            p.Pg(new[] { V(-0.12f, 0.56f), V(0.02f, 0.66f), V(0.12f, 0.57f) }, Light(c, 0.15f), Dark(c, 0.1f));
            p.E(0, 0.4f, 0.3f, 0.2f, ct, cb);
            p.Solid(R(-0.06f, 0.4f, 0.06f, 0.2f), q => Mathf.Max(Ell(q, 0, 0.4f, 0.3f, 0.2f), Mathf.Abs(q.x + 0.06f) - 0.035f), A(White, 0.55f));
            p.Solid(R(-0.18f, 0.4f, 0.06f, 0.2f), q => Mathf.Max(Ell(q, 0, 0.4f, 0.3f, 0.2f), Mathf.Abs(q.x + 0.18f) - 0.025f), A(White, 0.45f));
            p.E(0.0f, 0.33f, 0.07f, 0.035f, Light(c, 0.1f), Dark(c, 0.15f), -25);
            Eye(p, 0.15f, 0.45f, 0.04f, mood);
            Mouth(p, 0.22f, 0.35f, 0.035f, mood);
            p.SoftE(0.18f, 0.37f, 0.03f, 0.02f, A(Blush, 0.6f), 0.012f);
            p.Gloss(-0.02f, 0.52f, 0.1f, 0.03f, 0.5f, 5);
        }

        static void Turtle(P p, int mood, bool air)
        {
            Color skin = Col("A6DC85"), shell = Col("58A86E");
            if (!air) p.Ground(0.36f);
            p.E(-0.18f, 0.08f, 0.075f, 0.07f, Light(skin, 0.2f), skin);
            p.E(0.16f, 0.08f, 0.075f, 0.07f, Light(skin, 0.2f), skin);
            p.Pg(new[] { V(-0.3f, 0.16f), V(-0.42f, 0.13f), V(-0.3f, 0.1f) }, skin, skin);
            p.Cp(0.2f, 0.18f, 0.33f, 0.27f, 0.07f, Light(skin, 0.2f), skin);
            p.C(0.37f, 0.3f, 0.12f, Light(skin, 0.25f), skin);
            p.Shape(new Rect(-0.32f, 0.12f, 0.64f, 0.38f), q => Mathf.Max(Ell(q, 0, 0.16f, 0.3f, 0.3f), 0.15f - q.y), Light(shell, 0.2f), Dark(shell, 0.08f));
            p.C(-0.13f, 0.27f, 0.07f, A(Col("8ACF7A"), 0.9f), A(Col("6BBF59"), 0.9f), false);
            p.C(0.12f, 0.27f, 0.07f, A(Col("8ACF7A"), 0.9f), A(Col("6BBF59"), 0.9f), false);
            p.C(0, 0.37f, 0.075f, A(Col("8ACF7A"), 0.9f), A(Col("6BBF59"), 0.9f), false);
            p.Cp(-0.31f, 0.14f, 0.31f, 0.14f, 0.035f, Col("E9D58A"), Col("D9BE6A"));
            p.Gloss(-0.08f, 0.4f, 0.1f, 0.03f, 0.45f, 10);
            Eye(p, 0.41f, 0.34f, 0.028f, mood);
            Mouth(p, 0.43f, 0.26f, 0.03f, mood);
            p.SoftE(0.4f, 0.27f, 0.025f, 0.016f, A(Blush, 0.6f), 0.01f);
        }

        static void Starfish(P p, int mood)
        {
            p.Ground(0.3f);
            p.Shape(R(0, 0.38f, 0.42f, 0.42f), q =>
            {
                float dx = q.x, dy = q.y - 0.38f;
                float a = Mathf.Atan2(dy, dx) - Mathf.PI / 2f;
                return Mathf.Sqrt(dx * dx + dy * dy) - (0.26f + 0.12f * Mathf.Cos(a * 5f));
            }, Col("FFE27A"), Col("F6B93B"));
            for (int i = 0; i < 5; i++)
            {
                float a = Mathf.PI / 2f + i * Mathf.PI * 2f / 5f;
                p.Dot(Mathf.Cos(a) * 0.24f, 0.38f + Mathf.Sin(a) * 0.24f, 0.025f, A(White, 0.6f));
            }
            Eye(p, -0.07f, 0.42f, 0.03f, mood);
            Eye(p, 0.07f, 0.42f, 0.03f, mood);
            Mouth(p, 0, 0.35f, 0.04f, mood);
            Cheeks(p, 0.12f, 0.36f, 0.03f);
        }

        static void Crab(P p, int mood)
        {
            Color c = Col("F2645F"), ct = Light(c, 0.25f), cb = Dark(c, 0.08f);
            p.Ground(0.36f);
            for (int sgn = -1; sgn <= 1; sgn += 2)
                for (int k = 0; k < 3; k++)
                    p.Line(0.18f * sgn, 0.2f, (0.3f + k * 0.04f) * sgn, 0.02f + k * 0.03f, 0.022f, cb);
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                p.Cp(0.22f * sgn, 0.3f, 0.36f * sgn, 0.44f, 0.035f, ct, cb);
                float cx = 0.38f * sgn;
                p.Shape(R(cx, 0.52f, 0.1f, 0.1f), q => Mathf.Max(Circ(q, cx, 0.52f, 0.09f), -Circ(q, cx, 0.62f, 0.05f)), ct, cb);
            }
            p.Line(-0.08f, 0.4f, -0.09f, 0.55f, 0.02f, Ink);
            p.Line(0.08f, 0.4f, 0.09f, 0.55f, 0.02f, Ink);
            p.E(0, 0.28f, 0.27f, 0.17f, ct, cb);
            p.C(-0.09f, 0.57f, 0.055f, White, Col("F2EEF6"));
            p.C(0.09f, 0.57f, 0.055f, White, Col("F2EEF6"));
            Eye(p, -0.09f, 0.57f, 0.03f, mood);
            Eye(p, 0.09f, 0.57f, 0.03f, mood);
            Mouth(p, 0, 0.26f, 0.045f, mood);
            Cheeks(p, 0.15f, 0.27f, 0.035f);
            p.Gloss(-0.1f, 0.38f, 0.08f, 0.03f, 0.45f, 10);
        }

        static void Snowman(P p, int mood, int top)
        {
            Color w = Col("FFFFFF"), wb = Col("E3EAF8");
            p.Ground(0.26f);
            p.Line(-0.13f, 0.5f, -0.3f, 0.62f, 0.022f, Col("8C6A55"));
            p.Line(-0.25f, 0.585f, -0.3f, 0.66f, 0.018f, Col("8C6A55"));
            p.Line(0.13f, 0.5f, 0.3f, 0.62f, 0.022f, Col("8C6A55"));
            p.Line(0.25f, 0.585f, 0.31f, 0.65f, 0.018f, Col("8C6A55"));
            p.C(0, 0.2f, 0.2f, w, wb);
            p.C(0, 0.48f, 0.15f, w, wb);
            p.C(0, 0.72f, 0.115f, w, wb);
            p.Bx(0, 0.615f, 0.12f, 0.03f, 0.02f, Col("FF6B6E"), Col("E5484D"));
            p.Bx(0.07f, 0.52f, 0.03f, 0.08f, 0.015f, Col("FF6B6E"), Col("E5484D"), -10);
            for (int i = 0; i < 3; i++) p.Dot(0, 0.5f - i * 0.08f + (i == 0 ? 0.02f : 0), 0.017f, Ink);
            Eye(p, -0.04f, 0.75f, 0.018f, mood);
            Eye(p, 0.04f, 0.75f, 0.018f, mood);
            p.Pg(new[] { V(-0.01f, 0.73f), V(0.12f, 0.705f), V(-0.01f, 0.695f) }, Col("FFA94D"), Col("F27B2D"));
            for (int i = -2; i <= 2; i++) p.Dot(i * 0.02f, 0.67f - (2 - Mathf.Abs(i)) * 0.007f, 0.008f, Ink);
            Cheeks(p, 0.07f, 0.7f, 0.022f);
            p.Gloss(-0.06f, 0.3f, 0.06f, 0.03f, 0.6f, 20);
            if (top == 1)
            {
                p.Bx(0, 0.83f, 0.13f, 0.018f, 0.01f, Col("4A4560"), Col("3D3B4F"));
                p.Bx(0, 0.92f, 0.08f, 0.08f, 0.01f, Col("4A4560"), Col("3D3B4F"));
                p.Bx(0, 0.865f, 0.081f, 0.017f, 0.0f, Col("FF6B6E"), Col("E5484D"), 0, false);
            }
            else if (top == 2)
            {
                for (int i = 0; i < 6; i++)
                {
                    float a = i * Mathf.PI / 3f;
                    p.C(0.07f + Mathf.Cos(a) * 0.035f, 0.82f + Mathf.Sin(a) * 0.035f, 0.026f, Col("FFC8D6"), Col("FF8FAB"));
                }
                p.C(0.07f, 0.82f, 0.02f, Col("FFE27A"), Col("F6B93B"));
            }
        }

        // ======================= nesneler =======================

        static void Sun(P p, int mood)
        {
            p.SoftE(0, 0, 0.42f, 0.42f, A(Col("FFE9A0"), 0.5f), 0.22f);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6f;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                p.Cp(c * 0.27f, s * 0.27f, c * 0.36f, s * 0.36f, 0.03f, Col("FFD966"), Col("FFC145"));
            }
            p.C(0, 0, 0.22f, Col("FFE680"), Col("FFC145"));
            p.Gloss(-0.07f, 0.09f, 0.07f, 0.035f, 0.55f, 25);
            if (mood != Normal)
            {
                Eye(p, -0.07f, 0.03f, 0.025f, mood);
                Eye(p, 0.07f, 0.03f, 0.025f, mood);
                Mouth(p, 0, -0.06f, 0.04f, mood);
                Cheeks(p, 0.12f, -0.04f, 0.03f);
            }
        }

        static void Moon(P p, int phase, int mood)
        {
            if (phase > 0) p.SoftE(0, 0, 0.36f, 0.36f, A(Col("FFF6C9"), 0.35f), 0.18f);
            Func<Vector2, float> sdf = phase switch
            {
                0 => q => Circ(q, 0, 0, 0.2f),
                1 => q => Mathf.Max(Circ(q, 0, 0, 0.2f), -Circ(q, 0.09f, 0.04f, 0.18f)),
                2 => q => Mathf.Max(Circ(q, 0, 0, 0.2f), q.x),
                3 => q => Mathf.Max(Circ(q, 0, 0, 0.2f), -Ell(q, 0.2f, 0, 0.09f, 0.22f)),
                _ => q => Circ(q, 0, 0, 0.2f),
            };
            if (phase == 0)
            {
                p.Solid(R(0, 0, 0.21f, 0.21f), q => Mathf.Abs(Circ(q, 0, 0, 0.2f)) - 0.004f, A(White, 0.25f));
                return;
            }
            p.Shape(R(0, 0, 0.22f, 0.22f), sdf, Col("FFF6C2"), Col("F5D76E"));
            p.Solid(R(-0.06f, -0.05f, 0.05f, 0.05f), q => Mathf.Max(sdf(q), Circ(q, -0.07f, -0.06f, 0.035f)), A(Col("E9C45A"), 0.5f));
            p.Solid(R(-0.1f, 0.08f, 0.04f, 0.04f), q => Mathf.Max(sdf(q), Circ(q, -0.1f, 0.08f, 0.025f)), A(Col("E9C45A"), 0.5f));
            if (phase == 4 && mood != Normal)
            {
                Eye(p, -0.06f, 0.03f, 0.022f, mood);
                Eye(p, 0.06f, 0.03f, 0.022f, mood);
                Mouth(p, 0, -0.05f, 0.035f, mood == Sleep ? Normal : mood);
                Cheeks(p, 0.11f, -0.03f, 0.028f);
            }
        }

        static Vector2[] StarPts(float cx, float cy, float ro, float ri)
        {
            var v = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = i % 2 == 0 ? ro : ri;
                v[i] = V(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r);
            }
            return v;
        }

        static void StarProp(P p)
        {
            p.SoftE(0, 0, 0.5f, 0.5f, A(Col("FFF3B0"), 0.4f), 0.25f);
            p.Pg(StarPts(0, 0, 0.5f, 0.22f), Col("FFF09A"), Col("FFC94D"));
            p.Gloss(-0.08f, 0.1f, 0.08f, 0.04f, 0.6f, 20);
        }

        static void Cloud(P p, bool rain)
        {
            Color top = rain ? Col("C7CEDF") : White, bot = rain ? Col("9AA5BF") : Col("E6EDF8");
            if (rain)
            {
                for (int i = -1; i <= 1; i++)
                {
                    float x = i * 0.18f, y = -0.3f + Mathf.Abs(i) * 0.05f;
                    Tear(p, x, y, 0.035f);
                }
            }
            p.Shape(R(0, 0.02f, 0.46f, 0.34f), q => Mathf.Min(Mathf.Min(Circ(q, -0.24f, 0, 0.18f), Circ(q, 0.02f, 0.08f, 0.25f)),
                Mathf.Min(Circ(q, 0.26f, -0.01f, 0.17f), Box(q, 0, -0.08f, 0.42f, 0.1f, 0.1f))), top, bot);
            p.Gloss(-0.06f, 0.2f, 0.12f, 0.04f, 0.5f);
        }

        static void Rainbow(P p)
        {
            var cols = new[] { "FF8FAB", "FFB870", "FFE27A", "9BE38A", "8FD3F4", "BDB2FF" };
            for (int i = 0; i < cols.Length; i++)
            {
                float r = 0.6f - i * 0.055f;
                float rr = r;
                p.Solid(new Rect(-r - 0.03f, 0, 2 * r + 0.06f, r + 0.03f), q => Mathf.Max(Mathf.Abs(new Vector2(q.x, q.y).magnitude - rr) - 0.028f, -q.y), A(Col(cols[i]), 0.85f));
            }
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                float cx = 0.47f * sgn;
                p.Shape(R(cx, 0.04f, 0.2f, 0.12f), q => Mathf.Min(Mathf.Min(Circ(q, cx - 0.09f, 0.03f, 0.08f), Circ(q, cx + 0.02f, 0.07f, 0.1f)), Circ(q, cx + 0.11f, 0.02f, 0.07f)), White, Col("E6EDF8"));
            }
        }

        static void Tree(P p, int season)
        {
            p.Ground(0.34f);
            Color bark = Col("A9775A");
            p.Shape(new Rect(-0.12f, 0, 0.24f, 0.62f), q => Mathf.Min(Box(q, 0, 0.25f, 0.055f, 0.26f, 0.02f),
                Mathf.Min(Cap(q, 0, 0.42f, -0.14f, 0.58f, 0.028f), Cap(q, 0, 0.38f, 0.13f, 0.56f, 0.025f))), Light(bark, 0.15f), Dark(bark, 0.1f));
            p.Line(-0.015f, 0.06f, -0.02f, 0.3f, 0.012f, A(Dark(bark, 0.25f), 0.5f));
            if (season == 0)
            {
                p.Cp(0, 0.5f, -0.05f, 0.8f, 0.024f, Light(bark, 0.15f), bark);
                p.Cp(-0.14f, 0.58f, -0.24f, 0.7f, 0.018f, Light(bark, 0.15f), bark);
                p.Cp(0.13f, 0.56f, 0.22f, 0.72f, 0.018f, Light(bark, 0.15f), bark);
                p.E(-0.14f, 0.6f, 0.05f, 0.02f, White, Col("E8EEF8"), 40);
                p.E(0.13f, 0.58f, 0.05f, 0.02f, White, Col("E8EEF8"), -40);
                p.E(-0.05f, 0.81f, 0.035f, 0.018f, White, Col("E8EEF8"));
                return;
            }
            Color top, bot;
            switch (season)
            {
                case 1: top = Col("B8E29A"); bot = Col("7CC66B"); break;
                case 3: top = Col("FFC86B"); bot = Col("E5793A"); break;
                default: top = Col("8ED67A"); bot = Col("4FA35A"); break;
            }
            Func<Vector2, float> canopy = q => Mathf.Min(Mathf.Min(Circ(q, 0, 0.66f, 0.25f), Circ(q, -0.2f, 0.56f, 0.18f)),
                Mathf.Min(Mathf.Min(Circ(q, 0.2f, 0.56f, 0.18f), Circ(q, -0.12f, 0.8f, 0.17f)), Circ(q, 0.13f, 0.81f, 0.17f)));
            p.Shape(new Rect(-0.4f, 0.36f, 0.8f, 0.64f), canopy, top, bot);
            p.Soft(new Rect(-0.4f, 0.36f, 0.8f, 0.3f), q => Mathf.Max(canopy(q), q.y - 0.52f), A(Dark(bot, 0.2f), 0.35f), 0.06f);
            p.Gloss(-0.1f, 0.85f, 0.1f, 0.04f, 0.4f, 20);
            var r = new System.Random(7 + season);
            if (season == 1)
            {
                for (int i = 0; i < 16; i++)
                {
                    float x = Rf(r, -0.32f, 0.32f), y = Rf(r, 0.45f, 0.92f);
                    if (canopy(V(x, y)) > -0.03f) continue;
                    for (int k = 0; k < 5; k++)
                    {
                        float a = k * Mathf.PI * 2f / 5f;
                        p.Dot(x + Mathf.Cos(a) * 0.018f, y + Mathf.Sin(a) * 0.018f, 0.016f, Col("FFC2D3"));
                    }
                    p.Dot(x, y, 0.009f, Col("FFE27A"));
                }
            }
            else if (season == 3)
            {
                for (int i = 0; i < 7; i++)
                    p.E(Rf(r, -0.45f, 0.45f), Rf(r, 0.04f, 0.4f), 0.035f, 0.018f, Col("FFB25C"), Col("E5793A"), Rf(r, 0, 180), true);
            }
        }

        static void Pine(P p, bool snow)
        {
            p.Ground(0.25f);
            p.Bx(0, 0.06f, 0.04f, 0.07f, 0.01f, Col("A9775A"), Col("8C6A55"));
            Color top = Col("6FBF8F"), bot = Col("3E8E6A");
            for (int i = 0; i < 3; i++)
            {
                float y0 = 0.1f + i * 0.22f, w = 0.3f - i * 0.07f;
                p.Pg(new[] { V(-w, y0), V(0, y0 + 0.36f), V(w, y0) }, top, bot);
                if (snow) p.Pg(new[] { V(-w * 0.45f, y0 + 0.2f), V(0, y0 + 0.36f), V(w * 0.45f, y0 + 0.2f) }, White, Col("E8EEF8"), false);
            }
        }

        static void Flower(P p, Color c)
        {
            p.Line(0, 0, 0, 0.55f, 0.035f, Col("6BBF59"));
            p.E(0.08f, 0.22f, 0.09f, 0.04f, Col("9BE38A"), Col("6BBF59"), 30);
            p.E(-0.07f, 0.34f, 0.07f, 0.03f, Col("9BE38A"), Col("6BBF59"), -30);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                p.C(Mathf.Cos(a) * 0.09f, 0.62f + Mathf.Sin(a) * 0.09f, 0.075f, Light(c, 0.3f), c);
            }
            p.C(0, 0.62f, 0.06f, Col("FFE27A"), Col("F6B93B"));
            p.Gloss(-0.02f, 0.65f, 0.02f, 0.01f, 0.7f);
        }

        static void Tuft(P p, Color c)
        {
            p.Pg(new[] { V(-0.12f, 0), V(-0.2f, 0.5f), V(-0.02f, 0) }, Light(c, 0.2f), c, false);
            p.Pg(new[] { V(-0.06f, 0), V(0.0f, 0.75f), V(0.06f, 0) }, Light(c, 0.25f), c, false);
            p.Pg(new[] { V(0.02f, 0), V(0.2f, 0.55f), V(0.12f, 0) }, Light(c, 0.2f), c, false);
        }

        static void Mushroom(P p)
        {
            p.Ground(0.22f);
            p.Bx(0, 0.16f, 0.08f, 0.16f, 0.05f, Col("FFF8EC"), Col("EADBC4"));
            p.Shape(R(0, 0.4f, 0.26f, 0.2f), q => Mathf.Max(Ell(q, 0, 0.3f, 0.26f, 0.24f), 0.28f - q.y), Col("FF7B6E"), Col("D9363E"));
            p.Dot(-0.1f, 0.42f, 0.035f, White); p.Dot(0.06f, 0.47f, 0.03f, White); p.Dot(0.14f, 0.36f, 0.025f, White);
            p.Gloss(-0.08f, 0.48f, 0.07f, 0.025f, 0.45f, 20);
        }

        static void Bunting(P p)
        {
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                p.Cp(0.42f * sgn, 0, 0.42f * sgn, 0.82f, 0.022f, Col("FFFFFF"), Col("D9D2E6"));
                p.C(0.42f * sgn, 0.84f, 0.035f, Col("FFE27A"), Col("F6B93B"));
            }
            var cols = new[] { "FF8FAB", "FFD166", "8FD3F4", "9BE38A", "BDB2FF", "FFB870", "FF8FAB" };
            p.Arc(0, 1.25f, 0.6f, 228, 312, 0.012f, Ink);
            for (int i = 0; i < cols.Length; i++)
            {
                float x = -0.33f + i * 0.11f;
                float y = 1.25f - Mathf.Sqrt(Mathf.Max(0, 0.36f - x * x));
                p.Pg(new[] { V(x - 0.045f, y), V(x, y - 0.11f), V(x + 0.045f, y) }, Light(Col(cols[i]), 0.2f), Col(cols[i]));
            }
        }

        static void Jug(P p, int level)
        {
            p.Ground(0.26f);
            Func<Vector2, float> body = q => Mathf.Min(Box(q, 0, 0.32f, 0.2f, 0.3f, 0.13f), Box(q, 0, 0.66f, 0.11f, 0.07f, 0.03f));
            p.Shape(new Rect(-0.21f, 0, 0.42f, 0.74f), body, A(Col("EAF6FF"), 0.75f), A(Col("CFE7F7"), 0.75f));
            float[] lv = { 0.12f, 0.28f, 0.44f, 0.58f };
            float top = lv[Mathf.Clamp(level, 0, 3)];
            p.Fill(new Rect(-0.2f, 0, 0.4f, top + 0.02f), q => Mathf.Max(Box(q, 0, 0.32f, 0.17f, 0.27f, 0.1f), q.y - top), q => Color.Lerp(Col("4FB0E0"), Col("8FD3F4"), q.y / Mathf.Max(0.01f, top)));
            p.Arc(0, top + 0.1f, 0.18f, 250, 290, 0.012f, A(White, 0.8f));
            if (level >= 2)
            {
                var r = new System.Random(11);
                for (int i = 0; i < 9; i++)
                    p.C(Rf(r, -0.12f, 0.12f), Rf(r, 0.07f, 0.12f + level * 0.03f), 0.035f, Col("C9C4D3"), Col("9F98AD"));
            }
            p.Arc(0.2f, 0.42f, 0.09f, 290, 430, 0.03f, A(Col("CFE7F7"), 1f));
            p.Line(-0.12f, 0.2f, -0.12f, 0.48f, 0.03f, A(White, 0.65f));
        }

        static void Net(P p)
        {
            Color rope = Col("D8B47A");
            Func<Vector2, float> dome = q => Mathf.Max(Ell(q, 0, 0.05f, 0.5f, 0.6f), -q.y);
            p.Solid(new Rect(-0.5f, 0, 1f, 0.66f), q =>
            {
                float u = (q.x + q.y) * 0.7071f / 0.09f, v = (q.x - q.y) * 0.7071f / 0.09f;
                float du = Mathf.Abs(u - Mathf.Round(u)), dv = Mathf.Abs(v - Mathf.Round(v));
                return Mathf.Max(dome(q), Mathf.Min(du, dv) * 0.09f - 0.007f);
            }, rope);
            p.Solid(new Rect(-0.52f, 0, 1.04f, 0.68f), q => Mathf.Max(Mathf.Abs(Ell(q, 0, 0.05f, 0.5f, 0.6f)) - 0.012f, -q.y), Dark(rope, 0.2f));
        }

        static void Rope(P p)
        {
            Color rope = Col("D8B47A");
            p.Cp(-0.4f, 0.05f, -0.05f, 0.1f, 0.03f, Light(rope, 0.2f), rope);
            p.Cp(0.05f, 0.07f, 0.4f, 0.03f, 0.03f, Light(rope, 0.2f), rope);
            p.C(-0.05f, 0.1f, 0.05f, Light(rope, 0.2f), rope);
        }

        static void Wheat(P p, int count)
        {
            count = Mathf.Clamp(count, 1, 5);
            for (int k = 0; k < count; k++)
            {
                float x = (k - (count - 1) * 0.5f) * 0.12f, lean = (k - (count - 1) * 0.5f) * 0.06f;
                float tx = x + lean, ty = 0.75f + (k % 2) * 0.06f;
                p.Line(x, 0, tx, ty, 0.022f, Col("C9A13E"));
                p.Line(x + lean * 0.4f, 0.3f, x + lean * 0.4f + 0.08f, 0.42f, 0.018f, Col("9BC45A"));
                for (int i = 0; i < 5; i++)
                {
                    float t = 0.72f + i * 0.07f;
                    float cx = x + lean * t / 0.75f, cy = t + (k % 2) * 0.06f;
                    p.E(cx - 0.03f, cy, 0.028f, 0.045f, Col("FFE08A"), Col("E9B949"), 25);
                    p.E(cx + 0.03f, cy, 0.028f, 0.045f, Col("FFE08A"), Col("E9B949"), -25);
                }
            }
        }

        static void Bread(P p)
        {
            p.Ground(0.4f);
            p.Shape(R(0, 0.2f, 0.42f, 0.22f), q => Mathf.Max(Ell(q, 0, 0.1f, 0.42f, 0.3f), 0.03f - q.y), Col("F2C27E"), Col("C9803F"));
            for (int i = -1; i <= 1; i++) p.Arc(i * 0.14f, 0.18f, 0.08f, 45, 135, 0.025f, A(Col("FFE6B8"), 0.9f));
            p.Gloss(-0.14f, 0.3f, 0.1f, 0.03f, 0.45f, 10);
        }

        // tint: sonbahar yaprağı gibi renkli yaprak için (c=...); verilmezse yeşil
        static void BigLeaf(P p, bool wet, Color? tint = null)
        {
            Color top = tint.HasValue ? Light(tint.Value, 0.25f) : Col("9BE38A");
            Color bot = tint.HasValue ? Dark(tint.Value, 0.12f) : Col("4FA35A");
            p.Line(0.02f, -0.03f, 0.06f, -0.55f, 0.04f, tint.HasValue ? Dark(tint.Value, 0.2f) : Col("5FAF56"));
            Func<Vector2, float> leaf = q => Mathf.Max(Ell(q, 0, 0, 0.64f, 0.24f, -6), -Ell(q, 0.62f, 0.0f, 0.08f, 0.06f));
            p.Shape(R(0, 0, 0.66f, 0.3f), leaf, top, bot);
            p.Soft(R(0, -0.1f, 0.66f, 0.2f), q => Mathf.Max(leaf(q), q.y + 0.05f), A(tint.HasValue ? Dark(tint.Value, 0.3f) : Col("3E8E4A"), 0.35f), 0.05f);
            p.Line(-0.58f, 0.06f, 0.56f, -0.06f, 0.016f, A(Col("D9F5C8"), 0.9f));
            for (int i = -4; i <= 4; i++)
            {
                if (i == 0) continue;
                float x = i * 0.12f, y = -i * 0.012f;
                p.Line(x, y, x + 0.08f, y + 0.12f, 0.01f, A(Col("D9F5C8"), 0.8f));
                p.Line(x, y, x + 0.08f, y - 0.12f, 0.01f, A(Col("D9F5C8"), 0.8f));
            }
            p.Gloss(-0.2f, 0.12f, 0.2f, 0.03f, 0.35f, -6);
            if (wet)
            {
                Tear(p, -0.3f, 0.2f, 0.025f);
                Tear(p, 0.2f, 0.17f, 0.02f);
                Tear(p, 0.45f, -0.22f, 0.022f);
            }
        }

        static void Sprout(P p, int stage, int mood)
        {
            if (stage >= 3)
            {
                p.Line(0, 0, 0, 0.82f, 0.04f, Col("6BBF59"));
                p.E(-0.1f, 0.35f, 0.12f, 0.05f, Col("9BE38A"), Col("5FAF56"), -25);
                p.E(0.1f, 0.5f, 0.12f, 0.05f, Col("9BE38A"), Col("5FAF56"), 25);
                for (int i = 0; i < 14; i++)
                {
                    float a = i * Mathf.PI * 2f / 14f;
                    p.E(Mathf.Cos(a) * 0.15f, 0.85f + Mathf.Sin(a) * 0.15f, 0.06f, 0.028f, Col("FFE27A"), Col("F6B93B"), a * Mathf.Rad2Deg);
                }
                p.C(0, 0.85f, 0.11f, Col("A8744E"), Col("7A5038"));
                for (int i = 0; i < 10; i++)
                {
                    float a = i * 2.4f, r = 0.02f + i * 0.007f;
                    p.Dot(Mathf.Cos(a) * r, 0.85f + Mathf.Sin(a) * r, 0.008f, A(Col("5A3A28"), 0.8f));
                }
                Eye(p, -0.035f, 0.87f, 0.014f, mood == Normal ? Happy : mood);
                Eye(p, 0.035f, 0.87f, 0.014f, mood == Normal ? Happy : mood);
                p.Arc(0, 0.84f, 0.025f, 220, 320, 0.01f, Ink);
            }
            else if (stage == 2)
            {
                p.Line(0, 0, 0, 0.36f, 0.03f, Col("6BBF59"));
                p.E(-0.08f, 0.3f, 0.09f, 0.04f, Col("9BE38A"), Col("5FAF56"), 25);
                p.E(0.08f, 0.36f, 0.09f, 0.04f, Col("9BE38A"), Col("5FAF56"), -25);
                p.E(0, 0.42f, 0.04f, 0.06f, Col("B8E29A"), Col("7CC66B"));
            }
            else if (stage == 1)
            {
                p.Line(0, 0, 0, 0.12f, 0.025f, Col("6BBF59"));
                p.E(-0.05f, 0.14f, 0.05f, 0.025f, Col("9BE38A"), Col("5FAF56"), 25);
                p.E(0.05f, 0.14f, 0.05f, 0.025f, Col("9BE38A"), Col("5FAF56"), -25);
            }
            p.Shape(R(0, 0.03f, 0.26f, 0.08f), q => Mathf.Max(Ell(q, 0, 0, 0.26f, 0.08f), -q.y), Col("A8805E"), Col("7A5A44"));
            p.Dot(-0.1f, 0.03f, 0.012f, A(Col("5A3A28"), 0.6f)); p.Dot(0.08f, 0.04f, 0.01f, A(Col("5A3A28"), 0.6f));
        }

        static void Can(P p)
        {
            p.Ground(0.3f);
            Color c = Col("7FB6E0");
            p.Cp(0.18f, 0.16f, 0.42f, 0.36f, 0.03f, Light(c, 0.2f), c);
            p.Arc(-0.02f, 0.38f, 0.14f, 20, 160, 0.035f, Dark(c, 0.15f));
            p.Bx(0, 0.18f, 0.22f, 0.18f, 0.05f, Light(c, 0.25f), Dark(c, 0.05f));
            p.Gloss(-0.1f, 0.26f, 0.06f, 0.02f, 0.5f, 10);
            Tear(p, 0.48f, 0.3f, 0.02f);
            Tear(p, 0.52f, 0.2f, 0.018f);
        }

        static void Kite(P p, bool str)
        {
            if (str) p.Line(0, -0.28f, -1.0f, -1.9f, 0.01f, A(Ink, 0.6f));
            var tail = V(0, -0.3f);
            for (int i = 1; i <= 6; i++)
            {
                var q = V(Mathf.Sin(i * 1.3f) * 0.06f, -0.3f - i * 0.08f);
                p.Line(tail.x, tail.y, q.x, q.y, 0.01f, A(Ink, 0.7f));
                tail = q;
                if (i % 2 == 0)
                {
                    var c = Col(i == 2 ? "FFD166" : i == 4 ? "8FD3F4" : "FF8FAB");
                    p.Pg(new[] { V(q.x - 0.05f, q.y + 0.03f), V(q.x, q.y), V(q.x - 0.05f, q.y - 0.03f) }, c, c);
                    p.Pg(new[] { V(q.x + 0.05f, q.y + 0.03f), V(q.x, q.y), V(q.x + 0.05f, q.y - 0.03f) }, c, c);
                }
            }
            var dia = new[] { V(0, 0.28f), V(0.19f, 0), V(0, -0.3f), V(-0.19f, 0) };
            p.Pg(dia, Col("FFB3C7"), Col("FF8FAB"));
            p.Solid(R(0.1f, 0, 0.1f, 0.3f), q => Mathf.Max(Poly(q, dia), -q.x), A(Col("FFD166"), 0.9f));
            p.Solid(R(0, 0.14f, 0.2f, 0.15f), q => Mathf.Max(Poly(q, dia), -(q.y - 0.0f)) + (q.x > 0 ? 0 : 1) * 10f, A(Col("8FD3F4"), 0.9f));
            p.Line(0, 0.27f, 0, -0.29f, 0.012f, A(Ink, 0.6f));
            p.Line(-0.18f, 0, 0.18f, 0, 0.012f, A(Ink, 0.6f));
            Eye(p, -0.05f, 0.04f, 0.02f, Happy);
            Eye(p, 0.05f, 0.04f, 0.02f, Happy);
        }

        static void Hat(P p, bool flying)
        {
            float rot = flying ? 22f : 0f;
            Color top = Col("4A4560"), bot = Col("3D3B4F");
            float c = Mathf.Cos(rot * Mathf.Deg2Rad), s = Mathf.Sin(rot * Mathf.Deg2Rad);
            Vector2 Rot(float x, float y) => V(x * c - y * s, x * s + y * c);
            var cr = Rot(0, 0.32f);
            var bd = Rot(0, 0.16f);
            p.Bx(0, 0.04f, 0.42f, 0.05f, 0.03f, top, bot, rot);
            p.Bx(cr.x, cr.y, 0.26f, 0.27f, 0.04f, top, bot, rot);
            p.Bx(bd.x, bd.y, 0.262f, 0.06f, 0, Col("FF6B6E"), Col("E5484D"), rot, false);
            var g = Rot(-0.12f, 0.45f);
            p.Gloss(g.x, g.y, 0.04f, 0.1f, 0.25f, rot);
        }

        static void Branch(P p)
        {
            Color bark = Col("A9775A");
            p.Cp(-0.35f, -0.01f, 0.75f, 0.03f, 0.04f, Light(bark, 0.15f), Dark(bark, 0.1f));
            p.Cp(0.3f, 0.01f, 0.45f, 0.16f, 0.022f, Light(bark, 0.15f), bark);
            p.E(0.47f, 0.2f, 0.1f, 0.05f, Col("9BE38A"), Col("5FAF56"), 30);
            p.E(-0.25f, 0.05f, 0.09f, 0.04f, Col("9BE38A"), Col("5FAF56"), 20);
            p.E(0.62f, -0.04f, 0.09f, 0.04f, Col("9BE38A"), Col("5FAF56"), -25);
        }

        static void Anthill(P p, bool snow)
        {
            p.Ground(0.4f);
            Func<Vector2, float> hill = q => Mathf.Max(Ell(q, 0, 0, 0.38f, 0.32f), -q.y);
            p.Shape(new Rect(-0.38f, 0, 0.76f, 0.34f), hill, Col("D9B48A"), Col("B38B62"));
            var r = new System.Random(3);
            for (int i = 0; i < 14; i++)
            {
                float x = Rf(r, -0.28f, 0.28f), y = Rf(r, 0.03f, 0.25f);
                if (hill(V(x, y)) < -0.03f) p.Dot(x, y, 0.012f, A(Col("8C6A4A"), 0.5f));
            }
            p.Shape(R(0, 0.09f, 0.1f, 0.1f), q => Mathf.Max(Mathf.Min(Circ(q, 0, 0.1f, 0.08f), Box(q, 0, 0.05f, 0.08f, 0.05f)), -q.y), Col("FFC86B"), Col("E09A3E"));
            p.Shape(R(0, 0.09f, 0.08f, 0.08f), q => Mathf.Max(Mathf.Min(Circ(q, 0, 0.1f, 0.055f), Box(q, 0, 0.05f, 0.055f, 0.05f)), -q.y), Col("6E5A70"), Col("4A3F5C"), false);
            p.Dot(0.03f, 0.07f, 0.01f, Col("FFE27A"));
            if (snow)
            {
                p.Shape(R(0, 0.27f, 0.3f, 0.08f), q => Mathf.Max(Ell(q, 0, 0.26f, 0.27f, 0.08f), -(q.y - 0.22f) + 0.0f) + 0f, White, Col("E8EEF8"));
            }
        }

        static void Lily(P p)
        {
            var notch = new[] { V(0, 0), V(0.5f, 0.1f), V(0.5f, -0.02f) };
            Func<Vector2, float> pad = q => Mathf.Max(Ell(q, 0, 0, 0.42f, 0.13f), -Poly(q, notch));
            p.Shape(R(0, 0, 0.44f, 0.15f), pad, Col("9BE38A"), Col("4FA35A"));
            p.Line(-0.3f, 0, 0.0f, 0, 0.01f, A(Col("D9F5C8"), 0.7f));
            p.Gloss(-0.15f, 0.05f, 0.12f, 0.025f, 0.35f);
        }

        static void Honey(P p)
        {
            p.Ground(0.36f);
            p.E(0, 0.3f, 0.32f, 0.3f, Col("E9B27A"), Col("C9803F"));
            p.Bx(0, 0.6f, 0.22f, 0.05f, 0.03f, Col("F2C27E"), Col("D99A4E"));
            p.Fill(R(0, 0.53f, 0.24f, 0.1f), q => Mathf.Min(Box(q, 0, 0.6f, 0.21f, 0.04f, 0.03f),
                Mathf.Min(Mathf.Min(Cap(q, -0.12f, 0.57f, -0.12f, 0.46f, 0.03f), Cap(q, 0.02f, 0.57f, 0.02f, 0.5f, 0.025f)), Cap(q, 0.14f, 0.57f, 0.14f, 0.43f, 0.03f))),
                q => Color.Lerp(Col("F6B93B"), Col("FFD966"), (q.y - 0.43f) / 0.2f));
            p.Bx(0, 0.28f, 0.17f, 0.1f, 0.03f, Col("FFF8EC"), Col("F2E6D2"));
            p.Pg(StarPts(0, 0.28f, 0.07f, 0.03f), Col("FFD966"), Col("F6B93B"), false);
            p.Gloss(-0.17f, 0.4f, 0.05f, 0.1f, 0.45f, 10);
        }

        static void Hive(P p)
        {
            p.Ground(0.36f);
            float[] ys = { 0.14f, 0.32f, 0.48f, 0.62f, 0.73f };
            float[] rx = { 0.34f, 0.32f, 0.27f, 0.2f, 0.12f };
            for (int i = 0; i < ys.Length; i++) p.E(0, ys[i], rx[i], 0.11f, Col("FFE08A"), Col("E9B949"));
            p.Shape(R(0, 0.12f, 0.09f, 0.08f), q => Mathf.Max(Ell(q, 0, 0.12f, 0.08f, 0.07f), -(q.y - 0.07f)), Col("6E5A70"), Col("4A3F5C"));
            p.Gloss(-0.12f, 0.6f, 0.06f, 0.03f, 0.5f, 20);
        }

        static void Cake(P p, int candles, bool lit)
        {
            p.E(0, 0.03f, 0.44f, 0.05f, Col("FFFFFF"), Col("E3E0EC"));
            p.Bx(0, 0.2f, 0.36f, 0.17f, 0.05f, Col("FFC8D6"), Col("FF9FB5"));
            p.Fill(R(0, 0.33f, 0.37f, 0.06f), q => Mathf.Min(Box(q, 0, 0.35f, 0.36f, 0.03f, 0.02f),
                Mathf.Min(Mathf.Min(Circ(q, -0.24f, 0.31f, 0.04f), Circ(q, -0.05f, 0.3f, 0.045f)), Mathf.Min(Circ(q, 0.14f, 0.31f, 0.04f), Circ(q, 0.3f, 0.32f, 0.03f)))), _ => White);
            p.Bx(0, 0.47f, 0.25f, 0.11f, 0.04f, Col("FFF6E6"), Col("F2E0C6"));
            var r = new System.Random(5);
            string[] sc = { "FF8FAB", "8FD3F4", "FFD166", "9BE38A", "BDB2FF" };
            for (int i = 0; i < 18; i++)
            {
                float x = Rf(r, -0.3f, 0.3f), y = Rf(r, 0.08f, 0.26f);
                p.Line(x, y, x + 0.02f, y + 0.01f, 0.014f, Col(sc[i % sc.Length]));
            }
            candles = Mathf.Clamp(candles, 1, 6);
            for (int i = 0; i < candles; i++)
            {
                float x = (i - (candles - 1) * 0.5f) * 0.11f;
                p.Bx(x, 0.66f, 0.022f, 0.08f, 0.01f, Col("A9D6F5"), Col("7FB6E0"));
                if (lit)
                {
                    p.SoftE(x, 0.79f, 0.06f, 0.07f, A(Col("FFE9A0"), 0.6f), 0.04f);
                    var flame = new[] { V(x - 0.02f, 0.785f), V(x, 0.83f), V(x + 0.02f, 0.785f) };
                    p.Shape(R(x, 0.79f, 0.03f, 0.05f), q => Mathf.Min(Circ(q, x, 0.775f, 0.022f), Poly(q, flame)), Col("FFE27A"), Col("FF9F43"), false);
                }
                else
                {
                    // üflenmiş mum: fitil ve kıvrılan ince duman
                    p.Line(x, 0.745f, x, 0.77f, 0.01f, Ink);
                    p.Arc(x + 0.02f, 0.8f, 0.025f, 120, 260, 0.01f, A(Col("B8B2C8"), 0.8f));
                    p.Arc(x - 0.005f, 0.85f, 0.022f, 280, 420, 0.009f, A(Col("B8B2C8"), 0.6f));
                }
            }
            p.Gloss(-0.22f, 0.27f, 0.06f, 0.03f, 0.45f, 0);
        }

        static void Gift(P p, Color c)
        {
            p.Ground(0.36f);
            p.Bx(0, 0.22f, 0.3f, 0.22f, 0.03f, Light(c, 0.2f), Dark(c, 0.05f));
            p.Bx(0, 0.47f, 0.34f, 0.07f, 0.03f, Light(c, 0.3f), c);
            p.Bx(0, 0.27f, 0.05f, 0.27f, 0, Col("FFFFFF"), Col("EDE8F5"), 0, false);
            p.E(-0.1f, 0.6f, 0.11f, 0.06f, Col("FFFFFF"), Col("EDE8F5"), 25);
            p.E(0.1f, 0.6f, 0.11f, 0.06f, Col("FFFFFF"), Col("EDE8F5"), -25);
            p.C(0, 0.57f, 0.04f, Col("FFFFFF"), Col("EDE8F5"));
            p.Gloss(-0.18f, 0.3f, 0.05f, 0.08f, 0.35f);
        }

        static void Balloon(P p, Color c)
        {
            p.Line(0, 0, 0.03f, 0.5f, 0.01f, A(Ink, 0.6f));
            p.Pg(new[] { V(-0.03f, 0.5f), V(0.03f, 0.5f), V(0, 0.54f) }, c, Dark(c, 0.1f));
            p.E(0, 0.76f, 0.2f, 0.25f, Light(c, 0.3f), Dark(c, 0.05f));
            p.Gloss(-0.07f, 0.86f, 0.04f, 0.08f, 0.65f, 20);
        }

        static void Ball(P p)
        {
            p.Ground(0.4f);
            p.C(0, 0.48f, 0.47f, Col("FFFFFF"), Col("EDE8F5"));
            string[] cols = { "FF6B6E", "FFD166", "8FD3F4" };
            for (int i = 0; i < 3; i++)
            {
                float a0 = i * 120f;
                var col = Col(cols[i]);
                p.Fill(R(0, 0.48f, 0.47f, 0.47f), q =>
                {
                    float ang = Mathf.Atan2(q.y - 0.48f, q.x) * Mathf.Rad2Deg;
                    while (ang < a0) ang += 360f;
                    while (ang >= a0 + 360f) ang -= 360f;
                    return ang - a0 < 60f ? Circ(q, 0, 0.48f, 0.47f) : 1f;
                }, _ => col);
            }
            p.C(0, 0.48f, 0.08f, Col("FFFFFF"), Col("EDE8F5"));
            p.Gloss(-0.17f, 0.7f, 0.12f, 0.06f, 0.55f, 25);
        }

        static void Acorn(P p)
        {
            p.Ground(0.36f);
            p.E(0, 0.36f, 0.32f, 0.36f, Col("E9B27A"), Col("C9803F"));
            p.Shape(R(0, 0.66f, 0.42f, 0.16f), q => Mathf.Max(Ell(q, 0, 0.62f, 0.42f, 0.22f), 0.56f - q.y), Col("A8805E"), Col("7A5A44"));
            p.Line(0, 0.8f, 0.05f, 0.95f, 0.05f, Col("7A5A44"));
            p.Gloss(-0.12f, 0.42f, 0.06f, 0.12f, 0.45f, 10);
        }

        static void Nest(P p, int eggs)
        {
            var pos = new[] { V(-0.15f, 0.32f), V(0.15f, 0.32f), V(0, 0.36f) };
            for (int i = 0; i < Mathf.Min(eggs, 3); i++)
            {
                p.E(pos[i].x, pos[i].y, 0.1f, 0.13f, Col("FFFDF6"), Col("EADFCB"));
                if (i == 2)
                {
                    p.Line(pos[i].x - 0.07f, pos[i].y + 0.03f, pos[i].x - 0.02f, pos[i].y + 0.08f, 0.012f, Ink);
                    p.Line(pos[i].x - 0.02f, pos[i].y + 0.08f, pos[i].x + 0.03f, pos[i].y + 0.03f, 0.012f, Ink);
                    p.Line(pos[i].x + 0.03f, pos[i].y + 0.03f, pos[i].x + 0.07f, pos[i].y + 0.07f, 0.012f, Ink);
                }
            }
            Func<Vector2, float> bowl = q => Mathf.Max(Ell(q, 0, 0.22f, 0.42f, 0.2f), q.y - 0.32f);
            p.Shape(new Rect(-0.42f, 0, 0.84f, 0.34f), bowl, Col("C9A06E"), Col("9C7550"));
            var r = new System.Random(9);
            for (int i = 0; i < 26; i++)
            {
                float x = Rf(r, -0.36f, 0.3f), y = Rf(r, 0.06f, 0.3f);
                if (bowl(V(x, y)) < -0.02f) p.Line(x, y, x + 0.09f, y + Rf(r, -0.03f, 0.03f), 0.01f, A(Col("7A5A44"), 0.7f));
            }
        }

        static void Bubble(P p)
        {
            p.Solid(R(0, 0, 0.5f, 0.5f), q => Circ(q, 0, 0, 0.5f), A(White, 0.15f));
            p.Solid(R(0, 0, 0.52f, 0.52f), q => Mathf.Abs(Circ(q, 0, 0, 0.48f)) - 0.035f, A(White, 0.65f));
            p.Gloss(-0.18f, 0.18f, 0.12f, 0.06f, 0.8f, 40);
        }

        static Func<Vector2, float> HeartSdf(float cx, float cy, float s)
        {
            var tipPoly = new[] { V(cx - 0.48f * s, cy + 0.04f * s), V(cx, cy - 0.5f * s), V(cx + 0.48f * s, cy + 0.04f * s), V(cx, cy + 0.15f * s) };
            return q => Mathf.Min(Mathf.Min(Circ(q, cx - 0.25f * s, cy + 0.12f * s, 0.27f * s), Circ(q, cx + 0.25f * s, cy + 0.12f * s, 0.27f * s)),
                Poly(q, tipPoly));
        }

        static void Heart(P p)
        {
            p.Shape(R(0, 0, 0.56f, 0.56f), HeartSdf(0, 0, 1f), Col("FFB3C7"), Col("FF6B8E"));
            p.Gloss(-0.24f, 0.2f, 0.1f, 0.05f, 0.65f, 30);
        }

        static void Note(P p)
        {
            Color c = Col("8E7CC3");
            p.E(-0.12f, 0, 0.15f, 0.1f, Light(c, 0.2f), c, 20);
            p.Cp(0.01f, 0.03f, 0.01f, 0.6f, 0.03f, Light(c, 0.2f), c);
            p.Shape(R(0.12f, 0.5f, 0.14f, 0.12f), q => Ell(q, 0.12f, 0.5f, 0.13f, 0.06f, -30), Light(c, 0.2f), c);
        }

        static void Zz(P p)
        {
            Color c = Col("8E7CC3");
            float[] xs = { 0, 0.22f, 0.4f }, ys = { 0, 0.22f, 0.42f }, ss = { 0.1f, 0.13f, 0.16f };
            for (int i = 0; i < 3; i++)
            {
                float x = xs[i], y = ys[i], s = ss[i];
                p.Line(x - s, y + s, x + s, y + s, s * 0.35f, A(c, 0.9f));
                p.Line(x + s, y + s, x - s, y - s, s * 0.35f, A(c, 0.9f));
                p.Line(x - s, y - s, x + s, y - s, s * 0.35f, A(c, 0.9f));
            }
        }

        static void Spark(P p)
        {
            p.SoftE(0, 0, 0.4f, 0.4f, A(Col("FFF3B0"), 0.5f), 0.2f);
            p.Shape(R(0, 0, 0.5f, 0.5f), q =>
            {
                float ax = Mathf.Abs(q.x), ay = Mathf.Abs(q.y);
                return Mathf.Pow(Mathf.Pow(ax, 0.5f) + Mathf.Pow(ay, 0.5f), 2f) - 0.45f;
            }, Col("FFFBE0"), Col("FFD966"));
        }

        static void Pebbles(P p, int n)
        {
            var r = new System.Random(n * 3 + 1);
            for (int i = 0; i < Mathf.Clamp(n, 1, 8); i++)
            {
                float x = (i - (n - 1) * 0.5f) * 0.28f + Rf(r, -0.04f, 0.04f);
                p.E(x, 0.13f, 0.16f, 0.12f, Col("D2CDDC"), Col("A49DB3"), Rf(r, -15, 15));
                p.Gloss(x - 0.05f, 0.18f, 0.05f, 0.02f, 0.5f);
            }
        }

        static void Blanket(P p)
        {
            string[] cols = { "FFC2D3", "FFE08A", "A9D6F5", "B9F0C8" };
            Func<Vector2, float> sh = q => Box(q, 0, 0.29f, 0.29f, 0.24f, 0.09f);
            p.Shape(R(0, 0.29f, 0.3f, 0.25f), sh, Col("FFFFFF"), Col("EDE8F5"));
            for (int gy = 0; gy < 4; gy++)
                for (int gx = 0; gx < 4; gx++)
                {
                    float cx = -0.21f + gx * 0.14f, cy = 0.1f + gy * 0.125f;
                    var col = Col(cols[(gx + gy) % 4]);
                    p.Fill(R(cx, cy, 0.075f, 0.07f), q => Mathf.Max(sh(q), Box(q, cx, cy, 0.068f, 0.06f, 0.01f)), q => Color.Lerp(Dark(col, 0.05f), Light(col, 0.15f), (q.y - cy + 0.06f) / 0.12f));
                }
            for (int i = 0; i < 4; i++)
                p.Line(-0.25f + i * 0.17f, 0.52f, -0.17f + i * 0.17f, 0.52f, 0.012f, A(Col("FF8FAB"), 0.8f));
            p.Gloss(-0.15f, 0.45f, 0.08f, 0.03f, 0.45f);
        }

        static void BedHead(P p)
        {
            Color wood = Col("C99A6E");
            p.Bx(0, 0.4f, 0.46f, 0.32f, 0.14f, Light(wood, 0.2f), Dark(wood, 0.08f));
            p.Bx(0, 0.43f, 0.38f, 0.24f, 0.1f, Light(wood, 0.35f), Light(wood, 0.1f), 0, false);
            p.E(0, 0.42f, 0.28f, 0.1f, Col("FFFFFF"), Col("E6E1F0"));
        }

        static void Bed(P p)
        {
            Color wood = Col("C99A6E"), cover = Col("A9C8F5");
            p.Bx(-0.48f, 0.08f, 0.04f, 0.1f, 0.02f, Light(wood, 0.2f), wood);
            p.Bx(0.48f, 0.08f, 0.04f, 0.1f, 0.02f, Light(wood, 0.2f), wood);
            Func<Vector2, float> sh = q => Box(q, 0, 0.16f, 0.5f, 0.12f, 0.06f);
            p.Shape(R(0, 0.16f, 0.5f, 0.12f), sh, Light(cover, 0.25f), Dark(cover, 0.05f));
            var r = new System.Random(4);
            for (int i = 0; i < 9; i++)
            {
                float x = Rf(r, -0.42f, 0.42f), y = Rf(r, 0.07f, 0.22f);
                p.Pg(StarPts(x, y, 0.025f, 0.011f), Col("FFF3B0"), Col("FFE08A"), false);
            }
            p.Bx(0, 0.27f, 0.5f, 0.035f, 0.03f, Col("FFFFFF"), Col("E6E1F0"));
            p.Gloss(-0.3f, 0.22f, 0.1f, 0.02f, 0.35f);
        }

        static void Vase(P p)
        {
            string[] cols = { "FF8FAB", "FFD166", "BDB2FF" };
            float[] tx = { -0.17f, 0, 0.17f }, ty = { 0.66f, 0.76f, 0.66f };
            for (int i = 0; i < 3; i++)
            {
                p.Line(0, 0.36f, tx[i], ty[i], 0.025f, Col("6BBF59"));
                var c = Col(cols[i]);
                for (int k = 0; k < 6; k++)
                {
                    float a = k * Mathf.PI / 3f;
                    p.C(tx[i] + Mathf.Cos(a) * 0.05f, ty[i] + Mathf.Sin(a) * 0.05f, 0.045f, Light(c, 0.3f), c);
                }
                p.C(tx[i], ty[i], 0.035f, Col("FFE27A"), Col("F6B93B"));
            }
            p.Ground(0.24f);
            p.Shape(new Rect(-0.2f, 0, 0.4f, 0.42f), q => Mathf.Min(Ell(q, 0, 0.17f, 0.18f, 0.17f), Box(q, 0, 0.35f, 0.07f, 0.06f, 0.02f)), A(Col("CDE7FA"), 0.95f), A(Col("8FC3EB"), 0.95f));
            p.Line(-0.09f, 0.1f, -0.09f, 0.24f, 0.025f, A(White, 0.7f));
        }

        // ======================= arka planlar =======================

        static void Sky(P p, Color top, Color bottom, Color? mid = null)
        {
            for (int y = 0; y < p.H; y++)
            {
                float t = (y + 0.5f) / p.H;
                Color c = mid.HasValue
                    ? (t < 0.5f ? Color.Lerp(bottom, mid.Value, Mathf.SmoothStep(0, 1, t * 2f)) : Color.Lerp(mid.Value, top, Mathf.SmoothStep(0, 1, (t - 0.5f) * 2f)))
                    : Color.Lerp(bottom, top, Mathf.SmoothStep(0, 1, t));
                for (int x = 0; x < p.W; x++) p.Set(y * p.W + x, c);
            }
        }

        /// <summary>Kıvrımlı tepe şeridi: tepe çizgisinin altını boyar, üstünde güneşte parlayan ince bir kenar.</summary>
        static void Hill(P p, float baseY, float amp, float freq, float phase, Color top, Color bot)
        {
            float px = 1f / p.H;
            for (int x = 0; x < p.W; x++)
            {
                float X = (x + 0.5f) / p.H;
                float hy = baseY + amp * Mathf.Sin(X * freq + phase) + amp * 0.45f * Mathf.Sin(X * freq * 2.3f + phase * 1.7f);
                int ymax = Mathf.Min(p.H - 1, (int)(hy * p.H) + 2);
                for (int y = 0; y <= ymax; y++)
                {
                    float Y = (y + 0.5f) / p.H;
                    float a = Mathf.Clamp01((hy - Y) / px + 0.5f);
                    if (a <= 0) continue;
                    float t = Mathf.Clamp01((hy - Y) / 0.3f);
                    var c = Color.Lerp(top, bot, Mathf.Pow(t, 0.7f));
                    if (hy - Y < 0.007f) c = Color.Lerp(c, White, 0.22f);
                    p.Mix(y * p.W + x, c, a);
                }
            }
        }

        static void SoftCloud(P p, float x, float y, float s, float a)
        {
            p.At(x, y, s, false);
            p.Soft(R(0, 0, 0.5f, 0.3f), q => Mathf.Min(Mathf.Min(Circ(q, -0.22f, 0, 0.16f), Circ(q, 0.02f, 0.07f, 0.22f)), Mathf.Min(Circ(q, 0.24f, 0, 0.15f), Box(q, 0, -0.07f, 0.38f, 0.08f, 0.08f))), A(White, a), 0.02f);
            p.Reset();
        }

        static void Speckle(P p, System.Random r, int count, float y0, float y1, Color[] cols, float size)
        {
            for (int i = 0; i < count; i++)
            {
                p.At(Rf(r, 0, p.A), Rf(r, y0, y1), 1f, false);
                p.Dot(0, 0, size * Rf(r, 0.7f, 1.3f), cols[r.Next(cols.Length)]);
            }
            p.Reset();
        }

        static void Background(P p, string bg, string arg, System.Random r)
        {
            p.Reset();
            switch (bg)
            {
                case "night":
                    Sky(p, Col("1F2A5C"), Col("5A4A86"));
                    for (int i = 0; i < 110; i++)
                    {
                        p.At(Rf(r, 0, p.A), Rf(r, 0.35f, 1f), 1f, false);
                        float s = Rf(r, 0.002f, 0.006f);
                        p.SoftE(0, 0, s * 2.5f, s * 2.5f, A(Col("FFF3B0"), 0.25f), s * 2f);
                        p.Dot(0, 0, s, A(Col("FFF8DC"), Rf(r, 0.6f, 1f)));
                    }
                    p.Reset();
                    Hill(p, 0.42f, 0.04f, 3.1f, 1.3f, Col("3A4580"), Col("2E3668"));
                    Hill(p, 0.33f, 0.03f, 4.2f, 0.2f, Col("45528C"), Col("364174"));
                    Hill(p, 0.26f, 0.015f, 2.2f, 2f, Col("54639A"), Col("3E4A7E"));
                    for (int i = 0; i < 12; i++)
                    {
                        p.At(Rf(r, 0, p.A), Rf(r, 0.05f, 0.45f), 1f, false);
                        p.SoftE(0, 0, 0.012f, 0.012f, A(Col("FFF3A0"), 0.55f), 0.01f);
                        p.Dot(0, 0, 0.003f, Col("FFFBE0"));
                    }
                    p.Reset();
                    break;

                case "under":
                    Sky(p, Col("8EE0EE"), Col("2F7FA8"));
                    for (int i = 0; i < 5; i++)
                    {
                        float x0 = Rf(r, 0, p.A);
                        p.Soft(new Rect(x0 - 0.1f, 0, 0.56f, 1f), q => Mathf.Abs((q.x - x0) + (q.y - 1f) * 0.35f) - 0.03f, A(White, 0.07f), 0.04f);
                    }
                    Hill(p, 0.2f, 0.03f, 3f, 1f, Col("5FA3B8"), Col("4A8FA6"));
                    Hill(p, 0.13f, 0.015f, 2.5f, 0.3f, Col("F6E3B8"), Col("E0C48F"));
                    for (int i = 0; i < 6; i++)
                    {
                        float x = i < 3 ? Rf(r, 0.03f, 0.25f) : Rf(r, p.A - 0.25f, p.A - 0.03f);
                        float h = Rf(r, 0.3f, 0.55f);
                        for (int k = 0; k < 8; k++)
                        {
                            float y = 0.1f + k * h / 8f;
                            float xx = x + Mathf.Sin(k * 0.9f + i) * 0.015f;
                            p.At(xx, y, 1f, false);
                            p.E(0, 0, 0.022f, h / 14f, Col("7CD08A"), Col("3E9A5E"), Mathf.Sin(k + i) * 15f, false);
                        }
                    }
                    p.Reset();
                    Speckle(p, r, 40, 0.02f, 0.12f, new[] { A(Col("FFFFFF"), 0.5f), A(Col("D9B98A"), 0.6f) }, 0.004f);
                    break;

                case "snow":
                    Sky(p, Col("B7D0EF"), Col("F3F0FA"));
                    SoftCloud(p, p.A * 0.2f, 0.85f, 0.35f, 0.7f);
                    SoftCloud(p, p.A * 0.75f, 0.9f, 0.28f, 0.6f);
                    for (int i = 0; i < 7; i++)
                    {
                        p.At(Rf(r, 0, p.A), 0.4f + Rf(r, 0, 0.03f), Rf(r, 0.1f, 0.16f), false);
                        p.Pg(new[] { V(-0.25f, 0), V(0, 0.9f), V(0.25f, 0) }, A(Col("A9B8D6"), 0.8f), A(Col("A9B8D6"), 0.8f), false);
                    }
                    p.Reset();
                    Hill(p, 0.42f, 0.035f, 3.4f, 0.4f, Col("E7EDF8"), Col("D4DDEF"));
                    Hill(p, 0.33f, 0.03f, 4.2f, 1.1f, Col("F6F8FF"), Col("DEE5F4"));
                    Hill(p, 0.26f, 0.015f, 2.2f, 2.3f, Col("FFFFFF"), Col("E9EEF8"));
                    break;

                case "home":
                {
                    bool night = arg == "night";
                    Sky(p, night ? Col("6A5B8E") : Col("FDE6EA"), night ? Col("4F4472") : Col("F7D5DE"));
                    for (float y = 0.32f; y < 1f; y += 0.08f)
                        for (float x = ((int)(y / 0.08f) % 2) * 0.05f; x < p.A; x += 0.1f)
                        {
                            p.At(x, y, 1, false);
                            p.Dot(0, 0, 0.008f, A(White, night ? 0.12f : 0.4f));
                        }
                    // pencere
                    p.At(p.A * 0.78f, 0.6f, 1f, false);
                    p.Bx(0, 0, 0.17f, 0.19f, 0.02f, Col("FFFDF8"), Col("EDE4D6"));
                    p.Fill(R(0, 0, 0.15f, 0.17f), q => Box(q, 0, 0, 0.145f, 0.165f, 0.01f), q => night
                        ? Color.Lerp(Col("4B3F7A"), Col("2A3466"), (q.y + 0.165f) / 0.33f)
                        : Color.Lerp(Col("E3F2FD"), Col("A9D6F5"), (q.y + 0.165f) / 0.33f));
                    if (night)
                    {
                        p.Solid(R(0.06f, 0.08f, 0.05f, 0.05f), q => Mathf.Max(Circ(q, 0.06f, 0.08f, 0.04f), -Circ(q, 0.08f, 0.09f, 0.035f)), Col("FFF6C2"));
                        p.Dot(-0.07f, 0.1f, 0.006f, Col("FFF8DC")); p.Dot(-0.03f, -0.05f, 0.005f, Col("FFF8DC")); p.Dot(0.08f, -0.08f, 0.006f, Col("FFF8DC"));
                    }
                    else p.Soft(R(-0.03f, 0.05f, 0.08f, 0.05f), q => Mathf.Min(Circ(q, -0.06f, 0.04f, 0.035f), Circ(q, -0.01f, 0.06f, 0.045f)), A(White, 0.9f), 0.008f);
                    p.Line(0, -0.17f, 0, 0.17f, 0.014f, Col("FFFDF8"));
                    p.Line(-0.15f, 0, 0.15f, 0, 0.014f, Col("FFFDF8"));
                    p.Shape(R(-0.19f, 0, 0.06f, 0.22f), q => Box(q, -0.19f, 0.01f, 0.05f, 0.21f, 0.04f), Col("FFC8D6"), Col("F5A3B8"));
                    p.Shape(R(0.19f, 0, 0.06f, 0.22f), q => Box(q, 0.19f, 0.01f, 0.05f, 0.21f, 0.04f), Col("FFC8D6"), Col("F5A3B8"));
                    // tablo
                    p.At(p.A * 0.2f, 0.68f, 1f, false);
                    p.Bx(0, 0, 0.1f, 0.08f, 0.015f, Col("E9C46A"), Col("D4A24C"));
                    p.Bx(0, 0, 0.075f, 0.055f, 0.008f, Col("FFFDF8"), Col("F3EDE2"), 0, false);
                    p.Solid(R(0, 0, 0.05f, 0.05f), HeartSdf(0, 0, 0.07f), Col("FF8FAB"));
                    p.Reset();
                    // süpürgelik ve tahta zemin
                    for (int y = 0; y < (int)(0.26f * p.H); y++)
                    {
                        float Y = (y + 0.5f) / p.H;
                        for (int x = 0; x < p.W; x++)
                        {
                            float X = (x + 0.5f) / p.H;
                            Color c;
                            if (Y > 0.235f) c = night ? Col("B9AECF") : Col("FFFDF8");
                            else
                            {
                                int plank = (int)(Y / 0.045f);
                                float seam = Mathf.Abs(Y / 0.045f - Mathf.Round(Y / 0.045f));
                                float grain = Noise(X * 40f + plank * 7f, Y * 4f) * 0.08f;
                                c = Color.Lerp(night ? Col("9C7A68") : Col("E2B58A"), night ? Col("7A5A4E") : Col("C9915E"), (0.235f - Y) / 0.235f * 0.6f + grain);
                                if (seam < 0.04f) c = Dark(c, 0.18f);
                                float joint = Mathf.Abs(Mathf.Repeat(X + plank * 0.37f, 0.6f) - 0.3f);
                                if (joint < 0.002f) c = Dark(c, 0.15f);
                            }
                            p.Set(y * p.W + x, c);
                        }
                    }
                    p.At(p.A * 0.5f, 0.1f, 1f, false);
                    p.E(0, 0, 0.5f, 0.075f, night ? Col("9B8CC6") : Col("D9CCFF"), night ? Col("7F70AE") : Col("C3B5F0"), 0, false);
                    p.Solid(R(0, 0, 0.5f, 0.08f), q => Mathf.Abs(Ell(q, 0, 0, 0.44f, 0.055f)) - 0.004f, A(White, 0.6f));
                    p.Reset();
                    break;
                }

                case "rain":
                    Sky(p, Col("8E9CB9"), Col("CBD3E3"));
                    SoftCloud(p, p.A * 0.2f, 0.88f, 0.5f, 0.55f);
                    SoftCloud(p, p.A * 0.7f, 0.92f, 0.6f, 0.5f);
                    Hill(p, 0.42f, 0.04f, 3.2f, 0.7f, Col("A9BFB0"), Col("93AE9C"));
                    Hill(p, 0.34f, 0.035f, 4.5f, 2.1f, Col("94B89A"), Col("7FA88A"));
                    Hill(p, 0.27f, 0.015f, 2f, 0.4f, Col("86B58A"), Col("6E9C78"));
                    for (int i = 0; i < 4; i++)
                    {
                        p.At(Rf(r, 0.1f, p.A - 0.1f), Rf(r, 0.03f, 0.1f), 1f, false);
                        p.E(0, 0, Rf(r, 0.06f, 0.12f), 0.018f, Col("C9E6F5"), Col("9BC9E6"), 0, false);
                    }
                    p.Reset();
                    break;

                case "sunset":
                    Sky(p, Col("6A5BA8"), Col("FFC27A"), Col("F2899C"));
                    p.At(p.A * 0.7f, 0.38f, 1f, false);
                    p.SoftE(0, 0, 0.3f, 0.3f, A(Col("FFE0A0"), 0.45f), 0.15f);
                    p.C(0, 0, 0.12f, Col("FFE08A"), Col("FFB870"), false);
                    p.Reset();
                    SoftCloud(p, p.A * 0.25f, 0.8f, 0.4f, 0.35f);
                    Hill(p, 0.4f, 0.04f, 3f, 0.6f, Col("B97AA6"), Col("9E6896"));
                    Hill(p, 0.32f, 0.035f, 4f, 2.2f, Col("9561A0"), Col("7C4E86"));
                    Hill(p, 0.25f, 0.02f, 2.2f, 1.1f, Col("7A5590"), Col("5F4275"));
                    break;

                case "sky":
                    Sky(p, Col("86C5EE"), Col("DDF1FF"));
                    SoftCloud(p, p.A * 0.15f, 0.8f, 0.4f, 0.9f);
                    SoftCloud(p, p.A * 0.55f, 0.92f, 0.3f, 0.85f);
                    SoftCloud(p, p.A * 0.9f, 0.62f, 0.35f, 0.8f);
                    SoftCloud(p, p.A * 0.38f, 0.55f, 0.22f, 0.7f);
                    Hill(p, 0.2f, 0.03f, 3f, 0.4f, Col("A8DC8A"), Col("8CCB72"));
                    Hill(p, 0.14f, 0.012f, 2f, 1.6f, Col("8FD16A"), Col("6BBF59"));
                    break;

                default:   // meadow, forest, garden, pond, farm
                {
                    bool autumn = arg == "autumn";
                    Sky(p, autumn ? Col("A9C4E6") : Col("9BD2F2"), autumn ? Col("FBE3C4") : Col("EAF7FF"));
                    SoftCloud(p, p.A * 0.18f, 0.82f, 0.35f, 0.9f);
                    SoftCloud(p, p.A * 0.62f, 0.9f, 0.28f, 0.85f);
                    Hill(p, 0.44f, 0.04f, 3.1f, 0.5f, autumn ? Col("EBD3A6") : Col("CBE9CB"), autumn ? Col("E0C08E") : Col("B5DDB9"));
                    if (bg == "forest")
                    {
                        for (int i = 0; i < 16; i++)
                        {
                            float x = i / 15f * p.A + Rf(r, -0.03f, 0.03f), y = 0.4f + Rf(r, 0, 0.05f), s = Rf(r, 0.09f, 0.14f);
                            p.At(x, y, s, false);
                            p.Bx(0, 0.2f, 0.06f, 0.25f, 0.02f, A(Col("8E9C84"), 1f), A(Col("7E8C76"), 1f), 0, false);
                            p.Shape(R(0, 0.75f, 0.6f, 0.5f), q => Mathf.Min(Mathf.Min(Circ(q, 0, 0.8f, 0.42f), Circ(q, -0.3f, 0.6f, 0.3f)), Circ(q, 0.3f, 0.6f, 0.3f)), Col("9CC9A0"), Col("82B58C"), false);
                        }
                        p.Reset();
                    }
                    if (bg == "farm")
                    {
                        p.At(p.A * 0.86f, 0.36f, 0.3f, false);
                        p.Bx(0, 0.25f, 0.32f, 0.25f, 0.02f, Col("E5776F"), Col("C9534F"));
                        p.Pg(new[] { V(-0.38f, 0.48f), V(0, 0.78f), V(0.38f, 0.48f) }, Col("A8524E"), Col("8C3F3D"));
                        p.Bx(0, 0.16f, 0.13f, 0.16f, 0.01f, Col("FFFDF8"), Col("EDE4D6"));
                        p.Line(-0.12f, 0.02f, 0.12f, 0.3f, 0.03f, Col("C9534F"));
                        p.Line(0.12f, 0.02f, -0.12f, 0.3f, 0.03f, Col("C9534F"));
                        p.Reset();
                    }
                    Hill(p, 0.35f, 0.035f, 4.4f, 1.9f, autumn ? Col("EBC07A") : Col("ACDD8E"), autumn ? Col("D9A562") : Col("8DCB74"));
                    Speckle(p, r, 50, 0.3f, 0.36f, autumn ? new[] { Col("E58A3A"), Col("FFE08A") } : new[] { Col("FFFFFF"), Col("FFC2D3"), Col("FFE27A") }, 0.004f);
                    if (bg == "garden" || bg == "farm")
                    {
                        Color wood = Col("FFFDF8");
                        p.At(0, 0.26f, 1f, false);
                        p.Bx(p.A * 0.5f, 0.075f, p.A * 0.6f, 0.012f, 0.005f, wood, Col("E8DFD0"));
                        p.Bx(p.A * 0.5f, 0.03f, p.A * 0.6f, 0.012f, 0.005f, wood, Col("E8DFD0"));
                        for (float x = 0.03f; x < p.A; x += 0.075f)
                            p.Pg(new[] { V(x - 0.022f, -0.02f), V(x - 0.022f, 0.1f), V(x, 0.125f), V(x + 0.022f, 0.1f), V(x + 0.022f, -0.02f) }, wood, Col("E8DFD0"));
                        p.Reset();
                    }
                    Hill(p, 0.27f, 0.015f, 2.1f, 0.7f, autumn ? Col("D4B866") : Col("98D672"), autumn ? Col("BCA052") : Col("6EBF5C"));
                    if (bg == "pond")
                    {
                        p.At(p.A * 0.55f, 0.14f, 1f, false);
                        float rx = p.A * 0.52f;
                        p.Shape(R(0, 0, rx + 0.02f, 0.14f), q => Ell(q, 0, 0, rx, 0.12f), Col("8FD3F4"), Col("5BB2DE"), false);
                        p.Solid(R(0, 0, rx + 0.03f, 0.15f), q => Mathf.Abs(Ell(q, 0, 0, rx + 0.008f, 0.128f)) - 0.006f, A(Col("E8D9AE"), 0.9f));
                        for (int i = 0; i < 6; i++)
                        {
                            float x = Rf(r, -rx * 0.8f, rx * 0.8f), y = Rf(r, -0.08f, 0.06f);
                            p.Arc(x, y + 0.03f, Rf(r, 0.03f, 0.06f), 240, 300, 0.005f, A(White, 0.6f));
                        }
                        p.Reset();
                        for (int i = 0; i < 2; i++)
                        {
                            float x = i == 0 ? 0.04f : p.A - 0.05f;
                            for (int k = 0; k < 4; k++)
                            {
                                p.At(x + k * 0.018f, 0.12f, 1f, false);
                                float h = 0.18f + (k % 2) * 0.06f;
                                p.Line(0, 0, 0.01f, h, 0.006f, Col("6BBF59"));
                                if (k % 2 == 0) p.Cp(0.01f, h - 0.04f, 0.01f, h + 0.005f, 0.012f, Col("B38B62"), Col("8C6A4A"), false);
                            }
                        }
                        p.Reset();
                    }
                    // çimen tutamları ön planda
                    for (int i = 0; i < 26; i++)
                    {
                        float tx = Rf(r, 0, p.A), ty = Rf(r, 0.0f, 0.24f), ts = Rf(r, 0.03f, 0.05f);
                        bool tf = r.Next(2) == 0;
                        if (bg == "pond")
                        {
                            float ex = (tx - p.A * 0.55f) / (p.A * 0.52f), ey = (ty - 0.14f) / 0.12f;
                            if (ex * ex + ey * ey < 1.15f) continue;   // suyun içinde çimen olmaz
                        }
                        p.At(tx, ty, ts, tf);
                        Tuft(p, autumn ? Col("BCA052") : Col("5FAF56"));
                    }
                    p.Reset();
                    break;
                }
            }
        }

        /// <summary>Karakterlerin üstüne düşen hava: yağmur, kar, deniz ışıltısı, sonbahar yaprakları.</summary>
        static void After(P p, string bg, string arg, System.Random r)
        {
            p.Reset();
            switch (bg)
            {
                case "rain":
                    for (int i = 0; i < 90; i++)
                    {
                        float x = Rf(r, 0, p.A + 0.2f), y = Rf(r, 0.05f, 1f), l = Rf(r, 0.03f, 0.05f);
                        p.Line(x, y, x - l * 0.3f, y - l, 0.004f, A(Col("EAF4FF"), 0.55f));
                    }
                    break;
                case "snow":
                    for (int i = 0; i < 110; i++)
                    {
                        p.At(Rf(r, 0, p.A), Rf(r, 0.02f, 1f), 1f, false);
                        float s = Rf(r, 0.003f, 0.009f);
                        p.SoftE(0, 0, s, s, A(White, 0.9f), s * 0.5f);
                    }
                    break;
                case "under":
                    for (int i = 0; i < 18; i++)
                    {
                        p.At(Rf(r, 0, p.A), Rf(r, 0.15f, 0.95f), Rf(r, 0.012f, 0.03f), false);
                        Bubble(p);
                    }
                    p.Reset();
                    for (int i = 0; i < 4; i++)
                    {
                        float x0 = Rf(r, 0, p.A);
                        p.Soft(new Rect(x0 - 0.06f, 0, 0.42f, 1f), q => Mathf.Abs((q.x - x0) + (q.y - 1f) * 0.3f) - 0.015f, A(White, 0.06f), 0.03f);
                    }
                    break;
                default:
                    if (arg == "autumn")
                        for (int i = 0; i < 14; i++)
                        {
                            p.At(Rf(r, 0, p.A), Rf(r, 0.3f, 0.95f), 1f, false);
                            p.E(0, 0, 0.014f, 0.007f, Col("FFB25C"), Col("E5793A"), Rf(r, 0, 180), true);
                        }
                    if (arg == "spring" || bg == "garden")
                        for (int i = 0; i < 10; i++)
                        {
                            p.At(Rf(r, 0, p.A), Rf(r, 0.35f, 0.95f), 1f, false);
                            p.E(0, 0, 0.009f, 0.005f, A(Col("FFC2D3"), 0.9f), A(Col("FF9FB5"), 0.9f), Rf(r, 0, 180), false);
                        }
                    break;
            }
            p.Reset();
        }
    }
}
