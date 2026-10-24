using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class ThemeCombat
    {
        internal static bool Friendly(模块_基本属性 health) => health != null && health.当前生命值 > 0 &&
            health.gameObject.activeInHierarchy && health.当前阵营 == 阵营数据库.阵营.友方;

        internal static float ShapeDistance(模块_基本属性 target, Vector2 point)
        {
            float distance = float.PositiveInfinity;
            foreach (var collider in target.GetComponents<Collider2D>())
                if (collider.enabled && collider.isTrigger) distance = Mathf.Min(distance, (collider.ClosestPoint(point) - point).sqrMagnitude);
            return float.IsPositiveInfinity(distance) ? ((Vector2)target.transform.position - point).sqrMagnitude : distance;
        }

        internal static void Hit(ThemeEnemyActor source, 模块_基本属性 target, float referenceDamage, Vector2 point)
        {
            if (!Friendly(target) || Plugin.Role == 2) return;
            float scale = source.Definition.FinalDamage <= 0 ? 1 : source.Attack.当前攻击力 / source.Definition.FinalDamage;
            HitRaw(source, target, referenceDamage * scale, point);
        }

        internal static void HitRaw(ThemeEnemyActor source, 模块_基本属性 target, float damage, Vector2 point)
        {
            if (!Friendly(target) || Plugin.Role == 2) return;
            target.GetComponent<模块_受伤>()?.受到伤害(new 世界类.伤害数据包 {
                攻击者 = source.gameObject, 攻击目标 = target.transform, 攻击坐标 = point,
                所属阵营 = source.Stats.当前阵营, 攻击伤害 = damage, 是否产生仇恨 = true
            });
        }

        internal static void Area(ThemeEnemyActor source, Vector2 point, float radius, float referenceDamage)
        {
            // One native health component is one target, regardless of its number of collision shapes.
            for (int i = 世界事件系统.场上所有单位.Count - 1; i >= 0; i--)
            {
                if (i >= 世界事件系统.场上所有单位.Count) continue;
                var target = 世界事件系统.场上所有单位[i];
                if (Friendly(target) && ShapeDistance(target, point) <= radius * radius) Hit(source, target, referenceDamage, point);
            }
        }
    }

    public sealed class ThemeVolleyProjectile : MonoBehaviour
    { internal HashSet<GameObject> Targets; }

    [HarmonyPatch(typeof(模块_伤害精灵), "OnTriggerEnter2D")]
    internal static class ThemeVolleyHit
    {
        private static bool Prefix(模块_伤害精灵 __instance, Collider2D col)
        {
            var volley = __instance.GetComponent<ThemeVolleyProjectile>();
            if (volley == null || volley.Targets == null || !col.TryGetComponent<模块_基本属性>(out var target) ||
                target.当前是否无敌 || target.当前阵营 == __instance.伤害数据.所属阵营) return true;
            return volley.Targets.Add(col.gameObject);
        }
    }

    [HarmonyPatch(typeof(模块_移动), "移动目标赋值", new[] { typeof(Vector2) })]
    internal static class ThemeHeldMovement
    {
        private static bool Prefix(模块_移动 __instance)
        {
            var actor = __instance.GetComponent<ThemeEnemyActor>();
            return actor == null || !actor.MovementHeld && actor.ExternalMovementHolds == 0;
        }
    }

    [HarmonyPatch(typeof(模块_受伤), "受到伤害", new[] { typeof(世界类.伤害数据包) })]
    internal static class ThemeDirectionalDamage
    {
        private static bool Prefix(模块_受伤 __instance, ref 世界类.伤害数据包 sjb)
        {
            if (Plugin.Role == 2 && sjb.攻击者 != null && sjb.攻击者.GetComponent<ThemeEnemyActor>() != null) return false;
            var actor = __instance.GetComponent<ThemeEnemyActor>();
            if (actor == null || actor.Definition == null || actor.Definition.FrontalDamageMultiplier >= 1 || sjb.攻击者 == null) return true;
            Vector2 direction = ThemeDamageDirection.AreaOrigin.HasValue ?
                ThemeDamageDirection.AreaOrigin.Value - (Vector2)actor.transform.position :
                ThemeDamageDirection.Incoming ?? ((Vector2)sjb.攻击者.transform.position - (Vector2)actor.transform.position);
            if (Vector2.Dot(direction, actor.Facing) <= 0) return true;
            int origin = OtherworldDamage.Space(sjb);
            sjb = new 世界类.伤害数据包 {
                攻击者 = sjb.攻击者, 攻击目标 = sjb.攻击目标, 攻击坐标 = sjb.攻击坐标, 是否制导 = sjb.是否制导,
                所属阵营 = sjb.所属阵营, 攻击伤害 = sjb.攻击伤害 * actor.Definition.FrontalDamageMultiplier,
                暴击倍率 = sjb.暴击倍率, 造成状态 = sjb.造成状态, 是否产生仇恨 = sjb.是否产生仇恨,
                是否暴击 = sjb.是否暴击, 穿透数量 = sjb.穿透数量, 穿透伤害加成 = sjb.穿透伤害加成,
                是否击退 = sjb.是否击退, 是否爆炸 = sjb.是否爆炸, 击退类型 = sjb.击退类型, 击退参数 = sjb.击退参数
            };
            OtherworldDamage.Remember(sjb, origin);
            return true;
        }
    }

    internal static class ThemeDamageDirection
    {
        [ThreadStatic] internal static Vector2? Incoming, AreaOrigin;
        internal struct Scope { internal Vector2? Incoming, AreaOrigin; }
        internal static Scope Capture() => new Scope { Incoming = Incoming, AreaOrigin = AreaOrigin };
        internal static void Restore(Scope state) { Incoming = state.Incoming; AreaOrigin = state.AreaOrigin; }
    }

    [HarmonyPatch(typeof(模块_伤害精灵), "OnTriggerEnter2D")]
    internal static class ThemeProjectileDirection
    {
        private static void Prefix(模块_伤害精灵 __instance, out ThemeDamageDirection.Scope __state)
        {
            __state = ThemeDamageDirection.Capture();
            ThemeDamageDirection.Incoming = -(Vector2)__instance.transform.right;
            ThemeDamageDirection.AreaOrigin = null;
        }
        private static System.Exception Finalizer(System.Exception __exception, ThemeDamageDirection.Scope __state)
        { ThemeDamageDirection.Restore(__state); return __exception; }
    }

    [HarmonyPatch(typeof(模块_范围伤害精灵), "OnTriggerStay2D")]
    internal static class ThemeAreaDirection
    {
        private static void Prefix(模块_范围伤害精灵 __instance, out ThemeDamageDirection.Scope __state)
        {
            __state = ThemeDamageDirection.Capture();
            ThemeDamageDirection.Incoming = null;
            ThemeDamageDirection.AreaOrigin = __instance.transform.position;
        }
        private static System.Exception Finalizer(System.Exception __exception, ThemeDamageDirection.Scope __state)
        { ThemeDamageDirection.Restore(__state); return __exception; }
    }

    [HarmonyPatch(typeof(模块_受伤), "受到伤害", new[] { typeof(世界类.伤害数据包) })]
    internal static class ThemeBossDamageRage
    {
        private static void Prefix(模块_受伤 __instance, out float __state)
        { __state = __instance.GetComponent<模块_基本属性>()?.当前生命值 ?? 0; }
        private static void Postfix(模块_受伤 __instance, float __state)
        {
            var actor = __instance.GetComponent<ThemeEnemyActor>();
            if (actor == null || actor.Stats.当前生命值 >= __state) return;
            var rage = __instance.GetComponent<模块_BOSS生命和狂暴>();
            if (rage != null) rage.是否触发累积生气 = true;
        }
    }
}
