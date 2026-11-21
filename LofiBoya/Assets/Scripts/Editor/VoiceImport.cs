using UnityEditor;
using UnityEngine;

namespace LofiBoya.EditorTools
{
    /// <summary>
    /// Resources/Voice altındaki konuşma kayıtları: mono, 22 kHz, Vorbis %45, yüklenince açılır.
    /// (~1250 kısa cümle uygulamaya birkaç MB ekler; konuşma için bu kalite yeterli.)
    /// </summary>
    public class VoiceImport : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Voice/")) return;
            var imp = (AudioImporter)assetImporter;
            imp.forceToMono = true;
            imp.loadInBackground = false;
            var s = imp.defaultSampleSettings;
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.45f;
            s.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
            s.sampleRateOverride = 22050;
            s.preloadAudioData = false;
            imp.defaultSampleSettings = s;
        }
    }
}
