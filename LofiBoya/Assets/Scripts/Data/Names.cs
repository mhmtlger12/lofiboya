using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace LofiBoya
{
    /// <summary>
    /// Renklerin ve resimlerin adları, seçili dilde.
    /// Her ad iki biçimde: yalın ad ("aslan", "lion") ve cümle içi biçim (Türkçe "aslanı", Almanca
    /// "den Löwen", Fransızca "le lion", Rusça "льва"...). "{0} bul!" gibi kalıplar ikinciyi kullanır.
    /// </summary>
    public static class Names
    {
        // ---------------- Renkler ----------------

        static readonly (string key, Color color)[] BasicColors =
        {
            ("red", Theme.Hex("FF5A5F")), ("orange", Theme.Hex("FFA14A")), ("yellow", Theme.Hex("FFD93D")),
            ("green", Theme.Hex("6BCB77")), ("blue", Theme.Hex("4D96FF")), ("purple", Theme.Hex("9B72F2")),
            ("pink", Theme.Hex("FF8FC7")), ("brown", Theme.Hex("A9744F")),
        };

        /// <summary>Temel renkler (öğrenme oyunu ve sayılı mod), seçili dilde.</summary>
        public static (string name, string acc, Color color)[] Basic
        {
            get
            {
                var arr = new (string, string, Color)[BasicColors.Length];
                for (int i = 0; i < arr.Length; i++)
                {
                    var (n, f) = Word(BasicColors[i].key);
                    arr[i] = (n, f, BasicColors[i].color);
                }
                return arr;
            }
        }

        /// <summary>Herhangi bir rengin çocukların bileceği adı (seçili dilde).</summary>
        public static string ColorName(Color c) => Word(ColorKey(c)).name;

        public static string ColorKey(Color c)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            h *= 360f;
            if (v < 0.22f) return "black";
            if (s < 0.12f) return v > 0.85f ? "white" : "gray";
            if (s < 0.3f && v > 0.8f && (h < 50 || h > 340)) return h < 20 || h > 340 ? "pink" : "cream";
            if (h < 15 || h >= 345) return v > 0.8f && s < 0.55f ? "pink" : "red";
            if (h < 40) return v < 0.65f ? "brown" : "orange";
            if (h < 65) return v < 0.6f ? "brown" : "yellow";
            if (h < 160) return "green";
            if (h < 195) return "turquoise";
            if (h < 250) return v < 0.45f ? "navy" : "blue";
            if (h < 290) return "purple";
            return "pink";
        }

        // ---------------- Resimler ----------------

        /// <summary>Dosya adındaki İngilizce kelime -> tablo anahtarı.</summary>
        static readonly Dictionary<string, string> Words = new()
        {
            ["cat"] = "cat", ["kitten"] = "cat", ["dog"] = "dog", ["puppy"] = "puppy", ["lion"] = "lion", ["tiger"] = "tiger",
            ["elephant"] = "elephant", ["giraffe"] = "giraffe", ["unicorn"] = "unicorn", ["panda"] = "panda", ["owl"] = "owl",
            ["fox"] = "fox", ["wolf"] = "wolf", ["deer"] = "deer", ["duck"] = "duck", ["dolphin"] = "dolphin", ["bear"] = "bear",
            ["teddybear"] = "teddybear", ["strawberry"] = "strawberry", ["watermelon"] = "watermelon", ["ice"] = "ice",
            ["ghost"] = "ghost", ["witch"] = "witch", ["snowman"] = "snowman", ["bunny"] = "bunny", ["rabbit"] = "bunny",
            ["duckling"] = "duck", ["cupcake"] = "cupcake", ["moon"] = "moon", ["sunflower"] = "sunflower", ["tulip"] = "tulip",
            ["flowers"] = "flowers", ["bouquet"] = "flowers", ["flower"] = "flower", ["butterfly"] = "butterfly",
            ["scarecrow"] = "scarecrow", ["rose"] = "rose", ["alligator"] = "alligator", ["crab"] = "crab",
            ["cricket"] = "cricket", ["mandala"] = "mandala", ["pineapple"] = "pineapple", ["seal"] = "seal",
            ["christmas"] = "christmastree", ["apple"] = "apple", ["acorn"] = "acorn",
            ["rocket"] = "rocket", ["house"] = "house", ["fish"] = "fish", ["turtle"] = "turtle", ["snail"] = "snail", ["car"] = "car",
            ["frog"] = "frog", ["ladybug"] = "ladybug", ["whale"] = "whale", ["mouse"] = "mouse", ["penguin"] = "penguin",
            ["cow"] = "cow", ["bee"] = "bee", ["octopus"] = "octopus", ["hedgehog"] = "hedgehog", ["hippo"] = "hippo",
            ["koala"] = "koala", ["monkey"] = "monkey", ["parrot"] = "parrot", ["squirrel"] = "squirrel", ["starfish"] = "starfish",
            ["jellyfish"] = "jellyfish", ["hen"] = "hen", ["camel"] = "camel",
        };

        /// <summary>Dosya adından anlaşılmayan resimler.</summary>
        static readonly Dictionary<string, string> ById = new()
        {
            ["artrose-easter-6122817"] = "chick", ["artrose-easter-6122832"] = "bunny",
            ["artrose-easter-6122870"] = "chick", ["artrose-easter-6122901"] = "duck",
            ["purepageart-coloring-page-9943298"] = "bear", ["purepageart-coloring-page-9943301"] = "lion",
            ["purepageart-cute-9005708"] = "pony", ["purepageart-cute-9069178"] = "ghost",
            // yeni resimler (dosya adından anlaşılmayanlar, resme bakılarak)
            ["artrose-easter-6122827"] = "egg", ["artrose-easter-6122844"] = "chick", ["artrose-easter-6122931"] = "sheep",
            ["dreamdigitalartist-give-birth-6307791"] = "bear", ["gdj-ai-generated-8815287"] = "rose",
            ["gdj-sea-horse-6387536"] = "seahorse", ["marcos101-pie-4861996"] = "cupcake",
            ["openclipart-vectors-black-and-white-1295370"] = "flower", ["openclipart-vectors-black-and-white-1300567"] = "flower",
            ["clker-free-vector-images-tiger-lily-296789"] = "lily", ["evilfavoriteart-ai-generated-9379533"] = "flower",
            ["hatimestea-coloring-10120325"] = "cat", ["ihaveplants-ai-generated-8895759"] = "mushroom",
            ["ihaveplants-ai-generated-8895760"] = "mushroom", ["ajk347-ai-generated-9140229"] = "girl",
            ["bassloom-decoration-9971887"] = "fireplace", ["chenik-fajarhidayah-christmas-8467676"] = "stocking",
            ["coloringpage-ai-generated-8855030"] = "unicorn", ["gdj-floral-2746540"] = "mandala",
            ["kaylinart-drawing-2151087"] = "mandala", ["missywhimsyart-ai-generated-8673312"] = "princess",
            ["missywhimsyart-ai-generated-8673315"] = "princess", ["missywhimsyart-ai-generated-8673319"] = "princess",
            ["lofi-bus-001"] = "bus",
            ["lofi-train-001"] = "train",
            ["lofi-airplane-001"] = "airplane",
            ["lofi-ship-001"] = "ship",
            ["lofi-tractor-001"] = "tractor",
            ["lofi-vegbasket-001"] = "vegbasket",
            ["lofi-garden-001"] = "garden",
            ["lofi-doctor-001"] = "doctor",
            ["lofi-firefighter-001"] = "firefighter",
            ["lofi-chef-001"] = "chef",
            ["lofi-astronaut-001"] = "astronaut",
            ["lofi-toothbrush-001"] = "toothbrush",
            ["lofi-handwash-001"] = "handwash",
            ["lofi-feelings-001"] = "feelings",
            ["lofi-school-001"] = "school",
            ["lofi-instruments-001"] = "instruments",
            ["lofi-playground-001"] = "playground",
            ["lofi-autumn-001"] = "autumn",
            ["lofi-rainbow-001"] = "rainbow",
            ["lofi-toys-001"] = "toys",
            ["lofi-zenfish-001"] = "zenfish",
            ["lofi-space-001"] = "space",
            ["lofi-farm-001"] = "farm",
            ["lofi-startiles-001"] = "startiles",
            ["lofi-hexflowers-001"] = "hexflowers",
            ["lofi-kilim-001"] = "kilim",
            ["lofi-tuliptile-001"] = "tuliptile",
            ["lofi-city-001"] = "city",
            ["lofi-reef-001"] = "reef",
            ["lofi-flowergarden-001"] = "flowergarden",
            ["lofi-balloons-001"] = "balloons",
            ["lofi-market-001"] = "market",
            ["lofi-bookshelf-001"] = "bookshelf",
            ["lofi-puffy-001"] = "puffy",
            ["lofi-num-1"] = "num1",
            ["lofi-num-2"] = "num2",
            ["lofi-num-3"] = "num3",
            ["lofi-num-4"] = "num4",
            ["lofi-num-5"] = "num5",
            ["lofi-num-6"] = "num6",
            ["lofi-num-7"] = "num7",
            ["lofi-num-8"] = "num8",
            ["lofi-num-9"] = "num9",
            ["lofi-num-10"] = "num10",
            ["seiripan-ai-generated-9623276"] = "girl", ["seiripan-cute-9623238"] = "beach", ["seiripan-cute-9623248"] = "beach",
        };

        /// <summary>Resmin ad anahtarı ("lofi-whale-001" -> "whale"), bilinmiyorsa null.</summary>
        public static string KeyOf(string id) => Key(id);

        static string Key(string id)
        {
            if (ById.TryGetValue(id, out var k)) return k;
            var parts = id.ToLowerInvariant().Split('-');
            for (int i = 0; i + 1 < parts.Length; i++)
                if (parts[i] == "polar" && parts[i + 1] == "bear") return "polarbear";
            for (int i = 1; i < parts.Length; i++)
                if (Words.TryGetValue(parts[i], out var w)) return w;
            return null;
        }

        /// <summary>Tekil bir nesne değil, sahne/çoğul ad: Türkçede "Bu bir ..." denmez.</summary>
        static readonly HashSet<string> SceneKeys = new() { "puffy", "feelings", "toothbrush", "handwash", "instruments", "balloons", "hexflowers", "space" };
        /// <summary>Desen sayfaları: "Resimler" öğrenme oyununda birbirine benzediği için sorulmaz.</summary>
        static readonly HashSet<string> PatternKeys = new() { "startiles", "hexflowers", "kilim", "tuliptile", "zenfish", "mandala" };

        public static bool IsScene(string id) { var k = Key(id); return k != null && SceneKeys.Contains(k); }

        /// <summary>"Resimleri bul" oyununa uygun mu (sahne ve desen değil, tek bir şey).</summary>
        public static bool IsLearnable(string id)
        {
            var k = Key(id);
            return k != null && !SceneKeys.Contains(k) && !PatternKeys.Contains(k) && !k.StartsWith("num");
        }

        /// <summary>Galeri ve başlıklarda görünen ad.</summary>
        public static string LevelTitle(string id)
        {
            var k = Key(id);
            return Cap(k != null ? Word(k).name : Loc.T("picture"));
        }

        /// <summary>Şekil adı (yalın, cümle içi): anahtar "circle", "square", "triangle", "rectangle", "star", "heart".</summary>
        public static (string name, string acc) Shape(string key) => Word("shape_" + key);

        /// <summary>Rakam adı (yalın, cümle içi): 1..10.</summary>
        public static (string name, string acc) Num(int n) => Word("num" + n);

        /// <summary>Duygu yüzü adı (yalın, cümle içi): "happy", "sad", "angry", "surprised".</summary>
        public static (string name, string acc) Feel(string key) => Word("feel_" + key);

        /// <summary>Resmin adı biliniyorsa (yalın ad, cümle içi biçim).</summary>
        public static bool TryAnimal(string id, out string name, out string accusative)
        {
            var k = Key(id);
            name = accusative = null;
            if (k == null) return false;
            (name, accusative) = Word(k);
            return true;
        }

        /// <summary>Baş harfi büyük (Türkçede İ/ı kuralıyla).</summary>
        public static string Cap(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var culture = Loc.Lang == "tr" ? new CultureInfo("tr-TR") : CultureInfo.InvariantCulture;
            return char.ToUpper(s[0], culture) + s.Substring(1);
        }

        // ---------------- Tablo ----------------

        static Dictionary<string, string[]> _words;

        /// <summary>Tablodaki bir anahtarın (renk, şekil, duygu, resim) adı: rapor ve yeni oyunlar için.</summary>
        public static (string name, string form) Word(string key)
        {
            if (_words == null) Parse();
            if (key == null || !_words.TryGetValue(key, out var row)) return (key, key);
            int i = Loc.Index * 2;
            if (i + 1 >= row.Length) i = 0;
            return (row[i], row[i + 1]);
        }

        static void Parse()
        {
            _words = new Dictionary<string, string[]>();
            foreach (var raw in Table.Split('\n'))
            {
                var line = raw.Trim('\r');
                if (line.Length == 0 || line.StartsWith("#")) continue;
                var p = line.Split('|');
                var vals = new string[p.Length - 1];
                for (int i = 1; i < p.Length; i++) vals[i - 1] = p[i].Trim();
                _words[p[0].Trim()] = vals;
            }
        }

        // anahtar | en ad | en biçim | tr | tr | de | de | fr | fr | es | es | it | it | pt | pt | ru | ru | ar | ar | hi | hi
        const string Table = @"
# renkler
red|red|red|kırmızı|kırmızıyı|Rot|Rot|rouge|le rouge|rojo|el rojo|rosso|il rosso|vermelho|o vermelho|красный|красный|أحمر|الأحمر|लाल|लाल
orange|orange|orange|turuncu|turuncuyu|Orange|Orange|orange|l'orange|naranja|el naranja|arancione|l'arancione|laranja|o laranja|оранжевый|оранжевый|برتقالي|البرتقالي|नारंगी|नारंगी
yellow|yellow|yellow|sarı|sarıyı|Gelb|Gelb|jaune|le jaune|amarillo|el amarillo|giallo|il giallo|amarelo|o amarelo|жёлтый|жёлтый|أصفر|الأصفر|पीला|पीला
green|green|green|yeşil|yeşili|Grün|Grün|vert|le vert|verde|el verde|verde|il verde|verde|o verde|зелёный|зелёный|أخضر|الأخضر|हरा|हरा
blue|blue|blue|mavi|maviyi|Blau|Blau|bleu|le bleu|azul|el azul|blu|il blu|azul|o azul|синий|синий|أزرق|الأزرق|नीला|नीला
purple|purple|purple|mor|moru|Lila|Lila|violet|le violet|morado|el morado|viola|il viola|roxo|o roxo|фиолетовый|фиолетовый|بنفسجي|البنفسجي|बैंगनी|बैंगनी
pink|pink|pink|pembe|pembeyi|Rosa|Rosa|rose|le rose|rosa|el rosa|rosa|il rosa|cor-de-rosa|o cor-de-rosa|розовый|розовый|وردي|الوردي|गुलाबी|गुलाबी
brown|brown|brown|kahverengi|kahverengiyi|Braun|Braun|marron|le marron|marrón|el marrón|marrone|il marrone|castanho|o castanho|коричневый|коричневый|بني|البني|भूरा|भूरा
black|black|black|siyah|siyahı|Schwarz|Schwarz|noir|le noir|negro|el negro|nero|il nero|preto|o preto|чёрный|чёрный|أسود|الأسود|काला|काला
white|white|white|beyaz|beyazı|Weiß|Weiß|blanc|le blanc|blanco|el blanco|bianco|il bianco|branco|o branco|белый|белый|أبيض|الأبيض|सफ़ेद|सफ़ेद
gray|gray|gray|gri|griyi|Grau|Grau|gris|le gris|gris|el gris|grigio|il grigio|cinzento|o cinzento|серый|серый|رمادي|الرمادي|स्लेटी|स्लेटी
cream|cream|cream|krem|kremi|Creme|Creme|crème|le crème|crema|el crema|crema|il crema|creme|o creme|кремовый|кремовый|كريمي|الكريمي|क्रीम|क्रीम
turquoise|turquoise|turquoise|turkuaz|turkuazı|Türkis|Türkis|turquoise|le turquoise|turquesa|el turquesa|turchese|il turchese|turquesa|o turquesa|бирюзовый|бирюзовый|فيروزي|الفيروزي|फ़िरोज़ी|फ़िरोज़ी
navy|navy blue|navy blue|lacivert|laciverti|Dunkelblau|Dunkelblau|bleu marine|le bleu marine|azul marino|el azul marino|blu scuro|il blu scuro|azul-marinho|o azul-marinho|тёмно-синий|тёмно-синий|كحلي|الكحلي|गहरा नीला|गहरा नीला
# resimler
cat|cat|the cat|kedi|kediyi|Katze|die Katze|chat|le chat|gato|el gato|gatto|il gatto|gato|o gato|кошка|кошку|قطة|القطة|बिल्ली|बिल्ली
dog|dog|the dog|köpek|köpeği|Hund|den Hund|chien|le chien|perro|el perro|cane|il cane|cão|o cão|собака|собаку|كلب|الكلب|कुत्ता|कुत्ता
puppy|puppy|the puppy|yavru köpek|yavru köpeği|Welpe|den Welpen|chiot|le chiot|cachorro|el cachorro|cucciolo|il cucciolo|cachorrinho|o cachorrinho|щенок|щенка|جرو|الجرو|पिल्ला|पिल्ला
lion|lion|the lion|aslan|aslanı|Löwe|den Löwen|lion|le lion|león|el león|leone|il leone|leão|o leão|лев|льва|أسد|الأسد|शेर|शेर
tiger|tiger|the tiger|kaplan|kaplanı|Tiger|den Tiger|tigre|le tigre|tigre|el tigre|tigre|la tigre|tigre|o tigre|тигр|тигра|نمر|النمر|बाघ|बाघ
elephant|elephant|the elephant|fil|fili|Elefant|den Elefanten|éléphant|l'éléphant|elefante|el elefante|elefante|l'elefante|elefante|o elefante|слон|слона|فيل|الفيل|हाथी|हाथी
giraffe|giraffe|the giraffe|zürafa|zürafayı|Giraffe|die Giraffe|girafe|la girafe|jirafa|la jirafa|giraffa|la giraffa|girafa|a girafa|жираф|жирафа|زرافة|الزرافة|जिराफ़|जिराफ़
unicorn|unicorn|the unicorn|tek boynuzlu at|tek boynuzlu atı|Einhorn|das Einhorn|licorne|la licorne|unicornio|el unicornio|unicorno|l'unicorno|unicórnio|o unicórnio|единорог|единорога|يونيكورن|اليونيكورن|यूनिकॉर्न|यूनिकॉर्न
panda|panda|the panda|panda|pandayı|Panda|den Panda|panda|le panda|panda|el panda|panda|il panda|panda|o panda|панда|панду|باندا|الباندا|पांडा|पांडा
owl|owl|the owl|baykuş|baykuşu|Eule|die Eule|hibou|le hibou|búho|el búho|gufo|il gufo|coruja|a coruja|сова|сову|بومة|البومة|उल्लू|उल्लू
fox|fox|the fox|tilki|tilkiyi|Fuchs|den Fuchs|renard|le renard|zorro|el zorro|volpe|la volpe|raposa|a raposa|лиса|лису|ثعلب|الثعلب|लोमड़ी|लोमड़ी
wolf|wolf|the wolf|kurt|kurdu|Wolf|den Wolf|loup|le loup|lobo|el lobo|lupo|il lupo|lobo|o lobo|волк|волка|ذئب|الذئب|भेड़िया|भेड़िया
deer|deer|the deer|geyik|geyiği|Reh|das Reh|cerf|le cerf|ciervo|el ciervo|cervo|il cervo|veado|o veado|олень|оленя|غزال|الغزال|हिरण|हिरण
duck|duck|the duck|ördek|ördeği|Ente|die Ente|canard|le canard|pato|el pato|papera|la papera|pato|o pato|утка|утку|بطة|البطة|बत्तख|बत्तख
dolphin|dolphin|the dolphin|yunus|yunusu|Delfin|den Delfin|dauphin|le dauphin|delfín|el delfín|delfino|il delfino|golfinho|o golfinho|дельфин|дельфина|دلفين|الدلفين|डॉल्फ़िन|डॉल्फ़िन
bear|bear|the bear|ayı|ayıyı|Bär|den Bären|ours|l'ours|oso|el oso|orso|l'orso|urso|o urso|медведь|медведя|دب|الدب|भालू|भालू
teddybear|teddy bear|the teddy bear|oyuncak ayı|oyuncak ayıyı|Teddybär|den Teddybären|nounours|le nounours|osito|el osito|orsacchiotto|l'orsacchiotto|ursinho|o ursinho|мишка|мишку|دبدوب|الدبدوب|टेडी बियर|टेडी बियर
polarbear|polar bear|the polar bear|kutup ayısı|kutup ayısını|Eisbär|den Eisbären|ours polaire|l'ours polaire|oso polar|el oso polar|orso polare|l'orso polare|urso-polar|o urso-polar|белый медведь|белого медведя|دب قطبي|الدب القطبي|ध्रुवीय भालू|ध्रुवीय भालू
strawberry|strawberry|the strawberry|çilek|çileği|Erdbeere|die Erdbeere|fraise|la fraise|fresa|la fresa|fragola|la fragola|morango|o morango|клубника|клубнику|فراولة|الفراولة|स्ट्रॉबेरी|स्ट्रॉबेरी
watermelon|watermelon|the watermelon|karpuz|karpuzu|Wassermelone|die Wassermelone|pastèque|la pastèque|sandía|la sandía|anguria|l'anguria|melancia|a melancia|арбуз|арбуз|بطيخ|البطيخ|तरबूज़|तरबूज़
ice|ice cream|the ice cream|dondurma|dondurmayı|Eis|das Eis|glace|la glace|helado|el helado|gelato|il gelato|gelado|o gelado|мороженое|мороженое|آيس كريم|الآيس كريم|आइसक्रीम|आइसक्रीम
ghost|ghost|the ghost|hayalet|hayaleti|Gespenst|das Gespenst|fantôme|le fantôme|fantasma|el fantasma|fantasma|il fantasma|fantasma|o fantasma|привидение|привидение|شبح|الشبح|भूत|भूत
witch|witch|the witch|cadı|cadıyı|Hexe|die Hexe|sorcière|la sorcière|bruja|la bruja|strega|la strega|bruxa|a bruxa|ведьма|ведьму|ساحرة|الساحرة|चुड़ैल|चुड़ैल
snowman|snowman|the snowman|kardan adam|kardan adamı|Schneemann|den Schneemann|bonhomme de neige|le bonhomme de neige|muñeco de nieve|el muñeco de nieve|pupazzo di neve|il pupazzo di neve|boneco de neve|o boneco de neve|снеговик|снеговика|رجل الثلج|رجل الثلج|स्नोमैन|स्नोमैन
bunny|bunny|the bunny|tavşan|tavşanı|Hase|den Hasen|lapin|le lapin|conejo|el conejo|coniglio|il coniglio|coelho|o coelho|зайчик|зайчика|أرنب|الأرنب|खरगोश|खरगोश
chick|chick|the chick|civciv|civcivi|Küken|das Küken|poussin|le poussin|pollito|el pollito|pulcino|il pulcino|pintainho|o pintainho|цыплёнок|цыплёнка|صوص|الصوص|चूज़ा|चूज़ा
pony|pony|the pony|midilli|midilliyi|Pony|das Pony|poney|le poney|poni|el poni|pony|il pony|pónei|o pónei|пони|пони|مهر|المهر|टट्टू|टट्टू
cupcake|cupcake|the cupcake|kek|keki|Muffin|den Muffin|cupcake|le cupcake|magdalena|la magdalena|cupcake|il cupcake|queque|o queque|кекс|кекс|كب كيك|الكب كيك|कपकेक|कपकेक
egg|egg|the egg|yumurta|yumurtayı|Osterei|das Osterei|œuf|l'œuf|huevo|el huevo|uovo|l'uovo|ovo|o ovo|яйцо|яйцо|بيضة|البيضة|अंडा|अंडा
sheep|sheep|the sheep|kuzu|kuzuyu|Schaf|das Schaf|mouton|le mouton|oveja|la oveja|pecora|la pecora|ovelha|a ovelha|овечка|овечку|خروف|الخروف|भेड़|भेड़
moon|moon|the moon|ay|ayı|Mond|den Mond|lune|la lune|luna|la luna|luna|la luna|lua|a lua|луна|луну|قمر|القمر|चाँद|चाँद
sunflower|sunflower|the sunflower|ayçiçeği|ayçiçeğini|Sonnenblume|die Sonnenblume|tournesol|le tournesol|girasol|el girasol|girasole|il girasole|girassol|o girassol|подсолнух|подсолнух|دوار الشمس|دوار الشمس|सूरजमुखी|सूरजमुखी
tulip|tulip|the tulip|lale|laleyi|Tulpe|die Tulpe|tulipe|la tulipe|tulipán|el tulipán|tulipano|il tulipano|túlipa|a túlipa|тюльпан|тюльпан|توليب|التوليب|ट्यूलिप|ट्यूलिप
lily|lily|the lily|zambak|zambağı|Lilie|die Lilie|lys|le lys|lirio|el lirio|giglio|il giglio|lírio|o lírio|лилия|лилию|زنبق|الزنبق|लिली|लिली
flowers|flowers|the flowers|çiçekler|çiçekleri|Blumen|die Blumen|fleurs|les fleurs|flores|las flores|fiori|i fiori|flores|as flores|цветы|цветы|أزهار|الأزهار|फूल|फूल
flower|flower|the flower|çiçek|çiçeği|Blume|die Blume|fleur|la fleur|flor|la flor|fiore|il fiore|flor|a flor|цветок|цветок|زهرة|الزهرة|फूल|फूल
rose|rose|the rose|gül|gülü|Rose|die Rose|rose|la rose|rosa|la rosa|rosa|la rosa|rosa|a rosa|роза|розу|وردة|الوردة|गुलाब|गुलाब
butterfly|butterfly|the butterfly|kelebek|kelebeği|Schmetterling|den Schmetterling|papillon|le papillon|mariposa|la mariposa|farfalla|la farfalla|borboleta|a borboleta|бабочка|бабочку|فراشة|الفراشة|तितली|तितली
scarecrow|scarecrow|the scarecrow|korkuluk|korkuluğu|Vogelscheuche|die Vogelscheuche|épouvantail|l'épouvantail|espantapájaros|el espantapájaros|spaventapasseri|lo spaventapasseri|espantalho|o espantalho|пугало|пугало|فزاعة|الفزاعة|बिजूका|बिजूका
alligator|crocodile|the crocodile|timsah|timsahı|Krokodil|das Krokodil|crocodile|le crocodile|cocodrilo|el cocodrilo|coccodrillo|il coccodrillo|crocodilo|o crocodilo|крокодил|крокодила|تمساح|التمساح|मगरमच्छ|मगरमच्छ
crab|crab|the crab|yengeç|yengeci|Krabbe|die Krabbe|crabe|le crabe|cangrejo|el cangrejo|granchio|il granchio|caranguejo|o caranguejo|краб|краба|سلطعون|السلطعون|केकड़ा|केकड़ा
cricket|grasshopper|the grasshopper|çekirge|çekirgeyi|Grashüpfer|den Grashüpfer|sauterelle|la sauterelle|saltamontes|el saltamontes|cavalletta|la cavalletta|gafanhoto|o gafanhoto|кузнечик|кузнечика|جندب|الجندب|टिड्डा|टिड्डा
seahorse|seahorse|the seahorse|denizatı|denizatını|Seepferdchen|das Seepferdchen|hippocampe|l'hippocampe|caballito de mar|el caballito de mar|cavalluccio marino|il cavalluccio marino|cavalo-marinho|o cavalo-marinho|морской конёк|морского конька|حصان البحر|حصان البحر|समुद्री घोड़ा|समुद्री घोड़ा
seal|seal|the seal|fok|foku|Robbe|die Robbe|phoque|le phoque|foca|la foca|foca|la foca|foca|a foca|тюлень|тюленя|فقمة|الفقمة|सील|सील
pineapple|pineapple|the pineapple|ananas|ananası|Ananas|die Ananas|ananas|l'ananas|piña|la piña|ananas|l'ananas|ananás|o ananás|ананас|ананас|أناناس|الأناناس|अनानास|अनानास
apple|apple|the apple|elma|elmayı|Apfel|den Apfel|pomme|la pomme|manzana|la manzana|mela|la mela|maçã|a maçã|яблоко|яблоко|تفاحة|التفاحة|सेब|सेब
acorn|acorn|the acorn|meşe palamudu|meşe palamudunu|Eichel|die Eichel|gland|le gland|bellota|la bellota|ghianda|la ghianda|bolota|a bolota|жёлудь|жёлудь|بلوطة|البلوطة|बलूत|बलूत
mushroom|mushroom|the mushroom|mantar|mantarı|Pilz|den Pilz|champignon|le champignon|seta|la seta|fungo|il fungo|cogumelo|o cogumelo|гриб|гриб|فطر|الفطر|मशरूम|मशरूम
mandala|mandala|the mandala|mandala|mandalayı|Mandala|das Mandala|mandala|le mandala|mandala|el mandala|mandala|il mandala|mandala|a mandala|мандала|мандалу|ماندالا|الماندالا|मंडला|मंडला
christmastree|Christmas tree|the Christmas tree|yılbaşı ağacı|yılbaşı ağacını|Weihnachtsbaum|den Weihnachtsbaum|sapin de Noël|le sapin de Noël|árbol de Navidad|el árbol de Navidad|albero di Natale|l'albero di Natale|árvore de Natal|a árvore de Natal|ёлка|ёлку|شجرة الميلاد|شجرة الميلاد|क्रिसमस ट्री|क्रिसमस ट्री
stocking|Christmas stocking|the stocking|yılbaşı çorabı|yılbaşı çorabını|Weihnachtsstrumpf|den Weihnachtsstrumpf|chaussette de Noël|la chaussette de Noël|calcetín de Navidad|el calcetín de Navidad|calza di Natale|la calza di Natale|meia de Natal|a meia de Natal|рождественский носок|рождественский носок|جورب العيد|جورب العيد|क्रिसमस मोज़ा|क्रिसमस मोज़ा
fireplace|fireplace|the fireplace|şömine|şömineyi|Kamin|den Kamin|cheminée|la cheminée|chimenea|la chimenea|camino|il camino|lareira|a lareira|камин|камин|مدفأة|المدفأة|अंगीठी|अंगीठी
princess|princess|the princess|prenses|prensesi|Prinzessin|die Prinzessin|princesse|la princesse|princesa|la princesa|principessa|la principessa|princesa|a princesa|принцесса|принцессу|أميرة|الأميرة|राजकुमारी|राजकुमारी
girl|girl|the girl|kız|kızı|Mädchen|das Mädchen|fille|la fille|niña|la niña|bambina|la bambina|menina|a menina|девочка|девочку|بنت|البنت|लड़की|लड़की
beach|beach|the beach|plaj|plajı|Strand|den Strand|plage|la plage|playa|la playa|spiaggia|la spiaggia|praia|a praia|пляж|пляж|شاطئ|الشاطئ|समुद्र तट|समुद्र तट
rocket|rocket|the rocket|roket|roketi|Rakete|die Rakete|fusée|la fusée|cohete|el cohete|razzo|il razzo|foguete|o foguete|ракета|ракету|صاروخ|الصاروخ|रॉकेट|रॉकेट
house|house|the house|ev|evi|Haus|das Haus|maison|la maison|casa|la casa|casa|la casa|casa|a casa|дом|дом|بيت|البيت|घर|घर
fish|fish|the fish|balık|balığı|Fisch|den Fisch|poisson|le poisson|pez|el pez|pesce|il pesce|peixe|o peixe|рыбка|рыбку|سمكة|السمكة|मछली|मछली
turtle|turtle|the turtle|kaplumbağa|kaplumbağayı|Schildkröte|die Schildkröte|tortue|la tortue|tortuga|la tortuga|tartaruga|la tartaruga|tartaruga|a tartaruga|черепаха|черепаху|سلحفاة|السلحفاة|कछुआ|कछुए
snail|snail|the snail|salyangoz|salyangozu|Schnecke|die Schnecke|escargot|l'escargot|caracol|el caracol|lumaca|la lumaca|caracol|o caracol|улитка|улитку|حلزون|الحلزون|घोंघा|घोंघे
car|car|the car|araba|arabayı|Auto|das Auto|voiture|la voiture|coche|el coche|macchina|la macchina|carro|o carro|машинка|машинку|سيارة|السيارة|गाड़ी|गाड़ी
frog|frog|the frog|kurbağa|kurbağayı|Frosch|den Frosch|grenouille|la grenouille|rana|la rana|rana|la rana|sapo|o sapo|лягушка|лягушку|ضفدع|الضفدع|मेंढक|मेंढक
ladybug|ladybug|the ladybug|uğur böceği|uğur böceğini|Marienkäfer|den Marienkäfer|coccinelle|la coccinelle|mariquita|la mariquita|coccinella|la coccinella|joaninha|a joaninha|божья коровка|божью коровку|دعسوقة|الدعسوقة|लेडीबग|लेडीबग
whale|whale|the whale|balina|balinayı|Wal|den Wal|baleine|la baleine|ballena|la ballena|balena|la balena|baleia|a baleia|кит|кита|حوت|الحوت|व्हेल|व्हेल
mouse|mouse|the mouse|fare|fareyi|Maus|die Maus|souris|la souris|ratón|el ratón|topolino|il topolino|ratinho|o ratinho|мышка|мышку|فأر|الفأر|चूहा|चूहे
penguin|penguin|the penguin|penguen|pengueni|Pinguin|den Pinguin|pingouin|le pingouin|pingüino|el pingüino|pinguino|il pinguino|pinguim|o pinguim|пингвин|пингвина|بطريق|البطريق|पेंगुइन|पेंगुइन
cow|cow|the cow|inek|ineği|Kuh|die Kuh|vache|la vache|vaca|la vaca|mucca|la mucca|vaca|a vaca|корова|корову|بقرة|البقرة|गाय|गाय
bee|bee|the bee|arı|arıyı|Biene|die Biene|abeille|l'abeille|abeja|la abeja|ape|l'ape|abelha|a abelha|пчёлка|пчёлку|نحلة|النحلة|मधुमक्खी|मधुमक्खी
octopus|octopus|the octopus|ahtapot|ahtapotu|Krake|den Kraken|pieuvre|la pieuvre|pulpo|el pulpo|polpo|il polpo|polvo|o polvo|осьминог|осьминога|أخطبوط|الأخطبوط|ऑक्टोपस|ऑक्टोपस
hedgehog|hedgehog|the hedgehog|kirpi|kirpiyi|Igel|den Igel|hérisson|le hérisson|erizo|el erizo|riccio|il riccio|ouriço|o ouriço|ёжик|ёжика|قنفذ|القنفذ|हेजहॉग|हेजहॉग
hippo|hippo|the hippo|su aygırı|su aygırını|Nilpferd|das Nilpferd|hippopotame|l'hippopotame|hipopótamo|el hipopótamo|ippopotamo|l'ippopotamo|hipopótamo|o hipopótamo|бегемот|бегемота|فرس النهر|فرس النهر|दरियाई घोड़ा|दरियाई घोड़े
koala|koala|the koala|koala|koalayı|Koala|den Koala|koala|le koala|koala|el koala|koala|il koala|coala|o coala|коала|коалу|كوالا|الكوالا|कोआला|कोआला
monkey|monkey|the monkey|maymun|maymunu|Affe|den Affen|singe|le singe|mono|el mono|scimmietta|la scimmietta|macaco|o macaco|обезьянка|обезьянку|قرد|القرد|बंदर|बंदर
parrot|parrot|the parrot|papağan|papağanı|Papagei|den Papagei|perroquet|le perroquet|loro|el loro|pappagallo|il pappagallo|papagaio|o papagaio|попугай|попугая|ببغاء|الببغاء|तोता|तोते
squirrel|squirrel|the squirrel|sincap|sincabı|Eichhörnchen|das Eichhörnchen|écureuil|l'écureuil|ardilla|la ardilla|scoiattolo|lo scoiattolo|esquilo|o esquilo|белочка|белочку|سنجاب|السنجاب|गिलहरी|गिलहरी
starfish|starfish|the starfish|deniz yıldızı|deniz yıldızını|Seestern|den Seestern|étoile de mer|l'étoile de mer|estrella de mar|la estrella de mar|stella marina|la stella marina|estrela-do-mar|a estrela-do-mar|морская звезда|морскую звезду|نجم البحر|نجم البحر|तारामछली|तारामछली
jellyfish|jellyfish|the jellyfish|denizanası|denizanasını|Qualle|die Qualle|méduse|la méduse|medusa|la medusa|medusa|la medusa|água-viva|a água-viva|медуза|медузу|قنديل البحر|قنديل البحر|जेलीफ़िश|जेलीफ़िश
hen|hen|the hen|tavuk|tavuğu|Huhn|das Huhn|poule|la poule|gallina|la gallina|gallina|la gallina|galinha|a galinha|курочка|курочку|دجاجة|الدجاجة|मुर्गी|मुर्गी
camel|camel|the camel|deve|deveyi|Kamel|das Kamel|chameau|le chameau|camello|el camello|cammello|il cammello|camelo|o camelo|верблюд|верблюда|جمل|الجمل|ऊँट|ऊँट
num1|number one|number one|bir sayısı|bir sayısını|Zahl Eins|die Zahl Eins|chiffre un|le chiffre un|número uno|el número uno|numero uno|il numero uno|número um|o número um|цифра один|цифру один|رقم واحد|الرقم واحد|अंक एक|अंक एक
num2|number two|number two|iki sayısı|iki sayısını|Zahl Zwei|die Zahl Zwei|chiffre deux|le chiffre deux|número dos|el número dos|numero due|il numero due|número dois|o número dois|цифра два|цифру два|رقم اثنان|الرقم اثنان|अंक दो|अंक दो
num3|number three|number three|üç sayısı|üç sayısını|Zahl Drei|die Zahl Drei|chiffre trois|le chiffre trois|número tres|el número tres|numero tre|il numero tre|número três|o número três|цифра три|цифру три|رقم ثلاثة|الرقم ثلاثة|अंक तीन|अंक तीन
num4|number four|number four|dört sayısı|dört sayısını|Zahl Vier|die Zahl Vier|chiffre quatre|le chiffre quatre|número cuatro|el número cuatro|numero quattro|il numero quattro|número quatro|o número quatro|цифра четыре|цифру четыре|رقم أربعة|الرقم أربعة|अंक चार|अंक चार
num5|number five|number five|beş sayısı|beş sayısını|Zahl Fünf|die Zahl Fünf|chiffre cinq|le chiffre cinq|número cinco|el número cinco|numero cinque|il numero cinque|número cinco|o número cinco|цифра пять|цифру пять|رقم خمسة|الرقم خمسة|अंक पाँच|अंक पाँच
num6|number six|number six|altı sayısı|altı sayısını|Zahl Sechs|die Zahl Sechs|chiffre six|le chiffre six|número seis|el número seis|numero sei|il numero sei|número seis|o número seis|цифра шесть|цифру шесть|رقم ستة|الرقم ستة|अंक छह|अंक छह
num7|number seven|number seven|yedi sayısı|yedi sayısını|Zahl Sieben|die Zahl Sieben|chiffre sept|le chiffre sept|número siete|el número siete|numero sette|il numero sette|número sete|o número sete|цифра семь|цифру семь|رقم سبعة|الرقم سبعة|अंक सात|अंक सात
num8|number eight|number eight|sekiz sayısı|sekiz sayısını|Zahl Acht|die Zahl Acht|chiffre huit|le chiffre huit|número ocho|el número ocho|numero otto|il numero otto|número oito|o número oito|цифра восемь|цифру восемь|رقم ثمانية|الرقم ثمانية|अंक आठ|अंक आठ
num9|number nine|number nine|dokuz sayısı|dokuz sayısını|Zahl Neun|die Zahl Neun|chiffre neuf|le chiffre neuf|número nueve|el número nueve|numero nove|il numero nove|número nove|o número nove|цифра девять|цифру девять|رقم تسعة|الرقم تسعة|अंक नौ|अंक नौ
num10|number ten|number ten|on sayısı|on sayısını|Zahl Zehn|die Zahl Zehn|nombre dix|le nombre dix|número diez|el número diez|numero dieci|il numero dieci|número dez|o número dez|число десять|число десять|رقم عشرة|الرقم عشرة|अंक दस|अंक दस
bus|bus|the bus|otobüs|otobüsü|Bus|den Bus|bus|le bus|autobús|el autobús|autobus|l'autobus|autocarro|o autocarro|автобус|автобус|حافلة|الحافلة|बस|बस
train|train|the train|tren|treni|Zug|den Zug|train|le train|tren|el tren|treno|il treno|comboio|o comboio|поезд|поезд|قطار|القطار|रेलगाड़ी|रेलगाड़ी
airplane|airplane|the airplane|uçak|uçağı|Flugzeug|das Flugzeug|avion|l'avion|avión|el avión|aereo|l'aereo|avião|o avião|самолёт|самолёт|طائرة|الطائرة|हवाई जहाज़|हवाई जहाज़
ship|sailboat|the sailboat|yelkenli|yelkenliyi|Segelboot|das Segelboot|voilier|le voilier|velero|el velero|barca a vela|la barca a vela|veleiro|o veleiro|парусник|парусник|مركب شراعي|المركب الشراعي|नाव|नाव
tractor|tractor|the tractor|traktör|traktörü|Traktor|den Traktor|tracteur|le tracteur|tractor|el tractor|trattore|il trattore|trator|o trator|трактор|трактор|جرار|الجرار|ट्रैक्टर|ट्रैक्टर
vegbasket|vegetable basket|the vegetable basket|sebze sepeti|sebze sepetini|Gemüsekorb|den Gemüsekorb|panier de légumes|le panier de légumes|cesta de verduras|la cesta de verduras|cesto di verdure|il cesto di verdure|cesto de legumes|o cesto de legumes|корзина с овощами|корзину с овощами|سلة الخضار|سلة الخضار|सब्ज़ियों की टोकरी|सब्ज़ियों की टोकरी
garden|vegetable garden|the vegetable garden|sebze bahçesi|sebze bahçesini|Gemüsegarten|den Gemüsegarten|potager|le potager|huerto|el huerto|orto|l'orto|horta|a horta|огород|огород|حديقة الخضار|حديقة الخضار|सब्ज़ी का बगीचा|सब्ज़ी के बगीचे
doctor|doctor|the doctor|doktor|doktoru|Ärztin|die Ärztin|médecin|le médecin|doctora|la doctora|dottoressa|la dottoressa|médica|a médica|врач|врача|طبيبة|الطبيبة|डॉक्टर|डॉक्टर
firefighter|firefighter|the firefighter|itfaiyeci|itfaiyeciyi|Feuerwehr|die Feuerwehr|pompier|le pompier|bombero|el bombero|pompiere|il pompiere|bombeiro|o bombeiro|пожарный|пожарного|إطفائي|الإطفائي|दमकलकर्मी|दमकलकर्मी
chef|chef|the chef|aşçı|aşçıyı|Koch|den Koch|cuisinier|le cuisinier|cocinero|el cocinero|cuoco|il cuoco|cozinheiro|o cozinheiro|повар|повара|طاهٍ|الطاهي|रसोइया|रसोइए
astronaut|astronaut|the astronaut|astronot|astronotu|Astronaut|den Astronauten|astronaute|l'astronaute|astronauta|el astronauta|astronauta|l'astronauta|astronauta|o astronauta|космонавт|космонавта|رائد فضاء|رائد الفضاء|अंतरिक्ष यात्री|अंतरिक्ष यात्री
toothbrush|brushing teeth|the tooth brushing picture|diş fırçalama|diş fırçalama resmini|Zähneputzen|das Zähneputzen|brossage des dents|le brossage des dents|cepillado de dientes|el cepillado de dientes|lavaggio dei denti|il lavaggio dei denti|escovagem dos dentes|a escovagem dos dentes|Чистим зубы|картинку «Чистим зубы»|تنظيف الأسنان|صورة تنظيف الأسنان|दाँत साफ़ करना|दाँत साफ़ करने का चित्र
handwash|washing hands|the hand washing picture|el yıkama|el yıkama resmini|Händewaschen|das Händewaschen|lavage des mains|le lavage des mains|lavado de manos|el lavado de manos|lavaggio delle mani|il lavaggio delle mani|lavagem das mãos|a lavagem das mãos|Моем руки|картинку «Моем руки»|غسل اليدين|صورة غسل اليدين|हाथ धोना|हाथ धोने का चित्र
feelings|feelings|the feelings|duygular|duyguları|Gefühle|die Gefühle|émotions|les émotions|emociones|las emociones|emozioni|le emozioni|emoções|as emoções|эмоции|эмоции|المشاعر|المشاعر|भावनाएँ|भावनाएँ
school|school bag|the school bag|okul çantası|okul çantasını|Schulranzen|den Schulranzen|cartable|le cartable|mochila escolar|la mochila escolar|zaino di scuola|lo zaino di scuola|mochila da escola|a mochila da escola|школьный рюкзак|школьный рюкзак|حقيبة المدرسة|حقيبة المدرسة|स्कूल बैग|स्कूल बैग
instruments|musical instruments|the musical instruments|müzik aletleri|müzik aletlerini|Musikinstrumente|die Musikinstrumente|instruments de musique|les instruments de musique|instrumentos musicales|los instrumentos musicales|strumenti musicali|gli strumenti musicali|instrumentos musicais|os instrumentos musicais|музыкальные инструменты|музыкальные инструменты|الآلات الموسيقية|الآلات الموسيقية|वाद्य यंत्र|वाद्य यंत्रों
playground|playground|the playground|oyun parkı|oyun parkını|Spielplatz|den Spielplatz|aire de jeux|l'aire de jeux|parque infantil|el parque infantil|parco giochi|il parco giochi|parque infantil|o parque infantil|детская площадка|детскую площадку|ملعب الأطفال|ملعب الأطفال|खेल का मैदान|खेल के मैदान
autumn|autumn tree|the autumn tree|sonbahar ağacı|sonbahar ağacını|Herbstbaum|den Herbstbaum|arbre d'automne|l'arbre d'automne|árbol de otoño|el árbol de otoño|albero d'autunno|l'albero d'autunno|árvore de outono|a árvore de outono|осеннее дерево|осеннее дерево|شجرة الخريف|شجرة الخريف|पतझड़ का पेड़|पतझड़ के पेड़
rainbow|rainbow|the rainbow|gökkuşağı|gökkuşağını|Regenbogen|den Regenbogen|arc-en-ciel|l'arc-en-ciel|arcoíris|el arcoíris|arcobaleno|l'arcobaleno|arco-íris|o arco-íris|радуга|радугу|قوس قزح|قوس قزح|इंद्रधनुष|इंद्रधनुष
toys|toy box|the toy box|oyuncak kutusu|oyuncak kutusunu|Spielzeugkiste|die Spielzeugkiste|coffre à jouets|le coffre à jouets|baúl de juguetes|el baúl de juguetes|cesta dei giocattoli|la cesta dei giocattoli|caixa de brinquedos|a caixa de brinquedos|коробка с игрушками|коробку с игрушками|صندوق الألعاب|صندوق الألعاب|खिलौनों का डिब्बा|खिलौनों के डिब्बे
zenfish|patterned fish|the patterned fish|desenli balık|desenli balığı|Musterfisch|den Musterfisch|poisson à motifs|le poisson à motifs|pez con dibujos|el pez con dibujos|pesce decorato|il pesce decorato|peixe com padrões|o peixe com padrões|узорная рыбка|узорную рыбку|سمكة مزخرفة|السمكة المزخرفة|डिज़ाइन वाली मछली|डिज़ाइन वाली मछली
space|space|space|uzay|uzayı|Weltall|das Weltall|espace|l'espace|espacio|el espacio|spazio|lo spazio|espaço|o espaço|космос|космос|الفضاء|الفضاء|अंतरिक्ष|अंतरिक्ष
farm|farm|the farm|çiftlik|çiftliği|Bauernhof|den Bauernhof|ferme|la ferme|granja|la granja|fattoria|la fattoria|quinta|a quinta|ферма|ферму|مزرعة|المزرعة|खेत|खेत
startiles|star pattern|the star pattern|yıldız deseni|yıldız desenini|Sternmuster|das Sternmuster|motif d'étoiles|le motif d'étoiles|mosaico de estrellas|el mosaico de estrellas|motivo a stelle|il motivo a stelle|padrão de estrelas|o padrão de estrelas|звёздный узор|звёздный узор|زخرفة النجوم|زخرفة النجوم|तारों का डिज़ाइन|तारों के डिज़ाइन
hexflowers|honeycomb flowers|the honeycomb flowers|petek çiçekleri|petek çiçeklerini|Wabenblumen|die Wabenblumen|fleurs en nid d'abeille|les fleurs en nid d'abeille|flores en panal|las flores en panal|fiori a nido d'ape|i fiori a nido d'ape|flores em favo|as flores em favo|цветочные соты|цветочные соты|أزهار خلية النحل|أزهار خلية النحل|छत्ते के फूल|छत्ते के फूलों
kilim|kilim rug|the kilim rug|kilim|kilimi|Kelim|den Kelim|kilim|le kilim|kilim|el kilim|kilim|il kilim|kilim|o kilim|килим|килим|سجادة كليم|سجادة الكليم|किलिम गलीचा|किलिम गलीचे
tuliptile|tulip tile|the tulip tile|lale çinisi|lale çinisini|Tulpenfliese|die Tulpenfliese|carreau aux tulipes|le carreau aux tulipes|azulejo de tulipanes|el azulejo de tulipanes|piastrella con tulipani|la piastrella con tulipani|azulejo de túlipas|o azulejo de túlipas|изразец с тюльпанами|изразец с тюльпанами|بلاطة التوليب|بلاطة التوليب|ट्यूलिप टाइल|ट्यूलिप टाइल
city|city|the city|şehir|şehri|Stadt|die Stadt|ville|la ville|ciudad|la ciudad|città|la città|cidade|a cidade|город|город|مدينة|المدينة|शहर|शहर
reef|coral reef|the coral reef|mercan resifi|mercan resifini|Korallenriff|das Korallenriff|récif de corail|le récif de corail|arrecife de coral|el arrecife de coral|barriera corallina|la barriera corallina|recife de coral|o recife de coral|коралловый риф|коралловый риф|الشعاب المرجانية|الشعاب المرجانية|मूंगा चट्टान|मूंगा चट्टान
flowergarden|flower garden|the flower garden|çiçek bahçesi|çiçek bahçesini|Blumengarten|den Blumengarten|jardin fleuri|le jardin fleuri|jardín de flores|el jardín de flores|giardino fiorito|il giardino fiorito|jardim de flores|o jardim de flores|цветник|цветник|حديقة الزهور|حديقة الزهور|फूलों का बगीचा|फूलों के बगीचे
balloons|hot air balloons|the hot air balloons|sıcak hava balonları|sıcak hava balonlarını|Heißluftballons|die Heißluftballons|montgolfières|les montgolfières|globos aerostáticos|los globos aerostáticos|mongolfiere|le mongolfiere|balões de ar quente|os balões de ar quente|воздушные шары|воздушные шары|المناطيد|المناطيد|गर्म हवा के गुब्बारे|गर्म हवा के गुब्बारों
market|market stall|the market stall|pazar tezgâhı|pazar tezgâhını|Marktstand|den Marktstand|étal du marché|l'étal du marché|puesto del mercado|el puesto del mercado|bancarella|la bancarella|banca do mercado|a banca do mercado|прилавок на рынке|прилавок на рынке|بسطة السوق|بسطة السوق|बाज़ार की दुकान|बाज़ार की दुकान
bookshelf|bookshelf|the bookshelf|kitaplık|kitaplığı|Bücherregal|das Bücherregal|bibliothèque|la bibliothèque|estantería|la estantería|libreria|la libreria|estante|a estante|книжная полка|книжную полку|رف الكتب|رف الكتب|किताबों की अलमारी|किताबों की अलमारी
feel_happy|happy face|the happy face|mutlu yüz|mutlu yüzü|fröhliches Gesicht|das fröhliche Gesicht|visage content|le visage content|cara feliz|la cara feliz|faccina felice|la faccina felice|cara feliz|a cara feliz|весёлое личико|весёлое личико|وجه سعيد|الوجه السعيد|खुश चेहरा|खुश चेहरा
feel_sad|sad face|the sad face|üzgün yüz|üzgün yüzü|trauriges Gesicht|das traurige Gesicht|visage triste|le visage triste|cara triste|la cara triste|faccina triste|la faccina triste|cara triste|a cara triste|грустное личико|грустное личико|وجه حزين|الوجه الحزين|उदास चेहरा|उदास चेहरा
feel_angry|angry face|the angry face|kızgın yüz|kızgın yüzü|wütendes Gesicht|das wütende Gesicht|visage fâché|le visage fâché|cara enfadada|la cara enfadada|faccina arrabbiata|la faccina arrabbiata|cara zangada|a cara zangada|сердитое личико|сердитое личико|وجه غاضب|الوجه الغاضب|गुस्से वाला चेहरा|गुस्से वाला चेहरा
feel_surprised|surprised face|the surprised face|şaşkın yüz|şaşkın yüzü|überraschtes Gesicht|das überraschte Gesicht|visage surpris|le visage surpris|cara de sorpresa|la cara de sorpresa|faccina sorpresa|la faccina sorpresa|cara surpreendida|a cara surpreendida|удивлённое личико|удивлённое личико|وجه متفاجئ|الوجه المتفاجئ|हैरान चेहरा|हैरान चेहरा
feel_scared|scared face|the scared face|korkmuş yüz|korkmuş yüzü|ängstliches Gesicht|das ängstliche Gesicht|visage effrayé|le visage effrayé|cara asustada|la cara asustada|faccina spaventata|la faccina spaventata|cara assustada|a cara assustada|испуганное личико|испуганное личико|وجه خائف|الوجه الخائف|डरा हुआ चेहरा|डरा हुआ चेहरा
feel_proud|proud face|the proud face|gururlu yüz|gururlu yüzü|stolzes Gesicht|das stolze Gesicht|visage fier|le visage fier|cara orgullosa|la cara orgullosa|faccina orgogliosa|la faccina orgogliosa|cara orgulhosa|a cara orgulhosa|гордое личико|гордое личико|وجه فخور|الوجه الفخور|गर्व वाला चेहरा|गर्व वाला चेहरा
feel_calm|calm face|the calm face|sakin yüz|sakin yüzü|ruhiges Gesicht|das ruhige Gesicht|visage calme|le visage calme|cara tranquila|la cara tranquila|faccina serena|la faccina serena|cara calma|a cara calma|спокойное личико|спокойное личико|وجه هادئ|الوجه الهادئ|शांत चेहरा|शांत चेहरा
feel_shy|shy face|the shy face|utangaç yüz|utangaç yüzü|schüchternes Gesicht|das schüchterne Gesicht|visage timide|le visage timide|cara tímida|la cara tímida|faccina timida|la faccina timida|cara tímida|a cara tímida|застенчивое личико|застенчивое личико|وجه خجول|الوجه الخجول|शर्मीला चेहरा|शर्मीला चेहरा
puffy|Puffy|Puffy|Pofuduk|Pofuduk'u|Puffi|Puffi|Pouf|Pouf|Pufi|a Pufi|Puffi|Puffi|Fofinho|o Fofinho|Пушок|Пушка|نفنوف|نفنوف|पफ़ी|पफ़ी
shape_circle|circle|the circle|daire|daireyi|Kreis|den Kreis|rond|le rond|círculo|el círculo|cerchio|il cerchio|círculo|o círculo|круг|круг|دائرة|الدائرة|गोला|गोले
shape_square|square|the square|kare|kareyi|Quadrat|das Quadrat|carré|le carré|cuadrado|el cuadrado|quadrato|il quadrato|quadrado|o quadrado|квадрат|квадрат|مربع|المربع|वर्ग|वर्ग
shape_triangle|triangle|the triangle|üçgen|üçgeni|Dreieck|das Dreieck|triangle|le triangle|triángulo|el triángulo|triangolo|il triangolo|triângulo|o triângulo|треугольник|треугольник|مثلث|المثلث|त्रिभुज|त्रिभुज
shape_rectangle|rectangle|the rectangle|dikdörtgen|dikdörtgeni|Rechteck|das Rechteck|rectangle|le rectangle|rectángulo|el rectángulo|rettangolo|il rettangolo|retângulo|o retângulo|прямоугольник|прямоугольник|مستطيل|المستطيل|आयत|आयत
shape_star|star|the star|yıldız|yıldızı|Stern|den Stern|étoile|l'étoile|estrella|la estrella|stella|la stella|estrela|a estrela|звезда|звезду|نجمة|النجمة|तारा|तारे
shape_heart|heart|the heart|kalp|kalbi|Herz|das Herz|cœur|le cœur|corazón|el corazón|cuore|il cuore|coração|o coração|сердце|сердце|قلب|القلب|दिल|दिल
";
    }
}
