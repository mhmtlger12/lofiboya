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
            ["seiripan-ai-generated-9623276"] = "girl", ["seiripan-cute-9623238"] = "beach", ["seiripan-cute-9623248"] = "beach",
        };

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

        /// <summary>Galeri ve başlıklarda görünen ad.</summary>
        public static string LevelTitle(string id)
        {
            var k = Key(id);
            return Cap(k != null ? Word(k).name : Loc.T("picture"));
        }

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

        static (string name, string form) Word(string key)
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
";
    }
}
