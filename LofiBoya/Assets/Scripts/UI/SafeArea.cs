using UnityEngine;

namespace LofiBoya
{
    /// <summary>Çentikli telefonlarda içeriği güvenli alana sığdırır.</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        Rect _last;
        Vector2Int _lastScreen;

        void Update()
        {
            var sa = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (sa == _last && screen == _lastScreen) return;
            _last = sa;
            _lastScreen = screen;
            if (screen.x == 0 || screen.y == 0) return;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(sa.xMin / screen.x, sa.yMin / screen.y);
            rt.anchorMax = new Vector2(sa.xMax / screen.x, sa.yMax / screen.y);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
