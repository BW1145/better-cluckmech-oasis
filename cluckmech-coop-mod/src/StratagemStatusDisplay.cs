using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    [HarmonyPatch(typeof(模块_状态持续效果),"状态持续")]
    internal static class StratagemStatusDisplayScope
    {
        private static void Prefix(模块_状态持续效果 __instance,out StratagemHitDisplay.StatusScope __state)
            =>__state=StratagemHitDisplay.CaptureStatus(__instance.gameObject);
        private static Exception Finalizer(Exception __exception,StratagemHitDisplay.StatusScope __state)
        {__state.Dispose();return __exception;}
    }
    [HarmonyPatch(typeof(模块_状态持续效果),"状态伤害统计")]
    internal static class StratagemStatusDisplaySum
    {
        private static void Prefix(模块_状态持续效果 __instance)=>StratagemHitDisplay.BeginStatusSum(__instance.gameObject);
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var attributes=AccessTools.Field(typeof(模块_状态属性.状态类),"加成属性");
            var values=AccessTools.Field(typeof(模块_攻击详细属性.状态属性单元),"当前属性效果");
            var x=AccessTools.Field(typeof(Vector4),"x");
            var code=TranspilerContract.Exact(instructions,"Stratagem native status display sum",1,
                c=>c.opcode==OpCodes.Ldfld&&Equals(c.operand,attributes));
            int i=code.FindIndex(c=>c.opcode==OpCodes.Ldfld&&Equals(c.operand,attributes));
            if(i+2>=code.Count||code[i+1].opcode!=OpCodes.Ldflda||!Equals(code[i+1].operand,values)||
                code[i+2].opcode!=OpCodes.Ldfld||!Equals(code[i+2].operand,x)||
                code[i+1].labels.Count!=0||code[i+2].labels.Count!=0||code[i+1].blocks.Count!=0||code[i+2].blocks.Count!=0)
                throw new InvalidOperationException("Native status sum shape changed");
            // The helper returns exactly the original value. Keep the original add,
            // enumeration, branches and exception boundaries, with no second state scan.
            code[i].opcode=OpCodes.Call;
            code[i].operand=AccessTools.Method(typeof(StratagemHitDisplay),nameof(StratagemHitDisplay.StateValue));
            code.RemoveRange(i+1,2);return code;
        }
    }
    [HarmonyPatch(typeof(模块_死亡),"死亡实现")]
    internal static class StratagemDisplayBeforeDeath
    {
        // Send queued killing blows before native death RPCs and eventual destruction.
        // The first death drains the shared batch, not one message per dead unit.
        [HarmonyPriority(Priority.First)]
        private static void Prefix()=>StratagemHitDisplay.Flush();
    }
}
