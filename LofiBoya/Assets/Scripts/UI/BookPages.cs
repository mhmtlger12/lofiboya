using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Kitap sayfalarının düzeni (PageStudio içinde kurulur, dokuya basılır). Boyut PageStudio.PW × PH.
    /// Kâğıt: krem zemin + iki ölçekte lif dokusu + kenarlarda hafif sararma + sırtta kıvrım gölgesi.
    /// Kapak: bez ciltli sert kapak (doku + cila parıltısı), yaldızlı çerçeve, etiketli başlık, paspartulu resim.
    /// </summary>
    public static class BookPages
    {
        static readonly System.Collections.Generic.Dictionary<string, Texture2D> _cloth = new();

        static Sprite _bookIcon;

        /// <summary>Açık kitap ikonu (beyaz, renklendirilebilir): iki eğik sayfa, ortada sırt, sayfalarda satırlar.</summary>
        public static Sprite BookIcon
        {
            get
            {
                if (_bookIcon != null) return _bookIcon;
                const int n = 128;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "BookIcon" };
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                    float side = Mathf.Abs(u);
                    // sayfa: dışa doğru hafif yükselen üst kenar, alt kenarda kavis
                    float top = 0.55f + side * 0.12f - (side < 0.06f ? 0.08f : 0f);
                    float bottom = -0.55f + side * 0.12f + Mathf.Max(0, 0.06f - side) * 1.2f;
                    float d = Mathf.Max(Mathf.Max(v - top, bottom - v), side - 0.92f);
                    float a = Mathf.Clamp01(0.5f - d * n * 0.5f);
                    // satırlar ve sırt boşluğu: biraz saydam
                    if (side < 0.035f) a *= 0.35f;
                    float line = Mathf.Repeat(v - side * 0.12f + 0.06f, 0.2f);
                    if (side > 0.2f && side < 0.78f && line < 0.045f && v < top - 0.12f && v > bottom + 0.1f) a *= 0.45f;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                _bookIcon = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100);
                return _bookIcon;
            }
        }

        static Color Lighten(Color c, float k) => Color.Lerp(c, Color.white, k);
        static Color Darken(Color c, float k) => Color.Lerp(c, Theme.Text, k);

        static RawImage Raw(Transform parent, string name, Texture tex, Color c)
        {
            var rt = UIKit.Node(name, parent);
            var r = rt.gameObject.AddComponent<RawImage>();
            r.texture = tex;
            r.color = c;
            r.raycastTarget = false;
            return r;
        }

        static void Paper(RectTransform root)
        {
            var bg = UIKit.Node("Paper", root).Stretch();
            var img = bg.gameObject.AddComponent<Image>();
            img.color = Theme.Paper;
            img.raycastTarget = false;
            var fine = Raw(root, "Fibers", Art.Grain, Theme.Text.WithAlpha(0.055f));
            fine.rectTransform.Stretch();
            fine.uvRect = new Rect(0, 0, PageStudio.PW / 170f, PageStudio.PH / 170f);
            var blot = Raw(root, "Blots", Art.Grain, new Color(0.55f, 0.4f, 0.25f, 0.035f));
            blot.rectTransform.Stretch();
            blot.uvRect = new Rect(0.37f, 0.61f, PageStudio.PW / 700f, PageStudio.PH / 700f);
            var vig = Raw(root, "Age", PageStudio.Vignette, new Color(0.62f, 0.45f, 0.3f, 0.2f));
            vig.rectTransform.Stretch();
            // sırt kıvrımı: soldan gelen gölge + kâğıdın kalktığı yerde ince ışık
            var gut = Raw(root, "Gutter", PageStudio.Gutter, Theme.Text.WithAlpha(0.24f));
            gut.rectTransform.anchorMin = new Vector2(0, 0);
            gut.rectTransform.anchorMax = new Vector2(0, 1);
            gut.rectTransform.pivot = new Vector2(0, 0.5f);
            gut.rectTransform.sizeDelta = new Vector2(120, 0);
            var lip = UIKit.Box(root, "Lip", Color.white.WithAlpha(0.5f), null);
            lip.sprite = null;
            lip.type = Image.Type.Simple;
            lip.rectTransform.anchorMin = new Vector2(0, 0);
            lip.rectTransform.anchorMax = new Vector2(0, 1);
            lip.rectTransform.pivot = new Vector2(0, 0.5f);
            lip.rectTransform.sizeDelta = new Vector2(3, 0);
            lip.rectTransform.anchoredPosition = new Vector2(16, 0);
        }

        /// <summary>Kâğıda basılmış resim: altında yumuşak gölge, köşeleri yuvarlak (doku kendisi yuvarlatılmış).</summary>
        static void Picture(RectTransform root, Texture2D art, Vector2 pos, Vector2 size, Vector2 anchor)
        {
            var holder = UIKit.Node("Picture", root);
            holder.Place(anchor, pos, size);
            var sh = UIKit.Box(holder, "Shadow", new Color(0.3f, 0.2f, 0.4f, 0.32f), UIKit.Shadow, 1.6f);
            sh.rectTransform.Stretch(-20, -8, -20, -30);
            if (art != null)
            {
                var r = Raw(holder, "Art", art, Color.white);
                r.rectTransform.Stretch();
            }
            else
            {
                var ph = UIKit.Box(holder, "Wait", Theme.Lavender.WithAlpha(0.35f), UIKit.Rounded, 1.2f);
                ph.rectTransform.Stretch();
                for (int i = 0; i < 3; i++)
                {
                    var d = UIKit.Box(ph.transform, "Dot", Theme.Lavender, UIKit.Circle);
                    d.type = Image.Type.Simple;
                    d.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 60, 0), new Vector2(34, 34));
                }
            }
        }

        /// <summary>
        /// Kutuya sığan metin. Kutunun boyutu önceden ayarlanmış olmalı (yerleşim ondan hesaplanır).
        /// Arapçada satırları kendimiz böleriz: tek parça ters çevrilen cümleyi Unity satırlara bölünce cümlenin sonu
        /// ilk satıra düşüyordu. Her satır ayrı şekillendirilince (ArabicShaper satır satır çalışır) sıra doğru olur.
        /// </summary>
        static Text Words(RectTransform box, string text, int size, Color c, TextAnchor align, FontStyle style, float spacing,
            float l = 0, float t = 0, float r = 0, float b = 0)
        {
            var lbl = UIKit.Label(box, "", size, c, align, style);
            lbl.rectTransform.Stretch(l, t, r, b);
            lbl.lineSpacing = spacing;
            if (Loc.IsArabic) ArabicFit(lbl, text, size);
            else
            {
                lbl.horizontalOverflow = HorizontalWrapMode.Wrap;
                lbl.verticalOverflow = VerticalWrapMode.Truncate;
                lbl.resizeTextForBestFit = true;
                lbl.resizeTextMaxSize = size;
                lbl.resizeTextMinSize = Mathf.Max(24, size / 2);
                lbl.text = text;
            }
            return lbl;
        }

        static void ArabicFit(Text t, string text, int max)
        {
            var box = t.rectTransform.rect;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.resizeTextForBestFit = false;
            var font = t.font;
            int min = Mathf.Max(24, max / 2);
            string best = text;
            int bestSize = min;
            for (int s = max; s >= min; s -= 2)
            {
                best = Wrap(text, font, s, t.fontStyle, box.width);
                bestSize = s;
                int lines = 1;
                foreach (char ch in best) if (ch == '\n') lines++;
                float lineH = font != null && font.fontSize > 0 ? font.lineHeight * s / font.fontSize : s * 1.3f;
                if (lines * lineH * t.lineSpacing <= box.height) break;
            }
            t.fontSize = bestSize;
            t.text = best;
        }

        /// <summary>Kelimeleri genişliğe sığacak satırlara böler; genişlik şekillendirilmiş (bitişik) harflerle ölçülür.</summary>
        static string Wrap(string text, Font font, int size, FontStyle style, float width)
        {
            if (font == null) return text;
            var words = text.Split(' ');
            var lines = new System.Collections.Generic.List<string>();
            string cur = "";
            foreach (var w in words)
            {
                string test = cur.Length == 0 ? w : cur + " " + w;
                if (cur.Length > 0 && Measure(test, font, size, style) > width) { lines.Add(cur); cur = w; }
                else cur = test;
            }
            if (cur.Length > 0) lines.Add(cur);
            return string.Join("\n", lines);
        }

        static float Measure(string s, Font font, int size, FontStyle style)
        {
            var shaped = ArabicShaper.Shape(s);
            font.RequestCharactersInTexture(shaped, size, style);
            float w = 0;
            foreach (char ch in shaped)
                if (font.GetCharacterInfo(ch, out var ci, size, style)) w += ci.advance;
            return w;
        }

        static void Deco(RectTransform parent, Sprite s, Color c, Vector2 anchor, Vector2 pos, float size, float rot)
        {
            var img = UIKit.Box(parent, "Deco", c, s);
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.rectTransform.Place(anchor, pos, new Vector2(size, size));
            img.rectTransform.localRotation = Quaternion.Euler(0, 0, rot);
        }

        // ---------------- hikaye sayfası ----------------

        public static void Story(RectTransform root, Books.Book book, int page, Texture2D art)
        {
            Paper(root);
            Picture(root, art, new Vector2(16, -80 - 312), new Vector2(832, 624), new Vector2(0.5f, 1f));

            var text = UIKit.Node("TextBox", root);
            text.Stretch(100, 760, 70, 150);
            Words(text, book.Text(page), 60, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Normal, 1.08f);

            // sayfa numarası, iki yanında minik yıldız
            var num = UIKit.Label(root, page.ToString(), 38, Theme.TextSoft, TextAnchor.MiddleCenter, FontStyle.Bold);
            num.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(16, 80), new Vector2(120, 60));
            Deco(root, Art.Star5, Theme.Butter, new Vector2(0.5f, 0f), new Vector2(-50, 82), 30, -12);
            Deco(root, Art.Star5, Theme.Butter, new Vector2(0.5f, 0f), new Vector2(82, 82), 30, 12);
        }

        // ---------------- kapak ----------------

        public static void Cover(RectTransform root, Books.Book book, Texture2D art)
        {
            var c = book.Cover;
            if (!_cloth.TryGetValue(book.Id, out var grad) || grad == null)
                _cloth[book.Id] = grad = UIKit.Gradient(Lighten(c, 0.14f), c, Darken(c, 0.1f));
            var bg = Raw(root, "Cloth", grad, Color.white);
            bg.rectTransform.Stretch();
            // bez cilt dokusu: açık ve koyu iki kat lif
            var g1 = Raw(root, "Weave", Art.Grain, Color.white.WithAlpha(0.11f));
            g1.rectTransform.Stretch();
            g1.uvRect = new Rect(0, 0, PageStudio.PW / 110f, PageStudio.PH / 110f);
            var g2 = Raw(root, "Weave2", Art.Grain, Theme.Text.WithAlpha(0.09f));
            g2.rectTransform.Stretch();
            g2.uvRect = new Rect(0.5f, 0.25f, PageStudio.PW / 260f, PageStudio.PH / 260f);

            // sırt menteşesi
            var hinge = UIKit.Box(root, "Hinge", Darken(c, 0.22f).WithAlpha(0.55f), null);
            hinge.sprite = null; hinge.type = Image.Type.Simple;
            hinge.rectTransform.anchorMin = Vector2.zero; hinge.rectTransform.anchorMax = new Vector2(0, 1);
            hinge.rectTransform.pivot = new Vector2(0, 0.5f); hinge.rectTransform.sizeDelta = new Vector2(56, 0);
            var groove = Raw(root, "Groove", PageStudio.Gutter, Theme.Text.WithAlpha(0.3f));
            groove.rectTransform.anchorMin = Vector2.zero; groove.rectTransform.anchorMax = new Vector2(0, 1);
            groove.rectTransform.pivot = new Vector2(0, 0.5f); groove.rectTransform.sizeDelta = new Vector2(46, 0);
            groove.rectTransform.anchoredPosition = new Vector2(60, 0);
            var ridge = UIKit.Box(root, "Ridge", Color.white.WithAlpha(0.3f), null);
            ridge.sprite = null; ridge.type = Image.Type.Simple;
            ridge.rectTransform.anchorMin = Vector2.zero; ridge.rectTransform.anchorMax = new Vector2(0, 1);
            ridge.rectTransform.pivot = new Vector2(0, 0.5f); ridge.rectTransform.sizeDelta = new Vector2(4, 0);
            ridge.rectTransform.anchoredPosition = new Vector2(56, 0);

            // yaldızlı çerçeve
            var gold = Color.Lerp(Theme.Butter, Color.white, 0.2f).WithAlpha(0.85f);
            void Bar(Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
            {
                var b = UIKit.Box(root, "Gild", gold, null);
                b.sprite = null; b.type = Image.Type.Simple;
                b.rectTransform.anchorMin = aMin; b.rectTransform.anchorMax = aMax;
                b.rectTransform.anchoredPosition = pos; b.rectTransform.sizeDelta = size;
            }
            const float L = 104, M = 44;
            Bar(new Vector2(0, 1), new Vector2(1, 1), new Vector2((L - M) * 0.5f, -M), new Vector2(-(L + M), 5));
            Bar(new Vector2(0, 0), new Vector2(1, 0), new Vector2((L - M) * 0.5f, M), new Vector2(-(L + M), 5));
            Bar(new Vector2(0, 0), new Vector2(0, 1), new Vector2(L, 0), new Vector2(5, -2 * M));
            Bar(new Vector2(1, 0), new Vector2(1, 1), new Vector2(-M, 0), new Vector2(5, -2 * M));
            foreach (var corner in new[] { new Vector2(L, -M), new Vector2(-M, -M), new Vector2(L, M), new Vector2(-M, M) })
            {
                var anchor = new Vector2(corner.x > 0 ? 0 : 1, corner.y > 0 ? 0 : 1);
                Deco(root, Art.Star5, gold, anchor, corner, 40, 0);
            }

            // başlık etiketi
            var plate = UIKit.Card(root, "Plate", Theme.Paper, 0.8f, 8f);
            var prt = (RectTransform)plate.transform.parent;
            prt.Place(new Vector2(0.5f, 1f), new Vector2(30, -215), new Vector2(700, 250));
            var plateGrain = Raw(plate.transform, "Grain", Art.Grain, Theme.Text.WithAlpha(0.05f));
            plateGrain.rectTransform.Stretch(10, 10, 10, 10);
            plateGrain.uvRect = new Rect(0, 0, 4, 1.5f);
            Words((RectTransform)plate.transform, book.Title, 76, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, 0.85f, 40, 22, 40, 26);

            // paspartulu resim
            var mat = UIKit.Card(root, "Mat", Color.white, 1.4f, 10f);
            var mrt = (RectTransform)mat.transform.parent;
            mrt.Place(new Vector2(0.5f, 0.5f), new Vector2(30, -110), new Vector2(740, 570));
            mrt.localRotation = Quaternion.Euler(0, 0, -1.2f);
            if (art != null)
            {
                var r = Raw(mat.transform, "Art", art, Color.white);
                r.rectTransform.Stretch(22, 22, 22, 22);
            }

            // yaş etiketi
            var pill = UIKit.Box(root, "Age", Darken(c, 0.25f).WithAlpha(0.6f), UIKit.Rounded, 0.6f);
            pill.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(30, 130), new Vector2(220, 70));
            var age = UIKit.Label(pill.transform, Loc.T("age_tag", 3 + book.Age * 2), 38, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            age.rectTransform.Stretch();

            // cila parıltısı + kenar koyuluğu
            var sheen = Raw(root, "Sheen", PageStudio.Sheen, Color.white.WithAlpha(0.32f));
            sheen.rectTransform.Stretch();
            var vig = Raw(root, "Edge", PageStudio.Vignette, Theme.Text.WithAlpha(0.3f));
            vig.rectTransform.Stretch();
        }

        // ---------------- son sayfa ----------------

        public static void End(RectTransform root, Books.Book book, Texture2D art)
        {
            Paper(root);
            var end = UIKit.Label(root, Loc.T("the_end"), 150, Theme.AccentDark, TextAnchor.MiddleCenter, FontStyle.Bold);
            end.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(16, -250), new Vector2(820, 220));
            var sh = end.gameObject.AddComponent<Shadow>();
            sh.effectColor = Theme.Butter;
            sh.effectDistance = new Vector2(0, -8);
            Deco(root, Art.Heart, Theme.Accent, new Vector2(0.5f, 1f), new Vector2(-300, -170), 70, 18);
            Deco(root, Art.Star5, Theme.Butter, new Vector2(0.5f, 1f), new Vector2(330, -180), 80, -14);
            Deco(root, Art.Star5, Theme.Lavender, new Vector2(0.5f, 1f), new Vector2(-280, -340), 50, 10);
            Deco(root, Art.Heart, Theme.Mint, new Vector2(0.5f, 1f), new Vector2(310, -350), 54, -10);

            Picture(root, art, new Vector2(16, -40), new Vector2(600, 450), new Vector2(0.5f, 0.5f));

            var box = UIKit.Node("Box", root);
            box.Place(new Vector2(0.5f, 0f), new Vector2(16, 300), new Vector2(760, 150));
            Words(box, "~ " + book.Title + " ~", 50, Theme.TextSoft, TextAnchor.MiddleCenter, FontStyle.Italic, 1f);
            Deco(root, Art.Star5, Theme.Butter, new Vector2(0.5f, 0f), new Vector2(-50, 150), 44, -12);
            Deco(root, Art.Star5, Theme.Accent, new Vector2(0.5f, 0f), new Vector2(16, 165), 56, 0);
            Deco(root, Art.Star5, Theme.Mint, new Vector2(0.5f, 0f), new Vector2(82, 150), 44, 12);
        }
    }
}
