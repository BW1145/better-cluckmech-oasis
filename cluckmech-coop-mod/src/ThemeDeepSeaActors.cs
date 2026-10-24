using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class ThemeDeepSeaCombat
    {
        internal static 模块_基本属性 BaseUnit() => Plugin.Spawner?.GetComponent<yy引用实例数据库>()?.基地车?.GetComponent<模块_基本属性>();

        internal static bool IsPlayer(模块_基本属性 health) => ThemeCombat.Friendly(health) &&
            health.GetComponent<模块_玩家_移动>() != null && health.GetComponent<模块_玩家_死亡>()?.是否玩家死亡 != true;

        internal static bool DeployedTower(模块_基本属性 health) => ThemeCombat.Friendly(health) &&
            health.GetComponent<模块_玩家_移动>() == null && health.单位定位列表 != null &&
            !health.单位定位列表.Contains(模块_基本属性.单位定位枚举.基地) &&
            !health.单位定位列表.Contains(模块_基本属性.单位定位枚举.召唤物) &&
            health.单位类型 != 模块_基本属性.单位类型枚举.飞行单位 &&
            (health.单位类型 == 模块_基本属性.单位类型枚举.建筑 || health.单位类型 == 模块_基本属性.单位类型枚举.陷阱 ||
                health.启用建筑体积 || health.GetComponent<StratagemUnit>() != null);

        internal static List<模块_基本属性> ClosestTowers(Vector2 origin, int count, float radius = float.PositiveInfinity)
        {
            var result = new List<模块_基本属性>(count);
            foreach (var target in 世界事件系统.场上所有单位)
            {
                if (!DeployedTower(target) || target.当前是否无敌 ||
                    ((Vector2)target.transform.position - origin).sqrMagnitude > radius * radius) continue;
                int index = 0;
                float distance = ((Vector2)target.transform.position - origin).sqrMagnitude;
                while (index < result.Count && ((Vector2)result[index].transform.position - origin).sqrMagnitude <= distance) index++;
                if (index >= count) continue;
                result.Insert(index, target);
                if (result.Count > count) result.RemoveAt(count);
            }
            return result;
        }

        internal static void Protect(ThemeEnemyActor actor, bool value)
        {
            var health = actor.Stats != null ? actor.Stats : actor.GetComponent<模块_基本属性>();
            health.是否无敌 = value; health.运算无敌();
        }

        internal static 模块_BOSS生命和狂暴 ScaleNativeRage(ThemeEnemyActor actor, bool whiteVisitor)
        {
            var rage = actor.GetComponent<模块_BOSS生命和狂暴>();
            if (rage == null) return null;
            var clock = actor.GetComponent<ThemeNativeRageClock>() ?? actor.gameObject.AddComponent<ThemeNativeRageClock>();
            if (clock.Configured) return rage;
            clock.Configured = true;
            int choice = ModuleSettings.Progress?.Branch?.EventChoice ?? -1;
            float duration = rage.BOSS生气时间 * ThemeDeepSeaData.RageDurationMultiplier(whiteVisitor, choice);
            rage.BOSS生气时间 = rage.记录BOSS生气时间 = duration;
            rage.是否触发累积生气 = true;
            actor.Emit("native-rage", new Vector2(duration, duration));
            return rage;
        }

        internal static void ReceiveNativeRage(ThemeEnemyActor actor, ThemeActorMessage message)
        {
            if (Plugin.Role != 2) return;
            var rage = actor.GetComponent<模块_BOSS生命和狂暴>();
            if (rage == null) return;
            rage.记录BOSS生气时间 = Mathf.Max(.01f, message.X);
            rage.BOSS生气时间 = Mathf.Max(0, message.Y);
            rage.是否触发累积生气 = true;
        }

        internal static void Band(ThemeEnemyActor source, Vector2 from, Vector2 to, float width, float finalDamage, bool blockedByWalls = true)
        {
            Vector2 edge = to - from; float length = edge.magnitude;
            Vector2 forward = length > .001f ? edge / length : Vector2.down;
            float radius = width * .5f;
            for (int i = 世界事件系统.场上所有单位.Count - 1; i >= 0; i--)
            {
                if (i >= 世界事件系统.场上所有单位.Count) continue;
                var target = 世界事件系统.场上所有单位[i];
                if (!ThemeCombat.Friendly(target)) continue;
                Vector2 center = target.transform.position;
                Vector2 closest = from + forward * Mathf.Clamp(Vector2.Dot(center - from, forward), 0, length);
                if (ThemeCombat.ShapeDistance(target, closest) > radius * radius ||
                    blockedByWalls && ThemeBattleScene.Current?.BlocksShot(from, closest) == true) continue;
                ThemeCombat.Hit(source, target, finalDamage, closest);
            }
        }

        internal static void Cone(ThemeEnemyActor source, Vector2 origin, Vector2 direction, float range,
            float angleDegrees, float finalDamage, bool blockedByWalls = true)
        {
            float dot = Mathf.Cos(angleDegrees * .5f * Mathf.Deg2Rad);
            for (int i = 世界事件系统.场上所有单位.Count - 1; i >= 0; i--)
            {
                if (i >= 世界事件系统.场上所有单位.Count) continue;
                var target = 世界事件系统.场上所有单位[i];
                if (!ThemeCombat.Friendly(target) || ThemeCombat.ShapeDistance(target, origin) > range * range) continue;
                Vector2 offset = (Vector2)target.transform.position - origin;
                if (offset.sqrMagnitude > .01f && Vector2.Dot(offset.normalized, direction.normalized) < dot ||
                    blockedByWalls && ThemeBattleScene.Current?.BlocksShot(origin, target.transform.position) == true) continue;
                ThemeCombat.Hit(source, target, finalDamage, target.transform.position);
            }
        }
    }

    public sealed class ThemeNativeRageClock : MonoBehaviour { internal bool Configured; }

    // Each recipient owns a single native bonus; coverage from any number of lilies selects its maximum.
    public sealed class ThemeLilyAura : MonoBehaviour
    {
        internal static readonly List<ThemeLilyAura> Sources = new List<ThemeLilyAura>();
        private ThemeEnemyActor actor;
        private void OnEnable() { Sources.Add(this); }
        private void OnDisable() { Sources.Remove(this); }
        private void Start() { actor = GetComponent<ThemeEnemyActor>(); }
        internal bool Covers(ThemeEnemyActor unit)
        {
            if (actor == null || !actor.Alive || !actor.VisibleToUnits || !unit.VisibleToUnits) return false;
            float radius = actor.UnitId == ThemeDeepSeaData.CrownLily ? ThemeDeepSeaData.CrownLilyAuraRadius : ThemeDeepSeaData.LilyAuraRadius;
            return ((Vector2)unit.transform.position - (Vector2)transform.position).sqrMagnitude <= radius * radius;
        }
    }

    public sealed class ThemeLilyProtection : MonoBehaviour
    {
        private const string Bonus = "theme.lily-protection";
        private ThemeEnemyActor actor;
        private 模块_加成管理器 bonuses;
        private bool applied;
        private float next;
        private void Awake() { actor = GetComponent<ThemeEnemyActor>(); bonuses = GetComponent<模块_加成管理器>(); }
        private void Update() { if (Time.time >= next) { next = Time.time + .25f; Refresh(); } }
        internal void Refresh()
        {
            bool covered = false;
            if (actor != null && actor.Alive && actor.VisibleToUnits)
                foreach (var source in ThemeLilyAura.Sources)
                {
                    if (!source.Covers(actor)) continue;
                    covered = true; break;
                }
            if (covered == applied || bonuses == null) return;
            applied = covered;
            if (covered) bonuses.增加加成(Bonus, new List<模块_加成管理器.加成类> {
                new 模块_加成管理器.加成类 { 加成属性 = 模块_加成管理器.加成属性枚举.抗性, 加成值 = ThemeDeepSeaData.LilyResistance }
            });
            else bonuses.移除加成(Bonus);
            actor.Stats.运算抗性();
        }
        private void OnDisable()
        {
            if (!applied || bonuses == null) return;
            bonuses.移除加成(Bonus); applied = false; actor.Stats.运算抗性();
        }
    }

    public sealed class ThemeValveAnemoneActor : MonoBehaviour
    {
        private ThemeEnemyActor actor;
        private bool interrupted;
        private void Awake() { actor = GetComponent<ThemeEnemyActor>(); actor.SuppressDefaultCombat = true; }
        private IEnumerator Start()
        {
            yield return null; yield return null;
            if (Plugin.Role == 2) yield break;
            var values = ThemeDeepSeaData.ValveAnemoneParameters;
            float nextVolley = Time.time + values.CooldownSeconds, nextMelee = 0;
            while (actor.Alive)
            {
                if (!actor.CanAct) { actor.Hold(); yield return null; continue; }
                var target = actor.SelectTarget(values.Range);
                if (target == null)
                {
                    var baseUnit = ThemeDeepSeaCombat.BaseUnit();
                    if (baseUnit != null) actor.MoveTo(baseUnit.transform.position);
                    if (actor.Action != "walk") actor.Emit("walk", transform.position);
                    yield return new WaitForSeconds(.25f); continue;
                }
                actor.Hold();
                if (Time.time >= nextVolley)
                {
                    nextVolley = Time.time + values.CooldownSeconds;
                    Vector2 direction = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
                    actor.Face(direction);
                    Vector2 end = (Vector2)transform.position + direction * values.Range;
                    actor.Emit("valve-warning", transform.position, values.WindupSeconds, .7f, end);
                    yield return Interruptible(values.WindupSeconds);
                    if (interrupted) { actor.Emit("idle", transform.position); continue; }
                    for (int shot = 0; actor.Alive && shot < values.ProjectileCount; shot++)
                    {
                        if (!actor.CanAct) break;
                        actor.Emit("valve-shot", transform.position, .2f, .7f, end);
                        actor.NativeFireScaled(end, values.FinalProjectileDamage / actor.Definition.FinalDamage);
                        yield return Interruptible(values.ProjectileIntervalSeconds);
                        if (interrupted) break;
                    }
                    actor.Emit("idle", transform.position);
                }
                else if (Vector2.Distance(transform.position, target.transform.position) <= actor.Attack.当前攻击范围 && Time.time >= nextMelee)
                {
                    nextMelee = Time.time + actor.Definition.AttackIntervalSeconds;
                    Vector2 point = target.transform.position;
                    actor.Face(point - (Vector2)transform.position);
                    actor.Emit("attack", transform.position, .35f, 0, point, NetworkObjects.Id(target));
                    yield return Interruptible(.35f);
                    if (!interrupted && target != null && ThemeCombat.Friendly(target.GetComponent<模块_基本属性>()) &&
                        Vector2.Distance(transform.position, target.transform.position) <= actor.Attack.当前攻击范围 + .2f)
                        ThemeCombat.Hit(actor, target.GetComponent<模块_基本属性>(), actor.Definition.FinalDamage, point);
                }
                else if (Vector2.Distance(transform.position, target.transform.position) > actor.Attack.当前攻击范围)
                {
                    actor.MoveTo(target.transform.position);
                    if (actor.Action != "walk") actor.Emit("walk", transform.position);
                }
                yield return null;
            }
        }
        private IEnumerator Interruptible(float seconds)
        {
            interrupted = false; float end = Time.time + seconds;
            while (actor.Alive && Time.time < end)
            {
                if (!actor.CanAct) { interrupted = true; yield break; }
                yield return null;
            }
            if (!actor.Alive) interrupted = true;
        }
        public void 死亡触发() { StopAllCoroutines(); }
    }
}
