using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class HarmonyPatch:Attribute
    {public HarmonyPatch(Type type,string name){} public HarmonyPatch(Type type,string name,Type[] args){}}
}
namespace UnityEngine
{
    public class Object
    {
        public bool Destroyed;
        public static void Destroy(Object obj){if(obj!=null)obj.Destroyed=true;}
        public static T FindObjectOfType<T>() where T:class=>null;
        public static GameObject Instantiate(GameObject prefab,Vector3 position,Quaternion rotation)=>new GameObject();
    }
    public struct Vector3 {public float x,y,z;}
    public struct Quaternion {}
    public class Component:Object
    {
        public GameObject gameObject;public Transform transform=>gameObject.transform;
        public T GetComponent<T>() where T:class=>gameObject.GetComponent<T>();
        public void GetComponentsInChildren<T>(bool inactive,List<T> result) where T:class=>gameObject.GetComponentsInChildren(inactive,result);
    }
    public class MonoBehaviour:Component {}
    public class Renderer:Component {public bool forceRenderingOff,enabled=true;}
    public class Canvas:Component {public bool enabled=true;}
    public class Transform:Component
    {
        public Vector3 position;public List<Transform> Children=new List<Transform>();
        public void SetPositionAndRotation(Vector3 p,Quaternion r){position=p;}
        public void Add(GameObject child)
        {Children.Add(child.transform);gameObject.Message("OnTransformChildrenChanged");}
    }
    public class GameObject:Object
    {
        public readonly Transform transform;public bool activeSelf=true;public int Scans;
        private readonly List<Component> components=new List<Component>();
        public GameObject(){transform=new Transform{gameObject=this};components.Add(transform);}
        public T GetComponent<T>() where T:class=>components.OfType<T>().FirstOrDefault();
        public T AddComponent<T>() where T:Component,new(){var component=new T{gameObject=this};components.Add(component);return component;}
        public void SetActive(bool active){activeSelf=active;}
        public void Message(string name)
        {foreach(var component in components.ToArray())component.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(component,null);}
        public void GetComponentsInChildren<T>(bool inactive,List<T> result) where T:class
        {Scans++;Collect(result);}
        private void Collect<T>(List<T> result) where T:class
        {result.AddRange(components.OfType<T>());foreach(var child in transform.Children)child.gameObject.Collect(result);}
    }
}
namespace Mirror
{
    public interface NetworkMessage {}
    public class NetworkIdentity:UnityEngine.Component {public uint netId,assetId;public NetworkConnectionToClient connectionToClient;}
    public class NetworkConnectionToClient {public bool isReady=true;}
    public class InterestManagementBase:UnityEngine.MonoBehaviour
    {
        public virtual bool OnCheckObserver(NetworkIdentity id,NetworkConnectionToClient connection)=>true;
        public virtual void SetHostVisibility(NetworkIdentity id,bool visible){id.GetComponent<UnityEngine.Renderer>().enabled=visible;}
    }
    public abstract class InterestManagement:InterestManagementBase
    {public abstract void OnRebuildObservers(NetworkIdentity id,HashSet<NetworkConnectionToClient> observers);}
    public struct SpawnMessage {public uint netId;public UnityEngine.Vector3 position;public UnityEngine.Quaternion rotation;}
    public class NetworkWriter {public void WriteUInt(uint x){} public void WriteString(string x){}}
    public class NetworkReader {public uint ReadUInt()=>0;public string ReadString()=>null;}
    public static class Writer<T> {public static Action<NetworkWriter,T> write;}
    public static class Reader<T> {public static Func<NetworkReader,T> read;}
    public static class NetworkServer
    {
        public static bool active;public static InterestManagementBase aoi;public static NetworkConnectionToClient localConnection;
        public static readonly Dictionary<int,NetworkConnectionToClient> connections=new Dictionary<int,NetworkConnectionToClient>();
        public static readonly List<object> Sent=new List<object>();
        public static void SendToAll<T>(T message){Sent.Add(message);}
        public static void Destroy(UnityEngine.GameObject obj){}
    }
    public static class NetworkClient
    {
        public static bool active=true;public static InterestManagementBase aoi;
        public static readonly Dictionary<uint,NetworkIdentity> spawned=new Dictionary<uint,NetworkIdentity>();
        public static readonly Dictionary<Type,Delegate> Handlers=new Dictionary<Type,Delegate>();
        public static void RegisterHandler<T>(Action<T> handler){Handlers[typeof(T)]=handler;}
        public static void UnregisterHandler<T>(){Handlers.Remove(typeof(T));}
        public static void UnregisterPrefab(UnityEngine.GameObject prefab){}
        public static void RegisterPrefab(UnityEngine.GameObject prefab,Func<SpawnMessage,UnityEngine.GameObject> spawn,Action<UnityEngine.GameObject> unspawn){}
    }
}
public class \u6a21\u5757_\u57fa\u672c\u5c5e\u6027:UnityEngine.Component
{public float \u5f53\u524d\u751f\u547d\u503c=10;public int \u6240\u5c5e\u73a9\u5bb6;}
public class \u8054\u673a_\u5355\u4f4d\u5c5e\u6027\u540c\u6b65:UnityEngine.Component {public int \u6240\u5c5e\u73a9\u5bb6;}
public class \u8bcd\u6761_100028_Boss\u80dc\u5229\u5224\u65ad:UnityEngine.Component {}
public class \u6700\u7ec8boss\u72b6\u6001\u673a:UnityEngine.Component {}
public class \u6a21\u5757_BOSS\u751f\u547d\u663e\u793a:UnityEngine.Component {}
public static class Steam\u8054\u673a\u7ba1\u7406\u5668
{public static Mirror.NetworkIdentity \u73a9\u5bb6_\u4e3b\u673a,\u73a9\u5bb6_\u5ba2\u6237\u7aef;}
public class Prefab {public UnityEngine.GameObject \u5355\u4f4d\u9884\u5236\u4f53;}
public class Hero {public Prefab \u89d2\u8272\u6570\u636e;}
public class yx\u82f1\u96c4\u5e93 {public List<Hero> \u82f1\u96c4\u5217\u8868=new List<Hero>();}
namespace CluckmechCoop
{
    internal sealed class OtherworldUnit:UnityEngine.MonoBehaviour {internal int Space;internal bool IsPlayer,BossGroup;internal \u6a21\u5757_\u57fa\u672c\u5c5e\u6027 Health;}
    internal static class OtherworldWorld
    {
        internal static bool Active=true;internal static Dictionary<UnityEngine.GameObject,OtherworldUnit> Units=new Dictionary<UnityEngine.GameObject,OtherworldUnit>();
        internal static OtherworldUnit Unit(UnityEngine.GameObject obj)=>obj!=null&&Units.TryGetValue(obj,out var unit)?unit:null;
        internal static int Space(UnityEngine.GameObject obj)=>Unit(obj)?.Space??0;
        internal static void Add(\u6a21\u5757_\u57fa\u672c\u5c5e\u6027 health){Units[health.gameObject]=health.GetComponent<OtherworldUnit>();}
    }
    internal static class OtherworldActions {internal static OtherworldUnit Local;internal static OtherworldUnit Player(int role)=>Local;}
    internal static class KamuiWorld {internal static int At(UnityEngine.Vector3 point)=>0;}
    internal class UnitPrefabs {public List<Prefab> \u5355\u4f4d\u5217\u8868=new List<Prefab>();}
    internal static class Plugin
    {
        internal static int Role=2;internal static UnityEngine.Component Instance=new UnityEngine.GameObject().transform;
        internal static UnitPrefabs Units;internal static CombatState Combat=new CombatState();
    }
    internal class CombatState {internal string Token="current-run";}
}
