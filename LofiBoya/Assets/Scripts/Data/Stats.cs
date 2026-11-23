using System;
using UnityEngine;

namespace LofiBoya
{
    /// <summary>
    /// Ebeveyn raporu için günlük sayaçlar (yalnızca bu cihazda, PlayerPrefs).
    /// Anahtar: st_&lt;yyyyMMdd&gt;_&lt;alan&gt;. Alanlar: "pic" (bitirilen resim), "sec" (uygulamada saniye),
    /// "&lt;oyun&gt;_ok" / "&lt;oyun&gt;_no" (öğrenme oyununda turun İLK cevabı doğru / yanlış — deneme-yanılma sayılmaz),
    /// "&lt;oyun&gt;_done" (doğru/yanlışı olmayan oyunlarda tamamlanan tur: yazma, hafıza).
    /// </summary>
    public static class Stats
    {
        public static readonly string[] Games = { "colors", "pictures", "shapes", "counting", "feelings", "trace", "letters", "patterns", "memory", "opposites", "sequence", "shadows", "seasons", "sorting" };

        /// <summary>Doğruluk oranı anlamsız olan oyunlar: yalnızca kaç kez tamamlandığı raporlanır.</summary>
        public static bool IsDoneGame(string game) => game == "trace" || game == "memory";

        static string Key(DateTime day, string field) => "st_" + day.ToString("yyyyMMdd") + "_" + field;

        public static void Add(string field, int n = 1)
        {
            var k = Key(DateTime.Now, field);
            PlayerPrefs.SetInt(k, PlayerPrefs.GetInt(k, 0) + n);
        }

        /// <summary>Öğrenme oyununda turun ilk cevabı.</summary>
        public static void Answer(string game, bool right) => Add(game + (right ? "_ok" : "_no"));

        /// <summary>Doğru/yanlışı olmayan oyunda bir tur tamamlandı (yazma, hafıza).</summary>
        public static void Done(string game) => Add(game + "_done");

        /// <summary>Son 7 günün toplamı (bugün dahil). weeksAgo = 1: ondan önceki 7 gün.</summary>
        public static int Week(string field, int weeksAgo = 0)
        {
            int sum = 0;
            for (int i = 0; i < 7; i++) sum += PlayerPrefs.GetInt(Key(DateTime.Now.AddDays(-i - 7 * weeksAgo), field), 0);
            return sum;
        }

        /// <summary>
        /// Karışıklık kaydı: istenen şey yerine başka bir şey seçildi (ör. renk: "orange" yerine "red").
        /// Rapor "Turuncu ile kırmızıyı karıştırıyor" diyebilsin diye. Haftalık, bu cihazda.
        /// </summary>
        public static void Confused(string game, string wanted, string picked)
        {
            if (string.IsNullOrEmpty(wanted) || string.IsNullOrEmpty(picked) || wanted == picked) return;
            string pair = string.CompareOrdinal(wanted, picked) < 0 ? wanted + "+" + picked : picked + "+" + wanted;
            string list = PlayerPrefs.GetString("cf_list", "");
            string id = game + ":" + pair;
            if (!("," + list + ",").Contains("," + id + ","))
                PlayerPrefs.SetString("cf_list", list.Length == 0 ? id : list + "," + id);
            Add("cf_" + id);
        }

        /// <summary>Bu hafta en çok karıştırılan çift (oyun, a, b, kaç kez). Yoksa null.</summary>
        public static (string game, string a, string b, int n)? TopConfusion()
        {
            (string, string, string, int)? best = null;
            foreach (var id in PlayerPrefs.GetString("cf_list", "").Split(','))
            {
                if (id.Length == 0) continue;
                int n = Week("cf_" + id);
                if (n < 2 || (best.HasValue && best.Value.Item4 >= n)) continue;
                int c = id.IndexOf(':'), plus = id.IndexOf('+');
                if (c < 0 || plus < 0) continue;
                best = (id.Substring(0, c), id.Substring(c + 1, plus - c - 1), id.Substring(plus + 1), n);
            }
            return best;
        }

        /// <summary>
        /// Rapor yalnızca son 14 günü kullanır (bu hafta + geçen hafta). Daha eski günlük sayaçlar ve artık sayılmayan
        /// karışıklık çiftleri silinir; PlayerPrefs aylar içinde şişmez. Günde bir kez, açılışta çalışır.
        /// </summary>
        public static void Prune()
        {
            const int Keep = 14;
            var today = DateTime.Now.Date;
            int todayKey = int.Parse(today.ToString("yyyyMMdd"));
            int last = PlayerPrefs.GetInt("st_pruned", 0);
            if (last == todayKey) return;
            // ilk temizlikte geriye 120 gün, sonra yalnızca son temizlikten bu yana eskiyen günler
            int from = 120;
            if (last > 0 && DateTime.TryParseExact(last.ToString(), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var lastDay))
                from = Mathf.Clamp((int)(today - lastDay).TotalDays + Keep, Keep, 120);

            var fields = new System.Collections.Generic.List<string> { "pic", "sec", "book" };
            foreach (var g in Games) { fields.Add(g + "_ok"); fields.Add(g + "_no"); fields.Add(g + "_done"); }
            var cf = PlayerPrefs.GetString("cf_list", "").Split(',');
            foreach (var id in cf) if (id.Length > 0) fields.Add("cf_" + id);

            for (int d = Keep; d <= from; d++)
            {
                var day = today.AddDays(-d);
                foreach (var f in fields) PlayerPrefs.DeleteKey(Key(day, f));
            }
            // son iki haftada hiç görülmeyen karışıklık çiftleri listeden çıkar
            var keep = new System.Collections.Generic.List<string>();
            foreach (var id in cf)
                if (id.Length > 0 && Week("cf_" + id) + Week("cf_" + id, 1) > 0) keep.Add(id);
            PlayerPrefs.SetString("cf_list", string.Join(",", keep));
            PlayerPrefs.SetInt("st_pruned", todayKey);
            PlayerPrefs.Save();
        }

        /// <summary>Son 7 günde uygulamanın açık olduğu gün sayısı.</summary>
        public static int ActiveDays()
        {
            int n = 0;
            for (int i = 0; i < 7; i++) if (PlayerPrefs.GetInt(Key(DateTime.Now.AddDays(-i), "sec"), 0) > 0) n++;
            return n;
        }
    }

    /// <summary>Uygulamada geçen süreyi sayar (arka planda Update çalışmaz, sayılmaz).</summary>
    public class StatsClock : MonoBehaviour
    {
        float _acc;

        void Start() => Stats.Prune();

        void Update()
        {
            _acc += Mathf.Min(Time.unscaledDeltaTime, 1f);
            if (_acc >= 30f) Flush();
        }

        void Flush()
        {
            int s = Mathf.FloorToInt(_acc);
            if (s <= 0) return;
            _acc -= s;
            Stats.Add("sec", s);
            PlayerPrefs.Save();
        }

        void OnApplicationPause(bool paused) { if (paused) Flush(); }
        void OnApplicationQuit() => Flush();
    }
}
