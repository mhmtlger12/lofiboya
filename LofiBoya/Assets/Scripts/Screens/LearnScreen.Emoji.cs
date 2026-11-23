using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Emojili öğrenme turları (Fluent Emoji 3D, bkz. Emoji.cs):
    ///  • Ayır: meyve mi sebze mi, karada mı suda mı gökyüzünde mi (taşıtlar ve hayvanlar)
    ///  • Renkler: "Kırmızı olanı bul!" gerçek nesnelerle (renkleri elle seçildi, tartışmasız)
    ///  • Gölgeler: emojinin kendi gölgesi (saydam kısmı koyu boyanır)
    ///  • Sayma ve hafıza: aynı oyunlar, gerçek nesnelerle (meyveler, hayvanlar, taşıtlar…)
    /// Emoji adlarının çevirisi yok; turlar nesneyi adıyla sormaz (renk, kategori ya da şekil sorar).
    /// </summary>
    public partial class LearnScreen
    {
        // ================= Menü =================

        void EmojiMenuCards()
        {
            if (!Emoji.Available) return;
            var sort = MenuCard(Cell(13), Loc.T("sorting"), Loc.T("sorting_sub"), Theme.Hex("FFE7B8"), 1.14f, () => StartGame(Mode.Sort));
            MenuEmoji(sort, "fruit/red_apple", new Vector2(-46, 6), 92, 8);
            MenuEmoji(sort, "veg/carrot", new Vector2(46, -4), 92, -10);

            var world = MenuCard(Cell(14), Loc.T("world"), Loc.T("world_sub"), Theme.Hex("C9E6FF"), 1.22f, () => App.I.ShowStickerWorld());
            MenuEmoji(world, "animal/rabbit", new Vector2(-50, -6), 84, 6);
            MenuEmoji(world, "nature/sun_with_face", new Vector2(44, 22), 70, -8);
            MenuEmoji(world, "vehicle/automobile", new Vector2(36, -30), 74, 0);
        }

        static void MenuEmoji(RectTransform card, string id, Vector2 pos, float size, float rot)
        {
            var img = Emoji.Image(card, id, size);
            img.raycastTarget = false;
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.80f);
            img.rectTransform.anchoredPosition = pos;
            img.rectTransform.localRotation = Quaternion.Euler(0, 0, rot);
        }

        // ================= Ayır =================

        // Tartışmalı olanlar alınmadı: zeytin ve avokado (mutfakta sebze), mantar, kestane, yer fıstığı, zencefil;
        // ördek, penguen, timsah gibi hem karada hem suda yaşayanlar; uçamayan kuşlar.
        static readonly string[] SortFruit = { "red_apple", "green_apple", "banana", "blueberries", "cherries", "coconut", "grapes", "kiwi_fruit", "lemon", "mango", "melon", "peach", "pear", "pineapple", "strawberry", "tangerine", "watermelon" };
        static readonly string[] SortVeg = { "beans", "bell_pepper", "carrot", "cucumber", "ear_of_corn", "eggplant", "garlic", "hot_pepper", "leafy_green", "onion", "pea_pod", "potato", "tomato" };
        static readonly string[] LandVehicles = { "ambulance", "articulated_lorry", "automobile", "bicycle", "bus", "delivery_truck", "fire_engine", "kick_scooter", "minibus", "motor_scooter", "motorcycle", "pickup_truck", "police_car", "racing_car", "sport_utility_vehicle", "taxi", "tractor", "locomotive", "train", "tram", "bullet_train" };
        static readonly string[] WaterVehicles = { "canoe", "ferry", "passenger_ship", "sailboat", "ship", "speedboat", "place/motor_boat" };
        static readonly string[] SkyVehicles = { "airplane", "helicopter", "small_airplane", "rocket" };
        static readonly string[] LandAnimals = { "badger", "bison", "black_cat", "boar", "camel", "cat", "chipmunk", "cow", "deer", "dog", "elephant", "ewe", "giraffe", "goat", "gorilla", "horse", "kangaroo", "leopard", "llama", "monkey", "mouse", "orangutan", "ox", "pig", "rabbit", "raccoon", "ram", "rhinoceros", "skunk", "sloth", "tiger", "zebra", "snail", "wild/donkey", "wild/moose" };
        static readonly string[] WaterAnimals = { "dolphin", "fish", "tropical_fish", "blowfish", "whale", "shark", "octopus", "wild/jellyfish" };
        static readonly string[] SkyAnimals = { "bird", "dove", "eagle", "parrot", "owl", "bat", "butterfly", "honeybee", "wild/blackbird" };

        struct SortBin { public string Key, Icon; public Color Color; public string[] Items; public string Cat; }

        static string Full(string cat, string id) => id.Contains("/") ? id : cat + "/" + id;

        void SortRound()
        {
            _prompt.text = Loc.T("sort_q");
            bool little = SaveSystem.AgeBand == 0;
            int set = Random.Range(0, 3);
            var bins = new List<SortBin>();
            if (set == 0)
            {
                bins.Add(new SortBin { Key = "sort_fruit", Icon = "fruit/grapes", Color = Theme.Hex("FFC8D0"), Items = SortFruit, Cat = "fruit" });
                bins.Add(new SortBin { Key = "sort_veg", Icon = "veg/broccoli", Color = Theme.Mint, Items = SortVeg, Cat = "veg" });
            }
            else
            {
                bool vehicles = set == 1;
                string cat = vehicles ? "vehicle" : "animal";
                bins.Add(new SortBin { Key = "sort_land", Icon = "nature/deciduous_tree", Color = Theme.Hex("D8F0C8"), Items = vehicles ? LandVehicles : LandAnimals, Cat = cat });
                bins.Add(new SortBin { Key = "sort_water", Icon = "place/water_wave", Color = Theme.Sky, Items = vehicles ? WaterVehicles : WaterAnimals, Cat = cat });
                if (!little) bins.Add(new SortBin { Key = "sort_sky", Icon = "nature/cloud", Color = Theme.Hex("DCD6F7"), Items = vehicles ? SkyVehicles : SkyAnimals, Cat = cat });
            }

            // hangi kutuya ait bir nesne gelsin: kutular sırayla eşit şansta
            int right;
            do right = Random.Range(0, bins.Count); while (bins.Count > 1 && right == _lastTarget);
            _lastTarget = right;
            var pool = new List<string>();
            foreach (var id in bins[right].Items)
            {
                string full = Full(bins[right].Cat, id);
                if (Emoji.Has(full)) pool.Add(full);
            }
            if (pool.Count == 0) { ColorThingRound(); return; }
            string item = pool[Random.Range(0, pool.Count)];
            _wantKey = bins[right].Key;

            // üstte nesne
            var top = UIKit.Card(_area, "Item", Color.white, 0.5f, 6f);
            var trt = (RectTransform)top.transform.parent;
            trt.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(420, 420));
            trt.localRotation = Quaternion.Euler(0, 0, -3);
            var pic = Emoji.Image(top.transform, item, 330);
            pic.raycastTarget = false;
            pic.rectTransform.anchorMin = pic.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            trt.gameObject.AddComponent<Floaty>().Init(8f, 0.7f);
            trt.localScale = Vector3.zero;
            Tween.Pop(trt, 0.05f, 0.45f);

            // altta kutular
            int n = bins.Count;
            float w = n == 2 ? 420 : 300, gap = n == 2 ? 460 : 320;
            for (int i = 0; i < n; i++)
            {
                var bin = bins[i];
                var slot = UIKit.Node("Bin", _area);
                slot.Place(new Vector2(0.5f, 0.5f), new Vector2((i - (n - 1) * 0.5f) * gap, -360), new Vector2(w, 380));
                var face = UIKit.Card(slot, "Face", bin.Color, 0.5f, 6f);
                ((RectTransform)face.transform.parent).Stretch();
                face.raycastTarget = true;
                var icon = Emoji.Image(face.transform, bin.Icon, 170);
                icon.raycastTarget = false;
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.5f, 0.62f);
                var label = UIKit.Label(face.transform, Loc.T(bin.Key), 48, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                label.rectTransform.Band(false, 90, 26, 14);
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 26;
                label.resizeTextMaxSize = 48;
                bool ok = i == right;
                string name = Loc.T(bin.Key), key = bin.Key;
                var b = slot.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() =>
                {
                    if (_locked) return;
                    if (ok)
                    {
                        // nesne kutunun içine süzülür
                        var from = trt.anchoredPosition;
                        var to = slot.anchoredPosition + new Vector2(0, 40);
                        Tween.To(trt, 0.45f, k =>
                        {
                            trt.anchoredPosition = Vector2.Lerp(from, to, k) + new Vector2(0, Mathf.Sin(k * Mathf.PI) * 120f);
                            trt.localScale = Vector3.one * Mathf.Lerp(1f, 0.35f, k);
                        });
                        Sfx.Pop();
                    }
                    Answer(ok, slot, name, null, 1.6f, key);
                });
                A11y.Label(b, name);
                slot.localScale = Vector3.zero;
                Tween.Pop(slot, 0.2f + i * 0.08f, 0.45f);
            }
        }

        // ================= Renkler: gerçek nesneler =================

        void ColorThingRound()
        {
            // en az bir nesnesi olan renkler
            var colors = new List<int>();
            for (int c = 0; c < Names.Basic.Length; c++) if (Emoji.WithColor(c).Count > 0) colors.Add(c);
            if (colors.Count < 2) { ColorRound(); return; }
            int target;
            do target = colors[Random.Range(0, colors.Count)]; while (target == _lastTarget && colors.Count > 1);
            _lastTarget = target;

            int n = Mathf.Min(SaveSystem.OptionCount, colors.Count);
            var targets = Emoji.WithColor(target);
            var picks = new List<(int c, string id)> { (target, targets[Random.Range(0, targets.Count)]) };
            var others = new List<int>(colors);
            others.Remove(target);
            // kırmızı-pembe, turuncu-kahverengi gibi karışabilenler küçük yaşta yan yana gelmesin
            if (SaveSystem.AgeBand < 2) others.RemoveAll(o => ColorConfusable(target, o));
            Shuffle(others);
            foreach (var o in others)
            {
                if (picks.Count >= n) break;
                // çeldiricide sorulan renk hiç görünmesin (yeşil sorulurken yeşil yapraklı lale/üzüm çıkmaz)
                var pool = Emoji.WithColor(o);
                pool.RemoveAll(id => Emoji.Shows(id, target));
                if (pool.Count > 0) picks.Add((o, pool[Random.Range(0, pool.Count)]));
            }
            if (picks.Count < 2) { ColorRound(); return; }
            Shuffle(picks);

            var t = Names.Basic[target];
            _wantKey = BasicKeys[target];
            _promptVoice = Names.Cap(Loc.T("find_thing", t.name));
            _prompt.text = _promptVoice;

            for (int i = 0; i < picks.Count; i++)
            {
                var (c, id) = picks[i];
                var slot = PictureCard(OptionSlot(i, picks.Count), Emoji.Get(id), 0.1f + i * 0.08f, 0.14f);
                slot.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0 ? 1 : -1) * 3f);
                string name = Names.Basic[c].name, key = BasicKeys[c];
                var b = slot.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => Answer(c == target, slot, name, key: key));
                A11y.Label(b, Names.Cap(name));
            }
        }

        // ================= Gölgeler: emojinin gölgesi =================

        static readonly string[] ShadowCats = { "animal", "fruit", "veg", "vehicle", "play", "food", "nature", "animalface", "object" };

        void EmojiShadowRound()
        {
            string cat = ShadowCats[Random.Range(0, ShadowCats.Length)];
            int n = SaveSystem.OptionCount;
            var ids = Emoji.Pick(cat, n);
            if (ids.Count < 2) { ShadowRound(); return; }
            n = ids.Count;
            int target = Random.Range(0, n);
            string want = ids[target];
            _wantKey = null;   // emoji adlarının çevirisi yok: rapordaki "karıştırılanlar"a girmesin
            _prompt.text = Loc.T("shadow_q");

            var top = UIKit.Card(_area, "Picture", Color.white, 0.5f, 6f);
            var trt = (RectTransform)top.transform.parent;
            trt.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 280), new Vector2(460, 460));
            trt.localRotation = Quaternion.Euler(0, 0, -3);
            var pic = Emoji.Image(top.transform, want, 360);
            pic.raycastTarget = false;
            pic.rectTransform.anchorMin = pic.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            trt.localScale = Vector3.zero;
            Tween.Pop(trt, 0.05f, 0.45f);

            float w = n >= 4 ? 230 : n == 3 ? 300 : 360, gap = n >= 4 ? 244 : n == 3 ? 318 : 400;
            var shadow = Theme.Text.WithAlpha(0.92f);
            for (int i = 0; i < n; i++)
            {
                var slot = UIKit.Node("Option", _area);
                slot.Place(new Vector2(0.5f, 0.5f), new Vector2((i - (n - 1) * 0.5f) * gap, -330), new Vector2(w, w));
                var face = UIKit.Card(slot, "Card", Theme.Paper, 0.5f, 6f);
                ((RectTransform)face.transform.parent).Stretch();
                face.raycastTarget = true;
                var sil = Emoji.Image(face.transform, ids[i], w * 0.78f);
                sil.raycastTarget = false;
                sil.color = shadow;   // saydam olmayan her yer düz koyu: gerçek gölge
                sil.material = Emoji.Silhouette;
                sil.rectTransform.anchorMin = sil.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                bool right = i == target;
                var b = slot.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() =>
                {
                    // doğru gölge renklenir
                    if (right && !_locked)
                        Tween.To(sil, 0.5f, k =>
                        {
                            // gölge solar, renkli resim olarak geri gelir
                            if (k >= 0.5f && sil.material != sil.defaultMaterial) sil.material = null;
                            sil.color = k < 0.5f ? shadow.WithAlpha(1f - k * 2f) : Color.white.WithAlpha((k - 0.5f) * 2f);
                        });
                    Answer(right, slot, null, null, 2.4f);
                });
                A11y.Label(b, Loc.T("a11y_card", i + 1));
                slot.localScale = Vector3.zero;
                Tween.Pop(slot, 0.2f + i * 0.08f, 0.45f);
            }
        }

        // ================= Sayma ve hafıza için nesneler =================

        static readonly string[] CountCats = { "fruit", "veg", "animal", "play", "food", "vehicle", "nature" };
        static readonly string[] MemoryCats = { "fruit", "veg", "animal", "animalface", "vehicle", "play", "food", "nature", "wild", "object" };

        /// <summary>Sayma turu için bir nesne (çoğu turda), ya da null (eski kalp/yıldız/çiçek/balon).</summary>
        static string CountEmoji()
        {
            if (!Emoji.Available || Random.value > 0.6f) return null;
            var one = Emoji.Pick(CountCats[Random.Range(0, CountCats.Length)], 1);
            return one.Count > 0 ? one[0] : null;
        }

        /// <summary>Hafıza destesi: aynı kategoriden farklı nesneler (çoğu turda), ya da null.</summary>
        static List<string> MemoryDeck(int pairs)
        {
            if (!Emoji.Available || Random.value > 0.65f) return null;
            var deck = Emoji.Pick(MemoryCats[Random.Range(0, MemoryCats.Length)], pairs);
            return deck.Count == pairs ? deck : null;
        }
    }
}
