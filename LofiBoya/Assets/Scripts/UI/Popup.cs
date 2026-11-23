using System;
using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>Kısa bilgi balonu ve evet/hayır onay penceresi.</summary>
    public static class Popup
    {
        static GameObject _toast;

        struct Modal { public GameObject go; public Action close; }

        /// <summary>Açık pencereler (en üstteki sonda). Android geri tuşu önce en üsttekini kapatır.</summary>
        static readonly System.Collections.Generic.List<Modal> _modals = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _modals.Clear(); _toast = null; }

        /// <summary>close null ise pencere geri tuşuyla kapanmaz (yaş sorusu); tuş yalnızca yutulur.</summary>
        internal static void PushModal(GameObject go, Action close) => _modals.Add(new Modal { go = go, close = close });
        internal static void PopModal(GameObject go) => _modals.RemoveAll(m => m.go == go);

        /// <summary>Geri tuşu: açık pencere varsa en üsttekini kapatır (kapatılamıyorsa tuşu yutar) ve true döner.</summary>
        public static bool BackModal()
        {
            _modals.RemoveAll(m => m.go == null);
            if (_modals.Count == 0) return false;
            _modals[_modals.Count - 1].close?.Invoke();
            return true;
        }

        /// <summary>Ekranın üstünde 1.6 sn görünen yumuşak bilgi balonu.</summary>
        public static void Toast(string text)
        {
            if (_toast != null) UnityEngine.Object.Destroy(_toast);
            var root = App.I.SafeRoot;
            var holder = UIKit.Node("Toast", root);
            _toast = holder.gameObject;
            bool longText = text.Length > 30;
            holder.Place(new Vector2(0.5f, 1f), new Vector2(0, -250), new Vector2(Mathf.Clamp(120 + text.Length * 26, 360, 900), longText ? 160 : 110));
            var card = UIKit.Card(holder, "Card", Theme.Text.WithAlpha(0.92f), 0.6f, 4f);
            ((RectTransform)card.transform.parent).Stretch();
            var label = UIKit.Label(card.transform, text, 42, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.rectTransform.Stretch(28, 10, 28, 10);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 26;
            label.resizeTextMaxSize = 42;
            var cg = holder.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            Tween.Pop(holder, 0f, 0.35f);
            Tween.Delay(longText ? 2.6f : 1.6f, () =>
            {
                if (cg != null) Tween.Fade(cg, 1f, 0f, 0.35f, () => { if (holder != null) UnityEngine.Object.Destroy(holder.gameObject); });
            });
        }

        /// <summary>Ortada çıkan onay kartı. Arka plana dokunmak "vazgeç" sayılır.</summary>
        public static void Confirm(string title, string message, string yes, string no, Action onYes)
            => Confirm(title, message, yes, no, false, onYes);

        /// <summary>destructive: "evet" bir şeyi siler. O zaman vazgeç düğmesi renkli ve öne çıkan, silme düğmesi sade olur.</summary>
        public static void Confirm(string title, string message, string yes, string no, bool destructive, Action onYes)
        {
            var root = App.I.SafeRoot;
            var overlay = UIKit.Node("Confirm", root).Stretch(-UIKit.Bleed, -UIKit.Bleed, -UIKit.Bleed, -UIKit.Bleed);
            var dim = overlay.gameObject.AddComponent<Image>();
            dim.color = Theme.Text.WithAlpha(0f);
            Tween.To(dim, 0.25f, k => dim.color = Theme.Text.WithAlpha(0.35f * k));
            var dimBtn = overlay.gameObject.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;

            bool closed = false;
            void Close()
            {
                // çift dokunuş / geri tuşu + dokunuş: ikinci kapatma hiçbir şey yapmaz (onYes de bir kez çalışır)
                if (closed || overlay == null) return;
                closed = true;
                PopModal(overlay.gameObject);
                var cg = overlay.gameObject.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = false;
                Tween.Fade(cg, 1f, 0f, 0.2f, () => { if (overlay != null) UnityEngine.Object.Destroy(overlay.gameObject); });
            }
            dimBtn.onClick.AddListener(Close);
            PushModal(overlay.gameObject, Close);

            var holder = UIKit.Node("Holder", overlay);
            holder.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 560));
            var card = UIKit.Card(holder, "Card", Theme.Card, 0.6f);
            ((RectTransform)card.transform.parent).Stretch();
            card.raycastTarget = true; // karta dokunmak pencereyi kapatmasın
            Tween.Pop(holder, 0f, 0.4f);

            var t = UIKit.Label(card.transform, title, 64, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Band(true, 100, 50);
            var m = UIKit.Label(card.transform, message, 40, Theme.TextSoft);
            m.rectTransform.Band(true, 140, 160);
            m.horizontalOverflow = HorizontalWrapMode.Wrap;
            m.rectTransform.offsetMin = new Vector2(50, m.rectTransform.offsetMin.y);
            m.rectTransform.offsetMax = new Vector2(-50, m.rectTransform.offsetMax.y);

            var noBtn = destructive
                ? UIKit.Button(card.transform, no, Theme.Mint, Theme.Text, 46, Close)
                : UIKit.Button(card.transform, no, Theme.Paper, Theme.Text, 46, Close);
            var nrt = (RectTransform)noBtn.transform;
            nrt.anchorMin = nrt.anchorMax = new Vector2(0.5f, 0);
            nrt.sizeDelta = new Vector2(320, 130);
            nrt.anchoredPosition = new Vector2(-180, 110);

            var yesBtn = destructive
                ? UIKit.Button(card.transform, yes, Theme.Paper, Theme.TextSoft, 40, () => { if (closed) return; Close(); onYes?.Invoke(); })
                : UIKit.Button(card.transform, yes, Theme.AccentButton, Color.white, 46, () => { if (closed) return; Close(); onYes?.Invoke(); });
            var yrt = (RectTransform)yesBtn.transform;
            yrt.anchorMin = yrt.anchorMax = new Vector2(0.5f, 0);
            yrt.sizeDelta = new Vector2(320, 130);
            yrt.anchoredPosition = new Vector2(180, 110);
        }
    }
}
