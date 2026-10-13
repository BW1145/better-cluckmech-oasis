using System;
using System.Collections.Generic;
using System.Reflection;
using CluckmechCoop;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEngine
{
    public class Object { public static T FindObjectOfType<T>() { var root=GameObject.Find("menu");return root==null?default:root.GetComponent<T>(); } }
    public class Component : Object { public GameObject gameObject; }
    public class GameObject
    {
        static readonly Dictionary<string,GameObject> objects = new();
        readonly Dictionary<Type,Component> components = new();
        public string name;
        public GameObject(string name) { this.name=name; objects[name]=this; }
        public static void Clear() => objects.Clear();
        public static GameObject Find(string name) => objects.GetValueOrDefault(name);
        public T GetComponent<T>() => components.TryGetValue(typeof(T),out var value)?(T)(object)value:default;
        public Component GetComponent(Type type) => components.GetValueOrDefault(type);
        public Component AddComponent(Type type) { var value=(Component)Activator.CreateInstance(type); value.gameObject=this; components[type]=value; return value; }
    }
}
namespace UnityEngine.SceneManagement { public struct Scene { public int buildIndex; } public static class SceneManager { public static int Index; public static Scene GetActiveScene()=>new(){buildIndex=Index}; } }
namespace UnityEngine.UI
{
    public class Text { public string text; }
    public class Button { public bool interactable=true; public Text Label=new(); public T GetComponentInChildren<T>()=>(T)(object)Label; }
}
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)] public class HarmonyPatch:Attribute { public HarmonyPatch(Type type,string name){} public HarmonyPatch(Type type,string name,Type[] args){} }
    public static class AccessTools { public static MethodInfo Method(Type type,string name)=>type.GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public); }
}
namespace 函数库 { public static class 常数类 { public const int 场景id_开始菜单=0,场景id_选择地图=4; public const string 命名_总结="总结",存档_存档加密密码="fixture"; } }
public class 存档脚本:Component
{
    public string 文件名="danju",后缀=".cd";
    public List<Entry> 物体组件列表=new(),物体列表=new();
    public class Entry { public string 物体名称; public List<string> 脚本组件列表=new(); }
    public int CacheCalls;
    bool 存档载入内存(){CacheCalls++;return true;}
}
public class 总结脚本:Component
{
    public int 当前所选英雄id,当前金钱数,当前动力炉等级;
    public float 当前基地血量;
    public Statistics 单局统计数据=new();
    public List<Item> 当前拥有炮台=new();
    public class Statistics { public object 角色初始天赋; }
    public class Item { public int 炮台id,强化上限; public List<int> 强化词条=new(); }
}
public class Route:Component { public string Node; }
public class 场景切换按钮{}
public class 开始菜单继续游戏:Component { public UnityEngine.UI.Button 按钮=new(); }
public class ES3Settings { public ES3.EncryptionType encryptionType;public string encryptionPassword;public ES3Settings(ES3.Location location){} }
public static class ES3
{
    public enum Location { Cache } public enum EncryptionType { AES }
    public static readonly Dictionary<string,Action<object>> Data=new();
    public static readonly Dictionary<string,int> Reads=new();
    public static bool KeyExists(string key,string path,ES3Settings settings)=>Data.ContainsKey(path+key);
    public static void LoadInto<T>(string key,string path,T value,ES3Settings settings)
    {
        if(typeof(T)!=typeof(object)) throw new Exception("stored concrete type must drive deserialization");
        Reads[path+key]=Reads.GetValueOrDefault(path+key)+1;
        Data[path+key](value);
    }
}
namespace CluckmechCoop { internal static class Plugin { internal static int Role;internal static 总结脚本 Summary;internal static void BindSummary(总结脚本 value)=>Summary=value;internal static void Log(string message){} } }
static class Program
{
    static int checks;
    static void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}
    static bool Prefix(Type type,params object[] args)=>(bool)type.GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);
    static 存档脚本 Setup(string slot="danju")
    {
        GameObject.Clear();ES3.Data.Clear();ES3.Reads.Clear();RunSaveRepair.Blocked=false;Plugin.Role=0;SceneManager.Index=0;
        var root=new GameObject("总结");root.AddComponent(typeof(总结脚本));
        var save=(存档脚本)new GameObject(slot+"存档").AddComponent(typeof(存档脚本));save.文件名=slot;
        new GameObject("menu").AddComponent(typeof(开始菜单继续游戏));
        return save;
    }
    static void Healthy(object value)
    {
        var s=(总结脚本)value;s.当前所选英雄id=4;s.当前金钱数=127;s.当前动力炉等级=3;s.当前基地血量=3125;
        s.单局统计数据.角色初始天赋="talent";s.当前拥有炮台=new(){new(){炮台id=31,强化上限=8,强化词条=new(){9,12}}};
    }
    static void Template(总结脚本 s)
    {
        s.当前所选英雄id=0;s.当前金钱数=0;s.当前动力炉等级=0;s.当前基地血量=5000;s.单局统计数据.角色初始天赋=null;
        s.当前拥有炮台=new(){new(){炮台id=1},new(){炮台id=2},new(){炮台id=0}};
    }
    static void Main()
    {
        var save=Setup();string path="danju_m.cd";
        var current=GameObject.Find("总结").GetComponent<总结脚本>();Template(current);
        ES3.Data[path+"总结总结脚本"]=Healthy;
        ES3.Data[path+save.gameObject.name+"存档脚本"]=value=>{
            var s=(存档脚本)value;s.物体组件列表.Add(new(){物体名称="总结",脚本组件列表=new(){"总结脚本"}});
            s.物体列表.Add(new(){物体名称="route",脚本组件列表=new(){"Route"}});
        };
        new GameObject("route").AddComponent(typeof(Route));ES3.Data[path+"routeRoute"]=value=>((Route)value).Node="mine-3";
        Check(!Prefix(typeof(RestoreRunSlot),save),"run slot uses targeted restore");
        Check(save.CacheCalls==1,"native cache and backup recovery kept");
        Check(ReferenceEquals(Plugin.Summary,current),"restores current root and rebinds cached summary");
        Check(current.当前金钱数==127&&current.当前所选英雄id==4&&current.当前拥有炮台[0].强化上限==8,"run inventory and progress restored");
        Check(ES3.Reads[path+"总结总结脚本"]==1,"summary deserialized once");
        Check(GameObject.Find("route").GetComponent<Route>().Node=="mine-3","native metadata components restored");
        Check(!RunSaveRepair.Blocked&&Prefix(typeof(ContinueRunGate),4),"healthy save can continue");
        Check(!RunSaveRepair.CanSave(save),"menu never overwrites run slot");
        SceneManager.Index=4;Check(RunSaveRepair.CanSave(save),"loaded route can autosave");
        save=Setup("lianji");ES3.Data["lianji_m.cd总结总结脚本"]=Healthy;
        RunSaveRepair.Restore(save);Check(Plugin.Summary.当前金钱数==127&&!RunSaveRepair.Blocked,"co-op host restores separate slot without metadata");
        Check(!ES3.Reads.ContainsKey("danju_m.cd总结总结脚本"),"co-op never reads solo file");
        Plugin.Role=2;Check(Prefix(typeof(RestoreRunSlot),save),"client keeps native role path");
        save=Setup("quanju");Check(Prefix(typeof(RestoreRunSlot),save)&&RunSaveRepair.CanSave(save),"global progression keeps native load and save");
        save=Setup();ES3.Data["danju_m.cd总结总结脚本"]=value=>Template((总结脚本)value);
        RunSaveRepair.Restore(save);Check(RunSaveRepair.Blocked,"known broken template rejected");
        Check(!Prefix(typeof(ContinueRunGate),4),"bad continue cannot enter map");
        Check(Prefix(typeof(ContinueRunGate),1),"bad continue does not block new run");
        Check(!GameObject.Find("menu").GetComponent<开始菜单继续游戏>().按钮.interactable,"broken save labelled on existing continue button");
        SceneManager.Index=4;Check(!RunSaveRepair.CanSave(save),"bad loaded state cannot overwrite disk");
        save=Setup();RunSaveRepair.Restore(save);Check(RunSaveRepair.Blocked,"missing core stops continue");
        current=GameObject.Find("总结").GetComponent<总结脚本>();Template(current);
        Check(RunSaveRepair.IsTemplate(current),"exact menu template identified");
        current.当前拥有炮台[0].强化上限=4;Check(!RunSaveRepair.IsTemplate(current),"real zero-gold chicken inventory accepted");
        Console.WriteLine($"PASS: {checks} production run-save checks using Unity and ES3 storage doubles");
    }
}
