using System;
using System.Reflection;
using System.Collections.Generic;
using CluckmechCoop;
using Mirror;
namespace HarmonyLib { [AttributeUsage(AttributeTargets.Class)] sealed class HarmonyPatch:Attribute { public HarmonyPatch(Type type,string method){} } }
namespace UnityEngine
{
    public struct Vector3 { public float x,y,z; }
    public sealed class Transform
    {
        public Vector3 position;public Transform parent;
        public Vector3 TransformPoint(Vector3 p)=>new Vector3{x=position.x+p.x,y=position.y+p.y,z=position.z+p.z};
    }
    public class Component { public object Linked;public T GetComponent<T>() where T:class=>Linked as T;public Transform transform=new Transform(); }
}
namespace Mirror
{
    public enum CoordinateSpace { Local,World }
    public class NetworkTransformBase:UnityEngine.Component
    {
        public readonly List<int> serverSnapshots=new List<int>(),clientSnapshots=new List<int>();
        public int CodecBaseline=123;public CoordinateSpace coordinateSpace;public UnityEngine.Transform target=new UnityEngine.Transform();
    }
    public sealed class NetworkTransformReliable:NetworkTransformBase { }
}
namespace CluckmechCoop
{
    public sealed class OtherworldActor:UnityEngine.Component { public int Hero=13,Space;public bool Dashing,FilterKamuiSnapshots; }
    static class KamuiWorld { internal static int At(UnityEngine.Vector3 p)=>p.x>=100?2:0; }
}
static class Program
{
    static int checks;
    static void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
    static void Main()
    {
        var actor=new OtherworldActor();var sync=new NetworkTransformReliable();actor.Linked=sync;sync.Linked=actor;
        sync.serverSnapshots.Add(1);sync.clientSnapshots.Add(2);
        OtherworldDashNetwork.Transferred(actor,0,2);
        Check(actor.FilterKamuiSnapshots&&sync.serverSnapshots.Count==0&&sync.clientSnapshots.Count==0,"both peers clear interpolation history");
        Check(sync.CodecBaseline==123,"in-flight delta baseline is preserved");actor.Space=2;
        Check(!OtherworldDashNetwork.Accept(sync,new UnityEngine.Vector3{x=5}),"old main-space snapshot rejected after entry");
        Check(OtherworldDashNetwork.Accept(sync,new UnityEngine.Vector3{x=120}),"new room snapshot accepted");
        actor.Space=0;Check(!OtherworldDashNetwork.Accept(sync,new UnityEngine.Vector3{x=120}),"old room snapshot rejected after return");
        Check(OtherworldDashNetwork.Accept(sync,new UnityEngine.Vector3{x=5}),"main-space snapshot accepted after return");
        sync.target.parent=new UnityEngine.Transform{position=new UnityEngine.Vector3{x=110}};
        Check(!OtherworldDashNetwork.Accept(sync,new UnityEngine.Vector3{x=5}),"local snapshot transformed to world before space test");
        sync.coordinateSpace=CoordinateSpace.World;
        Check(OtherworldDashNetwork.Accept(sync,new UnityEngine.Vector3{x=5}),"world snapshot not transformed twice");
        Check(OtherworldDashNetwork.Accept(sync,null),"rotation-only snapshot remains valid");
        actor.FilterKamuiSnapshots=false;Check(OtherworldDashNetwork.Accept(sync,new UnityEngine.Vector3{x=120}),"before any transfer native movement untouched");
        actor.Hero=12;OtherworldDashNetwork.Transferred(actor,0,2);Check(!actor.FilterKamuiSnapshots,"other hero unaffected");actor.Hero=13;
        OtherworldDashNetwork.Transferred(actor,0,0);Check(!actor.FilterKamuiSnapshots,"same-space changes unaffected");
        OtherworldDashNetwork.Transferred(null,0,2);actor.Linked=null;OtherworldDashNetwork.Transferred(actor,0,2);Check(actor.FilterKamuiSnapshots,"missing network component safe");actor.Linked=sync;
        var prefix=typeof(OtherworldDashInterpolation).GetMethod("Prefix",BindingFlags.Static|BindingFlags.NonPublic);
        Check((bool)prefix.Invoke(null,new object[]{sync}),"normal interpolation unchanged");actor.Dashing=true;
        Check(!(bool)prefix.Invoke(null,new object[]{sync}),"dash exclusively owns transform application");sync.Linked=null;
        Check((bool)prefix.Invoke(null,new object[]{sync}),"ordinary actor interpolation unchanged");
        Check(OtherworldDashNetwork.Accept(sync,new UnityEngine.Vector3{x=120}),"ordinary actor snapshot unaffected");
        Console.WriteLine($"PASS {checks} dash-network ownership checks");
    }
}
