using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using 函数库;

namespace CluckmechCoop
{
    internal static class RunSaveRepair
    {
        private static readonly MethodInfo cache = AccessTools.Method(typeof(存档脚本), "存档载入内存");
        internal static bool Blocked;
        internal static bool IsRunSlot(存档脚本 save) => save.文件名 == "danju" || save.文件名 == "lianji";
        internal static bool AtMainMenu => SceneManager.GetActiveScene().buildIndex == 常数类.场景id_开始菜单;
        internal static bool IsTemplate(总结脚本 summary)
        {
            if (summary == null || summary.当前所选英雄id != 0 || summary.当前金钱数 != 0 ||
                summary.当前动力炉等级 != 0 || summary.当前基地血量 != 5000 ||
                summary.单局统计数据.角色初始天赋 != null || summary.当前拥有炮台.Count != 3) return false;
            int[] ids = { 1, 2, 0 };
            for (int i = 0; i < ids.Length; i++)
            {
                var item = summary.当前拥有炮台[i];
                if (item.炮台id != ids[i] || item.强化上限 != 0 || item.强化词条.Count != 0) return false;
            }
            return true;
        }
        internal static bool CanSave(存档脚本 save)
        {
            if (!IsRunSlot(save)) return true;
            var summary = GameObject.Find(常数类.命名_总结)?.GetComponent<总结脚本>();
            if (AtMainMenu || Blocked || IsTemplate(summary))
            {
                Plugin.Log("[Save] Run save skipped: menu preview or incomplete continuation");
                return false;
            }
            return true;
        }
        internal static void Restore(存档脚本 save)
        {
            Blocked = true;
            if (!(bool)cache.Invoke(save, null)) return;
            string path = save.文件名 + "_m" + save.后缀;
            var settings = new ES3Settings(ES3.Location.Cache)
            { encryptionType = ES3.EncryptionType.AES, encryptionPassword = 常数类.存档_存档加密密码 };
            Read(save.gameObject, typeof(存档脚本), path, settings);
            var root = GameObject.Find(常数类.命名_总结);
            if (root == null) return;
            bool restored = Read(root, typeof(总结脚本), path, settings);
            foreach (var item in save.物体组件列表)
            {
                var target = GameObject.Find(item.物体名称);
                if (target == null) continue;
                foreach (string name in item.脚本组件列表)
                {
                    var type = typeof(存档脚本).Assembly.GetType(name);
                    if (target != root || type != typeof(总结脚本)) Read(target, type, path, settings);
                }
            }
            foreach (var item in save.物体列表)
            {
                var target = GameObject.Find(item.物体名称);
                if (target == null) continue;
                foreach (string name in item.脚本组件列表)
                {
                    var type = typeof(存档脚本).Assembly.GetType(name);
                    if (target != root || type != typeof(总结脚本)) Read(target, type, path, settings);
                }
            }
            var summary = root.GetComponent<总结脚本>();
            if (!restored || IsTemplate(summary))
            {
                var menu = UnityEngine.Object.FindObjectOfType<开始菜单继续游戏>();
                if (menu != null)
                {
                    menu.按钮.interactable = false;
                    var label = menu.按钮.GetComponentInChildren<Text>();
                    if (label != null) label.text = "存档数据不完整";
                }
                Plugin.Log("[Save] Continuation stopped: run data contains the menu template or has no summary");
                return;
            }
            Plugin.BindSummary(summary);
            Blocked = false;
            Plugin.Log($"[Save] Restored {path}: hero {summary.当前所选英雄id}, gold {summary.当前金钱数}, machines {summary.当前拥有炮台.Count}");
        }
        private static bool Read(GameObject target, Type type, string path, ES3Settings settings)
        {
            if (type == null) return false;
            string key = target.name + type;
            if (!ES3.KeyExists(key, path, settings)) return false;
            var component = target.GetComponent(type) ?? target.AddComponent(type);
            ES3.LoadInto(key, path, (object)component, settings);
            return true;
        }
    }
    [HarmonyPatch(typeof(存档脚本), "读取")]
    internal static class RestoreRunSlot
    {
        private static bool Prefix(存档脚本 __instance)
        {
            if (Plugin.Role == 2 || !RunSaveRepair.IsRunSlot(__instance)) return true;
            RunSaveRepair.Restore(__instance);
            return false;
        }
    }
    [HarmonyPatch(typeof(场景切换按钮), "切换场景", new[] { typeof(int) })]
    internal static class ContinueRunGate
    {
        private static bool Prefix(int __0) => !RunSaveRepair.Blocked || !RunSaveRepair.AtMainMenu || __0 != 常数类.场景id_选择地图;
    }
}
