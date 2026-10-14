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
