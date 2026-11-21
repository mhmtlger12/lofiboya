"""Türkçe ses modeli denemesi: aksan ayarlarını (cfg_weight) karşılaştırır, Whisper ile yazıya çevirip gösterir."""
import os, sys, time
import torch, librosa
from chatterbox.mtl_tts import ChatterboxMultilingualTTS
from transformers import pipeline

texts = ["Bu kırmızı!", "Pofuduk'un balonu uçup gitti.", "Aferin!", "Turuncu kalemi seç!", "Şaşkın yüzü bul!", "Üzgün birine sarılmak iyi gelir."]
model = ChatterboxMultilingualTTS.from_pretrained(device="cuda")
asr = pipeline("automatic-speech-recognition", model="openai/whisper-large-v3-turbo", torch_dtype=torch.float16, device="cuda:0")
for cfg in (0.0, 0.3):
    for ex in (0.5,):
        for text in texts:
            wav = model.generate(text, language_id="tr", exaggeration=ex, cfg_weight=cfg, temperature=0.7)
            y = librosa.resample(wav.squeeze(0).cpu().numpy(), orig_sr=model.sr, target_sr=16000)
            yt, _ = librosa.effects.trim(y, top_db=35)
            r = asr({"raw": yt, "sampling_rate": 16000}, generate_kwargs={"language": "turkish", "task": "transcribe"})
            print(f"cfg={cfg} ex={ex} | {text} | {r['text'].strip()} | {len(yt)/16000:.2f}s", flush=True)
