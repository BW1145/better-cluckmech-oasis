using System;
using System.Collections.Generic;
using HarmonyLib;
using Mirror;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CluckmechCoop
{
    internal static class BronzeDragonRegistry
    {
        private static GameObject templates,prefab;
        private static string coloredName;
        internal static bool Enabled=>ModuleSettings.InRun?ModuleSettings.Extra.Bronze:ModuleSettings.Preference(ModuleId.Bronze);
        internal static string Name=>coloredName;
        internal static void Install(dw单位库 library)
        {
            if(prefab==null)
            {
                templates=new GameObject("BronzeDragonTemplates");templates.SetActive(false);Object.DontDestroyOnLoad(templates);
                prefab=Object.Instantiate(library.获取单位(93),templates.transform);prefab.name="青铜龙雕像";
                Object.DestroyImmediate(prefab.GetComponent<词条_244_黄金龙雕像>());
                var stats=prefab.GetComponent<模块_基本属性>();
                stats.ID=BronzeDragonRules.Unit;stats.母体id=stats.背包id位置=-1;stats.强化词条列表=new List<int>();
                stats.花费=BronzeDragonRules.Power;stats.放置cd=BronzeDragonRules.Cooldown;stats.生命值=BronzeDragonRules.Health;
                stats.当前生命值=stats.当前生命值上限=0;stats.格挡值=stats.抗性=stats.生命再生=0;
                var sample=library.获取单位(6).GetComponent<模块_基本属性>();stats.稀有度=sample.稀有度;
                string sampleName=sample.名称();int begin=sampleName.IndexOf("<color=",StringComparison.OrdinalIgnoreCase),end=begin<0?-1:sampleName.IndexOf('>',begin);
                coloredName=end<0?"青铜龙雕像":sampleName.Substring(begin,end-begin+1)+"青铜龙雕像</color>";
                stats.图标=BronzeDragonPalette.Sprite(stats.图标);stats.名片=stats.图标;
                stats.预放置图片=BronzeDragonPalette.Sprite(stats.预放置图片);
                var body=prefab.GetComponent<SpriteRenderer>();body.sprite=BronzeDragonPalette.Sprite(body.sprite);
                prefab.AddComponent<BronzeDragon>();
                var identity=prefab.GetComponent<NetworkIdentity>();
                AccessTools.Field(typeof(NetworkIdentity),"_assetId").SetValue(identity,(uint)0xBC070001);identity.sceneId=0;
            }
            if(!library.单位是否存在(BronzeDragonRules.Unit))library.单位列表.Add(new dw单位库.单位单元{
                id=BronzeDragonRules.Unit,单位预制体=prefab,单位阵营=dw单位库.单位单元.阵营枚举.友方,
                稀有度=2,单位等级=2,升级词缀上限=BronzeDragonRules.Cap,默认解锁=true,
                是否游戏内出现=true,是否商店内出现=true,是否工匠内出现=true});
            RegisterNetwork();
        }
        internal static void RegisterNetwork(){if(prefab!=null)NetworkClient.RegisterPrefab(prefab);}
        internal static void Install(cz词缀库 library)
        {
            for(int i=0;i<BronzeDragonRules.SpecialAffixes.Length;i++)
            {
                int id=BronzeDragonRules.SpecialAffixes[i];if(library.词缀列表.Exists(a=>a.id==id))continue;
                var affix=ScriptableObject.CreateInstance<词条数据模板>();affix.id=id;affix.name=BronzeDragonRules.Names[i];
                affix.是否唯一=true;affix.禁止继承=true;
                affix.图标=library.获取词缀数据(new[]{244,153,150,324}[i]).图标;
                library.词缀列表.Add(affix);
            }
        }
        internal static void Install(cz升级词缀库 library)
        {
            if(!library.升级方案列表.Exists(p=>p.炮台id==BronzeDragonRules.Unit))
                library.升级方案列表.Add(new cz升级词缀库.升级方案单元{炮台id=BronzeDragonRules.Unit,升级词缀id=BronzeDragonRules.Pool()});
        }
        internal static string Overview(模块_基本属性 stats)
        {
            var affixes=stats.强化词条列表;int count=affixes.Contains(25101)?2:1,seconds=affixes.Contains(25102)?6:10;
            return "<color=orange>强化方向：金币经济、动力经济、生存</color>\n"+
                $"·每{seconds}秒生产{count}金币\n·不攻击敌人"+
                (affixes.Contains(25103)?"\n·每完成5次生产，额外生产2金币":"")+
                (affixes.Contains(25104)?"\n·放置时立即生产3金币":"");
        }
    }
    [HarmonyPatch(typeof(dw单位库),"OnEnable")]
    internal static class RegisterBronzeDragon {private static void Prefix(dw单位库 __instance)=>BronzeDragonRegistry.Install(__instance);}
    [HarmonyPatch(typeof(cz词缀库),"Start")]
    internal static class RegisterBronzeAffixes {private static void Prefix(cz词缀库 __instance)=>BronzeDragonRegistry.Install(__instance);}
    [HarmonyPatch(typeof(cz升级词缀库),"Start")]
    internal static class RegisterBronzePool {private static void Prefix(cz升级词缀库 __instance)=>BronzeDragonRegistry.Install(__instance);}
    [HarmonyPatch(typeof(NetworkClient),"PrepareToSpawnSceneObjects")]
    internal static class RegisterBronzeNetwork {private static void Postfix()=>BronzeDragonRegistry.RegisterNetwork();}
    [HarmonyPatch(typeof(dw单位库),"是否抽取")]
    internal static class BronzeAvailability
    {private static bool Prefix(int id,ref bool __result){if(id!=BronzeDragonRules.Unit||BronzeDragonRegistry.Enabled)return true;__result=false;return false;}}
    [HarmonyPatch(typeof(模块_基本属性),"名称")]
    internal static class BronzeName
    {private static bool Prefix(模块_基本属性 __instance,ref string __result){if(__instance.ID!=BronzeDragonRules.Unit)return true;__result=BronzeDragonRegistry.Name;return false;}}
    [HarmonyPatch(typeof(模块_基本属性),"概述")]
    internal static class BronzeOverview
    {private static bool Prefix(模块_基本属性 __instance,ref string __result){if(__instance.ID!=BronzeDragonRules.Unit)return true;__result=BronzeDragonRegistry.Overview(__instance);return false;}}
    [HarmonyPatch(typeof(模块_基本属性),"说明")]
    internal static class BronzeDescription
    {private static bool Prefix(模块_基本属性 __instance,ref string __result){if(__instance.ID!=BronzeDragonRules.Unit)return true;__result="青铜铸成的盘龙雕像，龙口定时吐出金币。";return false;}}
    [HarmonyPatch(typeof(cz词缀库),"获取词缀名称")]
    internal static class BronzeAffixName
    {private static bool Prefix(int 词条id,ref string __result){if(!BronzeDragonRules.Special(词条id))return true;__result=BronzeDragonRules.Names[词条id-25101];return false;}}
    [HarmonyPatch(typeof(cz词缀库),"获取词缀描述")]
    internal static class BronzeAffixDescription
    {private static bool Prefix(int 词条id,ref string __result){if(!BronzeDragonRules.Special(词条id))return true;__result="<color=orange>唯一强化</color>\n"+BronzeDragonRules.Effects[词条id-25101];return false;}}
    [HarmonyPatch(typeof(词条数据模板),"获取词缀名称")]
    internal static class BronzeAffixDataName
    {private static bool Prefix(词条数据模板 __instance,ref string __result){if(!BronzeDragonRules.Special(__instance.id))return true;__result=BronzeDragonRules.Names[__instance.id-25101];return false;}}
    [HarmonyPatch(typeof(词条数据模板),"获取词缀描述")]
    internal static class BronzeAffixDataDescription
    {private static bool Prefix(词条数据模板 __instance,ref string __result){if(!BronzeDragonRules.Special(__instance.id))return true;__result="<color=orange>唯一强化</color>\n"+BronzeDragonRules.Effects[__instance.id-25101];return false;}}
}
