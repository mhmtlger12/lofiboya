using UnityEngine;

namespace LofiBoya
{
    /// <summary>Objeyi yavaşça aşağı yukarı süzdürür (bulutlar vb.).</summary>
    public class Floaty : MonoBehaviour
    {
        float _amp = 8f, _speed = 0.5f, _phase;
        Vector2 _base;
        RectTransform _rt;

        public void Init(float amplitude, float speed)
        {
            _amp = amplitude;
            _speed = speed;
        }

        void Start()
        {
            _rt = (RectTransform)transform;
            _base = _rt.anchoredPosition;
            _phase = Random.value * 10f;
        }

        void Update()
        {
            if (Motion.Reduced) return;   // sakin mod: yerinde dursun
            float t = Time.unscaledTime * _speed + _phase;
            _rt.anchoredPosition = _base + new Vector2(Mathf.Sin(t * 0.7f) * _amp * 1.5f, Mathf.Sin(t) * _amp);
        }
    }
}
