"""
Emoji paketleyici
=================
Microsoft Fluent Emoji 3D PNG'lerini (256x256) küçültüp kategori başına atlaslara dizer.
Oyunda Emoji.cs bu atlasları ve index.json'u okur.

Kullanım:
    python tools/emoji_paketle.py <kaynak_klasör>
    (varsayılan kaynak: ../lofi_assets_full/lofi_assets_full)

Çıktı: LofiBoya/Assets/Resources/Emoji/
    <kategori>_<n>.png   atlas sayfaları (hücre 144 px, resim 136 px + 4 px şeffaf pay: komşu sızmasın)
    index.json           her emoji: kategori, sayfa, hücre, baskın renk (renk oyunu için)

Boyut: 1524 x 256 px PNG (~53 MB) -> ~1450 x 136 px atlas; Unity'de ASTC ile sıkıştırılır (~12 MB).

Çocuk uygulamasına uygun olmayan ya da çocuk için anlamı olmayan birkaç emoji alınmaz (aşağıdaki liste):
dini semboller ve ibadethaneler (her aileye tarafsız kalmak için), burçlar, kan grubu ve Japonca yazılı
düğmeler, para, tehlike işaretleri, sarhoş yüz ve benzerleri. Listeden çıkarılan ad yeniden pakete girer.
"""
import json
import os
import sys

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "LofiBoya", "Assets", "Resources", "Emoji")
CELL, ICON, COLS, MAXROWS = 144, 136, 14, 14

# Kategori klasörü -> oyundaki kısa ad (Emoji.cs ile aynı)
CATS = {
    "Meyveler": "fruit", "Sebzeler": "veg", "Yiyecek_ve_Icecek": "food", "Hayvanlar": "animal",
    "Hayvan_Yuzleri": "animalface", "Hayvanlar_ve_Doga": "wild", "Doga_ve_Mevsimler": "nature",
    "Tasitlar": "vehicle", "Tasitlar_ve_Mekanlar": "place", "Oyun_Spor_ve_Aktivite": "play",
    "Esyalar_ve_Okul": "object", "Duygular_ve_Ifadeler": "face", "Insan_ve_Karakterler": "people",
    "Semboller_ve_Sekiller": "symbol",
}

SKIP = set("""
latin_cross orthodox_cross star_of_david star_and_crescent om khanda wheel_of_dharma menorah place_of_worship
church mosque synagogue hindu_temple shinto_shrine kaaba prayer_beads
aries taurus gemini cancer leo virgo libra scorpio sagittarius capricorn aquarius pisces ophiuchus
a_button_blood_type ab_button_blood_type b_button_blood_type o_button_(blood_type)
japanese_acceptable_button japanese_application_button japanese_bargain_button japanese_congratulations_button
japanese_discount_button japanese_free_of_charge_button japanese_here_button japanese_monthly_amount_button
japanese_no_vacancy_button japanese_not_free_of_charge_button japanese_open_for_business_button
japanese_passing_grade_button japanese_prohibited_button japanese_reserved_button japanese_secret_button
japanese_service_charge_button japanese_vacancy_button circled_m
dollar_banknote euro_banknote pound_banknote yen_banknote money_bag money_with_wings credit_card atm_sign
currency_exchange heavy_dollar_sign chart_increasing_with_yen money-mouth_face
biohazard radioactive military_helmet
woozy_face biting_lip lying_face
male_sign female_sign transgender_symbol copyright registered trade_mark
cl_button cool_button free_button id_button new_button ng_button ok_button sos_button up!_button vs_button
input_latin_letters input_latin_lowercase input_latin_uppercase input_symbols
""".split())

BASIC = ["red", "orange", "yellow", "green", "blue", "purple", "pink", "brown"]

