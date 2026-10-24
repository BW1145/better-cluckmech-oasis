using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CluckmechCoop
{
    // A source-owned native state identity lets rescue remove this grasp while preserving other control effects.
    public sealed class ThemeArmBinding : MonoBehaviour
    {
        private static readonly List<ThemeArmBinding> bindings = new List<ThemeArmBinding>();
        private ThemeEnemyActor owner;
        private 模块_基本属性 target;
        private 模块_状态属性 states;
        private 模块_状态属性.状态类 state;
        private bool player, active;
        private float until;
        internal static bool HasBindings => bindings.Count > 0;
        internal bool Active => active && owner != null && owner.Alive && ThemeCombat.Friendly(target) &&
            target.gameObject.scene == owner.gameObject.scene;
        internal float Remaining => Mathf.Max(0, until - Time.time);

        internal static bool CanBind(模块_基本属性 target, bool player)
        {
            if (!ThemeCombat.Friendly(target) || target.当前是否无敌 || target.GetComponent<ThemeArmBinding>()?.Active == true ||
                target.GetComponent<模块_状态属性>() == null) return false;
            var resistance = target.GetComponent<模块_抗性属性>();
            var type = player ? 攻击类型与状态数据库.状态枚举.禁锢 : 攻击类型与状态数据库.状态枚举.眩晕;
            var entry = resistance?.状态抗性列表[(int)type];
            return entry == null || entry.当前效果抗性 < 1 && entry.当前时间抗性 < 1;
        }

        internal static ThemeArmBinding Apply(ThemeEnemyActor source, 模块_基本属性 victim, float seconds,
            bool isPlayer, bool synchronizedDuration = false)
        {
            if (!CanBind(victim, isPlayer)) return null;
            var binding = victim.GetComponent<ThemeArmBinding>() ?? victim.gameObject.AddComponent<ThemeArmBinding>();
            binding.owner = source; binding.target = victim; binding.player = isPlayer;
            binding.states = victim.GetComponent<模块_状态属性>();
            var type = isPlayer ? 攻击类型与状态数据库.状态枚举.禁锢 : 攻击类型与状态数据库.状态枚举.眩晕;
            var resistance = victim.GetComponent<模块_抗性属性>()?.状态抗性列表[(int)type];
            float duration = synchronizedDuration ? seconds : seconds * (1 - (resistance?.当前时间抗性 ?? 0));
            if (duration <= 0) return null;
            binding.state = new 模块_状态属性.状态类 {
                攻击者 = source.gameObject, 状态类型 = type, 剩余时间 = duration,
                自身抗性属性 = resistance ?? new 模块_抗性属性.状态抗性单元(),
                加成属性 = new 模块_攻击详细属性.状态属性单元 {
                    默认持续时间 = duration, 当前持续时间 = duration, 当前属性效果 = Vector4.zero
                }
            };
            binding.states.当前状态列表.Add(binding.state);
            binding.until = Time.time + duration; binding.active = true;
            bindings.Add(binding);
            return binding;
        }

        private void Update()
        {
            if (!active) return;
            if (!Active) { Release(); return; }
            if (Time.time < until)
            {
                // Native status ticks every half-second; retain this exact identity through the grasp's own expiry.
                state.剩余时间 = Mathf.Max(state.剩余时间, Remaining + .5f);
                return;
            }
            bool destroyTower = !player && Plugin.Role != 2;
            Release();
            if (destroyTower && ThemeCombat.Friendly(target))
            {
                target.当前生命值 = 0;
                target.GetComponent<模块_死亡>()?.死亡实现();
            }
        }
        internal void Release()
        {
            active = false;
            bindings.Remove(this);
            if (states != null && state != null) states.当前状态列表.Remove(state);
            state = null;
        }
        private void OnDisable() { Release(); }

        internal static bool LocalPlayerBound()
        {
            foreach (var binding in bindings)
            {
                if (!binding.player || !binding.Active) continue;
                var movement = binding.target.GetComponent<模块_玩家_移动>();
                if (Plugin.Role != 0 && !movement.isOwned) continue;
                return true;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(模块_玩家_移动), "移动实现")]
    internal static class ThemeGraspedPlayerMovement
    {
        private static bool Prefix(模块_玩家_移动 __instance) => __instance.GetComponent<ThemeArmBinding>()?.Active != true;
    }

    [HarmonyPatch(typeof(模块_攻击), "攻击")]
    internal static class ThemeGraspedTowerFire
    {
        private static bool Prefix(模块_攻击 __instance)
        {
            var binding = __instance.GetComponent<ThemeArmBinding>();
            return binding == null || !binding.Active || ThemeDeepSeaCombat.IsPlayer(__instance.GetComponent<模块_基本属性>());
        }
    }

    [HarmonyPatch(typeof(建造系统), "是否允许放置")]
    internal static class ThemeGraspedPlacementAvailable
    {
        private static void Postfix(ref bool __result) { if (__result && ThemeArmBinding.LocalPlayerBound()) __result = false; }
    }

    [HarmonyPatch(typeof(建造系统), "开始拖动")]
    internal static class ThemeGraspedPlacementBegin
    { private static bool Prefix() => !ThemeArmBinding.LocalPlayerBound(); }

    [HarmonyPatch(typeof(建造系统), "结束拖动延迟")]
    internal static class ThemeGraspedPlacementRelease
    {
        private static bool Prefix(建造系统 __instance)
        {
            if (!ThemeArmBinding.LocalPlayerBound()) return true;
            __instance.取消建筑模式(); return false;
        }
    }

    [HarmonyPatch]
    internal static class ThemeGraspedNestProgress
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(词条_100009_巢穴类召唤), "Update");
            yield return AccessTools.Method(typeof(词条_100041_黑灯工作室巢穴), "Update");
            yield return AccessTools.Method(typeof(词条_100026_蜘蛛巢穴), "Update");
        }
        private static bool Prefix(MonoBehaviour __instance) => !ThemeArmBinding.HasBindings ||
            __instance.GetComponent<ThemeArmBinding>()?.Active != true;
    }

    [HarmonyPatch]
    internal static class ThemeGraspedNestProduction
    {
        private static readonly FieldInfo waitSeconds = AccessTools.Field(typeof(WaitForSeconds), "m_Seconds");
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(词条_100009_巢穴类召唤), "生成召唤物");
            yield return AccessTools.Method(typeof(词条_100041_黑灯工作室巢穴), "生成召唤物");
        }
        private static void Postfix(MonoBehaviour __instance, ref IEnumerator __result)
        {
            if (ThemeBattleScene.Current?.Definition.Id == ThemeDeepSeaData.OctopusDock.Id)
                __result = Continue(__instance, __result);
        }

        private static IEnumerator Continue(MonoBehaviour source, IEnumerator native)
        {
            try
            {
                while (true)
                {
                    while (source != null && source.GetComponent<ThemeArmBinding>()?.Active == true) yield return null;
                    if (!native.MoveNext()) yield break;
                    if (!(native.Current is WaitForSeconds wait)) { yield return native.Current; continue; }
                    float remaining = (float)waitSeconds.GetValue(wait);
                    while (remaining > 0)
                    {
                        yield return null;
                        if (source == null || source.GetComponent<ThemeArmBinding>()?.Active != true) remaining -= Time.deltaTime;
                    }
                }
            }
            finally { (native as System.IDisposable)?.Dispose(); }
        }
    }
}
