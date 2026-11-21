"""Deneme seslerini Whisper ile yazıya çevirir (üretilen sesin doğru okunup okunmadığını görmek için)."""
import glob, os, sys
import torch, librosa
from transformers import pipeline

asr = pipeline("automatic-speech-recognition", model="openai/whisper-large-v3-turbo",
               torch_dtype=torch.float16, device="cuda:0")
folder = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), "ses_deneme")
for f in sorted(glob.glob(os.path.join(folder, "*.wav"))):
    y, _ = librosa.load(f, sr=16000)
    r = asr({"raw": y, "sampling_rate": 16000}, generate_kwargs={"language": "turkish", "task": "transcribe"})
    print(os.path.basename(f), "->", r["text"].strip())
