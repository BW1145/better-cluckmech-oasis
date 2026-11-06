using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

internal static class Update0621Checks
{
    internal static void Run(Assembly native,Assembly mod,Action<List<CodeInstruction>> stack)
    {
        int checks=0;
        foreach(string name in new[]{"AllocateActiveStatusOnly","ReuseNativeStatusParticles","OptimizeCoinPresentation","OptimizeNativeHitPresentation"})
        {
            var type=mod.GetType("CluckmechCoop."+name,true);var targets=AccessTools.Method(type,"TargetMethods");
            var methods=targets!=null?((IEnumerable<MethodBase>)targets.Invoke(null,null)).ToArray():new[]{
                name=="OptimizeCoinPresentation"?AccessTools.Method(native.GetType("模块_拾取精灵_货币"),"OnTriggerStay2D"):AccessTools.Method(native.GetType("模块_状态持续效果"),"状态持续")};
            foreach(var method in methods)
            {
                var before=PatchProcessor.GetOriginalInstructions(method);var patch=AccessTools.Method(type,"Transpiler");
                object[] args=patch.GetParameters().Length==2?new object[]{before,method}:new object[]{before};
                var after=((IEnumerable<CodeInstruction>)patch.Invoke(null,args)).ToList();stack(after);checks++;
            }
        }
        foreach(string name in new[]{"RegisterVisualParticlePrefabs","ThemeTestMenu","ThemeTestMapReady","ThemeTestFailureReturn"})
        {
            var attr=mod.GetType("CluckmechCoop."+name).GetCustomAttributes(typeof(HarmonyPatch),false).Cast<HarmonyPatch>().Single();
            if(AccessTools.Method(attr.info.declaringType,attr.info.methodName)==null)throw new Exception("Missing binding "+name);checks++;
        }
        foreach(string name in new[]{"ThemeTestSaveGuard","ThemeTestStatisticsGuard"})
            foreach(var target in (IEnumerable<MethodBase>)AccessTools.Method(mod.GetType("CluckmechCoop."+name),"TargetMethods").Invoke(null,null))
            {if(target==null)throw new Exception("Missing test isolation entry");checks++;}
        Console.WriteLine($"PASS: {checks} native optimization IL stacks, entry bindings and test isolation bindings");
    }
}
