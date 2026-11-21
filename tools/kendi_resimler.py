"""
Kendi çizdiğimiz (özgün) boyama sayfaları.
Her resim kalın, kapalı çizgilerle çizilir (boya taşmasın), resimler/<zorluk>/ içine PNG kaydedilir.
Sonra:  python tools/donustur.py   ile oyuna eklenir.

Kullanım:  python tools/kendi_resimler.py
Dosya adı "lofi-<ingilizce-ad>-NNN.png": oyun adı bu kelimeden bulur (Names.cs: apple, cat, owl...).
"""
import math
import os

from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "resimler")

S = 2                 # süper örnekleme (yumuşak kenar)
W, H = 1400, 1600     # son boyut (dikey)
LW = 14               # çizgi kalınlığı (son boyutta)


class Pen:
    def __init__(self):
        self.im = Image.new("L", (W * S, H * S), 255)
        self.d = ImageDraw.Draw(self.im)

    def _b(self, box):
        return [v * S for v in box]

    def ellipse(self, cx, cy, rx, ry, fill=None, w=LW):
        self.d.ellipse(self._b((cx - rx, cy - ry, cx + rx, cy + ry)), outline=0, fill=fill, width=w * S)

    def circle(self, cx, cy, r, fill=None, w=LW):
        self.ellipse(cx, cy, r, r, fill, w)

    def poly(self, pts, fill=None, w=LW):
        p = [(x * S, y * S) for x, y in pts]
        if fill is not None:
            self.d.polygon(p, fill=fill)
        self.d.line(p + [p[0]], fill=0, width=w * S, joint="curve")
        for x, y in p:  # yuvarlak köşeler
            r = w * S / 2
            self.d.ellipse((x - r, y - r, x + r, y + r), fill=0)

    def line(self, pts, w=LW):
        p = [(x * S, y * S) for x, y in pts]
        self.d.line(p, fill=0, width=w * S, joint="curve")
        r = w * S / 2
        for x, y in (p[0], p[-1]):
            self.d.ellipse((x - r, y - r, x + r, y + r), fill=0)

    def arc(self, cx, cy, rx, ry, a0, a1, w=LW):
        self.d.arc(self._b((cx - rx, cy - ry, cx + rx, cy + ry)), a0, a1, fill=0, width=w * S)

    def dot(self, cx, cy, r):
        self.d.ellipse(self._b((cx - r, cy - r, cx + r, cy + r)), fill=0)

    def save(self, group, name):
        os.makedirs(os.path.join(OUT, group), exist_ok=True)
        im = self.im.resize((W, H), Image.LANCZOS)
        path = os.path.join(OUT, group, name + ".png")
        im.save(path)
        print("kaydedildi:", path)


def curve(points, n=24):
    """Catmull-Rom: verilen noktalardan geçen yumuşak kapalı eğri."""
    out = []
    k = len(points)
    for i in range(k):
        p0, p1, p2, p3 = points[(i - 1) % k], points[i], points[(i + 1) % k], points[(i + 2) % k]
        for j in range(n):
            t = j / n
            t2, t3 = t * t, t * t * t
            x = 0.5 * ((2 * p1[0]) + (-p0[0] + p2[0]) * t + (2 * p0[0] - 5 * p1[0] + 4 * p2[0] - p3[0]) * t2 + (-p0[0] + 3 * p1[0] - 3 * p2[0] + p3[0]) * t3)
            y = 0.5 * ((2 * p1[1]) + (-p0[1] + p2[1]) * t + (2 * p0[1] - 5 * p1[1] + 4 * p2[1] - p3[1]) * t2 + (-p0[1] + 3 * p1[1] - 3 * p2[1] + p3[1]) * t3)
            out.append((x, y))
    return out


def face(p, cx, cy, s=1.0, blush=True):
    """Sevimli yüz: iki göz (parlak noktalı), gülümseme, yanaklar."""
    for dx in (-60, 60):
        p.dot(cx + dx * s, cy, 20 * s)
        p.d.ellipse([(cx + dx * s + 5 * s) * S, (cy - 11 * s) * S, (cx + dx * s + 13 * s) * S, (cy - 3 * s) * S], fill=255)
    p.arc(cx, cy + 22 * s, 30 * s, 22 * s, 20, 160, w=max(8, int(LW * 0.8 * s)))
    if blush:
        for dx in (-105, 105):
            p.ellipse(cx + dx * s, cy + 40 * s, 30 * s, 18 * s, w=max(7, int(9 * s)))


# ---------------------------------------------------------------- resimler
# Not: oyunda zaten olan konular (elma, çilek, kedi, kardan adam, baykuş, kelebek...) tekrar çizilmez.

def star(p, x, y, r, w=10):
    pts = []
    for i in range(10):
        a = -math.pi / 2 + i * math.pi / 5
        rr = r if i % 2 == 0 else r * 0.45
        pts.append((x + math.cos(a) * rr, y + math.sin(a) * rr))
    p.poly(pts, w=w)


def rocket():
    """Roket: uzay merakı; pencere = daire, kanatlar = üçgen."""
    p = Pen()
    cx = 700
    for x, y, r in [(250, 300, 60), (1150, 520, 50), (1130, 1100, 45), (250, 1000, 40)]:
        star(p, x, y, r)
    p.circle(1130, 200, 90)                       # gezegen
    p.ellipse(1130, 200, 150, 35, w=10)
    # alev
    p.poly(curve([(cx - 110, 1230), (cx - 70, 1420), (cx, 1540), (cx + 70, 1420), (cx + 110, 1230)], 12))
    p.poly(curve([(cx - 55, 1240), (cx - 30, 1360), (cx, 1440), (cx + 30, 1360), (cx + 55, 1240)], 10), w=10)
    # kanatlar
    p.poly([(cx - 170, 950), (cx - 340, 1250), (cx - 170, 1200)])
    p.poly([(cx + 170, 950), (cx + 340, 1250), (cx + 170, 1200)])
    # gövde
    body = curve([(cx, 180), (cx + 150, 400), (cx + 190, 750), (cx + 170, 1220), (cx - 170, 1220), (cx - 190, 750), (cx - 150, 400)], 18)
    p.poly(body, fill=255)
    p.line([(cx - 160, 430), (cx + 160, 430)], w=12)
    p.line([(cx - 182, 1060), (cx + 182, 1060)], w=12)
    p.poly([(cx - 20, 1060), (cx + 20, 1060), (cx + 20, 1220), (cx - 20, 1220)], w=10)
    # pencere ve içinde gülen yüz
    p.circle(cx, 700, 135, fill=255)
    p.circle(cx, 700, 100)
    face(p, cx, 690, 0.6, blush=False)
    p.save("kolay", "lofi-rocket-001")


def house():
    """Ev: kare, üçgen, daire, dikdörtgen -> şekilleri tanıma."""
    p = Pen()
    p.circle(1170, 230, 95)                       # güneş
    for i in range(8):
        a = i * math.pi / 4
        p.line([(1170 + math.cos(a) * 130, 230 + math.sin(a) * 130), (1170 + math.cos(a) * 185, 230 + math.sin(a) * 185)], w=12)
    p.poly(curve([(220, 330), (300, 240), (420, 260), (480, 340), (420, 400), (240, 400)], 10))   # bulut
    p.poly(curve([(150, 1380), (400, 1320), (800, 1360), (1250, 1320), (1250, 1520), (150, 1520)], 10))  # zemin
    p.poly([(850, 330), (950, 330), (950, 560), (850, 560)])                    # baca
    p.poly([(700, 300), (1160, 700), (240, 700)], fill=255)                     # çatı (üçgen)
    p.circle(700, 560, 70)                                                      # yuvarlak pencere
    p.poly([(320, 700), (1080, 700), (1080, 1360), (320, 1360)], fill=255)      # duvar (kare)
    p.poly([(600, 1360), (600, 1060), (800, 1060), (800, 1360)])                # kapı
    p.arc(700, 1060, 100, 100, 180, 360)
    p.dot(760, 1220, 16)
    for x in (450, 950):                                                        # 4 bölmeli pencereler
        p.poly([(x - 90, 820), (x + 90, 820), (x + 90, 1000), (x - 90, 1000)])
        p.line([(x, 820), (x, 1000)], w=10)
        p.line([(x - 90, 910), (x + 90, 910)], w=10)
    for x in (170, 1230):                                                       # çiçekler
        p.line([(x, 1330), (x, 1210)], w=10)
        for i in range(5):
            a = i * 2 * math.pi / 5 - math.pi / 2
            p.circle(x + math.cos(a) * 40, 1160 + math.sin(a) * 40, 28, w=9)
        p.circle(x, 1160, 20, w=9)
    p.save("kolay", "lofi-house-001")


def fish():
    """Balık: pullar, kabarcıklar, deniz yosunu."""
    p = Pen()
    for x in (200, 1180):                                                       # yosunlar
        p.poly(curve([(x - 40, 1460), (x - 70, 1300), (x - 10, 1120), (x + 30, 1180), (x - 10, 1320), (x + 40, 1460)], 10))
    p.poly(curve([(150, 1450), (700, 1400), (1250, 1450), (1250, 1560), (150, 1560)], 8))   # kum
    p.poly(curve([(1000, 760), (1250, 520), (1300, 760), (1250, 1000)], 12))    # kuyruk
    p.poly(curve([(560, 540), (720, 380), (880, 420), (900, 560)], 10))         # üst yüzgeç
    p.poly(curve([(620, 1000), (700, 1150), (820, 1110), (800, 990)], 10))      # alt yüzgeç
    p.poly(curve([(220, 760), (380, 540), (700, 470), (980, 600), (1060, 760), (980, 920), (700, 1050), (380, 980)], 18), fill=255)
    p.arc(470, 760, 110, 210, 300, 60, w=12)                                    # solungaç
    for row, y in enumerate((640, 760, 880)):                                   # pullar
        for i in range(3 if row != 1 else 4):
            x = 640 + i * 110 + (0 if row == 1 else 55)
            p.arc(x, y, 55, 55, 270, 90, w=10)
    p.circle(340, 700, 55, fill=255)                                            # göz
    p.dot(350, 705, 28)
    p.d.ellipse([355 * S, 688 * S, 368 * S, 701 * S], fill=255)
    p.arc(290, 800, 40, 30, 20, 140, w=10)                                      # gülümseme
    for x, y, r in [(170, 520, 45), (220, 380, 32), (160, 270, 24), (1100, 300, 40), (1170, 200, 26)]:
        p.circle(x, y, r, w=10)                                                 # kabarcıklar
    p.save("kolay", "lofi-fish-001")


def sky(p, sun_x=260, clouds=((820, 220), (1150, 400))):
    """Üstte güneş ve bulutlar (yatay resimleri dikey yapar, gökyüzü bölgeleri ekler)."""
    p.circle(sun_x, 260, 100)
    for i in range(8):
        a = i * math.pi / 4
        p.line([(sun_x + math.cos(a) * 135, 260 + math.sin(a) * 135), (sun_x + math.cos(a) * 190, 260 + math.sin(a) * 190)], w=12)
    face(p, sun_x, 250, 0.7, blush=False)
    for x, y in clouds:
        p.poly(curve([(x - 160, y + 50), (x - 110, y - 40), (x - 10, y - 70), (x + 90, y - 40), (x + 150, y + 50)], 10))


def turtle():
    """Kaplumbağa: kabukta altıgen desen."""
    p = Pen()
    sky(p)
    p.poly(curve([(150, 1300), (700, 1260), (1250, 1300), (1250, 1500), (150, 1500)], 8))   # çimen
    for x in (200, 1180):
        p.line([(x, 1290), (x, 1200)], w=9)
        for i in range(5):
            a = i * 2 * math.pi / 5 - math.pi / 2
            p.circle(x + math.cos(a) * 32, 1170 + math.sin(a) * 32, 22, w=8)
        p.circle(x, 1170, 16, w=8)
    for x in (420, 980):                                                        # bacaklar
        p.poly(curve([(x - 90, 1050), (x - 100, 1200), (x, 1250), (x + 100, 1200), (x + 90, 1050)], 10))
    p.poly([(1150, 950), (1300, 1010), (1150, 1040)])                           # kuyruk
    shell = curve([(330, 1050), (380, 700), (740, 480), (1100, 700), (1160, 1050)], 18)
    p.poly(shell, fill=255)
    p.poly([(300, 1050), (1190, 1050), (1190, 1120), (300, 1120)], fill=255)
    p.ellipse(220, 880, 170, 150, fill=255)                                     # kafa (kabuğun önünde)
    face(p, 200, 860, 0.75)

    def hexa(x, y, r):
        return [(x + math.cos(i * math.pi / 3) * r, y + math.sin(i * math.pi / 3) * r) for i in range(6)]
    p.poly(hexa(740, 800, 120), w=11)
    for x, y in [(520, 930), (960, 930), (580, 680), (920, 680)]:
        p.poly(hexa(x, y, 80), w=10)
    p.save("kolay", "lofi-turtle-001")


def snail():
    """Salyangoz: sarmal kabuk, mantar ve yaprak."""
    p = Pen()
    sky(p)
    p.poly(curve([(80, 1330), (500, 1250), (1000, 1260), (1330, 1330), (1000, 1450), (400, 1460)], 14))  # yaprak
    p.line([(180, 1345), (1230, 1335)], w=10)
    p.poly(curve([(180, 1230), (230, 900), (330, 700), (450, 760), (420, 1000), (900, 1080), (1150, 1170), (1140, 1250)], 16), fill=255)
    p.line([(300, 740), (240, 520)], w=12)                                      # boynuzlar
    p.line([(380, 730), (420, 520)], w=12)
    p.circle(240, 500, 32)
    p.circle(420, 500, 32)
    face(p, 330, 860, 0.6, blush=False)
    p.circle(780, 760, 320, fill=255)                                           # kabuk
    pts = []
    for i in range(0, 721, 6):                                                  # sarmal
        a = math.radians(i)
        r = 290 * (1 - i / 820)
        pts.append((780 + math.cos(a) * r, 760 + math.sin(a) * r))
    p.line(pts, w=13)
    p.poly([(1190, 1260), (1180, 1070), (1250, 1070), (1270, 1260)])            # mantar
    p.poly(curve([(1090, 1080), (1130, 940), (1220, 900), (1310, 940), (1340, 1080)], 12))
    for x, y in [(1170, 1000), (1260, 970), (1300, 1040)]:
        p.circle(x, y, 22, w=9)
    p.save("kolay", "lofi-snail-001")


def car():
    """Araba: tekerlek = daire, pencere = yamuk; trafik ışığı (kırmızı-sarı-yeşil öğretimi)."""
    p = Pen()
    p.poly([(1120, 1240), (1170, 1240), (1170, 700), (1120, 700)])              # direk
    p.poly([(1040, 280), (1250, 280), (1250, 720), (1040, 720)], fill=255)      # ışık kutusu
    for y in (360, 500, 640):
        p.circle(1145, y, 55)
    p.poly([(40, 1240), (1360, 1240), (1360, 1500), (40, 1500)])                # yol
    for x in range(120, 1300, 260):
        p.poly([(x, 1360), (x + 150, 1360), (x + 150, 1390), (x, 1390)], w=8)
    p.poly(curve([(130, 1050), (140, 900), (300, 860), (420, 640), (760, 620), (920, 860), (1010, 900),
                  (1010, 1060), (900, 1120), (240, 1120)], 14), fill=255)       # gövde
    p.poly([(330, 870), (450, 680), (570, 680), (570, 870)], w=11)              # pencereler
    p.poly([(620, 870), (620, 680), (740, 680), (860, 870)], w=11)
    p.line([(595, 870), (595, 1100)], w=10)                                     # kapı çizgisi
    p.line([(540, 940), (500, 940)], w=10)
    p.line([(700, 940), (660, 940)], w=10)
    p.ellipse(980, 950, 30, 40, w=10)                                           # far
    p.ellipse(160, 950, 22, 35, w=10)                                           # stop
    for x in (330, 820):                                                        # tekerlekler
        p.circle(x, 1120, 125, fill=255)
        p.circle(x, 1120, 55)
    p.save("kolay", "lofi-car-001")


# ---------------------------------------------------------------- hayvanlar (2. set)

def eyes(p, cx, cy, gap, r=20):
    """Parlak noktalı iki göz."""
    for dx in (-gap, gap):
        p.dot(cx + dx, cy, r)
        p.d.ellipse([(cx + dx + r * 0.25) * S, (cy - r * 0.55) * S, (cx + dx + r * 0.65) * S, (cy - r * 0.15) * S], fill=255)


def waves(p, y, x0=150, x1=1250, depth=160, amp=22, k=5):
    """Üstü dalgalı su bandı (kapalı bölge)."""
    top = [(x, y + math.sin((x - x0) / (x1 - x0) * k * 2 * math.pi) * amp) for x in range(x0, x1 + 1, 25)]
    p.poly(top + [(x1, y + depth), (x0, y + depth)], fill=255)


def band(pts, w0, w1):
    """Bir çizgi boyunca kalınlığı azalan kapalı şerit (dokunaç, kuyruk)."""
    left, right = [], []
    n = len(pts)
    for i, (x, y) in enumerate(pts):
        a = pts[max(0, i - 1)]
        b = pts[min(n - 1, i + 1)]
        dx, dy = b[0] - a[0], b[1] - a[1]
        L = math.hypot(dx, dy) or 1
        nx, ny = -dy / L, dx / L
        w = w0 + (w1 - w0) * i / (n - 1)
        left.append((x + nx * w, y + ny * w))
        right.append((x - nx * w, y - ny * w))
    return left + right[::-1]


