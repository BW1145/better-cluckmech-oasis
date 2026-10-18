using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    [HarmonyPatch]
    internal static class KamuiTargetCandidates
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return TranspilerContract.InstanceVoid(typeof(模块_攻击), "检索目标");
            yield return TranspilerContract.InstanceVoid(typeof(模块_玩家_攻击), "检索目标");
        }
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            var find = AccessTools.Method(typeof(GameObject), nameof(GameObject.FindGameObjectsWithTag), new[] { typeof(string) });
            var scoped = AccessTools.Method(typeof(KamuiWorld), nameof(KamuiWorld.Tagged), new[] { typeof(string), typeof(Component) });
            var instructions = TranspilerContract.Exact(code, nameof(KamuiTargetCandidates), 1, instruction => instruction.Calls(find));
            foreach (var instruction in instructions)
            {
                if(instruction.Calls(find))
                {
                    var owner = new CodeInstruction(OpCodes.Ldarg_0);
                    owner.labels.AddRange(instruction.labels);
                    owner.blocks.AddRange(instruction.blocks);
                    yield return owner;
                    yield return new CodeInstruction(OpCodes.Call, scoped);
                }
                else yield return instruction;
            }
        }
        private static bool Prefix(Component __instance)
        {
            if(OtherworldWorld.Unit(__instance.gameObject)?.Restoring==true)return false;
            int space=OtherworldWorld.Space(__instance.gameObject);
            if(space!=0&&KamuiWorld.Idle(space))return false;
            return true;
        }
    }
    [HarmonyPatch]
    internal static class KamuiAuraCandidates
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return TranspilerContract.InstanceVoid(typeof(模块_光环), "检索光环内敌方");
            yield return TranspilerContract.InstanceVoid(typeof(模块_光环), "检索光环内友方");
        }
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            var all = AccessTools.Field(typeof(世界事件系统), "场上所有单位");
            var scoped = AccessTools.Method(typeof(KamuiWorld), nameof(KamuiWorld.LocalUnits), new[] { typeof(Component) });
            var instructions = TranspilerContract.Exact(code, nameof(KamuiAuraCandidates), 1,
                instruction => instruction.opcode == OpCodes.Ldsfld && Equals(instruction.operand, all));
            foreach (var instruction in instructions)
            {
                if(instruction.opcode==OpCodes.Ldsfld&&Equals(instruction.operand,all))
                {
                    var owner = new CodeInstruction(OpCodes.Ldarg_0);
                    owner.labels.AddRange(instruction.labels);
                    owner.blocks.AddRange(instruction.blocks);
                    yield return owner;
                    yield return new CodeInstruction(OpCodes.Call, scoped);
                }
                else yield return instruction;
            }
        }
    }
    [HarmonyPatch(typeof(模块_移动),"初始化")]
    internal static class KamuiNavigationMap
    {
        private static void Postfix(模块_移动 __instance)
        {
            int space=OtherworldWorld.Space(__instance.gameObject);
            var unit=OtherworldWorld.Unit(__instance.gameObject);
            if(space!=0&&unit!=null&&__instance.寻路脚本!=null)__instance.寻路脚本.map=KamuiWorld.MapFor(unit,KamuiWorld.Room(space));
        }
    }
    [HarmonyPatch(typeof(模块_移动),"移动目标赋值",new[]{typeof(Vector2)})]
    internal static class KamuiMovementTarget
    {
        private static bool Prefix(模块_移动 __instance,ref Vector2 坐标)
        {
            var unit=OtherworldWorld.Unit(__instance.gameObject);if(unit==null||unit.Space==0)return true;
            var room=KamuiWorld.Room(unit.Space);if(room==null)return true;
            if(KamuiWorld.Idle(unit.Space)){__instance.寻路脚本.Stop();return false;}
            if(!room.Bounds.Contains(坐标))
            {
                var target=unit.Attack?.攻击目标;
                坐标=target!=null&&OtherworldWorld.Space(target.gameObject)==unit.Space?(Vector2)target.position:(Vector2)unit.transform.position;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(模块_仇恨),"运算最大仇恨目标")]
    internal static class KamuiAggroTargets
    {
        private static void Prefix(模块_仇恨 __instance)
        {
            if(KamuiWorld.Rooms.Count==0)return;
            int space=OtherworldWorld.Space(__instance.gameObject);
            __instance.当前仇恨列表.RemoveAll(t=>t.仇恨目标!=null&&OtherworldWorld.Space(t.仇恨目标)!=space);
        }
    }
    [HarmonyPatch(typeof(模块_拾取精灵_移动),"Update")]
    internal static class KamuiPickupMovement
    {
        private static bool Prefix(模块_拾取精灵_移动 __instance)
        {
            if(KamuiWorld.Rooms.Count==0)return true;
            int space=KamuiWorld.At(__instance.transform.position);
            if(space!=0&&Plugin.Role==1&&space-1!=Plugin.Role)return false;
            if(__instance.跟随目标!=null&&OtherworldWorld.Space(__instance.跟随目标)!=space)__instance.跟随目标=null;
            if(space==0)return true;
            var player=OtherworldActions.Player(space-1);
            if(player==null||player.Space!=space||player.Death.是否玩家死亡)return false;
            __instance.跟随目标=player.gameObject;return true;
        }
    }
}
