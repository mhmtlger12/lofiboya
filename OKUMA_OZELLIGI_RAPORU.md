# Lofi Boya — "Okuma" (Hikaye Kitaplığı) Özelliği: Teknik Rapor

> Bu belge, Lofi Boya'ya eklenen hikaye okuma özelliğini ve ana menü düzenlemesini anlatır.
> Amaç: başka bir geliştiricinin ya da yapay zekânın kodu **incelemesi** ve **neler eklenebileceğini önermesi**.
> En sonda bir inceleme listesi ve açık sorular var.

---

## 1. Proje bağlamı (incelemeyi yapacak kişi için)

- **Uygulama:** Lofi Boya. 3–7+ yaş çocuklar için bir boyama ve öğrenme uygulaması, Android'e çıkıyor (mağaza hedefli, yani ticari).
- **Motor:** Unity **6000.6.10f1** ve URP 17.6 (2D Renderer). Renk uzayı Linear, Input System açık.
- **Mimari:** Sahne dosyası kullanılmıyor, **her şey koddan kuruluyor**.
  - `App.cs` açılışta bir Screen Space Overlay Canvas kurar. Tasarım çözünürlüğü 1080×1920 dikeydir.
  - Ekranlar `MonoBehaviour, IScreen` sınıflarıdır ve `App.I.Show<T>()` ile geçilir.
  - Arayüz parçaları `UIKit` ile kodla üretilir: yuvarlak kutu, gölge, buton.
  - Resimler/ikonlar `Art`, `MascotArt`, `LearnArt` içinde SDF ile **kodla çizilir**. Harici görsel yok.
- **Diller:** 10 dil desteklenir: en, tr, de, fr, es, it, pt, ru, ar, hi. Metinler `Strings.cs` içindeki `anahtar|en|tr|…` tablosundadır.
  - Arapça için `LocText` harf birleştirme yapar.
  - `Speech.Say()` şu an **boş bir taslak**, yani seslendirme yok.
- **Mevcut sistemler:**
  - Yıldızlar: `SaveSystem.Stars`
  - Yaş bandı: `SaveSystem.AgeBand` (0: 3–4, 1: 5–6, 2: 7+)
  - Hareket azaltma: `Motion.Reduced`
  - Erişilebilirlik: `A11y`, TalkBack
  - Ses efektleri: `Sfx`, kodla sentezlenir
  - Animasyon: `Tween`

## 2. İstenen ve yapılan

**Kullanıcının isteği:**
- javocsoft.github.io/grabfold sitesindeki gibi **3D sayfa çevirmeli** bir kitap okuyucu
- **En az 20, ücretsiz ve yasal, resimli** hikaye
- Ana menüde "Boyamaya Başla"nın yanında bir **"Okuma"** butonu
- 3D görünüme ve doku tasarımına özen

**Yapılanlar:**
1. **21 resimli hikaye**, her biri kapak + 5 sayfa + "Son" sayfası, **10 dilde**.
2. **Kitaplık ekranı:** ahşap raflarda sert kapaklı kitaplar.
3. **Okuma ekranı:** GrabFold yönteminin C#'a uyarlanmış 3D sayfa kıvrılması.
4. **Resimler tamamen kodla çiziliyor.** İnternetten indirilen görsel yok; lisans riski yok.
5. Ana menü yeniden düzenlendi: tam genişlikte "Boyamaya Başla" ve altında eşit boyda 4 kutucuk.

### Neden GrabFold'un kodu doğrudan kullanılmadı?
- GrabFold TypeScript/DOM/CSS ile yazılmış bir **web kütüphanesi**. Unity'de HTML/CSS yok.
- Bir WebView içinde çalıştırmak hem ağır olurdu hem de uygulamanın arayüzü, sesi ve erişilebilirliğiyle bütünleşmezdi.
- GrabFold **MIT lisanslı**. Bu yüzden **yöntemi** (kat çizgisi geometrisi, silindir kıvrılması, %40 eşiği, fiske) C#'a yeniden uygulandı.
- Kaynak, `PageCurl.cs` dosyasının başındaki yorumda belirtildi.

### Neden hazır ücretsiz kitaplar (StoryWeaver, Book Dash) kullanılmadı?
- Araştırıldı. StoryWeaver ve Book Dash **CC BY 4.0** lisanslı, ticari kullanıma uygun.
- African Storybook / Global Storybooks'un bir kısmı ise **CC BY-NC**, yani ticari uygulamada kullanılamaz.
- Yine de şu karar verildi: masallar kamu malı (Ezop, Andersen, halk masalı) ya da özgün olsun, metinler yeniden yazılsın, resimler kodla çizilsin. Sebepler:
  - Lisans ve atıf yükü yok.
  - 10 dile çeviri tek elden, tutarlı yapılabildi.
  - Görsel üslup uygulamayla birebir aynı.
  - APK boyutu neredeyse hiç artmıyor.

