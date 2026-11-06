using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class NativeCombatLifecycle
    {
        internal static void PrepareParticles()
        {
            if (!NativeRepairs.Enabled) return;
            // Repair the loaded source systems once, before effects are instantiated.
            // Sub-emitter lifetime belongs to its parent particle system.
            foreach (var parent in Resources.FindObjectsOfTypeAll<ParticleSystem>())
            {
                var children = parent.subEmitters;
                for (int i = 0; i < children.subEmittersCount; i++)
                {
                    var child = children.GetSubEmitterSystem(i);
                    if (child == null) continue;
                    var main = child.main;
                    if (main.stopAction != ParticleSystemStopAction.None) main.stopAction = ParticleSystemStopAction.None;
                }
            }
        }
    }

    [HarmonyPatch(typeof(模块_天赋影响), "Start")]
    internal static class ReplaceLegacyTalentInitialization
    {
        private static bool Prefix(模块_天赋影响 __instance)
        {
            var summary = Plugin.Summary != null ? Plugin.Summary : Object.FindObjectOfType<总结脚本>();
            if (!NativeRepairs.Enabled || summary == null || summary.GetComponent<模块_天赋总结>() != null) return true;
            // Current builds apply global bonuses through this component. Old prefabs
            // still carry the retired talent implementation referencing 模块_天赋总结.
            if (__instance.GetComponent<模块_全局加成实现>() == null)
                __instance.gameObject.AddComponent<模块_全局加成实现>();
            Object.Destroy(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(模块_伤害精灵), "OnTriggerEnter2D")]
    internal static class InitializeProjectileContact
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(模块_伤害精灵 __instance, ref 阵营数据库 ___阵营)
        {
            if (NativeRepairs.Enabled && ___阵营 == null) __instance.初始化();
        }
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.operand is MethodInfo method && method.Name == "TryGetComponent" && method.IsGenericMethod &&
                    method.GetGenericArguments()[0] == typeof(模块_基本属性))
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InitializeProjectileContact), nameof(Stats))).MoveLabelsFrom(instruction);
                else if(instruction.operand is MethodInfo send&&send.DeclaringType==typeof(GameObject)&&send.Name=="SendMessage"&&send.GetParameters().Length==3)
                    yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(InitializeProjectileContact),nameof(Deliver))).MoveLabelsFrom(instruction);
                else yield return instruction;
            }
        }
        private static void Deliver(GameObject summoned,string message,object packet,SendMessageOptions options)
        {if(summoned!=null)summoned.SendMessage(message,packet,options);}
        private static bool Stats(GameObject source, out 模块_基本属性 stats)
        {
            stats = source == null ? null : source.GetComponent<模块_基本属性>();
            return stats != null;
        }
    }

    [HarmonyPatch]
    internal static class InitializeDamageReceivers
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (var method in AccessTools.GetDeclaredMethods(typeof(模块_受伤)))
                if (method.Name == "受到伤害" || method.Name == "受到治疗" || method.Name == "实现伤害") yield return method;
        }
        [HarmonyPriority(Priority.First)]
        private static void Prefix(模块_受伤 __instance, 模块_基本属性 ___基本属性)
        {
            if (NativeRepairs.Enabled && ___基本属性 == null) __instance.初始化();
        }
    }

    [HarmonyPatch(typeof(模块_抗性属性), "Awake")]
    internal static class CompleteNativeResistanceTable
    {
        private const int count = (int)攻击类型与状态数据库.状态枚举.枚举数量;
        private static void Postfix(模块_抗性属性 __instance)
        {
            if (!NativeRepairs.Enabled) return;
            var entries = __instance.状态抗性列表;
            if (entries == null) entries = new 模块_抗性属性.状态抗性单元[count];
            else if (entries.Length < count) System.Array.Resize(ref entries, count);
            for (int i = 0; i < entries.Length; i++)
                if (entries[i] == null) entries[i] = new 模块_抗性属性.状态抗性单元();
            __instance.状态抗性列表 = entries;
        }
    }

    [HarmonyPatch(typeof(模块_状态属性), "增加状态")]
    internal static class InitializeStateReceiver
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(模块_状态属性 __instance, 模块_抗性属性 ___抗性属性)
        {
            if (NativeRepairs.Enabled && ___抗性属性 == null) __instance.初始化();
        }
    }
}
