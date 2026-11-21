using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LofiBoya
{
    [Serializable]
    public class LevelSave
    {
        /// <summary>Her bölgenin rengi 0xRRGGBB, boyanmamışsa -1. Dizinin 0. elemanı kullanılmaz.</summary>
        public int[] colors;
        public bool done;
        public float progress;
        /// <summary>Kalemle boyanmış bölgeler (ilerleme için).</summary>
        public bool[] brushed;
        public bool hasPaint;
    }

    /// <summary>İlerlemeyi cihazda saklar (Application.persistentDataPath/saves).</summary>
    public static class SaveSystem
    {
        static string Dir
        {
            get
            {
                var d = Path.Combine(Application.persistentDataPath, "saves");
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
                return d;
            }
        }

        static string JsonPath(string key) => Path.Combine(Dir, key + ".json");
        static string ThumbPath(string key) => Path.Combine(Dir, key + ".png");

        public static LevelSave Load(string key)
        {
            try
            {
                var p = JsonPath(key);
                return File.Exists(p) ? JsonUtility.FromJson<LevelSave>(File.ReadAllText(p)) : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Kayıt okunamadı {key}: {e.Message}");
                return null;
            }
        }

        static string PaintPath(string key) => Path.Combine(Dir, key + "_paint.png");

        public static void Save(string key, LevelSave save, Texture2D thumb, Texture2D paint = null)
        {
            try
            {
                File.WriteAllText(JsonPath(key), JsonUtility.ToJson(save));
                if (thumb != null) File.WriteAllBytes(ThumbPath(key), thumb.EncodeToPNG());
                if (paint != null && save.hasPaint) File.WriteAllBytes(PaintPath(key), paint.EncodeToPNG());
                else if (!save.hasPaint && File.Exists(PaintPath(key))) File.Delete(PaintPath(key));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Kayıt yazılamadı {key}: {e.Message}");
            }
        }

        public static void Delete(string key)
        {
            try
            {
                if (File.Exists(JsonPath(key))) File.Delete(JsonPath(key));
                if (File.Exists(ThumbPath(key))) File.Delete(ThumbPath(key));
                if (File.Exists(PaintPath(key))) File.Delete(PaintPath(key));
            }
            catch (Exception e) { Debug.LogWarning(e.Message); }
        }

        public static Texture2D LoadThumb(string key)
        {
            var p = ThumbPath(key);
            if (!File.Exists(p)) return null;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            return tex.LoadImage(File.ReadAllBytes(p), true) ? tex : null;
        }

        /// <summary>Kalem katmanı (premultiplied, sRGB baytları olduğu gibi).</summary>
        public static Texture2D LoadPaint(string key)
        {
            var p = PaintPath(key);
            if (!File.Exists(p)) return null;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            return tex.LoadImage(File.ReadAllBytes(p), false) ? tex : null;
        }

        /// <summary>Toplanan yıldızlar (boyama bitirme ve öğrenme oyunları).</summary>
        public static int Stars
        {
            get => PlayerPrefs.GetInt("stars", 0);
            set { PlayerPrefs.SetInt("stars", Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        // ---------------- Gün serisi ("3 gündür boyuyorsun!") ----------------

        static int Today => (int)(DateTime.Now.Date - new DateTime(2024, 1, 1)).TotalDays;

        /// <summary>Kaç gündür üst üste boyuyor (dün ya da bugün boyamadıysa seri bitmiştir: 0).</summary>
        public static int Streak
        {
            get
            {
                int last = PlayerPrefs.GetInt("lastDay", -10);
                return Today - last <= 1 ? PlayerPrefs.GetInt("streak", 0) : 0;
            }
        }

        public static bool PlayedToday => PlayerPrefs.GetInt("lastDay", -10) == Today;

        /// <summary>Bugün boyadı say. Yeni bir günse true döner.</summary>
        public static bool RecordPlay()
        {
            int today = Today, last = PlayerPrefs.GetInt("lastDay", -10);
            if (last == today) return false;
            int streak = last == today - 1 ? PlayerPrefs.GetInt("streak", 0) + 1 : 1;
            PlayerPrefs.SetInt("streak", streak);
            PlayerPrefs.SetInt("lastDay", today);
            PlayerPrefs.Save();
            return true;
        }

        /// <summary>Sayılı mod (1-5) açık mı.</summary>
        public static bool NumberMode
        {
            get => PlayerPrefs.GetInt("numbers", 0) == 1;
            set => PlayerPrefs.SetInt("numbers", value ? 1 : 0);
        }

        public static int ToolIndex
        {
            get => PlayerPrefs.GetInt("tool", 0);
            set => PlayerPrefs.SetInt("tool", value);
        }

        public static int BrushSize
        {
            get => PlayerPrefs.GetInt("brushSize", 1);
            set => PlayerPrefs.SetInt("brushSize", value);
        }

        public static bool MusicOn
        {
            get => PlayerPrefs.GetInt("music", 1) == 1;
            set { PlayerPrefs.SetInt("music", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool SoundOn
        {
            get => PlayerPrefs.GetInt("sound", 1) == 1;
            set { PlayerPrefs.SetInt("sound", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Kalem çizgilerin dışına taşmasın (küçükler için açık). Kapalıysa çocuk çizgiye kendisi uyar.</summary>
        public static bool PencilStaysInside
        {
            get => PlayerPrefs.GetInt("pencilInside", 1) == 1;
            set { PlayerPrefs.SetInt("pencilInside", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Mola hatırlatıcısı: kaç dakikada bir (0 = kapalı).</summary>
        public static int BreakMinutes
        {
            get => PlayerPrefs.GetInt("breakMin", 0);
            set { PlayerPrefs.SetInt("breakMin", value); PlayerPrefs.Save(); }
        }

        /// <summary>Çocuğun yaş grubu: 0 = 3–4, 1 = 5–6, 2 = 7+ (galerinin açılış sekmesi, sayma oyununun başlangıcı).</summary>
        public static int AgeBand
        {
            get => PlayerPrefs.GetInt("ageBand", 0);
            set { PlayerPrefs.SetInt("ageBand", value); PlayerPrefs.Save(); }
        }

        public static readonly string[] AgeLabels = { "3–4", "5–6", "7+" };

        /// <summary>Kaydı olan bütün resimler (en yeni önce): "grup_id" anahtarları.</summary>
        public static List<string> SavedKeys()
        {
            var list = new List<(string key, DateTime time)>();
            try
            {
                foreach (var f in Directory.GetFiles(Dir, "*.png"))
                {
                    var name = Path.GetFileNameWithoutExtension(f);
                    if (name.EndsWith("_paint")) continue;
                    list.Add((name, File.GetLastWriteTimeUtc(f)));
                }
            }
            catch (Exception e) { Debug.LogWarning(e.Message); }
            list.Sort((a, b) => b.time.CompareTo(a.time));
            return list.ConvertAll(e => e.key);
        }

        /// <summary>Yeni serbest çizim için kullanılmamış bir sayfa adı.</summary>
        public static string NewBlankId()
        {
            int n = PlayerPrefs.GetInt("blankCounter", 0) + 1;
            PlayerPrefs.SetInt("blankCounter", n);
            PlayerPrefs.Save();
            return Level.BlankId + "-" + n;
        }

        public static int PaletteIndex
        {
            get => PlayerPrefs.GetInt("palette", 0);
            set => PlayerPrefs.SetInt("palette", value);
        }
    }
}
