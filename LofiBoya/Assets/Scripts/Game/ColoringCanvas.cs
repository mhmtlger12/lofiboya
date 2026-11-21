using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif

namespace LofiBoya
{
    /// <summary>
    /// Boyama tuvali: dokun = bölgeyi boya, sürükle = kaydır, iki parmak / tekerlek = yakınlaştır.
    /// Bölge renkleri küçük bir dokuda tutulur, shader çizgilerle çarpıp çizer.
    /// </summary>
    public class ColoringCanvas : MonoBehaviour
    {
        public RectTransform Viewport;
        public RectTransform Content;
        public RawImage Image;
        public Material Material { get; private set; }

        public Level Level { get; private set; }
        public Color32[] RegionColors { get; private set; }
        public bool[] Filled { get; private set; }
        public Color CurrentColor = Color.white;
        public float MaxZoom = 8f;

        public event Action Changed;

        Texture2D _colorTex;
        int _colorRows;
        long _totalArea;
        readonly Stack<(int id, Color32 color, bool filled)> _undo = new();
        Coroutine _fillAnim;
        int _animRegion = -1;
        Color32 _animColor;
        float _zoom = 1f;
        Vector2 _lastViewportSize;
        int _hintRegion = -1;
        Coroutine _hintCo;

        // Dokunma durumu
        bool _gesture, _moved, _multi;
        Vector2 _startPos, _lastPos;
        float _lastPinchDist;
        Vector2 _lastPinchMid;

        static readonly int IdRegionTex = Shader.PropertyToID("_RegionTex"), IdColorTex = Shader.PropertyToID("_ColorTex"),
            IdRows = Shader.PropertyToID("_ColorRows"), IdTexSize = Shader.PropertyToID("_TexSize"),
            IdFillRegion = Shader.PropertyToID("_FillRegion"), IdFillCenter = Shader.PropertyToID("_FillCenter"),
            IdFillRadius = Shader.PropertyToID("_FillRadius"), IdFillColor = Shader.PropertyToID("_FillColor"),
            IdHint = Shader.PropertyToID("_HintRegion"), IdHintColor = Shader.PropertyToID("_HintColor");

        public float Progress
        {
            get
            {
                if (Level == null || _totalArea == 0) return 0;
                long a = 0;
                for (int i = 1; i <= Level.Count; i++) if (Filled[i]) a += Level.Meta.area[i - 1];
                return (float)((double)a / _totalArea);
            }
        }

        public bool AllFilled
        {
            get
            {
                if (Level == null) return false;
                for (int i = 1; i <= Level.Count; i++) if (!Filled[i]) return false;
                return true;
            }
        }

        public bool CanUndo => _undo.Count > 0;

        void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            EnhancedTouchSupport.Enable();
#endif
        }

        public void Init(Level level, LevelSave save)
        {
            Level = level;
            int n = level.Count;
            RegionColors = new Color32[n + 1];
            Filled = new bool[n + 1];
            _totalArea = 0;
            foreach (var a in level.Meta.area) _totalArea += a;

            _colorRows = Mathf.Max(1, Mathf.CeilToInt((n + 1) / 256f));
            _colorTex = new Texture2D(256, _colorRows, TextureFormat.RGBA32, false, false)
            {
                filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "RegionColors"
            };
            var fill = new Color32[256 * _colorRows];
            Color32 paper = Theme.Paper;
            for (int i = 0; i < fill.Length; i++) fill[i] = paper;
            _colorTex.SetPixels32(fill);

            for (int i = 0; i <= n; i++) RegionColors[i] = paper;
            if (save?.colors != null && save.colors.Length == n + 1)
            {
                for (int i = 1; i <= n; i++)
                {
                    if (save.colors[i] < 0) continue;
                    int c = save.colors[i];
                    RegionColors[i] = new Color32((byte)(c >> 16), (byte)(c >> 8), (byte)c, 255);
                    Filled[i] = true;
                    _colorTex.SetPixel(i % 256, i / 256, RegionColors[i]);
                }
            }
            _colorTex.Apply(false);

            Material = new Material(Resources.Load<Shader>("Shaders/ColoringFill"));
            Material.SetTexture(IdRegionTex, level.Regions);
            Material.SetTexture(IdColorTex, _colorTex);
            Material.SetFloat(IdRows, _colorRows);
            Material.SetVector(IdTexSize, new Vector4(level.Width, level.Height, 0, 0));
            Material.SetFloat(IdFillRegion, -1);
            Material.SetFloat(IdHint, -1);
            Material.SetColor(IdHintColor, Theme.Accent);
            Image.texture = level.Lines;
            Image.material = Material;
            Fit();
        }

