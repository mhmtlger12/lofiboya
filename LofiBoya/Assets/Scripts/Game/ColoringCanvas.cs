using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.LowLevel;
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
            public bool WasStamped;            // Paint ile birlikte: geri alınınca boş resim "boyalı" sayılmasın
            public int BrushedRegion = -1;
            public bool WasBrushed;
            public List<int> Cells;                        // bu çizginin yeni kapladığı ızgara kareleri
            public List<int> Uncovered;                    // silginin açtığı (kaplanmamış saydığı) kareler
            public List<(int region, bool was)> BrushedBefore;
            public ClearState Cleared;                     // "Temizle" öncesi resmin tamamı (geri alınabilsin)
        }

        class ClearState
        {
            public Color32[] Colors;
            public bool[] Filled, Brushed, Covered;
            public bool Stamped, MarkedDone;
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
        /// <summary>Hareket parmakla mı fareyle mi başladı: o kaynak bitince hareket her zaman biter.</summary>
        bool _touchGesture;
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
                double part = 0;
                for (int i = 1; i <= Level.Count; i++)
                {
                    if (Filled[i] || Brushed[i] || IsSkipped(i)) a += Level.Meta.area[i - 1];
                    else if (_regionCells != null && _regionCells[i] > 0)
                        part += Level.Meta.area[i - 1] * Mathf.Clamp01(_coveredCells[i] / (_regionCells[i] * CoverNeeded)) * 0.95;
                }
                return (float)((a + part) / _totalArea);
            }
        }

        /// <summary>Her bölge kova ile boyandı mı (otomatik kutlama için).</summary>
        public bool AllFilled
        {
            get
            {
                if (Level == null) return false;
                for (int i = 1; i <= Level.Count; i++) if (!Filled[i] && !IsSkipped(i)) return false;
                return true;
            }
        }

        /// <summary>Her bölge kova ya da kalemle boyandı mı.</summary>
        public bool AllDone
        {
            get
            {
                if (Level == null) return false;
                for (int i = 1; i <= Level.Count; i++) if (!Filled[i] && !Brushed[i] && !IsSkipped(i)) return false;
                return true;
            }
        }

        /// <summary>
        /// Sayılı modda numara almayan minik bölgeler: bitmiş sayılır ama boyanmaz ve kaydedilmez.
        /// (Eskiden beyaza boyanıp kalıcı kaydediliyordu; 123'e yanlışlıkla basmak resmin yarısını "boyanmış" yapıyordu.)
        /// </summary>
        bool[] _skipped;
        public bool IsSkipped(int id) => _skipped != null && id < _skipped.Length && _skipped[id];
        public void SetSkipped(int id, bool on)
        {
            if (Level == null) return;
            if (_skipped == null || _skipped.Length != Level.Count + 1) _skipped = new bool[Level.Count + 1];
            _skipped[id] = on;
        }
        public void ClearSkipped() => _skipped = null;

        /// <summary>Çocuk "Bitir"e bastı: minik bölgeler kalsa da resim bitmiş sayılır (galeri rozeti, çıkartma, şapka).</summary>
        public bool MarkedDone { get; set; }

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
            InputSystem.onEvent += OnInputEvent;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        // Enable sayaçlıdır: her Enable bir Disable ile eşleşmeli. Yoksa destek açık kalır ve (özellikle
        // domain reload kapalı hızlı Play'de) Play'den çıkarken Input System'in dokunmatik listesi bozulur.
        void OnDisable()
        {
            InputSystem.onEvent -= OnInputEvent;
            EnhancedTouchSupport.Disable();
        }

        // Fare kare başına tek konum verir; ama işletim sistemi kareler arasında çok daha sık (125–1000 Hz) bildirir.
        // Çizerken o ara konumlar toplanır: hızlı daireler köşeli/kesik değil, parmaktaki gibi düzgün çıkar.
        readonly List<Vector2> _mouseTrail = new();

        void OnInputEvent(InputEventPtr ev, InputDevice device)
        {
            if (!_stroking || TouchInput || device is not Mouse mouse) return;
            if (!ev.IsA<StateEvent>() && !ev.IsA<DeltaStateEvent>()) return;
            if (mouse.leftButton.ReadValueFromEvent(ev, out float btn) && btn < 0.5f) return;
            if (_mouseTrail.Count < 512 && mouse.position.ReadValueFromEvent(ev, out var pos)) _mouseTrail.Add(pos);
        }
