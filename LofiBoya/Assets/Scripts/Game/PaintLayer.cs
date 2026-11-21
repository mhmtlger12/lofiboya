using System.Collections.Generic;
using UnityEngine;

namespace LofiBoya
{
    /// <summary>
    /// Boya kalemi katmanı: resmin üstünde ayrı bir RenderTexture.
    /// Damga (stamp) çizer, geri alma için anlık kopya tutar, kaydedip yükler.
    /// </summary>
    public class PaintLayer
    {
        public RenderTexture Texture { get; private set; }
        /// <summary>Şu an çizilen çizginin kapsama maskesi (r kanalı). Parmak kalkınca Texture'a işlenir.</summary>
        public RenderTexture Stroke { get; private set; }
        public int Width { get; }
        public int Height { get; }
        /// <summary>Resim pikselinden boya katmanı pikseline oran.</summary>
        public float Scale { get; }
        public bool Dirty { get; set; }

        readonly Material _brush;
        readonly List<Vector4> _stamps = new(); // x, y (uv), radius uv x, radius uv y
        static readonly int IdColor = Shader.PropertyToID("_Color"), IdRegion = Shader.PropertyToID("_Region"),
            IdOpacity = Shader.PropertyToID("_Opacity"), IdHardness = Shader.PropertyToID("_Hardness"),
            IdGrainScale = Shader.PropertyToID("_GrainScale"), IdGrainAmount = Shader.PropertyToID("_GrainAmount");

        public PaintLayer(Level level, int maxSide = 1024)
        {
            Scale = Mathf.Min(1f, (float)maxSide / Mathf.Max(level.Width, level.Height));
            Width = Mathf.Max(1, Mathf.RoundToInt(level.Width * Scale));
            Height = Mathf.Max(1, Mathf.RoundToInt(level.Height * Scale));
            Texture = NewRT();
            Clear();
            Stroke = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, useMipMap = false, name = "StrokeLayer"
            };
            Stroke.Create();
            ClearTo(Stroke);

            _brush = new Material(Resources.Load<Shader>("Shaders/BrushStamp"));
            _brush.SetTexture("_RegionTex", level.Regions);
            _brush.SetTexture("_Grain", Art.Grain);
            // doku kâğıt piksel ölçeğinde kalsın
            _brush.SetVector(IdGrainScale, new Vector4(Width / 96f, Height / 96f, 0, 0));
        }

        RenderTexture NewRT()
        {
            var rt = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, useMipMap = false, name = "PaintLayer"
            };
            rt.Create();
            return rt;
        }

        public void Clear()
        {
            var prev = RenderTexture.active;
            RenderTexture.active = Texture;
            GL.Clear(false, true, new Color(0, 0, 0, 0));
            RenderTexture.active = prev;
        }

        public void ClearTo(RenderTexture rt)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(false, true, new Color(0, 0, 0, 0));
            RenderTexture.active = prev;
        }

        // ---------------- Damgalar ----------------

        public void AddStamp(Vector2 uv, float radiusPx)
        {
            _stamps.Add(new Vector4(uv.x, uv.y, radiusPx / Width, radiusPx / Height));
        }

        /// <summary>Biriken damgaları çizgi katmanına tek seferde basar.</summary>
        public void Flush(int region, float opacity, float hardness, float grain)
        {
            if (_stamps.Count == 0) return;
            _brush.SetFloat(IdRegion, region);
            _brush.SetFloat(IdOpacity, opacity);
            _brush.SetFloat(IdHardness, hardness);
            _brush.SetFloat(IdGrainAmount, grain);

            var prev = RenderTexture.active;
            RenderTexture.active = Stroke;
            GL.PushMatrix();
            GL.LoadOrtho();
            _brush.SetPass(0);
            GL.Begin(GL.QUADS);
            foreach (var s in _stamps)
            {
                Quad(s.x, s.y, s.z, s.w);
            }
            GL.End();
            GL.PopMatrix();
            RenderTexture.active = prev;
            _stamps.Clear();
        }

        /// <summary>Çizgiyi boya katmanına işler (boya ya da silgi) ve çizgi katmanını temizler.</summary>
        public void Commit(Color color, bool erase)
        {
            _brush.SetColor(IdColor, color);
            Graphics.Blit(Stroke, Texture, _brush, erase ? 2 : 1);
            ClearTo(Stroke);
            Dirty = true;
        }

        public void CancelStroke()
        {
            _stamps.Clear();
            ClearTo(Stroke);
        }

        static void Quad(float x, float y, float rx, float ry)
        {
            Corner(-1, -1, x, y, rx, ry);
            Corner(-1, 1, x, y, rx, ry);
            Corner(1, 1, x, y, rx, ry);
            Corner(1, -1, x, y, rx, ry);
        }

        static void Corner(float lx, float ly, float x, float y, float rx, float ry)
        {
            float px = x + lx * rx, py = y + ly * ry;
            GL.MultiTexCoord2(0, lx, ly);
            GL.MultiTexCoord2(1, px, py);
            GL.Vertex3(px, py, 0);
        }

        // ---------------- Geri alma ----------------

        public RenderTexture Snapshot()
        {
            var rt = NewRT();
            Graphics.CopyTexture(Texture, rt);
            return rt;
        }

        public void Restore(RenderTexture snap)
        {
            if (snap == null) return;
            Graphics.CopyTexture(snap, Texture);
            Dirty = true;
        }

        // ---------------- Kaydet / yükle ----------------

        public Texture2D ReadBack()
        {
            var prev = RenderTexture.active;
            RenderTexture.active = Texture;
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
            tex.Apply(false);
            RenderTexture.active = prev;
            return tex;
        }

        public void Load(Texture2D src)
        {
            if (src == null) return;
            // sRGB -> sRGB: okurken doğrusala, yazarken geri çevrilir; değerler korunur
            Graphics.Blit(src, Texture);
        }

        public void Release()
        {
            if (Texture != null) { Texture.Release(); Object.Destroy(Texture); }
            if (Stroke != null) { Stroke.Release(); Object.Destroy(Stroke); }
            if (_brush != null) Object.Destroy(_brush);
        }
    }
}
