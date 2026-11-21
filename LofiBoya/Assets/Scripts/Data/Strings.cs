using System.Collections.Generic;

namespace LofiBoya
{
    /// <summary>
    /// Metin tablosu. Her satır: anahtar | en | tr | de | fr | es | it | pt | ru | ar | hi
    /// "\n" satır sonu demektir. Yeni dil eklemek için Loc.Codes'a ve her satıra bir sütun ekleyin.
    /// </summary>
    public static class Strings
    {
        static Dictionary<string, string[]> _map;

        public static string Get(string key, int lang)
        {
            if (_map == null) Parse();
            if (!_map.TryGetValue(key, out var row)) return null;
            return lang < row.Length && !string.IsNullOrEmpty(row[lang]) ? row[lang] : null;
        }

        static void Parse()
        {
            _map = new Dictionary<string, string[]>();
            foreach (var raw in Table.Split('\n'))
            {
                var line = raw.Trim('\r');
                if (line.Length == 0 || line.StartsWith("#")) continue;
                var parts = line.Split('|');
                var vals = new string[parts.Length - 1];
                for (int i = 1; i < parts.Length; i++) vals[i - 1] = parts[i].Trim().Replace("\\n", "\n");
                _map[parts[0].Trim()] = vals;
            }
        }

        const string Table = @"
# ---------------- Karşılama ----------------
app_sub|relax  •  color  •  breathe|rahatla  •  boya  •  nefes al|entspannen  •  malen  •  atmen|détends-toi  •  colorie  •  respire|relájate  •  colorea  •  respira|rilassati  •  colora  •  respira|relaxa  •  pinta  •  respira|отдыхай  •  раскрашивай  •  дыши|استرخِ  •  لوّن  •  تنفّس|आराम  •  रंग  •  साँस
daily|~ today's picture ~|~ bugünün resmi ~|~ Bild des Tages ~|~ dessin du jour ~|~ dibujo del día ~|~ disegno del giorno ~|~ desenho do dia ~|~ картинка дня ~|~ صورة اليوم ~|~ आज का चित्र ~
start|Start Coloring|Boyamaya Başla|Jetzt malen!|On colorie !|¡A colorear!|Coloriamo!|Vamos pintar!|Раскрашивать!|هيا نلوّن!|रंग भरें!
learn|Learn|Öğren|Lernen|Apprendre|Aprender|Impara|Aprender|Учиться|تعلّم|सीखो
stickers_btn|Stickers|Çıkartmalar|Sticker|Stickers|Pegatinas|Adesivi|Cromos|Наклейки|ملصقات|स्टिकर
music_toast|music: {0}|müzik: {0}|Musik: {0}|musique : {0}|música: {0}|musica: {0}|música: {0}|музыка: {0}|الموسيقى: {0}|संगीत: {0}
bubble_streak|{0} days\nof coloring!|{0} gündür\nboyuyorsun!|{0} Tage\nam Malen!|{0} jours\nde coloriage !|¡{0} días\ncoloreando!|{0} giorni\na colorare!|{0} dias\na pintar!|Дней подряд:\n{0}!|أيام متتالية:
{0}!|लगातार
{0} दिन!
bubble_daily|today's picture\nis waiting!|bugünün resmi\nseni bekliyor!|Das Bild des\nTages wartet!|le dessin du jour\nt'attend !|¡el dibujo del día\nte espera!|il disegno del\ngiorno ti aspetta!|o desenho do dia\nespera por ti!|картинка дня\nждёт тебя!|صورة اليوم\nبانتظارك!|आज का चित्र\nतैयार है!
hello|Hi! I'm Puffy. Let's color together!|Merhaba! Ben Pofuduk. Hadi birlikte boyayalım!|Hallo! Ich bin Puffi. Komm, wir malen zusammen!|Coucou ! Je suis Pouf. On colorie ensemble ?|¡Hola! Soy Pufi. ¡Vamos a colorear juntos!|Ciao! Sono Puffi. Coloriamo insieme!|Olá! Eu sou o Fofinho. Vamos pintar juntos!|Привет! Я Пушок. Давай раскрашивать вместе!|مرحبًا! أنا نفنوف. هيا نلوّن معًا!|नमस्ते! मैं पफ़ी हूँ। चलो साथ में रंग भरें!
hello_streak|Hi! {0} days of coloring in a row. You're amazing!|Merhaba! {0} gündür boyuyorsun, süpersin!|Hallo! Schon {0} Tage hintereinander. Super!|Coucou ! Déjà {0} jours de suite. Bravo !|¡Hola! ¡Ya llevas {0} días seguidos! ¡Genial!|Ciao! Già {0} giorni di fila. Fantastico!|Olá! Já são {0} dias seguidos. Fantástico!|Привет! Дней подряд: {0}. Ты молодец!|مرحبًا! أيام متتالية: {0}. هذا رائع!|नमस्ते! लगातार {0} दिन। बहुत बढ़िया!
mascot_1|Hee hee! That tickles!|Hihi! Gıdıklanıyorum!|Hihi! Das kitzelt!|Hihi ! Ça chatouille !|¡Jiji! ¡Me haces cosquillas!|Ih ih! Mi fai il solletico!|Hihi! Fazes-me cócegas!|Хи-хи! Щекотно!|هههه! هذا يدغدغني!|हीहीही! गुदगुदी होती है!
mascot_2|Hello!|Merhaba!|Hallo!|Coucou !|¡Hola!|Ciao!|Olá!|Привет!|مرحبًا!|नमस्ते!
mascot_3|Let's color!|Hadi boyayalım!|Komm, wir malen!|On colorie ?|¡A colorear!|Coloriamo!|Vamos pintar!|Давай раскрашивать!|هيا نلوّن!|चलो रंग भरें!
mascot_4|You're awesome!|Sen harikasın!|Du bist toll!|Tu es génial !|¡Eres genial!|Sei fantastico!|És fantástico!|Ты супер!|أنت رائع!|तुम कमाल हो!
mascot_5|Which color shall we pick today?|Bugün hangi rengi seçelim?|Welche Farbe nehmen wir heute?|Quelle couleur on choisit aujourd'hui ?|¿Qué color elegimos hoy?|Che colore scegliamo oggi?|Que cor escolhemos hoje?|Какой цвет выберем сегодня?|أي لون نختار اليوم؟|आज कौन-सा रंग चुनें?
# ---------------- Galeri ----------------
gallery_title|My Coloring Book|Boyama Defterim|Mein Malbuch|Mon coloriage|Mi cuaderno|Il mio album|O meu caderno|Моя раскраска|دفتر التلوين|मेरी रंग किताब
gallery_sub|what would you like to color today?|bugün ne boyamak istersin?|Was möchtest du heute malen?|Que veux-tu colorier aujourd'hui ?|¿Qué quieres colorear hoy?|Cosa vuoi colorare oggi?|O que queres pintar hoje?|Что раскрасим сегодня?|ماذا سنلوّن اليوم؟|आज क्या रंगना है?
empty_tab|New pictures are on the way!\n\nWhy not color the\nEasy ones for now?|Yeni resimler yolda!\n\nŞimdilik Kolay'daki resimleri\nboyamaya ne dersin?|Neue Bilder sind unterwegs!\n\nMal doch erst die\nleichten Bilder aus!|De nouveaux dessins arrivent !\n\nEn attendant, colorie\nles dessins faciles !|¡Pronto habrá dibujos nuevos!\n\nMientras tanto,\n¡colorea los fáciles!|Nuovi disegni in arrivo!\n\nIntanto colora\nquelli facili!|Novos desenhos a caminho!\n\nEntretanto, pinta\nos fáceis!|Скоро будут новые картинки!\n\nА пока раскрась\nлёгкие!|رسومات جديدة قادمة!\n\nلوّن الآن\nالرسومات السهلة!|नए चित्र जल्दी आएँगे!\n\nअभी आसान वाले\nचित्र रंगो!
blank_page|Blank Page|Boş Sayfa|Leere Seite|Page blanche|Hoja en blanco|Foglio bianco|Folha em branco|Чистый лист|صفحة فارغة|खाली पन्ना
draw_badge|draw!|çiz!|malen!|dessine !|¡dibuja!|disegna!|desenha!|рисуй!|ارسم!|बनाओ!
badge_new|new|yeni|neu|nouveau|nuevo|nuovo|novo|новое|جديد|नया
badge_done|done!|bitti!|fertig!|fini !|¡listo!|fatto!|feito!|готово!|تم!|पूरा!
easy|Easy|Kolay|Leicht|Facile|Fácil|Facile|Fácil|Лёгкие|سهل|आसान
medium|Medium|Orta|Mittel|Moyen|Medio|Medio|Médio|Средние|متوسط|मध्यम
hard|Hard|Zor|Schwer|Difficile|Difícil|Difficile|Difícil|Сложные|صعب|कठिन
picture|Picture|Resim|Bild|Dessin|Dibujo|Disegno|Desenho|Картинка|صورة|चित्र
# ---------------- Boyama ----------------
percent|{0}%|%{0}|{0} %|{0} %|{0} %|{0}%|{0}%|{0}%|{0}%|{0}%
finish|Finish  ›|Bitir  ›|Fertig  ›|Terminer  ›|Terminar  ›|Fine  ›|Terminar  ›|Готово  ›|إنهاء|पूरा  ›
intro|{0}! Let's color it!|Bu bir {1}! Hadi boyayalım.|{0}! Lass uns malen!|{0} ! On colorie !|¡{0}! ¡A colorear!|{0}! Coloriamo!|{0}! Vamos pintar!|{0}! Давай раскрашивать!|{0}! هيا نلوّن!|{0}! चलो रंग भरें!
intro_number|{0}! Let's count and color!|Bu {1}! Hadi sayalım ve boyayalım.|{0}! Lass uns zählen und malen!|{0} ! On compte et on colorie !|¡{0}! ¡Contemos y coloreemos!|{0}! Contiamo e coloriamo!|{0}! Vamos contar e pintar!|{0}! Давай посчитаем и раскрасим!|{0}! هيا نعدّ ونلوّن!|{0}! चलो गिनें और रंग भरें!
intro_scene|{0}! Let's color it!|{0}! Hadi boyayalım.|{0}! Lass uns malen!|{0} ! On colorie !|¡{0}! ¡A colorear!|{0}! Coloriamo!|{0}! Vamos pintar!|{0}! Давай раскрашивать!|{0}! هيا نلوّن!|{0}! चलो रंग भरें!
all_painted|everything is colored|her yer boyanmış|alles ist ausgemalt|tout est colorié|todo está coloreado|è tutto colorato|está tudo pintado|всё раскрашено|كل شيء ملوّن|सब रंग गया
tip_pencil|draw with your finger · move with two fingers|parmağınla çiz · iki parmakla kaydır|mit dem Finger malen · mit zwei Fingern schieben|dessine avec ton doigt · deux doigts pour bouger|dibuja con el dedo · mueve con dos dedos|disegna con il dito · sposta con due dita|desenha com o dedo · move com dois dedos|рисуй пальцем · двигай двумя пальцами|ارسم بإصبعك · حرّك بإصبعين|उँगली से बनाओ · दो उँगलियों से हिलाओ
tip_stamp|tap to stamp! · pick a shape on the right|dokun, damga bas! · sağdan şekil seç|tippen zum Stempeln! · Form rechts wählen|touche pour tamponner ! · forme à droite|¡toca para estampar! · forma a la derecha|tocca per timbrare! · forma a destra|toca para carimbar! · forma à direita|нажми — будет штамп! · форма справа|المس لتطبع! · الشكل من اليمين|छूकर ठप्पा लगाओ! · दाएँ से आकार चुनो
tip_eraser|erase · tap to clear an area|sil · dokununca bölgeyi boşaltır|radieren · Tippen leert eine Fläche|gomme · touche pour vider une zone|borra · toca para vaciar una zona|cancella · tocca per svuotare un'area|apaga · toca para limpar uma área|стирай · нажми, чтобы очистить|امسح · المس لتفريغ منطقة|मिटाओ · छूकर हिस्सा साफ़ करो
numbered|Color by Number|Sayılı Boyama|Malen nach Zahlen|Coloriage magique|Colorear por números|Colora per numeri|Pintar por números|По номерам|التلوين بالأرقام|अंकों से रंग
no_numbers_blank|no numbers here, draw freely!|boş sayfada sayı yok, serbestçe çiz!|hier gibt es keine Zahlen, mal frei!|pas de numéros ici, dessine librement !|aquí no hay números, ¡dibuja libre!|qui non ci sono numeri, disegna liberamente!|aqui não há números, desenha à vontade!|здесь нет номеров, рисуй свободно!|لا أرقام هنا، ارسم بحرية!|यहाँ अंक नहीं हैं, खुलकर बनाओ!
numbers_on|Color by number! Pick the color with the same number.|Sayılı boyama! Numarayla aynı rengi seç.|Malen nach Zahlen! Nimm die Farbe mit der gleichen Zahl.|Coloriage magique ! Choisis la couleur du même numéro.|¡Colorear por números! Elige el color con el mismo número.|Colora per numeri! Scegli il colore con lo stesso numero.|Pintar por números! Escolhe a cor com o mesmo número.|Раскраска по номерам! Выбери цвет с таким же номером.|التلوين بالأرقام! اختر اللون الذي يحمل نفس الرقم.|अंकों से रंग! वही अंक वाला रंग चुनो।
number_hint|This is number {0}. Pick {1}!|Burası {0}. {1} kalemi seç!|Das ist die {0}. Nimm {1}!|C'est le {0}. Prends {1} !|Es el {0}. ¡Elige {1}!|È il {0}. Scegli {1}!|É o {0}. Escolhe {1}!|Это номер {0}. Выбери {1}!|هذا الرقم {0}. اختر {1}!|यह {0} नंबर है। {1} रंग चुनो!
already_clean|it's already clean|zaten tertemiz|schon ganz sauber|c'est déjà tout propre|ya está limpio|è già pulito|já está limpo|уже чисто|إنها نظيفة بالفعل|पहले से साफ़ है
clear_title|Clear everything?|Temizlensin mi?|Alles löschen?|Tout effacer ?|¿Borrar todo?|Cancellare tutto?|Apagar tudo?|Стереть всё?|مسح كل شيء؟|सब मिटा दें?
clear_msg|All the colors in this picture will be erased.|Bu resimdeki bütün boyalar silinecek.|Alle Farben in diesem Bild werden gelöscht.|Toutes les couleurs de ce dessin seront effacées.|Se borrarán todos los colores de este dibujo.|Tutti i colori di questo disegno verranno cancellati.|Todas as cores deste desenho serão apagadas.|Все цвета на этой картинке сотрутся.|سيتم مسح كل الألوان في هذه الصورة.|इस चित्र के सारे रंग मिट जाएँगे।
clear_yes|clear|temizle|löschen|effacer|borrar|cancella|apagar|стереть|امسح|मिटाओ
clear_no|cancel|vazgeç|abbrechen|annuler|cancelar|annulla|cancelar|отмена|إلغاء|रहने दो
streak_toast|{0} days of coloring in a row!|{0} gündür boyuyorsun!|{0} Tage hintereinander gemalt!|{0} jours de coloriage de suite !|¡{0} días seguidos coloreando!|{0} giorni di fila a colorare!|{0} dias seguidos a pintar!|Дней подряд: {0}!|أيام متتالية من التلوين: {0}!|लगातार {0} दिन से रंग भरना जारी!
streak_say|{0} days in a row! You're super!|{0} gündür boyuyorsun! Süpersin!|{0} Tage hintereinander! Super!|{0} jours de suite ! Tu es super !|¡{0} días seguidos! ¡Eres genial!|{0} giorni di fila! Sei super!|{0} dias seguidos! És o máximo!|Дней подряд: {0}! Ты супер!|أيام متتالية: {0}! هذا رائع!|लगातार {0} दिन! तुम कमाल हो!
magic_normal_say|Normal paint|Normal boya|Normale Farbe|Peinture normale|Pintura normal|Colore normale|Tinta normal|Обычная краска|لون عادي|सादा रंग
magic_say|{0} paint!|{0} boya!|{0}-Farbe!|Peinture {0} !|¡Pintura {0}!|Colore {0}!|Tinta {0}!|Краска «{0}»!|لون {0}!|{0} रंग!
magic_1|Glitter|Simli|Glitzer|pailletée|brillante|glitter|brilhante|Блёстки|لامع|चमकीला
magic_2|Rainbow|Gökkuşağı|Regenbogen|arc-en-ciel|arcoíris|arcobaleno|arco-íris|Радуга|قوس قزح|इंद्रधनुष
magic_3|Polka dot|Puantiye|Punkte|à pois|de lunares|a pois|às bolinhas|Горошек|منقّط|बिंदी वाला
magic_4|Starry|Yıldızlı|Sternen|étoilée|de estrellas|stellato|estrelado|Звёздочки|نجوم|तारों वाला
stamp_0|Heart|Kalp|Herz|Cœur|Corazón|Cuore|Coração|Сердечко|قلب|दिल
stamp_1|Star|Yıldız|Stern|Étoile|Estrella|Stella|Estrela|Звёздочка|نجمة|तारा
stamp_2|Flower|Çiçek|Blume|Fleur|Flor|Fiore|Flor|Цветок|زهرة|फूल
stamp_3|Balloon|Balon|Ballon|Ballon|Globo|Palloncino|Balão|Шарик|بالون|गुब्बारा
stamp_4|Cloud|Bulut|Wolke|Nuage|Nube|Nuvola|Nuvem|Облако|غيمة|बादल
pal_0|Pastel|Pastel|Pastell|Pastel|Pastel|Pastello|Pastel|Пастель|باستيل|पेस्टल
pal_1|Candy|Şeker|Bonbon|Bonbon|Caramelo|Caramella|Doce|Конфеты|حلوى|कैंडी
pal_2|Sunset|Gün Batımı|Abendrot|Coucher de soleil|Atardecer|Tramonto|Pôr do sol|Закат|غروب|सूर्यास्त
pal_3|Forest|Orman|Wald|Forêt|Bosque|Bosco|Floresta|Лес|غابة|जंगल
pal_4|Ocean|Okyanus|Ozean|Océan|Océano|Oceano|Oceano|Океан|محيط|सागर
pal_5|Classic|Klasik|Klassisch|Classique|Clásico|Classico|Clássico|Классика|كلاسيك|क्लासिक
# ---------------- Bitiş ----------------
great|Wonderful!|Harika!|Toll!|Génial !|¡Genial!|Fantastico!|Fantástico!|Чудесно!|رائع!|वाह!
done_sub|you finished your picture, it's lovely|resmini tamamladın, çok güzel oldu|dein Bild ist fertig, wunderschön|ton dessin est fini, il est superbe|terminaste tu dibujo, ¡qué bonito!|hai finito il disegno, è bellissimo|acabaste o desenho, ficou lindo|картинка готова, очень красиво|أنهيت رسمتك، إنها جميلة|तुम्हारा चित्र पूरा हुआ, बहुत सुंदर
gallery|Gallery|Galeri|Galerie|Galerie|Galería|Galleria|Galeria|Галерея|المعرض|गैलरी
gallery_next|Gallery  ›|Galeri  ›|Galerie  ›|Galerie  ›|Galería  ›|Galleria  ›|Galeria  ›|Галерея  ›|المعرض|गैलरी  ›
save|Save|Kaydet|Speichern|Garder|Guardar|Salva|Guardar|Сохранить|حفظ|सेव
saved_btn|Saved|Kaydedildi|Gespeichert|Gardé|Guardado|Salvato|Guardado|Готово|تم الحفظ|सेव हुआ
next|Next  ›|Sonraki  ›|Weiter  ›|Suivant  ›|Siguiente  ›|Avanti  ›|Seguinte  ›|Дальше  ›|التالي|अगला  ›
done_say|Great job! You finished your picture!|Harika! Resmini bitirdin!|Toll gemacht! Dein Bild ist fertig!|Bravo ! Tu as fini ton dessin !|¡Muy bien! ¡Terminaste tu dibujo!|Bravo! Hai finito il disegno!|Muito bem! Acabaste o desenho!|Молодец! Картинка готова!|أحسنت! أنهيت رسمتك!|शाबाश! तुमने चित्र पूरा किया!
done_named|Wonderful! You colored {0}!|Harika! {0} boyadın!|Toll! Du hast {0} ausgemalt!|Bravo ! Tu as colorié {0} !|¡Genial! ¡Coloreaste {0}!|Fantastico! Hai colorato {0}!|Fantástico! Pintaste {0}!|Отлично! Получилось раскрасить {0}!|رائع! لقد لوّنت {0}!|वाह! तुमने {0} रंग दिया!
blank_done_say|You made a beautiful picture!|Çok güzel bir resim yaptın!|Du hast ein wunderschönes Bild gemalt!|Tu as fait un très joli dessin !|¡Hiciste un dibujo precioso!|Hai fatto un disegno bellissimo!|Fizeste um desenho lindo!|Какая красивая картинка получилась!|رسمت صورة جميلة جدًا!|तुमने बहुत सुंदर चित्र बनाया!
saved_say|Your picture is saved! You can show it to your family.|Resmin kaydedildi! Annene babana gösterebilirsin.|Dein Bild ist gespeichert! Zeig es deiner Familie.|Ton dessin est gardé ! Montre-le à ta famille.|¡Tu dibujo está guardado! Enséñaselo a tu familia.|Il tuo disegno è salvato! Mostralo alla tua famiglia.|O teu desenho foi guardado! Mostra-o à tua família.|Картинка сохранена! Покажи её родным.|تم حفظ رسمتك! أرِها لعائلتك.|चित्र सेव हो गया! परिवार को दिखाओ।
saved_gallery|saved to your photos!|galeriye kaydedildi!|in deinen Fotos gespeichert!|enregistré dans tes photos !|¡guardado en tus fotos!|salvato nelle tue foto!|guardado nas tuas fotos!|сохранено в фото!|تم الحفظ في الصور!|फ़ोटो में सेव हुआ!
saved_local|your picture was saved!|resmin kaydedildi!|dein Bild wurde gespeichert!|ton dessin est enregistré !|¡tu dibujo se guardó!|il disegno è stato salvato!|o desenho foi guardado!|картинка сохранена!|تم حفظ رسمتك!|चित्र सेव हुआ!
save_failed|couldn't save|kaydedilemedi|Speichern fehlgeschlagen|impossible d'enregistrer|no se pudo guardar|impossibile salvare|não foi possível guardar|не удалось сохранить|تعذّر الحفظ|सेव नहीं हुआ
# ---------------- Öğren ----------------
learn_sub|which game shall we play?|hangi oyunu oynayalım?|Welches Spiel spielen wir?|À quel jeu on joue ?|¿A qué jugamos?|A che gioco giochiamo?|A que jogamos?|Во что поиграем?|أي لعبة نلعب؟|कौन-सा खेल खेलें?
colors|Colors|Renkler|Farben|Couleurs|Colores|Colori|Cores|Цвета|الألوان|रंग
colors_sub|find the color|rengi bul|finde die Farbe|trouve la couleur|encuentra el color|trova il colore|encontra a cor|найди цвет|ابحث عن اللون|रंग ढूँढो
pictures|Pictures|Resimler|Bilder|Dessins|Dibujos|Disegni|Desenhos|Картинки|صور|चित्र
pictures_sub|which one?|hangisi?|welches?|lequel ?|¿cuál?|quale?|qual?|какая?|أيّها؟|कौन-सा?
soon|coming soon!|yakında!|bald!|bientôt !|¡muy pronto!|presto!|em breve!|скоро!|قريبًا!|जल्द आ रहा है!
find|Find {0}!|{0} bul!|Finde {0}!|Trouve {0} !|¡Encuentra {0}!|Trova {0}!|Encontra {0}!|Найди {0}!|ابحث عن {0}!|{0} ढूँढो!
cheer_0|Well done!|Aferin!|Gut gemacht!|Bien joué !|¡Muy bien!|Bravo!|Muito bem!|Молодец!|أحسنت!|शाबाश!
cheer_1|Super!|Süpersin!|Super!|Super !|¡Súper!|Super!|Boa!|Супер!|ممتاز!|बहुत बढ़िया!
cheer_2|Wonderful!|Harika!|Toll!|Génial !|¡Genial!|Fantastico!|Fantástico!|Чудесно!|رائع!|वाह!
cheer_3|Bravo!|Bravo!|Bravo!|Bravo !|¡Bravo!|Evviva!|Bravo!|Браво!|برافو!|बहुत खूब!
right_say|{0} {1}!|{0} Bu {1}!|{0} {1}!|{0} {1} !|{0} ¡{1}!|{0} {1}!|{0} {1}!|{0} {1}!|{0} {1}!|{0} {1}!
wrong_say|{0}? Try again!|Bu {0}. Tekrar dene!|{0}? Versuch es nochmal!|{0} ? Essaie encore !|¿{0}? ¡Inténtalo otra vez!|{0}? Riprova!|{0}? Tenta outra vez!|{0}? Попробуй ещё!|{0}؟ حاول مرة أخرى!|{0}? फिर से कोशिश करो!
# ---------------- Çıkartmalar ----------------
stickers_title|My Stickers|Çıkartmalarım|Meine Sticker|Mes stickers|Mis pegatinas|I miei adesivi|Os meus cromos|Мои наклейки|ملصقاتي|मेरे स्टिकर
stickers_none|finish a picture to get your first sticker!|bir resmi bitir, ilk çıkartman gelsin!|Mal ein Bild fertig für deinen ersten Sticker!|finis un dessin pour ton premier sticker !|¡termina un dibujo y gana tu primera pegatina!|finisci un disegno per il primo adesivo!|acaba um desenho e ganha o primeiro cromo!|раскрась картинку — получишь наклейку!|أنهِ رسمة لتحصل على أول ملصق!|चित्र पूरा करो, पहला स्टिकर पाओ!
stickers_count|you collected {0} / {1} stickers|{0} / {1} çıkartma topladın|{0} / {1} Sticker gesammelt|{0} / {1} stickers gagnés|has reunido {0} / {1} pegatinas|hai raccolto {0} / {1} adesivi|juntaste {0} / {1} cromos|собрано наклеек: {0} / {1}|جمعت {0} / {1} ملصقًا|{0} / {1} स्टिकर मिले
# ---------------- Ebeveyn / ayarlar ----------------
parents|For grown-ups|Ebeveyn bölümü|Für Eltern|Pour les parents|Para los padres|Per i genitori|Para os pais|Для родителей|للوالدين|माता-पिता के लिए
gate_q|What is {0} + {1}?|{0} + {1} kaç eder?|Was ist {0} + {1}?|Combien font {0} + {1} ?|¿Cuánto es {0} + {1}?|Quanto fa {0} + {1}?|Quanto é {0} + {1}?|Сколько будет {0} + {1}?|كم يساوي {0} + {1}؟|{0} + {1} कितना है?
settings|Settings|Ayarlar|Einstellungen|Réglages|Ajustes|Impostazioni|Definições|Настройки|الإعدادات|सेटिंग्स
music|Music|Müzik|Musik|Musique|Música|Musica|Música|Музыка|الموسيقى|संगीत
sound|Sound effects|Efekt sesleri|Soundeffekte|Effets sonores|Efectos de sonido|Effetti sonori|Efeitos sonoros|Звуки|المؤثرات الصوتية|साउंड इफ़ेक्ट
haptics|Vibration|Titreşim|Vibration|Vibration|Vibración|Vibrazione|Vibração|Вибрация|الاهتزاز|कंपन
track|Track: {0}  ›|Parça: {0}  ›|Lied: {0}  ›|Morceau : {0}  ›|Canción: {0}  ›|Brano: {0}  ›|Faixa: {0}  ›|Трек: {0}  ›|المقطوعة: {0}|गाना: {0}  ›
language|Language: {0}  ›|Dil: {0}  ›|Sprache: {0}  ›|Langue : {0}  ›|Idioma: {0}  ›|Lingua: {0}  ›|Idioma: {0}  ›|Язык: {0}  ›|اللغة: {0}|भाषा: {0}  ›
lang_auto|Automatic|Otomatik|Automatisch|Automatique|Automático|Automatica|Automático|Авто|تلقائي|अपने आप
info|No ads · no personal data collected\nProgress is stored only on this device.|Reklam yok · kişisel veri toplanmaz\nİlerleme yalnızca bu cihazda saklanır.|Keine Werbung · keine persönlichen Daten\nFortschritt bleibt nur auf diesem Gerät.|Sans pub · aucune donnée personnelle\nLa progression reste sur cet appareil.|Sin anuncios · sin datos personales\nEl progreso se guarda solo en este dispositivo.|Niente pubblicità · nessun dato personale\nI progressi restano su questo dispositivo.|Sem anúncios · sem dados pessoais\nO progresso fica só neste dispositivo.|Без рекламы · без личных данных\nПрогресс хранится только на устройстве.|بلا إعلانات · لا نجمع بيانات شخصية\nيُحفظ التقدّم على هذا الجهاز فقط.|कोई विज्ञापन नहीं · कोई निजी डेटा नहीं\nप्रगति सिर्फ़ इसी डिवाइस में रहती है।
ok|OK|Tamam|OK|OK|Vale|OK|OK|Готово|حسنًا|ठीक है
# ---------------- Resimlerim (albüm) ----------------
album|My Art|Resimlerim|Meine Bilder|Mes dessins|Mis dibujos|I miei disegni|Os meus desenhos|Мои рисунки|رسوماتي|मेरे चित्र
album_sub|your drawings and colored pictures|çizdiğin ve boyadığın resimler|deine gemalten und ausgemalten Bilder|tes dessins et tes coloriages|tus dibujos y tus coloreados|i tuoi disegni e i tuoi colorati|os teus desenhos e pinturas|твои рисунки и раскраски|رسوماتك وتلويناتك|तुम्हारे बनाए और रंगे चित्र
new_drawing|New drawing|Yeni çizim|Neues Bild|Nouveau dessin|Dibujo nuevo|Nuovo disegno|Novo desenho|Новый рисунок|رسمة جديدة|नया चित्र
album_empty|Draw or color a picture and it will appear here!|Bir resim çiz ya da boya, burada görünecek!|Mal ein Bild, dann erscheint es hier!|Dessine ou colorie, et ton dessin apparaîtra ici !|¡Dibuja o colorea y aparecerá aquí!|Disegna o colora e apparirà qui!|Desenha ou pinta e vai aparecer aqui!|Нарисуй или раскрась картинку, и она появится здесь!|ارسم أو لوّن صورة وستظهر هنا!|चित्र बनाओ या रंग भरो, वह यहाँ दिखेगा!
new_drawing_say|A new page! Draw whatever you like.|Yeni bir sayfa! İstediğini çiz.|Ein neues Blatt! Mal, was du magst.|Une nouvelle page ! Dessine ce que tu veux.|¡Una hoja nueva! Dibuja lo que quieras.|Un foglio nuovo! Disegna quello che vuoi.|Uma folha nova! Desenha o que quiseres.|Новый лист! Рисуй что хочешь.|صفحة جديدة! ارسم ما تحب.|नया पन्ना! जो मन करे, बनाओ।
# ---------------- Öğren: şekiller ve sayma ----------------
shapes|Shapes|Şekiller|Formen|Formes|Formas|Forme|Formas|Фигуры|الأشكال|आकृतियाँ
shapes_sub|color the shape|şekli boya|male die Form an|colorie la forme|colorea la forma|colora la forma|pinta a forma|раскрась фигуру|لوّن الشكل|आकृति में रंग भरो
paint_shape|Color {0}!|{0} boya!|Male {0} an!|Colorie {0} !|¡Colorea {0}!|Colora {0}!|Pinta {0}!|Раскрась {0}!|لوّن {0}!|{0} में रंग भरो!
counting|Counting|Sayılar|Zählen|Compter|Contar|Contare|Contar|Счёт|العدّ|गिनती
counting_sub|how many are there?|kaç tane var?|wie viele sind es?|combien y en a-t-il ?|¿cuántos hay?|quanti sono?|quantos são?|сколько их?|كم عددها؟|कितने हैं?
count_q_0|How many hearts?|Kaç tane kalp var?|Wie viele Herzen sind es?|Combien de cœurs ?|¿Cuántos corazones hay?|Quanti cuori ci sono?|Quantos corações há?|Сколько сердечек?|كم قلبًا هنا؟|कितने दिल हैं?
count_q_1|How many stars?|Kaç tane yıldız var?|Wie viele Sterne sind es?|Combien d'étoiles ?|¿Cuántas estrellas hay?|Quante stelle ci sono?|Quantas estrelas há?|Сколько звёздочек?|كم نجمة هنا؟|कितने तारे हैं?
count_q_2|How many flowers?|Kaç tane çiçek var?|Wie viele Blumen sind es?|Combien de fleurs ?|¿Cuántas flores hay?|Quanti fiori ci sono?|Quantas flores há?|Сколько цветочков?|كم زهرة هنا؟|कितने फूल हैं?
count_q_3|How many balloons?|Kaç tane balon var?|Wie viele Luftballons sind es?|Combien de ballons ?|¿Cuántos globos hay?|Quanti palloncini ci sono?|Quantos balões há?|Сколько шариков?|كم بالونًا هنا؟|कितने गुब्बारे हैं?
count_together|Let's count together!|Hadi birlikte sayalım!|Lass uns zusammen zählen!|Comptons ensemble !|¡Contemos juntos!|Contiamo insieme!|Vamos contar juntos!|Давай посчитаем вместе!|هيا نعدّ معًا!|चलो साथ में गिनें!
count_right|{0} There are {1}!|{0} {1} tane!|{0} Es sind {1}!|{0} Il y en a {1} !|{0} ¡Hay {1}!|{0} Sono {1}!|{0} São {1}!|{0} Правильно, {1}!|{0} العدد {1}!|{0} ये {1} हैं!
# ---------------- Ebeveyn: mola ve kalem ----------------
pencil_inside|Pencil stays inside the lines (for little ones)|Kalem çizgi dışına taşmasın (küçükler için)|Stift bleibt in den Linien (für die Kleinen)|Le crayon ne dépasse pas (pour les petits)|El lápiz no se sale (para los pequeños)|La matita non esce dai bordi (per i piccoli)|O lápis não sai das linhas (para os pequenos)|Карандаш не выходит за контур (для малышей)|القلم لا يخرج عن الخطوط (للصغار)|पेंसिल रेखाओं से बाहर न जाए (छोटों के लिए)
break_btn|Break reminder: {0}  ›|Mola hatırlatma: {0}  ›|Pausen-Erinnerung: {0}  ›|Rappel de pause : {0}  ›|Aviso de descanso: {0}  ›|Promemoria pausa: {0}  ›|Lembrete de pausa: {0}  ›|Перерыв: {0}  ›|تذكير بالاستراحة: {0}  ›|आराम की याद: {0}  ›
break_off|off|kapalı|aus|désactivé|desactivado|disattivato|desligado|выкл.|متوقف|बंद
break_every|every {0} min|{0} dakikada bir|alle {0} Min.|toutes les {0} min|cada {0} min|ogni {0} min|a cada {0} min|каждые {0} мин|كل {0} د|हर {0} मिनट
break_title|Time for a break!|Mola zamanı!|Zeit für eine Pause!|C'est l'heure de la pause !|¡Hora de descansar!|È ora di una pausa!|Hora de uma pausa!|Пора отдохнуть!|حان وقت الاستراحة!|आराम का समय!
break_sub|Rest your eyes, stretch and drink some water. See you soon!|Gözlerini dinlendir, biraz hareket et, su iç. Sonra görüşürüz!|Ruh deine Augen aus, beweg dich und trink etwas Wasser. Bis bald!|Repose tes yeux, bouge un peu et bois de l'eau. À bientôt !|Descansa los ojos, muévete un poco y bebe agua. ¡Hasta pronto!|Riposa gli occhi, muoviti un po' e bevi dell'acqua. A presto!|Descansa os olhos, mexe-te um pouco e bebe água. Até já!|Дай глазкам отдохнуть, подвигайся и попей воды. До скорой встречи!|أرِح عينيك وتحرّك قليلًا واشرب الماء. نراك قريبًا!|आँखों को आराम दो, थोड़ा हिलो-डुलो और पानी पियो। फिर मिलेंगे!
break_wait|Play again in: {0}|Tekrar oynamaya kalan: {0}|Wieder spielen in: {0}|Rejouer dans : {0}|Volver a jugar en: {0}|Si rigioca tra: {0}|Voltar a jogar em: {0}|Снова играть через: {0}|اللعب مجددًا بعد: {0}|फिर खेलने में: {0}
break_continue|Grown-ups: continue|Ebeveyn: devam et|Eltern: weiter|Parents : continuer|Padres: continuar|Genitori: continua|Pais: continuar|Взрослым: продолжить|للوالدين: متابعة|बड़ों के लिए: जारी रखें
break_over|Break is over! Welcome back.|Mola bitti! Tekrar hoş geldin.|Die Pause ist vorbei! Willkommen zurück.|La pause est finie ! Re-bonjour.|¡Se acabó el descanso! ¡A seguir!|La pausa è finita! Si ricomincia!|A pausa acabou! Vamos continuar!|Перерыв окончен! С возвращением.|انتهت الاستراحة! أهلًا بعودتك.|आराम पूरा हुआ! फिर से स्वागत है।
feelings_game|Feelings|Duygular|Gefühle|Émotions|Emociones|Emozioni|Emoções|Эмоции|المشاعر|भावनाएँ
feelings_sub|how does Puffy feel?|Pofuduk nasıl hissediyor?|Wie fühlt sich Puffi?|Comment se sent Pouf ?|¿Cómo se siente Pufi?|Come si sente Puffi?|Como se sente o Fofinho?|Как чувствует себя Пушок?|بماذا يشعر نفنوف؟|पफ़ी कैसा महसूस कर रहा है?
puffy_name|Puffy|Pofuduk|Puffi|Pouf|Pufi|Puffi|Fofinho|Пушок|نفنوف|पफ़ी
puffy_more|{0} more pictures|{0} resim daha|noch {0} Bilder|encore {0} dessins|{0} dibujos más|ancora {0} disegni|mais {0} desenhos|ещё картинок: {0}|بقي {0} من الرسومات|{0} चित्र और
puffy_new|New Puffy sticker! Look in your sticker album.|Yeni Pofuduk çıkartması! Çıkartma albümüne bak.|Neuer Puffi-Sticker! Schau in dein Stickeralbum.|Nouveau sticker Pouf ! Regarde ton album.|¡Nueva pegatina de Pufi! Mira tu álbum.|Nuovo adesivo di Puffi! Guarda il tuo album.|Novo cromo do Fofinho! Vê a tua caderneta.|Новая наклейка с Пушком! Загляни в альбом.|ملصق نفنوف جديد! انظر في ألبومك.|पफ़ी का नया स्टिकर! अपना एल्बम देखो।
# ---------------- Boyama arkadaşı Pofuduk ----------------
buddy_25|Great start!|Harika bir başlangıç!|Toller Anfang!|Super début !|¡Buen comienzo!|Ottimo inizio!|Bom começo!|Отличное начало!|بداية رائعة!|बहुत अच्छी शुरुआत!
buddy_50|Halfway there!|Yarıladık bile!|Schon die Hälfte!|Déjà la moitié !|¡Ya vamos por la mitad!|Siamo già a metà!|Já vamos a meio!|Уже половина!|وصلنا إلى النصف!|आधा हो गया!
buddy_75|Almost done!|Az kaldı!|Fast fertig!|Presque fini !|¡Casi terminamos!|Quasi finito!|Quase pronto!|Почти готово!|اقتربنا من النهاية!|बस थोड़ा सा बाकी!
buddy_idle|Shall we color this part?|Burayı boyamaya ne dersin?|Wollen wir diesen Teil anmalen?|On colorie cette partie ?|¿Coloreamos esta parte?|Coloriamo questa parte?|Vamos pintar esta parte?|Раскрасим эту часть?|هل نلوّن هذا الجزء؟|क्या इस हिस्से में रंग भरें?
buddy_idle_blank|What shall we draw next?|Şimdi ne çizelim?|Was malen wir als Nächstes?|On dessine quoi maintenant ?|¿Qué dibujamos ahora?|Cosa disegniamo adesso?|O que vamos desenhar agora?|Что нарисуем дальше?|ماذا نرسم الآن؟|अब क्या बनाएँ?
# ---------------- Duygu–durum eşleştirme ----------------
feel_s0|Puffy's balloon flew away.|Pofuduk'un balonu uçup gitti.|Puffis Luftballon ist weggeflogen.|Le ballon de Pouf s'est envolé.|El globo de Pufi se fue volando.|Il palloncino di Puffi è volato via.|O balão do Fofinho voou para longe.|Воздушный шарик Пушка улетел.|طار بالون نفنوف بعيدًا.|पफ़ी का गुब्बारा उड़ गया।
feel_s1|Puffy got flowers as a present.|Pofuduk'a hediye olarak çiçek verdiler.|Puffi hat Blumen geschenkt bekommen.|Pouf a reçu des fleurs en cadeau.|Le regalaron flores a Pufi.|Hanno regalato dei fiori a Puffi.|Deram flores de presente ao Fofinho.|Пушку подарили цветы.|أهدوا نفنوف زهورًا.|पफ़ी को तोहफ़े में फूल मिले।
feel_s2|Someone scribbled on Puffy's drawing.|Biri Pofuduk'un resmini karaladı.|Jemand hat Puffis Bild bekritzelt.|Quelqu'un a gribouillé sur le dessin de Pouf.|Alguien garabateó el dibujo de Pufi.|Qualcuno ha scarabocchiato il disegno di Puffi.|Alguém rabiscou o desenho do Fofinho.|Кто-то исчеркал рисунок Пушка.|خربش أحدهم على رسمة نفنوف.|किसी ने पफ़ी की ड्रॉइंग पर लकीरें खींच दीं।
feel_s3|Puffy opened a box and a rabbit jumped out!|Pofuduk kutuyu açınca içinden bir tavşan fırladı!|Puffi öffnet eine Kiste und ein Hase springt heraus!|Pouf ouvre une boîte et un lapin en sort d'un bond !|¡Pufi abre una caja y salta un conejo!|Puffi apre una scatola e salta fuori un coniglio!|O Fofinho abre uma caixa e salta de lá um coelho!|Пушок открывает коробку, а оттуда выпрыгивает кролик!|يفتح نفنوف صندوقًا فيقفز منه أرنب!|पफ़ी ने डिब्बा खोला और उसमें से खरगोश कूद पड़ा!
feel_s4|Puffy is playing with friends.|Pofuduk arkadaşlarıyla oyun oynuyor.|Puffi spielt mit Freunden.|Pouf joue avec ses amis.|Pufi juega con sus amigos.|Puffi gioca con gli amici.|O Fofinho está a brincar com os amigos.|Пушок играет с друзьями.|يلعب نفنوف مع أصدقائه.|पफ़ी दोस्तों के साथ खेल रहा है।
feel_s5|Puffy's ice cream fell on the ground.|Pofuduk'un dondurması yere düştü.|Puffis Eis ist auf den Boden gefallen.|La glace de Pouf est tombée par terre.|El helado de Pufi se cayó al suelo.|Il gelato di Puffi è caduto per terra.|O gelado do Fofinho caiu ao chão.|Мороженое Пушка упало на землю.|سقطت مثلجات نفنوف على الأرض.|पफ़ी की आइसक्रीम ज़मीन पर गिर गई।
feel_s6|Someone took Puffy's toy without asking.|Biri Pofuduk'un oyuncağını sormadan aldı.|Jemand hat Puffis Spielzeug ohne zu fragen genommen.|Quelqu'un a pris le jouet de Pouf sans demander.|Alguien le quitó el juguete a Pufi sin pedirlo.|Qualcuno ha preso il giocattolo di Puffi senza chiedere.|Alguém tirou o brinquedo ao Fofinho sem pedir.|Кто-то взял игрушку Пушка без спроса.|أخذ أحدهم لعبة نفنوف دون أن يستأذن.|किसी ने बिना पूछे पफ़ी का खिलौना ले लिया।
feel_s7|Puffy opened the door and everyone shouted: Surprise!|Pofuduk kapıyı açınca herkes Sürpriz diye bağırdı!|Puffi öffnet die Tür und alle rufen: Überraschung!|Pouf ouvre la porte et tout le monde crie : Surprise !|Pufi abre la puerta y todos gritan: ¡Sorpresa!|Puffi apre la porta e tutti gridano: Sorpresa!|O Fofinho abre a porta e todos gritam: Surpresa!|Пушок открывает дверь, а все кричат: Сюрприз!|يفتح نفنوف الباب فيصرخ الجميع: مفاجأة!|पफ़ी ने दरवाज़ा खोला और सब चिल्लाए: सरप्राइज़!
feel_tip_happy|When we're happy, we smile!|Mutlu olunca gülümseriz!|Wenn wir fröhlich sind, lächeln wir!|Quand on est content, on sourit !|¡Cuando estamos felices, sonreímos!|Quando siamo felici, sorridiamo!|Quando estamos felizes, sorrimos!|Когда нам весело, мы улыбаемся!|عندما نفرح نبتسم!|खुश होने पर हम मुस्कुराते हैं!
feel_tip_sad|A hug helps when someone is sad.|Üzgün birine sarılmak iyi gelir.|Wenn jemand traurig ist, hilft eine Umarmung.|Un câlin fait du bien quand on est triste.|Un abrazo ayuda cuando alguien está triste.|Un abbraccio aiuta quando qualcuno è triste.|Um abraço ajuda quando alguém está triste.|Когда кому-то грустно, помогают объятия.|العناق يساعد عندما يكون أحدهم حزينًا.|कोई उदास हो तो गले लगाना अच्छा लगता है।
feel_tip_angry|When we're angry, let's take a deep breath.|Kızınca derin bir nefes alalım.|Wenn wir wütend sind, atmen wir tief durch.|Quand on est fâché, on respire profondément.|Cuando nos enfadamos, respiramos hondo.|Quando siamo arrabbiati, facciamo un bel respiro.|Quando estamos zangados, respiramos fundo.|Когда мы сердимся, делаем глубокий вдох.|عندما نغضب، نأخذ نفسًا عميقًا.|गुस्सा आए तो गहरी साँस लें।
feel_tip_surprised|When we're surprised, our eyes open wide!|Şaşırınca gözlerimiz kocaman açılır!|Wenn wir überrascht sind, werden unsere Augen ganz groß!|Quand on est surpris, nos yeux s'ouvrent grand !|¡Cuando nos sorprendemos, abrimos mucho los ojos!|Quando siamo sorpresi, spalanchiamo gli occhi!|Quando ficamos surpreendidos, abrimos muito os olhos!|Когда мы удивляемся, глаза становятся большими!|عندما نتفاجأ تتسع عيوننا!|हैरान होने पर हमारी आँखें बड़ी हो जाती हैं!
# ---------------- Sayılar: karşılaştırma ve toplama ----------------
cmp_q|Which side has more?|Hangi tarafta daha çok var?|Wo sind es mehr?|De quel côté y en a-t-il le plus ?|¿De qué lado hay más?|Da che parte ce ne sono di più?|De que lado há mais?|Где больше?|في أيّ جهة العدد أكثر؟|किस तरफ़ ज़्यादा हैं?
cmp_right|{0} {1} is more!|{0} {1} daha çok!|{0} {1} sind mehr!|{0} {1}, c'est plus !|{0} ¡{1} es más!|{0} {1} è di più!|{0} {1} é mais!|{0} {1} — больше!|{0} {1} أكثر!|{0} {1} ज़्यादा है!
cmp_show|Here {0}, there {1}. {2} is more!|Burada {0}, burada {1}. {2} daha çok!|Hier {0}, dort {1}. {2} sind mehr!|Ici {0}, là {1}. {2}, c'est plus !|Aquí {0}, allí {1}. ¡{2} es más!|Qui {0}, lì {1}. {2} è di più!|Aqui {0}, ali {1}. {2} é mais!|Здесь {0}, там {1}. {2} — больше!|هنا {0}، وهناك {1}. {2} أكثر!|यहाँ {0}, वहाँ {1}। {2} ज़्यादा है!
add_q|How many altogether?|Hepsi kaç tane?|Wie viele sind es zusammen?|Combien en tout ?|¿Cuántos hay en total?|Quanti sono in tutto?|Quantos são ao todo?|Сколько всего?|كم العدد كله؟|कुल कितने हैं?
add_say|{0}, and {1} more came. How many altogether?|{0} tane var, {1} tane daha geldi. Hepsi kaç tane?|{0}, und {1} kommen dazu. Wie viele sind es zusammen?|{0}, et {1} de plus arrivent. Combien en tout ?|{0}, y llegan {1} más. ¿Cuántos hay en total?|{0}, e ne arrivano altri {1}. Quanti sono in tutto?|{0}, e chegam mais {1}. Quantos são ao todo?|{0}, и пришло ещё {1}. Сколько всего?|{0}، وجاء {1} أيضًا. كم العدد كله؟|{0} हैं, {1} और आए। कुल कितने हैं?
# ---------------- Rakam yaz ----------------
trace|Write numbers|Rakam yaz|Zahlen schreiben|Écrire les chiffres|Escribir números|Scrivi i numeri|Escrever números|Пишем цифры|اكتب الأرقام|अंक लिखो
trace_sub|trace with your finger|parmağınla çiz|mit dem Finger nachfahren|trace avec ton doigt|traza con tu dedo|ricalca con il dito|traça com o dedo|обведи пальчиком|تتبّع بإصبعك|उंगली से बनाओ
trace_q|Trace {0}!|{0} parmağınla çiz!|Fahre {0} nach!|Trace {0} !|¡Traza {0}!|Ricalca {0}!|Traça {0}!|Обведи {0}!|تتبّع {0}!|{0} बनाओ!
trace_start|Start at the green dot.|Yeşil noktadan başla.|Fang beim grünen Punkt an.|Commence au point vert.|Empieza en el punto verde.|Inizia dal punto verde.|Começa no ponto verde.|Начни с зелёной точки.|ابدأ من النقطة الخضراء.|हरे बिंदु से शुरू करो।
trace_clear|Start again|Baştan|Nochmal|Recommencer|Otra vez|Di nuovo|De novo|Заново|من جديد|फिर से
# ---------------- Ebeveyn: yaş ve rapor ----------------
age_btn|Child's age: {0}  ›|Çocuğun yaşı: {0}  ›|Alter: {0}  ›|Âge : {0}  ›|Edad: {0}  ›|Età: {0}  ›|Idade: {0}  ›|Возраст: {0}  ›|العمر: {0}  ›|उम्र: {0}  ›
report_btn|Weekly report  ›|Haftalık rapor  ›|Wochenbericht  ›|Bilan de la semaine  ›|Informe semanal  ›|Resoconto settimanale  ›|Relatório semanal  ›|Отчёт за неделю  ›|التقرير الأسبوعي  ›|साप्ताहिक रिपोर्ट  ›
report_title|Last 7 days|Son 7 gün|Letzte 7 Tage|7 derniers jours|Últimos 7 días|Ultimi 7 giorni|Últimos 7 dias|Последние 7 дней|آخر 7 أيام|पिछले 7 दिन
rep_pictures|Pictures finished: {0}|Bitirilen resim: {0}|Fertige Bilder: {0}|Dessins terminés : {0}|Dibujos terminados: {0}|Disegni finiti: {0}|Desenhos terminados: {0}|Готовых картинок: {0}|رسومات مكتملة: {0}|पूरे किए चित्र: {0}
rep_time|Time in the app: {0} min|Uygulamada geçen süre: {0} dk|Zeit in der App: {0} Min.|Temps dans l'appli : {0} min|Tiempo en la app: {0} min|Tempo nell'app: {0} min|Tempo na app: {0} min|Время в приложении: {0} мин|الوقت في التطبيق: {0} دقيقة|ऐप में समय: {0} मिनट
rep_days|Days played: {0} / 7|Oynadığı gün: {0} / 7|Spieltage: {0} / 7|Jours de jeu : {0} / 7|Días de juego: {0} / 7|Giorni di gioco: {0} / 7|Dias de jogo: {0} / 7|Дней с игрой: {0} / 7|أيام اللعب: {0} / 7|खेलने के दिन: {0} / 7
rep_games_title|Learning games|Öğrenme oyunları|Lernspiele|Jeux d'apprentissage|Juegos para aprender|Giochi educativi|Jogos para aprender|Обучающие игры|ألعاب التعلّم|सीखने वाले खेल
rep_game|{0}: {1} right out of {2}|{0}: {2} denemede {1} doğru|{0}: {1} richtig von {2}|{0} : {1} bonnes réponses sur {2}|{0}: {1} aciertos de {2}|{0}: {1} giuste su {2}|{0}: {1} certas em {2}|{0}: верно {1} из {2}|{0}: {1} صحيحة من {2}|{0}: {2} में से {1} सही
rep_none|No learning games this week yet.|Bu hafta henüz öğrenme oyunu oynanmadı.|Diese Woche noch keine Lernspiele.|Pas encore de jeux cette semaine.|Aún no hay juegos esta semana.|Ancora nessun gioco questa settimana.|Ainda sem jogos esta semana.|На этой неделе игр пока не было.|لا توجد ألعاب تعلّم هذا الأسبوع بعد.|इस हफ़्ते अभी कोई खेल नहीं खेला गया।
rep_note|Mistakes are part of learning: the games never scold.|Hatalar öğrenmenin parçasıdır; oyunlar asla azarlamaz.|Fehler gehören zum Lernen: Die Spiele schimpfen nie.|Les erreurs font partie de l'apprentissage : les jeux ne grondent jamais.|Equivocarse es parte de aprender: los juegos nunca regañan.|Sbagliare fa parte dell'imparare: i giochi non sgridano mai.|Errar faz parte de aprender: os jogos nunca ralham.|Ошибки — часть обучения: игры никогда не ругают.|الأخطاء جزء من التعلّم، والألعاب لا توبّخ أبدًا.|गलतियाँ सीखने का हिस्सा हैं: खेल कभी डाँटते नहीं।
# ---------------- Müzik parçaları ----------------
track_0|Pink Clouds|Pembe Bulutlar|Rosa Wolken|Nuages roses|Nubes rosas|Nuvole rosa|Nuvens cor-de-rosa|Розовые облака|غيوم وردية|गुलाबी बादल
track_1|Rainy Café|Yağmurlu Kafe|Regencafé|Café sous la pluie|Café lluvioso|Caffè sotto la pioggia|Café à chuva|Дождливое кафе|مقهى ممطر|बारिश वाला कैफ़े
track_2|Night Walk|Gece Yürüyüşü|Nachtspaziergang|Balade de nuit|Paseo nocturno|Passeggiata notturna|Passeio noturno|Ночная прогулка|نزهة ليلية|रात की सैर
track_3|Morning Coffee|Sabah Kahvesi|Morgenkaffee|Café du matin|Café de la mañana|Caffè del mattino|Café da manhã|Утренний кофе|قهوة الصباح|सुबह की कॉफ़ी
track_4|Music Box|Müzik Kutusu|Spieluhr|Boîte à musique|Caja de música|Carillon|Caixa de música|Музыкальная шкатулка|صندوق الموسيقى|संगीत बक्सा
track_5|Bird Garden|Kuş Bahçesi|Vogelgarten|Jardin des oiseaux|Jardín de pájaros|Giardino degli uccelli|Jardim dos pássaros|Птичий сад|حديقة الطيور|चिड़ियों का बाग
track_6|Seaside|Deniz Kenarı|Am Meer|Bord de mer|Junto al mar|In riva al mare|À beira-mar|У моря|على الشاطئ|समुद्र किनारे
";
    }
}