def frog():
    p = Pen()
    sky(p)
    p.poly(curve([(200, 1360), (420, 1250), (700, 1230), (980, 1250), (1200, 1360), (980, 1470), (700, 1490), (420, 1470)], 12))  # nilüfer yaprağı
    p.line([(700, 1490), (760, 1380)], w=10)
    for x in (360, 1040):                                                        # arka bacaklar
        p.ellipse(x, 1180, 130, 150)
    p.ellipse(700, 1010, 380, 290, fill=255)                                     # gövde
    p.ellipse(700, 1100, 230, 160)                                               # karın
    for x in (520, 880):                                                         # göz tümsekleri
        p.circle(x, 740, 135, fill=255)
        p.circle(x, 740, 70)
    eyes(p, 700, 740, 180, 30)
    p.arc(700, 860, 230, 90, 25, 155, w=14)                                      # kocaman gülüş
    for x in (450, 950):
        p.ellipse(x, 900, 40, 24, w=9)
    for x in (540, 860):                                                         # ön ayaklar
        p.ellipse(x, 1290, 95, 50, fill=255)
    p.save("kolay", "lofi-frog-001")


def ladybug():
    p = Pen()
    # yaprak
    p.poly(curve([(250, 1400), (500, 1250), (900, 1240), (1200, 1380), (900, 1500), (500, 1500)], 12))
    p.line([(300, 1395), (1130, 1380)], w=9)
    # bacaklar
    for y in (800, 980, 1160):
        p.line([(330, y), (220, y + 60)], w=14)
        p.line([(1070, y), (1180, y + 60)], w=14)
    # antenler ve baş
    p.line([(640, 330), (560, 170)], w=12)
    p.line([(760, 330), (840, 170)], w=12)
    p.circle(560, 160, 34)
    p.circle(840, 160, 34)
    p.circle(700, 440, 190, fill=255)
    eyes(p, 700, 400, 65, 20)
    p.arc(700, 440, 40, 30, 20, 160, w=11)
    # gövde (kanat kabukları)
    p.circle(700, 930, 400, fill=255)
    p.line([(700, 530), (700, 1330)], w=14)
    for x, y, r in [(530, 760, 70), (870, 760, 70), (480, 980, 60), (920, 980, 60), (560, 1180, 65), (840, 1180, 65), (700, 620, 0)]:
        if r: p.circle(x, y, r)
    p.save("kolay", "lofi-ladybug-001")



def mouse():
    p = Pen()
    # kuyruk
    p.line(open_curve([(950, 1330), (1130, 1310), (1220, 1180), (1170, 1060), (1080, 1070)], 10), w=14)
    # gövde
    p.poly(curve([(700, 820), (950, 960), (980, 1250), (880, 1400), (520, 1400), (420, 1250), (450, 960)], 16))
    p.ellipse(700, 1170, 160, 170)                                               # karın
    # kulaklar
    for x in (460, 940):
        p.circle(x, 400, 170)
        p.circle(x, 400, 100)
    # baş
    p.ellipse(700, 620, 310, 240, fill=255)
    eyes(p, 700, 590, 95, 26)
    p.ellipse(700, 670, 34, 26)                                                  # burun
    p.arc(700, 700, 40, 30, 20, 160, w=11)
    for s in (-1, 1):
        for dy in (-12, 22):
            p.line([(700 + s * 120, 680 + dy), (700 + s * 300, 660 + dy * 2)], w=8)
    # ayaklar ve peynir
    for x in (580, 820):
        p.ellipse(x, 1410, 80, 40, fill=255)
    p.poly([(240, 1200), (520, 1300), (520, 1460), (240, 1460)], fill=255)
    for x, y, r in [(330, 1310, 30), (440, 1390, 24), (300, 1420, 18)]:
        p.circle(x, y, r, w=9)
    p.save("kolay", "lofi-mouse-001")


def penguin():
    p = Pen()
    # buz zemin
    p.poly([(180, 1400), (1220, 1400), (1160, 1520), (240, 1520)])
    for x, y, r in [(250, 400, 40), (1150, 500, 45), (1180, 1000, 35), (220, 1050, 35)]:
        star(p, x, y, r)                                                        # kar taneleri
    # kanatlar
    p.poly(curve([(390, 820), (250, 1050), (290, 1200), (390, 1100)], 10))
    p.poly(curve([(1010, 820), (1150, 1050), (1110, 1200), (1010, 1100)], 10))
    # gövde, karın
    p.ellipse(700, 920, 330, 500, fill=255)
    p.poly(curve([(700, 620), (870, 760), (900, 1100), (800, 1340), (600, 1340), (500, 1100), (530, 760)], 14))
    eyes(p, 700, 650, 75, 22)
    p.poly([(650, 710), (750, 710), (700, 780)])                                # gaga
    for x in (580, 820):
        p.ellipse(x, 740, 30, 18, w=8)
    # bere
    p.poly(curve([(450, 560), (500, 400), (700, 340), (900, 400), (950, 560)], 12), fill=255)
    p.poly([(440, 540), (960, 540), (960, 610), (440, 610)], fill=255)
    p.circle(700, 320, 55, fill=255)
    for x in (560, 700, 840):
        p.line([(x, 440), (x, 540)], w=9)
    # ayaklar
    for x in (590, 810):
        p.ellipse(x, 1410, 100, 45, fill=255)
    p.save("kolay", "lofi-penguin-001")



def bee():
    p = Pen()
    sky(p)
    # çiçekler
    for cx, cy in ((420, 1230), (1000, 1290)):
        p.line([(cx, cy + 80), (cx, 1480)], w=12)
        p.poly(curve([(cx, 1420), (cx + 90, 1340), (cx + 150, 1380), (cx + 60, 1440)], 8))
        for i in range(6):
            a = i * math.pi / 3
            p.circle(cx + math.cos(a) * 75, cy + math.sin(a) * 75, 52, w=11)
        p.circle(cx, cy, 48, fill=255, w=11)
    p.poly([(150, 1470), (1250, 1470), (1250, 1540), (150, 1540)])
    # kanatlar
    p.ellipse(680, 520, 120, 170)
    p.ellipse(850, 540, 110, 155)
    # gövde ve çizgiler
    cx, cy, rx, ry = 760, 760, 330, 230
    p.ellipse(cx, cy, rx, ry, fill=255)
    for dx in (-110, -40, 60, 130, 230):
        yy = ry * math.sqrt(max(0.0, 1 - (dx / rx) ** 2)) - 6
        p.line([(cx + dx, cy - yy), (cx + dx, cy + yy)], w=12)
    p.poly([(1080, 730), (1170, 760), (1080, 790)])                              # iğne
    # baş
    p.circle(400, 740, 170, fill=255)
    eyes(p, 390, 720, 60, 20)
    p.arc(390, 770, 40, 28, 20, 160, w=11)
    for x in (300, 480):
        p.ellipse(x, 790, 26, 16, w=8)
    p.line([(350, 590), (300, 460)], w=11)
    p.line([(440, 590), (480, 460)], w=11)
    p.circle(300, 450, 26)
    p.circle(480, 450, 26)
    p.save("kolay", "lofi-bee-001")


def octopus():
    p = Pen()
    for x in (180, 1220):                                                        # yosun
        p.poly(curve([(x - 40, 1500), (x - 70, 1300), (x - 10, 1120), (x + 30, 1180), (x - 10, 1320), (x + 40, 1500)], 10))
    p.poly(curve([(150, 1470), (700, 1430), (1250, 1470), (1250, 1560), (150, 1560)], 8))   # kum
    # dokunaçlar (dalgalı, uca doğru incelen)
    for i, bx in enumerate((440, 560, 700, 840, 960)):
        sgn = -1 if bx < 700 else (1 if bx > 700 else 0)
        pts = []
        for t in range(0, 21):
            u = t / 20
            x = bx + sgn * u * 170 + math.sin(u * math.pi * 2 + i) * 45
            y = 820 + u * 560
            pts.append((x, y))
        p.poly(band(pts, 55, 16), fill=255, w=12)
        for k in (6, 11, 15):                                                    # vantuzlar
            x, y = pts[k]
            p.circle(x, y, 16 - k * 0.5, w=7)
    # baş
    p.ellipse(700, 620, 380, 360, fill=255)
    eyes(p, 700, 680, 110, 32)
    p.arc(700, 740, 60, 40, 20, 160, w=12)
    for x in (510, 890):
        p.ellipse(x, 770, 44, 26, w=9)
    for x, y, r in [(560, 420, 40), (860, 450, 30), (720, 360, 26)]:
        p.circle(x, y, r, w=10)                                                  # benekler
    for x, y, r in [(220, 400, 40), (280, 260, 28), (1170, 330, 36), (1110, 200, 24)]:
        p.circle(x, y, r, w=10)                                                  # kabarcıklar
    p.save("kolay", "lofi-octopus-001")


def hedgehog():
    p = Pen()
    sky(p)
    p.poly(curve([(150, 1300), (700, 1260), (1250, 1300), (1250, 1500), (150, 1500)], 8))   # çimen
    # mantar
    p.poly([(1110, 1280), (1100, 1150), (1170, 1150), (1180, 1280)])
    p.poly(curve([(1020, 1160), (1060, 1040), (1140, 1010), (1220, 1040), (1250, 1160)], 10))
    # dikenli sırt (zikzak)
    cx, cy, rx, ry = 760, 1000, 420, 360
    pts = []
    for i in range(0, 37):
        a = math.radians(175 - i * 175 / 36)
        k = 1.0 if i % 2 == 0 else 0.84
        pts.append((cx + math.cos(a) * rx * k, cy - math.sin(a) * ry * k))
    pts += [(1150, 1250), (420, 1250)]
    p.poly(pts, fill=255)
    # içte dikenli desen
    for x, y in [(650, 800), (820, 760), (960, 880), (740, 960), (900, 1060)]:
        p.line([(x - 40, y + 40), (x, y - 20), (x + 40, y + 40)], w=10)
    # elma (sırtında)
    p.circle(800, 560, 90, fill=255)
    p.line([(800, 475), (815, 420)], w=12)
    p.poly(curve([(815, 440), (880, 400), (930, 420), (870, 460)], 8))
    # baş ve yüz
    p.ellipse(420, 1050, 250, 190, fill=255)
    p.dot(185, 1080, 26)                                                         # burun
    eyes(p, 380, 1010, 55, 20)
    p.arc(330, 1070, 40, 28, 20, 160, w=11)
    p.ellipse(470, 1100, 36, 22, w=8)
    p.ellipse(560, 900, 50, 40)                                                  # kulak
    for x in (520, 900):
        p.ellipse(x, 1260, 80, 40, fill=255)
    p.save("kolay", "lofi-hedgehog-001")


def hippo():
    p = Pen()
    sky(p, clouds=((850, 200), (1180, 260)))
    p.ellipse(700, 1200, 470, 330)                                               # gövde (sudan çıkıyor)
    for x in (480, 920):                                                         # kulaklar
        p.ellipse(x, 470, 60, 70)
    p.ellipse(700, 700, 330, 250, fill=255)                                      # baş
    for x in (570, 830):                                                         # gözler (tepede)
        p.circle(x, 560, 70, fill=255)
    eyes(p, 700, 570, 130, 26)
    p.ellipse(700, 880, 380, 190, fill=255)                                      # burun
    for x in (590, 810):
        p.ellipse(x, 830, 40, 28)
    p.arc(700, 910, 160, 70, 20, 160, w=13)
    for x in (480, 920):
        p.ellipse(x, 920, 44, 26, w=9)
    waves(p, 1240, depth=330)
    for x, y in [(300, 1180), (1120, 1170)]:                                     # nilüferler
        p.ellipse(x, y, 90, 30, fill=255, w=10)
    p.save("kolay", "lofi-hippo-001")


# ---------------------------------------------------------------- hayvanlar (3. set)


def parrot():
    p = Pen()
    sky(p)
    # kuyruk tüyleri
    for dx in (-90, 0, 90):
        p.poly(curve([(700 + dx * 0.3 - 45, 1150), (700 + dx * 0.3 + 45, 1150), (700 + dx + 30, 1420), (700 + dx, 1540), (700 + dx - 30, 1420)], 10), fill=255)
    # gövde
    p.poly(curve([(700, 560), (910, 680), (960, 950), (830, 1210), (580, 1210), (450, 950), (500, 680)], 16), fill=255)
    p.ellipse(650, 960, 150, 200)                                                # göğüs
    # kanat ve tüyleri
    p.poly(curve([(830, 700), (960, 900), (920, 1180), (760, 1080), (760, 850)], 12), fill=255)
    for y in (880, 970, 1060):
        p.arc(860, y, 70, 40, 200, 340, w=9)
    # tepe tüyleri
    for dx, h in ((-70, 300), (0, 340), (70, 300)):
        p.poly(curve([(700 + dx - 35, 330), (700 + dx, 330 - h * 0.6), (700 + dx + 35, 330)], 8))
    p.circle(700, 450, 200, fill=255)                                            # baş
    p.circle(770, 420, 58)                                                       # göz halkası
    eyes(p, 770, 420, 0, 26)
    p.poly(curve([(560, 440), (440, 470), (430, 600), (520, 560), (600, 520)], 10), fill=255)   # gaga
    p.ellipse(860, 520, 34, 20, w=8)
    # tünek ve ayaklar
    p.poly(band([(220 + i * 50, 1250) for i in range(20)], 32, 32), fill=255)
    for x in (620, 780):
        p.ellipse(x, 1220, 50, 34, fill=255)
    p.save("kolay", "lofi-parrot-001")



def starfish():
    p = Pen()
    for x, y, r in [(220, 300, 40), (300, 180, 28), (1150, 260, 36), (1090, 140, 24)]:
        p.circle(x, y, r, w=10)                                                  # kabarcıklar
    p.poly(curve([(150, 1380), (700, 1340), (1250, 1380), (1250, 1520), (150, 1520)], 8))   # kum
    # yumuşak köşeli deniz yıldızı
    pts = []
    for i in range(10):
        a = -math.pi / 2 + i * math.pi / 5
        r = 520 if i % 2 == 0 else 250
        pts.append((700 + math.cos(a) * r, 860 + math.sin(a) * r))
    p.poly(curve(pts, 14), fill=255)
    face(p, 700, 860, 1.2)
    for i in range(5):                                                           # kollardaki benekler
        a = -math.pi / 2 + i * 2 * math.pi / 5
        for d, r in ((330, 34), (430, 22)):
            p.circle(700 + math.cos(a) * d, 860 + math.sin(a) * d, r, w=9)
    # deniz kabuğu
    p.poly(curve([(1020, 1430), (1080, 1330), (1180, 1330), (1230, 1430)], 10), fill=255)
    for x in (1080, 1125, 1170):
        p.line([(1125, 1430), (x, 1350)], w=8)
    p.save("kolay", "lofi-starfish-001")


def jellyfish():
    p = Pen()
    for x, y, r in [(250, 350, 40), (310, 220, 26), (1150, 420, 36), (1110, 280, 24), (1180, 1200, 30)]:
        p.circle(x, y, r, w=10)
    # dokunaçlar
    for i, bx in enumerate((470, 590, 700, 810, 930)):
        pts = [(bx + math.sin(u / 20 * math.pi * 3 + i) * 40, 700 + u / 20 * 760) for u in range(21)]
        p.poly(band(pts, 30, 10), fill=255, w=11)
    # şemsiye: üstü kubbe, altı fırfırlı
    top = [(700 + math.cos(math.radians(a)) * 380, 700 - math.sin(math.radians(a)) * 360) for a in range(0, 181, 6)]
    frill = []
    for i in range(0, 41):
        x = 320 + i * 760 / 40
        frill.append((x, 700 + abs(math.sin(i / 40 * math.pi * 6)) * 60))
    p.poly(top + frill, fill=255)
    face(p, 700, 560, 1.1)
    for x, y, r in [(530, 430, 38), (880, 450, 30), (720, 380, 24)]:
        p.circle(x, y, r, w=9)
    p.save("kolay", "lofi-jellyfish-001")


def hen():
    p = Pen()
    sky(p)
    # kuyruk tüyleri
    for dy, h in ((0, 0), (90, 30), (180, 60)):
        p.poly(curve([(980, 820 + dy), (1150, 650 + dy + h), (1210, 720 + dy + h), (1050, 900 + dy)], 10))
    p.ellipse(720, 1000, 370, 300, fill=255)                                     # gövde
    p.poly(curve([(700, 930), (900, 900), (960, 1060), (820, 1160), (660, 1080)], 12))   # kanat
    for y in (1000, 1070):
        p.arc(820, y, 90, 40, 200, 340, w=9)
    for x, y in ((450, 430), (520, 400), (590, 430)):                            # ibik
        p.circle(x, y, 50)
    p.circle(520, 610, 185, fill=255)                                            # baş
    p.poly([(360, 600), (270, 640), (360, 680)])                                 # gaga
    p.ellipse(380, 730, 34, 50)                                                  # sakal
    eyes(p, 480, 570, 0, 22)
    p.ellipse(560, 650, 34, 20, w=8)
    # yuva ve yumurtalar
    for x in (550, 860):
        p.ellipse(x, 1270, 70, 90, fill=255)
    p.poly(curve([(280, 1280), (700, 1300), (1120, 1280), (1020, 1460), (700, 1490), (380, 1460)], 12), fill=255)
    for x, y in ((420, 1350), (600, 1400), (800, 1400), (980, 1350)):
        p.line([(x - 60, y - 20), (x + 60, y + 20)], w=9)
    p.save("kolay", "lofi-hen-001")


# ---------------------------------------------------------------- yeniden çizilenler

def leaf(p, x, y, ang, L=150, W=55, w=11):
    """Ucu sivri yaprak (ortasında damar)."""
    ca, sa = math.cos(math.radians(ang)), math.sin(math.radians(ang))
    def T(u, v): return (x + u * ca - v * sa, y + u * sa + v * ca)
    pts = [T(L * t, math.sin(math.pi * t) * W) for t in [i / 12 for i in range(13)]]
    pts += [T(L * t, -math.sin(math.pi * t) * W) for t in [i / 12 for i in range(11, 0, -1)]]
    p.poly(pts, w=w)
    p.line([T(10, 0), T(L * 0.8, 0)], w=8)


