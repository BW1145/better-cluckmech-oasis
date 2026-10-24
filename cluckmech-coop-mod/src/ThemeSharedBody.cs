using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    // A native projectile/area is one attack. Explicit area loops use Begin for one tick.
    // Damage packets may be reused by later attacks and never serve as lifetime identities.
    internal static class ThemeSharedDamageScope
    {
        private sealed class Stamp
        {
            internal readonly HashSet<int> Bodies = new HashSet<int>();
            internal readonly HashSet<(int Body, 模块_状态属性.状态类 State)> States = new HashSet<(int, 模块_状态属性.状态类)>();
        }
        private sealed class Lease : IDisposable
        {
            private readonly Stamp previous;
            private bool disposed;
            internal Lease(Stamp stamp) { previous = current; current = stamp; }
            public void Dispose() { if (!disposed) { current = previous; disposed = true; } }
        }
        private static readonly ConditionalWeakTable<Component, Stamp> attacks = new ConditionalWeakTable<Component, Stamp>();
        [ThreadStatic] private static Stamp current;
        [ThreadStatic] internal static int ForwardingBody;

        internal static IDisposable Begin() => ThemeSharedBodyPart.ActiveCount == 0 ? null : new Lease(new Stamp());
        internal static IDisposable Native(Component source) => ThemeSharedBodyPart.ActiveCount == 0 ? null : new Lease(attacks.GetValue(source, _ => new Stamp()));
        internal static bool Accept(int body) => current == null || current.Bodies.Add(body);
        internal static bool Seen(int body) => current != null && current.Bodies.Contains(body);
        internal static bool AcceptState(int body, 模块_状态属性.状态类 state) => current == null || current.States.Add((body, state));
        internal static 模块_基本属性 CanonicalHealth(模块_基本属性 health)
        {
            if (ThemeSharedBodyPart.ActiveCount == 0) return health;
            var part = health == null ? null : health.GetComponent<ThemeSharedBodyPart>();
            return part != null && part.MergeStatesWithOwner && part.Owner != null ? part.Owner.Stats : health;
        }
        internal static 模块_基本属性 CanonicalStateTarget(模块_基本属性 health) => CanonicalHealth(health);

        internal static 世界类.伤害数据包 Copy(世界类.伤害数据包 source, float multiplier = 1)
        {
            var result = new 世界类.伤害数据包 {
                攻击者 = source.攻击者, 攻击目标 = source.攻击目标, 攻击坐标 = source.攻击坐标,
                所属阵营 = source.所属阵营, 攻击伤害 = source.攻击伤害 * multiplier,
                是否制导 = source.是否制导, 暴击倍率 = source.暴击倍率, 是否暴击 = source.是否暴击,
                是否产生仇恨 = source.是否产生仇恨, 穿透数量 = source.穿透数量, 穿透伤害加成 = source.穿透伤害加成,
                是否击退 = source.是否击退, 是否爆炸 = source.是否爆炸, 击退类型 = source.击退类型, 击退参数 = source.击退参数
            };
            foreach (var state in source.造成状态) result.造成状态.Add(CopyState(state));
            OtherworldDamage.Remember(result, OtherworldDamage.Space(source));
            return result;
        }

        internal static 模块_状态属性.状态类 CopyState(模块_状态属性.状态类 state)
        {
            var value = state.加成属性;
            return new 模块_状态属性.状态类 {
                攻击者 = state.攻击者, 状态类型 = state.状态类型, 剩余时间 = state.剩余时间,
                加成属性 = new 模块_攻击详细属性.状态属性单元 {
                    默认持续时间 = value.默认持续时间, 默认属性效果 = value.默认属性效果,
                    变化持续时间 = value.变化持续时间, 变化百分比持续时间 = value.变化百分比持续时间,
                    变化属性效果 = value.变化属性效果, 变化百分比属性效果 = value.变化百分比属性效果,
                    当前持续时间 = value.当前持续时间, 当前属性效果 = value.当前属性效果
                }
            };
        }
    }

    public sealed class ThemeSharedBodyPart : MonoBehaviour
    {
        internal static int ActiveCount;
        internal ThemeEnemyActor Owner;
        internal int BodyId;
        internal bool MergeStatesWithOwner = true;
        internal float DamageMultiplier = 1;
        internal event Action<float> Damaged;
        internal bool IsCore => Owner != null && Owner.gameObject == gameObject;

        private void Awake() { ActiveCount++; }
        private void OnDestroy() { ActiveCount = Mathf.Max(0, ActiveCount - 1); }

        internal void Bind(ThemeEnemyActor owner, int partIndex, bool mergeBody)
        {
            Owner = owner;
            MergeStatesWithOwner = mergeBody;
            int identity = unchecked((int)NetworkObjects.Id(owner.gameObject));
            if (identity == 0) identity = owner.GetInstanceID();
            BodyId = mergeBody ? identity : unchecked(identity * 31 + partIndex + 1);
            if (Plugin.Role == 1)
            {
                var actor = GetComponent<ThemeEnemyActor>();
                Vector2 facing = actor.Facing;
                actor.Emit("shared-body", transform.position, mergeBody ? 1 : 0, partIndex,
                    target: NetworkObjects.Id(owner.gameObject));
                actor.Face(facing);
            }
        }

        private void LateUpdate()
        {
            if (Owner == null || IsCore) return;
            var stats = GetComponent<模块_基本属性>();
            if (stats == null || Owner.Stats == null) return;
            stats.当前生命值上限 = Owner.Stats.当前生命值上限;
            stats.当前生命值 = Owner.Stats.当前生命值;
        }

        internal bool Accept => Owner != null && Owner.Alive &&
            (ThemeSharedDamageScope.ForwardingBody == BodyId || ThemeSharedDamageScope.Accept(BodyId));

        internal void Forward(世界类.伤害数据包 damage)
        {
            int previous = ThemeSharedDamageScope.ForwardingBody;
            var core = Owner.GetComponent<ThemeSharedBodyPart>();
            ThemeSharedDamageScope.ForwardingBody = core == null ? BodyId : core.BodyId;
            float before = Owner.Stats.当前生命值;
            try
            {
                var packet = ThemeSharedDamageScope.Copy(damage, DamageMultiplier);
                if (!MergeStatesWithOwner) packet.造成状态.Clear();
                Owner.GetComponent<模块_受伤>().受到伤害(packet);
                if (!MergeStatesWithOwner)
                    foreach (var state in damage.造成状态) GetComponent<模块_状态属性>()?.增加状态(ThemeSharedDamageScope.CopyState(state));
            }
            finally { ThemeSharedDamageScope.ForwardingBody = previous; }
            Damaged?.Invoke(Mathf.Max(0, before - Owner.Stats.当前生命值));
        }

        internal void Forward(float damage)
        {
            int previous = ThemeSharedDamageScope.ForwardingBody;
            var core = Owner.GetComponent<ThemeSharedBodyPart>();
            ThemeSharedDamageScope.ForwardingBody = core == null ? BodyId : core.BodyId;
            float before = Owner.Stats.当前生命值;
            try { Owner.GetComponent<模块_受伤>().受到伤害(damage * DamageMultiplier); }
            finally { ThemeSharedDamageScope.ForwardingBody = previous; }
            Damaged?.Invoke(Mathf.Max(0, before - Owner.Stats.当前生命值));
        }

        internal void StatusHit(模块_状态持续效果.状态伤害类 damage)
        {
            if (Owner == null || !Owner.Alive) return;
            float before = Owner.Stats.当前生命值;
            int previous = ThemeSharedDamageScope.ForwardingBody;
            var core = Owner.GetComponent<ThemeSharedBodyPart>();
            ThemeSharedDamageScope.ForwardingBody = core == null ? BodyId : core.BodyId;
            try { Owner.GetComponent<模块_受伤>().受到伤害(new 模块_状态持续效果.状态伤害类 {
                状态 = damage.状态, 数值 = damage.数值 * DamageMultiplier
            }); }
            finally { ThemeSharedDamageScope.ForwardingBody = previous; }
            Damaged?.Invoke(Mathf.Max(0, before - Owner.Stats.当前生命值));
        }

        internal ThemeSharedLocalHit BeginLocalHit()
        {
            var stats = GetComponent<模块_基本属性>();
            stats.当前生命值 = Owner.Stats.当前生命值;
            return new ThemeSharedLocalHit { Part = this, Stats = stats, Before = stats.当前生命值 };
        }

        internal void FinishLocalHit(ThemeSharedLocalHit hit)
        {
            float damage = Mathf.Max(0, hit.Before - hit.Stats.当前生命值);
            if (Owner != null && Owner.Alive && damage > 0)
            {
                Owner.GetComponent<模块_受伤>().实现伤害(damage);
                Owner.Stats.最近攻击者 = hit.Stats.最近攻击者;
                Owner.Stats.最近受到伤害 = damage;
                Damaged?.Invoke(damage);
            }
            if (Owner != null && Owner.Stats != null) hit.Stats.当前生命值 = Owner.Stats.当前生命值;
        }
    }

    internal sealed class ThemeSharedLocalHit
    {
        internal ThemeSharedBodyPart Part;
        internal 模块_基本属性 Stats;
        internal float Before;
    }

    [HarmonyPatch(typeof(ThemeEnemyActor), "Play")]
    internal static class ThemeSharedBodyBinding
    {
        private static void Postfix(ThemeEnemyActor __instance, ThemeActorMessage message)
        {
            if (Plugin.Role != 2 || message.Action != "shared-body") return;
            __instance.StartCoroutine(Bind(__instance, message));
        }
        private static IEnumerator Bind(ThemeEnemyActor actor, ThemeActorMessage message)
        {
            float end = Time.time + 3;
            while (actor != null && Time.time <= end)
            {
                if (Mirror.NetworkClient.spawned.TryGetValue(message.Target, out var identity))
                {
                    var owner = identity.GetComponent<ThemeEnemyActor>();
                    if (owner != null)
                    {
                        var part = actor.GetComponent<ThemeSharedBodyPart>();
                        if (part == null) part = actor.gameObject.AddComponent<ThemeSharedBodyPart>();
                        part.Bind(owner, Mathf.RoundToInt(message.Width), message.Duration > 0);
                        if (owner.UnitId == ThemeEldritchData.WhiteVisitor)
                            ThemeWhiteVisitorActor.SetContactShape(actor, Mathf.RoundToInt(message.Width) == 0 ?
                                ThemeEldritchData.WhiteParameters.ContactRadius : ThemeEldritchData.WhiteParameters.HandRadius);
                        yield break;
                    }
                }
                yield return null;
            }
        }
    }

    [HarmonyPatch(typeof(模块_伤害精灵), "OnTriggerEnter2D")]
    internal static class ThemeProjectileBodyScope
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(模块_伤害精灵 __instance, out IDisposable __state) => __state = ThemeSharedDamageScope.Native(__instance);
        private static Exception Finalizer(Exception __exception, IDisposable __state) { __state?.Dispose(); return __exception; }
    }

    [HarmonyPatch(typeof(模块_范围伤害精灵), "OnTriggerStay2D")]
    internal static class ThemeAreaBodyScope
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(模块_范围伤害精灵 __instance, out IDisposable __state) => __state = ThemeSharedDamageScope.Native(__instance);
        private static Exception Finalizer(Exception __exception, IDisposable __state) { __state?.Dispose(); return __exception; }
    }

    [HarmonyPatch(typeof(模块_伤害精灵), "OnTriggerEnter2D")]
    internal static class ThemeProjectileSharedTarget
    {
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(Collider2D col)
        {
            if (ThemeSharedBodyPart.ActiveCount == 0) return true;
            var part = col.GetComponent<ThemeSharedBodyPart>();
            return part == null || !ThemeSharedDamageScope.Seen(part.BodyId);
        }
    }
    [HarmonyPatch(typeof(模块_范围伤害精灵), "OnTriggerStay2D")]
    internal static class ThemeAreaSharedTarget
    {
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(Collider2D col)
        {
            if (ThemeSharedBodyPart.ActiveCount == 0) return true;
            var part = col.GetComponent<ThemeSharedBodyPart>();
            return part == null || !ThemeSharedDamageScope.Seen(part.BodyId);
        }
    }

    [HarmonyPatch(typeof(模块_受伤), "受到伤害", new[] { typeof(世界类.伤害数据包) })]
    internal static class ThemeSharedPacketHit
    {
        private static bool Prefix(模块_受伤 __instance, ref 世界类.伤害数据包 sjb, out ThemeSharedLocalHit __state)
        {
            __state = null;
            if (ThemeSharedBodyPart.ActiveCount == 0) return true;
            var part = __instance.GetComponent<ThemeSharedBodyPart>();
            if (part == null) return true;
            if (!part.Accept) return false;
            if (part.IsCore)
            {
                if (ThemeSharedDamageScope.ForwardingBody != part.BodyId && part.DamageMultiplier != 1)
                    sjb = ThemeSharedDamageScope.Copy(sjb, part.DamageMultiplier);
                return true;
            }
            if (!part.MergeStatesWithOwner) { __state = part.BeginLocalHit(); return true; }
            part.Forward(sjb); return false;
        }
        private static void Postfix(ThemeSharedLocalHit __state) { __state?.Part.FinishLocalHit(__state); }
    }

    [HarmonyPatch(typeof(模块_受伤), "受到伤害", new[] { typeof(float) })]
    internal static class ThemeSharedScalarHit
    {
        private static bool Prefix(模块_受伤 __instance, ref float 伤害, out ThemeSharedLocalHit __state)
        {
            __state = null;
            if (ThemeSharedBodyPart.ActiveCount == 0) return true;
            var part = __instance.GetComponent<ThemeSharedBodyPart>();
            if (part == null) return true;
            if (!part.Accept) return false;
            if (part.IsCore)
            {
                if (ThemeSharedDamageScope.ForwardingBody != part.BodyId) 伤害 *= part.DamageMultiplier;
                return true;
            }
            if (!part.MergeStatesWithOwner) { __state = part.BeginLocalHit(); return true; }
            part.Forward(伤害); return false;
        }
        private static void Postfix(ThemeSharedLocalHit __state) { __state?.Part.FinishLocalHit(__state); }
    }

    [HarmonyPatch(typeof(模块_受伤), "受到伤害", new[] { typeof(模块_状态持续效果.状态伤害类) })]
    internal static class ThemeSharedStatusHit
    {
        private static bool Prefix(模块_受伤 __instance, ref 模块_状态持续效果.状态伤害类 sh, out ThemeSharedLocalHit __state)
        {
            __state = null;
            if (ThemeSharedBodyPart.ActiveCount == 0) return true;
            var part = __instance.GetComponent<ThemeSharedBodyPart>();
            if (part == null) return true;
            if (part.IsCore)
            {
                if (ThemeSharedDamageScope.ForwardingBody != part.BodyId && part.DamageMultiplier != 1)
                    sh = new 模块_状态持续效果.状态伤害类 { 状态 = sh.状态, 数值 = sh.数值 * part.DamageMultiplier };
                return true;
            }
            if (!part.MergeStatesWithOwner) { __state = part.BeginLocalHit(); return true; }
            part.StatusHit(sh);
            return false;
        }
        private static void Postfix(ThemeSharedLocalHit __state) { __state?.Part.FinishLocalHit(__state); }
    }

    [HarmonyPatch(typeof(模块_状态属性), "增加状态")]
    internal static class ThemeSharedStateRecipient
    {
        private static bool Prefix(模块_状态属性 __instance, 模块_状态属性.状态类 状态)
        {
            if (ThemeSharedBodyPart.ActiveCount == 0) return true;
            var part = __instance.GetComponent<ThemeSharedBodyPart>();
            if (part == null || !part.MergeStatesWithOwner) return true;
            if (!ThemeSharedDamageScope.AcceptState(part.BodyId, 状态)) return false;
            if (part.IsCore) return true;
            if (part.Owner != null && part.Owner.Alive)
                part.Owner.States?.增加状态(ThemeSharedDamageScope.CopyState(状态));
            return false;
        }
    }
}
