using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CluckmechCoop
{
    [HarmonyPatch(typeof(模块_主界面场景控制), "Start")]
    internal static class MenuTitle
    {
        private static void Postfix(模块_主界面场景控制 __instance)
        {
            var cover = __instance.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(x => x.name.Trim() == "上右盖");
            if (cover == null || cover.Find("BetterOasisTitle") != null) return;
            if (NativeUi.Font == null)
            {
                var fonts = __instance.GetComponentsInChildren<Text>(true).Select(x => x.font).Where(x => x != null).ToArray();
                NativeUi.Font = fonts.FirstOrDefault(x => x.name == "中文像素字体") ?? fonts.FirstOrDefault();
            }
            var root = NativeUi.Rect(cover, "BetterOasisTitle", 0, 0, 440, 280);
            root.anchorMin = root.anchorMax = new Vector2(1, .5f);
            root.pivot = new Vector2(0, .5f); root.anchoredPosition = new Vector2(-160, -108);
            var plaque = NativeUi.Image(root, NativeUi.MenuPlaque, Color.white); plaque.raycastTarget = false;
            var version = NativeUi.Label(root, "v" + Plugin.Version, 212, 236, 118, 30, 26);
            version.alignment = TextAnchor.MiddleCenter; version.color = new Color(1, .72f, .12f);
            var shadow = version.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(.16f, .12f, .08f); shadow.effectDistance = new Vector2(1, -1);
        }
    }
}
