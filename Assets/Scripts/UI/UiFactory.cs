using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace KanjiMaster.UI
{
    /// <summary>
    /// Small runtime UI-builder helpers used by the code-driven Learn screens.
    /// Not a UI framework — just factories for Canvas / text / buttons / scroll lists
    /// so the Learn screens and the reusable KanjiDetailView can build responsive UI
    /// without authored prefabs. Uses a 1080×1920 Canvas Scaler and layout groups so
    /// it adapts across phone/tablet aspect ratios.
    /// </summary>
    public static class UiFactory
    {
        public static readonly Color Bg = new Color(0.08f, 0.09f, 0.12f);
        public static readonly Color Panel = new Color(0.14f, 0.15f, 0.20f);
        public static readonly Color ButtonBg = new Color(0.20f, 0.22f, 0.30f);
        public static readonly Color Ink = new Color(0.93f, 0.94f, 0.97f);
        public static readonly Color Sub = new Color(0.62f, 0.65f, 0.74f);
        public static readonly Color Accent = new Color(0.36f, 0.55f, 0.95f);

        /// <summary>Ensure a camera + EventSystem exist so a near-empty scene works.</summary>
        public static void EnsureCore(Color? background = null)
        {
            if (Camera.main == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = background ?? Bg;
                camGo.transform.position = new Vector3(0, 0, -10);
            }
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }
        }

        public static Canvas CreateCanvas(string name = "Canvas")
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>Full-screen panel that follows the device safe area.</summary>
        public static RectTransform SafeArea(Transform parent)
        {
            var go = new GameObject("SafeArea", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            Stretch(rt);
            go.AddComponent<SafeAreaFitter>();
            return rt;
        }

        /// <summary>A vertical layout container. Caller positions the returned object.</summary>
        public static VerticalLayoutGroup Column(Transform parent, string name, int padding, int spacing,
            TextAnchor align = TextAnchor.UpperCenter)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            go.transform.SetParent(parent, false);
            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(padding, padding, padding, padding);
            vlg.spacing = spacing;
            vlg.childAlignment = align;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            return vlg;
        }

        /// <summary>A horizontal layout row that splits its children evenly across the width.</summary>
        public static HorizontalLayoutGroup Row(Transform parent, string name, int padding, int spacing,
            float preferredHeight = -1f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(parent, false);
            var h = go.GetComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(padding, padding, padding, padding);
            h.spacing = spacing;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            if (preferredHeight > 0f)
            {
                var le = go.AddComponent<LayoutElement>();
                le.minHeight = preferredHeight;
                le.preferredHeight = preferredHeight;
            }
            return h;
        }

        public static TextMeshProUGUI Label(Transform parent, string text, float fontSize,
            TextAlignmentOptions align, Color color, float preferredHeight = -1f)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            Stretch(t.rectTransform);
            if (preferredHeight > 0f)
            {
                var le = go.AddComponent<LayoutElement>();
                le.minHeight = preferredHeight;
                le.preferredHeight = preferredHeight;
            }
            return t;
        }

        public static Image Panelette(Transform parent, Color color, float preferredHeight = -1f)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            if (preferredHeight > 0f)
            {
                var le = go.AddComponent<LayoutElement>();
                le.minHeight = preferredHeight;
                le.preferredHeight = preferredHeight;
            }
            return img;
        }

        public static Button Button(Transform parent, string label, float fontSize, Color bg,
            float height, out TextMeshProUGUI labelText, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = bg;
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;

            labelText = Label(go.transform, label, fontSize, align, Ink);
            labelText.rectTransform.offsetMin = new Vector2(24, 0);
            labelText.rectTransform.offsetMax = new Vector2(-24, 0);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            return btn;
        }

        /// <summary>A vertical ScrollRect. Returns the scroll and its content column
        /// (a VerticalLayoutGroup + ContentSizeFitter) — add rows to the content.</summary>
        public static (ScrollRect scroll, RectTransform content) ScrollColumn(Transform parent, int padding, int spacing)
        {
            var scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(LayoutElement));
            scrollGo.transform.SetParent(parent, false);
            var scroll = scrollGo.GetComponent<ScrollRect>();
            var scrollLe = scrollGo.GetComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1f;
            scrollLe.flexibleWidth = 1f;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewport = (RectTransform)viewportGo.transform;
            Stretch(viewport);

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var content = (RectTransform)contentGo.transform;
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(padding, padding, padding, padding);
            vlg.spacing = spacing;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            return (scroll, content);
        }
    }
}
