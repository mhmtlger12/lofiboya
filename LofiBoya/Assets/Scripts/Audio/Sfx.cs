using System.Collections.Generic;
using UnityEngine;

namespace LofiBoya
{
    /// <summary>Müzik ve kısa efekt sesleri. Efektler kodla üretilir (yumuşak "pop", "tık", kutlama).</summary>
    public class Sfx : MonoBehaviour
    {
        static Sfx _i;
        AudioSource _music;
        readonly AudioSource[] _fx = new AudioSource[4];
        int _fxNext;
        AudioClip _pop, _tap, _win, _undo;

        public static void Init()
        {
            if (_i != null) return;
            var go = new GameObject("[Audio]");
            DontDestroyOnLoad(go);
            _i = go.AddComponent<Sfx>();
            _i.Setup();
        }

        class Track { public string name; public AudioClip clip; public LofiMusic gen; public int preset = -1; }
        readonly List<Track> _tracks = new();
        int _track;

        void Setup()
        {
            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true;
            _music.playOnAwake = false;

            // Assets/Resources/Audio içindeki gerçek parçalar önce, sonra kodla üretilen lofi havaları
            foreach (var clip in Resources.LoadAll<AudioClip>("Audio"))
                _tracks.Add(new Track { name = clip.name, clip = clip });
            for (int p = 0; p < LofiMusic.Presets.Length; p++)
            {
                var preset = LofiMusic.Presets[p];
                var gen = new LofiMusic(preset);
                _tracks.Add(new Track { name = preset.Name, clip = gen.CreateClip(), gen = gen, preset = p });
            }
            _track = Mathf.Clamp(PlayerPrefs.GetInt("track", 0), 0, _tracks.Count - 1);
            SetTrack(_track, false);

            // efekt kaynakları havuzu: her efekt kendi perdesiyle çalar (tek kaynağın perdesini değiştirmek
            // çalmakta olan kutlama sesini de detone ediyordu)
            for (int i = 0; i < _fx.Length; i++)
            {
                _fx[i] = gameObject.AddComponent<AudioSource>();
                _fx[i].playOnAwake = false;
            }

            // Kalemin kâğıtta hışırtısı: döngüdeki dokulu gürültü, sesi çizim hızına göre açılır
            _scribble = gameObject.AddComponent<AudioSource>();
            _scribble.loop = true;
            _scribble.playOnAwake = false;
            _scribble.volume = 0;
            _scribble.clip = MakeScribble();

            // frekansı kayan seslerde faz = 2π ∫ f(τ) dτ (f(t)·t yazmak frekansı hedefin ötesine kaydırıyordu)
            _pop = Synth("pop", 0.16f, (t, k) => Mathf.Sin(2 * Mathf.PI * (520 * t + 9.5f * (1 - Mathf.Exp(-t * 40)))) * Mathf.Exp(-t * 26) * 0.55f);
            _tap = Synth("tap", 0.06f, (t, k) => Mathf.Sin(2 * Mathf.PI * 1250 * t) * Mathf.Exp(-t * 70) * 0.3f);
            _undo = Synth("undo", 0.14f, (t, k) => Mathf.Sin(2 * Mathf.PI * (700 * t - 900 * t * t)) * Mathf.Exp(-t * 28) * 0.35f);
            _win = Synth("win", 1.6f, (t, k) =>
            {
                float s = 0;
                float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f };
                for (int i = 0; i < notes.Length; i++)
                {
                    float st = t - i * 0.11f;
                    if (st < 0) continue;
                    s += (Mathf.Sin(2 * Mathf.PI * notes[i] * st) + 0.3f * Mathf.Sin(4 * Mathf.PI * notes[i] * st)) * Mathf.Exp(-st * 3.2f);
                }
                return s * 0.16f;
            });

