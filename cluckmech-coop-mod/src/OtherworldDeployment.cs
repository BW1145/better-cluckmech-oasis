using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Mirror;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CluckmechCoop
{
    internal static class OtherworldDeployment
    {
        private sealed class Identity {internal string Id;}
        private static ConditionalWeakTable<总结脚本.背包单元,Identity> identities=new ConditionalWeakTable<总结脚本.背包单元,Identity>();
        private static bool rebound;
        private static 建造系统 build;
        private static float[] cooldown,power,currentPower;
        private static float factor=1;
        private static cz词缀库 affixes;
        private static readonly MethodInfo nativePlace=AccessTools.Method(typeof(联机玩家控件),"UserCode_放置单位_cmd__放置单位数据");
        internal static OtherworldActor Generating;
        internal static void RefreshLocal()
        {
            var run=OtherworldProgress.For(Plugin.Role);var summary=Plugin.Summary;if(run==null||summary==null)return;
            var bag=summary.当前拥有炮台;
            if(!rebound)
            {
                for(int i=0;i<bag.Count;i++)
                    identities.GetValue(bag[i],_=>new Identity{Id=run.CardIds!=null&&i<run.CardIds.Count?run.CardIds[i]:Guid.NewGuid().ToString("N")});
                rebound=true;
            }
            run.CardIds=new List<string>();run.LastDeployment=null;
            foreach(var card in bag)
            {
                string id=identities.GetValue(card,_=>new Identity{Id=Guid.NewGuid().ToString("N")}).Id;run.CardIds.Add(id);
                if(id==run.LastMachine)run.LastDeployment=Snapshot(card);
            }
        }
        private static Machine Snapshot(总结脚本.背包单元 card)=>new Machine{Id=card.炮台id,UpgradeLimit=card.强化上限,Upgrades=new List<int>(card.强化词条)};
        internal static void Manual(建造系统 value)
        {
            if(Generating!=null)return;
            var run=OtherworldProgress.For(Plugin.Role);if(run?.Initial!=26003)return;
            var card=Plugin.Summary.当前携带炮台[建造系统.当前选中背包id];
            run.LastMachine=identities.GetValue(card,_=>new Identity{Id=Guid.NewGuid().ToString("N")}).Id;
            RefreshLocal();OtherworldNetwork.Progress(Plugin.Role);
        }
        internal static void Dispatch(OtherworldActor actor,Vector2 point,int count)
        {
            RefreshLocal();var machine=actor.Run.LastDeployment;if(!Rules.ValidMachine(machine))return;
            var prefab=Plugin.Units.获取单位(machine.Id);if(prefab==null)return;
            var health=prefab.GetComponent<模块_基本属性>();
            Vector2 size=health.启用建筑体积?health.建筑体积:Vector2.one*.5f;
            if(affixes==null)affixes=Object.FindObjectOfType<cz词缀库>();
            for(int copy=0;copy<count;copy++)
            {
                if(!FindPoint(actor.Space,point,size,out var position))continue;
                Generating=actor;
                try
                {
                    if(Plugin.Role==1)
                    {
                        var owner=actor.Role==1?Steam联机管理器.玩家_主机:Steam联机管理器.玩家_客户端;
                        nativePlace.Invoke(owner,new object[]{new 联机玩家控件.放置单位数据{id=machine.Id,词条id列表=new List<int>(machine.Upgrades),放置位置=position,放置方=(Steam联机管理器.联机状态)actor.Role}});
                    }
                    else
                    {
                        var obj=Object.Instantiate(prefab,position,Quaternion.identity);var stats=obj.GetComponent<模块_基本属性>();
                        stats.母体id=machine.Id;stats.强化词条列表.AddRange(machine.Upgrades);
                        foreach(int id in machine.Upgrades)affixes.赋予词缀(id,obj,true);
                        Generated(stats);
                        Object.FindObjectOfType<事件管理器_战斗>().触发_单位放置(obj,Steam联机管理器.联机状态.未联机);
                    }
                }
                finally{Generating=null;}
            }
        }
        internal static void Generated(模块_基本属性 health)
        {
            if(Generating==null)return;
            health.所属玩家=(Steam联机管理器.联机状态)Generating.Role;
            OtherworldWorld.Add(health);var unit=OtherworldWorld.Unit(health.gameObject);
            if(unit!=null&&Generating.Run.Has(26110)){unit.WarmUntil=Time.time+10;unit.Refresh();}
        }
        internal static bool FindPoint(int space,Vector2 point,Vector2 size,out Vector2 found)
        {
            for(int ring=0;ring<=5;ring++)
                for(int i=0;i<(ring==0?1:16);i++)
                {
                    var candidate=point+new Vector2(Mathf.Cos(i*Mathf.PI/8),Mathf.Sin(i*Mathf.PI/8))*ring*.5f;
                    if(KamuiWorld.At(candidate)!=space||!StratagemDeployment.ValidPoint(candidate,size))continue;
                    if(space!=0)
                    {
                        var map=KamuiWorld.Room(space).Map;var half=size*.5f;
                        if(!map.PointIsValid(candidate)||!map.PointIsValid(candidate+half)||!map.PointIsValid(candidate-half)||
                           !map.PointIsValid(candidate+new Vector2(half.x,-half.y))||!map.PointIsValid(candidate+new Vector2(-half.x,half.y)))continue;
                    }
                    found=candidate;return true;
                }
            found=point;return false;
        }
        internal static void Bind(建造系统 value)
        {
            build=value;factor=1;int count=build.建造单位信息列表.Count;
            cooldown=new float[count];power=new float[count];currentPower=new float[count];
            for(int i=0;i<count;i++){var card=build.建造单位信息列表[i];cooldown[i]=card.cd时间;power[i]=card.花费动力;currentPower[i]=card.当前花费动力;}
            var actor=OtherworldActions.Player(Plugin.Role);if(actor!=null)SpaceChanged(actor);
        }
        internal static void SpaceChanged(OtherworldActor actor)
        {
            if(actor.Role!=Plugin.Role||build==null||cooldown==null)return;
            bool workshop=actor.Space!=0&&actor.Run.Initial==26006;
            float next=workshop?(actor.Run.Has(26119)?.25f:.5f):1;
            if(next==factor)return;
            for(int i=0;i<cooldown.Length;i++)
            {
                var card=build.建造单位信息列表[i];float progress=card.cd时间>0?card.剩余cd时间/card.cd时间:0;
                card.cd时间=cooldown[i]*next;card.剩余cd时间=progress*card.cd时间;
                card.花费动力=workshop?0:power[i];card.当前花费动力=workshop?0:currentPower[i];
            }
            factor=next;
        }
        internal static int Power(int original)=>factor<1?0:original;
        internal static void ClearScene(){build=null;cooldown=power=currentPower=null;factor=1;Generating=null;affixes=null;}
        internal static void Rebind(){identities=new ConditionalWeakTable<总结脚本.背包单元,Identity>();rebound=false;}
    }
    [HarmonyPatch(typeof(建造系统),"放置单位")]
    internal static class OtherworldManualPlacement {private static void Postfix(建造系统 __instance)=>OtherworldDeployment.Manual(__instance);}
    [HarmonyPatch(typeof(联机_单位属性同步),"放置单位初始化")]
    internal static class OtherworldAutomaticPlacement
    {private static void Postfix(联机_单位属性同步 __instance)=>OtherworldDeployment.Generated(__instance.GetComponent<模块_基本属性>());}
    [HarmonyPatch(typeof(建造系统),"背包cd初始化",new Type[0])]
    internal static class OtherworldWorkshopCards
    {[HarmonyPriority(Priority.Last)]private static void Postfix(建造系统 __instance)=>OtherworldDeployment.Bind(__instance);}
    [HarmonyPatch]
    internal static class OtherworldWorkshopPower
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(建造系统),"开始拖动");
            yield return AccessTools.Method(typeof(建造系统),"结束拖动延迟");
        }
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            var field=AccessTools.Field(typeof(模块_基本属性),"花费");var power=AccessTools.Method(typeof(OtherworldDeployment),nameof(OtherworldDeployment.Power));
            foreach(var instruction in code){yield return instruction;if(instruction.opcode==OpCodes.Ldfld&&Equals(instruction.operand,field))yield return new CodeInstruction(OpCodes.Call,power);}
        }
    }
}
