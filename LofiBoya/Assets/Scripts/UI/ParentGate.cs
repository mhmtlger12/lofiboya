using System;
using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Ebeveyn kilidi: ayarlara girmeden önce küçük bir toplama sorusu.
    /// (Google Play "Aileler" politikası için de iyi bir alışkanlık.)
    /// </summary>
    public static class ParentGate
    {
        public static void Show(Action onPass)
        {
            int a = UnityEngine.Random.Range(3, 9), b = UnityEngine.Random.Range(2, 8);
            int answer = a + b;
            var options = new System.Collections.Generic.List<int> { answer };
            while (options.Count < 3)
            {
                int o = answer + UnityEngine.Random.Range(-4, 5);
                if (o > 0 && !options.Contains(o)) options.Add(o);
            }
            for (int i = 0; i < options.Count; i++) { int j = UnityEngine.Random.Range(i, options.Count); (options[i], options[j]) = (options[j], options[i]); }

            var (overlay, card, close) = Panel(820, 640);
            var t = UIKit.Label(card.transform, "Ebeveyn bölümü", 56, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Band(true, 90, 50);
            var q = UIKit.Label(card.transform, $"{a} + {b} kaç eder?", 72, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            q.rectTransform.Band(true, 120, 170);
            for (int i = 0; i < 3; i++)
            {
                int val = options[i];
                var btn = UIKit.Button(card.transform, val.ToString(), i % 2 == 0 ? Theme.Paper : Theme.Paper, Theme.Text, 64, () =>
                {
                    if (val == answer) { close(); onPass?.Invoke(); }
                    else { Sfx.Wrong(); close(); }
                });
                var rt = (RectTransform)btn.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0);
                rt.sizeDelta = new Vector2(200, 160);
                rt.anchoredPosition = new Vector2((i - 1) * 230, 110);
            }
        }

        public static void Settings()
        {
            var (overlay, card, close) = Panel(860, 1040);
            var t = UIKit.Label(card.transform, "Ayarlar", 64, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Band(true, 100, 40);

            float y = -190;
            Toggle(card.transform, ref y, "Müzik", () => SaveSystem.MusicOn, v => { SaveSystem.MusicOn = v; Sfx.ApplySettings(); });
            Toggle(card.transform, ref y, "Efekt sesleri", () => SaveSystem.SoundOn, v => SaveSystem.SoundOn = v);
            Toggle(card.transform, ref y, "Konuşma (renk ve hayvan adları)", () => Speech.Enabled, v => Speech.Enabled = v);

            // müzik parçası
            var track = UIKit.Button(card.transform, "", Theme.Paper, Theme.Text, 40, null, 0.8f, false);
            var trt = (RectTransform)track.transform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1);
            trt.sizeDelta = new Vector2(700, 120);
            trt.anchoredPosition = new Vector2(0, y);
            var tl = UIKit.ButtonText(track);
            tl.text = "Parça: " + Sfx.TrackName + "  ›";
            track.onClick.AddListener(() => tl.text = "Parça: " + Sfx.NextTrack() + "  ›");
            y -= 150;

            var info = UIKit.Label(card.transform, "Reklam yok · kişisel veri toplanmaz\nİlerleme yalnızca bu cihazda saklanır.", 34, Theme.TextSoft);
            info.rectTransform.anchorMin = info.rectTransform.anchorMax = new Vector2(0.5f, 1);
            info.rectTransform.sizeDelta = new Vector2(760, 120);
            info.rectTransform.anchoredPosition = new Vector2(0, y - 20);

            var ok = UIKit.Button(card.transform, "Tamam", Theme.Accent, Color.white, 50, close);
            var ort = (RectTransform)ok.transform;
            ort.anchorMin = ort.anchorMax = new Vector2(0.5f, 0);
            ort.sizeDelta = new Vector2(360, 130);
            ort.anchoredPosition = new Vector2(0, 110);
        }

        static void Toggle(Transform parent, ref float y, string label, Func<bool> get, Action<bool> set)
        {
            var row = UIKit.Node("Row " + label, parent);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 1);
            row.sizeDelta = new Vector2(720, 120);
            row.anchoredPosition = new Vector2(0, y);
            var l = UIKit.Label(row, label, 40, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            l.rectTransform.Stretch(0, 0, 190, 0);
            l.horizontalOverflow = HorizontalWrapMode.Wrap;

            // yuvarlak anahtar
            var sw = UIKit.Box(row, "Switch", Theme.Mint, null, 0.4f);
            var srt = sw.rectTransform;
            srt.anchorMin = srt.anchorMax = new Vector2(1, 0.5f);
            srt.pivot = new Vector2(1, 0.5f);
            srt.sizeDelta = new Vector2(160, 86);
            sw.raycastTarget = true;
            var knob = UIKit.Box(sw.transform, "Knob", Color.white, UIKit.Circle);
            knob.type = Image.Type.Simple;
            knob.rectTransform.sizeDelta = new Vector2(70, 70);
            void Refresh(bool anim)
            {
                bool on = get();
                sw.color = on ? Theme.Mint : Theme.TextSoft.WithAlpha(0.4f);
                var target = new Vector2(on ? 37 : -37, 0);
                knob.rectTransform.anchorMin = knob.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                if (!anim) knob.rectTransform.anchoredPosition = target;
                else
                {
                    var from = knob.rectTransform.anchoredPosition;
                    Tween.To(knob, 0.25f, k => knob.rectTransform.anchoredPosition = Vector2.LerpUnclamped(from, target, k), Tween.OutBack);
                }
            }
            var b = sw.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { set(!get()); Sfx.Tap(); Refresh(true); });
            Refresh(false);
            y -= 140;
        }

        /// <summary>Karartılmış arka plan + ortada kart. close() ile kapanır.</summary>
        static (GameObject overlay, Image card, Action close) Panel(float w, float h)
        {
            var root = App.I.SafeRoot;
            var overlay = UIKit.Node("Panel", root).Stretch(-200, -200, -200, -200);
            var dim = overlay.gameObject.AddComponent<Image>();
            dim.color = Theme.Text.WithAlpha(0f);
            Tween.To(dim, 0.25f, k => dim.color = Theme.Text.WithAlpha(0.35f * k));
            var holder = UIKit.Node("Holder", overlay);
            holder.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, h));
            var card = UIKit.Card(holder, "Card", Theme.Card, 0.6f);
            ((RectTransform)card.transform.parent).Stretch();
            card.raycastTarget = true;
            Tween.Pop(holder, 0f, 0.4f);

            void Close()
            {
                if (overlay == null) return;
                var cg = overlay.gameObject.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = false;
                Tween.Fade(cg, 1f, 0f, 0.2f, () => { if (overlay != null) UnityEngine.Object.Destroy(overlay.gameObject); });
            }
            var dimBtn = overlay.gameObject.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(Close);
            return (overlay.gameObject, card, Close);
        }
    }
}
