using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CluckmechCoop
{
    public sealed class CombatStatusHud : MonoBehaviour
    {
        private Text label;
        private Graphic[] graphics;
        private RectTransform worldBar;
        private Canvas canvas;
        private Camera worldCamera;
        private readonly Vector3[] corners = new Vector3[4];
        private bool energy, wasVisible = true;
        private int previousCount = -1, previousSecond = -1;
        private float previousSpeed = -1;
        private static readonly Color Amber = new Color(1, .8f, .22f);

        private static RectTransform Frame(Text text)
        {
            for (var parent = text.transform.parent; parent != null && parent.GetComponent<Canvas>() == null; parent = parent.parent)
                if (parent.GetComponent<Image>() != null) return (RectTransform)parent;
            return text.rectTransform;
        }
        internal static void AttachEnergy(模块_基本属性 stats)
        {
            var display = stats.GetComponent<模块_生命显示>();
            var native = Object.FindObjectOfType<模块_金币和基地血量UI>();
            if (display == null || display.生命UI底 == null || native == null) return;
            var bar = display.生命UI底.rectTransform;
            var screen = native.GetComponentInParent<Canvas>().rootCanvas;
            string name = "后备能源状态_" + stats.GetInstanceID();
            if (screen.transform.Find(name) != null) return;
            var hud = Create(Frame(native.金币), screen.transform, name, true);
            hud.worldBar = bar; hud.canvas = screen; hud.worldCamera = Camera.main;
            var rect = (RectTransform)hud.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.pivot = new Vector2(.5f, 1);
        }
        internal static void AttachSpeed(Text style)
        {
            if (style == null) return;
            var reference = Frame(style);
            if (reference.parent.Find("倍速状态") != null) return;
            var hud = Create(reference, reference.parent, "倍速状态", false);
            var rect = (RectTransform)hud.transform;
            rect.anchorMin = rect.anchorMax = reference.anchorMin; rect.pivot = new Vector2(1, 1);
            rect.position = reference.TransformPoint(new Vector3(reference.rect.xMax, reference.rect.yMin - 5, 0));
        }
        private static CombatStatusHud Create(RectTransform reference, Transform parent, string name, bool isEnergy)
        {
            NativeUi.Capture(reference);
            float scale = reference.lossyScale.x / parent.lossyScale.x;
            float size = isEnergy ? 12 : Mathf.Max(14, Mathf.Round(reference.rect.height * .38f * scale));
            float width = isEnergy ? Mathf.Max(114, reference.rect.width * .9f * scale) : reference.rect.width * .72f * scale;
            var rect = NativeUi.Rect(parent, name, 0, 0, width, size * 1.65f);
            var image = NativeUi.Image(rect, NativeUi.Card, Color.white); image.raycastTarget = false;
            var hud = rect.gameObject.AddComponent<CombatStatusHud>(); hud.energy = isEnergy;
            hud.label = NativeUi.Label(rect, "", 8, 0, width - 16, rect.rect.height, (int)size);
            hud.label.alignment = TextAnchor.MiddleCenter;
            hud.label.horizontalOverflow = HorizontalWrapMode.Overflow;
            hud.label.verticalOverflow = VerticalWrapMode.Overflow;
            hud.graphics = rect.GetComponentsInChildren<Graphic>();
            return hud;
        }
        private void LateUpdate()
        {
            if (energy)
            {
                if (worldBar == null) { Destroy(gameObject); return; }
                worldBar.GetWorldCorners(corners);
                var bottom = worldCamera.WorldToScreenPoint((corners[0] + corners[3]) * .5f);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform.parent, bottom,
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out var point))
                    ((RectTransform)transform).anchoredPosition = point + new Vector2(0, -4);
            }
            bool visible = Plugin.InCombat && ModuleSettings.InRun && (energy ? ModuleSettings.Extra.Energy : ModuleSettings.Extra.Speed);
            if (visible != wasVisible)
            {
                foreach (var graphic in graphics) graphic.enabled = visible;
                wasVisible = visible;
            }
            if (!visible) return;
            if (energy)
            {
                int count = ModuleSettings.Progress.EnergyRemaining;
                int seconds = Mathf.CeilToInt(Plugin.Combat.RemainingEnergy);
                if (count == previousCount && seconds == previousSecond) return;
                previousCount = count; previousSecond = seconds;
                label.text = seconds > 0 ? "[F] ×" + count + "  " + seconds + "秒" : "[F] 能源×" + count;
                label.color = seconds > 0 ? Amber : count > 0 ? Color.white : new Color(.55f, .55f, .55f);
            }
            else
            {
                float speed = Time.timeScale;
                if (speed <= .2f || speed == previousSpeed) return;
                previousSpeed = speed;
                label.text = speed.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "× [Shift]";
                label.color = speed > 1.01f ? Amber : Color.white;
            }
        }
    }
    [HarmonyPatch(typeof(模块_波次倒计时UI), "Start")]
    internal static class SpeedHudReady
    {
        private static void Postfix(模块_波次倒计时UI __instance) => CombatStatusHud.AttachSpeed(__instance.当前波次);
    }
}
