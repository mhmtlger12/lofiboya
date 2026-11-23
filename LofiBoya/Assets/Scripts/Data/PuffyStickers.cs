using UnityEngine;

namespace LofiBoya
{
    /// <summary>Pofuduk çıkartmaları: bitirilen resim sayısına göre açılan şapkalı Pofuduklar.</summary>
    public static class PuffyStickers
    {
        public static readonly (MascotHat hat, int need)[] Hats =
        {
            (MascotHat.Party, 1), (MascotHat.Flower, 3), (MascotHat.Crown, 5), (MascotHat.Chef, 8),
            (MascotHat.Doctor, 12), (MascotHat.Firefighter, 16), (MascotHat.Astronaut, 20),
        };

        /// <summary>
        /// Yıldızlarla açılan Pofuduklar: öğrenme oyunlarında kazanılan yıldızların bir karşılığı olsun.
        /// Yıldız harcanmaz (eksilmez); toplam yıldız eşiği geçince açılır.
        /// </summary>
        public static readonly (MascotMood mood, int stars)[] StarMoods =
        {
            (MascotMood.Happy, 10), (MascotMood.Surprised, 25), (MascotMood.Proud, 50), (MascotMood.Calm, 80),
            (MascotMood.Shy, 120), (MascotMood.Scared, 170),
        };

        /// <summary>
        /// Okunan hikayelerle açılan Pofuduklar (kitap kurdu, tatlı rüyalar, masalcı). Ad anahtarı Strings tablosunda.
        /// </summary>
        public static readonly (MascotMood mood, int books, string name)[] BookPuffies =
        {
            (MascotMood.Happy, 1, "puffy_reader"), (MascotMood.Calm, 5, "puffy_dreamer"), (MascotMood.Proud, 21, "puffy_storyteller"),
        };

        /// <summary>Bitirilen resim sayısı (tüm gruplar).</summary>
        public static int DoneCount()
        {
            int n = 0;
            foreach (var g in Level.Index.groups)
                foreach (var l in g.levels)
                {
                    var s = SaveSystem.Load(g.name + "_" + l.id);
                    if (s != null && s.done) n++;
                }
            return n;
        }

        /// <summary>Tam bu sayıda yeni bir Pofuduk açıldıysa onun şapkası.</summary>
        public static MascotHat UnlockedAt(int done)
        {
            foreach (var (hat, need) in Hats) if (need == done) return hat;
            return MascotHat.None;
        }
    }
}
