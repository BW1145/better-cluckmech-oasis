using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class StratagemDeployment
    {
        private static 建造系统 build;
        private static GameObject previews;
        private static readonly Dictionary<int,GameObject> previewCards = new Dictionary<int,GameObject>();
        private static string node;
        internal static bool ValidPoint(Vector2 point,Vector2 volume)
        {
            if (volume.x <= 0 || volume.y <= 0) return true;
            var hit = Physics2D.BoxCast(point,volume-new Vector2(.1f,.1f),0,Vector2.zero,100,91136);
            if (hit.collider == null) return true;
            int layer = hit.collider.gameObject.layer;
            return layer != 10 && layer != 13 && layer != 14 && layer != 16;
        }
        private static Machine Snapshot(总结脚本.背包单元 card) => card == null || card.炮台id <= 0 ? null :
            new Machine { Id = card.炮台id, UpgradeLimit = card.强化上限, Upgrades = new List<int>(card.强化词条) };
        internal static void Clear()
        {
            build=null;node=null;
            if(previews!=null)UnityEngine.Object.Destroy(previews);
            previews=null;previewCards.Clear();
        }
        internal static void Initialize(建造系统 value)
        {
            build=value;node=ModuleSettings.NodeKey;
            if(!ModuleSettings.Extra.Stratagems || Plugin.Summary==null)return;
            for(int i=0;i<Plugin.Summary.当前携带炮台.Count;i++)
            {
                var card=Snapshot(Plugin.Summary.当前携带炮台[i]);
                if(card==null || !StratagemCatalog.IsUnit(card.Id))continue;
                var p=new StratagemParameters(card.Id,card.Upgrades);
                var item=build.建造单位信息列表[i];
                var saved=ModuleSettings.LocalProgress.StratagemCooldowns.FirstOrDefault(x=>x.Node==node && x.Slot==i && Rules.SameMachine(card,x.Machine));
                item.cd时间=p.Cooldown;item.花费动力=item.当前花费动力=p.Power;
                if(saved!=null)item.剩余cd时间=saved.Remaining;
                item.是否cd完毕=item.剩余cd时间<=0;item.百分比=item.剩余cd时间/p.Cooldown;
            }
        }
        internal static void Capture()
        {
            if(build==null || node==null || Plugin.Summary==null)return;
            var saved=ModuleSettings.LocalProgress.StratagemCooldowns;saved.Clear();
            for(int i=0;i<Plugin.Summary.当前携带炮台.Count;i++)
            {
                var card=Snapshot(Plugin.Summary.当前携带炮台[i]);
                if(card!=null && StratagemCatalog.IsUnit(card.Id))
                    saved.Add(new StratagemCooldown { Node=node,Slot=i,Machine=card,Remaining=Mathf.Max(0,build.建造单位信息列表[i].剩余cd时间) });
            }
        }
        internal static GameObject SelectedPrefab(dw单位库 units,int id) => Preview(建造系统.当前选中背包id,units.获取单位(id));
        internal static GameObject Preview(int slot,GameObject original)
        {
            if (!ModuleSettings.Extra.Stratagems || Plugin.Summary == null || slot < 0 || slot >= Plugin.Summary.当前携带炮台.Count) return original;
            var machine = Snapshot(Plugin.Summary.当前携带炮台[slot]);
            if (machine == null || !StratagemCatalog.IsUnit(machine.Id)) return original;
            if (previewCards.TryGetValue(slot,out var cached)) return cached;
            if (previews == null) { previews = new GameObject("StratagemPreviews"); previews.SetActive(false); }
            var obj = UnityEngine.Object.Instantiate(original,previews.transform);
            var p = new StratagemParameters(machine.Id,machine.Upgrades);
            var stats = obj.GetComponent<模块_基本属性>(); stats.花费 = p.Power; stats.放置cd = p.Cooldown;
            stats.强化词条列表 = new List<int>();
            stats.显示范围首选项 = 模块_基本属性.显示范围类型枚举.攻击范围;
            var attack = obj.GetComponent<模块_攻击属性>();
            attack.攻击范围 = p.Definition.Kind == StratagemKind.Barrage ? p.ScatterRadius :
                p.Definition.Kind == StratagemKind.Shield || p.Definition.Kind == StratagemKind.Eagle ? p.BlastRadius : p.Definition.Range;
            previewCards.Add(slot,obj); return obj;
        }
    }
    [HarmonyPatch(typeof(建造系统),"背包cd初始化",new Type[0])]
    internal static class StratagemCardCooldowns { private static void Postfix(建造系统 __instance) => StratagemDeployment.Initialize(__instance); }
    [HarmonyPatch(typeof(建造系统),"炮台运算")]
    internal static class PreviewStratagemCard
    {
        private static void Postfix(int 背包id号,ref GameObject __result,ref GameObject ___当前选中炮台)
        { __result=StratagemDeployment.Preview(背包id号,__result);___当前选中炮台=__result; }
    }
    [HarmonyPatch]
    internal static class StartStratagemDrag
    {
        private static System.Reflection.MethodBase TargetMethod() => TranspilerContract.InstanceVoid(typeof(建造系统), "开始拖动");
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            var original = AccessTools.Method(typeof(dw单位库), "获取单位", new[] { typeof(int) });
            var selected = AccessTools.Method(typeof(StratagemDeployment), nameof(StratagemDeployment.SelectedPrefab), new[] { typeof(dw单位库), typeof(int) });
            var instructions = TranspilerContract.Exact(code, nameof(StartStratagemDrag), 1, instruction => instruction.Calls(original));
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(original))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = selected;
                }
                yield return instruction;
            }
        }
    }
}
