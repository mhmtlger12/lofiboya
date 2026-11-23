"""
Öğren menüsü: Google Stitch tasarımından (tasarim.webp, 873x1801) oyunda kullanılacak parçaları keser.

  header.png      üst kısım (gök, gökkuşağı, kalem, tahta tabela); geri düğmesi, yıldız sayacı ve yazılar silinir
  footer.png      alt kısım (tepeler, kedi, levha); levhadaki yazı silinir, üst kenar saydamlaşır
  card_0..7.png   tasarımdaki 8 kart (Renkler … Örüntü); bulut üstündeki yazılar silinir, kenar dışı saydam
  card_blank_*.png  tasarımda olmayan oyunlar için: Şekiller kartının içi boşaltılıp başka renge boyanır
  layout.json     yazıların yerleri (oyunda kendi yazılarımız buraya, her dilde yazılır)

Yazı silme: yazı pikselleri renge göre seçilir, çevresinden içe doğru yayılarak (difüzyon) doldurulur.
Çalıştırma:  python tools/ogren/ogren_kes.py
"""
import json
import os
import colorsys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "..", "LofiBoya", "Assets", "Resources", "LearnMenu")

src = np.asarray(Image.open(os.path.join(HERE, "tasarim.webp")).convert("RGB")).astype(np.float32)
H, W, _ = src.shape


def dilate(m, r):
    out = m.copy()
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            if dx * dx + dy * dy > r * r:
                continue
            out |= np.roll(np.roll(m, dy, 0), dx, 1)
    return out


def inpaint(img, mask, iters=None):
    """Maskelenen pikselleri çevresinden yayarak doldurur (Laplace). Yalnızca maskenin çevresindeki pencerede çalışır."""
    ys, xs = np.nonzero(mask)
    if len(ys) == 0:
        return
    y0, y1 = max(0, ys.min() - 3), min(img.shape[0], ys.max() + 4)
    x0, x1 = max(0, xs.min() - 3), min(img.shape[1], xs.max() + 4)
    win = img[y0:y1, x0:x1]
    m = mask[y0:y1, x0:x1]
    ring = dilate(m, 2) & ~m
    win[m] = win[ring].mean(0) if ring.any() else 0
    # kaba-ince: önce küçük ölçekte yay, sonra tam ölçekte rötuş (büyük alanlar hızlı dolsun)
    n = iters or int(max(y1 - y0, x1 - x0) * 3)
    for _ in range(n):
        p = np.pad(win, ((1, 1), (1, 1), (0, 0)), mode="edge")
        avg = (p[:-2, 1:-1] + p[2:, 1:-1] + p[1:-1, :-2] + p[1:-1, 2:]) * 0.25
        win[m] = avg[m]


def box_mask(shape, x0, y0, x1, y1):
    m = np.zeros(shape[:2], bool)
    m[y0:y1, x0:x1] = True
    return m


def dark_text(img, x0, y0, x1, y1, lum=175, grow=4):
    """Krem bulut / levha üstündeki koyu yazı pikselleri."""
    l = img[..., 0] * 0.3 + img[..., 1] * 0.55 + img[..., 2] * 0.15
    m = (l < lum) & box_mask(img.shape, x0, y0, x1, y1)
    return dilate(m, grow) & box_mask(img.shape, x0 - grow, y0 - grow, x1 + grow, y1 + grow)


def bbox(m):
    ys, xs = np.nonzero(m)
    return [int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1]


def save(arr, alpha, name):
    rgba = np.dstack([np.clip(arr, 0, 255), np.clip(alpha * 255, 0, 255)]).astype(np.uint8)
    Image.fromarray(rgba, "RGBA").save(os.path.join(OUT, name + ".png"), optimize=True)


def outside_alpha(img, seed_rows=None):
    """Kartın dışındaki açık lila zemini saydamlaştırır: kenardan başlayıp zemine benzeyen piksellere yayılır."""
    h, w, _ = img.shape
    bg = np.median(np.concatenate([img[0], img[-1], img[:, 0], img[:, -1]]), 0)
    dist = np.sqrt(((img - bg) ** 2).sum(-1))
    mx, mn = img.max(-1), img.min(-1)
    sat = (mx - mn) / np.maximum(mx, 1)
    like = (dist < 70) | ((sat < 0.14) & (mx > 190))   # zemin, açık hale ve kartın yumuşak gölgesi
    reach = np.zeros((h, w), bool)
    if seed_rows is None:
        reach[0], reach[-1], reach[:, 0], reach[:, -1] = like[0], like[-1], like[:, 0], like[:, -1]
    else:
        reach[seed_rows] = like[seed_rows]
    while True:
        grown = dilate(reach, 1) & like
        if grown.sum() == reach.sum():
            break
        reach = grown
    alpha = np.ones((h, w), np.float32)
    # kartın dışı: yalnızca renkli (doygun) pikseller kalır, zemin ve hale tamamen saydam; gölgeyi oyun kendisi çizer
    a = np.clip((sat - 0.12) / 0.2, 0, 1)
    alpha[reach] = a[reach]
    return alpha, bg


