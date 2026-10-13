using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CluckmechCoop
{
    internal static class NativeUi
    {
        internal static Font Font;
        internal static Sprite Frame, Panel, Card, CheckOn, CheckOff;
        internal static Sprite TransferPanel, TransferButton, TransferTexture, TransferCard, GoldIcon, Selection;
        private static readonly List<Sprite> transferSprites = new List<Sprite>();
        private static Sprite menuPlaque;
        internal static Sprite MenuPlaque => menuPlaque != null ? menuPlaque :
            (menuPlaque = TransferSprite("menu-title", Vector4.zero, false, new Rect(101, 98, 1387, 786)));
        internal static Canvas Canvas;
        internal static void Capture(Transform source)
        {
            Canvas = source.GetComponentInParent<Canvas>();
            if (Font == null)
                Font = Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault(x => x.name == "中文像素字体") ?? source.GetComponentsInChildren<Text>(true).FirstOrDefault(x => x.font != null)?.font;
            if (Card == null)
            {
                var sprites = Resources.FindObjectsOfTypeAll<Sprite>();
                Frame = sprites.FirstOrDefault(x => x.name == "bt按钮边框50x17");
                Panel = sprites.FirstOrDefault(x => x.name == "UI_对话框");
                Card = sprites.FirstOrDefault(x => x.name == "UI_黑色底框");
                CheckOn = sprites.FirstOrDefault(x => x.name == "UI_暂停界面开关_1");
                CheckOff = sprites.FirstOrDefault(x => x.name == "UI_暂停界面开关_0");
            }
            if (TransferPanel == null)
            {
                TransferPanel = TransferSprite("panel", new Vector4(6, 6, 6, 7));
                TransferButton = TransferSprite("button", new Vector4(33, 38, 34, 38));
                TransferTexture = TransferSprite("grain", Vector4.zero, true);
                TransferCard = TransferSprite("card", new Vector4(2, 2, 2, 2));
                Selection = TransferSprite("selection", new Vector4(3, 3, 3, 3));
                GoldIcon = TransferSprite("gold", Vector4.zero);
            }
        }
        private static Sprite TransferSprite(string name, Vector4 border, bool repeat = false, Rect? region = null)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            using (var stream = typeof(NativeUi).Assembly.GetManifestResourceStream("CluckmechCoop.UiAssets." + name + ".png"))
            using (var reader = new BinaryReader(stream))
                ImageConversion.LoadImage(texture, reader.ReadBytes((int)stream.Length), true);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            var sprite = Sprite.Create(texture, region ?? new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 25, 0, SpriteMeshType.FullRect, border);
            transferSprites.Add(sprite);
            return sprite;
        }
        internal static void ReleaseTransferSkin()
        {
            foreach (var sprite in transferSprites)
            { UnityEngine.Object.Destroy(sprite.texture); UnityEngine.Object.Destroy(sprite); }
            transferSprites.Clear();
        }
        internal static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
            return rect;
        }
        internal static void CopyRect(RectTransform target, RectTransform source)
        {
            target.anchorMin = source.anchorMin; target.anchorMax = source.anchorMax; target.pivot = source.pivot;
            target.sizeDelta = source.sizeDelta; target.anchoredPosition = source.anchoredPosition;
        }
        internal static Image Image(RectTransform rect, Sprite sprite, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite; image.color = color; image.type = sprite != null && sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            return image;
        }
        internal static Text Label(Transform parent, string text, float x, float y, float w, float h, int size)
        {
            var label = Rect(parent, "Text", x, y, w, h).gameObject.AddComponent<Text>();
            label.font = Font; label.fontSize = size; label.color = Color.white; label.text = text;
            label.alignment = TextAnchor.MiddleLeft; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            return label;
        }
        internal static Button Button(Transform parent, string text, float x, float y, float w, float h, UnityAction action)
        {
            var rect = Rect(parent, text, x, y, w, h);
            var image = Image(rect, Frame, new Color(1, 0.72f, 0.16f));
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(1, 0.9f, 0.58f); colors.pressedColor = new Color(0.7f, 0.7f, 0.7f); button.colors = colors;
            button.onClick.AddListener(action);
            var title = Label(rect, text, 8, 0, w - 16, h, 24); title.alignment = TextAnchor.MiddleCenter; title.color = new Color(0.18f, 0.17f, 0.14f);
            return button;
        }
        internal static Button TransferAction(Transform parent, string text, float x, float y, float w, float h, UnityAction action)
        {
            var button = Button(parent, text, x, y, w, h, action);
            button.image.sprite = TransferButton;
            button.image.type = UnityEngine.UI.Image.Type.Sliced;
            button.image.pixelsPerUnitMultiplier = 8;
            button.image.color = Color.white;
            var colors = button.colors;
            colors.highlightedColor = new Color(1, 1, 0.82f);
            colors.pressedColor = new Color(0.8f, 0.72f, 0.5f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f);
            button.colors = colors;
            return button;
        }
        internal static ScrollRect ScrollArea(Transform parent, string name, float x, float y, float w, float h, out RectTransform content)
        {
            var root = NativeUi.Rect(parent, name, x, y, w + 24, h);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 32;
            var viewport = NativeUi.Rect(root, "Viewport", 0, 0, w, h);
            NativeUi.Image(viewport, null, Color.clear);
            viewport.gameObject.AddComponent<RectMask2D>(); scroll.viewport = viewport;
            content = NativeUi.Rect(viewport, "Content", 0, 0, w, h); scroll.content = content;
            var track = NativeUi.Rect(root, "Scrollbar", w + 8, 0, 12, h);
            NativeUi.Image(track, NativeUi.TransferCard, Color.white);
            var handle = NativeUi.Rect(track, "Handle", 0, 0, 0, 0);
            handle.anchorMin = Vector2.zero; handle.anchorMax = Vector2.one; handle.offsetMin = handle.offsetMax = Vector2.zero;
            var handleImage = NativeUi.Image(handle, NativeUi.TransferPanel, Color.white); handleImage.pixelsPerUnitMultiplier = 4;
            var bar = track.gameObject.AddComponent<Scrollbar>();
            bar.handleRect = handle; bar.targetGraphic = handleImage; bar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return scroll;
        }
        internal static Toggle Toggle(Transform parent, string title, float x, float y, float w, float h, bool value, UnityAction<bool> changed)
        {
            var rect = Rect(parent, title, x, y, w, h);
            var control = rect.gameObject.AddComponent<Toggle>();
            var hit = Image(rect, null, Color.clear); control.targetGraphic = hit;
            Label(rect, title, 0, 0, w - h - 20, h, 30);
            var icon = Rect(rect, "Checkbox", w - h, 5, h - 10, h - 10);
            icon.anchorMin = icon.anchorMax = new Vector2(1, 1); icon.pivot = new Vector2(1, 1); icon.anchoredPosition = new Vector2(-5, -5);
            var background = Image(icon, CheckOff, Color.white);
            var check = Image(Rect(icon, "Check", 0, 0, h - 10, h - 10), CheckOn, Color.white);
            control.graphic = check; control.SetIsOnWithoutNotify(value);
            control.onValueChanged.AddListener(changed);
            return control;
        }
    }
    public sealed class RuleTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        internal string Text;
        private GameObject tip;
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tip == null)
            {
                var rect = NativeUi.Rect(transform, "Rule tooltip", 0, -112, 690, 100);
                NativeUi.Image(rect, NativeUi.Panel, new Color(0.2f, 0.2f, 0.18f));
                NativeUi.Label(rect, Text, 18, 8, 654, 84, 23);
                foreach (var graphic in rect.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
                tip = rect.gameObject;
            }
            tip.SetActive(true);
        }
        public void OnPointerExit(PointerEventData eventData) { if (tip != null) tip.SetActive(false); }
        private void OnDisable() { if (tip != null) tip.SetActive(false); }
    }
}
