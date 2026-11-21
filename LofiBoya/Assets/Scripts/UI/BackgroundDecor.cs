using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>Arka planda yavaşça süzülen pastel baloncuklar ve yukarı yükselen minik ışıltılar.</summary>
    public class BackgroundDecor : MonoBehaviour
    {
        class Blob { public RectTransform rt; public Vector2 basePos; public float speed, phase, amp; }
        class Mote { public RectTransform rt; public Image img; public float speed, phase, drift; }

        readonly List<Blob> _blobs = new();
        readonly List<Mote> _motes = new();
        RectTransform _rt;

        void Start()
        {
            _rt = (RectTransform)transform;
            var rnd = new System.Random(7);
            for (int i = 0; i < 7; i++)
            {
                var img = UIKit.Box(transform, "Blob", Theme.Blobs[i % Theme.Blobs.Length].WithAlpha(0.55f), UIKit.Blob);
                img.type = Image.Type.Simple;
                float size = 420 + (float)rnd.NextDouble() * 520;
                var rt = img.rectTransform;
                var anchor = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble());
                rt.Place(anchor, Vector2.zero, new Vector2(size, size));
                _blobs.Add(new Blob
                {
                    rt = rt, basePos = Vector2.zero,
                    speed = 0.08f + (float)rnd.NextDouble() * 0.1f,
                    phase = (float)rnd.NextDouble() * 10f,
                    amp = 60 + (float)rnd.NextDouble() * 90
                });
            }
            for (int i = 0; i < 18; i++)
            {
                var img = UIKit.Box(transform, "Mote", Color.white.WithAlpha(0.8f), UIKit.Blob);
                img.type = Image.Type.Simple;
                float s = 10 + (float)rnd.NextDouble() * 18;
                img.rectTransform.Place(new Vector2((float)rnd.NextDouble(), 0), new Vector2(0, (float)rnd.NextDouble() * 2000), new Vector2(s, s));
                _motes.Add(new Mote
                {
                    rt = img.rectTransform, img = img,
                    speed = 25 + (float)rnd.NextDouble() * 45,
                    phase = (float)rnd.NextDouble() * 10f,
                    drift = 20 + (float)rnd.NextDouble() * 40
                });
            }
        }

        void Update()
        {
            float t = Time.unscaledTime;
            foreach (var b in _blobs)
            {
                b.rt.anchoredPosition = new Vector2(Mathf.Sin(t * b.speed + b.phase) * b.amp, Mathf.Cos(t * b.speed * 0.8f + b.phase * 1.3f) * b.amp);
                float s = 1f + 0.06f * Mathf.Sin(t * b.speed * 1.7f + b.phase);
                b.rt.localScale = new Vector3(s, s, 1);
            }
            float h = _rt.rect.height + 80;
            foreach (var m in _motes)
            {
                var p = m.rt.anchoredPosition;
                p.y += m.speed * Time.unscaledDeltaTime;
                if (p.y > h) p.y = -40;
                p.x = Mathf.Sin(t * 0.5f + m.phase) * m.drift;
                m.rt.anchoredPosition = p;
                m.img.color = Color.white.WithAlpha(0.35f + 0.45f * (0.5f + 0.5f * Mathf.Sin(t * 1.8f + m.phase)));
            }
        }
    }
}
