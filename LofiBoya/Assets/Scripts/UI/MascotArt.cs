using System;
using UnityEngine;

namespace LofiBoya
{
    public enum MascotMood { Normal, Happy, Sad, Angry, Surprised, Scared, Proud, Calm, Shy }
    public enum MascotHat { None, Party, Crown, Chef, Firefighter, Doctor, Flower, Astronaut }

    /// <summary>
    /// Maskot "Pofuduk": konturlu, sticker gibi yumuşak bir bulut.
    /// Gövde, açık göz, kapalı göz ve el ayrı katmanlar: göz kırpma ve el sallama için.
    /// Ayrıca yıldız ve yapboz ikonları.
    /// </summary>
    public static class MascotArt
    {
        const int W = 320, H = 280;
        static Sprite _body, _eyesOpen, _eyesClosed, _eyesHappy, _hand, _star, _puzzle, _starOutline;

        public static Sprite Body => _body != null ? _body : (_body = Make(W, H, DrawBody));
        public static Sprite EyesOpen => _eyesOpen != null ? _eyesOpen : (_eyesOpen = Make(W, H, (px) => DrawEyes(px, 0)));
        public static Sprite EyesClosed => _eyesClosed != null ? _eyesClosed : (_eyesClosed = Make(W, H, (px) => DrawEyes(px, 1)));
        /// <summary>Mutlu kapalı gözler (^^).</summary>
        public static Sprite EyesHappy => _eyesHappy != null ? _eyesHappy : (_eyesHappy = Make(W, H, (px) => DrawEyes(px, 2)));
        public static Sprite Hand => _hand != null ? _hand : (_hand = Make(96, 96, DrawHand));
        public static Sprite Star => _star != null ? _star : (_star = Make(128, 128, DrawStar));
        public static Sprite StarOutline => _starOutline != null ? _starOutline : (_starOutline = Make(128, 128, DrawStarOutline));
        public static Sprite Puzzle => _puzzle != null ? _puzzle : (_puzzle = Make(128, 128, DrawPuzzle));

        static readonly Color Ink = Theme.Hex("5E4B6E");

        // ---------------- çizim altyapısı ----------------
        class Canvas
        {
            public int w, h;
            public Color[] px;
            public float aa;
            public Vector2 P(int x, int y) => new((x + 0.5f) / w, (y + 0.5f) / w); // y de genişliğe göre ölçeklenir
            /// <summary>SDF ile şekli verilen renkle "üstüne" boya.</summary>
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
        }

        static Sprite Make(int w, int h, Action<Canvas> draw)
        {
            var cv = new Canvas { w = w, h = h, px = new Color[w * h], aa = 1.3f / w };
            draw(cv);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels(cv.px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100);
        }

        static float Circle(Vector2 p, float x, float y, float r) => (p - new Vector2(x, y)).magnitude - r;
        static float Ellipse(Vector2 p, float x, float y, float rx, float ry)
        {
            var q = new Vector2((p.x - x) / rx, (p.y - y) / ry);
            return (q.magnitude - 1f) * Mathf.Min(rx, ry);
        }
        static float SmoothMin(float a, float b, float k)
        {
            float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
            return Mathf.Lerp(b, a, h) - k * h * (1 - h);
        }

        // ---------------- gövde ----------------
        static float CloudSdf(Vector2 p)
        {
            // yumuşakça birleşen daireler = pofuduk bulut
            float d = Circle(p, 0.50f, 0.36f, 0.27f);
            d = SmoothMin(d, Circle(p, 0.25f, 0.33f, 0.17f), 0.06f);
            d = SmoothMin(d, Circle(p, 0.75f, 0.33f, 0.17f), 0.06f);
            d = SmoothMin(d, Circle(p, 0.36f, 0.56f, 0.17f), 0.05f);
            d = SmoothMin(d, Circle(p, 0.62f, 0.58f, 0.18f), 0.05f);
            d = SmoothMin(d, Circle(p, 0.15f, 0.22f, 0.10f), 0.05f);
            d = SmoothMin(d, Circle(p, 0.85f, 0.22f, 0.10f), 0.05f);
            // altı düzce
            d = Mathf.Max(d, 0.115f - p.y);
            return d;
        }

