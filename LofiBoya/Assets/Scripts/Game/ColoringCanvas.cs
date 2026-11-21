using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif

namespace LofiBoya
{
    public enum Tool { Fill, Pencil, Eraser, Stamp }

    /// <summary>
    /// Boyama tuvali.
    /// Kova: dokun = bölgeyi boya. Kalem: sürükle = çizgilerin dışına taşmadan boya. Silgi: kalem izini sil, dokununca bölgeyi boşalt.
    /// Tek parmak / sol tık sürükle = kaydır (kova modunda), iki parmak / tekerlek / sağ tık = yakınlaştır ve kaydır.
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
        public bool[] Brushed { get; private set; }
        public PaintLayer Paint { get; private set; }
        public Color CurrentColor = Color.white;
        public Tool Tool = Tool.Fill;
        public int BrushSize = 1;
        /// <summary>Kova için sihirli boya stili (MagicPaint).</summary>
        public int Magic;
        /// <summary>Damga aracı için şekil (Art.Stamps).</summary>
        public int StampIndex;
        /// <summary>Damga basıldı (ekran noktası, şekil, açı) — üstte "pıt" animasyonu için.</summary>
        public event Action<Vector2, Sprite, float> Stamped;
        bool _stamped;
        public float MaxZoom = 8f;

        /// <summary>Kalem çapı (ekran/kanvas biriminde) — küçük, orta, büyük.</summary>
        public static readonly float[] BrushDiameters = { 18f, 38f, 76f };

        public event Action Changed;
        /// <summary>Kova ile dokunuldu (ekran noktası) — kova animasyonu için.</summary>
        public event Action<Vector2> Tapped;
        /// <summary>Sayılı mod: false dönerse bölge boyanmaz (yanlış renk).</summary>
        public Func<int, Color, bool> CanFill;

        /// <summary>Elde kalem imleci için: parmak/fare nerede, çiziliyor mu, tuval üstünde mi.</summary>
        public Vector2 PointerScreen { get; private set; }
        public bool PointerOver { get; private set; }
        public bool Stroking => _stroking;
        public bool TouchInput { get; private set; }

        class UndoStep
        {
            public int Region = -1;            // bölge dolgusu değiştiyse
            public Color32 Color;
            public bool WasFilled;
            public RenderTexture Paint;        // kalem/silgi çizgisi öncesi kopya
            public int BrushedRegion = -1;
            public bool WasBrushed;
        }

        const int MaxPaintSnapshots = 12;
        readonly List<UndoStep> _undo = new();

        Texture2D _colorTex;
        int _colorRows;
        long _totalArea;
        Coroutine _fillAnim;
        int _animRegion = -1;
        Color32 _animColor;
        float _zoom = 1f;
        Vector2 _lastViewportSize;
        int _hintRegion = -1;
        Coroutine _hintCo;

        // Dokunma durumu
        bool _gesture, _moved, _multi, _panOnly;
        Vector2 _startPos, _lastPos;
        float _lastPinchDist;
        Vector2 _lastPinchMid;

        // Çizgi durumu
        bool _stroking;
        double _lastTouchTime;
        int _strokeRegion;
        Vector2 _lastStampUv;
        float _strokeStartTime, _strokeLength;
        UndoStep _strokeUndo;
        float _scribble;

