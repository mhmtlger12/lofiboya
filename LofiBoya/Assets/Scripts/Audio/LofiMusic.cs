using System;
using UnityEngine;

namespace LofiBoya
{
    /// <summary>
    /// Kodla üretilen sonsuz lofi müzik: yumuşak akor pad'i, rastgele arpej, hafif davul,
    /// bas, plak cızırtısı ve istenirse yağmur. Harici ses dosyası gerekmez.
    /// Farklı havalar <see cref="Presets"/> içinde tanımlı.
    /// </summary>
    public class LofiMusic
    {
        public class Preset
        {
            public string Name;
            public double Bpm = 72;
            public int[][] Chords;      // her akor 4 nota (MIDI)
            public int[] Roots;         // bas notaları
            public float Brightness = 0.16f;  // lowpass katsayısı (büyük = parlak)
            public float PluckDensity = 0.5f;
            public float Drums = 1f;
            public float Rain;          // 0 = yok, 1 = belirgin yağmur
            public float Bell;          // 0 = elektrik piyano, 1 = müzik kutusu çanı
            public float Birds;         // kuş cıvıltısı
            public float Waves;         // deniz dalgası
            public int Seed = 1234;
        }

        public static readonly Preset[] Presets =
        {
            new Preset
            {
                Name = "Pembe Bulutlar", Bpm = 72, Seed = 11,
                Chords = new[] { new[] { 53, 57, 60, 64 }, new[] { 52, 55, 59, 62 }, new[] { 50, 53, 57, 60 }, new[] { 48, 52, 55, 59 } },
                Roots = new[] { 41, 40, 38, 36 },
            },
            new Preset
            {
                Name = "Yağmurlu Kafe", Bpm = 66, Seed = 22, Rain = 1f, Brightness = 0.12f, PluckDensity = 0.4f, Drums = 0.8f,
                Chords = new[] { new[] { 57, 60, 64, 67 }, new[] { 53, 57, 60, 64 }, new[] { 55, 59, 60, 64 }, new[] { 55, 59, 62, 64 } },
                Roots = new[] { 45, 41, 48, 43 },
            },
            new Preset
            {
                Name = "Gece Yürüyüşü", Bpm = 76, Seed = 33, Brightness = 0.10f, PluckDensity = 0.45f, Rain = 0.25f,
                Chords = new[] { new[] { 51, 55, 58, 62 }, new[] { 51, 55, 58, 60 }, new[] { 51, 56, 60, 63 }, new[] { 50, 53, 58, 62 } },
                Roots = new[] { 39, 36, 44, 46 },
            },
            new Preset
            {
                Name = "Sabah Kahvesi", Bpm = 84, Seed = 44, Brightness = 0.2f, PluckDensity = 0.65f, Drums = 1.1f,
                Chords = new[] { new[] { 55, 59, 62, 66 }, new[] { 54, 57, 62, 64 }, new[] { 55, 59, 60, 64 }, new[] { 54, 57, 59, 62 } },
                Roots = new[] { 43, 47, 48, 50 },
            },
            new Preset
            {
                // ninni gibi: davulsuz, çan sesli müzik kutusu
                Name = "Müzik Kutusu", Bpm = 64, Seed = 55, Brightness = 0.22f, PluckDensity = 0.85f, Drums = 0f, Bell = 1f,
                Chords = new[] { new[] { 60, 64, 67, 72 }, new[] { 57, 60, 64, 69 }, new[] { 53, 57, 60, 65 }, new[] { 55, 59, 62, 67 } },
                Roots = new[] { 48, 45, 41, 43 },
            },
            new Preset
            {
                Name = "Kuş Bahçesi", Bpm = 80, Seed = 66, Brightness = 0.18f, PluckDensity = 0.55f, Drums = 0.6f, Birds = 1f, Bell = 0.4f,
                Chords = new[] { new[] { 55, 59, 62, 67 }, new[] { 52, 55, 59, 64 }, new[] { 48, 52, 55, 60 }, new[] { 50, 54, 57, 62 } },
                Roots = new[] { 43, 40, 36, 38 },
            },
            new Preset
            {
                Name = "Deniz Kenarı", Bpm = 68, Seed = 77, Brightness = 0.13f, PluckDensity = 0.4f, Drums = 0.5f, Waves = 1f,
                Chords = new[] { new[] { 52, 56, 59, 63 }, new[] { 49, 52, 56, 59 }, new[] { 45, 49, 52, 56 }, new[] { 47, 51, 54, 57 } },
                Roots = new[] { 40, 37, 45, 47 },
            },
        };

