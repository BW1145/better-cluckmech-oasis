using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    public sealed class WealthEncounter:MonoBehaviour
    {
        private static readonly MethodInfo spawn=AccessTools.Method(typeof(刷怪器),"刷怪");
        private static WealthEncounter current;
        internal static bool Spawning;
        internal static bool Pending=>current!=null && !current.finished;
        private readonly HashSet<GameObject> living=new HashSet<GameObject>();
        private bool spawned,finished;
        private Action continuation;
        private readonly Dictionary<UnityEngine.Object,Action> bossCompletions=new Dictionary<UnityEngine.Object,Action>();
        private bool waiting;
        private 刷怪器 spawner;
        private 事件管理器_战斗 events;
        internal static int RichPlayers
        {
            get
            {
                if(!ModuleSettings.Extra.Wealth)return 0;
                int count=Plugin.Summary?.当前金钱数>2000?1:0;
                if(Plugin.Role!=0)
                {
                    var peer=Plugin.Role==1?Steam联机管理器.玩家_客户端:Steam联机管理器.玩家_主机;
                    if(peer?.Network总结数据_联机?.当前金钱数>2000)count++;
                }
                return count;
            }
        }
        internal static void RefreshWarnings()=>PreparationPanel.RefreshWealth();
        internal static void Begin(刷怪器 spawner)
        {
            if(Plugin.Role==2 || !ModuleSettings.Extra.Wealth || current!=null || RichPlayers==0 || spawner.数据==null)return;
            var pool=spawner.数据.波次数据.SelectMany(x=>x.组次数据).Where(x=>!x.是否为boss).SelectMany(x=>x.单位)
                .Where(x=>x!=null && x.GetComponent<模块_基本属性>()?.初始阵营==阵营数据库.阵营.敌方 && x.GetComponent<模块_基本属性>().单位类型!=模块_基本属性.单位类型枚举.陷阱 && x.GetComponent<最终boss状态机>()==null && x.GetComponent<词条_100028_Boss胜利判断>()==null)
                .Distinct().OrderByDescending(Score).Take(3).ToList();
            if(pool.Count==0 || spawner.出怪口.Count==0){Plugin.Warn("[Wealth] This encounter has no native regular-enemy source or entrance");return;}
            current=spawner.gameObject.AddComponent<WealthEncounter>();current.spawner=spawner;
            current.events=(事件管理器_战斗)AccessTools.Field(typeof(刷怪器),"战斗事件").GetValue(spawner);
            current.events.事件_敌人生成.AddListener(current.Generated);
            current.StartCoroutine(current.SpawnBatches(pool,RichPlayers));
        }
        private static float Score(GameObject unit)
        {var hp=unit.GetComponent<模块_基本属性>();var attack=unit.GetComponent<模块_攻击属性>();return hp.生命值*(1+(attack==null?0:attack.攻击力));}
        private void Generated(GameObject unit)
        {
            if(!Spawning)return;
            living.Add(unit);unit.AddComponent<WealthEnemy>();
        }
        private IEnumerator SpawnBatches(List<GameObject> pool,int rich)
        {
            while(spawner.关卡状态!=刷怪器.关卡状态枚举.正在出怪)yield return null;
            for(int batch=0;batch<3;batch++)
            {
                if(batch>0)yield return new WaitForSeconds(5);
                Spawning=true;
                try{spawn.Invoke(spawner,new object[]{new 攻击波次数据模板.出怪单元{单位=new List<GameObject>{pool[batch%pool.Count]},数量=4*rich,是否为boss=false,出现延迟=0}});}
                finally{Spawning=false;}
            }
            spawned=true;Plugin.Log("[Wealth] Spawned "+(12*rich)+" pursuers in three batches");
        }
        internal static bool Finish(刷怪器 spawner,FinishKind kind,int jump)
        {
            if(current==null || current.spawner!=spawner || current.finished)return true;
            current.living.RemoveWhere(x=>x==null || x.GetComponent<模块_基本属性>().当前生命值<=0);
            if(current.spawned && current.living.Count==0){current.finished=true;return true;}
            if(current.continuation==null)
            {
                current.continuation=()=>
                {
                    if(kind==FinishKind.Advance)spawner.下一波次初始化();
                    else if(kind==FinishKind.Jump)spawner.下一波次初始化(jump);
                    else if(kind==FinishKind.Victory)spawner.游戏胜利();
                    else if(kind==FinishKind.Delay)spawner.延迟结算();
                    else AccessTools.Method(typeof(刷怪器),"结算界面切换").Invoke(spawner,null);
                };
                current.Wait();
            }
            return false;
        }
        internal static bool DeferBoss(UnityEngine.Object owner,Action action)
        {
            if(current==null || current.finished)return false;
            if(!current.bossCompletions.ContainsKey(owner))current.bossCompletions.Add(owner,action);
            current.Wait();return true;
        }
        private void Wait(){if(waiting)return;waiting=true;StartCoroutine(WaitForClear());}
        private IEnumerator WaitForClear()
        {
            var pause=new WaitForSeconds(.2f);
            while(!spawned || living.Any(x=>x!=null && x.GetComponent<模块_基本属性>().当前生命值>0))yield return pause;
            finished=true;
            foreach(var completion in bossCompletions.Values)completion();
            continuation?.Invoke();
        }
        private void OnDestroy()
        {
            if(events!=null)events.事件_敌人生成.RemoveListener(Generated);
            if(current==this){current=null;Spawning=false;}
        }
    }
    public sealed class WealthEnemy:MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return null;
            var hp=GetComponent<模块_基本属性>();float ratio=hp.当前生命值上限>0?hp.当前生命值/hp.当前生命值上限:1;
            hp.生命值*=3;hp.变化生命值*=3;hp.运算生命值上限();hp.当前生命值=hp.当前生命值上限*ratio;
            GetComponent<模块_攻击属性>()?.运算攻击力();
        }
    }
    [HarmonyPatch(typeof(模块_攻击属性),"运算攻击力")]
    internal static class WealthEnemyAttack
    {private static void Postfix(模块_攻击属性 __instance){if(__instance.GetComponent<WealthEnemy>()!=null)__instance.当前攻击力*=1.5f;}}
    [HarmonyPatch(typeof(联机玩家控件),"更新_总结数据")]
    internal static class PeerWalletWarning
    {private static void Postfix()=>WealthEncounter.RefreshWarnings();}
}
