using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CluckmechCoop;
using HarmonyLib;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

static class ClosureEntry
{
    static void Main()
    {
        // Preserve and run the existing stratagem suite before the focused additions.
        typeof(Program).GetMethod("Main",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,null);
        HostFeedbackTests.Run();
    }
}
static class HostFeedbackTests
{
    static int checks;
    static void Check(bool pass,string name){checks++;if(!pass)throw new Exception(name);Console.WriteLine("PASS: "+name);}
    static void Reset(bool stratagems=true,bool otherworld=true)
    {
        ModuleSettings.Extra.Stratagems=false;OtherworldWorld.Active=false;Plugin.Instance.Frame();
        Plugin.Instance.Pending.Clear();OtherworldWorld.Units.Clear();OtherworldProgress.Runs.Clear();OtherworldDamage.ApplyingSpace=null;
        NetworkClient.spawned.Clear();OtherworldNetwork.Sent.Clear();StratagemWorld.Ids.Clear();OtherworldActions.Viewer=null;StratagemWorld.NativeViewer=null;
        Steam联机管理器.玩家_客户端=new(){connectionToClient=new(){connectionId=2}};
        SceneLifecycle.Current=new();Plugin.Combat.Token=Guid.NewGuid().ToString("N");Time.realtimeSinceStartup=0;
        Plugin.Role=1;Plugin.InCombat=true;Plugin.Network.PeerReady=true;ModuleSettings.Extra.Stratagems=stratagems;OtherworldWorld.Active=otherworld;OtherworldRegistry.Enabled=otherworld;
    }
    static 模块_受伤 Target(uint id=1,int space=0)
    {
        var obj=new GameObject();var hp=obj.Add(new 模块_基本属性());var hurt=obj.Add(new 模块_受伤{基本属性=hp});
        obj.Add(new 模块_状态属性());NetworkClient.spawned[id]=obj.Add(new NetworkIdentity());StratagemWorld.Ids[obj]=id;
        OtherworldWorld.Units[obj]=obj.Add(new OtherworldUnit{Health=hp,Space=space});return hurt;
    }
    static GameObject Attacker(bool special=true,int space=0)
    {
        var obj=new GameObject();var hp=obj.Add(new 模块_基本属性());
        OtherworldWorld.Units[obj]=obj.Add(new OtherworldUnit{Health=hp,Space=space});
        if(special)OtherworldProgress.Runs[obj]=new OtherworldRun{Initial=26005};return obj;
    }
    static void Viewer(int space)
    {
        var obj=new GameObject();var hp=obj.Add(new 模块_基本属性());
        OtherworldWorld.Units[obj]=obj.Add(new OtherworldUnit{Health=hp,Space=space});
        OtherworldActions.Viewer=obj.Add(new OtherworldActor());StratagemWorld.NativeViewer=hp;
    }
    static 模块_状态属性.状态类 State(GameObject source,float damage=10)=>new(){攻击者=source,状态类型=攻击类型与状态数据库.状态枚举.灼烧,剩余时间=2,
        加成属性=new(){默认持续时间=2,当前持续时间=2,默认属性效果=new Vector4(damage,0,0,0),当前属性效果=new Vector4(damage,0,0,0)}};
    static 模块_状态属性.状态类 Apply(模块_受伤 target,模块_状态属性.状态类 incoming,out bool accepted)
    {
        var module=target.GetComponent<模块_状态属性>();var args=new object[]{module,incoming,0};
        accepted=(bool)typeof(OtherworldStatusApplication).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,args)!;
        if(!accepted)return incoming;
        // Matches the verified native Clone's MemberwiseClone and subsequent List.Add.
        var installed=(模块_状态属性.状态类)((模块_状态属性.状态类)args[1]).Clone();module.当前状态列表.Add(installed);return installed;
    }
    static HostStatusFeedback.Scope Exclusive()
    {
        var args=new object[]{default(HostStatusFeedback.Scope)};
        typeof(HostOnlyStatusApplication).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,args);
        return (HostStatusFeedback.Scope)args[0];
    }
    static void EndExclusive(HostStatusFeedback.Scope scope,Exception error=null)
    {
        var returned=typeof(HostOnlyStatusApplication).GetMethod("Finalizer",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,new object[]{error,scope});
        Check(ReferenceEquals(returned,error),"exclusive scope preserves original exception");
    }
    static void Tick(模块_受伤 target,int color,params 模块_状态属性.状态类[] states)
    {
        using(StratagemHitDisplay.CaptureStatus(target.gameObject))
        {
            StratagemHitDisplay.BeginStatusSum(target.gameObject);float total=0;
            foreach(var state in states)total+=StratagemHitDisplay.StateValue(state);
            target.ShowColored(total*.5f,color);
        }
    }
    static void Advance(){Time.realtimeSinceStartup+=.11f;Plugin.Instance.Frame();}
    static List<OtherworldDisplayHit> Results()=>OtherworldNetwork.Sent.SelectMany(m=>JsonConvert.DeserializeObject<List<OtherworldDisplayHit>>(m.Data)).ToList();
    static void StatusProvenance()
    {
        foreach(bool stratagems in new[]{false,true})foreach(int color in new[]{1,2,3})
        {
            Reset(stratagems);var target=Target();var source=Attacker();var original=State(source,8);
            OtherworldDamage.ApplyingSpace=0;var installed=Apply(target,original,out var accepted);OtherworldDamage.ApplyingSpace=null;
            Check(accepted&&installed is OtherworldState tagged&&tagged.HostDisplay,"protected character/owned-machine packet marks its native state clone");
            Check(original is not OtherworldState&&original.加成属性.默认持续时间==2&&installed.加成属性.默认持续时间==4,"input packet untouched; native burn-duration bonus unchanged");
            var local=Apply(target,State(source,12),out accepted);
            Check(local is OtherworldState localTagged&&!localTagged.HostDisplay,"same attacker outside a suppressed packet is not assumed host-only");
            int stateCount=target.GetComponent<模块_状态属性>().当前状态列表.Count;float hp=target.基本属性.当前生命值;
            Tick(target,color,installed,local);Advance();var hits=Results();
            Check(hits.Count==1&&hits[0].Amount==4&&hits[0].Color==color,"relay missing DoT share only, with color, stratagems="+stratagems);
            int before=target.Visuals;Plugin.Role=2;
            foreach(var sent in OtherworldNetwork.Sent){var message=sent;message.Token=Plugin.Combat.Token;OtherworldNetwork.Route(message);}
            Check(target.Visuals==before+1&&target.LastVisual==4,"Otherworld-only guest receives remaining burn/poison/bleed feedback");
            Check(target.基本属性.当前生命值==hp&&target.战斗事件.Events==0&&target.GetComponent<模块_状态属性>().当前状态列表.Count==stateCount,"DoT feedback does not alter health, events or states");
        }
        Reset(false);var t=Target();var a=Attacker(false);OtherworldDamage.ApplyingSpace=0;
        var state=Apply(t,State(a),out var ok);OtherworldDamage.ApplyingSpace=null;
        Check(ok&&state is not OtherworldState,"ordinary main-world packet retains native state identity");
        Tick(t,1,state);Advance();Check(OtherworldNetwork.Sent.Count==0,"ordinary mirrored DoT is not retransmitted");
        var outer=Exclusive();var inner=Exclusive();state=Apply(t,State(a),out ok);EndExclusive(inner);EndExclusive(outer,new InvalidOperationException("fixture"));
        Check(state is OtherworldState hostOnly&&hostOnly.HostDisplay,"host-exclusive skill status is marked even without a direct damage scope");
        Tick(t,1,state);Advance();Check(Results().Count==1,"flame/skill remaining state ticks continue outside the original skill batch");
        var after=Apply(t,State(a),out ok);Check(after is not OtherworldState,"exception/nested scope cleanup does not tag later native effects");
        Reset(false);t=Target();a=Attacker();OtherworldDamage.ApplyingSpace=0;state=Apply(t,State(a),out ok);OtherworldDamage.ApplyingSpace=null;
        state.攻击者=null;OtherworldProgress.Runs.Clear();var copy=OtherworldStates.Copy(state,1,1);
        Check(copy.HostDisplay&&copy.加成属性.默认持续时间==1,"feedback survives state copy and destroyed/missing attacker");
        Tick(t,1,copy);Advance();Check(Results().Count==1,"source destruction does not lose later DoT feedback");
        Reset(false);Viewer(3);t=Target(space:3);a=Attacker(false,3);OtherworldDamage.ApplyingSpace=3;
        state=Apply(t,State(a),out ok);OtherworldDamage.ApplyingSpace=null;
        Check(state is OtherworldState privateState&&privateState.HostDisplay,"private-space enemy/ordinary packet matches existing guest guard");
        Tick(t,1,state);Advance();Check(Results().Count==1,"guest's own private-space state feedback retained");
        Reset(false);t=Target(space:3);a=Attacker(true,0);OtherworldDamage.ApplyingSpace=0;Apply(t,State(a),out ok);
        Check(!ok,"state source-space rejection remains unchanged");
        OtherworldDamage.ApplyingSpace=null;
        foreach(int role in new[]{0,2})
        {
            Reset(false);Plugin.Role=role;t=Target();a=Attacker();OtherworldDamage.ApplyingSpace=0;state=Apply(t,State(a),out ok);
            Check(state is OtherworldState noRelay&&!noRelay.HostDisplay,"single-player/client does not request authoritative replay");
        }
        Reset(false);t=Target();var restored=RestoreFeedbackProbe.Restore(new RestoredStateSample{Type=(int)攻击类型与状态数据库.状态枚举.灼烧,Remaining=1.25f},OtherworldWorld.Unit(t.gameObject),null,-1);
        Check(restored.HostDisplay&&restored.剩余时间==1.25f,"old save initializer marks authority-only restored states without a schema migration");
        Plugin.Role=0;restored=RestoreFeedbackProbe.Restore(new(),OtherworldWorld.Unit(t.gameObject),null,-1);Check(!restored.HostDisplay,"single-player restore does not enable feedback transport");
        Reset(false);t=Target();a=Attacker(false);var reused=State(a);var scope=Exclusive();state=Apply(t,reused,out ok);EndExclusive(scope);
        var second=Apply(t,reused,out ok);Check(reused is not OtherworldState&&second is not OtherworldState,"marking one target does not contaminate a reused source state");
    }
    static void Visibility()
    {
        foreach(int guest in new[]{0,2,3})foreach(int targetSpace in new[]{0,2,3})foreach(bool observer in new[]{false,true})
        {
            Reset();Viewer(guest);var target=Target(space:targetSpace);var identity=target.GetComponent<NetworkIdentity>();if(!observer)identity.observers.Clear();
            using(StratagemHitDisplay.Capture())target.Show(10,false);
            bool visible=guest==targetSpace&&observer;
            Check(Plugin.Instance.Pending.Count==(visible?1:0),"record-time visibility starts no work for invisible hits");
            Advance();Check(Results().Count==(visible?1:0),"host/guest private spaces and observers: "+guest+"/"+targetSpace+"/"+observer);
        }
        foreach(string reason in new[]{"viewer","target","observer","connection","destroy","hidden"})
        {
            Reset();Viewer(0);var target=Target();using(StratagemHitDisplay.Capture())target.Show(20,false);
            var identity=target.GetComponent<NetworkIdentity>();
            switch(reason)
            {
                case "viewer":Viewer(3);break;
                case "target":OtherworldWorld.Unit(target.gameObject).Space=3;break;
                case "observer":identity.observers.Clear();break;
                case "connection":Steam联机管理器.玩家_客户端=new(){connectionToClient=new(){connectionId=2}};break;
                case "destroy":NetworkServer.spawned.Remove(identity.netId);break;
                case "hidden":target.gameObject.activeInHierarchy=false;break;
            }
            Advance();Check(OtherworldNetwork.Sent.Count==0&&Plugin.Instance.Pending.Count==0,"flush rechecks visibility and releases work: "+reason);
        }
        Reset();Viewer(0);var privateTarget=Target(space:2);
        using(StratagemHitDisplay.Capture())for(int i=0;i<5000;i++)privateTarget.Show(10,false);
        Check(OtherworldNetwork.Sent.Count==0&&Plugin.Instance.Pending.Count==0,"5000 host-private hits allocate no sender and emit zero messages");
        Reset(false);Viewer(0);privateTarget=Target(space:2);Tick(privateTarget,1,new OtherworldState{HostDisplay=true,加成属性=new(){当前属性效果=new Vector4(10,0,0,0)}});Advance();
        Check(OtherworldNetwork.Sent.Count==0&&Plugin.Instance.Pending.Count==0,"invisible authoritative DoT uses the same zero-traffic filter");
    }
    static List<CodeInstruction> Instructions(MethodInfo method)
    {
        var opcodes=typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static).Where(f=>f.FieldType==typeof(OpCode)).Select(f=>(OpCode)f.GetValue(null)!).ToDictionary(o=>unchecked((ushort)o.Value));
        var bytes=method.GetMethodBody()!.GetILAsByteArray()!;var code=new List<CodeInstruction>();int p=0;
        while(p<bytes.Length)
        {
            ushort value=bytes[p++];if(value==0xfe)value=(ushort)(0xfe00|bytes[p++]);var op=opcodes[value];object operand=null;
            switch(op.OperandType)
            {
                case OperandType.InlineNone:break;
                case OperandType.ShortInlineI:case OperandType.ShortInlineVar:case OperandType.ShortInlineBrTarget:operand=bytes[p++];break;
                case OperandType.InlineVar:operand=BitConverter.ToUInt16(bytes,p);p+=2;break;
                case OperandType.InlineI8:case OperandType.InlineR:operand=BitConverter.ToInt64(bytes,p);p+=8;break;
                case OperandType.ShortInlineR:operand=BitConverter.ToSingle(bytes,p);p+=4;break;
                case OperandType.InlineField:case OperandType.InlineMethod:case OperandType.InlineType:case OperandType.InlineTok:operand=method.Module.ResolveMember(BitConverter.ToInt32(bytes,p));p+=4;break;
                case OperandType.InlineString:operand=method.Module.ResolveString(BitConverter.ToInt32(bytes,p));p+=4;break;
                case OperandType.InlineI:case OperandType.InlineBrTarget:case OperandType.InlineSig:operand=BitConverter.ToInt32(bytes,p);p+=4;break;
                case OperandType.InlineSwitch:int count=BitConverter.ToInt32(bytes,p);p+=4+4*count;break;
                default:throw new Exception("Unknown operand "+op.OperandType);
            }
            code.Add(new CodeInstruction(op,operand));
        }
        return code;
    }
    static void BindingContract()
    {
        var show=typeof(OtherworldHitDisplay).GetMethod("Show",BindingFlags.Static|BindingFlags.NonPublic)!;
        var code=Instructions(show);CoopRepairChecks.CheckDisplayShow(code,Check);
        foreach(string missing in new[]{"normal","critical","colored"})
        {
            var broken=code.Where(c=>!(c.operand is FieldInfo f&&f.Name==missing)).ToList();bool rejected=false;
            try{CoopRepairChecks.CheckDisplayShow(broken,(pass,name)=>{if(!pass)throw new Exception(name);});}catch(Exception){rejected=true;}
            Check(rejected,"display contract rejects missing delegate: "+missing);
        }
        var dirty=new List<CodeInstruction>(code){new(OpCodes.Call,typeof(模块_受伤).GetMethod("受到伤害",new[]{typeof(float)}))};bool failed=false;
        try{CoopRepairChecks.CheckDisplayShow(dirty,(pass,name)=>{if(!pass)throw new Exception(name);});}catch(Exception){failed=true;}
        Check(failed,"display contract rejects an accidental gameplay call");
    }
    internal static void Run()
    {
        StatusProvenance();Visibility();BindingContract();
        Console.WriteLine($"RESULT: {checks}/{checks} status provenance, sender visibility and actual Show-IL checks passed");
    }
}
