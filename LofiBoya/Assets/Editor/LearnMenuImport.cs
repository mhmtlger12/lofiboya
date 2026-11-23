using UnityEditor;

namespace LofiBoya.EditorTools
{
    /// <summary>
    /// Resources/LearnMenu: Öğren menüsünün resimleri (tools/ogren/ogren_kes.py, Stitch tasarımından kesildi).
    /// Sprite olarak gelir; telefonda ASTC 4x4 (büyük, boyalı resimlerde 6x6'dan belirgin daha temiz), mipmap yok
    /// (ekranda ~1:1 gösterilir), şeffaf kenarlar renkle doldurulur.
    /// </summary>
    public class LearnMenuImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/LearnMenu/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
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
                s.format = TextureImporterFormat.ASTC_4x4;
                s.compressionQuality = 50;
                ti.SetPlatformTextureSettings(s);
            }
        }
    }
}
