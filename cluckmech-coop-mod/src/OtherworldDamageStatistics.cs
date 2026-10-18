using System;
using System.Collections.Generic;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class OtherworldDamageStatistics
    {
        private static 模块_伤害数据统计 display;
        internal static void Clear()=>display=null;
        internal static void Publish(模块_伤害数据统计 statistics)
        {
            if(Plugin.Role!=1||!Plugin.InCombat||!OtherworldWorld.Active||!Plugin.Network.PeerReady)return;
            OtherworldNetwork.Send(new OtherworldMessage{Kind=OtherworldMessageKind.DamageStatistics,
                Data=JsonConvert.SerializeObject(statistics.伤害统计列表)},2);
        }
        internal static void Receive(string data)
        {
            if(!Plugin.InCombat||string.IsNullOrEmpty(data))return;
            List<模块_伤害数据统计.伤害信息统计类> entries;
            try{entries=JsonConvert.DeserializeObject<List<模块_伤害数据统计.伤害信息统计类>>(data);}
            catch(JsonException){return;}
            if(entries==null)return;
            foreach(var entry in entries)
                if(entry==null||!Enum.IsDefined(typeof(模块_伤害数据统计.攻击者种类枚举),entry.攻击者种类)
                    ||float.IsNaN(entry.伤害值)||float.IsInfinity(entry.伤害值)||entry.伤害值<0)return;
            if(display==null)display=UnityEngine.Object.FindObjectOfType<模块_伤害数据统计>();
            if(display==null)return;
            display.伤害统计列表.Clear();display.伤害统计列表.AddRange(entries);
            display.更新名次();
        }
    }
    [HarmonyPatch(typeof(模块_伤害数据统计),"更新名次")]
    internal static class OtherworldDamageStatisticsPublish
    {private static void Postfix(模块_伤害数据统计 __instance)=>OtherworldDamageStatistics.Publish(__instance);}
    [HarmonyPatch(typeof(模块_伤害数据统计),"统计伤害数据")]
    internal static class OtherworldDamageStatisticsClient
    {
        private static bool Prefix()=>Plugin.Role!=2||!Plugin.InCombat||!OtherworldWorld.Active||!Plugin.Network.PeerReady;
    }
}
