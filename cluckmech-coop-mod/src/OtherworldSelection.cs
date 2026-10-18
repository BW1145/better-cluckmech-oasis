using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Reflection;

namespace CluckmechCoop
{
    internal static class OtherworldSelection
    {
        internal static void Attach(选择英雄缓存器脚本 cache)
        {
            if(!OtherworldRegistry.Enabled)return;
            Canvas.ForceUpdateCanvases();
            NativeUi.Capture(cache.GetComponentInParent<Canvas>()?.transform??Object.FindObjectOfType<Canvas>().transform);
            var toggles=Object.FindObjectsOfType<Toggle>(true);
            var originals=toggles.Where(t=>HeroToggle(t)).ToArray();
            if(originals.Length!=12)return;
            var template=originals.First(t=>Enumerable.Range(0,t.onValueChanged.GetPersistentEventCount()).Any(i=>
                t.onValueChanged.GetPersistentTarget(i) is GameObject target && target.name=="角色描述区_战斗鸡"));
            var statistics=Object.FindObjectOfType<全局存档>();
            while(statistics.角色统计列表.Count<14)statistics.角色统计列表.Add(new 全局存档.角色全局统计类());
            var targets=new List<GameObject>();UI焦点窗口 focus=null;
            for(int i=0;i<template.onValueChanged.GetPersistentEventCount();i++)
            {
                var target=template.onValueChanged.GetPersistentTarget(i);
                if(template.onValueChanged.GetPersistentMethodName(i)=="SetActive" && target is GameObject obj)targets.Add(obj);
                if(target is UI焦点窗口 window)focus=window;
            }
            float rowTolerance=((RectTransform)template.transform).rect.height*Mathf.Abs(template.transform.lossyScale.y)*.3f;
            var topRow=originals.Where(t=>Mathf.Abs(t.transform.position.y-template.transform.position.y)<rowTolerance).OrderBy(t=>t.transform.position.x).ToArray();
            var nativeSecond=topRow[1];var rightmost=topRow[topRow.Length-1];
            Vector3 step=nativeSecond.transform.position-template.transform.position;
            var additions=new List<Toggle>();
            foreach(var hero in OtherworldCatalog.Heroes)
            {
                var panels=new List<GameObject>();
                foreach(var target in targets)
                {
                    bool worldPreview=target.GetComponent<RectTransform>()==null;
                    GameObject panel;
                    if(worldPreview)panel=new GameObject(target.name);
                    else
                    {
                        var staging=new GameObject("OtherworldUiStaging");staging.SetActive(false);
                        panel=Object.Instantiate(target,staging.transform);panel.SetActive(false);
                        DisableLocalization(panel);panel.transform.SetParent(target.transform.parent,false);Object.Destroy(staging);
                    }
                    if(worldPreview)
                    {
                        panel.transform.SetParent(target.transform.parent,false);panel.transform.localPosition=target.transform.localPosition;
                        panel.transform.localRotation=target.transform.localRotation;panel.transform.localScale=target.transform.localScale;
                    }
                    panel.name=target.name+"_"+hero.Id;panel.SetActive(false);
                    DisableLocalization(panel);
                    if(panel.name.StartsWith("角色描述区"))Describe(panel,hero);
                    else if(panel.name.StartsWith("选角界面_天赋区"))
                    {
                        var talents=panel.GetComponentsInChildren<天赋单元>(true);
                        for(int i=0;i<talents.Length;i++)
                        {
                            talents[i].天赋id=(hero.Id==12?26001:26005)+i;
                            TalentIcon(talents[i]);
                        }
                    }
                    else if(panel.TryGetComponent<Text>(out var name))
                    {
                        name.text=hero.Name;name.alignment=TextAnchor.MiddleLeft;
                        name.rectTransform.pivot=new Vector2(0,.5f);
                        name.rectTransform.position=new Vector3(rightmost.transform.position.x+step.x*2.5f+12*template.transform.lossyScale.x,target.transform.position.y,target.transform.position.z);
                        name.fontSize=Mathf.Min(name.fontSize,54);
                    }
                    else
                    {
                        var bodies=worldPreview?target.GetComponentsInChildren<SpriteRenderer>(true):panel.GetComponentsInChildren<SpriteRenderer>(true);
                        if(!worldPreview)foreach(var body in bodies)body.enabled=false;
                        var visual=new GameObject("Character");visual.transform.SetParent(panel.transform,false);
                        var sprite=visual.AddComponent<SpriteRenderer>();sprite.sprite=OtherworldAssets.Hero(hero.Id);
                        if(bodies.Length>0)
                        {
                            sprite.sortingLayerID=bodies[0].sortingLayerID;sprite.sortingOrder=bodies[0].sortingOrder;
                            visual.transform.localScale=Vector3.one*(bodies[0].sprite.bounds.size.y/OtherworldAssets.VisibleBounds(sprite.sprite).size.y);
                            visual.transform.localPosition=Vector3.up*bodies[0].sprite.bounds.min.y;
                        }
                    }
                    panels.Add(panel);
                }
                var clone=Object.Instantiate(template,template.transform.parent);clone.gameObject.SetActive(false);
                clone.name="OtherworldSelection_"+hero.Id;clone.onValueChanged=new Toggle.ToggleEvent();clone.SetIsOnWithoutNotify(false);
                clone.transform.position=rightmost.transform.position+step*(hero.Id-11);
                additions.Add(clone);
                var automatic=clone.GetComponent<UI_勾选初始化>();if(automatic!=null)Object.Destroy(automatic);
                var unlock=clone.GetComponent<UI_内容解锁_角色按钮>();if(unlock!=null)unlock.角色id=hero.Id;
                if(unlock?.锁定图片!=null)unlock.锁定图片.SetActive(false);
                foreach(var trigger in clone.GetComponents<EventTrigger>())trigger.triggers.RemoveAll(entry=>
                    Enumerable.Range(0,entry.callback.GetPersistentEventCount()).Any(i=>entry.callback.GetPersistentTarget(i) is UI_内容解锁_角色按钮));
                foreach(var image in clone.GetComponentsInChildren<Image>(true))
                {
                    if(image.name=="角色头像"&&(unlock?.锁定图片==null||!image.transform.IsChildOf(unlock.锁定图片.transform)))
                    {
                        image.sprite=OtherworldAssets.Portrait(hero.Id);image.type=Image.Type.Simple;image.preserveAspect=true;image.color=Color.white;
                        var rect=image.rectTransform;rect.SetParent(clone.transform,false);rect.SetAsLastSibling();
                        rect.anchorMin=new Vector2(.1f,.24f);rect.anchorMax=new Vector2(.9f,.88f);rect.offsetMin=rect.offsetMax=Vector2.zero;
                        rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;
                    }
                    if(image.name=="未解锁" || image.name=="DLC角标" || image.name=="难度描边")image.gameObject.SetActive(false);
                }
                clone.interactable=true;
                clone.onValueChanged.AddListener(on=>
                {
                    foreach(var panel in panels)panel.SetActive(on);
                    if(!on)return;
                    foreach(var panel in panels)foreach(var cell in panel.GetComponentsInChildren<UI_单位格子>(true))cell.传入单位(hero.StartingMachine);
                    cache.当前选择的英雄(hero.Id);
                    foreach(var panel in panels)
                    {
                        var first=panel.GetComponentsInChildren<天赋单元>(true).OrderBy(t=>t.天赋id).FirstOrDefault();
                        if(first!=null)
                        {
                            var toggle=first.GetComponent<Toggle>();toggle.isOn=false;toggle.isOn=true;
                            cache.选择天赋(first.天赋id);
                        }
                    }
                    focus?.触发激活窗口("选择英雄_选择天赋");
                });
                clone.gameObject.SetActive(true);
            }
            Link(rightmost,additions[0]);Link(additions[0],additions[1]);
        }
        private static void Link(Toggle left,Toggle right)
        {
            var a=left.navigation;
            if(a.mode!=Navigation.Mode.Explicit){a.selectOnLeft=left.FindSelectableOnLeft();a.selectOnUp=left.FindSelectableOnUp();a.selectOnDown=left.FindSelectableOnDown();}
            a.mode=Navigation.Mode.Explicit;a.selectOnRight=right;left.navigation=a;
            var b=right.navigation;
            if(b.mode!=Navigation.Mode.Explicit){b.selectOnRight=right.FindSelectableOnRight();b.selectOnUp=right.FindSelectableOnUp();b.selectOnDown=right.FindSelectableOnDown();}
            b.mode=Navigation.Mode.Explicit;b.selectOnLeft=left;right.navigation=b;
        }
        private static void TalentIcon(天赋单元 talent)
        {
            var toggle=talent.GetComponent<Toggle>();var mark=talent.天赋图标;var background=mark.transform.parent.GetComponent<Image>();
            background.sprite=NativeUi.TransferButton;background.type=Image.Type.Sliced;background.overrideSprite=null;
            mark.sprite=NativeUi.Selection;mark.overrideSprite=null;mark.type=Image.Type.Sliced;mark.preserveAspect=false;
            toggle.interactable=true;toggle.transition=Selectable.Transition.ColorTint;toggle.graphic=mark;
            var obj=new GameObject("TalentIcon",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
            var rect=(RectTransform)obj.transform;rect.SetParent(background.transform,false);rect.anchorMin=Vector2.one*.12f;rect.anchorMax=Vector2.one*.88f;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var image=obj.GetComponent<Image>();image.sprite=OtherworldRegistry.Talents[talent.天赋id].图标;image.preserveAspect=true;image.raycastTarget=false;
            talent.天赋图标=image;
        }

        private static bool HeroToggle(Toggle toggle)
        {
            for(int i=0;i<toggle.onValueChanged.GetPersistentEventCount();i++)
                if(toggle.onValueChanged.GetPersistentMethodName(i)=="当前选择的英雄")return true;
            return false;
        }
        private static void DisableLocalization(GameObject root)
        {
            foreach(var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                if(behaviour.GetType().Name=="LocalizeStringEvent"&&behaviour.name!="Label")behaviour.enabled=false;
        }
        private static void Describe(GameObject panel,OtherworldHero hero)
        {
            bool perlica=hero.Id==12;int title=0,description=0;
            foreach(var text in panel.GetComponentsInChildren<Text>(true))
            {
                if(text.name=="角色名字")text.text=hero.Name;
                else if(text.name=="角色定位")text.text=perlica?"电磁轰击与战备调度":"虚化突袭与神威空间";
                else if(text.name=="技能名字")text.text=title++==0?(perlica?"普通攻击·电磁弹":"普通攻击·苦无"): (perlica?"主动技能·协议Ω":"主动技能·神威");
                else if(text.name=="技能描述")text.text=description++==0?(perlica?"发射电磁弹攻击敌人":"向瞄准方向投掷苦无"):
                    perlica?"雷击指定区域，造成攻击力×12伤害并麻痹5秒；冷却8秒":"短按虚化加速，长按冲刺并将沿途敌人带入神威空间；空间内按右键返回";
                else if(text.text=="500")text.text=hero.Health.ToString("0");
                else if(text.text=="20")text.text=hero.Attack.ToString("0");
                else if(text.text=="4")text.text=hero.Speed.ToString("0.#");
            }
            foreach(var image in panel.GetComponentsInChildren<Image>(true))
                if(image.name=="角色头像")image.sprite=OtherworldAssets.Portrait(hero.Id);
        }
    }
    [HarmonyPatch(typeof(选择英雄缓存器脚本),"Start")]
    internal static class OtherworldCharacterMenu
    {private static void Postfix(选择英雄缓存器脚本 __instance)=>OtherworldSelection.Attach(__instance);}

    [HarmonyPatch]
    internal static class OtherworldScenePortraits
    {
        private static readonly System.Type[] screens={typeof(UI_暂停菜单信息显示),typeof(UI_结算场景信息显示),typeof(模块_事件场景控制),typeof(模块_失败场景控制),typeof(模块_通关场景控制),typeof(模块_最终BOSS场景控制)};
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach(var type in screens)foreach(string name in new[]{"Start","OnEnable"})
            {var method=type.GetMethod(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);if(method!=null)yield return method;}
        }
        internal static GameObject Portrait(GameObject template,OtherworldHero hero,bool pause=false)
        {
            var clone=Object.Instantiate(template,template.transform.parent);clone.name=hero.Name;clone.SetActive(false);
            if(pause)
            {
                // The pause prefab is a layered animated portrait, not a single
                // image. Its first Image is not the body, and the chicken Animator
                // otherwise writes native sprites back over our replacement.
                foreach(var animator in clone.GetComponentsInChildren<Animator>(true))animator.enabled=false;
                var body=clone.transform.Find("角色")?.GetComponent<Image>();
                var hand=clone.transform.Find("手");if(hand!=null)hand.gameObject.SetActive(false);
                if(body!=null)
                {
                    body.sprite=OtherworldAssets.Portrait(hero.Id);body.overrideSprite=null;
                    body.type=Image.Type.Simple;body.preserveAspect=true;body.color=Color.white;
                }
                foreach(var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
                    if(behaviour.GetType().Name=="LocalizeStringEvent")behaviour.enabled=false;
                var tooltip=clone.transform.Find("详细信息提示Icon/角色技能") as RectTransform;
                if(tooltip!=null)tooltip.sizeDelta=new Vector2(460,270);
                foreach(var text in clone.GetComponentsInChildren<Text>(true))
                {
                    bool perlica=hero.Id==12;
                    if(text.name=="技能1名字")text.text=perlica?"普通攻击·电磁弹":"普通攻击·苦无";
                    else if(text.name=="技能1描述")text.text=perlica?"发射电磁弹攻击敌人":"向瞄准方向投掷苦无";
                    else if(text.name=="技能2名字")text.text=perlica?"主动技能·协议Ω":"主动技能·神威";
                    else if(text.name=="技能2描述")text.text=perlica?"雷击指定区域，造成攻击力×12伤害并麻痹5秒；冷却8秒":"短按虚化加速，长按冲刺并将沿途敌人带入神威空间；空间内按右键返回";
                    else continue;
                    int row=text.name=="技能1名字"?0:text.name=="技能1描述"?1:text.name=="技能2名字"?2:3;
                    var rect=text.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
                    rect.anchoredPosition=new Vector2(15,-15-row*45);rect.sizeDelta=new Vector2(430,row==3?105:40);
                    text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
                    text.resizeTextForBestFit=true;text.resizeTextMinSize=16;text.resizeTextMaxSize=24;
                }
                return clone;
            }
            var image=clone.GetComponentInChildren<Image>(true);
            if(image!=null){image.sprite=OtherworldAssets.Portrait(hero.Id);image.preserveAspect=true;}
            else
            {
                var renderers=clone.GetComponentsInChildren<SpriteRenderer>(true);
                foreach(var renderer in renderers)renderer.forceRenderingOff=true;
                if(renderers.Length>0)
                {
                    var source=renderers[0];var body=new GameObject("OtherworldPortrait");body.transform.SetParent(source.transform,false);
                    var sprite=body.AddComponent<SpriteRenderer>();sprite.sprite=OtherworldAssets.Hero(hero.Id);
                    sprite.sortingLayerID=source.sortingLayerID;sprite.sortingOrder=source.sortingOrder;
                    body.transform.localScale=Vector3.one*(source.sprite.bounds.size.y/OtherworldAssets.VisibleBounds(sprite.sprite).size.y);
                }
            }
            return clone;
        }
        private static void Prefix(MonoBehaviour __instance)
        {
            var field=AccessTools.Field(__instance.GetType(),"角色");if(field==null)return;
            if(field.GetValue(__instance) is Sprite[] sprites)field.SetValue(__instance,OtherworldRegistry.Portraits(sprites));
            else if(field.GetValue(__instance) is GameObject[] objects&&objects.Length>0&&objects.Length<14)
            {
                var expanded=new GameObject[14];System.Array.Copy(objects,expanded,objects.Length);
                foreach(var hero in OtherworldCatalog.Heroes)expanded[hero.Id]=Portrait(objects[0],hero,__instance is UI_暂停菜单信息显示);
                field.SetValue(__instance,expanded);
            }
            var names=AccessTools.Field(__instance.GetType(),"角色名字");
            if(names?.GetValue(__instance) is string[] labels&&labels.Length<14)
            {var expanded=new string[14];System.Array.Copy(labels,expanded,labels.Length);expanded[12]="佩丽卡";expanded[13]="宇智波带土";names.SetValue(__instance,expanded);}
        }
    }
    [HarmonyPatch(typeof(联机_英雄选择),"Start")]
    internal static class OtherworldPeerPortraits
    {
        private static void Prefix(联机_英雄选择 __instance)
        {
            var list=__instance.客户端角色待选列表;if(list.Count==0||list.Count>=14)return;
            foreach(var hero in OtherworldCatalog.Heroes)list.Add(OtherworldScenePortraits.Portrait(list[0],hero));
        }
    }

    [HarmonyPatch(typeof(天赋选择),"随机角色天赋运算")]
    internal static class OtherworldGrowthCandidates
    {
        private static bool Prefix(天赋选择 __instance,总结脚本 ___总结,数据库_角色天赋 ___角色天赋库,
            ref 角色天赋数据模板 ___随机天赋1,ref 角色天赋数据模板 ___随机天赋2,ref 角色天赋数据模板 ___随机天赋3)
        {
            int hero=___总结.单局统计数据.使用角色;
            if(OtherworldCatalog.Hero(hero)==null)return true;
            var initial=___总结.单局统计数据.角色初始天赋;
            var owned=new HashSet<int>(___总结.单局统计数据.角色天赋列表.Select(t=>t.id));
            var candidates=OtherworldCatalog.Talents.Where(t=>OtherworldCatalog.Eligible(t,hero,initial.id,owned)).Select(t=>___角色天赋库.查询角色天赋(t.Id)).ToList();
            if(candidates.Count==0)
            {
                __instance.天赋加成模式=天赋选择.天赋加成模式枚举.全局通用加成;
                AccessTools.Method(typeof(天赋选择),"随机全局加成运算").Invoke(__instance,null);return false;
            }
            var selected=new 角色天赋数据模板[3];
            for(int i=0;i<3&&candidates.Count>0;i++)
            {int index=Random.Range(0,candidates.Count);selected[i]=candidates[index];candidates.RemoveAt(index);}
            ___随机天赋1=selected[0];___随机天赋2=selected[1];___随机天赋3=selected[2];
            Card(__instance.天赋2名字).SetActive(selected[1]!=null);
            Card(__instance.天赋3名字).SetActive(selected[2]!=null);
            return false;
        }
        private static GameObject Card(Text title)
        {
            var selectable=title.GetComponentInParent<Selectable>();return selectable!=null?selectable.gameObject:title.transform.parent.gameObject;
        }
    }
    [HarmonyPatch(typeof(天赋选择),"选择")]
    internal static class OtherworldGrowthSelection
    {
        private static bool Prefix(天赋选择 __instance,int 选择值,角色天赋数据模板 ___随机天赋1,角色天赋数据模板 ___随机天赋2,角色天赋数据模板 ___随机天赋3)
            =>OtherworldCatalog.Hero(Plugin.Summary?.单局统计数据.使用角色??-1)==null||(选择值==0?___随机天赋1:选择值==1?___随机天赋2:___随机天赋3)!=null;
    }
}