---

## 3. Eklenen ve değişen dosyalar

| Dosya | Satır | Görev |
|---|---|---|
| `Assets/Scripts/Data/Books.cs` | ~216 | Kitap kataloğu: id, kapak rengi, yaş, kapak sahnesi, 5 sayfa sahnesi; okundu bilgisi (PlayerPrefs) |
| `Assets/Scripts/Data/BookStrings.cs` | ~191 | Tüm hikaye metinleri ve okuma ekranı yazıları, 10 dil (Strings tablosuyla aynı biçim) |
| `Assets/Scripts/UI/StoryArt.cs` | büyük | Sahne dilini okuyup 1024×768 resmi **arka plan iş parçacığında** SDF ile çizen ressam |
| `Assets/Scripts/UI/PageCurl.cs` | ~292 | 3D kıvrılma geometrisi (`PageCurl`), kıvrılan sayfa ağı (`PageMesh`), kat gölgesi (`CurlShadow`) |
| `Assets/Scripts/UI/PageStudio.cs` | ~208 | Sayfaları RenderTexture'a basan gizli dünya-uzayı Canvas + ayrı kamera; kâğıt dokuları |
| `Assets/Scripts/UI/BookPages.cs` | ~270 | Kapak / hikaye sayfası / "Son" sayfası düzenleri; kitap ikonu |
| `Assets/Scripts/Screens/BookScreen.cs` | ~591 | Okuma ekranı: sayfa yönetimi, sürükleme, otomatik çevirme, ödül |
| `Assets/Scripts/Screens/LibraryScreen.cs` | ~255 | Kitaplık ekranı: raf, kitap kartları, sıralama |
| `Assets/Scripts/Data/Strings.cs` | 1 satır | Ayrıştırıcı artık `Table + BookStrings.Table` okuyor |
| `Assets/Scripts/App.cs` | +2 | `ShowLibrary()`, `ShowBook(id)` |
| `Assets/Scripts/Audio/Sfx.cs` | +~20 | `Sfx.Page()`: sentezlenmiş sayfa çevirme sesi (hışırtı + "fıp") |
| `Assets/Scripts/Screens/WelcomeScreen.cs` | değişti | Yeni alt menü düzeni; `BigButton` yerine `Tile` |

**Commit yapılmadı.** Değişiklikler çalışma klasöründe duruyor.

---

## 4. Hikayeler

21 kitap, hepsi "kapak + 5 sayfa". Kaynak sütunu lisans durumunu gösterir.

| id | Türkçe başlık | Kaynak | Yaş | Mesaj |
|---|---|---|---|---|
| hare | Tavşan ile Kaplumbağa | Ezop (kamu malı) | 3+ | Sabır, istikrar |
| lion | Aslan ile Fare | Ezop | 3+ | Küçükler de yardım eder |
| ant | Karınca ile Ağustosböceği | Ezop (nazik son) | 5+ | Çalışma + paylaşma |
| crow | Akıllı Karga | Ezop | 5+ | Problem çözme |
| duckling | Çirkin Ördek Yavrusu | Andersen (kamu malı) | 5+ | Herkes kendi zamanında büyür |
| hen | Küçük Kırmızı Tavuk | Halk masalı (nazik son) | 5+ | Birlikte çalışmak |
| bear | Ayıcık Uyuyamıyor | Özgün | 3+ | Uyku zamanı |
| leaf | Kocaman Yaprak | Özgün | 3+ | Paylaşmak |
| seed | Küçük Tohum | Özgün | 3+ | Sabır, büyüme |
| fish | Balıkçık Renkleri Arıyor | Özgün | 3+ | Renkler |
| hedgehog | Kirpiye Sarılmak | Özgün | 3+ | Kabul, sevgi |
| kite | Uçurtma | Özgün | 5+ | Yardımlaşma |
| snowman | Kardan Adamın Şapkası | Özgün | 3+ | Cömertlik |
| frog | Kurbağa Sayıyor | Özgün | 3+ | 1'den 5'e sayma |
| snail | Salyangozun Yolculuğu | Özgün | 5+ | Yolculuğun tadını çıkarmak |
| ladybug | Uğur Böceğinin Benekleri | Özgün | 3+ | Dikkatli saymak |
| bee | Arının Hediyesi | Özgün | 3+ | Paylaşmak |
| moon | Ay Nereye Gitti? | Özgün | 5+ | Ayın evreleri |
| puppy | Köpekçiğin Doğum Günü | Özgün | 3+ | Arkadaşlık |
| seasons | Sincap ve Dört Mevsim | Özgün | 5+ | Mevsimler |
| birthday | Kedinin Hediyesi | Özgün | 5+ | Sevgiyle verilen hediye |

