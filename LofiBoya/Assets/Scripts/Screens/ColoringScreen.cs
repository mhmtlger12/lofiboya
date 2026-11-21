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
        Text _boxLabel, _numBtnText;
        Image _numBtnBg;
        RectTransform _paletteScroll;
        Image _cardFace;
        RectTransform _fitBtn;
        Coroutine _cardTween;
        Button _undoBtn;
        readonly List<Crayon> _crayons = new();
        readonly List<(Image bg, Text label)> _paletteButtons = new();
        readonly List<(Image bg, Image icon)> _toolButtons = new();
        readonly List<(RectTransform rt, Image dot, Image ring)> _sizeDots = new();
        int _palette, _selected;
        float _saveTimer = -1;
        bool _completedShown, _wasDone;
        GameObject _overlay;
        HandTool _hand;
        bool _ready;
        RectTransform _magicPicker, _stampPicker;
        readonly List<(Image img, Image ring)> _magicItems = new();
        readonly List<(Image img, Image ring)> _stampItems = new();
        readonly List<Sprite> _previewSprites = new();
        // Boyama arkadaşı Pofuduk
        Buddy _buddy;
        int _milestone;                 // 0..3: %25 / %50 / %75 söylendi mi
        float _lastActivity, _lastIdlePeek = -999f;

        // Sayılı mod (1-5): her bölgenin bir numarası, her numaranın bir rengi var
        bool _numbers;
        int[] _target;                                   // bölge -> renk sırası (-1 = serbest)
        (string name, string acc, Color color)[] _numColors;
        Text[] _numLabels;
        RectTransform _numLayer;
        const int NumberCount = 5;

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
            // çizerken gecikme az olsun: ekran 90/120 Hz ise o hızda çalış (çıkınca 60'a döner)
            int hz = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
            Application.targetFrameRate = Mathf.Clamp(hz, 60, 120);
            _level = Level.Load(Group, Id);
            if (_level == null) { App.I.ShowGallery(Group); return; }
            var save = SaveSystem.Load(_level.Key);
            _wasDone = save != null && save.done;

            BuildTopBar(root);
            if (_level.IsBlank)
            {
                // serbest çizimde yüzde anlamsız: çubuğu gizle
                _progressFill.parent.gameObject.SetActive(false);
                _progressText.gameObject.SetActive(false);
            }
            BuildCanvasCard(root, save);
            BuildPalette(root);

            _finishBtn = (RectTransform)UIKit.Button(root, Loc.T("finish"), Theme.Mint, Theme.Text, 44, ShowComplete).transform;
            _finishBtn.anchorMin = _finishBtn.anchorMax = new Vector2(0.5f, 0);
            _finishBtn.pivot = new Vector2(0.5f, 0);
            _finishBtn.sizeDelta = new Vector2(280, 110);
            _finishBtn.anchoredPosition = new Vector2(0, 600);
            _finishBtn.gameObject.SetActive(false);

            // Elde kalem (kalem/silgi) ve dokununca dökülen kova
            _hand = HandTool.Create(root, _canvas);
            _canvas.Tapped += sp => { if (_canvas.Tool == Tool.Fill) _hand.Pour(sp, _canvas.CurrentColor); };
            _canvas.CanFill = CheckNumber;
            _canvas.Stamped += StampPop;

            var startTool = (Tool)Mathf.Clamp(SaveSystem.ToolIndex, 0, 3);
            if (_level.IsBlank && startTool == Tool.Fill) startTool = Tool.Pencil; // boş sayfada kalemle başla
            SelectTool(startTool, false);
            if (SaveSystem.NumberMode && !_level.IsBlank) SetNumbers(true, false);
            _buddy = Buddy.Create(root, 610);
            var hat = Buddy.HatFor(Id);
            if (hat != MascotHat.None)
            {
                _buddy.SetHat(hat);
                Tween.Delay(1.3f, () => { if (this != null && _buddy != null) _buddy.Peek(null, 3f); });
            }
            _lastActivity = Time.unscaledTime;
            _canvas.Changed += OnChanged;
            OnChanged();
            float p0 = _canvas.Progress;
            _milestone = p0 >= 0.75f ? 3 : p0 >= 0.5f ? 2 : p0 >= 0.25f ? 1 : 0;
            _saveTimer = -1;
            _ready = true;

            // Yeni boş sayfa: "Yeni bir sayfa! İstediğini çiz."
            if (_level.IsBlank && save == null)
                Tween.Delay(0.7f, () => { if (this != null) Speech.Say(Loc.T("new_drawing_say")); });

            // Resmi tanıt: "Bu bir aslan!"
            if (Names.TryAnimal(Id, out var animal, out _))
                Tween.Delay(0.7f, () => { if (this != null) Speech.Say(Loc.T(Id.StartsWith("lofi-num-") ? "intro_number" : Names.IsScene(Id) ? "intro_scene" : "intro", Names.Cap(animal), animal), "bu_" + Speech.Slug(animal)); });
        }

        // ================= Kurulum =================

        void BuildTopBar(RectTransform root)
        {
            var back = UIKit.CircleButton(root, "‹", Theme.Card, Theme.Text, 116, 90, Exit);
            ((RectTransform)back.transform).Place(new Vector2(0, 1), new Vector2(100, -100), new Vector2(116, 116));

            // Sağdan sola: ipucu, geri al, temizle, müzik — hepsi çizilmiş ikonlu yuvarlak butonlar
            const float size = 100f, gap = 112f;
            var hint = UIKit.IconButton(root, Art.Bulb, Theme.Butter, Theme.Text, size, () =>
            {
                if (!_canvas.ShowHint()) Popup.Toast(Loc.T("all_painted"));
            }, 0.56f);
            ((RectTransform)hint.transform).Place(new Vector2(1, 1), new Vector2(-90, -100), new Vector2(size, size));

            _undoBtn = UIKit.IconButton(root, Art.UndoIcon, Theme.Card, Theme.Text, size, () => _canvas.Undo(), 0.5f);
            ((RectTransform)_undoBtn.transform).Place(new Vector2(1, 1), new Vector2(-90 - gap, -100), new Vector2(size, size));

            var clear = UIKit.IconButton(root, Art.Trash, Theme.Card, Theme.Text, size, AskClear, 0.5f);
            ((RectTransform)clear.transform).Place(new Vector2(1, 1), new Vector2(-90 - gap * 2, -100), new Vector2(size, size));

            var music = UIKit.IconButton(root, Art.Note, Theme.Card, Theme.Text, size, () => Popup.Toast(Sfx.NextTrack()), 0.48f);
            ((RectTransform)music.transform).Place(new Vector2(1, 1), new Vector2(-90 - gap * 3, -100), new Vector2(size, size));

            var title = UIKit.Label(root, _level.IsBlank ? Loc.T("blank_page") : Names.LevelTitle(Id), 46, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0, 1);
            title.rectTransform.pivot = new Vector2(0, 0.5f);
            title.rectTransform.anchoredPosition = new Vector2(180, -78);
            title.rectTransform.sizeDelta = new Vector2(330, 60);
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 26;
            title.resizeTextMaxSize = 46;
            // Ada dokununca tekrar söyler: çocuk kelimeyi istediği kadar dinleyebilir
            if (!_level.IsBlank && Names.TryAnimal(Id, out var titleName, out _))
            {
                title.raycastTarget = true;
                var tb = title.gameObject.AddComponent<Button>();
                tb.transition = Selectable.Transition.None;
                tb.onClick.AddListener(() =>
                {
                    Tween.Punch(title.transform, 0.15f, 0.3f);
                    Speech.Say(Names.Cap(titleName), Speech.Slug(titleName));
                });
            }

            // İlerleme çubuğu
            var track = UIKit.Box(root, "Track", Theme.Card.WithAlpha(0.8f), null, 3f);
            track.rectTransform.anchorMin = track.rectTransform.anchorMax = new Vector2(0, 1);
            track.rectTransform.pivot = new Vector2(0, 0.5f);
            track.rectTransform.anchoredPosition = new Vector2(180, -132);
            track.rectTransform.sizeDelta = new Vector2(230, 22);
            var fill = UIKit.Box(track.transform, "Fill", Theme.Accent, null, 3f);
            _progressFill = fill.rectTransform;
            _progressFill.anchorMin = Vector2.zero;
            _progressFill.anchorMax = new Vector2(0, 1);
            _progressFill.offsetMin = _progressFill.offsetMax = Vector2.zero;
            _progressText = UIKit.Label(root, Loc.T("percent", 0), 32, Theme.TextSoft, TextAnchor.MiddleLeft, FontStyle.Bold);
            _progressText.rectTransform.anchorMin = _progressText.rectTransform.anchorMax = new Vector2(0, 1);
            _progressText.rectTransform.pivot = new Vector2(0, 0.5f);
            _progressText.rectTransform.anchoredPosition = new Vector2(424, -132);
            _progressText.rectTransform.sizeDelta = new Vector2(120, 40);
        }

        void BuildCanvasCard(RectTransform root, LevelSave save)
        {
            // Kart = kâğıt. Rengi resmin arka plan bölgesiyle aynı olur, böylece resim kenara kadar uzanır.
            var card = UIKit.Card(root, "CanvasCard", Theme.Paper, 0.7f);
            _cardFace = card;
            var cardRt = (RectTransform)card.transform.parent;
            cardRt.Stretch(36, 190, 36, 590);
            Tween.Pop(cardRt, 0.05f, 0.5f);

            // Yuvarlak köşeli maske: yakınlaşınca resim kartın köşelerinden taşmaz
            var vp = UIKit.Node("Viewport", card.transform).Stretch();
            var maskImg = vp.gameObject.AddComponent<Image>();
            maskImg.sprite = UIKit.Rounded;
            maskImg.type = Image.Type.Sliced;
            maskImg.pixelsPerUnitMultiplier = 0.7f;
            maskImg.raycastTarget = false;
            vp.gameObject.AddComponent<Mask>().showMaskGraphic = false;
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
            // Resmi kartın oranına genişlet: damga/kalem kartın kenarına kadar gider, resim kenarında kesilmez.
            // Kayıtlı boya varsa onun oranı kullanılır (eski kayıtlar bozulmasın).
            var vr = vp.rect.size;
            float aspect = vr.y > 1f ? vr.x / vr.y : 0f;
            if (paint != null)
            {
                // Yeni kayıt: boya zaten genişletilmiş oranda -> aynısını kullan.
                // Eski kayıt: boya resmin kendi oranında -> karta genişlet, boyayı ortadaki yerine koy.
                float pa = (float)paint.width / paint.height, la = (float)_level.Width / _level.Height;
                if (Mathf.Abs(pa - la) > 0.01f * la) aspect = pa;
            }
            _level.PadToAspect(aspect);
            _canvas.Init(_level, save, paint);
            if (paint != null) Destroy(paint);

            _cardFace.color = _canvas.BorderColor;

            // Sağ alt: yalnızca yakınlaştırınca çıkan "resmi sığdır" butonu
            var fitBtn = UIKit.IconButton(card.transform, Art.Fit, Theme.Card.WithAlpha(0.95f), Theme.Text, 92, () => _canvas.ResetView(), 0.5f);
            _fitBtn = (RectTransform)fitBtn.transform;
            _fitBtn.anchorMin = _fitBtn.anchorMax = new Vector2(1, 0);
            _fitBtn.anchoredPosition = new Vector2(-72, 72);
            _fitBtn.gameObject.SetActive(false);

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

            // Sol kenar (kova seçiliyken): sihirli boyalar
            _magicPicker = SidePill(card.transform, "MagicPicker", true, MagicPaint.Count, _magicItems, i => { SelectMagic(i, true); });
            // Sağ kenar (damga seçiliyken): şekiller
            _stampPicker = SidePill(card.transform, "StampPicker", false, Art.Stamps.Length, _stampItems, i => SelectStamp(i, true));
            var stamps = Art.Stamps;
            for (int i = 0; i < stamps.Length; i++) { _stampItems[i].img.sprite = stamps[i]; _stampItems[i].img.preserveAspect = true; }
            SelectMagic(0, false);
            SelectStamp(Random.Range(0, stamps.Length), false);
        }

        RectTransform SidePill(Transform parent, string name, bool left, int count, List<(Image img, Image ring)> items, System.Action<int> onPick)
        {
            const float cellH = 92f;
            var rt = UIKit.Node(name, parent);
            rt.anchorMin = rt.anchorMax = new Vector2(left ? 0 : 1, 0.5f);
            rt.pivot = new Vector2(left ? 0 : 1, 0.5f);
            rt.anchoredPosition = new Vector2(left ? 26 : -26, 30);
            rt.sizeDelta = new Vector2(100, cellH * count + 20);
            var pill = UIKit.Card(rt, "Pill", Theme.Paper.WithAlpha(0.97f), 0.5f, 3f);
            ((RectTransform)pill.transform.parent).Stretch();
            for (int i = 0; i < count; i++)
            {
                int idx = i;
                var cell = UIKit.Node("Item", pill.transform);
                cell.anchorMin = cell.anchorMax = new Vector2(0.5f, 1f);
                cell.sizeDelta = new Vector2(96, cellH);
                cell.anchoredPosition = new Vector2(0, -10 - cellH * (i + 0.5f));
                var hit = cell.gameObject.AddComponent<Image>();
                hit.color = Color.clear;
                var ring = UIKit.Box(cell, "Ring", Theme.Accent, UIKit.Ring);
                ring.type = Image.Type.Simple;
                ring.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84, 84));
                var img = UIKit.Box(cell, "Img", Color.white, UIKit.Circle);
                img.type = Image.Type.Simple;
                img.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 64));
                var b = cell.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { Sfx.Tap(); onPick(idx); });
                items.Add((img, ring));
            }
            rt.gameObject.SetActive(false);
            return rt;
        }

        void SelectMagic(int index, bool feedback)
        {
            _canvas.Magic = index;
            for (int i = 0; i < _magicItems.Count; i++) _magicItems[i].ring.enabled = i == index;
            if (!feedback) return;
            Tween.Punch(_magicItems[index].img.transform, 0.2f);
            Haptics.Light();
            Speech.Say(index == 0 ? Loc.T("magic_normal_say") : Loc.T("magic_say", Loc.T("magic_" + index)), "sihir_" + index);
        }

        void SelectStamp(int index, bool feedback)
        {
            _canvas.StampIndex = index;
            for (int i = 0; i < _stampItems.Count; i++) _stampItems[i].ring.enabled = i == index;
            if (!feedback) return;
            Tween.Punch(_stampItems[index].img.transform, 0.2f);
            Haptics.Light();
            Speech.Say(Loc.T("stamp_" + index), "damga_" + index);
        }

        /// <summary>Seçili kalemin rengine göre sihirli boya önizlemeleri ve damga renkleri.</summary>
        void RefreshPickers()
        {
            if (_magicItems.Count == 0) return;
            foreach (var sp in _previewSprites) if (sp != null) { Destroy(sp.texture); Destroy(sp); }
            _previewSprites.Clear();
            var col = _canvas.CurrentColor;
            if (col.grayscale > 0.92f) col = Color.Lerp(col, Theme.Text, 0.12f);
            for (int i = 0; i < _magicItems.Count; i++)
            {
                var sp = MagicPaint.Preview(col, i);
                _previewSprites.Add(sp);
                _magicItems[i].img.sprite = sp;
                _magicItems[i].img.color = Color.white;
            }
            foreach (var it in _stampItems) it.img.color = col;
        }

        void StampPop(Vector2 screen, Sprite sprite, float rot)
        {
            var root = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var local);
            var img = UIKit.Box(root, "StampPop", _canvas.CurrentColor, sprite);
            img.type = Image.Type.Simple;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = local - root.rect.center;
            float s = new[] { 110f, 170f, 250f }[Mathf.Clamp(_canvas.BrushSize, 0, 2)];
            rt.sizeDelta = new Vector2(s, s);
            rt.localRotation = Quaternion.Euler(0, 0, rot);
            Tween.To(rt, 0.3f, k =>
            {
                rt.localScale = Vector3.one * Mathf.Lerp(1.5f, 1f, Tween.OutCubic(k));
                img.color = img.color.WithAlpha(1f - k);
            }, Tween.Linear, 0f, () => { if (rt != null) Destroy(rt.gameObject); });
            // minik parıltılar
            for (int i = 0; i < 6; i++)
            {
                var sp = UIKit.Box(root, "Spark", Color.white, Art.Star5);
                sp.type = Image.Type.Simple;
                var srt = sp.rectTransform;
                srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
                srt.sizeDelta = new Vector2(30, 30);
                var from = rt.anchoredPosition;
                float a = i * 60f * Mathf.Deg2Rad + Random.Range(-0.3f, 0.3f);
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (s * 0.6f + 40f);
                sp.color = Theme.Butter;
                Tween.To(srt, 0.45f, k =>
                {
                    srt.anchoredPosition = from + dir * Tween.OutCubic(k);
                    srt.localScale = Vector3.one * (1f - k);
                }, Tween.Linear, 0f, () => { if (srt != null) Destroy(srt.gameObject); });
            }
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
            trt.sizeDelta = new Vector2(500, 100);
            Sprite[] icons = { Art.Bucket, Art.Pencil, Art.Eraser, Art.Heart };
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                var cell = UIKit.Node("Tool", tools.transform);
                cell.anchorMin = new Vector2(i / 5f, 0);
                cell.anchorMax = new Vector2((i + 1) / 5f, 1);
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
            {
                // 4. düğme: sayılı boyama (1-2-3)
                var cell = UIKit.Node("Numbers", tools.transform);
                cell.anchorMin = new Vector2(4 / 5f, 0);
                cell.anchorMax = new Vector2(1, 1);
                cell.offsetMin = new Vector2(6, 6);
                cell.offsetMax = new Vector2(-6, -6);
                _numBtnBg = cell.gameObject.AddComponent<Image>();
                _numBtnBg.sprite = UIKit.Rounded;
                _numBtnBg.type = Image.Type.Sliced;
                _numBtnBg.pixelsPerUnitMultiplier = 0.55f;
                _numBtnBg.color = Color.clear;
                _numBtnText = UIKit.Label(cell, "123", 34, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                _numBtnText.rectTransform.Stretch();
                var b = cell.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { Sfx.Tap(); SetNumbers(!_numbers, true); });
                cell.gameObject.AddComponent<PressFeedback>();
            }

            // ---- Palet sekmeleri (yatay kaydırılır) ----
            var tabScrollRt = UIKit.Node("PaletteTabs", panel.transform);
            tabScrollRt.anchorMin = new Vector2(0, 1);
            tabScrollRt.anchorMax = new Vector2(1, 1);
            tabScrollRt.pivot = new Vector2(0.5f, 1);
            tabScrollRt.offsetMin = new Vector2(538, -120);
            _paletteScroll = tabScrollRt;
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
                le.preferredWidth = 50 + Loc.T("pal_" + i).Length * 21;
                le.preferredHeight = 76;
                var lbl = UIKit.Label(bg.transform, Loc.T("pal_" + i), 32, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
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
            if (tool != Tool.Fill && _numbers) SetNumbers(false, feedback);
            _canvas.Tool = tool;
            SaveSystem.ToolIndex = (int)tool;
            for (int i = 0; i < _toolButtons.Count; i++)
            {
                bool on = i == (int)tool;
                _toolButtons[i].bg.color = on ? Theme.Accent : Color.clear;
                _toolButtons[i].icon.color = on ? Color.white : Theme.Text;
            }

            ShowSide(_sizePicker, tool != Tool.Fill, feedback);
            ShowSide(_magicPicker, tool == Tool.Fill, feedback);
            ShowSide(_stampPicker, tool == Tool.Stamp, feedback);
            UpdateSizeDots();

            if (feedback)
            {
                Tween.Punch(_toolButtons[(int)tool].bg.transform, 0.1f);
                string key = "tip_" + tool;
                if (tool != Tool.Fill && PlayerPrefs.GetInt(key, 0) == 0)
                {
                    PlayerPrefs.SetInt(key, 1);
                    Popup.Toast(Loc.T(tool == Tool.Pencil ? "tip_pencil" : tool == Tool.Stamp ? "tip_stamp" : "tip_eraser"));
                }
            }
        }

        static void ShowSide(RectTransform rt, bool show, bool animate)
        {
            if (rt == null) return;
            if (show == rt.gameObject.activeSelf && animate) return;
            rt.gameObject.SetActive(show);
            if (show && animate) Tween.Pop(rt, 0f, 0.35f);
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
            _boxLabel.text = _numbers ? Loc.T("numbered") : Loc.T("pal_" + index);

            foreach (Transform c in _crayonRow) Destroy(c.gameObject);
            _crayons.Clear();
            var colors = Theme.Palettes[index].Colors;
            if (_numbers)
            {
                colors = new Color[_numColors.Length];
                for (int i = 0; i < colors.Length; i++) colors[i] = _numColors[i].color;
            }
            int n = colors.Length;
            float cw = _numbers ? 92f : CrayonW;
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
                holder.sizeDelta = new Vector2(cw, CrayonH);
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
                if (_numbers)
                {
                    // numara rozeti (kutunun üstünde kalan kısımda)
                    var badge = UIKit.Box(holder, "Badge", Color.white, UIKit.Circle);
                    badge.type = Image.Type.Simple;
                    badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(0.5f, 0.66f);
                    badge.rectTransform.sizeDelta = new Vector2(62, 62);
                    var num = UIKit.Label(badge.transform, (i + 1).ToString(), 42, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                    num.rectTransform.Stretch(0, 0, 0, 4);
                }

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
            RefreshPickers();
            if (feedback)
            {
                Sfx.Tap();
                _crayons[index].LiftVel += 500f;
                _crayons[index].Wobble = 1f;
                if (_canvas.Tool == Tool.Eraser) SelectTool(Tool.Pencil, true);
                Haptics.Light();
                // rengin adını söyle (sayılı modda numarasıyla)
                string cname = _numbers ? _numColors[index].name : Names.ColorName(_crayons[index].Color);
                Speech.Say(_numbers ? (index + 1) + ", " + cname : Names.Cap(cname), "renk_" + Speech.Slug(cname));
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

        // ================= Sayılı mod =================

        void SetNumbers(bool on, bool feedback)
        {
            if (on && _level.IsBlank) { Popup.Toast(Loc.T("no_numbers_blank")); return; }
            _numbers = on;
            SaveSystem.NumberMode = on;
            _numBtnBg.color = on ? Theme.Accent : Color.clear;
            _numBtnText.color = on ? Color.white : Theme.Text;
            if (on)
            {
                if (_canvas.Tool != Tool.Fill) SelectTool(Tool.Fill, false);
                BuildNumbers();
            }
            else if (_numLayer != null) { Destroy(_numLayer.gameObject); _numLayer = null; _numLabels = null; }
            _paletteScroll.gameObject.SetActive(!on);
            _selected = 0;
            SelectPalette(_palette, feedback);
            if (feedback)
            {
                Tween.Punch(_numBtnBg.transform, 0.12f);
                if (on) Speech.Say(Loc.T("numbers_on"));
                _saveTimer = 1.2f;
            }
        }

        void BuildNumbers()
        {
            int n = _level.Count;
            // resme özgü sabit tohum: her açılışta aynı numaralar
            uint seed = 2166136261u;
            foreach (char ch in Id) seed = (seed ^ ch) * 16777619u;
            var rnd = new System.Random((int)(seed & 0x7fffffff));
            var order = new List<int>();
            for (int i = 0; i < Names.Basic.Length; i++) order.Add(i);
            for (int i = 0; i < order.Count; i++) { int j = rnd.Next(i, order.Count); (order[i], order[j]) = (order[j], order[i]); }
            _numColors = new (string, string, Color)[NumberCount];
            for (int i = 0; i < NumberCount; i++) _numColors[i] = Names.Basic[order[i]];

            _target = new int[n + 1];
            if (_numLayer != null) Destroy(_numLayer.gameObject);
            _numLayer = UIKit.Node("Numbers", _canvas.Content).Stretch();
            _numLabels = new Text[n + 1];
            int border = _canvas.BorderRegion;
            for (int i = 1; i <= n; i++)
            {
                if (i == border) { _target[i] = -1; continue; }
                _target[i] = (int)(((((uint)i * 2654435761u) ^ seed) >> 9) % (uint)NumberCount);
                float r = _canvas.RegionLocalRadius(i);
                if (r < 14f)
                {
                    // parmağın sığmayacağı minik bölgeleri kendimiz boyarız
                    if (!_canvas.Filled[i]) _canvas.FillSilently(i, Color.white);
                    _target[i] = -1;
                    continue;
                }
                var uv = new Vector2((_level.Meta.cx[i - 1] + 0.5f) / _level.Width, (_level.Meta.cy[i - 1] + 0.5f) / _level.Height);
                var t = UIKit.Label(_numLayer, (_target[i] + 1).ToString(), Mathf.RoundToInt(Mathf.Clamp(r * 1.25f, 22f, 72f)), Theme.TextSoft, TextAnchor.MiddleCenter, FontStyle.Bold);
                t.rectTransform.anchorMin = t.rectTransform.anchorMax = uv;
                t.rectTransform.anchoredPosition = Vector2.zero;
                t.rectTransform.sizeDelta = new Vector2(80, 80);
                _numLabels[i] = t;
            }
            RefreshNumberLabels();
        }

        void RefreshNumberLabels()
        {
            if (!_numbers || _numLabels == null) return;
            for (int i = 1; i < _numLabels.Length; i++)
            {
                if (_numLabels[i] == null) continue;
                bool right = _canvas.Filled[i] && SameColor(_canvas.RegionColors[i], _numColors[_target[i]].color);
                _numLabels[i].enabled = !right;
            }
        }

        static bool SameColor(Color32 a, Color b)
        {
            Color32 c = b;
            return Mathf.Abs(a.r - c.r) < 3 && Mathf.Abs(a.g - c.g) < 3 && Mathf.Abs(a.b - c.b) < 3;
        }

        bool CheckNumber(int region, Color color)
        {
            if (!_numbers || _target == null || region >= _target.Length || _target[region] < 0) return true;
            int want = _target[region];
            if (SameColor(color, _numColors[want].color)) return true;
            // yanlış renk: kızmadan, doğru kalemi gösterelim
            Sfx.Wrong();
            if (_numLabels[region] != null) Tween.Punch(_numLabels[region].transform, 0.5f, 0.45f);
            if (want < _crayons.Count) { _crayons[want].LiftVel += 700f; _crayons[want].Wobble = 1f; }
            Speech.Say(Loc.T("number_hint", want + 1, Loc.Lang == "tr" ? Names.Cap(_numColors[want].name) : _numColors[want].acc));
            return false;
        }

        // ================= Akış =================

        void AskClear()
        {
            if (_canvas.Progress <= 0 && !_canvas.HasPaint) { Popup.Toast(Loc.T("already_clean")); return; }
            Popup.Confirm(Loc.T("clear_title"), Loc.T("clear_msg"), Loc.T("clear_yes"), Loc.T("clear_no"), () =>
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
            bool allDone = _canvas.AllDone;
            if (allDone) pct = 100;
            _progressText.text = Loc.T("percent", pct);
            _lastActivity = Time.unscaledTime;
            // Pofuduk: çeyrek, yarı ve üç çeyrekte kısa bir cesaretlendirme (not değil, sevinç)
            if (_ready && _buddy != null && !_level.IsBlank && !_completedShown && !allDone)
            {
                int m = p >= 0.75f ? 3 : p >= 0.5f ? 2 : p >= 0.25f ? 1 : 0;
                if (m > _milestone) { _milestone = m; _buddy.Peek(Loc.T("buddy_" + m * 25)); }
            }
            RefreshNumberLabels();
            float from = _progressFill.anchorMax.x;
            Tween.To(_progressFill, 0.35f, k => _progressFill.anchorMax = new Vector2(Mathf.Lerp(from, p, k), 1));
            SetUndoVisible(_canvas.CanUndo);

            // Kartın rengi arka plan bölgesini takip eder: resim kâğıda kenarsız uzanır
            var targetCol = _canvas.BorderColor;
            if (_cardFace.color != targetCol)
            {
                var startCol = _cardFace.color;
                Tween.Stop(_cardTween);
                _cardTween = Tween.To(_cardFace, 0.45f, k => _cardFace.color = Color.Lerp(startCol, targetCol, k), Tween.InOutSine);
            }

            // Neredeyse bitince (ya da kalemle çalışırken) "Bitir" her zaman ulaşılabilir olsun
            bool showFinish = (_level.IsBlank ? _canvas.HasPaint || p > 0 : p >= 0.8f) && !_completedShown;

            // gün serisi: bugün ilk kez boyadıysa say
            if (_ready && SaveSystem.RecordPlay())
            {
                int streak = SaveSystem.Streak;
                if (streak >= 2)
                {
                    Popup.Toast(Loc.T("streak_toast", streak));
                    Speech.Say(Loc.T("streak_say", streak));
                }
            }
            if (showFinish != _finishBtn.gameObject.activeSelf)
            {
                _finishBtn.gameObject.SetActive(showFinish);
                if (showFinish) Tween.Pop(_finishBtn);
            }
            _saveTimer = 1.2f;

            if (allDone && !_completedShown && !_wasDone && !_level.IsBlank)
                Tween.Delay(0.6f, () => { if (this != null && _canvas != null && _canvas.AllDone && !_completedShown) ShowComplete(); });
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
            // "sığdır" butonu yalnızca yakınlaştırılmışken görünür
            if (_canvas != null && _fitBtn != null)
            {
                bool zoomed = _canvas.Zoom > 1.05f;
                if (zoomed != _fitBtn.gameObject.activeSelf)
                {
                    _fitBtn.gameObject.SetActive(zoomed);
                    if (zoomed) Tween.Pop(_fitBtn, 0f, 0.3f);
                }
            }
            // uzun süre hiçbir şey olmadıysa Pofuduk yardıma gelir (dakikada en çok bir kez)
            if (_ready && _buddy != null && !_completedShown && !BreakTimer.Active)
            {
                float now = Time.unscaledTime;
                if (now - _lastActivity > 30f && now - _lastIdlePeek > 60f)
                {
                    _lastIdlePeek = now;
                    _lastActivity = now;
                    if (_level.IsBlank) _buddy.Peek(Loc.T("buddy_idle_blank"));
                    else if (_canvas.ShowHint()) _buddy.Peek(Loc.T("buddy_idle"));
                }
            }
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

        string BackGroup => _level != null && _level.IsBlank ? "kolay" : Group;

        void Exit()
        {
            SaveNow();
            // serbest çizimler "Resimlerim" albümünde durur
            if (_level != null && _level.IsBlank) App.I.ShowAlbum();
            else App.I.ShowGallery(BackGroup);
        }

        // ================= Kutlama =================

        void ShowComplete()
        {
            if (_completedShown) return;
            _completedShown = true;
            bool first = !_wasDone;
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
            cardHolder.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(860, 1320));
            var card = UIKit.Card(cardHolder, "Card", Theme.Card, 0.6f);
            ((RectTransform)card.transform.parent).Stretch();
            Tween.Pop(cardHolder, 0.1f, 0.6f);

            // Üç yıldız, sırayla "çın" diye yerine oturur
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                var slot = UIKit.Box(card.transform, "StarSlot", Theme.Butter.WithAlpha(0.9f), MascotArt.StarOutline);
                slot.type = Image.Type.Simple;
                float x = (i - 1) * 170f, y = i == 1 ? 10f : -20f, size = i == 1 ? 190f : 150f;
                slot.rectTransform.anchorMin = slot.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                slot.rectTransform.anchoredPosition = new Vector2(x, y);
                slot.rectTransform.sizeDelta = new Vector2(size, size);
                slot.rectTransform.localRotation = Quaternion.Euler(0, 0, (1 - i) * 12f);
                var star = UIKit.Box(slot.transform, "Star", Color.white, MascotArt.Star);
                star.type = Image.Type.Simple;
                star.rectTransform.Stretch();
                star.rectTransform.localScale = Vector3.zero;
                Tween.Delay(0.55f + 0.32f * i, () =>
                {
                    if (star == null) return;
                    Sfx.Star();
                    Tween.Pop(star.rectTransform, 0f, 0.45f);
                    if (k == 2) Sfx.Clap();
                });
            }

            var t = UIKit.Label(card.transform, Loc.T("great"), 110, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Band(true, 140, 110);
            var sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = Theme.Accent.WithAlpha(0.6f);
            sh.effectDistance = new Vector2(0, -6);
            var s = UIKit.Label(card.transform, Loc.T("done_sub"), 40, Theme.TextSoft);
            s.rectTransform.Band(true, 60, 245);

            var photoBg = UIKit.Box(card.transform, "PhotoBg", Theme.Paper, null, 1.2f);
            photoBg.rectTransform.Stretch(60, 330, 60, 260);
            photoBg.gameObject.AddComponent<Dance>();
            var hearts = UIKit.Node("Hearts", card.transform).Stretch(60, 330, 60, 260);
            hearts.gameObject.AddComponent<HeartRain>();
            var photo = UIKit.Node("Photo", photoBg.transform).Stretch(20, 20, 20, 20);
            var raw = photo.gameObject.AddComponent<RawImage>();
            raw.texture = _level.Lines;
            raw.material = _canvas.Material;
            raw.raycastTarget = false;
            var fit = photo.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = (float)_level.Width / _level.Height;

            var gallery = UIKit.Button(card.transform, Loc.T("gallery"), Theme.Lavender, Theme.Text, 44, Exit);
            var grt = (RectTransform)gallery.transform;
            grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0);
            grt.sizeDelta = new Vector2(230, 140);
            grt.anchoredPosition = new Vector2(-255, 120);

            // Kaydet: telefonun galerisine
            Button save = null;
            save = UIKit.Button(card.transform, Loc.T("save"), Theme.Butter, Theme.Text, 40, () => SaveToPhotos(save));
            var srt = (RectTransform)save.transform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0);
            srt.sizeDelta = new Vector2(240, 140);
            srt.anchoredPosition = new Vector2(0, 120);
            var stxt = UIKit.ButtonText(save);
            stxt.rectTransform.offsetMin = new Vector2(62, 0);
            var dl = UIKit.Box(save.transform.Find("Face"), "Icon", Theme.Text, Art.Download);
            dl.type = Image.Type.Simple;
            dl.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(52, 0), new Vector2(46, 46));

            var next = UIKit.Button(card.transform, _level.IsBlank ? Loc.T("gallery_next") : Loc.T("next"), Theme.Accent, Color.white, 44, NextLevel);
            var nrt = (RectTransform)next.transform;
            nrt.anchorMin = nrt.anchorMax = new Vector2(0.5f, 0);
            nrt.sizeDelta = new Vector2(230, 140);
            nrt.anchoredPosition = new Vector2(255, 120);
            if (_level.IsBlank) gallery.gameObject.SetActive(false);

            // Pofuduk kartın köşesinde sevinçten zıplar
            var mascot = Mascot.Create(card.transform, 230, false);
            var mrt = (RectTransform)mascot.transform;
            mrt.anchorMin = mrt.anchorMax = new Vector2(1, 1);
            mrt.anchoredPosition = new Vector2(-60, -330);
            Tween.Delay(1.4f, () => { if (mascot != null) { mascot.Jump(); mascot.Wave(); } });
            Tween.Delay(2.2f, () => { if (mascot != null) mascot.Jump(); });

            if (first) { SaveSystem.Stars += 3; Stats.Add("pic"); }
            // yeni Pofuduk çıkartması açıldıysa kutlama kartında şapkasıyla haber ver
            var newHat = first && !_level.IsBlank ? PuffyStickers.UnlockedAt(PuffyStickers.DoneCount()) : MascotHat.None;
            if (newHat != MascotHat.None)
            {
                mascot.SetHat(newHat);
                Tween.Delay(2.6f, () =>
                {
                    if (this == null) return;
                    Popup.Toast(Loc.T("puffy_new"));
                    Speech.Say(Loc.T("puffy_new"));
                    Sfx.Star();
                });
            }
            Haptics.Medium();
            string line = _level.IsBlank ? Loc.T("blank_done_say")
                : Names.TryAnimal(Id, out _, out var acc) ? Loc.T("done_named", Loc.Lang == "tr" ? Names.Cap(acc) : acc)
                : Loc.T("done_say");
            Tween.Delay(0.4f, () => Speech.Say(line));

            Confetti.Burst(_overlay.transform, 90);
        }

        void SaveToPhotos(Button btn)
        {
            var cols = new Color32[_canvas.RegionColors.Length];
            for (int i = 0; i < cols.Length; i++) cols[i] = _canvas.Filled[i] ? _canvas.RegionColors[i] : (Color32)Theme.Paper;
            var paint = _canvas.HasPaint ? _canvas.Paint.ReadBack() : null;
            var img = PhotoSaver.Render(_level, cols, paint);
            string msg = PhotoSaver.Save(img, _level.IsBlank ? "cizim" : Names.LevelTitle(Id));
            Destroy(img);
            if (paint != null) Destroy(paint);
            Popup.Toast(msg);
            Sfx.Star();
            Haptics.Medium();
            Speech.Say(Loc.T("saved_say"));
            if (btn != null)
            {
                UIKit.ButtonText(btn).text = Loc.T("saved_btn");
                UIKit.ButtonText(btn).fontSize = 32;
                btn.interactable = false;
            }
        }

        void NextLevel()
        {
            if (_level.IsBlank) { App.I.ShowAlbum(); return; }
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
            Application.targetFrameRate = 60;
            foreach (var sp in _previewSprites) if (sp != null) { Destroy(sp.texture); Destroy(sp); }
            if (_canvas != null && _canvas.Level != null && _saveTimer > 0) SaveNow();
            _level?.Unload();
        }
    }
}
