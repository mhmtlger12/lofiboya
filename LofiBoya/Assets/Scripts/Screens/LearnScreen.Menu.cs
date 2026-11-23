using System;
using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Öğren menüsü. Görseller Google Stitch tasarımından kesildi (tools/ogren/ogren_kes.py → Resources/LearnMenu):
    /// üstte gök + el sallayan kalem + tahta tabela, ortada pofuduk kartlar, altta tepeler + kedi + levha.
    /// Resimlerdeki yazılar silindi; başlıklar oyunun kendi yazısıyla, seçili dilde yazılır.
    /// Tasarımda 8 kart var (Renkler … Örüntü); diğer oyunlar Şekiller kartından boşaltılıp başka renge boyanmış
    /// kartları kullanır ve süslerini kod çizer. Yerleşim, tasarımdaki ölçülerle (layout.json) birebir.
    /// </summary>
    public partial class LearnScreen
    {
        [Serializable] class MenuCardInfo { public string file; public int[] box; public int[] text; }
        [Serializable] class MenuHeader { public int[] size; public int[] title; public int[] sub; }
        [Serializable] class MenuFooter { public int[] size; public int[] text; }
        [Serializable] class MenuBlank { public int[] box; public int[] text; public int[] art; public string[] colors; }
        [Serializable] class MenuLayout { public int[] src; public MenuCardInfo[] cards; public MenuHeader header; public MenuFooter footer; public MenuBlank blank; }

        static MenuLayout _menuLayout;

        static MenuLayout Layout
        {
            get
            {
                if (_menuLayout != null) return _menuLayout;
                var ta = Resources.Load<TextAsset>("LearnMenu/layout");
                _menuLayout = ta != null ? JsonUtility.FromJson<MenuLayout>(ta.text) : new MenuLayout();
                if (ta != null) Resources.UnloadAsset(ta);
                return _menuLayout;
            }
        }

        /// <summary>Tasarım pikselinden ekran birimine (tasarım 873 px geniş, ekran 1080).</summary>
        static float S => 1080f / Layout.src[0];

        static int MenuCards => Emoji.Available ? 15 : 13;
        const float MenuOverlap = 40f;   // kartlar alt sahnenin üst kenarının biraz arkasından kayar
        static readonly Color MenuTitle = Theme.Hex("4B2A6E");
        static readonly Color MenuBg = Theme.Hex("EFEAFC");
        RectTransform _menuGrid;

        void ShowMenu()
        {
            _mode = Mode.Menu;
            ClearArea();
            if (_mascot != null) _mascot.gameObject.SetActive(false);   // menüde kartların önünü kapatmasın
            if (Layout.cards == null || Layout.cards.Length == 0) { Debug.LogError("LearnMenu/layout.json yok"); return; }

            var bg = UIKit.Box(_area, "MenuBg", MenuBg, null);
            bg.type = Image.Type.Simple;
            bg.sprite = null;
            bg.rectTransform.Stretch(-UIKit.Bleed, -400, -UIKit.Bleed, -400);

            float headerH = Layout.header.size[1] * S, footerH = Layout.footer.size[1] * S;
            float top = Layout.cards[0].box[1] * S - 6f;

            // kartlar dikey kaydırılan alanda
            var view = UIKit.Node("MenuScroll", _area).Stretch(0, top, 0, footerH - MenuOverlap);
            view.gameObject.AddComponent<Image>().color = Color.clear;
            view.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(0, 24);
            var sr = view.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 40;
            _menuGrid = UIKit.Node("Grid", view);
            _menuGrid.anchorMin = new Vector2(0, 1);
            _menuGrid.anchorMax = new Vector2(1, 1);
            _menuGrid.pivot = new Vector2(0.5f, 1);
            _menuGrid.offsetMin = _menuGrid.offsetMax = Vector2.zero;
            sr.viewport = view;
            sr.content = _menuGrid;

            MenuCard(0, Loc.T("colors"), Loc.T("colors_sub"), 0.1f, () => StartGame(Mode.Colors));
            MenuCard(1, Loc.T("pictures"), Loc.T("pictures_sub"), 0.18f, () => StartGame(Mode.Animals));
            MenuCard(2, Loc.T("shapes"), Loc.T("shapes_sub"), 0.26f, () => StartGame(Mode.Shapes));
            MenuCard(3, Loc.T("counting"), Loc.T("counting_sub"), 0.34f, () => StartGame(Mode.Count));
            MenuCard(4, Loc.T("trace"), Loc.T("trace_sub"), 0.42f, () => StartGame(Mode.Trace));
            MenuCard(6, Loc.T("feelings_game"), Loc.T("feelings_sub"), 0.58f, () => StartGame(Mode.Feelings));
            MoreMenuCards();
            ConceptMenuCards();
            EmojiMenuCards();
            _menuGrid.sizeDelta = new Vector2(0, MenuGridHeight() + MenuOverlap + 20f);

            MenuHeaderArt(headerH);
            MenuFooterArt(footerH);
            if (_back != null) _back.SetAsLastSibling();   // üst resmin önünde kalsın
        }

        // ---------------- yerleşim ----------------

        int[] CardBox(int i) => i < Layout.cards.Length ? Layout.cards[i].box : Layout.blank.box;

        /// <summary>Kartın tasarımdaki kutusu ızgara içinde (ortası, üst kenarı): ilk 8 kart tasarımdaki yerinde,
        /// sonrakiler aynı iki sütunda alta eklenir; tek kalan son kart ortalanır.</summary>
        Vector2 CardTopCenter(int i, out Vector2 size)
        {
            float half = Layout.src[0] * 0.5f, y0 = Layout.cards[0].box[1];
            var b = CardBox(i);
            size = new Vector2(b[2] - b[0], b[3] - b[1]) * S;
            if (i < Layout.cards.Length)
                return new Vector2(((b[0] + b[2]) * 0.5f - half) * S, -(b[1] - y0) * S);
            int n = Layout.cards.Length, j = i - n;
            var left = Layout.cards[n - 2].box;
            var right = Layout.cards[n - 1].box;
            float rowTop = Mathf.Max(left[3], right[3]) - y0 + (j / 2) * (b[3] - b[1]);
            bool lone = i == MenuCards - 1 && j % 2 == 0;
            var col = j % 2 == 0 ? left : right;
            float x = lone ? 0f : ((col[0] + col[2]) * 0.5f - half) * S;
            return new Vector2(x, -rowTop * S);
        }

        float MenuGridHeight()
        {
            CardTopCenter(MenuCards - 1, out var size);
            return -CardTopCenter(MenuCards - 1, out _).y + size.y;
        }

        static void SetBox(RectTransform r, float l, float t, float rr, float b)
        {
            r.anchorMin = r.anchorMax = new Vector2(0, 1);
            r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(l, -t);
            r.sizeDelta = new Vector2(rr - l, b - t);
        }

        static Text FitLabel(RectTransform parent, string text, int max, int min, Color col, FontStyle style)
        {
            var t = UIKit.Label(parent, text, max, col, TextAnchor.MiddleCenter, style);
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = min;
            t.resizeTextMaxSize = max;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;   // best fit ancak taşma kapalıyken küçültür
            return t;
        }

        /// <summary>
        /// Kart: tasarımdan kesilmiş resim + bulutun üstüne başlık ve alt yazı. Tasarımdaki 8 kartın süsü resmin içinde
        /// (null döner); diğerleri için süslerin çizileceği kutu döner (eski 292×316 kart ölçüsünde, büyütülmüş).
        /// </summary>
        RectTransform MenuCard(int i, string title, string sub, float delay, Action onClick)
        {
            bool baked = i < Layout.cards.Length;
            var box = CardBox(i);
            var txt = baked ? Layout.cards[i].text : Layout.blank.text;
            string file = baked ? Layout.cards[i].file
                : "card_blank_" + Layout.blank.colors[(i - Layout.cards.Length) % Layout.blank.colors.Length];

            var b = UIKit.Button(_menuGrid, "", Color.white, MenuTitle, 10, onClick, 1f, true);
            var rt = (RectTransform)b.transform;
            var topCenter = CardTopCenter(i, out var size);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);   // basınca ortasından küçülsün
            rt.sizeDelta = size;
            rt.anchoredPosition = topCenter - new Vector2(0, size.y * 0.5f);
            var face = b.transform.Find("Face").GetComponent<Image>();
            face.sprite = Resources.Load<Sprite>("LearnMenu/" + file);
            face.type = Image.Type.Simple;
            face.color = Color.white;
            ((RectTransform)b.transform.Find("Shadow")).Stretch(4, 8, 4, -14);   // yumuşak gölge (resimdeki gölge saydamlaştırıldı)
            var frt = face.rectTransform;
            A11y.Label(b, title);

            // bulut üstündeki yazı alanı: tasarımdaki yazının yeri, iki yana biraz genişletilmiş (uzun diller sığsın)
            float w = box[2] - box[0];
            float l = Mathf.Min(txt[0] - box[0], 52) * S, r = Mathf.Max(txt[2] - box[0], w - 92) * S;
            float t0 = (txt[1] - box[1]) * S, t1 = (txt[3] - box[1]) * S, mid = Mathf.Lerp(t0, t1, 0.58f);
            var tl = FitLabel(frt, title, 60, 30, MenuTitle, FontStyle.Bold);
            SetBox(tl.rectTransform, l, t0 - 6, r, mid + 2);
            var sl = FitLabel(frt, sub, 34, 20, MenuTitle, FontStyle.Normal);
            sl.lineSpacing = 0.85f;
            SetBox(sl.rectTransform, l, mid, r, t1 + 6);

            Tween.Pop(rt, delay, 0.5f);
            if (baked) return null;

            var a = Layout.blank.art;
            const float k = 1.55f;
            var art = UIKit.Node("Art", frt);
            art.anchorMin = art.anchorMax = new Vector2(0, 1);
            art.pivot = new Vector2(0.5f, 1f);
            art.sizeDelta = new Vector2(292, 316);
            art.localScale = Vector3.one * k;
            float cx = ((a[0] + a[2]) * 0.5f - box[0]) * S, cy = ((a[1] + a[3]) * 0.5f - box[1]) * S;
            art.anchoredPosition = new Vector2(cx, -cy + 316 * 0.2f * k);   // eski kartta süs çizgisi üstten %20
            tl.transform.SetAsLastSibling();
            sl.transform.SetAsLastSibling();
            return art;
        }

        // ---------------- üst ve alt sahne ----------------

        void MenuHeaderArt(float h)
        {
            // geniş ekranda (tablet) resmin iki yanı: gök rengi
            var side = UIKit.Node("Sky", _area);
            side.anchorMin = new Vector2(0, 1);
            side.anchorMax = new Vector2(1, 1);
            side.pivot = new Vector2(0.5f, 1);
            side.offsetMin = new Vector2(-UIKit.Bleed, -h);
            side.offsetMax = new Vector2(UIKit.Bleed, 400);
            var raw = side.gameObject.AddComponent<RawImage>();
            if (_skyTex == null) _skyTex = UIKit.Gradient(Theme.Hex("56B9FC"), Theme.Hex("7FD0FC"), Theme.Hex("D8EEFD"), MenuBg);
            raw.texture = _skyTex;
            raw.raycastTarget = false;

            var img = UIKit.Box(_area, "Header", Color.white, Resources.Load<Sprite>("LearnMenu/header"));
            img.type = Image.Type.Simple;
            var r = img.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(Layout.header.size[0] * S, h);

            // tabelada kısa, zıplayan bir yazı ("Oyun zamanı!")
            var tb = Layout.header.title;
            var holder = UIKit.Node("Fun", r);
            SetBox(holder, tb[0] * S - 50, tb[1] * S + 10, tb[2] * S + 50, tb[3] * S - 6);
            var fun = FitLabel(holder, Loc.T("learn_fun"), 92, 48, Theme.Hex("7A3DB8"), FontStyle.Bold);
            fun.rectTransform.Stretch();
            fun.gameObject.AddComponent<LetterWave>();   // önce dalga: gölge ve kontur harfle birlikte zıplasın
            var ol = fun.gameObject.AddComponent<Outline>();   // ince krem kenar: tahtadan ayrılsın, harfleri birleştirmesin
            ol.effectColor = Theme.Hex("FFF6E6").WithAlpha(0.9f);
            ol.effectDistance = new Vector2(2f, -2f);
            var sh = fun.gameObject.AddComponent<Shadow>();
            sh.effectColor = Theme.Hex("5A3418").WithAlpha(0.45f);
            sh.effectDistance = new Vector2(0, -5);
            Tween.Pop(holder, 0.25f, 0.6f);

            var sb = Layout.header.sub;
            var sub = FitLabel(r, Names.Cap(Loc.T("learn_sub")), 42, 24, Theme.Hex("4A2C4A"), FontStyle.Bold);
            SetBox(sub.rectTransform, sb[0] * S, sb[1] * S, sb[2] * S, sb[3] * S + 4);
        }

        void MenuFooterArt(float h)
        {
            var side = UIKit.Node("Grass", _area);
            side.anchorMin = new Vector2(0, 0);
            side.anchorMax = new Vector2(1, 0);
            side.pivot = new Vector2(0.5f, 0);
            side.offsetMin = new Vector2(-UIKit.Bleed, -400);
            side.offsetMax = new Vector2(UIKit.Bleed, h * 0.55f);
            var raw = side.gameObject.AddComponent<RawImage>();
            if (_grassTex == null) _grassTex = UIKit.Gradient(new Color(MenuBg.r, MenuBg.g, MenuBg.b, 0f), Theme.Hex("8CCB6E"), Theme.Hex("59AE2F"), Theme.Hex("59AE2F"));
            raw.texture = _grassTex;
            raw.raycastTarget = false;

            var img = UIKit.Box(_area, "Footer", Color.white, Resources.Load<Sprite>("LearnMenu/footer"));
            img.type = Image.Type.Simple;
            var r = img.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
            r.pivot = new Vector2(0.5f, 0f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(Layout.footer.size[0] * S, h);

            var fb = Layout.footer.text;
            var cheer = FitLabel(r, Loc.T("learn_cheer"), 54, 28, MenuTitle, FontStyle.Bold);
            SetBox(cheer.rectTransform, fb[0] * S, fb[1] * S, fb[2] * S, fb[3] * S);
            cheer.rectTransform.localRotation = Quaternion.Euler(0, 0, 3f);   // levha hafif eğik
        }

        Texture2D _skyTex, _grassTex;
    }
}
