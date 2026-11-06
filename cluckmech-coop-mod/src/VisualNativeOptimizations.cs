using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class VisualSceneCache
    {
        private static GameObject rewards;
        private static readonly Dictionary<GameObject,float> coins=new Dictionary<GameObject,float>();
        internal static void Clear(){rewards=null;coins.Clear();}
        internal static GameObject Find(string name)
        {
            if(name!="奖励列表")return GameObject.Find(name);
            if(rewards==null)rewards=GameObject.Find(name);return rewards;
        }
        internal static GameObject Coin(GameObject prefab,Vector3 point,Quaternion rotation,Collider2D collector)
        {
            if(VisualPreferences.Coins)
            {
                var owner=collector.gameObject;
                if(coins.TryGetValue(owner,out float last)&&Time.realtimeSinceStartup-last<.1f)return null;
                coins[owner]=Time.realtimeSinceStartup;
            }
            return UnityEngine.Object.Instantiate(prefab,point,rotation);
        }
        internal static void Removed(GameObject owner){coins.Remove(owner);}
    }
    [HarmonyPatch(typeof(模块_拾取精灵_货币),"OnTriggerStay2D")]
    internal static class OptimizeCoinPresentation
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach(var code in instructions)
            {
                if(code.Calls(AccessTools.Method(typeof(GameObject),"Find")))
                    yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(VisualSceneCache),"Find")).MoveLabelsFrom(code);
                else if(code.operand is MethodInfo m&&m.DeclaringType==typeof(UnityEngine.Object)&&m.Name=="Instantiate"&&m.GetParameters().Length==3)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1).MoveLabelsFrom(code);
                    yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(VisualSceneCache),"Coin"));
                }
                else yield return code;
            }
        }
    }
    [HarmonyPatch(typeof(模块_状态持续效果),"状态持续")]
    internal static class AllocateActiveStatusOnly
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code=instructions.ToList();
            int allocation=code.FindIndex(c=>c.opcode==OpCodes.Newobj&&c.operand is ConstructorInfo ctor&&ctor.DeclaringType==typeof(模块_状态持续效果.状态伤害类));
            int query=code.FindIndex(c=>c.operand is MethodInfo m&&m.Name=="监测状态存在");
            if(allocation<0||query<allocation||!code[allocation+1].IsStloc()||!(code[query+1].opcode==OpCodes.Brfalse||code[query+1].opcode==OpCodes.Brfalse_S))throw new InvalidOperationException("Native status allocation seam changed");
            var create=new CodeInstruction(code[allocation]);var store=new CodeInstruction(code[allocation+1]);create.labels.Clear();store.labels.Clear();
            code[allocation].opcode=OpCodes.Nop;code[allocation].operand=null;code[allocation+1].opcode=OpCodes.Nop;code[allocation+1].operand=null;
            code.InsertRange(query+2,new[]{create,store});return code;
        }
    }
}
