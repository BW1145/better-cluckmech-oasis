using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using 函数库;

namespace CluckmechCoop
{
    public sealed class UnitModifiers : MonoBehaviour
    {
        private static readonly Dictionary<模块_玩家_攻击, UnitModifiers> players = new Dictionary<模块_玩家_攻击, UnitModifiers>();
        private static readonly Dictionary<模块_移动属性, UnitModifiers> movers = new Dictionary<模块_移动属性, UnitModifiers>();
        private static readonly Dictionary<模块_攻击属性, UnitModifiers> attackers = new Dictionary<模块_攻击属性, UnitModifiers>();
        private static readonly Dictionary<模块_基本属性, UnitModifiers> defenders = new Dictionary<模块_基本属性, UnitModifiers>();
        internal 模块_移动属性 Movement;
        internal 模块_攻击属性 AttackStats;
        internal 模块_玩家_攻击 PlayerAttack;
        internal 模块_玩家_死亡 Death;
        internal 模块_基本属性 Health;
        internal float MovementFactor = 1, AttackFactor = 1, NextHeal;
        internal float DefenseFactor = 1;
        internal float RallyAttackFactor = 1, RallySpeedFactor = 1, RallyUntil, NextRally;
        internal bool LionHeld, LionReleasePending;
        private 模块_单位跟随圈 rallyCircle;
        private Coroutine rallyRoutine;
        internal bool Burst, RatHeld, RequestedRat, AuraStarted, ChickenCooldownSet;
        internal Vector2 RabbitTarget;
        internal bool HasRabbitTarget;
        internal static UnitModifiers For(GameObject unit)
        {
            var modifiers = unit.GetComponent<UnitModifiers>();
            if (modifiers != null) return modifiers;
            modifiers = unit.AddComponent<UnitModifiers>();
            modifiers.Movement = unit.GetComponent<模块_移动属性>();
            modifiers.AttackStats = unit.GetComponent<模块_攻击属性>();
            modifiers.PlayerAttack = unit.GetComponent<模块_玩家_攻击>();
            modifiers.Death = unit.GetComponent<模块_玩家_死亡>();
            if (modifiers.PlayerAttack != null) players[modifiers.PlayerAttack] = modifiers;
            if (modifiers.Movement != null) movers[modifiers.Movement] = modifiers;
            if (modifiers.AttackStats != null) attackers[modifiers.AttackStats] = modifiers;
            return modifiers;
        }
        internal static bool IsHealing(模块_玩家_攻击 player) => players.TryGetValue(player, out var modifier) && modifier.RatHeld;
        internal void SetMovement(float factor) { MovementFactor = factor; Movement?.运算移动速度(); }
        internal static void Move(模块_移动属性 stats) { if (movers.TryGetValue(stats, out var modifier)) stats.当前移动速度 *= modifier.MovementFactor; }
        internal static void Attack(模块_攻击属性 stats) { if (attackers.TryGetValue(stats, out var modifier)) stats.当前攻击力 *= modifier.AttackFactor * modifier.RallyAttackFactor; }
        internal static void AttackSpeed(模块_攻击属性 stats)
        {
            if (!attackers.TryGetValue(stats, out var modifier) || modifier.RallySpeedFactor == 1) return;
            stats.当前攻击速度 *= modifier.RallySpeedFactor;
            if (stats.当前攻击速度 > 0 && stats.当前攻击倒计时 > 1 / stats.当前攻击速度) stats.当前攻击倒计时 = 1 / stats.当前攻击速度;
        }
        internal GameObject[] RallyParty(词条_100014_工程狮 lion)
        {
            if (Plugin.Role == 2 || !LionHeld || Time.time < NextRally) return new GameObject[0];
            if (rallyCircle == null) rallyCircle = lion.吸引光环?.GetComponent<模块_单位跟随圈>();
            if (rallyCircle == null || rallyCircle.跟随圈目标 == null) return new GameObject[0];
            var party = new HashSet<GameObject>();
            foreach (var collider in rallyCircle.单位列表)
            {
                if (collider == null) continue;
                var hp = collider.GetComponent<模块_基本属性>(); var movement = collider.GetComponent<模块_移动属性>();
                if (hp != null && movement != null && hp.当前阵营 == 阵营数据库.阵营.友方 && hp.当前生命值 > 0 &&
                    hp.单位分类 == 模块_基本属性.单位分类枚举.战士 && movement.前进副目标 == rallyCircle.跟随圈目标.transform &&
                    !hp.单位定位列表.Contains(模块_基本属性.单位定位枚举.基地) && !hp.单位定位列表.Contains(模块_基本属性.单位定位枚举.玩家)) party.Add(collider.gameObject);
            }
            var result = new GameObject[party.Count]; party.CopyTo(result); return result;
        }
        internal void LionRally(bool held, GameObject[] party)
        {
            if (LionHeld == held) return;
            LionHeld = held;
            LionReleasePending = false;
            float factor = held ? .75f : 1 / .75f;
            if (PlayerAttack != null) { PlayerAttack.蓄力蓄满时间 *= factor; PlayerAttack.蓄力时间 *= factor; }
            SetMovement(held ? 2f : 1);
            if (held || Plugin.Role == 2 || Death != null && Death.是否玩家死亡 || Time.time < NextRally || party.Length == 0) return;
            NextRally = Time.time + 8;
            foreach (var unit in party) if (unit != null) For(unit).RallyBoost();
            Plugin.Log($"[Characters] Rally attack boost: {party.Length} warriors, 4 seconds; cooldown 8");
        }
        internal void RallyBoost()
        {
            RallyUntil = Time.time + 4;
            if (rallyRoutine != null) return;
            RallyAttackFactor = 1.25f; RallySpeedFactor = 1.2f;
            AttackStats?.运算攻击力(); AttackStats?.运算攻击速度();
            rallyRoutine = StartCoroutine(EndRallyBoost());
        }
        private IEnumerator EndRallyBoost()
        {
            while (Time.time < RallyUntil) yield return new WaitForSeconds(RallyUntil - Time.time);
            RallyAttackFactor = RallySpeedFactor = 1;
            AttackStats?.运算攻击力(); AttackStats?.运算攻击速度(); rallyRoutine = null;
        }
        internal static void HealthMultiplier(模块_基本属性 stats, out float? original)
        {
            original = null;
            if (!defenders.TryGetValue(stats, out var modifier) || modifier.DefenseFactor == 1) return;
            original = stats.生命值倍率; stats.生命值倍率 *= modifier.DefenseFactor;
        }
        internal static void Block(模块_基本属性 stats) { if (defenders.TryGetValue(stats, out var modifier)) stats.当前格挡值 *= modifier.DefenseFactor; }
        internal void DogBoost()
        {
            Health = GetComponent<模块_基本属性>();
            if (Health == null) return;
            defenders[Health] = this;
            SetDefense(2); StartCoroutine(EndDogBoost());
        }
        private void SetDefense(float factor)
        {
            if (Health == null) return;
            float ratio = Health.当前生命值上限 > 0 ? Health.当前生命值 / Health.当前生命值上限 : 1;
            DefenseFactor = factor;
            Health.运算生命值上限(); Health.运算格挡值();
            Health.当前生命值 = Health.当前生命值上限 * ratio;
        }
        private IEnumerator EndDogBoost()
        {
            yield return new WaitForSeconds(5);
            SetDefense(1);
        }
        internal void RabbitBoost()
        {
            AttackFactor = 1.5f; SetMovement(1.3f); AttackStats?.运算攻击力();
            StartCoroutine(EndRabbitBoost());
        }
        private IEnumerator EndRabbitBoost()
        {
            yield return new WaitForSeconds(5);
            AttackFactor = 1; SetMovement(1); AttackStats?.运算攻击力();
        }
        internal IEnumerator ThreeShots(模块_玩家_技能 skill)
        {
            Burst = true;
            for (int i = 0; i < 2; i++)
            {
                yield return new WaitForSeconds(.2f);
                if (skill == null || Death != null && Death.是否玩家死亡) break;
                skill.技能实现();
            }
            Burst = false;
        }
        internal void RatPulse(词条_100033_肌肉鼠 rat)
        {
            if (Plugin.Role == 2 || Time.time < NextHeal) return;
            NextHeal = Time.time + 10; StartCoroutine(Pulse(rat));
        }
        private IEnumerator Pulse(词条_100033_肌肉鼠 rat)
        {
            yield return null;
            var aura = rat.治疗光环.GetComponent<模块_光环>();
            var healer = rat.治疗光环.GetComponent<模块_光环_治疗_肌肉鼠>();
            float radius = healer.光环范围 + (!AuraStarted && rat.治疗圈大小 == 2 ? 1.5f : 0);
            aura.初始化(); aura.光环优化(radius); aura.检索光环内友方();
            foreach (var item in aura.范围内友方)
            {
                var hp = item.基本属性;
                if (hp == null || hp.当前生命值 <= 0 || item.距离 > radius) continue;
                var death = hp.GetComponent<模块_玩家_死亡>();
                if (death != null && death.是否玩家死亡) continue;
                hp.gameObject.SendMessage("受到治疗", hp.当前生命值上限 * .2f, SendMessageOptions.DontRequireReceiver);
            }
            Plugin.Log("[Characters] Emergency healing pulse; cooldown 10");
        }
        internal void ResetHeld()
        {
            var rat = GetComponent<词条_100033_肌肉鼠>();
            if (rat != null) AccessTools.Method(typeof(词条_100033_肌肉鼠), "光环").Invoke(rat, new object[] { false });
            var lion = GetComponent<词条_100014_工程狮>();
            if (lion != null) AccessTools.Method(typeof(词条_100014_工程狮), "光环").Invoke(lion, new object[] { false });
            RequestedRat = RatHeld = false; SetMovement(1);
        }
        private void OnDestroy()
        {
            if (!object.ReferenceEquals(Movement, null)) movers.Remove(Movement);
            if (!object.ReferenceEquals(AttackStats, null)) attackers.Remove(AttackStats);
            if (!object.ReferenceEquals(PlayerAttack, null)) players.Remove(PlayerAttack);
            if (!object.ReferenceEquals(Health, null)) defenders.Remove(Health);
        }
    }

