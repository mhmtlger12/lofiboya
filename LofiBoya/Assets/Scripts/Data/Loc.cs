using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Çok dillilik. Dil cihazdan algılanır; desteklenmiyorsa İngilizce. Ayarlardan değiştirilebilir.
    /// Metinler Strings.cs içindeki tabloda. Arapça (sağdan sola, bitişik harf) ve Hintçe (ि işareti)
    /// için gösterim düzeltmesi LocText bileşeninde otomatik yapılır; seslendirmeye düz metin gider.
    /// </summary>
    public static class Loc
    {
        public static readonly string[] Codes = { "en", "tr", "de", "fr", "es", "it", "pt", "ru", "ar", "hi" };
        public static readonly string[] NativeNames = { "English", "Türkçe", "Deutsch", "Français", "Español", "Italiano", "Português", "Русский", "العربية", "हिन्दी" };
        static readonly string[] Countries = { "US", "TR", "DE", "FR", "ES", "IT", "PT", "RU", "", "IN" };

        public static string Lang { get; private set; } = "en";
        public static int Index { get; private set; }
        public static event Action Changed;

        /// <summary>"auto" ya da dil kodu.</summary>
        public static string Setting
        {
            get => PlayerPrefs.GetString("lang", "auto");
            set { PlayerPrefs.SetString("lang", value); PlayerPrefs.Save(); Apply(); Changed?.Invoke(); }
        }

        public static string TtsCountry => Countries[Index];
        public static bool IsArabic => Lang == "ar";
        public static bool IsHindi => Lang == "hi";

        static bool _init;
        public static void Init()
        {
            if (_init) return;
            _init = true;
            Apply();
        }

        static void Apply()
        {
            var s = Setting;
            string code = s == "auto" ? DeviceLanguage() : s;
            int i = Array.IndexOf(Codes, code);
            if (i < 0) i = 0; // desteklenmeyen dil -> İngilizce
            Index = i;
            Lang = Codes[i];
        }

        /// <summary>Cihazın dili (iki harfli kod).</summary>
        public static string DeviceLanguage()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var locale = new AndroidJavaClass("java.util.Locale");
                using var def = locale.CallStatic<AndroidJavaObject>("getDefault");
                var lang = def.Call<string>("getLanguage");
                if (!string.IsNullOrEmpty(lang)) return lang.ToLowerInvariant();
            }
            catch (Exception e) { Debug.LogWarning("Dil okunamadı: " + e.Message); }
