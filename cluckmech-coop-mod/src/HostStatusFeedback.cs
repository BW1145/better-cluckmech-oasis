using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    // Decide once, while the state is applied, not by reclassifying its attacker
    // on every tick. One attacker can also apply ordinary client-local states.
    internal static class HostStatusFeedback
    {
        [ThreadStatic] private static int exclusiveDepth;
        internal struct Scope:IDisposable
        {
            private bool active;
            private int previous;
            internal Scope(bool enabled)
            {active=enabled;previous=exclusiveDepth;if(enabled)exclusiveDepth++;}
            public void Dispose(){if(active){exclusiveDepth=previous;active=false;}}
        }
        internal static Scope Enter()=>new Scope(Plugin.Role==1&&OtherworldWorld.Active);
        internal static bool RequiresDisplay(Component target,模块_状态属性.状态类 state)
        {
            if(Plugin.Role!=1||!OtherworldWorld.Active)return false;
            if(exclusiveDepth>0)return true;
            if(!OtherworldDamage.ApplyingSpace.HasValue)return false;
            var unit=OtherworldWorld.Unit(target.gameObject);
            // Match the existing direct-damage guest guard; do not change that guard.
            return unit!=null&&(unit.Space!=0||OtherworldProgress.For(state.攻击者)!=null);
        }
        internal static 模块_状态属性.状态类 Tag(模块_状态属性.状态类 state)
        {
            if(state is OtherworldState tagged&&tagged.HostDisplay)return state;
            var copy=OtherworldStates.Copy(state,(state as OtherworldState)?.Owner??-1);
            copy.HostDisplay=true;return copy;
        }
    }
    [HarmonyPatch]
    internal static class HostOnlyStatusApplication
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(OtherworldActions),"Status");
            yield return AccessTools.Method(typeof(OtherworldStates),"Spread");
        }
        private static void Prefix(out HostStatusFeedback.Scope __state)=>__state=HostStatusFeedback.Enter();
        private static Exception Finalizer(Exception __exception,HostStatusFeedback.Scope __state)
        {__state.Dispose();return __exception;}
    }
}
