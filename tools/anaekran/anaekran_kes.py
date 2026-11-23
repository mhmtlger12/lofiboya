"""
Ana ekran: Google Stitch tasarımından oyunda kullanılacak parçaları keser.

  tasarim_parcalar.webp  (1024x1536, saydam zemin)  → parçalar
  tasarim_sahne.webp     (841x1870, arka planlı)     → yalnızca arka plan (gök, tepeler, çimen)

Çıktı: LofiBoya/Assets/Resources/MainMenu
  header.png   kalem + gökkuşağı + tahta tabela ("Lofi Boya" kalır, alt levhadaki yazı silinir)
  music.png, gear.png   üst köşe düğmeleri
  easel.png    şövale + bulut + boya kutuları + pastel boyalar (kâğıttaki kedi ve levha yazısı silinir:
               oyun bugünün resmini ve kendi yazısını koyar)
  bubble.png   konuşma balonu (yazısı silinir)
  start.png    "Boyamaya Başla" butonu (yazı silinir, fırça kalır)
  card_read/learn/album/stickers.png   alt kartlar (yazılar ve rozet sayısı silinir)
  bg.png       arka plan: sahne resminden ön plandaki her şey silinip çevresinden doldurulur
  layout.json  parçaların ve yazı alanlarının tasarımdaki yerleri (tasarim_parcalar koordinatı)

Çalıştırma:  python tools/anaekran/anaekran_kes.py
"""
import json
import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage as nd

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "..", "LofiBoya", "Assets", "Resources", "MainMenu")
DEBUG = "--debug" in sys.argv
os.makedirs(OUT, exist_ok=True)

P = np.asarray(Image.open(os.path.join(HERE, "tasarim_parcalar.webp")).convert("RGBA")).astype(np.float32)
RGB, A = P[..., :3].copy(), P[..., 3].copy()
H, W = A.shape


def dilate(m, r):
    return nd.binary_dilation(m, iterations=r) if r > 0 else m


def inpaint(img, mask, iters=None):
    """Maskelenen pikselleri çevresinden yayarak doldurur (Laplace), yalnızca maskenin çevresindeki pencerede."""
    ys, xs = np.nonzero(mask)
    if len(ys) == 0:
        return
    y0, y1 = max(0, ys.min() - 3), min(img.shape[0], ys.max() + 4)
    x0, x1 = max(0, xs.min() - 3), min(img.shape[1], xs.max() + 4)
    win = img[y0:y1, x0:x1]
    m = mask[y0:y1, x0:x1]
    ring = dilate(m, 2) & ~m
    win[m] = win[ring].mean(0) if ring.any() else 0
    # kaba başlangıç: küçültülmüş ölçekte doldurup büyüt (büyük boşluklar hızlı dolsun)
    if max(y1 - y0, x1 - x0) > 120:
        k = 4
        small = win[::k, ::k].copy()
        sm = m[::k, ::k]
        for _ in range(int(max(small.shape[:2]) * 3)):
            p = np.pad(small, ((1, 1), (1, 1), (0, 0)), mode="edge")
            small[sm] = ((p[:-2, 1:-1] + p[2:, 1:-1] + p[1:-1, :-2] + p[1:-1, 2:]) * 0.25)[sm]
        up = np.repeat(np.repeat(small, k, 0), k, 1)[:win.shape[0], :win.shape[1]]
        win[m] = up[m]
    n = iters or int(min(max(y1 - y0, x1 - x0) * 3, 600))
    for _ in range(n):
        p = np.pad(win, ((1, 1), (1, 1), (0, 0)), mode="edge")
        avg = (p[:-2, 1:-1] + p[2:, 1:-1] + p[1:-1, :-2] + p[1:-1, 2:]) * 0.25
        win[m] = avg[m]


