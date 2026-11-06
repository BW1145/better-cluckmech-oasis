using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    // Network final values remain authoritative after any native or mod recalculation.
    // Preview objects and units awaiting their first snapshot retain native initialization.
    internal static class CoopStatAuthority
    {
        internal static 联机_单位属性同步 Snapshot(Component owner)
        {
            if (Plugin.Role != 2 || owner == null) return null;
            var sync = owner.GetComponent<联机_单位属性同步>();
            return sync != null && sync.isClient && sync.Network生命值上限 > 0 ? sync : null;
        }
        internal static void Apply(Component owner, 联机_单位属性同步 sync)
        {
            if (sync == null) return;
            if (owner is 模块_攻击属性 attack)
            {
                attack.当前攻击力 = sync.Network当前攻击力;
                attack.当前攻击范围 = sync.Network当前攻击范围;
                attack.当前攻击速度 = sync.Network当前攻击速度;
            }
            else if (owner is 模块_基本属性 health)
            {
                health.当前生命值上限 = sync.Network生命值上限;
                health.当前生命值 = sync.Network当前生命值;
                health.当前抗性 = sync.Network当前抗性;
                health.当前格挡值 = sync.Network当前格挡;
                health.当前反伤值 = sync.Network当前反伤值;
            }
            else if (owner is 模块_移动属性 movement) movement.当前移动速度 = sync.Network当前移动速度;
        }
        internal static bool Calculate(Component owner)=>Snapshot(owner)==null;
    }

    [HarmonyPatch]
    internal static class PreserveGuestFinalStats
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (var name in new[] { "运算攻击力", "运算攻击速度", "运算攻击范围" })
                yield return AccessTools.Method(typeof(模块_攻击属性), name);
            foreach (var name in new[] { "运算生命值上限", "运算抗性", "运算格挡值", "运算反伤值" })
                yield return AccessTools.Method(typeof(模块_基本属性), name);
            yield return AccessTools.Method(typeof(模块_移动属性), "运算移动速度");
        }
        private static void Prefix(Component __instance, out 联机_单位属性同步 __state)
            =>__state=CoopStatAuthority.Snapshot(__instance);
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,ILGenerator generator)
        {
            var code=instructions.ToList();var calculate=generator.DefineLabel();code[0].labels.Add(calculate);
            // Keep every Harmony prefix/postfix pair active, including temporary
            // base-value scopes. Only the native calculation body is bypassed.
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(CoopStatAuthority),nameof(CoopStatAuthority.Calculate)));
            yield return new CodeInstruction(OpCodes.Brtrue,calculate);
            yield return new CodeInstruction(OpCodes.Ret);
            foreach(var instruction in code)yield return instruction;
        }
        // Other Harmony postfixes may multiply the field even when the original is skipped.
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Component __instance, 联机_单位属性同步 __state)
            => CoopStatAuthority.Apply(__instance, __state);
    }

    [HarmonyPatch]
    internal static class BindNativeSyncReferences
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (var name in new[] { "同步_生命值", "同步_当前生命值", "同步_生命值上限", "同步_母体id", "同步_攻击力",
                "同步_攻击范围", "同步_攻击速度", "同步_移动速度", "同步_抗性", "同步_格挡", "同步_反伤值" })
                yield return AccessTools.Method(typeof(联机_单位属性同步), name);
        }
        private static void Prefix(联机_单位属性同步 __instance, ref 模块_基本属性 ___基本属性,
            ref 模块_攻击属性 ___攻击属性, ref 模块_移动属性 ___移动属性)
        {
            if (___基本属性 == null) ___基本属性 = __instance.GetComponent<模块_基本属性>();
            if (___攻击属性 == null) ___攻击属性 = __instance.GetComponent<模块_攻击属性>();
            if (___移动属性 == null) ___移动属性 = __instance.GetComponent<模块_移动属性>();
        }
    }

    [HarmonyPatch(typeof(模块_受伤), "受到伤害", new[] { typeof(世界类.伤害数据包) })]
    internal static class GuestNativePacketDisplay
    {
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(模块_受伤 __instance, 世界类.伤害数据包 sjb)
        {
            if(Plugin.Role!=2||__instance.GetComponent<联机_单位属性同步>()==null)return true;
            OtherworldHitDisplay.NativeLocal(__instance,sjb);
            return false;
        }
    }

    [HarmonyPatch(typeof(模块_受伤), "受到伤害", new[] { typeof(float) })]
    internal static class GuestNativeScalarDisplay
    {
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(模块_受伤 __instance,float 伤害)
        {
            if(Plugin.Role!=2||__instance.GetComponent<联机_单位属性同步>()==null)return true;
            OtherworldHitDisplay.NativeScalar(__instance,伤害);
            return false;
        }
    }

    [HarmonyPatch(typeof(模块_受伤), "受到伤害", new[] { typeof(模块_状态持续效果.状态伤害类) })]
    internal static class GuestNativeStatusDisplay
    {
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(模块_受伤 __instance,模块_状态持续效果.状态伤害类 sh)
        {
            if(Plugin.Role!=2||__instance.GetComponent<联机_单位属性同步>()==null)return true;
            int color=sh.状态==攻击类型与状态数据库.状态枚举.灼烧?1:sh.状态==攻击类型与状态数据库.状态枚举.中毒?2:3;
            OtherworldHitDisplay.NativeScalar(__instance,sh.数值,color);
            return false;
        }
    }
}
