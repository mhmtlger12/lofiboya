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
        static string _lastGroup = "kolay";
        static readonly Dictionary<string, float> _scrollMemory = new();

        RectTransform _gridRoot, _tabIndicator;
        ScrollRect _scroll;
        readonly List<Texture2D> _textures = new();
        readonly List<(Text label, int index)> _tabLabels = new();
        Text _musicLabel;

        void Start()
        {
            if (string.IsNullOrEmpty(Group)) Group = _lastGroup;
            var root = (RectTransform)transform;

            // ---- Üst kısım ----
            var back = UIKit.CircleButton(root, "‹", Theme.Card, Theme.Text, 120, 90, () => App.I.ShowWelcome());
            ((RectTransform)back.transform).Place(new Vector2(0, 1), new Vector2(110, -110), new Vector2(120, 120));

            var music = UIKit.CircleButton(root, "♪", Theme.Card, Theme.Text, 120, 64, () => { Popup.Toast("♪  " + Sfx.NextTrack()); RefreshMusic(); });
            ((RectTransform)music.transform).Place(new Vector2(1, 1), new Vector2(-110, -110), new Vector2(120, 120));
            _musicLabel = UIKit.ButtonText(music);
            RefreshMusic();

            var title = UIKit.Label(root, "Boyama Defterim", 84, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Band(true, 110, 200);
            var sub = UIKit.Label(root, "bugün ne boyamak istersin?", 42, Theme.TextSoft);
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

            var levels = Level.GetGroup(Group).levels;
            if (levels.Count == 0)
            {
                var empty = UIKit.Node("Empty", _gridRoot);
                var msg = UIKit.Label(empty, "Bu bölüm henüz boş\n\nresimler/" + Group + " klasörüne resim ekleyip\ndönüştürücüyü çalıştır", 40, Theme.TextSoft);
                msg.rectTransform.Place(new Vector2(1f, 0.5f), new Vector2(25, -60), new Vector2(900, 300));
                return;
            }

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

        void BuildCard(RectTransform cell, LevelEntry entry)
        {
            string key = Group + "_" + entry.id;
            var save = SaveSystem.Load(key);

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
            var tex = (save != null ? SaveSystem.LoadThumb(key) : null) ?? Level.LoadThumb(Group, entry.id);
            if (tex != null)
            {
                _textures.Add(tex);
                raw.texture = tex;
                fit.aspectRatio = (float)tex.width / tex.height;
            }

            var name = UIKit.Label(face.transform, Level.PrettyName(entry.id), 36, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            name.rectTransform.anchorMin = new Vector2(0, 0);
            name.rectTransform.anchorMax = new Vector2(1, 0);
            name.rectTransform.pivot = new Vector2(0, 0);
            name.rectTransform.offsetMin = new Vector2(34, 22);
            name.rectTransform.offsetMax = new Vector2(-170, 92);
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 24;
            name.resizeTextMaxSize = 36;

            // Durum rozeti
            string badge; Color badgeCol;
            if (save == null || save.progress <= 0.001f) { badge = "yeni"; badgeCol = Theme.Lavender; }
            else if (save.done) { badge = "bitti!"; badgeCol = Theme.Mint; }
            else { badge = "%" + Mathf.Clamp(Mathf.RoundToInt(save.progress * 100), 1, 99); badgeCol = Theme.Butter; }
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
            string group = Group, id = entry.id;
            btn.onClick.AddListener(() => { Sfx.Tap(); App.I.ShowColoring(group, id); });
            inner.gameObject.AddComponent<PressFeedback>().pressedScale = 0.95f;
        }

        void RefreshMusic()
        {
            var face = _musicLabel.transform.parent.GetComponent<Image>();
            face.color = SaveSystem.MusicOn ? Theme.Card : Theme.Card.WithAlpha(0.5f);
            _musicLabel.color = SaveSystem.MusicOn ? Theme.Text : Theme.TextSoft;
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
