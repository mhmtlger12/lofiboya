using System;
using System.IO;
using UnityEngine;

namespace LofiBoya
{
    /// <summary>
    /// Bitmiş resmi telefonun galerisine kaydeder: Android 10+ MediaStore (izin gerekmez),
    /// "Pictures/Lofi Boya" albümüne. Eski Android'de ve editörde uygulamanın kendi klasörüne yazar.
    /// </summary>
    public static class PhotoSaver
    {
        /// <returns>Kullanıcıya gösterilecek kısa mesaj.</returns>
        public static string Save(Texture2D image, string title)
        {
            byte[] png = image.EncodeToPNG();
            string file = "LofiBoya_" + Speech.Slug(title) + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var ver = new AndroidJavaClass("android.os.Build$VERSION");
                if (ver.GetStatic<int>("SDK_INT") >= 29)
                {
                    using var up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var act = up.GetStatic<AndroidJavaObject>("currentActivity");
                    using var resolver = act.Call<AndroidJavaObject>("getContentResolver");
                    using var values = new AndroidJavaObject("android.content.ContentValues");
                    values.Call("put", "_display_name", file);
                    values.Call("put", "mime_type", "image/png");
                    values.Call("put", "relative_path", "Pictures/Lofi Boya");
                    using var media = new AndroidJavaClass("android.provider.MediaStore$Images$Media");
                    using var uriBase = media.GetStatic<AndroidJavaObject>("EXTERNAL_CONTENT_URI");
                    using var uri = resolver.Call<AndroidJavaObject>("insert", uriBase, values);
                    using var stream = resolver.Call<AndroidJavaObject>("openOutputStream", uri);
                    stream.Call("write", png);
                    stream.Call("flush");
                    stream.Call("close");
                    return "galeriye kaydedildi!";
                }
            }
            catch (Exception e) { Debug.LogWarning("Galeriye kaydedilemedi: " + e.Message); }
#endif
            try
            {
                var dir = Path.Combine(Application.persistentDataPath, "Resimlerim");
                Directory.CreateDirectory(dir);
                File.WriteAllBytes(Path.Combine(dir, file), png);
#if UNITY_EDITOR
                Debug.Log("Resim kaydedildi: " + Path.Combine(dir, file));
#endif
                return "resmin kaydedildi!";
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message);
                return "kaydedilemedi";
            }
        }

        /// <summary>Resmi kâğıt kenarlı, büyük boyutlu çıkar.</summary>
        public static Texture2D Render(Level level, Color32[] colors, Texture2D paint, int maxSide = 1600)
        {
            var img = level.RenderThumbnail(colors, paint, maxSide);
            int border = Mathf.RoundToInt(Mathf.Max(img.width, img.height) * 0.05f);
            int w = img.width + border * 2, h = img.height + border * 2;
            var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            Color32 paper = Theme.Paper;
            for (int i = 0; i < px.Length; i++) px[i] = paper;
            var src = img.GetPixels32();
            for (int y = 0; y < img.height; y++)
                Array.Copy(src, y * img.width, px, (y + border) * w + border, img.width);
            outTex.SetPixels32(px);
            outTex.Apply();
            UnityEngine.Object.Destroy(img);
            return outTex;
        }
    }
}
