using System.Collections.Generic;
using UnityEngine;

namespace CluckmechCoop
{
    internal static partial class OtherworldWorld
    {
        internal static void CollectNearby(GameObject source, Vector2 point, float radius, List<OtherworldUnit> result)
        {
            result.Clear();
            int space = Space(source);
            float radiusSquared = radius*radius;
            foreach (var health in KamuiWorld.CandidatesIn(space))
            {
                if (health == null || !Units.TryGetValue(health.gameObject, out var unit)) continue;
                if (unit == null || unit.Space != space || unit.Health.当前阵营 != 阵营数据库.阵营.敌方 ||
                    unit.Health.当前生命值 <= 0 || unit.Health.当前是否无敌) continue;

                bool hurt = false;
                foreach (var shape in unit.Shapes)
                {
                    if (shape != null && shape.enabled && shape.gameObject.activeInHierarchy && shape.isTrigger)
                    {
                        hurt = true;
                        break;
                    }
                }

                foreach (var shape in unit.Shapes)
                {
                    if (shape == null || !shape.enabled || !shape.gameObject.activeInHierarchy || hurt && !shape.isTrigger) continue;
                    if (((Vector2)shape.ClosestPoint(point)-point).sqrMagnitude > radiusSquared) continue;
                    result.Add(unit);
                    break;
                }
            }
        }

        internal static void SortNearest(List<OtherworldUnit> targets, Vector2 point)
        {
            targets.Sort((a, b) => ((Vector2)a.transform.position-point).sqrMagnitude.CompareTo(
                ((Vector2)b.transform.position-point).sqrMagnitude));
        }
    }
}
