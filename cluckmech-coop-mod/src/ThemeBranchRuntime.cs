using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using 函数库;

namespace CluckmechCoop
{
    internal static class ThemeBranchRuntime
    {
        internal static ThemeBranchState State => ModuleSettings.Progress.Branch;
        internal static bool InBranch => State.Active;
        internal static bool Enabled => ModuleSettings.Extra.ThemeBranches || State.Active;
        private static 地图管理器.地图存档类 Map => Plugin.Summary?.地图存档;
        internal static int CurrentStep => State.Active && Map != null ? ThemeBranchCatalog.Step(State.Theme, Map.当前节点名) : -1;

        internal static void BindSummary()
        {
            var summary = UnityEngine.Object.FindObjectOfType<总结脚本>();
            if (summary != null) Plugin.BindSummary(summary);
            ModuleSettings.EnterRun();
        }

        internal static bool Begin(模块_事件场景控制 controller)
        {
            if (Plugin.Role == 2 || !ModuleSettings.Extra.ThemeBranches || State.Used || Map == null) return false;
            string entrance = ModuleSettings.NodeKey;
            controller.信息保存?.退出节点场景();
            var main = ThemeMapCodec.Clone(Map);
            main.待跳转场景id = 常数类.场景id_选择地图;
            main.是否位于事件 = false;
            bool[] variants = { UnityEngine.Random.value < .3f, UnityEngine.Random.value < .3f, UnityEngine.Random.value < .3f };
            if (!State.Begin(ThemeMapCodec.Encode(main), Guid.NewGuid().ToString("N"),
                UnityEngine.Random.Range(1, 3), UnityEngine.Random.Range(0, 2), variants)) return false;
            State.EntranceNode = entrance; State.EntranceChoice = 0;
            var branch = ThemeMapCodec.Clone(main);
            branch.当前地图页 = ThemeBranchCatalog.Page(State.Theme);
            branch.当前节点名 = ThemeBranchCatalog.Node(State.Theme, 0);
            branch.走过的节点.Clear(); branch.走过的节点.Add(branch.当前节点名);
            branch.节点作用列表.Clear(); branch.是否初始化地图 = true;
            branch.是否开启挑战 = false; branch.是否开启无尽 = false; branch.是否位于无尽 = false;
            branch.当前波次 = 0;
            Plugin.Summary.地图存档 = branch;
            Changed();
            return true;
        }

        internal static void DeclineEntrance(string node)
        {
            if (Plugin.Role == 2 || State.EntranceNode == node && State.EntranceChoice >= 0) return;
            State.EntranceNode = node; State.EntranceChoice = 1;
            Changed();
        }

        internal static void SelectEvent(int choice)
        {
            if (Plugin.Role == 2 || !State.SelectEvent(choice)) return;
            GrantLocalEvent();
            Changed();
        }

        internal static void GrantLocalEvent()
        {
            if (!State.Used || State.EventChoice != 2 || Plugin.Summary == null ||
                ModuleSettings.LocalProgress.ThemeRewardToken == State.Token) return;
            ModuleSettings.LocalProgress.ThemeRewardToken = State.Token;
            Plugin.Summary.获得金钱(300);
            if (Plugin.Role != 0) Plugin.LocalPlayer?.上传玩家总结数据();
            ModuleSettings.SaveProgress();
        }

        internal static void Capture()
        {
            if (Plugin.Role != 2 && State.Active && Map != null && ThemeBranchCatalog.IsPage(Map.当前地图页))
                State.ActiveMap = ThemeMapCodec.Encode(Map);
        }

        internal static void Restore()
        {
            if (!State.Active || Plugin.Summary == null) return;
            if (!ThemeBranchCatalog.IsPage(Map.当前地图页)) Plugin.Summary.地图存档 = ThemeMapCodec.Decode(State.ActiveMap);
        }

        internal static void SettingsReceived()
        {
            if (Plugin.Role == 2 && Map != null && State.Used)
            {
                if (State.Active && !ThemeBranchCatalog.IsPage(Map.当前地图页))
                    Plugin.Summary.地图存档 = ThemeMapCodec.Decode(State.ActiveMap);
                else if (!State.Active && ThemeBranchCatalog.IsPage(Map.当前地图页))
                    Plugin.Summary.地图存档 = ThemeMapCodec.Decode(State.MainMap);
            }
            GrantLocalEvent();
            ThemeBranchEvents.RefreshState();
        }

        internal static void PrepareMap(地图管理器 manager)
        {
            if (Plugin.Role != 2 && State.Active && State.Completed == 5) Return(false);
            if (Plugin.Role != 2 && State.Active && Map != null && ThemeBranchCatalog.IsPage(Map.当前地图页))
            {
                // Reopening a cancelled preparation returns to the last completed node.
                int step = ThemeBranchCatalog.Step(State.Theme, Map.当前节点名);
                if (step > State.Completed)
                {
                    Map.当前节点名 = ThemeBranchCatalog.Node(State.Theme, State.Completed);
                    Map.走过的节点.RemoveAll(name => ThemeBranchCatalog.Step(State.Theme, name) > State.Completed);
                }
            }
            ThemeBranchMaps.Prepare(manager);
        }

