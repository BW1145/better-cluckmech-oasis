using System;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class ModuleSettings
    {
        private static readonly ModulePreferences preferences = new ModulePreferences();
        private static readonly ModuleRunState run = new ModuleRunState();
        private static readonly ModuleSaveStore saves = new ModuleSaveStore();
        private static 数据库_关卡奖励 rewards;
        private static 奖励列表脚本 rewardList;

        internal static GameplayModules Extra
        {
            get => run.Extra;
            set => run.SetExtra(value);
        }
        internal static RunProgress Progress
        {
            get => run.Progress;
            set => run.SetProgress(value);
        }
        internal static ExpeditionChoice Choice
        {
            get => run.Choice;
            set => run.SetChoice(value);
        }
        internal static bool InRun
        {
            get => run.InRun;
            set => run.SetInRun(value);
        }
        internal static bool Expedition
        {
            get => run.Expedition;
            set => run.SetExpedition(value);
        }
        internal static bool Transfer
        {
            get => run.Transfer;
            set => run.SetTransfer(value);
        }
        internal static bool Music
        {
            get => run.Music;
            set => run.SetMusic(value);
        }
        internal static PlayerProgress LocalProgress => run.Progress.Player(Plugin.Role);
        internal static bool Preference(ModuleId id) => preferences.Get(id);
        internal static void Preference(ModuleId id, bool value) => preferences.Set(id, value);
        internal static bool ExpeditionPreference
        {
            get => preferences.Get(ModuleId.Expedition);
            set => preferences.Set(ModuleId.Expedition, value);
        }
        internal static bool TransferPreference
        {
            get => preferences.Get(ModuleId.Transfer);
            set => preferences.Set(ModuleId.Transfer, value);
        }

        internal static void Initialize(ConfigFile config)
        {
            preferences.Bind(config);
            NativeRepairs.Enabled = preferences.Get(ModuleId.NativeRepairs);
        }
        internal static void EnterRun()
        {
            if (run.Enter(preferences, Plugin.Role) && Plugin.Role != 2)
                Plugin.Network.PublishSettings();
        }
        internal static void MainMenu()
        {
            OtherworldProgress.Clear();
            KamuiWorld.NewRun();
            run.Reset();
            ClearScene();
            CarryCapacity.Apply();
        }
        internal static void ClearScene()
        {
            rewards = null;
            rewardList = null;
        }
        internal static void ApplyHostSettings(bool expedition, bool transfer, ExpeditionChoice choice,
            GameplayModules modules, RunProgress progress)
        {
            EnterRun();
            run.ApplyHostSettings(expedition, transfer, choice, modules, progress);
        }
        internal static void SetPlayerProgress(int role, PlayerProgress progress) => run.SetPlayerProgress(role, progress);
        internal static bool Active => InRun && Expedition && !FinalLevel && Choice != null && Choice.Enabled;
        internal static bool FinalLevel => Plugin.Spawner?.数据?.是否最终关卡 == true || Plugin.Summary?.地图存档?.当前场景名 == "绿洲";
        internal static string NodeKey
        {
            get
            {
                var map = Plugin.Summary?.地图存档;
                return map == null ? null : map.当前地图页 + ":" + map.当前节点名;
            }
        }
        internal static void Prepare()
        {
            EnterRun();
            if (!Expedition || Plugin.Role == 2 || NodeKey == null) return;
            string node = NodeKey;
            if (Choice != null && (Choice.Node == node || Choice.Node?.StartsWith(node + ":", StringComparison.Ordinal) == true))
            {
                Choice.Node = node;
                return;
            }
            Choice = new ExpeditionChoice { Node = node, Token = Guid.NewGuid().ToString("N"), RewardId = FinalLevel ? -1 : UnityEngine.Random.Range(0, 3) };
            Plugin.Summary.保存单局存档();
            Plugin.Network.PublishSettings();
        }
        internal static void Select(bool enabled)
        {
            if (Plugin.Role == 2 || Choice == null || Choice.Locked) return;
            Choice.Enabled = enabled;
            Plugin.Summary?.保存单局存档();
            Plugin.Network.PublishSettings();
        }
        internal static void Freeze()
        {
            Prepare();
            if (Choice != null) Choice.Locked = true;
        }
        internal static void BindRewards()
        {
            if (!Active) return;
            rewardList = UnityEngine.Object.FindObjectOfType<奖励列表脚本>();
            rewards = UnityEngine.Object.FindObjectOfType<数据库_关卡奖励>();
        }
        internal static void CompleteReward()
        {
            if (!Active || FinalLevel || !Choice.TryReward()) return;
            if (rewards == null || rewardList == null) { Choice.RewardApplied = false; Plugin.Log("Reward list missing at completion"); return; }
            rewardList.添加奖励数据(rewards.查询关卡奖励(Choice.RewardId));
        }
        internal static string RewardText => Choice == null || Choice.RewardId < 0 ? "" :
            Choice.RewardId == 0 ? "额外奖励：50金币" : Choice.RewardId == 1 ? "额外奖励：1动力" : "额外奖励：1台鸡械";
        internal static void SaveProgress()
        {
            if (Plugin.Role == 2) Plugin.Network.SendProgress(LocalProgress);
            else Plugin.Summary?.保存单局存档();
        }
        internal static void Save(存档脚本 save) => saves.Save(save, run, Plugin.Role);
        internal static void Load(存档脚本 save)
        {
            if (!saves.TryLoad(save, Plugin.Role, out var snapshot)) return;
            run.Restore(snapshot.Progress, snapshot.Choice);
            KamuiWorld.Load(snapshot.Characters);
            EnterRun();
        }
    }
    [HarmonyPatch(typeof(存档脚本), "保存")]
    internal static class SaveExpedition
    {
        private static bool Prefix(存档脚本 __instance)
        {
            if (!RunSaveRepair.CanSave(__instance)) return false;
            ModuleSettings.Save(__instance); return true;
        }
    }
    [HarmonyPatch(typeof(存档脚本), "读取")]
    internal static class LoadExpedition { private static void Postfix(存档脚本 __instance) => ModuleSettings.Load(__instance); }
}
