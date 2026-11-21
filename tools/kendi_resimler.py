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


def sky(p):
    """Üstte güneş ve bulutlar (yatay resimleri dikey yapar, gökyüzü bölgeleri ekler)."""
    p.circle(260, 260, 100)
    for i in range(8):
        a = i * math.pi / 4
        p.line([(260 + math.cos(a) * 135, 260 + math.sin(a) * 135), (260 + math.cos(a) * 190, 260 + math.sin(a) * 190)], w=12)
    face(p, 260, 250, 0.7, blush=False)
    for x, y in ((820, 220), (1150, 400)):
        p.poly(curve([(x - 160, y + 50), (x - 110, y - 40), (x - 10, y - 70), (x + 90, y - 40), (x + 150, y + 50)], 10))


def turtle():
    """Kaplumbağa: kabukta altıgen desen."""
    p = Pen()
    sky(p)
    p.poly(curve([(150, 1300), (700, 1260), (1250, 1300), (1250, 1500), (150, 1500)], 8))   # çimen
    for x in range(150, 1300, 160):
        p.poly([(x - 25, 1290), (x, 1200), (x + 25, 1290)], w=9)
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
    p.save("orta", "lofi-turtle-001")


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
    p.save("orta", "lofi-snail-001")


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


if __name__ == "__main__":
    for f in (rocket, house, fish, turtle, snail, car):
        f()
