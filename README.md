# Lofi Boya

3–7+ yaş çocuklar için çevrimdışı boyama, hikaye ve erken öğrenme uygulaması (Android).
Reklam ve üçüncü taraf SDK yoktur.

- **Motor:** Unity `6000.6.10f1`, URP 2D, uGUI. Renk uzayı Linear, Input System.
- **Proje klasörü:** `LofiBoya/` (Unity projesi). Tek sahne; arayüzün tamamı koddan kurulur (`App.cs`).
- **Diller:** en, tr, de, fr, es, it, pt, ru, ar, hi — metinler `Assets/Scripts/Data/Strings.cs` ve `BookStrings.cs`
  içinde `anahtar|en|tr|…` tablosundadır. Arapça şekillendirme ve Hintçe `ि` düzeni `Loc.cs` içindeki `LocText` ile yapılır.

## Klasörler

| Yol | İçerik |
| --- | --- |
| `LofiBoya/Assets/Scripts/` | Oyun kodu (ekranlar, boyama motoru, veri, ses, UI) |
| `LofiBoya/Assets/Editor/` | Android build ve içe aktarma araçları |
| `LofiBoya/Assets/Resources/Levels/` | 143 boyama sayfası (`level.json`, `lines`, `regions`, `thumb`) |
| `LofiBoya/Assets/Resources/Emoji/` | Fluent Emoji atlasları |
| `LofiBoya/Assets/Licenses/` | Yazı tipi lisansları (paket dışı) |
| `tools/` | Seviye/emoji paketleme ve mağaza görseli betikleri |
| `keys/` | İmza anahtarı (**git'e girmez**) |

## Build (Play Store AAB)

1. İmza: `keys/upload-keystore.jks` ve `keys/key.properties` (`storePassword=`, `keyAlias=`, `keyPassword=`).
   Editör açılışta bunları okur; şifreler hiçbir proje dosyasına yazılmaz.
2. Unity menüsü: **Lofi Boya → Play Store için AAB Oluştur**. Sürüm kodu her build'de bir artar.
   Bu komut Android ayarlarını ve **veri toplama kapalı** gizlilik ayarlarını da uygular
   (**Lofi Boya → Gizlilik Ayarlarını Uygula** ile ayrıca da çalıştırılabilir).
3. Çıktı: `LofiBoya/Builds/LofiBoya-<sürüm>-<kod>.aab`.

## İçerik ekleme

- **Yeni dil metni:** `Strings.cs` tablosuna satır ekleyin; 10 sütunun hepsini doldurun. Metinde `|` ve `"` kullanmayın.
- **Yeni boyama sayfası:** `tools/` betikleriyle `Resources/Levels/<grup>/<id>/` klasörünü üretip `index.json`'a ekleyin.

## Kayıtlar

İlerleme `Application.persistentDataPath/saves` altında JSON + PNG olarak tutulur. Yazımlar önce `.tmp` dosyasına
yapılıp yerine taşınır (yarıda kesilen yazım eski kaydı bozmaz).

## Lisanslar

Bkz. [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
