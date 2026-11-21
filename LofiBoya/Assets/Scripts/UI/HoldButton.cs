using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>Basılı tutunca dolan buton (ebeveyn kilidi): süre dolunca bir kez çalışır.</summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        float _need, _t;
        bool _holding, _fired;
        Action _onDone;
        Image _fill;

        public void Init(float seconds, Action onDone)
        {
            _need = seconds;
            _onDone = onDone;
            var face = transform.Find("Face");
            _fill = UIKit.Box(face != null ? face : transform, "Fill", Theme.Mint, null, 0.8f);
            _fill.raycastTarget = false;
            _fill.rectTransform.anchorMin = Vector2.zero;
            _fill.rectTransform.anchorMax = new Vector2(1, 0);
            _fill.rectTransform.pivot = new Vector2(0.5f, 0);
            _fill.rectTransform.offsetMin = _fill.rectTransform.offsetMax = Vector2.zero;
            _fill.rectTransform.SetAsFirstSibling();
        }

        public bool Fired => _fired;

        public void OnPointerDown(PointerEventData e) { _holding = true; }
        public void OnPointerUp(PointerEventData e) { _holding = false; }
        public void OnPointerExit(PointerEventData e) { _holding = false; }

        void Update()
        {
            if (_fired || _fill == null) return;
            _t = _holding ? _t + Time.unscaledDeltaTime : Mathf.Max(0, _t - Time.unscaledDeltaTime * 2f);
            float h = ((RectTransform)_fill.transform.parent).rect.height;
            _fill.rectTransform.sizeDelta = new Vector2(0, h * Mathf.Clamp01(_t / _need));
            if (_t >= _need)
            {
                _fired = true;
                _onDone?.Invoke();
            }
        }
    }
}
