using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Pofuduk: nefes alır gibi süzülür, göz kırpar, ara sıra el sallar.
    /// Dokununca kıkırdar ve zıplar. Jump() ile sevinir.
    /// </summary>
    public class Mascot : MonoBehaviour, IPointerClickHandler
    {
        RectTransform _root, _body, _hand, _crayon;
        Image _eyes;
        float _t, _nextBlink, _blinkUntil, _waveUntil, _nextWave, _jumpT = -1, _happyUntil;
        float _size;
        Image _bodyImg, _hit;
        MascotMood _mood = MascotMood.Normal;
        Image _hat;

        /// <summary>Sabit bir duygu yüzü (Normal = canlı, göz kırpan hâl).</summary>
        public void SetMood(MascotMood m)
        {
            _mood = m;
            _bodyImg.sprite = m == MascotMood.Normal ? MascotArt.Body : MascotArt.BodyPlain;
            if (m != MascotMood.Normal) _eyes.sprite = MascotArt.Face(m);
        }

        /// <summary>Başa aksesuar takar (None = çıkar).</summary>
        public void SetHat(MascotHat h)
        {
            if (_hat != null) { Destroy(_hat.gameObject); _hat = null; }
            if (h == MascotHat.None) return;
            _hat = UIKit.Box(_body, "Hat", Color.white, MascotArt.Hat(h));
            _hat.type = Image.Type.Simple;
            _hat.raycastTarget = false;
            var rt = _hat.rectTransform;
            if (h == MascotHat.Astronaut)
            {
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(-_size * 0.06f, -_size * 0.02f);
                rt.offsetMax = new Vector2(_size * 0.06f, _size * 0.12f);
            }
            else
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.80f);
                rt.pivot = new Vector2(0.5f, 0.12f);
                rt.sizeDelta = new Vector2(_size * 0.42f, _size * 0.42f);
                rt.localRotation = Quaternion.Euler(0, 0, -8f);
            }
        }

        /// <summary>false: dokununca kıkırdamaz, dokunuşlar alttaki butona geçer (oyunlarda seçenek olarak).</summary>
        public void SetInteractive(bool on)
        {
            // Yalnızca dokunma alanını değil, gövde/göz/el resimlerini de kapat:
            // yoksa dokunuş Pofuduk'ta kalır, alttaki seçenek butonuna hiç ulaşmaz.
            foreach (var g in GetComponentsInChildren<Graphic>(true)) g.raycastTarget = on && g == _hit;
            _hit.raycastTarget = on;
        }

        static string TapLine() => Loc.T("mascot_" + Random.Range(1, 6));

        /// <summary>Maskotu oluşturur. size = genişlik (kanvas birimi).</summary>
        public static Mascot Create(Transform parent, float size, bool holdCrayon = true)
        {
            var rt = UIKit.Node("Pofuduk", parent);
            rt.sizeDelta = new Vector2(size, size * 0.875f);
            var m = rt.gameObject.AddComponent<Mascot>();
            m.Build(size, holdCrayon);
            return m;
        }

        void Build(float size, bool holdCrayon)
        {
            _size = size;
            _root = (RectTransform)transform;
            var hit = _root.gameObject.AddComponent<Image>();
            hit.color = Color.clear; // dokunma alanı
            _hit = hit;

            _body = UIKit.Node("Body", _root);
            _body.anchorMin = _body.anchorMax = new Vector2(0.5f, 0f);
            _body.pivot = new Vector2(0.5f, 0f);
            _body.sizeDelta = new Vector2(size, size * 0.875f);

            if (holdCrayon)
            {
                // elinde boya kalemi (gövdenin arkasında kalan kısmı)
                var cr = UIKit.Box(_body, "Crayon", Theme.Accent, Art.CrayonBody);
                cr.type = Image.Type.Simple;
                _crayon = cr.rectTransform;
                _crayon.anchorMin = _crayon.anchorMax = new Vector2(0.86f, 0.30f);
                _crayon.pivot = new Vector2(0.5f, 0.1f);
                _crayon.sizeDelta = new Vector2(size * 0.09f, size * 0.36f);
                _crayon.localRotation = Quaternion.Euler(0, 0, -28);
                var sh = UIKit.Box(_crayon, "Shine", Color.white, Art.CrayonShine);
                sh.type = Image.Type.Simple;
                sh.rectTransform.Stretch();
            }

            var body = UIKit.Box(_body, "Cloud", Color.white, MascotArt.Body);
            body.type = Image.Type.Simple;
            _bodyImg = body;
            body.rectTransform.Stretch();
            _eyes = UIKit.Box(_body, "Eyes", Color.white, MascotArt.EyesOpen);
            _eyes.type = Image.Type.Simple;
            _eyes.rectTransform.Stretch();

            // sallanan el (sol tarafta)
            var hand = UIKit.Box(_body, "Hand", Color.white, MascotArt.Hand);
            hand.type = Image.Type.Simple;
            _hand = hand.rectTransform;
            _hand.anchorMin = _hand.anchorMax = new Vector2(0.08f, 0.30f);
            _hand.sizeDelta = new Vector2(size * 0.16f, size * 0.16f);

            if (holdCrayon)
            {
                // kalemi tutan küçük el (önde)
                var grip = UIKit.Box(_body, "Grip", Color.white, MascotArt.Hand);
                grip.type = Image.Type.Simple;
                grip.rectTransform.anchorMin = grip.rectTransform.anchorMax = new Vector2(0.86f, 0.30f);
                grip.rectTransform.sizeDelta = new Vector2(size * 0.15f, size * 0.15f);
            }

            _nextBlink = Random.Range(1.5f, 3.5f);
            _nextWave = Random.Range(2f, 4f);
        }

        public void Wave(float seconds = 1.6f) => _waveUntil = _t + seconds;

        public void Jump()
        {
            _jumpT = 0;
            _happyUntil = _t + 1.2f;
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (!_hit.raycastTarget) return;
            Sfx.Giggle();
            Jump();
            Wave(1.2f);
            Speech.Say(TapLine());
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _t += dt;
            bool still = Motion.Reduced;

            // nefes alma: hafif ezilip uzama
            float breathe = Mathf.Sin(_t * 2.2f);
            float sx = 1f + 0.025f * breathe, sy = 1f - 0.025f * breathe;
            float y = Mathf.Sin(_t * 1.3f) * _size * 0.02f;

            // zıplama
            if (_jumpT >= 0)
            {
                _jumpT += dt;
                float k = _jumpT / 0.55f;
                if (k >= 1f) _jumpT = -1;
                else
                {
                    y += Mathf.Sin(k * Mathf.PI) * _size * 0.22f;
                    float squash = k < 0.15f ? Mathf.Sin(k / 0.15f * Mathf.PI) * 0.12f : 0f;
                    sx += squash; sy -= squash;
                }
            }
            if (still) { y = 0; sx = sy = 1f; }   // hareket azaltma: yerinde dursun, yüzü yine güler
            _body.anchoredPosition = new Vector2(0, y);
            _body.localScale = new Vector3(sx, sy, 1);

            // gözler: mutlu / kırpma / açık (sabit duygu yüzünde dokunma)
            if (_mood != MascotMood.Normal) { }
            else if (_t < _happyUntil) _eyes.sprite = MascotArt.EyesHappy;
            else if (_t < _blinkUntil) _eyes.sprite = MascotArt.EyesClosed;
            else _eyes.sprite = MascotArt.EyesOpen;
            if (_t > _nextBlink)
            {
                _blinkUntil = _t + 0.13f;
                _nextBlink = _t + Random.Range(2f, 5f);
            }

            // el sallama
            if (_t > _nextWave) { if (!still) Wave(); _nextWave = _t + Random.Range(5f, 9f); }
            float wave = _t < _waveUntil && !still ? Mathf.Sin(_t * 14f) : 0f;
            _hand.anchoredPosition = new Vector2(-_size * 0.03f, _size * (0.02f + 0.08f * Mathf.Abs(wave)) + (_t < _waveUntil ? _size * 0.08f : 0f));
            _hand.localRotation = Quaternion.Euler(0, 0, wave * 25f);

            if (_crayon != null) _crayon.localRotation = Quaternion.Euler(0, 0, -28 + (still ? 0f : Mathf.Sin(_t * 1.7f) * 4f));
        }
    }
}