        static readonly int IdRegionTex = Shader.PropertyToID("_RegionTex"), IdColorTex = Shader.PropertyToID("_ColorTex"),
            IdPaintTex = Shader.PropertyToID("_PaintTex"), IdStrokeTex = Shader.PropertyToID("_StrokeTex"),
            IdStrokeColor = Shader.PropertyToID("_StrokeColor"), IdStrokeMode = Shader.PropertyToID("_StrokeMode"),
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
                for (int i = 1; i <= Level.Count; i++) if (Filled[i] || Brushed[i]) a += Level.Meta.area[i - 1];
                return (float)((double)a / _totalArea);
            }
        }

        /// <summary>Her bölge kova ile boyandı mı (otomatik kutlama için).</summary>
        public bool AllFilled
        {
            get
            {
                if (Level == null) return false;
                for (int i = 1; i <= Level.Count; i++) if (!Filled[i]) return false;
                return true;
            }
        }

        /// <summary>Her bölge kova ya da kalemle boyandı mı.</summary>
        public bool AllDone
        {
            get
            {
                if (Level == null) return false;
                for (int i = 1; i <= Level.Count; i++) if (!Filled[i] && !Brushed[i]) return false;
                return true;
            }
        }

        public bool CanUndo => _undo.Count > 0;
        public float Zoom => _zoom;

        /// <summary>Resmin kenarına değen arka plan bölgesi (kart rengi bununla eşleşir).</summary>
        public int BorderRegion { get; private set; }

        /// <summary>Arka plan bölgesinin şu anki görünen rengi.</summary>
        public Color BorderColor
        {
            get
            {
                if (BorderRegion <= 0 || !Filled[BorderRegion]) return Theme.Paper;
                Color c = RegionColors[BorderRegion];
                c.a = 1;
                return c;
            }
        }

        void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            EnhancedTouchSupport.Enable();