    internal static class CharacterFeatures
    {
        internal static void ChickenCooldown(模块_玩家_技能 skill)
        {
            if (!ModuleSettings.Extra.Characters || skill == null || !skill.enabled || skill.GetComponent<OtherworldActor>() != null) return;
            var modifier = UnitModifiers.For(skill.gameObject);
            if (modifier.ChickenCooldownSet) return;
            skill.技能cd -= 1; modifier.ChickenCooldownSet = true;
        }
        internal static void ReceiveAim(int role, SessionMessage message)
        {
            if (!NetworkServer.spawned.TryGetValue(message.Unit, out var identity)) return;
            var rabbit = identity.GetComponent<词条_100191_八酱兔>();
            var owner = role == 1 ? Steam联机管理器.玩家_主机 : Steam联机管理器.玩家_客户端;
            if (rabbit == null || owner == null || identity.connectionToClient != owner.connectionToClient || float.IsNaN(message.X) || float.IsNaN(message.Y) || float.IsInfinity(message.X) || float.IsInfinity(message.Y)) return;
            var modifier = UnitModifiers.For(rabbit.gameObject);
            modifier.RabbitTarget = new Vector2(message.X, message.Y); modifier.HasRabbitTarget = true;
        }
        internal static void Nuclear(词条_100115_阿法狗 dog)
        {
            var refs = Plugin.Spawner?.GetComponent<效果物体引用器>();
            if (refs == null) return;
            var explosion = 词条函数.召唤.召唤衍生物(refs.预制体.爆炸_震荡核弹, dog.gameObject, dog.transform.position);
            var damage = explosion.GetComponent<模块_范围伤害精灵>();
            damage.伤害数据.攻击伤害 = 300;
            damage.伤害数据.攻击者 = dog.gameObject;
            if (Plugin.Role == 2)
            {
                damage.enabled = false;
                Object.Destroy(explosion, damage.前置生命周期 + damage.生命周期);
                foreach (var collider in explosion.GetComponentsInChildren<Collider2D>()) collider.enabled = false;
            }
            else dog.GetComponent<模块_基本属性>().当前生命值 = 0;
            Plugin.Log("[Characters] Alpha dog nuclear attack; damage 300");
        }
    }
    [HarmonyPatch(typeof(模块_玩家_技能), "Start")]
    internal static class ChickenReady
    {
        private static void Postfix(模块_玩家_技能 __instance)
        {
            CharacterFeatures.ChickenCooldown(__instance);
        }
    }
    [HarmonyPatch(typeof(词条_100072_霰弹专家), "Start")]
    internal static class ShotgunExpertReady
    {
        private static void Prefix(词条_100072_霰弹专家 __instance) => CharacterFeatures.ChickenCooldown(__instance.GetComponent<模块_玩家_技能>());
    }
    [HarmonyPatch(typeof(模块_玩家_技能), "技能实现")]
    internal static class ChickenBurst
    {
        private static void Postfix(模块_玩家_技能 __instance)
        {
            if (!ModuleSettings.Extra.Characters || !__instance.enabled || __instance.GetComponent<OtherworldActor>() != null) return;
            var modifier = UnitModifiers.For(__instance.gameObject);
            if (!modifier.Burst) modifier.StartCoroutine(modifier.ThreeShots(__instance));
        }
    }
    [HarmonyPatch(typeof(词条_100014_工程狮), "光环")]
    internal static class LionRally
    {
        private static void Prefix(词条_100014_工程狮 __instance, bool 值, out GameObject[] __state)
            => __state = ModuleSettings.Extra.Characters && !值 ? UnitModifiers.For(__instance.gameObject).RallyParty(__instance) : new GameObject[0];
        private static void Postfix(词条_100014_工程狮 __instance, bool 值, GameObject[] __state)
        {
            if (ModuleSettings.Extra.Characters) UnitModifiers.For(__instance.gameObject).LionRally(值, __state);
        }
    }
    [HarmonyPatch(typeof(词条_100014_工程狮), "Update")]
    internal static class LionRelease
    {
        private static readonly System.Reflection.MethodInfo close = AccessTools.Method(typeof(词条_100014_工程狮), "光环实现");
        private static void Postfix(词条_100014_工程狮 __instance, InputAction ___技能)
        {
            if (!ModuleSettings.Extra.Characters || Plugin.Role != 0 && !__instance.isOwned || ___技能 == null || ___技能.IsPressed()) return;
            var modifier = __instance.GetComponent<UnitModifiers>();
            if (modifier == null || !modifier.LionHeld || modifier.LionReleasePending) return;
            modifier.LionReleasePending = true;
            close.Invoke(__instance, new object[] { false });
        }
    }
    [HarmonyPatch(typeof(模块_攻击属性), "运算攻击速度")]
    internal static class RallyAttackSpeed
    {
        private static void Postfix(模块_攻击属性 __instance) => UnitModifiers.AttackSpeed(__instance);
    }
    [HarmonyPatch(typeof(词条_100033_肌肉鼠), "光环实现")]
    internal static class RatHoldEdge
    {
        private static bool Prefix(词条_100033_肌肉鼠 __instance, bool 值)
        {
            if (!ModuleSettings.Extra.Characters) return true;
            var modifier = UnitModifiers.For(__instance.gameObject);
            if (modifier.RequestedRat == 值) return false;
            modifier.RequestedRat = 值; return true;
        }
    }
    [HarmonyPatch(typeof(词条_100033_肌肉鼠), "光环")]
    internal static class RatMovingHealing
    {
        private static void Prefix(词条_100033_肌肉鼠 __instance, out float __state) => __state = __instance.移动属性.移动速度;
        private static void Postfix(词条_100033_肌肉鼠 __instance, bool 值, float __state)
        {
            if (!ModuleSettings.Extra.Characters) return;
            __instance.移动属性.移动速度 = __state;
            var modifier = UnitModifiers.For(__instance.gameObject);
            bool started = 值 && !modifier.RatHeld;
            modifier.RatHeld = 值; modifier.SetMovement(值 ? .7f : 1);
            if (modifier.PlayerAttack != null) modifier.PlayerAttack.能否攻击 = !值;
            if (started) modifier.RatPulse(__instance);
        }
    }
    [HarmonyPatch(typeof(模块_光环_治疗_肌肉鼠), "Start")]
    internal static class RatAuraReady
    {
        private static void Postfix(模块_光环_治疗_肌肉鼠 __instance)
        {
            if (ModuleSettings.Extra.Characters) UnitModifiers.For(__instance.肌肉鼠.gameObject).AuraStarted = true;
        }
    }
    [HarmonyPatch(typeof(模块_玩家_攻击), "攻击实现")]
    internal static class RatAttackGate
    {
        private static bool Prefix(模块_玩家_攻击 __instance)
        {
            if (!ModuleSettings.Extra.Characters) return true;
            return !UnitModifiers.IsHealing(__instance);
        }
    }
    [HarmonyPatch(typeof(模块_玩家_死亡), "玩家死亡")]
    internal static class HeldCharacterDied
    {
        private static void Postfix(模块_玩家_死亡 __instance) => __instance.GetComponent<UnitModifiers>()?.ResetHeld();
    }
    [HarmonyPatch(typeof(词条_100115_阿法狗), "技能实现")]
    internal static class DogNuclearAttack
    {
        private static void Postfix(词条_100115_阿法狗 __instance)
        {
            if (ModuleSettings.Extra.Characters) CharacterFeatures.Nuclear(__instance);
        }
    }
    [HarmonyPatch(typeof(词条_100115_阿法狗), "召唤单位")]
    internal static class DogSpawned
    {
        private static void Postfix(GameObject __result)
        {
            if (ModuleSettings.Extra.Characters && Plugin.Role != 2 && __result != null) UnitModifiers.For(__result).DogBoost();
        }
    }
    [HarmonyPatch(typeof(模块_基本属性), "运算生命值上限")]
    internal static class SummonHealth
    {
        private static void Prefix(模块_基本属性 __instance, out float? __state) => UnitModifiers.HealthMultiplier(__instance, out __state);
        private static void Postfix(模块_基本属性 __instance, float? __state)
        {
            if (__state.HasValue) __instance.生命值倍率 = __state.Value;
        }
    }
    [HarmonyPatch(typeof(模块_基本属性), "运算格挡值")]
    internal static class SummonBlock
    {
        private static void Postfix(模块_基本属性 __instance) => UnitModifiers.Block(__instance);
    }
    [HarmonyPatch(typeof(词条_100191_八酱兔), "技能实现实现")]
    internal static class RabbitAim
    {
        private static void Prefix(词条_100191_八酱兔 __instance)
        {
            if (!ModuleSettings.Extra.Characters || Plugin.Role != 0 && !__instance.isOwned || Mouse.current == null || Camera.main == null) return;
            Vector2 mouse = Mouse.current.position.ReadValue();
            Vector2 point = Camera.main.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, -Camera.main.transform.position.z));
            var modifier = UnitModifiers.For(__instance.gameObject);
            modifier.RabbitTarget = point; modifier.HasRabbitTarget = true;
            if (Plugin.Role != 0) Plugin.Network.Send(new SessionMessage { Type = "rabbit-aim", Token = Plugin.Combat.Token, Unit = __instance.netId, X = point.x, Y = point.y });
        }
    }
    [HarmonyPatch(typeof(词条_100191_八酱兔), "随机圆形落点")]
    internal static class RabbitPosition
    {
        private static void Prefix(词条_100191_八酱兔 __instance, ref Vector2 位置)
        {
            if (!ModuleSettings.Extra.Characters) return;
            var modifier = __instance.GetComponent<UnitModifiers>();
            if (modifier != null && modifier.HasRabbitTarget) 位置 = modifier.RabbitTarget;
        }
    }
    [HarmonyPatch(typeof(词条_100191_八酱兔), "召唤单位")]
    internal static class RabbitSpawned
    {
        private static void Postfix(GameObject __result)
        {
            if (ModuleSettings.Extra.Characters && __result != null) UnitModifiers.For(__result).RabbitBoost();
        }
    }
}
