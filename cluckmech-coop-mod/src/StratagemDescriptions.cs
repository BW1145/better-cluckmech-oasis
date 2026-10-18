using System;
using HarmonyLib;

namespace CluckmechCoop
{
    internal static class StratagemDescriptions
    {
        private static string Direction(string text) => "<color=orange>强化方向："+text+"</color>\n";
        private static string Duration(float seconds) => "<color=red>"+seconds.ToString("0.#")+"秒</color>";
        internal static string Summary(模块_基本属性 stats)
        {
            var p=new StratagemParameters(stats.ID,stats.强化词条列表);
            switch (stats.ID)
            {
                case 20001: return Direction("攻击力、攻速、布雷")+
                    "·本体为无敌陷阱\n"+
                    $"·首次布设{p.MineInitial}颗雷，每{p.RefillInterval:0.#}秒补{p.MineRefill}颗，上限{p.MineCap}颗\n"+
                    "·优先向敌人脚下空位布雷，只由地面敌人触发\n"+
                    "·地雷独立触发并造成范围爆炸";
                case 20002: return Direction("攻击力、攻速、范围")+
                    "·不会受到敌人攻击\n"+
                    $"·呼叫{Duration(p.CallDelay)}后，向指定区域随机炮击{p.Salvos}批，每批3发\n"+
                    "·攻速加快炮击节奏，最后一批结束后消失";
                case 20003: return Direction("攻击力、攻速、持续照射")+
                    "·不会受到敌人攻击\n"+
                    $"·呼叫{Duration(p.CallDelay)}后持续照射{Duration(p.Duration)}，自动追踪高生命目标\n"+
                    "·连续照射同一敌人3秒，攻击间隔逐渐缩短至0.02秒；换目标或中断后重置\n"+
                    "·照射附带<color=red>灼烧</color>，结束后消失";
                case 20004: return Direction("攻击力、爆炸范围")+
                    "·不会受到敌人攻击\n"+
                    $"·呼叫{Duration(1.8f+p.CallDelay)}后爆炸，中心{p.InnerRadius:0.#}格内造成全额伤害\n"+
                    $"·外圈伤害随距离衰减，最远{p.BlastRadius:0.#}格；爆炸后消失";
                case 20005: return Direction("攻速、范围、控制")+
                    "·会受到敌人攻击，自动优先攻击未覆盖的敌群\n"+
                    $"·每弹形成持续{Duration(p.FieldDuration)}的区域，使敌人<color=green>减速{p.Slow*100:0}%</color>\n"+
                    (p.Gas?"·瓦斯附加中毒，替代首次眩晕":$"·首次进入眩晕{Duration(p.Stun)}，同一敌人每5秒最多眩晕一次");
                case 20006: return Direction("生命值、格挡、护罩")+
                    "·会受到敌人攻击，吸引附近敌人的仇恨\n"+
                    $"·展开半径{p.BlastRadius:0.#}格护罩，拦截从罩外射入的敌方弹丸\n"+
                    $"·容量为本体最大生命的{p.CapacityRatio*100:0}%，破盾{Duration(p.Reboot)}后重新展开\n"+
                    "·敌人可走进护罩，近战仍可攻击本体";
                case 20007: return Direction("无人机攻击力、攻速、散热")+
                    "·固定基座召唤1架激光无人机，可放置多个基座\n"+
                    "·无人机为无敌召唤物，跟随所属角色，以连续激光攻击附近敌人\n"+
                    $"·过热返回自己的基座，充能{Duration(p.DockTime)}后再次出击\n"+
                    (p.Piercing?"·激光贯穿至射程终点，首个敌人全额、后续各50%伤害\n":"")+
                    (p.HeatRecovery>0?$"·每击杀一个敌人降低{p.HeatRecovery:0}热量\n":"")+
                    "·基座损毁后无人机消失";
                default: return "";
            }
        }
        internal static string Details(模块_基本属性 stats)
        {
            if (stats.ID == 20101)
            {
                var mine = stats.GetComponent<StratagemUnit>();
                return $"单雷伤害 {mine?.MineDamage ?? 100:0.#}\n爆炸半径 {mine?.Parent?.Parameters.BlastRadius ?? 1.1f:0.#}格";
            }
            if (stats.ID == 20102)
            {
                var drone = stats.GetComponent<StratagemUnit>();
                var gun = stats.GetComponent<模块_攻击属性>();
                float multiplier = drone?.Parent?.Parameters.DamageMultiplier ?? drone?.Parameters?.DamageMultiplier ?? 1;
                return $"耐久 ∞\n单次伤害 {gun.当前攻击力*multiplier:0.#}\n射程 {gun.当前攻击范围:0.#}格\n移速 {stats.GetComponent<模块_移动属性>().当前移动速度:0.#}格/秒";
            }
            var p = new StratagemParameters(stats.ID,stats.强化词条列表);
            var attack = stats.GetComponent<模块_攻击属性>();
            bool instance = stats.是否为实例();
            float damage = instance ? attack.当前攻击力 : p.Definition.Damage;
            float range = instance ? attack.当前攻击范围 : p.Definition.Range;
            float health = instance ? stats.当前生命值上限 : stats.生命值;
            string common = $"动力 {p.Power}　放置冷却 {p.Cooldown:0.#}秒\n";
            switch (p.Definition.Kind)
            {
                case StratagemKind.Mines: return common+$"首次 {p.MineInitial}颗　上限 {p.MineCap}颗\n每 {p.RefillInterval:0.#}秒补充 {p.MineRefill}颗\n单雷伤害 {damage*p.DamageMultiplier:0.#}　爆炸半径 {p.BlastRadius:0.#}格";
                case StratagemKind.Barrage: return common+$"{p.Salvos}批，每批3发\n单发伤害 {damage*p.DamageMultiplier:0.#}\n散布半径 {p.ScatterRadius:0.#}格　爆炸半径 {p.BlastRadius:0.#}格";
                case StratagemKind.Laser: return common+$"持续 {p.Duration:0.#}秒\n每次照射伤害 {damage*p.DamageMultiplier:0.#}\n光束半径 {p.BlastRadius:0.##}格　追踪速度 {p.TrackSpeed:0.#}格/秒";
                case StratagemKind.Eagle: return common+$"中心伤害 {damage*p.DamageMultiplier:0.#}\n内半径 {p.InnerRadius:0.#}格　外半径 {p.BlastRadius:0.#}格\n呼叫至爆炸 {1.8f+p.CallDelay:0.#}秒";
                case StratagemKind.Mortar: return common+$"射程 {range:0.#}格\n区域半径 {p.BlastRadius:0.#}格　持续 {p.FieldDuration:0.#}秒\n减速 {p.Slow*100:0}%　眩晕 {p.Stun:0.#}秒";
                case StratagemKind.Shield:
                    var shield = stats.GetComponent<StratagemShield>();
                    string capacity = shield == null ? $"{health*p.CapacityRatio:0}" : $"{shield.Capacity:0}/{health*p.CapacityRatio:0}";
                    return common+$"护罩半径 {p.BlastRadius:0.#}格\n容量 {capacity}\n破盾重启 {p.Reboot:0.#}秒";
                default:
                    var rover = stats.GetComponent<StratagemRover>();
                    var sync = stats.GetComponent<联机_单位属性同步>();
                    string heat = instance ? $"{(rover != null ? rover.Heat : sync == null ? 0 : sync.进度条*p.HeatCap):0}/{p.HeatCap:0}" : $"{p.HeatCap:0}";
                    return common+$"每个基座1架无人机\n热量 {heat}　停靠充能 {p.DockTime:0.#}秒\n无人机耐久 ∞\n单次伤害 {damage*p.DamageMultiplier:0.#}　射程 {range:0.#}格";
            }
        }
    }
    [HarmonyPatch(typeof(模块_基本属性),"概述")]
    internal static class StratagemOverview
    {
        private static bool Prefix(模块_基本属性 __instance,ref string __result)
        { if (!StratagemCatalog.IsUnit(__instance.ID) && __instance.ID != 20101 && __instance.ID != 20102) return true; __result = __instance.ID == 20101 ? "·由地面敌人触发，造成范围爆炸" : __instance.ID == 20102 ? "·跟随所属角色，以连续激光攻击附近敌人\n·过热返回自己的基座充能后再次起飞" : StratagemDescriptions.Summary(__instance); return false; }
    }
    [HarmonyPatch(typeof(模块_基本属性),"说明")]
    internal static class StratagemDetails
    {
        private static bool Prefix(模块_基本属性 __instance,ref string __result)
        { if (!StratagemCatalog.IsUnit(__instance.ID) && __instance.ID != 20101 && __instance.ID != 20102) return true; __result = StratagemDescriptions.Details(__instance); return false; }
    }
}
