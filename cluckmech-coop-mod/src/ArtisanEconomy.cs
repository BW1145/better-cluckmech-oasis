using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CluckmechCoop
{
    internal static class ArtisanEconomy
    {
        internal static bool Enabled => ModuleSettings.InRun && ModuleSettings.Extra.Growth;
        internal static List<int> Choices(总结脚本.背包单元 machine)
        {
            var result=new List<int>();
            if(machine==null || machine.强化词条.Count>=machine.强化上限)return result;
            var library=UnityEngine.Object.FindObjectOfType<cz升级词缀库>();
            var affixes=UnityEngine.Object.FindObjectOfType<cz词缀库>();
            if(library==null || affixes==null || !library.获取单位词缀列表(machine.炮台id,out var pool))return result;
            foreach(int id in pool.Distinct())
            {
                var data=affixes.获取词缀数据(id);
                if(data!=null && (!data.是否唯一 || !machine.强化词条.Contains(id)) && StratagemCatalog.CanChoose(id,machine.强化词条))result.Add(id);
            }
            return result;
        }
        internal static void Save()
        {
            if(Plugin.Role!=0)Plugin.LocalPlayer?.上传玩家总结数据();
            ModuleSettings.SaveProgress();Plugin.Summary.保存单局存档();
            ArtisanEconomyUi.RefreshAll();
        }
        internal static int Refund(int paid,int tier)=>(int)((long)paid*(tier==1?50:tier==2?20:0)/100);
        internal static void Cancel()
        {
            if(!Enabled || ModuleSettings.LocalProgress.ArtisanPaid==0)return;
            ModuleSettings.LocalProgress.ArtisanPaid=0;Save();
        }
    }
    [HarmonyPatch(typeof(工匠脚本),"购买词条")]
    internal static class ArtisanPayment
    {
        private static void Prefix(工匠脚本 __instance,out int __state)
        {__state=ArtisanEconomy.Enabled && Plugin.Summary.当前拥有炮台.Count>0 && Plugin.Summary.当前金钱数>=__instance.强化价格?__instance.强化价格:-1;}
        private static void Postfix(int __state)
        {
            if(__state<0)return;
            ModuleSettings.LocalProgress.ArtisanPaid=__state;ArtisanEconomy.Save();
        }
    }
    [HarmonyPatch(typeof(奖励结算脚本),"词缀结算")]
    internal static class ArtisanCompleted
    {
        private sealed class Payment
        {internal 总结脚本.背包单元 Machine;internal int Count;}
        private static void Prefix(奖励结算脚本 __instance,out Payment __state)
        {
            __state=null;
            if(!ArtisanEconomy.Enabled || ModuleSettings.LocalProgress.ArtisanPaid<=0 || !Plugin.Summary.地图存档.是否位于工匠)return;
            int slot=__instance.当前选择的词缀.x;
            if(slot<0 || slot>=Plugin.Summary.当前拥有炮台.Count)return;
            var item=Plugin.Summary.当前拥有炮台[slot];
            __state=new Payment{Machine=item,Count=item.强化词条.Count};
        }
        private static void Postfix(Payment __state)
        {
            if(__state==null || __state.Machine.强化词条.Count!=__state.Count+1)return;
            int tier=Plugin.Units.获取单位单元(__state.Machine.炮台id).单位等级;
            int refund=ModuleSettings.LocalProgress.CompleteArtisan(tier);
            if(refund>0)Plugin.Summary.获得金钱(refund);
            ArtisanEconomyUi.LastReceipt=refund>0?"强化完成，返还"+refund+"金币":"强化完成";
            ArtisanEconomy.Save();
        }
    }
    [HarmonyPatch(typeof(工匠脚本),"触发取消强化")]
    internal static class ArtisanCancelled {private static void Postfix()=>ArtisanEconomy.Cancel();}
    [HarmonyPatch(typeof(选择词条流程脚本),"卖出词条")]
    internal static class ArtisanSold {private static void Postfix()=>ArtisanEconomy.Cancel();}
    [HarmonyPatch(typeof(选择词条流程脚本),"选择升级炮台结算")]
    internal static class ArtisanSelected {private static void Postfix()=>ArtisanEconomyUi.RefreshAll();}
    [HarmonyPatch(typeof(工匠脚本),"Start")]
    internal static class ArtisanBenefitsReady
    {private static void Postfix(工匠脚本 __instance){if(ArtisanEconomy.Enabled)(__instance.GetComponent<ArtisanEconomyUi>()??__instance.gameObject.AddComponent<ArtisanEconomyUi>()).Build();}}

    public sealed class ArtisanEconomyUi : MonoBehaviour
    {
        private static readonly HashSet<ArtisanEconomyUi> instances=new HashSet<ArtisanEconomyUi>();
        internal static string LastReceipt="";
        private Text details,pending;
        private Button benefit;
        internal static void RefreshAll(){foreach(var ui in instances)if(ui!=null)ui.Refresh();}
        internal void Build()
        {
            NativeUi.Capture(transform);instances.Add(this);LastReceipt="";
            var root=NativeUi.Rect(transform,"强化回馈",0,0,460,184);
            root.anchorMin=root.anchorMax=root.pivot=new Vector2(1,1);root.anchoredPosition=new Vector2(-24,-224);
            NativeUi.Image(root,NativeUi.TransferPanel,Color.white);
            details=NativeUi.Label(root,"",16,8,428,104,21);
            benefit=NativeUi.TransferAction(root,"选择强化",16,126,428,48,()=>ArtisanBenefitPanel.Show(this));
            pending=NativeUi.Label(transform,"",0,0,800,44,23);
            pending.rectTransform.anchorMin=pending.rectTransform.anchorMax=pending.rectTransform.pivot=new Vector2(.5f,1);
            pending.rectTransform.anchoredPosition=new Vector2(0,-24);pending.alignment=TextAnchor.MiddleCenter;
            Refresh();
        }
        private void Refresh()
        {
            var progress=ModuleSettings.LocalProgress;
            string line="C级返款50%　B级返款20%";
            int slot=Plugin.Summary.当前选中的背包id;
            if(progress.ArtisanPaid>0 && slot>=0 && slot<Plugin.Summary.当前拥有炮台.Count)
            {
                int tier=Plugin.Units.获取单位单元(Plugin.Summary.当前拥有炮台[slot].炮台id).单位等级;
                line="本次支付"+progress.ArtisanPaid+"，预计返款"+ArtisanEconomy.Refund(progress.ArtisanPaid,tier);
            }
            pending.text=line;pending.gameObject.SetActive(progress.ArtisanPaid>0);
            details.transform.parent.gameObject.SetActive(progress.ArtisanPaid==0);
            details.text=line+"\n强化净花费 "+progress.ArtisanSpend+"／"+PlayerProgress.ArtisanThreshold+"　"+LastReceipt;
            benefit.GetComponentInChildren<Text>().text="自选强化　剩余"+progress.ArtisanCredits+"次";
            benefit.interactable=progress.ArtisanCredits>0 && progress.ArtisanPaid==0;
        }
        private void OnDestroy(){instances.Remove(this);}
    }
}
