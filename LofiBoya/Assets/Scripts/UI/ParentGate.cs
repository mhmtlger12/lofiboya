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
            var t = UIKit.Label(card.transform, Loc.T("parents"), 56, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Band(true, 90, 50);
            var q = UIKit.Label(card.transform, Loc.T("gate_q", a, b), 72, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
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
            var (overlay, card, close) = Panel(860, 1652);
            var t = UIKit.Label(card.transform, Loc.T("settings"), 64, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Band(true, 100, 40);

            float y = -190;
            Toggle(card.transform, ref y, Loc.T("music"), () => SaveSystem.MusicOn, v => { SaveSystem.MusicOn = v; Sfx.ApplySettings(); });
            Toggle(card.transform, ref y, Loc.T("sound"), () => SaveSystem.SoundOn, v => SaveSystem.SoundOn = v);
            Toggle(card.transform, ref y, Loc.T("haptics"), () => Haptics.Enabled, v => { Haptics.Enabled = v; Haptics.Light(); });
            Toggle(card.transform, ref y, Loc.T("pencil_inside"), () => SaveSystem.PencilStaysInside, v => SaveSystem.PencilStaysInside = v);

            // mola hatırlatıcısı: kapalı / 10 / 20 / 30 / 45 dk
            var brk = UIKit.Button(card.transform, "", Theme.Paper, Theme.Text, 40, null, 0.8f, false);
            var brt = (RectTransform)brk.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 1);
            brt.sizeDelta = new Vector2(700, 116);
            brt.anchoredPosition = new Vector2(0, y);
            var bl = UIKit.ButtonText(brk);
            bl.resizeTextForBestFit = true;
            bl.resizeTextMinSize = 26;
            bl.resizeTextMaxSize = 40;
            string BreakLabel() => Loc.T("break_btn", SaveSystem.BreakMinutes > 0 ? Loc.T("break_every", SaveSystem.BreakMinutes) : Loc.T("break_off"));
            bl.text = BreakLabel();
            brk.onClick.AddListener(() =>
            {
                int i = System.Array.IndexOf(BreakTimer.Options, SaveSystem.BreakMinutes);
                SaveSystem.BreakMinutes = BreakTimer.Options[(i + 1) % BreakTimer.Options.Length];
                BreakTimer.ResetCounter();
                Sfx.Tap();
                bl.text = BreakLabel();
            });
            y -= 135;

            // yaş grubu: galerinin açılış sekmesi ve sayma oyununun başlangıç seviyesi
            var age = SettingButton(card.transform, ref y);
            var al = UIKit.ButtonText(age);
            al.text = Loc.T("age_btn", SaveSystem.AgeLabels[SaveSystem.AgeBand]);
            age.onClick.AddListener(() =>
            {
                SaveSystem.AgeBand = (SaveSystem.AgeBand + 1) % SaveSystem.AgeLabels.Length;
                Sfx.Tap();
                al.text = Loc.T("age_btn", SaveSystem.AgeLabels[SaveSystem.AgeBand]);
            });

            // müzik parçası
            var track = SettingButton(card.transform, ref y);
            var tl = UIKit.ButtonText(track);
            tl.text = Loc.T("track", Sfx.TrackName);
            track.onClick.AddListener(() => tl.text = Loc.T("track", Sfx.NextTrack()));

            // dil
            var lang = SettingButton(card.transform, ref y);
            UIKit.ButtonText(lang).text = Loc.T("language", CurrentLanguageName());
            lang.onClick.AddListener(() => { close(); LanguagePicker(); });

            // haftalık rapor
            var rep = SettingButton(card.transform, ref y);
            UIKit.ButtonText(rep).text = Loc.T("report_btn");
            rep.onClick.AddListener(() => { close(); Report(); });
            y -= 10;

            var info = UIKit.Label(card.transform, Loc.T("info"), 34, Theme.TextSoft);
            info.rectTransform.anchorMin = info.rectTransform.anchorMax = new Vector2(0.5f, 1);
            info.rectTransform.sizeDelta = new Vector2(760, 120);
            info.rectTransform.anchoredPosition = new Vector2(0, y - 20);

            var ok = UIKit.Button(card.transform, Loc.T("ok"), Theme.Accent, Color.white, 50, close);
            var ort = (RectTransform)ok.transform;
            ort.anchorMin = ort.anchorMax = new Vector2(0.5f, 0);
            ort.sizeDelta = new Vector2(360, 130);
            ort.anchoredPosition = new Vector2(0, 110);
        }

        static Button SettingButton(Transform parent, ref float y)
        {
            var b = UIKit.Button(parent, "", Theme.Paper, Theme.Text, 40, null, 0.8f, false);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(700, 116);
            rt.anchoredPosition = new Vector2(0, y);
            var t = UIKit.ButtonText(b);
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 26;
            t.resizeTextMaxSize = 40;
            y -= 135;
            return b;
        }

        /// <summary>Son 7 gün: bitirilen resimler, süre, oynanan günler ve öğrenme oyunlarında doğru/deneme.
        /// Yüzde ya da not yok: aileye çocuğun neyle uğraştığını gösterir, kıyaslamaz.</summary>
        static void Report()
        {
            var rows = new System.Collections.Generic.List<(string text, bool head)>
            {
                (Loc.T("rep_pictures", Stats.Week("pic")), false),
                (Loc.T("rep_time", Mathf.RoundToInt(Stats.Week("sec") / 60f)), false),
                (Loc.T("rep_days", Stats.ActiveDays()), false),
                (Loc.T("rep_games_title"), true),
            };
            string[] titles = { "colors", "pictures", "shapes", "counting", "feelings_game", "trace" };
            bool any = false;
            for (int i = 0; i < Stats.Games.Length; i++)
            {
                int ok = Stats.Week(Stats.Games[i] + "_ok"), no = Stats.Week(Stats.Games[i] + "_no");
                if (ok + no == 0) continue;
                any = true;
                rows.Add((Loc.T("rep_game", Loc.T(titles[i]), ok, ok + no), false));
            }
            if (!any) rows.Add((Loc.T("rep_none"), false));

            const float rowH = 96f;
            float h = 150 + rows.Count * rowH + 150 + 200;
            var (overlay, card, close) = Panel(860, h);
            var t = UIKit.Label(card.transform, Loc.T("report_title"), 60, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Band(true, 100, 40);
            float y = -190;
            foreach (var (text, head) in rows)
            {
                var l = UIKit.Label(card.transform, text, head ? 42 : 38, head ? Theme.Text : Theme.TextSoft, TextAnchor.MiddleLeft, head ? FontStyle.Bold : FontStyle.Normal);
                var rt = l.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1);
                rt.sizeDelta = new Vector2(740, rowH);
                rt.anchoredPosition = new Vector2(0, y);
                l.resizeTextForBestFit = true;
                l.resizeTextMinSize = 24;
                l.resizeTextMaxSize = head ? 42 : 38;
                y -= rowH;
            }
            var note = UIKit.Label(card.transform, Loc.T("rep_note"), 32, Theme.TextSoft);
            note.rectTransform.anchorMin = note.rectTransform.anchorMax = new Vector2(0.5f, 1);
            note.rectTransform.sizeDelta = new Vector2(740, 130);
            note.rectTransform.anchoredPosition = new Vector2(0, y - 40);
            note.horizontalOverflow = HorizontalWrapMode.Wrap;

            var ok2 = UIKit.Button(card.transform, Loc.T("ok"), Theme.Accent, Color.white, 50, close);
            var ort = (RectTransform)ok2.transform;
            ort.anchorMin = ort.anchorMax = new Vector2(0.5f, 0);
            ort.sizeDelta = new Vector2(360, 130);
            ort.anchoredPosition = new Vector2(0, 110);
        }

        static string CurrentLanguageName() =>
            Loc.Setting == "auto" ? Loc.T("lang_auto") + " (" + Loc.NativeNames[Loc.Index] + ")" : Loc.NativeNames[Loc.Index];

        /// <summary>Dil listesi: Otomatik + 10 dil. Seçince oyun o dilde yeniden açılır.</summary>
        static void LanguagePicker()
        {
            int n = Loc.Codes.Length + 1;
            const float rowH = 104f;
            var (overlay, card, close) = Panel(720, 120 + rowH * n + 40);
            for (int i = 0; i < n; i++)
            {
                string code = i == 0 ? "auto" : Loc.Codes[i - 1];
                string label = i == 0 ? Loc.T("lang_auto") + " (" + Loc.NativeNames[System.Array.IndexOf(Loc.Codes, Loc.DeviceLanguage()) is int k && k >= 0 ? k : 0] + ")" : Loc.NativeNames[i - 1];
                bool selected = Loc.Setting == code;
                var b = UIKit.Button(card.transform, label, selected ? Theme.Accent : Theme.Paper, selected ? Color.white : Theme.Text, 40, () =>
                {
                    close();
                    if (Loc.Setting != code) Loc.Setting = code; // App ekranı yeni dilde baştan kurar
                }, 0.8f, false);
                var rt = (RectTransform)b.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1);
                rt.sizeDelta = new Vector2(600, rowH - 14);
                rt.anchoredPosition = new Vector2(0, -60 - rowH * (i + 0.5f));
                // her dil kendi yazı tipiyle görünsün
                var t = UIKit.ButtonText(b);
                if (code == "ar") t.font = Resources.Load<Font>("Fonts/Arabic");
                else if (code == "ru") t.font = Resources.Load<Font>("Fonts/Cyrillic");
                else t.font = Resources.Load<Font>("Fonts/Main");
                if (code == "ar") t.text = ArabicShaper.Shape(label);
                else if (code == "hi") t.text = Devanagari.Reorder(label);
            }
        }

        static void Toggle(Transform parent, ref float y, string label, Func<bool> get, Action<bool> set)
        {
            var row = UIKit.Node("Row " + label, parent);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 1);
            row.sizeDelta = new Vector2(720, 116);
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
            y -= 128;
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
