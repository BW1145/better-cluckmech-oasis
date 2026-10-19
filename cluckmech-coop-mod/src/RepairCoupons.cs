using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CluckmechCoop
{
    public sealed class RepairCoupons : MonoBehaviour
    {
        private string node;
        private bool bound;
        internal void Bind(string key)
        {
            node = key;
            if (bound) return;
            bound = true;
            var choices = new HashSet<联机_决策选择>();
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                var choice = button.GetComponent<联机_决策选择>();
                for (int i = 0; choice == null && i < button.onClick.GetPersistentEventCount(); i++)
                    choice = button.onClick.GetPersistentTarget(i) as 联机_决策选择;
                if (choice != null)
                {
                    if (choices.Add(choice)) choice.事件_决策实现.AddListener(Claim);
                }
                else
                {
                    bool reward=false;
                    for(int i=0;i<button.onClick.GetPersistentEventCount();i++)
                    {
                        string method=button.onClick.GetPersistentMethodName(i);
                        if(method=="动力炉结算"||method=="基地血量结算")reward=true;
                    }
                    if(!reward)continue;
                    button.onClick.AddListener(Claim);
                }
                button.gameObject.AddComponent<RuleTooltip>().Text = "额外获得2次工匠刷新。";
            }
        }
        private void Claim()
        {
            if (!ModuleSettings.InRun || !ModuleSettings.Extra.Growth || !ModuleSettings.LocalProgress.ClaimRepair(node)) return;
            ModuleSettings.SaveProgress();
            Plugin.Summary?.保存单局存档();
            if (Plugin.Role == 2) Plugin.Network.SendProgress(ModuleSettings.LocalProgress);
            Plugin.Log("[Growth] Repair node reroll claimed: " + node);
        }
    }
    [HarmonyPatch(typeof(地图页),"触发节点作用")]
    internal static class RepairNodeOpened
    {
        private static void Postfix(地图页 __instance,string 节点名)
        {
            if (!ModuleSettings.Extra.Growth || !ModuleSettings.InRun || __instance.修理_预制体 == null) return;
            var entry = __instance.节点作用列表.Find(x => x.节点名 == 节点名);
            if (entry == null || entry.作用 != 地图管理器.节点作用.修理) return;
            var panel = __instance.修理_预制体;
            (panel.GetComponent<RepairCoupons>() ?? panel.AddComponent<RepairCoupons>()).Bind(ModuleSettings.NodeKey);
        }
    }
}
