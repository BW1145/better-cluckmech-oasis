using System.Collections.Generic;
using HarmonyLib;
using Mirror;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class KamuiVisibility
    {
        private static KamuiInterest interest;
        private static readonly Dictionary<uint,GameObject> hidden=new Dictionary<uint,GameObject>();
        private static readonly HashSet<GameObject> restored=new HashSet<GameObject>();
        private static readonly Dictionary<Renderer,bool> previous=new Dictionary<Renderer,bool>();
        private static readonly Dictionary<Canvas,bool> previousCanvases=new Dictionary<Canvas,bool>();
        private static bool registered;
        internal static void Install()
        {
            if(Plugin.Role==1&&interest==null)
            {
                var prior=NetworkServer.aoi;
                interest=Plugin.Instance.gameObject.AddComponent<KamuiInterest>();interest.Previous=prior;
            }
            if(Plugin.Role==0||registered||!NetworkClient.active)return;
            registered=true;
            var prefabs=new HashSet<GameObject>();
            if(Plugin.Units!=null)foreach(var unit in Plugin.Units.单位列表)if(unit.单位预制体!=null)prefabs.Add(unit.单位预制体);
            var heroes=Object.FindObjectOfType<yx英雄库>();
            if(heroes!=null)foreach(var hero in heroes.英雄列表)if(hero.角色数据.单位预制体!=null)prefabs.Add(hero.角色数据.单位预制体);
            foreach(var prefab in prefabs)
            {
                var identity=prefab.GetComponent<NetworkIdentity>();if(identity==null||identity.assetId==0)continue;
                NetworkClient.UnregisterPrefab(prefab);
                NetworkClient.RegisterPrefab(prefab,(SpawnMessage message)=>Spawn(prefab,message),Unspawn);
            }
        }
        private static GameObject Spawn(GameObject prefab,SpawnMessage message)
        {
            GameObject obj;
            if(hidden.TryGetValue(message.netId,out obj)&&obj!=null)
            {hidden.Remove(message.netId);restored.Add(obj);obj.transform.SetPositionAndRotation(message.position,message.rotation);obj.SetActive(true);}
            else obj=Object.Instantiate(prefab,message.position,message.rotation);
            return obj;
        }
        private static void Unspawn(GameObject obj)
        {
            var unit=obj.GetComponent<OtherworldUnit>();
            if(unit!=null&&unit.Space!=0&&unit.Health.当前生命值>0)
            {hidden[obj.GetComponent<NetworkIdentity>().netId]=obj;obj.SetActive(false);}
            else Object.Destroy(obj);
        }
        internal static bool Restored(GameObject obj)=>restored.Remove(obj);
        internal static GameObject Find(uint id)=>NetworkClient.spawned.TryGetValue(id,out var identity)?identity.gameObject:hidden.TryGetValue(id,out var obj)?obj:null;
        internal static bool Observe(NetworkIdentity identity,NetworkConnectionToClient connection)
        {
            var unit=OtherworldWorld.Unit(identity.gameObject);
            int space=unit?.Space??KamuiWorld.At(identity.transform.position);
            if(space==0||unit!=null&&(unit.IsPlayer||unit.BossGroup)||connection==NetworkServer.localConnection)return true;
            int owner=space-1;var player=owner==1?Steam联机管理器.玩家_主机:Steam联机管理器.玩家_客户端;
            return player!=null&&player.connectionToClient==connection;
        }
        internal static void Refresh()
        {
            int space=OtherworldWorld.Space(OtherworldActions.Player(Plugin.Role)?.gameObject);
            foreach(var unit in OtherworldWorld.Units.Values)
            {
                if(unit==null)continue;
                bool visible=unit.Space==space;
                foreach(var renderer in unit.GetComponentsInChildren<Renderer>(true))
                {
                    if(!visible)
                    {
                        if(!previous.ContainsKey(renderer))previous[renderer]=renderer.forceRenderingOff;
                        renderer.forceRenderingOff=true;
                    }
                    else if(previous.TryGetValue(renderer,out bool prior)){renderer.forceRenderingOff=prior;previous.Remove(renderer);}
                }
                foreach(var canvas in unit.GetComponentsInChildren<Canvas>(true))
                {
                    if(!visible){if(!previousCanvases.ContainsKey(canvas))previousCanvases[canvas]=canvas.enabled;canvas.enabled=false;}
                    else if(previousCanvases.TryGetValue(canvas,out bool prior)){canvas.enabled=prior;previousCanvases.Remove(canvas);}
                }
            }
        }
        internal static void Forget(GameObject obj)
        {
            foreach(var renderer in obj.GetComponentsInChildren<Renderer>(true))previous.Remove(renderer);
            foreach(var canvas in obj.GetComponentsInChildren<Canvas>(true))previousCanvases.Remove(canvas);
            restored.Remove(obj);
        }
        internal static void Clear()
        {
            foreach(var obj in hidden.Values)if(obj!=null)Object.Destroy(obj);
            hidden.Clear();restored.Clear();
            foreach(var pair in previous)if(pair.Key!=null)pair.Key.forceRenderingOff=pair.Value;
            previous.Clear();registered=false;
            foreach(var pair in previousCanvases)if(pair.Key!=null)pair.Key.enabled=pair.Value;
            previousCanvases.Clear();
            if(interest!=null)
            {
                if(NetworkServer.aoi==interest)NetworkServer.aoi=interest.Previous;
                if(NetworkClient.aoi==interest)NetworkClient.aoi=interest.Previous;
                Object.Destroy(interest);interest=null;
            }
        }
    }
    internal sealed class KamuiInterest:InterestManagement
    {
        internal InterestManagementBase Previous;
        public override bool OnCheckObserver(NetworkIdentity identity,NetworkConnectionToClient observer)
            =>(Previous==null||Previous.OnCheckObserver(identity,observer))&&KamuiVisibility.Observe(identity,observer);
        public override void OnRebuildObservers(NetworkIdentity identity,HashSet<NetworkConnectionToClient> observers)
        {foreach(var connection in NetworkServer.connections.Values)if(connection.isReady&&OnCheckObserver(identity,connection))observers.Add(connection);}
        public override void SetHostVisibility(NetworkIdentity identity,bool visible)=>KamuiVisibility.Refresh();
    }
    [HarmonyPatch(typeof(联机_单位属性同步),"OnStartClient")]
    internal static class KamuiObserverRestore
    {private static bool Prefix(联机_单位属性同步 __instance)=>!KamuiVisibility.Restored(__instance.gameObject);}
    [HarmonyPatch(typeof(NetworkServer),"Spawn",new[]{typeof(GameObject),typeof(GameObject)})]
    internal static class KamuiBeforeNetworkSpawn
    {
        private static void Prefix(GameObject obj)
        {
            if(!OtherworldWorld.Active||obj==null)return;
            var health=obj.GetComponent<模块_基本属性>();if(health==null)return;
            var sync=obj.GetComponent<联机_单位属性同步>();if(sync!=null)health.所属玩家=sync.所属玩家;
            OtherworldWorld.Add(health);var unit=OtherworldWorld.Unit(obj);
            if(unit!=null&&(obj.GetComponent<词条_100028_Boss胜利判断>()!=null||obj.GetComponent<最终boss状态机>()!=null||obj.GetComponent<模块_BOSS生命显示>()!=null))unit.BossGroup=true;
        }
    }
}
