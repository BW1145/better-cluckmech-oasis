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
        internal static StratagemShieldVisual Create(StratagemUnit owner)
        {
            var obj=new GameObject("StratagemShieldVisual");
            var visual=obj.AddComponent<StratagemShieldVisual>();
            visual.owner=owner;visual.token=Plugin.Combat.Token;
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
            var patch=StratagemAssets.Animated("fx.shield.hit.",8,point-normal*.08f,1.65f,.48f,hold:true);
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
            string phase=open?(age<.64f?"start":"loop"):"end";
            int frame=phase=="loop"?Mathf.FloorToInt(age*12)%8:Mathf.Min(7,Mathf.FloorToInt(age/.08f));
            shell.sprite=StratagemAssets.Named("fx.shield."+phase+"."+frame);
            // The stable contact rim spans 76% of the registered frame; glow may spill beyond it.
            shell.transform.localScale=Vector3.one*(radius*2/.76f/shell.sprite.bounds.size.x);
            shell.color=Color.white;
        }
    }
}
