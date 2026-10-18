using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Mirror;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class OtherworldRegistry
    {
        private static GameObject templates;
        private static readonly Dictionary<int,GameObject> prefabs=new Dictionary<int,GameObject>();
        internal static readonly Dictionary<int,角色天赋数据模板> Talents=new Dictionary<int,角色天赋数据模板>();
        internal static bool Enabled=>ModuleSettings.InRun?ModuleSettings.Extra.Otherworld:ModuleSettings.Preference(ModuleId.Otherworld);
        internal static void Install(yx英雄库 library)
        {
            if(library.英雄列表.Count==0)return;
            if(templates==null)
            {
                templates=new GameObject("OtherworldTemplates");templates.SetActive(false);UnityEngine.Object.DontDestroyOnLoad(templates);
                foreach(var definition in OtherworldCatalog.Heroes)
                {
                    var prefab=UnityEngine.Object.Instantiate(library.获取英雄(0),templates.transform);
                    prefab.name="OtherworldHero_"+definition.Id;
                    var hp=prefab.GetComponent<模块_基本属性>();hp.ID=definition.UnitId;
                    hp.生命值=hp.当前生命值=definition.Health;hp.生命再生=5;hp.抗性=hp.格挡值=0;
                    hp.图标=hp.名片=OtherworldAssets.Portrait(definition.Id);
                    var attack=prefab.GetComponent<模块_攻击属性>();attack.攻击力=definition.Attack;attack.攻击速度=definition.Rate;attack.攻击范围=definition.Range;
                    var bullet=UnityEngine.Object.Instantiate(attack.子弹,templates.transform);bullet.name="OtherworldProjectile_"+definition.Id;
                    bullet.AddComponent<OtherworldProjectile>().Hero=definition.Id;attack.子弹=bullet;
                    attack.当前造成状态=new List<攻击类型与状态数据库.状态枚举>();
                    var detailed=prefab.GetComponent<模块_攻击详细属性>();detailed.暴击率=.1f;detailed.暴击倍率=1;
                    var move=prefab.GetComponent<模块_移动属性>();move.移动速度=definition.Speed;
                    prefab.GetComponent<模块_玩家_技能>().技能cd=definition.Id==12?8:10;
                    prefab.AddComponent<OtherworldActor>();
                    var identity=prefab.GetComponent<NetworkIdentity>();
                    AccessTools.Field(typeof(NetworkIdentity),"_assetId").SetValue(identity,(uint)(0xBC060000+definition.Id));identity.sceneId=0;
                    prefabs.Add(definition.Id,prefab);
                }
            }
            foreach(var definition in OtherworldCatalog.Heroes)
            {
                if(library.获取角色数据(definition.Id)!=null)continue;
                var data=ScriptableObject.CreateInstance<角色数据模板>();data.id=definition.Id;data.name=definition.Name;
                data.单位预制体=prefabs[definition.Id];data.固定单位奖励=new List<int>{definition.StartingMachine};
                library.英雄列表.Add(new yx英雄库.英雄单元{角色数据=data,是否解锁=true});
            }
            Register();
        }
        internal static void Register()
        {foreach(var prefab in prefabs.Values)NetworkClient.RegisterPrefab(prefab);}
        internal static void Install(数据库_角色天赋 library)
        {
            foreach(var definition in OtherworldCatalog.Talents)
            {
                if(library.查询角色天赋(definition.Id)!=null)continue;
                if(!Talents.TryGetValue(definition.Id,out var data))
                {
                    data=ScriptableObject.CreateInstance<角色天赋数据模板>();data.id=definition.Id;data.name=definition.Name;
                    data.是否指定角色=true;data.指定角色id=definition.Hero;data.是否录入随机池=!definition.Initial;
                    data.图标=data.卡牌图标=OtherworldAssets.Named(definition.Id==26008?"kamui.0.7":"talent."+definition.Id);
                    Talents.Add(data.id,data);
                }
                library.角色天赋数据列表.Add(data);
            }
        }
        internal static Sprite[] Portraits(Sprite[] originals)
        {
            if(originals==null || originals.Length>=14)return originals;
            var expanded=new Sprite[14];Array.Copy(originals,expanded,originals.Length);
            expanded[12]=OtherworldAssets.Portrait(12);expanded[13]=OtherworldAssets.Portrait(13);return expanded;
        }
    }
    [HarmonyPatch(typeof(yx英雄库),"Awake")]
    internal static class RegisterOtherworldHeroes { private static void Prefix(yx英雄库 __instance)=>OtherworldRegistry.Install(__instance); }
    [HarmonyPatch(typeof(数据库_角色天赋),"OnEnable")]
    internal static class RegisterOtherworldTalents { private static void Prefix(数据库_角色天赋 __instance)=>OtherworldRegistry.Install(__instance); }
    [HarmonyPatch(typeof(NetworkClient),"PrepareToSpawnSceneObjects")]
    internal static class OtherworldNetworkPrefabs { private static void Postfix()=>OtherworldRegistry.Register(); }
    [HarmonyPatch(typeof(角色天赋数据模板),"名称")]
    internal static class OtherworldTalentName
    { private static bool Prefix(角色天赋数据模板 __instance,ref string __result) {var t=OtherworldCatalog.Talent(__instance.id);if(t==null)return true;__result=t.Name;return false;} }
    [HarmonyPatch(typeof(角色天赋数据模板),"介绍")]
    internal static class OtherworldTalentDescription
    { private static bool Prefix(角色天赋数据模板 __instance,ref string __result) {var t=OtherworldCatalog.Talent(__instance.id);if(t==null)return true;__result=t.Description;return false;} }
    [HarmonyPatch(typeof(模块_基本属性),"名称")]
    internal static class OtherworldUnitName
    { private static bool Prefix(模块_基本属性 __instance,ref string __result) {if(__instance.ID<22000)return true;var h=OtherworldCatalog.Hero(__instance.ID);if(h==null)return true;__result=h.Name;return false;} }
    [HarmonyPatch(typeof(天赋选择),"Start")]
    internal static class OtherworldTalentPortrait { private static void Prefix(天赋选择 __instance)=>__instance.角色List=OtherworldRegistry.Portraits(__instance.角色List); }
}
