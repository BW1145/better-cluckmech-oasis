using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using PolyNav;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class KamuiNavigation
    {
        private static readonly MethodInfo remove=AccessTools.Method(typeof(PolyNavMap),"RemoveObstacle");
        private static readonly MethodInfo add=AccessTools.Method(typeof(PolyNavMap),"AddObstacle");
        private static readonly FieldInfo pending=AccessTools.Field(typeof(PolyNavMap),"regenerateFlag");
        private static readonly HashSet<PolyNavMap> dirty=new HashSet<PolyNavMap>();
        internal static bool PrivateMap(PolyNavMap map)
        {foreach(var room in KamuiWorld.Rooms.Values)if(room.Map==map||room.AirMap==map)return true;return false;}
        internal static bool Finite(Vector2 point)=>!float.IsNaN(point.x)&&!float.IsInfinity(point.x)&&!float.IsNaN(point.y)&&!float.IsInfinity(point.y);
        internal static bool TryPoint(PolyNavMap map,Vector2 desired,out Vector2 point,float maximumCorrection=float.PositiveInfinity)
        {
            point=desired;if(map==null||!Finite(desired))return false;
            if(map.PointIsValid(desired))return true;
            var candidate=map.GetCloserEdgePoint(desired);
            if(!Finite(candidate)||!map.PointIsValid(candidate)||(candidate-desired).sqrMagnitude>maximumCorrection*maximumCorrection)return false;
            point=candidate;return true;
        }
        internal static bool TryDash(PolyNavMap map,Vector2 start,Vector2 direction,float distance,out Vector2 end)
        {
            end=start;if(map==null||!Finite(start)||!Finite(direction)||!map.PointIsValid(start))return false;
            var desired=start+direction.normalized*distance;
            if(map.PointIsValid(desired)&&map.CheckLOS(start,desired)){end=desired;return true;}
            float low=0,high=1;
            for(int i=0;i<9;i++)
            {
                float middle=(low+high)*.5f;var candidate=Vector2.Lerp(start,desired,middle);
                if(map.PointIsValid(candidate)&&map.CheckLOS(start,candidate))low=middle;else high=middle;
            }
            end=Vector2.Lerp(start,desired,low);return (end-start).sqrMagnitude>.0001f;
        }
        internal static void Transfer(OtherworldUnit unit,int from,int to)
        {
            if(from==to||KamuiWorld.Navigation==null)return;
            var maps=new[]{KamuiWorld.Navigation.地面导航地图,KamuiWorld.Navigation.空中导航地图};
            foreach(var obstacle in unit.GetComponentsInChildren<PolyNavObstacle>(true))
                foreach(var map in maps)
                {
                    if(map==null||((int)map.obstaclesMask&(1<<obstacle.gameObject.layer))==0)continue;
                    if(from==0){remove.Invoke(map,new object[]{obstacle});dirty.Add(map);}
                    if(to==0){add.Invoke(map,new object[]{obstacle});dirty.Add(map);}
                }
            var room=KamuiWorld.Room(from);if(room!=null&&from!=0)room.NavigationDirty=true;
            room=KamuiWorld.Room(to);if(room!=null&&to!=0)room.NavigationDirty=true;
        }
        internal static void Flush()
        {
            foreach(var map in dirty)if(map!=null){pending.SetValue(map,false);map.GenerateMap(false);}
            dirty.Clear();
        }
        internal static void Clear()=>dirty.Clear();
    }
    [HarmonyPatch(typeof(PolyNavAgent),"SetDestination",new[]{typeof(Vector2),typeof(Action<bool>)})]
    internal static class KamuiValidDestination
    {
        private static void Prefix(PolyNavAgent __instance,ref Vector2 goal,out bool? __state)
        {
            __state=null;if(!KamuiNavigation.PrivateMap(__instance.map))return;
            if(KamuiNavigation.TryPoint(__instance.map,goal,out var corrected)){goal=corrected;return;}
            // Let native OnInvalid finish the request and callbacks once, without recursive projection.
            __state=__instance.closerPointOnInvalid;__instance.closerPointOnInvalid=false;
        }
        private static Exception Finalizer(PolyNavAgent __instance,bool? __state,Exception __exception)
        {if(__state.HasValue)__instance.closerPointOnInvalid=__state.Value;return __exception;}
    }
    [HarmonyPatch(typeof(PolyNavMap),"MonitorObstacle")]
    internal static class KamuiObstacleIsolation
    {
        private static bool Prefix(PolyNavMap __instance,PolyNavObstacle obstacle)
        {
            if(KamuiWorld.Rooms.Count==0)return true;
            foreach(var room in KamuiWorld.Rooms.Values)if(room.Map==__instance||room.AirMap==__instance)return false;
            int space=KamuiWorld.At(obstacle.transform.position);if(space==0)return true;
            KamuiWorld.Room(space).NavigationDirty=true;return false;
        }
    }
    [HarmonyPatch(typeof(PolyNavMap),"GenerateMap",new[]{typeof(bool)})]
    internal static class KamuiCustomMapLifetime
    {
        private static bool Prefix(PolyNavMap __instance)
        {foreach(var room in KamuiWorld.Rooms.Values)if(room.Map==__instance||room.AirMap==__instance)return false;return true;}
    }
    [HarmonyPatch]
    internal static class KamuiFactionChanged
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {yield return AccessTools.Method(typeof(模块_基本属性),"阵营转换",new[]{typeof(int)});yield return AccessTools.Method(typeof(模块_基本属性),"阵营转换",new[]{typeof(阵营数据库.阵营)});}
        private static void Postfix(模块_基本属性 __instance)
        {var unit=OtherworldWorld.Unit(__instance.gameObject);if(unit!=null){var room=KamuiWorld.Room(unit.Space);if(room!=null)room.Dirty=true;}}
    }
}