# Renk oyunu: rengi tartışmasız nesneler. Otomatik renk tespiti güvenilir değildi (kestaneye "kırmızı" dedi),
# öğrenme oyununda yanlış cevap öğretmemek için elle seçildi ve her biri gözle kontrol edildi.
COLOR_SETS = {
    "red": "red_apple strawberry cherries tomato hot_pepper rose fire_engine lady_beetle boxing_glove ribbon red_heart",
    "orange": "tangerine carrot jack-o-lantern orange_heart basketball orange_book fox",
    "yellow": "banana lemon ear_of_corn star baby_chick yellow_heart taxi cheese_wedge sunflower",
    "green": "green_apple broccoli cucumber kiwi_fruit leafy_green frog turtle crocodile four_leaf_clover cactus green_book green_heart evergreen_tree seedling pea_pod lizard",
    "blue": "blue_heart spouting_whale dolphin blue_book jeans droplet fish snowflake ice",
    "purple": "grapes eggplant purple_heart umbrella closed_umbrella hyacinth",
    "pink": "pig_face flamingo cherry_blossom tulip pink_heart hibiscus pig",
    "brown": "chestnut teddy_bear cookie chocolate_bar beaver brown_heart basket violin wood coconut peanuts",
}
COLOR_OF = {name: BASIC.index(col) for col, names in COLOR_SETS.items() for name in names.split()}


def main():
    src = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "..", "lofi_assets_full", "lofi_assets_full")
    os.makedirs(OUT, exist_ok=True)
    for f in os.listdir(OUT):
        if f.endswith(".png") or f == "index.json":
            os.remove(os.path.join(OUT, f))

    sheets, items, skipped = [], [], []
    per_sheet = COLS * MAXROWS
    for folder, cat in CATS.items():
        path = os.path.join(src, folder)
        if not os.path.isdir(path):
            print("kategori yok:", folder)
            continue
        names = sorted(n[:-4] for n in os.listdir(path) if n.lower().endswith(".png"))
        keep = [n for n in names if n not in SKIP]
        skipped += [n for n in names if n in SKIP]
        for s0 in range(0, len(keep), per_sheet):
            chunk = keep[s0:s0 + per_sheet]
            rows = (len(chunk) + COLS - 1) // COLS
            sheet = Image.new("RGBA", (COLS * CELL, rows * CELL), (0, 0, 0, 0))
            sheet_index = len(sheets)
            for i, name in enumerate(chunk):
                im = Image.open(os.path.join(path, name + ".png")).convert("RGBA")
                bbox = im.getbbox() or (0, 0, im.width, im.height)
                im = im.crop(bbox)   # kenardaki boşluğu at: hücreyi resim doldursun
                scale = ICON / max(im.width, im.height)
                im = im.resize((max(1, round(im.width * scale)), max(1, round(im.height * scale))), Image.LANCZOS)
                cx = (i % COLS) * CELL + (CELL - im.width) // 2
                cy = (i // COLS) * CELL + (CELL - im.height) // 2
                sheet.alpha_composite(im, (cx, cy))
                items.append({
                    "id": name, "cat": cat, "s": sheet_index, "i": i,
                    "c": COLOR_OF.get(name, -1) if cat != "face" else -1,
                    "w": round(im.width / ICON, 3), "h": round(im.height / ICON, 3),
                })
            fname = f"{cat}_{s0 // per_sheet}"
            sheet.save(os.path.join(OUT, fname + ".png"), optimize=True)
            sheets.append({"file": fname, "cat": cat, "cols": COLS, "rows": rows})
            print(f"{fname}: {len(chunk)} emoji, {sheet.width}x{sheet.height}")

    with open(os.path.join(OUT, "index.json"), "w", encoding="utf-8") as f:
        json.dump({"cell": CELL, "icon": ICON, "sheets": sheets, "items": items}, f, ensure_ascii=False, separators=(",", ":"))
    colored = sum(1 for it in items if it["c"] >= 0)
    print(f"toplam {len(items)} emoji, {len(sheets)} sayfa, renk etiketli {colored}; alınmayan {len(skipped)}")


if __name__ == "__main__":
    main()
