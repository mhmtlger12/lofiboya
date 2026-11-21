using System.Collections.Generic;
using UnityEngine;

namespace LofiBoya
{
    /// <summary>Renklerin ve resimlerin Türkçe adları (konuşma ve başlıklar için).</summary>
    public static class Names
    {
        // ---------------- Renkler ----------------

        /// <summary>Temel renkler: öğrenme oyununda ve sayılı modda kullanılır.</summary>
        public static readonly (string name, string acc, Color color)[] Basic =
        {
            ("kırmızı", "kırmızıyı", Theme.Hex("FF5A5F")), ("turuncu", "turuncuyu", Theme.Hex("FFA14A")), ("sarı", "sarıyı", Theme.Hex("FFD93D")),
            ("yeşil", "yeşili", Theme.Hex("6BCB77")), ("mavi", "maviyi", Theme.Hex("4D96FF")), ("mor", "moru", Theme.Hex("9B72F2")),
            ("pembe", "pembeyi", Theme.Hex("FF8FC7")), ("kahverengi", "kahverengiyi", Theme.Hex("A9744F")),
        };

        /// <summary>Baş harfi büyük (Türkçe kurallarıyla).</summary>
        public static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0], new System.Globalization.CultureInfo("tr-TR")) + s.Substring(1);

        /// <summary>Herhangi bir rengin çocukların bileceği adı.</summary>
        public static string ColorName(Color c)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            h *= 360f;
            if (v < 0.22f) return "siyah";
            if (s < 0.12f) return v > 0.85f ? "beyaz" : "gri";
            // açık ve az doygun sıcak tonlar: ten/krem
            if (s < 0.3f && v > 0.8f && (h < 50 || h > 340)) return h < 20 || h > 340 ? "pembe" : "krem";
            if (h < 15 || h >= 345) return v > 0.8f && s < 0.55f ? "pembe" : "kırmızı";
            if (h < 40) return v < 0.65f ? "kahverengi" : "turuncu";
            if (h < 65) return v < 0.6f ? "kahverengi" : "sarı";
            if (h < 160) return "yeşil";
            if (h < 195) return "turkuaz";
            if (h < 250) return v < 0.45f ? "lacivert" : "mavi";
            if (h < 290) return "mor";
            if (h < 345) return "pembe";
            return "renk";
        }

        // ---------------- Resimler ----------------

        /// <summary>İngilizce kelime -> (Türkçe ad, belirtme hâli "…ı bul" için).</summary>
        static readonly Dictionary<string, (string name, string acc)> Words = new()
        {
            ["cat"] = ("kedi", "kediyi"), ["kitten"] = ("kedi", "kediyi"),
            ["dog"] = ("köpek", "köpeği"), ["puppy"] = ("yavru köpek", "yavru köpeği"),
            ["lion"] = ("aslan", "aslanı"), ["tiger"] = ("kaplan", "kaplanı"),
            ["elephant"] = ("fil", "fili"), ["giraffe"] = ("zürafa", "zürafayı"),
            ["unicorn"] = ("tek boynuzlu at", "tek boynuzlu atı"), ["panda"] = ("panda", "pandayı"),
            ["owl"] = ("baykuş", "baykuşu"), ["fox"] = ("tilki", "tilkiyi"), ["wolf"] = ("kurt", "kurdu"),
            ["deer"] = ("geyik", "geyiği"), ["duck"] = ("ördek", "ördeği"), ["dolphin"] = ("yunus", "yunusu"),
            ["bear"] = ("ayı", "ayıyı"), ["teddybear"] = ("oyuncak ayı", "oyuncak ayıyı"),
            ["strawberry"] = ("çilek", "çileği"), ["watermelon"] = ("karpuz", "karpuzu"),
            ["ice"] = ("dondurma", "dondurmayı"), ["ghost"] = ("hayalet", "hayaleti"),
            ["witch"] = ("cadı", "cadıyı"), ["snowman"] = ("kardan adam", "kardan adamı"),
            ["easter"] = ("tavşan", "tavşanı"), ["bunny"] = ("tavşan", "tavşanı"), ["rabbit"] = ("tavşan", "tavşanı"),
            ["octopus"] = ("ahtapot", "ahtapotu"), ["seal"] = ("fok", "foku"), ["walrus"] = ("mors", "morsu"),
            ["cake"] = ("pasta", "pastayı"), ["tomato"] = ("domates", "domatesi"),
            ["monkey"] = ("maymun", "maymunu"), ["alligator"] = ("timsah", "timsahı"),
        };

        static (string name, string acc)? Lookup(string id)
        {
            var parts = id.ToLowerInvariant().Split('-');
            // "polar bear" gibi iki kelimelileri önce dene
            for (int i = 0; i + 1 < parts.Length; i++)
                if (parts[i] == "polar" && parts[i + 1] == "bear") return ("kutup ayısı", "kutup ayısını");
            for (int i = 1; i < parts.Length; i++)
                if (Words.TryGetValue(parts[i], out var w)) return w;
            return null;
        }

        /// <summary>Galeri ve başlıklarda görünen Türkçe ad.</summary>
        public static string LevelTitle(string id)
        {
            var w = Lookup(id);
            if (w == null) return Level.PrettyName(id);
            var n = w.Value.name;
            return char.ToUpper(n[0], new System.Globalization.CultureInfo("tr-TR")) + n.Substring(1);
        }

        /// <summary>Hayvan/nesne adı biliniyorsa (ad, belirtme hâli).</summary>
        public static bool TryAnimal(string id, out string name, out string accusative)
        {
            var w = Lookup(id);
            name = w?.name;
            accusative = w?.acc;
            return w != null;
        }
    }
}