#endif
        }

        public void Init(Level level, LevelSave save, Texture2D savedPaint)
        {
            Level = level;
            int n = level.Count;
            RegionColors = new Color32[n + 1];
            Filled = new bool[n + 1];
            Brushed = new bool[n + 1];
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
                    RegionColors[i] = new Color32((byte)(c >> 16), (byte)(c >> 8), (byte)c, MagicPaint.Encode((c >> 24) & 0xFF));
                    Filled[i] = true;
                    _colorTex.SetPixel(i % 256, i / 256, RegionColors[i]);
                }
            }
            if (save?.brushed != null && save.brushed.Length == n + 1) Array.Copy(save.brushed, Brushed, n + 1);
            _colorTex.Apply(false);

            Paint = new PaintLayer(level);
            if (savedPaint != null) { Paint.Load(savedPaint); _stamped = true; }
            Paint.Dirty = false;

            Material = new Material(Resources.Load<Shader>("Shaders/ColoringFill"));
            Material.SetTexture(IdRegionTex, level.Regions);
            Material.SetTexture(IdColorTex, _colorTex);
            Material.SetTexture(IdPaintTex, Paint.Texture);
            Material.SetFloat(IdRows, _colorRows);
            Material.SetVector(IdTexSize, new Vector4(level.Width, level.Height, 0, 0));
            Material.SetFloat(IdFillRegion, -1);
            Material.SetFloat(IdHint, -1);
            Material.SetColor(IdHintColor, Theme.Accent);
            Image.texture = level.Lines;
            Image.material = Material;
            BorderRegion = level.RegionAt(1, 1);
            Fit();
        }

        public LevelSave ToSave()
        {
            var s = new LevelSave { colors = new int[Level.Count + 1], done = AllDone, progress = Progress, brushed = (bool[])Brushed.Clone() };
            s.colors[0] = -1;
            for (int i = 1; i <= Level.Count; i++)
            {
                var c = RegionColors[i];
                s.colors[i] = Filled[i] ? (c.r << 16) | (c.g << 8) | c.b | (MagicPaint.Decode(c.a) << 24) : -1;
            }
            s.hasPaint = HasPaint;
            return s;
        }

        /// <summary>Boya katmanında bir şey var mı (kalem ya da damga).</summary>
        public bool HasPaint => _stamped || HasAnyBrush;

        public bool HasAnyBrush
        {
            get { foreach (var b in Brushed) if (b) return true; return false; }
        }

        void Fit()
        {
            if (Level == null) return;
            var vs = Viewport.rect.size;
            _lastViewportSize = vs;
            float s = Mathf.Min(vs.x / Level.Width, vs.y / Level.Height) * 0.97f;
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
            // kalem sesi: hıza göre yumuşakça açılır/kapanır
            _scribble = Mathf.MoveTowards(_scribble, 0f, Time.unscaledDeltaTime * 4f);
            Sfx.Scribble(_scribble);
        }

        // ================= Girdi =================

        void HandleInput()
        {
#if ENABLE_INPUT_SYSTEM
            var touches = ETouch.activeTouches;
            if (touches.Count > 0) TouchInput = true;
            PointerOver = false;
            if (touches.Count >= 2)
            {
                if (_stroking)
                {
                    // ikinci parmak hemen geldiyse bu bir yakınlaştırmadır: çizgiyi iptal et
                    if (Time.unscaledTime - _strokeStartTime < 0.3f || _strokeLength < 30f) CancelStroke();
                    else EndStroke();
                }
                var a = touches[0].screenPosition;
                var b = touches[1].screenPosition;
                float dist = Vector2.Distance(a, b);
                Vector2 mid = (a + b) * 0.5f;
                if (!_multi || !_gesture)
                {
                    if (!_gesture && (!InViewport(mid) || OverOtherUI(mid))) return;
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
                PointerScreen = p;
                PointerOver = InViewport(p);
                bool ended = t.phase is UnityEngine.InputSystem.TouchPhase.Ended or UnityEngine.InputSystem.TouchPhase.Canceled;
                if (!_gesture)
                {
                    // önünde pencere/buton/panel varsa (ör. "Temizle" onayı) tuval bu dokunuşu almaz
                    if (t.phase != UnityEngine.InputSystem.TouchPhase.Began || !InViewport(p) || OverOtherUI(p)) return;
                    BeginGesture(p, false);
                }
                else if (_multi)
                {
                    // iki parmaktan teke düştü: sadece kaydır, çizme
                    _lastPos = p;
                    _multi = false;
                    _panOnly = true;
                    _moved = true;
                }
                // bu karede gelen ara dokunuş noktaları (telefonlar 120-240 Hz okur): çizgi parmağı birebir izlesin
                if (_stroking)
                {
                    var hist = t.history;
                    for (int h = hist.Count - 1; h >= 0; h--)
                    {
                        var e = hist[h];
                        if (e.time <= _lastTouchTime) continue;
                        _lastTouchTime = e.time;
                        StrokeTo(e.screenPosition);
                    }
                }
                _lastTouchTime = t.time;
                PointerMove(p);
                if (ended) PointerUp(p);
                return;
            }
            if (_gesture && touches.Count == 0 && (_multi || _panOnly) && Touchscreen.current != null && Mouse.current == null)
            {
                _gesture = false; _multi = false; _panOnly = false;
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null) return;
            var mp = mouse.position.ReadValue();
            if (!TouchInput)
            {
                PointerScreen = mp;
                PointerOver = InViewport(mp);
            }
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f && InViewport(mp) && !OverOtherUI(mp)) ZoomAt(mp, scroll > 0 ? 1.15f : 1f / 1.15f);

            bool panBtnDown = mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame;
            bool panBtnHeld = mouse.rightButton.isPressed || mouse.middleButton.isPressed;
            if (!_gesture)
            {
                if (mouse.leftButton.wasPressedThisFrame && InViewport(mp) && !OverOtherUI(mp)) { BeginGesture(mp, false); PointerMove(mp); }
                else if (panBtnDown && InViewport(mp) && !OverOtherUI(mp)) BeginGesture(mp, true);
                return;
            }
            if (mouse.leftButton.isPressed || panBtnHeld) PointerMove(mp);
            else PointerUp(mp);
#endif
        }

        void BeginGesture(Vector2 p, bool panOnly)
        {
            _gesture = true; _moved = false; _multi = false; _panOnly = panOnly;
            _startPos = _lastPos = p;
            if (!panOnly && (Tool == Tool.Pencil || Tool == Tool.Eraser)) BeginStroke(p);
        }

        void PointerMove(Vector2 p)
        {
            if (_stroking) { StrokeTo(p); _lastPos = p; return; }
            float slop = Screen.dpi > 0 ? Screen.dpi * 0.06f : 14f;
            if (!_moved && Vector2.Distance(p, _startPos) > slop) _moved = true;
            if (_moved && (Tool == Tool.Fill || Tool == Tool.Stamp || _panOnly)) Pan(p - _lastPos);
            _lastPos = p;
        }

        void PointerUp(Vector2 p)
        {
            if (_stroking)
            {
                bool tap = _strokeLength < 6f;
                EndStroke();
                if (tap && Tool == Tool.Eraser) EraseRegionAt(p);
            }
            else if (!_moved && !_multi && !_panOnly && Tool == Tool.Fill) TapAt(p);
            else if (!_moved && !_multi && !_panOnly && Tool == Tool.Stamp) StampAt(p);
            _gesture = false; _moved = false; _multi = false; _panOnly = false;
        }

        static readonly List<RaycastResult> _hits = new();

        /// <summary>Bu noktada tuvalin önünde dokunulabilir bir arayüz öğesi var mı.</summary>
        bool OverOtherUI(Vector2 screen)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var ped = new PointerEventData(es) { position = screen };
            _hits.Clear();
            es.RaycastAll(ped, _hits);
            foreach (var h in _hits)
                if (h.gameObject != null && !h.gameObject.transform.IsChildOf(Viewport)) return true;
            return false;
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

        bool ScreenToUv(Vector2 screen, out Vector2 uv)
        {
            uv = default;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Image.rectTransform, screen, null, out var local)) return false;
            uv = Rect.PointToNormalized(Image.rectTransform.rect, local);
            // PointToNormalized sınırda kırpar; gerçek dışarıyı ayrıca kontrol et
            var r = Image.rectTransform.rect;
            return local.x >= r.xMin && local.x <= r.xMax && local.y >= r.yMin && local.y <= r.yMax;
        }

        int RegionAtUv(Vector2 uv) => Level.RegionAt(Mathf.FloorToInt(uv.x * Level.Width), Mathf.FloorToInt(uv.y * Level.Height));

        // ================= Kova =================

        void TapAt(Vector2 screen)
        {
            if (!ScreenToUv(screen, out var uv)) return;
            int id = RegionAtUv(uv);
            if (id <= 0) return;
            if (CanFill != null && !CanFill(id, CurrentColor)) return;
            Tapped?.Invoke(screen);
            var col = CurrentColor;
            col.a = MagicPaint.Encode(Magic) / 255f;
            Fill(id, uv, col);
            Haptics.Light();
        }

        void Fill(int id, Vector2 uv, Color color)
        {
            Color32 c = color;
            if (Filled[id] && RegionColors[id].Equals(c)) { Sfx.Tap(); return; }
            CommitAnimation();
            PushUndo(new UndoStep { Region = id, Color = RegionColors[id], WasFilled = Filled[id] });

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

        /// <summary>Animasyonsuz, geri alınamaz boyama (sayılı modda minik bölgeler için).</summary>
        public void FillSilently(int id, Color color)
        {
            CommitAnimation();
            SetRegion(id, color, true);
        }

        /// <summary>Bölgenin ortası tuval (Content) yerel koordinatında.</summary>
        public Vector2 RegionLocalCenter(int id)
        {
            var uv = new Vector2((Level.Meta.cx[id - 1] + 0.5f) / Level.Width, (Level.Meta.cy[id - 1] + 0.5f) / Level.Height);
            return (uv - new Vector2(0.5f, 0.5f)) * Content.sizeDelta;
        }

        /// <summary>Bölgenin iç yarıçapı tuval biriminde (yakınlaştırma hariç).</summary>
        public float RegionLocalRadius(int id) => Level.Meta.radius[id - 1] * Content.sizeDelta.x / Level.Width;

        void SetRegion(int id, Color32 color, bool filled)
        {
            RegionColors[id] = color;
            Filled[id] = filled;
            _colorTex.SetPixel(id % 256, id / 256, filled ? color : (Color32)Theme.Paper);
            _colorTex.Apply(false);
        }

        // ================= Kalem / silgi =================

        float BrushRadiusPaintPx()
        {
            float diam = BrushDiameters[Mathf.Clamp(BrushSize, 0, BrushDiameters.Length - 1)] * (Tool == Tool.Eraser ? 1.5f : 1f);
            return diam * 0.5f * Paint.Width / (Content.sizeDelta.x * _zoom);
        }

        void BeginStroke(Vector2 screen)
        {
            if (!ScreenToUv(screen, out var uv)) return;
            _strokeRegion = Tool == Tool.Eraser ? -1 : RegionAtUv(uv);
            if (Tool == Tool.Pencil && _strokeRegion <= 0) return;
            CommitAnimation();
            _stroking = true;
            _strokeStartTime = Time.unscaledTime;
            _strokeLength = 0;
            _strokeUndo = new UndoStep();
            Material.SetTexture(IdStrokeTex, Paint.Stroke);
            Material.SetColor(IdStrokeColor, CurrentColor);
            Material.SetFloat(IdStrokeMode, Tool == Tool.Eraser ? 2 : 1);
            if (Tool == Tool.Pencil)
            {
                _strokeUndo.BrushedRegion = _strokeRegion;
                _strokeUndo.WasBrushed = Brushed[_strokeRegion];
                if (_strokeRegion == _hintRegion) StopHint();
            }
            _lastStampUv = uv;
            Paint.AddStamp(uv, BrushRadiusPaintPx());
            FlushStroke();
        }

        void StrokeTo(Vector2 screen)
        {
            if (!_stroking) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Image.rectTransform, screen, null, out var local);
            var r = Image.rectTransform.rect;
            var uv = new Vector2((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height);
            float radius = BrushRadiusPaintPx();
            var a = new Vector2(_lastStampUv.x * Paint.Width, _lastStampUv.y * Paint.Height);
            var b = new Vector2(uv.x * Paint.Width, uv.y * Paint.Height);
            float dist = Vector2.Distance(a, b);
            float spacing = Mathf.Max(0.75f, radius * 0.22f);
            if (dist < spacing) return;
            int steps = Mathf.Min(400, Mathf.FloorToInt(dist / spacing));
            for (int i = 1; i <= steps; i++)
            {
                var p = Vector2.Lerp(a, b, i * spacing / dist);
                Paint.AddStamp(new Vector2(p.x / Paint.Width, p.y / Paint.Height), radius * UnityEngine.Random.Range(0.92f, 1.04f));
            }
            // son damgayı tam parmağın altına koy: boya kalemin ucundan geri kalmasın
            if (steps * spacing < dist - 0.01f) Paint.AddStamp(uv, radius);
            _lastStampUv = uv;
            float screenDist = dist / Paint.Width * Content.sizeDelta.x * _zoom;
            _strokeLength += screenDist;
            _scribble = Mathf.Clamp01(_scribble + screenDist / 120f);
            FlushStroke();
        }

        void FlushStroke()
        {
            if (Tool == Tool.Eraser) Paint.Flush(-1, 1f, 0.5f, 0.25f);
            else Paint.Flush(_strokeRegion, 1f, 0.5f, 0.85f);
        }

        void EndStroke()
        {
            if (!_stroking) return;
            _stroking = false;
            _strokeUndo.Paint = Paint.Snapshot(); // işlenmeden önceki hâl
            Paint.Commit(CurrentColor, Tool == Tool.Eraser);
            Material.SetFloat(IdStrokeMode, 0);
            if (Tool == Tool.Pencil && _strokeRegion > 0) Brushed[_strokeRegion] = true;
            PushUndo(_strokeUndo);
            _strokeUndo = null;
            Changed?.Invoke();
        }

        void CancelStroke()
        {
            if (!_stroking) return;
            _stroking = false;
            Paint.CancelStroke();
            Material.SetFloat(IdStrokeMode, 0);
            _strokeUndo = null;
        }

        void EraseRegionAt(Vector2 screen)
        {
            if (!ScreenToUv(screen, out var uv)) return;
            int id = RegionAtUv(uv);
            if (id <= 0) return;
            // silgiyle dokunma: önce kalem izini sildi (aynı adım); bölgede dolgu varsa onu da kaldır
            var last = _undo.Count > 0 ? _undo[^1] : null;
            if (Filled[id] && last != null)
            {
                last.Region = id;
                last.Color = RegionColors[id];
                last.WasFilled = true;
                SetRegion(id, Theme.Paper, false);
                Sfx.Undo();
                Changed?.Invoke();
            }
        }

        // ================= Damga =================

        float StampRadiusPx()
        {
            float diam = new[] { 110f, 170f, 250f }[Mathf.Clamp(BrushSize, 0, 2)];
            return diam * 0.5f * Paint.Width / (Content.sizeDelta.x * _zoom);
        }

        void StampAt(Vector2 screen)
        {
            if (!ScreenToUv(screen, out var uv)) return;
            CommitAnimation();
            var stamps = Art.Stamps;
            var sprite = stamps[Mathf.Clamp(StampIndex, 0, stamps.Length - 1)];
            float rot = UnityEngine.Random.Range(-15f, 15f);
            var step = new UndoStep { Paint = Paint.Snapshot() };
            Paint.StampShape(uv, StampRadiusPx(), sprite.texture, rot);
            Paint.Commit(CurrentColor, false);
            _stamped = true;
            PushUndo(step);
            Sfx.Pop();
            Haptics.Light();
            Stamped?.Invoke(screen, sprite, rot);
            Changed?.Invoke();
        }

        // ================= Geri alma =================

        void PushUndo(UndoStep step)
        {
            _undo.Add(step);
            // eski kalem kopyalarını bırak (bellek)
            int snaps = 0;
            for (int i = _undo.Count - 1; i >= 0; i--)
            {
                if (_undo[i].Paint == null) continue;
                if (++snaps > MaxPaintSnapshots)
                {
                    // bu adımdan öncesine geri dönülemez: hepsini at
                    for (int k = 0; k <= i; k++) Release(_undo[k]);
                    _undo.RemoveRange(0, i + 1);
                    break;
                }
            }
            if (_undo.Count > 300) { Release(_undo[0]); _undo.RemoveAt(0); }
        }

        static void Release(UndoStep s)
        {
            if (s?.Paint != null) { s.Paint.Release(); Destroy(s.Paint); s.Paint = null; }
        }

        public void Undo()
        {
            if (_undo.Count == 0 || _stroking) return;
            CommitAnimation();
            var s = _undo[^1];
            _undo.RemoveAt(_undo.Count - 1);
            if (s.Paint != null) Paint.Restore(s.Paint);
            if (s.BrushedRegion > 0) Brushed[s.BrushedRegion] = s.WasBrushed;
            if (s.Region > 0) SetRegion(s.Region, s.Color, s.WasFilled);
            Release(s);
            Sfx.Undo();
            Changed?.Invoke();
        }

        // ================= İpucu =================

        /// <summary>Boyanmamış en büyük bölgeyi parlatır ve gerekirse oraya yakınlaşır.</summary>
        public bool ShowHint()
        {
            int best = -1;
            int bestArea = -1;
            for (int i = 1; i <= Level.Count; i++)
                if (!Filled[i] && !Brushed[i] && Level.Meta.area[i - 1] > bestArea) { bestArea = Level.Meta.area[i - 1]; best = i; }
            if (best < 0) return false;
            StopHint();
            _hintRegion = best;
            Material.SetFloat(IdHint, best);

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

        // ================= Kayıt yardımcıları =================

        /// <summary>Galeri küçük resmi; boya katmanı varsa onu da içerir.</summary>
        public Texture2D MakeThumbnail(Texture2D paintReadback)
        {
            CommitAnimation();
            var cols = new Color32[RegionColors.Length];
            for (int i = 0; i < cols.Length; i++) cols[i] = Filled[i] ? RegionColors[i] : (Color32)Theme.Paper;
            return Level.RenderThumbnail(cols, paintReadback);
        }

        /// <summary>Tüm boyamayı temizle (geri alınamaz).</summary>
        public void ClearAll()
        {
            CommitAnimation();
            foreach (var s in _undo) Release(s);
            _undo.Clear();
            Color32 paper = Theme.Paper;
            for (int i = 1; i <= Level.Count; i++)
            {
                Filled[i] = false;
                Brushed[i] = false;
                RegionColors[i] = paper;
                _colorTex.SetPixel(i % 256, i / 256, paper);
            }
            _colorTex.Apply(false);
            Paint.Clear();
            Paint.Dirty = true;
            _stamped = false;
            Changed?.Invoke();
        }

        void OnDestroy()
        {
            Sfx.Scribble(0);
            foreach (var s in _undo) Release(s);
            Release(_strokeUndo);
            Paint?.Release();
            if (Material) Destroy(Material);
            if (_colorTex) Destroy(_colorTex);
        }
    }
}
