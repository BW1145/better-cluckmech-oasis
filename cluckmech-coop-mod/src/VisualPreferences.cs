using BepInEx.Configuration;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class VisualPreferences
    {
        internal static readonly string[] Names={"合并密集伤害数字","合并拾币光效","精简烟雾","精简状态特效"};
        private static readonly string[] keys={"MergeDamageNumbers","MergeCoinEffects","ReduceSmoke","ReduceStatusEffects"};
        private static readonly ConfigEntry<bool>[] entries=new ConfigEntry<bool>[4];
        private static readonly bool[] active=new bool[4];
        internal static bool Numbers=>active[0];
        internal static bool Coins=>active[1];
        internal static bool Smoke=>active[2];
        internal static bool Status=>active[3];
        internal static void Bind(ConfigFile config)
        {for(int i=0;i<4;i++)entries[i]=config.Bind("Visuals",keys[i],false,Names[i]+"；本机设置，下一场战斗生效。");}
        internal static void Begin(){for(int i=0;i<4;i++)active[i]=entries[i].Value;}
        internal static void Show()
        {
            var modal=FeatureModal.Open("特效显示",800,550);if(modal==null)return;
            NativeUi.Label(modal.Panel,"本机设置，下一场战斗生效",28,90,730,48,24);
            for(int i=0;i<4;i++){int index=i;NativeUi.Toggle(modal.Panel,Names[i],28,158+i*80,730,60,entries[i].Value,value=>entries[index].Value=value);}
        }
        internal static void ThinSmoke(GameObject effect)
        {
            if(!Smoke)return;
            foreach(var particles in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main=particles.main;main.maxParticles=Mathf.Max(1,Mathf.CeilToInt(main.maxParticles*.5f));
                var emission=particles.emission;emission.rateOverTimeMultiplier*=.5f;emission.rateOverDistanceMultiplier*=.5f;
                var bursts=new ParticleSystem.Burst[emission.burstCount];emission.GetBursts(bursts);
                for(int i=0;i<bursts.Length;i++){var count=bursts[i].count;count.constantMin=Mathf.Max(1,Mathf.CeilToInt(count.constantMin*.5f));count.constantMax=Mathf.Max(1,Mathf.CeilToInt(count.constantMax*.5f));bursts[i].count=count;}
                emission.SetBursts(bursts);
            }
        }
    }
}
