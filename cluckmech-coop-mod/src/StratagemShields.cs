using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    internal sealed class StratagemProjectile
    {
        internal 模块_伤害精灵 Spirit;
        internal Vector2 Previous;
        internal bool Cancelled;
        internal readonly HashSet<StratagemShield> Crossed = new HashSet<StratagemShield>();
    }
    internal struct ShieldCrossing
    {
        internal StratagemShield Shield;
        internal float T;
    }
    internal static class StratagemShields
    {
        private static readonly List<StratagemShield> shields = new List<StratagemShield>();
        private static readonly Dictionary<模块_伤害精灵,StratagemProjectile> projectiles = new Dictionary<模块_伤害精灵,StratagemProjectile>();
        private static readonly List<StratagemProjectile> tracked = new List<StratagemProjectile>();
        private static readonly List<ShieldCrossing> crossings = new List<ShieldCrossing>();
        internal static bool Active => shields.Count > 0;
        internal static bool First(StratagemShield shield) => shields.Count > 0 && shields[0] == shield;
        internal static void Add(StratagemShield shield)
        {
            if (shields.Count == 0) foreach (var projectile in tracked) if (projectile.Spirit != null) projectile.Previous = projectile.Spirit.transform.position;
            shields.Add(shield);
            foreach (var projectile in tracked)
                if (projectile.Spirit != null && Vector2.Distance(projectile.Spirit.transform.position,shield.transform.position) <= shield.Radius)
                    projectile.Crossed.Add(shield);
        }
        internal static void Remove(StratagemShield shield) => shields.Remove(shield);
        internal static void Clear() { shields.Clear(); projectiles.Clear(); tracked.Clear(); crossings.Clear(); }
        internal static void Register(模块_伤害精灵 spirit)
        {
            if (!Plugin.InCombat || !ModuleSettings.Extra.Stratagems || spirit.伤害数据.所属阵营 != 阵营数据库.阵营.敌方 ||
                spirit.GetComponent<模块_伤害精灵_飞行>() == null && spirit.GetComponent<模块_伤害精灵_弹道飞行>() == null || projectiles.ContainsKey(spirit)) return;
            var projectile = new StratagemProjectile { Spirit = spirit, Previous = spirit.transform.position };
            projectiles.Add(spirit,projectile); tracked.Add(projectile);
            spirit.gameObject.AddComponent<StratagemProjectileLifetime>().Spirit = spirit;
        }
        internal static void Remove(模块_伤害精灵 spirit)
        { if (projectiles.TryGetValue(spirit,out var projectile)) { projectiles.Remove(spirit); tracked.Remove(projectile); } }
        internal static void Tick()
        {
            if (!Active || !Plugin.InCombat) return;
            for (int i = tracked.Count-1; i >= 0; i--)
            {
                if (i >= tracked.Count) continue;
                var projectile = tracked[i];
                if (projectile.Spirit == null) continue;
                Check(projectile.Spirit);
                if (projectile.Spirit != null) projectile.Previous = projectile.Spirit.transform.position;
            }
        }
        internal static bool Check(模块_伤害精灵 spirit)
        {
            if (!Active || !projectiles.TryGetValue(spirit,out var projectile)) return true;
            if (projectile.Cancelled) return false;
            Vector2 from = projectile.Previous, to = spirit.transform.position;
            crossings.Clear();
            foreach (var shield in shields)
                if (shield != null && shield.Open && !projectile.Crossed.Contains(shield) &&
                    ShieldGeometry.Entry(from.x,from.y,to.x,to.y,shield.transform.position.x,shield.transform.position.y,shield.Radius,out var t))
                    crossings.Add(new ShieldCrossing { Shield = shield, T = t });
            crossings.Sort((a,b) => a.T.CompareTo(b.T));
            foreach (var crossing in crossings)
            {
                var shield = crossing.Shield;
                projectile.Crossed.Add(shield);
                Vector2 point = Vector2.Lerp(from,to,crossing.T);
                if (Plugin.Role == 2)
                {
                    Cancel(projectile);
                    return false;
                }
                var packet = spirit.伤害数据;
                float critical = packet.是否暴击 ? 1+packet.暴击倍率 : 1;
                float residual = shield.Block(Mathf.Max(0,packet.攻击伤害*critical),point);
                packet.攻击伤害 = residual/critical;
                if (residual <= 0) { Cancel(projectile); return false; }
            }
            return true;
        }
        private static void Cancel(StratagemProjectile projectile)
        {
            projectile.Cancelled = true;
            var obj = projectile.Spirit.gameObject;
            foreach (var collider in obj.GetComponentsInChildren<Collider2D>()) collider.enabled = false;
            projectile.Spirit.CancelInvoke(); projectile.Spirit.enabled = false;
            var straight = obj.GetComponent<模块_伤害精灵_飞行>();
            if (straight != null) { straight.CancelInvoke(); straight.enabled = false; }
            var arc = obj.GetComponent<模块_伤害精灵_弹道飞行>();
            if (arc != null) { arc.CancelInvoke(); arc.StopAllCoroutines(); arc.enabled = false; }
            var area = obj.GetComponent<模块_范围伤害精灵>();
            if (area != null) { area.CancelInvoke(); area.enabled = false; }
            StratagemWorld.DestroyUnit(obj);
        }
    }
    public sealed class StratagemProjectileLifetime : MonoBehaviour
    {
        internal 模块_伤害精灵 Spirit;
        private void OnDestroy() { if (!ReferenceEquals(Spirit,null)) StratagemShields.Remove(Spirit); }
    }
    public sealed class StratagemShield : MonoBehaviour
    {
        private StratagemUnit root;
        private StratagemShieldVisual dome;
        private float capacity,maximum,lastHit,restart,opening,nextThreat;
        private 联机_单位属性同步 sync;
        private bool waitingForBrokenCapacity;
        private StratagemGauge gauge;
        internal float Radius => root.Parameters.BlastRadius;
        internal float Capacity => capacity;
        internal bool Open => root != null && root.Alive && Time.time >= opening && restart == 0 && capacity > 0;
        internal void Initialize(StratagemUnit source)
        {
            root = source; opening = Time.time+.8f;
            maximum = root.Stats.当前生命值上限*root.Parameters.CapacityRatio; capacity = maximum;
            sync = GetComponent<联机_单位属性同步>();
            dome=StratagemShieldVisual.Create(root);
            dome.SetOpen(true,Radius);
            gauge = new StratagemGauge(transform,1.45f*StratagemRig.ModelScale,new Color(1,.75f,.15f));
            StratagemShields.Add(this);
        }
        private void Update()
        {
            if (root == null || !root.Alive) { if (dome != null) dome.SetOpen(false,Radius); return; }
            var p = root.Parameters;
            maximum = root.Stats.当前生命值上限*p.CapacityRatio;
            if (Plugin.Role == 2)
            {
                if (sync != null)
                {
                    if(sync.进度条<=0)waitingForBrokenCapacity=false;
                    capacity=waitingForBrokenCapacity?0:sync.进度条*maximum;
                }
            }
            else
            {
                capacity = Mathf.Min(capacity,maximum);
                if (restart > 0 && Time.time >= restart) { restart = 0; capacity = maximum; }
                if (restart == 0 && Time.time-lastHit >= p.RegenDelay) capacity = Mathf.Min(maximum,capacity+p.Regen*Time.deltaTime);
                if (sync != null) sync.Network进度条 = maximum > 0 ? capacity/maximum : 0;
                if (Time.time >= nextThreat)
                {
                    nextThreat = Time.time+.5f;
                    foreach (var enemy in StratagemWorld.Enemies)
                    {
                        if (!StratagemWorld.Living(enemy)) continue;
                        float distance = Vector2.Distance(enemy.transform.position,transform.position);
                        if (distance <= Radius) enemy.SendMessage("增加固定仇恨",new 世界类.仇恨数据包 { 仇恨目标 = gameObject, 仇恨值 = 10000/Mathf.Max(.1f,distance)+10000 },SendMessageOptions.DontRequireReceiver);
                    }
                }
            }
            bool starting=Time.time<opening;
            dome.SetOpen(Open||starting,Radius);
            gauge.Set(maximum > 0 ? capacity/maximum : 0);
            root.ShieldState(Open,maximum>0?capacity/maximum:0);
        }
        private void LateUpdate() { if (root != null && StratagemShields.First(this)) StratagemShields.Tick(); }
        internal float Block(float raw,Vector2 point)
        {
            float residual = ShieldGeometry.Residual(raw,root.Parameters.ShieldDamageFactor,ref capacity);
            lastHit = Time.time;
            StratagemEffects.Emit(root,11,point,1,.4f);
            if (capacity <= 0)
            {
                restart = Time.time+root.Parameters.Reboot;
                StratagemEffects.Emit(root,12,transform.position,Radius);
                if (root.Parameters.Discharge)
                    foreach (var enemy in StratagemWorld.Enemies)
                        if (StratagemWorld.Living(enemy) && StratagemWorld.DistanceSquared(enemy,transform.position) <= Radius*Radius)
                        {
                            root.Damage.Status(enemy,攻击类型与状态数据库.状态枚举.眩晕,5,Vector4.zero);
                            enemy.GetComponent<模块_移动>()?.被击退(new Vector4(transform.position.x,transform.position.y,2,.2f));
                        }
            }
            return residual;
        }
        internal void HitVisual(Vector2 point)=>dome.Hit(point);
        internal void BreakVisual()
        {
            if(Plugin.Role==2){waitingForBrokenCapacity=sync!=null&&sync.进度条>0;capacity=0;}
            dome.SetOpen(false,Radius);
        }
        private void OnDestroy() => StratagemShields.Remove(this);
    }
    [HarmonyPatch(typeof(模块_伤害精灵),"Start")]
    internal static class RegisterShieldProjectile { private static void Postfix(模块_伤害精灵 __instance) => StratagemShields.Register(__instance); }
    [HarmonyPatch(typeof(模块_伤害精灵),"OnTriggerEnter2D")]
    internal static class ShieldBeforeProjectileHit { private static bool Prefix(模块_伤害精灵 __instance) => StratagemShields.Check(__instance); }
    [HarmonyPatch(typeof(模块_伤害精灵),"死亡")]
    internal static class ShieldBeforeProjectilePayload { private static bool Prefix(模块_伤害精灵 __instance) => StratagemShields.Check(__instance); }
}