def keep_main(alpha):
    """Yalnızca ortadaki kartı bırakır: kenara taşan komşu kart şeritlerini siler."""
    h, w = alpha.shape
    solid = alpha > 0.35
    comp = np.zeros((h, w), bool)
    comp[h // 2, w // 2] = True
    while True:
        grown = dilate(comp, 1) & solid
        if grown.sum() == comp.sum():
            break
        comp = grown
    near = dilate(comp, 6)   # yumuşak kenar ve gölge payı
    alpha[~near] = 0
    return alpha


os.makedirs(OUT, exist_ok=True)
layout = {"src": [W, H], "cards": []}

# ---------------- Kartlar ----------------
# (kutu x0,y0,x1,y1) ve bulut üstündeki yazı alanı (başlık + alt yazı); ok düğmesi dışarıda kalır
CARDS = [
    ((14, 360, 445, 676), (130, 552, 345, 640)),    # Renkler
    ((446, 360, 862, 676), (535, 552, 750, 640)),   # Resimler
    ((14, 676, 445, 970), (130, 840, 320, 935)),    # Şekiller
    ((446, 676, 862, 970), (555, 840, 750, 935)),   # Sayılar
    ((14, 970, 445, 1266), (135, 1138, 335, 1230)), # Yazalım
    ((446, 970, 862, 1266), (555, 1138, 745, 1230)),# Harfler
    ((14, 1266, 445, 1540), (105, 1432, 360, 1508)),# Duygular
    ((446, 1266, 862, 1540), (560, 1432, 745, 1508)),# Örüntü
]

for i, ((x0, y0, x1, y1), (tx0, ty0, tx1, ty1)) in enumerate(CARDS):
    img = src.copy()
    tm = dark_text(img, tx0, ty0, tx1, ty1)
    tb = bbox(tm)
    inpaint(img, tm)
    crop = img[y0:y1, x0:x1].copy()
    alpha, bg = outside_alpha(crop)
    alpha = keep_main(alpha)
    # kartın görünen sınırı (gölge hariç)
    solid = alpha > 0.9
    cb = bbox(solid)
    cb = [cb[0] + x0, cb[1] + y0, cb[2] + x0, cb[3] + y0]
    save(crop, alpha, f"card_{i}")
    layout["cards"].append({"file": f"card_{i}", "box": [x0, y0, x1, y1], "card": cb, "text": tb})
    if i == 0:
        layout["bg"] = [round(float(c) / 255, 4) for c in bg]
    print("kart", i, (x1 - x0, y1 - y0), "yazı", tb)

# ---------------- Boş kart (tasarımda olmayan oyunlar için) ----------------
x0, y0, x1, y1 = CARDS[2][0]
img = src.copy()
tm = dark_text(img, *CARDS[2][1])
inpaint(img, tm)
# Şekiller'in süsleri (bulutun üstü) silinir. Kart rengi yukarıdan aşağı değişir, yana doğru neredeyse sabittir:
# her satır o satırdaki kart morunun ortancasıyla dolar; kutunun dört kenarında yumuşak geçiş (iz kalmasın).
AX0, AX1, AY0, AY1 = 40, 414, 690, 856
fill = np.zeros((AY1 - AY0, 3), np.float32)
for j, y in enumerate(range(AY0, AY1)):
    row = img[y, 30:425]
    hs = np.array([colorsys.rgb_to_hsv(*(c / 255)) for c in row])
    ok = (np.abs(hs[:, 0] - 0.75) < 0.07) & (hs[:, 1] > 0.3) & (hs[:, 2] > 0.6)
    fill[j] = np.median(row[ok], 0) if ok.sum() > 8 else (fill[j - 1] if j else row.mean(0))
fill = np.array([fill[max(0, j - 4):j + 5].mean(0) for j in range(len(fill))])   # satırlar arası yumuşat
yy, xx = np.mgrid[0:AY1 - AY0, 0:AX1 - AX0]
edge = np.minimum.reduce([xx, (AX1 - AX0 - 1) - xx, yy]).astype(np.float32)   # altta bulut var: orada geçiş yok
k = np.clip(edge / 16.0, 0, 1)
win = img[AY0:AY1, AX0:AX1] / 255.0
hs = np.array([colorsys.rgb_to_hsv(*c) for c in win.reshape(-1, 3)]).reshape(win.shape)
cloud = dilate((hs[..., 1] < 0.16) & (hs[..., 2] > 0.85) & (yy > 120), 1)   # bulutun kendisine dokunma
k[cloud] = 0
k = k[..., None]
img[AY0:AY1, AX0:AX1] = img[AY0:AY1, AX0:AX1] * (1 - k) + fill[:, None, :] * k
crop = img[y0:y1, x0:x1].copy()
alpha, _ = outside_alpha(crop)
alpha = keep_main(alpha)

# mor kartı istenen renge çevir: yalnızca morumsu ve doygun pikseller (bulut, yıldızlar değişmez)
rgb = crop / 255.0
hsv = np.array([colorsys.rgb_to_hsv(*p) for p in rgb.reshape(-1, 3)]).reshape(crop.shape)
base_h = 0.75   # mor
dh = np.abs(((hsv[..., 0] - base_h + 0.5) % 1.0) - 0.5)
sel = (dh < 0.12) & (hsv[..., 1] > 0.05)   # bulutun hafif mor tonu da kart rengine döner
BLANKS = {"orange": 0.07, "coral": 0.02, "mint": 0.40, "periwinkle": 0.66, "aqua": 0.49, "yellow": 0.13, "sky": 0.57}
for name, h in BLANKS.items():
    out = hsv.copy()
    out[..., 0] = np.where(sel, (hsv[..., 0] - base_h + h) % 1.0, hsv[..., 0])
    if name in ("yellow", "orange"):
        out[..., 2] = np.where(sel, np.minimum(1, hsv[..., 2] * 1.08), hsv[..., 2])
    rgb2 = np.array([colorsys.hsv_to_rgb(*p) for p in out.reshape(-1, 3)]).reshape(crop.shape) * 255
    save(rgb2, alpha, f"card_blank_{name}")
layout["blank"] = {"box": [x0, y0, x1, y1], "card": layout["cards"][2]["card"], "text": layout["cards"][2]["text"],
                   "art": [70, 688, 400, 842], "colors": list(BLANKS.keys())}
print("boş kartlar", list(BLANKS))

# ---------------- Üst kısım ----------------
HY = 362
img = src[:HY].copy()
# geri düğmesi ve yıldız sayacı (oyun kendi düğmelerini koyar)
inpaint(img, box_mask(img.shape, 30, 28, 142, 138), iters=500)
inpaint(img, box_mask(img.shape, 655, 26, 852, 132), iters=500)
# "Öğren": mor harfler + beyaz kontur (tahtanın kahverengisi kalır)
r, g, b = img[..., 0], img[..., 1], img[..., 2]
title_box = box_mask(img.shape, 286, 166, 594, 286)
purple = (g < 120) & (b > 110) & (b > r * 0.9)
white = (r > 225) & (g > 225) & (b > 225)
wood = (r > g) & (g > b) & (r - b > 45)
on_board = box_mask(img.shape, 286, 192, 594, 286)   # tahtanın üstü: tahta rengi olmayan her şey (harf gölgesi dahil)
tmask = dilate(((purple | white) & title_box) | (~wood & on_board), 2) & title_box
title_bb = bbox(tmask)
# tahtanın üstü: harflerin olduğu dikdörtgenin her satırı iki yandaki tahta pikselleri arasında geçişle dolar,
# böylece damarlar yatay devam eder ve harf izi kalmaz (oyunun kendi başlığı buraya yazılır)
TX0, TX1 = 288, 592
for y in range(192, 285):
    t = np.linspace(0, 1, TX1 - TX0 + 1)[:, None]
    img[y, TX0:TX1 + 1] = img[y, TX0 - 1] * (1 - t) + img[y, TX1 + 1] * t
tmask[192:285] = False
inpaint(img, tmask, iters=400)   # tahtanın üstüne taşan noktalar (gök)
sub = dark_text(img, 250, 288, 625, 338, lum=130, grow=2)
sub_bb = bbox(sub)
inpaint(img, sub, iters=300)
alpha = np.ones(img.shape[:2], np.float32)
fade = 26
alpha[HY - fade:] = np.linspace(1, 0, fade)[:, None]
save(img, alpha, "header")
layout["header"] = {"size": [W, HY], "title": title_bb, "sub": sub_bb, "back": [30, 28, 142, 138], "stars": [655, 26, 852, 132]}
print("üst", title_bb, sub_bb)

# ---------------- Alt kısım ----------------
FY = 1532
img = src[FY:].copy()
ft = dark_text(img, 280, 1705 - FY, 592, 1772 - FY, lum=150, grow=3)
foot_bb = bbox(ft)
inpaint(img, ft, iters=400)
alpha = np.ones(img.shape[:2], np.float32)
alpha[:fade] = np.linspace(0, 1, fade)[:, None]   # üst kenar zemine karışsın
save(img, alpha, "footer")
layout["footer"] = {"size": [W, H - FY], "text": foot_bb}
print("alt", foot_bb)

with open(os.path.join(OUT, "layout.json"), "w", encoding="utf-8") as f:
    json.dump(layout, f, indent=1)
print("bitti ->", OUT)
