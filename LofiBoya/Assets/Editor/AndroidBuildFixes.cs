using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LofiBoya.EditorTools
{
    /// <summary>
    /// Her Android build'inde otomatik çalışır (Build Profiles'tan da, menüden de):
    /// 1) AAB build'inde sürüm kodunu bir artırır -> Play Console "bu sürüm kodu zaten kullanıldı" demez.
    /// 2) Manifestteki "dik ekran zorunlu" özelliğini isteğe bağlı yapar -> Play Console
    ///    "Zorunlu donanım özelliği (android.hardware.screen.portrait)" uyarısı çıkmaz.
    /// </summary>
    public class AndroidBuildFixes : IPreprocessBuildWithReport, IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 0;

        /// <summary>BuildAab menüsü sürümü kendisi ayarladıysa tekrar artırma.</summary>
        internal static bool SkipBumpOnce;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;
            AndroidSetup.ApplySigning(false);
            if (!EditorUserBuildSettings.buildAppBundle) return;
            if (SkipBumpOnce) { SkipBumpOnce = false; return; }
            AndroidSetup.BumpVersion();
        }

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // path = .../unityLibrary ; launcher modülü de aynı klasörün yanında
            var root = Directory.GetParent(path)?.FullName ?? path;
            foreach (var manifest in Directory.GetFiles(root, "AndroidManifest.xml", SearchOption.AllDirectories))
            {
                var rel = "/" + Path.GetRelativePath(root, manifest).Replace('\\', '/');
                if (rel.Contains("/build/")) continue;
                MakeOptional(manifest, rel.StartsWith("/launcher/"), "android.hardware.screen.portrait");
            }
        }

        const string AndroidNs = "http://schemas.android.com/apk/res/android";

        static void SetAndroid(XmlDocument doc, XmlElement e, string name, string value)
        {
            var a = e.GetAttributeNode(name, AndroidNs) ?? doc.CreateAttribute("android", name, AndroidNs);
            a.Value = value;
            e.Attributes.SetNamedItem(a);
        }

        static void MakeOptional(string file, bool isLauncher, string feature)
        {
            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.Load(file);
            var manifest = doc.DocumentElement;
            if (manifest == null || manifest.Name != "manifest") return;

            bool changed = false, found = false;
            foreach (XmlNode n in manifest.ChildNodes)
            {
                if (n is not XmlElement e || e.Name != "uses-feature") continue;
                if (e.GetAttribute("name", AndroidNs) != feature) continue;
                found = true;
                if (e.GetAttribute("required", AndroidNs) != "false")
                {
                    SetAndroid(doc, e, "required", "false");
                    changed = true;
                }
            }
            // Ana (launcher) manifestte de "isteğe bağlı" olarak açıkça bildir: birleştirmede zorunlu kalmasın
            if (!found && isLauncher)
            {
                var e = doc.CreateElement("uses-feature");
                SetAndroid(doc, e, "name", feature);
                SetAndroid(doc, e, "required", "false");
                manifest.AppendChild(e);
                changed = true;
            }
            if (!changed) return;
            using (var w = new StreamWriter(file, false, new System.Text.UTF8Encoding(false))) doc.Save(w);
            Debug.Log($"[LofiBoya] {feature} -> required=false ({Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(file))))})");
        }
    }
}
