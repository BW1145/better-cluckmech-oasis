using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

internal static class CoopRepairChecks
{
    internal static void CheckDisplayShow(List<CodeInstruction> show,Action<bool,string> check)
    {
        foreach(string name in new[]{"normal","critical","colored"})
            check(show.Any(c=>c.opcode==OpCodes.Ldsfld&&c.operand is FieldInfo f&&f.DeclaringType.Name=="OtherworldHitDisplay"&&f.Name==name),"cached native display route: "+name);
        var calls=show.Where(c=>c.operand is MethodInfo).Select(c=>(MethodInfo)c.operand).ToArray();
        check(calls.Length>0&&calls.All(m=>m.Name=="Invoke"&&typeof(Delegate).IsAssignableFrom(m.DeclaringType)),"Show calls only cached display delegates, not damage/status/events");
        check(!show.Any(c=>c.opcode==OpCodes.Stfld||c.opcode==OpCodes.Stsfld),"Show does not write gameplay fields");
    }
    internal static void Run(Assembly native,Assembly mod)
    {
        int checks=0;
        void Check(bool pass,string label){checks++;if(!pass)throw new Exception(label);}
        Type Mod(string name)=>mod.GetType("CluckmechCoop."+name,true);
        List<CodeInstruction> Body(Type type,string name)=>PatchProcessor.GetOriginalInstructions(AccessTools.DeclaredMethod(type,name));
        bool Calls(CodeInstruction c,string type,string name)=>c.operand is MethodInfo m&&m.DeclaringType.Name==type&&m.Name==name;
        bool Field(CodeInstruction c,string name)=>c.operand is FieldInfo f&&f.Name==name;
        var injury=native.GetType("模块_受伤",true);
        foreach(string method in new[]{"伤害出现","暴击出现"})
        {
            var types=method=="伤害出现"?new[]{typeof(float),Assembly.Load("UnityEngine.CoreModule").GetType("UnityEngine.Vector4")}:new[]{typeof(float)};
            var helper=AccessTools.Method(injury,method,types);Check(helper!=null&&helper.IsPrivate,"native display seam exists: "+method);
            var code=PatchProcessor.GetOriginalInstructions(helper);
            Check(code.Any(c=>c.opcode==OpCodes.Ldc_I4_S&&Convert.ToInt32(c.operand)==75)||code.Any(c=>c.opcode==OpCodes.Ldc_I4&&Equals(c.operand,75)),"native 75 display cap: "+method);
            Check(code.Any(c=>Field(c,"是否位于相机视野"))&&code.Any(c=>Calls(c,"设置管理器","当前是否隐藏伤害数字"))&&code.Any(c=>Calls(c,"设置管理器","当前是否隐藏击中特效")),"native view/settings gate retained: "+method);
        }
        foreach(var pair in new[]{("RepairSnailSpawn","词条_100134_钢铁蜗牛"),("RepairHotStepSpawn","词条_194_滚烫步伐")})
        {
            var target=native.GetType(pair.Item2,true);var patch=Mod(pair.Item1);
            Check(AccessTools.Method(target,"生成生成物")!=null,"native periodic entry exists");
            var prefix=AccessTools.DeclaredMethod(patch,"Prefix");
            foreach(var p in prefix.GetParameters().Where(p=>p.Name.StartsWith("___")))Check(AccessTools.Field(target,p.Name.Substring(3))!=null,"native injected field: "+p.Name);
            Check(Body(patch,"Prefix").Count(c=>Calls(c,"NativePeriodicSpawn","Tick"))==1,"specific caller uses periodic repair");
            var original=Body(target,"生成生成物");
            Check(original.Any(c=>Calls(c,"召唤","召唤衍生物"))&&original.Any(c=>Calls(c,"GameObject","get_transform")),"original unsafe summon caller confirmed");
        }
        var tick=Body(Mod("NativePeriodicSpawn"),"Tick");
        Check(tick.Count(c=>Calls(c,"召唤","召唤衍生物"))==1&&!tick.Any(c=>Calls(c,"NetworkServer","Spawn")),"original summon remains responsible for Spawn");
        var local=Body(Mod("OtherworldHitDisplay"),"Local");
        Check(!local.Any(c=>c.operand is MethodInfo m&&(m.Name=="受到伤害"||m.Name=="实现伤害"||m.Name=="SendMessage")),"local presentation has no native gameplay call");
        CheckDisplayShow(Body(Mod("OtherworldHitDisplay"),"Show"),Check);
        Check(AccessTools.Field(injury,"引用器")!=null&&Body(injury,"Start").Any(c=>Calls(c,"模块_受伤","初始化")),"native Start owns display reference initialization");
        var replay=Body(Mod("OtherworldHitDisplay"),"Replay");
        Check(replay.Any(c=>Calls(c,"OtherworldWorld","Unit"))&&replay.Any(c=>Field(c,"references")),"network replay waits for native display and space registration");
        var delayedDisplay=Mod("OtherworldHitDisplay").GetNestedTypes(BindingFlags.NonPublic).Single(t=>t.Name.StartsWith("<ReplayPending>"));
        var delayedCode=Body(delayedDisplay,"MoveNext");
        Check(delayedCode.Any(c=>Calls(c,"SceneLifecycle","IsCurrent"))&&delayedCode.Any(c=>Field(c,"Token")),"pending display retains scene and combat token guards");
        var guard=Body(Mod("OtherworldDirectDamage"),"Prefix");
        Check(guard.Any(c=>Calls(c,"OtherworldHitDisplay","Local"))&&guard.Any(c=>Calls(c,"OtherworldDamage","Space"))&&guard.Any(c=>Calls(c,"OtherworldActor","Avoid")),"guard retains source-space and phase handling");
        var receive=Body(Mod("OtherworldNetwork"),"Receive");
        Check(receive.FindIndex(c=>Calls(c,"OtherworldHitDisplay","Receive"))<receive.FindIndex(c=>Field(c,"spawned")||Calls(c,"NetworkClient","get_spawned")),"results are read before requiring the skill actor");
        Check(!receive.Any(c=>Calls(c,"OtherworldActions","Hit")||Calls(c,"OtherworldActions","Status")),"skill reception never reapplies damage/status");
        var capture=Body(Mod("KamuiWorld"),"Capture");
        foreach(string seam in new[]{"Temporary","Identify","RetainPending"})Check(capture.Any(c=>Calls(c,"KamuiPrefabs",seam)),"snapshot wiring: "+seam);
        foreach(string field in new[]{"Bronze","Lifetime","TimedLifetime","DestroyLifetime"})Check(capture.Any(c=>Field(c,field)),"snapshot preserves "+field);
        var restore=Mod("KamuiWorld").GetNestedTypes(BindingFlags.NonPublic).Single(t=>t.Name.StartsWith("<RestoreRoutine>"));
        var restoreCode=Body(restore,"MoveNext");
        Check(restoreCode.Any(c=>Calls(c,"KamuiPrefabs","Resolve"))&&!restoreCode.Any(c=>Calls(c,"dw单位库","获取单位")),"restoration resolves prefab identity instead of gameplay ID");
        Check(restoreCode.Any(c=>Calls(c,"Plugin","Warn")),"unresolved snapshot entries are reported");
        Check(restoreCode.Any(c=>c.opcode==OpCodes.Stfld&&Field(c,"HostDisplay")),"restored authority-only states retain guest feedback");
        var stateType=native.GetType("模块_状态属性+状态类",true);
        Check(Body(stateType,"Clone").Any(c=>Calls(c,"Object","MemberwiseClone")),"native state cloning preserves derived feedback metadata");
        Check(Body(Mod("OtherworldStatusApplication"),"Prefix").Any(c=>Calls(c,"HostStatusFeedback","RequiresDisplay")),"state application records guest-missing provenance");
        var actions=Mod("OtherworldActions");
        foreach(string method in new[]{"ApplyBombard","Normal","Counter"})
        {
            var code=Body(actions,method);Check(code.Any(c=>c.operand is ConstructorInfo ctor&&ctor.DeclaringType.Name=="Batch"),"skill collects native results: "+method);
            Check(code.FindLastIndex(c=>Calls(c,"OtherworldNetwork","Effect")||Calls(c,"OtherworldNetwork","SendDisplay"))>code.FindIndex(c=>Calls(c,"OtherworldActions","Hit")),"skill sends after actual settlement: "+method);
        }
        foreach(string routine in new[]{"Flame","OrbitalImpact"})
        {
            var state=actions.GetNestedTypes(BindingFlags.NonPublic).Single(t=>t.Name.StartsWith("<"+routine+">"));
            var code=Body(state,"MoveNext");
            Check(code.Any(c=>Calls(c,"UnitActionLifetime","Current")),"delayed skill retains action lease: "+routine);
            Check(code.Any(c=>Calls(c,"OtherworldNetwork","Effect")||Calls(c,"OtherworldActions","ApplyBombard")),"result path is inside actual delayed impact: "+routine);
        }
        // Ordinary local-hit paths do not broadcast results.
        foreach(string type in new[]{"OtherworldDirectDamage","OtherworldHitDisplay","OtherworldNormalHit"})
        foreach(var method in Mod(type).GetMethods(BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.DeclaredOnly))
            Check(!PatchProcessor.GetOriginalInstructions(method).Any(c=>Calls(c,"OtherworldNetwork","Send")||Calls(c,"OtherworldNetwork","SendDisplay")),"ordinary hit network budget: "+type+"."+method.Name);
        Console.WriteLine($"PASS: {checks} actual native/compiled repair bindings, display gates, authority, snapshot and skill timing checks");
    }
}
