using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace CluckmechCoop
{
    [HarmonyPatch(typeof(UI_暂停菜单信息显示),"更新角色")]
    internal static class OtherworldPauseCharacter
    {
        private static bool Prefix(UI_暂停菜单信息显示 __instance,总结脚本 ___总结)
        {
            int id=___总结?.单局统计数据.使用角色??-1;
            var hero=OtherworldCatalog.Hero(id);
            // Native refresh ignores 角色名字 and localizes an unregistered key.
            // Also clear the previous selection when a persistent menu is reused.
            if(__instance.角色!=null)
                for(int i=0;i<__instance.角色.Length;i++)
                    if(__instance.角色[i]!=null)__instance.角色[i].SetActive(i==id);
            if(hero==null)return true;
            if(__instance.角色名字UI!=null)__instance.角色名字UI.text=hero.Name;
            return false;
        }
    }
    [HarmonyPatch]
    internal static class MapBoonsPauseVisibility
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(暂停菜单),"开启菜单");
            yield return AccessTools.Method(typeof(暂停菜单),"关闭菜单");
        }
        private static void Postfix(暂停菜单 __instance)=>MapBoonsUi.SetPauseVisibility(__instance.是否开启);
    }
}
