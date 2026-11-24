using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>Level seçimi: Kolay / Orta / Zor sekmeleri ve kaydırılabilir resim kartları.</summary>
    public class GalleryScreen : MonoBehaviour, IScreen
    {
        public string Group;

        static readonly string[] Groups = { "kolay", "orta", "zor" };
        static string _lastGroup;   // ilk açılışta yaş ayarına göre (3–4: kolay, 5–6: orta, 7+: zor)
        static readonly Dictionary<string, float> _scrollMemory = new();

        RectTransform _gridRoot, _tabIndicator;
        ScrollRect _scroll;
        readonly List<Texture2D> _textures = new();

        // Küçük resimler sekme açılınca tek karede değil, görünür olanlardan başlayarak karede birkaç tane yüklenir.
        readonly List<(RectTransform cell, RawImage raw, AspectRatioFitter fit, System.Func<Texture2D> load)> _lazy = new();
        static readonly Vector3[] _corners = new Vector3[4];

        void Update()
        {
            if (_lazy.Count == 0 || _scroll == null || _scroll.viewport == null) return;
            _scroll.viewport.GetWorldCorners(_corners);
            float vMin = Mathf.Min(_corners[0].y, _corners[2].y), vMax = Mathf.Max(_corners[0].y, _corners[2].y);
            float margin = (vMax - vMin) * 0.5f;
            int budget = 3;
            for (int i = 0; i < _lazy.Count && budget > 0; i++)
            {
                var e = _lazy[i];
                if (e.cell == null) { _lazy.RemoveAt(i--); continue; }
                e.cell.GetWorldCorners(_corners);
                float cMin = Mathf.Min(_corners[0].y, _corners[2].y), cMax = Mathf.Max(_corners[0].y, _corners[2].y);
                if (cMax < vMin - margin || cMin > vMax + margin) continue;   // henüz uzakta
                budget--;
                _lazy.RemoveAt(i--);
                var tex = e.load();
                if (tex == null) continue;
                _textures.Add(tex);
                e.raw.texture = tex;
                e.raw.enabled = true;
                e.fit.aspectRatio = (float)tex.width / tex.height;
            }
        }
        readonly List<(Text label, int index)> _tabLabels = new();

        void Start()
        {
            if (string.IsNullOrEmpty(Group)) Group = _lastGroup ?? Groups[Mathf.Clamp(SaveSystem.AgeBand, 0, Groups.Length - 1)];
            var root = (RectTransform)transform;

            // ---- Üst kısım ----
            var back = UIKit.CircleButton(root, "‹", Theme.Card, Theme.Text, 120, 90, () => App.I.ShowWelcome());
            ((RectTransform)back.transform).Place(new Vector2(0, 1), new Vector2(110, -110), new Vector2(120, 120));

            var music = UIKit.IconButton(root, Art.Note, Theme.Card, Theme.Text, 120, () => Popup.Toast(Sfx.MusicButton()), 0.5f);
            A11y.Label(music, Loc.T("a11y_music"));
            ((RectTransform)music.transform).Place(new Vector2(1, 1), new Vector2(-110, -110), new Vector2(120, 120));

            var title = UIKit.Label(root, Loc.T("gallery_title"), 84, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Band(true, 110, 200);
            var sub = UIKit.Label(root, Loc.T("gallery_sub"), 42, Theme.TextSoft);
            sub.rectTransform.Band(true, 60, 305);

            // ---- Sekmeler ----
            var tabs = UIKit.Card(root, "Tabs", Theme.Card.WithAlpha(0.9f), 0.5f, 4f);
            var tabsRt = (RectTransform)tabs.transform.parent;
            tabsRt.Band(true, 120, 400, 90);
            _tabIndicator = UIKit.Box(tabs.transform, "Indicator", Theme.Accent, null, 0.55f).rectTransform;
            _tabIndicator.anchorMin = new Vector2(0, 0);
            _tabIndicator.anchorMax = new Vector2(1f / 3f, 1);
            _tabIndicator.offsetMin = new Vector2(10, 10);
            _tabIndicator.offsetMax = new Vector2(-10, -10);
            for (int i = 0; i < Groups.Length; i++)
            {
                int idx = i;
                var seg = UIKit.Node("Tab", tabs.transform);
                seg.anchorMin = new Vector2(i / 3f, 0);
                seg.anchorMax = new Vector2((i + 1) / 3f, 1);
                seg.offsetMin = seg.offsetMax = Vector2.zero;
                var hit = seg.gameObject.AddComponent<Image>();
                hit.color = Color.clear;
                var btn = seg.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => { Sfx.Tap(); SelectGroup(Groups[idx], true); });
                int count = Level.GetGroup(Groups[i]).levels.Count;
                var lbl = UIKit.Label(seg, $"{Theme.DifficultyTitle(Groups[i])}  {count}", 44, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                lbl.rectTransform.Stretch();
                _tabLabels.Add((lbl, i));
            }

            // ---- Kaydırılabilir ızgara ----
            var scrollRt = UIKit.Node("Scroll", root);
            scrollRt.Stretch(0, 560, 0, 0);
            _scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            _scroll.horizontal = false;
            _scroll.movementType = ScrollRect.MovementType.Elastic;
            _scroll.decelerationRate = 0.12f;
            _scroll.scrollSensitivity = 40;
            var vp = UIKit.Node("Viewport", scrollRt).Stretch();
            vp.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(0, 40);
            var vpImg = vp.gameObject.AddComponent<Image>();
            vpImg.color = Color.clear;
            _scroll.viewport = vp;
            _gridRoot = UIKit.Node("Grid", vp);
            _gridRoot.anchorMin = new Vector2(0, 1);
            _gridRoot.anchorMax = new Vector2(1, 1);
            _gridRoot.pivot = new Vector2(0.5f, 1);
            _gridRoot.offsetMin = _gridRoot.offsetMax = Vector2.zero;
            var grid = _gridRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(450, 540);
            grid.spacing = new Vector2(50, 56);
            grid.padding = new RectOffset(0, 0, 50, 160);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            // Tablette genişlik arttığında 3 sütun: boşluk kalmasın, daha çok resim görünsün
            if (App.I.CanvasWidth >= 1380f)
            {
                grid.constraintCount = 3;
                grid.cellSize = new Vector2(400, 480);
                grid.spacing = new Vector2(36, 48);
            }
            _gridRoot.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _scroll.content = _gridRoot;

            SelectGroup(Group, false);
        }

        void SelectGroup(string group, bool animate)
        {
            if (animate && Group != null && _gridRoot.childCount > 0) _scrollMemory[Group] = _scroll.verticalNormalizedPosition;
            Group = group;
            _lastGroup = group;
            int gi = System.Array.IndexOf(Groups, group);
            if (gi < 0) gi = 0;

            // Sekme göstergesi kayar
            float fromMin = _tabIndicator.anchorMin.x;
            float toMin = gi / 3f;
            if (animate)
                Tween.To(_tabIndicator, 0.3f, k =>
                {
                    float x = Mathf.Lerp(fromMin, toMin, k);
                    _tabIndicator.anchorMin = new Vector2(x, 0);
                    _tabIndicator.anchorMax = new Vector2(x + 1f / 3f, 1);
                }, Tween.OutBack);
            else
            {
                _tabIndicator.anchorMin = new Vector2(toMin, 0);
                _tabIndicator.anchorMax = new Vector2(toMin + 1f / 3f, 1);
            }
            foreach (var (label, index) in _tabLabels) label.color = index == gi ? Color.white : Theme.Text;

            BuildGrid();
        }

        void BuildGrid()
        {
            foreach (Transform c in _gridRoot) Destroy(c.gameObject);
            foreach (var t in _textures) if (t) Destroy(t);
            _textures.Clear();
            _lazy.Clear();

            var levels = Level.GetGroup(Group).levels;
            if (levels.Count == 0)
            {
                var empty = UIKit.Node("Empty", _gridRoot);
                var msg = UIKit.Label(empty, Loc.T("empty_tab"), 44, Theme.TextSoft);
                msg.rectTransform.Place(new Vector2(1f, 0.5f), new Vector2(25, -60), new Vector2(900, 300));
                return;
            }

            {
                // her sekmenin başında: serbest çizim için boş sayfa (büyükler de çizebilsin)
                var cell = UIKit.Node("Blank", _gridRoot);
                BuildCard(cell, new LevelEntry { id = Level.BlankId }, Level.BlankGroup);
                Tween.Pop(cell.GetChild(0), 0f, 0.4f);
            }
            levels = NumbersTogether(levels);
            for (int i = 0; i < levels.Count; i++)
            {
                var entry = levels[i];
                var cell = UIKit.Node(entry.id, _gridRoot);
                BuildCard(cell, entry);
                if (i < 8) Tween.Pop(cell.GetChild(0), 0.04f * i, 0.4f);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_gridRoot);
            _scroll.StopMovement();
            _scroll.verticalNormalizedPosition = _scrollMemory.TryGetValue(Group, out var pos) ? pos : 1f;
        }

        /// <summary>
        /// Sayı sayfaları (lofi-num-1..10) bölge sayısına göre sıralanınca listeye dağılıyordu; ilk sayı sayfasının
        /// yerinde 1'den 10'a sırayla bir arada dururlar ("5'i aç" diyen öğretmen aramak zorunda kalmaz).
        /// </summary>
        static List<LevelEntry> NumbersTogether(List<LevelEntry> levels)
        {
            const string prefix = "lofi-num-";
            static int Num(LevelEntry e) => int.TryParse(e.id.Substring(prefix.Length), out var n) ? n : 999;
            var nums = levels.FindAll(e => e.id.StartsWith(prefix));
            if (nums.Count < 2) return levels;
            nums.Sort((a, b) => Num(a).CompareTo(Num(b)));
            var list = new List<LevelEntry>(levels.Count);
            bool placed = false;
            foreach (var e in levels)
            {
                if (!e.id.StartsWith(prefix)) { list.Add(e); continue; }
                if (placed) continue;
                list.AddRange(nums);
                placed = true;
            }
            return list;
        }

        void BuildCard(RectTransform cell, LevelEntry entry, string groupOverride = null)
        {
            string grp = groupOverride ?? Group;
            bool blank = grp == Level.BlankGroup;
            string key = grp + "_" + entry.id;
            // boş sayfa kartı her dokunuşta yeni bir sayfa açar; eski çizimler "Resimlerim"de
            var save = blank ? null : SaveSystem.Load(key);

            var inner = UIKit.Node("Inner", cell).Stretch();
            var face = UIKit.Card(inner, "Card", Theme.Card, 0.8f);
            ((RectTransform)face.transform.parent).Stretch();
            face.raycastTarget = true;

            var photoBg = UIKit.Box(face.transform, "PhotoBg", Theme.Paper, null, 1.4f);
            photoBg.rectTransform.Stretch(24, 24, 24, 110);
            var photo = UIKit.Node("Photo", photoBg.transform).Stretch(14, 14, 14, 14);
            var raw = photo.gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            var fit = photo.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            if (blank)
            {
                // boş sayfa: ortada büyük bir kalem ve "+"
                var pen = UIKit.Box(photoBg.transform, "Pencil", Theme.Accent, Art.Pencil);
                pen.type = Image.Type.Simple;
                pen.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(-10, 10), new Vector2(150, 150));
                var plus = UIKit.Label(photoBg.transform, "+", 90, Theme.Lavender, TextAnchor.MiddleCenter, FontStyle.Bold);
                plus.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(80, 70), new Vector2(100, 100));
            }
            else
            {
                raw.enabled = false;   // resim yüklenince açılır (Update)
                bool saved = save != null;
                _lazy.Add((cell, raw, fit, () => (saved ? SaveSystem.LoadThumb(key) : null) ?? Level.LoadThumb(grp, entry.id)));
            }

            var name = UIKit.Label(face.transform, blank ? Loc.T("blank_page") : Names.LevelTitle(entry.id), 36, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            name.rectTransform.anchorMin = new Vector2(0, 0);
            name.rectTransform.anchorMax = new Vector2(1, 0);
            name.rectTransform.pivot = new Vector2(0, 0);
            name.rectTransform.offsetMin = new Vector2(34, 22);
            name.rectTransform.offsetMax = new Vector2(-170, 92);
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;   // "Oyuncak kutusu" gibi iki satırlık adlar kartın içinde küçülür
            name.lineSpacing = 0.85f;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 22;
            name.resizeTextMaxSize = 36;

            // Durum rozeti
            string badge; Color badgeCol;
            if (blank) { badge = Loc.T("draw_badge"); badgeCol = Theme.Sky; }
            else if (save == null || save.progress <= 0.001f) { badge = Loc.T("badge_new"); badgeCol = Theme.Lavender; }
            else if (save.done) { badge = Loc.T("badge_done"); badgeCol = Theme.Mint; }
            else { badge = Loc.T("percent", Mathf.Clamp(Mathf.RoundToInt(save.progress * 100), 1, 99)); badgeCol = Theme.Butter; }
            var pill = UIKit.Box(face.transform, "Badge", badgeCol, null, 0.9f);
            pill.rectTransform.anchorMin = pill.rectTransform.anchorMax = new Vector2(1, 0);
            pill.rectTransform.pivot = new Vector2(1, 0);
            pill.rectTransform.sizeDelta = new Vector2(130, 62);
            pill.rectTransform.anchoredPosition = new Vector2(-26, 26);
            var bt = UIKit.Label(pill.transform, badge, 32, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            bt.rectTransform.Stretch();

            var btn = inner.gameObject.AddComponent<Button>();
            btn.targetGraphic = face;
            btn.transition = Selectable.Transition.None;
            string group = grp, id = entry.id;
            btn.onClick.AddListener(() => { Sfx.Tap(); App.I.ShowColoring(group, blank ? SaveSystem.NewBlankId() : id); });
            inner.gameObject.AddComponent<PressFeedback>().pressedScale = 0.95f;
        }

        public bool OnBack()
        {
            App.I.ShowWelcome();
            return true;
        }

        void OnDestroy()
        {
            if (_scroll != null && Group != null && _gridRoot != null && _gridRoot.childCount > 0) _scrollMemory[Group] = _scroll.verticalNormalizedPosition;
            foreach (var t in _textures) if (t) Destroy(t);
        }
    }
}
