using UnityEngine;

namespace LofiBoya
{
    /// <summary>
    /// Sihirli boyalar: kova dolgusuna desen ekler. Stil, bölge renginin alfa kanalında saklanır
    /// (alfa = 255 - stil). Aynı hesap ColoringFill.shader içinde de var; burası küçük resim ve
    /// galeriye kaydetme için CPU tarafı (parıltı orada donuk durur).
    /// </summary>
    public static class MagicPaint
    {
        public const int Normal = 0, Glitter = 1, Rainbow = 2, Dots = 3, Stars = 4, Count = 5;
        public static readonly string[] Names = { "Normal", "Simli", "Gökkuşağı", "Puantiye", "Yıldızlı" };

        public static byte Encode(int style) => (byte)(255 - Mathf.Clamp(style, 0, Count - 1));
        public static int Decode(byte alpha) => 255 - alpha;

        static float Hash(Vector2 p)
        {
            float h = Mathf.Sin(p.x * 127.1f + p.y * 311.7f) * 43758.5453f;
            return h - Mathf.Floor(h);
        }

        static Color Hsv(float h, float s, float v)
        {
            var c = Color.HSVToRGB(h - Mathf.Floor(h), s, v);
            return c;
        }

        static float Frac(float x) => x - Mathf.Floor(x);

        /// <summary>Shader'daki smoothstep (Mathf.SmoothStep farklı çalışır).</summary>
        static float SS(float e0, float e1, float x)
        {
            float t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3f - 2f * t);
        }

        /// <summary>px: resim piksel koordinatı.</summary>
        public static Color Apply(Color c, int style, Vector2 px, float time = 0f)
        {
            switch (style)
            {
                case Glitter:
                {
                    var cell = new Vector2(Mathf.Floor(px.x / 6f), Mathf.Floor(px.y / 6f));
                    float h = Hash(cell);
                    float tw = 0.5f + 0.5f * Mathf.Sin(time * 3f + h * 40f);
                    var col = c * (0.9f + 0.2f * Hash(cell + new Vector2(3.1f, 3.1f)));
                    float spark = (h > 0.86f ? 1f : 0f) * tw;
                    col = Color.Lerp(col, Color.white, spark * 0.85f);
                    var big = new Vector2(Mathf.Floor(px.x / 34f), Mathf.Floor(px.y / 34f));
                    float hb = Hash(big + new Vector2(7.7f, 7.7f));
                    var f = new Vector2(Frac(px.x / 34f) - 0.5f, Frac(px.y / 34f) - 0.5f);
                    float star4 = Mathf.Max(0f, 1f - (Mathf.Abs(f.x) * Mathf.Abs(f.y) * 600f + f.magnitude * 6f));
                    col += Color.white * star4 * (hb > 0.7f ? 1f : 0f) * (0.5f + 0.5f * Mathf.Sin(time * 2f + hb * 30f));
                    col.a = 1;
                    return col;
                }
                case Rainbow:
                {
                    float hue = (px.x + px.y) / 900f + time * 0.03f;
                    var r = Color.Lerp(c, Hsv(hue, 0.5f, 1f), 0.75f);
                    r.a = 1;
                    return r;
                }
                case Dots:
                {
                    float gy = px.y / 40f;
                    float gx = px.x / 40f + 0.5f * (Mathf.Floor(gy) % 2f);
                    var f = new Vector2(Frac(gx) - 0.5f, Frac(gy) - 0.5f);
                    float inside = 1f - SS(0.17f, 0.21f, f.magnitude);
                    var r = Color.Lerp(c, Color.Lerp(c, Color.white, 0.65f), inside);
                    r.a = 1;
                    return r;
                }
                case Stars:
                {
                    float gy = px.y / 56f;
                    float gx = px.x / 56f + 0.5f * (Mathf.Floor(gy) % 2f);
                    var f = new Vector2(Frac(gx) - 0.5f, Frac(gy) - 0.5f);
                    float a = Mathf.Atan2(f.x, f.y);
                    float rr = 0.30f * (0.45f + 0.55f * Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 2.5f)), 3f));
                    float inside = 1f - SS(rr - 0.02f, rr + 0.02f, f.magnitude);
                    var r = Color.Lerp(c, Color.Lerp(c, new Color(1f, 0.95f, 0.7f), 0.75f), inside);
                    r.a = 1;
                    return r;
                }
                default:
                    c.a = 1;
                    return c;
            }
        }

        /// <summary>Stil seçici için yuvarlak önizleme (renkle boyanmış).</summary>
        public static Sprite Preview(Color baseColor, int style, int size = 96)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((1f - d) * size * 0.5f);
                // önizlemede desen küçük dairede de görünsün diye ölçek büyütülür
                var c = style == Rainbow
                    ? Color.Lerp(baseColor, Color.HSVToRGB(Frac((x + y) / (float)(size * 2)), 0.5f, 1f), 0.75f)
                    : Apply(baseColor, style, new Vector2(x, y) * 1.6f + new Vector2(10, 10), 0.7f);
                // küçük dairede desen seçilsin: farkı büyüt
                if (style == Dots || style == Stars)
                {
                    float m = Mathf.Clamp01(((c.r - baseColor.r) + (c.g - baseColor.g) + (c.b - baseColor.b)) * 4f);
                    c = Color.Lerp(baseColor * 0.88f, Color.white, m);
                }
                c.a = a;
                px[y * size + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
