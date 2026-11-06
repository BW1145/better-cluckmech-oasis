using HarmonyLib;
using UnityEngine;
using 函数库;

namespace CluckmechCoop
{
    internal static class StratagemPanelValues
    {
        internal static StratagemParameters Parameters(UI_单位卡牌 card)
        {
            var stats=card.单位==null?null:card.单位.GetComponent<模块_基本属性>();
            if(stats==null||!StratagemCatalog.IsUnit(stats.ID))return null;
            var owned=stats.强化词条列表;
            if(card.是否统计词条加成&&!stats.是否为实例()&&Plugin.Summary!=null&&card.背包位置>=0&&card.背包位置<Plugin.Summary.当前拥有炮台.Count)
                owned=Plugin.Summary.当前拥有炮台[card.背包位置].强化词条;
            return new StratagemParameters(stats.ID,owned);
        }
    }
    [HarmonyPatch(typeof(UI_单位主信息),"传入参数")]
    internal static class StratagemCardDeploymentValues
    {
        private static void Postfix(UI_单位主信息 __instance,UI_单位卡牌 参数)
        {
            var p=StratagemPanelValues.Parameters(参数);if(p==null)return;
            __instance.所需动力.text=p.Power.ToString();__instance.冷却时间.text=p.Cooldown.ToString("0.##");
        }
    }
    [HarmonyPatch(typeof(UI_单位输出端),"传入参数")]
    internal static class StratagemCardAttackValues
    {
        private static void Postfix(UI_单位输出端 __instance,UI_单位卡牌 参数)
        {
            var p=StratagemPanelValues.Parameters(参数);if(p==null)return;
            var attack=参数.单位.GetComponent<模块_攻击属性>();
            var stats=参数.单位.GetComponent<模块_基本属性>();
            float value=attack.攻击力;
            if(stats.是否为实例())value=attack.当前攻击力;
            else if(参数.是否统计词条加成)
                value=(value+常用函数.背包内单位获得属性加成总和(参数.背包位置,加成属性枚举.攻击力,参数.是否衍生单位))*
                    (1+常用函数.背包内单位获得属性加成总和(参数.背包位置,加成属性枚举.百分比攻击力,参数.是否衍生单位));
            value*=p.DamageMultiplier;
            __instance.文本_攻击力.text=Mathf.RoundToInt(value)+常用函数.动态文本函数(value,attack.攻击力,Mathf.RoundToInt(attack.攻击力).ToString());
            __instance.默认攻击条.fillAmount=常数类.数值条_攻击力(attack.攻击力);
            __instance.加成攻击条.fillAmount=Mathf.Max(0,常数类.数值条_攻击力(value)-__instance.默认攻击条.fillAmount);
        }
    }
    [HarmonyPatch(typeof(UI_详细信息),"更新信息")]
    internal static class StratagemObservedAttack
    {
        private static void Postfix(UI_详细信息 __instance,GameObject ___当前观察单位)
        {
            var stats=___当前观察单位==null?null:___当前观察单位.GetComponent<模块_基本属性>();
            if(stats==null||!StratagemCatalog.IsUnit(stats.ID))return;
            var attack=stats.GetComponent<模块_攻击属性>();
            var p=new StratagemParameters(stats.ID,stats.强化词条列表);
            __instance.文本_攻击力.text=Mathf.RoundToInt(attack.当前攻击力*p.DamageMultiplier).ToString();
        }
    }
}