**Yazım kuralları:**
- Kısa cümleler, sayfa başına 1–2 cümle.
- Korkutucu öğe yok. Kurt gibi figürler bilinçli olarak dışarıda bırakıldı.
- Klasiklerin sert sonları yumuşatıldı. Örneğin karınca ağustosböceğini kapıdan çevirmiyor, yemeğini paylaşıyor.

**Metin tablosu:**
- Anahtarlar `bk_{id}_t` (başlık) ve `bk_{id}_{1..5}` (sayfalar).
- Her satırda 11 sütun var. Bir betikle kontrol edildi: hepsi 11 sütun, metinlerde `"` ya da `|` yok.

**Çeviri notu:** Tüm diller tarafımdan yazıldı. **Arapça ve Hintçe'nin anadili olan biri tarafından kontrol edilmesi önerilir.** Almanca/İspanyolca gibi dillerde konuşma çizgisi ve tırnak biçimleri o dilin kurallarına göre seçildi.

---

## 5. Resim sistemi — `StoryArt.cs`

### 5.1 Sahne dili
Her sayfanın resmi `Books.cs` içinde tek satırlık bir metindir:

```
"pond; lily .5 .15 .3; frog .5 .14 .34 happy; lily .2 .16 .16"
 zemin ; öğe   x   y   boy  [bayraklar]
```

- **Zemin:** `meadow`, `forest`, `garden`, `pond`, `farm`, `sky`, `night`, `rain`, `snow`, `sunset`, `under` (deniz altı), `home`.
  - İki ek ayar var: `home:night` ve `meadow:autumn`.
- **x, y:** resmin sol alt köşesine göre 0..1 arası değerler (y yukarı doğru).
- **boy:** resim yüksekliğine oranla.
- **Bayraklar:**
  - `f`: aynala
  - `happy`, `sad`, `sleep`, `wow`: yüz ifadesi
  - `n=3`: sayı ayarı (benek sayısı, ay evresi, mevsim, mum sayısı…)
  - `c=FF8899`: renk

### 5.2 Karakterler ve nesneler
- **Önden bakan hayvanlar** tek bir parametreli `Critter()` fonksiyonundan çıkar: tavşan, yaban tavşanı, ayı, kedi, köpek, fare, aslan (yeleli), tilki, sincap, kirpi (dikenli), kurbağa.
- **Yandan bakan kuşlar** `Bird()` fonksiyonundan çıkar: ördek yavrusu, gri yavru, kuğu (S boyunlu), tavuk, karga, serçe. Kuğular ve göletteki ördekler "yüzer": bacak çizilmez, su yüzeyi gövdeyi keser.
- **Ayrı fonksiyonu olan karakterler:** baykuş, uğur böceği, arı, kelebek, karınca, ağustosböceği, salyangoz, balık, kaplumbağa, denizyıldızı, yengeç, kardan adam.
- **Yaklaşık 45 nesne:** güneş (yüzlü), ay (5 evre), yıldız, bulut (yağmurlu), gökkuşağı, ağaç (4 mevsim), çam (karlı), çiçek, çimen, mantar, yarış bayrakları, cam testi (su seviyesi), ağ, ip, buğday, ekmek, dev yaprak, filiz (4 evre → ayçiçeği), sulama kabı, uçurtma, şapka, dal, karınca yuvası, nilüfer yaprağı, bal kavanozu, kovan, pasta, hediye, balon, top, palamut, yuva, baloncuk, kalp, nota, "zZ", parıltı, çakıl, battaniye, yatak, vazo.

### 5.3 Görsel dil (3D ve doku hissi)
Uygulamanın Pofuduk / LearnArt üslubuyla aynıdır:
- Koyu mor **kontur**: `#5E4B6E`, resim yüksekliğinin 0.0042'si kalınlığında.
- Her şeklin üstten alta **renk geçişi**.
- **Parlama** lekeleri, karakterin altında **yere düşen yumuşak gölge**, pembe **yanaklar**.
- **Derinlik:** arka tepeler daha soluk ve mavimsi. Hava perspektifiyle üç katman tepe var; ön planda çimen tutamları.
- **Hava efektleri** karakterlerin önünde: yağmur çizgileri, kar taneleri, deniz altı ışık huzmeleri ve baloncuklar, sonbahar yaprakları, bahar yaprakları.
- **Son işlem:**
  - İki ölçekli değer gürültüsüyle **suluboya kâğıdı** dokusu
  - Kenarlarda hafif kararma
  - **Yuvarlak köşeler** (alfa kanalında)

### 5.4 Performans ve iş parçacığı
- `StoryArt.Paint()` saf C# hesabıdır. Unity nesnesine dokunmaz.
  - `ColorUtility` yerine kendi hex çözümleyicisini kullanır.
  - `Mathf.PerlinNoise` yerine kendi gürültüsünü kullanır.
  - Bu yüzden `Task.Run` ile **arka planda** çalışır.
