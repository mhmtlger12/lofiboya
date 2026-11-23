using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace LofiBoya
{
    public interface IScreen
    {
        /// <summary>Android geri tuşu / Esc. true dönerse işlendi.</summary>
        bool OnBack();
    }

    /// <summary>
    /// Oyunun giriş noktası. Sahneye bir şey eklemeye gerek yok: oyun açılınca kendini kurar.
    /// Arka plan + süzülen baloncuklar kalıcıdır, ekranlar üstünde yumuşak geçişlerle değişir.
    /// </summary>
    public class App : MonoBehaviour
    {
        public static App I { get; private set; }
        public Canvas Canvas { get; private set; }
        public RectTransform SafeRoot { get; private set; }

        RectTransform _screens;
        CanvasScaler _scaler;
        Vector2Int _lastScreen;

        /// <summary>
        /// Tasarım 1080x1920 (9:16). Ekran bundan uzunsa genişliğe, kısaysa (tablet, 16:10, 4:3) yüksekliğe
        /// eşleriz: böylece kullanılabilir alan her zaman en az 1080 geniş ve 1920 yüksek olur, öğeler üst üste binmez.
        /// Ekran boyutu değişince (döndürme, katlanabilir telefon, simülatörde cihaz değişimi) yeniden hesaplanır.
        /// </summary>
        void ApplyScale()
        {
            if (_scaler == null || Screen.width <= 0) return;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);
            _scaler.matchWidthOrHeight = (float)Screen.height / Screen.width >= 1920f / 1080f ? 0f : 1f;
        }

        /// <summary>İçerik alanının (güvenli alan sütunu) genişliği: tablette 1080'den büyük olur.</summary>
        public float CanvasWidth => SafeRoot != null && SafeRoot.rect.width > 0 ? SafeRoot.rect.width
            : Canvas != null ? ((RectTransform)Canvas.transform).rect.width : 1080f;
        GameObject _current;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<App>() == null) new GameObject("LofiBoya").AddComponent<App>();
        }

        void Awake()
        {
            if (I != null) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            AllowTabletRotation();
            Input2D();
            Loc.Init();
            Sfx.Init();
            Loc.Changed += ShowWelcome;
            BuildCanvas();
            ShowWelcome();
            gameObject.AddComponent<BreakTimer>();
            gameObject.AddComponent<StatsClock>();
            gameObject.AddComponent<A11y>();   // TalkBack / Anahtarla Erişim
        }

        /// <summary>
        /// Telefon dikey kalır. Tablet (kısa kenarı ~3.4 inçten büyük) dört yöne de döner: sınıfta tabletler
        /// çoğunlukla yatay tutulur. Yatayda içerik ortada dikey bir sütundur (bkz. SafeArea).
        /// </summary>
        static void AllowTabletRotation()
        {
#if !UNITY_EDITOR
            float dpi = Screen.dpi;
            if (dpi <= 0) return;
            float shortInches = Mathf.Min(Screen.width, Screen.height) / dpi;
            if (shortInches < 3.4f) return;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = true;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
#endif
        }

        void Input2D()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
            DontDestroyOnLoad(es);
#if ENABLE_INPUT_SYSTEM
            var module = es.AddComponent<InputSystemUIInputModule>();
            if (module.actionsAsset == null) module.AssignDefaultActions();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        void BuildCanvas()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Theme.BgMid;
            }

            var go = new GameObject("Canvas", typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(transform, false);
            Canvas = go.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.pixelPerfect = false;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _scaler = scaler;
            ApplyScale();
            scaler.referencePixelsPerUnit = 100;
            go.AddComponent<GraphicRaycaster>();

            // Kalıcı arka plan
            var bg = UIKit.Node("Background", go.transform).Stretch();
            var raw = bg.gameObject.AddComponent<RawImage>();
            raw.texture = UIKit.Gradient(Theme.BgTop, Theme.BgMid, Theme.BgBottom);
            raw.raycastTarget = false;
            var decor = UIKit.Node("Decor", go.transform).Stretch();
            decor.gameObject.AddComponent<BackgroundDecor>();
            Application.quitting += SaveSystem.WaitAll;   // kapanırken arka planda yazılan kayıtlar yarım kalmasın
            UIKit.SubCanvas(decor, false);   // süzülen baloncuklar her kare tüm ekranı yeniden kurdurmasın

            SafeRoot = UIKit.Node("Safe", go.transform).Stretch();
            SafeRoot.gameObject.AddComponent<SafeArea>();
            _screens = UIKit.Node("Screens", SafeRoot).Stretch();
        }

        void Update()
        {
            if (Screen.width != _lastScreen.x || Screen.height != _lastScreen.y) ApplyScale();
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) Back();
#else
            if (Input.GetKeyDown(KeyCode.Escape)) Back();
#endif
        }

        void Back()
        {
            if (BreakTimer.Active) return; // mola sırasında geri tuşu molayı atlatmaz
            if (_current != null && _current.TryGetComponent<IScreen>(out var s) && s.OnBack()) return;
        }

        /// <summary>Yeni ekranı kurar; eskisi solarak kaybolur, yenisi hafifçe yukarı kayarak gelir.</summary>
        public T Show<T>(Action<T> setup = null) where T : MonoBehaviour
        {
            if (_current != null)
            {
                var old = _current;
                var og = old.GetComponent<CanvasGroup>();
                og.interactable = false;
                og.blocksRaycasts = false;
                Tween.Fade(og, og.alpha, 0f, 0.22f, () => Destroy(old));
            }

            var rt = UIKit.Node(typeof(T).Name, _screens).Stretch();
            var cg = rt.gameObject.AddComponent<CanvasGroup>();
            var screen = rt.gameObject.AddComponent<T>();
            setup?.Invoke(screen);
            _current = rt.gameObject;
            cg.alpha = 0;
            float slide = Motion.Reduced ? 0f : -40f;   // hareket azaltma: kaymadan, yalnızca solarak gelir
            rt.anchoredPosition = new Vector2(0, slide);
            Tween.To(rt, 0.4f, k =>
            {
                cg.alpha = k;
                rt.anchoredPosition = new Vector2(0, slide * (1 - k));
            }, Tween.OutCubic, 0.08f);
            return screen;
        }

        public void ShowWelcome() => Show<WelcomeScreen>();
        public void ShowGallery(string group = null) => Show<GalleryScreen>(s => s.Group = group);
        public void ShowColoring(string group, string id) => Show<ColoringScreen>(s => { s.Group = group; s.Id = id; });
        public void ShowStickers() => Show<StickerScreen>();
        public void ShowLearn() => Show<LearnScreen>();
        public void ShowAlbum() => Show<AlbumScreen>();
        public void ShowLibrary() => Show<LibraryScreen>();
        public void ShowBook(string id) => Show<BookScreen>(s => s.BookId = id);
        public void ShowStickerWorld() => Show<StickerWorldScreen>();
    }
}
