using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CluckmechCoop
{
    public sealed class ThemeOctopusActor : MonoBehaviour
    {
        internal ThemeEnemyActor Actor { get; private set; }
        private readonly List<ThemeOctopusArm> arms = new List<ThemeOctopusArm>(8);
        private DeepSeaOctopusParameters values;
        private 模块_BOSS生命和狂暴 rage;
        private bool exposed, ended;
        private void Awake()
        {
            Actor = GetComponent<ThemeEnemyActor>(); Actor.SuppressDefaultCombat = true;
            ThemeDeepSeaCombat.Protect(Actor, true);
        }
        private void OnEnable() { Actor.ActionChanged += Changed; }
        private void OnDisable() { Actor.ActionChanged -= Changed; }
        private void Changed(ThemeActorMessage message)
        {
            if (Plugin.Role == 2 && message.Action == "octopus-core")
            { exposed = true; ThemeDeepSeaCombat.Protect(Actor, false); }
        }
        private IEnumerator Start()
        {
            yield return null; yield return null;
            Actor.Hold();
            if (Plugin.Role == 2) yield break;
            values = ThemeDeepSeaData.OctopusParameters;
            rage = ThemeDeepSeaCombat.ScaleNativeRage(Actor, false);
            int choice = ModuleSettings.Progress?.Branch?.EventChoice ?? -1;
            float started = Time.time;
            foreach (var definition in ThemeDeepSeaData.OctopusArms)
            {
                Vector2 point = ThemeBattleScene.Current.World(definition.Position);
                foreach (var unit in ThemeBattleWaves.SpawnAt(definition.UnitId, 1, point))
                {
                    var arm = unit.GetComponent<ThemeOctopusArm>();
                    arm.Owner = this; arm.Definition = definition;
                    arm.FirstAttackAt = started + ThemeDeepSeaData.OctopusArmFirstDelay(definition, choice);
                    arms.Add(arm);
                }
            }
            Actor.Emit("octopus-arms", transform.position, 2);
            float clearedAt = -1, nextSiege = 0;
            while (Actor.Alive && !exposed)
            {
                if (AllArmsDestroyed())
                {
                    if (clearedAt < 0)
                    {
                        clearedAt = Time.time;
                        Actor.Emit("octopus-expose", transform.position, values.CoreTransitionSeconds);
                    }
                    if (Time.time - clearedAt >= values.CoreTransitionSeconds)
                    {
                        exposed = true; ThemeDeepSeaCombat.Protect(Actor, false);
                        Actor.Emit("octopus-core", transform.position, .5f);
                        break;
                    }
                }
                if (rage != null && rage.是否已生气 && Time.time >= nextSiege && Actor.CanAct)
                {
                    nextSiege = Time.time + values.SiegeIntervalSeconds;
                    yield return Siege();
                }
                yield return null;
            }
            if (Actor.Alive) yield return Core();
        }
        private bool AllArmsDestroyed()
        {
            if (arms.Count != ThemeDeepSeaData.OctopusArms.Length) return false;
            foreach (var arm in arms) if (arm != null && arm.GetComponent<ThemeEnemyActor>().Alive) return false;
            return true;
        }
        private IEnumerator Core()
        {
            float nextBomb = Time.time + values.BombInitialDelaySeconds;
            float nextBreath = Time.time + values.BreathInitialDelaySeconds;
            float nextBite = 0, nextSiege = 0;
            while (Actor.Alive)
            {
                Actor.Hold();
                if (!Actor.CanAct) { yield return null; continue; }
                if (rage != null && rage.是否已生气)
                {
                    if (Time.time >= nextSiege)
                    {
                        nextSiege = Time.time + values.SiegeIntervalSeconds;
                        yield return Siege();
                    }
                    else yield return null;
                    continue;
                }
                if (Time.time >= nextBomb)
                {
                    nextBomb = Time.time + values.BombIntervalSeconds;
                    yield return Bomb();
                }
                else if (Time.time >= nextBreath)
                {
                    nextBreath = Time.time + values.BreathIntervalSeconds;
                    yield return Breath();
                }
                else if (Time.time >= nextBite)
                {
                    var target = Actor.SelectTarget(values.BiteRange);
                    if (target == null) { yield return new WaitForSeconds(.1f); continue; }
                    nextBite = Time.time + values.BiteIntervalSeconds;
                    Vector2 point = target.transform.position;
                    Actor.Face(point - (Vector2)transform.position);
                    Actor.Emit("octopus-bite-warning", transform.position, values.BiteWarningSeconds, values.BiteRadius, point);
                    yield return Actor.Pause(values.BiteWarningSeconds);
                    if (!Actor.Alive) yield break;
                    Actor.Emit("octopus-bite", point, .4f, values.BiteRadius);
                    ThemeCombat.Area(Actor, point, values.BiteRadius, values.BiteFinalDamage);
                }
                else yield return null;
            }
        }
        private IEnumerator Bomb()
        {
            var baseUnit = ThemeDeepSeaCombat.BaseUnit();
            if (baseUnit == null) yield break;
            var towers = ThemeDeepSeaCombat.ClosestTowers(baseUnit.transform.position, values.BombClosestTowerCount);
            if (towers.Count == 0) yield break;
            var points = new List<Vector2>(towers.Count);
            foreach (var tower in towers)
            {
                Vector2 point = tower.transform.position; points.Add(point);
                Actor.Emit("octopus-bomb-warning", transform.position, values.BombWarningSeconds, values.BombRadius, point, NetworkObjects.Id(tower.gameObject));
            }
            yield return Actor.Pause(values.BombWarningSeconds);
            if (!Actor.Alive) yield break;
            foreach (var point in points)
            {
                Actor.Emit("octopus-bomb", point, .5f, values.BombRadius);
                ThemeCombat.Area(Actor, point, values.BombRadius, values.BombFinalDamage);
            }
        }
        private IEnumerator Breath()
        {
            var target = Actor.SelectTarget(values.BreathRange);
            if (target == null) yield break;
            Vector2 origin = (Vector2)transform.position + Vector2.down * .5f;
            Vector2 direction = ((Vector2)target.transform.position - origin).normalized;
            Vector2 end = origin + direction * values.BreathRange;
            Actor.Face(direction);
            Actor.Emit("octopus-breath-warning", origin, values.BreathWindupSeconds, values.BreathAngleDegrees * .5f, end);
            yield return Actor.Pause(values.BreathWindupSeconds);
            if (!Actor.Alive) yield break;
            Actor.Emit("octopus-breath", origin, values.BreathDurationSeconds, values.BreathAngleDegrees * .5f, end);
            float previous = 0;
            foreach (float tick in values.BreathTickSeconds)
            {
                yield return Actor.Pause(tick - previous); previous = tick;
                if (!Actor.Alive) yield break;
                ThemeDeepSeaCombat.Cone(Actor, origin, direction, values.BreathRange, values.BreathAngleDegrees, values.BreathFinalDamagePerTick);
            }
            yield return Actor.Pause(values.BreathDurationSeconds - previous);
        }
        private IEnumerator Siege()
        {
            var baseUnit = ThemeDeepSeaCombat.BaseUnit();
            if (!ThemeCombat.Friendly(baseUnit)) yield break;
            Vector2 point = baseUnit.transform.position;
            Actor.Emit("octopus-siege-warning", (Vector2)transform.position + Vector2.down * .5f,
                values.SiegeWarningSeconds, 1.8f, point, NetworkObjects.Id(baseUnit.gameObject));
            yield return Actor.Pause(values.SiegeWarningSeconds);
            if (!Actor.Alive || !ThemeCombat.Friendly(baseUnit)) yield break;
            point = baseUnit.transform.position;
            Actor.Emit("octopus-siege", point, .4f, 1.8f);
            ThemeCombat.Hit(Actor, baseUnit, values.SiegeFinalDamage, point);
        }
        public void 死亡触发()
        {
            if (ended) return;
            ended = true; StopAllCoroutines();
            foreach (var arm in arms) if (arm != null) arm.ReleaseTarget();
        }
    }

    public sealed class ThemeOctopusArm : MonoBehaviour
    {
        internal ThemeOctopusActor Owner;
        internal DeepSeaArmDefinition Definition;
        internal float FirstAttackAt;
        private ThemeEnemyActor actor;
        private ThemeArmBinding binding;
        private bool ended;
        private float actionClock;
        private void Awake() { actor = GetComponent<ThemeEnemyActor>(); actor.SuppressDefaultCombat = true; }
        private void OnEnable() { actor.ActionChanged += Changed; }
        private void OnDisable() { actor.ActionChanged -= Changed; ReleaseTarget(); }
        private void Update() { if (actor.CanAct) actionClock += Time.deltaTime; }
        private IEnumerator Start()
        {
            yield return null; yield return null;
            actor.Hold();
            if (Definition == null)
            {
                float closest = float.PositiveInfinity;
                foreach (var arm in ThemeDeepSeaData.OctopusArms)
                {
                    if (arm.UnitId != actor.UnitId) continue;
                    float distance = (ThemeBattleScene.Current.World(arm.Position) - (Vector2)transform.position).sqrMagnitude;
                    if (distance < closest) { closest = distance; Definition = arm; }
                }
            }
            if (Plugin.Role == 2) yield break;
            int choice = ModuleSettings.Progress?.Branch?.EventChoice ?? -1;
            var values = ThemeDeepSeaData.OctopusParameters;
            float first = actionClock + Mathf.Max(0, FirstAttackAt - Time.time);
            while (actor.Alive && actionClock < first) yield return null;
            while (actor.Alive)
            {
                while (actor.Alive && !actor.CanAct) { actor.Hold(); yield return null; }
                if (!actor.Alive) yield break;
                float next = actionClock + ThemeDeepSeaData.OctopusArmPeriod(Definition.Kind, choice);
                if (Definition.Kind == DeepSeaArmKind.Bind)
                {
                    var target = BindTarget(values);
                    if (target != null) yield return Bind(target, values);
                    else yield return Slam(values);
                }
                else yield return Slam(values);
                while (actor.Alive && actionClock < next) yield return null;
            }
        }
        private 模块_基本属性 BindTarget(DeepSeaOctopusParameters values)
        {
            模块_基本属性 selected = null; float closest = values.BindPlayerRange * values.BindPlayerRange;
            foreach (var target in 世界事件系统.场上所有单位)
            {
                if (!ThemeDeepSeaCombat.IsPlayer(target) || !ThemeArmBinding.CanBind(target, true)) continue;
                float distance = ((Vector2)target.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (distance > closest) continue;
                selected = target; closest = distance;
            }
            if (selected != null) return selected;
            var towers = ThemeDeepSeaCombat.ClosestTowers(transform.position, 世界事件系统.场上所有单位.Count, values.BindTowerRange);
            foreach (var tower in towers) if (ThemeArmBinding.CanBind(tower, false)) return tower;
            return null;
        }
        private IEnumerator Bind(模块_基本属性 target, DeepSeaOctopusParameters values)
        {
            bool player = ThemeDeepSeaCombat.IsPlayer(target);
            Vector2 point = target.transform.position;
            actor.Face(point - (Vector2)transform.position);
            actor.Emit("arm-bind-warning", transform.position, values.BindWarningSeconds, .9f, point, NetworkObjects.Id(target.gameObject));
            yield return actor.Pause(values.BindWarningSeconds);
            if (!actor.Alive || !ThemeCombat.Friendly(target) || !ThemeArmBinding.CanBind(target, player) ||
                player && Vector2.Distance(transform.position, target.transform.position) > values.BindPlayerRange) yield break;
            binding = ThemeArmBinding.Apply(actor, target, values.BindDurationSeconds, player);
            if (binding == null) yield break;
            actor.Emit("bind-lock", transform.position, binding.Remaining, player ? 1 : 0,
                target.transform.position, NetworkObjects.Id(target.gameObject));
            actor.Emit("arm-bind", transform.position, binding.Remaining, .9f, target.transform.position, NetworkObjects.Id(target.gameObject));
            while (actor.Alive && binding != null && binding.Active) yield return null;
            ReleaseTarget();
        }
        private IEnumerator Slam(DeepSeaOctopusParameters values)
        {
            var target = actor.SelectTarget(values.SlamRange);
            if (target == null) yield break;
            Vector2 origin = transform.position;
            Vector2 point = target.transform.position;
            Vector2 direction = (point - origin).normalized;
            // The visible strike is centred on the locked target, limited to the arm's reach.
            float along = Mathf.Clamp(Vector2.Distance(origin, point), values.SlamLength * .5f,
                values.SlamRange - values.SlamLength * .5f);
            Vector2 center = origin + direction * along;
            Vector2 from = center - direction * values.SlamLength * .5f;
            Vector2 to = center + direction * values.SlamLength * .5f;
            actor.Face(direction);
            actor.Emit("arm-slam-warning", from, values.SlamWarningSeconds, values.SlamWidth, to);
            yield return actor.Pause(values.SlamWarningSeconds);
            if (!actor.Alive) yield break;
            actor.Emit("arm-slam", from, .4f, values.SlamWidth, to);
            ThemeDeepSeaCombat.Band(actor, from, to, values.SlamWidth, values.SlamFinalDamage);
        }
        private void Changed(ThemeActorMessage message)
        {
            if (Plugin.Role != 2) return;
            if (message.Action == "bind-release") { ReleaseTarget(); return; }
            if (message.Action == "bind-lock") StartCoroutine(ReceiveBind(message));
        }
        private IEnumerator ReceiveBind(ThemeActorMessage message)
        {
            float until = Time.time + 2;
            while (actor.Alive && Time.time < until)
            {
                if (Mirror.NetworkClient.spawned.TryGetValue(message.Target, out var identity))
                {
                    var target = identity.GetComponent<模块_基本属性>();
                    if (target != null) binding = ThemeArmBinding.Apply(actor, target, message.Duration, message.Width > .5f, true);
                    yield break;
                }
                yield return null;
            }
        }
        internal void ReleaseTarget()
        {
            if (binding == null) return;
            var target = binding.GetComponent<模块_基本属性>();
            binding.Release(); binding = null;
            if (Plugin.Role != 2 && actor != null)
                actor.Emit("bind-release", transform.position, 0, 0, default, target == null ? 0 : NetworkObjects.Id(target.gameObject));
        }
        public void 死亡触发()
        {
            if (ended) return;
            ended = true; StopAllCoroutines(); ReleaseTarget();
            if (Definition != null) ThemeBattleScene.Current?.ReleaseFacility(Definition.Id);
        }
    }
}
