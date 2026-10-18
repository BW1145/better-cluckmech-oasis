using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CluckmechCoop
{
    internal sealed class OtherworldSave
    {
        public Dictionary<int,OtherworldRun> Players=new Dictionary<int,OtherworldRun>();
        public List<KamuiSavedRoom> Rooms=new List<KamuiSavedRoom>();
    }
    internal sealed class KamuiSavedRoom
    {
        public int Owner;
        public List<KamuiSavedUnit> Units=new List<KamuiSavedUnit>();
        public List<KamuiSavedDrop> Drops=new List<KamuiSavedDrop>();
    }
    internal sealed class KamuiSavedUnit
    {
        public string Key;
        public int Id,Owner,Faction,Difficulty;
        public bool Permanent;
        public StratagemSaved Stratagem;
        public List<string> Summons=new List<string>();
        public float Hatch=-1;
        public float X,Y,Health,Cooldown,Lifetime=-1,Warm,Rally;
        public List<int> Affixes=new List<int>();
        public Dictionary<string,Dictionary<string,float[]>> Components=new Dictionary<string,Dictionary<string,float[]>>();
        public List<float[]> Elements=new List<float[]>();
        public List<KamuiSavedStatus> States=new List<KamuiSavedStatus>();
        public List<模块_状态持续效果.底层状态类> Underlying=new List<模块_状态持续效果.底层状态类>();
        public List<模块_加成管理器.状态加成类> Buffs=new List<模块_加成管理器.状态加成类>();
    }
    internal sealed class KamuiSavedStatus
    {
        public int Type,Owner;
        public string Source;
        public float Remaining,EffectResistance,TimeResistance;
        public float[] Values;
    }
    internal sealed class KamuiSavedDrop {public string Key,Prefab;public float X,Y;}
    internal sealed class StratagemSaved
    {
        public string Parent;
        public int Stage,Index,PendingMines;
        public float Progress,Wait,Remaining,MineDamage,Arm;
        public bool Triggered,Releasing,Held;
        public float[] Shield,Rover;
        public List<StratagemSavedImpact> Impacts=new List<StratagemSavedImpact>();
        public List<StratagemSavedField> Fields=new List<StratagemSavedField>();
    }
    internal sealed class StratagemSavedImpact {public int Kind;public float X,Y,Wait;}
    internal sealed class StratagemSavedField {public float X,Y,Radius,Remaining;public bool Scorch;}

    internal static partial class KamuiWorld
    {
        private static readonly Type[] statTypes={typeof(模块_基本属性),typeof(模块_攻击属性),typeof(模块_攻击详细属性),typeof(模块_移动属性)};
        private static readonly Dictionary<Type,FieldInfo[]> scalarFields=new Dictionary<Type,FieldInfo[]>();
        internal static bool Valid(OtherworldSave save)
        {
            if(save?.Players==null||save.Rooms==null)return false;
            foreach(var pair in save.Players)
            {
                var run=pair.Value;
                if(pair.Key<0||pair.Key>2||run==null||OtherworldCatalog.Hero(run.Hero)==null||run.Talents==null||run.ShockKills<0||run.SpaceKills<0||run.Upgrades<0||!Finite(run.BaseInfluence))return false;
                if(run.Initial!=0&&(OtherworldCatalog.Talent(run.Initial)?.Hero!=run.Hero||!OtherworldCatalog.Talent(run.Initial).Initial))return false;
                if(run.Talents.Any(id=>OtherworldCatalog.Talent(id)==null||OtherworldCatalog.Talent(id).Hero!=run.Hero||OtherworldCatalog.Talent(id).Initial))return false;
                if(run.LastDeployment!=null&&(!Rules.ValidMachine(run.LastDeployment)||run.LastDeployment.Upgrades.Any(id=>id<=0)))return false;
                if(run.CardIds!=null&&(run.CardIds.Any(string.IsNullOrWhiteSpace)||run.CardIds.Distinct().Count()!=run.CardIds.Count))return false;
            }
            var owners=new HashSet<int>();var keys=new HashSet<string>();
            foreach(var room in save.Rooms)
            {
                if(room==null||room.Owner<0||room.Owner>2||!owners.Add(room.Owner)||room.Units==null||room.Drops==null)return false;
                foreach(var unit in room.Units)
                {
                    if(!ValidUnit(unit)||!keys.Add(unit.Key))return false;
                }
                foreach(var drop in room.Drops)
                    if(drop==null||string.IsNullOrWhiteSpace(drop.Key)||!keys.Add(drop.Key)||string.IsNullOrWhiteSpace(drop.Prefab)||!Finite(drop.X)||!Finite(drop.Y))return false;
            }
            return true;
        }
        private static bool ValidUnit(KamuiSavedUnit unit)
        {
            if(unit==null||unit.Id<=0||string.IsNullOrWhiteSpace(unit.Key)||!Enum.IsDefined(typeof(Steam联机管理器.联机状态),unit.Owner)||!Enum.IsDefined(typeof(阵营数据库.阵营),unit.Faction)||unit.Difficulty<0||
                !Finite(unit.X)||!Finite(unit.Y)||!Nonnegative(unit.Health)||!Finite(unit.Cooldown)||!Finite(unit.Hatch)||!Finite(unit.Lifetime)||!Nonnegative(unit.Warm)||!Nonnegative(unit.Rally)||
                unit.Components==null||unit.States==null||unit.Elements==null||unit.Underlying==null||unit.Buffs==null||unit.Affixes==null||unit.Summons==null)return false;
            if(unit.Affixes.Any(id=>id<=0)||unit.Summons.Any(string.IsNullOrWhiteSpace))return false;
            if(unit.Elements.Any(v=>!Values(v,20)))return false;
            foreach(var state in unit.States)
                if(state==null||!Status(state.Type)||state.Owner< -1||state.Owner>2||state.Source!=null&&string.IsNullOrWhiteSpace(state.Source)||
                    !Values(state.Values,20)||!Nonnegative(state.Remaining)||!Finite(state.EffectResistance)||!Finite(state.TimeResistance))return false;
            foreach(var state in unit.Underlying)
                if(state==null||!Enum.IsDefined(typeof(攻击类型与状态数据库.底层状态枚举),state.底层状态)||state.底层状态==攻击类型与状态数据库.底层状态枚举.枚举数量||!Finite(state.数值)||!Finite(state.持续时间))return false;
            foreach(var buff in unit.Buffs)
            {
                // Native permanent buffs use nonpositive durations; keep that convention.
                if(buff==null||string.IsNullOrWhiteSpace(buff.加成名称)||!Finite(buff.持续时间)||buff.加成列表==null)return false;
                foreach(var value in buff.加成列表)
                    if(value==null||!Enum.IsDefined(typeof(模块_加成管理器.加成属性枚举),value.加成属性)||!Finite(value.加成值))return false;
            }
            foreach(var component in unit.Components)
                if(component.Value==null||component.Value.Any(pair=>pair.Value==null||pair.Value.Any(v=>!Finite(v))))return false;
            foreach(var type in statTypes)
                if(unit.Components.TryGetValue(type.Name,out var values))
                    foreach(var field in Fields(type))
                        if(values.TryGetValue(field.Name,out var value)&&!Scalar(field.FieldType,value))return false;
            return unit.Stratagem==null||ValidRuntime(unit.Stratagem);
        }
        internal static bool ValidRuntime(StratagemSaved runtime)
        {
            if(runtime==null||runtime.Fields==null||runtime.Impacts==null||runtime.Stage<0||runtime.Stage>1||runtime.Index<0||runtime.PendingMines<0||
                !Nonnegative(runtime.Progress)||!Nonnegative(runtime.Wait)||!Nonnegative(runtime.Remaining)||!Nonnegative(runtime.MineDamage)||!Nonnegative(runtime.Arm)||
                runtime.Parent!=null&&string.IsNullOrWhiteSpace(runtime.Parent))return false;
            foreach(var impact in runtime.Impacts)
                if(impact==null||impact.Kind<0||impact.Kind>1||!Finite(impact.X)||!Finite(impact.Y)||!Nonnegative(impact.Wait))return false;
            foreach(var field in runtime.Fields)
                if(field==null||!Finite(field.X)||!Finite(field.Y)||!Finite(field.Radius)||field.Radius<=0||!Nonnegative(field.Remaining))return false;
            if(runtime.Shield!=null&&(!Values(runtime.Shield,4)||runtime.Shield.Any(v=>v<0)))return false;
            if(runtime.Rover!=null&&(!Values(runtime.Rover,6)||runtime.Rover[0]<0||runtime.Rover[1]<0||runtime.Rover[2]<0||runtime.Rover[2]>3||runtime.Rover[2]!=(int)runtime.Rover[2]||runtime.Rover[3]<0))return false;
            return true;
        }
        private static bool Status(int type)=>Enum.IsDefined(typeof(攻击类型与状态数据库.状态枚举),type)&&type!=(int)攻击类型与状态数据库.状态枚举.枚举数量;
        private static bool Values(float[] values,int size)=>values!=null&&values.Length==size&&values.All(Finite);
        private static bool Nonnegative(float value)=>Finite(value)&&value>=0;
        private static bool Finite(float value)=>!float.IsInfinity(value)&&!float.IsNaN(value);
        private static bool Scalar(Type type,float[] values)
        {
            int size=type==typeof(Vector4)?4:type==typeof(Vector3)?3:type==typeof(Vector2)?2:1;
            if(!Values(values,size))return false;
            if(type==typeof(bool))return values[0]==0||values[0]==1;
            if(type==typeof(int)||type.IsEnum)
            {
                double value=values[0];
                if(value<int.MinValue||value>int.MaxValue||value!=Math.Truncate(value))return false;
                return !type.IsEnum||Enum.IsDefined(type,(int)value);
            }
            return true;
        }
        private static FieldInfo[] Fields(Type type)
        {
            if(!scalarFields.TryGetValue(type,out var fields))
            {
                fields=type.GetFields(BindingFlags.Instance|BindingFlags.Public).Where(f=>!f.IsInitOnly&&!f.Name.StartsWith("当前")&&
                    (f.FieldType==typeof(float)||f.FieldType==typeof(int)||f.FieldType==typeof(bool)||f.FieldType.IsEnum||f.FieldType==typeof(Vector2)||f.FieldType==typeof(Vector3)||f.FieldType==typeof(Vector4))).ToArray();
                scalarFields[type]=fields;
            }
            return fields;
        }
    }
}
