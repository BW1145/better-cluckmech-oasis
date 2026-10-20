using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

string root=Path.GetFullPath(args[0]),game=Path.GetFullPath(args[1]);
string managed=Path.Combine(game,"Cluckmech Oasis_Data","Managed");
string core=Path.Combine(root,"dependencies","BepInEx","BepInEx","core");
AppDomain.CurrentDomain.AssemblyResolve+=(sender,eventArgs)=>
{
    string name=new AssemblyName(eventArgs.Name).Name+".dll";
    foreach(string directory in new[]{managed,core,Path.Combine(root,"src/bin/Release/net472")})
    {string path=Path.Combine(directory,name);if(File.Exists(path))return Assembly.LoadFrom(path);}
    return null;
};
var original=Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));
var mod=Assembly.LoadFrom(Path.Combine(root,"src/bin/Release/net472/CluckmechCoop.dll"));
int checks=0;
try{
if(args.Length>2 && args[2]=="--perlica-hits")
{
    TranspilerChecks.PerlicaHits(original,mod,ValidateStack);
    return;
}
if(args.Length>2 && args[2]=="--obito-balance")
{
    var catalog=mod.GetType("CluckmechCoop.OtherworldCatalog",true);
    var hero=AccessTools.Method(catalog,"Hero").Invoke(null,new object[]{13});
    foreach(var stat in new[]{("Attack",70f),("Health",750f),("Rate",2.5f),("Range",5f),("Speed",4.5f)})
        if((float)AccessTools.Field(hero.GetType(),stat.Item1).GetValue(hero)!=stat.Item2)throw new Exception("Obito stat mismatch: "+stat.Item1);
    var run=Activator.CreateInstance(mod.GetType("CluckmechCoop.OtherworldRun",true),true);
    var upgrades=AccessTools.Field(run.GetType(),"Upgrades");
    var bonus=AccessTools.Property(run.GetType(),"BurnBonus");
    foreach(var sample in new[]{(0,.5f),(10,1.2f),(20,1.9f),(30,2.6f)})
    {
        upgrades.SetValue(run,sample.Item1);
        if(Math.Abs((float)bonus.GetValue(run)-sample.Item2)>.00001f)throw new Exception("Fire growth mismatch");
    }
    ((HashSet<int>)AccessTools.Field(run.GetType(),"Talents").GetValue(run)).Add(26118);
    upgrades.SetValue(run,20);
    if(Math.Abs((float)bonus.GetValue(run)-2.5f)>.00001f)throw new Exception("Ember growth mismatch");
    Console.WriteLine("PASS: Obito base stats, 0/10/20/30 reinforcement fire growth and Ember override in final DLL");return;
}
if(args.Length>2 && args[2]=="--bronze-minerals")
{
    TranspilerChecks.BronzeMinerals(original,mod,ValidateStack);
    return;
}
if(args.Length>2 && args[2]=="--flame-trigger")
{
    TranspilerChecks.FlameTrigger(mod,ValidateStack);
    return;
}
if(args.Length>2 && args[2]=="--settlement-repairs")
{
    TranspilerChecks.SettlementRepairs(original,mod,ValidateStack);
    return;
}
if(args.Length>2 && args[2]=="--energy-audio")
{
    TranspilerChecks.EnergyAudio(original,mod);
    return;
}
if(args.Length>2 && args[2]=="--dash-chain")
{
    TranspilerChecks.DashChain(mod);
    return;
}
if(args.Length>2 && args[2]=="--shop")
{
    TranspilerChecks.Shop(original,mod,ValidateStack);
    return;
}
if(args.Length>2 && args[2]=="--transpilers")
{
    TranspilerChecks.Run(original,mod,ValidateStack);
    return;
}
if(args.Length>2 && args[2]=="--otherworld")
{
    foreach(var type in mod.GetTypes().Where(t=>t.Name.StartsWith("Otherworld")||t.Name.StartsWith("Kamui")))
    {
        if(!type.GetCustomAttributes(typeof(HarmonyPatch),false).Any())continue;
        var targetList=AccessTools.DeclaredMethod(type,"TargetMethods");
        if(targetList!=null)
        {
            foreach(var method in (IEnumerable<MethodBase>)targetList.Invoke(null,null))
            {if(method==null)throw new Exception("Missing target: "+type.Name);checks++;}
            continue;
        }
        var target=AccessTools.DeclaredMethod(type,"TargetMethod");
        if(target!=null){if(target.Invoke(null,null)==null)throw new Exception("Missing target: "+type.Name);checks++;continue;}
        var info=new HarmonyMethod();
        foreach(HarmonyPatch attribute in type.GetCustomAttributes(typeof(HarmonyPatch),false))
        {
            if(attribute.info.declaringType!=null)info.declaringType=attribute.info.declaringType;
            if(attribute.info.methodName!=null)info.methodName=attribute.info.methodName;
            if(attribute.info.argumentTypes!=null)info.argumentTypes=attribute.info.argumentTypes;
        }
        if(info.declaringType==null||AccessTools.Method(info.declaringType,info.methodName,info.argumentTypes)==null)throw new Exception("Missing binding: "+type.Name);checks++;
    }
    foreach(var binding in new[]{("模块_攻击","检索目标","KamuiTargetCandidates"),("模块_玩家_攻击","检索目标","KamuiTargetCandidates"),
        ("模块_光环","检索光环内敌方","KamuiAuraCandidates"),("模块_光环","检索光环内友方","KamuiAuraCandidates"),
        ("模块_基本属性","运算生命值上限","OtherworldHealth"),("模块_攻击属性","运算攻击力","OtherworldAttackStats"),
        ("模块_攻击属性","运算攻击速度","OtherworldAttackRate"),
        ("建造系统","开始拖动","OtherworldWorkshopPower"),("建造系统","结束拖动延迟","OtherworldWorkshopPower")})
    {
        var method=AccessTools.Method(original.GetType(binding.Item1,true),binding.Item2);
        var type=mod.GetType("CluckmechCoop."+binding.Item3,true);
        var patch=AccessTools.Method(type,"Transpiler");
        var before=PatchProcessor.GetOriginalInstructions(method);
        var after=((IEnumerable<CodeInstruction>)patch.Invoke(null,patch.GetParameters().Length==2?new object[]{before,method}:new object[]{before})).ToList();
        if(after.Count==before.Count&&after.Zip(before,(a,b)=>a.opcode==b.opcode&&Equals(a.operand,b.operand)).All(x=>x))throw new Exception("No replacement in "+binding);
        ValidateStack(after);checks++;
    }
    var catalog=mod.GetType("CluckmechCoop.OtherworldCatalog",true);
    var level=AccessTools.Method(catalog,"ResearchLevel");
    foreach(var example in new[]{(0,0),(9,0),(10,1),(29,1),(30,2),(60,3),(100,4),(550,10),(2100,20)})
    {if((int)level.Invoke(null,new object[]{example.Item1})!=example.Item2)throw new Exception("Research progression mismatch");checks++;}
    Console.WriteLine($"PASS: {checks} Otherworld native bindings, retained target selection IL and research thresholds");return;
}
if(args.Length>2 && args[2]=="--particle-init")
{
    var begin=AccessTools.Method(mod.GetType("CluckmechCoop.StratagemNativeParticles",true),"Begin");
    var code=PatchProcessor.GetOriginalInstructions(begin);
    int lookup=code.FindIndex(x=>x.operand is MethodInfo m&&m.Name=="GetComponentInChildren"&&m.IsGenericMethod&&m.GetGenericArguments()[0].Name=="SpriteRenderer");
    if(lookup<1||code[lookup-1].opcode!=OpCodes.Ldc_I4_1)throw new Exception("Particle sorting must use an inactive child renderer; native tower root has none");
    checks++;ValidateStack(code);checks++;
    Console.WriteLine($"PASS: {checks} particle initialization child-renderer and IL checks");return;
}
if(args.Length>2 && args[2]=="--native-repairs")
{
    foreach(var binding in new[]{
        ("模块_随从状态机","Start","敌人身体"),
        ("UI_突变模式信息显示","Start","数据库"),
        ("UI_突变模式信息显示","Start","引用器"),
        ("UI_突变模式信息显示","Start","全局存档")})
    {
        var type=original.GetType(binding.Item1,true);
        if(AccessTools.Method(type,binding.Item2)==null || AccessTools.Field(type,binding.Item3)==null)throw new Exception("Repair field binding missing: "+binding);
        checks++;
    }
    foreach(var binding in new[]{
        ("模块_状态持续效果","状态持续","RepairDamageNotifications",3),
        ("模块_受伤","受到伤害","RepairDamageNotifications",1),
        ("词条_100067_定时出现火山岩浆气泡","Start","RepairLavaInitialization",1)})
    {
        var type=original.GetType(binding.Item1,true);
        var method=binding.Item1=="模块_受伤"?AccessTools.Method(type,binding.Item2,new[]{original.GetType("世界类+伤害数据包",true)}):AccessTools.Method(type,binding.Item2);
        var before=PatchProcessor.GetOriginalInstructions(method);
        var patch=AccessTools.Method(mod.GetType("CluckmechCoop."+binding.Item3,true),"Transpiler");
        var after=((IEnumerable<CodeInstruction>)patch.Invoke(null,new object[]{before})).ToList();
        string helper=binding.Item3=="RepairDamageNotifications"?"Send":"Register";
        if(after.Count(x=>x.operand is MethodInfo call && call.DeclaringType.Assembly==mod && call.Name==helper)!=binding.Item4)throw new Exception("Native repair replacement mismatch: "+binding);
        checks++;ValidateStack(after);checks++;
        foreach(string retained in binding.Item1=="词条_100067_定时出现火山岩浆气泡"?new[]{"Find","GetComponent"}:new[]{"实现伤害","伤害出现","伤害统计","Add","查询状态"})
            if(before.Count(x=>x.operand is MethodInfo call&&call.Name==retained)!=after.Count(x=>x.operand is MethodInfo call&&call.Name==retained))throw new Exception("Native effect changed: "+retained);
        checks++;
    }
    foreach(string name in new[]{"StaticTurretAnimation","RewardSceneContext","RepairRewardArrows","MenuBattleBonuses","MenuBattleControls"})
        foreach(var method in (IEnumerable<MethodBase>)AccessTools.Method(mod.GetType("CluckmechCoop."+name,true),"TargetMethods").Invoke(null,null))
        {if(method==null)throw new Exception("Repair target missing: "+name);checks++;}
    var settings=mod.GetType("CluckmechCoop.NativeRepairs",true);
    AccessTools.Field(settings,"Enabled").SetValue(null,true);
    var lava=AccessTools.Method(mod.GetType("CluckmechCoop.RepairLavaInitialization",true),"Register");
    lava.Invoke(null,new object[]{null,Activator.CreateInstance(original.GetType("世界类+词条数据包",true))});checks++;
    Console.WriteLine($"PASS: {checks} repair bindings, native damage/terrain IL and optional terrain registration checks");return;
}
if(args.Length>2 && args[2]=="--shop-stock")
{
    var method=AccessTools.Method(original.GetType("商店脚本",true),"商店商品运算");
    var patchType=mod.GetType("CluckmechCoop.NewShopStock",true);
    var instructions=PatchProcessor.GetOriginalInstructions(method);
    var changed=((IEnumerable<CodeInstruction>)AccessTools.Method(patchType,"Transpiler").Invoke(null,new object[]{instructions})).ToList();
    if(changed.Count(x=>x.operand is MethodInfo call&&call.Name=="SpecialOfferCount")!=1)throw new Exception("Shop candidate filter binding mismatch");
    checks++;ValidateStack(changed);checks++;
    foreach(string nativeCall in new[]{"Range","商店_同步商店栏位","保存单局存档"})
        if(instructions.Count(x=>x.operand is MethodInfo call&&call.Name==nativeCall)!=changed.Count(x=>x.operand is MethodInfo call&&call.Name==nativeCall))throw new Exception("Native shop draw or persistence changed");
    checks++;
    var shop=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(method.DeclaringType);
    var stockField=AccessTools.Field(method.DeclaringType,"当前商品");
    var vectorType=stockField.FieldType.GetElementType();
    var moduleType=mod.GetType("CluckmechCoop.ModuleSettings",true);
    var modules=AccessTools.Field(moduleType,"Extra").GetValue(null);
    var enabled=AccessTools.Field(modules.GetType(),"Shop");
    var filter=AccessTools.Method(patchType,"SpecialOfferCount");
    foreach(int slots in new[]{3,4})
    {
        var stock=Array.CreateInstance(vectorType,slots);
        stock.SetValue(Activator.CreateInstance(vectorType,new object[]{7,100,0}),1);stockField.SetValue(shop,stock);
        enabled.SetValue(modules,true);var candidates=new List<int>{5,7,9};
        if((int)filter.Invoke(null,new object[]{candidates,shop})!=2||!candidates.SequenceEqual(new[]{5,9}))throw new Exception("Preferred machine repeated in special candidate pool");checks++;
    }
    enabled.SetValue(modules,false);var untouched=new List<int>{5,7,9};
    if((int)filter.Invoke(null,new object[]{untouched,shop})!=3||untouched.Count!=3)throw new Exception("Disabled module changed native candidates");checks++;
    Console.WriteLine($"PASS: {checks} shop selection, native IL stack and retained pricing/persistence checks");return;
}
foreach(var binding in new[]{
    ("词条_100159_肚子鳄","吞噬触发","SwallowExecution"),
    ("词条_100176_水母骑士","吞噬触发","SwallowExecution"),
    ("奖励结算脚本","随机词条结果运算","FilterStratagemUpgrades")})
{
    var method=AccessTools.Method(original.GetType(binding.Item1,true),binding.Item2);
    var patch=AccessTools.Method(mod.GetType("CluckmechCoop."+binding.Item3,true),"Transpiler");
    var instructions=PatchProcessor.GetOriginalInstructions(method);
    if(!instructions.Any(x=>x.operand is MethodInfo call&&call.Name==(binding.Item3=="SwallowExecution"?"FindAll":"获取单位词缀列表")))throw new Exception("Selection call missing");
    checks++;
    var changed=((IEnumerable<CodeInstruction>)patch.Invoke(null,new object[]{instructions})).ToList();
    string name=binding.Item3=="SwallowExecution"?"Targets":"Candidates";
    if(changed.Count(x=>x.operand is MethodInfo call&&call.Name==name)!=1)throw new Exception("Replacement count mismatch");
    checks++;
    ValidateStack(changed);
    checks++;
    if(binding.Item3=="SwallowExecution")
        foreach(string effect in new[]{"技能实现","吞噬效果实现"})
            if(instructions.Count(x=>x.operand is MethodInfo call&&call.Name==effect)!=changed.Count(x=>x.operand is MethodInfo call&&call.Name==effect))throw new Exception("Native benefit/effect call changed");
    checks++;
}
Console.WriteLine($"PASS: {checks} current-game selection bindings, rewritten IL stacks and retained native effects");
}catch(Exception error){for(var e=error;e!=null;e=e.InnerException)Console.WriteLine(e.GetType().FullName+": "+e.Message);Environment.Exit(1);}

