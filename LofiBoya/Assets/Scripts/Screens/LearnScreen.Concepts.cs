using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Öğren ekranının kavram oyunları:
    ///  • Zıtlar — "Sıcak olanı bul!": sıcak/soğuk, dolu/boş, açık/kapalı (iki büyük kart).
    ///  • Sıralama — "Önce ne olur?": büyüyen çiçek, çatlayan yumurta, eriyen dondurma, boyanan resim.
    ///    Kartlara sırayla dokunulur; doğru kart numaralı yerine uçar. 3–4 yaşta ilk aşama hazır gelir.
    ///  • Gölgeler — "Gölgesini bul!": boyalı resmin silueti gölgeler arasında aranır; bulununca gölge renklenir.
    ///  • Mevsimler — "Bunu hangi mevsimde görürüz?": resim ya da hava (kar, güneş…) dört mevsim ağacıyla eşleşir.
    /// Görseller LearnArt (kodla çizilmiş) ve boyama resimlerinden gelir; dış görsel kullanılmaz.
    /// </summary>
    public partial class LearnScreen
    {
        // ================= Menü kartları =================

        void ConceptMenuCards()
        {
            var opp = MenuCard(9, Loc.T("opposites"), Loc.T("opposites_sub"), 0.82f, () => StartGame(Mode.Opposites));
            MenuPic(opp, LearnArt.HotCup, new Vector2(-46, 0), 104, 6);
            MenuPic(opp, LearnArt.ColdGlass, new Vector2(46, 0), 104, -6);

            var seq = MenuCard(10, Loc.T("sequence"), Loc.T("sequence_sub"), 0.9f, () => StartGame(Mode.Sequence));
            for (int i = 0; i < 3; i++) MenuPic(seq, LearnArt.Plant(i), new Vector2((i - 1) * 74, 0), 84, 0);

            var sh = MenuCard(11, Loc.T("shadows"), Loc.T("shadows_sub"), 0.98f, () => StartGame(Mode.Shadows));
            // Pofuduk ve gölgesi
            var shadow = UIKit.Box(sh, "Shadow", Theme.Text.WithAlpha(0.85f), MascotArt.BodyPlain);
            shadow.type = Image.Type.Simple;
            shadow.preserveAspect = true;
            shadow.raycastTarget = false;
            shadow.rectTransform.anchorMin = shadow.rectTransform.anchorMax = new Vector2(0.5f, 0.80f);
            shadow.rectTransform.anchoredPosition = new Vector2(40, -4);
            shadow.rectTransform.sizeDelta = new Vector2(110, 96);
            var m = Mascot.Create(sh, 110, false);
            m.SetInteractive(false);
            var mrt = (RectTransform)m.transform;
            mrt.anchorMin = mrt.anchorMax = new Vector2(0.5f, 0.80f);
            mrt.anchoredPosition = new Vector2(-40, 4);

            var se = MenuCard(12, Loc.T("seasons"), Loc.T("seasons_sub"), 1.06f, () => StartGame(Mode.Seasons));
            MenuPic(se, LearnArt.Tree(1), new Vector2(-48, 0), 104, 0);
            MenuPic(se, LearnArt.Tree(3), new Vector2(48, 0), 104, 0);
        }

        static void MenuPic(RectTransform card, Sprite sprite, Vector2 pos, float size, float rot)
        {
            var img = UIKit.Box(card, "Pic", Color.white, sprite);
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.80f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(size, size);
            rt.localRotation = Quaternion.Euler(0, 0, rot);
        }

        // ================= Zıtlar =================

        static readonly string[] OppWords = { "hot", "cold", "full", "empty", "open", "closed" };
        readonly List<int> _oppBag = new();

        static (Sprite a, Sprite b) OppSprites(int pair) => pair switch
        {
            0 => (LearnArt.HotCup, LearnArt.ColdGlass),
            1 => (LearnArt.JarFull, LearnArt.JarEmpty),
            _ => (LearnArt.BoxOpen, LearnArt.BoxClosed),
        };

        void OppositeRound()
        {
            // torba: üç zıt çiftin hepsi sorulmadan tekrar gelmez
            if (_oppBag.Count == 0) { _oppBag.AddRange(new[] { 0, 1, 2 }); Shuffle(_oppBag); }
            int pair = _oppBag[0];
            _oppBag.RemoveAt(0);
            bool wantA = Random.value < 0.5f;
            string want = OppWords[pair * 2 + (wantA ? 0 : 1)];
            _wantKey = "opp_" + want;
            _prompt.text = Loc.T("opp_q_" + want);
            var (spA, spB) = OppSprites(pair);

            var order = new List<int> { 0, 1 };
            Shuffle(order);
            for (int i = 0; i < 2; i++)
            {
                int side = order[i];
                string word = OppWords[pair * 2 + side];
                var slot = PictureCard(OptionSlot(i, 2), side == 0 ? spA : spB, 0.1f + i * 0.08f, 0.03f);
                bool right = (side == 0) == wantA;
                string name = Loc.T("w_" + word);
                var b = slot.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => Answer(right, slot, name, null, 2.2f, "opp_" + word));
                A11y.Label(b, Names.Cap(name));
            }
        }

        /// <summary>Beyaz, hafif eğik resim kartı (seçenek). Kartın kendisi dokunma alanıdır.</summary>
        RectTransform PictureCard((Vector2 pos, Vector2 size) place, Sprite sprite, float delay, float pad = 0.1f)
        {
            var slot = UIKit.Node("Option", _area);
            slot.Place(new Vector2(0.5f, 0.5f), place.pos, place.size);
            var face = UIKit.Card(slot, "Card", Color.white, 0.5f, 6f);
            ((RectTransform)face.transform.parent).Stretch();
            face.raycastTarget = true;
            var img = UIKit.Box(face.transform, "Pic", Color.white, sprite);
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = false;
            float m = Mathf.Min(place.size.x, place.size.y) * pad;
            img.rectTransform.Stretch(m, m, m, m);
            slot.localScale = Vector3.zero;
            Tween.Pop(slot, delay, 0.45f);
            return slot;
        }

        // ================= Sıralama =================

        readonly List<int> _seqBag = new();
        int _seqNext, _seqKind;
        readonly List<RectTransform> _seqSlots = new();

        void SequenceRound()
        {
            // 3: boyanan resim (çizgi → yarı boyalı → boyalı); aşamalar ince fark, yalnızca 7+ yaşta
            if (_seqBag.Count == 0) { _seqBag.AddRange(SaveSystem.AgeBand >= 2 ? new[] { 0, 1, 2, 3 } : new[] { 0, 1, 2 }); Shuffle(_seqBag); }
            int kind = _seqBag[0];
            _seqBag.RemoveAt(0);
            _seqKind = kind;
            _prompt.text = Loc.T("seq_q");

            // aşamaların görselleri (önce → sonra)
            var pics = new List<(Sprite s, Texture t)>();
            if (kind == 3 && TryColoringStages(pics)) { }
            else
            {
                if (kind == 3) kind = Random.Range(0, 3);
                _seqKind = kind;
                for (int st = 0; st < 3; st++)
                    pics.Add((kind == 0 ? LearnArt.Plant(st) : kind == 1 ? LearnArt.Egg(st) : LearnArt.IceCream(st), null));
            }
            int n = pics.Count;

            // üstte numaralı yuvalar
            _seqSlots.Clear();
            var board = UIKit.Card(_area, "Board", Theme.Card.WithAlpha(0.85f), 0.6f, 4f);
            var brt = (RectTransform)board.transform.parent;
            brt.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(980, 360));
            float step = 315f;
            for (int i = 0; i < n; i++)
            {
                var slot = UIKit.Box(board.transform, "Slot", Theme.Text.WithAlpha(0.07f), null, 0.6f);
                slot.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2((i - (n - 1) * 0.5f) * step, -10), new Vector2(285, 285));
                var num = UIKit.Label(slot.transform, (i + 1).ToString(), 110, Theme.Text.WithAlpha(0.25f), TextAnchor.MiddleCenter, FontStyle.Bold);
                num.rectTransform.Stretch();
                // sıra oku: yuvaların arasında
                if (i < n - 1)
                {
                    var arrow = UIKit.Label(board.transform, "›", 90, Theme.Text.WithAlpha(0.35f), TextAnchor.MiddleCenter, FontStyle.Bold);
                    arrow.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2((i + 0.5f - (n - 1) * 0.5f) * step, -6), new Vector2(40, 120));
                }
                _seqSlots.Add(slot.rectTransform);
            }

            // 3–4 yaş: ilk aşama yerinde gelir, çocuk "sonra ne olur?"u bulur
            _seqNext = SaveSystem.AgeBand == 0 ? 1 : 0;
            if (_seqNext == 1)
            {
                var first = SeqCard(pics[0], Vector2.zero, 0.15f);
                first.SetParent(_seqSlots[0], false);
                first.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(285, 285));
                _prompt.text = Loc.T("seq_next");
            }

            // altta karışık kartlar (hiçbir zaman zaten doğru sırada gelmez)
            var order = new List<int>();
            for (int i = _seqNext; i < n; i++) order.Add(i);
            if (order.Count > 1) do Shuffle(order); while (IsSorted(order));
            int m = order.Count;
            for (int i = 0; i < m; i++)
            {
                int stage = order[i];
                var pos = new Vector2((i - (m - 1) * 0.5f) * 320f, -330);
                var card = SeqCard(pics[stage], pos, 0.25f + i * 0.08f);
                var b = card.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => SequenceTap(card, stage, n));
                A11y.Label(b, Names.Cap(Loc.T("seq_s" + _seqKind + "_" + stage)));
            }
        }

        static bool IsSorted(List<int> l)
        {
            for (int i = 1; i < l.Count; i++) if (l[i] < l[i - 1]) return false;
            return true;
        }

        RectTransform SeqCard((Sprite s, Texture t) pic, Vector2 pos, float delay)
        {
            var rt = UIKit.Node("Option", _area);
            rt.Place(new Vector2(0.5f, 0.5f), pos, new Vector2(290, 290));
            var face = UIKit.Card(rt, "Card", Color.white, 0.5f, 6f);
            ((RectTransform)face.transform.parent).Stretch();
            face.raycastTarget = true;
            Graphic g;
            if (pic.t != null)
            {
                var raw = UIKit.Node("Pic", face.transform).Stretch(22, 22, 22, 22).gameObject.AddComponent<RawImage>();
                raw.texture = pic.t;
                var fit = raw.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fit.aspectRatio = (float)pic.t.width / pic.t.height;
                g = raw;
            }
            else
            {
                var img = UIKit.Box(face.transform, "Pic", Color.white, pic.s);
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
                img.rectTransform.Stretch(22, 22, 22, 22);
                g = img;
            }
            g.raycastTarget = false;
            rt.localScale = Vector3.zero;
            Tween.Pop(rt, delay, 0.45f);
            return rt;
        }

        void SequenceTap(RectTransform card, int stage, int n)
        {
            if (_locked || card.parent != _area) return;
            bool right = stage == _seqNext;
            Record(right, null);
            if (!right)
            {
                _missed = true;
                Sfx.Wrong();
                Shake(card);
                return;
            }
            // doğru: kart numaralı yuvasına uçar
            Sfx.Pop();
            var slot = _seqSlots[stage];
            card.SetParent(slot, true);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            var start = card.anchoredPosition;
            float s0 = card.localScale.x;
            float target = 285f / 290f;
            Tween.To(card, Motion.Reduced ? 0.05f : 0.35f, k =>
            {
                card.anchoredPosition = Vector2.LerpUnclamped(start, Vector2.zero, k);
                card.localScale = Vector3.one * Mathf.LerpUnclamped(s0, target, k);
            }, Tween.OutBack);
            _seqNext++;
            if (_seqNext < n) return;
            Celebrate(slot);
            _prompt.text = Loc.T("seq_done");
            Later(2.6f, () => { if (this != null && _mode == Mode.Sequence) NextRound(); });
        }

        /// <summary>Bir boyama resminin boyanma aşamaları: çizgi, yarı boyalı, tam boyalı.</summary>
        bool TryColoringStages(List<(Sprite s, Texture t)> pics)
        {
            var pool = new List<int>();
            for (int i = 0; i < _animals.Count; i++) if (_animals[i].g == "kolay") pool.Add(i);
            if (pool.Count == 0) return false;
            var e = _animals[pool[Random.Range(0, pool.Count)]];
            var lv = Level.Load(e.g, e.id);
            if (lv == null) return false;
            int seed = Random.Range(0, 100000);
            foreach (float share in new[] { 0f, 0.35f, 1f })
            {
                var t = lv.RenderPastel(seed, 300, share);
                _textures.Add(t);
                pics.Add((null, t));
            }
            lv.Unload();
            return true;
        }

        // ================= Gölgeler =================

        void ShadowRound()
        {
            var pool = new List<int>();
            for (int i = 0; i < _animals.Count; i++) if (_animals[i].g == "kolay") pool.Add(i);
            if (pool.Count < 2) { AnimalRound(); return; }
            int ti;
            do ti = Random.Range(0, pool.Count); while (pool[ti] == _lastTarget && pool.Count > 1);
            int target = pool[ti];
            _lastTarget = target;
            var picks = PickOptions(ti, pool.Count, SaveSystem.OptionCount, (x, y) => AnimalConfusable(_animals[pool[x]].key, _animals[pool[y]].key));
            var e = _animals[target];
            _wantKey = e.key;
            _prompt.text = Loc.T("shadow_q");

            // üstte boyalı resim
            int seed = Random.Range(0, 100000);
            var lv = Level.Load(e.g, e.id);
            if (lv == null) { AnimalRound(); return; }
            var colored = lv.RenderPastel(seed, 360);
            lv.Unload();
            _textures.Add(colored);
            var top = UIKit.Card(_area, "Picture", Color.white, 0.5f, 6f);
            var trt = (RectTransform)top.transform.parent;
            trt.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 280), new Vector2(460, 460));
            trt.localRotation = Quaternion.Euler(0, 0, -3);
            RawIn(top.transform, colored, 26);
            trt.localScale = Vector3.zero;
            Tween.Pop(trt, 0.05f, 0.45f);

            // altta gölgeler
            int n = picks.Count;
            float w = n >= 4 ? 230 : n == 3 ? 300 : 360, gap = n >= 4 ? 244 : n == 3 ? 318 : 400;
            var shadowCol = (Color32)Theme.Text.WithAlpha(0.92f);
            for (int i = 0; i < n; i++)
            {
                var o = _animals[pool[picks[i]]];
                var olv = Level.Load(o.g, o.id);
                if (olv == null) continue;
                var sil = olv.RenderSilhouette(shadowCol, 300);
                olv.Unload();
                _textures.Add(sil);
                var slot = UIKit.Node("Option", _area);
                slot.Place(new Vector2(0.5f, 0.5f), new Vector2((i - (n - 1) * 0.5f) * gap, -330), new Vector2(w, w));
                var face = UIKit.Card(slot, "Card", Theme.Paper, 0.5f, 6f);
                ((RectTransform)face.transform.parent).Stretch();
                face.raycastTarget = true;
                var raw = RawIn(face.transform, sil, w * 0.1f);
                bool right = pool[picks[i]] == target;
                var b = slot.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                string name = o.name, key = o.key;
                b.onClick.AddListener(() =>
                {
                    // doğru gölge, renkli resme dönüşür
                    if (right && !_locked)
                        Tween.To(raw, 0.4f, k => { if (k > 0.5f && raw.texture != colored) raw.texture = colored; raw.color = Color.white.WithAlpha(Mathf.Abs(k - 0.5f) * 2f); });
                    Answer(right, slot, name, null, 2.4f, key);
                });
                A11y.Label(b, Names.Cap(name));
                slot.localScale = Vector3.zero;
                Tween.Pop(slot, 0.2f + i * 0.08f, 0.45f);
            }
        }

        RawImage RawIn(Transform parent, Texture tex, float pad)
        {
            var raw = UIKit.Node("Pic", parent).Stretch(pad, pad, pad, pad).gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            raw.raycastTarget = false;
            var fit = raw.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = (float)tex.width / tex.height;
            return raw;
        }

        // ================= Mevsimler =================

        /// <summary>
        /// Mevsim ipucu olan resimler (boyama resimlerinin anahtarları): kış, ilkbahar, yaz, sonbahar.
        /// Yalnızca mevsimi tartışmasız olanlar: fok, arı, ananas, kirpi gibi birden çok mevsime uyanlar alınmadı.
        /// </summary>
        static readonly string[][] SeasonKeys =
        {
            new[] { "snowman" },   // kutup hayvanları mevsim değil iklim: kışa eşlenmez (yanlış kavram olmasın)
            new[] { "tulip", "butterfly", "ladybug", "chick" },
            new[] { "watermelon", "ice", "strawberry", "starfish", "crab", "sunflower" },
            new[] { "acorn", "autumn", "scarecrow", "squirrel", "mushroom" },
        };

        void SeasonRound()
        {
            int target;
            do target = Random.Range(0, 4); while (target == _lastTarget);
            _lastTarget = target;
            _wantKey = "season_" + target;

            // ipucu: tek turlarda havanın resmi (kar, çiçek, güneş, yaprak), çift turlarda boyama resmi
            bool weather = _roundNo % 2 == 0;
            Texture2D tex = null;
            if (!weather)
            {
                var levels = new List<(string g, string id)>();
                foreach (var g in Level.Index.groups)
                    foreach (var l in g.levels)
                        if (System.Array.IndexOf(SeasonKeys[target], Names.KeyOf(l.id)) >= 0) levels.Add((g.name, l.id));
                if (levels.Count > 0)
                {
                    var (g, id) = levels[Random.Range(0, levels.Count)];
                    // çocuğun kendi boyadığı hâli varsa o, yoksa pastel renklerle boyanmış hâli
                    tex = SaveSystem.LoadThumb(g + "_" + id);
                    if (tex == null)
                    {
                        var lv = Level.Load(g, id);
                        if (lv != null) { tex = lv.RenderPastel(Random.Range(0, 100000), 360); lv.Unload(); }
                    }
                    if (tex != null) _textures.Add(tex);
                }
                weather = tex == null;
            }
            _prompt.text = weather ? Loc.T("season_w" + target) : Loc.T("season_q");

            var clue = UIKit.Card(_area, "Clue", Color.white, 0.5f, 6f);
            var crt = (RectTransform)clue.transform.parent;
            crt.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 290), new Vector2(430, 430));
            crt.localRotation = Quaternion.Euler(0, 0, 3);
            if (tex != null) RawIn(clue.transform, tex, 26);
            else
            {
                var img = UIKit.Box(clue.transform, "Pic", Color.white, LearnArt.Weather(target));
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
                img.rectTransform.Stretch(30, 30, 30, 30);
            }
            crt.localScale = Vector3.zero;
            Tween.Pop(crt, 0.05f, 0.45f);

            // seçenekler: mevsim ağaçları, hep aynı sırada (kış → sonbahar)
            var picks = new List<int> { target };
            var others = new List<int> { 0, 1, 2, 3 };
            others.Remove(target);
            Shuffle(others);
            for (int i = 0; picks.Count < Mathf.Min(4, SaveSystem.OptionCount); i++) picks.Add(others[i]);
            picks.Sort();
            int n = picks.Count;
            float w = n >= 4 ? 232 : n == 3 ? 300 : 340, gap = n >= 4 ? 246 : n == 3 ? 318 : 380;
            for (int i = 0; i < n; i++)
            {
                int s = picks[i];
                var slot = UIKit.Node("Option", _area);
                slot.Place(new Vector2(0.5f, 0.5f), new Vector2((i - (n - 1) * 0.5f) * gap, -330), new Vector2(w, w + 70));
                var face = UIKit.Card(slot, "Card", Color.white, 0.5f, 6f);
                ((RectTransform)face.transform.parent).Stretch();
                face.raycastTarget = true;
                var tree = UIKit.Box(face.transform, "Tree", Color.white, LearnArt.Tree(s));
                tree.type = Image.Type.Simple;
                tree.preserveAspect = true;
                tree.raycastTarget = false;
                tree.rectTransform.Stretch(14, 10, 14, 76);
                string name = Loc.T("season_" + s);
                var label = UIKit.Label(face.transform, Names.Cap(name), 40, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                label.rectTransform.Band(false, 70, 8, 10);
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 24;
                label.resizeTextMaxSize = 40;
                bool right = s == target;
                var b = slot.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => Answer(right, slot, name, null, 2.4f, "season_" + s));
                A11y.Label(b, Names.Cap(name));
                slot.localScale = Vector3.zero;
                Tween.Pop(slot, 0.2f + i * 0.08f, 0.45f);
            }
        }
    }
}
