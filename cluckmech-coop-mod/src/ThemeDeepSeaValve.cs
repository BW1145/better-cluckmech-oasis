using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace CluckmechCoop
{
    public sealed class ThemePumpPressure : MonoBehaviour
    {
        internal readonly List<ThemeDeepSeaValve> Valves = new List<ThemeDeepSeaValve>();
        internal bool Flowing;
        internal float ReadyAt;
        internal bool Ready => !Flowing && Time.time >= ReadyAt;
        private float nextDisplay;
        private void Update()
        {
            if (Time.unscaledTime < nextDisplay) return;
            nextDisplay = Time.unscaledTime + .1f;
            foreach (var valve in Valves) valve.DisplayPressure(Ready, Flowing, Mathf.Max(0, ReadyAt - Time.time));
        }
    }

    public sealed class ThemeDeepSeaValve : ThemeInteractive
    {
        private ThemeBattleScene scene;
        private ThemePumpPressure pressure;
        private DeepSeaValveDefinition definition;
        private int sequence, receivedSequence;
        private readonly Dictionary<uint, int> playerPushes = new Dictionary<uint, int>();

        internal static void Install(ThemeBattleScene scene)
        {
            var shared = scene.gameObject.AddComponent<ThemePumpPressure>();
            foreach (var definition in ThemeDeepSeaData.Valves)
            {
                var root = new GameObject("ThemeValve_" + definition.Id);
                root.transform.SetParent(scene.transform, false); root.transform.position = scene.World(definition.Control);
                var valve = root.AddComponent<ThemeDeepSeaValve>(); valve.scene = scene; valve.pressure = shared;
                valve.definition = definition; shared.Valves.Add(valve);
                valve.Bind(definition.Id, ThemeDeepSeaData.ValveInteractionRadius,
                    definition.Id == "west-valve" ? "E：向左排水" : "E：向右排水");
                valve.ShowWhenUnavailable = true;
                ThemeAssets.Renderer(root.transform, "deepsea.valve", 1.2f, Vector2.zero);
            }
        }

        internal void DisplayPressure(bool ready, bool flowing, float remaining)
        {
            Available = ready;
            SetPrompt(flowing ? "正在排水" : !ready ? "蓄压中 " + Mathf.CeilToInt(remaining) + "秒" :
                definition.Id == "west-valve" ? "E：向左排水" : "E：向右排水");
        }

        internal override void TryUse(模块_基本属性 player)
        {
            if (Plugin.Role == 2 || !CanUse(player) || !pressure.Ready) return;
            pressure.Flowing = true;
            Broadcast(new ThemeMechanismMessage { Sequence = ++sequence, Seconds = ThemeDeepSeaData.ValveWarningSeconds });
        }

        internal override void Apply(ThemeMechanismMessage update)
        {
            if (update.Action == "push")
            {
                if (Plugin.Role != 2 || playerPushes.TryGetValue(update.Source, out int previous) && previous >= update.Sequence) return;
                if (!NetworkClient.spawned.TryGetValue(update.Source, out var identity)) return;
                var health = identity.GetComponent<模块_基本属性>();
                if (health == null || (int)health.所属玩家 != Plugin.Role || !health.单位定位列表.Contains(模块_基本属性.单位定位枚举.玩家)) return;
                playerPushes[update.Source] = update.Sequence;
                ThemePushMotion.StartPush(health, scene, new Vector2(update.X, update.Y), update.Seconds);
                return;
            }
            if (update.Sequence <= receivedSequence) return;
            receivedSequence = update.Sequence;
            if (update.Value == 1)
            {
                pressure.Flowing = false; pressure.ReadyAt = Time.time + update.Seconds;
                return;
            }
            pressure.Flowing = true;
            StartCoroutine(Flow(update.Seconds));
        }

        private IEnumerator Flow(float warningSeconds)
        {
            Vector2 start = scene.World(definition.FlowStart), end = scene.World(definition.FlowEnd);
            Vector2 direction = (end - start).normalized;
            ThemeVisual.Line(start, end, ThemeDeepSeaData.ValveFlowWidth, warningSeconds);
            yield return new WaitForSeconds(warningSeconds);
            var jet = ThemeVisual.Effect("deepsea.fx.flow.", 4, (start + end) * .5f,
                Vector2.Distance(start, end), ThemeDeepSeaData.ValveFlowSeconds, scene.transform);
            jet.Angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            jet.SetSize(Vector2.Distance(start, end), ThemeDeepSeaData.ValveFlowWidth);
            jet.FramesPerSecond = 12;
            ThemeVisual.Effect("deepsea.fx.splash.", 4, end, 3.5f, ThemeDeepSeaData.ValveFlowSeconds, scene.transform);
            var pushed = new HashSet<GameObject>();
            float until = Time.time + ThemeDeepSeaData.ValveFlowSeconds;
            while (Time.time < until)
            {
                if (Plugin.Role != 2)
                    foreach (var collider in Physics2D.OverlapBoxAll((start + end) * .5f,
                        new Vector2(Vector2.Distance(start, end), ThemeDeepSeaData.ValveFlowWidth),
                        Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg))
                    {
                        var unit = collider.GetComponentInParent<模块_基本属性>();
                        if (!Eligible(unit) || pushed.Contains(unit.gameObject)) continue;
                        var footprint = ThemePushMotion.Footprint(unit.gameObject);
                        var stop = Project(unit, direction, footprint);
                        if ((stop - (Vector2)unit.transform.position).sqrMagnitude < .01f) continue;
                        pushed.Add(unit.gameObject);
                        ThemePushMotion.StartPush(unit, scene, stop, ThemeDeepSeaData.ValveFlowSeconds);
                        if (Plugin.Role == 1 && unit.单位定位列表.Contains(模块_基本属性.单位定位枚举.玩家))
                        {
                            var update = new ThemeMechanismMessage { Token = ModuleSettings.NodeKey, Id = Id,
                                Action = "push", Sequence = ++sequence, Source = NetworkObjects.Id(unit.gameObject),
                                X = stop.x, Y = stop.y, Seconds = ThemeDeepSeaData.ValveFlowSeconds };
                            Plugin.Network.Broadcast(new SessionMessage { Type = "theme-mechanism", Mechanism = update });
                        }
                    }
                yield return new WaitForSeconds(.05f);
            }
            if (Plugin.Role != 2)
                Broadcast(new ThemeMechanismMessage { Sequence = ++sequence, Value = 1, Seconds = ThemeDeepSeaData.ValveSharedRechargeSeconds });
            else
            {
                pressure.Flowing = false; pressure.ReadyAt = Time.time + ThemeDeepSeaData.ValveSharedRechargeSeconds;
            }
        }

        private static bool Eligible(模块_基本属性 unit)
        {
            if (unit == null || !unit.gameObject.activeInHierarchy || unit.当前生命值 <= 0 || unit.免疫击退 ||
                unit.GetComponent<ThemeEnemyActor>()?.VisibleToUnits == false) return false;
            if (unit.单位类型 != 模块_基本属性.单位类型枚举.地面单位 && unit.单位类型 != 模块_基本属性.单位类型枚举.地面友方单位 &&
                unit.单位类型 != 模块_基本属性.单位类型枚举.玩家地面单位) return false;
            var movement = unit.GetComponent<模块_移动属性>();
            return movement != null && movement.移动速度 > 0 && unit.GetComponent<模块_移动>()?.是否被击退 != true;
        }

        private Vector2 Project(模块_基本属性 unit, Vector2 direction, Bounds footprint)
        {
            Vector2 from = unit.transform.position, stop = from;
            Vector2 offset = (Vector2)footprint.center - from;
            for (float distance = .15f; distance <= ThemeDeepSeaData.ValveMaximumPushDistance + .15f; distance += .15f)
            {
                var point = from + direction * Mathf.Min(distance, ThemeDeepSeaData.ValveMaximumPushDistance);
                if (!scene.ValidMovement(point + offset, footprint.size, unit.gameObject)) break;
                stop = point;
            }
            return stop;
        }
    }

    public sealed class ThemePushMotion : MonoBehaviour
    {
        private ThemeBattleScene scene;
        private 模块_基本属性 health;
        private 模块_移动 navigation;
        private 模块_玩家_移动 playerMovement;
        private ThemeEnemyActor actor;
        private Vector2 from, to, previous, footprintSize, footprintOffset;
        private float started, seconds;
        private bool playerCouldMove, restored;

        internal static Bounds Footprint(GameObject unit)
        {
            bool found = false; var result = new Bounds(unit.transform.position, Vector3.one * .5f);
            foreach (var collider in unit.GetComponents<Collider2D>())
            {
                if (!collider.enabled || collider.isTrigger) continue;
                if (!found) { result = collider.bounds; found = true; }
                else result.Encapsulate(collider.bounds);
            }
            return result;
        }

        internal static void StartPush(模块_基本属性 unit, ThemeBattleScene scene, Vector2 target, float seconds)
        {
            if (unit == null || unit.免疫击退 || unit.GetComponent<ThemePushMotion>() != null) return;
            var motion = unit.gameObject.AddComponent<ThemePushMotion>();
            motion.scene = scene; motion.health = unit; motion.from = motion.previous = unit.transform.position;
            motion.to = target; motion.seconds = Mathf.Max(.01f, seconds); motion.started = Time.time;
            var footprint = Footprint(unit.gameObject); motion.footprintSize = footprint.size;
            motion.footprintOffset = (Vector2)footprint.center - motion.from;
            motion.navigation = unit.GetComponent<模块_移动>();
            if (motion.navigation != null)
            {
                motion.navigation.立刻停止(); motion.navigation.是否被击退 = true;
                motion.navigation.击退计时器 = motion.seconds + .1f;
            }
            motion.actor = unit.GetComponent<ThemeEnemyActor>();
            if (motion.actor != null) { motion.actor.ExternalMovementHolds++; motion.actor.Hold(); }
            motion.playerMovement = unit.GetComponent<模块_玩家_移动>();
            if (motion.playerMovement != null)
            { motion.playerCouldMove = motion.playerMovement.是否移动; motion.playerMovement.是否移动 = false; }
            var body = unit.GetComponent<Rigidbody2D>(); if (body != null) body.velocity = Vector2.zero;
        }

        private void LateUpdate()
        {
            if (scene == null || health == null || health.当前生命值 <= 0) { Restore(); Destroy(this); return; }
            float fraction = Mathf.Clamp01((Time.time - started) / seconds);
            var point = Vector2.Lerp(from, to, Mathf.SmoothStep(0, 1, fraction));
            if (!scene.ValidMovement(point + footprintOffset, footprintSize, gameObject))
            { Move(previous); Restore(); Destroy(this); return; }
            Move(point); previous = point;
            if (fraction >= 1) { Restore(); Destroy(this); }
        }

        private void Move(Vector2 point)
        { transform.position = new Vector3(point.x, point.y, transform.position.z); }
        private void Restore()
        {
            if (restored) return; restored = true;
            if (navigation != null) { navigation.是否被击退 = false; navigation.击退计时器 = 0; }
            if (actor != null) actor.ExternalMovementHolds = Mathf.Max(0, actor.ExternalMovementHolds - 1);
            if (playerMovement != null) playerMovement.是否移动 = playerCouldMove;
        }
        private void OnDestroy() => Restore();
    }
}
