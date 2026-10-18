using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CluckmechCoop
{
    public sealed class ModulePanel : MonoBehaviour
    {
        private RectTransform board;
        private Text title, details;
        private readonly List<Image> selections = new List<Image>();
        private readonly List<Toggle> toggles = new List<Toggle>();
        private void OnEnable()
        {
            if (board == null) Build();
            for (int i = 0; i < toggles.Count; i++) toggles[i].SetIsOnWithoutNotify(ModuleSettings.Preference(ModuleCatalog.All[i].Id));
            Resize();
        }
        private void Build()
        {
            board = NativeUi.Rect(transform, "Module settings", 0, 0, 1460, 900);
            board.anchorMin = board.anchorMax = board.pivot = new Vector2(.5f, .5f); board.anchoredPosition = Vector2.zero;
            NativeUi.Label(board, "模组", 16, 8, 900, 56, 38).color = new Color(1, .72f, .16f);
            NativeUi.Label(board, "玩法设置在开始或继续游戏时应用；启动项下次启动生效", 16, 70, 1050, 40, 24);
            var left = NativeUi.Rect(board, "Modules", 8, 134, 780, 748);
            var frame = NativeUi.Image(left, NativeUi.TransferPanel, Color.white); frame.pixelsPerUnitMultiplier = 2;
            NativeUi.ScrollArea(left, "Module list", 14, 14, 728, 720, out var content);
            content.sizeDelta = new Vector2(728, ModuleCatalog.All.Length * 126);
            for (int i = 0; i < ModuleCatalog.All.Length; i++)
            {
                int index = i; var info = ModuleCatalog.All[i];
                var card = NativeUi.Rect(content, info.Name, 0, i * 126, 728, 114);
                var image = NativeUi.Image(card, NativeUi.TransferCard, Color.white);
                var texture = NativeUi.Image(NativeUi.Rect(card, "Grain", 6, 6, 716, 102), NativeUi.TransferTexture, new Color(1, 1, 1, .035f));
                texture.type = Image.Type.Tiled; texture.raycastTarget = false;
                NativeUi.Label(card, info.Name, 20, 9, 602, 42, 28);
                NativeUi.Label(card, info.Description, 20, 58, 670, 40, 23).color = new Color(.8f, .8f, .74f);
                var pick = card.gameObject.AddComponent<Button>(); pick.targetGraphic = image; pick.onClick.AddListener(() => Select(index));
                var selected = NativeUi.Image(NativeUi.Rect(card, "Selected", 0, 0, 728, 114), NativeUi.Selection, Color.white);
                selected.raycastTarget = false; selections.Add(selected);
                var toggle = NativeUi.Toggle(card, "", 652, 9, 60, 50, ModuleSettings.Preference(info.Id), value => { ModuleSettings.Preference(info.Id, value); Select(index); });
                toggles.Add(toggle);
            }
            var right = NativeUi.Rect(board, "Module details", 812, 134, 638, 748);
            NativeUi.Image(right, NativeUi.TransferPanel, Color.white).pixelsPerUnitMultiplier = 2;
            title = NativeUi.Label(right, "", 28, 28, 582, 92, 32); title.color = new Color(1, .72f, .16f);
            NativeUi.Image(NativeUi.Rect(right, "Divider", 28, 133, 582, 2), null, new Color(.5f, .5f, .46f)).raycastTarget = false;
            details = NativeUi.Label(right, "", 28, 160, 582, 554, 27); details.alignment = TextAnchor.UpperLeft; details.lineSpacing = 1.2f;
            Select(0);
        }
        private void Select(int index)
        {
            for (int i = 0; i < selections.Count; i++) selections[i].enabled = i == index;
            var info = ModuleCatalog.All[index]; title.text = info.Name; details.text = info.Details;
        }
        private void Resize()
        {
            if (board == null) return;
            var bounds = ((RectTransform)transform).rect;
            board.localScale = Vector3.one * Mathf.Min((bounds.width - 24) / 1460, (bounds.height - 24) / 900);
        }
        private void OnRectTransformDimensionsChange() => Resize();
    }
}
