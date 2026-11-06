using System.Collections.Generic;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class NativeCombatTargets
    {
        private static 阵营数据库 factions;
        internal static bool CanDamage(GameObject source,模块_基本属性 target)
        {
            if(target==null||target.当前生命值<=0||target.当前是否无敌||target.GetComponent<模块_受伤>()==null||
                OtherworldWorld.Space(source)!=OtherworldWorld.Space(target.gameObject))return false;
            if(factions==null)factions=GameObject.Find(函数库.常数类.命名_世界)?.GetComponent<阵营数据库>();
            var owner=source.GetComponent<模块_基本属性>();
            return factions!=null&&owner!=null&&factions.运算是否可伤害((int)owner.当前阵营,(int)target.当前阵营);
        }
        internal static Dictionary<模块_基本属性,float> Scenery(GameObject source,Vector2 point,float radius)
        {
            var result=new Dictionary<模块_基本属性,float>();
            foreach(var shape in Physics2D.OverlapCircleAll(point,radius))
            {
                var target=shape.GetComponentInParent<模块_基本属性>();
                if(!CanDamage(source,target)||StratagemWorld.Present(target))continue;
                float distance=((Vector2)shape.ClosestPoint(point)-point).sqrMagnitude;
                if(!result.TryGetValue(target,out float previous)||distance<previous)result[target]=distance;
            }
            return result;
        }
    }
}
