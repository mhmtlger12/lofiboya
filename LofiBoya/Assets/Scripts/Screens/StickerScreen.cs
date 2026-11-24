using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Çıkartma albümü: bitirilen her resim, beyaz kenarlı, hafif yamuk bir çıkartma olur.
    /// Henüz boyanmamışlar soluk gölge olarak bekler; dokununca o resmi boyamaya gider.
    /// </summary>
    public class StickerScreen : MonoBehaviour, IScreen
    {
        readonly List<Texture2D> _textures = new();

        // Küçük resimler tembel yüklenir: yalnızca görünen (ve yakınındaki) hücreler, karede birkaç tane.
        // Eskiden 143 resmin hepsi açılışta tek karede çözülüyordu (eski telefonlarda donma / bellek taşması).
        class LazyThumb
        {
            public RectTransform cell;
            public RawImage raw;
            public AspectRatioFitter fit;
            public System.Func<Texture2D> load;
            public Texture2D tex;
            public Color color;
        }
        readonly List<LazyThumb> _lazy = new();
        readonly List<(RectTransform cell, Mascot m)> _mascots = new();
        RectTransform _viewport;
        static readonly Vector3[] _corners = new Vector3[4];
        const int LoadsPerFrame = 3;

        void Start()
        {
            var root = (RectTransform)transform;
            Emoji.Use();

            var back = UIKit.CircleButton(root, "‹", Theme.Card, Theme.Text, 120, 90, () => App.I.ShowWelcome());
            ((RectTransform)back.transform).Place(new Vector2(0, 1), new Vector2(110, -110), new Vector2(120, 120));
            StarPill(root, SaveSystem.Stars);

            var title = UIKit.Label(root, Loc.T("stickers_title"), 84, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Band(true, 110, 200);

            // Tüm resimler
            var all = new List<(string g, string id, bool done)>();
            int done = 0;
            foreach (var g in Level.Index.groups)
                foreach (var l in g.levels)
                {
                    var s = SaveSystem.Load(g.name + "_" + l.id);
                    bool d = s != null && s.done;
                    if (d) done++;
                    all.Add((g.name, l.id, d));
                }
            // bitenler önce
            all.Sort((a, b) => b.done.CompareTo(a.done));

            var sub = UIKit.Label(root, done == 0 ? Loc.T("stickers_none") : Loc.T("stickers_count", done, all.Count), 42, Theme.TextSoft);
            sub.rectTransform.Band(true, 60, 305);

            // Çıkartma Dünyası: bütün emojileri kendi sahnesine yapıştırma
            float top = 390;
            if (Emoji.Available)
            {
                var world = UIKit.Button(root, Loc.T("world"), Theme.Hex("C9E6FF"), Theme.Text, 50, () => App.I.ShowStickerWorld());
                var wrt = (RectTransform)world.transform;
                wrt.Band(true, 130, 385, 60);
                var wt = UIKit.ButtonText(world);
                wt.rectTransform.Stretch(160, 0, 160, 4);
                wt.horizontalOverflow = HorizontalWrapMode.Wrap;
                wt.verticalOverflow = VerticalWrapMode.Truncate;
                wt.resizeTextForBestFit = true;
                wt.resizeTextMinSize = 28;
                wt.resizeTextMaxSize = 50;
                var face = world.transform.Find("Face");
                foreach (var (id, x, rot) in new[] { ("animal/rabbit", 80f, 8f), ("nature/sun_with_face", -80f, -8f) })
                {
                    var img = Emoji.Image(face, id, 100);
                    img.raycastTarget = false;
                    img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(x > 0 ? 0 : 1, 0.5f);
                    img.rectTransform.anchoredPosition = new Vector2(x, 0);
                    img.rectTransform.localRotation = Quaternion.Euler(0, 0, rot);
                }
                Tween.Pop(wrt, 0.1f);
                top = 530;
            }

            // Kaydırılabilir ızgara
            var scrollRt = UIKit.Node("Scroll", root).Stretch(30, top, 30, 30);
            var sr = scrollRt.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 40;
            var vp = UIKit.Node("Viewport", scrollRt).Stretch();
            vp.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(0, 30);
            vp.gameObject.AddComponent<Image>().color = Color.clear;
            sr.viewport = vp;
            _viewport = vp;
            var content = UIKit.Node("Content", vp);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(310, 350);
            grid.spacing = new Vector2(16, 20);
            grid.padding = new RectOffset(8, 8, 30, 60);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = App.I.CanvasWidth >= 1380f ? 4 : 3;   // tablette 4 sütun
            grid.childAlignment = TextAnchor.UpperCenter;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = content;

            // Pofuduk koleksiyonu: resim bitirdikçe şapkalı Pofuduklar açılır
            for (int i = 0; i < PuffyStickers.Hats.Length; i++)
                PuffySticker(content, i, done);
            // yıldızlarla açılan duygulu Pofuduklar
            for (int i = 0; i < PuffyStickers.StarMoods.Length; i++)
                StarSticker(content, i);
            // okunan hikayelerle açılan Pofuduklar
            for (int i = 0; i < PuffyStickers.BookPuffies.Length; i++)
                BookSticker(content, i);

            for (int i = 0; i < all.Count; i++)
            {
                var e = all[i];
                Sticker(content, e.g, e.id, e.done, i);
            }
        }

        void StarSticker(Transform parent, int i)
        {
            var (mood, need) = PuffyStickers.StarMoods[i];
            int have = SaveSystem.Stars;
            bool open = have >= need;
            var cell = UIKit.Node("StarPuffy", parent);
            var holder = UIKit.Node("Sticker", cell);
            holder.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 14), new Vector2(270, 270));
            holder.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0 ? 1 : -1) * 4f);
            if (open)
            {
                var face = UIKit.Card(holder, "Paper", Theme.Sky, 0.45f, 6f);
                ((RectTransform)face.transform.parent).Stretch();
            }
            else
            {
                var face = UIKit.Box(holder, "Slot", Theme.Text.WithAlpha(0.06f), null, 0.45f);
                face.rectTransform.Stretch();
            }
            var m = Mascot.Create(holder, 200, false);
            _mascots.Add((cell, m));
            m.SetMood(mood);
            var mrt = (RectTransform)m.transform;
            mrt.anchorMin = mrt.anchorMax = new Vector2(0.5f, 0.38f);
            mrt.anchoredPosition = Vector2.zero;
            if (!open)
            {
                m.SetInteractive(false);
                m.gameObject.AddComponent<CanvasGroup>().alpha = 0.25f;
            }
            string label = open ? Names.Cap(Names.Feel(mood.ToString().ToLowerInvariant()).name) : Loc.T("stars_more", need - have);
            var l = UIKit.Label(cell, label, 30, open ? Theme.Text : Theme.TextSoft, TextAnchor.MiddleCenter, FontStyle.Bold);
            l.rectTransform.Band(false, 44, 0);
            l.resizeTextForBestFit = true;
            l.resizeTextMinSize = 20;
            l.resizeTextMaxSize = 30;
        }

        void BookSticker(Transform parent, int i)
        {
            var (mood, need, name) = PuffyStickers.BookPuffies[i];
            int have = Books.ReadCount;
            bool open = have >= need;
            var cell = UIKit.Node("BookPuffy", parent);
            var holder = UIKit.Node("Sticker", cell);
            holder.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 14), new Vector2(270, 270));
            holder.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0 ? -1 : 1) * 4f);
            if (open)
            {
                var face = UIKit.Card(holder, "Paper", Theme.Lavender, 0.45f, 6f);
                ((RectTransform)face.transform.parent).Stretch();
            }
            else
            {
                var face = UIKit.Box(holder, "Slot", Theme.Text.WithAlpha(0.06f), null, 0.45f);
                face.rectTransform.Stretch();
            }
            var inner = UIKit.Node("Inner", holder).Stretch();
            var m = Mascot.Create(inner, 200, false);
            _mascots.Add((cell, m));
            m.SetMood(mood);
            if (i == 2) m.SetHat(MascotHat.Crown);
            var mrt = (RectTransform)m.transform;
            mrt.anchorMin = mrt.anchorMax = new Vector2(0.5f, 0.38f);
            mrt.anchoredPosition = Vector2.zero;
            // kucağında açık kitap; rüya Pofuduğunun başında yıldızlar
            if (i != 1)
            {
                var book = UIKit.Box(inner, "Book", Theme.Hex("8E7CC3"), BookPages.BookIcon);
                book.type = Image.Type.Simple;
                book.preserveAspect = true;
                book.rectTransform.Place(new Vector2(0.5f, 0.38f), new Vector2(0, -52), new Vector2(110, 110));
            }
            else
            {
                foreach (var (x, y, sz) in new[] { (-70f, 92f, 44f), (62f, 104f, 34f), (8f, 124f, 26f) })
                {
                    var st = UIKit.Box(inner, "Star", Theme.Hex("FFC93C"), Art.Star5);
                    st.type = Image.Type.Simple;
                    st.rectTransform.Place(new Vector2(0.5f, 0.38f), new Vector2(x, y), new Vector2(sz, sz));
                }
            }
            if (!open)
            {
                m.SetInteractive(false);
                inner.gameObject.AddComponent<CanvasGroup>().alpha = 0.25f;
            }
            string label = open ? Loc.T(name) : Loc.T("books_more", need - have);
            var l = UIKit.Label(cell, label, 30, open ? Theme.Text : Theme.TextSoft, TextAnchor.MiddleCenter, FontStyle.Bold);
            l.rectTransform.Band(false, 44, 0);
            l.horizontalOverflow = HorizontalWrapMode.Wrap;
            l.verticalOverflow = VerticalWrapMode.Truncate;
            l.resizeTextForBestFit = true;
            l.resizeTextMinSize = 18;
            l.resizeTextMaxSize = 30;
        }

        void PuffySticker(Transform parent, int i, int done)
        {
            var (hat, need) = PuffyStickers.Hats[i];
            bool open = done >= need;
            var cell = UIKit.Node("Puffy", parent);
            var holder = UIKit.Node("Sticker", cell);
            holder.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 14), new Vector2(270, 270));
            holder.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0 ? -1 : 1) * 4f);
            if (open)
            {
                var face = UIKit.Card(holder, "Paper", Theme.Butter, 0.45f, 6f);
                ((RectTransform)face.transform.parent).Stretch();
            }
            else
            {
                var face = UIKit.Box(holder, "Slot", Theme.Text.WithAlpha(0.06f), null, 0.45f);
                face.rectTransform.Stretch();
            }
            var m = Mascot.Create(holder, 200, false);
            _mascots.Add((cell, m));
            m.SetHat(hat);
            var mrt = (RectTransform)m.transform;
            mrt.anchorMin = mrt.anchorMax = new Vector2(0.5f, 0.38f);
            mrt.anchoredPosition = Vector2.zero;
            if (!open)
            {
                m.SetInteractive(false);
                m.gameObject.AddComponent<CanvasGroup>().alpha = 0.25f;
            }
            var label = UIKit.Label(cell, open ? Loc.T("puffy_name") : Loc.T("puffy_more", need - done), 30, open ? Theme.Text : Theme.TextSoft, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.rectTransform.Band(false, 44, 0);
        }

        void Sticker(Transform parent, string group, string id, bool done, int index)
        {
            var cell = UIKit.Node("Cell", parent);
            var holder = UIKit.Node("Sticker", cell);
            holder.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 14), new Vector2(270, 270));
            // her çıkartma kendine has hafif bir açıyla yapışır
            uint h = 2166136261u;
            foreach (char ch in id) h = (h ^ ch) * 16777619u;
            holder.localRotation = Quaternion.Euler(0, 0, ((h % 1000) / 1000f - 0.5f) * 14f);

            System.Func<Texture2D> load;
            if (done)
            {
                // beyaz "kesim" kenarı + gölge
                var face = UIKit.Card(holder, "Paper", Color.white, 0.45f, 6f);
                ((RectTransform)face.transform.parent).Stretch();
                load = () => SaveSystem.LoadThumb(group + "_" + id) ?? Level.LoadThumb(group, id);
            }
            else
            {
                var face = UIKit.Box(holder, "Slot", Theme.Text.WithAlpha(0.06f), null, 0.45f);
                face.rectTransform.Stretch();
                load = () => Level.LoadThumb(group, id);
            }
            {
                var photo = UIKit.Node("Photo", holder).Stretch(18, 18, 18, 18);
                var raw = photo.gameObject.AddComponent<RawImage>();
                raw.raycastTarget = false;
                raw.enabled = false;   // resim yüklenince açılır
                var fit = photo.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                _lazy.Add(new LazyThumb { cell = cell, raw = raw, fit = fit, load = load, color = done ? Color.white : new Color(1, 1, 1, 0.28f) });
            }
            if (!done)
            {
                var q = UIKit.Label(holder, "?", 120, Theme.TextSoft.WithAlpha(0.6f), TextAnchor.MiddleCenter, FontStyle.Bold);
                q.rectTransform.Stretch();
            }
            else
            {
                // köşede parlak bir yıldız
                var star = UIKit.Box(holder, "Star", Color.white, MascotArt.Star);
                star.type = Image.Type.Simple;
                star.rectTransform.anchorMin = star.rectTransform.anchorMax = new Vector2(1, 1);
                star.rectTransform.sizeDelta = new Vector2(76, 76);
                star.rectTransform.anchoredPosition = new Vector2(-10, -10);
            }

            var name = UIKit.Label(cell, done ? Names.LevelTitle(id) : "", 32, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            name.rectTransform.Band(false, 44, 0);

            var hit = cell.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var b = cell.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() =>
            {
                if (done)
                {
                    Sfx.Pop();
                    Tween.Punch(holder, 0.18f, 0.4f);
                    Speech.Say(Names.LevelTitle(id) + "!");
                }
                else
                {
                    Sfx.Tap();
                    App.I.ShowColoring(group, id);
                }
            });
            cell.gameObject.AddComponent<PressFeedback>();
            // kilitli çıkartma ekran okuyucuda yalnızca "?" değil: resmin adı ve "boyayınca açılır"
            A11y.Label(b, done ? Names.LevelTitle(id) : Loc.T("a11y_sticker_todo", Names.LevelTitle(id)));
            holder.localScale = Vector3.zero;
            Tween.Pop(holder, 0.05f + Mathf.Min(index, 15) * 0.03f, 0.45f);
        }

        /// <summary>Hücrenin görünür alana uzaklığı (görünür alan yüksekliği cinsinden): 0 = görünüyor.</summary>
        float Distance(RectTransform cell, Rect view)
        {
            cell.GetWorldCorners(_corners);
            float yMin = Mathf.Min(_corners[0].y, _corners[2].y), yMax = Mathf.Max(_corners[0].y, _corners[2].y);
            if (yMax < view.yMin) return (view.yMin - yMax) / Mathf.Max(1f, view.height);
            if (yMin > view.yMax) return (yMin - view.yMax) / Mathf.Max(1f, view.height);
            return 0f;
        }

        void Update()
        {
            if (_viewport == null) return;
            _viewport.GetWorldCorners(_corners);
            var view = new Rect(_corners[0].x, Mathf.Min(_corners[0].y, _corners[2].y), _corners[2].x - _corners[0].x, Mathf.Abs(_corners[2].y - _corners[0].y));
            if (view.height <= 1f) return;

            int budget = LoadsPerFrame;
            foreach (var t in _lazy)
            {
                if (t.cell == null) continue;
                float d = Distance(t.cell, view);
                if (t.tex == null && d <= 0.5f && budget > 0)
                {
                    budget--;
                    t.tex = t.load();
                    if (t.tex == null) { t.load = () => null; continue; }
                    _textures.Add(t.tex);
                    t.raw.texture = t.tex;
                    t.raw.color = t.color;
                    t.raw.enabled = true;
                    t.fit.aspectRatio = (float)t.tex.width / t.tex.height;
                }
                else if (t.tex != null && d > 3f)
                {
                    // çok uzaklaştı: belleği bırak, yaklaşınca yeniden yüklenir
                    t.raw.texture = null;
                    t.raw.enabled = false;
                    _textures.Remove(t.tex);
                    Destroy(t.tex);
                    t.tex = null;
                }
            }
            // ekran dışındaki Pofuduklar nefes almasın (her biri kendi Update'i)
            foreach (var (cell, m) in _mascots)
                if (cell != null && m != null) m.enabled = Distance(cell, view) <= 0.25f;
        }

        /// <summary>Sağ üstte yıldız sayacı.</summary>
        public static Text StarPill(RectTransform root, int stars)
        {
            var pill = UIKit.Card(root, "Stars", Theme.Card, 0.5f, 4f);
            var prt = (RectTransform)pill.transform.parent;
            prt.Place(new Vector2(1, 1), new Vector2(-150, -110), new Vector2(210, 100));
            var icon = UIKit.Box(pill.transform, "Star", Color.white, MascotArt.Star);
            icon.type = Image.Type.Simple;
            icon.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(56, 2), new Vector2(78, 78));
            var t = UIKit.Label(pill.transform, stars.ToString(), 48, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Stretch(90, 0, 10, 4);
            return t;
        }

        public bool OnBack()
        {
            App.I.ShowWelcome();
            return true;
        }

        void OnDestroy()
        {
            foreach (var t in _textures) if (t) Destroy(t);
            Emoji.Release();
        }
    }
}
