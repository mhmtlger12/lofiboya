"""Play Store görselleri: uygulama ikonu (512x512) ve öne çıkan görsel (1024x500).

Oyunun kendi çizimlerini kullanır (Pofuduk, boya kalemleri, yıldız, kalp). Çizimler Unity'den
dışa aktarılan PNG'lerdir: python tools/magaza_gorselleri.py <art_klasoru>
Çıktı: magaza/ klasörü.
"""
import os
import sys
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageChops

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "magaza", "art")
OUT = os.path.join(ROOT, "magaza")
FONT = os.path.join(ROOT, "LofiBoya", "Assets", "Resources", "Fonts", "Main.ttf")

TEXT = (0x5E, 0x4B, 0x6E)
SOFT = (0xA2, 0x91, 0xAE)
ACCENT = (0xFF, 0x9E, 0xAA)
CRAYONS = [(0xFF, 0x5A, 0x5F), (0xFF, 0xD9, 0x3D), (0x6B, 0xCB, 0x77), (0x4D, 0x96, 0xFF), (0x9B, 0x72, 0xF2), (0xFF, 0x8F, 0xC7)]


def load(name):
    return Image.open(os.path.join(ART, name + ".png")).convert("RGBA")


def gradient(w, h, top, mid, bottom):
    img = Image.new("RGB", (w, h))
    px = img.load()
    for y in range(h):
        t = y / max(1, h - 1)
        if t < 0.5:
            a, b, k = top, mid, t * 2
        else:
            a, b, k = mid, bottom, (t - 0.5) * 2
        c = tuple(int(a[i] + (b[i] - a[i]) * k) for i in range(3))
        for x in range(w):
            px[x, y] = c
    return img.convert("RGBA")


def blob(canvas, center, radius, color, blur):
    # yalnızca alfa bulanıklaşır: kenarlarda gri/siyah leke olmaz
    mask = Image.new("L", canvas.size, 0)
    d = ImageDraw.Draw(mask)
    x, y = center
    d.ellipse([x - radius, y - radius, x + radius, y + radius], fill=color[3])
    mask = mask.filter(ImageFilter.GaussianBlur(blur))
    layer = Image.new("RGBA", canvas.size, color[:3] + (0,))
    layer.putalpha(mask)
    canvas.alpha_composite(layer)


def tint(img, rgb):
    """Gri tonlu sprite'ı renkle çarp (oyundaki Image.color gibi)."""
    r, g, b, a = img.split()
    solid = Image.new("RGB", img.size, rgb)
    mult = ImageChops.multiply(Image.merge("RGB", (r, g, b)), solid)
    return Image.merge("RGBA", (*mult.split(), a))


