using System;
using System.Collections.Generic;
using HarmonyLib;
using Mirror;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class StratagemWorld
    {
        internal static readonly List<模块_基本属性> Enemies = new List<模块_基本属性>();
        private static readonly HashSet<模块_基本属性> enemySet = new HashSet<模块_基本属性>();
        private static readonly Dictionary<模块_基本属性,Collider2D[]> hitboxes = new Dictionary<模块_基本属性,Collider2D[]>();
        private static readonly System.Reflection.FieldInfo hurtStats = AccessTools.Field(typeof(模块_受伤),"基本属性");
        internal static readonly List<模块_基本属性> Players = new List<模块_基本属性>();
        internal static GameObject Base;
        internal static PolyNav.PolyNavMap GroundMap;
        internal static void Begin()
        {
            Enemies.Clear(); enemySet.Clear(); hitboxes.Clear(); Players.Clear();
            if (!ModuleSettings.Extra.Stratagems) return;
            GroundMap = 函数库.常用函数.搜索物体并返回物体内组件<导航管理器>(函数库.常数类.命名_导航管理器)?.地面导航地图;
            foreach (var stats in 世界事件系统.场上所有单位) Add(stats);
            Base = Plugin.Spawner?.GetComponent<yy引用实例数据库>()?.基地车;
            StratagemAudio.Begin();
            StratagemNativeParticles.Begin();
        }
        internal static void Clear()
        {
            StratagemDeployment.Capture();
            StratagemAudio.Clear();
            StratagemNativeParticles.Clear();
            StratagemSurface.Clear();
            Enemies.Clear(); enemySet.Clear(); hitboxes.Clear(); Players.Clear(); Base = null; GroundMap = null;
            StratagemUnit.Clear(); StratagemFields.Clear(); StratagemShields.Clear(); StratagemDeployment.Clear();
        }
        internal static void Add(模块_基本属性 stats)
        {
            if (!ModuleSettings.Extra.Stratagems || !Plugin.InCombat || stats == null) return;
            if (stats.单位定位列表.Contains(模块_基本属性.单位定位枚举.基地)) Base = stats.gameObject;
            var hurt = stats.GetComponent<模块_受伤>();
            if (Difficulty.CountsForEncounter(stats.gameObject) && hurt != null && hurtStats.GetValue(hurt) != null && enemySet.Add(stats))
            {
                Enemies.Add(stats);
                var shapes = new List<Collider2D>();
                foreach (var shape in stats.GetComponentsInChildren<Collider2D>(true))
                    if (shape.GetComponentInParent<模块_基本属性>() == stats) shapes.Add(shape);
                hitboxes[stats] = shapes.ToArray();
            }
            if (stats.单位定位列表.Contains(模块_基本属性.单位定位枚举.玩家) && !Players.Contains(stats)) Players.Add(stats);
        }
        internal static void Remove(模块_基本属性 stats)
        {
            if (enemySet.Remove(stats)) Enemies.Remove(stats);
            hitboxes.Remove(stats);
            Players.Remove(stats); StratagemFields.Remove(stats);
        }
        internal static bool Living(模块_基本属性 stats) => stats != null && stats.当前生命值 > 0 && !stats.当前是否无敌 &&
            stats.当前阵营 == 阵营数据库.阵营.敌方 && enemySet.Contains(stats);
        internal static float DistanceSquared(模块_基本属性 target,Vector2 point)
        {
            if (!hitboxes.TryGetValue(target,out var shapes)) return float.PositiveInfinity;
            bool hurtShape = false;
            foreach (var shape in shapes) if (shape != null && shape.enabled && shape.gameObject.activeInHierarchy && shape.isTrigger) { hurtShape = true; break; }
            float distance = float.PositiveInfinity;
            foreach (var shape in shapes)
                if (shape != null && shape.enabled && shape.gameObject.activeInHierarchy && (!hurtShape || shape.isTrigger))
                    distance = Mathf.Min(distance,(shape.ClosestPoint(point)-point).sqrMagnitude);
            return distance;
        }
        internal static bool BeamContact(模块_基本属性 target,Vector2 start,Vector2 direction,float length,out float distance)
        {
            distance = float.PositiveInfinity;
            if (!hitboxes.TryGetValue(target,out var shapes)) return false;
            bool hurtShape = false;
            foreach (var shape in shapes) if (shape != null && shape.enabled && shape.gameObject.activeInHierarchy && shape.isTrigger) { hurtShape = true; break; }
            foreach (var shape in shapes)
            {
                if (shape == null || !shape.enabled || !shape.gameObject.activeInHierarchy || hurtShape && !shape.isTrigger) continue;
                var bounds = shape.bounds;
                if (StratagemBeamGeometry.Intersects(start.x,start.y,direction.x,direction.y,length,bounds.center.x,bounds.center.y,bounds.extents.x,bounds.extents.y,out var entry))
                    distance = Mathf.Min(distance,entry);
            }
            return !float.IsPositiveInfinity(distance);
        }
        internal static 模块_基本属性 Player(int role)
        {
            foreach (var player in Players)
                if (player != null && player.当前生命值 > 0 && (int)player.所属玩家 == role) return player;
            return null;
        }
        internal static void DestroyUnit(GameObject obj)
        {
            if (obj == null) return;
            if (Plugin.Role == 1 && obj.GetComponent<NetworkIdentity>() != null) NetworkServer.Destroy(obj);
            else UnityEngine.Object.Destroy(obj);
        }
        internal static uint Id(GameObject obj) => obj == null ? 0 : obj.GetComponent<NetworkIdentity>()?.netId ?? 0;
        internal static 模块_基本属性 Enemy(uint id)
        {
            if (NetworkClient.spawned.TryGetValue(id,out var identity)) return identity.GetComponent<模块_基本属性>();
            return null;
        }
        internal static 模块_基本属性 Target(Vector2 center, float radius, bool bossFirst, bool uncovered = false, 模块_基本属性 exclude = null)
        {
            模块_基本属性 best = null;
            bool bestBoss = false;
            float score = float.NegativeInfinity, distance = float.PositiveInfinity;
            Vector2 basePosition = Base == null ? center : (Vector2)Base.transform.position;
            foreach (var enemy in Enemies)
            {
                if (!Living(enemy) || enemy == exclude) continue;
                float d = ((Vector2)enemy.transform.position - center).sqrMagnitude;
                if (d > radius*radius) continue;
                bool boss = bossFirst && (enemy.GetComponent<词条_100028_Boss胜利判断>() != null || enemy.GetComponent<最终boss状态机>() != null);
                float value = bossFirst ? enemy.当前生命值 : -((Vector2)enemy.transform.position - basePosition).sqrMagnitude;
                if (uncovered && StratagemFields.Covered(enemy)) value -= 100000;
                if (best == null || boss && !bestBoss || boss == bestBoss && (value > score || value == score && (d < distance || d == distance && Id(enemy.gameObject) < Id(best.gameObject))))
                { best = enemy; score = value; distance = d; bestBoss = boss; }
            }
            return best;
        }
    }

    internal sealed class StratagemDamage
    {
        internal readonly GameObject Source;
        private readonly 模块_攻击属性 attack;
        private readonly 模块_攻击详细属性 detailed;
        private readonly 模块_状态属性 states;
        private readonly StratagemUnit unit;
        private readonly Dictionary<模块_基本属性,StratagemStateHandle> burns = new Dictionary<模块_基本属性,StratagemStateHandle>();
        internal StratagemDamage(GameObject source)
        { Source = source; attack = source.GetComponent<模块_攻击属性>(); detailed = source.GetComponent<模块_攻击详细属性>(); states = source.GetComponent<模块_状态属性>(); unit = source.GetComponent<StratagemUnit>(); }
        internal float Attack => attack == null ? 0 : attack.当前攻击力;
        internal float Rate => attack == null || !attack.当前是否能攻击 || states != null && states.查询状态(攻击类型与状态数据库.状态枚举.眩晕) ? 0 : attack.当前攻击速度;
        internal float Range => attack == null ? 0 : attack.当前攻击范围;
        internal bool Critical()
        {
            Source.SendMessage("攻击发动",SendMessageOptions.DontRequireReceiver);
            bool critical = detailed != null && UnityEngine.Random.value < detailed.当前暴击率;
            if (critical) Source.SendMessage("暴击触发",SendMessageOptions.DontRequireReceiver);
            return critical;
        }
        internal void Hit(模块_基本属性 target, float damage, bool critical, bool knockback = false)
        {
            if (!StratagemWorld.Living(target)) return;
            var packet = new 世界类.伤害数据包 {
                攻击者 = Source, 所属阵营 = 阵营数据库.阵营.友方, 攻击目标 = target.transform,
                攻击坐标 = target.transform.position, 攻击伤害 = damage, 是否暴击 = critical,
                暴击倍率 = detailed == null ? 1 : detailed.当前暴击倍率, 是否产生仇恨 = true, 是否爆炸 = true,
                是否击退 = knockback, 击退类型 = 世界类.伤害数据包.击退类型枚举.相对位置击退,
                击退参数 = new Vector4(2,.2f,0,0)
            };
            if (attack != null && detailed != null)
                foreach (var type in attack.当前造成状态)
                {
                    if (type == 攻击类型与状态数据库.状态枚举.灼烧 && (unit.Kind == StratagemKind.Laser || unit.Parameters.BurningMines || unit.Parameters.BurningRover || unit.Parameters.Scorch)) continue;
                    packet.造成状态.Add(State(type,detailed.状态加成列表[(int)type].当前持续时间,detailed.状态加成列表[(int)type].当前属性效果));
                }
            target.GetComponent<模块_受伤>()?.受到伤害(packet);
        }
        internal void Area(Vector2 point, float radius, float damage, bool critical, Func<模块_基本属性,float> multiplier = null)
        {
            for (int i = StratagemWorld.Enemies.Count-1; i >= 0; i--)
            {
                if (i >= StratagemWorld.Enemies.Count) continue;
                var enemy = StratagemWorld.Enemies[i];
                if (StratagemWorld.Living(enemy) && StratagemWorld.DistanceSquared(enemy,point) <= radius*radius)
                    Hit(enemy, damage * (multiplier == null ? 1 : multiplier(enemy)), critical);
            }
        }
        internal 模块_状态属性.状态类 State(攻击类型与状态数据库.状态枚举 type, float seconds, Vector4 values)
        {
            return new 模块_状态属性.状态类 { 攻击者 = Source, 状态类型 = type,
                加成属性 = new 模块_攻击详细属性.状态属性单元 { 默认持续时间 = seconds, 当前持续时间 = seconds, 默认属性效果 = values, 当前属性效果 = values } };
        }
        internal void Status(模块_基本属性 target, 攻击类型与状态数据库.状态枚举 type, float seconds, Vector4 values)
        { if (StratagemWorld.Living(target)) target.GetComponent<模块_状态属性>()?.增加状态(State(type,seconds,values)); }
        internal void StandardBurn(模块_基本属性 target) => Burn(target,10,2,new Vector3(.2f,.2f,.2f));
        internal void Burn(模块_基本属性 target, float damage, float seconds,Vector3 penalties=default)
        {
            if (!StratagemWorld.Living(target)) return;
            if (burns.TryGetValue(target,out var current) && current != null && current.Valid) return;
            var values = new Vector4(damage,penalties.x,penalties.y,penalties.z);
            if (detailed != null)
            {
                var settings = detailed.状态加成列表[(int)攻击类型与状态数据库.状态枚举.灼烧];
                values += settings.默认属性效果 + settings.变化属性效果;
                values = new Vector4(values.x*(1+settings.变化百分比属性效果.x),values.y*(1+settings.变化百分比属性效果.y),values.z*(1+settings.变化百分比属性效果.z),values.w*(1+settings.变化百分比属性效果.w));
                seconds = (seconds+settings.变化持续时间)*(1+settings.变化百分比持续时间);
            }
            burns[target] = StratagemStateHandle.Create(unit,target,攻击类型与状态数据库.状态枚举.灼烧,seconds,values);
        }
        internal void Forget(模块_基本属性 target) => burns.Remove(target);
    }
    [HarmonyPatch(typeof(模块_基本属性), "Awake")]
    internal static class CacheStratagemEnemy { private static void Postfix(模块_基本属性 __instance) => StratagemWorld.Add(__instance); }
    [HarmonyPatch(typeof(模块_受伤), "Start")]
    internal static class CacheReadyStratagemEnemy { private static void Postfix(模块_受伤 __instance) => StratagemWorld.Add(__instance.GetComponent<模块_基本属性>()); }
    [HarmonyPatch(typeof(模块_基本属性), "OnDisable")]
    internal static class RemoveStratagemEnemy { private static void Prefix(模块_基本属性 __instance) => StratagemWorld.Remove(__instance); }
}