def crescent(cx, cy, r, thick, a0, a1):
    """Hilal (muz gibi): dış yay + kayık iç yay."""
    outer = [(cx + math.cos(math.radians(a)) * r, cy + math.sin(math.radians(a)) * r) for a in range(a0, a1 + 1, 4)]
    icx, icy = cx - math.cos(math.radians((a0 + a1) / 2)) * thick, cy - math.sin(math.radians((a0 + a1) / 2)) * thick
    inner = [(icx + math.cos(math.radians(a)) * r, icy + math.sin(math.radians(a)) * r) for a in range(a1 - 8, a0 + 7, -4)]
    return outer + inner


def open_curve(pts, n=10):
    """Uçları açık yumuşak eğri (kuyruk, ip)."""
    return curve(pts, n)[: (len(pts) - 1) * n + 1]


def cow():
    p = Pen()
    p.poly(curve([(150, 1360), (700, 1320), (1250, 1360), (1250, 1530), (150, 1530)], 8))   # çayır
    for x in (240, 1150):                                                       # papatyalar
        for i in range(6):
            a = i * math.pi / 3
            p.circle(x + math.cos(a) * 34, 1250 + math.sin(a) * 34, 24, w=8)
        p.circle(x, 1250, 18, w=8)
        p.line([(x, 1290), (x, 1340)], w=8)
    p.line(open_curve([(940, 1150), (1060, 1120), (1110, 1020), (1080, 960)]), w=12)   # kuyruk
    p.ellipse(1080, 945, 30, 40)
    p.ellipse(700, 1130, 300, 270, fill=255)                                    # gövde
    p.poly(curve([(450, 1020), (540, 990), (580, 1080), (520, 1170), (440, 1130)], 10))   # benekler
    p.poly(curve([(860, 1060), (950, 1030), (990, 1130), (920, 1200), (850, 1150)], 10))
    for x in (590, 810):                                                        # ön bacaklar + toynak
        p.poly(curve([(x - 60, 1220), (x + 60, 1220), (x + 62, 1420), (x - 62, 1420)], 6), fill=255)
        p.line([(x - 60, 1360), (x + 60, 1360)], w=11)
    # kulaklar ve boynuzlar (başın arkasında)
    p.poly(curve([(470, 560), (330, 520), (260, 570), (330, 620), (470, 620)], 10))
    p.poly(curve([(930, 560), (1070, 520), (1140, 570), (1070, 620), (930, 620)], 10))
    p.poly(curve([(540, 440), (510, 330), (560, 300), (600, 420)], 8))
    p.poly(curve([(860, 440), (890, 330), (840, 300), (800, 420)], 8))
    p.ellipse(700, 620, 290, 250, fill=255)                                     # baş
    p.poly(curve([(560, 400), (650, 380), (660, 450), (590, 500), (530, 470)], 10))   # perçem/benek
    p.ellipse(700, 780, 220, 120, fill=255)                                     # burun
    for x in (630, 770):
        p.ellipse(x, 765, 30, 22)
    p.arc(700, 800, 70, 40, 25, 155, w=11)
    eyes(p, 700, 620, 110, 26)
    for x in (510, 890):
        p.ellipse(x, 690, 38, 22, w=8)
    # tasma ve çan
    p.arc(700, 760, 260, 170, 40, 140, w=26)
    p.poly(curve([(650, 920), (750, 920), (780, 1030), (620, 1030)], 8), fill=255)
    p.circle(700, 1035, 20)
    p.save("kolay", "lofi-cow-001")


def camel():
    p = Pen()
    sky(p, sun_x=1130, clouds=((620, 230),))
    p.poly(curve([(150, 1380), (500, 1310), (900, 1360), (1250, 1320), (1250, 1520), (150, 1520)], 10))   # kum tepeleri
    def rounded_bar(x0, y0, x1, y1):
        r = (x1 - x0) / 2
        top = [((x0 + x1) / 2 - math.cos(math.radians(t)) * r, y0 + r - math.sin(math.radians(t)) * r) for t in range(0, 181, 15)]
        return top + [(x1, y1), (x0, y1)]
    p.poly(rounded_bar(1125, 1080, 1165, 1190), fill=255)                        # kaktüs kolu
    p.poly([(1125, 1150), (1170, 1150), (1170, 1190), (1125, 1190)], fill=255)
    p.poly(rounded_bar(1165, 950, 1235, 1330), fill=255)                         # kaktüs gövdesi
    for x in (520, 640, 830, 950):                                              # kalın, yuvarlak bacaklar
        p.poly(curve([(x - 45, 1000), (x + 45, 1000), (x + 42, 1350), (x, 1375), (x - 42, 1350)], 8), fill=255)
        p.line([(x - 42, 1320), (x + 42, 1320)], w=9)
    p.line(open_curve([(1045, 960), (1090, 1010), (1100, 1070)]), w=12)          # kuyruk
    p.poly([(1080, 1060), (1120, 1060), (1100, 1120)], w=9)
    p.poly(band(open_curve([(500, 960), (400, 780), (340, 650), (320, 560)], 8), 75, 60), fill=255)
    body = [(420, 1040), (430, 860), (560, 640), (660, 760), (740, 760), (840, 620), (960, 760), (1060, 960), (1000, 1090), (500, 1100)]
    p.poly(curve(body, 14), fill=255)
    # eyer örtüsü ve püskülleri
    p.poly(curve([(600, 830), (900, 830), (920, 960), (580, 960)], 8), fill=255)
    for x in range(610, 900, 50):
        p.poly([(x, 960), (x + 25, 1010), (x + 50, 960)], w=9)
    # boyun ve baş
    p.ellipse(380, 470, 28, 45)                                                 # kulak
    p.ellipse(270, 540, 175, 115, fill=255)
    eyes(p, 300, 510, 0, 24)
    p.line([(285, 470), (275, 450)], w=7)                                       # kirpikler
    p.line([(305, 468), (310, 447)], w=7)
    p.ellipse(130, 545, 16, 10)                                                 # burun deliği
    p.arc(185, 585, 55, 30, 20, 150, w=11)
    p.ellipse(360, 590, 32, 18, w=8)
    p.save("kolay", "lofi-camel-001")


def monkey():
    p = Pen()
    p.poly(curve([(150, 1440), (700, 1400), (1250, 1440), (1250, 1540), (150, 1540)], 8))   # zemin
    tail = open_curve([(880, 1300), (1060, 1260), (1140, 1100), (1100, 960), (1000, 950), (990, 1040)], 10)
    p.poly(band(tail, 26, 20), fill=255)                                        # kıvrık kuyruk
    p.ellipse(700, 1140, 270, 280, fill=255)                                    # gövde
    p.ellipse(700, 1190, 170, 190)                                              # karın
    for x in (560, 840):                                                        # ayaklar
        p.ellipse(x, 1410, 115, 58, fill=255)
    p.poly(crescent(700, 920, 300, 70, 55, 125), fill=255)                      # muz (karnın önünde)
    p.line([(560, 1160), (530, 1130)], w=12)
    for side in (-1, 1):                                                        # muzu tutan eller
        p.ellipse(700 + side * 205, 1185, 66, 54, fill=255)
    for x in (380, 1020):                                                       # kulaklar
        p.circle(x, 600, 100)
        p.circle(x, 600, 56)
    p.circle(700, 600, 310, fill=255)                                           # baş
    p.poly(curve([(690, 300), (650, 220), (720, 190), (750, 300)], 8))          # perçem
    mask = [(700, 560), (810, 450), (930, 490), (950, 640), (860, 820), (700, 870), (540, 820), (450, 640), (470, 490), (590, 450)]
    p.poly(curve(mask, 12), fill=255)
    eyes(p, 700, 600, 105, 26)
    for x in (675, 725):
        p.dot(x, 700, 11)
    p.arc(700, 750, 100, 45, 20, 160, w=12)
    p.save("kolay", "lofi-monkey-001")


def squirrel():
    p = Pen()
    p.poly(curve([(150, 1420), (700, 1380), (1250, 1420), (1250, 1530), (150, 1530)], 8))   # zemin
    leaf(p, 230, 1380, -30, 130, 45)
    leaf(p, 1120, 1390, -150, 130, 45)
    tail = [(800, 1340), (1060, 1290), (1210, 1090), (1240, 830), (1170, 600), (1020, 470), (860, 470), (780, 560),
            (880, 640), (990, 800), (1020, 1000), (950, 1180), (800, 1210)]
    p.poly(curve(tail, 12), fill=255)                                           # kabarık kuyruk
    for a, b in (((1100, 640), (1160, 780)), ((1140, 900), (1150, 1040)), ((1090, 1130), (1030, 1230)), ((960, 520), (1060, 560))):
        p.line([a, b], w=9)                                                     # tüy çizgileri
    p.ellipse(650, 1110, 230, 270, fill=255)                                    # gövde
    p.ellipse(640, 1160, 135, 180)                                              # karın
    for x, y in ((450, 470), (700, 460)):                                       # kulaklar + püskül
        p.ellipse(x, y, 55, 90)
        p.poly([(x - 18, y - 80), (x, y - 140), (x + 18, y - 80)], w=9)
    p.ellipse(575, 680, 230, 195, fill=255)                                     # baş
    eyes(p, 575, 650, 80, 24)
    p.dot(575, 720, 16)
    p.arc(575, 745, 34, 24, 20, 160, w=10)
    for x in (440, 710):
        p.ellipse(x, 735, 34, 20, w=8)
    p.ellipse(640, 1060, 72, 82, fill=255)                                      # palamut
    p.poly(curve([(555, 1015), (640, 950), (725, 1015), (640, 1040)], 8), fill=255)
    p.line([(640, 955), (650, 915)], w=10)
    for x in (555, 725):                                                        # patiler
        p.ellipse(x, 1030, 48, 34, fill=255)
    for x in (520, 760):
        p.ellipse(x, 1385, 90, 42, fill=255)
    p.save("kolay", "lofi-squirrel-001")


def koala():
    p = Pen()
    p.ellipse(700, 1120, 300, 290)                                              # gövde
    p.ellipse(700, 1180, 170, 170)                                              # karın
    # okaliptüs dalı ve yaprakları
    br = open_curve([(130, 1290), (450, 1250), (800, 1270), (1270, 1230)], 10)
    p.poly(band(br, 40, 34), fill=255)
    leaf(p, 260, 1300, 70, 150, 50)
    leaf(p, 1060, 1270, 100, 160, 52)
    leaf(p, 1180, 1250, 60, 130, 45)
    for x in (370, 1030):                                                       # kulaklar
        p.circle(x, 460, 165)
        p.circle(x, 460, 95)
    p.ellipse(700, 620, 340, 290, fill=255)                                     # baş
    p.ellipse(700, 680, 75, 100, fill=255)                                      # büyük burun
    eyes(p, 700, 590, 160, 24)
    p.arc(700, 800, 40, 26, 20, 160, w=11)
    for x in (480, 920):
        p.ellipse(x, 720, 40, 24, w=9)
    for x in (550, 860):                                                        # dala sarılan patiler
        p.ellipse(x, 1255, 95, 62, fill=255)
    p.save("kolay", "lofi-koala-001")


def whale():
    p = Pen()
    for side in (-1, 1):                                                        # fıskiye
        p.line(open_curve([(620, 560), (620 + side * 40, 420), (620 + side * 130, 340)], 8), w=14)
    p.line([(620, 560), (620, 330)], w=14)
    for x, y, r in [(470, 310, 30), (780, 300, 30), (620, 260, 26)]:
        p.circle(x, y, r, w=10)
    p.poly(curve([(1040, 880), (1140, 700), (1280, 560), (1230, 740), (1300, 800), (1110, 930)], 12))   # kuyruk
    p.poly(curve([(180, 920), (330, 680), (650, 590), (980, 690), (1110, 900), (950, 1130), (560, 1180), (260, 1090)], 18), fill=255)
    p.line(open_curve([(185, 960), (420, 1050), (700, 1075), (1075, 990)], 10), w=12)   # karın çizgisi (alt bölge)
    p.poly(curve([(640, 900), (760, 840), (820, 930), (730, 980)], 8))          # yüzgeç
    eyes(p, 420, 830, 0, 26)
    p.arc(330, 900, 70, 40, 20, 150, w=12)
    p.ellipse(480, 910, 34, 20, w=9)
    # küçük balık arkadaş
    p.poly([(1000, 380), (1060, 340), (1060, 420)])
    p.ellipse(940, 380, 80, 50, fill=255)
    p.dot(910, 370, 10)
    waves(p, 1260)
    p.save("kolay", "lofi-whale-001")


# ---------------------------------------------------------------- rakam sayfaları (1-10)
# Büyük rakam + o kadar nesne: çocuk rakamı boyar, nesneleri sayar (rakam-miktar ilişkisi).

FONT = os.path.join(ROOT, "LofiBoya", "Assets", "Resources", "Fonts", "Main.ttf")


def heart_pts(x, y, r):
    pts = []
    for i in range(40):
        t = i / 40 * 2 * math.pi
        hx = 16 * math.sin(t) ** 3
        hy = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        pts.append((x + hx * r / 17, y - hy * r / 17))
    return pts


def obj_apple(p, x, y, r):
    p.poly(curve([(x, y - r * 0.7), (x + r * 0.8, y - r * 0.85), (x + r, y), (x + r * 0.55, y + r * 0.9),
                  (x, y + r * 0.8), (x - r * 0.55, y + r * 0.9), (x - r, y), (x - r * 0.8, y - r * 0.85)], 10), w=11)
    p.line([(x, y - r * 0.7), (x + r * 0.1, y - r * 1.05)], w=10)
    leaf(p, x + r * 0.12, y - r * 0.9, -30, r * 0.6, r * 0.22, w=8)


def obj_star(p, x, y, r):
    pts = []
    for i in range(10):
        a = -math.pi / 2 + i * math.pi / 5
        rr = r if i % 2 == 0 else r * 0.5
        pts.append((x + math.cos(a) * rr, y + math.sin(a) * rr))
    p.poly(curve(pts, 6), w=11)


def obj_heart(p, x, y, r):
    p.poly(heart_pts(x, y, r), w=11)


def obj_flower(p, x, y, r):
    for i in range(5):
        a = i * 2 * math.pi / 5 - math.pi / 2
        p.circle(x + math.cos(a) * r * 0.55, y + math.sin(a) * r * 0.55, r * 0.42, w=10)
    p.circle(x, y, r * 0.3, fill=255, w=10)


def obj_balloon(p, x, y, r):
    p.line(open_curve([(x, y + r * 0.95), (x - r * 0.15, y + r * 1.3), (x + r * 0.1, y + r * 1.6)], 6), w=8)
    p.poly([(x - r * 0.15, y + r * 1.0), (x + r * 0.15, y + r * 1.0), (x, y + r * 0.85)], w=8)
    p.ellipse(x, y, r * 0.75, r * 0.92, fill=255, w=11)


def obj_fish(p, x, y, r):
    p.poly([(x + r * 0.55, y), (x + r * 1.05, y - r * 0.45), (x + r * 1.05, y + r * 0.45)], w=10)
    p.ellipse(x - r * 0.1, y, r * 0.75, r * 0.5, fill=255, w=11)
    p.dot(x - r * 0.45, y - r * 0.08, r * 0.09)


def obj_ball(p, x, y, r):
    p.circle(x, y, r * 0.85, w=11)
    p.arc(x, y - r * 1.25, r * 1.3, r * 1.0, 60, 120, w=9)
    p.arc(x, y + r * 1.25, r * 1.3, r * 1.0, 240, 300, w=9)


OBJECTS = [obj_balloon, obj_heart, obj_star, obj_apple, obj_flower, obj_fish, obj_ball, obj_star, obj_heart, obj_apple]


