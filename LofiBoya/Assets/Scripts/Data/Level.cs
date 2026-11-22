using System;
using System.Collections.Generic;
using UnityEngine;

namespace LofiBoya
{
    [Serializable] public class LevelEntry { public string id; public int regions; }
    [Serializable] public class LevelGroup { public string name; public List<LevelEntry> levels = new(); }
    [Serializable] public class LevelIndex { public List<LevelGroup> groups = new(); }

    [Serializable]
    public class LevelMeta
    {
        public int width, height, regionCount;
        public int[] cx, cy, area;
        public float[] radius;
    }

    /// <summary>Dönüştürücünün ürettiği bir level: çizgi + bölge dokusu ve bölge bilgileri.</summary>
    public class Level
    {
        public string Group, Id;
        public LevelMeta Meta;
        public Texture2D Lines, Regions;
        Color32[] _regionPix, _linePix;

        public int Width => Meta.width;
        public int Height => Meta.height;
        public int Count => Meta.regionCount;
        public string Key => Group + "_" + Id;
        public string Path => $"Levels/{Group}/{Id}/";

        static LevelIndex _index;

        /// <summary>Editörde "hızlı Play" (domain reload kapalı) açıkken eski resim listesi kalmasın.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() => _index = null;
        public static LevelIndex Index
        {
            get
            {
                if (_index != null) return _index;
                var ta = Resources.Load<TextAsset>("Levels/index");
                _index = ta != null ? JsonUtility.FromJson<LevelIndex>(ta.text) : new LevelIndex();
                return _index;
            }
        }

        public static LevelGroup GetGroup(string name) => Index.groups.Find(g => g.name == name) ?? new LevelGroup { name = name };

        public static Texture2D LoadThumb(string group, string id)
        {
            var ta = Resources.Load<TextAsset>($"Levels/{group}/{id}/thumb");
            if (ta == null) return null;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(ta.bytes, true);
            tex.wrapMode = TextureWrapMode.Clamp;
            Resources.UnloadAsset(ta);
            return tex;
        }

        public const string BlankGroup = "serbest", BlankId = "bos-sayfa";
        public bool IsBlank => Group == BlankGroup;

        /// <summary>Serbest çizim için bembeyaz, tek bölgeli sayfa.</summary>
        public static Level Blank(string id = BlankId)
        {
            const int w = 960, h = 1280;
            var lv = new Level { Group = BlankGroup, Id = id };
            lv.Meta = new LevelMeta { width = w, height = h, regionCount = 1, cx = new[] { w / 2 }, cy = new[] { h / 2 }, area = new[] { w * h }, radius = new[] { w * 0.5f } };
            lv._linePix = new Color32[w * h];
            lv._regionPix = new Color32[w * h];
            var white = new Color32(255, 255, 255, 255);
            var one = new Color32(1, 0, 0, 255);
            for (int i = 0; i < w * h; i++) { lv._linePix[i] = white; lv._regionPix[i] = one; }
            lv.Lines = new Texture2D(w, h, TextureFormat.RGBA32, false, false) { wrapMode = TextureWrapMode.Clamp, name = "blank_lines" };
            lv.Lines.SetPixels32(lv._linePix);
            lv.Lines.Apply(false, false);
            lv.Regions = new Texture2D(w, h, TextureFormat.RGBA32, false, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point, name = "blank_regions" };
            lv.Regions.SetPixels32(lv._regionPix);
            lv.Regions.Apply(false, false);
            return lv;
        }

        public static Level Load(string group, string id)
        {
            if (group == BlankGroup) return Blank(id);
            var lv = new Level { Group = group, Id = id };
            var meta = Resources.Load<TextAsset>(lv.Path + "level");
            var lines = Resources.Load<TextAsset>(lv.Path + "lines");
            var regions = Resources.Load<TextAsset>(lv.Path + "regions");
            if (meta == null || lines == null || regions == null)
            {
                Debug.LogError($"Level bulunamadı: {lv.Path}");
                return null;
            }
            lv.Meta = JsonUtility.FromJson<LevelMeta>(meta.text);

            // Çizgi: sRGB, yumuşak filtre, mipmap ile uzaktan da temiz görünür
            var tmp = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tmp.LoadImage(lines.bytes);
            lv._linePix = tmp.GetPixels32();
            lv.Lines = new Texture2D(tmp.width, tmp.height, TextureFormat.RGBA32, true, false)
            {
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 2, name = id + "_lines"
            };
            lv.Lines.SetPixels32(lv._linePix);
            lv.Lines.Apply(true, false);

            // Bölge haritası: veri dokusu (linear, point, mipmap yok)
            tmp.LoadImage(regions.bytes);
            lv._regionPix = tmp.GetPixels32();
            lv.Regions = new Texture2D(tmp.width, tmp.height, TextureFormat.RGBA32, false, true)
            {
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point, name = id + "_regions"
            };
            lv.Regions.SetPixels32(lv._regionPix);
            lv.Regions.Apply(false, false);
            UnityEngine.Object.Destroy(tmp);

            Resources.UnloadAsset(meta);
            Resources.UnloadAsset(lines);
            Resources.UnloadAsset(regions);
            return lv;
        }

