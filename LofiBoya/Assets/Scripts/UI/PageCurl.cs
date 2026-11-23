using UnityEngine;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>
    /// 3B sayfa kıvrılması. Yöntem GrabFold'dan uyarlandı (github.com/JavocSoft/grabfold, MIT lisansı):
    ///  • Sayfa, çocuğun tuttuğu noktadan katlanır: kat çizgisi, tutma noktası (G0) ile parmağın şu anki yeri (G)
    ///    arasındaki doğrunun orta dikmesidir; parmak yukarı/aşağı gidince kat eğilir.
    ///  • Katın ötesi bir silindire sarılır (yarıçap R), böylece sayfa kırılmaz, kâğıt gibi kıvrılır. Sayfa döndükçe
    ///    R küçülür, dönen yaprak düzleşerek öbür yana yatar.
    ///  • Sayfa sırta bağlıdır: G, sırtın alt ve üst köşesine tutma noktasından daha uzağa gidemez (kâğıt yırtılmaz).
    /// Sayfa koordinatları: sol alt (0,0), sırt x=0, sağ üst (W,H).
    /// </summary>
    public class PageCurl
    {
        public float W = 1, H = 1;
        public Vector2 G0, G;
        public bool Active;

        /// <summary>0: sayfa yerinde, 1: tamamen döndü.</summary>
        public float Progress => !Active || G0.x <= 0 ? 0f : Mathf.Clamp01((G0.x - G.x) / (2f * G0.x));

        // her güncellemede hesaplanan kat bilgisi
        Vector2 _n, _m;
        float _r;

        public float Radius => _r;
        public Vector2 Normal => _n;
        public Vector2 LinePoint => _m;

        /// <summary>G'yi sırta bağlı tutar ve kat çizgisini hesaplar.</summary>
        public void Update()
        {
            if (!Active) return;
            G.y = Mathf.Clamp(G.y, -0.35f * H, 1.35f * H);
            var sb = new Vector2(0, 0);
            var st = new Vector2(0, H);
            float rb = (G0 - sb).magnitude, rt = (G0 - st).magnitude;
            for (int i = 0; i < 4; i++)
            {
                var d = G - sb;
                if (d.magnitude > rb) G = sb + d.normalized * rb;
                d = G - st;
                if (d.magnitude > rt) G = st + d.normalized * rt;
            }
            var delta = G0 - G;
            float len = delta.magnitude;
            if (len < 0.5f)
            {
                _n = Vector2.right;
                _r = 0;
                _m = G0 + Vector2.right * 10f;
                return;
            }
            _n = delta / len;
            float p = Progress;
            float rmax = W * 0.12f * Mathf.Pow(1f - p, 0.8f);
            _r = Mathf.Min(rmax, len / Mathf.PI);
            // silindirle sarılınca tutulan nokta tam parmağın altına gelsin diye çizgi πR/2 kadar kayar
            _m = (G0 + G) * 0.5f - _n * (Mathf.PI * _r * 0.5f);
        }

        /// <summary>
        /// Sayfadaki bir noktanın 3B yeri. theta: silindir üzerindeki açı (0 düz, π tam ters döndü).
        /// </summary>
        public Vector3 Map(Vector2 p, out float theta)
        {
            theta = 0;
            if (!Active || _r <= 0 && (G0 - G).sqrMagnitude < 0.25f) return new Vector3(p.x, p.y, 0);
            float d = Vector2.Dot(p - _m, _n);
            if (d <= 0) return new Vector3(p.x, p.y, 0);
            var basePt = p - _n * d;
            if (_r < 1e-3f)
            {
                theta = Mathf.PI;
                var q = basePt - _n * d;
                return new Vector3(q.x, q.y, 0.5f);
            }
            float arc = Mathf.PI * _r;
            if (d < arc)
            {
                theta = d / _r;
                var q = basePt + _n * (_r * Mathf.Sin(theta));
                return new Vector3(q.x, q.y, _r * (1f - Mathf.Cos(theta)));
            }
            theta = Mathf.PI + (d - arc) / Mathf.Max(1f, W);   // düz arka yüz: π'den biraz büyük
            var r = basePt - _n * (d - arc);
            return new Vector3(r.x, r.y, 2f * _r);
        }

        /// <summary>Hafif perspektif: kalkan kâğıt bakana doğru büyür.</summary>
        public Vector2 Project(Vector3 v)
        {
            float D = H * 2.6f;
            float f = D / Mathf.Max(D * 0.2f, D - v.z);
            var c = new Vector2(W * 0.5f, H * 0.5f);
            return c + (new Vector2(v.x, v.y) - c) * f;
        }
    }

    /// <summary>
    /// Kıvrılan sayfanın bir katmanı. Aynı ızgara her katmanda farklı süzülür:
    ///  Flat: düz duran ön yüz • Curl: kıvrılan ön yüz • Back: arka yüz (kâğıt) • Ghost: arkadan soluk görünen baskı
    ///  BackShadow: dönen yaprağın alttaki sayfaya düşen gölgesi
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class PageMesh : MaskableGraphic
    {
        public enum Part { Flat, Curl, Back, Ghost, BackShadow, Under }
        public Part Kind;
        [System.NonSerialized] public PageCurl Curl;
        Texture _tex;
        public Texture Tex { get => _tex; set { if (_tex == value) return; _tex = value; SetMaterialDirty(); } }
        public override Texture mainTexture => _tex != null ? _tex : s_WhiteTexture;

        const int NX = 34, NY = 48;
        static readonly Vector2[] _proj = new Vector2[(NX + 1) * (NY + 1)];
        static readonly float[] _theta = new float[(NX + 1) * (NY + 1)];
        static readonly Vector2[] _uv = new Vector2[(NX + 1) * (NY + 1)];

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Curl == null) return;
            float w = Curl.W, h = Curl.H;
            bool active = Curl.Active && Kind != Part.Under;

            // Durağan sayfa (ya da alttaki sayfa): tek dörtgen yeter
            if (!active)
            {
                if (Kind != Part.Flat && Kind != Part.Under) return;
                var c = (Color32)color;
                vh.AddVert(new Vector3(0, 0), c, new Vector2(0, 0));
                vh.AddVert(new Vector3(0, h), c, new Vector2(0, 1));
                vh.AddVert(new Vector3(w, h), c, new Vector2(1, 1));
                vh.AddVert(new Vector3(w, 0), c, new Vector2(1, 0));
                vh.AddTriangle(0, 1, 2);
                vh.AddTriangle(2, 3, 0);
                return;
            }

            // ızgarayı hesapla (her katman aynı sonucu bulur)
            for (int j = 0; j <= NY; j++)
            for (int i = 0; i <= NX; i++)
            {
                int k = j * (NX + 1) + i;
                var p = new Vector2(w * i / NX, h * j / NY);
                var v = Curl.Map(p, out float th);
                _proj[k] = Curl.Project(v);
                _theta[k] = th;
                _uv[k] = new Vector2((float)i / NX, (float)j / NY);
            }

            Vector2 shadowOff = Kind == Part.BackShadow ? -Curl.Normal * (w * 0.025f) + new Vector2(0, -w * 0.012f) : Vector2.zero;
            var baseCol = color;
            int vi = 0;
            for (int j = 0; j < NY; j++)
            for (int i = 0; i < NX; i++)
            {
                int a = j * (NX + 1) + i, b = a + (NX + 1), c = b + 1, d = a + 1;
                Tri(vh, a, b, c, ref vi, baseCol, shadowOff);
                Tri(vh, a, c, d, ref vi, baseCol, shadowOff);
            }
        }

        void Tri(VertexHelper vh, int a, int b, int c, ref int vi, Color baseCol, Vector2 off)
        {
            var pa = _proj[a]; var pb = _proj[b]; var pc = _proj[c];
            float area = (pb.x - pa.x) * (pc.y - pa.y) - (pb.y - pa.y) * (pc.x - pa.x);
            bool front = area < 0;   // düz sayfada üçgenler bu yönde
            bool curled = _theta[a] > 0 || _theta[b] > 0 || _theta[c] > 0;
            switch (Kind)
            {
                case Part.Flat: if (!front || curled) return; break;
                case Part.Curl: if (!front || !curled) return; break;
                default: if (front || !curled) return; break;   // Back, Ghost, BackShadow
            }
            Vert(vh, a, baseCol, off);
            Vert(vh, b, baseCol, off);
            Vert(vh, c, baseCol, off);
            vh.AddTriangle(vi, vi + 1, vi + 2);
            vi += 3;
        }

        void Vert(VertexHelper vh, int k, Color baseCol, Vector2 off)
        {
            float th = _theta[k];
            float s;
            switch (Kind)
            {
                case Part.Flat:
                    s = 1f;
                    break;
                case Part.Curl:
                {
                    // ışık önden-üstten: kıvrıldıkça kâğıt kararır, tepede ince bir parlama
                    float k1 = Mathf.Sin(Mathf.Min(th, Mathf.PI * 0.5f) * 0.5f);
                    s = 1f - 0.5f * k1 * k1 * 2f * 0.55f;
                    break;
                }
                case Part.Back:
                case Part.Ghost:
                {
                    if (th >= Mathf.PI) s = 0.97f;
                    else
                    {
                        float t = Mathf.InverseLerp(Mathf.PI * 0.5f, Mathf.PI, th);
                        s = Mathf.Lerp(0.72f, 0.97f, t) + 0.12f * Mathf.Exp(-Mathf.Pow((th - 2.45f) / 0.28f, 2f));
                    }
                    break;
                }
                default:
                    s = 1f;
                    break;
            }
            var col = Kind == Part.BackShadow ? baseCol : new Color(baseCol.r * s, baseCol.g * s, baseCol.b * s, baseCol.a);
            vh.AddVert(_proj[k] + off, col, _uv[k]);
        }
    }

    /// <summary>
    /// Kat çizgisi boyunca yumuşak gölge şeridi. Under: kıvrımın alttaki sayfaya düşen gölgesi.
    /// Crease: kıvrımın dibinde, kalkan kâğıdın altında kalan karanlık.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class CurlShadow : MaskableGraphic
    {
        [System.NonSerialized] public PageCurl Curl;
        public bool Under;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Curl == null || !Curl.Active || Curl.Radius <= 0) return;
            var n = Curl.Normal;
            var t = new Vector2(-n.y, n.x);
            var m = Curl.LinePoint;
            float L = (Curl.W + Curl.H) * 2f;
            float r = Curl.Radius;
            float p = Curl.Progress;
            // dönüş ilerledikçe gölge önce koyulaşır, sonra kâğıt yere yattıkça kaybolur
            float strength = Mathf.Sin(Mathf.Clamp01(p * 1.15f) * Mathf.PI) * 0.9f + 0.1f;
            Vector2 a0, a1;
            float w;
            Color inner;
            if (Under)
            {
                a0 = m + n * (r * 0.95f);
                w = Mathf.Lerp(Curl.W * 0.06f, Curl.W * 0.22f, Mathf.Clamp01(r / (Curl.W * 0.12f)));
                inner = new Color(0.22f, 0.14f, 0.3f, 0.42f * strength);
                a1 = a0 + n * w;
            }
            else
            {
                a0 = m;
                w = Mathf.Max(r * 1.2f, Curl.W * 0.03f);
                inner = new Color(0.22f, 0.14f, 0.3f, 0.3f * strength);
                a1 = m - n * w;
            }
            var outer = new Color(inner.r, inner.g, inner.b, 0f);
            // gölge yumuşak düşsün diye iki parça: hızlı sönme + uzun kuyruk
            var mid = Vector2.Lerp(a0, a1, 0.35f);
            var midCol = new Color(inner.r, inner.g, inner.b, inner.a * 0.35f);
            Quad(vh, a0, mid, t, L, inner, midCol);
            Quad(vh, mid, a1, t, L, midCol, outer);
        }

        static void Quad(VertexHelper vh, Vector2 from, Vector2 to, Vector2 t, float L, Color c0, Color c1)
        {
            int i = vh.currentVertCount;
            vh.AddVert(from - t * L, c0, Vector2.zero);
            vh.AddVert(from + t * L, c0, Vector2.zero);
            vh.AddVert(to + t * L, c1, Vector2.zero);
            vh.AddVert(to - t * L, c1, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i + 2, i + 3, i);
        }
    }
}
