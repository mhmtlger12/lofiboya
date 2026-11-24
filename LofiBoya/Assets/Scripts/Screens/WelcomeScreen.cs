using System;
using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Karşılama ekranı. Görseller Google Stitch tasarımından kesildi (tools/anaekran/anaekran_kes.py → Resources/MainMenu):
    /// üstte kalem + gökkuşağı + "Lofi Boya" tabelası, ortada şövale (kâğıdında bugünün resmi) ve bulut, altta
    /// "Boyamaya Başla" ile dört kart. Resimlerdeki yazılar silindi; oyun kendi yazısını seçili dilde yazar.
    /// Yerleşim tasarımdaki ölçülerle (layout.json); uzun ekranda fazla boşluk gruplar arasına paylaştırılır.
    /// </summary>
    public class WelcomeScreen : MonoBehaviour, IScreen
    {
        [Serializable] class CardInfo { public int[] text, box, badge; }
        [Serializable] class Cards { public CardInfo read, learn, album, stickers; }
        [Serializable]
        class Layout
        {
            public int[] src, paper_col, sub, header, music, gear, paper, plaque, bubble_text, bubble, easel, start_text, start, bg;
            public int btn_top;
            public Cards cards;
        }

        static Layout _l;

        static Layout L
        {
            get
            {
                if (_l != null) return _l;
                var ta = Resources.Load<TextAsset>("MainMenu/layout");
                _l = ta != null ? JsonUtility.FromJson<Layout>(ta.text) : new Layout();
                if (ta != null) Resources.UnloadAsset(ta);
                return _l;
            }
        }

        static readonly Color Ink = Theme.Hex("5B2E8C");   // tasarımdaki mor yazı

        RectTransform _root, _easel, _bubble, _startBtn;
        PressFeedback _startPress;   // nefes animasyonu basma geri bildirimini ezmesin
        Texture2D _preview;
        float _t0, _s, _top, _gap1, _gap2, _easelY, _bubbleY;
        static bool _greeted, _ageShown;
        string _dailyGroup, _dailyId;

        void Start()
        {
            _root = (RectTransform)transform;
            _t0 = Time.unscaledTime;
            if (L.src == null) { Debug.LogError("MainMenu/layout.json yok"); return; }
            Measure();
            _laidOut = _root.rect.size;

            Background();

            // ---- alt: Başla + dört kart (önce: şövalenin pastel boyası butonun üstüne biner) ----
            var start = SpriteButton("start", L.start, Bottom, () => App.I.ShowGallery(), Loc.T("start"));
            _startBtn = start;
            _startPress = start.GetComponent<PressFeedback>();
            var st = TextIn(start, L.start, Grow(L.start_text, 10, 4), Loc.T("start"), 64, 36, Color.white);
            var sh = st.gameObject.AddComponent<Shadow>();
            sh.effectColor = Theme.Hex("C2375E").WithAlpha(0.55f);
            sh.effectDistance = new Vector2(0, -4);
            Tween.Pop(start, 0.35f);

            Card("read", L.cards.read, Loc.T("read_btn"), 0.42f, () => App.I.ShowLibrary());
            Card("learn", L.cards.learn, Loc.T("learn"), 0.46f, () => App.I.ShowLearn());
            Card("album", L.cards.album, Loc.T("album"), 0.5f, () => App.I.ShowAlbum());
            var stickers = Card("stickers", L.cards.stickers, Loc.T("stickers_btn"), 0.54f, () => App.I.ShowStickers());
            int stars = SaveSystem.Stars;
            A11y.Label(stickers.GetComponent<Button>(), Loc.T("stickers_btn") + ", " + Loc.T("a11y_stars", stars));
            var badge = TextIn(stickers, L.cards.stickers.box, Grow(L.cards.stickers.badge, -8, -6), stars.ToString(), 34, 20, Color.white);
            badge.rectTransform.anchoredPosition += new Vector2(0, 2);

            // ---- orta: şövale (dokununca bugünün resmi boyanmaya başlar) + balon ----
            _easel = SpriteButton("easel", L.easel, Middle, () =>
            {
                if (_dailyId == null) return;
                Haptics.Light();
                App.I.ShowColoring(_dailyGroup, _dailyId);
            }, Loc.T("a11y_daily"));
            _easel.GetComponent<PressFeedback>().pressedScale = 0.97f;
            _easelY = _easel.anchoredPosition.y;
            var paper = UIKit.Node("Paper", _easel);
            SetIn(paper, L.easel, Grow(L.paper, -22, -22));
            var photo = UIKit.Node("Photo", paper).Stretch();
            var raw = photo.gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            var fitter = photo.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            LoadPreview(raw, fitter);
            TextIn(_easel, L.easel, Grow(L.plaque, -4, 9), Loc.T("daily").Replace("~", "").Trim(), 38, 20, Ink);
            Tween.Pop(_easel, 0.15f, 0.6f);

            int streak = SaveSystem.DaysColored;
            string say = streak >= 2 ? Loc.T("bubble_streak", streak) : !SaveSystem.PlayedToday ? Loc.T("bubble_daily") : null;
            if (say != null)
            {
                var bubble = Piece("bubble", L.bubble, Middle);
                var bt = TextIn(bubble, L.bubble, Grow(L.bubble_text, 6, 6), say, 40, 20, Ink);
                bt.lineSpacing = 0.85f;
                bt.rectTransform.localRotation = Quaternion.Euler(0, 0, 8f);   // tasarımdaki yazı balonla birlikte hafif eğik
                bubble.localScale = Vector3.zero;
                Tween.Pop(bubble, 1.1f, 0.5f);
                _bubble = bubble;
                _bubbleY = bubble.anchoredPosition.y;
            }
            if (!_greeted)
            {
                _greeted = true;
                Tween.Delay(1.1f, () =>
                {
                    if (this == null) return;
                    if (streak >= 2) Speech.Say(Loc.T("hello_streak", streak));
                    else Speech.Say(Loc.T("hello"), "merhaba");
                });
            }

            // ---- üst: tabela + köşe düğmeleri ----
            var header = Piece("header", L.header, Top);
            var sub = TextIn(header, L.header, Grow(L.sub, 20, 9), Loc.T("app_sub"), 46, 22, Ink);
            sub.fontStyle = FontStyle.Bold;
            Tween.Pop(header, 0.05f, 0.6f);

            var music = SpriteButton("music", L.music, Top, () => Popup.Toast(Sfx.MusicButton()), Loc.T("a11y_music"));
            var gear = SpriteButton("gear", L.gear, Top, () => ParentGate.Show(ParentGate.Settings), Loc.T("a11y_settings"));
            Tween.Pop(music, 0.46f);
            Tween.Pop(gear, 0.5f);

            // ilk açılış: oyunlar çocuğun yaşına göre ayarlansın (bir kez sorulur; sonra Ayarlar'dan değişir)
            if (!SaveSystem.AgeAsked && !_ageShown)
            {
                _ageShown = true;
                Tween.Delay(0.9f, () => { if (this != null && !SaveSystem.AgeAsked) ParentGate.AskAge(); });
            }
        }

        // ---------------- yerleşim ----------------

        const int Top = 0, Middle = 1, Bottom = 2;

        /// <summary>
        /// Ölçek: tasarımın düğmeden düğmeye genişliği ekrana, tüm yüksekliği güvenli alana sığar. Fazla dikey boşluk
        /// tabela–şövale ve şövale–buton arasına bölünür (şövale ortada kalır).
        /// </summary>
        void Measure()
        {
            float w = _root.rect.width > 0 ? _root.rect.width : 1080f;
            float h = _root.rect.height > 0 ? _root.rect.height : 1920f;
            float spanW = L.gear[2] - L.music[0];
            float spanH = L.cards.album.box[3] - L.header[1];
            const float margin = 24f;
            _s = Mathf.Min((w - 2 * margin) / spanW, (h - 2 * margin) / spanH);
            float extra = Mathf.Max(0, h - spanH * _s - 2 * margin);
            _top = margin + extra * 0.08f;
            _gap1 = extra * 0.42f;
            _gap2 = extra * 0.42f;
        }

        float GroupDy(int g) => g == Top ? 0 : g == Middle ? _gap1 : _gap1 + _gap2;

        // Ölçek ekran boyuna bağlı; ilk karede Canvas ölçekleyicisi henüz çalışmamış olabilir (kök, ekranın ham piksel
        // boyunu gösterir). Bu yüzden her yerleştirme saklanır, boyut değişince hepsi yeniden uygulanır.
        readonly System.Collections.Generic.List<Action> _relayout = new();
        Vector2 _laidOut;

        void Relayout()
        {
            var size = _root.rect.size;
            if (size == _laidOut || size.x <= 0 || size.y <= 0) return;
            _laidOut = size;
            Measure();
            foreach (var a in _relayout) a();
            if (_easel != null) _easelY = _easel.anchoredPosition.y;
            if (_bubble != null) _bubbleY = _bubble.anchoredPosition.y;
        }

        void Place(RectTransform r, int[] b, int group)
        {
            _relayout.Add(() => PlaceNow(r, b, group));
            PlaceNow(r, b, group);
        }

        void SetIn(RectTransform r, int[] parent, int[] b)
        {
            _relayout.Add(() => SetInNow(r, parent, b));
            SetInNow(r, parent, b);
        }

        /// <summary>Tasarımdaki kutuyu ekrana yerleştirir (üst-orta çapa, ortasından ölçeklenir).</summary>
        void PlaceNow(RectTransform r, int[] b, int group)
        {
            float cx = L.src[0] * 0.5f;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(b[2] - b[0], b[3] - b[1]) * _s;
            r.anchoredPosition = new Vector2(((b[0] + b[2]) * 0.5f - cx) * _s,
                -(_top + ((b[1] + b[3]) * 0.5f - L.header[1]) * _s + GroupDy(group)));
        }

        /// <summary>Parçanın içinde, tasarım koordinatıyla verilen alt kutu.</summary>
        void SetInNow(RectTransform r, int[] parent, int[] b)
        {
            r.anchorMin = r.anchorMax = new Vector2(0, 1);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(b[2] - b[0], b[3] - b[1]) * _s;
            r.anchoredPosition = new Vector2((b[0] + b[2]) * 0.5f - parent[0], -((b[1] + b[3]) * 0.5f - parent[1])) * _s;
        }

        static int[] Grow(int[] b, int dx, int dy) => new[] { b[0] - dx, b[1] - dy, b[2] + dx, b[3] + dy };

        RectTransform Piece(string file, int[] b, int group)
        {
            var img = UIKit.Box(_root, file, Color.white, Resources.Load<Sprite>("MainMenu/" + file));
            img.type = Image.Type.Simple;
            Place(img.rectTransform, b, group);
            return img.rectTransform;
        }

        RectTransform SpriteButton(string file, int[] b, int group, Action onClick, string label)
        {
            var r = Piece(file, b, group);
            var img = r.GetComponent<Image>();
            img.raycastTarget = true;
            var btn = r.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { Sfx.Tap(); onClick?.Invoke(); });
            r.gameObject.AddComponent<PressFeedback>();
            A11y.Label(btn, label);
            return r;
        }

        RectTransform Card(string file, CardInfo c, string label, float delay, Action onClick)
        {
            var r = SpriteButton("card_" + file, c.box, Bottom, onClick, label);
            // yazı alanı: tasarımdaki yazının satırı, kartın iki yanına kadar (uzun diller sığsın)
            var t = c.text;
            TextIn(r, c.box, new[] { c.box[0] + 30, t[1] - 14, c.box[2] - 30, t[3] + 12 }, label, 54, 24, Ink);
            Tween.Pop(r, delay);
            return r;
        }

        Text TextIn(RectTransform parent, int[] pb, int[] b, string text, int max, int min, Color col)
        {
            var t = UIKit.Label(parent, text, max, col, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = min;
            t.resizeTextMaxSize = max;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;   // best fit ancak taşma kapalıyken küçültür
            SetIn(t.rectTransform, pb, b);
            return t;
        }

        void Background()
        {
            // güvenli alanın dışına da taşar (çentik, gezinme çubuğu); resim ekranı kaplar, fazlası kenarlardan kırpılır
            var holder = UIKit.Node("Bg", _root).Stretch(0, -300, 0, -300);
            var img = UIKit.Box(holder, "Img", Color.white, Resources.Load<Sprite>("MainMenu/bg"));
            img.type = Image.Type.Simple;
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            var fit = img.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = L.bg != null && L.bg[1] > 0 ? (float)L.bg[0] / L.bg[1] : 0.45f;
        }

        void LoadPreview(RawImage raw, AspectRatioFitter fitter)
        {
            var groups = Level.Index.groups;
            var all = new System.Collections.Generic.List<(string g, string id)>();
            // bugünün resmi çocuğun yaşına uygun gruptan gelir (3–4 yaşa 400 bölgeli mandala çıkmasın);
            // 7+ yaşta orta ve zor birlikte; o grupta resim yoksa hepsinden
            string[] want = SaveSystem.AgeBand switch
            {
                0 => new[] { "kolay" },
                1 => new[] { "kolay", "orta" },
                _ => new[] { "orta", "zor" },
            };
            foreach (var g in groups)
                if (Array.IndexOf(want, g.name) >= 0)
                    foreach (var l in g.levels) all.Add((g.name, l.id));
            if (all.Count == 0) foreach (var g in groups) foreach (var l in g.levels) all.Add((g.name, l.id));
            if (all.Count == 0) return;
            // Bugünün resmi: her gün değişir, gün içinde aynı kalır; bitmemişler öncelikli
            var undone = all.FindAll(e => { var s = SaveSystem.Load(e.g + "_" + e.id); return s == null || !s.done; });
            var pool = undone.Count > 0 ? undone : all;
            var now = DateTime.Now;
            var pick = pool[(now.Year * 372 + now.DayOfYear) % pool.Count];
            _dailyGroup = pick.g;
            _dailyId = pick.id;

            // Kayıtlı renkli hâli varsa onu göster, yoksa boyanmamış çizgi hâli (şövalede boyanmayı bekler)
            _preview = SaveSystem.LoadThumb(pick.g + "_" + pick.id);
            if (_preview == null)
            {
                var lv = Level.Load(pick.g, pick.id);
                if (lv == null) return;
                // bölgeler şövaledeki kâğıdın rengiyle: resim kâğıda basılmış gibi, kenarında ayrı bir beyaz kutu kalmaz
                var pc = L.paper_col;
                Color32 paperCol = pc != null && pc.Length == 3 ? new Color32((byte)pc[0], (byte)pc[1], (byte)pc[2], 255) : (Color32)Theme.Paper;
                var cols = new Color32[lv.Count + 1];
                for (int i = 0; i < cols.Length; i++) cols[i] = paperCol;
                _preview = lv.RenderThumbnail(cols, 520);
                lv.Unload();
                // bölgelerin dışı (resmin zemini) Theme.Paper ile çizilir: o da kâğıt rengine
                var px = _preview.GetPixels32();
                Color32 tp = Theme.Paper;
                for (int i = 0; i < px.Length; i++)
                    if (px[i].r == tp.r && px[i].g == tp.g && px[i].b == tp.b) px[i] = paperCol;
                _preview.SetPixels32(px);
                _preview.Apply(false);
            }
            raw.texture = _preview;
            fitter.aspectRatio = (float)_preview.width / _preview.height;
        }

        void Update()
        {
            if (_root != null && L.src != null) Relayout();
            if (Motion.Reduced) return;   // sakin mod / sistem animasyonları kapalı: her şey yerinde dursun
            float t = Time.unscaledTime - _t0;
            if (_easel != null && t > 0.8f)
                _easel.anchoredPosition = new Vector2(_easel.anchoredPosition.x, _easelY + Mathf.Sin(t * 1.1f) * 5f);
            if (_bubble != null)   // balon şövaleden ayrı, kendi temposunda süzülür
                _bubble.anchoredPosition = new Vector2(_bubble.anchoredPosition.x, _bubbleY + Mathf.Sin(t * 1.6f + 1f) * 7f);
            if (_startBtn != null && t > 0.9f && (_startPress == null || !_startPress.Busy))
            {
                float s = 1f + 0.03f * Mathf.Sin(t * 2.4f);
                if (_startBtn.localScale.x > 0.5f && _startBtn.localScale.x < 1.1f) _startBtn.localScale = new Vector3(s, s, 1);
            }
        }

        // Android geri tuşu: yanlışlıkla basılınca uygulama kapanmasın; 2 sn içinde ikinci basış çıkar
        float _backAt = -10f;

        public bool OnBack()
        {
            if (Time.unscaledTime - _backAt < 2f)
            {
                Application.Quit();
                return true;
            }
            _backAt = Time.unscaledTime;
            Popup.Toast(Loc.T("back_again"));
            return true;
        }

        void OnDestroy()
        {
            if (_preview) Destroy(_preview);
        }
    }
}