        const int SR = 44100;
        readonly Preset _p;
        readonly double _beat, _eighth, _bar;
        readonly System.Random _rng;
        long _n;
        readonly double[] _padPhase = new double[8], _padFreq = new double[8];
        double _lp1, _lp2, _hissLp, _crackle, _bassPhase, _kickPhase;
        double _pluckPhase, _pluckFreq, _pluckAmp, _pluckPan;
        // çanın tam sayı olmayan kısmi sesleri kendi fazlarında sarılır (ortak faz sarılınca her periyotta "tık" oluyordu)
        double _bellPhase2, _bellPhase3;
        double _rainLp, _rainLp2, _drop, _dropLp;
        double _birdT = 10, _birdPhase, _birdF0, _birdPan, _birdDur, _nextBird = 2;
        int _birdNotes;
        double _waveLp, _waveLp2;
        long _lastEighth = -1;
        double _kickT = 10, _snareT = 10, _hatT = 10, _hatSwing;
        volatile float _fade;   // ana iş parçacığı (Restart) ve ses iş parçacığı birlikte kullanır

        public float Volume = 0.55f;
        public string Name => _p.Name;

        public LofiMusic(Preset preset)
        {
            _p = preset;
            _rng = new System.Random(preset.Seed);
            _beat = 60.0 / preset.Bpm;
            _eighth = _beat / 2;
            _bar = _beat * 4;
        }

        public AudioClip CreateClip()
        {
            for (int i = 0; i < 8; i++) _padFreq[i] = Mtof(_p.Chords[0][i % 4]);
            return AudioClip.Create("Lofi " + _p.Name, SR * 60 * 10, 2, SR, true, OnRead);
        }

        /// <summary>Parça baştan çalınacakken yumuşakça açılsın.</summary>
        public void Restart() => _fade = 0;

        static double Mtof(int m) => 440.0 * Math.Pow(2, (m - 69) / 12.0);
        double Rand() => _rng.NextDouble();

        void OnRead(float[] data)
        {
            for (int i = 0; i < data.Length; i += 2)
            {
                Next(out float l, out float r);
                data[i] = l;
                if (i + 1 < data.Length) data[i + 1] = r;
            }
        }

