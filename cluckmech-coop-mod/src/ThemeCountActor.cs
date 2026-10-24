using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CluckmechCoop
{
    public sealed class ThemeCountActor : MonoBehaviour
    {
        internal ThemeEnemyActor Actor;
        internal bool PhaseTwo;
        internal bool Alive => Actor != null && Actor.Alive;
        private CastleCountParameters values;
        private ThemeBattleScene scene;
        private readonly List<GameObject> supplied = new List<GameObject>();
        private float nextCharge, nextVine, nextMelee, nextSupply;
        private bool transitionPending, ended;
        private int previousPlayer = -1;
        private GameObject baseUnit;

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            Actor = GetComponent<ThemeEnemyActor>(); values = ThemeCastleData.CountParameters;
            scene = ThemeBattleScene.Current;
            baseUnit = Plugin.Spawner?.GetComponent<yy引用实例数据库>()?.基地车;
            if (Plugin.Role == 2 || scene == null || !Alive || Actor.SuppressDefaultCombat) yield break;
            Actor.Hold(); Actor.Emit("entry", transform.position, values.EntrySeconds);
            yield return Actor.Pause(values.EntrySeconds);
            nextCharge = Time.time; nextVine = Time.time + values.VineInitialDelaySeconds;
            while (Alive)
            {
                if (!Actor.CanAct) { Actor.Hold(); yield return null; continue; }
                if (transitionPending)
                {
                    transitionPending = false; Actor.Hold();
                    Actor.Emit("count-toast", transform.position, values.PhaseTransitionSeconds);
                    yield return Actor.Pause(values.PhaseTransitionSeconds); continue;
                }
                if (Time.time >= nextCharge)
                {
                    var player = ChargePlayer();
                    if (player != null)
                    {
                        nextCharge = Time.time + values.ChargeCooldownSeconds;
                        yield return Charge(player, PhaseTwo ? values.PhaseTwoCharges : values.PhaseOneCharges);
                        continue;
                    }
                }
                var vine = VineTarget();
                if (Time.time >= nextVine && vine != null)
                {
                    nextVine = Time.time + values.VineCooldownSeconds;
                    yield return Vine(vine.transform.position); continue;
                }
                var melee = Actor.SelectTarget(Actor.Attack.当前攻击范围);
                if (melee != null)
                {
                    Actor.Hold(); Actor.Face((Vector2)melee.transform.position - (Vector2)transform.position);
                    if (Time.time >= nextMelee && Actor.Attack.当前攻击速度 > 0)
                    {
                        nextMelee = Time.time + 1 / Actor.Attack.当前攻击速度;
                        Actor.Emit("attack", transform.position, Actor.Definition.AttackWindupSeconds, 0, melee.transform.position);
                        yield return Actor.Pause(Actor.Definition.AttackWindupSeconds);
                        if (Alive && melee != null && Vector2.Distance(transform.position, melee.transform.position) <= Actor.Attack.当前攻击范围 + 0.3f)
                        {
                            ThemeCombat.Hit(Actor, melee.GetComponent<模块_基本属性>(), Actor.Definition.FinalDamage, melee.transform.position);
                            Actor.Emit("count-stab", transform.position, 0.2f, 0.5f, melee.transform.position);
                        }
                    }
                    else yield return null;
                }
                else
                {
                    if (baseUnit != null) Actor.MoveTo(baseUnit.transform.position);
                    if (Actor.Action != "walk") Actor.Emit("walk", transform.position);
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }

        private void Update()
        {
            if (Plugin.Role == 2 || !Alive || ended || values == null || Actor.SuppressDefaultCombat) return;
            if (!PhaseTwo && Actor.Stats.当前生命值 <= Actor.Stats.当前生命值上限 * values.PhaseTwoHealthFraction)
            {
                PhaseTwo = true; transitionPending = true;
                SpawnSupply(true); nextSupply = Time.time + values.WineBatchIntervalSeconds;
            }
            else if (PhaseTwo && Time.time >= nextSupply)
            {
                nextSupply += values.WineBatchIntervalSeconds; SpawnSupply(false);
            }
        }

        private int EventChoice => ModuleSettings.Progress.Branch.EventChoice;

        internal void SpawnSupply(bool first)
        {
            foreach (var group in ThemeCastleData.CountSupply(EventChoice, first))
            {
                var generated = ThemeBattleWaves.Spawn(group.UnitId, group.Count, new[] { group.EntranceId });
                int station = 0;
                foreach (var unit in generated)
                {
                    supplied.Add(unit);
                    var actor = unit.GetComponent<ThemeEnemyActor>();
                    if (actor != null && actor.UnitId == ThemeCastleData.BossWineBat)
                    { actor.SupplyBoss = this; actor.WineStation = station++; }
                }
            }
            Actor.Emit("count-supply", transform.position, 0.8f);
        }

        private GameObject ChargePlayer()
        {
            GameObject chosen = null; int chosenRole = int.MaxValue;
            foreach (var unit in 世界事件系统.场上所有单位)
            {
                if (!ThemeCombat.Friendly(unit) || !unit.单位定位列表.Contains(模块_基本属性.单位定位枚举.玩家)) continue;
                int role = (int)unit.所属玩家;
                if (role > previousPlayer && role < chosenRole) { chosen = unit.gameObject; chosenRole = role; }
            }
            if (chosen == null)
                foreach (var unit in 世界事件系统.场上所有单位)
                {
                    if (!ThemeCombat.Friendly(unit) || !unit.单位定位列表.Contains(模块_基本属性.单位定位枚举.玩家)) continue;
                    int role = (int)unit.所属玩家;
                    if (role < chosenRole) { chosen = unit.gameObject; chosenRole = role; }
                }
            if (chosen != null) previousPlayer = chosenRole;
            return chosen ?? baseUnit;
        }

        private GameObject VineTarget()
        {
            if (baseUnit != null && Vector2.Distance(transform.position, baseUnit.transform.position) <= values.VineLength) return baseUnit;
            GameObject selected = null; float nearest = values.VineLength * values.VineLength;
            foreach (var unit in 世界事件系统.场上所有单位)
            {
                if (!ThemeCombat.Friendly(unit) || !unit.单位定位列表.Contains(模块_基本属性.单位定位枚举.玩家)) continue;
                float distance = ((Vector2)unit.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (distance <= nearest) { selected = unit.gameObject; nearest = distance; }
            }
            return selected;
        }

        private IEnumerator Charge(GameObject player, int count)
        {
            Actor.Hold();
            for (int segment = 0; Alive && segment < count; segment++)
            {
                Vector2 start = transform.position;
                Vector2 point = player == null ? (Vector2)baseUnit.transform.position : (Vector2)player.transform.position;
                Vector2 direction = (point - start).normalized;
                if (direction.sqrMagnitude < 0.5f) direction = Actor.Facing;
                Actor.Face(direction);
                bool pillar;
                Vector2 end = StopPoint(start, direction, values.ChargeWidth * 0.5f, float.PositiveInfinity, out pillar);
                Actor.Emit("charge-warning", start, values.ChargeWarningSeconds, values.ChargeWidth, end, NetworkObjects.Id(player));
                yield return Actor.Pause(values.ChargeWarningSeconds);
                if (!Alive) yield break;
                Actor.Emit("charge", start, Vector2.Distance(start, end) / values.ChargeSpeed, values.ChargeWidth, end);
                var hit = new HashSet<模块_基本属性>();
                var body = GetComponent<Rigidbody2D>();
                var oldType = body == null ? RigidbodyType2D.Dynamic : body.bodyType;
                if (body != null) body.bodyType = RigidbodyType2D.Kinematic;
                try
                {
                    Vector2 previous = transform.position;
                    while (Alive && Vector2.Distance(transform.position, end) > 0.03f)
                    {
                        Actor.MotionPause(Actor.Controlled);
                        if (!Actor.Controlled)
                        {
                            Vector2 next = Vector2.MoveTowards(transform.position, end, values.ChargeSpeed * Time.deltaTime);
                            HitCharge(previous, next, hit);
                            // The path, including the first pillar stop, is the path shown in the warning.
                            transform.position = new Vector3(next.x, next.y, transform.position.z);
                            previous = next;
                        }
                        yield return null;
                    }
                }
                finally { if (body != null) { body.bodyType = oldType; body.velocity = Vector2.zero; } }
                if (!Alive) yield break;
                Actor.MotionPause(false);
                Actor.Hold();
                if (pillar)
                {
                    float seconds = ThemeCastleData.CountPillarStunSeconds(EventChoice);
                    Actor.Emit("pillar-stun", end, seconds, values.ChargeWidth);
                    yield return Actor.Pause(seconds); yield break;
                }
                float delay = segment + 1 < count ? values.BetweenChargesSeconds : values.BoundaryPauseSeconds;
                Actor.Emit("charge-recover", end, delay);
                yield return Actor.Pause(delay);
            }
        }

        private void HitCharge(Vector2 start, Vector2 end, HashSet<模块_基本属性> hit)
        {
            foreach (var collider in Physics2D.OverlapCapsuleAll((start + end) * 0.5f,
                new Vector2(values.ChargeWidth, Vector2.Distance(start, end) + values.ChargeWidth),
                CapsuleDirection2D.Vertical, Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg - 90))
            {
                var health = collider.GetComponentInParent<模块_基本属性>();
                if (!ThemeCombat.Friendly(health) || !hit.Add(health)) continue;
                bool player = health.单位定位列表.Contains(模块_基本属性.单位定位枚举.玩家);
                bool isBase = health.单位定位列表.Contains(模块_基本属性.单位定位枚举.基地);
                if (player || isBase) ThemeCombat.Hit(Actor, health, values.ChargeFinalDamage, end);
                else
                {
                    health.当前生命值 = 0;
                    health.GetComponent<模块_死亡>()?.死亡实现();
                }
            }
        }

        private IEnumerator Vine(Vector2 point)
        {
            Actor.Hold(); Vector2 start = transform.position;
            Vector2 direction = (point - start).normalized;
            Actor.Face(direction);
            var ends = new Vector2[values.VineCount];
            for (int i = 0; i < ends.Length; i++)
            {
                float angle = (i - (ends.Length - 1) * 0.5f) * values.VineAngleDegrees;
                Vector2 ray = Quaternion.Euler(0, 0, angle) * direction;
                ends[i] = StopPoint(start, ray, values.VineWidth * 0.5f, values.VineLength, out _);
                Actor.Emit("vine-warning", start, values.VineWarningSeconds, values.VineWidth, ends[i]);
            }
            yield return Actor.Pause(values.VineWarningSeconds);
            if (!Alive) yield break;
            var hit = new HashSet<模块_基本属性>();
            foreach (var end in ends)
            {
                Actor.Emit("vine", start, values.VineActionSeconds - values.VineWarningSeconds, values.VineWidth, end);
                foreach (var collider in Physics2D.OverlapCapsuleAll((start + end) * 0.5f,
                    new Vector2(values.VineWidth, Vector2.Distance(start, end) + values.VineWidth),
                    CapsuleDirection2D.Vertical, Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg - 90))
                {
                    var target = collider.GetComponentInParent<模块_基本属性>();
                    if (ThemeCombat.Friendly(target) && hit.Add(target)) ThemeCombat.Hit(Actor, target, values.VineFinalDamage, end);
                }
            }
            yield return Actor.Pause(values.VineActionSeconds - values.VineWarningSeconds);
        }

        private Vector2 StopPoint(Vector2 start, Vector2 direction, float halfWidth, float maximum, out bool pillar)
        {
            var bounds = scene.Definition.Floors[0];
            Vector2 low = scene.World(new Vector2(bounds.MinX, bounds.MinY)) + Vector2.one * halfWidth;
            Vector2 high = scene.World(new Vector2(bounds.MaxX, bounds.MaxY)) - Vector2.one * halfWidth;
            float distance = maximum;
            if (direction.x > 0.0001f) distance = Mathf.Min(distance, (high.x - start.x) / direction.x);
            if (direction.x < -0.0001f) distance = Mathf.Min(distance, (low.x - start.x) / direction.x);
            if (direction.y > 0.0001f) distance = Mathf.Min(distance, (high.y - start.y) / direction.y);
            if (direction.y < -0.0001f) distance = Mathf.Min(distance, (low.y - start.y) / direction.y);
            pillar = false;
            foreach (var column in scene.Pillars)
            {
                if (column.Broken) continue;
                Vector2 delta = column.Center - start;
                float along = Vector2.Dot(delta, direction);
                float radius = column.Radius + halfWidth;
                float acrossSquared = delta.sqrMagnitude - along * along;
                if (along <= 0 || acrossSquared > radius * radius) continue;
                float entry = Mathf.Max(0, along - Mathf.Sqrt(radius * radius - acrossSquared));
                if (entry < distance) { distance = entry; pillar = true; }
            }
            return start + direction * Mathf.Max(0, distance);
        }

        public void 死亡触发()
        {
            if (ended) return;
            ended = true; StopAllCoroutines();
            if (Plugin.Role == 2) return;
            foreach (var unit in supplied)
            {
                if (unit == null) continue;
                var health = unit.GetComponent<模块_基本属性>();
                if (health == null || health.当前生命值 <= 0) continue;
                health.当前生命值 = 0; unit.GetComponent<模块_死亡>()?.死亡实现();
            }
            supplied.Clear();
        }
    }
}
