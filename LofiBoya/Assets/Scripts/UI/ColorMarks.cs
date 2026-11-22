using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Renk körü desteği: her renk ailesinin kendine özel küçük bir işareti olur (kırmızı daire, turuncu üçgen...).
    /// Ayarlardan açılır; kapalıyken hiçbir şey eklenmez.
    /// </summary>
    public static class ColorMarks
    {
        static Sprite ForKey(string key) => key switch
        {
            "red" => Art.Shape("circle"),
            "orange" => Art.Shape("triangle"),
            "yellow" => Art.Shape("star"),
            "green" => Art.Shape("square"),
            "blue" => Art.Shape("heart"),
            "purple" => Art.Shape("rectangle"),
            "pink" => Art.Flower,
            "brown" => Art.Balloon,
            "turquoise" => Art.CloudShape,
            "navy" => Art.Note,
            _ => null,
        };

        /// <summary>Temel renk sırası (Names.Basic) ile.</summary>
        static readonly string[] BasicKeys = { "red", "orange", "yellow", "green", "blue", "purple", "pink", "brown" };

        public static void AddBasic(RectTransform target, int basicIndex, float size, float anchorY = 0.3f)
        {
            if (basicIndex >= 0 && basicIndex < BasicKeys.Length) Add(target, ForKey(BasicKeys[basicIndex]), size, anchorY);
        }

        public static void AddFor(RectTransform target, Color c, float size, float anchorY = 0.3f) =>
            Add(target, ForKey(Names.ColorKey(c)), size, anchorY);

        static void Add(RectTransform target, Sprite sprite, float size, float anchorY)
        {
            if (!SaveSystem.ColorAssist || sprite == null) return;
            var badge = UIKit.Box(target, "Mark", Color.white, UIKit.Circle);
            badge.type = Image.Type.Simple;
            badge.raycastTarget = false;
            var rt = badge.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, anchorY);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(size, size);
            var icon = UIKit.Box(rt, "Icon", Theme.Text, sprite);
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.rectTransform.Stretch(size * 0.2f, size * 0.2f, size * 0.2f, size * 0.2f);
        }
    }
}
