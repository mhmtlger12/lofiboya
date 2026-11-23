using UnityEditor;

namespace LofiBoya.EditorTools
{
    /// <summary>
    /// Resources/Emoji atlasları: telefonda ASTC 6x6 ile sıkıştırılır (PNG'nin ~1/9'u), mipmap yok (emoji ~1:1 boyda
    /// gösterilir, atlasta komşu sızmasın), şeffaf kenarlar renkle doldurulur (kenarda koyu hale olmasın).
    /// </summary>
    public class EmojiImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Emoji/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = true;
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            ti.filterMode = UnityEngine.FilterMode.Bilinear;
            ti.isReadable = false;
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.Compressed;
            foreach (var platform in new[] { "Android", "iPhone" })
            {
                var s = ti.GetPlatformTextureSettings(platform);
                s.overridden = true;
                s.maxTextureSize = 2048;
                s.format = TextureImporterFormat.ASTC_6x6;
                s.compressionQuality = 50;
                ti.SetPlatformTextureSettings(s);
            }
        }
    }
}