def paste(canvas, img, center, width, angle=0, shadow=True):
    h = int(img.height * width / img.width)
    im = img.resize((width, h), Image.LANCZOS)
    if angle:
        im = im.rotate(angle, resample=Image.BICUBIC, expand=True)
    x, y = int(center[0] - im.width / 2), int(center[1] - im.height / 2)
    if shadow:
        sh = Image.new("RGBA", im.size, (0x5E, 0x4B, 0x6E, 0))
        sh.putalpha(im.split()[3].point(lambda v: int(v * 0.22)))
        sh = sh.filter(ImageFilter.GaussianBlur(max(4, width // 30)))
        canvas.alpha_composite(sh, (x + width // 40, y + width // 18))
    canvas.alpha_composite(im, (x, y))


def mascot():
    body = load("body")
    eyes = load("eyes_happy")
    m = body.copy()
    m.alpha_composite(eyes)
    return m


def crayon(rgb):
    c = tint(load("crayon"), rgb)
    c.alpha_composite(load("crayon_shine"))
    return c


def icon():
    S = 1024  # büyük çiz, sonra küçült: kenarlar pürüzsüz olsun
    img = gradient(S, S, (0xFF, 0xD9, 0xE0), (0xF6, 0xE3, 0xF6), (0xD9, 0xE6, 0xFB))
    blob(img, (S * 0.25, S * 0.2), S * 0.28, (255, 255, 255, 150), 60)
    blob(img, (S * 0.82, S * 0.85), S * 0.3, (0xC3, 0xB5, 0xF0, 120), 70)
    # arkada yelpaze gibi üç kalem
    for i, col in enumerate([CRAYONS[0], CRAYONS[1], CRAYONS[3]]):
        paste(img, crayon(col), (S * (0.36 + i * 0.14), S * 0.34), int(S * 0.12), angle=(1 - i) * 22)
    paste(img, mascot(), (S * 0.5, S * 0.6), int(S * 0.78))
    star = load("star")
    paste(img, star, (S * 0.84, S * 0.2), int(S * 0.16), angle=-12, shadow=False)
    paste(img, star, (S * 0.16, S * 0.78), int(S * 0.1), angle=15, shadow=False)
    out = img.resize((512, 512), Image.LANCZOS).convert("RGB")
    out.save(os.path.join(OUT, "ikon_512.png"))
    return out


RAINBOW = [(0xFF, 0x9E, 0xAA), (0xFF, 0xC4, 0x8C), (0xFF, 0xE6, 0x8A), (0xB5, 0xE8, 0xA8), (0x9F, 0xD0, 0xF5), (0xC8, 0xB4, 0xF2)]


def icon_bg(S):
    """Arka katman: şeftali-pembe-lila zemin, arkada yumuşak gökkuşağı, ışık lekeleri."""
    bg = gradient(S, S, (0xFF, 0xD3, 0xDC), (0xFF, 0xE4, 0xE0), (0xE4, 0xDB, 0xFA))
    # gökkuşağı yayı (alt kenara oturan yarım daire), kenarları yumuşak
    arc = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(arc)
    cx, cy, r0, band = S * 0.5, S * 0.98, S * 0.62, S * 0.045
    for i, col in enumerate(RAINBOW):
        r = r0 - i * band
        d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=col + (255,))
    rin = r0 - len(RAINBOW) * band
    d.ellipse([cx - rin, cy - rin, cx + rin, cy + rin], fill=(0, 0, 0, 0))
    a = arc.split()[3].point(lambda v: int(v * 0.55)).filter(ImageFilter.GaussianBlur(S * 0.006))
    arc.putalpha(a)
    bg.alpha_composite(arc)
    blob(bg, (S * 0.22, S * 0.2), S * 0.2, (255, 255, 255, 170), S * 0.06)
    blob(bg, (S * 0.82, S * 0.25), S * 0.12, (255, 255, 255, 120), S * 0.05)
    return bg


def icon_fg(S):
    """Ön katman: büyük, gülen Pofuduk elinde pembe kalemle; çevresinde yıldızcıklar. Güvenli alanda (orta %66)."""
    fg = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    paste(fg, crayon((0xFF, 0x8F, 0xB1)), (S * 0.68, S * 0.43), int(S * 0.07), angle=-28)
    paste(fg, mascot(), (S * 0.5, S * 0.54), int(S * 0.52))
    hand = load("hand")
    paste(fg, hand, (S * 0.68, S * 0.53), int(S * 0.09), shadow=False)
    star = load("star")
    paste(fg, star, (S * 0.31, S * 0.29), int(S * 0.1), angle=-14, shadow=False)
    paste(fg, tint(load("star5"), (255, 255, 255)), (S * 0.66, S * 0.25), int(S * 0.05), angle=10, shadow=False)
    paste(fg, tint(load("heart"), (0xFF, 0x7A, 0x9C)), (S * 0.24, S * 0.6), int(S * 0.07), angle=-12, shadow=False)
    return fg


def adaptive():
    """Android 8+ katmanlı ikon: arka plan + ön plan (432x432) ve mağaza ikonu (512)."""
    S = 1296
    bg, fg = icon_bg(S), icon_fg(S)
    bg.resize((432, 432), Image.LANCZOS).convert("RGB").save(os.path.join(OUT, "ikon_arka_432.png"))
    fg.resize((432, 432), Image.LANCZOS).save(os.path.join(OUT, "ikon_on_432.png"))
    # Play Store ikonu: aynı tasarım, 108dp'lik katmanın görünen ortası (72/108) büyütülmüş
    full = bg.copy()
    full.alpha_composite(fg)
    m = int(S * (1 - 72 / 108) / 2 * 0.55)  # biraz daha geniş kadraj: kenarlar nefes alsın
    full = full.crop((m, m, S - m, S - m)).resize((512, 512), Image.LANCZOS).convert("RGB")
    full.save(os.path.join(OUT, "ikon_512.png"))
    # önizleme: telefonların kesim biçimleri (daire, yumuşak kare, damla)
    prev = Image.new("RGB", (3 * 260 + 40, 300), (245, 245, 248))
    vis = bg.copy()
    vis.alpha_composite(fg)
    m2 = int(S * (1 - 72 / 108) / 2)
    vis = vis.crop((m2, m2, S - m2, S - m2)).resize((240, 240), Image.LANCZOS)
    for k, shape in enumerate(["circle", "squircle", "drop"]):
        mask = Image.new("L", (240 * 4, 240 * 4), 0)
        md = ImageDraw.Draw(mask)
        if shape == "circle":
            md.ellipse([0, 0, 959, 959], fill=255)
        elif shape == "squircle":
            md.rounded_rectangle([0, 0, 959, 959], radius=300, fill=255)
        else:
            md.rounded_rectangle([0, 0, 959, 959], radius=480, fill=255)
            md.rectangle([480, 480, 959, 959], fill=255)
        mask = mask.resize((240, 240), Image.LANCZOS)
        prev.paste(vis, (20 + k * 260, 30), mask)
    prev.save(os.path.join(OUT, "ikon_onizleme.png"))


def feature():
    W, H = 2048, 1000
    img = gradient(W, H, (0xFD, 0xE7, 0xE4), (0xF6, 0xEA, 0xF6), (0xE3, 0xEE, 0xFB))
    blob(img, (W * 0.15, H * 0.2), 260, (255, 255, 255, 170), 80)
    blob(img, (W * 0.78, H * 0.5), 420, (0xFF, 0xD6, 0xE0, 150), 110)
    blob(img, (W * 0.95, H * 0.95), 300, (0xC3, 0xB5, 0xF0, 120), 90)

    # sağda: kalem yelpazesi + Pofuduk
    for i, col in enumerate(CRAYONS):
        a = (2.5 - i) * 13
        paste(img, crayon(col), (W * 0.745 + (i - 2.5) * 70, H * 0.42), 120, angle=a)
    paste(img, mascot(), (W * 0.76, H * 0.66), 640)
    paste(img, load("star"), (W * 0.93, H * 0.2), 150, angle=-12, shadow=False)
    paste(img, tint(load("heart"), (0xFF, 0x8F, 0xAB)), (W * 0.58, H * 0.2), 110, angle=12, shadow=False)
    paste(img, tint(load("star5"), (0xFF, 0xD1, 0x66)), (W * 0.6, H * 0.84), 80, angle=-8, shadow=False)

    # solda: başlık
    d = ImageDraw.Draw(img)
    title = ImageFont.truetype(FONT, 250)
    sub = ImageFont.truetype(FONT, 84)
    tx, ty = 130, 250
    d.text((tx, ty + 16), "Lofi Boya", font=title, fill=ACCENT)
    d.text((tx, ty), "Lofi Boya", font=title, fill=TEXT)
    d.text((tx + 10, ty + 330), "Çocuklar için sevimli boyama", font=sub, fill=SOFT)
    # küçük etiketler
    tags = ["sihirli boyalar", "renkleri öğren", "reklamsız"]
    x = tx + 10
    small = ImageFont.truetype(FONT, 56)
    for t in tags:
        w = d.textlength(t, font=small) + 70
        d.rounded_rectangle([x, ty + 480, x + w, ty + 580], radius=50, fill=(255, 255, 255))
        d.text((x + 35, ty + 492), t, font=small, fill=TEXT)
        x += w + 26
    out = img.resize((1024, 500), Image.LANCZOS).convert("RGB")
    out.save(os.path.join(OUT, "one_cikan_gorsel_1024x500.png"))
    return out


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    icon()
    adaptive()
    feature()
    print("tamam:", OUT)
