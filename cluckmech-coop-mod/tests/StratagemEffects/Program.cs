using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Reflection;
using CluckmechCoop;
using UnityEngine;

namespace UnityEngine
{
    public class Object { public static void Destroy(object value) { } }
    public class MonoBehaviour { }
    public sealed class Coroutine { }
    public sealed class WaitForSeconds { public WaitForSeconds(float seconds) { } }
    public static class Time { public static float time; }
    public enum RigidbodyType2D { Kinematic }
    public sealed class Rigidbody2D { public RigidbodyType2D bodyType; }
    public class Collider2D { public T GetComponentInParent<T>() => default; }
    public sealed class CircleCollider2D : Collider2D { public float radius; public bool isTrigger; }
    public enum SendMessageOptions { DontRequireReceiver }
    public sealed class Transform { public Vector3 position; }
    public struct Vector3 { public float x,y,z; public Vector3(float a,float b,float c) {x=a;y=b;z=c;} public static implicit operator Vector2(Vector3 v) => new Vector2(v.x,v.y); }
    public struct Vector2
    {
        public float x,y; public Vector2(float a,float b) {x=a;y=b;}
        public float sqrMagnitude => x*x+y*y;
        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x,v.y,0);
        public static Vector2 operator -(Vector2 a,Vector2 b) => new Vector2(a.x-b.x,a.y-b.y);
    }
    public struct Vector4
    {
        public static Vector4 zero => default;
        public float x,y,z,w; public Vector4(float a,float b,float c,float d) {x=a;y=b;z=c;w=d;}
        public static Vector4 operator +(Vector4 a,Vector4 b) => new Vector4(a.x+b.x,a.y+b.y,a.z+b.z,a.w+b.w);
    }
    public static class Random { public static float value = .5f; }
    public sealed class GameObject
    {
        public GameObject(string name="") { }
        public readonly Transform transform = new Transform();
        private readonly Dictionary<Type,object> parts = new Dictionary<Type,object>();
        public readonly Dictionary<string,int> Messages = new Dictionary<string,int>();
        public T AddComponent<T>() where T : new() => Add(new T());
        public T Add<T>(T part) {parts[typeof(T)]=part;return part;}
        public T GetComponent<T>() => parts.TryGetValue(typeof(T),out var part) ? (T)part : default;
        public void SendMessage(string name,SendMessageOptions options) {Messages.TryGetValue(name,out var count);Messages[name]=count+1;}
        public void SendMessage(string name,模块_状态属性.状态类 state) => GetComponent<模块_状态持续效果>()?.Attach(state);
    }
}
namespace HarmonyLib { public static class AccessTools { public static System.Reflection.MethodInfo Method(Type t,string n) => t.GetMethod(n,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic); } }
public static class 攻击类型与状态数据库
{
    public enum 状态枚举 {无状态,减速,灼烧,中毒,流血,眩晕}
    public enum 底层状态枚举 {百分比移动速度}
}
public static class 阵营数据库 { public enum 阵营 {友方,敌方} }
public sealed class 模块_基本属性
{
    public readonly GameObject gameObject; public Transform transform => gameObject.transform;
    public bool Living = true, Enemy = true;
    public 模块_基本属性(GameObject go) {gameObject=go;}
    public T GetComponent<T>() => gameObject.GetComponent<T>();
}
public sealed class 模块_攻击属性
{
    public float 当前攻击力=250,当前攻击范围=6,当前攻击速度=4;
    public bool 当前是否能攻击=true;
    public List<攻击类型与状态数据库.状态枚举> 当前造成状态 = new List<攻击类型与状态数据库.状态枚举>();
}
public sealed class 模块_攻击详细属性
{
    public sealed class 状态属性单元
    {
        public float 默认持续时间,当前持续时间,变化持续时间,变化百分比持续时间;
        public Vector4 默认属性效果,当前属性效果,变化属性效果,变化百分比属性效果;
    }
    public float 当前暴击率=.1f,当前暴击倍率=1;
    public 状态属性单元[] 状态加成列表 = Enumerable.Range(0,18).Select(_=>new 状态属性单元()).ToArray();
}
public sealed class 模块_抗性属性
{
    public sealed class 状态抗性单元 { public float 当前时间抗性,当前效果抗性; }
    public 状态抗性单元[] 状态抗性列表 = Enumerable.Range(0,18).Select(_=>new 状态抗性单元()).ToArray();
}
public sealed class FakeBattleEvents { public void 触发_造成伤害(GameObject source,float value) { } }
public partial class 模块_状态属性
{
    private GameObject 自身;
    private readonly FakeBattleEvents 战斗事件=new FakeBattleEvents();
    private 模块_抗性属性 抗性属性;
    public List<状态类> 当前状态列表=new List<状态类>();
    public 模块_状态属性(GameObject go,模块_抗性属性 resistance) {自身=go;抗性属性=resistance;}
    public void Tick(float delta)
    {
        foreach(var state in 当前状态列表) state.剩余时间-=delta;
        当前状态列表.RemoveAll(x=>x.剩余时间<=0);
    }
}
public sealed class 模块_状态持续效果
{
    public sealed class 底层状态类 { public float 数值,持续时间; }
    public readonly List<底层状态类> 当前底层状态列表=new List<底层状态类>();
    public int Recalculations;
    public void Attach(模块_状态属性.状态类 state)
    {if(state.状态类型==攻击类型与状态数据库.状态枚举.减速) 当前底层状态列表.Add(new 底层状态类 {数值=-state.加成属性.当前属性效果.x,持续时间=state.剩余时间});}
    private void 还原底层状态(攻击类型与状态数据库.底层状态枚举 type) {Recalculations++;}
    private void 底层状态刷新(攻击类型与状态数据库.底层状态枚举 type) {Recalculations++;}
}
public static class 世界类
{
    public sealed class 伤害数据包
    {
        public enum 击退类型枚举 {相对位置击退}
        public GameObject 攻击者; public 阵营数据库.阵营 所属阵营; public Transform 攻击目标; public Vector3 攻击坐标;
        public float 攻击伤害,暴击倍率; public bool 是否暴击,是否产生仇恨,是否爆炸,是否击退;
        public 击退类型枚举 击退类型; public Vector4 击退参数;
        public List<模块_状态属性.状态类> 造成状态=new List<模块_状态属性.状态类>();
    }
}
public sealed class 模块_受伤 { public readonly List<世界类.伤害数据包> Packets=new List<世界类.伤害数据包>(); public void 受到伤害(世界类.伤害数据包 packet) {Packets.Add(packet);} }
namespace CluckmechCoop
{
    internal sealed class StratagemUnit { internal StratagemDamage Damage; internal StratagemKind Kind; internal StratagemParameters Parameters; internal int ActiveFields; }
    internal static class Plugin
    {
        internal static bool InCombat = true;
        internal static Runner Instance = new Runner();
        internal sealed class Runner
        {
            internal Coroutine StartCoroutine(IEnumerator task) => new Coroutine();
            internal void StopCoroutine(Coroutine task) { }
        }
    }
    internal sealed class StratagemSavedField { public float X,Y,Radius,Remaining; public bool Scorch; }
    internal static class StratagemWorld
    {
        internal static List<模块_基本属性> Enemies=new List<模块_基本属性>();
        internal static int GlobalScans;
        internal static List<模块_基本属性> EnemiesFor(Vector2 point) { GlobalScans++; return Enemies; }
        internal static float DistanceSquared(模块_基本属性 target,Vector2 point) => ((Vector2)target.transform.position-point).sqrMagnitude;
        internal static bool Living(模块_基本属性 stats) => stats!=null&&stats.Living&&stats.Enemy;
    }
}
static class Program
{
    private static int checks;
    private static void Check(bool yes,string name) {if(!yes)throw new Exception(name);checks++;}
    private static 模块_基本属性 Actor()
    {
        var go=new GameObject(); var hp=go.Add(new 模块_基本属性(go));
        var resistance=go.Add(new 模块_抗性属性());
        go.Add(new 模块_状态属性(go,resistance));go.Add(new 模块_状态持续效果());go.Add(new 模块_受伤());return hp;
    }
    private static int BurnCount(StratagemDamage damage) => ((IDictionary)typeof(StratagemDamage).GetField("burns",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(damage)).Count;
    private static HashSet<StratagemDamage> Sources() => (HashSet<StratagemDamage>)typeof(StratagemDamage).GetField("sources",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
    private static StratagemUnit FieldSource(params int[] upgrades)
    {
        var go=new GameObject();
        var unit=go.Add(new StratagemUnit {Kind=StratagemKind.Mortar,Parameters=new StratagemParameters(20005,upgrades)});
        unit.Damage=new StratagemDamage(go); return unit;
    }
    private static StratagemZone AddField(StratagemUnit source,bool scorch=false,float duration=10)
    {
        StratagemFields.Add(source,new Vector2(),2,duration,scorch);
        var zones=(List<StratagemZone>)typeof(StratagemFields).GetField("zones",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        return zones[zones.Count-1];
    }
    private static int PoisonSources(模块_基本属性 target)
    {
        var map=(Dictionary<模块_基本属性,Dictionary<StratagemUnit,StratagemStateHandle>>)typeof(StratagemFields).GetField("poisons",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        return map.TryGetValue(target,out var sources)?sources.Count:0;
    }
    private static void FieldChecks()
    {
        StratagemFields.Clear(); Time.time=0;
        var first=Actor(); var outside=Actor(); StratagemWorld.Enemies.Add(outside);
        var weak=FieldSource(); var strong=FieldSource(25053); var gasA=FieldSource(25055); var gasB=FieldSource(25055);
        var low=AddField(weak);var high=AddField(strong);var a=AddField(gasA);var duplicate=AddField(gasA);var b=AddField(gasB);var fire=AddField(weak,true);
        foreach(var zone in new[]{low,high,a,duplicate,b,fire}) StratagemFields.Enter(zone,first);
        StratagemFields.Tick();
        var states=first.GetComponent<模块_状态属性>().当前状态列表;
        var slow=states.Single(x=>x.状态类型==攻击类型与状态数据库.状态枚举.减速);
        Check(slow.加成属性.当前属性效果.x==strong.Parameters.Slow,"overlapping fields select the strongest slow from actual members");
        Check(states.Count(x=>x.状态类型==攻击类型与状态数据库.状态枚举.中毒)==2 && PoisonSources(first)==2,"gas stacks once per source across overlapping same-source zones");
        Check(states.Count(x=>x.状态类型==攻击类型与状态数据库.状态枚举.灼烧)==1,"scorch member receives native burn");
        Check(StratagemWorld.GlobalScans==0 && outside.GetComponent<模块_状态属性>().当前状态列表.Count==0,"field tick does not enumerate unrelated world enemies");
        StratagemFields.Exit(a,first);Check(PoisonSources(first)==2,"exiting one same-source gas zone preserves its remaining membership");
        var gasTail=states.First(x=>x.状态类型==攻击类型与状态数据库.状态枚举.中毒);
        StratagemFields.Exit(duplicate,first);gasTail.剩余时间=.3f;StratagemFields.Tick();
        Check(gasTail.剩余时间==.3f,"final same-source gas exit stops refreshing its native poison tail");
        StratagemFields.Exit(high,first);StratagemFields.Tick();
        Check(slow.加成属性.当前属性效果.x==gasB.Parameters.Slow,"leaving strongest field downgrades to strongest remaining slow");
        foreach(var zone in new[]{low,b,fire})StratagemFields.Exit(zone,first);
        Check(slow.剩余时间==.5f,"final exit retains native half-second slow tail");
        StratagemFields.Enter(b,first);StratagemFields.Tick();
        var independent=StratagemStateHandle.Create(weak,first,攻击类型与状态数据库.状态枚举.减速,3,.1f);
        StratagemFields.Remove(first);
        Check(PoisonSources(first)==0 && !b.Entered.Contains(first),"unit removal clears field membership and state caches");
        Check(!states.Contains(slow) && states.Count(x=>x.状态类型==攻击类型与状态数据库.状态枚举.中毒)==0 && independent.Valid,
            "space change expires only tracked field states and preserves an unrelated slow");
        var c1=new Collider2D();var c2=new Collider2D();
        low.Membership.Track(c1,first);low.Membership.Track(c2,first);low.Membership.Untrack(c1);
        Check(low.Entered.Contains(first),"one collider exit retains membership while another collider overlaps");
        low.Membership.Untrack(c2);Check(!low.Entered.Contains(first),"last collider exit removes field membership");
        low.Membership.Track(c1,first);StratagemFields.Remove(first);low.Membership.Track(c1,first);
        Check(low.Entered.Contains(first),"unit removal clears sensor contacts so a later entry is accepted");
        first.GetComponent<模块_状态属性>().Tick(4);StratagemFields.Tick();
        Check(PoisonSources(first)==0,"expired poison tail handles are pruned without world scans");
        StratagemFields.Enter(low,first);Time.time=11;StratagemFields.Tick();
        Check(weak.ActiveFields==0 && gasA.ActiveFields==0 && gasB.ActiveFields==0 && strong.ActiveFields==0,"field expiry releases every sensor and source dependency");
        StratagemFields.Clear();StratagemWorld.Enemies.Clear();
        foreach(var source in new[]{weak,strong,gasA,gasB})source.Damage.Dispose();
    }
    static void Main(string[] args)
    {
        if (args.Contains("--effect-protocol"))
        {
            foreach (StratagemEffectAction action in Enum.GetValues(typeof(StratagemEffectAction)))
                Check(new StratagemEffectMessage {Kind=(int)StratagemKind.Barrage,Action=(int)action}.Valid,"defined effect action is accepted by protocol");
            Check(new StratagemEffectMessage {Kind=(int)StratagemKind.Barrage,Action=(int)StratagemEffectAction.ShellWarning,Duration=.09f,Radius=1}.Valid,"authoritative shell warning reaches protocol validation");
            int next=Enum.GetValues<StratagemEffectAction>().Max(x=>(int)x)+1;
            foreach (int action in new[]{-1,next,int.MaxValue})
                Check(!new StratagemEffectMessage {Kind=0,Action=action}.Valid,"undefined effect action is rejected");
            Check(!new StratagemEffectMessage {Kind=0,Action=(int)StratagemEffectAction.ShellWarning,Radius=float.NaN}.Valid,"shell warning still rejects malformed geometry");
            Console.WriteLine($"PASS: {checks} effect protocol checks"); return;
        }
        var source=new GameObject(); var attack=source.Add(new 模块_攻击属性());var detail=source.Add(new 模块_攻击详细属性());
        var unit=source.Add(new StratagemUnit {Kind=StratagemKind.Laser,Parameters=new StratagemParameters(20003,new int[0])});
        unit.Damage=new StratagemDamage(source);var damage=unit.Damage;
        var first=Actor();var second=Actor();var states=first.GetComponent<模块_状态属性>();
        var settings=detail.状态加成列表[(int)攻击类型与状态数据库.状态枚举.灼烧];
        settings.变化持续时间=1; settings.变化属性效果=new Vector4(5,.1f,.2f,.3f);
        settings.变化百分比属性效果=new Vector4(.5f,1,2,3);
        damage.Burn(first,4,2);for(int i=0;i<100;i++)damage.Burn(first,4,2);
        Check(states.当前状态列表.Count==1,"rapid ticks retain a single native burn per source and target");
        var burn=states.当前状态列表[0];
        Check(burn.剩余时间==3&&burn.加成属性.当前属性效果.x==13.5f,"duration and damage upgrades use native state snapshot");
        Check(Math.Abs(burn.加成属性.当前属性效果.y-.2f)<.001f&&Math.Abs(burn.加成属性.当前属性效果.z-.6f)<.001f&&Math.Abs(burn.加成属性.当前属性效果.w-1.2f)<.001f,"each status vector component receives its own modifier");
        states.Tick(2.1f);damage.Burn(first,4,2);
        Check(states.当前状态列表.Count==1,"extended native duration prevents early burn stacking");
        states.Tick(1);damage.Burn(first,4,2);
        Check(states.当前状态列表.Count==1&&!ReferenceEquals(states.当前状态列表[0],burn),"expired burn is replaced with a new native state");
        second.GetComponent<模块_抗性属性>().状态抗性列表[(int)攻击类型与状态数据库.状态枚举.灼烧].当前时间抗性=.5f;
        damage.Burn(second,4,2);
        Check(second.GetComponent<模块_状态属性>().当前状态列表[0].剩余时间==1.5f&&states.当前状态列表[0].剩余时间==3,"native shallow cloning cannot share mutable damage attributes between targets");
        var slow=StratagemStateHandle.Create(unit,first,攻击类型与状态数据库.状态枚举.减速,.75f,.35f);
        int count=states.当前状态列表.Count;var effects=first.GetComponent<模块_状态持续效果>();
        for(int i=0;i<100;i++)slow.Maintain(.75f,.35f);
        Check(states.当前状态列表.Count==count&&effects.当前底层状态列表.Count==1&&effects.Recalculations==0,"persistent slow reuses native state and bottom modifier");
        slow.Maintain(.75f,.5f);
        Check(effects.Recalculations==2&&effects.当前底层状态列表[0].数值==-.5f,"stronger slow recalculates exactly the changed modifier");
        slow.State.自身抗性属性.当前时间抗性=.5f;slow.Maintain(.5f,.5f);
        Check(slow.State.剩余时间==.25f&&slow.Bottom.持续时间==.25f,"refreshed slow duration keeps native resistance");
        attack.当前造成状态.Add(攻击类型与状态数据库.状态枚举.灼烧);attack.当前造成状态.Add(攻击类型与状态数据库.状态枚举.中毒);
        damage.Hit(first,250,true);var packet=first.GetComponent<模块_受伤>().Packets[0];
        Check(packet.攻击伤害==250&&packet.是否暴击&&packet.暴击倍率==1,"original injury receives raw attack and performs critical calculation once");
        Check(packet.造成状态.Count==1&&packet.造成状态[0].状态类型==攻击类型与状态数据库.状态枚举.中毒,"intrinsic burn avoids a duplicate packet burn while retaining other native states");
        second.Enemy=false;damage.Hit(second,250,false);
        Check(second.GetComponent<模块_受伤>().Packets.Count==0,"friendly target receives no damage");
        source.Add(new 模块_状态属性(source,new 模块_抗性属性()));
        var paused=new StratagemDamage(source);source.GetComponent<模块_状态属性>().增加状态(paused.State(攻击类型与状态数据库.状态枚举.眩晕,1,new Vector4()));
        Check(paused.Rate==0,"native stun pauses custom attack rate");
        FieldChecks();
        damage.Burn(first,4,2); paused.Burn(first,4,2);
        StratagemDamage.ForgetTarget(first);
        Check(BurnCount(damage)==1 && BurnCount(paused)==0,"removed target is forgotten by every damage source while other target stays cached");
        damage.Dispose();
        Check(BurnCount(damage)==0 && !Sources().Contains(damage),"destroying damage source releases burn handles and unregisters source");
        StratagemDamage.ClearCaches();
        Check(Sources().Count==0 && BurnCount(paused)==0,"battle teardown clears all damage sources");
        Console.WriteLine($"PASS: {checks} production damage/status checks with native clone and resistance code");
    }
}
