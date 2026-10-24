using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CluckmechCoop
{
    public sealed class ThemeCastleEldritch : MonoBehaviour
    {
        private static ThemeCastleEldritch current;
        private ThemeBattleScene scene;
        private ThemeBattleWaves waves;
        private readonly Dictionary<string, ThemeTransportRoute> routes = new Dictionary<string, ThemeTransportRoute>();

        internal static void Attach(ThemeBattleScene scene)
        {
            if (scene == null || !scene.Eldritch || ThemeBranchRuntime.State.Theme != 1) return;
            var owner = scene.gameObject.AddComponent<ThemeCastleEldritch>(); current = owner;
            owner.scene = scene; owner.waves = ThemeBattleWaves.Current;
            ThemeEnemyActor.Ready += owner.Ready;
            if (scene.Definition.Id == ThemeEldritchData.Banquet.Id)
            {
                var values = ThemeEldritchData.BanquetParameters;
                foreach (var link in ThemeEldritchData.TableLinks)
                    owner.routes.Add(link.Id, new ThemeTransportRoute(scene, link, values.EnterSeconds,
                        values.TransitSeconds, values.ExitSeconds, values.EntrySpacingSeconds));
                owner.waves.Spawned += owner.Spawned;
            }
        }

        private void Ready(ThemeEnemyActor actor)
        {
            if (actor.UnitId == ThemeEldritchData.ShadowKnight || actor.UnitId == ThemeEldritchData.ShadowCrossbow ||
                actor.UnitId == ThemeEldritchData.ShadowLeader)
                actor.gameObject.AddComponent<ThemeShadowAttack>().Initialize(actor);
            else if (actor.UnitId == ThemeEldritchData.HangingCount)
            {
                actor.SuppressDefaultCombat = true;
                actor.gameObject.AddComponent<ThemeHangingCount>().Initialize(actor, scene);
            }
            else if (actor.UnitId == ThemeEldritchData.FleshKnot) actor.SuppressDefaultCombat = true;
            else if (actor.UnitId == ThemeEldritchData.BanquetEater)
            {
                actor.SuppressDefaultCombat = true;
                if (ThemeBanquetBoss.Current == null)
                    actor.gameObject.AddComponent<ThemeBanquetBoss>().Initialize(actor, scene);
                else ThemeBanquetBoss.Current.AttachPart(actor);
            }
        }

        private void Spawned(GameObject unit, ThemeSpawnContext context)
        {
            if (context == null || context.BatchIndex < 0) return;
            foreach (var assignment in ThemeEldritchData.TableAssignments)
                if (assignment.WaveNumber == context.WaveIndex + 1 && assignment.BatchIndex == context.BatchIndex &&
                    assignment.EntranceId == context.EntranceId && assignment.UnitId == context.UnitId &&
                    context.UnitIndex < assignment.Count)
                { routes[assignment.LinkId].Assign(unit); break; }
        }

        internal static void ChandelierDropped(string id)
        {
            if (current == null || current.routes.Count == 0) return;
            string link = id == "west-chandelier" ? ThemeEldritchData.TableEastToWest : ThemeEldritchData.TableWestToEast;
            current.routes[link].Close(ThemeEldritchData.BanquetParameters.ChandelierClosureSeconds);
        }

        internal static void StopTableRoutes()
        { if (current != null) foreach (var route in current.routes.Values) route.Stop(); }

        private void OnDestroy()
        {
            ThemeEnemyActor.Ready -= Ready;
            if (waves != null) waves.Spawned -= Spawned;
            foreach (var route in routes.Values) route.Stop();
            if (current == this) current = null;
        }
    }

    [HarmonyPatch(typeof(ThemeBattleWaves), "Attach")]
    internal static class ThemeCastleEldritchScene
    { private static void Postfix(ThemeBattleScene scene) => ThemeCastleEldritch.Attach(scene); }

    public sealed class ThemeShadowAttack : MonoBehaviour
    {
        private sealed class Shot
        {
            internal GameObject Prototype;
            internal Vector3 Position;
            internal Quaternion Rotation;
            internal 世界类.伤害数据包 Damage;
        }
        private sealed class Snapshot
        {
            internal uint Sequence;
            internal Vector2 Origin, End;
            internal readonly List<Shot> Shots = new List<Shot>();
        }
        private readonly Dictionary<uint, Snapshot> pending = new Dictionary<uint, Snapshot>();
        private ThemeEnemyActor actor;
        private Transform cache;
        private uint sequence;
        private bool replaying;

        internal void Initialize(ThemeEnemyActor owner)
        {
            actor = owner; actor.Fired += Fired; actor.ActionChanged += Changed;
            var root = new GameObject("ThemeShadowSnapshots"); root.SetActive(false);
            root.transform.SetParent(transform, false); cache = root.transform;
        }

        private void Fired(IReadOnlyList<GameObject> projectiles)
        {
            if (replaying || !actor.Alive) return;
            var snapshot = new Snapshot { Sequence = ++sequence, Origin = transform.position };
            foreach (var projectile in projectiles)
            {
                if (projectile == null) continue;
                var spirit = projectile.GetComponent<模块_伤害精灵>();
                var area = projectile.GetComponent<模块_范围伤害精灵>();
                var packet = spirit != null ? spirit.伤害数据 : area?.伤害数据;
                if (packet == null) continue;
                var prototype = Object.Instantiate(projectile, cache);
                snapshot.Shots.Add(new Shot { Prototype = prototype, Position = projectile.transform.position,
                    Rotation = projectile.transform.rotation, Damage = ThemeSharedDamageScope.Copy(packet, 0.5f) });
                snapshot.End = (Vector2)projectile.transform.position + (Vector2)projectile.transform.right * actor.Attack.当前攻击范围;
            }
            if (snapshot.Shots.Count == 0) return;
            pending.Add(snapshot.Sequence, snapshot);
            if (Plugin.Role == 2) return;
            Overlay("shadow-warning", snapshot, ThemeEldritchData.ShadowParameters.DelaySeconds);
            StartCoroutine(Delay(snapshot));
        }

        private IEnumerator Delay(Snapshot snapshot)
        {
            float elapsed = 0;
            while (actor.Alive && elapsed < ThemeEldritchData.ShadowParameters.DelaySeconds)
            {
                if (actor.Controlled)
                {
                    Overlay("shadow-cancel", snapshot, 0); Remove(snapshot); yield break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (!actor.Alive) { Remove(snapshot); yield break; }
            Overlay("shadow-shot", snapshot, ThemeEldritchData.ShadowParameters.RecedeSeconds);
            Replay(snapshot); Remove(snapshot);
        }

        private void Overlay(string action, Snapshot snapshot, float duration)
        {
            Vector2 facing = actor.Facing;
            actor.Emit(action, snapshot.Origin, duration, 0, snapshot.End, snapshot.Sequence);
            actor.Face(facing);
        }

        private void Changed(ThemeActorMessage action)
        {
            if (Plugin.Role != 2 || !pending.TryGetValue(action.Target, out var snapshot)) return;
            if (action.Action == "shadow-shot") { Replay(snapshot); Remove(snapshot); }
            else if (action.Action == "shadow-cancel") Remove(snapshot);
        }

        private void Replay(Snapshot snapshot)
        {
            replaying = true;
            var volley = new HashSet<GameObject>();
            foreach (var shot in snapshot.Shots)
            {
                if (shot.Prototype == null) continue;
                var projectile = Object.Instantiate(shot.Prototype, shot.Position, shot.Rotation);
                var damage = projectile.GetComponent<模块_伤害精灵>();
                if (damage != null) damage.伤害数据 = ThemeSharedDamageScope.Copy(shot.Damage);
                var area = projectile.GetComponent<模块_范围伤害精灵>();
                if (area != null) area.伤害数据 = ThemeSharedDamageScope.Copy(shot.Damage);
                if (snapshot.Shots.Count > 1)
                {
                    var group = projectile.GetComponent<ThemeVolleyProjectile>();
                    if (group == null) group = projectile.AddComponent<ThemeVolleyProjectile>();
                    group.Targets = volley;
                }
                projectile.SetActive(true);
            }
            replaying = false;
        }

        private void Remove(Snapshot snapshot)
        {
            pending.Remove(snapshot.Sequence);
            foreach (var shot in snapshot.Shots) if (shot.Prototype != null) Object.Destroy(shot.Prototype);
        }
        public void 死亡触发()
        {
            StopAllCoroutines();
            foreach (var snapshot in new List<Snapshot>(pending.Values)) Remove(snapshot);
        }
        private void OnDestroy()
        {
            if (actor != null) { actor.Fired -= Fired; actor.ActionChanged -= Changed; }
            死亡触发();
        }
    }

    public sealed class ThemeBanquetBoss : MonoBehaviour
    {
        internal static ThemeBanquetBoss Current;
        internal ThemeEnemyActor Actor;
        internal bool Alive => Actor != null && Actor.Alive && !ended;
        private ThemeBattleScene scene;
        private readonly List<ThemeEnemyActor> parts = new List<ThemeEnemyActor>();
        private bool ended;

        internal void Initialize(ThemeEnemyActor actor, ThemeBattleScene battlefield)
        {
            Current = this; Actor = actor; scene = battlefield;
            AttachPart(actor);
            actor.WineRecipient = true; actor.WineHealFraction = 0.15f;
            actor.WineApproachPoint = ClosestMouth;
            if (Plugin.Role != 2) StartCoroutine(Fight());
        }

        internal void AttachPart(ThemeEnemyActor part)
        {
            if (parts.Contains(part)) return;
            var shared = part.GetComponent<ThemeSharedBodyPart>();
            if (shared == null) shared = part.gameObject.AddComponent<ThemeSharedBodyPart>();
            shared.Bind(Actor, parts.Count, true);
            parts.Add(part); part.SuppressDefaultCombat = true; part.Hold();
            if (part != Actor)
            {
                part.WineRecipient = false;
                var victory = part.GetComponent<词条_100028_Boss胜利判断>(); if (victory != null) Object.Destroy(victory);
                var rage = part.GetComponent<模块_BOSS生命和狂暴>(); if (rage != null) Object.Destroy(rage);
            }
        }

        private Vector2 ClosestMouth(Vector2 point)
        {
            Vector2 result = scene.World(ThemeEldritchData.BanquetMouths[0]); float distance = float.MaxValue;
            foreach (var mouth in ThemeEldritchData.BanquetMouths)
            {
                var candidate = scene.World(mouth); float value = (candidate - point).sqrMagnitude;
                if (value < distance) { result = candidate; distance = value; }
            }
            return result;
        }

        private IEnumerator Fight()
        {
            Vector2 first = scene.World(ThemeEldritchData.BanquetMouths[0]);
            transform.position = new Vector3(first.x, first.y, transform.position.z); Actor.Hold();
            for (int i = 1; i < ThemeEldritchData.BanquetMouths.Length; i++)
                foreach (var unit in ThemeBattleWaves.SpawnAt(ThemeEldritchData.BanquetEater, 1, scene.World(ThemeEldritchData.BanquetMouths[i])))
                {
                    var part = unit.GetComponent<ThemeEnemyActor>();
                    part.transform.position = scene.World(ThemeEldritchData.BanquetMouths[i]);
                    AttachPart(part);
                }
            Actor.Emit("banquet-open", transform.position, 1);
            var values = ThemeEldritchData.BanquetParameters;
            while (Alive)
            {
                yield return Actor.Pause(values.CrackCooldownSeconds);
                if (!Alive) yield break;
                var target = Actor.SelectTarget(values.CrackMaximumLength + 10);
                if (target == null) continue;
                Vector2 start = ClosestMouth(target.transform.position);
                Vector2 direction = ((Vector2)target.transform.position - start).normalized;
                Vector2 end = start + direction * Mathf.Min(values.CrackMaximumLength, Vector2.Distance(start, target.transform.position) + 2);
                Actor.Emit("crack-warning", start, values.CrackWarningSeconds, values.CrackWidth, end);
                yield return Actor.Pause(values.CrackWarningSeconds);
                if (!Alive) yield break;
                Actor.Emit("crack-bite", start, 0.4f, values.CrackWidth, end);
                ThemeCastleAttackGeometry.Line(Actor, start, end, values.CrackWidth, values.CrackFinalDamage, new HashSet<模块_基本属性>());
            }
        }

        public void 死亡触发()
        {
            if (ended) return;
            ended = true; StopAllCoroutines(); ThemeCastleEldritch.StopTableRoutes();
            if (Plugin.Role != 2)
                foreach (var part in parts)
                    if (part != null && part != Actor && part.Alive)
                    { part.Stats.当前生命值 = 0; part.GetComponent<模块_死亡>()?.死亡实现(); }
        }
        private void OnDestroy() { 死亡触发(); if (Current == this) Current = null; }
    }

    public sealed class ThemeHangingCount : MonoBehaviour
    {
        internal ThemeEnemyActor Actor;
        internal bool Fallen;
        private ThemeBattleScene scene;
        private ThemeCountActor supply;
        private readonly List<ThemeFleshKnot> knots = new List<ThemeFleshKnot>();
        private Coroutine main;
        private float nextSupply, nextSweep;
        private int previousPlayer = -1;
        private bool phaseTwo, transitioning, ended;
        private BoxCollider2D hurtBody;
        private Vector2 hurtSize = new Vector2(1.45f, 2.9f);

        internal void Initialize(ThemeEnemyActor actor, ThemeBattleScene battlefield)
        {
            Actor = actor; scene = battlefield; hurtBody = actor.GetComponent<BoxCollider2D>();
            actor.ActionChanged += BodyPose;
            if (Plugin.Role != 2) StartCoroutine(Begin());
        }

        private void BodyPose(ThemeActorMessage message)
        {
            if (message.Action == "hanging-fall") hurtSize = new Vector2(2.8f, .85f);
            else if (message.Action == "hanging-recover") hurtSize = new Vector2(1.45f, 2.25f);
            else if (message.Action == "hanging-rethread") hurtSize = new Vector2(1.45f, 2.9f);
        }

        private IEnumerator Begin()
        {
            yield return null; yield return null;
            supply = GetComponent<ThemeCountActor>(); supply.Actor = Actor;
            SpawnKnots(); main = StartCoroutine(Fight());
        }

        private void Update()
        {
            if (hurtBody != null)
            {
                hurtBody.size = Vector2.MoveTowards(hurtBody.size, hurtSize, Time.deltaTime * 5);
                hurtBody.offset = new Vector2(0, hurtBody.size.y * .5f);
            }
            if (Plugin.Role == 2 || Actor == null || !Actor.Alive || ended || supply == null) return;
            if (!phaseTwo && Actor.Stats.当前生命值 <= Actor.Stats.当前生命值上限 * 0.5f)
            {
                phaseTwo = supply.PhaseTwo = true; supply.SpawnSupply(true);
                nextSupply = Time.time + ThemeCastleData.CountParameters.WineBatchIntervalSeconds;
            }
            else if (phaseTwo && Time.time >= nextSupply)
            { nextSupply = Time.time + ThemeCastleData.CountParameters.WineBatchIntervalSeconds; supply.SpawnSupply(false); }
            if (!transitioning && Time.time >= nextSweep)
            {
                nextSweep = Time.time + ThemeEldritchData.HangingParameters.SweepIntervalSeconds;
                for (int i = 0; i < knots.Count; i++)
                    if (knots[i] != null && knots[i].Alive)
                        knots[i].StartCoroutine(knots[i].Sweep(i * ThemeEldritchData.HangingParameters.BetweenKnotSweepsSeconds));
            }
        }

        private void SpawnKnots()
        {
            knots.Clear();
            var baseUnit = Plugin.Spawner?.GetComponent<yy引用实例数据库>()?.基地车;
            Vector2 origin = baseUnit == null ? scene.World(scene.Definition.BaseHints[0].Example) : (Vector2)baseUnit.transform.position;
            var candidates = new List<ThemeBattlePillar>(scene.Pillars);
            candidates.Sort((a, b) => {
                int distance = (a.Center - origin).sqrMagnitude.CompareTo((b.Center - origin).sqrMagnitude);
                return distance != 0 ? distance : scene.Pillars.IndexOf(a).CompareTo(scene.Pillars.IndexOf(b));
            });
            for (int i = 0; i < ThemeEldritchData.HangingParameters.KnotCount; i++)
            {
                var pillar = candidates[i];
                Vector2 direction = (scene.World(new Vector2(0, 14)) - pillar.Center).normalized;
                Vector2 point = scene.ValidLanding(pillar.Center + direction * ThemeEldritchData.HangingParameters.KnotOffsetFromPillarCenter, Vector2.one);
                foreach (var unit in ThemeBattleWaves.SpawnAt(ThemeEldritchData.FleshKnot, 1, point))
                {
                    var knot = unit.AddComponent<ThemeFleshKnot>();
                    knot.Initialize(this, unit.GetComponent<ThemeEnemyActor>(), pillar, scene);
                    knots.Add(knot);
                }
            }
            nextSweep = Time.time + ThemeEldritchData.HangingParameters.SweepIntervalSeconds;
        }

        private IEnumerator Fight()
        {
            while (Actor.Alive && !ended)
            {
                yield return Melee();
                yield return null;
            }
        }

        private IEnumerator Melee()
        {
            if (!Actor.CanAct) { Actor.Hold(); yield break; }
            var target = Actor.SelectTarget(Actor.Attack.当前攻击范围);
            if (target == null)
            {
                var baseUnit = Plugin.Spawner?.GetComponent<yy引用实例数据库>()?.基地车;
                if (baseUnit != null) Actor.MoveTo(baseUnit.transform.position);
                if (Actor.Action != "walk") Actor.Emit("walk", transform.position);
                yield return new WaitForSeconds(0.1f); yield break;
            }
            Actor.Hold(); Actor.Face((Vector2)target.transform.position - (Vector2)transform.position);
            Actor.Emit("attack", transform.position, Actor.Definition.AttackWindupSeconds, 0, target.transform.position);
            yield return Actor.Pause(Actor.Definition.AttackWindupSeconds);
            if (Actor.Alive && target != null && Vector2.Distance(transform.position, target.transform.position) <= Actor.Attack.当前攻击范围 + .3f)
                ThemeCombat.Hit(Actor, target.GetComponent<模块_基本属性>(), Actor.Definition.FinalDamage, target.transform.position);
            yield return Actor.Pause(Mathf.Max(.05f, 1 / Mathf.Max(.01f, Actor.Attack.当前攻击速度) - Actor.Definition.AttackWindupSeconds));
        }

        internal void KnotDied()
        {
            if (Plugin.Role == 2 || transitioning || ended || !Actor.Alive) return;
            foreach (var knot in knots) if (knot != null && knot.Alive) return;
            transitioning = true;
            if (main != null) StopCoroutine(main);
            main = StartCoroutine(FallAndRecover());
        }

        private IEnumerator FallAndRecover()
        {
            Fallen = true; Actor.Hold();
            float duration = ThemeEldritchData.HangingWindowSeconds(ModuleSettings.Progress.Branch.EventChoice);
            Actor.Emit("hanging-fall", transform.position, duration);
            yield return new WaitForSeconds(duration);
            if (!Actor.Alive) yield break;
            Fallen = false; Actor.Emit("hanging-recover", transform.position, ThemeEldritchData.HangingParameters.RecoverySeconds);
            float end = Time.time + ThemeEldritchData.HangingParameters.RecoverySeconds;
            while (Actor.Alive && Time.time < end) { yield return Melee(); yield return null; }
            if (!Actor.Alive) yield break;
            Actor.Hold(); Actor.Emit("hanging-rethread", transform.position, ThemeEldritchData.HangingParameters.RethreadSeconds);
            yield return Actor.Pause(ThemeEldritchData.HangingParameters.RethreadSeconds);
            if (!Actor.Alive) yield break;
            SpawnKnots(); transitioning = false; main = StartCoroutine(Fight());
        }

        internal GameObject PlayerTarget()
        {
            GameObject selected = null; int selectedRole = int.MaxValue;
            foreach (var unit in 世界事件系统.场上所有单位)
            {
                if (!ThemeCombat.Friendly(unit) || !unit.单位定位列表.Contains(模块_基本属性.单位定位枚举.玩家)) continue;
                int role = (int)unit.所属玩家;
                if (role > previousPlayer && role < selectedRole) { selected = unit.gameObject; selectedRole = role; }
            }
            if (selected == null)
                foreach (var unit in 世界事件系统.场上所有单位)
                {
                    if (!ThemeCombat.Friendly(unit) || !unit.单位定位列表.Contains(模块_基本属性.单位定位枚举.玩家)) continue;
                    int role = (int)unit.所属玩家;
                    if (role < selectedRole) { selected = unit.gameObject; selectedRole = role; }
                }
            if (selected != null) previousPlayer = selectedRole;
            return selected ?? Plugin.Spawner?.GetComponent<yy引用实例数据库>()?.基地车;
        }

        public void 死亡触发()
        {
            if (ended) return; ended = true; StopAllCoroutines();
            if (Plugin.Role == 2) return;
            foreach (var knot in knots)
                if (knot != null && knot.Alive)
                { knot.Actor.Stats.当前生命值 = 0; knot.GetComponent<模块_死亡>()?.死亡实现(); }
        }
        private void OnDestroy() { if (Actor != null) Actor.ActionChanged -= BodyPose; 死亡触发(); }
    }

    public sealed class ThemeFleshKnot : MonoBehaviour
    {
        internal ThemeEnemyActor Actor;
        internal bool Alive => Actor != null && Actor.Alive;
        private ThemeHangingCount owner;
        private ThemeBattlePillar source;
        private ThemeBattleScene scene;
        internal void Initialize(ThemeHangingCount boss, ThemeEnemyActor actor, ThemeBattlePillar pillar, ThemeBattleScene battlefield)
        { owner = boss; Actor = actor; source = pillar; scene = battlefield; actor.SuppressDefaultCombat = true; }

        internal IEnumerator Sweep(float delay)
        {
            if (delay > 0) yield return Actor.Pause(delay);
            if (!Alive || owner == null || !owner.Actor.Alive) yield break;
            var target = owner.PlayerTarget(); if (target == null) yield break;
            var values = ThemeEldritchData.HangingParameters;
            Vector2 start = transform.position, direction = ((Vector2)target.transform.position - start).normalized;
            float center = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float from = center - values.SweepAngleDegrees * .5f, to = center + values.SweepAngleDegrees * .5f;
            Actor.Emit("rope-warning", start, values.SweepWarningSeconds, values.SweepWidth,
                start + (Vector2)(Quaternion.Euler(0, 0, center) * Vector2.right) * values.SweepRange);
            yield return Actor.Pause(values.SweepWarningSeconds);
            if (!Alive) yield break;
            Actor.Emit("rope-sweep", start, values.SweepDurationSeconds, values.SweepWidth,
                start + (Vector2)(Quaternion.Euler(0, 0, to) * Vector2.right) * values.SweepRange);
            var hit = new HashSet<模块_基本属性>(); float elapsed = 0, previous = from;
            while (Alive && elapsed < values.SweepDurationSeconds)
            {
                Actor.MotionPause(Actor.Controlled);
                if (Actor.Controlled) { yield return null; continue; }
                elapsed += Time.deltaTime;
                float angle = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / values.SweepDurationSeconds));
                int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(angle - previous)));
                for (int i = 0; i <= steps; i++)
                {
                    Vector2 ray = Quaternion.Euler(0, 0, Mathf.Lerp(previous, angle, (float)i / steps)) * Vector2.right;
                    Vector2 end = ThemeCastleAttackGeometry.StopAtPillar(scene, start, ray, values.SweepRange, source);
                    ThemeCastleAttackGeometry.Line(owner.Actor, start, end, values.SweepWidth, values.SweepFinalDamage, hit);
                }
                previous = angle; yield return null;
            }
            if (Alive) Actor.MotionPause(false);
        }
        public void 死亡触发() { StopAllCoroutines(); owner?.KnotDied(); }
    }

    internal static class ThemeCastleAttackGeometry
    {
        internal static void Line(ThemeEnemyActor actor, Vector2 start, Vector2 end, float width, float damage, HashSet<模块_基本属性> hit)
        {
            foreach (var collider in Physics2D.OverlapCapsuleAll((start + end) * .5f,
                new Vector2(width, Vector2.Distance(start, end) + width), CapsuleDirection2D.Vertical,
                Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg - 90))
            {
                var target = collider.GetComponentInParent<模块_基本属性>();
                if (ThemeCombat.Friendly(target) && hit.Add(target)) ThemeCombat.Hit(actor, target, damage, end);
            }
        }

        internal static Vector2 StopAtPillar(ThemeBattleScene scene, Vector2 start, Vector2 direction, float range, ThemeBattlePillar exclude = null)
        {
            float distance = range;
            foreach (var pillar in scene.Pillars)
            {
                if (pillar == exclude || pillar.Broken) continue;
                Vector2 delta = pillar.Center - start; float along = Vector2.Dot(delta, direction);
                float across = delta.sqrMagnitude - along * along;
                if (along <= 0 || across > pillar.Radius * pillar.Radius) continue;
                distance = Mathf.Min(distance, Mathf.Max(0, along - Mathf.Sqrt(pillar.Radius * pillar.Radius - across)));
            }
            return start + direction * distance;
        }
    }

    [HarmonyPatch(typeof(模块_受伤), "受到伤害", new[] { typeof(世界类.伤害数据包) })]
    internal static class ThemeHangingDamage
    {
        private static void Prefix(模块_受伤 __instance, ref 世界类.伤害数据包 sjb)
        { if (__instance.GetComponent<ThemeHangingCount>()?.Fallen == true) sjb = ThemeSharedDamageScope.Copy(sjb, 1.5f); }
    }

    [HarmonyPatch(typeof(模块_受伤), "受到伤害", new[] { typeof(float) })]
    internal static class ThemeHangingScalarDamage
    { private static void Prefix(模块_受伤 __instance, ref float 伤害) { if (__instance.GetComponent<ThemeHangingCount>()?.Fallen == true) 伤害 *= 1.5f; } }

    [HarmonyPatch(typeof(模块_受伤), "受到伤害", new[] { typeof(模块_状态持续效果.状态伤害类) })]
    internal static class ThemeHangingStatusDamage
    {
        private static void Prefix(模块_受伤 __instance, ref 模块_状态持续效果.状态伤害类 sh)
        {
            if (__instance.GetComponent<ThemeHangingCount>()?.Fallen == true)
                sh = new 模块_状态持续效果.状态伤害类 { 状态 = sh.状态, 数值 = sh.数值 * 1.5f };
        }
    }
}
