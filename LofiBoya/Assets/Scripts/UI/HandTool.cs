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
        Vector2 _pos, _vel, _lastScreen;
        float _alpha, _tilt, _press, _idle;
        bool _placed;

        const float Len = 230f, Wid = 58f, BaseAngle = 150f;
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

        void OnDisable() => Cursor.visible = true;

        static void AddHand(RectTransform parent, Vector2 anchor, float size)
        {
            var hand = UIKit.Box(parent, "Hand", Color.white, MascotArt.Hand);
            hand.type = Image.Type.Simple;
            hand.rectTransform.anchorMin = hand.rectTransform.anchorMax = anchor;
            hand.rectTransform.sizeDelta = new Vector2(size, size);
        }

        /// <summary>
        /// Farede kalem sağ-yukarı uzanır (uç imlecin yerinde). Dokunmatikte uç parmağın biraz önünde
        /// (bkz. ColoringCanvas.TipOffset) ve gövde aşağı, parmağa doğru uzanır: parmak kalemi tutar, uç görünür.
        /// </summary>
        float Angle
        {
            get
            {
                var off = _canvas.TipOffset;
                if (off == Vector2.zero) return BaseAngle;
                // gövdenin yerel yönü aşağı (-90°); parmağa (uçtan -off yönüne) çevir
                return Mathf.Atan2(-off.y, -off.x) * Mathf.Rad2Deg + 90f;
            }
        }

        void LateUpdate()
        {
            if (_canvas == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            bool drawTool = _canvas.Tool == Tool.Pencil || _canvas.Tool == Tool.Eraser;
            bool eraser = _canvas.Tool == Tool.Eraser;

            // görünürlük: çizerken her zaman; farede tuvalin üstündeyken; dokunmatikte parmak kalkınca kısa süre kalır
            bool active = drawTool && (_canvas.Stroking || (!_canvas.TouchInput && _canvas.PointerOver));
            if (active) _idle = 0; else _idle += dt;
            float target = drawTool && (active || (_canvas.TouchInput && _placed && _idle < 0.9f)) ? 1f : 0f;
            _alpha = Mathf.MoveTowards(_alpha, target, dt * (target > _alpha ? 8f : 3f));
            // Farede Windows/Mac imleci donanımla çizilir ve oyunun çizgisinden hep 1–3 kare öndedir:
            // çizgi geriden geliyor gibi görünür. Kalem tuvalin üstündeyken imleç gizlenir; imleç kalemin
            // kendisidir ve çizgiyle aynı karede çizilir.
            bool hideOs = drawTool && !_canvas.TouchInput && (_canvas.PointerOver || _canvas.Stroking);
            if (Cursor.visible == hideOs) Cursor.visible = !hideOs;
            _group.alpha = _alpha;

            _crayon.gameObject.SetActive(!eraser);
            _eraser.gameObject.SetActive(eraser);
            _shadow.gameObject.SetActive(!eraser);
            _body.color = _canvas.CurrentColor.grayscale > 0.92f ? Color.Lerp(_canvas.CurrentColor, Theme.Text, 0.07f) : _canvas.CurrentColor;

            if (active || !_placed)
            {
                var sp = _canvas.PointerScreen;
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, sp, null, out var local);
                if (!_placed || _alpha < 0.05f) { _pos = local; _vel = Vector2.zero; }
                if (_canvas.Stroking || _canvas.PointerOver) _placed = true;
                // uç tam boyanın çıktığı noktada durur (yumuşatma yok: gecikme hissi verir)
                var prev = _pos;
                _pos = local;
                _vel = Vector2.Lerp(_vel, (_pos - prev) / Mathf.Max(dt, 1e-4f), 1f - Mathf.Exp(-dt * 10f));
            }
            else _vel = Vector2.Lerp(_vel, Vector2.zero, 1f - Mathf.Exp(-dt * 6f));

            // hareketle eğilme: sağa çizerken kalem biraz geride kalır
            float tiltTarget = Mathf.Clamp(-_vel.x * 0.012f, -16f, 16f) + Mathf.Clamp(_vel.y * 0.006f, -6f, 6f);
            _tilt = Mathf.Lerp(_tilt, tiltTarget, 1f - Mathf.Exp(-dt * 9f));
            float pressTarget = _canvas.Stroking ? 1f : 0f;
            _press = Mathf.Lerp(_press, pressTarget, 1f - Mathf.Exp(-dt * 14f));

            _root.anchoredPosition = _pos;
            float wobble = Mathf.Sin(Time.unscaledTime * 2.1f) * 1.5f * (1f - _press);
            _crayon.localRotation = Quaternion.Euler(0, 0, Angle + _tilt + wobble);
            float s = 1f - 0.05f * _press;
            _crayon.localScale = new Vector3(s, s, 1);
            // gölge: kaldırınca uzaklaşır, bastırınca uca yaklaşır
            float lift = Mathf.Lerp(26f, 8f, _press);
            _shadow.anchoredPosition = new Vector2(lift * 0.6f, -lift);
            _shadow.localRotation = _crayon.localRotation;
            _shadowImg.color = Theme.Text.WithAlpha(Mathf.Lerp(0.10f, 0.2f, _press));

            _eraser.localRotation = Quaternion.Euler(0, 0, _tilt * 0.7f + (_canvas.Stroking ? Mathf.Sin(Time.unscaledTime * 30f) * 4f : 0f));
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