        static void DrawBody(Canvas c) => DrawBody(c, true);

        static void DrawBody(Canvas c, bool mouth)
        {
            // yumuşak gölge
            c.Fill(p => CloudSdf(p + new Vector2(0, 0.018f)) - 0.004f, new Color(0.37f, 0.29f, 0.43f, 0.18f));
            // kontur
            c.Fill(p => CloudSdf(p) - 0.012f, Ink);
            // dolgu: üstte beyaz, altta hafif lila
            c.Fill(CloudSdf, p => Color.Lerp(Theme.Hex("E9E1FF"), Color.white, Mathf.SmoothStep(0.12f, 0.6f, p.y)));
            // üstte parlama
            c.Fill(p => Ellipse(p, 0.40f, 0.66f, 0.08f, 0.035f), new Color(1, 1, 1, 0.9f));
            // yanaklar
            c.Fill(p => Ellipse(p, 0.33f, 0.33f, 0.055f, 0.035f), new Color(1f, 0.62f, 0.7f, 0.75f));
            c.Fill(p => Ellipse(p, 0.67f, 0.33f, 0.055f, 0.035f), new Color(1f, 0.62f, 0.7f, 0.75f));
            // gülümseme (ağız açık, içi pembe)
            if (mouth) DrawSmile(c);
        }

        static void DrawEyes(Canvas c, int mode)
        {
            float[] xs = { 0.40f, 0.60f };
            foreach (var x in xs)
            {
                if (mode == 0)
                {
                    c.Fill(p => Ellipse(p, x, 0.43f, 0.036f, 0.05f), Ink);
                    c.Fill(p => Circle(p, x - 0.012f, 0.452f, 0.013f), Color.white);
                    c.Fill(p => Circle(p, x + 0.012f, 0.418f, 0.006f), new Color(1, 1, 1, 0.8f));
                }
                else if (mode == 1)
                {
                    // kapalı göz: aşağı bakan yay
                    c.Fill(p => Mathf.Max(Mathf.Abs(Circle(p, x, 0.445f, 0.035f)) - 0.008f, p.y - 0.445f), Ink);
                }
                else
                {
                    // mutlu ^^
                    c.Fill(p => Mathf.Max(Mathf.Abs(Circle(p, x, 0.415f, 0.035f)) - 0.009f, 0.415f - p.y), Ink);
                }
            }
        }

        static void DrawHand(Canvas c)
        {
            c.Fill(p => Circle(p, 0.5f, 0.5f, 0.36f), Ink);
            c.Fill(p => Circle(p, 0.5f, 0.5f, 0.31f), p => Color.Lerp(Theme.Hex("E9E1FF"), Color.white, p.y));
        }

