using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using CluckmechCoop;
using HarmonyLib;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

static class Program
{
    static int checks;
    static void Check(bool value,string name){checks++;if(!value)throw new Exception(name);Console.WriteLine("PASS: "+name);}
    static bool Near(float x,float y)=>Math.Abs(x-y)<.0001f;
    static void Reset(bool otherworld=false)
    {
        SceneLifecycle.Current=new();Plugin.Combat.Token=Guid.NewGuid().ToString("N");
        ModuleSettings.Extra.Stratagems=false;OtherworldWorld.Active=false;Plugin.Instance.Frame();
        Plugin.Instance.Pending.Clear();OtherworldWorld.Units.Clear();NetworkClient.spawned.Clear();StratagemWorld.Ids.Clear();
        OtherworldActions.Viewer=null;StratagemWorld.NativeViewer=null;OtherworldNetwork.Sent.Clear();OtherworldNetwork.CharacterRoutes=0;
        Plugin.Role=1;Plugin.InCombat=true;Plugin.Network.PeerReady=true;ModuleSettings.Extra.Stratagems=true;
        OtherworldWorld.Active=otherworld;OtherworldRegistry.Enabled=otherworld;Time.realtimeSinceStartup=0;
    }
    static 模块_受伤 Target(uint id=1,int space=0,bool register=true)
    {
        var obj=new GameObject();var hp=obj.Add(new 模块_基本属性());
        var hurt=obj.Add(new 模块_受伤{基本属性=hp});var identity=obj.Add(new NetworkIdentity());
        NetworkClient.spawned[id]=identity;StratagemWorld.Ids[obj]=id;
        if(register)OtherworldWorld.Units[obj]=obj.Add(new OtherworldUnit{Health=hp,Space=space});
        return hurt;
    }
    static StratagemDamage Source(StratagemKind kind=StratagemKind.Laser)
    {
        var obj=new GameObject();obj.Add(new 模块_基本属性());obj.Add(new 模块_攻击属性());
        obj.Add(new 模块_攻击详细属性());obj.Add(new StratagemUnit{Kind=kind});return new StratagemDamage(obj);
    }
    static void Advance(float dt=.11f){Time.realtimeSinceStartup+=dt;Plugin.Instance.Frame();}
    static List<OtherworldDisplayHit> Results()=>OtherworldNetwork.Sent.SelectMany(m=>JsonConvert.DeserializeObject<List<OtherworldDisplayHit>>(m.Data)).ToList();
    static void Deliver()
    {
        Plugin.Role=2;
        foreach(var original in OtherworldNetwork.Sent.ToArray())
        {var message=original;message.Token=Plugin.Combat.Token;OtherworldNetwork.Route(message);}
    }
    static void Direct()
    {
        foreach(bool otherworld in new[]{false,true})
        foreach(var kind in new[]{StratagemKind.Mines,StratagemKind.Barrage,StratagemKind.Laser,StratagemKind.Eagle,StratagemKind.Rover})
        foreach(bool critical in new[]{false,true})
        {
            Reset(otherworld);var source=Source(kind);var target=Target(register:otherworld);
            target.基本属性.当前抗性=.2f;target.基本属性.当前格挡值=10;
            source.Hit(target.基本属性,100,critical);
            float expected=critical?140:70;
            Check(Near(target.基本属性.当前生命值,1000-expected)&&target.战斗事件.Events==1,"unchanged host hit: "+kind+" crit="+critical+" otherworld="+otherworld);
            Check(OtherworldNetwork.Sent.Count==0,"stratagem hit is queued, not sent per target");
            Advance();var hits=Results();
            Check(hits.Count==1&&Near(hits[0].Amount,expected)&&hits[0].Critical==critical,"native display value captured: "+kind);
            Check(OtherworldNetwork.LastOwner==2,"results target guest only");
            int before=target.Visuals;float health=target.基本属性.当前生命值;Deliver();
            Check(target.Visuals==before+1&&Near(target.LastVisual,expected),"guest displays with or without OtherworldUnit");
            Check(target.基本属性.当前生命值==health&&target.战斗事件.Events==1&&target.状态属性.Count==0,"replay has no damage, state or statistic side effects");
            Check(OtherworldNetwork.CharacterRoutes==0,"stratagem envelope is consumed once before character routing");
        }
        Reset();var damage=Source();var t=Target();t.基本属性.当前是否无敌=true;damage.Hit(t.基本属性,10,false);Advance();
        Check(OtherworldNetwork.Sent.Count==0&&t.Visuals==0,"invulnerability has no fabricated feedback");
        t.基本属性.当前是否无敌=false;t.基本属性.当前是否保护=true;damage.Hit(t.基本属性,10,false);Advance();
        Check(Results().Count==1&&Results()[0].Amount==0,"native protected zero display preserved");
        Reset();damage=Source(StratagemKind.Eagle);t=Target();t.基本属性.当前阵营=阵营数据库.阵营.无;t.基本属性.单位定位列表.Add(模块_基本属性.单位定位枚举.矿山);
        damage.Hit(t.基本属性,30,false,mineral:true);Advance();Check(Results().Count==1&&Results()[0].Amount==30,"Eagle mineral hit shares captured native path");
        foreach(int role in new[]{0,2})
        {
            Reset();Plugin.Role=role;t=Target();using(StratagemHitDisplay.Capture())t.Show(10,false);Advance();
            Check(OtherworldNetwork.Sent.Count==0&&Plugin.Instance.Pending.Count==0,"no capture allocation or send worker on role "+role);
        }
        Reset();t=Target();t.Show(1,false);Advance();Check(OtherworldNetwork.Sent.Count==0,"ordinary native hit does not create result traffic");
    }
    static void Batching()
    {
        Reset();var t=Target();using(StratagemHitDisplay.Capture())for(int i=0;i<600;i++)t.Show(i,false);
        Check(OtherworldNetwork.Sent.Count==2&&Plugin.Instance.Pending.Count==1,"600 results use shared bounded sender, not 600 messages");
        Advance();var hits=Results();
        Check(OtherworldNetwork.Sent.Count==3&&hits.Count==600&&hits.Select(h=>h.Amount).SequenceEqual(Enumerable.Range(0,600).Select(i=>(float)i)),"all 600 results arrive ordered in 3 batches");
        Check(OtherworldNetwork.Sent.All(m=>Encoding.UTF8.GetByteCount(m.Data)<65535&&JsonConvert.DeserializeObject<List<OtherworldDisplayHit>>(m.Data).Count<=256),"wire batches respect existing serializer budget");
        Check(Plugin.Instance.Pending.Count==0,"sender exits when empty");
        Reset();t=Target();
        for(int i=0;i<50;i++)
        {using(StratagemHitDisplay.Capture()){t.Show(i,false);t.Show(i,true);}Advance(.02f);}
        Advance();Check(Results().Count==100&&OtherworldNetwork.Sent.Count<=11,"continuous 50Hz hits from producers are batched at 100ms");
        Check(Plugin.Instance.Pending.Count==0,"continuous stream leaves no idle worker");
        Reset();t=Target();using(StratagemHitDisplay.Capture())t.Show(1000,false);
        typeof(StratagemDisplayBeforeDeath).GetMethod("Prefix",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,null);
        Check(OtherworldNetwork.Sent.Count==1,"killing blow batch flushes before native death RPC");
        StratagemHitDisplay.Flush();Check(OtherworldNetwork.Sent.Count==1,"subsequent deaths do not duplicate drained batch");
        foreach(string reason in new[]{"scene","token","module","combat","peer"})
        {
            Reset();t=Target();using(StratagemHitDisplay.Capture())t.Show(20,false);
            switch(reason){case "scene":SceneLifecycle.Current=new();break;case "token":Plugin.Combat.Token="next";break;case "module":ModuleSettings.Extra.Stratagems=false;break;case "combat":Plugin.InCombat=false;break;case "peer":Plugin.Network.PeerReady=false;break;}
            Advance();Check(OtherworldNetwork.Sent.Count==0&&Plugin.Instance.Pending.Count==0,"sender cancellation: "+reason);
        }
        Reset();t=Target();using(StratagemHitDisplay.Capture())t.Show(1,false);
        SceneLifecycle.Current=new();Plugin.Combat.Token="replacement";using(StratagemHitDisplay.Capture())t.Show(2,false);
        Advance();Check(Results().Count==1&&Results()[0].Amount==2&&Plugin.Instance.Pending.Count==0,"old sender cannot clear or deliver new scene results");
    }
    static void States()
    {
        foreach(int color in new[]{1,2,3})
        {
            Reset();var t=Target();var source=Source(StratagemKind.Mortar);
            var own=new 模块_状态属性.状态类{攻击者=source.Source,加成属性=new(){当前属性效果=new Vector4(4,0,0,0)}};
            var ordinary=new 模块_状态属性.状态类{攻击者=new GameObject(),加成属性=new(){当前属性效果=new Vector4(10,0,0,0)}};
            using(StratagemHitDisplay.CaptureStatus(t.gameObject))
            {
                StratagemHitDisplay.BeginStatusSum(t.gameObject);GameObject.ComponentReads=0;
                float sum=StratagemHitDisplay.StateValue(own)+StratagemHitDisplay.StateValue(ordinary);
                Check(sum==14&&GameObject.ComponentReads==0,"native state sum unchanged, weak source lookup without component scan");
                t.ShowColored(sum*.5f,color);
            }
            Advance();var hit=Results().Single();
            Check(Near(hit.Amount,2)&&hit.Color==color,"only missing stratagem DoT portion forwarded, color="+color);
            int before=t.Visuals;Deliver();Check(t.Visuals==before+1&&Near(t.LastVisual,2)&&OtherworldHitDisplay.ColorKind(t.LastColor)==color,"guest native status color preserved");
            Check(t.基本属性.当前生命值==1000&&t.战斗事件.Events==0,"DoT capture/replay does not execute damage");
        }
        Reset();var target=Target();var src=Source();
        var state=new 模块_状态属性.状态类{攻击者=src.Source,加成属性=new(){当前属性效果=new Vector4(5,0,0,0)}};
        using(StratagemHitDisplay.CaptureStatus(target.gameObject))
        {
            StratagemHitDisplay.BeginStatusSum(target.gameObject);StratagemHitDisplay.StateValue(state);target.ShowColored(5,1);
            StratagemHitDisplay.BeginStatusSum(target.gameObject);target.ShowColored(7,2);
        }
        Advance();Check(Results().Count==1&&Results()[0].Amount==5,"each native status type resets attribution, no stale poison/burn reuse");
        Reset();target=Target();
        using(StratagemHitDisplay.CaptureStatus(target.gameObject))
        {
            StratagemHitDisplay.BeginStatusSum(target.gameObject);
            StratagemHitDisplay.StateValue(new(){攻击者=new GameObject(),加成属性=new(){当前属性效果=new Vector4(10,0,0,0)}});
            target.ShowColored(10,1);
        }
        Advance();Check(OtherworldNetwork.Sent.Count==0,"ordinary DoT is not retransmitted");
        Reset();target=Target();
        try{using(StratagemHitDisplay.CaptureStatus(target.gameObject))throw new InvalidOperationException("test");}catch(InvalidOperationException){}
        target.ShowColored(9,1);Advance();Check(OtherworldNetwork.Sent.Count==0,"exception restores scoped status sink");
    }
    static void NestedAndReceive()
    {
        Reset();var t=Target();
        using(var character=new OtherworldHitDisplay.Batch())
        {
            t.Show(1,false);using(StratagemHitDisplay.Capture())t.Show(2,false);t.Show(3,false);
            Check(character.Hits.Count==2&&character.Hits[0].Amount==1&&character.Hits[1].Amount==3,"nested stratagem does not duplicate parent character results");
        }
        Advance();Check(Results().Count==1&&Results()[0].Amount==2,"nested stratagem result sent once");
        Reset();t=Target();
        using(StratagemHitDisplay.Capture())
        {
            t.Show(1,false);using(var character=new OtherworldHitDisplay.Batch()){t.Show(2,false);Check(character.Hits.Count==1,"nested character owns its result");}t.Show(3,false);
        }
        Advance();Check(Results().Select(h=>h.Amount).SequenceEqual(new[]{1f,3f}),"stratagem sink restored after nested character batch");
        Reset();t=Target(register:false);t.引用器=null;Plugin.Role=2;
        var msg=new OtherworldMessage{Kind=11,Value=0,Sequence=StratagemHitDisplay.ResultMarker,Data="[{\"Target\":1,\"Amount\":12,\"Space\":0}]",Token=Plugin.Combat.Token};
        OtherworldNetwork.Route(msg);Check(Plugin.Instance.Pending.Count==1&&t.Visuals==0,"standalone stratagem waits for native initialization without Otherworld registry");
        t.引用器=new();Advance(.01f);Check(t.Visuals==1&&t.LastVisual==12&&Plugin.Instance.Pending.Count==0,"standalone pending result replays once");
        foreach(string reject in new[]{"token","module","combat","role"})
        {
            Reset();t=Target(register:false);Plugin.Role=2;msg.Token=Plugin.Combat.Token;
            if(reject=="token")msg.Token="old";else if(reject=="module")ModuleSettings.Extra.Stratagems=false;else if(reject=="combat")Plugin.InCombat=false;else Plugin.Role=1;
            OtherworldNetwork.Route(msg);Check(t.Visuals==0,"receive boundary: "+reject);
        }
        Reset(true);t=Target(space:2);var viewer=Target(2,2);StratagemWorld.NativeViewer=viewer.基本属性;OtherworldActions.Viewer=null;Plugin.Role=2;
        msg.Token=Plugin.Combat.Token;msg.Data="[{\"Target\":1,\"Amount\":15,\"Space\":2}]";
        OtherworldNetwork.Route(msg);Check(t.Visuals==1,"ordinary-character viewer in Kamui uses native player fallback");
        OtherworldWorld.Units[viewer.gameObject].Space=0;OtherworldNetwork.Route(msg);Check(t.Visuals==1,"private space remains filtered");
        Reset();t=Target(register:false);Plugin.Role=2;OtherworldHitDisplay.Receive("[{\"Target\":1,\"Amount\":4,\"Space\":0,\"Burn\":true}]");
        Check(t.LastColor.y==.4f,"old Burn payload remains compatible");
        int v=t.Visuals;OtherworldHitDisplay.Receive("[{\"Target\":1,\"Amount\":4,\"Space\":0,\"Color\":99}]");Check(t.Visuals==v,"invalid color discarded");
        msg=new(){Kind=11,Value=0,Player=99,Token=Plugin.Combat.Token};OtherworldRegistry.Enabled=true;OtherworldNetwork.Route(msg);
        Check(OtherworldNetwork.CharacterRoutes==1,"existing character result envelope retains original routing");
    }
    static void Transpiler()
    {
        var patch=typeof(StratagemStatusDisplaySum).GetMethod("Transpiler",BindingFlags.NonPublic|BindingFlags.Static)!;
        var a=AccessTools.Field(typeof(模块_状态属性.状态类),"加成属性");var v=AccessTools.Field(typeof(模块_攻击详细属性.状态属性单元),"当前属性效果");var x=AccessTools.Field(typeof(Vector4),"x");
        var native=new List<CodeInstruction>{new(OpCodes.Ldloc_0),new(OpCodes.Ldloc_2),new(OpCodes.Ldfld,a),new(OpCodes.Ldflda,v),new(OpCodes.Ldfld,x),new(OpCodes.Add),new(OpCodes.Stloc_0)};
        var changed=((IEnumerable<CodeInstruction>)patch.Invoke(null,new object[]{native})!).ToList();
        Check(changed.Count==5&&changed[2].opcode==OpCodes.Call&&((MethodInfo)changed[2].operand).Name=="StateValue"&&changed[3].opcode==OpCodes.Add,"transpiler observes the existing native summand and retains addition");
        Check(native.Count==7&&native[2].opcode==OpCodes.Ldfld,"native input instructions untouched");
        foreach(var bad in new[]{native.Where(c=>!Equals(c.operand,a)).ToList(),native.Concat(native).ToList(),native.Select(c=>new CodeInstruction(c)).ToList()})
        {
            if(bad.Count==7)bad[4].operand=AccessTools.Field(typeof(Vector4),"y");
            bool rejected=false;try{patch.Invoke(null,new object[]{bad});}catch(TargetInvocationException e)when(e.InnerException is InvalidOperationException){rejected=true;}
            Check(rejected,"missing/duplicate/changed native seam rejected");
        }
    }
    static void Main()
    {
        Direct();Batching();States();NestedAndReceive();Transpiler();
        Console.WriteLine($"RESULT: {checks}/{checks} stratagem display checks passed; production code with Unity/transport/native/render substitutes");
    }
}
