using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    // A route owns its entrance queue and reserved full-footprint landing places.
    // The native enemy remains alive and registered throughout travel.
    internal sealed class ThemeTransportRoute
    {
        internal readonly ThemeBattleScene Scene;
        internal readonly EldritchTransportLink Link;
        internal readonly float EnterSeconds, TransitSeconds, ExitSeconds, EntrySpacingSeconds;
        private readonly HashSet<ThemeEnemyTransport> members = new HashSet<ThemeEnemyTransport>();
        private readonly Dictionary<ThemeEnemyTransport, Bounds> reservations = new Dictionary<ThemeEnemyTransport, Bounds>();
        private float nextEntry, closedUntil;
        private bool stopped;
        internal bool Open => !stopped && Time.time >= closedUntil;

        internal ThemeTransportRoute(ThemeBattleScene scene, EldritchTransportLink link,
            float enterSeconds, float transitSeconds, float exitSeconds, float entrySpacingSeconds)
        {
            Scene = scene; Link = link; EnterSeconds = enterSeconds; TransitSeconds = transitSeconds;
            ExitSeconds = exitSeconds; EntrySpacingSeconds = entrySpacingSeconds;
        }

        internal void Assign(GameObject unit)
        {
            if (unit == null || !Open) return;
            var actor = unit.GetComponent<ThemeEnemyActor>();
            if (actor == null || unit.GetComponent<ThemeEnemyTransport>() != null) return;
            actor.SuppressDefaultCombat = true;
            var transport = unit.AddComponent<ThemeEnemyTransport>();
            transport.Configure(this, actor); members.Add(transport);
        }

        internal void Close(float seconds)
        {
            closedUntil = Mathf.Max(closedUntil, Time.time + seconds);
            foreach (var member in new List<ThemeEnemyTransport>(members)) member.ReturnToEntrance();
        }

        internal void Stop()
        {
            stopped = true;
            foreach (var member in new List<ThemeEnemyTransport>(members)) member.ReturnToEntrance();
        }

        internal bool TryEnter(ThemeEnemyTransport member, out Vector2 point)
        {
            point = default;
            if (!Open || Time.time < nextEntry || !FindLanding(Scene.World(Link.Exit), member, out point)) return false;
            reservations[member] = new Bounds(point, member.Footprint);
            nextEntry = Time.time + EntrySpacingSeconds;
            return true;
        }

        internal bool FindLanding(Vector2 center, ThemeEnemyTransport member, out Vector2 point)
        {
            for (int ring = 0; ring <= 8; ring++)
            {
                int count = ring == 0 ? 1 : 20;
                for (int step = 0; step < count; step++)
                {
                    float angle = step * Mathf.PI * 2 / count;
                    point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (ring * 0.5f);
                    if (!Scene.ValidFootprint(point, member.Footprint)) continue;
                    var bounds = new Bounds(point, member.Footprint);
                    bool occupied = false;
                    foreach (var pair in reservations)
                        if (pair.Key != member && pair.Value.Intersects(bounds)) { occupied = true; break; }
                    if (occupied) continue;
                    foreach (var collider in Physics2D.OverlapBoxAll(point, member.Footprint, 0))
                    {
                        var health = collider.GetComponentInParent<模块_基本属性>();
                        if (health != null && health.gameObject != member.gameObject && health.当前生命值 > 0)
                        { occupied = true; break; }
                    }
                    if (!occupied) return true;
                }
            }
            point = default; return false;
        }

        internal void Remove(ThemeEnemyTransport member)
        { members.Remove(member); reservations.Remove(member); }
    }

    public sealed class ThemeEnemyTransport : MonoBehaviour
    {
        internal static event Action<ThemeEnemyActor, bool> VisibilityChanged;
        internal static int HiddenCount;
        internal ThemeEnemyActor Actor;
        internal bool Hidden { get; private set; }
        internal bool Finished { get; private set; }
        internal Vector2 Footprint { get; private set; }
        private ThemeTransportRoute route;
        private bool returning, held, restoreDefault;
        private string originalTag;
        private readonly Dictionary<Collider2D, bool> colliders = new Dictionary<Collider2D, bool>();
        private readonly Dictionary<SpriteRenderer, bool> renderers = new Dictionary<SpriteRenderer, bool>();

        internal void Configure(ThemeTransportRoute owner, ThemeEnemyActor actor)
        { route = owner; Actor = actor; restoreDefault = true; Footprint = Volume(gameObject); }

        private IEnumerator Start()
        {
            if (route == null || Plugin.Role == 2) yield break;
            yield return null; yield return null;
            if (Actor == null || !Actor.Alive) yield break;
            try
            {
                Vector2 entry = route.Scene.World(route.Link.Entry);
                while (Actor.Alive && !returning && route.Open && Vector2.Distance(transform.position, entry) > 0.6f)
                {
                    if (!Actor.CanAct) { Actor.Hold(); yield return null; continue; }
                    var blocker = Actor.SelectTarget(Actor.Attack.当前攻击范围);
                    if (blocker != null)
                    {
                        Actor.Hold(); Actor.Face((Vector2)blocker.transform.position - (Vector2)transform.position);
                        Actor.Emit("attack", transform.position, Actor.Definition.AttackWindupSeconds, 0, blocker.transform.position);
                        yield return Actor.Pause(Actor.Definition.AttackWindupSeconds);
                        if (Actor.Alive && blocker != null) Actor.NativeFire(blocker.transform.position, 1);
                        yield return Actor.Pause(Mathf.Max(0.05f, 1 / Mathf.Max(0.01f, Actor.Attack.当前攻击速度)));
                    }
                    else { Actor.MoveTo(entry); yield return new WaitForSeconds(0.1f); }
                }
                Vector2 landing = default;
                while (Actor.Alive && !returning && route.Open && !route.TryEnter(this, out landing))
                { Actor.Hold(); yield return new WaitForSeconds(0.1f); }
                if (!Actor.Alive || returning || !route.Open) yield break;
                HoldMovement();
                Actor.Emit("transport-in", transform.position, route.EnterSeconds, 0, landing);
                yield return Wait(route.EnterSeconds, true);
                if (!Actor.Alive || returning || !route.Open) yield break;
                Actor.Emit("transport-travel", transform.position, route.TransitSeconds, 0, landing);
                SetHidden(true);
                yield return Wait(route.TransitSeconds, false);
                if (!Actor.Alive) yield break;
                if (returning || !route.Open)
                {
                    while (Actor.Alive && !route.FindLanding(entry, this, out landing)) yield return new WaitForSeconds(0.1f);
                }
                else
                {
                    while (Actor.Alive && !route.FindLanding(returning || !route.Open ? entry : route.Scene.World(route.Link.Exit), this, out landing))
                        yield return new WaitForSeconds(0.1f);
                }
                if (!Actor.Alive) yield break;
                Teleport(landing); SetHidden(false);
                Actor.Emit("transport-out", landing, route.ExitSeconds);
                yield return Wait(route.ExitSeconds, true);
            }
            finally
            {
                Finished = true; SetHidden(false); ReleaseMovement(); route.Remove(this);
                if (restoreDefault && route.Scene != null && Actor != null && Actor.Alive && Plugin.Role != 2)
                    Actor.StartCoroutine(Actor.DefaultRoutine());
            }
        }

        private IEnumerator Wait(float seconds, bool controlledPause)
        {
            float elapsed = 0;
            while (Actor.Alive && elapsed < seconds)
            {
                if (!controlledPause || !Actor.Controlled) elapsed += Time.deltaTime;
                Actor.Hold();
                if (returning && !Hidden) yield break;
                yield return null;
            }
        }

        internal void ReturnToEntrance() { returning = true; }

        private void HoldMovement()
        { if (!held) { held = true; Actor.ExternalMovementHolds++; Actor.Hold(); } }
        private void ReleaseMovement()
        { if (held && Actor != null) { held = false; Actor.ExternalMovementHolds = Mathf.Max(0, Actor.ExternalMovementHolds - 1); } }

        private void SetHidden(bool hidden)
        {
            if (Hidden == hidden) return;
            Hidden = hidden;
            HiddenCount = Mathf.Max(0, HiddenCount + (hidden ? 1 : -1));
            bool visible = !hidden && Actor != null && Actor.Alive;
            if (Actor != null) Actor.VisibleToUnits = visible;
            if (hidden)
            {
                originalTag = gameObject.tag; gameObject.tag = "Untagged";
                foreach (var collider in GetComponentsInChildren<Collider2D>(true))
                { colliders[collider] = collider.enabled; collider.enabled = false; }
                foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
                { renderers[renderer] = renderer.enabled; renderer.enabled = false; }
            }
            else
            {
                if (visible)
                {
                    if (originalTag != null) gameObject.tag = originalTag;
                    foreach (var pair in colliders) if (pair.Key != null) pair.Key.enabled = pair.Value;
                    foreach (var pair in renderers) if (pair.Key != null) pair.Key.enabled = pair.Value;
                }
                colliders.Clear(); renderers.Clear();
            }
            VisibilityChanged?.Invoke(Actor, visible);
        }

        private void Teleport(Vector2 point)
        {
            transform.position = new Vector3(point.x, point.y, transform.position.z);
            var body = GetComponent<Rigidbody2D>();
            if (body != null) { body.position = point; body.velocity = Vector2.zero; }
        }

        internal void Observe(ThemeEnemyActor actor, ThemeActorMessage message)
        {
            Actor = actor;
            if (Plugin.Role != 2) return;
            if (message.Action == "transport-in") HoldMovement();
            else if (message.Action == "transport-travel") { HoldMovement(); SetHidden(true); }
            else if (message.Action == "transport-out")
            { Teleport(new Vector2(message.X, message.Y)); SetHidden(false); ReleaseMovement(); Finished = true; }
        }

        private static Vector2 Volume(GameObject unit)
        {
            Bounds bounds = default; bool found = false;
            foreach (var collider in unit.GetComponents<Collider2D>())
            {
                if (!collider.enabled || collider.isTrigger) continue;
                if (found) bounds.Encapsulate(collider.bounds); else { bounds = collider.bounds; found = true; }
            }
            return found ? new Vector2(Mathf.Max(.3f, bounds.size.x), Mathf.Max(.3f, bounds.size.y)) : Vector2.one * .8f;
        }

        private void OnDestroy()
        { restoreDefault = false; SetHidden(false); ReleaseMovement(); route?.Remove(this); }
    }

    [HarmonyPatch(typeof(ThemeEnemyActor), "Play")]
    internal static class ThemeTransportPresentation
    {
        private static void Postfix(ThemeEnemyActor __instance, ThemeActorMessage message)
        {
            if (!message.Action.StartsWith("transport-", StringComparison.Ordinal)) return;
            var transport = __instance.GetComponent<ThemeEnemyTransport>();
            if (transport == null) transport = __instance.gameObject.AddComponent<ThemeEnemyTransport>();
            transport.Observe(__instance, message);
        }
    }

    [HarmonyPatch(typeof(模块_受伤), "受到伤害", new[] { typeof(世界类.伤害数据包) })]
    internal static class ThemeTransitDirectHit
    { private static bool Prefix(模块_受伤 __instance) => ThemeEnemyTransport.HiddenCount == 0 || __instance.GetComponent<ThemeEnemyTransport>()?.Hidden != true; }

    [HarmonyPatch(typeof(模块_受伤), "受到伤害", new[] { typeof(float) })]
    internal static class ThemeTransitScalarHit
    { private static bool Prefix(模块_受伤 __instance) => ThemeEnemyTransport.HiddenCount == 0 || __instance.GetComponent<ThemeEnemyTransport>()?.Hidden != true; }
}
