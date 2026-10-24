using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    internal sealed class StratagemZone
    {
        internal StratagemUnit Source;
        internal Vector2 Center;
        internal float Radius, Until;
        internal bool Scorch;
        internal GameObject Sensor;
        internal StratagemFieldSensor Membership;
        internal readonly HashSet<模块_基本属性> Entered = new HashSet<模块_基本属性>();
    }
    internal sealed class StratagemStateHandle
    {
        internal 模块_状态属性 Owner;
        internal 模块_状态持续效果 Effects;
        internal 模块_状态属性.状态类 State;
        internal 模块_状态持续效果.底层状态类 Bottom;
        private static readonly MethodInfo restore = AccessTools.Method(typeof(模块_状态持续效果),"还原底层状态");
        private static readonly MethodInfo refresh = AccessTools.Method(typeof(模块_状态持续效果),"底层状态刷新");
        internal bool Valid => Owner != null && State != null && Owner.当前状态列表.Contains(State);
        internal static StratagemStateHandle Create(StratagemUnit source,模块_基本属性 target,攻击类型与状态数据库.状态枚举 type,float seconds,float value)
            => Create(source,target,type,seconds,new Vector4(value,0,0,0));
        internal static StratagemStateHandle Create(StratagemUnit source,模块_基本属性 target,攻击类型与状态数据库.状态枚举 type,float seconds,Vector4 values)
        {
            target = ThemeSharedDamageScope.CanonicalStateTarget(target);
            var owner = target.GetComponent<模块_状态属性>();
            if (owner == null) return null;
            var effects = target.GetComponent<模块_状态持续效果>();
            int stateCount = owner.当前状态列表.Count, bottomCount = effects == null ? 0 : effects.当前底层状态列表.Count;
            source.Damage.Status(target,type,seconds,values);
            if (owner.当前状态列表.Count == stateCount) return null;
            var handle = new StratagemStateHandle { Owner = owner, Effects = effects, State = owner.当前状态列表[stateCount] };
            if (type == 攻击类型与状态数据库.状态枚举.减速 && effects != null && effects.当前底层状态列表.Count > bottomCount)
                handle.Bottom = effects.当前底层状态列表[bottomCount];
            return handle;
        }
        internal void Expire()
        {
            // Match native expiry, removing only this handle rather than every state of its type.
            if (Owner != null && State != null) Owner.当前状态列表.Remove(State);
            if (Effects != null && Bottom != null && Effects.当前底层状态列表.Contains(Bottom))
            {
                object[] type = { 攻击类型与状态数据库.底层状态枚举.百分比移动速度 };
                restore.Invoke(Effects,type); Effects.当前底层状态列表.Remove(Bottom); refresh.Invoke(Effects,type);
            }
            Owner = null; Effects = null; State = null; Bottom = null;
        }
        internal void Maintain(float seconds,float value)
        {
            if (!Valid) return;
            float time = seconds*(1-State.自身抗性属性.当前时间抗性);
            if (time <= 0) return;
            State.剩余时间 = time;
            State.加成属性.当前属性效果 = new Vector4(value,0,0,0);
            if (Bottom == null || Effects == null || !Effects.当前底层状态列表.Contains(Bottom)) return;
            Bottom.持续时间 = time;
            if (Bottom.数值 == -value) return;
            object[] type = { 攻击类型与状态数据库.底层状态枚举.百分比移动速度 };
            restore.Invoke(Effects,type); Bottom.数值 = -value; refresh.Invoke(Effects,type);
        }
    }
    internal static class StratagemFields
    {
        private static readonly List<StratagemZone> zones = new List<StratagemZone>();
        private static readonly Dictionary<模块_基本属性,float> stunUntil = new Dictionary<模块_基本属性,float>();
        private static readonly Dictionary<模块_基本属性,StratagemStateHandle> slows = new Dictionary<模块_基本属性,StratagemStateHandle>();
        private static readonly Dictionary<模块_基本属性,Dictionary<StratagemUnit,StratagemStateHandle>> poisons = new Dictionary<模块_基本属性,Dictionary<StratagemUnit,StratagemStateHandle>>();
        private static Coroutine task;
        internal static List<StratagemSavedField> SaveRuntime(StratagemUnit source,Vector2 center)
        {
            var saved=new List<StratagemSavedField>();
            foreach(var zone in zones)if(zone.Source==source&&zone.Until>Time.time)
                saved.Add(new StratagemSavedField{X=zone.Center.x-center.x,Y=zone.Center.y-center.y,Radius=zone.Radius,Remaining=zone.Until-Time.time,Scorch=zone.Scorch});
            return saved;
        }
        private static readonly Dictionary<模块_基本属性,StratagemZone> strongestSlow = new Dictionary<模块_基本属性,StratagemZone>();
        private static readonly List<模块_基本属性> members = new List<模块_基本属性>();
        private static readonly List<模块_基本属性> cachedTargets = new List<模块_基本属性>();
        private static readonly List<StratagemUnit> cachedSources = new List<StratagemUnit>();
        internal static void Add(StratagemUnit source,Vector2 center,float radius,float duration,bool scorch)
        {
            if (source == null || !Plugin.InCombat) return;
            var zone = new StratagemZone { Source = source, Center = center, Radius = radius, Until = Time.time+duration, Scorch = scorch };
            zone.Sensor = new GameObject("StratagemFieldSensor"); zone.Sensor.transform.position = center;
            var body = zone.Sensor.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
            var collider = zone.Sensor.AddComponent<CircleCollider2D>(); collider.radius = radius; collider.isTrigger = true;
            zone.Membership = zone.Sensor.AddComponent<StratagemFieldSensor>(); zone.Membership.Zone = zone;
            zones.Add(zone);
            source.ActiveFields++;
            if (task == null) task = Plugin.Instance.StartCoroutine(Maintain());
        }
        internal static bool Covered(模块_基本属性 target)
        {
            foreach (var zone in zones)
                if (!zone.Scorch && zone.Until > Time.time && ((Vector2)target.transform.position-zone.Center).sqrMagnitude <= zone.Radius*zone.Radius) return true;
            return false;
        }
        internal static void Remove(模块_基本属性 target)
        {
            stunUntil.Remove(target);
            if (slows.TryGetValue(target,out var slow)) slow?.Expire();
            slows.Remove(target);
            if (poisons.TryGetValue(target,out var bySource)) foreach (var poison in bySource.Values) poison?.Expire();
            poisons.Remove(target);
            foreach (var zone in zones) { zone.Entered.Remove(target); zone.Membership?.Forget(target); }
        }
        internal static void Clear()
        {
            if (task != null && Plugin.Instance != null) Plugin.Instance.StopCoroutine(task);
            foreach (var zone in zones) if (zone.Sensor != null) UnityEngine.Object.Destroy(zone.Sensor);
            foreach (var slow in slows.Values) slow?.Expire();
            foreach (var bySource in poisons.Values) foreach (var poison in bySource.Values) poison?.Expire();
            task = null; zones.Clear(); stunUntil.Clear(); slows.Clear(); poisons.Clear(); strongestSlow.Clear(); members.Clear(); cachedTargets.Clear(); cachedSources.Clear();
        }
        private static IEnumerator Maintain()
        {
            while ((zones.Count > 0 || slows.Count > 0 || poisons.Count > 0) && Plugin.InCombat)
            {
                Tick();
                yield return new WaitForSeconds(.5f);
            }
            task = null;
        }
        internal static void Tick()
        {
            strongestSlow.Clear();
            for (int i = zones.Count-1; i >= 0; i--)
            {
                var zone = zones[i];
                if (zone.Source != null && zone.Until > Time.time) continue;
                if (zone.Source != null) zone.Source.ActiveFields--;
                zones.RemoveAt(i);
                members.Clear();
                members.AddRange(zone.Entered);
                foreach (var target in members)
                {
                    Exit(zone, target);
                }
                if (zone.Sensor != null) UnityEngine.Object.Destroy(zone.Sensor);
            }
            // Work is proportional to actual memberships, independent of unrelated enemies.
            foreach (var zone in zones)
            {
                zone.Membership?.Prune();
                members.Clear();
                members.AddRange(zone.Entered);
                var source = zone.Source;
                foreach (var member in members)
                {
                    if (!StratagemWorld.Present(member))
                    {
                        Remove(member);
                        continue;
                    }
                    var target = ThemeSharedDamageScope.CanonicalStateTarget(member);
                    if (!StratagemWorld.Living(target)) continue;
                    if (zone.Scorch)
                    {
                        source.Damage.StandardBurn(target);
                        continue;
                    }
                    var p = source.Parameters;
                    if (p.Slow > 0 && (!strongestSlow.TryGetValue(target, out var strongest) ||
                        p.Slow > strongest.Source.Parameters.Slow))
                    {
                        strongestSlow[target] = zone;
                    }
                    if (p.Gas)
                    {
                        if (!poisons.TryGetValue(target, out var bySource))
                        {
                            poisons.Add(target, bySource = new Dictionary<StratagemUnit,StratagemStateHandle>());
                        }
                        if (!bySource.TryGetValue(source, out var poison) || poison == null || !poison.Valid)
                        {
                            bySource[source] = poison = StratagemStateHandle.Create(source, target, 攻击类型与状态数据库.状态枚举.中毒, 2, 5);
                        }
                        poison?.Maintain(2, 5);
                    }
                }
            }
            foreach (var entry in strongestSlow)
            {
                var target = entry.Key;
                var slowSource = entry.Value.Source;
                float slow = slowSource.Parameters.Slow;
                if (!slows.TryGetValue(target, out var handle) || handle == null || !handle.Valid)
                {
                    slows[target] = handle = StratagemStateHandle.Create(slowSource, target, 攻击类型与状态数据库.状态枚举.减速, .75f, slow);
                }
                handle?.Maintain(.75f, slow);
            }
            strongestSlow.Clear();
            members.Clear();
            cachedTargets.Clear();
            cachedTargets.AddRange(slows.Keys);
            foreach (var target in cachedTargets)
            {
                if (slows[target] == null || !slows[target].Valid) slows.Remove(target);
            }
            cachedTargets.Clear();
            cachedTargets.AddRange(poisons.Keys);
            foreach (var target in cachedTargets)
            {
                var bySource = poisons[target];
                cachedSources.Clear();
                cachedSources.AddRange(bySource.Keys);
                foreach (var source in cachedSources)
                {
                    if (bySource[source] == null || !bySource[source].Valid) bySource.Remove(source);
                }
                if (bySource.Count == 0) poisons.Remove(target);
            }
            cachedTargets.Clear();
            cachedSources.Clear();
        }
        internal static void Enter(StratagemZone zone,模块_基本属性 target)
        {
            if (!StratagemWorld.Present(target) || !zone.Entered.Add(target)) return;
            target = ThemeSharedDamageScope.CanonicalStateTarget(target);
            if (!StratagemWorld.Living(target) || zone.Source == null || zone.Scorch || zone.Source.Parameters.Gas) return;
            if (stunUntil.TryGetValue(target,out var next) && next > Time.time) return;
            zone.Source.Damage.Status(target,攻击类型与状态数据库.状态枚举.眩晕,zone.Source.Parameters.Stun,Vector4.zero);
            stunUntil[target] = Time.time+5;
        }
        internal static void Exit(StratagemZone zone,模块_基本属性 target)
        {
            if (ReferenceEquals(target,null) || !zone.Entered.Remove(target)) return;
            target = ThemeSharedDamageScope.CanonicalStateTarget(target);
            foreach (var other in zones)
                if (!other.Scorch && other.Source != null && other.Source.Parameters.Slow > 0)
                    foreach (var member in other.Entered)
                        if (ThemeSharedDamageScope.CanonicalStateTarget(member) == target) return;
            if (slows.TryGetValue(target,out var slow) && slow != null && slow.Valid) slow.Maintain(.5f,slow.State.加成属性.当前属性效果.x);
        }
    }
    public sealed class StratagemFieldSensor : MonoBehaviour
    {
        internal StratagemZone Zone;
        private readonly Dictionary<Collider2D,模块_基本属性> contacts = new Dictionary<Collider2D,模块_基本属性>();
        private readonly Dictionary<模块_基本属性,int> counts = new Dictionary<模块_基本属性,int>();
        private readonly List<Collider2D> staleContacts = new List<Collider2D>();
        internal void Prune()
        {
            staleContacts.Clear();
            foreach (var contact in contacts)
            {
                if (contact.Key == null || !contact.Key.enabled || !contact.Key.gameObject.activeInHierarchy ||
                    !StratagemWorld.Present(contact.Value))
                {
                    staleContacts.Add(contact.Key);
                }
            }
            foreach (var collider in staleContacts)
            {
                Untrack(collider);
            }
            staleContacts.Clear();
        }
        internal void Track(Collider2D collider, 模块_基本属性 target)
        {
            if (collider == null || !StratagemWorld.Present(target) || contacts.ContainsKey(collider)) return;
            contacts.Add(collider, target);
            counts.TryGetValue(target, out int count);
            counts[target] = count+1;
            if (count == 0) StratagemFields.Enter(Zone, target);
        }
        internal void Untrack(Collider2D collider)
        {
            if (ReferenceEquals(collider, null) || !contacts.TryGetValue(collider, out var target)) return;
            contacts.Remove(collider);
            int count = counts[target]-1;
            if (count > 0)
            {
                counts[target] = count;
            }
            else
            {
                counts.Remove(target);
                StratagemFields.Exit(Zone, target);
            }
        }
        internal void Forget(模块_基本属性 target)
        {
            if (!counts.Remove(target)) return;
            foreach (var collider in new List<Collider2D>(contacts.Keys))
            {
                if (contacts[collider] == target) contacts.Remove(collider);
            }
        }
        private void OnTriggerEnter2D(Collider2D collider) => Track(collider,collider.GetComponentInParent<模块_基本属性>());
        private void OnTriggerExit2D(Collider2D collider) => Untrack(collider);
    }
}