        internal static bool HandleNode(地图页 page, string node)
        {
            if (!ThemeBranchCatalog.IsPage(page.地图页名)) return true;
            if (node == ThemeBranchCatalog.ExitNode || ThemeBranchCatalog.Step(State.Theme, node) == 6)
            {
                Return(true);
                return false;
            }
            if (!State.Active) return false;
            int step = ThemeBranchCatalog.Step(State.Theme, node);
            if (step != State.Completed + 1) return false;
            if (step == 2)
            {
                Map.是否位于事件 = false;
                Map.事件id = State.Theme == 1 ? ThemeBranchCatalog.CastleEvent : ThemeBranchCatalog.DeepSeaEvent;
            }
            return true;
        }

        internal static void RewardsExited()
        {
            if (Plugin.Role == 2 || !State.Active) return;
            int step = CurrentStep;
            if ((step == 1 || step == 3 || step == 5) && State.Complete(step)) Changed();
        }

        internal static void ServiceExited()
        {
            if (State.Active && CurrentStep == 4 && State.Complete(4) && Plugin.Role != 2) Changed();
        }

        internal static void EventExited(模块_事件场景控制 controller)
        {
            if (!State.Active || CurrentStep != 2 || State.EventChoice < 0 ||
                controller.随机数 != (State.Theme == 1 ? ThemeBranchCatalog.CastleEvent : ThemeBranchCatalog.DeepSeaEvent)) return;
            if (State.Complete(2) && Plugin.Role != 2) Changed();
        }

        private static void Return(bool changeScene)
        {
            if (!State.Active) return;
            var main = ThemeMapCodec.Decode(State.MainMap);
            State.Return();
            main.是否初始化地图 = false;
            main.待跳转场景id = 常数类.场景id_选择地图;
            Plugin.Summary.地图存档 = main;
            Changed();
            if (changeScene) 场景函数.切换选择地图();
        }

        private static void Changed()
        {
            Capture();
            if (Plugin.Role != 2)
            {
                Plugin.Summary?.保存单局存档();
                Plugin.Network.PublishSettings();
            }
            ThemeBranchEvents.RefreshState();
        }
    }

    [HarmonyPatch(typeof(地图管理器), "Start")]
    internal static class ThemeBranchMapInstall
    {
        private static void Prefix(地图管理器 __instance)
        {
            ThemeBranchRuntime.BindSummary();
            ThemeBranchMaps.Install(__instance);
        }
    }

    [HarmonyPatch]
    internal static class ThemeBranchMapPrepare
    {
        private static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(地图管理器), "地图初始化");
            yield return AccessTools.Method(typeof(地图管理器), "地图加载");
        }
        private static void Prefix(地图管理器 __instance) => ThemeBranchRuntime.PrepareMap(__instance);
    }

    [HarmonyPatch(typeof(地图页), "运算地图页内节点的场景")]
    internal static class ThemeBranchNodeData
    {
        private static void Postfix(地图页 __instance) => ThemeBranchMaps.ApplyNodeData(__instance);
    }

    [HarmonyPatch(typeof(地图页), "触发节点作用")]
    internal static class ThemeBranchNodeAction
    {
        private static bool Prefix(地图页 __instance, string __0) => ThemeBranchRuntime.HandleNode(__instance, __0);
    }

    [HarmonyPatch(typeof(节点场景信息保存), "退出节点场景")]
    internal static class ThemeBranchServiceExit
    {
        private static void Postfix() => ThemeBranchRuntime.ServiceExited();
    }

    [HarmonyPatch(typeof(模块_事件场景控制), "退出事件场景")]
    internal static class ThemeBranchEventExit
    {
        private static void Prefix(模块_事件场景控制 __instance) => ThemeBranchRuntime.EventExited(__instance);
    }

    [HarmonyPatch(typeof(场景函数), "切换场景", new[] { typeof(int) })]
    internal static class ThemeBranchRewardExit
    {
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(int __0, bool __runOriginal)
        {
            if (__runOriginal && __0 == 常数类.场景id_选择地图 &&
                SceneManager.GetActiveScene().buildIndex == 常数类.场景id_选择奖励)
                ThemeBranchRuntime.RewardsExited();
        }
    }

    [HarmonyPatch(typeof(联机玩家控件), "地图同步给客户端")]
    internal static class ThemeBranchMapSync
    {
        private static void Prefix()
        {
            if (Plugin.Role != 1 || !ThemeBranchRuntime.Enabled) return;
            ThemeBranchRuntime.Capture();
            Plugin.Network.PublishSettings();
        }
    }

    [HarmonyPatch(typeof(战斗准备背包), "最终Boss关卡检测")]
    internal static class ThemeBranchPreparation
    {
        private static bool Prefix(战斗准备背包 __instance)
        {
            if (!ThemeBranchRuntime.InBranch) return true;
            __instance.无尽UI.SetActive(false);
            __instance.挑战UI.SetActive(true);
            return false;
        }
    }
}
