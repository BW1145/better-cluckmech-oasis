using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

internal static class Update0620Checks
{
    internal static void Run(Assembly native,Assembly mod,Action<List<CodeInstruction>> stack)
    {
        int count=0;
        void Check(bool value,string text){count++;if(!value)throw new Exception(text);}
        Type Mod(string name)=>mod.GetType("CluckmechCoop."+name,true);
        List<CodeInstruction> Body(Type t,string name)=>PatchProcessor.GetOriginalInstructions(AccessTools.DeclaredMethod(t,name));
        bool Calls(CodeInstruction c,string type,string name)=>c.operand is MethodInfo m&&m.DeclaringType.Name==type&&m.Name==name;
        foreach(var binding in new[]{("模块_伤害精灵","OnTriggerEnter2D","InitializeProjectileContact"),("栏位单元","Update","StratagemSlotCost")})
        {
            var before=Body(native.GetType(binding.Item1,true),binding.Item2);
            var after=((IEnumerable<CodeInstruction>)AccessTools.Method(Mod(binding.Item3),"Transpiler").Invoke(null,new object[]{before})).ToList();
            stack(after);Check(true,"updated native IL stack: "+binding.Item3);
            if(binding.Item3=="InitializeProjectileContact")
            {
                Check(after.Count(c=>Calls(c,binding.Item3,"Deliver"))==1,"native contact has exactly one safe child-payload delivery");
                Check(after.Count(c=>Calls(c,binding.Item3,"Stats"))==2,"both native collider and attacker-stat lookups are retained through a lifetime-safe lookup");
                Check(after.Count(c=>Calls(c,"模块_伤害精灵","死亡"))==before.Count(c=>Calls(c,"模块_伤害精灵","死亡")),"contact preserves the original projectile lifetime decision");
            }
            else Check(after.Count(c=>Calls(c,binding.Item3,"Cost"))==1,"slot cost, color and drag use the effective cost");
        }
        var stats=Mod("PreserveGuestFinalStats");
        var targets=((IEnumerable<MethodBase>)AccessTools.Method(stats,"TargetMethods").Invoke(null,null)).ToArray();
        Check(targets.Length==8&&targets.All(m=>m!=null),"eight final-stat recalculation entrypoints bind the installed game");
        Check(AccessTools.Method(stats,"Prefix").ReturnType==typeof(void),"attribute snapshots keep Harmony prefix/postfix scopes paired");
        foreach(var method in targets)
        {
            var generator=new DynamicMethod("stat_guard",typeof(void),Type.EmptyTypes).GetILGenerator();
            var before=PatchProcessor.GetOriginalInstructions(method);
            var after=((IEnumerable<CodeInstruction>)AccessTools.Method(stats,"Transpiler").Invoke(null,new object[]{before,generator})).ToList();
            stack(after);
            Check(after.Count==before.Count+4&&Calls(after[1],"CoopStatAuthority","Calculate")&&after[3].opcode==OpCodes.Ret,"guest bypasses only the native calculation body: "+method.Name);
        }
        foreach(string name in new[]{"BindNativeSyncReferences","InitializeDamageReceivers"})
        {
            var patch=Mod(name);
            foreach(var target in (IEnumerable<MethodBase>)AccessTools.Method(patch,"TargetMethods").Invoke(null,null))
            foreach(var parameter in AccessTools.Method(patch,"Prefix").GetParameters().Where(p=>p.Name.StartsWith("___")))
                Check(AccessTools.Field(target.DeclaringType,parameter.Name.Substring(3))!=null,"native reference binding: "+name+parameter.Name);
        }
        Check(Body(Mod("GuestNativePacketDisplay"),"Prefix").Any(c=>Calls(c,"OtherworldHitDisplay","NativeLocal")),"ordinary guest packets retain local damage presentation");
        foreach(string name in new[]{"NativeLocal","NativeScalar"})
            Check(!Body(Mod("OtherworldHitDisplay"),name).Any(c=>c.operand is MethodInfo m&&(m.Name=="受到伤害"||m.Name=="实现伤害")),"guest presentation has no real damage call: "+name);
        var scenery=Body(Mod("NativeCombatTargets"),"Scenery");
        Check(scenery.Count(c=>c.operand is MethodInfo m&&m.Name=="OverlapCircleAll")==1&&scenery.Any(c=>c.operand is MethodInfo m&&m.Name=="TryGetValue"),"scene targets use one overlap query and deduplicate native roots");
        Check(Body(Mod("NativeCombatTargets"),"CanDamage").Any(c=>Calls(c,"阵营数据库","运算是否可伤害")),"scene targeting uses the native damage relationship");
        Check(Body(Mod("StratagemDamage"),"EagleMinerals").Any(c=>Calls(c,"StratagemParameters","EagleDamage")),"scene targets retain Eagle distance falloff");
        Check(Body(Mod("StratagemDamage"),"Area").Any(c=>Calls(c,"NativeCombatTargets","Scenery")),"other stratagem area damage shares scene-target handling");
        Check(Body(Mod("ShopWheelGraphic"),"OnPopulateMesh").Any(c=>Calls(c,"ShopRewardRules","SectorStart"))&&Body(Mod("ShopRewardsView"),"Wheel").Any(c=>Calls(c,"ShopRewardRules","SectorStart")),"wheel geometry and labels share probability-based sector boundaries");
        Console.WriteLine($"PASS: {count} installed-game stat, lifecycle, targeting, deployment and wheel binding checks");
    }
}