- Doku ana iş parçacığında oluşturulur: `PaintAsync` bir coroutine ile görevi bekler.
- WebGL'de senkron yedek yol var.
- Her şekil yalnızca kendi sınır kutusu içindeki pikselleri dolaşır.
- Masaüstünde 512×384 boyutunda sahne başına **~20–30 ms** sürüyor (126 resim 3.3 sn). 1024×768 yaklaşık 4 katıdır, yani ~100 ms. Ekran donmaz.
- **Doğrulama:** Resim kodu Unity'den bağımsız bir .NET test programında (Unity tipleri için küçük taslaklarla) çalıştırıldı.
  - 126 resmin tamamı çizildi ve tek tek gözle incelendi.
  - Bu inceleme sonucunda düzeltilenler: kuğu suyundaki dikdörtgen izi, üstten taşan ağaçlar, suyun içinde duran kurbağa (altına nilüfer eklendi).

---

## 6. 3D sayfa çevirme — `PageCurl.cs`

### 6.1 Geometri (GrabFold yönteminin uyarlaması)
- **Sayfa koordinatları:** sırt `x=0`, sağ üst `(W,H)`.
- **G0** tutulan noktadır, **G** parmağın şu anki yeridir.
- **Kat çizgisi:** G0 ile G'nin **orta dikmesi**. Normal `n = (G0−G)/|G0−G|`.
- **Silindire sarma:** Çizginin ötesindeki her nokta, çizgiye uzaklığı `d` ile hesaplanır:
  - `d < πR` ise silindir üzerinde: açı `θ = d/R`, `x += R·sinθ`, `z = R(1−cosθ)`.
  - `d ≥ πR` ise arka yüz düz yatar: `z = 2R`.
  - Tutulan nokta tam parmağın altına gelsin diye kat çizgisi `πR/2` kadar kaydırılır.
- **Yarıçap:** `R = min(0.12W·(1−ilerleme)^0.8, |G0−G|/π)`. Sayfa döndükçe kâğıt düzleşir.
- **Sırta bağlılık:** G, sırtın alt ve üst köşesine G0'dan daha uzağa gidemez. Kâğıt yırtılmaz, sırttan kopmaz.
- **Perspektif:** `f = D/(D−z)` ve `D = 2.6H`. Kalkan kâğıt bakana doğru hafifçe büyür.

### 6.2 Çizim katmanları
34×48'lik bir ızgara her karede hesaplanır. Aynı ızgara farklı katmanlarda süzülür. Üçgenin önü mü arkası mı göründüğüne, ekrandaki işaretli alanına bakılarak karar verilir.

| Katman | İçerik | Işık/renk |
|---|---|---|
| `Under` | Alttaki sayfa (düz) | — |
| `CurlShadow (Under)` | Kıvrımın alttaki sayfaya düşen gölgesi | Dönüşün ortasında en koyu |
| `Flat` | Ön yüzün düz kalan kısmı | — |
| `CurlShadow (Crease)` + `BackShadow` | Kıvrım dibindeki karanlık + dönen yaprağın gölgesi | RectMask2D ile sayfaya kırpılır |
| `Curl` | Ön yüzün kıvrılan kısmı | Kıvrıldıkça kararır |
| `Back` | Arka yüz (kâğıt dokusu) | Silindir tepesinde parlama |
| `Ghost` | Arkadan soluk görünen baskı (%9) | Ayna görüntüsü, gerçek kâğıt gibi |

### 6.3 Sayfa dokuları — `PageStudio.cs`
- Yazıların da sayfayla birlikte kıvrılması gerekiyordu. Bu yüzden sayfalar normal arayüz parçalarıyla (Text, RawImage) gizli bir **dünya-uzayı Canvas'ında** kurulur.
- Katman 31'deki ayrı bir kamera sayfayı bir **RenderTexture**'a basar. Ana kameranın bu katmanı görmemesi sağlanır.
- URP'de `RenderPipeline.StandardRequest` + `SubmitRenderRequest` kullanılır. Desteklenmezse `Camera.Render()` yedeği devreye girer.
- RT boyutu ekrandaki sayfa piksel yüksekliğine göre ayarlanır (480–2048). Mipmap ve anizotropik filtre açıktır.
- Yazı keskinliği için `CanvasScaler.dynamicPixelsPerUnit = rtHeight / 1400`.
- **Bellek:** Sadece gösterilen sayfanın ±2 komşusu bellekte tutulur, gerisi bırakılır.

### 6.4 Sayfa tasarımları — `BookPages.cs`
Tasarım boyutu 952×1400 birim.

