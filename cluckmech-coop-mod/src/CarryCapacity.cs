using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CluckmechCoop
{
    internal static class CarryCapacity
    {
        private static Sprite sixthKey;
        internal static int Slots => SlotsFor(Plugin.Role);
        internal static int SlotsFor(int role) => 5+(ModuleSettings.Extra.Growth ? ModuleSettings.Progress.Player(role).ExtraCarrySlots : 0);
        internal static void Apply()
        {
            模块_玩家_动力.动力上限=20+(ModuleSettings.InRun && ModuleSettings.Extra.Growth ? ModuleSettings.LocalProgress.ExtraPowerCapacity : 0);
            if(!ModuleSettings.InRun || RunSaveRepair.AtMainMenu || Plugin.Summary==null)return;
            var carried=Plugin.Summary.当前携带炮台;
            while(carried.Count<Slots)carried.Add(new 总结脚本.背包单元());
            if(carried.Count>Slots)carried.RemoveRange(Slots,carried.Count-Slots);
            if(建造系统.当前选中背包id>=Slots)建造系统.当前选中背包id=0;
        }
        internal static void Prepare(战斗背包 bag)
        {
            Apply();
            if(Slots==5 || bag.单元.Count>=Slots)return;
            bag.背包类.选择上限=Slots;
            var original=bag.单元[4];
            var extra=UnityEngine.Object.Instantiate(original,original.transform.parent);
            extra.name="战斗背包格子5";extra.代表id号=5;extra.当前背包id号=-1;
            var icon=NativeUi.Rect(extra.transform,"第六格鸡械",0,0,0,0);
            icon.anchorMin=Vector2.zero;icon.anchorMax=Vector2.one;
            icon.offsetMin=new Vector2(8,8);icon.offsetMax=new Vector2(-8,-8);
            extra.炮台图片=NativeUi.Image(icon,null,Color.white);extra.炮台图片.raycastTarget=false;
            extra.炮台图片.preserveAspect=true;extra.炮台图片.enabled=false;
            var button=extra.GetComponent<Button>();button.onClick=new Button.ButtonClickedEvent();
            button.onClick.AddListener(()=>
            {
                int position=extra.当前背包id号;
                if(position>=0 && position<bag.背包类.单元列表.Count)
                    bag.背包类.单元列表[position].GetComponent<Toggle>().isOn=false;
                bag.背包类.当前已选单元.Remove(position);bag.取消选择(5);
            });
            bag.单元.Add(extra);
            for(int i=0;i<bag.单元.Count;i++)
            {
                var cell=bag.单元[i];var rect=cell.GetComponent<RectTransform>();
                rect.anchorMin=new Vector2(.03f+i*.94f/Slots,.03f);
                rect.anchorMax=new Vector2(.03f+(i+1)*.94f/Slots-.01f,.955f);
                rect.offsetMin=rect.offsetMax=Vector2.zero;
                if(cell.炮台图片!=null)cell.炮台图片.preserveAspect=true;
            }
        }
        internal static void Build(建造系统 build)
        {
            Apply();
            while(build.建造单位信息列表.Count<Slots)
                build.建造单位信息列表.Add(new 建造系统.建造单位信息类());
            if(build.建造单位信息列表.Count>Slots)build.建造单位信息列表.RemoveRange(Slots,build.建造单位信息列表.Count-Slots);
        }
        internal static void Hud(栏位单元 unit)
        {
            if(Slots==5)return;
            var group=unit.transform.parent.parent;
            var cells=group.GetComponentsInChildren<栏位单元>(true).OrderBy(x=>x.当前背包位置).ToList();
            if(cells.Count!=5)return;
            var original=cells[4];
            var root=UnityEngine.Object.Instantiate(original.transform.parent.gameObject,group);
            root.name="第六栏";
            var extra=root.GetComponentInChildren<栏位单元>(true);extra.当前背包位置=extra.背包位置=5;
            // Instantiating the whole slot preserves native drag, hover, cooldown and selection references.
            var key=root.transform.Find("快捷键提示").GetComponent<Image>();
            var sprite=key.sprite;
            if(sixthKey==null)sixthKey=Sprite.Create(sprite.texture,sprite.rect,new Vector2(.5f,.5f),sprite.pixelsPerUnit,0,SpriteMeshType.FullRect,new Vector4(3,3,3,3));
            key.sprite=sixthKey;
            key.type=Image.Type.Sliced;key.fillCenter=false;
            var face=NativeUi.Rect(key.transform,"按键底色",0,0,0,0);
            face.anchorMin=new Vector2(3f/9,3f/11);face.anchorMax=new Vector2(6f/9,8f/11);
            face.offsetMin=face.offsetMax=Vector2.zero;
            NativeUi.Image(face,null,new Color(86f/255,97f/255,117f/255)).raycastTarget=false;
            var label=UnityEngine.Object.Instantiate(extra.cd数字,key.transform);
            label.name="第六格按键";label.text="6";label.fontSize=32;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;
            label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;
            label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
            var controllerHint=extra.框选图片.transform.Find("手柄UI_Y");
            if(controllerHint!=null)controllerHint.gameObject.SetActive(false);
            cells.Add(extra);
            for(int i=0;i<cells.Count;i++)
            {
                var rect=(RectTransform)cells[i].transform.parent;
                var min=rect.anchorMin;var max=rect.anchorMax;
                min.x=.05f+i*.905f/Slots;max.x=.05f+(i+1)*.905f/Slots-.02f;
                rect.anchorMin=min;rect.anchorMax=max;
                rect.sizeDelta=new Vector2(0,rect.sizeDelta.y);rect.anchoredPosition=new Vector2(0,rect.anchoredPosition.y);
            }
        }
        internal static void RefreshPreparation()
        {
            Apply();
            foreach(var bag in UnityEngine.Object.FindObjectsOfType<战斗背包>())Prepare(bag);
        }
        internal static void Release(){if(sixthKey!=null)UnityEngine.Object.Destroy(sixthKey);sixthKey=null;}
    }

    [HarmonyPatch(typeof(战斗背包),"Update")]
    internal static class ExtraPreparationSlot
    {
        private static void Postfix(战斗背包 __instance)
        {
            if(CarryCapacity.Slots==5 || __instance.单元.Count<6)return;
            __instance.选择同步(5);
            var cell=__instance.单元[5];int id=__instance.总结.当前携带炮台[5].炮台id;
            cell.炮台图片.enabled=id!=0;
            if(id!=0)cell.炮台图片.sprite=Plugin.Units.获取单位(id).GetComponent<模块_基本属性>().图标;
            var text=cell.GetComponentInChildren<Text>(true);if(text!=null)text.enabled=id==0;
        }
    }
    [HarmonyPatch(typeof(选择携带炮台脚本),"Start")]
    internal static class ExtraCarriedBagSlot
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            foreach(var instruction in code)
            {
                if(instruction.opcode==OpCodes.Ldc_I4_5)
                {instruction.opcode=OpCodes.Call;instruction.operand=AccessTools.PropertyGetter(typeof(CarryCapacity),nameof(CarryCapacity.Slots));}
                yield return instruction;
            }
        }
        private static void Prefix()=>CarryCapacity.Apply();
    }
    [HarmonyPatch(typeof(建造系统),"背包cd初始化",new Type[0])]
    internal static class ExtraBuildSlot
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(建造系统 __instance)=>CarryCapacity.Build(__instance);
    }
    [HarmonyPatch(typeof(栏位单元),"Start")]
    internal static class ExtraHudSlot { private static void Prefix(栏位单元 __instance)=>CarryCapacity.Hud(__instance); }
    [HarmonyPatch(typeof(模块_玩家_建造),"序号选择")]
    internal static class SixthBuildKey
    {
        private static readonly FieldInfo system=AccessTools.Field(typeof(模块_玩家_建造),"建造系统");
        private static readonly FieldInfo death=AccessTools.Field(typeof(模块_玩家_建造),"死亡");
        private static void Postfix(模块_玩家_建造 __instance)
        {
            if(CarryCapacity.Slots<6 || !Input.GetKeyDown(KeyCode.Alpha6) || !建造系统.是否放置基地车 ||
                Plugin.Role!=0 && !__instance.isOwned || ((模块_玩家_死亡)death.GetValue(__instance)).是否玩家死亡)return;
            var build=(建造系统)system.GetValue(__instance);
            建造系统.当前选中背包id=5;建造系统.按键建造模式=true;build.开始拖动();
        }
    }
    [HarmonyPatch(typeof(模块_玩家_动力),"Start")]
    internal static class EventPowerCapacity {private static void Prefix()=>CarryCapacity.Apply();}
    [HarmonyPatch(typeof(模块_动力UI),"显示玩家动力")]
    internal static class CapacityPowerBar
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            foreach(var instruction in code)
            {
                if(instruction.opcode==OpCodes.Ldc_R4 && (float)instruction.operand==20f)
                {instruction.opcode=OpCodes.Ldsfld;instruction.operand=AccessTools.Field(typeof(模块_玩家_动力),"动力上限");}
                yield return instruction;
            }
        }
    }
}
