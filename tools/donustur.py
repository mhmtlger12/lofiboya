"""
LofiBoya level dönüştürücü
==========================
resimler/kolay|orta|zor içindeki çizgi resimleri (PNG) oyunun level dosyalarına çevirir.

Kullanım:
    python tools/donustur.py            -> tüm resimleri dönüştür (yeni / değişmiş olanları)
    python tools/donustur.py --hepsi    -> hepsini baştan dönüştür
    python tools/donustur.py --onizleme -> ayrıca tools/onizleme/ içine renkli önizleme kaydet

Her level için çıktı (LofiBoya/Assets/Resources/Levels/<zorluk>/<id>/):
    lines.bytes    gri tonlu çizgi resmi, PNG (oyunda boya ile çarpılır)
    regions.bytes  bölge haritası, PNG; her pikselin bölge numarası R + G*256
    thumb.bytes    galeri için küçük resim, PNG
    level.json     bölge sayısı, boyut, ipucu noktaları
"""
import argparse
import json
import os
import re
import shutil
import sys

import numpy as np
from PIL import Image, ImageOps
from scipy import ndimage as ndi

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "resimler")
DST = os.path.join(ROOT, "LofiBoya", "Assets", "Resources", "Levels")
PREVIEW = os.path.join(ROOT, "tools", "onizleme")
DIFFICULTIES = ["kolay", "orta", "zor"]

MAX_SIDE = 1600        # mobilde bellek/hız dengesi
LINE_THRESHOLD = 165   # bu parlaklığın altı çizgi sayılır
MIN_REGION_FRAC = 0.00004  # resmin bu oranından küçük bölgeler çizgiye katılır
MAX_REGIONS = 4000
MAX_ASPECT = 1.15      # genişlik/yükseklik bundan büyükse (yatay) resim atlanır


def slug(name):
    name = os.path.splitext(name)[0]
    name = re.sub(r"_\d+$", "", name)          # _1920
    name = re.sub(r"[^a-zA-Z0-9]+", "-", name).strip("-").lower()
    return name


def load_luminance(path):
    im = Image.open(path)
    im = im.convert("RGBA")
    white = Image.new("RGBA", im.size, (255, 255, 255, 255))
    white.alpha_composite(im)
    return white.convert("L")


def crop_to_content(lum, margin_frac=0.04):
    a = np.asarray(lum)
    ys, xs = np.where(a < 200)
    if len(xs) == 0:
        return lum
    tight = lum.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
    m = int(max(tight.size) * margin_frac)
    return ImageOps.expand(tight, border=m, fill=255)  # arka plan her yerden bağlı kalsın


