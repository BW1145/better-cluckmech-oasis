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
        internal static void Add(StratagemUnit source,Vector2 center,float radius,float duration,bool scorch)
        {
            if (source == null || !Plugin.InCombat) return;
            var zone = new StratagemZone { Source = source, Center = center, Radius = radius, Until = Time.time+duration, Scorch = scorch };
            zone.Sensor = new GameObject("StratagemFieldSensor"); zone.Sensor.transform.position = center;
            var body = zone.Sensor.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
            var collider = zone.Sensor.AddComponent<CircleCollider2D>(); collider.radius = radius; collider.isTrigger = true;
            zone.Sensor.AddComponent<StratagemFieldSensor>().Zone = zone;
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
        { stunUntil.Remove(target); slows.Remove(target); poisons.Remove(target); foreach (var zone in zones) zone.Entered.Remove(target); }
        internal static void Clear()
        {
            if (task != null && Plugin.Instance != null) Plugin.Instance.StopCoroutine(task);
            foreach (var zone in zones) if (zone.Sensor != null) UnityEngine.Object.Destroy(zone.Sensor);
            task = null; zones.Clear(); stunUntil.Clear(); slows.Clear(); poisons.Clear();
        }
        private static IEnumerator Maintain()
        {
            while (zones.Count > 0 && Plugin.InCombat)
            {
                for (int i = zones.Count-1; i >= 0; i--)
                {
                    var zone = zones[i];
                    if (zone.Source != null && zone.Until > Time.time) continue;
                    if (zone.Source != null) zone.Source.ActiveFields--;
                    zones.RemoveAt(i);
                    foreach (var target in new List<模块_基本属性>(zone.Entered)) Exit(zone,target);
                    if (zone.Sensor != null) UnityEngine.Object.Destroy(zone.Sensor);
                }
                foreach (var target in StratagemWorld.Enemies)
                {
                    if (!StratagemWorld.Living(target)) continue;
                    float slow = 0;
                    StratagemUnit slowSource = null;
                    foreach (var zone in zones)
                    {
                        if (!zone.Entered.Contains(target)) continue;
                        var source = zone.Source;
                        if (zone.Scorch) { source.Damage.Burn(target,2,1); continue; }
                        var p = source.Parameters;
                        if (p.Slow > slow) { slow = p.Slow; slowSource = source; }
                        if (p.Gas)
                        {
                            if (!poisons.TryGetValue(target,out var bySource)) poisons.Add(target,bySource = new Dictionary<StratagemUnit,StratagemStateHandle>());
                            if (!bySource.TryGetValue(source,out var poison) || poison == null || !poison.Valid)
                                bySource[source] = poison = StratagemStateHandle.Create(source,target,攻击类型与状态数据库.状态枚举.中毒,2,5);
                            poison?.Maintain(2,5);
                        }
                    }
                    if (slowSource == null) continue;
                    if (!slows.TryGetValue(target,out var handle) || handle == null || !handle.Valid)
                        slows[target] = handle = StratagemStateHandle.Create(slowSource,target,攻击类型与状态数据库.状态枚举.减速,.75f,slow);
                    handle?.Maintain(.75f,slow);
                }
                yield return new WaitForSeconds(.5f);
            }
            task = null;
        }
        internal static void Enter(StratagemZone zone,模块_基本属性 target)
        {
            if (!StratagemWorld.Living(target) || !zone.Entered.Add(target) || zone.Source == null || zone.Scorch || zone.Source.Parameters.Gas) return;
            if (stunUntil.TryGetValue(target,out var next) && next > Time.time) return;
            zone.Source.Damage.Status(target,攻击类型与状态数据库.状态枚举.眩晕,zone.Source.Parameters.Stun,Vector4.zero);
            stunUntil[target] = Time.time+5;
        }
        internal static void Exit(StratagemZone zone,模块_基本属性 target)
        {
            if (target == null || !zone.Entered.Remove(target)) return;
            foreach (var other in zones) if (!other.Scorch && other.Entered.Contains(target)) return;
            if (slows.TryGetValue(target,out var slow)) slow?.Maintain(.5f,slow.State.加成属性.当前属性效果.x);
        }
    }
    public sealed class StratagemFieldSensor : MonoBehaviour
    {
        internal StratagemZone Zone;
        private void OnTriggerEnter2D(Collider2D collider) => StratagemFields.Enter(Zone,collider.GetComponentInParent<模块_基本属性>());
        private void OnTriggerExit2D(Collider2D collider) => StratagemFields.Exit(Zone,collider.GetComponentInParent<模块_基本属性>());
    }
}
