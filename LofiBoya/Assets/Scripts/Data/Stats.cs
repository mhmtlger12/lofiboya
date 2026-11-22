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
        public static readonly string[] Games = { "colors", "pictures", "shapes", "counting", "feelings", "trace", "letters", "patterns", "memory" };

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
