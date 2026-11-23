using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Kitap sayfalarını dokuya basar. Sayfa normal arayüz parçalarıyla (yazı, resim, kâğıt dokusu) gizli bir
    /// dünya-uzayı Canvas'ında kurulur, ayrı bir kamera onu RenderTexture'a çeker. Böylece yazılar da sayfayla
    /// birlikte kıvrılır. Kamera kapalı durur, yalnızca istendiğinde tek kare çizer.
    /// </summary>
    public class PageStudio : MonoBehaviour
    {
        /// <summary>Sayfanın tasarım boyutu (birim). Ekranda bu oranla gösterilir.</summary>
        public const float PW = 952f, PH = 1400f;
        public const float Aspect = PW / PH;
        const int Layer = 31;   // yalnızca bu kameranın gördüğü katman

        static PageStudio _i;
        Camera _cam;
        Canvas _canvas;
        CanvasScaler _scaler;
        RectTransform _root;

        public static PageStudio I
        {
            get
            {
                if (_i != null) return _i;
                var go = new GameObject("[PageStudio]");
                DontDestroyOnLoad(go);
                _i = go.AddComponent<PageStudio>();
                _i.Setup();
                return _i;
            }
        }

        void Setup()
        {
            var origin = new Vector3(20000f, 20000f, 0f);
            var cgo = new GameObject("PageCanvas", typeof(RectTransform));
            cgo.layer = Layer;
            cgo.transform.SetParent(transform, false);
            _canvas = cgo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _scaler = cgo.AddComponent<CanvasScaler>();
            _scaler.dynamicPixelsPerUnit = 1f;
            _root = (RectTransform)cgo.transform;
            _root.sizeDelta = new Vector2(PW, PH);
            _root.position = origin;
            _root.localScale = Vector3.one;

            var camGo = new GameObject("PageCamera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.position = origin + new Vector3(0, 0, -100f);
            _cam = camGo.AddComponent<Camera>();
            _cam.enabled = false;
            _cam.orthographic = true;
            _cam.orthographicSize = PH * 0.5f;
            _cam.aspect = Aspect;
            _cam.nearClipPlane = 1f;
            _cam.farClipPlane = 300f;
            _cam.cullingMask = 1 << Layer;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Theme.Paper;
            _cam.allowHDR = false;
            _cam.allowMSAA = false;
            _canvas.worldCamera = _cam;

            // ana kamera bu katmanı hiç çizmesin
            var main = Camera.main;
            if (main != null) main.cullingMask &= ~(1 << Layer);
        }

        /// <summary>Ekrandaki sayfa boyutuna (piksel) uygun bir doku açar.</summary>
        public static RenderTexture NewTexture(float screenHeightPx)
        {
            int h = Mathf.Clamp(Mathf.RoundToInt(screenHeightPx), 480, 2048);
            int w = Mathf.RoundToInt(h * Aspect);
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                name = "BookPage",
                useMipMap = true,
                autoGenerateMips = true,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4,
                wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 1,
            };
            rt.Create();
            return rt;
        }

        /// <summary>build ile kurulan sayfayı rt'ye çizer, sonra parçaları siler.</summary>
        public void Render(RenderTexture rt, Action<RectTransform> build)
        {
            if (rt == null) return;
            for (int i = _root.childCount - 1; i >= 0; i--) DestroyImmediate(_root.GetChild(i).gameObject);
            try
            {
                build(_root);
                SetLayer(_root, Layer);
                _scaler.dynamicPixelsPerUnit = rt.height / PH;
                Canvas.ForceUpdateCanvases();
                Draw(rt);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                for (int i = _root.childCount - 1; i >= 0; i--) DestroyImmediate(_root.GetChild(i).gameObject);
            }
        }

        void Draw(RenderTexture rt)
        {
            _cam.aspect = (float)rt.width / rt.height;
            var req = new RenderPipeline.StandardRequest();
            if (GraphicsSettings.currentRenderPipeline != null && RenderPipeline.SupportsRenderRequest(_cam, req))
            {
                req.destination = rt;
                RenderPipeline.SubmitRenderRequest(_cam, req);
            }
            else
            {
                _cam.targetTexture = rt;
                _cam.Render();
                _cam.targetTexture = null;
            }
        }

        static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayer(t.GetChild(i), layer);
        }

        // ---------------- sayfa dokuları (bir kez üretilir) ----------------

        static Texture2D _gutter, _vignette, _sheen;

        /// <summary>Soldan sağa sönen gölge (sırt kıvrımı).</summary>
        public static Texture2D Gutter
        {
            get
            {
                if (_gutter != null) return _gutter;
                const int n = 128;
                _gutter = new Texture2D(n, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Gutter" };
                for (int x = 0; x < n; x++)
                {
                    float t = x / (n - 1f);
                    float a = Mathf.Pow(1f - t, 2.2f);
                    _gutter.SetPixel(x, 0, new Color(1, 1, 1, a));
                }
                _gutter.Apply(false, true);
                return _gutter;
            }
        }

        /// <summary>Kenarlara doğru koyulaşan yumuşak çerçeve (eski kâğıt hissi).</summary>
        public static Texture2D Vignette
        {
            get
            {
                if (_vignette != null) return _vignette;
                const int n = 96;
                _vignette = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Vignette" };
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - n * 0.5f) / (n * 0.5f), dy = Mathf.Abs(y + 0.5f - n * 0.5f) / (n * 0.5f);
                    float d = Mathf.Max(Mathf.Pow(dx, 6f), Mathf.Pow(dy, 6f)) * 0.7f + (dx * dx + dy * dy) * 0.15f;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(d) * 255));
                }
                _vignette.SetPixels32(px);
                _vignette.Apply(false, true);
                return _vignette;
            }
        }

        /// <summary>Çapraz parlak şerit (cilalı kapak).</summary>
        public static Texture2D Sheen
        {
            get
            {
                if (_sheen != null) return _sheen;
                const int n = 128;
                _sheen = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Sheen" };
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float t = (x + (n - y)) / (2f * n);
                    float a = Mathf.Exp(-Mathf.Pow((t - 0.32f) / 0.09f, 2f)) * 0.55f + Mathf.Exp(-Mathf.Pow((t - 0.45f) / 0.03f, 2f)) * 0.25f;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
                }
                _sheen.SetPixels32(px);
                _sheen.Apply(false, true);
                return _sheen;
            }
        }
    }
}