        /// <summary>
        /// Resmi verilen en/boy oranına kadar kenar boşluğu ekleyerek genişletir. Boşluk arka plan
        /// bölgesine (sol alt köşedeki bölge) katılır; böylece kalem, damga ve kova kartın
        /// tamamında çalışır, resmin kenarında kesilmez. Bölge alanları (ilerleme) değişmez.
        /// </summary>
        /// <summary>Asıl resmin, genişletilmiş doku içindeki yeri (0..1). Genişletilmediyse tamamı.</summary>
        public Rect OriginalRect { get; private set; } = new Rect(0, 0, 1, 1);

        public void PadToAspect(float aspect)
        {
            if (aspect <= 0f || _linePix == null) return;
            int w = Width, h = Height;
            if (Mathf.Abs(aspect - (float)w / h) < 0.01f * w / h) return;   // zaten neredeyse aynı oran (eski kayıtlar)
            int nw = w, nh = h;
            if (aspect > (float)w / h) nw = Mathf.RoundToInt(h * aspect);
            else nh = Mathf.RoundToInt(w / aspect);
            if (nw - w < 4 && nh - h < 4) return;   // zaten neredeyse aynı oran
            nw = Mathf.Min(nw, 4096); nh = Mathf.Min(nh, 4096);
            int ox = (nw - w) / 2, oy = (nh - h) / 2;

            var border = _regionPix[Mathf.Min(1, h - 1) * w + Mathf.Min(1, w - 1)];
            var white = new Color32(255, 255, 255, 255);
            var lines = new Color32[nw * nh];
            var regions = new Color32[nw * nh];
            for (int i = 0; i < lines.Length; i++) { lines[i] = white; regions[i] = border; }
            for (int y = 0; y < h; y++)
            {
                Array.Copy(_linePix, y * w, lines, (y + oy) * nw + ox, w);
                Array.Copy(_regionPix, y * w, regions, (y + oy) * nw + ox, w);
            }
            _linePix = lines;
            _regionPix = regions;

            var oldLines = Lines;
            Lines = new Texture2D(nw, nh, TextureFormat.RGBA32, oldLines.mipmapCount > 1, false)
            {
                wrapMode = TextureWrapMode.Clamp, filterMode = oldLines.filterMode, anisoLevel = oldLines.anisoLevel, name = oldLines.name
            };
            Lines.SetPixels32(lines);
            Lines.Apply(oldLines.mipmapCount > 1, false);
            UnityEngine.Object.Destroy(oldLines);

            var oldRegions = Regions;
            Regions = new Texture2D(nw, nh, TextureFormat.RGBA32, false, true)
            {
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point, name = oldRegions.name
            };
            Regions.SetPixels32(regions);
            Regions.Apply(false, false);
            UnityEngine.Object.Destroy(oldRegions);

            Meta.width = nw;
            Meta.height = nh;
            OriginalRect = new Rect((float)ox / nw, (float)oy / nh, (float)w / nw, (float)h / nh);
            if (Meta.cx != null) for (int i = 0; i < Meta.cx.Length; i++) Meta.cx[i] += ox;
            if (Meta.cy != null) for (int i = 0; i < Meta.cy.Length; i++) Meta.cy[i] += oy;
        }

        List<int>[] _neighbors;

