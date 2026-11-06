using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class DifficultyUnlocks
    {
        internal static void Restore(数据库_难度解锁 database)
        {
            var save=Object.FindObjectOfType<全局存档>();
            if(database==null||save==null||save.解锁难度列表==null||save.解锁难度列表.Count==0)return;
            int maximum=save.解锁难度列表.Max();
            foreach(var entry in database.难度列表)
                if(entry.难度数据.id<=maximum)entry.是否解锁=true;
        }
    }
    [HarmonyPatch(typeof(数据库_难度解锁),"内容解锁检测")]
    internal static class RestoreEarnedDifficulty
    {private static void Postfix(数据库_难度解锁 __instance)=>DifficultyUnlocks.Restore(__instance);}
    [HarmonyPatch(typeof(UI_内容解锁_难度选择),"Start")]
    internal static class RestoreDifficultySelection
    {private static void Prefix()=>DifficultyUnlocks.Restore(Object.FindObjectOfType<数据库_难度解锁>());}
}
