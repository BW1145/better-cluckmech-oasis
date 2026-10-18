using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Mirror;
using PolyNav;
using UnityEngine;

namespace CluckmechCoop
{
    internal sealed class KamuiRoom
    {
        internal int Owner;
        internal Vector2 Center;
        internal Rect Bounds;
        internal GameObject Root;
        internal PolyNavMap Map,AirMap;
        internal Vector2[] Floor;
        internal readonly HashSet<OtherworldUnit> Units=new HashSet<OtherworldUnit>();
        internal readonly List<KamuiCapture> Temporary=new List<KamuiCapture>();
        internal bool Dirty=true,NavigationDirty,Idle;
        internal readonly Dictionary<int,int> CategoryCounts=new Dictionary<int,int>();
        internal List<模块_基本属性> Candidates=new List<模块_基本属性>();
        internal readonly Dictionary<string,GameObject[]> Tags=new Dictionary<string,GameObject[]>();
    }
    internal sealed class KamuiCapture
    {
        internal OtherworldUnit Unit;
        internal Vector2 Origin;
        internal float Until;
    }
    internal static partial class KamuiWorld
    {
        internal static readonly Dictionary<int,KamuiRoom> Rooms=new Dictionary<int,KamuiRoom>();
        private static readonly KamuiRoom main=new KamuiRoom();
        private static 导航管理器 navigation;
        internal static 导航管理器 Navigation=>navigation;
        private static KamuiClock clock;
        internal static bool Restoring;
        internal static OtherworldRun Run(int space)=>space==0?null:OtherworldProgress.For(space-1);
        internal static int At(Vector2 point)
        {foreach(var room in Rooms.Values)if(room.Bounds.Contains(point))return room.Owner+1;return 0;}
        internal static PolyNavMap MapFor(OtherworldUnit unit,KamuiRoom room)
        {
            bool flying=unit.Health.单位类型==模块_基本属性.单位类型枚举.飞行单位;
            return room==main?(flying?navigation.空中导航地图:navigation.地面导航地图):(flying?room.AirMap:room.Map);
        }
        internal static KamuiRoom Room(int space)=>space==0?main:Rooms.TryGetValue(space,out var room)?room:null;
        internal static KamuiRoom Ensure(int role)
        {
            int space=role+1;if(Rooms.TryGetValue(space,out var room))return room;
            if(navigation==null)navigation=Object.FindObjectOfType<导航管理器>();
            float right=0,middle=0;
            var collider=navigation.地面导航地图.GetComponent<Collider2D>();
            if(collider!=null){right=collider.bounds.max.x;middle=collider.bounds.center.y;}
            var center=new Vector2(right+100+Mathf.Max(0,role-1)*100,middle);
            var root=new GameObject("KamuiSpace_"+role);root.SetActive(false);
            room=new KamuiRoom{Owner=role,Center=center,Bounds=new Rect(center.x-19.55f,center.y-14.45f,39.1f,28.9f),Root=root};
            Rooms.Add(space,room);
            var ground=new GameObject("KamuiPlatform");ground.transform.SetParent(root.transform,false);ground.transform.position=center;
            var renderer=ground.AddComponent<SpriteRenderer>();renderer.sprite=OtherworldAssets.Ground;renderer.sortingOrder=-20;
            ground.transform.localScale=new Vector3(.51f,.85f,1);
            room.Map=root.AddComponent<PolyNavMap>();room.Map.obstaclesMask=0;room.Map.autoRegenerateInterval=0;
            room.Floor=new[]{center+new Vector2(-17.7f,-11.8f)*.85f,center+new Vector2(17.7f,-11.8f)*.85f,center+new Vector2(14.45f,11.6f)*.85f,center+new Vector2(-14.45f,11.6f)*.85f};
            room.Map.SetCustomMap(new PolyNavMap.PolyMap(new PolyNavMap.Polygon(room.Floor)));
            var air=new GameObject("AirNavigation");air.transform.SetParent(root.transform,false);
            room.AirMap=air.AddComponent<PolyNavMap>();room.AirMap.obstaclesMask=0;room.AirMap.autoRegenerateInterval=0;
            room.AirMap.SetCustomMap(new PolyNavMap.PolyMap(new PolyNavMap.Polygon(room.Floor)));
            for(int i=0;i<room.Floor.Length;i++)
            {
                var edge=new GameObject("Boundary");edge.transform.SetParent(root.transform,false);edge.layer=10;
                edge.AddComponent<EdgeCollider2D>().points=new[]{room.Floor[i],room.Floor[(i+1)%room.Floor.Length]};
            }
            root.SetActive(true);
            foreach(var unit in OtherworldWorld.Units.Values.ToArray())
                if(unit.Space==0&&room.Bounds.Contains(unit.transform.position)){UnitLifecycle.ChangingSpace(unit);main.Units.Remove(unit);unit.Space=space;room.Units.Add(unit);Navigate(unit,room);}
            main.Dirty=room.Dirty=true;
            if(clock==null)clock=Plugin.Instance.gameObject.AddComponent<KamuiClock>();
            KamuiVisibility.Install();
            if(Plugin.Role==1)OtherworldNetwork.Send(new OtherworldMessage{Kind=OtherworldMessageKind.EnsureRoom,Value=role},role);
            return room;
        }
        internal static void Added(OtherworldUnit unit)
        {
            unit.Space=At(unit.transform.position);var room=Room(unit.Space);if(room==null)return;
            room.Units.Add(unit);room.Dirty=true;room.NavigationDirty|=unit.Space!=0&&unit.Health.单位类型==模块_基本属性.单位类型枚举.建筑;
            if(unit.Space!=0)Navigate(unit,room);
        }
        internal static void Removed(OtherworldUnit unit)
        {
            var room=Room(unit.Space);if(room!=null){room.Units.Remove(unit);room.Dirty=true;room.NavigationDirty|=unit.Space!=0&&unit.Health.单位类型==模块_基本属性.单位类型枚举.建筑;}
        }
        internal static int Categories(int space,int role)
        {
            var room=Room(space);if(room==null)return 0;
            RebuildCandidates(room);return room.CategoryCounts.TryGetValue(role,out var count)?count:0;
        }
        internal static bool Idle(int space)
        {var room=Room(space);if(room==null)return true;RebuildCandidates(room);return room.Idle;}
        private static void RebuildCandidates(KamuiRoom room)
        {
            if(!room.Dirty)return;
            room.Candidates=room.Units.Where(u=>u!=null&&u.Health!=null).Select(u=>u.Health).ToList();
            room.CategoryCounts.Clear();
            foreach(var group in room.Units.Where(u=>u!=null&&u.IsMachine&&u.Health.当前生命值>0).GroupBy(u=>(int)u.Health.所属玩家))
                room.CategoryCounts[group.Key]=group.Select(u=>u.Health.单位分类).Distinct().Count();
            room.Idle=!room.Candidates.Any(u=>u.当前生命值>0&&u.当前阵营==阵营数据库.阵营.敌方)||!room.Candidates.Any(u=>u.当前生命值>0&&u.当前阵营==阵营数据库.阵营.友方);
            foreach(var unit in room.Units)
                if(unit.Attack!=null&&unit.Attack.攻击目标!=null&&OtherworldWorld.Space(unit.Attack.攻击目标.gameObject)!=unit.Space)unit.Attack.攻击目标=null;
            room.Tags.Clear();room.Dirty=false;
        }
        internal static List<模块_基本属性> Candidates(Component owner)
        {
            var room=Room(OtherworldWorld.Space(owner.gameObject));
            if(room==null)return new List<模块_基本属性>();
            RebuildCandidates(room);
            return room.Candidates;
        }
        internal static List<模块_基本属性> CandidatesAt(Vector2 point)
            =>CandidatesIn(At(point));
        internal static List<模块_基本属性> CandidatesIn(int space)
        {var room=Room(space);if(room==null)return main.Candidates;RebuildCandidates(room);return room.Candidates;}
        internal static GameObject[] Tagged(string tag,Component owner)
        {
            if(Rooms.Count==0)return GameObject.FindGameObjectsWithTag(tag);
            var room=Room(OtherworldWorld.Space(owner.gameObject));if(room==null)return new GameObject[0];
            var candidates=Candidates(owner);
            if(!room.Tags.TryGetValue(tag,out var units)){units=candidates.Where(u=>u.gameObject.CompareTag(tag)).Select(u=>u.gameObject).ToArray();room.Tags[tag]=units;}
            return units;
        }
        internal static List<模块_基本属性> LocalUnits(Component owner)=>Rooms.Count==0?世界事件系统.场上所有单位:Candidates(owner);
        internal static void Dash(OtherworldActor actor,Vector2 direction)
        {if(actor.Space==0&&Plugin.Spawner!=null&&Plugin.Spawner.关卡状态!=刷怪器.关卡状态枚举.结束&&Plugin.Spawner.关卡状态!=刷怪器.关卡状态枚举.无)actor.StartCoroutine(DashRoutine(actor,direction));}
        private static IEnumerator DashRoutine(OtherworldActor actor,Vector2 direction)
        {
            var owner=OtherworldWorld.Unit(actor.gameObject);if(owner==null)yield break;
            ulong lease=owner.Actions.Capture();
            var start=(Vector2)actor.transform.position;
            if(navigation==null)navigation=Object.FindObjectOfType<导航管理器>();
            if(navigation==null||!KamuiNavigation.TryDash(navigation.地面导航地图,start,direction,actor.Run.Has(26114)?5:3,out var end))yield break;
            actor.BeginDash(direction);
            try
            {
            var captured=new HashSet<OtherworldUnit>();Vector2 previous=start;
            OtherworldNetwork.Effect(actor,6,end,.35f);
            for(float elapsed=0;elapsed<.35f;elapsed+=Time.deltaTime)
            {
                if(actor==null||!owner.Actions.Current(lease))yield break;
                if(actor.Death.是否玩家死亡){actor.Dashing=false;yield break;}
                var point=Vector2.Lerp(start,end,elapsed/.35f);actor.transform.position=point;
                Sweep(actor,previous,point,captured);previous=point;yield return null;
            }
            if(actor==null||!owner.Actions.Current(lease))yield break;
            actor.transform.position=end;Sweep(actor,previous,end,captured);actor.ReturnPoint=end;
            var room=Ensure(actor.Role);Vector2 destination=room.Center;
            if(!Transfer(OtherworldWorld.Unit(actor.gameObject),actor.Role+1,destination))yield break;
            foreach(var unit in captured.ToArray())
            {
                if(unit==null||unit.Health.当前生命值<=0)continue;
                var group=Group(unit).ToArray();var origins=group.ToDictionary(member=>member,member=>(Vector2)member.transform.position);
                foreach(var member in group)
                {
                    if(member.Space!=0)continue;
                    var origin=origins[member];var position=destination+Vector2.ClampMagnitude(origin-end,8);
                    bool permanent=actor.Run.Has(26113)&&Ordinary(member);
                    if(!Transfer(member,actor.Role+1,position,false))continue;
                    if(permanent){member.Permanent=true;RemoveFromEncounter(member);}
                    else room.Temporary.Add(new KamuiCapture{Unit=member,Origin=origin,Until=Time.time+(actor.Run.Has(26114)?8:5)});
                }
            }
            KamuiVisibility.Refresh();
            OtherworldDeployment.SpaceChanged(actor);
            }
            finally {if(actor!=null)actor.Dashing=false;}
        }
        private static void Sweep(OtherworldActor actor,Vector2 from,Vector2 to,HashSet<OtherworldUnit> captured)
        {
            foreach(var unit in main.Units)
            {
                if(unit==null||!Difficulty.CountsForEncounter(unit.gameObject)||unit.Health.当前生命值<=0)continue;
                foreach(var shape in unit.Shapes)
                {
                    if(shape==null||!shape.enabled||!shape.gameObject.activeInHierarchy)continue;
                    Vector2 closest=shape.ClosestPoint(to);Vector2 segment=to-from;
                    float t=segment.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(closest-from,segment)/segment.sqrMagnitude):0;
                    if((closest-(from+segment*t)).sqrMagnitude<=.65f*.65f){captured.Add(unit);break;}
                }
            }
        }
        private static IEnumerable<OtherworldUnit> Group(OtherworldUnit unit)
        {
            var group=new HashSet<OtherworldUnit>{unit};
            foreach(var boss in Object.FindObjectsOfType<词条_100028_Boss胜利判断>())
            {
                if(boss.gameObject!=unit.gameObject&&(boss.子物体==null||!boss.子物体.Contains(unit.gameObject)))continue;
                var root=OtherworldWorld.Unit(boss.gameObject);if(root!=null)group.Add(root);
                if(boss.子物体!=null)foreach(var part in boss.子物体){var member=OtherworldWorld.Unit(part);if(member!=null)group.Add(member);}
                foreach(var member in group)member.BossGroup=true;
            }
            if(unit.GetComponent<最终boss状态机>()!=null)unit.BossGroup=true;
            return group.OrderBy(u=>Depth(u.transform));
        }
        private static int Depth(Transform transform){int value=0;while(transform.parent!=null){value++;transform=transform.parent;}return value;}
        internal static bool Ordinary(OtherworldUnit unit)=>!unit.BossGroup&&unit.GetComponent<词条_100028_Boss胜利判断>()==null&&unit.GetComponent<最终boss状态机>()==null&&unit.GetComponent<模块_BOSS生命显示>()==null&&unit.Health.强化词条列表.Count==0;
        private static void RemoveFromEncounter(OtherworldUnit unit)
        {
            Plugin.Spawner?.波次存活怪物.Remove(unit.gameObject);
            var tracked=unit.GetComponent<EncounterEnemy>();
            if(tracked?.Owner!=null)
            {
                if(tracked.Owner.Living.Remove(unit.gameObject)&&tracked.Owner.StageCounts.TryGetValue(tracked.Stage,out var count)){count.Enemies--;if(tracked.Boss)count.Bosses--;}
                tracked.Owner=null;
            }
            WealthEncounter.ReleaseCaptive(unit.gameObject);
            if(unit.GetComponent<词条_4_敌人死亡统计>()!=null)Plugin.Spawner?.gameObject.SendMessage("减少敌人",SendMessageOptions.DontRequireReceiver);
        }
        internal static void Return(OtherworldActor actor)
        {
            if(!Transfer(OtherworldWorld.Unit(actor.gameObject),0,actor.ReturnPoint))return;
            OtherworldDeployment.SpaceChanged(actor);
            actor.EndPhase(false);
            if(actor.Run.Has(26115))actor.BeginPhase(true);else actor.EndAction();
        }
        internal static bool Transfer(OtherworldUnit unit,int space,Vector2 point,bool refresh=true)
        {
            if (unit==null) return false;
            var next=Room(space);
            if (next==null) return false;
            var map=MapFor(unit,next);
            if (!KamuiNavigation.TryPoint(map,point,out point,4) || space!=0&&!next.Bounds.Contains(point)) return false;

            int originalSpace=unit.Space;
            UnitLifecycle.ChangingSpace(unit);
            var body=unit.GetComponent<OtherworldActor>()?.Body??unit.GetComponentInChildren<SpriteRenderer>();
            if (body!=null) OtherworldWarpVisual.Play(body,false,.55f,false);
            var old=Room(unit.Space);
            old?.Units.Remove(unit);
            if (old!=null) old.Dirty=true;
            unit.Space=space;
            next.Units.Add(unit);
            next.Dirty=true;
            var rigidbody=unit.GetComponent<Rigidbody2D>();
            if (rigidbody!=null)
            {
                rigidbody.position=point;
                rigidbody.MovePosition(point);
            }
            unit.transform.position=point;
            Navigate(unit,next);
            ClearTargets(unit);
            unit.Refresh();
            if (unit.IsPlayer&&(int)unit.Health.所属玩家==Plugin.Role)
            {
                var camera=Object.FindObjectOfType<摄像机固定>();
                if (camera!=null)
                {
                    var position=new Vector3(point.x,point.y,0);
                    AccessTools.Field(typeof(摄像机固定),"实际位置").SetValue(camera,position);
                    AccessTools.Field(typeof(摄像机固定),"过渡位置").SetValue(camera,Vector3.zero);
                    AccessTools.Field(typeof(摄像机固定),"跟随目标位置").SetValue(camera,position);
                    camera.transform.position=position;
                }
            }
            KamuiNavigation.Transfer(unit,originalSpace,space);
            OtherworldDashNetwork.Transferred(unit.GetComponent<OtherworldActor>(),originalSpace,space);
            if (body!=null) OtherworldWarpVisual.Play(body,true);
            if (Plugin.Role==1)
            {
                OtherworldNetwork.Send(new OtherworldMessage
                {
                    Kind=OtherworldMessageKind.Transfer,
                    Player=StratagemWorld.Id(unit.gameObject),
                    Value=space,
                    X=point.x,
                    Y=point.y
                });
                var identity=unit.GetComponent<NetworkIdentity>();
                if (identity!=null) NetworkServer.RebuildObservers(identity,false);
            }
            if (refresh) KamuiVisibility.Refresh();
            return true;
        }
        private static void Navigate(OtherworldUnit unit,KamuiRoom room)
        {
            var agent=unit.GetComponent<PolyNavAgent>();if(agent!=null)
            {agent.Stop();agent.map=MapFor(unit,room);}
            if(unit.Movement!=null){unit.Movement.初始位置=unit.transform.position;unit.Movement.目标坐标=unit.transform.position;}
        }
        private static void ClearTargets(OtherworldUnit unit)
        {
            if(unit.Attack!=null)unit.Attack.攻击目标=null;
            var aggro=unit.GetComponent<模块_仇恨>();if(aggro!=null){aggro.当前仇恨列表.Clear();aggro.最大仇恨单位=null;}
            if(unit.Movement!=null){unit.Movement.前进目标=null;unit.Movement.前进副目标=null;}
        }
        internal static void ReceiveTransfer(OtherworldMessage message)
        {
            var obj=KamuiVisibility.Find(message.Player);if(obj==null)return;
            var health=obj.GetComponent<模块_基本属性>();OtherworldWorld.Add(health);
            if(message.Value!=0)Ensure(message.Value-1);
            var unit=OtherworldWorld.Unit(obj);if(unit==null)return;
            if(!Transfer(unit,message.Value,new Vector2(message.X,message.Y)))return;
            var actor=obj.GetComponent<OtherworldActor>();if(actor!=null){actor.CancelClientDash();if(message.Value==0)actor.EndPhase(false);OtherworldDeployment.SpaceChanged(actor);}
        }
        internal static void Tick()
        {
            KamuiNavigation.Flush();
            if(Plugin.Role==2)return;
            foreach(var room in Rooms.Values)
            {
                if(room.NavigationDirty)RebuildNavigation(room);
                for(int i=room.Temporary.Count-1;i>=0;i--)
                {
                    var capture=room.Temporary[i];
                    if(capture.Unit==null||capture.Unit.Health.当前生命值<=0){room.Temporary.RemoveAt(i);continue;}
                    if(Time.time<capture.Until)continue;
                    if(Transfer(capture.Unit,0,capture.Origin))room.Temporary.RemoveAt(i);else capture.Until=Time.time+.25f;
                }
            }
        }
        private static void RebuildNavigation(KamuiRoom room)
        {
            room.NavigationDirty=false;var obstacles=new List<PolyNavMap.Polygon>();
            foreach(var unit in room.Units)
            {
                if(unit==null||unit.Health.单位类型!=模块_基本属性.单位类型枚举.建筑||!unit.Health.启用建筑体积)continue;
                Vector2 point=unit.transform.position,size=unit.Health.建筑体积*.5f;
                obstacles.Add(new PolyNavMap.Polygon(new[]{point+new Vector2(-size.x,-size.y),point+new Vector2(-size.x,size.y),point+new Vector2(size.x,size.y),point+new Vector2(size.x,-size.y)}));
            }
            room.Map.SetCustomMap(new PolyNavMap.PolyMap(new PolyNavMap.Polygon(room.Floor),obstacles.ToArray()));
            foreach(var unit in room.Units){var agent=unit.GetComponent<PolyNavAgent>();if(agent!=null&&agent.map==room.Map)agent.地图障碍物改变();}
        }
        internal static void Clear()
        {
            Restoring=false;
            foreach (var room in Rooms.Values)
                if (room.Root!=null) Object.Destroy(room.Root);
            Rooms.Clear();
            main.Units.Clear();
            main.Candidates.Clear();
            main.Tags.Clear();
            main.Dirty=true;
            navigation=null;
            if (clock!=null) Object.Destroy(clock);
            clock=null;
            KamuiVisibility.Clear();
            KamuiNavigation.Clear();
            OtherworldFx.Clear();
            KamuiDrop.Live.Clear();
            OtherworldVfx.Clear();
        }
    }
    internal sealed class KamuiClock:MonoBehaviour {private void Update()=>KamuiWorld.Tick();}
}
