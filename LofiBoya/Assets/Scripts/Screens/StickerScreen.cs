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

        void Start()
        {
            var root = (RectTransform)transform;

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

            // Kaydırılabilir ızgara
            var scrollRt = UIKit.Node("Scroll", root).Stretch(30, 390, 30, 30);
            var sr = scrollRt.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 40;
            var vp = UIKit.Node("Viewport", scrollRt).Stretch();
            vp.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(0, 30);
            vp.gameObject.AddComponent<Image>().color = Color.clear;
            sr.viewport = vp;
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
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = content;

            for (int i = 0; i < all.Count; i++)
            {
                var e = all[i];
                Sticker(content, e.g, e.id, e.done, i);
            }
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

            Texture2D tex;
            if (done)
            {
                // beyaz "kesim" kenarı + gölge
                var face = UIKit.Card(holder, "Paper", Color.white, 0.45f, 6f);
                ((RectTransform)face.transform.parent).Stretch();
                tex = SaveSystem.LoadThumb(group + "_" + id) ?? Level.LoadThumb(group, id);
            }
            else
            {
                var face = UIKit.Box(holder, "Slot", Theme.Text.WithAlpha(0.06f), null, 0.45f);
                face.rectTransform.Stretch();
                tex = Level.LoadThumb(group, id);
            }
            if (tex != null)
            {
                _textures.Add(tex);
                var photo = UIKit.Node("Photo", holder).Stretch(18, 18, 18, 18);
                var raw = photo.gameObject.AddComponent<RawImage>();
                raw.texture = tex;
                raw.raycastTarget = false;
                raw.color = done ? Color.white : new Color(1, 1, 1, 0.28f);
                var fit = photo.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fit.aspectRatio = (float)tex.width / tex.height;
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
            holder.localScale = Vector3.zero;
            Tween.Pop(holder, 0.05f + Mathf.Min(index, 15) * 0.03f, 0.45f);
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
        }
    }
}
