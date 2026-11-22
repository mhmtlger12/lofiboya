using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>Karşılama ekranı: bulutlu başlık, süzülen polaroid, nefes alan "Başla" butonu.</summary>
    public class WelcomeScreen : MonoBehaviour, IScreen
    {
        RectTransform _polaroid, _title, _startBtn;
        Texture2D _preview;
        Mascot _mascot;
        float _t0;
        static bool _greeted;
        string _dailyGroup, _dailyId;
        const float PolX = 40f, PolY = 70f;
        float _polScale = 1f;   // uzun ekranda polaroid büyür, başlıkla butonlar arasındaki boşluk dolar

        void Start()
        {
            var root = (RectTransform)transform;
            _t0 = Time.unscaledTime;

            // ---- Başlık + bulutlar ----
            var head = UIKit.Node("Head", root);
            head.Place(new Vector2(0.5f, 1f), new Vector2(0, -300), new Vector2(900, 360));
            Cloud(head, new Vector2(-330, 90), 0.9f);
            Cloud(head, new Vector2(340, 120), 0.7f);
            Cloud(head, new Vector2(300, -110), 0.55f);

            var title = UIKit.Label(head, "Lofi Boya", 168, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(900, 220));
            var sh = title.gameObject.AddComponent<Shadow>();
            sh.effectColor = Theme.Accent.WithAlpha(0.55f);
            sh.effectDistance = new Vector2(0, -8);
            _title = title.rectTransform;

            var sub = UIKit.Label(head, Loc.T("app_sub"), 46, Theme.TextSoft);
            sub.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -120), new Vector2(900, 70));

            // ---- Polaroid (rastgele bir resim, pastel renklerle) ----
            // başlık (üstten 480) ile Başla (alttan 495) arasındaki boşluğa göre büyür
            float h = App.I != null && App.I.SafeRoot != null ? App.I.SafeRoot.rect.height : 1920f;
            _polScale = Mathf.Clamp((h - 975f - 200f) / 745f, 1f, 1.3f);
            _polaroid = UIKit.Node("Polaroid", root);
            _polaroid.Place(new Vector2(0.5f, 0.5f), new Vector2(PolX, PolY) * _polScale, new Vector2(600, 700) * _polScale);
            _polaroid.localRotation = Quaternion.Euler(0, 0, -5);
            var card = UIKit.Card(_polaroid, "Card", Theme.Card);
            ((RectTransform)card.transform.parent).Stretch();
            var photoBg = UIKit.Box(card.transform, "PhotoBg", Theme.Paper, null, 2.2f);
            photoBg.rectTransform.Stretch(34, 34, 34, 140);
            var photo = UIKit.Node("Photo", photoBg.transform).Stretch(16, 16, 16, 16);
            var raw = photo.gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            var fitter = photo.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            var caption = UIKit.Label(card.transform, Loc.T("daily"), 42, Theme.TextSoft, TextAnchor.MiddleCenter, FontStyle.Italic);
            caption.rectTransform.Band(false, 112, 16);
            LoadPreview(raw, fitter);
            Tween.Pop(_polaroid, 0.15f, 0.6f);
            // bugünün resmine dokununca hemen boyamaya başla
            var hit = _polaroid.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var pb = _polaroid.gameObject.AddComponent<Button>();
            A11y.Label(pb, Loc.T("a11y_daily"));
            pb.transition = Selectable.Transition.None;
            pb.onClick.AddListener(() =>
            {
                if (_dailyId == null) return;
                Sfx.Tap();
                Haptics.Light();
                App.I.ShowColoring(_dailyGroup, _dailyId);
            });
            _polaroid.gameObject.AddComponent<PressFeedback>();

            // ---- Pofuduk: polaroidin köşesinde oturur, dokununca kıkırdar ----
            _mascot = Mascot.Create(root, 320);
            var mrt = (RectTransform)_mascot.transform;
            mrt.Place(new Vector2(0.5f, 0.5f), new Vector2(-300, -250 * _polScale), mrt.sizeDelta);   // yalnızca dikeyde: yana taşmasın
            Tween.Pop(mrt, 0.45f, 0.6f);
            // Boyama günleri koleksiyonu: Pofuduk'un konuşma balonunda (ara vermek kaybettirmez)
            int streak = SaveSystem.DaysColored;
            if (streak >= 2) Bubble(mrt, Loc.T("bubble_streak", streak));
            else if (!SaveSystem.PlayedToday) Bubble(mrt, Loc.T("bubble_daily"));
            if (!_greeted)
            {
                _greeted = true;
                Tween.Delay(1.1f, () =>
                {
                    if (_mascot == null) return;
                    _mascot.Wave(2f);
                    if (streak >= 2) Speech.Say(Loc.T("hello_streak", streak));
                    else Speech.Say(Loc.T("hello"), "merhaba");
                });
            }

            // ---- Başla ----
            var start = UIKit.Button(root, Loc.T("start"), Theme.AccentButton, Color.white, 64, () => App.I.ShowGallery());
            _startBtn = (RectTransform)start.transform;
            _startBtn.Place(new Vector2(0.5f, 0f), new Vector2(0, 410), new Vector2(700, 170));
            Tween.Pop(_startBtn, 0.35f);

            // ---- Büyük ikonlu butonlar: Öğren / Resimlerim / Çıkartmalarım ----
            BigButton(root, Loc.T("learn"), MascotArt.Puzzle, Theme.Hex("54C29A"), Theme.Mint, new Vector2(-342, 225), 0.45f, () => App.I.ShowLearn());
            BigButton(root, Loc.T("album"), Art.Pencil, Theme.AccentDark, Theme.Sky, new Vector2(0, 225), 0.48f, () => App.I.ShowAlbum());
            var stickers = BigButton(root, Loc.T("stickers_btn"), MascotArt.Star, Color.white, Theme.Butter, new Vector2(342, 225), 0.5f, () => App.I.ShowStickers());
            // yıldız sayısı rozeti
            int stars = SaveSystem.Stars;
            A11y.Label(stickers, Loc.T("stickers_btn") + ", " + Loc.T("a11y_stars", stars));
            if (stars > 0)
            {
                var badge = UIKit.Box(stickers.transform, "Badge", Theme.AccentButton, null, 0.5f);
                badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(1, 1);
                badge.rectTransform.sizeDelta = new Vector2(46 + stars.ToString().Length * 26, 64);
                badge.rectTransform.anchoredPosition = new Vector2(-24, -4);
                var bl = UIKit.Label(badge.transform, stars.ToString(), 38, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                bl.rectTransform.Stretch(0, 0, 0, 4);
            }

            // ---- Üst köşeler: müzik (sol), ebeveyn ayarları (sağ) ----
            var music = UIKit.IconButton(root, Art.Note, Theme.Card.WithAlpha(0.9f), Theme.Text, 110, () =>
            {
                Popup.Toast(Sfx.MusicButton());
            }, 0.46f);
            ((RectTransform)music.transform).Place(new Vector2(0, 1), new Vector2(100, -100), new Vector2(110, 110));
            A11y.Label(music, Loc.T("a11y_music"));
            var gear = UIKit.IconButton(root, Art.Gear, Theme.Card.WithAlpha(0.9f), Theme.TextSoft, 110, () => ParentGate.Show(ParentGate.Settings), 0.5f);
            ((RectTransform)gear.transform).Place(new Vector2(1, 1), new Vector2(-100, -100), new Vector2(110, 110));
            A11y.Label(gear, Loc.T("a11y_settings"));

            // ilk açılış: oyunlar çocuğun yaşına göre ayarlansın (bir kez sorulur; sonra Ayarlar'dan değişir)
            if (!SaveSystem.AgeAsked) Tween.Delay(0.9f, () => { if (this != null && !SaveSystem.AgeAsked) ParentGate.AskAge(); });
        }

        void Bubble(RectTransform mascot, string text)
        {
            var root = (RectTransform)transform;
            var b = UIKit.Card(root, "Bubble", Color.white, 0.6f, 4f);
            var brt = (RectTransform)b.transform.parent;
            brt.Place(new Vector2(0.5f, 0.5f), mascot.anchoredPosition + new Vector2(10, 230), new Vector2(330, 130));
            var tail = UIKit.Box(b.transform, "Tail", Color.white, UIKit.Circle);
            tail.type = Image.Type.Simple;
            tail.rectTransform.Place(new Vector2(0.3f, 0f), new Vector2(0, -12), new Vector2(40, 40));
            var t = UIKit.Label(b.transform, text, 36, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Stretch(10, 6, 10, 10);
            t.lineSpacing = 0.85f;
            brt.localScale = Vector3.zero;
            Tween.Pop(brt, 1.3f, 0.5f);
            brt.gameObject.AddComponent<Floaty>().Init(6f, 0.8f);
        }

        static Button BigButton(Transform parent, string label, Sprite icon, Color iconColor, Color bg, Vector2 pos, float delay, System.Action onClick)
        {
            var b = UIKit.Button(parent, label, bg, Theme.Text, 42, onClick);
            var rt = (RectTransform)b.transform;
            rt.Place(new Vector2(0.5f, 0f), pos, new Vector2(322, 150));
            var txt = UIKit.ButtonText(b);
            txt.rectTransform.offsetMin = new Vector2(112, 0);
            txt.alignment = Loc.IsArabic ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 22;
            txt.resizeTextMaxSize = 42;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.rectTransform.offsetMax = new Vector2(-14, 0);
            var bubble = UIKit.Box(b.transform.Find("Face"), "IconBg", Color.white.WithAlpha(0.7f), UIKit.Circle);
            bubble.type = Image.Type.Simple;
            bubble.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(60, 0), new Vector2(88, 88));
            var img = UIKit.Box(bubble.transform, "Icon", iconColor, icon);
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70, 70));
            Tween.Pop(rt, delay);
            return b;
        }

        static void Cloud(Transform parent, Vector2 pos, float scale)
        {
            var c = UIKit.Node("Cloud", parent);
            c.Place(new Vector2(0.5f, 0.5f), pos, new Vector2(260, 120));
            c.localScale = Vector3.one * scale;
            void Puff(float x, float y, float s)
            {
                var img = UIKit.Box(c, "Puff", Color.white.WithAlpha(0.85f), UIKit.Circle);
                img.type = Image.Type.Simple;
                img.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(s, s));
            }
            Puff(-70, -10, 120); Puff(0, 20, 160); Puff(75, -8, 125);
            var baseBar = UIKit.Box(c, "Base", Color.white.WithAlpha(0.85f), null, 1.4f);
            baseBar.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -38), new Vector2(250, 70));
            c.gameObject.AddComponent<Floaty>().Init(6f + scale * 10f, 0.4f + scale * 0.2f);
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
                if (System.Array.IndexOf(want, g.name) >= 0)
                    foreach (var l in g.levels) all.Add((g.name, l.id));
            if (all.Count == 0) foreach (var g in groups) foreach (var l in g.levels) all.Add((g.name, l.id));
            if (all.Count == 0) return;
            // Bugünün resmi: her gün değişir, gün içinde aynı kalır; bitmemişler öncelikli
            var undone = all.FindAll(e => { var s = SaveSystem.Load(e.g + "_" + e.id); return s == null || !s.done; });
            var pool = undone.Count > 0 ? undone : all;
            var now = System.DateTime.Now;
            var pick = pool[(now.Year * 372 + now.DayOfYear) % pool.Count];
            _dailyGroup = pick.g;
            _dailyId = pick.id;

            // Kayıtlı renkli hâli varsa onu göster, yoksa pastel renklerle boya
            _preview = SaveSystem.LoadThumb(pick.g + "_" + pick.id);
            if (_preview == null)
            {
                var lv = Level.Load(pick.g, pick.id);
                if (lv == null) return;
                var pal = Theme.Palettes[0].Colors;
                var cols = new Color32[lv.Count + 1];
                var rnd = new System.Random(System.DateTime.Now.DayOfYear);
                for (int i = 0; i < cols.Length; i++) cols[i] = pal[rnd.Next(pal.Length - 2)];
                // en büyük bölge (genelde arka plan) kâğıt rengi kalsın
                int big = 1;
                for (int i = 1; i <= lv.Count; i++) if (lv.Meta.area[i - 1] > lv.Meta.area[big - 1]) big = i;
                cols[big] = Theme.Paper;
                _preview = lv.RenderThumbnail(cols, 520);
                lv.Unload();
            }
            raw.texture = _preview;
            fitter.aspectRatio = (float)_preview.width / _preview.height;
        }

        void Update()
        {
            if (Motion.Reduced) return;   // sakin mod / sistem animasyonları kapalı: her şey yerinde dursun
            float t = Time.unscaledTime - _t0;
            if (_polaroid != null && t > 0.8f)
            {
                _polaroid.anchoredPosition = new Vector2(PolX, PolY) * _polScale + new Vector2(0, Mathf.Sin(t * 1.1f) * 14f);
                _polaroid.localRotation = Quaternion.Euler(0, 0, -5 + Mathf.Sin(t * 0.7f) * 2f);
            }
            if (_title != null) _title.anchoredPosition = new Vector2(0, 10 + Mathf.Sin(t * 1.6f) * 6f);
            if (_startBtn != null && t > 0.9f)
            {
                float s = 1f + 0.035f * Mathf.Sin(t * 2.4f);
                if (_startBtn.localScale.x > 0.5f) _startBtn.localScale = new Vector3(s, s, 1);
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
