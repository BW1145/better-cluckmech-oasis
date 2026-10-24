using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CluckmechCoop
{
    public sealed class ThemeWhiteVisitorActor : MonoBehaviour
    {
        internal static ThemeWhiteVisitorActor Current;
        internal ThemeEnemyActor Actor;
        internal bool Alive => Actor != null && Actor.Alive && !ended;
        internal ThemeBattleScene Scene;
        private readonly EldritchWhiteVisitorParameters values = ThemeEldritchData.WhiteParameters;
        private readonly List<ThemeWhiteHandActor> hands = new List<ThemeWhiteHandActor>();
        private readonly List<GameObject> reinforcements = new List<GameObject>();
        private ThemeSharedBodyPart mouth;
        private 模块_BOSS生命和狂暴 rage;
        private EldritchWindowSide side = EldritchWindowSide.North;
        private int contactIndex, previousPlayer = -1;
        private float residenceStarted = -1, previewAt = -1, nextBite, nextSpit, nextSiege;
        private bool phasePending, phaseTwo, ended, traveling;

        internal void Initialize(ThemeEnemyActor actor, ThemeBattleScene scene)
        {
            Current = this; Actor = actor; Scene = scene; Actor.SuppressDefaultCombat = true;
            mouth = gameObject.AddComponent<ThemeSharedBodyPart>(); mouth.Bind(Actor, 0, false);
            SetContactShape(Actor, values.ContactRadius);
            Actor.ActionChanged += Changed;
            if (Plugin.Role != 2) StartCoroutine(Begin());
        }
        private IEnumerator Begin()
        {
            yield return null; yield return null;
            if (!Alive) yield break;
            Actor.Hold(); SetContactShape(Actor, values.ContactRadius);
            rage = ThemeDeepSeaCombat.ScaleNativeRage(Actor, true);
            transform.position = Scene.World(Contact(side).Position);
            while (Alive)
            {
                if (phasePending && !phaseTwo) { yield return Transition(); continue; }
                if (phaseTwo) { yield return PhaseTwoAction(); continue; }
                if (rage != null && rage.是否已生气) { yield return Siege(); continue; }
                yield return Residence();
                if (!Alive || phasePending || phaseTwo || rage != null && rage.是否已生气) continue;
                var next = ThemeEldritchData.WhiteContactCycle[(contactIndex + 1) % ThemeEldritchData.WhiteContactCycle.Length];
                traveling = true; residenceStarted = -1;
                StartCoroutine(OldWindowTroops(side));
                yield return Move(next);
                side = next; contactIndex = (contactIndex + 1) % ThemeEldritchData.WhiteContactCycle.Length;
                traveling = false;
            }
        }
        private void Update()
        {
            if (Plugin.Role == 2 || !Alive) return;
            if (!phaseTwo && Actor.Stats.当前生命值 <= Actor.Stats.当前生命值上限 * values.PhaseTwoHealthFraction) phasePending = true;
            if (!phaseTwo && !phasePending && !traveling && residenceStarted >= 0 && previewAt < 0 &&
                Time.time >= residenceStarted + values.ResidenceSeconds - values.NextSideWarningSeconds)
            {
                previewAt = Time.time;
                var next = ThemeEldritchData.WhiteContactCycle[(contactIndex + 1) % ThemeEldritchData.WhiteContactCycle.Length];
                Actor.Emit("white-next-side", transform.position, values.NextSideWarningSeconds, values.ContactRadius, Scene.World(Contact(next).Position));
            }
        }
        private IEnumerator Residence()
        {
            residenceStarted = Time.time; previewAt = -1;
            nextBite = Time.time + values.FirstBiteDelaySeconds;
            int spit = 0; bool crushed = false;
            Actor.Hold(); Actor.Emit("white-reside", transform.position, values.ResidenceSeconds);
            while (Alive && !phasePending)
            {
                if (rage != null && rage.是否已生气) yield break;
                float elapsed = Time.time - residenceStarted;
                if (elapsed >= values.ResidenceSeconds && previewAt >= 0 && Time.time >= previewAt + values.NextSideWarningSeconds) yield break;
                if (!Actor.CanAct || elapsed >= values.LastNewActionSecond) { Actor.Hold(); yield return null; continue; }
                if (spit < values.SpitAtResidenceSeconds.Length && elapsed >= values.SpitAtResidenceSeconds[spit])
                { spit++; yield return Spit(); continue; }
                if (!crushed && elapsed >= values.CrushAtResidenceSecond)
                { crushed = true; yield return Crush(); continue; }
                if (Time.time >= nextBite && Actor.SelectTarget(values.BiteRange) is GameObject target)
                { nextBite = Time.time + values.BiteIntervalSeconds; yield return Bite(target.transform.position); continue; }
                yield return null;
            }
        }
        private IEnumerator Move(EldritchWindowSide next)
        {
            EldritchFaceRoute route = null;
            foreach (var candidate in ThemeEldritchData.WhiteRoutes)
                if (candidate.From == side && candidate.To == next) { route = candidate; break; }
            if (route == null) yield break;
            Actor.Hold();
            for (int i = 1; Alive && i < route.Points.Length; i++)
            {
                Vector2 end = Scene.World(route.Points[i]);
                Actor.Emit("white-move", transform.position, Vector2.Distance(transform.position, end) / values.RouteSpeed, values.ContactRadius, end);
                while (Alive && Vector2.Distance(transform.position, end) > .03f)
                {
                    if (!Actor.Controlled)
                        transform.position = Vector2.MoveTowards(transform.position, end, values.RouteSpeed * Time.deltaTime);
                    yield return null;
                }
            }
        }
        private IEnumerator OldWindowTroops(EldritchWindowSide previous)
        {
            Vector2 point = Scene.World(Contact(previous).Position);
            for (int batch = 0; Alive && batch < values.DrownedPerContact / values.DrownedPerBatch; batch++)
            {
                if (batch > 0) yield return new WaitForSeconds(values.DrownedBatchIntervalSeconds);
                if (!Alive) yield break;
                reinforcements.AddRange(ThemeBattleWaves.SpawnAt(ThemeEldritchData.Drowned, values.DrownedPerBatch, point));
            }
        }
        private IEnumerator Bite(Vector2 point)
        {
            Actor.Hold(); Actor.Emit("white-bite-warning", transform.position, values.BiteWarningSeconds, values.BiteRadius, point);
            yield return Actor.Pause(values.BiteWarningSeconds);
            if (!Alive) yield break;
            ThemeCombat.Area(Actor, point, values.BiteRadius, values.BiteFinalDamage);
            Actor.Emit("white-bite", transform.position, .3f, values.BiteRadius, point);
        }
        private IEnumerator Spit()
        {
            var target = PlayerTarget(); if (target == null) yield break;
            Vector2 point = target.transform.position;
            Actor.Hold(); Actor.Emit("white-spit-warning", transform.position, values.SpitWarningSeconds, values.SpitRadius, point, NetworkObjects.Id(target));
            yield return Actor.Pause(values.SpitWarningSeconds);
            if (!Alive) yield break;
            Actor.Emit("white-spit", transform.position, .35f, values.SpitRadius, point);
            ThemeCombat.Area(Actor, point, values.SpitRadius, values.SpitFinalDamage);
        }
        private IEnumerator Crush()
        {
            var contact = Contact(side); Vector2 inward = new Vector2(contact.GroundInward.X, contact.GroundInward.Y);
            Vector2 tangent = new Vector2(-inward.y, inward.x);
            Vector2 start = (Vector2)transform.position - tangent * (values.CrushWidth * .5f) + inward * (values.CrushDepth * .5f);
            Vector2 end = (Vector2)transform.position + tangent * (values.CrushWidth * .5f) + inward * (values.CrushDepth * .5f);
            int left = 0, right = 0;
            foreach (var unit in 世界事件系统.场上所有单位)
            {
                if (!ThemeCombat.Friendly(unit)) continue;
                if (Vector2.Dot((Vector2)unit.transform.position - (Vector2)transform.position, tangent) < 0) left++; else right++;
            }
            if (left > right) { var swap = start; start = end; end = swap; }
            Actor.Hold(); Actor.Emit("white-crush-warning", start, values.CrushWarningSeconds, values.CrushDepth, end);
            yield return Actor.Pause(values.CrushWarningSeconds);
            if (!Alive) yield break;
            Actor.Emit("white-crush", start, values.CrushDurationSeconds, values.CrushDepth, end);
            ThemeDeepSeaCombat.Band(Actor, start, end, values.CrushDepth, values.CrushFinalDamage, false);
            yield return Actor.Pause(values.CrushDurationSeconds);
        }
        private IEnumerator Transition()
        {
            phaseTwo = true; phasePending = false; residenceStarted = -1;
            Actor.Hold(); mouth.DamageMultiplier = values.PhaseTwoMouthDamageMultiplier;
            var positions = HandPositions();
            Actor.Emit("white-phase", transform.position, values.PhaseTransitionSeconds);
            for (int i = 0; i < positions.Count; i++)
                foreach (var unit in ThemeBattleWaves.SpawnAt(ThemeEldritchData.WhiteHand, 1, positions[i]))
                {
                    unit.transform.position = positions[i];
                    var hand = unit.AddComponent<ThemeWhiteHandActor>();
                    hand.Initialize(unit.GetComponent<ThemeEnemyActor>(), this, i + 1,
                        values.PhaseTransitionSeconds + i * values.SecondHandStartDelaySeconds);
                    hands.Add(hand);
                }
            yield return Actor.Pause(values.PhaseTransitionSeconds);
            nextBite = Time.time; nextSpit = Time.time + 3;
        }
        private List<Vector2> HandPositions()
        {
            var result = new List<Vector2>(); var candidates = new List<Vector2>();
            var baseUnit = ThemeDeepSeaCombat.BaseUnit(); if (baseUnit == null) return result;
            Vector2 center = baseUnit.transform.position;
            for (int i = 0; i < 72; i++)
            {
                float angle = i * Mathf.PI / 36;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * values.HandInitialBaseDistance;
                if (ThemeBattleGeometry.ContainsFootprint(Scene.Definition.Floors, point.x - Scene.Origin.x, point.y - Scene.Origin.y,
                    values.HandRadius * 2, values.HandRadius * 2) && Physics2D.OverlapCircle(point, values.HandRadius, 1 << 10) == null)
                    candidates.Add(point);
            }
            float best = -1, alignment = -1;
            Vector2 sideAxis = new Vector2(-Contact(side).GroundInward.Y, Contact(side).GroundInward.X);
            for (int i = 0; i < candidates.Count; i++)
                for (int j = i + 1; j < candidates.Count; j++)
                {
                    float distance = (candidates[i] - candidates[j]).sqrMagnitude;
                    float value = Mathf.Abs(Vector2.Dot((candidates[i] - candidates[j]).normalized, sideAxis));
                    if (distance > best + .01f || Mathf.Abs(distance - best) <= .01f && value > alignment)
                    {
                        best = distance; alignment = value; result.Clear(); result.Add(candidates[i]); result.Add(candidates[j]);
                    }
                }
            return result;
        }
        private IEnumerator PhaseTwoAction()
        {
            if (!Actor.CanAct) { Actor.Hold(); yield return null; yield break; }
            if (rage != null && rage.是否已生气) { yield return Siege(); yield break; }
            if (Time.time >= nextSpit) { nextSpit = Time.time + values.PhaseTwoSpitIntervalSeconds; yield return Spit(); yield break; }
            var target = Actor.SelectTarget(values.BiteRange);
            if (target != null && Time.time >= nextBite)
            { nextBite = Time.time + values.BiteIntervalSeconds; yield return Bite(target.transform.position); yield break; }
            yield return null;
        }
        private IEnumerator Siege()
        {
            if (Time.time < nextSiege || !Actor.CanAct) { yield return null; yield break; }
            var baseUnit = ThemeDeepSeaCombat.BaseUnit(); if (baseUnit == null) { yield return null; yield break; }
            nextSiege = Time.time + values.SiegeIntervalSeconds;
            Actor.Hold(); Actor.Emit("white-siege-warning", transform.position, values.SiegeWarningSeconds, 1.5f, baseUnit.transform.position);
            yield return Actor.Pause(values.SiegeWarningSeconds);
            if (Alive && baseUnit != null) ThemeCombat.Hit(Actor, baseUnit, values.SiegeFinalDamage, baseUnit.transform.position);
        }
        private GameObject PlayerTarget()
        {
            GameObject result = null; int role = int.MaxValue;
            foreach (var unit in 世界事件系统.场上所有单位)
                if (ThemeCombat.Friendly(unit) && ThemeDeepSeaCombat.IsPlayer(unit) && (int)unit.所属玩家 > previousPlayer && (int)unit.所属玩家 < role)
                { result = unit.gameObject; role = (int)unit.所属玩家; }
            if (result == null)
                foreach (var unit in 世界事件系统.场上所有单位)
                    if (ThemeCombat.Friendly(unit) && ThemeDeepSeaCombat.IsPlayer(unit) && (int)unit.所属玩家 < role)
                    { result = unit.gameObject; role = (int)unit.所属玩家; }
            if (result != null) previousPlayer = role;
            return result ?? ThemeDeepSeaCombat.BaseUnit()?.gameObject;
        }
        private EldritchContactDefinition Contact(EldritchWindowSide value)
        {
            foreach (var contact in ThemeEldritchData.WhiteContacts) if (contact.Side == value) return contact;
            return ThemeEldritchData.WhiteContacts[0];
        }
        private void Changed(ThemeActorMessage action)
        {
            if (Plugin.Role == 2 && action.Action == "white-phase")
            { phaseTwo = true; mouth.DamageMultiplier = values.PhaseTwoMouthDamageMultiplier; }
        }
        internal static void SetContactShape(ThemeEnemyActor actor, float radius)
        {
            foreach (var shape in actor.GetComponents<Collider2D>()) shape.enabled = false;
            var circle = actor.GetComponent<CircleCollider2D>();
            if (circle == null) circle = actor.gameObject.AddComponent<CircleCollider2D>();
            circle.isTrigger = true; circle.radius = radius; circle.enabled = true;
            var body = actor.GetComponent<Rigidbody2D>();
            if (body != null) { body.bodyType = RigidbodyType2D.Kinematic; body.velocity = Vector2.zero; }
        }
        public void 死亡触发()
        {
            if (ended) return; ended = true; StopAllCoroutines();
            if (Plugin.Role == 2) return;
            foreach (var hand in hands) if (hand != null) hand.Retire();
            foreach (var unit in reinforcements)
                if (unit != null && unit.GetComponent<模块_基本属性>() is 模块_基本属性 health && health.当前生命值 > 0)
                { health.当前生命值 = 0; unit.GetComponent<模块_死亡>()?.死亡实现(); }
        }
        private void OnDestroy()
        { if (Actor != null) Actor.ActionChanged -= Changed; 死亡触发(); if (Current == this) Current = null; }
    }

    public sealed class ThemeWhiteHandActor : MonoBehaviour
    {
        private ThemeEnemyActor actor;
        private ThemeWhiteVisitorActor owner;
        private ThemeSharedBodyPart shared;
        private readonly EldritchWhiteVisitorParameters values = ThemeEldritchData.WhiteParameters;
        private float startAt, nextSlam, nextGrasp, damageHeld, recoilUntil;
        private Vector2 recoilFrom, recoilTo;
        private bool paused, ended;
        internal void Initialize(ThemeEnemyActor unit, ThemeWhiteVisitorActor boss, int index, float delay)
        {
            actor = unit; owner = boss; actor.SuppressDefaultCombat = true;
            shared = gameObject.AddComponent<ThemeSharedBodyPart>(); shared.Bind(boss.Actor, index, false);
            shared.Damaged += Damaged;
            startAt = Time.time + delay; nextSlam = startAt + values.HandSlamIntervalSeconds;
            if (Plugin.Role != 2) StartCoroutine(Fight());
        }
        private IEnumerator Fight()
        {
            yield return null; yield return null;
            ThemeWhiteVisitorActor.SetContactShape(actor, values.HandRadius); actor.Hold();
            actor.Emit("white-hand-arrive", transform.position, Mathf.Max(0, startAt - Time.time), values.HandRadius);
            while (actor.Alive && owner.Alive && !ended)
            {
                if (Time.time < startAt || actor.Controlled) { yield return null; continue; }
                var baseUnit = ThemeDeepSeaCombat.BaseUnit();
                if (baseUnit == null) { yield return null; continue; }
                if (Vector2.Distance(transform.position, baseUnit.transform.position) <= values.GraspBaseDistance && Time.time >= nextGrasp)
                { nextGrasp = Time.time + values.GraspIntervalSeconds; yield return Grasp(baseUnit); continue; }
                if (Time.time >= nextSlam)
                { nextSlam = Time.time + values.HandSlamIntervalSeconds; yield return Slam(); continue; }
                yield return null;
            }
        }
        private void Update()
        {
            if (Plugin.Role == 2 || actor == null || !actor.Alive || owner == null || !owner.Alive || ended) return;
            var baseUnit = ThemeDeepSeaCombat.BaseUnit(); if (baseUnit == null) return;
            actor.Hold();
            if (Time.time < recoilUntil)
                transform.position = Vector2.Lerp(recoilFrom, recoilTo, 1 - (recoilUntil - Time.time) / values.HandRecoilSeconds);
            else if (!paused && !actor.Controlled && Time.time >= startAt)
                transform.position = Vector2.MoveTowards(transform.position, baseUnit.transform.position,
                    ThemeEldritchData.WhiteHandSpeed(ModuleSettings.Progress.Branch.EventChoice) * Time.deltaTime);
        }
        private void Damaged(float damage)
        {
            if (Plugin.Role == 2 || ended || owner == null || !owner.Alive) return;
            damageHeld += damage; int steps = Mathf.FloorToInt(damageHeld / values.HandDamagePerPush);
            if (steps == 0) return;
            damageHeld -= steps * values.HandDamagePerPush;
            var baseUnit = ThemeDeepSeaCombat.BaseUnit(); if (baseUnit == null) return;
            Vector2 delta = (Vector2)transform.position - (Vector2)baseUnit.transform.position;
            Vector2 previousTarget = Time.time < recoilUntil ? recoilTo : (Vector2)transform.position;
            float distance = Mathf.Min(values.HandMaximumBaseDistance, Vector2.Distance(previousTarget, baseUnit.transform.position) + steps * values.HandPushDistance);
            recoilFrom = transform.position; recoilTo = (Vector2)baseUnit.transform.position + delta.normalized * distance;
            recoilUntil = Time.time + values.HandRecoilSeconds;
            actor.Emit("white-hand-recoil", recoilFrom, values.HandRecoilSeconds, values.HandRadius, recoilTo);
        }
        private IEnumerator Slam()
        {
            paused = true; Vector2 point = transform.position;
            actor.Emit("white-hand-slam-warning", point, values.HandSlamWarningSeconds, values.HandSlamRadius);
            yield return actor.Pause(values.HandSlamWarningSeconds);
            if (actor.Alive && owner.Alive) ThemeCombat.Area(actor, point, values.HandSlamRadius, values.HandSlamFinalDamage);
            actor.Emit("white-hand-slam", point, .3f, values.HandSlamRadius); paused = false;
        }
        private IEnumerator Grasp(模块_基本属性 baseUnit)
        {
            paused = true; Vector2 point = baseUnit.transform.position; float elapsed = 0;
            actor.Emit("white-hand-grasp-warning", transform.position, values.GraspWarningSeconds, values.GraspRadius, point);
            while (actor.Alive && owner.Alive && baseUnit != null && baseUnit.当前生命值 > 0 && elapsed < values.GraspWarningSeconds)
            {
                if (Vector2.Distance(transform.position, baseUnit.transform.position) > values.GraspBaseDistance)
                { actor.MotionPause(false); actor.Emit("white-hand-grasp-cancel", transform.position, .3f, values.HandRadius); paused = false; yield break; }
                actor.MotionPause(!actor.CanAct);
                if (actor.CanAct) elapsed += Time.deltaTime;
                yield return null;
            }
            if (actor.Alive) actor.MotionPause(false);
            if (actor.Alive && owner.Alive && baseUnit != null && baseUnit.当前生命值 > 0)
                ThemeCombat.Area(actor, point, values.GraspRadius, values.GraspFinalDamage);
            actor.Emit("white-hand-grasp", transform.position, .5f, values.GraspRadius, point); paused = false;
        }
        internal void Retire()
        {
            if (ended) return; ended = true; StopAllCoroutines();
            if (actor != null && actor.Alive)
            { actor.Stats.当前生命值 = 0; actor.GetComponent<模块_死亡>()?.死亡实现(); }
        }
        private void OnDestroy() { if (shared != null) shared.Damaged -= Damaged; }
    }
}
