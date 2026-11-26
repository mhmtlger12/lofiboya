#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif

namespace LofiBoya.Editor
{
    /// <summary>
    /// Unity Device Simulator (SimulatorWindow) penceresinde fare basılı değilken de (hover)
    /// ekran koordinatını okuyup ColoringCanvas'a iletir; böylece Simulator'da da tıpkı
    /// Windows Paint ve Game ekranındaki gibi kalem tuval üzerinde imleci takip eder.
    /// </summary>
    [InitializeOnLoad]
    static class SimulatorHoverBridge
    {
        static readonly Type SimWinType;
        static readonly FieldInfo MainField;
        static MethodInfo _vec2Converter;
        static MethodInfo _tryConverter;
        static bool _reflectionChecked;

        static EditorWindow _hookedWindow;
        static VisualElement _hookedRoot;
        static VisualElement _previewElem;
        static bool _inside;
        static Vector2 _panelPos;
        static bool _hideCursor;
        static bool _appliedHide;
        static Texture2D _blankCursor;

        // Tıklama ile otomatik alt-piksel çerçeve (bezel) kalibrasyonu
        static bool _hasSampleA, _hasCalib;
        static Vector2 _normA, _scrA;
        static Vector2 _calibScale, _calibOffset; // norm (0..1) -> screen (0..W, 0..H)
        static Vector2Int _calibScreen;

        static SimulatorHoverBridge()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                SimWinType = asm.GetType("UnityEditor.DeviceSimulation.SimulatorWindow", false);
                if (SimWinType != null) break;
            }
            if (SimWinType != null)
                MainField = SimWinType.GetField("m_Main", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            ColoringCanvas.EditorHoverProvider = GetHover;
            ColoringCanvas.EditorHideCursor = SetHideCursor;
            EditorApplication.update += OnEditorUpdate;
        }

        static void OnEditorUpdate()
        {
            if (SimWinType == null) return;
            var over = EditorWindow.mouseOverWindow;
            if (over != null && over.GetType() == SimWinType)
            {
                EnsureHooked(over);
                CalibrateFromActiveTouch();
                ApplyCursorStyle(_hideCursor && EditorApplication.isPlaying && _inside);
                if (EditorApplication.isPlaying && _inside)
                    EditorApplication.QueuePlayerLoopUpdate();
            }
            else
            {
                if (_inside && over != _hookedWindow) _inside = false;
                if (!EditorApplication.isPlaying) _hideCursor = false;
                ApplyCursorStyle(false);
            }
        }

        static void EnsureHooked(EditorWindow win)
        {
            if (_hookedWindow == win && _hookedRoot == win.rootVisualElement && _previewElem != null) return;
            Unhook();
            _hookedWindow = win;
            _hookedRoot = win.rootVisualElement;
            if (_hookedRoot == null) return;
            _previewElem = FindPreviewElement(_hookedRoot);
            _hookedRoot.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            _hookedRoot.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            _hookedRoot.RegisterCallback<PointerEnterEvent>(OnPointerEnter, TrickleDown.TrickleDown);
            _hookedRoot.RegisterCallback<PointerLeaveEvent>(OnPointerLeave, TrickleDown.TrickleDown);
        }

