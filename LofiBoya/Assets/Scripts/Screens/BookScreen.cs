using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Kitap okuma ekranı. Sayfalar 3B olarak kıvrılarak döner (PageCurl): parmakla sayfanın herhangi bir yerinden
    /// tutup çekmek, sağ/sol kenara dokunmak ya da alttaki oklar. Sayfa 0 kapak, 1..N hikaye, N+1 "Son".
    /// </summary>
    public class BookScreen : MonoBehaviour, IScreen
    {
        public string BookId;

        Books.Book _book;
        int _count;          // toplam sayfa (kapak + hikaye + son)
        int _cur;            // gösterilen sayfa
        readonly Dictionary<int, Texture2D> _art = new();          // sahne resimleri (0: kapak sahnesi)
        readonly Dictionary<int, RenderTexture> _pages = new();    // basılmış sayfalar
        readonly HashSet<int> _stale = new();                      // resmi gelince yeniden basılacaklar

        RectTransform _root, _view, _stack;
        PageCurl _curl;
        PageMesh _flat, _curlFront, _back, _ghost, _backShadow, _under;
        CurlShadow _underShadow, _crease;
        RectTransform _dots;
        readonly List<Image> _dotImgs = new();
        Button _prev, _next;
        RectTransform _endBar;
        Text _title;
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
        bool _rewarded;

        static Texture2D _paperBack;

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
            _title.resizeTextForBestFit = true;
            _title.resizeTextMinSize = 30;
            _title.resizeTextMaxSize = 52;
            _title.lineSpacing = 0.85f;

            // tüm sahneleri arka planda boya; kapak önce
            RequestArt(0);
            for (int i = 1; i <= _book.Pages; i++) RequestArt(i);

            Build();
        }

        // ---------------- kurulum ----------------

        void Build()
        {
            if (_book == null) return;
            _lastSize = _root.rect.size;

            float availW = _root.rect.width - 80f, availH = _root.rect.height - 250f - 300f;
            _pageH = Mathf.Min(availH, (availW - 30f) / PageStudio.Aspect);
            _pageW = _pageH * PageStudio.Aspect;

            _bookHolder = UIKit.Node("Book", _root);
            _bookHolder.anchorMin = _bookHolder.anchorMax = new Vector2(0.5f, 0.5f);
            _bookHolder.pivot = new Vector2(0, 0);
            _bookHolder.sizeDelta = new Vector2(_pageW, _pageH);
            _bookHolder.anchoredPosition = new Vector2(-_pageW * 0.5f + 14f, -_pageH * 0.5f + 20f);
            _bookHolder.SetSiblingIndex(0);

            // gölge + sert kapak + sayfa yığını
            var sh = UIKit.Box(_bookHolder, "Shadow", new Color(0.3f, 0.2f, 0.4f, 0.28f), UIKit.Shadow);
            sh.rectTransform.Stretch(-50, -30, -40, -70);
            var cover = UIKit.Box(_bookHolder, "Board", Dark(_book.Cover, 0.08f), UIKit.Rounded, 2.4f);
            cover.rectTransform.Stretch(-30, -12, -16, -20);
            var coverHi = UIKit.Box(cover.transform, "Edge", Color.white.WithAlpha(0.18f), UIKit.Rounded, 2.4f);
            coverHi.rectTransform.Stretch(0, 0, 0, 6);
            _stack = UIKit.Node("Stack", _bookHolder);
            _stack.Stretch();
            BuildStack();

            _view = UIKit.Node("Page", _bookHolder);
            _view.anchorMin = _view.anchorMax = Vector2.zero;
            _view.pivot = Vector2.zero;
            _view.sizeDelta = new Vector2(_pageW, _pageH);
            _view.anchoredPosition = Vector2.zero;

            _curl = new PageCurl { W = _pageW, H = _pageH };

            var underMask = UIKit.Node("UnderMask", _view);
            underMask.anchorMin = underMask.anchorMax = Vector2.zero;
            underMask.pivot = Vector2.zero;
            underMask.sizeDelta = new Vector2(_pageW, _pageH);
            underMask.gameObject.AddComponent<RectMask2D>();
            _under = Mesh(underMask, "Under", PageMesh.Part.Under, Color.white);
            _underShadow = Shadow(underMask, "UnderShadow", true);

            _flat = Mesh(_view, "Flat", PageMesh.Part.Flat, Color.white);
            var creaseMask = UIKit.Node("CreaseMask", _view);
            creaseMask.anchorMin = creaseMask.anchorMax = Vector2.zero;
            creaseMask.pivot = Vector2.zero;
            creaseMask.sizeDelta = new Vector2(_pageW, _pageH);
            creaseMask.gameObject.AddComponent<RectMask2D>();
            _crease = Shadow(creaseMask, "Crease", false);
            _backShadow = Mesh(creaseMask, "BackShadow", PageMesh.Part.BackShadow, new Color(0.2f, 0.12f, 0.28f, 0.16f));
            _curlFront = Mesh(_view, "CurlFront", PageMesh.Part.Curl, Color.white);
            _back = Mesh(_view, "Back", PageMesh.Part.Back, Color.white);
            _back.Tex = PaperBack;
            _ghost = Mesh(_view, "Ghost", PageMesh.Part.Ghost, new Color(1, 1, 1, 0.09f));

            // dokunma alanı: sayfanın kendisi (biraz taşkın)
            var hit = UIKit.Node("Hit", _view).Stretch(-30, -20, -40, -20);
            var hitImg = hit.gameObject.AddComponent<Image>();
            hitImg.color = Color.clear;
            var fwd = hit.gameObject.AddComponent<DragForward>();
            fwd.Target = this;
            A11y.Label(fwd, () => _cur >= 1 && _cur <= _book.Pages ? _book.Text(_cur) : _book.Title);

            // alt çubuk: oklar + noktalar
            if (_dots == null) BuildControls();
            ShowPage(_cur, true);
        }

        RectTransform _bookHolder;

        static Color Dark(Color c, float k) => Color.Lerp(c, Theme.Text, k);

        PageMesh Mesh(Transform parent, string name, PageMesh.Part part, Color c)
        {
            var rt = UIKit.Node(name, parent);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.sizeDelta = new Vector2(_pageW, _pageH);
            rt.gameObject.AddComponent<CanvasRenderer>();
            var m = rt.gameObject.AddComponent<PageMesh>();
            m.Kind = part;
            m.Curl = _curl;
            m.color = c;
            return m;
        }

        CurlShadow Shadow(Transform parent, string name, bool under)
        {
            var rt = UIKit.Node(name, parent);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.sizeDelta = new Vector2(_pageW, _pageH);
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

            _dots = UIKit.Node("Dots", bar);
            _dots.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 40));
            var hl = _dots.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 14;
            hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childControlWidth = hl.childControlHeight = false;
            hl.childForceExpandWidth = hl.childForceExpandHeight = false;
            for (int i = 0; i < _count; i++)
            {
                var d = UIKit.Box(_dots, "Dot", Theme.Lavender, UIKit.Circle);
                d.type = Image.Type.Simple;
                d.rectTransform.sizeDelta = new Vector2(24, 24);
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
                bool on = i == _cur;
                _dotImgs[i].color = on ? Theme.AccentButton : (i < _cur ? Theme.Accent.WithAlpha(0.55f) : Theme.Lavender.WithAlpha(0.7f));
                _dotImgs[i].rectTransform.sizeDelta = on ? new Vector2(34, 34) : new Vector2(22, 22);
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
        }

        // ---------------- resimler ve sayfa basımı ----------------

        void RequestArt(int scene)
        {
            string s = scene == 0 ? _book.CoverScene : _book.Scenes[scene - 1];
            StoryArt.PaintAsync(s, 1024, 768, this, tex =>
            {
                _art[scene] = tex;
                // bu resmi kullanan sayfalar yeniden basılsın (kapak sahnesi hem kapakta hem son sayfada)
                if (scene == 0) { MarkStale(0); MarkStale(_count - 1); }
                else MarkStale(scene);
            });
        }

        void MarkStale(int page)
        {
            if (!_pages.ContainsKey(page)) return;
            _stale.Add(page);
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

        // ---------------- sayfa gösterimi ----------------

        void ShowPage(int i, bool instant)
        {
            _cur = Mathf.Clamp(i, 0, _count - 1);
            _curl.Active = false;
            _flat.Tex = Page(_cur);
            _under.enabled = false;
            _ghost.Tex = null;
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
            if (_cur >= 1 && _cur <= _book.Pages) A11y.Announce(_book.Text(_cur));
            if (_cur == _count - 1) Finish();
            _idleSince = Time.unscaledTime;
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
            if (commit)
            {
                ShowPage(_dir > 0 ? _cur + 1 : _cur - 1, false);
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
            if (_mode != Mode.Idle) return;
            _peeks = 99;
            if (Motion.Reduced)
            {
                // hareket azaltma: kıvrılma yok, sayfa yumuşakça değişir
                if ((dir > 0 && _cur >= _count - 1) || (dir < 0 && _cur <= 0)) return;
                Sfx.Page();
                ShowPage(_cur + dir, true);
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
            ShowPage(0, true);
            Tween.Punch(_bookHolder, 0.04f);
        }

        void Finish()
        {
            if (_rewarded) return;
            _rewarded = true;
            Sfx.Win();
            Confetti.Burst(_root, 40);
            if (!_book.Read)
            {
                _book.Read = true;
                SaveSystem.Stars++;
                Tween.Delay(0.6f, () => { Sfx.Star(); Popup.Toast(Loc.T("book_star")); });
            }
        }

        // ---------------- dokunma ----------------

        Vector2 Local(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_view, e.position, e.pressEventCamera, out var p);
            return p;   // pivot sol altta: doğrudan sayfa koordinatı
        }

        public void OnPointerDown(PointerEventData e) { _peeks = 99; }

        public void OnBeginDrag(PointerEventData e)
        {
            if (_mode != Mode.Idle) return;
            _dragStart = Local(e);
            int dir = e.delta.x < 0 ? +1 : -1;
            if (Mathf.Abs(e.delta.x) < Mathf.Abs(e.delta.y) * 0.5f) dir = _dragStart.x > _pageW * 0.5f ? +1 : -1;
            if (!BeginTurn(dir, _dragStart)) return;
            _mode = Mode.Drag;
            _lastPos = _dragStart;
            _lastTime = Time.unscaledTime;
            _vel = Vector2.zero;
        }

        public void OnDrag(PointerEventData e)
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

        public void OnEndDrag(PointerEventData e)
        {
            if (_mode != Mode.Drag) return;
            float prog = _curl.Progress;
            bool fling = Mathf.Abs(_vel.x) > _pageW * 1.4f;
            bool commit = _dir > 0
                ? (fling ? _vel.x < 0 : prog > 0.4f)
                : (fling ? _vel.x > 0 : prog < 0.6f);
            var target = commit == (_dir > 0) ? new Vector2(-_curl.G0.x, _curl.G.y) : _curl.G0;
            float dist = (target - _curl.G).magnitude / (_pageW * 2f);
            Animate(target, Mathf.Lerp(0.18f, 0.42f, dist), commit);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.dragging || _mode != Mode.Idle) return;
            var p = Local(e);
            if (p.x > _pageW * 0.55f) AutoTurn(+1);
            else if (p.x < _pageW * 0.4f) AutoTurn(-1);
        }

        // ---------------- yaşam döngüsü ----------------

        void Update()
        {
            // resmi yeni gelen sayfa ekrandaysa hemen yeniden bas (aynı dokuya çizilir, ekran kendiliğinden yenilenir)
            if (_mode == Mode.Idle && _flat != null && _stale.Contains(_cur)) _flat.Tex = Page(_cur);
            if (_root != null && (_root.rect.size - _lastSize).sqrMagnitude > 4f && _mode == Mode.Idle)
            {
                // döndürme / pencere boyutu değişti: kitabı yeniden ölç
                foreach (var kv in _pages) if (kv.Value != null) { kv.Value.Release(); Destroy(kv.Value); }
                _pages.Clear();
                _stale.Clear();
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

        /// <summary>Dokunma olaylarını sayfa alanından ekrana iletir.</summary>
        class DragForward : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler
        {
            public BookScreen Target;
            public void OnBeginDrag(PointerEventData e) => Target.OnBeginDrag(e);
            public void OnDrag(PointerEventData e) => Target.OnDrag(e);
            public void OnEndDrag(PointerEventData e) => Target.OnEndDrag(e);
            public void OnPointerClick(PointerEventData e) => Target.OnPointerClick(e);
            public void OnPointerDown(PointerEventData e) => Target.OnPointerDown(e);
        }
    }
}
