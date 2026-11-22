using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LofiBoya
{
    /// <summary>Ekran okuyucunun (TalkBack / VoiceOver) okuyacağı ad. Yazısı olmayan butonlara eklenir.</summary>
    public class A11yLabel : MonoBehaviour
    {
        public string Text;
        public Func<string> Getter;
        public string Resolve() => Getter != null ? Getter() : Text;
    }

    /// <summary>Buton olmayan ama ekran okuyucuyla / anahtarla (switch access) tetiklenebilen öğe (ör. yazma tahtası).</summary>
    public interface IA11yAction
    {
        string A11yText { get; }
        bool A11yAvailable { get; }
        void A11yActivate();
    }

    /// <summary>
    /// Ekran okuyucu desteği. Ekrandaki etkileşimli öğelerden (butonlar, anahtarlar, IA11yAction) ve
    /// büyük başlıklardan Unity'nin erişilebilirlik ağacını kurar; TalkBack açıkken her öğe sırayla okunur ve
    /// çift dokunuşla çalışır. Android Anahtarla Erişim (Switch Access) de aynı ağacı kullanır.
    /// Öğeler, ekranın en üstünde görünen katmana göre süzülür: açılır pencere varken arkadaki ekran okunmaz.
    /// </summary>
    public class A11y : MonoBehaviour
    {
        // ---------------- Kolay kullanım ----------------

        public static void Label(Component c, string text)
        {
            if (c == null) return;
            var l = c.GetComponent<A11yLabel>() ?? c.gameObject.AddComponent<A11yLabel>();
            l.Text = text;
            l.Getter = null;
        }

        public static void Label(Component c, Func<string> getter)
        {
            if (c == null) return;
            var l = c.GetComponent<A11yLabel>() ?? c.gameObject.AddComponent<A11yLabel>();
            l.Getter = getter;
        }

        /// <summary>Ekran okuyucu açıksa kısa bir duyuru okur ("Turuncu").</summary>
        public static void Announce(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            try
            {
                if (AssistiveSupport.isScreenReaderEnabled) AssistiveSupport.notificationDispatcher.SendAnnouncement(Plain(text));
            }
            catch (Exception) { /* desteklenmeyen platform */ }
        }

        /// <summary>Test / hata ayıklama: ekran okuyucu kapalıyken de ağacı kurar.</summary>
        public static bool ForceBuild;

        /// <summary>Son kurulan ağaçtaki öğeler (sırasıyla), test için.</summary>
        public static string Describe()
        {
            var sb = new StringBuilder();
            foreach (var it in _items) sb.Append(it.role).Append(": ").Append(it.label).Append('\n');
            return sb.ToString();
        }

        // ---------------- Ağaç ----------------

        struct Item
        {
            public RectTransform rt;
            public string label;
            public AccessibilityRole role;
            public Action invoke;
            public int id;
        }

        static readonly List<Item> _items = new();
        static readonly HashSet<string> ModalNames = new() { "Panel", "Break", "Confirm", "Complete" };
        AccessibilityHierarchy _hierarchy;
        readonly List<AccessibilityNode> _nodes = new();
        string _signature = "";
        float _next;
        bool _active;
        readonly List<RaycastResult> _hits = new();

        void Update()
        {
            bool on;
            try { on = ForceBuild || AssistiveSupport.isScreenReaderEnabled; }
            catch (Exception) { on = ForceBuild; }
            if (!on)
            {
                if (_active) { try { AssistiveSupport.activeHierarchy = null; } catch (Exception) { } _active = false; _signature = ""; }
                return;
            }
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.4f;
            Collect();
            string sig = Signature();
            if (sig != _signature || !_active)
            {
                _signature = sig;
                Build();
            }
            else
            {
                // aynı öğeler: yalnızca adları (anahtar açık/kapalı gibi) ve yerleri güncelle, odak kaybolmasın
                for (int i = 0; i < _nodes.Count && i < _items.Count; i++)
                    if (_nodes[i].label != _items[i].label) _nodes[i].label = _items[i].label;
                try { _hierarchy?.RefreshNodeFrames(); } catch (Exception) { }
            }
        }

        static string Signature()
        {
            var sb = new StringBuilder();
            foreach (var it in _items) sb.Append(it.id).Append(',');
            return sb.ToString();
        }

        void Build()
        {
            _hierarchy = new AccessibilityHierarchy();
            _nodes.Clear();
            foreach (var it in _items)
            {
                var item = it;
                var node = _hierarchy.AddNode(item.label);
                node.role = item.role;
                node.frameGetter = () => ScreenRect(item.rt);
                if (item.invoke != null)
                    node.invoked += () => { if (item.rt != null) item.invoke(); return true; };
                _nodes.Add(node);
            }
            try
            {
                bool first = !_active;
                AssistiveSupport.activeHierarchy = _hierarchy;
                _active = true;
                if (!first && _nodes.Count > 0) AssistiveSupport.notificationDispatcher.SendLayoutChanged(_nodes[0]);
            }
            catch (Exception) { _active = true; }
        }

        /// <summary>Ekran koordinatı (sol üst köşe başlangıç): Screen Space - Overlay kanvasta dünya köşeleri pikseldir.</summary>
        static Rect ScreenRect(RectTransform rt)
        {
            if (rt == null) return Rect.zero;
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            float xMin = Mathf.Min(c[0].x, c[2].x), xMax = Mathf.Max(c[0].x, c[2].x);
            float yMin = Mathf.Min(c[0].y, c[2].y), yMax = Mathf.Max(c[0].y, c[2].y);
            // ekranla kesişen kısım (tam ekran perdeler ekranın dışına taşar)
            xMin = Mathf.Max(0, xMin); yMin = Mathf.Max(0, yMin);
            xMax = Mathf.Min(Screen.width, xMax); yMax = Mathf.Min(Screen.height, yMax);
            if (xMax <= xMin || yMax <= yMin) return Rect.zero;
            return new Rect(xMin, Screen.height - yMax, xMax - xMin, yMax - yMin);
        }

        void Collect()
        {
            _items.Clear();
            var app = App.I;
            if (app == null || app.Canvas == null) return;
            var es = EventSystem.current;

            // en üstteki katman: ekranın ortasındaki dokunuş hangi açılır pencereye düşüyor
            Transform scope = app.SafeRoot;
            var top = TopHit(es, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            for (var t = top; t != null; t = t.parent)
                if (ModalNames.Contains(t.name)) { scope = t; break; }

            bool modal = scope != app.SafeRoot;
            Item? close = null;

            // butonlar: yalnızca gerçekten dokunulabilir ve en üstte görünenler
            foreach (var b in scope.GetComponentsInChildren<Button>(false))
            {
                if (!b.isActiveAndEnabled || !b.IsInteractable() || Alpha(b) < 0.2f) continue;
                var rt = (RectTransform)b.transform;
                var btn = b;
                if (ModalNames.Contains(b.name))
                {
                    // açılır pencerenin karartılmış arka planı: "Kapat" olarak en sona
                    close = new Item { rt = rt, label = Loc.T("a11y_close"), role = AccessibilityRole.Button, id = b.GetHashCode(),
                                       invoke = () => { if (btn != null) btn.onClick.Invoke(); } };
                    continue;
                }
                if (!OnScreen(rt)) continue;
                var hit = TopHit(es, RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center)));
                if (hit == null || !hit.IsChildOf(b.transform)) continue;
                string label = LabelOf(b);
                if (string.IsNullOrEmpty(label)) continue;
                var hold = b.GetComponent<HoldButton>();
                bool toggle = b.GetComponent<A11yLabel>() is A11yLabel al && al.Getter != null;
                _items.Add(new Item
                {
                    rt = rt, label = label, id = b.GetHashCode(),
                    role = toggle ? AccessibilityRole.Toggle : AccessibilityRole.Button,
                    invoke = hold != null ? hold.Activate : () => { if (btn != null) btn.onClick.Invoke(); },
                });
            }

            // yazılar: ekranda başlık ve sorular (büyük yazılar), açılır pencerede hepsi (bilgi metni dahil).
            // Bir butonun adında zaten geçen yazı (anahtarın yanındaki etiket gibi) tekrar okunmaz.
            int buttons = _items.Count;
            foreach (var txt in scope.GetComponentsInChildren<Text>(false))
            {
                if ((!modal && txt.fontSize < 56) || !txt.isActiveAndEnabled || InsideButton(txt.transform, scope)) continue;
                string s = Plain(txt.text);
                if (s.Length == 0 || Alpha(txt) < 0.2f || !OnScreen(txt.rectTransform)) continue;
                bool dup = false;
                for (int i = 0; i < buttons && !dup; i++) dup = _items[i].label.Contains(s);
                if (dup) continue;
                _items.Add(new Item { rt = txt.rectTransform, label = s, role = AccessibilityRole.StaticText, id = txt.GetHashCode() });
            }

            // buton olmayan eylemler (yazma tahtası: "Sıradaki çizgiyi çiz")
            foreach (var mb in scope.GetComponentsInChildren<MonoBehaviour>(false))
            {
                if (mb is not IA11yAction act || !mb.isActiveAndEnabled || !act.A11yAvailable) continue;
                var rt = (RectTransform)mb.transform;
                _items.Add(new Item { rt = rt, label = act.A11yText, role = AccessibilityRole.Button, invoke = act.A11yActivate, id = mb.GetHashCode() });
            }

            // okuma sırası: yukarıdan aşağı, aynı satırda soldan sağa
            _items.Sort((a, b) =>
            {
                var ra = ScreenRect(a.rt);
                var rb = ScreenRect(b.rt);
                if (Mathf.Abs(ra.center.y - rb.center.y) > Mathf.Max(24f, Mathf.Min(ra.height, rb.height) * 0.5f)) return ra.center.y.CompareTo(rb.center.y);
                return ra.center.x.CompareTo(rb.center.x);
            });
            if (close.HasValue) _items.Add(close.Value);
        }

        /// <summary>Yazı bir butonun içinde mi (açılır pencerenin karartma katmanı sayılmaz).</summary>
        static bool InsideButton(Transform t, Transform scope)
        {
            for (var p = t; p != null && p != scope.parent; p = p.parent)
                if (p.TryGetComponent<Button>(out var b) && !ModalNames.Contains(b.name)) return true;
            return false;
        }

        Transform TopHit(EventSystem es, Vector2 screen)
        {
            if (es == null) return null;
            _hits.Clear();
            es.RaycastAll(new PointerEventData(es) { position = screen }, _hits);
            return _hits.Count > 0 ? _hits[0].gameObject.transform : null;
        }

        static string LabelOf(Button b)
        {
            var l = b.GetComponent<A11yLabel>();
            if (l != null) return l.Resolve();
            var sb = new StringBuilder();
            foreach (var t in b.GetComponentsInChildren<Text>(false))
            {
                string s = Plain(t.text);
                if (s.Length == 0) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(s);
            }
            string label = sb.ToString();
            return label == "‹" ? Loc.T("a11y_back") : label;
        }

        static float Alpha(Component c)
        {
            float a = 1f;
            foreach (var g in c.GetComponentsInParent<CanvasGroup>()) { a *= g.alpha; if (g.ignoreParentGroups) break; }
            if (c is Graphic gr) a *= gr.color.a;
            return a;
        }

        static bool OnScreen(RectTransform rt)
        {
            var r = ScreenRect(rt);
            return r.width > 1 && r.height > 1 && r.xMax > 0 && r.yMax > 0 && r.xMin < Screen.width && r.yMin < Screen.height;
        }

        /// <summary>Zengin metin etiketlerini (&lt;color&gt;) ve satır sonlarını temizler.</summary>
        static string Plain(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length);
            bool tag = false;
            foreach (char ch in s)
            {
                if (ch == '<') { tag = true; continue; }
                if (ch == '>') { tag = false; continue; }
                if (!tag) sb.Append(ch == '\n' ? ' ' : ch);
            }
            return sb.ToString().Trim();
        }
    }
}
