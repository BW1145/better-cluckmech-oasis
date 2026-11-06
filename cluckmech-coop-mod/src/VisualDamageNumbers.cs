using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class VisualDamageNumbers
    {
        private static readonly Dictionary<(模块_受伤,GameObject,Vector4),VisualNumberSum> active=new Dictionary<(模块_受伤,GameObject,Vector4),VisualNumberSum>();
        internal static void Clear()=>active.Clear();
        internal static void Removed(GameObject target)
        {var keys=new List<(模块_受伤,GameObject,Vector4)>();foreach(var key in active.Keys)if(key.Item1==null||key.Item1.gameObject==target)keys.Add(key);foreach(var key in keys)active.Remove(key);}
        internal static GameObject Create(GameObject prefab,Vector3 point,Quaternion rotation,模块_受伤 owner,Vector4 color)
        {
            if(prefab.GetComponent<显示精灵>()==null)return VisualParticlePool.Create(prefab,point,rotation);
            if(!VisualPreferences.Numbers)return Object.Instantiate(prefab,point,rotation);
            var key=(owner,prefab,color);
            if(active.TryGetValue(key,out var previous)&&previous!=null&&Time.realtimeSinceStartup-previous.Started<.1f)
            {
                // Native code applies its scale multiplier after each request.
                // Restore the source scale so merging does not compound it.
                previous.transform.localScale=prefab.transform.localScale;return previous.gameObject;
            }
            var obj=Object.Instantiate(prefab,point,rotation);var sum=obj.AddComponent<VisualNumberSum>();sum.Started=Time.realtimeSinceStartup;
            active[key]=sum;return obj;
        }
    }
    public sealed class VisualNumberSum:MonoBehaviour
    {internal float Started,Total;}
    [HarmonyPatch(typeof(显示精灵),"UI显示数字",new[]{typeof(float),typeof(Vector4)})]
    internal static class SumPresentedNumbers
    {
        private static void Prefix(显示精灵 __instance,ref float 伤害数值)
        {var sum=__instance.GetComponent<VisualNumberSum>();if(sum==null)return;sum.Total+=伤害数值;伤害数值=sum.Total;}
    }
    [HarmonyPatch]
    internal static class OptimizeNativeHitPresentation
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(模块_受伤),"伤害出现",new[]{typeof(float),typeof(Vector4)});
            yield return AccessTools.Method(typeof(模块_受伤),"暴击出现",new[]{typeof(float)});
        }
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,MethodBase __originalMethod)
        {
            foreach(var c in instructions)
            {
                if(c.operand is MethodInfo m&&m.DeclaringType==typeof(Object)&&m.Name=="Instantiate"&&m.GetParameters().Length==3)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(c);
                    if(__originalMethod.Name=="伤害出现")yield return new CodeInstruction(OpCodes.Ldarg_2);
                    else
                    {
                        for(int i=0;i<4;i++)yield return new CodeInstruction(OpCodes.Ldc_R4,1f);
                        yield return new CodeInstruction(OpCodes.Newobj,AccessTools.Constructor(typeof(Vector4),new[]{typeof(float),typeof(float),typeof(float),typeof(float)}));
                    }
                    yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(VisualDamageNumbers),"Create"));
                }
                else yield return c;
            }
        }
    }
}
