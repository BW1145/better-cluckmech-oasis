using UnityEngine;

namespace CluckmechCoop
{
    public sealed class StratagemShieldVisual : MonoBehaviour
    {
        private StratagemUnit owner;
        private SpriteRenderer shell;
        private string token;
        private bool open;
        private float changed,radius;
        private Sprite[] opening,closing;
        private Sprite steady;
        private int previous=-1;
        internal static StratagemShieldVisual Create(StratagemUnit owner)
        {
            var obj=new GameObject("StratagemShieldVisual");
            var visual=obj.AddComponent<StratagemShieldVisual>();
            visual.owner=owner;visual.token=Plugin.Combat.Token;
            visual.opening=StratagemAssets.Sequence("fx.shield.start.",8);visual.closing=StratagemAssets.Sequence("fx.shield.end.",8);visual.steady=StratagemAssets.Named("fx.shield.loop.3");
            visual.shell=StratagemAssets.Renderer(obj.transform,StratagemAssets.Named("fx.shield.start.0"),1);
            visual.shell.enabled=false;
            visual.transform.position=owner.transform.position;
            return visual;
        }
        internal void SetOpen(bool value,float coverage)
        {if(value!=open){open=value;changed=Time.time;}radius=coverage;}
        internal void Hit(Vector2 point)
        {
            Vector2 normal=(point-(Vector2)transform.position).normalized;
            var patch=StratagemAssets.Animated("fx.shield.hit.",8,point,1.65f,.48f,hold:true);
            patch.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(normal.y,normal.x)*Mathf.Rad2Deg);
        }
        private void Update()
        {
            if(!Plugin.InCombat||token!=Plugin.Combat?.Token){Destroy(gameObject);return;}
            bool alive=owner!=null&&owner.Alive;
            if(!alive)SetOpen(false,radius);
            if(owner!=null)transform.position=owner.transform.position;
            transform.position=new Vector3(transform.position.x,transform.position.y,StratagemAssets.Z(transform.position.y)+.4f);
            float age=Time.time-changed;
            shell.enabled=open||age<.64f;
            if(!shell.enabled){if(!alive)Destroy(gameObject);return;}
            int phase=open?(age<.64f?0:1):2;
            int frame=phase==1?3:Mathf.Min(7,Mathf.FloorToInt(age/.08f));
            if(previous!=phase*8+frame){previous=phase*8+frame;shell.sprite=phase==0?opening[frame]:phase==2?closing[frame]:steady;}
            // The stable contact rim spans 76% of the registered frame; all phases share this registered footprint.
            shell.transform.localScale=Vector3.one*(radius*2/.76f/shell.sprite.bounds.size.x);
            shell.color=Color.white;
        }
    }
}
