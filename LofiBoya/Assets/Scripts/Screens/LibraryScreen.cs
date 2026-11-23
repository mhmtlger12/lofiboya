using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Hikaye kitaplığı: tahta raflarda duran sert kapaklı kitaplar. Kapak resimleri arka planda çizilir,
    /// geldikçe yumuşakça belirir. Okunan kitabın köşesinde yıldız rozeti olur. Yaşa uygun kitaplar önce gelir.
    /// </summary>
    public class LibraryScreen : MonoBehaviour, IScreen
    {
        enum Filter { All, Little, Big, Bedtime }

        readonly Dictionary<string, Texture2D> _covers = new();   // çizilen kapaklar: filtre değişince yeniden çizilmez
        readonly HashSet<string> _painting = new();               // kuyrukta/boyanmakta olanlar: çift istek gitmesin
        readonly Dictionary<string, (RawImage art, GameObject wait)> _slots = new();
        static Texture2D _wood;
        static Filter _filter;   // ekrana dönünce seçili raf hatırlansın
        ScrollRect _scroll;
        RectTransform _content;
        GridLayoutGroup _grid;
        readonly List<(Filter f, Image face, Text label)> _chips = new();
        float _width;

        void Start()
        {
            var root = (RectTransform)transform;

            var back = UIKit.CircleButton(root, "‹", Theme.Card, Theme.Text, 120, 90, () => App.I.ShowWelcome());
            ((RectTransform)back.transform).Place(new Vector2(0, 1), new Vector2(110, -110), new Vector2(120, 120));

            var title = UIKit.Label(root, Loc.T("library_title"), 80, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Band(true, 110, 200, 150);
            Fit(title, 40, 80);
            var sub = UIKit.Label(root, Loc.T("library_sub", Books.ReadCount, Books.All.Count), 42, Theme.TextSoft);
            sub.rectTransform.Band(true, 60, 305, 60);
            Fit(sub, 26, 42);

            // raf filtresi: Tümü · 3+ · 5+ · Uyku vakti
            var chips = UIKit.Node("Filters", root);
            chips.Band(true, 84, 382, 40);
            var hl = chips.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 16;
            hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childControlWidth = hl.childControlHeight = true;
            hl.childForceExpandWidth = hl.childForceExpandHeight = true;
            Chip(chips, Filter.All, Loc.T("filter_all"));
            Chip(chips, Filter.Little, Loc.T("age_tag", 3));
            Chip(chips, Filter.Big, Loc.T("age_tag", 5));
            Chip(chips, Filter.Bedtime, Loc.T("filter_sleep"));

            // kaydırılabilir raf
            var scrollRt = UIKit.Node("Scroll", root).Stretch(20, 486, 20, 0);
            var sr = scrollRt.gameObject.AddComponent<ScrollRect>();
            _scroll = sr;
            sr.horizontal = false;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 40;
            var vp = UIKit.Node("Viewport", scrollRt).Stretch();
            vp.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(0, 30);
            vp.gameObject.AddComponent<Image>().color = Color.clear;
            sr.viewport = vp;
            _content = UIKit.Node("Content", vp);
            _content.anchorMin = new Vector2(0, 1);
            _content.anchorMax = new Vector2(1, 1);
            _content.pivot = new Vector2(0.5f, 1);
            _content.offsetMin = _content.offsetMax = Vector2.zero;
            _grid = _content.gameObject.AddComponent<GridLayoutGroup>();
            _grid.spacing = Vector2.zero;
            _grid.padding = new RectOffset(0, 0, 10, 120);
            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.childAlignment = TextAnchor.UpperCenter;
            _content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = _content;

            Fill();
        }

        static void Fit(Text t, int min, int max)
        {
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = min;
            t.resizeTextMaxSize = max;
        }

        void Chip(Transform parent, Filter f, string label)
        {
            var b = UIKit.Button(parent, label, Theme.Card, Theme.Text, 36, () =>
            {
                if (_filter == f) return;
                _filter = f;
                Haptics.Light();
                Fill();
                if (_scroll != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
                    _scroll.verticalNormalizedPosition = 1f;
                }
            }, 0.5f, false);
            var face = b.transform.Find("Face").GetComponent<Image>();
            var t = UIKit.ButtonText(b);
            t.rectTransform.Stretch(10, 0, 10, 2);
            Fit(t, 20, 36);
            _chips.Add((f, face, t));
        }

        bool Shows(Books.Book b) => _filter switch
        {
            Filter.Little => b.Age == 0,
            Filter.Big => b.Age >= 1,
            Filter.Bedtime => b.Sleepy,
            _ => true,
        };

        /// <summary>Rafı (yeniden) dizer: filtre değişince ve tablet döndürülünce.</summary>
        void Fill()
        {
            _width = App.I.CanvasWidth;
            _slots.Clear();
            foreach (var (f, face, label) in _chips)
            {
                bool on = f == _filter;
                face.color = on ? Theme.AccentButton : Theme.Card.WithAlpha(0.9f);
                label.color = on ? Color.white : Theme.Text;
            }
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                var child = _content.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            int cols = _width >= 1380f ? 3 : 2;
            float cellW = Mathf.Floor((_width - 40f) / cols);
            _grid.cellSize = new Vector2(cellW, cellW * 1.36f);
            _grid.constraintCount = cols;

            // çocuğun yaşına uygun kitaplar önce; aynı yaşta okunmamışlar önce
            int age = SaveSystem.AgeBand;
            var list = Books.All.FindAll(Shows);
            list.Sort((a, b) =>
            {
                int fa = a.Age <= age ? 0 : 1, fb = b.Age <= age ? 0 : 1;
                if (fa != fb) return fa.CompareTo(fb);
                if (a.Read != b.Read) return a.Read.CompareTo(b.Read);
                return Books.All.IndexOf(a).CompareTo(Books.All.IndexOf(b));
            });
            for (int i = 0; i < list.Count; i++) Card(_content, list[i], cellW, i, cols);
            // rafın son gözünde Pofuduk oturur; satır yarım kalırsa boş raf tahtasıyla tamamlanır
            Puffy(_content, cellW, list.Count, cols);
            int total = list.Count + 1;
            while (total % cols != 0)
            {
                Shelf(UIKit.Node("Empty", _content), cellW, total, cols);
                total++;
            }
        }

        void Update()
        {
            if (App.I != null && Mathf.Abs(App.I.CanvasWidth - _width) > 1f) Fill();
        }

        /// <summary>Raf tahtası: hücreler bitişik, tahtalar yan yana gelince tek uzun raf olur.</summary>
        static void Shelf(RectTransform cell, float cellW, int index, int cols)
        {
            var shelf = UIKit.Node("Shelf", cell);
            shelf.anchorMin = new Vector2(0, 0); shelf.anchorMax = new Vector2(1, 0);
            shelf.pivot = new Vector2(0.5f, 0);
            shelf.sizeDelta = new Vector2(0, 46);
            shelf.anchoredPosition = new Vector2(0, 26);
            var shShadow = UIKit.Box(shelf, "Shadow", new Color(0.3f, 0.2f, 0.4f, 0.22f), UIKit.Shadow, 3f);
            shShadow.rectTransform.Stretch(-10, 10, -10, -30);
            var plank = shelf.gameObject.AddComponent<RawImage>();
            plank.texture = Wood;
            plank.uvRect = new Rect((index % cols) * 0.37f, 0, cellW / 256f, 1);
            plank.raycastTarget = false;
            var lip = UIKit.Node("Lip", shelf);
            lip.anchorMin = new Vector2(0, 1); lip.anchorMax = new Vector2(1, 1);
            lip.pivot = new Vector2(0.5f, 1); lip.sizeDelta = new Vector2(0, 6);
            var lipImg = lip.gameObject.AddComponent<Image>();
            lipImg.color = Color.white.WithAlpha(0.35f);
            lipImg.raycastTarget = false;
        }

        /// <summary>Rafın sonunda oturan Pofuduk: "Bugün hangi masalı okuyalım?"</summary>
        void Puffy(Transform parent, float cellW, int index, int cols)
        {
            var cell = UIKit.Node("Puffy", parent);
            Shelf(cell, cellW, index, cols);
            var m = Mascot.Create(cell, Mathf.Min(300f, cellW * 0.6f));
            var mrt = (RectTransform)m.transform;
            mrt.anchorMin = mrt.anchorMax = new Vector2(0.5f, 0f);
            mrt.pivot = new Vector2(0.5f, 0f);
            mrt.anchoredPosition = new Vector2(0, 70);
            var bub = UIKit.Card(cell, "Bubble", Color.white, 0.6f, 4f);
            var brt = (RectTransform)bub.transform.parent;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0f);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.sizeDelta = new Vector2(cellW * 0.82f, 150);
            brt.anchoredPosition = new Vector2(0, 90 + mrt.sizeDelta.y);
            var tail = UIKit.Box(bub.transform, "Tail", Color.white, UIKit.Circle);
            tail.type = Image.Type.Simple;
            tail.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0, -12), new Vector2(40, 40));
            var t = UIKit.Label(bub.transform, "", 36, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Stretch(18, 10, 18, 14);
            t.lineSpacing = 0.9f;
            if (Loc.IsArabic) BookPages.ArabicFit(t, Loc.T("library_buddy"), 36, 22);
            else { Fit(t, 22, 36); t.text = Loc.T("library_buddy"); }
            brt.gameObject.AddComponent<Floaty>().Init(6f, 0.7f);
            Tween.Pop(brt, 0.4f + Mathf.Min(index, 8) * 0.04f);
            Tween.Pop(mrt, 0.3f + Mathf.Min(index, 8) * 0.04f);
        }

        void Card(Transform parent, Books.Book book, float cellW, int index, int cols)
        {
            var cell = UIKit.Node("Book " + book.Id, parent);
            Shelf(cell, cellW, index, cols);

            // kitap: sayfa yığını + sert kapak
            float bw = cellW * 0.74f, bh = bw / 0.72f;
            var holder = UIKit.Node("Holder", cell);
            holder.Place(new Vector2(0.5f, 0f), new Vector2(0, 72 + bh * 0.5f), new Vector2(bw, bh));
            var bookShadow = UIKit.Box(holder, "Shadow", new Color(0.3f, 0.2f, 0.4f, 0.3f), UIKit.Shadow, 1.6f);
            bookShadow.rectTransform.Stretch(-16, -6, -24, -18);
            var pages = UIKit.Box(holder, "Pages", Color.Lerp(Theme.Paper, Theme.Text, 0.08f), UIKit.Rounded, 4f);
            pages.rectTransform.Stretch(10, 10, -10, -6);
            for (int i = 1; i <= 3; i++)
            {
                var line = UIKit.Box(pages.transform, "Line", Theme.Text.WithAlpha(0.08f), null);
                line.sprite = null; line.type = Image.Type.Simple;
                line.rectTransform.anchorMin = new Vector2(1, 0); line.rectTransform.anchorMax = new Vector2(1, 1);
                line.rectTransform.pivot = new Vector2(1, 0.5f);
                line.rectTransform.sizeDelta = new Vector2(2, -20);
                line.rectTransform.anchoredPosition = new Vector2(-i * 3.5f, 0);
            }

            var c = book.Cover;
            var face = UIKit.Box(holder, "Cover", c, UIKit.Rounded, 4f);
            face.rectTransform.Stretch();
            face.raycastTarget = true;
            var grain = Raw(face.transform, "Weave", Art.Grain, Color.white.WithAlpha(0.1f));
            grain.rectTransform.Stretch(2, 2, 2, 2);
            grain.uvRect = new Rect(index * 0.13f, 0, bw / 110f, bh / 110f);
            var spine = UIKit.Box(face.transform, "Spine", Color.Lerp(c, Theme.Text, 0.25f).WithAlpha(0.6f), UIKit.Rounded, 4f);
            spine.rectTransform.anchorMin = Vector2.zero; spine.rectTransform.anchorMax = new Vector2(0, 1);
            spine.rectTransform.pivot = new Vector2(0, 0.5f); spine.rectTransform.sizeDelta = new Vector2(22, 0);
            var ridge = UIKit.Box(face.transform, "Ridge", Color.white.WithAlpha(0.3f), null);
            ridge.sprite = null; ridge.type = Image.Type.Simple;
            ridge.rectTransform.anchorMin = Vector2.zero; ridge.rectTransform.anchorMax = new Vector2(0, 1);
            ridge.rectTransform.pivot = new Vector2(0, 0.5f); ridge.rectTransform.sizeDelta = new Vector2(3, -8);
            ridge.rectTransform.anchoredPosition = new Vector2(24, 0);

            // resim penceresi
            float pw = bw - 60, ph = pw * 0.75f;
            var mat = UIKit.Box(face.transform, "Mat", Color.white, UIKit.Rounded, 2.5f);
            mat.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(12, -20 - ph * 0.5f - 8), new Vector2(pw + 16, ph + 16));
            string id = book.Id;
            if (_covers.TryGetValue(id, out var ready) && ready != null)
            {
                var art = Raw(mat.transform, "Art", ready, Color.white);
                art.rectTransform.Stretch(8, 8, 8, 8);
            }
            else
            {
                var art = Raw(mat.transform, "Art", null, Color.white.WithAlpha(0f));
                art.rectTransform.Stretch(8, 8, 8, 8);
                var wait = UIKit.Box(mat.transform, "Wait", Theme.Lavender.WithAlpha(0.3f), UIKit.Rounded, 2f);
                wait.rectTransform.Stretch(8, 8, 8, 8);
                _slots[id] = (art, wait.gameObject);
                if (_painting.Add(id))
                {
                    StoryArt.PaintAsync(book.CoverScene, 480, 360, this, tex =>
                    {
                        _painting.Remove(id);
                        if (_covers.TryGetValue(id, out var old) && old != null) Destroy(old);
                        _covers[id] = tex;
                        if (!_slots.TryGetValue(id, out var slot) || slot.art == null) return;
                        slot.art.texture = tex;
                        if (slot.wait != null) Destroy(slot.wait);
                        Tween.To(slot.art, 0.35f, k => slot.art.color = Color.white.WithAlpha(k));
                    });
                }
            }

            // başlık
            var title = UIKit.Label(face.transform, "", 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Stretch(30, ph + 44, 14, 16);
            title.lineSpacing = 0.85f;
            if (Loc.IsArabic) BookPages.ArabicFit(title, book.Title, 40, 22);
            else { Fit(title, 22, 40); title.text = book.Title; }
            var tsh = title.gameObject.AddComponent<Shadow>();
            tsh.effectColor = Color.Lerp(c, Theme.Text, 0.5f).WithAlpha(0.6f);
            tsh.effectDistance = new Vector2(0, -3);

            var sheen = Raw(face.transform, "Sheen", PageStudio.Sheen, Color.white.WithAlpha(0.28f));
            sheen.rectTransform.Stretch(2, 2, 2, 2);

            // okundu rozeti / yaş etiketi
            if (book.Read)
            {
                var badge = UIKit.Box(holder, "Read", Theme.Butter, UIKit.Circle);
                badge.type = Image.Type.Simple;
                badge.rectTransform.Place(new Vector2(1, 1), new Vector2(-6, -6), new Vector2(74, 74));
                var star = UIKit.Box(badge.transform, "Star", Color.white, MascotArt.Star);
                star.type = Image.Type.Simple;
                star.preserveAspect = true;
                star.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(54, 54));
            }
            var age = UIKit.Box(holder, "Age", Theme.Card.WithAlpha(0.92f), UIKit.Rounded, 0.5f);
            age.rectTransform.Place(new Vector2(0, 0), new Vector2(70, 8), new Vector2(116, 46));
            var al = UIKit.Label(age.transform, Loc.T("age_tag", 3 + book.Age * 2), 28, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            al.rectTransform.Stretch(4, 0, 4, 2);
            Fit(al, 16, 28);

            var btn = holder.gameObject.AddComponent<Button>();
            btn.targetGraphic = face;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() =>
            {
                Sfx.Tap();
                Haptics.Light();
                // çizilmiş kapak kitaba devredilir: kitap beklemeden açılır (artık kitaplık değil kitap sahibi)
                if (_covers.TryGetValue(id, out var cover) && cover != null)
                {
                    BookScreen.PendingPreview = cover;
                    BookScreen.PendingPreviewId = id;
                    _covers.Remove(id);
                }
                App.I.ShowBook(id);
            });
            holder.gameObject.AddComponent<PressFeedback>();
            A11y.Label(btn, book.Title);

            holder.localRotation = Quaternion.Euler(0, 0, ((index * 37) % 5 - 2) * 0.6f);
            Tween.Pop(holder, 0.05f + Mathf.Min(index, 8) * 0.04f);
        }

        static RawImage Raw(Transform parent, string name, Texture tex, Color c)
        {
            var rt = UIKit.Node(name, parent);
            var r = rt.gameObject.AddComponent<RawImage>();
            r.texture = tex;
            r.color = c;
            r.raycastTarget = false;
            return r;
        }

        /// <summary>Raf tahtası dokusu: damarlı, sıcak tonlu ahşap.</summary>
        static Texture2D Wood
        {
            get
            {
                if (_wood != null) return _wood;
                const int w = 256, h = 32;
                _wood = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, name = "Wood" };
                var top = Theme.Hex("E2B58A");
                var bot = Theme.Hex("B98356");
                var px = new Color32[w * h];
                var rnd = new System.Random(8);
                var knots = new float[6];
                for (int i = 0; i < knots.Length; i++) knots[i] = (float)rnd.NextDouble() * w;
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float t = y / (h - 1f);
                    var c = Color.Lerp(bot, top, Mathf.SmoothStep(0, 1, t));
                    float grain = Mathf.Sin((y + Mathf.Sin(x * 0.045f) * 2.5f + Mathf.Sin(x * 0.013f + 1.3f) * 3f) * 1.4f) * 0.5f + 0.5f;
                    c = Color.Lerp(c, Color.Lerp(c, Theme.Text, 0.18f), grain * 0.35f);
                    foreach (var k in knots)
                    {
                        float dx = Mathf.Abs(x - k), dy = Mathf.Abs(y - h * 0.5f);
                        float d = Mathf.Sqrt(dx * dx * 0.12f + dy * dy);
                        if (d < 5f) c = Color.Lerp(c, Color.Lerp(c, Theme.Text, 0.3f), (1f - d / 5f) * 0.6f);
                    }
                    if (y == h - 1 || y == h - 2) c = Color.Lerp(c, Color.white, 0.25f);
                    if (y == 0) c = Color.Lerp(c, Theme.Text, 0.25f);
                    px[y * w + x] = c;
                }
                _wood.SetPixels32(px);
                _wood.Apply(true, true);
                return _wood;
            }
        }

        public bool OnBack()
        {
            App.I.ShowWelcome();
            return true;
        }

        void OnDestroy()
        {
            foreach (var t in _covers.Values) if (t != null) Destroy(t);
        }
    }
}
