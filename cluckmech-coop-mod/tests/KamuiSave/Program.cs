using System;
using System.Collections.Generic;
using CluckmechCoop;
using Newtonsoft.Json;

static class Program
{
    private static int checks;
    private static void Check(bool value,string name)
    {checks++;if(!value)throw new Exception(name);}
    private static OtherworldSave Healthy()
    {
        var unit=new KamuiSavedUnit
        {
            Key="unit",Id=20001,Owner=1,Faction=1,Health=100,Hatch=-.01f,Lifetime=-1,Cooldown=-2,
            Affixes=new(){201},Summons=new(){"child"},
            Components=new(){[nameof(模块_基本属性)]=new(){[nameof(模块_基本属性.ID)]=new[]{20001f},[nameof(模块_基本属性.是否无敌)]=new[]{0f}},
                [nameof(模块_移动属性)]=new(){[nameof(模块_移动属性.目标坐标)]=new[]{1f,2f}}},
            Elements=new(){new float[20]},
            States=new(){new(){Type=1,Owner=-1,Remaining=1,Values=new float[20],EffectResistance=-.2f,TimeResistance=.5f}},
            Underlying=new(){new(){底层状态=(攻击类型与状态数据库.底层状态枚举)1,数值=-.1f,持续时间=-.01f}},
            Buffs=new(){new(){加成名称="permanent",持续时间=-1,加成列表=new(){new(){加成属性=(模块_加成管理器.加成属性枚举)0,加成值=-.2f}}}},
            Stratagem=new(){Stage=1,Index=2,PendingMines=3,Progress=1,Wait=2,Remaining=3,MineDamage=20,Arm=.5f,
                Shield=new[]{10f,1f,2f,3f},Rover=new[]{10f,.5f,3f,2f,-1f,2f},
                Fields=new(){new(){X=1,Y=2,Radius=1.2f,Remaining=3}},Impacts=new(){new(){Kind=1,X=1,Y=2,Wait=.4f}}}
        };
        return new OtherworldSave
        {
            Players=new(){[1]=new(){Hero=12,Initial=26001,Talents=new(){26104},CardIds=new(){"card"},BaseInfluence=-.1f,
                LastDeployment=new(){Id=16,UpgradeLimit=3,Upgrades=new(){201}}}},
            Rooms=new(){new(){Owner=1,Units=new(){unit},Drops=new(){new(){Key="drop",Prefab="coin",X=1,Y=-1}}}}
        };
    }
    private static void Bad(string name,Action<OtherworldSave> mutate)
    {
        var save=Healthy();mutate(save);string original=JsonConvert.SerializeObject(save);
        Check(!KamuiWorld.Valid(save),name);
        Check(JsonConvert.SerializeObject(save)==original,name+" leaves original snapshot untouched");
    }
    private static void BadUnit(string name,Action<KamuiSavedUnit> mutate)=>Bad(name,s=>mutate(s.Rooms[0].Units[0]));
    private static void BadRuntime(string name,Action<StratagemSaved> mutate)=>BadUnit(name,u=>mutate(u.Stratagem));
    static void Main(string[] args)
    {
        if(args.Length>0&&args[0].TrimStart('-')=="lifecycle"){LifecycleHarness.Run();return;}
        if(args.Length>0&&args[0]=="storage"){ModuleSettings.Initialize(new BepInEx.Configuration.ConfigFile());StorageHarness.Run();return;}
        if(args.Length>0&&args[0]=="modules"){ModuleStateHarness.Run();return;}
        Check(KamuiWorld.Valid(Healthy()),"native state models, negative modifiers, permanent buffs and elapsed native timers accepted");
        Check(KamuiWorld.Valid(JsonConvert.DeserializeObject<OtherworldSave>(JsonConvert.SerializeObject(Healthy()))),"real JSON nested snapshot round trip");
        Check(KamuiWorld.Valid(new OtherworldSave()),"native-only/default addon accepted");
        Check(KamuiWorld.Valid(JsonConvert.DeserializeObject<OtherworldSave>("{\"Players\":{},\"Rooms\":[{\"Owner\":1,\"Units\":[{\"Id\":16,\"Key\":\"old\"}]}]}")),"older missing optional runtime fields retain initialized defaults");
        Check(!KamuiWorld.Valid(null),"null addon rejected");
        Bad("null players",s=>s.Players=null);Bad("null player",s=>s.Players[1]=null);
        Bad("unknown hero",s=>s.Players[1].Hero=1);Bad("unknown talent",s=>s.Players[1].Talents.Add(1));
        Bad("other hero initial",s=>s.Players[1].Initial=26005);Bad("other hero talent",s=>s.Players[1].Talents.Add(26113));
        Bad("null talents",s=>s.Players[1].Talents=null);Bad("nonfinite influence",s=>s.Players[1].BaseInfluence=float.NaN);
        Bad("bad deployment",s=>s.Players[1].LastDeployment.Upgrades=null);Bad("bad card identity",s=>s.Players[1].CardIds.Add(null));
        Bad("duplicate card identity",s=>s.Players[1].CardIds.Add("card"));Bad("null rooms",s=>s.Rooms=null);
        Bad("null room",s=>s.Rooms.Add(null));Bad("duplicate room",s=>s.Rooms.Add(new(){Owner=1}));
        Bad("bad room owner",s=>s.Rooms[0].Owner=3);Bad("null unit list",s=>s.Rooms[0].Units=null);
        Bad("null unit",s=>s.Rooms[0].Units.Add(null));Bad("duplicate identity",s=>s.Rooms[0].Drops[0].Key="unit");
        Bad("null drops",s=>s.Rooms[0].Drops=null);Bad("null drop",s=>s.Rooms[0].Drops.Add(null));
        Bad("null prefab",s=>s.Rooms[0].Drops[0].Prefab=null);Bad("infinite drop coordinate",s=>s.Rooms[0].Drops[0].X=float.PositiveInfinity);
        BadUnit("null unit key",u=>u.Key=null);BadUnit("unknown owner",u=>u.Owner=3);BadUnit("unknown faction",u=>u.Faction=99);
        BadUnit("negative health",u=>u.Health=-1);BadUnit("nonfinite cooldown",u=>u.Cooldown=float.NaN);
        BadUnit("nonfinite lifetime",u=>u.Lifetime=float.PositiveInfinity);BadUnit("nonfinite hatch",u=>u.Hatch=float.NaN);
        BadUnit("negative warmup",u=>u.Warm=-1);BadUnit("nonfinite rally",u=>u.Rally=float.NaN);
        BadUnit("null affixes",u=>u.Affixes=null);BadUnit("bad affix",u=>u.Affixes.Add(-1));
        BadUnit("null summons",u=>u.Summons=null);BadUnit("null summon key",u=>u.Summons.Add(null));
        BadUnit("null elements",u=>u.Elements=null);BadUnit("null element",u=>u.Elements.Add(null));
        BadUnit("short element",u=>u.Elements[0]=new float[19]);BadUnit("nonfinite element",u=>u.Elements[0][3]=float.NaN);
        BadUnit("null states",u=>u.States=null);BadUnit("null state",u=>u.States.Add(null));
        BadUnit("unknown state",u=>u.States[0].Type=99);BadUnit("state sentinel",u=>u.States[0].Type=(int)攻击类型与状态数据库.状态枚举.枚举数量);
        BadUnit("bad state owner",u=>u.States[0].Owner=-2);BadUnit("empty source key",u=>u.States[0].Source="");
        BadUnit("negative state duration",u=>u.States[0].Remaining=-1);BadUnit("nonfinite state duration",u=>u.States[0].Remaining=float.NaN);
        BadUnit("null state values",u=>u.States[0].Values=null);BadUnit("short state values",u=>u.States[0].Values=new float[1]);
        BadUnit("nonfinite state resistance",u=>u.States[0].EffectResistance=float.NaN);BadUnit("nonfinite state time resistance",u=>u.States[0].TimeResistance=float.PositiveInfinity);
        BadUnit("null underlying",u=>u.Underlying=null);BadUnit("null underlying element",u=>u.Underlying.Add(null));
        BadUnit("unknown underlying type",u=>u.Underlying[0].底层状态=(攻击类型与状态数据库.底层状态枚举)99);
        BadUnit("underlying sentinel",u=>u.Underlying[0].底层状态=攻击类型与状态数据库.底层状态枚举.枚举数量);
        BadUnit("nonfinite underlying value",u=>u.Underlying[0].数值=float.NaN);BadUnit("nonfinite underlying duration",u=>u.Underlying[0].持续时间=float.PositiveInfinity);
        BadUnit("null buffs",u=>u.Buffs=null);BadUnit("null buff",u=>u.Buffs.Add(null));
        BadUnit("null buff name",u=>u.Buffs[0].加成名称=null);BadUnit("nonfinite buff duration",u=>u.Buffs[0].持续时间=float.NaN);
        BadUnit("null buff values",u=>u.Buffs[0].加成列表=null);BadUnit("null buff value",u=>u.Buffs[0].加成列表.Add(null));
        BadUnit("unknown buff type",u=>u.Buffs[0].加成列表[0].加成属性=(模块_加成管理器.加成属性枚举)99);
        BadUnit("nonfinite buff value",u=>u.Buffs[0].加成列表[0].加成值=float.NaN);
        BadUnit("null components",u=>u.Components=null);BadUnit("null component fields",u=>u.Components[nameof(模块_基本属性)]=null);
        BadUnit("null component value",u=>u.Components[nameof(模块_基本属性)][nameof(模块_基本属性.ID)]=null);
        BadUnit("short vector",u=>u.Components[nameof(模块_移动属性)][nameof(模块_移动属性.目标坐标)]=new[]{1f});
        BadUnit("overflow native integer",u=>u.Components[nameof(模块_基本属性)][nameof(模块_基本属性.ID)]=new[]{float.MaxValue});
        BadUnit("fraction native integer",u=>u.Components[nameof(模块_基本属性)][nameof(模块_基本属性.ID)]=new[]{1.5f});
        BadUnit("invalid native bool",u=>u.Components[nameof(模块_基本属性)][nameof(模块_基本属性.是否无敌)]=new[]{2f});
        BadUnit("invalid native enum",u=>u.Components[nameof(模块_基本属性)][nameof(模块_基本属性.初始阵营)]=new[]{99f});
        BadRuntime("null fields",r=>r.Fields=null);BadRuntime("null field element",r=>r.Fields.Add(null));
        BadRuntime("nonfinite field coordinate",r=>r.Fields[0].Y=float.NaN);BadRuntime("negative field radius",r=>r.Fields[0].Radius=-1);
        BadRuntime("zero field radius",r=>r.Fields[0].Radius=0);BadRuntime("negative field duration",r=>r.Fields[0].Remaining=-1);
        BadRuntime("null impacts",r=>r.Impacts=null);BadRuntime("null impact element",r=>r.Impacts.Add(null));
        BadRuntime("unknown impact kind",r=>r.Impacts[0].Kind=2);BadRuntime("nonfinite impact coordinate",r=>r.Impacts[0].X=float.NaN);
        BadRuntime("negative impact delay",r=>r.Impacts[0].Wait=-1);BadRuntime("invalid task stage",r=>r.Stage=2);
        BadRuntime("negative task index",r=>r.Index=-1);BadRuntime("negative mine count",r=>r.PendingMines=-1);
        BadRuntime("negative progress",r=>r.Progress=-1);BadRuntime("nonfinite progress",r=>r.Progress=float.PositiveInfinity);
        BadRuntime("nonfinite wait",r=>r.Wait=float.NaN);BadRuntime("negative remaining time",r=>r.Remaining=-1);
        BadRuntime("negative mine damage",r=>r.MineDamage=-1);BadRuntime("nonfinite mine arm delay",r=>r.Arm=float.NaN);
        BadRuntime("short shield",r=>r.Shield=new float[3]);BadRuntime("negative shield capacity",r=>r.Shield[0]=-1);
        BadRuntime("nonfinite shield timer",r=>r.Shield[1]=float.NaN);BadRuntime("negative shield timer",r=>r.Shield[2]=-1);
        BadRuntime("short rover",r=>r.Rover=new float[5]);BadRuntime("negative rover heat",r=>r.Rover[0]=-1);
        BadRuntime("negative rover shots",r=>r.Rover[1]=-1);BadRuntime("invalid rover phase",r=>r.Rover[2]=4);
        BadRuntime("fraction rover phase",r=>r.Rover[2]=1.5f);BadRuntime("negative rover timer",r=>r.Rover[3]=-1);
        BadRuntime("nonfinite rover position",r=>r.Rover[4]=float.NaN);BadRuntime("empty parent key",r=>r.Parent="");
        Check(!KamuiWorld.ValidRuntime(null),"standalone null runtime guard");
        Console.WriteLine($"PASS: {checks} Kamui save checks using production validation, real game state types and JSON round trips");
    }
}