        void Next(out float outL, out float outR)
        {
            double t = _n / (double)SR;
            const double dt = 1.0 / SR;
            _n++;

            int bar = (int)(t / _bar);
            int chordIdx = bar % _p.Chords.Length;
            var chord = _p.Chords[chordIdx];

            // ---- Pad: 4 ses x 2 (hafif detune), akor değişince yumuşak glide ----
            double wow = 1.0 + 0.0025 * Math.Sin(2 * Math.PI * 0.45 * t) + 0.001 * Math.Sin(2 * Math.PI * 3.1 * t);
            double pad = 0;
            for (int v = 0; v < 8; v++)
            {
                double target = Mtof(chord[v % 4]) * (v < 4 ? 1.0 : 1.004);
                _padFreq[v] += (target - _padFreq[v]) * 0.0006;
                _padPhase[v] += _padFreq[v] * wow * dt;
                if (_padPhase[v] > 1) _padPhase[v] -= 1;
                double ph = _padPhase[v] * 2 * Math.PI;
                pad += Math.Sin(ph) + 0.22 * Math.Sin(2 * ph) + 0.08 * Math.Sin(3 * ph);
            }
            double barPos = (t % _bar) / _bar;
            pad *= 0.028 * (0.75 + 0.25 * Math.Sin(2 * Math.PI * (barPos - 0.25)));

            // ---- 8'lik vuruşlar: arpej + davul tetikleri ----
            long eighth = (long)(t / _eighth);
            if (eighth != _lastEighth)
            {
                _lastEighth = eighth;
                int step = (int)(eighth % 8);
                double prob = (step % 2 == 0 ? 1.1 : 0.7) * _p.PluckDensity;
                if (Rand() < prob)
                {
                    _pluckFreq = Mtof(chord[_rng.Next(4)] + (Rand() < 0.7 ? 12 : 24));
                    _pluckAmp = 0.10 + 0.05 * Rand();
                    _pluckPan = Rand() * 0.6 - 0.3;
                }
                if (step == 0 || step == 4 || (step == 5 && Rand() < 0.35)) { _kickT = 0; _kickPhase = 0; }
                if (step == 2 || step == 6) _snareT = 0;
                _hatT = 0;
                _hatSwing = step % 2 == 1 ? 0.055 : 0;
            }

            // Pluck (yumuşak, elektrik piyano gibi)
            _pluckPhase += _pluckFreq * dt;
            if (_pluckPhase > 1) _pluckPhase -= 1;
            double pp = _pluckPhase * 2 * Math.PI;
            double ep = Math.Sin(pp) + 0.3 * Math.Sin(2 * pp) * _pluckAmp * 6;
            // müzik kutusu: uyumsuz yüksek kısmi sesler = çan/metal dil
            _bellPhase2 += _pluckFreq * 2.76 * dt;
            if (_bellPhase2 > 1) _bellPhase2 -= 1;
            _bellPhase3 += _pluckFreq * 5.4 * dt;
            if (_bellPhase3 > 1) _bellPhase3 -= 1;
            double bell = Math.Sin(pp) + 0.4 * Math.Sin(_bellPhase2 * 2 * Math.PI) + 0.2 * Math.Sin(_bellPhase3 * 2 * Math.PI);
            double pluck = (ep * (1 - _p.Bell) + bell * _p.Bell * 0.8) * _pluckAmp;
            _pluckAmp *= _p.Bell > 0.5f ? 0.99992 : 0.99988;

            // Bas
            double bassF = Mtof(_p.Roots[chordIdx]);
            _bassPhase += bassF * dt;
            if (_bassPhase > 1) _bassPhase -= 1;
            double beatPos = (t % (_beat * 2)) / (_beat * 2);
            double bass = Math.Sin(_bassPhase * 2 * Math.PI) * 0.09 * Math.Exp(-beatPos * 1.6);

            // Kick
            _kickT += dt;
            double kf = 48 + 85 * Math.Exp(-_kickT * 28);
            _kickPhase += kf * dt;
            double kick = Math.Sin(_kickPhase * 2 * Math.PI) * Math.Exp(-_kickT * 9) * 0.32 * _p.Drums;

            // Snare / fırça
            _snareT += dt;
            double noise = Rand() * 2 - 1;
            double snare = noise * Math.Exp(-_snareT * 16) * 0.075 * _p.Drums;

            // Hi-hat (swing)
            _hatT += dt;
            double ht = _hatT - _hatSwing;
            double hat = ht > 0 ? (noise - _hissLp) * Math.Exp(-ht * 70) * 0.035 * _p.Drums : 0;

            // Plak cızırtısı + hışırtı
            _hissLp += (noise - _hissLp) * 0.05;
            if (Rand() < 0.00025) _crackle = (Rand() * 2 - 1) * 0.18;
            _crackle *= 0.93;
            double vinyl = _hissLp * 0.012 + _crackle;

            // Yağmur: yumuşak hışırtı + camdaki damlalar
            double rainL = 0, rainR = 0;
            if (_p.Rain > 0)
            {
                double n2 = Rand() * 2 - 1;
                _rainLp += (n2 - _rainLp) * 0.35;
                _rainLp2 += (_rainLp - _rainLp2) * 0.08;
                double wash = (_rainLp - _rainLp2) * 0.09;
                if (Rand() < 0.0009) _drop = (Rand() * 0.5 + 0.5) * 0.12;
                _drop *= 0.985;
                _dropLp += (_drop * n2 - _dropLp) * 0.5;
                double swell = 0.8 + 0.2 * Math.Sin(2 * Math.PI * 0.07 * t);
                rainL = (wash * swell + _dropLp) * _p.Rain;
                rainR = (wash * swell * 0.9 - _dropLp * 0.6 + (Rand() * 2 - 1) * 0.004) * _p.Rain;
            }

            // Kuşlar: ara sıra 2-5 notalık hızlı cıvıltı
            double bird = 0;
            if (_p.Birds > 0)
            {
                _birdT += dt;
                if (_birdT > _nextBird)
                {
                    _birdT = 0;
                    _birdNotes = 2 + _rng.Next(4);
                    _birdF0 = 2600 + Rand() * 1600;
                    _birdDur = 0.07 + Rand() * 0.05;
                    _birdPan = Rand() * 1.2 - 0.6;
                    _nextBird = 1.5 + Rand() * 4.5;
                }
                double total = _birdNotes * _birdDur * 1.4;
                if (_birdT < total)
                {
                    double nt = _birdT % (_birdDur * 1.4);
                    if (nt < _birdDur)
                    {
                        double k = nt / _birdDur;
                        double f = _birdF0 * (1 + 0.35 * Math.Sin(k * Math.PI)) * (1 - 0.06 * (int)(_birdT / (_birdDur * 1.4)));
                        _birdPhase += f * dt;
                        bird = Math.Sin(_birdPhase * 2 * Math.PI) * Math.Sin(k * Math.PI) * 0.035 * _p.Birds;
                    }
                }
            }

            // Dalgalar: yavaşça gelip giden kumlu hışırtı
            double waveL = 0, waveR = 0;
            if (_p.Waves > 0)
            {
                double n3 = Rand() * 2 - 1;
                _waveLp += (n3 - _waveLp) * 0.06;
                _waveLp2 += (_waveLp - _waveLp2) * 0.01;
                double cyc = (t / 7.5) % 1.0;
                double swell = Math.Pow(Math.Sin(Math.PI * cyc), 3);
                double w = (_waveLp - _waveLp2 * 0.6) * 0.22 * swell * _p.Waves;
                waveL = w * (0.8 + 0.2 * Math.Sin(t * 0.3));
                waveR = w * (0.8 - 0.2 * Math.Sin(t * 0.3));
            }

            // Pad + pluck'ı tozlu bir lowpass'ten geçir
            double tonal = pad + pluck * 0.55;
            _lp1 += (tonal - _lp1) * _p.Brightness;
            _lp2 += (_lp1 - _lp2) * _p.Brightness;

            double mono = _lp2 + bass + kick + snare * 0.8 + vinyl;
            _fade = Math.Min(1f, _fade + 1f / (SR * 3f));
            double g = Volume * _fade;
            double l = Math.Tanh((mono + pluck * 0.15 * (0.5 - _pluckPan) + hat * 0.6 + rainL + waveL + bird * (0.5 - _birdPan)) * 1.2) * g;
            double r = Math.Tanh((mono + pluck * 0.15 * (0.5 + _pluckPan) + hat * 1.2 + rainR + waveR + bird * (0.5 + _birdPan)) * 1.2) * g;
            outL = (float)l;
            outR = (float)r;
        }
    }
}
