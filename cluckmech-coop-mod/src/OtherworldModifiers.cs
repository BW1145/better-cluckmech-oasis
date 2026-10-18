using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class OtherworldModifiers
    {
        internal static float Health(float value,模块_基本属性 health)
        {
            var unit=OtherworldWorld.Unit(health.gameObject);var run=OtherworldProgress.For((int)health.所属玩家);
            if(unit==null||!unit.IsMachine||run==null)return value;
            if(run.Initial==26007)value*=1+run.Hunting;
            if(unit.Space!=0&&run.Initial==26006)value*=2;
            return value;
        }
        internal static float BaseAttack(float value,模块_攻击属性 stats)
        {
            var unit=OtherworldWorld.Unit(stats.gameObject);if(unit==null||!unit.IsPlayer)return value;
            var run=OtherworldProgress.For((int)unit.Health.所属玩家);return run!=null&&run.Has(26108)?value+run.Upgrades:value;
        }
        internal static float Rate(float value,模块_攻击属性 stats)
        {
            var unit=OtherworldWorld.Unit(stats.gameObject);if(unit==null||!unit.IsMachine)return value;
            var run=OtherworldProgress.For((int)unit.Health.所属玩家);if(run==null)return value;
            if(unit.WarmUntil>Time.time)value+=.5f;if(unit.RallyUntil>Time.time)value+=.5f;
            if(unit.Space!=0&&run.Initial==26006&&run.Has(26120))value+=KamuiWorld.Categories(unit.Space,(int)unit.Health.所属玩家)*.15f;
            return value;
        }
        internal static void Attack(模块_攻击属性 stats)
        {
            var unit=OtherworldWorld.Unit(stats.gameObject);if(unit==null||!unit.IsMachine)return;
            var run=OtherworldProgress.For((int)unit.Health.所属玩家);if(run==null)return;
            if(run.Initial==26007)stats.当前攻击力*=1+run.Hunting;
            if(unit.Space!=0&&run.Initial==26006)stats.当前攻击力*=run.Has(26119)?2.5f:1.5f;
            if(unit.WarmUntil>Time.time)stats.当前攻击力*=1.5f;
        }
        internal static IEnumerable<CodeInstruction> Wrap(IEnumerable<CodeInstruction> instructions,Type type,string field,string method)
        {
            var target=AccessTools.Field(type,field);var wrapper=AccessTools.Method(typeof(OtherworldModifiers),method);
            foreach(var instruction in instructions)
            {
                yield return instruction;
                if(instruction.opcode==OpCodes.Ldfld&&Equals(instruction.operand,target))
                {yield return new CodeInstruction(OpCodes.Ldarg_0);yield return new CodeInstruction(OpCodes.Call,wrapper);}
            }
        }
    }
    [HarmonyPatch(typeof(模块_基本属性),"运算生命值上限")]
    internal static class OtherworldHealth
    {private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)=>OtherworldModifiers.Wrap(code,typeof(模块_基本属性),"生命值倍率",nameof(OtherworldModifiers.Health));}
    [HarmonyPatch(typeof(模块_攻击属性),"运算攻击力")]
    internal static class OtherworldAttackStats
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)=>OtherworldModifiers.Wrap(code,typeof(模块_攻击属性),"攻击力",nameof(OtherworldModifiers.BaseAttack));
        private static void Postfix(模块_攻击属性 __instance)=>OtherworldModifiers.Attack(__instance);
    }
    [HarmonyPatch(typeof(模块_攻击属性),"运算攻击速度")]
    internal static class OtherworldAttackRate
    {private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)=>OtherworldModifiers.Wrap(code,typeof(模块_攻击属性),"变化百分比攻击速度",nameof(OtherworldModifiers.Rate));}
    [HarmonyPatch(typeof(模块_攻击属性),"运算攻击范围")]
    internal static class OtherworldAttackRange
    {
        private static void Postfix(模块_攻击属性 __instance)
        {var unit=OtherworldWorld.Unit(__instance.gameObject);if(unit!=null&&unit.IsPlayer&&OtherworldProgress.For((int)unit.Health.所属玩家)?.Initial==26002)__instance.当前攻击范围+=2;}
    }
    [HarmonyPatch(typeof(模块_基本属性),"运算格挡值")]
    internal static class OtherworldBlock
    {
        private static void Postfix(模块_基本属性 __instance)
        {
            var unit=OtherworldWorld.Unit(__instance.gameObject);if(unit==null||!unit.IsMachine||unit.Space==0)return;
            var run=OtherworldProgress.For((int)__instance.所属玩家);
            if(run?.Initial==26006&&run.Has(26120))__instance.当前格挡值+=KamuiWorld.Categories(unit.Space,(int)__instance.所属玩家)*5;
        }
    }
    [HarmonyPatch(typeof(模块_移动属性),"运算移动速度")]
    internal static class OtherworldPhaseSpeed
    {private static void Postfix(模块_移动属性 __instance){var actor=__instance.GetComponent<OtherworldActor>();if(actor!=null&&actor.Phased)__instance.当前移动速度*=1.5f;}}
    [HarmonyPatch(typeof(模块_基本属性),"运算无敌")]
    internal static class OtherworldPhaseInvulnerability
    {private static void Postfix(模块_基本属性 __instance){var actor=__instance.GetComponent<OtherworldActor>();if(actor!=null&&actor.Phased)__instance.当前是否无敌=true;}}

    [HarmonyPatch(typeof(模块_受伤),"受到伤害",new[]{typeof(世界类.伤害数据包)})]
    internal static class OtherworldDirectDamage
    {
        private static bool Prefix(模块_受伤 __instance,ref 世界类.伤害数据包 sjb,out OtherworldDamage.Scope __state)
        {
            __state=new OtherworldDamage.Scope{Captured=true,Previous=OtherworldDamage.ApplyingSpace};
            if(!OtherworldWorld.Active)return true;
            var unit=OtherworldWorld.Unit(__instance.gameObject);if(unit==null)return true;
            int origin=OtherworldDamage.Space(sjb);
            if(origin!=unit.Space)return false;
            var actor=unit.IsPlayer?unit.GetComponent<OtherworldActor>():null;
            if(actor!=null&&actor.Phased)
            {
                actor.Avoid(sjb);
                return false;
            }
            if(Plugin.Role==2&&(unit.Space!=0||OtherworldProgress.For(sjb.攻击者)!=null))return false;
            if(unit.Space!=0&&unit.Health.当前阵营==阵营数据库.阵营.敌方&&KamuiWorld.Run(unit.Space)?.Has(26116)==true)
            {
                sjb=new 世界类.伤害数据包{攻击者=sjb.攻击者,攻击目标=sjb.攻击目标,攻击坐标=sjb.攻击坐标,是否制导=sjb.是否制导,
                    所属阵营=sjb.所属阵营,攻击伤害=sjb.攻击伤害*1.25f,暴击倍率=sjb.暴击倍率,造成状态=sjb.造成状态,
                    是否产生仇恨=sjb.是否产生仇恨,是否暴击=sjb.是否暴击,穿透数量=sjb.穿透数量,穿透伤害加成=sjb.穿透伤害加成,
                    是否击退=sjb.是否击退,是否爆炸=sjb.是否爆炸,击退类型=sjb.击退类型,击退参数=sjb.击退参数};
                OtherworldDamage.Remember(sjb,origin);
            }
            OtherworldDamage.ApplyingSpace=origin;
            return true;
        }
        private static System.Exception Finalizer(System.Exception __exception,OtherworldDamage.Scope __state)
        {if(__state.Captured)OtherworldDamage.ApplyingSpace=__state.Previous;return __exception;}
    }
    [HarmonyPatch(typeof(模块_受伤),"受到伤害",new[]{typeof(float)})]
    internal static class OtherworldFlatDamage
    {
        private static bool Prefix(模块_受伤 __instance,ref float 伤害)
        {
            var unit=OtherworldWorld.Unit(__instance.gameObject);if(unit==null)return true;
            var actor=unit.IsPlayer?unit.GetComponent<OtherworldActor>():null;if(actor!=null&&actor.Phased)return false;
            if(unit.Space!=0&&unit.Health.当前阵营==阵营数据库.阵营.敌方&&KamuiWorld.Run(unit.Space)?.Has(26116)==true)伤害*=1.25f;
            return true;
        }
    }
    [HarmonyPatch(typeof(模块_受伤),"受到伤害",new[]{typeof(模块_状态持续效果.状态伤害类)})]
    internal static class OtherworldStatusDamage
    {
        private static bool Prefix(模块_受伤 __instance,ref 模块_状态持续效果.状态伤害类 sh)
        {
            var unit=OtherworldWorld.Unit(__instance.gameObject);if(unit==null)return true;
            if(unit.IsPlayer&&unit.GetComponent<OtherworldActor>()?.Phased==true)return false;
            if(unit.Space!=0&&unit.Health.当前阵营==阵营数据库.阵营.敌方&&KamuiWorld.Run(unit.Space)?.Has(26116)==true)
                sh=new 模块_状态持续效果.状态伤害类{状态=sh.状态,数值=sh.数值*1.25f};
            return true;
        }
    }
}
