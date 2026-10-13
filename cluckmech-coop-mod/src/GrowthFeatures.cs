using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using 函数库;
using UnityEngine.SceneManagement;

namespace CluckmechCoop
{
    internal static class GrowthFeatures
    {
        private static readonly HashSet<string> regionBosses = new HashSet<string> { "王_幽灵白桦树", "王_石像仙人掌", "王_酒精红头孢", "王_PlantDos", "王_水果海盗团", "王_玉米蜗牛", "王_矿洞盆栽" };
        internal static string Region;
        internal static void Inspect(刷怪器 spawner)
        {
            Region = null;
            if (!ModuleSettings.Extra.Growth || spawner.数据 == null || spawner.数据.是否最终关卡) return;
            if (spawner.数据.波次数据.SelectMany(x => x.组次数据).SelectMany(x => x.单位).Any(x => x != null && regionBosses.Contains(x.name)))
                Region = Plugin.Summary.地图存档.当前地图页位置 + ":" + Plugin.Summary.地图存档.当前地图页;
        }
        internal static void Offer(string region)
        {
            if (!ModuleSettings.Extra.Growth || !ModuleSettings.LocalProgress.OfferRegion(region)) return;
            ModuleSettings.SaveProgress(); GrowthPanel.ShowPending();
            Plugin.Log("[Growth] Region upgrade offered: " + region);
        }
        internal static void FinishRegion()
        {
            if (Region == null || Plugin.Role == 2) return;
            Offer(Region); Plugin.Network.Broadcast(new SessionMessage { Type = "growth", Token = Region });
        }
    }