        // ---------------- ikonlar ----------------
        static Vector2[] StarPoly(float cx, float cy, float r1, float r2)
        {
            var v = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2 + i * Mathf.PI / 5;
                float r = i % 2 == 0 ? r1 : r2;
                v[i] = new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r);
            }
            return v;
        }

        static void DrawStar(Canvas c)
        {
            var outer = StarPoly(0.5f, 0.47f, 0.44f, 0.2f);
            c.Fill(p => Polygon(p, outer) - 0.04f, Theme.Hex("F4A93B"));
            c.Fill(p => Polygon(p, outer) - 0.005f, p => Color.Lerp(Theme.Hex("FFC93C"), Theme.Hex("FFE58A"), p.y));
            c.Fill(p => Ellipse(p, 0.42f, 0.6f, 0.07f, 0.04f), new Color(1, 1, 1, 0.7f));
        }

        static void DrawStarOutline(Canvas c)
        {
            var outer = StarPoly(0.5f, 0.47f, 0.44f, 0.2f);
            c.Fill(p => Polygon(p, outer) - 0.04f, new Color(0.37f, 0.29f, 0.43f, 0.18f));
            c.Fill(p => Polygon(p, outer) + 0.01f, new Color(1, 1, 1, 0.6f));
        }

        static void DrawPuzzle(Canvas c)
        {
            float Sdf(Vector2 p)
            {
                float box = RoundBox(p, new Vector2(0.48f, 0.46f), new Vector2(0.28f, 0.28f), 0.06f);
                box = Mathf.Min(box, Circle(p, 0.48f, 0.80f, 0.1f));    // üst çıkıntı
                box = Mathf.Min(box, Circle(p, 0.82f, 0.46f, 0.1f));    // sağ çıkıntı
                box = Mathf.Max(box, -Circle(p, 0.18f, 0.46f, 0.09f));  // sol girinti
                return box;
            }
            c.Fill(Sdf, Color.white);
        }

        static float RoundBox(Vector2 p, Vector2 c, Vector2 half, float r)
        {
            var q = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + new Vector2(r, r);
            return new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - r;
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

        // ---------------- duygular ----------------
        static void DrawSmile(Canvas c)
        {
            c.Fill(p => Mathf.Max(Circle(p, 0.5f, 0.345f, 0.052f), p.y - 0.345f), Ink);
            c.Fill(p => Mathf.Max(Circle(p, 0.5f, 0.345f, 0.036f), p.y - 0.335f), Theme.Hex("FF8FA3"));
        }

        static float Seg(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            var pa = p - a; var ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - r;
        }

        static Sprite _bodyPlain;
        /// <summary>Ağızsız gövde: duygu yüzleri üstüne gelir.</summary>
        public static Sprite BodyPlain => _bodyPlain != null ? _bodyPlain : (_bodyPlain = Make(W, H, c => DrawBody(c, false)));

        static readonly Sprite[] _faces = new Sprite[9];
        /// <summary>Duygu yüzü (göz + kaş + ağız), BodyPlain üstüne.</summary>
        public static Sprite Face(MascotMood m)
        {
            int i = (int)m;
            if (_faces[i] == null) _faces[i] = Make(W, H, c => DrawFace(c, m));
            return _faces[i];
        }

        static void OpenEye(Canvas c, float x, float y, float rx, float ry)
        {
            c.Fill(p => Ellipse(p, x, y, rx, ry), Ink);
            c.Fill(p => Circle(p, x - 0.012f, y + 0.022f, 0.013f), Color.white);
        }

        static void DrawFace(Canvas c, MascotMood m)
        {
            switch (m)
            {
                case MascotMood.Happy:
                    foreach (var x in new[] { 0.40f, 0.60f })
                        c.Fill(p => Mathf.Max(Mathf.Abs(Circle(p, x, 0.415f, 0.035f)) - 0.009f, 0.415f - p.y), Ink);
                    DrawSmile(c);
                    break;
                case MascotMood.Sad:
                    OpenEye(c, 0.40f, 0.425f, 0.034f, 0.046f);
                    OpenEye(c, 0.60f, 0.425f, 0.034f, 0.046f);
                    c.Fill(p => Seg(p, new Vector2(0.355f, 0.49f), new Vector2(0.43f, 0.515f), 0.008f), Ink);   // iç uçları yukarı kaşlar
                    c.Fill(p => Seg(p, new Vector2(0.645f, 0.49f), new Vector2(0.57f, 0.515f), 0.008f), Ink);
                    c.Fill(p => Mathf.Max(Mathf.Abs(Circle(p, 0.5f, 0.30f, 0.045f)) - 0.008f, 0.30f - p.y), Ink);   // ters gülüş
                    c.Fill(p => Ellipse(p, 0.635f, 0.37f, 0.014f, 0.022f), Theme.Hex("7EC8F5"));                  // gözyaşı
                    break;
                case MascotMood.Angry:
                    OpenEye(c, 0.40f, 0.42f, 0.034f, 0.04f);
                    OpenEye(c, 0.60f, 0.42f, 0.034f, 0.04f);
                    c.Fill(p => Seg(p, new Vector2(0.35f, 0.505f), new Vector2(0.44f, 0.475f), 0.01f), Ink);    // iç uçları aşağı kaşlar
                    c.Fill(p => Seg(p, new Vector2(0.65f, 0.505f), new Vector2(0.56f, 0.475f), 0.01f), Ink);
                    c.Fill(p => Seg(p, new Vector2(0.46f, 0.325f), new Vector2(0.54f, 0.325f), 0.009f), Ink);   // düz ağız
                    break;
                case MascotMood.Surprised:
                    foreach (var x in new[] { 0.40f, 0.60f })
                    {
                        c.Fill(p => Circle(p, x, 0.43f, 0.045f), Ink);
                        c.Fill(p => Circle(p, x, 0.43f, 0.03f), Color.white);
                        c.Fill(p => Circle(p, x, 0.425f, 0.018f), Ink);
                        c.Fill(p => Mathf.Max(Mathf.Abs(Circle(p, x, 0.49f, 0.04f)) - 0.007f, 0.505f - p.y), Ink);   // kalkık kaşlar
                    }
                    c.Fill(p => Ellipse(p, 0.5f, 0.32f, 0.032f, 0.042f), Ink);                                  // "O" ağız
                    c.Fill(p => Ellipse(p, 0.5f, 0.32f, 0.019f, 0.028f), Theme.Hex("FF8FA3"));
                    break;
                case MascotMood.Scared:
                    // kocaman gözler, küçük gözbebekleri, titrek ağız, ter damlası
                    foreach (var x in new[] { 0.40f, 0.60f })
                    {
                        c.Fill(p => Circle(p, x, 0.43f, 0.046f), Ink);
                        c.Fill(p => Circle(p, x, 0.43f, 0.034f), Color.white);
                        c.Fill(p => Circle(p, x, 0.425f, 0.011f), Ink);
                    }
                    c.Fill(p => Seg(p, new Vector2(0.355f, 0.50f), new Vector2(0.43f, 0.525f), 0.008f), Ink);
                    c.Fill(p => Seg(p, new Vector2(0.645f, 0.50f), new Vector2(0.57f, 0.525f), 0.008f), Ink);
                    for (int k = 0; k < 4; k++)
                    {
                        float x0 = 0.44f + k * 0.03f, y0 = k % 2 == 0 ? 0.325f : 0.305f, y1 = k % 2 == 0 ? 0.305f : 0.325f;
                        c.Fill(p => Seg(p, new Vector2(x0, y0), new Vector2(x0 + 0.03f, y1), 0.007f), Ink);
                    }
                    var tear = new[] { new Vector2(0.682f, 0.475f), new Vector2(0.718f, 0.475f), new Vector2(0.70f, 0.515f) };
                    c.Fill(p => Mathf.Min(Circle(p, 0.70f, 0.47f, 0.018f), Polygon(p, tear)), Theme.Hex("7EC8F5"));
                    break;
                case MascotMood.Proud:
                    // kapalı gülen gözler, kocaman gülüş, pembe yanaklar, kalkık burun gibi kaşlar
                    foreach (var x in new[] { 0.40f, 0.60f })
                        c.Fill(p => Mathf.Max(Mathf.Abs(Circle(p, x, 0.415f, 0.035f)) - 0.009f, 0.415f - p.y), Ink);
                    foreach (var x in new[] { 0.33f, 0.67f })
                        c.Fill(p => Ellipse(p, x, 0.36f, 0.035f, 0.022f), Theme.Hex("FFB3C1"));
                    c.Fill(p => Mathf.Max(Circle(p, 0.5f, 0.35f, 0.07f), p.y - 0.35f), Ink);
                    c.Fill(p => Mathf.Max(Circle(p, 0.5f, 0.35f, 0.054f), p.y - 0.34f), Theme.Hex("FF8FA3"));
                    // gurur yıldızı: kafanın yanında parlayan küçük altın yıldız (mutlu yüzden ayırt edilsin)
                    {
                        var st = StarPoly(0.76f, 0.56f, 0.07f, 0.032f);
                        c.Fill(p => Polygon(p, st) - 0.008f, Theme.Hex("F4A93B"));
                        c.Fill(p => Polygon(p, st), Theme.Hex("FFD54A"));
                    }
                    break;
                case MascotMood.Calm:
                    // aşağı bakan yumuşak kapalı gözler (huzurlu), küçük gülümseme
                    foreach (var x in new[] { 0.40f, 0.60f })
                        c.Fill(p => Mathf.Max(Mathf.Abs(Circle(p, x, 0.44f, 0.035f)) - 0.008f, p.y - 0.44f), Ink);
                    c.Fill(p => Mathf.Max(Mathf.Abs(Circle(p, 0.5f, 0.35f, 0.035f)) - 0.008f, p.y - 0.35f), Ink);
                    break;
                case MascotMood.Shy:
                    // yana bakan gözler, kızarmış yanaklar, minik dalgalı ağız
                    OpenEye(c, 0.39f, 0.41f, 0.03f, 0.036f);
                    OpenEye(c, 0.59f, 0.41f, 0.03f, 0.036f);
                    foreach (var x in new[] { 0.33f, 0.67f })
                        c.Fill(p => Ellipse(p, x, 0.355f, 0.045f, 0.026f), Theme.Hex("FF9FB2"));
                    c.Fill(p => Seg(p, new Vector2(0.47f, 0.32f), new Vector2(0.50f, 0.31f), 0.007f), Ink);
                    c.Fill(p => Seg(p, new Vector2(0.50f, 0.31f), new Vector2(0.53f, 0.32f), 0.007f), Ink);
                    break;
                default:
                    DrawEyes(c, 0);
                    DrawSmile(c);
                    break;
            }
        }

        // ---------------- şapkalar ----------------
        static readonly Sprite[] _hats = new Sprite[8];
        /// <summary>Başa takılan aksesuar. Astronot kaskı gövdeyle aynı boyda (W x H).</summary>
        public static Sprite Hat(MascotHat h)
        {
            int i = (int)h;
            if (h == MascotHat.None) return null;
            if (_hats[i] == null) _hats[i] = h == MascotHat.Astronaut ? Make(W, H, DrawHelmet) : Make(160, 160, c => DrawHat(c, h));
            return _hats[i];
        }

        static void Outlined(Canvas c, Func<Vector2, float> sdf, Func<Vector2, Color> col, float w = 0.035f)
        {
            c.Fill(p => sdf(p) - w, Ink);
            c.Fill(sdf, col);
        }

        static void Outlined(Canvas c, Func<Vector2, float> sdf, Color col, float w = 0.035f) => Outlined(c, sdf, _ => col, w);

        static void DrawHat(Canvas c, MascotHat h)
        {
            var pink = Theme.Hex("FF8FC7"); var yellow = Theme.Hex("FFD93D"); var red = Theme.Hex("FF5A5F"); var blue = Theme.Hex("7EC8F5");
            switch (h)
            {
                case MascotHat.Party:
                {
                    var tri = new[] { new Vector2(0.28f, 0.12f), new Vector2(0.72f, 0.12f), new Vector2(0.5f, 0.82f) };
                    Outlined(c, p => Polygon(p, tri), p => Mathf.Repeat(p.x * 4f + p.y * 3f, 1f) < 0.5f ? pink : Theme.Hex("FFD1E8"));
                    Outlined(c, p => Circle(p, 0.5f, 0.84f, 0.075f), yellow);
                    break;
                }
                case MascotHat.Crown:
                {
                    var cr = new[] { new Vector2(0.18f, 0.14f), new Vector2(0.82f, 0.14f), new Vector2(0.86f, 0.62f), new Vector2(0.68f, 0.40f),
                                     new Vector2(0.5f, 0.70f), new Vector2(0.32f, 0.40f), new Vector2(0.14f, 0.62f) };
                    Outlined(c, p => Polygon(p, cr), p => Color.Lerp(Theme.Hex("FFC93C"), Theme.Hex("FFE58A"), p.y));
                    Outlined(c, p => Circle(p, 0.5f, 0.72f, 0.05f), pink, 0.025f);
                    Outlined(c, p => Circle(p, 0.14f, 0.64f, 0.045f), blue, 0.025f);
                    Outlined(c, p => Circle(p, 0.86f, 0.64f, 0.045f), blue, 0.025f);
                    Outlined(c, p => Circle(p, 0.5f, 0.26f, 0.05f), red, 0.025f);
                    break;
                }
                case MascotHat.Chef:
                {
                    float Puff(Vector2 p) => Mathf.Min(Mathf.Min(Circle(p, 0.30f, 0.48f, 0.17f), Circle(p, 0.5f, 0.58f, 0.2f)), Circle(p, 0.70f, 0.48f, 0.17f));
                    Outlined(c, Puff, Color.white);
                    Outlined(c, p => RoundBox(p, new Vector2(0.5f, 0.22f), new Vector2(0.30f, 0.09f), 0.03f), Color.white);
                    break;
                }
                case MascotHat.Firefighter:
                {
                    Outlined(c, p => Mathf.Max(Ellipse(p, 0.5f, 0.24f, 0.30f, 0.34f), 0.24f - p.y), red);
                    Outlined(c, p => Ellipse(p, 0.5f, 0.22f, 0.42f, 0.065f), red);
                    Outlined(c, p => RoundBox(p, new Vector2(0.5f, 0.42f), new Vector2(0.07f, 0.08f), 0.02f), yellow, 0.025f);
                    break;
                }
                case MascotHat.Doctor:
                {
                    Outlined(c, p => RoundBox(p, new Vector2(0.5f, 0.32f), new Vector2(0.27f, 0.17f), 0.06f), Color.white);
                    c.Fill(p => RoundBox(p, new Vector2(0.5f, 0.32f), new Vector2(0.035f, 0.11f), 0.01f), red);
                    c.Fill(p => RoundBox(p, new Vector2(0.5f, 0.32f), new Vector2(0.11f, 0.035f), 0.01f), red);
                    break;
                }
                case MascotHat.Flower:
                {
                    for (int k = 0; k < 5; k++)
                    {
                        float a = Mathf.PI / 2 + k * Mathf.PI * 2 / 5;
                        float px = 0.5f + Mathf.Cos(a) * 0.17f, py = 0.42f + Mathf.Sin(a) * 0.17f;
                        Outlined(c, p => Circle(p, px, py, 0.12f), pink, 0.03f);
                    }
                    Outlined(c, p => Circle(p, 0.5f, 0.42f, 0.09f), yellow, 0.03f);
                    break;
                }
            }
        }

        static void DrawHelmet(Canvas c)
        {
            // cam fanus: ince kontur halkası + çok hafif mavi cam + parlama
            c.Fill(p => Mathf.Abs(Circle(p, 0.5f, 0.43f, 0.405f)) - 0.012f, Ink);
            c.Fill(p => Circle(p, 0.5f, 0.43f, 0.393f), new Color(0.6f, 0.85f, 1f, 0.12f));
            c.Fill(p => Mathf.Max(Mathf.Abs(Circle(p, 0.5f, 0.43f, 0.34f)) - 0.012f, Mathf.Max(p.x - 0.36f, 0.62f - p.y)), new Color(1, 1, 1, 0.85f));
            // yaka halkası
            c.Fill(p => RoundBox(p, new Vector2(0.5f, 0.07f), new Vector2(0.33f, 0.045f), 0.03f) - 0.01f, Ink);
            c.Fill(p => RoundBox(p, new Vector2(0.5f, 0.07f), new Vector2(0.33f, 0.045f), 0.03f), Theme.Hex("D9DEE8"));
        }
    }
}