        /// <summary>
        /// Komşu bölgeler (aralarında yalnızca kontur çizgisi olanlar). Sayılı boyamada yan yana iki bölgeye
        /// aynı numara düşmesin diye. İlk istekte bir kez hesaplanır.
        /// </summary>
        public List<int>[] Neighbors()
        {
            if (_neighbors != null) return _neighbors;
            int n = Count;
            var sets = new HashSet<int>[n + 1];
            for (int i = 0; i <= n; i++) sets[i] = new HashSet<int>();
            void Link(int a, int b)
            {
                if (a <= 0 || b <= 0 || a == b || a > n || b > n) return;
                sets[a].Add(b);
                sets[b].Add(a);
            }
            for (int y = 0; y < Height; y += 2)
                for (int x = 0; x < Width; x += 2)
                {
                    int r = RegionAt(x, y);
                    if (r <= 0) continue;
                    // çizgi kalınlığı birkaç piksel: iki mesafeye bakılır
                    Link(r, RegionAt(x + 3, y)); Link(r, RegionAt(x, y + 3));
                    Link(r, RegionAt(x + 7, y)); Link(r, RegionAt(x, y + 7));
                }
            _neighbors = new List<int>[n + 1];
            for (int i = 0; i <= n; i++) _neighbors[i] = new List<int>(sets[i]);
            return _neighbors;
        }

        /// <summary>Doku koordinatındaki bölge numarası (1..Count, 0 = yok).</summary>
        public int RegionAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return 0;
            var c = _regionPix[y * Width + x];
            return c.r + c.g * 256;
        }

        /// <summary>Verilen bölge renkleriyle küçük bir renkli önizleme üretir (galeri için).</summary>
        public Texture2D RenderThumbnail(Color32[] regionColors, int maxSide = 360) => RenderThumbnail(regionColors, null, maxSide);

