using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>Boyama ekranı: üst çubuk, tuval kartı, palet ve kutlama.</summary>
    public class ColoringScreen : MonoBehaviour, IScreen
    {
        public string Group, Id;

        Level _level;
        ColoringCanvas _canvas;
        Text _progressText;
        RectTransform _progressFill, _finishBtn, _swatchRoot, _paletteTabs;
        Button _undoBtn;
        readonly List<(RectTransform rt, Image ring, Color color)> _swatches = new();
        readonly List<(Image bg, Text label)> _paletteButtons = new();
        int _palette, _selected;
        float _saveTimer = -1;
        bool _completedShown, _wasDone;
        GameObject _overlay;

        void Start()
        {
            var root = (RectTransform)transform;
            _level = Level.Load(Group, Id);
            if (_level == null) { App.I.ShowGallery(Group); return; }
            var save = SaveSystem.Load(_level.Key);
            _wasDone = save != null && save.done;

            BuildTopBar(root);
            BuildCanvasCard(root, save);
            BuildPalette(root);

            _finishBtn = (RectTransform)UIKit.Button(root, "Bitir  ›", Theme.Mint, Theme.Text, 44, ShowComplete).transform;
            _finishBtn.anchorMin = _finishBtn.anchorMax = new Vector2(0.5f, 0);
            _finishBtn.pivot = new Vector2(0.5f, 0);
            _finishBtn.sizeDelta = new Vector2(280, 110);
            _finishBtn.anchoredPosition = new Vector2(0, 570);
            _finishBtn.gameObject.SetActive(false);

            _canvas.Changed += OnChanged;
            OnChanged();
            _saveTimer = -1;
        }

        // ---------------- Kurulum ----------------

        void BuildTopBar(RectTransform root)
        {
            var back = UIKit.CircleButton(root, "‹", Theme.Card, Theme.Text, 116, 90, Exit);
            ((RectTransform)back.transform).Place(new Vector2(0, 1), new Vector2(100, -100), new Vector2(116, 116));

            var hint = UIKit.Button(root, "ipucu", Theme.Butter, Theme.Text, 36, () =>
            {
                if (!_canvas.ShowHint()) Sfx.Tap();
            });
            ((RectTransform)hint.transform).Place(new Vector2(1, 1), new Vector2(-110, -100), new Vector2(170, 104));

            _undoBtn = UIKit.Button(root, "geri al", Theme.Card, Theme.Text, 36, () => _canvas.Undo());
            ((RectTransform)_undoBtn.transform).Place(new Vector2(1, 1), new Vector2(-305, -100), new Vector2(190, 104));

            var title = UIKit.Label(root, Level.PrettyName(Id), 46, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0, 1);
            title.rectTransform.pivot = new Vector2(0, 0.5f);
            title.rectTransform.anchoredPosition = new Vector2(190, -78);
            title.rectTransform.sizeDelta = new Vector2(420, 60);
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 26;
            title.resizeTextMaxSize = 46;

            // İlerleme çubuğu
            var track = UIKit.Box(root, "Track", Theme.Card.WithAlpha(0.8f), null, 3f);
            track.rectTransform.anchorMin = track.rectTransform.anchorMax = new Vector2(0, 1);
            track.rectTransform.pivot = new Vector2(0, 0.5f);
            track.rectTransform.anchoredPosition = new Vector2(190, -132);
            track.rectTransform.sizeDelta = new Vector2(300, 24);
            var fill = UIKit.Box(track.transform, "Fill", Theme.Accent, null, 3f);
            _progressFill = fill.rectTransform;
            _progressFill.anchorMin = Vector2.zero;
            _progressFill.anchorMax = new Vector2(0, 1);
            _progressFill.offsetMin = _progressFill.offsetMax = Vector2.zero;
            _progressText = UIKit.Label(root, "%0", 32, Theme.TextSoft, TextAnchor.MiddleLeft, FontStyle.Bold);
            _progressText.rectTransform.anchorMin = _progressText.rectTransform.anchorMax = new Vector2(0, 1);
            _progressText.rectTransform.pivot = new Vector2(0, 0.5f);
            _progressText.rectTransform.anchoredPosition = new Vector2(505, -132);
            _progressText.rectTransform.sizeDelta = new Vector2(120, 40);
        }

        void BuildCanvasCard(RectTransform root, LevelSave save)
        {
            var card = UIKit.Card(root, "CanvasCard", Theme.Card, 0.7f);
            var cardRt = (RectTransform)card.transform.parent;
            cardRt.Stretch(40, 200, 40, 540);
            Tween.Pop(cardRt, 0.05f, 0.5f);

            var vp = UIKit.Node("Viewport", card.transform).Stretch(16, 16, 16, 16);
            vp.gameObject.AddComponent<RectMask2D>();
            var content = UIKit.Node("Content", vp);
            content.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 100));
            var raw = content.gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;

            _canvas = gameObject.AddComponent<ColoringCanvas>();
            _canvas.Viewport = vp;
            _canvas.Content = content;
            _canvas.Image = raw;
            Canvas.ForceUpdateCanvases();
            _canvas.Init(_level, save);

            // Çift dokunmayla değil, köşedeki küçük butonla görünümü sıfırla
            var reset = UIKit.CircleButton(card.transform, "1x", Theme.Paper, Theme.TextSoft, 84, 34, () => _canvas.ResetView());
            var rrt = (RectTransform)reset.transform;
            rrt.anchorMin = rrt.anchorMax = new Vector2(1, 0);
            rrt.anchoredPosition = new Vector2(-66, 66);

            // Müzik değiştir
            var music = UIKit.CircleButton(card.transform, "♪", Theme.Paper, Theme.TextSoft, 84, 44, () => Popup.Toast("♪  " + Sfx.NextTrack()));
            var mrt = (RectTransform)music.transform;
            mrt.anchorMin = mrt.anchorMax = new Vector2(1, 0);
            mrt.anchoredPosition = new Vector2(-170, 66);

            // Tümünü temizle (onay ister)
            var clear = UIKit.Button(card.transform, "temizle", Theme.Paper, Theme.TextSoft, 32, AskClear);
            var crt = (RectTransform)clear.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(0, 0);
            crt.sizeDelta = new Vector2(170, 84);
            crt.anchoredPosition = new Vector2(115, 66);
        }

        void AskClear()
        {
            if (_canvas.Progress <= 0) { Popup.Toast("zaten tertemiz"); return; }
            Popup.Confirm("Temizlensin mi?", "Bu resimdeki bütün boyalar silinecek.", "temizle", "vazgeç", () =>
            {
                _canvas.ClearAll();
                _completedShown = false;
                _wasDone = false;
                _canvas.ResetView();
                SaveNow();
            });
        }

        void BuildPalette(RectTransform root)
        {
            var panel = UIKit.Card(root, "Palette", Theme.Card.WithAlpha(0.95f), 0.6f);
            var panelRt = (RectTransform)panel.transform.parent;
            panelRt.Band(false, 480, 30, 30);

            // Palet sekmeleri (yatay kaydırılır)
            var tabScrollRt = UIKit.Node("PaletteTabs", panel.transform);
            tabScrollRt.Band(true, 100, 22, 24);
            var sr = tabScrollRt.gameObject.AddComponent<ScrollRect>();
            sr.vertical = false;
            sr.movementType = ScrollRect.MovementType.Elastic;
            var vp = UIKit.Node("Vp", tabScrollRt).Stretch();
            vp.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(30, 0);
            vp.gameObject.AddComponent<Image>().color = Color.clear;
            sr.viewport = vp;
            _paletteTabs = UIKit.Node("Row", vp);
            _paletteTabs.anchorMin = new Vector2(0, 0);
            _paletteTabs.anchorMax = new Vector2(0, 1);
            _paletteTabs.pivot = new Vector2(0, 0.5f);
            _paletteTabs.offsetMin = _paletteTabs.offsetMax = Vector2.zero;
            var h = _paletteTabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 16;
            h.padding = new RectOffset(10, 10, 10, 10);
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            _paletteTabs.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = _paletteTabs;

            for (int i = 0; i < Theme.Palettes.Length; i++)
            {
                int idx = i;
                var bg = UIKit.Box(_paletteTabs, "Tab", Theme.Paper, null, 0.9f);
                bg.raycastTarget = true;
                var le = bg.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 60 + Theme.Palettes[i].Name.Length * 22;
                le.preferredHeight = 80;
                var lbl = UIKit.Label(bg.transform, Theme.Palettes[i].Name, 34, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                lbl.rectTransform.Stretch();
                var b = bg.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { Sfx.Tap(); SelectPalette(idx, true); });
                _paletteButtons.Add((bg, lbl));
            }

            // Renk daireleri: 2 sıra x 6
            _swatchRoot = UIKit.Node("Swatches", panel.transform);
            _swatchRoot.Stretch(30, 130, 30, 24);

            _palette = Mathf.Clamp(SaveSystem.PaletteIndex, 0, Theme.Palettes.Length - 1);
            SelectPalette(_palette, false);
        }

        void SelectPalette(int index, bool animate)
        {
            _palette = index;
            SaveSystem.PaletteIndex = index;
            for (int i = 0; i < _paletteButtons.Count; i++)
            {
                bool on = i == index;
                _paletteButtons[i].bg.color = on ? Theme.Accent : Theme.Paper;
                _paletteButtons[i].label.color = on ? Color.white : Theme.Text;
            }

            foreach (Transform c in _swatchRoot) Destroy(c.gameObject);
            _swatches.Clear();
            var colors = Theme.Palettes[index].Colors;
            const int cols = 6;
            int rows = Mathf.CeilToInt(colors.Length / (float)cols);
            for (int i = 0; i < colors.Length; i++)
            {
                int idx = i;
                int cx = i % cols, cy = i / cols;
                var cell = UIKit.Node("Swatch", _swatchRoot);
                cell.anchorMin = new Vector2(cx / (float)cols, 1f - (cy + 1f) / rows);
                cell.anchorMax = new Vector2((cx + 1f) / cols, 1f - cy / (float)rows);
                cell.offsetMin = cell.offsetMax = Vector2.zero;

                var holder = UIKit.Node("Holder", cell);
                holder.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120, 120));
                var shadow = UIKit.Box(holder, "Shadow", Theme.Shadow, UIKit.Blob);
                shadow.type = Image.Type.Simple;
                shadow.rectTransform.Stretch(-12, -2, -12, -18);
                var dot = UIKit.Box(holder, "Dot", colors[i], UIKit.Circle);
                dot.type = Image.Type.Simple;
                dot.rectTransform.Stretch();
                dot.raycastTarget = true;
                // Beyaz renk de görünsün diye ince kenar
                var edge = UIKit.Box(holder, "Edge", Theme.Text.WithAlpha(0.08f), UIKit.Ring);
                edge.type = Image.Type.Simple;
                edge.rectTransform.Stretch();
                var ring = UIKit.Box(holder, "Ring", Theme.Text, UIKit.Ring);
                ring.type = Image.Type.Simple;
                ring.rectTransform.Stretch(-16, -16, -16, -16);
                ring.enabled = false;

                var b = dot.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => SelectSwatch(idx, true));
                _swatches.Add((holder, ring, colors[i]));
                if (animate) Tween.Pop(holder, i * 0.025f, 0.35f);
            }
            SelectSwatch(Mathf.Clamp(_selected, 0, colors.Length - 1), false);
        }

        void SelectSwatch(int index, bool feedback)
        {
            _selected = index;
            for (int i = 0; i < _swatches.Count; i++)
            {
                var (rt, ring, _) = _swatches[i];
                bool on = i == index;
                ring.enabled = on;
                rt.localScale = Vector3.one * (on ? 1.12f : 1f);
            }
            _canvas.CurrentColor = _swatches[index].color;
            if (feedback)
            {
                Sfx.Tap();
                var rt = _swatches[index].rt;
                Tween.To(rt, 0.35f, k => rt.localScale = Vector3.one * (1.12f + 0.12f * Mathf.Sin(k * Mathf.PI)), Tween.Linear);
            }
        }

        // ---------------- Akış ----------------

        void OnChanged()
        {
            float p = _canvas.Progress;
            int pct = Mathf.FloorToInt(p * 100);
            if (p > 0 && pct == 0) pct = 1;
            if (_canvas.AllFilled) pct = 100;
            _progressText.text = "%" + pct;
            float from = _progressFill.anchorMax.x;
            Tween.To(_progressFill, 0.35f, k => _progressFill.anchorMax = new Vector2(Mathf.Lerp(from, p, k), 1));
            SetUndoVisible(_canvas.CanUndo);

            bool showFinish = p >= 0.9f && !_canvas.AllFilled && !_completedShown;
            if (showFinish != _finishBtn.gameObject.activeSelf)
            {
                _finishBtn.gameObject.SetActive(showFinish);
                if (showFinish) Tween.Pop(_finishBtn);
            }
            _saveTimer = 1.2f;

            if (_canvas.AllFilled && !_completedShown && !_wasDone)
                Tween.Delay(0.55f, () => { if (this != null && _canvas.AllFilled) ShowComplete(); });
        }

        void SetUndoVisible(bool on)
        {
            if (!_undoBtn.TryGetComponent<CanvasGroup>(out var cg)) cg = _undoBtn.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = on ? 1f : 0.45f;
            cg.interactable = on;
        }

        void Update()
        {
            if (_saveTimer > 0)
            {
                _saveTimer -= Time.unscaledDeltaTime;
                if (_saveTimer <= 0) SaveNow();
            }
        }

        void SaveNow()
        {
            if (_canvas == null || _canvas.Level == null) return;
            var s = _canvas.ToSave();
            if (s.progress <= 0 && !s.done)
            {
                SaveSystem.Delete(_level.Key);
                return;
            }
            var thumb = _canvas.MakeThumbnail();
            SaveSystem.Save(_level.Key, s, thumb);
            Destroy(thumb);
            _saveTimer = -1;
        }

        void OnApplicationPause(bool paused) { if (paused) SaveNow(); }

        void Exit()
        {
            SaveNow();
            App.I.ShowGallery(Group);
        }

        // ---------------- Kutlama ----------------

        void ShowComplete()
        {
            if (_completedShown) return;
            _completedShown = true;
            _wasDone = true;
            _finishBtn.gameObject.SetActive(false);
            SaveNow();
            Sfx.Win();

            var root = (RectTransform)transform;
            _overlay = UIKit.Node("Complete", root).Stretch(-200, -200, -200, -200).gameObject;
            var dim = _overlay.AddComponent<Image>();
            dim.color = Theme.Text.WithAlpha(0f);
            Tween.To(dim, 0.4f, k => dim.color = Theme.Text.WithAlpha(0.35f * k));

            var cardHolder = UIKit.Node("CardHolder", _overlay.transform);
            cardHolder.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(860, 1280));
            var card = UIKit.Card(cardHolder, "Card", Theme.Card, 0.6f);
            ((RectTransform)card.transform.parent).Stretch();
            Tween.Pop(cardHolder, 0.1f, 0.6f);

            var t = UIKit.Label(card.transform, "Harika!", 110, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Band(true, 140, 50);
            var sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = Theme.Accent.WithAlpha(0.6f);
            sh.effectDistance = new Vector2(0, -6);
            var s = UIKit.Label(card.transform, "resmini tamamladın, çok güzel oldu", 40, Theme.TextSoft);
            s.rectTransform.Band(true, 60, 190);

            var photoBg = UIKit.Box(card.transform, "PhotoBg", Theme.Paper, null, 1.2f);
            photoBg.rectTransform.Stretch(60, 280, 60, 260);
            var photo = UIKit.Node("Photo", photoBg.transform).Stretch(20, 20, 20, 20);
            var raw = photo.gameObject.AddComponent<RawImage>();
            raw.texture = _level.Lines;
            raw.material = _canvas.Material;
            raw.raycastTarget = false;
            var fit = photo.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = (float)_level.Width / _level.Height;

            var gallery = UIKit.Button(card.transform, "Galeri", Theme.Card, Theme.Text, 48, Exit);
            var grt = (RectTransform)gallery.transform;
            grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0);
            grt.sizeDelta = new Vector2(330, 140);
            grt.anchoredPosition = new Vector2(-190, 120);
            var edge = UIKit.Box(gallery.transform.Find("Face"), "Edge", Theme.Lavender, null, 1f);
            edge.rectTransform.Stretch(-4, -4, -4, -4);
            edge.transform.SetAsFirstSibling();

            var next = UIKit.Button(card.transform, "Sonraki  ›", Theme.Accent, Color.white, 48, NextLevel);
            var nrt = (RectTransform)next.transform;
            nrt.anchorMin = nrt.anchorMax = new Vector2(0.5f, 0);
            nrt.sizeDelta = new Vector2(330, 140);
            nrt.anchoredPosition = new Vector2(190, 120);

            Confetti.Burst(_overlay.transform, 90);
        }

        void NextLevel()
        {
            var levels = Level.GetGroup(Group).levels;
            int i = levels.FindIndex(l => l.id == Id);
            // Sıradaki bitmemiş resmi bul
            for (int k = 1; k <= levels.Count; k++)
            {
                var cand = levels[(i + k) % levels.Count];
                var s = SaveSystem.Load(Group + "_" + cand.id);
                if (s == null || !s.done) { App.I.ShowColoring(Group, cand.id); return; }
            }
            App.I.ShowGallery(Group);
        }

        public bool OnBack()
        {
            if (_overlay != null) { Exit(); return true; }
            Exit();
            return true;
        }

        void OnDestroy()
        {
            if (_canvas != null && _canvas.Level != null && _saveTimer > 0) SaveNow();
            _level?.Unload();
        }
    }
}