            // Kıkırdama: dört kısa, inen "hi" hecesi
            _giggle = Synth("giggle", 0.55f, (t, k) =>
            {
                float s = 0;
                for (int i = 0; i < 4; i++)
                {
                    float st = t - i * 0.115f;
                    if (st < 0 || st > 0.09f) continue;
                    // f = taban + 30·sin(60t)  ->  faz/2π = taban·t + 0.5·(1 − cos(60t))
                    float cyc = (640f - i * 45f) * st + 0.5f * (1f - Mathf.Cos(st * 60f));
                    float env = Mathf.Sin(st / 0.09f * Mathf.PI);
                    s += Mathf.Sin(2 * Mathf.PI * cyc) * (0.6f + 0.4f * Mathf.Sin(2 * Mathf.PI * cyc * 3f)) * env;
                }
                return s * 0.32f;
            });
            // Alkış: üç kısa gürültü patlaması
            var rnd = new System.Random(3);
            _clap = Synth("clap", 0.9f, (t, k) =>
            {
                float s = 0;
                for (int i = 0; i < 4; i++)
                {
                    float st = t - i * 0.2f;
                    if (st < 0) continue;
                    s += ((float)rnd.NextDouble() * 2 - 1) * Mathf.Exp(-st * 38f);
                }
                return s * 0.35f;
            });
            // Yıldız: parıltılı çınlama
            _star = Synth("star", 0.9f, (t, k) =>
                (Mathf.Sin(2 * Mathf.PI * 1568f * t) * 0.6f + Mathf.Sin(2 * Mathf.PI * 2093f * t) * 0.4f * Mathf.Sin(t * 40f)) * Mathf.Exp(-t * 5f) * 0.25f);
            // Yanlış: yumuşak, üzmeyen bir "bıp"
            _wrong = Synth("wrong", 0.3f, (t, k) => Mathf.Sin(2 * Mathf.PI * (320f * t - 80f * t * t)) * Mathf.Exp(-t * 9f) * 0.4f);

            // Sayfa çevirme: kâğıdın havayı süpürmesi (yükselen-sönen hışırtı) ve sonda yumuşak bir "fıp"
            var prnd = new System.Random(12);
            float plp = 0, plp2 = 0;
            _page = Synth("page", 0.42f, (t, k) =>
            {
                float w = (float)prnd.NextDouble() * 2f - 1f;
                plp += (w - plp) * 0.35f;
                plp2 += (plp - plp2) * 0.08f;
                float band = plp - plp2;
                float env = Mathf.Sin(Mathf.Clamp01(t / 0.3f) * Mathf.PI) * Mathf.Exp(-t * 2f);
                float flap = Mathf.Sin(2 * Mathf.PI * 140f * t) * Mathf.Exp(-Mathf.Pow((t - 0.3f) / 0.03f, 2f)) * 0.5f;
                return band * env * 0.9f + flap * 0.4f;
            });

            ApplySettings();
        }

        AudioClip _page;
        public static void Page() => Play(_i ? _i._page : null, 0.12f);

        AudioClip _giggle, _clap, _star, _wrong;
        public static void Giggle() => Play(_i ? _i._giggle : null, 0.08f);
        public static void Clap() => Play(_i ? _i._clap : null, 0.05f);
        public static void Star() => Play(_i ? _i._star : null, 0.06f);
        public static void Wrong() => Play(_i ? _i._wrong : null, 0.02f);

        AudioSource _scribble;

