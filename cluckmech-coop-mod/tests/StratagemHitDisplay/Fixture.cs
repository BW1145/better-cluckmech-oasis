// Unity, transport, native gameplay and rendering substitutes. The capture,
// queue, replay, source method and native-sum transpiler are production code.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using CluckmechCoop;

namespace HarmonyLib
{
    public class HarmonyPriority:Attribute {public HarmonyPriority(int priority){}}
    public static class Priority {public const int First=800;}
    public class CodeInstruction
    {
        public OpCode opcode;public object operand;
        public List<Label> labels=new();public List<object> blocks=new();
        public CodeInstruction(OpCode op,object arg=null){opcode=op;operand=arg;}
        public CodeInstruction(CodeInstruction c){opcode=c.opcode;operand=c.operand;labels=new(c.labels);blocks=new(c.blocks);}
    }
    public static partial class AccessTools
    {
        public static FieldInfo Field(Type type,string name)=>type.GetField(name,BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
        public static MethodInfo DeclaredMethod(Type type,string name,Type[] args)=>Method(type,name,args);
    }
}
public class 世界类
{
    public class 伤害数据包
    {
        public GameObject 攻击者;public Transform 攻击目标;public Vector3 攻击坐标;
        public 阵营数据库.阵营 所属阵营;
        public float 攻击伤害,暴击倍率;
        public bool 是否暴击,是否产生仇恨,是否爆炸,是否击退;
        public enum 击退类型枚举 {相对位置击退} public 击退类型枚举 击退类型;
        public Vector4 击退参数;
        public List<模块_状态属性.状态类> 造成状态=new();
    }
}
public partial class 模块_基本属性
{
    public enum 单位定位枚举 {矿山}
    public List<单位定位枚举> 单位定位列表=new();
}
public partial class 模块_攻击属性 {public List<攻击类型与状态数据库.状态枚举> 当前造成状态=new();}
public partial class 模块_受伤
{
    public void 受到伤害(float value)=>throw new InvalidOperationException("Display replay must not run damage");
    public void 受到伤害(世界类.伤害数据包 packet)
    {
        if(基本属性.当前是否无敌)return;
        float value=OtherworldHitDisplay.Value(packet.攻击伤害,基本属性.当前抗性,基本属性.当前格挡值,基本属性.当前是否保护,packet.是否暴击,packet.暴击倍率);
        基本属性.当前生命值-=value;战斗事件.Events++;
        foreach(var state in packet.造成状态)状态属性.增加状态(state);
        Show(value,packet.是否暴击);
    }
    public void ShowColored(float value,int color)=>伤害出现(value,color==1?new Vector4(1,.4f,.2f,1):color==2?new Vector4(.2f,1,.2f,1):new Vector4(.7f,0,0,1));
}
public class 模块_死亡:Component { }
namespace CluckmechCoop
{
    internal static class OtherworldFlatDamage { }
    internal static class OtherworldRegistry {internal static bool Enabled=true;}
    internal enum StratagemKind {Mines,Barrage,Laser,Eagle,Mortar,Shield,Rover}
    internal sealed class StratagemUnit:Component {internal StratagemKind Kind;internal StratagemParameters Parameters=new();}
    internal sealed class StratagemParameters {internal bool BurningMines,BurningRover,Scorch;}
    internal sealed class StratagemStateHandle { }
    internal static partial class StratagemWorld
    {
        internal static bool Living(模块_基本属性 target)=>target!=null&&target.当前生命值>0&&!target.当前是否无敌&&target.当前阵营==阵营数据库.阵营.敌方;
    }
}
