using HarmonyLib;
using UnityEngine;
using 函数库;

namespace CluckmechCoop
{
    internal static class NativePeriodicSpawn
    {
        internal static void Tick(Component source,GameObject prefab,float period,ref float remaining,bool mother)
        {
            remaining-=Time.deltaTime;
            if(remaining>0)return;
            remaining=period; // A failed/guest spawn still consumes this period.
            if(prefab==null||Plugin.Role==2&&prefab.GetComponent<模块_基本属性>()!=null)return;
            _=Random.Range(-.8f,.8f); // Retain the original random draw.
            var child=词条函数.召唤.召唤衍生物(prefab,source.gameObject,source.transform.position);
            if(child==null)return;
            child.transform.localScale=source.transform.localScale;
            if(mother&&child.TryGetComponent<模块_基本属性>(out var hp))
            {var parent=source.GetComponent<模块_基本属性>();if(parent!=null)hp.母体id=parent.ID;}
        }
    }
    [HarmonyPatch(typeof(词条_100134_钢铁蜗牛),"生成生成物")]
    internal static class RepairSnailSpawn
    {
        private static bool Prefix(词条_100134_钢铁蜗牛 __instance,GameObject ___生成物,ref float ___当前计时器)
        {
            if(!NativeRepairs.Enabled)return true;
            NativePeriodicSpawn.Tick(__instance,___生成物,__instance.计时器,ref ___当前计时器,true);return false;
        }
    }
    [HarmonyPatch(typeof(词条_194_滚烫步伐),"生成生成物")]
    internal static class RepairHotStepSpawn
    {
        private static bool Prefix(词条_194_滚烫步伐 __instance,ref float ___当前计时器)
        {
            if(!NativeRepairs.Enabled)return true;
            NativePeriodicSpawn.Tick(__instance,__instance.生成物,__instance.计时器,ref ___当前计时器,false);return false;
        }
    }
}