- **Hikaye sayfası:**
  - Krem kâğıt, iki ölçekte lif dokusu (`Art.Grain`), kenarlarda sararma
  - Sırtta kıvrım gölgesi ve ince ışık çizgisi
  - Altında yumuşak gölgesi olan resim
  - Ortalanmış metin (en çok 60 pt, sığmazsa küçülür)
  - Sayfa numarası ve iki yanında yıldızlar
- **Kapak:**
  - Bez cilt (renk geçişi + açık ve koyu iki kat doku), sırt menteşesi ve oluk
  - **Yaldızlı çerçeve** (köşelerde yıldız)
  - Kâğıt dokulu **başlık etiketi**, beyaz **paspartulu** ve hafif eğik resim
  - Yaş etiketi, **cila parıltısı** ve kenar koyuluğu
- **"Son" sayfası:** Büyük "Son" yazısı, kalp ve yıldız süsleri, kapak resmi, başlık.

### 6.5 Okuma ekranı — `BookScreen.cs`
- **Sayfa sırası:** 0 kapak, 1..5 hikaye, 6 "Son". Altta noktalarla ilerleme gösterilir.
- **Etkileşim:**
  - **Sürükleme:** Sayfanın her yerinden tutulabilir. İleri için sola, geri için sağa çekilir; geri çekişte hareket 1.6× hızlanır.
  - **Bırakma:** İleri çevirmede ilerleme > %40 ise sayfa döner. Geri çevirmede < %60 ise döner. Hızlı fiske eşikten bağımsız olarak çevirir.
  - **Dokunma:** Sağ kenar ileri, sol kenar geri çevirir. Alttaki ‹ › okları da aynı işi yapar.
  - Kapakta beklerken köşe iki kez hafifçe kalkar ("beni çevir" ipucu).
- **Ses ve dokunsal geri bildirim:** Sayfa dönerken `Sfx.Page()` ve `Haptics.Light()`.
- **Hareket azaltma:** `Motion.Reduced` açıksa oklar ve dokunma sayfayı kıvrılmadan değiştirir. Elle sürükleme yine çalışır.
- **Ödül:** "Son" sayfasına ilk varışta konfeti, `Sfx.Win`, **+1 yıldız** ve bir bildirim gösterilir; kitap "okundu" işaretlenir. Altta "Tekrar oku" ve "Kitaplık" butonları çıkar.
- **Erişilebilirlik:**
  - Sayfa değişince metin `A11y.Announce` ile okunur.
  - Oklar ve geri butonu etiketlidir.
  - `Speech.Say` doldurulursa seslendirme kolayca eklenir.
- **Ekran döndürme / boyut değişimi:** Kitap yeniden ölçülür ve sayfalar yeniden basılır.

### 6.6 Kitaplık — `LibraryScreen.cs`
- 2 sütun (tablette 3). Hücreler bitişik olduğu için raf tahtaları tek uzun raf gibi görünür. Ahşap doku kodla üretilir: damar ve budak.
- **Kitap kartı:** sayfa yığını kenarı, bez dokulu kapak, sırt bandı, paspartulu resim, başlık, cila parıltısı.
- Okunan kitapta yıldız rozeti var. Yaş etiketi ("3+ yaş") gösterilir.
- Kitaplar hafifçe eğik durur ve sırayla "pop" animasyonuyla gelir.
- Kapak resimleri (480×360) arka planda çizilir ve gelince yavaşça belirir. Ekrandan çıkınca dokular silinir.
- **Sıralama:**
  1. Çocuğun yaş bandına uygunlar
  2. Okunmamışlar
  3. Katalog sırası

---

## 7. Ana menü düzenlemesi — `WelcomeScreen.cs`

**Önce:**
- Üst satırda geniş "Boyamaya Başla" ve yanında "Okuma"
- Alt satırda üç yatay buton
- İki farklı boy ve iki rozet ("21" ve "67")

Bu düzen dağınık görünüyordu.

**Sonra:**
- Tam genişlikte (1000×160), "nefes alan" **Boyamaya Başla**
- Altında eşit boyda **4 kutucuk** (235×215, aralarında 20 birim). Her kutucukta pastel kart, üstte beyaz daire içinde renkli ikon, altta ad var. Sırasıyla:
  - Okuma (lila)
  - Öğren (nane)
  - Resimlerim (mavi)
  - Çıkartmalar (sarı)
- Tek rozet kaldı: yıldız sayısı, Çıkartmalar ikonunun köşesinde.
- Kutucuklar sırayla "pop" animasyonuyla gelir.

---

## 8. Test ve doğrulama

