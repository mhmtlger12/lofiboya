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
