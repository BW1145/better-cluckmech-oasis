using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    public sealed class UpgradeSelectionIdentity : MonoBehaviour
    {
        internal 总结脚本.背包单元 Target;
        internal static void InventoryChanged()
        {
            foreach(var identity in Object.FindObjectsOfType<UpgradeSelectionIdentity>())
                if(identity.Target!=null)identity.Resolve(identity.GetComponent<奖励结算脚本>());
        }
        internal bool Resolve(奖励结算脚本 reward)
        {
            if(Target==null)return true;
            int slot=Plugin.Summary.当前拥有炮台.IndexOf(Target);
            if(slot>=0)
            {
                Plugin.Summary.当前选中的背包id=slot;
                reward.当前选择的词缀=new Vector2Int(slot,reward.当前选择的词缀.y);
                if(Plugin.Summary.地图存档.是否选择单位强化)Plugin.Summary.地图存档.选择的背包位置=slot;
                return true;
            }
            Target=null;
            Plugin.Summary.地图存档.是否选择单位强化=false;
            foreach(var flow in Object.FindObjectsOfType<选择词条流程脚本>(true))
            {
                var select=flow.流程_选择要强化鸡械;
                if(select==null)continue;
                select.关闭子单元();select.gameObject.SetActive(true);
                flow.玩家背包脚本?.更新背包();
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(奖励结算脚本),"选择要升级的炮台")]
    internal static class CaptureUpgradeMachine
    {
        private static void Prefix(奖励结算脚本 __instance)
        {
            var identity=__instance.GetComponent<UpgradeSelectionIdentity>()??__instance.gameObject.AddComponent<UpgradeSelectionIdentity>();
            identity.Target=Plugin.Summary.当前拥有炮台[Plugin.Summary.当前选中的背包id];
        }
    }
    [HarmonyPatch(typeof(奖励结算脚本),"词缀结算")]
    internal static class ResolveUpgradeMachine
    {
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(奖励结算脚本 __instance)=>__instance.GetComponent<UpgradeSelectionIdentity>()?.Resolve(__instance)!=false;
        private static void Postfix(奖励结算脚本 __instance)
        {var identity=__instance.GetComponent<UpgradeSelectionIdentity>();if(identity!=null)identity.Target=null;}
    }
}
