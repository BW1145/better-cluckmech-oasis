using System;
using System.Collections.Generic;
using System.Reflection;
using CluckmechCoop;
using UnityEngine;
using Mirror;
using Newtonsoft.Json;

static class Program
{
    static int checks;
    static void Check(bool pass,string name){checks++;if(!pass)throw new Exception(name);}
    static (模块_受伤 injury,OtherworldUnit unit) Unit(int space=0,bool special=false)
    {
        var g=new GameObject();var hp=g.Add(new 模块_基本属性());var injury=g.Add(new 模块_受伤{基本属性=hp});
        var unit=g.Add(new OtherworldUnit{Health=hp,Space=space});OtherworldWorld.Units[g]=unit;
        if(special)OtherworldProgress.Runs[g]=new OtherworldRun();return(injury,unit);
    }
    static void Apply(模块_受伤 target,世界类.伤害数据包 packet)
    {
        var args=new object[]{target,packet,null};
        bool run=(bool)typeof(OtherworldDirectDamage).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);
        try{if(run)target.受到伤害((世界类.伤害数据包)args[1]);}
        finally{typeof(OtherworldDirectDamage).GetMethod("Finalizer",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new[]{null,args[2]});}
    }
    static 世界类.伤害数据包 Packet(GameObject source,bool crit=false)=>new(){攻击者=source,攻击伤害=100,是否暴击=crit,暴击倍率=1,是否产生仇恨=true,是否击退=true,造成状态=new(){new()}};
    static void Damage()
    {
        var(view,vu)=Unit();OtherworldActions.Viewer=view.gameObject.Add(new OtherworldActor());
        foreach(bool crit in new[]{false,true})foreach(bool protect in new[]{false,true})foreach(bool invulnerable in new[]{false,true})foreach(int space in new[]{0,1})
        {
            var(src,su)=Unit(space,true);var(host,hu)=Unit(space);var(client,cu)=Unit(space);vu.Space=space;
            foreach(var hp in new[]{host.基本属性,client.基本属性}){hp.当前抗性=.2f;hp.当前格挡值=10;hp.当前反伤值=.1f;hp.当前是否保护=protect;hp.当前是否无敌=invulnerable;}
            KamuiWorld.Current.Pressure=space==1;
            Plugin.Role=1;Apply(host,Packet(src.gameObject,crit));
            Plugin.Role=2;float attackerHealth=src.基本属性.当前生命值;Apply(client,Packet(src.gameObject,crit));
            Check(client.Visuals==host.Visuals&&client.LastVisual==host.LastVisual&&client.CriticalVisuals==host.CriticalVisuals,"native display parity");
            Check(client.基本属性.当前生命值==1000&&src.基本属性.当前生命值==attackerHealth,"display does not change HP or reflect");
            Check(client.状态属性.Count==0&&client.仇恨.Count==0&&client.移动.Pushes==0&&client.战斗事件.Events==0&&client.gameObject.StatisticMessages==0,"display has no gameplay/statistical side effects");
        }
        KamuiWorld.Current.Pressure=false;Plugin.Role=2;vu.Space=0;
        var(a,au)=Unit(0,true);var(t,tu)=Unit(1);Apply(t,Packet(a.gameObject));Check(t.Visuals==0,"cross-space rejection");
        (t,tu)=Unit();tu.IsPlayer=true;t.gameObject.Add(new OtherworldActor{Phased=true});Apply(t,Packet(a.gameObject));Check(t.Visuals==0,"phase rejection");
        (t,tu)=Unit(1);(a,au)=Unit(1,true);Apply(t,Packet(a.gameObject));Check(t.Visuals==0,"private-space viewer isolation");
        (t,tu)=Unit();(a,au)=Unit(1,true);var old=Packet(a.gameObject);OtherworldDamage.Remember(old,0);Apply(t,old);Check(t.Visuals==1,"emission space survives attacker transfer");
        (t,tu)=Unit();Apply(t,new 世界类.伤害数据包{攻击者=a.gameObject,攻击伤害=-1});Check(t.Visuals==0,"negative damage rejected");
        Check(OtherworldHitDisplay.Value(0,0,100,false,true,1)==2,"minimum damage before critical");
        // Preserve the explicitly chosen host cross-space reflection behavior.
        (t,tu)=Unit();t.基本属性.当前反伤值=.1f;Plugin.Role=1;old=Packet(a.gameObject);OtherworldDamage.Remember(old,0);Apply(t,old);
        Check(a.基本属性.当前生命值==990,"host scalar reflection retains gameplay behavior");
        StratagemWorld.Ids[t.gameObject]=9;var identity=t.gameObject.Add(new NetworkIdentity());NetworkClient.spawned[9]=identity;
        using(var display=new OtherworldHitDisplay.Batch())
        {
            Apply(t,old);Check(display.Hits.Count==1&&display.Hits[0].Amount==100,"batch captures actual native target value");
            using(var nested=new OtherworldHitDisplay.Batch()){Apply(t,old);Check(nested.Hits.Count==1,"nested batch owns its result");}
            Check(display.Hits.Count==1,"nested result not duplicated in parent");
            var data=JsonConvert.SerializeObject(display.Hits);float health=t.基本属性.当前生命值;int events=t.战斗事件.Events;
            Plugin.Role=2;int visuals=t.Visuals;OtherworldHitDisplay.Receive(data);
            Check(t.Visuals==visuals+1&&t.基本属性.当前生命值==health&&t.战斗事件.Events==events,"result replay changes display only");
            tu.Space=1;OtherworldHitDisplay.Receive(data);Check(t.Visuals==visuals+1,"moved target rejected");tu.Space=0;
            NetworkClient.spawned.Clear();OtherworldHitDisplay.Receive(data);Check(t.Visuals==visuals+1,"destroyed target skipped");
        }
    }
    static GameObject Prefab(int id,uint asset,string name,bool temporary=false,int kind=9,float health=100)
    {
        var g=new GameObject{name=name};g.Add(new 模块_基本属性{ID=id,单位类型=(模块_基本属性.单位类型枚举)kind,生命值=health});
        if(asset!=0)g.Add(new NetworkIdentity{assetId=asset});if(temporary)g.Add(new 词条_100001_毒气带倒计时());return g;
    }
    static KamuiSavedUnit Legacy(GameObject g)=>new(){Id=g.GetComponent<模块_基本属性>().ID,Components=new(){[nameof(模块_基本属性)]=new(){["初始阵营"]=new[]{1f},["单位类型"]=new[]{(float)g.GetComponent<模块_基本属性>().单位类型},["单位分类"]=new[]{0f},["生命值"]=new[]{g.GetComponent<模块_基本属性>().生命值}}}};
    static void Prefabs()
    {
        var root=Prefab(13,1,"nest",kind:13);var child=Prefab(13,2,"ant");var stage1=Prefab(58,3,"egg1",kind:13);var stage2=Prefab(58,4,"egg2",kind:13,health:200);var adult=Prefab(58,5,"dinosaur");
        var oil=Prefab(1012,1026146981,"oil",true,14);var mushroom=Prefab(1012,1126518621,"mushroom",kind:11);
        var timed=Prefab(80,6,"timed");timed.Add(new 词条_100098_限时自杀());var local=Prefab(90,0,"local");
        NetworkManager.singleton.spawnPrefabs=new(){root,child,stage1,stage2,adult,oil,mushroom,timed,local};
        var registry=new KamuiPrefabs();
        foreach(var g in NetworkManager.singleton.spawnPrefabs)
        {
            var saved=Legacy(g);KamuiPrefabs.Identify(g,saved);var resolved=registry.Resolve(saved,out bool temp);
            Check(temp==KamuiPrefabs.Temporary(g)&&resolved==(temp?null:g),"exact identity including duplicate IDs");
            var legacy=Legacy(g);resolved=registry.Resolve(legacy,out temp);
            if(g==child||g==stage2||g==adult||g==root||g==oil||g==mushroom)Check(resolved==(temp?null:g),"old record resolves by template evidence");
        }
        Check(!KamuiPrefabs.Temporary(timed),"valid limited-lifetime units persist");
        var blast=Prefab(26,80,"blast",kind:14);blast.Add(new 词条_100034_定时销毁());Check(KamuiPrefabs.Temporary(blast),"timed area damage is ephemeral");
        var mobile=Prefab(81,81,"mobile");mobile.Add(new 词条_100034_定时销毁());Check(!KamuiPrefabs.Temporary(mobile),"timed mobile body remains a unit");
        var pendingUnit=Legacy(child);pendingUnit.Key="pending";pendingUnit.TimedLifetime=3;pendingUnit.DestroyLifetime=4;pendingUnit.Lifetime=5;
        var activeUnit=Legacy(root);activeUnit.Key="active";
        var previous=new List<KamuiSavedRoom>{new(){Owner=1,Units=new(){pendingUnit,activeUnit},Drops=new(){new(){Key="collected"}}}};
        var current=new List<KamuiSavedRoom>{new(){Owner=0,Units=new(){activeUnit}}};
        var pendingKeys=new HashSet<string>{"pending"};KamuiPrefabs.RetainPending(current,previous,pendingKeys);
        Check(current.Count==2&&ReferenceEquals(current[1].Units[0],pendingUnit)&&current[1].Drops.Count==0,"pending raw record survives owner migration without duplicating live units/drops");
        KamuiPrefabs.RetainPending(current,previous,pendingKeys);Check(current[1].Units.Count==1,"repeated capture retains pending once");
        var roundTrip=JsonConvert.DeserializeObject<KamuiSavedUnit>(JsonConvert.SerializeObject(pendingUnit));Check(roundTrip.TimedLifetime==3&&roundTrip.DestroyLifetime==4&&roundTrip.Lifetime==5,"all native unit lifetime values round trip");
        var oldRoundTrip=JsonConvert.DeserializeObject<KamuiSavedUnit>("{\"Id\":13}");Check(oldRoundTrip.TimedLifetime==-1&&oldRoundTrip.DestroyLifetime==-1&&oldRoundTrip.AssetId==0,"old records retain optional field defaults");

        var unknown=Legacy(child);unknown.AssetId=999;Check(registry.Resolve(unknown,out _)==null,"unregistered identity never falls back to ID");
        var ambiguous=Prefab(13,7,"otherAnt");NetworkClient.prefabs[7]=ambiguous;registry=new KamuiPrefabs();
        var retained=Legacy(child);string original=JsonConvert.SerializeObject(retained);Check(registry.Resolve(retained,out _)==null&&JsonConvert.SerializeObject(retained)==original,"ambiguous legacy record remains intact");
        var incomplete=new KamuiSavedUnit{Id=13};Check(registry.Resolve(incomplete,out _)==null,"insufficient legacy identity remains pending");
        foreach(int id in new[]{1013,1014,1016,1009,4,10011})
        {
            var effect=Prefab(id,(uint)id,"temporary",true,14);Check(KamuiPrefabs.Temporary(effect),"lifecycle qualification independent of ID");
        }
    }
    static void Spawn()
    {
        foreach(int role in new[]{0,1,2})foreach(bool snail in new[]{true,false})foreach(bool networkUnit in new[]{true,false})
        {
            Plugin.Role=role;Steam联机管理器.当前联机状态=(Steam联机管理器.联机状态)role;
            var source=new GameObject();source.Add(new 模块_基本属性{ID=108});var prefab=new GameObject();if(networkUnit)prefab.Add(new 模块_基本属性{ID=1012});
            var native=snail?(Component)new SnailNative():(Component)new HotStepNative();native.gameObject=source;
            native.GetType().GetField("生成物").SetValue(native,prefab);native.GetType().GetField("基本属性").SetValue(native,source.GetComponent<模块_基本属性>());
            UnityEngine.Object.Instantiations=NetworkServer.Calls=0;int errors=0;
            for(int i=0;i<600;i++)try{native.GetType().GetMethod("Tick").Invoke(native,null);}catch(TargetInvocationException e)when(e.InnerException is NullReferenceException){errors++;}
            int nativeCount=UnityEngine.Object.Instantiations,nativeSpawns=NetworkServer.Calls;
            UnityEngine.Object.Instantiations=NetworkServer.Calls=0;float remaining=2.5f;
            for(int i=0;i<600;i++)NativePeriodicSpawn.Tick(native,prefab,2.5f,ref remaining,snail);
            Check(remaining>0,"timer resets even on guest");
            if(role==2&&networkUnit)Check(errors>400&&UnityEngine.Object.Instantiations==0&&NetworkServer.Calls==0,"native exception flood replaced by zero guest spawns");
            else Check(errors==0&&UnityEngine.Object.Instantiations==nativeCount&&NetworkServer.Calls==nativeSpawns,"native period and authority/visual spawn counts retained");
            Console.WriteLine($"spawn role={role} snail={snail} unit={networkUnit}: nativeErrors={errors}, fixedSpawns={UnityEngine.Object.Instantiations}, network={NetworkServer.Calls}");
        }
        float failed=0;NativePeriodicSpawn.Tick(new SnailNative{gameObject=new GameObject()},null,2.5f,ref failed,true);Check(failed==2.5f,"missing prefab consumes period");
    }
    static void NetworkBatches()
    {
        Plugin.Role=1;
        foreach(int count in new[]{0,1,256,257,512,513,1000})
        {
            OtherworldNetwork.Sent.Clear();var hits=new List<OtherworldDisplayHit>();
            for(int i=0;i<count;i++)hits.Add(new(){Target=(uint)(i+1),Amount=i,Space=0,Critical=i%2==0});
            OtherworldNetwork.SendDisplay(new(){Kind=11,Value=4},hits,-1);
            Check(OtherworldNetwork.Sent.Count==Math.Max(1,(count+255)/256),"bounded result message count");
            int total=0;
            for(int i=0;i<OtherworldNetwork.Sent.Count;i++)
            {
                var message=OtherworldNetwork.Sent[i];Check(message.Value==(i==0?4:0),"one visual invocation per tick");
                if(message.Data==null)continue;
                var received=JsonConvert.DeserializeObject<List<OtherworldDisplayHit>>(message.Data);
                Check(received.Count<=256&&System.Text.Encoding.UTF8.GetByteCount(message.Data)<65535,"Mirror string budget");
                foreach(var hit in received)Check(hit.Target==++total,"all target results retained in order");
            }
            Check(total==count,"all batch results sent once");
        }
        Plugin.Role=2;OtherworldNetwork.Sent.Clear();OtherworldNetwork.SendDisplay(new(),new(){new(){Target=1}},-1);Check(OtherworldNetwork.Sent.Count==0,"guest cannot send authoritative results");
        Plugin.Role=1;OtherworldNetwork.SendDisplay(new(){Kind=11,Value=0},new(),-1);Check(OtherworldNetwork.Sent.Count==0,"empty delayed result consumes no network message");
    }
    static void SpawnReadiness()
    {
        Plugin.Role=2;Plugin.Instance.Pending.Clear();SceneLifecycle.Current=new();Plugin.Combat.Token="ready-test";
        var(view,viewer)=Unit(1);OtherworldActions.Viewer=view.gameObject.Add(new OtherworldActor());
        var(target,unit)=Unit(1);var identity=target.gameObject.Add(new NetworkIdentity());NetworkClient.spawned[100]=identity;
        var data=JsonConvert.SerializeObject(new[]{new OtherworldDisplayHit{Target=100,Space=1,Amount=10},new OtherworldDisplayHit{Target=100,Space=1,Amount=20,Critical=true}});
        target.引用器=null;OtherworldWorld.Units.Remove(target.gameObject);OtherworldHitDisplay.Receive(data);
        Check(target.Visuals==0&&Plugin.Instance.Pending.Count==1,"same-batch Spawn/result waits for native Start and space registration");
        target.引用器=new();Plugin.Instance.Frame();Check(target.Visuals==0&&Plugin.Instance.Pending.Count==1,"native Start alone waits for unit registration");
        OtherworldWorld.Units[target.gameObject]=unit;Plugin.Instance.Frame();
        Check(target.Visuals==2&&target.LastVisual==20&&target.CriticalVisuals==1&&Plugin.Instance.Pending.Count==0,"initialized private target replays both results once and in order");
        Plugin.Instance.Frame();Check(target.Visuals==2&&target.基本属性.当前生命值==1000,"ready replay is display only and not repeated");
        foreach(string cancellation in new[]{"scene","token","destroy","space"})
        {
            unit.Space=viewer.Space=1;NetworkClient.spawned[100]=identity;
            target.引用器=null;OtherworldHitDisplay.Receive(data);int visuals=target.Visuals;target.引用器=new();
            if(cancellation=="scene")SceneLifecycle.Current=new();
            else if(cancellation=="token")Plugin.Combat.Token+="next";
            else if(cancellation=="destroy")NetworkClient.spawned.Remove(100);
            else unit.Space=0;
            Plugin.Instance.Frame();Check(target.Visuals==visuals&&Plugin.Instance.Pending.Count==0,"pending result respects "+cancellation);
        }
        viewer.Space=unit.Space=0;NetworkClient.spawned[100]=identity;target.引用器=null;
        OtherworldHitDisplay.Receive(JsonConvert.SerializeObject(new[]{new OtherworldDisplayHit{Target=100,Space=0,Amount=30}}));
        Check(Plugin.Instance.Pending.Count==1,"new main-world target also waits for native initialization");
        target.引用器=new();Plugin.Instance.Frame();Check(target.LastVisual==30,"main-world initialized result displays");
    }
    static void Main(string[] args)
    {
        if(Array.IndexOf(args,"--readiness")>=0){SpawnReadiness();Console.WriteLine($"PASS: {checks} same-batch Spawn/display readiness and cancellation checks");return;}
        if(Array.IndexOf(args,"--network")<0){Damage();Prefabs();Spawn();}
        NetworkBatches();SpawnReadiness();Console.WriteLine($"PASS: {checks} production repair checks; extracted native damage/spawn, substituted Unity/render/network");
    }
}