    public sealed class GrowthPanel : MonoBehaviour
    {
        private static GrowthPanel active;
        private RectTransform panel, content;
        private sealed class GrowthCard
        {
            public Button Button;
            public Image Icon, Marker;
            public Text Name, Count;
            public 总结脚本.背包单元 Item;
            public Machine Snapshot;
        }
        private GrowthCard selectedCard;
        private Button confirm;
        private Text chosen;
        private 总结脚本.背包单元 selected;
        private Machine snapshot;
        private bool previousUi;
        private readonly List<GrowthCard> cards = new List<GrowthCard>();
        private readonly Dictionary<int, 模块_基本属性> attributes = new Dictionary<int, 模块_基本属性>();
        internal static void ShowPending()
        {
            if (SceneManager.GetActiveScene().buildIndex == 常数类.场景id_异步加载场景 || active != null || !ModuleSettings.Extra.Growth || ModuleSettings.LocalProgress.PendingRegion == null || Plugin.InCombat || Plugin.Summary == null || Plugin.Summary.当前拥有炮台.Count == 0) return;
            var cursor = Object.FindObjectOfType<光标管理器>();
            var canvas = cursor != null ? cursor.GetComponentInParent<Canvas>().rootCanvas : Object.FindObjectOfType<Canvas>()?.rootCanvas;
            if (canvas == null) return;
            NativeUi.Capture(canvas.transform);
            var root = NativeUi.Rect(canvas.transform, "强化成长", 0, 0, 0, 0);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
            active = root.gameObject.AddComponent<GrowthPanel>(); active.Build();
            if (cursor != null)
            {
                var layer = cursor.transform; while (layer.parent != canvas.transform && layer.parent != null) layer = layer.parent;
                layer.SetAsLastSibling();
            }
        }
        internal static void Close() { if (active != null) Object.Destroy(active.gameObject); active = null; }
        internal static void InventoryChanged() { if (active != null) active.Refresh(); else ShowPending(); }
        private void Build()
        {
            previousUi = Plugin.Summary.是否光标位于UI界面; Plugin.Summary.是否光标位于UI界面 = true;
            NativeUi.Image((RectTransform)transform, null, new Color(0, 0, 0, .6f));
            panel = NativeUi.Rect(transform, "Panel", 0, 0, 920, 690);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f); panel.anchoredPosition = Vector2.zero;
            NativeUi.Image(panel, NativeUi.TransferPanel, Color.white);
            NativeUi.Label(panel, "增加强化上限", 28, 22, 840, 48, 32);
            NativeUi.Label(panel, "选择一台鸡械，强化上限+1", 28, 78, 840, 40, 24);
            NativeUi.ScrollArea(panel, "Machines", 28, 142, 836, 418, out content);
            chosen = NativeUi.Label(panel, "", 28, 586, 514, 60, 25);
            confirm = NativeUi.TransferAction(panel, "确认", 592, 588, 290, 54, Confirm);
            Refresh(); Resize();
        }
        private void Refresh()
        {
            if (selectedCard != null) selectedCard.Marker.enabled = false;
            selectedCard = null; selected = null; snapshot = null; confirm.interactable = false; chosen.text = "";
            var bag = Plugin.Summary.当前拥有炮台;
            content.sizeDelta = new Vector2(836, Mathf.Max(418, ((bag.Count + 3) / 4) * 142));
            for (int i = 0; i < bag.Count; i++)
            {
                if (i == cards.Count) cards.Add(CreateCard(i));
                var card = cards[i]; card.Button.gameObject.SetActive(true); card.Item = bag[i];
                var item = card.Item;
                if (Rules.SameMachine(item.炮台id, item.强化上限, item.强化词条, card.Snapshot)) continue;
                card.Snapshot = new Machine { Id = item.炮台id, UpgradeLimit = item.强化上限, Upgrades = new List<int>(item.强化词条) };
                if (!attributes.TryGetValue(item.炮台id, out var stats)) attributes[item.炮台id] = stats = Plugin.Units.获取单位(item.炮台id).GetComponent<模块_基本属性>();
                card.Icon.sprite = stats.图标; card.Name.text = stats.名称(); card.Count.text = item.强化词条.Count + "/" + item.强化上限;
            }
            for (int i = bag.Count; i < cards.Count; i++) { cards[i].Item = null; cards[i].Button.gameObject.SetActive(false); }
        }
        private GrowthCard CreateCard(int index)
        {
            var rect = NativeUi.Rect(content, "Machine", index % 4 * 209, index / 4 * 142, 199, 132);
            var card = new GrowthCard();
            var image = NativeUi.Image(rect, NativeUi.TransferCard, Color.white);
            card.Icon = NativeUi.Image(NativeUi.Rect(rect, "Icon", 66, 10, 66, 58), null, Color.white); card.Icon.preserveAspect = true; card.Icon.raycastTarget = false;
            card.Name = NativeUi.Label(rect, "", 8, 74, 183, 30, 22); card.Name.alignment = TextAnchor.MiddleCenter;
            card.Count = NativeUi.Label(rect, "", 8, 106, 183, 22, 20); card.Count.alignment = TextAnchor.MiddleCenter;
            card.Marker = NativeUi.Image(NativeUi.Rect(rect, "Selection", 0, 0, 199, 132), NativeUi.Selection, Color.white); card.Marker.raycastTarget = false; card.Marker.enabled = false;
            card.Button = rect.gameObject.AddComponent<Button>(); card.Button.targetGraphic = image; card.Button.onClick.AddListener(() => Select(card));
            return card;
        }
        private void Select(GrowthCard card)
        {
            var item = card.Item;
            if (item == null || !Plugin.Summary.当前拥有炮台.Contains(item) || !Rules.SameMachine(item.炮台id, item.强化上限, item.强化词条, card.Snapshot)) { Refresh(); return; }
            if (selectedCard != null) selectedCard.Marker.enabled = false;
            selectedCard = card; card.Marker.enabled = true; selected = item; snapshot = card.Snapshot;
            chosen.text = attributes[item.炮台id].名称() + "  " + item.强化上限 + " → " + (item.强化上限 + 1); confirm.interactable = true;
        }
        private void Confirm()
        {
            if (selected == null || !Plugin.Summary.当前拥有炮台.Contains(selected) || !Rules.SameMachine(selected.炮台id, selected.强化上限, selected.强化词条, snapshot)) { Refresh(); return; }
            if (!ModuleSettings.LocalProgress.ClaimRegion()) return;
            selected.强化上限++;
            Plugin.Log("[Growth] Upgrade limit +1: machine " + selected.炮台id + ", new limit " + selected.强化上限);
            if (Plugin.Role != 0) Plugin.LocalPlayer?.上传玩家总结数据();
            ModuleSettings.SaveProgress(); Plugin.Summary.保存单局存档();
            foreach (var bag in Object.FindObjectsOfType<玩家背包脚本>()) bag.更新背包();
            Plugin.Panel.RefreshInventory(); Close();
        }
        private void Resize()
        {
            if (panel == null) return;
            var bounds = ((RectTransform)transform).rect;
            panel.localScale = Vector3.one * Mathf.Min(bounds.height / 1080, (bounds.width - 40) / 920, (bounds.height - 40) / 690);
        }
        private void OnRectTransformDimensionsChange() => Resize();
        private void OnDestroy() { if (Plugin.Summary != null) Plugin.Summary.是否光标位于UI界面 = previousUi; if (active == this) active = null; }
    }

    [HarmonyPatch(typeof(刷怪器), "结算界面切换")]
    internal static class RegionCompletion
    {
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(bool __runOriginal) { if (__runOriginal) GrowthFeatures.FinishRegion(); }
    }
    [HarmonyPatch(typeof(场景函数), "切换场景", new[] { typeof(int) })]
    internal static class GrowthExitGate
    {
        private static bool Prefix(int __0)
        {
            if (__0 != 常数类.场景id_选择地图 || !ModuleSettings.Extra.Growth || ModuleSettings.LocalProgress.PendingRegion == null || Plugin.Summary == null || Plugin.Summary.当前拥有炮台.Count == 0) return true;
            GrowthPanel.ShowPending(); return false;
        }
    }
}