def convert(path, out_dir, preview_path=None):
    lum = crop_to_content(load_luminance(path))
    if lum.width > lum.height * MAX_ASPECT:
        return -1  # yatay resim: telefonda küçük ve boş görünüyor, atla
    scale = MAX_SIDE / max(lum.size)
    if scale < 1:
        lum = lum.resize((round(lum.width * scale), round(lum.height * scale)), Image.LANCZOS)
    a = np.asarray(lum).astype(np.uint8)
    h, w = a.shape

    lines = a < LINE_THRESHOLD
    # Küçük çizgi boşluklarını kapat (boya taşmasın)
    closed = ndi.binary_dilation(lines, structure=np.ones((3, 3), bool), iterations=1)
    labels, n = ndi.label(~closed)  # 4-bağlantı

    # Küçük bölgeleri at
    min_area = max(25, int(w * h * MIN_REGION_FRAC))
    areas = np.bincount(labels.ravel(), minlength=n + 1)
    small = areas < min_area
    small[0] = True
    labels[small[labels]] = 0

    # Kalanları 1..N diye yeniden numarala, büyükten küçüğe değil sıradan
    keep = np.unique(labels)
    keep = keep[keep != 0]
    if len(keep) > MAX_REGIONS:
        raise RuntimeError(f"{len(keep)} bölge çok fazla (en çok {MAX_REGIONS}), resmi 'zor'a koy veya atla")
    remap = np.zeros(labels.max() + 1, np.int32)
    remap[keep] = np.arange(1, len(keep) + 1)
    labels = remap[labels]
    n = len(keep)

    # İpucu noktaları: her bölgenin en iç noktası (çizgilere en uzak)
    dist = ndi.distance_transform_edt(labels != 0)
    idx = np.arange(1, n + 1)
    centers = ndi.maximum_position(dist, labels, idx)
    radii = ndi.maximum(dist, labels, idx)
    areas = np.bincount(labels.ravel(), minlength=n + 1)

    # Çizgi pikselleri dahil her pikseli en yakın bölgeye ata (çizgiye dokunmak da çalışsın)
    _, (iy, ix) = ndi.distance_transform_edt(labels == 0, return_indices=True)
    full = labels[iy, ix]

    rgb = np.zeros((h, w, 3), np.uint8)
    rgb[..., 0] = full & 255
    rgb[..., 1] = full >> 8

    os.makedirs(out_dir, exist_ok=True)
    # Unity'de doku y ekseni aşağıdan yukarı; LoadImage bunu kendisi çevirir, olduğu gibi kaydediyoruz
    lum.save(os.path.join(out_dir, "lines.bytes"), format="PNG", optimize=True)
    Image.fromarray(rgb, "RGB").save(os.path.join(out_dir, "regions.bytes"), format="PNG", optimize=True)
    thumb = lum.copy()
    thumb.thumbnail((360, 360), Image.LANCZOS)
    thumb.save(os.path.join(out_dir, "thumb.bytes"), format="PNG", optimize=True)

    # Unity JsonUtility iç içe dizi okuyamaz: düz diziler. y ekseni Unity dokusu gibi aşağıdan yukarı.
    cx = [int(c[1]) for c in centers]
    cy = [int(h - 1 - c[0]) for c in centers]
    meta = {
        "width": w, "height": h, "regionCount": n,
        "cx": cx, "cy": cy,
        "radius": [round(float(r), 1) for r in radii],
        "area": [int(x) for x in areas[1:]],
    }
    with open(os.path.join(out_dir, "level.json"), "w", encoding="utf-8") as f:
        json.dump(meta, f, separators=(",", ":"))

    if preview_path:
        rng = np.random.default_rng(7)
        pal = (rng.uniform(0.55, 1.0, (n + 1, 3)) * 255).astype(np.uint8)
        col = pal[full].astype(np.float32) * (a[..., None] / 255.0)
        Image.fromarray(col.astype(np.uint8)).save(preview_path)
    return n


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--hepsi", action="store_true", help="hepsini yeniden dönüştür")
    ap.add_argument("--onizleme", action="store_true", help="renkli önizleme kaydet")
    args = ap.parse_args()

    index = {}
    for diff in DIFFICULTIES:
        src_dir = os.path.join(SRC, diff)
        index[diff] = []
        if not os.path.isdir(src_dir):
            continue
        for fn in sorted(os.listdir(src_dir)):
            if not fn.lower().endswith((".png", ".jpg", ".jpeg", ".webp")):
                continue
            lid = slug(fn)
            out_dir = os.path.join(DST, diff, lid)
            src_path = os.path.join(src_dir, fn)
            meta_path = os.path.join(out_dir, "level.json")
            fresh = os.path.exists(meta_path) and os.path.getmtime(meta_path) >= os.path.getmtime(src_path)
            try:
                if fresh and not args.hepsi:
                    with open(meta_path, encoding="utf-8") as f:
                        n = json.load(f)["regionCount"]
                    status = "hazır"
                else:
                    prev = None
                    if args.onizleme:
                        os.makedirs(PREVIEW, exist_ok=True)
                        prev = os.path.join(PREVIEW, f"{diff}_{lid}.png")
                    n = convert(src_path, out_dir, prev)
                    status = "dönüştürüldü"
                if n < 0:
                    if os.path.isdir(out_dir):
                        shutil.rmtree(out_dir)
                        meta_file = out_dir + ".meta"  # Unity'nin klasör meta dosyası
                        if os.path.exists(meta_file):
                            os.remove(meta_file)
                    print(f"[{diff}] {lid:50s}   yatay resim, atlandı")
                    continue
                index[diff].append({"id": lid, "regions": n})
                print(f"[{diff}] {lid:50s} {n:5d} bölge  ({status})")
            except Exception as e:  # noqa: BLE001
                print(f"[{diff}] {fn}: HATA {e}", file=sys.stderr)

    os.makedirs(DST, exist_ok=True)
    groups = [{"name": d, "levels": index[d]} for d in DIFFICULTIES]
    with open(os.path.join(DST, "index.json"), "w", encoding="utf-8") as f:
        json.dump({"groups": groups}, f, ensure_ascii=False, indent=1)
    total = sum(len(v) for v in index.values())
    print(f"\nToplam {total} level. index.json güncellendi.")


if __name__ == "__main__":
    main()
