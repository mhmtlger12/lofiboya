using UnityEngine;
using UnityEngine.EventSystems;

namespace LofiBoya
{
    /// <summary>Butona basınca yumuşakça küçülür, bırakınca geri zıplar.</summary>
    public class PressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float pressedScale = 0.92f;
        float _target = 1f;
        bool _down;

        public void OnPointerDown(PointerEventData e) { _down = true; _target = pressedScale; }
        public void OnPointerUp(PointerEventData e) { if (_down) Tween.Punch(transform, 0.08f, 0.3f); _down = false; _target = 1f; }
        public void OnPointerExit(PointerEventData e) { _down = false; _target = 1f; }

        void Update()
        {
            if (!_down && Mathf.Approximately(_target, 1f)) return;
            float s = Mathf.Lerp(transform.localScale.x, _target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 22f));
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
