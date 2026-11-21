namespace LofiBoya
{
    /// <summary>
    /// Seslendirme kaldırıldı: oyun hiçbir şeyi sesli okumaz, yönergeler ekranda yazılı ve görsel.
    /// Say() çağrıları zararsız kalsın diye boş bırakıldı.
    /// </summary>
    public static class Speech
    {
        public static void Say(string text, string key = null) { }

        /// <summary>"Kırmızı" -> "kirmizi" (dosya adı için).</summary>
        public static string Slug(string s)
        {
            s = s.ToLowerInvariant().Replace('ı', 'i').Replace('ğ', 'g').Replace('ü', 'u').Replace('ş', 's').Replace('ö', 'o').Replace('ç', 'c').Replace('İ', 'i');
            var sb = new System.Text.StringBuilder();
            foreach (var ch in s) sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
            return sb.ToString();
        }
    }
}
