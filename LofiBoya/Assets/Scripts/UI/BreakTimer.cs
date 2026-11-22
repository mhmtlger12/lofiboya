using System;
using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Nazik mola hatırlatıcısı (ebeveyn ayarı: kapalı / 10 / 20 / 30 / 45 dk).
    /// Süre dolunca tüm ekranı kaplayan sakin bir "Mola zamanı!" kartı çıkar, 5 dakika geri sayar.
    /// • Uygulamayı kapatıp açmak molayı atlatmaz (bitiş zamanı kayıtlı).
    /// • 10 dakikadan uzun ara verildiyse (çocuk zaten dinlendi) sayaç sıfırlanır.
    /// • Ebeveyn kilit sorusunu çözerek molayı bitirebilir.
    /// </summary>
    public class BreakTimer : MonoBehaviour
    {
        public static readonly int[] Options = { 0, 10, 20, 30, 45 };
        const float BreakSeconds = 5 * 60f;
        const long NaturalBreakSeconds = 10 * 60;

        public static bool Active { get; private set; }

        float _played, _saveIn;
        GameObject _overlay;
        Text _countdown;

        static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        static long BreakUntil
        {
            get => long.TryParse(PlayerPrefs.GetString("breakUntil", "0"), out var v) ? v : 0;
            set { PlayerPrefs.SetString("breakUntil", value.ToString()); PlayerPrefs.Save(); }
        }

        void Start()
        {
            _played = PlayerPrefs.GetFloat("playSec", 0);
            long last = long.TryParse(PlayerPrefs.GetString("playStamp", "0"), out var l) ? l : 0;
            if (Now - last >= NaturalBreakSeconds) _played = 0;
            if (SaveSystem.BreakMinutes > 0 && BreakUntil > Now) ShowOverlay(false);
        }

        void Update()
        {
            if (Active)
            {
                long left = BreakUntil - Now;
                if (_countdown != null) _countdown.text = Loc.T("break_wait", $"{Math.Max(0, left) / 60}:{Math.Max(0, left) % 60:00}");
                if (left <= 0) EndBreak(true);
                return;
            }
            int minutes = SaveSystem.BreakMinutes;
            if (minutes <= 0) return;
            _played += Time.unscaledDeltaTime;
            _saveIn -= Time.unscaledDeltaTime;
            if (_saveIn <= 0) Persist();
            if (_played >= minutes * 60f)
            {
                BreakUntil = Now + (long)BreakSeconds;
                ShowOverlay(true);
            }
        }

        void Persist()
        {
            _saveIn = 10f;
            PlayerPrefs.SetFloat("playSec", _played);
            PlayerPrefs.SetString("playStamp", Now.ToString());
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) { Persist(); PlayerPrefs.Save(); return; }
            // geri dönüş: uzun ara verdiyse yeni oturum
            long last = long.TryParse(PlayerPrefs.GetString("playStamp", "0"), out var l) ? l : 0;
            if (!Active && Now - last >= NaturalBreakSeconds) _played = 0;
        }

        void OnApplicationQuit() { Persist(); PlayerPrefs.Save(); }

        void ShowOverlay(bool speak)
        {
            if (_overlay != null) return;
            Active = true;
            var root = App.I.SafeRoot;
            var ov = UIKit.Node("Break", root).Stretch(-UIKit.Bleed, -UIKit.Bleed, -UIKit.Bleed, -UIKit.Bleed);
            _overlay = ov.gameObject;
            var dim = ov.gameObject.AddComponent<Image>();
            dim.color = Theme.BgMid.WithAlpha(0f);
            Tween.To(dim, 0.6f, k => dim.color = Theme.BgMid.WithAlpha(0.97f * k));

            var holder = UIKit.Node("Holder", ov);
            holder.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860, 1160));
            var card = UIKit.Card(holder, "Card", Theme.Card, 0.6f);
            ((RectTransform)card.transform.parent).Stretch();
            Tween.Pop(holder, 0.2f, 0.6f);

            var mascot = Mascot.Create(card.transform, 300, false);
            var mrt = (RectTransform)mascot.transform;
            mrt.anchorMin = mrt.anchorMax = new Vector2(0.5f, 1);
            mrt.anchoredPosition = new Vector2(0, -230);

            var t = UIKit.Label(card.transform, Loc.T("break_title"), 84, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Band(true, 120, 420);
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 48;
            t.resizeTextMaxSize = 84;
            var s = UIKit.Label(card.transform, Loc.T("break_sub"), 44, Theme.TextSoft);
            s.rectTransform.Band(true, 200, 550, 50);
            s.horizontalOverflow = HorizontalWrapMode.Wrap;
            _countdown = UIKit.Label(card.transform, "", 52, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            _countdown.rectTransform.Band(true, 80, 780);

            var cont = UIKit.Button(card.transform, Loc.T("break_continue"), Theme.Paper, Theme.TextSoft, 36, () =>
                ParentGate.Show(() => EndBreak(false)), 0.8f, false);
            var crt = (RectTransform)cont.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0);
            crt.sizeDelta = new Vector2(560, 110);
            crt.anchoredPosition = new Vector2(0, 100);

            Sfx.Star();
            if (speak) Tween.Delay(0.8f, () => Speech.Say(Loc.T("break_title") + " " + Loc.T("break_sub")));
            Tween.Delay(1.6f, () => { if (mascot != null) mascot.Wave(2f); });
        }

        void EndBreak(bool naturally)
        {
            BreakUntil = 0;
            _played = 0;
            Persist();
            PlayerPrefs.Save();
            Active = false;
            if (naturally) Speech.Say(Loc.T("break_over"));
            if (_overlay == null) return;
            var ov = _overlay;
            _overlay = null;
            _countdown = null;
            var cg = ov.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            Tween.Fade(cg, 1f, 0f, 0.4f, () => { if (ov != null) Destroy(ov); });
        }

        /// <summary>Ayar değişince sayaç baştan başlar.</summary>
        public static void ResetCounter()
        {
            PlayerPrefs.SetFloat("playSec", 0);
            var bt = FindAnyObjectByType<BreakTimer>();
            if (bt != null) bt._played = 0;
        }
    }
}
