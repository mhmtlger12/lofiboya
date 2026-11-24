using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Kitap okuma ekranı. Sayfalar 3B olarak kıvrılarak döner (PageCurl): parmakla sayfanın herhangi bir yerinden
    /// tutup çekmek, sağ/sol kenara dokunmak ya da alttaki oklar. Sayfa 0 kapak, 1..N hikaye, N+1 "Son".
    /// Resimdeki karakterlere dokununca kalpler/yıldızlar fışkırır; sayma kitabında dokunulan her öğe sayılır.
    /// </summary>
    public class BookScreen : MonoBehaviour, IScreen
    {
        public string BookId;

        /// <summary>Kitaplıktan gelirken önceden çizilmiş küçük kapak: kitap beklemeden açılır, büyüğü gelince netleşir.</summary>
        public static Texture2D PendingPreview;
        public static string PendingPreviewId;

        Books.Book _book;
        int _count;          // toplam sayfa (kapak + hikaye + son)
        int _cur;            // gösterilen sayfa
        readonly Dictionary<int, Texture2D> _art = new();          // sahne resimleri (0: kapak sahnesi)
        readonly Dictionary<int, RenderTexture> _pages = new();    // basılmış sayfalar
        readonly HashSet<int> _stale = new();                      // yeniden basılacaklar (resim geldi / doku kayboldu)

        RectTransform _root, _bookHolder, _view, _stack;
        PageCurl _curl;
        PageMesh _flat, _curlFront, _back, _ghost, _backShadow, _under;
        CurlShadow _underShadow, _crease;
        RectTransform _dots, _endBar, _fx;
        readonly List<Image> _dotImgs = new();
        Button _prev, _next;
        Text _title;
        CanvasGroup _titleGroup;
        float _pageH, _pageW;
        Vector2 _lastSize;

        // sürükleme
        enum Mode { Idle, Drag, Anim }
        Mode _mode;
        int _turnPage;      // kıvrılan sayfa
        int _dir;           // +1 ileri, -1 geri
        Vector2 _dragStart, _gStart, _vel, _lastPos;
        float _lastTime;
        Coroutine _anim;
        float _idleSince;
        int _peeks;
        bool _peeking;
        bool _celebrated;
        Buddy _buddy;

        // dokunulabilir resim: sayma kitabında bu sayfada sayılanlar
        readonly HashSet<int> _counted = new();
        readonly List<GameObject> _badges = new();

        static Texture2D _paperBack, _ribbon;

        // Sayfa tasarımında resmin yeri (BookPages.Story ile aynı): 952×1400 içinde x 76..908, y 696..1320
        const float PicX = 76f, PicY = 696f, PicW = 832f, PicH = 624f, ArtAspect = 4f / 3f;

        void Start()
        {
            _root = (RectTransform)transform;
            _book = Books.Find(BookId) ?? Books.All[0];
            _count = _book.Pages + 2;
            _idleSince = Time.unscaledTime;

            var back = UIKit.CircleButton(_root, "‹", Theme.Card, Theme.Text, 120, 90, () => App.I.ShowLibrary());
            ((RectTransform)back.transform).Place(new Vector2(0, 1), new Vector2(110, -110), new Vector2(120, 120));
            A11y.Label(back, Loc.T("to_library"));

            _title = UIKit.Label(_root, _book.Title, 52, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            _title.rectTransform.Band(true, 120, 50, 190);
            _title.horizontalOverflow = HorizontalWrapMode.Wrap;
            _title.verticalOverflow = VerticalWrapMode.Truncate;
            _title.resizeTextForBestFit = true;
            _title.resizeTextMinSize = 30;
            _title.resizeTextMaxSize = 52;
            _title.lineSpacing = 0.85f;
            _titleGroup = _title.gameObject.AddComponent<CanvasGroup>();
            _titleGroup.alpha = 0f;   // kapakta başlık zaten büyükçe yazıyor

            // kitaplıktan gelen küçük kapak hemen kullanılsın
            if (PendingPreview != null && PendingPreviewId == _book.Id) _art[0] = PendingPreview;
            else if (PendingPreview != null) Destroy(PendingPreview);
            PendingPreview = null;
            PendingPreviewId = null;

            // tüm sahneleri arka planda boya (sırayla, en çok ikisi aynı anda); kapak önce
            RequestArt(0);
            for (int i = 1; i <= _book.Pages; i++) RequestArt(i);

            Build();
        }

        // ---------------- kurulum ----------------

        void Build()
        {
            _lastSize = _root.rect.size;

            float availW = _root.rect.width - 80f, availH = _root.rect.height - 250f - 300f;
            // ilk karede kök henüz boyutlanmamış olabilir: sayfa boyutu 0/eksi olmasın (doku boyutu ondan hesaplanır)
            _pageH = Mathf.Max(100f, Mathf.Min(availH, (availW - 30f) / PageStudio.Aspect));
            _pageW = _pageH * PageStudio.Aspect;

            _bookHolder = UIKit.Node("Book", _root);
            _bookHolder.anchorMin = _bookHolder.anchorMax = new Vector2(0.5f, 0.5f);
            _bookHolder.pivot = new Vector2(0, 0);
            _bookHolder.sizeDelta = new Vector2(_pageW, _pageH);
            _bookHolder.anchoredPosition = new Vector2(-_pageW * 0.5f + 14f, -_pageH * 0.5f + 20f);
            _bookHolder.SetSiblingIndex(0);
            // sayfa her karede kıvrılırken ekranın geri kalanı (başlık, noktalar, arka plan) yeniden kurulmasın
            UIKit.SubCanvas(_bookHolder, true);

            // gölge + sert kapak + sayfa yığını + kurdele ayraç
            var sh = UIKit.Box(_bookHolder, "Shadow", new Color(0.3f, 0.2f, 0.4f, 0.28f), UIKit.Shadow);
            sh.rectTransform.Stretch(-50, -30, -40, -70);
            var board = UIKit.Box(_bookHolder, "Board", Dark(_book.Cover, 0.08f), UIKit.Rounded, 2.4f);
            board.rectTransform.Stretch(-30, -12, -16, -20);
            var boardHi = UIKit.Box(board.transform, "Edge", Color.white.WithAlpha(0.18f), UIKit.Rounded, 2.4f);
            boardHi.rectTransform.Stretch(0, 0, 0, 6);
            Ribbon();
            _stack = UIKit.Node("Stack", _bookHolder);
            _stack.Stretch();
            BuildStack();

            _view = UIKit.Node("Page", _bookHolder);
            _view.anchorMin = _view.anchorMax = Vector2.zero;
            _view.pivot = Vector2.zero;
            _view.sizeDelta = new Vector2(_pageW, _pageH);
            _view.anchoredPosition = Vector2.zero;

            _curl = new PageCurl { W = _pageW, H = _pageH };

            var underMask = Layer("UnderMask", _view);
            underMask.gameObject.AddComponent<RectMask2D>();
            _under = Mesh(underMask, "Under", PageMesh.Part.Under, Color.white);
            _underShadow = Shadow(underMask, "UnderShadow", true);

            _flat = Mesh(_view, "Flat", PageMesh.Part.Flat, Color.white);
            var creaseMask = Layer("CreaseMask", _view);
            creaseMask.gameObject.AddComponent<RectMask2D>();
            _crease = Shadow(creaseMask, "Crease", false);
            _backShadow = Mesh(creaseMask, "BackShadow", PageMesh.Part.BackShadow, new Color(0.2f, 0.12f, 0.28f, 0.16f));
            _curlFront = Mesh(_view, "CurlFront", PageMesh.Part.Curl, Color.white);
            _back = Mesh(_view, "Back", PageMesh.Part.Back, Color.white);
            _back.Tex = PaperBack;
            _ghost = Mesh(_view, "Ghost", PageMesh.Part.Ghost, new Color(1, 1, 1, 0.09f));

            // dokunma alanı: sayfanın kendisi (biraz taşkın). Ekran okuyucu için de sayfanın kendisi.
            var hit = UIKit.Node("Hit", _view).Stretch(-30, -20, -40, -20);
            var hitImg = hit.gameObject.AddComponent<Image>();
            hitImg.color = Color.clear;
            hit.gameObject.AddComponent<PageInput>().Target = this;

            // dokunma efektleri sayfanın üstünde, kitapla birlikte
            _fx = Layer("Fx", _bookHolder);

            if (_dots == null) BuildControls();
            ShowPage(_cur);
        }

        static Color Dark(Color c, float k) => Color.Lerp(c, Theme.Text, k);

        RectTransform Layer(string name, Transform parent)
        {
            var rt = UIKit.Node(name, parent);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.sizeDelta = new Vector2(_pageW, _pageH);
            return rt;
        }

        PageMesh Mesh(Transform parent, string name, PageMesh.Part part, Color c)
        {
            var rt = Layer(name, parent);
            rt.gameObject.AddComponent<CanvasRenderer>();
            var m = rt.gameObject.AddComponent<PageMesh>();
            m.Kind = part;
            m.Curl = _curl;
            m.color = c;
            return m;
        }

        CurlShadow Shadow(Transform parent, string name, bool under)
        {
            var rt = Layer(name, parent);
            rt.gameObject.AddComponent<CanvasRenderer>();
            var s = rt.gameObject.AddComponent<CurlShadow>();
            s.Curl = _curl;
            s.Under = under;
            return s;
        }

        /// <summary>Sağ ve alt kenarda görünen kalan sayfalar: okudukça incelir.</summary>
        void BuildStack()
        {
            for (int i = _stack.childCount - 1; i >= 0; i--) Destroy(_stack.GetChild(i).gameObject);
            int left = Mathf.Clamp(_count - 1 - _cur, 0, 4);
            for (int i = left; i >= 1; i--)
            {
                var s = UIKit.Box(_stack, "Leaf", Color.Lerp(Theme.Paper, Theme.Text, 0.05f + i * 0.035f), UIKit.Rounded, 6f);
                s.rectTransform.Stretch(0, 2 + i * 2.5f, -i * 3.2f, -i * 3.2f);
            }
        }

        /// <summary>Sayfaların arasından alta sarkan saten kurdele ayraç.</summary>
        void Ribbon()
        {
            var img = UIKit.Box(_bookHolder, "Ribbon", Color.white, RibbonSprite);
            img.type = Image.Type.Simple;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.74f, 0f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(46, 150);
            rt.anchoredPosition = new Vector2(0, 40);
            rt.localRotation = Quaternion.Euler(0, 0, 4f);
        }

        void BuildControls()
        {
            var bar = UIKit.Node("Controls", _root);
            bar.Band(false, 170, 60);
            _prev = UIKit.CircleButton(bar, "‹", Theme.Card, Theme.Text, 140, 100, () => AutoTurn(-1));
            ((RectTransform)_prev.transform).Place(new Vector2(0, 0.5f), new Vector2(140, 0), new Vector2(140, 140));
            A11y.Label(_prev, Loc.T("a11y_prev_page"));
            _next = UIKit.CircleButton(bar, "›", Theme.AccentButton, Color.white, 140, 100, () => AutoTurn(+1));
            ((RectTransform)_next.transform).Place(new Vector2(1, 0.5f), new Vector2(-140, 0), new Vector2(140, 140));
            A11y.Label(_next, Loc.T("a11y_next_page"));

            // ilerleme: noktalar, sonuncusu yıldız (okumayı bilmeyen çocuk da "yıldıza kaç sayfa kaldı"yı görür)
            _dots = UIKit.Node("Dots", bar);
            _dots.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 60));
            var hl = _dots.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 14;
            hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childControlWidth = hl.childControlHeight = false;
            hl.childForceExpandWidth = hl.childForceExpandHeight = false;
            for (int i = 0; i < _count; i++)
            {
                bool star = i == _count - 1;
                var d = UIKit.Box(_dots, star ? "Star" : "Dot", Theme.Lavender, star ? Art.Star5 : UIKit.Circle);
                d.type = Image.Type.Simple;
                d.preserveAspect = true;
                d.rectTransform.sizeDelta = star ? new Vector2(44, 44) : new Vector2(24, 24);
                _dotImgs.Add(d);
            }

            _endBar = UIKit.Node("End", _root);
            _endBar.Band(false, 170, 60);
            var again = UIKit.Button(_endBar, Loc.T("read_again"), Theme.Card, Theme.Text, 44, Restart);
            ((RectTransform)again.transform).Place(new Vector2(0.5f, 0.5f), new Vector2(-235, 0), new Vector2(420, 140));
            var shelf = UIKit.Button(_endBar, Loc.T("to_library"), Theme.AccentButton, Color.white, 44, () => App.I.ShowLibrary());
            ((RectTransform)shelf.transform).Place(new Vector2(0.5f, 0.5f), new Vector2(235, 0), new Vector2(420, 140));
            foreach (var b in new[] { again, shelf })
            {
                var t = UIKit.ButtonText(b);
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                t.verticalOverflow = VerticalWrapMode.Truncate;
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize = 26;
                t.resizeTextMaxSize = 44;
                t.rectTransform.Stretch(20, 0, 20, 0);
            }
            _endBar.gameObject.SetActive(false);
        }

        void UpdateControls()
        {
            for (int i = 0; i < _dotImgs.Count; i++)
            {
                bool on = i == _cur, star = i == _count - 1;
                _dotImgs[i].color = star ? (on ? Theme.Hex("FFC93C") : Theme.Butter)
                    : on ? Theme.AccentButton : (i < _cur ? Theme.Accent.WithAlpha(0.55f) : Theme.Lavender.WithAlpha(0.7f));
                _dotImgs[i].rectTransform.sizeDelta = star ? new Vector2(48, 48) : on ? new Vector2(34, 34) : new Vector2(22, 22);
            }
            bool end = _cur == _count - 1;
            if (_prev != null) _prev.gameObject.SetActive(_cur > 0 && !end);
            if (_next != null) _next.gameObject.SetActive(!end);
            if (_dots != null) _dots.gameObject.SetActive(!end);
            if (_endBar != null && _endBar.gameObject.activeSelf != end)
            {
                _endBar.gameObject.SetActive(end);
                if (end) Tween.Pop(_endBar, 0.15f);
            }
            // kapakta üstteki başlık gizli (kapak zaten söylüyor), içeride yavaşça belirir
            float want = _cur == 0 ? 0f : 1f;
            if (_titleGroup != null && !Mathf.Approximately(_titleGroup.alpha, want)) Tween.Fade(_titleGroup, _titleGroup.alpha, want, 0.4f);
        }

        // ---------------- resimler ve sayfa basımı ----------------

        void RequestArt(int scene)
        {
            string s = scene == 0 ? _book.CoverScene : _book.Scenes[scene - 1];
            StoryArt.PaintAsync(s, 1024, 768, this, tex =>
            {
                // yerini aldığı önizleme (kitaplıktan gelen küçük kapak) geçiş solması bitince bırakılır
                if (_art.TryGetValue(scene, out var old) && old != null && old != tex) Destroy(old, 0.3f);
                _art[scene] = tex;
                // bu resmi kullanan sayfalar yeniden basılsın (kapak sahnesi hem kapakta hem son sayfada)
                if (scene == 0) { MarkStale(0); MarkStale(_count - 1); }
                else MarkStale(scene);
            });
        }

        void MarkStale(int page)
        {
            if (_pages.ContainsKey(page)) _stale.Add(page);
        }

        Texture Page(int i)
        {
            if (i < 0 || i >= _count) return null;
            if (_pages.TryGetValue(i, out var rt) && rt != null && rt.IsCreated() && !_stale.Contains(i)) return rt;
            if (rt != null && !rt.IsCreated()) rt.Create();
            if (rt == null)
            {
                float scale = App.I != null && App.I.Canvas != null ? App.I.Canvas.scaleFactor : 1f;
                rt = PageStudio.NewTexture(_pageH * scale);
                _pages[i] = rt;
            }
            _stale.Remove(i);
            int idx = i;
            PageStudio.I.Render(rt, root => BuildPage(root, idx));
            return rt;
        }

        /// <summary>Gösterilen sayfanın iki yanındakiler dışında basılı sayfaları bırak (bellek).</summary>
        void Trim()
        {
            var drop = new List<int>();
            foreach (var kv in _pages) if (Mathf.Abs(kv.Key - _cur) > 2) drop.Add(kv.Key);
            foreach (var k in drop)
            {
                if (_pages[k] != null) { _pages[k].Release(); Destroy(_pages[k]); }
                _pages.Remove(k);
                _stale.Remove(k);
            }
        }

        void BuildPage(RectTransform root, int i)
        {
            if (i == 0) BookPages.Cover(root, _book, _art.TryGetValue(0, out var c) ? c : null);
            else if (i == _count - 1) BookPages.End(root, _book, _art.TryGetValue(0, out var e) ? e : null);
            else BookPages.Story(root, _book, i, _art.TryGetValue(i, out var a) ? a : null);
        }

        /// <summary>Android: uygulama arka plandan dönünce RenderTexture içerikleri kaybolabilir; hepsi yeniden basılsın.</summary>
        void OnApplicationPause(bool paused) { if (!paused) MarkAllStale(); }
        void OnApplicationFocus(bool focus) { if (focus) MarkAllStale(); }

        void MarkAllStale()
        {
            foreach (var k in _pages.Keys) _stale.Add(k);
        }

        // ---------------- sayfa gösterimi ----------------

        void ShowPage(int i)
        {
            _cur = Mathf.Clamp(i, 0, _count - 1);
            _curl.Active = false;
            _flat.Tex = Page(_cur);
            _under.enabled = false;
            _ghost.Tex = null;
            ClearBadges();
            DirtyAll();
            BuildStack();
            UpdateControls();
            Trim();
            // komşu sayfaları önceden bas: dönüş anında takılma olmasın
            Tween.Delay(0.05f, () =>
            {
                if (this == null || _mode != Mode.Idle) return;
                Page(_cur + 1);
                if (_cur > 0) Page(_cur - 1);
            });
            A11y.Announce(PageSpeech());
            if (_cur == _count - 1) Finish();
            _idleSince = Time.unscaledTime;
        }

        /// <summary>Ekran okuyucunun söyleyeceği: kapakta başlık, sayfada hikaye metni, sonda "Son".</summary>
        string PageSpeech()
        {
            if (_cur == 0) return _book.Title;
            if (_cur == _count - 1) return Loc.T("the_end") + ". " + _book.Title;
            return _book.Text(_cur);
        }

        void DirtyAll()
        {
            _flat.SetVerticesDirty(); _curlFront.SetVerticesDirty(); _back.SetVerticesDirty();
            _ghost.SetVerticesDirty(); _backShadow.SetVerticesDirty(); _under.SetVerticesDirty();
            _underShadow.SetVerticesDirty(); _crease.SetVerticesDirty();
        }

        /// <summary>Dönüşe hazırlan: dir +1 ise şimdiki sayfa kalkar, -1 ise önceki sayfa geri gelir.</summary>
        bool BeginTurn(int dir, Vector2 grab)
        {
            if (dir > 0 && _cur >= _count - 1) return false;
            if (dir < 0 && _cur <= 0) return false;
            _dir = dir;
            _turnPage = dir > 0 ? _cur : _cur - 1;
            int underPage = dir > 0 ? _cur + 1 : _cur;
            var turnTex = Page(_turnPage);
            var underTex = Page(underPage);
            _flat.Tex = turnTex;
            _curlFront.Tex = turnTex;
            _ghost.Tex = turnTex;
            _under.Tex = underTex;
            _under.enabled = true;
            // kapağın arkası "iç kapak": kapak renginin çok açık pastel tonu, baskı izi yok
            bool cover = _turnPage == 0;
            _back.color = cover ? Color.Lerp(_book.Cover, Color.white, 0.72f) : Color.white;
            _ghost.enabled = !cover;
            ClearBadges();
            _curl.Active = true;
            float gy = Mathf.Clamp(grab.y, _pageH * 0.06f, _pageH * 0.94f);
            if (dir > 0)
            {
                _curl.G0 = new Vector2(Mathf.Clamp(grab.x, _pageW * 0.45f, _pageW), gy);
                _curl.G = _curl.G0;
            }
            else
            {
                _curl.G0 = new Vector2(_pageW, gy);
                _curl.G = new Vector2(-_pageW, gy);
            }
            _gStart = _curl.G;
            _curl.Update();
            DirtyAll();
            return true;
        }

        void SetG(Vector2 g)
        {
            _curl.G = g;
            _curl.Update();
            DirtyAll();
        }

        void EndTurn(bool commit)
        {
            _curl.Active = false;
            _under.enabled = false;
            _peeking = false;
            if (commit)
            {
                ShowPage(_dir > 0 ? _cur + 1 : _cur - 1);
            }
            else
            {
                _flat.Tex = Page(_cur);
                DirtyAll();
            }
            _mode = Mode.Idle;
            _idleSince = Time.unscaledTime;
        }

        void Animate(Vector2 to, float dur, bool commit, System.Func<float, float> ease = null)
        {
            if (_anim != null) Tween.Stop(_anim);
            _mode = Mode.Anim;
            var from = _curl.G;
            if (commit) { Sfx.Page(); Haptics.Light(); }
            _anim = Tween.To(this, dur, k => SetG(Vector2.LerpUnclamped(from, to, k)), ease ?? Tween.OutCubic, 0f, () =>
            {
                _anim = null;
                EndTurn(commit);
            });
        }

        void AutoTurn(int dir)
        {
            StopPeek();
            if (_mode != Mode.Idle) return;
            _peeks = 99;
            if (Motion.Reduced)
            {
                // hareket azaltma: kıvrılma yok, sayfa doğrudan değişir
                if ((dir > 0 && _cur >= _count - 1) || (dir < 0 && _cur <= 0)) return;
                Sfx.Page();
                ShowPage(_cur + dir);
                return;
            }
            if (!BeginTurn(dir, new Vector2(_pageW, _pageH * 0.16f))) return;
            var target = dir > 0 ? new Vector2(-_curl.G0.x, _pageH * 0.3f) : _curl.G0;
            if (dir < 0) _curl.G = new Vector2(-_pageW, _pageH * 0.3f);
            Animate(target, 0.62f, true, Tween.InOutSine);
        }

        void Restart()
        {
            if (_mode != Mode.Idle) return;
            Sfx.Page();
            _celebrated = false;   // ikinci okuyuşta da sonunda kutlama olsun (yıldız yalnızca ilkinde)
            ShowPage(0);
            Tween.Punch(_bookHolder, 0.04f);
        }

        void Finish()
        {
            if (_celebrated) return;
            _celebrated = true;
            bool first = !_book.Read;
            Stats.Add("book");
            Confetti.Burst(_root, first ? 40 : 22);
            if (first)
            {
                Sfx.Win();
                _book.Read = true;
                SaveSystem.Stars++;
                Tween.Delay(0.6f, () => { if (this == null) return; Sfx.Star(); Popup.Toast(Loc.T("book_star")); });
                int read = Books.ReadCount;
                foreach (var (_, need, _) in PuffyStickers.BookPuffies)
                    if (need == read)
                        Tween.Delay(2.6f, () => { if (this == null) return; Sfx.Star(); Popup.Toast(Loc.T("puffy_new")); });
            }
            else
            {
                PlayerPrefs.Save();
                Sfx.Clap();
            }
            // Pofuduk sol alttan gelip sevinir
            Tween.Delay(0.9f, () =>
            {
                if (this == null) return;
                if (_buddy == null) _buddy = Buddy.Create(_root, 250f);
                _buddy.Peek(Loc.T("book_done_buddy"), 3f);
            });
        }

        // ---------------- dokunma ----------------

        Vector2 Local(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_view, e.position, e.pressEventCamera, out var p);
            return p;   // pivot sol altta: doğrudan sayfa koordinatı
        }

        /// <summary>"Beni çevir" ipucu sırasında parmak gelirse ipucu hemen biter, sürükleme parmağa geçer.</summary>
        void StopPeek()
        {
            if (!_peeking) return;
            if (_anim != null) { Tween.Stop(_anim); _anim = null; }
            EndTurn(false);
        }

        void OnPointerDown(PointerEventData e)
        {
            _peeks = 99;
            StopPeek();
        }

        void OnBeginDrag(PointerEventData e)
        {
            StopPeek();
            if (_mode != Mode.Idle) return;
            _dragStart = Local(e);
            // yön: sağ kenardan tutan ileri, sol kenardan tutan geri çevirmek ister. İlk karedeki birkaç piksellik
            // titreme yönü belirlemesin; ortada tutunca hareketin yönüne bakılır.
            int dir;
            if (_dragStart.x > _pageW * 0.7f) dir = +1;
            else if (_dragStart.x < _pageW * 0.3f) dir = -1;
            else if (Mathf.Abs(e.delta.x) >= Mathf.Abs(e.delta.y) * 0.5f) dir = e.delta.x < 0 ? +1 : -1;
            else dir = _dragStart.x > _pageW * 0.5f ? +1 : -1;
            if (!BeginTurn(dir, _dragStart) && !BeginTurn(-dir, _dragStart)) return;
            _mode = Mode.Drag;
            _lastPos = _dragStart;
            _lastTime = Time.unscaledTime;
            _vel = Vector2.zero;
        }

        void OnDrag(PointerEventData e)
        {
            if (_mode != Mode.Drag) return;
            var p = Local(e);
            float dt = Mathf.Max(1e-3f, Time.unscaledTime - _lastTime);
            _vel = Vector2.Lerp(_vel, (p - _lastPos) / dt, 0.5f);
            _lastPos = p;
            _lastTime = Time.unscaledTime;
            var d = p - _dragStart;
            if (_dir > 0) SetG(_gStart + d);
            else SetG(_gStart + new Vector2(d.x * 1.6f, d.y));
        }

        void OnEndDrag(PointerEventData e)
        {
            if (_mode != Mode.Drag) return;
            float prog = _curl.Progress;
            // Tek sayfa ekranda: sağ kenardan sırta kadar çekmek ilerlemenin yarısı (0.5). Eşik 0.22 ≈ sayfanın %44'ü,
            // küçük bir parmak için rahat. Fiske: yalnızca son anda gerçekten hızlıysa (çekip bekleyip bırakmak fiske değil).
            bool fling = Mathf.Abs(_vel.x) > _pageW * 0.9f && Time.unscaledTime - _lastTime < 0.08f;
            bool commit = _dir > 0
                ? (fling ? _vel.x < 0 : prog > 0.22f)
                : (fling ? _vel.x > 0 : prog < 0.72f);
            var target = commit == (_dir > 0) ? new Vector2(-_curl.G0.x, _curl.G.y) : _curl.G0;
            float dist = (target - _curl.G).magnitude / (_pageW * 2f);
            Animate(target, Mathf.Lerp(0.2f, 0.45f, dist), commit);
        }

        void OnPointerClick(PointerEventData e)
        {
            if (e.dragging || _mode != Mode.Idle) return;
            var p = Local(e);
            // hikaye sayfasında resim kutusu içine dokunmak yalnızca resmi tetikler:
            // küçük parmak balığı/karakteri biraz ıskalasa bile sayfa yanlışlıkla çevrilmez
            if (_cur >= 1 && _cur <= _book.Pages)
            {
                var uv = ToArt(p);
                if (uv.x >= 0 && uv.x <= 1 && uv.y >= 0 && uv.y <= 1) { Touch(p); return; }
            }
            if (p.x > _pageW * 0.6f) AutoTurn(+1);
            else if (p.x < _pageW * 0.4f) AutoTurn(-1);
        }

        // ---------------- dokunulabilir resimler ----------------

        static readonly HashSet<string> Silent = new() { "rainbow", "net", "rope", "bedhead", "bed", "branch", "blanket", "zz", "tuft", "flag", "anthill", "tree", "pine" };
        static readonly HashSet<string> Centered = new() { "sun", "moon", "star", "spark", "heart", "note", "bubble", "cloud", "leaf", "lily", "kite" };
        static readonly HashSet<string> Alive = new()
        {
            "bunny", "hare", "bear", "cat", "dog", "mouse", "lion", "fox", "squirrel", "hedgehog", "frog", "owl", "duck", "gduck", "swan", "hen",
            "crow", "bird", "ladybug", "bee", "butterfly", "ant", "cricket", "snail", "fish", "turtle", "star5", "crab", "snowman",
        };

        /// <summary>Sayfa koordinatını resim koordinatına (0..1, y yukarı) çevirir.</summary>
        Vector2 ToArt(Vector2 page) => new(
            (page.x * PageStudio.PW / _pageW - PicX) / PicW,
            (page.y * PageStudio.PH / _pageH - PicY) / PicH);

        Vector2 FromArt(Vector2 uv) => new(
            (uv.x * PicW + PicX) * _pageW / PageStudio.PW,
            (uv.y * PicH + PicY) * _pageH / PageStudio.PH);

        bool Touch(Vector2 page)
        {
            var uv = ToArt(page);
            if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) return false;
            var items = StoryArt.Items(_book.Scenes[_cur - 1]);
            // sayma kitabında sayılacaklar önce (ör. kurbağanın oturduğu nilüfer, kurbağanın altında kalsa da sayılsın),
            // sonra en üstte çizilenden başlayarak diğerleri
            string countable = _book.Counts != null && _cur - 1 < _book.Counts.Length ? _book.Counts[_cur - 1] : null;
            for (int pass = countable != null ? 0 : 1; pass < 2; pass++)
            for (int i = items.Count - 1; i >= 0; i--)
            {
                var (name, x, y, s) = items[i];
                if (Silent.Contains(name)) continue;
                if (pass == 0 && (name != countable || _counted.Contains(i))) continue;
                float hw = s * 0.42f / ArtAspect;
                bool hit;
                Vector2 top;
                if (Centered.Contains(name))
                {
                    hit = Mathf.Abs(uv.x - x) < Mathf.Max(hw, 0.045f) && Mathf.Abs(uv.y - y) < Mathf.Max(s * 0.45f, 0.06f);
                    top = new Vector2(x, y + s * 0.3f);
                }
                else
                {
                    hit = Mathf.Abs(uv.x - x) < Mathf.Max(hw, 0.045f) && uv.y > y - 0.03f && uv.y < y + Mathf.Max(s * 0.95f, 0.08f);
                    top = new Vector2(x, y + s * 0.9f);
                }
                if (!hit) continue;
                React(i, name, FromArt(top));
                return true;
            }
            return false;
        }

        void React(int index, string name, Vector2 at)
        {
            var counts = _book.Counts;
            if (counts != null && _cur - 1 < counts.Length && counts[_cur - 1] == name && !_counted.Contains(index))
            {
                // sayma kitabı: her dokunuşta sıradaki sayı belirir
                _counted.Add(index);
                int n = _counted.Count;
                Badge(at, n);
                Sfx.Star();
                Haptics.Light();
                int total = 0;
                foreach (var it in StoryArt.Items(_book.Scenes[_cur - 1])) if (it.name == name) total++;
                if (n == total)
                {
                    Tween.Delay(0.35f, () => { if (this != null) { Confetti.Burst(_root, 18); Sfx.Clap(); } });
                }
                return;
            }
            bool alive = Alive.Contains(name);
            if (alive) Sfx.Giggle(); else Sfx.Pop();
            Haptics.Light();
            Burst(at, alive);
        }

        /// <summary>Dokunulan yerden yukarı süzülen kalpler (canlılar) ya da yıldızlar (nesneler).</summary>
        void Burst(Vector2 at, bool hearts)
        {
            if (Motion.Reduced) { Badge(at, -1); return; }
            Color[] cols = hearts ? new[] { Theme.Accent, Theme.Hex("FF8FAB"), Theme.Hex("FFB3C7") } : new[] { Theme.Hex("FFC93C"), Theme.Butter, Theme.Mint };
            for (int i = 0; i < 5; i++)
            {
                var img = UIKit.Box(_fx, "Pop", cols[i % cols.Length], hearts ? Art.Heart : Art.Star5);
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                float size = Random.Range(34f, 58f);
                rt.sizeDelta = new Vector2(size, size);
                rt.anchoredPosition = at;
                float ang = Mathf.Lerp(-55f, 55f, i / 4f) + Random.Range(-10f, 10f);
                var dir = Quaternion.Euler(0, 0, ang) * Vector3.up;
                float dist = Random.Range(90f, 150f);
                var from = at;
                float spin = Random.Range(-40f, 40f);
                Tween.To(rt, 0.8f, k =>
                {
                    rt.anchoredPosition = from + (Vector2)(dir * dist * k) + new Vector2(0, -60f * k * k);
                    rt.localScale = Vector3.one * (k < 0.2f ? k / 0.2f : 1f);
                    rt.localRotation = Quaternion.Euler(0, 0, spin * k);
                    img.color = img.color.WithAlpha(1f - Mathf.Clamp01((k - 0.55f) / 0.45f));
                }, Tween.OutCubic, i * 0.03f, () => { if (rt != null) Destroy(rt.gameObject); });
            }
        }

        /// <summary>Sayılan öğenin üstünde sayı rozeti (n &lt; 0: hareket azaltmada sade parıltı).</summary>
        void Badge(Vector2 at, int n)
        {
            var b = UIKit.Box(_fx, "Count", n > 0 ? Theme.AccentButton : Theme.Butter, UIKit.Circle);
            b.type = Image.Type.Simple;
            var rt = b.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.sizeDelta = new Vector2(78, 78);
            rt.anchoredPosition = at + new Vector2(0, 20);
            var ring = UIKit.Box(rt, "Ring", Color.white, UIKit.Ring);
            ring.type = Image.Type.Simple;
            ring.rectTransform.Stretch(-4, -4, -4, -4);
            if (n > 0)
            {
                var t = UIKit.Label(rt, n.ToString(), 46, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                t.rectTransform.Stretch(0, 0, 0, 4);
                _badges.Add(rt.gameObject);
                Tween.Pop(rt, 0f, 0.4f);
            }
            else
            {
                Tween.Delay(0.6f, () => { if (rt != null) Destroy(rt.gameObject); });
            }
        }

        void ClearBadges()
        {
            foreach (var g in _badges) if (g != null) Destroy(g);
            _badges.Clear();
            _counted.Clear();
        }

        // ---------------- yaşam döngüsü ----------------

        void Update()
        {
            // resmi yeni gelen ya da dokusu kaybolan sayfa ekrandaysa hemen yeniden bas
            if (_mode == Mode.Idle && _flat != null && _pages.TryGetValue(_cur, out var cur)
                && (_stale.Contains(_cur) || cur == null || !cur.IsCreated()))
                _flat.Tex = Page(_cur);

            if (_root != null && (_root.rect.size - _lastSize).sqrMagnitude > 4f && _mode == Mode.Idle)
            {
                // döndürme / pencere boyutu değişti: kitabı yeniden ölç
                foreach (var kv in _pages) if (kv.Value != null) { kv.Value.Release(); Destroy(kv.Value); }
                _pages.Clear();
                _stale.Clear();
                _badges.Clear();
                _bookHolder.gameObject.SetActive(false);   // maskeler hemen kaydını silsin
                Destroy(_bookHolder.gameObject);
                _view = null;
                Build();
            }
            // kapakta beklerken köşe hafifçe kalkar: "beni çevir"
            if (_mode == Mode.Idle && _peeks < 2 && !Motion.Reduced && _cur < _count - 1 && Time.unscaledTime - _idleSince > (_peeks == 0 ? 1.6f : 4f))
            {
                _peeks++;
                Peek();
            }
        }

        void Peek()
        {
            if (!BeginTurn(+1, new Vector2(_pageW, _pageH * 0.1f))) return;
            _mode = Mode.Anim;
            _peeking = true;
            var g0 = _curl.G0;
            var lift = g0 + new Vector2(-_pageW * 0.16f, _pageH * 0.035f);
            _anim = Tween.To(this, 1.1f, k => SetG(Vector2.Lerp(g0, lift, Mathf.Sin(k * Mathf.PI))), Tween.Linear, 0f, () =>
            {
                _anim = null;
                EndTurn(false);
            });
        }

        public bool OnBack()
        {
            App.I.ShowLibrary();
            return true;
        }

        void OnDestroy()
        {
            if (_anim != null) Tween.Stop(_anim);
            foreach (var kv in _pages) if (kv.Value != null) { kv.Value.Release(); Destroy(kv.Value); }
            foreach (var kv in _art) if (kv.Value != null) Destroy(kv.Value);
        }

        // ---------------- dokular ----------------

        static Texture2D PaperBack
        {
            get
            {
                if (_paperBack != null) return _paperBack;
                const int n = 64;
                _paperBack = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, name = "PaperBack" };
                var rnd = new System.Random(2);
                var px = new Color32[n * n];
                var b = (Color32)Color.Lerp(Theme.Paper, Theme.Text, 0.04f);
                for (int i = 0; i < px.Length; i++)
                {
                    int v = (int)(rnd.NextDouble() * 10) - 5;
                    px[i] = new Color32((byte)Mathf.Clamp(b.r + v, 0, 255), (byte)Mathf.Clamp(b.g + v, 0, 255), (byte)Mathf.Clamp(b.b + v, 0, 255), 255);
                }
                _paperBack.SetPixels32(px);
                _paperBack.Apply(false, true);
                return _paperBack;
            }
        }

        static Sprite _ribbonSprite;

        /// <summary>Saten kurdele: ortada parlak şerit, ucu V kesik, kenarları hafif koyu.</summary>
        static Sprite RibbonSprite
        {
            get
            {
                if (_ribbonSprite != null) return _ribbonSprite;
                const int w = 48, h = 160;
                _ribbon = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Ribbon" };
                var baseCol = Theme.AccentButton;
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w;
                    // alt uçta V kesik: ortada yukarı doğru çentik
                    float notch = 26f * (1f - Mathf.Abs(u - 0.5f) * 2f);
                    float a = Mathf.Clamp01(y + 0.5f - notch);
                    float edge = Mathf.Min(u, 1f - u);
                    a *= Mathf.Clamp01(edge * w - 0.5f + 0.5f);
                    float shine = Mathf.Exp(-Mathf.Pow((u - 0.42f) / 0.12f, 2f)) * 0.35f;
                    float dark = Mathf.Pow(1f - Mathf.Clamp01(edge * 4f), 2f) * 0.25f;
                    var c = Color.Lerp(Color.Lerp(baseCol, Color.white, shine), Theme.Text, dark);
                    // sayfaların arasından çıktığı üst kısım gölgede
                    c = Color.Lerp(c, Theme.Text, Mathf.Clamp01((y - (h - 40f)) / 40f) * 0.35f);
                    c.a = a;
                    px[y * w + x] = c;
                }
                _ribbon.SetPixels32(px);
                _ribbon.Apply(false, true);
                _ribbonSprite = Sprite.Create(_ribbon, new Rect(0, 0, w, h), new Vector2(0.5f, 1f), 100);
                return _ribbonSprite;
            }
        }

        /// <summary>
        /// Sayfa alanının dokunma girişi. Ekran okuyucu (TalkBack) için de sayfanın kendisidir: odaklanınca o anki
        /// sayfayı (kapakta başlığı, hikayede metni) söyler, çift dokunuşla sonraki sayfaya geçer.
        /// </summary>
        class PageInput : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler, IA11yAction
        {
            public BookScreen Target;
            public void OnBeginDrag(PointerEventData e) => Target.OnBeginDrag(e);
            public void OnDrag(PointerEventData e) => Target.OnDrag(e);
            public void OnEndDrag(PointerEventData e) => Target.OnEndDrag(e);
            public void OnPointerClick(PointerEventData e) => Target.OnPointerClick(e);
            public void OnPointerDown(PointerEventData e) => Target.OnPointerDown(e);

            public string A11yText => Target.PageSpeech();
            public bool A11yAvailable => Target != null;
            public void A11yActivate() => Target.AutoTurn(+1);
        }
    }
}
