using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class VisualParticlePool
    {
        private static readonly HashSet<GameObject> allowed=new HashSet<GameObject>();
        private static readonly Dictionary<(GameObject,bool),Stack<VisualParticleLease>> idle=new Dictionary<(GameObject,bool),Stack<VisualParticleLease>>();
        private static readonly HashSet<VisualParticleLease> all=new HashSet<VisualParticleLease>();
        internal static void Forget(VisualParticleLease lease)=>all.Remove(lease);
        private static GameObject parking;
        internal static void Register(效果物体引用器 effects)
        {if(effects==null)return;allowed.Add(effects.特效.普通攻击效果);allowed.Add(effects.特效.灼烧效果);allowed.Add(effects.特效.感电效果);}
        internal static GameObject Create(GameObject prefab,Vector3 point,Quaternion rotation)=>Rent(prefab,point,rotation,null,false);
        internal static GameObject Rent(GameObject prefab,Vector3 point,Quaternion rotation,Transform parent,bool custom)
        {
            if(!allowed.Contains(prefab))return Object.Instantiate(prefab,point,rotation,parent);
            var key=(prefab,custom);if(!idle.TryGetValue(key,out var cache))idle[key]=cache=new Stack<VisualParticleLease>();
            VisualParticleLease lease=null;while(cache.Count>0&&lease==null)lease=cache.Pop();
            if(lease==null)
            {
                if(parking==null){parking=new GameObject("SceneParticlePool");parking.SetActive(false);}
                var obj=Object.Instantiate(prefab,parking.transform);lease=obj.AddComponent<VisualParticleLease>();
                lease.Prefab=prefab;lease.Custom=custom;lease.Systems=obj.GetComponentsInChildren<ParticleSystem>(true);lease.Baselines=prefab.GetComponentsInChildren<ParticleSystem>(true);
                lease.Bursts=new ParticleSystem.Burst[lease.Baselines.Length][];
                for(int n=0;n<lease.Baselines.Length;n++){var emission=lease.Baselines[n].emission;lease.Bursts[n]=new ParticleSystem.Burst[emission.burstCount];emission.GetBursts(lease.Bursts[n]);}
                all.Add(lease);
            }
            var instance=lease.gameObject;instance.transform.SetParent(parent,false);instance.transform.SetPositionAndRotation(point,rotation);instance.transform.localScale=prefab.transform.localScale;
            var originals=lease.Baselines;
            for(int i=0;i<lease.Systems.Length;i++)
            {
                var ps=lease.Systems[i];ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=ps.main;var source=originals[i].main;main.loop=source.loop;main.maxParticles=source.maxParticles;
                main.simulationSpace=source.simulationSpace;main.useUnscaledTime=source.useUnscaledTime;main.stopAction=ParticleSystemStopAction.None;
                var emission=ps.emission;var initial=originals[i].emission;emission.rateOverTime=initial.rateOverTime;emission.rateOverDistance=initial.rateOverDistance;
                emission.SetBursts(lease.Bursts[i]);
            }
            lease.Leased=true;instance.SetActive(true);
            foreach(var ps in lease.Systems)ps.Play(false);
            return instance;
        }
        internal static void Return(GameObject obj)
        {
            var lease=obj.GetComponent<VisualParticleLease>();if(lease==null){Object.Destroy(obj);return;}if(!lease.Leased)return;
            lease.Leased=false;foreach(var ps in lease.Systems)ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            obj.SetActive(false);if(parking==null){Object.Destroy(obj);return;}obj.transform.SetParent(parking.transform,false);
            var cache=idle[(lease.Prefab,lease.Custom)];int retained=cache.Count;
            if(idle.TryGetValue((lease.Prefab,!lease.Custom),out var other))retained+=other.Count;
            if(retained<64)cache.Push(lease);else Object.Destroy(obj);
        }
        internal static GameObject Status(GameObject prefab,Transform parent)
        {
            var tracker=parent.GetComponent<VisualStatusInstances>();
            if(VisualPreferences.Status)
            {
                if(tracker==null)tracker=parent.gameObject.AddComponent<VisualStatusInstances>();
                if(tracker.Active.TryGetValue(prefab,out var previous)&&previous!=null&&previous.activeInHierarchy&&previous.transform.parent==parent)return previous;
            }
            var obj=Rent(prefab,parent.position,parent.rotation,parent,false);
            obj.transform.localPosition=prefab.transform.localPosition;obj.transform.localRotation=prefab.transform.localRotation;
            if(VisualPreferences.Status)tracker.Active[prefab]=obj;return obj;
        }
        internal static void Clear()
        {foreach(var lease in all)if(lease!=null){lease.Leased=false;Object.Destroy(lease.gameObject);}all.Clear();idle.Clear();allowed.Clear();if(parking!=null)Object.Destroy(parking);parking=null;}
    }
    public sealed class VisualStatusInstances:MonoBehaviour
    {internal readonly Dictionary<GameObject,GameObject> Active=new Dictionary<GameObject,GameObject>();}
    public sealed class VisualParticleLease:MonoBehaviour
    {
        internal GameObject Prefab;internal bool Custom,Leased;internal ParticleSystem[] Systems,Baselines;internal ParticleSystem.Burst[][] Bursts;
        private void OnDestroy()=>VisualParticlePool.Forget(this);
        private void Update(){if(!Leased||Custom)return;foreach(var ps in Systems)if(ps!=null&&ps.IsAlive(false))return;VisualParticlePool.Return(gameObject);}
    }
    [HarmonyPatch(typeof(效果物体引用器),"Start")]
    internal static class RegisterVisualParticlePrefabs
    {private static void Postfix(效果物体引用器 __instance)=>VisualParticlePool.Register(__instance);}
    [HarmonyPatch(typeof(模块_状态持续效果),"状态持续")]
    internal static class ReuseNativeStatusParticles
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach(var c in instructions)
                if(c.operand is MethodInfo m&&m.DeclaringType==typeof(Object)&&m.Name=="Instantiate"&&m.GetParameters().Length==2)
                    yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(VisualParticlePool),"Status")).MoveLabelsFrom(c);
                else yield return c;
        }
    }
}