#endif

        public void Init(Level level, LevelSave save, Texture2D savedPaint)
        {
            Level = level;
            int n = level.Count;
            RegionColors = new Color32[n + 1];
            Filled = new bool[n + 1];
            Brushed = new bool[n + 1];
            BuildCoverGrid();
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
            RestoreCover(save?.cover);
            MarkedDone = save != null && save.done;
            _colorTex.Apply(false);

            Paint = new PaintLayer(level);
            if (savedPaint != null)
            {
                // eski kayıt (resmin kendi oranında) genişletilmiş kartın ortasına yerleşir
                float pa = (float)savedPaint.width / savedPaint.height, la = (float)level.Width / level.Height;
                if (Mathf.Abs(pa - la) > 0.01f * la) Paint.Load(savedPaint, level.OriginalRect);
                else Paint.Load(savedPaint);
                _stamped = true;
            }
            Paint.Dirty = false;

            Material = new Material(Resources.Load<Shader>("Shaders/ColoringFill"));
            MatT(IdRegionTex, level.Regions);
            MatT(IdColorTex, _colorTex);
            MatT(IdPaintTex, Paint.Texture);
            MatF(IdRows, _colorRows);
            MatV(IdTexSize, new Vector4(level.Width, level.Height, 0, 0));
            MatF(IdFillRegion, -1);
            MatF(IdHint, -1);
            MatC(IdHintColor, Theme.Accent);
            Image.texture = level.Lines;
            Image.material = Material;
            BorderRegion = level.RegionAt(1, 1);
            Fit();
        }

        public LevelSave ToSave()
        {
            var s = new LevelSave { colors = new int[Level.Count + 1], done = AllDone || MarkedDone, progress = Progress, brushed = (bool[])Brushed.Clone(), cover = EncodeCover() };
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
        /// <summary>Boya katmanında bir şey var mı (kalem çizgisi, damga ya da eski kayıt).</summary>
        public bool HasPaint => _stamped || HasAnyBrush;

        public bool HasAnyBrush
        {
            get { foreach (var b in Brushed) if (b) return true; return false; }
        }

        void Fit()
        {
            if (Level == null) return;
            var vs = Viewport.rect.size;
            if (vs.x <= 0f || vs.y <= 0f || Level.Width <= 0 || Level.Height <= 0) return;   // düzen henüz hazır değil: boyut gelince yeniden denenir
            _lastViewportSize = vs;
            float s = Mathf.Min(vs.x / Level.Width, vs.y / Level.Height);
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
            // geri alma dokuları çizilmezken ayrılır (çizim sırasında sürücü takılması olmasın)
            if (!_stroking && Paint != null) Paint.Prewarm(MaxPaintSnapshots + 1);
            if (Level == null) return;
            if (Viewport.rect.size != _lastViewportSize) Fit();
            HandleInput();
            // kalem sesi: hıza göre yumuşakça açılır/kapanır
            _scribble = Mathf.MoveTowards(_scribble, 0f, Time.unscaledDeltaTime * 4f);
            Sfx.Scribble(_scribble);
        }

        // ================= Girdi =================

        /// <summary>
        /// Uç ile parmak arasındaki fark. Sıfır: boya parmağın tam altından çıkar. (Eskiden dokunmatikte uç parmağın
        /// ~8 mm önündeydi; çocuk noktayı koymak istediği yere dokununca boya başka yerde çıkıyor, yer değiştirirken
        /// kalemi sürükleyip yolu boyuyordu. Okul öncesi için doğrudan dokunma daha anlaşılır.)
        /// </summary>
        public Vector2 TipOffset => Vector2.zero;

        Vector2 DrawPoint(Vector2 finger) => finger + TipOffset;

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
                    _gesture = true; _multi = true; _moved = true; _touchGesture = true;
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
                PointerScreen = DrawPoint(p);
                PointerOver = InViewport(p);
                bool ended = t.phase is UnityEngine.InputSystem.TouchPhase.Ended or UnityEngine.InputSystem.TouchPhase.Canceled;
                if (!_gesture)
                {
                    // önünde pencere/buton/panel varsa (ör. "Temizle" onayı) tuval bu dokunuşu almaz
                    if (t.phase != UnityEngine.InputSystem.TouchPhase.Began || !InViewport(p) || OverOtherUI(p)) return;
                    BeginGesture(p, false, DrawPoint(p));
                    _touchGesture = true;
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
                        StrokeTo(DrawPoint(e.screenPosition), false);
                    }
                }
                _lastTouchTime = t.time;
                PointerMove(p, DrawPoint(p));
                if (ended) PointerUp(p, DrawPoint(p));
                return;
            }
            // parmakla başlayan hareket, parmaklar kalkınca biter ("bitti" olayı kaçmış olsa bile).
            // Eskiden bu yalnızca faresiz cihazda oluyordu; editörde/faresi olan cihazda tuval
            // yakınlaştırmadan sonra "kaydırılıyor" durumunda takılı kalıp boyamayı engelliyordu.
            if (_gesture && _touchGesture && touches.Count == 0)
            {
                if (_stroking) EndStroke();
                ResetGesture();
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
                if (mouse.leftButton.wasPressedThisFrame && InViewport(mp) && !OverOtherUI(mp)) { BeginGesture(mp, false); PointerMove(mp, mp); }
                else if (panBtnDown && InViewport(mp) && !OverOtherUI(mp)) BeginGesture(mp, true);
                return;
            }
            if (_stroking)
            {
                foreach (var tp in _mouseTrail) StrokeTo(tp, false);
            }
            _mouseTrail.Clear();
            if (mouse.leftButton.isPressed || panBtnHeld) PointerMove(mp, mp);
            else PointerUp(mp, mp);