        public LevelSave ToSave()
        {
            var s = new LevelSave { colors = new int[Level.Count + 1], done = AllFilled, progress = Progress };
            s.colors[0] = -1;
            for (int i = 1; i <= Level.Count; i++)
            {
                var c = RegionColors[i];
                s.colors[i] = Filled[i] ? (c.r << 16) | (c.g << 8) | c.b : -1;
            }
            return s;
        }

        void Fit()
        {
            if (Level == null) return;
            var vs = Viewport.rect.size;
            _lastViewportSize = vs;
            float s = Mathf.Min(vs.x / Level.Width, vs.y / Level.Height) * 0.94f;
            Content.sizeDelta = new Vector2(Level.Width * s, Level.Height * s);
            _zoom = 1f;
            Content.localScale = Vector3.one;
            Content.anchoredPosition = Vector2.zero;
        }

        public void ResetView()
        {
            var from = Content.anchoredPosition;
            float z0 = _zoom;
            Tween.To(this, 0.35f, k =>
            {
                _zoom = Mathf.Lerp(z0, 1f, k);
                Content.localScale = new Vector3(_zoom, _zoom, 1);
                Content.anchoredPosition = Vector2.Lerp(from, Vector2.zero, k);
            });
        }

        void Update()
        {
            if (Level == null) return;
            if (Viewport.rect.size != _lastViewportSize) Fit();
            HandleInput();
        }

        // ---------------- Girdi ----------------

