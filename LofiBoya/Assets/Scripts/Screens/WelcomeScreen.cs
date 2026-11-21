using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>Karşılama ekranı: bulutlu başlık, süzülen polaroid, nefes alan "Başla" butonu.</summary>
    public class WelcomeScreen : MonoBehaviour, IScreen
    {
        RectTransform _polaroid, _title, _startBtn;
        Texture2D _preview;
        Text _musicLabel, _muteLabel;
        float _t0;

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

            var sub = UIKit.Label(head, "rahatla  •  boya  •  nefes al", 46, Theme.TextSoft);
            sub.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -120), new Vector2(900, 70));

            // ---- Polaroid (rastgele bir resim, pastel renklerle) ----
            _polaroid = UIKit.Node("Polaroid", root);
            _polaroid.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(600, 700));
            _polaroid.localRotation = Quaternion.Euler(0, 0, -5);
            var card = UIKit.Card(_polaroid, "Card", Theme.Card);
            ((RectTransform)card.transform.parent).Stretch();
            var photoBg = UIKit.Box(card.transform, "PhotoBg", Theme.Paper, null, 2.2f);
            photoBg.rectTransform.Stretch(36, 36, 36, 150);
            var photo = UIKit.Node("Photo", photoBg.transform).Stretch(16, 16, 16, 16);
            var raw = photo.gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            var fitter = photo.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            var caption = UIKit.Label(card.transform, "~ bugünün resmi ~", 44, Theme.TextSoft, TextAnchor.MiddleCenter, FontStyle.Italic);
            caption.rectTransform.Band(false, 120, 18);
            LoadPreview(raw, fitter);
            Tween.Pop(_polaroid, 0.15f, 0.6f);

            // ---- Başla ----
            var start = UIKit.Button(root, "Boyamaya Başla", Theme.Accent, Color.white, 64, () => App.I.ShowGallery());
            _startBtn = (RectTransform)start.transform;
            _startBtn.Place(new Vector2(0.5f, 0f), new Vector2(0, 360), new Vector2(700, 180));
            Tween.Pop(_startBtn, 0.35f);

            // ---- Müzik anahtarı ----
            // Parça adı: dokununca sıradaki lofi havasına geçer
            var music = UIKit.Button(root, "", Theme.Card.WithAlpha(0.85f), Theme.Text, 40, () => { Sfx.NextTrack(); RefreshMusic(); }, 1f, false);
            ((RectTransform)music.transform).Place(new Vector2(0.5f, 0f), new Vector2(-60, 170), new Vector2(480, 110));
            _musicLabel = UIKit.ButtonText(music);
            // Sessiz / sesli
            var mute = UIKit.CircleButton(root, "", Theme.Card.WithAlpha(0.85f), Theme.Text, 110, 40, ToggleMusic);
            ((RectTransform)mute.transform).Place(new Vector2(0.5f, 0f), new Vector2(260, 170), new Vector2(110, 110));
            _muteLabel = UIKit.ButtonText(mute);
            RefreshMusic();
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
            foreach (var g in groups) foreach (var l in g.levels) all.Add((g.name, l.id));
            if (all.Count == 0) return;
            var pick = all[Random.Range(0, all.Count)];

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

        void ToggleMusic()
        {
            SaveSystem.MusicOn = !SaveSystem.MusicOn;
            Sfx.ApplySettings();
            RefreshMusic();
        }

        void RefreshMusic()
        {
            _musicLabel.text = SaveSystem.MusicOn ? "♪  " + Sfx.TrackName + "  ›" : "♪  müzik kapalı";
            _muteLabel.text = SaveSystem.MusicOn ? "ses" : "sessiz";
            _muteLabel.fontSize = SaveSystem.MusicOn ? 34 : 26;
            _muteLabel.color = SaveSystem.MusicOn ? Theme.Text : Theme.TextSoft;
        }

        void Update()
        {
            float t = Time.unscaledTime - _t0;
            if (_polaroid != null && t > 0.8f)
            {
                _polaroid.anchoredPosition = new Vector2(0, 20 + Mathf.Sin(t * 1.1f) * 14f);
                _polaroid.localRotation = Quaternion.Euler(0, 0, -5 + Mathf.Sin(t * 0.7f) * 2f);
            }
            if (_title != null) _title.anchoredPosition = new Vector2(0, 10 + Mathf.Sin(t * 1.6f) * 6f);
            if (_startBtn != null && t > 0.9f)
            {
                float s = 1f + 0.035f * Mathf.Sin(t * 2.4f);
                if (_startBtn.localScale.x > 0.5f) _startBtn.localScale = new Vector3(s, s, 1);
            }
        }

        public bool OnBack()
        {
            Application.Quit();
            return true;
        }

        void OnDestroy()
        {
            if (_preview) Destroy(_preview);
        }
    }
}
