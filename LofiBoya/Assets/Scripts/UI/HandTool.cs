using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// "Elimde kalem": kalem ya da silgi seçiliyken parmağın/farenin ucunda duran, Pofuduk'un
    /// minik eliyle tutulan büyük boya kalemi. Çizerken hafifçe bastırır ve harekete göre eğilir.
    /// Kova seçiliyken dokunulan yere küçük bir kova dökülür.
    /// </summary>
    [DefaultExecutionOrder(100)] // tuval girdiyi okuduktan sonra çalış: aynı karede
    public class HandTool : MonoBehaviour
    {
        ColoringCanvas _canvas;
        RectTransform _root, _crayon, _eraser, _shadow;
        Image _body, _shadowImg;
        CanvasGroup _group;
        Vector2 _pos, _vel, _lastScreen, _hopOffset;
        float _alpha, _tilt, _press;
        bool _placed, _pendingEquip, _pendingAnimate;
        Coroutine _equipCo;

        const float Len = 230f, Wid = 58f, BaseAngle = 140f;
        /// <summary>Kalem sprite'ında ucun gerçek yüksekliği (Art.CrayonSdf: 3.725 / 4.0).</summary>
        const float TipV = 0.93f;

        public static HandTool Create(Transform parent, ColoringCanvas canvas)
        {
            var rt = UIKit.Node("HandTool", parent).Stretch();
            var h = rt.gameObject.AddComponent<HandTool>();
            UIKit.SubCanvas(h, false);   // kalem her kare hareket eder: yalnızca kendi katmanı yeniden kurulur
            h._canvas = canvas;
            h.Build();
            return h;
        }

        void Build()
        {
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _group.alpha = 0;

            _root = UIKit.Node("Pivot", transform);
            _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.sizeDelta = Vector2.zero;

            // Yere düşen yumuşak gölge (kâğıttan biraz yukarıda tutuluyor hissi)
            _shadow = UIKit.Node("Shadow", _root);
            _shadow.anchorMin = _shadow.anchorMax = new Vector2(0.5f, 0.5f);
            _shadow.pivot = new Vector2(0.5f, TipV);
            _shadow.sizeDelta = new Vector2(Wid, Len);
            _shadowImg = _shadow.gameObject.AddComponent<Image>();
            _shadowImg.sprite = Art.CrayonBody;
            _shadowImg.color = Theme.Text.WithAlpha(0.16f);
            _shadowImg.raycastTarget = false;

            // Kalem: ucu pivotta, gövdesi sağ-yukarı uzanır
            _crayon = UIKit.Node("Crayon", _root);
            _crayon.anchorMin = _crayon.anchorMax = new Vector2(0.5f, 0.5f);
            _crayon.pivot = new Vector2(0.5f, TipV);
            _crayon.sizeDelta = new Vector2(Wid, Len);
            _body = UIKit.Box(_crayon, "Body", Color.white, Art.CrayonBody);
            _body.type = Image.Type.Simple;
            _body.rectTransform.Stretch();
            var shine = UIKit.Box(_crayon, "Shine", Color.white, Art.CrayonShine);
            shine.type = Image.Type.Simple;
            shine.rectTransform.Stretch();
            // tutan el: Pofuduk'un pamuk eli, gerçek tutuş gibi uca yakın
            AddHand(_crayon, new Vector2(0.5f, 0.6f), 80f);

            // Silgi: pembe, yumuşak köşeli
            _eraser = UIKit.Node("Eraser", _root);
            _eraser.anchorMin = _eraser.anchorMax = new Vector2(0.5f, 0.5f);
            _eraser.pivot = new Vector2(0.24f, 0.2f);
            _eraser.sizeDelta = new Vector2(190, 190);
            var er = UIKit.Box(_eraser, "Body", Theme.Hex("FFB7C5"), Art.Eraser);
            er.type = Image.Type.Simple;
            er.rectTransform.Stretch();
            var band = UIKit.Box(_eraser, "Band", Theme.Sky, Art.Eraser);
            band.type = Image.Type.Filled;
            band.fillMethod = Image.FillMethod.Vertical;
            band.fillOrigin = 1;
            band.fillAmount = 0.45f;
            band.rectTransform.Stretch();
            AddHand(_eraser, new Vector2(0.66f, 0.66f), 110f);
        }

        void OnDisable()
        {
            Cursor.visible = true;
#if UNITY_EDITOR
            ColoringCanvas.EditorHideCursor?.Invoke(false);
#endif
        }

        static void AddHand(RectTransform parent, Vector2 anchor, float size)
        {
            var hand = UIKit.Box(parent, "Hand", Color.white, MascotArt.Hand);
            hand.type = Image.Type.Simple;
            hand.rectTransform.anchorMin = hand.rectTransform.anchorMax = anchor;
            hand.rectTransform.sizeDelta = new Vector2(size, size);
        }

        /// <summary>
        /// Windows Paint tutuşu: sivri uç sol-altta (tam imlecin/dokunuşun noktasında), gövde sağ-yukarıda (~45°).
        /// Solak modda simetrik olarak sol-yukarıya uzanır.
        /// </summary>
        float Angle => SaveSystem.LeftHanded ? -BaseAngle : BaseAngle;

        /// <summary>
        /// Kalem/silgi seçildiğinde: fare/imleç gezdirme varsa tuvale girince doğrudan imlece yapışır;
        /// gerçek dokunmatik cihazda (hover yok) tuvalin üstünde hazır belirir.
        /// </summary>
        public void Equip(bool animate)
        {
            if (_canvas == null || _canvas.Viewport == null) return;
            UpdateVisuals();
            if (_canvas.HasHoverPointer)
            {
                if (_canvas.PointerOver)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, _canvas.PointerScreen, null, out _pos);
                    _placed = true;
                    _vel = Vector2.zero;
                    if (animate) Hop();
                }
                return;
            }
            if (_placed && _alpha > 0.2f)
            {
                if (animate) Hop();
                return;
            }
            var vp = _canvas.Viewport;
            if (vp.rect.height <= 0f) { _pendingEquip = true; _pendingAnimate = animate; return; }
            _pendingEquip = false;
            var vpPoint = vp.rect.center + new Vector2(0f, -vp.rect.height * 0.12f);
            var screenPt = RectTransformUtility.WorldToScreenPoint(null, vp.TransformPoint(vpPoint));
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, screenPt, null, out _pos);
            _placed = true;
            _vel = Vector2.zero;
            if (_alpha < 0.2f) _alpha = animate ? 0.35f : 0.92f;
            _group.alpha = _alpha;
            Tween.Stop(_equipCo);
            float startY = animate ? -180f : -80f;
            _hopOffset = new Vector2(0f, startY);
            _root.anchoredPosition = _pos + _hopOffset;
            _crayon.localRotation = Quaternion.Euler(0, 0, Angle);
            _shadow.localRotation = _crayon.localRotation;
            _equipCo = Tween.To(this, animate ? 0.24f : 0.28f, k =>
            {
                _hopOffset = new Vector2(0f, Mathf.LerpUnclamped(startY, 0f, Tween.OutBack(k)));
            }, Tween.Linear, 0f, () => { _hopOffset = Vector2.zero; _equipCo = null; });
        }

        public void Hop()
        {
            Tween.Stop(_equipCo);
            _equipCo = Tween.To(this, 0.2f, k =>
            {
                _hopOffset = new Vector2(0f, Mathf.Sin(k * Mathf.PI) * 28f);
            }, Tween.Linear, 0f, () => { _hopOffset = Vector2.zero; _equipCo = null; });
        }

        void UpdateVisuals()
        {
            bool eraser = _canvas.Tool == Tool.Eraser;
            _crayon.gameObject.SetActive(!eraser);
            _eraser.gameObject.SetActive(eraser);
            _shadow.gameObject.SetActive(!eraser);
            _body.color = _canvas.CurrentColor.grayscale > 0.92f ? Color.Lerp(_canvas.CurrentColor, Theme.Text, 0.07f) : _canvas.CurrentColor;
        }

        bool _lastHideOs;

        void LateUpdate()
        {
            if (_canvas == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            bool drawTool = _canvas.Tool == Tool.Pencil || _canvas.Tool == Tool.Eraser;
            if (_pendingEquip && drawTool && !_canvas.HasHoverPointer && _canvas.Viewport != null && _canvas.Viewport.rect.height > 0f)
                Equip(_pendingAnimate);

            // Windows Paint mantığı: imleç gezdirme (hover) olan ortamda tuvale girince kalem anında imlecin
            // ucuna gelir, tuval dışına (palete/butonlara) çıkınca normal ok geri döner.
            // Hover olmayan gerçek telefonda ise kalem son bırakılan noktada görünür kalır.
            bool down = _canvas.Stroking || _canvas.Pressing;
            bool hover = _canvas.HasHoverPointer;
            bool active = drawTool && (down || (hover && _canvas.PointerOver));
            float target = !drawTool ? 0f : active ? 1f : (!hover && _placed) ? 0.9f : 0f;
            float speed = hover ? 20f : (target > _alpha ? 8f : 3f);
            _alpha = Mathf.MoveTowards(_alpha, target, dt * speed);

            bool hideOs = drawTool && hover && (_canvas.PointerOver || down);
            if (Cursor.visible == hideOs) Cursor.visible = !hideOs;
#if UNITY_EDITOR
            if (_lastHideOs != hideOs) { _lastHideOs = hideOs; ColoringCanvas.EditorHideCursor?.Invoke(hideOs); }
#endif
            _group.alpha = _alpha;

            UpdateVisuals();

            if (active)
            {
                if (_equipCo != null) { Tween.Stop(_equipCo); _equipCo = null; _hopOffset = Vector2.zero; }
                var sp = _canvas.PointerScreen;
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, sp, null, out var local);
                if (!_placed || _alpha < 0.05f) { _pos = local; _vel = Vector2.zero; }
                _placed = true;
                // uç tam boyanın çıktığı noktada durur (yumuşatma yok: gecikme hissi verir)
                var prev = _pos;
                _pos = local;
                _vel = Vector2.Lerp(_vel, (_pos - prev) / Mathf.Max(dt, 1e-4f), 1f - Mathf.Exp(-dt * 10f));
            }
            else _vel = Vector2.Lerp(_vel, Vector2.zero, 1f - Mathf.Exp(-dt * 6f));

            // hareketle eğilme: sağa çizerken kalem biraz geride kalır
            float tiltTarget = Mathf.Clamp(-_vel.x * 0.012f, -16f, 16f) + Mathf.Clamp(_vel.y * 0.006f, -6f, 6f);
            _tilt = Mathf.Lerp(_tilt, tiltTarget, 1f - Mathf.Exp(-dt * 9f));
            float pressTarget = down ? 1f : 0f;
            _press = Mathf.Lerp(_press, pressTarget, 1f - Mathf.Exp(-dt * 14f));

            _root.anchoredPosition = _pos + _hopOffset;
            float wobble = Mathf.Sin(Time.unscaledTime * 2.1f) * 1.5f * (1f - _press);
            _crayon.localRotation = Quaternion.Euler(0, 0, Angle + _tilt + wobble);
            float s = 1f - 0.05f * _press;
            _crayon.localScale = new Vector3(s, s, 1);
            // gölge: kaldırınca uzaklaşır, bastırınca uca yaklaşır
            float lift = Mathf.Lerp(26f, 8f, _press);
            _shadow.anchoredPosition = new Vector2(lift * 0.6f, -lift);
            _shadow.localRotation = _crayon.localRotation;
            _shadowImg.color = Theme.Text.WithAlpha(Mathf.Lerp(0.10f, 0.2f, _press));

            _eraser.localRotation = Quaternion.Euler(0, 0, _tilt * 0.7f + (down ? Mathf.Sin(Time.unscaledTime * 30f) * 4f : 0f));
            _eraser.localScale = Vector3.one * (1f - 0.06f * _press);
        }

        /// <summary>Kova modunda dokunulan yere küçük bir kova eğilip boya döker.</summary>
        public void Pour(Vector2 screen, Color color)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, screen, null, out var local);
            var b = UIKit.Node("Pour", transform.parent);
            b.SetSiblingIndex(transform.GetSiblingIndex());
            b.anchorMin = b.anchorMax = new Vector2(0.5f, 0.5f);
            b.pivot = new Vector2(0.2f, 0.2f);
            b.sizeDelta = new Vector2(130, 130);
            b.anchoredPosition = local + new Vector2(24, 30);
            var icon = UIKit.Box(b, "Bucket", Theme.Text, Art.Bucket);
            icon.type = Image.Type.Simple;
            icon.rectTransform.Stretch();
            var drop = UIKit.Box(b, "Drop", color, UIKit.Circle);
            drop.type = Image.Type.Simple;
            drop.rectTransform.anchorMin = drop.rectTransform.anchorMax = new Vector2(0.22f, 0.18f);
            drop.rectTransform.sizeDelta = new Vector2(34, 34);
            var cg = b.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            Tween.To(b, 0.55f, k =>
            {
                b.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-10f, 40f, Tween.OutBack(Mathf.Min(1f, k * 2f))));
                float sc = k < 0.2f ? Mathf.Lerp(0.6f, 1f, k / 0.2f) : 1f;
                b.localScale = new Vector3(sc, sc, 1);
                drop.rectTransform.anchoredPosition = new Vector2(-k * 30f, -k * k * 60f);
                cg.alpha = k < 0.65f ? 1f : 1f - (k - 0.65f) / 0.35f;
            }, Tween.Linear, 0f, () => { if (b != null) Destroy(b.gameObject); });
        }
    }
}
