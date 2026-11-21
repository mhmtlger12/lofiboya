using System.Collections.Generic;
using UnityEngine;

namespace LofiBoya
{
    /// <summary>Müzik ve kısa efekt sesleri. Efektler kodla üretilir (yumuşak "pop", "tık", kutlama).</summary>
    public class Sfx : MonoBehaviour
    {
        static Sfx _i;
        AudioSource _music, _fx;
        AudioClip _pop, _tap, _win, _undo;

        public static void Init()
        {
            if (_i != null) return;
            var go = new GameObject("[Audio]");
            DontDestroyOnLoad(go);
            _i = go.AddComponent<Sfx>();
            _i.Setup();
        }

        class Track { public string name; public AudioClip clip; public LofiMusic gen; }
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
            foreach (var preset in LofiMusic.Presets)
            {
                var gen = new LofiMusic(preset);
                _tracks.Add(new Track { name = preset.Name, clip = gen.CreateClip(), gen = gen });
            }
            _track = Mathf.Clamp(PlayerPrefs.GetInt("track", 0), 0, _tracks.Count - 1);
            SetTrack(_track, false);

            _fx = gameObject.AddComponent<AudioSource>();
            _fx.playOnAwake = false;

            _pop = Synth("pop", 0.16f, (t, k) => Mathf.Sin(2 * Mathf.PI * (520 + 380 * Mathf.Exp(-t * 40)) * t) * Mathf.Exp(-t * 26) * 0.55f);
            _tap = Synth("tap", 0.06f, (t, k) => Mathf.Sin(2 * Mathf.PI * 1250 * t) * Mathf.Exp(-t * 70) * 0.3f);
            _undo = Synth("undo", 0.14f, (t, k) => Mathf.Sin(2 * Mathf.PI * (700 - 1800 * t) * t) * Mathf.Exp(-t * 28) * 0.35f);
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

            ApplySettings();
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
            if (play && SaveSystem.MusicOn) _music.Play();
        }

        /// <summary>Sıradaki parçaya geç; adını döndürür. Müzik kapalıysa açar.</summary>
        public static string NextTrack()
        {
            if (_i == null) return "";
            if (!SaveSystem.MusicOn) SaveSystem.MusicOn = true;
            _i.SetTrack(_i._track + 1, true);
            return TrackName;
        }

        public static string TrackName => _i != null && _i._tracks.Count > 0 ? _i._tracks[_i._track].name : "";

        public static void ApplySettings()
        {
            if (_i == null) return;
            if (SaveSystem.MusicOn) { if (!_i._music.isPlaying) _i._music.Play(); }
            else _i._music.Stop();
        }

        static void Play(AudioClip c, float pitchJitter = 0.08f, float vol = 1f)
        {
            if (_i == null || c == null || !SaveSystem.SoundOn) return;
            _i._fx.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            _i._fx.PlayOneShot(c, vol);
        }

        public static void Pop() => Play(_i ? _i._pop : null, 0.15f);
        public static void Tap() => Play(_i ? _i._tap : null, 0.05f, 0.7f);
        public static void Undo() => Play(_i ? _i._undo : null, 0.03f);
        public static void Win() => Play(_i ? _i._win : null, 0f);
    }
}
