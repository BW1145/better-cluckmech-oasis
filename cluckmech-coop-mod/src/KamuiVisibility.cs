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
        private static readonly HashSet<uint> destroyed=new HashSet<uint>();
        private static readonly Dictionary<GameObject,KamuiVisibilityView> views=new Dictionary<GameObject,KamuiVisibilityView>();
        private static KamuiVisibilityRefresh refresh;
        private static bool pendingRefresh;
        private static bool prefabsRegistered;
        private static bool messagesInitialized, messagesRegistered;

        internal static void RegisterMessages()
        {
            if (!messagesInitialized)
            {
                Writer<KamuiDestroyedMessage>.write = (writer, message) =>
                {
                    writer.WriteUInt(message.NetId);
                    writer.WriteString(message.Token);
                };
                Reader<KamuiDestroyedMessage>.read = reader => new KamuiDestroyedMessage
                {
                    NetId = reader.ReadUInt(),
                    Token = reader.ReadString()
                };
                messagesInitialized = true;
            }
            if (NetworkClient.active && !messagesRegistered)
            {
                NetworkClient.RegisterHandler<KamuiDestroyedMessage>(Destroyed);
                messagesRegistered = true;
            }
        }

        internal static void Install()
        {
            RegisterMessages();
            if(refresh==null)refresh=Plugin.Instance.gameObject.AddComponent<KamuiVisibilityRefresh>();
            if(Plugin.Role==1&&interest==null)
            {
                var prior=NetworkServer.aoi;
                interest=Plugin.Instance.gameObject.AddComponent<KamuiInterest>();interest.Previous=prior;
            }
            if(Plugin.Role==0||prefabsRegistered||!NetworkClient.active)return;
            prefabsRegistered=true;
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
            {
                hidden.Remove(message.netId);restored.Add(obj);
                obj.transform.SetPositionAndRotation(message.position,message.rotation);obj.SetActive(true);
                var health=obj.GetComponent<模块_基本属性>();
                if(health!=null)OtherworldWorld.Add(health);
                Update(obj);
            }
            else {hidden.Remove(message.netId);obj=Object.Instantiate(prefab,message.position,message.rotation);}
            return obj;
        }
        private static void Unspawn(GameObject obj)
        {
            var identity=obj.GetComponent<NetworkIdentity>();
            if(identity!=null&&destroyed.Remove(identity.netId)){Forget(obj);Object.Destroy(obj);return;}
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
            if(refresh==null)refresh=Plugin.Instance.gameObject.AddComponent<KamuiVisibilityRefresh>();
            pendingRefresh=true;
        }
        internal static void Flush()
        {
            if(!pendingRefresh)return;
            pendingRefresh=false;
            int space=OtherworldWorld.Space(OtherworldActions.Player(Plugin.Role)?.gameObject);
            foreach(var unit in OtherworldWorld.Units.Values)if(unit!=null)Update(unit.gameObject,space);
        }
        internal static void Update(GameObject obj)
            =>Update(obj,OtherworldWorld.Space(OtherworldActions.Player(Plugin.Role)?.gameObject));
        private static void Update(GameObject obj,int space)
        {
            var unit=OtherworldWorld.Unit(obj);if(unit==null)return;
            if(!views.TryGetValue(obj,out var view)||view==null)
            {view=obj.GetComponent<KamuiVisibilityView>()??obj.AddComponent<KamuiVisibilityView>();views[obj]=view;}
            view.SetVisible(unit.Space==space);
        }
        // Disable also happens when Mirror hides a live unit; it releases render leases, never its hidden object.
        internal static void Removed(OtherworldUnit unit)
        {
            if(unit!=null)ReleaseView(unit.gameObject);
        }
        private static void ReleaseView(GameObject obj)
        {
            if(views.TryGetValue(obj,out var view)){if(view!=null)view.Release();views.Remove(obj);}
        }
        internal static void ServerDestroyed(GameObject obj)
        {
            if(Plugin.Role!=1||!NetworkServer.active||Plugin.Network?.PeerReady!=true||obj==null||obj.GetComponent<OtherworldUnit>()==null)return;
            var identity=obj.GetComponent<NetworkIdentity>();if(identity==null||identity.netId==0)return;
            RegisterMessages();
            // Destroy normally goes only to current observers; hidden clients still own an inactive instance.
            NetworkServer.SendToAll(new KamuiDestroyedMessage{NetId=identity.netId,Token=Plugin.Combat.Token});
        }
        private static void Destroyed(KamuiDestroyedMessage message)
        {
            if(Plugin.Role!=2||message.Token!=Plugin.Combat.Token)return;
            if(!hidden.TryGetValue(message.NetId,out var obj))
            {
                // The native destroy can arrive next while the client's last health value is still positive.
                if(NetworkClient.spawned.ContainsKey(message.NetId))destroyed.Add(message.NetId);
                return;
            }
            hidden.Remove(message.NetId);
            if(obj!=null){Forget(obj);Object.Destroy(obj);}
        }
        internal static void Forget(GameObject obj)
        {
            ReleaseView(obj);
            restored.Remove(obj);
            uint found=0;foreach(var pair in hidden)if(pair.Value==obj){found=pair.Key;break;}
            if(found!=0)hidden.Remove(found);
        }
        internal static void Clear()
        {
            var retired=new List<GameObject>(hidden.Values);hidden.Clear();
            foreach(var obj in retired)if(obj!=null)Object.Destroy(obj);
            restored.Clear();destroyed.Clear();
            foreach(var view in views.Values)if(view!=null)view.Release();views.Clear();
            prefabsRegistered=false;pendingRefresh=false;
            if(refresh!=null)Object.Destroy(refresh);refresh=null;
            if(interest!=null)
            {
                if(NetworkServer.aoi==interest)NetworkServer.aoi=interest.Previous;
                if(NetworkClient.aoi==interest)NetworkClient.aoi=interest.Previous;
                Object.Destroy(interest);interest=null;
            }
        }

        internal static void Disconnect()
        {
            Clear();
            if (messagesRegistered)
                NetworkClient.UnregisterHandler<KamuiDestroyedMessage>();
            messagesRegistered = false;
        }
    }
    internal sealed class KamuiInterest:InterestManagement
    {
        internal InterestManagementBase Previous;
        public override bool OnCheckObserver(NetworkIdentity identity,NetworkConnectionToClient observer)
            =>(Previous==null||Previous.OnCheckObserver(identity,observer))&&KamuiVisibility.Observe(identity,observer);
        public override void OnRebuildObservers(NetworkIdentity identity,HashSet<NetworkConnectionToClient> observers)
        {foreach(var connection in NetworkServer.connections.Values)if(connection.isReady&&OnCheckObserver(identity,connection))observers.Add(connection);}
        public override void SetHostVisibility(NetworkIdentity identity,bool visible)
        {
            if(OtherworldWorld.Unit(identity.gameObject)!=null)KamuiVisibility.Update(identity.gameObject);
            else if(Previous!=null)Previous.SetHostVisibility(identity,visible);
            else base.SetHostVisibility(identity,visible);
        }
    }
    internal struct KamuiDestroyedMessage:NetworkMessage {internal uint NetId;internal string Token;}
    internal sealed class KamuiVisibilityRefresh:MonoBehaviour {private void LateUpdate()=>KamuiVisibility.Flush();}
    [HarmonyPatch(typeof(NetworkServer),nameof(NetworkServer.Destroy),new[]{typeof(GameObject)})]
    internal static class KamuiBeforeNetworkDestroy
    {private static void Prefix(GameObject obj)=>KamuiVisibility.ServerDestroyed(obj);}
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