def number_page(n):
    from PIL import ImageFont
    p = Pen()
    # büyük, kabarık rakam (içi boş, kalın kenarlı)
    font = ImageFont.truetype(FONT, 700 * S)
    text = str(n)
    box = p.d.textbbox((0, 0), text, font=font, stroke_width=LW * S)
    tw, th = box[2] - box[0], box[3] - box[1]
    p.d.text((W * S / 2 - tw / 2 - box[0], 60 * S - box[1]), text, font=font, fill=255, stroke_width=LW * S, stroke_fill=0)
    # nesneler: en çok 5'li iki sıra, ortalı
    draw = OBJECTS[n - 1]
    rows = [n] if n <= 5 else [math.ceil(n / 2), n // 2]
    r = {1: 210, 2: 175, 3: 150, 4: 128, 5: 108}.get(n, 108)
    gap = 2.3 * r + 20
    for ri, cnt in enumerate(rows):
        y = (1150 if len(rows) == 1 else 1030 + ri * 290)
        x0 = W / 2 - (cnt - 1) * gap / 2
        for k in range(cnt):
            draw(p, x0 + k * gap, y, r)
    p.save("kolay", f"lofi-num-{n}")


def numbers():
    for n in range(1, 11):
        number_page(n)


# ================================================================ ORTA SEVİYE (sahneler)
# Daha çok parça (≈45-130 bölge): taşıtlar, sebzeler, meslekler, alışkanlıklar, duygular...

def rect(p, x0, y0, x1, y1, fill=255, w=LW):
    p.poly([(x0, y0), (x1, y0), (x1, y1), (x0, y1)], fill=fill, w=w)


def rrect(x0, y0, x1, y1, r):
    """Yuvarlak köşeli dikdörtgen noktaları."""
    pts = []
    for cx, cy, a0 in ((x1 - r, y0 + r, -90), (x1 - r, y1 - r, 0), (x0 + r, y1 - r, 90), (x0 + r, y0 + r, 180)):
        for a in range(a0, a0 + 91, 15):
            pts.append((cx + math.cos(math.radians(a)) * r, cy + math.sin(math.radians(a)) * r))
    return pts


def sun(p, x, y, r=90):
    p.circle(x, y, r)
    for i in range(8):
        a = i * math.pi / 4
        p.line([(x + math.cos(a) * (r + 30), y + math.sin(a) * (r + 30)), (x + math.cos(a) * (r + 80), y + math.sin(a) * (r + 80))], w=12)
    face(p, x, y - 10, r / 145, blush=False)


def cloud(p, x, y, s=1.0):
    p.poly(curve([(x - 160 * s, y + 50 * s), (x - 110 * s, y - 40 * s), (x - 10 * s, y - 70 * s), (x + 90 * s, y - 40 * s), (x + 150 * s, y + 50 * s)], 10), fill=255)


def tree(p, x, y, s=1.0):
    """Gövde + yuvarlak yaprak kümesi + elmalar."""
    rect(p, x - 35 * s, y - 230 * s, x + 35 * s, y)
    p.poly(curve([(x, y - 520 * s), (x + 150 * s, y - 470 * s), (x + 190 * s, y - 330 * s), (x + 110 * s, y - 210 * s),
                  (x - 110 * s, y - 210 * s), (x - 190 * s, y - 330 * s), (x - 150 * s, y - 470 * s)], 10), fill=255)
    for dx, dy in ((-80, -380), (70, -420), (20, -280)):
        p.circle(x + dx * s, y + dy * s, 26 * s, w=9)


def wheel(p, x, y, r):
    p.circle(x, y, r, fill=255)
    p.circle(x, y, r * 0.5)
    p.dot(x, y, r * 0.12)


def kid(p, cx, top, s=1.0, hair="short", mood="happy", body=True):
    """Sevimli çocuk figürü. top: başın üstü. Döner: (omuz_y, gövde_alt_y)."""
    r = 140 * s
    hy = top + r
    if hair == "long":                                                          # arkadaki saç
        p.poly(curve([(cx - r * 1.1, hy), (cx - r * 1.15, hy + r * 1.2), (cx, hy + r * 1.0), (cx + r * 1.15, hy + r * 1.2), (cx + r * 1.1, hy)], 10), fill=255)
    by = hy + r
    if body:
        p.poly([(cx - 90 * s, by - 10 * s), (cx + 90 * s, by - 10 * s), (cx + 135 * s, by + 30 * s), (cx + 150 * s, by + 280 * s),
                (cx - 150 * s, by + 280 * s), (cx - 135 * s, by + 30 * s)], fill=255)
        for dx in (-60, 60):                                                    # bacaklar ve ayakkabılar
            rect(p, cx + (dx - 35) * s, by + 280 * s, cx + (dx + 35) * s, by + 420 * s)
            p.ellipse(cx + (dx + (8 if dx > 0 else -8)) * s, by + 430 * s, 62 * s, 32 * s, fill=255)
    p.circle(cx, hy, r, fill=255)                                               # baş
    # saç (üst kısım + kâkül)
    top_arc = [(cx + math.cos(math.radians(a)) * r * 1.06, hy + math.sin(math.radians(a)) * r * 1.06) for a in range(180, 361, 10)]
    bangs = []
    for i in range(25):
        t = i / 24
        x = cx + r * 1.0 - t * r * 2.0
        bangs.append((x, hy - r * 0.30 + abs(math.sin(t * math.pi * 3)) * 26 * s))
    p.poly(top_arc + bangs, fill=255)
    if hair == "bun":
        p.circle(cx, hy - r * 1.15, r * 0.35, fill=255)
    # yüz
    ey = hy + 10 * s
    eyes(p, cx, ey, 50 * s, 15 * s)
    if mood == "happy":
        p.arc(cx, ey + 35 * s, 30 * s, 22 * s, 20, 160, w=max(8, int(10 * s)))
    elif mood == "open":
        p.ellipse(cx, ey + 55 * s, 26 * s, 20 * s, fill=255, w=max(8, int(9 * s)))
    for dx in (-85, 85):
        p.ellipse(cx + dx * s, ey + 40 * s, 22 * s, 13 * s, w=7)
    return by, by + 280 * s


# ---------------------------------------------------------------- taşıtlar

def bus():
    p = Pen()
    # arkadaki binalar
    for x0, top, x1 in ((180, 420, 420), (460, 520, 700), (760, 380, 1000), (1040, 470, 1280)):
        rect(p, x0, top, x1, 1230)
        for r in range(3):
            for c in range(2):
                wx = x0 + 30 + c * (x1 - x0 - 60) / 2
                rect(p, wx, top + 30 + r * 70, wx + 70, top + 80 + r * 70, w=7)
    sun(p, 1170, 230)
    cloud(p, 330, 230)
    cloud(p, 760, 330, 0.8)
    p.poly([(60, 1230), (1340, 1230), (1340, 1480), (60, 1480)], fill=255)      # yol
    for x in range(110, 1300, 260):
        rect(p, x, 1340, x + 150, 1370, w=8)
    # otobüs gövdesi
    p.poly(rrect(300, 620, 1300, 1180, 60), fill=255)
    rect(p, 300, 1040, 1300, 1080, w=10)                                         # şerit
    for i in range(4):                                                          # yolcu pencereleri
        x0 = 340 + i * 175
        p.poly(rrect(x0, 680, x0 + 150, 860, 20), fill=255, w=11)
        p.circle(x0 + 75, 790, 46, fill=255, w=9)                                # yolcu yüzü
        p.arc(x0 + 75, 760, 46, 40, 190, 350, w=9)
        eyes(p, x0 + 75, 790, 16, 7)
        p.arc(x0 + 75, 805, 14, 10, 20, 160, w=7)
    p.poly(rrect(1060, 680, 1270, 1000, 20), fill=255, w=11)                     # şoför camı
    rect(p, 1060, 870, 1160, 1030, w=10)                                          # kapı
    p.line([(1110, 870), (1110, 1030)], w=8)
    p.ellipse(1285, 1110, 22, 30, w=9)                                           # far
    rect(p, 380, 560, 700, 620, w=10)                                             # tabela
    for x in (500, 1100):
        wheel(p, x, 1185, 95)
    # durak tabelası
    rect(p, 115, 600, 135, 1230, w=10)
    p.circle(125, 560, 70, fill=255, w=11)
    p.save("orta", "lofi-bus-001")


def train():
    p = Pen()
    sun(p, 230, 230)
    cloud(p, 1100, 220)
    p.poly(curve([(130, 900), (350, 760), (700, 840), (1050, 740), (1270, 880), (1270, 1000), (130, 1000)], 12), fill=255)   # tepeler
    # raylar ve traversler
    p.poly([(130, 1300), (1270, 1300), (1270, 1340), (130, 1340)])
    for x in range(80, 1270, 110):
        rect(p, x, 1340, x + 50, 1390, w=8)
    # duman bulutları
    for x, y, r in ((1120, 560, 55), (1050, 470, 70), (950, 390, 85)):
        p.circle(x, y, r, fill=255, w=11)
    # lokomotif
    rect(p, 880, 900, 1260, 1210)                                                  # kazan
    p.poly(rrect(950, 700, 1150, 900, 20), fill=255)                               # kabin
    rect(p, 990, 740, 1110, 840, w=10)
    p.poly([(1100, 680), (1180, 680), (1170, 610), (1110, 610)], fill=255)         # baca
    p.poly([(1260, 1210), (1330, 1210), (1260, 1100)], fill=255)                   # mahmuz
    p.circle(1210, 980, 40, w=10)
    # vagonlar
    for x0 in (120, 500):
        p.poly(rrect(x0, 870, x0 + 340, 1210, 25), fill=255)
        for k in range(2):
            rect(p, x0 + 40 + k * 145, 920, x0 + 150 + k * 145, 1040, w=10)
        rect(p, x0, 1110, x0 + 340, 1150, w=9)
        p.line([(x0 + 340, 1180), (x0 + 380, 1180)], w=12)                          # bağlantı
        for dx in (80, 260):
            wheel(p, x0 + dx, 1240, 55)
    for dx in (960, 1100, 1220):
        wheel(p, dx, 1240, 58)
    p.save("orta", "lofi-train-001")


def airplane():
    p = Pen()
    sun(p, 1180, 230)
    for x, y, s in ((280, 260, 1.0), (700, 1250, 1.1), (1150, 1350, 0.8), (230, 1100, 0.7)):
        cloud(p, x, y, s)
    for x, y in ((500, 250), (600, 330)):                                       # kuşlar
        p.arc(x - 30, y, 30, 22, 200, 340, w=9)
        p.arc(x + 30, y, 30, 22, 200, 340, w=9)
    # kanat (arka), kuyruk
    p.poly([(640, 800), (780, 800), (560, 560), (470, 560)], fill=255)
    p.poly([(180, 700), (300, 700), (260, 470), (190, 470)], fill=255)
    p.poly([(160, 770), (330, 770), (300, 840), (140, 840)], fill=255)
    # gövde
    p.poly(curve([(160, 760), (400, 700), (1000, 690), (1220, 760), (1260, 820), (1180, 880), (400, 900), (170, 850)], 14), fill=255)
    p.poly(curve([(1060, 720), (1180, 740), (1230, 800), (1100, 790)], 8), fill=255)   # kokpit camı
    for i in range(7):
        p.circle(420 + i * 85, 780, 26, w=9)                                     # yolcu camları
    rect(p, 300, 820, 1150, 845, w=8)                                             # şerit
    # ön kanat
    p.poly([(600, 840), (760, 840), (560, 1120), (450, 1120)], fill=255)
    p.ellipse(540, 1000, 60, 30, fill=255, w=10)                                 # motor
    # kanat ve kuyruk panelleri
    p.line([(520, 960), (660, 960)], w=8)
    p.line([(480, 1040), (610, 1040)], w=8)
    p.line([(200, 600), (280, 600)], w=8)
    rect(p, 900, 760, 960, 850, w=8)                                             # kapı
    # aşağıda küçük köy
    p.poly(curve([(170, 1480), (500, 1400), (900, 1440), (1230, 1400), (1230, 1520), (170, 1520)], 8), fill=255)
    for x in (380, 620, 1000):
        p.poly([(x - 70, 1420), (x, 1360), (x + 70, 1420)], fill=255, w=9)
        rect(p, x - 60, 1420, x + 60, 1500, w=9)
        rect(p, x - 15, 1450, x + 15, 1500, w=7)
    # küçük balon
    p.ellipse(1150, 560, 90, 110, fill=255)
    for c in (-0.5, 0, 0.5):
        p.line([(1150 + c * 90 * math.cos(math.radians(t)), 560 + 110 * math.sin(math.radians(t))) for t in range(-90, 91, 10)], w=7)
    p.line([(1110, 650), (1130, 740)], w=7)
    p.line([(1190, 650), (1170, 740)], w=7)
    rect(p, 1120, 740, 1180, 790, w=8)
    p.save("orta", "lofi-airplane-001")


def ship():
    p = Pen()
    sun(p, 230, 240)
    cloud(p, 1120, 250)
    for x, y in ((850, 300), (960, 380)):                                       # martılar
        p.arc(x - 30, y, 30, 22, 200, 340, w=9)
        p.arc(x + 30, y, 30, 22, 200, 340, w=9)
    rect(p, 690, 300, 720, 1010)                                                    # direk
    p.poly([(720, 310), (820, 340), (720, 370)], fill=255)                          # bayrak
    # çizgili yelkenler
    big = [(735, 400), (735, 960), (1150, 960)]
    p.poly(big, fill=255)
    for k in (0.33, 0.66):
        y = 400 + (960 - 400) * k
        x1 = 735 + (1150 - 735) * k
        p.line([(735, y), (x1, y)], w=10)
    p.poly([(675, 470), (675, 960), (380, 960)], fill=255)
    p.line([(675, 720), (510, 840)], w=10)
    # tekne
    p.poly([(230, 1010), (1220, 1010), (1100, 1200), (350, 1200)], fill=255)
    rect(p, 250, 1040, 1200, 1070, w=9)
    for x in (480, 640, 800, 960):
        p.circle(x, 1120, 30, w=9)
    waves(p, 1200, x0=60, x1=1340, depth=330)
    # su altında balıklar
    for x, y in ((300, 1400), (900, 1440)):
        obj_fish(p, x, y, 80)
    # deniz feneri
    p.poly([(1150, 1200), (1290, 1200), (1260, 700), (1180, 700)], fill=255)
    for y in (850, 1000):
        p.line([(1150 + (1200 - y) * 0.06, y), (1290 - (1200 - y) * 0.06, y)], w=9)
    rect(p, 1160, 620, 1280, 700, w=10)
    p.poly([(1150, 620), (1220, 540), (1290, 620)], fill=255, w=10)
    rect(p, 1195, 640, 1245, 690, w=7)
    p.ellipse(1220, 1210, 120, 40, fill=255, w=10)
    for x, y in ((600, 1480), (1100, 1380)):
        obj_fish(p, x, y, 70)
    p.save("orta", "lofi-ship-001")


def tractor():
    p = Pen()
    sun(p, 1170, 230)
    cloud(p, 420, 250)
    # ambar
    p.poly([(100, 620), (300, 450), (500, 620)], fill=255)
    rect(p, 120, 620, 480, 1000)
    rect(p, 230, 780, 370, 1000, w=11)
    p.line([(230, 780), (370, 1000)], w=10)
    p.line([(370, 780), (230, 1000)], w=10)
    p.circle(300, 560, 40, w=10)
    # çit
    for x in range(520, 1270, 90):
        p.poly([(x, 920), (x + 40, 920), (x + 40, 1060), (x, 1060)], fill=255, w=9)
    rect(p, 500, 950, 1270, 975, w=8)
    p.poly(curve([(130, 1250), (700, 1180), (1270, 1250), (1270, 1500), (130, 1500)], 8), fill=255)   # tarla
    for y in (1300, 1380):
        p.line([(120, y), (1280, y - 30)], w=8)
    # traktör
    p.poly([(560, 930), (900, 930), (900, 1150), (560, 1150)], fill=255)          # gövde
    p.poly([(900, 760), (1180, 760), (1180, 1150), (900, 1150)], fill=255)        # kabin
    rect(p, 940, 800, 1140, 940, w=10)
    rect(p, 620, 850, 660, 930, w=9)                                                # egzoz
    p.circle(640, 820, 30, w=9)
    for x in range(600, 880, 60):
        p.line([(x, 980), (x, 1100)], w=8)                                          # ızgara
    wheel(p, 1090, 1200, 150)
    wheel(p, 660, 1240, 95)
    for x in (220, 420, 820, 1020, 1220):
        for y in (1310, 1390):
            leaf(p, x, y, -60, 60, 20, w=7)
            leaf(p, x, y, -120, 60, 20, w=7)
    for x in (250, 440):                                                        # saman balyaları
        p.circle(x, 1170, 70, fill=255, w=10)
        p.circle(x, 1170, 40, w=8)
        p.circle(x, 1170, 14, w=6)
    p.save("orta", "lofi-tractor-001")


# ---------------------------------------------------------------- sebzeler

def carrot(p, x, y, ang=0, L=320):
    ca, sa = math.cos(math.radians(ang)), math.sin(math.radians(ang))
    def T(u, v): return (x + u * ca - v * sa, y + u * sa + v * ca)
    p.poly([T(0, -55), T(L, 0), T(0, 55)], fill=255)
    for u in (80, 160, 230):
        p.line([T(u, -40 + u * 0.12), T(u, -10)], w=8)
    for dv in (-30, 0, 30):
        leaf(p, *T(-10, dv), ang + 180 + dv, 110, 28, w=9)


def veg_basket():
    p = Pen()
    # sepetin arkasındaki sebzeler
    carrot(p, 330, 640, 60)
    carrot(p, 1080, 600, 115)
    # mısır (taneli)
    p.poly(curve([(700, 300), (790, 420), (800, 800), (600, 800), (610, 420)], 10), fill=255)
    for row in range(9):
        y = 400 + row * 45
        for col in range(4):
            p.ellipse(640 + col * 40, y, 18, 20, w=7)
    leaf(p, 600, 800, -110, 330, 60)
    leaf(p, 800, 800, -70, 330, 60)
    # patlıcan
    p.poly(curve([(420, 640), (520, 700), (560, 900), (440, 980), (360, 860)], 10), fill=255)
    p.poly([(400, 640), (480, 600), (450, 690)], fill=255)
    # domates
    p.circle(950, 820, 150, fill=255)
    for i in range(5):
        a = -math.pi / 2 + i * 2 * math.pi / 5
        p.poly([(950, 690), (950 + math.cos(a) * 70, 690 + math.sin(a) * 30 - 10), (950 + math.cos(a + 0.4) * 30, 690 + math.sin(a + 0.4) * 15)], w=8)
    # brokoli
    for dx, dy, r in ((-60, 0, 70), (40, -30, 80), (120, 20, 60), (30, 50, 60)):
        p.circle(560 + dx, 760 + dy, r, fill=255, w=11)
    # örgülü sepet
    p.poly([(180, 880), (1220, 880), (1080, 1400), (320, 1400)], fill=255)
    for k in range(1, 6):
        y = 880 + k * 87
        x0 = 180 + (320 - 180) * k * 87 / 520
        x1 = 1220 - (1220 - 1080) * k * 87 / 520
        p.line([(x0, y), (x1, y)], w=9)
    for k in range(1, 8):
        xt = 180 + k * 1040 / 8
        xb = 320 + k * 760 / 8
        p.line([(xt, 880), (xb, 1400)], w=9)
    p.poly(rrect(150, 840, 1250, 920, 30), fill=255)                              # sepet ağzı
    p.save("orta", "lofi-vegbasket-001")


def veg_garden():
    p = Pen()
    sun(p, 1180, 220)
    cloud(p, 300, 220)
    for x, y in ((620, 330), (900, 420)):                                       # kelebekler
        p.ellipse(x - 30, y, 30, 40, w=9)
        p.ellipse(x + 30, y, 30, 40, w=9)
        p.line([(x, y - 35), (x, y + 35)], w=10)
    # tarhlar
    for row, y in enumerate((700, 950, 1200)):
        p.poly(rrect(120, y, 1280, y + 160, 40), fill=255)
        for i in range(5):
            x = 260 + i * 220
            if row == 0:                                                         # havuçlar (yaprak üstte)
                for dv in (-25, 0, 25):
                    leaf(p, x, y + 20, -90 + dv, 110, 26, w=8)
                p.ellipse(x, y + 60, 40, 30, fill=255, w=10)
            elif row == 1:                                                       # lahanalar
                p.circle(x, y + 50, 75, fill=255)
                p.circle(x, y + 50, 40, w=9)
            else:                                                                # turplar
                p.ellipse(x, y + 60, 50, 45, fill=255)
                leaf(p, x, y + 20, -110, 90, 25, w=8)
                leaf(p, x, y + 20, -70, 90, 25, w=8)
    # sulama kabı
    p.poly([(980, 1420), (1200, 1420), (1180, 1540), (1000, 1540)], fill=255)
    p.poly([(980, 1440), (860, 1380), (850, 1400), (980, 1470)], fill=255)
    p.arc(1090, 1420, 70, 60, 180, 360, w=11)
    p.save("orta", "lofi-garden-001")


# ---------------------------------------------------------------- meslekler, alışkanlıklar, duygular...

def arm(p, x0, y0, x1, y1, s=1.0, hand=True):
    """Omuzdan ele kol (kapalı şerit) + yuvarlak el."""
    mx, my = (x0 + x1) / 2 + 20 * s, (y0 + y1) / 2
    p.poly(band(open_curve([(x0, y0), (mx, my), (x1, y1)], 6), 34 * s, 30 * s), fill=255)
    if hand:
        p.circle(x1, y1, 36 * s, fill=255)


def tiles(p, x0, y0, x1, y1, step=110):
    rect(p, x0, y0, x1, y1, w=9)
    for x in range(int(x0) + step, int(x1), step):
        p.line([(x, y0), (x, y1)], w=7)
    for y in range(int(y0) + step, int(y1), step):
        p.line([(x0, y), (x1, y)], w=7)


def doctor():
    p = Pen()
    # duvar: saat ve kalp tablosu (resmi dikeyleştirir)
    p.circle(1050, 280, 110, fill=255)
    p.circle(1050, 280, 85, w=8)
    p.line([(1050, 280), (1050, 215)], w=10)
    p.line([(1050, 280), (1100, 300)], w=10)
    for k in range(12):
        a = math.radians(k * 30)
        p.dot(1050 + math.cos(a) * 70, 280 + math.sin(a) * 70, 6)
    p.poly(rrect(200, 200, 460, 480, 20), fill=255)
    p.poly(rrect(230, 230, 430, 450, 12), w=8)
    obj_heart(p, 330, 340, 70)
    p.line([(60, 1290), (1340, 1290)], w=12)                                   # zemin
    # çanta
    p.poly(rrect(160, 1000, 470, 1280, 30), fill=255)
    p.arc(315, 1000, 80, 70, 180, 360, w=14)
    p.poly([(290, 1080), (340, 1080), (340, 1120), (380, 1120), (380, 1170), (340, 1170), (340, 1210), (290, 1210),
            (290, 1170), (250, 1170), (250, 1120), (290, 1120)], fill=255, w=10)
    # bandajlı oyuncak ayı
    for x in (1010, 1190):
        p.circle(x, 860, 45)
    p.ellipse(1100, 1140, 140, 150, fill=255)
    p.circle(1100, 930, 120, fill=255)
    eyes(p, 1100, 920, 40, 12)
    p.ellipse(1100, 975, 30, 20)
    p.poly([(1020, 860), (1180, 820), (1190, 860), (1030, 900)], fill=255, w=9)  # bandaj
    p.ellipse(1100, 1170, 70, 80, w=10)
    for x in (1010, 1190):
        p.ellipse(x, 1270, 50, 25, fill=255)
    # doktor çocuk
    by, bb = kid(p, 700, 330, 1.25, hair="bun")
    coat = [(700 - 125, by - 5), (700 + 125, by - 5), (700 + 200, bb + 40), (700 - 200, bb + 40)]
    p.poly(coat, fill=255)
    p.line([(700, by), (700, bb + 40)], w=10)
    p.poly([(700 - 120, by), (700, by + 140), (700 + 120, by)], fill=255, w=10)  # yaka
    for y in (by + 200, by + 280):
        p.circle(730, y, 12, w=7)
    rect(p, 520, by + 230, 620, by + 310, w=9)                                      # cep
    # steteskop
    p.line(open_curve([(620, by + 20), (600, by + 140), (650, by + 230), (760, by + 240), (800, by + 150), (780, by + 20)], 8), w=10)
    p.circle(650, by + 250, 30, fill=255, w=9)
    arm(p, 700 - 120, by + 30, 520, by + 330, 1.2)
    arm(p, 700 + 120, by + 30, 880, by + 330, 1.2)
    p.save("orta", "lofi-doctor-001")


def firefighter():
    p = Pen()
    cloud(p, 300, 220)
    # yanan değil, sadece dumanı tüten bina (korkutmasın)
    rect(p, 880, 320, 1300, 1300)
    for r in range(3):
        for c in range(2):
            rect(p, 930 + c * 200, 400 + r * 230, 1050 + c * 200, 540 + r * 230, w=10)
    rect(p, 1030, 1110, 1150, 1300, w=10)
    for x, y, rr in ((1000, 330, 40), (930, 250, 55), (850, 160, 70)):
        p.circle(x, y, rr, fill=255, w=10)
    p.line([(60, 1300), (1340, 1300)], w=12)
    # yangın musluğu
    p.poly(rrect(160, 1060, 300, 1300, 20), fill=255)
    p.poly(curve([(150, 1070), (230, 990), (310, 1070)], 8), fill=255)
    p.circle(230, 1150, 30, w=9)
    # hortum
    hose = open_curve([(300, 1180), (420, 1250), (540, 1160), (600, 960)], 8)
    p.poly(band(hose, 22, 22), fill=255, w=10)
    # itfaiyeci çocuk
    by, bb = kid(p, 560, 380, 1.15)
    p.poly([(560 - 120, by - 5), (560 + 120, by - 5), (560 + 180, bb + 30), (560 - 180, bb + 30)], fill=255)   # ceket
    rect(p, 560 - 175, bb - 60, 560 + 175, bb - 25, w=9)                              # şerit
    rect(p, 560 - 160, by + 120, 560 + 160, by + 150, w=9)
    # kask
    r = 140 * 1.15
    hy = 380 + r
    dome = [(560 + math.cos(math.radians(a)) * r * 1.12, hy - 10 + math.sin(math.radians(a)) * r * 1.12) for a in range(185, 356, 10)]
    p.poly(dome, fill=255)
    p.ellipse(560, hy - 20, r * 1.35, 30, fill=255)
    p.poly(rrect(525, hy - r * 0.95, 595, hy - r * 0.55, 10), fill=255, w=9)       # arma
    p.poly(rrect(600, by + 200, 790, by + 245, 15), fill=255, w=10)               # hortum ucu
    for x, y in ((830, by + 200), (860, by + 150), (850, by + 250), (875, by + 205)):
        p.poly(curve([(x - 25, y), (x + 5, y - 18), (x + 30, y), (x + 5, y + 18)], 6), w=8)   # su damlaları
    arm(p, 560 - 110, by + 30, 610, by + 225, 1.1)
    arm(p, 560 + 110, by + 30, 720, by + 220, 1.1)
    p.save("orta", "lofi-firefighter-001")


def chef():
    p = Pen()
    tiles(p, 60, 200, 1340, 1000, 160)                                        # mutfak duvarı
    # aşçı çocuk (tezgahın arkasında)
    by, bb = kid(p, 640, 300, 1.05)
    p.poly(rrect(560, by + 60, 720, 1000, 20), fill=255, w=10)                     # önlük
    rect(p, 600, by + 170, 680, by + 220, w=8)
    r = 140 * 1.05
    hy = 300 + r
    for dx, dy, rr in ((-80, -170, 75), (0, -210, 85), (80, -170, 75)):          # aşçı şapkası
        p.circle(640 + dx, hy + dy, rr, fill=255, w=11)
    rect(p, 640 - 115, hy - 150, 640 + 115, hy - 90, w=11)
    arm(p, 640 - 100, by + 30, 500, by + 230, 1.0)
    arm(p, 640 + 100, by + 30, 840, by + 170, 1.0)                                 # kaşığı tutan kol
    # tezgah ve dolaplar (çocuğun önünde)
    rect(p, 60, 1000, 1340, 1090)
    rect(p, 100, 1090, 1300, 1450)
    for x in (100, 500, 900):
        rect(p, x + 20, 1120, x + 380, 1420, w=10)
        p.circle(x + 200, 1180, 16, w=8)
    # tencere, kaşık, buhar
    p.poly(rrect(900, 830, 1220, 1000, 30), fill=255)
    rect(p, 880, 800, 1240, 840, w=10)
    for x in (900, 1220):
        p.ellipse(x, 880, 30, 20, w=9)
    p.line([(840, by + 170), (960, 780)], w=12)
    for x in (1030, 1110, 1180):
        p.line(open_curve([(x, 770), (x - 25, 690), (x + 15, 610), (x - 10, 550)], 6), w=9)
    # tezgahtaki sebzeler
    carrot(p, 160, 960, 0, 230)
    p.circle(450, 945, 55, fill=255)
    p.poly([(450, 890), (470, 870), (440, 875)], w=8)
    p.save("orta", "lofi-chef-001")

def astronaut():
    p = Pen()
    for x, y, r in ((200, 250, 40), (1200, 330, 35), (420, 520, 25), (1050, 700, 30), (180, 820, 30), (1250, 950, 40)):
        obj_star(p, x, y, r)
    p.circle(1120, 240, 110, fill=255)                                         # halkalı gezegen
    p.ellipse(1120, 240, 190, 40, w=10)
    p.circle(260, 600, 80, fill=255)                                           # küçük gezegen
    p.arc(260, 600, 80, 30, 0, 180, w=8)
    # ay yüzeyi ve kraterler
    p.poly(curve([(130, 1300), (700, 1220), (1270, 1300), (1270, 1540), (130, 1540)], 8), fill=255)
    for x, y, r in ((250, 1380, 50), (600, 1440, 35), (1000, 1270, 60), (1200, 1460, 30)):
        p.ellipse(x, y, r, r * 0.5, w=9)
    # bayrak
    rect(p, 1040, 860, 1055, 1280)
    p.poly([(1055, 870), (1230, 900), (1055, 960)], fill=255)
    obj_star(p, 1120, 910, 28)
    # sırt çantası
    p.poly(rrect(470, 740, 930, 1110, 40), fill=255)
    # astronot
    by, bb = kid(p, 700, 320, 1.1)
    p.poly(rrect(700 - 170, by - 10, 700 + 170, bb + 20, 50), fill=255)              # tulum
    p.poly(rrect(620, by + 90, 780, by + 200, 15), fill=255, w=10)                     # kontrol paneli
    for i, x in enumerate((650, 700, 750)):
        p.circle(x, by + 145, 18, w=7)
    arm(p, 700 - 160, by + 40, 470, by + 260, 1.3)
    arm(p, 700 + 160, by + 40, 960, by - 40, 1.3)                                  # el sallıyor
    p.circle(700, 320 + 154, 215, w=14)                                          # kask camı
    for x, y, r in ((620, 220, 22), (850, 380, 18), (330, 1000, 22), (1260, 650, 20), (900, 1120, 22)):
        obj_star(p, x, y, r)
    p.save("orta", "lofi-astronaut-001")


def toothbrush():
    p = Pen()
    tiles(p, 60, 160, 1340, 1000, 150)                                         # banyo fayansı
    p.poly(rrect(130, 260, 470, 720, 50), fill=255)                            # ayna (solda)
    p.poly(rrect(165, 295, 435, 685, 35), w=9)
    p.line([(220, 360), (300, 440)], w=9)
    rect(p, 1000, 560, 1290, 590, w=9)                                           # raf
    p.poly([(1060, 440), (1180, 440), (1165, 560), (1075, 560)], fill=255)        # bardak
    p.line([(1100, 450), (1080, 330)], w=12)
    p.line([(1140, 450), (1170, 340)], w=12)
    # çocuk
    cx, s_ = 760, 1.15
    by, bb = kid(p, cx, 290, s_, hair="short", mood="none", body=False)
    hy = 290 + 140 * s_
    p.poly([(cx - 90 * s_, by - 10), (cx + 90 * s_, by - 10), (cx + 160, by + 40), (cx + 175, 1000), (cx - 175, 1000), (cx - 160, by + 40)], fill=255)
    p.line([(cx - 60, by + 10), (cx, by + 70), (cx + 60, by + 10)], w=9)          # yaka
    mouth_y = hy + 82
    arm(p, cx + 150, by + 50, cx + 200, mouth_y + 20, 1.15)
    p.poly(rrect(cx - 20, mouth_y - 14, cx + 200, mouth_y + 14, 12), fill=255, w=9)   # diş fırçası
    rect(p, cx - 20, mouth_y - 34, cx + 40, mouth_y - 14, w=7)                     # fırça kılları
    for x, y, r in ((cx - 45, mouth_y + 5, 16), (cx - 65, mouth_y - 20, 11), (cx - 50, mouth_y + 32, 10)):
        p.circle(x, y, r, w=7)                                                  # köpük
    # lavabo (önde)
    p.poly(rrect(160, 960, 1240, 1080, 40), fill=255)
    p.poly([(240, 1080), (1160, 1080), (1060, 1480), (340, 1480)], fill=255)
    rect(p, 480, 1180, 920, 1420, w=10)
    p.circle(700, 1300, 16, w=8)
    p.poly([(220, 900), (420, 880), (430, 950), (220, 960)], fill=255)            # diş macunu
    rect(p, 430, 895, 470, 930, w=9)
    p.save("orta", "lofi-toothbrush-001")

def handwash():
    p = Pen()
    tiles(p, 60, 160, 1340, 900, 160)
    # havlu askısı ve havlu
    rect(p, 1040, 300, 1300, 330, w=10)
    p.poly(rrect(1080, 320, 1260, 700, 20), fill=255)
    for y in (580, 620):
        p.line([(1080, y), (1260, y)], w=8)
    # lavabo
    p.poly(rrect(120, 880, 1280, 1000, 40), fill=255)
    p.poly([(200, 1000), (1200, 1000), (1100, 1480), (300, 1480)], fill=255)
    rect(p, 450, 1120, 950, 1420, w=10)
    # musluk ve su
    p.poly(rrect(600, 520, 700, 880, 20), fill=255)
    p.poly(rrect(600, 520, 860, 600, 30), fill=255)
    for x in (650, 760):
        p.ellipse(x, 500, 40, 20, fill=255, w=9)
    for i in range(4):
        p.line([(800 + i * 15, 610), (790 + i * 15, 760)], w=8)
    # sabun
    p.poly(rrect(200, 800, 380, 880, 30), fill=255)
    p.ellipse(290, 840, 50, 20, w=8)
    # köpüklü eller
    for x, flip in ((640, -1), (900, 1)):
        p.poly(curve([(x - 70, 860), (x - 80, 740), (x - 40, 690), (x + 40, 700), (x + 70, 760), (x + 60, 860)], 8), fill=255)
        p.poly(curve([(x + flip * 60, 760), (x + flip * 120, 720), (x + flip * 130, 770), (x + flip * 70, 800)], 6), fill=255)
        p.poly([(x - 75, 860), (x + 65, 860), (x + 70, 960), (x - 80, 960)], fill=255)   # bilek/kol
    for x, y, r in ((560, 680, 35), (620, 630, 25), (980, 660, 30), (1020, 610, 20), (760, 420, 40), (870, 380, 28), (480, 560, 22)):
        p.circle(x, y, r, fill=255, w=9)
        p.arc(x - r * 0.3, y - r * 0.3, r * 0.4, r * 0.4, 200, 280, w=6)
    p.save("orta", "lofi-handwash-001")


def feelings():
    p = Pen()
    cards = [(160, 220, "happy"), (740, 220, "sad"), (160, 870, "angry"), (740, 870, "surprised")]
    for x0, y0, mood in cards:
        p.poly(rrect(x0, y0, x0 + 500, y0 + 560, 50), fill=255)
        cx, cy = x0 + 250, y0 + 280
        p.circle(cx, cy, 190, fill=255)
        # saç tutamı
        p.poly(curve([(cx - 40, cy - 185), (cx - 10, cy - 250), (cx + 40, cy - 240), (cx + 20, cy - 188)], 6), fill=255)
        ey = cy - 30
        if mood == "happy":
            for dx in (-70, 70):
                p.arc(cx + dx, ey + 10, 30, 25, 200, 340, w=12)
            p.poly([(cx - 90, cy + 50)] + [(cx + math.cos(math.radians(a)) * 90, cy + 50 + math.sin(math.radians(a)) * 80) for a in range(0, 181, 15)], fill=255, w=12)
        elif mood == "sad":
            eyes(p, cx, ey, 70, 20)
            for dx, sgn in ((-70, 1), (70, -1)):
                p.line([(cx + dx - 35, ey - 45 - sgn * 10), (cx + dx + 35, ey - 45 + sgn * 10)], w=10)
            p.arc(cx, cy + 120, 70, 50, 200, 340, w=12)
            p.poly(curve([(cx + 85, ey + 40), (cx + 105, ey + 90), (cx + 85, ey + 110), (cx + 65, ey + 90)], 6), w=8)   # gözyaşı
        elif mood == "angry":
            eyes(p, cx, ey + 10, 70, 20)
            for dx, sgn in ((-70, -1), (70, 1)):
                p.line([(cx + dx - 40, ey - 35 + sgn * 15), (cx + dx + 40, ey - 35 - sgn * 15)], w=12)
            p.line([(cx - 60, cy + 90), (cx + 60, cy + 90)], w=12)
        else:
            for dx in (-70, 70):
                p.circle(cx + dx, ey, 26, fill=255, w=10)
                p.dot(cx + dx, ey, 11)
                p.arc(cx + dx, ey - 55, 35, 20, 200, 340, w=10)
            p.ellipse(cx, cy + 90, 40, 55, fill=255, w=12)
        if mood in ("happy", "surprised"):
            for dx in (-120, 120):
                p.ellipse(cx + dx, cy + 40, 30, 18, w=8)
    for x0, y0, mood in cards:
        for cx2, cy2 in ((x0 + 60, y0 + 60), (x0 + 440, y0 + 60), (x0 + 60, y0 + 500), (x0 + 440, y0 + 500)):
            obj_star(p, cx2, cy2, 30)
        obj_heart(p, x0 + 250, y0 + 520, 22)
    p.save("orta", "lofi-feelings-001")


def pencil_obj(p, x, y, ang, L=360, w=46):
    ca, sa = math.cos(math.radians(ang)), math.sin(math.radians(ang))
    def T(u, v): return (x + u * ca - v * sa, y + u * sa + v * ca)
    p.poly([T(0, -w / 2), T(L, -w / 2), T(L, w / 2), T(0, w / 2)], fill=255, w=10)
    p.poly([T(L, -w / 2), T(L + 90, 0), T(L, w / 2)], fill=255, w=10)
    p.line([T(L + 60, -w / 6), T(L + 60, w / 6)], w=8)
    p.poly([T(-50, -w / 2), T(0, -w / 2), T(0, w / 2), T(-50, w / 2)], fill=255, w=10)   # silgi


def school():
    p = Pen()
    # sırt çantası
    p.poly(rrect(380, 330, 1020, 1180, 90), fill=255)
    p.arc(700, 340, 150, 120, 180, 360, w=26)                                   # sap
    p.poly(rrect(450, 420, 950, 640, 60), fill=255)                             # üst kapak
    p.line([(470, 560), (930, 560)], w=9)
    p.poly(rrect(470, 760, 930, 1100, 50), fill=255)                            # ön cep
    p.line([(500, 840), (900, 840)], w=9)
    p.poly(rrect(680, 820, 720, 890, 10), fill=255, w=8)                        # fermuar
    obj_star(p, 700, 970, 70)
    # kalemler, cetvel, kitap, silgi, elma
    pencil_obj(p, 120, 1300, -10, 360)
    pencil_obj(p, 150, 1420, -10, 320)
    rect(p, 760, 1260, 1290, 1340, w=11)                                         # cetvel
    for i in range(1, 12):
        x = 760 + i * 44
        p.line([(x, 1260), (x, 1260 + (40 if i % 2 == 0 else 22))], w=7)
    p.poly([(1060, 360), (1300, 400), (1260, 800), (1020, 760)], fill=255)       # kitap
    p.poly([(1020, 760), (1260, 800), (1250, 840), (1015, 800)], fill=255, w=10)
    rect(p, 1080, 480, 1220, 560, w=9)
    obj_apple(p, 220, 800, 120)
    p.poly(rrect(160, 1060, 330, 1160, 20), fill=255)                           # silgi
    p.line([(230, 1060), (230, 1160)], w=9)
    # pastel boya kutusu
    p.poly([(400, 1220), (700, 1220), (700, 1500), (400, 1500)], fill=255)
    for i in range(5):
        x = 420 + i * 55
        p.poly([(x, 1220), (x + 40, 1220), (x + 40, 1100), (x + 20, 1060), (x, 1100)], fill=255, w=8)
        p.line([(x, 1100), (x + 40, 1100)], w=7)
        p.line([(x, 1170), (x + 40, 1170)], w=7)
    p.poly(rrect(440, 1300, 660, 1420, 20), w=8)
    # çanta kapağında puantiyeler
    for x in (520, 610, 790, 880):
        p.circle(x, 500, 18, w=7)
    p.save("orta", "lofi-school-001")


def instruments():
    p = Pen()
    # notalar
    for x, y in ((250, 250), (1150, 300), (700, 200)):
        p.ellipse(x, y + 80, 40, 30, fill=0)
        p.line([(x + 36, y + 80), (x + 36, y - 40)], w=12)
        p.line([(x + 36, y - 40), (x + 90, y - 10)], w=12)
    # davul
    p.ellipse(380, 650, 230, 70, fill=255)
    p.poly([(150, 650), (610, 650), (610, 950), (150, 950)], fill=255)
    p.ellipse(380, 950, 230, 70, fill=255)
    p.ellipse(380, 650, 230, 70, fill=255)
    for i in range(6):                                                         # çapraz ipler
        x0 = 170 + i * 80
        p.line([(x0, 700), (x0 + 40, 900)], w=8)
        p.line([(x0 + 40, 700), (x0, 900)], w=8)
    for dx in (-1, 1):                                                         # bagetler
        p.line([(380 + dx * 60, 600), (380 + dx * 220, 450)], w=14)
        p.circle(380 + dx * 230, 440, 24, fill=255, w=9)
    # gitar
    p.poly(rrect(985, 300, 1055, 820, 20), fill=255)
    p.poly(rrect(965, 220, 1075, 330, 25), fill=255)
    for y in (250, 300):
        p.circle(955, y, 14, w=7)
        p.circle(1085, y, 14, w=7)
    p.circle(1020, 960, 190, fill=255)
    p.circle(1020, 780, 140, fill=255)
    p.circle(1020, 870, 60, w=10)
    rect(p, 960, 1030, 1080, 1060, w=8)
    for dx in (-20, 0, 20):
        p.line([(1020 + dx, 330), (1020 + dx, 1045)], w=5)
    # ksilofon
    for i in range(8):
        h = 260 - i * 20
        x0 = 180 + i * 90
        p.poly(rrect(x0, 1200 - h / 2, x0 + 70, 1200 + h / 2, 15), fill=255, w=10)
        p.circle(x0 + 35, 1200 - h / 2 + 25, 9, w=6)
        p.circle(x0 + 35, 1200 + h / 2 - 25, 9, w=6)
    p.line([(160, 1110), (900, 1150)], w=12)
    p.line([(160, 1290), (900, 1250)], w=12)
    # marakaslar
    for x, a in ((1080, -20), (1230, 20)):
        p.ellipse(x, 1200, 70, 90, fill=255)
        p.line([(x, 1290), (x + a * 3, 1460)], w=22)
        p.arc(x, 1200, 50, 60, 200, 340, w=8)
    p.save("orta", "lofi-instruments-001")


def playground():
    p = Pen()
    sun(p, 230, 230)
    cloud(p, 900, 220)
    p.poly(curve([(130, 1260), (700, 1200), (1270, 1260), (1270, 1520), (130, 1520)], 8), fill=255)   # çim
    # salıncak
    p.line([(200, 1260), (290, 560), (380, 1260)], w=18)
    p.line([(560, 1260), (650, 560), (740, 1260)], w=18)
    rect(p, 250, 545, 690, 580, w=12)
    for x in (400, 540):
        p.line([(x - 40, 580), (x - 40, 1040)], w=8)
        p.line([(x + 40, 580), (x + 40, 1040)], w=8)
        rect(p, x - 60, 1040, x + 60, 1075, w=10)
    # kaydırak
    for x in (1170, 1260):
        p.line([(x, 1260), (x, 680)], w=16)
    for y in range(760, 1260, 90):
        p.line([(1170, y), (1260, y)], w=10)
    rect(p, 1130, 650, 1300, 690, w=10)
    p.poly(curve([(1130, 690), (1060, 760), (930, 1100), (840, 1240), (900, 1260), (980, 1150), (1110, 820), (1170, 720)], 8), fill=255)
    # kum havuzu, kova, top
    p.poly(rrect(170, 1330, 720, 1470, 30), fill=255)
    p.poly([(250, 1320), (360, 1320), (340, 1420), (270, 1420)], fill=255)
    p.arc(305, 1320, 50, 50, 180, 360, w=9)
    p.circle(1100, 1380, 80, fill=255)
    p.arc(1100, 1380, 80, 30, 0, 180, w=8)
    p.line([(1100, 1300), (1100, 1460)], w=8)
    for x in (620, 760):
        obj_flower(p, x, 1300, 40)
    for x in (820, 1260):
        obj_flower(p, x, 1430, 45)
    p.save("orta", "lofi-playground-001")


def autumn():
    p = Pen()
    cloud(p, 1100, 220)
    p.poly(curve([(130, 1300), (700, 1250), (1270, 1300), (1270, 1520), (130, 1520)], 8), fill=255)   # zemin
    # gövde ve dallar
    p.poly([(620, 1280), (780, 1280), (740, 800), (660, 800)], fill=255)
    p.poly(band([(700, 900), (560, 760), (430, 650)], 30, 18), fill=255)
    p.poly(band([(710, 880), (860, 740), (980, 640)], 30, 18), fill=255)
    p.ellipse(700, 1100, 40, 60, w=9)                                           # kovuk
    crown = [(700 + math.cos(math.radians(a)) * (470 + 40 * math.sin(math.radians(a) * 6)), 560 + math.sin(math.radians(a)) * (370 + 30 * math.sin(math.radians(a) * 6))) for a in range(0, 360, 6)]
    p.poly(crown, fill=255)                                                     # yaprak tacı
    # yaprak tacı: halka halka yapraklar
    import random as _r
    rnd = _r.Random(7)
    for ring, (R, n) in enumerate(((270, 16), (170, 10), (70, 5))):
        for i in range(n):
            a = i * 2 * math.pi / n + ring * 0.3
            x, y = 700 + math.cos(a) * R * 1.25, 560 + math.sin(a) * R
            leaf(p, x, y, math.degrees(a) + rnd.randint(-40, 40), 110, 40, w=9)
    # düşen yapraklar ve yerdekiler
    for x, y, a in ((250, 900, 30), (1150, 1000, -40), (300, 1180, 80), (1080, 1230, 10), (900, 1380, -20), (420, 1400, 60)):
        leaf(p, x, y, a, 110, 40, w=9)
    # sepet ve mantarlar
    for x in (220, 1200):
        p.poly([(x - 25, 1330), (x + 25, 1330), (x + 20, 1260), (x - 20, 1260)], fill=255, w=9)
        p.poly(curve([(x - 70, 1265), (x - 40, 1200), (x + 40, 1200), (x + 70, 1265)], 8), fill=255, w=9)
    p.save("orta", "lofi-autumn-001")


def rainbow():
    p = Pen()
    # gökkuşağı bantları
    cx, cy = 700, 900
    for k, R in enumerate((620, 540, 460, 380, 300, 230)):
        p.arc(cx, cy, R, R, 180, 360, w=12)
    p.line([(cx - 620, cy), (cx - 230, cy)], w=12)
    p.line([(cx + 230, cy), (cx + 620, cy)], w=12)
    cloud(p, 250, 880, 1.0)
    cloud(p, 1150, 880, 1.0)
    # damlalar
    for x, y in ((160, 1020), (230, 1100), (110, 1130), (1240, 1020), (1180, 1100), (1300, 1120)):
        p.poly(curve([(x, y - 30), (x + 20, y + 5), (x, y + 25), (x - 20, y + 5)], 6), w=8)
    # şemsiye
    p.poly([(700 + math.cos(math.radians(a)) * 300, 1150 + math.sin(math.radians(a)) * 180) for a in range(180, 361, 10)], fill=255)
    for dx in (-150, 0, 150):
        p.line([(700, 970), (700 + dx * 1.3, 1150)], w=9)
    p.line([(700, 1150), (700, 1380)], w=14)
    p.arc(650, 1380, 50, 50, 0, 180, w=14)
    # su birikintisi ve botlar
    p.ellipse(1000, 1450, 230, 60, fill=255)
    for x in (930, 1080):
        p.poly([(x - 40, 1250), (x + 40, 1250), (x + 40, 1420), (x + 90, 1420), (x + 90, 1460), (x - 40, 1460)], fill=255, w=11)
        p.line([(x - 40, 1300), (x + 40, 1300)], w=8)
    for x in (150, 300, 450):
        p.line([(x, 1460), (x, 1330)], w=9)
        obj_flower(p, x, 1290, 60)
        leaf(p, x, 1410, -40, 70, 22, w=7)
    p.save("orta", "lofi-rainbow-001")


def toys():
    p = Pen()
    # oyuncak kutusu
    p.poly([(250, 900), (1150, 900), (1150, 1450), (250, 1450)], fill=255)
    for y in (1040, 1180, 1320):
        p.line([(250, y), (1150, y)], w=9)
    p.poly([(230, 860), (1170, 860), (1170, 920), (230, 920)], fill=255)
    obj_heart(p, 700, 1110, 80)
    # kutudan taşan oyuncaklar
    # robot
    p.poly(rrect(300, 520, 520, 700, 25), fill=255)
    p.poly(rrect(270, 700, 550, 880, 25), fill=255)
    p.line([(410, 520), (410, 450)], w=10)
    p.circle(410, 440, 22, w=9)
    for x in (360, 460):
        p.circle(x, 600, 28, fill=255, w=9)
        p.dot(x, 600, 10)
    rect(p, 370, 650, 450, 670, w=8)
    for x in (340, 410, 480):
        p.circle(x, 790, 18, w=7)
    # top
    p.circle(700, 700, 160, fill=255)
    p.arc(700, 700, 160, 60, 0, 180, w=10)
    p.arc(700, 700, 60, 160, 270, 450, w=10)
    # küpler
    for x, y, sh in ((900, 640, "star"), (1060, 640, "heart"), (980, 490, "circle")):
        p.poly(rrect(x - 70, y - 70, x + 70, y + 70, 15), fill=255)
        if sh == "star": obj_star(p, x, y, 45)
        elif sh == "heart": obj_heart(p, x, y, 40)
        else: p.circle(x, y, 40, w=10)
    # yerde: oyuncak araba ve ördek
    p.poly(rrect(80, 1470, 420, 1540, 20), fill=255)
    p.poly([(140, 1470), (190, 1400), (320, 1400), (360, 1470)], fill=255)
    for x in (160, 340):
        wheel(p, x, 1540, 40)
    p.ellipse(1180, 1490, 110, 60, fill=255)
    p.circle(1240, 1410, 55, fill=255)
    p.poly([(1290, 1405), (1340, 1415), (1290, 1430)], w=9)
    p.dot(1250, 1400, 9)
    # uçurtma
    p.poly([(1150, 180), (1260, 300), (1150, 450), (1040, 300)], fill=255)
    p.line([(1150, 180), (1150, 450)], w=8)
    p.line([(1040, 300), (1260, 300)], w=8)
    p.line(open_curve([(1150, 450), (1100, 520), (1150, 600), (1100, 680)], 6), w=7)
    for x, y in ((1110, 520), (1145, 600), (1105, 680)):
        p.poly([(x - 25, y - 15), (x, y), (x - 25, y + 15)], w=6)
        p.poly([(x + 25, y - 15), (x, y), (x + 25, y + 15)], w=6)
    p.save("orta", "lofi-toys-001")


# ================================================================ ZOR SEVİYE (çok parçalı: ≈140+ bölge)
# Geleneksel desenler (geometrik yıldız, kilim, lale çinisi) ve kalabalık sahneler.

def frame(p, x0, y0, x1, y1, gap=40, w=12):
    rect(p, x0, y0, x1, y1, w=w)
    rect(p, x0 + gap, y0 + gap, x1 - gap, y1 - gap, w=w)


def star_tiles():
    """Sekiz köşeli yıldız (geometrik) desen: kare + 45° dönük kare, içinde sekizgen."""
    p = Pen()
    frame(p, 80, 120, 1320, 1480)
    for gx in range(4):
        for gy in range(5):
            cx, cy = 260 + gx * 293, 300 + gy * 250
            R = 120
            sq1 = [(cx + math.cos(math.radians(45 + 90 * k)) * R, cy + math.sin(math.radians(45 + 90 * k)) * R) for k in range(4)]
            sq2 = [(cx + math.cos(math.radians(90 * k)) * R, cy + math.sin(math.radians(90 * k)) * R) for k in range(4)]
            p.poly(sq1, w=10)
            p.poly(sq2, w=10)
            p.poly([(cx + math.cos(math.radians(22.5 + 45 * k)) * 52, cy + math.sin(math.radians(22.5 + 45 * k)) * 52) for k in range(8)], w=9)
            p.circle(cx, cy, 18, w=8)
    # yıldızların arasındaki küçük baklavalar
    for gx in range(3):
        for gy in range(4):
            cx, cy = 260 + gx * 293 + 146, 300 + gy * 250 + 125
            p.poly([(cx, cy - 55), (cx + 55, cy), (cx, cy + 55), (cx - 55, cy)], w=9)
    p.save("zor", "lofi-startiles-001")


def hex_flowers():
    """Altıgen petek içinde çiçekler."""
    p = Pen()
    frame(p, 80, 100, 1320, 1500)
    R = 118
    h = R * math.sqrt(3)
    for col in range(7):
        for row in range(7):
            cx = 262 + col * R * 1.5
            cy = 260 + row * h + (h / 2 if col % 2 else 0)
            if cx + R > 1300 or cy + h / 2 > 1480: continue
            hexa = [(cx + math.cos(math.radians(60 * k)) * R, cy + math.sin(math.radians(60 * k)) * R) for k in range(6)]
            p.poly(hexa, w=10)
            for k in range(6):
                a = math.radians(30 + 60 * k)
                p.circle(cx + math.cos(a) * 54, cy + math.sin(a) * 54, 25, w=8)
            p.circle(cx, cy, 22, w=8)
    p.save("zor", "lofi-hexflowers-001")


def kilim():
    """Kilim: kenar şeritleri, üçgen bordür, ortada iç içe baklavalar ve kancalar, saçaklar."""
    p = Pen()
    x0, y0, x1, y1 = 180, 160, 1220, 1440
    rect(p, x0, y0, x1, y1)
    for x in range(x0 + 20, x1, 40):                                            # saçaklar
        p.line([(x, y0), (x, y0 - 60)], w=8)
        p.line([(x, y1), (x, y1 + 60)], w=8)
    rect(p, x0 + 40, y0 + 40, x1 - 40, y1 - 40, w=10)
    rect(p, x0 + 110, y0 + 110, x1 - 110, y1 - 110, w=10)
    # bordürde üçgen sıraları
    def tri_row(ax, ay, bx, by, n, inward):
        for i in range(n):
            t0, t1 = i / n, (i + 1) / n
            px0, py0 = ax + (bx - ax) * t0, ay + (by - ay) * t0
            px1, py1 = ax + (bx - ax) * t1, ay + (by - ay) * t1
            mx, my = (px0 + px1) / 2 + inward[0] * 60, (py0 + py1) / 2 + inward[1] * 60
            p.poly([(px0, py0), (px1, py1), (mx, my)], w=8)
    tri_row(x0 + 40, y0 + 40, x1 - 40, y0 + 40, 13, (0, 1))
    tri_row(x0 + 40, y1 - 40, x1 - 40, y1 - 40, 13, (0, -1))
    tri_row(x0 + 40, y0 + 40, x0 + 40, y1 - 40, 16, (1, 0))
    tri_row(x1 - 40, y0 + 40, x1 - 40, y1 - 40, 16, (-1, 0))
    # orta alan: üç büyük baklava, iç içe
    cx = (x0 + x1) / 2
    for k, cy in enumerate((480, 800, 1120)):
        for s_, w in ((300, 12), (220, 10), (140, 9), (60, 8)):
            p.poly([(cx, cy - s_ * 0.55), (cx + s_, cy), (cx, cy + s_ * 0.55), (cx - s_, cy)], w=w)
        for sx in (-1, 1):                                                      # kancalar
            p.poly([(cx + sx * 300, cy), (cx + sx * 350, cy - 40), (cx + sx * 380, cy), (cx + sx * 350, cy + 40)], w=9)
    # köşelerde küçük baklavalar
    for x in (x0 + 190, x1 - 190):
        for y in (330, 640, 960, 1270):
            p.poly([(x, y - 50), (x + 40, y), (x, y + 50), (x - 40, y)], w=8)
            p.circle(x, y, 12, w=6)
    p.save("zor", "lofi-kilim-001")


def tulip(p, x, y, ang, L=170, w=10):
    """Osmanlı lalesi: sivri üç yapraklı çiçek + sap."""
    ca, sa = math.cos(math.radians(ang)), math.sin(math.radians(ang))
    def T(u, v): return (x + u * ca - v * sa, y + u * sa + v * ca)
    p.line([T(0, 0), T(L * 0.45, 0)], w=w)
    base = L * 0.45
    p.poly(curve([T(base, -38), T(base + 60, -55), T(L + 20, -40), T(base + 70, -10), T(L + 30, 0), T(base + 70, 10), T(L + 20, 40), T(base + 60, 55), T(base, 38)], 4), w=w)
    p.line([T(base + 10, 0), T(base + 75, 0)], w=w - 3)


def tulip_tile():
    """İznik çinisi: ortada rozet, dört yönde lale, saz yaprakları, motifli bordür."""
    p = Pen()
    frame(p, 90, 150, 1310, 1450, gap=70)
    cx, cy = 700, 800
    # bordür motifleri (küçük daire + yaprak)
    for i in range(10):
        x = 230 + i * 105
        p.circle(x, 185, 18, w=7)
        p.circle(x, 1415, 18, w=7)
    for i in range(11):
        y = 300 + i * 100
        p.circle(125, y, 18, w=7)
        p.circle(1275, y, 18, w=7)
    # orta rozet
    for r, w in ((170, 11), (120, 10), (60, 9)):
        p.circle(cx, cy, r, w=w)
    for k in range(12):
        a = math.radians(k * 30)
        p.ellipse(cx + math.cos(a) * 145, cy + math.sin(a) * 145, 18, 18, w=7)
    for k in range(8):
        a = math.radians(k * 45)
        p.line([(cx + math.cos(a) * 60, cy + math.sin(a) * 60), (cx + math.cos(a) * 120, cy + math.sin(a) * 120)], w=8)
    # dört yönde laleler ve saz yaprakları
    for k in range(4):
        a = k * 90 - 90
        ra = math.radians(a)
        bx, by = cx + math.cos(ra) * 170, cy + math.sin(ra) * 170
        tulip(p, bx, by, a, 200)
        for da in (-35, 35):
            leaf(p, bx, by, a + da, 260, 55, w=10)
        for da in (-60, 60):                                                    # köşeye doğru küçük laleler
            tulip(p, cx + math.cos(math.radians(a + 45)) * 330, cy + math.sin(math.radians(a + 45)) * 330, a + 45 + da * 0.3, 120, w=9)
    # köşe gülleri
    for x, y in ((240, 300), (1160, 300), (240, 1300), (1160, 1300)):
        p.circle(x, y, 60, w=10)
        for k in range(6):
            a = math.radians(k * 60)
            p.circle(x + math.cos(a) * 32, y + math.sin(a) * 32, 18, w=7)
    p.save("zor", "lofi-tuliptile-001")


def city():
    """Şehir: pencereli binalar (pencere saymaca), yol, arabalar, ağaçlar."""
    import random as _r
    rnd = _r.Random(3)
    p = Pen()
    sun(p, 1200, 200, 70)
    cloud(p, 300, 200, 0.8)
    cloud(p, 760, 260, 0.7)
    buildings = [(90, 640, 300), (300, 420, 520), (520, 560, 700), (700, 340, 920), (920, 600, 1100), (1100, 460, 1320)]
    for x0, top, x1 in buildings:
        rect(p, x0, top, x1, 1180)
        if rnd.random() < 0.5:                                                  # çatı detayı
            p.poly([(x0 + 20, top), ((x0 + x1) / 2, top - 70), (x1 - 20, top)], fill=255, w=10)
        cols = max(3, int((x1 - x0 - 30) / 55))
        ww = (x1 - x0 - 30) / cols
        y = top + 40
        while y + 70 < 1080:
            for c in range(cols):
                wx = x0 + 15 + c * ww + 10
                rect(p, wx, y, wx + ww - 16, y + 48, w=6)
            y += 72
        rect(p, (x0 + x1) / 2 - 40, 1080, (x0 + x1) / 2 + 40, 1180, w=9)         # kapı
    # kaldırım, yol
    rect(p, 40, 1180, 1360, 1240, w=10)
    rect(p, 40, 1240, 1360, 1500, w=12)
    for x in range(80, 1340, 220):
        rect(p, x, 1360, x + 120, 1385, w=7)
    # arabalar
    for x0, flip in ((180, 1), (820, -1)):
        p.poly(rrect(x0, 1290, x0 + 330, 1380, 25), fill=255)
        p.poly([(x0 + 60, 1290), (x0 + 110, 1230), (x0 + 230, 1230), (x0 + 280, 1290)], fill=255, w=10)
        p.line([(x0 + 170, 1235), (x0 + 170, 1290)], w=8)
        for dx in (70, 260):
            wheel(p, x0 + dx, 1385, 38)
    # ağaçlar (kaldırımda)
    for x in (410, 1010):
        rect(p, x - 12, 1080, x + 12, 1180, w=8)
        p.circle(x, 1040, 60, fill=255, w=10)
    p.save("zor", "lofi-city-001")


def reef():
    """Mercan resifi: çizgili balıklar, mercanlar, yosunlar, deniz yıldızı, yengeç, kabarcıklar."""
    import random as _r
    rnd = _r.Random(5)
    p = Pen()
    p.poly(curve([(130, 1320), (400, 1270), (800, 1310), (1270, 1260), (1270, 1560), (130, 1560)], 8), fill=255)   # kum
    for x, y in ((150, 1450), (520, 1480), (950, 1430), (1250, 1480)):
        p.circle(x, y, 18, w=7)
    # mercanlar (dallı)
    def coral(x, y, h, spread):
        p.poly(band([(x, y), (x, y - h)], 26, 18), fill=255, w=10)
        for k, sgn in ((0.35, -1), (0.55, 1), (0.75, -1)):
            bx, by = x, y - h * k
            p.poly(band([(bx, by), (bx + sgn * spread, by - h * 0.3)], 18, 12), fill=255, w=9)
            p.circle(bx + sgn * spread, by - h * 0.3 - 5, 20, fill=255, w=8)
        p.circle(x, y - h - 5, 24, fill=255, w=8)
    coral(220, 1340, 380, 90)
    coral(1150, 1330, 420, 100)
    coral(700, 1330, 260, 80)
    # yosunlar
    for x in (420, 900, 1300, 80):
        p.poly(band(open_curve([(x, 1340), (x - 30, 1220), (x + 20, 1100), (x - 10, 980)], 6), 20, 8), fill=255, w=9)
    # deniz yıldızı, yengeç
    obj_star(p, 560, 1430, 60)
    p.ellipse(1000, 1380, 80, 50, fill=255)
    for sx in (-1, 1):
        p.line([(1000 + sx * 70, 1370), (1000 + sx * 120, 1320)], w=10)
        p.circle(1000 + sx * 125, 1305, 22, w=8)
    eyes(p, 1000, 1370, 25, 10)
    # çizgili balıklar
    for (x, y, r, d) in ((300, 400, 110, 1), (900, 330, 95, -1), (620, 640, 130, 1), (1100, 760, 90, -1), (260, 820, 85, 1),
                         (820, 960, 100, -1), (450, 1080, 70, 1)):
        p.poly([(x + d * r * 0.7, y), (x + d * r * 1.35, y - r * 0.55), (x + d * r * 1.35, y + r * 0.55)], fill=255, w=10)
        p.ellipse(x, y, r, r * 0.62, fill=255, w=11)
        for k in (-0.3, 0.1, 0.45):
            xx = x + d * r * k
            hh = r * 0.62 * math.sqrt(max(0, 1 - k * k)) - 6
            p.line([(xx, y - hh), (xx, y + hh)], w=8)
        eyes(p, x - d * r * 0.6, y - r * 0.1, 0, max(6, r * 0.09))
    # kabarcıklar
    for _ in range(30):
        x, y = rnd.randint(100, 1300), rnd.randint(180, 1000)
        p.circle(x, y, rnd.randint(12, 26), w=7)
    for (x, y, r, d) in ((1150, 480, 70, -1), (520, 260, 60, 1), (1180, 1040, 65, -1), (980, 600, 55, 1), (150, 600, 60, 1), (700, 870, 55, -1)):
        p.poly([(x + d * r * 0.7, y), (x + d * r * 1.35, y - r * 0.55), (x + d * r * 1.35, y + r * 0.55)], fill=255, w=9)
        p.ellipse(x, y, r, r * 0.62, fill=255, w=10)
        for k in (-0.2, 0.3):
            xx = x + d * r * k
            hh = r * 0.62 * math.sqrt(max(0, 1 - k * k)) - 6
            p.line([(xx, y - hh), (xx, y + hh)], w=7)
        p.dot(x - d * r * 0.6, y - r * 0.1, 7)
    for x, y in ((380, 1460), (820, 1500), (1120, 1490)):                         # deniz kabukları
        p.poly(curve([(x - 40, y + 20), (x - 30, y - 25), (x + 30, y - 25), (x + 40, y + 20)], 6), fill=255, w=8)
        for dx in (-15, 0, 15):
            p.line([(x, y + 18), (x + dx * 2, y - 18)], w=5)
    p.save("zor", "lofi-reef-001")


def space():
    """Uzay: bantlı gezegenler, halkalı gezegen, kraterli ay, roket, kuyruklu yıldız, yıldızlar."""
    import random as _r
    rnd = _r.Random(11)
    p = Pen()
    # bantlı dev gezegen
    p.circle(330, 420, 220, fill=255)
    for dy in (-140, -70, 0, 70, 140):
        hw = math.sqrt(max(0, 220 ** 2 - dy ** 2)) - 6
        p.line(open_curve([(330 - hw, 420 + dy), (330, 420 + dy + 20), (330 + hw, 420 + dy)], 6), w=9)
    p.ellipse(400, 470, 45, 25, w=8)                                            # fırtına lekesi
    # halkalı gezegen
    p.ellipse(1060, 760, 300, 70, fill=255)
    p.circle(1060, 760, 160, fill=255)
    p.arc(1060, 760, 300, 70, 0, 180, w=12)
    for dy in (-80, 0, 80):
        hw = math.sqrt(max(0, 160 ** 2 - dy ** 2)) - 6
        p.line([(1060 - hw, 760 + dy), (1060 + hw, 760 + dy)], w=8)
    # kraterli ay
    p.circle(1100, 250, 140, fill=255)
    for x, y, r in ((1050, 210, 35), (1150, 300, 25), (1120, 190, 18), (1060, 320, 20)):
        p.circle(x, y, r, w=8)
    # roket
    rx, ry = 520, 1050
    p.poly(curve([(rx, ry - 260), (rx + 90, ry - 120), (rx + 95, ry + 140), (rx - 95, ry + 140), (rx - 90, ry - 120)], 10), fill=255)
    p.circle(rx, ry - 60, 50, w=10)
    p.circle(rx, ry + 50, 30, w=9)
    for sx in (-1, 1):
        p.poly([(rx + sx * 90, ry + 40), (rx + sx * 170, ry + 180), (rx + sx * 90, ry + 140)], fill=255, w=10)
    p.poly([(rx - 60, ry + 140), (rx + 60, ry + 140), (rx, ry + 300)], fill=255, w=10)
    p.poly([(rx - 30, ry + 150), (rx + 30, ry + 150), (rx, ry + 240)], w=8)
    # kuyruklu yıldız
    obj_star(p, 300, 1350, 55)
    for k in range(3):
        p.line([(270 - k * 5, 1385 + k * 20), (90 - k * 10, 1460 + k * 30)], w=8)
    # küçük gezegenler
    p.circle(880, 1300, 90, fill=255)
    p.arc(880, 1300, 90, 30, 0, 180, w=8)
    p.circle(1250, 1150, 55, fill=255)
    # yıldızlar
    placed = []
    for _ in range(140):
        x, y = rnd.randint(80, 1320), rnd.randint(80, 1520)
        if any((x - a) ** 2 + (y - b) ** 2 < (r + 60) ** 2 for a, b, r in tuple((px, py, 15) for px, py in placed) + ((330, 420, 220), (1060, 760, 300), (1100, 250, 140), (520, 1050, 300), (880, 1300, 90), (1250, 1150, 55), (300, 1350, 60), (900, 520, 150), (180, 1460, 90), (960, 1440, 120), (800, 1440, 120), (1120, 1440, 120))):
            continue
        obj_star(p, x, y, rnd.randint(16, 28))
        placed.append((x, y))
        if len(placed) >= 65: break
    # uydu: güneş panelleri ızgaralı
    rect(p, 860, 480, 940, 560, w=10)
    for sx in (-1, 1):
        x0 = 900 + sx * 60
        x1 = 900 + sx * 250
        rect(p, min(x0, x1), 470, max(x0, x1), 570, w=9)
        for k in range(1, 4):
            xx = x0 + (x1 - x0) * k / 4
            p.line([(xx, 470), (xx, 570)], w=6)
        p.line([(min(x0, x1), 520), (max(x0, x1), 520)], w=6)
    p.line([(900, 480), (900, 420)], w=8)
    p.circle(900, 410, 14, w=6)
    # asteroitler
    import random as _r2
    r2 = _r2.Random(4)
    for k in range(12):
        x, y = 640 + k * 55, 1460 - (k % 3) * 40
        pts = [(x + math.cos(math.radians(a)) * r2.randint(14, 24), y + math.sin(math.radians(a)) * r2.randint(14, 24)) for a in range(0, 360, 45)]
        p.poly(pts, w=6)
    p.save("orta", "lofi-space-001")


def flower_garden():
    """Çiçek bahçesi: çit, lale/papatya/ayçiçeği, kelebekler, arı, kuş evi, taş yol."""
    p = Pen()
    sun(p, 1180, 200, 70)
    cloud(p, 380, 200, 0.8)
    # kuş evi
    rect(p, 140, 600, 160, 1000, w=9)
    p.poly([(130, 450), (150, 360), (240, 450)], fill=255, w=10)
    rect(p, 80, 450, 220, 610, w=10)
    p.circle(150, 520, 30, w=8)
    # çit
    for x in range(260, 1270, 90):
        p.poly([(x, 760), (x + 35, 720), (x + 70, 760), (x + 70, 1000), (x, 1000)], fill=255, w=9)
    rect(p, 240, 820, 1250, 845, w=8)
    rect(p, 240, 930, 1250, 955, w=8)
    p.poly(curve([(170, 1000), (700, 980), (1230, 1000), (1230, 1500), (170, 1500)], 8), fill=255)   # çimen
    # taş yol
    for i, (x, y) in enumerate(((700, 1500), (660, 1400), (720, 1300), (680, 1210), (730, 1120), (700, 1050))):
        p.ellipse(x, y, 70 - i * 7, 35 - i * 3, w=9)
    # çiçekler: ayçiçekleri arkada, laleler ve papatyalar önde
    for x in (330, 1100):
        p.line([(x, 1100), (x, 700)], w=12)
        leaf(p, x, 950, -30, 120, 40, w=9)
        for k in range(14):
            a = math.radians(k * 360 / 14)
            p.ellipse(x + math.cos(a) * 95, 650 + math.sin(a) * 95, 30, 30, w=8)
        p.circle(x, 650, 70, fill=255, w=10)
        for k in range(6):
            a = math.radians(k * 60)
            p.circle(x + math.cos(a) * 35, 650 + math.sin(a) * 35, 12, w=6)
    for x in (200, 470, 930, 1250):
        p.line([(x, 1250), (x, 1080)], w=10)
        tulip(p, x, 1080, -90, 130, w=9)
    for x, y in ((360, 1350), (540, 1200), (880, 1240), (1060, 1380), (1200, 1200), (160, 1430)):
        for k in range(8):
            a = math.radians(k * 45)
            p.ellipse(x + math.cos(a) * 34, y + math.sin(a) * 34, 16, 16, w=7)
        p.circle(x, y, 18, w=7)
    # kelebekler ve arı
    for x, y in ((560, 420), (860, 520)):
        for sx in (-1, 1):
            p.ellipse(x + sx * 40, y - 20, 38, 45, fill=255, w=9)
            p.ellipse(x + sx * 32, y + 35, 26, 30, fill=255, w=9)
            p.circle(x + sx * 40, y - 20, 14, w=6)
        p.ellipse(x, y + 5, 12, 50, fill=255, w=8)
    p.ellipse(1000, 380, 50, 34, fill=255, w=9)
    for dx in (-15, 10):
        p.line([(1000 + dx, 350), (1000 + dx, 412)], w=7)
    p.save("zor", "lofi-flowergarden-001")


def zen_fish():
    """Desenli balık: gövde şeritlere bölünmüş, her şeritte farklı küçük desen."""
    p = Pen()
    p.poly(curve([(200, 1380), (700, 1340), (1200, 1380), (1200, 1470), (200, 1470)], 8), fill=255)   # kum
    for x in (260, 1120):
        p.poly(band(open_curve([(x, 1380), (x - 30, 1260), (x + 20, 1170)], 6), 18, 8), fill=255, w=8)
    for x, y, r in ((560, 170, 28), (620, 250, 20), (580, 320, 14), (900, 220, 24), (860, 140, 16)):
        p.circle(x, y, r, w=8)
    cx, cy, rx, ry = 640, 800, 470, 330
    def inside(x, y, m=0):
        return ((x - cx) / (rx - m)) ** 2 + ((y - cy) / (ry - m)) ** 2 < 1
    # kuyruk ve yüzgeçler (desenli)
    tail = [(1080, 800), (1320, 520), (1290, 800), (1320, 1080)]
    p.poly(tail, fill=255)
    for k in (0.33, 0.66):
        p.line([(1080, 800), (1320 - 30 * k, 520 + 560 * k)], w=8)
    p.poly(curve([(500, 500), (640, 330), (820, 380), (860, 500)], 8), fill=255)
    for x in (600, 680, 760):
        p.line([(x, 490), (x + 20, 400)], w=8)
    p.poly(curve([(560, 1100), (620, 1260), (760, 1220), (760, 1110)], 8), fill=255)
    # gövde
    p.ellipse(cx, cy, rx, ry, fill=255)
    bands = [cx - 280, cx - 120, cx + 40, cx + 200, cx + 340]
    for bx in bands:
        hh = ry * math.sqrt(max(0, 1 - ((bx - cx) / rx) ** 2)) - 6
        p.line(open_curve([(bx, cy - hh), (bx + 30, cy), (bx, cy + hh)], 6), w=10)
    # şerit desenleri
    for i in range(len(bands) - 1):
        a, b = bands[i] + 30, bands[i + 1] - 10
        kind = i % 4
        for y in range(cy - ry + 40, cy + ry - 20, 70):
            for x in range(int(a), int(b), 70):
                px, py = x + 35, y + (35 if (x // 70) % 2 else 0)
                if not inside(px, py, 45) or px > b - 25 or px < a + 10: continue
                if kind == 0: p.circle(px, py, 20, w=7)
                elif kind == 1: p.poly([(px, py - 24), (px + 22, py + 16), (px - 22, py + 16)], w=7)
                elif kind == 2: p.poly([(px, py - 24), (px + 20, py), (px, py + 24), (px - 20, py)], w=7)
                else: p.arc(px, py + 10, 22, 22, 180, 360, w=7)
    # baş: göz ve solungaç
    p.circle(290, 740, 60, fill=255, w=11)
    p.circle(290, 740, 30, w=9)
    p.dot(295, 740, 12)
    p.arc(270, 860, 40, 25, 20, 160, w=10)
    for x, y, r in ((120, 520, 30), (160, 420, 22), (110, 330, 16)):
        p.circle(x, y, r, w=8)
    p.save("orta", "lofi-zenfish-001")


def balloons():
    """Sıcak hava balonları: dilimli, şeritli balonlar, sepetler, bulutlar, kuşlar."""
    p = Pen()
    def balloon(cx, cy, rx, ry, gores=6, bands=3):
        # sepet ve ipler
        by = cy + ry + 130
        p.line([(cx - rx * 0.45, cy + ry * 0.75), (cx - rx * 0.2, by)], w=8)
        p.line([(cx + rx * 0.45, cy + ry * 0.75), (cx + rx * 0.2, by)], w=8)
        p.poly([(cx - rx * 0.25, by), (cx + rx * 0.25, by), (cx + rx * 0.2, by + rx * 0.3), (cx - rx * 0.2, by + rx * 0.3)], fill=255, w=10)
        p.line([(cx - rx * 0.23, by + rx * 0.15), (cx + rx * 0.23, by + rx * 0.15)], w=7)
        # balon gövdesi (alt kısmı sivrilen)
        outline = []
        for t in range(0, 361, 6):
            a = math.radians(t)
            k = 1 - 0.35 * max(0, math.sin(a))                                  # alt daralır
            outline.append((cx + math.cos(a) * rx * k, cy + math.sin(a) * ry))
        p.poly(outline, fill=255)
        for g in range(1, gores):                                               # dilimler
            c = -1 + 2 * g / gores
            pts = []
            for t in range(-90, 91, 6):
                a = math.radians(t)
                k = 1 - 0.35 * max(0, math.sin(a))
                pts.append((cx + c * math.cos(a) * rx * k, cy + math.sin(a) * ry))
            p.line(pts, w=8)
        for b in range(1, bands + 1):                                           # şeritler
            y = cy - ry * 0.55 + b * ry * 0.4
            a = math.asin(max(-1, min(1, (y - cy) / ry)))
            k = 1 - 0.35 * max(0, math.sin(a))
            hw = math.cos(a) * rx * k - 4
            p.line([(cx - hw, y), (cx + hw, y)], w=8)
    cloud(p, 250, 1350, 1.2)
    cloud(p, 1150, 1420, 1.0)
    cloud(p, 700, 230, 0.8)
    balloon(420, 520, 260, 300, 9, 5)
    balloon(1050, 760, 200, 230, 8, 4)
    balloon(720, 1180, 150, 170, 6, 3)
    balloon(1170, 300, 80, 95, 4, 2)
    balloon(220, 1050, 90, 105, 4, 2)
    for x, y in ((1000, 260), (1100, 330), (180, 1000)):
        p.arc(x - 30, y, 30, 22, 200, 340, w=9)
        p.arc(x + 30, y, 30, 22, 200, 340, w=9)
    p.save("zor", "lofi-balloons-001")


def farm():
    """Çiftlik: tahtalı ambar, silo, çit, koyunlar, tavuklar, gölet ve ördek, tarla, elma ağacı."""
    p = Pen()
    sun(p, 1200, 190, 70)
    cloud(p, 420, 180, 0.8)
    p.poly(curve([(130, 640), (400, 560), (800, 620), (1270, 560), (1270, 720), (130, 720)], 10), fill=255)   # uzak tepe
    # silo
    p.poly(rrect(880, 380, 1040, 980, 30), fill=255)
    p.arc(960, 400, 80, 70, 180, 360, w=12)
    for y in (520, 640, 760, 880):
        p.line([(880, y), (1040, y)], w=8)
    # ambar
    p.poly([(320, 520), (600, 340), (880, 520)], fill=255)
    rect(p, 340, 520, 860, 980)
    for x in range(380, 860, 60):
        p.line([(x, 520), (x, 760)], w=6)
    rect(p, 500, 760, 700, 980, w=10)
    p.line([(500, 760), (700, 980)], w=9)
    p.line([(700, 760), (500, 980)], w=9)
    rect(p, 540, 560, 660, 680, w=10)
    p.line([(600, 560), (600, 680)], w=7)
    # ağaç
    tree(p, 1170, 1000, 0.8)
    # çit
    for x in range(130, 1270, 80):
        rect(p, x, 940, x + 30, 1080, w=8)
    rect(p, 130, 980, 1270, 1000, w=7)
    rect(p, 130, 1040, 1270, 1060, w=7)
    p.poly(curve([(170, 1080), (700, 1060), (1230, 1080), (1230, 1490), (170, 1490)], 8), fill=255)   # çayır
    # gölet ve ördek
    p.ellipse(1050, 1270, 260, 110, fill=255)
    p.ellipse(1000, 1340, 70, 38, fill=255, w=10)
    p.circle(1050, 1300, 30, fill=255, w=10)
    p.poly([(1078, 1295), (1110, 1305), (1078, 1312)], w=7)
    # koyunlar (bulut gövdeli)
    for x, y in ((250, 1200), (560, 1260)):
        for dx, dy in ((-60, 0), (0, -30), (130, 0), (0, 30), (-30, 25), (30, 25)):
            p.circle(x + dx, y + dy, 50, fill=255, w=9)
        p.ellipse(x - 110, y - 10, 45, 35, fill=255, w=10)
        p.dot(x - 120, y - 15, 7)
        for dx in (-40, 40):
            rect(p, x + dx - 10, y + 60, x + dx + 10, y + 110, w=7)
    # tavuklar
    for x, y in ((300, 1440), (720, 1450)):
        p.ellipse(x, y, 60, 45, fill=255, w=10)
        p.circle(x + 50, y - 50, 30, fill=255, w=9)
        p.circle(x + 50, y - 90, 12, w=6)
        p.poly([(x + 78, y - 52), (x + 100, y - 45), (x + 78, y - 38)], w=6)
    for x in (260, 470, 680, 900):                                              # tarla filizleri
        for y in (1150, 1230):
            leaf(p, x, y, -60, 45, 15, w=6)
            leaf(p, x, y, -120, 45, 15, w=6)
    for x in (180, 380, 820, 980, 1150):                                        # çit dibinde çiçekler
        obj_flower(p, x, 1120, 34)
    p.save("orta", "lofi-farm-001")


def market():
    """Pazar tezgâhı: çizgili tente, kasalarda meyve-sebze, terazi, flama süsleri."""
    p = Pen()
    # flama
    p.line(open_curve([(60, 160), (700, 240), (1340, 160)], 10), w=8)
    for i in range(10):
        x = 120 + i * 120
        y = 160 + 80 * math.sin(math.pi * (x - 60) / 1280)
        p.poly([(x, y), (x + 70, y + 4), (x + 35, y + 80)], w=8)
    # tente (çizgili, dalgalı alt kenar)
    p.poly([(120, 300), (1280, 300), (1340, 560), (60, 560)], fill=255)
    for i in range(1, 10):
        t = i / 10
        p.line([(120 + 1160 * t, 300), (60 + 1280 * t, 560)], w=9)
    for i in range(10):
        x0 = 60 + i * 128
        p.arc(x0 + 64, 560, 64, 50, 0, 180, w=10)
    # direkler
    for x in (120, 1280):
        rect(p, x - 15, 560, x + 15, 1500, w=9)
    # tezgâh
    rect(p, 100, 1000, 1300, 1080)
    rect(p, 140, 1080, 1260, 1480)
    for x in range(140, 1260, 160):
        p.line([(x, 1080), (x, 1480)], w=6)
    # kasalar ve ürünler
    crates = [(150, 760, "apple"), (450, 760, "orange"), (750, 760, "tomato"), (1030, 760, "melon")]
    for x0, y0, kind in crates:
        rect(p, x0, y0 + 110, x0 + 240, y0 + 240, w=10)
        p.line([(x0, y0 + 175), (x0 + 240, y0 + 175)], w=7)
        if kind == "melon":
            for k in range(2):
                cx = x0 + 70 + k * 100
                p.poly([(cx - 70, y0 + 110)] + [(cx + math.cos(math.radians(a)) * 70, y0 + 110 - math.sin(math.radians(a)) * 70) for a in range(180, -1, -15)], fill=255, w=9)
                p.arc(cx, y0 + 110, 52, 52, 180, 360, w=7)
                for d in (-25, 0, 25):
                    p.ellipse(cx + d, y0 + 80, 5, 8, w=4)
        else:
            for k in range(4):
                for row in range(3):
                    cx = x0 + 40 + k * 55 + (25 if row else 0)
                    cy = y0 + 80 - row * 45
                    if cx > x0 + 215: continue
                    p.circle(cx, cy, 30, fill=255, w=8)
                    if kind == "apple": p.line([(cx, cy - 30), (cx + 6, cy - 44)], w=6)
                    elif kind == "tomato": p.poly([(cx - 10, cy - 28), (cx, cy - 40), (cx + 10, cy - 28)], w=5)
                    else: p.dot(cx, cy, 4)
    # terazi
    rect(p, 640, 940, 760, 1000, w=10)
    rect(p, 690, 860, 710, 940, w=8)
    p.ellipse(700, 850, 110, 25, fill=255, w=9)
    # muz salkımı (tezgahta)
    for k in range(3):
        p.poly(crescent(330 + k * 40, 900, 120, 35, 30, 110), fill=255, w=8)
    for y in (1180, 1280, 1380):
        p.line([(140, y), (1260, y)], w=6)                                       # tezgah önü tahtaları
    for x0, y0, kind in crates:
        rect(p, x0 + 80, y0 + 200, x0 + 160, y0 + 230, w=6)                       # fiyat etiketleri
    for i in range(10):                                                         # ikinci sıra flama
        x = 180 + i * 110
        p.poly([(x, 255), (x + 60, 258), (x + 30, 320)], w=7)
    p.save("zor", "lofi-market-001")


def bookshelf():
    """Kitaplık: desenli kitap sırtları, saksı, küre, saat, oyuncak ayı."""
    import random as _r
    rnd = _r.Random(9)
    p = Pen()
    rect(p, 150, 120, 1250, 1500)
    shelves = (420, 760, 1100, 1440)
    for y in shelves:
        rect(p, 150, y, 1250, y + 40, w=10)
    tops = (160,) + tuple(y + 40 for y in shelves[:-1])
    for si, (ytop, ybot) in enumerate(zip(tops, shelves)):
        x = 180
        limit = 1220 if si != 1 else 860                                        # 2. rafta küre için yer
        if si == 3: limit = 820                                                 # son rafta ayı için yer
        while x < limit - 60:
            wdt = rnd.randint(48, 80)
            h = rnd.randint(int((ybot - ytop) * 0.7), int((ybot - ytop) * 0.95))
            if rnd.random() < 0.15 and x + 140 < limit:                         # yatık kitap yığını
                for k in range(3):
                    rect(p, x, ybot - (k + 1) * 45, x + 140 - k * 10, ybot - k * 45, w=8)
                x += 150
                continue
            rect(p, x, ybot - h, x + wdt, ybot, w=8)
            p.line([(x, ybot - h + 25), (x + wdt, ybot - h + 25)], w=6)
            p.line([(x, ybot - 25), (x + wdt, ybot - 25)], w=6)
            pat = rnd.randint(1, 3)
            if pat == 0: p.line([(x, ybot - h + 30), (x + wdt, ybot - h + 30)], w=6); p.line([(x, ybot - 30), (x + wdt, ybot - 30)], w=6)
            elif pat == 1: p.circle(x + wdt / 2, ybot - h / 2, min(18, wdt / 3), w=6)
            elif pat == 2: p.poly([(x + wdt / 2, ybot - h / 2 - 20), (x + wdt / 2 + 14, ybot - h / 2), (x + wdt / 2, ybot - h / 2 + 20), (x + wdt / 2 - 14, ybot - h / 2)], w=6)
            x += wdt + 6
    # küre (2. raf)
    p.circle(1040, 600, 120, fill=255)
    p.arc(1040, 600, 120, 40, 0, 360, w=7)
    p.arc(1040, 600, 50, 120, 0, 360, w=7)
    rect(p, 1010, 720, 1070, 760, w=8)
    # oyuncak ayı (son raf)
    for x in (970, 1130):
        p.circle(x, 1180, 38, w=9)
    p.ellipse(1050, 1360, 130, 80, fill=255)
    p.circle(1050, 1240, 110, fill=255)
    eyes(p, 1050, 1230, 38, 11)
    p.ellipse(1050, 1275, 28, 18)
    p.save("zor", "lofi-bookshelf-001")


if __name__ == "__main__":
    import sys
    if len(sys.argv) > 1:                       # yalnızca adı verilenleri çiz: python kendi_resimler.py bus train
        for name in sys.argv[1:]:
            globals()[name]()
        raise SystemExit
    for f in (rocket, house, fish, turtle, snail, car,
              frog, ladybug, whale, mouse, penguin, cow, bee, octopus, hedgehog, hippo,
              koala, monkey, parrot, squirrel, starfish, jellyfish, hen, camel, numbers):
        f()
