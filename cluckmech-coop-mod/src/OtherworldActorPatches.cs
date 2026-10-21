using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CluckmechCoop
{
    [HarmonyPatch(typeof(模块_玩家_技能),"按键触发_技能")]
    internal static class OtherworldRightInput
    {
        private static bool Prefix(模块_玩家_技能 __instance,InputAction.CallbackContext 上下文,bool ___是否光标位于UI界面,bool ___是否建造模式,bool ___是否放置基地)
        {
            if(__instance==null)return false;
            var actor=__instance.GetComponent<OtherworldActor>();if(actor==null)return true;
            bool owned=Plugin.Role==0||__instance.isOwned;
            if(owned)actor.Input(上下文,!___是否光标位于UI界面&&!___是否建造模式&&___是否放置基地&&!actor.Death.是否玩家死亡);
            return false;
        }
    }
    internal sealed class OtherworldSkillLifetime:MonoBehaviour
    {
        internal 模块_玩家_技能 Skill;
        internal InputAction Action;
        private void OnDestroy()
        {
            if(Action==null||ReferenceEquals(Skill,null))return;
            Action.started-=Skill.按键触发_技能;Action.canceled-=Skill.按键触发_技能;
        }
    }
    [HarmonyPatch(typeof(模块_玩家_技能),"Start")]
    internal static class OtherworldSkillSubscription
    {
        private static void Postfix(模块_玩家_技能 __instance,InputAction ___技能)
        {var lifetime=__instance.gameObject.AddComponent<OtherworldSkillLifetime>();lifetime.Skill=__instance;lifetime.Action=___技能;}
    }
    [HarmonyPatch(typeof(模块_玩家_技能),"技能冷却")]
    internal static class OtherworldRightCooldown
    {private static bool Prefix(模块_玩家_技能 __instance)=>__instance.GetComponent<OtherworldActor>()==null;}
    [HarmonyPatch(typeof(模块_玩家_攻击),"攻击实现")]
    internal static class OtherworldNormalAttack
    {
        private struct Saved {internal OtherworldActor Actor;internal float Attack;internal Vector3 Muzzle;}
        private static void Prefix(模块_玩家_攻击 __instance,out Saved __state)
        {
            __state=default;var actor=__instance.GetComponent<OtherworldActor>();if(actor==null||actor.Attack==null)return;
            __state=new Saved{Actor=actor,Attack=actor.Attack.当前攻击力,Muzzle=__instance.炮口位置.position};
            actor.Attack.当前攻击力*=actor.NormalFactor;__instance.炮口位置.position=actor.Muzzle;
        }
        private static void Postfix(Saved __state)
        {
            if(__state.Actor==null)return;var actor=__state.Actor;
            actor.Attack.当前攻击力=__state.Attack;actor.Gun.炮口位置.position=__state.Muzzle;actor.Fired();
        }
    }
}

namespace CluckmechCoop
{
    // Native locomotion translates the root in FixedUpdate. During dash only the dash owns it.
    [HarmonyPatch(typeof(模块_玩家_移动),"移动实现")]
    internal static class OtherworldDashMovement
    {
        private static bool Prefix(模块_玩家_移动 __instance)
        {var actor=__instance.GetComponent<OtherworldActor>();return actor==null||!actor.Dashing;}
    }
}
