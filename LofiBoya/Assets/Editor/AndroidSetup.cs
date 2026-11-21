using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace LofiBoya.EditorTools
{
    /// <summary>
    /// Play Store ayarları ve imza.
    /// İmza anahtarı repo DIŞINDA tutulur: games_v2/keys/upload-keystore.jks + key.properties (.gitignore'da).
    /// Şifreler hiçbir dosyaya yazılmaz; editör her açıldığında key.properties'ten belleğe alınır.
    /// </summary>
    [InitializeOnLoad]
    public static class AndroidSetup
    {
        public const string PackageName = "com.snapload.app";   // eski uygulamanın paket adı (aynı mağaza kaydı)
        public const string VersionName = "2.0.0";
        public const int VersionCode = 42;                       // eski sürüm 1.1.3 (41) -> daha büyük olmalı

        static string KeysDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "keys"));

        static AndroidSetup()
        {
            EditorApplication.delayCall += () => ApplySigning(false);
        }

        [MenuItem("Lofi Boya/Android Ayarlarını Uygula")]
        public static void ApplyAll()
        {
            var t = NamedBuildTarget.Android;
            PlayerSettings.companyName = "LofiBoya";
            PlayerSettings.productName = "Lofi Boya";
            PlayerSettings.SetApplicationIdentifier(t, PackageName);
            PlayerSettings.bundleVersion = VersionName;
            PlayerSettings.Android.bundleVersionCode = VersionCode;
            PlayerSettings.SetScriptingBackend(t, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.Android.renderOutsideSafeArea = false;
            PlayerSettings.runInBackground = false;
            EditorUserBuildSettings.buildAppBundle = true;
            ApplySigning(true);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LofiBoya] Android: {PackageName} v{VersionName} ({VersionCode}), IL2CPP ARM64+ARMv7, AAB");
        }

        /// <summary>keys/key.properties varsa imzayı ayarla.</summary>
        public static bool ApplySigning(bool log)
        {
            var props = Path.Combine(KeysDir, "key.properties");
            var jks = Path.Combine(KeysDir, "upload-keystore.jks");
            if (!File.Exists(props) || !File.Exists(jks))
            {
                if (log) Debug.LogWarning("[LofiBoya] İmza anahtarı bulunamadı: " + KeysDir);
                return false;
            }
            var map = new Dictionary<string, string>();
            foreach (var line in File.ReadAllLines(props))
            {
                int i = line.IndexOf('=');
                if (i > 0) map[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
            }
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = jks;
            PlayerSettings.Android.keystorePass = map.GetValueOrDefault("storePassword", "");
            PlayerSettings.Android.keyaliasName = map.GetValueOrDefault("keyAlias", "upload");
            PlayerSettings.Android.keyaliasPass = map.GetValueOrDefault("keyPassword", "");
            if (log) Debug.Log("[LofiBoya] İmza: " + jks + " / alias " + PlayerSettings.Android.keyaliasName);
            return true;
        }

        [MenuItem("Lofi Boya/Play Store için AAB Oluştur")]
        public static void BuildAab()
        {
            ApplyAll();
            if (!ApplySigning(true)) { Debug.LogError("[LofiBoya] İmza yok, AAB oluşturulmadı."); return; }
            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds"));
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"LofiBoya-{VersionName}-{VersionCode}.aab");
            var scenes = new List<string>();
            foreach (var s in EditorBuildSettings.scenes) if (s.enabled) scenes.Add(s.path);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = path,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            });
            Debug.Log($"[LofiBoya] Build: {report.summary.result} -> {path} ({report.summary.totalSize / (1024 * 1024)} MB)");
        }
    }
}
