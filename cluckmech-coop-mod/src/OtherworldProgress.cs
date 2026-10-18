using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class OtherworldProgress
    {
        internal static Dictionary<int,OtherworldRun> Players=new Dictionary<int,OtherworldRun>();
        private static 数据库_角色天赋 talents;
        internal static OtherworldRun For(int role)=>Players.TryGetValue(role==0?1:role,out var run)?run:null;
        internal static OtherworldRun For(GameObject source)
        {
            if(source==null)return null;
            if(OtherworldWorld.Units.TryGetValue(source,out var unit))return unit.Health.当前阵营==阵营数据库.阵营.友方?For((int)unit.Health.所属玩家):null;
            var health=source.GetComponent<模块_基本属性>();return health==null||health.当前阵营!=阵营数据库.阵营.友方?null:For((int)health.所属玩家);
        }
        internal static void ReadNative(int role)
        {
            int hero,initial,upgrades=0;IEnumerable<int> owned;
            if(Plugin.Role==0 || role==Plugin.Role)
            {
                var summary=Plugin.Summary;if(summary==null)return;
                hero=summary.单局统计数据.使用角色;initial=summary.单局统计数据.角色初始天赋?.id??0;
                upgrades=summary.单局统计数据.获得强化;
                owned=summary.单局统计数据.角色天赋列表.Select(t=>t.id);
            }
            else
            {
                var peer=role==1?Steam联机管理器.玩家_主机:Steam联机管理器.玩家_客户端;
                if(peer==null || peer.单据统计数据_联机==null)return;
                hero=peer.单据统计数据_联机.使用角色;
                if(talents==null)talents=Object.FindObjectOfType<数据库_角色天赋>();
                int index=peer.单据统计数据_联机.角色初始天赋;
                initial=index>=0&&index<talents.角色天赋数据列表.Count?talents.角色天赋数据列表[index].id:0;
                owned=peer.单据统计数据_联机.角色天赋列表.Where(i=>i>=0&&i<talents.角色天赋数据列表.Count).Select(i=>talents.角色天赋数据列表[i].id);
            }
            if(OtherworldCatalog.Hero(hero)==null)return;
            int key=role==0?1:role;
            if(!Players.TryGetValue(key,out var run) || run.Hero!=hero)Players[key]=run=new OtherworldRun{Hero=hero};
            run.Initial=initial;run.Talents=new HashSet<int>(owned.Where(id=>OtherworldCatalog.Talent(id)?.Initial==false));
            if(Plugin.Role==0 || role==Plugin.Role)run.Upgrades=upgrades;
            run.BaseInfluence=NativeInfluence(role);
            if(Plugin.Role==0 || role==Plugin.Role)OtherworldDeployment.RefreshLocal();
            OtherworldWorld.Refresh(role);
        }
        internal static float NativeInfluence(int role)
        {
            if(Plugin.Role==0&&(role==0||role==1) || role==Plugin.Role)
                return Plugin.Summary==null?0:Plugin.Summary.GetComponent<全局加成>().全局元素影响加成;
            var peer=role==1?Steam联机管理器.玩家_主机:Steam联机管理器.玩家_客户端;
            return peer?.总结数据_联机?.全局元素影响加成??For(role)?.BaseInfluence??0;
        }
        internal static void Clear(){Players.Clear();talents=null;}
    }

    internal sealed class OtherworldUnit : MonoBehaviour
    {
        internal 模块_基本属性 Health;
        internal 模块_攻击属性 Attack;
        internal 模块_攻击详细属性 Detailed;
        internal 模块_移动属性 Movement;
        internal Collider2D[] Shapes;
        internal readonly UnitActionLifetime Actions=new UnitActionLifetime();
        internal int Space;
        internal bool IsPlayer,IsMachine,DeathRecorded;
        internal bool Permanent,BossGroup,Restoring;
        internal string PersistentId=System.Guid.NewGuid().ToString("N");
        internal float WarmUntil,RallyUntil;
        private float influence,shock,burn;

        internal void Initialize(模块_基本属性 health)
        {
            Health=health;Attack=GetComponent<模块_攻击属性>();Detailed=GetComponent<模块_攻击详细属性>();Movement=GetComponent<模块_移动属性>();
            IsPlayer=GetComponent<模块_玩家_攻击>()!=null;
            IsMachine=!IsPlayer&&health.ID>=0&&health.当前阵营==阵营数据库.阵营.友方&&!health.单位定位列表.Contains(模块_基本属性.单位定位枚举.基地);
            Shapes=GetComponentsInChildren<Collider2D>(true).Where(c=>c.GetComponentInParent<模块_基本属性>()==health).ToArray();
            OtherworldWorld.Units[gameObject]=this;
            if(IsPlayer)OtherworldWorld.Players[(int)Health.所属玩家]=this;
            KamuiWorld.Added(this);Refresh();
        }
        internal void Refresh()
        {
            var run=OtherworldProgress.For((int)Health.所属玩家);
            if(run==null || Health.当前阵营!=阵营数据库.阵营.友方)return;
            if(Detailed!=null)
            {
                float next=run.Initial==26001?run.Influence:0;
                Detailed.增加状态元素效果(攻击类型与状态数据库.元素状态分类枚举.元素影响类,next-influence);influence=next;
                next=run.Initial==26001?1:0;
                Detailed.增加状态百分比效果加成(攻击类型与状态数据库.状态枚举.感电,next-shock);shock=next;
                next=run.Initial==26005?run.BurnBonus:0;
                Detailed.增加状态百分比效果加成(攻击类型与状态数据库.状态枚举.灼烧,new Vector4(next-burn,0,0,0));burn=next;
            }
            float ratio=Health.当前生命值上限>0?Health.当前生命值/Health.当前生命值上限:1;
            Health.运算生命值上限();Health.运算格挡值();
            Health.当前生命值=Health.当前生命值上限*ratio;
            Attack?.运算攻击力();Attack?.运算攻击速度();Attack?.运算攻击范围();
        }
        private void OnDisable()=>UnitLifecycle.Removed(Health);
        private void OnDestroy()=>UnitLifecycle.Destroyed(gameObject);
    }

    internal static partial class OtherworldWorld
    {
        internal static readonly Dictionary<GameObject,OtherworldUnit> Units=new Dictionary<GameObject,OtherworldUnit>();
        internal static readonly Dictionary<int,OtherworldUnit> Players=new Dictionary<int,OtherworldUnit>();
        internal static bool Active=>OtherworldRegistry.Enabled&&OtherworldProgress.Players.Count>0;
        internal static OtherworldUnit Unit(GameObject obj)=>obj!=null&&Units.TryGetValue(obj,out var unit)?unit:null;
        internal static int Space(GameObject obj)=>Unit(obj)?.Space??0;
        internal static void Add(模块_基本属性 health)
        {
            if(!Active || Units.ContainsKey(health.gameObject))return;
            (health.GetComponent<OtherworldUnit>()??health.gameObject.AddComponent<OtherworldUnit>()).Initialize(health);
        }
        internal static void Begin()
        {
            OtherworldProgress.ReadNative(Plugin.Role);
            if(Plugin.Role==1)OtherworldProgress.ReadNative(2);
            if(!Active)return;
            OtherworldAudio.Begin();
            foreach(var health in 世界事件系统.场上所有单位.ToArray())if(health!=null)Add(health);
        }
        internal static void Refresh(int role)
        {
            foreach(var unit in Units.Values)if(unit!=null&&(int)unit.Health.所属玩家==role)unit.Refresh();
            var actor=OtherworldActions.Player(role);if(actor!=null)OtherworldDeployment.SpaceChanged(actor);
        }
        internal static void Remove(OtherworldUnit unit)
        {
            KamuiWorld.Removed(unit);Units.Remove(unit.gameObject);
            int role=(int)unit.Health.所属玩家;
            if(unit.IsPlayer&&Players.TryGetValue(role,out var player)&&ReferenceEquals(player,unit))Players.Remove(role);
        }
        internal static void Clear(){foreach(var unit in Units.Values)if(unit!=null)unit.Actions.Invalidate();Units.Clear();Players.Clear();OtherworldAudio.Clear();}
    }

    [HarmonyPatch(typeof(模块_基本属性),"Start")]
    internal static class OtherworldUnitReady {private static void Postfix(模块_基本属性 __instance)=>OtherworldWorld.Add(__instance);}
    [HarmonyPatch(typeof(总结脚本),"获得天赋")]
    internal static class OtherworldTalentAcquired {private static void Postfix(){OtherworldProgress.ReadNative(Plugin.Role);OtherworldNetwork.Progress(Plugin.Role);}}
    [HarmonyPatch(typeof(总结脚本),"获得强化词条")]
    internal static class OtherworldMachineUpgraded {private static void Postfix(){OtherworldProgress.ReadNative(Plugin.Role);OtherworldNetwork.Progress(Plugin.Role);}}
    [HarmonyPatch(typeof(选择英雄缓存器脚本),"进入游戏实现")]
    internal static class OtherworldNewRun {private static void Postfix(){OtherworldProgress.Clear();KamuiWorld.NewRun();OtherworldProgress.ReadNative(Plugin.Role);}}
}
