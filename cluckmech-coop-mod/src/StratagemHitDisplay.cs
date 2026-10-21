using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mirror;
using UnityEngine;

namespace CluckmechCoop
{
    // Shared authoritative feedback for stratagem hits and missing Otherworld DoT.
    // No scene search, per-unit worker, damage prediction or permanent Update loop.
    internal static class StratagemHitDisplay
    {
        internal const int BatchSize=256;
        internal const uint ResultMarker=0x53545241; // Preserve the existing result envelope.
        internal const float FlushInterval=.1f;
        private sealed class SourceTag { }
        private static readonly ConditionalWeakTable<GameObject,SourceTag> sources=new ConditionalWeakTable<GameObject,SourceTag>();
        private static readonly OtherworldHitDisplay.DisplaySink directSink=Record;
        private static readonly OtherworldHitDisplay.DisplaySink statusSink=RecordStatus;
        private sealed class Pending
        {
            internal SceneLifetime Scene;
            internal string Token;
            internal float Until;
            internal readonly List<OtherworldDisplayHit> Hits=new List<OtherworldDisplayHit>();
        }
        private static Pending pending;
        [ThreadStatic] private static GameObject statusTarget;
        [ThreadStatic] private static float statusTotal,statusPart;
        private static bool ModulesEnabled=>ModuleSettings.Extra.Stratagems||OtherworldWorld.Active;
        private static bool Enabled=>Plugin.Role==1&&Plugin.InCombat&&ModulesEnabled&&Plugin.Network.PeerReady;
        private static bool Current(Pending batch)=>ReferenceEquals(pending,batch)&&Enabled&&
            SceneLifecycle.IsCurrent(batch.Scene)&&batch.Token==Plugin.Combat.Token;

        internal static void TrackSource(GameObject source)
        {
            // A weak tag retains attribution without retaining destroyed source bodies.
            if(Plugin.Role==1&&!ReferenceEquals(source,null))sources.GetValue(source,_=>new SourceTag());
        }
        internal static OtherworldHitDisplay.CaptureScope Capture()=>Enabled&&ModuleSettings.Extra.Stratagems?
            OtherworldHitDisplay.CaptureTo(directSink):default;
        private static void Record(模块_受伤 target,float amount,bool critical,int color)
        {
            if(!Enabled||!OtherworldDataValidation.Finite(amount)||amount<0)return;
            var identity=target.GetComponent<NetworkIdentity>();
            int space=OtherworldWorld.Space(target.gameObject);
            // Drop invisible results before allocating a hit or starting a sender.
            if(!GuestHitVisibility.Visible(identity,space))return;
            var scene=SceneLifecycle.Current;if(scene==null||!SceneLifecycle.IsCurrent(scene))return;
            var batch=pending;
            if(batch==null||!Current(batch))
            {
                if(batch!=null)batch.Hits.Clear();
                pending=batch=new Pending{Scene=scene,Token=Plugin.Combat.Token,Until=Time.realtimeSinceStartup+FlushInterval};
                Plugin.Instance.StartCoroutine(SendPending(batch));
            }
            batch.Hits.Add(new OtherworldDisplayHit{Target=identity.netId,Space=space,
                Amount=amount,Critical=critical,Burn=color==1,Color=color});
            if(batch.Hits.Count>=BatchSize)Flush(batch);
        }
        private static void Flush(Pending batch)
        {
            if(!Current(batch)||batch.Hits.Count==0)return;
            // The guest or target can move during the <=100ms collection window.
            GuestHitVisibility.Filter(batch.Hits);
            if(batch.Hits.Count==0)return;
            OtherworldNetwork.SendDisplay(new OtherworldMessage{Kind=OtherworldMessageKind.Effect,Value=0,Sequence=ResultMarker},batch.Hits,2);
            batch.Hits.Clear();batch.Until=Time.realtimeSinceStartup+FlushInterval;
        }
        internal static void Flush()
        {
            var batch=pending;if(batch!=null)Flush(batch);
        }
        private static IEnumerator SendPending(Pending batch)
        {
            try
            {
                yield return null;
                while(Current(batch)&&batch.Hits.Count>0)
                {
                    if(Time.realtimeSinceStartup>=batch.Until)Flush(batch);
                    if(batch.Hits.Count==0)yield break;
                    yield return null;
                }
            }
            finally
            {
                batch.Hits.Clear();if(ReferenceEquals(pending,batch))pending=null;
            }
        }
        internal static bool Receive(OtherworldMessage message)
        {
            if(message.Kind!=OtherworldMessageKind.Effect||message.Value!=0||message.Player!=0||message.Sequence!=ResultMarker)return false;
            if(Plugin.Role==2&&Plugin.InCombat&&ModulesEnabled&&message.Token==Plugin.Combat.Token)
                OtherworldHitDisplay.Receive(message.Data);
            return true;
        }

        // Observe the existing native sum; do not run a second state scan or replay
        // damage. Relay only states absent on the guest, even with mixed sources.
        internal struct StatusScope:IDisposable
        {
            private bool active;
            private GameObject previousTarget;
            private float previousTotal,previousPart;
            private OtherworldHitDisplay.CaptureScope display;
            internal StatusScope(GameObject target)
            {
                active=true;previousTarget=statusTarget;previousTotal=statusTotal;previousPart=statusPart;
                display=OtherworldHitDisplay.CaptureTo(statusSink);
                statusTarget=target;statusTotal=statusPart=0;
            }
            public void Dispose()
            {
                if(!active)return;
                display.Dispose();statusTarget=previousTarget;statusTotal=previousTotal;statusPart=previousPart;active=false;
            }
        }
        internal static StatusScope CaptureStatus(GameObject target)=>Enabled?new StatusScope(target):default;
        internal static void BeginStatusSum(GameObject target)
        {if(ReferenceEquals(statusTarget,target))statusTotal=statusPart=0;}
        internal static float StateValue(模块_状态属性.状态类 state)
        {
            float value=state.加成属性.当前属性效果.x;
            if(!ReferenceEquals(statusTarget,null))
            {
                statusTotal+=value;
                bool missing=OtherworldWorld.Active&&state is OtherworldState tagged&&tagged.HostDisplay;
                if(missing||ModuleSettings.Extra.Stratagems&&!ReferenceEquals(state.攻击者,null)&&sources.TryGetValue(state.攻击者,out _))statusPart+=value;
            }
            return value;
        }
        private static void RecordStatus(模块_受伤 target,float amount,bool critical,int color)
        {
            if(!ReferenceEquals(target.gameObject,statusTarget)||critical||color==0||statusPart<=0||statusTotal<=0)return;
            Record(target,amount*Math.Min(1,statusPart/statusTotal),false,color);
        }
    }
}
