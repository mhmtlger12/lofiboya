using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// Harf harf dalga: harfler soldan sağa sırayla hafifçe zıplar, sonra kısa bir mola. Yazının kendi
    /// köşelerini kaydırır (Text'in ağına dokunur), bu yüzden gölge/kontur efektlerinden ÖNCE eklenmelidir ki
    /// onlar da harfle birlikte zıplasın. Arapçada ve Hintçede harfler birbirine bağlı olduğundan dalga yapılmaz.
    /// Hareket azaltma açıksa yazı durur.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class LetterWave : BaseMeshEffect
    {
        public float Height = 14f;     // zıplama yüksekliği
        public float Step = 0.09f;     // harfler arası gecikme (sn)
        public float Hop = 0.42f;      // bir harfin zıplama süresi (sn)
        public float Rest = 1.6f;      // dalgalar arası mola (sn)

        readonly List<UIVertex> _verts = new();
        float _t0;

        protected override void OnEnable()
        {
            base.OnEnable();
            _t0 = Time.unscaledTime;
        }

        int _letters = -1;   // henüz ölçülmedi
        bool _moving = true;

        // Arapça ve Hintçe harfler birbirine bağlıdır (Hintçede üst çizgi ve işaretler): harf harf zıplatılmaz
        static bool Still => Motion.Reduced || Loc.IsArabic || Loc.IsHindi;

        void Update()
        {
            if (Still) { if (_moving) { _moving = false; graphic.SetVerticesDirty(); } return; }
            // molada yazı durur: ağ yeniden kurulmaz (tuvali her kare kirletmesin); dalga bitince bir kez düzlenir
            float cycle = _letters * Step + Hop + Rest;
            bool moving = _letters < 0 || (Time.unscaledTime - _t0) % cycle < _letters * Step + Hop;
            if (moving || _moving) graphic.SetVerticesDirty();
            _moving = moving;
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || Still) return;
            _verts.Clear();
            vh.GetUIVertexStream(_verts);
            int letters = _letters = _verts.Count / 6;
            if (letters == 0) return;
            float cycle = letters * Step + Hop + Rest;
            float t = (Time.unscaledTime - _t0) % cycle;
            for (int i = 0; i < letters; i++)
            {
                float k = (t - i * Step) / Hop;
                if (k <= 0f || k >= 1f) continue;
                float up = Mathf.Sin(k * Mathf.PI);           // yumuşak yukarı-aşağı
                float squash = Mathf.Sin(k * Mathf.PI * 2f);   // kalkarken uzar, inerken yassılır
                var c = Vector3.zero;
                for (int v = 0; v < 6; v++) c += _verts[i * 6 + v].position;
                c /= 6f;
                for (int v = 0; v < 6; v++)
                {
                    var vert = _verts[i * 6 + v];
                    var p = vert.position - c;
                    p.x *= 1f - 0.06f * squash;
                    p.y *= 1f + 0.08f * squash;
                    vert.position = c + p + new Vector3(0, Height * up, 0);
                    _verts[i * 6 + v] = vert;
                }
            }
            vh.Clear();
            vh.AddUIVertexTriangleStream(_verts);
        }
    }
}
