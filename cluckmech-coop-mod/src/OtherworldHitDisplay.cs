using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

namespace CluckmechCoop
{
    internal sealed class OtherworldDisplayHit
    {
        public uint Target;
        public int Space;
        public float Amount;
        public bool Critical,Burn;
    }
    internal static class OtherworldHitDisplay
    {
        private static readonly Action<模块_受伤,float> normal=AccessTools.MethodDelegate<Action<模块_受伤,float>>(AccessTools.Method(typeof(模块_受伤),"伤害出现",new[]{typeof(float)}));
        private static readonly Action<模块_受伤,float> critical=AccessTools.MethodDelegate<Action<模块_受伤,float>>(AccessTools.Method(typeof(模块_受伤),"暴击出现",new[]{typeof(float)}));
        private static readonly Action<模块_受伤,float,Vector4> colored=AccessTools.MethodDelegate<Action<模块_受伤,float,Vector4>>(AccessTools.Method(typeof(模块_受伤),"伤害出现",new[]{typeof(float),typeof(Vector4)}));
        private static readonly AccessTools.FieldRef<模块_受伤,效果物体引用器> references=AccessTools.FieldRefAccess<模块_受伤,效果物体引用器>("引用器");
        [ThreadStatic] private static Batch current;
        internal const int MaxPendingHits=512;
        internal const float PendingTimeoutSeconds=1;
        private static PendingReplay pendingReplay;
        private struct PendingHit
        {
            internal OtherworldDisplayHit Hit;
            internal float ExpiresAt;
        }
        private sealed class PendingReplay
        {
            internal SceneLifetime Scene;
            internal string Token;
            internal readonly List<PendingHit> Hits=new List<PendingHit>();
        }
        internal sealed class Batch:IDisposable
        {
            private readonly Batch previous;
            internal readonly List<OtherworldDisplayHit> Hits=new List<OtherworldDisplayHit>();
            internal Batch(){previous=current;current=this;}
            public void Dispose(){current=previous;}
        }
        internal static float Value(float damage,float resistance,float block,bool protect,bool crit,float multiplier)
        {if(protect)return 0;float value=damage*(1-resistance)-block;if(value<1)value=1;return crit?value*(1+multiplier):value;}
        internal static void Local(模块_受伤 target,OtherworldUnit unit,世界类.伤害数据包 packet)
        {
            if(!(packet.攻击伤害>=0)||unit.Health.当前是否无敌||unit.Space!=OtherworldWorld.Space(OtherworldActions.Player(Plugin.Role)?.gameObject))return;
            float damage=packet.攻击伤害;
            if(unit.Space!=0&&unit.Health.当前阵营==阵营数据库.阵营.敌方&&KamuiWorld.Run(unit.Space)?.Has(26116)==true)damage*=1.25f;
            float value=Value(damage,unit.Health.当前抗性,unit.Health.当前格挡值,unit.Health.当前是否保护,packet.是否暴击,packet.暴击倍率);
            Show(target,value,packet.是否暴击,false);
        }
        internal static void Record(模块_受伤 target,float amount,bool crit,bool burn)
        {
            if(Plugin.Role!=1||current==null)return;
            uint id=StratagemWorld.Id(target.gameObject);if(id==0)return;
            current.Hits.Add(new OtherworldDisplayHit{Target=id,Space=OtherworldWorld.Space(target.gameObject),Amount=amount,Critical=crit,Burn=burn});
        }
        private static void Show(模块_受伤 target,float amount,bool crit,bool burn)
        {
            if(burn)colored(target,amount,new Vector4(1,.4f,.2f,1));
            else if(crit)critical(target,amount);else normal(target,amount);
        }
        internal static void Receive(string data)
        {
            if(string.IsNullOrEmpty(data))return;
            List<OtherworldDisplayHit> hits;
            try{hits=JsonConvert.DeserializeObject<List<OtherworldDisplayHit>>(data);}catch(JsonException){return;}
            if(hits==null)return;
            foreach(var hit in hits)if(!Replay(hit))Queue(hit);
        }
        private static void Queue(OtherworldDisplayHit hit)
        {
            var scene=SceneLifecycle.Current;
            if(scene==null||!SceneLifecycle.IsCurrent(scene)||!OtherworldWorld.Active)return;
            string token=Plugin.Combat.Token;
            var pending=pendingReplay;
            if(pending==null||!ReferenceEquals(pending.Scene,scene)||pending.Token!=token)
            {
                // Retire the previous scope without letting its coroutine clear a new one.
                if(pending!=null)pending.Hits.Clear();
                pendingReplay=pending=new PendingReplay{Scene=scene,Token=token};
                pending.Hits.Add(new PendingHit{Hit=hit,ExpiresAt=Time.realtimeSinceStartup+PendingTimeoutSeconds});
                Plugin.Instance.StartCoroutine(ReplayPending(pending));
            }
            else if(pending.Hits.Count<MaxPendingHits)
                pending.Hits.Add(new PendingHit{Hit=hit,ExpiresAt=Time.realtimeSinceStartup+PendingTimeoutSeconds});
            // Overflow drops only delayed visuals; ready results above still display normally.
        }
        private static bool Replay(OtherworldDisplayHit hit)
        {
            int viewer=OtherworldWorld.Space(OtherworldActions.Player(Plugin.Role)?.gameObject);
            if(hit==null||hit.Space!=viewer||!OtherworldDataValidation.Finite(hit.Amount)||hit.Amount<0||!NetworkClient.spawned.TryGetValue(hit.Target,out var identity))return true;
            if(!identity.gameObject.activeInHierarchy)return true;
            var target=identity.GetComponent<模块_受伤>();if(target==null)return true;
            // Mirror can register a spawned identity before Unity calls the native Start.
            var unit=OtherworldWorld.Unit(identity.gameObject);
            if(unit==null||references(target)==null)return false;
            if(unit.Space==hit.Space)Show(target,hit.Amount,hit.Critical,hit.Burn);
            return true;
        }
        private static IEnumerator ReplayPending(PendingReplay pending)
        {
            try
            {
                yield return null;
                while(ReferenceEquals(pendingReplay,pending)&&SceneLifecycle.IsCurrent(pending.Scene)&&pending.Token==Plugin.Combat.Token&&OtherworldWorld.Active)
                {
                    float now=Time.realtimeSinceStartup;
                    int count=pending.Hits.Count,keep=0;
                    // Compact in order once, rather than shifting the list after every ready hit.
                    for(int i=0;i<count;i++)
                    {
                        var entry=pending.Hits[i];
                        if(now>=entry.ExpiresAt||Replay(entry.Hit))continue;
                        pending.Hits[keep++]=entry;
                    }
                    if(keep<count)pending.Hits.RemoveRange(keep,count-keep);
                    if(pending.Hits.Count==0)yield break;
                    yield return null;
                }
            }
            finally
            {
                pending.Hits.Clear();
                if(ReferenceEquals(pendingReplay,pending))pendingReplay=null;
            }
        }
    }
    [HarmonyPatch(typeof(模块_受伤),"伤害出现",new[]{typeof(float),typeof(Vector4)})]
    internal static class OtherworldNormalDisplayResult
    {
        private static void Prefix(模块_受伤 __instance,float sh,Vector4 颜色)=>OtherworldHitDisplay.Record(__instance,sh,false,颜色.y==.4f);
    }
    [HarmonyPatch(typeof(模块_受伤),"暴击出现",new[]{typeof(float)})]
    internal static class OtherworldCriticalDisplayResult
    {
        private static void Prefix(模块_受伤 __instance,float sh)=>OtherworldHitDisplay.Record(__instance,sh,true,false);
    }
}
