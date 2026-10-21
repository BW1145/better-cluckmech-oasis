using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    internal sealed class OtherworldState:模块_状态属性.状态类
    {
        internal int Owner;
        internal int Space;
        internal bool BurnSpread;
        // Native State.Clone uses MemberwiseClone, so this survives state cloning
        // and source destruction without keeping a separate per-tick lookup table.
        internal bool HostDisplay;
    }
    internal static class OtherworldStates
    {
        internal static OtherworldState Copy(模块_状态属性.状态类 state,int owner,float duration=-1)
        {
            var s=state.加成属性;
            return new OtherworldState{Owner=owner,BurnSpread=state is OtherworldState original&&original.BurnSpread,HostDisplay=state is OtherworldState feedback&&feedback.HostDisplay,Space=OtherworldDamage.ApplyingSpace??(state is OtherworldState tagged?tagged.Space:OtherworldWorld.Space(state.攻击者)),攻击者=state.攻击者,状态类型=state.状态类型,剩余时间=state.剩余时间,
                自身抗性属性=state.自身抗性属性,加成属性=new 模块_攻击详细属性.状态属性单元{默认持续时间=duration<0?s.默认持续时间:duration,
                    默认属性效果=s.默认属性效果,变化持续时间=duration<0?s.变化持续时间:0,变化百分比持续时间=duration<0?s.变化百分比持续时间:0,
                    变化属性效果=s.变化属性效果,变化百分比属性效果=s.变化百分比属性效果,当前持续时间=duration<0?s.当前持续时间:duration,当前属性效果=s.当前属性效果}};
        }
        internal static void Died(OtherworldUnit victim)
        {
            if(Plugin.Role==2||victim==null||victim.DeathRecorded||!Difficulty.CountsForEncounter(victim.gameObject))return;
            victim.DeathRecorded=true;
            var room=KamuiWorld.Room(victim.Space);if(room!=null)room.Dirty=true;
            var stateModule=victim.GetComponent<模块_状态属性>();var states=stateModule==null?new List<模块_状态属性.状态类>():stateModule.当前状态列表.ToList();
            var researchers=new HashSet<int>();var spreadBurns=new HashSet<int>();
            foreach(var state in states)
            {
                var owner=state.攻击者!=null?OtherworldProgress.For(state.攻击者):state is OtherworldState prepared?OtherworldProgress.For(prepared.Owner):null;
                if(owner==null)continue;
                int role=state is OtherworldState own?own.Owner:(int)state.攻击者.GetComponent<模块_基本属性>().所属玩家;
                if(state.状态类型==攻击类型与状态数据库.状态枚举.感电&&owner.Initial==26001&&researchers.Add(role))
                {
                    int previous=OtherworldCatalog.ResearchLevel(owner.ShockKills);owner.ShockKills++;
                    if(previous!=OtherworldCatalog.ResearchLevel(owner.ShockKills)){OtherworldWorld.Refresh(role);OtherworldNetwork.Progress(role);}
                    if(owner.Has(26105))Spread(victim,state,role,3,5);
                }
                if(state.状态类型==攻击类型与状态数据库.状态枚举.灼烧&&owner.Initial==26005&&spreadBurns.Add(role))
                {
                    var burns=states.Where(s=>s.状态类型==攻击类型与状态数据库.状态枚举.灼烧&&!(s is OtherworldState propagated&&propagated.BurnSpread)&&
                        (s is OtherworldState mark?mark.Owner==role:s.攻击者!=null&&OtherworldProgress.For(s.攻击者)==owner)).ToArray();
                    var targets = new List<OtherworldUnit>();
                    OtherworldWorld.CollectNearby(victim.gameObject,victim.transform.position,3,targets);
                    OtherworldWorld.SortNearest(targets,victim.transform.position);
                    foreach(var bundle in burns.GroupBy(s=>new{Source=s.攻击者,Remaining=s.剩余时间}))
                    {
                        var combined=Copy(bundle.First(),role,bundle.Key.Remaining);
                        Vector4 total=Vector4.zero;foreach(var burn in bundle)total+=burn.加成属性.当前属性效果;
                        total.x*=.5f;
                        combined.加成属性.当前属性效果=combined.加成属性.默认属性效果=total;
                        combined.加成属性.变化属性效果=combined.加成属性.变化百分比属性效果=Vector4.zero;
                        Spread(victim,combined,role,Mathf.Max(0,bundle.Key.Remaining),3,targets);
                    }
                }
            }
            if(victim.Space==0)return;
            var run=KamuiWorld.Run(victim.Space);if(run==null)return;
            run.SpaceKills++;
            if(run.SpaceKills%10==0){OtherworldWorld.Refresh(victim.Space-1);OtherworldNetwork.Progress(victim.Space-1);}
            if(run.Has(26122))
            {
                var player=OtherworldActions.Player(victim.Space-1);
                if(player!=null)player.GetComponent<模块_受伤>()?.受到治疗(player.Health.当前生命值上限*.03f);
            }
        }
        private static void Spread(OtherworldUnit victim,模块_状态属性.状态类 state,int role,float seconds,int maximum,List<OtherworldUnit> nearby=null)
        {
            if(seconds<=0)return;
            var source=state.攻击者!=null?state.攻击者:OtherworldActions.Player(role)?.gameObject;
            if(source==null)return;
            // A recursive death owns a separate list; outer bundles retain their current snapshot.
            var targets = nearby;
            if (targets == null)
            {
                targets = new List<OtherworldUnit>();
                OtherworldWorld.CollectNearby(victim.gameObject,victim.transform.position,3,targets);
                OtherworldWorld.SortNearest(targets,victim.transform.position);
            }
            int count = 0;
            foreach(var target in targets)
            {
                if(target==null||target==victim||target.Space!=victim.Space||target.Health.当前生命值<=0)continue;
                var copy = Copy(state,role,seconds);
                if(copy.状态类型==攻击类型与状态数据库.状态枚举.灼烧)copy.BurnSpread=true;
                copy.攻击者 = source;
                copy.Space = victim.Space;
                target.GetComponent<模块_状态属性>()?.增加状态(copy);
                if(++count==maximum)break;
            }
        }
    }
    [HarmonyPatch(typeof(模块_状态属性),"增加状态")]
    internal static class OtherworldStatusApplication
    {
        private static bool Prefix(模块_状态属性 __instance,ref 模块_状态属性.状态类 状态,out int __state)
        {
            __state=__instance.当前状态列表.Count;
            if(!OtherworldWorld.Active)return true;
            bool feedback=HostStatusFeedback.RequiresDisplay(__instance,状态);
            if(状态.攻击者==null)
            {if(feedback)状态=HostStatusFeedback.Tag(状态);return true;}
            int origin=OtherworldDamage.ApplyingSpace??(状态 is OtherworldState tagged?tagged.Space:OtherworldWorld.Space(状态.攻击者));
            if(origin!=OtherworldWorld.Space(__instance.gameObject))return false;
            var run=OtherworldProgress.For(状态.攻击者);
            if(状态 is OtherworldState)
            {if(feedback)状态=HostStatusFeedback.Tag(状态);return true;}
            if(run==null&&!feedback)return true;
            int role=run==null?-1:(int)状态.攻击者.GetComponent<模块_基本属性>().所属玩家;
            var copy=OtherworldStates.Copy(状态,role);copy.HostDisplay=feedback;
            if(copy.状态类型==攻击类型与状态数据库.状态枚举.灼烧&&run?.Initial==26005)
                copy.加成属性.默认持续时间+=run.Has(26118)?4:2;
            状态=copy;return true;
        }
        private static void Postfix(模块_状态属性 __instance,模块_状态属性.状态类 状态,int __state)
        {
            if(__instance.当前状态列表.Count<=__state||状态.状态类型!=攻击类型与状态数据库.状态枚举.感电||!(状态 is OtherworldState prepared))return;
            var run=OtherworldProgress.For(prepared.Owner);if(run?.Initial!=26001)return;
            var applied=__instance.当前状态列表[__state];
            float seconds=applied.剩余时间+(run.Has(26106)?2:0);
            var fragile=OtherworldActions.State(状态.攻击者,攻击类型与状态数据库.状态枚举.脆弱,seconds,new Vector4(.25f*(1+OtherworldProgress.NativeInfluence(prepared.Owner)+run.Influence),0,0,0));
            var copy=OtherworldStates.Copy(fragile,prepared.Owner);copy.Space=prepared.Space;
            __instance.增加状态(copy);
        }
    }
    [HarmonyPatch(typeof(模块_死亡),"死亡实现")]
    internal static class OtherworldEnemyDeath
    {private static void Prefix(模块_死亡 __instance,bool ___死亡触发生效){if(!___死亡触发生效)OtherworldStates.Died(OtherworldWorld.Unit(__instance.gameObject));}}
    [HarmonyPatch(typeof(词条_4_敌人死亡统计),"死亡触发")]
    internal static class KamuiPermanentDeathCount
    {private static bool Prefix(词条_4_敌人死亡统计 __instance)=>OtherworldWorld.Unit(__instance.gameObject)?.Permanent!=true;}

    [HarmonyPatch(typeof(词条_100004_死亡掉落_货币),"死亡触发")]
    internal static class KamuiHuntingLoot
    {
        private static void Postfix(词条_100004_死亡掉落_货币 __instance,int ___掉落数量)
        {
            if(Plugin.Role==2)return;
            int space=OtherworldWorld.Space(__instance.gameObject);
            if(space==0||KamuiWorld.Run(space)?.Has(26122)!=true||__instance.随机掉落物.Length==0)return;
            float bonus=___掉落数量*.5f;int count=Mathf.FloorToInt(bonus)+(Random.value<bonus%1?1:0);
            for(int i=0;i<count;i++)
            {
                var point=__instance.transform.position+new Vector3(Random.Range(-.5f,.5f),Random.Range(-.5f,.5f));
                Object.Instantiate(__instance.随机掉落物[Random.Range(0,__instance.随机掉落物.Length-1)],point,__instance.transform.rotation);
            }
        }
    }
}
