using System;
using System.Collections.Generic;
using System.Linq;
using CluckmechCoop;
using UnityEngine;

namespace UnityEngine
{
    public enum SendMessageOptions { DontRequireReceiver }
    public sealed class Transform { public Vector3 position; }
    public struct Vector3 { public float x,y,z; public static implicit operator Vector2(Vector3 v) => new Vector2(v.x,v.y); }
    public struct Vector2
    {
        public float x,y; public Vector2(float a,float b) {x=a;y=b;}
        public float sqrMagnitude => x*x+y*y;
        public static Vector2 operator -(Vector2 a,Vector2 b) => new Vector2(a.x-b.x,a.y-b.y);
    }
    public struct Vector4
    {
        public float x,y,z,w; public Vector4(float a,float b,float c,float d) {x=a;y=b;z=c;w=d;}
        public static Vector4 operator +(Vector4 a,Vector4 b) => new Vector4(a.x+b.x,a.y+b.y,a.z+b.z,a.w+b.w);
    }
    public static class Random { public static float value = .5f; }
    public sealed class GameObject
    {
        public readonly Transform transform = new Transform();
        private readonly Dictionary<Type,object> parts = new Dictionary<Type,object>();
        public readonly Dictionary<string,int> Messages = new Dictionary<string,int>();
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
    internal sealed class StratagemUnit { internal StratagemDamage Damage; internal StratagemKind Kind; internal StratagemParameters Parameters; }
    internal static class StratagemWorld
    {
        internal static List<模块_基本属性> Enemies=new List<模块_基本属性>();
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
    static void Main()
    {
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
        Console.WriteLine($"PASS: {checks} production damage/status checks with native clone and resistance code");
    }
}
