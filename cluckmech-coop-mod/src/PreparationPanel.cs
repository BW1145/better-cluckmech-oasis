using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using 函数库;

namespace CluckmechCoop
{
    public sealed class PreparationPanel : MonoBehaviour
    {
        private static readonly HashSet<PreparationPanel> panels = new HashSet<PreparationPanel>();
        private 战斗准备背包 preparation;
        private GameObject row;
        private Toggle toggle;
        private Text reward, status, wealth;
        private RectTransform selection, challenge;
        private float shift;
        private bool shifted;
        internal static void Attach(战斗背包 bag)
        {
            var root = bag.GetComponentInParent<战斗准备背包>();
            if (root == null || root.是否事件) return;
            NativeUi.Capture(root.transform);
            var panel = root.GetComponent<PreparationPanel>() ?? root.gameObject.AddComponent<PreparationPanel>();
            panel.preparation = root; panels.Add(panel); panel.Refresh();
        }
        internal static void RefreshAll() { foreach (var panel in panels) if (panel != null) panel.Refresh(); }
        internal static void RefreshWealth(){foreach(var panel in panels)if(panel!=null)panel.WealthWarning();}
        internal static void ShowStatus(string text) { foreach (var panel in panels) if (panel.status != null) panel.status.text = text; }
        private void OnEnable() { if (preparation != null) { ModuleSettings.Prepare(); Refresh(); } }
        private void OnDestroy() => panels.Remove(this);
        private void Refresh()
        {
            bool show=ModuleSettings.Expedition && !ModuleSettings.FinalLevel;
            if(show && row==null)Build();
            if(row!=null)row.SetActive(show);
            if(challenge!=null && shifted!=show)
            {
                float delta=show?shift:-shift;
                challenge.anchoredPosition+=new Vector2(0,delta);
                if(selection!=null)selection.anchoredPosition+=new Vector2(0,delta);
                shifted=show;
            }
            EnsureStatus();
            WealthWarning();
            status.text=Plugin.Network.SettingsReady?"":Plugin.Network.Status;
            if(!show)return;
            toggle.SetIsOnWithoutNotify(ModuleSettings.Choice?.Enabled == true);
            toggle.interactable = Plugin.Role != 2 && ModuleSettings.Choice?.Locked != true;
            reward.text = ModuleSettings.RewardText;
        }
        private void EnsureStatus()
        {
            if(status!=null)return;
            status=NativeUi.Label(transform,"",0,0,650,34,22);
            var rect=status.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,1);
            rect.anchoredPosition=new Vector2(0,-12);status.alignment=TextAnchor.MiddleCenter;
        }
        private void WealthWarning()
        {
            if(wealth==null)
            {
                wealth=NativeUi.Label(transform,"",0,0,1040,38,24);
                var rect=wealth.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-52);
                wealth.alignment=TextAnchor.MiddleCenter;wealth.color=new Color(1,.7f,.15f);
            }
            int rich=WealthEncounter.RichPlayers;
            wealth.text=rich>0?"财不露白：持有超过2000金币，本场额外出现"+(12*rich)+"只强敌":"";
        }
        private void Build()
        {
            challenge = preparation.挑战UI?.GetComponent<RectTransform>();
            if (challenge == null) return;
            selection = transform.Find("背包内容") as RectTransform;
            var rect = NativeUi.Rect(transform, "强化远征", 0, 0, 0, 0);
            var left = challenge.Find("底图") as RectTransform;
            var right = challenge.Find("图标") as RectTransform;
            if (left == null || right == null) { Destroy(rect.gameObject); return; }
            var span = challenge.anchorMax - challenge.anchorMin;
            rect.anchorMin = challenge.anchorMin + Vector2.Scale(span, new Vector2(left.anchorMin.x, Mathf.Min(left.anchorMin.y, right.anchorMin.y)));
            rect.anchorMax = challenge.anchorMin + Vector2.Scale(span, new Vector2(right.anchorMax.x, Mathf.Max(left.anchorMax.y, right.anchorMax.y)));
            rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = Vector2.zero;
            NativeUi.Image(rect, NativeUi.Frame, new Color(0.25f, 0.31f, 0.18f));
            row = rect.gameObject;
            shift = rect.rect.height + 8;
            toggle = NativeUi.Toggle(rect, "强化远征", 20, 12, rect.rect.width - 40, 60, false, value => { ModuleSettings.Select(value); Refresh(); });
            var toggleRect = toggle.GetComponent<RectTransform>();
            toggleRect.anchorMax = new Vector2(1, 1); toggleRect.pivot = new Vector2(0.5f, 1); toggleRect.sizeDelta = new Vector2(-40, 60); toggleRect.anchoredPosition = new Vector2(0, -12);
            reward = NativeUi.Label(rect, "", 0, 12, 0, 60, 26);
            reward.rectTransform.anchorMin = new Vector2(0.3f, 1); reward.rectTransform.anchorMax = new Vector2(0.9f, 1); reward.rectTransform.sizeDelta = new Vector2(0, 60);

            row.AddComponent<RuleTooltip>().Text = "原版敌人全部出完并消灭后，再出现一次；血量×2，攻击力×2。";
        }
    }
    [HarmonyPatch(typeof(UI_设置界面), "Start")]
    internal static class MainMenuModules
    {
        private static void Postfix(UI_设置界面 __instance)
        {
            if (__instance.gameObject.scene.buildIndex != 常数类.场景id_开始菜单) return;
            var root = __instance.transform;
            var categories = root.Find("选择区"); var content = root.Find("内容区");
            var nativeTab = categories?.Find("音频设置")?.GetComponent<Toggle>();
            if (nativeTab == null || content == null || root.Find("选择区/模组") != null) return;
            NativeUi.Capture(root);
            var page = NativeUi.Rect(content, "模组", 0, 0, 0, 0);
            page.anchorMin = Vector2.zero; page.anchorMax = Vector2.one; page.offsetMin = page.offsetMax = Vector2.zero;
            page.gameObject.AddComponent<ModulePanel>();
            page.gameObject.SetActive(false);
            var tabRect = NativeUi.Rect(categories, "模组", 0, 0, 0, 0);
            NativeUi.CopyRect(tabRect, nativeTab.GetComponent<RectTransform>());
            tabRect.anchoredPosition += new Vector2(0, -110);
            var label = NativeUi.Label(tabRect, "模组", 0, 0, tabRect.rect.width > 0 ? tabRect.rect.width : 240, 70, 36);
            var originalLabel = nativeTab.GetComponentInChildren<Text>();
            if (originalLabel != null) { NativeUi.CopyRect(label.rectTransform, originalLabel.rectTransform); label.fontSize = originalLabel.fontSize; label.alignment = originalLabel.alignment; }
            var tab = tabRect.gameObject.AddComponent<Toggle>();
            tab.targetGraphic = NativeUi.Image(tabRect, null, Color.clear);
            tab.group = nativeTab.group;
            tab.SetIsOnWithoutNotify(false);
            tab.onValueChanged.AddListener(value =>
            {
                page.gameObject.SetActive(value); label.color = value ? new Color(1, 0.7f, 0.1f) : Color.white;
                if (value)
                {
                    foreach (var other in categories.GetComponentsInChildren<Toggle>(true)) if (other != tab) other.isOn = false;
                    foreach (Transform child in content) if (child != page) child.gameObject.SetActive(false);
                }
            });
            foreach (var other in categories.GetComponentsInChildren<Toggle>(true))
                if (other != tab) other.onValueChanged.AddListener(value => { if (value) { tab.SetIsOnWithoutNotify(false); page.gameObject.SetActive(false); label.color = Color.white; } });
        }
    }
}
