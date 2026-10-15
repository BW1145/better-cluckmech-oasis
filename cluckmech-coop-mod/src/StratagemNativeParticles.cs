using UnityEngine;

namespace CluckmechCoop
{
    internal enum StratagemParticleKind { Sparks, Smoke, Electric }
    internal static class StratagemNativeParticles
    {
        private static GameObject sparks,smoke,electric;
        private static int sortingLayer,sortingOrder;
        internal static void Begin()
        {
            var effects=GameObject.Find(函数库.常数类.命名_世界).GetComponent<效果物体引用器>().特效;
            sparks=effects.普通攻击效果;smoke=effects.烟雾效果;electric=effects.感电效果;
            var model=StratagemRegistry.Prefab(20001).GetComponent<SpriteRenderer>();
            sortingLayer=model.sortingLayerID;sortingOrder=model.sortingOrder;
        }
        internal static void Clear(){sparks=smoke=electric=null;}
        internal static StratagemNativeParticle Play(StratagemParticleKind kind,Vector2 point,float scale,float seconds,bool loop=false,Transform parent=null)
        {
            var prefab=kind==StratagemParticleKind.Sparks?sparks:kind==StratagemParticleKind.Smoke?smoke:electric;
            var obj=Object.Instantiate(prefab,new Vector3(point.x,point.y,StratagemAssets.Z(point.y)+(kind==StratagemParticleKind.Sparks?-.03f:.4f)),Quaternion.identity);
            if(parent!=null)obj.transform.SetParent(parent,true);
            obj.transform.localScale=Vector3.one*scale;
            // These native effect prefabs contain particles and an optional impact sprite.
            foreach(var renderer in obj.GetComponentsInChildren<SpriteRenderer>(true))renderer.enabled=false;
            foreach(var renderer in obj.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {renderer.sortingLayerID=sortingLayer;renderer.sortingOrder=sortingOrder;}
            var effect=obj.AddComponent<StratagemNativeParticle>();effect.Initialize(kind,seconds,loop);
            return effect;
        }
    }
    public sealed class StratagemNativeParticle : MonoBehaviour
    {
        private ParticleSystem[] systems;
        private float until,tail;
        private bool stopping;
        private string token;
        private StratagemParticleKind kind;
        internal void Initialize(StratagemParticleKind type,float seconds,bool loop)
        {
            kind=type;token=Plugin.Combat.Token;until=Time.time+seconds;
            systems=GetComponentsInChildren<ParticleSystem>(true);
            foreach(var particles in systems)
            {
                particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=particles.main;
                main.loop=loop;main.stopAction=ParticleSystemStopAction.None;main.useUnscaledTime=false;
                main.simulationSpace=ParticleSystemSimulationSpace.World;
                main.maxParticles=type==StratagemParticleKind.Smoke?16:24;
                main.startColor=type==StratagemParticleKind.Smoke?new Color(.65f,.6f,.5f,.22f):
                    type==StratagemParticleKind.Electric?new Color(.5f,.85f,1,.45f):new Color(1,.8f,.35f,.7f);
                tail=Mathf.Max(tail,Mathf.Min(2,main.startLifetime.constantMax));
                if(loop)
                {
                    var emission=particles.emission;emission.SetBursts(new ParticleSystem.Burst[0]);
                    emission.rateOverTime=type==StratagemParticleKind.Electric?8:14;
                }
                particles.Play(false);
            }
        }
        internal void Move(Vector2 point)
        {transform.position=new Vector3(point.x,point.y,StratagemAssets.Z(point.y)+(kind==StratagemParticleKind.Sparks?-.03f:.4f));}
        internal void Stop()
        {
            if(stopping)return;
            stopping=true;until=Time.time+tail;
            foreach(var particles in systems)particles.Stop(false,ParticleSystemStopBehavior.StopEmitting);
        }
        private void Update()
        {
            if(!Plugin.InCombat||token!=Plugin.Combat?.Token){Destroy(gameObject);return;}
            if(Time.time<until)return;
            if(stopping)Destroy(gameObject);else Stop();
        }
    }
}
