using System;
using System.Collections.Generic;
using UnityEngine;

namespace LofiBoya
{
    /// <summary>
    /// Emoji kütüphanesi (Microsoft Fluent Emoji 3D, MIT lisansı). Resimler tools/emoji_paketle.py ile küçültülüp
    /// kategori başına atlaslara dizildi (Resources/Emoji). Atlas sayfası ilk istendiğinde yüklenir; Unload ile bırakılır.
    /// Kategoriler: fruit, veg, food, animal, animalface, wild, nature, vehicle, place, play, object, face, people, symbol.
    /// </summary>
    public static class Emoji
    {
        [Serializable] class SheetInfo { public string file, cat; public int cols, rows; }
        [Serializable] class ItemInfo { public string id, cat; public int s, i, c; public float w, h; }
        [Serializable] class IndexInfo { public int cell, icon; public SheetInfo[] sheets; public ItemInfo[] items; }

        public static readonly string[] Categories =
            { "fruit", "veg", "food", "animal", "animalface", "wild", "nature", "vehicle", "place", "play", "object", "face", "people", "symbol" };

        static IndexInfo _index;
        static Dictionary<string, ItemInfo> _byId;
        static Dictionary<string, List<string>> _byCat;
        static Texture2D[] _sheets;
        static readonly Dictionary<string, Sprite> _sprites = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache()
        {
            _index = null;
            _sheets = null;
            _sprites.Clear();
        }

        static bool Load()
        {
            if (_index != null) return _index.items != null;
            var ta = Resources.Load<TextAsset>("Emoji/index");
            _index = ta != null ? JsonUtility.FromJson<IndexInfo>(ta.text) : new IndexInfo();
            if (ta != null) Resources.UnloadAsset(ta);
            _byId = new Dictionary<string, ItemInfo>();
            _byCat = new Dictionary<string, List<string>>();
            if (_index.items == null) return false;
            _sheets = new Texture2D[_index.sheets.Length];
            foreach (var it in _index.items)
            {
                string key = it.cat + "/" + it.id;
                _byId[key] = it;
                if (!_byId.ContainsKey(it.id)) _byId[it.id] = it;   // kısa ad: ilk bulunan
                if (!_byCat.TryGetValue(it.cat, out var list)) _byCat[it.cat] = list = new List<string>();
                list.Add(key);
            }
            return true;
        }

        public static bool Available => Load() && _index.items.Length > 0;

        /// <summary>Kategorideki emojilerin anahtarları ("kategori/ad").</summary>
        public static IReadOnlyList<string> In(string cat)
        {
            if (!Load() || !_byCat.TryGetValue(cat, out var list)) return Array.Empty<string>();
            return list;
        }

        public static bool Has(string id) => Load() && _byId.ContainsKey(id);

        /// <summary>Emojinin temel rengi (Names.Basic sırası: 0 kırmızı … 7 kahverengi), rengi tartışmalıysa -1.</summary>
        public static int ColorOf(string id) => Load() && _byId.TryGetValue(id, out var it) ? it.c : -1;

        /// <summary>Rengi belli emojiler (renk oyunu için elle seçildi).</summary>
        public static List<string> WithColor(int basic)
        {
            var list = new List<string>();
            if (!Load()) return list;
            foreach (var it in _index.items) if (it.c == basic) list.Add(it.cat + "/" + it.id);
            return list;
        }

        public static string Category(string id) => Load() && _byId.TryGetValue(id, out var it) ? it.cat : null;

        /// <summary>Emojinin sprite'ı ("kategori/ad" ya da yalnızca "ad"). Yoksa null.</summary>
        public static Sprite Get(string id)
        {
            if (string.IsNullOrEmpty(id) || !Load()) return null;
            if (!_byId.TryGetValue(id, out var it)) return null;
            string key = it.cat + "/" + it.id;
            if (_sprites.TryGetValue(key, out var sp) && sp != null) return sp;
            var tex = Sheet(it.s);
            if (tex == null) return null;
            var sh = _index.sheets[it.s];
            int cell = _index.cell, pad = (cell - _index.icon) / 2;
            int col = it.i % sh.cols, row = it.i / sh.cols;
            // PNG'de satır 0 üstte, dokuda alt: y'yi çevir
            var rect = new Rect(col * cell + pad, tex.height - (row + 1) * cell + pad, _index.icon, _index.icon);
            sp = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sp.name = key;
            _sprites[key] = sp;
            return sp;
        }

        static Texture2D Sheet(int s)
        {
            if (_sheets == null || s < 0 || s >= _sheets.Length) return null;
            if (_sheets[s] == null) _sheets[s] = Resources.Load<Texture2D>("Emoji/" + _index.sheets[s].file);
            return _sheets[s];
        }

        /// <summary>Kategoriden birbirinden farklı rastgele n emoji (yetmezse olduğu kadar).</summary>
        public static List<string> Pick(string cat, int n, Predicate<string> filter = null)
        {
            var pool = new List<string>();
            foreach (var k in In(cat)) if (filter == null || filter(k)) pool.Add(k);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            if (pool.Count > n) pool.RemoveRange(n, pool.Count - n);
            return pool;
        }

        /// <summary>Yüklü atlasları bellekten bırakır (ekran kapanırken). Sprite'lar bir sonraki istekte yeniden kurulur.</summary>
        public static void Unload()
        {
            foreach (var sp in _sprites.Values) if (sp != null) UnityEngine.Object.Destroy(sp);
            _sprites.Clear();
            if (_sheets == null) return;
            for (int i = 0; i < _sheets.Length; i++)
            {
                if (_sheets[i] != null) Resources.UnloadAsset(_sheets[i]);
                _sheets[i] = null;
            }
        }

        /// <summary>Emojili Image: beyaz (boyanmamış) ve oranını koruyan.</summary>
        public static UnityEngine.UI.Image Image(Transform parent, string id, float size)
        {
            var img = UIKit.Box(parent, "Emoji", Color.white, Get(id));
            img.type = UnityEngine.UI.Image.Type.Simple;
            img.preserveAspect = true;
            img.rectTransform.sizeDelta = new Vector2(size, size);
            return img;
        }
    }
}
