using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class OtherworldDamage
    {
        private sealed class Origin { internal int Space; }
        internal struct Scope { internal bool Captured; internal int? Previous; }
        private static readonly ConditionalWeakTable<世界类.伤害数据包,Origin> origins=new ConditionalWeakTable<世界类.伤害数据包,Origin>();
        [System.ThreadStatic] internal static int? ApplyingSpace;
        internal static int Space(世界类.伤害数据包 packet)=>origins.TryGetValue(packet,out var origin)?origin.Space:OtherworldWorld.Space(packet.攻击者);
        internal static void Remember(世界类.伤害数据包 packet,int space)
        {origins.Remove(packet);origins.Add(packet,new Origin{Space=space});}
    }
    [HarmonyPatch]
    internal static class OtherworldProjectileOrigin
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {yield return AccessTools.Method(typeof(模块_伤害精灵),"伤害数据传递");yield return AccessTools.Method(typeof(模块_范围伤害精灵),"伤害数据传递");}
        private static void Postfix(Component __instance,世界类.伤害数据包 伤害数据包)
        {
            if(!OtherworldWorld.Active)return;
            var packet=__instance is 模块_伤害精灵 projectile?projectile.伤害数据:((模块_范围伤害精灵)__instance).伤害数据;
            OtherworldDamage.Remember(packet,OtherworldDamage.Space(伤害数据包));
        }
    }
    [HarmonyPatch]
    internal static class OtherworldProjectileAttacker
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {yield return AccessTools.Method(typeof(模块_伤害精灵),"攻击者传递");yield return AccessTools.Method(typeof(模块_范围伤害精灵),"攻击者传递");}
        private static void Postfix(Component __instance)
        {
            if(!OtherworldWorld.Active)return;
            var packet=__instance is 模块_伤害精灵 projectile?projectile.伤害数据:((模块_范围伤害精灵)__instance).伤害数据;
            OtherworldDamage.Remember(packet,KamuiWorld.At(__instance.transform.position));
        }
    }
    [HarmonyPatch]
    internal static class OtherworldProjectileContact
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {yield return AccessTools.Method(typeof(模块_伤害精灵),"OnTriggerEnter2D");yield return AccessTools.Method(typeof(模块_范围伤害精灵),"OnTriggerStay2D");}
        private static bool Prefix(Component __instance,Collider2D col,世界类.伤害数据包 ___伤害数据,阵营数据库 ___阵营,List<GameObject> ___已攻击单位,int ___当前命中数量)
        {
            if(!OtherworldWorld.Active)return true;
            if(OtherworldDamage.Space(___伤害数据)!=OtherworldWorld.Space(col.gameObject))return false;
            var actor=col.GetComponent<OtherworldActor>();if(actor==null||!actor.Phased)return true;
            bool damaging=__instance is 模块_伤害精灵 projectile?!projectile.攻击转换为治疗&&!projectile.被动触发:
                !((模块_范围伤害精灵)__instance).攻击转换为治疗&&___当前命中数量<=((模块_范围伤害精灵)__instance).命中数量上限;
            if(damaging&&___阵营!=null&&!___已攻击单位.Contains(col.gameObject)&&___阵营.运算是否可伤害((int)___伤害数据.所属阵营,___阵营.阵营对照表(col.gameObject.tag)))
                actor.Avoid(___伤害数据);
            return true;
        }
    }
}