        static void Unhook()
        {
            ApplyCursorStyle(false);
            if (_hookedRoot != null)
            {
                _hookedRoot.UnregisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
                _hookedRoot.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
                _hookedRoot.UnregisterCallback<PointerEnterEvent>(OnPointerEnter, TrickleDown.TrickleDown);
                _hookedRoot.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave, TrickleDown.TrickleDown);
            }
            _hookedRoot = null;
            _hookedWindow = null;
            _previewElem = null;
            _inside = false;
        }

        static void OnPointerMove(PointerMoveEvent evt)
        {
            _panelPos = evt.position;
            _inside = true;
            CalibrateFromActiveTouch();
            if (EditorApplication.isPlaying) EditorApplication.QueuePlayerLoopUpdate();
        }

        static void OnPointerDown(PointerDownEvent evt)
        {
            _panelPos = evt.position;
            _inside = true;
        }

        static void OnPointerEnter(PointerEnterEvent evt) { _panelPos = evt.position; _inside = true; }
        static void OnPointerLeave(PointerLeaveEvent evt) { _inside = false; ApplyCursorStyle(false); }

        static void SetHideCursor(bool hide)
        {
            _hideCursor = hide;
            ApplyCursorStyle(hide && EditorApplication.isPlaying && _inside);
        }

        static void ApplyCursorStyle(bool hide)
        {
            if (_appliedHide == hide || _hookedRoot == null) return;
            _appliedHide = hide;
            try
            {
                if (hide)
                {
                    if (_blankCursor == null)
                    {
                        _blankCursor = new Texture2D(32, 32, TextureFormat.RGBA32, false)
                        {
                            name = "BlankSimCursor",
                            alphaIsTransparency = true,
                            filterMode = FilterMode.Point
                        };
                        var clear = new Color32[32 * 32];
                        _blankCursor.SetPixels32(clear);
                        _blankCursor.Apply(false, false);
                    }
                    _hookedRoot.style.cursor = new StyleCursor(new UnityEngine.UIElements.Cursor { texture = _blankCursor, hotspot = Vector2.zero });
                }
                else
                {
                    _hookedRoot.style.cursor = StyleKeyword.Null;
                }
            }
            catch { /* Editor sürümü imleç stilini desteklemiyorsa yoksay */ }
        }

        static (bool active, Vector2 screenPos) GetHover()
        {
            if (!_inside || _hookedWindow == null || _hookedRoot == null) return (false, default);
            if (_previewElem == null) _previewElem = FindPreviewElement(_hookedRoot);
            if (TryMapToScreen(_panelPos, out var sp)) return (true, sp);
            return (false, default);
        }

        static void CalibrateFromActiveTouch()
        {
#if ENABLE_INPUT_SYSTEM
            if (!EditorApplication.isPlaying || !UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled) return;
            var touches = ETouch.activeTouches;
            if (touches.Count != 1 || !_inside) return;
            if (_previewElem == null && _hookedRoot != null) _previewElem = FindPreviewElement(_hookedRoot);
            if (_previewElem == null) return;
            var r = _previewElem.worldBound;
            if (r.width <= 10f || r.height <= 10f || !r.Contains(_panelPos)) return;

            var curScreen = new Vector2Int(Screen.width, Screen.height);
            if (_calibScreen != curScreen) { _calibScreen = curScreen; _hasSampleA = _hasCalib = false; }

            var norm = new Vector2((_panelPos.x - r.xMin) / r.width, 1f - (_panelPos.y - r.yMin) / r.height);
            var scr = touches[0].screenPosition;
            if (!_hasSampleA)
            {
                _normA = norm;
                _scrA = scr;
                _hasSampleA = true;
                // Merkez simetrisini kullanarak tek noktadan bile çerçeve payını tahmin et
                if (Mathf.Abs(norm.y - 0.5f) > 0.08f)
                {
                    float sy = (scr.y - Screen.height * 0.5f) / (norm.y - 0.5f);
                    if (sy > Screen.height * 0.8f && sy < Screen.height * 1.5f)
                    {
                        float sx = sy * (r.width / r.height) * ((float)Screen.height / Mathf.Max(1, Screen.width));
                        // Ekran en-boy oranını koruyarak merkezden ölçekle
                        float sxAspect = Screen.width * (sy / Screen.height) * ((r.width / r.height) / ((float)Screen.width / Screen.height));
                        _calibScale = new Vector2(sxAspect, sy);
                        _calibOffset = new Vector2(Screen.width * 0.5f - 0.5f * sxAspect, Screen.height * 0.5f - 0.5f * sy);
                        _hasCalib = true;
                    }
                }
                return;
            }
            if (Mathf.Abs(norm.x - _normA.x) > 0.08f && Mathf.Abs(norm.y - _normA.y) > 0.08f)
            {
                float sx = (scr.x - _scrA.x) / (norm.x - _normA.x);
                float sy = (scr.y - _scrA.y) / (norm.y - _normA.y);
                if (sx > Screen.width * 0.8f && sx < Screen.width * 1.6f && sy > Screen.height * 0.8f && sy < Screen.height * 1.6f)
                {
                    _calibScale = new Vector2(sx, sy);
                    _calibOffset = scr - Vector2.Scale(norm, _calibScale);
                    _hasCalib = true;
                }
            }
#endif
        }

        static bool TryMapToScreen(Vector2 panelPos, out Vector2 screenPos)
        {
            screenPos = default;
            if (_hookedWindow == null || _hookedRoot == null) return false;

            // 1) DeviceSimulatorMain üzerinde doğrudan dönüşüm metodu varsa kullan
            var main = MainField?.GetValue(_hookedWindow);
            if (main != null && TryConvertViaMain(main, panelPos, out screenPos))
                return true;

            // 2) Önizleme görseli üzerinden hesapla (+ varsa tıklama kalibrasyonu)
            if (_previewElem == null) _previewElem = FindPreviewElement(_hookedRoot);
            if (_previewElem == null) return false;
            var r = _previewElem.worldBound;
            if (r.width <= 10f || r.height <= 10f || !r.Contains(panelPos)) return false;

            var norm = new Vector2((panelPos.x - r.xMin) / r.width, 1f - (panelPos.y - r.yMin) / r.height);
            if (_hasCalib)
            {
                screenPos = _calibOffset + Vector2.Scale(norm, _calibScale);
                return screenPos.x >= 0f && screenPos.x <= Screen.width && screenPos.y >= 0f && screenPos.y <= Screen.height;
            }

            // Çerçeve (bezel) varsa bile cihaz ekranı ortalanmıştır; en-boy oranından iç ekran kutusunu bul
            float screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            float elemAspect = r.width / r.height;
            Rect inner = r;
            if (Mathf.Abs(elemAspect - screenAspect) > 0.01f)
            {
                if (elemAspect > screenAspect)
                {
                    float w = r.height * screenAspect;
                    inner = new Rect(r.center.x - w * 0.5f, r.yMin, w, r.height);
                }
                else
                {
                    float h = r.width / screenAspect;
                    inner = new Rect(r.xMin, r.center.y - h * 0.5f, r.width, h);
                }
            }
            // Tipik telefon çerçeve payı (~%3.5 kenar boşluğu) kalibrasyon öncesi yaklaşık düzeltme
            if (!(_previewElem is Image img && img.image is RenderTexture))
            {
                float padX = inner.width * 0.032f, padY = inner.height * 0.032f;
                inner = new Rect(inner.xMin + padX, inner.yMin + padY, inner.width - padX * 2f, inner.height - padY * 2f);
            }
            if (!inner.Contains(panelPos)) return false;
            float u = (panelPos.x - inner.xMin) / inner.width;
            float v = 1f - (panelPos.y - inner.yMin) / inner.height;
            screenPos = new Vector2(u * Screen.width, v * Screen.height);
            return true;
        }

        static bool TryConvertViaMain(object main, Vector2 panelPos, out Vector2 screenPos)
        {
            screenPos = default;
            if (!_reflectionChecked)
            {
                _reflectionChecked = true;
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                foreach (var m in main.GetType().GetMethods(flags))
                {
                    if (!m.Name.Contains("Screen") && !m.Name.Contains("Pointer") && !m.Name.Contains("Coord")) continue;
                    var ps = m.GetParameters();
                    if (m.ReturnType == typeof(Vector2) && ps.Length == 1 && ps[0].ParameterType == typeof(Vector2))
                        _vec2Converter = m;
                    else if (m.ReturnType == typeof(bool) && ps.Length == 2 && ps[0].ParameterType == typeof(Vector2) && ps[1].IsOut && ps[1].ParameterType == typeof(Vector2).MakeByRefType())
                        _tryConverter = m;
                }
            }
            try
            {
                Vector2 local = _previewElem != null ? _previewElem.WorldToLocal(panelPos) : panelPos;
                if (_tryConverter != null)
                {
                    object[] args = { local, Vector2.zero };
                    if ((bool)_tryConverter.Invoke(main, args)) { screenPos = (Vector2)args[1]; return true; }
                }
                if (_vec2Converter != null)
                {
                    var res = (Vector2)_vec2Converter.Invoke(main, new object[] { local });
                    if (res.x >= 0f && res.x <= Screen.width && res.y >= 0f && res.y <= Screen.height) { screenPos = res; return true; }
                }
            }
            catch { _tryConverter = null; _vec2Converter = null; }
            return false;
        }

        static VisualElement FindPreviewElement(VisualElement root)
        {
            // Önce doğrudan cihaz ekranını/önizlemesini tutan Image veya preview öğesini ara
            VisualElement best = null;
            float bestArea = 0f;
            void Walk(VisualElement ve)
            {
                if (ve == null || ve.resolvedStyle.display == DisplayStyle.None) return;
                var b = ve.worldBound;
                float area = b.width * b.height;
                if (ve is Image im && im.image != null && area > 5000f)
                {
                    if (im.image is RenderTexture) { best = ve; bestArea = float.MaxValue; return; }
                    if (area > bestArea) { best = ve; bestArea = area; }
                }
                else if ((ve.name.IndexOf("preview", StringComparison.OrdinalIgnoreCase) >= 0 ||
                          ve.name.IndexOf("device", StringComparison.OrdinalIgnoreCase) >= 0) &&
                         area > 10000f && bestArea < float.MaxValue && best == null)
                {
                    best = ve;
                }
                foreach (var c in ve.Children()) Walk(c);
            }
            Walk(root);
            return best;
        }
    }
}
#endif
