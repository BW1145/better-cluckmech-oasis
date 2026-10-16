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
            bag.背包类.选择上限=Slots;
            if(bag.单元.Count<Slots)
            {
                var extra=UnityEngine.Object.Instantiate(bag.单元[4],bag.单元[4].transform.parent);
                extra.name="战斗背包格子5";extra.代表id号=5;extra.当前背包id号=-1;
                foreach(var native in extra.GetComponentsInChildren<背包类单元>(true))native.背包id号=5;
                var button=extra.GetComponent<Button>();button.onClick=new Button.ButtonClickedEvent();
                button.onClick.AddListener(()=>
                {
                    int position=extra.当前背包id号;
                    if(position>=0 && position<bag.背包类.单元列表.Count)
                        bag.背包类.单元列表[position].GetComponent<Toggle>().isOn=false;
                    bag.背包类.当前已选单元.Remove(position);bag.取消选择(5);
                });
                bag.单元.Add(extra);
            }
            (bag.GetComponent<CarryPreparationLayout>() ?? bag.gameObject.AddComponent<CarryPreparationLayout>()).Bind(bag);
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
            if(sixthKey==null)sixthKey=CreateSixthKey(sprite);
            key.sprite=sixthKey;
            key.type=Image.Type.Simple;key.preserveAspect=true;
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
        private static Sprite CreateSixthKey(Sprite source)
        {
            var area=source.textureRect;
            int width=(int)area.width,height=(int)area.height;
            var previous=RenderTexture.active;
            var target=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32);
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};
            try
            {
                Graphics.Blit(source.texture,target,new Vector2(area.width/source.texture.width,area.height/source.texture.height),new Vector2(area.x/source.texture.width,area.y/source.texture.height));
                RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,width,height),0,0);
            }
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);}
            var background=texture.GetPixel(width/2-2,height/2);
            Color ink=Color.black;
            for(int y=2;y<height-2;y++)for(int x=2;x<width-2;x++)
            {var pixel=texture.GetPixel(x,y);if(pixel.grayscale>ink.grayscale)ink=pixel;}
            for(int y=2;y<height-2;y++)for(int x=2;x<width-2;x++)texture.SetPixel(x,y,background);
            string[] glyph={"111","100","100","111","101","101","111"};
            for(int y=0;y<7;y++)for(int x=0;x<3;x++)if(glyph[y][x]=='1')texture.SetPixel((width-3)/2+x,(height+7)/2-1-y,ink);
            texture.Apply(false,true);
            return Sprite.Create(texture,new Rect(0,0,width,height),new Vector2(.5f,.5f),source.pixelsPerUnit);
        }
        internal static void RefreshPreparation()
        {
            Apply();
            foreach(var bag in UnityEngine.Object.FindObjectsOfType<战斗背包>())Prepare(bag);
        }
        internal static void Release(){if(sixthKey!=null){UnityEngine.Object.Destroy(sixthKey.texture);UnityEngine.Object.Destroy(sixthKey);}sixthKey=null;}
    }

    public sealed class CarryPreparationLayout : MonoBehaviour
    {
        private 战斗背包 bag;
        private bool queued;
        internal void Bind(战斗背包 value){bag=value;Schedule();}
        private void OnEnable()=>Schedule();
        private void OnRectTransformDimensionsChange()=>Schedule();
        private void Schedule(){if(bag!=null && !queued && isActiveAndEnabled){queued=true;StartCoroutine(Arrange());}}
        private System.Collections.IEnumerator Arrange()
        {
            yield return null;
            int slots=CarryCapacity.Slots;
            for(int i=0;i<bag.单元.Count;i++)
            {
                var cell=bag.单元[i];cell.gameObject.SetActive(i<slots);
                var rect=cell.GetComponent<RectTransform>();
                rect.anchorMin=new Vector2(.03f+i*.94f/slots,.03f);
                rect.anchorMax=new Vector2(.03f+(i+1)*.94f/slots-.01f,.955f);
                rect.offsetMin=rect.offsetMax=Vector2.zero;
            }
            var root=bag.GetComponentInParent<战斗准备背包>();
            var carried=root==null?null:root.GetComponentInChildren<选择携带炮台脚本>(true);
            var grid=carried==null?null:carried.GetComponentInChildren<GridLayoutGroup>(true);
            if(grid!=null)
            {
                var area=(RectTransform)grid.transform;
                var fitter=grid.GetComponent<ContentSizeFitter>();
                if(fitter!=null){fitter.horizontalFit=ContentSizeFitter.FitMode.Unconstrained;fitter.verticalFit=ContentSizeFitter.FitMode.Unconstrained;}
                area.anchorMin=Vector2.zero;area.anchorMax=Vector2.one;area.offsetMin=area.offsetMax=Vector2.zero;
                Canvas.ForceUpdateCanvases();
                var first=bag.单元[0].GetComponent<RectTransform>();
                Vector2 low=area.InverseTransformPoint(first.TransformPoint(first.rect.min));
                Vector2 high=area.InverseTransformPoint(first.TransformPoint(first.rect.max));
                var second=bag.单元[1].GetComponent<RectTransform>();
                Vector2 next=area.InverseTransformPoint(second.TransformPoint(second.rect.min));
                grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=slots;
                grid.startCorner=GridLayoutGroup.Corner.UpperLeft;grid.startAxis=GridLayoutGroup.Axis.Horizontal;
                grid.childAlignment=TextAnchor.UpperLeft;
                grid.padding=new RectOffset(Mathf.RoundToInt(low.x-area.rect.xMin),0,Mathf.RoundToInt(area.rect.yMax-high.y),0);
                grid.cellSize=high-low;grid.spacing=new Vector2(next.x-high.x,0);
                foreach(var icon in carried.GetComponentsInChildren<携带背包单元脚本>(true))icon.图标图片.preserveAspect=true;
                LayoutRebuilder.ForceRebuildLayoutImmediate(area);
            }
            queued=false;
        }
    }
    [HarmonyPatch(typeof(战斗背包),"Update")]
    internal static class ExtraPreparationSlot
    {
        private static void Postfix(战斗背包 __instance)
        {
            if(CarryCapacity.Slots==5 || __instance.单元.Count<6)return;
            __instance.选择同步(5);
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
