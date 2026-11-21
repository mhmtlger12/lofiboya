using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Resimlerim: çocuğun çizdiği ve boyadığı her şey, en yenisi önde.
    /// İlk kart her zaman "Yeni çizim" (sınırsız boş sayfa). Not, yüzde ya da "bitmedi" yok:
    /// her resim olduğu gibi değerlidir. Dokununca kaldığı yerden devam eder.
    /// </summary>
    public class AlbumScreen : MonoBehaviour, IScreen
    {
        RectTransform _gridRoot;
        ScrollRect _scroll;
        readonly List<Texture2D> _textures = new();

        void Start()
        {
            var root = (RectTransform)transform;
            var back = UIKit.CircleButton(root, "‹", Theme.Card, Theme.Text, 120, 90, () => App.I.ShowWelcome());
            ((RectTransform)back.transform).Place(new Vector2(0, 1), new Vector2(110, -110), new Vector2(120, 120));

            var title = UIKit.Label(root, Loc.T("album"), 84, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Band(true, 110, 200);
            var sub = UIKit.Label(root, Loc.T("album_sub"), 42, Theme.TextSoft);
            sub.rectTransform.Band(true, 60, 305);

            var scrollRt = UIKit.Node("Scroll", root);
            scrollRt.Stretch(0, 390, 0, 0);
            _scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            _scroll.horizontal = false;
            _scroll.movementType = ScrollRect.MovementType.Elastic;
            _scroll.decelerationRate = 0.12f;
            _scroll.scrollSensitivity = 40;
            var vp = UIKit.Node("Viewport", scrollRt).Stretch();
            vp.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(0, 40);
            vp.gameObject.AddComponent<Image>().color = Color.clear;
            _scroll.viewport = vp;
            _gridRoot = UIKit.Node("Grid", vp);
            _gridRoot.anchorMin = new Vector2(0, 1);
            _gridRoot.anchorMax = new Vector2(1, 1);
            _gridRoot.pivot = new Vector2(0.5f, 1);
            _gridRoot.offsetMin = _gridRoot.offsetMax = Vector2.zero;
            var grid = _gridRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(450, 540);
            grid.spacing = new Vector2(50, 56);
            grid.padding = new RectOffset(0, 0, 40, 160);
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

            // Yeni çizim: her dokunuşta yeni bir boş sayfa
            var add = UIKit.Node("New", _gridRoot);
            BuildNewCard(add);
            Tween.Pop(add.GetChild(0), 0f, 0.4f);

            int n = 0;
            foreach (var key in SaveSystem.SavedKeys())
            {
                int us = key.IndexOf('_');
                if (us <= 0) continue;
                string group = key.Substring(0, us), id = key.Substring(us + 1);
                if (group != Level.BlankGroup && !Level.GetGroup(group).levels.Exists(l => l.id == id)) continue;
                var tex = SaveSystem.LoadThumb(key);
                if (tex == null) continue;
                _textures.Add(tex);
                var cell = UIKit.Node(key, _gridRoot);
                BuildCard(cell, group, id, tex);
                if (n < 8) Tween.Pop(cell.GetChild(0), 0.04f * (n + 1), 0.4f);
                n++;
            }
            if (n == 0)
            {
                // boş albüm: Pofuduk balonla davet eder ("Yeni çizim" kartını gösterir gibi)
                var hint = UIKit.Node("Empty", _gridRoot);
                var m = Mascot.Create(hint, 280);
                var mrt = (RectTransform)m.transform;
                mrt.anchorMin = mrt.anchorMax = new Vector2(0.5f, 0f);
                mrt.pivot = new Vector2(0.5f, 0f);
                mrt.anchoredPosition = new Vector2(0, 20);
                var bub = UIKit.Card(hint, "Bubble", Color.white, 0.6f, 4f);
                var brt = (RectTransform)bub.transform.parent;
                brt.anchorMin = new Vector2(0, 1); brt.anchorMax = new Vector2(1, 1);
                brt.pivot = new Vector2(0.5f, 1f);
                brt.offsetMin = new Vector2(10, -250); brt.offsetMax = new Vector2(-10, -10);
                var msg = UIKit.Label(bub.transform, Loc.T("album_empty"), 36, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                msg.rectTransform.Stretch(18, 12, 18, 12);
                msg.horizontalOverflow = HorizontalWrapMode.Wrap;
                msg.resizeTextForBestFit = true;
                msg.resizeTextMinSize = 24;
                msg.resizeTextMaxSize = 36;
                brt.gameObject.AddComponent<Floaty>().Init(6f, 0.8f);
                Tween.Delay(0.8f, () => { if (m != null) m.Wave(2f); });
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(_gridRoot);
            _scroll.verticalNormalizedPosition = 1f;
        }

        void BuildNewCard(RectTransform cell)
        {
            var inner = UIKit.Node("Inner", cell).Stretch();
            var face = UIKit.Card(inner, "Card", Theme.Sky, 0.8f);
            ((RectTransform)face.transform.parent).Stretch();
            face.raycastTarget = true;
            var bg = UIKit.Box(face.transform, "PhotoBg", Theme.Paper, null, 1.4f);
            bg.rectTransform.Stretch(24, 24, 24, 110);
            var pen = UIKit.Box(bg.transform, "Pencil", Theme.Accent, Art.Pencil);
            pen.type = Image.Type.Simple;
            pen.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(-10, 10), new Vector2(150, 150));
            var plus = UIKit.Label(bg.transform, "+", 90, Theme.Lavender, TextAnchor.MiddleCenter, FontStyle.Bold);
            plus.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(80, 70), new Vector2(100, 100));
            var name = UIKit.Label(face.transform, Loc.T("new_drawing"), 38, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            name.rectTransform.Band(false, 90, 14);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 24;
            name.resizeTextMaxSize = 38;
            var btn = inner.gameObject.AddComponent<Button>();
            btn.targetGraphic = face;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { Sfx.Tap(); App.I.ShowColoring(Level.BlankGroup, SaveSystem.NewBlankId()); });
            inner.gameObject.AddComponent<PressFeedback>().pressedScale = 0.95f;
        }

        void BuildCard(RectTransform cell, string group, string id, Texture2D tex)
        {
            var inner = UIKit.Node("Inner", cell).Stretch();
            // polaroid gibi hafif eğik: duvara asılmış resimler
            inner.localRotation = Quaternion.Euler(0, 0, Random.Range(-2.5f, 2.5f));
            var face = UIKit.Card(inner, "Card", Theme.Card, 0.8f);
            ((RectTransform)face.transform.parent).Stretch();
            face.raycastTarget = true;
            var photoBg = UIKit.Box(face.transform, "PhotoBg", Theme.Paper, null, 1.4f);
            photoBg.rectTransform.Stretch(24, 24, 24, 110);
            var photo = UIKit.Node("Photo", photoBg.transform).Stretch(14, 14, 14, 14);
            var raw = photo.gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            raw.texture = tex;
            var fit = photo.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = (float)tex.width / tex.height;

            bool blank = group == Level.BlankGroup;
            var name = UIKit.Label(face.transform, blank ? Loc.T("blank_page") : Names.LevelTitle(id), 36, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            name.rectTransform.Band(false, 90, 14);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 24;
            name.resizeTextMaxSize = 36;

            var btn = inner.gameObject.AddComponent<Button>();
            btn.targetGraphic = face;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { Sfx.Tap(); App.I.ShowColoring(group, id); });
            inner.gameObject.AddComponent<PressFeedback>().pressedScale = 0.95f;
        }

        public bool OnBack()
        {
            App.I.ShowWelcome();
            return true;
        }

        void OnDestroy()
        {
            foreach (var t in _textures) if (t) Destroy(t);
        }
    }
}
