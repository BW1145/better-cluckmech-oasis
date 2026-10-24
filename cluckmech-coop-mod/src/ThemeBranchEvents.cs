using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CluckmechCoop
{
    internal static class ThemeBranchEvents
    {
        private static ThemeBranchEventPanel current;
        internal static void Bind(ThemeBranchEventPanel panel) => current = panel;
        internal static void Release(ThemeBranchEventPanel panel) { if (current == panel) current = null; }
        internal static void RefreshState() { if (current != null) current.Refresh(); }

        internal static void Install(模块_事件场景控制 controller)
        {
            ThemeBranchRuntime.BindSummary();
            if (!ThemeBranchRuntime.Enabled && Plugin.Role != 2) return;
            var original = controller.全事件[0];
            if (controller.全事件.Length <= ThemeBranchCatalog.DeepSeaEvent)
                Array.Resize(ref controller.全事件, ThemeBranchCatalog.DeepSeaEvent + 1);
            if (controller.全事件布局.Length <= ThemeBranchCatalog.DeepSeaEvent)
                Array.Resize(ref controller.全事件布局, ThemeBranchCatalog.DeepSeaEvent + 1);
            for (int id = ThemeBranchCatalog.TreeEvent; id <= ThemeBranchCatalog.DeepSeaEvent; id++)
            {
                if (controller.全事件[id]?.GetComponent<ThemeBranchEventPanel>()?.EventId == id) continue;
                var root = UnityEngine.Object.Instantiate(original, original.transform.parent);
                root.SetActive(false);
                root.name = id == ThemeBranchCatalog.TreeEvent ? "树洞" : id == ThemeBranchCatalog.CastleEvent ? "管家的待办清单" : "值守员的最后一班";
                foreach (var script in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (script.GetType().Namespace?.StartsWith("UnityEngine.Localization", StringComparison.Ordinal) == true)
                        UnityEngine.Object.DestroyImmediate(script);
                root.AddComponent<ThemeBranchEventPanel>().Build(id, controller);
                controller.全事件[id] = root;
                int layout = id == ThemeBranchCatalog.DeepSeaEvent ? 70 : 10;
                var background = UnityEngine.Object.Instantiate(controller.全事件布局[layout], controller.全事件布局[layout].transform.parent);
                background.SetActive(false); background.name = root.name;
                controller.全事件布局[id] = background;
            }
            var state = ThemeBranchRuntime.State;
            if (!ModuleSettings.Extra.ThemeBranches || state.PoolsAdded || Plugin.Role == 2) return;
            var pools = Plugin.Summary.GetComponent<模块_事件总结>();
            pools.火山随机 = Add(pools.火山随机, ThemeBranchCatalog.TreeEvent);
            pools.研究所随机 = Add(pools.研究所随机, ThemeBranchCatalog.TreeEvent);
            state.PoolsAdded = true;
        }

        private static int[] Add(int[] source, int id)
        {
            var result = new List<int>(source);
            if (!result.Contains(id)) result.Add(id);
            return result.ToArray();
        }

        internal static bool SelectFixed(模块_事件场景控制 controller)
        {
            int id;
            var map = Plugin.Summary?.地图存档;
            if (ThemeBranchRuntime.InBranch && ThemeBranchRuntime.CurrentStep == 2)
                id = ThemeBranchRuntime.State.Theme == 1 ? ThemeBranchCatalog.CastleEvent : ThemeBranchCatalog.DeepSeaEvent;
            else if (ModuleSettings.Extra.ThemeBranches && !ThemeBranchRuntime.State.Used &&
                map?.当前地图页 == ThemeBranchCatalog.FourthPage && map.当前节点名 == ThemeBranchCatalog.FourthPage + ":tree")
                id = ThemeBranchCatalog.TreeEvent;
            else return true;
            controller.随机数 = id;
            controller.全事件[id].SetActive(true);
            controller.全事件布局[id].SetActive(true);
            return false;
        }
    }

    public sealed class ThemeBranchEventPanel : MonoBehaviour
    {
        internal int EventId { get; private set; }
        private 模块_事件场景控制 controller;
        private GameObject upper, lower, result;
        private Text story, resultText;
        private string entrance;

        internal void Build(int id, 模块_事件场景控制 owner)
        {
            EventId = id; controller = owner;
            upper = transform.Find("上框").gameObject;
            lower = transform.Find("下框").gameObject;
            story = transform.Find("上框/内容").GetComponent<Text>();
            transform.Find("上框/标题").GetComponent<Text>().text = gameObject.name;
            result = transform.Find("选择反馈").gameObject;
            resultText = result.transform.Find("上框 /内容").GetComponent<Text>();
            foreach (string name in new[] { "选择反馈 (1)", "选择反馈 (2)" })
                UnityEngine.Object.DestroyImmediate(transform.Find(name).gameObject);
            var buttons = lower.transform.Find("按钮区域");
            int count = id == ThemeBranchCatalog.TreeEvent ? 2 : 3;
            string[] labels = Labels(id);
            for (int choice = 0; choice < 3; choice++)
            {
                var button = buttons.Find(choice == 0 ? "按钮选项" : "按钮选项 (" + choice + ")").GetComponent<Button>();
                if (choice >= count) { UnityEngine.Object.DestroyImmediate(button.gameObject); continue; }
                int selected = choice;
                Bind(button, id * 100 + choice, labels[choice], () => Resolve(selected), true);
            }
            Bind(result.transform.Find("下框/按钮区域/按钮选项").GetComponent<Button>(), id * 100 + 9,
                "继续", controller.退出事件场景, false);
            upper.SetActive(true); lower.SetActive(true); result.SetActive(false);
        }

        private static void Bind(Button button, int value, string label, UnityAction action, bool hostOnly)
        {
            var decision = button.GetComponent<联机_决策选择>();
            decision.提交决策值_int = value;
            decision.事件_决策选择时 = new UnityEvent();
            decision.事件_决策实现 = new UnityEvent();
            decision.事件_决策实现_仅主机端 = new UnityEvent();
            (hostOnly ? decision.事件_决策实现_仅主机端 : decision.事件_决策实现).AddListener(action);
            button.onClick = new Button.ButtonClickedEvent(); button.onClick.AddListener(decision.决策选择);
            button.GetComponentInChildren<Text>(true).text = label;
        }

        private void OnEnable()
        {
            if (upper == null) return;
            entrance = ModuleSettings.NodeKey;
            ThemeBranchEvents.Bind(this);
            Refresh();
        }
        private void OnDisable() => ThemeBranchEvents.Release(this);

        private void Resolve(int choice)
        {
            if (EventId == ThemeBranchCatalog.TreeEvent)
            {
                if (ThemeBranchRuntime.State.EntranceNode == entrance && ThemeBranchRuntime.State.EntranceChoice >= 0) return;
                if (choice == 0) ThemeBranchRuntime.Begin(controller);
                else ThemeBranchRuntime.DeclineEntrance(entrance);
            }
            else ThemeBranchRuntime.SelectEvent(choice);
            Refresh();
        }

        internal void Refresh()
        {
            if (upper == null) return;
            var state = ThemeBranchRuntime.State;
            string text = null;
            if (EventId == ThemeBranchCatalog.TreeEvent)
            {
                story.text = "枯树的裂口里透出一线微光。里面的道路向下延伸，远处隐约可见石砌拱门，也像是浸在幽蓝水光中的长廊。\n\n进去看看，或沿着原来的路继续前进。";
                if (state.EntranceNode == entrance && state.EntranceChoice >= 0)
                    text = state.EntranceChoice == 0 ? "树洞后的道路逐渐清晰。\n\n前方是<color=#FFD45A>" + ThemeBranchCatalog.Name(state.Theme) + "</color>。" : "你在裂口旁留下记号，沿原路线继续前进。";
            }
            else
            {
                bool castle = EventId == ThemeBranchCatalog.CastleEvent;
                string boss = state.Used ? ThemeBranchCatalog.BattleName(state.Theme, 2, state.Eldritch[2]) : "";
                story.text = castle ? "南瓜管家将一串钥匙、一盒铁钩和一只钱匣摆在你面前。\n“酒窖可以封，厅柱也能修——您若是来收账，礼金就在这里。”\n\n管家合上账本：“开宴前，只够办一件。”" :
                    "值守室的控制台还亮着，屏幕显示着深处的巨兽。备用电池下面压着一张纸条：“剩下的电只够启动一台设备。”\n旁边的回收机显示：备用电池，收购价300金币。";
                story.text += "\n\n本层Boss：<color=#FFD45A>" + boss + "</color>";
                UpdateLabels();
                if (state.EventChoice >= 0)
                {
                    ThemeBranchRuntime.GrantLocalEvent();
                    string[] outcomes = castle ? new[] { "管家锁上东酒窖，将钥匙收回袖口。", "四根厅柱都装好了捕藤钩。", "管家打开钱匣，将礼金交给你。" } :
                        new[] { "干扰器的指示灯依次亮起，程序开始运行。", "舒缓脉冲沿管线传向深处，仪表上的暴怒增长逐渐放缓。", "回收机收走电池，将金币交给你。" };
                    text = outcomes[state.EventChoice] + "\n\n<color=#00FF02>" + Effects(EventId)[state.EventChoice] + "</color>";
                }
            }
            bool resolved = text != null;
            upper.SetActive(!resolved); lower.SetActive(!resolved); result.SetActive(resolved);
            if (resolved) resultText.text = text;
        }

        private void UpdateLabels()
        {
            var labels = Labels(EventId);
            var area = lower.transform.Find("按钮区域");
            for (int i = 0; i < 3; i++)
                area.Find(i == 0 ? "按钮选项" : "按钮选项 (" + i + ")").GetComponentInChildren<Text>(true).text = labels[i];
        }

        private static string[] Labels(int id)
        {
            if (id == ThemeBranchCatalog.TreeEvent) return new[] {
                "<b>进入树洞</b>\n<size=23><color=#00FF02>探索一层随机主题支线，之后返回原路线</color></size>",
                "<b>继续前进</b>\n<size=23>沿原路线前进</size>" };
            string[] names = id == ThemeBranchCatalog.CastleEvent ? new[] { "封住东酒窖", "安装捕藤钩", "领取宴会礼金" } :
                new[] { "启动干扰器", "延缓暴怒", "出售电池" };
            var effects = Effects(id);
            for (int i = 0; i < names.Length; i++) names[i] = "<b>" + names[i] + "</b>\n<size=23><color=#00FF02>" + effects[i] + "</color></size>";
            return names;
        }

        private static string[] Effects(int id)
        {
            bool altered = ThemeBranchRuntime.State.Eldritch[2];
            if (id == ThemeBranchCatalog.CastleEvent) return new[] {
                "每10秒的侍酒蝠4只→2只，从西侧出发；东侧改出2名重甲骑士",
                altered ? "双结击破后，伯爵坠地时间6秒→9秒" : "四根厅柱的撞柱僵直3秒→6秒", "每位玩家获得300金币" };
            return new[] { altered ? "巨手会更慢地逼近基地，为你争取防守时间" : "触手攻击间隔延长50%",
                "Boss暴怒条积累速度降低40%", "每位玩家获得300金币" };
        }
    }

    [HarmonyPatch(typeof(模块_事件场景控制), "Start")]
    internal static class InstallThemeBranchEvents
    {
        private static void Prefix(模块_事件场景控制 __instance) => ThemeBranchEvents.Install(__instance);
    }

    [HarmonyPatch(typeof(模块_事件场景控制), "随机场景控制")]
    internal static class ThemeBranchFixedEvent
    {
        private static bool Prefix(模块_事件场景控制 __instance) => ThemeBranchEvents.SelectFixed(__instance);
    }
}
