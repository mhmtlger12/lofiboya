using System;
using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>Kod ile yumuşak, yuvarlak köşeli arayüz parçaları üretir. Harici görsel gerekmez.</summary>
    public static class UIKit
    {
        static Sprite _rounded, _circle, _shadow, _ring, _blob;

        /// <summary>9-dilimli yuvarlak köşeli kutu (köşe yarıçapı 32 px).</summary>
        public static Sprite Rounded => _rounded != null ? _rounded : (_rounded = MakeRounded(96, 32, 0f));
        /// <summary>Yumuşak gölge, kenarı bulanık yuvarlak kutu.</summary>
        public static Sprite Shadow => _shadow != null ? _shadow : (_shadow = MakeRounded(160, 48, 22f));
        public static Sprite Circle => _circle != null ? _circle : (_circle = MakeCircle(128, 0f, 0f));
        public static Sprite Ring => _ring != null ? _ring : (_ring = MakeCircle(128, 10f, 0f));
        /// <summary>Kenarları çok yumuşak daire (arka plan baloncukları).</summary>
        public static Sprite Blob => _blob != null ? _blob : (_blob = MakeCircle(128, 0f, 40f));

        static Sprite MakeRounded(int size, int radius, float blur)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            float inner = half - radius - blur;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - half) - inner;
                float qy = Mathf.Abs(y + 0.5f - half) - inner;
                float outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0);
                float d = outside - radius;
                float a = blur > 0 ? 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-blur, blur, d)) : Mathf.Clamp01(0.5f - d);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            float b = radius + blur + 2;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        static Sprite MakeCircle(int size, float ring, float blur)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            float r = size * 0.5f - 1f - blur;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = new Vector2(x + 0.5f - size * 0.5f, y + 0.5f - size * 0.5f).magnitude - r;
                float a;
                if (blur > 0) a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-blur, blur, d));
                else if (ring > 0) a = Mathf.Clamp01(0.5f - d) * Mathf.Clamp01(d + ring + 0.5f);
                else a = Mathf.Clamp01(0.5f - d);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }

        /// <summary>Dikey yumuşak geçişli arka plan dokusu.</summary>
        public static Texture2D Gradient(params Color[] stops)
        {
            const int h = 256;
            var tex = new Texture2D(1, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < h; y++)
            {
                float t = 1f - y / (h - 1f); // üstten alta
                float f = t * (stops.Length - 1);
                int i = Mathf.Min((int)f, stops.Length - 2);
                tex.SetPixel(0, y, Color.Lerp(stops[i], stops[i + 1], Mathf.SmoothStep(0, 1, f - i)));
            }
            tex.Apply(false, true);
            return tex;
        }

        // ---------- Yerleşim yardımcıları ----------

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Tek bir noktaya bağla: anchor (0..1), konum ve boyut.</summary>
        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>Üstte/altta yatay şerit.</summary>
        public static RectTransform Band(this RectTransform rt, bool top, float height, float offset = 0, float side = 0)
        {
            rt.anchorMin = new Vector2(0, top ? 1 : 0);
            rt.anchorMax = new Vector2(1, top ? 1 : 0);
            rt.pivot = new Vector2(0.5f, top ? 1 : 0);
            rt.offsetMin = new Vector2(side, 0);
            rt.offsetMax = new Vector2(-side, 0);
            rt.sizeDelta = new Vector2(-2 * side, height);
            rt.anchoredPosition = new Vector2(0, top ? -offset : offset);
            return rt;
        }

        // ---------- Bileşenler ----------

        public static Image Box(Transform parent, string name, Color color, Sprite sprite = null, float corner = 1f)
        {
            var rt = Node(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite != null ? sprite : Rounded;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = corner; // büyük değer = daha küçük köşe
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Gölgeli kart: önce yumuşak gölge, üstünde kart.</summary>
        public static Image Card(Transform parent, string name, Color color, float corner = 1f, float shadowOffset = 10f)
        {
            var holder = Node(name, parent);
            var sh = Box(holder, "Shadow", Theme.Shadow, Shadow, corner);
            sh.rectTransform.Stretch(-14, -10 + shadowOffset, -14, -18 - shadowOffset);
            var card = Box(holder, "Face", color, Rounded, corner);
            card.rectTransform.Stretch();
            return card;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var rt = Node("Text", parent);
            var t = rt.gameObject.AddComponent<LocText>(); // dile göre şekillenen metin
            t.font = Theme.Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        /// <summary>Basınca hafifçe küçülen, bırakınca zıplayan yuvarlak buton.</summary>
        public static Button Button(Transform parent, string label, Color bg, Color fg, int fontSize, Action onClick, float corner = 1f, bool shadow = true)
        {
            var holder = Node("Btn " + label, parent);
            if (shadow)
            {
                var sh = Box(holder, "Shadow", Theme.Shadow, Shadow, corner);
                sh.rectTransform.Stretch(-12, -4, -12, -20);
            }
            var face = Box(holder, "Face", bg, Rounded, corner);
            face.rectTransform.Stretch();
            face.raycastTarget = true;
            var txt = Label(face.transform, label, fontSize, fg, TextAnchor.MiddleCenter, FontStyle.Bold);
            txt.rectTransform.Stretch();
            var btn = holder.gameObject.AddComponent<Button>();
            btn.targetGraphic = face;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { Sfx.Tap(); onClick?.Invoke(); });
            holder.gameObject.AddComponent<PressFeedback>();
            return btn;
        }

        public static Button CircleButton(Transform parent, string glyph, Color bg, Color fg, float size, int fontSize, Action onClick)
        {
            var b = Button(parent, glyph, bg, fg, fontSize, onClick, 1f, true);
            var face = b.transform.Find("Face").GetComponent<Image>();
            face.sprite = Circle;
            face.type = Image.Type.Simple;
            var sh = b.transform.Find("Shadow").GetComponent<Image>();
            sh.sprite = Blob;
            sh.type = Image.Type.Simple;
            sh.rectTransform.Stretch(-10, -2, -10, -16);
            ((RectTransform)b.transform).sizeDelta = new Vector2(size, size);
            return b;
        }

        public static Text ButtonText(Button b) => b.GetComponentInChildren<Text>();

        /// <summary>Ortasında çizilmiş ikon olan yuvarlak buton.</summary>
        public static Button IconButton(Transform parent, Sprite icon, Color bg, Color fg, float size, Action onClick, float iconScale = 0.55f, bool shadow = true)
        {
            var b = CircleButton(parent, "", bg, fg, size, 10, onClick);
            if (!shadow) b.transform.Find("Shadow").gameObject.SetActive(false);
            var face = b.transform.Find("Face");
            var img = Box(face, "Icon", fg, icon);
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            float s = size * iconScale;
            img.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(s, s));
            return b;
        }

        public static Image ButtonIcon(Button b) => b.transform.Find("Face/Icon")?.GetComponent<Image>();
    }
}