| Ne | Nasıl | Sonuç |
|---|---|---|
| Derleme | Unity Editor'de `unity recompile` | Hata yok. Uyarılar `[NonSerialized]` ile giderildi |
| 126 sahne resmi | Unity dışı .NET test programı + temas tabloları | Tamamı çizildi; bulunan görsel kusurlar düzeltildi |
| Ana menü | Play modu + ekran görüntüsü | Yeni düzen doğru görünüyor |
| Kitaplık | Play modu + ekran görüntüsü | Raflar, kapaklar, başlıklar doğru |
| Kapak (RT basımı) | Play modu + ekran görüntüsü | Yazı ve doku RT'de net |
| Yarıda kıvrılmış sayfa | Kod üzerinden G'yi ayarlayıp ekran görüntüsü | Kıvrım, arka yüz, gölge ve ayna baskı doğru |
| Otomatik sayfa çevirme | `AutoTurn(+1)` | Sayfa 1'e geçti, hata yok |
| "Son" sayfası + ödül | `ShowPage(6)` | Konfeti, +1 yıldız bildirimi, butonlar çıktı |
| Konsol | `console get` | Hata/uyarı yok |

**Test sırasında düzeltilen hata:** Özel Graphic sınıflarında `CanvasRenderer` eksikliği istisnası çıktı.
- `[RequireComponent(typeof(CanvasRenderer))]` eklendi ve bileşen elle de ekleniyor.
- Yeniden kurulumda maske kaydının temiz silinmesi için kitap önce pasif yapılıp sonra yok ediliyor.

**Test sırasında değişen veri geri alındı:** "hare" okundu işareti kaldırıldı, yıldız 67'ye döndü.

**Yapılmamış testler:**
- **Gerçek parmakla sürükleme** (dokunmatik ya da fare) denenmedi. Sürükleme kodu incelendi ama elle oynanmadı.
- Gerçek Android cihazda performans ve RenderTexture davranışı denenmedi.
- Arapça (sağdan sola) uzun metinlerin satır kaydırması görsel olarak kontrol edilmedi.
- Tablet yatay mod ve ekran döndürme denenmedi.

---

## 9. Bilinen sınırlamalar ve riskler

1. **Arapça satır kaydırma:** `LocText` metni tek parça çeviriyor. Unity Text satır kaydırınca çok satırlı Arapça metinde satır sırası bozulabilir. Uygulamanın geri kalanında da aynı durum var.
2. **Sağdan sola kitap yönü:** Arapça'da kitaplar normalde sağdan sola açılır. Şu an sırt her dilde solda.
3. **Seslendirme yok:** `Speech.Say` boş. Sesli okuma, okuma bilmeyen 3–4 yaş için en büyük eksik.
4. **RenderTexture bellek:** 3–5 sayfa × ~1100×1600 × 4 bayt + mipmap yaklaşık 25–40 MB tutar. Düşük bellekli cihazlarda izlenmeli.
5. **`RenderPipeline.StandardRequest`** URP 2D Renderer'da Editor'de çalıştı. Android'de doğrulanmadı.
6. **Katman 31** sayfa stüdyosuna ayrıldı. Projede başka bir şey bu katmanı kullanırsa çakışır.
7. Kapak sayfasının arka yüzü düz kâğıt. Gerçek kitapta bu bir "iç kapak" olurdu.
8. Kitap 5 sayfadan uzun olursa sayfa yığını efekti en fazla 4 yaprak gösteriyor (bilinçli sınır).
9. Resimler her açılışta yeniden çiziliyor (önbellek yok). Hızlı olduğu için şimdilik sorun değil.

---

## 10. İnceleyen kişiye: kontrol listesi

- [ ] `PageCurl.Update()` sırt kısıtı: G'nin iki çember dışına çıkmaması 4 yinelemede yeterli mi? Uç durumlar: sırta çok yakın tutmak, çok yukarı/aşağı çekmek.
- [ ] `PageMesh.Tri()` ön/arka ayrımı: sınırda kalan üçgenler titreşiyor mu?
- [ ] `BookScreen.OnEndDrag` eşikleri (%40 / %60, fiske = 1.4 sayfa genişliği/sn) çocuk parmağı için uygun mu?
- [ ] `BookScreen.Page()` ve `Trim()`: RenderTexture sızıntısı var mı? Ekran kapanırken hepsi bırakılıyor mu?
- [ ] `StoryArt.PaintAsync`: ekran kapandıktan sonra biten görev doku sızdırıyor mu? (`owner == null` kontrolü var, ama doku hiç oluşturulmuyor mu?)
- [ ] `Strings.Parse` iki tabloyu birleştiriyor: anahtar çakışması var mı?
- [ ] Çocuk güvenliği ve pedagoji: hikaye metinleri ve mesajlar yaşa uygun mu? Korkutucu öğe var mı?
- [ ] Çeviriler, özellikle ar ve hi.
- [ ] Erişilebilirlik: TalkBack ile kitap okunabiliyor mu?
- [ ] Ana menü: 4 kutucuk küçük ekranda (ör. 360 dp genişlik) sığıyor mu, yazılar okunuyor mu?

---

