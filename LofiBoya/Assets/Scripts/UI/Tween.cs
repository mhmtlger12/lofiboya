using System;
using System.Collections;
using UnityEngine;

namespace LofiBoya
{
    /// <summary>Küçük, bağımlılıksız animasyon yardımcısı. Hedef obje yok olursa animasyon sessizce durur.</summary>
    public class Tween : MonoBehaviour
    {
        static Tween _runner;
        static Tween Runner
        {
            get
            {
                if (_runner == null)
                {
                    var go = new GameObject("[Tween]");
                    DontDestroyOnLoad(go);
                    _runner = go.AddComponent<Tween>();
                }
                return _runner;
            }
        }

        public static Coroutine Run(IEnumerator routine) => Runner.StartCoroutine(routine);
        public static void Stop(Coroutine c) { if (c != null && _runner != null) _runner.StopCoroutine(c); }

        public static Coroutine To(UnityEngine.Object owner, float duration, Action<float> step, Func<float, float> ease = null, float delay = 0f, Action done = null)
            => Run(ToRoutine(owner, duration, step, ease ?? OutCubic, delay, done));

        static IEnumerator ToRoutine(UnityEngine.Object owner, float duration, Action<float> step, Func<float, float> ease, float delay, Action done)
        {
            if (delay > 0) yield return new WaitForSecondsRealtime(delay);
            float t = 0;
            while (t < duration)
            {
                if (owner == null) yield break;
                step(ease(t / duration));
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (owner == null) yield break;
            step(ease(1f));
            done?.Invoke();
        }

        public static Coroutine Delay(float seconds, Action action) => Run(DelayRoutine(seconds, action));
        static IEnumerator DelayRoutine(float s, Action a) { yield return new WaitForSecondsRealtime(s); a?.Invoke(); }

        // ---------- Hazır animasyonlar ----------

        public static Coroutine Pop(Transform t, float delay = 0f, float duration = 0.45f)
        {
            t.localScale = Vector3.zero;
            return To(t, duration, k => t.localScale = Vector3.one * k, OutBack, delay);
        }

        public static Coroutine Fade(CanvasGroup g, float from, float to, float duration = 0.3f, Action done = null)
        {
            g.alpha = from;
            return To(g, duration, k => g.alpha = Mathf.Lerp(from, to, k), InOutSine, 0f, done);
        }

        public static Coroutine Punch(Transform t, float amount = 0.12f, float duration = 0.35f)
            => To(t, duration, k => t.localScale = Vector3.one * (1f + amount * Mathf.Sin(k * Mathf.PI) * (1f - k * 0.5f)), Linear);

        // ---------- Easing ----------
        public static float Linear(float x) => x;
        public static float OutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
        public static float InOutSine(float x) => -(Mathf.Cos(Mathf.PI * x) - 1f) / 2f;
        public static float OutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