def rowfill(img, bb, pad=6):
    """
    Yazının dikdörtgeni satır satır iki yanındaki piksellerin arasında geçişle dolar: düz zeminde (levha, buton,
    kart) yazının kenar yumuşaması ve gölgesi de iz bırakmadan gider; zeminin dikey renk geçişi korunur.
    """
    x0, y0, x1, y1 = bb[0] - pad, bb[1] - pad // 2, bb[2] + pad, bb[3] + pad // 2
    t = np.linspace(0, 1, x1 - x0)[:, None]
    for y in range(y0, y1):
        left = img[y, x0 - 3:x0].mean(0)
        right = img[y, x1:x1 + 3].mean(0)
        img[y, x0:x1] = left * (1 - t) + right * t


def box(x0, y0, x1, y1, shape=(H, W)):
    m = np.zeros(shape, bool)
    m[y0:y1, x0:x1] = True
    return m


def bbox(m):
    ys, xs = np.nonzero(m)
    return [int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1]


def lum(rgb):
    return rgb[..., 0] * 0.3 + rgb[..., 1] * 0.59 + rgb[..., 2] * 0.11


def ink(rgb, dark=170):
    """Tasarımdaki mor yazı (tahtanın kahverengi çizgileri ve gri kenarlar değil)."""
    return (lum(rgb) < dark) & (rgb[..., 2] > rgb[..., 0] + 15) & (rgb[..., 2] > rgb[..., 1] + 30)


def save(name, mask, rgb=None, alpha=None):
    rgb = RGB if rgb is None else rgb
    alpha = A if alpha is None else alpha
    x0, y0, x1, y1 = bbox(mask)
    a = np.where(mask, alpha, 0)[y0:y1, x0:x1]
    c = rgb[y0:y1, x0:x1]
    # saydam piksellerin rengi komşudan: sıkıştırma ve filtrelemede kenarda koyu hale çıkmasın
    solid = a > 0
    if (~solid).any() and solid.any():
        idx = nd.distance_transform_edt(~solid, return_distances=False, return_indices=True)
        c = c[idx[0], idx[1]]
    out = np.dstack([np.clip(c, 0, 255), np.clip(a, 0, 255)]).astype(np.uint8)
    Image.fromarray(out, "RGBA").save(os.path.join(OUT, name + ".png"), optimize=True)
    return [x0, y0, x1, y1]


# ---------------- parçalar ----------------
lab, n = nd.label(A > 40)
sizes = nd.sum(A > 40, lab, range(1, n + 1))
objs = nd.find_objects(lab)


def comp_at(x, y):
    """(x, y) noktasındaki parça."""
    return int(lab[y, x])


def grown(m):
    """Yumuşak kenar pikselleri de parçaya katılsın."""
    return dilate(m, 3) & (A > 0)


comps = {i + 1: (s, int(sizes[i])) for i, s in enumerate(objs) if sizes[i] > 300}
small = [i + 1 for i, s in enumerate(objs) if 0 < sizes[i] <= 300]

header_ids = [i for i, (s, z) in comps.items() if s[0].stop < 400 and z > 400 and not (s[1].stop < 230 or s[1].start > 800)]
music_id = comp_at(160, 106)
gear_id = comp_at(868, 106)
big_id = comp_at(520, 700)        # şövale grubu + buton
bubble_id = comp_at(270, 470)
card_ids = {"read": comp_at(320, 1180), "learn": comp_at(700, 1180), "album": comp_at(320, 1390), "stickers": comp_at(700, 1390)}
sparkle_ids = [i for i, (s, z) in comps.items() if 380 < s[0].start < 600 and s[1].stop < 230 and i != bubble_id]

layout = {"src": [W, H]}

# üst tabela
hm = grown(np.isin(lab, header_ids))
sub_win = box(300, 318, 730, 380) & hm
sub = dilate(ink(RGB) & sub_win, 2) & sub_win
layout["sub"] = bbox(sub)
rowfill(RGB, layout["sub"])
layout["header"] = save("header", hm)
layout["music"] = save("music", grown(lab == music_id))
layout["gear"] = save("gear", grown(lab == gear_id))

