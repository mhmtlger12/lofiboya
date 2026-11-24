using System;
using System.Collections.Generic;
using UnityEngine;

namespace LofiBoya
{
    /// <summary>
    /// Öğrenme oyunlarının resimli kartları, Pofuduk'la aynı çizim diliyle kodla çizilir:
    /// koyu mor kontur, yukarıdan aşağı yumuşak renk geçişi, beyaz parlama ve altta hafif gölge.
    ///  • Zıtlar: sıcak / soğuk içecek, dolu / boş kavanoz, açık / kapalı hediye kutusu
    ///  • Sıralama: saksıda büyüyen çiçek, çatlayan yumurta, eriyen dondurma (3'er aşama)
    ///  • Mevsimler: dört mevsimde aynı ağaç ve mevsim ipuçları (kar, güneş, dökülen yaprak, açan çiçek)
    /// Hepsi 256×256, kare; ilk istendiğinde bir kez üretilip saklanır.
    /// </summary>
    public static class LearnArt
    {
        const int Size = 256;
        static readonly Color Ink = Theme.Hex("5E4B6E");
        const float Line = 0.016f;   // kontur kalınlığı (kutu genişliğine oranla)

        static readonly Dictionary<string, Sprite> _cache = new();

        static Sprite Get(string key, Action<Painter> draw)
        {
            if (_cache.TryGetValue(key, out var s) && s != null) return s;
            var cv = new Painter { w = Size, h = Size, px = new Color[Size * Size], aa = 1.3f / Size };
            draw(cv);
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = key };
            tex.SetPixels(cv.px);
            tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100);
            _cache[key] = s;
            return s;
        }

        // ================= Zıtlar =================

        public static Sprite HotCup => Get("hot", DrawHotCup);
        public static Sprite ColdGlass => Get("cold", DrawColdGlass);
        public static Sprite JarFull => Get("jarFull", c => DrawJar(c, true));
        public static Sprite JarEmpty => Get("jarEmpty", c => DrawJar(c, false));
        public static Sprite BoxOpen => Get("boxOpen", c => DrawBox(c, true));
        public static Sprite BoxClosed => Get("boxClosed", c => DrawBox(c, false));

        // ================= Sıralama =================

        /// <summary>Sıralama dizileri: her biri 3 aşama (önce → sonra).</summary>
        public static Sprite Plant(int stage) => Get("plant" + stage, c => DrawPlant(c, stage));
        public static Sprite Egg(int stage) => Get("egg" + stage, c => DrawEgg(c, stage));
        public static Sprite IceCream(int stage) => Get("ice" + stage, c => DrawIceCream(c, stage));

        // ================= Mevsimler =================

        /// <summary>0 kış, 1 ilkbahar, 2 yaz, 3 sonbahar.</summary>
        public static Sprite Tree(int season) => Get("tree" + season, c => DrawTree(c, season));
        /// <summary>Mevsimin havası: 0 kar yağan bulut, 1 açan çiçekler, 2 gülen güneş, 3 dökülen yapraklar.</summary>
        public static Sprite Weather(int season) => Get("weather" + season, c => DrawWeather(c, season));

        // ---------------- çizim altyapısı ----------------

        class Painter
        {
            public int w, h;
            public Color[] px;
            public float aa;
            Vector2 P(int x, int y) => new((x + 0.5f) / w, (y + 0.5f) / w);

            public void Fill(Func<Vector2, float> sdf, Func<Vector2, Color> col)
            {
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var p = P(x, y);
                    float a = Mathf.Clamp01(0.5f - sdf(p) / aa);
                    if (a <= 0) continue;
                    var c = col(p);
                    a *= c.a;
                    int i = y * w + x;
                    var d = px[i];
                    float oa = a + d.a * (1 - a);
                    if (oa <= 0) continue;
                    px[i] = new Color((c.r * a + d.r * d.a * (1 - a)) / oa, (c.g * a + d.g * d.a * (1 - a)) / oa, (c.b * a + d.b * d.a * (1 - a)) / oa, oa);
                }
            }

            public void Fill(Func<Vector2, float> sdf, Color c) => Fill(sdf, _ => c);

            /// <summary>Konturlu, yukarıdan aşağı renk geçişli parça (çıkartma görünümü).</summary>
            public void Part(Func<Vector2, float> sdf, Color top, Color bottom, float y0 = 0f, float y1 = 1f)
            {
                Fill(p => sdf(p) - Line, Ink);
                Fill(sdf, p => Color.Lerp(bottom, top, Mathf.SmoothStep(y0, y1, p.y)));
            }

            public void Part(Func<Vector2, float> sdf, Color c) => Part(sdf, c, c);

            /// <summary>Konturlu çizgi (yay, sap, dal): kalınlık r.</summary>
            public void Stroke(Func<Vector2, float> sdf, Color c)
            {
                Fill(p => sdf(p) - Line, Ink);
                Fill(sdf, c);
            }

            public void Shine(float x, float y, float rx, float ry, float alpha = 0.75f) =>
                Fill(p => Ellipse(p, x, y, rx, ry), new Color(1, 1, 1, alpha));

            public void Shadow(float x, float y, float rx, float ry) =>
                Fill(p => Ellipse(p, x, y, rx, ry), new Color(0.37f, 0.29f, 0.43f, 0.16f));

            /// <summary>Sevimli yüz: iki göz, gülümseme, pembe yanaklar.</summary>
            public void Face(float cx, float cy, float s)
            {
                foreach (float dx in new[] { -0.07f, 0.07f })
                {
                    Fill(p => Ellipse(p, cx + dx * s, cy, 0.022f * s, 0.03f * s), Ink);
                    Fill(p => Circle(p, cx + dx * s - 0.007f * s, cy + 0.01f * s, 0.008f * s), Color.white);
                    Fill(p => Ellipse(p, cx + dx * s * 1.75f, cy - 0.04f * s, 0.032f * s, 0.02f * s), new Color(1f, 0.55f, 0.65f, 0.7f));
                }
                Fill(p => Mathf.Max(Mathf.Abs(Circle(p, cx, cy - 0.02f * s, 0.035f * s)) - 0.007f * s, p.y - (cy - 0.02f * s)), Ink);
            }
        }

        // ---------------- SDF yardımcıları ----------------

        static float Circle(Vector2 p, float x, float y, float r) => (p - new Vector2(x, y)).magnitude - r;

        static float Ellipse(Vector2 p, float x, float y, float rx, float ry)
        {
            // iq'nun elips yaklaşımı: uzun-ince elipslerde de kontur her yerde eşit kalınlıkta
            var d = p - new Vector2(x, y);
            float k0 = new Vector2(d.x / rx, d.y / ry).magnitude;
            float k1 = new Vector2(d.x / (rx * rx), d.y / (ry * ry)).magnitude;
            return k1 > 1e-6f ? k0 * (k0 - 1f) / k1 : -Mathf.Min(rx, ry);
        }

        /// <summary>Döndürülmüş elips (yaprak, taç yaprağı). deg: saat yönünün tersine.</summary>
        static float EllipseRot(Vector2 p, float x, float y, float rx, float ry, float deg)
        {
            float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            var d = p - new Vector2(x, y);
            return Ellipse(new Vector2(d.x * c + d.y * s, -d.x * s + d.y * c), 0, 0, rx, ry);
        }

        static float Box(Vector2 p, float x, float y, float hx, float hy, float r)
        {
            var q = new Vector2(Mathf.Abs(p.x - x), Mathf.Abs(p.y - y)) - new Vector2(hx, hy) + new Vector2(r, r);
            return new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - r;
        }

        static float BoxRot(Vector2 p, float x, float y, float hx, float hy, float r, float deg)
        {
            float a = deg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            var d = p - new Vector2(x, y);
            return Box(new Vector2(d.x * c + d.y * s, -d.x * s + d.y * c), 0, 0, hx, hy, r);
        }

        static float Seg(Vector2 p, float ax, float ay, float bx, float by, float r)
        {
            var a = new Vector2(ax, ay);
            var pa = p - a;
            var ba = new Vector2(bx, by) - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - r;
        }

        static float Polygon(Vector2 p, Vector2[] v)
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

        static Vector2[] V(params float[] xy)
        {
            var v = new Vector2[xy.Length / 2];
            for (int i = 0; i < v.Length; i++) v[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
            return v;
        }

        static float SmoothMin(float a, float b, float k)
        {
            float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
            return Mathf.Lerp(b, a, h) - k * h * (1 - h);
        }

        /// <summary>Dalgalı buhar / rüzgâr çizgisi: x0 çevresinde yukarı kıvrılır.</summary>
        static float Wave(Vector2 p, float x0, float y0, float y1, float amp, float r)
        {
            if (p.y < y0 - r || p.y > y1 + r) return Mathf.Min(Circle(p, x0, y0, r), Circle(p, x0 + amp * Mathf.Sin((y1 - y0) * 18f), y1, r));
            float t = Mathf.Clamp(p.y, y0, y1);
            float cx = x0 + amp * Mathf.Sin((t - y0) * 18f);
            return new Vector2(p.x - cx, p.y - t).magnitude * 0.8f - r;
        }

        static float Snowflake(Vector2 p, float x, float y, float s)
        {
            float d = 10f;
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI / 3f;
                float dx = Mathf.Cos(a) * s, dy = Mathf.Sin(a) * s;
                d = Mathf.Min(d, Seg(p, x - dx, y - dy, x + dx, y + dy, s * 0.16f));
            }
            return d;
        }

        static readonly Color White = Color.white;

        // ================= çizimler: zıtlar =================

        static void DrawHotCup(Painter c)
        {
            c.Shadow(0.47f, 0.13f, 0.3f, 0.04f);
            // buhar: üç kıvrımlı çizgi
            foreach (float x in new[] { 0.38f, 0.47f, 0.56f })
                c.Fill(p => Wave(p, x, 0.66f, 0.86f, 0.02f, 0.012f), Theme.Hex("C3B5F0").WithAlpha(0.85f));
            // kulp
            c.Stroke(p => Mathf.Abs(Circle(p, 0.71f, 0.38f, 0.085f)) - 0.025f, Theme.Hex("F07C8C"));
            // kupa
            c.Part(p => Box(p, 0.47f, 0.37f, 0.22f, 0.22f, 0.07f), Theme.Hex("FF9EAA"), Theme.Hex("F07C8C"));
            // sıcak çikolata
            c.Fill(p => Ellipse(p, 0.47f, 0.555f, 0.18f, 0.035f), Theme.Hex("8D5524"));
            c.Fill(p => Ellipse(p, 0.43f, 0.56f, 0.06f, 0.012f), new Color(1, 1, 1, 0.35f));
            // kalp süsü
            c.Fill(p => SmoothMin(Circle(p, 0.43f, 0.38f, 0.04f), Circle(p, 0.51f, 0.38f, 0.04f), 0.01f) , Color.white.WithAlpha(0.9f));
            var _poly1 = V(0.395f, 0.37f, 0.545f, 0.37f, 0.47f, 0.29f);
            c.Fill(p => Polygon(p, _poly1), Color.white.WithAlpha(0.9f));
            c.Shine(0.32f, 0.47f, 0.025f, 0.07f, 0.55f);
        }

        static void DrawColdGlass(Painter c)
        {
            c.Shadow(0.5f, 0.12f, 0.26f, 0.04f);
            var glass = V(0.32f, 0.15f, 0.68f, 0.15f, 0.73f, 0.78f, 0.27f, 0.78f);
            // pipet (bardağın arkasında)
            c.Stroke(p => Seg(p, 0.58f, 0.5f, 0.66f, 0.92f, 0.022f), Theme.Hex("FF8FAB"));
            c.Fill(p => Polygon(p, glass) - Line, Ink);
            c.Fill(p => Polygon(p, glass), Theme.Hex("EAF6FF"));
            // soğuk su
            c.Fill(p => Mathf.Max(Polygon(p, glass) + 0.012f, p.y - 0.6f), p => Color.Lerp(Theme.Hex("7CC4F2"), Theme.Hex("B9E3FB"), p.y / 0.6f));
            // buz küpleri
            c.Part(p => BoxRot(p, 0.43f, 0.55f, 0.075f, 0.075f, 0.02f, 14f), Theme.Hex("FFFFFF"), Theme.Hex("D6EEFF"));
            c.Part(p => BoxRot(p, 0.57f, 0.48f, 0.07f, 0.07f, 0.02f, -10f), Theme.Hex("FFFFFF"), Theme.Hex("D6EEFF"));
            c.Part(p => BoxRot(p, 0.47f, 0.34f, 0.065f, 0.065f, 0.02f, 25f), Theme.Hex("FFFFFF"), Theme.Hex("D6EEFF"));
            c.Shine(0.34f, 0.5f, 0.018f, 0.16f, 0.7f);
            // kar tanesi
            c.Fill(p => Snowflake(p, 0.18f, 0.82f, 0.06f) - Line * 0.6f, Ink);
            c.Fill(p => Snowflake(p, 0.18f, 0.82f, 0.06f), Theme.Hex("A9D6F5"));
            c.Fill(p => Snowflake(p, 0.84f, 0.66f, 0.045f) - Line * 0.6f, Ink);
            c.Fill(p => Snowflake(p, 0.84f, 0.66f, 0.045f), Theme.Hex("A9D6F5"));
        }

        static void DrawJar(Painter c, bool full)
        {
            c.Shadow(0.5f, 0.11f, 0.3f, 0.04f);
            float Jar(Vector2 p) => Box(p, 0.5f, 0.42f, 0.25f, 0.29f, 0.09f);
            c.Fill(p => Jar(p) - Line, Ink);
            c.Fill(Jar, Theme.Hex("EAF6FF").WithAlpha(0.95f));
            if (full)
            {
                // renkli şekerler
                var cols = new[] { "FF8FAB", "FFD166", "8FD6A8", "A0C4FF", "C3B5F0", "FFB38A" };
                var rnd = new System.Random(7);
                for (int row = 0; row < 5; row++)
                for (int k = 0; k < 4; k++)
                {
                    float x = 0.33f + k * 0.115f + (row % 2) * 0.055f, y = 0.2f + row * 0.1f;
                    if (x > 0.7f) continue;
                    var col = Theme.Hex(cols[rnd.Next(cols.Length)]);
                    c.Fill(p => Mathf.Max(Circle(p, x, y, 0.052f), Jar(p) + 0.012f) - Line * 0.5f, Ink);
                    c.Fill(p => Mathf.Max(Circle(p, x, y, 0.052f), Jar(p) + 0.012f), col);
                    c.Fill(p => Mathf.Max(Circle(p, x - 0.015f, y + 0.017f, 0.014f), Jar(p) + 0.012f), new Color(1, 1, 1, 0.6f));
                }
            }
            c.Shine(0.33f, 0.48f, 0.02f, 0.14f, 0.7f);
            // kapak
            c.Part(p => Box(p, 0.5f, 0.75f, 0.2f, 0.055f, 0.03f), Theme.Hex("C3B5F0"), Theme.Hex("9F8FD9"));
            c.Part(p => Box(p, 0.5f, 0.83f, 0.08f, 0.03f, 0.025f), Theme.Hex("C3B5F0"), Theme.Hex("9F8FD9"));
        }

        static void DrawBox(Painter c, bool open)
        {
            c.Shadow(0.5f, 0.11f, 0.33f, 0.04f);
            var body = new Func<Vector2, float>(p => Box(p, 0.5f, 0.32f, 0.27f, 0.19f, 0.03f));
            if (open)
            {
                // kutudan fırlayan yıldızlar
                c.Part(p => Circle(p, 0.36f, 0.72f, 0.045f), Theme.Hex("FFE58A"), Theme.Hex("FFC93C"));
                c.Part(p => Circle(p, 0.62f, 0.8f, 0.035f), Theme.Hex("FFC6FF"), Theme.Hex("FF8FAB"));
                c.Part(p => Circle(p, 0.52f, 0.66f, 0.05f), Theme.Hex("B9FBC0"), Theme.Hex("8FD6A8"));
                c.Fill(p => Snowflake(p, 0.73f, 0.66f, 0.035f) - Line * 0.6f, Ink);
                c.Fill(p => Snowflake(p, 0.73f, 0.66f, 0.035f), Theme.Hex("FFD166"));
            }
            c.Part(body, Theme.Hex("A9D6F5"), Theme.Hex("7FB5E6"));
            if (open)
            {
                // kutunun ağzı: içi karanlık
                c.Part(p => Box(p, 0.5f, 0.51f, 0.27f, 0.035f, 0.02f), Theme.Hex("5E7FA8"));
            }
            // dikey kurdele
            c.Fill(p => Mathf.Max(Box(p, 0.5f, 0.32f, 0.045f, 0.19f, 0.0f), body(p)), Theme.Hex("FF8FAB"));
            if (open)
            {
                // kapak havada, eğik
                c.Part(p => BoxRot(p, 0.27f, 0.72f, 0.2f, 0.05f, 0.025f, 35f), Theme.Hex("A9D6F5"), Theme.Hex("7FB5E6"));
                c.Fill(p => Mathf.Max(BoxRot(p, 0.27f, 0.72f, 0.045f, 0.05f, 0f, 35f), BoxRot(p, 0.27f, 0.72f, 0.2f, 0.05f, 0.025f, 35f)), Theme.Hex("FF8FAB"));
            }
            else
            {
                c.Part(p => Box(p, 0.5f, 0.535f, 0.3f, 0.055f, 0.025f), Theme.Hex("A9D6F5"), Theme.Hex("7FB5E6"));
                c.Fill(p => Mathf.Max(Box(p, 0.5f, 0.535f, 0.045f, 0.055f, 0f), Box(p, 0.5f, 0.535f, 0.3f, 0.055f, 0.025f)), Theme.Hex("FF8FAB"));
                // fiyonk
                c.Part(p => EllipseRot(p, 0.42f, 0.65f, 0.08f, 0.045f, 25f), Theme.Hex("FFB3C6"), Theme.Hex("FF8FAB"));
                c.Part(p => EllipseRot(p, 0.58f, 0.65f, 0.08f, 0.045f, -25f), Theme.Hex("FFB3C6"), Theme.Hex("FF8FAB"));
                c.Part(p => Circle(p, 0.5f, 0.625f, 0.035f), Theme.Hex("FF8FAB"));
            }
            c.Shine(0.31f, 0.36f, 0.02f, 0.08f, 0.6f);
        }

        // ================= çizimler: sıralama =================

        static void Pot(Painter c)
        {
            c.Shadow(0.5f, 0.07f, 0.24f, 0.035f);
            var _poly2 = V(0.35f, 0.09f, 0.65f, 0.09f, 0.7f, 0.33f, 0.3f, 0.33f);
            c.Part(p => Polygon(p, _poly2), Theme.Hex("F4A98A"), Theme.Hex("D9805E"));
            c.Part(p => Box(p, 0.5f, 0.35f, 0.23f, 0.045f, 0.025f), Theme.Hex("F4A98A"), Theme.Hex("E08E6C"));
            c.Fill(p => Ellipse(p, 0.5f, 0.395f, 0.19f, 0.022f), Theme.Hex("6F4518"));
            c.Shine(0.39f, 0.22f, 0.018f, 0.06f, 0.45f);
        }

        static void DrawPlant(Painter c, int stage)
        {
            if (stage == 0)
            {
                Pot(c);
                // toprağın üstünde tohum ve yağmur damlası
                c.Part(p => EllipseRot(p, 0.5f, 0.43f, 0.035f, 0.024f, 20f), Theme.Hex("C68642"), Theme.Hex("8D5524"));
                var _poly3 = V(0.59f, 0.64f, 0.65f, 0.64f, 0.62f, 0.72f);
                c.Part(p => SmoothMin(Circle(p, 0.62f, 0.62f, 0.035f), Polygon(p, _poly3), 0.01f), Theme.Hex("B9E3FB"), Theme.Hex("7CC4F2"));
                var _poly4 = V(0.375f, 0.72f, 0.425f, 0.72f, 0.4f, 0.79f);
                c.Part(p => SmoothMin(Circle(p, 0.4f, 0.7f, 0.03f), Polygon(p, _poly4), 0.01f), Theme.Hex("B9E3FB"), Theme.Hex("7CC4F2"));
                return;
            }
            float top = stage == 1 ? 0.56f : 0.66f;
            c.Stroke(p => Seg(p, 0.5f, 0.4f, 0.5f, top, 0.016f), Theme.Hex("6BBF6A"));
            float ls = stage == 1 ? 0.75f : 1f;
            c.Part(p => EllipseRot(p, 0.5f - 0.075f * ls, 0.5f + (stage == 1 ? 0.0f : 0.02f), 0.075f * ls, 0.035f * ls, 25f), Theme.Hex("A8E6A1"), Theme.Hex("6BBF6A"));
            c.Part(p => EllipseRot(p, 0.5f + 0.075f * ls, 0.53f + (stage == 1 ? 0.0f : 0.02f), 0.075f * ls, 0.035f * ls, -25f), Theme.Hex("A8E6A1"), Theme.Hex("6BBF6A"));
            Pot(c);
            if (stage == 2)
            {
                // çiçek: beş taç yaprak + yüzlü göbek
                for (int i = 0; i < 5; i++)
                {
                    float a = Mathf.PI / 2 + i * Mathf.PI * 2 / 5;
                    float px = 0.5f + Mathf.Cos(a) * 0.095f, py = 0.76f + Mathf.Sin(a) * 0.095f;
                    c.Part(p => Circle(p, px, py, 0.075f), Theme.Hex("FFC6FF"), Theme.Hex("FF8FAB"));
                }
                c.Part(p => Circle(p, 0.5f, 0.76f, 0.07f), Theme.Hex("FFE58A"), Theme.Hex("FFC93C"));
                c.Face(0.5f, 0.765f, 0.55f);
            }
        }

        static float EggSdf(Vector2 p)
        {
            // altı geniş, üstü dar yumurta
            float k = Mathf.Lerp(1.08f, 0.82f, Mathf.InverseLerp(0.12f, 0.8f, p.y));
            return Ellipse(new Vector2(0.5f + (p.x - 0.5f) / k, p.y), 0.5f, 0.44f, 0.21f, 0.3f);
        }

        static float Zigzag(Vector2 p, float y)
        {
            // yatay zikzak çizgisinin altı negatif (kırık kabuğun kenarı)
            float t = Mathf.Repeat(p.x * 14f, 2f);
            float edge = y + (t < 1f ? t : 2f - t) * 0.045f;
            return p.y - edge;
        }

        static void DrawEgg(Painter c, int stage)
        {
            c.Shadow(0.5f, 0.12f, 0.24f, 0.035f);
            var shellTop = Theme.Hex("FFFDF8");
            var shellBottom = Theme.Hex("F1E3D3");
            if (stage < 2)
            {
                c.Part(EggSdf, shellTop, shellBottom);
                // benekler
                c.Fill(p => Mathf.Max(Circle(p, 0.42f, 0.56f, 0.028f), EggSdf(p)), Theme.Hex("C3B5F0").WithAlpha(0.7f));
                c.Fill(p => Mathf.Max(Circle(p, 0.6f, 0.4f, 0.022f), EggSdf(p)), Theme.Hex("A9D6F5").WithAlpha(0.7f));
                c.Fill(p => Mathf.Max(Circle(p, 0.48f, 0.27f, 0.02f), EggSdf(p)), Theme.Hex("FFC6FF").WithAlpha(0.8f));
                c.Shine(0.4f, 0.6f, 0.03f, 0.07f, 0.8f);
                if (stage == 1)
                {
                    // çatlak
                    var crack = V(0.31f, 0.47f, 0.38f, 0.43f, 0.43f, 0.5f, 0.5f, 0.42f, 0.56f, 0.49f, 0.62f, 0.43f, 0.69f, 0.47f);
                    for (int i = 0; i + 1 < crack.Length; i++)
                    {
                        var a = crack[i]; var b = crack[i + 1];
                        c.Fill(p => Mathf.Max(Seg(p, a.x, a.y, b.x, b.y, 0.009f), EggSdf(p) + 0.005f), Ink);
                    }
                }
                return;
            }
            // civciv: gövde, kanat, gaga; altta kırık kabuk, başında kabuk şapka
            c.Part(p => Circle(p, 0.5f, 0.52f, 0.2f), Theme.Hex("FFE58A"), Theme.Hex("FFC93C"));
            c.Part(p => EllipseRot(p, 0.31f, 0.47f, 0.06f, 0.035f, 30f), Theme.Hex("FFD166"), Theme.Hex("F4B740"));
            c.Part(p => EllipseRot(p, 0.69f, 0.47f, 0.06f, 0.035f, -30f), Theme.Hex("FFD166"), Theme.Hex("F4B740"));
            c.Face(0.5f, 0.56f, 1.1f);
            var _poly5 = V(0.47f, 0.5f, 0.53f, 0.5f, 0.5f, 0.465f);
            c.Part(p => Polygon(p, _poly5), Theme.Hex("FFB38A"), Theme.Hex("F48C5A"));
            c.Part(p => Mathf.Max(EggSdf(p), Zigzag(p, 0.38f)), shellTop, shellBottom);
            c.Part(p => Mathf.Max(EggSdf(new Vector2(p.x, p.y - 0.06f)), -Zigzag(p, 0.69f)), shellTop, shellBottom);
            c.Shine(0.42f, 0.66f, 0.03f, 0.03f, 0.7f);
        }

        static void Cone(Painter c)
        {
            var cone = V(0.37f, 0.48f, 0.63f, 0.48f, 0.5f, 0.1f);
            c.Part(p => Polygon(p, cone) - 0.01f, Theme.Hex("F6D29A"), Theme.Hex("E0A95C"));
            for (int i = 0; i < 4; i++)
            {
                float o = -0.12f + i * 0.08f;
                c.Fill(p => Mathf.Max(Seg(p, 0.5f + o, 0.48f, 0.5f + o + 0.18f, 0.12f, 0.006f), Polygon(p, cone)), Theme.Hex("C98A3E").WithAlpha(0.7f));
                c.Fill(p => Mathf.Max(Seg(p, 0.5f + o, 0.48f, 0.5f + o - 0.18f, 0.12f, 0.006f), Polygon(p, cone)), Theme.Hex("C98A3E").WithAlpha(0.7f));
            }
        }

        static void DrawIceCream(Painter c, int stage)
        {
            if (stage == 2)
            {
                // tamamen eridi: koca bir pembe-yeşil gölcük, külah yerde
                c.Part(p => SmoothMin(Ellipse(p, 0.5f, 0.17f, 0.36f, 0.075f), Ellipse(p, 0.32f, 0.2f, 0.12f, 0.06f), 0.04f), Theme.Hex("FFC6E0"), Theme.Hex("FF9EBB"));
                c.Fill(p => Ellipse(p, 0.62f, 0.16f, 0.14f, 0.04f), Theme.Hex("B9FBC0"));
                c.Shine(0.4f, 0.2f, 0.06f, 0.015f, 0.7f);
            }
            else c.Shadow(0.5f, 0.08f, 0.2f, 0.03f);
            Cone(c);
            var pink = (Theme.Hex("FFC6E0"), Theme.Hex("FF8FAB"));
            var mint = (Theme.Hex("D2FBE0"), Theme.Hex("8FD6A8"));
            if (stage == 0)
            {
                c.Part(p => Circle(p, 0.5f, 0.56f, 0.15f), pink.Item1, pink.Item2);
                c.Part(p => Circle(p, 0.5f, 0.74f, 0.12f), mint.Item1, mint.Item2);
                c.Part(p => Circle(p, 0.5f, 0.88f, 0.035f), Theme.Hex("FF6B81"), Theme.Hex("E8455E"));
                c.Face(0.5f, 0.57f, 1f);
                c.Shine(0.43f, 0.79f, 0.03f, 0.02f);
            }
            else if (stage == 1)
            {
                // eriyor: top basık, damlalar külaha akıyor
                c.Part(p => SmoothMin(Ellipse(p, 0.5f, 0.53f, 0.17f, 0.1f),
                    Mathf.Min(Box(p, 0.4f, 0.42f, 0.025f, 0.08f, 0.025f), Box(p, 0.58f, 0.4f, 0.025f, 0.1f, 0.025f)), 0.03f), pink.Item1, pink.Item2, 0.3f, 0.6f);
                c.Part(p => Ellipse(p, 0.5f, 0.64f, 0.11f, 0.065f), mint.Item1, mint.Item2);
                c.Part(p => Circle(p, 0.57f, 0.7f, 0.03f), Theme.Hex("FF6B81"), Theme.Hex("E8455E"));
                c.Fill(p => Ellipse(p, 0.75f, 0.12f, 0.06f, 0.02f), Theme.Hex("FFB3C6"));
                c.Face(0.5f, 0.54f, 0.9f);
            }
        }

        // ================= çizimler: mevsimler =================

        static void Trunk(Painter c, bool branches)
        {
            var _poly6 = V(0.45f, 0.12f, 0.55f, 0.12f, 0.53f, 0.5f, 0.47f, 0.5f);
            c.Part(p => Polygon(p, _poly6), Theme.Hex("B5835A"), Theme.Hex("8D5E3C"));
            if (!branches) return;
            c.Stroke(p => Seg(p, 0.5f, 0.42f, 0.3f, 0.66f, 0.018f), Theme.Hex("A7744E"));
            c.Stroke(p => Seg(p, 0.5f, 0.46f, 0.7f, 0.7f, 0.018f), Theme.Hex("A7744E"));
            c.Stroke(p => Seg(p, 0.5f, 0.48f, 0.5f, 0.8f, 0.018f), Theme.Hex("A7744E"));
            c.Stroke(p => Seg(p, 0.36f, 0.59f, 0.27f, 0.58f, 0.012f), Theme.Hex("A7744E"));
            c.Stroke(p => Seg(p, 0.64f, 0.63f, 0.76f, 0.62f, 0.012f), Theme.Hex("A7744E"));
        }

        static float Canopy(Vector2 p, float s)
        {
            float d = Circle(p, 0.5f, 0.62f, 0.2f * s);
            d = SmoothMin(d, Circle(p, 0.33f, 0.56f, 0.14f * s), 0.04f);
            d = SmoothMin(d, Circle(p, 0.67f, 0.56f, 0.14f * s), 0.04f);
            d = SmoothMin(d, Circle(p, 0.4f, 0.75f, 0.14f * s), 0.04f);
            d = SmoothMin(d, Circle(p, 0.61f, 0.76f, 0.14f * s), 0.04f);
            return d;
        }

        static void DrawTree(Painter c, int season)
        {
            // zemin
            var ground = season switch
            {
                0 => (Theme.Hex("FFFFFF"), Theme.Hex("DCEBF7")),
                3 => (Theme.Hex("F6D29A"), Theme.Hex("E7B16C")),
                _ => (Theme.Hex("B9F0B0"), Theme.Hex("8FD6A8")),
            };
            c.Part(p => Ellipse(p, 0.5f, 0.12f, 0.38f, 0.06f), ground.Item1, ground.Item2);
            switch (season)
            {
                case 0: // kış: çıplak dallar, üstünde kar, kar taneleri
                    Trunk(c, true);
                    c.Part(p => Ellipse(p, 0.3f, 0.675f, 0.04f, 0.017f), White);
                    c.Part(p => Ellipse(p, 0.7f, 0.715f, 0.04f, 0.017f), White);
                    c.Part(p => Ellipse(p, 0.5f, 0.815f, 0.035f, 0.017f), White);
                    foreach (var (x, y, s) in new[] { (0.16f, 0.82f, 0.04f), (0.85f, 0.86f, 0.035f), (0.2f, 0.42f, 0.03f), (0.82f, 0.4f, 0.04f), (0.62f, 0.92f, 0.03f) })
                    {
                        c.Fill(p => Snowflake(p, x, y, s) - Line * 0.6f, Ink);
                        c.Fill(p => Snowflake(p, x, y, s), Theme.Hex("A9D6F5"));
                    }
                    // kardan küçük tepecik
                    c.Part(p => Ellipse(p, 0.3f, 0.16f, 0.1f, 0.05f), White, Theme.Hex("E6F1FB"));
                    break;
                case 1: // ilkbahar: açık yeşil taç, pembe çiçekler
                    Trunk(c, false);
                    c.Part(p => Canopy(p, 0.95f), Theme.Hex("C9F2C1"), Theme.Hex("8FD6A8"));
                    foreach (var (x, y) in new[] { (0.38f, 0.7f), (0.55f, 0.79f), (0.64f, 0.6f), (0.44f, 0.55f), (0.3f, 0.58f), (0.68f, 0.74f) })
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            float a = i * Mathf.PI * 2 / 5;
                            float fx = x + Mathf.Cos(a) * 0.022f, fy = y + Mathf.Sin(a) * 0.022f;
                            c.Fill(p => Circle(p, fx, fy, 0.019f), Theme.Hex("FF9EBB"));
                        }
                        c.Fill(p => Circle(p, x, y, 0.012f), Theme.Hex("FFE58A"));
                    }
                    c.Shine(0.4f, 0.8f, 0.05f, 0.025f, 0.5f);
                    break;
                case 2: // yaz: koyu yeşil gür taç, kırmızı elmalar, güneş
                    c.Part(p => Circle(p, 0.84f, 0.86f, 0.08f), Theme.Hex("FFE58A"), Theme.Hex("FFC93C"));
                    Trunk(c, false);
                    c.Part(p => Canopy(p, 1.05f), Theme.Hex("8FD6A8"), Theme.Hex("4FA66B"));
                    foreach (var (x, y) in new[] { (0.36f, 0.6f), (0.58f, 0.77f), (0.66f, 0.56f), (0.47f, 0.66f), (0.33f, 0.74f) })
                        c.Part(p => Circle(p, x, y, 0.032f), Theme.Hex("FF8F8F"), Theme.Hex("E8455E"));
                    c.Shine(0.4f, 0.82f, 0.05f, 0.025f, 0.45f);
                    break;
                default: // sonbahar: turuncu-kırmızı seyrek taç, düşen yapraklar
                    Trunk(c, true);
                    c.Part(p => Canopy(p, 0.92f), Theme.Hex("FFC07A"), Theme.Hex("F28C3A"));
                    foreach (var (x, y, col) in new[] { (0.38f, 0.7f, "E86A4A"), (0.56f, 0.78f, "FFD27A"), (0.66f, 0.6f, "E86A4A"), (0.45f, 0.56f, "FFD27A"), (0.3f, 0.58f, "C0503A"), (0.62f, 0.7f, "FFE58A") })
                        c.Fill(p => EllipseRot(p, x, y, 0.028f, 0.017f, 35f), Theme.Hex(col));
                    c.Shine(0.4f, 0.8f, 0.05f, 0.025f, 0.45f);
                    foreach (var (x, y, r) in new[] { (0.2f, 0.4f, 30f), (0.8f, 0.34f, -40f), (0.72f, 0.18f, 10f), (0.26f, 0.2f, -20f), (0.86f, 0.55f, 60f) })
                        c.Part(p => EllipseRot(p, x, y, 0.04f, 0.022f, r), Theme.Hex("FFB35A"), Theme.Hex("E86A4A"));
                    break;
            }
        }

        static void DrawWeather(Painter c, int season)
        {
            switch (season)
            {
                case 0: // kar yağan bulut
                {
                    float Cloud(Vector2 p) => SmoothMin(SmoothMin(Circle(p, 0.4f, 0.6f, 0.15f), Circle(p, 0.6f, 0.64f, 0.18f), 0.05f),
                        Box(p, 0.5f, 0.52f, 0.27f, 0.07f, 0.07f), 0.05f);
                    c.Part(Cloud, White, Theme.Hex("E3E8F5"));
                    c.Face(0.5f, 0.58f, 1f);
                    foreach (var (x, y, s) in new[] { (0.3f, 0.3f, 0.045f), (0.5f, 0.22f, 0.05f), (0.7f, 0.3f, 0.045f), (0.4f, 0.12f, 0.035f), (0.62f, 0.1f, 0.035f) })
                    {
                        c.Fill(p => Snowflake(p, x, y, s) - Line * 0.6f, Ink);
                        c.Fill(p => Snowflake(p, x, y, s), Theme.Hex("A9D6F5"));
                    }
                    break;
                }
                case 1: // ilkbahar: çimenden açan üç çiçek ve kelebek
                {
                    c.Part(p => Ellipse(p, 0.5f, 0.12f, 0.38f, 0.06f), Theme.Hex("B9F0B0"), Theme.Hex("8FD6A8"));
                    var flowers = new[] { (0.3f, 0.46f, "FF9EBB"), (0.52f, 0.58f, "FFD166"), (0.72f, 0.44f, "C3B5F0") };
                    foreach (var (x, y, col) in flowers)
                    {
                        c.Stroke(p => Seg(p, x, 0.14f, x, y, 0.013f), Theme.Hex("6BBF6A"));
                        c.Part(p => EllipseRot(p, x + 0.045f, (0.14f + y) * 0.5f, 0.045f, 0.02f, 30f), Theme.Hex("A8E6A1"), Theme.Hex("6BBF6A"));
                        for (int i = 0; i < 5; i++)
                        {
                            float a = Mathf.PI / 2 + i * Mathf.PI * 2 / 5;
                            float fx = x + Mathf.Cos(a) * 0.05f, fy = y + Mathf.Sin(a) * 0.05f;
                            c.Part(p => Circle(p, fx, fy, 0.04f), Theme.Hex(col));
                        }
                        c.Part(p => Circle(p, x, y, 0.032f), Theme.Hex("FFE58A"), Theme.Hex("FFC93C"));
                    }
                    // kelebek
                    c.Part(p => EllipseRot(p, 0.64f, 0.82f, 0.05f, 0.035f, 30f), Theme.Hex("FFC6FF"), Theme.Hex("FF8FAB"));
                    c.Part(p => EllipseRot(p, 0.75f, 0.82f, 0.05f, 0.035f, -30f), Theme.Hex("FFC6FF"), Theme.Hex("FF8FAB"));
                    c.Stroke(p => Seg(p, 0.695f, 0.78f, 0.695f, 0.86f, 0.01f), Ink);
                    break;
                }
                case 2: // gülen güneş
                {
                    for (int i = 0; i < 10; i++)
                    {
                        float a = i * Mathf.PI * 2 / 10;
                        float x0 = 0.5f + Mathf.Cos(a) * 0.29f, y0 = 0.52f + Mathf.Sin(a) * 0.29f;
                        float x1 = 0.5f + Mathf.Cos(a) * 0.39f, y1 = 0.52f + Mathf.Sin(a) * 0.39f;
                        c.Stroke(p => Seg(p, x0, y0, x1, y1, 0.022f), Theme.Hex("FFC93C"));
                    }
                    c.Part(p => Circle(p, 0.5f, 0.52f, 0.23f), Theme.Hex("FFE58A"), Theme.Hex("FFC93C"));
                    c.Face(0.5f, 0.52f, 1.6f);
                    c.Shine(0.42f, 0.64f, 0.05f, 0.03f, 0.7f);
                    break;
                }
                default: // sonbahar: rüzgârda savrulan yapraklar
                {
                    foreach (var (x, y, w) in new[] { (0.2f, 0.72f, 0.12f), (0.36f, 0.5f, 0.14f), (0.18f, 0.3f, 0.12f), (0.42f, 0.2f, 0.1f) })
                        c.Fill(p => Seg(p, x, y, x + w, y, 0.012f), Theme.Hex("C3B5F0").WithAlpha(0.8f));
                    foreach (var (x, y, r, col) in new[] { (0.4f, 0.7f, 30f, "FFB35A"), (0.64f, 0.52f, -35f, "E86A4A"), (0.42f, 0.32f, 15f, "F2B13A"), (0.7f, 0.22f, 55f, "FF9E7A") })
                    {
                        c.Part(p => EllipseRot(p, x, y, 0.1f, 0.05f, r), Theme.Hex(col), Color.Lerp(Theme.Hex(col), Theme.Hex("C0503A"), 0.35f));
                        float rr = r * Mathf.Deg2Rad;
                        c.Fill(p => Seg(p, x - Mathf.Cos(rr) * 0.09f, y - Mathf.Sin(rr) * 0.09f, x + Mathf.Cos(rr) * 0.08f, y + Mathf.Sin(rr) * 0.08f, 0.006f), Ink.WithAlpha(0.6f));
                    }
                    break;
                }
            }
        }
    }
}