        static AudioClip MakeScribble()
        {
            const int sr = 44100;
            int n = sr * 2;
            var data = new float[n];
            var rnd = new System.Random(9);
            float lp = 0, lp2 = 0, env = 0;
            for (int i = 0; i < n; i++)
            {
                float w = (float)rnd.NextDouble() * 2f - 1f;
                lp += (w - lp) * 0.45f;
                lp2 += (lp - lp2) * 0.12f;
                float band = lp - lp2; // ~1-5 kHz arası hışırtı
                if (rnd.NextDouble() < 0.004) env = 0.6f + (float)rnd.NextDouble() * 0.4f; // kâğıt lifine takılma
                env *= 0.9992f;
                float t = i / (float)sr;
                float wobble = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3.3f)); // ileri-geri el hareketi
                data[i] = band * (0.35f + env) * wobble * 0.9f;
            }
            // döngü dikişi görünmesin: baş ve sonu çapraz karıştır
            int fade = 2000;
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                data[i] = data[i] * k + data[n - fade + i] * (1 - k);
            }
            var clip = AudioClip.Create("scribble", n - fade, 1, sr, false);
            var trimmed = new float[n - fade];
            System.Array.Copy(data, trimmed, n - fade);
            clip.SetData(trimmed, 0);
            return clip;
        }

        /// <summary>0..1 kalem hışırtısı şiddeti (her karede çağrılır).</summary>
        public static void Scribble(float intensity)
        {
            if (_i == null || _i._scribble == null) return;
            var s = _i._scribble;
            float target = SaveSystem.SoundOn ? Mathf.Clamp01(intensity) * 0.55f : 0f;
            s.volume = Mathf.MoveTowards(s.volume, target, Time.unscaledDeltaTime * 6f);
            s.pitch = 0.9f + 0.25f * Mathf.Clamp01(intensity);
            if (s.volume > 0.001f) { if (!s.isPlaying) s.Play(); }
            else if (s.isPlaying) s.Pause();
        }

        delegate float Wave(float t, float k);

        static AudioClip Synth(string name, float seconds, Wave f)
        {
            const int sr = 44100;
            int n = Mathf.CeilToInt(seconds * sr);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                float edge = Mathf.Clamp01(i / 60f) * Mathf.Clamp01((n - i) / 200f);
                data[i] = f(t, i / (float)n) * edge;
            }
            var clip = AudioClip.Create(name, n, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        void SetTrack(int index, bool play)
        {
            _track = (index % _tracks.Count + _tracks.Count) % _tracks.Count;
            PlayerPrefs.SetInt("track", _track);
            var t = _tracks[_track];
            t.gen?.Restart();
            _music.Stop();
            _music.clip = t.clip;
            _music.volume = t.gen != null ? 1f : 0.5f;
            if (play && SaveSystem.MusicPlaying) _music.Play();
        }

        /// <summary>Sıradaki parçaya geç; adını döndürür (ebeveyn ayarı). Müzik kapalıysa açmaz.</summary>
        public static string NextTrack()
        {
            if (_i == null) return "";
            _i.SetTrack(_i._track + 1, true);
            return TrackName;
        }

        /// <summary>
        /// Ekranlardaki nota düğmesi: parçalar arasında gezer, son parçadan sonra "sessiz" olur, bir dokunuşla yeniden başlar.
        /// Ebeveyn/öğretmen müziği Ayarlar'dan kapattıysa çocuk düğmeyle açamaz (sınıfta sessizlik). Balon metnini döndürür.
        /// </summary>
        public static string MusicButton()
        {
            if (_i == null) return "";
            if (!SaveSystem.MusicOn) return Loc.T("music_locked");
            if (SaveSystem.MusicHush)
            {
                SaveSystem.MusicHush = false;
                _i.SetTrack(0, true);
                return Loc.T("music_toast", TrackName);
            }
            if (_i._track >= _i._tracks.Count - 1)
            {
                SaveSystem.MusicHush = true;
                _i._music.Stop();
                return Loc.T("music_quiet");
            }
            _i.SetTrack(_i._track + 1, true);
            return Loc.T("music_toast", TrackName);
        }

        public static string TrackName
        {
            get
            {
                if (_i == null || _i._tracks.Count == 0) return "";
                var t = _i._tracks[_i._track];
                return t.preset >= 0 ? Loc.T("track_" + t.preset) : t.name;
            }
        }

        public static void ApplySettings()
        {
            if (_i == null) return;
            if (SaveSystem.MusicPlaying) { if (!_i._music.isPlaying) _i._music.Play(); }
            else _i._music.Stop();
        }

        static void Play(AudioClip c, float pitchJitter = 0.08f, float vol = 1f)
        {
            if (_i == null || c == null || !SaveSystem.SoundOn) return;
            // boştaki bir kaynak; yoksa sıradaki (en eski) kaynak
            AudioSource src = null;
            var fx = _i._fx;
            for (int k = 0; k < fx.Length && src == null; k++) if (!fx[k].isPlaying) src = fx[k];
            if (src == null) { src = fx[_i._fxNext]; _i._fxNext = (_i._fxNext + 1) % fx.Length; }
            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            src.PlayOneShot(c, vol);
        }

        public static void Pop() => Play(_i ? _i._pop : null, 0.15f);
        public static void Tap() => Play(_i ? _i._tap : null, 0.05f, 0.7f);
        public static void Undo() => Play(_i ? _i._undo : null, 0.03f);
        public static void Win() => Play(_i ? _i._win : null, 0f);
    }
}
