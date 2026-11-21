using System;
using UnityEngine;

namespace LofiBoya
{
    /// <summary>
    /// Ebeveyn raporu için günlük sayaçlar (yalnızca bu cihazda, PlayerPrefs).
    /// Anahtar: st_&lt;yyyyMMdd&gt;_&lt;alan&gt;. Alanlar: "pic" (bitirilen resim), "sec" (uygulamada saniye),
    /// "&lt;oyun&gt;_ok" / "&lt;oyun&gt;_no" (öğrenme oyunlarında doğru / yanlış dokunuş).
    /// </summary>
    public static class Stats
    {
        public static readonly string[] Games = { "colors", "pictures", "shapes", "counting", "feelings", "trace" };

        static string Key(DateTime day, string field) => "st_" + day.ToString("yyyyMMdd") + "_" + field;

        public static void Add(string field, int n = 1)
        {
            var k = Key(DateTime.Now, field);
            PlayerPrefs.SetInt(k, PlayerPrefs.GetInt(k, 0) + n);
        }

        /// <summary>Öğrenme oyununda bir cevap.</summary>
        public static void Answer(string game, bool right) => Add(game + (right ? "_ok" : "_no"));

        /// <summary>Son 7 günün toplamı (bugün dahil).</summary>
        public static int Week(string field)
        {
            int sum = 0;
            for (int i = 0; i < 7; i++) sum += PlayerPrefs.GetInt(Key(DateTime.Now.AddDays(-i), field), 0);
            return sum;
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
