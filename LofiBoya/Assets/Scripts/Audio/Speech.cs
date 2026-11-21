using System;
using UnityEngine;

namespace LofiBoya
{
    /// <summary>
    /// Türkçe konuşma.
    /// 1) Hazır ses: cümle parçalara bölünür ("Aferin!" + "Bu kırmızı!"); her parçanın kaydı
    ///    Resources/Voice/&lt;dil&gt;/a/&lt;fnv-hash&gt; olarak varsa sırayla çalınır (tools/ses_uret.py üretir).
    ///    Eski yol: Voice/&lt;dil&gt;/&lt;anahtar&gt; (kendi sesinizle kayıt).
    /// 2) Yoksa Android'de telefonun Türkçe seslendirme motorunu (TextToSpeech) kullanır.
    /// 3) Editörde ses yok: söylenen cümle ekranda kısa bir balon olarak görünür.
    /// </summary>
    public static class Speech
    {
        public static event Action<string> Said;

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject _tts;
        static bool _ready;
        static bool _langOk = true;   // telefonda bu dilin sesi yoksa yanlış aksanla okumayalım
        static string _pending;

        /// <summary>setLanguage sonucu: -1 (veri eksik) ve -2 (desteklenmiyor) hata demektir.</summary>
        static void ApplyLanguage()
        {
            using var locale = new AndroidJavaObject("java.util.Locale", Loc.Lang, Loc.TtsCountry);
            int r = _tts.Call<int>("setLanguage", locale);
            _langOk = r >= 0;
            if (!_langOk) Debug.LogWarning($"TTS: '{Loc.Lang}' sesi bu cihazda yok (setLanguage={r}); konuşma kapalı.");
        }

        class InitListener : AndroidJavaProxy
        {
            public InitListener() : base("android.speech.tts.TextToSpeech$OnInitListener") { }
            public void onInit(int status)
            {
                if (status != 0 || _tts == null) return;
                try
                {
                    ApplyLanguage();
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
            if (!_langOk) return;
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

            // 1) hazır ses parçaları (hepsi varsa)
            var clips = PrerecordedClips(text);
            if (clips == null && key != null)
            {
                // 2) eski tek dosya kaydı: Voice/<dil>/<anahtar> (Türkçe için Voice/<anahtar> da olur)
                var clip = Resources.Load<AudioClip>("Voice/" + Loc.Lang + "/" + key);
                if (clip == null && Loc.Lang == "tr") clip = Resources.Load<AudioClip>("Voice/" + key);
                if (clip != null) clips = new System.Collections.Generic.List<AudioClip> { clip };
            }
            if (clips != null)
            {
                Player.Play(clips);
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
                if (_tts != null && _ready) ApplyLanguage();
            }
            catch (Exception e) { Debug.LogWarning("TTS: " + e.Message); }
#endif
        }

        // ================= Hazır ses =================

        /// <summary>Cümleyi parçalara böler: ".", "!" ya da "?" ve ardından boşluk/satır sonu.
        /// tools/ses_uret.py ile aynı kural (değiştirirseniz ikisini birlikte değiştirin).</summary>
        public static System.Collections.Generic.List<string> Pieces(string text)
        {
            var list = new System.Collections.Generic.List<string>();
            int start = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if ((c == '.' || c == '!' || c == '?') && (i + 1 == text.Length || text[i + 1] == ' ' || text[i + 1] == '\n'))
                {
                    var piece = text.Substring(start, i + 1 - start).Trim();
                    if (piece.Length > 0) list.Add(piece);
                    start = i + 1;
                }
            }
            if (start < text.Length)
            {
                var rest = text.Substring(start).Trim();
                if (rest.Length > 0) list.Add(rest);
            }
            return list;
        }

        /// <summary>FNV-1a (32 bit, UTF-8) — ses dosyasının adı.</summary>
        public static string Hash(string s)
        {
            uint h = 2166136261;
            foreach (byte b in System.Text.Encoding.UTF8.GetBytes(s)) { h ^= b; h *= 16777619; }
            return h.ToString("x8");
        }

        static System.Collections.Generic.List<AudioClip> PrerecordedClips(string text)
        {
            var pieces = Pieces(text);
            if (pieces.Count == 0) return null;
            var clips = new System.Collections.Generic.List<AudioClip>();
            foreach (var p in pieces)
            {
                var clip = Resources.Load<AudioClip>("Voice/" + Loc.Lang + "/a/" + Hash(p));
                if (clip == null)
                {
#if UNITY_EDITOR
                    if (Loc.Lang == "tr") Debug.Log("Hazır ses yok: \"" + p + "\" (" + Hash(p) + ")");
#endif
                    return null;
                }
                clips.Add(clip);
            }
            return clips;
        }

        static VoicePlayer _player;
        static VoicePlayer Player
        {
            get
            {
                if (_player == null)
                {
                    var go = new GameObject("[Voice]");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    _player = go.AddComponent<VoicePlayer>();
                }
                return _player;
            }
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

    /// <summary>Hazır ses parçalarını arka arkaya çalar; yeni cümle gelince eskisini keser.</summary>
    public class VoicePlayer : MonoBehaviour
    {
        AudioSource _src;
        readonly System.Collections.Generic.Queue<AudioClip> _queue = new();
        float _gap;

        void Awake()
        {
            _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false;
        }

        public void Play(System.Collections.Generic.List<AudioClip> clips)
        {
            _queue.Clear();
            _src.Stop();
            foreach (var c in clips) _queue.Enqueue(c);
            _gap = 0;
        }

        void Update()
        {
            if (_src.isPlaying || _queue.Count == 0) return;
            _gap -= Time.unscaledDeltaTime;
            if (_gap > 0) return;
            _src.clip = _queue.Dequeue();
            _src.Play();
            _gap = 0.12f;   // parçalar arasında kısa, doğal bir duraklama
        }
    }
}
