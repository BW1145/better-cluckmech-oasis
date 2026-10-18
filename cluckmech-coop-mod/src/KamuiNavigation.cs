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