## 11. Eklenebilecekler (öneri olarak sorulacaklar)

Şu an düşündüklerim. İnceleyen kişinin görüşü ve öncelik sıralaması isteniyor:

1. **Sesli okuma:**
   - Android TTS'i `Speech.Say` üzerinden bağlamak
   - Okunan kelimenin vurgulanması
   - "Bana oku" / "Kendim okuyacağım" modu
2. **Hikayeden boyamaya:** Her kitabın sonunda o hikayenin bir sahnesini boyama sayfası olarak açmak. `StoryArt` çizgi + bölge haritası üretebilir.
3. **Etkileşimli sayfalar:** Karaktere dokununca zıplama/ses; sayma kitabında nesnelere dokunup saydırma.
4. **Arapça için sağdan sola kitap.**
5. **Daha fazla kitap / sezon paketleri:** Sahne dili sayesinde yeni kitap = birkaç satır metin.
6. **Okuma istatistikleri** (ebeveyn ekranında): kaç kitap, ne kadar süre.
7. **"Kendi kitabını yap":** Çocuğun boyadığı resimlerden kitap oluşturma.
8. **Gece modu / uyku hikayeleri rafı:** Ekran kararır, müzik yumuşar.
9. **Sayfa başına küçük soru** (anlama): "Kaplumbağa neden kazandı?"
10. **Telefonda iki sayfalık açık kitap**, tablet yatayda.

---

## 12. Kaynaklar

- GrabFold: https://github.com/JavocSoft/grabfold (MIT). Sayfa kıvrılma yöntemi buradan uyarlandı.
- Araştırılan ama kullanılmayan açık lisanslı kitap kaynakları:
  - StoryWeaver (CC BY 4.0): https://storyweaver.org.in/about
  - Book Dash (CC BY 4.0): https://bookdash.org/books/
  - Global Storybooks (karışık; bir kısmı CC BY-NC): https://global-asp.github.io/
- Masallar: Ezop ve H. C. Andersen (kamu malı), Küçük Kırmızı Tavuk (halk masalı). Metinler yeniden yazıldı.

---

## 13. İnceleme sonrası yapılanlar (2. tur)

Başka bir yapay zekânın incelemesindeki maddeler tek tek kodda doğrulandı ve uygulandı. Her madde Unity'de Play modunda test edildi; konsolda hata yok.