#endif
        }

        void ResetGesture()
        {
            _gesture = false; _moved = false; _multi = false; _panOnly = false; _touchGesture = false;
        }

        /// <param name="drawAt">Kalemin çizdiği nokta (dokunmatikte parmağın biraz önü); yoksa p.</param>
        void BeginGesture(Vector2 p, bool panOnly, Vector2? drawAt = null)
        {
            _touchGesture = false;
            _gesture = true; _moved = false; _multi = false; _panOnly = panOnly;
            _startPos = _lastPos = p;
            if (!panOnly && (Tool == Tool.Pencil || Tool == Tool.Eraser)) BeginStroke(drawAt ?? p);
        }

        void PointerMove(Vector2 p, Vector2 draw)
        {
            if (_stroking) { StrokeTo(draw); _lastPos = p; return; }
            float slop = Screen.dpi > 0 ? Screen.dpi * 0.06f : 14f;
            if (!_moved && Vector2.Distance(p, _startPos) > slop) _moved = true;
            if (_moved && (Tool == Tool.Fill || Tool == Tool.Stamp || _panOnly)) Pan(p - _lastPos);
            _lastPos = p;
        }

        void PointerUp(Vector2 p, Vector2 draw)
        {
            if (_stroking)
            {
                bool tap = _strokeLength < 6f;
                EndStroke();
                if (tap && Tool == Tool.Eraser) EraseRegionAt(draw);
            }
            else if (!_moved && !_multi && !_panOnly && Tool == Tool.Fill) TapAt(p);
            else if (!_moved && !_multi && !_panOnly && Tool == Tool.Stamp) StampAt(p);
            ResetGesture();
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
            MatF(IdFillRegion, id);
            MatV(IdFillCenter, new Vector4(uv.x, uv.y, 0, 0));
            MatC(IdFillColor, color);
            MatF(IdFillRadius, 0);
            float duration = Mathf.Clamp(0.25f + extent / 2600f, 0.25f, 0.6f);
            _fillAnim = Tween.To(this, duration, k => MatF(IdFillRadius, extent * k), Tween.OutCubic, 0f, CommitAnimation);
            Changed?.Invoke();
        }

        void CommitAnimation()
        {
            if (_animRegion < 0) return;
            Tween.Stop(_fillAnim);
            _fillAnim = null;
            _colorTex.SetPixel(_animRegion % 256, _animRegion / 256, _animColor);
            _colorTex.Apply(false);
            MatF(IdFillRegion, -1);
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

        // ================= Materyal ayarları =================
        // Tuval bir maskenin (Mask) içinde: Unity ekrana materyalin maske KOPYASIYLA çizer. Sonradan
        // asıl materyale yazılan ayarlar (çizgi önizlemesi, kova animasyonu, ipucu) o kopyaya geçmez;
        // kalem çizgisi parmak kalkana kadar görünmüyordu. Ayarlar ikisine birden yazılır.

        Material RenderMat
        {
            get
            {
                if (Image == null || Image.canvasRenderer.materialCount == 0) return null;
                var m = Image.canvasRenderer.GetMaterial(0);
                return m != null && m != Material ? m : null;
            }
        }

        void MatF(int id, float v) { Material.SetFloat(id, v); RenderMat?.SetFloat(id, v); }
        void MatV(int id, Vector4 v) { Material.SetVector(id, v); RenderMat?.SetVector(id, v); }
        void MatC(int id, Color v) { Material.SetColor(id, v); RenderMat?.SetColor(id, v); }
        void MatT(int id, Texture v) { Material.SetTexture(id, v); RenderMat?.SetTexture(id, v); }

        // ================= Tahmini kalem ucu =================
        // Dokunuştan ekrana 1–2 kare gecikme olur (Android + Unity). Kalem çizgisi parmağın gerisinde
        // kalmasın diye çizginin ucuna, parmağın hızına göre 1–2 kare ilerisine uzanan geçici bir parça
        // çizilir; bir sonraki karede gerçek boya oraya gelince yerini ona bırakır.

        RectTransform _tip;
        Image _tipBody, _tipEndA, _tipEndB;
        Vector2 _ptrPrev, _ptrVel;
        bool _ptrHas;

        void EnsureTip()
        {
            if (_tip != null) return;
            _tip = UIKit.Node("PredictTip", Image.transform).Stretch();
            UnityEngine.UI.Image Part(string n, Sprite sp)
            {
                var img = UIKit.Box(_tip, n, Color.white, sp);
                img.type = UnityEngine.UI.Image.Type.Simple;
                img.raycastTarget = false;
                img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                return img;
            }
            _tipBody = Part("Body", null);
            _tipBody.sprite = null;
            _tipEndA = Part("EndA", UIKit.Circle);
            _tipEndB = Part("EndB", UIKit.Circle);
        }

        static readonly int IdCalm = Shader.PropertyToID("_LofiCalm");

        void LateUpdate()
        {
            // sakin mod: sihirli boyaların parıltısı ve ipucunun yanıp sönmesi durur
            Shader.SetGlobalFloat(IdCalm, Motion.Reduced ? 1f : 0f);
            if (Level == null || Image == null) return;
            float dt = Mathf.Max(Time.unscaledDeltaTime, 1e-4f);
            var p = PointerScreen;
            if (_ptrHas) _ptrVel = Vector2.Lerp(_ptrVel, (p - _ptrPrev) / dt, 0.6f);

            var move = p - _ptrPrev;
            _ptrPrev = p;
            _ptrHas = true;
            // farede gecikme zaten az (vSync kapalı, ara noktalar okunuyor): uzatma yok.
            // Dokunmatikte yavaşken ya da yön değiştirirken de yok: yanlış yöne fırlayıp geri çekilmesin.
            float speed = _ptrVel.magnitude;
            bool steady = speed > 250f && move.sqrMagnitude > 1f && Vector2.Dot(_ptrVel / speed, move.normalized) > 0.85f;
            bool show = _stroking && Tool == Tool.Pencil && _strokeLength > 0f && TouchInput && steady;
            if (!show)
            {
                if (_tip != null && _tip.gameObject.activeSelf) _tip.gameObject.SetActive(false);
                return;
            }
            EnsureTip();
            _tip.gameObject.SetActive(true);
            var r = Image.rectTransform.rect;
            // çizginin son boyanan noktası (yerel) ve parmağın 1–2 kare sonraki tahmini yeri
            var a = new Vector2(r.xMin + _lastStampUv.x * r.width, r.yMin + _lastStampUv.y * r.height) - r.center;
            // ~1,5 kare ileri (60 Hz'de ~25 ms); en çok 70 px
            float lead = Mathf.Clamp(dt * 1.5f, 0.012f, 0.025f);
            var pred = p + Vector2.ClampMagnitude(_ptrVel * lead, 70f);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Image.rectTransform, pred, null, out var bl)) return;
            var b = bl - r.center;
            float thick = BrushRadiusPaintPx() * 2f * r.width / Paint.Width * 0.9f;
            var col = CurrentColor;
            col.a = 0.9f;
            var d = b - a;
            _tipBody.rectTransform.anchoredPosition = (a + b) * 0.5f;
            _tipBody.rectTransform.sizeDelta = new Vector2(d.magnitude, thick);
            _tipBody.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            _tipEndA.rectTransform.anchoredPosition = a;
            _tipEndB.rectTransform.anchoredPosition = b;
            _tipEndA.rectTransform.sizeDelta = _tipEndB.rectTransform.sizeDelta = new Vector2(thick, thick);
            _tipBody.color = _tipEndA.color = _tipEndB.color = col;
        }

        // ================= Kalem kaplama ızgarası =================
        // Kalemle bir bölgeye tek çizgi çekmek o bölgeyi "boyandı" saymasın: resim CoverCell piksellik
        // karelere bölünür, kalemin değdiği kareler işaretlenir; bölge, karelerinin CoverNeeded kadarı
        // kaplanınca boyanmış sayılır (yüzde ve "bitti" buna göre). Kalem bölge dışına taşabiliyorsa
        // değdiği her bölge ayrı ayrı sayılır.

        const int CoverCell = 8;
        const float CoverNeeded = 0.6f;
        int _gw, _gh;
        int[] _cellRegion, _regionCells, _coveredCells;
        bool[] _covered;
        readonly HashSet<int> _strokeRegions = new();

        void BuildCoverGrid()
        {
            int n = Level.Count;
            _gw = Mathf.Max(1, Mathf.CeilToInt(Level.Width / (float)CoverCell));
            _gh = Mathf.Max(1, Mathf.CeilToInt(Level.Height / (float)CoverCell));
            _cellRegion = new int[_gw * _gh];
            _covered = new bool[_gw * _gh];
            _regionCells = new int[n + 1];
            _coveredCells = new int[n + 1];
            for (int y = 0; y < _gh; y++)
                for (int x = 0; x < _gw; x++)
                {
                    int r = Level.RegionAt(Mathf.Min(Level.Width - 1, x * CoverCell + CoverCell / 2), Mathf.Min(Level.Height - 1, y * CoverCell + CoverCell / 2));
                    if (r < 1 || r > n) r = 0;
                    _cellRegion[y * _gw + x] = r;
                    if (r > 0) _regionCells[r]++;
                }
        }

        /// <summary>Kalem damgasının altındaki kareleri kaplanmış işaretler.</summary>
        void Cover(Vector2 uv, float radiusPaintPx)
        {
            bool erase = Tool == Tool.Eraser;
            if ((Tool != Tool.Pencil && !erase) || _covered == null || _strokeUndo == null) return;
            float rx = radiusPaintPx / Paint.Width * Level.Width / CoverCell;   // kare biriminde yarıçap
            float cx = uv.x * Level.Width / CoverCell - 0.5f, cy = uv.y * Level.Height / CoverCell - 0.5f;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx)), x1 = Mathf.Min(_gw - 1, Mathf.CeilToInt(cx + rx));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - rx)), y1 = Mathf.Min(_gh - 1, Mathf.CeilToInt(cy + rx));
            float r2 = rx * rx;
            bool inside = SaveSystem.PencilStaysInside;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    if (dx * dx + dy * dy > r2) continue;
                    int c = y * _gw + x;
                    int reg = _cellRegion[c];
                    if (erase)
                    {
                        // silgi: kalemin kapladığı kareleri geri açar; bölge eşiğin altına düşerse "boyandı" olmaktan çıkar
                        if (!_covered[c] || reg == 0) continue;
                        _covered[c] = false;
                        _coveredCells[reg]--;
                        (_strokeUndo.Uncovered ??= new List<int>()).Add(c);
                        _strokeRegions.Add(reg);
                        continue;
                    }
                    if (_covered[c]) continue;
                    if (reg == 0 || (inside && reg != _strokeRegion)) continue;
                    _covered[c] = true;
                    _coveredCells[reg]++;
                    (_strokeUndo.Cells ??= new List<int>()).Add(c);
                    _strokeRegions.Add(reg);
                }
        }

        Vector2 _lastCoverUv;
        bool _coverFresh;

        void AddPencilStamp(Vector2 uv, float radiusPaintPx)
        {
            Paint.AddStamp(uv, radiusPaintPx);
            // kaplama hesabı: damgalar yarım ızgara karesinden sık; her damgada değil, yarım kare ilerleyince
            float dx = (uv.x - _lastCoverUv.x) * Level.Width / CoverCell, dy = (uv.y - _lastCoverUv.y) * Level.Height / CoverCell;
            float rx = radiusPaintPx / Paint.Width * Level.Width / CoverCell;   // kare biriminde yarıçap
            float step = Mathf.Max(0.5f, rx * 0.5f);
            if (!_coverFresh && dx * dx + dy * dy < step * step) return;
            _coverFresh = false;
            _lastCoverUv = uv;
            Cover(uv, radiusPaintPx);
        }

        /// <summary>Çizgi bitince: yeterince kaplanan bölgeler boyanmış sayılır.</summary>
        void ApplyCoverage()
        {
            foreach (int reg in _strokeRegions)
            {
                if (Brushed[reg] || !CoverEnough(reg)) continue;
                (_strokeUndo.BrushedBefore ??= new List<(int, bool)>()).Add((reg, false));
                Brushed[reg] = true;
            }
            _strokeRegions.Clear();
        }

        // çok küçük bölgeler (birkaç kare) tek dokunuşla boyanmış sayılır
        bool CoverEnough(int reg)
        {
            int total = _regionCells[reg];
            return total <= 3 ? _coveredCells[reg] > 0 : _coveredCells[reg] >= total * CoverNeeded;
        }

        /// <summary>Silgi çizgisi bitince: kalem izi silinen bölgeler artık boyanmış sayılmaz.</summary>
        void ApplyErase()
        {
            foreach (int reg in _strokeRegions)
            {
                if (!Brushed[reg] || CoverEnough(reg)) continue;
                (_strokeUndo.BrushedBefore ??= new List<(int, bool)>()).Add((reg, true));
                Brushed[reg] = false;
            }
            _strokeRegions.Clear();
        }

        void RecoverCells(List<int> cells)
        {
            if (cells == null || _covered == null) return;
            foreach (int c in cells)
            {
                if (_covered[c]) continue;
                _covered[c] = true;
                _coveredCells[_cellRegion[c]]++;
            }
        }

        string EncodeCover()
        {
            if (_covered == null) return null;
            var bytes = new byte[(_covered.Length + 7) / 8];
            bool any = false;
            for (int i = 0; i < _covered.Length; i++)
                if (_covered[i]) { bytes[i >> 3] |= (byte)(1 << (i & 7)); any = true; }
            return any ? Convert.ToBase64String(bytes) : null;
        }

        /// <summary>Kayıttan kaplama ızgarasını geri yükler. Eski kayıtlarda (ızgara yok) kalemle boyanmış bölgelerin tamamı kaplanmış sayılır.</summary>
        void RestoreCover(string data)
        {
            if (_covered == null) return;
            byte[] bytes = null;
            if (!string.IsNullOrEmpty(data))
            {
                try { bytes = Convert.FromBase64String(data); } catch (FormatException) { bytes = null; }
                if (bytes != null && bytes.Length != (_covered.Length + 7) / 8) bytes = null;
            }
            for (int c = 0; c < _covered.Length; c++)
            {
                int reg = _cellRegion[c];
                if (reg == 0) continue;
                bool on = bytes != null ? (bytes[c >> 3] & (1 << (c & 7))) != 0 : Brushed[reg];
                if (!on) continue;
                _covered[c] = true;
                _coveredCells[reg]++;
            }
        }

        void UncoverCells(List<int> cells)
        {
            if (cells == null || _covered == null) return;
            foreach (int c in cells)
            {
                if (!_covered[c]) continue;
                _covered[c] = false;
                _coveredCells[_cellRegion[c]]--;
            }
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
            // kalem bölgenin içinde kalıyorsa çizgi üstünden başlamaz; serbest kalemde (büyükler) çizginin üstünden de başlanabilir
            if (Tool == Tool.Pencil && _strokeRegion <= 0 && SaveSystem.PencilStaysInside) return;
            CommitAnimation();
            _stroking = true;
            _strokeStartTime = Time.unscaledTime;
            _strokeLength = 0;
            _strokeUndo = new UndoStep();
            MatT(IdStrokeTex, Paint.Stroke);
            MatC(IdStrokeColor, CurrentColor);
            MatF(IdStrokeMode, Tool == Tool.Eraser ? 2 : 1);
            if (Tool == Tool.Pencil && _strokeRegion > 0)
            {
                _strokeUndo.BrushedRegion = _strokeRegion;
                _strokeUndo.WasBrushed = Brushed[_strokeRegion];
                if (_strokeRegion == _hintRegion) StopHint();
            }
            _lastStampUv = uv;
            _strokeRegions.Clear();
            _coverFresh = true;
#if ENABLE_INPUT_SYSTEM
            _mouseTrail.Clear();
#endif
            AddPencilStamp(uv, BrushRadiusPaintPx());
            FlushStroke();
        }

        void StrokeTo(Vector2 screen, bool flush = true)
        {
            if (!_stroking) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Image.rectTransform, screen, null, out var local);
            var r = Image.rectTransform.rect;
            var uv = new Vector2((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height);
            float radius = BrushRadiusPaintPx();
            var a = new Vector2(_lastStampUv.x * Paint.Width, _lastStampUv.y * Paint.Height);
            var b = new Vector2(uv.x * Paint.Width, uv.y * Paint.Height);
            float dist = Vector2.Distance(a, b);
            float spacing = Mathf.Max(0.75f, radius * 0.3f);
            if (dist < spacing) { if (flush) FlushStroke(); return; }
            int steps = Mathf.FloorToInt(dist / spacing);
            // çok hızlı çizgide damga sayısı sınırlı: aralığı açarak sonuna kadar yay (eskiden çizgi yarıda kopuyordu)
            if (steps > 400) { steps = 400; spacing = dist / steps; }
            for (int i = 1; i <= steps; i++)
            {
                var p = Vector2.Lerp(a, b, i * spacing / dist);
                AddPencilStamp(new Vector2(p.x / Paint.Width, p.y / Paint.Height), radius * UnityEngine.Random.Range(0.92f, 1.04f));
            }
            // son damgayı tam parmağın altına koy: boya kalemin ucundan geri kalmasın
            if (steps * spacing < dist - 0.01f) AddPencilStamp(uv, radius);
            _lastStampUv = uv;
            float screenDist = dist / Paint.Width * Content.sizeDelta.x * _zoom;
            _strokeLength += screenDist;
            _scribble = Mathf.Clamp01(_scribble + screenDist / 120f);
            if (flush) FlushStroke();
        }

        void FlushStroke()
        {
            if (Tool == Tool.Eraser) Paint.Flush(-1, 1f, 0.5f, 0.25f);
            // Küçükler için kalem başladığı bölgenin dışına taşmaz; büyükler ayarlardan kapatıp
            // çizgiye kendisi uymayı dener (ince motor becerisi).
            else Paint.Flush(SaveSystem.PencilStaysInside ? _strokeRegion : -1, 1f, 0.5f, 0.85f);
        }

        void EndStroke()
        {
            if (!_stroking) return;
            FlushStroke();   // bu karede toplanıp henüz basılmamış damgalar
            _stroking = false;
            _strokeUndo.Paint = Paint.Snapshot(); // işlenmeden önceki hâl
            _strokeUndo.WasStamped = _stamped;
            Paint.Commit(CurrentColor, Tool == Tool.Eraser);
            MatF(IdStrokeMode, 0);
            if (Tool == Tool.Pencil) { ApplyCoverage(); _stamped = true; }
            else if (Tool == Tool.Eraser) ApplyErase();
            PushUndo(_strokeUndo);
            _strokeUndo = null;
            Changed?.Invoke();
        }

        void CancelStroke()
        {
            if (!_stroking) return;
            _stroking = false;
            Paint.CancelStroke();
            MatF(IdStrokeMode, 0);
            UncoverCells(_strokeUndo?.Cells);
            RecoverCells(_strokeUndo?.Uncovered);
            _strokeRegions.Clear();
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
            var step = new UndoStep { Paint = Paint.Snapshot(), WasStamped = _stamped };
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

        void Release(UndoStep s)
        {
            if (s?.Paint == null) return;
            if (Paint != null) Paint.Recycle(s.Paint);
            else { s.Paint.Release(); Destroy(s.Paint); }
            s.Paint = null;
        }

        public void Undo()
        {
            if (_undo.Count == 0 || _stroking) return;
            CommitAnimation();
            var s = _undo[^1];
            _undo.RemoveAt(_undo.Count - 1);
            if (s.Paint != null) { Paint.Restore(s.Paint); _stamped = s.WasStamped; }
            if (s.Cleared != null) RestoreCleared(s.Cleared);
            if (s.BrushedRegion > 0 && s.BrushedBefore == null && s.Cells == null) Brushed[s.BrushedRegion] = s.WasBrushed;
            UncoverCells(s.Cells);
            RecoverCells(s.Uncovered);
            if (s.BrushedBefore != null) foreach (var (reg, was) in s.BrushedBefore) Brushed[reg] = was;
            if (s.Region > 0) SetRegion(s.Region, s.Color, s.WasFilled);
            Release(s);
            Sfx.Undo();
            Changed?.Invoke();
        }

        void RestoreCleared(ClearState c)
        {
            for (int i = 1; i <= Level.Count; i++)
            {
                RegionColors[i] = c.Colors[i];
                Filled[i] = c.Filled[i];
                Brushed[i] = c.Brushed[i];
                _colorTex.SetPixel(i % 256, i / 256, Filled[i] ? RegionColors[i] : (Color32)Theme.Paper);
            }
            _colorTex.Apply(false);
            if (c.Covered != null && _covered != null)
            {
                Array.Copy(c.Covered, _covered, _covered.Length);
                Array.Clear(_coveredCells, 0, _coveredCells.Length);
                for (int k = 0; k < _covered.Length; k++) if (_covered[k]) _coveredCells[_cellRegion[k]]++;
            }
            _stamped = c.Stamped;
            MarkedDone = c.MarkedDone;
        }

        // ================= İpucu =================

        /// <summary>Boyanmamış en büyük bölgeyi parlatır ve gerekirse oraya yakınlaşır.</summary>
        public bool ShowHint()
        {
            int best = -1;
            int bestArea = -1;
            for (int i = 1; i <= Level.Count; i++)
                if (!Filled[i] && !Brushed[i] && !IsSkipped(i) && Level.Meta.area[i - 1] > bestArea) { bestArea = Level.Meta.area[i - 1]; best = i; }
            if (best < 0) return false;
            StopHint();
            _hintRegion = best;
            MatF(IdHint, best);

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
            if (Material != null) MatF(IdHint, -1);
        }

        // ================= Kayıt yardımcıları =================

        /// <summary>Galeri küçük resminin pikselleri (doku oluşturmadan; kayıt arka planda PNG'ye çevirir).</summary>
        public Color32[] MakeThumbnailPixels(Color32[] paint, out int w, out int h)
        {
            CommitAnimation();
            var cols = new Color32[RegionColors.Length];
            for (int i = 0; i < cols.Length; i++) cols[i] = Filled[i] ? RegionColors[i] : (Color32)Theme.Paper;
            return Level.RenderThumbnailPixels(cols, paint, paint != null ? Paint.Width : 0, paint != null ? Paint.Height : 0, 360, out w, out h);
        }

        /// <summary>Galeri küçük resmi; boya katmanı varsa onu da içerir.</summary>
        public Texture2D MakeThumbnail(Texture2D paintReadback)
        {
            CommitAnimation();
            var cols = new Color32[RegionColors.Length];
            for (int i = 0; i < cols.Length; i++) cols[i] = Filled[i] ? RegionColors[i] : (Color32)Theme.Paper;
            return Level.RenderThumbnail(cols, paintReadback);
        }

        /// <summary>Tüm boyamayı temizle. Yanlışlıkla basıldıysa tek "geri al" ile resmin tamamı geri gelir.</summary>
        public void ClearAll()
        {
            CommitAnimation();
            foreach (var s in _undo) Release(s);
            _undo.Clear();
            var step = new UndoStep
            {
                Paint = Paint.Snapshot(),
                Cleared = new ClearState
                {
                    Colors = (Color32[])RegionColors.Clone(),
                    Filled = (bool[])Filled.Clone(),
                    Brushed = (bool[])Brushed.Clone(),
                    Covered = _covered != null ? (bool[])_covered.Clone() : null,
                    Stamped = _stamped,
                    MarkedDone = MarkedDone,
                },
            };
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
            MarkedDone = false;
            if (_covered != null) { Array.Clear(_covered, 0, _covered.Length); Array.Clear(_coveredCells, 0, _coveredCells.Length); }
            PushUndo(step);
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