# şövale grubu ve buton: butonun üst kenarı, şövalenin ayakları arasındaki boş sütunlardan
bigm = lab == big_id
cols = bigm[:, 480:540]
ys = np.nonzero(cols[870:].any(1))[0]
btn_top = 870 + int(ys.min())
layout["btn_top"] = btn_top
r, g, b = RGB[..., 0], RGB[..., 1], RGB[..., 2]
# yeşil pastel boya: butonun sağ üstüne biniyor, şövale grubuna ait
crayon_win = box(740, 860, W, btn_top + 40)
crayon = (g > r + 10) & bigm & crayon_win
crayon = nd.binary_closing(crayon, iterations=3)
crayon = dilate(crayon, 2) & (A > 0) & crayon_win & ~((r > g + 40) & (r > 200))
lc, nc = nd.label(crayon)
if nc > 1:
    keep = np.argmax(nd.sum(crayon, lc, range(1, nc + 1))) + 1
    crayon = lc == keep
easel = grown(bigm & box(0, 0, W, btn_top)) | crayon
for i in sparkle_ids:
    easel |= grown(lab == i)
btn = grown(bigm & box(0, btn_top, W, H)) & ~crayon & box(0, btn_top - 4, W, H)
# boyanın altında kalan buton kenarı: buton yatay simetrik, karşı taraftan aynalanır
bx0, _, bx1, _ = bbox(bigm & box(0, btn_top + 20, W, H))
cx2 = bx0 + bx1 - 1
hole = crayon & box(0, btn_top - 4, W, btn_top + 60)
yy, xx = np.nonzero(hole)
mx = np.clip(cx2 - xx, 0, W - 1)
A_btn = A.copy()
RGB_btn = RGB.copy()
A_btn[yy, xx] = A[yy, mx] * (lab[yy, mx] == big_id).astype(np.float32)
RGB_btn[yy, xx] = RGB[yy, mx]
btn |= hole & (A_btn > 0)

# kâğıttaki kedi: kâğıdın içi düz kâğıt rengine (oyun bugünün resmini koyar)
paper_win = box(400, 415, 800, 800) & easel
paper_col = np.median(RGB[paper_win & (lum(RGB) > 235)], axis=0)
near = (np.abs(RGB - paper_col).max(-1) < 14)
lp, _ = nd.label(near & paper_win)
pid = np.bincount(lp[lp > 0]).argmax()
paper = nd.binary_fill_holes(lp == pid)
layout["paper"] = bbox(paper)
layout["paper_col"] = [int(round(v)) for v in paper_col]   # bugünün resminin zemini bununla boyanır
draw = paper & ~(lp == pid)
inpaint(RGB, dilate(draw, 2) & paper)

# levhadaki "Bugünün resmi" (kalpler kalır)
pl_win = box(476, 785, 672, 840) & easel
pl = dilate(ink(RGB) & pl_win, 2) & pl_win
layout["plaque"] = bbox(pl)
x0, y0, x1, y1 = layout["plaque"]
inpaint(RGB, box(x0 - 3, y0 - 3, x1 + 3, y1 + 3), iters=400)   # iki yandaki kalpler kalır

# balon yazısı
bm = grown(lab == bubble_id)
bx = bbox(lab == bubble_id)
b_win = box(bx[0] + 28, bx[1] + 18, bx[2] - 22, bx[3] - 26) & bm
bt = dilate(ink(RGB, 190) & b_win, 2) & b_win
layout["bubble_text"] = bbox(bt)
inpaint(RGB, dilate(bt, 3) & b_win)
layout["bubble"] = save("bubble", bm)
layout["easel"] = save("easel", easel)

# buton yazısı (beyaz harfler; fırça ve parlak şerit kalır)
t_win = box(360, btn_top + 30, 820, btn_top + 115)
bt2 = dilate((RGB.min(-1) > 205) & t_win & btn, 2) & t_win
layout["start_text"] = bbox(bt2)
rowfill(RGB_btn, layout["start_text"], 8)
layout["start"] = save("start", btn, RGB_btn, A_btn)

