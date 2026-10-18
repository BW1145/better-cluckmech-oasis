using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Mirror;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CluckmechCoop
{
    internal static partial class KamuiWorld
    {
        internal static List<KamuiSavedRoom> SavedRooms=new List<KamuiSavedRoom>();
        private static readonly Dictionary<string,GameObject> dropPrefabs=new Dictionary<string,GameObject>();
        private static int restoredScene=-1;
        internal static OtherworldSave Save()
        {Capture();OtherworldDeployment.RefreshLocal();return new OtherworldSave{Players=OtherworldProgress.Players,Rooms=SavedRooms};}
        internal static void Load(OtherworldSave save)
        {
            OtherworldProgress.Players=save.Players;SavedRooms=save.Rooms;
            OtherworldDeployment.Rebind();restoredScene=-1;
        }
        internal static void NewRun(){SavedRooms.Clear();restoredScene=-1;OtherworldDeployment.Rebind();}
        private static Dictionary<string,float[]> ReadScalars(Component component)
        {
            var values=new Dictionary<string,float[]>();
            foreach(var field in Fields(component.GetType()))
            {
                object value=field.GetValue(component);
                if(value is Vector4 v4)values[field.Name]=new[]{v4.x,v4.y,v4.z,v4.w};
                else if(value is Vector3 v3)values[field.Name]=new[]{v3.x,v3.y,v3.z};
                else if(value is Vector2 v2)values[field.Name]=new[]{v2.x,v2.y};
                else values[field.Name]=new[]{Convert.ToSingle(value)};
            }
            return values;
        }
        private static void WriteScalars(Component component,Dictionary<string,float[]> values)
        {
            foreach(var field in Fields(component.GetType()))
            {
                if(!values.TryGetValue(field.Name,out var value))continue;
                object restored=field.FieldType==typeof(Vector4)?(object)new Vector4(value[0],value[1],value[2],value[3]):
                    field.FieldType==typeof(Vector3)?new Vector3(value[0],value[1],value[2]):field.FieldType==typeof(Vector2)?(object)new Vector2(value[0],value[1]):
                    field.FieldType.IsEnum?Enum.ToObject(field.FieldType,(int)value[0]):Convert.ChangeType(value[0],field.FieldType);
                field.SetValue(component,restored);
            }
        }
        private static float[] Pack(模块_攻击详细属性.状态属性单元 s)=>new[]{s.默认持续时间,s.默认属性效果.x,s.默认属性效果.y,s.默认属性效果.z,s.默认属性效果.w,
            s.变化持续时间,s.变化百分比持续时间,s.变化属性效果.x,s.变化属性效果.y,s.变化属性效果.z,s.变化属性效果.w,
            s.变化百分比属性效果.x,s.变化百分比属性效果.y,s.变化百分比属性效果.z,s.变化百分比属性效果.w,s.当前持续时间,s.当前属性效果.x,s.当前属性效果.y,s.当前属性效果.z,s.当前属性效果.w};
        private static 模块_攻击详细属性.状态属性单元 Unpack(float[] v)=>new 模块_攻击详细属性.状态属性单元{默认持续时间=v[0],默认属性效果=new Vector4(v[1],v[2],v[3],v[4]),变化持续时间=v[5],变化百分比持续时间=v[6],
            变化属性效果=new Vector4(v[7],v[8],v[9],v[10]),变化百分比属性效果=new Vector4(v[11],v[12],v[13],v[14]),当前持续时间=v[15],当前属性效果=new Vector4(v[16],v[17],v[18],v[19])};
        internal static void Capture()
        {
            if(Plugin.Role==2||Rooms.Count==0||Restoring)return;
            var saved=new List<KamuiSavedRoom>();
            foreach(var room in Rooms.Values)
            {
                var record=new KamuiSavedRoom{Owner=room.Owner};
                foreach(var unit in room.Units)
                {
                    var stratagem=unit==null?null:unit.GetComponent<StratagemUnit>();
                    if(unit==null||unit.IsPlayer||unit.Health.当前生命值<=0&&stratagem?.HasDependents!=true||unit.Health.ID<=0||!unit.IsMachine&&!unit.Permanent||unit.Health.ID==20102)continue;
                    var entry=new KamuiSavedUnit{Key=unit.PersistentId,Id=unit.Health.ID,Owner=(int)unit.Health.所属玩家,Faction=(int)unit.Health.当前阵营,
                        Permanent=unit.Permanent,X=unit.transform.position.x-room.Center.x,Y=unit.transform.position.y-room.Center.y,
                        Health=unit.Health.当前生命值,Cooldown=unit.Attack?.当前攻击倒计时??0,Warm=Mathf.Max(0,unit.WarmUntil-Time.time),Rally=Mathf.Max(0,unit.RallyUntil-Time.time),Affixes=new List<int>(unit.Health.强化词条列表)};
                    entry.Stratagem=stratagem?.SaveRuntime(room.Center);
                    var nest=unit.GetComponent<词条_100009_巢穴类召唤>();
                    if(nest!=null){entry.Hatch=nest.当前孵化时间;foreach(var child in nest.当前存在召唤物){var tag=OtherworldWorld.Unit(child);if(tag!=null)entry.Summons.Add(tag.PersistentId);}}
                    var level=unit.GetComponent<模块_难度等级>();if(level!=null)entry.Difficulty=level.设定等级;
                    var lifetime=unit.GetComponent<词条_100098_限时自杀>();if(lifetime!=null)entry.Lifetime=lifetime.自杀倒计时;
                    foreach(var type in statTypes){var component=unit.GetComponent(type);if(component!=null)entry.Components[type.Name]=ReadScalars(component);}
                    if(unit.Detailed!=null)entry.Elements=unit.Detailed.状态加成列表.Select(Pack).ToList();
                    var states=unit.GetComponent<模块_状态属性>();
                    if(states!=null)foreach(var state in states.当前状态列表)
                    {
                        if(state.剩余时间<=0)continue;
                        var source=OtherworldWorld.Unit(state.攻击者);
                        entry.States.Add(new KamuiSavedStatus{Type=(int)state.状态类型,Owner=state is OtherworldState own?own.Owner:source!=null&&source.Health.当前阵营==阵营数据库.阵营.友方&&OtherworldProgress.For((int)source.Health.所属玩家)!=null?(int)source.Health.所属玩家:-1,
                            Source=source?.PersistentId,Remaining=state.剩余时间,Values=Pack(state.加成属性),EffectResistance=state.自身抗性属性.当前效果抗性,TimeResistance=state.自身抗性属性.当前时间抗性});
                    }
                    var effects=unit.GetComponent<模块_状态持续效果>();
                    if(effects!=null)entry.Underlying=effects.当前底层状态列表.Select(s=>new 模块_状态持续效果.底层状态类{底层状态=s.底层状态,数值=s.数值,持续时间=s.持续时间}).ToList();
                    var buffs=unit.GetComponent<模块_加成管理器>();
                    if(buffs!=null)entry.Buffs=buffs.状态加成列表.Select(b=>new 模块_加成管理器.状态加成类{加成名称=b.加成名称,持续时间=b.持续时间,加成列表=b.加成列表.Select(v=>new 模块_加成管理器.加成类{加成属性=v.加成属性,加成值=v.加成值}).ToList()}).ToList();
                    record.Units.Add(entry);
                }
                foreach(var drop in Object.FindObjectsOfType<模块_拾取精灵_移动>())
                    if(room.Bounds.Contains(drop.transform.position))
                    {KamuiDrop.Attach(drop.gameObject);var tracked=drop.GetComponent<KamuiDrop>();if(!tracked.Collected)record.Drops.Add(tracked.Save(room));}
                saved.Add(record);
            }
            SavedRooms=saved;
        }
        internal static void Restore()
        {
            int scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
            if(Plugin.Role==2||restoredScene==scene||SavedRooms.Count==0||Plugin.Spawner==null)return;
            restoredScene=scene;Plugin.Instance.StartCoroutine(RestoreRoutine(SceneLifecycle.Current));
        }
        private static IEnumerator RestoreRoutine(SceneLifetime scene)
        {
            if(!SceneLifecycle.IsCurrent(scene))yield break;
            Restoring=true;var objects=new Dictionary<string,OtherworldUnit>();var pending=new List<KeyValuePair<KamuiSavedUnit,OtherworldUnit>>();
            var affixes=Object.FindObjectOfType<cz词缀库>();
            try
            {
                foreach(var record in SavedRooms)
                {
                    int owner=Plugin.Role==0&&record.Owner==1?0:Plugin.Role==1&&record.Owner==0?1:record.Owner;
                    var room=Ensure(owner);
                    foreach(var saved in record.Units)
                    {
                        var prefab=Plugin.Units.获取单位(saved.Id);if(prefab==null)continue;
                        var obj=Object.Instantiate(prefab,room.Center+new Vector2(saved.X,saved.Y),Quaternion.identity);
                        var hp=obj.GetComponent<模块_基本属性>();hp.所属玩家=(Steam联机管理器.联机状态)(saved.Faction==(int)阵营数据库.阵营.友方&&saved.Owner==record.Owner?owner:saved.Owner);
                        hp.强化词条列表.AddRange(saved.Affixes);
                        if(Plugin.Role==1)
                        {
                            var sync=obj.GetComponent<联机_单位属性同步>();sync.Network所属玩家=hp.所属玩家;sync.添加待同步词条(saved.Affixes,true);
                            NetworkServer.Spawn(obj,Steam联机管理器.玩家_主机.gameObject);
                        }
                        else foreach(int id in saved.Affixes)affixes.赋予词缀(id,obj,true);
                        OtherworldWorld.Add(hp);var unit=OtherworldWorld.Unit(obj);unit.Permanent=saved.Permanent;unit.PersistentId=saved.Key;unit.Restoring=true;
                        var stratagem=obj.GetComponent<StratagemUnit>();if(stratagem!=null)stratagem.Resume=saved.Stratagem;
                        objects[saved.Key]=unit;pending.Add(new KeyValuePair<KamuiSavedUnit,OtherworldUnit>(saved,unit));
                        var level=obj.GetComponent<模块_难度等级>();if(level!=null)level.设定等级=saved.Difficulty;
                    }
                }
                yield return null;yield return null;yield return null;
                if(!SceneLifecycle.IsCurrent(scene))yield break;
                foreach(var pair in pending)
                {
                    var saved=pair.Key;var unit=pair.Value;if(unit==null)continue;
                    var stratagem=unit.GetComponent<StratagemUnit>();
                    if(stratagem!=null&&saved.Stratagem?.Parent!=null&&objects.TryGetValue(saved.Stratagem.Parent,out var parent))
                    {stratagem.Parent=parent.GetComponent<StratagemUnit>();stratagem.Parent?.Mines.Add(stratagem);}
                    var nest=unit.GetComponent<词条_100009_巢穴类召唤>();
                    if(nest!=null){nest.当前孵化时间=saved.Hatch;nest.当前存在召唤物.Clear();foreach(string key in saved.Summons)if(objects.TryGetValue(key,out var child))nest.当前存在召唤物.Add(child.gameObject);}
                    var owner=unit.Health.所属玩家;
                    foreach(var type in statTypes)if(saved.Components.TryGetValue(type.Name,out var values))
                    {var component=unit.GetComponent(type);if(component!=null)WriteScalars(component,values);}
                    unit.Health.当前阵营=(阵营数据库.阵营)saved.Faction;
                    unit.Health.所属玩家=owner;
                    if(unit.Detailed!=null&&saved.Elements.Count==unit.Detailed.状态加成列表.Length)
                        for(int i=0;i<saved.Elements.Count;i++)unit.Detailed.状态加成列表[i]=Unpack(saved.Elements[i]);
                    var stateModule=unit.GetComponent<模块_状态属性>();
                    if(stateModule!=null)
                    {
                        stateModule.当前状态列表.Clear();
                        foreach(var state in saved.States)
                        {
                            int sourceOwner=Plugin.Role==0&&state.Owner==1?0:Plugin.Role==1&&state.Owner==0?1:state.Owner;
                            var source=state.Source!=null&&objects.TryGetValue(state.Source,out var found)?found.gameObject:OtherworldActions.Player(sourceOwner)?.gameObject;
                            stateModule.当前状态列表.Add(new OtherworldState{Owner=sourceOwner,Space=unit.Space,攻击者=source,状态类型=(攻击类型与状态数据库.状态枚举)state.Type,
                                剩余时间=state.Remaining,加成属性=Unpack(state.Values),自身抗性属性=new 模块_抗性属性.状态抗性单元{当前效果抗性=state.EffectResistance,当前时间抗性=state.TimeResistance}});
                        }
                    }
                    var effects=unit.GetComponent<模块_状态持续效果>();if(effects!=null)effects.当前底层状态列表=saved.Underlying.Select(s=>new 模块_状态持续效果.底层状态类{底层状态=s.底层状态,数值=s.数值,持续时间=s.持续时间}).ToList();
                    var manager=unit.GetComponent<模块_加成管理器>();
                    if(manager!=null)
                    {
                        manager.状态加成列表.Clear();
                        foreach(var buff in saved.Buffs)
                            if(buff.持续时间>0)manager.临时加成(buff.加成名称,buff.加成列表,buff.持续时间);else manager.增加加成(buff.加成名称,buff.加成列表);
                    }
                    var lifetime=unit.GetComponent<词条_100098_限时自杀>();if(lifetime!=null&&saved.Lifetime>=0)lifetime.自杀倒计时=saved.Lifetime;
                    unit.WarmUntil=Time.time+saved.Warm;unit.RallyUntil=Time.time+saved.Rally;
                    unit.Health.运算生命值上限();unit.Health.运算抗性();unit.Health.运算格挡值();unit.Health.运算无敌();
                    unit.Health.当前生命值=Mathf.Min(unit.Health.当前生命值上限,saved.Health);
                    unit.Attack?.运算攻击力();unit.Attack?.运算攻击速度();unit.Attack?.运算攻击范围();
                    if(unit.Attack!=null)unit.Attack.当前攻击倒计时=saved.Cooldown;
                    var room=Room(unit.Space);room.Dirty=true;room.NavigationDirty=true;
                }
                CacheDropPrefabs();
                foreach(var savedRoom in SavedRooms)
                {
                    int owner=Plugin.Role==0&&savedRoom.Owner==1?0:Plugin.Role==1&&savedRoom.Owner==0?1:savedRoom.Owner;var room=Ensure(owner);
                    foreach(var drop in savedRoom.Drops)
                        if(dropPrefabs.TryGetValue(drop.Prefab,out var prefab))
                        {var obj=Object.Instantiate(prefab,room.Center+new Vector2(drop.X,drop.Y),Quaternion.identity);if(Plugin.Role==1&&obj.GetComponent<NetworkIdentity>()!=null)NetworkServer.Spawn(obj);KamuiDrop.Attach(obj,drop.Key);}
                }
                foreach(var pair in pending)if(pair.Value!=null)pair.Value.Restoring=false;
                KamuiVisibility.Refresh();
            }
            finally
            {
                foreach(var pair in pending)if(pair.Value!=null)pair.Value.Restoring=false;
                if(SceneLifecycle.IsCurrent(scene))Restoring=false;
            }
        }
        private static void CacheDropPrefabs()
        {
            if(Plugin.Units==null)return;
            foreach(var entry in Plugin.Units.单位列表)
            {
                var prefab=entry.单位预制体;if(prefab==null)continue;
                var loot=prefab.GetComponent<词条_6_死亡掉落>();var currency=prefab.GetComponent<词条_100004_死亡掉落_货币>();
                if(loot!=null)foreach(var drop in loot.随机掉落物)if(drop!=null)dropPrefabs[drop.name]=drop;
                if(currency!=null)foreach(var drop in currency.随机掉落物)if(drop!=null)dropPrefabs[drop.name]=drop;
            }
        }
        internal static GameObject DropPrefab(string name)
        {if(string.IsNullOrEmpty(name))return null;if(!dropPrefabs.TryGetValue(name,out var prefab)||prefab==null){CacheDropPrefabs();dropPrefabs.TryGetValue(name,out prefab);}return prefab;}
    }
    [HarmonyPatch]
    internal static class KamuiBeforeSceneChange
    {
        private static IEnumerable<MethodBase> TargetMethods()=>typeof(函数库.场景函数).GetMethods(BindingFlags.Public|BindingFlags.Static).Where(m=>m.Name=="加载场景")
            .Concat(new[]{AccessTools.Method(typeof(Steam联机管理器),"切换场景_联机",new[]{typeof(int)})});
        private static void Prefix()=>KamuiWorld.Capture();
    }
}
