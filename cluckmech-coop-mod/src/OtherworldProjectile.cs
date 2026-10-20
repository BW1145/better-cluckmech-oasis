using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    internal sealed class OtherworldProjectile:MonoBehaviour
    {
        [SerializeField] internal int Hero;
        internal OtherworldActor Owner;
        internal float NormalDamage;
        private SpriteRenderer image;
        private Sprite[] frames;
        private float started;
        private void 伤害数据传递(世界类.伤害数据包 packet)
        {
            if(Hero!=12 || packet.攻击者==null)return;
            Owner=packet.攻击者.GetComponent<OtherworldActor>();
            if(Owner!=null)NormalDamage=Owner.Attack.当前攻击力;
        }
        private void Start()
        {
            var originals=GetComponentsInChildren<SpriteRenderer>(true);
            foreach(var renderer in originals)renderer.forceRenderingOff=true;
            var visual=new GameObject("ProjectileImage");visual.transform.SetParent(transform,false);
            image=visual.AddComponent<SpriteRenderer>();
            frames=new Sprite[8];for(int i=0;i<8;i++)frames[i]=OtherworldAssets.Named(Hero==12?"electric.projectile."+i:"projectile.1."+i);
            image.sprite=frames[0];visual.transform.localScale=Vector3.one*(.7f/OtherworldAssets.VisibleBounds(frames[0]).size.x);
            if(originals.Length>0){image.sortingLayerID=originals[0].sortingLayerID;image.sortingOrder=originals[0].sortingOrder;}
            started=Time.time;
        }
        private void Update(){if(image!=null)image.sprite=frames[(int)((Time.time-started)*16)%8];}
    }
    [HarmonyPatch(typeof(模块_伤害精灵),"OnTriggerEnter2D")]
    internal static class OtherworldNormalHit
    {
        private struct Contact
        {
            internal OtherworldActor Owner;
            internal 模块_基本属性 Target;
            internal float Health,Damage;
        }
        private static void Prefix(模块_伤害精灵 __instance,Collider2D col,out Contact __state)
        {
            __state=default;
            if(Plugin.Role==2)return;
            var shot=__instance.GetComponent<OtherworldProjectile>();
            if(shot==null || shot.Hero!=12 || shot.Owner==null || shot.Owner.Run.Initial!=26002)return;
            var target=col.GetComponent<模块_基本属性>();
            if(target==null || target.当前阵营!=阵营数据库.阵营.敌方 || target.当前生命值<=0)return;
            __state=new Contact{Owner=shot.Owner,Target=target,Health=target.当前生命值,Damage=shot.NormalDamage};
        }
        private static void Postfix(Contact __state)
        {
            if(__state.Owner!=null && __state.Target!=null && __state.Target.当前生命值<__state.Health)
                OtherworldActions.Normal(__state.Owner,__state.Damage);
        }
    }
}
