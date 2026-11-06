using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using 函数库;

namespace CluckmechCoop
{
    internal static class ThemeTest
    {
        internal static bool Active {get;private set;}
        internal static int Theme {get;private set;}
        internal static bool Eldritch {get;private set;}
        private static bool configured;
        internal static void Menu()
        {
            var modal=FeatureModal.Open("主题测试",800,510);if(modal==null)return;
            if(Plugin.Role!=0){NativeUi.Label(modal.Panel,"单人测试：请先离开联机大厅",30,110,730,60,25);return;}
            NativeUi.Label(modal.Panel,"选择主题，随后按原版选人和难度，再自由配装。",30,95,730,70,24);
            for(int i=0;i<4;i++)
            {
                int selection=i;
                NativeUi.TransferAction(modal.Panel,(i<2?"古堡":"深海")+" · "+(i%2==0?"常规":"异化"),30+i%2*375,210+i/2*105,350,75,()=>
                {Active=true;Theme=selection<2?1:2;Eldritch=selection%2!=0;configured=false;modal.Close();场景函数.切换选择英雄();});
            }
        }
        internal static void Leave(){Active=false;configured=false;}
        internal static void Finish()=>场景函数.切换场景(常数类.场景id_开始菜单);
        internal static IEnumerator MapReady(地图管理器 manager)
        {
            yield return null;
            if(!Active||configured||manager==null)yield break;
            configured=true;
            var set=manager.GetComponent<ThemeBranchMapSet>();var map=Plugin.Summary.地图存档;
            var anchor=map.地图页列表.Select((name,index)=>new{Page=set.NativePages.FirstOrDefault(p=>p.地图页名==name),Index=index})
                .First(x=>x.Page!=null&&(x.Page.所属场景=="焦香火山"||x.Page.所属场景=="废弃研究所"));
            map.当前地图页位置=anchor.Index;map.当前地图页=anchor.Page.地图页名;map.当前场景名=anchor.Page.所属场景;
            map.当前节点名=anchor.Page.地图页内节点列表.First(x=>x.是否起点).节点名;
            map.走过的节点.Clear();map.走过的节点.Add(map.当前节点名);map.节点作用列表.Clear();
            Plugin.Summary.当前拥有炮台.Clear();Plugin.Summary.背包选择初始化();
            var canvas=Object.FindObjectOfType<Canvas>();NativeUi.Capture(canvas.transform);
            var launcher=NativeUi.TransferAction(canvas.transform,"测试配装",28,30,220,60,()=>ThemeTestLoadout.Show());
            ThemeTestLoadout.Show();
        }
        internal static void StartBattles()
        {
            if(Plugin.Summary.当前拥有炮台.Count==0)return;
            FeatureModal.Clear();
            ThemeBranchRuntime.BeginSelected(Theme,new[]{Eldritch,Eldritch,Eldritch});
            场景函数.切换选择地图();
        }
    }
    public sealed class ThemeTestLoadout:MonoBehaviour
    {
        private FeatureModal modal;private RectTransform body;private 总结脚本.背包单元 selected;
        internal static void Show()
        {
            var modal=FeatureModal.Open("测试配装",1040,820);if(modal==null)return;
            var panel=modal.gameObject.AddComponent<ThemeTestLoadout>();panel.modal=modal;panel.Inventory();
        }
        private void Clear()
        {if(body!=null){body.gameObject.SetActive(false);Destroy(body.gameObject);}body=NativeUi.Rect(modal.Panel,"配装内容",24,90,992,700);}
        private void Inventory()
        {
            Clear();var bag=Plugin.Summary.当前拥有炮台;
            NativeUi.Label(body,"最多5台，可重复选择；强化使用原版上限与合法候选。",8,0,960,52,24);
            for(int i=0;i<bag.Count;i++)
            {
                var item=bag[i];var unit=Plugin.Units.获取单位(item.炮台id).GetComponent<模块_基本属性>();
                NativeUi.Image(NativeUi.Rect(body,"图标",12,70+i*94,70,70),unit.图标,Color.white).preserveAspect=true;
                NativeUi.Label(body,unit.名称()+"　"+item.强化词条.Count+"／"+item.强化上限,100,74+i*94,520,65,24);
                NativeUi.TransferAction(body,"强化",650,80+i*94,140,55,()=>{selected=item;Affixes();});
                NativeUi.TransferAction(body,"移除",810,80+i*94,140,55,()=>{bag.Remove(item);Inventory();});
            }
            var add=NativeUi.TransferAction(body,"添加鸡械",10,562,260,64,Machines);add.interactable=bag.Count<5;
            NativeUi.TransferAction(body,"退出测试",360,562,260,64,ThemeTest.Finish);
            var start=NativeUi.TransferAction(body,"开始测试",710,562,260,64,ThemeTest.StartBattles);start.interactable=bag.Count>0;
        }
        private void Machines()
        {
            Clear();NativeUi.TransferAction(body,"返回配装",8,0,210,52,Inventory);
            NativeUi.ScrollArea(body,"鸡械",8,74,948,570,out var content);
            var pool=Plugin.Units.单位列表.Where(x=>x.是否游戏内出现&&x.是否商店内出现&&x.单位等级>=1&&Plugin.Units.是否抽取(x.id)&&(!StratagemCatalog.IsUnit(x.id)||ModuleSettings.Extra.Stratagems)&&(x.id!=BronzeDragonRules.Unit||ModuleSettings.Extra.Bronze)).ToList();
            content.sizeDelta=new Vector2(948,Mathf.Max(570,((pool.Count+3)/4)*142));
            for(int i=0;i<pool.Count;i++)
            {
                int id=pool[i].id;var unit=Plugin.Units.获取单位(id).GetComponent<模块_基本属性>();
                var card=NativeUi.Rect(content,"鸡械",i%4*237,i/4*142,225,132);
                NativeUi.Image(NativeUi.Rect(card,"图标",77,4,70,70),unit.图标,Color.white).preserveAspect=true;
                NativeUi.TransferAction(card,System.Text.RegularExpressions.Regex.Replace(unit.名称(),@"</?color(?:=[^>]+)?>",""),4,80,217,48,()=>{if(Plugin.Summary.当前拥有炮台.Count<5)Plugin.Summary.获得炮台(id);Inventory();});
            }
        }
        private void Affixes()
        {
            Clear();NativeUi.TransferAction(body,"返回配装",8,0,210,52,Inventory);
            NativeUi.Label(body,selected.强化词条.Count+"／"+selected.强化上限,250,0,600,52,25);
            NativeUi.ScrollArea(body,"强化",8,74,948,570,out var content);
            var library=Object.FindObjectOfType<cz词缀库>();float y=0;
            foreach(int id in ArtisanEconomy.Choices(selected))
            {
                int choice=id;var data=library.获取词缀数据(id);
                var description=NativeUi.Label(content,data.获取词缀名称()+"\n"+data.获取词缀描述(),10,y,690,110,23);
                float height=Mathf.Max(120,description.preferredHeight+12);description.rectTransform.sizeDelta=new Vector2(690,height);
                NativeUi.TransferAction(content,"获得强化",720,y+20,210,62,()=>
                {if(ArtisanEconomy.Choices(selected).Contains(choice))Plugin.Summary.获得强化词条(Plugin.Summary.当前拥有炮台.IndexOf(selected),choice);Affixes();});
                y+=height+12;
            }
            content.sizeDelta=new Vector2(948,Mathf.Max(570,y));
        }
    }
    [HarmonyPatch(typeof(模块_主界面场景控制),"Start")]
    internal static class ThemeTestMenu
    {
        private static void Postfix(模块_主界面场景控制 __instance)
        {
            var canvas=__instance.GetComponentInParent<Canvas>()??Object.FindObjectOfType<Canvas>();if(canvas==null)return;
            NativeUi.Capture(canvas.transform);var button=NativeUi.TransferAction(canvas.transform,"主题测试",0,0,220,60,ThemeTest.Menu);
            var rect=button.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,0);rect.anchoredPosition=new Vector2(-28,28);
        }
    }
    [HarmonyPatch(typeof(地图管理器),"Start")]
    internal static class ThemeTestMapReady
    {private static void Postfix(地图管理器 __instance){if(ThemeTest.Active)Plugin.Instance.StartCoroutine(ThemeTest.MapReady(__instance));}}
    [HarmonyPatch]
    internal static class ThemeTestSaveGuard
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {foreach(string name in new[]{"保存","删除存档","删除备用存档","删除未加密存档"})yield return AccessTools.Method(typeof(存档脚本),name);}
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(存档脚本 __instance)=>!ThemeTest.Active||!RunSaveRepair.IsRunSlot(__instance);
    }
    [HarmonyPatch]
    internal static class ThemeTestStatisticsGuard
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {foreach(string name in new[]{"单局统计保存","覆盖存档单局统计保存","通关或失败统计保存"})yield return AccessTools.Method(typeof(总结脚本),name);}
        private static bool Prefix()=>!ThemeTest.Active;
    }
    [HarmonyPatch(typeof(场景函数),"切换场景",new[]{typeof(int)})]
    internal static class ThemeTestFailureReturn
    {private static void Prefix(ref int i){if(ThemeTest.Active&&i==常数类.场景id_游戏失败)i=常数类.场景id_开始菜单;}}
}
