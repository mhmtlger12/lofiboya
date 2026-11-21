using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Boyama arkadaşı Pofuduk: sol alttan süzülerek gelir, konuşma balonuyla kısa bir şey söyler,
    /// birkaç saniye sonra geri çekilir (çizimi kapatmaz). Not vermez, yalnızca cesaretlendirir.
    /// </summary>
    public class Buddy : MonoBehaviour
    {
        RectTransform _rt, _bubble;
        Text _bubbleText;
        Mascot _mascot;
        Coroutine _anim;
        float _hideAt = -1;
        const float Size = 200f, ShownX = 30f, HiddenX = -260f;

        public static Buddy Create(RectTransform parent, float bottomY)
        {
            var rt = UIKit.Node("Buddy", parent);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.sizeDelta = new Vector2(Size, Size * 0.875f);
            rt.anchoredPosition = new Vector2(HiddenX, bottomY);
            var b = rt.gameObject.AddComponent<Buddy>();
            b._rt = rt;
            b._mascot = Mascot.Create(rt, Size, true);
            var mrt = (RectTransform)b._mascot.transform;
            mrt.anchorMin = mrt.anchorMax = new Vector2(0.5f, 0f);
            mrt.pivot = new Vector2(0.5f, 0f);
            mrt.anchoredPosition = Vector2.zero;

            // konuşma balonu (sağ üstte)
            var bub = UIKit.Card(rt, "Bubble", Color.white, 0.6f, 4f);
            b._bubble = (RectTransform)bub.transform.parent;
            b._bubble.anchorMin = b._bubble.anchorMax = new Vector2(1f, 1f);
            b._bubble.pivot = new Vector2(0f, 0f);
            b._bubble.anchoredPosition = new Vector2(-30, -20);
            b._bubble.sizeDelta = new Vector2(380, 130);
            var tail = UIKit.Box(bub.transform, "Tail", Color.white, UIKit.Circle);
            tail.type = Image.Type.Simple;
            tail.rectTransform.Place(new Vector2(0.1f, 0f), new Vector2(0, -10), new Vector2(36, 36));
            b._bubbleText = UIKit.Label(bub.transform, "", 34, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            b._bubbleText.rectTransform.Stretch(14, 8, 14, 8);
            b._bubbleText.resizeTextForBestFit = true;
            b._bubbleText.resizeTextMinSize = 22;
            b._bubbleText.resizeTextMaxSize = 34;
            b._bubble.gameObject.SetActive(false);
            rt.gameObject.SetActive(false);
            return b;
        }

        public void SetHat(MascotHat h) => _mascot.SetHat(h);

        public bool Visible => gameObject.activeSelf;

        /// <summary>Gelir, (varsa) söyler, seconds sonra gider.</summary>
        public void Peek(string text, float seconds = 3.2f, bool speak = true)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Tween.Stop(_anim);
            float from = _rt.anchoredPosition.x;
            _anim = Tween.To(_rt, 0.45f, k => _rt.anchoredPosition = new Vector2(Mathf.LerpUnclamped(from, ShownX, k), _rt.anchoredPosition.y), Tween.OutBack);
            _mascot.Jump();
            _mascot.Wave(1.4f);
            bool hasText = !string.IsNullOrEmpty(text);
            _bubble.gameObject.SetActive(hasText);
            if (hasText)
            {
                _bubbleText.text = text;
                _bubble.localScale = Vector3.zero;
                Tween.Pop(_bubble, 0.25f, 0.4f);
                if (speak) Speech.Say(text);
            }
            _hideAt = Time.unscaledTime + seconds;
        }

        void Update()
        {
            if (_hideAt > 0 && Time.unscaledTime > _hideAt)
            {
                _hideAt = -1;
                Tween.Stop(_anim);
                _bubble.gameObject.SetActive(false);
                float from = _rt.anchoredPosition.x;
                _anim = Tween.To(_rt, 0.4f, k => _rt.anchoredPosition = new Vector2(Mathf.Lerp(from, HiddenX, k), _rt.anchoredPosition.y), Tween.InOutSine,
                    0f, () => { if (this != null) gameObject.SetActive(false); });
            }
        }

        /// <summary>Resmin konusuna göre şapka (meslekler).</summary>
        public static MascotHat HatFor(string id)
        {
            if (id == null) return MascotHat.None;
            if (id.Contains("-doctor-")) return MascotHat.Doctor;
            if (id.Contains("-chef-")) return MascotHat.Chef;
            if (id.Contains("-firefighter-")) return MascotHat.Firefighter;
            if (id.Contains("-astronaut-") || id.Contains("-space-") || id.Contains("-rocket-")) return MascotHat.Astronaut;
            return MascotHat.None;
        }
    }
}
