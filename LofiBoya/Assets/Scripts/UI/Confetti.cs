using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>Pastel konfeti patlaması (yalnızca UI Image, parçacık sistemi yok).</summary>
    public class Confetti : MonoBehaviour
    {
        class Piece { public RectTransform rt; public Image img; public Vector2 vel; public float spin, life, wobble; }
        readonly List<Piece> _pieces = new();
        float _age;

        public static void Burst(Transform parent, int count)
        {
            if (SaveSystem.CalmMode) return;   // sakin mod: konfeti yok
            var rt = UIKit.Node("Confetti", parent).Stretch();
            rt.gameObject.AddComponent<Confetti>().Spawn(count);
        }

        void Spawn(int count)
        {
            var colors = new[] { Theme.Accent, Theme.Lavender, Theme.Mint, Theme.Butter, Theme.Sky, Theme.Hex("FFC6FF") };
            for (int i = 0; i < count; i++)
            {
                bool round = Random.value < 0.4f;
                var img = UIKit.Box(transform, "P", colors[i % colors.Length], round ? UIKit.Circle : UIKit.Rounded, round ? 1f : 4f);
                if (round) img.type = Image.Type.Simple;
                var size = round ? Vector2.one * Random.Range(18f, 30f) : new Vector2(Random.Range(16f, 26f), Random.Range(30f, 46f));
                float side = i % 2 == 0 ? -1 : 1;
                img.rectTransform.Place(new Vector2(0.5f, 0.42f), new Vector2(side * Random.Range(0f, 60f), 0), size);
                float ang = Random.Range(55f, 125f) * Mathf.Deg2Rad;
                float speed = Random.Range(1300f, 2300f);
                _pieces.Add(new Piece
                {
                    rt = img.rectTransform, img = img,
                    vel = new Vector2(Mathf.Cos(ang) * speed * 0.9f, Mathf.Sin(ang) * speed),
                    spin = Random.Range(-540f, 540f),
                    life = Random.Range(2.2f, 3.2f),
                    wobble = Random.Range(0f, 10f)
                });
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _age += dt;
            foreach (var p in _pieces)
            {
                p.vel.y -= 2600f * dt;
                p.vel *= 1f - 1.6f * dt;
                p.vel.y = Mathf.Max(p.vel.y, -520f);
                p.rt.anchoredPosition += (p.vel + new Vector2(Mathf.Sin(_age * 6f + p.wobble) * 90f, 0)) * dt;
                p.rt.localRotation = Quaternion.Euler(0, 0, p.rt.localEulerAngles.z + p.spin * dt);
                p.rt.localScale = new Vector3(Mathf.Cos(_age * 5f + p.wobble), 1, 1);
                var c = p.img.color;
                c.a = Mathf.Clamp01((p.life - _age) / 0.6f);
                p.img.color = c;
            }
            if (_age > 3.4f) Destroy(gameObject);
        }
    }
}
