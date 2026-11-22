using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>Bitmiş resim "canlanır": zıplar, ezilip uzar, sağa sola sallanır, arada göz kırpar gibi büzülür.</summary>
    public class Dance : MonoBehaviour
    {
        RectTransform _rt;
        Vector2 _base;
        float _t, _nextWink = 2.2f, _wink = -1;

        void Start()
        {
            _rt = (RectTransform)transform;
            _base = _rt.anchoredPosition;
        }

        void Update()
        {
            if (Motion.Reduced) return;
            float dt = Time.unscaledDeltaTime;
            _t += dt;
            float b = Mathf.Abs(Mathf.Sin(_t * 3.2f));          // zıplama
            float land = 1f - b;                                   // yere değince ezilme
            float sx = 1f + 0.035f * land * land, sy = 1f - 0.05f * land * land;
            // "göz kırpma": arada bir hızlıca dikey büzülüp açılır
            if (_t > _nextWink) { _wink = 0; _nextWink = _t + Random.Range(2.5f, 4.5f); }
            if (_wink >= 0)
            {
                _wink += dt;
                float k = _wink / 0.22f;
                if (k >= 1) _wink = -1;
                else { float s = Mathf.Sin(k * Mathf.PI); sy *= 1f - 0.12f * s; sx *= 1f + 0.05f * s; }
            }
            _rt.anchoredPosition = _base + new Vector2(0, b * 22f);
            _rt.localScale = new Vector3(sx, sy, 1);
            _rt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(_t * 1.6f) * 4f);
        }
    }

    /// <summary>Resmin etrafından yukarı süzülen kalpler ve yıldızlar.</summary>
    public class HeartRain : MonoBehaviour
    {
        float _next;
        static readonly string[] Tints = { "FF8FAB", "FFB3C6", "FF9EAA", "C3B5F0", "FFD166" };

        void Update()
        {
            if (Motion.Reduced) return;
            _next -= Time.unscaledDeltaTime;
            if (_next > 0) return;
            _next = Random.Range(0.18f, 0.4f);
            Spawn();
        }

        void Spawn()
        {
            var parent = (RectTransform)transform;
            var size = parent.rect.size;
            bool star = Random.value < 0.25f;
            var img = UIKit.Box(parent, "Float", Theme.Hex(Tints[Random.Range(0, Tints.Length)]), star ? Art.Star5 : Art.Heart);
            img.type = Image.Type.Simple;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            float s = Random.Range(46f, 92f);
            rt.sizeDelta = new Vector2(s, s);
            float side = Random.value < 0.5f ? -1f : 1f;
            var start = new Vector2(side * Random.Range(size.x * 0.3f, size.x * 0.55f), Random.Range(-size.y * 0.45f, size.y * 0.1f));
            float sway = Random.Range(0.8f, 1.6f), rise = Random.Range(260f, 420f), dur = Random.Range(1.6f, 2.4f);
            Tween.To(rt, dur, k =>
            {
                rt.anchoredPosition = start + new Vector2(Mathf.Sin(k * Mathf.PI * 2f * sway) * 26f, rise * k);
                float pop = k < 0.15f ? Tween.OutBack(k / 0.15f) : 1f;
                rt.localScale = Vector3.one * pop;
                rt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(k * 9f) * 12f);
                img.color = img.color.WithAlpha(k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f);
            }, Tween.Linear, 0f, () => { if (rt != null) Destroy(rt.gameObject); });
        }
    }
}
