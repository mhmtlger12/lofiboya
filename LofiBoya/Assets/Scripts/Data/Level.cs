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

        public static Level Load(string group, string id)
        {
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

        /// <summary>Doku koordinatındaki bölge numarası (1..Count, 0 = yok).</summary>
        public int RegionAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return 0;
            var c = _regionPix[y * Width + x];
            return c.r + c.g * 256;
        }

        /// <summary>Verilen bölge renkleriyle küçük bir renkli önizleme üretir (galeri için).</summary>
        public Texture2D RenderThumbnail(Color32[] regionColors, int maxSide = 360)
        {
            float s = Mathf.Min(1f, (float)maxSide / Mathf.Max(Width, Height));
            int tw = Mathf.Max(1, Mathf.RoundToInt(Width * s)), th = Mathf.Max(1, Mathf.RoundToInt(Height * s));
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
                    // komşu piksellerin çizgi değerinin ortalaması: küçültürken çizgiler kaybolmasın
                    int l = _linePix[i].r;
                    if (sx + 1 < Width) l = Mathf.Min(l, _linePix[i + 1].r);
                    if (sy + 1 < Height) l = Mathf.Min(l, _linePix[i + Width].r);
                    px[y * tw + x] = new Color32((byte)(c.r * l / 255), (byte)(c.g * l / 255), (byte)(c.b * l / 255), 255);
                }
            }
            var tex = new Texture2D(tw, th, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
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
