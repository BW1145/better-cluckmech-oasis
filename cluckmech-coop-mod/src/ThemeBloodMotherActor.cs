using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CluckmechCoop
{
    public sealed class ThemeDeepSeaEldritch : MonoBehaviour
    {
        private ThemeBattleScene scene;
        private ThemeBattleWaves waves;
        internal static void Attach(ThemeBattleScene scene)
        {
            if (scene == null || !scene.Eldritch || ThemeBranchRuntime.State.Theme != 2) return;
            var owner = scene.gameObject.AddComponent<ThemeDeepSeaEldritch>();
            owner.scene = scene; owner.waves = ThemeBattleWaves.Current;
            ThemeEnemyActor.Ready += owner.Ready;
            if (scene.Definition.Id == ThemeEldritchData.BloodGallery.Id)
                scene.gameObject.AddComponent<ThemeBloodMotherLevel>().Initialize(scene, owner.waves);
            else if (scene.Definition.Id == ThemeEldritchData.PumpLung.Id)
                scene.gameObject.AddComponent<ThemePumpLung>().Initialize(scene, owner.waves);
        }
        private void Ready(ThemeEnemyActor actor)
        {
            if (actor.UnitId == ThemeEldritchData.BloodMother || actor.UnitId == ThemeEldritchData.BloodSac ||
                actor.UnitId == ThemeEldritchData.WhiteVisitor || actor.UnitId == ThemeEldritchData.WhiteHand)
                actor.SuppressDefaultCombat = true;
            if (actor.UnitId == ThemeEldritchData.BloodMother || actor.UnitId == ThemeEldritchData.BloodSac)
                ThemeDeepSeaCombat.Protect(actor, true);
            if (Plugin.Role == 2 && (actor.UnitId == ThemeEldritchData.BloodMother || actor.UnitId == ThemeEldritchData.BloodSac))
                actor.gameObject.AddComponent<ThemeBloodClientState>().Initialize(actor);
            if (actor.UnitId == ThemeEldritchData.WhiteVisitor)
                actor.gameObject.AddComponent<ThemeWhiteVisitorActor>().Initialize(actor, scene);
        }
        private void OnDestroy() { ThemeEnemyActor.Ready -= Ready; }
    }

    [HarmonyPatch(typeof(ThemeBattleWaves), "Attach")]
    internal static class ThemeDeepSeaEldritchScene
    { private static void Postfix(ThemeBattleScene scene) => ThemeDeepSeaEldritch.Attach(scene); }

    public sealed class ThemeBloodClientState : MonoBehaviour
    {
        private ThemeEnemyActor actor;
        private Coroutine transition;
        private bool movementHeld;
        internal void Initialize(ThemeEnemyActor owner)
        {
            actor = owner; actor.ActionChanged += Observe;
            if (actor.UnitId == ThemeEldritchData.BloodMother)
            { ThemeDeepSeaCombat.Protect(actor, true); SetRage(false); actor.Hold(); }
        }
        private void Observe(ThemeActorMessage message)
        {
            if (message.Action == "blood-dormant")
            {
                ThemeDeepSeaCombat.Protect(actor, true); SetRage(false); actor.Hold();
            }
            else if (message.Action == "blood-awaken")
            {
                ThemeDeepSeaCombat.Protect(actor, true); SetRage(false);
                StartTransition(Wake(message.Duration));
            }
            else if (message.Action == "sac-flight")
            {
                ThemeDeepSeaCombat.Protect(actor, true);
                if (!movementHeld) { movementHeld = true; actor.ExternalMovementHolds++; }
                actor.Hold(); StartTransition(Fly(message));
            }
            else if (message.Action == "sac-land")
            {
                if (transition != null) { StopCoroutine(transition); transition = null; }
                SetPosition(new Vector2(message.X, message.Y));
                actor.Hold(); ThemeDeepSeaCombat.Protect(actor, false);
            }
        }
        private void StartTransition(IEnumerator operation)
        {
            if (transition != null) StopCoroutine(transition);
            transition = StartCoroutine(operation);
        }
        private IEnumerator Wake(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            transition = null;
            if (actor.Alive) { ThemeDeepSeaCombat.Protect(actor, false); SetRage(true); }
        }
        private IEnumerator Fly(ThemeActorMessage message)
        {
            Vector2 start = new Vector2(message.X, message.Y), end = new Vector2(message.EndX, message.EndY);
            float elapsed = 0;
            while (actor.Alive && elapsed < message.Duration)
            {
                elapsed += Time.deltaTime;
                SetPosition(Vector2.Lerp(start, end, Mathf.Clamp01(elapsed / Mathf.Max(.001f, message.Duration))));
                actor.Hold(); yield return null;
            }
            transition = null;
        }
        private void SetRage(bool active)
        {
            var rage = GetComponent<模块_BOSS生命和狂暴>();
            if (rage == null) return;
            if (!active) rage.CancelInvoke();
            rage.是否触发累积生气 = active; rage.enabled = active;
        }
        private void SetPosition(Vector2 point)
        {
            transform.position = new Vector3(point.x, point.y, transform.position.z);
            var body = GetComponent<Rigidbody2D>();
            if (body != null) { body.position = point; body.velocity = Vector2.zero; }
        }
        private void OnDestroy()
        {
            if (actor != null)
            {
                actor.ActionChanged -= Observe;
                if (movementHeld) actor.ExternalMovementHolds = Mathf.Max(0, actor.ExternalMovementHolds - 1);
            }
        }
    }

    public sealed class ThemeBloodMotherLevel : MonoBehaviour
    {
        private ThemeBattleScene scene;
        private ThemeBattleWaves waves;
        private 事件管理器_战斗 events;
        private readonly HashSet<GameObject> registered = new HashSet<GameObject>();
        private ThemeBloodMotherActor mother;
        private bool started, awakening;
        internal void Initialize(ThemeBattleScene battlefield, ThemeBattleWaves owner)
        {
            scene = battlefield; waves = owner;
            waves.FinalRewardUnitId = ThemeEldritchData.BloodMother;
            waves.Spawned += Spawned; waves.WaveStarting += WaveStarting;
            waves.AllScheduledEnemiesCleared += Cleared;
            events = Object.FindObjectOfType<事件管理器_战斗>();
            events.事件_单位死亡.AddListener(Died);
            if (Plugin.Role != 2) StartCoroutine(CreateMother());
        }
        private IEnumerator CreateMother()
        {
            yield return null; yield return null;
            foreach (var unit in ThemeBattleWaves.SpawnAt(ThemeEldritchData.BloodMother, 1,
                scene.World(ThemeEldritchData.BloodMotherInitialPosition)))
            {
                unit.transform.position = scene.World(ThemeEldritchData.BloodMotherInitialPosition);
                mother = unit.AddComponent<ThemeBloodMotherActor>();
                mother.Initialize(unit.GetComponent<ThemeEnemyActor>(), scene);
                waves.IgnoreForWaveClear(unit);
            }
        }
        private void Spawned(GameObject unit, ThemeSpawnContext context)
        { if (context != null && context.BatchIndex >= 0) registered.Add(unit); }
        private void WaveStarting(int index) { if (index == 0) started = true; }
        private void Died(GameObject unit)
        {
            if (Plugin.Role == 2 || awakening || !started || !registered.Remove(unit) || mother == null) return;
            mother.Feed();
        }
        private void Update()
        {
            if (Plugin.Role != 2 && started && !awakening && mother != null)
                mother.Drain(Time.deltaTime);
        }
        private void Cleared()
        {
            if (Plugin.Role == 2 || awakening || mother == null) return;
            awakening = true; waves.IgnoreForWaveClear(mother.gameObject, false); mother.Awaken();
        }
        private void OnDestroy()
        {
            if (waves != null)
            { waves.Spawned -= Spawned; waves.WaveStarting -= WaveStarting; waves.AllScheduledEnemiesCleared -= Cleared; }
            if (events != null) events.事件_单位死亡.RemoveListener(Died);
        }
    }

    public sealed class ThemeBloodMotherActor : MonoBehaviour
    {
        internal ThemeEnemyActor Actor;
        internal bool Alive => Actor != null && Actor.Alive && !ended;
        private ThemeBattleScene scene;
        private readonly HashSet<ThemeBloodSacActor> sacs = new HashSet<ThemeBloodSacActor>();
        private readonly EldritchBloodMotherParameters values = ThemeEldritchData.BloodMotherParameters;
        private bool prepared, dormant = true, ended;
        private float dormantHealth, healthScale = 1, nextThrow, nextSweep = float.PositiveInfinity;

        internal void Initialize(ThemeEnemyActor actor, ThemeBattleScene battlefield)
        {
            Actor = actor; scene = battlefield; Actor.SuppressDefaultCombat = true;
            ThemeDeepSeaCombat.Protect(Actor, true);
            StartCoroutine(Prepare());
        }
        private IEnumerator Prepare()
        {
            yield return null; yield return null;
            if (!Alive) yield break;
            Actor.Hold(); ThemeDeepSeaCombat.Protect(Actor, true);
            var rage = GetComponent<模块_BOSS生命和狂暴>();
            if (rage != null) { rage.CancelInvoke(); rage.是否触发累积生气 = false; rage.enabled = false; }
            dormantHealth = Actor.Stats.当前生命值; healthScale = dormantHealth / values.InitialHealth;
            int enemyCount = 0; foreach (var wave in scene.Definition.Waves) enemyCount += wave.DirectCount;
            SetMaximum((values.InitialHealth + enemyCount * values.HealthPerRegisteredDeath) * healthScale);
            Actor.Stats.当前生命值 = dormantHealth;
            prepared = true; Actor.Emit("blood-dormant", transform.position);
        }
        private void SetMaximum(float maximum)
        {
            Actor.Stats.变化生命值 = maximum / Mathf.Max(.001f, Actor.Stats.生命值倍率) - Actor.Stats.生命值;
            Actor.Stats.运算生命值上限();
        }
        internal void Feed()
        {
            if (!prepared || !dormant || !Alive) return;
            dormantHealth += values.HealthPerRegisteredDeath * healthScale; Actor.Stats.当前生命值 = dormantHealth;
            Actor.Emit("blood-feed", transform.position, .4f, dormantHealth / (values.InitialHealth * healthScale));
        }
        internal void Drain(float seconds)
        {
            if (!prepared || !dormant || !Alive) return;
            dormantHealth = Mathf.Max(values.DormantMinimumHealth * healthScale,
                dormantHealth - values.HealthDrainPerSecond * healthScale * seconds);
            Actor.Stats.当前生命值 = dormantHealth;
        }
        internal void Awaken()
        {
            if (!dormant || !Alive) return;
            dormant = false; StartCoroutine(WakeAndFight());
        }
        private IEnumerator WakeAndFight()
        {
            Actor.Hold(); Actor.Emit("blood-awaken", transform.position, values.AwakeningSeconds);
            SetMaximum(dormantHealth); Actor.Stats.当前生命值 = dormantHealth;
            yield return new WaitForSeconds(values.AwakeningSeconds);
            if (!Alive) yield break;
            ThemeDeepSeaCombat.Protect(Actor, false);
            var rage = GetComponent<模块_BOSS生命和狂暴>();
            if (rage != null) { rage.enabled = true; rage.是否触发累积生气 = true; }
            Vector2 end = scene.World(ThemeEldritchData.BloodMotherWakePath[1]);
            while (Alive && Vector2.Distance(transform.position, end) > .6f)
            { if (Actor.CanAct) Actor.MoveTo(end); else Actor.Hold(); yield return null; }
            nextThrow = Time.time;
            while (Alive)
            {
                if (!Actor.CanAct) { Actor.Hold(); yield return null; continue; }
                if (Time.time >= nextThrow && Actor.SelectTarget(values.SacThrowRange) != null)
                { nextThrow = Time.time + values.SacThrowIntervalSeconds; yield return Throw(); continue; }
                var target = Actor.SelectTarget(values.SweepRadius);
                if (Time.time >= nextSweep && target != null)
                { nextSweep = Time.time + values.SweepIntervalSeconds; yield return Sweep(target.transform.position); continue; }
                var melee = Actor.SelectTarget(Actor.Attack.当前攻击范围);
                if (melee != null)
                {
                    Actor.Hold(); Actor.Emit("attack", transform.position, Actor.Definition.AttackWindupSeconds, 0, melee.transform.position);
                    yield return Actor.Pause(Actor.Definition.AttackWindupSeconds);
                    if (Alive && melee != null &&
                        Vector2.Distance(transform.position, melee.transform.position) <= Actor.Attack.当前攻击范围 + .2f)
                        ThemeCombat.Hit(Actor, melee.GetComponent<模块_基本属性>(), Actor.Definition.FinalDamage, melee.transform.position);
                    yield return Actor.Pause(Mathf.Max(.05f, Actor.Definition.AttackIntervalSeconds - Actor.Definition.AttackWindupSeconds));
                }
                else
                {
                    var baseUnit = ThemeDeepSeaCombat.BaseUnit();
                    if (baseUnit != null) Actor.MoveTo(baseUnit.transform.position);
                    yield return new WaitForSeconds(.1f);
                }
            }
        }
        private IEnumerator Throw()
        {
            Actor.Hold();
            int count = Actor.Stats.当前生命值 >= values.HighHealthSacThreshold * healthScale ? values.HighHealthSacCount : values.NormalSacCount;
            var points = ThrowPoints(count);
            foreach (var point in points) Actor.Emit("sac-warning", transform.position, values.SacWindupSeconds + values.SacFlightSeconds, values.SacRadius, point);
            yield return Actor.Pause(values.SacWindupSeconds);
            if (!Alive) yield break;
            foreach (var point in points)
                foreach (var unit in ThemeBattleWaves.SpawnAt(ThemeEldritchData.BloodSac, 1, transform.position))
                {
                    var sac = unit.AddComponent<ThemeBloodSacActor>();
                    sac.Initialize(unit.GetComponent<ThemeEnemyActor>(), this, point); sacs.Add(sac);
                }
            if (float.IsPositiveInfinity(nextSweep)) nextSweep = Time.time + values.SacFlightSeconds + values.SacLifetimeSeconds;
            yield return Actor.Pause(values.SacFlightSeconds);
        }
        private List<Vector2> ThrowPoints(int count)
        {
            var targets = new List<模块_基本属性>();
            var front = Actor.SelectTarget(values.SacThrowRange)?.GetComponent<模块_基本属性>();
            if (front != null) targets.Add(front);
            var baseUnit = ThemeDeepSeaCombat.BaseUnit();
            if (baseUnit != null)
                foreach (var tower in ThemeDeepSeaCombat.ClosestTowers(baseUnit.transform.position, count))
                    if (!targets.Contains(tower)) targets.Add(tower);
            foreach (var unit in 世界事件系统.场上所有单位)
                if (ThemeCombat.Friendly(unit) && !targets.Contains(unit)) targets.Add(unit);
            var points = new List<Vector2>();
            foreach (var target in targets)
            {
                for (int ring = 0; ring <= 8; ring++)
                {
                    bool accepted = false;
                    for (int i = 0; i < (ring == 0 ? 1 : 16); i++)
                    {
                        float angle = i * Mathf.PI / 8;
                        Vector2 point = (Vector2)target.transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ring * .5f;
                        if (!scene.ValidFootprint(point, Vector2.one * .8f)) continue;
                        bool separated = true;
                        foreach (var existing in points) if (Vector2.Distance(existing, point) < values.MinimumSacSeparation) { separated = false; break; }
                        if (!separated) continue;
                        points.Add(point); accepted = true; break;
                    }
                    if (accepted) break;
                }
                if (points.Count == count) break;
            }
            return points;
        }
        private IEnumerator Sweep(Vector2 point)
        {
            Actor.Hold(); Vector2 direction = (point - (Vector2)transform.position).normalized; Actor.Face(direction);
            Actor.Emit("blood-sweep-warning", transform.position, values.SweepWarningSeconds, values.SweepRadius,
                (Vector2)transform.position + direction * values.SweepRadius);
            yield return Actor.Pause(values.SweepWarningSeconds);
            if (!Alive) yield break;
            ThemeDeepSeaCombat.Cone(Actor, transform.position, direction, values.SweepRadius, values.SweepAngleDegrees, values.SweepFinalDamage);
            Actor.Emit("blood-sweep", transform.position, values.SweepRecoverySeconds, values.SweepRadius);
            yield return Actor.Pause(values.SweepRecoverySeconds);
        }
        internal void Remove(ThemeBloodSacActor sac) { sacs.Remove(sac); }
        public void 死亡触发()
        {
            if (ended) return; ended = true; StopAllCoroutines();
            foreach (var sac in new List<ThemeBloodSacActor>(sacs)) if (sac != null) sac.Cancel();
            sacs.Clear();
        }
        private void OnDestroy() { 死亡触发(); }
    }

    public sealed class ThemeBloodSacActor : MonoBehaviour
    {
        private ThemeEnemyActor actor;
        private ThemeBloodMotherActor owner;
        private Vector2 landing;
        private bool resolved;
        internal void Initialize(ThemeEnemyActor unit, ThemeBloodMotherActor mother, Vector2 point)
        {
            actor = unit; owner = mother; landing = point; actor.SuppressDefaultCombat = true;
            ThemeDeepSeaCombat.Protect(actor, true); StartCoroutine(Run());
        }
        private IEnumerator Run()
        {
            yield return null; yield return null;
            if (!actor.Alive || owner == null || !owner.Alive) yield break;
            var values = ThemeEldritchData.BloodMotherParameters;
            actor.Hold(); actor.ExternalMovementHolds++; ThemeDeepSeaCombat.Protect(actor, true);
            Vector2 start = transform.position; float time = 0;
            actor.Emit("sac-flight", start, values.SacFlightSeconds, 1, landing, NetworkObjects.Id(owner.gameObject));
            while (actor.Alive && time < values.SacFlightSeconds)
            { time += Time.deltaTime; transform.position = Vector2.Lerp(start, landing, Mathf.Clamp01(time / values.SacFlightSeconds)); yield return null; }
            if (!actor.Alive || !owner.Alive) yield break;
            transform.position = landing; actor.Hold(); ThemeDeepSeaCombat.Protect(actor, false);
            actor.Emit("sac-land", landing, values.SacLifetimeSeconds, values.SacRadius);
            yield return new WaitForSeconds(values.SacLifetimeSeconds);
            if (resolved || !actor.Alive || !owner.Alive) yield break;
            resolved = true; ThemeCombat.Area(actor, landing, values.SacRadius, values.SacFinalDamage);
            actor.Emit("sac-burst", landing, .5f, values.SacRadius);
            actor.Stats.当前生命值 = 0; actor.GetComponent<模块_死亡>()?.死亡实现();
        }
        public void 死亡触发()
        {
            StopAllCoroutines();
            if (!resolved && Plugin.Role != 2 && owner != null && owner.Alive)
            {
                resolved = true;
                owner.Actor.GetComponent<模块_受伤>().受到伤害(new 世界类.伤害数据包 {
                    攻击者 = actor.Stats.最近攻击者, 攻击目标 = owner.transform, 所属阵营 = 阵营数据库.阵营.友方,
                    攻击伤害 = ThemeEldritchData.BloodMotherParameters.SacBacklashDamage
                });
                actor.Emit("sac-backlash", transform.position, .4f, 1, owner.transform.position);
            }
            owner?.Remove(this);
        }
        internal void Cancel()
        {
            resolved = true; StopAllCoroutines();
            if (actor != null && actor.Alive)
            { actor.Stats.当前生命值 = 0; actor.GetComponent<模块_死亡>()?.死亡实现(); }
        }
        private void OnDestroy() { owner?.Remove(this); }
    }
}