# kartlar: alttaki yazı, Çıkartmalar'da rozet sayısı
layout["cards"] = {}
for name, cid in card_ids.items():
    cm = grown(lab == cid)
    c = bbox(lab == cid)
    h = c[3] - c[1]
    lw = box(c[0] + 30, c[1] + int(h * 0.665), c[2] - 30, c[3] - 12) & cm   # simgenin altından başlar
    lt = dilate(ink(RGB, 115) & lw, 2) & lw
    entry = {"text": bbox(lt)}
    rowfill(RGB, entry["text"])
    if name == "stickers":
        red = (r > 200) & (g < 120) & (b < 140) & cm & box(c[2] - 120, c[1] - 10, c[2] + 5, c[1] + 70)
        rb = bbox(red)
        digits = (RGB.min(-1) > 215) & box(rb[0] + 12, rb[1] + 10, rb[2] - 12, rb[3] - 10)
        rowfill(RGB, bbox(digits), 3)
        entry["badge"] = rb
    entry["box"] = save("card_" + name, cm)
    layout["cards"][name] = entry

# ---------------- arka plan ----------------
S = np.asarray(Image.open(os.path.join(HERE, "tasarim_sahne.webp")).convert("RGB")).astype(np.float32)
SH, SW = S.shape[:2]
fg = np.zeros((SH, SW), bool)
# sahnedeki ön plan (tasarim_sahne koordinatı, gözle): tabela + kalem + gökkuşağı, düğmeler, şövale grubu,
# buton, kartlar. Kenarlardaki bulutlar, çalılar ve çimen kalır.
for x0, y0, x1, y1 in [
    (70, 0, 792, 458), (5, 5, 150, 150), (692, 5, 838, 150),
    (20, 455, 826, 1168), (50, 1150, 840, 1348), (8, 1340, 838, 1812),
    # tabelanın arkasından taşan yarım bulutlar (tam olanlar kalır)
    (0, 150, 110, 290), (0, 405, 160, 505), (750, 150, 841, 230), (690, 400, 841, 505),
]:
    fg[y0:y1, x0:x1] = True
# her satır iki kenardaki görünen şeritlerin renginden (ortanca, dikeyde yumuşatılmış) yatay geçişle dolar:
# gök yukarıda lavanta-pembe, aşağıda çimen yeşili kalır; ön planın kenarı yumuşak geçişle birleşir
LS, RS = S[:, 2:26], S[:, SW - 24:SW - 2]
lc = nd.gaussian_filter1d(np.median(LS, axis=1), 14, axis=0)
rc = nd.gaussian_filter1d(np.median(RS, axis=1), 14, axis=0)
# ufuk: kenarlardaki çalılar ortaya yayılmasın; gökten çimene yumuşak geçiş
H0, H1 = 800, 975
k = np.linspace(0, 1, H1 - H0)[:, None]
for c in (lc, rc):
    c[H0:H1] = c[H0] * (1 - k) + c[H1] * k
t = np.linspace(0, 1, SW)[None, :, None]
fill = lc[:, None, :] * (1 - t) + rc[:, None, :] * t
d = nd.distance_transform_edt(fg)
w = np.clip(d / 24.0, 0, 1)[..., None]
S = S * (1 - w) + fill * w
Image.fromarray(np.clip(S, 0, 255).astype(np.uint8)).save(os.path.join(OUT, "bg.png"), optimize=True)
layout["bg"] = [SW, SH]

with open(os.path.join(OUT, "layout.json"), "w", encoding="utf-8") as f:
    json.dump(layout, f, indent=1)
print(json.dumps(layout))

if DEBUG:
    dbg = Image.new("RGBA", (W, H), (60, 60, 80, 255))
    for nm in ["header", "music", "gear", "easel", "bubble", "start", "card_read", "card_learn", "card_album", "card_stickers"]:
        im = Image.open(os.path.join(OUT, nm + ".png"))
        key = nm.replace("card_", "")
        bb = layout["cards"][key]["box"] if nm.startswith("card_") else layout[nm]
        dbg.alpha_composite(im, (bb[0], bb[1]))
    dbg.save(os.path.join(HERE, "_debug.png"))
