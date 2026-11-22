using System;
using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Ebeveyn kilidi: çarpma sorusu (çocuğun oyunda öğrendiği toplama değil) ve cevaba 2 saniye basılı tutma
    /// (okuma gerektiren talimat). Tek dokunuşla rastgele geçilemez.
    /// (Google Play "Aileler" politikası için de iyi bir alışkanlık.)
    /// </summary>
    public static class ParentGate
    {
        public static void Show(Action onPass)
        {
            int a = UnityEngine.Random.Range(3, 10), b = UnityEngine.Random.Range(3, 10);
            int answer = a * b;
            var options = new System.Collections.Generic.List<int> { answer };
            while (options.Count < 4)
            {
                int o = answer + UnityEngine.Random.Range(-12, 13);
                if (o > 4 && !options.Contains(o)) options.Add(o);
            }
            for (int i = 0; i < options.Count; i++) { int j = UnityEngine.Random.Range(i, options.Count); (options[i], options[j]) = (options[j], options[i]); }

            var (overlay, card, close) = Panel(860, 760);
            var t = UIKit.Label(card.transform, Loc.T("parents"), 56, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Band(true, 90, 50);
            var q = UIKit.Label(card.transform, a + " × " + b + " = ?", 80, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            q.rectTransform.Band(true, 120, 160);
            var hint = UIKit.Label(card.transform, Loc.T("gate_hold"), 36, Theme.TextSoft);
            hint.rectTransform.Band(true, 70, 290);
            hint.rectTransform.offsetMin = new Vector2(40, hint.rectTransform.offsetMin.y);
            hint.rectTransform.offsetMax = new Vector2(-40, hint.rectTransform.offsetMax.y);
            hint.horizontalOverflow = HorizontalWrapMode.Wrap;
            hint.resizeTextForBestFit = true;
            hint.resizeTextMinSize = 24;
            hint.resizeTextMaxSize = 36;
            for (int i = 0; i < options.Count; i++)
            {
                int val = options[i];
                HoldButton hold = null;
                var btn = UIKit.Button(card.transform, val.ToString(), Theme.Paper, Theme.Text, 64, () => { if (hold != null && !hold.Fired) Popup.Toast(Loc.T("gate_hold")); });
                var rt = (RectTransform)btn.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0);
                rt.sizeDelta = new Vector2(176, 176);
                rt.anchoredPosition = new Vector2((i - 1.5f) * 196, 150);
                hold = btn.gameObject.AddComponent<HoldButton>();
                hold.Init(2f, () =>
                {
                    if (val == answer) { close(); onPass?.Invoke(); }
                    else { Sfx.Wrong(); close(); }
                });
            }
        }

        /// <summary>İlk açılışta (bir kez) ebeveyne çocuğun yaşını sorar; oyunlar buna göre ayarlanır.</summary>
        public static void AskAge()
        {
            var (overlay, card, close) = Panel(860, 820);
            var t = UIKit.Label(card.transform, Loc.T("age_q"), 60, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Band(true, 100, 50);
            var sub = UIKit.Label(card.transform, Loc.T("age_q_sub"), 34, Theme.TextSoft);
            sub.rectTransform.Band(true, 130, 160);
            sub.rectTransform.offsetMin = new Vector2(50, sub.rectTransform.offsetMin.y);
            sub.rectTransform.offsetMax = new Vector2(-50, sub.rectTransform.offsetMax.y);
            sub.horizontalOverflow = HorizontalWrapMode.Wrap;
            for (int i = 0; i < SaveSystem.AgeLabels.Length; i++)
            {
                int band = i;
                var b = UIKit.Button(card.transform, SaveSystem.AgeLabels[i], i == 0 ? Theme.Butter : i == 1 ? Theme.Mint : Theme.Sky, Theme.Text, 72, () =>
                {
                    SaveSystem.AgeBand = band;
                    close();
                });
                var rt = (RectTransform)b.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0);
                rt.sizeDelta = new Vector2(230, 230);
                rt.anchoredPosition = new Vector2((i - 1) * 260, 220);
                UIKit.ButtonText(b).fontStyle = FontStyle.Bold;
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

            // erişilebilirlik: sakin mod, renk körü desteği, kalem taşmasın
            var acc = SettingButton(card.transform, ref y);
            UIKit.ButtonText(acc).text = Loc.T("access_btn") + "  ›";
            acc.onClick.AddListener(() => { close(); Accessibility(); });

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

            var ok = UIKit.Button(card.transform, Loc.T("ok"), Theme.AccentButton, Color.white, 50, close);
            var ort = (RectTransform)ok.transform;
            ort.anchorMin = ort.anchorMax = new Vector2(0.5f, 0);
            ort.sizeDelta = new Vector2(360, 130);
            ort.anchoredPosition = new Vector2(0, 110);
        }

        static void Accessibility()
        {
            var (overlay, card, close) = Panel(860, 1240);
            var t = UIKit.Label(card.transform, Loc.T("access_title"), 60, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Band(true, 100, 40);
            float y = -190;
            Toggle(card.transform, ref y, Loc.T("calm_mode"), () => SaveSystem.CalmMode, v => SaveSystem.CalmMode = v);
            Toggle(card.transform, ref y, Loc.T("color_assist"), () => SaveSystem.ColorAssist, v => SaveSystem.ColorAssist = v);
            Toggle(card.transform, ref y, Loc.T("pencil_inside"), () => SaveSystem.PencilStaysInside, v => SaveSystem.PencilStaysInside = v);
            Toggle(card.transform, ref y, Loc.T("easy_trace"), () => SaveSystem.EasyTrace, v => SaveSystem.EasyTrace = v);
            var info = UIKit.Label(card.transform, Loc.T("access_info"), 32, Theme.TextSoft);
            info.rectTransform.anchorMin = info.rectTransform.anchorMax = new Vector2(0.5f, 1);
            info.rectTransform.sizeDelta = new Vector2(740, 340);
            info.rectTransform.anchoredPosition = new Vector2(0, y - 150);
            info.horizontalOverflow = HorizontalWrapMode.Wrap;
            info.verticalOverflow = VerticalWrapMode.Truncate;
            info.resizeTextForBestFit = true;
            info.resizeTextMinSize = 22;
            info.resizeTextMaxSize = 32;
            var ok = UIKit.Button(card.transform, Loc.T("ok"), Theme.AccentButton, Color.white, 50, close);
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

        /// <summary>
        /// Haftalık rapor: üstte üç özet kutusu (resim, dakika, gün), altında her öğrenme oyunu için
        /// renkli doğru oranı çubuğu, en çok karıştırılan ikili ve "Evde deneyin" kartı.
        /// Not ya da sıralama yok: aileye çocuğun neyle uğraştığını gösterir, kıyaslamaz.
        /// </summary>
        static void Report()
        {
            int pic = Stats.Week("pic"), min = Mathf.RoundToInt(Stats.Week("sec") / 60f), days = Stats.ActiveDays();
            int lastPic = Stats.Week("pic", 1), lastMin = Mathf.RoundToInt(Stats.Week("sec", 1) / 60f);
            bool hasLast = lastPic + lastMin > 0;

            string[] titles = { "colors", "pictures", "shapes", "counting", "feelings_game", "trace", "letters", "patterns", "memory", "opposites", "sequence", "shadows", "seasons" };
            Color[] colors =
            {
                Theme.Accent, Theme.Mint, Theme.Lavender, Theme.Butter, Theme.Sky, Theme.Hex("FFCBA4"),
                Theme.Hex("F5C6E0"), Theme.Hex("CDEBB5"), Theme.Hex("EBD9C4"),
                Theme.Hex("FFD6C9"), Theme.Hex("D8F0C8"), Theme.Hex("DCD6F7"), Theme.Hex("CFE8F7"),
            };
            // ok/total: turun ilk cevabı (deneme-yanılma sayılmaz). Yazma ve hafızada oran yok: total = -1, ok = tamamlanan.
            var games = new System.Collections.Generic.List<(int i, int ok, int total)>();
            string weakest = null, leastPlayed = null;
            float weakRate = 2f;
            int leastN = int.MaxValue;
            for (int i = 0; i < Stats.Games.Length; i++)
            {
                string g = Stats.Games[i];
                if (Stats.IsDoneGame(g))
                {
                    int done = Stats.Week(g + "_done");
                    if (done < leastN) { leastN = done; leastPlayed = g; }
                    if (done > 0) games.Add((i, done, -1));
                    continue;
                }
                int ok = Stats.Week(g + "_ok"), no = Stats.Week(g + "_no");
                if (ok + no < leastN) { leastN = ok + no; leastPlayed = g; }
                if (ok + no == 0) continue;
                games.Add((i, ok, ok + no));
                float rate = ok / (float)(ok + no);
                if (ok + no >= 5 && rate < weakRate) { weakRate = rate; weakest = g; }
            }
            var cf = Stats.TopConfusion();
            string tipGame = weakRate < 0.7f ? weakest : leastPlayed;
            string tip = tipGame != null ? Loc.T("tip_" + tipGame) : null;

            // ---- içerik yüksekliği ----
            const float tileH = 210f, rowH = 78f, gap = 34f;
            float contentH = tileH + gap + 64 + 44 + Mathf.Max(1, games.Count) * rowH + gap;
            if (cf.HasValue) contentH += 130 + gap;
            if (tip != null) contentH += 210 + gap;
            contentH += 100;
            float panelH = Mathf.Min(contentH + 340f, 1680f);

            var (overlay, card, close) = Panel(920, panelH);
            var t = UIKit.Label(card.transform, Loc.T("report_title"), 62, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Band(true, 100, 36);

            // kaydırılabilir gövde (uzun raporlar ekrana sığsın)
            var scrollRt = UIKit.Node("Scroll", card.transform).Stretch(0, 150, 0, 190);
            var sr = scrollRt.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 40;
            var vp = UIKit.Node("Viewport", scrollRt).Stretch();
            vp.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(0, 24);
            vp.gameObject.AddComponent<Image>().color = Color.clear;
            sr.viewport = vp;
            var content = UIKit.Node("Content", vp);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            content.sizeDelta = new Vector2(0, contentH);
            sr.content = content;

            RectTransform At(Transform parent, float x, float y, float w, float h)
            {
                var rt = UIKit.Node("Item", parent);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.anchoredPosition = new Vector2(x, -y);
                rt.sizeDelta = new Vector2(w, h);
                return rt;
            }
            Text Txt(Transform parent, string s, int size, Color c, TextAnchor a, FontStyle f = FontStyle.Normal)
            {
                var l = UIKit.Label(parent, s, size, c, a, f);
                l.horizontalOverflow = HorizontalWrapMode.Wrap;
                l.resizeTextForBestFit = true;
                l.resizeTextMinSize = Mathf.Min(20, size);
                l.resizeTextMaxSize = size;
                return l;
            }

            float y = 10;
            // ---- üç özet kutusu ----
            var tiles = new (string value, string label, int last, Color color)[]
            {
                (pic.ToString(), Loc.T("rep_l_pic"), lastPic, Theme.Butter),
                (min.ToString(), Loc.T("rep_l_min"), lastMin, Theme.Mint),
                (days + "/7", Loc.T("rep_l_days"), -1, Theme.Sky),
            };
            for (int i = 0; i < tiles.Length; i++)
            {
                var (value, label, last, color) = tiles[i];
                var box = UIKit.Box(At(content, (i - 1) * 272, y, 252, tileH), "Tile", color, null, 0.6f);
                box.rectTransform.Stretch();
                var num = Txt(box.transform, value, 84, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                num.rectTransform.Stretch(8, 14, 8, 92);
                var lab = Txt(box.transform, label, 30, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                lab.rectTransform.Stretch(10, 112, 10, 48);
                if (hasLast && last >= 0)
                {
                    var prev = Txt(box.transform, Loc.T("rep_last", last), 24, Theme.Text.WithAlpha(0.75f), TextAnchor.MiddleCenter);
                    prev.rectTransform.Stretch(10, 158, 10, 12);
                }
            }
            y += tileH + gap;

            // ---- oyunlar: renkli doğru oranı çubukları ----
            var head = Txt(At(content, 0, y, 800, 56), Loc.T("rep_games_title"), 40, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            head.rectTransform.Stretch();
            y += 64;
            // ölçümün ne olduğu açık olsun: "ilk denemede doğru / soru"
            var how = Txt(At(content, 0, y - 8, 800, 44), Loc.T("rep_first_try"), 26, Theme.TextSoft, TextAnchor.MiddleLeft);
            how.rectTransform.Stretch();
            y += 44;
            if (games.Count == 0)
            {
                var none = Txt(At(content, 0, y, 800, rowH), Loc.T("rep_none"), 32, Theme.TextSoft, TextAnchor.MiddleLeft);
                none.rectTransform.Stretch();
                y += rowH;
            }
            foreach (var (i, ok, total) in games)
            {
                var row = At(content, 0, y, 800, rowH - 12);
                var dot = UIKit.Box(row, "Dot", colors[i], UIKit.Circle);
                dot.type = Image.Type.Simple;
                dot.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(30, 30));
                var name = Txt(row, Loc.T(titles[i]), 32, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
                name.rectTransform.anchorMin = new Vector2(0, 0);
                name.rectTransform.anchorMax = new Vector2(0, 1);
                name.rectTransform.pivot = new Vector2(0, 0.5f);
                name.rectTransform.anchoredPosition = new Vector2(48, 0);
                name.rectTransform.sizeDelta = new Vector2(250, 0);
                if (total < 0)
                {
                    // yazma / hafıza: oran değil, kaç kez tamamlandı
                    var doneTxt = Txt(row, Loc.T("rep_done", ok), 30, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
                    doneTxt.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(310 + 245, 0), new Vector2(490, 60));
                    y += rowH;
                    continue;
                }
                var track = UIKit.Box(row, "Track", Theme.Text.WithAlpha(0.08f), null, 2f);
                track.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(310 + 200, 0), new Vector2(400, 30));
                float rate = ok / (float)total;
                var fill = UIKit.Box(track.transform, "Fill", Color.Lerp(colors[i], Theme.Text, 0.12f), null, 2f);
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.04f, rate), 1);
                fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
                var count = Txt(row, ok + "/" + total, 30, Theme.Text, TextAnchor.MiddleRight, FontStyle.Bold);
                count.rectTransform.anchorMin = new Vector2(1, 0);
                count.rectTransform.anchorMax = new Vector2(1, 1);
                count.rectTransform.pivot = new Vector2(1, 0.5f);
                count.rectTransform.anchoredPosition = Vector2.zero;
                count.rectTransform.sizeDelta = new Vector2(84, 0);
                y += rowH;
            }
            y += gap;

            // ---- en çok karıştırılan ikili ----
            if (cf.HasValue)
            {
                var (game, a, b, n) = cf.Value;
                string Nice(string k) => game switch
                {
                    "counting" or "letters" => k,
                    "opposites" => Names.Cap(Loc.T("w_" + k.Replace("opp_", ""))),
                    "seasons" => Names.Cap(Loc.T(k)),
                    _ => Names.Cap(Names.Word(k).name),
                };
                var box = UIKit.Box(At(content, 0, y, 820, 130), "Confused", Theme.Accent.WithAlpha(0.22f), null, 0.6f);
                box.rectTransform.Stretch();
                var ht = Txt(box.transform, Loc.T("rep_cf_title"), 30, Theme.Text, TextAnchor.UpperLeft, FontStyle.Bold);
                ht.rectTransform.Stretch(30, 16, 30, 70);
                var pt = Txt(box.transform, Loc.T("rep_cf_pair", Nice(a), Nice(b), n), 34, Theme.Text, TextAnchor.MiddleLeft);
                pt.rectTransform.Stretch(30, 60, 30, 14);
                y += 130 + gap;
            }

            // ---- evde deneyin ----
            if (tip != null)
            {
                var box = UIKit.Box(At(content, 0, y, 820, 210), "Tip", Theme.Butter.WithAlpha(0.75f), null, 0.6f);
                box.rectTransform.Stretch();
                var bulb = UIKit.Box(box.transform, "Bulb", Theme.Text, Art.Bulb);
                bulb.type = Image.Type.Simple;
                bulb.preserveAspect = true;
                bulb.rectTransform.Place(new Vector2(0, 1), new Vector2(62, -62), new Vector2(64, 64));
                var ht = Txt(box.transform, Loc.T("tip_title"), 32, Theme.Text, TextAnchor.UpperLeft, FontStyle.Bold);
                ht.rectTransform.Stretch(112, 34, 28, 140);
                var tt = Txt(box.transform, tip, 31, Theme.Text, TextAnchor.UpperLeft);
                tt.rectTransform.Stretch(112, 80, 28, 16);
                y += 210 + gap;
            }

            var note = Txt(At(content, 0, y, 800, 90), Loc.T("rep_note"), 28, Theme.TextSoft, TextAnchor.MiddleCenter);
            note.rectTransform.Stretch();

            var ok2 = UIKit.Button(card.transform, Loc.T("ok"), Theme.AccentButton, Color.white, 50, close);
            var ort = (RectTransform)ok2.transform;
            ort.anchorMin = ort.anchorMax = new Vector2(0.5f, 0);
            ort.sizeDelta = new Vector2(360, 130);
            ort.anchoredPosition = new Vector2(0, 100);
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
                var b = UIKit.Button(card.transform, label, selected ? Theme.AccentButton : Theme.Paper, selected ? Color.white : Theme.Text, 40, () =>
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
            l.verticalOverflow = VerticalWrapMode.Truncate;   // uzun etiket satırın içinde küçülür, alttakiyle çakışmaz
            l.lineSpacing = 0.9f;
            l.resizeTextForBestFit = true;
            l.resizeTextMinSize = 28;
            l.resizeTextMaxSize = 40;

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
            A11y.Label(b, () => label + ", " + Loc.T(get() ? "a11y_on" : "a11y_off"));
            Refresh(false);
            y -= 128;
        }

        /// <summary>Karartılmış arka plan + ortada kart. close() ile kapanır.</summary>
        static (GameObject overlay, Image card, Action close) Panel(float w, float h)
        {
            var root = App.I.SafeRoot;
            var overlay = UIKit.Node("Panel", root).Stretch(-UIKit.Bleed, -UIKit.Bleed, -UIKit.Bleed, -UIKit.Bleed);
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
