"""
Türkçe hazır sesleri üretir (kendi ses kaydı gerekmez).

  1) Unity editöründe tools/ses_listesi.cs.txt çalıştırılır -> LofiBoya/.utmp/ses_listesi.txt
     (oyunda söylenen bütün cümle parçaları; Speech.Pieces ile aynı bölme kuralı)
  2) python tools/ses_uret.py  (tools/ses_env içindeki Python ile)
     Her parça Chatterbox Multilingual (MIT lisanslı) ile seslendirilir, Whisper ile yazıya çevrilip
     kontrol edilir; uymazsa 5 kereye kadar yeniden denenir. Tutmayanlar dosyaya yazılmaz (oyun o
     cümlede telefonun TTS sesine düşer) ve ses_hatalar.txt'ye not edilir.
  Çıktı: LofiBoya/Assets/Resources/Voice/tr/a/<fnv1a32>.ogg  (Speech.Hash ile aynı ad)

Zaten var olan dosyalar atlanır; liste değişince yalnızca yeni parçalar üretilir.
"""
import difflib, os, re, sys, time
import numpy as np
import soundfile as sf
import torch, librosa
import pyloudnorm

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LIST = os.path.join(ROOT, "LofiBoya", ".utmp", "ses_listesi.txt")
OUT = os.path.join(ROOT, "LofiBoya", "Assets", "Resources", "Voice", "tr", "a")
LOG = os.path.join(ROOT, "tools", "ses_hatalar.txt")
SR = 24000
TRIES = 5


def fnv1a(s: str) -> str:
    h = 2166136261
    for b in s.encode("utf-8"):
        h ^= b
        h = (h * 16777619) & 0xFFFFFFFF
    return f"{h:08x}"


ONES = ["", "bir", "iki", "üç", "dört", "beş", "altı", "yedi", "sekiz", "dokuz"]
TENS = ["", "on", "yirmi", "otuz", "kırk", "elli", "altmış", "yetmiş", "seksen", "doksan"]


def num_tr(n: int) -> str:
    if n == 0:
        return "sıfır"
    if n >= 100:
        return str(n)
    return (TENS[n // 10] + " " + ONES[n % 10]).strip()


def speakable(t: str) -> str:
    """Modele verilecek metin: rakamlar Türkçe kelimeyle."""
    return re.sub(r"\d+", lambda m: num_tr(int(m.group())), t)


def norm(t: str) -> str:
    t = speakable(t).replace("I", "ı").replace("İ", "i").lower()
    t = re.sub(r"[^\w\s]", " ", t)
    return " ".join(t.split())


def score(target: str, heard: str) -> float:
    return difflib.SequenceMatcher(None, norm(target), norm(heard)).ratio()


def max_len(t: str) -> float:
    """Beklenen en uzun süre (sn): uzun kuyruk / saçmalama yakalansın."""
    return 0.8 + 0.105 * len(speakable(t))


def main():
    from chatterbox.mtl_tts import ChatterboxMultilingualTTS
    from transformers import pipeline

    os.makedirs(OUT, exist_ok=True)
    pieces = [l.strip() for l in open(LIST, encoding="utf-8") if l.strip()]
    todo = [p for p in pieces if not os.path.exists(os.path.join(OUT, fnv1a(p) + ".ogg"))]
    print(f"{len(pieces)} parça, {len(todo)} yeni", flush=True)
    if not todo:
        return
    tts = ChatterboxMultilingualTTS.from_pretrained(device="cuda")
    asr = pipeline("automatic-speech-recognition", model="openai/whisper-large-v3-turbo",
                   torch_dtype=torch.float16, device="cuda:0")
    meter = pyloudnorm.Meter(SR)
    failed = []
    t0 = time.time()
    for i, text in enumerate(todo):
        best = None
        for attempt in range(TRIES):
            wav = tts.generate(speakable(text), language_id="tr", exaggeration=0.5, cfg_weight=0.3,
                               temperature=0.7 if attempt < 2 else 0.6)
            y = wav.squeeze(0).cpu().numpy().astype(np.float32)
            if tts.sr != SR:
                y = librosa.resample(y, orig_sr=tts.sr, target_sr=SR)
            y, _ = librosa.effects.trim(y, top_db=38)
            dur = len(y) / SR
            heard = asr({"raw": librosa.resample(y, orig_sr=SR, target_sr=16000), "sampling_rate": 16000},
                        generate_kwargs={"language": "turkish", "task": "transcribe"})["text"].strip()
            sc = score(text, heard)
            if dur > max_len(text):
                sc -= 0.3   # uzun kuyruk: büyük ihtimalle sonuna saçma ses eklendi
            if best is None or sc > best[0]:
                best = (sc, y, heard)
            if sc >= 0.92:
                break
        sc, y, heard = best
        if sc < 0.8:
            failed.append(f"{text}\t{heard}\t{sc:.2f}")
            print(f"[{i + 1}/{len(todo)}] YOK  {text} -> {heard} ({sc:.2f})", flush=True)
            continue
        # ses düzeyi: -18 LUFS, tepe -1 dBFS; baş/sonda kısa yumuşatma ve sessizlik
        try:
            y = pyloudnorm.normalize.loudness(y, meter.integrated_loudness(y), -18.0)
        except Exception:
            pass
        peak = np.max(np.abs(y)) or 1.0
        if peak > 0.89:
            y = y * (0.89 / peak)
        f = int(0.012 * SR)
        y[:f] *= np.linspace(0, 1, f)
        y[-f:] *= np.linspace(1, 0, f)
        y = np.concatenate([np.zeros(int(0.04 * SR)), y, np.zeros(int(0.06 * SR))]).astype(np.float32)
        sf.write(os.path.join(OUT, fnv1a(text) + ".ogg"), y, SR, format="OGG", subtype="VORBIS")
        el = time.time() - t0
        print(f"[{i + 1}/{len(todo)}] {sc:.2f} {text}  (kalan ~{el / (i + 1) * (len(todo) - i - 1) / 60:.0f} dk)", flush=True)
    with open(LOG, "w", encoding="utf-8") as fh:
        fh.write("\n".join(failed))
    print(f"bitti: {len(todo) - len(failed)} ses, {len(failed)} tutmadı -> {LOG}", flush=True)


if __name__ == "__main__":
    main()
