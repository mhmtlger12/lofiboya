using UnityEngine;

namespace LofiBoya
{
    /// <summary>Renkler, paletler ve yazı tipi. Oyunun bütün "tatlı lofi" havası buradan gelir.</summary>
    public static class Theme
    {
        public static readonly Color BgTop = Hex("FDE7E4");
        public static readonly Color BgMid = Hex("F6EAF6");
        public static readonly Color BgBottom = Hex("E3EEFB");
        public static readonly Color Card = Hex("FFFDFB");
        public static readonly Color Paper = Hex("FFFDF8");
        public static readonly Color Text = Hex("5E4B6E");
        // krem zeminde en az 4.5:1 kontrast (eski A291AE yaklaşık 3:1'di)
        public static readonly Color TextSoft = Hex("76648A");
        public static readonly Color Accent = Hex("FF9EAA");
        public static readonly Color AccentDark = Hex("F07C8C");
        // üstünde beyaz yazı olan butonlar: büyük kalın yazı için en az 3:1 kontrast
        public static readonly Color AccentButton = Hex("E8697F");
        public static readonly Color Lavender = Hex("C3B5F0");
        public static readonly Color Mint = Hex("A8E6CF");
        public static readonly Color Sky = Hex("A9D6F5");
        public static readonly Color Butter = Hex("FFE7A3");
        public static readonly Color Shadow = new Color(0.42f, 0.30f, 0.50f, 0.16f);

        public static readonly Color[] Blobs =
        {
            Hex("FFC8D0"), Hex("D9CCFF"), Hex("C6F0DF"), Hex("FFE7B8"), Hex("C9E6FF")
        };

        public struct Palette
        {
            public string Name;
            public Color[] Colors;
            public Palette(string name, params string[] hex)
            {
                Name = name;
                Colors = new Color[hex.Length];
                for (int i = 0; i < hex.Length; i++) Colors[i] = Hex(hex[i]);
            }
        }

        public static readonly Palette[] Palettes =
        {
            new Palette("Pastel", "FFADAD", "FFD6A5", "FDFFB6", "CAFFBF", "9BF6FF", "A0C4FF", "BDB2FF", "FFC6FF", "F4D6CC", "C9ADA7", "FFFFFF", "6D6875"),
            new Palette("Şeker", "FFCFD2", "FF8FAB", "FB6F92", "F1C0E8", "CFBAF0", "A3C4F3", "90DBF4", "98F5E1", "B9FBC0", "FBF8CC", "FDE4CF", "FFFFFF"),
            new Palette("Gün Batımı", "FFCDB2", "FFB4A2", "E5989B", "B5838D", "6D6875", "FEC89A", "F28482", "F6BD60", "F5CAC3", "84A59D", "F8EDEB", "3D3340"),
            new Palette("Orman", "E9EDC9", "CCD5AE", "A3B18A", "588157", "3A5A40", "FEFAE0", "FAEDCD", "D4A373", "BC6C25", "6F4518", "DAD7CD", "2F2A26"),
            new Palette("Okyanus", "CAF0F8", "ADE8F4", "90E0EF", "48CAE4", "0096C7", "0077B6", "023E8A", "FFF1E6", "F4A261", "E9C46A", "FFFFFF", "1B2A3A"),
            new Palette("Klasik", "E63946", "F4A261", "E9C46A", "2A9D8F", "457B9D", "A8DADC", "8D5524", "C68642", "F1C27D", "FFDBAC", "FFFFFF", "1D1D1D"),
        };

        static Font _font;
        /// <summary>Assets/Resources/Fonts/Main.ttf varsa onu kullanır (ör. Nunito, Fredoka), yoksa Unity'nin yerleşik yazı tipini.</summary>
        public static Font Font
        {
            get
            {
                var lf = Loc.Font; // Arapça / Rusça için ayrı yazı tipi
                if (lf != null) return lf;
                if (_font == null) _font = Resources.Load<Font>("Fonts/Main");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        public static Color WithAlpha(this Color c, float a) { c.a = a; return c; }

        public static string DifficultyTitle(string id) => id switch
        {
            "kolay" => Loc.T("easy"),
            "orta" => Loc.T("medium"),
            "zor" => Loc.T("hard"),
            _ => id
        };
    }
}
