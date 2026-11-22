using UnityEngine;

namespace LofiBoya
{
    /// <summary>
    /// Hareket azaltma: ebeveynin açtığı Sakin mod YA DA telefonun "Animasyonları kaldır" ayarı açıksa true.
    /// Süsleme hareketleri (süzülme, zıplama, sallanma, konfeti, açılış "pop"ları) buna bakar;
    /// işlevsel geri bildirim (seçili kalemin kalkması, solma) kalır.
    /// </summary>
    public static class Motion
    {
        static float _nextCheck = -1f;
        static bool _system;

        public static bool Reduced => SaveSystem.CalmMode || SystemReduced;

        /// <summary>Android: Ayarlar > Erişilebilirlik > Animasyonları kaldır (animator süre ölçeği 0). 2 sn'de bir okunur.</summary>
        public static bool SystemReduced
        {
            get
            {
                float now = Time.unscaledTime;
                if (now >= _nextCheck)
                {
                    _nextCheck = now + 2f;
                    _system = ReadSystem();
                }
                return _system;
            }
        }

        static bool ReadSystem()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var resolver = activity.Call<AndroidJavaObject>("getContentResolver");
                using var global = new AndroidJavaClass("android.provider.Settings$Global");
                return global.CallStatic<float>("getFloat", resolver, "animator_duration_scale", 1f) == 0f;
            }
            catch { return false; }
#else
            return false;
#endif
        }
    }
}
