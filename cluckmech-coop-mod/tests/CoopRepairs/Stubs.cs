using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

namespace HarmonyLib {
 public class HarmonyPatch:Attribute { public HarmonyPatch(params object[] args){} }
 public static class AccessTools {
  public delegate ref F FieldRef<T,F>(T instance);
  public static FieldRef<T,F> FieldRefAccess<T,F>(string name)=>(FieldRef<T,F>)(object)(FieldRef<模块_受伤,效果物体引用器>)((模块_受伤 owner)=>ref owner.引用器);
  public static MethodInfo Method(Type t,string n,Type[] a)=>t.GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public,null,a,null);
  public static T MethodDelegate<T>(MethodInfo m) where T:Delegate=>m.CreateDelegate<T>();
 }
}
namespace UnityEngine {
    public struct Vector2 { public float x,y; public static Vector2 operator *(Vector2 a,float v)=>a; }
    public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} }
    public struct Quaternion {public Quaternion(float x,float y,float z,float w){}}
    public struct Vector4 { public float x,y,z,w; public Vector4(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;} }
    public enum SendMessageOptions { DontRequireReceiver }
    public class Transform { public Vector3 position,localScale; }
    public class Component {
        public GameObject gameObject; public Transform transform=>gameObject.transform;
        public T GetComponent<T>() where T:class => gameObject.GetComponent<T>();
        public Component GetComponent(Type t)=>gameObject.GetComponent(t);
    }
    public class GameObject {
        readonly Dictionary<Type,object> components=new(); public Transform transform=new();public string name="unit";public bool activeInHierarchy=true;
        public static int ComponentReads;
        public Component GetComponent(Type t){ComponentReads++;return components.TryGetValue(t,out var c)?c as Component:null;}
        public bool TryGetComponent<T>(out T c) where T:class {c=GetComponent<T>();return c!=null;}
        public int StatisticMessages;
        public T Add<T>(T value) where T:Component {components[typeof(T)]=value;value.gameObject=this;return value;}
        public T GetComponent<T>() where T:class {ComponentReads++;return components.TryGetValue(typeof(T),out var o)?o as T:null;}
        public void SendMessage(string name,object value,SendMessageOptions options=SendMessageOptions.DontRequireReceiver) {
            if(name=="伤害统计")StatisticMessages++;
            if(name=="受到伤害"){
                var injury=GetComponent<模块_受伤>();var args=new object[]{injury,Convert.ToSingle(value)};
                var prefix=typeof(CluckmechCoop.OtherworldFlatDamage).GetMethod("Prefix",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
                if((bool)prefix.Invoke(null,args))injury.受到伤害((float)args[1]);
            }
        }
    }
}
public class 阵营数据库 { public enum 阵营 { 无,友方,敌方 } }
public class 攻击类型与状态数据库 { public enum 状态枚举 { 中毒,灼烧,流血 } }
public class 模块_基本属性:Component {
    public int ID,母体id;public float 生命值=100;public int 初始阵营=1,单位分类=0;public enum 单位类型枚举{地面友方单位=9,陷阱=14,场景=16}public 单位类型枚举 单位类型=单位类型枚举.地面友方单位;
    public float 当前生命值=1000,当前反伤值,当前抗性,当前格挡值,最近受到伤害;
    public bool 当前是否无敌,当前是否保护; public GameObject 最近攻击者; public 阵营数据库.阵营 当前阵营=阵营数据库.阵营.敌方;
}
public class 模块_状态持续效果 {public class 底层状态类 {} public class 状态伤害类 {public float 数值;public 攻击类型与状态数据库.状态枚举 状态;} }
public class 模块_状态属性 { public class 状态类{} public int Count; public void 增加状态(状态类 state){Count++;} }
public class 模块_仇恨 {public int Count;public void 增加仇恨目标(float value,GameObject source){Count++;} }
public class 模块_移动 {public class Path {public Vector2 movingDirection;} public Path 寻路脚本=new();public int Pushes;public void 被击退(Vector4 p){Pushes++;} }
public class 事件管理器_战斗 {public int Events;public float Total;public void 触发_造成伤害(GameObject g,float v){Events++;Total+=v;} }
public partial class 模块_受伤:Component {
    public 效果物体引用器 引用器=new();
    GameObject 自身=>gameObject;
    public 模块_基本属性 基本属性;public 模块_状态属性 状态属性=new();public 模块_仇恨 仇恨=new();public 模块_移动 移动=new();public 事件管理器_战斗 战斗事件=new();
    public int Visuals,CriticalVisuals,PushVisuals; public float LastVisual;
    void 伤害出现(float value){伤害出现(value,new Vector4(1,1,1,1));}
    void 伤害出现(float value,Vector4 color){CluckmechCoop.OtherworldHitDisplay.Record(this,value,false,color.y==.4f);Visuals++;LastVisual=value;}
    void 暴击出现(float value){CluckmechCoop.OtherworldHitDisplay.Record(this,value,true,false);Visuals++;CriticalVisuals++;LastVisual=value;}
    void 击退效果出现(float value){PushVisuals++;}
    public void Show(float value,bool critical){if(critical)暴击出现(value);else 伤害出现(value);}
}
namespace CluckmechCoop {
    public class OtherworldUnit:Component {public 模块_基本属性 Health;public int Space;public bool IsPlayer;}
    public class OtherworldActor:Component {public bool Phased;public int Avoids;public void Avoid(世界类.伤害数据包 p){Avoids++;}}
    public class OtherworldRun {public bool Pressure;public bool Has(int id)=>Pressure&&id==26116;}
    public static class Plugin {public static int Role;public static dw单位库 Units;public static CoroutineHost Instance=new();public static CombatStub Combat=new();}
    public static class OtherworldWorld {public static int Space(GameObject g)=>Unit(g)?.Space??0;public static bool Active=true;public static readonly Dictionary<GameObject,OtherworldUnit> Units=new();public static OtherworldUnit Unit(GameObject g)=>g!=null&&Units.TryGetValue(g,out var v)?v:null;}
    public static class OtherworldProgress {public static readonly Dictionary<GameObject,OtherworldRun> Runs=new();public static OtherworldRun For(GameObject g)=>g!=null&&Runs.TryGetValue(g,out var r)?r:null;}
    public static class KamuiWorld {public static OtherworldRun Current=new();public static OtherworldRun Run(int space)=>Current;}
    public static class OtherworldDamage {
        public struct Scope {public bool Captured;public int? Previous;}
        public static int? ApplyingSpace;
        public static readonly Dictionary<世界类.伤害数据包,int> Origins=new();
        public static int Space(世界类.伤害数据包 p)=>Origins.TryGetValue(p,out int s)?s:OtherworldWorld.Unit(p.攻击者)?.Space??0;
        public static void Remember(世界类.伤害数据包 p,int space)=>Origins[p]=space;
    }
}

