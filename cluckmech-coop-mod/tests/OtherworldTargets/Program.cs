using System;
using System.Collections.Generic;
using System.Linq;
using CluckmechCoop;
using UnityEngine;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x,y;
        public Vector2(float x,float y) { this.x=x; this.y=y; }
        public float sqrMagnitude => x*x+y*y;
        public static Vector2 operator -(Vector2 a,Vector2 b) => new Vector2(a.x-b.x,a.y-b.y);
    }
    public sealed class Transform { public Vector2 position; }
    public sealed class GameObject
    {
        public readonly Transform transform = new Transform();
        public bool activeInHierarchy = true;
    }
    public sealed class Collider2D
    {
        public readonly GameObject gameObject = new GameObject();
        public bool enabled = true, isTrigger = true;
        public Vector2 Contact;
        public Vector2 ClosestPoint(Vector2 point) => Contact;
    }
}
public static class 阵营数据库 { public enum 阵营 { 友方,敌方 } }
public sealed class 模块_基本属性
{
    public readonly GameObject gameObject = new GameObject();
    public 阵营数据库.阵营 当前阵营 = 阵营数据库.阵营.敌方;
    public float 当前生命值 = 10;
    public bool 当前是否无敌;
}
namespace CluckmechCoop
{
    internal sealed class OtherworldUnit
    {
        internal 模块_基本属性 Health = new 模块_基本属性();
        internal int Space;
        internal Collider2D[] Shapes;
        internal Transform transform => Health.gameObject.transform;
    }
    internal static partial class OtherworldWorld
    {
        internal static readonly Dictionary<GameObject,OtherworldUnit> Units = new Dictionary<GameObject,OtherworldUnit>();
        internal static int Space(GameObject source) => source != null && Units.TryGetValue(source,out var unit) ? unit.Space : 0;
    }
    internal static class KamuiWorld
    {
        internal static readonly Dictionary<int,List<模块_基本属性>> Candidates = new Dictionary<int,List<模块_基本属性>>();
        internal static List<模块_基本属性> CandidatesIn(int space) => Candidates[space];
    }
}
static class Program
{
    private static int checks;
    private static void Check(bool value,string message)
    {
        if (!value) throw new Exception(message);
        checks++;
    }
    private static OtherworldUnit Unit(float position,float contact,int space=0)
    {
        var unit = new OtherworldUnit {Space=space,Shapes=new[]{new Collider2D {Contact=new Vector2(contact,0)}}};
        unit.transform.position = new Vector2(position,0);
        OtherworldWorld.Units.Add(unit.Health.gameObject,unit);
        if (!KamuiWorld.Candidates.TryGetValue(space,out var candidates)) KamuiWorld.Candidates[space]=candidates=new List<模块_基本属性>();
        candidates.Add(unit.Health);
        return unit;
    }
    static void Main()
    {
        var far = Unit(9,2);
        var near = Unit(1,1);
        var outside = Unit(0,4);
        var friend = Unit(0,1); friend.Health.当前阵营=阵营数据库.阵营.友方;
        var dead = Unit(0,1); dead.Health.当前生命值=0;
        var invincible = Unit(0,1); invincible.Health.当前是否无敌=true;
        var disabled = Unit(0,1); disabled.Shapes[0].enabled=false;
        var inactive = Unit(0,1); inactive.Shapes[0].gameObject.activeInHierarchy=false;
        var otherSpace = Unit(0,1,2);
        var staleSpace = Unit(0,1); staleSpace.Space=2;
        var unknown = new 模块_基本属性(); KamuiWorld.Candidates[0].Add(unknown);
        var buffer = new List<OtherworldUnit> {outside};
        OtherworldWorld.CollectNearby(null,new Vector2(),3,buffer);
        Check(buffer.SequenceEqual(new[]{far,near}),"candidate collection clears caller buffer, filters eligibility and preserves candidate order without sorting");
        Check(buffer.Contains(far),"radius uses collider contact rather than distant transform center");
        OtherworldWorld.SortNearest(buffer,new Vector2());
        Check(buffer.SequenceEqual(new[]{near,far}),"ordered selection sorts only when requested");
        OtherworldWorld.CollectNearby(null,new Vector2(),3,buffer);
        OtherworldWorld.SortNearest(buffer,new Vector2(10,0));
        Check(buffer.SequenceEqual(new[]{far,near}),"aim ordering is independent of range origin");
        var nested = new List<OtherworldUnit>();
        var visited = new List<OtherworldUnit>();
        foreach (var target in buffer)
        {
            OtherworldWorld.CollectNearby(otherSpace.Health.gameObject,new Vector2(),3,nested);
            OtherworldWorld.SortNearest(nested,new Vector2());
            visited.Add(target);
        }
        Check(visited.SequenceEqual(new[]{far,near}) && nested.SequenceEqual(new[]{otherSpace}),"nested death/status query uses independent buffers and cannot overwrite an active selection");
        var hurt = Unit(0,9);
        hurt.Shapes = new[]{new Collider2D {isTrigger=false,Contact=new Vector2(0,0)},new Collider2D {Contact=new Vector2(9,0)}};
        OtherworldWorld.CollectNearby(null,new Vector2(),3,buffer);
        Check(!buffer.Contains(hurt),"active hurt trigger takes precedence over a nearby non-trigger body");
        hurt.Shapes[1].gameObject.activeInHierarchy=false;
        OtherworldWorld.CollectNearby(null,new Vector2(),3,buffer);
        Check(buffer.Contains(hurt),"inactive hurt shape does not suppress a live body collider fallback");
        OtherworldWorld.CollectNearby(null,new Vector2(),2,buffer);
        Check(buffer.Contains(far),"radius boundary is inclusive");
        Check(!buffer.Contains(otherSpace) && !buffer.Contains(staleSpace),"candidate registry and unit space both enforce isolation");
        Console.WriteLine($"PASS: {checks} range candidate, explicit ordering and reentrant snapshot checks");
    }
}
