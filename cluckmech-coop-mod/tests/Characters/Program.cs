using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CluckmechCoop;
using UnityEngine;

namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)] public class HarmonyPatch : Attribute { public HarmonyPatch(Type type, string name) { } }
    public static class AccessTools { public static MethodInfo Method(Type type, string name) => type.GetMethod(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static); }
}
namespace UnityEngine
{
    public class Object
    {
        public static float DestroyDelay;
        public static void Destroy(Object value, float delay) { DestroyDelay = delay; }
    }
    public class Component : Object
    {
        public GameObject gameObject; public bool enabled = true;
    public Transform transform => gameObject.transform;
        public T GetComponent<T>() => gameObject.GetComponent<T>();
    }
    public class MonoBehaviour : Component
    {
        public Coroutine StartCoroutine(IEnumerator body) => Scheduler.Start(body);
    }
    public class Coroutine { public IEnumerator Body; public float Due; }
    public class WaitForSeconds { public float Seconds; public WaitForSeconds(float seconds) { Seconds = seconds; } }
    public static class Time { public static float time; }
    public class GameObject : Object
    {
        private readonly Dictionary<Type, Component> components = new Dictionary<Type, Component>();
        public Transform transform = new Transform();
        public T GetComponent<T>() => components.TryGetValue(typeof(T),out var value)?(T)(object)value:default;
        public T AddComponent<T>() where T: Component,new() { var value=new T {gameObject=this}; components[typeof(T)]=value; return value; }
        public T[] GetComponentsInChildren<T>() { var value=GetComponent<T>(); return value==null?Array.Empty<T>():new[]{value}; }
        public float LastHeal;
        public void SendMessage(string name,float value,SendMessageOptions options) { LastHeal=value; }
    }
    public class Transform { public Vector3 position; }
    public struct Vector2
    {
        public float x,y; public Vector2(float x,float y) {this.x=x;this.y=y;}
        public static implicit operator Vector2(Vector3 v)=>new Vector2(v.x,v.y);
    }
    public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} }
    public class Camera { public static Camera main=new Camera(); public Transform transform=new Transform(); public Vector3 ScreenToWorldPoint(Vector3 value)=>value; }
    public class Collider2D : Component { }
    public enum SendMessageOptions { DontRequireReceiver }
}
namespace UnityEngine.InputSystem
{
    public class Position { public Vector2 Value=new Vector2(25,30); public Vector2 ReadValue()=>Value; }
    public class Mouse { public static Mouse current=new Mouse(); public Position position=new Position(); }
}
namespace Mirror
{
    public class NetworkBehaviour : MonoBehaviour { public uint netId; public bool isOwned=true; public object connectionToClient; }
    public class NetworkIdentity : MonoBehaviour { public object connectionToClient; }
    public static class NetworkServer { public static Dictionary<uint,NetworkIdentity> spawned=new Dictionary<uint,NetworkIdentity>(); }
}
public class 联机玩家控件 : Mirror.NetworkBehaviour { }
public static class Steam联机管理器 { public static 联机玩家控件 玩家_主机=new 联机玩家控件(), 玩家_客户端=new 联机玩家控件(); }
public class 模块_移动属性 : MonoBehaviour
{
    public Transform 前进副目标;
    public float 移动速度=5,当前移动速度,变化百分比移动速度;
    public void 运算移动速度(){ 当前移动速度=移动速度*(1+变化百分比移动速度); UnitModifiers.Move(this); }
}
public class 模块_攻击属性 : MonoBehaviour
{
    public float 攻击力=10, 当前攻击力, 百分比攻击力加成;
    public float 攻击速度=2,当前攻击速度,百分比攻击速度加成,当前攻击倒计时=1;
    public void 运算攻击力(){ 当前攻击力=攻击力*(1+百分比攻击力加成); UnitModifiers.Attack(this); }
    public void 运算攻击速度(){当前攻击速度=攻击速度*(1+百分比攻击速度加成);UnitModifiers.AttackSpeed(this);}
}
public class 模块_玩家_攻击 : MonoBehaviour { public bool 能否攻击=true;public float 蓄力蓄满时间=5,蓄力时间; }
public class 模块_玩家_死亡 : MonoBehaviour { public bool 是否玩家死亡; }
public class 模块_玩家_技能 : MonoBehaviour
{
    public float 技能cd=5; public int Shots;
    public void 技能实现() { Shots++; Program.Call(typeof(ChickenBurst),"Postfix",this); }
}
public class 模块_基本属性 : MonoBehaviour
{
    public enum 单位分类枚举 { 战士,炮台 }
    public enum 单位定位枚举 { 玩家,基地 }
    public 阵营数据库.阵营 当前阵营=阵营数据库.阵营.友方;
    public 单位分类枚举 单位分类=单位分类枚举.战士;
    public List<单位定位枚举> 单位定位列表=new();
    public float 当前生命值=100, 当前生命值上限=200, 生命值=200, 变化生命值, 生命值倍率=1, 格挡值=5, 变化格挡值, 当前格挡值=5;
    public void 运算生命值上限()
    {
        UnitModifiers.HealthMultiplier(this, out float? original);
        float old=当前生命值上限;
        当前生命值上限=生命值+变化生命值<=0?1:(生命值+变化生命值)*生命值倍率;
        float added=当前生命值上限-old;
        if(added>0) 当前生命值+=added;
        if(当前生命值>当前生命值上限) 当前生命值=当前生命值上限;
        Program.Call(typeof(SummonHealth),"Postfix",this,original);
    }
    public void 运算格挡值(){当前格挡值=格挡值+变化格挡值;Program.Call(typeof(SummonBlock),"Postfix",this);}
}
public class 词条_100072_霰弹专家 : MonoBehaviour { }
public static class 阵营数据库 { public enum 阵营 {友方,敌方} }
public class 模块_单位跟随圈:MonoBehaviour {public GameObject 跟随圈目标;public List<Collider2D> 单位列表=new();}
public class 词条_100014_工程狮 : MonoBehaviour {public GameObject 吸引光环;private void 光环(bool value) {Program.Rally(this,value);} }
public class 词条_100033_肌肉鼠 : MonoBehaviour
{
    public GameObject 治疗光环; public float 治疗圈大小=1; public 模块_移动属性 移动属性;
    private void 光环(bool value) { float old=移动属性.移动速度; 移动属性.移动速度=value?0:5; Program.Call(typeof(RatMovingHealing),"Postfix",this,value,old); }
}
public class 词条_100115_阿法狗 : MonoBehaviour { public float 技能cd=20; public int 召唤物数量=2; }
public class 词条_100191_八酱兔 : Mirror.NetworkBehaviour { }
public class 模块_光环_治疗_肌肉鼠 : MonoBehaviour { public float 光环范围=2; public 词条_100033_肌肉鼠 肌肉鼠; }
public class 模块_光环 : MonoBehaviour
{
    public class 距离单元 { public 模块_基本属性 基本属性; public float 距离; }
    public List<距离单元> 范围内友方=new List<距离单元>();
    public void 初始化(){} public void 光环优化(float value){} public void 检索光环内友方(){}
}
public class 模块_范围伤害精灵 : MonoBehaviour
{
    public float 前置生命周期=.2f, 生命周期=1;
    public class Damage { public float 攻击伤害; public GameObject 攻击者; }
    public Damage 伤害数据=new Damage();
}
public class 效果物体引用器 : MonoBehaviour { public class Prefabs { public GameObject 爆炸_震荡核弹=new GameObject(); } public Prefabs 预制体=new Prefabs(); }
public class 刷怪器 : MonoBehaviour { }
namespace 函数库
{
    public static class 词条函数
    {
        public static class 召唤
        {
            public static GameObject Last;
            public static GameObject 召唤衍生物(GameObject prefab,GameObject parent,Vector3 position)
            { Last=new GameObject();Last.transform.position=position;Last.AddComponent<模块_范围伤害精灵>();Last.AddComponent<Collider2D>();return Last; }
        }
    }
}
namespace CluckmechCoop
{
    internal static class ModuleSettings { internal class Modules { public bool Characters=true; } internal static Modules Extra=new Modules(); }
    internal class SessionMessage { public string Type,Token; public uint Unit; public float X,Y; }
    internal class NetworkService { public SessionMessage Last; public void Send(SessionMessage value){Last=value;} }
    internal class CombatFeatures { public string Token="battle"; }
    internal static class Plugin
    {
        public static int Role; public static 刷怪器 Spawner; public static CombatFeatures Combat=new CombatFeatures(); public static NetworkService Network=new NetworkService();
        public static void Log(string text){}
    }
}
static class Scheduler
{
    private static List<Coroutine> pending=new List<Coroutine>();
    internal static Coroutine Start(IEnumerator body){var c=new Coroutine{Body=body};if(Step(c))pending.Add(c);return c;}
    private static bool Step(Coroutine c){if(!c.Body.MoveNext())return false;c.Due=Time.time+(c.Body.Current is WaitForSeconds wait?wait.Seconds:.001f);return true;}
    internal static void Advance(float seconds)
    {
        float end=Time.time+seconds;
        while(true){var next=pending.Find(c=>c.Due<=end);if(next==null)break;Time.time=next.Due;if(!Step(next))pending.Remove(next);}
        Time.time=end;
    }
}
static class Program
{
    private static int checks;
    internal static object Call(Type type,string name,params object[] args)=>type.GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);
    private static void Check(bool value,string name){if(!value)throw new Exception(name);checks++;}
    internal static void Rally(词条_100014_工程狮 lion,bool held)
    {
        object[] args={lion,held,null};Call(typeof(LionRally),"Prefix",args);
        var circle=lion.吸引光环?.GetComponent<模块_单位跟随圈>();
        if(!held&&circle!=null)foreach(var c in circle.单位列表){var m=c.GetComponent<模块_移动属性>();if(m!=null&&m.前进副目标==circle.跟随圈目标.transform)m.前进副目标=null;}
        Call(typeof(LionRally),"Postfix",lion,held,args[2]);
    }
    private static GameObject Unit()
    {
        var go=new GameObject();go.AddComponent<模块_移动属性>();go.AddComponent<模块_攻击属性>();go.AddComponent<模块_玩家_死亡>();go.AddComponent<模块_玩家_攻击>();go.AddComponent<模块_基本属性>();return go;
    }
    static void Main()
    {
        var chicken=Unit().AddComponent<模块_玩家_技能>();Call(typeof(ChickenReady),"Postfix",chicken);chicken.技能实现();
        Check(chicken.技能cd==4&&chicken.Shots==1,"chicken starts one shot with four-second cooldown");
        Scheduler.Advance(.199f);Check(chicken.Shots==1,"burst does not fire second shot early");
        Scheduler.Advance(.0011f);Check(chicken.Shots==2,"second shotgun at 0.2 game seconds");
        Scheduler.Advance(.2f);Check(chicken.Shots==3&&!UnitModifiers.For(chicken.gameObject).Burst,"burst ends after exactly three shots");
        var expert=Unit().AddComponent<模块_玩家_技能>();var talent=expert.gameObject.AddComponent<词条_100072_霰弹专家>();
        Call(typeof(ShotgunExpertReady),"Prefix",talent);expert.技能cd/=2;Call(typeof(ChickenReady),"Postfix",expert);Check(expert.技能cd==2,"shotgun expert halves four-second base regardless of Start order");
        var interrupted=Unit().AddComponent<模块_玩家_技能>();interrupted.技能实现();interrupted.GetComponent<模块_玩家_死亡>().是否玩家死亡=true;Scheduler.Advance(.5f);Check(interrupted.Shots==1,"death stops queued burst");
        var lion=Unit().AddComponent<词条_100014_工程狮>();var speed=lion.GetComponent<模块_移动属性>();speed.变化百分比移动速度=.5f;
        Rally(lion,true);Check(Math.Abs(speed.当前移动速度-9.75f)<.001,"rally multiplies movement including original buffs");
        Rally(lion,false);Check(speed.当前移动速度==7.5f,"rally release preserves original movement buffs");
        lion.吸引光环=new GameObject();var circle=lion.吸引光环.AddComponent<模块_单位跟随圈>();circle.跟随圈目标=new GameObject();
        var warrior=Unit();var collider=warrior.AddComponent<Collider2D>();circle.单位列表.Add(collider);circle.单位列表.Add(collider);warrior.GetComponent<模块_移动属性>().前进副目标=circle.跟随圈目标.transform;
        var left=Unit();circle.单位列表.Add(left.AddComponent<Collider2D>());
        var enemy=Unit();enemy.GetComponent<模块_基本属性>().当前阵营=阵营数据库.阵营.敌方;enemy.GetComponent<模块_移动属性>().前进副目标=circle.跟随圈目标.transform;circle.单位列表.Add(enemy.AddComponent<Collider2D>());
        var baseUnit=Unit();baseUnit.GetComponent<模块_基本属性>().单位定位列表.Add(模块_基本属性.单位定位枚举.基地);baseUnit.GetComponent<模块_移动属性>().前进副目标=circle.跟随圈目标.transform;circle.单位列表.Add(baseUnit.AddComponent<Collider2D>());
        var charge=lion.GetComponent<模块_玩家_攻击>();charge.蓄力时间=2;Rally(lion,true);Rally(lion,true);
        Check(charge.蓄力蓄满时间==3.75f&&charge.蓄力时间==1.5f,"held rally applies charge multiplier once and preserves gauge proportion");
        charge.蓄力蓄满时间*=.8f;Rally(lion,false);float release=Time.time;
        Check(Math.Abs(charge.蓄力蓄满时间-4)<.001&&Math.Abs(charge.蓄力时间-2)<.001,"release removes only own charge multiplier and keeps gained native talent");
        var army=warrior.GetComponent<模块_攻击属性>();
        Check(army.当前攻击力==12.5f&&Math.Abs(army.当前攻击速度-2.4f)<.001,"party captured before native OnDisable receives damage and attack speed boost");
        Check(army.当前攻击倒计时<=1/army.当前攻击速度,"initial attack countdown honors faster rate");
        Check(left.GetComponent<UnitModifiers>()==null&&enemy.GetComponent<UnitModifiers>()==null&&baseUnit.GetComponent<UnitModifiers>()==null,"historical departed units, enemies and base excluded from rally boost");
        Check(UnitModifiers.For(lion.gameObject).NextRally==release+8,"party boost starts independent eight-second cooldown");
        UnitModifiers.For(warrior).RabbitBoost();Check(army.当前攻击力==18.75f,"rally composes with existing summon boost");
        army.百分比攻击力加成=.2f;army.百分比攻击速度加成=.5f;army.运算攻击力();army.运算攻击速度();
        Scheduler.Advance(3.999f);Check(Math.Abs(army.当前攻击力-22.5f)<.001,"rally survives four full game seconds and native recalculations");
        Scheduler.Advance(.002f);Check(Math.Abs(army.当前攻击力-18)<.001&&Math.Abs(army.当前攻击速度-3)<.001,"rally expiry keeps native upgrades and remaining rabbit multiplier");
        Scheduler.Advance(1);Check(Math.Abs(army.当前攻击力-12)<.001,"other summon boost expires independently");
        warrior.GetComponent<模块_移动属性>().前进副目标=circle.跟随圈目标.transform;Rally(lion,true);Rally(lion,false);
        Check(Math.Abs(army.当前攻击力-12)<.001&&UnitModifiers.For(lion.gameObject).NextRally==release+8,"repeated press and release during cooldown cannot reapply party boost");
        Scheduler.Advance(3.01f);warrior.GetComponent<模块_移动属性>().前进副目标=circle.跟随圈目标.transform;Rally(lion,true);Rally(lion,false);Check(Math.Abs(army.当前攻击力-15)<.001,"party boost can return after cooldown");
        Scheduler.Advance(2);UnitModifiers.For(warrior).RallyBoost();Scheduler.Advance(2.01f);Check(Math.Abs(army.当前攻击力-15)<.001,"second engineer refreshes duration without stacking strength");Scheduler.Advance(2);Check(Math.Abs(army.当前攻击力-12)<.001,"refreshed shared-party boost expires at final deadline");
        Plugin.Role=2;var clientLion=Unit().AddComponent<词条_100014_工程狮>();Rally(clientLion,true);Check(clientLion.GetComponent<模块_玩家_攻击>().蓄力蓄满时间==3.75f,"client applies own charge response through native rally broadcast");Rally(clientLion,false);Check(UnitModifiers.For(clientLion.gameObject).NextRally==0,"client does not author duplicated party effects");Plugin.Role=0;
        var rat=Unit().AddComponent<词条_100033_肌肉鼠>();rat.移动属性=rat.GetComponent<模块_移动属性>();rat.治疗光环=new GameObject();
        var aura=rat.治疗光环.AddComponent<模块_光环>();rat.治疗光环.AddComponent<模块_光环_治疗_肌肉鼠>().肌肉鼠=rat;
        var friend=Unit().GetComponent<模块_基本属性>();friend.当前生命值上限=1000;aura.范围内友方.Add(new 模块_光环.距离单元{基本属性=friend,距离=1});
        var far=Unit().GetComponent<模块_基本属性>();aura.范围内友方.Add(new 模块_光环.距离单元{基本属性=far,距离=9});
        var dead=Unit().GetComponent<模块_基本属性>();dead.GetComponent<模块_玩家_死亡>().是否玩家死亡=true;aura.范围内友方.Add(new 模块_光环.距离单元{基本属性=dead,距离=1});
        rat.移动属性.移动速度=0;Call(typeof(RatMovingHealing),"Postfix",rat,true,5f);Scheduler.Advance(.01f);
        Check(rat.移动属性.移动速度==5&&rat.移动属性.当前移动速度==3.5f,"rat retains base speed while healing at 70 percent");
        Check(friend.gameObject.LastHeal==200&&far.gameObject.LastHeal==0&&dead.gameObject.LastHeal==0,"pulse heals twenty percent max health within radius and excludes dead players");
        Check(!(bool)Call(typeof(RatAttackGate),"Prefix",rat.GetComponent<模块_玩家_攻击>()),"rat blocks attacks during healing");
        friend.gameObject.LastHeal=0;Call(typeof(RatMovingHealing),"Postfix",rat,true,5f);Scheduler.Advance(.01f);Check(friend.gameObject.LastHeal==0,"holding right mouse does not repeat emergency pulse");
        Call(typeof(RatMovingHealing),"Postfix",rat,false,5f);Call(typeof(RatMovingHealing),"Postfix",rat,true,5f);Scheduler.Advance(.01f);Check(friend.gameObject.LastHeal==0,"fresh press during independent cooldown does not heal");
        Scheduler.Advance(10);Call(typeof(RatMovingHealing),"Postfix",rat,false,5f);Call(typeof(RatMovingHealing),"Postfix",rat,true,5f);Scheduler.Advance(.01f);Check(friend.gameObject.LastHeal==200,"emergency pulse returns after independent ten-second cooldown");
        UnitModifiers.For(rat.gameObject).ResetHeld();Check(!UnitModifiers.For(rat.gameObject).RatHeld&&rat.移动属性.当前移动速度==5,"death clears healing hold and restores movement");
        var rabbit=Unit().AddComponent<词条_100191_八酱兔>();Call(typeof(RabbitAim),"Prefix",rabbit);object[] arguments={rabbit,new Vector2(0,0)};Call(typeof(RabbitPosition),"Prefix",arguments);
        Check(((Vector2)arguments[1]).x==25&&((Vector2)arguments[1]).y==30,"rabbit spawn circle is centered at captured aim");
        var summon=Unit();Call(typeof(RabbitSpawned),"Postfix",summon);Check(summon.GetComponent<模块_攻击属性>().当前攻击力==15&&summon.GetComponent<模块_移动属性>().当前移动速度==6.5f,"rabbit receives initial damage and speed multipliers");
        summon.GetComponent<模块_攻击属性>().百分比攻击力加成=.2f;Scheduler.Advance(5);Check(summon.GetComponent<模块_攻击属性>().当前攻击力==12&&summon.GetComponent<模块_移动属性>().当前移动速度==5,"five-second boost expires while preserving new native buffs");
        var world=new GameObject();Plugin.Spawner=world.AddComponent<刷怪器>();world.AddComponent<效果物体引用器>();var dog=Unit().AddComponent<词条_100115_阿法狗>();
        Call(typeof(DogReady),"Postfix",dog);Check(dog.技能cd==30&&dog.召唤物数量==2,"dog cooldown increases ten seconds while preserving native two summons");
        var brothers=Unit().AddComponent<词条_100115_阿法狗>();brothers.召唤物数量++;Call(typeof(DogReady),"Postfix",brothers);Check(brothers.技能cd==30&&brothers.召唤物数量==3,"three brothers talent retains three summons");
        var support=Unit();var supportHp=support.GetComponent<模块_基本属性>();supportHp.当前生命值=200;Call(typeof(DogSpawned),"Postfix",support);
        Check(supportHp.当前生命值==400&&supportHp.当前生命值上限==400&&supportHp.当前格挡值==10&&supportHp.生命值倍率==1,"dog support starts with doubled health and block without changing native multiplier");
        supportHp.当前生命值=300;supportHp.运算生命值上限();supportHp.运算格挡值();supportHp.运算生命值上限();supportHp.运算格挡值();
        Check(supportHp.当前生命值==300&&supportHp.当前格挡值==10,"native repeated recalculation neither heals nor stacks defense boost");
        supportHp.变化生命值=50;supportHp.变化格挡值=3;supportHp.运算生命值上限();supportHp.运算格挡值();
        Check(supportHp.当前生命值上限==500&&supportHp.当前生命值==400&&supportHp.当前格挡值==16,"intervening native defense upgrades are included in doubled stats");
        Scheduler.Advance(4.999f);Check(supportHp.当前生命值上限==500,"support boost lasts full five game seconds");
        Scheduler.Advance(.002f);Check(supportHp.当前生命值上限==250&&supportHp.当前生命值==200&&supportHp.当前格挡值==8&&supportHp.生命值倍率==1,"boost expiry preserves health fraction and native upgrades");
        var fallen=Unit();Call(typeof(DogSpawned),"Postfix",fallen);fallen.GetComponent<模块_基本属性>().当前生命值=0;Scheduler.Advance(5);
        Check(fallen.GetComponent<模块_基本属性>().当前生命值==0,"boost expiry never revives a dead support");
        Plugin.Role=2;var remote=Unit();Call(typeof(DogSpawned),"Postfix",remote);Check(remote.GetComponent<模块_基本属性>().当前生命值上限==200,"guest leaves support attributes to native host synchronization");Plugin.Role=0;
        CharacterFeatures.Nuclear(dog);
        Check(dog.GetComponent<模块_基本属性>().当前生命值==0&&函数库.词条函数.召唤.Last.GetComponent<模块_范围伤害精灵>().伤害数据.攻击伤害==300,"dog nuclear attack deals 300 and enters native lethal-health path");
        Plugin.Role=2;dog.GetComponent<模块_基本属性>().当前生命值=100;CharacterFeatures.Nuclear(dog);
        Check(dog.GetComponent<模块_基本属性>().当前生命值==100&&!函数库.词条函数.召唤.Last.GetComponent<模块_范围伤害精灵>().enabled&&!函数库.词条函数.召唤.Last.GetComponent<Collider2D>().enabled&&Math.Abs(UnityEngine.Object.DestroyDelay-1.2f)<.001,"guest nuclear effect is visual only and has native lifetime");
        ModuleSettings.Extra.Characters=false;var vanilla=Unit().AddComponent<模块_玩家_技能>();Call(typeof(ChickenReady),"Postfix",vanilla);vanilla.技能实现();Scheduler.Advance(1);Check(vanilla.技能cd==5&&vanilla.Shots==1,"disabled character module preserves native shotgun");
        var nativeDog=Unit().AddComponent<词条_100115_阿法狗>();Call(typeof(DogReady),"Postfix",nativeDog);Call(typeof(DogSpawned),"Postfix",nativeDog.gameObject);Check(nativeDog.技能cd==20&&nativeDog.GetComponent<模块_基本属性>().当前生命值上限==200,"disabled module preserves native dog cooldown and defense");
        Console.WriteLine($"PASS: {checks} production character timing, movement, healing, targeting and nuclear behavior checks");
    }
}