namespace UnityEngine {
 public static class Time {public static float deltaTime=1f/60,realtimeSinceStartup;}
 public static class Random {public static float Range(float a,float b)=>0;}
 public static class Object {public static int Instantiations;public static GameObject Instantiate(GameObject p,Vector3 pos,Quaternion q){Instantiations++;var g=new GameObject();if(p.GetComponent<模块_基本属性>() is {} hp)g.Add(new 模块_基本属性{ID=hp.ID});return g;}}
}
public class 模块_攻击属性:Component {public float 攻击力=10,攻击速度=1,攻击范围=3;}
public class 效果物体引用器 {}
public class 模块_移动属性:Component {public float 移动速度=2;}
public class 模块_加成管理器 {public class 状态加成类 {}}
public class 模块_伤害精灵:Component {public 世界类.伤害数据包 伤害数据=new();}
public class 模块_范围伤害精灵:Component {public 世界类.伤害数据包 伤害数据=new();}
public class 词条_100001_毒气带倒计时:Component {}
public class 词条_100034_定时销毁:Component {}
public class 词条_100098_限时自杀:Component {}
public class 词条_100134_钢铁蜗牛:Component {public float 计时器;}
public class 词条_194_滚烫步伐:Component {public GameObject 生成物;public float 计时器;}
public class Steam联机管理器 {public enum 联机状态 {未联机,主机端,客户端} public static 联机状态 当前联机状态;}
public class dw单位库 {public class Entry {public GameObject 单位预制体;}public List<Entry> 单位列表=new();}
namespace Mirror {
 public interface NetworkMessage {}
 public class NetworkIdentity:Component {public uint assetId;}
 public class NetworkManager {public static NetworkManager singleton=new();public List<GameObject> spawnPrefabs=new();}
 public static class NetworkClient {public static Dictionary<uint,GameObject> prefabs=new();public static Dictionary<uint,NetworkIdentity> spawned=new();}
 public static class NetworkServer {public static int Calls;public static void Spawn(GameObject g){if(g==null)throw new Exception("null spawn");Calls++;}}
}
namespace CluckmechCoop {
 public class SceneLifetime {}
 public static class SceneLifecycle {public static SceneLifetime Current=new();public static bool IsCurrent(SceneLifetime scene)=>ReferenceEquals(scene,Current);}
 public class CombatStub {public string Token="run";}
 public class CoroutineHost {
  public List<IEnumerator> Pending=new();
  public void StartCoroutine(IEnumerator routine){if(routine.MoveNext())Pending.Add(routine);}
  public void Frame(){for(int i=Pending.Count-1;i>=0;i--)if(!Pending[i].MoveNext())Pending.RemoveAt(i);}
 }
 internal static class OtherworldMessageKind {internal const int Effect=11;}
 internal static partial class OtherworldNetwork {
  internal static List<OtherworldMessage> Sent=new();
  internal static void Send(OtherworldMessage message,int owner)=>Sent.Add(message);
 }
 internal class StratagemSaved {} internal class BronzeDragonState {}
 internal static class NativeRepairs {internal static bool Enabled=true;}
 internal static class OtherworldActions {internal static OtherworldActor Viewer;internal static OtherworldActor Player(int role)=>Viewer;}
 internal static class StratagemWorld {internal static readonly Dictionary<GameObject,uint> Ids=new();internal static uint Id(GameObject g)=>Ids.TryGetValue(g,out var id)?id:0;}
 internal static class OtherworldDataValidation {internal static bool Finite(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);}
}
