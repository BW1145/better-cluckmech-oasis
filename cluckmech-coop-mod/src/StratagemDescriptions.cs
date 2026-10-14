using System;
using HarmonyLib;

namespace CluckmechCoop
{
    internal static class StratagemDescriptions
    {
        internal static string Summary(int id)
        {
            switch (id)
            {
                case 20001: return "自动布设地雷，阻截地面敌人。";
                case 20002: return "呼叫多批随机轨道炮击，覆盖大片区域。";
                case 20003: return "呼叫追踪光束，持续攻击高生命目标。";
                case 20004: return "呼叫飞鹰投下重型炸弹，中心造成大量伤害。";
                case 20005: return "远距离投射静电场，眩晕并减速敌人。";
                case 20006: return "吸引敌人攻击，展开拦截弹丸的护罩。";
                case 20007: return "随身背包部署激光无人机，过热后返航充能。";
                default: return "";
            }
        }
        internal static string Details(模块_基本属性 stats)
        {
            if (stats.ID == 20101)
            {
                var mine = stats.GetComponent<StratagemUnit>();
                return $"单雷伤害 {mine?.MineDamage ?? 100:0.#}\n爆炸半径 {mine?.Parent?.Parameters.BlastRadius ?? 1.1f:0.#}";
            }
            if (stats.ID == 20102)
            {
                var drone = stats.GetComponent<StratagemUnit>();
                var gun = stats.GetComponent<模块_攻击属性>();
                float multiplier = drone?.Parent?.Parameters.DamageMultiplier ?? drone?.Parameters?.DamageMultiplier ?? 1;
                return $"生命 {stats.当前生命值:0}/{stats.当前生命值上限:0}\n单次伤害 {gun.当前攻击力*multiplier:0.#}\n射程 {gun.当前攻击范围:0.#}\n移速 {stats.GetComponent<模块_移动属性>().当前移动速度:0.#}";
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
                case StratagemKind.Mines: return common+$"首次 {p.MineInitial}颗　上限 {p.MineCap}颗\n每 {p.RefillInterval:0.#}秒补充 {p.MineRefill}颗\n单雷伤害 {damage*p.DamageMultiplier:0.#}　爆炸半径 {p.BlastRadius:0.#}";
                case StratagemKind.Barrage: return common+$"{p.Salvos}批，每批3发\n单发伤害 {damage*p.DamageMultiplier:0.#}\n散布半径 {p.ScatterRadius:0.#}　爆炸半径 {p.BlastRadius:0.#}";
                case StratagemKind.Laser: return common+$"持续 {p.Duration:0.#}秒\n每次照射伤害 {damage*p.DamageMultiplier:0.#}\n光束半径 {p.BlastRadius:0.##}　追踪速度 {p.TrackSpeed:0.#}";
                case StratagemKind.Eagle: return common+$"中心伤害 {damage*p.DamageMultiplier:0.#}\n内半径 {p.InnerRadius:0.#}　外半径 {p.BlastRadius:0.#}\n呼叫至爆炸 {1.8f+p.CallDelay:0.#}秒";
                case StratagemKind.Mortar: return common+$"射程 {range:0.#}\n区域半径 {p.BlastRadius:0.#}　持续 {p.FieldDuration:0.#}秒\n减速 {p.Slow*100:0}%　眩晕 {p.Stun:0.#}秒";
                case StratagemKind.Shield:
                    var shield = stats.GetComponent<StratagemShield>();
                    string capacity = shield == null ? $"{health*p.CapacityRatio:0}" : $"{shield.Capacity:0}/{health*p.CapacityRatio:0}";
                    return common+$"护罩半径 {p.BlastRadius:0.#}\n容量 {capacity}\n破盾重启 {p.Reboot:0.#}秒";
                default:
                    var rover = stats.GetComponent<StratagemRover>();
                    var sync = stats.GetComponent<联机_单位属性同步>();
                    string heat = instance ? $"{(rover != null ? rover.Heat : sync == null ? 0 : sync.进度条*p.HeatCap):0}/{p.HeatCap:0}" : $"{p.HeatCap:0}";
                    return common+$"每个基座1架无人机\n热量 {heat}　停靠充能 {p.DockTime:0.#}秒\n单次伤害 {damage*p.DamageMultiplier:0.#}　射程 {range:0.#}";
            }
        }
    }
    [HarmonyPatch(typeof(模块_基本属性),"概述")]
    internal static class StratagemOverview
    {
        private static bool Prefix(模块_基本属性 __instance,ref string __result)
        { if (!StratagemCatalog.IsUnit(__instance.ID) && __instance.ID != 20101 && __instance.ID != 20102) return true; __result = __instance.ID == 20101 ? "敌人触发后爆炸的地雷。" : __instance.ID == 20102 ? "随身护卫无人机，过热后返航充能。" : StratagemDescriptions.Summary(__instance.ID); return false; }
    }
    [HarmonyPatch(typeof(模块_基本属性),"说明")]
    internal static class StratagemDetails
    {
        private static bool Prefix(模块_基本属性 __instance,ref string __result)
        { if (!StratagemCatalog.IsUnit(__instance.ID) && __instance.ID != 20101 && __instance.ID != 20102) return true; __result = StratagemDescriptions.Details(__instance); return false; }
    }
}