        /// <param name="paint">Boya kalemi katmanı (premultiplied sRGB), yoksa null.</param>
        public Texture2D RenderThumbnail(Color32[] regionColors, Texture2D paint, int maxSide = 360)
        {
            Color32[] paintPix = paint != null ? paint.GetPixels32() : null;
            var px = RenderThumbnailPixels(regionColors, paintPix, paint != null ? paint.width : 0, paint != null ? paint.height : 0, maxSide, out int tw, out int th);
            var tex = new Texture2D(tw, th, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        /// <summary>Küçük resmin pikselleri (paintPix: boya katmanı, premultiplied; yoksa null).</summary>
        public Color32[] RenderThumbnailPixels(Color32[] regionColors, Color32[] paintPix, int pw, int ph, int maxSide, out int tw, out int th)
        {
            float s = Mathf.Min(1f, (float)maxSide / Mathf.Max(Width, Height));
            tw = Mathf.Max(1, Mathf.RoundToInt(Width * s));
            th = Mathf.Max(1, Mathf.RoundToInt(Height * s));
            var px = new Color32[tw * th];
            for (int y = 0; y < th; y++)
            {
                int sy = Mathf.Min(Height - 1, (int)((y + 0.5f) / s));
                for (int x = 0; x < tw; x++)
                {
                    int sx = Mathf.Min(Width - 1, (int)((x + 0.5f) / s));
                    int i = sy * Width + sx;
                    var rc = _regionPix[i];
                    int id = rc.r + rc.g * 256;
                    Color32 c = id > 0 && id < regionColors.Length ? regionColors[id] : (Color32)Theme.Paper;
                    if (c.a < 255) c = MagicPaint.Apply(c, MagicPaint.Decode(c.a), new Vector2(sx, sy));
                    if (paintPix != null)
                    {
                        int px2 = Mathf.Min(pw - 1, (int)((x + 0.5f) / tw * pw));
                        int py2 = Mathf.Min(ph - 1, (int)((y + 0.5f) / th * ph));
                        var p = paintPix[py2 * pw + px2];
                        if (p.a > 0)
                        {
                            int ia = 255 - p.a;
                            c = new Color32((byte)Mathf.Min(255, c.r * ia / 255 + p.r), (byte)Mathf.Min(255, c.g * ia / 255 + p.g), (byte)Mathf.Min(255, c.b * ia / 255 + p.b), 255);
                        }
                    }
                    // komşu piksellerin çizgi değerinin ortalaması: küçültürken çizgiler kaybolmasın
                    int l = _linePix[i].r;
                    if (sx + 1 < Width) l = Mathf.Min(l, _linePix[i + 1].r);
                    if (sy + 1 < Height) l = Mathf.Min(l, _linePix[i + Width].r);
                    px[y * tw + x] = new Color32((byte)(c.r * l / 255), (byte)(c.g * l / 255), (byte)(c.b * l / 255), 255);
                }
            }
            return px;
        }

        /// <summary>
        /// Gölge oyunu için siluet: resmin kenarına değen bölgeler arka plandır (saydam), geri kalan her şey
        /// tek renk. Kenarlar yumuşak (2×2 örnekleme), dış çizgi de gölgeye dahil olsun diye 1 piksel genişletilir.
        /// </summary>
        /// <summary>Resmin kenarına değen bölgeler: arka plan.</summary>
        public HashSet<int> BorderRegions()
        {
            var bg = new HashSet<int>();
            for (int x = 0; x < Width; x++) { bg.Add(RegionAt(x, 0)); bg.Add(RegionAt(x, Height - 1)); }
            for (int y = 0; y < Height; y++) { bg.Add(RegionAt(0, y)); bg.Add(RegionAt(Width - 1, y)); }
            return bg;
        }

        /// <summary>
        /// Pastel renklerle boyanmış önizleme (arka plan kâğıt rengi). share &lt; 1: bölgelerin yalnızca bir kısmı
        /// boyalı (sıralama oyununda "boyanıyor" aşaması). Aynı seed her zaman aynı renkleri verir.
        /// </summary>
        public Texture2D RenderPastel(int seed, int maxSide = 360, float share = 1f)
        {
            var pal = Theme.Palettes[0].Colors;
            var rnd = new System.Random(seed);
            var bg = BorderRegions();
            var cols = new Color32[Count + 1];
            for (int i = 0; i <= Count; i++)
            {
                Color32 c = pal[rnd.Next(pal.Length - 2)];   // her bölge için hep çekilir: share değişse de renkler aynı kalır
                bool painted = share >= 1f || (i * 37 % 100) < share * 100f;
                cols[i] = bg.Contains(i) || !painted ? (Color32)Theme.Paper : c;
            }
            return RenderThumbnail(cols, maxSide);
        }

        public Texture2D RenderSilhouette(Color32 color, int maxSide = 300)
        {
            var bg = BorderRegions();
            float s = Mathf.Min(1f, (float)maxSide / Mathf.Max(Width, Height));
            int tw = Mathf.Max(1, Mathf.RoundToInt(Width * s)), th = Mathf.Max(1, Mathf.RoundToInt(Height * s));
            var cover = new float[tw * th];
            for (int y = 0; y < th; y++)
            for (int x = 0; x < tw; x++)
            {
                int inside = 0;
                for (int sy = 0; sy < 2; sy++)
                for (int sx = 0; sx < 2; sx++)
                {
                    int px = Mathf.Min(Width - 1, (int)((x + 0.25f + sx * 0.5f) / s));
                    int py = Mathf.Min(Height - 1, (int)((y + 0.25f + sy * 0.5f) / s));
                    if (!bg.Contains(RegionAt(px, py))) inside++;
                }
                cover[y * tw + x] = inside / 4f;
            }
            var pix = new Color32[tw * th];
            for (int y = 0; y < th; y++)
            for (int x = 0; x < tw; x++)
            {
                float a = 0;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= tw || ny >= th) continue;
                    a = Mathf.Max(a, cover[ny * tw + nx] * (dx == 0 && dy == 0 ? 1f : 0.85f));
                }
                pix[y * tw + x] = new Color32(color.r, color.g, color.b, (byte)(a * color.a));
            }
            var tex = new Texture2D(tw, th, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(pix);
            tex.Apply();
            return tex;
        }

        public void Unload()
        {
            if (Lines) UnityEngine.Object.Destroy(Lines);
            if (Regions) UnityEngine.Object.Destroy(Regions);
            _regionPix = _linePix = null;
        }

        /// <summary>"gdj-polar-bear-6387496" -> "Polar Bear"</summary>
        public static string PrettyName(string id)
        {
            var parts = id.Split('-');
            var words = new List<string>();
            for (int i = 1; i < parts.Length; i++)
            {
                var p = parts[i];
                if (p.Length == 0 || char.IsDigit(p[0])) continue;
                if (p is "ai" or "generated" or "coloring" or "page" or "free" or "vector" or "images") continue;
                words.Add(char.ToUpperInvariant(p[0]) + p.Substring(1));
            }
            return words.Count > 0 ? string.Join(" ", words) : "Boyama";
        }
    }
}
