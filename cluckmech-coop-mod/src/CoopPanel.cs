using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CluckmechCoop
{
    public sealed class CoopPanel : MonoBehaviour
    {
        private GameObject canvasRoot, dialog, launcher;
        private RectTransform inventory, panelRect, launcherRect, upgradeContent;
        private Transform cursorLayer;
        private float uiScale = 1;
        private Text balance, status, empty, selectedName, selectedUpgrades;
        private Image selectedIcon;
        private InputField amount;
        private Button giveMachine;
        private bool open, dirty = true, previousUiFocus;
        private MachineCard selected;
        private cz词缀库 upgradeLibrary;
        private readonly List<Button> controls = new List<Button>();
        private readonly List<MachineCard> cards = new List<MachineCard>();
        private readonly List<Text> upgradeLabels = new List<Text>();
        private readonly Dictionary<int, 模块_基本属性> attributes = new Dictionary<int, 模块_基本属性>();
        private readonly Dictionary<int, string> upgradeNames = new Dictionary<int, string>();
        private sealed class MachineCard
        {
            internal Button Button;
            internal Image Icon, Selection;
            internal Text Name, Upgrades;
            internal 总结脚本.背包单元 Item;
            internal Machine Snapshot;
        }
        internal void Rebuild()
        {
            CloseImmediately(); dirty = true; attributes.Clear(); upgradeNames.Clear(); upgradeLibrary = null;
            if (launcher != null) launcher.SetActive(false);
        }
        private void OnDestroy() { if (canvasRoot != null) Destroy(canvasRoot); }
        internal void RefreshWallet()
        {
            MapBoonsUi.RefreshCurrent();WealthEncounter.RefreshWarnings();
            if (open && balance != null) balance.text = (Plugin.Summary?.当前金钱数 ?? 0).ToString();
        }
        internal void RefreshState()
        {
            if (Plugin.InCombat || !ModuleSettings.Transfer)
            {
                if (launcher != null) launcher.SetActive(false);
                CloseImmediately(); return;
            }
            bool available = Plugin.CanTrade;
            if (canvasRoot == null && available && NativeUi.Canvas != null) Build();
            if (canvasRoot == null) return;
            bool pending = Plugin.Transfers.Locked;
            if (pending && !open) Open();
            launcher.SetActive(available && !open);
            if (!open) return;
            if (!available && !pending) { CloseImmediately(); return; }
            RefreshWallet(); status.text = Plugin.Transfers.Status;
            foreach (var button in controls) button.interactable = !pending;
            foreach (var card in cards) card.Button.interactable = !pending;
            giveMachine.interactable = !pending && Plugin.Transfers.PeerReady && selected != null;
            amount.interactable = !pending;
        }
        internal void Open()
        {
            if (!Plugin.CanTrade || NativeUi.Canvas == null) return;
            if (canvasRoot == null) Build();
            if (!open)
            {
                previousUiFocus = Plugin.Summary.是否光标位于UI界面;
                dirty = true;
                canvasRoot.transform.SetAsLastSibling();
                if (cursorLayer != null) cursorLayer.SetAsLastSibling();
            }
            open = true; dialog.SetActive(true);
            Plugin.Summary.是否光标位于UI界面 = true;
            UpdateCards(); RefreshState();
        }
        private void Close() { if (!Plugin.Transfers.Locked) CloseImmediately(); }
        private void CloseImmediately()
        {
            if (open && Plugin.Summary != null) Plugin.Summary.是否光标位于UI界面 = previousUiFocus;
            open = false; ClearSelection();
            if (dialog != null) dialog.SetActive(false);
            if (launcher != null) launcher.SetActive(Plugin.CanTrade);
        }
        private static Image Surface(Transform parent, string name, float x, float y, float w, float h, Sprite sprite)
        {
            var image = NativeUi.Image(NativeUi.Rect(parent, name, x, y, w, h), sprite, Color.white);
            image.raycastTarget = false;
            return image;
        }
        private static void Texture(Transform parent, float x, float y, float w, float h)
        {
            var image = Surface(parent, "Texture", x, y, w, h, NativeUi.TransferTexture);
            image.type = Image.Type.Tiled; image.pixelsPerUnitMultiplier = 1; image.color = new Color(1, 1, 1, 0.035f);
        }
        private static ScrollRect ScrollArea(Transform parent, string name, float x, float y, float w, float h, out RectTransform content) => NativeUi.ScrollArea(parent, name, x, y, w, h, out content);
        private void Build()
        {
            cards.Clear(); controls.Clear(); upgradeLabels.Clear(); selected = null; dirty = true;
            var cursor = UnityEngine.Object.FindObjectOfType<光标管理器>();
            var cursorCanvas = cursor != null ? cursor.GetComponentInParent<Canvas>() : null;
            var parent = (cursorCanvas != null ? cursorCanvas.rootCanvas : NativeUi.Canvas.rootCanvas).transform;
            var root = NativeUi.Rect(parent, "队友转交界面", 0, 0, 0, 0);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
            canvasRoot = root.gameObject;
            root.gameObject.AddComponent<TransferUiLayout>();
            cursorLayer = cursor != null ? cursor.transform : null;
            while (cursorLayer != null && cursorLayer.parent != parent) cursorLayer = cursorLayer.parent;
            if (cursorLayer != null) cursorLayer.SetAsLastSibling();
            var launch = NativeUi.TransferAction(root, "队友转交", 0, 0, 190, 54, Open);
            launcher = launch.gameObject;
            launcherRect = launch.GetComponent<RectTransform>();
            launcherRect.anchorMin = launcherRect.anchorMax = launcherRect.pivot = new Vector2(1, 0);
            launcherRect.anchoredPosition = new Vector2(-24, 24);
            var shade = NativeUi.Rect(root, "Transfer", 0, 0, 0, 0);
            shade.anchorMin = Vector2.zero; shade.anchorMax = Vector2.one; shade.offsetMin = shade.offsetMax = Vector2.zero;
            NativeUi.Image(shade, null, new Color(0, 0, 0, 0.55f)); dialog = shade.gameObject;
            panelRect = NativeUi.Rect(shade, "Panel", 0, 0, 1060, 700);
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            var frame = NativeUi.Image(panelRect, NativeUi.TransferPanel, Color.white); frame.pixelsPerUnitMultiplier = 1;
            Texture(panelRect, 24, 24, 1012, 652);
            NativeUi.Label(panelRect, "队友转交", 30, 20, 700, 48, 32);
            controls.Add(NativeUi.TransferAction(panelRect, "关闭", 930, 24, 100, 40, Close));
            Surface(panelRect, "Gold tray", 24, 82, 1012, 66, NativeUi.TransferCard);
            var coin = Surface(panelRect, "Gold icon", 42, 96, 36, 36, NativeUi.GoldIcon); coin.preserveAspect = true;
            balance = NativeUi.Label(panelRect, "", 92, 94, 310, 42, 28);
            var input = NativeUi.Rect(panelRect, "金额", 470, 92, 240, 46);
            var inputImage = NativeUi.Image(input, NativeUi.TransferCard, Color.white);
            amount = input.gameObject.AddComponent<InputField>(); amount.targetGraphic = inputImage;
            amount.textComponent = NativeUi.Label(input, "", 14, 0, 212, 46, 24);
            amount.contentType = InputField.ContentType.IntegerNumber; amount.text = "100";
            controls.Add(NativeUi.TransferAction(panelRect, "转交金币", 730, 92, 288, 46, () =>
            {
                if (int.TryParse(amount.text, out int value)) Plugin.Transfers.Request("gold", value, -1);
                else Plugin.Transfers.Status = "请输入金币金额";
                RefreshState();
            }));
            NativeUi.Label(panelRect, "鸡械", 28, 148, 500, 30, 23);
            NativeUi.Label(panelRect, "已选鸡械", 718, 148, 300, 30, 23);
            Surface(panelRect, "Inventory frame", 24, 180, 672, 432, NativeUi.TransferCard);
            ScrollArea(panelRect, "Inventory scroll", 32, 188, 632, 416, out inventory);
            empty = NativeUi.Label(inventory.parent, "背包暂无鸡械", 16, 16, 560, 40, 24);
            Surface(panelRect, "Selection frame", 708, 180, 328, 432, NativeUi.TransferCard);
            Texture(panelRect, 716, 188, 312, 416);
            selectedIcon = Surface(panelRect, "Selected icon", 824, 194, 96, 96, null); selectedIcon.preserveAspect = true;
            selectedName = NativeUi.Label(panelRect, "", 724, 302, 296, 58, 26); selectedName.alignment = TextAnchor.MiddleCenter;
            selectedUpgrades = NativeUi.Label(panelRect, "", 724, 368, 296, 30, 22);
            var divider = Surface(panelRect, "Divider", 724, 408, 296, 2, null); divider.color = new Color(0.5f, 0.5f, 0.46f);
            ScrollArea(panelRect, "Upgrade scroll", 724, 422, 272, 174, out upgradeContent);
            status = NativeUi.Label(panelRect, "", 30, 634, 660, 40, 22);
            giveMachine = NativeUi.TransferAction(panelRect, "交给队友", 708, 630, 328, 46, ConfirmMachine);
            ClearSelection(); dialog.SetActive(false);
            var settings = UnityEngine.Object.FindObjectOfType<设置管理器>();
            ApplyScale(settings != null ? settings.UI界面大小 : 1);
        }
        internal void ApplyScale(float value) { uiScale = value; Resize(); }
        internal void Resize()
        {
            if (panelRect == null || canvasRoot == null) return;
            var bounds = canvasRoot.GetComponent<RectTransform>().rect;
            float scale = Mathf.Min(bounds.height / 1080 * uiScale, (bounds.width - 40) / 1060, (bounds.height - 40) / 700);
            panelRect.localScale = launcherRect.localScale = Vector3.one * scale;
        }
        internal void RefreshInventory() { dirty = true; ClearSelection(); if (open) UpdateCards(); }
        private void UpdateCards()
        {
            if (!dirty || inventory == null || Plugin.Summary == null) return;
            dirty = false;
            var machines = Plugin.Summary.当前拥有炮台;
            inventory.sizeDelta = new Vector2(632, Mathf.Max(416, ((machines.Count + 3) / 4) * 140 + 8));
            empty.gameObject.SetActive(machines.Count == 0);
            for (int i = 0; i < machines.Count; i++)
            {
                if (i == cards.Count) cards.Add(CreateCard(i));
                var card = cards[i]; card.Button.gameObject.SetActive(true);
                var machine = card.Item = machines[i];
                if (Matches(machine, card.Snapshot)) continue;
                if (!attributes.TryGetValue(machine.炮台id, out var attribute))
                {
                    attribute = Plugin.Units?.获取单位(machine.炮台id)?.GetComponent<模块_基本属性>(); attributes[machine.炮台id] = attribute;
                }
                card.Name.text = attribute != null ? attribute.名称() : "鸡械 " + machine.炮台id;
                card.Icon.sprite = attribute?.图标; card.Icon.enabled = card.Icon.sprite != null;
                card.Upgrades.text = machine.强化词条.Count + "/" + machine.强化上限;
                card.Upgrades.color = machine.强化词条.Count > 0 ? new Color(1, 0.82f, 0.33f) : new Color(0.72f, 0.72f, 0.68f);
                card.Snapshot = TransferService.Snapshot(machine);
            }
            for (int i = machines.Count; i < cards.Count; i++) cards[i].Button.gameObject.SetActive(false);
        }
        private static bool Matches(总结脚本.背包单元 item, Machine snapshot)
        {
            return Rules.SameMachine(item.炮台id, item.强化上限, item.强化词条, snapshot);
        }
        private void Select(MachineCard card)
        {
            if (!Plugin.Summary.当前拥有炮台.Contains(card.Item) || !Matches(card.Item, card.Snapshot))
            {
                Plugin.Transfers.Status = "背包已变化，请重新选择鸡械"; RefreshInventory(); RefreshState(); return;
            }
            if (selected != null) selected.Selection.enabled = false;
            selected = card; card.Selection.enabled = true;
            Plugin.Transfers.Status = "";
            selectedIcon.sprite = card.Icon.sprite; selectedIcon.enabled = card.Icon.enabled;
            selectedName.text = card.Name.text; selectedUpgrades.text = "强化 " + card.Upgrades.text;
            if (upgradeLibrary == null && card.Snapshot.Upgrades.Count > 0)
                upgradeLibrary = Plugin.Units.GetComponent<cz词缀库>();
            for (int i = 0; i < card.Snapshot.Upgrades.Count; i++)
            {
                if (i == upgradeLabels.Count) upgradeLabels.Add(NativeUi.Label(upgradeContent, "", 0, i * 30, 272, 30, 20));
                int id = card.Snapshot.Upgrades[i];
                if (!upgradeNames.TryGetValue(id, out var name))
                { name = upgradeLibrary != null ? upgradeLibrary.获取词缀名称(id) : id.ToString(); upgradeNames[id] = name; }
                upgradeLabels[i].text = name; upgradeLabels[i].gameObject.SetActive(true);
            }
            for (int i = card.Snapshot.Upgrades.Count; i < upgradeLabels.Count; i++) upgradeLabels[i].gameObject.SetActive(false);
            upgradeContent.sizeDelta = new Vector2(272, Mathf.Max(174, card.Snapshot.Upgrades.Count * 30));
            upgradeContent.anchoredPosition = Vector2.zero;
            RefreshState();
        }
        private void ClearSelection()
        {
            if (selected != null && selected.Selection != null) selected.Selection.enabled = false;
            selected = null;
            if (selectedIcon == null) return;
            selectedIcon.enabled = false; selectedName.text = ""; selectedUpgrades.text = "";
            foreach (var label in upgradeLabels) label.gameObject.SetActive(false);
            giveMachine.interactable = false;
        }
        private void ConfirmMachine()
        {
            if (selected == null) return;
            int index = Plugin.Summary.当前拥有炮台.IndexOf(selected.Item);
            Plugin.Transfers.Request("machine", 0, index, selected.Snapshot);
            RefreshState();
        }
        private MachineCard CreateCard(int index)
        {
            var card = new MachineCard();
            var buttonRect = NativeUi.Rect(inventory, "Machine card", 8 + index % 4 * 154, 4 + index / 4 * 140, 146, 132);
            var background = NativeUi.Image(buttonRect, NativeUi.TransferPanel, Color.white); background.pixelsPerUnitMultiplier = 2;
            card.Button = buttonRect.gameObject.AddComponent<Button>(); card.Button.targetGraphic = background;
            card.Button.onClick.AddListener(() => Select(card));
            var colors = card.Button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1, 0.86f, 0.55f);
            colors.pressedColor = new Color(0.75f, 0.65f, 0.45f); card.Button.colors = colors;
            var rect = card.Button.transform;
            Surface(rect, "Card inset", 6, 6, 134, 120, NativeUi.TransferCard);
            Texture(rect, 8, 8, 130, 116);
            card.Icon = Surface(rect, "Icon", 41, 8, 64, 64, null); card.Icon.preserveAspect = true;
            card.Name = NativeUi.Label(rect, "", 8, 72, 130, 26, 20); card.Name.alignment = TextAnchor.MiddleCenter;
            card.Upgrades = NativeUi.Label(rect, "", 8, 100, 130, 20, 16); card.Upgrades.alignment = TextAnchor.MiddleCenter;
            card.Selection = Surface(rect, "Selected corners", 0, 0, 146, 132, NativeUi.Selection); card.Selection.enabled = false;
            return card;
        }
    }
    public sealed class TransferUiLayout : MonoBehaviour
    {
        private void OnRectTransformDimensionsChange() => Plugin.Panel?.Resize();
    }
    [HarmonyPatch(typeof(设置管理器), "改变UI界面大小")]
    internal static class TransferUiScale
    {
        private static void Postfix(设置管理器 __instance) => Plugin.Panel.ApplyScale(__instance.UI界面大小);
    }
}
