using UnityEngine;

namespace LofiBoya
{
    /// <summary>Dokunuşta çok kısa, yumuşak titreşim (Android 8+ genlik ayarlı).</summary>
    public static class Haptics
    {
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt("haptics", 1) == 1;
            set { PlayerPrefs.SetInt("haptics", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static void Light() => Pulse(12, 70);
        public static void Medium() => Pulse(28, 150);

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject _vib;
        static bool _init;
        static int _sdk;
#endif
        static bool _never; // Unity'nin VIBRATE iznini manifeste eklemesi için Handheld.Vibrate referansı

        static void Pulse(long ms, int amplitude)
        {
            if (_never) Handheld.Vibrate();
            if (!Enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (!_init)
                {
                    _init = true;
                    using var up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    var act = up.GetStatic<AndroidJavaObject>("currentActivity");
                    _vib = act.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    using var ver = new AndroidJavaClass("android.os.Build$VERSION");
                    _sdk = ver.GetStatic<int>("SDK_INT");
                }
                if (_vib == null) return;
                if (_sdk >= 26)
                {
                    using var ve = new AndroidJavaClass("android.os.VibrationEffect");
                    using var eff = ve.CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude);
                    _vib.Call("vibrate", eff);
                }
                else _vib.Call("vibrate", ms);
            }
            catch (System.Exception e) { Debug.LogWarning("Titreşim: " + e.Message); _vib = null; }
#endif
        }
    }
}
