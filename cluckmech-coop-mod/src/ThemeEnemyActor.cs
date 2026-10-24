using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CluckmechCoop
{
    internal sealed class ThemeActorMessage
    {
        public uint Unit, Target, Sequence;
        public string Action;
        public float X, Y, EndX, EndY, Duration, Width, FacingX, FacingY, Elapsed;
        public bool Valid => Unit != 0 && !string.IsNullOrEmpty(Action) && Action.Length <= 40 &&
            NetworkValues.Finite(X) && NetworkValues.Finite(Y) && NetworkValues.Finite(EndX) &&
            NetworkValues.Finite(EndY) && NetworkValues.Finite(Duration) && Duration >= 0 && Duration <= 60 &&
            NetworkValues.Finite(Width) && Width >= 0 && Width <= 40 && NetworkValues.Finite(FacingX) && NetworkValues.Finite(FacingY) &&
            NetworkValues.Finite(Elapsed) && Elapsed >= 0 && Elapsed <= 60;
    }

    public sealed class ThemeEnemyActor : MonoBehaviour
    {
        internal static readonly List<ThemeEnemyActor> Active = new List<ThemeEnemyActor>();
        internal static event Action<ThemeEnemyActor> Ready;
        private static readonly Dictionary<ThemeEnemyActor, ThemeEnemyActor> pours = new Dictionary<ThemeEnemyActor, ThemeEnemyActor>();
        [SerializeField] internal string UnitId;
        internal ThemeEnemyStats Definition;
        internal 模块_基本属性 Stats;
        internal 模块_攻击属性 Attack;
        internal 模块_移动属性 Movement;
        internal 模块_移动 Navigation;
        internal ThemeCountActor SupplyBoss;
        internal int WineStation;
        internal bool WineRecipient, VisibleToUnits = true;
        internal float WineHealFraction = ThemeCastleData.OrdinaryWineHealFraction;
        internal Func<Vector2, Vector2> WineApproachPoint;
        internal Vector2 Facing = Vector2.down;
        internal bool Dead, MovementHeld, SuppressDefaultCombat;
        internal int ExternalMovementHolds;
        internal bool Alive => !Dead && Stats != null && Stats.当前生命值 > 0;
        internal bool Controlled => Navigation != null && Navigation.是否被击退 || States != null &&
            (States.查询状态(攻击类型与状态数据库.状态枚举.眩晕) || States.查询状态(攻击类型与状态数据库.状态枚举.死亡过程));
        internal bool CanAct => Alive && !Controlled && ExternalMovementHolds == 0 && Attack.当前是否能攻击;
        internal 模块_状态属性 States;
        internal string Action = "idle";
        internal bool ActionPaused;
        internal float ActionElapsed => ActionPaused ? pausedElapsed : Mathf.Max(0, Time.time - actionStarted);
        internal float ActionProgress => actionDuration <= 0 ? 0 : Mathf.Clamp01((ActionPaused ? pausedElapsed : Time.time - actionStarted) / actionDuration);
        internal ThemeActorMessage LastAction;
        internal event Action<ThemeActorMessage> ActionChanged;
        internal event Action<IReadOnlyList<GameObject>> Fired;
        private float actionStarted, actionDuration, pausedElapsed, nextAttack, nextSearch;
        private uint sequence, receivedSequence;
        private 模块_攻击 nativeAttack;
        private 模块_基本属性 combatTarget;
        private 模块_基本属性 pursuitTarget;
        private 模块_仇恨 threat;
        private float nextTargetSearch;
        private Transform destination, aim;
        private GameObject baseUnit;

        private void Awake()
        {
            Stats = GetComponent<模块_基本属性>(); Attack = GetComponent<模块_攻击属性>();
            Movement = GetComponent<模块_移动属性>(); Navigation = GetComponent<模块_移动>();
            States = GetComponent<模块_状态属性>(); Definition = ThemeEnemyRegistry.Find(UnitId);
            threat = GetComponent<模块_仇恨>();
            WineRecipient = UnitId == ThemeCastleData.Knight || UnitId == ThemeEldritchData.ShadowKnight || ThemeEnemyRegistry.IsLeader(UnitId);
        }

        private IEnumerator Start()
        {
            // Native difficulty, health, hurt and navigation Start all initialize before actions begin.
            yield return null;
            if (!Alive) yield break;
            Definition = ThemeEnemyRegistry.Find(UnitId);
            nativeAttack = GetComponent<模块_攻击>(); nativeAttack.初始化();
            nativeAttack.CancelInvoke(); nativeAttack.StopAllCoroutines(); nativeAttack.enabled = false;
            destination = new GameObject("ThemeDestination").transform;
            aim = new GameObject("ThemeAim").transform;
            baseUnit = Plugin.Spawner?.GetComponent<yy引用实例数据库>()?.基地车;
            Active.Add(this);
            Ready?.Invoke(this);
            if (Plugin.Role == 2) yield break;
            if (SuppressDefaultCombat) yield break;
            if (UnitId == ThemeCastleData.Count || UnitId == ThemeEldritchData.HangingCount) yield break;
            yield return DefaultRoutine();
        }

        internal IEnumerator DefaultRoutine()
        {
            if (UnitId == ThemeCastleData.WineBat || UnitId == ThemeCastleData.BossWineBat) yield return Wine();
            else if (UnitId == ThemeCastleData.FlowerGhost) yield return Flower();
            else if (UnitId == ThemeCastleData.Coffin) yield return Coffin();
            if (Alive) yield return Fight();
        }

        private void Update()
        {
            if (!Alive || Movement == null) return;
            if (Plugin.Role == 2 && Action == "walk") Face(Movement.目标坐标 - (Vector2)transform.position);
        }

        internal void Face(Vector2 direction)
        { if (direction.sqrMagnitude > 0.001f) Facing = direction.normalized; }

        internal void MoveTo(Vector2 point)
        {
            if (destination == null) return;
            if (ExternalMovementHolds > 0) { Hold(); return; }
            destination.position = point; Movement.前进副目标 = destination;
            MovementHeld = false;
            Navigation.移动目标赋值(point);
            Face(point - (Vector2)transform.position);
        }

        internal void Hold()
        {
            MovementHeld = true; Navigation?.立刻停止();
            var body = GetComponent<Rigidbody2D>();
            if (body != null && Navigation?.是否被击退 != true) body.velocity = Vector2.zero;
        }

        internal IEnumerator Pause(float seconds)
        {
            float elapsed = 0;
            while (Alive && elapsed < seconds)
            {
                MotionPause(!CanAct);
                if (CanAct) elapsed += Time.deltaTime;
                else Hold();
                yield return null;
            }
            if (Alive) MotionPause(false);
        }

        internal void MotionPause(bool pause)
        {
            if (ActionPaused == pause) return;
            float elapsed = pause ? Mathf.Max(0, Time.time - actionStarted) : pausedElapsed;
            var message = new ThemeActorMessage { Unit = NetworkObjects.Id(gameObject), Sequence = ++sequence,
                Action = pause ? "motion-pause" : "motion-resume", X = transform.position.x, Y = transform.position.y,
                Duration = actionDuration, Elapsed = Mathf.Min(60, elapsed), FacingX = Facing.x, FacingY = Facing.y };
            Play(message);
            if (Plugin.Role == 1) Plugin.Network.Broadcast(new SessionMessage { Type = "theme-actor", Token = Plugin.Combat.Token, Theme = message });
        }

        internal void Emit(string action, Vector2 point, float duration = 0, float width = 0, Vector2 end = default, uint target = 0)
        {
            sequence = Math.Max(sequence, receivedSequence);
            var message = new ThemeActorMessage { Unit = NetworkObjects.Id(gameObject), Sequence = ++sequence,
                Action = action, X = point.x, Y = point.y, EndX = end.x, EndY = end.y,
                Duration = duration, Width = width, Target = target, FacingX = Facing.x, FacingY = Facing.y };
            Play(message);
            if (Plugin.Role == 1) Plugin.Network.Broadcast(new SessionMessage {
                Type = "theme-actor", Token = Plugin.Combat.Token, Theme = message
            });
        }

        internal void Play(ThemeActorMessage message)
        {
            if (message.Sequence <= receivedSequence && receivedSequence != 0) return;
            receivedSequence = message.Sequence;
            if (message.Action == "motion-pause" || message.Action == "motion-resume")
            {
                ActionPaused = message.Action == "motion-pause"; pausedElapsed = message.Elapsed;
                if (!ActionPaused) actionStarted = Time.time - message.Elapsed;
                ActionChanged?.Invoke(message); return;
            }
            if (message.Action == "count-supply" || message.Action.StartsWith("shadow-", StringComparison.Ordinal) ||
                message.Action.StartsWith("shared-", StringComparison.Ordinal) ||
                message.Action.StartsWith("bind-", StringComparison.Ordinal) || message.Action == "native-rage")
            {
                if (message.Action == "native-rage") ThemeDeepSeaCombat.ReceiveNativeRage(this, message);
                ActionChanged?.Invoke(message); return;
            }
            LastAction = message; Action = message.Action;
            ActionPaused = false;
            actionStarted = Time.time; actionDuration = message.Duration;
            Face(new Vector2(message.FacingX, message.FacingY));
            ActionChanged?.Invoke(message);
            if (Plugin.Role == 2 && message.Action == "shot" && nativeAttack != null)
                NativeFire(new Vector2(message.EndX, message.EndY), ThemeEnemyRegistry.VolleyCount(UnitId));
            if (Plugin.Role == 2 && message.Action == "valve-shot" && nativeAttack != null)
                NativeFireScaled(new Vector2(message.EndX, message.EndY), ThemeDeepSeaData.ValveAnemoneParameters.FinalProjectileDamage / Definition.FinalDamage);
        }

        private static GameObject Resolve(uint id) => Mirror.NetworkClient.spawned.TryGetValue(id, out var identity) ? identity.gameObject : null;

        internal static void Receive(ThemeActorMessage message)
        {
            if (!Plugin.InCombat || !message.Valid) return;
            if (Resolve(message.Unit)?.GetComponent<ThemeEnemyActor>() is ThemeEnemyActor actor && actor.nativeAttack != null)
                actor.Play(message);
            else Plugin.Instance.StartCoroutine(WaitForActor(message));
        }

        private static IEnumerator WaitForActor(ThemeActorMessage message)
        {
            var scene = SceneLifecycle.Current; float end = Time.time + 2;
            while (SceneLifecycle.IsCurrent(scene) && Time.time < end)
            {
                var actor = Resolve(message.Unit)?.GetComponent<ThemeEnemyActor>();
                if (actor != null && actor.nativeAttack != null) { actor.Play(message); yield break; }
                yield return null;
            }
        }

        internal GameObject SelectTarget(float radius)
        {
            GameObject selected = null; float distance = radius * radius;
            foreach (var target in 世界事件系统.场上所有单位)
            {
                if (!ThemeCombat.Friendly(target) || target.当前是否无敌) continue;
                float value = ((Vector2)target.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (value > distance) continue;
                selected = target.gameObject; distance = value;
            }
            return selected;
        }

        private IEnumerator Fight()
        {
            while (Alive)
            {
                if (!CanAct) { Hold(); yield return null; continue; }
                if (Time.time >= nextTargetSearch || combatTarget != null && !ThemeCombat.Friendly(combatTarget))
                {
                    var hated = threat?.运算最大仇恨目标();
                    pursuitTarget = hated == null ? null : hated.GetComponent<模块_基本属性>();
                    if (!ThemeCombat.Friendly(pursuitTarget) || pursuitTarget.当前是否无敌) pursuitTarget = null;
                    var selected = pursuitTarget != null && Vector2.Distance(transform.position, pursuitTarget.transform.position) <= Attack.当前攻击范围 ?
                        pursuitTarget.gameObject : SelectTarget(Attack.当前攻击范围);
                    combatTarget = selected == null ? null : selected.GetComponent<模块_基本属性>();
                    nextTargetSearch = Time.time + 0.25f;
                }
                var target = combatTarget == null ? null : combatTarget.gameObject;
                if (target != null && Vector2.Distance(transform.position, target.transform.position) > Attack.当前攻击范围 + 0.1f)
                { target = null; combatTarget = null; }
                if (target == null)
                {
                    if (baseUnit != null && Time.time >= nextSearch)
                    {
                        nextSearch = Time.time + 0.25f;
                        MoveTo(ThemeCombat.Friendly(pursuitTarget) ? pursuitTarget.transform.position : baseUnit.transform.position);
                        if (Action != "walk") Emit("walk", transform.position);
                    }
                    yield return null; continue;
                }
                Face((Vector2)target.transform.position - (Vector2)transform.position);
                Hold();
                if (Time.time < nextAttack) { yield return null; continue; }
                float rate = Attack.当前攻击速度;
                if (rate <= 0) { yield return null; continue; }
                nextAttack = Time.time + 1 / rate;
                Vector2 point = target.transform.position;
                Emit(ThemeEnemyRegistry.IsCrossbow(UnitId) ? "bow-warning" : "attack", transform.position,
                    Definition.AttackWindupSeconds, 0, point, NetworkObjects.Id(target));
                yield return Pause(Definition.AttackWindupSeconds);
                if (!Alive) yield break;
                if (target != null) point = target.transform.position;
                Emit("shot", transform.position, 0.2f, 0, point, NetworkObjects.Id(target));
                NativeFire(point, ThemeEnemyRegistry.VolleyCount(UnitId));
            }
        }

        internal void NativeFireScaled(Vector2 point, float scale) => NativeFire(point, 1, scale);

        internal void NativeFire(Vector2 point, int arrows, float damageScale = 1)
        {
            if (nativeAttack == null || aim == null) return;
            aim.position = point; Attack.攻击目标 = aim;
            var hitTargets = new HashSet<GameObject>();
            var projectiles = new List<GameObject>(arrows);
            for (int i = 0; i < arrows; i++)
            {
                nativeAttack.攻击();
                var projectile = nativeAttack.生成的子弹;
                if (UnitId.StartsWith("deepsea.", StringComparison.Ordinal) && ThemeEnemyRegistry.IsRanged(UnitId))
                {
                    foreach (var renderer in projectile.GetComponentsInChildren<SpriteRenderer>(true)) renderer.enabled = false;
                    ThemeAssets.Renderer(projectile.transform, "deepsea.fx.projectile.0", .5f, Vector2.zero);
                }
                projectile.transform.Rotate(0, 0, (i - (arrows - 1) * 0.5f) * 12);
                if (damageScale != 1)
                    foreach (var damage in projectile.GetComponentsInChildren<模块_伤害精灵>()) damage.伤害数据.攻击伤害 *= damageScale;
                if (arrows > 1) projectile.AddComponent<ThemeVolleyProjectile>().Targets = hitTargets;
                projectiles.Add(projectile);
            }
            Fired?.Invoke(projectiles);
            gameObject.SendMessage("攻击发动", SendMessageOptions.DontRequireReceiver);
        }

        private IEnumerator Flower()
        {
            Hold(); Emit("unfold", transform.position, 0.5f);
            yield return Pause(0.5f);
            var target = ThemeBattleWaves.Current?.NextFlowerTarget();
            Vector2 point = target == null ? (Vector2)transform.position : (Vector2)target.transform.position;
            var scene = ThemeBattleScene.Current;
            if (scene != null) point = scene.ValidLanding(point, new Vector2(0.8f, 0.8f));
            Emit("flower-warning", transform.position, ThemeCastleData.FlowerWarningSeconds, ThemeCastleData.FlowerRadius, point);
            yield return Pause(ThemeCastleData.FlowerWarningSeconds);
            if (!Alive) yield break;
            Vector2 origin = transform.position;
            Emit("dive", origin, 0.3f, ThemeCastleData.FlowerRadius, point);
            yield return Travel(origin, point, 0.3f);
            if (!Alive) yield break;
            ThemeCombat.Area(this, point, ThemeCastleData.FlowerRadius, ThemeCastleData.FlowerFinalDamage);
            Emit("land", point, ThemeCastleData.FlowerLandingPauseSeconds, ThemeCastleData.FlowerRadius);
            yield return Pause(ThemeCastleData.FlowerLandingPauseSeconds);
        }

        private IEnumerator Travel(Vector2 from, Vector2 to, float seconds)
        {
            var body = GetComponent<Rigidbody2D>(); float elapsed = 0;
            while (Alive && elapsed < seconds)
            {
                if (CanAct)
                {
                    elapsed += Time.deltaTime;
                    Vector2 point = Vector2.Lerp(from, to, Mathf.Clamp01(elapsed / seconds));
                    if (body != null) body.MovePosition(point); else transform.position = point;
                }
                yield return null;
            }
        }

        private IEnumerator Coffin()
        {
            while (Alive && baseUnit != null)
            {
                if (CanAct) MoveTo(baseUnit.transform.position);
                var agent = Navigation.寻路脚本;
                if (CanAct && agent != null && !agent.pathPending && (agent.hasPath ?
                    agent.remainingDistance <= ThemeCastleData.CoffinRemainingPathDistance :
                    Vector2.Distance(transform.position, baseUnit.transform.position) <= ThemeCastleData.CoffinRemainingPathDistance)) break;
                yield return new WaitForSeconds(0.25f);
            }
            for (int batch = 0; Alive && batch < ThemeCastleData.CoffinReleaseBatches; batch++)
            {
                Hold(); Emit("coffin-open", transform.position, ThemeCastleData.CoffinOpeningSeconds);
                yield return Pause(ThemeCastleData.CoffinOpeningSeconds);
                if (!Alive) yield break;
                ThemeBattleWaves.SpawnAt(ThemeCastleData.Footman, ThemeCastleData.CoffinReleaseCount, transform.position);
                Emit("coffin-release", transform.position, 0.4f);
                if (batch + 1 < ThemeCastleData.CoffinReleaseBatches)
                {
                    Hold(); yield return Pause(ThemeCastleData.CoffinSecondReleaseDelaySeconds);
                }
            }
            if (Alive) Emit("coffin-empty", transform.position);
        }

        private IEnumerator Wine()
        {
            if (UnitId == ThemeCastleData.BossWineBat)
            {
                while (SupplyBoss == null && Alive) yield return null;
                if (!Alive || SupplyBoss == null) yield break;
                var values = ThemeCastleData.CountParameters;
                var path = transform.position.x < ThemeBattleScene.Current.World(Vector2.zero).x ? values.WestWinePath : values.EastWinePath;
                for (int i = 1; Alive && SupplyBoss.Alive && i < path.Length; i++)
                {
                    Vector2 point = ThemeBattleScene.Current.World(path[i]);
                    point.x += values.WineStationOffsetsX[WineStation % values.WineStationOffsetsX.Length];
                    while (Alive && SupplyBoss.Alive && Vector2.Distance(transform.position, point) > values.WineArrivalTolerance)
                    {
                        if (CanAct) MoveTo(point); else Hold();
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                if (!Alive || !SupplyBoss.Alive) yield break;
                while (Alive && SupplyBoss.Alive && !CanAct) { Hold(); yield return null; }
                if (!Alive || !SupplyBoss.Alive) yield break;
                Hold(); Emit("wine-pour", transform.position, ThemeCastleData.WinePourSeconds, 0,
                    SupplyBoss.transform.position, NetworkObjects.Id(SupplyBoss.gameObject));
                yield return Pause(ThemeCastleData.WinePourSeconds);
                if (!Alive || !SupplyBoss.Alive) yield break;
                SupplyBoss.GetComponent<模块_受伤>().受到治疗(SupplyBoss.Actor.Stats.当前生命值上限 * values.WineHealFraction);
                Emit("wine-empty", transform.position);
                yield break;
            }

            float missingSince = -1;
            while (Alive)
            {
                if (!CanAct) { Hold(); yield return null; continue; }
                var target = WineTarget();
                if (target == null)
                {
                    if (missingSince < 0) missingSince = Time.time;
                    if (Time.time - missingSince >= ThemeCastleData.WineTargetMissingGraceSeconds) yield break;
                    yield return new WaitForSeconds(0.25f); continue;
                }
                missingSince = -1;
                float missing = target.Stats.当前生命值上限 - target.Stats.当前生命值;
                float approach = missing > 0 ? 0.7f : ThemeCastleData.WineFollowDistance;
                Vector2 deliveryPoint = target.WineApproachPoint?.Invoke(transform.position) ?? (Vector2)target.transform.position;
                if (Vector2.Distance(transform.position, deliveryPoint) > approach)
                {
                    if (CanAct) MoveTo(deliveryPoint); else Hold();
                    yield return new WaitForSeconds(0.1f); continue;
                }
                Hold();
                if (missing <= 0 || pours.ContainsKey(target)) { yield return new WaitForSeconds(0.1f); continue; }
                pours[target] = this;
                Emit("wine-pour", transform.position, ThemeCastleData.WinePourSeconds, 0,
                    deliveryPoint, NetworkObjects.Id(target.gameObject));
                yield return Pause(ThemeCastleData.WinePourSeconds);
                if (pours.TryGetValue(target, out var owner) && owner == this) pours.Remove(target);
                if (!Alive) yield break;
                if (!target.Alive || !target.VisibleToUnits || target.Stats.当前生命值 >= target.Stats.当前生命值上限) continue;
                target.GetComponent<模块_受伤>().受到治疗(target.Stats.当前生命值上限 * target.WineHealFraction);
                Emit("wine-empty", transform.position); yield break;
            }
        }

        private ThemeEnemyActor WineTarget()
        {
            ThemeEnemyActor selected = null; float missing = -1; int rank = -1;
            foreach (var actor in Active)
            {
                if (!actor.Alive || !actor.VisibleToUnits || !actor.WineRecipient) continue;
                bool candidateLeader = ThemeEnemyRegistry.IsLeader(actor.UnitId) || actor.UnitId == ThemeEldritchData.BanquetEater;
                float value = actor.Stats.当前生命值上限 - actor.Stats.当前生命值;
                bool reserved = pours.TryGetValue(actor, out var owner) && owner != this;
                int candidateRank = value > 0 && !reserved ? (candidateLeader ? 3 : 2) : candidateLeader ? 1 : 0;
                if (selected == null || candidateRank > rank || candidateRank == rank && value > missing)
                { selected = actor; missing = value; rank = candidateRank; }
            }
            return selected;
        }

        public void 死亡触发()
        {
            if (Dead) return;
            Dead = true; Hold(); StopAllCoroutines();
            if (Action != "death") Emit("death", transform.position, 1);
        }

        private void OnDestroy()
        {
            Active.Remove(this);
            var remove = new List<ThemeEnemyActor>();
            foreach (var pair in pours) if (pair.Key == this || pair.Value == this) remove.Add(pair.Key);
            foreach (var key in remove) pours.Remove(key);
            if (destination != null) Destroy(destination.gameObject);
            if (aim != null) Destroy(aim.gameObject);
        }
    }

}
