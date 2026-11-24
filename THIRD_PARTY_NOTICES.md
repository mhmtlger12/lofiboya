# Üçüncü Taraf Bildirimleri — Lofi Boya

Uygulamada kullanılan dış kaynaklar ve lisansları. Mağaza sayfasında ve (eklendiğinde) ebeveyn menüsündeki
"Lisanslar" bölümünde bu bilgiler gösterilmelidir.

## Yazı tipleri (SIL Open Font License 1.1)

| Yazı tipi | Dosya | Kullanım | Lisans metni |
| --- | --- | --- | --- |
| Baloo 2 — © 2019 The Baloo 2 Project Authors | `Assets/Resources/Fonts/Main.ttf` | Latin + Devanagari (Hintçe) | `Assets/Licenses/Baloo2-OFL.txt` |
| Cairo — © 2009 The Cairo Project Authors | `Assets/Resources/Fonts/Arabic.ttf` | Arapça | `Assets/Licenses/Cairo-OFL.txt` |
| Nunito — © 2014 The Nunito Project Authors | `Assets/Resources/Fonts/Cyrillic.ttf` | Kiril (Rusça) | `Assets/Licenses/Nunito-OFL.txt` |

OFL, yazı tipinin uygulamayla birlikte dağıtılmasına izin verir; lisans metni yazı tipiyle birlikte verilmelidir.
Lisans dosyaları `Resources` dışına taşındı (oyun paketine gereksiz yere girmesinler); bildirim bu belgede ve
uygulama içi lisans sayfasında yer almalıdır.

## Emoji görselleri (MIT)

- **Microsoft Fluent Emoji (3D)** — © Microsoft Corporation, MIT lisansı.
  `Assets/Resources/Emoji/` altındaki atlaslar bu setten `tools/emoji_paketle.py` ile üretilmiştir.
  MIT lisansı, telif bildiriminin ve lisans metninin kopyalarla birlikte verilmesini ister.

## Boyama sayfaları

- Seviye kimlikleri (`Assets/Resources/Levels/index.json`) kaynak sitenin kullanıcı adı ve görsel numarasını taşır
  (ör. `gdj-dolphin-6387856`). Bu adlandırma Pixabay / OpenClipart / Clker kaynaklarına işaret eder.
  **Yapılacak:** her kaynağın lisansını (Pixabay Content License, CC0 vb.) tek tek doğrulayıp burada listeleyin.
- `lofi-*` ile başlayan sayfalar projenin kendi çizimleridir.

## Ses ve müzik

- Tüm efektler ve lofi müzik kodla üretilir (`Sfx.cs`, `LofiMusic.cs`); dış ses dosyası yoktur.