        void HandleInput()
        {
#if ENABLE_INPUT_SYSTEM
            var touches = ETouch.activeTouches;
            if (touches.Count >= 2)
            {
                var a = touches[0].screenPosition;
                var b = touches[1].screenPosition;
                float dist = Vector2.Distance(a, b);
                Vector2 mid = (a + b) * 0.5f;
                if (!_multi || !_gesture)
                {
                    if (!_gesture && !InViewport(mid)) return;
                    _gesture = true; _multi = true; _moved = true;
                    _lastPinchDist = dist; _lastPinchMid = mid;
                    return;
                }
                ZoomAt(mid, dist / Mathf.Max(1f, _lastPinchDist));
                Pan(mid - _lastPinchMid);
                _lastPinchDist = dist; _lastPinchMid = mid;
                return;
            }
            if (touches.Count == 1)
            {
                var t = touches[0];
                var p = t.screenPosition;
                if (!_gesture)
                {
                    if (t.phase != UnityEngine.InputSystem.TouchPhase.Began || !InViewport(p)) return;
                    BeginGesture(p);
                }
                else if (_multi)
                {
                    // iki parmaktan teke düşünce yeniden başla, ama dokunma sayma
                    _lastPos = p;
                    _multi = false;
                    return;
                }
                DragTo(p);
                if (t.phase is UnityEngine.InputSystem.TouchPhase.Ended or UnityEngine.InputSystem.TouchPhase.Canceled) EndGesture(p);
                return;
            }
            if (_gesture && Touchscreen.current != null && touches.Count == 0 && _multi)
            {
                _gesture = false; _multi = false;
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null) return;
            var mp = mouse.position.ReadValue();
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f && InViewport(mp)) ZoomAt(mp, scroll > 0 ? 1.15f : 1f / 1.15f);
            if (mouse.leftButton.wasPressedThisFrame && InViewport(mp)) BeginGesture(mp);
            else if (_gesture && mouse.leftButton.isPressed) DragTo(mp);
            else if (_gesture && mouse.leftButton.wasReleasedThisFrame) EndGesture(mp);
#endif
        }

        void BeginGesture(Vector2 p)
        {
            _gesture = true; _moved = false; _multi = false;
            _startPos = _lastPos = p;
        }

        void DragTo(Vector2 p)
        {
            float slop = Screen.dpi > 0 ? Screen.dpi * 0.06f : 14f;
            if (!_moved && Vector2.Distance(p, _startPos) > slop) _moved = true;
            if (_moved) Pan(p - _lastPos);
            _lastPos = p;
        }

        void EndGesture(Vector2 p)
        {
            if (!_moved && !_multi) TapAt(p);
            _gesture = false; _moved = false; _multi = false;
        }

        bool InViewport(Vector2 screen) => RectTransformUtility.RectangleContainsScreenPoint(Viewport, screen, null);

        float CanvasScale => Viewport.lossyScale.x <= 0 ? 1f : Viewport.lossyScale.x;

        void Pan(Vector2 screenDelta)
        {
            Content.anchoredPosition += screenDelta / CanvasScale;
            Clamp();
        }

        void ZoomAt(Vector2 screen, float factor)
        {
            float nz = Mathf.Clamp(_zoom * factor, 1f, MaxZoom);
            if (Mathf.Approximately(nz, _zoom)) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Viewport, screen, null, out var local);
            local -= Viewport.rect.center;
            var pos = Content.anchoredPosition;
            Content.anchoredPosition = local - (local - pos) * (nz / _zoom);
            _zoom = nz;
            Content.localScale = new Vector3(_zoom, _zoom, 1);
            Clamp();
        }

        void Clamp()
        {
            var vs = Viewport.rect.size;
            var cs = Content.sizeDelta * _zoom;
            float mx = Mathf.Max(0, (cs.x - vs.x) * 0.5f + vs.x * 0.25f);
            float my = Mathf.Max(0, (cs.y - vs.y) * 0.5f + vs.y * 0.25f);
            if (_zoom <= 1.001f) { mx = my = 0; }
            var p = Content.anchoredPosition;
            Content.anchoredPosition = new Vector2(Mathf.Clamp(p.x, -mx, mx), Mathf.Clamp(p.y, -my, my));
        }

        // ---------------- Boyama ----------------

        void TapAt(Vector2 screen)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Image.rectTransform, screen, null, out var local)) return;
            var uv = Rect.PointToNormalized(Image.rectTransform.rect, local);
            if (uv.x <= 0 || uv.x >= 1 || uv.y <= 0 || uv.y >= 1) return;
            int x = Mathf.FloorToInt(uv.x * Level.Width), y = Mathf.FloorToInt(uv.y * Level.Height);
            int id = Level.RegionAt(x, y);
            if (id <= 0) return;
            Fill(id, uv, CurrentColor);
        }

        void Fill(int id, Vector2 uv, Color color)
        {
            Color32 c = color;
            if (Filled[id] && RegionColors[id].Equals(c)) { Sfx.Tap(); return; }
            CommitAnimation();
            _undo.Push((id, RegionColors[id], Filled[id]));
            if (_undo.Count > 300) TrimUndo();

            RegionColors[id] = c;
            Filled[id] = true;
            if (id == _hintRegion) StopHint();
            Sfx.Pop();

            // Dalga animasyonu: dokunulan noktadan bölgenin en uzak köşesine kadar
            _animRegion = id;
            _animColor = c;
            float extent = Mathf.Sqrt(Level.Meta.area[id - 1]) * 1.6f + Level.Meta.radius[id - 1] * 2f + 30f;
            Material.SetFloat(IdFillRegion, id);
            Material.SetVector(IdFillCenter, new Vector4(uv.x, uv.y, 0, 0));
            Material.SetColor(IdFillColor, color);
            Material.SetFloat(IdFillRadius, 0);
            float duration = Mathf.Clamp(0.25f + extent / 2600f, 0.25f, 0.6f);
            _fillAnim = Tween.To(this, duration, k => Material.SetFloat(IdFillRadius, extent * k), Tween.OutCubic, 0f, CommitAnimation);
            Changed?.Invoke();
        }

        void CommitAnimation()
        {
            if (_animRegion < 0) return;
            Tween.Stop(_fillAnim);
            _fillAnim = null;
            _colorTex.SetPixel(_animRegion % 256, _animRegion / 256, _animColor);
            _colorTex.Apply(false);
            Material.SetFloat(IdFillRegion, -1);
            _animRegion = -1;
        }

        void TrimUndo()
        {
            var arr = _undo.ToArray();
            _undo.Clear();
            for (int i = 199; i >= 0; i--) _undo.Push(arr[i]);
        }

        public void Undo()
        {
            if (_undo.Count == 0) return;
            CommitAnimation();
            var (id, color, filled) = _undo.Pop();
            RegionColors[id] = color;
            Filled[id] = filled;
            _colorTex.SetPixel(id % 256, id / 256, filled ? color : (Color32)Theme.Paper);
            _colorTex.Apply(false);
            Sfx.Undo();
            Changed?.Invoke();
        }

        /// <summary>Boyanmamış en büyük bölgeyi parlatır ve gerekirse oraya yakınlaşır.</summary>
        public bool ShowHint()
        {
            int best = -1;
            int bestArea = -1;
            for (int i = 1; i <= Level.Count; i++)
                if (!Filled[i] && Level.Meta.area[i - 1] > bestArea) { bestArea = Level.Meta.area[i - 1]; best = i; }
            if (best < 0) return false;
            StopHint();
            _hintRegion = best;
            Material.SetFloat(IdHint, best);

            // Küçük bölgeye yakınlaş
            float r = Level.Meta.radius[best - 1];
            float targetZoom = Mathf.Clamp(Level.Width * 0.06f / Mathf.Max(4f, r), 1f, MaxZoom * 0.75f);
            var uv = new Vector2((Level.Meta.cx[best - 1] + 0.5f) / Level.Width, (Level.Meta.cy[best - 1] + 0.5f) / Level.Height);
            var local = (uv - new Vector2(0.5f, 0.5f)) * Content.sizeDelta;
            var fromPos = Content.anchoredPosition;
            float fromZoom = _zoom;
            Tween.To(this, 0.6f, k =>
            {
                _zoom = Mathf.Lerp(fromZoom, Mathf.Max(fromZoom, targetZoom), k);
                Content.localScale = new Vector3(_zoom, _zoom, 1);
                Content.anchoredPosition = Vector2.Lerp(fromPos, -local * _zoom, k);
                Clamp();
            }, Tween.InOutSine);
            _hintCo = Tween.Delay(4f, StopHint);
            return true;
        }

        void StopHint()
        {
            Tween.Stop(_hintCo);
            _hintRegion = -1;
            if (Material != null) Material.SetFloat(IdHint, -1);
        }

        public Texture2D MakeThumbnail()
        {
            CommitAnimation();
            var cols = new Color32[RegionColors.Length];
            for (int i = 0; i < cols.Length; i++) cols[i] = Filled[i] ? RegionColors[i] : (Color32)Theme.Paper;
            return Level.RenderThumbnail(cols);
        }

        /// <summary>Tüm boyamayı temizle.</summary>
        public void ClearAll()
        {
            CommitAnimation();
            _undo.Clear();
            Color32 paper = Theme.Paper;
            for (int i = 1; i <= Level.Count; i++)
            {
                Filled[i] = false;
                RegionColors[i] = paper;
                _colorTex.SetPixel(i % 256, i / 256, paper);
            }
            _colorTex.Apply(false);
            Changed?.Invoke();
        }

        void OnDestroy()
        {
            if (Material) Destroy(Material);
            if (_colorTex) Destroy(_colorTex);
        }
    }
}
