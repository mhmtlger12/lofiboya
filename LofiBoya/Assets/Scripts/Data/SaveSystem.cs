using System;
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

        public static void Save(string key, LevelSave save, Texture2D thumb)
        {
            try
            {
                File.WriteAllText(JsonPath(key), JsonUtility.ToJson(save));
                if (thumb != null) File.WriteAllBytes(ThumbPath(key), thumb.EncodeToPNG());
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

        public static int PaletteIndex
        {
            get => PlayerPrefs.GetInt("palette", 0);
            set => PlayerPrefs.SetInt("palette", value);
        }
    }
}
