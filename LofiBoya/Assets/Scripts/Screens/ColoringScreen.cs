using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>Boyama ekranı: üst çubuk, tuval kartı, araçlar, boya kalemi kutusu ve kutlama.</summary>
    public class ColoringScreen : MonoBehaviour, IScreen
    {
        public string Group, Id;

        Level _level;
        ColoringCanvas _canvas;
        Text _progressText;
        RectTransform _progressFill, _finishBtn, _crayonRow, _paletteTabs, _sizePicker;
        Text _boxLabel;
        Button _undoBtn;
        readonly List<Crayon> _crayons = new();
        readonly List<(Image bg, Text label)> _paletteButtons = new();
        readonly List<(Image bg, Image icon)> _toolButtons = new();
        readonly List<(RectTransform rt, Image dot, Image ring)> _sizeDots = new();
        int _palette, _selected;
        float _saveTimer = -1;
        bool _completedShown, _wasDone;
        GameObject _overlay;

        class Crayon
        {
            public RectTransform Rt;
            public Color Color;
            public float Lift, LiftVel, Target, Wobble;
            public bool Hold; // kutunun içinde bekle (sıralı çıkış animasyonu)
        }

        const float CrayonW = 66f, CrayonH = 264f, CrayonRest = 40f, CrayonUp = 112f;

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
            _finishBtn.anchoredPosition = new Vector2(0, 600);
            _finishBtn.gameObject.SetActive(false);

            SelectTool((Tool)Mathf.Clamp(SaveSystem.ToolIndex, 0, 2), false);
            _canvas.Changed += OnChanged;
            OnChanged();
            _saveTimer = -1;
        }

        // ================= Kurulum =================

        void BuildTopBar(RectTransform root)
        {
            var back = UIKit.CircleButton(root, "‹", Theme.Card, Theme.Text, 116, 90, Exit);
            ((RectTransform)back.transform).Place(new Vector2(0, 1), new Vector2(100, -100), new Vector2(116, 116));

            var hint = UIKit.Button(root, "ipucu", Theme.Butter, Theme.Text, 36, () =>
            {
                if (!_canvas.ShowHint()) Popup.Toast("her yer boyanmış");
            });
            ((RectTransform)hint.transform).Place(new Vector2(1, 1), new Vector2(-110, -100), new Vector2(170, 104));

            _undoBtn = UIKit.Button(root, "geri al", Theme.Card, Theme.Text, 36, () => _canvas.Undo());
            ((RectTransform)_undoBtn.transform).Place(new Vector2(1, 1), new Vector2(-305, -100), new Vector2(190, 104));

            var title = UIKit.Label(root, Level.PrettyName(Id), 46, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0, 1);
            title.rectTransform.pivot = new Vector2(0, 0.5f);
            title.rectTransform.anchoredPosition = new Vector2(190, -78);
            title.rectTransform.sizeDelta = new Vector2(400, 60);
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
            cardRt.Stretch(40, 200, 40, 590);
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
            var paint = save != null && save.hasPaint ? SaveSystem.LoadPaint(_level.Key) : null;
            _canvas.Init(_level, save, paint);
            if (paint != null) Destroy(paint);

            // Sağ alt: görünümü sıfırla + müzik
            var reset = UIKit.CircleButton(card.transform, "1x", Theme.Paper, Theme.TextSoft, 84, 34, () => _canvas.ResetView());
            var rrt = (RectTransform)reset.transform;
            rrt.anchorMin = rrt.anchorMax = new Vector2(1, 0);
            rrt.anchoredPosition = new Vector2(-66, 66);

            var music = UIKit.IconButton(card.transform, Art.Note, Theme.Paper, Theme.TextSoft, 84, () => Popup.Toast(Sfx.NextTrack()), 0.5f);
            var mrt = (RectTransform)music.transform;
            mrt.anchorMin = mrt.anchorMax = new Vector2(1, 0);
            mrt.anchoredPosition = new Vector2(-170, 66);

            // Sol alt: tümünü temizle (onay ister)
            var clear = UIKit.Button(card.transform, "temizle", Theme.Paper, Theme.TextSoft, 32, AskClear);
            var crt = (RectTransform)clear.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(0, 0);
            crt.sizeDelta = new Vector2(170, 84);
            crt.anchoredPosition = new Vector2(115, 66);

            // Sol kenar: kalem kalınlığı (kalem/silgi seçiliyken görünür)
            _sizePicker = UIKit.Node("SizePicker", card.transform);
            _sizePicker.anchorMin = _sizePicker.anchorMax = new Vector2(0, 0.5f);
            _sizePicker.pivot = new Vector2(0, 0.5f);
            _sizePicker.anchoredPosition = new Vector2(26, 30);
            _sizePicker.sizeDelta = new Vector2(96, 300);
            var pill = UIKit.Card(_sizePicker, "Pill", Theme.Paper.WithAlpha(0.97f), 0.5f, 3f);
            ((RectTransform)pill.transform.parent).Stretch();
            float[] dotSizes = { 18, 32, 50 };
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var cell = UIKit.Node("Size", pill.transform);
                cell.anchorMin = new Vector2(0, 1f - (i + 1) / 3f);
                cell.anchorMax = new Vector2(1, 1f - i / 3f);
                cell.offsetMin = cell.offsetMax = Vector2.zero;
                var hit = cell.gameObject.AddComponent<Image>();
                hit.color = Color.clear;
                var ring = UIKit.Box(cell, "Ring", Theme.Accent, UIKit.Ring);
                ring.type = Image.Type.Simple;
                ring.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(78, 78));
                var dot = UIKit.Box(cell, "Dot", Theme.TextSoft, UIKit.Circle);
                dot.type = Image.Type.Simple;
                dot.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(dotSizes[i], dotSizes[i]));
                var b = cell.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { Sfx.Tap(); SelectSize(idx); });
                _sizeDots.Add((cell, dot, ring));
            }
            SelectSize(Mathf.Clamp(SaveSystem.BrushSize, 0, 2));
        }

        void BuildPalette(RectTransform root)
        {
            var panel = UIKit.Card(root, "Palette", Theme.Card.WithAlpha(0.96f), 0.6f);
            var panelRt = (RectTransform)panel.transform.parent;
            panelRt.Band(false, 540, 26, 26);

            // ---- Araçlar: kova / kalem / silgi ----
            var tools = UIKit.Box(panel.transform, "Tools", Theme.Paper, null, 0.5f);
            var trt = tools.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(0, 1);
            trt.pivot = new Vector2(0, 1);
            trt.anchoredPosition = new Vector2(22, -20);
            trt.sizeDelta = new Vector2(312, 100);
            Sprite[] icons = { Art.Bucket, Art.Pencil, Art.Eraser };
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var cell = UIKit.Node("Tool", tools.transform);
                cell.anchorMin = new Vector2(i / 3f, 0);
                cell.anchorMax = new Vector2((i + 1) / 3f, 1);
                cell.offsetMin = new Vector2(6, 6);
                cell.offsetMax = new Vector2(-6, -6);
                var bg = cell.gameObject.AddComponent<Image>();
                bg.sprite = UIKit.Rounded;
                bg.type = Image.Type.Sliced;
                bg.pixelsPerUnitMultiplier = 0.55f;
                var icon = UIKit.Box(cell, "Icon", Theme.Text, icons[i]);
                icon.type = Image.Type.Simple;
                icon.preserveAspect = true;
                icon.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(58, 58));
                var b = cell.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { Sfx.Tap(); SelectTool((Tool)idx, true); });
                cell.gameObject.AddComponent<PressFeedback>();
                _toolButtons.Add((bg, icon));
            }

            // ---- Palet sekmeleri (yatay kaydırılır) ----
            var tabScrollRt = UIKit.Node("PaletteTabs", panel.transform);
            tabScrollRt.anchorMin = new Vector2(0, 1);
            tabScrollRt.anchorMax = new Vector2(1, 1);
            tabScrollRt.pivot = new Vector2(0.5f, 1);
            tabScrollRt.offsetMin = new Vector2(350, -120);
            tabScrollRt.offsetMax = new Vector2(-16, -20);
            var sr = tabScrollRt.gameObject.AddComponent<ScrollRect>();
            sr.vertical = false;
            sr.movementType = ScrollRect.MovementType.Elastic;
            var vp = UIKit.Node("Vp", tabScrollRt).Stretch();
            vp.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(24, 0);
            vp.gameObject.AddComponent<Image>().color = Color.clear;
            sr.viewport = vp;
            _paletteTabs = UIKit.Node("Row", vp);
            _paletteTabs.anchorMin = new Vector2(0, 0);
            _paletteTabs.anchorMax = new Vector2(0, 1);
            _paletteTabs.pivot = new Vector2(0, 0.5f);
            _paletteTabs.offsetMin = _paletteTabs.offsetMax = Vector2.zero;
            var h = _paletteTabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12;
            h.padding = new RectOffset(6, 6, 12, 12);
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
                le.preferredWidth = 50 + Theme.Palettes[i].Name.Length * 21;
                le.preferredHeight = 76;
                var lbl = UIKit.Label(bg.transform, Theme.Palettes[i].Name, 32, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                lbl.rectTransform.Stretch();
                var b = bg.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { Sfx.Tap(); SelectPalette(idx, true); });
                _paletteButtons.Add((bg, lbl));
            }

            // ---- Boya kalemi kutusu ----
            var area = UIKit.Node("CrayonBox", panel.transform).Stretch(16, 132, 16, 0);
            area.gameObject.AddComponent<RectMask2D>();
            _crayonRow = UIKit.Node("Crayons", area).Stretch(10, 0, 10, 0);

            // Kutunun ön yüzü (kalemlerin alt kısmını örter)
            var front = UIKit.Node("BoxFront", area);
            front.anchorMin = new Vector2(0, 0);
            front.anchorMax = new Vector2(1, 0);
            front.pivot = new Vector2(0.5f, 0);
            front.offsetMin = new Vector2(6, 16);
            front.offsetMax = new Vector2(-6, 136);
            var lip = UIKit.Box(front, "LipShadow", Theme.Shadow, UIKit.Shadow, 1.2f);
            lip.rectTransform.Stretch(-10, -24, -10, 20);
            var box = UIKit.Box(front, "Front", Theme.Lavender, null, 0.7f);
            box.rectTransform.Stretch();
            var opening = UIKit.Box(box.transform, "Opening", Theme.Text.WithAlpha(0.10f), null, 2f);
            opening.rectTransform.Band(true, 14, 0, 14);
            // dikiş çizgisi
            var stitch = UIKit.Node("Stitch", box.transform);
            stitch.Band(true, 6, 30, 40);
            for (int i = 0; i < 26; i++)
            {
                var d = UIKit.Box(stitch, "s", Color.white.WithAlpha(0.65f), null, 6f);
                d.rectTransform.anchorMin = new Vector2(i / 26f + 0.006f, 0);
                d.rectTransform.anchorMax = new Vector2((i + 1) / 26f - 0.012f, 1);
                d.rectTransform.offsetMin = d.rectTransform.offsetMax = Vector2.zero;
            }
            _boxLabel = UIKit.Label(box.transform, "", 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            _boxLabel.rectTransform.Stretch(0, 40, 0, 6);
            var lblShadow = _boxLabel.gameObject.AddComponent<Shadow>();
            lblShadow.effectColor = Theme.Text.WithAlpha(0.25f);
            lblShadow.effectDistance = new Vector2(0, -3);

            _palette = Mathf.Clamp(SaveSystem.PaletteIndex, 0, Theme.Palettes.Length - 1);
            SelectPalette(_palette, false);
        }

        // ================= Seçimler =================

        void SelectTool(Tool tool, bool feedback)
        {
            _canvas.Tool = tool;
            SaveSystem.ToolIndex = (int)tool;
            for (int i = 0; i < _toolButtons.Count; i++)
            {
                bool on = i == (int)tool;
                _toolButtons[i].bg.color = on ? Theme.Accent : Color.clear;
                _toolButtons[i].icon.color = on ? Color.white : Theme.Text;
            }

            bool showSizes = tool != Tool.Fill;
            if (showSizes != _sizePicker.gameObject.activeSelf || !feedback)
            {
                _sizePicker.gameObject.SetActive(showSizes);
                if (showSizes && feedback) Tween.Pop(_sizePicker, 0f, 0.35f);
            }
            UpdateSizeDots();

            if (feedback)
            {
                Tween.Punch(_toolButtons[(int)tool].bg.transform, 0.1f);
                string key = "tip_" + tool;
                if (tool != Tool.Fill && PlayerPrefs.GetInt(key, 0) == 0)
                {
                    PlayerPrefs.SetInt(key, 1);
                    Popup.Toast(tool == Tool.Pencil ? "parmağınla çiz · iki parmakla kaydır" : "sil · dokununca bölgeyi boşaltır");
                }
            }
        }

        void SelectSize(int index)
        {
            _canvas.BrushSize = index;
            SaveSystem.BrushSize = index;
            UpdateSizeDots();
            if (_sizeDots.Count > index) Tween.Punch(_sizeDots[index].rt, 0.15f);
        }

        void UpdateSizeDots()
        {
            for (int i = 0; i < _sizeDots.Count; i++)
            {
                bool on = i == _canvas.BrushSize;
                var col = _canvas.Tool == Tool.Eraser ? Theme.TextSoft : _canvas.CurrentColor;
                // açık renkler beyaz kartta kaybolmasın
                if (on && col.grayscale > 0.92f) col = Color.Lerp(col, Theme.Text, 0.25f);
                _sizeDots[i].dot.color = on ? col : Theme.TextSoft.WithAlpha(0.6f);
                _sizeDots[i].ring.enabled = on;
            }
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
            _boxLabel.text = Theme.Palettes[index].Name;

            foreach (Transform c in _crayonRow) Destroy(c.gameObject);
            _crayons.Clear();
            var colors = Theme.Palettes[index].Colors;
            int n = colors.Length;
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                var slot = UIKit.Node("Slot", _crayonRow);
                slot.anchorMin = new Vector2(i / (float)n, 0);
                slot.anchorMax = new Vector2((i + 1) / (float)n, 1);
                slot.offsetMin = slot.offsetMax = Vector2.zero;

                var holder = UIKit.Node("Crayon", slot);
                holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 0);
                holder.pivot = new Vector2(0.5f, 0);
                holder.sizeDelta = new Vector2(CrayonW, CrayonH);
                holder.anchoredPosition = new Vector2(0, animate ? -CrayonH : CrayonRest);

                var shadow = UIKit.Box(holder, "Shadow", Theme.Shadow, UIKit.Shadow, 2f);
                shadow.rectTransform.Stretch(-6, 4, -14, -6);
                var body = UIKit.Box(holder, "Body", colors[i], Art.CrayonBody);
                body.type = Image.Type.Simple;
                body.rectTransform.Stretch();
                body.raycastTarget = true;
                var shine = UIKit.Box(holder, "Shine", Color.white, Art.CrayonShine);
                shine.type = Image.Type.Simple;
                shine.rectTransform.Stretch();
                // beyaz kalem kâğıtta kaybolmasın diye ince kontur
                if (colors[i].grayscale > 0.9f) body.color = Color.Lerp(colors[i], Theme.Text, 0.06f);

                // dokunma alanı kalemden geniş olsun
                var hit = slot.gameObject.AddComponent<Image>();
                hit.color = Color.clear;
                var b = slot.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => SelectCrayon(idx, true));

                var cr = new Crayon { Rt = holder, Color = colors[i], Lift = animate ? -CrayonH : CrayonRest, Target = CrayonRest, Hold = animate };
                _crayons.Add(cr);
                if (animate)
                {
                    // kutudan soldan sağa sırayla fırlasınlar
                    var c2 = cr;
                    Tween.Delay(0.035f * i, () => { c2.Hold = false; c2.LiftVel = 900f; });
                }
            }
            SelectCrayon(Mathf.Clamp(_selected, 0, n - 1), false);
        }

        void SelectCrayon(int index, bool feedback)
        {
            _selected = index;
            for (int i = 0; i < _crayons.Count; i++) _crayons[i].Target = i == index ? CrayonUp : CrayonRest;
            _canvas.CurrentColor = _crayons[index].Color;
            UpdateSizeDots();
            if (feedback)
            {
                Sfx.Tap();
                _crayons[index].LiftVel += 500f;
                _crayons[index].Wobble = 1f;
                if (_canvas.Tool == Tool.Eraser) SelectTool(Tool.Pencil, true);
            }
        }

        void AnimateCrayons(float dt)
        {
            foreach (var c in _crayons)
            {
                // yay (spring): yumuşak zıplama
                float target = c.Hold ? -CrayonH : c.Target;
                float force = (target - c.Lift) * 180f - c.LiftVel * 16f;
                c.LiftVel += force * dt;
                c.Lift += c.LiftVel * dt;
                c.Wobble = Mathf.MoveTowards(c.Wobble, 0f, dt * 1.6f);
                if (c.Rt == null) continue;
                c.Rt.anchoredPosition = new Vector2(0, c.Lift);
                c.Rt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 22f) * 7f * c.Wobble * c.Wobble);
            }
        }

        // ================= Akış =================

        void AskClear()
        {
            if (_canvas.Progress <= 0 && !_canvas.HasAnyBrush) { Popup.Toast("zaten tertemiz"); return; }
            Popup.Confirm("Temizlensin mi?", "Bu resimdeki bütün boyalar silinecek.", "temizle", "vazgeç", () =>
            {
                _canvas.ClearAll();
                _completedShown = false;
                _wasDone = false;
                _canvas.ResetView();
                SaveNow();
            });
        }

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
            AnimateCrayons(Mathf.Min(Time.unscaledDeltaTime, 1f / 30f));
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
            if (s.progress <= 0 && !s.done && !s.hasPaint)
            {
                SaveSystem.Delete(_level.Key);
                _saveTimer = -1;
                return;
            }
            var paint = s.hasPaint ? _canvas.Paint.ReadBack() : null;
            var thumb = _canvas.MakeThumbnail(paint);
            SaveSystem.Save(_level.Key, s, thumb, paint);
            Destroy(thumb);
            if (paint != null) Destroy(paint);
            _saveTimer = -1;
        }

        void OnApplicationPause(bool paused) { if (paused) SaveNow(); }

        void Exit()
        {
            SaveNow();
            App.I.ShowGallery(Group);
        }

        // ================= Kutlama =================

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

            var gallery = UIKit.Button(card.transform, "Galeri", Theme.Lavender, Theme.Text, 48, Exit);
            var grt = (RectTransform)gallery.transform;
            grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0);
            grt.sizeDelta = new Vector2(330, 140);
            grt.anchoredPosition = new Vector2(-190, 120);

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
