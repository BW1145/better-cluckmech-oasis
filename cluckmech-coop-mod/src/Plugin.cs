using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using 函数库;

namespace CluckmechCoop
{
    [BepInPlugin("billy.cluckmech.coop", "更好的鸡械绿洲", Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        internal const string Version = "0.6.11";
        internal static Plugin Instance;
        internal static TransferService Transfers;
        internal static NetworkService Network;
        internal static CoopPanel Panel;
        internal static CombatFeatures Combat;
        internal static MusicExpansion Music;
        internal static 刷怪器 Spawner;
        internal static bool InCombat;
        private static 总结脚本 summary;
        private static dw单位库 units;
        private Harmony patches;
        private void Awake()
        {
            Instance = this;
            ModuleSettings.Initialize(Config);
            Transfers = new TransferService(); Network = new NetworkService();
            Panel = gameObject.AddComponent<CoopPanel>();
            Combat = gameObject.AddComponent<CombatFeatures>();
            Music = gameObject.AddComponent<MusicExpansion>();
            patches = new Harmony("billy.cluckmech.coop");
            patches.PatchAll(typeof(Plugin).Assembly);
            Steam联机管理器.事件_联机消息广播_string.AddListener(Network.Receive);
            Steam联机管理器.事件_联机消息广播_string.AddListener(Transfers.Receive);
            Steam联机管理器.事件_玩家控件创建.AddListener(Network.PlayerCreated);
            Steam联机管理器.事件_玩家离开.AddListener(Network.Disconnect);
            SceneManager.sceneLoaded += Loaded;
            SceneLifecycle.Enter(SceneManager.GetActiveScene().handle);
            StartCoroutine(CaptureScene(SceneLifecycle.Current));
            Log($"更好的鸡械绿洲 {Version} loaded; {patches.GetPatchedMethods().Count()} patches");
        }
        private void Loaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.buildIndex == 常数类.场景id_选择英雄) RunSaveRepair.Blocked = false;
            if (!SceneLifecycle.Enter(scene.handle)) return;
            Spawner = null; units = null; InCombat = false;
            Panel.Rebuild();
            if (scene.buildIndex == 常数类.场景id_开始菜单) { Network.Disconnect(); ModuleSettings.MainMenu(); }
            else ModuleSettings.EnterRun();
            StartCoroutine(CaptureScene(SceneLifecycle.Current));
        }
        private IEnumerator CaptureScene(SceneLifetime scene)
        {
            yield return null;
            if (!SceneLifecycle.IsCurrent(scene)) yield break;
            summary = UnityEngine.Object.FindObjectOfType<总结脚本>();
            Spawner = UnityEngine.Object.FindObjectOfType<刷怪器>();
            if (RunSaveRepair.AtMainMenu) Spawner = null;
            InCombat = Spawner != null && Spawner.关卡状态 != 刷怪器.关卡状态枚举.放置等待;
            if (ModuleSettings.Transfer && Role != 0)
            {
                var canvas = UnityEngine.Object.FindObjectOfType<UnityEngine.Canvas>();
                if (canvas != null) NativeUi.Capture(canvas.transform);
            }
            Network.BeginHandshake();
            if (Spawner != null && OtherworldRegistry.Enabled) OtherworldWorld.Begin();
            CarryCapacity.Apply();Panel.RefreshState(); GrowthPanel.ShowPending(); MapBoonsUi.Attach();
        }
        internal static void BindSpawner(刷怪器 spawner)
        {
            if (RunSaveRepair.AtMainMenu) return;
            Spawner = spawner;
            ModuleSettings.Freeze(); ModuleSettings.BindRewards(); GrowthFeatures.Inspect(spawner);
            if (ModuleSettings.Expedition) Difficulty.BindEvents(spawner);
            Panel.RefreshState();
        }
        internal static void CombatStarted(刷怪器 spawner)
        {
            if (RunSaveRepair.AtMainMenu) return;
            if (SceneLifecycle.Current == null) SceneLifecycle.Enter(SceneManager.GetActiveScene().handle);
            if (!SceneLifecycle.Current.BeginCombat()) return;
            Spawner = spawner; InCombat = true; Combat.Begin(spawner); CharacterBoons.Begin(); WealthEncounter.Begin(spawner);
            StratagemWorld.Begin();
            if (OtherworldRegistry.Enabled) { OtherworldWorld.Begin(); KamuiWorld.Restore(); }
            SwallowTargets.Begin();
            Panel.RefreshState();
        }
        internal static void BindSummary(总结脚本 value) => summary = value;
        private void OnDestroy()
        {
            Steam联机管理器.事件_联机消息广播_string.RemoveListener(Network.Receive);
            Steam联机管理器.事件_联机消息广播_string.RemoveListener(Transfers.Receive);
            Steam联机管理器.事件_玩家控件创建.RemoveListener(Network.PlayerCreated);
            Steam联机管理器.事件_玩家离开.RemoveListener(Network.Disconnect);
            SceneManager.sceneLoaded -= Loaded;
            SceneLifecycle.Leave();
            Network?.Shutdown();
            patches?.UnpatchSelf();
            NativeUi.ReleaseTransferSkin();
            CarryCapacity.Release();
            OtherworldAssets.Release();
        }
        internal static 总结脚本 Summary => summary;
        internal static dw单位库 Units { get { if (units == null) units = UnityEngine.Object.FindObjectOfType<dw单位库>(); return units; } }
        internal static int Role => Steam联机管理器.当前联机状态 == Steam联机管理器.联机状态.主机端 ? 1 :
            Steam联机管理器.当前联机状态 == Steam联机管理器.联机状态.客户端 ? 2 : 0;
        internal static 联机玩家控件 LocalPlayer => Steam联机管理器.玩家_本地 != null ? Steam联机管理器.玩家_本地 :
            Role == 1 ? Steam联机管理器.玩家_主机 : Steam联机管理器.玩家_客户端;
        internal static bool Challenge => ModuleSettings.Active;
        internal static bool CanTrade => ModuleSettings.InRun && ModuleSettings.Transfer && !InCombat && Role != 0 && summary != null &&
            summary.是否拥有地图存档 && (Spawner == null || Spawner.关卡状态 == 刷怪器.关卡状态枚举.放置等待);
        internal static void Warn(string message) => Instance.Logger.LogWarning(message);
        internal static void Log(string message) => Instance.Logger.LogInfo(message);
    }
    [HarmonyPatch(typeof(总结脚本), "Start")]
    internal static class SummaryReady { private static void Postfix(总结脚本 __instance) => Plugin.BindSummary(__instance); }
    [HarmonyPatch(typeof(刷怪器), "Start")]
    internal static class SpawnerReady
    {
        private static void Prefix(刷怪器 __instance) { if (!RunSaveRepair.AtMainMenu) { Plugin.Spawner = __instance; ModuleSettings.Freeze(); } }
        private static void Postfix(刷怪器 __instance) => Plugin.BindSpawner(__instance);
    }
    [HarmonyPatch(typeof(刷怪器), "重置并开始计时", new Type[0])]
    internal static class CombatReady { private static void Postfix(刷怪器 __instance) => Plugin.CombatStarted(__instance); }
    [HarmonyPatch(typeof(战斗背包), "OnEnable")]
    internal static class PreparationReady
    {
        private static void Postfix(战斗背包 __instance)
        {
            if (__instance.总结 != null) Plugin.BindSummary(__instance.总结);
            ModuleSettings.Prepare();
            CarryCapacity.Prepare(__instance);
            PreparationPanel.Attach(__instance);
            Plugin.Panel.RefreshState();
        }
    }
    [HarmonyPatch(typeof(战斗准备背包), "是否携带检测")]
    internal static class ExpeditionStart
    {
        private static bool Prefix()
        {
            if (Plugin.Role == 0) { ModuleSettings.Freeze(); return true; }
            if (!Plugin.Network.SettingsReady) { PreparationPanel.ShowStatus(Plugin.Network.Status); return false; }
            ModuleSettings.Freeze(); return true;
        }
    }
    [HarmonyPatch(typeof(总结脚本), "获得金钱")]
    internal static class WalletChanged { private static void Postfix() { Plugin.Panel.RefreshWallet(); ShopFeatures.WalletChanged(); } }
    [HarmonyPatch]
    internal static class InventoryChanged
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (string name in new[] { "获得炮台", "删除炮台", "获得强化词条", "背包排序" }) yield return AccessTools.Method(typeof(总结脚本), name);
        }
        private static void Postfix() { Plugin.Panel.RefreshInventory(); GrowthPanel.InventoryChanged(); MapBoonsUi.RefreshCurrent(); }
    }
}