**Sayfa çevirme hissi**
- **Eşik:** Tek sayfada `Progress`, sağ kenardan sırta kadar 0.5 olur. İleri çevirme eşiği 0.40 → **0.22** yapıldı (sayfanın ~%44'ü), geri çevirme eşiği 0.60 → **0.72**.
  - Fiske eşiği 0.9 sayfa/sn'ye indi ve yalnızca son 80 ms içinde hızlıysa sayılıyor.
  - Test: sağ kenardan sayfanın yarısına sürükleyip bırakınca sayfa döndü.
- **İpucu sırasında dokunma:** "Beni çevir" animasyonu sırasında parmak gelince animasyon hemen biter, sürükleme parmağa geçer.
- **Yön kilidi:** Sayfanın sağ %30'undan tutmak her zaman ileri, sol %30'undan tutmak her zaman geri çevirir; ilk karedeki titreme yönü belirlemez. Yön imkânsızsa (kapakta geri gibi) ters yön denenir.
- **Sırt:** Kat çizgisinin πR/2 kayması, sırtın iki köşesinin kata uzaklığıyla sınırlandı. Kâğıt hiçbir açıda ciltten kalkmaz.

**Android / erişilebilirlik / metin**
- **Arka plandan dönüş:** `OnApplicationPause(false)` ve `OnApplicationFocus(true)` tüm sayfaları yeniden basılacak işaretler. `Update` de `!IsCreated()` olan sayfayı yeniden basar.
- **TalkBack:** Sayfa alanı artık `IA11yAction`.
  - Odaklanınca kapakta başlığı, sayfada metni, sonda "Son"u söyler; çift dokunuş sonraki sayfaya geçer.
  - Kapak ve "Son" sayfasında da `Announce` çağrılıyor.
- **Best Fit taşması:** Best Fit kullanan tüm yeni yazılarda `Wrap` + `Truncate` açıldı: menü kutucukları, Başla, kitaplık başlık/alt yazı/yaş etiketi, okuma ekranı başlığı ve butonları.
- **Arapça satır sırası:** Hikaye metni, kapak başlığı ve son sayfa başlığı Arapçada kelime kelime ölçülüp (bitişik harflerle) `\n` ile satırlara bölünüyor. `ArabicShaper` satır satır şekillendirdiği için sıra doğru.
  - Test: Arapça uzun sayfada cümlenin başı ilk satırda.

**İçerik**
- **Kurbağa 4. sayfa:** Bir boş nilüfer kaldırıldı. Şimdi tam 4 nilüfer var (biri kurbağanın altında).
- **Uğur böceği:** 7. benek (0.085, 0.385) konumuna taşındı, diğer beneklerle çakışmıyor.
- **Ayıcık:** 3. sayfa artık odada geçiyor (baykuş pencerede, yıldızlar pencerede). 4. sayfadaki yıldızlar oda yerine pencerede; 5. sayfadaki fazladan ay kaldırıldı.
- **Kedinin Hediyesi:** 3. sayfada kedinin yanında çiçekler var.
- **Köpekçik:** 5. sayfada mumlar üflenmiş (fitil + duman). `cake` için `sleep` bayrağı söndürülmüş mum anlamına gelir.
- **Hintçe:**
  - `चूज़े/भूरा` → `बत्तख के बच्चे/स्लेटी`
  - `बलूत` → `मेवे`
- **Arapça:**
  - Gri yavru baştan sona dişil (البطّة الرمادية).
  - `برطمان` → `وعاء من العسل`
- **Fransızca yaş etiketi:** "3 ans +" → "dès 3 ans".

**Performans ve bellek**
- **Izgara:** `PageCurl.Version` eklendi. Izgara karede bir kez hesaplanıyor, beş katman aynı sonucu kullanıyor.
- **Alt Canvas:** Kitap `UIKit.SubCanvas` içinde; sayfa kıvrılırken ekranın geri kalanı yeniden kurulmuyor.
- **RenderTexture:** Mipmap kapatıldı.
  - **Derinlik tamponu tamamen kaldırılamadı:** URP Render Graph derinliği olmayan hedefe çizmeyi reddetti ("Fake or uninitialized surface"). Bu yüzden 24 → **16 bit** yapıldı.
- **Resim kuyruğu:** `StoryArt.PaintAsync` artık sıraya alıyor, aynı anda en çok 2 resim boyuyor. Sırası gelmeden ekran kapanırsa resim hiç başlamıyor.
- **Kâğıt arkası dokusu:** Döşeniyor (6×8.5), bulanıklık gitti.
- **Deniz altı ışık huzmeleri:** Dar sınır kutusu kullanıyor.
- **Kitaplık:** Tablet döndürülünce (genişlik değişince) yeniden diziliyor.

**Tasarım dokunuşları**
- **Kitaplık:**
  - Rafın son gözünde oturan Pofuduk: "Bugün hangi masalı okuyalım?"
  - Filtre şeridi: Tümü · 3+ · 5+ · Uyku vakti. Uyku kitapları: ayıcık, ay, salyangoz, kirpi.
  - Kapaklar önbellekte tutuluyor, filtre değişince yeniden çizilmiyor.
- **Anında açılış:** Kitaplıkta çizilmiş kapak kitaba devrediliyor. Kitap beklemeden açılıyor, büyük resim gelince netleşiyor.
- **Kapak ve iç kapak:**
  - Kapak sayfasında üstteki başlık gizli, içeride yavaşça beliriyor.
  - Kapağın arkası "iç kapak": kapak renginin açık pastel tonu, baskı izi yok.
- **Kitap detayları:** Alttan sarkan saten kurdele ayraç. İlerleme noktalarının sonuncusu yıldız.
- **Dokunulabilir resimler:**
  - Karaktere dokununca kalpler fışkırıyor ve kıkırdama sesi çıkıyor; nesnelere dokununca yıldızlar fışkırıyor ve "pop" sesi çıkıyor. Hareket azaltmada sade bir parıltı.
  - Sayma kitabında (`Books.Book.Counts`) dokunulan her öğenin üstünde sıradaki sayı beliriyor; hepsi sayılınca konfeti ve alkış.
  - Dokunulan bir şey varsa sayfa çevrilmez; boş yere dokunmak sayfayı çevirir.
- **"Son" sayfası:**
  - Pofuduk sol alttan gelip "Ne güzel bir hikayeydi!" diyor.
  - Tekrar okuyunca da kutlama oluyor (küçük konfeti + alkış); yıldız yalnızca ilk okuyuşta.
- **Çıkartma albümü:** Hikaye okudukça açılan 3 Pofuduk:
  - Kitap kurdu (1 kitap)
  - Tatlı rüyalar (5 kitap)
  - Masalcı (21 kitap, taçlı)
- **Ebeveyn raporu:** "Okunan hikaye" kutusu eklendi (`Stats.Add("book")`, yalnızca ilk okuyuşta).

**Ayrıca düzeltilen eski hata:** `ColoringCanvas` her açılışta `EnhancedTouchSupport.Enable()` çağırıyor, hiç `Disable()` çağırmıyordu. Domain reload kapalı Play'den çıkarken bu `ArgumentOutOfRangeException` veriyordu. Artık `OnDisable` içinde eşleniyor.
