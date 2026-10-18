using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CluckmechCoop
{
    internal sealed class KamuiDrop:MonoBehaviour
    {
        internal string Key;
        internal int Space;
        internal bool Collected;
        private bool networked;
        internal static readonly Dictionary<string,KamuiDrop> Live=new Dictionary<string,KamuiDrop>();
        internal static void Attach(GameObject obj,string key=null)
        {
            int space=KamuiWorld.At(obj.transform.position);if(space==0)return;
            var drop=obj.GetComponent<KamuiDrop>();if(drop!=null)return;
            drop=obj.AddComponent<KamuiDrop>();drop.Space=space;
            var identity=obj.GetComponent<NetworkIdentity>();drop.networked=Plugin.Role!=0&&identity!=null;
            drop.Key=drop.networked?"network."+identity.netId:key??Guid.NewGuid().ToString("N");Live[drop.Key]=drop;
            if(Plugin.Role==1&&space-1!=Plugin.Role)drop.Send();
        }
        internal KamuiSavedDrop Save(KamuiRoom room)=>new KamuiSavedDrop{Key=Key,Prefab=name.Replace("(Clone)",""),X=transform.position.x-room.Center.x,Y=transform.position.y-room.Center.y};
        private void Send(){if(!networked)OtherworldNetwork.Send(new OtherworldMessage{Kind=OtherworldMessageKind.Drop,Value=Space-1,Data=JsonConvert.SerializeObject(Save(KamuiWorld.Room(Space)))},Space-1);}
        internal static void SendPending(int owner)
        {foreach(var drop in Live.Values)if(drop!=null&&drop.Space==owner+1&&!drop.Collected)drop.Send();}
        internal static void Receive(string data,int owner)
        {
            if(owner!=Plugin.Role||string.IsNullOrEmpty(data))return;
            KamuiSavedDrop record;try{record=JsonConvert.DeserializeObject<KamuiSavedDrop>(data);}catch(JsonException){return;}
            if(record==null||string.IsNullOrEmpty(record.Key)||Live.ContainsKey(record.Key)||float.IsNaN(record.X)||float.IsInfinity(record.X)||float.IsNaN(record.Y)||float.IsInfinity(record.Y))return;
            var room=KamuiWorld.Ensure(owner);var prefab=KamuiWorld.DropPrefab(record.Prefab);if(prefab==null)return;
            var obj=Object.Instantiate(prefab,room.Center+new Vector2(record.X,record.Y),Quaternion.identity);Attach(obj,record.Key);
        }
        internal static void Collect(int owner,string key)
        {
            if(key==null||!Live.TryGetValue(key,out var drop)||drop==null||drop.Space!=owner+1)return;
            drop.Collected=true;StratagemWorld.DestroyUnit(drop.gameObject);
        }
        private void OnDestroy()
        {
            if(Key!=null&&Live.TryGetValue(Key,out var existing)&&existing==this)Live.Remove(Key);
            if(Collected&&Plugin.Role==2)OtherworldNetwork.CollectDrop(Key);
        }
    }
    [HarmonyPatch(typeof(模块_拾取精灵_移动),"Start")]
    internal static class KamuiDropReady
    {private static void Postfix(模块_拾取精灵_移动 __instance)=>KamuiDrop.Attach(__instance.gameObject);}

    [HarmonyPatch]
    internal static class KamuiDropPickup
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {foreach(var type in new[]{typeof(模块_拾取精灵_货币),typeof(模块_拾取精灵_动力),typeof(模块_拾取精灵_回血)})yield return AccessTools.Method(type,"OnTriggerStay2D");}
        private static bool Prefix(Component __instance,Collider2D col)
        {
            var drop=__instance.GetComponent<KamuiDrop>();if(drop==null)return true;
            if(drop.Collected||Plugin.Role==1&&drop.Space-1!=Plugin.Role)return false;
            if(__instance is 模块_拾取精灵_回血)
            {
                var owner=OtherworldActions.Player(drop.Space-1);
                if(owner==null||owner.Space!=drop.Space||owner.Death.是否玩家死亡)return false;
            }
            if(OtherworldWorld.Space(col.gameObject)!=drop.Space)return false;
            if(col.CompareTag("友方")&&(__instance is 模块_拾取精灵_回血||col.name=="玩家"))drop.Collected=true;
            return true;
        }
    }
    [HarmonyPatch]
    internal static class KamuiDropGeneration
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {yield return AccessTools.Method(typeof(词条_6_死亡掉落),"死亡触发");yield return AccessTools.Method(typeof(词条_100004_死亡掉落_货币),"死亡触发");}
        private static bool Prefix(Component __instance)=>Plugin.Role!=2||OtherworldWorld.Space(__instance.gameObject)==0;
    }
}