#endif
            return Application.systemLanguage switch
            {
                SystemLanguage.Turkish => "tr",
                SystemLanguage.German => "de",
                SystemLanguage.French => "fr",
                SystemLanguage.Spanish => "es",
                SystemLanguage.Italian => "it",
                SystemLanguage.Portuguese => "pt",
                SystemLanguage.Russian => "ru",
                SystemLanguage.Arabic => "ar",
                _ => "en",
            };
        }

        /// <summary>Çevrilmiş metin. Bulunamazsa İngilizcesi, o da yoksa anahtarın kendisi.</summary>
        public static string T(string key, params object[] args)
        {
            string s = Strings.Get(key, Index) ?? Strings.Get(key, 0) ?? key;
            if (args != null && args.Length > 0)
            {
                try { s = string.Format(s, args); } catch (FormatException) { }
            }
            return s;
        }

        /// <summary>Dile göre yazı tipi: Arapça (Cairo), Kiril (Nunito), diğerleri (Baloo 2 — Latin + Devanagari).</summary>
        public static Font Font
        {
            get
            {
                if (IsArabic) return _arabic ??= Resources.Load<Font>("Fonts/Arabic");
                if (Lang == "ru") return _cyrillic ??= Resources.Load<Font>("Fonts/Cyrillic");
                return null;
            }
        }
        static Font _arabic, _cyrillic, _main;

        /// <summary>Belirli bir dilin yazısını gösterebilen yazı tipi (dil seçici gibi karışık listeler için).</summary>
        public static Font FontFor(string code) => code switch
        {
            "ar" => _arabic ??= Resources.Load<Font>("Fonts/Arabic"),
            "ru" => _cyrillic ??= Resources.Load<Font>("Fonts/Cyrillic"),
            _ => _main ??= Resources.Load<Font>("Fonts/Main"),
        };

        /// <summary>Metni belirli bir dilin kurallarıyla şekillendir (arayüz dilinden bağımsız).</summary>
        public static string ShapeAs(string code, string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (code == "ar") return ArabicShaper.Shape(s);
            if (code == "hi") return Devanagari.Reorder(s);
            return s;
        }

        // ================= Gösterim düzeltmesi =================

        /// <summary>Ekranda doğru görünmesi için metni hazırla (seslendirmeye düz metin gider).</summary>
        public static string Shape(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (IsArabic) return ArabicShaper.Shape(s);
            if (IsHindi) return Devanagari.Reorder(s);
            return s;
        }
    }

    /// <summary>
    /// uGUI Text: atanan metni dile göre şekillendirir. Ham (mantıksal sıradaki) metin RawText'te durur:
    /// ekran okuyucu onu okur (şekillendirilmiş Arapça ters sıradadır, Hintçede ि yeri değişmiştir).
    /// Arapçada kelime sarması açıksa satırları kendimiz böleriz: tek parça ters çevrilen cümleyi
    /// Unity satırlara bölünce cümlenin sonu ilk satıra düşüyordu.
    /// </summary>
    public class LocText : UnityEngine.UI.Text
    {
        string _raw;
        float _wrapW = -1f;
        int _wrapSize = -1;
        bool _wrapped;

        /// <summary>Şekillendirilmemiş, okunuş sırasındaki metin (seslendirme / TalkBack için).</summary>
        public string RawText => _raw ?? base.text;

        public override string text
        {
            get => base.text;
            set
            {
                _raw = value;
                _wrapped = false;
                base.text = Render(value);
            }
        }

        /// <summary>Metni şekillendirmeden olduğu gibi göster (çağıran zaten hazırladı). raw: okunuş metni.</summary>
        public void SetDisplay(string display, string raw)
        {
            _raw = raw;
            _wrapped = false;
            base.text = display;
        }

        bool WantsArabicWrap(string v) =>
            !string.IsNullOrEmpty(v) && Loc.IsArabic && horizontalOverflow == HorizontalWrapMode.Wrap && font != null && v.IndexOf('<') < 0;

        string Render(string v)
        {
            if (!WantsArabicWrap(v)) return Loc.Shape(v);
            float w = rectTransform.rect.width;
            if (w <= 1f) return Loc.Shape(v);
            int size = resizeTextForBestFit ? resizeTextMaxSize : fontSize;
            _wrapW = w; _wrapSize = size; _wrapped = true;
            return ArabicShaper.Shape(ArabicShaper.WrapLines(v, font, size, fontStyle, w * 0.97f));
        }

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            // kutu boyutu metin atandıktan sonra belli olabilir: genişlik değiştiyse Arapça satırları yeniden böl
            if (_raw != null && WantsArabicWrap(_raw))
            {
                int size = resizeTextForBestFit ? resizeTextMaxSize : fontSize;
                float w = rectTransform.rect.width;
                if (w > 1f && (!_wrapped || Mathf.Abs(w - _wrapW) > 1f || size != _wrapSize))
                    m_Text = Render(_raw);
            }
            base.OnPopulateMesh(toFill);
        }
    }

    /// <summary>
    /// Arapça: harfleri bağlam biçimlerine (başta/ortada/sonda) çevirir ve sağdan sola görünüm için
    /// satırı ters çevirir (rakamlar ve Latin harfler kendi sırasında kalır).
    /// </summary>
    public static class ArabicShaper
    {
        // harf -> (tekil biçim kodu, iki yana bağlanır mı)
        static readonly Dictionary<char, (int iso, bool dual)> Forms = new()
        {
            ['ء'] = (0xFE80, false), ['آ'] = (0xFE81, false), ['أ'] = (0xFE83, false),
            ['ؤ'] = (0xFE85, false), ['إ'] = (0xFE87, false), ['ئ'] = (0xFE89, true),
            ['ا'] = (0xFE8D, false), ['ب'] = (0xFE8F, true), ['ة'] = (0xFE93, false),
            ['ت'] = (0xFE95, true), ['ث'] = (0xFE99, true), ['ج'] = (0xFE9D, true),
            ['ح'] = (0xFEA1, true), ['خ'] = (0xFEA5, true), ['د'] = (0xFEA9, false),
            ['ذ'] = (0xFEAB, false), ['ر'] = (0xFEAD, false), ['ز'] = (0xFEAF, false),
            ['س'] = (0xFEB1, true), ['ش'] = (0xFEB5, true), ['ص'] = (0xFEB9, true),
            ['ض'] = (0xFEBD, true), ['ط'] = (0xFEC1, true), ['ظ'] = (0xFEC5, true),
            ['ع'] = (0xFEC9, true), ['غ'] = (0xFECD, true), ['ف'] = (0xFED1, true),
            ['ق'] = (0xFED5, true), ['ك'] = (0xFED9, true), ['ل'] = (0xFEDD, true),
            ['م'] = (0xFEE1, true), ['ن'] = (0xFEE5, true), ['ه'] = (0xFEE9, true),
            ['و'] = (0xFEED, false), ['ى'] = (0xFEEF, false), ['ي'] = (0xFEF1, true),
        };

        static bool IsMark(char c) => c >= 'ً' && c <= 'ْ';
        static bool IsLetter(char c) => Forms.ContainsKey(c) || c == 'ـ';
        static bool JoinsLeft(char c) => c == 'ـ' || (Forms.TryGetValue(c, out var f) && f.dual);

        public static string Shape(string s)
        {
            // zaten şekillendirilmişse (sunum biçimleri) ya da hiç Arapça harf yoksa dokunma
            bool arabic = false;
            foreach (char c in s)
            {
                if (c >= 'ﹰ' && c <= '﻿') return s;
                if (c >= '؀' && c <= 'ۿ') arabic = true;
            }
            if (!arabic) return s;
            var lines = s.Split('\n');
            for (int i = 0; i < lines.Length; i++) lines[i] = Visual(Join(lines[i]));
            return string.Join("\n", lines);
        }

        static string Join(string s)
        {
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (IsMark(c)) continue; // harekeler: yazı tipinde konumlanamaz, atla
                if (!IsLetter(c)) { sb.Append(c); continue; }
                char prev = PrevLetter(s, i), next = NextLetter(s, i);
                // hemze (ء) hiçbir yana bağlanmaz: sonda biçimi yoktur (0xFE81 = آ olurdu)
                bool joinPrev = c != 'ء' && prev != '\0' && JoinsLeft(prev);
                // lam + elif bağı
                if (c == 'ل' && next != '\0' && (next == 'آ' || next == 'أ' || next == 'إ' || next == 'ا'))
                {
                    int lig = next switch { 'آ' => 0xFEF5, 'أ' => 0xFEF7, 'إ' => 0xFEF9, _ => 0xFEFB };
                    sb.Append((char)(lig + (joinPrev ? 1 : 0)));
                    i = IndexOfNextLetter(s, i);
                    continue;
                }
                if (c == 'ـ') { sb.Append(c); continue; }
                var f = Forms[c];
                bool joinNext = f.dual && next != '\0';
                int code;
                if (joinPrev && joinNext) code = f.iso + 3;
                else if (joinPrev) code = f.iso + 1;
                else if (joinNext) code = f.iso + 2;
                else { sb.Append(c); continue; } // tekil: temel harf (yazı tiplerinde her zaman var)
                sb.Append((char)code);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Mantıksal metni genişliğe sığacak satırlara böler (var olan satır sonları korunur).
        /// Genişlik şekillendirilmiş (bitişik) harflerle ölçülür. Her satır sonra ayrı şekillendirilir.
        /// </summary>
        public static string WrapLines(string text, Font font, int size, FontStyle style, float width)
        {
            if (font == null || string.IsNullOrEmpty(text) || width <= 1f) return text;
            var outLines = new List<string>();
            foreach (var para in text.Split('\n'))
            {
                var words = para.Split(' ');
                string cur = "";
                foreach (var w in words)
                {
                    string test = cur.Length == 0 ? w : cur + " " + w;
                    if (cur.Length > 0 && Measure(test, font, size, style) > width) { outLines.Add(cur); cur = w; }
                    else cur = test;
                }
                outLines.Add(cur);
            }
            return string.Join("\n", outLines);
        }

        static float Measure(string s, Font font, int size, FontStyle style)
        {
            var shaped = Visual(Join(s));
            font.RequestCharactersInTexture(shaped, size, style);
            float w = 0;
            foreach (char ch in shaped)
                if (font.GetCharacterInfo(ch, out var ci, size, style)) w += ci.advance;
            return w;
        }

        static char PrevLetter(string s, int i)
        {
            for (int k = i - 1; k >= 0; k--) { if (IsMark(s[k])) continue; return IsLetter(s[k]) ? s[k] : '\0'; }
            return '\0';
        }

        static char NextLetter(string s, int i)
        {
            for (int k = i + 1; k < s.Length; k++) { if (IsMark(s[k])) continue; return IsLetter(s[k]) ? s[k] : '\0'; }
            return '\0';
        }

        static int IndexOfNextLetter(string s, int i)
        {
            for (int k = i + 1; k < s.Length; k++) if (!IsMark(s[k])) return k;
            return i;
        }

        static bool IsLtr(char c)
        {
            if (c >= '؀' && c <= 'ۿ' || c >= 'ﹰ') return false;
            if (char.IsLetterOrDigit(c)) return true;
            var cat = char.GetUnicodeCategory(c);
            return cat == System.Globalization.UnicodeCategory.NonSpacingMark || cat == System.Globalization.UnicodeCategory.SpacingCombiningMark
                || c == '%' || c == '/' || c == '+' || c == '=';
        }

        /// <summary>Mantıksal sıradan görsel (soldan sağa çizilecek) sıraya.</summary>
        static string Visual(string s)
        {
            var arr = s.ToCharArray();
            Array.Reverse(arr);
            // ters dönen sayı/Latin parçalarını geri düzelt
            int i = 0;
            while (i < arr.Length)
            {
                if (!IsLtr(arr[i])) { i++; continue; }
                int j = i;
                while (j + 1 < arr.Length && (IsLtr(arr[j + 1]) || (arr[j + 1] == ' ' && j + 2 < arr.Length && IsLtr(arr[j + 2])) || arr[j + 1] == '.' && j + 2 < arr.Length && IsLtr(arr[j + 2]))) j++;
                Array.Reverse(arr, i, j - i + 1);
                i = j + 1;
            }
            for (int k = 0; k < arr.Length; k++)
                arr[k] = arr[k] switch { '(' => ')', ')' => '(', '‹' => '›', '›' => '‹', _ => arr[k] };
            return new string(arr);
        }
    }

    /// <summary>Hintçe: "ि" (i işareti) yazıda harften sonra gelir ama önünde görünmeli.</summary>
    public static class Devanagari
    {
        static bool IsConsonant(char c) => (c >= 'क' && c <= 'ह') || (c >= 'क़' && c <= 'य़');

        public static string Reorder(string s)
        {
            if (s.IndexOf('ि') < 0) return s;
            var sb = new StringBuilder(s);
            for (int i = 1; i < sb.Length; i++)
            {
                if (sb[i] != 'ि') continue;
                // geriye doğru ünsüz kümesinin başını bul: Ü (+nokta) (+virama Ü)*
                int start = i - 1;
                if (start >= 0 && sb[start] == '़') start--;
                if (start < 0 || !IsConsonant(sb[start])) continue;
                while (start >= 2 && sb[start - 1] == '्' && IsConsonant(sb[start - 2])) start -= 2;
                sb.Remove(i, 1);
                sb.Insert(start, 'ि');
            }
            return sb.ToString();
        }
    }
}
