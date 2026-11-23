using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Çıkartma Dünyası: çocuk bir manzara seçer (hikaye kitaplarıyla aynı kodla çizilen çayır, deniz altı, kar,
    /// gece, ev…) ve 1400'den fazla emojiyi (meyve, hayvan, taşıt, insan, eşya…) sahneye yapıştırır.
    /// Dokun: ekle • Sürükle: taşı • Seçiliyken: büyüt, küçült, çevir, kaldır • Sahne dışına sürükle: kaldır.
    /// Sahne kendiliğinden saklanır (geri gelince kaldığı yerden), istenirse galeriye fotoğraf olarak kaydedilir.
    /// </summary>
    public class StickerWorldScreen : MonoBehaviour, IScreen
    {
        static readonly string[] Scenes = { "meadow", "pond", "under", "snow", "night", "home", "farm", "sunset", "sky", "garden", "forest", "home:night", "meadow:autumn", "rain" };
        static readonly (string cat, string icon)[] Tabs =
        {
            ("animal", "animal/dog"), ("fruit", "fruit/red_apple"), ("veg", "veg/carrot"), ("food", "food/pizza"),
            ("nature", "nature/sun_with_face"), ("vehicle", "vehicle/automobile"), ("people", "people/girl"),
            ("animalface", "animalface/cat_face"), ("wild", "wild/lotus"), ("play", "play/soccer_ball"),
            ("place", "place/house"), ("object", "object/backpack"), ("face", "face/grinning_face"), ("symbol", "symbol/sparkle"),
        };

        [Serializable] class SavedSticker { public string id; public float x, y, s, r; public bool f; }
        [Serializable] class SavedWorld { public int scene; public List<SavedSticker> items = new(); }

        RectTransform _root, _scene, _layer, _toolbar, _palette, _tabsContent, _hint;
        RawImage _bg;
        Texture2D _bgTex;
        int _sceneIndex, _tab;
        readonly List<Image> _tabFaces = new();
        Sticker _selected;
        bool _capturing;

        const float StickerSize = 220f;

        void Start()
        {
            _root = (RectTransform)transform;
            Emoji.Use();

            var back = UIKit.CircleButton(_root, "‹", Theme.Card, Theme.Text, 120, 90, () => OnBack());
            ((RectTransform)back.transform).Place(new Vector2(0, 1), new Vector2(110, -110), new Vector2(120, 120));

            var title = UIKit.Label(_root, Loc.T("world"), 60, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Band(true, 110, 55, 190);
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.verticalOverflow = VerticalWrapMode.Truncate;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 30;
            title.resizeTextMaxSize = 60;

            // ---- sahne ----
            var frame = UIKit.Card(_root, "Scene", Color.white, 0.6f, 8f);
            var frt = (RectTransform)frame.transform.parent;
            frt.Stretch(30, 200, 30, 590);
            _scene = UIKit.Node("Clip", frame.transform).Stretch(10, 10, 10, 10);
            _scene.gameObject.AddComponent<RectMask2D>();
            _bg = _scene.gameObject.AddComponent<RawImage>();
            _bg.color = Theme.BgMid;
            // boş yere dokununca seçim kalkar
            var bgHit = _scene.gameObject.AddComponent<Button>();
            bgHit.transition = Selectable.Transition.None;
            bgHit.onClick.AddListener(() => Select(null));
            _layer = UIKit.Node("Stickers", _scene).Stretch();

            _hint = UIKit.Node("Hint", _scene).Stretch(60, 0, 60, 0);
            var hintCard = UIKit.Card(_hint, "Card", Color.white.WithAlpha(0.85f), 0.6f, 4f);
            var hct = (RectTransform)hintCard.transform.parent;
            hct.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 130));
            var hintText = UIKit.Label(hintCard.transform, Loc.T("world_hint"), 42, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            hintText.rectTransform.Stretch(24, 8, 24, 8);
            hintText.horizontalOverflow = HorizontalWrapMode.Wrap;
            hintText.verticalOverflow = VerticalWrapMode.Truncate;
            hintText.resizeTextForBestFit = true;
            hintText.resizeTextMinSize = 24;
            hintText.resizeTextMaxSize = 42;
            hct.gameObject.AddComponent<Floaty>().Init(6f, 0.6f);
            _hint.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;

            // ---- sahnenin altında: manzara, temizle, kaydet ----
            var tools = UIKit.Node("Tools", _root);
            tools.Band(false, 120, 455, 30);
            ToolButton(tools, Emoji.Get("place/sunrise_over_mountains"), Color.white, new Vector2(0.5f, 0.5f), new Vector2(-150, 0), NextScene, Loc.T("a11y_scene"));
            ToolButton(tools, Art.Trash, Theme.TextSoft, new Vector2(0.5f, 0.5f), new Vector2(0, 0), AskClear, Loc.T("a11y_world_clear"));
            ToolButton(tools, Art.Download, Theme.AccentButton, new Vector2(0.5f, 0.5f), new Vector2(150, 0), () => StartCoroutine(SavePhoto()), Loc.T("a11y_save"));

            // ---- kategori sekmeleri + çıkartma paleti ----
            var panel = UIKit.Card(_root, "Palette", Theme.Card.WithAlpha(0.95f), 0.6f, 6f);
            ((RectTransform)panel.transform.parent).Band(false, 430, 20, 20);
            _tabsContent = HScroll((RectTransform)panel.transform, "Tabs", 120, 10, out _);
            var tabLayout = _tabsContent.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 12;
            tabLayout.padding = new RectOffset(14, 14, 6, 6);
            tabLayout.childAlignment = TextAnchor.MiddleLeft;
            tabLayout.childControlWidth = tabLayout.childControlHeight = false;
            tabLayout.childForceExpandWidth = tabLayout.childForceExpandHeight = false;
            for (int i = 0; i < Tabs.Length; i++) Tab(i);

            _palette = HScroll((RectTransform)panel.transform, "Stickers", 290, 135, out _);
            var grid = _palette.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(126, 126);
            grid.spacing = new Vector2(10, 10);
            grid.padding = new RectOffset(14, 14, 6, 6);
            grid.startAxis = GridLayoutGroup.Axis.Vertical;
            grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.MiddleLeft;

            // ---- seçili çıkartmanın araç çubuğu ----
            _toolbar = UIKit.Node("Toolbar", _root);
            _toolbar.anchorMin = _toolbar.anchorMax = new Vector2(0.5f, 0.5f);
            _toolbar.sizeDelta = new Vector2(440, 104);
            var tbBg = UIKit.Card(_toolbar, "Bg", Color.white, 0.5f, 4f);
            ((RectTransform)tbBg.transform.parent).Stretch();
            SmallButton(_toolbar, "−", 0, () => Resize(1f / 1.2f), Loc.T("a11y_smaller"));
            SmallButton(_toolbar, "+", 1, () => Resize(1.2f), Loc.T("a11y_bigger"));
            SmallButton(_toolbar, "⇆", 2, Flip, Loc.T("a11y_flip"));
            SmallButton(_toolbar, "×", 3, Remove, Loc.T("a11y_delete"));
            _toolbar.gameObject.SetActive(false);

            Load();
            ShowTab(_tab);
        }

        // ---------------- kurulum yardımcıları ----------------

        void ToolButton(Transform parent, Sprite icon, Color fg, Vector2 anchor, Vector2 pos, Action onClick, string label)
        {
            var b = UIKit.IconButton(parent, icon, Theme.Card, fg, 110, onClick, 0.62f);
            ((RectTransform)b.transform).Place(anchor, pos, new Vector2(110, 110));
            A11y.Label(b, label);
        }

        void SmallButton(RectTransform bar, string glyph, int i, Action onClick, string label)
        {
            var b = UIKit.CircleButton(bar, glyph, i == 3 ? Theme.AccentButton : Theme.Lavender, i == 3 ? Color.white : Theme.Text, 84, 56, onClick);
            ((RectTransform)b.transform).Place(new Vector2(0, 0.5f), new Vector2(62 + i * 105, 0), new Vector2(84, 84));
            A11y.Label(b, label);
        }

        /// <summary>Yatay kaydırılan şerit; içerik soldan sağa büyür.</summary>
        static RectTransform HScroll(RectTransform parent, string name, float height, float top, out ScrollRect sr)
        {
            var view = UIKit.Node(name, parent);
            view.anchorMin = new Vector2(0, 1);
            view.anchorMax = new Vector2(1, 1);
            view.pivot = new Vector2(0.5f, 1);
            view.offsetMin = new Vector2(10, -top - height);
            view.offsetMax = new Vector2(-10, -top);
            view.gameObject.AddComponent<Image>().color = Color.clear;
            view.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(24, 0);
            sr = view.gameObject.AddComponent<ScrollRect>();
            sr.vertical = false;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 40;
            var content = UIKit.Node("Content", view);
            content.anchorMin = new Vector2(0, 0);
            content.anchorMax = new Vector2(0, 1);
            content.pivot = new Vector2(0, 0.5f);
            content.offsetMin = content.offsetMax = Vector2.zero;
            content.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = view;
            sr.content = content;
            return content;
        }

        void Tab(int i)
        {
            var (cat, icon) = Tabs[i];
            var b = UIKit.IconButton(_tabsContent, Emoji.Get(icon), Theme.Card, Color.white, 104, () => ShowTab(i), 0.7f, false);
            ((RectTransform)b.transform).sizeDelta = new Vector2(104, 104);
            _tabFaces.Add(b.transform.Find("Face").GetComponent<Image>());
            A11y.Label(b, Loc.T("cat_" + cat));
        }

        void ShowTab(int i)
        {
            _tab = Mathf.Clamp(i, 0, Tabs.Length - 1);
            for (int k = 0; k < _tabFaces.Count; k++) _tabFaces[k].color = k == _tab ? Theme.Butter : Theme.Card;
            for (int k = _palette.childCount - 1; k >= 0; k--) Destroy(_palette.GetChild(k).gameObject);
            var ids = Emoji.In(Tabs[_tab].cat);
            for (int k = 0; k < ids.Count; k++)
            {
                string id = ids[k];
                var cell = UIKit.Node("Item", _palette);
                var hit = cell.gameObject.AddComponent<Image>();
                hit.color = Color.clear;
                var img = Emoji.Image(cell, id, 112);
                img.raycastTarget = false;
                img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                var b = cell.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => Add(id));
                cell.gameObject.AddComponent<PressFeedback>();
            }
            ((RectTransform)_palette.parent).GetComponent<ScrollRect>().horizontalNormalizedPosition = 0f;
        }

        // ---------------- manzara ----------------

        void NextScene()
        {
            _sceneIndex = (_sceneIndex + 1) % Scenes.Length;
            Haptics.Light();
            PaintScene();
            Save();
        }

        void PaintScene()
        {
            var r = _scene.rect;
            if (r.width < 10) r = new Rect(0, 0, 1000, 1100);
            int w = 900, h = Mathf.Clamp(Mathf.RoundToInt(w * r.height / r.width), 500, 1600);
            int index = _sceneIndex;
            StoryArt.PaintAsync(Scenes[index], w, h, this, tex =>
            {
                if (index != _sceneIndex) { Destroy(tex); return; }   // bu arada başka manzara seçildi
                if (_bgTex != null) Destroy(_bgTex);
                _bgTex = tex;
                _bg.texture = tex;
                _bg.color = Color.white;
                Tween.To(_bg, 0.3f, k => _bg.color = Color.Lerp(new Color(1, 1, 1, 0.6f), Color.white, k));
            });
        }

        // ---------------- çıkartmalar ----------------

        Sticker Add(string id, float x, float y, float scale, float rot, bool flip, bool pop)
        {
            var sp = Emoji.Get(id);
            if (sp == null) return null;
            var rt = UIKit.Node("Sticker", _layer);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(StickerSize, StickerSize);
            rt.anchoredPosition = new Vector2(x, y);
            rt.localRotation = Quaternion.Euler(0, 0, rot);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sp;
            img.preserveAspect = true;
            var s = rt.gameObject.AddComponent<Sticker>();
            s.Owner = this;
            s.Id = id;
            s.Scale = scale;
            s.Flip = flip;
            s.Apply();
            if (pop && !Motion.Reduced)
            {
                rt.localScale = Vector3.zero;
                Tween.To(rt, 0.4f, k => { if (s != null) rt.localScale = new Vector3(s.Flip ? -1 : 1, 1, 1) * s.Scale * Tween.OutBack(k); }, Tween.Linear);
            }
            UpdateHint();
            return s;
        }

        void Add(string id)
        {
            var r = _scene.rect;
            float x = UnityEngine.Random.Range(-r.width * 0.25f, r.width * 0.25f);
            float y = UnityEngine.Random.Range(-r.height * 0.15f, r.height * 0.15f);
            var s = Add(id, x, y, 1f, UnityEngine.Random.Range(-8f, 8f), false, true);
            if (s == null) return;
            Sfx.Pop();
            Haptics.Light();
            Select(s);
            Save();
        }

        internal void Select(Sticker s)
        {
            if (_selected != null) _selected.SetSelected(false);
            _selected = s;
            if (s != null)
            {
                s.SetSelected(true);
                s.transform.SetAsLastSibling();
            }
            _toolbar.gameObject.SetActive(s != null && !_capturing);
            if (s != null) Tween.Pop(_toolbar, 0f, 0.25f);
        }

        void Resize(float k)
        {
            if (_selected == null) return;
            _selected.Scale = Mathf.Clamp(_selected.Scale * k, 0.35f, 3.2f);
            _selected.Apply();
            Sfx.Tap();
            Save();
        }

        void Flip()
        {
            if (_selected == null) return;
            _selected.Flip = !_selected.Flip;
            _selected.Apply();
            Sfx.Tap();
            Save();
        }

        void Remove()
        {
            if (_selected == null) return;
            var s = _selected;
            Select(null);
            Poof(s);
        }

        /// <summary>Çıkartma küçülüp kaybolur.</summary>
        internal void Poof(Sticker s)
        {
            if (s == null) return;
            if (_selected == s) Select(null);
            var rt = (RectTransform)s.transform;
            var start = rt.localScale;
            Sfx.Undo();
            s.enabled = false;
            Tween.To(rt, 0.22f, k => rt.localScale = start * (1f - k), Tween.Linear, 0f, () =>
            {
                if (rt != null) Destroy(rt.gameObject);
                UpdateHint();
                Save();
            });
        }

        /// <summary>Çıkartma sahnenin dışına bırakıldı mı (ortası sahne dışında)?</summary>
        internal bool Outside(RectTransform rt)
        {
            var r = _scene.rect;
            var p = rt.anchoredPosition;
            return Mathf.Abs(p.x) > r.width * 0.5f + 20f || Mathf.Abs(p.y) > r.height * 0.5f + 20f;
        }

        void AskClear()
        {
            if (_layer.childCount == 0) return;
            Popup.Confirm(Loc.T("world_clear_q"), Loc.T("world_clear_msg"), Loc.T("clear_yes"), Loc.T("clear_no"), true, () =>
            {
                Select(null);
                for (int i = _layer.childCount - 1; i >= 0; i--) Destroy(_layer.GetChild(i).gameObject);
                Tween.Delay(0.05f, () => { if (this != null) { UpdateHint(); Save(); } });
            });
        }

        void UpdateHint()
        {
            if (_hint == null) return;
            int live = 0;
            for (int i = 0; i < _layer.childCount; i++) if (_layer.GetChild(i).TryGetComponent<Sticker>(out var s) && s.enabled) live++;
            _hint.gameObject.SetActive(live == 0);
        }

        void LateUpdate()
        {
            if (_selected == null || !_toolbar.gameObject.activeSelf) return;
            // araç çubuğu seçili çıkartmanın üstünde durur, sahnenin dışına taşmaz
            var rt = (RectTransform)_selected.transform;
            Vector2 above = _root.InverseTransformPoint(rt.TransformPoint(new Vector3(0, StickerSize * 0.5f, 0)));
            Vector2 below = _root.InverseTransformPoint(rt.TransformPoint(new Vector3(0, -StickerSize * 0.5f, 0)));
            Vector2 sceneTop = _root.InverseTransformPoint(_scene.TransformPoint(new Vector3(0, _scene.rect.yMax, 0)));
            // üstte yer yoksa altına geçer
            float y = Mathf.Max(above.y, below.y) + 70f;
            if (y > sceneTop.y - 50f) y = Mathf.Min(above.y, below.y) - 70f;
            var area = _root.rect;
            float x = Mathf.Clamp(above.x, area.xMin + 240f, area.xMax - 240f);
            _toolbar.anchoredPosition = new Vector2(x, y);
        }

        // ---------------- kaydet / yükle ----------------

        const string Key = "sticker_world";

        void Save()
        {
            if (_layer == null) return;
            var w = new SavedWorld { scene = _sceneIndex };
            for (int i = 0; i < _layer.childCount; i++)
            {
                if (!_layer.GetChild(i).TryGetComponent<Sticker>(out var s) || !s.enabled) continue;
                var rt = (RectTransform)s.transform;
                w.items.Add(new SavedSticker { id = s.Id, x = rt.anchoredPosition.x, y = rt.anchoredPosition.y, s = s.Scale, r = rt.localEulerAngles.z, f = s.Flip });
            }
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(w));
        }

        void Load()
        {
            var json = PlayerPrefs.GetString(Key, "");
            SavedWorld w = null;
            if (!string.IsNullOrEmpty(json))
            {
                try { w = JsonUtility.FromJson<SavedWorld>(json); }
                catch (Exception) { w = null; }
            }
            _sceneIndex = w != null ? Mathf.Clamp(w.scene, 0, Scenes.Length - 1) : 0;
            PaintScene();
            if (w != null)
                foreach (var it in w.items) Add(it.id, it.x, it.y, it.s, it.r, it.f, false);
            UpdateHint();
        }

        /// <summary>Sahnenin fotoğrafı galeriye (seçim çerçevesi ve araç çubuğu görünmeden).</summary>
        IEnumerator SavePhoto()
        {
            if (_capturing) yield break;
            _capturing = true;
            var keep = _selected;
            Select(null);
            _hint.gameObject.SetActive(false);
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            var corners = new Vector3[4];
            _scene.GetWorldCorners(corners);
            Vector2 a = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 c = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            int x0 = Mathf.Clamp(Mathf.RoundToInt(a.x), 0, shot.width - 1), y0 = Mathf.Clamp(Mathf.RoundToInt(a.y), 0, shot.height - 1);
            int x1 = Mathf.Clamp(Mathf.RoundToInt(c.x), x0 + 1, shot.width), y1 = Mathf.Clamp(Mathf.RoundToInt(c.y), y0 + 1, shot.height);
            var crop = new Texture2D(x1 - x0, y1 - y0, TextureFormat.RGB24, false);
            crop.SetPixels(shot.GetPixels(x0, y0, x1 - x0, y1 - y0));
            crop.Apply();
            Destroy(shot);
            string msg = PhotoSaver.Save(crop, "sahne");
            Destroy(crop);
            _capturing = false;
            UpdateHint();
            if (keep != null) Select(keep);
            Popup.Toast(msg);
            Sfx.Star();
            Haptics.Medium();
        }

        public bool OnBack()
        {
            App.I.ShowWelcome();
            return true;
        }

        void OnDestroy()
        {
            Save();
            PlayerPrefs.Save();
            if (_bgTex != null) Destroy(_bgTex);
            Emoji.Release();
        }

        /// <summary>Sahnedeki tek çıkartma: dokununca seçilir, sürükleyince taşınır.</summary>
        internal class Sticker : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
        {
            public StickerWorldScreen Owner;
            public string Id;
            public float Scale = 1f;
            public bool Flip;
            Image _ring;
            Vector2 _grab;

            public void Apply() => transform.localScale = new Vector3(Flip ? -Scale : Scale, Scale, 1f);

            public void SetSelected(bool on)
            {
                if (on && _ring == null)
                {
                    _ring = UIKit.Box(transform, "Ring", Theme.AccentButton.WithAlpha(0.8f), UIKit.Ring);
                    _ring.type = Image.Type.Simple;
                    _ring.rectTransform.Stretch(-16, -16, -16, -16);
                    _ring.transform.SetAsFirstSibling();
                }
                if (_ring != null) _ring.gameObject.SetActive(on);
            }

            public void OnPointerDown(PointerEventData e)
            {
                if (!enabled) return;
                Owner.Select(this);
            }

            public void OnPointerClick(PointerEventData e) { }

            public void OnBeginDrag(PointerEventData e)
            {
                if (!enabled) return;
                var rt = (RectTransform)transform;
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rt.parent, e.position, e.pressEventCamera, out var p);
                _grab = rt.anchoredPosition - p;
                if (!Motion.Reduced) transform.localScale = new Vector3(Flip ? -Scale : Scale, Scale, 1f) * 1.08f;
            }

            public void OnDrag(PointerEventData e)
            {
                if (!enabled) return;
                var rt = (RectTransform)transform;
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rt.parent, e.position, e.pressEventCamera, out var p);
                rt.anchoredPosition = p + _grab;
            }

            public void OnEndDrag(PointerEventData e)
            {
                if (!enabled) return;
                Apply();
                if (Owner.Outside((RectTransform)transform)) { Owner.Poof(this); return; }
                Sfx.Tap();
                Owner.Save();
            }
        }
    }
}
