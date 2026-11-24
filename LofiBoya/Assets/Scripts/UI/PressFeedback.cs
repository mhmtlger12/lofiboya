using UnityEngine;
using UnityEngine.EventSystems;

namespace LofiBoya
{
    /// <summary>Butona basınca yumuşakça küçülür, bırakınca geri zıplar.</summary>
    public class PressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float pressedScale = 0.92f;
        float _target = 1f;
        bool _down, _releasing;
        float _releasedAt = -10f;

        /// <summary>Basılı ya da bırakma zıplaması sürüyor: başka bir animasyon ölçeğe dokunmasın.</summary>
        public bool Busy => _down || _releasing || Time.unscaledTime - _releasedAt < 0.35f;

        public void OnPointerDown(PointerEventData e) { _down = true; _releasing = false; _target = pressedScale; }

        public void OnPointerUp(PointerEventData e)
        {
            if (!_down) return;
            _down = false;
            _target = 1f;
            _releasedAt = Time.unscaledTime;
            // zıplama (Punch) ölçeği 1'e getirir; sakin modda zıplama olmadığından yumuşakça geri büyür
            if (Motion.Reduced) _releasing = true;
            else Tween.Punch(transform, 0.08f, 0.3f);
        }

        // parmak butondan kayarsa buton küçük kalmasın: yumuşakça eski boyuna döner
        public void OnPointerExit(PointerEventData e)
        {
            if (!_down) return;
            _down = false;
            _target = 1f;
            _releasing = true;
        }

        void OnDisable()
        {
            if (_down || _releasing) transform.localScale = Vector3.one;
            _down = _releasing = false;
            _target = 1f;
        }

        void Update()
        {
            if (!_down && !_releasing) return;
            float s = Mathf.Lerp(transform.localScale.x, _target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 22f));
            if (_releasing && Mathf.Abs(s - 1f) < 0.002f) { s = 1f; _releasing = false; }
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
