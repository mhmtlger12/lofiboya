using System;
using UnityEngine;

namespace LofiBoya
{
    /// <summary>
    /// Türkçe konuşma.
    /// 1) Assets/Resources/Voice/&lt;anahtar&gt;.wav|mp3 varsa onu çalar (kendi sesinizle kayıt).
    /// 2) Yoksa Android'de telefonun Türkçe seslendirme motorunu (TextToSpeech) kullanır.
    /// 3) Editörde ses yok: söylenen cümle ekranda kısa bir balon olarak görünür.
    /// </summary>
    public static class Speech
    {
        public static event Action<string> Said;

        static AudioSource _source;
#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject _tts;
        static bool _ready;
        static string _pending;

        class InitListener : AndroidJavaProxy
        {
            public InitListener() : base("android.speech.tts.TextToSpeech$OnInitListener") { }
            public void onInit(int status)
            {
                if (status != 0 || _tts == null) return;
                try
                {
                    var locale = new AndroidJavaObject("java.util.Locale", Loc.Lang, Loc.TtsCountry);
                    _tts.Call<int>("setLanguage", locale);
                    _tts.Call<int>("setPitch", 1.2f);       // biraz daha tatlı, çocuksu
                    _tts.Call<int>("setSpeechRate", 0.88f); // yavaş ve net
                    _ready = true;
                    if (_pending != null) { var p = _pending; _pending = null; SpeakNative(p); }
                }
                catch (Exception e) { Debug.LogWarning("TTS: " + e.Message); }
            }
        }

        static void SpeakNative(string text)
        {
            try
            {
                using var bundle = new AndroidJavaObject("android.os.Bundle");
                _tts.Call<int>("speak", text, 0, bundle, "lofi");
            }
            catch (Exception e) { Debug.LogWarning("TTS: " + e.Message); }
        }
#endif

        public static void Init()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_tts != null) return;
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                _tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, new InitListener());
            }
            catch (Exception e) { Debug.LogWarning("TTS başlatılamadı: " + e.Message); }
#endif
        }

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt("voice", 1) == 1;
            set { PlayerPrefs.SetInt("voice", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <param name="key">Kayıtlı ses dosyası adı (ör. "renk_kirmizi"). Yoksa metin okunur.</param>
        public static void Say(string text, string key = null)
        {
            if (!Enabled || string.IsNullOrEmpty(text)) return;
            Said?.Invoke(text);

            // kayıtlı ses: Voice/<dil>/<anahtar> (Türkçe için eski yer Voice/<anahtar> da olur)
            AudioClip clip = null;
            if (key != null)
            {
                clip = Resources.Load<AudioClip>("Voice/" + Loc.Lang + "/" + key);
                if (clip == null && Loc.Lang == "tr") clip = Resources.Load<AudioClip>("Voice/" + key);
            }
            if (clip != null)
            {
                if (_source == null)
                {
                    var go = new GameObject("[Voice]");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    _source = go.AddComponent<AudioSource>();
                }
                _source.Stop();
                _source.clip = clip;
                _source.Play();
                return;
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_tts == null) Init();
            if (_ready) SpeakNative(text); else _pending = text;
#endif
        }

        /// <summary>Dil değişince seslendirme motorunu yeni dile ayarla.</summary>
        public static void SetLanguage()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (_tts != null && _ready)
                {
                    using var locale = new AndroidJavaObject("java.util.Locale", Loc.Lang, Loc.TtsCountry);
                    _tts.Call<int>("setLanguage", locale);
                }
            }
            catch (Exception e) { Debug.LogWarning("TTS: " + e.Message); }
#endif
        }

        /// <summary>"Kırmızı" -> "kirmizi" (ses dosyası adı için).</summary>
        public static string Slug(string s)
        {
            s = s.ToLowerInvariant().Replace('ı', 'i').Replace('ğ', 'g').Replace('ü', 'u').Replace('ş', 's').Replace('ö', 'o').Replace('ç', 'c').Replace('İ', 'i');
            var sb = new System.Text.StringBuilder();
            foreach (var ch in s) sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
            return sb.ToString();
        }
    }
}