static void ValidateStack(List<CodeInstruction> code)
{
    var labels=new Dictionary<Label,int>();
    for(int i=0;i<code.Count;i++)foreach(var label in code[i].labels)labels[label]=i;
    var heights=new Dictionary<int,int>();var queue=new Queue<(int,int)>();queue.Enqueue((0,0));
    while(queue.Count>0)
    {
        var entry=queue.Dequeue();int index=entry.Item1,height=entry.Item2;
        if(index>=code.Count)throw new Exception("Instruction path escapes body");
        if(heights.TryGetValue(index,out int prior)){if(prior!=height)throw new Exception("Stack heights disagree at branch join");continue;}
        heights.Add(index,height);var instruction=code[index];var op=instruction.opcode;
        int pop,push;
        if(op.StackBehaviourPop==StackBehaviour.Varpop)
        {
            if(op==OpCodes.Ret)pop=0;
            else if(instruction.operand is MethodBase method)pop=method.GetParameters().Length+(op!=OpCodes.Newobj&&!method.IsStatic?1:0);
            else throw new Exception("Unsupported variable stack operation");
        }
        else pop=op.StackBehaviourPop==StackBehaviour.Pop0?0:op.StackBehaviourPop.ToString().Split('_').Length;
        if(op.StackBehaviourPush==StackBehaviour.Varpush)
            push=op==OpCodes.Newobj||instruction.operand is MethodInfo info&&info.ReturnType!=typeof(void)?1:0;
        else push=op.StackBehaviourPush==StackBehaviour.Push0?0:op.StackBehaviourPush.ToString().Split('_').Length;
        if(height<pop)throw new Exception("Rewritten method underflows evaluation stack");
        height=height-pop+push;
        if(op.FlowControl==FlowControl.Return||op.FlowControl==FlowControl.Throw)
        {if(height!=0)throw new Exception("Method exits with a nonempty stack");continue;}
        if(instruction.operand is Label target)queue.Enqueue((labels[target],op==OpCodes.Leave||op==OpCodes.Leave_S?0:height));
        if(instruction.operand is Label[] targets)foreach(var targetLabel in targets)queue.Enqueue((labels[targetLabel],height));
        if(op.FlowControl!=FlowControl.Branch)queue.Enqueue((index+1,height));
    }
}
