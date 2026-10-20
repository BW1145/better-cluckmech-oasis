using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

namespace CluckmechCoop
{
    // One index per restore snapshot; never cache across changing prefab registries.
    internal sealed class KamuiPrefabs
    {
        private sealed class Template
        {
            internal GameObject Prefab;
            internal int Id;
            internal bool Temporary;
        }
        // Buckets retain collisions so ambiguous identities still fail closed.
        private readonly Dictionary<uint,List<Template>> byAsset=new Dictionary<uint,List<Template>>();
        private readonly Dictionary<string,List<Template>> byName=new Dictionary<string,List<Template>>(StringComparer.Ordinal);
        private readonly Dictionary<int,List<Template>> byId=new Dictionary<int,List<Template>>();
        internal KamuiPrefabs()
        {
            var known=new HashSet<GameObject>();
            if(NetworkManager.singleton!=null)foreach(var prefab in NetworkManager.singleton.spawnPrefabs)known.Add(prefab);
            foreach(var prefab in NetworkClient.prefabs.Values)known.Add(prefab);
            if(Plugin.Units!=null)foreach(var entry in Plugin.Units.单位列表)known.Add(entry.单位预制体);
            foreach(var prefab in known)
            {
                if(prefab==null)continue;
                var health=prefab.GetComponent<模块_基本属性>();if(health==null)continue;
                var entry=new Template{Prefab=prefab,Id=health.ID,Temporary=Temporary(prefab)};
                uint asset=Asset(prefab);
                if(asset!=0)Add(byAsset,asset,entry);
                Add(byName,Name(prefab),entry);
                Add(byId,entry.Id,entry);
            }
        }
        private static void Add<TKey>(Dictionary<TKey,List<Template>> index,TKey key,Template entry)
        {
            if(!index.TryGetValue(key,out var bucket))index.Add(key,bucket=new List<Template>());
            bucket.Add(entry);
        }
        internal static bool Temporary(GameObject obj)
        {
            if(obj.GetComponent<词条_100001_毒气带倒计时>()!=null)return true;
            var health=obj.GetComponent<模块_基本属性>();
            return obj.GetComponent<词条_100034_定时销毁>()!=null&&health!=null&&
                (health.单位类型==模块_基本属性.单位类型枚举.陷阱||health.单位类型==模块_基本属性.单位类型枚举.场景);
        }
        private static uint Asset(GameObject obj)=>obj.GetComponent<NetworkIdentity>()?.assetId??0;
        internal static string Name(GameObject obj)=>obj.name.EndsWith("(Clone)",StringComparison.Ordinal)?obj.name.Substring(0,obj.name.Length-7):obj.name;
        internal static void Identify(GameObject obj,KamuiSavedUnit saved)
        {saved.AssetId=Asset(obj);saved.PrefabName=Name(obj);}
        internal static void RetainPending(List<KamuiSavedRoom> current,List<KamuiSavedRoom> previous,ISet<string> pending)
        {
            var present=new HashSet<string>(current.SelectMany(r=>r.Units).Select(u=>u.Key));
            foreach(var old in previous)
            {
                var room=current.FirstOrDefault(r=>r.Owner==old.Owner);
                foreach(var unit in old.Units)
                {
                    if(!pending.Contains(unit.Key)||!present.Add(unit.Key))continue;
                    if(room==null){room=new KamuiSavedRoom{Owner=old.Owner};current.Add(room);}
                    room.Units.Add(unit);
                }
            }
        }
        internal GameObject Resolve(KamuiSavedUnit saved,out bool temporary)
        {
            temporary=false;
            List<Template> candidates;
            bool legacy=saved.AssetId==0&&string.IsNullOrEmpty(saved.PrefabName);
            if(saved.AssetId!=0)
            {if(!byAsset.TryGetValue(saved.AssetId,out candidates))return null;}
            else if(!legacy)
            {if(!byName.TryGetValue(saved.PrefabName,out candidates))return null;}
            else if(!byId.TryGetValue(saved.Id,out candidates))return null;
            int count=0;bool allTemporary=true;GameObject match=null;
            foreach(var entry in candidates)
            {
                if(entry.Prefab==null||entry.Id!=saved.Id||legacy&&!LegacyMatches(saved,entry.Prefab))continue;
                count++;match=entry.Prefab;allTemporary&=entry.Temporary;
            }
            temporary=count>0&&allTemporary;
            return !temporary&&count==1?match:null;
        }
        private static bool LegacyMatches(KamuiSavedUnit saved,GameObject prefab)
        {
            // These native template fields distinguish faction, child bodies and egg stages.
            // Never guess from ID, current faction, or current health alone.
            if(!saved.Components.TryGetValue(nameof(模块_基本属性),out var values))return false;
            var hp=prefab.GetComponent<模块_基本属性>();
            foreach(string name in new[]{"初始阵营","单位类型","单位分类","生命值"})
            {
                var field=typeof(模块_基本属性).GetField(name);
                if(field==null||!values.TryGetValue(name,out var value)||value.Length!=1||Convert.ToSingle(field.GetValue(hp))!=value[0])return false;
            }
            foreach(string name in new[]{nameof(模块_攻击属性),nameof(模块_移动属性)})
            {
                if(!saved.Components.TryGetValue(name,out var scalars))continue;
                var type=typeof(模块_基本属性).Assembly.GetType(name);var component=prefab.GetComponent(type);
                if(component==null)return false;
                foreach(string fieldName in name==nameof(模块_攻击属性)?new[]{"攻击力","攻击速度","攻击范围"}:new[]{"移动速度"})
                    if(scalars.TryGetValue(fieldName,out var value)&&value.Length==1&&Convert.ToSingle(type.GetField(fieldName).GetValue(component))!=value[0])return false;
            }
            return true;
        }
    }
}
