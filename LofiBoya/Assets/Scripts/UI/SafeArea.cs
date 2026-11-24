using UnityEngine;

namespace LofiBoya
{
    /// <summary>
    /// Çentikli telefonlarda içeriği güvenli alana sığdırır.
    /// Ekran dikey tasarımdan çok genişse (tablet yatay) içerik ortada dikey bir sütun olur:
    /// arka plan tüm ekranı kaplar, butonlar ve oyun alanı tasarlandığı oranda kalır.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        /// <summary>Sütunun en geniş oranı (genişlik / yükseklik). Tasarım 9:16 = 0.56; 0.8 tablette rahat bir sütun.</summary>
        public const float MaxAspect = 0.8f;

        Rect _last;
        Vector2Int _lastScreen;

        // ilk karede de doğru yerleşim (yalnızca Update'te yapılınca açılış karesinde içerik sıçrıyordu)
        void Awake() => Apply();
        void Update() => Apply();

        void Apply()
        {
            var sa = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (sa == _last && screen == _lastScreen) return;
            _last = sa;
            _lastScreen = screen;
            if (screen.x == 0 || screen.y == 0) return;

            // yatay ekranda ortalanmış dikey sütun
            float maxW = sa.height * MaxAspect;
            if (sa.width > maxW)
            {
                float cut = (sa.width - maxW) * 0.5f;
                sa = new Rect(sa.x + cut, sa.y, maxW, sa.height);
            }

            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(sa.xMin / screen.x, sa.yMin / screen.y);
            rt.anchorMax = new Vector2(sa.xMax / screen.x, sa.yMax / screen.y);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
