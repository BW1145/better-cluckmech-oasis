using System;
using System.Collections.Generic;
using System.Linq;
using CluckmechCoop;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

static class Program
{
    static int checks;
    static readonly HashSet<string> failures=new();
    static void Check(bool pass,string name)
    {
        checks++;
        if(!pass)failures.Add(name);
        Console.WriteLine((pass?"PASS: ":"FAIL: ")+name);
    }
    static GameObject Prefab(int id,uint asset,string name,bool temporary=false,float health=100)
    {
        var obj=new GameObject{name=name};
        obj.Add(new 模块_基本属性{ID=id,生命值=health});
        if(asset!=0)obj.Add(new NetworkIdentity{assetId=asset});
        if(temporary)obj.Add(new 词条_100001_毒气带倒计时());
        return obj;
    }
    static KamuiSavedUnit Saved(GameObject obj,bool legacy=false)
    {
        var hp=obj.GetComponent<模块_基本属性>();
        var saved=new KamuiSavedUnit{Id=hp.ID,Components=new(){[nameof(模块_基本属性)]=new(){
            ["初始阵营"]=new[]{(float)hp.初始阵营},["单位类型"]=new[]{(float)hp.单位类型},
            ["单位分类"]=new[]{(float)hp.单位分类},["生命值"]=new[]{hp.生命值}}}};
        if(!legacy)KamuiPrefabs.Identify(obj,saved);
        return saved;
    }
    static void PrefabLookup()
    {
        NetworkClient.prefabs.Clear();Plugin.Units=new();
        var templates=Enumerable.Range(1,704).Select(i=>Prefab(i,(uint)i,"unit"+i)).ToList();
        NetworkManager.singleton.spawnPrefabs=templates;
        // The same registered object seen in multiple native registries is not ambiguity.
        NetworkClient.prefabs[704]=templates[703];
        Plugin.Units.单位列表.Add(new(){单位预制体=templates[703]});
        var registry=new KamuiPrefabs();var exact=Saved(templates[703]);
        GameObject.ComponentReads=0;bool all=true;
        for(int i=0;i<2000;i++)all&=registry.Resolve(exact,out bool temporary)==templates[703]&&!temporary;
        Check(all,"exact identity survives repeated resolution and duplicate registry references");
        Check(GameObject.ComponentReads==0,"asset lookup does not inspect unrelated components");
        Console.WriteLine("METRIC: 704 templates, 2000 exact resolutions, component reads="+GameObject.ComponentReads);
        var named=Saved(templates[703]);named.AssetId=0;
        GameObject.ComponentReads=0;var found=registry.Resolve(named,out _);
        Check(found==templates[703]&&GameObject.ComponentReads==0,"name fallback uses indexed bucket");
        var legacy=Saved(templates[703],true);
        GameObject.ComponentReads=0;found=registry.Resolve(legacy,out _);
        Check(found==templates[703]&&GameObject.ComponentReads<10,"legacy lookup inspects only same-ID templates");
        exact.Id=1;Check(registry.Resolve(exact,out _)==null,"asset lookup retains gameplay-ID cross-check");
        exact.Id=704;exact.AssetId=uint.MaxValue;
        Check(registry.Resolve(exact,out _)==null,"unknown asset never falls back to matching name or ID");
        var added=Prefab(900,900,"new-mod-unit");NetworkClient.prefabs[900]=added;
        Check(registry.Resolve(Saved(added),out _)==null&&new KamuiPrefabs().Resolve(Saved(added),out _)==added,"new restore rebuilds registration snapshot");

        var oil=Prefab(1012,10,"oil",true);var mushroom=Prefab(1012,20,"mushroom",health:200);
        var child=Prefab(13,30,"child");var nest=Prefab(13,40,"nest",health:300);
        var collision=Prefab(13,30,"collision");
        NetworkClient.prefabs.Clear();Plugin.Units=null;
        NetworkManager.singleton.spawnPrefabs=new(){oil,mushroom,child,nest};
        registry=new KamuiPrefabs();
        Check(registry.Resolve(Saved(oil),out bool temp)==null&&temp,"temporary effect is not restored");
        Check(registry.Resolve(Saved(mushroom),out temp)==mushroom&&!temp,"same-ID real enemy is retained");
        Check(registry.Resolve(Saved(child),out _)==child&&registry.Resolve(Saved(nest),out _)==nest,"child and producer identities remain distinct");
        Check(registry.Resolve(Saved(child,true),out _)==child,"unambiguous legacy child is retained");
        NetworkManager.singleton.spawnPrefabs.Add(collision);registry=new KamuiPrefabs();
        Check(registry.Resolve(Saved(child),out temp)==null&&!temp,"duplicate asset identity stays unresolved");
        Check(registry.Resolve(Saved(child,true),out _)==null,"ambiguous legacy identity stays unresolved");
        var local1=Prefab(9,0,"local");var local2=Prefab(9,0,"local");
        NetworkManager.singleton.spawnPrefabs=new(){local1,local2};registry=new KamuiPrefabs();
        Check(registry.Resolve(Saved(local1),out temp)==null&&!temp,"duplicate name identity stays unresolved");
        var localCase=Saved(local1);localCase.PrefabName="LOCAL";
        Check(registry.Resolve(localCase,out _)==null,"name identity comparison remains ordinal");
    }
    static (模块_受伤 injury,OtherworldUnit unit) Unit(uint id,int space=0,bool ready=true)
    {
        var obj=new GameObject();var hp=obj.Add(new 模块_基本属性());
        var injury=obj.Add(new 模块_受伤{基本属性=hp,引用器=ready?new():null});
        var unit=obj.Add(new OtherworldUnit{Health=hp,Space=space});
        OtherworldWorld.Units[obj]=unit;
        if(id!=0)NetworkClient.spawned[id]=obj.Add(new NetworkIdentity());
        return(injury,unit);
    }
    static void Reset()
    {
        OtherworldWorld.Active=false;
        Plugin.Instance.Frame(); // Run existing scope cancellation rather than clearing host lists.
        if(Plugin.Instance.Pending.Count!=0)throw new Exception("Scope did not stop");
        OtherworldWorld.Active=true;Plugin.Role=2;
        SceneLifecycle.Current=new();Plugin.Combat.Token=Guid.NewGuid().ToString("N");
        NetworkClient.spawned.Clear();OtherworldWorld.Units.Clear();
        Time.realtimeSinceStartup=0;Time.deltaTime=1f/60;
        var(view,_)=Unit(0);OtherworldActions.Viewer=view.gameObject.Add(new OtherworldActor());
    }
    static void Send(uint id,int count=1,int space=0,int first=1)
    {
        OtherworldHitDisplay.Receive(JsonConvert.SerializeObject(Enumerable.Range(first,count)
            .Select(n=>new OtherworldDisplayHit{Target=id,Space=space,Amount=n,Critical=n%2==0}).ToArray()));
    }
    static void DisplayReadiness()
    {
        Reset();var(target,unit)=Unit(1,ready:false);
        OtherworldWorld.Units.Remove(target.gameObject);
        Send(1,2);Check(target.Visuals==0&&Plugin.Instance.Pending.Count==1,"new target waits in one worker");
        target.引用器=new();Time.realtimeSinceStartup=.1f;Plugin.Instance.Frame();
        Check(target.Visuals==0&&Plugin.Instance.Pending.Count==1,"native initialization still requires space registration");
        OtherworldWorld.Units[target.gameObject]=unit;Time.realtimeSinceStartup=.2f;Plugin.Instance.Frame();
        Check(target.Visuals==2&&target.LastVisual==2&&target.CriticalVisuals==1&&Plugin.Instance.Pending.Count==0,"ready pending results display once in order");
        Plugin.Instance.Frame();Check(target.Visuals==2,"finished worker does not replay results");
        Check(target.基本属性.当前生命值==1000&&target.战斗事件.Events==0&&target.gameObject.StatisticMessages==0,"pending display does not change damage or statistics");
        Send(1);Check(target.Visuals==3&&Plugin.Instance.Pending.Count==0,"ready results bypass waiting");

        Reset();(target,unit)=Unit(1,ready:false);
        for(int i=0;i<3;i++)Send(1,256,first:1+i*256);
        Check(Plugin.Instance.Pending.Count==1,"one readiness worker across messages");
        var(ready,_)=Unit(2);Send(2,600);
        Check(ready.Visuals==600,"pending capacity does not cap ready results");
        target.引用器=new();Plugin.Instance.Frame();
        Check(target.Visuals==512&&target.LastVisual==512,"global pending limit is 512 results");
        Check(Plugin.Instance.Pending.Count==0,"capacity is released after ready replay");
        var(next,_)=Unit(3,ready:false);Send(3,10);next.引用器=new();Plugin.Instance.Frame();
        Check(next.Visuals==10,"later pending results can use released capacity");
    }
    static void DisplayTimeout()
    {
        Reset();var(target,_)=Unit(1,ready:false);Send(1,2);
        Time.deltaTime=0;Time.realtimeSinceStartup=1;Plugin.Instance.Frame();
        Check(Plugin.Instance.Pending.Count==0&&target.Visuals==0,"timeout stops waiting even with paused game time");
        target.引用器=new();Plugin.Instance.Frame();
        Check(target.Visuals==0,"expired results never replay after late initialization");
        Send(1);Check(target.Visuals==1,"timeout does not suppress later ready results");

        Reset();(target,_)=Unit(1,ready:false);Send(1,first:1);
        Time.realtimeSinceStartup=.75f;Send(1,first:2);
        Time.realtimeSinceStartup=1.01f;Plugin.Instance.Frame();
        target.引用器=new();Time.realtimeSinceStartup=1.1f;Plugin.Instance.Frame();
        Check(target.Visuals==1&&target.LastVisual==2,"each result has its own deadline without refresh by later batches");

        Reset();(target,_)=Unit(1,ready:false);
        for(int i=0;i<120;i++)
        {Time.realtimeSinceStartup=i/60f;Send(1);Plugin.Instance.Frame();}
        Time.realtimeSinceStartup=3.1f;Plugin.Instance.Frame();
        Check(Plugin.Instance.Pending.Count==0&&target.Visuals==0,"continuous unready arrivals do not leave immortal work");
    }
    static void DisplayCancellation()
    {
        foreach(string reason in new[]{"scene","token","inactive","destroy","hidden","space"})
        {
            Reset();var(target,unit)=Unit(1,ready:false);Send(1,2);
            if(reason=="scene")SceneLifecycle.Current=new();
            else if(reason=="token")Plugin.Combat.Token="new-token";
            else if(reason=="inactive")OtherworldWorld.Active=false;
            else if(reason=="destroy")NetworkClient.spawned.Remove(1);
            else if(reason=="hidden")target.gameObject.activeInHierarchy=false;
            else unit.Space=1;
            target.引用器=new();Plugin.Instance.Frame();
            Check(target.Visuals==0&&Plugin.Instance.Pending.Count==0,"pending cancellation: "+reason);
        }
        foreach(bool newScene in new[]{true,false})
        {
            Reset();var(old,_)=Unit(1,ready:false);Send(1,512);
            if(newScene)SceneLifecycle.Current=new();else Plugin.Combat.Token="new-token";
            var(fresh,_)=Unit(2,ready:false);Send(2,2);Plugin.Instance.Frame();
            Check(Plugin.Instance.Pending.Count==1,"old scope cannot hold new capacity: "+newScene);
            old.引用器=new();fresh.引用器=new();Plugin.Instance.Frame();
            Check(old.Visuals==0&&fresh.Visuals==2&&Plugin.Instance.Pending.Count==0,"old scope cleanup cannot clear newer pending work: "+newScene);
        }
        Reset();OtherworldHitDisplay.Receive("bad-json");OtherworldHitDisplay.Receive("null");
        Send(999);Check(Plugin.Instance.Pending.Count==0,"invalid or missing targets never reserve pending capacity");
        OtherworldWorld.Active=false;Plugin.Instance.Frame();
    }
    static int Main(string[] args)
    {
        PrefabLookup();DisplayReadiness();DisplayTimeout();DisplayCancellation();
        if(args.Contains("--baseline"))
        {
            string[] required={"asset lookup does not inspect unrelated components","one readiness worker across messages",
                "global pending limit is 512 results","timeout stops waiting even with paused game time"};
            bool reproduced=required.All(failures.Contains);
            Console.WriteLine("BASELINE: "+(reproduced?"expected defects reproduced":"missing expected defects")+"; failing assertions="+failures.Count);
            return reproduced?0:1;
        }
        Console.WriteLine($"RESULT: {checks-failures.Count}/{checks} checks passed; production source, substituted Unity/render/clock host");
        return failures.Count==0?0:1;
    }
}
